from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_and_pop(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner.pop_ds_int()


def test_qdo_normal():
    # limit != start: runs like DO (5 iterations)
    assert run_and_pop(": t 0 5 0 ?DO 1+ LOOP ; t") == 5


def test_qdo_skips_when_equal():
    # limit == start: body is skipped entirely (the bug ?DO fixes vs DO)
    assert run_and_pop(": t 0 5 5 ?DO 1+ LOOP ; t") == 0


def test_qdo_uses_index():
    # sum of I for I in 0..4
    assert run_and_pop(": t 0 5 0 ?DO I + LOOP ; t") == 10


def test_qdo_with_leave():
    # LEAVE inside ?DO: stop after I reaches 3 -> 0+1+2+3
    assert run_and_pop(": t 0 9 0 ?DO I + I 3 = IF LEAVE THEN LOOP ; t") == 6


def test_qdo_nested():
    # 3 x 3 = 9; inner ?DO skipped on the empty range
    assert run_and_pop(": t 0 3 0 ?DO 3 0 ?DO 1+ LOOP LOOP ; t") == 9
    assert run_and_pop(": t 0 3 0 ?DO 5 5 ?DO 1+ LOOP LOOP ; t") == 0


# CASE/OF/ENDOF/ENDCASE: default clause uses "value SWAP" to leave value below the selector that ENDCASE drops.
_CASE = ": f CASE 1 OF 11 ENDOF 2 OF 22 ENDOF 99 SWAP ENDCASE ;  "


def test_case_first_match():
    assert run_and_pop(_CASE + "1 f") == 11


def test_case_second_match():
    assert run_and_pop(_CASE + "2 f") == 22


def test_case_default():
    assert run_and_pop(_CASE + "7 f") == 99


def test_case_single_clause():
    src = ": g CASE 5 OF 50 ENDOF -1 SWAP ENDCASE ;  "
    assert run_and_pop(src + "5 g") == 50
    assert run_and_pop(src + "8 g") == -1
