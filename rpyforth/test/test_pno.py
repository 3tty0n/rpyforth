from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def pno_result(line):
    # #> now returns an ANS ( c-addr u ) pair in char memory.
    inner = run(line)
    u = inner.pop_ds_int()
    c_addr = inner.pop_ds_int()
    assert inner.depth_ds_int() == 0, "leaked %d int cells" % inner.depth_ds_int()
    chars = []
    for k in range(u):
        chars.append(chr(inner.char_fetch(c_addr + k)))
    return "".join(chars)


def test_numsign_extracts_from_low_cell():
    # gforth: 4567 0 <# # # # #> type -> "567", no leftovers
    assert pno_result("4567 0 <# # # # #>") == "567"


def test_numsign_s_single():
    assert pno_result("1 0 <# #S #>") == "1"
    assert pno_result("4567 0 <# #S #>") == "4567"


def test_numsign_s_high_cell():
    # ud = 2^64 (lo=0 hi=1); gforth prints 18446744073709551616
    assert pno_result("0 1 <# #S #>") == "18446744073709551616"


def test_sign_prepends_minus():
    # gforth: -123 dup abs 0 <# #S rot sign #> type -> "-123"
    assert pno_result("-123 dup abs 0 <# #S rot sign #>") == "-123"


def test_fcp_dot_ms_pattern_no_leak():
    # .ms from fcp: S>D 1000 UM/MOD 5 U.R ." ." 0 <# # # # #> TYPE
    inner = run(': .ms S>D 1000 UM/MOD 5 U.R ." ." 0 <# # # # #> TYPE ;'
                "  4567 .ms depth")
    assert inner.pop_ds_int() == 0
