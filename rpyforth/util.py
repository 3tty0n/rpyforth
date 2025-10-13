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


def digit_to_char(d):
    if d < 10:
        return chr(ord('0') + d)
    else:
        return chr(ord('A') + (d - 10))
