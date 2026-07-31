from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def run_and_pop(line):
    return run(line).pop_ds_int()


# --- float/double size words (64-bit gforth: floats=8, dfloats=8, sfloats=4) ---

def test_dfloats():
    assert run_and_pop("1 dfloats") == 8
    assert run_and_pop("3 dfloats") == 24


def test_sfloats():
    assert run_and_pop("1 sfloats") == 4
    assert run_and_pop("3 sfloats") == 12


def test_floats_still_8():
    assert run_and_pop("1 floats") == 8


# --- float-alignment words: faligned/dfaligned align to 8, sfaligned to 4 ---

def test_faligned():
    assert run_and_pop("0 faligned") == 0
    assert run_and_pop("1 faligned") == 8
    assert run_and_pop("8 faligned") == 8
    assert run_and_pop("9 faligned") == 16


def test_dfaligned():
    assert run_and_pop("0 dfaligned") == 0
    assert run_and_pop("1 dfaligned") == 8
    assert run_and_pop("9 dfaligned") == 16


def test_sfaligned():
    assert run_and_pop("0 sfaligned") == 0
    assert run_and_pop("1 sfaligned") == 4
    assert run_and_pop("4 sfaligned") == 4
    assert run_and_pop("5 sfaligned") == 8


# --- alignment words compile inside a colon body ---

def test_faligned_in_definition():
    assert run_and_pop(": fa faligned ; 9 fa") == 16


def test_dfloats_in_definition():
    assert run_and_pop(": df dfloats ; 3 df") == 24


# --- 2CONSTANT works at interpret time and compiled into a defining word ---

def test_2constant_interpret():
    inner = run("7 11 2constant pair pair")
    assert inner.pop_ds_int() == 11
    assert inner.pop_ds_int() == 7


def test_2constant_in_defining_word():
    # end-struct-style: a word that runs 2constant at its own runtime.
    src = ": mk 2constant ; 3 5 mk foo foo"
    inner = run(src)
    assert inner.pop_ds_int() == 5
    assert inner.pop_ds_int() == 3


def test_2variable():
    inner = run("2variable dv 20 30 dv 2! dv 2@")
    assert inner.pop_ds_int() == 30
    assert inner.pop_ds_int() == 20


# CS-ROLL: [ 1 ] cs-roll swaps the top two control-flow entries so a THEN after a BEGIN resolves an earlier IF (gc.fs sweep1 idiom).

def test_cs_roll_resolves_earlier_if():
    # gc.fs sweep1: IF with no immediate THEN, BEGIN loop, inside it [cs-roll] THEN resolves the earlier IF; n<>0 pushes 222 and EXITs.
    src = (
        ": [cs-roll] cs-roll ; immediate "
        ": t ( n -- x ) "
        "  0<> if "
        "     begin "
        "        222 "
        "        [ 1 ] [cs-roll] then "
        "        exit "
        "     again ; "
        "5 t"
    )
    assert run_and_pop(src) == 222


def test_cs_roll_false_branch():
    # n=0: IF not taken; cs-roll'd THEN sits before EXIT so control exits without pushing 222.
    inner = run(
        ": [cs-roll] cs-roll ; immediate "
        ": t ( n -- ) "
        "  0<> if begin 222 [ 1 ] [cs-roll] then exit again ; "
        "42 0 t"
    )
    assert inner.pop_ds_int() == 42
    assert inner.ds_int_size() == 0


def test_postpone_paren_comments_out():
    # POSTPONE of the immediate '(' word (compat/assert.fs assertn): foo executes '(' during bar's compilation, eating tokens up to ')'; bar computes 1+5=6.
    src = (
        ": foo postpone ( ; immediate "
        ": bar 1 foo 2 3 4 ) 5 + ; "
        "bar"
    )
    assert run_and_pop(src) == 6
