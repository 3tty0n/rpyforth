# forth_rpy.py  — Minimal Forth in RPython (int stack, :, ;, basic ops)
# Run as RPython: rpython --opt=jit forth_rpy.py
from rpython.rlib import jit, rfile

# ----- Small tagged instruction set -----
OP_PUSH = 1
OP_CALL = 2

class Instr(object):
    _immutable_fields_ = ["op", "arg"]
    def __init__(self, op, arg):
        self.op = op   # int: OP_PUSH / OP_CALL
        self.arg = arg # int (literal) or str (word name)

class Word(object):
    _immutable_fields_ = ["name", "is_primitive", "immediate"]
    def __init__(self, name, is_primitive, immediate):
        self.name = name
        self.is_primitive = is_primitive
        self.immediate = immediate
        self.prim_fn = None      # for primitive
        self.code = []           # list of Instr for compiled words


class W_AbstractObject(object):
    pass


class W_IntObject(W_AbstractObject):
    def __init__(self, intval):
        self.intval = intval

    def __repr__(self):
        return str(self.intval)

    def add(self, w_other):
        if isinstance(w_other, W_IntObject):
            self.intval += w_other.intval
            return self
        elif isinstance(w_other, W_FloatObject):
            self.intval += int(w_other.floatval)
            return self
        else:
            raise NotImplementedError("Unimplemented operation")

    def sub(self, w_other):
        if isinstance(w_other, W_IntObject):
            self.intval -= w_other.intval
            return self
        elif isinstance(w_other, W_FloatObject):
            self.intval -= int(w_other.floatval)
            return self
        else:
            raise NotImplementedError("Unimplemented operation")

    def mul(self, w_other):
        if isinstance(w_other, W_IntObject):
            self.intval = self.intval * w_other.intval
            return self
        elif isinstance(w_other, W_FloatObject):
            self.intval = self.intval * int(w_other.floatval)
            return self
        else:
            raise NotImplementedError("Unimplemented operation")

    def div(self, w_other):
        raise NotImplementedError("not implemented")

class W_FloatObject(W_AbstractObject):
    def __init__(self, floatval):
        self.floatval = floatval

    def __repr__(self):
        return str(self.floatval)

    def add(self, w_other):
        if isinstance(w_other, W_FloatObject):
            self.floatval += w_other.floatval
            return self
        elif isinstance(w_other, W_IntObject):
            self.floatval += float(w_other.intval)
            return self
        else:
            raise NotImplementedError("Unimplemented operation")

    def sub(self, w_other):
        if isinstance(w_other, W_FloatObject):
            self.floatval -= w_other.floatval
            return self
        elif isinstance(w_other, W_IntObject):
            self.floatval -= float(w_other.intval)
            return self
        else:
            raise NotImplementedError("Unimplemented operation")

    def mul(self, w_other):
        if isinstance(w_other, W_FloatObject):
            self.floatval = self.floatval * w_other.floatval
            return self
        elif isinstance(w_other, W_IntObject):
            self.floatval = self.floatval * float(w_other.intval)
            return self
        else:
            raise NotImplementedError("Unimplemented operation")

    def div(self, w_other):
        raise NotImplementedError("not implemented")


def make_w_object(primval):
    if isinstance(primval, int):
        return W_IntObject(primval)
    elif isinstance(primval, float):
        return W_FloatObject(primval)
    else:
        assert 0


