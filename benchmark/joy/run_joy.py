#!/usr/bin/env python3
"""Run rpyjoy paper / shootout benchmarks.

Usage:
    ./benchmark/joy/run_joy.py
    ./benchmark/joy/run_joy.py --suite shootout --iterations 5 --pin 3
    ./benchmark/joy/run_joy.py --engines ./rpyjoy-c,./rpyjoy-c-stkfrag
    ./benchmark/joy/run_joy.py --benchmarks sieve.joy,nrev.joy

Engines default to the translated binaries (rpyjoy-c, rpyjoy-c-stkfrag);
missing binaries are skipped with a warning.

Timing: if the engine supports the `clock` word ( -- usec ), each benchmark
runs ITERATIONS times inside one process via a generated driver that prints
one CSV line per iteration ("<i> ,<usec>", the parse_curve_output convention
from benchmark/run_ablation.py). Otherwise each iteration is a separate
process and the whole-run "Elapsed: <n> usec" line is used.

Results are persisted as JSON plus a text summary under
logs/joybench/<git-rev>/.

Layout:
    benchmark/joy/shootout/         Forth shootout homologs
    benchmark/joy/axis-b/           Joy combinator / list / recursion suite
    shootout/*.fs                   Forth shootout originals
"""

from __future__ import annotations

import argparse
import datetime
import json
import platform
import random
import re
import statistics
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple


REPO_ROOT = Path(__file__).resolve().parents[2]
JOY_DIR = REPO_ROOT / "benchmark" / "joy" / "shootout"
JOY_EXTRA = REPO_ROOT / "benchmark" / "joy" / "axis-b"
LOG_ROOT = REPO_ROOT / "logs" / "joybench"

DEFAULT_ENGINES = ["rpyjoy-c", "rpyjoy-c-stkfrag"]

# Expected integer results, verified against the Forth originals
# (shootout/*.fs on ./rpyforth-c / gforth) or closed form.
EXPECTED: Dict[str, int] = {
    "ack.joy": 2045,
    "fibo.joy": 165580141,
    "sieve.joy": 1028,
    "nestedloop.joy": 729000000,
    "ary.joy": 1000,
    "recurse.joy": 18,
    "callheavy.joy": 16000000,
    "composite.joy": 895583921,
    "matrix.joy": 15205770,
    "methcall.joy": 1000001,
    "except.joy": 500000500000,
    "random.joy": 36792695,
    "heap.joy": 999707,
    # Axis B
    "nrev.joy": 1250025000,
    "sum.joy": 1250025000,
    "fact.joy": 479001600,
    "sumtree.joy": 860,
    "tak.joy": 3,
    "mapfold.joy": 1250075000,
    "step.joy": 1250025000,
    "filter.joy": 25000,
    "quotheavy.joy": 3200000,
    "fibrec.joy": 317811,
    "fib.joy": 102334155,
}

# Per-iteration timeout (seconds); scaled by iteration count in clock mode.
TIMEOUT: Dict[str, int] = {
    "ack.joy": 60,
    "fibo.joy": 300,
    "sieve.joy": 120,
    "nestedloop.joy": 180,
    "ary.joy": 60,
    "recurse.joy": 120,
    "callheavy.joy": 120,
    "composite.joy": 600,
    "matrix.joy": 120,
    "methcall.joy": 240,
    "except.joy": 60,
    "random.joy": 180,
    "heap.joy": 60,
    "nrev.joy": 120,
    "sum.joy": 120,
    "mapfold.joy": 120,
    "step.joy": 120,
    "filter.joy": 120,
    "quotheavy.joy": 120,
    "tak.joy": 180,
    "fibrec.joy": 60,
    "fact.joy": 60,
    "sumtree.joy": 60,
    "fib.joy": 60,
}

DEFAULT_TIMEOUT = 120

# Same problem, reduced size (immutable-list port is asymptotically more
# expensive than the Forth array code); result value still matches Forth.
REDUCED_BENCH = frozenset({
    "sieve.joy",     # filter-sieve; 100 reps instead of 10000
    "matrix.joy",    # list matmul; 300 reps instead of 3000
    "ary.joy",       # zip-add lists; length 1000 instead of 30096
    "composite.joy", # inherits the sieve/ary/heap reductions
})

