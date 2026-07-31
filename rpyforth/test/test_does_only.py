from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_does_only_word_rebinds_prior_create():
    # DOES>-only idiom (lexex 1darray): a defining word with no CREATE of its own patches a separately CREATEd word so it runs the DOES> body. Must consume its index argument and leave exactly the computed address.
    inner, _ = run([
        "create dat 111 , 222 , 333 , 444 , 555 ,",
        ": arr does> @ swap cells + ;",   # ( i -- ad ) ad = dat + i*cells; body holds dat
        "create aa dat , arr",
        "0 aa @",
    ])
    assert inner.pop_ds_int() == 111
    assert inner.ds_int_size() == 0


def test_does_only_consumes_index_no_leak():
    # The bug: aa retained default CREATE behavior (push body addr) and left the index on the stack, leaking one cell per call.
    inner, _ = run([
        "create dat 10 , 20 , 30 ,",
        ": arr does> @ swap cells + ;",
        "create aa dat , arr",
        "2 aa @",
    ])
    assert inner.pop_ds_int() == 30
    assert inner.ds_int_size() == 0


def test_in_word_create_does_still_works():
    # The common CREATE ... DOES> in a single word must keep working.
    inner, _ = run([
        ": const2 create , does> @ ;",
        "42 const2 answer",
        "answer",
    ])
    assert inner.pop_ds_int() == 42
    assert inner.ds_int_size() == 0
