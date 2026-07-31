from rpyforth.objects import W_StringObject, CELL_SIZE_BYTES, W_IntObject, W_FloatObject, W_WordObject, word_from_wid
from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


import pytest

def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner

def run_and_pop(line):
    return run(line).pop_ds_int()

def run_and_pop_float(line):
    return run(line).pop_ds_float()

def test_basic_primitives():
    assert run_and_pop(": SQUARE DUP * ; 3 SQUARE") == 9
    assert run_and_pop(": INC 1 + ;  5 INC") == 6

def test_ZEROs():
    assert run_and_pop("0 0=") == -1
    assert run_and_pop("5 0=") == 0
    assert run_and_pop("0 0<") == 0
    assert run_and_pop("-128 0<") == -1
    assert run_and_pop("-128 0>") == -0
    assert run_and_pop("47 0>") == -1

def test_STORE_FETCH():
    assert run_and_pop("5 0 !    0 @") == 5
    assert run_and_pop("VARIABLE X    123 X !    X @") == 123
    assert run_and_pop("VARIABLE A    10 A !    A @ 5 + A !    A @") == 15
    assert run_and_pop(""": SQUARE DUP * ;    VARIABLE N 7 N !    N @ SQUARE""") == 49
    assert run_and_pop(""": SQUARE DUP * ;    VARIABLE N
7 N !    N @ SQUARE""") == 49
    assert run_and_pop(""": SQUARE DUP * ;    VARIABLE N
7 N !    N @ SQUARE""") == 49
    assert run_and_pop("VARIABLE N   -42 N !   N @") == -42

def test_cell_primitives():
    cell_bytes = CELL_SIZE_BYTES
    assert run_and_pop("CELL") == cell_bytes
    assert run_and_pop("3 CELLS") == 3 * cell_bytes
    assert run_and_pop("VARIABLE X VARIABLE Y Y X -") == cell_bytes
    assert run_and_pop("VARIABLE X VARIABLE Y X CELL+ Y -") == 0

def test_DROP():
    assert run_and_pop("1 2 DROP") == 1

def test_MAX():
    assert run_and_pop("3 5 MAX") == 5

def test_min():
    assert run_and_pop("3 5 MIN") == 3

def test_abs():
    assert run_and_pop("-3 ABS") == 3
    assert run_and_pop("3 ABS") == 3

def test_negate():
    assert run_and_pop("3 NEGATE") == -3
    assert run_and_pop("-3 NEGATE") == 3

def test_rot():
    assert run_and_pop("1 2 3 ROT") == 1

def test_2dup():
    inner = run("1 2 2DUP")
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1

def test_2drop():
    inner = run_and_pop("1 2 3 2DROP") == 1


def test_3drop():
    assert run_and_pop("1 2 3 4 3DROP") == 1
    assert run_and_pop(": KEEP-FIRST 3DROP ; 1 2 3 4 KEEP-FIRST") == 1


def test_2swap():
    inner = run("1 2 3 4 2SWAP")
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 4
    assert inner.pop_ds_int() == 3

def test_2over():
    inner = run("1 2 3 4 2OVER")
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 4
    assert inner.pop_ds_int() == 3
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1

def test_mod():
    assert run_and_pop("10 3 MOD") == 1
    assert run_and_pop("-20 6 MOD") == 4

def test_inc():
    assert run_and_pop("5 1+") == 6
    assert run_and_pop("-1 1+") == 0

def test_dec():
    assert run_and_pop("5 1-") == 4
    assert run_and_pop("0 1-") == -1

def test_BRANCH():
    assert run_and_pop(": Z? 0= IF 1 ELSE 2 THEN ; 0 Z?") == 1
    assert run_and_pop(": Z? 0= IF 1 ELSE 2 THEN ; 7 Z?") == 2
    assert run_and_pop(": T1  1 0= IF 111 ELSE  0 0= IF 222 ELSE 333 THEN THEN ; T1") == 222

def test_EMIT():
    assert run_and_pop('10 65 EMIT') == 10

def test_questiondup():
    inner = run("0 ?DUP")
    assert inner.pop_ds_int() == 0
    inner = run("5 ?DUP")
    assert inner.pop_ds_int() == 5
    assert inner.pop_ds_int() == 5

def test_depth():
    assert run_and_pop("0 1 DEPTH") == 2
    assert run_and_pop("0 DEPTH") == 1
    assert run_and_pop("DEPTH") == 0

def test_rshift():
    assert run_and_pop("1 0 RSHIFT") == 1
    assert run_and_pop("1 1 RSHIFT") == 0
    assert run_and_pop("2 1 RSHIFT") == 1
    assert run_and_pop("4 2 RSHIFT") == 1
    assert run_and_pop("32768 15 RSHIFT") == 1

