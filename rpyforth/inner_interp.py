from rpyforth.objects import Word, CodeThread, ZERO

class InnerInterpreter(object):
    def __init__(self):
        self._ds = [None] * 16 # data stack
        self.ds_ptr = 0

        self._rs = [None]* 16  # return stack
        self.rs_ptr = 0

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
