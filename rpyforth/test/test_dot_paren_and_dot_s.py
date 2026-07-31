from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def make_outer():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    return inner, outer


def test_dot_paren_single(capfd):
    inner, outer = make_outer()
    outer.interpret_line('.( hello)')
    out, _ = capfd.readouterr()
    assert out == 'hello'


def test_dot_paren_two_on_same_line(capfd):
    inner, outer = make_outer()
    outer.interpret_line('.( A) .( B)')
    out, _ = capfd.readouterr()
    assert out == 'AB'


def test_dot_paren_token_counter_increments():
    inner, outer = make_outer()
    outer.interpret_line('.( X) .( Y)')
    assert outer.string_token_counts.get('.(', 0) == 2


def test_dot_paren_in_colon_body(capfd):
    inner, outer = make_outer()
    outer.interpret_line(': greet .( hello) .( world) ;')
    outer.interpret_line('greet')
    out, _ = capfd.readouterr()
    assert out == 'helloworld'


def test_dot_s_empty_stack(capfd):
    inner, outer = make_outer()
    outer.interpret_line('.S')
    out, _ = capfd.readouterr()
    assert out == '<0> '


def test_dot_s_three_items(capfd):
    inner, outer = make_outer()
    outer.interpret_line('1 2 3 .S')
    out, _ = capfd.readouterr()
    assert out == '<3> 1 2 3 '


def test_dot_s_nondestructive():
    inner, outer = make_outer()
    outer.interpret_line('1 2 3 .S')
    assert inner.depth_ds_int() == 3
    assert inner.pop_ds_int() == 3
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1


def test_dot_s_lowercase(capfd):
    inner, outer = make_outer()
    outer.interpret_line('10 20 .s')
    out, _ = capfd.readouterr()
    assert out == '<2> 10 20 '


def test_dot_s_in_colon_body(capfd):
    inner, outer = make_outer()
    outer.interpret_line(': show .S ;')
    outer.interpret_line('7 8 show')
    out, _ = capfd.readouterr()
    assert out == '<2> 7 8 '