def test_lshift():
    assert run_and_pop("1 0 LSHIFT") == 1
    assert run_and_pop("1 1 LSHIFT") == 2
    assert run_and_pop("1 2 LSHIFT") == 4
    assert run_and_pop("1 15 LSHIFT") == 32768


def test_lshift_wraps_to_signed_cell():
    # Shifting into the sign bit wraps to a signed cell so results round-trip through a cell store (brainless hash codes).
    assert run_and_pop("1 63 LSHIFT") == -9223372036854775808
    assert run_and_pop("1 64 LSHIFT") == 0
    inner = run("CREATE c 8 ALLOT  65535 48 LSHIFT c !  c @")
    stored = inner.pop_ds_int()
    inner2 = run("65535 48 LSHIFT")
    assert stored == inner2.pop_ds_int()


def test_s_to_d():
    inner = run("1024 S>D")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 1024
    inner = run("-1024 S>D")
    assert inner.pop_ds_int() == -1
    assert inner.pop_ds_int() == -1024

def test_mul_star():
    inner = run("1024 4 M*")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 4096
    inner = run("-1024 4 M*")
    assert inner.pop_ds_int() == -1
    assert inner.pop_ds_int() == -4096
    inner = run("-1024 -4 M*")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 4096
    inner = run("9223372036854775807 2 M*")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == -2

def test_bl():
    assert run_and_pop("BL") == 32

def test_u_mul_star():
    inner = run("1024 4 UM*")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 4096
    inner = run("9223372036854775808 2 UM*")
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 0
    inner = run("18446744073709551615 2 UM*")
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == -2  # 0xFFFFFFFFFFFFFFFE as signed 64-bit
    inner = run("18446744073709551615 18446744073709551615 UM*")
    assert inner.pop_ds_int() == -2  # hi = 0xFFFFFFFFFFFFFFFE as signed 64-bit
    assert inner.pop_ds_int() == 1

def test_and():
    assert run_and_pop("6 3 AND") == 2
    assert run_and_pop("0 9223372036854775807 AND") == 0
    assert run_and_pop("-1 -1 AND") == -1
    assert run_and_pop("18446744073709551615 18446744073709551615 AND") == 18446744073709551615

def test_or():
    assert run_and_pop("6 3 OR") == 7
    assert run_and_pop("0 9223372036854775807 OR") == 9223372036854775807
    assert run_and_pop("-1 -1 OR") == -1
    assert run_and_pop("18446744073709551615 18446744073709551615 OR") == 18446744073709551615

def test_xor():
    assert run_and_pop("6 3 XOR") == 5
    assert run_and_pop("0 9223372036854775807 XOR") == 9223372036854775807
    assert run_and_pop("-1 -1 XOR") == 0
    assert run_and_pop("18446744073709551615 18446744073709551615 XOR") == 0

def test_2STAR():
    assert run_and_pop("16384 2*") == 32768
    assert run_and_pop("1 2*") == 2
    assert run_and_pop("0 2*") == 0

def test_2SLASH():
    assert run_and_pop("16384 2/") == 8192
    assert run_and_pop("2 2/") == 1
    assert run_and_pop("-1 2/") == -1

def test_SDOUBLE_QUOTE():
    str = "Hello, World!"
    inner = run("S\" Hello, World!\"")
    assert inner.pop_ds_int() == len(str)
    ptr = inner.pop_ds_int()
    buf_entry = inner.buf[ptr]
    assert buf_entry is not None
    assert buf_entry.strval == str

def test_DIV():
    assert run_and_pop("10 3 /") == 3
    assert run_and_pop("-20 6 /") == -4
    assert run_and_pop("-9223372036854775808 -1 /") == 9223372036854775808

def test_mul_slash():
    assert run_and_pop("10 3 2 */") == 15
    assert run_and_pop("-20 6 7 */") == -18

def test_div_mod():
    inner = run("10 3 /MOD")
    assert inner.pop_ds_int() == 3
    assert inner.pop_ds_int() == 1
    inner = run("-20 6 /MOD")
    assert inner.pop_ds_int() == -4
    assert inner.pop_ds_int() == 4

def test_mul_div_mod():
    inner = run("10 2 3 */MOD")
    assert inner.pop_ds_int() == 6
    assert inner.pop_ds_int() == 2
    inner = run("-20 4 6 */MOD")
    assert inner.pop_ds_int() == -14
    assert inner.pop_ds_int() == 4

def test_fm_div_mod():
    inner = run("20 S>D 3 FM/MOD")
    assert inner.pop_ds_int() == 6
    assert inner.pop_ds_int() == 2
    inner = run("-20 S>D 6 FM/MOD")
    assert inner.pop_ds_int() == -4
    assert inner.pop_ds_int() == 4
    inner = run("20 5 M* 3 FM/MOD")
    assert inner.pop_ds_int() == 33
    assert inner.pop_ds_int() == 1
    inner = run("-20 5 M* 6 FM/MOD")
    assert inner.pop_ds_int() == -17
    assert inner.pop_ds_int() == 2