# Same result value, but different machinery measured (see file comments).
ALTERED_BENCH = frozenset({
    "except.joy",    # no catch/throw in Joy: plain parity-count loop
    "heap.joy",      # no floats/arrays: LCG max instead of heapsort
})

# Axis A: Forth-homologous micros (paper claim 1).
AXIS_A = [
    "nestedloop.joy",
    "fibo.joy",
    "callheavy.joy",
    "random.joy",
]

# Axis B: Joy combinator / list / recursion pressure (paper claim 2).
AXIS_B = [
    "nrev.joy",
    "sum.joy",
    "mapfold.joy",
    "step.joy",
    "filter.joy",
    "quotheavy.joy",
    "fact.joy",
    "fibrec.joy",
    "tak.joy",
    "sumtree.joy",
]

PAPER_ORDER = AXIS_A + AXIS_B

SHOOTOUT_ORDER = [
    "ack.joy",
    "fibo.joy",
    "sieve.joy",
    "nestedloop.joy",
    "ary.joy",
    "recurse.joy",
    "callheavy.joy",
    "composite.joy",
    "matrix.joy",
    "methcall.joy",
    "except.joy",
    "random.joy",
    "heap.joy",
]

ALL_ORDER = SHOOTOUT_ORDER + AXIS_B


@dataclass
class EngineSpec:
    name: str
    path: Path
    has_clock: bool = False


@dataclass
class RunOutcome:
    status: str = "ok"          # ok | error | timeout | overflow
    result: Optional[int] = None
    times: List[int] = field(default_factory=list)
    mode: str = "clock"          # clock | elapsed
    error: str = ""


def median_ci(samples: List[int], confidence: float = 0.90,
              resamples: int = 2000) -> Tuple[Optional[float], float]:
    if not samples:
        return (None, 0.0)
    med = statistics.median(samples)
    if len(samples) == 1 or med == 0:
        return (med, 0.0)
    rng = random.Random(20240709)
    n = len(samples)
    boot = sorted(
        statistics.median(samples[rng.randrange(n)] for _ in range(n))
        for _ in range(resamples)
    )
    lo = boot[int((1.0 - confidence) / 2 * resamples)]
    hi = boot[min(resamples - 1, int((1.0 + confidence) / 2 * resamples))]
    return (med, 100.0 * (hi - lo) / 2.0 / med)


def git_revision(root: Path) -> str:
    def _git(cmd):
        return subprocess.check_output(
            ["git"] + cmd, cwd=str(root), stderr=subprocess.DEVNULL
        ).decode().strip()
    try:
        rev = _git(["rev-parse", "--short", "HEAD"])
    except (subprocess.CalledProcessError, OSError):
        return "unknown"
    try:
        if _git(["status", "--porcelain"]):
            rev += "-dirty"
    except (subprocess.CalledProcessError, OSError):
        pass
    return rev


def resolve_bench(name: str) -> Path:
    if (JOY_DIR / name).is_file():
        return JOY_DIR / name
    if (JOY_EXTRA / name).is_file():
        return JOY_EXTRA / name
    raise RuntimeError("missing benchmark: %s" % name)


def discover_benchmarks(suite: str) -> List[Path]:
    order = {
        "shootout": SHOOTOUT_ORDER,
        "axis-a": AXIS_A,
        "axis-b": AXIS_B,
        "paper": PAPER_ORDER,
        "all": ALL_ORDER,
    }.get(suite)
    if order is None:
        raise RuntimeError("unknown suite: %s" % suite)
    return [resolve_bench(n) for n in order]


def resolve_engines(spec: str) -> List[EngineSpec]:
    engines = []
    names = spec.split(",") if spec else DEFAULT_ENGINES
    for name in names:
        name = name.strip()
        if not name:
            continue
        path = Path(name)
        if not path.is_absolute():
            path = REPO_ROOT / name
        if not path.is_file():
            print("WARNING: engine %s not found at %s -- skipping"
                  % (name, path), file=sys.stderr)
            continue
        engines.append(EngineSpec(name=Path(name).name, path=path))
    return engines


