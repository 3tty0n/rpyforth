from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_inline_defines_word_like_colon():
    # :inline must behave structurally like : name body ; (fcp's profiling fallback is exactly : :inline : ;).
    inner, _ = run_lines([
        ":inline piece 7 AND ;",
        ": t 15 piece ;",
        "t",
    ])
    assert inner.pop_ds_int() == (15 & 7)


def test_inline_reported_defined():
    # fcp guards its own :inline with [UNDEFINED] :inline [IF]; we must report it defined so that block is skipped.
    inner, _ = run_lines([
        "[UNDEFINED] :inline 0= ",
    ])
    assert inner.pop_ds_int() == -1


def test_inline_with_stack_comment_tail():
    # fcp writes :inline otherSide COLORMASK XOR ; ( color -- ~color ) — trailing stack comment must not confuse parsing.
    inner, _ = run_lines([
        "$30 CONSTANT COLORMASK",
        ":inline otherSide COLORMASK XOR ; ( color -- ~color )",
        ": t $10 otherSide ;",
        "t",
    ])
    assert inner.pop_ds_int() == (0x10 ^ 0x30)
