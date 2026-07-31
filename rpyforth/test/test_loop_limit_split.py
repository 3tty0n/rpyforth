from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_and_pop(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner.pop_ds_int()


def test_literal_limit_do_loop():
    # Compile-time literal limit -> promoting (LOOP) variant.
    assert run_and_pop(": t 0 10 0 DO 1+ LOOP ; t") == 10


def test_constant_limit_do_loop():
    # CONSTANT limit compiles to a LIT, so it counts as literal -> promoting.
    assert run_and_pop("10 CONSTANT TEN  : t 0 TEN 0 DO I + LOOP ; t") == 45


def test_computed_limit_do_loop():
    # Runtime-computed limit (5 3 *) -> non-promoting (LOOPNP) variant.
    assert run_and_pop(": t 0 5 3 * 0 DO 1+ LOOP ; t") == 15


def test_computed_limit_uses_index():
    # Sum of I for I in 0..(15-1) with a computed limit.
    assert run_and_pop(": t 0 5 3 * 0 DO I + LOOP ; t") == 105


def test_nested_literal_limits():
    assert run_and_pop(": t 0 4 0 DO 4 0 DO 1+ LOOP LOOP ; t") == 16


def test_nested_computed_and_literal():
    # Outer computed limit, inner literal limit -> mixed variants, same result.
    assert run_and_pop(": t 0 2 2 * 0 DO 4 0 DO 1+ LOOP LOOP ; t") == 16


def test_computed_limit_plusloop():
    # +LOOP is unaffected by the split (never promotes); verify still correct.
    assert run_and_pop(": t 0 5 2 * 0 DO I + 2 +LOOP ; t") == 20


def test_computed_limit_leave():
    assert run_and_pop(
        ": t 0 50 2 * 0 DO I + I 3 = IF LEAVE THEN LOOP ; t") == 6


def test_computed_limit_qdo_zero_trip():
    # ?DO with equal computed limit and start skips the body.
    assert run_and_pop(": t 0 5 1 * 5 ?DO 1+ LOOP ; t") == 0


def test_computed_limit_qdo_runs():
    assert run_and_pop(": t 0 5 1 * 0 ?DO 1+ LOOP ; t") == 5


def test_negative_computed_limit():
    # Downward +LOOP with a computed (negative) limit -6 (as 2 -3 *). From 0 by -2 until crossing -6: indices 0, -2, -4, -6 -> sum -12.
    assert run_and_pop(": t 0 2 -3 * 0 DO I + -2 +LOOP ; t") == -12


def test_literal_and_computed_same_result():
    # The two variants must agree on identical iteration bounds.
    lit = run_and_pop(": a 0 12 0 DO I + LOOP ; a")
    comp = run_and_pop(": b 0 6 2 * 0 DO I + LOOP ; b")
    assert lit == comp == 66
