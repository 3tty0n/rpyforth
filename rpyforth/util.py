def to_upper(s):
    out = ''
    for i in range(len(s)):
        o = ord(s[i])
        if o >= 97 and o <= 122: # a-z
            out += chr(o - 32)
        else:
            out += s[i]
    return out

def remove_comments(line):
    """Remove Forth comments from a line.

    Handles two types:
    - \ comment: backslash to end of line
    - ( comment ): parenthetical comment

    Returns the line with comments removed.
    """
    result = ''
    i = 0
    while i < len(line):
        ch = line[i]

        # Handle backslash comment - rest of line is ignored
        if ch == '\\':
            # Check if it's actually a backslash comment (needs space before or at start)
            if i == 0 or line[i-1] in ' \t\n\r\v\f':
                break  # Skip rest of line

        # Handle parenthetical comment
        if ch == '(':
            # Check if it's actually a comment (needs space before or at start)
            if i == 0 or line[i-1] in ' \t\n\r\v\f':
                # Find matching ) with nesting support
                i += 1
                depth = 1
                while i < len(line) and depth > 0:
                    if line[i] == '(':
                        depth += 1
                    elif line[i] == ')':
                        depth -= 1
                    i += 1
                continue

        result += ch
        i += 1

    return result

def split_whitespace(line):
    """Split line into tokens, removing Forth comments first."""
    # Remove comments before tokenization
    line = remove_comments(line)

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


def digit_to_char(d):
    if d < 10:
        return chr(ord('0') + d)
    else:
        return chr(ord('A') + (d - 10))
