from rpyforth.util import remove_comments
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


def test_backslash_inside_s_quote_not_a_comment():
    # Backslash inside S" is literal content, not a line comment (lexex savetables.fth).
    assert remove_comments('s" \\ hi " foo') == 's" \\ hi " foo'


def test_backslash_inside_dot_quote_not_a_comment():
    assert remove_comments('." \\ hi " foo') == '." \\ hi " foo'


def test_paren_inside_s_quote_not_a_comment():
    assert remove_comments('s" a ( b ) c " foo') == 's" a ( b ) c " foo'


def test_real_backslash_comment_still_stripped():
    assert remove_comments('1 2 + \\ comment') == '1 2 + '


def test_real_paren_comment_still_stripped():
    assert remove_comments(': f ( a -- b ) ;') == ': f  ;'


def test_dot_paren_with_backslash_kept():
    assert remove_comments('.( a \\ b ) rest') == '.( a \\ b ) rest'


def test_s_quote_with_backslash_writes_full_string():
    # End-to-end: S" string starting with '\ ' must emit fully without comment truncation.
    import os
    path = "/tmp/_rpyforth_scomment_test.txt"
    if os.path.exists(path):
        os.remove(path)
    run([
        's" ' + path + '" w/o create-file drop constant fh',
        ': wl fh write-line drop ;',
        ': doit  s" \\ header line" wl  s" body" wl ;',
        "doit",
        "fh close-file drop",
    ])
    data = open(path).read()
    assert data == "\\ header line\nbody\n", repr(data)
    os.remove(path)


def test_string_word_as_data_token_with_trailing_comment():
    # ." used as a data token (lexex lexinput.fth): no closing quote, trailing comment still stripped.
    assert remove_comments('symbol ."              \\ core') == 'symbol ."              '
    assert remove_comments('symbol s"   \\ core') == 'symbol s"   '


def test_quote_word_opener_only_with_closing_quote():
    # A real ." string (closing quote present) keeps embedded backslash content.
    assert remove_comments('." a \\ b" x') == '." a \\ b" x'


def test_dot_paren_without_close_falls_through_to_comment():
    # .( used as a data token with no ')' must not swallow a trailing comment.
    assert remove_comments('symbol .( \\ note') == 'symbol .( '
