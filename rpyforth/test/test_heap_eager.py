from rpyforth.inner_interp import InnerInterpreter
from rpyforth.objects import ZERO


def make_interp():
    return InnerInterpreter()


def test_cell_fetch_int_unwritten_returns_zero():
    inner = make_interp()
    assert inner.cell_fetch_int(0) == 0
    assert inner.cell_fetch_int(10) == 0


def test_cell_fetch_unwritten_returns_ZERO_object():
    inner = make_interp()
    assert inner.cell_fetch(0).getvalue() == 0
    assert inner.cell_fetch(42).getvalue() == 0


def test_char_fetch_unwritten_returns_zero():
    inner = make_interp()
    assert inner.char_fetch(0) == 0
    assert inner.char_fetch(7) == 0


def test_float_fetch_unwritten_returns_zero():
    inner = make_interp()
    assert inner.cell_float_fetch(0) == 0.0
    assert inner.cell_float_fetch(5) == 0.0


def test_cell_store_fetch_roundtrip():
    inner = make_interp()
    inner.cell_store(0, 99)
    assert inner.cell_fetch_int(0) == 99
    inner.cell_store(10, -7)
    assert inner.cell_fetch_int(10) == -7


def test_cell_fetch_after_store_returns_int_object():
    inner = make_interp()
    inner.cell_store(3, 42)
    obj = inner.cell_fetch(3)
    assert obj.intval == 42


def test_char_store_fetch_roundtrip():
    inner = make_interp()
    inner.char_store(0, 65)
    assert inner.char_fetch(0) == 65
    inner.char_store(5, 255)
    assert inner.char_fetch(5) == 255


def test_float_store_fetch_roundtrip():
    inner = make_interp()
    inner.float_store(0, 3.14)
    assert abs(inner.cell_float_fetch(0) - 3.14) < 1e-9
    inner.float_store(4, -1.5)
    assert abs(inner.cell_float_fetch(4) - (-1.5)) < 1e-9


def test_heap_is_not_none_after_init():
    # Heap must be present immediately after init (eager allocation), before any store.
    inner = make_interp()
    assert inner.heap is not None
