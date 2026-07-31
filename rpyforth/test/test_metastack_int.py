import pytest

from rpyforth.metastack import (
    NTOP,
    FRAME_SIZE,
    ACTIVE_MAX,
    CALL_WINDOW,
    SPILL_SIZE,
    STACK_FRAGMENT_VIRTUALIZABLES,
    USE_FRAME_ONLY,
)
from rpyforth.metastack_int import DSIntMetaStack, init_fields


class _Host(object):
    pass


def test_init_fields_sets_virtualizable_host_slots():
    host = _Host()
    init_fields(host)
    assert host.t0 == 0
    assert host.t1 == 0
    assert host.cache_depth == 0
    assert host.spill_ptr == 0
    assert len(host.frame) == FRAME_SIZE
    assert len(host.spill) == SPILL_SIZE


@pytest.mark.skipif(USE_FRAME_ONLY,
                    reason="frame-only (NTOP=0) ablation drops the scalar tops")
def test_stack_fragment_virtualizables_include_tops_and_frame():
    for name in (
        "t0", "t1", "cache_depth", "frame[*]",
        "spill_ptr",
        "rs_ptr", "cs_pcs", "cs_ptr", "cs_base",
    ):
        assert name in STACK_FRAGMENT_VIRTUALIZABLES
    for name in ("spill", "spill[*]", "rs", "ds_locals"):
        assert name not in STACK_FRAGMENT_VIRTUALIZABLES


def test_int_push_pop_peek():
    s = DSIntMetaStack()
    n = ACTIVE_MAX
    for v in range(n):
        s.push(v)
    assert s.size() == n
    assert s.cache_depth == n
    assert s.peek(0) == n - 1
    assert s.peek(n - 1) == 0
    assert [s.pop() for _ in range(n)] == list(range(n - 1, -1, -1))
    assert s.size() == 0


def test_int_deep_peek_poke():
    # poke/peek across the scalar/array boundary.
    s = DSIntMetaStack()
    n = ACTIVE_MAX
    for v in range(n):
        s.push(v)
    s.poke(n - 1, 424242)        # deepest (in the frame array)
    s.poke(0, 99)                # top (a scalar)
    assert s.peek(n - 1) == 424242
    assert s.peek(0) == 99
    out = [s.pop() for _ in range(n)]
    assert out[0] == 99
    assert out[-1] == 424242


def test_int_spill_beyond_active_max():
    # Pushing past the cache spills into the overflow slot; values survive peek/pop across cache/spill boundary.
    s = DSIntMetaStack()
    n = 2 * ACTIVE_MAX + 3
    for v in range(n):
        s.push(v)
    assert s.size() == n
    assert s.cache_depth == ACTIVE_MAX
    assert s.spill_ptr == n - ACTIVE_MAX
    assert s.peek(0) == n - 1            # top, in the cache
    assert s.peek(ACTIVE_MAX) == n - 1 - ACTIVE_MAX   # just below the cache
    assert s.peek(n - 1) == 0            # bottom, deep in the spill
    assert [s.pop() for _ in range(n)] == list(range(n - 1, -1, -1))
    assert s.size() == 0
    assert s.spill_ptr == 0


def test_int_clear_resets():
    s = DSIntMetaStack()
    for v in range(5):
        s.push(v)
    s.clear()
    assert s.size() == 0
    assert s.cache_depth == 0
    assert s.spill_ptr == 0
    s.push(42)
    s.push(43)
    assert s.size() == 2


def test_int_fragment_call_within_window():
    # Caller depth fits NTOP scalar tops: nothing is parked, tops flow into the callee.
    s = DSIntMetaStack()
    for v in range(NTOP):
        s.push(v)
    s.push_fragment()
    assert s.spill_ptr == 0
    assert s.cache_depth == NTOP
    assert s.size() == NTOP
    assert s.peek(0) == NTOP - 1
    s.push(99)
    assert s.size() == NTOP + 1
    assert s.peek(0) == 99
    assert [s.pop() for _ in range(NTOP + 1)] == [99] + list(range(NTOP - 1, -1, -1))


def test_int_fragment_call_beyond_window():
    # Caller deeper than NTOP: below-top cells are parked in the spill; callee runs with cache normalized to NTOP.
    s = DSIntMetaStack()
    n = NTOP + 2
    for v in range(n):
        s.push(v)
    s.push_fragment()
    parked = n - NTOP
    assert s.cache_depth == NTOP
    assert s.spill_ptr == parked
    assert s.size() == n
    assert s.peek(0) == n - 1
    assert s.peek(NTOP - 1) == n - NTOP
    assert s.peek(NTOP) == n - NTOP - 1
    assert s.peek(n - 1) == 0
    assert s.size() == n
    assert [s.pop() for _ in range(n)] == list(range(n - 1, -1, -1))
    assert s.spill_ptr == 0


