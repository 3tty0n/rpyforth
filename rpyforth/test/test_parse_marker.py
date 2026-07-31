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


def test_parse_returns_string():
    # BL PARSE reads the next token and returns ( c-addr u ) (used by fcp FEN).
    inner, _ = run_lines([
        ": grab BL PARSE ;",
        "grab hello",
    ])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert u == 5
    assert _chars(inner, c, u) == "hello"


def test_parse_at_interpret_time():
    inner, _ = run_lines(["BL PARSE world"])
    u = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert u == 5
    assert _chars(inner, c, u) == "world"


def test_marker_defines_name():
    # MARKER defines a word; dictionary rollback is not modeled (harmless for single load).
    inner, outer = run_lines([
        "MARKER wipe",
        ": foo 1 ;",
        "wipe",
    ])
    assert "WIPE" in outer.dict


def test_marker_word_executes_without_error():
    inner, outer = run_lines([
        "MARKER pt",
        "pt",
        "42",
    ])
    assert inner.pop_ds_int() == 42


def test_quit_in_colon_unwinds():
    # QUIT compiled in a colon body must clear stacks and return to the interpreter without raising past the outer loop.
    inner, outer = run_lines([
        ": maybe-quit 1 IF QUIT THEN ;",
        "99 maybe-quit",
        "7",
    ])
    assert inner.pop_ds_int() == 7
    assert inner.ds_int_size() == 0
