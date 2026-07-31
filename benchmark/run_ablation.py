#!/usr/bin/env python3
"""Unified ablation-analysis harness (three subcommands).

This file consolidates three previously-separate tools into one, each preserved
verbatim as an argparse subcommand:

  measure  Paper-grade comparative-analysis harness: rpyforth (RPython/PyPy
           meta-tracing JIT) vs gforth, answering three causal questions with
           data rather than endpoint numbers. Measures every (config x benchmark)
           cell, captures PYPYLOG jit-summary for each rpyforth run, parses
           tracing/backend/loops/bridges/guards/aborts, correlates JIT internals
           with the win/loss vs gforth-fast, and writes results.json + report.md
           plus four PDF charts.

               Q1  WHY did rpyforth get fast?           -> Axis 1: rpyforth ablation ladder
               Q2  WHY does it still lose where it loses? -> Axis 3: JIT internals vs outcomes
               Q3  WHERE does gforth-fast's speed come from? -> Axis 2: gforth decomposition ladder

           Design axes
           -----------
           Axis 1 (rpyforth ablation ladder, Q1):
             rpyforth-c-naive    HEAD src, virtualize build (metastack/stkfrag OFF)
             rpyforth-c-prefix1  da61000 stkfrag (metastack+inlining; pre-FIX1)
             rpyforth-c-fix1     3f8222b stkfrag (FIX1: tid-only promotion; pre-FIX2)
             rpyforth-c-stkfrag  HEAD stkfrag (FIX1+FIX2+benchgc words; flagship)
             Ladder deltas: (naive->stkfrag)=stack representation, (prefix1->fix1)=FIX1,
                            (fix1->stkfrag)=FIX2.

           Axis 2 (gforth decomposition ladder, Q3):
             gforth-fast              full: dynamic replication + superinstructions
             gforth-fast --ss-number=0  replication only, no dynamically-formed superinsts
             gforth-fast --no-dynamic static primitives only
             gforth                   plain engine (debugging hooks)
             NOTE: gforth-fast --no-super HANGS on this build (never emits output); the
             equivalent "no superinstructions" point is --ss-number=0, used here instead.

           Axis 3 (JIT internals, Q1/Q2): jit-summary parse per rpyforth (config x benchmark).

           Usage:
             .venv/bin/python benchmark/run_ablation.py measure [--iterations 5] [--pin 3]
                               [--stages build,measure,report,chart] [--quiet-wait 3600]

  curves   All-benchmarks warm-up curve harness: shootout + appbench.
           Produces a single PDF (logs/warmup/<rev>/warmup_all.pdf by default) with
           one subplot per benchmark showing per-iteration time from cold to
           plateau for every engine. With --json, writes the warm-curve JSON
           consumed by the `render` warm chart.

  render   Ablation visualization: three PDFs showing what each ladder step
           contributed (waterfall / step-summary / vs-gforth-fast cold), and with
           --steady-json the vs-gforth-fast WARM chart, from a measure results.json.
"""

from __future__ import annotations

import argparse
import json
import math
import os
import re
import sys
import tempfile
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple

REPO_ROOT = Path(__file__).resolve().parent.parent
BENCH_DIR = REPO_ROOT / "benchmark"
sys.path.insert(0, str(BENCH_DIR))

from benchlib import curves, drivers, engines, paths, plotting, procs, stats, suites  # noqa: E402
from benchlib.drivers import write_driver  # noqa: E402
from benchlib.engines import (  # noqa: E402
    ENGINE_BINARY,
    ENGINE_GFORTH,
    ENGINE_GFORTH_FAST,
    ENGINE_RPYFORTH,
    ENGINES,
)
from benchlib.paths import SHOOTOUT_DIR  # noqa: E402,F401
# Appbench program specs and command builders come from the appbench harness.
from run_appbench import (  # noqa: E402
    build_program_registry,
    build_gforth_cmd,
    build_rpyforth_cmd,
    ProgramSpec,
    PROGRAMS as APPBENCH_PROGRAMS,
    build_driver as appbench_build_driver,
    build_cmd as appbench_build_cmd,
)

MEASURE_DEFAULT_ITERATIONS = 5
MEASURE_DEFAULT_PIN = procs.DEFAULT_PIN
MEASURE_DEFAULT_TIMEOUT = procs.DEFAULT_TIMEOUT

# Axis 1: rpyforth ablation ladder. (id, exe_name, build_kind, source_commit)
RPYFORTH_LADDER = [
    ("rpyforth-c-naive",   "rpyforth-c-naive",   "virt",    "7038abb"),
    ("rpyforth-c-prefix1", "rpyforth-c-prefix1", "stkfrag", "da61000"),
    ("rpyforth-c-fix1",    "rpyforth-c-fix1",    "stkfrag", "3f8222b"),
    ("rpyforth-c-stkfrag", "rpyforth-c-stkfrag", "stkfrag", "7038abb"),
]
RPYFORTH_FLAGSHIP = "rpyforth-c-stkfrag"

# Axis 2: gforth decomposition ladder. (id, binary, extra_flags)
GFORTH_LADDER = [
    ("gforth-fast",         ENGINE_BINARY[ENGINE_GFORTH_FAST], []),
    ("gforth-fast-nosuper", ENGINE_BINARY[ENGINE_GFORTH_FAST], ["--ss-number=0"]),
    ("gforth-fast-nodyn",   ENGINE_BINARY[ENGINE_GFORTH_FAST], ["--no-dynamic"]),
    ("gforth",              ENGINE_BINARY[ENGINE_GFORTH],      []),
]
GFORTH_REFERENCE = "gforth-fast"

# One short label per ladder id, used by the report tables, the chart legends
# and the axis labels.
LADDER_LABELS = {
    "rpyforth-c-naive":    "rpy-naive",
    "rpyforth-c-prefix1":  "rpy-prefix1",
    "rpyforth-c-fix1":     "rpy-fix1",
    "rpyforth-c-stkfrag":  "rpy-stkfrag",
    "gforth-fast":         "gf-full",
    "gforth-fast-nosuper": "gf-nosuper",
    "gforth-fast-nodyn":   "gf-nodyn",
    "gforth":              "gf-plain",
}


def ladder_label(config_id: str) -> str:
    return LADDER_LABELS.get(config_id, config_id)

SMOKE_BENCH = "sieve"
SMOKE_EXPECT = "Count: 1028"


@dataclass
class Cell:
    """One (config, benchmark) measurement."""
    config: str
    suite: str            # "shootout" | "appbench"
    bench: str
    metric: str           # "usec" (shootout self-timed) | "wall_s" (appbench cold)
    samples: List[float] = field(default_factory=list)
    median: Optional[float] = None
    ci_pct: float = 0.0
    status: str = "pending"     # ok | error | timeout | missing_binary
    detail: str = ""
    jit: Optional[Dict] = None  # parsed jit-summary (rpyforth only)


def sh(cmd: List[str], timeout=15, env=None, cwd=None) -> Tuple[int, str, str]:
    """Run a build/inspection command and return (rc, stdout, stderr)."""
    out = procs.run_capture(cmd, cwd=cwd, env=env, timeout=timeout)
    return out.rc, out.stdout, out.stderr


# ---------------------------------------------------------------------------
# Stage 0: quiesce -- wait until no competing benchmark/translation processes
# ---------------------------------------------------------------------------

def competing_processes(my_pid: int) -> List[str]:
    """List other agents' translation (pypy/rpython) or benchmark (rpyforth-c/gforth)
    processes that would skew medians. Excludes this process tree."""
    rc, out, _ = sh(["ps", "-eo", "pid,ppid,comm,args"], timeout=10)
    if rc != 0:
        return []
    hits = []
    mypids = {str(my_pid), str(os.getppid())}
    for line in out.splitlines()[1:]:
        parts = line.split(None, 3)
        if len(parts) < 4:
            continue
        pid, ppid, comm, args = parts
        if pid in mypids or ppid in mypids:
            continue
        low = args.lower()
        is_translate = ("rpython" in low) or ("pypy" in low and "targetrpyforth" in low)
        is_bench = bool(re.search(r"(rpyforth-c[\w-]*|gforth(-fast)?)\b", args)) \
            and "run_analysis" not in low and "grep" not in comm
        # our own build script uses rpython -> exclude by build.log marker path
        if "/tmp/ana-" in args:
            continue
        if is_translate or is_bench:
            hits.append("%s %s: %s" % (pid, comm, args[:80]))
    return hits


def wait_for_quiet(my_pid: int, max_wait: int, poll: int = 20) -> bool:
    waited = 0
    while True:
        busy = competing_processes(my_pid)
        if not busy:
            return True
        if waited >= max_wait:
            print("WARNING: still %d competing processes after %ds; proceeding anyway"
                  % (len(busy), waited), file=sys.stderr)
            for p in busy[:5]:
                print("   busy: " + p, file=sys.stderr)
            return False
        print("[quiesce] %d competing process(es); waited %ds/%ds ..."
              % (len(busy), waited, max_wait), file=sys.stderr)
        time.sleep(poll)
        waited += poll


# ---------------------------------------------------------------------------
# Stage 1: build + validate Axis-1 binaries
# ---------------------------------------------------------------------------

def build_ladder(iterations_note=""):
    """Build the three ablation binaries in detached worktrees if not cached.
    Returns dict id->status. Uses /tmp/ana-<id> worktrees created out-of-band."""
    status = {}
    for cid, exe, kind, commit in RPYFORTH_LADDER:
        target = REPO_ROOT / exe
        if target.exists() and os.access(target, os.X_OK):
            status[cid] = "cached"
            continue
        wt = Path("/tmp") / ("ana-" + cid.replace("rpyforth-c-", ""))
        if not wt.exists():
            # create the worktree ourselves (worktree add is allowed)
            rc, _, err = sh(["git", "worktree", "add", str(wt), "--detach", commit],
                            timeout=120, cwd=str(REPO_ROOT))
            if rc != 0:
                status[cid] = "worktree_fail: " + err[:120]
                continue
        # symlink pypy toolchain from main repo
        for d in ("_pypy_binary", "pypy"):
            link = wt / d
            if not link.exists():
                try:
                    link.symlink_to(REPO_ROOT / d)
                except OSError:
                    pass
        env = os.environ.copy()
        env["PYTHONPATH"] = "."
        env["RPYFORTH_EXE_NAME"] = exe
        if kind == "stkfrag":
            env["RPYFORTH_STACK_LAYOUT"] = "fragment"
        elif kind == "virt":
            env["RPYFORTH_VIRTUALIZE"] = "1"
        print("[build] %s (%s @ %s) ..." % (exe, kind, commit), file=sys.stderr)
        rc, _, err = sh(["./_pypy_binary/bin/python2", "./pypy/rpython/bin/rpython",
                         "-Ojit", "rpyforth/targetrpyforth.py"],
                        timeout=1200, env=env, cwd=str(wt))
        built = wt / exe
        if built.exists():
            sh(["cp", "-f", str(built), str(target)])
            status[cid] = "built"
        else:
            status[cid] = "build_fail: rc=%d %s" % (rc, err[-120:])
    return status


