from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    for l in lines:
        outer.interpret_line(l)
    return inner, outer


def _read_chars(inner, addr, u):
    out = []
    for k in range(u):
        out.append(chr(inner.char_fetch(addr + k)))
    return "".join(out)


# --- FDEPTH ---

def test_fdepth_empty():
    inner, outer = run_lines(["fdepth"])
    assert inner.pop_ds_int() == 0


def test_fdepth_two():
    inner, outer = run_lines(["1e 2e fdepth"])
    assert inner.pop_ds_int() == 2
    # leave the floats consistent
    assert inner.pop_ds_float() == 2.0
    assert inner.pop_ds_float() == 1.0


# --- SEARCH ---

def test_search_found():
    # ( c-addr3 u3 flag ) flag=-1, u3=5, c-addr3 points at "world" in the tail.
    inner, outer = run_lines(['s" hello world" s" world" search'])
    flag = inner.pop_ds_int()
    u3 = inner.pop_ds_int()
    a3 = inner.pop_ds_int()
    assert flag == -1
    assert u3 == 5
    assert _read_chars(inner, a3, u3) == "world"


def test_search_found_at_start():
    inner, outer = run_lines(['s" hello world" s" hello" search'])
    flag = inner.pop_ds_int()
    u3 = inner.pop_ds_int()
    a3 = inner.pop_ds_int()
    assert flag == -1
    assert u3 == 11
    assert _read_chars(inner, a3, u3) == "hello world"


def test_search_not_found():
    # unchanged c-addr1/u1, flag = 0
    inner, outer = run_lines(['s" hello" s" xyz" search'])
    flag = inner.pop_ds_int()
    u3 = inner.pop_ds_int()
    a3 = inner.pop_ds_int()
    assert flag == 0
    assert u3 == 5
    assert _read_chars(inner, a3, u3) == "hello"


def test_search_empty_needle():
    # empty needle matches at start (gforth: found, whole string)
    inner, outer = run_lines(['s" hello" s" " search'])
    flag = inner.pop_ds_int()
    u3 = inner.pop_ds_int()
    a3 = inner.pop_ds_int()
    assert flag == -1
    assert u3 == 5


# --- REPRESENT ---

def _represent(rf_line, u):
    inner, outer = run_lines([
        rf_line + " pad " + str(u) + " represent"
    ])
    f2 = inner.pop_ds_int()
    f1 = inner.pop_ds_int()
    n = inner.pop_ds_int()
    return inner, n, f1, f2


def test_represent_pi():
    inner, n, f1, f2 = _represent("3.14e0", 20)
    assert n == 1
    assert f1 == 0
    assert f2 == -1
    # digits start with "314"
    inner2, outer2 = run_lines(["pad"])
    pad = inner2.pop_ds_int()
    digits = _read_chars(inner, pad, 20)
    assert digits.startswith("314")
    assert digits == "31400000000000001243"


def test_represent_neg():
    inner, n, f1, f2 = _represent("-2.5e0", 20)
    assert n == 1
    assert f1 == -1
    assert f2 == -1
    inner2, outer2 = run_lines(["pad"])
    pad = inner2.pop_ds_int()
    digits = _read_chars(inner, pad, 20)
    assert digits.startswith("25")


def test_represent_zero():
    inner, n, f1, f2 = _represent("0e0", 20)
    assert n == 1
    assert f1 == 0
    assert f2 == -1
    inner2, outer2 = run_lines(["pad"])
    pad = inner2.pop_ds_int()
    digits = _read_chars(inner, pad, 20)
    assert digits == "0" * 20


def test_represent_hundred():
    inner, n, f1, f2 = _represent("100e0", 20)
    assert n == 3
    assert f1 == 0
    assert f2 == -1
    inner2, outer2 = run_lines(["pad"])
    pad = inner2.pop_ds_int()
    digits = _read_chars(inner, pad, 20)
    assert digits.startswith("1")


def test_represent_small():
    inner, n, f1, f2 = _represent("0.001e0", 10)
    assert n == -2
    assert f1 == 0
    assert f2 == -1


# --- compiled ALSO / FORTH / DEFINITIONS / PREVIOUS ---

def test_compiled_order_words_no_error():
    run_lines([": setord also forth definitions previous ;", "setord"])


def test_vocabulary_compiled_also():
    inner, outer = run_lines([
        "vocabulary myv",
        ": go also myv definitions ;",
        "go",
        ": inv 7 ;",
        "forth definitions",
        "myv inv",
    ])
    # compiled ALSO/DEFINITIONS selected myv; inv was defined into it and is found when myv is on the order.
    assert inner.pop_ds_int() == 7
