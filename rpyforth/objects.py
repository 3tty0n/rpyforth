try:
    from rpython.rlib.rarithmetic import LONG_BIT
except ImportError:
    import struct
    LONG_BIT = struct.calcsize("P") * 8

from rpython.rlib.jit import elidable


class ForthException(Exception):
    """Raised by THROW with a non-zero code; caught by the nearest CATCH."""

    def __init__(self, code):
        self.code = code


class WordRegistry(object):
    """Maps a dense int id -> Word so an execution token can be a plain integer
    on the data stack. Write-once-per-slot; a prebuilt singleton."""

    _immutable_fields_ = ["words"]

    def __init__(self):
        self.words = [None] * MAX_WORDS
        self.count = 0

    def register(self, w):
        wid = self.count
        assert wid < MAX_WORDS
        self.words[wid] = w
        self.count = wid + 1
        return wid


MAX_WORDS = 262144
WORD_REGISTRY = WordRegistry()


@elidable
def word_from_wid(wid):
    return WORD_REGISTRY.words[wid]


class Word(object):
    """
    Dictionary entry for a Forth word.
    """
    _immutable_fields_ = ['name', 'prim', 'thread?']

    def __init__(self, name, prim=None, immediate=False, thread=None):
        self.name = name
        self.prim = prim # callable(vm) or None
        self.immediate = immediate # mutable for IMMEDIATE
        self.thread = thread # mutable for RECURSIVE
        self.does_ip = -1  # DOES> instruction pointer (-1 means not set)
        # Integer execution token: stable id so an xt can live on the data stack and be stored/fetched with ! @ ,; WORD_REGISTRY maps it back for EXECUTE.
        self.wid = WORD_REGISTRY.register(self)
        # Single-instruction thread built on first use; reused so EXECUTE/CATCH doesn't allocate a fresh thread (and register id) on every call.
        self.now_thread = None

    @elidable
    def is_primitive(self):
        return self.prim is not None

    def __repr__(self):
        return "<Word %s>" % (self.name)

    @elidable
    def to_string(self):
        return "<Word %s>" % (self.name)


MAX_THREADS = 65536


class ThreadRegistry(object):
    """Maps a dense int id -> CodeThread"""

    _immutable_fields_ = ["threads[*]"]

    def __init__(self):
        self.threads = [None] * MAX_THREADS
        self.count = 0

    def register(self, thread):
        tid = self.count
        assert tid < MAX_THREADS
        self.threads[tid] = thread
        self.count = tid + 1
        return tid


THREAD_REGISTRY = ThreadRegistry()


class CodeThread(object):
    _immutable_fields_ = ["code[*]", "lits[*]", "tid"]

    def __init__(self, code, lits):
        self.code = code     # list of Words
        self.lits = lits     # literal values used by code[i]
        self.tid = THREAD_REGISTRY.register(self)
        self.does_word = None


class DeferredCodeThread(CodeThread):
    """Thread owned by one DEFER word, with a directly accessible action."""

    def __init__(self, code, lits):
        CodeThread.__init__(self, code, lits)
        self.deferred_word = None

    def set_deferred_word(self, word):
        self.deferred_word = word


