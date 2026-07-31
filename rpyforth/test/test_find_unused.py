from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.objects import word_from_wid


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner


def test_find_counted_string_from_word():
    # Standard FIND ( c-addr -- xt 1|-1 ): counted string from BL WORD (char memory, length byte first), as fcp uses it.
    inner = run_lines([
        "BL WORD DUP FIND",
    ])
    flag = inner.pop_ds_int()
    xt = inner.pop_ds_int()
    assert flag != 0
    assert word_from_wid(xt).name == "DUP"


def test_find_counted_string_not_found():
    inner = run_lines([
        "BL WORD NOSUCHWORDXYZ FIND",
    ])
    flag = inner.pop_ds_int()
    caddr = inner.pop_ds_int()
    assert flag == 0


def test_find_in_colon_body():
    # FIND compiled inside a colon body (fcp's [UNDEFINED]/[DEFINED] pattern).
    inner = run_lines([
        ": defined? BL WORD FIND NIP ;",
        "defined? SWAP",
    ])
    assert inner.pop_ds_int() != 0


def test_find_undefined_in_colon_body():
    inner = run_lines([
        ": defined? BL WORD FIND NIP ;",
        "defined? ZZZNOPE",
    ])
    assert inner.pop_ds_int() == 0


def test_unused_pushes_number():
    # UNUSED ( -- u ): free dictionary space. fcp calls it at load time.
    inner = run_lines(["UNUSED"])
    u = inner.pop_ds_int()
    assert u > 0