def test_um_div_mod():
    inner = run("0 1 2 UM/MOD")
    assert inner.pop_ds_int() == -9223372036854775808
    assert inner.pop_ds_int() == 0
    inner = run("3 0 2 UM/MOD")
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 1
    inner = run("18446744073709551615 2 UM* 2 UM/MOD")
    assert inner.pop_ds_int() == -1
    assert inner.pop_ds_int() == 0  # 36893488147419103230 % 2 = 0

def test_sm_div_rem():
    inner = run("7 S>D 3 SM/REM")
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1
    inner = run("-7 S>D 3 SM/REM")
    assert inner.pop_ds_int() == -2
    assert inner.pop_ds_int() == -1
    inner = run("7 S>D -3 SM/REM")
    assert inner.pop_ds_int() == -2
    assert inner.pop_ds_int() == 1
    inner = run("-7 S>D -3 SM/REM")
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == -1

def test_u_less():
    assert run_and_pop("3 5 U<") == -1
    assert run_and_pop("5 3 U<") == 0
    assert run_and_pop("18446744073709551615 0 U<") == 0
    assert run_and_pop("0 18446744073709551615 U<") == -1
    assert run_and_pop("-1 0 U<") == 0
    assert run_and_pop("0 -1 U<") == -1
    # -N is huge unsigned, so it is never less than a small positive (brainless WITHIN path).
    assert run_and_pop("-46 8 U<") == 0
    assert run_and_pop("8 -46 U<") == -1
    assert run_and_pop("45 91 99 WITHIN") == 0
    assert run_and_pop("45 21 29 WITHIN") == 0
    assert run_and_pop("95 91 99 WITHIN") == -1

def test_u_dot():
    assert run_and_pop("10 10 U.") == 10

def test_float_literals():
    def run_and_pop(prog):
        inner = run(prog)
        return inner.pop_ds_float()
    assert run_and_pop("1.0") == 1.0
    assert run_and_pop("3.14") == 3.14
    assert run_and_pop("-2.5") == -2.5
    assert run_and_pop("0.") == 0.0

def test_float_addition():
    def run_and_pop(prog):
        inner = run(prog)
        return inner.pop_ds_float()

    assert run_and_pop("1.0 2.0 F+") == 3.0
    assert run_and_pop("3.5 2.5 F+") == 6.0
    assert run_and_pop("-1.5 1.5 F+") == 0.0

def test_float_subtraction():
    assert run_and_pop_float("5.0 3.0 F-") == 2.0
    assert run_and_pop_float("10.5 2.5 F-") == 8.0
    assert run_and_pop_float("1.0 5.0 F-") == -4.0

def test_float_multiplication():
    assert run_and_pop_float("3.0 4.0 F*") == 12.0
    assert run_and_pop_float("2.5 2.0 F*") == 5.0
    assert run_and_pop_float("-2.0 3.0 F*") == -6.0

def test_float_division():
    assert run_and_pop_float("10.0 2.0 F/") == 5.0
    assert run_and_pop_float("7.0 2.0 F/") == 3.5
    assert run_and_pop_float("1.0 4.0 F/") == 0.25

def test_float_comparison():
    assert run_and_pop("5.0 3.0 F>") == -1
    assert run_and_pop("2.0 8.0 F>") == 0
    assert run_and_pop("3.0 3.0 F>") == 0

def test_float_swap():
    inner = run("1.0 2.0 FSWAP")
    assert inner.pop_ds_float() == 1.0
    assert inner.pop_ds_float() == 2.0

def test_float_in_colon_def():
    assert run_and_pop_float(": CIRCLE-AREA 3.14159 F* ; 5.0 5.0 F* CIRCLE-AREA") == 78.53975

def test_simple_do_loop():
    inner = run(": TEST 10 0 DO I LOOP ; TEST")
    results = []
    for i in range(10):
        results.append(inner.pop_ds_int())
    assert results == list(range(9, -1, -1))

    inner = run(": SUM 0 5 0 DO I + LOOP ; SUM")
    assert inner.pop_ds_int() == 10

    run(": TEST 10 0 DO I . LOOP ; TEST")
    assert run_and_pop(": SUM 0 5 0 DO I + LOOP ; SUM") == 10

def test_nested_do_loops():
    inner = run(": NESTED 3 0 DO 3 0 DO I J * LOOP LOOP ; NESTED")
    expected = [0, 0, 0, 0, 1, 2, 0, 2, 4]
    results = []
    for i in range(9):
        results.append(inner.pop_ds_int())
    assert results == expected[::-1]

