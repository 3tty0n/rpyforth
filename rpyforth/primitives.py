from rpython.rlib.rfile import create_stdio
from rpython.rlib.jit import promote, unroll_safe

from rpyforth.objects import (
    BINARY,
    OCTAL,
    DECIMAL,
    HEX,
    TRUE,
    ZERO,
    W_IntObject,
    W_StringObject,
    W_FloatObject,
    CELL_SIZE,
)
from rpyforth.inner_interp import jitdriver
from rpyforth.util import digit_to_char


# Internal helpers -----------------------------------------------------------

def _maybe_enter_jit(inner, target_ip, origin_ip, thread):
    """Signal the interpreter back-edge to the JIT when jumping backward."""
    if target_ip < origin_ip:
        jitdriver.can_enter_jit(
            ip=target_ip,
            thread=thread,
            self=inner,
        )


# 0= ( x -- flag )
def prim_ZEROEQUAL(inner, cur, ip):
    """GForth core 2012: flag is true when x equals zero."""
    w_x = inner.pop_ds()
    if w_x.zero_equal():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)
    return ip


# 0< ( n -- flag )
def prim_ZEROLESS(inner, cur, ip):
    """GForth core 2012: flag is true when n is strictly negative."""
    w_x = inner.pop_ds()
    if w_x.zero_less():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)
    return ip


# 0> ( n -- flag )
def prim_ZEROGREATER(inner, cur, ip):
    """GForth core 2012: flag is true when n is strictly positive."""
    w_x = inner.pop_ds()
    if w_x.zero_greater():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)
    return ip


# > ( n1 n2 -- flag )
def prim_GREATER(inner, cur, ip):
    """GForth core 2012: flag is true when n1 is greater than n2."""
    # Pop in correct order: n2 is top, n1 is second
    w_n2 = inner.pop_ds()
    w_n1 = inner.pop_ds()
    assert isinstance(w_n1, W_IntObject)
    assert isinstance(w_n2, W_IntObject)
    # Direct field access for better JIT optimization
    if w_n1.intval > w_n2.intval:
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)
    return ip

# < ( n1 n2 -- flag )
def prim_LESS(inner, cur, ip):
    """GForth core 2012: flag is true when n1 is less than n2."""
    # Pop in correct order: n2 is top, n1 is second
    w_n2 = inner.pop_ds()
    w_n1 = inner.pop_ds()
    assert isinstance(w_n1, W_IntObject)
    assert isinstance(w_n2, W_IntObject)
    # Direct field access for better JIT optimization
    if w_n1.intval < w_n2.intval:
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)
    return ip


# 0<> ( n -- flag )
def prim_ZERONOTEQUAL(inner, cur, ip):
    """GForth core 2012: flag is true when n is non-zero."""
    w_x = inner.pop_ds()
    if not w_x.zero_equal():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)
    return ip


# DUP ( x -- x x )
def prim_DUP(inner, cur, ip):
    """GForth core 2012: duplicate x, leaving two copies on the stack."""
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(a)
    return ip


def prim_DUP2(inner, cur, ip):
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(b)
    inner.push_ds(a)
    inner.push_ds(b)
    return ip


# DROP ( x -- )
def prim_DROP(inner, cur, ip):
    """GForth core 2012: discard the top stack item."""
    inner.pop_ds()
    return ip


def prim_DROP2(inner, cur, ip):
    inner.pop_ds()
    inner.pop_ds()
    return ip

# SWAP ( x1 x2 -- x2 x1 )
def prim_SWAP(inner, cur, ip):
    """GForth core 2012: exchange the top two stack items."""
    a, b = inner.top2_ds()
    inner.push_ds(b)
    inner.push_ds(a)
    return ip


def prim_SWAP2(inner, cur, ip):
    c, d = inner.top2_ds()
    a, b = inner.top2_ds()
    inner.push_ds(c)
    inner.push_ds(d)
    inner.push_ds(a)
    inner.push_ds(b)
    return ip


# OVER ( x1 x2 -- x1 x2 x1 )
def prim_OVER(inner, cur, ip):
    """GForth core 2012: copy the second stack item to the top."""
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(b)
    inner.push_ds(a)
    return ip


def prim_OVER2(inner, cur, ip):
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
    return ip


def prim_ROT(inner, cur, ip):
    c = inner.pop_ds()
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(b)
    inner.push_ds(c)
    inner.push_ds(a)
    return ip


def prim_MAX(inner, cur, ip):
    a, b = inner.top2_ds()
    if a.lt(b):
        inner.push_ds(b)
    else:
        inner.push_ds(a)
    return ip