def probe_clock(engine: EngineSpec, tmpdir: str) -> bool:
    """True if the engine supports the `clock` word ( -- usec )."""
    probe = Path(tmpdir) / "clock_probe.joy"
    probe.write_text("clock 0 >=\n", encoding="utf-8")
    try:
        proc = subprocess.run(
            [str(engine.path), str(probe)],
            capture_output=True, text=True, timeout=20,
        )
    except (subprocess.TimeoutExpired, OSError):
        return False
    return proc.returncode == 0 and "Result: 1" in proc.stdout


_COMMENT_RE = re.compile(r"\(\*.*?\*\)", re.DOTALL)
_DEFINE_RE = re.compile(r"DEFINE\b.*?\.", re.DOTALL)


def build_driver(source: str, iterations: int) -> str:
    """Wrap a benchmark in a per-iteration timing loop using `clock`.

    Definitions stay at the top; the top-level code runs `iterations` times
    inside one process. Each iteration prints "<i> ,<usec>". Stack invariant
    across iterations is (i result), result on top; the final result is left
    for the interpreter's "Result:" line.
    """
    text = _COMMENT_RE.sub(" ", source)
    defines = _DEFINE_RE.findall(text)
    main = _DEFINE_RE.sub(" ", text).strip()
    if not main:
        raise RuntimeError("benchmark has no top-level code")
    parts = []
    parts.extend(defines)
    parts.append("")
    parts.append("0 0")
    parts.append("%d [" % iterations)
    parts.append("  pop")
    parts.append("  clock")
    parts.append(main)
    parts.append("  swap clock swap -")
    parts.append("  [ [ dup put ] dip ] dip")
    parts.append('  " ," putchars')
    parts.append("  dup put")
    parts.append('  "')
    parts.append('" putchars')
    parts.append("  pop")
    parts.append("  [ succ ] dip")
    parts.append("] times")
    parts.append("nip")
    return "\n".join(parts) + "\n"


def parse_curve_output(stdout: str) -> List[int]:
    """Per-iteration timings: lines "<i> ,<usec>" (run_ablation convention)."""
    times = []
    for line in stdout.splitlines():
        line = line.strip()
        if not line or line.lower().startswith("iteration"):
            continue
        parts = [p.strip() for p in line.split(",")]
        if len(parts) >= 2 and parts[0].isdigit():
            try:
                times.append(int(parts[1]))
            except ValueError:
                continue
    return times


def _classify_failure(proc_stdout: str, proc_stderr: str, rc: int) -> Tuple[str, str]:
    err = (proc_stderr or proc_stdout or "exit %d" % rc).strip()
    if "StackOverflow" in err or "StackOverflow" in proc_stdout:
        return ("overflow", "stack overflow")
    if "call stack overflow" in err or "call stack overflow" in proc_stdout:
        return ("overflow", "call stack overflow")
    return ("error", err.splitlines()[-1][:160] if err else "exit %d" % rc)


def _run_proc(cmd: List[str], timeout: int):
    return subprocess.run(
        cmd, capture_output=True, text=True, timeout=timeout,
        cwd=str(REPO_ROOT), stdin=subprocess.DEVNULL,
    )


