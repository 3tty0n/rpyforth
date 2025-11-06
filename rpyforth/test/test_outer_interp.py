from rpyforth.objects import W_StringObject, CELL_SIZE_BYTES
from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter

import pytest

def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner

def run_and_pop(line):
    return run(line).pop_ds()

def test_basic_primitives():
    assert run_and_pop(": SQUARE DUP * ; 3 SQUARE").intval == 9
    assert run_and_pop(": INC 1 + ;  5 INC").intval == 6

def test_ZEROs():
    assert run_and_pop("0 0=").intval == -1 # True
    assert run_and_pop("5 0=").intval == 0  # False
    assert run_and_pop("0 0<").intval == 0  # False
    assert run_and_pop("-128 0<").intval == -1
    assert run_and_pop("-128 0>").intval == -0
    assert run_and_pop("47 0>").intval == -1

def test_STORE_FETCH():
    assert run_and_pop("5 0 !    0 @").intval == 5
    assert run_and_pop("VARIABLE X    123 X !    X @").intval == 123
    assert run_and_pop("VARIABLE A    10 A !    A @ 5 + A !    A @").intval == 15
    assert run_and_pop(""": SQUARE DUP * ;    VARIABLE N 7 N !    N @ SQUARE""").intval == 49
    assert run_and_pop(""": SQUARE DUP * ;    VARIABLE N
7 N !    N @ SQUARE""").intval == 49
    assert run_and_pop(""": SQUARE DUP * ;    VARIABLE N
7 N !    N @ SQUARE""").intval == 49
    assert run_and_pop("VARIABLE N   -42 N !   N @").intval == -42

def test_cell_primitives():
    cell_bytes = CELL_SIZE_BYTES
    assert run_and_pop("CELL").intval == cell_bytes
    assert run_and_pop("3 CELLS").intval == 3 * cell_bytes
    assert run_and_pop("VARIABLE X VARIABLE Y Y X -").intval == cell_bytes
    assert run_and_pop("VARIABLE X VARIABLE Y X CELL+ Y -").intval == 0

def test_PNO():
    assert run_and_pop("DECIMAL  12345 <# #S #>").strval == '12345'
    assert run_and_pop("HEX      255   <# #S #>").strval == 'FF'
    assert run_and_pop("BINARY   5     <# #S #>").strval == '101'

def test_drop():
    assert run_and_pop("1 2 DROP").intval == 1

def test_max():
    assert run_and_pop("3 5 MAX").intval == 5

def test_min():
    assert run_and_pop("3 5 MIN").intval == 3

def test_abs():
    assert run_and_pop("-3 ABS").intval == 3
    assert run_and_pop("3 ABS").intval == 3

def test_negate():
    assert run_and_pop("3 NEGATE").intval == -3
    assert run_and_pop("-3 NEGATE").intval == 3

def test_rot():
    assert run_and_pop("1 2 3 ROT").intval == 1

def test_dup2():
    inner = run("1 2 2DUP")
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1

def test_drop2():
    inner = run_and_pop("1 2 3 2DROP").intval == 1

def test_swap2():
    inner = run("1 2 3 4 2SWAP")
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1
    assert inner.pop_ds().intval == 4
    assert inner.pop_ds().intval == 3

def test_over2():
    inner = run("1 2 3 4 2OVER")
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1
    assert inner.pop_ds().intval == 4
    assert inner.pop_ds().intval == 3
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1

def test_mod():
    assert run_and_pop("10 3 MOD").intval == 1
    assert run_and_pop("-20 6 MOD").intval == 4

def test_inc():
    assert run_and_pop("5 1+").intval == 6
    assert run_and_pop("-1 1+").intval == 0

def test_dec():
    assert run_and_pop("5 1-").intval == 4
    assert run_and_pop("0 1-").intval == -1

def test_BRANCH():
    assert run_and_pop(": Z? 0= IF 1 ELSE 2 THEN ; 0 Z?").intval == 1
    assert run_and_pop(": Z? 0= IF 1 ELSE 2 THEN ; 7 Z?").intval == 2
    assert run_and_pop(": T1  1 0= IF 111 ELSE  0 0= IF 222 ELSE 333 THEN THEN ; T1").intval == 222

def test_SDOUBLE_QUOTE():
    str = "Hello, World!"
    inner = run("S\" Hello, World!\"")
    assert inner.pop_ds().intval == len(str)
    assert inner.pop_ds().ptrval == len(str) # Cheating!

# Floating point tests
def test_float_literals():
    assert run_and_pop("1.0").floatval == 1.0
    assert run_and_pop("3.14").floatval == 3.14
    assert run_and_pop("-2.5").floatval == -2.5
    assert run_and_pop("0.").floatval == 0.0

