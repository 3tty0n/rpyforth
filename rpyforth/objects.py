try:
    from rpython.rlib.rarithmetic import LONG_BIT
except ImportError:
    import struct
    LONG_BIT = struct.calcsize("P") * 8


class Word(object):
    """
    Dictionary entry for a Forth word.
    """
    _immutable_fields_ = ['name', 'prim', 'immediate', 'thread']

    def __init__(self, name, prim=None, immediate=False, thread=None):
        self.name = name
        self.prim = prim # callable(vm) or None
        self.immediate = immediate # bool
        self.thread = thread # code thread

    def is_primitive(self):
        return self.prim is not None

    def __repr__(self):
        return "<Word %s>" % (self.name)


class CodeThread(object):
    _immutable_fields_ = ["code[*]", "lits[*]"]

    def __init__(self, code, lits):
        self.code = code # code (list of Words)
        self.lits = lits # literal values used by code[i]


class W_Object(object):
    _immutable_fields_ = ['intval', 'floatval', 'strval', 'ptrval'] # OK??

    def __init__(self):
        pass

    def add(self, other):
        raise NotImplementedError

    def sub(self, other):
        raise NotImplementedError

    def mul(self, other):
        raise NotImplementedError

    def div(self, other):
        raise NotImplementedError


class W_IntObject(W_Object):
    _immutable_fields_ = ['intval']

    def __init__(self, intval):
        W_Object.__init__(self)
        self.intval = intval

    def __repr__(self):
        return self.to_string()

    def to_string(self):
        return str(self.intval)

    def is_true(self):
        return self.intval == -1

    def zero_less(self):
        return self.intval < 0

    def zero_greater(self):
        return self.intval > 0

    def zero_equal(self):
        return self.intval == 0

    def add(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval + other.intval)

    def sub(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval - other.intval)

    def mul(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval * other.intval)

    def div(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval // other.intval)

    def neg(self):
        return W_IntObject(-self.intval)

    def abs(self):
        return W_IntObject(abs(self.intval))

    def lt(self, other):
        assert isinstance(other, W_IntObject)
        return self.intval < other.intval

    def gt(self, other):
        assert isinstance(other, W_IntObject)
        return self.intval > other.intval

    def mod(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval % other.intval)

    def inc(self):
        return W_IntObject(self.intval + 1)

    def dec(self):
        return W_IntObject(self.intval - 1)

    def eq(self, other):
        if isinstance(other, W_IntObject):
            return self.intval == other.intval
        return False

class W_PtrObject(W_Object):
    _immutable_fields_ = ['ptrval']

    def __init__(self, ptrval):
        W_Object.__init__(self)
        self.ptrval = ptrval

    def __repr__(self):
        return self.to_string()

    def to_string(self):
        return "<Ptr %d>" % (self.ptrval)

class W_StringObject(W_Object):
    _immutable_fields_ = ['strval']

    def __init__(self, strval):
        W_Object.__init__(self)
        self.strval = strval

    def __repr__(self):
        return self.to_string()

    def to_string(self):
        return self.strval

class W_FloatObject(W_Object):
    _immutable_fields_ = ['floatval']

    def __init__(self, floatval):
        W_Object.__init__(self)
        self.floatval = floatval

    def __repr__(self):
        return self.to_string()

    def to_string(self):
        return str(self.floatval)

    def add(self, other):
        assert isinstance(other, W_FloatObject)
        return W_FloatObject(self.floatval + other.floatval)

    def sub(self, other):
        assert isinstance(other, W_FloatObject)
        return W_FloatObject(self.floatval - other.floatval)

    def mul(self, other):
        assert isinstance(other, W_FloatObject)
        return W_FloatObject(self.floatval * other.floatval)

    def div(self, other):
        assert isinstance(other, W_FloatObject)
        return W_FloatObject(self.floatval / other.floatval)

    def gt(self, other):
        assert isinstance(other, W_FloatObject)
        return self.floatval > other.floatval

ZERO = W_IntObject(0)
TRUE = W_IntObject(-1)

# BASE
HEX     = W_IntObject(16)
DECIMAL = W_IntObject(10)
OCTAL   = W_IntObject(8)
BINARY  = W_IntObject(2)

# data space characteristics
CELL_SIZE_BYTES = LONG_BIT // 8
CELL_SIZE = W_IntObject(CELL_SIZE_BYTES)
