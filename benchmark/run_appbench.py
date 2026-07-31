#!/usr/bin/env python3
"""Appbench-1.4 benchmark harness.

Two modes, selected by subcommand:

  steady  (DEFAULT)  Warm STEADY-STATE + per-iteration warm-up curve.
  func               Cold functional + performance grid.

--- steady (default) --------------------------------------------------------

Single-shot appbench wall-clock is unfair to a meta-tracing JIT: a ~1-2s run
spends a large fraction on one-time JIT warm-up (tracing/compiling bridges).
gforth-fast is precompiled native code with zero warm-up, so a cold comparison
measures rpyforth's *compiler*, not its generated code quality.

This mode measures WARM STEADY-STATE performance instead. For each program
and each engine it builds a Forth driver that loads the program ONCE, then runs
the core workload word R times in a single process, timing EACH iteration with
UTIME ( -- d : microseconds, double-cell). It prints one CSV line per iteration
(`i, elapsed_usec`), records ALL iterations (cold ones included so the warm-up
curve is visible), and reports the steady-state as the median of the converged
tail (last ~50%), matching run_shootout.py's parse_curve_output /
steady_state_tail convention.

Deliverables:
  1. A steady-state comparison table (cold first-iter, warm-tail median,
     ratio gforth-fast/rpyforth; >1.0 means rpyforth wins warm).
  2. A warm-up curve PDF: per-iteration time from cold to plateau, all three
     engines on one axis per program, with the steady-state onset marked.

lexex is INCLUDED (see the lexex section below): run.fth is one-shot, but its
compute core (syntax-tree decoration + 1153-state FSM transition-table build +
output-array generation, i.e. everything `lexgen` does except the stt.fth file
write) is made repeatable by loading + parsing the input ONCE and re-running the
core per iteration with a dictionary rewind + followPos reset. Correctness is
preserved: a final saveAllTables + compare against ref.tt still passes.

Per-program core-word / count choices (see SteadySpec below):
  - cd16sim : unit = `150000 clear 150000 steps`  (clear resets state -> repeatable)
  - brainless: unit = benchmark3 x8               (self-contained movegen loops)
  - fcp     : unit = benchThink                    (position set up once in prelude)
  - lexex   : unit = lexcore + dict rewind         (re-do decorate + table build)
  - coremark: unit = steady-unit (2000 iter run)   (CRC self-check each iteration)

Safety: never modifies appbench/appbench-1.4/. Drivers are written to a tmpdir
and run with cwd set to the program dir. Subprocesses use a hard timeout so a
stuck engine is killed.

--- func --------------------------------------------------------------------

Functional + performance benchmark harness for the appbench-1.4 suite.

Programs covered: cd16sim, brainless, fcp, lexex, benchgc, coremark.

Each program is run under gforth (reference), gforth-fast, and rpyforth-c-stkfrag.
Functional status per (program, engine):
    PASS    - stdout matches gforth reference (after normalisation)
    PARTIAL - exit 0 but output differs
    FAIL    - crash / timeout / non-zero exit
"""

import argparse
import copy
import difflib
import json
import re
import shutil
import sys
import tempfile
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple

sys.path.insert(0, str(Path(__file__).resolve().parent))

from benchlib import paths, plotting, procs, suites  # noqa: E402
from benchlib.curves import (  # noqa: E402
    parse_curve_output,
    steady_onset_index,
    steady_state_tail_usec,
)
from benchlib.drivers import build_appbench_driver, write_driver  # noqa: E402
from benchlib.engines import (  # noqa: E402
    ALL_ENGINES,
    ENGINE_BINARY,
    ENGINE_GFORTH,
    ENGINE_GFORTH_FAST,
    ENGINE_RPYFORTH,
    ENGINE_SWIFTFORTH,
    ENGINE_VFXFORTH,
    ENGINES,
    GFORTH_MEM,
    REFERENCE_ENGINE,
    build_engine_cmd,
    engine_color,
    sort_engines,
)
from benchlib.paths import (  # noqa: E402,F401
    APPBENCH_DIR,
    COREMARK_DIR,
    GFORTH_DIR,
    GFORTH_SETUP,
    REPO_ROOT,
    git_revision,
)
from benchlib.procs import STEADY_DEFAULT_TIMEOUT, capture_environment  # noqa: E402
from benchlib.stats import fmt_usec, geomean, median_ci  # noqa: E402

STEADY_DEFAULT_ITERATIONS = 50

FUNC_DEFAULT_TIMEOUT = 300
FUNC_DEFAULT_ITERATIONS = 3


# ===========================================================================
# steady mode (default): warm steady-state + warm-up curve
# ===========================================================================

class SteadySpec:
    """A repeatable appbench workload.

    prelude : code run ONCE before the timing loop (includes + one-time setup).
    unit    : the core workload word(s), timed each iteration. It MUST leave the
              stack balanced and be repeatable (idempotent across iterations).
    """

    def __init__(self, name, workdir, pre_include, include_file, setup, unit,
                 rpy_env=None, prelude=None, gforth_mem=GFORTH_MEM):
        self.name = name
        self.workdir = workdir
        # pre_include : words defined before the program is loaded (e.g. 3drop).
        self.pre_include = pre_include
        # include_file: the program's main source, resolved to an ABSOLUTE path
        #   at driver-build time so `include` works regardless of cwd / engine.
        self.include_file = include_file
        # setup : one-time state set up after loading, before the timing loop.
        self.setup = setup
        # unit  : the core workload word(s), timed each iteration.
        self.unit = unit
        # rpy_env : extra environment for the rpyforth engine only (benchgc needs
        #   a big ALLOCATE region for its GC memory block). gforth ignores it.
        self.rpy_env = rpy_env or {}
        # prelude : raw Forth that REPLACES the default `include <include_file>`
        #   load step. lexex needs it because it is a multi-file program whose
        #   libraries must load in a fixed order before its (truncated) input is
        #   parsed; a single `include` cannot express that. When set, include_file
        #   is ignored and this block is emitted verbatim as the one-time prelude.
        self.prelude = prelude
        # gforth_mem : dataspace size passed as `-m` to gforth/gforth-fast. lexex's
        #   per-iteration table build allots ~3.6 MB and needs a roomy dictionary.
        self.gforth_mem = gforth_mem


# benchgc ALLOCATEs one ~120 MB GC memory block, so the rpyforth engine needs a
# large ALLOCATE region; gforth uses system malloc and ignores it.
BENCHGC_RPY_ENV = {"RPYFORTH_ALLOC_MB": "256"}


class ProgramInfo:
    """Where an appbench program lives and how its source has to be loaded.

    Both modes share these facts; what each mode actually measures (the timed
    unit in steady mode, the one-shot body in func mode) differs and stays in
    the two registries below.
    """

    def __init__(self, workdir, include_file, pre_include="", rpy_env=None):
        self.workdir = workdir
        self.include_file = include_file
        # pre_include : words or values that must exist before the source loads.
        self.pre_include = pre_include
        # rpy_env : extra environment for the rpyforth engine only.
        self.rpy_env = rpy_env or {}


APPBENCH_INFO = {
    "cd16sim": ProgramInfo(
        workdir=APPBENCH_DIR / "cd16sim",
        include_file="bench.f",
        pre_include="[undefined] 3drop [if] : 3drop 2drop drop ; [then]",
    ),
    "brainless": ProgramInfo(
        workdir=APPBENCH_DIR / "brainless",
        include_file="benchmark.fs",
    ),
    "fcp": ProgramInfo(
        workdir=APPBENCH_DIR / "fcp",
        include_file="fcp-1.31-64.f",
    ),
    "benchgc": ProgramInfo(
        workdir=APPBENCH_DIR / "benchgc",
        include_file="bench-gc5.fs",
        # bench-gc5.fs starts with `cells dup constant limit`, so 64000 (the
        # cell count for limit=512000) must be on the stack before it loads.
        pre_include="64000",
        rpy_env=BENCHGC_RPY_ENV,
    ),
    "coremark": ProgramInfo(
        workdir=COREMARK_DIR,
        include_file="coremark.f",
    ),
    "lexex": ProgramInfo(
        workdir=APPBENCH_DIR / "lexex",
        include_file="run.fth",
    ),
}


def _steady_spec(name, setup, unit, prelude=None):
    """A SteadySpec for `name`, taking its load recipe from APPBENCH_INFO."""
    info = APPBENCH_INFO[name]
    return SteadySpec(
        name=name,
        workdir=info.workdir,
        pre_include=info.pre_include,
        include_file=info.include_file,
        setup=setup,
        unit=unit,
        rpy_env=info.rpy_env,
        prelude=prelude,
    )


