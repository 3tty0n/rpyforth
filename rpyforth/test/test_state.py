from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    for line in lines:
        outer.interpret_line(line)
    return inner


def test_state_false_when_interpreting():
    inner = run(["STATE @"])
    assert inner.pop_ds_int() == 0


def test_state_true_when_compiling():
    # grab-state runs at compile time of t (STATE = -1 during compilation).
    inner = run([
        "VARIABLE saved",
        ": grab-state  STATE @ saved ! ; IMMEDIATE",
        ": t  grab-state ;",
        "saved @",
    ])
    assert inner.pop_ds_int() == -1


def test_state_addr_is_stable():
    # STATE returns the same address each time (a variable, not scratch).
    inner = run(["STATE STATE ="])
    assert inner.pop_ds_int() == -1


def test_state_smart_word_compiles_or_runs():
    # marker (IMMEDIATE) executes at compile time of t and pushes -1 onto the stack.
    inner = run([
        ": marker  STATE @ ; IMMEDIATE",
        ": t  marker ;",
    ])
    # marker ran during compile of t and left -1 on the stack
    assert inner.pop_ds_int() == -1


def test_quit_resets_and_interpretation_continues():
    from rpyforth.outer_interp import OuterInterpreter
    from rpyforth.inner_interp import InnerInterpreter
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(": bail 1 2 3 quit ;")
    outer.interpret_line("7 bail")
    assert inner.depth_ds_int() == 0
    assert inner.rs_ptr == 0
    assert inner.cs_ptr == 0
    outer.interpret_line("42")
    assert inner.pop_ds_int() == 42


def test_quit_abandons_rest_of_line():
    from rpyforth.outer_interp import OuterInterpreter
    from rpyforth.inner_interp import InnerInterpreter
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(": bail quit ;")
    outer.interpret_line("bail 42")
    assert inner.depth_ds_int() == 0
    outer.interpret_line("5")
    assert inner.pop_ds_int() == 5
