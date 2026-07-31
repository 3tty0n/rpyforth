#!/usr/bin/env python3
"""
Run all RPyForth shootout benchmarks and analyze their output.

Usage:
    ./benchmark/run_shootout.py [--output logs/]

Comparison (--compare, repeatable):
    Presets (single --compare):
        jit      ./rpyforth-c vs ./rpyforth-c --jit off
        virt     ./rpyforth-c vs ./rpyforth-c-novirt
        gforth   gforth vs ./rpyforth-c
    Explicit binaries (two --compare values):
        ./run_shootout.py --compare ./rpyforth-c-stkfrag --compare ./rpyforth-c
        ./run_shootout.py --compare gforth --compare ./rpyforth-c
    Single executable (one --compare value that is not a preset):
        ./run_shootout.py --compare ./rpyforth-c-stkfrag

Override either side with --a-cmd / --b-cmd. Omit --compare for a single run.
Use --iterations N to repeat each run and report median elapsed times.
"""

import argparse
import json
import os
import re
import shlex
import shutil
import statistics
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple

from benchlib import curves, engines, paths, procs, stats, suites

from jitlog_analysis import (
    aggregate_benchmark_jitlogs,
    format_jitlog_comparison,
)

# Filesystem location of the shootout .fs sources (repo-root shootout/).
REPO_ROOT = paths.REPO_ROOT
SHOOTOUT_DIR = paths.SHOOTOUT_DIR


# Per-benchmark command-line arguments (filename relative to repo root).
# Most shootout files hard-code their input size; only a few are wired to
# read an argument from the command line.
DEFAULT_ARGS: Dict[str, List[str]] = {}

# How long we are willing to wait for a single benchmark run.
DEFAULT_TIMEOUT = procs.DEFAULT_TIMEOUT

# Named A/B comparison presets: (a_cmd, b_cmd, speedup = a/b when True else b/a).
COMPARE_PRESETS: Dict[str, Tuple[str, str, bool]] = {
    "jit": ("./rpyforth-c", "./rpyforth-c --jit off", False),
    "virt": ("./rpyforth-c", "./rpyforth-c-novirt", False),
    "gforth": ("gforth", "./rpyforth-c", True),
    "stkfrag": ("./rpyforth-c-stkfrag", "./rpyforth-c", False),
}

COMPARE_TITLES: Dict[str, str] = {
    "jit": "JIT Comparison",
    "virt": "Virtualization Comparison",
    "gforth": "Gforth Baseline Comparison",
    "stkfrag": "Stack-fragment Ablation (stkfrag vs contiguous)",
}


@dataclass(frozen=True)
class RunPlan:
    """Resolved commands and reporting options for one invocation."""

    preset: Optional[str]
    configs: Tuple[Tuple[str, List[str], str], ...]  # (id, cmd, label)
    speedup_a_over_b: bool
    gforth_baseline: bool
    skip_jit_analysis: bool
    skip_jitlog_for: frozenset[str]
    reference_config: str = "A"  # baseline column for multi-way (>2) comparisons

    @property
    def compare(self) -> bool:
        return len(self.configs) > 1

    @property
    def multi(self) -> bool:
        """True for an N-way comparison of three or more configurations."""
        return len(self.configs) > 2

    def label_for(self, config_id: str) -> str:
        for cfg_id, _, label in self.configs:
            if cfg_id == config_id:
                return label
        return config_id


@dataclass
class BenchmarkResult:
    name: str
    path: Path
    args: List[str]
    config: str = "A"
    label: str = ""
    command: str = ""
    status: str = "pending"
    returncode: int = 0
    stdout: str = ""
    stderr: str = ""
    wall_seconds: float = 0.0
    result_value: Optional[str] = None
    elapsed_usec: Optional[int] = None
    curve_times: List[int] = field(default_factory=list)
    curve_runs: List[List[int]] = field(default_factory=list)
    error_message: str = ""
    jitlog_path: Optional[Path] = None
    iteration: int = 1
    run_count: int = 1
    elapsed_samples: List[int] = field(default_factory=list)


# Set from --steady-state: curve/ metric becomes the converged-tail median.
STEADY_STATE = False


def steady_state_tail(times: List[int], frac: float = 0.5) -> Optional[int]:
    """Median of the converged tail (last `frac`) of a per-iteration curve."""
    return curves.steady_state_tail_usec(times, frac)


def median_ci(samples: List[int], confidence: float = 0.90,
              resamples: int = 2000) -> Tuple[Optional[float], float]:
    """Median and the relative half-width (%) of a bootstrap CI of the median."""
    return stats.median_ci(samples, confidence, resamples)


def capture_environment(pin: Optional[int]) -> str:
    """One-line description of the measurement environment, for reproducibility."""
    return procs.capture_environment(pin)


def git_revision(repo_root: Path) -> str:
    """Short git revision of the tree, with a -dirty suffix when there are
    uncommitted changes, so benchmark outputs are traceable to a commit."""
    return paths.git_revision(repo_root)


def discover_benchmarks(root: Path) -> List[Path]:
    """Return all .fs files under shootout/, sorted.

    Forth-specific adaptations live in shootout/vfxforth/ and
    shootout/swiftforth/; they are selected by the wrapper scripts and must
    not be discovered as separate benchmarks.
    """
    return suites.discover_shootout(root / "shootout")


def benchmark_display_name(benchmark: Path, repo_root: Path) -> str:
    """Return the "shootout/..." display name for a benchmark path."""
    return suites.shootout_display_name(benchmark, repo_root)