PROGRAMS = [
    _steady_spec(
        "cd16sim",
        setup="",
        # clear resets the machine state so each 150000-step run is identical.
        # func mode runs the stock `1000000 benchmark` one-shot instead; the two
        # workloads are deliberately different sizes.
        unit="150000 clear 150000 steps",
    ),
    _steady_spec(
        "brainless",
        # benchmark3 sets up its own positions and runs movegen loops -> fully
        # self-contained and repeatable. One call is ~4ms warm; x8 lands the
        # unit in the ~100-300ms window we want.
        setup=": steady-unit "
              "benchmark3 benchmark3 benchmark3 benchmark3 "
              "benchmark3 benchmark3 benchmark3 benchmark3 ;",
        unit="steady-unit",
    ),
    _steady_spec(
        "fcp",
        # bench sets up a fixed position then does best-of-three benchThink.
        # We set the position up ONCE here and time a single benchThink per
        # iteration; benchThink re-runs the depth-5 search from the same
        # position each time (thinker resets its state), so it is repeatable.
        #
        # `' NOOP IS checkTime` neutralises fcp's DEFERred time/keyboard poll.
        # Without it, benchThink's ?thinkAbort fires QUIT when KEY? sees EOF on
        # the DEVNULL stdin (or a wall-time limit trips), truncating the timing
        # loop after a few iterations. Disabling the poll makes each search run
        # the full fixed depth deterministically -> a clean repeatable unit.
        # Use NOOP (fcp's spelling); VFX is case-insensitive but keep the name.
        setup="' NOOP IS checkTime\n"
              'S" setup 1rb2rk/p4ppp/1p1qp1n/3n2N/2pP4/2P3P/PPQ2PBP/R1B1R1K w" '
              "evaluate 5 sd",
        unit="benchThink drop",
    ),
    _steady_spec(
        "benchgc",
        # Loading bench-gc5.fs already runs one (cold) testgc and prints the GC
        # statistics; those non-CSV lines are ignored by the parser. testgc is a
        # self-contained, stack-balanced, repeatable unit: each call allocates
        # ~500 KB of live GC-managed nodes and the collector reclaims them, so the
        # heap stays bounded across iterations (verified: active-end stays ~514000
        # and RSS flat over 20+ runs). One testgc is ~270 ms warm -- in the
        # 100-300 ms window. The RNG seed carries over between calls, which only
        # varies the exact allocation sizes, not the balance or the work amount.
        setup="",
        unit="testgc drop",
    ),
    _steady_spec(
        "coremark",
        # 2000 iterations is ~200 ms warm on gforth-fast (utime ticks); CRC
        # self-check runs inside coremark each timed iteration. func mode runs
        # the stock 131072-iteration workload instead: a cold single-shot run
        # needs to be long enough to dwarf start-up, a per-iteration timed unit
        # does not.
        setup=": steady-unit 2000 0 iterations 2! coremark ;",
        unit="steady-unit",
    ),
]


# --- lexex: make the one-shot generator repeatable ------------------------------
#
# lexex's run.fth is one-shot: it loads ten library files, parses lexinput.fth
# (which builds a syntax tree from regex definitions and, on its LAST line, runs
# `syntaxTree lexgen` to decorate the tree, build the 1153-state FSM transition
# table, generate the output arrays and write stt.fth), then self-checks the
# file against ref.tt. `lexgen` cannot simply be looped: each call
#   * allots the whole transition table with `here to TransTable` + per-state
#     `allot` (~3.6 MB of dictionary per run -> unbounded growth), and
#   * mutates the syntax tree in place (calcFollowPos UNIONS into each leaf's
#     existing followPos set, so a naive re-run reads freed set pointers).
#
# The unit below re-does the COMPUTE CORE (createPositionSet + tree decoration +
# buildTransTable + loadLexTokens + buildLexArrays -- everything lexgen does
# EXCEPT the saveAllTables file write) and makes it repeatable with two resets:
#   1. `zero-followpos` walks the current leaf map and nulls every leaf's
#      followPos, so calcFollowPos re-allocates fresh sets instead of unioning
#      into pointers that the dictionary rewind just reclaimed;
#   2. `savedp @ here - allot` rewinds the dictionary pointer (portable negative
#      ALLOT; rpyforth has no writable `dp`) to a snapshot captured on the first
#      iteration, so the per-run allocations are reused and HERE stays flat.
# The snapshot is captured lazily inside `unit` (on the first call, when no later
# word definition sits above it) so rewinding never frees the running loop word.
#
# Verified on rpyforth, gforth and gforth-fast: after N looped units, one final
# saveAllTables + compare against ref.tt still prints "Output file is correct",
# and HERE is flat across iterations (bounded ~3.6 MB working set).
#
# VFX Forth cannot do this in-process: the second lexcore SIGSEGVs (with or
# without dictionary rewind) and then hangs on "Press E to exit". Steady mode
# therefore uses process-per-iteration for lexex/vfxforth (see
# needs_process_per_iteration): each sample is a fresh VFX process that loads
# once and times a single unit.
#
# lexinput.fth is used UNMODIFIED except for two edits made to a /tmp COPY (the
# real appbench tree is never touched): its last line `syntaxTree lexgen` is
# dropped (so loading it only PARSES, leaving the tree ready), and its
# `s" stt.fth" setOutputFile` is redirected to an absolute /tmp path so the final
# save never writes into appbench/appbench-1.4/.
LEXEX_DIR = APPBENCH_INFO["lexex"].workdir
LEXEX_LIB_FILES = [
    "ansify.fth", "xmini_oof.fth", "sets.fth", "shellsort.fth", "syntaxtree.fth",
    "transitiontable.fth", "lexarrays.fth", "savetables.fth", "userinterface.fth",
    "anstokens.fth",
]
LEXEX_STT_OUT = "/tmp/rpyforth_lexex_stt_out.fth"


def _make_lexex_input_copy():
    """Write a /tmp copy of lexinput.fth that only parses (no lexgen) and whose
    output file is redirected out of the appbench tree. Returns its path."""
    src = (LEXEX_DIR / "lexinput.fth").read_text(encoding="utf-8", errors="replace")
    lines = src.splitlines()
    # Drop the trailing `syntaxTree lexgen` invocation (keep everything up to and
    # including `end-symbols syntaxTree`, which finishes building the tree).
    kept = []
    for ln in lines:
        if ln.strip() == "syntaxTree lexgen":
            break
        kept.append(ln)
    body = "\n".join(kept) + "\n"
    # Redirect the generated-file path so the final save+check never writes into
    # the protected appbench directory.
    body = body.replace('s" stt.fth" setOutputFile',
                        's" %s" setOutputFile' % LEXEX_STT_OUT)
    out = Path(tempfile.gettempdir()) / "rpyforth_lexex_lexinput_notrun.fth"
    out.write_text(body, encoding="utf-8")
    return out


def _lexex_prelude():
    """Build the one-time prelude: ordered library loads, tree parse, and the
    lexcore / reset / unit word definitions."""
    inc = ['s" %s" included' % str(LEXEX_DIR / f) for f in LEXEX_LIB_FILES]
    inc.append('s" %s" included' % str(_make_lexex_input_copy()))
    defs = [
        ": lexcore ( tree -- )",
        '   s" createPositionSet (PSC)" evaluate',
        "   dup updateSyntaxTree dup createLeafMap dup updateFollowPos",
        "   buildTransTable loadLexTokens buildLexArrays ;",
        "variable savedp 0 savedp !",
        ": zero-followpos ( -- )",
        "   maxPosition 1+ 1 ?do",
        "      i cells leaves + @ ?dup if 0 swap followPos ! then",
        "   loop ;",
        ": unit ( -- )",
        "   savedp @ if zero-followpos savedp @ here - allot",
        "   else here savedp ! then",
        "   syntaxTree lexcore ;",
    ]
    return "\n".join(inc + defs)


PROGRAMS.append(
    _steady_spec(
        "lexex",
        setup="",
        # unit re-runs the full compute core (decorate + FSM table build + output
        # arrays) with a dictionary rewind; ~4 s warm on rpyforth (buildTransTable
        # dominates at ~3 s of the 1153-state construction).
        unit="unit",
        # The prelude does its own ordered multi-file load, so include_file is
        # unused here.
        prelude=_lexex_prelude(),
    )
)


