from rpyforth.objects import Word, CodeThread, W_IntObject, ZERO

INTERPRET = 0
COMPILE   = 1

def to_upper(s):
    out = ''
    for i in range(len(s)):
        o = ord(s[i])
        if o >= 97 and o <= 122: # a-z
            out += chr(o - 32)
        else:
            out += s[i]
    return out

def split_whitespace(line):
    res = []
    cur = ''
    for i in range(len(line)):
        ch = line[i]
        if ch == ' ' or ch == '\n' or ch == '\t' or ch == '\r' or \
           ch == '\v' or ch == '\f':
            if cur != '':
                res.append(cur)
                cur = ''
            continue
        if ch == ':' or ch == ';':
            if cur != '':
                res.append(cur)
                cur = ''
            res.append(ch)
            continue
        cur += ch
    if cur != '':
        res.append(cur)
    return res

class OuterInterpreter(object):
    def __init__(self, inner):
        self.inner = inner
        # dictionary is owned here (case-insensitive by uppercase keys)
        self.dict = {}
        # state for compilation
        self.state = INTERPRET
        self.current_name = ''
        self.current_code = []  # list[Word]
        self.current_lits = []  # list[W_Object]

        # install minimal core words into dictionary
        install_primitives(self)

    def define_prim(self, name, func):
        w = Word(name, prim=func, immediate=False, thread=None)
        self.dict[to_upper(name)] = w
        return w

    def define_colon(self, name, thread):
        w = Word(name, prim=None, immediate=False, thread=thread)
        self.dict[to_upper(name)] = w
        return w

    def _emit_word(self, w):
        self.current_code.append(w)
        self.current_lits.append(ZERO)

    def _emit_lit(self, w_n):
        self.current_code.append(self.dict["LIT"])  # Word for LIT
        self.current_lits.append(w_n)

    def _is_number(self, s):
        if len(s) == 0:
            return False
        neg = s[0] == '-'
        if neg:
            s = s[1:]
            if len(s) == 0:
                return False
        for i in range(len(s)):
            ch = s[i]
            if ch < '0' or ch > '9':
                return False
        return True

    def _to_number(self, s):
        sign = 1
        if s.startswith('-'):
            sign = -1
            s = s[1:]
        n = 0
        for i in range(len(s)):
            n = n * 10 + (ord(s[i]) - ord('0'))
        result = sign * n
        return W_IntObject(result)

    # main outer interpreter
    def interpret_line(self, line):
        toks = split_whitespace(line)
        i = 0
        while i < len(toks):
            t = toks[i]
            i += 1
            # handle ':' and ';' lexically (not as immediate words)
            if t == ':':
                if i >= len(toks):
                    self.inner.print_str(": requires a name")
                    return
                self.state = COMPILE
                self.current_name = toks[i]
                i += 1
                self.current_code = []
                self.current_lits = []
                continue

            if t == ';':
                if self.state != COMPILE:
                    self.inner.print_str("; outside definition")
                    continue

                # append EXIT and install
                self._emit_word(self.dict["EXIT"])
                thread = CodeThread(self.current_code, self.current_lits)
                self.define_colon(self.current_name, thread)

                # reset
                self.state = INTERPRET
                self.current_name = ''
                self.current_code = []
                self.current_lits = []
                continue

            tkey = to_upper(t)
            w = self.dict.get(tkey, None)
            if self.state == INTERPRET:
                if w is not None:
                    self.inner.execute_word_now(w)
                elif self._is_number(t):
                    self.inner.push_ds(self._to_number(t))
                else:
                    self.inner.print_str("UNKNOWN: " + t)
            elif self.state == COMPILE:
                if w is not None:
                    self._emit_word(w)
                elif self._is_number(t):
                    self._emit_lit(self._to_number(t))
                else:
                    self.inner.print_str("UNKNOWN: " + t)
            else:
                assert 0, "unreachable state"

def prim_DUP(inner):
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(a)

def prim_DROP(inner):
    inner.pop_ds()

def prim_SWAP(inner):
    a,b = inner.top2_ds()
    inner.push_ds(b)
    inner.push_ds(a)

def prim_OVER(inner):
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(b)
    inner.push_ds(a)

# Arithmetic

def prim_ADD(inner):
    a,b = inner.top2_ds()
    inner.push_ds(a.add(b))

def prim_SUB(inner):
    a,b = inner.top2_ds()
    inner.push_ds(a.sub(b))

def prim_MUL(inner):
    a,b = inner.top2_ds()
    inner.push_ds(a.mul(b))

# I/O

def prim_DOT(inner):
    x = inner.pop_ds()
    inner.print_int(x)

# CodeThread-aware primitives

def prim_LIT(inner):
    inner.prim_LIT()

def prim_EXIT(inner):
    inner.prim_EXIT()

def install_primitives(outer):
    # stack manipulation
    outer.define_prim("DUP",  prim_DUP)
    outer.define_prim("DROP", prim_DROP)
    outer.define_prim("SWAP", prim_SWAP)
    outer.define_prim("OVER", prim_OVER)
    # arithmetic
    outer.define_prim("+",    prim_ADD)
    outer.define_prim("-",    prim_SUB)
    outer.define_prim("*",    prim_MUL)
    # I/O
    outer.define_prim(".",    prim_DOT)
    # thread ops
    outer.define_prim("LIT",  prim_LIT)
    outer.define_prim("EXIT", prim_EXIT)