def test_leave_in_loop():
    inner = run(": EARLY 10 0 DO I DUP 5 = IF LEAVE THEN LOOP ; EARLY")
    results = []
    while inner.ds_int_size() > 0:
        results.append(inner.pop_ds_int())
    assert results == [5, 4, 3, 2, 1, 0]

def test_compare_op():
    assert run_and_pop("5 3 >") == -1
    assert run_and_pop("2 8 >") == 0
    assert run_and_pop("3 3 >") == 0

    assert run_and_pop("5 3 <") == 0
    assert run_and_pop("2 8 <") == -1
    assert run_and_pop("3 3 <") == 0

    assert run_and_pop("5 5 =") == -1
    assert run_and_pop("3 7 =") == 0
    assert run_and_pop("0 0 =") == -1

def test_pick():
    assert run_and_pop("10 20 30 0 PICK") == 30
    assert run_and_pop("10 20 30 1 PICK") == 20
    assert run_and_pop("10 20 30 2 PICK") == 10

def test_char_bracket():
    assert run_and_pop(": TEST [CHAR] A ; TEST") == ord('A')
    assert run_and_pop(": TEST [CHAR] Z ; TEST") == ord('Z')
    assert run_and_pop(": TEST [CHAR] 0 ; TEST") == ord('0')


def test_float():
    assert run_and_pop_float("1.0") == 1.0
    assert run_and_pop_float("3.14") == 3.14

    assert run_and_pop_float("1.0 2.0 F+") == 3.0
    assert run_and_pop_float("5.0 3.0 F-") == 2.0
    assert run_and_pop_float("3.0 4.0 F*") == 12.0
    assert run_and_pop_float("10.0 2.0 F/") == 5.0

    assert run_and_pop("5.0 3.0 F>") == -1
    assert run_and_pop("2.0 8.0 F>") == 0

def test_CHAR():
    assert run_and_pop("CHAR Hello") == ord('H')
    assert run_and_pop("CHAR ello") == ord('e')

def test_2STORE():
    inner = run("2VARIABLE buf 10 20 buf 2!")
    assert inner.cell_fetch(0).intval == 20
    assert inner.cell_fetch(CELL_SIZE_BYTES).intval == 10

def test_s2f():
    assert run_and_pop_float("10 S>F") == 10.0

def test_fstore_ffetch():
    result = run_and_pop_float("FVARIABLE X  3.14E0 X F!  X F@")
    assert abs(result - 3.14) < 0.01

def test_fdup():
    inner = run("5.5E0 FDUP")
    f1 = inner.pop_ds_float()
    f2 = inner.pop_ds_float()
    assert f1 == f2 == 5.5

def test_begin_while_repeat():
    result = run_and_pop(": TEST 0 BEGIN DUP 5 < WHILE 1+ REPEAT ; TEST")
    assert result == 5

def test_to_r():
    inner = run("42 >R")
    assert inner.ds_ptr_ints == 0
    result = inner.pop_rs()
    assert result == 42

def test_r_from():
    inner = run("42 >R R>")
    result = inner.pop_ds_int()
    assert result == 42
    assert inner.rs_ptr == 0

def test_r_fetch():
    inner = run("42 >R R@")
    result = inner.pop_ds_int()
    assert result == 42
    result2 = inner.pop_rs()
    assert result2 == 42

def test_2to_r():
    inner = run("10 20 2>R")
    assert inner.ds_ptr_ints == 0
    result2 = inner.pop_rs()
    result1 = inner.pop_rs()
    assert result1 == 10
    assert result2 == 20

def test_2r_from():
    inner = run("10 20 2>R 2R>")
    result2 = inner.pop_ds_int()
    result1 = inner.pop_ds_int()
    assert result1 == 10
    assert result2 == 20
    assert inner.rs_ptr == 0

def test_2r_fetch():
    inner = run("10 20 2>R 2R@")
    result2 = inner.pop_ds_int()
    result1 = inner.pop_ds_int()
    assert result1 == 10
    assert result2 == 20
    result2_rs = inner.pop_rs()
    result1_rs = inner.pop_rs()
    assert result1_rs == 10
    assert result2_rs == 20

def test_return_stack_complex():
    inner = run("1 2 3 >R >R >R R> R> R>")
    result1 = inner.pop_ds_int()
    result2 = inner.pop_ds_int()
    result3 = inner.pop_ds_int()
    assert result1 == 3
    assert result2 == 2
    assert result3 == 1

def test_here():
    inner = run("HERE")
    addr1 = inner.pop_ds_int()
    assert addr1 >= 0

def test_comma():
    inner = run("HERE  42 ,  HERE")
    addr2 = inner.pop_ds_int()
    addr1 = inner.pop_ds_int()
    assert addr2 == addr1 + inner.cell_size_bytes
    stored_val = inner.cell_fetch(addr1)
    assert stored_val.intval == 42

