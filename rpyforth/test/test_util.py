from rpyforth.util import (split_whitespace, remove_comments,
                           remove_comments_stateful, split_whitespace_stateful)


def test_remove_comments_backslash():
    assert remove_comments("\\ this is a comment") == ""
    assert remove_comments("\\ comment") == ""

    assert remove_comments("1 2 + \\ add two numbers") == "1 2 + "

    assert remove_comments("1 2 +") == "1 2 +"


def test_remove_comments_parenthetical():
    assert remove_comments("( this is a comment )") == ""
    assert remove_comments("1 ( comment ) 2") == "1  2"

    assert remove_comments("( first ) 1 ( second ) 2") == " 1  2"


def test_remove_comments_mixed():
    assert remove_comments("1 ( add ) 2 + \\ result is 3") == "1  2 + "


def test_split_whitespace_with_comments():
    assert split_whitespace("1 2 + \\ add") == ["1", "2", "+"]

    assert split_whitespace("1 ( dup it ) DUP") == ["1", "DUP"]

    assert split_whitespace("( start ) 1 2 \\ end") == ["1", "2"]

    assert split_whitespace(": DOUBLE ( n -- n*2 ) DUP + ;") == [":", "DOUBLE", "DUP", "+", ";"]


def test_split_whitespace_no_false_positives():
    # Backslash and parens not at word boundaries must not trigger comment parsing.
    assert split_whitespace("test\\value") == ["test\\value"]
    assert split_whitespace("test(value)") == ["test(value)"]


def test_paren_comment_spanning_lines():
    res, depth = remove_comments_stateful("( this comment", 0)
    assert res == ""
    assert depth == 1
    res, depth = remove_comments_stateful("spans multiple", depth)
    assert res == ""
    assert depth == 1
    res, depth = remove_comments_stateful("lines ) 42", depth)
    assert res == " 42"
    assert depth == 0


def test_paren_comment_nested_across_lines():
    # ANS ( does not nest: the first ')' closes the comment regardless of inner '(' on an earlier line.
    res, depth = remove_comments_stateful("( outer ( inner", 0)
    assert res == ""
    assert depth == 1
    res, depth = remove_comments_stateful("still ) done", depth)
    assert res.split() == ["done"]
    assert depth == 0


def test_split_whitespace_stateful_multiline():
    toks, depth = split_whitespace_stateful("code1 ( open", 0)
    assert toks == ["code1"]
    assert depth == 1
    toks, depth = split_whitespace_stateful("close ) code2 ;", depth)
    assert toks == ["code2", ";"]
    assert depth == 0


def test_split_colon_words_kept_whole():
    # Words that merely contain ':' (e.g. cd16sim's r: in:) are single tokens; ':' / ';' are delimited only by whitespace.
    assert split_whitespace(": r: registers @ CONSTANT ;") == \
        [":", "r:", "registers", "@", "CONSTANT", ";"]
    assert split_whitespace("' undef in: reset") == ["'", "undef", "in:", "reset"]
    assert split_whitespace("2^n 1- mask &sa") == ["2^n", "1-", "mask", "&sa"]


def test_paren_comment_does_not_nest():
    # ANS ( parses up to the first ')'; an inner '(' does not nest (gforth).
    from rpyforth.util import remove_comments_stateful
    out, depth = remove_comments_stateful("a ( x ( y ) b", 0)
    assert depth == 0
    assert out.split() == ["a", "b"]


def test_paren_comment_resume_does_not_nest():
    from rpyforth.util import remove_comments_stateful
    out, depth = remove_comments_stateful("still ( inside", 1)
    assert depth == 1
    out, depth = remove_comments_stateful("done ) code", 1)
    assert depth == 0
    assert out.split() == ["code"]
