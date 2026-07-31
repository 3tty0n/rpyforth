from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def _chars(inner, c_addr, u):
    return "".join(chr(inner.char_fetch(c_addr + k)) for k in range(u))


def test_parse_scans_to_delimiter_across_spaces():
    # gforth-verified: `[char] } parse` from a colon body scans the current input line from >IN to the next '}', spanning intervening spaces.
    inner, _ = run_lines([
        ": grab [char] } parse ;",
        "grab aa bb cc}xx",
    ])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert u == 8
    assert _chars(inner, c, u) == "aa bb cc"


def test_parse_empty_when_delimiter_immediate():
    inner, _ = run_lines([
        ": grab [char] } parse ;",
        "grab }rest",
    ])
    u = inner.pop_ds_int()
    inner.pop_ds_int()
    assert u == 0


def test_parse_to_end_of_line_when_no_delimiter():
    inner, _ = run_lines([
        ": grab [char] } parse ;",
        "grab aa bb",
    ])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert _chars(inner, c, u) == "aa bb"


def test_parse_length_hello_world():
    inner, _ = run_lines([
        ": grab [char] } parse nip ;",
        "grab hello}world}",
    ])
    assert inner.pop_ds_int() == 5


def test_cvs_style_immediate_parse():
    # brew's `cvs"` word: an IMMEDIATE parsing word that trims CVS markers and compiles the remaining text with SLITERAL.
    inner, _ = run_lines([
        ': cvs" [char] " parse swap 6 + swap 12 - 2 max POSTPONE sliteral ; IMMEDIATE',
        ': t cvs" \tHELLOWORLD$Id: x,v 1.0 stuff $\t" ;',
        "t",
    ])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    # gforth-fast produces "WORLD$Id: x,v 1.0 st" for this input.
    assert _chars(inner, c, u) == "WORLD$Id: x,v 1.0 st"
