#!/usr/bin/env python3
"""Parse, analyze, and visualize PYPYLOG jit logs from run_shootout."""

from __future__ import annotations

import argparse
import json
import re
import statistics
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable, Dict, Iterable, List, Optional, Tuple

from benchlib import plotting

# A/B here are the two jitlog configurations under comparison, not engines, so
# they keep their own two-color palette.
CONFIG_A_COLOR = "#1f77b4"
CONFIG_B_COLOR = "#ff7f0e"

JITLOG_FILENAME = re.compile(
    r"^(?P<safe>.+)_(?P<config>A|B)(?:_i(?P<iteration>\d{3}))?\.jitlog$"
)
JIT_SUMMARY_BLOCK = re.compile(r"\{jit-summary(.*?)jit-summary\}", re.DOTALL)

METRIC_ROWS: Tuple[Tuple[str, str, Callable[[object], str]], ...] = (
    ("tracing_time_sec", "Tracing time (s)", lambda x: f"{x:.6f}"),
    ("backend_time_sec", "Backend time (s)", lambda x: f"{x:.6f}"),
    ("total_time_sec", "JIT total time (s)", lambda x: f"{x:.6f}"),
    ("tracing_count", "Tracing runs", str),
    ("backend_count", "Backend runs", str),
    ("loops", "Loops", str),
    ("bridges", "Bridges", str),
    ("ops", "Ops", str),
    ("recorded_ops", "Recorded ops", str),
    ("guards", "Guards", str),
    ("opt_ops", "Opt ops", str),
    ("opt_guards", "Opt guards", str),
    ("heapcached_ops", "Heapcached ops", str),
    ("calls", "Calls", str),
    ("abort_total", "Abort total", str),
)

# Subset used for overview heatmaps and summary charts.
OVERVIEW_METRICS: Tuple[Tuple[str, str], ...] = (
    ("tracing_time_sec", "Tracing (s)"),
    ("backend_time_sec", "Backend (s)"),
    ("total_time_sec", "Total (s)"),
    ("ops", "Ops"),
    ("loops", "Loops"),
    ("bridges", "Bridges"),
    ("guards", "Guards"),
    ("opt_ops", "Opt ops"),
)


@dataclass
class JitlogMetrics:
    """Selected metrics from a PYPYLOG jit-summary block."""

    tracing_count: Optional[int] = None
    tracing_time_sec: Optional[float] = None
    backend_count: Optional[int] = None
    backend_time_sec: Optional[float] = None
    total_time_sec: Optional[float] = None
    ops: Optional[int] = None
    heapcached_ops: Optional[int] = None
    recorded_ops: Optional[int] = None
    calls: Optional[int] = None
    guards: Optional[int] = None
    opt_ops: Optional[int] = None
    opt_guards: Optional[int] = None
    loops: Optional[int] = None
    bridges: Optional[int] = None
    abort_total: Optional[int] = None

    def as_dict(self) -> Dict[str, Optional[float]]:
        return {
            "tracing_count": self.tracing_count,
            "tracing_time_sec": self.tracing_time_sec,
            "backend_count": self.backend_count,
            "backend_time_sec": self.backend_time_sec,
            "total_time_sec": self.total_time_sec,
            "ops": self.ops,
            "heapcached_ops": self.heapcached_ops,
            "recorded_ops": self.recorded_ops,
            "calls": self.calls,
            "guards": self.guards,
            "opt_ops": self.opt_ops,
            "opt_guards": self.opt_guards,
            "loops": self.loops,
            "bridges": self.bridges,
            "abort_total": self.abort_total,
        }


@dataclass
class JitlogRecord:
    path: Path
    benchmark: str
    config: str
    iteration: int
    metrics: JitlogMetrics


def is_curve_benchmark(name: str) -> bool:
    """Return True for curve/ warmup benchmarks."""
    normalized = name
    if normalized.startswith("shootout/"):
        normalized = normalized[len("shootout/") :]
    return normalized.startswith("curve/")


def filter_stable_jitlog_records(
    records: List[JitlogRecord], *, exclude_curve: bool = True
) -> List[JitlogRecord]:
    if not exclude_curve:
        return records
    return [record for record in records if not is_curve_benchmark(record.benchmark)]


def filter_stable_jitlog_grouped(
    grouped: Dict[str, Dict[str, JitlogMetrics]], *, exclude_curve: bool = True
) -> Dict[str, Dict[str, JitlogMetrics]]:
    if not exclude_curve:
        return grouped
    return {
        benchmark: by_config
        for benchmark, by_config in grouped.items()
        if not is_curve_benchmark(benchmark)
    }


def decode_benchmark_name(safe_name: str) -> str:
    """Restore a benchmark path from a run_shootout safe filename."""
    if "_curve_" in safe_name:
        return safe_name.replace("_curve_", "/curve/", 1)
    if safe_name.startswith("shootout_"):
        return safe_name.replace("shootout_", "shootout/", 1)
    return safe_name.replace("_", "/")


def parse_jitlog_filename(path: Path) -> Optional[Tuple[str, str, int]]:
    match = JITLOG_FILENAME.match(path.name)
    if not match:
        return None
    iteration = int(match.group("iteration") or "1")
    return decode_benchmark_name(match.group("safe")), match.group("config"), iteration


def parse_jit_summary_text(text: str) -> JitlogMetrics:
    """Parse metrics from jit-summary text."""
    metrics = JitlogMetrics()

    tracing_match = re.search(r"Tracing:\s*(\d+)\s+(\d+\.\d+)", text)
    if tracing_match:
        metrics.tracing_count = int(tracing_match.group(1))
        metrics.tracing_time_sec = float(tracing_match.group(2))

    backend_match = re.search(r"Backend:\s*(\d+)\s+(\d+\.\d+)", text)
    if backend_match:
        metrics.backend_count = int(backend_match.group(1))
        metrics.backend_time_sec = float(backend_match.group(2))

    total_match = re.search(r"TOTAL:\s*(\d+\.\d+)", text)
    if total_match:
        metrics.total_time_sec = float(total_match.group(1))

    for attr, label in [
        ("ops", "ops:"),
        ("heapcached_ops", "heapcached ops:"),
        ("recorded_ops", "recorded ops:"),
        ("calls", "calls:"),
        ("guards", "guards:"),
        ("opt_ops", "opt ops:"),
        ("opt_guards", "opt guards:"),
        ("loops", "Total # of loops:"),
        ("bridges", "Total # of bridges:"),
    ]:
        match = re.search(re.escape(label) + r"\s*(\d+)", text)
        if match:
            setattr(metrics, attr, int(match.group(1)))

    abort_total = 0
    for match in re.finditer(r"^abort:[^:\n]*:\s*(\d+)", text, re.MULTILINE):
        abort_total += int(match.group(1))
    if abort_total:
        metrics.abort_total = abort_total

    return metrics


def parse_jit_summary(path: Path) -> Optional[JitlogMetrics]:
    """Parse a jitlog file, using the last jit-summary block if several exist."""
    if not path.exists():
        return None
    text = path.read_text(encoding="utf-8", errors="replace")
    blocks = JIT_SUMMARY_BLOCK.findall(text)
    if blocks:
        return parse_jit_summary_text(blocks[-1])
    if "Tracing:" in text or "TOTAL:" in text:
        return parse_jit_summary_text(text)
    return None


def load_jitlog_record(path: Path) -> Optional[JitlogRecord]:
    parsed = parse_jitlog_filename(path)
    metrics = parse_jit_summary(path)
    if parsed is None or metrics is None:
        return None
    benchmark, config, iteration = parsed
    return JitlogRecord(path, benchmark, config, iteration, metrics)


def discover_jitlog_files(
    roots: Iterable[Path], only: Optional[str] = None
) -> List[Path]:
    """Collect jitlog files from paths (files or directories)."""
    files: List[Path] = []
    for root in roots:
        if root.is_file():
            files.append(root)
            continue
        if root.is_dir():
            files.extend(sorted(root.rglob("*.jitlog")))
    if only:
        files = [path for path in files if only in str(path)]
    return sorted(set(files))


def discover_jitlogs_from_benchmark_logs(log_dir: Path) -> List[Path]:
    """Find jitlog paths referenced in run_shootout benchmark logs."""
    paths: List[Path] = []
    for log_path in sorted(log_dir.glob("*.log")):
        for line in log_path.read_text(encoding="utf-8", errors="replace").splitlines():
            if line.startswith("# jitlog:"):
                paths.append(Path(line.split(":", 1)[1].strip()))
    return sorted(set(paths))


def load_records(paths: Iterable[Path]) -> List[JitlogRecord]:
    records: List[JitlogRecord] = []
    for path in paths:
        record = load_jitlog_record(path)
        if record is not None:
            records.append(record)
    return records


def aggregate_records(
    records: List[JitlogRecord], *, exclude_curve: bool = True
) -> Dict[str, Dict[str, JitlogMetrics]]:
    """Median numeric metrics across iterations, grouped by benchmark and config."""
    records = filter_stable_jitlog_records(records, exclude_curve=exclude_curve)
    grouped: Dict[str, Dict[str, List[JitlogMetrics]]] = {}
    for record in records:
        grouped.setdefault(record.benchmark, {}).setdefault(record.config, []).append(
            record.metrics
        )

    aggregated: Dict[str, Dict[str, JitlogMetrics]] = {}
    for benchmark, by_config in grouped.items():
        aggregated[benchmark] = {}
        for config, metric_list in by_config.items():
            aggregated[benchmark][config] = _median_metrics(metric_list)
    return aggregated


def _median_metrics(metric_list: List[JitlogMetrics]) -> JitlogMetrics:
    if len(metric_list) == 1:
        return metric_list[0]

    merged = JitlogMetrics()
    for attr, _, _ in METRIC_ROWS:
        values = [getattr(metrics, attr) for metrics in metric_list]
        numbers = [value for value in values if isinstance(value, (int, float))]
        if not numbers:
            continue
        median = statistics.median(numbers)
        if isinstance(numbers[0], int) and float(median).is_integer():
            setattr(merged, attr, int(median))
        else:
            setattr(merged, attr, float(median))
    return merged


def records_from_benchmark_results(
    results: Iterable[object], *, exclude_curve: bool = True
) -> List[JitlogRecord]:
    """Build jitlog records from run_shootout benchmark results."""
    records: List[JitlogRecord] = []
    for result in results:
        if exclude_curve and is_curve_benchmark(result.name):
            continue
        jitlog_path = getattr(result, "jitlog_path", None)
        if not jitlog_path:
            continue
        path = Path(jitlog_path)
        metrics = parse_jit_summary(path)
        if metrics is None:
            continue
        iteration = getattr(result, "iteration", 1)
        records.append(
            JitlogRecord(
                path=path,
                benchmark=result.name,
                config=result.config,
                iteration=iteration,
                metrics=metrics,
            )
        )
    return records


