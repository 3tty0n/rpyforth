from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner


def run_and_pop(line):
    return run(line).pop_ds_int()


def test_runtime_constant():
    inner = run(": mkc 42 CONSTANT ;  mkc answer  answer")
    assert inner.pop_ds_int() == 42


def test_runtime_variable():
    inner = run(": mkv VARIABLE ;  mkv v  7 v !  v @")
    assert inner.pop_ds_int() == 7


def test_runtime_create_plain():
    inner = run(": mk CREATE ;  mk here1  here1 here1 =")
    assert inner.pop_ds_int() == -1


def test_runtime_create_comma():
    inner = run(": mk CREATE 123 , 456 , ;  mk arr  arr @")
    assert inner.pop_ds_int() == 123


def test_runtime_defer():
    inner = run_lines([
        ": mkd DEFER ;",
        "mkd act",
        ": hello 99 ;",
        "' hello IS act",
        "act",
    ])
    assert inner.pop_ds_int() == 99


def test_does_at_runtime():
    inner = run(": con  CREATE , DOES> @ ;  5 con five  five")
    assert inner.pop_ds_int() == 5


def test_does_mask_pattern():
    inner = run(": mask CREATE , DOES> @ AND ;  255 mask lowbyte  4660 lowbyte")
    assert inner.pop_ds_int() == (4660 & 255)


def test_does_body_with_branch_interpret():
    # DOES> body with IF/ELSE/THEN is carved into its own thread; branch targets must be rebased to the carved body or the ELSE path jumps wrong.
    inner = run_lines([
        ": arr  CREATE CELLS ALLOT"
        "   DOES> 0 IF DROP ELSE SWAP CELLS + THEN ;",
        "120 arr bd",
        "5 3 bd !",
        "3 bd @",
    ])
    assert inner.pop_ds_int() == 5


def test_does_body_state_smart_interpret_path():
    # STATE-smart DOES> (brainless ARRAY): at interpret time (STATE 0) takes ELSE branch; SWAP CELLS + yields the addressed cell.
    inner = run_lines([
        ": create-array  CREATE IMMEDIATE CELLS ALLOT"
        "   DOES> STATE @ IF POSTPONE CELLS POSTPONE LITERAL POSTPONE +"
        "   ELSE SWAP CELLS + THEN ;",
        "120 create-array board",
        "5 3 board !",
        "7 4 board !",
        "3 board @",
        "4 board @",
    ])
    assert inner.pop_ds_int() == 7
    assert inner.pop_ds_int() == 5


def test_does_body_with_do_leave_loop():
    # DO/LEAVE/LOOP inside DOES>: LOOP branch-back and LEAVE exit targets are absolute parent indices that must be rebased.
    inner = run_lines([
        ": countup  CREATE CELLS ALLOT"
        "   DOES> DROP 0 5 0 DO I 3 = IF LEAVE THEN I + LOOP ;",
        "1 countup c",
        "0 c",
    ])
    assert inner.pop_ds_int() == 0 + 1 + 2


def test_in_pattern():
    inner = run_lines([
        ": in: ( xt <name> -- ) >in @ defer >in ! ' defer! ;",
        ": undef -1 ;",
        "' undef in: reset",
        ": t reset ;",
        "t",
    ])
    assert inner.pop_ds_int() == -1


def test_hex_numbers():
    inner = run("HEX 0F0 3F 00F8 DECIMAL")
    assert inner.pop_ds_int() == 0xF8
    assert inner.pop_ds_int() == 0x3F
    assert inner.pop_ds_int() == 0xF0


def test_hex_lowercase():
    inner = run("HEX 0aa 0cc DECIMAL")
    assert inner.pop_ds_int() == 0xCC
    assert inner.pop_ds_int() == 0xAA


def test_base_restored_decimal():
    inner = run("HEX DECIMAL 10")
    assert inner.pop_ds_int() == 10


def test_dot_paren():
    # .( prints during parse, consumes through ')', leaves nothing on the stack; tokens after ) are still interpreted.
    inner = run(".( hello world ) 5")
    assert inner.ds_int_size() == 1
    assert inner.pop_ds_int() == 5


def test_s_quote_in_colon():
    # S" compiled inside a colon body, consumed by EVALUATE.
    inner = run(': step s" 3 4 +" evaluate ;  step')
    assert inner.pop_ds_int() == 7


def test_xt_table():
    # CREATE table  ' w ,  ...  then IDX CELLS table + @ EXECUTE (cd16sim ~ / mojmp)
    inner = run_lines([
        ": ~ ' , ;",
        ": mo0 10 ;",
        ": mo1 20 ;",
        ": mo2 30 ;",
        "CREATE tbl ~ mo0 ~ mo1 ~ mo2",
        "2 CELLS tbl + @ EXECUTE",
    ])
    assert inner.pop_ds_int() == 30


def test_redefinition_chain():
    # Each redefinition calls the previous version (cd16sim process chain), not itself; must not recurse infinitely.
    inner = run_lines([
        ": acc 0 ;",
        ": step1 1 2 3 4 5 6 7 8 9 10 + + + + + + + + + ;",
        ": p acc step1 drop ;",
        ": p p 100 + ;",
        ": p p 20 + ;",
        "p",
    ])
    # p(final) = p(mid) + 20 = (p(first)+100) + 20 = 100 + 20 = 120
    assert inner.pop_ds_int() == 120


def test_runtime_immediate_marks_child():
    # IMMEDIATE run inside a colon body must mark the freshly created child immediate in the dictionary.
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(": defimm  CREATE IMMEDIATE ;")
    outer.interpret_line("defimm foo")
    w = outer.dict["FOO"]
    assert w.immediate is True


def test_runtime_immediate_child_is_immediate():
    # CREATE IMMEDIATE child runs at compile time (compiles CELLS LITERAL +); ELSE branch does SWAP CELLS + at interpret time.
    inner = run_lines([
        ": create-array  CREATE IMMEDIATE"
        "   DOES>  STATE @ IF POSTPONE CELLS POSTPONE LITERAL POSTPONE +"
        "   ELSE SWAP CELLS + THEN ;",
        "create-array board",
        "3 cells allot",
        "11 0 board ! 22 1 board ! 33 2 board !",   # board[0..2] = 11,22,33
        ": second  1 board @ ;",       # 1 board -> board+1cell ; @ fetches 22
        "second",
    ])
    assert inner.pop_ds_int() == 22


def test_recurse_still_works():
    inner = run(": fact dup 1 > if dup 1 - recurse * then ; 5 fact")
    assert inner.pop_ds_int() == 120


def test_parsing_immediate_word_during_compilation():
    # brainless's [DEF?] pattern: IMMEDIATE word running BL WORD FIND during compilation must see the correct next token.
    from rpyforth.outer_interp import OuterInterpreter
    from rpyforth.inner_interp import InnerInterpreter
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(": [DEF?] BL WORD FIND NIP ; IMMEDIATE")
    outer.interpret_line(": probe [DEF?] dup [IF] 222 [ELSE] 111 [THEN] ;")
    outer.interpret_line("probe")
    assert inner.pop_ds_int() == 222