def validate_binaries(pin: Optional[int]) -> Dict[str, str]:
    """Run the sieve smoke benchmark on every config; expect 'Count: 1028'."""
    results = {}
    sieve = SHOOTOUT_DIR / (SMOKE_BENCH + ".fs")
    for cid, exe, _, _ in RPYFORTH_LADDER:
        binp = REPO_ROOT / exe
        if not binp.exists():
            results[cid] = "MISSING"
            continue
        out = procs.run_capture([str(binp), str(sieve)], timeout=60, pin=pin)
        results[cid] = ("PASS" if SMOKE_EXPECT in out.stdout
                        else "FAIL(%s)" % (out.stdout + out.stderr)[:60])
    for cid, binp, flags in GFORTH_LADDER:
        out = procs.run_capture([str(binp)] + flags + [str(sieve)], timeout=60, pin=pin)
        results[cid] = ("PASS" if SMOKE_EXPECT in out.stdout
                        else "FAIL(rc=%d,%s)" % (out.rc, (out.stdout + out.stderr)[:50]))
    return results


# ---------------------------------------------------------------------------
# Stage 2: measurement
# ---------------------------------------------------------------------------

_ELAPSED_RE = re.compile(r"Elapsed:\s*(\d+)\s*usec")


def run_shootout_once(binp: Path, flags: List[str], bench: Path, pin: Optional[int],
                      jitlog: Optional[Path]) -> Tuple[str, Optional[int], float, str]:
    """One shootout run. Returns (status, elapsed_usec_or_None, wall_s, output)."""
    pypylog = (procs.pypylog_value("jit-summary", jitlog)
               if jitlog is not None else None)
    out = procs.run_capture(
        [str(binp)] + flags + [str(bench)],
        env=procs.engine_env(pypylog=pypylog),
        timeout=MEASURE_DEFAULT_TIMEOUT,
        pin=pin,
        stdin_path=suites.stdin_path_for(bench),
    )
    if out.timed_out:
        return "timeout", None, out.wall, "timeout"
    if out.rc != 0:
        return "error", None, out.wall, (out.stdout + out.stderr)[:120]
    m = _ELAPSED_RE.search(out.stdout)
    usec = int(m.group(1)) if m else None
    return "ok", usec, out.wall, out.stdout


def measure_shootout(configs_rpy, configs_gf, benches: List[Path], iterations: int,
                     pin: Optional[int], jitdir: Path) -> List[Cell]:
    cells = []
    for bench in benches:
        name = bench.stem
        # rpyforth configs (self-timed usec; capture jit-summary on last iter)
        for cid, exe, _, _ in configs_rpy:
            binp = REPO_ROOT / exe
            cell = Cell(config=cid, suite="shootout", bench=name, metric="usec")
            if not binp.exists():
                cell.status = "missing_binary"
                cells.append(cell)
                continue
            jitlog = None
            for it in range(iterations):
                if it == iterations - 1:
                    jitlog = jitdir / ("%s__%s.jitsum" % (cid, name))
                st, usec, wall, out = run_shootout_once(binp, [], bench, pin, jitlog)
                if st != "ok":
                    cell.status = st
                    cell.detail = out
                    break
                cell.samples.append(float(usec if usec is not None else wall * 1e6))
            else:
                cell.status = "ok"
            if jitlog and jitlog.exists():
                cell.jit = parse_jit_summary(jitlog)
            _finalize(cell)
            cells.append(cell)
        # gforth configs (self-timed usec; no jit)
        for cid, binp, flags in configs_gf:
            cell = Cell(config=cid, suite="shootout", bench=name, metric="usec")
            for it in range(iterations):
                st, usec, wall, out = run_shootout_once(binp, flags, bench, pin, None)
                if st != "ok":
                    cell.status = st
                    cell.detail = out
                    break
                cell.samples.append(float(usec if usec is not None else wall * 1e6))
            else:
                cell.status = "ok"
            _finalize(cell)
            cells.append(cell)
        print("[shootout] %s done" % name, file=sys.stderr)
    return cells


def measure_appbench(configs_rpy, configs_gf, specs: List[ProgramSpec],
                     iterations: int, pin: Optional[int], jitdir: Path,
                     tmpdir: Path) -> List[Cell]:
    """Cold appbench: fresh process per iteration, wall-clock metric."""
    cells = []
    for spec in specs:
        for cid, exe, _, _ in configs_rpy:
            binp = REPO_ROOT / exe
            cell = Cell(config=cid, suite="appbench", bench=spec.name, metric="wall_s")
            if not binp.exists():
                cell.status = "missing_binary"
                cells.append(cell)
                continue
            cmd = build_rpyforth_cmd(binp, spec, tmpdir)
            jitlog = None
            for it in range(iterations):
                pypylog = None
                if it == iterations - 1:
                    jitlog = jitdir / ("%s__%s.jitsum" % (cid, spec.name))
                    pypylog = procs.pypylog_value("jit-summary", jitlog)
                out = procs.run_capture(
                    cmd, cwd=spec.workdir,
                    env=procs.engine_env(spec.rpy_env, pypylog=pypylog),
                    timeout=MEASURE_DEFAULT_TIMEOUT, pin=pin)
                if out.timed_out:
                    cell.status = "timeout"
                    break
                if out.rc != 0:
                    cell.status = "error"
                    cell.detail = (out.stdout + out.stderr)[:120]
                    break
                cell.samples.append(out.wall)
            else:
                cell.status = "ok"
            if jitlog and jitlog.exists():
                cell.jit = parse_jit_summary(jitlog)
            _finalize(cell)
            cells.append(cell)
        for cid, binp, flags in configs_gf:
            cell = Cell(config=cid, suite="appbench", bench=spec.name, metric="wall_s")
            base = build_gforth_cmd(binp, spec, tmpdir)
            # inject engine flags right after the binary
            cmd = [base[0]] + flags + base[1:]
            for it in range(iterations):
                out = procs.run_capture(cmd, cwd=spec.workdir,
                                        timeout=MEASURE_DEFAULT_TIMEOUT, pin=pin)
                if out.timed_out:
                    cell.status = "timeout"
                    break
                if out.rc != 0:
                    cell.status = "error"
                    cell.detail = (out.stdout + out.stderr)[:120]
                    break
                cell.samples.append(out.wall)
            else:
                cell.status = "ok"
            _finalize(cell)
            cells.append(cell)
        print("[appbench] %s done" % spec.name, file=sys.stderr)
    return cells


def _finalize(cell: Cell):
    if cell.samples:
        med, ci = stats.median_ci(cell.samples)
        cell.median = med
        cell.ci_pct = ci
        if cell.status == "pending":
            cell.status = "ok"


# ---------------------------------------------------------------------------
# Stage 3: jit-summary parsing
# ---------------------------------------------------------------------------

# jit-summary lines look like:  Tracing:      123   0.045
#                               Backend:       12   0.008
#                               TOTAL:                0.500
#                               ops:  ...  recorded ops  ...
#                               guards: N
#                               opt ops: N
#                               opt guards: N
#                               forcings: N
#                               abort: N
#                               nvirtuals / nvholes ...
#                               Total # of loops: N
#                               Total # of bridges: N
_JIT_PATTERNS = {
    "tracing_time":   re.compile(r"^Tracing:\s+\d+\s+([\d.]+)", re.M),
    "backend_time":   re.compile(r"^Backend:\s+\d+\s+([\d.]+)", re.M),
    "tracing_no":     re.compile(r"^Tracing:\s+(\d+)", re.M),
    "backend_no":     re.compile(r"^Backend:\s+(\d+)", re.M),
    "total_time":     re.compile(r"^TOTAL:\s+([\d.]+)", re.M),
    "ops":            re.compile(r"^ops:\s+(\d+)", re.M),
    "recorded_ops":   re.compile(r"^recorded ops:\s+(\d+)", re.M),
    "guards":         re.compile(r"^guards:\s+(\d+)", re.M),
    "opt_ops":        re.compile(r"^opt ops:\s+(\d+)", re.M),
    "opt_guards":     re.compile(r"^opt guards:\s+(\d+)", re.M),
    "forcings":       re.compile(r"^forcings:\s+(\d+)", re.M),
    "loops":          re.compile(r"^Total # of loops:\s+(\d+)", re.M),
    "bridges":        re.compile(r"^Total # of bridges:\s+(\d+)", re.M),
    "loop_tokens":    re.compile(r"^Total # of loop tokens:\s+(\d+)", re.M),
}


def parse_jit_summary(path: Path) -> Dict:
    try:
        text = path.read_text()
    except OSError:
        return {}
    out = {}
    for key, pat in _JIT_PATTERNS.items():
        m = pat.search(text)
        if m:
            v = m.group(1)
            out[key] = float(v) if "." in v else int(v)
    # aborts: the summary emits several "abort: <reason>:\tN" lines; sum them.
    out["abort"] = sum(int(n) for n in re.findall(r"^abort:[^\t]*\t(\d+)", text, re.M))
    # derived
    tt = out.get("tracing_time", 0.0)
    bt = out.get("backend_time", 0.0)
    out["warmup_time"] = tt + bt
    loops = out.get("loops", 0)
    bridges = out.get("bridges", 0)
    out["bridges_per_loop"] = (float(bridges) / loops) if loops else 0.0
    out["raw_present"] = bool(out)
    return out


# ---------------------------------------------------------------------------
# Stage 4: counters (perf / cachegrind) auto-detect
# ---------------------------------------------------------------------------

