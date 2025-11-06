from rpyforth.objects import (
    W_StringObject, Word, CodeThread, W_IntObject, W_PtrObject, W_FloatObject, ZERO)
from rpyforth.primitives import install_primitives
from rpyforth.util import to_upper, split_whitespace

INTERPRET = 0
COMPILE   = 1

# Control stack entry kinds
CTRL_IF   = 0
CTRL_ELSE = 1

class CtrlEntry(object):
    """Control stack entry for compilation-time control structures.

    RPython-friendly class to avoid tuple unpacking and string comparisons.
    """
    def __init__(self, kind, index):
        self.kind = kind    # int: CTRL_IF or CTRL_ELSE
        self.index = index  # int: position in current_code for patching

class OuterInterpreter(object):
    def __init__(self, inner):
        self.inner = inner
        self.dict = {}         # dictionary is owned here (case-insensitive by uppercase keys)
        self.state = INTERPRET # state for compilation
        self.comment = False
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

    def _is_float(self, s):
        """Check if string is a floating point literal like 0., 1.0, 2.0E0, -3.14E0"""
        if len(s) == 0:
            return False

        # Handle negative sign
        idx = 0
        if s[idx] == '-':
            idx += 1
            if idx >= len(s):
                return False

        # Must have at least one digit or decimal point
        has_digit = False
        has_dot = False
        has_e = False

        while idx < len(s):
            ch = s[idx]
            if ch == '.':
                if has_dot or has_e:
                    return False
                has_dot = True
            elif ch == 'E' or ch == 'e':
                if has_e or not has_digit:
                    return False
                has_e = True
                # Check for optional sign after E
                if idx + 1 < len(s) and (s[idx + 1] == '+' or s[idx + 1] == '-'):
                    idx += 1
            elif '0' <= ch <= '9':
                has_digit = True
            else:
                return False
            idx += 1

        # Must have at least a dot or E to be a float
        return has_digit and (has_dot or has_e)

    def _to_float(self, s):
        """Convert string to float"""
        # Python's float() handles the format we need
        return W_FloatObject(float(s))

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

            if t == 'S"':
                sdouble_quote_str = []
                while i < len(toks):
                    t = toks[i]
                    i += 1
                    if t[-1] == '"':
                        t = t[:-1]
                        sdouble_quote_str.append(t)
                        break
                    sdouble_quote_str.append(t)
                parsed_str = ' '.join(sdouble_quote_str)
                size = len(parsed_str)
                c_addr = self.inner.alloc_buf(parsed_str, size)
                assert c_addr is not None
                self.inner.push_ds(c_addr)
                self.inner.push_ds(W_IntObject(size))
                continue

            # handle ':' and ';' lexically (not as immediate words)
            if t == ':':
                if i >= len(toks):
                    print ": requires a name"
                    return
                self.state = COMPILE
                self.current_name = toks[i]
                i += 1
                self.current_code = []
                self.current_lits = []
                continue

            if t == ';':
                if self.state != COMPILE:
                    print "; outside definition"
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

            # Handle control flow words
            if self.state == INTERPRET:
                if tkey == "IF":
                    # Pop condition from stack
                    cond = self.inner.pop_ds()
                    if cond.intval == 0:
                        # Condition is false, skip to ELSE or THEN
                        depth = 1
                        while i < len(toks) and depth > 0:
                            tok = to_upper(toks[i])
                            if tok == "IF":
                                depth += 1
                            elif tok == "ELSE" and depth == 1:
                                # Found matching ELSE, skip past it and continue
                                i += 1
                                break
                            elif tok == "THEN":
                                depth -= 1
                                if depth == 0:
                                    # Found matching THEN, skip past it
                                    i += 1
                                    break
                            i += 1
                    # If condition is true, just continue normally
                    continue

                if tkey == "ELSE":
                    # When we hit ELSE in interpret mode after IF was true,
                    # skip to matching THEN
                    depth = 1
                    while i < len(toks) and depth > 0:
                        tok = to_upper(toks[i])
                        if tok == "IF":
                            depth += 1
                        elif tok == "THEN":
                            depth -= 1
                            if depth == 0:
                                i += 1
                                break
                        i += 1
                    continue

                if tkey == "THEN":
                    continue

            if self.state == INTERPRET:
                if tkey == "VARIABLE" or tkey == "FVARIABLE":
                   if i >= len(toks):
                       print "VARIABLE/FVARIABLE requires a name"
                       return
                   name = toks[i]
                   i += 1

                   addr = W_IntObject(self.inner.here)
                   self.inner.here += self.inner.cell_size_bytes

                   code = [self.wLIT, self.wEXIT]
                   lits = [addr, ZERO]
                   thread = CodeThread(code, lits)
                   self.define_colon(name, thread)
                   continue

                if tkey == "CONSTANT":
                    if i >= len(toks):
                        print "CONSTANT requires a name"
                        return
                    name = toks[i]
                    i += 1
                    val = self.inner.pop_ds()

                    code = [self.wLIT, self.wEXIT]
                    lits = [val, ZERO]
                    thread = CodeThread(code, lits)
                    self.define_colon(name, thread)
                    continue

                if tkey == "FCONSTANT":
                    if i >= len(toks):
                        print "FCONSTANT requires a name"
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
                    self.ctrl.append(CtrlEntry(CTRL_IF, orig))
                    continue

                if tkey == "ELSE":
                    entry = self.ctrl.pop()
                    if entry.kind != CTRL_IF:
                        print "ELSE without IF"
                        return
                    self._patch_here(entry.index)
                    orig2 = len(self.current_code)
                    self._emit_with_target(self.wBR, 0)

                    self.ctrl.append(CtrlEntry(CTRL_ELSE, orig2))

                    self._patch_here(entry.index)
                    continue

                if tkey == "THEN":
                    entry = self.ctrl.pop()
                    if entry.kind != CTRL_IF and entry.kind != CTRL_ELSE:
                        print "THEN without IF/ELSE"
                        return
                    self._patch_here(entry.index)
                    continue

                if tkey == "DO":
                    self._emit_word(self.wDO)
                    do_body_start = len(self.current_code)
                    self.ctrl.append(CtrlEntry(CTRL_DO, do_body_start))
                    continue

                if tkey == "LOOP":
                    entry = self.ctrl.pop()
                    if entry.kind != CTRL_DO:
                        print "LOOP without DO"
                        return
                    self._emit_with_target(self.wLOOP, entry.index)
                    loop_end = len(self.current_code)
                    for leave_addr in entry.leave_addrs:
                        self.current_lits[leave_addr] = W_IntObject(loop_end)
                    continue

                if tkey == "[CHAR]":
                    if i >= len(toks):
                        print "[CHAR] requires a following character"
                        continue
                    char_tok = toks[i]
                    i += 1
                    if len(char_tok) > 0:
                        char_code = ord(char_tok[0])
                        self._emit_lit(W_IntObject(char_code))
                    else:
                        print "[CHAR] got empty token"
                    continue

            w = self.dict.get(tkey, None)
            if self.state == INTERPRET:
                if w is not None:
                    self.inner.execute_word_now(w)
                elif self._is_float(t):
                    self.inner.push_ds(self._to_float(t))
                elif self._is_number(t):
                    self.inner.push_ds(self._to_number(t))
                else:
                    print "UNKNOWN: " + t
            elif self.state == COMPILE:
                if w is not None:
                    self._emit_word(w)
                elif self._is_float(t):
                    self._emit_lit(self._to_float(t))
                elif self._is_number(t):
                    self._emit_lit(self._to_number(t))
                else:
                    print "UNKNOWN: " + t
            else:
                assert 0, "unreachable state"
