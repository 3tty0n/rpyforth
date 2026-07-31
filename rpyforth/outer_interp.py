from rpyforth.objects import (
    W_StringObject, Word, CodeThread, DeferredCodeThread, W_IntObject,
    W_PtrObject, W_FloatObject, W_WordObject, ZERO, TRUE,
    word_from_wid, ForthException, THREAD_REGISTRY)
from rpyforth.inner_interp import Abort
from rpyforth.primitives import install_primitives
from rpyforth.util import to_upper, split_whitespace, split_whitespace_stateful, _tokenize as _tokenize_raw

from rpython.rlib.rfile import create_stdio
from rpython.rlib.streamio import open_file_as_stream
from rpython.rlib.jit import elidable, unroll_safe, promote

INTERPRET = 0
COMPILE   = 1

# Max callee body length (excluding trailing EXIT) spliced inline at a call site.
MAX_INLINE_BODY = 32

# Control stack entry kinds
CTRL_IF   = 0
CTRL_ELSE = 1
CTRL_DO   = 2
CTRL_BEGIN = 3
CTRL_WHILE = 4
CTRL_CASE = 5
CTRL_OF   = 6

# POSTPONE/[COMPILE] replay codes for control-flow words not in the dictionary; (CF) re-runs the compile action.
CF_IF      = 0
CF_ELSE    = 1
CF_THEN    = 2
CF_BEGIN   = 3
CF_WHILE   = 4
CF_REPEAT  = 5
CF_AGAIN   = 6
CF_UNTIL   = 7
CF_DO      = 8
CF_QDO     = 9
CF_LOOP    = 10
CF_PLUSLOOP = 11
CF_LEAVE   = 12
CF_CASE    = 13
CF_OF      = 14
CF_ENDOF   = 15
CF_ENDCASE = 16

# Map a control-flow token (uppercased) to its CF_* replay code, or -1.
def cf_code_for(name_upper):
    if name_upper == "IF":       return CF_IF
    if name_upper == "ELSE":     return CF_ELSE
    if name_upper == "THEN":     return CF_THEN
    if name_upper == "BEGIN":    return CF_BEGIN
    if name_upper == "WHILE":    return CF_WHILE
    if name_upper == "REPEAT":   return CF_REPEAT
    if name_upper == "AGAIN":    return CF_AGAIN
    if name_upper == "UNTIL":    return CF_UNTIL
    if name_upper == "DO":       return CF_DO
    if name_upper == "?DO":      return CF_QDO
    if name_upper == "LOOP":     return CF_LOOP
    if name_upper == "+LOOP":    return CF_PLUSLOOP
    if name_upper == "LEAVE":    return CF_LEAVE
    if name_upper == "CASE":     return CF_CASE
    if name_upper == "OF":       return CF_OF
    if name_upper == "ENDOF":    return CF_ENDOF
    if name_upper == "ENDCASE":  return CF_ENDCASE
    return -1

class CtrlEntry(object):
    """Control stack entry for compilation-time control structures.

    RPython-friendly class to avoid tuple unpacking and string comparisons.
    """
    def __init__(self, kind, index):
        self.kind = kind    # int: CTRL_IF, CTRL_ELSE, or CTRL_DO
        self.index = index  # int: position in scurrent_code for patching
        self.leave_addrs = []  # list of LEAVE positions to patch (for DO loops)
        self.limit_is_literal = False  # True when a DO limit came from a compile-time literal


class CompContext(object):
    """A saved compilation context, used to nest a runtime :NONAME definition
    inside a word that is already being compiled (or interpreted)."""
    def __init__(self, code, lits, cc_ptr, lit_ptr, ctrl, does_ip_mark,
                 current_name, noname_mode, current_predefined, state):
        self.code = code
        self.lits = lits
        self.cc_ptr = cc_ptr
        self.lit_ptr = lit_ptr
        self.ctrl = ctrl
        self.does_ip_mark = does_ip_mark
        self.current_name = current_name
        self.noname_mode = noname_mode
        self.current_predefined = current_predefined
        self.state = state


