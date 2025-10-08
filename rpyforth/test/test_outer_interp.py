from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter

def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner

def test_interp_line():
    assert run(": SQUARE DUP * ; 3 SQUARE").ds.pop().intval == 9
    assert run(": INC 1 + ;  5 INC").ds.pop().intval == 6
