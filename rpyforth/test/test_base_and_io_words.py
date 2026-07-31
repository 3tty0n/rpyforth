from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner


def test_base_fetch_reflects_hex():
    inner = run_lines(["HEX BASE @ DECIMAL"])
    assert inner.pop_ds_int() == 16


def test_base_store_sets_radix():
    # BASE ! changes the radix used to parse subsequent numbers.
    inner = run_lines(["16 BASE !  FF  DECIMAL"])
    assert inner.pop_ds_int() == 0xFF


def test_base_save_restore_roundtrip():
    # BASE @ >R ... R> BASE ! must restore the radix; number parsing sees changes live.
    inner = run_lines([
        "HEX",
        "BASE @ >R DECIMAL",
        "10",          # parsed in DECIMAL -> 10
        "R> BASE !",   # restore HEX
        "10",          # parsed in restored HEX -> 16
        "DECIMAL",
    ])
    assert inner.pop_ds_int() == 16   # the trailing 10 in hex
    assert inner.pop_ds_int() == 10


def test_base_in_colon_body():
    inner = run_lines([
        ": b BASE @ ;",
        "HEX b DECIMAL",
    ])
    assert inner.pop_ds_int() == 16


def test_dot_r_right_justifies(capfd):
    run_lines(["42 5 .R"])
    out, _ = capfd.readouterr()
    assert out == "   42"


def test_key_question_no_input():
    inner = run_lines([": k KEY? ;", "k"])
    assert inner.pop_ds_int() == 0
