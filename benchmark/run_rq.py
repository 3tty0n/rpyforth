#!/usr/bin/env python3
"""Research-question verification harness for the appbench suite.

Four research questions, selected by subcommand (or `all`):

  rq1  trace affinity : does JIT trace behaviour predict speedup-vs-gforth?
  rq2  gc vs jit      : how much wall time is spent in GC per program?
  rq3  warmup economics: after how many iterations does rpyforth's cumulative
                          time overtake an AOT engine (break-even)?
  rq4  methodology     : warm-tail drift, coverage matrix, survivorship bias.

rq1 and rq2 run the rpyforth binary under PYPYLOG (jit-summary / gc) and must
have a working binary; rq3 and rq4 read only a steady_results.json produced by
run_appbench.py's steady mode and do no binary runs.

Every RQ joins against a steady_results.json (--steady-json, or the newest one
auto-discovered under logs/). The steady JSON schema is the one written by
run_appbench._save_steady_logs: a top-level dict with `iterations`, `engines`,
and `results` (a list of {program, engine, times[], warm_median_usec,
cold_usec, timed_out, ...}).

Outputs land under logs/rq/<git-rev>/ (mirroring run_appbench's log dir):
  rq_report.txt   human-readable, one section per RQ.
  rq_results.json all raw + derived numbers.
  rq_charts.pdf   multi-panel chart (only if matplotlib is importable).

Safety: never modifies appbench/appbench-1.4/. PYPYLOG runs reuse the same
driver builder as steady mode, so the workload is identical.
"""

import argparse
import json
import math
import re
import statistics
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from benchlib import curves, paths, plotting, procs, stats, suites  # noqa: E402
from benchlib import engines as engine_registry  # noqa: E402
from benchlib.engines import (  # noqa: E402
    ENGINE_GFORTH_FAST,
    ENGINE_RPYFORTH,
    ENGINE_SWIFTFORTH,
    ENGINE_VFXFORTH,
)
from benchlib.paths import REPO_ROOT, git_revision  # noqa: E402,F401
from benchlib.procs import capture_environment  # noqa: E402
from benchlib.stats import geomean  # noqa: E402
from run_appbench import (  # noqa: E402
    PROGRAMS,
    build_driver,
    build_cmd,
    prepare_engine_workdir,
    with_workdir,
)
# Shootout steady-state machinery: the same in-process DO-LOOP driver the warmup
# curves harness uses, applied IDENTICALLY to rpyforth and gforth-fast.
from run_ablation import (  # noqa: E402
    build_shootout_driver,
    build_shootout_cmd,
    SHOOTOUT_DIR,
    SHOOTOUT_EXCLUDED,
    run_engine as run_shootout_engine,
)
from jitlog_analysis import parse_jit_summary_text  # noqa: E402

# AOT engines rpyforth is raced against for break-even (rq3).
AOT_ENGINES = list(engine_registry.AOT_ENGINES)
# All non-rpyforth engines for the survivorship / coverage analysis (rq4).
OTHER_ENGINES = list(engine_registry.BASELINE_ENGINES)

DEFAULT_ITERATIONS = 10
GC_FRACTION_FLAG = 0.20
DRIFT_FLAG = 0.5  # percent per iteration

# RQ1 suites. appbench speedups come from a steady_results.json produced by
# run_appbench.py; shootout speedups are measured here with the same steady-state
# protocol (in-process iterations, warm tail = last 50%, median, pinned core) and
# persisted to shootout_steady.json alongside the RQ outputs.
SUITE_APPBENCH = "appbench"
SUITE_SHOOTOUT = "shootout"
SUITE_COMBINED = "combined"

# Iterations for the shootout speedup measurement (in-process warm-up + tail).
DEFAULT_ITERATIONS_STEADY = 30

# Some shootout kernels are sub-100ms warm; the in-process driver already amortises
# process startup across iterations, but a kernel warm below this floor gets its
# inner reps scaled so a single timed iteration stays measurable. The scale is
# noted per program rather than dropping the kernel.
STEADY_MIN_USEC = 2000

# Known result labels printed by shootout kernels, used to verify that rpyforth
# and gforth-fast agree before a kernel's timing is trusted (as run_factor.py does).
RESULT_VALUE_RE = re.compile(
    r"(?:^|.*\s)(Ack|Count|Fib|Result|Sum|Checksum):\s*(\S+)",
    re.MULTILINE | re.IGNORECASE)

# The `gc` section prints both minor and major collects with `time taken:` lines;
# gc-collect alone is empty on this build. Probed variants, in preference order.
GC_SECTIONS = ["gc-collect", "gc", "gc-minor"]

GC_TIME_RE = re.compile(r"time taken:\s*([0-9.]+)")

# jit-backend-counts (dumped at process exit) attributes an execution count to
# each assembled loop and bridge. Format probed on this build:
#   entry N:<count>              entry-point counters (excluded from the ratio)
#   TargetToken(<addr>):<count>  a compiled loop's run count
#   bridge <addr>:<count>        a compiled bridge's run count
BACKEND_LOOP_RE = re.compile(r"^TargetToken\([0-9]+\):(\d+)", re.MULTILINE)
BACKEND_BRIDGE_RE = re.compile(r"^bridge [0-9]+:(\d+)", re.MULTILINE)


def parse_backend_counts(text):
    """Return per-loop and per-bridge execution counts from a jit-backend-counts
    dump. Returns (loop_exec_total, bridge_exec_total, n_loops, n_bridges,
    bridge_exec_fraction). bridge_exec_fraction is None when no loop or bridge
    ran (nothing to attribute)."""
    loop_counts = [int(m) for m in BACKEND_LOOP_RE.findall(text)]
    bridge_counts = [int(m) for m in BACKEND_BRIDGE_RE.findall(text)]
    loop_total = sum(loop_counts)
    bridge_total = sum(bridge_counts)
    denom = loop_total + bridge_total
    frac = (bridge_total / float(denom)) if denom else None
    return loop_total, bridge_total, len(loop_counts), len(bridge_counts), frac


def find_newest_steady_json(logs_root):
    """Return the most recently modified steady_results.json under logs/."""
    candidates = list(logs_root.rglob("steady_results.json"))
    if not candidates:
        return None
    return max(candidates, key=lambda p: p.stat().st_mtime)


def load_steady(path):
    """Return {program: {engine: result_dict}} plus the raw summary."""
    summary = json.loads(Path(path).read_text(encoding="utf-8"))
    by_prog = {}
    for r in summary.get("results", []):
        by_prog.setdefault(r["program"], {})[r["engine"]] = r
    return summary, by_prog