def prim_MIN(inner, cur, ip):
    a, b = inner.top2_ds()
    if a.lt(b):
        inner.push_ds(a)
    else:
        inner.push_ds(b)
    return ip


# Arithmetic


# + ( n1 n2 -- n3 )
def prim_ADD(inner, cur, ip):
    """GForth core 2012: add n1 and n2, leaving their sum."""
    # top2_ds pops in correct order: second-to-top (a), then top (b)
    a, b = inner.top2_ds()
    assert isinstance(a, W_IntObject)
    assert isinstance(b, W_IntObject)
    # Direct field access for better JIT optimization
    inner.push_ds(W_IntObject(a.intval + b.intval))
    return ip


# - ( n1 n2 -- n3 )
def prim_SUB(inner, cur, ip):
    """GForth core 2012: subtract n2 from n1, leaving the difference."""
    # top2_ds pops in correct order: second-to-top (a), then top (b)
    a, b = inner.top2_ds()
    assert isinstance(a, W_IntObject)
    assert isinstance(b, W_IntObject)
    # Direct field access for better JIT optimization
    inner.push_ds(W_IntObject(a.intval - b.intval))
    return ip


# * ( n1 n2 -- n3 )
def prim_MUL(inner, cur, ip):
    """GForth core 2012: multiply n1 by n2, leaving the product."""
    # top2_ds pops in correct order: second-to-top (a), then top (b)
    a, b = inner.top2_ds()
    assert isinstance(a, W_IntObject)
    assert isinstance(b, W_IntObject)
    # Direct field access for better JIT optimization
    inner.push_ds(W_IntObject(a.intval * b.intval))
    return ip


def prim_ABS(inner, cur, ip):
    a = inner.pop_ds()
    inner.push_ds(a.abs())
    return ip


def prim_NEGATE(inner, cur, ip):
    a = inner.pop_ds()
    inner.push_ds(a.neg())
    return ip


def prim_MOD(inner, cur, ip):
    a, b = inner.top2_ds()
    inner.push_ds(a.mod(b))
    return ip


def prim_INC(inner, cur, ip):
    a = inner.pop_ds()
    inner.push_ds(a.inc())
    return ip


def prim_DEC(inner, cur, ip):
    a = inner.pop_ds()
    inner.push_ds(a.dec())
    return ip


# memory management


# ! ( x addr -- )
def prim_STORE(inner, cur, ip):
    """GForth core 2012: store x at cell address addr."""
    addr_obj = inner.pop_ds()
    val_obj = inner.pop_ds()
    inner.cell_store(addr_obj, val_obj)
    return ip


# @ ( addr -- x )
def prim_FETCH(inner, cur, ip):
    """GForth core 2012: fetch the cell contents at addr."""
    addr_obj = inner.pop_ds()
    inner.push_ds(inner.cell_fetch(addr_obj))
    return ip


# ( -- n )
def prim_CELL(inner, cur, ip):
    """push the size of one cell in address units."""
    inner.push_ds(CELL_SIZE)
    return ip


# ( n -- n )
def prim_CELLPLUS(inner, cur, ip):
    """GForth core 2012: add one cell to an address."""
    addr = inner.pop_ds()
    assert isinstance(addr, W_IntObject)
    inner.push_ds(addr.add(CELL_SIZE))
    return ip


# ( n -- n * cell_size )
def prim_CELLS(inner, cur, ip):
    """GForth core 2012: convert a cell count to address units."""
    count = inner.pop_ds()
    assert isinstance(count, W_IntObject)
    inner.push_ds(count.mul(CELL_SIZE))
    return ip


# IF THEN ELSE


# 0BRANCH ( flag -- )
def prim_0BRANCH(inner, cur, ip):
    """GForth core 2012: branch to target when flag is zero."""
    origin_ip = ip - 1
    w_x = inner.pop_ds()
    assert isinstance(w_x, W_IntObject)
    # Direct field access for better optimization
    if w_x.intval == 0:
        w_target = promote(cur.lits[origin_ip])
        assert isinstance(w_target, W_IntObject)
        target_ip = w_target.intval
        ip = target_ip
        _maybe_enter_jit(inner, target_ip, origin_ip, cur)
    return ip


# BRANCH ( -- )
def prim_BRANCH(inner, cur, ip):
    """GForth core 2012: branch unconditionally to the target."""
    origin_ip = ip - 1
    target = promote(cur.lits[origin_ip])
    assert isinstance(target, W_IntObject)
    target_ip = target.intval
    ip = target_ip
    _maybe_enter_jit(inner, target_ip, origin_ip, cur)
    return ip


