from rpyforth.objects import W_StringObject, Word, CodeThread, W_IntObject, ZERO
from rpyforth.primitives import install_primitives
from rpyforth.util import to_upper, split_whitespace

INTERPRET = 0
COMPILE   = 1

class OuterInterpreter(object):
    def __init__(self, inner):
        self.inner = inner
        self.dict = {}         # dictionary is owned here (case-insensitive by uppercase keys)
        self.state = INTERPRET # state for compilation
        self.current_name = ''
        self.current_code = []
        self.current_lits = []

        self.ctrl = []         # control stack at compilation

        # install minimal core words into dictionary
        install_primitives(self)

        self.wBR = self.dict["BRANCH"]
        self.w0BR = self.dict["0BRANCH"]
        self.wLIT = self.dict["LIT"]
        self.wEXIT = self.dict["EXIT"]

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
        self.current_code.append(self.wLIT)  # Word for LIT
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

    def _emit_with_target(self, w, target_index):
        self.current_code.append(w)
        self.current_lits.append(W_IntObject(target_index))

    def _patch_here(self, at_index):
        self.current_lits[at_index] = W_IntObject(len(self.current_code))

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
                self._emit_word(self.wEXIT)
                thread = CodeThread(self.current_code, self.current_lits)
                self.define_colon(self.current_name, thread)

                # reset
                self.state = INTERPRET
                self.current_name = ''
                self.current_code = []
                self.current_lits = []
                continue

            tkey = to_upper(t)

            if self.state == INTERPRET:
                if tkey == "VARIABLE":
                   if i >= len(toks):
                       self.inner.print_str(W_StringObject("VARIABLE requires a name"))
                       return
                   name = toks[i]
                   i += 1

                   addr = W_IntObject(self.inner.here)
                   self.inner.here += 1

                   code = [self.wLIT, self.wEXIT]
                   lits = [addr, ZERO]
                   thread = CodeThread(code, lits)
                   self.define_colon(name, thread)
                   continue

                if tkey == "CONSTANT":
                    if i >= len(toks):
                        self.inner.print_str(W_StringObject("CONSTANT requires a name"))
                        return
                    name = toks[i]
                    i += 1
                    val = self.inner.pop_ds()

                    code = [self.wLIT, self.wEXIT]
                    lits = [val, ZERO]
                    thread = CodeThread(code, lits)
                    self.define_colon(name, thread)
                    continue

            if self.state == COMPILE:
                if tkey == "IF":
                    orig = len(self.current_code)
                    self._emit_with_target(self.w0BR, 0)
                    self.ctrl.append(("IF", orig))
                    continue

                if tkey == "ELSE":
                    kind, orig1 = self.ctrl.pop()
                    if kind != "IF":
                        self.inner.print_str(W_StringObject("ELSE without IF"))
                        return
                    self._patch_here(orig1)
                    orig2 = len(self.current_code)
                    self._emit_with_target(self.wBR, 0)

                    self.ctrl.append(("ELSE", orig2))

                    self._patch_here(orig1)
                    continue

                if tkey == "THEN":
                    kind, at = self.ctrl.pop()
                    if kind not in ("IF", "ELSE"):
                        self.inner.print_str(W_StringObject("THEN without IF/ELSE"))
                        return
                    self._patch_here(at)
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