def detect_counters() -> Dict[str, object]:
    """Auto-detect perf stat and valgrind/cachegrind usability. Structured so that
    enabling perf later needs no code change -- just flip 'perf_usable'."""
    info = {"perf_usable": False, "cachegrind_usable": False, "notes": []}
    rc, out, err = sh(["perf", "stat", "-e", "cycles,instructions,branches,branch-misses",
                       "--", "true"], timeout=15)
    combined = (out + err)
    if rc == 0 and "Performance counter stats" in combined and "not supported" not in combined \
            and "not permitted" not in combined and "<not counted>" not in combined:
        info["perf_usable"] = True
    else:
        try:
            para = Path("/proc/sys/kernel/perf_event_paranoid").read_text().strip()
        except OSError:
            para = "?"
        info["notes"].append("perf stat unusable (perf_event_paranoid=%s); rc=%d" % (para, rc))
    rc2, _, _ = sh(["valgrind", "--version"], timeout=10)
    if rc2 == 0:
        info["cachegrind_usable"] = True
    else:
        info["notes"].append("valgrind/cachegrind not installed")
    if not info["perf_usable"] and not info["cachegrind_usable"]:
        info["notes"].append("WARNING: no hardware counters available; counters stage SKIPPED")
    return info


def run_counters(configs_rpy, benches, pin, info) -> Optional[List[Dict]]:
    """Placeholder that runs only if perf/cachegrind become usable. Returns None
    when skipped. Structured so enabling perf later needs no other changes."""
    if not info.get("perf_usable") and not info.get("cachegrind_usable"):
        print("WARNING: counters stage skipped -- " + "; ".join(info["notes"]),
              file=sys.stderr)
        return None
    rows = []
    for cid, exe, _, _ in configs_rpy:
        binp = REPO_ROOT / exe
        if not binp.exists():
            continue
        for bench in benches:
            cmd = ["perf", "stat", "-x", ",", "-e",
                   "cycles,instructions,branches,branch-misses", "--",
                   str(binp), str(bench)]
            out = procs.run_capture(cmd, timeout=MEASURE_DEFAULT_TIMEOUT, pin=pin)
            counters = {}
            for line in out.stderr.splitlines():
                parts = line.split(",")
                if len(parts) >= 3 and parts[0].replace(".", "").isdigit():
                    counters[parts[2]] = parts[0]
            rows.append({"config": cid, "bench": bench.stem, "counters": counters})
    return rows


# ---------------------------------------------------------------------------
# Stage 5: report + charts
# ---------------------------------------------------------------------------

def cells_index(cells: List[Cell]) -> Dict[Tuple[str, str, str], Cell]:
    return {(c.suite, c.bench, c.config): c for c in cells}


def _fmt(v, metric):
    if v is None:
        return "  N/A "
    if metric == "usec":
        return "%8.1f" % (v / 1000.0)  # ms
    return "%8.3f" % v  # seconds


def build_report(cells, smoke, build_status, counter_info, jit_note,
                 iterations, pin, rev) -> str:
    idx = cells_index(cells)
    rpy_ids = [c[0] for c in RPYFORTH_LADDER]
    gf_ids = [c[0] for c in GFORTH_LADDER]
    all_ids = rpy_ids + gf_ids
    suite_metrics = [("shootout", "usec", "ms"), ("appbench", "wall_s", "s")]
    L = []
    L.append("# rpyforth vs gforth -- comparative analysis (%s)" % rev)
    L.append("")
    L.append("iterations=%d (median-of-N, bootstrap 90%% CI), pin=core %s" % (iterations, pin))
    L.append("")

    L.append("## Binary validation (sieve smoke, expect 'Count: 1028')")
    L.append("")
    for cid in all_ids:
        L.append("- %-22s %s" % (cid, smoke.get(cid, "?")))
    L.append("")
    L.append("Build status: " + ", ".join("%s=%s" % (k, v) for k, v in build_status.items()))
    L.append("")

    for suite, metric, unit in suite_metrics:
        benches = sorted({c.bench for c in cells if c.suite == suite})
        L.append("## Master table -- %s (%s, lower is faster)" % (suite, unit))
        L.append("")
        hdr = "%-12s" % "bench" + "".join("%10s" % ladder_label(cid) for cid in all_ids)
        L.append("```")
        L.append(hdr)
        for b in benches:
            row = "%-12s" % b
            for cid in all_ids:
                cell = idx.get((suite, b, cid))
                row += _fmt(cell.median if cell else None, metric)
            L.append(row)
        L.append("```")
        L.append("")

    L.append("## Axis 1 -- rpyforth ablation ladder (shootout, speedup vs naive)")
    L.append("")
    L.append("Each cell = naive_time / config_time (>1 = faster than naive).")
    L.append("Ladder deltas: naive->stkfrag = stack representation; "
             "prefix1->fix1 = FIX1; fix1->stkfrag = FIX2.")
    L.append("```")
    L.append("%-12s%10s%10s%10s%10s" % ("bench", "naive", "prefix1", "fix1", "stkfrag"))
    sh_benches = sorted({c.bench for c in cells if c.suite == "shootout"})
    geomeans = {cid: [] for cid in rpy_ids}
    for b in sh_benches:
        base = idx.get(("shootout", b, "rpyforth-c-naive"))
        row = "%-12s" % b
        for cid in rpy_ids:
            cell = idx.get(("shootout", b, cid))
            if base and base.median and cell and cell.median:
                sp = base.median / cell.median
                geomeans[cid].append(sp)
                row += "%10.2f" % sp
            else:
                row += "%10s" % "N/A"
        L.append(row)
    row = "%-12s" % "GEOMEAN"
    for cid in rpy_ids:
        g = stats.geomean(geomeans[cid])
        row += "%10s" % ("n/a" if g is None else "%.2f" % g)
    L.append(row)
    L.append("```")
    L.append("")

    L.append("## Axis 2 -- gforth decomposition ladder (shootout, time / gforth-fast)")
    L.append("")
    L.append("Each cell = config_time / gforth-fast_time (>1 = slower than full gforth-fast).")
    L.append("Configs: gf-full=dynamic+superinsts, gf-nosuper=--ss-number=0 (replication only), "
             "gf-nodyn=--no-dynamic (static prims), gf=plain engine.")
    L.append("```")
    L.append("%-12s%10s%10s%10s%10s" % ("bench", "gf-full", "gf-nosup", "gf-nodyn", "gf-plain"))
    for b in sh_benches:
        base = idx.get(("shootout", b, "gforth-fast"))
        row = "%-12s" % b
        for cid in gf_ids:
            cell = idx.get(("shootout", b, cid))
            if base and base.median and cell and cell.median:
                row += "%10.2f" % (cell.median / base.median)
            else:
                row += "%10s" % "N/A"
        L.append(row)
    L.append("```")
    L.append("")

    L.append("## Axis 3 -- JIT internals vs outcome (flagship rpyforth-c-stkfrag)")
    L.append("")
    L.append("speedup = gforth-fast_time / rpyforth_time (>1 = rpyforth wins). "
             "warmup_share = (tracing+backend)/wall.")
    L.append("```")
    L.append("%-12s%9s%8s%8s%8s%9s%8s%9s" %
             ("bench", "speedup", "loops", "bridges", "guards", "aborts", "b/loop", "warmup%"))
    corr_rows = []
    for suite in ("shootout", "appbench"):
        benches = sorted({c.bench for c in cells if c.suite == suite})
        for b in benches:
            fc = idx.get((suite, b, RPYFORTH_FLAGSHIP))
            gf = idx.get((suite, b, GFORTH_REFERENCE))
            if not fc or fc.median is None or not gf or gf.median is None:
                continue
            speedup = gf.median / fc.median
            jit = fc.jit or {}
            wall_s = fc.median / (1e6 if suite == "shootout" else 1.0)
            warmup = jit.get("warmup_time", 0.0)
            share = (warmup / wall_s * 100.0) if wall_s else 0.0
            corr_rows.append((suite, b, speedup, jit, share))
    corr_rows.sort(key=lambda r: r[2])
    for suite, b, speedup, jit, share in corr_rows:
        L.append("%-12s%9.2f%8d%8d%8d%9d%8.2f%9.1f" % (
            ("%s/%s" % (suite[:3], b))[:12], speedup,
            jit.get("loops", 0), jit.get("bridges", 0), jit.get("guards", 0),
            jit.get("abort", 0), jit.get("bridges_per_loop", 0.0), share))
    L.append("```")
    L.append("")

    L.append("## Counters stage")
    L.append("")
    L.append("perf_usable=%s cachegrind_usable=%s" %
             (counter_info["perf_usable"], counter_info["cachegrind_usable"]))
    for n in counter_info["notes"]:
        L.append("- " + n)
    L.append("")
    L.append("_" + jit_note + "_")
    return "\n".join(L)


