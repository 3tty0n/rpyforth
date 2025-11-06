from rpyforth.objects import (
    DECIMAL,
    Word,
    CodeThread,
    ZERO,
    W_IntObject,
    W_StringObject,
    W_PtrObject,
    CELL_SIZE_BYTES,
    CELL_SIZE,
)

from rpython.rlib.jit import JitDriver, promote, elidable, unroll_safe

STACK_SIZE = 256
BUF_SIZE = 1024
HEAP_CELL_COUNT = 65536
HEAP_SIZE_BYTES = HEAP_CELL_COUNT * CELL_SIZE_BYTES

def get_printable_location(ip, code, lits):
    return "ip=%d %s %s" % (ip, code[ip], lits[ip])

jitdriver = JitDriver(
    greens=['ip', 'code', 'lits'],
    reds=['self'],
    virtualizables=['self'],
    get_printable_location=get_printable_location
)

class InnerInterpreter(object):
    _immutable_fields_ = ["cell_size", "cell_size_bytes"]
    _virtualizable_ = ["ip", "ds_ptr", "rs_ptr", "_ds[*]", "_rs[*]", "cur"]


    def __init__(self):
        # Pre-allocate larger stacks to reduce growth overhead
        self._ds = [None] * STACK_SIZE # data stack
        self.ds_ptr = 0

        self._rs = [None] * STACK_SIZE  # return stack
        self.rs_ptr = 0

        self.mem = [0] * HEAP_SIZE_BYTES
        self.here = 0
        self.cell_size = CELL_SIZE
        self.cell_size_bytes = CELL_SIZE_BYTES

        self.buf = [None] * BUF_SIZE
        self.buf_ptr = 0

        self.base = DECIMAL
        self._pno_active = False      # inside <# ... #> or not
        self._pno_buf = []            # buffer for pno (pictured numeric output)

        self.ip = 0
        self.cur = None       # type: CodeThread

    def push_ds(self, w_x):
        ds_ptr = self.ds_ptr
        self._ds[ds_ptr] = w_x
        self.ds_ptr = ds_ptr + 1

    def pop_ds(self):
        ds_ptr = self.ds_ptr - 1
        assert ds_ptr >= 0
        w_x = self._ds[ds_ptr]
        self._ds[ds_ptr] = None
        self.ds_ptr = ds_ptr
        return w_x

    def top2_ds(self):
        w_y = self.pop_ds()
        w_x = self.pop_ds()
        return w_x, w_y

    def push_rs(self, w_x):
        rs_ptr = self.rs_ptr
        self._rs[rs_ptr] = w_x
        self.rs_ptr = rs_ptr + 1

    def pop_rs(self):
        rs_ptr = self.rs_ptr - 1
        assert rs_ptr >= 0
        w_x = self._rs[rs_ptr]
        self._rs[rs_ptr] = None
        self.rs_ptr = rs_ptr
        return w_x

    def print_int(self, x):
        assert isinstance(x, W_IntObject)
        print x.to_string()

    def print_str(self, s):
        assert isinstance(s, W_StringObject)
        print s.to_string()

    def alloc_buf(self, content, size):
        assert isinstance(content, str)
        for i in range(self.buf_ptr, self.buf_ptr + size):
            self.buf[i] = content[i]
        self.buf_ptr += size
        return W_PtrObject(self.buf_ptr)

    def _ensure_addr(self, addr, span):
        assert 0 <= addr < len(self.mem)
        assert addr + span <= len(self.mem)

    def cell_store(self, addr_obj, value_obj):
        assert isinstance(addr_obj, W_IntObject)
        assert isinstance(value_obj, W_IntObject)
        addr = addr_obj.intval
        self._ensure_addr(addr, self.cell_size_bytes)
        masked = value_obj.intval
        for offset in range(self.cell_size_bytes):
            self.mem[addr + offset] = masked & 0xFF
            masked >>= 8

    def cell_fetch(self, addr_obj):
        assert isinstance(addr_obj, W_IntObject)
        addr = addr_obj.intval
        self._ensure_addr(addr, self.cell_size_bytes)
        accum = 0
        for offset in range(self.cell_size_bytes):
            accum |= self.mem[addr + offset] << (8 * offset)
        top_byte = self.mem[addr + self.cell_size_bytes - 1]
        if top_byte & 0x80:
            sign_adjust = 1 << (self.cell_size_bytes * 8)
            accum -= sign_adjust
        return W_IntObject(accum)

    def execute_thread(self, thread):
        self.cur = thread
        self.ip = 0
        code = thread.code
        lits = thread.lits
        while self.ip < len(code):
            jitdriver.jit_merge_point(
                ip=self.ip,
                code=code,
                lits=lits,
                self=self
            )

            w = promote(code[self.ip])

            if w.prim is not None:
                w.prim(self)
            else:
                self.execute_thread(w.thread)
            self.ip += 1
        self.cur = None

    def execute_word_now(self, w):
        code = [w]
        lits = [ZERO]
        self.execute_thread(CodeThread(code, lits))

    def prim_LIT(self):
        lit = self.cur.lits[self.ip]
        self.push_ds(lit)

    def prim_EXIT(self):
        self.ip = len(self.cur.code)
