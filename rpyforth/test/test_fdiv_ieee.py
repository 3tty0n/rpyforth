import math

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


def test_fdiv_positive_by_zero_is_inf():
    inner, _ = run_lines(["1e0 0e0 f/"])
    v = inner.pop_ds_float()
    assert math.isinf(v) and v > 0


def test_fdiv_negative_by_zero_is_neg_inf():
    inner, _ = run_lines(["-1e0 0e0 f/"])
    v = inner.pop_ds_float()
    assert math.isinf(v) and v < 0


def test_fdiv_zero_by_zero_is_nan():
    inner, _ = run_lines(["0e0 0e0 f/"])
    v = inner.pop_ds_float()
    assert math.isnan(v)


def test_fdiv_normal_still_works():
    inner, _ = run_lines(["6e0 2e0 f/"])
    assert inner.pop_ds_float() == 3.0


def test_brew_infinity_fconstant():
    # brew defines its IEEE constants this way.
    inner, outer = run_lines([
        "1e0 0e0 f/ FCONSTANT +infinity",
        "-1e0 0e0 f/ FCONSTANT -infinity",
        "+infinity",
    ])
    v = inner.pop_ds_float()
    assert math.isinf(v) and v > 0
