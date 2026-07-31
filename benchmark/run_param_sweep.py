#!/usr/bin/env python3
"""
FRAME_SIZE sensitivity sweep for rpyforth-c-stkfrag.

For each FRAME_SIZE F in a configurable list, build rpyforth-c-stkfrag-f<F>
(reusing the cached binary when it is newer than rpyforth/*.py), run the
shootout benchmarks (excluding curve/), and report a table normalized to F=8.

Usage:
    .venv/bin/python benchmark/run_param_sweep.py [options]

Options:
    --frame-sizes F,...   Comma-separated list of FRAME_SIZE values (default: 2,4,8,16,32)
    --iterations N        Runs per benchmark per binary (default: 3)
    --pin CORE            CPU core to pin benchmark runs on (default: 3)
    --pdf PATH            Write a chart to this path (extension sets format: .pdf .png .svg)
    --no-build            Skip build step; require all binaries to exist already
"""

import argparse
import math
import shutil
import subprocess
import sys
import time
from pathlib import Path
from typing import Dict, List, Optional

sys.path.insert(0, str(Path(__file__).resolve().parent))
from benchlib import plotting, procs, stats
from run_shootout import (
    discover_benchmarks,
    run_benchmark,
    analyze_result,
    aggregate_iterations,
    BenchmarkResult,
)

REPO_ROOT = Path(__file__).resolve().parent.parent
PYTHON2 = REPO_ROOT / "_pypy_binary" / "bin" / "python2"
RPYTHON = REPO_ROOT / "pypy" / "rpython" / "bin" / "rpython"
RPYFORTH_PY = REPO_ROOT / "rpyforth"
TARGET = "targetrpyforth"

DEFAULT_FRAME_SIZES = [2, 4, 8, 16, 32]
DEFAULT_ITERATIONS = 3
DEFAULT_PIN = procs.DEFAULT_PIN
DEFAULT_TIMEOUT = procs.DEFAULT_TIMEOUT


def binary_name(f: int) -> str:
    return "rpyforth-c-stkfrag-f%d" % f


def binary_path(f: int) -> Path:
    return REPO_ROOT / binary_name(f)


def source_mtime() -> float:
    mt = 0.0
    for p in RPYFORTH_PY.glob("*.py"):
        mt = max(mt, p.stat().st_mtime)
    return mt


def binary_is_fresh(f: int) -> bool:
    bp = binary_path(f)
    if not bp.exists():
        return False
    src_mt = source_mtime()
    return bp.stat().st_mtime >= src_mt


def build_binary(f: int) -> bool:
    bp = binary_path(f)
    print("Building %s (FRAME_SIZE=%d) ..." % (bp.name, f), flush=True)
    env = procs.engine_env(extra={
        "RPYFORTH_STACK_LAYOUT": "fragment",
        "RPYFORTH_FRAME_SIZE": str(f),
        "RPYFORTH_EXE_NAME": binary_name(f),
        "PYTHONPATH": str(REPO_ROOT),
    })
    cmd = [
        str(PYTHON2), str(RPYTHON), "-Ojit",
        "rpyforth/" + TARGET + ".py",
    ]
    start = time.time()
    proc = subprocess.run(
        cmd,
        env=env,
        cwd=str(REPO_ROOT),
    )
    elapsed = time.time() - start
    if proc.returncode != 0:
        print("ERROR: build failed for F=%d (exit %d, %.0fs)" % (f, proc.returncode, elapsed))
        return False
    if not bp.exists():
        print("ERROR: build completed but binary not found at %s" % bp)
        return False
    print("Built %s in %.0fs" % (bp.name, elapsed), flush=True)
    return True


def ensure_binary(f: int, no_build: bool) -> bool:
    if binary_is_fresh(f):
        print("Using cached binary %s" % binary_name(f), flush=True)
        return True
    if no_build:
        print("ERROR: binary %s not found and --no-build set" % binary_name(f))
        return False
    return build_binary(f)


