from rpyforth.objects import (
    DECIMAL,
    ForthException,
    Word,
    CodeThread,
    DeferredCodeThread,
    ZERO,
    W_Object,
    W_IntObject,
    W_StringObject,
    W_PtrObject,
    CELL_SIZE_BYTES,
    CELL_SIZE,
    make_int,
    THREAD_REGISTRY,
)


from rpython.rlib.jit import JitDriver, promote, elidable, unroll_safe, promote_string, hint
from rpython.rlib.debug import make_sure_not_resized
from rpython.rlib.rfile import create_stdio

from rpyforth.heap import (HEAP_CELL_COUNT, HEAP_SIZE_BYTES, DICT_SIZE_BYTES,
                           ALLOC_BASE, Heap, _alloc_region_bytes)


def alloc_region_bytes():
    return _alloc_region_bytes()

from rpyforth.config import USE_VIRTUALIZATION

from rpyforth.metastack import (
    FRAME_SIZE,
    ACTIVE_MAX,
    EFFECTIVE_NTOP,
    USE_STACK_FRAGMENT,
    USE_FLOAT_FRAGMENT,
    USE_FRAME_ONLY,
    USE_NTOP_VARIANT,
    STACK_FRAGMENT_VIRTUALIZABLES,
    push_ds_fragments,
    reset_ds_fragments,
)
from rpyforth.vm_constants import (
    STACK_SIZE,
    BUF_SIZE,
    CATCH_DEPTH,
    DEFAULT_BASE,
    CONTROL_IP_BITS,
    CONTROL_IP_MASK,
    EXIT_SENTINEL,
    TAILCALL_SENTINEL,
    CALL_SENTINEL,
    DEFER_TAILCALL_SENTINEL,
)

# CATCH word copies the int cache only in the stack-fragment builds.
if not USE_STACK_FRAGMENT:
    CA_FRAME_WIDTH = 0
elif USE_FRAME_ONLY:
    CA_FRAME_WIDTH = ACTIVE_MAX
elif USE_NTOP_VARIANT:
    CA_FRAME_WIDTH = EFFECTIVE_NTOP + FRAME_SIZE
else:
    CA_FRAME_WIDTH = FRAME_SIZE

# A catch frame is one logical record.  Store its integer metadata in one flat
# stack instead of maintaining a parallel allocation for every field.  The
# layout is specialized at translation time, so disabled stack layouts consume
# no catch slots for state they cannot use.
CA_TID = 0
CA_IP = 1
CA_DSF = 2
CA_DSL = 3
CA_RS = 4
CA_LI = 5
CA_LC = 6
CA_CONTROL = 7
_ca_next = 8
if USE_STACK_FRAGMENT:
    CA_CACHE_DEPTH = _ca_next
    CA_SPILL = _ca_next + 1
    _ca_next += 2
    if not USE_FRAME_ONLY and not USE_NTOP_VARIANT:
        CA_T0 = _ca_next
        CA_T1 = _ca_next + 1
        _ca_next += 2
else:
    CA_DSI = _ca_next
    _ca_next += 1
if USE_FLOAT_FRAGMENT:
    CA_FDEP = _ca_next
    CA_FSPILL = _ca_next + 1
    _ca_next += 2
CA_INT_WIDTH = _ca_next
CA_FLOAT_WIDTH = 2 if USE_FLOAT_FRAGMENT else 0

if USE_STACK_FRAGMENT:
    if USE_FRAME_ONLY:
        from rpyforth.metastack_int_frameonly import DSIntMetaStackFrameOnly
        InterpBase = DSIntMetaStackFrameOnly
    elif USE_NTOP_VARIANT:
        from rpyforth.metastack_int_ntop import DSIntMetaStackN
        InterpBase = DSIntMetaStackN
    elif USE_FLOAT_FRAGMENT:
        from rpyforth.metastack_float import DSFloatMetaStack
        InterpBase = DSFloatMetaStack
    else:
        from rpyforth.metastack_int import DSIntMetaStack
        InterpBase = DSIntMetaStack
else:
    class InterpBase(object):
        pass

class Exit(Exception):
    pass

class Bye(Exception):
    """Raised by BYE to exit the Forth system cleanly."""
    pass

class Abort(Exception):
    """Raised by ABORT" at runtime: unwinds every active portal (the stacks are
    already cleared by the raiser); the outer interpreter resumes."""
    pass


