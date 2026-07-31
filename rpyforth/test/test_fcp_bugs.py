"""Tests for fcp end-to-end bugs: POSTPONE semantics and WORD here-stability."""
from rpyforth.inner_interp import InnerInterpreter, HEAP_SIZE_BYTES
from rpyforth.outer_interp import OuterInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner


def test_postpone_nonimmediate_defers_not_executes():
    """POSTPONE on a non-immediate word must compile the word, not run it."""
    inner = run_lines([
        ": my-0< POSTPONE 0< ; IMMEDIATE",
        "42",
        ": check my-0< ;",
        "-1 check",
    ])
    assert inner.pop_ds_int() == -1


def test_postpone_does_not_consume_stack_at_compile_time():
    """Stack value present at compile time must survive compilation unchanged."""
    inner = run_lines([
        ": wrap-drop POSTPONE DROP ; IMMEDIATE",
        "999",
        ": skip wrap-drop ;",
        "999",
    ])
    assert inner.pop_ds_int() == 999
    assert inner.pop_ds_int() == 999


def test_fcp_northerly_pattern():
    """Exact fcp pattern: UNUSED on stack survives compilation of a POSTPONE word."""
    inner = run_lines([
        "UNUSED",
        ": Northerly? POSTPONE 0< ; IMMEDIATE",
        ": sqAttacks? Northerly? ;",
    ])
    u0 = inner.pop_ds_int()
    assert u0 > 0


def test_fcp_total_bytes_positive():
    """UNUSED delta computed around some compilation must be non-negative.

    fcp does:
        UNUSED                    ( u0 )
        ... many definitions ...
        UNUSED - .                ( u0 - u1 -- prints "total bytes used" )

    u1 <= u0 because here can only grow, so u0 - u1 >= 0.
    If POSTPONE corrupts the stack at compile time (consuming u0 and replacing
    it with 0), u0 - u1 becomes 0 - u1 = negative.
    """
    inner = run_lines([
        "UNUSED",
        ": Northerly? POSTPONE 0< ; IMMEDIATE",
        ": check1 Northerly? ;",
        ": check2 Northerly? ;",
        "UNUSED",
    ])
    u1 = inner.pop_ds_int()
    u0 = inner.pop_ds_int()
    assert u0 > 0, "u0 must be positive (UNUSED before compilation)"
    assert u1 >= 0, "u1 must be non-negative"
    assert u0 >= u1, "bytes used must be non-negative (u0 - u1 >= 0)"


def test_bl_word_does_not_advance_here():
    """BL WORD must leave HERE unchanged."""
    inner = run_lines([
        "HERE",
        "BL WORD hello",
        "HERE",
    ])
    here_after = inner.pop_ds_int()
    _ = inner.pop_ds_int()   # address returned by WORD
    here_before = inner.pop_ds_int()
    assert here_after == here_before, (
        "HERE changed after BL WORD: before=%d after=%d" % (here_before, here_after)
    )


def test_bl_word_in_colon_body_does_not_advance_here():
    """BL WORD inside a colon definition must not disturb HERE."""
    inner = run_lines([
        ": read-token BL WORD DROP ;",
        "HERE",
        "read-token abc",
        "HERE",
    ])
    here_after = inner.pop_ds_int()
    here_before = inner.pop_ds_int()
    assert here_after == here_before


def test_repeated_bl_word_stable_here():
    """Multiple BL WORD calls must not cumulatively advance HERE."""
    inner = run_lines([
        ": eat BL WORD DROP ;",
        "HERE eat a eat bb eat ccc eat dddd HERE -",
    ])
    delta = inner.pop_ds_int()
    assert delta == 0, "HERE drifted by %d after repeated BL WORD calls" % delta


def test_unused_nonnegative():
    inner = run_lines(["UNUSED"])
    u = inner.pop_ds_int()
    assert u >= 0


def test_unused_decreases_after_allot():
    inner = run_lines([
        "UNUSED",
        "64 ALLOT",
        "UNUSED",
    ])
    u1 = inner.pop_ds_int()
    u0 = inner.pop_ds_int()
    assert u0 > u1, "UNUSED should decrease after ALLOT"
    assert u0 - u1 >= 64


def test_cputime_pushes_four_cells():
    """CPUTIME pushes two double-cell values (user, sys) = 4 cells total."""
    inner = run_lines(["CPUTIME"])
    sys_hi = inner.pop_ds_int()
    sys_lo = inner.pop_ds_int()
    user_hi = inner.pop_ds_int()
    user_lo = inner.pop_ds_int()
    assert user_lo >= 0
    assert user_hi >= 0
    assert sys_lo >= 0
    assert sys_hi >= 0


def test_ms_at_via_cputime_nondecreasing():
    """ms@ (via cputime d+ 1000 um/mod nip) must return non-decreasing values."""
    inner = run_lines([
        ": ms@ cputime d+ 1000 um/mod nip ;",
        "ms@ ms@",
    ])
    t1 = inner.pop_ds_int()
    t0 = inner.pop_ds_int()
    assert t0 >= 0, "ms@ returned negative: %d" % t0
    assert t1 >= t0, "ms@ not non-decreasing: t0=%d t1=%d" % (t0, t1)