def run_benchmark(
    cmd_prefix: List[str],
    benchmark: Path,
    args: List[str],
    timeout: int,
    config: str,
    repo_root: Path,
    env: Optional[Dict[str, str]] = None,
    wrapper: Optional[List[str]] = None,
    pin: Optional[int] = None,
) -> BenchmarkResult:
    """Execute one benchmark and capture its output."""
    result = BenchmarkResult(
        name=benchmark_display_name(benchmark, repo_root),
        path=benchmark,
        args=list(args),
        config=config,
        command=" ".join(cmd_prefix),
        label=short_label(cmd_prefix),
    )

    cmd = list(wrapper or []) + list(cmd_prefix) + [str(benchmark)] + args
    outcome = procs.run_capture(
        cmd,
        cwd=repo_root,
        env=env,
        timeout=timeout,
        pin=pin,
        stdin_path=suites.stdin_path_for(result.name),
    )

    if outcome.missing:
        result.status = "error"
        result.returncode = -1
        result.error_message = f"binary not found: {cmd[0]}"
        return result

    result.wall_seconds = outcome.wall
    result.returncode = outcome.rc
    result.stdout = outcome.stdout
    result.stderr = outcome.stderr

    if outcome.timed_out:
        result.status = "timeout"
        result.error_message = f"timed out after {timeout}s"
        return result

    if outcome.rc != 0:
        result.status = "error"
        result.error_message = f"exit code {outcome.rc}"
        return result

    result.status = "ok"
    return result


def parse_standard_output(result: BenchmarkResult) -> None:
    """Extract result value and elapsed time from standard shootout output."""
    text = result.stdout

    elapsed_match = re.search(r"Elapsed:\s*(\d+)\s*usec", text)
    if elapsed_match:
        result.elapsed_usec = int(elapsed_match.group(1))

    # Known result labels (e.g. "Ack: 8189", "Count: 1028").
    # Allow the label anywhere on the line so wrappers/forths that prefix the
    # line with "Including ..." still report the correct value.
    known_label_match = re.search(
        r"(?:^|.*\s)(Ack|Count|Fib|Result):\s*(\S+)", text, re.MULTILINE | re.IGNORECASE
    )
    if known_label_match:
        result.result_value = f"{known_label_match.group(1)}: {known_label_match.group(2)}"
        return

    # Other "Word: value" labels, but not the elapsed/time report lines.
    for match in re.finditer(r"^\s*([A-Za-z][A-Za-z0-9_]*):\s*(\S+)", text, re.MULTILINE):
        label = match.group(1)
        if label.lower() in ("elapsed", "time"):
            continue
        result.result_value = f"{label}: {match.group(2)}"
        return

    # Fallback: first line that looks like one or more unsigned integers.
    for line in text.splitlines():
        line = line.strip()
        if not line or line.lower().startswith("elapsed"):
            continue
        numbers = re.findall(r"\d+", line)
        if numbers:
            result.result_value = " ".join(numbers)
            return


def parse_curve_output(result: BenchmarkResult) -> None:
    """Extract per-iteration timings from curve/ CSV output."""
    times = curves.parse_curve_output(result.stdout)
    result.curve_times = times
    if times:
        result.elapsed_usec = steady_state_tail(times) if STEADY_STATE else sum(times)


def analyze_result(result: BenchmarkResult) -> None:
    """Parse benchmark output according to its category."""
    if result.status != "ok":
        return

    from jitlog_analysis import is_curve_benchmark

    if is_curve_benchmark(result.name):
        parse_curve_output(result)
    else:
        parse_standard_output(result)

    # Treat any stderr or obvious failure markers as a warning/error.
    if result.stderr and "error" not in result.status:
        # Some benchmarks may legitimately print to stderr, so just keep it.
        pass

    failure_markers = ["UNKNOWN:", "ABORT", "THROW:", "stack underflow"]
    combined = result.stdout + result.stderr
    if any(marker in combined for marker in failure_markers):
        result.status = "warning"
        result.error_message = "detected failure marker in output"


def log_path_for(
    result: BenchmarkResult, log_dir: Path, iteration: int, total_iterations: int
) -> Path:
    safe_name = result.name.replace("/", "_").replace("\\", "_")
    if total_iterations > 1:
        return log_dir / f"{safe_name}_{result.config}_i{iteration:03d}.log"
    return log_dir / f"{safe_name}_{result.config}.log"


def discover_log_files(
    output_dir: Path, compare: bool, only: Optional[str], iterations: int
) -> List[Path]:
    """Find saved log files for analyze-only mode."""
    if compare:
        # Config ids are single uppercase letters (A, B, C, ...), so a charclass
        # glob covers two-way and N-way comparisons alike.
        numbered = sorted(output_dir.glob("*_[A-Z]_i*.log"))
        unnumbered = sorted(output_dir.glob("*_[A-Z].log"))
        if iterations > 1 or (numbered and not unnumbered):
            log_files = numbered
        else:
            log_files = unnumbered or numbered
    elif iterations > 1:
        log_files = sorted(output_dir.glob("*_A_i*.log"))
    else:
        numbered = sorted(output_dir.glob("*_A_i*.log"))
        unnumbered = sorted(
            p
            for p in output_dir.glob("*.log")
            if not p.name.endswith(("_A.log", "_B.log"))
            and "_i" not in p.stem
        )
        log_files = numbered if numbered and not unnumbered else unnumbered or numbered

    if only:
        log_files = [p for p in log_files if only in p.name]
    return sorted(set(log_files))


