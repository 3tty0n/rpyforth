#!/usr/bin/env python3
"""NTOP sweep: measure shootout + appbench absolute times across the
NTOP in {0,2,4,8,16} scalar-top ablation binaries and render a PDF.

NTOP=0  rpyforth-c-stkfrag-frameonly   (frame-only ablation)
NTOP=2  rpyforth-c-stkfrag             (flagship)
NTOP=4/8/16  rpyforth-c-stkfrag-ntopN  (parametric sweep builds)

Per benchmark the binaries run interleaved (bench outer, binary inner) to
control thermal/frequency drift. Warm time = median of the last 50% of
in-process iterations. Results: logs/ntop-sweep/<git-rev>/ntop_sweep.json
+ ntop_sweep.pdf (absolute times, log scale).
"""
from __future__ import annotations

import argparse
import json
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from benchlib import curves, paths, plotting, procs, stats, suites  # noqa: E402
from benchlib.engines import ENGINE_RPYFORTH  # noqa: E402
from run_appbench import (  # noqa: E402
    PROGRAMS, build_driver, build_cmd, prepare_engine_workdir,
)
from run_ablation import build_shootout_driver  # noqa: E402
import run_rq  # noqa: E402

REPO_ROOT = paths.REPO_ROOT

# The scalar-top depths this sweep covers, in chart order.
NTOPS = [0, 2, 4, 8, 16]

NTOP_BINARIES = {
    0: REPO_ROOT / "rpyforth-c-stkfrag-frameonly",
    2: REPO_ROOT / "rpyforth-c-stkfrag",
    4: REPO_ROOT / "rpyforth-c-stkfrag-ntop4",
    8: REPO_ROOT / "rpyforth-c-stkfrag-ntop8",
    16: REPO_ROOT / "rpyforth-c-stkfrag-ntop16",
}

SHOOTOUT_KERNELS = suites.SHOOTOUT_KERNELS


def run_driver(binary, driver_path, cwd, pin, timeout, env=None):
    outcome = procs.run_capture([str(binary), str(driver_path)], cwd=cwd,
                                env=env, timeout=timeout, pin=pin)
    return outcome.rc, curves.parse_curve_output(outcome.stdout)


def measure_shootout(iterations, pin, timeout, tmpdir):
    rows = {}
    for kernel in SHOOTOUT_KERNELS:
        bench = REPO_ROOT / "shootout" / ("%s.fs" % kernel)
        driver = build_shootout_driver(bench, iterations)
        dp = Path(tmpdir) / ("%s_sweep.fs" % kernel)
        dp.write_text(driver, encoding="utf-8")
        row = {}
        for ntop, binary in sorted(NTOP_BINARIES.items()):
            rc, times = run_driver(binary, dp, REPO_ROOT / "shootout",
                                   pin, timeout)
            if rc != 0 or not times:
                print("  shootout %-11s NTOP=%-2d FAILED rc=%d" %
                      (kernel, ntop, rc), flush=True)
                row[str(ntop)] = None
                continue
            warm = curves.steady_state_tail(times)
            row[str(ntop)] = {"cold_usec": curves.cold_usec(times),
                              "warm_median_usec": warm,
                              "n": len(times)}
            print("  shootout %-11s NTOP=%-2d warm=%.0fus" %
                  (kernel, ntop, warm), flush=True)
        rows[kernel] = row
    return rows


def measure_appbench(iterations, pin, timeout, tmpdir, lexex_iterations=None):
    rows = {}
    for spec in PROGRAMS:
        iters = suites.iterations_for(spec.name, iterations, lexex_iterations)
        patched = prepare_engine_workdir(ENGINE_RPYFORTH, spec, tmpdir)
        run_spec = (spec if patched == Path(spec.workdir)
                    else run_rq.with_workdir(spec, patched))
        driver = build_driver(run_spec, iters, ENGINE_RPYFORTH)
        dp = Path(tmpdir) / ("%s_sweep_drv.fs" % spec.name)
        dp.write_text(driver, encoding="utf-8")
        base_cmd = build_cmd(ENGINE_RPYFORTH, dp, run_spec)
        env = procs.engine_env(extra=run_spec.rpy_env)
        row = {}
        for ntop, binary in sorted(NTOP_BINARIES.items()):
            outcome = procs.run_capture([str(binary)] + base_cmd[1:],
                                        cwd=run_spec.workdir, env=env,
                                        timeout=timeout, pin=pin)
            rc = outcome.rc
            times = curves.parse_curve_output(outcome.stdout)
            if rc != 0 or not times:
                print("  appbench %-11s NTOP=%-2d FAILED rc=%d" %
                      (spec.name, ntop, rc), flush=True)
                row[str(ntop)] = None
                continue
            warm = curves.steady_state_tail(times)
            row[str(ntop)] = {"cold_usec": curves.cold_usec(times),
                              "warm_median_usec": warm,
                              "n": len(times)}
            print("  appbench %-11s NTOP=%-2d warm=%.0fus" %
                  (spec.name, ntop, warm), flush=True)
        rows[spec.name] = row
    return rows


