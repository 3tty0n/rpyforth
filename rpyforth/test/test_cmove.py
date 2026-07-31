from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    outer.interpret_line(line)
    return inner


def test_cmove_copies_forward():
    inner = run(
        "CREATE S 8 ALLOT "
        "65 S C!  66 S 1+ C!  67 S 2 + C! "
        "S S 4 + 3 CMOVE "
        "S 4 + C@  S 5 + C@  S 6 + C@"
    )
    assert inner.pop_ds_int() == 67
    assert inner.pop_ds_int() == 66
    assert inner.pop_ds_int() == 65


def test_cmove_overlapping_forward_propagates():
    # CMOVE copies low->high; dest one above source smears the first byte
    inner = run(
        "CREATE S 8 ALLOT "
        "65 S C!  66 S 1+ C!  67 S 2 + C! "
        "S S 1+ 3 CMOVE "
        "S 1+ C@  S 2 + C@  S 3 + C@"
    )
    assert inner.pop_ds_int() == 65
    assert inner.pop_ds_int() == 65
    assert inner.pop_ds_int() == 65


def test_cmove_up_overlapping_preserves():
    # CMOVE> copies high->low; dest one above source preserves the bytes
    inner = run(
        "CREATE S 8 ALLOT "
        "65 S C!  66 S 1+ C!  67 S 2 + C! "
        "S S 1+ 3 CMOVE> "
        "S 1+ C@  S 2 + C@  S 3 + C@"
    )
    assert inner.pop_ds_int() == 67
    assert inner.pop_ds_int() == 66
    assert inner.pop_ds_int() == 65