class OuterInterpreter(object):
    _immutable_fields_ = ['wBR', 'w0BR', 'wLIT', 'wEXIT', 'wDO', 'wQDO', 'wLOOP', 'wLOOPNP', 'wPLUSLOOP', 'wLEAVE', 'wTYPE', 'wUNLOOP', 'wABORTQUOTE']

    def __init__(self, inner):
        self.inner = inner
        inner.outer = self    # Back-reference for LITERAL/FLITERAL primitives
        self.dict = {}         # dictionary is owned here (case-insensitive by uppercase keys)
        self.state = INTERPRET
        self.comment = False
        self.current_name = ''
        self.last_word = None  # Last defined word (for IMMEDIATE)

        # Input source tracking for SOURCE and >IN
        self.source_buffer = ''
        self.source_index = 0    # Current parse position (>IN)
        # include_lines/include_pos: line buffer for the INCLUDEd file, so REFILL can pull the next line.
        self.include_lines = []
        self.include_pos = 0
        # Per-spelling occurrence counters for string tokens (S", .", C") on the current line.
        self.string_token_counts = {}

        # NEXTNAME: overrides the name the next defining word parses; consumed once then cleared.
        self.nextname_str = ''
        self.has_nextname = False

        # Runtime parse cursor in a dedicated heap cell (to_in_addr) so >IN save/restore works via @ / !; token index, not byte offset.
        self.toks = []
        # Reserved fixed cells at top of DICTIONARY region; never collide with HERE-managed allocations, stable for tests.
        from rpyforth.heap import DICT_SIZE_BYTES
        self.to_in_addr = DICT_SIZE_BYTES - 8
        inner.cell_store(self.to_in_addr, 0)
        self.base_addr = DICT_SIZE_BYTES - 16
        inner.cell_store(self.base_addr, inner.base)
        self.state_addr = DICT_SIZE_BYTES - 24
        # Fixed scratch for WORD's counted string; must not advance HERE (fcp measures HERE deltas around book loading).
        self.word_scratch_addr = DICT_SIZE_BYTES - 280
        # Fixed scratch for XT>STRING / NAME>STRING: must not advance HERE (brew interleaves xt>string with `,`/ALLOT).
        self.xt_string_scratch_addr = DICT_SIZE_BYTES - 600
        # Fixed rotating scratch for PARSE-NAME/PARSE-WORD; alloc-off-HERE would corrupt brew's xt array, so ring of slots.
        self.parse_name_scratch_base = DICT_SIZE_BYTES - 2200
        self.parse_name_scratch_slots = 4
        self.parse_name_scratch_slot_size = 256
        self.parse_name_scratch_next = 0
        # SP@ scratch: data stack is an array so SP@ can't return a real address; stashes top value here instead.
        self.sp_scratch_addr = DICT_SIZE_BYTES - 2400
        inner.cell_store(self.state_addr, 0)

        # DOES> body start index in current code (-1 = none); sliced into a standalone thread at finalize.
        self.does_ip_mark = -1

        # True when RECURSE/RECURSIVE pre-bound the word; finalize reuses the existing Word object rather than creating a fresh one.
        self.current_predefined = False

        self.reset_code()

        self.ctrl = []         # control stack at compilation

        # Saved compilation contexts for runtime :NONAME nesting (lexex setOutputFile).
        self.comp_stack = []

        install_primitives(self)

        self.wBR = self.dict["BRANCH"]
        self.w0BR = self.dict["0BRANCH"]
        self.wLIT = self.dict["LIT"]
        self.wEXIT = self.dict["EXIT"]
        self.wDO = self.dict["(DO)"]
        self.wQDO = self.dict["(?DO)"]
        self.wLOOP = self.dict["(LOOP)"]
        self.wLOOPNP = self.dict["(LOOPNP)"]
        self.wPLUSLOOP = self.dict["(+LOOP)"]
        self.wUNLOOP = self.dict["UNLOOP"]
        self.wLEAVE = self.dict["LEAVE"]
        self.wTYPE = self.dict["TYPE"]
        self.wABORTQUOTE = self.dict['(ABORT")']

        # Cell address backing each VALUE name (for TO to locate its storage).
        self.value_addrs = {}

        # DeferredCodeThread id backing each DEFER name.
        self.defer_ids = {}

        # Search order (A5): wordlist 0 is FORTH-WORDLIST; search_order is front-first; lone default stays on the fast path.
        self.forth_wl = self.dict
        self.wordlists = [self.dict]
        self.search_order = [0]
        self.current_wl = 0
        self.order_is_default = True

        # True while compiling a :NONAME definition (push xt instead of naming it).
        self.noname_mode = False

        # [IF]/[ELSE]/[THEN] conditional-compilation skip state (spans lines).
        self.cond_skipping = False
        self.cond_skip_depth = 0
        self.cond_skip_to_else = False

        # Open-paren comment depth carried across lines (gforth: unterminated '(' continues onto following lines).
        self.paren_depth = 0

        self._define_literal_word()
        self._define_fliteral_word()

    def reset_code(self):
        self.current_code = [None] * 128
        self.current_lits = [None] * 128
        self.cc_ptr = 0
        self.lit_ptr = 0

    def push_code(self, w):
        if self.cc_ptr >= len(self.current_code):
            self.current_code = self.current_code + [None] * len(self.current_code)
        self.current_code[self.cc_ptr] = w
        self.cc_ptr += 1

    def pop_code(self):
        assert self.cc_ptr > 0
        self.cc_ptr -= 1
        return self.current_code[self.cc_ptr]

    def push_lit(self, w):
        if self.lit_ptr >= len(self.current_lits):
            self.current_lits = self.current_lits + [None] * len(self.current_lits)
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
        self.last_word = w
        return w

    def _define_literal_word(self):
        from rpyforth.primitives import prim_LITERAL
        w = Word("LITERAL", prim=prim_LITERAL, immediate=True, thread=None)
        self.dict["LITERAL"] = w
        self.last_word = w

    def _define_fliteral_word(self):
        from rpyforth.primitives import prim_FLITERAL
        w = Word("FLITERAL", prim=prim_FLITERAL, immediate=True, thread=None)
        self.dict["FLITERAL"] = w
        self.last_word = w

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
        for i in range(start_idx, length):
            ch = s[i]
            if ch < '0' or ch > '9':
                return False
        return True

    @elidable
    def _to_number(self, s):
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
        return result

    def _digit_value(self, ch):
        o = ord(ch)
        if o >= 48 and o <= 57:      # 0-9
            return o - 48
        if o >= 65 and o <= 90:      # A-Z
            return o - 55
        if o >= 97 and o <= 122:     # a-z
            return o - 87
        return -1

    def _prefix_base(self, s):
        """Recognize gforth number-prefix specifiers that override BASE: '$' hex,
        '#'/'&' decimal, '%' binary. Returns the effective base, or the passed-in
        default when there is no prefix."""
        if len(s) == 0:
            return -1, 0
        c = s[0]
        if c == '$':
            return 16, 1
        if c == '#' or c == '&':
            return 10, 1
        if c == '%':
            return 2, 1
        return 0, 0

    def _is_number_base(self, s, base):
        """True if s is an integer literal in the given base (leading '-' ok).
        A leading '$'/'#'/'%' prefix overrides the base (gforth specifiers)."""
        length = len(s)
        if length == 0:
            return False
        pbase, pskip = self._prefix_base(s)
        if pbase > 0:
            base = pbase
        start_idx = pskip
        if start_idx < length and s[start_idx] == '-':
            start_idx += 1
        if start_idx >= length:
            return False
        for i in range(start_idx, length):
            d = self._digit_value(s[i])
            if d < 0 or d >= base:
                return False
        return True

    def _to_number_base(self, s, base):
        length = len(s)
        pbase, pskip = self._prefix_base(s)
        if pbase > 0:
            base = pbase
        sign = 1
        start_idx = pskip
        if start_idx < length and s[start_idx] == '-':
            sign = -1
            start_idx += 1
        n = 0
        for i in range(start_idx, length):
            n = n * base + self._digit_value(s[i])
        return sign * n

    @unroll_safe
    def _is_float(self, s):
        length = len(s)
        if length == 0:
            return False

        idx = 0
        if s[idx] == '-':
            idx += 1
            if idx >= length:
                return False

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
                if idx + 1 < length and (s[idx + 1] == '+' or s[idx + 1] == '-'):
                    idx += 1
            elif '0' <= ch <= '9':
                has_digit = True
            else:
                return False
            idx += 1

        return has_digit and (has_dot or has_e)

    def _to_float(self, s):
        # Forth allows '1e' (= 1e0); Python needs a digit after 'e', so append '0'.
        if s.endswith('e') or s.endswith('E'):
            s = s + '0'
        elif s.endswith('e+') or s.endswith('E+') or s.endswith('e-') or s.endswith('E-'):
            s = s + '0'
        return float(s)

    def _emit_with_target(self, w, target_index):
        self.push_code(w)
        self.push_lit(W_IntObject(target_index))

    def _patch_here(self, at_index):
        self.current_lits[at_index] = W_IntObject(self.cc_ptr)

    def _read_tok(self, toks, i):
        t = toks[i]
        return t, i+1

    def _take_defining_name(self, toks, i):
        """Read the name for a defining word from the token stream, unless
        NEXTNAME left a pending override -- then use that instead and leave
        the token cursor untouched (NEXTNAME's string is not itself a
        token in the current line)."""
        if self.has_nextname:
            name = self.nextname_str
            self.has_nextname = False
            self.nextname_str = ''
            return name, i
        return self._read_tok(toks, i)

    def _take_defining_name_rt(self):
        """Like _take_defining_name, but for defining words executed via the
        runtime parse cursor (parse_next_token), e.g. CREATE/CONSTANT/VARIABLE
        executed as ordinary dictionary words."""
        if self.has_nextname:
            name = self.nextname_str
            self.has_nextname = False
            self.nextname_str = ''
            return name
        return self.parse_next_token()

    def _string_from_c_addr(self, c_addr, length):
        """Read length bytes starting at c_addr as a str. Prefers the boxed
        buffer parked by alloc_buf (S", etc.) when it is long enough, else
        falls back to raw char memory (mirrors _handle_evaluate)."""
        if length < 0:
            length = 0
        buf_str = self.inner.buf_get(c_addr)
        if isinstance(buf_str, W_StringObject) and len(buf_str.strval) >= length:
            return buf_str.strval[:length]
        chars = []
        for k in range(length):
            chars.append(chr(self.inner.char_fetch(c_addr + k)))
        return "".join(chars)

    def runtime_nextname(self):
        """NEXTNAME ( c-addr u -- ) (gforth): set the pending name used by the
        next defining word (CREATE, :, CONSTANT, VALUE, ...). Consumed once,
        then cleared automatically by the defining word that picks it up."""
        length = self.inner.pop_ds_int()
        c_addr = self.inner.pop_ds_int()
        self.nextname_str = self._string_from_c_addr(c_addr, length)
        self.has_nextname = True

    @unroll_safe
    def _find_nth_occurrence(self, line, token, n):
        """Find the Nth occurrence (0-indexed) of token in line. Returns position after token, or -1."""
        line_len = len(line)
        token_len = len(token)
        count = 0
        pos = 0

        while pos < line_len:
            while pos < line_len and line[pos] in ' \t\n\r\v\f':
                pos += 1
            if pos >= line_len:
                break
            tok_start = pos
            while pos < line_len and line[pos] not in ' \t\n\r\v\f':
                pos += 1
            tok = line[tok_start:pos]

            if tok == token:
                if count == n:
                    return pos
                count += 1

        return -1

    @unroll_safe
    def _parse_string_at_occurrence(self, start_token, occurrence):
        """Parse string from the Nth occurrence of start_token in source_buffer.

        Returns the string content (without quotes).
        """
        line = self.source_buffer
        line_len = len(line)

        pos = self._find_nth_occurrence(line, start_token, occurrence)
        if pos < 0:
            return ''

        if pos < line_len and line[pos] == ' ':
            pos += 1
        str_start = pos
        while pos < line_len and line[pos] != '"':
            pos += 1

        return line[str_start:pos]

    @unroll_safe
    def _skip_tokens_until_quote(self, toks, i):
        """Skip tokens until we find one ending with quote. Returns new index."""
        toks_len = len(toks)
        while i < toks_len:
            t = toks[i]
            i += 1
            t_len = len(t)
            if t_len > 0 and t[t_len - 1] == '"':
                break
        return i

    @unroll_safe
    def _parse_string_until_quote(self, toks, i):
        """Parse tokens until closing quote, return (parsed_string, new_index).

        Note: This is kept for ABORT" in compile mode where we need token-based parsing.
        """
        toks_len = len(toks)
        parts = []
        while i < toks_len:
            t, i = self._read_tok(toks, i)
            t_len = len(t)
            if t_len > 0 and t[t_len - 1] == '"':
                stop = t_len - 1
                assert 0 <= stop <= len(t)
                parts.append(t[:stop])
                break
            parts.append(t)
        return ' '.join(parts), i

    def _next_string_occurrence(self, start_token):
        n = self.string_token_counts.get(start_token, 0)
        self.string_token_counts[start_token] = n + 1
        return n

    def _copy_string_counts(self):
        d = {}
        for k in self.string_token_counts:
            d[k] = self.string_token_counts[k]
        return d

    def _handle_s_quote(self, toks, i, start_token):
        """Handle S" / s" - parse a string. In interpret mode push ( c-addr u );
        in compile mode allocate the buffer now and emit two literals so the
        compiled word pushes the same pair at runtime."""
        parsed_str = self._parse_string_at_occurrence(
            start_token, self._next_string_occurrence(start_token))
        i = self._skip_tokens_until_quote(toks, i)
        size = len(parsed_str)
        c_addr = self.inner.alloc_buf(parsed_str, size)
        if self.state == INTERPRET:
            self.inner.push_ds_int(c_addr)
            self.inner.push_ds_int(size)
        else:
            self._emit_lit(W_IntObject(c_addr))
            self._emit_lit(W_IntObject(size))
        return i

    def _handle_dot_quote(self, toks, i, start_token):
        """Handle ." / ." - parse string and print or compile."""
        parsed_str = self._parse_string_at_occurrence(
            start_token, self._next_string_occurrence(start_token))
        i = self._skip_tokens_until_quote(toks, i)
        w_str = W_StringObject(parsed_str)
        if self.state == INTERPRET:
            self.inner.print_str(w_str)
        else:
            self._emit_lit(w_str)
            self._emit_word(self.wTYPE)
        return i

    def _find_close_paren(self, line, start):
        """Return the position of the next ')' at or after start, else len(line)."""
        pos = start
        n = len(line)
        while pos < n and line[pos] != ')':
            pos += 1
        return pos

    def _handle_dot_paren(self, toks, i, start_token):
        """.( ccc ) -- print ccc immediately during parsing (up to ')')."""
        line = self.source_buffer
        occ = self._find_nth_occurrence(line, start_token, self._next_string_occurrence(start_token))
        if occ < 0:
            return i
        if occ < len(line) and line[occ] == ' ':
            occ += 1
        end = self._find_close_paren(line, occ)
        assert 0 <= occ <= end
        self.inner.print_str(W_StringObject(line[occ:end]))
        toks_len = len(toks)
        while i < toks_len:
            tok = toks[i]
            i += 1
            if ')' in tok:
                break
        return i

    def _define_simple_word(self, name, val):
        """Define a word that pushes a single value (for VARIABLE, CONSTANT, etc.)."""
        code = [self.wLIT, self.wEXIT]
        lits = [val, ZERO]
        thread = CodeThread(code, lits)
        self.define_colon(name, thread)

    def _handle_variable(self, toks, i):
        """Handle VARIABLE and FVARIABLE."""
        toks_len = len(toks)
        if not self.has_nextname and i >= toks_len:
            print "VARIABLE/FVARIABLE requires a name"
            return -1
        name, i = self._take_defining_name(toks, i)
        addr = self.inner.here
        self.inner.here += self.inner.cell_size_bytes
        self._define_simple_word(name, W_IntObject(addr))
        return i

    def _handle_2variable(self, toks, i):
        toks_len = len(toks)
        if not self.has_nextname and i >= toks_len:
            print "2VARIABLE requires a name"
            return -1
        name, i = self._take_defining_name(toks, i)
        addr = self.inner.here
        self.inner.here += self.inner.cell_size_bytes
        self.inner.here += self.inner.cell_size_bytes  # allocate 2 cells
        self._define_simple_word(name, W_IntObject(addr))
        return i

    def _handle_constant(self, toks, i):
        """Handle CONSTANT and FCONSTANT."""
        toks_len = len(toks)
        if not self.has_nextname and i >= toks_len:
            print "CONSTANT requires a name"
            return -1
        name, i = self._take_defining_name(toks, i)
        if self.inner.ds_int_size() > 0:
            intval = self.inner.pop_ds_int()
            val = W_IntObject(intval)
        elif self.inner.depth_ds_float() > 0:
            floatval = self.inner.pop_ds_float()
            val = W_FloatObject(floatval)
        elif self.inner.ds_ptr_locals > 0:
            val = self.inner.pop_ds()
        else:
            print "CONSTANT: stack underflow"
            return -1
        self._define_simple_word(name, val)
        return i

    def _naligned(self, addr, align):
        if align <= 1:
            return addr
        return (addr + align - 1) & ~(align - 1)

    def _handle_field(self, toks, i):
        """FIELD ( align1 offset1 align size "name" -- align2 offset2 ). Defines
        name as ( addr -- addr+field-offset ) and advances the running offset."""
        toks_len = len(toks)
        if i >= toks_len:
            print "FIELD requires a name"
            return -1
        name, i = self._read_tok(toks, i)
        if self.inner.ds_int_size() < 4:
            print "FIELD: stack underflow"
            return -1
        size = self.inner.pop_ds_int()
        align = self.inner.pop_ds_int()
        offset1 = self.inner.pop_ds_int()
        align1 = self.inner.pop_ds_int()
        field_offset = self._naligned(offset1, align)
        code = [self.wLIT, self.forth_wl["+"], self.wEXIT]
        lits = [W_IntObject(field_offset), ZERO, ZERO]
        self.define_colon(name, CodeThread(code, lits))
        self.inner.push_ds_int(align1)
        self.inner.push_ds_int(field_offset + size)
        return i

    def _handle_end_struct(self, toks, i):
        """END-STRUCT ( align size "name" -- ). Defines name to push ( align size )."""
        toks_len = len(toks)
        if i >= toks_len:
            print "END-STRUCT requires a name"
            return -1
        name, i = self._read_tok(toks, i)
        if self.inner.ds_int_size() < 2:
            print "END-STRUCT: stack underflow"
            return -1
        size = self.inner.pop_ds_int()
        align = self.inner.pop_ds_int()
        size = self._naligned(size, align)
        code = [self.wLIT, self.wLIT, self.wEXIT]
        lits = [W_IntObject(align), W_IntObject(size), ZERO]
        self.define_colon(name, CodeThread(code, lits))
        return i

    def _handle_2constant(self, toks, i):
        """Handle 2CONSTANT ( x1 x2 "name" -- ): name pushes x1 x2."""
        toks_len = len(toks)
        if not self.has_nextname and i >= toks_len:
            print "2CONSTANT requires a name"
            return -1
        name, i = self._take_defining_name(toks, i)
        if self.inner.ds_int_size() < 2:
            print "2CONSTANT: stack underflow"
            return -1
        x2 = self.inner.pop_ds_int()
        x1 = self.inner.pop_ds_int()
        code = [self.wLIT, self.wLIT, self.wEXIT]
        lits = [W_IntObject(x1), W_IntObject(x2), ZERO]
        self.define_colon(name, CodeThread(code, lits))
        return i

    def _handle_create(self, toks, i):
        toks_len = len(toks)
        if not self.has_nextname and i >= toks_len:
            print "CREATE requires a name"
            return -1
        name, i = self._take_defining_name(toks, i)
        addr = self.inner.here
        self._define_simple_word(name, W_IntObject(addr))
        return i

    def _handle_value(self, toks, i):
        """Handle VALUE: a cell-backed mutable constant. The word fetches its cell
        (LIT addr @); TO stores into the cell."""
        toks_len = len(toks)
        if not self.has_nextname and i >= toks_len:
            print "VALUE requires a name"
            return -1
        name, i = self._take_defining_name(toks, i)
        if self.inner.ds_int_size() <= 0:
            print "VALUE: stack underflow"
            return -1
        intval = self.inner.pop_ds_int()
        addr = self.inner.here
        self.inner.here += self.inner.cell_size_bytes
        self.inner.cell_store(addr, intval)
        self.value_addrs[to_upper(name)] = addr
        code = [self.wLIT, self.forth_wl["@"], self.wEXIT]
        lits = [W_IntObject(addr), ZERO, ZERO]
        self.define_colon(name, CodeThread(code, lits))
        return i

    def _handle_to(self, toks, i):
        """Handle TO (interpret mode): store the top value into the named VALUE."""
        toks_len = len(toks)
        if i >= toks_len:
            print "TO requires a name"
            return -1
        name, i = self._read_tok(toks, i)
        key = to_upper(name)
        if key not in self.value_addrs:
            print "TO: not a VALUE"
            return -1
        if self.inner.ds_int_size() <= 0:
            print "TO: stack underflow"
            return -1
        self.inner.cell_store(self.value_addrs[key], self.inner.pop_ds_int())
        return i

    def _include_file(self, path):
        """Read a Forth source file and interpret it line by line, preserving the
        caller's source-scan state across the nested interpretation. A missing or
        unreadable file raises a Forth exception (THROW -38, "non-existent file")
        so an enclosing CATCH can recover -- brew probes for an optional identity
        file with `['] included catch` on first run."""
        try:
            f = open_file_as_stream(path)
        except OSError:
            raise ForthException(-38)
        content = f.readall()
        f.close()
        saved_buf = self.source_buffer
        saved_idx = self.source_index
        saved_cnt = self._copy_string_counts()
        saved_toks = self.toks
        saved_cur = self.inner.cell_fetch_int(self.to_in_addr)
        saved_lines = self.include_lines
        saved_pos = self.include_pos
        saved_paren = self.paren_depth
        self.paren_depth = 0
        self.include_lines = content.split('\n')
        self.include_pos = 0
        n = len(self.include_lines)
        while self.include_pos < n:
            line = self.include_lines[self.include_pos]
            self.include_pos += 1
            self.interpret_line(line)
        self.paren_depth = saved_paren
        self.include_lines = saved_lines
        self.include_pos = saved_pos
        self.source_buffer = saved_buf
        self.source_index = saved_idx
        self.string_token_counts = saved_cnt
        self.toks = saved_toks
        self.inner.cell_store(self.to_in_addr, saved_cur)

    def _handle_include(self, toks, i):
        """Handle INCLUDE / REQUIRE: the filename is the next token."""
        toks_len = len(toks)
        if i >= toks_len:
            print "INCLUDE requires a filename"
            return -1
        name, i = self._read_tok(toks, i)
        self._include_file(name)
        return i

    def _handle_included(self):
        """INCLUDED / REQUIRED ( c-addr u -- ): the filename is a string."""
        self.inner.pop_ds_int()           # length (unused: the cell holds the string)
        c_addr = self.inner.pop_ds_int()
        w = self.inner.buf_get(c_addr)
        assert isinstance(w, W_StringObject)
        self._include_file(w.strval)

    def _handle_defer(self, toks, i):
        """DEFER: define a word that executes a later-bound xt."""
        toks_len = len(toks)
        if not self.has_nextname and i >= toks_len:
            print "DEFER requires a name"
            return -1
        name, i = self._take_defining_name(toks, i)
        code = [self.forth_wl["(DEFER)"], self.wEXIT]
        thread = DeferredCodeThread(code, [ZERO, ZERO])
        self.defer_ids[to_upper(name)] = thread.tid
        self.define_colon(name, thread)
        return i

    def _handle_is(self, toks, i):
        """IS (interpret mode): bind the xt on the stack to a DEFER word."""
        toks_len = len(toks)
        if i >= toks_len:
            print "IS requires a name"
            return -1
        name, i = self._read_tok(toks, i)
        key = to_upper(name)
        if key not in self.defer_ids:
            print "IS: not a DEFER"
            return -1
        thread = THREAD_REGISTRY.threads[self.defer_ids[key]]
        assert isinstance(thread, DeferredCodeThread)
        thread.deferred_word = word_from_wid(self.inner.pop_ds_int())
        return i

    # Control structure compilation helpers

    def _compile_if(self):
        orig = self.cc_ptr
        self._emit_with_target(self.w0BR, 0)
        self.ctrl.append(CtrlEntry(CTRL_IF, orig))

    def _compile_else(self):
        if not self.ctrl:
            print "ELSE without IF"
            return False
        entry = self.ctrl.pop()
        # A dangling WHILE is also resolved by ELSE (brew's basics.fs char-search-backwards).
        if entry.kind != CTRL_IF and entry.kind != CTRL_WHILE:
            print "ELSE without IF"
            return False
        self._patch_here(entry.index)
        orig2 = self.cc_ptr
        self._emit_with_target(self.wBR, 0)
        self.ctrl.append(CtrlEntry(CTRL_ELSE, orig2))
        self._patch_here(entry.index)
        return True

    def _compile_then(self):
        """Compile THEN. Resolves the forward branch left by IF/ELSE, or by a
        WHILE whose loop was closed with UNTIL (BEGIN ... WHILE ... UNTIL THEN)."""
        if not self.ctrl:
            print "THEN without IF/ELSE"
            return False
        entry = self.ctrl.pop()
        if (entry.kind != CTRL_IF and entry.kind != CTRL_ELSE
                and entry.kind != CTRL_WHILE):
            print "THEN without IF/ELSE"
            return False
        self._patch_here(entry.index)
        return True

    def _do_limit_is_literal(self):
        """True when the DO limit came from a compile-time literal. DO is
        ( limit start -- ), so the last two emitted cells push `limit` (deeper)
        and `start` (top); when both are LIT each pushed one value, so the limit
        is two cells back. Requiring both LIT keeps it sound: a multi-cell `start`
        hides the limit's position, so we report False and skip only the promote
        fast path. CONSTANTs compile to a LIT, so `NUM 0 DO` counts too."""
        if self.cc_ptr < 2:
            return False
        return (self.current_code[self.cc_ptr - 1] is self.wLIT and
                self.current_code[self.cc_ptr - 2] is self.wLIT)

    def _compile_do(self):
        limit_is_literal = self._do_limit_is_literal()
        self._emit_word(self.wDO)
        do_body_start = self.cc_ptr
        entry = CtrlEntry(CTRL_DO, do_body_start)
        entry.limit_is_literal = limit_is_literal
        self.ctrl.append(entry)

    def _compile_qdo(self):
        """Compile ?DO. Like DO, but emits a forward branch (patched to the loop
        end by LOOP/+LOOP) taken at runtime when limit == start."""
        limit_is_literal = self._do_limit_is_literal()
        qdo_addr = self.cc_ptr
        self._emit_with_target(self.wQDO, 0)
        do_body_start = self.cc_ptr
        entry = CtrlEntry(CTRL_DO, do_body_start)
        entry.leave_addrs.append(qdo_addr)
        entry.limit_is_literal = limit_is_literal
        self.ctrl.append(entry)

    def _compile_loop(self):
        if not self.ctrl:
            print "LOOP without DO"
            return False
        entry = self.ctrl.pop()
        if entry.kind != CTRL_DO:
            print "LOOP without DO"
            return False
        if entry.limit_is_literal:
            self._emit_with_target(self.wLOOP, entry.index)
        else:
            self._emit_with_target(self.wLOOPNP, entry.index)
        loop_end = self.cc_ptr
        for leave_addr in entry.leave_addrs:
            self.current_lits[leave_addr] = W_IntObject(loop_end)
        return True

    def _compile_begin(self):
        begin_addr = self.cc_ptr
        self.ctrl.append(CtrlEntry(CTRL_BEGIN, begin_addr))

    def _find_begin_index(self):
        """Return the loop-back target of the nearest enclosing BEGIN on the
        control stack (searching under any WHILE entries), or -1. Does not modify
        the stack, so multiple WHILEs can share one BEGIN (BEGIN .. WHILE .. WHILE
        .. REPEAT THEN, as in ansify.fth xt-skip)."""
        idx = len(self.ctrl) - 1
        while idx >= 0:
            if self.ctrl[idx].kind == CTRL_BEGIN:
                return self.ctrl[idx].index
            idx -= 1
        return -1

    def _compile_while(self):
        """Compile WHILE. Emits a forward conditional branch and leaves a WHILE
        entry on the control stack (above the BEGIN, which stays put). REPEAT
        resolves the nearest WHILE; any further WHILEs are resolved by THEN."""
        if self._find_begin_index() < 0:
            print "WHILE without BEGIN"
            return False
        while_addr = self.cc_ptr
        self._emit_with_target(self.w0BR, 0)
        self.ctrl.append(CtrlEntry(CTRL_WHILE, while_addr))
        return True

    def _compile_repeat(self):
        """Compile REPEAT. Branch back to the enclosing BEGIN and resolve the
        nearest WHILE. Extra WHILEs remain for a trailing THEN to resolve."""
        if not self.ctrl or self.ctrl[len(self.ctrl) - 1].kind != CTRL_WHILE:
            print "REPEAT without proper BEGIN...WHILE"
            return False
        while_entry = self.ctrl.pop()
        begin_index = self._find_begin_index()
        if begin_index < 0:
            print "REPEAT without BEGIN...WHILE"
            return False
        self._emit_with_target(self.wBR, begin_index)
        self._patch_here(while_entry.index)
        self._drop_nearest_begin()
        return True

    def _drop_nearest_begin(self):
        """Remove the nearest BEGIN entry from the control stack (used by REPEAT
        once it has branched back to it), preserving the WHILE entries above it."""
        target = -1
        idx = len(self.ctrl) - 1
        while idx >= 0:
            if self.ctrl[idx].kind == CTRL_BEGIN:
                target = idx
                break
            idx -= 1
        if target < 0:
            return
        new_ctrl = []
        for k in range(len(self.ctrl)):
            if k != target:
                new_ctrl.append(self.ctrl[k])
        self.ctrl = new_ctrl

    def _compile_plusloop(self):
        if not self.ctrl:
            print "+LOOP without DO"
            return False
        entry = self.ctrl.pop()
        if entry.kind != CTRL_DO:
            print "+LOOP without DO"
            return False
        self._emit_with_target(self.wPLUSLOOP, entry.index)
        loop_end = self.cc_ptr
        for leave_addr in entry.leave_addrs:
            self.current_lits[leave_addr] = W_IntObject(loop_end)
        return True

    def _compile_again(self):
        """Compile AGAIN (unconditional branch back to BEGIN)."""
        if not self.ctrl:
            print "AGAIN without BEGIN"
            return False
        entry = self.ctrl.pop()
        if entry.kind != CTRL_BEGIN:
            print "AGAIN without BEGIN"
            return False
        self._emit_with_target(self.wBR, entry.index)
        return True

    def _compile_until(self):
        """Compile UNTIL (conditional branch back to BEGIN if false). Also handles
        BEGIN ... WHILE ... UNTIL THEN (fcp's >goodVar): the WHILE entry sits above
        its BEGIN, so pop it, branch back to BEGIN, then re-push it so the
        following THEN resolves its forward exit branch."""
        if not self.ctrl:
            print "UNTIL without BEGIN"
            return False
        entry = self.ctrl.pop()
        if entry.kind == CTRL_WHILE:
            if not self.ctrl:
                print "UNTIL without BEGIN"
                self.ctrl.append(entry)
                return False
            begin_entry = self.ctrl.pop()
            if begin_entry.kind != CTRL_BEGIN:
                print "UNTIL without BEGIN"
                self.ctrl.append(begin_entry)
                self.ctrl.append(entry)
                return False
            self._emit_with_target(self.w0BR, begin_entry.index)
            self.ctrl.append(entry)
            return True
        if entry.kind != CTRL_BEGIN:
            print "UNTIL without BEGIN"
            return False
        self._emit_with_target(self.w0BR, entry.index)
        return True

    def _compile_leave(self):
        for i in range(len(self.ctrl) - 1, -1, -1):
            entry = self.ctrl[i]
            if entry.kind == CTRL_DO:
                leave_addr = self.cc_ptr
                self._emit_with_target(self.wLEAVE, 0)
                entry.leave_addrs.append(leave_addr)
                return True
        print "LEAVE without DO"
        return False

    def _compile_case(self):
        """Compile CASE. The entry collects each ENDOF's forward branch, all
        patched past the selector DROP at ENDCASE."""
        self.ctrl.append(CtrlEntry(CTRL_CASE, 0))

    def _compile_of(self):
        """Compile OF: OVER = 0BRANCH<next-clause> DROP."""
        if not self.ctrl or self.ctrl[len(self.ctrl) - 1].kind != CTRL_CASE:
            print "OF without CASE"
            return False
        self._emit_word(self.forth_wl["OVER"])
        self._emit_word(self.forth_wl["="])
        of_addr = self.cc_ptr
        self._emit_with_target(self.w0BR, 0)
        self._emit_word(self.forth_wl["DROP"])
        self.ctrl.append(CtrlEntry(CTRL_OF, of_addr))
        return True

    def _compile_endof(self):
        """Compile ENDOF: BRANCH<endcase>; patch the OF 0BRANCH to the next clause."""
        if not self.ctrl or self.ctrl[len(self.ctrl) - 1].kind != CTRL_OF:
            print "ENDOF without OF"
            return False
        of_entry = self.ctrl.pop()
        endof_addr = self.cc_ptr
        self._emit_with_target(self.wBR, 0)
        self.ctrl[len(self.ctrl) - 1].leave_addrs.append(endof_addr)
        self._patch_here(of_entry.index)
        return True

    def _compile_endcase(self):
        """Compile ENDCASE: DROP the selector, then patch every ENDOF branch past it."""
        if not self.ctrl or self.ctrl[len(self.ctrl) - 1].kind != CTRL_CASE:
            print "ENDCASE without CASE"
            return False
        entry = self.ctrl.pop()
        self._emit_word(self.forth_wl["DROP"])
        end = self.cc_ptr
        for endof_addr in entry.leave_addrs:
            self.current_lits[endof_addr] = W_IntObject(end)
        return True
        return False

    def _compile_char(self, toks, i):
        toks_len = len(toks)
        if i >= toks_len:
            print "[CHAR] requires a following character"
            return i
        char_tok = toks[i]
        i += 1
        if len(char_tok) > 0:
            self._emit_lit(W_IntObject(ord(char_tok[0])))
        else:
            print "[CHAR] got empty token"
        return i

    # Word execution/compilation helpers

    def _execute_or_push(self, w, t):
        """Execute word or push number in INTERPRET mode."""
        base = self.inner.base
        if w is not None:
            self.inner.execute_word_now(w)
        elif base == 10 and self._is_float(t):
            self.inner.push_ds_float(self._to_float(t))
        elif self._is_number_base(t, base):
            self.inner.push_ds_int(self._to_number_base(t, base))
        else:
            print "UNKNOWN: " + t

    def _value_word_literal(self, w):
        """Return the literal x if w is a value-word (body LIT x ; EXIT), else
        None. Lets a VARIABLE/CONSTANT/CREATE reference compile to an immediate
        push instead of a call; the word stays in the dictionary, so ' / >BODY /
        EXECUTE are unaffected."""
        if w.prim is not None:
            return None
        if w.immediate:
            return None
        if w.does_ip != -1:          # CREATE ... DOES> has custom runtime behavior
            return None
        thread = w.thread
        if thread is None:
            return None
        code = thread.code
        if len(code) != 2:
            return None
        if code[0] is not self.wLIT:
            return None
        if code[1] is not self.wEXIT:
            return None
        return thread.lits[0]

    def _inlinable_colon_body(self, w):
        """Return w's CodeThread if it is a small colon word safe to splice
        inline, else None. Control flow is fine: branch/loop words carry an
        absolute instruction index that is relocated at splice time, and interior
        EXITs become branches past the spliced body. Not splicable: primitives,
        immediate words, DOES>-carved words, and tail-call-optimized bodies (a
        mid-thread TAILCALL would misread its length-anchored literal)."""
        if w.prim is not None:
            return None
        if w.immediate:
            return None
        if w.does_ip != -1:            # CREATE ... DOES> has custom runtime behavior
            return None
        thread = w.thread
        if thread is None:
            return None
        # (DEFER) resolves its action from the identity of its owning thread;
        # splicing it into a caller would lose that identity.
        if isinstance(thread, DeferredCodeThread):
            return None
        if thread.does_word is not None:
            return None
        code = thread.code
        n = len(code)
        if n < 2:
            return None
        if code[n - 1] is not self.wEXIT:  # tail-call-optimized words end in TAILCALL
            return None
        body_len = n - 1
        if body_len > MAX_INLINE_BODY:
            return None
        wTAILCALL = self.forth_wl.get("TAILCALL", None)
        i = 0
        while i < body_len:
            cw = code[i]
            if wTAILCALL is not None and cw is wTAILCALL:
                return None
            if self._is_branch_target_word(cw):
                lit = thread.lits[i]
                if not isinstance(lit, W_IntObject):
                    return None
                if lit.intval < 0 or lit.intval > n:
                    return None
            i += 1
        return thread

    def _inlinable_does_body(self, w):
        """Return the carved DOES> body thread when w is a CREATEd word of
        shape [LIT addr, does_word, EXIT] and that body is itself splicable.
        The call site then compiles as LIT addr + spliced body, removing the
        per-call round-trip through the anonymous does word."""
        if w.prim is not None or w.immediate or w.does_ip != -1:
            return None
        thread = w.thread
        if thread is None or isinstance(thread, DeferredCodeThread):
            return None
        if thread.does_word is not None:
            return None
        code = thread.code
        if len(code) != 3:
            return None
        if code[0] is not self.wLIT or code[2] is not self.wEXIT:
            return None
        dw = code[1]
        if dw is None:
            return None
        return self._inlinable_colon_body(dw)

    def _emit_inline(self, thread):
        """Splice a callee body (all but its trailing EXIT) into the current
        def, relocating branch targets by the insertion offset. A target at or
        past the callee's EXIT slot means "leave the body": it is clamped to
        land just after the spliced code, which is also where interior EXITs
        branch to."""
        code = thread.code
        lits = thread.lits
        body_len = len(code) - 1
        base = self.cc_ptr
        i = 0
        while i < body_len:
            cw = code[i]
            if cw is self.wEXIT:
                self.push_code(self.wBR)
                self.push_lit(W_IntObject(base + body_len))
            elif self._is_branch_target_word(cw):
                lit = lits[i]
                assert isinstance(lit, W_IntObject)
                t = lit.intval
                if t > body_len:
                    t = body_len
                self.push_code(cw)
                self.push_lit(W_IntObject(base + t))
            else:
                self.push_code(cw)
                self.push_lit(lits[i])
            i += 1

    def _compile_word_or_literal(self, w, t):
        """Compile word or literal in COMPILE mode."""
        if w is not None:
            if w.immediate:
                self.inner.execute_word_now(w)
            else:
                lit = self._value_word_literal(w)
                if lit is not None:
                    self._emit_lit(lit)
                else:
                    dbody = self._inlinable_does_body(w)
                    if dbody is not None:
                        self._emit_lit(w.thread.lits[0])
                        self._emit_inline(dbody)
                    else:
                        body = self._inlinable_colon_body(w)
                        if body is not None:
                            self._emit_inline(body)
                        else:
                            self._emit_word(w)
        elif self.inner.base == 10 and self._is_float(t):
            self._emit_lit(W_FloatObject(self._to_float(t)))
        elif self._is_number_base(t, self.inner.base):
            self._emit_lit(W_IntObject(self._to_number_base(t, self.inner.base)))
        else:
            print "UNKNOWN: " + t

    # System word handlers

    def _lookup(self, name_upper):
        """Resolve a name to a Word through the search order. On the default
        single-wordlist order this is exactly self.dict.get (the fast path)."""
        if self.order_is_default:
            return self.dict.get(name_upper, None)
        for idx in range(len(self.search_order)):
            wl = self.wordlists[self.search_order[idx]]
            w = wl.get(name_upper, None)
            if w is not None:
                return w
        return None

    def _recompute_order_default(self):
        self.order_is_default = (
            len(self.search_order) == 1
            and self.search_order[0] == 0
            and self.current_wl == 0
        )

    def _set_current_wl(self, wl_id):
        self.current_wl = wl_id
        self.dict = self.wordlists[wl_id]
        self._recompute_order_default()

    # WORDLIST ( -- wid )
    def _handle_wordlist(self):
        wid = len(self.wordlists)
        self.wordlists.append({})
        self.inner.push_ds_int(wid)

    # VOCABULARY ( "name" -- )
    def _handle_vocabulary(self, toks, i):
        toks_len = len(toks)
        if i >= toks_len:
            print "VOCABULARY requires a name"
            return -1
        name, i = self._read_tok(toks, i)
        wid = len(self.wordlists)
        self.wordlists.append({})
        code = [self.wLIT, self.forth_wl["(VOCABULARY)"], self.wEXIT]
        lits = [W_IntObject(wid), ZERO, ZERO]
        self.define_colon(name, CodeThread(code, lits))
        return i

    def runtime_vocab_select(self, wid):
        # Always keep wid 0 (FORTH-WORDLIST) in the order so selecting a vocab never hides core words.
        if wid < 0 or wid >= len(self.wordlists):
            return
        if len(self.search_order) == 0:
            self.search_order = [wid]
        else:
            self.search_order[0] = wid
        has_forth = False
        k = 0
        while k < len(self.search_order):
            if self.search_order[k] == 0:
                has_forth = True
                break
            k += 1
        if not has_forth:
            self.search_order.append(0)
        self._recompute_order_default()

    # GET-CURRENT ( -- wid ) / SET-CURRENT ( wid -- ) / FORTH-WORDLIST ( -- wid )
    def _handle_get_current(self):
        self.inner.push_ds_int(self.current_wl)

    def _handle_set_current(self):
        wid = self.inner.pop_ds_int()
        if 0 <= wid < len(self.wordlists):
            self._set_current_wl(wid)

    def _handle_forth_wordlist(self):
        self.inner.push_ds_int(0)

    # DEFINITIONS ( -- ) / FORTH ( -- ) / ONLY ( -- ) / ALSO ( -- ) / PREVIOUS ( -- )
    def _handle_definitions(self):
        if len(self.search_order) > 0:
            self._set_current_wl(self.search_order[0])

    def _handle_forth(self):
        if len(self.search_order) == 0:
            self.search_order = [0]
        else:
            self.search_order[0] = 0
        self._recompute_order_default()

    def _handle_only(self):
        self.search_order = [0]
        self._recompute_order_default()

    def _handle_also(self):
        if len(self.search_order) > 0:
            self.search_order.insert(0, self.search_order[0])
        else:
            self.search_order = [0]
        self._recompute_order_default()

    # >ORDER ( wid -- ) / GET-ORDER / SET-ORDER
    def _handle_to_order(self):
        wid = self.inner.pop_ds_int()
        if 0 <= wid < len(self.wordlists):
            self.search_order.insert(0, wid)
            self._recompute_order_default()

    def _handle_previous(self):
        if len(self.search_order) > 1:
            del self.search_order[0]
        self._recompute_order_default()

    def _handle_get_order(self):
        n = len(self.search_order)
        for k in range(n - 1, -1, -1):
            self.inner.push_ds_int(self.search_order[k])
        self.inner.push_ds_int(n)

    def _handle_set_order(self):
        n = self.inner.pop_ds_int()
        if n < 0:
            self.search_order = [0]
            self._recompute_order_default()
            return
        order = [0] * n
        for k in range(n):
            order[k] = self.inner.pop_ds_int()
        self.search_order = order
        self._recompute_order_default()

    # SEARCH-WORDLIST ( c-addr u wid -- 0 | xt 1 | xt -1 )
    def _handle_search_wordlist(self):
        wid = self.inner.pop_ds_int()
        length = self.inner.pop_ds_int()
        c_addr = self.inner.pop_ds_int()
        name_upper = to_upper(self._string_from_c_addr(c_addr, length))
        if 0 <= wid < len(self.wordlists):
            wl = self.wordlists[wid]
            w = wl.get(name_upper, None)
        else:
            w = None
        if w is None:
            self.inner.push_ds_int(0)
            return
        self.inner.push_ds_int(w.wid)
        if w.immediate:
            self.inner.push_ds_int(1)
        else:
            self.inner.push_ds_int(-1)

    def _counted_string_at(self, c_addr):
        """Read a counted string ( length byte then chars ) at c_addr. If a buf
        slot holds a W_StringObject there (an S"-style whole-string address), use
        it; otherwise decode the char-memory counted string that BL WORD builds."""
        buf_entry = self.inner.buf_get(c_addr)
        if isinstance(buf_entry, W_StringObject):
            return buf_entry.strval
        length = self.inner.char_fetch(c_addr)
        if length < 0:
            length = 0
        chars = []
        for k in range(length):
            chars.append(chr(self.inner.char_fetch(c_addr + 1 + k)))
        return "".join(chars)

    # FIND ( c-addr -- c-addr 0 | xt 1 | xt -1 )
    def _handle_find(self):
        w_caddr = self.inner.pop_ds_int()
        name = self._counted_string_at(w_caddr)
        word = self._lookup(to_upper(name))
        if word is not None:
            self.inner.push_ds_int(word.wid)
            if word.immediate:
                self.inner.push_ds_int(1)
            else:
                self.inner.push_ds_int(-1)
        else:
            self.inner.push_ds_int(w_caddr)
            self.inner.push_ds_int(0)

    # SOURCE ( -- c-addr u )
    def _handle_source(self):
        size = len(self.source_buffer)
        c_addr = self.inner.alloc_buf(self.source_buffer, size)
        self.inner.push_ds_int(c_addr)
        self.inner.push_ds_int(size)

    # >IN ( -- a-addr ) address of the runtime parse cursor (a token index).
    def _handle_to_in(self):
        self.inner.push_ds_int(self.to_in_addr)

    def parse_next_token(self):
        """Consume and return the next token from the line being interpreted,
        advancing the runtime parse cursor. Returns '' at end of line."""
        idx = self.inner.cell_fetch_int(self.to_in_addr)
        if idx < 0 or idx >= len(self.toks):
            return ''
        tok = self.toks[idx]
        self.inner.cell_store(self.to_in_addr, idx + 1)
        return tok

    def runtime_paren(self):
        """( executed as a word: consume tokens up to and including the next one
        containing ')'. Reached when '(' is POSTPONEd (compat/assert.fs). If the
        ')' is not on this line, leave paren_depth open so the line loop's comment
        stripper continues onto the next physical line, rather than pulling lines
        here and desynchronising the INCLUDE cursor."""
        while True:
            idx = self.inner.cell_fetch_int(self.to_in_addr)
            if idx < 0 or idx >= len(self.toks):
                self.paren_depth = 1
                return
            tok = self.toks[idx]
            self.inner.cell_store(self.to_in_addr, idx + 1)
            if ')' in tok:
                return

    def runtime_refill(self):
        """REFILL ( -- flag ): consume the next physical line of the file being
        INCLUDEd, make it the current parse buffer (resetting the parse cursor),
        and push true. If there is no next line, push false. Words like squares:
        use this to read table rows and parse names from them at run time."""
        if self.include_pos < len(self.include_lines):
            line = self.include_lines[self.include_pos]
            self.include_pos += 1
            self.source_buffer = line
            self.source_index = 0
            self.string_token_counts = {}
            self.toks = split_whitespace(line)
            self.inner.cell_store(self.to_in_addr, 0)
            self.inner.push_ds_int(-1)
        else:
            self.inner.push_ds_int(0)

    def runtime_postpone(self, word):
        """Append word to the definition currently being compiled. Invoked by the
        (POSTPONE) primitive that POSTPONE compiles for a non-immediate target,
        so an immediate word built with POSTPONE defers the target instead of
        executing it (e.g. hash! = POSTPONE ! must compile !, not run it)."""
        self._emit_word(word)

    def runtime_compile_cf(self, code):
        """Replay a built-in control-flow compile action, invoked by the (CF)
        primitive that POSTPONE/[COMPILE] emit for control-flow words (IF, THEN,
        BEGIN ...). Runs when the enclosing immediate word executes during
        compilation, so e.g. ': endif POSTPONE then ; immediate' resolves THEN's
        forward branch in the definition currently being compiled."""
        if code == CF_IF:
            self._compile_if()
        elif code == CF_ELSE:
            self._compile_else()
        elif code == CF_THEN:
            self._compile_then()
        elif code == CF_BEGIN:
            self._compile_begin()
        elif code == CF_WHILE:
            self._compile_while()
        elif code == CF_REPEAT:
            self._compile_repeat()
        elif code == CF_AGAIN:
            self._compile_again()
        elif code == CF_UNTIL:
            self._compile_until()
        elif code == CF_DO:
            self._compile_do()
        elif code == CF_QDO:
            self._compile_qdo()
        elif code == CF_LOOP:
            self._compile_loop()
        elif code == CF_PLUSLOOP:
            self._compile_plusloop()
        elif code == CF_LEAVE:
            self._compile_leave()
        elif code == CF_CASE:
            self._compile_case()
        elif code == CF_OF:
            self._compile_of()
        elif code == CF_ENDOF:
            self._compile_endof()
        elif code == CF_ENDCASE:
            self._compile_endcase()

    def runtime_begin_noname(self):
        """:NONAME executed from a colon body: save the enclosing compilation
        context and open a fresh nameless definition. Paired with runtime_end_
        definition, which restores the saved context (lexex setOutputFile)."""
        ctx = CompContext(
            self.current_code, self.current_lits, self.cc_ptr, self.lit_ptr,
            self.ctrl, self.does_ip_mark, self.current_name,
            self.noname_mode, self.current_predefined, self.state)
        self.comp_stack.append(ctx)
        self.reset_code()
        self.ctrl = []
        self.does_ip_mark = -1
        self.current_name = "NONAME"
        self.noname_mode = True
        self.current_predefined = False
        self.state = COMPILE

    def runtime_begin_named(self):
        """: executed from a colon body -- parse the next name and open a named
        definition, saving the enclosing compilation context. Paired with
        runtime_end_definition (POSTPONE ;). brew's basics.fs offset-defining
        words `(zero-offset:)` etc. are `: ... : ... POSTPONE ; ;`, i.e. they run
        `:` at execution time to create a new word."""
        name = self.parse_next_token()
        if name == '':
            print ": requires a name"
            return
        ctx = CompContext(
            self.current_code, self.current_lits, self.cc_ptr, self.lit_ptr,
            self.ctrl, self.does_ip_mark, self.current_name,
            self.noname_mode, self.current_predefined, self.state)
        self.comp_stack.append(ctx)
        self.reset_code()
        self.ctrl = []
        self.does_ip_mark = -1
        self.current_name = name
        self.noname_mode = to_upper(name) == "NONAME"
        self.current_predefined = False
        self.state = COMPILE

    def runtime_end_definition(self):
        """; executed from a colon body (POSTPONE ;): finalize the definition
        currently being compiled, then restore the enclosing compilation context
        saved by runtime_begin_noname."""
        self._finalize_definition()
        if self.comp_stack:
            ctx = self.comp_stack.pop()
            self.current_code = ctx.code
            self.current_lits = ctx.lits
            self.cc_ptr = ctx.cc_ptr
            self.lit_ptr = ctx.lit_ptr
            self.ctrl = ctx.ctrl
            self.does_ip_mark = ctx.does_ip_mark
            self.current_name = ctx.current_name
            self.noname_mode = ctx.noname_mode
            self.current_predefined = ctx.current_predefined
            self.state = ctx.state

    def runtime_sliteral(self):
        """SLITERAL ( c-addr u -- ) executed while compiling: append the string to
        the definition currently being compiled so it pushes ( c-addr u ) at run
        time (lexex setOutputFile builds a :NONAME that returns the file name)."""
        size = self.inner.pop_ds_int()
        c_addr = self.inner.pop_ds_int()
        self._emit_lit(W_IntObject(c_addr))
        self._emit_lit(W_IntObject(size))

    def _runtime_pop_value(self):
        """Pop one value off whichever data stack holds it, boxed for storage in
        a defined word's literal (mirrors _handle_constant)."""
        if self.inner.ds_int_size() > 0:
            return W_IntObject(self.inner.pop_ds_int())
        elif self.inner.depth_ds_float() > 0:
            return W_FloatObject(self.inner.pop_ds_float())
        elif self.inner.ds_ptr_locals > 0:
            return self.inner.pop_ds()
        return ZERO

    def runtime_constant(self):
        """CONSTANT executed from a colon body: name the next token, bind value."""
        name = self._take_defining_name_rt()
        if name == '':
            print "CONSTANT requires a name"
            return
        self._define_simple_word(name, self._runtime_pop_value())

    def runtime_variable(self):
        name = self._take_defining_name_rt()
        if name == '':
            print "VARIABLE requires a name"
            return
        addr = self.inner.here
        self.inner.here += self.inner.cell_size_bytes
        self._define_simple_word(name, W_IntObject(addr))

    def runtime_2constant(self):
        """2CONSTANT executed from a colon body: name the next token, bind a
        double-cell value. The child word pushes x1 then x2."""
        name = self._take_defining_name_rt()
        if name == '':
            print "2CONSTANT requires a name"
            return
        if self.inner.ds_int_size() < 2:
            print "2CONSTANT: stack underflow"
            return
        x2 = self.inner.pop_ds_int()
        x1 = self.inner.pop_ds_int()
        code = [self.wLIT, self.wLIT, self.wEXIT]
        lits = [W_IntObject(x1), W_IntObject(x2), ZERO]
        self.define_colon(name, CodeThread(code, lits))

    def runtime_2variable(self):
        """2VARIABLE executed from a colon body: allocate two cells, name it."""
        name = self._take_defining_name_rt()
        if name == '':
            print "2VARIABLE requires a name"
            return
        addr = self.inner.here
        self.inner.here += self.inner.cell_size_bytes
        self.inner.here += self.inner.cell_size_bytes
        self._define_simple_word(name, W_IntObject(addr))

    def runtime_cs_roll(self):
        """CS-ROLL ( C: origN..orig0 N -- origN-1..orig0 origN ): roll the top
        N+1 entries of the compile-time control-flow stack. N is on the data
        stack. Executed during compilation (e.g. via gc.fs [cs-roll])."""
        n = self.inner.pop_ds_int()
        depth = len(self.ctrl) - 1 - n
        if n < 0 or depth < 0:
            print "CS-ROLL: control-flow stack underflow"
            return
        entry = self.ctrl[depth]
        del self.ctrl[depth]
        self.ctrl.append(entry)

    def runtime_create(self, does_word):
        """CREATE executed from a colon body. The child word pushes its data
        field address; if the enclosing definition had a DOES>, does_word runs
        after the push."""
        name = self._take_defining_name_rt()
        if name == '':
            print "CREATE requires a name"
            return
        addr = self.inner.here
        if does_word is None:
            code = [self.wLIT, self.wEXIT]
            lits = [W_IntObject(addr), ZERO]
        else:
            code = [self.wLIT, does_word, self.wEXIT]
            lits = [W_IntObject(addr), ZERO, ZERO]
        self.define_colon(name, CodeThread(code, lits))

    def runtime_does(self, does_word):
        """(DOES>) executed where DOES> appears in a defining word: rebind the most
        recently CREATEd word so it pushes its data-field address then runs
        does_word. Covers the DOES>-only idiom where a word without its own CREATE
        patches a separately CREATEd one (lexex lexarrays.fth: 1darray), and is
        idempotent for the in-word CREATE ... DOES> case."""
        if does_word is None:
            return
        w = self.last_word
        if w is None:
            return
        old = w.thread
        if old is None:
            return
        addr_lit = old.lits[0]
        code = [self.wLIT, does_word, self.wEXIT]
        lits = [addr_lit, ZERO, ZERO]
        w.thread = CodeThread(code, lits)

    def runtime_defer(self):
        name = self._take_defining_name_rt()
        if name == '':
            print "DEFER requires a name"
            return
        code = [self.forth_wl["(DEFER)"], self.wEXIT]
        thread = DeferredCodeThread(code, [ZERO, ZERO])
        self.defer_ids[to_upper(name)] = thread.tid
        self.define_colon(name, thread)

    def runtime_word(self):
        """WORD executed from a colon body: consume the next token of the line
        being interpreted (via the shared parse cursor) and store it as a counted
        string at HERE, leaving its address. The delimiter char is popped and
        ignored -- tokens are already whitespace-split, which covers BL WORD."""
        self.inner.pop_ds_int()
        word_str = self.parse_next_token()
        length = len(word_str)
        addr = self.word_scratch_addr
        self.inner.char_store(addr, length)
        i = 0
        while i < length:
            self.inner.char_store(addr + 1 + i, ord(word_str[i]))
            i += 1
        self.inner.push_ds_int(addr)

    def _parse_start_char_pos(self, tok_index):
        """Return the char offset in source_buffer where PARSE begins for the
        current token cursor. gforth's PARSE does not skip leading whitespace: the
        cursor sits right after the single delimiter that terminated the previous
        token. So this returns the char just past token (tok_index-1) and its one
        trailing delimiter, or 0 at the start of the line."""
        line = self.source_buffer
        n = line and len(line) or 0
        if tok_index <= 0:
            return 0
        pos = 0
        count = 0
        while pos < n:
            while pos < n and line[pos] in ' \t\n\r\v\f':
                pos += 1
            if pos >= n:
                break
            # Skip comment regions so token index matches self.toks (built from comment-stripped line).
            skipped = self._comment_skip(line, pos, n)
            if skipped != pos:
                pos = skipped
                continue
            while pos < n and line[pos] not in ' \t\n\r\v\f':
                pos += 1
            count += 1
            if count == tok_index:
                if pos < n:
                    return pos + 1
                return n
        return n

    def _comment_skip(self, line, pos, n):
        """If a comment word begins at pos (a token boundary), return the position
        just past that comment, else pos unchanged. Mirrors the tokenizer's comment
        removal ('\\' to end of line, '( ... )' to the next ')') so char-based token
        counting over source_buffer stays aligned with the comment-stripped
        self.toks, letting PARSE find the right start after a stack comment."""
        if pos >= n:
            return pos
        ch = line[pos]
        if ch == '\\':
            nxt = pos + 1
            if nxt >= n or line[nxt] in ' \t\n\r\v\f':
                return n
        if ch == '(':
            nxt = pos + 1
            if nxt >= n or line[nxt] in ' \t\n\r\v\f':
                j = pos + 1
                while j < n and line[j] != ')':
                    j += 1
                if j < n:
                    return j + 1
                return n
        return pos

    def _token_index_at_char_pos(self, char_pos, raw):
        """Return the number of tokens that start before char_pos in source_buffer.
        When raw is False, comment regions are skipped to match self.toks (built
        from the comment-stripped line). When raw is True, every whitespace token
        counts (self.toks was replaced with a verbatim tokenization)."""
        line = self.source_buffer
        n = len(line)
        pos = 0
        count = 0
        while pos < n:
            while pos < n and line[pos] in ' \t\n\r\v\f':
                pos += 1
            if pos >= n:
                break
            if not raw:
                skipped = self._comment_skip(line, pos, n)
                if skipped != pos:
                    pos = skipped
                    continue
            if pos >= char_pos:
                return count
            while pos < n and line[pos] not in ' \t\n\r\v\f':
                pos += 1
            count += 1
        return count

    def runtime_parse(self):
        """PARSE from a colon body ( char "ccc<char>" -- c-addr u ). Scans the
        input line from the parse cursor to the next delimiter char, returns the
        text between (delimiter excluded) as ( c-addr u ), and advances past the
        delimiter. Leading spaces are not skipped; BL keeps the whitespace-token
        fast path (fcp's `BL PARSE`)."""
        delim = self.inner.pop_ds_int()
        if delim == 32:
            word_str = self.parse_next_token()
            self._store_parsed(word_str)
            return
        line = self.source_buffer
        n = len(line)
        idx = self.inner.cell_fetch_int(self.to_in_addr)
        start = self._parse_start_char_pos(idx)
        delim_ch = chr(delim & 0xFF)
        pos = start
        while pos < n and line[pos] != delim_ch:
            pos += 1
        assert 0 <= start <= pos <= n
        parsed = line[start:pos]
        if pos < n:
            new_char_pos = pos + 1
        else:
            new_char_pos = n
        # If consumed text contains '(' / ')' / '\', self.toks was truncated by the tokenizer; re-tokenize raw.
        consumed = line[start:new_char_pos]
        if '(' in consumed or ')' in consumed or '\\' in consumed:
            self.toks = _tokenize_raw(line)
            self.paren_depth = 0
            new_idx = self._token_index_at_char_pos(new_char_pos, True)
        else:
            new_idx = self._token_index_at_char_pos(new_char_pos, False)
        self.inner.cell_store(self.to_in_addr, new_idx)
        self._store_parsed(parsed)

    def _store_parsed(self, word_str):
        length = len(word_str)
        addr = self.inner.here
        for ch in word_str:
            self.inner.char_store(self.inner.here, ord(ch))
            self.inner.here += 1
        self.inner.push_ds_int(addr)
        self.inner.push_ds_int(length)

    def runtime_marker(self):
        """MARKER executed from a colon body ( "name" -- ): define a word that, on
        execution, would forget everything defined after it. Dictionary rollback
        is not modeled here, so the marker word is a harmless no-op -- adequate
        for a single benchmark load."""
        name = self.parse_next_token()
        if name == '':
            print "MARKER requires a name"
            return
        code = [self.wEXIT]
        lits = [ZERO]
        self.define_colon(name, CodeThread(code, lits))

    def runtime_included(self):
        """INCLUDED executed from a colon body ( c-addr u -- ). Every string is
        byte-backed in data space (alloc_buf writes S" strings there too), so
        the filename is always read from char memory. Loading goes through the
        same nested source path as INCLUDE, which saves/restores the parse
        cursor."""
        u = self.inner.pop_ds_int()
        c_addr = self.inner.pop_ds_int()
        if u < 0:
            u = 0
        chars = []
        for k in range(u):
            chars.append(chr(self.inner.char_fetch(c_addr + k)))
        self._include_file("".join(chars))

    def runtime_find(self):
        """FIND executed from a colon body ( c-addr -- c-addr 0 | xt 1 | xt -1 )."""
        self._handle_find()

    def runtime_base(self):
        """BASE executed from a colon body ( -- a-addr )."""
        self._handle_base()

    def _raw_token_at_index(self, tok_index):
        """Scan the raw source line for the whitespace-delimited token at
        tok_index (0-based), ignoring the tokenizer's comment/string handling.
        Returns the token text, or '' past the end. Used by PARSE-NAME so keywords
        that are themselves tokenizer-special (lexex declares '(' , '."' , 's"' as
        scanner keywords) are still read literally."""
        line = self.source_buffer
        n = len(line)
        pos = 0
        count = 0
        while pos < n:
            while pos < n and line[pos] in ' \t\n\r\v\f':
                pos += 1
            if pos >= n:
                break
            start = pos
            while pos < n and line[pos] not in ' \t\n\r\v\f':
                pos += 1
            if count == tok_index:
                assert 0 <= start <= pos
                return line[start:pos]
            count += 1
        return ''

    def runtime_parse_name(self):
        """PARSE-NAME ( "<spaces>name" -- c-addr u ): parse the next whitespace-
        delimited token from the input and return its address and length in data
        space. Reads the raw source line (not the comment/string-stripped token
        list) so tokenizer-special keywords ('(', '."', 's"') are read literally;
        lexex symbol declares such words as scanner keywords."""
        idx = self.inner.cell_fetch_int(self.to_in_addr)
        name = self._raw_token_at_index(idx)
        self.inner.cell_store(self.to_in_addr, idx + 1)
        # If name contains '(' / ')', the tokenizer wrongly bumped paren_depth; reset it.
        if '(' in name or ')' in name:
            self.paren_depth = 0
        size = len(name)
        if size > self.parse_name_scratch_slot_size:
            size = self.parse_name_scratch_slot_size
        slot = self.parse_name_scratch_next
        self.parse_name_scratch_next = (slot + 1) % self.parse_name_scratch_slots
        c_addr = self.parse_name_scratch_base + slot * self.parse_name_scratch_slot_size
        k = 0
        while k < size:
            self.inner.char_store(c_addr + k, ord(name[k]))
            k += 1
        self.inner.buf_set(c_addr, W_StringObject(name[:size]))
        self.inner.push_ds_int(c_addr)
        self.inner.push_ds_int(size)

    def runtime_defined(self):
        """DEFINED ( "name" -- flag ): parse the next name and push -1 if a word
        of that name exists, else 0. gforth's interpret-level DEFINED; brew's
        gforth.fs bootstraps [defined] as `[compile] defined`."""
        name = self.parse_next_token()
        if name != '' and self._lookup(to_upper(name)) is not None:
            self.inner.push_ds_int(-1)
        else:
            self.inner.push_ds_int(0)

    def runtime_char(self):
        """CHAR executed from a colon body: parse the next token from the input
        and push the code of its first character. Compiled where CHAR appears in a
        definition (lexex 'char' = char 'lit'), so it parses at run time rather
        than at compile time."""
        name = self.parse_next_token()
        if len(name) > 0:
            self.inner.push_ds_int(ord(name[0]))
        else:
            self.inner.push_ds_int(0)

    def runtime_bracket_if(self):
        """[IF] executed (from a stored xt or a colon body): consume the flag and,
        if false, start skipping the rest of the input up to the matching [ELSE] /
        [THEN]. brew stores ' [IF] in a gene descriptor and runs it later."""
        v = self.inner.pop_ds_int()
        if v == 0:
            self.cond_skipping = True
            self.cond_skip_depth = 0
            self.cond_skip_to_else = True

    def runtime_bracket_else(self):
        """[ELSE] executed on the true branch: skip to the matching [THEN]."""
        self.cond_skipping = True
        self.cond_skip_depth = 0
        self.cond_skip_to_else = False

    def runtime_bracket_then(self):
        """[THEN] executed: no-op (skipping, if any, is ended by the token loop)."""
        pass

    def runtime_tick(self):
        name = self.parse_next_token()
        if name == '':
            print "' requires a following word"
            return
        word = self._lookup(to_upper(name))
        if word is not None:
            self.inner.push_ds_int(word.wid)
        else:
            print "' cannot find word:", name

    # ' (tick) ( "<spaces>name" -- xt )
    def _handle_tick(self, toks, i):
        toks_len = len(toks)
        if i >= toks_len:
            print "' requires a following word"
            return i
        name, i = self._read_tok(toks, i)
        name_upper = to_upper(name)
        word = self._lookup(name_upper)
        if word is not None:
            self.inner.push_ds_int(word.wid)
        else:
            print "' cannot find word:", name
        return i

    # ( - skip until )
    def _handle_paren_comment(self, toks, i):
        toks_len = len(toks)
        while i < toks_len:
            tok, i = self._read_tok(toks, i)
            if ')' in tok:
                break
        return i

    # COUNT ( c-addr1 -- c-addr2 u )
    def _handle_count(self):
        c_addr1 = self.inner.pop_ds_int()
        count = self.inner.cell_fetch(c_addr1)
        assert isinstance(count, W_IntObject)
        c_addr2 = c_addr1 + self.inner.cell_size_bytes
        self.inner.push_ds_int(c_addr2)
        self.inner.push_ds_int(count.intval)

    # WORD ( char "<chars>ccc<char>" -- c-addr )
    def _handle_word(self, toks, i):
        toks_len = len(toks)
        char_obj = self.inner.pop_ds_int()

        if i >= toks_len:
            word_str = ''
        else:
            word_str, i = self._read_tok(toks, i)

        length = len(word_str)
        addr = self.word_scratch_addr
        self.inner.char_store(addr, length)
        k = 0
        while k < length:
            self.inner.char_store(addr + 1 + k, ord(word_str[k]))
            k += 1
        self.inner.push_ds_int(addr)
        return i

    # STATE ( -- a-addr )
    def _handle_state(self):
        self.runtime_state()

    def runtime_state(self):
        """STATE ( -- a-addr ): refresh the dedicated state cell from the live
        compilation state and push its address. Works from interpret mode and
        from a colon body (state-smart words compiled with POSTPONE)."""
        state_val = -1 if self.state == COMPILE else 0
        self.inner.cell_store(self.state_addr, state_val)
        self.inner.push_ds_int(self.state_addr)

    def runtime_save_input(self):
        """SAVE-INPUT ( -- xn..x1 n ): save enough of the input-source state to
        allow RESTORE-INPUT to rewind. Here the only mutable position is the
        token parse cursor (>IN), so we push it plus a count of 1. brainless
        option-exists? saves, parses+finds the option name, then restores so the
        name can be re-parsed by create-option."""
        cur = self.inner.cell_fetch_int(self.to_in_addr)
        self.inner.push_ds_int(cur)
        self.inner.push_ds_int(1)

    def runtime_restore_input(self):
        """RESTORE-INPUT ( xn..x1 n -- flag ): restore the input position saved
        by SAVE-INPUT and push false (success). If the saved spec is not the one
        we produce (n != 1) push true (failure) leaving the cursor unchanged."""
        n = self.inner.pop_ds_int()
        if n != 1:
            self.inner.push_ds_int(-1)
            return
        cur = self.inner.pop_ds_int()
        self.inner.cell_store(self.to_in_addr, cur)
        self.inner.push_ds_int(0)

    # EVALUATE ( c-addr u -- )
    def _handle_evaluate(self):
        length = self.inner.pop_ds_int()
        c_addr = self.inner.pop_ds_int()
        if length < 0:
            length = 0
        buf_str = self.inner.buf_get(c_addr)
        if isinstance(buf_str, W_StringObject) and len(buf_str.strval) >= length:
            text = buf_str.strval[:length]
        else:
            chars = []
            for k in range(length):
                chars.append(chr(self.inner.char_fetch(c_addr + k)))
            text = "".join(chars)
        saved_buf = self.source_buffer
        saved_cnt = self._copy_string_counts()
        saved_toks = self.toks
        saved_cur = self.inner.cell_fetch_int(self.to_in_addr)
        self.interpret_line(text)
        self.source_buffer = saved_buf
        self.string_token_counts = saved_cnt
        self.toks = saved_toks
        self.inner.cell_store(self.to_in_addr, saved_cur)

    # >NUMBER ( ud1 c-addr1 u1 -- ud2 c-addr2 u2 )
    def _handle_to_number(self):
        """Convert string to number according to BASE."""
        u1 = self.inner.pop_ds_int()
        c_addr1 = self.inner.pop_ds_int()
        ud1_hi = self.inner.pop_ds_int()
        ud1_lo = self.inner.pop_ds_int()

        base = self.inner.base
        addr = c_addr1
        length = u1
        value = ud1_lo

        chars_processed = 0
        for j in range(length):
            ch = chr(self.inner.char_fetch(addr + j))

            digit = -1
            if '0' <= ch <= '9':
                digit = ord(ch) - ord('0')
            elif 'A' <= ch <= 'Z':
                digit = ord(ch) - ord('A') + 10
            elif 'a' <= ch <= 'z':
                digit = ord(ch) - ord('a') + 10

            if digit < 0 or digit >= base:
                break

            value = value * base + digit
            chars_processed += 1

        self.inner.push_ds_int(value)
        self.inner.push_ds_int(0)
        self.inner.push_ds_int(addr + chars_processed)
        self.inner.push_ds_int(length - chars_processed)

    # ENVIRONMENT? ( c-addr u -- false | i*x true )
    def _handle_environment_query(self):
        u = self.inner.pop_ds_int()
        c_addr = self.inner.pop_ds_int()

        buf_entry = self.inner.buf_get(c_addr)
        assert isinstance(buf_entry, W_StringObject)
        strval = buf_entry.strval
        assert 0 <= u <= len(strval)
        query = strval[:u]
        query_upper = to_upper(query)

        if query_upper == "/COUNTED-STRING":
            self.inner.push_ds_int(255)
            self.inner.push_ds_int(-1)
        elif query_upper == "/HOLD":
            self.inner.push_ds_int(128)
            self.inner.push_ds_int(-1)
        elif query_upper == "/PAD":
            self.inner.push_ds_int(256)
            self.inner.push_ds_int(-1)
        elif query_upper == "ADDRESS-UNIT-BITS":
            self.inner.push_ds_int(8)
            self.inner.push_ds_int(-1)
        elif query_upper == "CORE":
            self.inner.push_ds_int(-1)
            self.inner.push_ds_int(-1)
        elif query_upper == "CORE-EXT":
            self.inner.push_ds_int(0)
            self.inner.push_ds_int(-1)
        elif query_upper == "FLOORED":
            self.inner.push_ds_int(-1)
            self.inner.push_ds_int(-1)
        elif query_upper == "GFORTH":
            # Report as gforth so brew's system-dependent.fs takes the gforth path.
            ver = "0.7.9"
            addr = self.inner.alloc_buf(ver, len(ver))
            self.inner.push_ds_int(addr)
            self.inner.push_ds_int(len(ver))
            self.inner.push_ds_int(-1)
        elif query_upper == "MAX-CHAR":
            self.inner.push_ds_int(255)
            self.inner.push_ds_int(-1)
        elif query_upper == "MAX-N":
            self.inner.push_ds_int((1 << 62) - 1 + (1 << 62))
            self.inner.push_ds_int(-1)
        elif query_upper == "MAX-U":
            self.inner.push_ds_int(-1)
            self.inner.push_ds_int(-1)
        elif query_upper == "MAX-D":
            self.inner.push_ds_int(-1)
            self.inner.push_ds_int((1 << 62) - 1 + (1 << 62))
            self.inner.push_ds_int(-1)
        elif query_upper == "MAX-UD":
            self.inner.push_ds_int(-1)
            self.inner.push_ds_int(-1)
            self.inner.push_ds_int(-1)
        elif query_upper == "RETURN-STACK-CELLS":
            self.inner.push_ds_int(64)
            self.inner.push_ds_int(-1)
        elif query_upper == "STACK-CELLS":
            self.inner.push_ds_int(64)
            self.inner.push_ds_int(-1)
        else:
            self.inner.push_ds_int(0)

    # BASE ( -- a-addr )
    def _handle_base(self):
        """Return the address of the BASE variable ( -- a-addr ). The cell is the
        source of truth (HEX/DECIMAL/BASE! and a direct `BASE !` all write it), so
        adopt it into inner.base -- which drives number parsing -- before handing
        out the address for a following `BASE @` / `BASE !`."""
        cell_val = self.inner.cell_fetch_int(self.base_addr)
        if cell_val >= 2:
            self.inner.base = cell_val
        self.inner.push_ds_int(self.base_addr)

    # ABORT ( -- )
    def _handle_abort(self):
        self.inner.ds_ptr = 0
        self.inner.rs_ptr = 0
        self.state = INTERPRET
        print "ABORT"

    # ABORT" ( flag "ccc<quote>" -- )
    @unroll_safe
    def _handle_abort_quote(self, toks, i):
        toks_len = len(toks)
        abort_msg_parts = []
        while i < toks_len:
            tok, i = self._read_tok(toks, i)
            tok_len = len(tok)
            if tok_len > 0 and tok[tok_len - 1] == '"':
                stop = tok_len - 1
                if stop > 0:
                    abort_msg_parts.append(tok[:stop])
                break
            abort_msg_parts.append(tok)

        abort_msg = ' '.join(abort_msg_parts)
        flag = self.inner.pop_ds_int()

        if flag != 0:
            print "ABORT:", abort_msg
            self.inner.clear_ds_int()
            self.inner.reset_ds_float()
            self.inner.ds_ptr_locals = 0
            self.inner.rs_ptr = 0
            self.state = INTERPRET
            return i, True
        return i, False

    # QUIT ( -- )
    def _handle_quit(self):
        self.inner.ds_ptr = 0
        self.inner.rs_ptr = 0
        self.state = INTERPRET

    # IMMEDIATE ( -- )
    def _handle_immediate(self):
        self.runtime_immediate()

    def runtime_immediate(self):
        """Mark the most recently defined word immediate. Called both from the
        interpret-mode lexical handler and from the (IMMEDIATE) primitive that
        IMMEDIATE compiles into a colon body, so a defining word can run
        CREATE IMMEDIATE ... DOES> to build immediate children at runtime."""
        if self.last_word is not None:
            self.last_word.immediate = True
        else:
            print "IMMEDIATE: no word to mark"

    # Dispatch methods for interpret and compile modes

    def _dispatch_interpret(self, tkey, toks, i, toks_len):
        """Dispatch interpret-mode words. Returns (handled, new_i, should_return)."""
        if tkey == "2VARIABLE":
            result = self._handle_2variable(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "2CONSTANT":
            result = self._handle_2constant(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "FIELD":
            result = self._handle_field(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "VOCABULARY":
            result = self._handle_vocabulary(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "END-STRUCT":
            result = self._handle_end_struct(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "VALUE":
            result = self._handle_value(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "TO":
            result = self._handle_to(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "INCLUDE" or tkey == "REQUIRE":
            result = self._handle_include(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "INCLUDED" or tkey == "REQUIRED":
            self._handle_included()
            return True, i, False

        if tkey == "IS":
            result = self._handle_is(toks, i)
            if result < 0:
                return True, i, True
            return True, result, False

        if tkey == "FIND":
            self._handle_find()
            return True, i, False

        if tkey == "SOURCE":
            self._handle_source()
            return True, i, False

        if tkey == "(":
            i = self._handle_paren_comment(toks, i)
            return True, i, False

        if tkey == "WORD":
            i = self._handle_word(toks, i)
            return True, i, False

        if tkey == "STATE":
            self._handle_state()
            return True, i, False

        if tkey == 'ABORT"':
            i, should_return = self._handle_abort_quote(toks, i)
            return True, i, should_return

        if tkey == "QUIT":
            self._handle_quit()
            return True, i, True

        if tkey == "IMMEDIATE":
            self._handle_immediate()
            return True, i, False

        if tkey == "]":
            self.state = COMPILE
            return True, i, False

        if tkey == "BASE":
            self._handle_base()
            return True, i, False

        if tkey == ">NUMBER":
            self._handle_to_number()
            return True, i, False

        if tkey == "ENVIRONMENT?":
            self._handle_environment_query()
            return True, i, False

        # Search order / wordlists (A5)
        if tkey == "WORDLIST":
            self._handle_wordlist()
            return True, i, False
        if tkey == "GET-CURRENT":
            self._handle_get_current()
            return True, i, False
        if tkey == "SET-CURRENT":
            self._handle_set_current()
            return True, i, False
        if tkey == "FORTH-WORDLIST":
            self._handle_forth_wordlist()
            return True, i, False
        if tkey == "DEFINITIONS":
            self._handle_definitions()
            return True, i, False
        if tkey == "FORTH":
            self._handle_forth()
            return True, i, False
        if tkey == "ONLY":
            self._handle_only()
            return True, i, False
        if tkey == "ALSO":
            self._handle_also()
            return True, i, False
        if tkey == "PREVIOUS":
            self._handle_previous()
            return True, i, False
        if tkey == ">ORDER":
            self._handle_to_order()
            return True, i, False
        if tkey == "GET-ORDER":
            self._handle_get_order()
            return True, i, False
        if tkey == "SET-ORDER":
            self._handle_set_order()
            return True, i, False
        if tkey == "SEARCH-WORDLIST":
            self._handle_search_wordlist()
            return True, i, False

        return False, i, False

    def _dispatch_compile(self, tkey, toks, i, toks_len):
        """Dispatch compile-mode words. Returns (handled, new_i, should_return)."""
        if tkey == "IMMEDIATE":
            # In compile mode, emit a call that marks the runtime-defined word immediate.
            self._emit_word(self.forth_wl["(IMMEDIATE)"])
            return True, i, False

        if tkey == "IF":
            self._compile_if()
            return True, i, False

        if tkey == "ELSE":
            if not self._compile_else():
                return True, i, True
            return True, i, False

        if tkey == "THEN":
            if not self._compile_then():
                return True, i, True
            return True, i, False

        if tkey == "DO":
            self._compile_do()
            return True, i, False

        if tkey == "?DO":
            self._compile_qdo()
            return True, i, False

        if tkey == "TO":
            if i >= toks_len:
                print "TO requires a name"
                return True, i, True
            name = toks[i]
            i += 1
            key = to_upper(name)
            if key not in self.value_addrs:
                print "TO: not a VALUE"
                return True, i, True
            self._emit_lit(W_IntObject(self.value_addrs[key]))
            self._emit_word(self.forth_wl["!"])
            return True, i, False

        if tkey == "IS":
            if i >= toks_len:
                print "IS requires a name"
                return True, i, True
            name = toks[i]
            i += 1
            key = to_upper(name)
            if key not in self.defer_ids:
                print "IS: not a DEFER"
                return True, i, True
            self._emit_lit(W_IntObject(self.defer_ids[key]))
            self._emit_word(self.forth_wl["(IS!)"])
            return True, i, False

        if tkey == "LOOP":
            if not self._compile_loop():
                return True, i, True
            return True, i, False

        if tkey == "+LOOP":
            if not self._compile_plusloop():
                return True, i, True
            return True, i, False

        if tkey == "BEGIN":
            self._compile_begin()
            return True, i, False

        if tkey == "WHILE":
            if not self._compile_while():
                return True, i, True
            return True, i, False

        if tkey == "REPEAT":
            if not self._compile_repeat():
                return True, i, True
            return True, i, False

        if tkey == "AGAIN":
            if not self._compile_again():
                return True, i, True
            return True, i, False

        if tkey == "UNTIL":
            if not self._compile_until():
                return True, i, True
            return True, i, False

        if tkey == "LEAVE":
            if not self._compile_leave():
                return True, i, True
            return True, i, False

        if tkey == "CASE":
            self._compile_case()
            return True, i, False

        if tkey == "OF":
            if not self._compile_of():
                return True, i, True
            return True, i, False

        if tkey == "ENDOF":
            if not self._compile_endof():
                return True, i, True
            return True, i, False

        if tkey == "ENDCASE":
            if not self._compile_endcase():
                return True, i, True
            return True, i, False

        if tkey == "[CHAR]":
            i = self._compile_char(toks, i)
            return True, i, False

        if tkey == "RECURSIVE":
            if self.current_name:
                thread = CodeThread([], [])
                self.define_colon(self.current_name, thread)
                self.current_predefined = True
            return True, i, False

        if tkey == "RECURSE":
            if self.current_name:
                name_upper = to_upper(self.current_name)
                if name_upper in self.dict and self.current_predefined:
                    self._emit_word(self.dict[name_upper])
                else:
                    thread = CodeThread([], [])
                    word = self.define_colon(self.current_name, thread)
                    self._emit_word(word)
                    self.current_predefined = True
            else:
                print "RECURSE outside of definition"
            return True, i, False

        if tkey == 'ABORT"':
            parsed_str, i = self._parse_string_until_quote(toks, i)
            w_str = W_StringObject(parsed_str)
            self._emit_lit(w_str)
            self._emit_lit(W_IntObject(len(parsed_str)))
            self._emit_word(self.wABORTQUOTE)
            return True, i, False

        if tkey == "[":
            self.state = INTERPRET
            return True, i, False

        if tkey == "STATE":
            self._emit_word(self.forth_wl["(STATE)"])
            return True, i, False

        if tkey == "POSTPONE":
            if i >= toks_len:
                print "POSTPONE requires a following word"
                return True, i, True
            name, i = self._read_tok(toks, i)
            name_upper = to_upper(name)
            cf = cf_code_for(name_upper)
            if cf >= 0:
                # Control-flow word not in the dictionary: emit a deferred (CF) replay.
                self._emit_lit(W_IntObject(cf))
                self._emit_word(self.forth_wl["(CF)"])
                return True, i, False
            if name == ';':
                # ';' is handled lexically and not in the dictionary; emit (;) to close a runtime :NONAME.
                self._emit_word(self.forth_wl["(;)"])
                return True, i, False
            word = self._lookup(name_upper)
            if word is not None:
                if word.immediate:
                    self._emit_word(word)
                else:
                    self._emit_lit(W_IntObject(word.wid))
                    self._emit_word(self.forth_wl["(POSTPONE)"])
            else:
                print "POSTPONE: word not found:", name
            return True, i, False

        # [COMPILE] name: force-compile the next word (gforth.fs bootstrap).
        if tkey == "[COMPILE]":
            if i >= toks_len:
                print "[COMPILE] requires a following word"
                return True, i, True
            name, i = self._read_tok(toks, i)
            name_upper = to_upper(name)
            cf = cf_code_for(name_upper)
            if cf >= 0:
                self._emit_lit(W_IntObject(cf))
                self._emit_word(self.forth_wl["(CF)"])
                return True, i, False
            word = self._lookup(name_upper)
            if word is not None:
                self._emit_word(word)
            else:
                print "[COMPILE]: word not found:", name
            return True, i, False

        if tkey == "[']":
            if i >= toks_len:
                print "['] requires a following word"
                return True, i, True
            name, i = self._read_tok(toks, i)
            name_upper = to_upper(name)
            word = self._lookup(name_upper)
            if word is not None:
                self._emit_lit(W_IntObject(word.wid))
            else:
                print "['] word not found:", name
            return True, i, False

        if tkey == "DOES>":
            # Mark the DOES> body start; (DOES>) also handles the DOES>-only idiom (lexex 1darray).
            self._emit_word(self.forth_wl["(DOES>)"])
            self._emit_word(self.wEXIT)
            self.does_ip_mark = self.cc_ptr
            return True, i, False

        return False, i, False

    # main outer interpreter
    @unroll_safe
    def interpret_line(self, line):
        try:
            self._interpret_line_tokens(line)
        except Abort:
            # Reset here (outside any portal) so compiled frames never unwind over half-cleared state.
            self.inner.reset_after_abort()
            self.state = INTERPRET

    def _interpret_line_tokens(self, line):
        self.source_buffer = line
        self.source_index = 0
        self.string_token_counts = {}

        toks, self.paren_depth = split_whitespace_stateful(line, self.paren_depth)
        toks_len = len(toks)
        self.toks = toks
        i = 0
        while i < toks_len:
            t, i = self._read_tok(toks, i)

            # Conditional compilation: while skipping only [IF]/[ELSE]/[THEN] adjust nesting (spans lines).
            ckey = to_upper(t)
            if self.cond_skipping:
                if ckey == "[IF]":
                    self.cond_skip_depth += 1
                elif ckey == "[ELSE]":
                    if self.cond_skip_depth == 0 and self.cond_skip_to_else:
                        self.cond_skipping = False
                elif ckey == "[THEN]":
                    if self.cond_skip_depth == 0:
                        self.cond_skipping = False
                    else:
                        self.cond_skip_depth -= 1
                continue
            if ckey == "[IF]":
                v = self.inner.pop_ds_int()
                if v == 0:
                    self.cond_skipping = True
                    self.cond_skip_depth = 0
                    self.cond_skip_to_else = True
                continue
            if ckey == "[ELSE]":
                self.cond_skipping = True
                self.cond_skip_depth = 0
                self.cond_skip_to_else = False
                continue
            if ckey == "[THEN]":
                continue
            if ckey == "[DEFINED]" or ckey == "[UNDEFINED]":
                nm, i = self._read_tok(toks, i)
                present = self._lookup(to_upper(nm)) is not None
                if ckey == "[UNDEFINED]":
                    present = not present
                if present:
                    self.inner.push_ds_int(-1)
                else:
                    self.inner.push_ds_int(0)
                continue

            tup = to_upper(t)
            if tup == 'S"' or tup == 'C"':
                i = self._handle_s_quote(toks, i, t)
                continue

            if tup == '."':
                i = self._handle_dot_quote(toks, i, t)
                continue

            if tup == '.(':
                i = self._handle_dot_paren(toks, i, t)
                continue

            if tup == "CHAR" and self.state == INTERPRET:
                s, i = self._read_tok(toks, i)
                if len(s) > 0:
                    self.inner.push_ds_int(ord(s[0]))
                else:
                    self.inner.push_ds_int(0)
                continue

            # :INLINE (fcp): treat as plain ':'; stub in dict so fcp's [UNDEFINED] guard still fires.
            if tup == ":INLINE":
                if not self.has_nextname and i >= toks_len:
                    print ":inline requires a name"
                    return
                self.current_name, i = self._take_defining_name(toks, i)
                self.noname_mode = False
                self.state = COMPILE
                self.does_ip_mark = -1
                self.current_predefined = False
                self.reset_code()
                continue

            # :NONAME in compile mode emits a runtime-definition call (lexex setOutputFile).
            if self.state == COMPILE and to_upper(t) == ":NONAME":
                self._emit_word(self.forth_wl["(:NONAME)"])
                continue

            # ':' in compile mode opens a new named definition at RUN time (brew's basics.fs offset words).
            if t == ':' and self.state == COMPILE:
                self._emit_word(self.forth_wl["(:)"])
                continue

            if t == ':' or to_upper(t) == ":NONAME":
                if to_upper(t) == ":NONAME":
                    self.current_name = "NONAME"
                    self.noname_mode = True
                else:
                    if not self.has_nextname and i >= toks_len:
                        print ": requires a name"
                        return
                    self.current_name, i = self._take_defining_name(toks, i)
                    self.noname_mode = to_upper(self.current_name) == "NONAME"
                self.state = COMPILE
                self.does_ip_mark = -1
                self.current_predefined = False
                self.reset_code()
                continue

            if t == ';':
                if self.state != COMPILE:
                    print "; outside definition"
                    continue
                self._finalize_definition()
                continue

            tkey = to_upper(t)

            if self.state == INTERPRET:
                handled, i, should_return = self._dispatch_interpret(tkey, toks, i, toks_len)
                if handled:
                    if should_return:
                        return
                    continue

            if self.state == COMPILE:
                handled, i, should_return = self._dispatch_compile(tkey, toks, i, toks_len)
                if handled:
                    if should_return:
                        return
                    continue

            # Default: look up and execute/compile; order_is_default keeps the common case on the fast path.
            if self.order_is_default:
                w = self.dict.get(tkey, None)
            else:
                w = self._lookup(tkey)
            w = promote(w)
            if self.state == INTERPRET:
                self.inner.cell_store(self.to_in_addr, i)
                self._execute_or_push(w, t)
                i = self.inner.cell_fetch_int(self.to_in_addr)
                if self.toks is not toks:
                    toks = self.toks
                    toks_len = len(toks)
            elif self.state == COMPILE:
                # Keep parse cursor in sync; immediate words may parse (brainless's [DEF?] runs BL WORD FIND).
                self.inner.cell_store(self.to_in_addr, i)
                self._compile_word_or_literal(w, t)
                i = self.inner.cell_fetch_int(self.to_in_addr)
                # A PARSE call may have re-tokenized self.toks; adopt it.
                if self.toks is not toks:
                    toks = self.toks
                    toks_len = len(toks)
            else:
                assert 0, "unreachable state"

    def _rebase_branch_targets(self, dcode, dlits, mark):
        """Subtract `mark` from the target literal of every branch/loop word in a
        carved DOES> body, so absolute thread indices stay valid after slicing."""
        for idx in range(len(dcode)):
            w = dcode[idx]
            if (w is self.wBR or w is self.w0BR or w is self.wLOOP
                    or w is self.wLOOPNP or w is self.wPLUSLOOP or w is self.wQDO):
                lit = dlits[idx]
                if isinstance(lit, W_IntObject):
                    new_target = lit.intval - mark
                    assert new_target >= 0
                    dlits[idx] = W_IntObject(new_target)

    def _finalize_definition(self):
        tail_call_applied = False
        if self.cc_ptr > 0 and self.does_ip_mark < 0:
            last_word = self.current_code[self.cc_ptr - 1]
            if last_word is not None and last_word.prim is None and last_word.thread is not None:
                # Look TAILCALL up in forth_wl (not current_wl: gc.fs compiles while garbage-collector is current).
                wTAILCALL = self.forth_wl.get("TAILCALL", None)
                if wTAILCALL is not None:
                    self.cc_ptr -= 1
                    self.lit_ptr -= 1
                    self.push_code(wTAILCALL)
                    self.push_lit(W_WordObject(last_word))
                    tail_call_applied = True

        if not tail_call_applied:
            self._emit_word(self.wEXIT)

        code = [self.current_code[idx] for idx in range(self.cc_ptr)]
        lits = [self.current_lits[idx] for idx in range(self.lit_ptr)]

        does_word = None
        if self.does_ip_mark >= 0:
            mark = self.does_ip_mark
            assert mark >= 0
            dlen = len(code) - mark
            dcode = [None] * dlen
            dlits = [ZERO] * dlen
            j = 0
            while j < dlen:
                w = code[mark + j]
                cl = lits[mark + j]
                # Branch-target literals are absolute parent indices; rebase onto the carved body.
                if self._is_branch_target_word(w) or w is self.wLEAVE:
                    assert isinstance(cl, W_IntObject)
                    cl = W_IntObject(cl.intval - mark)
                dcode[j] = w
                dlits[j] = cl
                j += 1
            does_thread = CodeThread(dcode, dlits)
            does_word = Word("", prim=None, immediate=False, thread=does_thread)
            self.does_ip_mark = -1

        if self.noname_mode:
            thread = CodeThread(code, lits)
            thread.does_word = does_word
            self.inner.push_ds_int(Word("", thread=thread).wid)
            self.noname_mode = False
        else:
            name_upper = to_upper(self.current_name)
            if self.current_predefined and name_upper in self.dict:
                existing_word = self.dict[name_upper]
                if does_word is None:
                    # DOES> body sits past create-time code; splicing would tear it, so stay uninlined.
                    code, lits = self._inline_self_calls(code, lits, existing_word)
                thread = CodeThread(code, lits)
                thread.does_word = does_word
                existing_word.thread = thread
                self.last_word = existing_word
            else:
                thread = CodeThread(code, lits)
                thread.does_word = does_word
                self.define_colon(self.current_name, thread)

        self.state = INTERPRET
        self.current_name = ''
        self.reset_code()

    def _is_branch_target_word(self, w):
        """True for words whose literal is an instruction index in the current
        thread (must be relocated when code is spliced)."""
        return (w is self.wBR or w is self.w0BR or w is self.wQDO or
                w is self.wLOOP or w is self.wLOOPNP or w is self.wPLUSLOOP or
                w is self.wLEAVE)

    def _inline_self_calls(self, code, lits, selfword):
        """Splice one copy of the body into each non-tail self-call site, so a
        recursive word does two levels of work per interpreter call frame. The
        copy's EXITs become branches past the copy; its self-calls stay real
        calls (which re-enter the inlined thread). Branch-target literals are
        relocated on both the original code and the copies."""
        n = len(code)
        if n == 0 or n > 48:
            return code, lits
        sites = 0
        i = 0
        while i < n:
            if code[i] is selfword:
                sites += 1
            i += 1
        # Cap expansion: many-site words with large bodies explode trace length and run slower inlined.
        if sites == 0 or sites > 4 or n * (sites + 1) > 96:
            return code, lits
        wTAILCALL = self.forth_wl.get("TAILCALL", None)
        # Pass 1: map every original instruction index (including one-past-end, a valid branch target) to its new position.
        newpos = [0] * (n + 1)
        p = 0
        i = 0
        while i < n:
            newpos[i] = p
            if code[i] is selfword:
                p += n
            else:
                p += 1
            i += 1
        newpos[n] = p
        # Pass 2: emit into pre-sized lists, relocating branch targets.
        total = newpos[n]
        newcode = [None] * total
        newlits = [ZERO] * total
        out = 0
        i = 0
        while i < n:
            w = code[i]
            if w is selfword:
                base = newpos[i]
                copy_end = base + n
                j = 0
                while j < n:
                    cw = code[j]
                    cl = lits[j]
                    if cw is self.wEXIT:
                        newcode[out] = self.wBR
                        newlits[out] = W_IntObject(copy_end)
                    elif wTAILCALL is not None and cw is wTAILCALL:
                        assert isinstance(cl, W_WordObject)
                        if cl.word is selfword:
                            newcode[out] = self.wBR
                            newlits[out] = W_IntObject(base)
                        else:
                            # Mid-thread TAILCALL would misread its literal; demote to plain call.
                            newcode[out] = cl.word
                            newlits[out] = ZERO
                    elif self._is_branch_target_word(cw):
                        assert isinstance(cl, W_IntObject)
                        newcode[out] = cw
                        newlits[out] = W_IntObject(base + cl.intval)
                    else:
                        newcode[out] = cw
                        newlits[out] = cl
                    j += 1
                    out += 1
            elif self._is_branch_target_word(w):
                cl = lits[i]
                assert isinstance(cl, W_IntObject)
                target = cl.intval
                assert 0 <= target <= n
                newcode[out] = w
                newlits[out] = W_IntObject(newpos[target])
                out += 1
            else:
                newcode[out] = w
                newlits[out] = lits[i]
                out += 1
            i += 1
        assert out == total
        return newcode, newlits
