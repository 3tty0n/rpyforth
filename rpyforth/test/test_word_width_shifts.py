"""Tests for word-width-shift bugs in arithmetic primitives.

All expected values verified against gforth-0.7.9.  The bugs described here
are *translated-only*: CPython / untranslated PyPy use arbitrary-precision
integers, so the bit-masking and shifts produce the correct large values
accidentally.  Under RPython translation (64-bit backend) `1 << 64` is masked
to 0 and `x >> 64` is UB/0, so every site that used `(1 << LONG_BIT) - 1` as
a mask got -1, turning unsigned ops into signed ones.

Fixed primitives and their classification:
  U. / U.R    -- class (b) broken translated: BIT_MASK=-1, str(negative) wrong
  UM*         -- class (b)+(c): high = c >> 64 = 0 always
  M*          -- class (b)+(c): same + sign-fixup with 1<<64 = 0
  D+ / D-     -- class (b)+(c): hi << 64 = 0, hi contribution lost
  D2/         -- class (b)+(c): same
  D.          -- class (b)+(c): hi << 64 = 0, prints only lo
  FM/MOD      -- class (b)+(c): (b << 64) = 0, hi lost
  UM/MOD      -- class (b)+(c): same + sign-fixup with 1<<64 = 0

Left as-is (harmless):
  UTIME / CPUTIME -- high cell is always 0 for current timestamps; >> 64 of a
                     small positive is 0 in both Python and translated, fine.
  */ assert       -- debug guards only, valid-range results make them vacuous.
"""

import pytest
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.outer_interp import OuterInterpreter


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner


def run_and_pop(line):
    return run(line).pop_ds_int()


# U. -- LONG_BIT shift hazard: BIT_MASK = (1<<64)-1 was -1 in translated builds.

def test_udot_minus_one(capfd):
    # gforth: -1 U. CR => 18446744073709551615
    run("-1 U.")
    out, _ = capfd.readouterr()
    assert out.strip() == "18446744073709551615"


def test_udot_minus_two(capfd):
    # gforth: -2 U. CR => 18446744073709551614
    run("-2 U.")
    out, _ = capfd.readouterr()
    assert out.strip() == "18446744073709551614"


def test_udot_zero(capfd):
    # gforth: 0 U. CR => 0
    run("0 U.")
    out, _ = capfd.readouterr()
    assert out.strip() == "0"


def test_udot_positive(capfd):
    # gforth: 1 U. CR => 1
    run("1 U.")
    out, _ = capfd.readouterr()
    assert out.strip() == "1"


# U.R -- same LONG_BIT hazard; right-justifies unsigned value in field width.

def test_udotr_minus_one(capfd):
    # gforth: -1 20 U.R => "18446744073709551615" (exactly 20 chars, no padding)
    run("-1 20 U.R")
    out, _ = capfd.readouterr()
    assert out.rstrip('\n') == "18446744073709551615"


def test_udotr_zero(capfd):
    # gforth: 0 20 U.R => "                   0"
    run("0 20 U.R")
    out, _ = capfd.readouterr()
    assert out.rstrip('\n') == "                   0"


# UM* ( u1 u2 -- ud ) -- hi = c >> 64 was always 0 in translated (LONG_BIT hazard).

def test_um_star_small():
    # gforth: 1024 4 UM* swap . . => 4096 0
    inner = run("1024 4 UM*")
    assert inner.pop_ds_int() == 0       # hi
    assert inner.pop_ds_int() == 4096    # lo


def test_um_star_high_cell():
    # gforth: 9223372036854775808 2 UM* swap . . => 0 1
    inner = run("9223372036854775808 2 UM*")
    assert inner.pop_ds_int() == 1       # hi
    assert inner.pop_ds_int() == 0       # lo


def test_um_star_minus_one_times_two():
    # gforth: -1 2 UM* swap . . => -2 1 (lo=-2==2^64-2, hi=1)
    inner = run("-1 2 UM*")
    assert inner.pop_ds_int() == 1       # hi
    assert inner.pop_ds_int() == -2      # lo (signed representation of 2^64-2)


def test_um_star_minus_one_times_minus_one():
    # gforth: 18446744073709551615 18446744073709551615 UM* swap . . => 1 -2 (lo=1, hi=-2==2^64-2 unsigned)
    inner = run("18446744073709551615 18446744073709551615 UM*")
    assert inner.pop_ds_int() == -2      # hi (signed repr of 2^64-2)
    assert inner.pop_ds_int() == 1       # lo


def test_um_star_maxuint_times_two():
    # gforth: 18446744073709551615 2 UM* swap . . => -2 1
    inner = run("18446744073709551615 2 UM*")
    assert inner.pop_ds_int() == 1       # hi
    assert inner.pop_ds_int() == -2      # lo