# Loop control primitives

# (DO) ( limit start -- ) ( R: -- limit start )
def prim_DO_RUNTIME(inner, cur, ip):
    start = inner.pop_ds()
    limit = inner.pop_ds()
    inner.push_rs(limit)
    inner.push_rs(start)
    return ip


# (LOOP) ( -- ) ( R: limit counter -- limit counter+1 | )
def prim_LOOP_RUNTIME(inner, cur, ip):
    counter = inner.pop_rs()
    limit = inner.pop_rs()

    assert isinstance(counter, W_IntObject)
    assert isinstance(limit, W_IntObject)

    # Directly access intval for better optimization
    # The JIT can constant-fold these if they're promoted
    counter_val = counter.intval
    limit_val = limit.intval
    new_counter_val = counter_val + 1

    if new_counter_val < limit_val:
        # Continue loop: push back to return stack and branch
        new_counter = W_IntObject(new_counter_val)
        inner.push_rs(limit)
        inner.push_rs(new_counter)
        origin_ip = ip - 1
        target = promote(cur.lits[origin_ip])
        assert isinstance(target, W_IntObject)
        target_ip = target.intval
        ip = target_ip
        _maybe_enter_jit(inner, target_ip, origin_ip, cur)
    return ip

# LEAVE ( -- ) ( R: limit counter -- )
def prim_LEAVE(inner, cur, ip):
    """Exit the current loop by cleaning up return stack and jumping to end."""
    inner.pop_rs()  # counter
    inner.pop_rs()  # limit
    target = cur.lits[ip - 2]
    assert isinstance(target, W_IntObject)
    ip = target.intval + 1
    return ip

# I ( -- n ) ( R: limit counter -- limit counter )
def prim_I(inner, cur, ip):
    """Get the current loop counter (innermost loop)."""
    counter = inner.pop_rs()
    limit = inner.pop_rs()
    inner.push_rs(limit)
    inner.push_rs(counter)
    inner.push_ds(counter)
    return ip


# J ( -- n ) ( R: limit1 counter1 limit2 counter2 -- limit1 counter1 limit2 counter2 )
def prim_J(inner, cur, ip):
    """Get the outer loop counter (second innermost loop)."""
    counter2 = inner.pop_rs()
    limit2 = inner.pop_rs()
    counter1 = inner.pop_rs()
    limit1 = inner.pop_rs()
    inner.push_rs(limit1)
    inner.push_rs(counter1)
    inner.push_rs(limit2)
    inner.push_rs(counter2)
    inner.push_ds(counter1)
    return ip


# BASE


# BASE@ ( -- u )
def prim_BASE_FETCH(inner, cur, ip):
    """GForth core 2012: return the current conversion base."""
    inner.push_ds(inner.base)
    return ip


# BASE! ( u -- )
def prim_BASE_STORE(inner, cur, ip):
    """GForth core 2012: set the conversion base to u."""
    u = inner.pop_ds()
    inner.base = u
    return ip


# DECIMAL ( -- )
def prim_DECIMAL(inner, cur, ip):
    """GForth core 2012: set BASE to decimal (radix 10)."""
    inner.base = DECIMAL
    return ip


# HEX ( -- )
def prim_HEX(inner, cur, ip):
    """GForth core 2012: set BASE to hexadecimal (radix 16)."""
    inner.base = HEX
    return ip


# OCTAL ( -- )
def prim_OCTAL(inner, cur, ip):
    """GForth core 2012: set BASE to octal (radix 8)."""
    inner.base = OCTAL
    return ip


# BINARY ( -- )
def prim_BINARY(inner, cur, ip):
    """GForth core 2012: set BASE to binary (radix 2)."""
    inner.base = BINARY
    return ip


# <# ( -- )
def prim_LESSNUM(inner, cur, ip):
    """GForth core 2012: begin pictured numeric output conversion."""
    inner._pno_active = True
    inner._pno_buf = []
    return ip


# # ( ud1 -- ud2 )
def prim_NUMSIGN(inner, cur, ip):
    """GForth core 2012: extract one digit during pictured numeric output."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("# outside <# #>"))
        return ip
    x = inner.pop_ds()
    assert isinstance(x, W_IntObject)
    base = inner.base.intval
    q = x.getvalue() // base
    r = x.getvalue() % base
    inner._pno_buf.insert(0, digit_to_char(r))
    inner.push_ds(W_IntObject(q))
    return ip


# #S ( ud -- 0 )
@unroll_safe
def prim_NUMSIGN_S(inner, cur, ip):
    """GForth core 2012: convert remaining digits during pictured numeric output."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("#S outside <# #>"))
        return ip
    while True:
        x = inner.pop_ds()
        assert isinstance(x, W_IntObject)
        base = inner.base.intval
        q = x.getvalue() // base
        r = x.getvalue() % base
        inner._pno_buf.insert(0, digit_to_char(r))
        inner.push_ds(W_IntObject(q))
        if q == 0:
            break
    return ip