# SwiftForth rejects empty DOES> bodies (`CREATE ... DOES> ;` is an illegal
# instruction at the created word). cd16sim's `w:` uses that idiom; ANS CREATE
# alone already returns the body address, so drop DOES> for SF runs only.
_SF_W_WIRE_OLD = ": w:  ( <name> -- ) CREATE -1 , DOES> ;      \\ wire"
_SF_W_WIRE_NEW = ": w:  ( <name> -- ) CREATE -1 , ;             \\ wire"

# VFX Forth mismatches gforth-style `if`/`endif` control-flow items in gc.fs
# (and the CS-ROLL jump-into-loop in sweep1). Patch a /tmp copy: deep-stacks
# mark path, native THEN, and a CS-ROLL-free sweep1.
_VFX_SWEEP1 = """\
: sweep1 ( uactivestart uactiveend -- )
    \\ VFX-safe: no CS-ROLL jump-into-loop; use native THEN
    >r
    begin ( u )
	dup live @ bit-set? 0= IF
	    assert2( dup live @ bit-set? 0= )
	    dup 1+ live @ find-next-bit ( ustart uend )
	    dup r@ u>= IF ( ustart uend )
		assert2( dup r@ = )
		2dup one-chunk
		2dup erase-chunk
		drop grain-num-addr active-end !
		r> drop EXIT
	    THEN
	    2dup free-chunk nip
	THEN
	dup sweep-live
	dup rot - live-grains +!
	dup r@ u>= IF ( u )
	    assert2( dup r@ 1+ = )
	    r> 2drop EXIT
	THEN
    again ;

"""

# core_portme.f ships gforth `utime`; VFX/SwiftForth need their own timers.
_CORE_PORTME_GFORTH_START = "   utime start_time_var 2! ;  \\ gforth"
_CORE_PORTME_GFORTH_STOP = "   utime stop_time_var 2! ;  \\ gforth"
_CORE_PORTME_VFX_START = "   ticks #1000 um* start_time_var 2! ;  \\ Vfx"
_CORE_PORTME_VFX_STOP = "   ticks #1000 um* stop_time_var 2! ;  \\ Vfx"
_CORE_PORTME_SF_START = "   ucounter start_time_var 2! ;  \\ SwiftForth"
_CORE_PORTME_SF_STOP = "   ucounter stop_time_var 2! ;  \\ SwiftForth"


def _patch_core_portme(portme_path, engine):
    text = portme_path.read_text(encoding="utf-8")
    if engine == ENGINE_VFXFORTH:
        start, stop = _CORE_PORTME_VFX_START, _CORE_PORTME_VFX_STOP
    elif engine == ENGINE_SWIFTFORTH:
        start, stop = _CORE_PORTME_SF_START, _CORE_PORTME_SF_STOP
        # SwiftForth lacks ANS d>; coremark's matrix_sum needs it.
        # Avoid matching the commented `\ : d>` stub already in the file.
        if "\n: d>  (" not in text and not text.startswith(": d>  ("):
            d_gt = (
                ": d>  ( d1 d2 -- flag )\n"
                "  2over 2over d= >r\n"
                "  d< r> or invert ;\n\n"
            )
            text = d_gt + text
    else:
        return
    if _CORE_PORTME_GFORTH_START not in text or _CORE_PORTME_GFORTH_STOP not in text:
        raise RuntimeError("core_portme.f: expected gforth utime lines not found")
    text = text.replace(_CORE_PORTME_GFORTH_START, start, 1)
    text = text.replace(_CORE_PORTME_GFORTH_STOP, stop, 1)
    portme_path.write_text(text, encoding="utf-8")


def _patch_benchgc_for_vfx(dst):
    import re

    b5 = dst / "bench-gc5.fs"
    text = b5.read_text(encoding="utf-8")
    old = "15000000 cells false false"
    if old not in text:
        raise RuntimeError("bench-gc5.fs: expected deep-stacks flag line not found")
    b5.write_text(text.replace(old, "15000000 cells false true ", 1), encoding="utf-8")

    gc = dst / "gc.fs"
    src = gc.read_text(encoding="utf-8")
    patched, n = re.subn(
        r": sweep1 \( uactivestart uactiveend -- \).*?\n(?=: set-sweep-sentinel)",
        _VFX_SWEEP1,
        src,
        count=1,
        flags=re.S,
    )
    if n != 1:
        raise RuntimeError("gc.fs: failed to rewrite sweep1 for VFX")
    # gforth `endif` (POSTPONE then) mismatches VFX IF origs; use native THEN.
    patched = patched.replace("endif", "THEN")
    gc.write_text(patched, encoding="utf-8")


def prepare_engine_workdir(engine, spec, tmpdir):
    """Return a workdir safe for the given engine (patched /tmp copy when needed).

    Never modifies appbench/appbench-1.4/ or coremark-src in place.
    """
    workdir = Path(spec.workdir)
    name = getattr(spec, "name", None)

    if engine == ENGINE_SWIFTFORTH and name == "cd16sim":
        dst = Path(tmpdir) / "cd16sim_swiftforth"
        if not dst.is_dir():
            shutil.copytree(workdir, dst)
            pkg = dst / "cd16pkg.vhd"
            text = pkg.read_text(encoding="utf-8")
            if _SF_W_WIRE_OLD not in text:
                raise RuntimeError(
                    "cd16sim SwiftForth patch: expected w: line not found in %s"
                    % pkg
                )
            pkg.write_text(
                text.replace(_SF_W_WIRE_OLD, _SF_W_WIRE_NEW, 1),
                encoding="utf-8",
            )
        return dst

    if engine == ENGINE_VFXFORTH and name == "benchgc":
        dst = Path(tmpdir) / "benchgc_vfxforth"
        if not dst.is_dir():
            shutil.copytree(workdir, dst)
            _patch_benchgc_for_vfx(dst)
        return dst

    if name == "coremark" and engine in (ENGINE_VFXFORTH, ENGINE_SWIFTFORTH):
        dst = Path(tmpdir) / ("coremark_%s" % engine)
        if not dst.is_dir():
            shutil.copytree(workdir, dst)
            _patch_core_portme(dst / "core_portme.f", engine)
        return dst

    return workdir


def prepare_swiftforth_workdir(spec, tmpdir):
    """Backward-compatible alias."""
    return prepare_engine_workdir(ENGINE_SWIFTFORTH, spec, tmpdir)


def with_workdir(spec, workdir):
    """Shallow-copy a SteadySpec/ProgramSpec with an alternate workdir."""
    cloned = copy.copy(spec)
    cloned.workdir = Path(workdir)
    return cloned


def build_driver(spec, iterations, engine):
    """Return Forth source for a driver that times `unit` `iterations` times."""
    return build_appbench_driver(spec, iterations, engine)


def needs_process_per_iteration(engine, spec):
    """True when the engine cannot safely re-run `unit` in one process.

    VFX Forth SIGSEGVs (then hangs on 'Press E to exit') on lexex's second
    lexcore — both with dictionary rewind and without. Steady mode therefore
    launches one fresh VFX process per timed iteration (load once per process,
    time a single unit). Func mode already does this via run_program's loop.
    """
    return engine == ENGINE_VFXFORTH and getattr(spec, "name", None) == "lexex"


def build_cmd(engine, driver_path, spec):
    return build_engine_cmd(engine, driver_path, gforth_mem=spec.gforth_mem,
                            gforth_setup=GFORTH_SETUP)


def steady_state_tail(times, frac=0.5):
    """Median of the converged tail (last `frac`) of a per-iteration curve."""
    return steady_state_tail_usec(times, frac)


def run_engine_process_per_iteration(engine, run_spec, iterations, tmpdir, timeout, pin):
    """Collect N samples by launching one fresh process per timed unit.

    Used when in-process multi-iter is unsafe (lexex on VFX). Each process
    loads the program once and times a single unit; CSV indices are remapped
    to 0..N-1 so downstream warm-tail / curve code sees a normal series.
    `timeout` applies per process (same as func-mode per-run timeout).
    """
    env = procs.engine_env(
        run_spec.rpy_env if engine == ENGINE_RPYFORTH else None
    )

    times = []
    stderr_parts = []
    cmd = None
    rc = 0
    timed_out = False
    t0 = time.perf_counter()

    for i in range(iterations):
        driver = build_driver(run_spec, 1, engine)
        driver_path = write_driver(
            tmpdir, run_spec.name, driver, "%s_iter%d_driver" % (engine, i)
        )
        cmd = build_cmd(engine, driver_path, run_spec)

        out = procs.run_popen(cmd, cwd=run_spec.workdir, env=env,
                              timeout=timeout, pin=pin)
        cmd = out.cmd
        if out.stderr.strip():
            stderr_parts.append(out.stderr.strip())
        sample = parse_curve_output(out.stdout)
        if out.timed_out:
            timed_out = True
            rc = -1
            break
        if out.rc != 0 or not sample:
            rc = out.rc if out.rc != 0 else 1
            break
        times.append(sample[0])
        rc = out.rc

    return {
        "engine": engine,
        "times": times,
        "wall": time.perf_counter() - t0,
        "rc": rc,
        "timed_out": timed_out,
        "stderr": "\n".join(stderr_parts),
        "cmd": cmd,
        "process_per_iteration": True,
    }


