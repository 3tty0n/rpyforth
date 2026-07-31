from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_and_pop(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner.pop_ds_int()


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for ln in lines:
        outer.interpret_line(ln)
    return inner


def test_defined():
    assert run_and_pop("[DEFINED] DUP") == -1
    assert run_and_pop("[DEFINED] NOTAWORD") == 0


def test_undefined():
    assert run_and_pop("[UNDEFINED] NOTAWORD") == -1
    assert run_and_pop("[UNDEFINED] DUP") == 0


def test_bracket_if_true_runs():
    assert run_and_pop("-1 [IF] 42 [THEN]") == 42


def test_bracket_if_false_skips():
    # the 42 is skipped, leaving the earlier 7
    assert run_and_pop("7  0 [IF] 42 [THEN]") == 7


def test_bracket_if_else_true():
    assert run_and_pop("-1 [IF] 11 [ELSE] 22 [THEN]") == 11


def test_bracket_if_else_false():
    assert run_and_pop("0 [IF] 11 [ELSE] 22 [THEN]") == 22


def test_bracket_if_nested():
    assert run_and_pop("-1 [IF] -1 [IF] 5 [ELSE] 6 [THEN] [ELSE] 7 [THEN]") == 5
    assert run_and_pop("-1 [IF] 0 [IF] 5 [ELSE] 6 [THEN] [ELSE] 7 [THEN]") == 6


def test_bracket_if_skips_definitions():
    # a definition inside a false [IF] is skipped entirely
    assert run_and_pop("0 [IF] : secret 999 ; [THEN]  [DEFINED] secret") == 0


def test_bracket_if_multiline_skip():
    # skip state persists across lines: [IF] on line 1, [THEN] on line 3
    inner = run_lines(["0 [IF]", ": secret 999 ;", "[THEN]", "[DEFINED] secret"])
    assert inner.pop_ds_int() == 0
