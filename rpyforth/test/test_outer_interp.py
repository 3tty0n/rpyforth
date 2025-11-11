from rpyforth.objects import W_StringObject, CELL_SIZE_BYTES
from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter

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

def test_2dup():
    inner = run("1 2 2DUP")
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1

def test_2drop():
    inner = run_and_pop("1 2 3 2DROP").intval == 1

def test_2swap():
    inner = run("1 2 3 4 2SWAP")
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1
    assert inner.pop_ds().intval == 4
    assert inner.pop_ds().intval == 3

def test_2over():
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

def test_EMIT():
    assert run_and_pop('10 65 EMIT').intval == 10

def test_questiondup():
    inner = run("0 ?DUP")
    assert inner.pop_ds().intval == 0
    inner = run("5 ?DUP")
    assert inner.pop_ds().intval == 5
    assert inner.pop_ds().intval == 5

def test_depth():
    assert run_and_pop("0 1 DEPTH").intval == 2
    assert run_and_pop("0 DEPTH").intval == 1
    assert run_and_pop("DEPTH").intval == 0

def test_rshift():
    assert run_and_pop("1 0 RSHIFT").intval == 1
    assert run_and_pop("1 1 RSHIFT").intval == 0
    assert run_and_pop("2 1 RSHIFT").intval == 1
    assert run_and_pop("4 2 RSHIFT").intval == 1
    assert run_and_pop("32768 15 RSHIFT").intval == 1

def test_lshift():
    assert run_and_pop("1 0 LSHIFT").intval == 1
    assert run_and_pop("1 1 LSHIFT").intval == 2
    assert run_and_pop("1 2 LSHIFT").intval == 4
    assert run_and_pop("1 15 LSHIFT").intval == 32768

def test_s_to_d():
    inner = run("1024 S>D")
    assert inner.pop_ds().intval == 0
    assert inner.pop_ds().intval == 1024
    inner = run("-1024 S>D")
    assert inner.pop_ds().intval == -1
    assert inner.pop_ds().intval == -1024

def test_mul_star():
    inner = run("1024 4 M*")
    assert inner.pop_ds().intval == 0  
    assert inner.pop_ds().intval == 4096 
    inner = run("-1024 4 M*")
    assert inner.pop_ds().intval == -1  
    assert inner.pop_ds().intval == -4096
    inner = run("-1024 -4 M*")
    assert inner.pop_ds().intval == 0
    assert inner.pop_ds().intval == 4096
    inner = run("9223372036854775807 2 M*")
    assert inner.pop_ds().intval == 0
    assert inner.pop_ds().intval == -2