from rpyforth.objects import (
    DECIMAL,
    Word,
    CodeThread,
    ZERO,
    W_IntObject,
    CELL_SIZE_BYTES,
    CELL_SIZE,
)

HEAP_CELL_COUNT = 65536
HEAP_SIZE_BYTES = HEAP_CELL_COUNT * CELL_SIZE_BYTES

class InnerInterpreter(object):

    def __init__(self):
        self._ds = [None] * 16 # data stack
        self.ds_ptr = 0

        self._rs = [None] * 16  # return stack
        self.rs_ptr = 0

        self.mem = [0] * HEAP_SIZE_BYTES
        self.here = 0
        self.cell_size = CELL_SIZE
        self.cell_size_bytes = CELL_SIZE_BYTES

        self.base = DECIMAL
        self._pno_active = False      # inside <# ... #> or not
        self._pno_buf = []            # buffoer for pno (pictured numeric output)

        self.ip = 0
        self.cur = None       # type: CodeThread

    def push_ds(self, w_x):
        self._ds[self.ds_ptr] = w_x
        self.ds_ptr += 1

    def pop_ds(self):
        self.ds_ptr -= 1
        assert self.ds_ptr >= 0
        w_x = self._ds[self.ds_ptr]
        return w_x

    def top2_ds(self):
        w_y = self.pop_ds()
        w_x = self.pop_ds()
        return w_x, w_y

    def push_rs(self, w_x):
        self._rs[self.rs_ptr] = w_x
        self.rs_ptr += 1

    def pop_rs(self):
        self.rs_ptr -= 1
        assert self.rs_ptr >= 0
        w_x = self._rs[self.rs_ptr]
        return w_x

    def print_int(self, x):
        print(x)

    def print_str(self, s):
        print(s)

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
        while self.ip < len(code):
            w = code[self.ip]
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
