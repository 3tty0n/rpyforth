from rpyforth.objects import Word, CodeThread

class Stack(object):
    def __init__(self):
        self._a = [] # list[int]

    def push(self, x):
        self._a.append(x)

    def pop(self):
        return self._a.pop()

    def top2(self):
        b = self._a.pop(); a = self._a.pop(); return a, b

    def __len__(self):
        return len(self._a)

class InnerInterpreter(object):
    def __init__(self):
        self.ds = Stack()     # data stack
        self.rs = Stack()     # return stack (reserved for future control words)
        self.ip = 0
        self.cur = None       # type: CodeThread

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
        lits = [0]
        self.execute_thread(CodeThread(code, lits))

    def prim_LIT(self):
        lit = self.cur.lits[self.ip]
        self.ds.push(lit)

    def prim_EXIT(self):
        self.ip = len(self.cur.code)