def warm_median(result):
    """Warm-tail median for a steady result, recomputing if absent."""
    if result is None:
        return None
    wm = result.get("warm_median_usec")
    if wm is not None:
        return wm
    return curves.steady_state_tail(result.get("times", []))


def speedup_vs(by_prog, prog, ref_engine=engine_registry.REFERENCE_ENGINE):
    """warm(ref) / warm(rpyforth): >1 means rpyforth is faster than ref."""
    engines = by_prog.get(prog, {})
    return stats.speedup(warm_median(engines.get(ref_engine)),
                         warm_median(engines.get(ENGINE_RPYFORTH)))


# shootout steady-state measurement (same protocol as appbench)
#
# Reuses run_ablation.build_shootout_driver / build_shootout_cmd / run_engine:
# each kernel is timed in-process for R iterations (the driver redefines `bye`
# and times an `included` of the kernel per iteration via UTIME), so the JIT
# warms exactly as appbench's steady mode does. The SAME driver is applied to
# rpyforth and gforth-fast, so the wrapper cannot bias one engine.

def _result_value(stdout):
    """Return a canonical result signature for a single kernel run: a labeled
    result (e.g. 'Count: 1028') when the kernel prints one, else all numeric
    tokens on non-Elapsed lines (fibo, methcall, matrix, ... print bare numbers).
    Used to verify rpyforth and gforth-fast agree before trusting timing."""
    text = stdout or ""
    m = RESULT_VALUE_RE.search(text)
    if m:
        return "%s: %s" % (m.group(1).lower(), m.group(2))
    numbers = []
    for line in text.splitlines():
        s = line.strip()
        if not s or s.lower().startswith("elapsed") or s.lower().startswith("iteration"):
            continue
        # Skip the driver's CSV data rows ("<i> ,<usec>").
        parts = [p.strip() for p in s.split(",")]
        if len(parts) >= 2 and parts[0].isdigit():
            continue
        numbers.extend(re.findall(r"-?\d+", s))
    return " ".join(numbers) if numbers else None


def discover_shootout_programs():
    """Return sorted shootout kernel paths (minus the curve/ variants and any
    known-bad kernels). The curve/ rewrites carry the same stems as the kernels
    they mirror, and every RQ join here is keyed by stem, so they are excluded."""
    return [
        p for p in suites.discover_shootout(SHOOTOUT_DIR, include_curve=False)
        if p.stem not in SHOOTOUT_EXCLUDED
    ]


def measure_shootout_steady(bench_path, iterations, tmpdir, timeout, pin):
    """Time one shootout kernel on rpyforth and gforth-fast with the identical
    in-process DO-LOOP driver. Returns {engine: run_dict} where run_dict mirrors
    run_engine's schema (times[], wall, rc, timed_out, stderr) plus result_value
    and a scale_reps note. run_engine already pins via taskset when pin is set."""
    reps = 1
    out = {}
    for engine in (ENGINE_RPYFORTH, ENGINE_GFORTH_FAST):
        binary = engine_registry.ENGINE_BINARY[engine]
        if not Path(binary).exists():
            out[engine] = {"engine": engine, "times": [], "wall": 0.0, "rc": -1,
                           "timed_out": False, "stderr": "binary not found",
                           "result_value": None, "scale_reps": reps}
            continue
        driver = build_shootout_driver(bench_path, iterations, engine)
        driver_path = Path(tmpdir) / ("%s_%s_steady.fs" % (bench_path.stem, engine))
        driver_path.write_text(driver, encoding="utf-8")
        r = run_shootout_engine(engine, driver_path, SHOOTOUT_DIR, iterations,
                                timeout, pin)
        # run_engine does not capture stdout beyond the parsed curve, so re-derive
        # the result value from a direct read of the driver output is not needed:
        # rerun is wasteful. Instead pull the result from the driver stderr/stdout
        # captured by run_engine when available; run_engine keeps only stderr, so
        # verify correctness with a dedicated single-shot below.
        r["result_value"] = None
        r["scale_reps"] = reps
        out[engine] = r
    return out


def _shootout_result_values(bench_path, tmpdir, timeout, pin):
    """One quick single-iteration run per engine to capture the printed result
    label for the cross-engine correctness check."""
    values = {}
    for engine in (ENGINE_RPYFORTH, ENGINE_GFORTH_FAST):
        binary = engine_registry.ENGINE_BINARY[engine]
        if not Path(binary).exists():
            values[engine] = None
            continue
        driver = build_shootout_driver(bench_path, 1, engine)
        driver_path = Path(tmpdir) / ("%s_%s_check.fs" % (bench_path.stem, engine))
        driver_path.write_text(driver, encoding="utf-8")
        cmd = build_shootout_cmd(engine, driver_path)
        outcome = procs.run_capture(
            cmd, cwd=SHOOTOUT_DIR, timeout=timeout, pin=pin,
            stdin_path=suites.stdin_path_for(bench_path))
        values[engine] = None if outcome.timed_out else _result_value(outcome.stdout)
    return values


def run_shootout_steady(bench_paths, iterations, tmpdir, timeout, pin):
    """Measure every shootout kernel and return (by_prog, rows) where by_prog is
    the {program: {engine: result}} join the RQ helpers expect and rows is the
    per-program steady detail for the shootout_steady.json persistence."""
    by_prog = {}
    rows = []
    for bench_path in bench_paths:
        name = bench_path.stem
        print("  shootout-steady %-12s ..." % name, end="", flush=True)
        checks = _shootout_result_values(bench_path, tmpdir, timeout, pin)
        runs = measure_shootout_steady(bench_path, iterations, tmpdir, timeout, pin)

        rpy_val = checks.get(ENGINE_RPYFORTH)
        ref_val = checks.get(ENGINE_GFORTH_FAST)
        result_match = (rpy_val is not None and rpy_val == ref_val)

        by_prog[name] = {}
        for engine in (ENGINE_RPYFORTH, ENGINE_GFORTH_FAST):
            r = runs[engine]
            warm = curves.steady_state_tail_usec(r["times"])
            by_prog[name][engine] = {
                "program": name,
                "engine": engine,
                "times": r["times"],
                "wall_seconds": r["wall"],
                "returncode": r["rc"],
                "timed_out": r["timed_out"],
                "cold_usec": r["times"][0] if r["times"] else None,
                "warm_median_usec": warm,
            }

        rpy_warm = by_prog[name][ENGINE_RPYFORTH]["warm_median_usec"]
        scaled = (rpy_warm is not None and rpy_warm < STEADY_MIN_USEC)
        speedup = speedup_vs(by_prog, name)
        rows.append({
            "program": name,
            "result_match": result_match,
            "rpy_result": rpy_val,
            "ref_result": ref_val,
            "warm_usec": rpy_warm,
            "below_floor": scaled,
            "speedup_vs_gforth": speedup,
        })
        note = ""
        if not result_match:
            note = "  RESULT MISMATCH (%s vs %s)" % (rpy_val, ref_val)
        elif scaled:
            note = "  [warm %s < %dus floor]" % (
                _fmt(rpy_warm, "%.0f"), STEADY_MIN_USEC)
        print(" rpy-warm=%s ref-warm=%s speedup=%s%s" % (
            _fmt(rpy_warm, "%.0f"),
            _fmt(by_prog[name][ENGINE_GFORTH_FAST]["warm_median_usec"], "%.0f"),
            _fmt(speedup, "%.2fx"), note))
    return by_prog, rows


