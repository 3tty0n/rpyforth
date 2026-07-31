"""get_printable_location must be total: the JIT logger calls it for any
(ip, thread) a merge point or guard descr ever saw, including the thread-end
ip (== len(code)), the empty HALT thread, and slots holding None. Translated
builds have no bounds checks, so an out-of-range index or None dereference
here segfaults the whole VM as soon as PYPYLOG jit logging is enabled."""

from rpyforth.inner_interp import get_printable_location, HALT_THREAD
from rpyforth.objects import CodeThread, Word, W_IntObject


def _thread():
    w = Word("W")
    return CodeThread([w, None], [W_IntObject(7), None])


def test_normal_slot():
    t = _thread()
    s = get_printable_location(0, t)
    assert "ip=0" in s


def test_ip_at_end_of_thread():
    t = _thread()
    s = get_printable_location(len(t.code), t)
    assert "ip=2" in s


def test_empty_halt_thread():
    s = get_printable_location(0, HALT_THREAD)
    assert "ip=0" in s


def test_none_word_and_lit_slot():
    t = _thread()
    s = get_printable_location(1, t)
    assert "ip=1" in s


def test_negative_ip():
    t = _thread()
    s = get_printable_location(-1, t)
    assert "ip=-1" in s
