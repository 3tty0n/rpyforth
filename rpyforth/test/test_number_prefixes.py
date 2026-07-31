from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def test_dollar_hex_prefix_interpret():
    # $FF is hex regardless of BASE (fcp uses $ pervasively).
    inner = run("$FF $10 $30")
    assert inner.pop_ds_int() == 0x30
    assert inner.pop_ds_int() == 0x10
    assert inner.pop_ds_int() == 0xFF


def test_dollar_hex_prefix_compile():
    inner = run(": t $1F ; t")
    assert inner.pop_ds_int() == 0x1F


def test_dollar_hex_negative():
    inner = run("$-10")
    assert inner.pop_ds_int() == -0x10


def test_hash_decimal_prefix():
    # #99 is decimal even in HEX mode.
    inner = run("HEX #99 DECIMAL")
    assert inner.pop_ds_int() == 99


def test_percent_binary_prefix():
    inner = run("%1010")
    assert inner.pop_ds_int() == 0b1010


def test_ampersand_decimal_prefix():
    # gforth's '&' is a decimal specifier (like '#'); brew's mutation-0.3.fs does `&64 constant max-stack-effect`.
    inner = run("HEX &64 DECIMAL")
    assert inner.pop_ds_int() == 64


def test_dollar_as_constant_value():
    inner = run("$30 CONSTANT COLORMASK  COLORMASK")
    assert inner.pop_ds_int() == 0x30