def make_charts(cells, outdir: Path):
    try:
        plt = plotting.pyplot()
    except Exception as e:
        print("WARNING: matplotlib unavailable, charts skipped: %s" % e, file=sys.stderr)
        return []
    idx = cells_index(cells)
    rpy_ids = [c[0] for c in RPYFORTH_LADDER]
    gf_ids = [c[0] for c in GFORTH_LADDER]
    sh_benches = sorted({c.bench for c in cells if c.suite == "shootout"})
    app_benches = sorted({c.bench for c in cells if c.suite == "appbench"})
    outputs = []

    fig, ax = plt.subplots(figsize=(12, 5))
    x = range(len(sh_benches))
    w = 0.2
    for i, cid in enumerate(gf_ids):
        vals = []
        for b in sh_benches:
            base = idx.get(("shootout", b, "gforth-fast"))
            cell = idx.get(("shootout", b, cid))
            vals.append((cell.median / base.median) if base and base.median and cell and cell.median else 0)
        ax.bar([xi + i * w for xi in x], vals, w, label=ladder_label(cid))
    ax.set_xticks([xi + 1.5 * w for xi in x])
    ax.set_xticklabels(sh_benches, rotation=45, ha="right")
    ax.set_ylabel("time / gforth-fast (lower=faster)")
    ax.set_title("Axis 2: gforth decomposition (normalized to gforth-fast)")
    ax.axhline(1.0, color="k", lw=0.7, ls="--")
    ax.legend()
    fig.tight_layout()
    p = outdir / "chart_gforth_decomposition.pdf"
    plotting.save_figure(fig, p); outputs.append(p)

    fig, ax = plt.subplots(figsize=(12, 5))
    for i, cid in enumerate(rpy_ids):
        vals = []
        for b in sh_benches:
            base = idx.get(("shootout", b, "rpyforth-c-naive"))
            cell = idx.get(("shootout", b, cid))
            vals.append((base.median / cell.median) if base and base.median and cell and cell.median else 0)
        ax.bar([xi + i * w for xi in x], vals, w, label=ladder_label(cid))
    ax.set_xticks([xi + 1.5 * w for xi in x])
    ax.set_xticklabels(sh_benches, rotation=45, ha="right")
    ax.set_ylabel("speedup vs naive (higher=faster)")
    ax.set_title("Axis 1: rpyforth ablation ladder (normalized to naive)")
    ax.axhline(1.0, color="k", lw=0.7, ls="--")
    ax.legend()
    fig.tight_layout()
    p = outdir / "chart_rpyforth_ablation.pdf"
    plotting.save_figure(fig, p); outputs.append(p)

    fig, ax = plt.subplots(figsize=(9, 7))
    for suite in ("shootout", "appbench"):
        benches = sorted({c.bench for c in cells if c.suite == suite})
        for b in benches:
            fc = idx.get((suite, b, RPYFORTH_FLAGSHIP))
            gf = idx.get((suite, b, GFORTH_REFERENCE))
            if not fc or fc.median is None or not gf or gf.median is None:
                continue
            bpl = (fc.jit or {}).get("bridges_per_loop", 0.0)
            speedup = gf.median / fc.median
            xv = max(bpl, 0.05)
            ax.scatter(xv, speedup, s=40,
                       c="C0" if suite == "shootout" else "C1")
            ax.annotate(b, (xv, speedup), fontsize=7,
                        xytext=(3, 3), textcoords="offset points")
    ax.set_xscale("log")
    ax.axhline(1.0, color="k", lw=0.7, ls="--")
    ax.set_xlabel("bridges per loop (log)")
    ax.set_ylabel("speedup vs gforth-fast (>1 = rpyforth wins)")
    ax.set_title("Axis 3: bridges-per-loop vs speedup")
    fig.tight_layout()
    p = outdir / "chart_bridges_vs_speedup.pdf"
    plotting.save_figure(fig, p); outputs.append(p)

    fig, ax = plt.subplots(figsize=(9, 5))
    shares = []
    for b in app_benches:
        fc = idx.get(("appbench", b, RPYFORTH_FLAGSHIP))
        if not fc or fc.median is None:
            shares.append(0)
            continue
        warm = (fc.jit or {}).get("warmup_time", 0.0)
        shares.append(warm / fc.median * 100.0 if fc.median else 0)
    ax.bar(range(len(app_benches)), shares, color="C2")
    ax.set_xticks(range(len(app_benches)))
    ax.set_xticklabels(app_benches, rotation=30, ha="right")
    ax.set_ylabel("warmup share of wall time (%)")
    ax.set_title("Axis 3: JIT warm-up share (appbench cold, flagship)")
    fig.tight_layout()
    p = outdir / "chart_warmup_share.pdf"
    plotting.save_figure(fig, p); outputs.append(p)
    return outputs


def cells_to_json(cells):
    return [{
        "config": c.config, "suite": c.suite, "bench": c.bench, "metric": c.metric,
        "median": c.median, "ci_pct": c.ci_pct, "status": c.status,
        "samples": c.samples, "detail": c.detail, "jit": c.jit,
    } for c in cells]


def cmd_measure(args):
    stages = set(args.stages.split(","))

    rev = paths.git_revision(REPO_ROOT)
    outdir = paths.log_dir(paths.LOGS_DIR, "analysis", rev)
    jitdir = outdir / "jitsum"
    jitdir.mkdir(exist_ok=True)
    tmpdir = outdir / "tmp"
    tmpdir.mkdir(exist_ok=True)

    print("=== analysis harness :: rev %s :: out %s ===" % (rev, outdir), file=sys.stderr)

    build_status = {}
    if "build" in stages:
        build_status = build_ladder()
        print("[build] " + str(build_status), file=sys.stderr)

    smoke = validate_binaries(args.pin)
    print("[smoke] " + str(smoke), file=sys.stderr)

    counter_info = detect_counters()
    print("[counters] " + str(counter_info), file=sys.stderr)

    if "quiesce" in stages:
        wait_for_quiet(os.getpid(), args.quiet_wait)

    cells = []
    if "measure" in stages:
        benches = suites.discover_shootout(SHOOTOUT_DIR)
        benches = [b for b in benches if args.exclude not in str(b.relative_to(REPO_ROOT))]
        specs = build_program_registry()
        print("[measure] %d shootout benches, %d appbench programs, %d rpy + %d gf configs"
              % (len(benches), len(specs), len(RPYFORTH_LADDER), len(GFORTH_LADDER)),
              file=sys.stderr)
        cells += measure_shootout(RPYFORTH_LADDER, GFORTH_LADDER, benches,
                                  args.iterations, args.pin, jitdir)
        cells += measure_appbench(RPYFORTH_LADDER, GFORTH_LADDER, specs,
                                  args.iterations, args.pin, jitdir, tmpdir)

    # merge hook: fold cells from a prior run (e.g. the warm steady-state
    # harness JSON) into this run's tables/charts without re-measuring.
    if args.merge_json:
        try:
            prior = json.loads(Path(args.merge_json).read_text())
            merged = 0
            have = {(c.suite, c.bench, c.config) for c in cells}
            for pc in prior.get("cells", prior if isinstance(prior, list) else []):
                key = (pc.get("suite"), pc.get("bench"), pc.get("config"))
                if None in key or key in have:
                    continue
                cell = Cell(config=pc["config"], suite=pc["suite"], bench=pc["bench"],
                            metric=pc.get("metric", "wall_s"))
                cell.samples = pc.get("samples", [])
                cell.median = pc.get("median")
                cell.ci_pct = pc.get("ci_pct", 0.0)
                cell.status = pc.get("status", "ok")
                cell.jit = pc.get("jit")
                cells.append(cell)
                merged += 1
            print("[merge] folded %d cells from %s" % (merged, args.merge_json),
                  file=sys.stderr)
        except (OSError, ValueError, KeyError) as e:
            print("WARNING: --merge-json failed: %s" % e, file=sys.stderr)

    counters = None
    if "counters" in stages:
        counters = run_counters(RPYFORTH_LADDER,
                                suites.discover_shootout(SHOOTOUT_DIR)[:3], args.pin,
                                counter_info)

    jit_note = ("jit-summary captured per rpyforth (config x benchmark) via "
                "PYPYLOG=jit-summary on the final iteration.")

    results = {
        "rev": rev, "iterations": args.iterations, "pin": args.pin,
        "smoke": smoke, "build_status": build_status,
        "counter_info": counter_info, "counters": counters,
        "cells": cells_to_json(cells),
    }
    (outdir / "results.json").write_text(json.dumps(results, indent=2))
    print("[json] wrote " + str(outdir / "results.json"), file=sys.stderr)

    report = ""
    if "report" in stages:
        report = build_report(cells, smoke, build_status, counter_info, jit_note,
                              args.iterations, args.pin, rev)
        (outdir / "report.md").write_text(report)
        print("\n" + report + "\n")
        print("[report] wrote " + str(outdir / "report.md"), file=sys.stderr)

    if "chart" in stages and cells:
        chart_paths = make_charts(cells, outdir)
        for p in chart_paths:
            print("[chart] " + str(p), file=sys.stderr)

    return 0


# ===========================================================================
# Appbench specs and helpers are imported from run_appbench.py.
#
# Shootout strategy: each benchmark is a self-contained script that ends with
# `bye`.  A driver redefines `bye` as a no-op and `(bye)` as the real exit,
# then loops N times timing `s" <abs-path>/<bench>.fs" included` via UTIME.
# Re-inclusion re-defines all words (gforth warns; warnings are suppressed) and
# re-runs the workload identically each time.  Memory allocations accumulate
# across iterations (ary.fs, heap.fs) but RSS growth is bounded (small allocs)
# and timing is stable.
#
# Misbehaving benchmarks are marked and excluded from the chart with a note.
# ===========================================================================

# Benchmarks kept out of the curve grid; none today. Engine-specific source
# variants are dropped by the discovery itself. shootout/curve/ holds a second
# copy of every benchmark under the same stem, so the grid takes the top level
# only and its entries stay unique.
SHOOTOUT_EXCLUDED: Dict[str, str] = {}

SHOOTOUT_FILES = [
    p for p in suites.discover_shootout(SHOOTOUT_DIR, include_curve=False)
    if p.stem not in SHOOTOUT_EXCLUDED
]

# The driver re-INCLUDEs the benchmark source, which ends in `bye`; the shared
# builder neuters that and times each inclusion with the engine's own clock.
build_shootout_driver = drivers.build_shootout_driver

# The shootout sources are standalone, so no appbench setup shim is injected.
build_shootout_cmd = engines.build_engine_cmd

CURVES_DEFAULT_ITERATIONS = 30
CURVES_DEFAULT_TIMEOUT = procs.STEADY_DEFAULT_TIMEOUT
CURVES_DEFAULT_PIN = procs.DEFAULT_PIN
DEFAULT_PDF = None
DEFAULT_JSON = None


def engine_label(engine: str) -> str:
    """Legend and JSON label for one curve series."""
    if engine == ENGINE_RPYFORTH:
        return RPYFORTH_FLAGSHIP
    return engines.engine_display_name(engine)


def _curve_run(engine_key, binary, cmd, workdir, timeout, pin, env=None,
               stdin_path=None):
    """Run one pre-built driver and turn its CSV output into a curve result.

    A benchmark with an stdin fixture has to be fed from a file, which only
    run_capture does; everything else goes through run_popen so a timed-out
    wrapper script (vfxforth.sh parked on VFX's "Press E to exit") is killed
    along with the engine it started.
    """
    if not Path(binary).exists():
        return {"engine": engine_key, "times": [], "wall": 0, "rc": -1,
                "timed_out": False, "stderr": "binary not found",
                "cmd": [str(c) for c in cmd], "skipped": True}
    if stdin_path is None:
        out = procs.run_popen(cmd, cwd=workdir, env=env, timeout=timeout, pin=pin)
    else:
        out = procs.run_capture(cmd, cwd=workdir, env=env, timeout=timeout,
                                pin=pin, stdin_path=stdin_path)
    return {
        "engine": engine_key,
        "times": curves.parse_curve_output(out.stdout),
        "wall": out.wall,
        "rc": out.rc,
        "timed_out": out.timed_out,
        "stderr": out.stderr,
        "cmd": out.cmd,
        "skipped": False,
    }