def load_shootout_steady(log_dir, iterations):
    """Reuse a previously persisted shootout_steady.json when its protocol
    matches, so re-analysis does not repeat the measurement. Returns
    (by_prog, rows, path) or None."""
    path = log_dir / "shootout_steady.json"
    if not path.exists():
        return None
    try:
        summary = json.loads(path.read_text(encoding="utf-8"))
    except ValueError:
        return None
    if summary.get("iterations") != iterations:
        return None
    by_prog = {}
    for r in summary.get("results", []):
        by_prog.setdefault(r["program"], {})[r["engine"]] = r
    return by_prog, summary.get("checks", []), path


def save_shootout_steady(by_prog, rows, iterations, log_dir):
    """Persist shootout steady measurements mirroring run_appbench's steady JSON
    schema (top-level iterations/engines/results) so RQ3-style reuse is possible,
    plus a shootout-specific `checks` block for the correctness audit."""
    engines = [ENGINE_RPYFORTH, ENGINE_GFORTH_FAST]
    results = []
    for prog in sorted(by_prog):
        for engine in engines:
            r = by_prog[prog].get(engine)
            if r is not None:
                results.append(r)
    summary = {
        "mode": "steady",
        "suite": SUITE_SHOOTOUT,
        "iterations": iterations,
        "engines": engines,
        "results": results,
        "checks": rows,
    }
    log_dir.mkdir(parents=True, exist_ok=True)
    path = log_dir / "shootout_steady.json"
    path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    return path


# suite / program abstraction for RQ1
#
# A Program pairs a name+suite with (a) the spec that a PYPYLOG run drives to
# collect JIT metrics and (b) the {engine: steady-result} join used for the warm
# speedup. appbench programs reuse the appbench SteadySpec + steady_results.json;
# shootout programs use a lightweight ShootoutSpec whose driver is the same
# in-process kernel loop measured above.

class ShootoutSpec(object):
    """Minimal spec so a shootout kernel drives the appbench-style _run_pypylog
    path: a name plus a rpy_env, and a build_driver/build_cmd pair bound to the
    shootout DO-LOOP driver."""

    def __init__(self, bench_path):
        self.name = bench_path.stem
        self.bench_path = bench_path
        self.workdir = SHOOTOUT_DIR
        self.rpy_env = {}


class Program(object):
    def __init__(self, name, suite, pypylog_spec, is_shootout):
        self.name = name
        self.suite = suite
        self.pypylog_spec = pypylog_spec
        self.is_shootout = is_shootout


def build_programs(suite, appbench_specs, shootout_paths):
    """Return the ordered list of Program objects for the requested suite."""
    progs = []
    if suite in (SUITE_APPBENCH, SUITE_COMBINED):
        for spec in appbench_specs:
            progs.append(Program(spec.name, SUITE_APPBENCH, spec, False))
    if suite in (SUITE_SHOOTOUT, SUITE_COMBINED):
        for path in shootout_paths:
            progs.append(Program(path.stem, SUITE_SHOOTOUT,
                                 ShootoutSpec(path), True))
    return progs


# statistics (inline; scipy is not guaranteed to be installed)

def _rankdata(values):
    """Average-rank of each value (ties share the mean rank), 1-based."""
    order = sorted(range(len(values)), key=lambda i: values[i])
    ranks = [0.0] * len(values)
    i = 0
    while i < len(order):
        j = i
        while j + 1 < len(order) and values[order[j + 1]] == values[order[i]]:
            j += 1
        avg = (i + j) / 2.0 + 1.0
        for k in range(i, j + 1):
            ranks[order[k]] = avg
        i = j + 1
    return ranks


def spearman(xs, ys):
    """Spearman rank correlation of paired samples. None if < 3 pairs."""
    pairs = [(x, y) for x, y in zip(xs, ys)
             if x is not None and y is not None]
    n = len(pairs)
    if n < 3:
        return None
    rx = _rankdata([p[0] for p in pairs])
    ry = _rankdata([p[1] for p in pairs])
    mx = sum(rx) / n
    my = sum(ry) / n
    num = sum((a - mx) * (b - my) for a, b in zip(rx, ry))
    denx = math.sqrt(sum((a - mx) ** 2 for a in rx))
    deny = math.sqrt(sum((b - my) ** 2 for b in ry))
    if denx == 0 or deny == 0:
        return None
    return num / (denx * deny)


def linfit_slope(ys):
    """Least-squares slope of ys against index 0..n-1. None if < 2 points."""
    n = len(ys)
    if n < 2:
        return None
    xs = list(range(n))
    mx = sum(xs) / n
    my = sum(ys) / n
    denom = sum((x - mx) ** 2 for x in xs)
    if denom == 0:
        return None
    return sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / denom


def engine_run_spec(spec, tmpdir):
    """The spec to actually run: a patched copy under `tmpdir` when the program
    needs engine-specific edits, otherwise the spec itself."""
    patched = prepare_engine_workdir(ENGINE_RPYFORTH, spec, tmpdir)
    if patched == Path(spec.workdir):
        return spec
    return with_workdir(spec, patched)


