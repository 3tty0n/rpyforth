from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner


def test_ans_type_from_char_memory(capfd):
    # ANS-style ( c-addr u ) TYPE path (brainless load-part uses WORD/COUNT + TYPE).
    inner = run_lines([
        ": grab BL WORD COUNT ;",
        "grab hiZ",
        "TYPE",
    ])
    out, _ = capfd.readouterr()
    assert "hiZ" in out


def test_boxed_type_still_works(capfd):
    # ." compiles a boxed string + TYPE; that path must keep working alongside ANS TYPE.
    run_lines([': say ." boxed" ;', "say"])
    out, _ = capfd.readouterr()
    assert "boxed" in out