# Portal-boundary sentinel: pushed on entry, popping it ends the dispatch loop; the promoted return thread id constant-folds the halt test away in traces.
HALT_THREAD = CodeThread([], [])


def get_printable_location(ip, thread):
    # Must be total for every (ip, thread) the JIT logs (ip==len(code), None slots at merge points): translated builds drop bounds checks, so a bad index/None deref here segfaults under PYPYLOG.
    if ip < 0 or ip >= len(thread.code):
        return "ip=%d <thread-end>" % ip
    w = thread.code[ip]
    lit = thread.lits[ip]
    wname = w.to_string() if w is not None else "<none>"
    lname = lit.to_string() if lit is not None else "<none>"
    return "ip=%d %s %s" % (ip, wname, lname)

if USE_STACK_FRAGMENT:
    jitdriver = JitDriver(
        greens=['ip', 'thread'],
        reds=['self'],
        virtualizables=['self'],
        get_printable_location=get_printable_location,
    )
elif USE_VIRTUALIZATION:
    jitdriver = JitDriver(
        greens=['ip', 'thread'],
        reds=['self'],
        virtualizables=['self'],
        get_printable_location=get_printable_location,
    )
else:
    jitdriver = JitDriver(
        greens=['ip', 'thread'],
        reds=['self'],
        get_printable_location=get_printable_location,
    )