class VM(object):
    def __init__(self):
        self.max_stack_size = 32
        self.stack = [None] * self.max_stack_size
        self.stackptr = 0

        self.rstack = []
        self.dict = {}
        self.state_compile = False
        self.current_def = None

        self._install_primitives()

    # -------- stack helpers --------
    def push(self, v):
        self.stack[self.stackptr] = v
        self.stackptr += 1

    def pop(self):
        if self.stackptr >= self.max_stack_size:
            raise RuntimeError("stack underflow")

        self.stackptr -= 1
        w_x = self.stack[self.stackptr]
        return w_x

    def peek(self):
        return self.stack[self.stackptr]

    # -------- dictionary helpers --------
    def add_primitive(self, name, fn):
        w = Word(name, True, False)
        w.prim_fn = fn
        self.dict[name] = w
    def add_compiled(self, w):
        self.dict[w.name] = w

    # -------- execution --------
    def execute_word(self, name):
        w = self.dict.get(name, None)
        if w is None:
            raise RuntimeError("undefined word: " + name)
        if w.is_primitive:
            w.prim_fn(self)
        else:
            self._run_code(w.code)

    @jit.unroll_safe
    def _run_code(self, code):
        pc = 0
        # bytecode-style loop
        while pc < len(code):
            inst = code[pc]
            pc += 1

            if inst.op == OP_PUSH:
                w_x = make_w_object(inst.arg)
                self.push(w_x)               # arg: int

            elif inst.op == OP_CALL:
                name = inst.arg                   # arg: str
                w = self.dict.get(name, None)     # TODO: improve dict access
                if w is None:
                    raise RuntimeError("undefined word: " + name)
                if w.is_primitive:
                    w.prim_fn(self)
                    pc += 1
                else:
                    # call: push caller frame and jump into callee code
                    self.rstack.append((code, pc + 1))
                    code = w.code
                    pc = 0
                    # return from callee
                    while pc >= len(code):
                        if not self.rstack:
                            # callee ended and no frame to return to
                            return
                        code, pc = self.rstack.pop()
            else:
                raise RuntimeError("bad opcode")

    # -------- compile helpers --------
    def emit_push(self, n):
        self.current_def.code.append(Instr(OP_PUSH, n))
    def emit_call(self, name):
        self.current_def.code.append(Instr(OP_CALL, name))

    # -------- primitives --------
    def _install_primitives(self):
        # stack ops
        self.add_primitive("dup", prim_dup)
        self.add_primitive("drop", prim_drop)
        self.add_primitive("swap", prim_swap)
        self.add_primitive("over", prim_over)
        # arithmetic
        self.add_primitive("+", prim_add)
        self.add_primitive("-", prim_sub)
        self.add_primitive("*", prim_mul)
        self.add_primitive("/", prim_div)
        # print top
        self.add_primitive(".", prim_dot)
        # colon and semicolon
        self.add_primitive(":", prim_colon)
        self.add_primitive(";", prim_semicolon)

# -------- primitive implementations --------
def prim_dup(vm):
    a = vm.peek()
    vm.push(a)

def prim_drop(vm):
    vm.pop()

def prim_swap(vm):
    a = vm.pop()
    b = vm.pop()
    vm.push(a)
    vm.push(b)

def prim_over(vm):
    if len(vm.stack) < 2:
        raise RuntimeError("stack underflow")
    vm.push(vm.stack[-2])

def prim_add(vm):
    b = vm.pop(); a = vm.pop()
    vm.push(a.add(b))

def prim_sub(vm):
    b = vm.pop(); a = vm.pop()
    vm.push(a.sub(b))

def prim_mul(vm):
    b = vm.pop(); a = vm.pop()
    vm.push(a.mul(b))

def prim_div(vm):
    b = vm.pop(); a = vm.pop()
    if b == 0:
        raise RuntimeError("division by zero")
    vm.push(a.div(b))

def prim_dot(vm):
    a = vm.pop()
    # stdout print (RPython-ok)
    print a

def prim_colon(vm):
    # next token becomes the word name (handled by front-end)
    # here: we just flip state; the front-end sets current_def
    vm.state_compile = True

def prim_semicolon(vm):
    # end current definition
    if not vm.state_compile or vm.current_def is None:
        raise RuntimeError("';' outside definition")
    vm.add_compiled(vm.current_def)
    vm.current_def = None
    vm.state_compile = False

# -------- front-end (tokenization & interpret/compile) --------
def is_number(tok):
    neg = tok.startswith('-') and len(tok) > 1
    digits = tok[1:] if neg else tok
    if not digits:
        return False
    for c in digits:
        if c < '0' or c > '9':
            return False
    return True

def process_tokens(vm, tokens):
    i = 0
    while i < len(tokens):
        t = tokens[i]
        if t == "":
            i += 1; continue

        if not vm.state_compile:
            # INTERPRET state
            if is_number(t):
                vm.push(W_IntObject(t))
            elif t == ":":
                # switch to compile; next token must be name
                vm.execute_word(":")     # set state
                i += 1
                if i >= len(tokens):
                    raise RuntimeError("expected word name after ':'")
                name = tokens[i]
                w = Word(name, False, False)
                vm.current_def = w
            else:
                vm.execute_word(t)
        else:
            # COMPILE state
            if t == ";":
                vm.execute_word(";")
            elif is_number(t):
                vm.emit_push(W_IntObject(t))
            else:
                vm.emit_call(t)
        i += 1

def repl(vm):
    # very simple REPL
    stdin, stdout, stderr = rfile.create_stdio()

    try:
        while True:
            stdout.write("OK> ")
            # naive tokenizer: split by whitespace
            line = stdin.readline()
            toks = line.strip().split()
            process_tokens(vm, toks)
    except EOFError:
        return