def run_engine(engine, driver_path, workdir, iterations, timeout, pin, rpy_env=None,
               stdin_path=None):
    """Run a single engine on a pre-built driver file. Returns a result dict."""
    return _curve_run(
        engine, ENGINE_BINARY[engine], build_shootout_cmd(engine, driver_path),
        workdir, timeout, pin,
        env=procs.engine_env(rpy_env if engine == ENGINE_RPYFORTH else None),
        stdin_path=stdin_path,
    )


def run_appbench_engine(engine, spec, iterations, tmpdir, timeout, pin):
    """Wrap appbench run: build driver, write to tmpdir, run."""
    driver_src = appbench_build_driver(spec, iterations, engine)
    driver_path = write_driver(tmpdir, spec.name, driver_src, "%s_driver" % engine)
    return _curve_run(
        engine, ENGINE_BINARY[engine],
        appbench_build_cmd(engine, driver_path, spec),
        spec.workdir, timeout, pin,
        env=procs.engine_env(spec.rpy_env if engine == ENGINE_RPYFORTH else None),
    )


def convergence_iteration(times, threshold=0.10):
    """Return the first iteration index where the time enters a band within
    `threshold` fraction of the steady-state tail median and stays there.

    Returns None if the curve never converges or if there are fewer than 4 data
    points.
    """
    if len(times) < 4:
        return None
    warm = curves.steady_state_tail_usec(times)
    if warm is None or warm == 0:
        return None
    band_lo = warm * (1.0 - threshold)
    band_hi = warm * (1.0 + threshold)
    n = len(times)
    for start in range(n):
        if all(band_lo <= t <= band_hi for t in times[start:]):
            return start
    return None


def _curve_series(extra_engines):
    """Series keys plus their colors and labels, engines first then extras."""
    keys = list(ENGINES) + [k for k, _label in extra_engines]
    color_map = engines.color_map_for(keys)
    label_map = dict((k, engine_label(k)) for k in keys)
    label_map.update(dict(extra_engines))
    return keys, color_map, label_map


def make_all_chart(results, pdf_path, extra_engines=None):
    """Build an 18-subplot grid (4 cols) of warm-up curves for all benchmarks.

    `results` is an ordered list of (bench_name, eng_results_dict) where
    eng_results_dict maps engine key -> run_dict.
    `extra_engines` is an optional list of (engine_key, label) for additional
    rpyforth variants added via --extra-rpyforth.
    """
    plt = plotting.pyplot()
    import matplotlib.lines as mlines

    if extra_engines is None:
        extra_engines = []

    all_engine_keys, color_map, label_map = _curve_series(extra_engines)

    n = len(results)
    ncols = 4
    nrows = math.ceil(n / ncols)

    fig, axes = plt.subplots(nrows, ncols, figsize=(ncols * 4.5, nrows * 3.2),
                             squeeze=False)

    for idx, (bench_name, eng_res) in enumerate(results):
        row = idx // ncols
        col = idx % ncols
        ax = axes[row][col]

        all_times_ms = []
        for engine in all_engine_keys:
            r = eng_res.get(engine)
            if not r or not r["times"]:
                continue
            times_ms = [t / 1000.0 for t in r["times"]]
            all_times_ms.extend(times_ms)
            xs = list(range(len(times_ms)))
            ax.plot(xs, times_ms, marker=".", markersize=2.5, linewidth=1.0,
                    color=color_map[engine], label=label_map[engine])
            warm = curves.steady_state_tail_usec(r["times"])
            if warm is not None:
                ax.axhline(warm / 1000.0, color=color_map[engine], linewidth=0.7,
                           linestyle=":", alpha=0.7)

        if all_times_ms:
            lo = min(all_times_ms)
            hi = max(all_times_ms)
            if lo > 0 and hi / lo > 10.0:
                ax.set_yscale("log")

        ax.set_title(bench_name, fontsize=9, fontweight="bold")
        ax.set_xlabel("iter (0=cold)", fontsize=7)
        ax.set_ylabel("time (ms)", fontsize=7)
        ax.tick_params(labelsize=7)
        ax.grid(True, alpha=0.25)

    for idx in range(n, nrows * ncols):
        row = idx // ncols
        col = idx % ncols
        axes[row][col].set_visible(False)

    # Shared legend (proxy artists)
    legend_handles = [
        mlines.Line2D([], [], color=color_map[e], linewidth=1.5, label=label_map[e])
        for e in all_engine_keys
    ]
    legend_handles.append(
        mlines.Line2D([], [], color="grey", linewidth=0.7, linestyle=":",
                      alpha=0.7, label="warm-tail median")
    )
    ncol_legend = min(4, len(legend_handles))
    fig.legend(handles=legend_handles, loc="lower center", ncol=ncol_legend,
               fontsize=8, frameon=True,
               bbox_to_anchor=(0.5, 0.01))

    n_appbench = sum(1 for name, _res in results if suites.suite_of(name) == "appbench")
    fig.suptitle(
        "Warm-up curves: rpyforth (meta-tracing JIT) vs gforth-fast / gforth\n"
        "%d shootout benchmarks + %d appbench programs | dotted = warm-tail median"
        % (n - n_appbench, n_appbench),
        fontsize=10, fontweight="bold")

    fig.tight_layout(rect=(0, 0.045, 1, 0.96))
    plotting.save_figure(fig, pdf_path)


def _run_extra_shootout(extra_binary, extra_label, bench_path, iters, tmpdir,
                         timeout, pin):
    driver_src = build_shootout_driver(bench_path, iters, ENGINE_RPYFORTH)
    driver_path = write_driver(tmpdir, bench_path.stem, driver_src,
                               "%s_driver" % extra_label)
    return _curve_run(
        extra_label, extra_binary, [str(extra_binary), str(driver_path)],
        SHOOTOUT_DIR, timeout, pin,
        env=procs.engine_env(),
        stdin_path=suites.stdin_path_for(bench_path),
    )


def _run_extra_appbench(extra_binary, extra_label, spec, iters, tmpdir, timeout, pin):
    driver_src = appbench_build_driver(spec, iters, ENGINE_RPYFORTH)
    driver_path = write_driver(tmpdir, spec.name, driver_src,
                               "%s_driver" % extra_label)
    # The extra binary is an rpyforth build, so it takes the rpyforth command
    # line and the program's rpyforth-only environment.
    return _curve_run(
        extra_label, extra_binary, [str(extra_binary), str(driver_path)],
        spec.workdir, timeout, pin,
        env=procs.engine_env(spec.rpy_env),
    )


def _dump_json(results, iterations, json_path, extra_engines):
    """Dump warm data JSON.

    Schema:
    {
      "iterations": N,
      "benchmarks": {
        "<name>": {
          "<engine_label>": {
            "times_usec": [...],
            "cold_usec": <first>,
            "warm_median_usec": <median-of-last-50%>   # null if no data
          }
        }
      }
    }
    """
    all_engine_keys, _colors, label_map = _curve_series(extra_engines)

    benchmarks = {}
    for name, eng_res in results:
        bench_entry = {}
        for engine in all_engine_keys:
            r = eng_res.get(engine)
            label = label_map.get(engine, engine)
            if r is None or r.get("skipped") or r.get("timed_out") or not r["times"]:
                bench_entry[label] = {
                    "times_usec": [],
                    "cold_usec": None,
                    "warm_median_usec": None,
                }
            else:
                times = r["times"]
                warm = curves.steady_state_tail_usec(times)
                bench_entry[label] = {
                    "times_usec": times,
                    "cold_usec": times[0],
                    "warm_median_usec": warm,
                }
        benchmarks[name] = bench_entry

    payload = {"iterations": iterations, "benchmarks": benchmarks}
    Path(json_path).parent.mkdir(parents=True, exist_ok=True)
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2)
    print("Warm data JSON written to %s" % json_path)


