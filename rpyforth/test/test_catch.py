from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def test_catch_no_throw():
    inner = run(": ok 5 ;  ' ok CATCH")
    assert inner.pop_ds_int() == 0   # CATCH result code
    assert inner.pop_ds_int() == 5   # ok's value


def test_catch_catches_throw():
    inner = run(": bad 7 THROW ;  ' bad CATCH")
    assert inner.pop_ds_int() == 7


def test_catch_restores_stack():
    # items pushed before THROW are discarded; the pre-CATCH 99 survives
    inner = run("99  : bad 1 2 3 7 THROW ;  ' bad CATCH")
    assert inner.pop_ds_int() == 7    # code
    assert inner.pop_ds_int() == 99   # restored depth


def test_throw_zero_is_noop():
    inner = run(": ok 5 0 THROW ;  ' ok CATCH")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 5


def test_catch_nested():
    inner = run(": bad 9 THROW ;  : mid ['] bad CATCH ;  ' mid CATCH")
    assert inner.pop_ds_int() == 0   # outer: mid returned normally
    assert inner.pop_ds_int() == 9   # inner caught 9


def test_catch_of_primitive_xt():
    inner = run("1 2 ' + CATCH")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 3


def test_throw_through_execute():
    inner = run(": bad 5 THROW ;  : mid ['] bad EXECUTE ;  ' mid CATCH")
    assert inner.pop_ds_int() == 5


def test_throw_from_deep_call_chain():
    inner = run(": a 7 THROW ;  : b a ;  : c b ;  ' c CATCH")
    assert inner.pop_ds_int() == 7


def test_throw_inside_loop_restores_loop_state():
    # THROW from inside DO..LOOP must restore the loop-control stack so the handler's own loop still works.
    inner = run(": bad 10 0 do i 3 = if 7 throw then loop ;"
                "  : go ['] bad catch 100 + 3 0 do 10 + loop ;  go")
    assert inner.pop_ds_int() == 137


def test_throw_restores_return_stack():
    inner = run(": bad 42 >r 7 throw ;  ' bad CATCH")
    assert inner.pop_ds_int() == 7
    assert inner.rs_ptr == 0


def test_execute_dispatch_in_tight_loop():
    # Repeated EXECUTE in a loop (OOP dispatch pattern); each iteration must return the right result.
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    prog = [
        ": sq dup * ;",
        "' sq constant XT",
        ": go 0 100 0 do i XT execute + loop ;",
        "go",
    ]
    for line in prog:
        outer.interpret_line(line)
    assert inner.pop_ds_int() == 328350


def test_execute_reentrant_nested():
    # EXECUTE of a word that itself EXECUTEs: pending-target channel must survive nesting.
    inner = run(": inner 3 * ;  : outer ['] inner execute 1+ ;  10 ' outer execute")
    assert inner.pop_ds_int() == 31


def test_nested_catch_in_loop_bounds_recursion():
    # execute_word_now must stop at its word boundary; leaking into the caller's frames causes native StackOverflow at scale.
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    prog = [
        "1 constant LOE  2 constant HIE  variable cn",
        ": bad cn @ 1 and if HIE throw else LOE throw then ;",
        ": lo ['] bad catch ?dup if dup LOE = if drop else throw then then ;",
        ": hi ['] lo catch ?dup if dup HIE = if drop else throw then then ;",
        ": some ['] hi catch ?dup if drop then ;",
        ": go 3000 0 do i cn ! some loop 123 ;",
        "go",
    ]
    for line in prog:
        outer.interpret_line(line)
    assert inner.pop_ds_int() == 123