def run_engine(engine, spec, iterations, tmpdir, timeout, pin):
    run_spec = spec
    patched = prepare_engine_workdir(engine, spec, tmpdir)
    if patched != Path(spec.workdir):
        run_spec = with_workdir(spec, patched)

    if needs_process_per_iteration(engine, run_spec):
        return run_engine_process_per_iteration(
            engine, run_spec, iterations, tmpdir, timeout, pin
        )

    driver = build_driver(run_spec, iterations, engine)
    driver_path = write_driver(tmpdir, spec.name, driver, "%s_driver" % engine)

    cmd = build_cmd(engine, driver_path, run_spec)

    env = procs.engine_env(
        run_spec.rpy_env if engine == ENGINE_RPYFORTH else None
    )

    out = procs.run_popen(cmd, cwd=run_spec.workdir, env=env,
                          timeout=timeout, pin=pin)
    times = parse_curve_output(out.stdout)
    return {
        "engine": engine,
        "times": times,
        "wall": out.wall,
        "rc": out.rc,
        "timed_out": out.timed_out,
        "stderr": out.stderr,
        "cmd": out.cmd,
    }


def print_table(results, engines, iterations):
    print("")
    print("=" * 100)
    print("STEADY-STATE COMPARISON  (R=%d iterations/program, warm tail = last 50%%)" % iterations)
    print("=" * 100)
    header = "%-10s %-13s %12s %20s %12s" % (
        "program", "engine", "cold[iter0]", "warm[tail-med]", "vs-ref",
    )
    print(header)
    print("-" * 100)
    warm_by_engine = {e: [] for e in engines}
    ratio_by_engine = {e: [] for e in engines}
    for prog in results:
        eng_res = results[prog]
        ref = eng_res.get(REFERENCE_ENGINE)
        ref_warm = steady_state_tail(ref["times"]) if ref and ref["times"] else None
        first_prog_row = True
        for engine in engines:
            r = eng_res.get(engine)
            if not r:
                continue
            times = r["times"]
            cold = times[0] if times else None
            warm = steady_state_tail(times)
            if warm is None:
                warm_disp = "n/a"
            else:
                warm_disp = fmt_usec(warm).strip()
                tail = times[int(len(times) * 0.5):] or times
                _, ci = median_ci(tail)
                if round(ci) > 0:
                    warm_disp = "%s ±%.0f%%" % (warm_disp, ci)
                warm_by_engine[engine].append(float(warm))
            if r["timed_out"]:
                ratio = "TIMEOUT"
            elif not times:
                ratio = "NO-DATA"
            elif engine == REFERENCE_ENGINE:
                ratio = "1.00x (ref)"
            elif ref_warm and warm:
                ratio_v = ref_warm / float(warm)
                ratio = "%.2fx" % ratio_v
                ratio_by_engine[engine].append(ratio_v)
            else:
                ratio = "  -"
            name_col = prog if first_prog_row else ""
            first_prog_row = False
            print("%-10s %-13s %12s %20s %12s" % (
                name_col, engine, fmt_usec(cold), warm_disp, ratio,
            ))
        print("-" * 100)

    geo_warm = []
    geo_ratio = []
    for engine in engines:
        gm_w = geomean(warm_by_engine.get(engine, []))
        geo_warm.append(fmt_usec(gm_w).strip() if gm_w is not None else "-")
        if engine == REFERENCE_ENGINE:
            geo_ratio.append("1.00x (ref)")
        else:
            gm_r = geomean(ratio_by_engine.get(engine, []))
            geo_ratio.append("%.2fx" % gm_r if gm_r is not None else "-")
    # Print geomean as one row per engine to keep the table aligned.
    first = True
    for engine, warm_s, ratio_s in zip(engines, geo_warm, geo_ratio):
        name_col = "geomean" if first else ""
        first = False
        print("%-10s %-13s %12s %20s %12s" % (name_col, engine, "", warm_s, ratio_s))
    print("-" * 100)
    print("Note: 'vs-ref' = %s_warm / engine_warm.  >1.00x => engine beats %s warm."
          % (REFERENCE_ENGINE, REFERENCE_ENGINE))
    print("Note: ± = 90% bootstrap CI of the warm-tail median")
    print("Note: lexex re-runs its compute core (decorate + FSM table build + output")
    print("      arrays) per iteration via a dictionary rewind; the file-writing")
    print("      saveAllTables is excluded and correctness is re-verified against ref.tt.")
    print("")


def make_chart(results, engines, iterations, pdf_path):
    plt = plotting.pyplot()

    progs = list(results.keys())
    n = len(progs)
    cols = min(3, max(1, n))
    rows = (n + cols - 1) // cols
    fig, axes = plt.subplots(rows, cols, figsize=(5 * cols, 3.6 * rows), squeeze=False)
    ordered_engines = sort_engines(engines)
    flat = axes.flatten()

    for idx, prog in enumerate(progs):
        ax = flat[idx]
        eng_res = results[prog]
        for engine in ordered_engines:
            r = eng_res.get(engine)
            if not r or not r["times"]:
                continue
            times_ms = [t / 1000.0 for t in r["times"]]
            xs = list(range(len(times_ms)))
            color = engine_color(engine)
            ax.plot(xs, times_ms, marker="o", markersize=2.5, linewidth=1.2,
                    color=color, label=engine)
            warm = steady_state_tail(r["times"])
            if warm is not None:
                ax.axhline(warm / 1000.0, color=color, linewidth=0.7,
                           linestyle=":", alpha=0.6)
        onset = steady_onset_index(
            eng_res.get(ENGINE_RPYFORTH, {}).get("times", []))
        if onset:
            ax.axvline(onset, color="grey", linestyle="--", linewidth=0.8,
                       alpha=0.7)
            ax.text(onset, ax.get_ylim()[1] * 0.95, " steady-state tail ->",
                    fontsize=7, color="grey", va="top")
        ax.set_title("%s : per-iteration warm-up curve" % prog)
        ax.set_xlabel("iteration (0 = cold)")
        ax.set_ylabel("iteration time (ms)")
        ax.grid(True, alpha=0.3)
        ax.legend(loc="upper right", fontsize=8)

    for idx in range(n, rows * cols):
        flat[idx].set_visible(False)

    fig.suptitle(
        "Warm-up curves: rpyforth / gforth-fast / vfxforth / swiftforth\n"
        "dotted line = warm-tail median; dashed vertical = steady-state onset",
        fontsize=11)
    fig.tight_layout(rect=(0, 0, 1, 0.97))
    fig.savefig(str(pdf_path))
    plt.close(fig)