class InnerInterpreter(InterpBase, object):
    # Spill is allocated once and never reassigned (immutable reference), so the JIT hoists the array-pointer load to the loop header instead of reloading it every spill/refill.
    _immutable_fields_ = ["cell_size", "cell_size_bytes", "base", "spill",
                          "lc_is", "lc_ls", "rs", "ds_locals", "heap",
                          "ca_ints", "ca_floats", "ca_frames", "ca_fframes",
                          "pending_box"]

    if USE_VIRTUALIZATION:
        _virtualizable_ = ["ds_ints", "ds_floats",
                           "ds_ptr_ints", "ds_ptr_floats", "ds_ptr_locals",
                           "rs_ptr", "cs_pcs", "cs_ptr", "cs_base",
                           "li",
                           "cell_size", "cell_size_bytes", "base"]
    elif USE_STACK_FRAGMENT:
        _virtualizable_ = STACK_FRAGMENT_VIRTUALIZABLES
    else:
        _virtualizable_ = []


    def __init__(self):
        self.outer = None

        # Pre-allocate larger stacks to reduce growth overhead
        self.ds_ints = [0] * STACK_SIZE  # unboxed integer data stack (fixed path)
        self.ds_ptr_ints = 0

        self.ds_floats = [0.0] * STACK_SIZE
        self.ds_ptr_floats = 0

        self.ds_locals = [None] * STACK_SIZE
        self.ds_ptr_locals = 0

        self.rs = [0] * STACK_SIZE
        self.rs_ptr = 0

        self.li = 0
        self.ll = 0
        self.lc_depth = 0
        # Dedicated loop-control stacks.  Keep index and limit separate: the
        # JIT can optimize their independent access better than an interleaved
        # array with a multiply in every nested I/J/LOOP lookup.
        self.lc_is = [0] * STACK_SIZE
        self.lc_ls = [0] * STACK_SIZE

        # Runtime control stack
        # control address packed as (tid << CONTROL_IP_BITS | ip)
        # only the tid is promoted so THREAD_REGISTRY folds and the target inlines, while the runtime ip avoids a guard_value/bridge per return site.
        # Keep the compact cs_* storage names: changing virtualizable field
        # descriptors perturbs trace layout even though the abstraction exposed
        # by this interpreter is the runtime control stack.
        self.cs_pcs = [0] * STACK_SIZE
        self.cs_ptr = 0
        # Lower bound for execute_thread: stop popping entries at this depth; execute_word_now raises it so a nested run (CATCH) returns instead of escaping into outer control state.
        self.cs_base = 0

        # Total heap = dictionary region + a runtime-sized ALLOCATE region.
        self.heap_size = DICT_SIZE_BYTES + alloc_region_bytes()
        self.heap = Heap(self.heap_size)
        self.cell_size = CELL_SIZE
        self.cell_size_bytes = CELL_SIZE_BYTES

        # Boxed-string side table, sized to the dictionary region only: boxed strings park at HERE-allocated addresses, never in the high ALLOCATE region.
        self.buf = [None] * (DICT_SIZE_BYTES >> 3)
        self.here = 0
        # Bump pointer for ALLOCATE in the high region, and the highest address it may reach.
        self.alloc_ptr = ALLOC_BASE
        self.alloc_limit = self.heap_size
        # Free-list reuse for ALLOCATE/FREE, bucketed by usable size: gc.fs re-ALLOCATEs same-size blocks every collection, so without reuse the bump pointer grows without bound.
        self.alloc_free = {}

        self.base = DEFAULT_BASE
        self._pno_active = False
        self._pno_buf = []
        self.argv = []

        # Catch-frame stack.  Metadata is stored as flat records; cache-frame
        # arrays have zero length when their stack layout is disabled.
        self.ca_ints = [0] * (CATCH_DEPTH * CA_INT_WIDTH)
        self.ca_floats = [0.0] * (CATCH_DEPTH * CA_FLOAT_WIDTH)
        self.ca_frames = [0] * (CATCH_DEPTH * CA_FRAME_WIDTH)
        float_frame_cells = CATCH_DEPTH * FRAME_SIZE if USE_FLOAT_FRAGMENT else 0
        self.ca_fframes = [0.0] * float_frame_cells
        self.catch_ptr = 0

        self.pending_box = [None]

        if USE_STACK_FRAGMENT:
            self.init_fields()
            if USE_FLOAT_FRAGMENT:
                self.init_float_fields()
        else:
            # Placeholders for a consistent attribute set; unused on the fixed path (translator folds the dead USE_STACK_FRAGMENT branch).
            self.t0 = 0
            self.t1 = 0
            self.cache_depth = 0
            self.frame = [0]
            self.spill = [0]
            self.spill_ptr = 0

    def push_control(self, thread, ip):
        """Push a packed runtime control address."""
        ptr = self.cs_ptr
        assert ptr < len(self.cs_pcs)
        assert 0 <= ip < (1 << CONTROL_IP_BITS)
        self.cs_pcs[ptr] = (thread.tid << CONTROL_IP_BITS) | ip
        self.cs_ptr = ptr + 1

    def pop_control(self):
        """Pop a packed runtime control address."""
        ptr = self.cs_ptr - 1
        assert ptr >= 0
        self.cs_ptr = ptr
        pc = self.cs_pcs[ptr]
        # Clear the slot so the JIT treats the tail above cs_ptr as dead and elides the reads on recursive traces.
        self.cs_pcs[ptr] = 0
        # Promote only the thread id (registry folds, target inlines) but leave the runtime ip, so a polymorphic caller avoids a guard_value/bridge per return site; skipping the promote for megamorphic callees measured 2x slower on cd16sim.
        tid = promote(pc >> CONTROL_IP_BITS)
        ip = pc & CONTROL_IP_MASK
        thread = THREAD_REGISTRY.threads[tid]
        return thread, ip

    def is_control_stack_empty(self):
        return self.cs_ptr == 0

    def reset_after_abort(self):
        """Clear every machine stack. Runs at the Abort catch site, outside
        any portal, so compiled frames never see half-cleared state."""
        self.reset_ds_int()
        self.reset_ds_float()
        self.ds_ptr_locals = 0
        self.rs_ptr = 0
        self.lc_depth = 0
        self.cs_ptr = 0
        self.catch_ptr = 0

    @unroll_safe
    def catch_push_frame(self, thread, ip):
        """Record a CATCH resume point: return address plus all the state a
        THROW must restore. Flat array writes, no allocation."""
        cp = self.catch_ptr
        assert 0 <= cp < CATCH_DEPTH
        base = cp * CA_INT_WIDTH
        self.ca_ints[base + CA_TID] = thread.tid
        self.ca_ints[base + CA_IP] = ip
        self.ca_ints[base + CA_DSF] = self.depth_ds_float()
        self.ca_ints[base + CA_DSL] = self.ds_ptr_locals
        self.ca_ints[base + CA_RS] = self.rs_ptr
        self.ca_ints[base + CA_LI] = self.li
        self.ca_ints[base + CA_LC] = self.lc_depth
        self.ca_ints[base + CA_CONTROL] = self.cs_ptr
        if USE_STACK_FRAGMENT:
            if not USE_FRAME_ONLY and not USE_NTOP_VARIANT:
                self.ca_ints[base + CA_T0] = self.t0
                self.ca_ints[base + CA_T1] = self.t1
            self.ca_ints[base + CA_CACHE_DEPTH] = self.cache_depth
            self.ca_ints[base + CA_SPILL] = self.spill_ptr
            fbase = cp * CA_FRAME_WIDTH
            if USE_NTOP_VARIANT:
                k = 0
                while k < EFFECTIVE_NTOP:
                    self.ca_frames[fbase + k] = self._get_scalar(k)
                    k += 1
                i = 0
                while i < FRAME_SIZE:
                    self.ca_frames[fbase + EFFECTIVE_NTOP + i] = self.frame[i]
                    i += 1
            else:
                i = 0
                while i < CA_FRAME_WIDTH:
                    self.ca_frames[fbase + i] = self.frame[i]
                    i += 1
        else:
            self.ca_ints[base + CA_DSI] = self.ds_ptr_ints
        if USE_FLOAT_FRAGMENT:
            fmeta = cp * CA_FLOAT_WIDTH
            self.ca_floats[fmeta] = self.ft0
            self.ca_floats[fmeta + 1] = self.ft1
            self.ca_ints[base + CA_FDEP] = self.fdep
            self.ca_ints[base + CA_FSPILL] = self.fspill_ptr
            ffbase = cp * FRAME_SIZE
            i = 0
            while i < FRAME_SIZE:
                self.ca_fframes[ffbase + i] = self.fframe[i]
                i += 1
        self.catch_ptr = cp + 1

    def catch_drop_frame(self):
        """Discard the top catch frame (protected word returned normally)."""
        cp = self.catch_ptr - 1
        assert cp >= 0
        self.catch_ptr = cp

    @unroll_safe
    def throw_unwind(self, code):
        """Unwind to the nearest CATCH frame of this portal: restore its saved
        state, push the throw code, and return the resume (thread, ip). If the
        top frame belongs to an outer portal (below control_base), re-raise so the
        exception propagates out of this execute_thread invocation."""
        cp = self.catch_ptr - 1
        base = cp * CA_INT_WIDTH
        if cp < 0 or self.ca_ints[base + CA_CONTROL] < self.cs_base:
            raise ForthException(code)
        assert cp >= 0
        self.catch_ptr = cp
        if not USE_FLOAT_FRAGMENT:
            self.ds_ptr_floats = self.ca_ints[base + CA_DSF]
        self.ds_ptr_locals = self.ca_ints[base + CA_DSL]
        self.rs_ptr = self.ca_ints[base + CA_RS]
        self.li = self.ca_ints[base + CA_LI]
        self.lc_depth = self.ca_ints[base + CA_LC]
        self.cs_ptr = self.ca_ints[base + CA_CONTROL]
        if USE_STACK_FRAGMENT:
            if not USE_FRAME_ONLY and not USE_NTOP_VARIANT:
                self.t0 = self.ca_ints[base + CA_T0]
                self.t1 = self.ca_ints[base + CA_T1]
            self.cache_depth = self.ca_ints[base + CA_CACHE_DEPTH]
            self.spill_ptr = self.ca_ints[base + CA_SPILL]
            fbase = cp * CA_FRAME_WIDTH
            if USE_NTOP_VARIANT:
                k = 0
                while k < EFFECTIVE_NTOP:
                    self._set_scalar(k, self.ca_frames[fbase + k])
                    k += 1
                i = 0
                while i < FRAME_SIZE:
                    self.frame[i] = self.ca_frames[fbase + EFFECTIVE_NTOP + i]
                    i += 1
            else:
                i = 0
                while i < CA_FRAME_WIDTH:
                    self.frame[i] = self.ca_frames[fbase + i]
                    i += 1
        else:
            self.ds_ptr_ints = self.ca_ints[base + CA_DSI]
        if USE_FLOAT_FRAGMENT:
            fmeta = cp * CA_FLOAT_WIDTH
            self.ft0 = self.ca_floats[fmeta]
            self.ft1 = self.ca_floats[fmeta + 1]
            self.fdep = self.ca_ints[base + CA_FDEP]
            self.fspill_ptr = self.ca_ints[base + CA_FSPILL]
            ffbase = cp * FRAME_SIZE
            i = 0
            while i < FRAME_SIZE:
                self.fframe[i] = self.ca_fframes[ffbase + i]
                i += 1
        self.push_ds_int(code)
        thread = THREAD_REGISTRY.threads[self.ca_ints[base + CA_TID]]
        ip = self.ca_ints[base + CA_IP]
        return thread, ip

    def push_loop(self, limit, counter):
        d = self.lc_depth
        assert d < len(self.lc_is)
        if d > 0:
            self.lc_is[d - 1] = self.li
            self.lc_ls[d - 1] = self.ll
        self.li = counter
        self.ll = limit
        self.lc_depth = d + 1

    def pop_loop(self):
        d = self.lc_depth - 1
        assert d >= 0
        limit = self.ll
        counter = self.li
        if d > 0:
            self.li = self.lc_is[d - 1]
            self.ll = self.lc_ls[d - 1]
        self.lc_depth = d
        return limit, counter

    @unroll_safe
    def peek_loop_counter(self, depth=0):
        """Get current loop counter without popping (raw int)."""
        depth = promote(depth)
        if depth == 0:
            return self.li
        return self.lc_is[self.lc_depth - 1 - depth]

    @unroll_safe
    def peek_loop_limit(self, depth=0):
        """Get current loop limit without popping (raw int)."""
        depth = promote(depth)
        if depth == 0:
            return self.ll
        return self.lc_ls[self.lc_depth - 1 - depth]

    @unroll_safe
    def set_loop_counter(self, depth, value):
        """Set loop counter in place (raw int)."""
        depth = promote(depth)
        if depth == 0:
            self.li = value
        else:
            self.lc_is[self.lc_depth - 1 - depth] = value

    def push_ds(self, w_x):
        assert isinstance(w_x, W_Object)
        ds_ptr = self.ds_ptr_locals
        self.ds_locals[ds_ptr] = w_x
        self.ds_ptr_locals = ds_ptr + 1

    def pop_ds(self):
        ds_ptr = self.ds_ptr_locals - 1
        assert ds_ptr >= 0
        w_x = self.ds_locals[ds_ptr]
        assert isinstance(w_x, W_Object)
        self.ds_locals[ds_ptr] = None
        self.ds_ptr_locals = ds_ptr
        return w_x

    def push_ds_int(self, intval):
        if USE_STACK_FRAGMENT:
            self.push_on(intval)
        else:
            self.push_ds_int_fixed(intval)

    def push_ds_int_fixed(self, intval):
        ds_ptr = self.ds_ptr_ints
        self.ds_ints[ds_ptr] = intval
        self.ds_ptr_ints = ds_ptr + 1

    def push_ds_float(self, floatval):
        if USE_FLOAT_FRAGMENT:
            self.fpush_on(floatval)
        else:
            self.push_ds_float_fixed(floatval)

    def push_ds_float_fixed(self, floatval):
        ds_ptr = self.ds_ptr_floats
        self.ds_floats[ds_ptr] = floatval
        self.ds_ptr_floats = ds_ptr + 1

    def pop_ds_int(self):
        if USE_STACK_FRAGMENT:
            return self.pop_on()
        return self.pop_ds_int_fixed()

    def pop_ds_int_fixed(self):
        ds_ptr = self.ds_ptr_ints - 1
        assert ds_ptr >= 0
        intval = self.ds_ints[ds_ptr]
        self.ds_ptr_ints = ds_ptr
        return intval

    def pop_ds_float(self):
        if USE_FLOAT_FRAGMENT:
            return self.fpop_on()
        return self.pop_ds_float_fixed()

    def pop_ds_float_fixed(self):
        ds_ptr = self.ds_ptr_floats - 1
        assert ds_ptr >= 0
        floatval = self.ds_floats[ds_ptr]
        self.ds_ptr_floats = ds_ptr
        return floatval

    def peek_ds_int(self, depth=0):
        if USE_STACK_FRAGMENT:
            return self.peek_on(depth)
        return self.peek_ds_int_fixed(depth)

    def peek_ds_int_fixed(self, depth=0):
        ptr = self.ds_ptr_ints - 1 - depth
        assert ptr >= 0
        return self.ds_ints[ptr]

    def peek_ds_float(self, depth=0):
        if USE_FLOAT_FRAGMENT:
            return self.fpeek_on(depth)
        return self.peek_ds_float_fixed(depth)

    def peek_ds_float_fixed(self, depth=0):
        ptr = self.ds_ptr_floats - 1 - depth
        assert ptr >= 0
        return self.ds_floats[ptr]

    def poke_ds_float(self, depth, floatval):
        if USE_FLOAT_FRAGMENT:
            self.fpoke_on(depth, floatval)
        else:
            ptr = self.ds_ptr_floats - 1 - depth
            assert ptr >= 0
            self.ds_floats[ptr] = floatval

    def depth_ds_float(self):
        if USE_FLOAT_FRAGMENT:
            return self.fdepth_on()
        return self.ds_ptr_floats

    def reset_ds_float(self):
        if USE_FLOAT_FRAGMENT:
            self.freset_on()
        else:
            self.ds_ptr_floats = 0

    def peek_ds(self, depth=0):
        ptr = self.ds_ptr_locals - 1 - depth
        assert ptr >= 0
        return self.ds_locals[ptr]

    def poke_ds_int(self, depth, intval):
        if USE_STACK_FRAGMENT:
            self.poke_on(depth, intval)
        else:
            self.poke_ds_int_fixed(depth, intval)

    def poke_ds_int_fixed(self, depth, intval):
        ptr = self.ds_ptr_ints - 1 - depth
        assert ptr >= 0
        self.ds_ints[ptr] = intval

    def depth_ds_int(self):
        if USE_STACK_FRAGMENT:
            return self.depth_on()
        return self.ds_ptr_ints

    def reset_ds_int(self):
        if USE_STACK_FRAGMENT:
            reset_ds_fragments(self)
        else:
            self.ds_ptr_ints = 0

    def ds_int_size(self):
        return self.depth_ds_int()

    def clear_ds_int(self):
        self.reset_ds_int()

    def top2_ds(self):
        w_y = self.pop_ds()
        w_x = self.pop_ds()
        return w_x, w_y

    def top2_ds_int(self):
        y = self.pop_ds_int()
        x = self.pop_ds_int()
        return x, y

    def peek_rs(self, depth=0):
        ptr = self.rs_ptr - 1 - depth
        assert ptr >= 0
        return self.rs[ptr]

    def poke_rs(self, depth, w_x):
        ptr = self.rs_ptr - 1 - depth
        assert ptr >= 0
        self.rs[ptr] = w_x

    def push_rs(self, w_x):
        rs_ptr = self.rs_ptr
        self.rs[rs_ptr] = w_x
        self.rs_ptr = rs_ptr + 1

    def pop_rs(self):
        rs_ptr = self.rs_ptr - 1
        assert rs_ptr >= 0
        w_x = self.rs[rs_ptr]
        self.rs_ptr = rs_ptr
        return w_x

    def print_int(self, x):
        assert isinstance(x, W_IntObject)
        _, stdout, _ = create_stdio()
        stdout.write(x.to_string())
        stdout.flush()

    def print_str(self, s):
        assert isinstance(s, W_StringObject)
        _, stdout, _ = create_stdio()
        stdout.write(s.to_string())
        stdout.flush()

    def buf_get(self, addr):
        """Boxed string parked at a cell-aligned data-space address, or None."""
        i = addr >> 3
        if 0 <= i < len(self.buf):
            return self.buf[i]
        return None

    def buf_set(self, addr, w_str):
        i = addr >> 3
        assert 0 <= i < len(self.buf)
        self.buf[i] = w_str

    @unroll_safe
    def alloc_buf(self, content, size):
        # String lives as both a boxed object (buf_get) and real bytes in data space so char-level words see the same characters; here is aligned first so each owns a distinct buf slot.
        addr = (self.here + CELL_SIZE_BYTES - 1) & ~(CELL_SIZE_BYTES - 1)
        assert size >= 0
        self.buf_set(addr, W_StringObject(content[:size]))
        i = 0
        while i < size:
            self.char_store(addr + i, ord(content[i]))
            i += 1
        self.here = (addr + size + CELL_SIZE_BYTES) & ~(CELL_SIZE_BYTES - 1)
        return addr

    def _ensure_addr(self, addr, span):
        assert 0 <= addr < self.heap_size
        assert addr + span <= self.heap_size

    def cell_store(self, addr, intval):
        assert isinstance(addr, int)
        self.heap.cell_store(addr, intval)

    def cell_fetch_int(self, addr):
        return self.heap.cell_fetch_int(addr)

    def cell_fetch(self, addr):
        return self.heap.cell_fetch(addr)

    def cell_2store(self, addr, x1_int, x2_int):
        """Standard 2!: x2 (the top cell) lands at addr, x1 at the next."""
        assert 0 <= addr < self.heap_size - self.cell_size_bytes
        self.cell_store(addr, x2_int)
        self.cell_store(addr + self.cell_size_bytes, x1_int)

    def cell_2fetch(self, addr):
        """Standard 2@: returns (x1, x2) with x2 (the top) taken from addr."""
        assert 0 <= addr < self.heap_size - self.cell_size_bytes
        x1 = make_int(self.cell_fetch_int(addr + self.cell_size_bytes))
        x2 = make_int(self.cell_fetch_int(addr))
        return x1, x2

    def char_store(self, addr, intval):
        self.heap.char_store(addr, intval)

    def char_fetch(self, addr):
        return self.heap.char_fetch(addr)

    def set_base(self, n):
        """Set the conversion radix and mirror it into the BASE cell so `BASE @`
        reads the current value. Keeps HEX/DECIMAL/etc. in step with the cell that
        Forth code manipulates through @ / !."""
        self.base = n
        if self.outer is not None:
            self.cell_store(self.outer.base_addr, n)

    def float_store(self, addr, value):
        self.heap.float_store(addr, value)

    def cell_float_fetch(self, addr):
        return self.heap.float_fetch_float(addr)

    def float_fetch(self, addr):
        return self.heap.float_fetch(addr)

    def execute_thread(self, thread, ip=0):
        self.push_control(HALT_THREAD, 0)
        if USE_STACK_FRAGMENT:
            push_ds_fragments(self)
        while True:
            jitdriver.jit_merge_point(ip=ip, thread=thread, self=self)
            if ip >= len(thread.code):
                thread, ip = self.pop_control()
                if thread is HALT_THREAD:
                    break
                continue

            w = promote(thread.code[ip])
            if w is None:
                thread, ip = self.pop_control()
                if thread is HALT_THREAD:
                    break
                continue
            ip += 1

            prim = promote(w.prim)
            if prim is not None:
                try:
                    ip = prim(self, thread, ip)
                except ForthException as e:
                    thread, ip = self.throw_unwind(e.code)
                    continue
                if ip == EXIT_SENTINEL:
                    thread, ip = self.pop_control()
                    if thread is HALT_THREAD:
                        break
                    continue
                if ip == CALL_SENTINEL:
                    target = promote(self.pending_box[0])
                    self.pending_box[0] = None
                    thread = target.thread
                    ip = 0
                    continue
                if ip == TAILCALL_SENTINEL:
                    from rpyforth.objects import W_WordObject
                    tailcall_idx = len(thread.code) - 1
                    target = promote(thread.lits[tailcall_idx])
                    if isinstance(target, W_WordObject):
                        target_word = promote(target.word)
                        nested_thread = promote(target_word.thread)
                        if nested_thread is not None:
                            thread = nested_thread
                            ip = 0
                            jitdriver.can_enter_jit(ip=ip, thread=thread, self=self)
                            continue
                    thread, ip = self.pop_control()
                    if thread is HALT_THREAD:
                        break
                    continue
                if ip == DEFER_TAILCALL_SENTINEL:
                    assert isinstance(thread, DeferredCodeThread)
                    target = promote(thread.deferred_word)
                    assert target is not None
                    thread = target.thread
                    ip = 0
                    continue
            else:
                nested_thread = promote(w.thread)
                self.push_control(thread, ip)
                if USE_STACK_FRAGMENT:
                    push_ds_fragments(self)
                recursing = nested_thread is thread
                thread = nested_thread
                ip = 0
                if recursing:
                    jitdriver.can_enter_jit(ip=ip, thread=thread, self=self)

    def execute_word_now(self, w):
        nt = w.now_thread
        if nt is None:
            nt = CodeThread([w], [ZERO])
            w.now_thread = nt
        saved_base = self.cs_base
        self.cs_base = self.cs_ptr
        try:
            self.execute_thread(nt, 0)
        finally:
            self.cs_base = saved_base
