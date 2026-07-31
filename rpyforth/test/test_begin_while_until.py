from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_begin_double_while_repeat_then():
    # ansify.fth xt-skip: two WHILEs share one BEGIN; REPEAT resolves the inner WHILE, THEN resolves the outer.
    inner, outer = run_lines([
        ": skip-zeros ( n -- n' )"
        "  BEGIN DUP WHILE"            # n != 0
        "    DUP 1 AND 0= WHILE"       # n even
        "    2/"                        # halve
        "  REPEAT THEN ;",
    ])
    assert "SKIP-ZEROS" in outer.dict
    inner, _ = run_lines([
        ": skip-zeros ( n -- n' )"
        "  BEGIN DUP WHILE DUP 1 AND 0= WHILE 2/ REPEAT THEN ;",
        "8 skip-zeros",   # 8->4->2->1 (odd, exits via 2nd WHILE): 1
    ])
    assert inner.pop_ds_int() == 1
    inner, _ = run_lines([
        ": skip-zeros ( n -- n' )"
        "  BEGIN DUP WHILE DUP 1 AND 0= WHILE 2/ REPEAT THEN ;",
        "0 skip-zeros",   # 0 exits via 1st WHILE: 0
    ])
    assert inner.pop_ds_int() == 0


def test_begin_while_until_then_defines():
    # fcp >goodVar: BEGIN...WHILE...UNTIL THEN mixes an early WHILE exit with an UNTIL back-branch.
    inner, outer = run_lines([
        ": walk ( n -- n' )"
        "  BEGIN DUP WHILE"          # while n != 0
        "    DUP 1 = IF EXIT THEN"    # stop at 1
        "    1-"                      # decrement
        "  0 UNTIL"                   # UNTIL false -> loop back to BEGIN
        "  THEN ;",
    ])
    assert "WALK" in outer.dict


def test_begin_while_until_then_runs():
    inner, _ = run_lines([
        ": countdown BEGIN DUP WHILE 1- DUP 0= UNTIL THEN ;",
        "5 countdown",
    ])
    assert inner.pop_ds_int() == 0
