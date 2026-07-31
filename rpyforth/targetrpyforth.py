import sys

from rpyforth.config import EXE_NAME
from rpyforth.inner_interp import InnerInterpreter, Bye, Abort
from rpyforth.objects import ForthException
from rpyforth.outer_interp import OuterInterpreter
from rpyforth.metastack import DataStackOverflow
from rpyforth.prelude import load_prelude

from rpython.rlib import jit
from rpython.rlib.streamio import open_file_as_stream

def entry_point(argv):
    jit.set_user_param(None, "trace_limit=200000,retrace_limit=60")
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
    load_prelude(outer)
    path = argv[1]
    args = []
    if len(argv) > 2:
        args = argv[2:]
    inner.argv = args
    f = open_file_as_stream(path)
    err = 0
    try:
        for line in f.readall().split('\n'):
            outer.interpret_line(line)
    except Bye:
        pass
    except Abort:
        inner.reset_after_abort()
        err = 1
    except DataStackOverflow:
        print("data stack overflow")
        err = 1
    except ForthException as e:
        print("uncaught THROW: %d" % e.code)
        err = 1
    f.close()
    return err

def target(driver, args):
    driver.exe_name = EXE_NAME
    return entry_point, None

if __name__ == '__main__':
    import sys
    print(entry_point(sys.argv))
