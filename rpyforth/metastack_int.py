from rpython.rlib.jit import promote, unroll_safe
from rpython.rlib.debug import make_sure_not_resized

from rpyforth.metastack import (
    NTOP,
    FRAME_SIZE,
    ACTIVE_MAX,
    SPILL_SIZE,
    DataStackOverflow,
    DSFragment,
    DSMetaStack,
)


class DSIntFragment(DSFragment):
    """Legacy value object, retained only for import compatibility."""

    _immutable_fields_ = ["parent"]

    def __init__(self, parent):
        self.parent = parent


def init_fields(host):
    """Install the host-resident active-fragment + metastack spill state."""
    # Active fragment caches the stack top: NTOP cells in scalars t0/t1, the next in the virtualizable frame; cache_depth counts cached cells (0..ACTIVE_MAX).
    host.t0 = 0
    host.t1 = 0
    host.cache_depth = 0
    host.frame = [0] * FRAME_SIZE
    make_sure_not_resized(host.frame)

    # Shared spill holds every cell below the cache (parked caller frames + overflow), sized to full stack depth (immutable reference, too large to virtualize); every fragment is a window [0, spill_ptr) onto it, so nest/unnest allocates nothing.
    host.spill = [0] * SPILL_SIZE
    make_sure_not_resized(host.spill)
    host.spill_ptr = 0


class DSCacheSnapshot(object):
    """Immutable snapshot of the cache: scalar tops, cache_depth, a private copy
    of the frame and the spill pointer. The shared spill is not copied; restore
    rolls spill_ptr back and relies on the cells below it being undisturbed."""

    _immutable_fields_ = ["t0", "t1", "cache_depth", "frame[*]", "spill_ptr"]

    def __init__(self, t0, t1, cache_depth, frame, spill_ptr):
        self.t0 = t0
        self.t1 = t1
        self.cache_depth = cache_depth
        self.frame = frame
        self.spill_ptr = spill_ptr


def snapshot_cache(host):
    """Capture host's active-fragment state. Copies the fixed-size frame so later
    cache writes cannot disturb the snapshot."""
    frame_copy = [0] * FRAME_SIZE
    i = 0
    while i < FRAME_SIZE:
        frame_copy[i] = host.frame[i]
        i += 1
    make_sure_not_resized(frame_copy)
    return DSCacheSnapshot(host.t0, host.t1, host.cache_depth, frame_copy,
                           host.spill_ptr)


def restore_cache(host, snap):
    """Roll host's stack back to a snapshot, discarding everything pushed since.
    Restores the cache and the cache/spill pointers; the spill cells below the
    saved spill pointer are left in place."""
    host.t0 = snap.t0
    host.t1 = snap.t1
    host.cache_depth = snap.cache_depth
    i = 0
    while i < FRAME_SIZE:
        host.frame[i] = snap.frame[i]
        i += 1
    host.spill_ptr = snap.spill_ptr


class DSIntMetaStack(DSMetaStack):
    def init_fields(self):
        init_fields(self)

    def push_on(self, v):
        dd = self.cache_depth
        if dd >= ACTIVE_MAX:
            self._spill_bottom()
            dd = self.cache_depth
        if dd >= NTOP:
            si = dd - NTOP
            assert si >= 0
            self.frame[si] = self.t1
        self.t1 = self.t0
        self.t0 = v
        self.cache_depth = dd + 1

    def pop_on(self):
        dd = self.cache_depth
        if dd <= 0:
            return self._pop_from_spill()
        r = self.t0
        self.t0 = self.t1
        if dd > NTOP:
            si = dd - NTOP - 1
            assert si >= 0
            self.t1 = self.frame[si]
        self.cache_depth = dd - 1
        return r

    def _pop_from_spill(self):
        ap = self.spill_ptr - 1
        assert ap >= 0
        r = self.spill[ap]
        self.spill_ptr = ap
        return r

    @unroll_safe
    def _spill_bottom(self):
        ap = self.spill_ptr
        if ap >= SPILL_SIZE:
            raise DataStackOverflow()
        assert ap >= 0
        self.spill[ap] = self.frame[0]
        self.spill_ptr = ap + 1
        i = 0
        while i < FRAME_SIZE - 1:
            self.frame[i] = self.frame[i + 1]
            i += 1
        self.cache_depth = self.cache_depth - 1

    def peek_on(self, depth):
        depth = promote(depth)
        dd = self.cache_depth
        if depth < dd:
            if depth == 0:
                return self.t0
            if depth == 1:
                return self.t1
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
            if depth == 0:
                self.t0 = v
            elif depth == 1:
                self.t1 = v
            else:
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
        self.t0 = 0
        self.t1 = 0
        self.cache_depth = 0
        self.spill_ptr = 0

    @unroll_safe
    def push_fragment_on(self):
        dd = self.cache_depth
        if dd > NTOP:
            n = dd - NTOP
            ap = self.spill_ptr
            if ap + n > SPILL_SIZE:
                raise DataStackOverflow()
            assert ap >= 0
            i = 0
            while i < n:
                self.spill[ap + i] = self.frame[i]
                i += 1
            self.spill_ptr = ap + n
            self.cache_depth = NTOP

    # Public, test-facing wrappers.
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