def test_c_comma():
    inner = run("HERE  65 C,  HERE")
    addr2 = inner.pop_ds_int()
    addr1 = inner.pop_ds_int()
    assert addr2 == addr1 + 1

def test_allot():
    inner = run("HERE  10 ALLOT  HERE")
    addr2 = inner.pop_ds_int()
    addr1 = inner.pop_ds_int()
    assert addr2 == addr1 + 10

def test_create():
    result = run_and_pop("CREATE MYDATA  MYDATA")
    assert result >= 0

def test_create_with_comma():
    result = run_and_pop("CREATE MYVAR  123 ,  456 ,  MYVAR")
    addr = result
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("CREATE MYVAR2  123 ,  456 ,  MYVAR2")
    addr = inner.pop_ds_int()
    val1 = inner.cell_fetch(addr)
    assert val1.intval == 123
    addr2 = addr + CELL_SIZE_BYTES
    val2 = inner.cell_fetch(addr2)
    assert val2.intval == 456

def test_find():
    """Test FIND - find a word in the dictionary (standard: c-addr -- xt flag)."""
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" DUP" DROP')
    outer.interpret_line("FIND")
    flag = inner.pop_ds_int()
    xt = inner.pop_ds_int()
    assert word_from_wid(xt).name == "DUP"
    assert flag == -1

def test_find_not_found():
    """Test FIND with non-existent word (standard: leaves c-addr 0)."""
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" NOTAWORD" DROP')
    outer.interpret_line("FIND")
    flag = inner.pop_ds_int()
    caddr = inner.pop_ds_int()
    assert flag == 0

def test_execute():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" DUP" DROP')
    outer.interpret_line("FIND")
    flag = inner.pop_ds_int()
    xt = inner.pop_ds_int()
    inner.push_ds_int(42)
    inner.push_ds_int(xt)
    outer.interpret_line("EXECUTE")
    val2 = inner.pop_ds_int()
    val1 = inner.pop_ds_int()
    assert val1 == 42
    assert val2 == 42

def test_to_body():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("VARIABLE MYVAR")
    outer.interpret_line('S" MYVAR" DROP')
    outer.interpret_line("FIND")
    flag = inner.pop_ds_int()
    xt = inner.pop_ds_int()
    inner.push_ds_int(xt)
    outer.interpret_line(">BODY")
    body_addr = inner.pop_ds_int()
    outer.interpret_line("MYVAR")
    var_addr = inner.pop_ds_int()
    assert body_addr == var_addr

def test_source():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    test_line = "1 2 3 SOURCE"
    outer.interpret_line(test_line)
    u = inner.pop_ds_int()
    caddr = inner.pop_ds_int()
    assert u == len(test_line)

def test_to_in():
    """>IN returns the address of the runtime parse cursor (a token index).
    After the interpreter has consumed the >IN token itself, the cursor is 1."""
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(">IN")
    addr = inner.pop_ds_int()
    pos = inner.cell_fetch(addr)
    assert isinstance(pos, W_IntObject)
    assert pos.intval == 1

def test_tick():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("' DUP")
    xt = inner.pop_ds_int()
    assert word_from_wid(xt).name == "DUP"

def test_paren_comment():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("1 ( this is a comment ) 2")
    val2 = inner.pop_ds_int()
    val1 = inner.pop_ds_int()
    assert val1 == 1
    assert val2 == 2

def test_tick_execute():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("' DUP")
    xt = inner.pop_ds_int()
    inner.push_ds_int(42)
    inner.push_ds_int(xt)
    outer.interpret_line("EXECUTE")
    val2 = inner.pop_ds_int()
    val1 = inner.pop_ds_int()
    assert val1 == 42
    assert val2 == 42

def test_plusstore():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("VARIABLE X  10 X !  5 X +!  X @")
    result = inner.pop_ds_int()
    assert result == 15

def test_2fetch():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("2VARIABLE BUF  10 20 BUF 2!  BUF 2@")
    x2 = inner.pop_ds_int()
    x1 = inner.pop_ds_int()
    assert x1 == 10
    assert x2 == 20

def test_c_store_fetch():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("VARIABLE CBUF  65 CBUF C!  CBUF C@")
    result = inner.pop_ds_int()
    assert result == 65

def test_char_plus():
    result = run_and_pop("10 CHAR+")
    assert result == 11

def test_chars():
    result = run_and_pop("5 CHARS")
    assert result == 5

def test_align():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("HERE  1 C,  ALIGN  HERE")
    here_after = inner.pop_ds_int()
    here_before = inner.pop_ds_int()
    assert here_after % CELL_SIZE_BYTES == 0

def test_aligned():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("1 ALIGNED")
    result = inner.pop_ds_int()
    assert result % CELL_SIZE_BYTES == 0