def aggregate_benchmark_jitlogs(
    results: Iterable[object], *, exclude_curve: bool = True
) -> Dict[str, Dict[str, JitlogMetrics]]:
    """Median JIT metrics from benchmark results that reference jitlog files."""
    grouped: Dict[str, Dict[str, List[JitlogMetrics]]] = {}
    for result in results:
        if exclude_curve and is_curve_benchmark(result.name):
            continue
        jitlog_path = getattr(result, "jitlog_path", None)
        if not jitlog_path:
            continue
        metrics = parse_jit_summary(Path(jitlog_path))
        if metrics is None:
            continue
        grouped.setdefault(result.name, {}).setdefault(result.config, []).append(metrics)

    return {
        benchmark: {config: _median_metrics(metric_list) for config, metric_list in by_config.items()}
        for benchmark, by_config in grouped.items()
    }


def format_jitlog_summary(records: List[JitlogRecord]) -> str:
    lines = ["=" * 100, "JIT Summary", "=" * 100]
    header = (
        f"{'Benchmark':<25} {'Config':<8} {'Iter':>4} "
        f"{'Trace(s)':>10} {'Backend(s)':>11} {'Total(s)':>10} "
        f"{'Ops':>8} {'Loops':>6} {'Bridges':>8}"
    )
    lines.append(header)
    lines.append("-" * 100)
    for record in sorted(records, key=lambda r: (r.benchmark, r.config, r.iteration)):
        metrics = record.metrics
        lines.append(
            f"{record.benchmark:<25} {record.config:<8} {record.iteration:>4} "
            f"{_fmt(metrics.tracing_time_sec, 10)} "
            f"{_fmt(metrics.backend_time_sec, 11)} "
            f"{_fmt(metrics.total_time_sec, 10)} "
            f"{_fmt(metrics.ops, 8, as_int=True)} "
            f"{_fmt(metrics.loops, 6, as_int=True)} "
            f"{_fmt(metrics.bridges, 8, as_int=True)}"
        )
    lines.append("=" * 100)
    return "\n".join(lines)


def format_jitlog_comparison(
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    label_a: str = "A",
    label_b: str = "B",
    *,
    title: str = "JIT Comparison",
    note_excludes_curve: bool = True,
) -> str:
    lines = ["=" * 110, title, "=" * 110]
    header = (
        f"{'Benchmark':<25} {'Metric':<20} "
        f"{label_a[:18]:>18} {label_b[:18]:>18} {'B/A':>10} {'Diff':>12}"
    )
    lines.append(header)
    lines.append("-" * 110)

    for benchmark in sorted(grouped):
        configs = grouped[benchmark]
        metrics_a = configs.get("A")
        metrics_b = configs.get("B")
        if metrics_a is None or metrics_b is None:
            continue

        first = True
        for attr, label, fmt in METRIC_ROWS:
            a_val = getattr(metrics_a, attr) if metrics_a else None
            b_val = getattr(metrics_b, attr) if metrics_b else None
            if a_val is None and b_val is None:
                continue

            a_str = fmt(a_val) if a_val is not None else "-"
            b_str = fmt(b_val) if b_val is not None else "-"
            if isinstance(a_val, (int, float)) and isinstance(b_val, (int, float)) and a_val != 0:
                ratio_str = f"{b_val / a_val:.2f}x"
                diff = b_val - a_val
                diff_str = f"{diff:+.2f}" if isinstance(diff, float) else f"{diff:+d}"
            else:
                ratio_str = "-"
                diff_str = "-"

            bench_col = benchmark if first else ""
            lines.append(
                f"{bench_col:<25} {label:<20} {a_str:>18} {b_str:>18} {ratio_str:>10} {diff_str:>12}"
            )
            first = False

    lines.append("=" * 110)
    lines.append("Interpretation: B/A > 1 means config B spent more JIT time or produced more artifacts.")
    if note_excludes_curve:
        lines.append("Note: curve/ warmup benchmarks are excluded from this comparison.")
    return "\n".join(lines)


def _paired_benchmarks(grouped: Dict[str, Dict[str, JitlogMetrics]]) -> List[str]:
    return sorted(
        name for name, configs in grouped.items() if "A" in configs and "B" in configs
    )


def _metric_ratio(
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    benchmark: str,
    attr: str,
) -> Optional[float]:
    metrics_a = grouped[benchmark].get("A")
    metrics_b = grouped[benchmark].get("B")
    if metrics_a is None or metrics_b is None:
        return None
    a_val = getattr(metrics_a, attr)
    b_val = getattr(metrics_b, attr)
    if not isinstance(a_val, (int, float)) or not isinstance(b_val, (int, float)):
        return None
    if a_val == 0:
        return None
    return float(b_val) / float(a_val)


def _short_benchmark_name(name: str) -> str:
    if name.startswith("shootout/"):
        return name[len("shootout/") :]
    return name


def _plot_jitlog_heatmap(
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    label_a: str,
    label_b: str,
):
    plt = plotting.pyplot()
    import numpy as np

    benchmarks = [_short_benchmark_name(name) for name in _paired_benchmarks(grouped)]
    if not benchmarks:
        return None

    full_names = _paired_benchmarks(grouped)
    metric_attrs = [attr for attr, _ in OVERVIEW_METRICS]
    metric_labels = [label for _, label in OVERVIEW_METRICS]
    matrix = []
    for full_name in full_names:
        row = []
        for attr in metric_attrs:
            ratio = _metric_ratio(grouped, full_name, attr)
            row.append(ratio if ratio is not None else float("nan"))
        matrix.append(row)

    data = np.array(matrix, dtype=float)
    fig, ax = plt.subplots(figsize=(max(8, len(metric_labels) * 1.2), max(4, len(benchmarks) * 0.45)))
    im = ax.imshow(data, aspect="auto", cmap="RdYlGn_r", vmin=0.5, vmax=2.0)
    ax.set_xticks(range(len(metric_labels)))
    ax.set_xticklabels(metric_labels, rotation=35, ha="right")
    ax.set_yticks(range(len(benchmarks)))
    ax.set_yticklabels(benchmarks)
    ax.set_title(f"JIT metric ratios ({label_b} / {label_a})")
    cbar = fig.colorbar(im, ax=ax, fraction=0.03, pad=0.02)
    cbar.set_label("B/A ratio")

    for row_idx, row in enumerate(data):
        for col_idx, value in enumerate(row):
            if np.isnan(value):
                text = "-"
            else:
                text = f"{value:.2f}x"
            ax.text(col_idx, row_idx, text, ha="center", va="center", fontsize=8, color="black")

    plt.tight_layout()
    return fig


def _plot_jitlog_total_time(grouped: Dict[str, Dict[str, JitlogMetrics]], label_a: str, label_b: str):
    plt = plotting.pyplot()

    names: List[str] = []
    a_vals: List[float] = []
    b_vals: List[float] = []
    for full_name in _paired_benchmarks(grouped):
        metrics_a = grouped[full_name]["A"]
        metrics_b = grouped[full_name]["B"]
        if metrics_a.total_time_sec is None or metrics_b.total_time_sec is None:
            continue
        names.append(_short_benchmark_name(full_name))
        a_vals.append(metrics_a.total_time_sec)
        b_vals.append(metrics_b.total_time_sec)
    if not names:
        return None

    x = range(len(names))
    width = 0.35
    fig, ax = plt.subplots(figsize=(10, max(4, len(names) * 0.45)))
    ax.barh([i - width / 2 for i in x], a_vals, width, label=label_a, color=CONFIG_A_COLOR)
    ax.barh([i + width / 2 for i in x], b_vals, width, label=label_b, color=CONFIG_B_COLOR)
    ax.set_yticks(list(x))
    ax.set_yticklabels(names)
    ax.set_xlabel("JIT total time (seconds)")
    ax.set_title("JIT total time by benchmark")
    ax.legend()
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_jitlog_speedup(grouped: Dict[str, Dict[str, JitlogMetrics]], label_a: str, label_b: str):
    plt = plotting.pyplot()

    names: List[str] = []
    speedups: List[float] = []
    for full_name in _paired_benchmarks(grouped):
        ratio = _metric_ratio(grouped, full_name, "total_time_sec")
        if ratio is None:
            continue
        names.append(_short_benchmark_name(full_name))
        speedups.append(ratio)

    if not names:
        return None

    fig, ax = plt.subplots(figsize=(10, max(4, len(names) * 0.45)))
    colors = ["#2ecc71" if value < 1 else "#e74c3c" for value in speedups]
    ax.barh(names, speedups, color=colors, alpha=0.85)
    ax.axvline(1.0, color="black", linestyle="--", linewidth=1)
    ax.set_xlabel(f"JIT total time ratio ({label_b} / {label_a})")
    ax.set_title(f"JIT overhead ratio (<1 means {label_b} spent less JIT time)")
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_jitlog_time_breakdown(
    grouped: Dict[str, Dict[str, JitlogMetrics]], label_a: str, label_b: str
):
    plt = plotting.pyplot()

    names: List[str] = []
    a_trace: List[float] = []
    a_backend: List[float] = []
    b_trace: List[float] = []
    b_backend: List[float] = []
    for full_name in _paired_benchmarks(grouped):
        metrics_a = grouped[full_name]["A"]
        metrics_b = grouped[full_name]["B"]
        if (
            metrics_a.tracing_time_sec is None
            or metrics_a.backend_time_sec is None
            or metrics_b.tracing_time_sec is None
            or metrics_b.backend_time_sec is None
        ):
            continue
        names.append(_short_benchmark_name(full_name))
        a_trace.append(metrics_a.tracing_time_sec)
        a_backend.append(metrics_a.backend_time_sec)
        b_trace.append(metrics_b.tracing_time_sec)
        b_backend.append(metrics_b.backend_time_sec)
    if not names:
        return None

    x = range(len(names))
    width = 0.35
    fig, ax = plt.subplots(figsize=(10, max(4, len(names) * 0.55)))
    ax.barh(
        [i - width / 2 for i in x],
        a_trace,
        width,
        label=f"{label_a} tracing",
        color="#6baed6",
    )
    ax.barh(
        [i - width / 2 for i in x],
        a_backend,
        width,
        left=a_trace,
        label=f"{label_a} backend",
        color="#2171b5",
    )
    ax.barh(
        [i + width / 2 for i in x],
        b_trace,
        width,
        label=f"{label_b} tracing",
        color="#fdae6b",
    )
    ax.barh(
        [i + width / 2 for i in x],
        b_backend,
        width,
        left=b_trace,
        label=f"{label_b} backend",
        color="#e6550d",
    )
    ax.set_yticks(list(x))
    ax.set_yticklabels(names)
    ax.set_xlabel("Time (seconds)")
    ax.set_title("Tracing vs backend time")
    ax.legend(fontsize=8, loc="lower right")
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_jitlog_artifacts(grouped: Dict[str, Dict[str, JitlogMetrics]], label_a: str, label_b: str):
    plt = plotting.pyplot()

    names: List[str] = []
    a_loops: List[float] = []
    b_loops: List[float] = []
    a_bridges: List[float] = []
    b_bridges: List[float] = []
    a_guards: List[float] = []
    b_guards: List[float] = []
    for full_name in _paired_benchmarks(grouped):
        metrics_a = grouped[full_name]["A"]
        metrics_b = grouped[full_name]["B"]
        if (
            metrics_a.loops is None
            or metrics_b.loops is None
            or metrics_a.bridges is None
            or metrics_b.bridges is None
            or metrics_a.guards is None
            or metrics_b.guards is None
        ):
            continue
        names.append(_short_benchmark_name(full_name))
        a_loops.append(float(metrics_a.loops))
        b_loops.append(float(metrics_b.loops))
        a_bridges.append(float(metrics_a.bridges))
        b_bridges.append(float(metrics_b.bridges))
        a_guards.append(float(metrics_a.guards))
        b_guards.append(float(metrics_b.guards))
    if not names:
        return None

    x = range(len(names))
    width = 0.12
    offsets = [-1.5 * width, -0.5 * width, 0.5 * width, 1.5 * width]
    fig, ax = plt.subplots(figsize=(10, max(4, len(names) * 0.55)))
    ax.barh([i + offsets[0] for i in x], a_loops, width, label=f"{label_a} loops", color=CONFIG_A_COLOR)
    ax.barh([i + offsets[1] for i in x], b_loops, width, label=f"{label_b} loops", color=CONFIG_B_COLOR)
    ax.barh([i + offsets[2] for i in x], a_bridges, width, label=f"{label_a} bridges", color="#2ca02c")
    ax.barh([i + offsets[3] for i in x], b_bridges, width, label=f"{label_b} bridges", color="#d62728")
    ax.set_yticks(list(x))
    ax.set_yticklabels(names)
    ax.set_xlabel("Count")
    ax.set_title("JIT artifacts: loops and bridges")
    ax.legend(fontsize=8, ncol=2)
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_jitlog_iterations(records: List[JitlogRecord], label_a: str, label_b: str):
    plt = plotting.pyplot()

    by_benchmark: Dict[str, Dict[str, List[Tuple[int, float]]]] = {}
    for record in records:
        total = record.metrics.total_time_sec
        if total is None:
            continue
        by_benchmark.setdefault(record.benchmark, {}).setdefault(record.config, []).append(
            (record.iteration, total)
        )

    plots = [
        (benchmark, configs)
        for benchmark, configs in sorted(by_benchmark.items())
        if len(configs.get("A", [])) > 1 or len(configs.get("B", [])) > 1
    ]
    if not plots:
        return None

    cols = 2
    rows = (len(plots) + cols - 1) // cols
    fig, axes = plt.subplots(rows, cols, figsize=(12, max(4, rows * 3)))
    axes_list = axes.flatten() if hasattr(axes, "flatten") else [axes]

    colors = {"A": CONFIG_A_COLOR, "B": CONFIG_B_COLOR}
    labels = {"A": label_a, "B": label_b}
    for ax, (benchmark, configs) in zip(axes_list, plots):
        for config in ("A", "B"):
            points = sorted(configs.get(config, []))
            if len(points) < 2:
                continue
            xs = [point[0] for point in points]
            ys = [point[1] for point in points]
            ax.plot(xs, ys, marker="o", label=labels[config], color=colors[config])
        ax.set_title(_short_benchmark_name(benchmark))
        ax.set_xlabel("Iteration")
        ax.set_ylabel("JIT total time (s)")
        ax.grid(True, linestyle="--", alpha=0.5)
        ax.legend(fontsize=8)

    for ax in axes_list[len(plots) :]:
        ax.set_visible(False)

    fig.suptitle("JIT total time across iterations", fontsize=13)
    plt.tight_layout()
    return fig


