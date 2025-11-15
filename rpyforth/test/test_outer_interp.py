from rpyforth.objects import W_StringObject, CELL_SIZE_BYTES, W_IntObject, W_FloatObject, W_WordObject
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
    #assert run_and_pop("0x8000 0xF RSHIFT").intval == 1

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

def test_bl():
    assert run_and_pop("BL").intval == 32
    #assert run_and_pop("1 0xF LSHIFT").intval == 0x8000

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

def test_CHAR():
    assert run_and_pop("CHAR Hello").intval == ord('H')
    assert run_and_pop("CHAR ello").intval == ord('e')

def test_2STORE():
    inner = run("2VARIABLE buf 10 20 buf 2!")
    assert inner.cell_fetch(W_IntObject(0)).intval == 10
    assert inner.cell_fetch(W_IntObject(1)).intval == 20

def test_s2f():
    """Test S>F (integer to float conversion)"""
    result = run_and_pop("10 S>F")
    assert isinstance(result, W_FloatObject)
    assert result.floatval == 10.0

def test_fstore_ffetch():
    """Test F! and F@"""
    result = run_and_pop("FVARIABLE X  3.14E0 X F!  X F@")
    assert isinstance(result, W_FloatObject)
    assert abs(result.floatval - 3.14) < 0.01

def test_fdup():
    """Test FDUP"""
    inner = run("5.5E0 FDUP")
    f1 = inner.pop_ds()
    f2 = inner.pop_ds()
    assert isinstance(f1, W_FloatObject)
    assert isinstance(f2, W_FloatObject)
    assert f1.floatval == f2.floatval == 5.5

def test_begin_while_repeat():
    """Test BEGIN...WHILE...REPEAT loop"""
    # Count from 0 to 4
    result = run_and_pop(": TEST 0 BEGIN DUP 5 < WHILE 1+ REPEAT ; TEST")
    assert isinstance(result, W_IntObject)
    assert result.intval == 5

def test_to_r():
    """Test >R (to-R) - move value from data stack to return stack"""
    inner = run("42 >R")
    # Data stack should be empty
    assert inner.ds_ptr == 0
    # Return stack should have 42
    result = inner.pop_rs()
    assert isinstance(result, W_IntObject)
    assert result.intval == 42

def test_r_from():
    """Test R> (R-from) - move value from return stack to data stack"""
    inner = run("42 >R R>")
    # Data stack should have 42
    result = inner.pop_ds()
    assert isinstance(result, W_IntObject)
    assert result.intval == 42
    # Return stack should be empty
    assert inner.rs_ptr == 0

def test_r_fetch():
    """Test R@ (R-fetch) - copy value from return stack to data stack"""
    inner = run("42 >R R@")
    # Data stack should have 42
    result = inner.pop_ds()
    assert isinstance(result, W_IntObject)
    assert result.intval == 42
    # Return stack should still have 42
    result2 = inner.pop_rs()
    assert isinstance(result2, W_IntObject)
    assert result2.intval == 42

def test_2to_r():
    """Test 2>R - move two values from data stack to return stack"""
    inner = run("10 20 2>R")
    # Data stack should be empty
    assert inner.ds_ptr == 0
    # Return stack should have 20 on top, 10 below
    result2 = inner.pop_rs()
    result1 = inner.pop_rs()
    assert isinstance(result1, W_IntObject)
    assert isinstance(result2, W_IntObject)
    assert result1.intval == 10
    assert result2.intval == 20

def test_2r_from():
    """Test 2R> - move two values from return stack to data stack"""
    inner = run("10 20 2>R 2R>")
    # Data stack should have 10 and 20
    result2 = inner.pop_ds()
    result1 = inner.pop_ds()
    assert isinstance(result1, W_IntObject)
    assert isinstance(result2, W_IntObject)
    assert result1.intval == 10
    assert result2.intval == 20
    # Return stack should be empty
    assert inner.rs_ptr == 0

def test_2r_fetch():
    """Test 2R@ - copy two values from return stack to data stack"""
    inner = run("10 20 2>R 2R@")
    # Data stack should have 10 and 20
    result2 = inner.pop_ds()
    result1 = inner.pop_ds()
    assert isinstance(result1, W_IntObject)
    assert isinstance(result2, W_IntObject)
    assert result1.intval == 10
    assert result2.intval == 20
    # Return stack should still have 10 and 20
    result2_rs = inner.pop_rs()
    result1_rs = inner.pop_rs()
    assert isinstance(result1_rs, W_IntObject)
    assert isinstance(result2_rs, W_IntObject)
    assert result1_rs.intval == 10
    assert result2_rs.intval == 20

