from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def test_defer_is():
    inner = run("DEFER foo  : bar 42 ;  ' bar IS foo  foo")
    assert inner.ds_int_size() == 1
    assert inner.pop_ds_int() == 42


def test_defer_rebind():
    inner = run("DEFER foo  : a 1 ;  : b 2 ;  ' a IS foo  ' b IS foo  foo")
    assert inner.pop_ds_int() == 2


def test_is_compile_mode():
    inner = run("DEFER foo  : bar 7 ;  : setup ['] bar IS foo ;  setup  foo")
    assert inner.pop_ds_int() == 7


def test_defer_used_in_colon():
    inner = run("DEFER hook  : run-hook hook ;  : impl 99 ;  ' impl IS hook  run-hook")
    assert inner.pop_ds_int() == 99


def test_defer_primitive_action():
    inner = run("DEFER hook  ' DUP IS hook  7 hook")
    assert inner.pop_ds_int() == 7
    assert inner.pop_ds_int() == 7


def test_defer_store():
    # DEFER! ( xt xt-deferred -- )
    inner = run("DEFER foo  : bar 55 ;  ' bar  ' foo DEFER!  foo")
    assert inner.pop_ds_int() == 55


def test_noname_execute():
    inner = run(":NONAME 42 ;  EXECUTE")
    assert inner.pop_ds_int() == 42


def test_noname_bound_to_defer():
    inner = run("DEFER hook  :NONAME 88 ; IS hook  hook")
    assert inner.pop_ds_int() == 88
