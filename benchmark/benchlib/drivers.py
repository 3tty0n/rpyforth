"""Forth driver generation: load a workload once, then time it N times.

Both suites print the same `i,elapsed_usec` CSV that benchlib.curves parses.
They differ in what one iteration is: an appbench driver calls a workload word,
a shootout driver re-INCLUDEs the benchmark source.

The per-engine timing idiom is the delicate part and lives only here. gforth's
DO LOOP keeps its loop-control indices on the return stack, so the `utime 2>r`
idiom that rpyforth accepts corrupts them and segfaults; gforth therefore
stashes the timestamp in variables. VFXForth has TICKS ( -- ms ) and SwiftForth
has uCOUNTER ( -- d ) instead of UTIME.
"""

from __future__ import annotations

from pathlib import Path
from typing import List

from .engines import (
    ENGINE_RPYFORTH,
    ENGINE_SETUP_INCLUDE,
    ENGINE_SWIFTFORTH,
    ENGINE_VFXFORTH,
)

# A CR both before and after the CSV isolates the data line: the leading CR
# detaches it from anything the workload printed, and the trailing CR keeps the
# next iteration's output from being appended to it.
CSV_LINE = 'cr I . ." ," . cr \\ CSV: i , elapsed_usec'


def timer_words(engine: str) -> tuple:
    """Return (definitions, start_word, elapsed_word) for one engine."""
    if engine == ENGINE_VFXFORTH:
        return (
            [
                "variable _tstart",
                ": _start-timer ticks 1000 * _tstart ! ;",
                ": _elapsed-us ticks 1000 * _tstart @ - ;",
            ],
            "_start-timer",
            "_elapsed-us",
        )
    if engine == ENGINE_SWIFTFORTH:
        return (
            [
                "variable _tlo variable _thi",
                ": _start-timer uCOUNTER _thi ! _tlo ! ;",
                ": _elapsed-us uCOUNTER _tlo @ _thi @ D- drop ;",
            ],
            "_start-timer",
            "_elapsed-us",
        )
    if engine == ENGINE_RPYFORTH:
        # rpyforth's DO LOOP does not store control indices on the return
        # stack, so the cheaper 2>r / 2r> idiom is safe here.
        return (
            [
                ": _start-timer utime 2>r ;",
                ": _elapsed-us utime 2r> D- drop ;",
            ],
            "_start-timer",
            "_elapsed-us",
        )
    return (
        [
            "variable _tlo variable _thi",
            ": _start-timer utime _thi ! _tlo ! ;",
            ": _elapsed-us utime _tlo @ _thi @ D- drop ;",
        ],
        "_start-timer",
        "_elapsed-us",
    )


def build_appbench_driver(spec, iterations: int, engine: str) -> str:
    """Driver that loads an appbench program once and times `spec.unit` N times.

    Elapsed for one iteration is far below 2^31 us (~35 min), so the high cell
    of the double is dropped and the low cell printed as a bare integer.
    """
    lines: List[str] = []

    setup_include = ENGINE_SETUP_INCLUDE.get(engine)
    if setup_include is not None:
        # Mirrors appbench-1.4/run's `include ../setup/<engine>` preamble. VFX
        # needs setup/vfx.fth so ms@ is `ticks` before fcp's timer cascade;
        # otherwise Linux VFX can pick a wrong gettimeofday branch and SIGSEGV.
        lines.append("include %s" % setup_include)

    if spec.pre_include:
        lines.append(spec.pre_include)
    if spec.prelude is not None:
        # Multi-file program (lexex): the prelude does its own ordered loads.
        lines.append(spec.prelude)
    else:
        # Absolute include path, so the driver works from any directory.
        lines.append("include " + str(Path(spec.workdir) / spec.include_file))
    if spec.setup:
        lines.append(spec.setup)

    # VFX Forth SIGSEGVs on nested CATCH while a file is still being INCLUDEd
    # (INCLUDE-FILE installs its own CATCH). fcp's stock benchThink is
    # `['] thinker CATCH ...`, which crashes under our driver include and then
    # sits on "Press E to exit" until the harness timeout. With checkTime=NOOP
    # the CATCH is unnecessary -- call thinker directly.
    if engine == ENGINE_VFXFORTH and getattr(spec, "name", None) == "fcp":
        lines.append("' NOOP IS checkTime")
        lines.append(": benchThink thinker readTimer ;")

    defs, start_word, elapsed_word = timer_words(engine)
    lines.extend(defs)

    lines.append(": steady-run  ( n -- )")
    lines.append("  0 DO")
    lines.append("    " + start_word)
    lines.append("    " + spec.unit)
    lines.append("    " + elapsed_word)
    lines.append("    " + CSV_LINE)
    lines.append("  LOOP ;")
    lines.append("%d steady-run" % iterations)
    lines.append("cr bye")
    return "\n".join(lines) + "\n"


# A shootout source ends in `bye`, which would exit before the next iteration.
# Neutering it and keeping the real exit under `(bye)` lets one process run the
# whole curve.
SHOOTOUT_PREAMBLE = """\
: warnings 2drop ;
: (bye) bye ;
: bye ;
"""


def build_shootout_driver(bench_path, iterations: int, engine: str = ENGINE_RPYFORTH) -> str:
    """Driver that re-INCLUDEs a shootout benchmark `iterations` times."""
    defs, start_word, elapsed_word = timer_words(engine)
    lines = [SHOOTOUT_PREAMBLE.strip(), ""]
    lines.extend(defs)
    lines.append("")
    lines.append(": run-one-iter ( -- usec )")
    lines.append("  " + start_word)
    lines.append('  s" %s" included' % Path(bench_path).resolve())
    lines.append("  " + elapsed_word + " ;")
    lines.append("")
    lines.append(": run-bench ( n -- )")
    lines.append("  0 do")
    lines.append("    run-one-iter")
    lines.append("    " + CSV_LINE)
    lines.append("  loop ;")
    lines.append("")
    lines.append("%d run-bench" % iterations)
    lines.append("(bye)")
    return "\n".join(lines) + "\n"


def write_driver(tmpdir, name: str, source: str, suffix: str = "driver") -> Path:
    """Write a driver next to the other temporaries and return its path."""
    path = Path(tmpdir) / ("%s_%s.fs" % (name, suffix))
    path.write_text(source, encoding="utf-8")
    return path