def test_count():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("HERE  3 C,")
    addr = inner.pop_ds_int()
    outer.interpret_line("65 C,  66 C,  67 C,")
    inner.push_ds_int(addr)
    outer.interpret_line("COUNT")
    u = inner.pop_ds_int()
    caddr2 = inner.pop_ds_int()
    assert u == 3
    assert caddr2 == addr + 1

def test_word():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("32 WORD Hello")
    caddr = inner.pop_ds_int()
    length = inner.char_fetch(caddr)
    assert length == 5

def test_word_count():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("32 WORD Test COUNT")
    u = inner.pop_ds_int()
    caddr2 = inner.pop_ds_int()
    assert u == 4

def test_PNO():
    # #S expects (ud.lo ud.hi); #> returns ( c-addr u ) in char memory.
    def run_and_pop(line):
        inner = InnerInterpreter()
        outer = OuterInterpreter(inner)
        outer.interpret_line(line)
        u = inner.pop_ds_int()
        c_addr = inner.pop_ds_int()
        chars = []
        for k in range(u):
            chars.append(chr(inner.char_fetch(c_addr + k)))
        return "".join(chars)
    assert run_and_pop("DECIMAL  12345 0 <# #S #>") == '12345'
    assert run_and_pop("HEX      0FF 0   <# #S #>") == 'FF'
    assert run_and_pop("DECIMAL 5 0  BINARY  <# #S #>") == '101'


def test_sign_negative():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("<# -1 SIGN 123 0 #S #> TYPE")

def test_sign_positive():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("<# 1 SIGN 123 0 #S #>")
    u = inner.pop_ds_int()
    c_addr = inner.pop_ds_int()
    chars = [chr(inner.char_fetch(c_addr + k)) for k in range(u)]
    assert "".join(chars) == '123'

def test_sign_in_pno():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("<# -5 SIGN 0 0 #>")
    u = inner.pop_ds_int()
    c_addr = inner.pop_ds_int()
    chars = [chr(inner.char_fetch(c_addr + k)) for k in range(u)]
    assert "".join(chars) == '-'

def test_fill():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("HERE 5 65 FILL")

def test_move():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("VARIABLE SRC1  VARIABLE SRC2  VARIABLE SRC3")
    outer.interpret_line("10 SRC1 !  20 SRC2 !  30 SRC3 !")
    outer.interpret_line("VARIABLE DST")
    outer.interpret_line("SRC1 DST 3 MOVE")
    outer.interpret_line("DST @")
    result = inner.pop_ds_int()
    assert result == 10

def test_state():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("STATE @")
    state_val = inner.pop_ds_int()
    assert state_val == 0

def test_evaluate():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" 1 2 +"')
    outer.interpret_line("EVALUATE")
    result = inner.pop_ds_int()
    assert result == 3

def test_abort_quote_false():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("1 2 3")
    outer.interpret_line('0 ABORT" This should not print"')
    assert inner.ds_int_size() == 3

def test_abort_quote_true():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("1 2 3")
    outer.interpret_line('-1 ABORT" Error occurred"')
    assert inner.ds_int_size() == 0


def test_divmod():
    inner = run("10 3 /MOD")
    quot = inner.pop_ds_int()
    rem = inner.pop_ds_int()
    assert quot == 3
    assert rem == 1

def test_starslash():
    assert run_and_pop("10 3 2 */") == 15  # (10*3)/2

def test_starslashmod():
    inner = run("10 3 4 */MOD")
    quot = inner.pop_ds_int()
    rem = inner.pop_ds_int()
    assert quot == 7
    assert rem == 2

def test_fmslashmod():
    inner = run("7 0 3 FM/MOD")
    quot = inner.pop_ds_int()
    rem = inner.pop_ds_int()
    assert quot == 2
    assert rem == 1

def test_smslashrem():
    inner = run("7 0 3 SM/REM")
    quot = inner.pop_ds_int()
    rem = inner.pop_ds_int()
    assert quot == 2
    assert rem == 1

def test_umslashmod():
    inner = run("10 0 3 UM/MOD")
    quot = inner.pop_ds_int()
    rem = inner.pop_ds_int()
    assert quot == 3
    assert rem == 1

def test_invert():
    assert run_and_pop("0 INVERT") == -1
    assert run_and_pop("-1 INVERT") == 0

def test_u_less():
    assert run_and_pop("1 2 U<") == -1
    assert run_and_pop("2 1 U<") == 0
    assert run_and_pop("-1 1 U<") == 0  # -1 is unsigned-large; never less than a small positive

def test_plusloop():
    result = run_and_pop(": TEST 0 10 0 DO I + 2 +LOOP ; TEST")
    assert result == 20

def test_again():
    result = run_and_pop(": TEST 0 BEGIN 1+ DUP 5 = IF EXIT THEN AGAIN ; TEST")
    assert result == 5

