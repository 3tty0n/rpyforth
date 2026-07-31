from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def run_lines(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_self_call_sites_are_inlined():
    # Non-tail recursive site: the finalized thread must contain a spliced copy of the body (roughly 2x plain).
    inner, outer = run_lines([": g dup 1 > if dup 1- recurse 1+ then ;"])
    w = outer.dict["G"]
    assert len(w.thread.code) > 14


def test_inlined_fibo_is_correct():
    inner, outer = run_lines([
        ": fibo dup 2 < if drop 1 else dup 1- recurse swap 2 - recurse + then ;",
        "10 fibo",
        "20 fibo",
    ])
    assert inner.pop_ds_int() == 10946
    assert inner.pop_ds_int() == 89


def test_inlined_recursion_with_loop_body():
    # DO..LOOP back-branch targets inside the copied body must be remapped on inline.
    inner, outer = run_lines([
        ": r2 dup 0 > if dup 3 0 do 1+ loop swap 1- recurse + else drop 0 then ;",
        "3 r2",
    ])
    assert inner.pop_ds_int() == 15


def test_inlined_early_exit_in_branch():
    # EXIT inside the copied body must leave only the inlined call, not the enclosing word.
    inner, outer = run_lines([
        ": s dup 0 = if exit then dup 1- recurse + ;",
        "4 s 100 +",
    ])
    assert inner.pop_ds_int() == 110  # 4+3+2+1+0 = 10, plus 100


def test_tail_recursive_word_still_works():
    inner, outer = run_lines([
        ": cnt dup 0 > if 1- recurse then ;",
        "100000 cnt",
    ])
    assert inner.pop_ds_int() == 0


def test_inlined_tak_four_sites_by_name():
    # Four self-call sites via RECURSIVE (not RECURSE), matching the shape of the tak benchmark.
    inner, outer = run_lines([
        ": tak recursive 2 pick 2 pick > if "
        "2 pick 1- 2 pick 2 pick tak "
        "2 pick 1- 2 pick 5 pick tak "
        "2 pick 1- 5 pick 5 pick tak "
        "tak nip nip nip else nip nip then ;",
        "18 12 6 tak",
    ])
    assert inner.pop_ds_int() == 7


def test_deep_inlined_recursion_matches_plain():
    inner, outer = run_lines([
        ": tri dup 0 > if dup 1- recurse + then ;",
        "300 tri",
    ])
    assert inner.pop_ds_int() == 300 * 301 // 2
