class Word(object):
    """
    Dictionary entry for a Forth word.
    """
    def __init__(self, name, prim=None, immediate=False, thread=None):
        self.name = name
        self.prim = prim # callable(vm) or None
        self.immediate = immediate # bool
        self.thread = thread # code thread

    def is_primitive(self):
        return self.prim is not None


class CodeThread(object):
    def __init__(self, code, lits):
        self.code = code # code
        self.lits = lits # literal values used by code[i]


class W_Object(object):
    "abstract representation of an inner object"

    def add(self, other):
        raise NotImplementedError

    def sub(self, other):
        raise NotImplementedError

    def mul(self, other):
        raise NotImplementedError

    def div(self, other):
        raise NotImplementedError


class W_IntObject(W_Object):
    def __init__(self, intval):
        self.intval = intval

    def __repr__(self):
        return str(self.intval)

    def add(self, other):
        return W_IntObject(self.intval + other.intval)

    def sub(self, other):
        return W_IntObject(self.intval - other.intval)

    def mul(self, other):
        return W_IntObject(self.intval * other.intval)

    def div(self, other):
        return W_IntObject(self.intval // other.intval)

    def neg(self):
        return W_IntObject(-self.intval)

    def abs(self):
        return W_IntObject(abs(self.intval))

    def lt(self, other):
        if isinstance(other, W_IntObject):
            return self.intval < other.intval
        else:
            assert 0

    def mod(self, other):
        return W_IntObject(self.intval % other.intval)
    
    def inc(self):
        return W_IntObject(self.intval + 1)
    
    def dec(self):
        return W_IntObject(self.intval - 1)

class W_StringObject(W_Object):
    def __init__(self, strval):
        self.strval = strval

    def __repr__(self):
        return str(self.strval)

ZERO = W_IntObject(0)

# BASE
HEX     = W_IntObject(16)
DECIMAL = W_IntObject(10)
OCTAL   = W_IntObject(8)
BINARY  = W_IntObject(2)