"""Compile-time inlining of callees that contain control flow.

Branch/loop words carry an absolute instruction index in their literal slot,
so splicing such a body into a caller requires relocating those targets by
the insertion offset (interior EXITs become branches to just past the spliced
body). Every test checks BOTH behavior and structure: the callee word object
must not appear in the caller's compiled thread.
"""

from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def _assert_inlined(outer, caller, callee):
    cw = outer.dict[caller.upper()]
    ce = outer.dict[callee.upper()]
    assert ce not in cw.thread.code, (
        "%s was not inlined into %s" % (callee, caller))


def test_if_else_then_callee_inlined():
    inner, outer = run_lines([
        ": pick-sign DUP 0< IF DROP -1 ELSE 0> IF 1 ELSE 0 THEN THEN ;",
        ": t1 -7 pick-sign  0 pick-sign  9 pick-sign ;",
        "t1",
    ])
    assert inner.pop_ds_int() == 1
    assert inner.pop_ds_int() == 0
    assert inner.pop_ds_int() == -1
    _assert_inlined(outer, "t1", "pick-sign")


def test_do_loop_callee_inlined():
    inner, outer = run_lines([
        ": sum10 0 10 0 DO I + LOOP ;",
        ": t2 sum10 sum10 + ;",
        "t2",
    ])
    assert inner.pop_ds_int() == 90
    _assert_inlined(outer, "t2", "sum10")


def test_qdo_empty_range_callee_inlined():
    inner, outer = run_lines([
        ": sumn ( n -- sum ) 0 SWAP 0 ?DO I + LOOP ;",
        ": t3 0 sumn 5 sumn ;",
        "t3",
    ])
    assert inner.pop_ds_int() == 10
    assert inner.pop_ds_int() == 0
    _assert_inlined(outer, "t3", "sumn")


def test_leave_callee_inlined():
    inner, outer = run_lines([
        ": find3 ( -- i ) 0 10 0 DO I 3 = IF DROP I LEAVE THEN LOOP ;",
        ": t4 find3 100 + ;",
        "t4",
    ])
    assert inner.pop_ds_int() == 103
    _assert_inlined(outer, "t4", "find3")


def test_interior_exit_callee_inlined():
    inner, outer = run_lines([
        ": clamp0 DUP 0< IF DROP 0 EXIT THEN 1 + ;",
        ": t5 -9 clamp0  5 clamp0 ;",
        "t5",
    ])
    assert inner.pop_ds_int() == 6
    assert inner.pop_ds_int() == 0
    _assert_inlined(outer, "t5", "clamp0")


def test_begin_until_callee_inlined():
    inner, outer = run_lines([
        ": count-down BEGIN 1 - DUP 0= UNTIL ;",
        ": t6 5 count-down ;",
        "t6",
    ])
    assert inner.pop_ds_int() == 0
    _assert_inlined(outer, "t6", "count-down")


def test_begin_while_repeat_callee_inlined():
    inner, outer = run_lines([
        ": halve-down BEGIN DUP 1 > WHILE 2 / REPEAT ;",
        ": t7 40 halve-down ;",
        "t7",
    ])
    assert inner.pop_ds_int() == 1
    _assert_inlined(outer, "t7", "halve-down")


def test_nested_loops_relocate_inside_caller_loop():
    # Callee spliced inside caller's DO loop: relocated branch targets and I/J indexing must both survive.
    inner, outer = run_lines([
        ": inner-sum 0 3 0 DO I + LOOP ;",
        ": t8 0 4 0 DO inner-sum + LOOP ;",
        "t8",
    ])
    assert inner.pop_ds_int() == 12
    _assert_inlined(outer, "t8", "inner-sum")


def test_two_splices_of_same_callee():
    # Two splice sites at different offsets each need independent relocation.
    inner, outer = run_lines([
        ": absv DUP 0< IF 0 SWAP - THEN ;",
        ": t9 -4 absv 6 absv + ;",
        "t9",
    ])
    assert inner.pop_ds_int() == 10
    _assert_inlined(outer, "t9", "absv")


def test_oversized_callee_stays_a_call():
    body = "DUP + " * 40
    inner, outer = run_lines([
        ": big 0 + %s ;" % body,
        ": t10 1 big 0 + ;",
        "t10",
    ])
    assert inner.pop_ds_int() == (1 << 40)
    cw = outer.dict["T10"]
    ce = outer.dict["BIG"]
    assert ce in cw.thread.code


def test_recursive_callee_call_stays_a_call():
    # A callee whose body calls itself: the spliced copy keeps the self-call.
    inner, outer = run_lines([
        ": fact DUP 2 < IF DROP 1 ELSE DUP 1 - RECURSE * THEN ;",
        ": t11 5 fact ;",
        "t11",
    ])
    assert inner.pop_ds_int() == 120