def _save_figure(fig, target) -> None:
    plotting.save_figure(fig, target)


def _figure_to_base64_png(fig) -> str:
    import base64
    from io import BytesIO

    buffer = BytesIO()
    _save_figure(fig, buffer)
    return base64.b64encode(buffer.getvalue()).decode("ascii")


def append_jitlog_visualization_pages(
    pdf,
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    records: List[JitlogRecord],
    label_a: str = "A",
    label_b: str = "B",
    *,
    chart_title_prefix: str = "JIT Analysis",
    include_detail: bool = False,
) -> None:
    """Append overview JIT charts (and optional per-metric detail) to a PdfPages object."""
    if not _paired_benchmarks(grouped):
        raise RuntimeError("No paired jit-summary data to plot.")

    overview_plotters = [
        _plot_jitlog_heatmap,
        _plot_jitlog_total_time,
        _plot_jitlog_speedup,
        _plot_jitlog_time_breakdown,
        _plot_jitlog_artifacts,
    ]
    for plotter in overview_plotters:
        fig = plotter(grouped, label_a, label_b)
        if fig is not None:
            _save_figure(fig, pdf)

    iteration_fig = _plot_jitlog_iterations(records, label_a, label_b)
    if iteration_fig is not None:
        _save_figure(iteration_fig, pdf)

    if include_detail:
        append_jitlog_pdf_pages(
            pdf,
            grouped,
            label_a,
            label_b,
            chart_title_prefix=chart_title_prefix,
        )


def generate_jitlog_visualization(
    pdf_path: Path,
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    records: List[JitlogRecord],
    label_a: str = "A",
    label_b: str = "B",
    *,
    include_detail: bool = False,
) -> None:
    """Write an overview JIT visualization PDF."""
    from matplotlib.backends.backend_pdf import PdfPages

    plotting.pyplot()
    pdf_path.parent.mkdir(parents=True, exist_ok=True)
    if not grouped:
        raise RuntimeError("No jit-summary data to plot.")

    with PdfPages(str(pdf_path)) as pdf:
        append_jitlog_visualization_pages(
            pdf,
            grouped,
            records,
            label_a,
            label_b,
            include_detail=include_detail,
        )


def generate_jitlog_html(
    html_path: Path,
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    records: List[JitlogRecord],
    label_a: str = "A",
    label_b: str = "B",
    *,
    include_detail: bool = False,
) -> None:
    """Write a self-contained HTML report with embedded chart images."""
    if not grouped:
        raise RuntimeError("No jit-summary data to visualize.")

    sections: List[str] = []
    chart_specs = [
        ("Overview heatmap", _plot_jitlog_heatmap),
        ("JIT total time", _plot_jitlog_total_time),
        ("JIT overhead ratio", _plot_jitlog_speedup),
        ("Tracing vs backend", _plot_jitlog_time_breakdown),
        ("Loops and bridges", _plot_jitlog_artifacts),
        ("Iteration trends", _plot_jitlog_iterations),
    ]

    for title, plotter in chart_specs:
        if plotter is _plot_jitlog_iterations:
            fig = plotter(records, label_a, label_b)
        else:
            fig = plotter(grouped, label_a, label_b)
        if fig is None:
            continue
        encoded = _figure_to_base64_png(fig)
        sections.append(
            f"<section><h2>{title}</h2>"
            f'<img alt="{title}" src="data:image/png;base64,{encoded}"></section>'
        )

    if include_detail and _paired_benchmarks(grouped):
        detail_fig = _plot_jitlog_detail_metrics(grouped, label_a, label_b)
        if detail_fig is not None:
            encoded = _figure_to_base64_png(detail_fig)
            sections.append(
                f"<section><h2>Additional metrics</h2>"
                f'<img alt="Additional metrics" src="data:image/png;base64,{encoded}"></section>'
            )

    summary_html = format_jitlog_summary(records).replace("&", "&amp;").replace("<", "&lt;")
    comparison_html = ""
    if _paired_benchmarks(grouped):
        comparison_html = (
            "<section><h2>Comparison table</h2><pre>"
            + format_jitlog_comparison(grouped, label_a, label_b)
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            + "</pre></section>"
        )

    html_path.parent.mkdir(parents=True, exist_ok=True)
    html_path.write_text(
        "\n".join(
            [
                "<!DOCTYPE html>",
                "<html lang='en'>",
                "<head>",
                "<meta charset='utf-8'>",
                "<title>JIT log analysis</title>",
                "<style>",
                "body { font-family: sans-serif; margin: 2rem; color: #222; }",
                "h1, h2 { margin-top: 1.5rem; }",
                "section { margin-bottom: 2rem; }",
                "img { max-width: 100%; height: auto; border: 1px solid #ddd; }",
                "pre { background: #f7f7f7; padding: 1rem; overflow-x: auto; }",
                "</style>",
                "</head>",
                "<body>",
                "<h1>JIT log analysis</h1>",
                f"<p>Configs: <strong>{label_a}</strong> vs <strong>{label_b}</strong></p>",
                *sections,
                f"<section><h2>Summary table</h2><pre>{summary_html}</pre></section>",
                comparison_html,
                "</body>",
                "</html>",
            ]
        ),
        encoding="utf-8",
    )


def _plot_jitlog_detail_metrics(
    grouped: Dict[str, Dict[str, JitlogMetrics]], label_a: str, label_b: str
):
    plt = plotting.pyplot()

    detail_attrs = [
        ("ops", "Ops"),
        ("recorded_ops", "Recorded ops"),
        ("guards", "Guards"),
        ("opt_ops", "Opt ops"),
        ("opt_guards", "Opt guards"),
        ("heapcached_ops", "Heapcached ops"),
    ]
    names = [_short_benchmark_name(name) for name in _paired_benchmarks(grouped)]
    if not names:
        return None

    fig, axes = plt.subplots(2, 3, figsize=(14, max(6, len(names) * 0.25)))
    for ax, (attr, title) in zip(axes.flatten(), detail_attrs):
        a_vals = []
        b_vals = []
        plot_names = []
        for full_name in _paired_benchmarks(grouped):
            metrics_a = grouped[full_name]["A"]
            metrics_b = grouped[full_name]["B"]
            a_val = getattr(metrics_a, attr)
            b_val = getattr(metrics_b, attr)
            if a_val is None or b_val is None:
                continue
            plot_names.append(_short_benchmark_name(full_name))
            a_vals.append(float(a_val))
            b_vals.append(float(b_val))
        if not plot_names:
            ax.set_visible(False)
            continue
        x = range(len(plot_names))
        width = 0.35
        ax.barh([i - width / 2 for i in x], a_vals, width, label=label_a, color=CONFIG_A_COLOR)
        ax.barh([i + width / 2 for i in x], b_vals, width, label=label_b, color=CONFIG_B_COLOR)
        ax.set_yticks(list(x))
        ax.set_yticklabels(plot_names, fontsize=7)
        ax.set_title(title, fontsize=10)
        ax.grid(axis="x", linestyle="--", alpha=0.5)
    handles, labels = axes.flatten()[0].get_legend_handles_labels()
    if handles:
        fig.legend(handles, labels, loc="upper center", ncol=2)
    fig.suptitle("Additional JIT metrics", fontsize=13)
    plt.tight_layout()
    return fig


