from rpyforth.objects import BINARY, OCTAL, DECIMAL, HEX, TRUE, ZERO
from rpyforth.util import digit_to_char

def prim_ZEROEQUAL(inner):     # 0=  ( x -- flag )
    w_x = inner.pop_ds()
    if w_x.zero_equal():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)

def prim_ZEROLESS(inner):      # 0< ( n -- flag )
    w_x = inner.pop_ds()
    if w_x.zero_less():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)

def prim_ZEROGREATER(inner):   # 0> ( n -- flag )
    w_x = inner.pop_ds()
    if w_x.zero_greater():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)

def prim_ZERONOTEQUAL(inner):  # 0<> ( n -- flag )
    w_x = inner.pop_ds()
    if not w_x.zero_equal():
        inner.push_ds(TRUE)
    else:
        inner.push_ds(ZERO)

def prim_DUP(inner):
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(a)

def prim_DROP(inner):
    inner.pop_ds()

def prim_SWAP(inner):
    a,b = inner.top2_ds()
    inner.push_ds(b)
    inner.push_ds(a)

def prim_OVER(inner):
    b = inner.pop_ds()
    a = inner.pop_ds()
    inner.push_ds(a)
    inner.push_ds(b)
    inner.push_ds(a)

# Arithmetic

def prim_ADD(inner):
    a,b = inner.top2_ds()
    inner.push_ds(a.add(b))

def prim_SUB(inner):
    a,b = inner.top2_ds()
    inner.push_ds(a.sub(b))

def prim_MUL(inner):
    a,b = inner.top2_ds()
    inner.push_ds(a.mul(b))

# memory management

def prim_STORE(inner):
    addr_obj = inner.pop_ds()
    val_obj  = inner.pop_ds()
    idx = addr_obj.intval
    inner.mem[idx] = val_obj

def prim_FETCH(inner):
    addr_obj = inner.pop_ds()
    idx = addr_obj.intval
    inner.push_ds(inner.mem[idx])

# BASE

def prim_BASE_FETCH(inner): #  BASE@ ( -- u )
    inner.push_ds(inner.base)


def prim_BASE_STORE(inner):  # BASE! ( u -- )
    u = inner.pop_ds()
    inner.base = u

def prim_DECIMAL(inner): inner.base = DECIMAL
def prim_HEX(inner):     inner.base = HEX
def prim_OCTAL(inner):   inner.base = OCTAL
def prim_BINARY(inner):  inner.base = BINARY

from rpyforth.objects import W_IntObject, W_StringObject

def prim_LESSNUM(inner):         # <#   ( -- )
    """
    Begins Pictured Numeric Output (PNO) conversion. PNO is a method of
    formatting numbers for display on the screen, using # symbols to represent
    digits. PNO actually converts the number to be displayed to a string,
    allowing the opportunity to insert characters into the character stream as
    conversion progresses (for example, commas, to separate hundreds and
    thousands, etc.). See # and #> for more information.
    """
    inner._pno_active = True
    inner._pno_buf = []


def prim_NUMSIGN(inner):         # #    ( x -- q )
    """
    The word # takes a single digit from the unsigned-double number on the
    stack (it divides the number by the current number base, as determined by
    BASE) and places this digit into the PNO buffer for display later.
    """
    if not inner._pno_active:
        inner.print_str(W_StringObject("# outside <# #>"))
        return
    x = inner.pop_ds()
    base = inner.base.intval
    q = x.intval // base
    r = x.intval %  base
    inner._pno_buf.insert(0, digit_to_char(r))
    inner.push_ds(W_IntObject(q))

def prim_NUMSIGN_S(inner):       # #S   ( x -- 0 )
    """
    The word #S converts all remaining digits until the value on the stack is
    0 (i.e. there are no more digits to convert).
    """
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

def prim_HOLD(inner):
    """
    HOLD inserts the ASCII value on the stack directly into the PNO buffer at
    the current PNO buffer position. Normally the word ASCII or CHAR is used to
    obtain the ASCII value of the character immediately following it.
    """
    if not inner._pno_active:
        inner.print_str(W_StringObject("HOLD outside <# #>"))
        return
    ch = inner.pop_ds()
    inner._pno_buf.insert(0, chr(ch.intval))

def prim_NUMGREATER(inner):      # #>   ( x -- string )
    """
    #> ends pictured numeric output. The double value that has/was been
    converted is removed from the stack and is replaced with the address and
    length of the string that has been built in the PNO buffer. This
    address/length pair is suitable for use with TYPE for displaying the
    string in the PNO buffer.
    """
    if not inner._pno_active:
        inner.print_str(W_StringObject("#> outside <# #>"))
        return
    _ = inner.pop_ds()
    s = ''.join(inner._pno_buf)
    inner._pno_active = False
    inner.push_ds(W_StringObject(s))

def prim_TYPE(inner):            # TYPE ( string -- )
    w_s = inner.pop_ds()
    inner.print_str(w_s)

# I/O

def prim_DOT(inner):
    x = inner.pop_ds()
    inner.print_int(x)

# CodeThread-aware primitives

def prim_LIT(inner):
    inner.prim_LIT()

def prim_EXIT(inner):
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

    # thread ops
    outer.define_prim("LIT",  prim_LIT)
    outer.define_prim("EXIT", prim_EXIT)