def aggregate_iterations(results: List[BenchmarkResult]) -> List[BenchmarkResult]:
    """Collapse repeated runs into one result per benchmark/config (median elapsed)."""
    groups: Dict[Tuple[str, str], List[BenchmarkResult]] = {}
    for result in results:
        groups.setdefault((result.name, result.config), []).append(result)

    aggregated: List[BenchmarkResult] = []
    for (name, config) in sorted(groups):
        runs = sorted(groups[(name, config)], key=lambda r: r.iteration)
        if len(runs) == 1:
            run = runs[0]
            if run.elapsed_usec is not None:
                run.elapsed_samples = [run.elapsed_usec]
            if run.curve_times and not run.curve_runs:
                run.curve_runs = [run.curve_times]
            aggregated.append(run)
            continue

        ok_runs = [r for r in runs if r.status in ("ok", "warning")]
        failed_count = len(runs) - len(ok_runs)
        elapsed_samples = [r.elapsed_usec for r in ok_runs if r.elapsed_usec is not None]
        last_ok = ok_runs[-1] if ok_runs else runs[-1]

        merged = BenchmarkResult(
            name=name,
            path=runs[0].path,
            args=runs[0].args,
            config=config,
            status=last_ok.status,
            returncode=last_ok.returncode,
            stdout=last_ok.stdout,
            stderr=last_ok.stderr,
            wall_seconds=statistics.mean(r.wall_seconds for r in runs),
            result_value=last_ok.result_value,
            elapsed_usec=int(statistics.median(elapsed_samples)) if elapsed_samples else None,
            curve_times=last_ok.curve_times,
            curve_runs=[r.curve_times for r in ok_runs if r.curve_times],
            error_message=last_ok.error_message,
            jitlog_path=last_ok.jitlog_path,
            iteration=0,
            run_count=len(runs),
            elapsed_samples=elapsed_samples,
        )
        if failed_count:
            merged.status = "warning" if ok_runs else "error"
            detail = f"{failed_count}/{len(runs)} runs failed"
            merged.error_message = (
                f"{merged.error_message}; {detail}" if merged.error_message else detail
            )
        aggregated.append(merged)
    return aggregated


def save_log(
    result: BenchmarkResult,
    log_dir: Path,
    jitlog_path: Optional[Path] = None,
    *,
    iteration: int = 1,
    total_iterations: int = 1,
) -> Path:
    """Write the raw log for a benchmark run to disk."""
    result.jitlog_path = jitlog_path
    log_dir.mkdir(parents=True, exist_ok=True)
    log_path = log_path_for(result, log_dir, iteration, total_iterations)
    with log_path.open("w", encoding="utf-8") as f:
        f.write(f"# benchmark: {result.name}\n")
        f.write(f"# config: {result.config}\n")
        if result.label:
            f.write(f"# label: {result.label}\n")
        if result.command:
            f.write(f"# command: {result.command}\n")
        f.write(f"# iteration: {iteration}\n")
        f.write(f"# command args: {' '.join(result.args)}\n")
        f.write(f"# status: {result.status}\n")
        f.write(f"# return code: {result.returncode}\n")
        f.write(f"# wall seconds: {result.wall_seconds:.6f}\n")
        if jitlog_path:
            f.write(f"# jitlog: {jitlog_path}\n")
        if result.error_message:
            f.write(f"# error: {result.error_message}\n")
        f.write("# --- stdout ---\n")
        f.write(result.stdout)
        if result.stderr:
            f.write("\n# --- stderr ---\n")
            f.write(result.stderr)
    return log_path


def load_log(log_path: Path) -> Optional[BenchmarkResult]:
    """Reconstruct a BenchmarkResult from a saved log file."""
    if not log_path.exists():
        return None
    text = log_path.read_text(encoding="utf-8")

    lines = text.splitlines()
    headers: Dict[str, str] = {}
    stdout_lines: List[str] = []
    stderr_lines: List[str] = []
    section = None
    for line in lines:
        if line.startswith("# --- stdout ---"):
            section = "stdout"
            continue
        if line.startswith("# --- stderr ---"):
            section = "stderr"
            continue
        if section is None and line.startswith("# "):
            key_value = line[2:].strip()
            if ":" in key_value:
                key, value = key_value.split(":", 1)
                headers[key.strip()] = value.strip()
            continue
        if section == "stdout":
            stdout_lines.append(line)
        elif section == "stderr":
            stderr_lines.append(line)

    name = headers.get("benchmark", log_path.stem)
    # Reconstruct the relative path from the safe name if possible.
    # Log files use underscores in place of slashes.
    name = name.replace("_curve_", "/curve/").replace("_shootout_", "/")

    result = BenchmarkResult(
        name=name,
        path=Path("shootout") / name,
        args=headers.get("command args", "").split(),
        config=headers.get("config", "A"),
        label=headers.get("label", ""),
        command=headers.get("command", ""),
        status=headers.get("status", "ok"),
        returncode=int(headers.get("return code", "0")),
        stdout="\n".join(stdout_lines),
        stderr="\n".join(stderr_lines),
        wall_seconds=float(headers.get("wall seconds", "0")),
        error_message=headers.get("error", ""),
        iteration=int(headers.get("iteration", "1")),
    )
    jitlog = headers.get("jitlog")
    if jitlog:
        result.jitlog_path = Path(jitlog)
    analyze_result(result)
    return result


