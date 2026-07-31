from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    outer.interpret_line(line)
    return inner


def mstarslash(line):
    inner = run(line)
    high = inner.pop_ds_int()
    low = inner.pop_ds_int()
    return low, high


def test_basic():
    # 7 0 1 3 m*/ d. -> 2  (gforth)
    assert mstarslash("7 0 1 3 m*/") == (2, 0)


def test_negative_double():
    # -7 -1 1 3 m*/ d. -> -3  (gforth, floored)
    assert mstarslash("-7 -1 1 3 m*/") == (-3, -1)


def test_no_overflow_scale():
    # 100 0 3 7 m*/ d. -> 42  (gforth)
    assert mstarslash("100 0 3 7 m*/") == (42, 0)


def test_triple_precision_overflow():
    # 1000000000000 0 1000000 3 m*/ d. -> 333333333333333333  (gforth) d1*n1 overflows 64 bits, exercising the triple-cell path.
    assert mstarslash("1000000000000 0 1000000 3 m*/") == (333333333333333333, 0)


def test_negative_single():
    # 7 0 -1 3 m*/ d. -> -3  (gforth, floored)
    assert mstarslash("7 0 -1 3 m*/") == (-3, -1)


def test_identity_negative_one():
    # -1 -1 1 1 m*/ d. -> -1  (gforth)
    assert mstarslash("-1 -1 1 1 m*/") == (-1, -1)


def test_positive_identity():
    assert mstarslash("5 0 1 1 m*/") == (5, 0)
