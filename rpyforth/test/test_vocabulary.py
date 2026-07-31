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


def test_vocabulary_defines_selector_word():
    inner, outer = run_lines([
        "vocabulary myv",
    ])
    assert "MYV" in outer.dict


def test_word_defined_in_vocab_found_when_selected():
    # gforth-verified: a word defined into a vocabulary is found once that vocabulary is on the search order.
    inner, outer = run_lines([
        "vocabulary myv",
        "myv definitions",
        ": inmyv 42 ;",
        "forth",
        "myv inmyv",
    ])
    assert inner.pop_ds_int() == 42


def test_vocabulary_brew_words_pattern():
    # brew.fs uses `VOCABULARY brew-words` unguarded, then later selects it.
    inner, outer = run_lines([
        "vocabulary brew-words",
        "brew-words definitions",
        ": marker-in-brew 7 ;",
        "forth",
        "brew-words marker-in-brew",
    ])
    assert inner.pop_ds_int() == 7


def test_vocab_select_keeps_forth_searchable():
    # gforth-verified: selecting a vocabulary must not remove FORTH-WORDLIST from the search order (brew's gene metaprogramming relies on core words remaining visible).
    inner, outer = run_lines([
        "vocabulary genes",
        "genes definitions",
        ": gtest 42 ;",
        "forth definitions",
        "genes gtest",   # select genes, find gtest
        "1+",            # a core word must still be found after selecting genes
    ])
    assert inner.pop_ds_int() == 43