def format_summary(results: List[BenchmarkResult]) -> str:
    """Build a human-readable summary table."""
    lines: List[str] = []
    lines.append("=" * 90)
    lines.append(f"{'Benchmark':<25} {'Config':<8} {'Status':<10} {'Result / Metric':<30} {'Wall (s)':>12}")
    lines.append("-" * 90)

    for r in results:
        if r.status != "ok":
            metric = r.error_message or "-"
        elif r.name.startswith("curve/"):
            if r.curve_times:
                mean = statistics.mean(r.curve_times)
                median = statistics.median(r.curve_times)
                metric = f"{len(r.curve_times)} iters, mean={mean:.0f}us, median={median:.0f}us"
            else:
                metric = "no timings parsed"
        else:
            elapsed = f"{r.elapsed_usec}us" if r.elapsed_usec is not None else "-"
            value = r.result_value if r.result_value is not None else "-"
            metric = f"{value} ({elapsed})"
            if r.run_count > 1:
                metric = f"{metric}, median of {r.run_count}"

        lines.append(
            f"{r.name:<25} {r.config:<8} {r.status:<10} {metric:<30} {r.wall_seconds:>12.3f}"
        )

    lines.append("=" * 90)

    ok = sum(1 for r in results if r.status == "ok")
    warn = sum(1 for r in results if r.status == "warning")
    err = sum(1 for r in results if r.status in ("error", "timeout"))
    lines.append(f"Summary: {ok} ok, {warn} warnings, {err} errors/timeouts out of {len(results)} runs")
    return "\n".join(lines)


def parse_cmd(text: str) -> List[str]:
    """Split a shell-style command string into a list."""
    return shlex.split(text)


def short_label(cmd: List[str]) -> str:
    """Return a compact display label for a command prefix."""
    name = Path(cmd[0]).name
    # Keep stack-layout / build variants distinct for ablation charts.
    variant = engines.engine_display_name(name)
    if variant in ("stkfrag", "contiguous", "novirt"):
        return variant
    canon = engines.normalize_engine(name)
    if canon in ("rpyforth", "gforth-fast", "gforth", "vfxforth", "swiftforth"):
        return engines.engine_display_name(canon)
    if len(cmd) == 1:
        return name
    return f"{name} {' '.join(cmd[1:])}"


def is_gforth_cmd(cmd: List[str]) -> bool:
    """Return True when a command prefix invokes Gforth."""
    return bool(cmd) and Path(cmd[0]).name == "gforth"


def normalize_comparison_commands(
    a_text: str, b_text: str
) -> Tuple[str, str, bool]:
    """Return commands with gforth on the A side when present."""
    a_cmd = parse_cmd(a_text)
    b_cmd = parse_cmd(b_text)
    if is_gforth_cmd(a_cmd):
        return a_text, b_text, True
    if is_gforth_cmd(b_cmd):
        return b_text, a_text, True
    return a_text, b_text, False


def resolve_command(cmd: List[str], repo_root: Path) -> Optional[Path]:
    """Return the executable path if a command prefix is runnable."""
    if not cmd:
        return None
    exe = Path(cmd[0])
    if exe.is_absolute() and exe.exists():
        return exe
    local = repo_root / exe
    if local.exists():
        return local
    found = shutil.which(cmd[0])
    if found:
        return Path(found)
    return None


def build_run_plan(args: argparse.Namespace) -> RunPlan:
    """Resolve preset, commands, and reporting behavior from CLI args."""
    compare_values = args.compare or []

    if len(compare_values) >= 2 and all(value in COMPARE_PRESETS for value in compare_values):
        raise SystemExit(
            "Pass one comparison preset on its own "
            f"({', '.join(sorted(COMPARE_PRESETS))}), not several together"
        )

    def single_plan(cmd_text: str) -> RunPlan:
        cmd = parse_cmd(cmd_text)
        return RunPlan(
            preset=None,
            configs=(("A", cmd, short_label(cmd)),),
            speedup_a_over_b=False,
            gforth_baseline=False,
            skip_jit_analysis=True,
            skip_jitlog_for=frozenset(),
        )

    def paired_plan(
        a_text: str,
        b_text: str,
        *,
        preset: Optional[str] = None,
        speedup_a_over_b: Optional[bool] = None,
    ) -> RunPlan:
        if preset is None:
            a_text, b_text, inferred = normalize_comparison_commands(a_text, b_text)
            if speedup_a_over_b is None:
                speedup_a_over_b = inferred
        elif speedup_a_over_b is None:
            speedup_a_over_b = COMPARE_PRESETS[preset][2]

        a_cmd = parse_cmd(a_text)
        b_cmd = parse_cmd(b_text)
        gforth_baseline = is_gforth_cmd(a_cmd)
        skip_jit_analysis = gforth_baseline or is_gforth_cmd(b_cmd)
        skip_jitlog_for = frozenset({"A"}) if gforth_baseline else frozenset()
        return RunPlan(
            preset=preset,
            configs=(
                ("A", a_cmd, short_label(a_cmd)),
                ("B", b_cmd, short_label(b_cmd)),
            ),
            speedup_a_over_b=speedup_a_over_b,
            gforth_baseline=gforth_baseline,
            skip_jit_analysis=skip_jit_analysis,
            skip_jitlog_for=skip_jitlog_for,
        )

    def multi_plan(cmd_texts: List[str]) -> RunPlan:
        """Compare three or more configurations; the first is the baseline."""
        configs: List[Tuple[str, List[str], str]] = []
        skip_jitlog: set[str] = set()
        for index, text in enumerate(cmd_texts):
            config_id = chr(ord("A") + index)
            cmd = parse_cmd(text)
            configs.append((config_id, cmd, short_label(cmd)))
            if is_gforth_cmd(cmd):
                skip_jitlog.add(config_id)
        return RunPlan(
            preset=None,
            configs=tuple(configs),
            speedup_a_over_b=False,
            gforth_baseline=False,
            # The JIT-summary comparison is intrinsically pairwise; skip it when
            # more than two configs are present.
            skip_jit_analysis=True,
            skip_jitlog_for=frozenset(skip_jitlog),
            reference_config="A",
        )

    if len(compare_values) == 1 and compare_values[0] in COMPARE_PRESETS:
        preset = compare_values[0]
        default_a, default_b, speedup_a_over_b = COMPARE_PRESETS[preset]
        return paired_plan(
            args.a_cmd or default_a,
            args.b_cmd or default_b,
            preset=preset,
            speedup_a_over_b=speedup_a_over_b,
        )

    if len(compare_values) == 2:
        return paired_plan(
            args.a_cmd or compare_values[0],
            args.b_cmd or compare_values[1],
        )

    if len(compare_values) == 1:
        return single_plan(args.a_cmd or compare_values[0])

    if len(compare_values) > 2:
        return multi_plan(compare_values)

    if args.b_cmd:
        return paired_plan(args.a_cmd or "./rpyforth-c", args.b_cmd)

    return single_plan(args.a_cmd or "./rpyforth-c")