def make_bar_chart(results, engines, pdf_path):
    plt = plotting.pyplot()

    ordered_engines = sort_engines(engines)
    progs = list(results.keys())
    warm = {e: [] for e in ordered_engines}
    for prog in progs:
        for engine in ordered_engines:
            r = results[prog].get(engine)
            med = steady_state_tail(r["times"]) if r and r.get("times") else None
            warm[engine].append(float(med) if med is not None else None)

    names = list(progs) + ["geomean"]
    warm_g = {
        e: warm[e] + [geomean(warm[e])]
        for e in ordered_engines
    }
    height = max(4.0, len(progs) * 0.55)
    fig, (ax_abs, ax_spd) = plt.subplots(1, 2, figsize=(14, height))

    n_cfg = max(1, len(ordered_engines))
    width = plotting.bar_width(n_cfg)
    deltas = plotting.group_offsets(n_cfg)
    y = range(len(names))
    for j, engine in enumerate(ordered_engines):
        offsets = [i + deltas[j] for i in y]
        heights = [v if v is not None else 0 for v in warm_g[engine]]
        ax_abs.barh(offsets, heights, width, label=engine, color=engine_color(engine))
    plotting.add_geomean_separator(ax_abs, len(names))
    ax_abs.set_xscale("log")
    ax_abs.set_yticks(list(y))
    ax_abs.set_yticklabels(names)
    ax_abs.set_xlabel("Warm-tail median (usec, log)")
    ax_abs.set_title("Elapsed time per engine")
    ax_abs.legend(fontsize=8)
    ax_abs.grid(axis="x", linestyle="--", alpha=0.5)

    baselines = [e for e in ordered_engines if e != ENGINE_RPYFORTH]
    rpy_vals = warm.get(ENGINE_RPYFORTH, [])
    if baselines and any(v is not None for v in rpy_vals):
        n_base = max(1, len(baselines))
        width_s = plotting.bar_width(n_base)
        deltas_s = plotting.group_offsets(n_base)
        for j, engine in enumerate(baselines):
            offsets = [i + deltas_s[j] for i in y]
            speedups = []
            for o, r in zip(warm[engine], rpy_vals):
                if o is not None and r:
                    speedups.append(o / r)
                else:
                    speedups.append(None)
            speedups = speedups + [geomean(speedups)]
            heights = [v if v is not None else 0 for v in speedups]
            ax_spd.barh(offsets, heights, width_s, label="vs " + engine,
                        color=engine_color(engine))
        ax_spd.axvline(1.0, color="black", linestyle="--", linewidth=1)
        plotting.add_geomean_separator(ax_spd, len(names))
        ax_spd.set_yticks(list(y))
        ax_spd.set_yticklabels(names)
        ax_spd.set_xlabel("Speedup = baseline / rpyforth (>1 means rpyforth faster)")
        ax_spd.set_title("rpyforth speedup vs baselines")
        ax_spd.legend(fontsize=8)
        ax_spd.grid(axis="x", linestyle="--", alpha=0.5)
    else:
        ax_spd.text(0.5, 0.5, "No rpyforth baselines", ha="center", va="center",
                    transform=ax_spd.transAxes)
        ax_spd.set_axis_off()

    fig.suptitle("Appbench steady-state (warm-tail median)", fontsize=12)
    fig.tight_layout(rect=(0, 0, 1, 0.96))
    fig.savefig(str(pdf_path), dpi=120)
    plt.close(fig)


def _save_steady_logs(results, engines, log_dir, iterations):
    """Write JSON summary and per-iteration CSV files for steady mode."""
    log_dir.mkdir(parents=True, exist_ok=True)
    csv_dir = log_dir / "csv"
    csv_dir.mkdir(parents=True, exist_ok=True)

    summary = {
        "mode": "steady",
        "iterations": iterations,
        "engines": engines,
        "results": [],
    }
    for prog in sorted(results):
        for engine in engines:
            r = results[prog].get(engine)
            if not r:
                continue
            csv_path = csv_dir / ("%s_%s.csv" % (prog, engine))
            with csv_path.open("w", encoding="utf-8") as f:
                f.write("iteration,elapsed_usec\n")
                for i, t in enumerate(r["times"]):
                    f.write("%d,%d\n" % (i, t))
            warm = steady_state_tail(r["times"])
            warm_ci = 0.0
            if r["times"] and warm is not None:
                tail = r["times"][int(len(r["times"]) * 0.5):] or r["times"]
                _, warm_ci = median_ci(tail)
            summary["results"].append({
                "program": prog,
                "engine": engine,
                "times": r["times"],
                "wall_seconds": r["wall"],
                "returncode": r["rc"],
                "timed_out": r["timed_out"],
                "cold_usec": r["times"][0] if r["times"] else None,
                "warm_median_usec": warm,
                "warm_ci_pct": warm_ci,
            })
    json_path = log_dir / "steady_results.json"
    json_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    return json_path, csv_dir


def run_steady(args):
    wanted = suites.select_programs(args.programs, [p.name for p in PROGRAMS])
    selected = [p for p in PROGRAMS if p.name in wanted]

    for engine in args.engines:
        b = ENGINE_BINARY[engine]
        if not Path(b).exists():
            print("WARNING: engine binary missing: %s (%s)" % (engine, b),
                  file=sys.stderr)

    revision = git_revision(REPO_ROOT)
    print(capture_environment(args.pin) + " | commit " + revision)

    out_base = args.output if args.output.is_absolute() else REPO_ROOT / args.output
    log_dir = paths.log_dir(out_base, "appbench", revision, rev_first=True)

    results = {}
    with tempfile.TemporaryDirectory(prefix="appbench_steady_") as tmpdir:
        for spec in selected:
            results[spec.name] = {}
            for engine in args.engines:
                if not Path(ENGINE_BINARY[engine]).exists():
                    continue
                print("running %-10s on %-13s ..." % (spec.name, engine),
                      end="", flush=True)
                r = run_engine(engine, spec, args.iterations, tmpdir,
                               args.timeout, args.pin)
                warm = steady_state_tail(r["times"])
                ppi = " [proc/iter]" if r.get("process_per_iteration") else ""
                if r["timed_out"]:
                    print(" TIMEOUT after %.1fs%s" % (r["wall"], ppi))
                elif not r["times"]:
                    print(" NO CSV DATA (rc=%d)%s" % (r["rc"], ppi))
                    if r["stderr"].strip():
                        print("    stderr: %s" % r["stderr"].strip()[:300])
                else:
                    print(" %d iters, cold=%s warm=%s (%.1fs)%s" % (
                        len(r["times"]), fmt_usec(r["times"][0]),
                        fmt_usec(warm), r["wall"], ppi))
                results[spec.name][engine] = r

    json_path, csv_dir = _save_steady_logs(results, args.engines, log_dir, args.iterations)
    print("Steady logs written to %s" % log_dir)
    print("  JSON: %s" % json_path)
    print("  CSVs: %s" % csv_dir)

    print_table(results, args.engines, args.iterations)

    pdf_path = Path(args.pdf)
    if not pdf_path.is_absolute():
        pdf_path = REPO_ROOT / pdf_path
    try:
        make_chart(results, args.engines, args.iterations, pdf_path)
        print("Warm-up curve chart written to %s" % pdf_path)
    except Exception as exc:
        print("ERROR generating chart: %s" % exc, file=sys.stderr)
        return 1

    bar_path = pdf_path.with_name(pdf_path.stem + "-bars" + pdf_path.suffix)
    try:
        make_bar_chart(results, args.engines, bar_path)
        print("Bar chart written to %s" % bar_path)
    except Exception as exc:
        print("ERROR generating bar chart: %s" % exc, file=sys.stderr)
        return 1

    return 0


# ===========================================================================
# func mode: cold functional + performance grid
# ===========================================================================

@dataclass
class ProgramSpec:
    name: str
    workdir: Path
    prelude: str
    body: str
    supported_engines: List[str]
    rpy_jit_args: List[str] = field(default_factory=list)
    # Extra environment for the rpyforth engine only (gforth ignores it). benchgc
    # ALLOCATEs a large GC memory block, so it needs a big ALLOCATE region.
    rpy_env: Dict[str, str] = field(default_factory=dict)


@dataclass
class RunResult:
    program: str
    engine: str
    returncode: int = 0
    stdout: str = ""
    stderr: str = ""
    wall_seconds: float = 0.0
    timed_out: bool = False
    error_message: str = ""
    elapsed_samples: List[float] = field(default_factory=list)


@dataclass
class FuncStatus:
    status: str
    diff_excerpt: str = ""
    first_error_line: str = ""
    differing_lines: int = 0


def _func_spec(name: str, body: str) -> ProgramSpec:
    """A ProgramSpec for `name`, taking its load recipe from APPBENCH_INFO."""
    info = APPBENCH_INFO[name]
    return ProgramSpec(
        name=name,
        workdir=info.workdir,
        prelude=info.pre_include,
        body=body,
        supported_engines=list(ALL_ENGINES),
        rpy_env=info.rpy_env,
    )


def build_program_registry() -> List[ProgramSpec]:
    # Each body is the stock one-shot workload. Where it differs from the timed
    # unit in steady mode (cd16sim, coremark) that is deliberate: a cold run has
    # to be long enough to dwarf start-up, a per-iteration unit does not.
    return [
        _func_spec("cd16sim", "include bench.f\n1000000 benchmark\nbye"),
        _func_spec("brainless", "include benchmark.fs\nbenchmark\nbye"),
        _func_spec("fcp", "include fcp-1.31-64.f\nbench\nbye"),
        _func_spec("benchgc", "include bench-gc5.fs\nbye"),
        _func_spec(
            "coremark",
            'S" coremark.f" included 131072 0 iterations 2! coremark bye',
        ),
        _func_spec("lexex", "include run.fth\nbye"),
    ]