def test_float_addition():
    assert run_and_pop("1.0 2.0 F+").floatval == 3.0
    assert run_and_pop("3.5 2.5 F+").floatval == 6.0
    assert run_and_pop("-1.5 1.5 F+").floatval == 0.0

def test_float_subtraction():
    assert run_and_pop("5.0 3.0 F-").floatval == 2.0
    assert run_and_pop("10.5 2.5 F-").floatval == 8.0
    assert run_and_pop("1.0 5.0 F-").floatval == -4.0

def test_float_multiplication():
    assert run_and_pop("3.0 4.0 F*").floatval == 12.0
    assert run_and_pop("2.5 2.0 F*").floatval == 5.0
    assert run_and_pop("-2.0 3.0 F*").floatval == -6.0

def test_float_division():
    assert run_and_pop("10.0 2.0 F/").floatval == 5.0
    assert run_and_pop("7.0 2.0 F/").floatval == 3.5
    assert run_and_pop("1.0 4.0 F/").floatval == 0.25

def test_float_comparison():
    assert run_and_pop("5.0 3.0 F>").intval == -1  # True
    assert run_and_pop("2.0 8.0 F>").intval == 0   # False
    assert run_and_pop("3.0 3.0 F>").intval == 0   # False

def test_float_swap():
    inner = run("1.0 2.0 FSWAP")
    assert inner.pop_ds().floatval == 1.0
    assert inner.pop_ds().floatval == 2.0

def test_float_in_colon_def():
    assert run_and_pop(": CIRCLE-AREA 3.14159 F* ; 5.0 5.0 F* CIRCLE-AREA").floatval == 78.53975

# DO...LOOP tests
def test_simple_do_loop():
    # : TEST 10 0 DO I LOOP ; should leave 0-9 on stack
    inner = run(": TEST 10 0 DO I LOOP ; TEST")
    results = []
    for i in range(10):
        results.append(inner.pop_ds().intval)
    assert results == list(range(9, -1, -1))  # popped in reverse order

    inner = run(": SUM 0 5 0 DO I + LOOP ; SUM")
    assert inner.pop_ds().intval == 10

    run(": TEST 10 0 DO I . LOOP ; TEST")
    assert run_and_pop(": SUM 0 5 0 DO I + LOOP ; SUM").intval == 10

def test_nested_do_loops():
    inner = run(": NESTED 3 0 DO 3 0 DO I J * LOOP LOOP ; NESTED")
    expected = [0, 0, 0, 0, 1, 2, 0, 2, 4]
    results = []
    for i in range(9):
        results.append(inner.pop_ds().intval)
    assert results == expected[::-1]  # reversed because stack

def test_leave_in_loop():
    inner = run(": EARLY 10 0 DO I DUP 5 = IF LEAVE THEN LOOP ; EARLY")
    results = []
    while inner.ds_ptr > 0:
        results.append(inner.pop_ds().intval)
    assert results == [5, 4, 3, 2, 1, 0]

def test_compare_op():
    assert run_and_pop("5 3 >").intval == -1  # True
    assert run_and_pop("2 8 >").intval == 0   # False
    assert run_and_pop("3 3 >").intval == 0   # False

    assert run_and_pop("5 3 <").intval == 0
    assert run_and_pop("2 8 <").intval == -1
    assert run_and_pop("3 3 <").intval == 0

    assert run_and_pop("5 5 =").intval == -1  # True
    assert run_and_pop("3 7 =").intval == 0   # False
    assert run_and_pop("0 0 =").intval == -1  # True

def test_pick():
    assert run_and_pop("10 20 30 0 PICK").intval == 30
    assert run_and_pop("10 20 30 1 PICK").intval == 20
    assert run_and_pop("10 20 30 2 PICK").intval == 10

def test_char_bracket():
    # [CHAR] A should compile character code for 'A'
    assert run_and_pop(": TEST [CHAR] A ; TEST").intval == ord('A')
    assert run_and_pop(": TEST [CHAR] Z ; TEST").intval == ord('Z')
    assert run_and_pop(": TEST [CHAR] 0 ; TEST").intval == ord('0')


def test_float():
    assert run_and_pop("1.0").floatval == 1.0
    assert run_and_pop("3.14").floatval == 3.14

    assert run_and_pop("1.0 2.0 F+").floatval == 3.0
    assert run_and_pop("5.0 3.0 F-").floatval == 2.0
    assert run_and_pop("3.0 4.0 F*").floatval == 12.0
    assert run_and_pop("10.0 2.0 F/").floatval == 5.0

    assert run_and_pop("5.0 3.0 F>").intval == -1
    assert run_and_pop("2.0 8.0 F>").intval == 0
