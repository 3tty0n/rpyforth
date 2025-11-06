import sys

from rpyforth.inner_interp import InnerInterpreter
from rpyforth.outer_interp import OuterInterpreter

from rpython.rlib.streamio import open_file_as_stream

def entry_point(argv):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    if len(argv) < 2:
        print "Please specify the path your target file"
        return 1
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