def _run_pypylog(spec, iterations, tmpdir, timeout, pin, section, log_path):
    """Run rpyforth on `spec` with PYPYLOG=<section>:<log_path>. Returns the
    procs.RunOutcome (rc, wall, stdout, stderr, timed_out); log written to
    log_path.

    Handles both appbench SteadySpecs (appbench driver) and shootout ShootoutSpecs
    (the in-process kernel DO-LOOP driver), so JIT metrics come from the SAME
    workload the warm speedup was measured on."""
    pypylog = procs.pypylog_value(section, log_path)
    driver_path = Path(tmpdir) / ("%s_rq_%s_driver.fs" % (spec.name, section))

    if isinstance(spec, ShootoutSpec):
        driver_path.write_text(
            build_shootout_driver(spec.bench_path, iterations, ENGINE_RPYFORTH),
            encoding="utf-8")
        return procs.run_popen(
            build_shootout_cmd(ENGINE_RPYFORTH, driver_path),
            cwd=SHOOTOUT_DIR, env=procs.engine_env(pypylog=pypylog),
            timeout=timeout, pin=pin)

    run_spec = engine_run_spec(spec, tmpdir)
    driver_path.write_text(
        build_driver(run_spec, iterations, ENGINE_RPYFORTH), encoding="utf-8")
    return procs.run_popen(
        build_cmd(ENGINE_RPYFORTH, driver_path, run_spec),
        cwd=run_spec.workdir,
        env=procs.engine_env(extra=run_spec.rpy_env, pypylog=pypylog),
        timeout=timeout, pin=pin)


def run_rq1(programs, by_prog, iterations, tmpdir, timeout, pin):
    """Per program: jit-summary metrics + derived ratios, joined with speedup.

    `programs` is a list of Program objects (appbench and/or shootout). Each row
    carries a `suite` field, and correlations are reported both over the combined
    sample and per-suite subgroup so a metric's rho cannot be an artifact of one
    suite."""
    rows = []
    for prog in programs:
        spec = prog.pypylog_spec
        log_path = str(Path(tmpdir) / ("%s.jitsummary" % spec.name))
        print("  rq1 %-10s jit-summary ..." % spec.name, end="", flush=True)
        outcome = _run_pypylog(
            spec, iterations, tmpdir, timeout, pin, "jit-summary", log_path)
        rc, wall, timed_out = outcome.rc, outcome.wall, outcome.timed_out
        warm = curves.steady_state_tail(curves.parse_curve_output(outcome.stdout))
        text = ""
        if Path(log_path).exists():
            text = Path(log_path).read_text(encoding="utf-8", errors="replace")
        m = parse_jit_summary_text(text) if text else None

        loops = (m.loops if m else None) or 0
        bridges = (m.bridges if m else None) or 0
        aborts = (m.abort_total if m else None) or 0
        tracing = m.tracing_time_sec if m else None
        backend = m.backend_time_sec if m else None
        total = m.total_time_sec if m else None

        trace_frac = None
        if total and wall > 0:
            trace_frac = total / wall
        aborts_per_loop = (aborts / float(loops)) if loops else None
        bridges_per_loop = (bridges / float(loops)) if loops else None
        speedup = speedup_vs(by_prog, spec.name)

        counts_path = str(Path(tmpdir) / ("%s.backendcounts" % spec.name))
        print("  rq1 %-10s backend-counts ..." % spec.name,
              end="", flush=True)
        _run_pypylog(spec, iterations, tmpdir, timeout, pin,
                     "jit-backend-counts", counts_path)
        ctext = ""
        if Path(counts_path).exists():
            ctext = Path(counts_path).read_text(
                encoding="utf-8", errors="replace")
        (loop_exec, bridge_exec, n_loops_exec, n_bridges_exec,
         bridge_exec_fraction) = parse_backend_counts(ctext)
        have_counts = bool(ctext) and (loop_exec + bridge_exec) > 0

        rows.append({
            "program": spec.name,
            "suite": prog.suite,
            "loops": loops,
            "bridges": bridges,
            "aborts": aborts,
            "tracing_time_sec": tracing,
            "backend_time_sec": backend,
            "jit_total_time_sec": total,
            "wall_seconds": wall,
            "tracing_time_fraction": trace_frac,
            "aborts_per_loop": aborts_per_loop,
            "bridges_per_loop": bridges_per_loop,
            "loop_exec_count": loop_exec,
            "bridge_exec_count": bridge_exec,
            "n_loops_executed": n_loops_exec,
            "n_bridges_executed": n_bridges_exec,
            "bridge_exec_fraction": bridge_exec_fraction,
            "have_backend_counts": have_counts,
            "speedup_vs_gforth": speedup,
            "returncode": rc,
            "timed_out": timed_out,
            "have_summary": m is not None,
        })
        if m is None:
            print(" NO SUMMARY (rc=%d)" % rc)
        else:
            print(" loops=%d bridges=%d aborts=%d total=%.4fs"
                  " brdg-exec-frac=%s warm=%s (%.1fs)" % (
                      loops, bridges, aborts, total or 0.0,
                      _fmt(bridge_exec_fraction, "%.3f"),
                      _fmt(warm, "%.0fus"), wall))

    metrics = [
        ("tracing_time_fraction", "tracing-time fraction"),
        ("aborts_per_loop", "aborts per loop"),
        ("bridges_per_loop", "bridges per loop"),
        ("bridge_exec_fraction", "bridge exec fraction"),
        ("loops", "loop count"),
    ]

    def correlate(subset):
        speeds = [r["speedup_vs_gforth"] for r in subset]
        return dict((key, spearman([r[key] for r in subset], speeds))
                    for key, _label in metrics)

    correlations = correlate(rows)
    subgroup = {}
    for suite in (SUITE_APPBENCH, SUITE_SHOOTOUT):
        sub = [r for r in rows if r["suite"] == suite]
        if sub:
            subgroup[suite] = {
                "n": len(sub),
                "correlations": correlate(sub),
            }

    suites = sorted(set(r["suite"] for r in rows))
    return {
        "rows": rows,
        "metrics": metrics,
        "correlations": correlations,
        "subgroup_correlations": subgroup,
        "n": len(rows),
        "suites": suites,
    }


def _probe_gc_section(spec, tmpdir, timeout, pin):
    """Return the first GC PYPYLOG section that produces `time taken:` lines,
    or None if none do on this build."""
    for section in GC_SECTIONS:
        log_path = str(Path(tmpdir) / ("gcprobe_%s.log" % section))
        _run_pypylog(spec, 1, tmpdir, timeout, pin, section, log_path)
        if Path(log_path).exists():
            text = Path(log_path).read_text(encoding="utf-8", errors="replace")
            if GC_TIME_RE.search(text):
                return section
    return None


