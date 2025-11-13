from rpyforth.objects import W_FloatObject, W_IntObject
from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter

def run(line):
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line(line)
    return inner

def run_and_pop(line):
    return run(line).pop_ds()

def test_s2f():
    """Test S>F (integer to float conversion)"""
    result = run_and_pop("10 S>F")
    assert isinstance(result, W_FloatObject)
    assert result.floatval == 10.0

def test_fstore_ffetch():
    """Test F! and F@"""
    result = run_and_pop("FVARIABLE X  3.14E0 X F!  X F@")
    assert isinstance(result, W_FloatObject)
    assert abs(result.floatval - 3.14) < 0.01

def test_fdup():
    """Test FDUP"""
    inner = run("5.5E0 FDUP")
    f1 = inner.pop_ds()
    f2 = inner.pop_ds()
    assert isinstance(f1, W_FloatObject)
    assert isinstance(f2, W_FloatObject)
    assert f1.floatval == f2.floatval == 5.5

def test_begin_while_repeat():
    """Test BEGIN...WHILE...REPEAT loop"""
    # Count from 0 to 4
    result = run_and_pop(": TEST 0 BEGIN DUP 5 < WHILE 1+ REPEAT ; TEST")
    assert isinstance(result, W_IntObject)
    assert result.intval == 5
