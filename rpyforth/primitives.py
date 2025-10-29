from rpyforth.objects import BINARY, OCTAL, DECIMAL, HEX, TRUE, ZERO
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

# DROP ( x -- )
def prim_DROP(inner):
    """GForth core 2012: discard the top stack item."""
    inner.pop_ds()

# SWAP ( x1 x2 -- x2 x1 )
def prim_SWAP(inner):
    """GForth core 2012: exchange the top two stack items."""
    a,b = inner.top2_ds()
    inner.push_ds(b)
    inner.push_ds(a)

# OVER ( x1 x2 -- x1 x2 x1 )
def prim_OVER(inner):
    """GForth core 2012: copy the second stack item to the top."""
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(b)
    inner.push_ds(a)

# Arithmetic

# + ( n1 n2 -- n3 )
def prim_ADD(inner):
    """GForth core 2012: add n1 and n2, leaving their sum."""
    a,b = inner.top2_ds()
    inner.push_ds(a.add(b))

# - ( n1 n2 -- n3 )
def prim_SUB(inner):
    """GForth core 2012: subtract n2 from n1, leaving the difference."""
    a,b = inner.top2_ds()
    inner.push_ds(a.sub(b))

# * ( n1 n2 -- n3 )
def prim_MUL(inner):
    """GForth core 2012: multiply n1 by n2, leaving the product."""
    a,b = inner.top2_ds()
    inner.push_ds(a.mul(b))

# memory management

# ! ( x addr -- )
def prim_STORE(inner):
    """GForth core 2012: store x at cell address addr."""
    addr_obj = inner.pop_ds()
    val_obj  = inner.pop_ds()
    idx = addr_obj.intval
    inner.mem[idx] = val_obj

# @ ( addr -- x )
def prim_FETCH(inner):
    """GForth core 2012: fetch the cell contents at addr."""
    addr_obj = inner.pop_ds()
    idx = addr_obj.intval
    inner.push_ds(inner.mem[idx])


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


# Loops

# DO ( limit start -- ) ( R: -- limit start )
def prim_DO(inner):
    """GForth core 2012: set up a counted loop, skipping when start >= limit."""
    w_start = inner.pop_ds()
    w_limit = inner.pop_ds()
    target = inner.cur.lits[inner.ip]
    if w_start.intval >= w_limit.intval:
        inner.ip = target.intval - 1
        return
    inner.push_rs(w_limit)
    inner.push_rs(w_start)

# LOOP ( -- ) ( R: limit index -- | limit index' )
def prim_LOOP(inner):
    """GForth core 2012: advance a counted loop and branch while index < limit."""
    target = inner.cur.lits[inner.ip]
    w_index = inner.pop_rs()
    w_limit = inner.pop_rs()
    new_index = w_index.intval + 1
    if new_index < w_limit.intval:
        inner.push_rs(w_limit)
        inner.push_rs(W_IntObject(new_index))
        inner.ip = target.intval - 1
    else:
        # loop finished; do not push back limit/index
        return

# I ( -- n ) ( R: limit index -- limit index )
def prim_I(inner):
    """GForth core 2012: copy the current loop index to the data stack."""
    w_index = inner.pop_rs()
    w_limit = inner.pop_rs()
    inner.push_rs(w_limit)
    inner.push_rs(w_index)
    inner.push_ds(W_IntObject(w_index.intval))


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

from rpyforth.objects import W_IntObject, W_StringObject

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
    r = x.intval %  base
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
        r = x.intval %  base
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
    s = ''.join(inner._pno_buf)
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
    outer.define_prim("0=",   prim_ZEROEQUAL)
    outer.define_prim("0<",   prim_ZEROLESS)
    outer.define_prim("0>",   prim_ZEROGREATER)
    outer.define_prim("0<>",  prim_ZERONOTEQUAL)
    # stack manipulation
    outer.define_prim("DUP",  prim_DUP)
    outer.define_prim("DROP", prim_DROP)
    outer.define_prim("SWAP", prim_SWAP)
    outer.define_prim("OVER", prim_OVER)
    # arithmetic
    outer.define_prim("+",    prim_ADD)
    outer.define_prim("-",    prim_SUB)
    outer.define_prim("*",    prim_MUL)
    # I/O
    outer.define_prim(".",    prim_DOT)
    # memory management
    outer.define_prim("!",    prim_STORE)
    outer.define_prim("@",    prim_FETCH)
    # BASE
    outer.define_prim("BASE@",   prim_BASE_FETCH)
    outer.define_prim("BASE!",   prim_BASE_STORE)
    outer.define_prim("DECIMAL", prim_DECIMAL)
    outer.define_prim("HEX",     prim_HEX)
    outer.define_prim("OCTAL",   prim_OCTAL)
    outer.define_prim("BINARY",  prim_BINARY)

    outer.define_prim("<#",  prim_LESSNUM)
    outer.define_prim("#",   prim_NUMSIGN)
    outer.define_prim("#S",  prim_NUMSIGN_S)
    outer.define_prim("#>",  prim_NUMGREATER)
    outer.define_prim("HOLD",prim_HOLD)

    outer.define_prim("TYPE", prim_TYPE)   # for testing output

    # loop
    outer.define_prim("0BRANCH", prim_0BRANCH)
    outer.define_prim("BRANCH",  prim_BRANCH)
    outer.define_prim("DO",      prim_DO)
    outer.define_prim("LOOP",    prim_LOOP)
    outer.define_prim("I",       prim_I)

    # thread ops
    outer.define_prim("LIT",  prim_LIT)
    outer.define_prim("EXIT", prim_EXIT)