def test_until():
    result = run_and_pop(": TEST 0 BEGIN 1+ DUP 5 = UNTIL ; TEST")
    assert result == 5

def test_unloop():
    result = run_and_pop(": TEST 5 0 DO I DUP 3 = IF UNLOOP EXIT THEN DROP LOOP 99 ; TEST")
    assert result == 3

def test_immediate():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(": TWICE 2 * ;")
    outer.interpret_line("IMMEDIATE")
    assert outer.last_word.immediate == True

def test_literal():
    result = run_and_pop(": TEST [ 5 3 + ] LITERAL ; TEST")
    assert result == 8

def test_bracket():
    result = run_and_pop(": TEST [ 2 3 + ] LITERAL 10 + ; TEST")
    assert result == 15  # (2+3) + 10

def test_bracket_tick():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(": TEST ['] DUP ; TEST")
    xt = inner.pop_ds_int()
    assert word_from_wid(xt).name == "DUP"

def test_base():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("BASE@")
    result = inner.pop_ds_int()
    assert result == 10

def test_base_change():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("HEX")
    outer.interpret_line("BASE@")
    result = inner.pop_ds_int()
    assert result == 16
    outer.interpret_line("DECIMAL")
    outer.interpret_line("BASE@")
    result2 = inner.pop_ds_int()
    assert result2 == 10

def test_space():
    run("SPACE")

def test_spaces():
    run("5 SPACES")

def test_environment_core():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" CORE" ENVIRONMENT?')
    flag = inner.pop_ds_int()
    result = inner.pop_ds_int()
    assert flag == -1
    assert result == -1

def test_environment_stack_cells():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" STACK-CELLS" ENVIRONMENT?')
    flag = inner.pop_ds_int()
    result = inner.pop_ds_int()
    assert flag == -1
    assert result == 64

def test_environment_unknown():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line('S" UNKNOWN-QUERY" ENVIRONMENT?')
    flag = inner.pop_ds_int()
    assert flag == 0


def test_recurse_factorial():
    result = run_and_pop(": FACT DUP 1 > IF DUP 1- RECURSE * THEN ; 5 FACT")
    assert result == 120

def test_recurse_countdown():
    result = run_and_pop(": COUNTDOWN DUP 0 > IF 1- RECURSE THEN ; 5 COUNTDOWN")
    assert result == 0

def test_abort_quote_compiled_false():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(': TEST 0 ABORT" Should not abort" 42 ;')
    outer.interpret_line("TEST")
    result = inner.pop_ds_int()
    assert result == 42

def test_abort_quote_compiled_true():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(': TEST -1 ABORT" Aborted!" 42 ;')
    outer.interpret_line("99")
    outer.interpret_line("TEST")
    assert inner.ds_ptr_ints == 0


def test_utime():
    inner = run("UTIME")
    high = inner.pop_ds_int()
    low = inner.pop_ds_int()
    assert high >= 0
    assert low >= 0
    usecs = low + (high << 64)
    assert usecs > 0


def test_utime_increases():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(": DELAY 1000 0 DO LOOP ;")
    outer.interpret_line("UTIME")
    high1 = inner.pop_ds_int()
    low1 = inner.pop_ds_int()
    outer.interpret_line("DELAY")
    outer.interpret_line("UTIME")
    high2 = inner.pop_ds_int()
    low2 = inner.pop_ds_int()
    time1 = low1 + (high1 << 64)
    time2 = low2 + (high2 << 64)
    assert time2 >= time1


def test_cputime():
    inner = run("CPUTIME")
    sys_high = inner.pop_ds_int()
    sys_low = inner.pop_ds_int()
    user_high = inner.pop_ds_int()
    user_low = inner.pop_ds_int()
    assert user_high >= 0
    assert user_low >= 0
    assert sys_high >= 0
    assert sys_low >= 0


def test_cputime_user_increases():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(": WORK 10000 0 DO I DROP LOOP ;")
    outer.interpret_line("CPUTIME")
    sys_high1 = inner.pop_ds_int()
    sys_low1 = inner.pop_ds_int()
    user_high1 = inner.pop_ds_int()
    user_low1 = inner.pop_ds_int()
    outer.interpret_line("WORK")
    outer.interpret_line("CPUTIME")
    sys_high2 = inner.pop_ds_int()
    sys_low2 = inner.pop_ds_int()
    user_high2 = inner.pop_ds_int()
    user_low2 = inner.pop_ds_int()
    user1 = user_low1 + (user_high1 << 64)
    user2 = user_low2 + (user_high2 << 64)
    assert user2 >= user1


def test_d_plus():
    inner = run("100 S>D 200 S>D D+")
    high = inner.pop_ds_int()
    low = inner.pop_ds_int()
    assert low == 300
    assert high == 0