def format_comparison(results: List[BenchmarkResult], plan: RunPlan) -> str:
    groups: Dict[str, Dict[str, BenchmarkResult]] = {}
    for r in results:
        groups.setdefault(r.name, {})[r.config] = r

    a_label = plan.label_for("A")
    b_label = plan.label_for("B")

    lines: List[str] = []
    lines.append("=" * 100)
    if plan.preset:
        lines.append(COMPARE_TITLES[plan.preset])
        lines.append("=" * 100)
    header = (
        f"{'Benchmark':<25} "
        f"{a_label + ' elapsed':>16} "
        f"{b_label + ' elapsed':>16} "
        f"{'Speedup':>10} "
        f"{'Match':>8}"
    )
    lines.append(header)
    lines.append("-" * 100)

    mismatches = 0
    errors = 0
    speedup_vals: List[float] = []
    for name in sorted(groups):
        group = groups[name]
        a = group.get("A")
        b = group.get("B")

        if a is None or b is None:
            errors += 1
            lines.append(f"{name:<25} {'missing config pair':>58}")
            continue

        a_status_ok = a.status in ("ok", "warning")
        b_status_ok = b.status in ("ok", "warning")
        if not (a_status_ok and b_status_ok):
            errors += 1
            a_err = a.error_message or a.status
            b_err = b.error_message or b.status
            lines.append(f"{name:<25} A:{a_err:>14} B:{b_err:>14}")
            continue

        a_elapsed = a.elapsed_usec if a.elapsed_usec is not None else 0
        b_elapsed = b.elapsed_usec if b.elapsed_usec is not None else 0

        if plan.speedup_a_over_b:
            speedup = stats.speedup(a_elapsed, b_elapsed)
        else:
            speedup = stats.speedup(b_elapsed, a_elapsed)
        speedup_str = f"{speedup:.2f}x" if speedup is not None else "n/a"
        if speedup is not None and speedup > 0:
            speedup_vals.append(speedup)

        match = a.result_value == b.result_value
        if not match:
            mismatches += 1
        match_str = "yes" if match else "NO"

        lines.append(
            f"{name:<25} {a_elapsed:>16,}us {b_elapsed:>16,}us {speedup_str:>10} {match_str:>8}"
        )

    if speedup_vals:
        gm = stats.geomean(speedup_vals)
        gm_str = f"{gm:.2f}x" if gm is not None else "n/a"
        lines.append(
            f"{'geomean':<25} {'':>16} {'':>16} {gm_str:>10} {'':>8}"
        )

    lines.append("=" * 100)
    lines.append(
        f"Comparison summary: {len(groups)} pairs, {mismatches} result mismatches, {errors} errors"
    )
    if plan.speedup_a_over_b:
        lines.append(
            f"Interpretation: speedup = {a_label} / {b_label}; >1 means {b_label} is faster."
        )
    else:
        lines.append(
            f"Interpretation: speedup = {b_label} / {a_label}; >1 means {a_label} is faster."
        )
    if mismatches:
        lines.append("Warning: result values differ between configurations!")
    return "\n".join(lines)


