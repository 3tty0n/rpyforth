from rpyforth.objects import Word, CodeThread, W_IntObject, ZERO, BINARY, OCTAL, DECIMAL, HEX

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

            if self.state == INTERPRET and tkey == "VARIABLE":
               if i >= len(toks):
                   self.inner.print_str("VARIABLE requires a name")
                   return
               name = toks[i]
               i += 1

               addr = W_IntObject(self.inner.here)
               self.inner.here += 1

               def prim_PUSH_ADDR(inner):
                   inner.push_ds(addr)
               self.define_prim(name, prim_PUSH_ADDR)
               continue

            if self.state == INTERPRET and tkey == "CONSTANT":
                if i >= len(toks):
                    self.inner.print_str("CONSTANT requires a name")
                    return
                name = toks[i]
                i += 1
                val = self.inner.pop_ds()
                def prim_PUSH_VAL(inner):
                    inner.push_ds(val)
                self.define_prim(name, prim_PUSH_VAL)
                continue

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

# memory management

def prim_STORE(inner):
    addr_obj = inner.pop_ds()
    val_obj  = inner.pop_ds()
    idx = addr_obj.intval
    inner.mem[idx] = val_obj

def prim_FETCH(inner):
    addr_obj = inner.pop_ds()
    idx = addr_obj.intval
    inner.push_ds(inner.mem[idx])

# BASE

def prim_BASE_FETCH(inner): #  BASE@ ( -- u )
    inner.push_(inner.base)


def prim_BASE_STORE(inner):  # BASE! ( u -- )
    u = inner.pop_ds()
    inner.base = u

def prim_DECIMAL(inner): inner.base = DECIMAL
def prim_HEX(inner):     inner.base = HEX
def prim_OCTAL(inner):   inner.base = OCTAL
def prim_BINARY(inner):  inner.base = BINARY

from rpyforth.objects import W_IntObject, W_StringObject

def prim_LESSNUM(inner):         # <#   ( -- )
    """
    Begins Pictured Numeric Output (PNO) conversion. PNO is a method of
    formatting numbers for display on the screen, using # symbols to represent
    digits. PNO actually converts the number to be displayed to a string,
    allowing the opportunity to insert characters into the character stream as
    conversion progresses (for example, commas, to separate hundreds and
    thousands, etc.). See # and #> for more information.
    """
    inner._pno_active = True
    inner._pno_buf = []


def prim_NUMSIGN(inner):         # #    ( x -- q )
    """
    The word # takes a single digit from the unsigned-double number on the
    stack (it divides the number by the current number base, as determined by
    BASE) and places this digit into the PNO buffer for display later.
    """
    if not inner._pno_active:
        inner.print_str("# outside <# #>")
        return
    x = inner.pop_ds()
    base = inner.base.intval
    q = x.intval // base
    r = x.intval %  base
    inner._pno_buf.insert(0, _digit_to_char(r))
    inner.push_ds(W_IntObject(q))

def prim_NUMSIGN_S(inner):       # #S   ( x -- 0 )
    """
    The word #S converts all remaining digits until the value on the stack is
    0 (i.e. there are no more digits to convert).
    """
    if not inner._pno_active:
        inner.print_str("#S outside <# #>")
        return
    while True:
        x = inner.pop_ds()
        base = inner.base.intval
        q = x.intval // base
        r = x.intval %  base
        inner._pno_buf.insert(0, _digit_to_char(r))
        inner.push_ds(W_IntObject(q))
        if q == 0:
            break

def prim_HOLD(inner):
    """
    HOLD inserts the ASCII value on the stack directly into the PNO buffer at
    the current PNO buffer position. Normally the word ASCII or CHAR is used to
    obtain the ASCII value of the character immediately following it.
    """
    if not inner._pno_active:
        inner.print_str("HOLD outside <# #>")
        return
    ch = inner.pop_ds()
    inner._pno_buf.insert(0, chr(ch.intval))

def prim_NUMGREATER(inner):      # #>   ( x -- string )
    """
    #> ends pictured numeric output. The double value that has/was been
    converted is removed from the stack and is replaced with the address and
    length of the string that has been built in the PNO buffer. This
    address/length pair is suitable for use with TYPE for displaying the
    string in the PNO buffer.
    """
    if not inner._pno_active:
        inner.print_str("#> outside <# #>")
        return
    _ = inner.pop_ds()
    s = ''.join(inner._pno_buf)
    inner._pno_active = False
    inner.push_ds(W_StringObject(s))

def prim_TYPE(inner):            # TYPE ( string -- )
    s = inner.pop_ds()
    inner.print_str(s)

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
    # memory management
    outer.define_prim("!",    prim_STORE)
    outer.define_prim("@",    prim_FETCH)
    # BASE
    outer.define_prim("BASE@",   prim_BASE_FETCH)
    outer.define_prim("BASE!",   prim_BASE_STORE)
    outer.define_prim("DECIMAL", prim_DECIMAL)
    outer.define_prim("HEX",     prim_HEX)
    outer.define_prim("OCTAL",   prim_OCTAL)
    outer.define_prim("BINARY",  prim_BINARY)

    outer.define_prim("<#",  prim_LESSNUM)
    outer.define_prim("#",   prim_NUMSIGN)
    outer.define_prim("#S",  prim_NUMSIGN_S)
    outer.define_prim("#>",  prim_NUMGREATER)
    outer.define_prim("HOLD",prim_HOLD)

    outer.define_prim("TYPE", prim_TYPE)   # for testing output

    # thread ops
    outer.define_prim("LIT",  prim_LIT)
    outer.define_prim("EXIT", prim_EXIT)


def _digit_to_char(d):
    if d < 10:
        return chr(ord('0') + d)
    else:
        return chr(ord('A') + (d - 10))
