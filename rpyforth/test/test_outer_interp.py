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
    assert run_and_pop(""": SQUARE DUP * ;    VARIABLE N
7 N !    N @ SQUARE""").intval == 49

def test_PNO():
    assert run_and_pop("DECIMAL  12345 <# #S #>").strval == '12345'
    assert run_and_pop("HEX      255   <# #S #>").strval == 'FF'
    assert run_and_pop("BINARY   5     <# #S #>").strval == '101'

def test_BRANCH():
    assert run_and_pop(": Z? 0= IF 1 ELSE 2 THEN ; 0 Z?").intval == 1
    assert run_and_pop(": Z? 0= IF 1 ELSE 2 THEN ; 7 Z?").intval == 2
    assert run_and_pop(": T1  1 0= IF 111 ELSE  0 0= IF 222 ELSE 333 THEN THEN ; T1").intval == 222