def run_bench(engine: EngineSpec, bench: Path, iterations: int,
              timeout: int, pin: Optional[int], tmpdir: str) -> RunOutcome:
    wrapper = ["taskset", "-c", str(pin)] if pin is not None else []
    out = RunOutcome()

    if engine.has_clock:
        out.mode = "clock"
        driver = build_driver(bench.read_text(encoding="utf-8"), iterations)
        driver_path = Path(tmpdir) / ("%s_%s_driver.joy"
                                      % (bench.stem, engine.name))
        driver_path.write_text(driver, encoding="utf-8")
        total_timeout = timeout * iterations + 60
        try:
            proc = _run_proc(wrapper + [str(engine.path), str(driver_path)],
                             total_timeout)
        except subprocess.TimeoutExpired:
            out.status = "timeout"
            out.error = "timeout (%ds)" % total_timeout
            return out
        if proc.returncode != 0:
            out.status, out.error = _classify_failure(
                proc.stdout, proc.stderr, proc.returncode)
            return out
        m = re.search(r"Result:\s*(-?\d+)", proc.stdout)
        if not m:
            out.status = "error"
            out.error = "missing Result in output"
            return out
        out.result = int(m.group(1))
        out.times = parse_curve_output(proc.stdout)
        if len(out.times) != iterations:
            out.status = "error"
            out.error = ("expected %d iteration timings, got %d"
                         % (iterations, len(out.times)))
        return out

    # Fallback: no `clock` word -- one process per iteration, whole-run
    # "Elapsed: <n> usec" (includes startup and JIT warmup every time).
    out.mode = "elapsed"
    for _ in range(iterations):
        t0 = time.perf_counter()
        try:
            proc = _run_proc(wrapper + [str(engine.path), str(bench)], timeout)
        except subprocess.TimeoutExpired:
            out.status = "timeout"
            out.error = "timeout (%ds)" % timeout
            return out
        wall_usec = int((time.perf_counter() - t0) * 1_000_000)
        if proc.returncode != 0:
            out.status, out.error = _classify_failure(
                proc.stdout, proc.stderr, proc.returncode)
            return out
        m = re.search(r"Result:\s*(-?\d+)", proc.stdout)
        if not m:
            out.status = "error"
            out.error = "missing Result in output"
            return out
        out.result = int(m.group(1))
        e = re.search(r"Elapsed:\s*(\d+)\s*usec", proc.stdout)
        out.times.append(int(e.group(1)) if e else wall_usec)
    return out


def axis_of(name: str) -> str:
    if name in AXIS_A:
        return "A"
    if name in AXIS_B:
        return "B"
    return "-"


def tag_of(name: str) -> str:
    if name in REDUCED_BENCH:
        return "reduced"
    if name in ALTERED_BENCH:
        return "altered"
    return "full"


def fmt_usec(v) -> str:
    if v is None:
        return "-"
    return "%d" % int(v)


