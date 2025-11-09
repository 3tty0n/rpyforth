from rpyforth.objects import (
    BINARY,
    OCTAL,
    DECIMAL,
    HEX,
    TRUE,
    ZERO,
    W_IntObject,
    W_StringObject,
    CELL_SIZE,
)
from rpyforth.util import digit_to_char


# 0= ( x -- flag )
def prim_ZEROEQUAL(inner):
    """GForth core 2012: flag is true when x equals zero."""
    w_x = inner.pop_ds()
    if w_x.zero_equal():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)


# 0< ( n -- flag )
def prim_ZEROLESS(inner):
    """GForth core 2012: flag is true when n is strictly negative."""
    w_x = inner.pop_ds()
    if w_x.zero_less():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)


# 0> ( n -- flag )
def prim_ZEROGREATER(inner):
    """GForth core 2012: flag is true when n is strictly positive."""
    w_x = inner.pop_ds()
    if w_x.zero_greater():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)


# 0<> ( n -- flag )
def prim_ZERONOTEQUAL(inner):
    """GForth core 2012: flag is true when n is non-zero."""
    w_x = inner.pop_ds()
    if not w_x.zero_equal():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)


# DUP ( x -- x x )
def prim_DUP(inner):
    """GForth core 2012: duplicate x, leaving two copies on the stack."""
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(a)


# 2DUP ( x1 x2 -- x1 x2 x1 x2 )
def prim_2DUP(inner):
    """GForth core 2012: duplicate cell pair x1 x2,  leaving four copies on the stack. """
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(b)
    inner.push_ds(a)
    inner.push_ds(b)


# ?DUP ( x -- 0 | x x )
def prim_QUESTIONDUP(inner):
    """GForth core 2012: duplicate x if it is non-zero."""
    a = inner.pop_ds()
    inner.push_ds(a)
    if not a.zero_equal():
        inner.push_ds(a)


# DROP ( x -- )
def prim_DROP(inner):
    """GForth core 2012: discard the top stack item."""
    inner.pop_ds()


# 2DROP ( x1 x2 -- )
def prim_2DROP(inner):
    """GForth core 2012: discard the top two stack items."""
    inner.pop_ds()
    inner.pop_ds()


# SWAP ( x1 x2 -- x2 x1 )
def prim_SWAP(inner):
    """GForth core 2012: exchange the top two stack items."""
    a, b = inner.top2_ds()
    inner.push_ds(b)
    inner.push_ds(a)


# 2SWAP ( x1 x2 x3 x4 -- x3 x4 x1 x2 )
def prim_2SWAP(inner):
    """GForth core 2012: exchange the top two cell pairs."""
    c, d = inner.top2_ds()
    a, b = inner.top2_ds()
    inner.push_ds(c)
    inner.push_ds(d)
    inner.push_ds(a)
    inner.push_ds(b)


# OVER ( x1 x2 -- x1 x2 x1 )
def prim_OVER(inner):
    """GForth core 2012: copy the second stack item to the top."""
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(b)
    inner.push_ds(a)


# 2OVER ( x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2 )
def prim_2OVER(inner):
    """GForth core 2012: copy cell pair x1 x2 to the top of the stack."""
    d = inner.pop_ds()
    c = inner.pop_ds()
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(b)
    inner.push_ds(c)
    inner.push_ds(d)
    inner.push_ds(a)
    inner.push_ds(b)


# ROT ( x1 x2 x3 -- x2 x3 x1 )
def prim_ROT(inner):
    """GForth core 2012: rotate the top three stack entries."""
    c = inner.pop_ds()
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(b)
    inner.push_ds(c)
    inner.push_ds(a)

# MAX ( n1 n2 -- n3 )
def prim_MAX(inner):
    """GForth core 2012: n3 is the greater of n1 and n2."""
    a, b = inner.top2_ds()
    if a.lt(b):
        inner.push_ds(b)
    else:
        inner.push_ds(a)

# MIN ( n1 n2 -- n3 )
def prim_MIN(inner):
    """GForth core 2012: n3 is the lesser of n1 and n2."""
    a, b = inner.top2_ds()
    if a.lt(b):
        inner.push_ds(a)
    else:
        inner.push_ds(b)


# DEPTH ( -- +n )
def prim_DEPTH(inner):
    """GForth core 2012: +n is the number of single-cell values contained in the data stack."""
    inner.push_ds(W_IntObject(inner.ds_ptr))


# RSHIFT ( n1 u -- n2 )
def prim_RSHIFT(inner):
    """GForth core 2012: perform a logical right shift of u bit-places on n1, giving n2."""
    a = inner.pop_ds()
    b = inner.pop_ds()
    inner.push_ds(b.rshift(a))


# LSHIFT ( n1 u -- n2 )
def prim_LSHIFT(inner):
    """GForth core 2012: perform a logical left shift of u bit-places on n1, giving n2."""
    a = inner.pop_ds()
    b = inner.pop_ds()
    inner.push_ds(b.lshift(a))

