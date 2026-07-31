from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def test_value_defines_readable_value():
    # VALUE consumes the initializer; each `x` re-pushes it (depth discriminates a no-op VALUE, which would leave the initializer on the stack).
    inner = run("123 VALUE x  x x")
    assert inner.ds_int_size() == 2
    assert inner.pop_ds_int() == 123
    assert inner.pop_ds_int() == 123


def test_value_to_updates():
    inner = run("123 VALUE x  456 TO x  x x")
    assert inner.ds_int_size() == 2
    assert inner.pop_ds_int() == 456
    assert inner.pop_ds_int() == 456


def test_value_to_in_colon():
    inner = run("10 VALUE c  : bump 5 TO c ;  bump  c c")
    assert inner.ds_int_size() == 2
    assert inner.pop_ds_int() == 5
    assert inner.pop_ds_int() == 5


def test_value_read_in_colon():
    inner = run("7 VALUE k  : twice k k + ;  twice")
    assert inner.ds_int_size() == 1
    assert inner.pop_ds_int() == 14


def test_value_multiple():
    inner = run("1 VALUE a  2 VALUE b  3 TO a  a b +")
    assert inner.ds_int_size() == 1
    assert inner.pop_ds_int() == 5