def test_return_stack_complex():
    """Test complex combination of return stack operations"""
    # Test: 1 2 3 >R >R >R R> R> R>
    # Should result in: 3 2 1
    inner = run("1 2 3 >R >R >R R> R> R>")
    result1 = inner.pop_ds()
    result2 = inner.pop_ds()
    result3 = inner.pop_ds()
    assert result1.intval == 3
    assert result2.intval == 2
    assert result3.intval == 1

# Data Space Tests

def test_here():
    """Test HERE - return current data space pointer"""
    inner = run("HERE")
    addr1 = inner.pop_ds()
    assert isinstance(addr1, W_IntObject)
    # HERE should return a valid address
    assert addr1.intval >= 0

def test_comma():
    """Test , (comma) - store value at HERE and increment"""
    inner = run("HERE  42 ,  HERE")
    addr2 = inner.pop_ds()
    addr1 = inner.pop_ds()
    assert isinstance(addr1, W_IntObject)
    assert isinstance(addr2, W_IntObject)
    # addr2 should be addr1 + cell_size_bytes
    assert addr2.intval == addr1.intval + inner.cell_size_bytes
    # Check that 42 was stored at addr1
    stored_val = inner.cell_fetch(addr1)
    assert stored_val.intval == 42

def test_c_comma():
    """Test C, - store character at HERE and increment by 1"""
    inner = run("HERE  65 C,  HERE")
    addr2 = inner.pop_ds()
    addr1 = inner.pop_ds()
    assert isinstance(addr1, W_IntObject)
    assert isinstance(addr2, W_IntObject)
    # addr2 should be addr1 + 1
    assert addr2.intval == addr1.intval + 1

def test_allot():
    """Test ALLOT - allocate n address units"""
    inner = run("HERE  10 ALLOT  HERE")
    addr2 = inner.pop_ds()
    addr1 = inner.pop_ds()
    assert isinstance(addr1, W_IntObject)
    assert isinstance(addr2, W_IntObject)
    # addr2 should be addr1 + 10
    assert addr2.intval == addr1.intval + 10

# Dictionary Tests

def test_create():
    """Test CREATE - create a new word with data field"""
    result = run_and_pop("CREATE MYDATA  MYDATA")
    # MYDATA should push its address
    assert isinstance(result, W_IntObject)
    assert result.intval >= 0

def test_create_with_comma():
    """Test CREATE with , to store data"""
    result = run_and_pop("CREATE MYVAR  123 ,  456 ,  MYVAR")
    # MYVAR should push its address
    addr = result
    # We need to access the inner interpreter to fetch values
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("CREATE MYVAR2  123 ,  456 ,  MYVAR2")
    addr = inner.pop_ds()
    # Fetch the first value
    val1 = inner.cell_fetch(addr)
    assert val1.intval == 123
    # Fetch the second value
    addr2 = W_IntObject(addr.intval + CELL_SIZE_BYTES)
    val2 = inner.cell_fetch(addr2)
    assert val2.intval == 456

def test_find():
    """Test FIND - find a word in the dictionary"""
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" DUP"')
    outer.interpret_line("FIND")
    flag = inner.pop_ds()
    xt = inner.pop_ds()
    assert isinstance(flag, W_IntObject)
    assert isinstance(xt, W_WordObject)
    assert xt.word.name == "DUP"
    assert flag.intval == 1

def test_find_not_found():
    """Test FIND with non-existent word"""
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" NOTAWORD"')
    outer.interpret_line("FIND")
    flag = inner.pop_ds()
    u = inner.pop_ds()
    caddr = inner.pop_ds()
    assert flag.intval == 0

def test_execute():
    """Test EXECUTE - execute an execution token"""
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" DUP"')
    outer.interpret_line("FIND")
    flag = inner.pop_ds()
    xt = inner.pop_ds()
    inner.push_ds(W_IntObject(42))
    inner.push_ds(xt)
    outer.interpret_line("EXECUTE")
    val2 = inner.pop_ds()
    val1 = inner.pop_ds()
    assert val1.intval == 42
    assert val2.intval == 42

def test_to_body():
    """Test >BODY - get body address from execution token"""
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("VARIABLE MYVAR")
    outer.interpret_line('S" MYVAR"')
    outer.interpret_line("FIND")
    flag = inner.pop_ds()
    xt = inner.pop_ds()
    inner.push_ds(xt)
    outer.interpret_line(">BODY")
    body_addr = inner.pop_ds()
    outer.interpret_line("MYVAR")
    var_addr = inner.pop_ds()
    assert body_addr.intval == var_addr.intval