def format_multi_comparison(results: List[BenchmarkResult], plan: RunPlan) -> str:
    groups: Dict[str, Dict[str, BenchmarkResult]] = {}
    for r in results:
        groups.setdefault(r.name, {})[r.config] = r

    config_ids = [cfg_id for cfg_id, _, _ in plan.configs]
    ref = plan.reference_config
    others = [c for c in config_ids if c != ref]

    def cell(r: Optional[BenchmarkResult]) -> Tuple[str, float, Optional[int]]:
        if r is None or r.status not in ("ok", "warning") or r.elapsed_usec is None:
            return ("-", 0.0, None)
        _, ci = median_ci(r.elapsed_samples or [r.elapsed_usec])
        disp = f"{r.elapsed_usec:,}" + (f" ±{ci:.0f}%" if ci > 0 else "")
        return (disp, ci, r.elapsed_usec)

    colw = 22
    speedup_w = max(12, max((len(plan.label_for(c)) for c in others), default=8) + 2)
    width = 24 + (colw + 1) * len(config_ids) + 1 + (speedup_w + 1) * len(others)
    lines: List[str] = []
    lines.append("=" * width)
    lines.append(
        f"Performance comparison (median us; speedup = baseline / {plan.label_for(ref)})"
    )
    lines.append("=" * width)
    header = f"{'Benchmark':<24}"
    for cfg_id in config_ids:
        header += f" {plan.label_for(cfg_id):>{colw}}"
    for cfg_id in others:
        header += f" {'vs ' + plan.label_for(cfg_id):>{speedup_w}}"
    lines.append(header)
    lines.append("-" * width)

    mismatches = 0
    noisy = False
    elapsed_by_cfg: Dict[str, List[float]] = {c: [] for c in config_ids}
    speedup_by_other: Dict[str, List[float]] = {c: [] for c in others}

    for name in sorted(groups):
        group = groups[name]
        row = f"{name:<24}"
        _, ref_ci, ref_elapsed = cell(group.get(ref))
        values = set()
        elapsed_row: Dict[str, Optional[int]] = {}
        for cfg_id in config_ids:
            r = group.get(cfg_id)
            disp, ci, elapsed = cell(r)
            row += f" {disp:>{colw}}"
            elapsed_row[cfg_id] = elapsed
            if elapsed is not None and elapsed > 0:
                elapsed_by_cfg[cfg_id].append(float(elapsed))
            if r is not None and r.result_value is not None:
                values.add(r.result_value)
        for cfg_id in others:
            elapsed = elapsed_row.get(cfg_id)
            speedup = stats.speedup(elapsed, ref_elapsed)
            if speedup is not None:
                _, ci, _ = cell(group.get(cfg_id))
                within = abs(speedup - 1.0) <= (ci + ref_ci) / 100.0
                noisy = noisy or within
                mark = "~" if within else ""
                cell_s = f"{speedup:.2f}x{mark}"
                row += f" {cell_s:>{speedup_w}}"
                if speedup > 0:
                    speedup_by_other[cfg_id].append(speedup)
            else:
                row += f" {'-':>{speedup_w}}"
        if len(values) > 1:
            mismatches += 1
        lines.append(row)

    geo_row = f"{'geomean':<24}"
    for cfg_id in config_ids:
        gm = stats.geomean(elapsed_by_cfg[cfg_id])
        if gm is not None:
            geo_row += f" {f'{gm:,.0f}':>{colw}}"
        else:
            geo_row += f" {'-':>{colw}}"
    for cfg_id in others:
        gm = stats.geomean(speedup_by_other[cfg_id])
        if gm is not None:
            geo_row += f" {f'{gm:.2f}x':>{speedup_w}}"
        else:
            geo_row += f" {'-':>{speedup_w}}"
    lines.append(geo_row)

    lines.append("=" * width)
    lines.append(
        f"speedup = baseline / {plan.label_for(ref)}; >1.0 means {plan.label_for(ref)} is faster"
    )
    lines.append("± = 90% bootstrap CI of the median over the timed runs")
    if noisy:
        lines.append("~ = difference within combined run-to-run noise (not significant)")
    if mismatches:
        lines.append(f"Warning: {mismatches} benchmark(s) produced differing result values!")
    return "\n".join(lines)


def format_virtualization_analysis(results: List[BenchmarkResult], plan: RunPlan) -> str:
    """Build a virtualization-focused report comparing JIT metrics from jit-summaries."""
    grouped = aggregate_benchmark_jitlogs(results)
    if not grouped:
        return ""
    return "\n" + format_jitlog_comparison(
        grouped,
        plan.label_for("A"),
        plan.label_for("B"),
        title="Virtualization Analysis (JIT metrics from jit-summary)",
    )


def save_analysis_report(
    report_path: Path,
    results: List[BenchmarkResult],
    plan: RunPlan,
    jitlog_mode: str,
) -> None:
    """Write a full text analysis report to disk."""
    report_path.parent.mkdir(parents=True, exist_ok=True)
    sections: List[str] = [format_summary(results), ""]
    if plan.multi:
        sections.append(format_multi_comparison(results, plan))
    elif plan.compare:
        sections.append(format_comparison(results, plan))
    if jitlog_mode == "jit-summary" and not plan.skip_jit_analysis:
        sections.append(format_virtualization_analysis(results, plan))

    report_path.write_text("\n".join(sections), encoding="utf-8")


def make_env_with_jitlog(
    base_env: Dict[str, str],
    jitlog_dir: Path,
    benchmark_name: str,
    config: str,
    mode: str = "jit-summary",
    iteration: int = 1,
    total_iterations: int = 1,
) -> Tuple[Dict[str, str], Path]:
    """Return environment dict and path for a PYPYLOG capture.

    mode is the PYPYLOG category (e.g. jit-summary, jit-log-opt, jit-backend).
    """
    jitlog_dir.mkdir(parents=True, exist_ok=True)
    safe_name = benchmark_name.replace("/", "_").replace("\\", "_")
    if total_iterations > 1:
        jitlog_path = jitlog_dir / f"{safe_name}_{config}_i{iteration:03d}.jitlog"
    else:
        jitlog_path = jitlog_dir / f"{safe_name}_{config}.jitlog"
    env = dict(base_env)
    env["PYPYLOG"] = procs.pypylog_value(mode, jitlog_path)
    return env, jitlog_path