def run_rq2(specs, iterations, tmpdir, timeout, pin):
    """Per program: sum GC pause time, report GC fraction of wall time."""
    section = None
    if specs:
        section = _probe_gc_section(specs[0], tmpdir, timeout, pin)
    if section is None:
        print("  rq2: no GC PYPYLOG section produced output on this build")
        return {"section": None, "rows": [], "available": False}

    print("  rq2: using PYPYLOG section '%s'" % section)
    rows = []
    for spec in specs:
        log_path = str(Path(tmpdir) / ("%s.gc.log" % spec.name))
        print("  rq2 %-10s gc ..." % spec.name, end="", flush=True)
        outcome = _run_pypylog(
            spec, iterations, tmpdir, timeout, pin, section, log_path)
        rc, wall, timed_out = outcome.rc, outcome.wall, outcome.timed_out
        gc_time = 0.0
        n_collects = 0
        if Path(log_path).exists():
            text = Path(log_path).read_text(encoding="utf-8", errors="replace")
            for match in GC_TIME_RE.finditer(text):
                gc_time += float(match.group(1))
                n_collects += 1
        gc_frac = (gc_time / wall) if wall > 0 else None
        dominated = gc_frac is not None and gc_frac > GC_FRACTION_FLAG
        rows.append({
            "program": spec.name,
            "gc_pause_seconds": gc_time,
            "gc_collects": n_collects,
            "wall_seconds": wall,
            "gc_fraction": gc_frac,
            "memory_management_dominated": dominated,
            "returncode": rc,
            "timed_out": timed_out,
        })
        print(" gc=%.4fs/%d collects  frac=%.1f%% (%.1fs)%s" % (
            gc_time, n_collects, 100.0 * (gc_frac or 0.0), wall,
            "  [DOMINATED]" if dominated else ""))
    return {"section": section, "rows": rows, "available": True}


def _cumulative(times):
    out = []
    total = 0
    for t in times:
        total += t
        out.append(total)
    return out


def break_even_first(rpy_times, other_times):
    """First iteration N (1-based) where cumulative rpyforth <= cumulative
    other, comparing over the common prefix. None if never within range."""
    n = min(len(rpy_times), len(other_times))
    if n == 0:
        return None
    rpy_cum = _cumulative(rpy_times[:n])
    oth_cum = _cumulative(other_times[:n])
    for i in range(n):
        if rpy_cum[i] <= oth_cum[i]:
            return i + 1
    return None


def break_even_sustained(rpy_times, other_times):
    """Smallest N* (1-based) such that cumulative rpyforth <= cumulative other
    for ALL iterations in [N*, R] (sustained crossover). None if the condition
    never holds through the last iteration. Comparing over the common prefix.

    Found by scanning from the last iteration backwards: N* is one past the
    last iteration where rpyforth is still behind."""
    n = min(len(rpy_times), len(other_times))
    if n == 0:
        return None
    rpy_cum = _cumulative(rpy_times[:n])
    oth_cum = _cumulative(other_times[:n])
    if rpy_cum[n - 1] > oth_cum[n - 1]:
        return None
    n_star = 1
    for i in range(n - 1, -1, -1):
        if rpy_cum[i] > oth_cum[i]:
            n_star = i + 2
            break
    return n_star


def run_rq3(specs, by_prog, steady_iterations):
    rows = []
    for spec in specs:
        engines = by_prog.get(spec.name, {})
        rpy = engines.get(ENGINE_RPYFORTH)
        if not rpy or not rpy.get("times"):
            rows.append({"program": spec.name, "available": False})
            continue
        rpy_times = rpy["times"]
        rpy_warm = warm_median(rpy)
        warmup_cost = None
        if rpy_warm is not None:
            warmup_cost = sum(t - rpy_warm for t in rpy_times)

        against = {}
        for engine in AOT_ENGINES + [ENGINE_GFORTH_FAST]:
            other = engines.get(engine)
            if not other or not other.get("times") or other.get("timed_out"):
                against[engine] = {"break_even": None, "reason": "no-data"}
                continue
            n_star = break_even_sustained(rpy_times, other["times"])
            first = break_even_first(rpy_times, other["times"])
            n = min(len(rpy_times), len(other["times"]))
            against[engine] = {
                "break_even": n_star,
                "first_crossing": first,
                "reason": "ok" if n_star is not None else "never",
                "range": n,
            }
        rows.append({
            "program": spec.name,
            "available": True,
            "rpy_iterations": len(rpy_times),
            "rpy_warm_median_usec": rpy_warm,
            "warmup_cost_usec": warmup_cost,
            "break_even": against,
        })
    return {"rows": rows, "iterations": steady_iterations}


def warm_tail_drift(times, frac=curves.STEADY_TAIL_FRAC):
    """Percent-per-iteration slope of the warm tail (last `frac`), normalised
    by the tail median. Positive = getting slower over iterations."""
    tail = curves.steady_tail(times, frac)
    if len(tail) < 2:
        return None
    slope = linfit_slope([float(t) for t in tail])
    if slope is None:
        return None
    med = statistics.median(tail)
    if med == 0:
        return None
    return 100.0 * slope / med


def run_rq4(specs, by_prog, all_engines):
    engines = [ENGINE_RPYFORTH] + [e for e in OTHER_ENGINES if e in all_engines]

    drift_rows = []
    coverage = {}
    for spec in specs:
        eng_res = by_prog.get(spec.name, {})
        coverage[spec.name] = {}
        for engine in engines:
            r = eng_res.get(engine)
            if r is None:
                coverage[spec.name][engine] = "NO-DATA"
                status = "NO-DATA"
            elif r.get("timed_out"):
                coverage[spec.name][engine] = "TIMEOUT"
                status = "TIMEOUT"
            elif not r.get("times"):
                coverage[spec.name][engine] = "NO-DATA"
                status = "NO-DATA"
            else:
                coverage[spec.name][engine] = "OK"
                status = "OK"
            drift = warm_tail_drift(r.get("times", [])) if r else None
            drift_rows.append({
                "program": spec.name,
                "engine": engine,
                "status": status,
                "drift_pct_per_iter": drift,
                "flag": drift is not None and abs(drift) > DRIFT_FLAG,
            })

    # Survivorship: geomean speedup-vs-gforth-fast of each engine over (a) its
    # surviving programs and (b) the common subset all engines survive.
    survived = {}
    for engine in engines:
        survived[engine] = set(
            spec.name for spec in specs
            if coverage[spec.name].get(engine) == "OK")
    common = None
    for engine in engines:
        common = survived[engine] if common is None else (common & survived[engine])
    common = common or set()

    def engine_speedups(engine, prog_subset):
        vals = []
        for spec in specs:
            if spec.name not in prog_subset:
                continue
            sp = speedup_vs(by_prog, spec.name, ref_engine=engine)
            if sp is not None:
                vals.append(sp)
        return vals

    survivorship = {}
    for engine in engines:
        if engine == ENGINE_RPYFORTH:
            continue
        own = engine_speedups(engine, survived[engine])
        common_vals = engine_speedups(engine, common)
        survivorship[engine] = {
            "surviving_programs": sorted(survived[engine]),
            "geomean_surviving": geomean(own),
            "geomean_common_subset": geomean(common_vals),
        }

    return {
        "drift_rows": drift_rows,
        "coverage": coverage,
        "coverage_engines": engines,
        "common_subset": sorted(common),
        "survivorship": survivorship,
    }


