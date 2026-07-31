from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.prelude import load_prelude


def run(lines):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    load_prelude(outer)
    for line in lines:
        outer.interpret_line(line)
    return inner, outer


def test_source_compiles_into_colon_body():
    # SOURCE must be usable inside a colon definition (lexex parse-name), not just in interpret mode.
    inner, _ = run([
        ": linelen  SOURCE nip ;",
        "linelen",
    ])
    assert inner.pop_ds_int() == 7


def test_compile_comma_appends_xt():
    # COMPILE, ( xt -- ) appends an xt from an immediate metaprogramming word (lexex xmini_oof ::).
    inner, _ = run([
        ": add3  3 + ;",
        ": plus3  ['] add3 compile, ; IMMEDIATE",
        ": t  10 plus3 ;",
        "t",
    ])
    assert inner.pop_ds_int() == 13


def test_does_body_with_if_then_branch_targets():
    # DOES> body with IF/ELSE/THEN must have branch targets rebased when carved out (lexex userinterface regexp).
    inner, _ = run([
        ": mk  create , 0 , does>  dup @ swap cell+ dup @"
        "      if drop 1+ else -1 swap ! then ;",
        "42 mk foo",
        "foo",   # first use: else branch -> 42
    ])
    assert inner.ds_int_size() == 1
    assert inner.pop_ds_int() == 42
    inner2, _ = run([
        ": mk  create , 0 , does>  dup @ swap cell+ dup @"
        "      if drop 1+ else -1 swap ! then ;",
        "42 mk foo",
        "foo foo",   # first -> 42 (discarded by 2nd), second use: if branch -> 43
    ])
    assert inner2.pop_ds_int() == 43
    assert inner2.pop_ds_int() == 42


def test_char_parses_at_runtime_in_colon_body():
    # CHAR inside a colon definition must parse its argument at RUNTIME, not compile time (lexex 'char' = char 'lit').
    inner, _ = run([
        ": ch  char ;",
        "ch A",
    ])
    assert inner.pop_ds_int() == ord("A")
    inner2, _ = run([
        ": two  char char ;",
        "two x y",
    ])
    assert inner2.pop_ds_int() == ord("y")
    assert inner2.pop_ds_int() == ord("x")
    assert inner2.ds_int_size() == 0


def test_runtime_noname_sliteral_semicolon():
    # lexex setOutputFile: a word builds a :NONAME at runtime using POSTPONE SLITERAL / POSTPONE ;.
    inner, _ = run([
        "variable outputFile",
        ": setOut  ( c-addr u -- )",
        "   2>r :noname 2r> postpone sliteral postpone ; outputFile ! ;",
        's" hi.fth" setOut',
        "outputFile @ execute",
    ])
    n = inner.pop_ds_int()
    c_addr = inner.pop_ds_int()
    assert n == 6
    chars = "".join(chr(inner.char_fetch(c_addr + k)) for k in range(n))
    assert chars == "hi.fth"


def test_search_wordlist_in_colon_body():
    # GET-CURRENT / SEARCH-WORDLIST must work compiled into a colon body (lexex lexarrays updateArrayName).
    inner, _ = run([
        ": present?  ( c-addr u -- f )  get-current search-wordlist 0<> ;",
        's" DUP" present?',
    ])
    assert inner.pop_ds_int() != 0
    inner2, _ = run([
        ": present?  ( c-addr u -- f )  get-current search-wordlist 0<> ;",
        's" NOSUCHWORDXYZ" present?',
    ])
    assert inner2.pop_ds_int() == 0


def test_compare_reads_char_memory():
    # COMPARE must work on ALLOTted byte buffers (lexex compare-files), not just boxed S" strings.
    from rpyforth.outer_interp import OuterInterpreter
    from rpyforth.inner_interp import InnerInterpreter
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("create b1 8 allot  create b2 8 allot")
    outer.interpret_line("65 b1 c! 66 b1 1+ c! 67 b1 2 + c!")   # ABC
    outer.interpret_line("65 b2 c! 66 b2 1+ c! 68 b2 2 + c!")   # ABD
    outer.interpret_line("b1 3 b2 3 compare")
    assert inner.pop_ds_int() == -1
    outer.interpret_line("b1 3 b1 3 compare")
    assert inner.pop_ds_int() == 0
    outer.interpret_line("b1 2 b1 3 compare")
    assert inner.pop_ds_int() == -1
    outer.interpret_line("b1 3 b1 2 compare")
    assert inner.pop_ds_int() == 1
