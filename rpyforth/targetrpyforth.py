from rpyforth.inner_interp import InnerInterpreter
from rpyforth.outer_interp import OuterInterpreter

def entry_point(argv):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    if len(argv) > 1:
        line = ' '.join(argv[1:])
        outer.interpret_line(line)
    else:
        outer.interpret_line(": SQUARE DUP * ; 3 SQUARE .")
    return 0

def target(*args):
    return entry_point, None

if __name__ == '__main__':
    import sys
    print(entry_point(sys.argv))
