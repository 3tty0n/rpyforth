from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_does_accessor_call_site_is_flattened():
    # A CREATE..DOES> child is [LIT addr, does_word, EXIT]; compiling a call to
    # it must splice LIT addr + the does body instead of calling does_word
    # (lexex spends 78% of its time in a per-bit bitvec/member? round-trip).
    inner, outer = run_lines([
        ": const create , does> @ ;",
        "7 const seven",
        ": t seven 3 + ;",
        "t",
    ])
    assert inner.pop_ds_int() == 10
    sw = outer.dict["SEVEN"]
    dw = sw.thread.code[1]
    tcode = outer.dict["T"].thread.code
    assert sw not in tcode
    assert dw not in tcode


def test_offset_accessor_like_mini_oof_var():
    # xmini_oof: : var create over , + does> @ + ; -- the hot bitvec shape.
    inner, outer = run_lines([
        ": offs create , does> @ + ;",
        "16 offs fld",
        ": t 100 fld ;",
        "t",
    ])
    assert inner.pop_ds_int() == 116
    fw = outer.dict["FLD"]
    tcode = outer.dict["T"].thread.code
    assert fw not in tcode
    assert fw.thread.code[1] not in tcode


def test_does_body_with_loop_relocates_targets():
    inner, outer = run_lines([
        ": add3 create , does> @ 3 0 do 1+ loop ;",
        "5 add3 a5",
        ": t a5 ;",
        "t",
    ])
    assert inner.pop_ds_int() == 8


def test_oversized_loop_body_stays_called_and_correct():
    inner, outer = run_lines([
        ": sum-to create , does> @ 0 swap 0 do i + loop ;",
        "5 sum-to s5",
        ": t s5 ;",
        "t",
    ])
    assert inner.pop_ds_int() == 0 + 1 + 2 + 3 + 4


def test_does_body_with_early_exit():
    inner, outer = run_lines([
        ": cw create , does> @ dup 0= if exit then 1+ ;",
        "0 cw z  5 cw f",
        ": tz z ;  : tf f ;",
        "tz", "tf",
    ])
    assert inner.pop_ds_int() == 6
    assert inner.pop_ds_int() == 0


def test_oversized_does_body_stays_called():
    body = "@ " + "1 + " * 40
    inner, outer = run_lines([
        ": big create , does> %s;" % body,
        "2 big b2",
        ": t b2 1 - ;",
        "t",
    ])
    assert inner.pop_ds_int() == 41
    assert outer.dict["B2"].thread.code[1] in outer.dict["T"].thread.code


def test_interpreted_call_still_works():
    # EXECUTE / interpret-mode use of the child word must keep the call path.
    inner, outer = run_lines([
        ": const create , does> @ ;",
        "9 const nine",
        "nine",
        "' nine execute",
    ])
    assert inner.pop_ds_int() == 9
    assert inner.pop_ds_int() == 9
