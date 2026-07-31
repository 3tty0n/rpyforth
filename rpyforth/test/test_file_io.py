import os
import tempfile

from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def _write_temp(content):
    fd, path = tempfile.mkstemp(suffix=".fs")
    os.write(fd, content)
    os.close(fd)
    return path


def test_include_parses_filename():
    path = _write_temp(": forty-two 42 ;\n")
    try:
        inner = run("INCLUDE " + path + "  forty-two")
        assert inner.pop_ds_int() == 42
    finally:
        os.remove(path)


def test_included_takes_string():
    path = _write_temp(": hundred 100 ;\n")
    try:
        inner = run('S" ' + path + '" INCLUDED  hundred')
        assert inner.pop_ds_int() == 100
    finally:
        os.remove(path)


def test_include_multiline_definition():
    path = _write_temp(": sq dup * ;\n: cube dup sq * ;\n")
    try:
        inner = run("INCLUDE " + path + "  3 cube")
        assert inner.pop_ds_int() == 27
    finally:
        os.remove(path)
