import os
import subprocess
import sys

import pytest

from rpyforth.metastack import NTOP, ACTIVE_MAX, CALL_WINDOW
from rpyforth.metastack_int_frameonly import DSIntMetaStackFrameOnly


def test_push_pop_order_small():
    s = DSIntMetaStackFrameOnly()
    for v in range(3):
        s.push(v)
    assert s.size() == 3
    assert [s.pop() for _ in range(3)] == [2, 1, 0]


def test_push_pop_across_cache_boundary():
    # Push past ACTIVE_MAX to force spill evacuation, then drain in order.
    n = 2 * ACTIVE_MAX + 3
    s = DSIntMetaStackFrameOnly()
    for v in range(n):
        s.push(v)
    assert s.size() == n
    assert [s.pop() for _ in range(n)] == list(range(n - 1, -1, -1))


def test_peek_across_boundary_and_spill():
    n = ACTIVE_MAX + 5
    s = DSIntMetaStackFrameOnly()
    for v in range(n):
        s.push(v)
    for depth in range(n):
        assert s.peek(depth) == n - 1 - depth


def test_poke_across_boundary_and_spill():
    n = ACTIVE_MAX + 5
    s = DSIntMetaStackFrameOnly()
    for v in range(n):
        s.push(v)
    s.poke(0, 100)
    s.poke(n - 1, 200)
    s.poke(ACTIVE_MAX, 300)   # first spilled cell below the cache
    assert s.peek(0) == 100
    assert s.peek(n - 1) == 200
    assert s.peek(ACTIVE_MAX) == 300


def test_top_floats_at_frame_top():
    s = DSIntMetaStackFrameOnly()
    s.push(7)
    s.push(8)
    assert s.frame[s.cache_depth - 1] == 8
    assert s.frame[s.cache_depth - 2] == 7


@pytest.mark.parametrize("depth", [1, 2, 5, 10, 15])
def test_call_park_roundtrip(depth):
    # Park below-window cells, keep CALL_WINDOW tops, and verify full stack order.
    s = DSIntMetaStackFrameOnly()
    for v in range(depth):
        s.push(v)
    s.push_fragment()
    # Cache is normalized to at most the argument window.
    assert s.cache_depth <= CALL_WINDOW
    assert s.size() == depth
    # Argument-window tops are still readable at the shallow depths.
    for k in range(min(depth, CALL_WINDOW)):
        assert s.peek(k) == depth - 1 - k
    assert s.size() == depth
    assert [s.pop() for _ in range(depth)] == list(range(depth - 1, -1, -1))


def test_deep_recursion_chain():
    s = DSIntMetaStackFrameOnly()
    depth = 12
    for v in range(depth):
        s.push(v)
        s.push_fragment()
    assert [s.pop() for _ in range(depth)] == list(range(depth - 1, -1, -1))


def test_snapshot_restore():
    n = ACTIVE_MAX + 2
    s = DSIntMetaStackFrameOnly()
    for v in range(n):
        s.push(v)
    snap = s.snapshot()
    s.push(999)
    s.push(998)
    assert s.size() == n + 2
    s.restore(snap)
    assert s.size() == n
    for depth in range(n):
        assert s.peek(depth) == n - 1 - depth


def test_reset_clears_everything():
    s = DSIntMetaStackFrameOnly()
    for v in range(ACTIVE_MAX + 3):
        s.push(v)
    s.clear()
    assert s.size() == 0
    s.push(42)
    assert s.pop() == 42


def _run_forth(line):
    env = dict(os.environ)
    env["RPYFORTH_STACK_LAYOUT"] = "frame-only"
    env["PYTHONPATH"] = os.pathsep.join([p for p in sys.path if p])
    script = (
        "from rpyforth.outer_interp import OuterInterpreter\n"
        "from rpyforth.inner_interp import InnerInterpreter\n"
        "inner = InnerInterpreter()\n"
        "outer = OuterInterpreter(inner)\n"
        "outer.interpret_line(%r)\n"
        "vals = []\n"
        "while inner.depth_ds_int() > 0:\n"
        "    vals.append(inner.pop_ds_int())\n"
        "print(' '.join(str(v) for v in vals))\n"
    ) % (line,)
    out = subprocess.check_output(
        [sys.executable, "-c", script], env=env,
        cwd=os.getcwd(), stderr=subprocess.STDOUT,
    )
    text = out.decode("utf-8").strip().splitlines()
    last = text[-1] if text else ""
    if not last:
        return []
    return [int(x) for x in last.split()]


def test_e2e_deep_stack_sum():
    # Push more ints than the cache holds and sum them, crossing frame->spill.
    n = ACTIVE_MAX + 5
    prog = " ".join("1" for _ in range(n)) + " " + " ".join("+" for _ in range(n - 1))
    assert _run_forth(prog) == [n]


def test_e2e_colon_call_boundary():
    # A colon word consumes args across the call boundary (parking path).
    assert _run_forth(": add3 + + ; 10 20 30 add3") == [60]


def test_e2e_recursive_fib():
    prog = (": fib dup 2 < if drop 1 else dup 1 - recurse swap 2 - recurse + then ; 10 fib")
    assert _run_forth(prog) == [89]


def test_e2e_catch_restores_depth():
    # Pre-CATCH int survives; ints pushed before THROW are discarded.
    got = _run_forth("99 : bad 1 2 3 7 THROW ; ' bad CATCH")
    assert got == [7, 99]


def test_e2e_catch_no_throw():
    got = _run_forth("5 : good 2 3 + ; ' good CATCH")
    assert got == [0, 5, 5]


def test_e2e_deep_pick():
    n = ACTIVE_MAX + 4
    prog = " ".join(str(v) for v in range(n)) + " " + str(n - 1) + " PICK"
    assert _run_forth(prog)[0] == 0
