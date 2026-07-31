from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def make():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    return inner, outer


def run(line):
    inner, outer = make()
    outer.interpret_line(line)
    return inner


def test_nextname_then_create():
    inner = run('S" foo" NEXTNAME CREATE 1 ,  foo @')
    assert inner.pop_ds_int() == 1


def test_nextname_then_constant():
    inner = run('5 S" bar" NEXTNAME CONSTANT  bar')
    assert inner.pop_ds_int() == 5


def test_nextname_then_colon():
    inner = run('S" baz" NEXTNAME : 99 ;  baz')
    assert inner.pop_ds_int() == 99


def test_nextname_then_variable():
    inner = run('S" qux" NEXTNAME VARIABLE  42 qux !  qux @')
    assert inner.pop_ds_int() == 42


def test_nextname_is_consumed_once():
    # After the first defining word consumes the pending name, the next word parses its name from input normally.
    inner = run('S" one" NEXTNAME CREATE 10 ,  20 CONSTANT two  one @ two')
    assert inner.pop_ds_int() == 20
    assert inner.pop_ds_int() == 10


def test_to_order_findable_word():
    # >ORDER must be a real dictionary word (findable via ') and usable inside a colon body.
    inner, outer = make()
    outer.interpret_line("' >ORDER DROP")  # DROP underflows (raises) if ' found nothing

    outer.interpret_line("WORDLIST DUP CONSTANT W1")
    w1_wid = inner.pop_ds_int()
    outer.interpret_line(": use-it ( wid -- ) >order ;")
    outer.interpret_line("W1 use-it")
    outer.interpret_line("GET-ORDER")
    n = inner.pop_ds_int()
    assert n >= 1
    top = inner.pop_ds_int()
    for _ in range(n - 1):
        inner.pop_ds_int()
    assert top == w1_wid


def test_percent_alloc_prelude_word():
    inner = run("cell% %alloc  1234 over !  @")
    assert inner.pop_ds_int() == 1234
