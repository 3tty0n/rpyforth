from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    for line in lines:
        outer.interpret_line(line)
    return inner


def test_postpone_nonimmediate_primitive_defers_not_executes():
    # POSTPONE of a non-immediate primitive must compile it rather than run it (brainless hash! = POSTPONE !).
    inner = run([
        "VARIABLE v",
        ": mystore  POSTPONE ! ; IMMEDIATE",
        ": t  v mystore ;",
        "42 t",
        "v @",
    ])
    assert inner.pop_ds_int() == 42


def test_postpone_nonimmediate_word_defers():
    inner = run([
        ": add2  2 + ;",
        ": plus2  POSTPONE add2 ; IMMEDIATE",
        ": t  10 plus2 ;",
        "t",
    ])
    assert inner.pop_ds_int() == 12


def test_postpone_immediate_word_still_runs_at_enclosing_runtime():
    # POSTPONE of an immediate word emits it directly; it runs when the enclosing word runs (LITERAL is the canonical example).
    inner = run([
        ": lit5  5 POSTPONE LITERAL ; IMMEDIATE",
        ": t  lit5 ;",
        "t",
    ])
    assert inner.pop_ds_int() == 5


def test_postpone_control_flow_then():
    # POSTPONE of a control-flow word (THEN) replays the compile action while compiling (lexex ansify.fth: endif).
    inner = run([
        ": endif  POSTPONE then ; IMMEDIATE",
        ": t  ( f -- )  IF 1 endif 2 ;",
        "0 t",    # false: leaves just 2
        "-1 t",   # true: leaves 1 then 2
    ])
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 2


def test_postpone_control_flow_begin_while_repeat():
    inner = run([
        ": my-begin  POSTPONE begin ; IMMEDIATE",
        ": my-while  POSTPONE while ; IMMEDIATE",
        ": my-repeat POSTPONE repeat ; IMMEDIATE",
        ": t  ( n -- sum )  0 swap my-begin dup my-while tuck + swap 1 - my-repeat drop ;",
        "5 t",
    ])
    assert inner.pop_ds_int() == 15


def test_state_smart_word_via_postpone():
    inner = run([
        ": myplus  STATE @ IF POSTPONE + ELSE + THEN ; IMMEDIATE",
        "3 4 myplus",
    ])
    assert inner.pop_ds_int() == 7
    inner2 = run([
        ": myplus  STATE @ IF POSTPONE + ELSE + THEN ; IMMEDIATE",
        ": t  10 20 myplus ;",
        "t",
    ])
    assert inner2.pop_ds_int() == 30


def test_postpone_if_then_early_exit():
    # POSTPONE IF/EXIT/THEN from an IMMEDIATE word must splice the full control structure into the enclosing definition.
    inner = run([
        ": stop-if  POSTPONE IF  POSTPONE EXIT  POSTPONE THEN ; IMMEDIATE",
        ": t  ( flag -- n ) 1 SWAP stop-if DROP 2 ;",
        "-1 t",
    ])
    assert inner.pop_ds_int() == 1
    inner2 = run([
        ": stop-if  POSTPONE IF  POSTPONE EXIT  POSTPONE THEN ; IMMEDIATE",
        ": t  ( flag -- n ) 1 SWAP stop-if DROP 2 ;",
        "0 t",
    ])
    assert inner2.pop_ds_int() == 2


def test_postpone_if_else_then():
    inner = run([
        ": choose  POSTPONE IF  1 POSTPONE LITERAL  POSTPONE ELSE  2 POSTPONE LITERAL  POSTPONE THEN ; IMMEDIATE",
        ": t  ( flag -- n ) choose ;",
        "-1 t",
    ])
    assert inner.pop_ds_int() == 1
    inner2 = run([
        ": choose  POSTPONE IF  1 POSTPONE LITERAL  POSTPONE ELSE  2 POSTPONE LITERAL  POSTPONE THEN ; IMMEDIATE",
        ": t  ( flag -- n ) choose ;",
        "0 t",
    ])
    assert inner2.pop_ds_int() == 2