def run_benchmarks_for_f(
    f: int,
    iterations: int,
    pin: Optional[int],
    timeout: int,
) -> Dict[str, Optional[int]]:
    bp = binary_path(f)
    cmd_prefix = [str(bp)]
    wrapper: List[str] = procs.pin_prefix(pin)

    benchmarks = discover_benchmarks(REPO_ROOT)
    benchmarks = [b for b in benchmarks if "curve/" not in str(b.relative_to(REPO_ROOT))]

    all_results: List[BenchmarkResult] = []
    for bench in benchmarks:
        for iteration in range(1, iterations + 1):
            result = run_benchmark(
                cmd_prefix, bench, [], timeout, "A", REPO_ROOT, wrapper=wrapper
            )
            result.iteration = iteration
            analyze_result(result)
            all_results.append(result)

    aggregated = aggregate_iterations(all_results)
    out: Dict[str, Optional[int]] = {}
    for r in aggregated:
        out[r.name] = r.elapsed_usec
    return out


def print_table(
    frame_sizes: List[int],
    results: Dict[int, Dict[str, Optional[int]]],
) -> None:
    ref_f = 8 if 8 in frame_sizes else frame_sizes[0]
    ref = results.get(ref_f, {})

    all_benchmarks: List[str] = []
    for f in frame_sizes:
        for b in results.get(f, {}):
            if b not in all_benchmarks:
                all_benchmarks.append(b)
    all_benchmarks.sort()

    col_w = 14
    name_w = 30
    header = ("%-*s" % (name_w, "Benchmark"))
    for f in frame_sizes:
        label = "F=%d" % f
        header += ("  %*s" % (col_w, label))
        if f != ref_f:
            header += ("  %*s" % (col_w, "vs F=%d" % ref_f))
    print(header)
    print("-" * (name_w + (col_w + 2) * len(frame_sizes) * 2))

    all_ratios: Dict[int, List[float]] = {f: [] for f in frame_sizes if f != ref_f}

    for bench in all_benchmarks:
        short = bench.replace("shootout/", "").replace(".fs", "")
        row = "%-*s" % (name_w, short)
        ref_t = ref.get(bench)
        for f in frame_sizes:
            t = results.get(f, {}).get(bench)
            if t is not None:
                row += ("  %*s" % (col_w, "%d us" % t))
            else:
                row += ("  %*s" % (col_w, "-"))
            if f != ref_f:
                ratio = stats.speedup(t, ref_t)
                if ratio is not None:
                    all_ratios[f].append(ratio)
                    row += ("  %*.2fx" % (col_w - 1, ratio))
                else:
                    row += ("  %*s" % (col_w, "-"))
        print(row)

    print("-" * (name_w + (col_w + 2) * len(frame_sizes) * 2))
    gm_row = "%-*s" % (name_w, "geomean")
    for f in frame_sizes:
        gm = stats.geomean(results.get(f, {}).get(b) for b in all_benchmarks)
        if gm is not None:
            gm_row += ("  %*s" % (col_w, "%.0f us" % gm))
        else:
            gm_row += ("  %*s" % (col_w, "-"))
        if f == ref_f:
            continue
        gm_ratio = stats.geomean(all_ratios[f])
        if gm_ratio is not None:
            gm_row += ("  %*.2fx" % (col_w - 1, gm_ratio))
        else:
            gm_row += ("  %*s" % (col_w, "-"))
    print(gm_row)


