import os
import tempfile

from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def make():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    return inner, outer


def run(line):
    inner, outer = make()
    outer.interpret_line(line)
    return inner


def _write_temp(content):
    fd, path = tempfile.mkstemp(suffix=".dat")
    os.write(fd, content)
    os.close(fd)
    return path


def test_modes_are_distinct_constants():
    inner = run("R/O R/W W/O")
    wo = inner.pop_ds_int()
    rw = inner.pop_ds_int()
    ro = inner.pop_ds_int()
    assert ro != rw
    assert rw != wo
    assert ro != wo


def test_std_handles():
    inner = run("STDIN STDOUT STDERR")
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 0


def test_open_read_close_line():
    path = _write_temp("hello\nworld\n")
    try:
        inner, outer = make()
        outer.interpret_line("CREATE BUF 64 ALLOT")
        outer.interpret_line('S" ' + path + '" R/O OPEN-FILE')
        ior = inner.pop_ds_int()
        assert ior == 0
        fid = inner.pop_ds_int()
        outer.interpret_line("BUF 64 %d READ-LINE" % fid)
        rl_ior = inner.pop_ds_int()
        flag = inner.pop_ds_int()
        n = inner.pop_ds_int()
        assert rl_ior == 0
        assert flag == -1
        assert n == 5
        outer.interpret_line("BUF C@  BUF 1+ C@  BUF 4 + C@")
        assert inner.pop_ds_int() == ord('o')
        assert inner.pop_ds_int() == ord('e')
        assert inner.pop_ds_int() == ord('h')
        outer.interpret_line("BUF 64 %d READ-LINE" % fid)
        inner.pop_ds_int()  # ior
        assert inner.pop_ds_int() == -1  # flag
        assert inner.pop_ds_int() == 5   # "world"
        outer.interpret_line("BUF 64 %d READ-LINE" % fid)
        inner.pop_ds_int()  # ior
        assert inner.pop_ds_int() == 0   # flag false at EOF
        inner.pop_ds_int()  # n
        outer.interpret_line("%d CLOSE-FILE" % fid)
        assert inner.pop_ds_int() == 0
    finally:
        os.remove(path)


def test_open_missing_file_returns_ior():
    inner = run('S" /nonexistent/path/zzz.dat" R/O OPEN-FILE')
    ior = inner.pop_ds_int()
    inner.pop_ds_int()  # fileid (undefined)
    assert ior != 0


def test_read_file_bulk():
    path = _write_temp("abcdef")
    try:
        inner, outer = make()
        outer.interpret_line("CREATE B 64 ALLOT")
        outer.interpret_line('S" ' + path + '" R/O OPEN-FILE')
        inner.pop_ds_int()
        fid = inner.pop_ds_int()
        outer.interpret_line("B 64 %d READ-FILE" % fid)
        ior = inner.pop_ds_int()
        n = inner.pop_ds_int()
        assert ior == 0
        assert n == 6
        outer.interpret_line("B C@  B 5 + C@")
        assert inner.pop_ds_int() == ord('f')
        assert inner.pop_ds_int() == ord('a')
        outer.interpret_line("%d CLOSE-FILE DROP" % fid)
    finally:
        os.remove(path)


def test_write_and_read_back():
    fd, path = tempfile.mkstemp(suffix=".out")
    os.close(fd)
    os.remove(path)
    try:
        inner, outer = make()
        outer.interpret_line("CREATE B 16 ALLOT  72 B C!  105 B 1+ C!")
        outer.interpret_line('S" ' + path + '" W/O CREATE-FILE')
        inner.pop_ds_int()  # ior
        fid = inner.pop_ds_int()
        outer.interpret_line("B 2 %d WRITE-FILE" % fid)
        assert inner.pop_ds_int() == 0
        outer.interpret_line("%d CLOSE-FILE DROP" % fid)
        with open(path, "rb") as f:
            assert f.read() == b"Hi"
    finally:
        if os.path.exists(path):
            os.remove(path)


def test_file_size_and_position():
    path = _write_temp("0123456789")
    try:
        inner, outer = make()
        outer.interpret_line('S" ' + path + '" R/O OPEN-FILE DROP')
        fid = inner.pop_ds_int()
        outer.interpret_line("%d FILE-SIZE" % fid)
        ior = inner.pop_ds_int()
        hi = inner.pop_ds_int()
        lo = inner.pop_ds_int()
        assert ior == 0
        assert hi == 0
        assert lo == 10
        outer.interpret_line("%d FILE-POSITION" % fid)
        inner.pop_ds_int()  # ior
        inner.pop_ds_int()  # hi
        assert inner.pop_ds_int() == 0
        outer.interpret_line("%d CLOSE-FILE DROP" % fid)
    finally:
        os.remove(path)