def build_gforth_cmd(binary: Path, spec: ProgramSpec, tmpdir: Path) -> List[str]:
    forth_expr = ""
    if spec.prelude:
        forth_expr += spec.prelude + " "
    forth_expr += spec.body.replace("\n", " ")

    cmd = [
        str(binary),
        "-m", GFORTH_MEM,
        str(GFORTH_SETUP),
        "-e", forth_expr,
    ]
    return cmd


def build_rpyforth_cmd(binary: Path, spec: ProgramSpec, tmpdir: Path) -> List[str]:
    lines = []
    if spec.prelude:
        lines.append(spec.prelude)
    lines.append(spec.body)
    forth_expr = "\n".join(lines)

    wrapper_path = tmpdir / f"{spec.name}_rpy_wrapper.fs"
    wrapper_path.write_text(forth_expr, encoding="utf-8")

    cmd = [str(binary)] + list(spec.rpy_jit_args) + [str(wrapper_path)]
    return cmd


def build_vfxforth_cmd(binary: Path, spec: ProgramSpec, tmpdir: Path) -> List[str]:
    lines = []
    if spec.prelude:
        lines.append(spec.prelude)
    lines.append(spec.body)
    forth_expr = "\n".join(lines)

    wrapper_path = tmpdir / f"{spec.name}_vfx_wrapper.fs"
    wrapper_path.write_text(forth_expr, encoding="utf-8")

    return [str(binary), str(wrapper_path)]


def build_swiftforth_cmd(binary: Path, spec: ProgramSpec, tmpdir: Path) -> List[str]:
    lines = ["warning off"]
    if spec.prelude:
        lines.append(spec.prelude)
    lines.append(spec.body)
    forth_expr = "\n".join(lines)

    wrapper_path = tmpdir / f"{spec.name}_sf_wrapper.fs"
    wrapper_path.write_text(forth_expr, encoding="utf-8")

    return [str(binary), str(wrapper_path)]


def run_once(
    cmd: List[str],
    workdir: Path,
    timeout: int,
    extra_env: Optional[Dict[str, str]] = None,
) -> Tuple[int, str, str, float, bool]:
    out = procs.run_capture(
        cmd,
        cwd=workdir,
        env=procs.engine_env(extra=extra_env),
        timeout=timeout,
    )
    if out.missing:
        return -2, "", "binary not found: %s" % cmd[0], 0.0, False
    return out.rc, out.stdout, out.stderr, out.wall, out.timed_out


def strip_ansi(text: str) -> str:
    return re.sub(r'\x1b\[[0-9;]*[A-Za-z]', '', text)


def normalise_output(text: str) -> List[str]:
    text = strip_ansi(text)
    lines = []
    for line in text.splitlines():
        line = re.sub(r'\s+', ' ', line).strip()
        if not line:
            continue
        if re.search(r'\b(seconds?|secs|elapsed|ms,|Hz|nps\b)\b', line, re.IGNORECASE):
            continue
        if re.match(r'^Total ticks\b', line, re.IGNORECASE):
            continue
        if re.match(r'^Iterations/Sec\b', line, re.IGNORECASE):
            continue
        if re.match(r'^(?:Gforth|Authors:|Copyright|License|Gforth comes|Type)', line):
            continue
        if re.match(r'^\*terminal\*:', line):
            continue
        if 'warning:' in line.lower() and 'redefined' in line.lower():
            continue
        if 'warning:' in line.lower() and 'original location' in line.lower():
            continue
        if 'warning:' in line.lower() and 'defined literal' in line.lower():
            continue
        if re.match(r'^(?:ok\s*)?$', line):
            continue
        if re.match(r'^Loading run\.fth', line):
            continue
        if re.match(r'^Time taken:', line):
            continue
        if re.match(r'^Elapsed:', line):
            continue
        line = re.sub(r'\b\d+\.\d+\b', '<T>', line)
        lines.append(line)
    return lines


def compute_functional_status(
    ref_stdout: str,
    cand_stdout: str,
    cand_rc: int,
    cand_timed_out: bool,
    cand_stderr: str,
) -> FuncStatus:
    if cand_timed_out:
        first_err = cand_stderr.splitlines()[0] if cand_stderr else "timed out"
        return FuncStatus(status="FAIL", first_error_line=first_err)

    failure_markers = ["UNKNOWN:", "ABORT", "THROW:", "stack underflow", "Stack empty"]
    combined = cand_stdout + cand_stderr
    if cand_rc != 0 or any(m in combined for m in failure_markers):
        first_err = ""
        noise = re.compile(
            r'^(?:Gforth|Authors:|Copyright|License|Gforth comes|Type|\*terminal\*|warning:|\[|Loading |ok\s*$)'
        )
        for text in (cand_stderr, cand_stdout):
            for line in text.splitlines():
                stripped = strip_ansi(line).strip()
                if stripped and not noise.match(stripped):
                    first_err = stripped[:120]
                    break
            if first_err:
                break
        if not first_err:
            first_err = f"exit code {cand_rc}"
        return FuncStatus(status="FAIL", first_error_line=first_err)

    ref_lines = normalise_output(ref_stdout)
    cand_lines = normalise_output(cand_stdout)

    if ref_lines == cand_lines:
        return FuncStatus(status="PASS")

    diff = list(
        difflib.unified_diff(ref_lines, cand_lines, lineterm="", n=2)
    )
    differing = sum(
        1 for line in diff
        if line.startswith(("+", "-")) and not line.startswith(("---", "+++"))
    )
    excerpt = "\n".join(diff[:12])
    return FuncStatus(
        status="PARTIAL",
        diff_excerpt=excerpt,
        differing_lines=differing,
    )


def resolve_engines(overrides: Dict[str, str]) -> Dict[str, Path]:
    result: Dict[str, Path] = {}
    for name, default in ENGINE_BINARY.items():
        override = overrides.get(name)
        if override:
            p = Path(override)
            if not p.is_absolute():
                p = REPO_ROOT / p
            result[name] = p
        else:
            result[name] = default
    return result


def save_log(
    log_dir: Path,
    program: str,
    engine: str,
    iteration: int,
    total: int,
    returncode: int,
    stdout: str,
    stderr: str,
    wall: float,
    timed_out: bool,
    cmd: List[str],
) -> None:
    log_dir.mkdir(parents=True, exist_ok=True)
    suffix = f"_i{iteration:03d}" if total > 1 else ""
    path = log_dir / f"{program}_{engine}{suffix}.log"
    with path.open("w", encoding="utf-8") as f:
        f.write(f"# program: {program}\n")
        f.write(f"# engine: {engine}\n")
        f.write(f"# iteration: {iteration}\n")
        f.write(f"# cmd: {' '.join(cmd)}\n")
        f.write(f"# returncode: {returncode}\n")
        f.write(f"# wall_seconds: {wall:.6f}\n")
        if timed_out:
            f.write("# timed_out: true\n")
        f.write("# --- stdout ---\n")
        f.write(stdout)
        if stderr:
            f.write("\n# --- stderr ---\n")
            f.write(stderr)


def run_program(
    spec: ProgramSpec,
    engine_name: str,
    engine_path: Path,
    tmpdir: Path,
    log_dir: Path,
    iterations: int,
    timeout: int,
) -> RunResult:
    result = RunResult(program=spec.name, engine=engine_name)

    run_spec = spec
    patched = prepare_engine_workdir(engine_name, spec, tmpdir)
    if patched != Path(spec.workdir):
        run_spec = with_workdir(spec, patched)

    extra_env: Optional[Dict[str, str]] = None
    if engine_name == ENGINE_RPYFORTH:
        cmd = build_rpyforth_cmd(engine_path, run_spec, tmpdir)
        if run_spec.rpy_env:
            extra_env = dict(run_spec.rpy_env)
    elif engine_name == ENGINE_VFXFORTH:
        cmd = build_vfxforth_cmd(engine_path, run_spec, tmpdir)
    elif engine_name == ENGINE_SWIFTFORTH:
        cmd = build_swiftforth_cmd(engine_path, run_spec, tmpdir)
    else:
        cmd = build_gforth_cmd(engine_path, run_spec, tmpdir)

    for i in range(1, iterations + 1):
        rc, stdout, stderr, wall, timed_out = run_once(
            cmd, run_spec.workdir, timeout, extra_env
        )
        save_log(
            log_dir, spec.name, engine_name, i, iterations,
            rc, stdout, stderr, wall, timed_out, cmd,
        )
        if i == 1:
            result.returncode = rc
            result.stdout = stdout
            result.stderr = stderr
            result.wall_seconds = wall
            result.timed_out = timed_out
            if timed_out:
                result.error_message = f"timed out after {timeout}s"
            elif rc not in (0, -2):
                result.error_message = f"exit code {rc}"
        result.elapsed_samples.append(wall)

    return result