# S>D ( n -- d )
def prim_S_TO_D(inner):
    """GForth core 2012: convert tne number n to double-cell number d."""
    a = inner.pop_ds()
    inner.push_ds(a)  
    inner.push_ds(a.s_to_d())  

# Arithmetic


# + ( n1 n2 -- n3 )
def prim_ADD(inner):
    """GForth core 2012: add n1 and n2, leaving their sum."""
    a, b = inner.top2_ds()
    inner.push_ds(a.add(b))


# - ( n1 n2 -- n3 )
def prim_SUB(inner):
    """GForth core 2012: subtract n2 from n1, leaving the difference."""
    a, b = inner.top2_ds()
    inner.push_ds(a.sub(b))


# * ( n1 n2 -- n3 )
def prim_MUL(inner):
    """GForth core 2012: multiply n1 by n2, leaving the product."""
    a, b = inner.top2_ds()
    inner.push_ds(a.mul(b))

# ABS ( n -- u )
def prim_ABS(inner):
    """GForth core 2012: u is the absolute value of n."""
    a = inner.pop_ds()
    inner.push_ds(a.abs())


# NEGATE ( n1 -- n2 )
def prim_NEGATE(inner):
    """GForth core 2012: negate n1, giving its arithmetic inverse n2."""
    a = inner.pop_ds()
    inner.push_ds(a.neg())


# MOD ( n1 n2 -- n3 )
def prim_MOD(inner):
    """GForth core 2012: divide n1 by n2, giving the single-cell remainder n3."""
    a, b = inner.top2_ds()
    inner.push_ds(a.mod(b))


# 1+ ( n1 -- n2 )
def prim_INC(inner):
    """GForth core 2012: add one to n1."""
    a = inner.pop_ds()
    inner.push_ds(a.inc())


# 1- ( n1 -- n2 )
def prim_DEC(inner):
    """GForth core 2012: subtract one from n1."""
    a = inner.pop_ds()
    inner.push_ds(a.dec())


# memory management


# ! ( x addr -- )
def prim_STORE(inner):
    """GForth core 2012: store x at cell address addr."""
    addr_obj = inner.pop_ds()
    val_obj = inner.pop_ds()
    inner.cell_store(addr_obj, val_obj)


# @ ( addr -- x )
def prim_FETCH(inner):
    """GForth core 2012: fetch the cell contents at addr."""
    addr_obj = inner.pop_ds()
    inner.push_ds(inner.cell_fetch(addr_obj))


# ( -- n )
def prim_CELL(inner):
    """push the size of one cell in address units."""
    inner.push_ds(CELL_SIZE)

# ( n -- n )
def prim_CELLPLUS(inner):
    """GForth core 2012: add one cell to an address."""
    addr = inner.pop_ds()
    inner.push_ds(addr.add(CELL_SIZE))


# ( n -- n * cell_size )
def prim_CELLS(inner):
    """GForth core 2012: convert a cell count to address units."""
    count = inner.pop_ds()
    inner.push_ds(count.mul(CELL_SIZE))


# IF THEN ELSE


# 0BRANCH ( flag -- )
def prim_0BRANCH(inner):
    """GForth core 2012: branch to target when flag is zero."""
    w_x = inner.pop_ds()
    if w_x.intval == 0:
        target = inner.cur.lits[inner.ip]
        inner.ip = target.intval - 1


# BRANCH ( -- )
def prim_BRANCH(inner):
    """GForth core 2012: branch unconditionally to the target."""
    target = inner.cur.lits[inner.ip]
    inner.ip = target.intval - 1


# BASE


# BASE@ ( -- u )
def prim_BASE_FETCH(inner):
    """GForth core 2012: return the current conversion base."""
    inner.push_ds(inner.base)


# BASE! ( u -- )
def prim_BASE_STORE(inner):
    """GForth core 2012: set the conversion base to u."""
    u = inner.pop_ds()
    inner.base = u


# DECIMAL ( -- )
def prim_DECIMAL(inner):
    """GForth core 2012: set BASE to decimal (radix 10)."""
    inner.base = DECIMAL


# HEX ( -- )
def prim_HEX(inner):
    """GForth core 2012: set BASE to hexadecimal (radix 16)."""
    inner.base = HEX


# OCTAL ( -- )
def prim_OCTAL(inner):
    """GForth core 2012: set BASE to octal (radix 8)."""
    inner.base = OCTAL


# BINARY ( -- )
def prim_BINARY(inner):
    """GForth core 2012: set BASE to binary (radix 2)."""
    inner.base = BINARY


# <# ( -- )
def prim_LESSNUM(inner):
    """GForth core 2012: begin pictured numeric output conversion."""
    inner._pno_active = True
    inner._pno_buf = []


# # ( ud1 -- ud2 )
def prim_NUMSIGN(inner):
    """GForth core 2012: extract one digit during pictured numeric output."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("# outside <# #>"))
        return
    x = inner.pop_ds()
    base = inner.base.intval
    q = x.intval // base
    r = x.intval % base
    inner._pno_buf.insert(0, digit_to_char(r))
    inner.push_ds(W_IntObject(q))


# #S ( ud -- 0 )
def prim_NUMSIGN_S(inner):
    """GForth core 2012: convert remaining digits during pictured numeric output."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("#S outside <# #>"))
        return
    while True:
        x = inner.pop_ds()
        base = inner.base.intval
        q = x.intval // base
        r = x.intval % base
        inner._pno_buf.insert(0, digit_to_char(r))
        inner.push_ds(W_IntObject(q))
        if q == 0:
            break