def cmd_curves(args):
    extra_engines = []
    for spec_str in args.extra_rpyforth:
        if ":" not in spec_str:
            print("ERROR: --extra-rpyforth must be PATH:LABEL, got: %s" % spec_str,
                  file=sys.stderr)
            return 1
        colon = spec_str.index(":")
        path_str = spec_str[:colon]
        label = spec_str[colon + 1:]
        # Resolve to absolute path so it works regardless of subprocess cwd.
        path_str = str(Path(path_str).resolve())
        extra_engines.append((path_str, label))

    for engine in ENGINES:
        b = ENGINE_BINARY[engine]
        if not Path(b).exists():
            print("WARNING: engine binary missing: %s (%s)" % (engine, b),
                  file=sys.stderr)
    for path_str, label in extra_engines:
        if not Path(path_str).exists():
            print("WARNING: extra engine binary missing: %s (%s)" % (label, path_str),
                  file=sys.stderr)

    shootout_benches = SHOOTOUT_FILES

    excluded_notes = {}
    for name in SHOOTOUT_EXCLUDED:
        excluded_notes[name] = SHOOTOUT_EXCLUDED[name] if isinstance(SHOOTOUT_EXCLUDED, dict) else "excluded"

    results = []  # list of (name, {engine_key: run_dict})

    with tempfile.TemporaryDirectory(prefix="warmup_all_") as tmpdir:

        if not args.appbench_only:
            for bench_path in shootout_benches:
                name = bench_path.stem
                eng_res = {}
                iters = args.iterations

                print("[shootout] %s  (%d iterations)" % (name, iters))

                for engine in ENGINES:
                    if not Path(ENGINE_BINARY[engine]).exists():
                        continue
                    driver_src = build_shootout_driver(bench_path, iters, engine)
                    driver_path = write_driver(tmpdir, name, driver_src,
                                               "%s_driver" % engine)

                    print("  %-20s ... " % engine, end="", flush=True)
                    r = run_engine(engine, driver_path, SHOOTOUT_DIR, iters,
                                   args.timeout, args.pin,
                                   stdin_path=suites.stdin_path_for(bench_path))
                    warm = curves.steady_state_tail_usec(r["times"])
                    if r.get("skipped"):
                        print("SKIP (binary missing)")
                    elif r["timed_out"]:
                        print("TIMEOUT after %.1fs" % r["wall"])
                    elif not r["times"]:
                        print("NO CSV DATA (rc=%d)" % r["rc"])
                        if r["stderr"].strip():
                            print("    stderr: %s" % r["stderr"].strip()[:200])
                    else:
                        conv = convergence_iteration(r["times"])
                        print("%d iters, cold=%s warm=%s conv@%s (%.1fs)" % (
                            len(r["times"]),
                            stats.fmt_usec(r["times"][0]),
                            stats.fmt_usec(warm),
                            str(conv) if conv is not None else "n/a",
                            r["wall"],
                        ))
                    eng_res[engine] = r

                # Extra engines for shootout
                for path_str, label in extra_engines:
                    print("  %-20s ... " % label, end="", flush=True)
                    r = _run_extra_shootout(path_str, label, bench_path, iters,
                                            tmpdir, args.timeout, args.pin)
                    warm = curves.steady_state_tail_usec(r["times"])
                    if r.get("skipped"):
                        print("SKIP (binary missing)")
                    elif r["timed_out"]:
                        print("TIMEOUT after %.1fs" % r["wall"])
                    elif not r["times"]:
                        print("NO CSV DATA (rc=%d)" % r["rc"])
                        if r["stderr"].strip():
                            print("    stderr: %s" % r["stderr"].strip()[:200])
                    else:
                        conv = convergence_iteration(r["times"])
                        print("%d iters, cold=%s warm=%s conv@%s (%.1fs)" % (
                            len(r["times"]),
                            stats.fmt_usec(r["times"][0]),
                            stats.fmt_usec(warm),
                            str(conv) if conv is not None else "n/a",
                            r["wall"],
                        ))
                    eng_res[path_str] = r

                results.append((name, eng_res))

        if not args.shootout_only:
            for spec in APPBENCH_PROGRAMS:
                iters = suites.iterations_for(spec.name, args.iterations)
                eng_res = {}

                print("[appbench] %s  (%d iterations)" % (spec.name, iters))

                for engine in ENGINES:
                    if not Path(ENGINE_BINARY[engine]).exists():
                        continue
                    print("  %-20s ... " % engine, end="", flush=True)
                    r = run_appbench_engine(engine, spec, iters, tmpdir,
                                            args.timeout, args.pin)
                    warm = curves.steady_state_tail_usec(r["times"])
                    if r.get("skipped"):
                        print("SKIP (binary missing)")
                    elif r["timed_out"]:
                        print("TIMEOUT after %.1fs" % r["wall"])
                    elif not r["times"]:
                        print("NO CSV DATA (rc=%d)" % r["rc"])
                        if r["stderr"].strip():
                            print("    stderr: %s" % r["stderr"].strip()[:200])
                    else:
                        conv = convergence_iteration(r["times"])
                        print("%d iters, cold=%s warm=%s conv@%s (%.1fs)" % (
                            len(r["times"]),
                            stats.fmt_usec(r["times"][0]),
                            stats.fmt_usec(warm),
                            str(conv) if conv is not None else "n/a",
                            r["wall"],
                        ))
                    eng_res[engine] = r

                # Extra engines for appbench
                for path_str, label in extra_engines:
                    print("  %-20s ... " % label, end="", flush=True)
                    r = _run_extra_appbench(path_str, label, spec, iters,
                                            tmpdir, args.timeout, args.pin)
                    warm = curves.steady_state_tail_usec(r["times"])
                    if r.get("skipped"):
                        print("SKIP (binary missing)")
                    elif r["timed_out"]:
                        print("TIMEOUT after %.1fs  [null]" % r["wall"])
                    elif not r["times"]:
                        print("NO CSV DATA (rc=%d)" % r["rc"])
                        if r["stderr"].strip():
                            print("    stderr: %s" % r["stderr"].strip()[:200])
                    else:
                        conv = convergence_iteration(r["times"])
                        print("%d iters, cold=%s warm=%s conv@%s (%.1fs)" % (
                            len(r["times"]),
                            stats.fmt_usec(r["times"][0]),
                            stats.fmt_usec(warm),
                            str(conv) if conv is not None else "n/a",
                            r["wall"],
                        ))
                    eng_res[path_str] = r

                results.append((spec.name, eng_res))

    # --- Convergence table (rpyforth) ---
    print("\n" + "=" * 72)
    print("CONVERGENCE TABLE  (rpyforth-c-stkfrag, within 10% of warm-tail)")
    print("=" * 72)
    print("%-14s  %8s  %12s  %12s  %8s" % (
        "benchmark", "conv@iter", "cold", "warm-tail", "speedup"))
    print("-" * 72)
    for name, eng_res in results:
        r = eng_res.get(ENGINE_RPYFORTH)
        if not r or not r["times"]:
            print("%-14s  %8s" % (name, "NO DATA"))
            continue
        times = r["times"]
        conv = convergence_iteration(times)
        warm = curves.steady_state_tail_usec(times)
        cold = times[0]
        speedup = ("%.2fx" % (cold / float(warm))) if warm and warm > 0 else "-"
        print("%-14s  %8s  %12s  %12s  %8s" % (
            name,
            str(conv) if conv is not None else "n/a",
            stats.fmt_usec(cold),
            stats.fmt_usec(warm),
            speedup,
        ))
    print("-" * 72)
    if excluded_notes:
        print("\nEXCLUDED BENCHMARKS:")
        for name, reason in excluded_notes.items():
            print("  %-14s  %s" % (name, reason))

    # Default to a persistent, per-revision directory so both the chart and its
    # underlying data survive across runs (logs/warmup/<rev>/). /tmp defaults are
    # gone: an explicit --pdf/--json still overrides.
    rev = paths.git_revision(REPO_ROOT)
    outdir = paths.log_dir(paths.LOGS_DIR, "warmup", rev)
    pdf_path = Path(args.pdf) if args.pdf else outdir / "warmup_all.pdf"
    json_path = Path(args.json_path) if args.json_path else outdir / "warmup_curves.json"
    pdf_path.parent.mkdir(parents=True, exist_ok=True)
    json_path.parent.mkdir(parents=True, exist_ok=True)

    _dump_json(results, args.iterations, json_path, extra_engines)

    try:
        make_all_chart(results, pdf_path, extra_engines=extra_engines)
        print("\nWarm-up curve chart written to %s" % pdf_path)
    except Exception as exc:
        import traceback
        print("ERROR generating chart: %s" % exc, file=sys.stderr)
        traceback.print_exc()
        return 1

    return 0


# ===========================================================================
# Usage:
#     .venv/bin/python benchmark/run_ablation.py render logs/analysis/7038abb-dirty/results.json
#
# Outputs:
#     /tmp/ablation_waterfall.pdf
#     /tmp/ablation_summary.pdf
#     /tmp/ablation_vs_gforthfast.pdf
# ===========================================================================

LADDER = [
    "rpyforth-c-naive",
    "rpyforth-c-prefix1",
    "rpyforth-c-fix1",
    "rpyforth-c-stkfrag",
]

STEP_LABELS = [
    "+stack cache & inlining",
    "+FIX1: return-address promotion split",
    "+FIX2: loop-limit promotion split",
]

STEP_COLORS = ["#4C72B0", "#55A868", "#C44E52"]
REGRESSION_HATCH = "///"

SHOOTOUT_BENCHES = suites.SHOOTOUT_KERNELS
APPBENCH_BENCHES = suites.APPBENCH_PROGRAMS


def load_cells(path: str) -> Dict[Tuple[str, str, str], Optional[float]]:
    with open(path) as f:
        data = json.load(f)
    cells = data["cells"]
    lookup: Dict[Tuple[str, str, str], Optional[float]] = {}
    for c in cells:
        key = (c["config"], c["suite"], c["bench"])
        if c["status"] == "ok" and c["median"] is not None:
            lookup[key] = float(c["median"])
        else:
            lookup[key] = None
    return lookup


def speedup_vs_naive(
    lookup: Dict[Tuple[str, str, str], Optional[float]],
    suite: str,
    bench: str,
    config: str,
) -> Optional[float]:
    naive = lookup.get(("rpyforth-c-naive", suite, bench))
    val = lookup.get((config, suite, bench))
    if naive is None or val is None or val == 0:
        return None
    return naive / val


def step_multipliers(
    lookup: Dict[Tuple[str, str, str], Optional[float]],
    suite: str,
    bench: str,
) -> Tuple[Optional[float], List[Optional[float]]]:
    """Return (final_speedup_vs_naive, [step1_mult, step2_mult, step3_mult]).

    Each step_i_mult = time[i] / time[i+1] (>1 means improvement).
    Returns None for steps where data is missing.
    """
    times: List[Optional[float]] = [
        lookup.get((cfg, suite, bench)) for cfg in LADDER
    ]
    mults: List[Optional[float]] = []
    for i in range(3):
        t0, t1 = times[i], times[i + 1]
        if t0 is None or t1 is None or t1 == 0:
            mults.append(None)
        else:
            mults.append(t0 / t1)

    final = speedup_vs_naive(lookup, suite, bench, "rpyforth-c-stkfrag")
    return final, mults


def fmt_mult(m: float) -> str:
    return f"\xd7{m:.2f}"


