from rpython.rlib.rfile import create_stdio
from rpython.rlib.jit import promote, unroll_safe, dont_look_inside, hint
from rpython.rlib.rfloat import formatd, INFINITY, NAN
from rpython.rlib.rarithmetic import intmask, r_uint

from rpyforth.objects import (
    BINARY,
    CELL_SIZE_BYTES,
    OCTAL,
    DECIMAL,
    HEX,
    TRUE,
    ZERO,
    CodeThread,
    DeferredCodeThread,
    ForthException,
    Word,
    W_IntObject,
    W_StringObject,
    W_FloatObject,
    W_WordObject,
    LONG_BIT,
    _small_int_cache,
    SMALL_INT_MIN,
    SMALL_INT_MAX,
    word_from_wid,
    WORD_REGISTRY,
    THREAD_REGISTRY,
)
from rpyforth.inner_interp import (
    jitdriver,
    Abort,
    HEAP_SIZE_BYTES,
    USE_STACK_FRAGMENT,
    CALL_SENTINEL,
    DEFER_TAILCALL_SENTINEL,
)
from rpyforth.heap import ALLOC_BASE, DICT_SIZE_BYTES
from rpyforth.metastack import push_ds_fragments
from rpyforth.metastack import USE_FLOAT_FRAGMENT, USE_FRAME_ONLY
if USE_FRAME_ONLY:
    from rpyforth.metastack_int_frameonly import snapshot_cache, restore_cache
else:
    from rpyforth.metastack_int import snapshot_cache, restore_cache
from rpyforth.metastack_float import snapshot_float_cache, restore_float_cache
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
    x = inner.pop_ds_int()
    if x == 0:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# 0< ( n -- flag )
def prim_ZEROLESS(inner, cur, ip):
    """GForth core 2012: flag is true when n is strictly negative."""
    x = inner.pop_ds_int()
    if x < 0:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# 0> ( n -- flag )
def prim_ZEROGREATER(inner, cur, ip):
    """GForth core 2012: flag is true when n is strictly positive."""
    x = inner.pop_ds_int()
    if x > 0:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# > ( n1 n2 -- flag )
def prim_GREATER(inner, cur, ip):
    """GForth core 2012: flag is true when n1 is greater than n2."""
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    if n1 > n2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip

# < ( n1 n2 -- flag )
def prim_LESS(inner, cur, ip):
    """GForth core 2012: flag is true when n1 is less than n2."""
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    if n1 < n2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# 0<> ( n -- flag )
def prim_ZERONOTEQUAL(inner, cur, ip):
    """GForth core 2012: flag is true when n is non-zero."""
    x = inner.pop_ds_int()
    if x != 0:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip

# def U< (n1 n2 -- flag )
def prim_U_LESS(inner, cur, ip):
    """GForth core 2012: flag is true if and only if u1 is less than u2."""
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    if r_uint(n1) < r_uint(n2):
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip

# DUP ( x -- x x )
def prim_DUP(inner, cur, ip):
    """GForth core 2012: duplicate x, leaving two copies on the stack."""
    a = inner.peek_ds_int(0)
    inner.push_ds_int(a)
    return ip


# 2DUP ( x1 x2 -- x1 x2 x1 x2 )
def prim_2DUP(inner, cur, ip):
    b = inner.peek_ds_int(0)
    a = inner.peek_ds_int(1)
    inner.push_ds_int(a)
    inner.push_ds_int(b)
    return ip


# ?DUP ( x -- 0 | x x )
def prim_QUESTIONDUP(inner, cur, ip):
    """GForth core 2012: duplicate x if it is non-zero."""
    a = inner.peek_ds_int(0)
    if a != 0:
        inner.push_ds_int(a)
    return ip


# DROP ( x -- )
def prim_DROP(inner, cur, ip):
    """GForth core 2012: discard the top stack item."""
    inner.pop_ds_int()
    return ip


# NIP ( x1 x2 -- x2 )
def prim_NIP(inner, cur, ip):
    """GForth core 2012: discard the second stack item."""
    x2 = inner.peek_ds_int(0)
    inner.pop_ds_int()
    inner.poke_ds_int(0, x2)
    return ip


# 2DROP ( x1 x2 -- )
def prim_2DROP(inner, cur, ip):
    """GForth core 2012: discard the top two stack items."""
    inner.pop_ds_int()
    inner.pop_ds_int()
    return ip


# 3DROP ( x1 x2 x3 -- )
def prim_3DROP(inner, cur, ip):
    inner.pop_ds_int()
    inner.pop_ds_int()
    inner.pop_ds_int()
    return ip


# SWAP ( x1 x2 -- x2 x1 )
def prim_SWAP(inner, cur, ip):
    """GForth core 2012: exchange the top two stack items."""
    a = inner.peek_ds_int(1)
    b = inner.peek_ds_int(0)
    inner.poke_ds_int(1, b)
    inner.poke_ds_int(0, a)
    return ip


# 2SWAP ( x1 x2 x3 x4 -- x3 x4 x1 x2 )
def prim_2SWAP(inner, cur, ip):
    """GForth core 2012: exchange the top two cell pairs."""
    a = inner.peek_ds_int(3)
    b = inner.peek_ds_int(2)
    c = inner.peek_ds_int(1)
    d = inner.peek_ds_int(0)
    inner.poke_ds_int(3, c)
    inner.poke_ds_int(2, d)
    inner.poke_ds_int(1, a)
    inner.poke_ds_int(0, b)
    return ip


# OVER ( x1 x2 -- x1 x2 x1 )
def prim_OVER(inner, cur, ip):
    """GForth core 2012: copy the second stack item to the top."""
    a = inner.peek_ds_int(1)
    inner.push_ds_int(a)
    return ip


# 2OVER ( x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2 )
def prim_2OVER(inner, cur, ip):
    """GForth core 2012: copy cell pair x1 x2 to the top of the stack."""
    a = inner.peek_ds_int(3)
    b = inner.peek_ds_int(2)
    inner.push_ds_int(a)
    inner.push_ds_int(b)
    return ip


# ROT ( x1 x2 x3 -- x2 x3 x1 )
def prim_ROT(inner, cur, ip):
    a = inner.peek_ds_int(2)
    b = inner.peek_ds_int(1)
    c = inner.peek_ds_int(0)
    inner.poke_ds_int(2, b)
    inner.poke_ds_int(1, c)
    inner.poke_ds_int(0, a)
    return ip


# -ROT ( x1 x2 x3 -- x3 x1 x2 )
def prim_NROT(inner, cur, ip):
    """Inverse of ROT."""
    a = inner.peek_ds_int(2)
    b = inner.peek_ds_int(1)
    c = inner.peek_ds_int(0)
    inner.poke_ds_int(2, c)
    inner.poke_ds_int(1, a)
    inner.poke_ds_int(0, b)
    return ip


# MAX ( n1 n2 -- n3 )
def prim_MAX(inner, cur, ip):
    """GForth core 2012: n3 is the greater of n1 and n2."""
    a, b = inner.top2_ds_int()
    if a < b:
        inner.push_ds_int(b)
    else:
        inner.push_ds_int(a)
    return ip


# MIN ( n1 n2 -- n3 )
def prim_MIN(inner, cur, ip):
    """GForth core 2012: n3 is the lesser of n1 and n2."""
    a, b = inner.top2_ds_int()
    if a < b:
        inner.push_ds_int(a)
    else:
        inner.push_ds_int(b)
    return ip


# DEPTH ( -- +n )
def prim_DEPTH(inner, cur, ip):
    """GForth core 2012: +n is the number of single-cell values contained in the data stack."""
    inner.push_ds_int(inner.ds_int_size())
    return ip


# RSHIFT ( n1 u -- n2 )
def prim_RSHIFT(inner, cur, ip):
    """GForth core 2012: logical (zero-fill) right shift of u bit-places."""
    from rpython.rlib.rarithmetic import intmask, r_uint
    a = inner.pop_ds_int()
    b = inner.pop_ds_int()
    if a >= LONG_BIT or a < 0:
        inner.push_ds_int(0)
    else:
        inner.push_ds_int(intmask(r_uint(b) >> a))
    return ip


# LSHIFT ( n1 u -- n2 )
def prim_LSHIFT(inner, cur, ip):
    """GForth core 2012: perform a logical left shift of u bit-places on n1, giving n2.
    The result is wrapped to a signed cell so values with the top bits set (e.g.
    random 48 LSHIFT in brainless hash codes) stay in range."""
    from rpython.rlib.rarithmetic import intmask, r_uint
    a = inner.pop_ds_int()
    b = inner.pop_ds_int()
    if a >= LONG_BIT or a < 0:
        inner.push_ds_int(0)
    else:
        inner.push_ds_int(intmask(r_uint(b) << a))
    return ip

# S>D ( n -- d )
def prim_S_TO_D(inner, cur, ip):
    """GForth core 2012: convert tne number n to double-cell number d."""
    a = inner.pop_ds_int()
    inner.push_ds_int(a)
    if a >= 0:
        inner.push_ds_int(0)
    else:
        inner.push_ds_int(-1)
    return ip


# D+ ( d1 d2 -- d3 )
def prim_D_PLUS(inner, cur, ip):
    """GForth double 2012: add d1 to d2, giving the sum d3."""
    d2_hi = inner.pop_ds_int()
    d2_lo = inner.pop_ds_int()
    d1_hi = inner.pop_ds_int()
    d1_lo = inner.pop_ds_int()
    # No word-width shift (1<<LONG_BIT = 0 translated); carry detected by unsigned overflow.
    ulo1 = r_uint(d1_lo)
    ulo2 = r_uint(d2_lo)
    lo_sum = ulo1 + ulo2
    carry = 1 if lo_sum < ulo1 else 0
    hi_sum = intmask(d1_hi + d2_hi + carry)
    inner.push_ds_int(intmask(lo_sum))
    inner.push_ds_int(hi_sum)
    return ip


# D- ( d1 d2 -- d3 )
def prim_D_MINUS(inner, cur, ip):
    """GForth double 2012: subtract d2 from d1, giving the difference d3."""
    d2_hi = inner.pop_ds_int()
    d2_lo = inner.pop_ds_int()
    d1_hi = inner.pop_ds_int()
    d1_lo = inner.pop_ds_int()
    # No word-width shift (1<<LONG_BIT = 0 translated); borrow detected by unsigned underflow.
    ulo1 = r_uint(d1_lo)
    ulo2 = r_uint(d2_lo)
    lo_diff = ulo1 - ulo2
    borrow = 1 if ulo2 > ulo1 else 0
    hi_diff = intmask(d1_hi - d2_hi - borrow)
    inner.push_ds_int(intmask(lo_diff))
    inner.push_ds_int(hi_diff)
    return ip


# D2/ ( d -- d/2 ) -- arithmetic right shift of a double by one bit.
def prim_D2SLASH(inner, cur, ip):
    d_hi = inner.pop_ds_int()
    d_lo = inner.pop_ds_int()
    # hi keeps its sign; its LSB becomes lo's MSB. No word-width shift (1<<LONG_BIT = 0 translated).
    new_hi = d_hi >> 1
    hi_lsb = r_uint(d_hi) & r_uint(1)
    ulo = r_uint(d_lo)
    new_lo = (ulo >> 1) | (hi_lsb << (LONG_BIT - 1))
    inner.push_ds_int(intmask(new_lo))
    inner.push_ds_int(new_hi)
    return ip


# D. ( d -- )
def prim_D_DOT(inner, cur, ip):
    """GForth double 2012: display d according to current BASE."""
    d_hi = inner.pop_ds_int()
    d_lo = inner.pop_ds_int()
    # No word-width shift (1<<LONG_BIT = 0 translated); _ud_divmod_base extracts digits.
    negative = d_hi < 0
    if negative:
        ulo = ~r_uint(d_lo)
        carry = 1 if ulo == r_uint(0) - r_uint(1) else 0
        ulo = ulo + r_uint(1)
        uhi = intmask(~d_hi + carry)
    else:
        ulo = r_uint(d_lo)
        uhi = d_hi
    buf = []
    lo = intmask(ulo)
    hi = uhi
    if lo == 0 and hi == 0:
        buf.append('0')
    else:
        while lo != 0 or hi != 0:
            lo, hi, digit = _ud_divmod_base(lo, hi, 10)
            buf.append(digit_to_char(digit))
    if negative:
        buf.append('-')
    buf.reverse()
    s = "".join(buf)
    stdin, stdout, stderr = create_stdio()
    stdout.write(s)
    stdout.write(' ')
    stdout.flush()
    return ip


# BL ( -- char )
def prim_BL(inner, cur, ip):
    """GForth core 2012: char is the character value of a space."""
    inner.push_ds_int(ord(' '))
    return ip

# 2* ( x1 -- x2 )
def prim_2STAR(inner, cur, ip):
    """GForth core 2012: x2 is the result of shifting x1 one bit toward the most-significant bit."""
    a = inner.pop_ds_int()
    inner.push_ds_int(a << 1)
    return ip

# 2/ ( x1 -- x2 )
def prim_2SLASH(inner, cur, ip):
    """GForth core 2012: x2 is the result of shifting x1 one bit right towards the least-significant bit."""
    a = inner.pop_ds_int()
    inner.push_ds_int(a >> 1)
    return ip

# Arithmetic

def int_shortcut(intval):
    if SMALL_INT_MIN <= intval < SMALL_INT_MAX:
        return _small_int_cache[intval - SMALL_INT_MIN]
    else:
        return W_IntObject(intval)

# + ( n1 n2 -- n3 )
def prim_ADD(inner, cur, ip):
    """GForth core 2012: add n1 and n2, leaving their sum."""
    a, b = inner.top2_ds_int()
    inner.push_ds_int(a + b)
    return ip


# - ( n1 n2 -- n3 )
def prim_SUB(inner, cur, ip):
    """GForth core 2012: subtract n2 from n1, leaving the difference."""
    a, b = inner.top2_ds_int()
    inner.push_ds_int(a - b)
    return ip


# * ( n1 n2 -- n3 )
def prim_MUL(inner, cur, ip):
    """GForth core 2012: multiply n1 by n2, leaving the product."""
    a, b = inner.top2_ds_int()
    inner.push_ds_int(a * b)
    return ip

