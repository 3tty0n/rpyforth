from rpython.rlib.jit import unroll_safe


def _is_space(ch):
    return (ch == ' ' or ch == '\t' or ch == '\n' or ch == '\r' or
            ch == '\v' or ch == '\f')


# Words opening a '"'-terminated string literal; '\' and '(' inside are content, not comments.
_QUOTE_STRING_WORDS = {
    'S"': True, 'C"': True, '."': True, 'ABORT"': True, 'S\\"': True,
}


def _delim_ahead(line, we, delim):
    """True if the closing delimiter appears at/after position we. we is the end
    of the opening word (the following char is the single delimiter space)."""
    j = we
    n = len(line)
    while j < n:
        if line[j] == delim:
            return True
        j += 1
    return False


def _quote_word_upper(word):
    out = ''
    for i in range(len(word)):
        o = ord(word[i])
        if o >= 97 and o <= 122:
            out += chr(o - 32)
        else:
            out += word[i]
    return out


def _prev_word_is_postpone(result):
    """True if the last whitespace-delimited word emitted so far is POSTPONE or
    [COMPILE] (case-insensitive). Used so `POSTPONE (` names the comment word
    rather than opening a comment. Builds the last word char by char to stay
    clear of RPython's non-negative-slice-bound requirement."""
    n = len(result)
    j = n - 1
    while j >= 0 and _is_space(result[j]):
        j -= 1
    # result[k] for k in (j .. first space below j] is the last word, reversed.
    word = ''
    while j >= 0 and not _is_space(result[j]):
        ch = result[j]
        o = ord(ch)
        if o >= 97 and o <= 122:
            ch = chr(o - 32)
        word = ch + word
        j -= 1
    return word == 'POSTPONE' or word == '[COMPILE]'


@unroll_safe
def to_upper(s):
    out = ''
    for i in range(len(s)):
        o = ord(s[i])
        if o >= 97 and o <= 122: # a-z
            out += chr(o - 32)
        else:
            out += s[i]
    return out

@unroll_safe
def remove_comments_stateful(line, depth):
    """Remove Forth comments from a line, carrying paren-comment nesting depth
    across lines. Handles `\\` (backslash to end of line) and `( ... )`, which may
    span INCLUDEd lines (gforth behaviour); ``depth`` is the number of open '('
    comments inherited from earlier lines. Returns (result, out_depth)."""
    result = ''
    i = 0
    n = len(line)
    if depth > 0:
        # ANS ( does not nest: first ')' closes the comment; consume until then.
        while i < n and depth > 0:
            if line[i] == ')':
                depth = 0
            i += 1
        if depth > 0:
            return result, depth

    while i < n:
        ch = line[i]
        at_word_start = i == 0 or _is_space(line[i-1])

        # At a word boundary: if the word opens a string literal (S" ." ABORT" .() copy verbatim to the closing delimiter.
        if at_word_start and not _is_space(ch):
            we = i
            while we < n and not _is_space(line[we]):
                we += 1
            word = line[i:we]
            wu = _quote_word_upper(word)
            if wu == '.(' and _delim_ahead(line, we, ')'):
                # Copy the word, then everything up to and including the ')'.
                while i < n and line[i] != ')':
                    result += line[i]
                    i += 1
                if i < n:
                    result += line[i]
                    i += 1
                continue
            # Only treat S" ." ABORT" as a string opener when a closing '"' follows on this line; otherwise it's a plain token (e.g. `symbol ."`).
            if wu in _QUOTE_STRING_WORDS and _delim_ahead(line, we, '"'):
                j = we
                result += word
                if j < n:  # the single delimiter space after the word
                    result += line[j]
                    j += 1
                while j < n and line[j] != '"':
                    result += line[j]
                    j += 1
                if j < n:
                    result += line[j]
                    j += 1
                i = j
                continue

        if ch == '\\':
            if at_word_start:
                break

        # '(' opens a comment only when it is its own word (space/start before, space/end after); paren-named words like (checkTime) are left intact.
        if ch == '(':
            before_ok = at_word_start
            after_ok = i + 1 >= n or line[i+1] in ' \t\n\r\v\f'
            # POSTPONE ( names the comment word rather than opening a comment (gforth compat/assert.fs).
            if before_ok and after_ok and _prev_word_is_postpone(result):
                result += ch
                i += 1
                continue
            if before_ok and after_ok:
                # ANS ( does not nest: scan to the first ')'.
                i += 1
                depth = 1
                while i < n and depth > 0:
                    if line[i] == ')':
                        depth = 0
                    i += 1
                if depth > 0:
                    # Unterminated: comment continues onto the next line (only meaningful inside an INCLUDEd file).
                    return result, depth
                continue

        result += ch
        i += 1

    return result, 0

@unroll_safe
def remove_comments(line):
    """Remove Forth comments from a single line (no cross-line paren state)."""
    result, _ = remove_comments_stateful(line, 0)
    return result

@unroll_safe
def split_whitespace_stateful(line, depth):
    """Split line into tokens, carrying paren-comment depth across lines.

    Returns (tokens, out_depth)."""
    line, depth = remove_comments_stateful(line, depth)
    res = _tokenize(line)
    return res, depth

@unroll_safe
def split_whitespace(line):
    """Split line into tokens, removing Forth comments first."""
    line = remove_comments(line)
    return _tokenize(line)

@unroll_safe
def _tokenize(line):

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
        cur += ch
    if cur != '':
        res.append(cur)
    return res


def digit_to_char(d):
    if d < 10:
        return chr(ord('0') + d)
    else:
        return chr(ord('A') + (d - 10))
