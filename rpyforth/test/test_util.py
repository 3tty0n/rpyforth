from rpyforth.util import split_whitespace, remove_comments


def test_remove_comments_backslash():
    # Backslash comment at start
    assert remove_comments("\\ this is a comment") == ""
    assert remove_comments("\\ comment") == ""

    # Backslash comment after code
    assert remove_comments("1 2 + \\ add two numbers") == "1 2 + "

    # No comment
    assert remove_comments("1 2 +") == "1 2 +"


def test_remove_comments_parenthetical():
    # Parenthetical comment
    assert remove_comments("( this is a comment )") == ""
    assert remove_comments("1 ( comment ) 2") == "1  2"

    # Multiple comments
    assert remove_comments("( first ) 1 ( second ) 2") == " 1  2"


def test_remove_comments_mixed():
    # Both types
    assert remove_comments("1 ( add ) 2 + \\ result is 3") == "1  2 + "


def test_split_whitespace_with_comments():
    # Backslash comment
    assert split_whitespace("1 2 + \\ add") == ["1", "2", "+"]

    # Parenthetical comment
    assert split_whitespace("1 ( dup it ) DUP") == ["1", "DUP"]

    # Mixed
    assert split_whitespace("( start ) 1 2 \\ end") == ["1", "2"]

    # Colon definition with comment
    assert split_whitespace(": DOUBLE ( n -- n*2 ) DUP + ;") == [":", "DOUBLE", "DUP", "+", ";"]


def test_split_whitespace_no_false_positives():
    # Backslash not at word boundary shouldn't be treated as comment
    # (though in Forth it typically would need a space before)
    assert split_whitespace("test\\value") == ["test\\value"]

    # Parentheses in the middle of a token shouldn't trigger comment
    assert split_whitespace("test(value)") == ["test(value)"]