class W_Object(object):
    _attrs_ = []

    def __init__(self):
        pass

    @elidable
    def getvalue(self):
        raise NotImplementedError

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

    @elidable
    def getvalue(self):
        return self.intval

    def __repr__(self):
        return self.to_string()

    @elidable
    def to_string(self):
        return 'W_IntObject(%s)' % str(self.intval)

    @elidable
    def is_true(self):
        return self.intval == -1

    @elidable
    def zero_less(self):
        return self.intval < 0

    @elidable
    def zero_greater(self):
        return self.intval > 0

    @elidable
    def zero_equal(self):
        return self.intval == 0

    @elidable
    def add(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval + other.intval)

    @elidable
    def sub(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval - other.intval)

    @elidable
    def mul(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval * other.intval)

    @elidable
    def div(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval // other.intval)

    @elidable
    def neg(self):
        return W_IntObject(-self.intval)

    @elidable
    def abs(self):
        return W_IntObject(abs(self.intval))

    @elidable
    def lt(self, other):
        assert isinstance(other, W_IntObject)
        return self.intval < other.intval

    @elidable
    def gt(self, other):
        assert isinstance(other, W_IntObject)
        return self.intval > other.intval

    @elidable
    def mod(self, other):
        assert isinstance(other, W_IntObject)
        return W_IntObject(self.intval % other.intval)

    @elidable
    def inc(self):
        return W_IntObject(self.intval + 1)

    @elidable
    def dec(self):
        return W_IntObject(self.intval - 1)

    @elidable
    def eq(self, other):
        if isinstance(other, W_IntObject):
            return self.intval == other.intval
        return False

    @elidable
    def rshift(self, other):
        return W_IntObject(self.intval >> other.intval)

    @elidable
    def lshift(self, other):
        return W_IntObject(self.intval << other.intval)

    @elidable
    def s_to_d(self):
        if self.intval >= 0:
            return W_IntObject(0)
        else:
            return W_IntObject(-1)

class W_PtrObject(W_Object):
    _immutable_fields_ = ['ptrval']

    def __init__(self, ptrval):
        W_Object.__init__(self)
        self.ptrval = ptrval

    def __repr__(self):
        return self.to_string()

    @elidable
    def getvalue(self):
        return self.ptrval

    @elidable
    def to_string(self):
        return "<Ptr %d>" % (self.ptrval)

    @elidable
    def add(self, other):
        assert isinstance(other, W_PtrObject)
        return W_PtrObject(self.ptrval + other.ptrval)

    @elidable
    def sub(self, other):
        assert isinstance(other, W_PtrObject)
        return W_PtrObject(self.ptrval - other.ptrval)

class W_StringObject(W_Object):
    _immutable_fields_ = ['strval']

    def __init__(self, strval):
        W_Object.__init__(self)
        self.strval = strval

    def __repr__(self):
        return self.to_string()

    @elidable
    def getvalue(self):
        return self.strval

    @elidable
    def to_string(self):
        return self.strval

class W_FloatObject(W_Object):
    _immutable_fields_ = ['floatval']

    def __init__(self, floatval):
        W_Object.__init__(self)
        self.floatval = floatval

    def __repr__(self):
        return self.to_string()

    @elidable
    def getvalue(self):
        return self.floatval

    @elidable
    def to_string(self):
        return str(self.floatval)

    @elidable
    def add(self, other):
        assert isinstance(other, W_FloatObject)
        return W_FloatObject(self.floatval + other.floatval)

    @elidable
    def sub(self, other):
        assert isinstance(other, W_FloatObject)
        return W_FloatObject(self.floatval - other.floatval)

    @elidable
    def mul(self, other):
        assert isinstance(other, W_FloatObject)
        return W_FloatObject(self.floatval * other.floatval)

    @elidable
    def div(self, other):
        assert isinstance(other, W_FloatObject)
        return W_FloatObject(self.floatval / other.floatval)

    @elidable
    def gt(self, other):
        assert isinstance(other, W_FloatObject)
        return self.floatval > other.floatval


class W_WordObject(W_Object):
    """Wrapper for Word objects to use as execution tokens."""
    _immutable_fields_ = ['word']

    def __init__(self, word):
        W_Object.__init__(self)
        self.word = word

    def __repr__(self):
        return self.to_string()

    @elidable
    def to_string(self):
        return "<XT:%s>" % self.word.name

    @elidable
    def getvalue(self):
        return self.word


ZERO = W_IntObject(0)
TRUE = W_IntObject(-1)

HEX     = W_IntObject(16)
DECIMAL = W_IntObject(10)
OCTAL   = W_IntObject(8)
BINARY  = W_IntObject(2)

SMALL_INT_MIN = -128
SMALL_INT_MAX = 2048  # Extended range for loop counters (covers 0-1000 loops)

_small_int_cache = [W_IntObject(i) for i in range(SMALL_INT_MIN, SMALL_INT_MAX)]

@elidable
def make_int(val):
    """Get a cached W_IntObject for small integers, or create a new one."""
    if SMALL_INT_MIN <= val < SMALL_INT_MAX:
        return _small_int_cache[val - SMALL_INT_MIN]
    return W_IntObject(val)

CELL_SIZE_BYTES = 8  # byte-addressed heap with 64-bit little-endian cells
CELL_SIZE = W_IntObject(CELL_SIZE_BYTES)
