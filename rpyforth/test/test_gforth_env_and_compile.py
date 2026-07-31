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


def test_environment_gforth_reports_true():
    inner, _ = run_lines(['S" gforth" ENVIRONMENT?'])
    flag = inner.pop_ds_int()
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert flag == -1
    assert _chars(inner, c, u) == "0.7.9"


def test_environment_floored_is_true():
    # / and MOD are floored here (matching gforth): report FLOORED true.
    inner, _ = run_lines(['S" floored" ENVIRONMENT?'])
    flag = inner.pop_ds_int()
    val = inner.pop_ds_int()
    assert flag == -1
    assert val == -1


def test_bracket_compile_forces_compilation():
    # [COMPILE] name compiles the (possibly immediate) word into the definition.
    inner, outer = run_lines([
        ": inc [COMPILE] 1+ ;",
        "5 inc",
    ])
    assert inner.pop_ds_int() == 6


def test_look_then_name_string_gives_name():
    # gforth.fs defines xt>string as `look IF name>string THEN`; LOOK leaves ( xt -1 ) so that idiom yields the word name.
    inner, outer = run_lines([
        ": foo ;",
        ": xts look IF name>string THEN ;",
        "' foo xts",
    ])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert _chars(inner, c, u) == "foo"
