from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def make():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    return inner, outer


def run(line):
    inner, outer = make()
    outer.interpret_line(line)
    return inner


def test_only_forth_also_definitions_is_noop_for_core():
    # The cd16sim idiom must not break ordinary FORTH words.
    inner = run("ONLY FORTH ALSO DEFINITIONS  2 3 +")
    assert inner.pop_ds_int() == 5


def test_words_defined_after_setup_are_found():
    inner, outer = make()
    outer.interpret_line("ONLY FORTH ALSO DEFINITIONS")
    outer.interpret_line(": SQ DUP * ;")
    outer.interpret_line("5 SQ")
    assert inner.pop_ds_int() == 25


def test_wordlist_and_search_wordlist():
    inner, outer = make()
    outer.interpret_line("WORDLIST CONSTANT MY-WL")
    outer.interpret_line("GET-CURRENT  MY-WL SET-CURRENT")
    outer.interpret_line(": PRIV 111 ;")
    outer.interpret_line("SET-CURRENT")
    # PRIV is not in the FORTH search order; must be found only via explicit SEARCH-WORDLIST on MY-WL.
    outer.interpret_line('S" PRIV" MY-WL SEARCH-WORDLIST')
    found = inner.pop_ds_int()
    assert found != 0     # 1 or -1: found, xt beneath
    inner.pop_ds_int()    # drop xt (an integer wid)


def test_search_wordlist_from_char_memory():
    # shootout spellcheck/wordfreq: READ-LINE fills a CREATE buffer; SEARCH-WORDLIST must honour (c-addr u) against raw char memory, not only S" W_StringObject.
    inner, outer = make()
    outer.interpret_line("WORDLIST CONSTANT MY-WL")
    outer.interpret_line("GET-CURRENT MY-WL SET-CURRENT")
    outer.interpret_line('S" HELLO" NEXTNAME CREATE')
    outer.interpret_line("SET-CURRENT")
    outer.interpret_line("CREATE BUF 32 ALLOT")
    outer.interpret_line('S" hello" BUF SWAP MOVE')
    outer.interpret_line("BUF 5 MY-WL SEARCH-WORDLIST")
    found = inner.pop_ds_int()
    assert found != 0
    inner.pop_ds_int()


def test_also_previous_restore_order():
    inner, outer = make()
    outer.interpret_line("WORDLIST CONSTANT W2")
    outer.interpret_line("W2 SET-CURRENT  : HIDDEN 999 ;  FORTH-WORDLIST SET-CURRENT")
    # put W2 on top of the order, use its word, then PREVIOUS to drop it
    outer.interpret_line("W2 >ORDER")
    outer.interpret_line("HIDDEN")
    assert inner.pop_ds_int() == 999
    outer.interpret_line("PREVIOUS")
    # after PREVIOUS, HIDDEN is no longer reachable; a core word must still work.
    outer.interpret_line("7 8 +")
    assert inner.pop_ds_int() == 15


def test_get_order_set_order_roundtrip():
    inner, outer = make()
    outer.interpret_line("GET-ORDER")
    n = inner.pop_ds_int()
    assert n >= 1
    for _ in range(n):
        inner.pop_ds_int()


# benchgc coverage: WORDLIST/GET-ORDER/SET-ORDER must work when compiled into a colon body (compat/vocabulary.fs uses this pattern).
def test_wordlist_compiles_in_definition():
    inner = run(": mkwl wordlist ; mkwl")
    assert inner.pop_ds_int() >= 1  # 0 is FORTH-WORDLIST


def test_set_order_negative_restores_default():
    inner = run("-1 set-order get-order")
    n = inner.pop_ds_int()
    assert n == 1
    assert inner.pop_ds_int() == 0


def test_tailcall_word_defined_in_other_wordlist():
    # Regression for gc.fs: tail-call optimisation looks up TAILCALL in FORTH-WORDLIST, so words in other wordlists must not lose their final colon-call.
    inner, outer = make()
    outer.interpret_line(": vocabulary wordlist create , "
                         "does> @ >r get-order dup 0= -50 and throw nip r> swap set-order ; ")
    outer.interpret_line("vocabulary v3")
    outer.interpret_line("also v3 definitions")
    outer.interpret_line(": callee ( n -- n ) dup if 1+ else 1- then ;")
    outer.interpret_line(": caller ( n -- n ) 10 + callee ;")  # ends in a colon-call
    outer.interpret_line("previous definitions")
    outer.interpret_line("also v3  5 caller  previous")
    # 5 -> +10 = 15 -> callee: 15 true -> 1+ = 16
    assert inner.pop_ds_int() == 16


def test_vocabulary_defined_in_forth():
    # Exactly compat/vocabulary.fs: wordlist/get-order/set-order and also/definitions/previous cooperate for a Forth-defined vocabulary.
    src = (
        ": vocabulary wordlist create , "
        "does> @ >r get-order dup 0= -50 and throw nip r> swap set-order ; "
        "vocabulary myvoc "
        "also myvoc definitions : secret 4242 ; previous definitions "
        "also myvoc secret previous"
    )
    inner = run(src)
    assert inner.pop_ds_int() == 4242
