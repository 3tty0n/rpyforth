#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from statistics import median
from typing import Dict

sys.path.insert(0, str(Path(__file__).resolve().parent))
from benchlib import plotting  # noqa: E402
from benchlib.engines import (  # noqa: E402
    BASELINE_ENGINES,
    engine_color,
    engine_display_name,
    normalize_engine,
    sort_engines,
)
from benchlib.stats import geomean  # noqa: E402,F401
from benchlib.suites import (  # noqa: E402
    APPBENCH_PROGRAMS,
    SHOOTOUT_PROGRAMS,
    SUITE_PROGRAMS,
)

SHOOTOUT_BENCHMARKS = SHOOTOUT_PROGRAMS
APPBENCH_BENCHMARKS = APPBENCH_PROGRAMS
BASELINES = BASELINE_ENGINES
SUITE_BENCHS = SUITE_PROGRAMS


def percentile(sorted_values, percent):
    if not sorted_values:
        raise ValueError("cannot compute percentile of an empty list")
    if len(sorted_values) == 1:
        return sorted_values[0]
    rank = (len(sorted_values) - 1) * percent
    lower = int(rank)
    upper = min(lower + 1, len(sorted_values) - 1)
    fraction = rank - lower
    return sorted_values[lower] * (1.0 - fraction) + sorted_values[upper] * fraction


def box_stats(values):
    values = sorted(values)
    return {
        "lower whisker": values[0],
        "lower quartile": percentile(values, 0.25),
        "median": median(values),
        "upper quartile": percentile(values, 0.75),
        "upper whisker": values[-1],
    }


def _warm_of(entry):
    if entry is None:
        return None
    if isinstance(entry, (int, float)):
        return float(entry)
    if isinstance(entry, dict):
        for key in ("warm_median_usec", "usec", "elapsed_usec", "median_usec"):
            if key in entry and entry[key] is not None:
                return float(entry[key])
    return None


def load_benchmarks(path: Path) -> Dict[str, Dict[str, float]]:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)

    out: Dict[str, Dict[str, float]] = {}

    if "benchmarks" in data and isinstance(data["benchmarks"], dict):
        for prog, by_engine in data["benchmarks"].items():
            for engine, entry in by_engine.items():
                warm = _warm_of(entry)
                if warm is None or warm <= 0:
                    continue
                out.setdefault(prog, {})[normalize_engine(engine)] = warm
        return out

    if "appbench" in data and "shootout" in data:
        for suite in ("appbench", "shootout"):
            runs = (data.get(suite) or {}).get("runs") or []
            if not runs:
                continue
            programs = runs[0].get("programs") or {}
            for prog, info in programs.items():
                usec = info.get("usec") or {}
                for engine, warm in usec.items():
                    if warm is None or float(warm) <= 0:
                        continue
                    out.setdefault(prog, {})[normalize_engine(engine)] = float(warm)
        return out

    if "results" in data and isinstance(data["results"], list):
        for row in data["results"]:
            prog = row.get("program")
            engine = row.get("engine")
            warm = row.get("warm_median_usec")
            if not prog or not engine or warm is None or float(warm) <= 0:
                continue
            out.setdefault(prog, {})[normalize_engine(engine)] = float(warm)
        return out

    raise ValueError(
        "unsupported JSON shape in %s (need benchmarks, dashboard data, or steady_results)"
        % path
    )


def collect_engine_speedups(warm, benchmarks, baseline, engines):
    by_engine = {engine: [] for engine in engines}
    skipped = []
    for benchmark in benchmarks:
        entry = warm.get(benchmark)
        if entry is None or baseline not in entry:
            skipped.append(benchmark)
            continue
        base_time = float(entry[baseline])
        if base_time <= 0:
            skipped.append(benchmark)
            continue
        for engine in engines:
            if engine not in entry:
                continue
            target = float(entry[engine])
            if target <= 0:
                continue
            by_engine[engine].append(base_time / target)
    if skipped:
        print(
            "warning: skipped benchmarks: %s" % ", ".join(skipped),
            file=sys.stderr,
        )
    return {e: vals for e, vals in by_engine.items() if vals}


def collect_rpy_vs_baselines(warm, benchmarks, baselines, rpy="rpyforth"):
    by_base = {b: [] for b in baselines}
    for benchmark in benchmarks:
        entry = warm.get(benchmark)
        if entry is None or rpy not in entry:
            continue
        rpy_time = float(entry[rpy])
        if rpy_time <= 0:
            continue
        for base in baselines:
            if base not in entry:
                continue
            other = float(entry[base])
            if other <= 0:
                continue
            by_base[base].append(other / rpy_time)
    return {b: vals for b, vals in by_base.items() if vals}


def bxp_entry(label, stats):
    return {
        "label": label,
        "whislo": stats["lower whisker"],
        "q1": stats["lower quartile"],
        "med": stats["median"],
        "q3": stats["upper quartile"],
        "whishi": stats["upper whisker"],
        "fliers": [],
    }