def render_waterfall(
    lookup: Dict[Tuple[str, str, str], Optional[float]],
    pdf_path: str,
) -> None:
    """Per-benchmark horizontal waterfall: rows sorted by final speedup.

    The x-axis is log-scale speedup vs naive.  Each benchmark row shows three
    stacked segments (one per ladder step).  Segment left edge = cumulative
    speedup after all prior steps; right edge = cumulative speedup after this
    step.  A step that regresses (mult < 1) is drawn in a hatched red overlay
    extending leftward from the current position.
    """

    plt = plotting.pyplot()
    import matplotlib.patches as mpatches

    n_shootout = len(SHOOTOUT_BENCHES)
    n_appbench = len(APPBENCH_BENCHES)
    suite_info = [
        ("shootout", SHOOTOUT_BENCHES, "Shootout (%d benchmarks)" % n_shootout),
        ("appbench", APPBENCH_BENCHES, "Appbench (%d benchmarks)" % n_appbench),
    ]

    fig_h = max(7, (n_shootout + n_appbench) * 0.48 + 2.5)

    fig, axes = plt.subplots(
        1, 2,
        figsize=(15, fig_h),
        gridspec_kw={"width_ratios": [n_shootout, n_appbench]},
    )

    for ax_idx, (suite, benches, panel_title) in enumerate(suite_info):
        ax = axes[ax_idx]

        rows = []
        for bench in benches:
            final, mults = step_multipliers(lookup, suite, bench)
            rows.append((bench, final, mults))

        rows.sort(key=lambda r: (r[1] if r[1] is not None else 0.0))

        bench_names = [r[0] for r in rows]
        n_rows = len(bench_names)

        for yi, (bench, final, mults) in enumerate(rows):
            cum_log = 0.0

            for si, mult in enumerate(mults):
                if mult is None:
                    x_cur = math.exp(cum_log)
                    ax.text(
                        x_cur * 1.03, yi, "n/a",
                        va="center", ha="left", fontsize=6, color="gray",
                    )
                    continue

                is_regression = mult < 1.0
                color = STEP_COLORS[si]

                x_left = math.exp(cum_log)
                new_cum_log = cum_log + math.log(max(mult, 1e-9))
                x_right = math.exp(new_cum_log)

                if is_regression:
                    bar_left = x_right
                    bar_width = x_left - x_right
                    ec = "#8B0000"
                    fc = "#C44E52"
                    hatch = REGRESSION_HATCH
                    alpha = 0.65
                else:
                    bar_left = x_left
                    bar_width = x_right - x_left
                    ec = color
                    fc = color
                    hatch = ""
                    alpha = 0.85

                ax.barh(
                    yi, bar_width, left=bar_left,
                    height=0.72, color=fc, edgecolor=ec,
                    hatch=hatch, linewidth=0.5, alpha=alpha,
                )

                if abs(math.log(max(mult, 1e-9))) > math.log(1.05):
                    label_x = (bar_left + bar_left + bar_width) / 2
                    ax.text(
                        label_x, yi, fmt_mult(mult),
                        ha="center", va="center", fontsize=6,
                        color="white" if not is_regression else "#600000",
                        fontweight="bold",
                    )

                cum_log = new_cum_log

        ax.set_yticks(list(range(n_rows)))
        ax.set_yticklabels(bench_names, fontsize=8)
        ax.set_xscale("log")
        ax.axvline(x=1.0, color="black", linewidth=0.9, linestyle="--", alpha=0.5)
        ax.set_xlabel("Cumulative speedup vs. naive (log scale)", fontsize=9)
        ax.set_title(panel_title, fontsize=10, fontweight="bold")
        ax.grid(axis="x", alpha=0.3, linestyle=":")
        all_finals = [r[1] for r in rows if r[1] is not None]
        x_max = max(all_finals) * 1.25 if all_finals else 4.0
        x_min = min(0.3, min(all_finals) * 0.7) if all_finals else 0.3
        ax.set_xlim(left=x_min, right=x_max)

    legend_patches = [
        mpatches.Patch(facecolor=STEP_COLORS[i], alpha=0.85, label=STEP_LABELS[i])
        for i in range(3)
    ]
    regression_patch = mpatches.Patch(
        facecolor="#C44E52", hatch=REGRESSION_HATCH, alpha=0.65,
        label="regression step (hatched, leftward)",
    )
    legend_patches.append(regression_patch)
    fig.legend(
        handles=legend_patches,
        loc="lower center",
        ncol=2,
        fontsize=8,
        bbox_to_anchor=(0.5, 0.0),
        frameon=True,
    )

    fig.suptitle(
        "Ablation waterfall: per-step multiplicative contribution to speedup vs. naive",
        fontsize=12,
        fontweight="bold",
    )
    plt.tight_layout(rect=[0, 0.08, 1, 0.97])

    with plotting.pdf_pages(pdf_path) as pdf:
        plotting.save_figure(fig, pdf)
    print(f"Wrote {pdf_path}")


def render_summary(
    lookup: Dict[Tuple[str, str, str], Optional[float]],
    pdf_path: str,
) -> None:
    """Two grouped bars: shootout geomean and appbench geomean, cumulative after each step."""

    suite_info = [
        ("shootout", SHOOTOUT_BENCHES, "Shootout"),
        ("appbench", APPBENCH_BENCHES, "Appbench"),
    ]

    step_configs = LADDER[1:]
    n_steps = len(step_configs)
    suite_labels = [s[2] for s in suite_info]

    # A step with no usable data stays None and is reported as n/a rather than
    # drawn as a 1.00x "no change" bar.
    cumulative: Dict[str, List[Optional[float]]] = {}
    for suite, benches, label in suite_info:
        cum_by_step = []
        for cfg in step_configs:
            cum_by_step.append(stats.geomean(
                speedup_vs_naive(lookup, suite, bench, cfg) for bench in benches
            ))
        cumulative[label] = cum_by_step

    plt = plotting.pyplot()
    fig, ax = plt.subplots(figsize=(9, 5))

    n_suites = len(suite_labels)
    x = list(range(n_suites))
    bar_total_width = 0.65
    bar_w = plotting.bar_width(n_steps, bar_total_width)
    offsets = plotting.group_offsets(n_steps, bar_total_width)

    for si, (cfg, label, color) in enumerate(
        zip(step_configs, STEP_LABELS, STEP_COLORS)
    ):
        heights = [cumulative[sl][si] for sl in suite_labels]
        bars = ax.bar(
            [xi + offsets[si] for xi in x],
            [h if h is not None else 0.0 for h in heights],
            width=bar_w * 0.9,
            color=color,
            label=f"After {label}",
            alpha=0.85,
            edgecolor="white",
            linewidth=0.5,
        )
        for bar, h in zip(bars, heights):
            ax.text(
                bar.get_x() + bar.get_width() / 2,
                (h if h is not None else 0.0) + 0.02,
                "n/a" if h is None else f"{h:.2f}x",
                ha="center",
                va="bottom",
                fontsize=8,
                color=STEP_COLORS[si],
                fontweight="bold",
            )

    for si in range(1, n_steps):
        for suite_idx, sl in enumerate(suite_labels):
            prev = cumulative[sl][si - 1]
            curr = cumulative[sl][si]
            if prev is None or curr is None or prev == 0:
                continue
            delta = curr / prev
            x_pos = x[suite_idx] + offsets[si]
            y_pos = curr + 0.12
            sign = "+" if delta >= 1.0 else ""
            pct = (delta - 1.0) * 100
            ax.text(
                x_pos,
                y_pos,
                f"{sign}{pct:.0f}%",
                ha="center",
                va="bottom",
                fontsize=7,
                color="#333333",
            )

    ax.axhline(y=1.0, color="black", linewidth=0.8, linestyle="--", alpha=0.5, label="naive baseline")
    ax.set_xticks(x)
    ax.set_xticklabels(suite_labels, fontsize=11)
    ax.set_ylabel("Geomean speedup vs. rpy-naive", fontsize=10)
    ax.set_title(
        "Ablation summary: cumulative geomean speedup per suite",
        fontsize=12,
        fontweight="bold",
    )
    ax.legend(fontsize=8, loc="upper left")
    ax.grid(axis="y", alpha=0.3, linestyle=":")
    ax.set_ylim(bottom=0)

    plt.tight_layout()
    with plotting.pdf_pages(pdf_path) as pdf:
        plotting.save_figure(fig, pdf)
    print(f"Wrote {pdf_path}")


def render_vs_gforthfast(
    lookup: Dict[Tuple[str, str, str], Optional[float]],
    pdf_path: str,
) -> None:
    """Per-benchmark: stkfrag vs gforth-fast, with naive marker."""

    plt = plotting.pyplot()
    import matplotlib.patches as mpatches

    FLAGSHIP = ladder_label(RPYFORTH_FLAGSHIP)
    NAIVE = ladder_label(LADDER[0])

    all_benches = []
    for bench in SHOOTOUT_BENCHES:
        all_benches.append(("shootout", bench))
    for bench in APPBENCH_BENCHES:
        all_benches.append(("appbench", bench))

    rows = []
    for suite, bench in all_benches:
        gf = lookup.get(("gforth-fast", suite, bench))
        stkfrag = lookup.get(("rpyforth-c-stkfrag", suite, bench))
        naive = lookup.get(("rpyforth-c-naive", suite, bench))
        if gf is None or stkfrag is None or gf == 0 or stkfrag == 0:
            continue
        ratio_stkfrag = gf / stkfrag
        ratio_naive = (gf / naive) if naive and naive > 0 else None
        rows.append((bench, suite, ratio_stkfrag, ratio_naive))

    rows.sort(key=lambda r: r[2])

    fig, ax = plt.subplots(figsize=(12, 6))

    bench_labels = [f"{r[0]}" for r in rows]
    y_pos = list(range(len(rows)))

    win_color = "#4C72B0"
    loss_color = "#C44E52"

    for yi, (bench, suite, ratio_stkfrag, ratio_naive) in enumerate(rows):
        color = win_color if ratio_stkfrag >= 1.0 else loss_color
        ax.barh(
            yi,
            ratio_stkfrag,
            height=0.65,
            color=color,
            alpha=0.8,
            edgecolor="white",
            linewidth=0.3,
        )
        suite_marker = "S" if suite == "shootout" else "A"
        label_x = max(ratio_stkfrag + 0.02, 0.1)
        ax.text(
            label_x,
            yi,
            f"{ratio_stkfrag:.2f}x  [{suite_marker}]",
            va="center",
            fontsize=7.5,
            color="#333333",
        )

        if ratio_naive is not None:
            ax.plot(
                ratio_naive,
                yi,
                marker="D",
                markersize=5,
                color="black",
                alpha=0.6,
                zorder=5,
            )

    ax.axvline(x=1.0, color="black", linewidth=1.2, linestyle="--", alpha=0.7)
    ax.set_yticks(y_pos)
    ax.set_yticklabels(bench_labels, fontsize=8)
    ax.set_xlabel("Speedup of %s vs. gforth-fast  (>1 = rpy wins)" % FLAGSHIP, fontsize=10)
    ax.set_title(
        "Final position: %s vs gforth-fast\n(bar = stkfrag; diamond = %s; sorted by final)"
        % (FLAGSHIP, NAIVE),
        fontsize=11,
        fontweight="bold",
    )

    win_patch = mpatches.Patch(facecolor=win_color, alpha=0.8, label="%s wins" % FLAGSHIP)
    loss_patch = mpatches.Patch(facecolor=loss_color, alpha=0.8, label="%s loses" % FLAGSHIP)
    naive_marker = plt.Line2D(
        [0], [0], marker="D", color="w", markerfacecolor="black",
        markersize=6, alpha=0.6, label="%s baseline" % NAIVE,
    )
    ax.legend(handles=[win_patch, loss_patch, naive_marker], fontsize=8, loc="lower right")
    ax.grid(axis="x", alpha=0.3, linestyle=":")

    plt.tight_layout()
    with plotting.pdf_pages(pdf_path) as pdf:
        plotting.save_figure(fig, pdf)
    print(f"Wrote {pdf_path}")