def append_jitlog_pdf_pages(
    pdf,
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    label_a: str = "A",
    label_b: str = "B",
    *,
    chart_title_prefix: str = "JIT Analysis",
) -> None:
    """Append one bar chart page per metric to an open matplotlib PdfPages object."""
    from matplotlib import pyplot as plt

    benchmarks = _paired_benchmarks(grouped)
    if not benchmarks:
        raise RuntimeError("No paired jit-summary data to plot.")

    for attr, title, _fmt in METRIC_ROWS:
        names: List[str] = []
        a_vals: List[float] = []
        b_vals: List[float] = []
        for benchmark in benchmarks:
            metrics_a = grouped[benchmark].get("A")
            metrics_b = grouped[benchmark].get("B")
            a_val = getattr(metrics_a, attr) if metrics_a else None
            b_val = getattr(metrics_b, attr) if metrics_b else None
            if a_val is None or b_val is None:
                continue
            names.append(benchmark)
            a_vals.append(float(a_val))
            b_vals.append(float(b_val))
        if not names:
            continue

        x = range(len(names))
        width = 0.35
        fig, ax = plt.subplots(figsize=(10, max(6, len(names) * 0.4)))
        ax.barh([i - width / 2 for i in x], a_vals, width, label=label_a)
        ax.barh([i + width / 2 for i in x], b_vals, width, label=label_b)
        ax.set_yticks(list(x))
        ax.set_yticklabels(names)
        ax.set_xlabel(title)
        ax.set_title(f"{chart_title_prefix}: {title}")
        ax.legend()
        ax.grid(axis="x", linestyle="--", alpha=0.5)
        plt.tight_layout()
        pdf.savefig(fig)
        plt.close(fig)


def generate_jitlog_pdf(
    pdf_path: Path,
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    label_a: str = "A",
    label_b: str = "B",
    *,
    records: Optional[List[JitlogRecord]] = None,
    include_detail: bool = False,
) -> None:
    """Write a JIT visualization PDF (overview charts by default)."""
    if records is None:
        records = []
    generate_jitlog_visualization(
        pdf_path,
        grouped,
        records,
        label_a,
        label_b,
        include_detail=include_detail,
    )


VIRTUALIZABLE_FIELDS: Tuple[str, ...] = (
    "ds_ints",
    "ds_floats",
    "ds_locals",
    "ds_ptr_ints",
    "ds_ptr_floats",
    "ds_ptr_locals",
    "rs",
    "rs_ptr",
    "cs_threads",
    "cs_ips",
    "cs_ptr",
)

FIELD_OP_RE = re.compile(
    r"(getfield_gc_[ri]|setfield_gc)\([^)]*InnerInterpreter\.inst_(\w+)"
)
ALL_FIELD_OP_RE = re.compile(r"(getfield_gc_[ri]|setfield_gc)\(")
JIT_OP_LINE_RE = re.compile(r"^[+-]\d+:\s*(?:\w+\s*=\s*)?([a-zA-Z_][a-zA-Z0-9_]*)")
JIT_LOG_OPT_MARKER = "{jit-log-opt-"

OP_CATEGORIES: Tuple[Tuple[str, Tuple[str, ...]], ...] = (
    ("vable_getfield", ("getfield_gc_r", "getfield_gc_i")),
    ("vable_setfield", ("setfield_gc",)),
    ("array_read", ("getarrayitem_gc_i", "getarrayitem_gc_r", "getarrayitem_gc_f", "arraylen_gc")),
    ("array_write", ("setarrayitem_gc", "setarrayitem_gc_r", "setarrayitem_gc_f")),
    ("guard", ("guard_true", "guard_false", "guard_value", "guard_class", "guard_nonnull_class",
               "guard_not_invalidated", "guard_no_exception", "guard_is_object", "guard_isnull")),
    ("integer", ("int_add", "int_sub", "int_mul", "int_floordiv", "int_mod", "int_and", "int_or",
                 "int_xor", "int_lshift", "int_rshift", "int_eq", "int_ne", "int_lt", "int_le",
                 "int_gt", "int_ge", "int_is_true", "int_is_zero", "int_invert", "int_force_ge_zero")),
    ("control", ("jump", "label", "finish", "exit", "switch")),
    ("call", ("call", "call_may_force", "call_loopinvariant", "call_release_gil", "cast_ptr_to_int")),
    ("meta", ("debug_merge_point",)),
)

_OP_TO_CATEGORY: Dict[str, str] = {}
for category, ops in OP_CATEGORIES:
    for op in ops:
        _OP_TO_CATEGORY[op] = category


@dataclass
class VirtualizableFieldStats:
    """Counts of getfield/setfield on InnerInterpreter virtualizable fields."""

    getfield: int = 0
    setfield: int = 0
    opt_blocks: int = 0
    by_field: Dict[str, Dict[str, int]] = field(default_factory=dict)

    @property
    def total(self) -> int:
        return self.getfield + self.setfield

    def as_dict(self) -> Dict[str, object]:
        return {
            "getfield": self.getfield,
            "setfield": self.setfield,
            "total": self.total,
            "opt_blocks": self.opt_blocks,
            "by_field": self.by_field,
        }


@dataclass
class OptTraceStats:
    """Parsed operation counts from a jit-log-opt trace."""

    opt_blocks: int = 0
    total_ops: int = 0
    by_op: Dict[str, int] = field(default_factory=dict)
    by_category: Dict[str, int] = field(default_factory=dict)
    all_getfield: int = 0
    all_setfield: int = 0
    virtualizable: VirtualizableFieldStats = field(default_factory=VirtualizableFieldStats)

    def as_dict(self) -> Dict[str, object]:
        return {
            "opt_blocks": self.opt_blocks,
            "total_ops": self.total_ops,
            "by_op": self.by_op,
            "by_category": self.by_category,
            "all_getfield": self.all_getfield,
            "all_setfield": self.all_setfield,
            "virtualizable": self.virtualizable.as_dict(),
        }


@dataclass
class OptTraceRecord:
    path: Path
    benchmark: str
    config: str
    iteration: int
    stats: OptTraceStats


# Backward-compatible aliases.
FieldOpRecord = OptTraceRecord


def has_jit_log_opt(path: Path) -> bool:
    if not path.exists():
        return False
    text = path.read_text(encoding="utf-8", errors="replace")
    return JIT_LOG_OPT_MARKER in text


def _categorize_jit_op(op_name: str, line: str) -> str:
    if "InnerInterpreter.inst_" in line:
        if op_name.startswith("getfield"):
            return "vable_getfield"
        if op_name.startswith("setfield"):
            return "vable_setfield"
    if op_name in _OP_TO_CATEGORY:
        return _OP_TO_CATEGORY[op_name]
    if op_name.startswith("getfield"):
        return "getfield"
    if op_name.startswith("setfield"):
        return "setfield"
    if op_name.startswith("getarrayitem") or op_name.startswith("arraylen"):
        return "array_read"
    if op_name.startswith("setarrayitem"):
        return "array_write"
    if op_name.startswith("guard"):
        return "guard"
    if op_name.startswith("int_"):
        return "integer"
    if op_name.startswith("float_"):
        return "float"
    if op_name in {"jump", "label", "finish", "exit", "switch"}:
        return "control"
    if op_name.startswith("call") or op_name.startswith("cast_"):
        return "call"
    if op_name == "debug_merge_point":
        return "meta"
    return "other"


def parse_opt_trace_stats(text: str) -> Optional[OptTraceStats]:
    """Parse operation counts from a jit-log-opt trace."""
    if JIT_LOG_OPT_MARKER not in text:
        return None

    stats = OptTraceStats(opt_blocks=text.count(JIT_LOG_OPT_MARKER))
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("[") or stripped.startswith("#") or stripped.startswith("{"):
            continue
        if "--end of the loop--" in stripped:
            stats.by_op["--end--"] = stats.by_op.get("--end--", 0) + 1
            stats.by_category["meta"] = stats.by_category.get("meta", 0) + 1
            stats.total_ops += 1
            continue

        match = JIT_OP_LINE_RE.match(stripped)
        if not match:
            if stripped.startswith("debug_merge_point"):
                op_name = "debug_merge_point"
            elif stripped.startswith(("jump(", "label(", "finish(", "exit(")):
                op_name = stripped.split("(", 1)[0]
            else:
                continue
        else:
            op_name = match.group(1)

        stats.by_op[op_name] = stats.by_op.get(op_name, 0) + 1
        category = _categorize_jit_op(op_name, stripped)
        stats.by_category[category] = stats.by_category.get(category, 0) + 1
        stats.total_ops += 1

        if op_name.startswith("getfield"):
            stats.all_getfield += 1
        elif op_name.startswith("setfield"):
            stats.all_setfield += 1

        field_match = FIELD_OP_RE.search(stripped)
        if field_match:
            vop = "getfield" if field_match.group(1).startswith("getfield") else "setfield"
            field = field_match.group(2)
            vstats = stats.virtualizable
            if vop == "getfield":
                vstats.getfield += 1
            else:
                vstats.setfield += 1
            field_counts = vstats.by_field.setdefault(field, {"getfield": 0, "setfield": 0})
            field_counts[vop] += 1

    stats.virtualizable.opt_blocks = stats.opt_blocks
    return stats


def parse_virtualizable_field_ops(text: str) -> Optional[VirtualizableFieldStats]:
    """Parse getfield/setfield counts from a jit-log-opt trace."""
    stats = parse_opt_trace_stats(text)
    if stats is None:
        return None
    return stats.virtualizable


def load_opt_trace_record(path: Path) -> Optional[OptTraceRecord]:
    parsed = parse_jitlog_filename(path)
    stats = parse_opt_trace_stats(path.read_text(encoding="utf-8", errors="replace"))
    if parsed is None or stats is None:
        return None
    benchmark, config, iteration = parsed
    return OptTraceRecord(path, benchmark, config, iteration, stats)


def load_field_op_record(path: Path) -> Optional[FieldOpRecord]:
    return load_opt_trace_record(path)


def load_opt_trace_records(paths: Iterable[Path]) -> List[OptTraceRecord]:
    records: List[OptTraceRecord] = []
    for path in paths:
        record = load_opt_trace_record(path)
        if record is not None:
            records.append(record)
    return records


def load_field_op_records(paths: Iterable[Path]) -> List[FieldOpRecord]:
    return load_opt_trace_records(paths)


def filter_stable_opt_trace_records(
    records: List[OptTraceRecord], *, exclude_curve: bool = True
) -> List[OptTraceRecord]:
    if not exclude_curve:
        return records
    return [record for record in records if not is_curve_benchmark(record.benchmark)]


def filter_stable_field_op_records(
    records: List[FieldOpRecord], *, exclude_curve: bool = True
) -> List[FieldOpRecord]:
    return filter_stable_opt_trace_records(records, exclude_curve=exclude_curve)


def _median_int_values(values: List[int]) -> int:
    if not values:
        return 0
    median = statistics.median(values)
    return int(median) if float(median).is_integer() else int(round(median))


