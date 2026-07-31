from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def _chars(inner, c_addr, u):
    return "".join(chr(inner.char_fetch(c_addr + k)) for k in range(u))


def test_parse_keeps_paren_as_data():
    # gforth-verified: `[char] " parse` keeps a '(' inside the parsed text (it is data, not a comment). brew's help-node" / cvs" strings rely on this.
    inner, outer = run_lines([
        ": grab [char] \" parse ;",
        "grab abc ( def\"",
    ])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert _chars(inner, c, u) == "abc ( def"


def test_definition_with_paren_string_closes_and_following_lines_run():
    # The ';' that closes a definition containing a parse-string with '(' must still be seen, and later lines must not be swallowed as a paren comment.
    inner, outer = run_lines([
        ": hn 2drop ;",
        ": hnq [char] \" parse hn ; IMMEDIATE",
        ": d1 hnq has a ( paren\" ;",
        "55 constant v2",
        "v2",
    ])
    assert "D1" in outer.dict
    assert inner.pop_ds_int() == 55
