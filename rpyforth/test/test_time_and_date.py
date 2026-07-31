import time as pytime

from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter
from rpyforth.objects import LONG_BIT
from rpyforth.primitives import prim_CPUTIME


def _make_interp():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    return inner, outer


def _sample_cputime_usecs(inner):
    prim_CPUTIME(inner, None, 0)
    inner.pop_ds_int()
    inner.pop_ds_int()
    usr_high = inner.pop_ds_int()
    usr_low  = inner.pop_ds_int()
    return usr_low | (usr_high << LONG_BIT)


def test_cputime_resolution_detects_sub_millisecond_advance():
    inner, _ = _make_interp()

    for _ in range(3):
        _sample_cputime_usecs(inner)

    before_usecs = _sample_cputime_usecs(inner)
    after_usecs  = _sample_cputime_usecs(inner)

    delta = after_usecs - before_usecs

    assert delta > 0, (
        "CPUTIME has too coarse granularity (os.times 10ms ticks?): "
        "two consecutive samples gave delta=%d usecs; rtime.clock() "
        "returns ~500 usecs for this call overhead" % delta
    )


def test_time_and_date_matches_utc_clock():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    before = pytime.gmtime()
    outer.interpret_line("TIME&DATE")
    after = pytime.gmtime()
    year = inner.pop_ds_int()
    month = inner.pop_ds_int()
    day = inner.pop_ds_int()
    hour = inner.pop_ds_int()
    minute = inner.pop_ds_int()
    sec = inner.pop_ds_int()
    assert 0 <= sec <= 60
    assert 0 <= minute <= 59
    assert 0 <= hour <= 23
    assert (year, month, day) in (
        (before.tm_year, before.tm_mon, before.tm_mday),
        (after.tm_year, after.tm_mon, after.tm_mday),
    )
    assert (hour, minute) in (
        (before.tm_hour, before.tm_min),
        (after.tm_hour, after.tm_min),
    )