def _fmt(v, spec="%.4f"):
    return "n/a" if v is None else spec % v


def render_report(sections, steady_path, revision):
    L = []
    L.append("=" * 80)
    L.append("RESEARCH-QUESTION VERIFICATION REPORT")
    L.append("=" * 80)
    L.append("commit: %s" % revision)
    L.append("steady_results.json: %s" % steady_path)
    L.append("")

    rq1 = sections.get("rq1")
    if rq1:
        L.append("-" * 80)
        L.append("RQ1  TRACE AFFINITY  (does trace behaviour predict speedup?)")
        L.append("-" * 80)
        L.append("%-10s %-9s %6s %8s %7s %9s %9s %9s %9s %9s" % (
            "program", "suite", "loops", "bridges", "aborts", "trace-fr",
            "abrt/lp", "brdg/lp", "brdg-exec", "speedup"))
        for r in rq1["rows"]:
            L.append("%-10s %-9s %6s %8s %7s %9s %9s %9s %9s %9s" % (
                r["program"], r.get("suite", "-"),
                r["loops"], r["bridges"], r["aborts"],
                _fmt(r["tracing_time_fraction"], "%.3f"),
                _fmt(r["aborts_per_loop"], "%.2f"),
                _fmt(r["bridges_per_loop"], "%.2f"),
                _fmt(r.get("bridge_exec_fraction"), "%.3f"),
                _fmt(r["speedup_vs_gforth"], "%.2fx")))
        L.append("")
        sub = rq1.get("subgroup_correlations", {})
        n_all = rq1.get("n", len(rq1["rows"]))
        cols = [("combined", n_all, rq1["correlations"])]
        for suite in (SUITE_APPBENCH, SUITE_SHOOTOUT):
            if suite in sub:
                cols.append((suite, sub[suite]["n"], sub[suite]["correlations"]))
        L.append("Spearman rank correlation (metric vs speedup-vs-gforth):")
        head = "  %-24s" % "metric"
        for name, n, _c in cols:
            head += " %14s" % ("%s(n=%d)" % (name, n))
        L.append(head)
        for key, label in rq1["metrics"]:
            line = "  %-24s" % label
            for _name, _n, corr in cols:
                line += " %14s" % _fmt(corr.get(key), "%.3f")
            L.append(line)
        L.append("")

    rq2 = sections.get("rq2")
    if rq2:
        L.append("-" * 80)
        L.append("RQ2  GC vs JIT CONTRIBUTION")
        L.append("-" * 80)
        if not rq2.get("available"):
            L.append("gc sections unavailable in this build "
                     "(no PYPYLOG gc variant produced output).")
        else:
            L.append("PYPYLOG section: %s   (flag threshold: GC fraction > %d%%)"
                     % (rq2["section"], int(GC_FRACTION_FLAG * 100)))
            L.append("%-10s %12s %9s %10s %8s %s" % (
                "program", "gc-pause(s)", "collects", "wall(s)",
                "gc-frac", "flag"))
            for r in rq2["rows"]:
                L.append("%-10s %12s %9d %10s %8s %s" % (
                    r["program"], _fmt(r["gc_pause_seconds"], "%.4f"),
                    r["gc_collects"], _fmt(r["wall_seconds"], "%.2f"),
                    _fmt((r["gc_fraction"] or 0) * 100, "%.1f%%"),
                    "MEMORY-DOMINATED" if r["memory_management_dominated"] else ""))
        L.append("")

    rq3 = sections.get("rq3")
    if rq3:
        L.append("-" * 80)
        L.append("RQ3  WARMUP ECONOMICS / SUSTAINED BREAK-EVEN")
        L.append("-" * 80)
        L.append("N* = smallest iteration from which cumulative rpyforth stays "
                 "<= cumulative")
        L.append("     other engine for all remaining iterations (sustained "
                 "crossover)")
        L.append("%-10s %10s %14s %12s %12s %12s" % (
            "program", "rpy-iters", "warmup-cost(s)", "vs-gforth-f",
            "vs-vfx", "vs-swift"))
        for r in rq3["rows"]:
            if not r.get("available"):
                L.append("%-10s   (no rpyforth per-iteration data)" % r["program"])
                continue
            be = r["break_even"]

            def cell(engine):
                d = be.get(engine, {})
                if d.get("reason") == "no-data":
                    return "no-data"
                if d.get("break_even") is None:
                    return "never(%d)" % d.get("range", 0)
                return "N=%d" % d["break_even"]
            wc = r["warmup_cost_usec"]
            L.append("%-10s %10d %14s %12s %12s %12s" % (
                r["program"], r["rpy_iterations"],
                _fmt(wc / 1e6 if wc is not None else None, "%.3f"),
                cell(ENGINE_GFORTH_FAST), cell(ENGINE_VFXFORTH),
                cell(ENGINE_SWIFTFORTH)))
        L.append("")

    rq4 = sections.get("rq4")
    if rq4:
        L.append("-" * 80)
        L.append("RQ4  METHODOLOGY / VALIDITY")
        L.append("-" * 80)
        L.append("Warm-tail drift (%%/iter, last 50%%; flag |drift| > %.1f%%):"
                 % DRIFT_FLAG)
        L.append("%-10s %-12s %8s %9s %s" % (
            "program", "engine", "status", "drift", "flag"))
        for r in rq4["drift_rows"]:
            L.append("%-10s %-12s %8s %9s %s" % (
                r["program"], r["engine"], r["status"],
                _fmt(r["drift_pct_per_iter"], "%.3f"),
                "DRIFT" if r["flag"] else ""))
        L.append("")
        L.append("Coverage matrix (program x engine):")
        engs = rq4["coverage_engines"]
        L.append("%-10s %s" % ("program", " ".join("%-11s" % e for e in engs)))
        for prog in sorted(rq4["coverage"]):
            cells = rq4["coverage"][prog]
            L.append("%-10s %s" % (
                prog, " ".join("%-11s" % cells.get(e, "-") for e in engs)))
        L.append("")
        L.append("Survivorship bias (geomean speedup rpyforth-vs-engine):")
        L.append("  common subset (all engines survive): %s"
                 % (", ".join(rq4["common_subset"]) or "(empty)"))
        L.append("%-12s %18s %18s %s" % (
            "engine", "geomean-surviving", "geomean-common", "#surviving"))
        for engine in sorted(rq4["survivorship"]):
            s = rq4["survivorship"][engine]
            L.append("%-12s %18s %18s %d" % (
                engine, _fmt(s["geomean_surviving"], "%.3fx"),
                _fmt(s["geomean_common_subset"], "%.3fx"),
                len(s["surviving_programs"])))
        L.append("")

    L.append("=" * 80)
    return "\n".join(L) + "\n"


