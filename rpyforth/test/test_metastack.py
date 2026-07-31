import pytest

from rpyforth.metastack import (
    push_ds_fragments,
    reset_ds_fragments,
)
from rpyforth.metastack_float import DSFloatMetaStack
from rpyforth.metastack_obj import DSObjMetaStack
from rpyforth.inner_interp import InnerInterpreter, USE_STACK_FRAGMENT


class _Host(object):
    pass


def test_float_metastack_push_pop():
    s = DSFloatMetaStack()
    s.fpush(1.0)
    s.fpush(2.0)
    assert s.fsize() == 2
    assert s.fpop() == 2.0
    assert s.fpop() == 1.0


def test_obj_metastack_not_implemented():
    s = DSObjMetaStack()
    with pytest.raises(NotImplementedError):
        s.push(None)
    with pytest.raises(NotImplementedError):
        DSObjMetaStack.push_fragment_on(s)


def test_float_init_fields():
    from rpyforth.metastack_float import init_float_fields
    host = _Host()
    init_float_fields(host)
    assert host.fspill_ptr == 0
    assert host.fdep == 0


def test_obj_init_fields():
    host = _Host()
    DSObjMetaStack.init_fields(host)
    assert host.ds_obj_sp == 0
    assert host.obj_top_count == 0


def test_inner_int_stack_fixed_path():
    inner = InnerInterpreter()
    if USE_STACK_FRAGMENT:
        pytest.skip("requires RPYFORTH_STACK_LAYOUT=plain")
    n = 100
    for v in range(n):
        inner.push_ds_int(v)
    assert inner.depth_ds_int() == n
    assert inner.peek_ds_int(0) == n - 1
    out = [inner.pop_ds_int() for _ in range(n)]
    assert out[0] == n - 1


def test_inner_depth_and_reset_fixed():
    inner = InnerInterpreter()
    if USE_STACK_FRAGMENT:
        pytest.skip("requires fixed-stack build")
    inner.push_ds_int(1)
    inner.push_ds_int(2)
    assert inner.depth_ds_int() == 2
    inner.reset_ds_int()
    assert inner.depth_ds_int() == 0


def test_coordination_noop_without_fragment_flag():
    inner = InnerInterpreter()
    if USE_STACK_FRAGMENT:
        pytest.skip("requires fixed-stack build")
    push_ds_fragments(inner)
    reset_ds_fragments(inner)