class RunProgress:
    """Simple stderr progress reporter for benchmark runs."""

    def __init__(self, total: int, *, enabled: bool = True) -> None:
        self.total = total
        self.enabled = enabled and total > 0 and sys.stderr.isatty()
        self.current = 0
        self._width = 36

    def update(self, label: str) -> None:
        self.current += 1
        if not self.enabled:
            print(f"[{self.current}/{self.total}] {label}", file=sys.stderr)
            return
        ratio = self.current / self.total
        filled = int(self._width * ratio)
        bar = "#" * filled + "-" * (self._width - filled)
        sys.stderr.write(
            f"\r[{bar}] {self.current}/{self.total} {label[:50]:<50}"
        )
        sys.stderr.flush()

    def finish(self) -> None:
        if self.enabled and self.total > 0:
            sys.stderr.write("\n")
            sys.stderr.flush()


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        description="Run RPyForth shootout benchmarks and analyze their output."
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("logs"),
        help="Directory to save per-benchmark log files (default: ./logs)",
    )
    parser.add_argument(
        "--json",
        type=Path,
        default=None,
        help="Write a machine-readable JSON summary to this file",
    )
    parser.add_argument(
        "--timeout",
        type=int,
        default=DEFAULT_TIMEOUT,
        help=f"Timeout in seconds for each benchmark run (default: {DEFAULT_TIMEOUT})",
    )
    parser.add_argument(
        "--iterations",
        type=int,
        default=1,
        metavar="N",
        help="Run each benchmark/configuration N times; reports use median elapsed time (default: 1)",
    )
    parser.add_argument(
        "--only",
        metavar="PATTERN",
        default=None,
        help="Only run benchmarks whose path contains this substring",
    )
    parser.add_argument(
        "--exclude",
        metavar="PATTERN",
        default=None,
        help="Skip benchmarks whose path contains this substring (e.g. curve/)",
    )
    parser.add_argument(
        "--compare",
        action="append",
        metavar="CMD",
        default=None,
        help=(
            "Executable or shell command to run. Repeat twice to compare A vs B "
            f"(e.g. --compare ./rpyforth-c-stkfrag --compare ./rpyforth-c). "
            f"A single preset name also works ({', '.join(sorted(COMPARE_PRESETS))})."
        ),
    )
    parser.add_argument(
        "--a-cmd",
        type=str,
        default=None,
        help="Shell command for configuration A (default: ./rpyforth-c, or preset default)",
    )
    parser.add_argument(
        "--b-cmd",
        type=str,
        default=None,
        help="Shell command for configuration B (enables comparison when set without --compare)",
    )
    parser.add_argument(
        "--jitlog",
        action="store_true",
        help="Capture a PYPYLOG jit-log-opt trace for every run",
    )
    parser.add_argument(
        "--jitlog-dir",
        type=Path,
        default=Path("logs/jitlog"),
        help="Directory for jitlog files (default: ./logs/jitlog)",
    )
    parser.add_argument(
        "--jitlog-mode",
        type=str,
        default="jit-summary",
        help="PYPYLOG category to capture (default: jit-summary)",
    )
    parser.add_argument(
        "--report",
        type=Path,
        default=None,
        help="Write a combined text report (summary + A/B + virtualization analysis) to this file",
    )
    parser.add_argument(
        "--pdf",
        type=Path,
        default=None,
        help="Generate a multi-page PDF graph report and save it to this file",
    )
    parser.add_argument(
        "--chart",
        type=Path,
        default=None,
        help="Save a single-image bar chart (normalized + absolute elapsed); "
        "the format is chosen from the file extension (.pdf, .png, .svg)",
    )
    parser.add_argument(
        "--curve-chart",
        type=Path,
        default=None,
        help="Save a single-image warm-up curve chart (time per iteration for the "
        "curve/ benchmarks); format chosen from the file extension",
    )
    parser.add_argument(
        "--analyze-only",
        action="store_true",
        help="Skip running benchmarks; analyze existing logs in --output and jitlogs in --jitlog-dir",
    )
    parser.add_argument(
        "--pin",
        type=int,
        default=None,
        metavar="CORE",
        help="Pin every benchmark to this CPU core via taskset, to reduce scheduler noise",
    )
    parser.add_argument(
        "--steady-state",
        action="store_true",
        help="For curve/ benchmarks, report the converged-tail median (warm-up excluded) "
        "instead of the total time",
    )
    args = parser.parse_args(argv)
    if args.iterations < 1:
        print("Error: --iterations must be at least 1", file=sys.stderr)
        return 1

    global STEADY_STATE
    STEADY_STATE = args.steady_state

    if args.pin is not None and shutil.which("taskset") is None:
        print("Error: --pin requires taskset (util-linux)", file=sys.stderr)
        return 1

    repo_root = Path(__file__).resolve().parent.parent
    plan = build_run_plan(args)
    revision = git_revision(repo_root)

    env_line = capture_environment(args.pin) + f" | commit {revision}"

    base_output = args.output if args.output.is_absolute() else repo_root / args.output
    output_dir = base_output / revision
    jitlog_dir = args.jitlog_dir if args.jitlog_dir.is_absolute() else repo_root / args.jitlog_dir

    if args.analyze_only:
        log_files = discover_log_files(output_dir, plan.compare, args.only, args.iterations)
        if not log_files:
            print("No log files found to analyze.", file=sys.stderr)
            return 1
        results: List[BenchmarkResult] = []
        for log_path in log_files:
            result = load_log(log_path)
            if result is not None:
                results.append(result)
    else:
        for config_id, cmd, _label in plan.configs:
            if resolve_command(cmd, repo_root) is None:
                print(
                    f"Error: command for config {config_id} not found: {' '.join(cmd)}",
                    file=sys.stderr,
                )
                return 1

        benchmarks = discover_benchmarks(repo_root)
        if args.only:
            benchmarks = [
                b for b in benchmarks if args.only in benchmark_display_name(b, repo_root)
            ]
        if args.exclude:
            benchmarks = [
                b
                for b in benchmarks
                if args.exclude not in benchmark_display_name(b, repo_root)
            ]

        if not benchmarks:
            print("No benchmarks found.", file=sys.stderr)
            return 1

        base_env = os.environ.copy()
        results = []
        total_runs = len(benchmarks) * len(plan.configs) * args.iterations
        progress = RunProgress(total_runs)
        try:
            for benchmark in benchmarks:
                rel = benchmark_display_name(benchmark, repo_root)
                bench_args = DEFAULT_ARGS.get(rel, [])

                for config_id, cmd_prefix, _label in plan.configs:
                    for iteration in range(1, args.iterations + 1):
                        env = base_env
                        jitlog_path: Optional[Path] = None
                        if args.jitlog and config_id not in plan.skip_jitlog_for:
                            env, jitlog_path = make_env_with_jitlog(
                                base_env,
                                jitlog_dir,
                                rel,
                                config_id,
                                args.jitlog_mode,
                                iteration=iteration,
                                total_iterations=args.iterations,
                            )

                        result = run_benchmark(
                            cmd_prefix,
                            benchmark,
                            bench_args,
                            args.timeout,
                            config_id,
                            repo_root,
                            env=env,
                            pin=args.pin,
                        )
                        result.iteration = iteration
                        analyze_result(result)
                        save_log(
                            result,
                            output_dir,
                            jitlog_path=jitlog_path,
                            iteration=iteration,
                            total_iterations=args.iterations,
                        )
                        results.append(result)

                        label = f"{rel} ({config_id}"
                        if args.iterations > 1:
                            label += f", iter {iteration}"
                        label += ")"
                        progress.update(label)
        finally:
            progress.finish()

    results = aggregate_iterations(results)

    print(env_line)
    if STEADY_STATE:
        print("metric: curve/ steady-state = converged-tail median (warm-up excluded)")
    print(format_summary(results))

    if plan.multi:
        print()
        print(format_multi_comparison(results, plan))
    elif plan.compare:
        print()
        print(format_comparison(results, plan))

    if (
        args.jitlog_mode == "jit-summary"
        and not plan.skip_jit_analysis
        and any(r.jitlog_path for r in results)
    ):
        print(format_virtualization_analysis(results, plan))

    if args.report:
        report_path = args.report if args.report.is_absolute() else repo_root / args.report
        save_analysis_report(report_path, results, plan, args.jitlog_mode)
        print(f"\nAnalysis report written to {report_path}")

    if args.pdf:
        from plot_shootout import generate_pdf_report

        pdf_path = args.pdf if args.pdf.is_absolute() else repo_root / args.pdf
        try:
            generate_pdf_report(pdf_path, results, plan, args.jitlog_mode)
            print(f"PDF graph report written to {pdf_path}")
        except RuntimeError as exc:
            print(f"Error: {exc}", file=sys.stderr)
            return 1

    if args.chart:
        from plot_shootout import generate_bar_chart

        chart_path = args.chart if args.chart.is_absolute() else repo_root / args.chart
        try:
            generate_bar_chart(chart_path, results, plan, caption=f"commit {revision}")
            print(f"Bar chart written to {chart_path}")
        except RuntimeError as exc:
            print(f"Error: {exc}", file=sys.stderr)
            return 1

    if args.curve_chart:
        from plot_shootout import generate_curve_chart

        curve_path = args.curve_chart if args.curve_chart.is_absolute() else repo_root / args.curve_chart
        try:
            generate_curve_chart(curve_path, results, plan, caption=f"commit {revision}")
            print(f"Warm-up curve chart written to {curve_path}")
        except RuntimeError as exc:
            print(f"Error: {exc}", file=sys.stderr)
            return 1

    if args.json:
        summary = {
            "compare": plan.preset,
            "iterations": args.iterations,
            "configs": [
                {"id": config_id, "cmd": cmd, "label": label}
                for config_id, cmd, label in plan.configs
            ],
            "jitlog": args.jitlog,
            "jitlog_mode": args.jitlog_mode,
            "benchmarks": [
                {
                    "name": r.name,
                    "config": r.config,
                    "status": r.status,
                    "returncode": r.returncode,
                    "wall_seconds": r.wall_seconds,
                    "result_value": r.result_value,
                    "elapsed_usec": r.elapsed_usec,
                    "curve_times": r.curve_times,
                    "error_message": r.error_message,
                    "jitlog_path": str(r.jitlog_path) if r.jitlog_path else None,
                    "run_count": r.run_count,
                    "elapsed_samples": r.elapsed_samples,
                }
                for r in results
            ],
        }
        json_path = args.json if args.json.is_absolute() else repo_root / args.json
        json_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
        print(f"JSON summary written to {json_path}")

    return 0 if all(r.status in ("ok", "warning") for r in results) else 1


if __name__ == "__main__":
    sys.exit(main())