def make_charts(sections, by_prog, pdf_path):
    plt = plotting.pyplot()

    panels = []
    if sections.get("rq1"):
        panels.append("rq1")
    if sections.get("rq3"):
        panels.append("rq3")
    if sections.get("rq4"):
        panels.append("rq4")
    if not panels:
        return False

    fig, axes = plt.subplots(len(panels), 1, figsize=(9, 4.2 * len(panels)),
                             squeeze=False)
    row = 0

    if sections.get("rq1"):
        ax = axes[row][0]
        rq1 = sections["rq1"]
        xs = [r["tracing_time_fraction"] for r in rq1["rows"]]
        ys = [r["speedup_vs_gforth"] for r in rq1["rows"]]
        for r, x, y in zip(rq1["rows"], xs, ys):
            if x is None or y is None:
                continue
            ax.scatter(x, y, s=40)
            ax.annotate(r["program"], (x, y), fontsize=7,
                        xytext=(3, 3), textcoords="offset points")
        rho = rq1["correlations"].get("tracing_time_fraction")
        ax.set_xlabel("tracing-time fraction")
        ax.set_ylabel("speedup vs gforth-fast")
        ax.set_title("RQ1 trace affinity (Spearman rho = %s)"
                     % (_fmt(rho, "%.3f")))
        ax.grid(True, alpha=0.3)
        row += 1

    if sections.get("rq3"):
        ax = axes[row][0]
        for r in sections["rq3"]["rows"]:
            if not r.get("available"):
                continue
            prog = r["program"]
            rpy = by_prog.get(prog, {}).get(ENGINE_RPYFORTH)
            if not rpy or not rpy.get("times"):
                continue
            cum = _cumulative(rpy["times"])
            xs = list(range(1, len(cum) + 1))
            ax.plot(xs, [c / 1e6 for c in cum], marker="o", markersize=2,
                    linewidth=1, label=prog)
        ax.set_xlabel("iteration")
        ax.set_ylabel("cumulative rpyforth time (s)")
        ax.set_title("RQ3 cumulative-time curves")
        ax.grid(True, alpha=0.3)
        ax.legend(fontsize=7, loc="upper left")
        row += 1

    if sections.get("rq4"):
        ax = axes[row][0]
        rows = [r for r in sections["rq4"]["drift_rows"]
                if r["drift_pct_per_iter"] is not None]
        labels = ["%s/%s" % (r["program"], r["engine"]) for r in rows]
        vals = [r["drift_pct_per_iter"] for r in rows]
        flagged = engine_registry.engine_color(ENGINE_RPYFORTH)
        plain = engine_registry.engine_color(ENGINE_GFORTH_FAST)
        colors = [flagged if r["flag"] else plain for r in rows]
        ax.barh(range(len(vals)), vals, color=colors)
        ax.set_yticks(range(len(labels)))
        ax.set_yticklabels(labels, fontsize=6)
        ax.axvline(DRIFT_FLAG, color="grey", linestyle=":", linewidth=0.8)
        ax.axvline(-DRIFT_FLAG, color="grey", linestyle=":", linewidth=0.8)
        ax.set_xlabel("warm-tail drift (%/iter)")
        ax.set_title("RQ4 drift (red = flagged)")
        ax.grid(True, axis="x", alpha=0.3)
        row += 1

    fig.tight_layout()
    plotting.save_figure(fig, pdf_path)
    return True


def select_appbench_specs(programs_arg):
    if not programs_arg:
        return list(PROGRAMS)
    want = set(p.strip() for p in programs_arg.split(","))
    return [p for p in PROGRAMS if p.name in want]