# M* ( n1 n2 -- d ) -- same LONG_BIT hazard plus sign-fixup with 1<<64 = 0.

def test_m_star_small():
    # gforth: 1024 4 M* swap . . => 4096 0
    inner = run("1024 4 M*")
    assert inner.pop_ds_int() == 0       # hi
    assert inner.pop_ds_int() == 4096    # lo


def test_m_star_negative():
    # gforth: -1024 4 M* swap . . => -4096 -1
    inner = run("-1024 4 M*")
    assert inner.pop_ds_int() == -1      # hi
    assert inner.pop_ds_int() == -4096   # lo


def test_m_star_both_negative():
    # gforth: -1024 -4 M* swap . . => 4096 0
    inner = run("-1024 -4 M*")
    assert inner.pop_ds_int() == 0       # hi
    assert inner.pop_ds_int() == 4096    # lo


def test_m_star_overflow():
    # gforth: 9223372036854775807 2 M* swap . . => -2 0
    inner = run("9223372036854775807 2 M*")
    assert inner.pop_ds_int() == 0       # hi
    assert inner.pop_ds_int() == -2      # lo


def test_m_star_minus_one_times_two():
    # gforth: -1 2 M* swap . . => -2 -1
    inner = run("-1 2 M*")
    assert inner.pop_ds_int() == -1      # hi
    assert inner.pop_ds_int() == -2      # lo


def test_m_star_minus_one_squared():
    # gforth: -1 -1 M* swap . . => 1 0
    inner = run("-1 -1 M*")
    assert inner.pop_ds_int() == 0       # hi
    assert inner.pop_ds_int() == 1       # lo


# D+ ( d1 d2 -- d3 ) -- hi << 64 was 0 in translated (LONG_BIT hazard); hi contribution was lost.

def test_d_plus_basic():
    # gforth: -1 0 1 0 D+ swap . . => 0 1 (lo=0, hi=1, value=2^64)
    inner = run("-1 0 1 0 D+")
    assert inner.pop_ds_int() == 1    # hi
    assert inner.pop_ds_int() == 0    # lo


def test_d_plus_wrap():
    # gforth: -1 -1 1 0 D+ swap . . => 0 0 (d1=-1, d2=1, result=0)
    inner = run("-1 -1 1 0 D+")
    assert inner.pop_ds_int() == 0    # hi
    assert inner.pop_ds_int() == 0    # lo


def test_d_plus_carry_into_hi():
    # gforth: -1 0 -1 0 D+ swap . . => -2 1 (d1=d2=2^64-1, result lo=-2==2^64-2, hi=1)
    inner = run("-1 0 -1 0 D+")
    assert inner.pop_ds_int() == 1    # hi
    assert inner.pop_ds_int() == -2   # lo


def test_d_plus_hi_cells():
    # gforth: 0 1 0 1 D+ swap . . => 0 2
    inner = run("0 1 0 1 D+")
    assert inner.pop_ds_int() == 2    # hi
    assert inner.pop_ds_int() == 0    # lo


# D- ( d1 d2 -- d3 ) -- same LONG_BIT hazard as D+.

def test_d_minus_basic():
    # gforth: -1 0 1 0 D- swap . . => -2 0 (2^64-1 - 1 = 2^64-2)
    inner = run("-1 0 1 0 D-")
    assert inner.pop_ds_int() == 0    # hi
    assert inner.pop_ds_int() == -2   # lo


def test_d_minus_borrow():
    # gforth: 1 0 -1 0 D- swap . . => 2 -1 (lo=2, hi=-1; value = 2-2^64)
    inner = run("1 0 -1 0 D-")
    assert inner.pop_ds_int() == -1   # hi
    assert inner.pop_ds_int() == 2    # lo


def test_d_minus_hi_borrow():
    # gforth: 0 1 0 0 D- swap . . => 0 1
    inner = run("0 1 0 0 D-")
    assert inner.pop_ds_int() == 1    # hi
    assert inner.pop_ds_int() == 0    # lo


# D2/ ( d -- d/2 ) -- arithmetic right shift; same LONG_BIT hazard as D+/D-.

def test_d2slash_even_positive():
    # gforth: -2 0 D2/ swap . . => 9223372036854775807 0 (d=2^64-2, D2/=2^63-1)
    inner = run("-2 0 D2/")
    assert inner.pop_ds_int() == 0                     # hi
    assert inner.pop_ds_int() == 9223372036854775807   # lo


def test_d2slash_odd_unsigned():
    # gforth: -1 0 D2/ swap . . => 9223372036854775807 0 (d=2^64-1, truncate toward -inf)
    inner = run("-1 0 D2/")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 9223372036854775807