# / ( n1 n2 -- n3 )
def prim_DIV(inner, cur, ip):
    """GForth core 2012: divide n1 by n2, giving the single-cell quotient n3."""
    a, b = inner.top2_ds_int()
    assert b != 0, "Division by zero"
    inner.push_ds_int(a // b)
    return ip

# */ ( n1 n2 n3 -- n4 )
def prim_MUL_SLASH(inner, cur, ip):
    """GForth core 2012: multiply n1 by n2 producing the intermediate double-cell result d. divide d by n3 giving the single-cell quotient n4."""
    n3 = inner.pop_ds_int()
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    d = n1 * n2
    assert n3 != 0, "Division by zero"
    result = d // n3
    assert (result >> LONG_BIT) == 0 or (result >> LONG_BIT) == -1, "Overflow in */"
    inner.push_ds_int(result)
    return ip

# /MOD ( n1 n2 -- n3 n4 )
def prim_DIV_MOD(inner, cur, ip):
    """GForth core 2012: divide n1 by n2, giving the single-cell remainder n3 and the single-cell quotient n4."""
    a, b = inner.top2_ds_int()
    assert b != 0, "Division by zero"
    inner.push_ds_int(a % b)
    inner.push_ds_int(a // b)
    return ip

# */MOD ( n1 n2 n3 -- n4 n5 )
def prim_MUL_DIV_MOD(inner, cur, ip):
    """GForth core 2012: multiply n1 by n2 producing the intermediate double-cell result d. divide d by n3 giving the single-cell remainder n4 and the single-cell quotient n5."""
    n3 = inner.pop_ds_int()
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    d = n1 * n2
    assert n3 != 0, "Division by zero"
    q = d // n3
    assert (q >> LONG_BIT) == 0 or (q >> LONG_BIT) == -1, "Overflow in */mod"
    inner.push_ds_int(d % n3)
    inner.push_ds_int(q)
    return ip

# ABS ( n -- u )
def prim_ABS(inner, cur, ip):
    """GForth core 2012: u is the absolute value of n."""
    a = inner.pop_ds_int()
    inner.push_ds_int(abs(a))
    return ip


# NEGATE ( n1 -- n2 )
def prim_NEGATE(inner, cur, ip):
    """GForth core 2012: negate n1, giving its arithmetic inverse n2."""
    a = inner.pop_ds_int()
    inner.push_ds_int(-a)
    return ip


# MOD ( n1 n2 -- n3 )
def prim_MOD(inner, cur, ip):
    """GForth core 2012: divide n1 by n2, giving the single-cell remainder n3."""
    a, b = inner.top2_ds_int()
    inner.push_ds_int(a % b)
    return ip


# /MOD ( n1 n2 -- n3 n4 )
def prim_DIVMOD(inner, cur, ip):
    """GForth core 2012: divide n1 by n2, giving remainder n3 and quotient n4."""
    a, b = inner.top2_ds_int()
    # Symmetric division: quotient rounds toward zero.
    q = int(a / b)
    r = a - (q * b)
    inner.push_ds_int(r)
    inner.push_ds_int(q)
    return ip


# */ ( n1 n2 n3 -- n4 )
def prim_STARSLASH(inner, cur, ip):
    """GForth core 2012: multiply n1 by n2 producing intermediate double-cell result, divide by n3."""
    n3 = inner.pop_ds_int()
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    product = n1 * n2
    result = int(product / n3)
    inner.push_ds_int(result)
    return ip


# */MOD ( n1 n2 n3 -- n4 n5 )
def prim_STARSLASHMOD(inner, cur, ip):
    """GForth core 2012: multiply n1 by n2, divide by n3, giving remainder n4 and quotient n5."""
    n3 = inner.pop_ds_int()
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    product = n1 * n2
    q = int(product / n3)
    r = product - (q * n3)
    inner.push_ds_int(r)
    inner.push_ds_int(q)
    return ip


# FM/MOD ( d n1 -- n2 n3 )
def prim_FMSLASHMOD(inner, cur, ip):
    """GForth core 2012: floored division of double d by n1, giving remainder n2 and quotient n3."""
    n1 = inner.pop_ds_int()
    d_hi = inner.pop_ds_int()
    d_lo = inner.pop_ds_int()

    dividend = d_lo
    divisor = n1

    if divisor == 0:
        inner.push_ds_int(0)
        inner.push_ds_int(0)
        return ip

    q = dividend // divisor
    r = dividend - (q * divisor)

    inner.push_ds_int(r)
    inner.push_ds_int(q)
    return ip


# SM/REM ( d n1 -- n2 n3 )
def prim_SMSLASHREM(inner, cur, ip):
    """GForth core 2012: symmetric division of double d by n1, giving remainder n2 and quotient n3."""
    n1 = inner.pop_ds_int()
    d_hi = inner.pop_ds_int()
    d_lo = inner.pop_ds_int()

    dividend = d_lo
    divisor = n1

    if divisor == 0:
        inner.push_ds_int(0)
        inner.push_ds_int(0)
        return ip

    # Symmetric division: quotient rounds toward zero.
    q = int(dividend / divisor)
    r = dividend - (q * divisor)

    inner.push_ds_int(r)
    inner.push_ds_int(q)
    return ip


# UM/MOD ( ud u1 -- u2 u3 )
def prim_UMSLASHMOD(inner, cur, ip):
    """GForth core 2012: unsigned division of double ud by u1, giving remainder u2 and quotient u3."""
    u1 = inner.pop_ds_int()
    ud_hi = inner.pop_ds_int()
    ud_lo = inner.pop_ds_int()

    if u1 == 0:
        inner.push_ds_int(0)
        inner.push_ds_int(0)
        return ip

    # Full 128-bit unsigned divide; avoids word-width shifts (BIT_MASK=-1, hi<<LONG_BIT=0 translated).
    qlo, qhi, rem = _ud_divmod_base(ud_lo, ud_hi, u1)
    inner.push_ds_int(rem)
    inner.push_ds_int(qlo)
    return ip


# 1+ ( n1 -- n2 )
def prim_INC(inner, cur, ip):
    """GForth core 2012: add one to n1."""
    a = inner.pop_ds_int()
    inner.push_ds_int(a + 1)
    return ip


# 1- ( n1 -- n2 )
def prim_DEC(inner, cur, ip):
    """GForth core 2012: subtract one from n1."""
    a = inner.pop_ds_int()
    inner.push_ds_int(a - 1)
    return ip


# M* ( n1 n2 -- d )
def prim_MUL_STAR(inner, cur, ip):
    """GForth core 2012: d is the signed product of n1 times n2."""
    a, b = inner.top2_ds_int()
    # Signed 128-bit product via unsigned mulhi (no word-width shift, no BIT_MASK=-1).
    ua = r_uint(a)
    ub = r_uint(b)
    HALF = r_uint(32)
    LOMASK = r_uint(0xFFFFFFFF)
    a_lo = ua & LOMASK
    a_hi = ua >> HALF
    b_lo = ub & LOMASK
    b_hi = ub >> HALF
    ll = a_lo * b_lo
    lh = a_lo * b_hi
    hl = a_hi * b_lo
    hh = a_hi * b_hi
    mid = lh + hl
    lo = ll + (mid << HALF)
    carry_mid = r_uint(1) if lo < ll else r_uint(0)
    carry_lh = r_uint(1) if mid < lh else r_uint(0)
    uhi = hh + (mid >> HALF) + (carry_lh << HALF) + carry_mid
    if a < 0:
        uhi = uhi - ub
    if b < 0:
        uhi = uhi - ua
    inner.push_ds_int(intmask(lo))
    inner.push_ds_int(intmask(uhi))
    return ip

# UM* ( u1 u2 -- ud )
def prim_U_MUL_STAR(inner, cur, ip):
    """GForth core 2012: multiply u1 by u2, giving the unsigned double-cell product ud."""
    a, b = inner.top2_ds_int()
    # 32-bit-halves unsigned mulhi: no word-width shift.
    ua = r_uint(a)
    ub = r_uint(b)
    HALF = r_uint(32)
    LOMASK = r_uint(0xFFFFFFFF)
    a_lo = ua & LOMASK
    a_hi = ua >> HALF
    b_lo = ub & LOMASK
    b_hi = ub >> HALF
    ll = a_lo * b_lo
    lh = a_lo * b_hi
    hl = a_hi * b_lo
    hh = a_hi * b_hi
    mid = lh + hl
    lo = ll + (mid << HALF)
    carry_mid = r_uint(1) if lo < ll else r_uint(0)
    carry_lh = r_uint(1) if mid < lh else r_uint(0)
    hi = hh + (mid >> HALF) + (carry_lh << HALF) + carry_mid
    inner.push_ds_int(intmask(lo))
    inner.push_ds_int(intmask(hi))
    return ip


# M*/ ( d1 n1 +n2 -- d2 )
def prim_MSTARSLASH(inner, cur, ip):
    """GForth tools-ext 2012: multiply signed double d1 by signed single n1 to a
    triple-cell intermediate, then floored-divide by positive single n2, giving
    the signed double quotient d2."""
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    d1_hi = inner.pop_ds_int()
    d1_lo = inner.pop_ds_int()

    if n2 == 0:
        inner.push_ds_int(0)
        inner.push_ds_int(0)
        return ip

    LOMASK = r_uint(0xFFFFFFFF)
    HALF = r_uint(32)

    neg = False
    if d1_hi < 0:
        neg = not neg
    if n1 < 0:
        neg = not neg

    if d1_hi < 0:
        tlo = ~r_uint(d1_lo)
        carry = r_uint(1) if tlo == r_uint(0) - r_uint(1) else r_uint(0)
        ulo = tlo + r_uint(1)
        uhi = ~r_uint(d1_hi) + carry
    else:
        ulo = r_uint(d1_lo)
        uhi = r_uint(d1_hi)

    if n1 < 0:
        un1 = r_uint(0) - r_uint(n1)
    else:
        un1 = r_uint(n1)

    # n2 is positive by spec, but handle negative to flip sign.
    if n2 < 0:
        un2 = r_uint(0) - r_uint(n2)
        neg = not neg
    else:
        un2 = r_uint(n2)

    a0 = ulo & LOMASK
    a1 = ulo >> HALF
    a2 = uhi & LOMASK
    a3 = uhi >> HALF
    b0 = un1 & LOMASK
    b1 = un1 >> HALF
    p = [r_uint(0), r_uint(0), r_uint(0), r_uint(0), r_uint(0), r_uint(0)]
    aa = [a0, a1, a2, a3]
    bb = [b0, b1]
    i = 0
    while i < 4:
        carry = r_uint(0)
        j = 0
        while j < 2:
            acc = p[i + j] + aa[i] * bb[j] + carry
            p[i + j] = acc & LOMASK
            carry = acc >> HALF
            j += 1
        p[i + 2] = p[i + 2] + carry
        i += 1

    chunks = [p[5], p[4], p[3], p[2], p[1], p[0]]
    q = [r_uint(0), r_uint(0), r_uint(0), r_uint(0), r_uint(0), r_uint(0)]
    r = r_uint(0)
    k = 0
    while k < 6:
        acc = (r << HALF) | chunks[k]
        q[k] = acc / un2
        r = acc % un2
        k += 1

    qlo = q[5] | (q[4] << HALF)
    qhi = q[3] | (q[2] << HALF)

    if neg:
        nlo = ~qlo
        c = r_uint(1) if nlo == r_uint(0) - r_uint(1) else r_uint(0)
        nlo = nlo + r_uint(1)
        nhi = ~qhi + c
        if r != r_uint(0):
            if nlo == r_uint(0):
                nlo = r_uint(0) - r_uint(1)
                nhi = nhi - r_uint(1)
            else:
                nlo = nlo - r_uint(1)
        qlo = nlo
        qhi = nhi

    inner.push_ds_int(intmask(qlo))
    inner.push_ds_int(intmask(qhi))
    return ip


# AND ( x1 x2 -- x3 )
def prim_AND(inner, cur, ip):
    """GForth core 2012: x3 is the bit-by-bit logical "and" of x1 with x2."""
    a, b = inner.top2_ds_int()
    inner.push_ds_int(a & b)
    return ip


# OR ( x1 x2 -- x3 )
def prim_OR(inner, cur, ip):
    """GForth core 2012: x3 is the bit-by-bit inclusive-or of x1 with x2."""
    a, b = inner.top2_ds_int()
    inner.push_ds_int(a | b)
    return ip

# XOR ( x1 x2 -- x3 )
def prim_XOR(inner, cur, ip):
    """GForth core 2012: x3 is the bit-by-bit exclusive-or of x1 with x2."""
    a, b = inner.top2_ds_int()
    inner.push_ds_int(a ^ b)
    return ip


# FM/MOD ( d1 n1 -- n2 n3 )
def prim_FM_DIV_MOD(inner, cur, ip):
    """GForth core 2012: divide d1 by n1, giving the floored quotient n3 and the remainder n2."""
    n1 = inner.pop_ds_int()
    d_hi = inner.pop_ds_int()
    d_lo = inner.pop_ds_int()
    assert n1 != 0, "Division by zero"
    d_negative = d_hi < 0
    if d_negative:
        ulo = ~r_uint(d_lo)
        carry = r_uint(1) if ulo == r_uint(0) - r_uint(1) else r_uint(0)
        ulo = ulo + r_uint(1)
        uhi = intmask(~d_hi + intmask(carry))
    else:
        ulo = r_uint(d_lo)
        uhi = d_hi
    n1_negative = n1 < 0
    un1 = r_uint(-n1 if n1_negative else n1)
    # 128-bit unsigned divide |d1| by |n1|; BIT_MASK and SIGN_BIT avoided via _ud_divmod_base.
    qlo, qhi, rem_u = _ud_divmod_base(intmask(ulo), uhi, intmask(un1))
    rem = -rem_u if d_negative else rem_u
    quot = -qlo if (d_negative ^ n1_negative) else qlo
    # Floored: adjust when remainder != 0 and signs differ.
    if rem != 0 and (d_negative ^ n1_negative):
        quot = quot - 1
        rem = rem + n1
    inner.push_ds_int(rem)
    inner.push_ds_int(quot)
    return ip

# UM/MOD ( ud u1 -- u2 u3 )
def prim_UM_DIV_MOD(inner, cur, ip):
    """GForth core 2012: divide ud by u1, giving the quotient u3 and the remainder u2."""
    u1 = inner.pop_ds_int()
    ud_hi = inner.pop_ds_int()
    ud_lo = inner.pop_ds_int()
    assert u1 != 0, "Division by zero"
    # No word-width shift (b<<LONG_BIT=0) and no SIGN_BIT correction (1<<LONG_BIT=0).
    qlo, qhi, rem = _ud_divmod_base(ud_lo, ud_hi, u1)
    inner.push_ds_int(rem)
    inner.push_ds_int(qlo)
    return ip

# SM/REM ( d1 n1 -- n2 n3 )
def prim_SM_DIV_REM(inner, cur, ip):
    """GForth core 2012: divide d1 by n1, giving the symmetric quotient n3 and the remainder n2."""
    a = inner.pop_ds_int()
    b = inner.pop_ds_int()
    c = inner.pop_ds_int()
    assert a != 0, "Division by zero"
    # Do NOT reassemble via (b<<LONG_BIT)|c: word-width int_lshift makes the JIT infer contradictory {0,-1} bounds and abort enclosing loops ("two integer ranges don't overlap").
    d = c
    a_abs = abs(a)
    d_abs = abs(d)
    e = d_abs // a_abs
    f = d_abs % a_abs
    if (d < 0) ^ (a < 0):
        e = -e
    if d < 0:
        f = -f
    inner.push_ds_int(f)
    inner.push_ds_int(e)
    return ip


# INVERT ( x1 -- x2 )
def prim_INVERT(inner, cur, ip):
    """GForth core 2012: invert all bits of x1, giving x2 (one's complement)."""
    x = inner.pop_ds_int()
    inner.push_ds_int(~x)
    return ip


# U< ( u1 u2 -- flag )
def prim_ULESS(inner, cur, ip):
    """GForth core 2012: flag is true if and only if u1 is less than u2 (unsigned comparison)."""
    u2 = inner.pop_ds_int()
    u1 = inner.pop_ds_int()
    if r_uint(u1) < r_uint(u2):
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip

# memory management


# ! ( x addr -- )
def prim_STORE(inner, cur, ip):
    """GForth core 2012: store x at cell address addr."""
    addr = inner.pop_ds_int()
    val = inner.pop_ds_int()
    inner.cell_store(addr, val)
    # A store to the sentinel BASE cell also updates the parsing radix immediately.
    if inner.outer is not None and addr == inner.outer.base_addr and val >= 2:
        inner.base = val
    return ip


# ! ( x1 x2 a-addr -- )
def prim_2STORE(inner, cur, ip):
    """Store the cell pair x1 x2 at a-addr (x2 at a-addr, x1 at the next cell);
    equivalent to SWAP OVER ! CELL+ !."""
    addr = inner.pop_ds_int()
    x2 = inner.pop_ds_int()
    x1 = inner.pop_ds_int()
    inner.cell_2store(addr, x1, x2)
    return ip

# @ ( addr -- x )
def prim_FETCH(inner, cur, ip):
    """GForth core 2012: fetch the cell contents at addr."""
    addr = inner.pop_ds_int()
    inner.push_ds_int(inner.cell_fetch_int(addr))
    return ip


# ( -- n )
def prim_CELL(inner, cur, ip):
    """push the size of one cell in address units."""
    inner.push_ds_int(CELL_SIZE_BYTES)
    return ip


# FLOAT ( -- n )
def prim_FLOAT(inner, cur, ip):
    """GForth floating 2012: return the size of one float in address units."""
    inner.push_ds_int(8)
    return ip


# FLOATS ( n1 -- n2 )
def prim_FLOATS(inner, cur, ip):
    """GForth floating 2012: convert  float count to address units."""
    n = inner.pop_ds_int()
    inner.push_ds_int(n * 8)
    return ip


# DFLOATS ( n1 -- n2 ) -- dfloats are 8 bytes on 64-bit (FLOATING-EXT).
def prim_DFLOATS(inner, cur, ip):
    n = inner.pop_ds_int()
    inner.push_ds_int(n * 8)
    return ip


# SFLOATS ( n1 -- n2 ) -- sfloats are 4 bytes (FLOATING-EXT).
def prim_SFLOATS(inner, cur, ip):
    n = inner.pop_ds_int()
    inner.push_ds_int(n * 4)
    return ip


# FALIGNED ( addr -- f-addr ) -- align to a float boundary (8 bytes here).
def prim_FALIGNED(inner, cur, ip):
    addr = inner.pop_ds_int()
    remainder = addr % 8
    if remainder != 0:
        addr += (8 - remainder)
    inner.push_ds_int(addr)
    return ip


# DFALIGNED ( addr -- dfaddr ) -- align to a dfloat boundary (8 bytes).
def prim_DFALIGNED(inner, cur, ip):
    addr = inner.pop_ds_int()
    remainder = addr % 8
    if remainder != 0:
        addr += (8 - remainder)
    inner.push_ds_int(addr)
    return ip


# SFALIGNED ( addr -- sfaddr ) -- align to an sfloat boundary (4 bytes).
def prim_SFALIGNED(inner, cur, ip):
    addr = inner.pop_ds_int()
    remainder = addr % 4
    if remainder != 0:
        addr += (4 - remainder)
    inner.push_ds_int(addr)
    return ip


# ( n -- n )
def prim_CELLPLUS(inner, cur, ip):
    """GForth core 2012: add one cell to an address."""
    addr = inner.pop_ds_int()
    inner.push_ds_int(addr + CELL_SIZE_BYTES)
    return ip


# ( n -- n * cell_size )
def prim_CELLS(inner, cur, ip):
    """GForth core 2012: convert a cell count to address units."""
    count = inner.pop_ds_int()
    inner.push_ds_int(count * CELL_SIZE_BYTES)
    return ip


# IF THEN ELSE


# 0BRANCH ( flag -- )
def prim_0BRANCH(inner, cur, ip):
    """GForth core 2012: branch to target when flag is zero."""
    origin_ip = ip - 1
    x = inner.pop_ds_int()
    if x == 0:
        w_target = promote(cur.lits[origin_ip])
        assert isinstance(w_target, W_IntObject)
        target_ip = promote(w_target.intval)
        ip = target_ip
        _maybe_enter_jit(inner, target_ip, origin_ip, cur)
    return ip


# BRANCH ( -- )
def prim_BRANCH(inner, cur, ip):
    """GForth core 2012: branch unconditionally to the target."""
    origin_ip = ip - 1
    target = promote(cur.lits[origin_ip])
    assert isinstance(target, W_IntObject)
    target_ip = promote(target.intval)
    ip = target_ip
    _maybe_enter_jit(inner, target_ip, origin_ip, cur)
    return ip


# Loop control primitives

# (DO) ( limit start -- )
def prim_DO_RUNTIME(inner, cur, ip):
    """Runtime for DO: pop limit and start from data stack, push to loop stack."""
    start = inner.pop_ds_int()
    limit = inner.pop_ds_int()
    inner.push_loop(limit, start)
    return ip


# (?DO) ( limit start -- )
def prim_QDO_RUNTIME(inner, cur, ip):
    """Runtime for ?DO: like (DO), but skip the loop body when limit == start
    (branching to the patched loop end, with no loop parameters pushed)."""
    start = inner.pop_ds_int()
    limit = inner.pop_ds_int()
    if limit == start:
        target = cur.lits[ip - 1]
        assert isinstance(target, W_IntObject)
        ip = target.intval
    else:
        inner.push_loop(limit, start)
    return ip


# (LOOP) ( -- ) ( R: limit counter -- limit counter+1 | ) -- promoting; runtime limits use (LOOPNP) to avoid bridge storms.
def prim_LOOP_RUNTIME(inner, cur, ip):
    counter_val = inner.peek_loop_counter(0)
    limit_val = promote(inner.peek_loop_limit(0))
    new_counter_val = counter_val + 1

    if new_counter_val < limit_val:
        inner.set_loop_counter(0, new_counter_val)
        origin_ip = ip - 1
        target = promote(cur.lits[origin_ip])
        assert isinstance(target, W_IntObject)
        target_ip = promote(target.intval)
        ip = target_ip
        _maybe_enter_jit(inner, target_ip, origin_ip, cur)
    else:
        inner.pop_loop()
    return ip


# (LOOPNP) ( -- ) ( R: limit counter -- limit counter+1 | ) -- non-promoting; distinct limit per activation does not fail guard_value and spawn a bridge.
def prim_LOOPNP_RUNTIME(inner, cur, ip):
    counter_val = inner.peek_loop_counter(0)
    limit_val = inner.peek_loop_limit(0)
    new_counter_val = counter_val + 1

    if new_counter_val < limit_val:
        inner.set_loop_counter(0, new_counter_val)
        origin_ip = ip - 1
        target = promote(cur.lits[origin_ip])
        assert isinstance(target, W_IntObject)
        target_ip = promote(target.intval)
        ip = target_ip
        _maybe_enter_jit(inner, target_ip, origin_ip, cur)
    else:
        inner.pop_loop()
    return ip

# (+LOOP) ( n -- ) ( R: limit counter -- limit counter+n | )
def prim_PLUSLOOP_RUNTIME(inner, cur, ip):
    """Runtime for +LOOP: increment counter by n and conditionally branch back."""
    inc_val = inner.pop_ds_int()

    # +LOOP limits not promoted: promoting would specialize per limit value, storming bridges on megamorphic scans.
    counter_val = inner.peek_loop_counter(0)
    limit_val = inner.peek_loop_limit(0)
    new_counter_val = counter_val + inc_val

    continue_loop = False
    if inc_val >= 0:
        if counter_val < limit_val and new_counter_val < limit_val:
            continue_loop = True
        elif counter_val >= limit_val:
            continue_loop = False
    else:
        if counter_val >= limit_val and new_counter_val >= limit_val:
            continue_loop = True

    if continue_loop:
        inner.set_loop_counter(0, new_counter_val)
        origin_ip = ip - 1
        target = promote(cur.lits[origin_ip])
        assert isinstance(target, W_IntObject)
        target_ip = promote(target.intval)
        ip = target_ip
        _maybe_enter_jit(inner, target_ip, origin_ip, cur)
    else:
        inner.pop_loop()
    return ip


# UNLOOP ( -- ) ( R: limit counter -- )
def prim_UNLOOP(inner, cur, ip):
    """GForth core 2012: discard loop parameters from return stack."""
    inner.pop_loop()
    return ip


# LEAVE ( -- ) ( R: limit counter -- )
def prim_LEAVE(inner, cur, ip):
    """Exit the current loop by cleaning up return stack and jumping to end."""
    inner.pop_loop()
    target = cur.lits[ip - 1]
    assert isinstance(target, W_IntObject)
    ip = target.intval
    return ip

# I ( -- n ) ( R: limit counter -- limit counter )
def prim_I(inner, cur, ip):
    """Get the current loop counter (innermost loop)."""
    counter_val = inner.peek_loop_counter(0)
    inner.push_ds_int(counter_val)
    return ip


# J ( -- n ) ( R: limit1 counter1 limit2 counter2 -- limit1 counter1 limit2 counter2 )
def prim_J(inner, cur, ip):
    """Get the outer loop counter (second innermost loop)."""
    counter_val = inner.peek_loop_counter(1)
    inner.push_ds_int(counter_val)
    return ip


# BASE


# BASE@ ( -- u )
def prim_BASE_FETCH(inner, cur, ip):
    """GForth core 2012: return the current conversion base."""
    inner.push_ds_int(inner.base)
    return ip


# BASE! ( u -- )
def prim_BASE_STORE(inner, cur, ip):
    """GForth core 2012: set the conversion base to u."""
    u = inner.pop_ds_int()
    inner.set_base(u)
    return ip


# DECIMAL ( -- )
def prim_DECIMAL(inner, cur, ip):
    """GForth core 2012: set BASE to decimal (radix 10)."""
    inner.set_base(10)
    return ip


# HEX ( -- )
def prim_HEX(inner, cur, ip):
    """GForth core 2012: set BASE to hexadecimal (radix 16)."""
    inner.set_base(16)
    return ip


# OCTAL ( -- )
def prim_OCTAL(inner, cur, ip):
    """GForth core 2012: set BASE to octal (radix 8)."""
    inner.set_base(8)
    return ip


# BINARY ( -- )
def prim_BINARY(inner, cur, ip):
    """GForth core 2012: set BASE to binary (radix 2)."""
    inner.set_base(2)
    return ip


# <# ( -- )
def prim_LESSNUM(inner, cur, ip):
    """GForth core 2012: begin pictured numeric output conversion."""
    inner._pno_active = True
    inner._pno_buf = []
    return ip


def _ud_divmod_base(lo, hi, base):
    """Divide the unsigned double (lo, hi) by base; return (qlo, qhi, digit).
    128-by-64 long division done in 32-bit halves so every intermediate fits
    an unsigned machine word."""
    ulo = r_uint(lo)
    uhi = r_uint(hi)
    ubase = r_uint(base)
    qhi = uhi / ubase
    r = uhi % ubase
    t1 = (r << 32) | (ulo >> 32)
    q1 = t1 / ubase
    r1 = t1 % ubase
    t2 = (r1 << 32) | (ulo & r_uint(0xFFFFFFFF))
    q2 = t2 / ubase
    r2 = t2 % ubase
    qlo = (q1 << 32) | q2
    return intmask(qlo), intmask(qhi), intmask(r2)


# # ( ud1 -- ud2 )
def prim_NUMSIGN(inner, cur, ip):
    """GForth core 2012: extract one digit during pictured numeric output."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("# outside <# #>"))
        return ip
    hi = inner.pop_ds_int()
    lo = inner.pop_ds_int()
    qlo, qhi, digit = _ud_divmod_base(lo, hi, inner.base)
    inner._pno_buf.insert(0, digit_to_char(digit))
    inner.push_ds_int(qlo)
    inner.push_ds_int(qhi)
    return ip


# #S ( ud -- 0 0 )
def prim_NUMSIGN_S(inner, cur, ip):
    """GForth core 2012: convert all remaining digits during pictured numeric output."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("#S outside <# #>"))
        return ip
    hi = inner.pop_ds_int()
    lo = inner.pop_ds_int()
    base = inner.base
    while True:
        lo, hi, digit = _ud_divmod_base(lo, hi, base)
        inner._pno_buf.insert(0, digit_to_char(digit))
        if lo == 0 and hi == 0:
            break
    inner.push_ds_int(0)
    inner.push_ds_int(0)
    return ip


# HOLD ( char -- )
def prim_HOLD(inner, cur, ip):
    """GForth core 2012: insert character into pictured numeric output buffer."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("HOLD outside <# #>"))
        return ip
    ch = inner.pop_ds_int()
    inner._pno_buf.insert(0, chr(ch))
    return ip


# SIGN ( n -- )
def prim_SIGN(inner, cur, ip):
    """GForth core 2012: add a minus sign to pictured numeric output if n is negative."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("SIGN outside <# #>"))
        return ip
    n = inner.pop_ds_int()
    if n < 0:
        inner._pno_buf.insert(0, '-')
    return ip


# #> ( xd -- c-addr u )
def prim_NUMGREATER(inner, cur, ip):
    """GForth core 2012: finish pictured numeric output and deliver the string as
    ( c-addr u ). The digits are materialized in char memory so downstream ANS
    code can do length arithmetic (lexex savetables.fth writeNumber.R) and hand
    the pair to WRITE-FILE. alloc_buf also registers a buf slot, so the boxed-
    string consumers (TYPE, S"-style) keep seeing the same characters."""
    if not inner._pno_active:
        inner.print_str(W_StringObject("#> outside <# #>"))
        return ip
    inner.pop_ds_int()
    inner.pop_ds_int()
    s = "".join(inner._pno_buf)
    inner._pno_active = False
    c_addr = inner.alloc_buf(s, len(s))
    inner.push_ds_int(c_addr)
    inner.push_ds_int(len(s))
    return ip


# TYPE ( c-addr u -- )
@dont_look_inside
def prim_TYPE(inner, cur, ip):
    """GForth core 2012: display the character string. Two calling forms coexist:
    ." / S" (in the boxed path) leave a W_StringObject on the object stack; ANS
    code (e.g. brainless load-part) leaves ( c-addr u ) on the int stack pointing
    at char memory. Prefer the boxed object when present, else read u chars."""
    if inner.ds_ptr_locals > 0:
        w_s = inner.pop_ds()
        inner.print_str(w_s)
        return ip
    u = inner.pop_ds_int()
    c_addr = inner.pop_ds_int()
    if u < 0:
        u = 0
    chars = []
    for k in range(u):
        chars.append(chr(inner.char_fetch(c_addr + k)))
    inner.print_str(W_StringObject("".join(chars)))
    return ip


# I/O


# . ( n -- )
@dont_look_inside
def prim_DOT(inner, cur, ip):
    """GForth core 2012: display n according to current BASE."""
    x = inner.pop_ds_int()
    stdin, stdout, stderr = create_stdio()
    stdout.write(str(x))
    stdout.write(' ')
    return ip

# .S ( -- )
@dont_look_inside
def prim_DOT_S(inner, cur, ip):
    """Print all int stack items non-destructively, gforth format: <n> v1 v2 ... TOS """
    depth = inner.depth_ds_int()
    stdin, stdout, stderr = create_stdio()
    stdout.write('<')
    stdout.write(str(depth))
    stdout.write('> ')
    i = depth - 1
    while i >= 0:
        stdout.write(str(inner.peek_ds_int(i)))
        stdout.write(' ')
        i -= 1
    stdout.flush()
    return ip


@dont_look_inside
def prim_U_DOT(inner, cur, ip):
    """GForth core 2012: display u in field format according to current BASE."""
    x = inner.pop_ds_int()
    # r_uint treats the signed cell as unsigned; avoids BIT_MASK=-1 translated.
    lo, hi, digit = _ud_divmod_base(x, 0, inner.base)
    buf = [digit_to_char(digit)]
    while lo != 0 or hi != 0:
        lo, hi, digit = _ud_divmod_base(lo, hi, inner.base)
        buf.append(digit_to_char(digit))
    buf.reverse()
    s = "".join(buf)
    stdin, stdout, stderr = create_stdio()
    stdout.write(s)
    stdout.write(' ')
    return ip


# EMIT ( char -- )
@dont_look_inside
def prim_EMIT(inner, cur, ip):
    """GForth core 2012: display character with char code."""
    x = inner.pop_ds_int()
    stdin, stdout, stderr = create_stdio()
    stdout.write(chr(x))
    stdout.flush()
    return ip


# SPACE ( -- )
@dont_look_inside
def prim_SPACE(inner, cur, ip):
    """GForth core 2012: display one space."""
    stdin, stdout, stderr = create_stdio()
    stdout.write(' ')
    return ip


# SPACES ( n -- )
@dont_look_inside
def prim_SPACES(inner, cur, ip):
    """GForth core 2012: display n spaces."""
    count = inner.pop_ds_int()
    if count > 0:
        stdin, stdout, stderr = create_stdio()
        for i in range(count):
            stdout.write(' ')
    return ip


# CR ( -- )
@dont_look_inside
def prim_CR(inner, cur, ip):
    """GForth core 2012: cause subsequent output to appear at the beginning of the next line."""
    stdin, stdout, stderr = create_stdio()
    stdout.write('\n')
    stdout.flush()
    return ip


# U. ( u -- )
@dont_look_inside
def prim_UDOT(inner, cur, ip):
    """GForth core 2012: display u as unsigned according to current BASE."""
    x = inner.pop_ds_int()
    # r_uint avoids BIT_MASK = (1<<LONG_BIT)-1 = -1 translated.
    lo, hi, digit = _ud_divmod_base(x, 0, inner.base)
    buf = [digit_to_char(digit)]
    while lo != 0 or hi != 0:
        lo, hi, digit = _ud_divmod_base(lo, hi, inner.base)
        buf.append(digit_to_char(digit))
    buf.reverse()
    s = "".join(buf)
    stdin, stdout, stderr = create_stdio()
    stdout.write(s)
    stdout.write(' ')
    stdout.flush()
    return ip


# KEY ( -- char )
@dont_look_inside
def prim_KEY(inner, cur, ip):
    """GForth core 2012: receive one character from input device."""
    stdin, stdout, stderr = create_stdio()
    ch = stdin.read(1)
    if len(ch) > 0:
        inner.push_ds_int(ord(ch[0]))
    else:
        inner.push_ds_int(0)
    return ip


# ACCEPT ( c-addr +n1 -- +n2 )
@dont_look_inside
def prim_ACCEPT(inner, cur, ip):
    """GForth core 2012: receive a string of at most +n1 characters from input."""
    max_count = inner.pop_ds_int()
    addr = inner.pop_ds_int()

    stdin, stdout, stderr = create_stdio()
    line = stdin.readline()
    line_len = len(line)
    if line_len > 0 and line[line_len - 1] == '\n':
        new_len = line_len - 1
        assert new_len >= 0  # Help RPython prove non-negative
        line = line[:new_len]

    line_len = len(line)
    if line_len > max_count and max_count >= 0:
        line = line[:max_count]

    final_len = len(line)
    for j in range(final_len):
        inner.char_store(addr+j, ord(line[j]))

    inner.push_ds_int(final_len)
    return ip

# U.R ( u n -- )
@dont_look_inside
def prim_UDOTR(inner, cur, ip):
    """Display unsigned number right-justified in n-character field."""
    n = inner.pop_ds_int()
    u = inner.pop_ds_int()

    # r_uint, no BIT_MASK.
    lo, hi, digit = _ud_divmod_base(u, 0, 10)
    buf = [digit_to_char(digit)]
    while lo != 0 or hi != 0:
        lo, hi, digit = _ud_divmod_base(lo, hi, 10)
        buf.append(digit_to_char(digit))
    buf.reverse()
    num_str = "".join(buf)
    width = n

    stdin, stdout, stderr = create_stdio()
    if len(num_str) < width:
        stdout.write(' ' * (width - len(num_str)))
    stdout.write(num_str)
    stdout.flush()
    return ip

# .R ( n1 n2 -- ) -- display signed n1 right-justified in an n2-character field.
@dont_look_inside
def prim_DOTR(inner, cur, ip):
    n = inner.pop_ds_int()
    x = inner.pop_ds_int()
    num_str = str(x)
    stdin, stdout, stderr = create_stdio()
    pad = n - len(num_str)
    if pad > 0:
        stdout.write(' ' * pad)
    stdout.write(num_str)
    stdout.flush()
    return ip


# KEY? ( -- flag ) -- batch driver has no terminal; always reports none available.
@dont_look_inside
def prim_KEY_QUESTION(inner, cur, ip):
    inner.push_ds_int(0)
    return ip


# CodeThread-aware primitives

# LIT ( -- x )
def prim_LIT(inner, cur, ip):
    """GForth core 2012: push the next compilation literal."""
    lit = promote(cur.lits[ip - 1])
    if isinstance(lit, W_IntObject):
        inner.push_ds_int(lit.intval)
    elif isinstance(lit, W_FloatObject):
        inner.push_ds_float(lit.floatval)
    else:
        inner.push_ds(lit)
    return ip


# EXIT ( -- )
def prim_EXIT(inner, cur, ip):
    """GForth core 2012: terminate the current definition."""
    from rpyforth.inner_interp import EXIT_SENTINEL
    return EXIT_SENTINEL


# TAILCALL ( -- ) -- emitted when a colon body ends with a call followed by EXIT; avoids push/pop round-trip.
def prim_TAILCALL(inner, cur, ip):
    """Execute a tail call: jump to the target word (its W_WordObject is the
    literal at ip-1) without pushing a return address, via TAILCALL_SENTINEL."""
    from rpyforth.inner_interp import TAILCALL_SENTINEL
    return TAILCALL_SENTINEL


# (ABORT") ( flag c-addr u -- )
def prim_ABORT_QUOTE_RUNTIME(inner, cur, ip):
    """Runtime for ABORT" - abort if flag is non-zero, printing message."""
    u = inner.pop_ds_int()
    c_addr = inner.pop_ds()
    flag = inner.pop_ds_int()

    if flag != 0:
        if isinstance(c_addr, W_StringObject):
            msg = c_addr.strval
        else:
            msg = "ABORT"
        stdin, stdout, stderr = create_stdio()
        stdout.write("ABORT: ")
        stdout.write(msg)
        stdout.write("\n")
        raise Abort
    return ip


# Floating point operations

# F* ( f1 f2 -- f3 )
def prim_FMUL(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    inner.push_ds_float(f1 * f2)
    return ip


# F+ ( f1 f2 -- f3 )
def prim_FADD(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    inner.push_ds_float(f1 + f2)
    return ip


# F- ( f1 f2 -- f3 )
def prim_FSUB(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    inner.push_ds_float(f1 - f2)
    return ip


# F/ ( f1 f2 -- f3 )
def prim_FDIV(inner, cur, ip):
    """Divide f1 by f2 with IEEE-754 semantics. Division by zero yields signed
    infinity (or NaN for 0/0) rather than trapping, matching gforth -- brew builds
    its +infinity / -infinity / NaN FCONSTANTs with `1e0 0e0 f/` etc."""
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    if f2 == 0.0:
        if f1 == 0.0:
            inner.push_ds_float(NAN)
        elif f1 > 0.0:
            inner.push_ds_float(INFINITY)
        else:
            inner.push_ds_float(-INFINITY)
        return ip
    inner.push_ds_float(f1 / f2)
    return ip


# F> ( f1 f2 -- flag )
def prim_FGREATER(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    if f1 > f2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# FSWAP ( f1 f2 -- f2 f1 )
def prim_FSWAP(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    inner.push_ds_float(f2)
    inner.push_ds_float(f1)
    return ip


# Return Stack Operations

# >R ( x -- ) ( R: -- x )
def prim_TORETURN(inner, cur, ip):
    """GForth core 2012: move x from data stack to return stack."""
    x = inner.pop_ds_int()
    inner.push_rs(x)
    return ip


# R> ( -- x ) ( R: x -- )
def prim_FROMRETURN(inner, cur, ip):
    """GForth core 2012: move x from return stack to data stack."""
    x = inner.pop_rs()
    inner.push_ds_int(x)
    return ip


# R@ ( -- x ) ( R: x -- x )
def prim_RFETCH(inner, cur, ip):
    """GForth core 2012: copy x from top of return stack to data stack."""
    x = inner.peek_rs(0)
    inner.push_ds_int(x)
    return ip


# 2>R ( x1 x2 -- ) ( R: -- x1 x2 )
def prim_2TORETURN(inner, cur, ip):
    """GForth core 2012: move x1 and x2 from data stack to return stack."""
    x2 = inner.pop_ds_int()
    x1 = inner.pop_ds_int()
    inner.push_rs(x1)
    inner.push_rs(x2)
    return ip


# 2R> ( -- x1 x2 ) ( R: x1 x2 -- )
def prim_2FROMRETURN(inner, cur, ip):
    """GForth core 2012: move x1 and x2 from return stack to data stack."""
    x2 = inner.pop_rs()
    x1 = inner.pop_rs()
    inner.push_ds_int(x1)
    inner.push_ds_int(x2)
    return ip


# 2R@ ( -- x1 x2 ) ( R: x1 x2 -- x1 x2 )
def prim_2RFETCH(inner, cur, ip):
    """GForth core 2012: copy x1 and x2 from top of return stack to data stack."""
    x2 = inner.pop_rs()
    x1 = inner.pop_rs()
    inner.push_rs(x1)
    inner.push_rs(x2)
    inner.push_ds_int(x1)
    inner.push_ds_int(x2)
    return ip

# PICK ( xu ... x1 x0 u -- xu ... x1 x0 xu )
def prim_PICK(inner, cur, ip):
    """Copy the u-th stack item to the top (0 PICK is equivalent to DUP)."""
    u = inner.pop_ds_int()
    item = inner.peek_ds_int(u)
    inner.push_ds_int(item)
    return ip


# ROLL ( x_u ... x_0 u -- x_u-1 ... x_0 x_u )
def prim_ROLL(inner, cur, ip):
    """Remove the u-th stack item and place it on top (2 ROLL = ROT, 1 ROLL =
    SWAP, 0 ROLL is a no-op). brew's mutation/list code uses it heavily."""
    u = inner.pop_ds_int()
    if u <= 0:
        return ip
    top = inner.peek_ds_int(u)
    # Shift items x_u-1..x_0 up by one slot (toward the top), then place x_u on top.
    k = u
    while k > 0:
        inner.poke_ds_int(k, inner.peek_ds_int(k - 1))
        k -= 1
    inner.poke_ds_int(0, top)
    return ip


# Floating point conversion and storage

# S>F ( n -- ) ( F: -- f )
def prim_S2F(inner, cur, ip):
    n = inner.pop_ds_int()
    inner.push_ds_float(float(n))
    return ip


# F! ( f-addr -- ) ( F: f -- )
def prim_FSTORE(inner, cur, ip):
    addr = inner.pop_ds_int()
    val = inner.pop_ds_float()
    inner.float_store(addr, val)
    return ip


# F@ ( f-addr -- ) ( F: -- f )
def prim_FFETCH(inner, cur, ip):
    addr = inner.pop_ds_int()
    inner.push_ds_float(inner.cell_float_fetch(addr))
    return ip


# FDUP ( F: f -- f f )
def prim_FDUP(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(f)
    inner.push_ds_float(f)
    return ip


# Dictionary Operations

def _call_word_inline(inner, cur, ip, word):
    """Transfer control to a colon word from inside a primitive without leaving
    the dispatch loop: push the return frame exactly like a compiled call, hand
    the target to the loop via pending_box, and signal with CALL_SENTINEL.
    Keeps EXECUTE/CATCH calls traceable (no nested portal)."""
    inner.push_control(cur, ip)
    push_ds_fragments(inner)
    inner.pending_box[0] = word
    return CALL_SENTINEL


# [IF] / [ELSE] / [THEN] -- brew's gene engine stores and re-executes these as evaluated words.
def prim_BRACKET_IF(inner, cur, ip):
    inner.outer.runtime_bracket_if()
    return ip


def prim_BRACKET_ELSE(inner, cur, ip):
    inner.outer.runtime_bracket_else()
    return ip


def prim_BRACKET_THEN(inner, cur, ip):
    inner.outer.runtime_bracket_then()
    return ip


# SP@ ( -- addr ) -- stack is an array, not byte-addressed; stashes top cell in scratch and returns that address.
def prim_SP_FETCH(inner, cur, ip):
    addr = inner.outer.sp_scratch_addr
    if inner.ds_int_size() > 0:
        inner.cell_store(addr, inner.peek_ds_int(0))
    else:
        inner.cell_store(addr, 0)
    inner.push_ds_int(addr)
    return ip


# EXECUTE ( xt -- )
def prim_EXECUTE(inner, cur, ip):
    """GForth core 2012: execute the execution token xt (an integer wid). A wid
    outside the registry (a corrupt xt) would index out of bounds and crash the
    translated VM; guard it and THROW -21 ("unsupported operation") so the error
    is catchable / abortable instead of a segfault."""
    _w = inner.pop_ds_int()
    if _w < 0 or _w >= WORD_REGISTRY.count:
        raise ForthException(-21)
    _w = promote(_w)
    word = word_from_wid(_w)
    if word.thread is not None:
        return _call_word_inline(inner, cur, ip, word)
    inner.execute_word_now(word)
    return ip


# XT>STRING ( xt -- addr len ) -- writes name into a fixed scratch buffer (not HERE) so interleaved `,` / ALLOT stays intact.
def prim_XT_TO_STRING(inner, cur, ip):
    word = word_from_wid(inner.pop_ds_int())
    name = word.name
    n = len(name)
    if n > 256:
        n = 256
    addr = inner.outer.xt_string_scratch_addr
    k = 0
    while k < n:
        inner.char_store(addr + k, ord(name[k]))
        k += 1
    inner.buf_set(addr, W_StringObject(name[:n]))
    inner.push_ds_int(addr)
    inner.push_ds_int(n)
    return ip


# (VOCABULARY) ( wid -- ) -- replace the top of the search order with wid.
def prim_VOCAB_SELECT(inner, cur, ip):
    inner.outer.runtime_vocab_select(inner.pop_ds_int())
    return ip


# (DEFER) -- execute the action stored on this deferred word's own thread.
def prim_DEFER_EXEC(inner, cur, ip):
    assert isinstance(cur, DeferredCodeThread)
    word = cur.deferred_word
    if word is None:
        print "uninitialized DEFER"
        return ip
    if word.thread is not None:
        return DEFER_TAILCALL_SENTINEL
    inner.execute_word_now(word)
    return ip


# (IS!) ( xt tid -- ) -- bind xt to a DeferredCodeThread (compiled by IS).
def prim_IS_STORE(inner, cur, ip):
    tid = inner.pop_ds_int()
    thread = THREAD_REGISTRY.threads[tid]
    assert isinstance(thread, DeferredCodeThread)
    thread.deferred_word = word_from_wid(inner.pop_ds_int())
    return ip


# (POSTPONE) ( wid -- ) -- compiled by POSTPONE for non-immediate targets; defers the target into the enclosing word.
def prim_POSTPONE(inner, cur, ip):
    inner.outer.runtime_postpone(word_from_wid(inner.pop_ds_int()))
    return ip


# (CF) ( code -- ) -- replay a built-in control-flow compile action inside a definition being compiled.
def prim_COMPILE_CF(inner, cur, ip):
    inner.outer.runtime_compile_cf(inner.pop_ds_int())
    return ip


# COMPILE, ( xt -- ) -- append xt to the definition currently being compiled.
def prim_COMPILE_COMMA(inner, cur, ip):
    inner.outer.runtime_postpone(word_from_wid(inner.pop_ds_int()))
    return ip


# Search-order words as primitives so they work compiled into colon bodies.
def prim_GET_CURRENT(inner, cur, ip):
    inner.outer._handle_get_current()
    return ip


def prim_SET_CURRENT(inner, cur, ip):
    inner.outer._handle_set_current()
    return ip


def prim_SEARCH_WORDLIST(inner, cur, ip):
    inner.outer._handle_search_wordlist()
    return ip


def prim_FORTH_WORDLIST(inner, cur, ip):
    inner.outer._handle_forth_wordlist()
    return ip


# (:NONAME) ( -- ) -- open a nameless definition from within a running word.
def prim_BEGIN_NONAME(inner, cur, ip):
    inner.outer.runtime_begin_noname()
    return ip


# (:) ( "name" -- ) -- open a named definition from within a running word, parsing the name at run time.
def prim_BEGIN_NAMED(inner, cur, ip):
    inner.outer.runtime_begin_named()
    return ip


# (;) ( -- xt ) -- finish the definition being compiled and restore the enclosing context.
def prim_END_DEF(inner, cur, ip):
    inner.outer.runtime_end_definition()
    return ip


# SLITERAL ( c-addr u -- ) -- compile the string so the current definition pushes ( c-addr u ) at runtime.
def prim_SLITERAL(inner, cur, ip):
    inner.outer.runtime_sliteral()
    return ip


# CHAR ( "<spaces>name" -- c ) -- parse the next token and push its first character's code.
def prim_CHAR(inner, cur, ip):
    inner.outer.runtime_char()
    return ip


# PARSE-NAME ( "<spaces>name" -- c-addr u ) -- parse the next token, returning its address and length.
def prim_PARSE_NAME(inner, cur, ip):
    inner.outer.runtime_parse_name()
    return ip


# DEFINED ( "name" -- flag ) -- gforth interpret-level word-existence test.
def prim_DEFINED(inner, cur, ip):
    inner.outer.runtime_defined()
    return ip


# STATE ( -- a-addr ) -- push the address of the compilation-state cell.
def prim_STATE(inner, cur, ip):
    inner.outer.runtime_state()
    return ip


# (IMMEDIATE) ( -- ) -- mark the most recently defined word immediate.
def prim_IMMEDIATE(inner, cur, ip):
    inner.outer.runtime_immediate()
    return ip


# SAVE-INPUT ( -- xn..x1 n ) -- save the input-source position for RESTORE-INPUT.
def prim_SAVE_INPUT(inner, cur, ip):
    inner.outer.runtime_save_input()
    return ip


# RESTORE-INPUT ( xn..x1 n -- flag ) -- rewind to a saved input position.
def prim_RESTORE_INPUT(inner, cur, ip):
    inner.outer.runtime_restore_input()
    return ip


# COMPARE ( c-addr1 u1 c-addr2 u2 -- n ) -- lexicographic string comparison.
def prim_COMPARE(inner, cur, ip):
    """Lexicographic byte comparison of two strings in data space. Every
    string (S" literal or ALLOTted buffer) is byte-backed, so both operands
    are read from char memory."""
    u2 = inner.pop_ds_int()
    a2 = inner.pop_ds_int()
    u1 = inner.pop_ds_int()
    a1 = inner.pop_ds_int()
    if u1 < 0:
        u1 = 0
    if u2 < 0:
        u2 = 0
    n = u1
    if u2 < n:
        n = u2
    i = 0
    res = 0
    while i < n:
        c1 = inner.char_fetch(a1 + i)
        c2 = inner.char_fetch(a2 + i)
        if c1 < c2:
            res = -1
            break
        if c1 > c2:
            res = 1
            break
        i += 1
    if res == 0:
        if u1 < u2:
            res = -1
        elif u1 > u2:
            res = 1
    inner.push_ds_int(res)
    return ip


# SEARCH ( c-addr1 u1 c-addr2 u2 -- c-addr3 u3 flag ) -- STRING; locate c-addr2/u2 inside c-addr1/u1.
def prim_SEARCH(inner, cur, ip):
    u2 = inner.pop_ds_int()
    a2 = inner.pop_ds_int()
    u1 = inner.pop_ds_int()
    a1 = inner.pop_ds_int()
    if u1 < 0:
        u1 = 0
    if u2 < 0:
        u2 = 0
    s1 = _read_cstr(inner, a1, u1)
    s2 = _read_cstr(inner, a2, u2)
    found = -1
    if u2 == 0:
        found = 0
    elif u2 <= u1:
        limit = u1 - u2
        i = 0
        while i <= limit:
            k = 0
            match = True
            while k < u2:
                if s1[i + k] != s2[k]:
                    match = False
                    break
                k += 1
            if match:
                found = i
                break
            i += 1
    if found >= 0:
        inner.push_ds_int(a1 + found)
        inner.push_ds_int(u1 - found)
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(a1)
        inner.push_ds_int(u1)
        inner.push_ds_int(0)
    return ip


# DEFER! ( xt xt-deferred -- ) -- bind xt as the action of a deferred word.
def prim_DEFER_STORE(inner, cur, ip):
    xt_def = word_from_wid(inner.pop_ds_int())
    xt = word_from_wid(inner.pop_ds_int())
    thread = xt_def.thread
    assert isinstance(thread, DeferredCodeThread)
    thread.deferred_word = xt
    return ip


# CONSTANT ( x "<name>" -- ) -- runtime defining word.
def prim_CONSTANT(inner, cur, ip):
    inner.outer.runtime_constant()
    return ip


# VARIABLE ( "<name>" -- ) -- runtime defining word.
def prim_VARIABLE(inner, cur, ip):
    inner.outer.runtime_variable()
    return ip


# 2CONSTANT ( x1 x2 "<name>" -- ) -- runtime defining word (DOUBLE); child word pushes x1 x2.
def prim_2CONSTANT(inner, cur, ip):
    inner.outer.runtime_2constant()
    return ip


# 2VARIABLE ( "<name>" -- ) -- runtime defining word (DOUBLE-EXT).
def prim_2VARIABLE(inner, cur, ip):
    inner.outer.runtime_2variable()
    return ip


# CREATE ( "<name>" -- ) -- runtime defining word; uses cur.does_word if the enclosing definition had a DOES>.
def prim_CREATE(inner, cur, ip):
    inner.outer.runtime_create(cur.does_word)
    return ip


# (DOES>) ( -- ) -- rebinds the most recent CREATEd word to run this thread's carved DOES> body.
def prim_DODOES(inner, cur, ip):
    inner.outer.runtime_does(cur.does_word)
    return ip


# DEFER ( "<name>" -- ) -- runtime defining word.
def prim_DEFER(inner, cur, ip):
    inner.outer.runtime_defer()
    return ip


# ' ( "<name>" -- xt ) -- tick, executable inside a colon body.
def prim_TICK(inner, cur, ip):
    inner.outer.runtime_tick()
    return ip


# ( ( "ccc<paren>" -- ) -- comment word, immediate so it can be POSTPONEd; consumes input to next ')'.
def prim_PAREN(inner, cur, ip):
    inner.outer.runtime_paren()
    return ip


# WORDLIST ( -- wid ) -- SEARCH; prim so it works compiled into a colon body.
def prim_WORDLIST(inner, cur, ip):
    inner.outer._handle_wordlist()
    return ip


# GET-ORDER ( -- widn..wid1 n ) -- SEARCH.
def prim_GET_ORDER(inner, cur, ip):
    inner.outer._handle_get_order()
    return ip


# SET-ORDER ( widn..wid1 n -- ) -- SEARCH.
def prim_SET_ORDER(inner, cur, ip):
    inner.outer._handle_set_order()
    return ip


# ALSO / PREVIOUS / DEFINITIONS / FORTH -- SEARCH-EXT; prims so they take effect compiled into a colon body.
def prim_ALSO(inner, cur, ip):
    inner.outer._handle_also()
    return ip


def prim_PREVIOUS(inner, cur, ip):
    inner.outer._handle_previous()
    return ip


# >ORDER ( wid -- ) -- SEARCH-EXT; prim so it also takes effect compiled into a colon body.
def prim_TO_ORDER(inner, cur, ip):
    inner.outer._handle_to_order()
    return ip


# >NUMBER ( ud1 c-addr1 u1 -- ud2 c-addr2 u2 ) -- CORE; prim so colon bodies can call it.
def prim_TO_NUMBER(inner, cur, ip):
    inner.outer._handle_to_number()
    return ip


# NEXTNAME ( c-addr u -- ) -- override the name the next defining word will use instead of parsing one.
def prim_NEXTNAME(inner, cur, ip):
    inner.outer.runtime_nextname()
    return ip


def prim_DEFINITIONS(inner, cur, ip):
    inner.outer._handle_definitions()
    return ip


def prim_FORTH(inner, cur, ip):
    inner.outer._handle_forth()
    return ip


# CS-ROLL ( C: origN..orig0 N -- origN-1..orig0 origN ) -- TOOLS-EXT; rolls the compile-time control-flow stack.
def prim_CS_ROLL(inner, cur, ip):
    inner.outer.runtime_cs_roll()
    return ip


# WORD ( char "<chars>ccc<char>" -- c-addr ) -- parse the next token and store as a counted string.
def prim_WORD(inner, cur, ip):
    inner.outer.runtime_word()
    return ip


# INCLUDED ( c-addr u -- ) -- load a source file named by the string.
def prim_INCLUDED(inner, cur, ip):
    inner.outer.runtime_included()
    return ip


# PARSE ( char "ccc<char>" -- c-addr u ) -- parse the next token off the input line.
def prim_PARSE(inner, cur, ip):
    inner.outer.runtime_parse()
    return ip


# REFILL ( -- flag ) -- read the next line into the input buffer; push false at EOF.
def prim_REFILL(inner, cur, ip):
    inner.outer.runtime_refill()
    return ip


# MARKER ( "name" -- ) -- define a dictionary marker (no-op forget here).
def prim_MARKER(inner, cur, ip):
    inner.outer.runtime_marker()
    return ip


# QUIT ( -- ) -- abandon the current activity and return to the interpreter via Abort.
@dont_look_inside
def prim_QUIT(inner, cur, ip):
    # State cleared here: zeroing virtualizable stacks inside a compiled frame before raising would unwind through already-gone bookkeeping.
    inner.outer.state = 0
    raise Abort
    return ip


# FIND ( c-addr -- c-addr 0 | xt 1 | xt -1 ) -- look up a counted string in the dictionary.
def prim_FIND(inner, cur, ip):
    inner.outer.runtime_find()
    return ip


# BASE ( -- a-addr ) -- address of the radix variable.
def prim_BASE(inner, cur, ip):
    inner.outer.runtime_base()
    return ip


# UNUSED ( -- u ) -- remaining free dictionary space, in address units.
def prim_UNUSED(inner, cur, ip):
    remaining = DICT_SIZE_BYTES - inner.here
    if remaining < 0:
        remaining = 0
    inner.push_ds_int(remaining)
    return ip


# :INLINE ( -- ) -- dict stub so [UNDEFINED] :inline reports it defined; body never executed.
def prim_INLINE_STUB(inner, cur, ip):
    return ip


# COUNT ( c-addr1 -- c-addr2 u ) -- read the length byte of a counted string.
def prim_COUNT(inner, cur, ip):
    c_addr1 = inner.pop_ds_int()
    inner.push_ds_int(c_addr1 + 1)
    inner.push_ds_int(inner.char_fetch(c_addr1))
    return ip


# EVALUATE ( c-addr u -- ) -- interpret the string; usable inside a colon body.
def prim_EVALUATE(inner, cur, ip):
    inner.outer._handle_evaluate()
    return ip


# >IN ( -- a-addr ) -- address of the parse cursor.
def prim_TO_IN(inner, cur, ip):
    inner.push_ds_int(inner.outer.to_in_addr)
    return ip


# SOURCE ( -- c-addr u ) -- push address and length of current input buffer; primitive so it also compiles into colon bodies.
def prim_SOURCE(inner, cur, ip):
    inner.outer._handle_source()
    return ip


# ABORT ( -- ) -- clear the stacks and unwind to the top level.
def prim_ABORT(inner, cur, ip):
    inner.reset_ds_int()
    inner.reset_ds_float()
    inner.ds_ptr_locals = 0
    inner.rs_ptr = 0
    inner.lc_depth = 0
    raise ForthException(-1)


# >BODY ( xt -- a-addr )
def prim_TOBODY(inner, cur, ip):
    """GForth core 2012: return the parameter field address corresponding to xt."""
    word = word_from_wid(inner.pop_ds_int())
    if word.thread is not None and len(word.thread.lits) > 0:
        body = word.thread.lits[0]
        if isinstance(body, W_IntObject):
            inner.push_ds_int(body.intval)
        else:
            inner.push_ds(body)
    else:
        inner.push_ds_int(0)
    return ip


# System Operations

# FILL ( c-addr u char -- )
def prim_FILL(inner, cur, ip):
    """GForth core 2012: fill u bytes of memory starting at c-addr with char."""
    char = inner.pop_ds_int()
    u = inner.pop_ds_int()
    addr = inner.pop_ds_int()
    inner.heap.fill_bytes(addr, u, char)
    return ip


# MOVE ( addr1 addr2 u -- )
def prim_MOVE(inner, cur, ip):
    """GForth core 2012: copy u bytes from addr1 to addr2 (overlap-safe)."""
    u = inner.pop_ds_int()
    addr2 = inner.pop_ds_int()
    addr1 = inner.pop_ds_int()
    inner.heap.move_bytes(addr1, addr2, u)
    return ip


# CMOVE ( c-addr1 c-addr2 u -- )
@unroll_safe
def prim_CMOVE(inner, cur, ip):
    """GForth string 2012: copy u chars from c-addr1 to c-addr2, low address
    first (so an overlapping move propagates the leading bytes upward)."""
    u = inner.pop_ds_int()
    addr2 = inner.pop_ds_int()
    addr1 = inner.pop_ds_int()
    for i in range(u):
        inner.char_store(addr2 + i, inner.char_fetch(addr1 + i))
    return ip


# CMOVE> ( c-addr1 c-addr2 u -- )
@unroll_safe
def prim_CMOVE_UP(inner, cur, ip):
    """GForth string 2012: copy u chars from c-addr1 to c-addr2, high address
    first, so a move toward a higher overlapping address preserves the source."""
    u = inner.pop_ds_int()
    addr2 = inner.pop_ds_int()
    addr1 = inner.pop_ds_int()
    i = u - 1
    while i >= 0:
        inner.char_store(addr2 + i, inner.char_fetch(addr1 + i))
        i -= 1
    return ip


# Memory Access Operations (additional)

# +! ( n|u a-addr -- )
def prim_PLUSSTORE(inner, cur, ip):
    """GForth core 2012: add n to the value stored at a-addr."""
    addr = inner.pop_ds_int()
    n = inner.pop_ds_int()
    new_val = inner.cell_fetch_int(addr) + n
    inner.cell_store(addr, new_val)
    return ip


# 2@ ( a-addr -- x1 x2 )
def prim_2FETCH(inner, cur, ip):
    """GForth core 2012: fetch the cell pair at a-addr (x2, the top, comes
    from a-addr; x1 from the next cell)."""
    addr = inner.pop_ds_int()
    addr2 = addr + inner.cell_size_bytes
    inner.push_ds_int(inner.cell_fetch_int(addr2))
    inner.push_ds_int(inner.cell_fetch_int(addr))
    return ip


# C! ( char c-addr -- )
def prim_C_STORE(inner, cur, ip):
    """GForth core 2012: store char at c-addr."""
    addr = inner.pop_ds_int()
    char = inner.pop_ds_int()
    inner.char_store(addr, char)
    return ip


# C@ ( c-addr -- char )
def prim_C_FETCH(inner, cur, ip):
    """GForth core 2012: fetch the character stored at c-addr."""
    addr = inner.pop_ds_int()
    char = inner.char_fetch(addr)
    inner.push_ds_int(char)
    return ip


# CHAR+ ( c-addr1 -- c-addr2 )
def prim_CHAR_PLUS(inner, cur, ip):
    """GForth core 2012: add the size of a character to c-addr1."""
    addr = inner.pop_ds_int()
    inner.push_ds_int(addr + 1)
    return ip


# CHARS ( n1 -- n2 )
def prim_CHARS(inner, cur, ip):
    """GForth core 2012: convert n1 characters to address units."""
    n = inner.pop_ds_int()
    inner.push_ds_int(n)
    return ip


# ALIGN ( -- )
def prim_ALIGN(inner, cur, ip):
    """GForth core 2012: align the data-space pointer."""
    remainder = inner.here % inner.cell_size_bytes
    if remainder != 0:
        inner.here += (inner.cell_size_bytes - remainder)
    return ip


# ALIGNED ( addr -- a-addr )
def prim_ALIGNED(inner, cur, ip):
    """GForth core 2012: return the aligned address."""
    addr_val = inner.pop_ds_int()
    remainder = addr_val % inner.cell_size_bytes
    if remainder != 0:
        addr_val += (inner.cell_size_bytes - remainder)
    inner.push_ds_int(addr_val)
    return ip


# Data Space Operations

# HERE ( -- addr )
def prim_HERE(inner, cur, ip):
    """GForth core 2012: return the address of the next available data space location."""
    inner.push_ds_int(inner.here)
    return ip


# , ( x -- )
def prim_COMMA(inner, cur, ip):
    """GForth core 2012: reserve one cell of data space and store x in it."""
    x = inner.pop_ds_int()
    addr = inner.here
    inner.cell_store(addr, x)
    inner.here += inner.cell_size_bytes
    return ip


# C, ( char -- )
def prim_C_COMMA(inner, cur, ip):
    """GForth core 2012: reserve one character of data space and store char in it.
    Stores into character (byte) space so C@ / COUNT read it back consistently."""
    char = inner.pop_ds_int()
    addr = inner.here
    inner.char_store(addr, char)
    inner.here += 1
    return ip


# ALLOT ( n -- )
def prim_ALLOT(inner, cur, ip):
    """GForth core 2012: reserve n address units of data space."""
    n = inner.pop_ds_int()
    inner.here += n
    return ip


# Comparison

# = ( x1 x2 -- flag )
def prim_EQUAL(inner, cur, ip):
    x2 = inner.pop_ds_int()
    x1 = inner.pop_ds_int()
    if x1 == x2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# System Operations

# BYE ( -- )
def prim_BYE(inner, cur, ip):
    """GForth core 2012: exit the Forth system."""
    from rpyforth.inner_interp import Bye
    raise Bye()


# <= ( n1 n2 -- flag )
def prim_LESSEQUAL(inner, cur, ip):
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    if n1 <= n2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# >= ( n1 n2 -- flag )
def prim_GREATEREQUAL(inner, cur, ip):
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    if n1 >= n2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# <> ( n1 n2 -- flag )
def prim_NOTEQUAL(inner, cur, ip):
    n2 = inner.pop_ds_int()
    n1 = inner.pop_ds_int()
    if n1 != n2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# F< ( r1 r2 -- flag )
def prim_FLESS(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    if f1 < f2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# F= ( r1 r2 -- flag )
def prim_FEQUAL(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    if f1 == f2:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# F0< ( r -- flag )
def prim_FZEROLESS(inner, cur, ip):
    f = inner.pop_ds_float()
    if f < 0.0:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# F0= ( r -- flag )
def prim_FZEROEQUAL(inner, cur, ip):
    f = inner.pop_ds_float()
    if f == 0.0:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# FNEGATE ( r1 -- r2 )
def prim_FNEGATE(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(-f)
    return ip


# FABS ( r1 -- r2 )
def prim_FABS(inner, cur, ip):
    f = inner.pop_ds_float()
    if f < 0.0:
        inner.push_ds_float(-f)
    else:
        inner.push_ds_float(f)
    return ip


# FOVER ( r1 r2 -- r1 r2 r1 )
def prim_FOVER(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    inner.push_ds_float(f1)
    inner.push_ds_float(f2)
    inner.push_ds_float(f1)
    return ip


# FDROP ( r -- )
def prim_FDROP(inner, cur, ip):
    inner.pop_ds_float()
    return ip


# FDEPTH ( -- +n ) -- number of items on the float stack.
def prim_FDEPTH(inner, cur, ip):
    inner.push_ds_int(inner.depth_ds_float())
    return ip


# FROT ( r1 r2 r3 -- r2 r3 r1 )
def prim_FROT(inner, cur, ip):
    f3 = inner.pop_ds_float()
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    inner.push_ds_float(f2)
    inner.push_ds_float(f3)
    inner.push_ds_float(f1)
    return ip


# FLOAT+ ( f-addr1 -- f-addr2 )
def prim_FLOATPLUS(inner, cur, ip):
    addr = inner.pop_ds_int()
    inner.push_ds_int(addr + 8)
    return ip


# File access -- fam encoding: bit 0/1 = read/write access, bit 2 = BIN flag (ignored on POSIX but must round-trip).
FAM_RO = 0
FAM_WO = 1
FAM_RW = 2
FAM_BIN = 4


def _read_cstr(inner, addr, u):
    """Return the u-char string at c-addr. Handles both representations: a
    filename produced by S" lives as a single W_StringObject in inner.buf,
    while a byte buffer built with C!/CMOVE lives in char memory."""
    entry = inner.buf_get(addr)
    if entry is not None and isinstance(entry, W_StringObject):
        s = entry.strval
        if 0 <= u < len(s):
            return s[:u]
        return s
    chars = []
    for i in range(u):
        chars.append(chr(inner.char_fetch(addr + i)))
    return "".join(chars)


def _open_flags(fam):
    import os
    access = fam & 3
    if access == FAM_WO:
        return os.O_WRONLY
    elif access == FAM_RW:
        return os.O_RDWR
    else:
        return os.O_RDONLY


# R/O ( -- fam )
def prim_R_O(inner, cur, ip):
    inner.push_ds_int(FAM_RO)
    return ip


# W/O ( -- fam )
def prim_W_O(inner, cur, ip):
    inner.push_ds_int(FAM_WO)
    return ip


# R/W ( -- fam )
def prim_R_W(inner, cur, ip):
    inner.push_ds_int(FAM_RW)
    return ip


# BIN ( fam1 -- fam2 )
def prim_BIN(inner, cur, ip):
    fam = inner.pop_ds_int()
    inner.push_ds_int(fam | FAM_BIN)
    return ip


# STDIN / STDOUT / STDERR ( -- fileid )
def prim_STDIN(inner, cur, ip):
    inner.push_ds_int(0)
    return ip


def prim_STDOUT(inner, cur, ip):
    inner.push_ds_int(1)
    return ip


def prim_STDERR(inner, cur, ip):
    inner.push_ds_int(2)
    return ip


# OS calls are in @dont_look_inside helpers that never receive `inner`; passing a virtualizable to an opaque residual forces it and aborts the trace (vable escape).
@dont_look_inside
def _open_file_raw(name, flags):
    import os
    try:
        return os.open(name, flags, 0666)
    except OSError as e:
        return -e.errno


# OPEN-FILE ( c-addr u fam -- fileid ior )
def prim_OPEN_FILE(inner, cur, ip):
    fam = inner.pop_ds_int()
    u = inner.pop_ds_int()
    addr = inner.pop_ds_int()
    name = _read_cstr(inner, addr, u)
    fd = _open_file_raw(name, _open_flags(fam))
    if fd < 0:
        inner.push_ds_int(0)
        inner.push_ds_int(-fd)
        return ip
    inner.push_ds_int(fd)
    inner.push_ds_int(0)
    return ip


# CREATE-FILE ( c-addr u fam -- fileid ior )
@dont_look_inside
def prim_CREATE_FILE(inner, cur, ip):
    import os
    fam = inner.pop_ds_int()
    u = inner.pop_ds_int()
    addr = inner.pop_ds_int()
    name = _read_cstr(inner, addr, u)
    flags = _open_flags(fam) | os.O_CREAT | os.O_TRUNC
    try:
        fd = os.open(name, flags, 0666)
    except OSError as e:
        inner.push_ds_int(0)
        inner.push_ds_int(e.errno)
        return ip
    inner.push_ds_int(fd)
    inner.push_ds_int(0)
    return ip


@dont_look_inside
def _close_file_raw(fd):
    import os
    try:
        os.close(fd)
    except OSError as e:
        return e.errno
    return 0


# CLOSE-FILE ( fileid -- ior )
def prim_CLOSE_FILE(inner, cur, ip):
    inner.push_ds_int(_close_file_raw(inner.pop_ds_int()))
    return ip


# DELETE-FILE ( c-addr u -- ior )
@dont_look_inside
def prim_DELETE_FILE(inner, cur, ip):
    import os
    u = inner.pop_ds_int()
    addr = inner.pop_ds_int()
    name = _read_cstr(inner, addr, u)
    try:
        os.unlink(name)
    except OSError as e:
        inner.push_ds_int(e.errno)
        return ip
    inner.push_ds_int(0)
    return ip


# READ-FILE ( c-addr u1 fileid -- u2 ior )
@dont_look_inside
def prim_READ_FILE(inner, cur, ip):
    import os
    fd = inner.pop_ds_int()
    u1 = inner.pop_ds_int()
    addr = inner.pop_ds_int()
    try:
        data = os.read(fd, u1)
    except OSError as e:
        inner.push_ds_int(0)
        inner.push_ds_int(e.errno)
        return ip
    n = len(data)
    for i in range(n):
        inner.char_store(addr + i, ord(data[i]))
    inner.push_ds_int(n)
    inner.push_ds_int(0)
    return ip


class FileLineResult(object):
    """Out-params of _read_line_raw. One prebuilt instance: the VM is
    single-threaded and the result is consumed before the next file op."""
    def __init__(self):
        self.n = 0
        self.flag = 0
        self.ior = 0


_read_line_result = FileLineResult()


@dont_look_inside
def _read_line_raw(heap, fd, u1, addr):
    import os
    res = _read_line_result
    n = 0
    got_any = False
    try:
        while n < u1:
            ch = os.read(fd, 1)
            if len(ch) == 0:
                break
            got_any = True
            c = ch[0]
            if c == '\n':
                res.n = n
                res.flag = -1
                res.ior = 0
                return
            if c != '\r':
                heap.char_store(addr + n, ord(c))
                n += 1
    except OSError as e:
        res.n = n
        res.flag = -1
        res.ior = e.errno
        return
    res.n = n
    if got_any:
        res.flag = -1
    else:
        res.flag = 0
    res.ior = 0


# READ-LINE ( c-addr u1 fileid -- u2 flag ior )
def prim_READ_LINE(inner, cur, ip):
    fd = inner.pop_ds_int()
    u1 = inner.pop_ds_int()
    addr = inner.pop_ds_int()
    _read_line_raw(inner.heap, fd, u1, addr)
    res = _read_line_result
    inner.push_ds_int(res.n)
    inner.push_ds_int(res.flag)
    inner.push_ds_int(res.ior)
    return ip


# WRITE-FILE ( c-addr u fileid -- ior )
@dont_look_inside
def prim_WRITE_FILE(inner, cur, ip):
    import os
    fd = inner.pop_ds_int()
    u = inner.pop_ds_int()
    addr = inner.pop_ds_int()
    data = _read_cstr(inner, addr, u)
    try:
        os.write(fd, data)
    except OSError as e:
        inner.push_ds_int(e.errno)
        return ip
    inner.push_ds_int(0)
    return ip


# FLUSH-FILE ( fileid -- ior ) -- WRITE-FILE uses unbuffered os.write; nothing to flush.
def prim_FLUSH_FILE(inner, cur, ip):
    inner.pop_ds_int()
    inner.push_ds_int(0)
    return ip


# WRITE-LINE ( c-addr u fileid -- ior )
@dont_look_inside
def prim_WRITE_LINE(inner, cur, ip):
    import os
    fd = inner.pop_ds_int()
    u = inner.pop_ds_int()
    addr = inner.pop_ds_int()
    data = _read_cstr(inner, addr, u)
    try:
        os.write(fd, data)
        os.write(fd, "\n")
    except OSError as e:
        inner.push_ds_int(e.errno)
        return ip
    inner.push_ds_int(0)
    return ip


# FILE-POSITION ( fileid -- ud ior )
@dont_look_inside
def prim_FILE_POSITION(inner, cur, ip):
    import os
    fd = inner.pop_ds_int()
    try:
        pos = os.lseek(fd, 0, 1)   # SEEK_CUR
    except OSError as e:
        inner.push_ds_int(0)
        inner.push_ds_int(0)
        inner.push_ds_int(e.errno)
        return ip
    BIT_MASK = (1 << LONG_BIT) - 1
    inner.push_ds_int(pos & BIT_MASK)
    inner.push_ds_int(pos >> LONG_BIT)
    inner.push_ds_int(0)
    return ip


# REPOSITION-FILE ( ud fileid -- ior )
@dont_look_inside
def prim_REPOSITION_FILE(inner, cur, ip):
    import os
    fd = inner.pop_ds_int()
    hi = inner.pop_ds_int()
    lo = inner.pop_ds_int()
    BIT_MASK = (1 << LONG_BIT) - 1
    pos = (lo & BIT_MASK) + (hi << LONG_BIT)
    try:
        os.lseek(fd, pos, 0)   # SEEK_SET
    except OSError as e:
        inner.push_ds_int(e.errno)
        return ip
    inner.push_ds_int(0)
    return ip


# FILE-SIZE ( fileid -- ud ior )
@dont_look_inside
def prim_FILE_SIZE(inner, cur, ip):
    import os
    fd = inner.pop_ds_int()
    try:
        st = os.fstat(fd)
        size = st.st_size
    except OSError as e:
        inner.push_ds_int(0)
        inner.push_ds_int(0)
        inner.push_ds_int(e.errno)
        return ip
    BIT_MASK = (1 << LONG_BIT) - 1
    inner.push_ds_int(size & BIT_MASK)
    inner.push_ds_int(size >> LONG_BIT)
    inner.push_ds_int(0)
    return ip


# ALLOCATE ( u -- a-addr ior )
def prim_ALLOCATE(inner, cur, ip):
    """Allocate u bytes from the high ALLOCATE region (separate from dictionary
    space, so a large block does not disturb HERE). Returns ( addr 0 ) on success,
    ( 0 -1 ) on failure. Each block carries an 8-byte size header just below the
    returned address so FREE can recover the size and recycle it; a same-size
    freed block is reused before the bump pointer advances, keeping the region
    bounded across gc.fs's repeated FREE/ALLOCATE of equal-size bitvectors."""
    size = inner.pop_ds_int()
    if size < 0:
        inner.push_ds_int(0)
        inner.push_ds_int(-1)
        return ip
    # Round up to a whole cell to keep user addresses cell-aligned.
    usable = size
    rem = usable & (CELL_SIZE_BYTES - 1)
    if rem != 0:
        usable += (CELL_SIZE_BYTES - rem)
    bucket = inner.alloc_free.get(usable, None)
    if bucket is not None and len(bucket) > 0:
        addr = bucket.pop()
        inner.push_ds_int(addr)
        inner.push_ds_int(0)
        return ip
    header = inner.alloc_ptr
    addr = header + CELL_SIZE_BYTES
    new_ptr = addr + usable
    if new_ptr <= inner.alloc_limit:
        inner.cell_store(header, usable)
        inner.alloc_ptr = new_ptr
        inner.push_ds_int(addr)
        inner.push_ds_int(0)
    else:
        inner.push_ds_int(0)
        inner.push_ds_int(-1)
    return ip


# FREE ( a-addr -- ior )
def prim_FREE(inner, cur, ip):
    """Return a previously ALLOCATEd block to its size bucket for reuse. Reads
    the usable size from the header just below the block. Always succeeds."""
    addr = inner.pop_ds_int()
    if addr >= ALLOC_BASE + CELL_SIZE_BYTES and addr <= inner.alloc_limit:
        usable = inner.cell_fetch_int(addr - CELL_SIZE_BYTES)
        if usable > 0:
            bucket = inner.alloc_free.get(usable, None)
            if bucket is None:
                bucket = []
                inner.alloc_free[usable] = bucket
            bucket.append(addr)
    inner.push_ds_int(0)
    return ip


# RESIZE ( a-addr1 u -- a-addr2 ior )
def prim_RESIZE(inner, cur, ip):
    """Resize a previously ALLOCATEd block to u bytes. Allocates a fresh block,
    copies min(old-usable, u) bytes, and frees the old one. On failure the
    original block is left intact and ( a-addr1 -1 ) is returned. gforth allows
    a zero address (acts like ALLOCATE)."""
    new_size = inner.pop_ds_int()
    old_addr = inner.pop_ds_int()
    if new_size < 0:
        inner.push_ds_int(old_addr)
        inner.push_ds_int(-1)
        return ip
    # In-place fast path: if existing block is large enough, keep the same address (invalidating cached addresses would break brew).
    if old_addr >= ALLOC_BASE + CELL_SIZE_BYTES and old_addr <= inner.alloc_limit:
        cur_usable = inner.cell_fetch_int(old_addr - CELL_SIZE_BYTES)
        if 0 < new_size <= cur_usable:
            inner.push_ds_int(old_addr)
            inner.push_ds_int(0)
            return ip
    inner.push_ds_int(new_size)
    prim_ALLOCATE(inner, cur, ip)
    ior = inner.pop_ds_int()
    new_addr = inner.pop_ds_int()
    if ior != 0:
        inner.push_ds_int(old_addr)
        inner.push_ds_int(-1)
        return ip
    if old_addr >= ALLOC_BASE + CELL_SIZE_BYTES and old_addr <= inner.alloc_limit:
        old_usable = inner.cell_fetch_int(old_addr - CELL_SIZE_BYTES)
        n = old_usable
        if new_size < n:
            n = new_size
        k = 0
        while k < n:
            inner.char_store(new_addr + k, inner.char_fetch(old_addr + k))
            k += 1
        inner.push_ds_int(old_addr)
        prim_FREE(inner, cur, ip)
        inner.pop_ds_int()
    inner.push_ds_int(new_addr)
    inner.push_ds_int(0)
    return ip


# THROW ( k*x n -- k*x | i*x n )
def prim_THROW(inner, cur, ip):
    """Throw an exception with code n; n=0 is a no-op. Unwinds to the nearest CATCH."""
    n = inner.pop_ds_int()
    if n != 0:
        raise ForthException(n)
    return ip


# (CATCH-EPILOGUE) -- runs on normal return from CATCH-protected word; drops catch frame and pushes 0.
def prim_CATCH_EPILOGUE(inner, cur, ip):
    inner.catch_drop_frame()
    inner.push_ds_int(0)
    return ip


CATCH_EPILOGUE_WORD = Word("(catch-epilogue)", prim=prim_CATCH_EPILOGUE)
CATCH_EPILOGUE_THREAD = CodeThread([CATCH_EPILOGUE_WORD], [ZERO])


# CATCH ( i*x xt -- j*x 0 | i*x n ) -- execute xt; return 0 normally or THROW code with stack restored.
def prim_CATCH(inner, cur, ip):
    word = word_from_wid(inner.pop_ds_int())
    if word.thread is None:
        return _catch_primitive_xt(inner, cur, ip, word)
    inner.catch_push_frame(cur, ip)
    inner.push_control(cur, ip)
    push_ds_fragments(inner)
    inner.push_control(CATCH_EPILOGUE_THREAD, 0)
    push_ds_fragments(inner)
    inner.pending_box[0] = word
    return CALL_SENTINEL


# CATCH fallback for a primitive xt: run in a bounded nested portal.
@dont_look_inside
def _catch_primitive_xt(inner, cur, ip, word):
    if USE_STACK_FRAGMENT:
        snap = snapshot_cache(inner)
    else:
        snap = None
    if USE_FLOAT_FRAGMENT:
        fsnap = snapshot_float_cache(inner)
    else:
        fsnap = None
    s_dsi = inner.ds_ptr_ints
    s_dsf = inner.ds_ptr_floats
    s_dsl = inner.ds_ptr_locals
    s_rs = inner.rs_ptr
    s_li = inner.li
    s_lc = inner.lc_depth
    s_control = inner.cs_ptr
    s_catch = inner.catch_ptr
    try:
        inner.execute_word_now(word)
    except ForthException as e:
        if USE_STACK_FRAGMENT:
            restore_cache(inner, snap)
        if USE_FLOAT_FRAGMENT:
            restore_float_cache(inner, fsnap)
        inner.ds_ptr_ints = s_dsi
        if not USE_FLOAT_FRAGMENT:
            inner.ds_ptr_floats = s_dsf
        inner.ds_ptr_locals = s_dsl
        inner.rs_ptr = s_rs
        inner.li = s_li
        inner.lc_depth = s_lc
        inner.cs_ptr = s_control
        inner.catch_ptr = s_catch
        inner.push_ds_int(e.code)
        return ip
    inner.push_ds_int(0)
    return ip


# Precision for floating point output
_float_precision = [6]  # list for mutability in RPython


# SET-PRECISION ( u -- )
def prim_SET_PRECISION(inner, cur, ip):
    """Set number of significant digits for floating point output."""
    prec = inner.pop_ds_int()
    _float_precision[0] = prec
    return ip


# PRECISION ( -- u )
def prim_PRECISION(inner, cur, ip):
    inner.push_ds_int(_float_precision[0])
    return ip


# F. ( r -- )
def prim_FDOT(inner, cur, ip):
    f = inner.pop_ds_float()
    prec = _float_precision[0]
    result = formatd(f, 'g', prec)
    stdin, stdout, stderr = create_stdio()
    stdout.write(result + " ")
    stdout.flush()
    return ip


# REPRESENT ( rf c-addr u -- n f1 f2 ) -- FLOATING-EXT; n is exponent+1, f1 is sign, f2 is finite flag.
def prim_REPRESENT(inner, cur, ip):
    u = inner.pop_ds_int()
    c_addr = inner.pop_ds_int()
    rf = inner.pop_ds_float()
    if u < 0:
        u = 0
    negative = -1 if _float_is_negative(rf) else 0
    if rf != rf:  # NaN
        _store_digits(inner, c_addr, u, "nan")
        inner.push_ds_int(0)
        inner.push_ds_int(negative)
        inner.push_ds_int(0)
        return ip
    if rf == INFINITY or rf == -INFINITY:
        _store_digits(inner, c_addr, u, "inf")
        inner.push_ds_int(0)
        inner.push_ds_int(negative)
        inner.push_ds_int(0)
        return ip
    prec = u - 1
    if prec < 0:
        prec = 0
    s = formatd(rf, 'e', prec)
    if len(s) > 0 and (s[0] == '-' or s[0] == '+'):
        s = s[1:]
    epos = -1
    i = 0
    while i < len(s):
        if s[i] == 'e' or s[i] == 'E':
            epos = i
            break
        i += 1
    if epos < 0:
        mant = s
        exp = 0
    else:
        mant = s[:epos]
        exp = _parse_int(s[epos + 1:])
    digits = []
    j = 0
    while j < len(mant):
        c = mant[j]
        if c != '.':
            digits.append(c)
        j += 1
    digit_str = "".join(digits)
    _store_digits(inner, c_addr, u, digit_str)
    n = exp + 1
    inner.push_ds_int(n)
    inner.push_ds_int(negative)
    inner.push_ds_int(-1)
    return ip


def _float_is_negative(f):
    if f < 0.0:
        return True
    if f == 0.0:
        # detect -0.0 via its formatted sign
        return formatd(f, 'e', 0)[0] == '-'
    return False


def _parse_int(s):
    sign = 1
    i = 0
    if len(s) > 0 and (s[0] == '+' or s[0] == '-'):
        if s[0] == '-':
            sign = -1
        i = 1
    val = 0
    while i < len(s):
        c = s[i]
        if '0' <= c <= '9':
            val = val * 10 + (ord(c) - ord('0'))
        i += 1
    return sign * val


def _store_digits(inner, c_addr, u, digit_str):
    k = 0
    while k < u:
        if k < len(digit_str):
            inner.char_store(c_addr + k, ord(digit_str[k]))
        else:
            inner.char_store(c_addr + k, ord('0'))
        k += 1


# D>F ( d -- r )
def prim_D2F(inner, cur, ip):
    high = inner.pop_ds_int()
    low = inner.pop_ds_int()
    if LONG_BIT == 64:
        d = low  # on 64-bit, the double fits in one cell
    else:
        d = (high << 32) | (low & 0xFFFFFFFF)
    inner.push_ds_float(float(d))
    return ip


# F>D ( r -- d )
def prim_F2D(inner, cur, ip):
    """Convert float to double-cell integer (truncate toward zero)."""
    f = inner.pop_ds_float()
    d = int(f)
    if LONG_BIT == 64:
        inner.push_ds_int(d)
        inner.push_ds_int(0)
    else:
        inner.push_ds_int(d & 0xFFFFFFFF)
        inner.push_ds_int(d >> 32)
    return ip


# FMIN ( r1 r2 -- r3 )
def prim_FMIN(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    if f1 < f2:
        inner.push_ds_float(f1)
    else:
        inner.push_ds_float(f2)
    return ip


# FMAX ( r1 r2 -- r3 )
def prim_FMAX(inner, cur, ip):
    f2 = inner.pop_ds_float()
    f1 = inner.pop_ds_float()
    if f1 > f2:
        inner.push_ds_float(f1)
    else:
        inner.push_ds_float(f2)
    return ip


# FLOOR ( r1 -- r2 )
def prim_FLOOR(inner, cur, ip):
    """Round r1 toward negative infinity."""
    import math
    f = inner.pop_ds_float()
    inner.push_ds_float(math.floor(f))
    return ip


# FROUND ( r1 -- r2 )
def prim_FROUND(inner, cur, ip):
    """Round r1 to nearest integer."""
    f = inner.pop_ds_float()
    inner.push_ds_float(float(int(f + 0.5 if f >= 0 else f - 0.5)))
    return ip


# FLITERAL ( F: r -- ) compilation; ( -- F: r ) run-time
def prim_FLITERAL(inner, cur, ip):
    """Compile a float literal. At run-time, push the float onto the float stack."""
    if inner.depth_ds_float() > 0:
        floatval = inner.pop_ds_float()
        outer = inner.outer
        if outer is not None:
            outer._emit_lit(W_FloatObject(floatval))
    elif inner.ds_ptr_locals > 0:
        val = inner.pop_ds()
        outer = inner.outer
        if outer is not None:
            outer._emit_lit(val)
    else:
        print "FLITERAL: float stack underflow"
    return ip


import math

# FSQRT ( r1 -- r2 )
def prim_FSQRT(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.sqrt(f))
    return ip


# FSIN ( r1 -- r2 )
def prim_FSIN(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.sin(f))
    return ip


# FCOS ( r1 -- r2 )
def prim_FCOS(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.cos(f))
    return ip


# FTAN ( r1 -- r2 )
def prim_FTAN(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.tan(f))
    return ip


# FASIN ( r1 -- r2 )
def prim_FASIN(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.asin(f))
    return ip


# FACOS ( r1 -- r2 )
def prim_FACOS(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.acos(f))
    return ip


# FATAN ( r1 -- r2 )
def prim_FATAN(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.atan(f))
    return ip


# FATAN2 ( r1 r2 -- r3 )
def prim_FATAN2(inner, cur, ip):
    r2 = inner.pop_ds_float()
    r1 = inner.pop_ds_float()
    inner.push_ds_float(math.atan2(r1, r2))
    return ip


# FSINH ( r1 -- r2 )
def prim_FSINH(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.sinh(f))
    return ip


# FCOSH ( r1 -- r2 )
def prim_FCOSH(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.cosh(f))
    return ip


# FTANH ( r1 -- r2 )
def prim_FTANH(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.tanh(f))
    return ip


# FASINH ( r1 -- r2 )
def prim_FASINH(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.asinh(f))
    return ip


# FACOSH ( r1 -- r2 )
def prim_FACOSH(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.acosh(f))
    return ip


# FATANH ( r1 -- r2 )
def prim_FATANH(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.atanh(f))
    return ip


# FEXP ( r1 -- r2 )
def prim_FEXP(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.exp(f))
    return ip


# FEXPM1 ( r1 -- r2 )
def prim_FEXPM1(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.expm1(f))
    return ip


# FLN ( r1 -- r2 )
def prim_FLN(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.log(f))
    return ip


# FLNP1 ( r1 -- r2 )
def prim_FLNP1(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.log1p(f))
    return ip


# FLOG ( r1 -- r2 )
def prim_FLOG(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.log10(f))
    return ip


# FALOG ( r1 -- r2 )
def prim_FALOG(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.pow(10.0, f))
    return ip


# F** ( r1 r2 -- r3 )
def prim_FSTARSTAR(inner, cur, ip):
    r2 = inner.pop_ds_float()
    r1 = inner.pop_ds_float()
    inner.push_ds_float(math.pow(r1, r2))
    return ip


# F2* ( r1 -- r2 )
def prim_F2STAR(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(f * 2.0)
    return ip


# F2/ ( r1 -- r2 )
def prim_F2SLASH(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(f * 0.5)
    return ip


# 1/F ( r1 -- r2 )
def prim_1SLASHF(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(1.0 / f)
    return ip


# FTRUNC ( r1 -- r2 )
def prim_FTRUNC(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(float(int(f)))
    return ip


PI_VALUE = 3.141592653589793

# PI ( -- r )
def prim_PI(inner, cur, ip):
    inner.push_ds_float(PI_VALUE)
    return ip


# FSINCOS ( r1 -- r2 r3 )
def prim_FSINCOS(inner, cur, ip):
    f = inner.pop_ds_float()
    inner.push_ds_float(math.sin(f))
    inner.push_ds_float(math.cos(f))
    return ip


# F~ ( r1 r2 r3 -- flag )
def prim_FPROXIMATE(inner, cur, ip):
    r3 = inner.pop_ds_float()
    r2 = inner.pop_ds_float()
    r1 = inner.pop_ds_float()

    if r3 > 0.0:
        # Absolute tolerance
        result = abs(r1 - r2) < r3
    elif r3 == 0.0:
        # Exact comparison
        result = (r1 == r2)
    else:
        # Relative tolerance
        result = abs(r1 - r2) < abs(r3) * (abs(r1) + abs(r2))

    if result:
        inner.push_ds_int(-1)
    else:
        inner.push_ds_int(0)
    return ip


# Time-related primitives

# MS ( u -- )
def prim_MS(inner, cur, ip):
    """Wait at least u milliseconds."""
    import time
    u = inner.pop_ds_int()
    if u > 0:
        time.sleep(u / 1000.0)
    return ip


def _civil_from_days(z):
    """Gregorian (year, month, day) for a count of days since 1970-01-01.
    Howard Hinnant's civil_from_days, integer-only so it is RPython-safe."""
    z += 719468
    era = z // 146097
    doe = z - era * 146097
    yoe = (doe - doe // 1460 + doe // 36524 - doe // 146096) // 365
    y = yoe + era * 400
    doy = doe - (365 * yoe + yoe // 4 - yoe // 100)
    mp = (5 * doy + 2) // 153
    d = doy - (153 * mp + 2) // 5 + 1
    if mp < 10:
        m = mp + 3
    else:
        m = mp - 9
    if m <= 2:
        y += 1
    return y, m, d


# TIME&DATE ( -- nsec nmin nhour nday nmonth nyear )
def prim_TIME_AND_DATE(inner, cur, ip):
    """Current UTC time, decomposed."""
    from rpython.rlib.rtime import time
    t = int(time())
    days = t // 86400
    rem = t - days * 86400
    year, month, day = _civil_from_days(days)
    inner.push_ds_int(rem % 60)
    inner.push_ds_int((rem // 60) % 60)
    inner.push_ds_int(rem // 3600)
    inner.push_ds_int(day)
    inner.push_ds_int(month)
    inner.push_ds_int(year)
    return ip


# ARGC ( -- n )
def prim_ARGC(inner, cur, ip):
    """Push the number of command-line arguments (after the filename)."""
    inner.push_ds_int(len(inner.argv))
    return ip


# ARGV ( n -- c-addr u )
def prim_ARGV(inner, cur, ip):
    """Push the nth command-line argument as a counted string (c-addr u)."""
    n = inner.pop_ds_int()
    if n >= 0 and n < len(inner.argv):
        s = inner.argv[n]
        length = len(s)
        addr = inner.here
        for i in range(length):
            inner.char_store(addr + i, ord(s[i]))
        inner.here += length
        inner.push_ds_int(addr)
        inner.push_ds_int(length)
    else:
        inner.push_ds_int(0)
        inner.push_ds_int(0)
    return ip


# UTIME ( -- d )
def prim_UTIME(inner, cur, ip):
    from rpython.rlib.rtime import time
    t = time()
    usecs = int(t * 1000000.0)
    BIT_MASK = (1 << LONG_BIT) - 1
    low = usecs & BIT_MASK
    high = usecs >> LONG_BIT
    inner.push_ds_int(low)
    inner.push_ds_int(high)
    return ip


# CPUTIME ( -- duser dsystem )
def prim_CPUTIME(inner, cur, ip):
    from rpython.rlib.rtime import clock
    user_usecs = int(clock() * 1000000.0)
    BIT_MASK = (1 << LONG_BIT) - 1
    user_low = user_usecs & BIT_MASK
    user_high = user_usecs >> LONG_BIT
    inner.push_ds_int(user_low)
    inner.push_ds_int(user_high)
    inner.push_ds_int(0)
    inner.push_ds_int(0)
    return ip


# LITERAL ( x -- ) compilation; ( -- x ) run-time
def prim_LITERAL(inner, cur, ip):
    """Compile a literal. At run-time, push the value onto the stack."""
    if inner.ds_int_size() > 0:
        intval = inner.pop_ds_int()
        outer = inner.outer
        if outer is not None:
            outer._emit_lit(W_IntObject(intval))
    elif inner.depth_ds_float() > 0:
        floatval = inner.pop_ds_float()
        outer = inner.outer
        if outer is not None:
            outer._emit_lit(W_FloatObject(floatval))
    elif inner.ds_ptr_locals > 0:
        val = inner.pop_ds()
        outer = inner.outer
        if outer is not None:
            outer._emit_lit(val)
    else:
        print "LITERAL: stack underflow"
    return ip


def install_primitives(outer):
    outer.define_prim("0=", prim_ZEROEQUAL)
    outer.define_prim("0<", prim_ZEROLESS)
    outer.define_prim("0>", prim_ZEROGREATER)
    outer.define_prim(">",  prim_GREATER)
    outer.define_prim("<",  prim_LESS)
    outer.define_prim("0<>", prim_ZERONOTEQUAL)
    outer.define_prim("U<", prim_U_LESS)
    # stack manipulation
    outer.define_prim("DUP", prim_DUP)
    outer.define_prim("DROP", prim_DROP)
    outer.define_prim("NIP", prim_NIP)
    outer.define_prim("SWAP", prim_SWAP)
    outer.define_prim("OVER", prim_OVER)

    outer.define_prim("2DUP", prim_2DUP)
    outer.define_prim("2DROP", prim_2DROP)
    outer.define_prim("3DROP", prim_3DROP)
    outer.define_prim("2SWAP", prim_2SWAP)
    outer.define_prim("2OVER", prim_2OVER)

    outer.define_prim("?DUP", prim_QUESTIONDUP)

    outer.define_prim("ROT", prim_ROT)
    outer.define_prim("-ROT", prim_NROT)
    outer.define_prim("MAX", prim_MAX)
    outer.define_prim("MIN", prim_MIN)

    outer.define_prim("DEPTH", prim_DEPTH)

    outer.define_prim("RSHIFT", prim_RSHIFT)
    outer.define_prim("LSHIFT", prim_LSHIFT)

    outer.define_prim("S>D", prim_S_TO_D)
    outer.define_prim("D+", prim_D_PLUS)
    outer.define_prim("D-", prim_D_MINUS)
    outer.define_prim("D.", prim_D_DOT)
    outer.define_prim("BL", prim_BL)
    outer.define_prim("FILL", prim_FILL)
    outer.define_prim("MOVE", prim_MOVE)
    outer.define_prim("CMOVE", prim_CMOVE)
    outer.define_prim("CMOVE>", prim_CMOVE_UP)

    outer.define_prim("2*", prim_2STAR)
    outer.define_prim("2/", prim_2SLASH)

    # arithmetic
    outer.define_prim("+", prim_ADD)
    outer.define_prim("-", prim_SUB)
    outer.define_prim("*", prim_MUL)
    outer.define_prim("/", prim_DIV)
    outer.define_prim("*/", prim_MUL_SLASH)
    outer.define_prim("/MOD", prim_DIV_MOD)
    outer.define_prim("*/MOD", prim_MUL_DIV_MOD)

    outer.define_prim("ABS", prim_ABS)
    outer.define_prim("NEGATE", prim_NEGATE)
    outer.define_prim("MOD", prim_MOD)
    outer.define_prim("/", prim_DIV)
    outer.define_prim("/MOD", prim_DIVMOD)
    outer.define_prim("*/", prim_STARSLASH)
    outer.define_prim("*/MOD", prim_STARSLASHMOD)
    outer.define_prim("FM/MOD", prim_FMSLASHMOD)
    outer.define_prim("SM/REM", prim_SMSLASHREM)
    outer.define_prim("UM/MOD", prim_UMSLASHMOD)

    outer.define_prim("1+", prim_INC)
    outer.define_prim("1-", prim_DEC)

    outer.define_prim("M*", prim_MUL_STAR)
    outer.define_prim("M*/", prim_MSTARSLASH)
    outer.define_prim("UM*", prim_U_MUL_STAR)

    outer.define_prim("AND", prim_AND)
    outer.define_prim("OR", prim_OR)
    outer.define_prim("XOR", prim_XOR)
    outer.define_prim("INVERT", prim_INVERT)

    # comparison
    outer.define_prim("U<", prim_ULESS)

    outer.define_prim("FM/MOD", prim_FM_DIV_MOD)
    outer.define_prim("UM/MOD", prim_UM_DIV_MOD)
    outer.define_prim("SM/REM", prim_SM_DIV_REM)

    # I/O
    outer.define_prim(".", prim_DOT)
    outer.define_prim(".S", prim_DOT_S)
    outer.define_prim("U.", prim_U_DOT)
    outer.define_prim("EMIT", prim_EMIT)
    outer.define_prim("SPACE", prim_SPACE)
    outer.define_prim("SPACES", prim_SPACES)
    outer.define_prim("CR", prim_CR)
    outer.define_prim("U.", prim_UDOT)
    outer.define_prim("KEY", prim_KEY)
    outer.define_prim("ACCEPT", prim_ACCEPT)
    outer.define_prim("U.R", prim_UDOTR)
    outer.define_prim(".R", prim_DOTR)
    outer.define_prim("KEY?", prim_KEY_QUESTION)

    # memory management
    outer.define_prim("!", prim_STORE)
    outer.define_prim("2!", prim_2STORE)
    outer.define_prim("@", prim_FETCH)
    outer.define_prim("CELL", prim_CELL)
    outer.define_prim("CELL+", prim_CELLPLUS)
    outer.define_prim("CELLS", prim_CELLS)
    outer.define_prim("+!", prim_PLUSSTORE)
    outer.define_prim("2@", prim_2FETCH)
    outer.define_prim("C!", prim_C_STORE)
    outer.define_prim("C@", prim_C_FETCH)
    outer.define_prim("CHAR+", prim_CHAR_PLUS)
    outer.define_prim("CHARS", prim_CHARS)
    outer.define_prim("ALIGN", prim_ALIGN)
    outer.define_prim("ALIGNED", prim_ALIGNED)

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
    outer.define_prim("SIGN", prim_SIGN)

    outer.define_prim("TYPE", prim_TYPE)

    # loop
    outer.define_prim("0BRANCH", prim_0BRANCH)
    outer.define_prim("BRANCH", prim_BRANCH)
    outer.define_prim("(DO)", prim_DO_RUNTIME)
    outer.define_prim("(?DO)", prim_QDO_RUNTIME)
    outer.define_prim("(LOOP)", prim_LOOP_RUNTIME)
    outer.define_prim("(LOOPNP)", prim_LOOPNP_RUNTIME)
    outer.define_prim("(+LOOP)", prim_PLUSLOOP_RUNTIME)
    outer.define_prim("UNLOOP", prim_UNLOOP)
    outer.define_prim("LEAVE", prim_LEAVE)
    outer.define_prim("I", prim_I)
    outer.define_prim("J", prim_J)

    # thread ops
    outer.define_prim("LIT", prim_LIT)
    outer.define_prim("EXIT", prim_EXIT)
    outer.define_prim("TAILCALL", prim_TAILCALL)
    outer.define_prim("(ABORT\")", prim_ABORT_QUOTE_RUNTIME)

    # floating point
    outer.define_prim("F*", prim_FMUL)
    outer.define_prim("F+", prim_FADD)
    outer.define_prim("F-", prim_FSUB)
    outer.define_prim("F/", prim_FDIV)
    outer.define_prim("F>", prim_FGREATER)
    outer.define_prim("F<", prim_FLESS)
    outer.define_prim("F=", prim_FEQUAL)
    outer.define_prim("F0<", prim_FZEROLESS)
    outer.define_prim("F0=", prim_FZEROEQUAL)
    outer.define_prim("FSWAP", prim_FSWAP)
    outer.define_prim("S>F", prim_S2F)
    outer.define_prim("F!", prim_FSTORE)
    outer.define_prim("F@", prim_FFETCH)
    # DF!/DF@ (IEEE double, 8 bytes) map to F!/F@: floats here are 64-bit.
    outer.define_prim("DF!", prim_FSTORE)
    outer.define_prim("DF@", prim_FFETCH)
    outer.define_prim("FDUP", prim_FDUP)
    outer.define_prim("FDROP", prim_FDROP)
    outer.define_prim("FDEPTH", prim_FDEPTH)
    outer.define_prim("FOVER", prim_FOVER)
    outer.define_prim("FROT", prim_FROT)
    outer.define_prim("FNEGATE", prim_FNEGATE)
    outer.define_prim("FABS", prim_FABS)
    outer.define_prim("FMIN", prim_FMIN)
    outer.define_prim("FMAX", prim_FMAX)
    outer.define_prim("FLOOR", prim_FLOOR)
    outer.define_prim("FROUND", prim_FROUND)
    outer.define_prim("FLOAT", prim_FLOAT)
    outer.define_prim("FLOATS", prim_FLOATS)
    outer.define_prim("DFLOATS", prim_DFLOATS)
    outer.define_prim("SFLOATS", prim_SFLOATS)
    outer.define_prim("FALIGNED", prim_FALIGNED)
    outer.define_prim("DFALIGNED", prim_DFALIGNED)
    outer.define_prim("SFALIGNED", prim_SFALIGNED)
    outer.define_prim("FLOAT+", prim_FLOATPLUS)
    outer.define_prim("F.", prim_FDOT)
    outer.define_prim("D>F", prim_D2F)
    outer.define_prim("F>D", prim_F2D)
    outer.define_prim("SET-PRECISION", prim_SET_PRECISION)
    outer.define_prim("PRECISION", prim_PRECISION)
    outer.define_prim("REPRESENT", prim_REPRESENT)

    # floating point math library (Forth 2012 Floating-Point Extensions)
    outer.define_prim("FSQRT", prim_FSQRT)
    outer.define_prim("FSIN", prim_FSIN)
    outer.define_prim("FCOS", prim_FCOS)
    outer.define_prim("FTAN", prim_FTAN)
    outer.define_prim("FASIN", prim_FASIN)
    outer.define_prim("FACOS", prim_FACOS)
    outer.define_prim("FATAN", prim_FATAN)
    outer.define_prim("FATAN2", prim_FATAN2)
    outer.define_prim("FSINH", prim_FSINH)
    outer.define_prim("FCOSH", prim_FCOSH)
    outer.define_prim("FTANH", prim_FTANH)
    outer.define_prim("FASINH", prim_FASINH)
    outer.define_prim("FACOSH", prim_FACOSH)
    outer.define_prim("FATANH", prim_FATANH)
    outer.define_prim("FEXP", prim_FEXP)
    outer.define_prim("FEXPM1", prim_FEXPM1)
    outer.define_prim("FLN", prim_FLN)
    outer.define_prim("FLNP1", prim_FLNP1)
    outer.define_prim("FLOG", prim_FLOG)
    outer.define_prim("FALOG", prim_FALOG)
    outer.define_prim("F**", prim_FSTARSTAR)
    outer.define_prim("F2*", prim_F2STAR)
    outer.define_prim("F2/", prim_F2SLASH)
    outer.define_prim("1/F", prim_1SLASHF)
    outer.define_prim("FTRUNC", prim_FTRUNC)
    outer.define_prim("PI", prim_PI)
    outer.define_prim("FSINCOS", prim_FSINCOS)
    outer.define_prim("F~", prim_FPROXIMATE)

    # stack manipulation
    outer.define_prim("PICK", prim_PICK)
    outer.define_prim("ROLL", prim_ROLL)

    # return stack
    outer.define_prim(">R", prim_TORETURN)
    outer.define_prim("R>", prim_FROMRETURN)
    outer.define_prim("R@", prim_RFETCH)
    outer.define_prim("2>R", prim_2TORETURN)
    outer.define_prim("2R>", prim_2FROMRETURN)
    outer.define_prim("2R@", prim_2RFETCH)

    # dictionary
    outer.define_prim("EXECUTE", prim_EXECUTE)
    outer.define_prim("SP@", prim_SP_FETCH)
    # Dict stubs so ' / FIND locate [IF]/[ELSE]/[THEN]; token loop still handles them lexically.
    outer.define_prim("[IF]", prim_BRACKET_IF).immediate = True
    outer.define_prim("[ELSE]", prim_BRACKET_ELSE).immediate = True
    outer.define_prim("[THEN]", prim_BRACKET_THEN).immediate = True
    outer.define_prim("XT>STRING", prim_XT_TO_STRING)
    # NAME>STRING: this VM does not distinguish name tokens from execution tokens.
    outer.define_prim("NAME>STRING", prim_XT_TO_STRING)
    # PARSE-WORD: gforth alias of PARSE-NAME; avoids including parse-word.fs which uses >IN offsets this VM lacks.
    outer.define_prim("PARSE-WORD", prim_PARSE_NAME)
    outer.define_prim("(VOCABULARY)", prim_VOCAB_SELECT)
    outer.define_prim("CONSTANT", prim_CONSTANT)
    outer.define_prim("FCONSTANT", prim_CONSTANT)
    outer.define_prim("VARIABLE", prim_VARIABLE)
    outer.define_prim("FVARIABLE", prim_VARIABLE)
    outer.define_prim("CREATE", prim_CREATE)
    outer.define_prim("2CONSTANT", prim_2CONSTANT)
    outer.define_prim("2VARIABLE", prim_2VARIABLE)
    outer.define_prim("(", prim_PAREN).immediate = True
    outer.define_prim("WORDLIST", prim_WORDLIST)
    outer.define_prim("GET-ORDER", prim_GET_ORDER)
    outer.define_prim("SET-ORDER", prim_SET_ORDER)
    outer.define_prim("ALSO", prim_ALSO)
    outer.define_prim("PREVIOUS", prim_PREVIOUS)
    outer.define_prim(">ORDER", prim_TO_ORDER)
    outer.define_prim(">NUMBER", prim_TO_NUMBER)
    outer.define_prim("DEFINITIONS", prim_DEFINITIONS)
    outer.define_prim("FORTH", prim_FORTH)
    outer.define_prim("CS-ROLL", prim_CS_ROLL)
    outer.define_prim("DEFER", prim_DEFER)
    outer.define_prim("NEXTNAME", prim_NEXTNAME)
    outer.define_prim("'", prim_TICK)
    outer.define_prim("WORD", prim_WORD)
    outer.define_prim("PARSE", prim_PARSE)
    outer.define_prim("REFILL", prim_REFILL)
    outer.define_prim("MARKER", prim_MARKER)
    outer.define_prim("QUIT", prim_QUIT)
    outer.define_prim("FIND", prim_FIND)
    outer.define_prim("BASE", prim_BASE)
    outer.define_prim("UNUSED", prim_UNUSED)
    outer.define_prim(":INLINE", prim_INLINE_STUB)
    outer.define_prim("INCLUDED", prim_INCLUDED)
    outer.define_prim("REQUIRED", prim_INCLUDED)
    outer.define_prim(">IN", prim_TO_IN)
    outer.define_prim("SOURCE", prim_SOURCE)
    outer.define_prim("EVALUATE", prim_EVALUATE)
    outer.define_prim("COUNT", prim_COUNT)
    outer.define_prim("D2/", prim_D2SLASH)
    outer.define_prim("ABORT", prim_ABORT)
    outer.define_prim("(DEFER)", prim_DEFER_EXEC)
    outer.define_prim("(IS!)", prim_IS_STORE)
    outer.define_prim("(POSTPONE)", prim_POSTPONE)
    outer.define_prim("(CF)", prim_COMPILE_CF)
    outer.define_prim("COMPILE,", prim_COMPILE_COMMA)
    outer.define_prim("GET-CURRENT", prim_GET_CURRENT)
    outer.define_prim("SET-CURRENT", prim_SET_CURRENT)
    outer.define_prim("SEARCH-WORDLIST", prim_SEARCH_WORDLIST)
    outer.define_prim("FORTH-WORDLIST", prim_FORTH_WORDLIST)
    outer.define_prim("(:NONAME)", prim_BEGIN_NONAME)
    outer.define_prim("(:)", prim_BEGIN_NAMED)
    outer.define_prim("(;)", prim_END_DEF)
    outer.define_prim("(DOES>)", prim_DODOES)
    # SLITERAL is immediate: POSTPONE SLITERAL emits it so it runs during compilation of the enclosing word.
    outer.define_prim("SLITERAL", prim_SLITERAL).immediate = True
    outer.define_prim("CHAR", prim_CHAR)
    outer.define_prim("PARSE-NAME", prim_PARSE_NAME)
    outer.define_prim("DEFINED", prim_DEFINED)
    outer.define_prim("(STATE)", prim_STATE)
    outer.define_prim("(IMMEDIATE)", prim_IMMEDIATE)
    outer.define_prim("SAVE-INPUT", prim_SAVE_INPUT)
    outer.define_prim("RESTORE-INPUT", prim_RESTORE_INPUT)
    outer.define_prim("DEFER!", prim_DEFER_STORE)
    outer.define_prim("COMPARE", prim_COMPARE)
    outer.define_prim("SEARCH", prim_SEARCH)
    outer.define_prim(">BODY", prim_TOBODY)

    # data space
    outer.define_prim("HERE", prim_HERE)
    outer.define_prim(",", prim_COMMA)
    outer.define_prim("C,", prim_C_COMMA)
    outer.define_prim("ALLOT", prim_ALLOT)

    # comparison
    outer.define_prim("=", prim_EQUAL)
    outer.define_prim("<=", prim_LESSEQUAL)
    outer.define_prim(">=", prim_GREATEREQUAL)
    outer.define_prim("<>", prim_NOTEQUAL)

    # memory allocation
    outer.define_prim("ALLOCATE", prim_ALLOCATE)
    outer.define_prim("FREE", prim_FREE)
    outer.define_prim("RESIZE", prim_RESIZE)

    # file access
    outer.define_prim("R/O", prim_R_O)
    outer.define_prim("W/O", prim_W_O)
    outer.define_prim("R/W", prim_R_W)
    outer.define_prim("BIN", prim_BIN)
    outer.define_prim("STDIN", prim_STDIN)
    outer.define_prim("STDOUT", prim_STDOUT)
    outer.define_prim("STDERR", prim_STDERR)
    outer.define_prim("OPEN-FILE", prim_OPEN_FILE)
    outer.define_prim("CREATE-FILE", prim_CREATE_FILE)
    outer.define_prim("CLOSE-FILE", prim_CLOSE_FILE)
    outer.define_prim("READ-FILE", prim_READ_FILE)
    outer.define_prim("READ-LINE", prim_READ_LINE)
    outer.define_prim("WRITE-FILE", prim_WRITE_FILE)
    outer.define_prim("WRITE-LINE", prim_WRITE_LINE)
    outer.define_prim("FILE-POSITION", prim_FILE_POSITION)
    outer.define_prim("REPOSITION-FILE", prim_REPOSITION_FILE)
    outer.define_prim("FILE-SIZE", prim_FILE_SIZE)
    outer.define_prim("FLUSH-FILE", prim_FLUSH_FILE)
    outer.define_prim("DELETE-FILE", prim_DELETE_FILE)

    # exception handling
    outer.define_prim("THROW", prim_THROW)
    outer.define_prim("CATCH", prim_CATCH)

    # system
    outer.define_prim("BYE", prim_BYE)

    # command-line arguments
    outer.define_prim("ARGC", prim_ARGC)
    outer.define_prim("ARGV", prim_ARGV)

    # time
    outer.define_prim("MS", prim_MS)
    outer.define_prim("TIME&DATE", prim_TIME_AND_DATE)
    outer.define_prim("UTIME", prim_UTIME)
    outer.define_prim("CPUTIME", prim_CPUTIME)
