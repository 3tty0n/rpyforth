from rpyforth.util import split_whitespace, remove_comments
from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_paren_word_name_not_a_comment():
    # (checkTime) is a NAME (no space after '('), not a comment. fcp defines : (checkTime) ... ; and words like (mv), (checkTime).
    toks = split_whitespace(": (checkTime) ( -- )")
    assert toks == [":", "(checkTime)"]


def test_real_paren_comment_still_stripped():
    # A standalone '(' (space after) is a comment.
    assert split_whitespace("1 ( two ) 3") == ["1", "3"]
    assert split_whitespace("foo ( a b c ) bar") == ["foo", "bar"]


def test_paren_comment_at_line_start():
    assert split_whitespace("( whole line comment )") == []


def test_define_paren_named_word():
    inner, outer = run_lines([
        ": (checkTime) ( -- ) 7 ;",
        "(checkTime)",
    ])
    assert inner.pop_ds_int() == 7


def test_paren_name_with_trailing_comment():
    inner, outer = run_lines([
        ": (mv) ( x -- x ) 1+ ; ( defines a paren-named word )",
        "5 (mv)",
    ])
    assert inner.pop_ds_int() == 6