def print_status_table(
    func_statuses: Dict[Tuple[str, str], FuncStatus],
    programs: List[str],
    engines: List[str],
    timings: Dict[Tuple[str, str], Tuple[Optional[float], float]],
) -> None:
    col_w = 14
    header = f"{'Program':<12}" + "".join(f"{e:>{col_w}}" for e in engines)
    print("=" * (12 + col_w * len(engines)))
    print("Functional status (PASS / PARTIAL / FAIL)")
    print("=" * (12 + col_w * len(engines)))
    print(header)
    print("-" * (12 + col_w * len(engines)))

    for prog in programs:
        row = f"{prog:<12}"
        for eng in engines:
            key = (prog, eng)
            if key not in func_statuses:
                row += f"{'N/A':>{col_w}}"
            else:
                fs = func_statuses[key]
                row += f"{fs.status:>{col_w}}"
        print(row)
    print("=" * (12 + col_w * len(engines)))

    print()
    print("Wall-clock time in seconds (median of iterations, N/A = FAIL/not run)")
    print("-" * (12 + col_w * len(engines)))
    print(header)
    print("-" * (12 + col_w * len(engines)))
    for prog in programs:
        row = f"{prog:<12}"
        for eng in engines:
            key = (prog, eng)
            if key not in timings or timings[key][0] is None:
                row += f"{'N/A':>{col_w}}"
            else:
                med, ci = timings[key]
                cell = f"{med:.2f}s"
                if ci > 0:
                    cell += f" ±{ci:.0f}%"
                row += f"{cell:>{col_w}}"
        print(row)
    print("=" * (12 + col_w * len(engines)))


def print_diff_details(
    func_statuses: Dict[Tuple[str, str], FuncStatus],
    programs: List[str],
    engines: List[str],
) -> None:
    any_printed = False
    for prog in programs:
        for eng in engines:
            key = (prog, eng)
            if key not in func_statuses:
                continue
            fs = func_statuses[key]
            if fs.status == "PARTIAL" and fs.diff_excerpt:
                if not any_printed:
                    print()
                    print("PARTIAL diff excerpts (first 12 diff lines, ref=gforth-fast)")
                    print("=" * 70)
                    any_printed = True
                print(f"\n[{prog} / {eng}]  ({fs.differing_lines} differing lines)")
                for line in fs.diff_excerpt.splitlines()[:12]:
                    print("  " + line)
            elif fs.status == "FAIL" and fs.first_error_line:
                if not any_printed:
                    print()
                    print("FAIL details")
                    print("=" * 70)
                    any_printed = True
                print(f"\n[{prog} / {eng}]  first error: {fs.first_error_line}")


def generate_appbench_chart(
    out_path: Path,
    programs: List[str],
    engines: List[str],
    func_statuses: Dict[Tuple[str, str], FuncStatus],
    timings: Dict[Tuple[str, str], Tuple[Optional[float], float]],
    caption: Optional[str] = None,
) -> None:
    plt = plotting.pyplot()
    from matplotlib import patches as mpatches

    status_colors = {"PASS": "#2ca02c", "PARTIAL": "#ff7f0e", "FAIL": "#d62728", "N/A": "#aaaaaa"}

    fig = plt.figure(figsize=(14, 7), layout="constrained")
    gs = fig.add_gridspec(1, 2, width_ratios=[1, 2], wspace=0.35)

    ax_grid = fig.add_subplot(gs[0])
    n_prog = len(programs)
    n_eng = len(engines)
    grid = []
    for prog in programs:
        row = []
        for eng in engines:
            key = (prog, eng)
            s = func_statuses.get(key, FuncStatus(status="N/A")).status
            color = status_colors.get(s, "#aaaaaa")
            row.append(color)
        grid.append(row)

    for i, prog in enumerate(programs):
        for j, eng in enumerate(engines):
            color = grid[i][j]
            key = (prog, eng)
            s = func_statuses.get(key, FuncStatus(status="N/A")).status
            rect = mpatches.FancyBboxPatch(
                (j - 0.4, i - 0.4), 0.8, 0.8,
                boxstyle="round,pad=0.05",
                linewidth=0.5,
                edgecolor="white",
                facecolor=color,
            )
            ax_grid.add_patch(rect)
            ax_grid.text(j, i, s, ha="center", va="center", fontsize=7,
                         color="white" if s != "N/A" else "black", fontweight="bold")

    ax_grid.set_xlim(-0.6, n_eng - 0.4)
    ax_grid.set_ylim(-0.6, n_prog - 0.4)
    ax_grid.set_xticks(range(n_eng))
    ax_grid.set_xticklabels(engines, rotation=20, ha="right", fontsize=8)
    ax_grid.set_yticks(range(n_prog))
    ax_grid.set_yticklabels(programs, fontsize=9)
    ax_grid.set_title("Functional status", fontsize=10)
    ax_grid.set_aspect("equal")
    plotting.legend_patches(
        ax_grid,
        [(s, c) for s, c in status_colors.items() if s != "N/A"],
        alpha=1.0, loc="upper right", fontsize=7,
    )

    ax_bar = fig.add_subplot(gs[1])
    runnable_progs = []
    for prog in programs:
        has_any = any(
            timings.get((prog, eng), (None, 0))[0] is not None
            for eng in engines
        )
        if has_any:
            runnable_progs.append(prog)

    if runnable_progs:
        width = plotting.bar_width(max(1, n_eng))
        deltas = plotting.group_offsets(max(1, n_eng))
        row_names = runnable_progs + ["geomean"]
        y_pos = range(len(row_names))

        for j, eng in enumerate(engines):
            offsets = [i + deltas[j] for i in y_pos]
            vals = []
            for prog in runnable_progs:
                med, _ = timings.get((prog, eng), (None, 0.0))
                vals.append((med * 1e6) if med is not None else 0)
            gm = geomean(vals) or 0
            ax_bar.barh(
                offsets, vals + [gm], width,
                label=eng,
                color=engine_color(eng),
                alpha=0.85,
            )

        plotting.add_geomean_separator(ax_bar, len(row_names))
        ax_bar.set_xscale("log")
        ax_bar.set_yticks(list(y_pos))
        ax_bar.set_yticklabels(row_names, fontsize=9)
        ax_bar.set_xlabel("Wall-clock time (microseconds, log scale)", fontsize=9)
        ax_bar.set_title("Runtime comparison (runnable subset)", fontsize=10)
        ax_bar.legend(fontsize=8)
        ax_bar.grid(axis="x", linestyle="--", alpha=0.4)
    else:
        ax_bar.text(0.5, 0.5, "No runnable programs", ha="center", va="center",
                    transform=ax_bar.transAxes, fontsize=11, color="0.5")
        ax_bar.set_title("Runtime comparison", fontsize=10)

    fig.suptitle("Appbench-1.4 results", fontsize=13)
    if caption:
        fig.text(0.99, 0.005, caption, ha="right", va="bottom", fontsize=8, color="0.5")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(str(out_path), dpi=120)
    plt.close(fig)