def load_warm_steady(path: str) -> Dict[Tuple[str, str], Optional[float]]:
    """Load warm_steady.json produced by run_ablation.py curves --json.

    Returns a dict keyed by (engine_label, bench_name) -> warm_median_usec.
    """
    with open(path) as f:
        data = json.load(f)
    benchmarks = data.get("benchmarks", {})
    lookup: Dict[Tuple[str, str], Optional[float]] = {}
    for bench_name, eng_map in benchmarks.items():
        for label, info in eng_map.items():
            warm = info.get("warm_median_usec")
            lookup[(label, bench_name)] = float(warm) if warm is not None else None
    return lookup


def _suite_for(bench: str) -> str:
    if bench in SHOOTOUT_BENCHES:
        return "shootout"
    return "appbench"


def render_vs_gforthfast_warm(
    cold_lookup: Dict[Tuple[str, str, str], Optional[float]],
    warm_lookup: Dict[Tuple[str, str], Optional[float]],
    pdf_path: str,
) -> None:
    """vs-gforth-fast chart using WARM steady-state medians.

    Bar = warm ratio (gforth-fast_warm / flagship_warm).
    Naive warm marker = black diamond.
    Cold ratio shown as hollow diamond per bar.
    """
    plt = plotting.pyplot()
    import matplotlib.patches as mpatches

    FLAGSHIP_LABEL = "rpyforth-c-stkfrag"
    GFFAST_LABEL = "gforth-fast"
    NAIVE_LABEL = "rpyforth-naive"
    FLAGSHIP = ladder_label(RPYFORTH_FLAGSHIP)
    NAIVE = ladder_label(LADDER[0])

    all_benches = []
    for bench in SHOOTOUT_BENCHES:
        all_benches.append(("shootout", bench))
    for bench in APPBENCH_BENCHES:
        all_benches.append(("appbench", bench))

    rows = []
    for suite, bench in all_benches:
        gf_warm = warm_lookup.get((GFFAST_LABEL, bench))
        flagship_warm = warm_lookup.get((FLAGSHIP_LABEL, bench))
        naive_warm = warm_lookup.get((NAIVE_LABEL, bench))

        gf_cold = cold_lookup.get(("gforth-fast", suite, bench))
        flagship_cold = cold_lookup.get(("rpyforth-c-stkfrag", suite, bench))

        if gf_warm is None or flagship_warm is None or flagship_warm == 0:
            continue

        warm_ratio = gf_warm / flagship_warm
        naive_warm_ratio = (gf_warm / naive_warm) if (naive_warm and naive_warm > 0) else None
        cold_ratio = (gf_cold / flagship_cold) if (gf_cold and flagship_cold and flagship_cold > 0) else None

        rows.append((bench, suite, warm_ratio, naive_warm_ratio, cold_ratio))

    rows.sort(key=lambda r: r[2])

    fig, ax = plt.subplots(figsize=(13, 6.5))

    bench_labels = [r[0] for r in rows]
    y_pos = list(range(len(rows)))

    win_color = "#4C72B0"
    loss_color = "#C44E52"

    for yi, (bench, suite, warm_ratio, naive_warm_ratio, cold_ratio) in enumerate(rows):
        color = win_color if warm_ratio >= 1.0 else loss_color
        ax.barh(
            yi,
            warm_ratio,
            height=0.65,
            color=color,
            alpha=0.8,
            edgecolor="white",
            linewidth=0.3,
        )
        suite_marker = "S" if suite == "shootout" else "A"
        label_x = max(warm_ratio + 0.02, 0.1)
        ax.text(
            label_x,
            yi,
            f"{warm_ratio:.2f}x  [{suite_marker}]",
            va="center",
            fontsize=7.5,
            color="#333333",
        )

        if naive_warm_ratio is not None:
            ax.plot(
                naive_warm_ratio,
                yi,
                marker="D",
                markersize=5,
                color="black",
                alpha=0.6,
                zorder=5,
            )

        if cold_ratio is not None:
            ax.plot(
                cold_ratio,
                yi,
                marker="D",
                markersize=5,
                markerfacecolor="none",
                markeredgecolor="#888888",
                markeredgewidth=1.0,
                alpha=0.75,
                zorder=4,
            )

    ax.axvline(x=1.0, color="black", linewidth=1.2, linestyle="--", alpha=0.7)
    ax.set_yticks(y_pos)
    ax.set_yticklabels(bench_labels, fontsize=8)
    ax.set_xlabel("Speedup of %s vs. gforth-fast  (>1 = rpy wins)" % FLAGSHIP, fontsize=10)
    ax.set_title(
        "Final position: %s vs gforth-fast — warm steady-state\n"
        "(bar = warm ratio; filled ◆ = %s warm; hollow ◆ = cold ratio)"
        % (FLAGSHIP, NAIVE),
        fontsize=11,
        fontweight="bold",
    )

    win_patch = mpatches.Patch(facecolor=win_color, alpha=0.8,
                               label="%s wins (warm)" % FLAGSHIP)
    loss_patch = mpatches.Patch(facecolor=loss_color, alpha=0.8,
                                label="%s loses (warm)" % FLAGSHIP)
    naive_marker = plt.Line2D(
        [0], [0], marker="D", color="w", markerfacecolor="black",
        markersize=6, alpha=0.6, label="%s warm baseline" % NAIVE,
    )
    cold_marker = plt.Line2D(
        [0], [0], marker="D", color="w", markerfacecolor="none",
        markeredgecolor="#888888", markeredgewidth=1.0,
        markersize=6, alpha=0.75, label="cold ratio (hollow)",
    )
    ax.legend(handles=[win_patch, loss_patch, naive_marker, cold_marker],
              fontsize=8, loc="lower right")
    ax.grid(axis="x", alpha=0.3, linestyle=":")

    plt.tight_layout()
    with plotting.pdf_pages(pdf_path) as pdf:
        plotting.save_figure(fig, pdf)
    print(f"Wrote {pdf_path}")


def cmd_render(args):
    lookup = load_cells(args.results_json)

    render_waterfall(lookup, "/tmp/ablation_waterfall.pdf")
    render_summary(lookup, "/tmp/ablation_summary.pdf")
    render_vs_gforthfast(lookup, "/tmp/ablation_vs_gforthfast.pdf")

    if args.steady_json:
        warm_lookup = load_warm_steady(args.steady_json)
        render_vs_gforthfast_warm(
            lookup,
            warm_lookup,
            "/tmp/ablation_vs_gforthfast_warm.pdf",
        )


# ===========================================================================
# Top-level dispatch
# ===========================================================================

def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Unified ablation-analysis harness (measure / curves / render).")
    sub = parser.add_subparsers(dest="command", required=True)

    ap = sub.add_parser("measure", help="comparative-analysis harness "
                        "(rpyforth ablation ladder + gforth decomposition + "
                        "JIT internals)")
    ap.add_argument("--iterations", type=int, default=MEASURE_DEFAULT_ITERATIONS)
    ap.add_argument("--pin", type=int, default=MEASURE_DEFAULT_PIN)
    ap.add_argument("--stages", default="build,quiesce,measure,report,chart",
                    help="comma list: build,quiesce,measure,counters,report,chart")
    ap.add_argument("--quiet-wait", type=int, default=3600,
                    help="max seconds to wait for competing processes")
    ap.add_argument("--exclude", default="curve/")
    ap.add_argument("--merge-json", default=None, metavar="PATH",
                    help="merge cells from a prior results.json (e.g. warm "
                         "steady-state numbers) into this run's outputs instead "
                         "of re-measuring them; cells need config/suite/bench/"
                         "metric/median fields")
    ap.set_defaults(func=cmd_measure)

    cp = sub.add_parser("curves", help="all-benchmark warm-up curve grid "
                        "(shootout + appbench)")
    cp.add_argument("--iterations", type=int, default=CURVES_DEFAULT_ITERATIONS,
                    help="iterations per benchmark (default %d; lexex uses %d)"
                         % (CURVES_DEFAULT_ITERATIONS, suites.LEXEX_ITERATIONS))
    cp.add_argument("--timeout", type=int, default=CURVES_DEFAULT_TIMEOUT,
                    help="per-run timeout in seconds (default %d)" % CURVES_DEFAULT_TIMEOUT)
    cp.add_argument("--pin", type=int, default=CURVES_DEFAULT_PIN,
                    help="CPU core to pin via taskset -c (default %d)" % CURVES_DEFAULT_PIN)
    cp.add_argument("--pdf", type=str, default=DEFAULT_PDF,
                    help="output PDF path (default logs/warmup/<rev>/warmup_all.pdf)")
    cp.add_argument("--json", type=str, default=DEFAULT_JSON, dest="json_path",
                    help="dump warm data JSON to this path "
                         "(default logs/warmup/<rev>/warmup_curves.json; always written)")
    cp.add_argument("--extra-rpyforth", action="append", default=[],
                    metavar="PATH:LABEL", dest="extra_rpyforth",
                    help="additional rpyforth binary to run (repeatable); "
                         "format PATH:LABEL")
    cp.add_argument("--shootout-only", action="store_true",
                    help="run only shootout benchmarks (skip appbench)")
    cp.add_argument("--appbench-only", action="store_true",
                    help="run only appbench benchmarks (skip shootout)")
    cp.set_defaults(func=cmd_curves)

    rp = sub.add_parser("render", help="render ablation PDFs from a measure "
                        "results.json (waterfall / summary / vs-gforth-fast)")
    rp.add_argument("results_json", help="cold ablation results.json")
    rp.add_argument("--steady-json", default=None,
                    help="warm_steady.json from run_ablation.py curves --json; "
                         "renders warm vs-gforthfast chart to "
                         "/tmp/ablation_vs_gforthfast_warm.pdf")
    rp.set_defaults(func=cmd_render)

    args = parser.parse_args(argv)
    rc = args.func(args)
    return rc if rc is not None else 0


if __name__ == "__main__":
    sys.exit(main())
