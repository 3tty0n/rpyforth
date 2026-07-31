#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "dashboard"))

from benchlib.engines import (  # noqa: E402
    PRIMARY_ENGINES,
    REFERENCE_ENGINE,
    engine_color,
    engine_display_name,
    normalize_engine,
)
from benchlib.stats import geomean  # noqa: E402
from build_data import (  # noqa: E402
    load_shootout_curve_dir,
    load_shootout_steady_json,
    shootout_log_candidates,
    warm_ok,
)
from plot_warmup_boxplot import (  # noqa: E402
    APPBENCH_BENCHMARKS,
    BASELINES,
    SHOOTOUT_BENCHMARKS,
    box_stats,
    collect_engine_speedups,
    collect_rpy_vs_baselines,
    render_pdf,
)

REPO_ROOT = Path(__file__).resolve().parent.parent
RPY = "rpyforth"


def load_appbench_run(path: Path) -> Dict[str, Dict[str, float]]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    warm: Dict[str, Dict[str, float]] = {}
    for row in payload.get("results", []):
        prog = row.get("program")
        eng = normalize_engine(row.get("engine", ""))
        usec = row.get("warm_median_usec")
        if not prog or not eng or usec is None or float(usec) <= 0:
            continue
        warm.setdefault(prog, {})[eng] = float(usec)
    return {p: e for p, e in warm.items() if warm_ok(e)}


def load_shootout_run(log_dir: Path) -> Dict[str, Dict[str, float]]:
    steady = log_dir / "shootout_steady.json"
    if steady.is_file():
        warm = load_shootout_steady_json(steady)
    else:
        warm = load_shootout_curve_dir(log_dir)
    return {p: e for p, e in warm.items() if warm_ok(e)}


def pick_best_appbench(logs_root: Path) -> Tuple[Path, Dict[str, Dict[str, float]]]:
    candidates = []
    for path in logs_root.rglob("steady_results.json"):
        if "appbench" not in path.parts:
            continue
        warm = load_appbench_run(path)
        if not warm:
            continue
        candidates.append((len(warm), path.stat().st_mtime, path, warm))
    if not candidates:
        raise SystemExit("no usable appbench steady_results.json under %s" % logs_root)
    candidates.sort(key=lambda x: (x[0], x[1]), reverse=True)
    _n, _mtime, path, warm = candidates[0]
    return path, warm


def pick_best_shootout(logs_root: Path) -> Tuple[Path, Dict[str, Dict[str, float]]]:
    candidates = []
    seen = set()
    for mtime, path, kind, _rev in shootout_log_candidates(logs_root):
        log_dir = path.parent if kind == "shootout_steady.json" else path
        key = str(log_dir.resolve())
        if key in seen:
            continue
        warm = load_shootout_run(log_dir)
        if not warm:
            continue
        seen.add(key)
        candidates.append((len(warm), mtime, log_dir, warm))
    if not candidates:
        raise SystemExit("no usable shootout logs under %s" % logs_root)
    candidates.sort(key=lambda x: (x[0], x[1]), reverse=True)
    _n, _mtime, path, warm = candidates[0]
    return path, warm


def coverage_report(suite: str, expected: List[str], warm: Dict[str, Dict[str, float]]) -> None:
    present = [p for p in expected if p in warm]
    missing = [p for p in expected if p not in warm]
    incomplete = []
    for prog in present:
        engs = set(warm[prog])
        lack = [b for b in (RPY,) + BASELINES if b not in engs]
        if lack:
            incomplete.append("%s(missing:%s)" % (prog, ",".join(lack)))
    print(
        "%s coverage: %d/%d programs with rpyforth + >=2 baselines"
        % (suite, len(present), len(expected))
    )
    if missing:
        print("  missing programs: %s" % ", ".join(missing))
    if incomplete:
        print("  incomplete engine sets: %s" % ", ".join(incomplete))


def emit_suite(
    suite: str,
    warm: Dict[str, Dict[str, float]],
    expected: List[str],
    out_dir: Path,
    source: Path,
) -> None:
    coverage_report(suite, expected, warm)
    print("  source: %s" % source)
    programs = [p for p in expected if p in warm]
    if not programs:
        raise SystemExit("%s: no programs to plot" % suite)

    out_dir.mkdir(parents=True, exist_ok=True)

    engines = [e for e in PRIMARY_ENGINES if any(e in warm[p] for p in programs)]
    by_eng = collect_engine_speedups(warm, programs, REFERENCE_ENGINE, engines)
    stats = {engine_display_name(e): box_stats(v) for e, v in by_eng.items()}
    colors = {engine_display_name(e): engine_color(e) for e in by_eng}
    path = out_dir / ("%s_boxplot.pdf" % suite)
    render_pdf(
        stats,
        colors,
        path,
        "warm-tail speedup over gforth-fast (log, >1 = faster)",
        title="%s: warm-tail speedup vs gforth-fast (n=%d)" % (suite, len(programs)),
    )
    print("  wrote %s" % path)
    for label, vals in by_eng.items():
        g = geomean(vals)
        print(
            "    vs-gforth %s: geomean=%s median=%.3f n=%d"
            % (
                engine_display_name(label),
                ("%.3fx" % g) if g else "n/a",
                box_stats(vals)["median"],
                len(vals),
            )
        )

    by_base = collect_rpy_vs_baselines(warm, programs, BASELINES)
    stats = {b: box_stats(v) for b, v in by_base.items()}
    colors = {b: engine_color(b) for b in by_base}
    path = out_dir / ("%s_rpy_speedup_boxplot.pdf" % suite)
    render_pdf(
        stats,
        colors,
        path,
        "warm-tail speedup = baseline / rpyforth (log, >1 = rpy faster)",
        title="%s: rpyforth speedup vs baselines (n=%d)" % (suite, len(programs)),
    )
    print("  wrote %s" % path)
    for base, vals in by_base.items():
        g = geomean(vals)
        print(
            "    vs-%s: geomean=%s median=%.3f n=%d"
            % (
                base,
                ("%.3fx" % g) if g else "n/a",
                box_stats(vals)["median"],
                len(vals),
            )
        )


def parse_args(argv: Optional[List[str]] = None):
    p = argparse.ArgumentParser(
        description="Generate blog boxplots for shootout and appbench from logs/."
    )
    p.add_argument("--logs", type=Path, default=REPO_ROOT / "logs")
    p.add_argument("--out", type=Path, default=REPO_ROOT / "tmp" / "blog")
    p.add_argument(
        "--suite",
        choices=("shootout", "appbench", "all"),
        default="all",
    )
    return p.parse_args(argv)


def main(argv: Optional[List[str]] = None) -> int:
    args = parse_args(argv)
    logs = args.logs if args.logs.is_absolute() else REPO_ROOT / args.logs
    out = args.out if args.out.is_absolute() else REPO_ROOT / args.out
    if not logs.is_dir():
        print("logs root not found: %s" % logs, file=sys.stderr)
        return 1

    if args.suite in ("shootout", "all"):
        path, warm = pick_best_shootout(logs)
        emit_suite("shootout", warm, SHOOTOUT_BENCHMARKS, out, path)
    if args.suite in ("appbench", "all"):
        path, warm = pick_best_appbench(logs)
        emit_suite("appbench", warm, APPBENCH_BENCHMARKS, out, path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
