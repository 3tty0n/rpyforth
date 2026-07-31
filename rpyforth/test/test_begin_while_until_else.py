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


def test_begin_while_until_else_then():
    # gforth-verified: WHILE's forward branch resolved by ELSE (brew basics.fs char-search-backwards).
    inner, outer = run_lines([
        ": t ( n -- r )",
        "  BEGIN dup 0>",
        "  WHILE 1-",
        "  dup 5 =",
        "  UNTIL 111",
        "  ELSE 222",
        "  THEN ;",
    ])
    outer.interpret_line("7 t")
    assert inner.pop_ds_int() == 111
    outer.interpret_line("3 t")
    assert inner.pop_ds_int() == 222


def test_begin_while_until_then_still_works():
    # Regression: BEGIN WHILE UNTIL THEN (no ELSE) must still compile (fcp >goodVar pattern).
    inner, outer = run_lines([
        ": u ( n -- r )",
        "  BEGIN dup 0>",
        "  WHILE 1-",
        "  dup 5 =",
        "  UNTIL",
        "  THEN ;",
    ])
    outer.interpret_line("7 u")
    assert inner.pop_ds_int() == 5
