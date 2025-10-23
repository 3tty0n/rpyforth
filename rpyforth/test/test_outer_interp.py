from rpyforth.objects import W_StringObject
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

def test_STORE_FETCH():
    assert run_and_pop("5 0 !    0 @").intval == 5
    assert run_and_pop("VARIABLE X    123 X !    X @").intval == 123
    assert run_and_pop("VARIABLE A    10 A !    A @ 5 + A !    A @").intval == 15
    assert run_and_pop(""": SQUARE DUP * ;    VARIABLE N 7 N !    N @ SQUARE""").intval == 49

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
    inner = run("1 2 DUP2")
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1

def test_drop2():
    inner = run_and_pop("1 2 3 DROP2").intval == 1

def test_swap2():
    inner = run("1 2 3 4 SWAP2")
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1
    assert inner.pop_ds().intval == 4
    assert inner.pop_ds().intval == 3

def test_over2():
    inner = run("1 2 3 4 OVER2")
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1
    assert inner.pop_ds().intval == 4
    assert inner.pop_ds().intval == 3
    assert inner.pop_ds().intval == 2
    assert inner.pop_ds().intval == 1


    