def _median_counter_dict(dicts: List[Dict[str, int]]) -> Dict[str, int]:
    keys = sorted({key for counter in dicts for key in counter})
    merged: Dict[str, int] = {}
    for key in keys:
        value = _median_int_values([counter.get(key, 0) for counter in dicts])
        if value:
            merged[key] = value
    return merged


def _median_field_stats(stats_list: List[VirtualizableFieldStats]) -> VirtualizableFieldStats:
    if len(stats_list) == 1:
        return stats_list[0]

    merged = VirtualizableFieldStats()
    merged.getfield = _median_int_values([stats.getfield for stats in stats_list])
    merged.setfield = _median_int_values([stats.setfield for stats in stats_list])
    merged.opt_blocks = _median_int_values([stats.opt_blocks for stats in stats_list])

    for field in VIRTUALIZABLE_FIELDS:
        get_values = [stats.by_field.get(field, {}).get("getfield", 0) for stats in stats_list]
        set_values = [stats.by_field.get(field, {}).get("setfield", 0) for stats in stats_list]
        get_median = _median_int_values(get_values)
        set_median = _median_int_values(set_values)
        if get_median or set_median:
            merged.by_field[field] = {"getfield": get_median, "setfield": set_median}
    return merged


def _median_opt_trace_stats(stats_list: List[OptTraceStats]) -> OptTraceStats:
    if len(stats_list) == 1:
        return stats_list[0]

    merged = OptTraceStats(
        opt_blocks=_median_int_values([stats.opt_blocks for stats in stats_list]),
        total_ops=_median_int_values([stats.total_ops for stats in stats_list]),
        by_op=_median_counter_dict([stats.by_op for stats in stats_list]),
        by_category=_median_counter_dict([stats.by_category for stats in stats_list]),
        all_getfield=_median_int_values([stats.all_getfield for stats in stats_list]),
        all_setfield=_median_int_values([stats.all_setfield for stats in stats_list]),
        virtualizable=_median_field_stats([stats.virtualizable for stats in stats_list]),
    )
    merged.virtualizable.opt_blocks = merged.opt_blocks
    return merged


def aggregate_opt_trace_records(
    records: List[OptTraceRecord], *, exclude_curve: bool = True
) -> Dict[str, Dict[str, OptTraceStats]]:
    """Median operation counts grouped by benchmark and config."""
    records = filter_stable_opt_trace_records(records, exclude_curve=exclude_curve)
    grouped: Dict[str, Dict[str, List[OptTraceStats]]] = {}
    for record in records:
        grouped.setdefault(record.benchmark, {}).setdefault(record.config, []).append(
            record.stats
        )

    return {
        benchmark: {
            config: _median_opt_trace_stats(stats_list)
            for config, stats_list in by_config.items()
        }
        for benchmark, by_config in grouped.items()
    }


def aggregate_field_op_records(
    records: List[FieldOpRecord], *, exclude_curve: bool = True
) -> Dict[str, Dict[str, VirtualizableFieldStats]]:
    """Median virtualizable field-op counts grouped by benchmark and config."""
    grouped = aggregate_opt_trace_records(records, exclude_curve=exclude_curve)
    return {
        benchmark: {config: stats.virtualizable for config, stats in by_config.items()}
        for benchmark, by_config in grouped.items()
    }


def _paired_opt_benchmarks(
    grouped: Dict[str, Dict[str, OptTraceStats]],
) -> List[str]:
    return sorted(
        benchmark
        for benchmark, configs in grouped.items()
        if "A" in configs and "B" in configs
    )


def _op_delta(a_count: int, b_count: int) -> int:
    return b_count - a_count


def _sum_stats_by_config(
    grouped: Dict[str, Dict[str, OptTraceStats]], config: str
) -> OptTraceStats:
    totals = OptTraceStats()
    for configs in grouped.values():
        if config not in configs:
            continue
        stats = configs[config]
        totals.opt_blocks += stats.opt_blocks
        totals.total_ops += stats.total_ops
        totals.all_getfield += stats.all_getfield
        totals.all_setfield += stats.all_setfield
        totals.virtualizable.getfield += stats.virtualizable.getfield
        totals.virtualizable.setfield += stats.virtualizable.setfield
        for field, counts in stats.virtualizable.by_field.items():
            merged = totals.virtualizable.by_field.setdefault(
                field, {"getfield": 0, "setfield": 0}
            )
            merged["getfield"] += counts.get("getfield", 0)
            merged["setfield"] += counts.get("setfield", 0)
        for op, count in stats.by_op.items():
            totals.by_op[op] = totals.by_op.get(op, 0) + count
        for category, count in stats.by_category.items():
            totals.by_category[category] = totals.by_category.get(category, 0) + count
    return totals


def _format_op_delta_rows(
    stats_a: OptTraceStats,
    stats_b: OptTraceStats,
    *,
    top_n: int,
    min_delta: int = 1,
) -> Tuple[List[str], List[str]]:
    """Return (added_in_b_lines, reduced_in_b_lines) for significant op deltas."""
    all_ops = set(stats_a.by_op) | set(stats_b.by_op)
    deltas = [
        (op, stats_a.by_op.get(op, 0), stats_b.by_op.get(op, 0), _op_delta(stats_a.by_op.get(op, 0), stats_b.by_op.get(op, 0)))
        for op in all_ops
    ]
    deltas = [row for row in deltas if abs(row[3]) >= min_delta]
    added = sorted([row for row in deltas if row[3] > 0], key=lambda row: row[3], reverse=True)[:top_n]
    reduced = sorted([row for row in deltas if row[3] < 0], key=lambda row: row[3])[:top_n]

    def render_rows(rows: List[Tuple[str, int, int, int]]) -> List[str]:
        lines: List[str] = []
        for op, a_count, b_count, delta in rows:
            lines.append(f"  {op:<28} {a_count:8d} {b_count:8d} {delta:+8d}")
        return lines

    return render_rows(added), render_rows(reduced)


def format_opt_trace_analysis_report(
    grouped: Dict[str, Dict[str, OptTraceStats]],
    label_a: str = "A",
    label_b: str = "B",
    *,
    note_excludes_curve: bool = True,
    top_n: int = 20,
    per_benchmark_top_n: int = 8,
) -> str:
    """Detailed jit-log-opt operation analysis comparing configs A and B."""
    paired = _paired_opt_benchmarks(grouped)
    lines: List[str] = [
        "=" * 110,
        "JIT operation analysis (jit-log-opt traces)",
        "=" * 110,
    ]
    if not paired:
        lines.append("No paired A/B jit-log-opt traces found.")
        lines.append("=" * 110)
        return "\n".join(lines)

    total_a = _sum_stats_by_config(grouped, "A")
    total_b = _sum_stats_by_config(grouped, "B")
    lines.extend(
        [
            f"Overall optimized trace ops: {label_a}={total_a.total_ops}  "
            f"{label_b}={total_b.total_ops}  B-A={_op_delta(total_a.total_ops, total_b.total_ops):+d}",
            f"All getfield/setfield:       {label_a}={total_a.all_getfield}/{total_a.all_setfield}  "
            f"{label_b}={total_b.all_getfield}/{total_b.all_setfield}",
            "",
            "Category summary (summed across benchmarks)",
            f"{'Category':<18} {label_a:>10} {label_b:>10} {'B-A':>10}",
            "-" * 52,
        ]
    )
    categories = sorted(set(total_a.by_category) | set(total_b.by_category))
    for category in categories:
        a_count = total_a.by_category.get(category, 0)
        b_count = total_b.by_category.get(category, 0)
        lines.append(
            f"{category:<18} {a_count:10d} {b_count:10d} {_op_delta(a_count, b_count):+10d}"
        )

    lines.extend(["", "=" * 110, "Virtualizable field ops (by benchmark)", "=" * 110])
    lines.append(
        format_virtualizable_field_report(
            grouped_for_virtualizable(grouped),
            label_a,
            label_b,
            note_excludes_curve=False,
            include_header=False,
        )
    )
    lines.extend(["", "Virtualizable fields (totals across benchmarks)", "-" * 90])
    lines.append(
        f"{'Field':<16} {label_a + ' get':>10} {label_a + ' set':>10} "
        f"{label_b + ' get':>10} {label_b + ' set':>10} {'Removed':>10}"
    )
    for field in VIRTUALIZABLE_FIELDS:
        a_get = total_a.virtualizable.by_field.get(field, {}).get("getfield", 0)
        a_set = total_a.virtualizable.by_field.get(field, {}).get("setfield", 0)
        b_get = total_b.virtualizable.by_field.get(field, {}).get("getfield", 0)
        b_set = total_b.virtualizable.by_field.get(field, {}).get("setfield", 0)
        removed = (b_get + b_set) - (a_get + a_set)
        if not (a_get or a_set or b_get or b_set):
            continue
        lines.append(
            f"{field:<16} {a_get:10d} {a_set:10d} {b_get:10d} {b_set:10d} {removed:10d}"
        )

    lines.extend(
        [
            "",
            f"Top operations added in {label_b} vs {label_a} (positive B-A, TOTAL)",
            f"{'Operation':<28} {label_a:>8} {label_b:>8} {'B-A':>8}",
            "-" * 56,
        ]
    )
    added, reduced = _format_op_delta_rows(total_a, total_b, top_n=top_n)
    lines.extend(added or ["  (none)"])
    lines.extend(
        [
            "",
            f"Top operations reduced in {label_b} vs {label_a} (negative B-A, TOTAL)",
            f"{'Operation':<28} {label_a:>8} {label_b:>8} {'B-A':>8}",
            "-" * 56,
        ]
    )
    lines.extend(reduced or ["  (none)"])

    for benchmark in paired:
        stats_a = grouped[benchmark]["A"]
        stats_b = grouped[benchmark]["B"]
        bench_added, bench_reduced = _format_op_delta_rows(
            stats_a, stats_b, top_n=per_benchmark_top_n, min_delta=2
        )
        if not bench_added and not bench_reduced:
            continue
        lines.extend(["", f"--- {_short_benchmark_name(benchmark)} ---"])
        if bench_added:
            lines.append(f"Added in {label_b}:")
            lines.extend(bench_added)
        if bench_reduced:
            lines.append(f"Reduced in {label_b}:")
            lines.extend(bench_reduced)

    lines.extend(
        [
            "=" * 110,
            "Interpretation:",
            f"  B-A > 0: {label_b} emits more of that operation in optimized traces.",
            f"  B-A < 0: {label_a} emits more (often due to virtualization keeping state in registers).",
            "  vable_getfield/setfield: InnerInterpreter.inst_* virtualizable fields only.",
            "  getfield/setfield: non-virtualizable object field accesses.",
        ]
    )
    if note_excludes_curve:
        lines.append("Note: curve/ warmup benchmarks are excluded from this report.")
    return "\n".join(lines)


