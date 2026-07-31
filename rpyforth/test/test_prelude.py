from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run_with_prelude(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    outer.interpret_line(line)
    return inner


def pop(line):
    return run_with_prelude(line).pop_ds_int()


def test_prelude_does_not_break_core():
    assert pop("2 3 +") == 5


def test_true_false():
    assert pop("TRUE") == -1
    assert pop("FALSE") == 0


def test_tuck():
    # a b -- b a b
    inner = run_with_prelude("1 2 TUCK")
    assert inner.pop_ds_int() == 2
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 2


def test_u_greater():
    assert pop("3 2 U>") == -1
    assert pop("2 3 U>") == 0
    assert pop("2 2 U>") == 0


def test_within():
    # n lo hi -- flag ; true iff lo <= n < hi
    assert pop("5 1 10 WITHIN") == -1
    assert pop("1 1 10 WITHIN") == -1
    assert pop("0 1 10 WITHIN") == 0
    assert pop("10 1 10 WITHIN") == 0


def test_erase():
    inner = run_with_prelude(
        "CREATE BUF 8 ALLOT  BUF 3 255 FILL  BUF 3 ERASE  BUF C@"
    )
    assert inner.pop_ds_int() == 0


def test_slash_string():
    # c-addr u n -- c-addr+n  u-n
    inner = run_with_prelude("100 10 3 /STRING")
    assert inner.pop_ds_int() == 7
    assert inner.pop_ds_int() == 103


def test_blank():
    inner = run_with_prelude("CREATE B 8 ALLOT  B 8 0 FILL  B 3 BLANK  B C@")
    assert inner.pop_ds_int() == 32  # BL


def test_dash_trailing_strips_spaces():
    inner = run_with_prelude(
        "CREATE S 8 ALLOT  65 S C!  66 S 1+ C!  32 S 2 + C!  32 S 3 + C! "
        "S 4 -TRAILING"
    )
    assert inner.pop_ds_int() == 2   # two trailing spaces stripped
    inner.pop_ds_int()               # drop addr


def test_dash_trailing_all_spaces():
    inner = run_with_prelude("CREATE S 4 ALLOT  S 4 32 FILL  S 4 -TRAILING")
    assert inner.pop_ds_int() == 0
    inner.pop_ds_int()


def test_dnegate():
    # d + (-d) = 0  (representation-agnostic: avoids signed/unsigned cell display)
    assert pop("5 0 DNEGATE  5 0 D+  D0=") == -1


def test_d0_equals():
    assert pop("0 0 D0=") == -1
    assert pop("5 0 D0=") == 0
    assert pop("0 7 D0=") == 0


def test_d2star():
    inner = run_with_prelude("5 0 D2*")
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == 10


def test_cell_minus():
    # round-trip, stride-agnostic
    assert pop("100 CELL+ CELL-") == 100
    assert pop("50 CELL- CELL+") == 50


def test_m_plus():
    # (d + n): 5. + 3 = 8.
    assert pop("5 0 3 M+  8 0 D-  D0=") == -1


def test_d_equals():
    assert pop("5 0 5 0 D=") == -1
    assert pop("5 0 6 0 D=") == 0


def test_d_not_equals():
    assert pop("5 0 6 0 D<>") == -1
    assert pop("5 0 5 0 D<>") == 0


def test_d_less_than():
    assert pop("1 0 3 0 D<") == -1
    assert pop("3 0 1 0 D<") == 0
    assert pop("5 0 5 0 D<") == 0


def test_d_greater_than():
    assert pop("3 0 1 0 D>") == -1
    assert pop("1 0 3 0 D>") == 0
    assert pop("5 0 5 0 D>") == 0


def test_d0_less_than():
    assert pop("-1 -1 D0<") == -1
    assert pop("0 -1 D0<") == -1
    assert pop("-1 0 D0<") == 0
    assert pop("0 0 D0<") == 0


def test_pad_round_trips():
    # PAD is a stable scratch buffer: store then fetch at the same address
    inner = run_with_prelude("PAD 42 SWAP !  PAD @")
    assert inner.pop_ds_int() == 42


def test_compare():
    assert pop('S" abc" S" abc" COMPARE') == 0
    assert pop('S" abc" S" abd" COMPARE') == -1
    assert pop('S" abd" S" abc" COMPARE') == 1
    assert pop('S" ab" S" abc" COMPARE') == -1


def test_bounds():
    inner = run_with_prelude("10 5 BOUNDS")
    hi = inner.pop_ds_int()
    lo = inner.pop_ds_int()
    assert lo == 15 and hi == 10


def test_d2slash():
    inner = run_with_prelude("20 0 D2/")
    hi = inner.pop_ds_int()
    lo = inner.pop_ds_int()
    assert lo == 10 and hi == 0


def test_perform():
    # PERFORM ( addr -- )  performs @ EXECUTE: fetch xt from addr and run it
    inner = run_with_prelude(
        ": seven 7 ;  CREATE vec ' seven ,  vec PERFORM"
    )
    assert inner.pop_ds_int() == 7
