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


def test_runtime_colon_defines_noop_word():
    # brew's basics.fs `(zero-offset:)` pattern: a defining word that runs `:` then `POSTPONE ;` to create a no-op named word.
    inner, outer = run_lines([
        ": (zero-offset:) : POSTPONE ; ;",
        "(zero-offset:) foo",
        ": bar foo 99 ;",
        "bar",
    ])
    assert "FOO" in outer.dict
    assert inner.pop_ds_int() == 99


def test_runtime_colon_with_body():
    # A defining word that builds a word with a real body via POSTPONE.
    inner, outer = run_lines([
        ": make-adder : POSTPONE 1+ POSTPONE ; ;",
        "make-adder inc",
        "5 inc",
    ])
    assert inner.pop_ds_int() == 6
