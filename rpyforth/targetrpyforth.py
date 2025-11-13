import sys

from rpyforth.inner_interp import InnerInterpreter
from rpyforth.outer_interp import OuterInterpreter

from rpython.rlib import jit
from rpython.rlib.streamio import open_file_as_stream

def entry_point(argv):
    for i in range(len(argv)):
        if argv[i] == "--jit":
            if len(argv) == i + 1:
                print("missing argument after --jit")
                return 2
            jitarg = argv[i + 1]
            del argv[i:i+2]
            jit.set_user_param(None, jitarg)
            break

    if len(argv) < 2:
        print("Usage: %s filename x" % (argv[0],))
        return 2

    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    path = argv[1]
    f = open_file_as_stream(path)
    for line in f.readall().split('\n'):
        outer.interpret_line(line)
    f.close()
    return 0

def target(driver, args):
    driver.exe_name = "rpyforth-%(backend)s"
    return entry_point, None

if __name__ == '__main__':
    import sys
    print(entry_point(sys.argv))
