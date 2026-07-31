"""Frame-only (NTOP=0) variant of the int metastack (metastack_int.py).

Drops the scalar tops t0, t1 entirely: every cached cell lives in the
virtualizable ``frame`` array, top at frame[cache_depth-1], so push is
``frame[cache_depth]=v; cache_depth+=1`` with no data movement (the flagship's
"t0=top" invariant costs a shift on every push/pop). Inside a trace the array
slots are virtual registers and the depth-derived indices fold, like PyPy's
fastlocals_w[*].

``frame`` holds ACTIVE_MAX = NTOP + FRAME_SIZE cells, the same total the flagship
caches. frame[0] is the deepest cell, cache_depth the count (0..ACTIVE_MAX), and
everything below sits in the shared ``spill``. The call boundary keeps
CALL_WINDOW = NTOP, parking below-window cells and normalizing cache_depth, so
the calling convention matches the flagship. See docs/NOTE_STACK_LAYOUT.md.
"""

from rpython.rlib.jit import promote, unroll_safe
from rpython.rlib.debug import make_sure_not_resized

from rpyforth.metastack import (
    NTOP,
    ACTIVE_MAX,
    CALL_WINDOW,
    SPILL_SIZE,
    DataStackOverflow,
    DSMetaStack,
)


def init_fields(host):
    """Install the host-resident frame-only active cache + metastack spill."""
    # Active cache: top ACTIVE_MAX cells in the virtualizable ``frame`` array (frame[0] deepest). No scalar tops; push/pop move no data.
    host.cache_depth = 0
    host.frame = [0] * ACTIVE_MAX
    make_sure_not_resized(host.frame)

    # Shared spill: plain heap (immutable reference), one per VM; every fragment is a window [0, spill_ptr).
    host.spill = [0] * SPILL_SIZE
    make_sure_not_resized(host.spill)
    host.spill_ptr = 0


class DSCacheSnapshotFrameOnly(object):
    """Immutable snapshot of the frame-only cache: cache_depth, a private copy of
    the frame and the spill pointer. The shared spill is not copied; restore rolls
    spill_ptr back and relies on the cells below it being undisturbed."""

    _immutable_fields_ = ["cache_depth", "frame[*]", "spill_ptr"]

    def __init__(self, cache_depth, frame, spill_ptr):
        self.cache_depth = cache_depth
        self.frame = frame
        self.spill_ptr = spill_ptr


def snapshot_cache(host):
    """Capture host's frame-only cache. Copies the fixed-size frame so later
    cache writes cannot disturb the snapshot."""
    frame_copy = [0] * ACTIVE_MAX
    i = 0
    while i < ACTIVE_MAX:
        frame_copy[i] = host.frame[i]
        i += 1
    make_sure_not_resized(frame_copy)
    return DSCacheSnapshotFrameOnly(host.cache_depth, frame_copy,
                                    host.spill_ptr)


def restore_cache(host, snap):
    """Roll host's stack back to a snapshot, discarding everything pushed since.
    Restores the cache and the cache/spill pointers; the spill cells below the
    saved spill pointer are left in place."""
    host.cache_depth = snap.cache_depth
    i = 0
    while i < ACTIVE_MAX:
        host.frame[i] = snap.frame[i]
        i += 1
    host.spill_ptr = snap.spill_ptr


class DSIntMetaStackFrameOnly(DSMetaStack):
    """Integer data stack in the frame-only (NTOP=0) layout:
    frame[ACTIVE_MAX] (virtualizable) | one shared spill array. The spill is
    allocated once per VM; a fragment is just the window [0, spill_ptr) onto it,
    so nest/unnest allocates nothing."""

    def init_fields(self):
        init_fields(self)

    # Hot path: every cached cell is a virtualizable frame slot; depth-derived indices fold inside a trace.
    def push_on(self, v):
        dd = self.cache_depth
        if dd >= ACTIVE_MAX:
            self._spill_bottom()
            dd = self.cache_depth
        assert dd >= 0
        self.frame[dd] = v
        self.cache_depth = dd + 1

    def pop_on(self):
        dd = self.cache_depth
        if dd <= 0:
            return self._pop_from_spill()
        dd -= 1
        assert dd >= 0
        r = self.frame[dd]
        self.cache_depth = dd
        return r

    def _pop_from_spill(self):
        # Underflow is an interpreter bug; assert instead of branching on the hot path.
        ap = self.spill_ptr - 1
        assert ap >= 0
        r = self.spill[ap]
        self.spill_ptr = ap
        return r

    @unroll_safe
    def _spill_bottom(self):
        # Cold path: only when depth exceeds ACTIVE_MAX.
        ap = self.spill_ptr
        if ap >= SPILL_SIZE:
            raise DataStackOverflow()
        assert ap >= 0
        self.spill[ap] = self.frame[0]
        self.spill_ptr = ap + 1
        i = 0
        while i < ACTIVE_MAX - 1:
            self.frame[i] = self.frame[i + 1]
            i += 1
        self.cache_depth = self.cache_depth - 1

    def peek_on(self, depth):
        depth = promote(depth)
        dd = self.cache_depth
        if depth < dd:
            si = dd - 1 - depth
            assert si >= 0
            return self.frame[si]
        ai = self.spill_ptr - 1 - (depth - dd)
        assert ai >= 0
        return self.spill[ai]

    def poke_on(self, depth, v):
        depth = promote(depth)
        dd = self.cache_depth
        if depth < dd:
            si = dd - 1 - depth
            assert si >= 0
            self.frame[si] = v
            return
        ai = self.spill_ptr - 1 - (depth - dd)
        assert ai >= 0
        self.spill[ai] = v

    def depth_on(self):
        return self.cache_depth + self.spill_ptr

    def reset_on(self):
        self.cache_depth = 0
        self.spill_ptr = 0

    # Call entry / return: return is O(1); _spill_bottom avoids runtime-offset index into the virtualizable array.
    @unroll_safe
    def push_fragment_on(self):
        dd = self.cache_depth
        while dd > CALL_WINDOW:
            self._spill_bottom()
            dd -= 1

    def __init__(self):
        self.init_fields()

    def push(self, v):
        self.push_on(v)

    def pop(self):
        return self.pop_on()

    def peek(self, depth):
        return self.peek_on(depth)

    def poke(self, depth, v):
        self.poke_on(depth, v)

    def size(self):
        return self.depth_on()

    def clear(self):
        self.reset_on()

    def push_fragment(self):
        self.push_fragment_on()

    def snapshot(self):
        return snapshot_cache(self)

    def restore(self, snap):
        restore_cache(self, snap)
