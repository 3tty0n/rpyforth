import pytest

from rpyforth.metastack import NTOP, ACTIVE_MAX, CALL_WINDOW, USE_FLOAT_FRAGMENT
from rpyforth.metastack_float import DSFloatMetaStack
from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def test_float_push_pop_order():
    s = DSFloatMetaStack()
    n = CALL_WINDOW + 2
    for v in range(n):
        s.fpush(float(v))
    assert s.fsize() == n
    got = [s.fpop() for _ in range(n)]
    assert got == [float(v) for v in range(n - 1, -1, -1)]


def test_float_deep_spill():
    s = DSFloatMetaStack()
    n = 2 * ACTIVE_MAX
    for v in range(n):
        s.fpush(float(v))
    assert s.fsize() == n
    assert s.fpeek(n - 1) == 0.0
    assert s.fpeek(0) == float(n - 1)
    got = [s.fpop() for _ in range(n)]
    assert got == [float(v) for v in range(n - 1, -1, -1)]


def test_float_nested_fragments():
    s = DSFloatMetaStack()
    s.fpush(1.5)
    s.fpush(2.5)
    s.push_float_fragment()
    assert s.fsize() == 2
    s.fpush(3.5)
    assert s.fpeek(0) == 3.5
    assert s.fsize() == 3


def test_float_call_window_cap():
    s = DSFloatMetaStack()
    n = CALL_WINDOW + 4
    for v in range(n):
        s.fpush(float(v))
    s.push_float_fragment()
    assert s.fdep == NTOP
    assert s.fsize() == n


def test_float_deep_recursion_chain():
    s = DSFloatMetaStack()
    depth = 12
    for v in range(depth):
        s.fpush(float(v))
        s.push_float_fragment()
    got = [s.fpop() for _ in range(depth)]
    assert got == [float(v) for v in range(depth - 1, -1, -1)]


def test_float_poke():
    s = DSFloatMetaStack()
    n = ACTIVE_MAX + 3
    for v in range(n):
        s.fpush(float(v))
    s.fpoke(0, 100.0)
    s.fpoke(n - 1, 200.0)
    s.fpoke(NTOP + 1, 300.0)
    assert s.fpeek(0) == 100.0
    assert s.fpeek(n - 1) == 200.0
    assert s.fpeek(NTOP + 1) == 300.0


def test_float_snapshot_restore():
    s = DSFloatMetaStack()
    s.fpush(1.0)
    s.fpush(2.0)
    snap = s.fsnapshot()
    s.fpush(3.0)
    s.fpush(4.0)
    assert s.fsize() == 4
    s.frestore(snap)
    assert s.fsize() == 2
    assert s.fpeek(0) == 2.0
    assert s.fpeek(1) == 1.0


def test_int_and_float_independent():
    s = DSFloatMetaStack()
    s.push(11)
    s.fpush(1.25)
    s.push(22)
    s.fpush(2.5)
    assert s.size() == 2
    assert s.fsize() == 2
    assert s.pop() == 22
    assert s.fpop() == 2.5
    assert s.pop() == 11
    assert s.fpop() == 1.25


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def test_e2e_fdup_fplus():
    inner = run("2e 3e F+ FDUP F+")
    assert inner.pop_ds_float() == 10.0


def test_e2e_fswap_fover_frot():
    inner = run("1e 2e FSWAP")
    assert inner.pop_ds_float() == 1.0
    assert inner.pop_ds_float() == 2.0

    inner = run("1e 2e FOVER")
    assert inner.pop_ds_float() == 1.0
    assert inner.pop_ds_float() == 2.0
    assert inner.pop_ds_float() == 1.0

    inner = run("1e 2e 3e FROT")
    assert inner.pop_ds_float() == 1.0
    assert inner.pop_ds_float() == 3.0
    assert inner.pop_ds_float() == 2.0


def test_e2e_fdepth():
    inner = run("1e 2e 3e FDEPTH")
    assert inner.pop_ds_int() == 3


def test_e2e_colon_word_floats():
    inner = run(": scale 2e F* ; 3e scale 4e F+")
    assert inner.pop_ds_float() == 10.0


def test_e2e_deep_float_stack_crossing_frame():
    n = ACTIVE_MAX + 5
    src = " ".join("1e" for _ in range(n))
    prog = src + " " + " ".join("F+" for _ in range(n - 1))
    inner = run(prog)
    assert inner.pop_ds_float() == float(n)


def test_e2e_fstore_ffetch_roundtrip():
    inner = run("FVARIABLE X 3.5e X F! X F@")
    assert inner.pop_ds_float() == 3.5


def test_e2e_mixed_int_float():
    inner = run(": mix 5 SWAP 2e F* ; 3e 7 mix")
    # data stack: 5 7 (7 on top) ; float stack: 6.0
    assert inner.pop_ds_float() == 6.0
    assert inner.pop_ds_int() == 7
    assert inner.pop_ds_int() == 5


def test_e2e_s_to_f_and_f_to_d():
    inner = run("42 S>F")
    assert inner.pop_ds_float() == 42.0

    # F>D leaves a double ( 9 0 ), high cell on top.
    inner = run("9e F>D")
    hi = inner.pop_ds_int()
    lo = inner.pop_ds_int()
    assert hi == 0
    assert lo == 9


def test_e2e_catch_restores_float_depth():
    # Floats pushed before THROW are discarded; only the pre-CATCH float survives.
    inner = run("9e  : bad 1e 2e 3e 7 THROW ;  ' bad CATCH")
    assert inner.pop_ds_int() == 7          # throw code
    assert inner.pop_ds_float() == 9.0      # float depth restored
    assert inner.depth_ds_float() == 0


def test_e2e_catch_no_throw_keeps_floats():
    inner = run("1e  : good 2e 3e F+ ;  ' good CATCH")
    assert inner.pop_ds_int() == 0          # no throw
    assert inner.pop_ds_float() == 5.0
    assert inner.pop_ds_float() == 1.0


def test_e2e_float_heapish_pattern():
    prog = (": sq FDUP F* ; "
            ": hyp sq FSWAP sq F+ FSQRT ; "
            "3e 4e hyp")
    inner = run(prog)
    assert abs(inner.pop_ds_float() - 5.0) < 1e-9
