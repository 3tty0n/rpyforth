#!/usr/bin/env python3
"""Adapt a gforth-oriented shootout .fs for VFXForth or SwiftForth.

Writes a self-contained temporary program to stdout:
  - defines utime (and recursive, when needed)
  - rewrites gforth `recursive` defs to DEFER form for VFXForth
  - keeps SwiftForth recursion via -SMUDGE

Usage:
  python3 adapt_shootout_for_engine.py vfxforth path/to/bench.fs > /tmp/out.fs
  python3 adapt_shootout_for_engine.py swiftforth path/to/bench.fs > /tmp/out.fs
"""

from __future__ import annotations

import re
import sys
from pathlib import Path


VFX_PRELUDE = r"""\
\ Auto-generated prelude for VFXForth shootout runs.
SetTicks
0 to EditOnError?
: utime ( -- d )
  { | tv[ /timeval ] -- }
  tv[ 0 gettimeofday drop
  tv[ tv_sec @ #1000000 um*
  tv[ tv_nsec @ s>d d+ ;
2variable _nn
variable _nn?
0 _nn? !
: nextname ( c-addr u -- ) _nn 2! -1 _nn? ! ;
: create
  _nn? @ if 0 _nn? ! _nn 2@ ($create)
  else bl word count ($create) then ;
: constant  create , does> @ ;
[undefined] struct [if]
  : cell% 1 cells ;
  : struct ( -- n ) 0 ;
  : field ( offset size "name" -- offset' )
    create over , + does> @ + ;
  : end-struct ( n "name" -- ) constant ;
  : %alloc ( n -- addr ) allocate throw ;
[then]
"""

SF_PRELUDE = r"""\
\ Auto-generated prelude for SwiftForth shootout runs.
warning off
: utime ( -- d ) uCOUNTER ;
: recursive  -smudge ; immediate
2variable _nn
variable _nn?
0 _nn? !
: nextname ( c-addr u -- ) _nn 2! -1 _nn? ! ;
: create
  _nn? @ if 0 _nn? ! _nn 2@ get-current (WID-CREATE)
  else bl word count get-current (WID-CREATE) then ;
: constant  create , does> @ ;
[undefined] struct [if]
  : cell% 1 cells ;
  : struct ( -- n ) 0 ;
  : field ( offset size "name" -- offset' )
    create over , + does> @ + ;
  : end-struct ( n "name" -- ) constant ;
  : %alloc ( n -- addr ) allocate throw ;
[then]
"""


def _split_tokens(line: str) -> list[str]:
    # Strip backslash comments; keep quoted strings intact enough for our needs.
    if "\\" in line:
        in_quote = False
        out = []
        i = 0
        while i < len(line):
            ch = line[i]
            if ch == '"':
                in_quote = not in_quote
                out.append(ch)
            elif ch == "\\" and not in_quote:
                break
            else:
                out.append(ch)
            i += 1
        line = "".join(out)
    return line.split()


def _convert_vfx_recursive(source: str) -> str:
    """Rewrite `: name ... recursive ... ;` into a DEFER-based definition.

    VFXForth's reveal-based RECURSIVE compiles self-calls that work from
    interpret mode but SIGSEGV when invoked from another colon definition.
    """
    lines = source.splitlines(keepends=True)
    out: list[str] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        tokens = _split_tokens(line)
        if not tokens or tokens[0] != ":":
            out.append(line)
            i += 1
            continue

        # Gather a full colon definition (supports multi-line bodies).
        # Account for ';' on the same line as ':' (common for one-liners).
        block = [line]
        depth = 0
        for tk in tokens:
            if tk == ":":
                depth += 1
            elif tk == ";":
                depth -= 1
        j = i + 1
        while j < len(lines) and depth > 0:
            block.append(lines[j])
            tks = _split_tokens(lines[j])
            for tk in tks:
                if tk == ":":
                    depth += 1
                elif tk == ";":
                    depth -= 1
                    if depth == 0:
                        break
            j += 1

        flat = []
        for bl in block:
            flat.extend(_split_tokens(bl))

        # flat: : NAME [stack-effect...] recursive? ... ;
        if len(flat) < 4 or flat[0] != ":" or "recursive" not in flat:
            out.extend(block)
            i = j
            continue

        name = flat[1]

        # Drop stack-effect comments "( ... )" and the recursive marker.
        body: list[str] = []
        k = 2
        while k < len(flat) - 1:  # exclude final ';'
            tok = flat[k]
            if tok == "recursive":
                k += 1
                continue
            if tok.startswith("(") and not tok.endswith(")"):
                k += 1
                while k < len(flat) - 1 and not flat[k].endswith(")"):
                    k += 1
                k += 1
                continue
            if tok.startswith("(") and tok.endswith(")"):
                k += 1
                continue
            # Replace self-calls with the defer name.
            if tok == name:
                body.append(name + "-x")
            else:
                body.append(tok)
            k += 1

        defer = name + "-x"
        impl = "(" + name + "-x)"
        out.append("defer %s\n" % defer)
        out.append(": %s\n" % impl)
        # Pretty-print body with simple wrapping.
        if body:
            out.append("  %s\n" % " ".join(body))
        out.append(";\n")
        out.append("' %s to-do %s\n" % (impl, defer))
        out.append(": %s %s ;\n" % (name, defer))
        i = j

    return "".join(out)


def _rewrite_utime_return_stack(source: str) -> str:
    """Avoid utime+2>R, which SIGSEGVs on SwiftForth at interpret level."""
    source = re.sub(r"(?i)\butime\s+2>R\b", "_start-timer", source)
    source = re.sub(r"(?i)\butime\s+2R>\s*d-", "_elapsed-us", source)
    return source


def _strip_interpret_elapsed(source: str) -> str:
    """Drop gforth-style interpret-level elapsed prints.

    SwiftForth's ." is compile-only at interpret state, and the shootout
    wrappers already append a wall-clock 'Elapsed: N usec' line.
    Keep `_start-timer` / `_elapsed-us` inside colon defs (curve/).
    """
    return re.sub(
        r"(?im)^\s*_elapsed-us\s*\.\s*\"\s*Elapsed:\s*\"\s*d\.\s*\.\s*\"\s*usec\s*\"\s*cr\s*$",
        "",
        source,
    )


TIMER_WORDS = r"""\
2variable _tstart
: _start-timer  utime _tstart 2! ;
: _elapsed-us   utime _tstart 2@ D- ;
"""


def adapt(engine: str, src_path: Path) -> str:
    text = src_path.read_text(encoding="utf-8", errors="replace")
    engine = engine.lower()
    if engine in ("vfx", "vfxforth", "vfxforth.sh"):
        body = _strip_interpret_elapsed(
            _rewrite_utime_return_stack(_convert_vfx_recursive(text))
        )
        return VFX_PRELUDE + "\n" + TIMER_WORDS + "\n" + body
    if engine in ("sf", "swift", "swiftforth", "swiftforth.sh"):
        body = _strip_interpret_elapsed(_rewrite_utime_return_stack(text))
        return SF_PRELUDE + "\n" + TIMER_WORDS + "\n" + body
    raise SystemExit("unknown engine: %s" % engine)


def main(argv: list[str]) -> int:
    if len(argv) != 3:
        print(__doc__.strip(), file=sys.stderr)
        return 2
    engine, path = argv[1], Path(argv[2])
    if not path.is_file():
        print("not a file: %s" % path, file=sys.stderr)
        return 2
    sys.stdout.write(adapt(engine, path))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