def test_d_minus():
    inner = run("500 S>D 200 S>D D-")
    high = inner.pop_ds_int()
    low = inner.pop_ds_int()
    assert low == 300
    assert high == 0


def test_d_minus_with_utime():
    inner = run("UTIME 100 S>D D+ UTIME 2SWAP D-")
    high = inner.pop_ds_int()
    low = inner.pop_ds_int()
    elapsed = low + (high << 64)
    assert elapsed >= 0


def test_argc():
    inner = InnerInterpreter()
    inner.argv = ["10", "20"]
    outer = OuterInterpreter(inner)
    outer.interpret_line("ARGC")
    assert inner.pop_ds_int() == 2


def test_argv():
    inner = InnerInterpreter()
    inner.argv = ["10"]
    outer = OuterInterpreter(inner)
    outer.interpret_line("0 0 0 ARGV >NUMBER 2DROP DROP")
    assert inner.pop_ds_int() == 10


def test_argv_out_of_range():
    inner = InnerInterpreter()
    inner.argv = ["10"]
    outer = OuterInterpreter(inner)
    outer.interpret_line("1 ARGV")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 0


def _compile(*lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_variable_reference_is_inlined():
    inner, outer = _compile("VARIABLE X", ": GETX X ;")
    thread = outer.dict["GETX"].thread
    x_word = outer.dict["X"]
    assert x_word not in thread.code
    assert thread.code[0] is outer.wLIT
    outer.interpret_line("X")
    assert thread.lits[0].intval == inner.pop_ds_int()


def test_constant_reference_is_inlined():
    inner, outer = _compile("100 CONSTANT C", ": GETC C ;")
    thread = outer.dict["GETC"].thread
    assert outer.dict["C"] not in thread.code
    assert thread.code[0] is outer.wLIT
    assert thread.lits[0].intval == 100


def test_inlined_value_words_preserve_semantics():
    assert run_and_pop("VARIABLE X 123 X !  : GETX X @ ;  GETX") == 123
    assert run_and_pop("100 CONSTANT C  : DBL C C + ;  DBL") == 200
    assert run_and_pop("VARIABLE Y VARIABLE Z  : DIFF Z Y - ;  DIFF") == CELL_SIZE_BYTES


def test_inlining_does_not_break_tick_or_body():
    # Inlining direct references must not remove the word from the dictionary; tick and >BODY must still resolve it.
    inner, outer = _compile("VARIABLE X", ": GETX X ;")
    outer.interpret_line("' X >BODY   GETX")
    getx_addr = inner.pop_ds_int()
    body_addr = inner.pop_ds_int()
    assert getx_addr == body_addr


def test_leaf_colon_word_is_inlined():
    inner, outer = _compile(
        "VARIABLE B  0 B !",
        ": geta  B @ 1 + ;",
        ": useit  geta geta ;",
    )
    geta = outer.dict["GETA"]
    thread = outer.dict["USEIT"].thread
    assert geta not in thread.code
    assert not any(getattr(l, "word", None) is geta for l in thread.lits)
    assert thread.code[-1] is outer.wEXIT


def test_inlined_leaf_word_semantics():
    assert run_and_pop("VARIABLE B 5 B !  : geta B @ 1 + ;  : useit geta geta + ;  useit") == 12


def test_transitive_inlining():
    # c inlines b which inlined a -- all straight-line.
    assert run_and_pop("VARIABLE B 3 B !  : a B @ ;  : b a 1 + ;  : c b 2 * ;  c") == 8


def test_control_flow_word_not_inlined_but_correct():
    # IF/ELSE branch targets are absolute indices; inlining would corrupt them, so control-flow words must not be spliced.
    assert run_and_pop(": classify dup 0 < if drop -1 else drop 1 then ;  : u -5 classify ;  u") == -1
    assert run_and_pop(": classify dup 0 < if drop -1 else drop 1 then ;  : u 5 classify ;  u") == 1


def test_loop_word_not_miscompiled_when_referenced():
    # (LOOP) targets are absolute indices into sumto's own thread; they must not be spliced.
    src = (": sumto  0 swap 0 do i + loop ;  "
           ": twice  5 sumto  5 sumto + ;  twice")
    assert run_and_pop(src) == 20


def test_inlining_grows_code_buffer_beyond_128():
    leaf = ": leaf 1 2 3 4 5 drop drop drop drop ;"
    src = leaf + " : big " + (" ".join(["leaf"] * 20)) + " ;  big"
    assert run_and_pop(src) == 1


def test_rshift_is_logical():
    # Forth RSHIFT is a logical (zero-fill) shift; -1 shifted right once gives the largest positive cell.
    assert run_and_pop("-1 1 RSHIFT") == 0x7FFFFFFFFFFFFFFF
    assert run_and_pop("8 2 RSHIFT") == 2
