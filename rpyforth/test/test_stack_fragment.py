from rpyforth.metastack import NTOP, ACTIVE_MAX, CALL_WINDOW
from rpyforth.metastack_int import DSIntMetaStack


def test_push_pop_order():
    s = DSIntMetaStack()
    n = CALL_WINDOW + 2
    for v in range(n):
        s.push(v)
    assert s.size() == n
    assert [s.pop() for _ in range(n)] == list(range(n - 1, -1, -1))


def test_nested_fragments():
    s = DSIntMetaStack()
    s.push(1)
    s.push(2)
    s.push_fragment()
    assert s.size() == 2          # total depth preserved across entry
    s.push(3)
    assert s.peek(0) == 3
    assert s.size() == 3


def test_call_window_cap():
    # A caller deeper than the tops has its cache normalized to NTOP on entry.
    s = DSIntMetaStack()
    n = CALL_WINDOW + 4
    for v in range(n):
        s.push(v)
    s.push_fragment()
    assert s.cache_depth == NTOP
    assert s.size() == n          # nothing lost: the rest is parked


def test_deep_recursion_chain():
    s = DSIntMetaStack()
    depth = 12
    for v in range(depth):
        s.push(v)
        s.push_fragment()
    assert [s.pop() for _ in range(depth)] == list(range(depth - 1, -1, -1))


def test_large_stack():
    # Push well past the cache so the spill is exercised, then read it all back.
    s = DSIntMetaStack()
    n = 2 * ACTIVE_MAX
    for v in range(n):
        s.push(v)
    assert s.size() == n
    assert s.peek(n - 1) == 0
    assert s.peek(0) == n - 1
    assert [s.pop() for _ in range(n)] == list(range(n - 1, -1, -1))