def render_pdf(stats_by_label, colors_by_label, output_path, xlabel, title=None):
    plt = plotting.pyplot()

    output_path = Path(output_path).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)

    rows = list(stats_by_label.keys())
    box_data = [bxp_entry(label, stats_by_label[label]) for label in rows]
    all_vals = []
    for stats in stats_by_label.values():
        all_vals.extend(
            [
                stats["lower whisker"],
                stats["upper whisker"],
            ]
        )

    fig, ax = plt.subplots(figsize=(8.4, max(2.4, 0.55 * len(rows) + 1.4)))
    artists = ax.bxp(
        box_data,
        orientation="horizontal",
        patch_artist=True,
        showfliers=False,
    )

    for box, label in zip(artists["boxes"], rows):
        color = colors_by_label.get(label, "#d8e9ff")
        box.set(facecolor=color, edgecolor="#333333", linewidth=1.1, alpha=0.75)

    for median_line in artists["medians"]:
        median_line.set(color="#1f1f1f", linewidth=1.4)

    for whisker in artists["whiskers"]:
        whisker.set(color="#444444", linewidth=1.1)

    for cap in artists["caps"]:
        cap.set(color="#444444", linewidth=1.1)

    lo, hi = plotting.nice_log_limits(all_vals, floor=0.05)
    ticks, labels = plotting.ratio_log_ticks(lo, hi)
    plotting.apply_log_axis(ax, all_vals, axis="x", ticks=ticks, labels=labels,
                            floor=0.05)
    ax.tick_params(axis="x", labelsize=9, pad=4)
    ax.tick_params(axis="y", labelsize=9)
    for label in ax.get_xticklabels():
        label.set_rotation(30)
        label.set_horizontalalignment("right")
    ax.invert_yaxis()
    ax.axvline(1.0, color="#d75a5a", linestyle="--", linewidth=1.2)
    ax.set_xlabel(xlabel)
    if title:
        ax.set_title(title)
    ax.grid(True, which="major", axis="both", color="#d8d8d8", linewidth=0.7)
    ax.set_axisbelow(True)
    fig.tight_layout(pad=0.8)
    plotting.save_figure(fig, output_path)


def parse_args(argv):
    parser = argparse.ArgumentParser(
        description="Warm-tail speedup boxplot with one box per engine or baseline."
    )
    parser.add_argument(
        "input_json",
        help="warmup_curves.json, dashboard/data.json, or steady_results.json",
    )
    parser.add_argument(
        "-o",
        "--output",
        default=None,
        help="Output PDF path (default depends on --suite/--mode)",
    )
    parser.add_argument(
        "--baseline",
        default="gforth-fast",
        help="Baseline for --mode engines (default: gforth-fast)",
    )
    parser.add_argument(
        "--suite",
        choices=("shootout", "appbench", "all"),
        default="all",
        help="Which benchmark suite to include",
    )
    parser.add_argument(
        "--mode",
        choices=("engines", "rpy-vs-baselines"),
        default="engines",
        help="engines: one box/engine vs baseline; "
        "rpy-vs-baselines: one box/baseline of baseline/rpyforth",
    )
    parser.add_argument(
        "--title",
        default=None,
        help="Optional plot title",
    )
    return parser.parse_args(argv)


def default_output(suite, mode):
    if mode == "rpy-vs-baselines":
        return "%s_rpy_speedup_boxplot.pdf" % suite
    return "%s_boxplot.pdf" % suite


def main(argv=None):
    args = parse_args(argv or sys.argv[1:])
    warm = load_benchmarks(Path(args.input_json))
    benchmarks = [
        name
        for name in SUITE_BENCHS[args.suite]
        if name in warm
    ]
    if not benchmarks:
        raise SystemExit("no %s benchmarks found in %s" % (args.suite, args.input_json))

    output = args.output or default_output(args.suite, args.mode)

    if args.mode == "engines":
        present = {e for prog in benchmarks for e in warm[prog]}
        engines = sort_engines(present)
        if args.baseline not in present:
            raise ValueError("baseline %r not found" % args.baseline)
        by_label = collect_engine_speedups(
            warm, benchmarks, args.baseline, engines
        )
        labels = list(by_label.keys())
        stats_by_label = {
            engine_display_name(eng): box_stats(vals)
            for eng, vals in by_label.items()
        }
        colors = {
            engine_display_name(eng): engine_color(eng) for eng in labels
        }
        gm = {
            engine_display_name(eng): geomean(vals)
            for eng, vals in by_label.items()
        }
        xlabel = (
            "warm-tail speedup over %s (log scale, >1 = faster)" % args.baseline
        )
        title = args.title or (
            "%s: warm-tail speedup vs %s (n=%d)"
            % (args.suite, args.baseline, len(benchmarks))
        )
    else:
        by_label = collect_rpy_vs_baselines(warm, benchmarks, BASELINES)
        if not by_label:
            raise SystemExit("no rpyforth/baseline pairs found")
        stats_by_label = {b: box_stats(vals) for b, vals in by_label.items()}
        colors = {b: engine_color(b) for b in by_label}
        gm = {b: geomean(vals) for b, vals in by_label.items()}
        xlabel = "warm-tail speedup = baseline / rpyforth (log, >1 = rpy faster)"
        title = args.title or (
            "%s: rpyforth speedup vs baselines (n=%d)"
            % (args.suite, len(benchmarks))
        )

    render_pdf(stats_by_label, colors, output, xlabel, title=title)

    print("suite=%s mode=%s programs=%s" % (args.suite, args.mode, ",".join(benchmarks)))
    for label, stats in stats_by_label.items():
        g = gm.get(label)
        g_s = "%.3fx" % g if g is not None else "n/a"
        formatted = ", ".join("%s=%.3f" % (key, value) for key, value in stats.items())
        print("%s: geomean=%s; %s" % (label, g_s, formatted))
    print("wrote %s" % output)


if __name__ == "__main__":
    main()
