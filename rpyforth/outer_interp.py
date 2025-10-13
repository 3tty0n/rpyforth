from rpyforth.objects import W_StringObject, Word, CodeThread, W_IntObject, ZERO
from rpyforth.primitives import install_primitives
from rpyforth.util import to_upper, split_whitespace

INTERPRET = 0
COMPILE   = 1

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
                    self.inner.print_str(W_StringObject(": requires a name"))
                    return
                self.state = COMPILE
                self.current_name = toks[i]
                i += 1
                self.current_code = []
                self.current_lits = []
                continue

            if t == ';':
                if self.state != COMPILE:
                    self.inner.print_str(W_StringObject("; outside definition"))
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
                   self.inner.print_str(W_StringObject("VARIABLE requires a name"))
                   return
               name = toks[i]
               i += 1

               addr = W_IntObject(self.inner.here)
               self.inner.here += 1

               wLIT  = self.dict["LIT"]
               wEXIT = self.dict["EXIT"]
               code = [wLIT, wEXIT]
               lits = [addr, ZERO]
               thread = CodeThread(code, lits)
               self.define_colon(name, thread)
               continue

            if self.state == INTERPRET and tkey == "CONSTANT":
                if i >= len(toks):
                    self.inner.print_str(W_StringObject("CONSTANT requires a name"))
                    return
                name = toks[i]
                i += 1
                val = self.inner.pop_ds()

                wLIT  = self.dict["LIT"]
                wEXIT = self.dict["EXIT"]
                code = [wLIT, wEXIT]
                lits = [val, ZERO]
                thread = CodeThread(code, lits)
                self.define_colon(name, thread)
                continue

            w = self.dict.get(tkey, None)
            if self.state == INTERPRET:
                if w is not None:
                    self.inner.execute_word_now(w)
                elif self._is_number(t):
                    self.inner.push_ds(self._to_number(t))
                else:
                    self.inner.print_str(W_StringObject("UNKNOWN: " + t))
            elif self.state == COMPILE:
                if w is not None:
                    self._emit_word(w)
                elif self._is_number(t):
                    self._emit_lit(self._to_number(t))
                else:
                    self.inner.print_str(W_StringObject("UNKNOWN: " + t))
            else:
                assert 0, "unreachable state"
