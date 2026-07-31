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


def _stack(inner):
    n = inner.ds_int_size()
    return [inner.peek_ds_int(n - 1 - i) for i in range(n)]


def test_roll_three():
    # gforth-verified: 1 2 3 4 3 roll -> 2 3 4 1 (bottom..top)
    inner, _ = run_lines(["1 2 3 4 3 roll"])
    assert _stack(inner) == [2, 3, 4, 1]


def test_roll_two_is_rot():
    inner, _ = run_lines(["1 2 3 2 roll"])
    assert _stack(inner) == [2, 3, 1]


def test_roll_one_is_swap():
    inner, _ = run_lines(["1 2 1 roll"])
    assert _stack(inner) == [2, 1]


def test_roll_zero_noop():
    inner, _ = run_lines(["1 2 3 0 roll"])
    assert _stack(inner) == [1, 2, 3]