def test_d2slash_with_hi():
    # gforth: 0 1 D2/ swap . . => -9223372036854775808 0 (d=2^64, D2/=2^63; signed lo wraps)
    inner = run("0 1 D2/")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == -9223372036854775808


def test_d2slash_negative():
    # gforth: -2 -1 D2/ swap . . => -1 -1 (d=-2, D2/=-1)
    inner = run("-2 -1 D2/")
    assert inner.pop_ds_int() == -1
    assert inner.pop_ds_int() == -1


def test_d2slash_zero():
    inner = run("0 0 D2/")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 0


# D. ( d -- ) -- hi << 64 was 0 in translated; only lo was printed.

def test_d_dot_minus_one(capfd):
    # gforth: -1 -1 D. => "-1 " (value = 2^64-1 + (-1)*2^64 = -1)
    run("-1 -1 D.")
    out, _ = capfd.readouterr()
    assert out.strip() == "-1"


def test_d_dot_large_positive(capfd):
    # gforth: 0 1 D. => "18446744073709551616 "
    run("0 1 D.")
    out, _ = capfd.readouterr()
    assert out.strip() == "18446744073709551616"


def test_d_dot_max_low(capfd):
    # gforth: -1 0 D. => "18446744073709551615 " (= 2^64-1)
    run("-1 0 D.")
    out, _ = capfd.readouterr()
    assert out.strip() == "18446744073709551615"


def test_d_dot_negative(capfd):
    # gforth: 0 -1 D. => "-18446744073709551616 " (= -(2^64))
    run("0 -1 D.")
    out, _ = capfd.readouterr()
    assert out.strip() == "-18446744073709551616"


# FM/MOD ( d1 n1 -- n2 n3 ) -- b<<64 was 0 in translated; hi cell contribution lost.

def test_fm_mod_positive():
    # gforth: 7 S>D 3 FM/MOD . . => 1 2 (rem=1, quot=2)
    inner = run("7 S>D 3 FM/MOD")
    assert inner.pop_ds_int() == 2    # quot
    assert inner.pop_ds_int() == 1    # rem


def test_fm_mod_neg_dividend():
    # gforth: -7 S>D 3 FM/MOD . . => 2 -3 (rem=2, quot=-3, floored)
    inner = run("-7 S>D 3 FM/MOD")
    assert inner.pop_ds_int() == -3   # quot
    assert inner.pop_ds_int() == 2    # rem (note: 2, not -1, because floored)


def test_fm_mod_neg_divisor():
    # gforth: 7 S>D -3 FM/MOD . . => -3 -2 (quot=-3, rem=-2)
    inner = run("7 S>D -3 FM/MOD")
    assert inner.pop_ds_int() == -3   # quot (floored toward -inf)
    assert inner.pop_ds_int() == -2   # rem


def test_fm_mod_both_negative():
    # gforth: -7 S>D -3 FM/MOD . . => 2 -1 (floor(-7/-3)=2, rem=-1)
    inner = run("-7 S>D -3 FM/MOD")
    assert inner.pop_ds_int() == 2    # quot
    assert inner.pop_ds_int() == -1   # rem


def test_fm_mod_hi_cell():
    # gforth: -1 -1 3 FM/MOD . . => -1 2 (d=-1, floor(-1/3)=-1, rem=2)
    inner = run("-1 -1 3 FM/MOD")
    assert inner.pop_ds_int() == -1   # quot
    assert inner.pop_ds_int() == 2    # rem


# UM/MOD ( ud u1 -- u2 u3 ) -- same LONG_BIT hazard plus sign-fixup with 1<<64=0.

def test_um_div_mod_simple():
    # gforth: 2 0 3 UM/MOD . . => 0 2 (quot=0, rem=2)
    inner = run("2 0 3 UM/MOD")
    assert inner.pop_ds_int() == 0    # quot
    assert inner.pop_ds_int() == 2    # rem


def test_um_div_mod_hi_cell():
    # gforth: 0 1 3 UM/MOD . . => 6148914691236517205 1 (quot on top, rem below)
    inner = run("0 1 3 UM/MOD")
    assert inner.pop_ds_int() == 6148914691236517205   # quot
    assert inner.pop_ds_int() == 1                     # rem


def test_um_div_mod_unsigned_lo():
    # gforth: -1 0 3 UM/MOD . . => 6148914691236517205 0 (ud_lo=2^64-1, rem=0)
    inner = run("-1 0 3 UM/MOD")
    assert inner.pop_ds_int() == 6148914691236517205   # quot
    assert inner.pop_ds_int() == 0                     # rem
