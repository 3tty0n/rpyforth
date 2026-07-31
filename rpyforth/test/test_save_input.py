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


def test_save_restore_round_trips_false():
    # SAVE-INPUT then RESTORE-INPUT succeeds (false flag), consuming what it saved.
    inner = run([": t  SAVE-INPUT RESTORE-INPUT ;", "t"])
    assert inner.pop_ds_int() == 0
    assert inner.ds_int_size() == 0


def test_restore_input_rewinds_parse_cursor():
    # Mirror brainless option-exists?: after SAVE-INPUT, parse+lookup a name, then RESTORE-INPUT rewinds so the same name can be parsed again. Here we parse a following token twice and confirm both reads see it.
    inner = run([
        ": grab  BL WORD COUNT NIP ;",           # parses next token, returns len
        ": twice  SAVE-INPUT grab >R RESTORE-INPUT DROP R> DROP grab ;",
        "twice hello",
    ])
    assert inner.pop_ds_int() == 5


def test_option_exists_idiom():
    # The full brainless option-exists? idiom over a not-yet-defined name.
    inner = run([
        ": option-exists?  ( \"name\" -- \"name\" flag )",
        "   SAVE-INPUT  BL WORD FIND NIP",
        "   >R RESTORE-INPUT ABORT\" RESTORE-INPUT failed!\" R> ;",
        ": create-option  ( x \"name\" -- )  CREATE ,  DOES> @ ;",
        ": option  ( x \"name\" -- )  option-exists? 0= IF create-option THEN ;",
        "7 option speed",
        "speed",
    ])
    assert inner.pop_ds_int() == 7