def grouped_for_virtualizable(
    grouped: Dict[str, Dict[str, OptTraceStats]],
) -> Dict[str, Dict[str, VirtualizableFieldStats]]:
    result: Dict[str, Dict[str, VirtualizableFieldStats]] = {}
    for benchmark, by_config in grouped.items():
        result[benchmark] = {config: stats.virtualizable for config, stats in by_config.items()}
        for config, stats in by_config.items():
            result[benchmark][config].opt_blocks = stats.opt_blocks
    return result


def format_virtualizable_field_report(
    grouped: Dict[str, Dict[str, VirtualizableFieldStats]],
    label_a: str = "A",
    label_b: str = "B",
    *,
    note_excludes_curve: bool = True,
    include_header: bool = True,
) -> str:
    """Format a comparison of virtualizable getfield/setfield ops in jit-log-opt traces."""
    lines: List[str] = []
    if include_header:
        lines.extend(
            [
                "=" * 110,
                "Virtualizable field ops (getfield/setfield in jit-log-opt traces)",
                "=" * 110,
            ]
        )
    lines.extend(
        [
        (
            f"{'Benchmark':<25} "
            f"{label_a + ' get':>10} "
            f"{label_a + ' set':>10} "
            f"{label_b + ' get':>10} "
            f"{label_b + ' set':>10} "
            f"{'Removed':>10} "
            f"{'Blocks':>10}"
        ),
        "-" * 110,
        ]
    )

    total_a_get = total_a_set = total_b_get = total_b_set = total_removed = 0
    paired = [
        benchmark
        for benchmark in sorted(grouped)
        if "A" in grouped[benchmark] and "B" in grouped[benchmark]
    ]
    if not paired:
        if include_header:
            lines.append("No paired A/B jit-log-opt traces with virtualizable field ops found.")
            lines.append("=" * 110)
        return "\n".join(lines)

    for benchmark in paired:
        stats_a = grouped[benchmark]["A"]
        stats_b = grouped[benchmark]["B"]
        removed = stats_b.total - stats_a.total
        total_a_get += stats_a.getfield
        total_a_set += stats_a.setfield
        total_b_get += stats_b.getfield
        total_b_set += stats_b.setfield
        total_removed += removed
        short_name = _short_benchmark_name(benchmark)
        blocks = f"{stats_a.opt_blocks}/{stats_b.opt_blocks}"
        lines.append(
            f"{short_name:<25} "
            f"{stats_a.getfield:10d} "
            f"{stats_a.setfield:10d} "
            f"{stats_b.getfield:10d} "
            f"{stats_b.setfield:10d} "
            f"{removed:10d} "
            f"{blocks:>10}"
        )

    lines.append("-" * 110)
    lines.append(
        f"{'TOTAL':<25} "
        f"{total_a_get:10d} "
        f"{total_a_set:10d} "
        f"{total_b_get:10d} "
        f"{total_b_set:10d} "
        f"{total_removed:10d}"
    )
    lines.append("=" * 110)
    if include_header:
        lines.append(
            "Interpretation: counts are getfield_gc_*/setfield_gc on InnerInterpreter.inst_* "
            "in optimized JIT traces."
        )
        lines.append(
            f"Removed = ({label_b} get + set) - ({label_a} get + set); "
            "virtualization hoists these fields into registers."
        )
        if note_excludes_curve:
            lines.append("Note: curve/ warmup benchmarks are excluded from this report.")
        lines.append(
            f"Tracked fields ({len(VIRTUALIZABLE_FIELDS)}): {', '.join(VIRTUALIZABLE_FIELDS)}."
        )
    return "\n".join(lines)


def _collect_op_deltas(
    stats_a: OptTraceStats,
    stats_b: OptTraceStats,
    *,
    top_n: Optional[int] = None,
    min_delta: int = 1,
) -> List[Tuple[str, int, int, int]]:
    all_ops = set(stats_a.by_op) | set(stats_b.by_op)
    deltas = [
        (
            op,
            stats_a.by_op.get(op, 0),
            stats_b.by_op.get(op, 0),
            _op_delta(stats_a.by_op.get(op, 0), stats_b.by_op.get(op, 0)),
        )
        for op in all_ops
    ]
    deltas = [row for row in deltas if abs(row[3]) >= min_delta]
    deltas.sort(key=lambda row: abs(row[3]), reverse=True)
    if top_n is not None:
        deltas = deltas[:top_n]
    return deltas


def _plot_opt_category_comparison(
    total_a: OptTraceStats,
    total_b: OptTraceStats,
    label_a: str,
    label_b: str,
):
    plt = plotting.pyplot()

    categories = sorted(set(total_a.by_category) | set(total_b.by_category))
    if not categories:
        return None

    a_vals = [total_a.by_category.get(cat, 0) for cat in categories]
    b_vals = [total_b.by_category.get(cat, 0) for cat in categories]
    y = range(len(categories))
    width = 0.35
    fig, ax = plt.subplots(figsize=(10, max(4, len(categories) * 0.45)))
    ax.barh([i - width / 2 for i in y], a_vals, width, label=label_a, color=CONFIG_A_COLOR)
    ax.barh([i + width / 2 for i in y], b_vals, width, label=label_b, color=CONFIG_B_COLOR)
    ax.set_yticks(list(y))
    ax.set_yticklabels(categories)
    ax.set_xlabel("Operation count in optimized traces")
    ax.set_title("JIT op categories: A vs B")
    ax.legend()
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_opt_category_deltas(
    total_a: OptTraceStats,
    total_b: OptTraceStats,
    label_a: str,
    label_b: str,
):
    plt = plotting.pyplot()

    categories = sorted(set(total_a.by_category) | set(total_b.by_category))
    deltas = [
        (cat, _op_delta(total_a.by_category.get(cat, 0), total_b.by_category.get(cat, 0)))
        for cat in categories
    ]
    deltas = [row for row in deltas if row[1] != 0]
    if not deltas:
        return None

    deltas.sort(key=lambda row: row[1], reverse=True)
    names = [row[0] for row in deltas]
    values = [row[1] for row in deltas]
    colors = ["#e74c3c" if value > 0 else "#2ecc71" for value in values]

    fig, ax = plt.subplots(figsize=(10, max(4, len(names) * 0.45)))
    ax.barh(names, values, color=colors, alpha=0.85)
    ax.axvline(0, color="black", linewidth=1)
    ax.set_xlabel(f"Operation delta ({label_b} - {label_a})")
    ax.set_title(f"JIT category deltas (>0 means more ops in {label_b})")
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_vable_removed_by_benchmark(
    grouped: Dict[str, Dict[str, OptTraceStats]],
    label_a: str,
    label_b: str,
):
    plt = plotting.pyplot()

    paired = _paired_opt_benchmarks(grouped)
    names: List[str] = []
    removed_vals: List[int] = []
    for benchmark in paired:
        stats_a = grouped[benchmark]["A"].virtualizable
        stats_b = grouped[benchmark]["B"].virtualizable
        removed = stats_b.total - stats_a.total
        if removed <= 0:
            continue
        names.append(_short_benchmark_name(benchmark))
        removed_vals.append(removed)
    if not names:
        return None

    fig, ax = plt.subplots(figsize=(10, max(4, len(names) * 0.45)))
    ax.barh(names, removed_vals, color="#3498db", alpha=0.85)
    ax.set_xlabel(f"Virtualizable field ops removed ({label_b} - {label_a})")
    ax.set_title("Virtualizable getfield/setfield removed by virtualization")
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_vable_field_deltas(
    total_a: OptTraceStats,
    total_b: OptTraceStats,
    label_a: str,
    label_b: str,
):
    plt = plotting.pyplot()

    names: List[str] = []
    deltas: List[int] = []
    for field in VIRTUALIZABLE_FIELDS:
        a_get = total_a.virtualizable.by_field.get(field, {}).get("getfield", 0)
        a_set = total_a.virtualizable.by_field.get(field, {}).get("setfield", 0)
        b_get = total_b.virtualizable.by_field.get(field, {}).get("getfield", 0)
        b_set = total_b.virtualizable.by_field.get(field, {}).get("setfield", 0)
        delta = (b_get + b_set) - (a_get + a_set)
        if delta == 0:
            continue
        names.append(field)
        deltas.append(delta)
    if not names:
        return None

    pairs = sorted(zip(names, deltas), key=lambda row: row[1], reverse=True)
    names = [row[0] for row in pairs]
    deltas = [row[1] for row in pairs]
    colors = ["#e74c3c" if value > 0 else "#2ecc71" for value in deltas]

    fig, ax = plt.subplots(figsize=(10, max(4, len(names) * 0.4)))
    ax.barh(names, deltas, color=colors, alpha=0.85)
    ax.axvline(0, color="black", linewidth=1)
    ax.set_xlabel(f"Field op delta ({label_b} - {label_a})")
    ax.set_title("Virtualizable field op deltas by field")
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_top_op_deltas(
    total_a: OptTraceStats,
    total_b: OptTraceStats,
    label_a: str,
    label_b: str,
    *,
    top_n: int = 20,
):
    plt = plotting.pyplot()

    deltas = _collect_op_deltas(total_a, total_b, top_n=top_n, min_delta=1)
    if not deltas:
        return None

    names = [row[0] for row in deltas]
    values = [row[3] for row in deltas]
    colors = ["#e74c3c" if value > 0 else "#2ecc71" for value in values]

    fig, ax = plt.subplots(figsize=(10, max(5, len(names) * 0.35)))
    ax.barh(names[::-1], values[::-1], color=colors[::-1], alpha=0.85)
    ax.axvline(0, color="black", linewidth=1)
    ax.set_xlabel(f"Operation delta ({label_b} - {label_a})")
    ax.set_title(f"Top {len(names)} JIT operation deltas")
    ax.grid(axis="x", linestyle="--", alpha=0.5)
    plt.tight_layout()
    return fig


def _plot_benchmark_top_op_deltas(
    grouped: Dict[str, Dict[str, OptTraceStats]],
    label_a: str,
    label_b: str,
    *,
    per_benchmark_top_n: int = 5,
):
    plt = plotting.pyplot()

    paired = _paired_opt_benchmarks(grouped)
    if not paired:
        return None

    cols = 2
    rows = (len(paired) + cols - 1) // cols
    fig, axes = plt.subplots(rows, cols, figsize=(14, max(4, rows * 3.2)))
    axes_list = axes.flatten() if hasattr(axes, "flatten") else [axes]

    for ax, benchmark in zip(axes_list, paired):
        stats_a = grouped[benchmark]["A"]
        stats_b = grouped[benchmark]["B"]
        deltas = _collect_op_deltas(stats_a, stats_b, top_n=per_benchmark_top_n, min_delta=2)
        if not deltas:
            ax.set_visible(False)
            continue
        names = [row[0] for row in deltas][::-1]
        values = [row[3] for row in deltas][::-1]
        colors = ["#e74c3c" if value > 0 else "#2ecc71" for value in values]
        ax.barh(names, values, color=colors, alpha=0.85)
        ax.axvline(0, color="black", linewidth=0.8)
        ax.set_title(_short_benchmark_name(benchmark), fontsize=10)
        ax.grid(axis="x", linestyle="--", alpha=0.4)

    for ax in axes_list[len(paired) :]:
        ax.set_visible(False)

    fig.suptitle(f"Per-benchmark op deltas ({label_b} - {label_a})", fontsize=13)
    plt.tight_layout()
    return fig


