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


def test_evaluate_of_char_memory_string():
    # EVALUATE must work on plain bytes in char memory, not only boxed S" buffers (brew assembles text and EVALUATEs it).
    inner, outer = run_lines([])
    text = "3 4 +"
    addr = inner.here
    for ch in text:
        inner.char_store(inner.here, ord(ch))
        inner.here += 1
    inner.push_ds_int(addr)
    inner.push_ds_int(len(text))
    outer.interpret_line("evaluate")
    assert inner.pop_ds_int() == 7


def test_evaluate_of_s_quote_buffer_still_works():
    inner, outer = run_lines([
        's" 10 20 +" evaluate',
    ])
    assert inner.pop_ds_int() == 30