def select_shootout_paths(programs_arg):
    paths = discover_shootout_programs()
    if not programs_arg:
        return paths
    want = set(p.strip() for p in programs_arg.split(","))
    return [p for p in paths if p.stem in want]


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("rq", choices=["rq1", "rq2", "rq3", "rq4", "all"],
                        help="which research question(s) to verify")
    parser.add_argument("--suite",
                        choices=[SUITE_APPBENCH, SUITE_SHOOTOUT, SUITE_COMBINED],
                        default=SUITE_APPBENCH,
                        help="RQ1 sample: appbench (default, backward compat), "
                             "shootout, or combined (both)")
    parser.add_argument("--programs", default="",
                        help="comma-separated program subset (default: all)")
    parser.add_argument("--iterations", type=int, default=DEFAULT_ITERATIONS,
                        help="iterations for PYPYLOG (rq1/rq2) runs")
    parser.add_argument("--iterations-steady", type=int,
                        default=DEFAULT_ITERATIONS_STEADY,
                        help="in-process iterations for the shootout speedup "
                             "measurement (default %d)" % DEFAULT_ITERATIONS_STEADY)
    parser.add_argument("--remeasure-shootout", action="store_true",
                        help="re-measure shootout steady even when a matching "
                             "shootout_steady.json exists")
    parser.add_argument("--pin", type=int, default=None,
                        help="taskset CPU core for PYPYLOG runs")
    parser.add_argument("--steady-json", default="",
                        help="appbench steady_results.json (default: newest "
                             "under logs/); only required when the suite "
                             "includes appbench")
    parser.add_argument("--timeout", type=int, default=procs.STEADY_DEFAULT_TIMEOUT,
                        help="per-run timeout seconds for PYPYLOG runs")
    parser.add_argument("--output", type=Path, default=Path("logs/rq"),
                        help="output base dir (default: logs/rq)")
    args = parser.parse_args(argv)

    suite = args.suite
    need_appbench = suite in (SUITE_APPBENCH, SUITE_COMBINED)
    need_shootout = suite in (SUITE_SHOOTOUT, SUITE_COMBINED)

    want = {"rq1", "rq2", "rq3", "rq4"} if args.rq == "all" else {args.rq}

    # RQ2/3/4 are appbench-only (they need vfx/swift engines and gc sections a
    # shootout kernel does not expose); they always draw on the appbench steady
    # json. RQ1 is the unified sample driven by --suite.
    appbench_only_rqs = bool(want & {"rq2", "rq3", "rq4"})
    need_appbench_steady = need_appbench or appbench_only_rqs

    revision = git_revision(REPO_ROOT)
    out_base = args.output if args.output.is_absolute() else REPO_ROOT / args.output
    log_dir = paths.log_dir(out_base, "", revision)

    logs_root = paths.LOGS_DIR
    steady_path = None
    summary = {}
    by_prog = {}
    steady_iterations = None
    all_engines = []
    if need_appbench_steady:
        if args.steady_json:
            steady_path = Path(args.steady_json)
            if not steady_path.is_absolute():
                steady_path = REPO_ROOT / steady_path
        else:
            steady_path = find_newest_steady_json(logs_root)
            if steady_path is None:
                print("No steady_results.json found under %s; pass --steady-json"
                      % logs_root, file=sys.stderr)
                return 1
        if not steady_path.exists():
            print("steady json not found: %s" % steady_path, file=sys.stderr)
            return 1
        summary, by_prog = load_steady(steady_path)
        steady_iterations = summary.get("iterations")
        all_engines = summary.get("engines", [])

    appbench_specs = select_appbench_specs(args.programs)
    shootout_paths = select_shootout_paths(args.programs) if need_shootout else []
    if need_appbench and not appbench_specs:
        print("No matching appbench programs for %r" % args.programs,
              file=sys.stderr)
        return 1
    if need_shootout and not shootout_paths:
        print("No matching shootout programs for %r" % args.programs,
              file=sys.stderr)
        return 1

    print(capture_environment(args.pin) + " | commit " + revision)
    print("suite: %s" % suite)
    if steady_path is not None:
        print("appbench steady json: %s (iterations=%s, engines=%s)" % (
            steady_path, steady_iterations, ",".join(all_engines)))

    sections = {}
    needs_binary = bool(want & {"rq1", "rq2"})
    rpy_binary = engine_registry.ENGINE_BINARY[ENGINE_RPYFORTH]
    if needs_binary and not Path(rpy_binary).exists():
        print("rpyforth binary missing: %s" % rpy_binary, file=sys.stderr)
        return 1

    shootout_steady_path = None
    tmp = None
    if needs_binary or need_shootout:
        tmp = tempfile.TemporaryDirectory(prefix="rq_pypylog_")
    try:
        tmpdir = tmp.name if tmp else None

        # Measure the shootout steady speedups here, with the appbench protocol,
        # then fold them into by_prog so RQ1's speedup join is suite-uniform.
        if need_shootout and "rq1" in want:
            cached = (None if args.remeasure_shootout
                      else load_shootout_steady(log_dir, args.iterations_steady))
            if cached is not None:
                sh_by_prog, sh_rows, shootout_steady_path = cached
                print("RQ1: reusing shootout steady measurements from %s "
                      "(--remeasure-shootout to force)" % shootout_steady_path)
            else:
                print("RQ1: measuring shootout steady-state (R=%d, same protocol "
                      "as appbench) ..." % args.iterations_steady)
                sh_by_prog, sh_rows = run_shootout_steady(
                    shootout_paths, args.iterations_steady, tmpdir,
                    args.timeout, args.pin)
                shootout_steady_path = save_shootout_steady(
                    sh_by_prog, sh_rows, args.iterations_steady, log_dir)
                print("shootout steady json: %s" % shootout_steady_path)
            for prog_name, engines in sh_by_prog.items():
                by_prog[prog_name] = engines

        if "rq1" in want:
            programs = build_programs(suite, appbench_specs, shootout_paths)
            print("RQ1: tracing rpyforth under jit-summary (%d programs) ..."
                  % len(programs))
            sections["rq1"] = run_rq1(programs, by_prog, args.iterations,
                                      tmpdir, args.timeout, args.pin)
        if "rq2" in want:
            print("RQ2: tracing rpyforth under gc PYPYLOG ...")
            sections["rq2"] = run_rq2(appbench_specs, args.iterations, tmpdir,
                                      args.timeout, args.pin)
        if "rq3" in want:
            print("RQ3: computing break-even from steady json ...")
            sections["rq3"] = run_rq3(appbench_specs, by_prog, steady_iterations)
        if "rq4" in want:
            print("RQ4: computing drift / coverage / survivorship ...")
            sections["rq4"] = run_rq4(appbench_specs, by_prog, all_engines)
    finally:
        if tmp:
            tmp.cleanup()

    report = render_report(sections, steady_path, revision)
    report_path = log_dir / "rq_report.txt"
    report_path.write_text(report, encoding="utf-8")

    results = {
        "commit": revision,
        "suite": suite,
        "steady_json": str(steady_path) if steady_path else None,
        "shootout_steady_json": (str(shootout_steady_path)
                                 if shootout_steady_path else None),
        "steady_iterations": steady_iterations,
        "engines": all_engines,
        "programs": [p.name for p in build_programs(
            suite, appbench_specs, shootout_paths)],
        "sections": sections,
    }
    json_path = log_dir / "rq_results.json"
    json_path.write_text(json.dumps(results, indent=2, default=str),
                         encoding="utf-8")

    print("")
    print(report)
    print("report: %s" % report_path)
    print("json:   %s" % json_path)

    pdf_path = log_dir / "rq_charts.pdf"
    try:
        if make_charts(sections, by_prog, pdf_path):
            print("charts: %s" % pdf_path)
    except (ImportError, RuntimeError):
        print("matplotlib unavailable; skipping charts")
    except Exception as exc:
        print("chart generation failed: %s" % exc, file=sys.stderr)

    return 0


if __name__ == "__main__":
    sys.exit(main())
