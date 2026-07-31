from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def _chars(inner, c_addr, u):
    return "".join(chr(inner.char_fetch(c_addr + k)) for k in range(u))


def test_xt_to_string_does_not_advance_here():
    # gforth returns a pointer into the word header without allocating; xt>string must not move HERE, or code that interleaves it with `,` / ALLOT (brew's gene compiler builds a token array that way) gets its data spliced apart.
    inner, outer = run_lines([
        ": bar ;",
        "here",
        "111 ,",
        "' bar xt>string 2drop",
        "222 ,",
    ])
    base = inner.pop_ds_int()
    assert inner.cell_fetch_int(base) == 111
    assert inner.cell_fetch_int(base + inner.cell_size_bytes) == 222


def test_xt_to_string_returns_word_name():
    # gforth-verified (via brew's `: xt>string look IF name>string THEN ;`): xt>string ( xt -- addr len ) returns the word's name text.
    inner, _ = run_lines([
        ": foo ;",
        "' foo xt>string",
    ])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert _chars(inner, c, u) == "foo"


def test_xt_to_string_preserves_case():
    inner, _ = run_lines([
        ": MixedCase ;",
        "' MixedCase xt>string",
    ])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert _chars(inner, c, u) == "MixedCase"


def test_xt_to_string_defined_marker():
    # brew guards on [UNDEFINED] xt>string; the word must exist.
    inner, outer = run_lines([
        "[undefined] xt>string [if] 1 [else] 0 [then]",
    ])
    assert inner.pop_ds_int() == 0


def test_see_is_defined():
    # brew guards on [UNDEFINED] see; the word must exist (a definition-display word; not exercised on the benchmark path).
    inner, outer = run_lines([
        "[undefined] see [if] 1 [else] 0 [then]",
    ])
    assert inner.pop_ds_int() == 0