# HOLD ( char -- )
def prim_HOLD(inner, cur, ip):
    """GForth core 2012: insert character into pictured numeric output buffer."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("HOLD outside <# #>"))
        return ip
    ch = inner.pop_ds()
    assert isinstance(ch, W_IntObject)
    inner._pno_buf.insert(0, chr(ch.getvalue()))
    return ip


# #> ( xd -- c-addr u )
def prim_NUMGREATER(inner, cur, ip):
    """GForth core 2012: finish pictured numeric output and deliver the string."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("#> outside <# #>"))
        return ip
    _ = inner.pop_ds()
    s = "".join(inner._pno_buf)
    inner._pno_active = False
    inner.push_ds(W_StringObject(s))
    return ip


# TYPE ( c-addr u -- )
def prim_TYPE(inner, cur, ip):
    """GForth core 2012: display the character string."""
    w_s = inner.pop_ds()
    inner.print_str(w_s)
    return ip


# I/O


# . ( n -- )
def prim_DOT(inner, cur, ip):
    """GForth core 2012: display n according to current BASE."""
    x = inner.pop_ds()
    assert isinstance(x, W_IntObject)
    stdin, stdout, stderr = create_stdio()
    stdout.write(str(x.getvalue()))
    stdout.write(' ')
    #stdout.flush()
    return ip


# EMIT ( char -- )
def prim_EMIT(inner, cur, ip):
    """GForth core 2012: display character with char code."""
    x = inner.pop_ds()
    assert isinstance(x, W_IntObject)
    stdin, stdout, stderr = create_stdio()
    stdout.write(chr(x.getvalue()))
    stdout.flush()
    return ip


# CodeThread-aware primitives

# LIT ( -- x )
def prim_LIT(inner, cur, ip):
    """GForth core 2012: push the next compilation literal."""
    lit = promote(cur.lits[ip - 1])
    inner.push_ds(lit)
    return ip


# EXIT ( -- )
def prim_EXIT(inner, cur, ip):
    """GForth core 2012: terminate the current definition."""
    from rpyforth.inner_interp import Exit
    raise Exit


# Floating point operations

# F* ( f1 f2 -- f3 )
def prim_FMUL(inner, cur, ip):
    """Multiply two floating point numbers."""
    f2 = inner.pop_ds()
    f1 = inner.pop_ds()
    assert isinstance(f1, W_FloatObject)
    assert isinstance(f2, W_FloatObject)
    inner.push_ds(f1.mul(f2))
    return ip


# F+ ( f1 f2 -- f3 )
def prim_FADD(inner, cur, ip):
    """Add two floating point numbers."""
    f2 = inner.pop_ds()
    f1 = inner.pop_ds()
    assert isinstance(f1, W_FloatObject)
    assert isinstance(f2, W_FloatObject)
    inner.push_ds(f1.add(f2))
    return ip


# F- ( f1 f2 -- f3 )
def prim_FSUB(inner, cur, ip):
    """Subtract f2 from f1."""
    f2 = inner.pop_ds()
    f1 = inner.pop_ds()
    assert isinstance(f1, W_FloatObject)
    assert isinstance(f2, W_FloatObject)
    inner.push_ds(f1.sub(f2))
    return ip


# F/ ( f1 f2 -- f3 )
def prim_FDIV(inner, cur, ip):
    """Divide f1 by f2."""
    f2 = inner.pop_ds()
    f1 = inner.pop_ds()
    assert isinstance(f1, W_FloatObject)
    assert isinstance(f2, W_FloatObject)
    inner.push_ds(f1.div(f2))
    return ip


# F> ( f1 f2 -- flag )
def prim_FGREATER(inner, cur, ip):
    """Compare if f1 > f2."""
    f2 = inner.pop_ds()
    f1 = inner.pop_ds()
    assert isinstance(f1, W_FloatObject)
    assert isinstance(f2, W_FloatObject)
    if f1.gt(f2):
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)
    return ip


# FSWAP ( f1 f2 -- f2 f1 )
def prim_FSWAP(inner, cur, ip):
    """Exchange the top two floating point stack items."""
    f2 = inner.pop_ds()
    f1 = inner.pop_ds()
    inner.push_ds(f2)
    inner.push_ds(f1)
    return ip


