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


def test_included_missing_file_is_catchable():
    # gforth-verified: INCLUDED on a non-existent file THROWs, so an enclosing CATCH recovers with a nonzero code (brew probes an optional identity file).
    inner, outer = run_lines([
        ": try-load ['] included catch ;",
        's" /nonexistent/definitely-not-here-xyz123" try-load',
    ])
    code = inner.pop_ds_int()
    assert code != 0