def test_int_below_window_poke():
    # Poke below the cache writes the parked spill.
    s = DSIntMetaStack()
    n = NTOP + 2
    for v in range(n):
        s.push(v)
    s.push_fragment()
    s.poke(n - 1, 777)           # deepest caller cell, parked in the spill
    assert s.peek(n - 1) == 777
    assert s.peek(n - 1) == 777


def test_int_nested_fragments_bounded_entry_depth():
    # Every entry normalizes the cache to at most NTOP; the spill absorbs the rest.
    s = DSIntMetaStack()
    depth = 40
    for v in range(depth):
        s.push(v)
        s.push_fragment()
        assert s.cache_depth <= NTOP
    assert s.size() == depth
    assert s.size() == depth
    assert [s.pop() for _ in range(depth)] == list(range(depth - 1, -1, -1))
    assert s.spill_ptr == 0


def test_int_fragment_consume_all_args():
    # A callee that drains the entire window leaves the stack empty.
    s = DSIntMetaStack()
    for v in range(NTOP):
        s.push(v)
    s.push_fragment()
    for _ in range(NTOP):
        s.pop()                  # callee drains the cache
    assert s.size() == 0
    assert s.size() == 0


def test_snapshot_restore_identity():
    s = DSIntMetaStack()
    for v in range(5):
        s.push(v)
    snap = s.snapshot()
    s.restore(snap)
    assert s.size() == 5
    assert [s.pop() for _ in range(5)] == [4, 3, 2, 1, 0]


def test_snapshot_restore_within_cache():
    s = DSIntMetaStack()
    for v in range(NTOP + 3):
        s.push(v)
    snap = s.snapshot()
    before = [s.peek(i) for i in range(s.size())]
    s.push(100)
    s.push(101)
    s.pop()
    s.poke(0, 999)
    s.restore(snap)
    assert s.size() == NTOP + 3
    assert [s.peek(i) for i in range(s.size())] == before


def test_restore_discards_pushed_values():
    s = DSIntMetaStack()
    for v in range(NTOP + 1):
        s.push(v)
    d0 = s.size()
    snap = s.snapshot()
    for v in range(50):
        s.push(v)
    s.restore(snap)
    assert s.size() == d0
    assert s.peek(0) == NTOP


def test_snapshot_is_independent_copy():
    # Overwriting every cached cell after the snapshot must not mutate it.
    s = DSIntMetaStack()
    for v in range(ACTIVE_MAX):
        s.push(v)
    snap = s.snapshot()
    for i in range(ACTIVE_MAX):
        s.poke(i, -1)
    s.restore(snap)
    assert [s.peek(i) for i in range(ACTIVE_MAX)] == [ACTIVE_MAX - 1 - i for i in range(ACTIVE_MAX)]


def test_snapshot_restore_across_spill():
    # Depth past ACTIVE_MAX; restore must rebuild the stack through the cache/spill boundary.
    s = DSIntMetaStack()
    n = 2 * ACTIVE_MAX + 5
    for v in range(n):
        s.push(v)
    snap = s.snapshot()
    before = [s.peek(i) for i in range(s.size())]
    for v in range(ACTIVE_MAX + 7):
        s.push(1000 + v)
    for _ in range(3):
        s.pop()
    s.restore(snap)
    assert s.size() == n
    assert [s.peek(i) for i in range(s.size())] == before
    assert [s.pop() for _ in range(n)] == list(range(n - 1, -1, -1))
    assert s.spill_ptr == 0


def test_snapshot_restore_nested():
    # Nested CATCH frames: each snapshot restores its own depth independently.
    s = DSIntMetaStack()
    for v in range(NTOP + 1):
        s.push(v)
    outer = s.snapshot()
    for v in range(ACTIVE_MAX):
        s.push(50 + v)
    inner = s.snapshot()
    for v in range(20):
        s.push(900 + v)
    s.restore(inner)
    assert s.size() == NTOP + 1 + ACTIVE_MAX
    assert s.peek(0) == 50 + ACTIVE_MAX - 1
    s.restore(outer)
    assert s.size() == NTOP + 1
    assert s.peek(0) == NTOP
