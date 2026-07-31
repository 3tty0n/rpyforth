from rpython.rlib.jit import promote, unroll_safe
from rpython.rlib.debug import make_sure_not_resized

from rpyforth.metastack import (
    NTOP,
    FRAME_SIZE,
    ACTIVE_MAX,
    SPILL_SIZE,
    DataStackOverflow,
)
from rpyforth.metastack_int import DSIntMetaStack


def init_float_fields(host):
    """Float counterpart of init_fields: NTOP scalar tops ft0/ft1, the
    virtualizable fframe array, and the shared fspill. fdep counts cells cached in
    ft0/ft1 + fframe (0..ACTIVE_MAX)."""
    host.ft0 = 0.0
    host.ft1 = 0.0
    host.fdep = 0
    host.fframe = [0.0] * FRAME_SIZE
    make_sure_not_resized(host.fframe)

    host.fspill = [0.0] * SPILL_SIZE
    make_sure_not_resized(host.fspill)
    host.fspill_ptr = 0


class DSFloatCacheSnapshot(object):
    """Immutable capture of the active float-fragment cache. Same discipline as
    DSCacheSnapshot: private copy of fframe, spill buffer not copied."""

    _immutable_fields_ = ["ft0", "ft1", "fdep", "fframe[*]", "fspill_ptr"]

    def __init__(self, ft0, ft1, fdep, fframe, fspill_ptr):
        self.ft0 = ft0
        self.ft1 = ft1
        self.fdep = fdep
        self.fframe = fframe
        self.fspill_ptr = fspill_ptr


def snapshot_float_cache(host):
    frame_copy = [0.0] * FRAME_SIZE
    i = 0
    while i < FRAME_SIZE:
        frame_copy[i] = host.fframe[i]
        i += 1
    make_sure_not_resized(frame_copy)
    return DSFloatCacheSnapshot(host.ft0, host.ft1, host.fdep, frame_copy,
                                host.fspill_ptr)


def restore_float_cache(host, snap):
    host.ft0 = snap.ft0
    host.ft1 = snap.ft1
    host.fdep = snap.fdep
    i = 0
    while i < FRAME_SIZE:
        host.fframe[i] = snap.fframe[i]
        i += 1
    host.fspill_ptr = snap.fspill_ptr


class DSFloatMetaStack(DSIntMetaStack):
    """Float data stack with the same three-tier layout as the int stack. Inherits
    DSIntMetaStack so InnerInterpreter keeps one InterpBase chain; float state
    lives in separate f-prefixed fields, so the two stacks never alias."""

    def __init__(self):
        self.init_fields()
        self.init_float_fields()

    def init_float_fields(self):
        init_float_fields(self)

    # Hot path: NTOP scalar tops (ft0, ft1) keep FDUP/F+/FSWAP in registers.
    def fpush_on(self, v):
        dd = self.fdep
        if dd >= ACTIVE_MAX:
            self._fspill_bottom()
            dd = self.fdep
        if dd >= NTOP:
            si = dd - NTOP
            assert si >= 0
            self.fframe[si] = self.ft1
        self.ft1 = self.ft0
        self.ft0 = v
        self.fdep = dd + 1

    def fpop_on(self):
        dd = self.fdep
        if dd <= 0:
            return self._fpop_from_spill()
        r = self.ft0
        self.ft0 = self.ft1
        if dd > NTOP:
            si = dd - NTOP - 1
            assert si >= 0
            self.ft1 = self.fframe[si]
        self.fdep = dd - 1
        return r

    def _fpop_from_spill(self):
        ap = self.fspill_ptr - 1
        assert ap >= 0
        r = self.fspill[ap]
        self.fspill_ptr = ap
        return r

    @unroll_safe
    def _fspill_bottom(self):
        ap = self.fspill_ptr
        if ap >= SPILL_SIZE:
            raise DataStackOverflow()
        assert ap >= 0
        self.fspill[ap] = self.fframe[0]
        self.fspill_ptr = ap + 1
        i = 0
        while i < FRAME_SIZE - 1:
            self.fframe[i] = self.fframe[i + 1]
            i += 1
        self.fdep = self.fdep - 1

    def fpeek_on(self, depth):
        depth = promote(depth)
        dd = self.fdep
        if depth < dd:
            if depth == 0:
                return self.ft0
            if depth == 1:
                return self.ft1
            si = dd - 1 - depth
            assert si >= 0
            return self.fframe[si]
        ai = self.fspill_ptr - 1 - (depth - dd)
        assert ai >= 0
        return self.fspill[ai]

    def fpoke_on(self, depth, v):
        depth = promote(depth)
        dd = self.fdep
        if depth < dd:
            if depth == 0:
                self.ft0 = v
            elif depth == 1:
                self.ft1 = v
            else:
                si = dd - 1 - depth
                assert si >= 0
                self.fframe[si] = v
            return
        ai = self.fspill_ptr - 1 - (depth - dd)
        assert ai >= 0
        self.fspill[ai] = v

    def fdepth_on(self):
        return self.fdep + self.fspill_ptr

    def freset_on(self):
        self.ft0 = 0.0
        self.ft1 = 0.0
        self.fdep = 0
        self.fspill_ptr = 0

    # Call entry / return: float cells nest at the same points as int cells.
    @unroll_safe
    def push_float_fragment_on(self):
        dd = self.fdep
        if dd > NTOP:
            n = dd - NTOP
            ap = self.fspill_ptr
            if ap + n > SPILL_SIZE:
                raise DataStackOverflow()
            assert ap >= 0
            i = 0
            while i < n:
                self.fspill[ap + i] = self.fframe[i]
                i += 1
            self.fspill_ptr = ap + n
            self.fdep = NTOP

    def fpush(self, v):
        self.fpush_on(v)

    def fpop(self):
        return self.fpop_on()

    def fpeek(self, depth):
        return self.fpeek_on(depth)

    def fpoke(self, depth, v):
        self.fpoke_on(depth, v)

    def fsize(self):
        return self.fdepth_on()

    def fclear(self):
        self.freset_on()

    def push_float_fragment(self):
        self.push_float_fragment_on()


    def fsnapshot(self):
        return snapshot_float_cache(self)

    def frestore(self, snap):
        restore_float_cache(self, snap)