def main() -> int:
    parser = argparse.ArgumentParser(description="Run rpyjoy benchmarks")
    parser.add_argument("--suite", default="paper",
                        choices=["paper", "axis-a", "axis-b", "shootout", "all"],
                        help="paper=Axis A+B (default); shootout=13 homologs; "
                             "all=shootout+axis-b")
    parser.add_argument("--benchmarks", default="",
                        help="comma-separated subset filter (e.g. sieve.joy,nrev.joy)")
    parser.add_argument("--iterations", type=int, default=3)
    parser.add_argument("--timeout", type=int, default=None,
                        help="per-iteration timeout override (seconds)")
    parser.add_argument("--pin", type=int, default=None,
                        help="pin benchmark processes to this CPU core (taskset -c)")
    parser.add_argument("--engines", default="",
                        help="comma-separated engine binaries "
                             "(default: %s)" % ",".join(DEFAULT_ENGINES))
    parser.add_argument("--no-save", action="store_true",
                        help="do not write logs/joybench/<rev>/")
    parser.add_argument("--list-stubs", action="store_true",
                        help="print benchmarks that are reduced or altered "
                             "relative to the Forth original")
    args = parser.parse_args()

    if args.list_stubs:
        for name in sorted(REDUCED_BENCH):
            print("%s (reduced)" % name)
        for name in sorted(ALTERED_BENCH):
            print("%s (altered)" % name)
        return 0

    benches = discover_benchmarks(args.suite)
    if args.benchmarks:
        keep = {n.strip() for n in args.benchmarks.split(",") if n.strip()}
        benches = [b for b in benches if b.name in keep]
        missing = keep - {b.name for b in benches}
        if missing:
            print("WARNING: not in suite '%s': %s"
                  % (args.suite, ", ".join(sorted(missing))), file=sys.stderr)

    engines = resolve_engines(args.engines)
    if not engines:
        print("ERROR: no engines available", file=sys.stderr)
        return 2

    rev = git_revision(REPO_ROOT)
    records = []
    all_ok = True

    with tempfile.TemporaryDirectory(prefix="joybench_") as tmpdir:
        for eng in engines:
            eng.has_clock = probe_clock(eng, tmpdir)
            if not eng.has_clock:
                print("note: %s has no `clock` word; falling back to "
                      "whole-run Elapsed timing" % eng.name)

        for bench in benches:
            name = bench.name
            exp = EXPECTED.get(name)
            timeout = args.timeout or TIMEOUT.get(name, DEFAULT_TIMEOUT)
            for eng in engines:
                outcome = run_bench(eng, bench, args.iterations, timeout,
                                    args.pin, tmpdir)
                med, ci = median_ci(outcome.times)
                if outcome.status == "ok" and exp is not None \
                        and outcome.result != exp:
                    check = "FAIL got %s" % outcome.result
                    all_ok = False
                elif outcome.status == "ok":
                    check = "PASS"
                else:
                    check = outcome.status.upper()
                    all_ok = False
                records.append({
                    "benchmark": name,
                    "engine": eng.name,
                    "axis": axis_of(name),
                    "tag": tag_of(name),
                    "status": outcome.status,
                    "check": check,
                    "result": outcome.result,
                    "expected": exp,
                    "mode": outcome.mode,
                    "times_usec": outcome.times,
                    "median_usec": int(med) if med is not None else None,
                    "ci_pct": round(ci, 2),
                    "error": outcome.error,
                })
                status_str = check if outcome.status == "ok" \
                    else "%s (%s)" % (check, outcome.error)
                print("%-16s %-24s %-10s median=%s usec  %s"
                      % (name, eng.name, outcome.mode,
                         fmt_usec(med), status_str))

    # Summary table: one row per benchmark, one median column per engine.
    eng_names = [e.name for e in engines]
    lines = []
    cpu = platform.processor() or platform.machine()
    lines.append("rpyjoy %s  rev=%s  iterations=%d  pin=%s  (%s)"
                 % (args.suite, rev, args.iterations, args.pin, cpu))
    lines.append("tags: reduced = smaller N than Forth original; "
                 "altered = same result, different machinery (see source)")
    hdr = ["benchmark", "axis", "tag", "check"] + \
          ["%s usec" % n for n in eng_names]
    lines.append(" | ".join(hdr))
    lines.append("-" * (40 + 18 * len(eng_names)))
    by_bench: Dict[str, Dict[str, dict]] = {}
    for r in records:
        by_bench.setdefault(r["benchmark"], {})[r["engine"]] = r
    for bench in benches:
        name = bench.name
        row = by_bench.get(name, {})
        checks = {r["check"] for r in row.values()}
        check = "PASS" if checks == {"PASS"} else \
            ";".join(sorted(c for c in checks if c != "PASS")) or "?"
        cells = [name, axis_of(name), tag_of(name), check]
        for en in eng_names:
            r = row.get(en)
            cells.append(fmt_usec(r["median_usec"]) if r else "-")
        lines.append(" | ".join(cells))
    summary = "\n".join(lines)
    print()
    print(summary)

    if not args.no_save:
        outdir = LOG_ROOT / rev
        outdir.mkdir(parents=True, exist_ok=True)
        stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
        payload = {
            "revision": rev,
            "timestamp": stamp,
            "suite": args.suite,
            "iterations": args.iterations,
            "pin": args.pin,
            "cpu": cpu,
            "engines": [
                {"name": e.name, "path": str(e.path), "clock": e.has_clock}
                for e in engines
            ],
            "results": records,
        }
        json_path = outdir / ("results-%s-%s.json" % (args.suite, stamp))
        json_path.write_text(json.dumps(payload, indent=2) + "\n",
                             encoding="utf-8")
        txt_path = outdir / ("summary-%s-%s.txt" % (args.suite, stamp))
        txt_path.write_text(summary + "\n", encoding="utf-8")
        print()
        print("saved: %s" % json_path)
        print("saved: %s" % txt_path)

    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