def write_chart(
    chart_path: Path,
    frame_sizes: List[int],
    results: Dict[int, Dict[str, Optional[int]]],
) -> None:
    try:
        plt = plotting.pyplot()
    except RuntimeError:
        print("matplotlib not available; skipping chart", file=sys.stderr)
        return

    ref_f = 8 if 8 in frame_sizes else frame_sizes[0]
    ref = results.get(ref_f, {})

    all_benchmarks: List[str] = []
    for f in frame_sizes:
        for b in results.get(f, {}):
            if b not in all_benchmarks:
                all_benchmarks.append(b)
    all_benchmarks.sort()

    x = [math.log2(f) for f in frame_sizes]

    fig, ax = plt.subplots(figsize=(9, 5))

    all_ratio_by_f: Dict[int, List[float]] = {}
    for bench in all_benchmarks:
        ref_t = ref.get(bench)
        if ref_t is None or ref_t <= 0:
            continue
        y = []
        for f in frame_sizes:
            ratio = stats.speedup(results.get(f, {}).get(bench), ref_t)
            y.append(float("nan") if ratio is None else ratio)
        short = bench.replace("shootout/", "").replace(".fs", "")
        ax.plot(x, y, marker="o", linewidth=1, alpha=0.5, label=short)
        for fi, f in enumerate(frame_sizes):
            if not math.isnan(y[fi]):
                all_ratio_by_f.setdefault(f, []).append(y[fi])

    gm_y = []
    for f in frame_sizes:
        gm = stats.geomean(all_ratio_by_f.get(f, []))
        gm_y.append(float("nan") if gm is None else gm)

    ax.plot(x, gm_y, marker="D", linewidth=2.5, color="black", label="geomean", zorder=10)
    ax.axhline(1.0, color="gray", linewidth=0.8, linestyle="--")

    ax.set_xlabel("FRAME_SIZE (log2)")
    ax.set_ylabel("Time normalized to F=%d" % ref_f)
    ax.set_title("rpyforth FRAME_SIZE sensitivity sweep")
    ax.set_xticks(x)
    ax.set_xticklabels([str(f) for f in frame_sizes])
    ax.legend(fontsize=7, ncol=2, loc="upper right")
    fig.tight_layout()

    suffix = chart_path.suffix.lower()
    fmt = suffix.lstrip(".") if suffix in (".pdf", ".png", ".svg") else "pdf"
    fig.savefig(str(chart_path), format=fmt)
    plt.close(fig)
    print("Chart written to %s" % chart_path, flush=True)


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        description="FRAME_SIZE sensitivity sweep for rpyforth shootout benchmarks."
    )
    parser.add_argument(
        "--frame-sizes",
        type=str,
        default=",".join(str(f) for f in DEFAULT_FRAME_SIZES),
        help="Comma-separated FRAME_SIZE values (default: %s)" % ",".join(str(f) for f in DEFAULT_FRAME_SIZES),
    )
    parser.add_argument(
        "--iterations",
        type=int,
        default=DEFAULT_ITERATIONS,
        help="Benchmark runs per binary (default: %d)" % DEFAULT_ITERATIONS,
    )
    parser.add_argument(
        "--pin",
        type=int,
        default=DEFAULT_PIN,
        help="CPU core to pin benchmark runs on (default: %d)" % DEFAULT_PIN,
    )
    parser.add_argument(
        "--pdf",
        type=Path,
        default=None,
        help="Chart output path (format from extension: .pdf .png .svg)",
    )
    parser.add_argument(
        "--no-build",
        action="store_true",
        help="Skip builds; require binaries to exist already",
    )
    parser.add_argument(
        "--timeout",
        type=int,
        default=DEFAULT_TIMEOUT,
        help="Per-benchmark timeout in seconds (default: %d)" % DEFAULT_TIMEOUT,
    )
    args = parser.parse_args(argv)

    frame_sizes: List[int] = []
    for part in args.frame_sizes.split(","):
        part = part.strip()
        if part.isdigit():
            frame_sizes.append(int(part))
    if not frame_sizes:
        print("Error: no valid FRAME_SIZE values", file=sys.stderr)
        return 1

    if args.pin is not None and shutil.which("taskset") is None:
        print("Error: --pin requires taskset (util-linux)", file=sys.stderr)
        return 1

    print("FRAME_SIZE sweep: %s" % frame_sizes, flush=True)

    build_ok: Dict[int, bool] = {}
    for f in frame_sizes:
        build_ok[f] = ensure_binary(f, args.no_build)

    results: Dict[int, Dict[str, Optional[int]]] = {}
    for f in frame_sizes:
        if not build_ok[f]:
            print("Skipping benchmarks for F=%d (build failed)" % f, flush=True)
            continue
        print("\nRunning benchmarks for F=%d ..." % f, flush=True)
        results[f] = run_benchmarks_for_f(f, args.iterations, args.pin, args.timeout)

    fs_with_results = [f for f in frame_sizes if f in results]
    if not fs_with_results:
        print("No results to report.", file=sys.stderr)
        return 1

    print("\n")
    print_table(fs_with_results, results)

    if args.pdf:
        chart_path = args.pdf if args.pdf.is_absolute() else REPO_ROOT / args.pdf
        write_chart(chart_path, fs_with_results, results)

    failed = [f for f in frame_sizes if not build_ok.get(f, False)]
    if failed:
        print("\nFailed to build F=%s" % failed)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
