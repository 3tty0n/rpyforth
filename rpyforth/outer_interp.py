from rpyforth.objects import (
    W_StringObject, Word, CodeThread, W_IntObject, W_PtrObject, W_FloatObject, ZERO)
from rpyforth.primitives import install_primitives
from rpyforth.util import to_upper, split_whitespace

from rpython.rlib.rfile import create_stdio
from rpython.rlib.jit import elidable, unroll_safe, promote

INTERPRET = 0
COMPILE   = 1

# Control stack entry kinds
CTRL_IF   = 0
CTRL_ELSE = 1
CTRL_DO   = 2
CTRL_BEGIN = 3
CTRL_WHILE = 4

class CtrlEntry(object):
    """Control stack entry for compilation-time control structures.

    RPython-friendly class to avoid tuple unpacking and string comparisons.
    """
    def __init__(self, kind, index):
        self.kind = kind    # int: CTRL_IF, CTRL_ELSE, or CTRL_DO
        self.index = index  # int: position in scurrent_code for patching
        self.leave_addrs = []  # list of LEAVE positions to patch (for DO loops)

class OuterInterpreter(object):
    _immutable_fields_ = ['wBR', 'w0BR', 'wLIT', 'wEXIT', 'wDO', 'wLOOP', 'wLEAVE', 'wTYPE']

    def __init__(self, inner):
        self.inner = inner
        self.dict = {}         # dictionary is owned here (case-insensitive by uppercase keys)
        self.state = INTERPRET # state for compilation
        self.comment = False
        self.current_name = ''

        self.reset_code()

        self.ctrl = []         # control stack at compilation

        # install minimal core words into dictionary
        install_primitives(self)

        self.wBR = self.dict["BRANCH"]
        self.w0BR = self.dict["0BRANCH"]
        self.wLIT = self.dict["LIT"]
        self.wEXIT = self.dict["EXIT"]
        self.wDO = self.dict["(DO)"]
        self.wLOOP = self.dict["(LOOP)"]
        self.wLEAVE = self.dict["LEAVE"]
        self.wTYPE = self.dict["TYPE"]

    def reset_code(self):
        self.current_code = [None] * 128
        self.current_lits = [None] * 128
        self.cc_ptr = 0
        self.lit_ptr = 0

    def push_code(self, w):
        assert self.cc_ptr < len(self.current_code)
        self.current_code[self.cc_ptr] = w
        self.cc_ptr += 1

    def pop_code(self):
        assert self.cc_ptr > 0
        self.cc_ptr -= 1
        return self.current_code[self.cc_ptr]

    def push_lit(self, w):
        assert self.lit_ptr < len(self.current_lits)
        self.current_lits[self.lit_ptr] = w
        self.lit_ptr += 1

    def pop_lit(self):
        assert self.lit_ptr > 0
        self.lit_ptr -= 1
        return self.current_lits[self.lit_ptr]

    def define_prim(self, name, func):
        w = Word(name, prim=func, immediate=False, thread=None)
        self.dict[to_upper(name)] = w
        return w

    def define_colon(self, name, thread):
        w = Word(name, prim=None, immediate=False, thread=thread)
        self.dict[to_upper(name)] = w
        return w

    def _emit_word(self, w):
        self.push_code(w)
        self.push_lit(ZERO)

    def _emit_lit(self, w_n):
        self.push_code(self.wLIT)
        self.push_lit(w_n)

    @elidable
    def _is_number(self, s):
        length = len(s)
        if length == 0:
            return False
        start_idx = 0
        if s[0] == '-':
            start_idx = 1
            if length == 1:
                return False
        # Unroll the check for better performance
        for i in range(start_idx, length):
            ch = s[i]
            if ch < '0' or ch > '9':
                return False
        return True

    @elidable
    def _to_number(self, s):
        """Convert string to integer. Optimized for JIT."""
        sign = 1
        start_idx = 0
        length = len(s)
        if s[0] == '-':
            sign = -1
            start_idx = 1
        n = 0
        for i in range(start_idx, length):
            n = n * 10 + (ord(s[i]) - ord('0'))
        result = sign * n
        return W_IntObject(result)

    @elidable
    def _is_float(self, s):
        length = len(s)
        if length == 0:
            return False

        # Handle negative sign
        idx = 0
        if s[idx] == '-':
            idx += 1
            if idx >= length:
                return False

        # Must have at least one digit or decimal point
        has_digit = False
        has_dot = False
        has_e = False

        while idx < length:
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
                if idx + 1 < length and (s[idx + 1] == '+' or s[idx + 1] == '-'):
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
        self.push_code(w)
        self.push_lit(W_IntObject(target_index))

    def _patch_here(self, at_index):
        self.current_lits[at_index] = W_IntObject(self.cc_ptr)

    def _read_tok(self, toks, i):
        t = toks[i]
        return t, i+1

    def w_CR(self):
        stdin, stdout, stderr = create_stdio()
        stdout.write('\n')


    # main outer interpreter
    def interpret_line(self, line):
        toks = split_whitespace(line)
        toks_len = len(toks)
        i = 0
        while i < toks_len:
            t, i = self._read_tok(toks, i)

            if t == 'S"':
                sdouble_quote_str = []
                while i < toks_len:
                    t, i = self._read_tok(toks, i)
                    t_len = len(t)
                    if t_len > 0 and t[t_len - 1] == '"':
                        stop = t_len - 1
                        assert 0 <= stop < len(t)
                        t = t[:stop]
                        sdouble_quote_str.append(t)
                        break
                    sdouble_quote_str.append(t)
                parsed_str = ' '.join(sdouble_quote_str)
                size = len(parsed_str)
                c_addr = self.inner.alloc_buf(parsed_str, size)
                self.inner.push_ds(c_addr)
                self.inner.push_ds(W_IntObject(size))
                continue

            if t == '."':
                parts = []
                while i < toks_len:
                    token, i = self._read_tok(toks, i)
                    token_len = len(token)
                    if token_len > 0 and token[token_len - 1] == '"':
                        stop = token_len - 1
                        assert 0 <= stop <= len(token)
                        parts.append(token[:stop])
                        break
                    parts.append(token)
                parsed_str = ' '.join(parts)
                w_str = W_StringObject(parsed_str)
                if self.state == INTERPRET:
                    self.inner.print_str(w_str)
                else:
                    self._emit_lit(w_str)
                    self._emit_word(self.wTYPE)
                continue

            if t == "CHAR":
                s, i = self._read_tok(toks, i)
                self.inner.push_ds(W_IntObject(ord(s[0])))
                continue

            if t == "CR":
                self.w_CR()
                continue

            # handle ':' and ';' lexically (not as immediate words)
            if t == ':':
                if i >= toks_len:
                    print ": requires a name"
                    return
                self.state = COMPILE
                self.current_name, i = self._read_tok(toks, i)
                self.reset_code()
                continue

            if t == ';':
                if self.state != COMPILE:
                    print "; outside definition"
                    continue

                # append EXIT and install
                self._emit_word(self.wEXIT)
                # Create new lists with only the used portion (RPython needs proper list sizes)
                code = [self.current_code[idx] for idx in range(self.cc_ptr)]
                lits = [self.current_lits[idx] for idx in range(self.lit_ptr)]
                thread = CodeThread(code, lits)
                self.define_colon(self.current_name, thread)

                # reset
                self.state = INTERPRET
                self.current_name = ''
                self.reset_code()
                continue

            tkey = to_upper(t)

            # Handle control flow words
            if self.state == INTERPRET:
                if tkey == "IF":
                    cond = self.inner.pop_ds()
                    if cond.intval == 0:
                        depth = 1
                        while i < toks_len and depth > 0:
                            tok = to_upper(toks[i])
                            if tok == "IF":
                                depth += 1
                            elif tok == "ELSE" and depth == 1:
                                i += 1
                                break
                            elif tok == "THEN":
                                depth -= 1
                                if depth == 0:
                                    i += 1
                                    break
                            i += 1
                    continue

                if tkey == "ELSE":
                    depth = 1
                    while i < toks_len and depth > 0:
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
                   if i >= toks_len:
                       print "VARIABLE/FVARIABLE requires a name"
                       return
                   name, i = self._read_tok(toks, i)

                   addr = W_IntObject(self.inner.here)
                   self.inner.here += self.inner.cell_size_bytes

                   code = [self.wLIT, self.wEXIT]
                   lits = [addr, ZERO]
                   thread = CodeThread(code, lits)
                   self.define_colon(name, thread)
                   continue

                if tkey == "CONSTANT":
                    if i >= toks_len:
                        print "CONSTANT requires a name"
                        return
                    name, i = self._read_tok(toks, i)
                    val = self.inner.pop_ds()

                    code = [self.wLIT, self.wEXIT]
                    lits = [val, ZERO]
                    thread = CodeThread(code, lits)
                    self.define_colon(name, thread)
                    continue

                if tkey == "FCONSTANT":
                    if i >= toks_len:
                        print "FCONSTANT requires a name"
                        return
                    name, i = self._read_tok(toks, i)
                    val = self.inner.pop_ds()

                    code = [self.wLIT, self.wEXIT]
                    lits = [val, ZERO]
                    thread = CodeThread(code, lits)
                    self.define_colon(name, thread)
                    continue

            if self.state == COMPILE:
                if tkey == "IF":
                    orig = self.cc_ptr
                    self._emit_with_target(self.w0BR, 0)
                    self.ctrl.append(CtrlEntry(CTRL_IF, orig))
                    continue

                if tkey == "ELSE":
                    entry = self.ctrl.pop()
                    if entry.kind != CTRL_IF:
                        print "ELSE without IF"
                        return
                    self._patch_here(entry.index)
                    orig2 = self.cc_ptr
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
                    do_body_start = self.cc_ptr
                    self.ctrl.append(CtrlEntry(CTRL_DO, do_body_start))
                    continue

                if tkey == "LOOP":
                    entry = self.ctrl.pop()
                    if entry.kind != CTRL_DO:
                        print "LOOP without DO"
                        return
                    self._emit_with_target(self.wLOOP, entry.index)
                    loop_end = self.cc_ptr
                    for leave_addr in entry.leave_addrs:
                        self.current_lits[leave_addr] = W_IntObject(loop_end)
                    continue

                if tkey == "BEGIN":
                    begin_addr = self.cc_ptr
                    self.ctrl.append(CtrlEntry(CTRL_BEGIN, begin_addr))
                    continue

                if tkey == "WHILE":
                    entry = self.ctrl.pop()
                    if entry.kind != CTRL_BEGIN:
                        print "WHILE without BEGIN"
                        return
                    while_addr = self.cc_ptr
                    self._emit_with_target(self.w0BR, 0)
                    self.ctrl.append(CtrlEntry(CTRL_BEGIN, entry.index))
                    self.ctrl.append(CtrlEntry(CTRL_WHILE, while_addr))
                    continue

                if tkey == "REPEAT":
                    if len(self.ctrl) < 2:
                        print "REPEAT without BEGIN...WHILE"
                        return
                    while_entry = self.ctrl.pop()
                    begin_entry = self.ctrl.pop()
                    if while_entry.kind != CTRL_WHILE or begin_entry.kind != CTRL_BEGIN:
                        print "REPEAT without proper BEGIN...WHILE"
                        return
                    self._emit_with_target(self.wBR, begin_entry.index)
                    self._patch_here(while_entry.index)
                    continue

                if tkey == "[CHAR]":
                    if i >= toks_len:
                        print "[CHAR] requires a following character"
                        continue
                    char_tok = toks[i]
                    i += 1
                    char_tok_len = len(char_tok)
                    if char_tok_len > 0:
                        char_code = ord(char_tok[0])
                        self._emit_lit(W_IntObject(char_code))
                    else:
                        print "[CHAR] got empty token"
                    continue

            w = self.dict.get(tkey, None)
            w = promote(w)
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