def append_opt_trace_visualization_pages(
    pdf,
    grouped: Dict[str, Dict[str, OptTraceStats]],
    label_a: str = "A",
    label_b: str = "B",
    *,
    top_n: int = 20,
    per_benchmark_top_n: int = 5,
) -> None:
    """Append JIT operation analysis charts to an open PdfPages object."""
    if not _paired_opt_benchmarks(grouped):
        raise RuntimeError("No paired jit-log-opt data to plot.")

    total_a = _sum_stats_by_config(grouped, "A")
    total_b = _sum_stats_by_config(grouped, "B")
    plotters = [
        lambda: _plot_opt_category_comparison(total_a, total_b, label_a, label_b),
        lambda: _plot_opt_category_deltas(total_a, total_b, label_a, label_b),
        lambda: _plot_vable_removed_by_benchmark(grouped, label_a, label_b),
        lambda: _plot_vable_field_deltas(total_a, total_b, label_a, label_b),
        lambda: _plot_top_op_deltas(total_a, total_b, label_a, label_b, top_n=top_n),
        lambda: _plot_benchmark_top_op_deltas(
            grouped, label_a, label_b, per_benchmark_top_n=per_benchmark_top_n
        ),
    ]
    for plotter in plotters:
        fig = plotter()
        if fig is not None:
            _save_figure(fig, pdf)


def generate_opt_trace_visualization(
    pdf_path: Path,
    grouped: Dict[str, Dict[str, OptTraceStats]],
    label_a: str = "A",
    label_b: str = "B",
    *,
    top_n: int = 20,
    per_benchmark_top_n: int = 5,
) -> None:
    """Write JIT operation analysis charts to a PDF."""
    from matplotlib.backends.backend_pdf import PdfPages

    plotting.pyplot()
    pdf_path.parent.mkdir(parents=True, exist_ok=True)
    if not grouped:
        raise RuntimeError("No jit-log-opt data to plot.")

    with PdfPages(str(pdf_path)) as pdf:
        append_opt_trace_visualization_pages(
            pdf,
            grouped,
            label_a,
            label_b,
            top_n=top_n,
            per_benchmark_top_n=per_benchmark_top_n,
        )


def _opt_trace_chart_specs(
    grouped: Dict[str, Dict[str, OptTraceStats]],
    label_a: str,
    label_b: str,
    *,
    top_n: int,
    per_benchmark_top_n: int,
) -> List[Tuple[str, Callable[[], object]]]:
    total_a = _sum_stats_by_config(grouped, "A")
    total_b = _sum_stats_by_config(grouped, "B")
    return [
        ("JIT op categories", lambda: _plot_opt_category_comparison(total_a, total_b, label_a, label_b)),
        ("Category deltas", lambda: _plot_opt_category_deltas(total_a, total_b, label_a, label_b)),
        ("Virtualizable ops removed by benchmark", lambda: _plot_vable_removed_by_benchmark(grouped, label_a, label_b)),
        ("Virtualizable field deltas", lambda: _plot_vable_field_deltas(total_a, total_b, label_a, label_b)),
        (f"Top {top_n} operation deltas", lambda: _plot_top_op_deltas(total_a, total_b, label_a, label_b, top_n=top_n)),
        (
            "Per-benchmark operation deltas",
            lambda: _plot_benchmark_top_op_deltas(
                grouped, label_a, label_b, per_benchmark_top_n=per_benchmark_top_n
            ),
        ),
    ]


def generate_opt_trace_html(
    html_path: Path,
    grouped: Dict[str, Dict[str, OptTraceStats]],
    label_a: str = "A",
    label_b: str = "B",
    *,
    top_n: int = 20,
    per_benchmark_top_n: int = 5,
    note_excludes_curve: bool = True,
) -> None:
    """Write a self-contained HTML report for JIT operation analysis."""
    if not grouped:
        raise RuntimeError("No jit-log-opt data to visualize.")

    sections: List[str] = []
    for title, plotter in _opt_trace_chart_specs(
        grouped, label_a, label_b, top_n=top_n, per_benchmark_top_n=per_benchmark_top_n
    ):
        fig = plotter()
        if fig is None:
            continue
        encoded = _figure_to_base64_png(fig)
        sections.append(
            f"<section><h2>{title}</h2>"
            f'<img alt="{title}" src="data:image/png;base64,{encoded}"></section>'
        )

    report_html = (
        format_opt_trace_analysis_report(
            grouped, label_a, label_b, note_excludes_curve=note_excludes_curve, top_n=top_n
        )
        .replace("&", "&amp;")
        .replace("<", "&lt;")
    )

    html_path.parent.mkdir(parents=True, exist_ok=True)
    html_path.write_text(
        "\n".join(
            [
                "<!DOCTYPE html>",
                "<html lang='en'>",
                "<head>",
                "<meta charset='utf-8'>",
                "<title>JIT operation analysis</title>",
                "<style>",
                "body { font-family: sans-serif; margin: 2rem; color: #222; }",
                "h1, h2 { margin-top: 1.5rem; }",
                "section { margin-bottom: 2rem; }",
                "img { max-width: 100%; height: auto; border: 1px solid #ddd; }",
                "pre { background: #f7f7f7; padding: 1rem; overflow-x: auto; }",
                "</style>",
                "</head>",
                "<body>",
                "<h1>JIT operation analysis</h1>",
                f"<p>Configs: <strong>{label_a}</strong> vs <strong>{label_b}</strong></p>",
                *sections,
                f"<section><h2>Text report</h2><pre>{report_html}</pre></section>",
                "</body>",
                "</html>",
            ]
        ),
        encoding="utf-8",
    )


def write_jitlog_analysis_pdf(
    pdf_path: Path,
    *,
    grouped: Optional[Dict[str, Dict[str, JitlogMetrics]]] = None,
    records: Optional[List[JitlogRecord]] = None,
    opt_trace_grouped: Optional[Dict[str, Dict[str, OptTraceStats]]] = None,
    label_a: str = "A",
    label_b: str = "B",
    include_detail: bool = False,
    op_top_n: int = 20,
    op_benchmark_top_n: int = 5,
) -> None:
    """Write summary and/or operation-analysis charts to one PDF."""
    from matplotlib.backends.backend_pdf import PdfPages

    plotting.pyplot()
    pdf_path.parent.mkdir(parents=True, exist_ok=True)
    has_summary = grouped and _paired_benchmarks(grouped)
    has_ops = opt_trace_grouped and _paired_opt_benchmarks(opt_trace_grouped)
    if not has_summary and not has_ops:
        raise RuntimeError("No paired jit-summary or jit-log-opt data to plot.")

    with PdfPages(str(pdf_path)) as pdf:
        if has_summary:
            append_jitlog_visualization_pages(
                pdf,
                grouped,
                records or [],
                label_a,
                label_b,
                include_detail=include_detail,
            )
        if has_ops:
            append_opt_trace_visualization_pages(
                pdf,
                opt_trace_grouped,
                label_a,
                label_b,
                top_n=op_top_n,
                per_benchmark_top_n=op_benchmark_top_n,
            )


def _html_document(title: str, sections: List[str], *, subtitle: str = "") -> str:
    header = f"<h1>{title}</h1>"
    if subtitle:
        header += f"<p>{subtitle}</p>"
    return "\n".join(
        [
            "<!DOCTYPE html>",
            "<html lang='en'>",
            "<head>",
            "<meta charset='utf-8'>",
            f"<title>{title}</title>",
            "<style>",
            "body { font-family: sans-serif; margin: 2rem; color: #222; }",
            "h1, h2 { margin-top: 1.5rem; }",
            "section { margin-bottom: 2rem; }",
            "img { max-width: 100%; height: auto; border: 1px solid #ddd; }",
            "pre { background: #f7f7f7; padding: 1rem; overflow-x: auto; }",
            "</style>",
            "</head>",
            "<body>",
            header,
            *sections,
            "</body>",
            "</html>",
        ]
    )


def _figures_to_html_sections(
    chart_specs: List[Tuple[str, Callable[[], object]]],
) -> List[str]:
    sections: List[str] = []
    for title, plotter in chart_specs:
        fig = plotter()
        if fig is None:
            continue
        encoded = _figure_to_base64_png(fig)
        sections.append(
            f"<section><h2>{title}</h2>"
            f'<img alt="{title}" src="data:image/png;base64,{encoded}"></section>'
        )
    return sections


def _jitlog_summary_chart_specs(
    grouped: Dict[str, Dict[str, JitlogMetrics]],
    records: List[JitlogRecord],
    label_a: str,
    label_b: str,
    *,
    include_detail: bool,
) -> List[Tuple[str, Callable[[], object]]]:
    specs: List[Tuple[str, Callable[[], object]]] = [
        ("Overview heatmap", lambda: _plot_jitlog_heatmap(grouped, label_a, label_b)),
        ("JIT total time", lambda: _plot_jitlog_total_time(grouped, label_a, label_b)),
        ("JIT overhead ratio", lambda: _plot_jitlog_speedup(grouped, label_a, label_b)),
        ("Tracing vs backend", lambda: _plot_jitlog_time_breakdown(grouped, label_a, label_b)),
        ("Loops and bridges", lambda: _plot_jitlog_artifacts(grouped, label_a, label_b)),
        ("Iteration trends", lambda: _plot_jitlog_iterations(records, label_a, label_b)),
    ]
    if include_detail and _paired_benchmarks(grouped):
        specs.append(
            (
                "Additional metrics",
                lambda: _plot_jitlog_detail_metrics(grouped, label_a, label_b),
            )
        )
    return specs


