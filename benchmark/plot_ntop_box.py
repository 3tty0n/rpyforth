#!/usr/bin/env python3
"""Box plots of the NTOP sweep, one PDF page: top row absolute warm-median
times (log scale), bottom row normalized to NTOP=2 (=1.0); columns are the
suites. Individual benchmarks are overlaid as points with min/max labeled.

Input: ntop_sweep.json produced by run_ntop_sweep.py (default: newest under
logs/ntop-sweep/). Output: ntop_sweep_box.pdf next to the input JSON.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from benchlib import paths, plotting  # noqa: E402
from run_ntop_sweep import NTOPS  # noqa: E402

REPO_ROOT = paths.REPO_ROOT


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def values_for(rows, relative):
    """Per NTOP: list of (benchmark, value). Relative divides by NTOP=2."""
    out = {n: [] for n in NTOPS}
    for name, row in rows.items():
        base = row.get("2")
        if relative and not base:
            continue
        for n in NTOPS:
            cell = row.get(str(n))
            if cell:
                v = cell["warm_median_usec"]
                if relative:
                    v /= base["warm_median_usec"]
                out[n].append((name, v))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--json", type=Path, default=None)
    ap.add_argument("--output", type=Path, default=None)
    args = ap.parse_args()

    json_path = args.json
    if json_path is None:
        candidates = sorted(
            (paths.LOGS_DIR / "ntop-sweep").glob("*/ntop_sweep.json"),
            key=lambda p: p.stat().st_mtime)
        if not candidates:
            print("no ntop_sweep.json under logs/ntop-sweep/",
                  file=sys.stderr)
            return 1
        json_path = candidates[-1]
    results = load(json_path)
    out_pdf = args.output or json_path.parent / "ntop_sweep_box.pdf"

    plt = plotting.pyplot()

    suites = [s for s in ("shootout", "appbench") if s in results]
    fig, axes = plt.subplots(2, len(suites),
                             figsize=(6.2 * len(suites), 9.6),
                             constrained_layout=True)
    if len(suites) == 1:
        axes = axes.reshape(2, 1)

    for row_i, relative in enumerate((False, True)):
        for col_i, suite in enumerate(suites):
            ax = axes[row_i][col_i]
            per_ntop = values_for(results[suite], relative)
            data = [[v for _, v in per_ntop[n]] for n in NTOPS]
            ax.boxplot(data, tick_labels=["%d" % n for n in NTOPS],
                       showmeans=True, widths=0.55,
                       meanprops=dict(marker="D", markerfacecolor="black",
                                      markeredgecolor="black",
                                      markersize=4.5),
                       medianprops=dict(color="tab:red", linewidth=1.6))
            for i, n in enumerate(NTOPS):
                pts = per_ntop[n]
                xs = [i + 1] * len(pts)
                ax.plot(xs, [v for _, v in pts], "o", markersize=3.5,
                        color="tab:blue", alpha=0.55, zorder=3)
                lo = min(pts, key=lambda t: t[1])
                hi = max(pts, key=lambda t: t[1])
                for name, v in {lo, hi}:
                    ax.annotate(name, (i + 1, v),
                                textcoords="offset points",
                                xytext=(7, -3), fontsize=7, alpha=0.8)
            nbench = len(results[suite])
            ax.set_xlabel("NTOP (scalar tops)")
            if relative:
                ax.axhline(1.0, color="gray", linestyle="--",
                           linewidth=0.9, alpha=0.7)
                ax.set_ylabel("warm time relative to NTOP=2")
                ax.set_title("%s: relative to NTOP=2 (%d benchmarks)"
                             % (suite, nbench))
            else:
                plotting.apply_log_axis(ax, [v for col in data for v in col])
                ax.set_ylabel("warm median per iteration [µs, log]")
                ax.set_title("%s: absolute time (%d benchmarks)"
                             % (suite, nbench))
            ax.grid(axis="y", alpha=0.3, which="both")

    fig.suptitle("NTOP sweep: warm-median distributions -- absolute (top) "
                 "and relative to NTOP=2 (bottom); lower is better",
                 fontsize=12)
    fig.savefig(out_pdf)
    print("wrote %s" % out_pdf)
    return 0


if __name__ == "__main__":
    sys.exit(main())