# HOLD ( char -- )
def prim_HOLD(inner):
    """GForth core 2012: insert character into pictured numeric output buffer."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("HOLD outside <# #>"))
        return
    ch = inner.pop_ds()
    inner._pno_buf.insert(0, chr(ch.intval))


# #> ( xd -- c-addr u )
def prim_NUMGREATER(inner):
    """GForth core 2012: finish pictured numeric output and deliver the string."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("#> outside <# #>"))
        return
    _ = inner.pop_ds()
    s = "".join(inner._pno_buf)
    inner._pno_active = False
    inner.push_ds(W_StringObject(s))


# TYPE ( c-addr u -- )
def prim_TYPE(inner):
    """GForth core 2012: display the character string."""
    w_s = inner.pop_ds()
    inner.print_str(w_s)


# I/O


# . ( n -- )
def prim_DOT(inner):
    """GForth core 2012: display n according to current BASE."""
    x = inner.pop_ds()
    inner.print_int(x)

# EMIT ( x -- )
def prim_EMIT(inner):
    """GForth core 2012: if x is a character, display x. """
    x = inner.pop_ds()
    assert isinstance(x, W_IntObject)
    assert 0 <= x.intval <= 255, "EMIT argument out of range"
    print(chr(x.intval)),


# CodeThread-aware primitives


# LIT ( -- x )
def prim_LIT(inner):
    """GForth core 2012: push the next compilation literal."""
    inner.prim_LIT()


# EXIT ( -- )
def prim_EXIT(inner):
    """GForth core 2012: terminate the current definition."""
    inner.prim_EXIT()


def install_primitives(outer):
    outer.define_prim("0=", prim_ZEROEQUAL)
    outer.define_prim("0<", prim_ZEROLESS)
    outer.define_prim("0>", prim_ZEROGREATER)
    outer.define_prim("0<>", prim_ZERONOTEQUAL)
    # stack manipulation
    outer.define_prim("DUP", prim_DUP)
    outer.define_prim("DROP", prim_DROP)
    outer.define_prim("SWAP", prim_SWAP)
    outer.define_prim("OVER", prim_OVER)

    outer.define_prim("2DUP", prim_2DUP)
    outer.define_prim("2DROP", prim_2DROP)
    outer.define_prim("2SWAP", prim_2SWAP)
    outer.define_prim("2OVER", prim_2OVER)

    outer.define_prim("?DUP", prim_QUESTIONDUP)

    outer.define_prim("ROT", prim_ROT)
    outer.define_prim("MAX", prim_MAX)
    outer.define_prim("MIN", prim_MIN)

    outer.define_prim("DEPTH", prim_DEPTH)

    outer.define_prim("RSHIFT", prim_RSHIFT)
    outer.define_prim("LSHIFT", prim_LSHIFT)

    outer.define_prim("S>D", prim_S_TO_D)
    # arithmetic
    outer.define_prim("+", prim_ADD)
    outer.define_prim("-", prim_SUB)
    outer.define_prim("*", prim_MUL)

    outer.define_prim("ABS", prim_ABS)
    outer.define_prim("NEGATE", prim_NEGATE)
    outer.define_prim("MOD", prim_MOD)

    outer.define_prim("1+", prim_INC)
    outer.define_prim("1-", prim_DEC)

    # I/O
    outer.define_prim(".", prim_DOT)
    outer.define_prim("EMIT", prim_EMIT)

    # memory management
    outer.define_prim("!", prim_STORE)
    outer.define_prim("@", prim_FETCH)
    outer.define_prim("CELL", prim_CELL)
    outer.define_prim("CELL+", prim_CELLPLUS)
    outer.define_prim("CELLS", prim_CELLS)

    # BASE
    outer.define_prim("BASE@", prim_BASE_FETCH)
    outer.define_prim("BASE!", prim_BASE_STORE)
    outer.define_prim("DECIMAL", prim_DECIMAL)
    outer.define_prim("HEX", prim_HEX)
    outer.define_prim("OCTAL", prim_OCTAL)
    outer.define_prim("BINARY", prim_BINARY)

    outer.define_prim("<#", prim_LESSNUM)
    outer.define_prim("#", prim_NUMSIGN)
    outer.define_prim("#S", prim_NUMSIGN_S)
    outer.define_prim("#>", prim_NUMGREATER)
    outer.define_prim("HOLD", prim_HOLD)

    outer.define_prim("TYPE", prim_TYPE)  # for testing output

    # loop
    outer.define_prim("0BRANCH", prim_0BRANCH)
    outer.define_prim("BRANCH", prim_BRANCH)

    # thread ops
    outer.define_prim("LIT", prim_LIT)
    outer.define_prim("EXIT", prim_EXIT)