# Stack manipulation

# PICK ( xu ... x1 x0 u -- xu ... x1 x0 xu )
def prim_PICK(inner, cur, ip):
    """Copy the u-th stack item to the top (0 PICK is equivalent to DUP)."""
    u = inner.pop_ds()
    assert isinstance(u, W_IntObject)
    u_val = u.getvalue()

    # We need to access virtualizable stack without direct indexing
    # Pop items off, find the one we want, and push them all back
    if u_val == 0:
        # 0 PICK is just DUP
        item = inner.pop_ds()
        inner.push_ds(item)
        inner.push_ds(item)
    else:
        # Pop u_val+1 items to get to the target
        temp = []
        for i in range(u_val + 1):
            temp.append(inner.pop_ds())
        # The item we want is at the end
        item = temp[u_val]
        # Push everything back
        for i in range(u_val, -1, -1):
            inner.push_ds(temp[i])
        # Push the picked item
        inner.push_ds(item)
    return ip


# Floating point conversion and storage

# S>F ( n -- ) ( F: -- f )
def prim_S2F(inner, cur, ip):
    """Convert signed integer to float."""
    n = inner.pop_ds()
    assert isinstance(n, W_IntObject)
    inner.push_ds(W_FloatObject(float(n.getvalue())))
    return ip


# F! ( f-addr -- ) ( F: f -- )
def prim_FSTORE(inner, cur, ip):
    """Store float at address."""
    addr_obj = inner.pop_ds()
    val_obj = inner.pop_ds()
    inner.float_store(addr_obj, val_obj)
    return ip


# F@ ( f-addr -- ) ( F: -- f )
def prim_FFETCH(inner, cur, ip):
    """Fetch float from address."""
    addr_obj = inner.pop_ds()
    inner.push_ds(inner.float_fetch(addr_obj))
    return ip


# FDUP ( F: f -- f f )
def prim_FDUP(inner, cur, ip):
    """Duplicate float on stack."""
    f = inner.pop_ds()
    assert isinstance(f, W_FloatObject)
    inner.push_ds(f)
    inner.push_ds(f)
    return ip


# Comparison

# = ( x1 x2 -- flag )
def prim_EQUAL(inner, cur, ip):
    x2 = inner.pop_ds()
    x1 = inner.pop_ds()
    if x1.eq(x2):
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)
    return ip


def install_primitives(outer):
    outer.define_prim("0=", prim_ZEROEQUAL)
    outer.define_prim("0<", prim_ZEROLESS)
    outer.define_prim("0>", prim_ZEROGREATER)
    outer.define_prim(">",  prim_GREATER)
    outer.define_prim("<",  prim_LESS)
    outer.define_prim("0<>", prim_ZERONOTEQUAL)
    # stack manipulation
    outer.define_prim("DUP", prim_DUP)
    outer.define_prim("DROP", prim_DROP)
    outer.define_prim("SWAP", prim_SWAP)
    outer.define_prim("OVER", prim_OVER)

    outer.define_prim("2DUP", prim_DUP2)
    outer.define_prim("2DROP", prim_DROP2)
    outer.define_prim("2SWAP", prim_SWAP2)
    outer.define_prim("2OVER", prim_OVER2)

    outer.define_prim("ROT", prim_ROT)
    outer.define_prim("MAX", prim_MAX)
    outer.define_prim("MIN", prim_MIN)

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
    outer.define_prim("(DO)", prim_DO_RUNTIME)
    outer.define_prim("(LOOP)", prim_LOOP_RUNTIME)
    outer.define_prim("LEAVE", prim_LEAVE)
    outer.define_prim("I", prim_I)
    outer.define_prim("J", prim_J)

    # thread ops
    outer.define_prim("LIT", prim_LIT)
    outer.define_prim("EXIT", prim_EXIT)

    # floating point
    outer.define_prim("F*", prim_FMUL)
    outer.define_prim("F+", prim_FADD)
    outer.define_prim("F-", prim_FSUB)
    outer.define_prim("F/", prim_FDIV)
    outer.define_prim("F>", prim_FGREATER)
    outer.define_prim("FSWAP", prim_FSWAP)
    outer.define_prim("S>F", prim_S2F)
    outer.define_prim("F!", prim_FSTORE)
    outer.define_prim("F@", prim_FFETCH)
    outer.define_prim("FDUP", prim_FDUP)

    # stack manipulation
    outer.define_prim("PICK", prim_PICK)

    # comparison
    outer.define_prim("=", prim_EQUAL)