def write_jitlog_analysis_html(
    html_path: Path,
    *,
    grouped: Optional[Dict[str, Dict[str, JitlogMetrics]]] = None,
    records: Optional[List[JitlogRecord]] = None,
    opt_trace_grouped: Optional[Dict[str, Dict[str, OptTraceStats]]] = None,
    label_a: str = "A",
    label_b: str = "B",
    include_detail: bool = False,
    op_top_n: int = 20,
    op_benchmark_top_n: int = 5,
    note_excludes_curve: bool = True,
) -> None:
    """Write summary and/or operation-analysis charts to one HTML file."""
    sections: List[str] = []
    has_summary = grouped and _paired_benchmarks(grouped)
    has_ops = opt_trace_grouped and _paired_opt_benchmarks(opt_trace_grouped)

    if has_summary:
        sections.extend(
            _figures_to_html_sections(
                _jitlog_summary_chart_specs(
                    grouped,
                    records or [],
                    label_a,
                    label_b,
                    include_detail=include_detail,
                )
            )
        )
        sections.append(
            "<section><h2>Summary table</h2><pre>"
            + format_jitlog_summary(records or []).replace("&", "&amp;").replace("<", "&lt;")
            + "</pre></section>"
        )
        sections.append(
            "<section><h2>Comparison table</h2><pre>"
            + format_jitlog_comparison(grouped, label_a, label_b, note_excludes_curve=note_excludes_curve)
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            + "</pre></section>"
        )

    if has_ops:
        sections.extend(
            _figures_to_html_sections(
                _opt_trace_chart_specs(
                    opt_trace_grouped,
                    label_a,
                    label_b,
                    top_n=op_top_n,
                    per_benchmark_top_n=op_benchmark_top_n,
                )
            )
        )
        sections.append(
            "<section><h2>JIT operation analysis (text)</h2><pre>"
            + format_opt_trace_analysis_report(
                opt_trace_grouped,
                label_a,
                label_b,
                note_excludes_curve=note_excludes_curve,
                top_n=op_top_n,
                per_benchmark_top_n=op_benchmark_top_n,
            )
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            + "</pre></section>"
        )

    if not sections:
        raise RuntimeError("No jit-summary or jit-log-opt data to visualize.")

    html_path.parent.mkdir(parents=True, exist_ok=True)
    html_path.write_text(
        _html_document(
            f"JIT analysis: {label_a} vs {label_b}",
            sections,
            subtitle=f"Configs: <strong>{label_a}</strong> vs <strong>{label_b}</strong>",
        ),
        encoding="utf-8",
    )


def _fmt(
    value: Optional[float], width: int, *, as_int: bool = False
) -> str:
    if value is None:
        return f"{'-':>{width}}"
    if as_int:
        return f"{int(value):>{width}d}"
    return f"{value:>{width}.6f}"


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        description="Analyze PYPYLOG jit-summary and jit-log-opt files from run_shootout.",
    )
    parser.add_argument(
        "paths",
        nargs="*",
        type=Path,
        help="Jitlog files or directories to analyze (default: logs/jitlog)",
    )
    parser.add_argument(
        "--from-logs",
        type=Path,
        default=None,
        metavar="DIR",
        help="Also load jitlog paths referenced in benchmark logs under DIR",
    )
    parser.add_argument(
        "--only",
        metavar="PATTERN",
        default=None,
        help="Only analyze files whose path contains this substring",
    )
    parser.add_argument(
        "--label-a",
        default="A",
        help="Label for config A in comparison output (default: A)",
    )
    parser.add_argument(
        "--label-b",
        default="B",
        help="Label for config B in comparison output (default: B)",
    )
    parser.add_argument(
        "--report",
        type=Path,
        default=None,
        help="Write the text report to this file",
    )
    parser.add_argument(
        "--json",
        type=Path,
        default=None,
        help="Write parsed metrics as JSON to this file",
    )
    parser.add_argument(
        "--pdf",
        type=Path,
        default=None,
        help="Write JIT charts (summary + operation analysis when available) to this PDF",
    )
    parser.add_argument(
        "--html",
        type=Path,
        default=None,
        help="Write JIT charts (summary + operation analysis when available) to this HTML file",
    )
    parser.add_argument(
        "--viz-detail",
        action="store_true",
        help="Include per-metric detail charts in PDF/HTML output",
    )
    parser.add_argument(
        "--include-curve",
        action="store_true",
        help="Include curve/ warmup benchmarks in analysis and visualization",
    )
    parser.add_argument(
        "--op-top",
        type=int,
        default=20,
        metavar="N",
        help="Show top N operation deltas in the detailed op report (default: 20)",
    )
    parser.add_argument(
        "--op-benchmark-top",
        type=int,
        default=8,
        metavar="N",
        help="Show top N operation deltas per benchmark (default: 8)",
    )
    parser.add_argument(
        "--op-pdf",
        type=Path,
        default=None,
        help="Write JIT operation analysis charts only to this PDF",
    )
    parser.add_argument(
        "--op-html",
        type=Path,
        default=None,
        help="Write JIT operation analysis charts only to this HTML file",
    )
    args = parser.parse_args(argv)

    repo_root = Path(__file__).resolve().parent.parent
    roots = list(args.paths)
    if not roots:
        default_dir = repo_root / "logs" / "jitlog"
        roots = [default_dir]

    jitlog_paths = discover_jitlog_files(roots, args.only)
    if args.from_logs:
        log_dir = args.from_logs if args.from_logs.is_absolute() else repo_root / args.from_logs
        jitlog_paths = sorted(set(jitlog_paths + discover_jitlogs_from_benchmark_logs(log_dir)))
        if args.only:
            jitlog_paths = [path for path in jitlog_paths if args.only in str(path)]

    records = load_records(jitlog_paths)
    field_op_records = load_field_op_records(jitlog_paths)
    if not records and not field_op_records:
        print("No jit-summary or jit-log-opt files found to analyze.", file=sys.stderr)
        return 1

    exclude_curve = not args.include_curve
    curve_summary_count = sum(1 for record in records if is_curve_benchmark(record.benchmark))
    curve_field_count = sum(
        1 for record in field_op_records if is_curve_benchmark(record.benchmark)
    )
    records = filter_stable_jitlog_records(records, exclude_curve=exclude_curve)
    field_op_records = filter_stable_field_op_records(
        field_op_records, exclude_curve=exclude_curve
    )
    if not records and not field_op_records:
        print("No stable jitlog files found (curve/ benchmarks excluded).", file=sys.stderr)
        return 1
    if exclude_curve and (curve_summary_count or curve_field_count):
        excluded = curve_summary_count + curve_field_count
        print(
            f"Note: excluded {excluded} curve/ jitlog(s) from analysis.",
            file=sys.stderr,
        )

    grouped = aggregate_records(records, exclude_curve=False) if records else {}
    field_ops_grouped = (
        aggregate_field_op_records(field_op_records, exclude_curve=False)
        if field_op_records
        else {}
    )
    opt_trace_grouped = (
        aggregate_opt_trace_records(field_op_records, exclude_curve=False)
        if field_op_records
        else {}
    )
    has_field_ops = bool(opt_trace_grouped)

    sections: List[str] = []
    if records:
        sections.extend([format_jitlog_summary(records), ""])
    has_comparison = any("A" in configs and "B" in configs for configs in grouped.values())
    if has_comparison:
        sections.append(
            format_jitlog_comparison(
                grouped,
                args.label_a,
                args.label_b,
                note_excludes_curve=exclude_curve,
            )
        )
    if has_field_ops:
        if sections:
            sections.append("")
        sections.append(
            format_opt_trace_analysis_report(
                opt_trace_grouped,
                args.label_a,
                args.label_b,
                note_excludes_curve=exclude_curve,
                top_n=args.op_top,
                per_benchmark_top_n=args.op_benchmark_top,
            )
        )
    elif records and any(path.suffix == ".jitlog" for path in jitlog_paths):
        sections.append("")
        sections.append(
            "Virtualizable field ops: no jit-log-opt traces found.\n"
            "Re-run with: python3 benchmark/run_shootout.py --compare virt --jitlog --jitlog-mode jit-log-opt"
        )

    report = "\n".join(sections)
    print(report)

    if args.report:
        report_path = args.report if args.report.is_absolute() else repo_root / args.report
        report_path.parent.mkdir(parents=True, exist_ok=True)
        report_path.write_text(report, encoding="utf-8")
        print(f"\nReport written to {report_path}")

    if args.json:
        payload = {
            "records": [
                {
                    "path": str(record.path),
                    "benchmark": record.benchmark,
                    "config": record.config,
                    "iteration": record.iteration,
                    "metrics": record.metrics.as_dict(),
                }
                for record in records
            ],
            "aggregated": {
                benchmark: {
                    config: metrics.as_dict() for config, metrics in by_config.items()
                }
                for benchmark, by_config in grouped.items()
            },
        }
        if has_field_ops:
            payload["virtualizable_field_ops"] = {
                benchmark: {
                    config: stats.as_dict() for config, stats in by_config.items()
                }
                for benchmark, by_config in field_ops_grouped.items()
            }
            payload["opt_trace_ops"] = {
                benchmark: {
                    config: stats.as_dict() for config, stats in by_config.items()
                }
                for benchmark, by_config in opt_trace_grouped.items()
            }
        json_path = args.json if args.json.is_absolute() else repo_root / args.json
        json_path.parent.mkdir(parents=True, exist_ok=True)
        json_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
        print(f"JSON written to {json_path}")

    if args.pdf:
        pdf_path = args.pdf if args.pdf.is_absolute() else repo_root / args.pdf
        try:
            write_jitlog_analysis_pdf(
                pdf_path,
                grouped=grouped if has_comparison else None,
                records=records,
                opt_trace_grouped=opt_trace_grouped if has_field_ops else None,
                label_a=args.label_a,
                label_b=args.label_b,
                include_detail=args.viz_detail,
                op_top_n=args.op_top,
                op_benchmark_top_n=args.op_benchmark_top,
            )
        except RuntimeError as exc:
            print(f"Error: {exc}", file=sys.stderr)
            return 1
        print(f"PDF written to {pdf_path}")

    if args.html:
        html_path = args.html if args.html.is_absolute() else repo_root / args.html
        try:
            write_jitlog_analysis_html(
                html_path,
                grouped=grouped if has_comparison else None,
                records=records,
                opt_trace_grouped=opt_trace_grouped if has_field_ops else None,
                label_a=args.label_a,
                label_b=args.label_b,
                include_detail=args.viz_detail,
                op_top_n=args.op_top,
                op_benchmark_top_n=args.op_benchmark_top,
                note_excludes_curve=exclude_curve,
            )
        except RuntimeError as exc:
            print(f"Error: {exc}", file=sys.stderr)
            return 1
        print(f"HTML written to {html_path}")

    if args.op_pdf:
        if not has_field_ops:
            print("Skipping op PDF: no paired jit-log-opt data found.", file=sys.stderr)
            return 1
        op_pdf_path = args.op_pdf if args.op_pdf.is_absolute() else repo_root / args.op_pdf
        try:
            generate_opt_trace_visualization(
                op_pdf_path,
                opt_trace_grouped,
                args.label_a,
                args.label_b,
                top_n=args.op_top,
                per_benchmark_top_n=args.op_benchmark_top,
            )
        except RuntimeError as exc:
            print(f"Error: {exc}", file=sys.stderr)
            return 1
        print(f"Op PDF written to {op_pdf_path}")

    if args.op_html:
        if not has_field_ops:
            print("Skipping op HTML: no paired jit-log-opt data found.", file=sys.stderr)
            return 1
        op_html_path = args.op_html if args.op_html.is_absolute() else repo_root / args.op_html
        try:
            generate_opt_trace_html(
                op_html_path,
                opt_trace_grouped,
                args.label_a,
                args.label_b,
                top_n=args.op_top,
                per_benchmark_top_n=args.op_benchmark_top,
                note_excludes_curve=exclude_curve,
            )
        except RuntimeError as exc:
            print(f"Error: {exc}", file=sys.stderr)
            return 1
        print(f"Op HTML written to {op_html_path}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