def plot(results, pdf_path):
    plt = plotting.pyplot()

    colors = plt.get_cmap("viridis")([0.05, 0.28, 0.5, 0.72, 0.92])
    offsets = plotting.group_offsets(len(NTOPS))
    width = plotting.bar_width(len(NTOPS))
    with plotting.pdf_pages(pdf_path) as pdf:
        for suite, unit_div, unit in (("shootout", 1.0, "µs"),
                                      ("appbench", 1000.0, "ms")):
            data = results.get(suite)
            if not data:
                continue
            names = [n for n in data if any(data[n].values())]
            fig, ax = plt.subplots(figsize=(max(8, 0.9 * (len(names) + 1) * 1.6), 5),
                                   constrained_layout=True)
            xs = range(len(names) + 1)
            drawn = []
            for j, ntop in enumerate(NTOPS):
                vals = []
                for name in names:
                    cell = data[name].get(str(ntop))
                    vals.append(cell["warm_median_usec"] / unit_div
                                if cell else float("nan"))
                gm = stats.geomean(vals)
                bars = vals + [float("nan") if gm is None else gm]
                drawn.extend(bars)
                ax.bar([x + offsets[j] for x in xs], bars, width,
                       label="NTOP=%d" % ntop, color=colors[j])
            plotting.add_geomean_separator(ax, len(names) + 1, horizontal=False)
            plotting.apply_log_axis(ax, drawn)
            ax.set_xticks(list(xs))
            ax.set_xticklabels(names + ["geomean"], rotation=30, ha="right")
            ax.set_ylabel("warm median per iteration [%s, log]" % unit)
            ax.set_title("%s: absolute steady-state time by NTOP "
                         "(lower is better)" % suite)
            ax.grid(axis="y", alpha=0.3, which="both")
            ax.legend(ncol=5, fontsize=9)
            plotting.save_figure(fig, pdf)
    print("wrote %s" % pdf_path)


def geomean_vs2(rows):
    out = {}
    for ntop in NTOPS:
        ratios = []
        for row in rows.values():
            a, b = row.get("2"), row.get(str(ntop))
            if a and b:
                ratios.append(stats.speedup(b["warm_median_usec"],
                                            a["warm_median_usec"]))
        out[ntop] = stats.geomean(ratios)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--pin", type=int, default=3)
    ap.add_argument("--shootout-iterations", type=int, default=30)
    ap.add_argument("--appbench-iterations", type=int, default=50)
    ap.add_argument("--lexex-iterations", type=int,
                    default=suites.LEXEX_ITERATIONS,
                    help="iterations for lexex, which rebuilds its FSM tables "
                         "every pass and is an order of magnitude slower than "
                         "the rest (default: %d)" % suites.LEXEX_ITERATIONS)
    ap.add_argument("--timeout", type=int, default=900)
    ap.add_argument("--suites", default="shootout,appbench")
    args = ap.parse_args()

    for ntop, b in NTOP_BINARIES.items():
        if not b.exists():
            print("missing binary for NTOP=%d: %s" % (ntop, b),
                  file=sys.stderr)
            return 1

    rev = paths.git_revision(REPO_ROOT)
    out_dir = paths.log_dir(paths.LOGS_DIR, "ntop-sweep", rev)

    results = {}
    with tempfile.TemporaryDirectory(dir=str(REPO_ROOT / "tmp")) as td:
        if "shootout" in args.suites:
            print("shootout sweep (R=%d) ..." % args.shootout_iterations)
            results["shootout"] = measure_shootout(
                args.shootout_iterations, args.pin, args.timeout, td)
        if "appbench" in args.suites:
            print("appbench sweep (R=%d, lexex R=%d) ..." %
                  (args.appbench_iterations, args.lexex_iterations))
            results["appbench"] = measure_appbench(
                args.appbench_iterations, args.pin, args.timeout, td,
                args.lexex_iterations)

    json_path = out_dir / "ntop_sweep.json"
    json_path.write_text(json.dumps(results, indent=1), encoding="utf-8")
    print("wrote %s" % json_path)

    for suite in results:
        print("%s geomean vs NTOP=2:" % suite)
        for ntop, g in sorted(geomean_vs2(results[suite]).items()):
            print("  NTOP=%-2d %s" % (ntop, "%.3f" % g if g else "n/a"))

    plot(results, out_dir / "ntop_sweep.pdf")
    return 0


if __name__ == "__main__":
    sys.exit(main())