def run_func(args) -> int:
    overrides: Dict[str, str] = {}
    if args.gforth:
        overrides[ENGINE_GFORTH] = args.gforth
    if getattr(args, "gforth_fast", None):
        overrides[ENGINE_GFORTH_FAST] = args.gforth_fast
    if args.rpyforth:
        overrides[ENGINE_RPYFORTH] = args.rpyforth
    if args.vfxforth:
        overrides[ENGINE_VFXFORTH] = args.vfxforth
    if args.swiftforth:
        overrides[ENGINE_SWIFTFORTH] = args.swiftforth

    engine_paths = resolve_engines(overrides)
    selected_engines = args.engines

    for eng in selected_engines:
        if eng not in engine_paths:
            print(f"Error: unknown engine '{eng}'", file=sys.stderr)
            return 1
        p = engine_paths[eng]
        if not p.exists():
            print(f"Warning: {eng} binary not found at {p}", file=sys.stderr)

    revision = git_revision(REPO_ROOT)
    env_line = capture_environment() + f" | commit {revision}"
    print(env_line)

    out_base = args.output if args.output.is_absolute() else REPO_ROOT / args.output
    log_dir = paths.log_dir(out_base, "appbench", revision, rev_first=True)

    specs = build_program_registry()
    if args.only:
        specs = [s for s in specs if s.name == args.only]
        if not specs:
            print(f"Error: no program named '{args.only}'", file=sys.stderr)
            return 1

    all_results: Dict[Tuple[str, str], RunResult] = {}
    func_statuses: Dict[Tuple[str, str], FuncStatus] = {}

    with tempfile.TemporaryDirectory(prefix="appbench_wrappers_") as _tmpdir:
        tmpdir = Path(_tmpdir)

        for spec in specs:
            print(f"\n--- {spec.name} ---", file=sys.stderr)
            for eng in selected_engines:
                ep = engine_paths.get(eng)
                if ep is None or not ep.exists():
                    print(f"  [{spec.name}/{eng}] skip (binary missing)", file=sys.stderr)
                    continue
                print(f"  [{spec.name}/{eng}] running {args.iterations}x ...", file=sys.stderr)
                result = run_program(
                    spec, eng, ep, tmpdir, log_dir,
                    args.iterations, args.timeout,
                )
                all_results[(spec.name, eng)] = result

        print(file=sys.stderr)

        ref_engine = REFERENCE_ENGINE
        for spec in specs:
            ref_key = (spec.name, ref_engine)
            ref = all_results.get(ref_key)
            ref_stdout = ref.stdout if ref else ""

            for eng in selected_engines:
                key = (spec.name, eng)
                if key not in all_results:
                    continue
                result = all_results[key]
                if eng == ref_engine and ref is not None:
                    if not ref.timed_out and ref.returncode == 0:
                        func_statuses[key] = FuncStatus(status="PASS")
                    else:
                        func_statuses[key] = compute_functional_status(
                            ref_stdout, result.stdout, result.returncode,
                            result.timed_out, result.stderr,
                        )
                else:
                    func_statuses[key] = compute_functional_status(
                        ref_stdout, result.stdout, result.returncode,
                        result.timed_out, result.stderr,
                    )

    programs = [s.name for s in specs]

    timings: Dict[Tuple[str, str], Tuple[Optional[float], float]] = {}
    for prog in programs:
        for eng in selected_engines:
            key = (prog, eng)
            result = all_results.get(key)
            if result is None:
                timings[key] = (None, 0.0)
                continue
            if result.timed_out or result.returncode not in (0,):
                timings[key] = (None, 0.0)
                continue
            if not result.elapsed_samples:
                timings[key] = (result.wall_seconds, 0.0)
                continue
            med, ci = median_ci(result.elapsed_samples)
            timings[key] = (med, ci)

    print_status_table(func_statuses, programs, selected_engines, timings)
    print_diff_details(func_statuses, programs, selected_engines)

    json_path = log_dir / "results.json"
    summary = {
        "revision": revision,
        "iterations": args.iterations,
        "timeout": args.timeout,
        "engines": selected_engines,
        "results": [
            {
                "program": prog,
                "engine": eng,
                "status": func_statuses.get((prog, eng), FuncStatus(status="N/A")).status,
                "diff_excerpt": func_statuses.get((prog, eng), FuncStatus(status="N/A")).diff_excerpt,
                "first_error_line": func_statuses.get((prog, eng), FuncStatus(status="N/A")).first_error_line,
                "differing_lines": func_statuses.get((prog, eng), FuncStatus(status="N/A")).differing_lines,
                "wall_median_s": timings.get((prog, eng), (None, 0))[0],
                "wall_ci_pct": timings.get((prog, eng), (None, 0))[1],
                "returncode": all_results.get((prog, eng), RunResult("", "")).returncode,
                "timed_out": all_results.get((prog, eng), RunResult("", "")).timed_out,
            }
            for prog in programs
            for eng in selected_engines
        ],
    }
    json_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(f"\nResults JSON written to {json_path}")

    if args.chart:
        chart_path = args.chart if args.chart.is_absolute() else REPO_ROOT / args.chart
        try:
            generate_appbench_chart(
                chart_path, programs, selected_engines,
                func_statuses, timings,
                caption=f"commit {revision}",
            )
            print(f"Chart written to {chart_path}")
        except RuntimeError as exc:
            print(f"Error generating chart: {exc}", file=sys.stderr)
            return 1

    return 0


# ===========================================================================
# CLI
# ===========================================================================

def _add_steady_args(parser: argparse.ArgumentParser) -> None:
    parser.add_argument("--iterations", type=int, default=STEADY_DEFAULT_ITERATIONS,
                        help="R: workload repetitions per process (default %d)"
                             % STEADY_DEFAULT_ITERATIONS)
    parser.add_argument("--timeout", type=int, default=STEADY_DEFAULT_TIMEOUT,
                        help="per-run timeout in seconds (default %d)" % STEADY_DEFAULT_TIMEOUT)
    parser.add_argument("--pin", type=int, default=None,
                        help="pin runs to this CPU core via taskset -c")
    parser.add_argument("--programs", type=str, default=None,
                        help="comma-separated subset of program names")
    parser.add_argument("--engines", nargs="+", metavar="NAME",
                        default=ENGINES,
                        help="Engines to benchmark (default: %s)" % " ".join(ENGINES))
    parser.add_argument("--pdf", type=str,
                        default=str(REPO_ROOT / "appbench_steady_curves.pdf"),
                        help="output PDF for the warm-up curve chart")
    parser.add_argument("--output", type=Path, default=Path("logs"),
                        help="parent directory for per-run logs and CSV data "
                             "(default: logs/)")


def _add_func_args(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--iterations", type=int, default=FUNC_DEFAULT_ITERATIONS, metavar="N",
        help=f"Timed runs per (program, engine) pair (default: {FUNC_DEFAULT_ITERATIONS})",
    )
    parser.add_argument(
        "--timeout", type=int, default=FUNC_DEFAULT_TIMEOUT,
        help=f"Per-run timeout in seconds (default: {FUNC_DEFAULT_TIMEOUT})",
    )
    parser.add_argument(
        "--only", metavar="NAME", default=None,
        help="Run only the program matching this name",
    )
    parser.add_argument(
        "--chart", type=Path, default=None, metavar="PATH",
        help="Save a PDF/PNG status+timing chart to this path",
    )
    parser.add_argument(
        "--output", type=Path, default=Path("logs"),
        help="Parent directory for per-run logs (default: logs/)",
    )
    parser.add_argument(
        "--engines", nargs="+", metavar="NAME",
        default=[ENGINE_GFORTH, ENGINE_GFORTH_FAST, ENGINE_RPYFORTH],
        help="Engines to benchmark (default: gforth gforth-fast rpyforth; "
             "also supported: vfxforth swiftforth)",
    )
    parser.add_argument(
        "--gforth", metavar="PATH", default=None,
        help="Override path to gforth binary",
    )
    parser.add_argument(
        "--gforth-fast", metavar="PATH", default=None,
        help="Override path to gforth-fast binary",
    )
    parser.add_argument(
        "--rpyforth", metavar="PATH", default=None,
        help="Override path to rpyforth-c-stkfrag binary",
    )
    parser.add_argument(
        "--vfxforth", metavar="PATH", default=None,
        help="Override path to vfxforth runner (default: ./vfxforth.sh)",
    )
    parser.add_argument(
        "--swiftforth", metavar="PATH", default=None,
        help="Override path to swiftforth runner (default: ./swiftforth.sh)",
    )


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        description="Appbench-1.4 harness: warm steady-state (default) or cold "
                    "functional + performance grid.",
    )
    sub = parser.add_subparsers(dest="mode")

    p_steady = sub.add_parser(
        "steady",
        help="warm steady-state + per-iteration warm-up curve (default)",
    )
    _add_steady_args(p_steady)

    p_func = sub.add_parser(
        "func",
        help="cold functional + performance grid",
    )
    _add_func_args(p_func)

    # Make `steady` the default when no subcommand is given: if the first
    # non-flag token is not a known subcommand, prepend `steady`.
    if argv is None:
        argv = sys.argv[1:]
    argv = list(argv)
    if not argv or argv[0] not in ("steady", "func", "-h", "--help"):
        argv = ["steady"] + argv

    args = parser.parse_args(argv)

    if args.mode == "func":
        return run_func(args)
    return run_steady(args)


if __name__ == "__main__":
    sys.exit(main())
