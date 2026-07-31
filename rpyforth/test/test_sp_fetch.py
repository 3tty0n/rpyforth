from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_sp_fetch_addresses_top_cell():
    # brew's mutation-0.3.fs uses `sp@ cell cat` to copy the top data-stack cell. SP@ returns an address whose cell holds the current top value.
    inner, _ = run_lines(["42 sp@ @"])
    # ( 42 addr ) -> @ reads the stashed top value 42; 42 remains below.
    assert inner.pop_ds_int() == 42
    assert inner.pop_ds_int() == 42


def test_sp_fetch_reflects_current_top():
    inner, _ = run_lines(["7 sp@ @ nip", "99 sp@ @ nip"])
    assert inner.pop_ds_int() == 99
    assert inner.pop_ds_int() == 7
