from rpyforth.metastack import FRAGMENT_SIZE, DSFragment, DSMetaStack


class DSObjFragment(DSFragment):
    _immutable_fields_ = ["parent", "cells"]

    def __init__(self, parent):
        self.parent = parent
        self.cells = [None] * FRAGMENT_SIZE
        self.sp = 0
        self.saved_parent_sp = 0
        self.import_count = 0

    def push_cell(self, v):
        raise NotImplementedError

    def pop_cell(self):
        raise NotImplementedError


class DSObjMetaStack(DSMetaStack):

    @classmethod
    def init_fields(cls, host):
        host.obj_top0 = None
        host.obj_top1 = None
        host.obj_top2 = None
        host.obj_top3 = None
        host.obj_top_count = 0
        host.ds_obj_current = DSObjFragment(None)
        host.ds_obj_sp = 0

    def __init__(self):
        self.init_fields(self)

    def push(self, v):
        self.push_on(self, v)

    def pop(self):
        return self.pop_on(self)

    def peek(self, depth):
        return self.peek_on(self, depth)

    def poke(self, depth, v):
        self.poke_on(self, depth, v)

    def size(self):
        return self.depth_on(self)

    def clear(self):
        self.reset_on(self)

    def push_fragment(self):
        self.push_fragment_on(self)

    @classmethod
    def push_on(cls, state, v):
        raise NotImplementedError

    @classmethod
    def pop_on(cls, state):
        raise NotImplementedError

    @classmethod
    def peek_on(cls, state, depth):
        raise NotImplementedError

    @classmethod
    def poke_on(cls, state, depth, v):
        raise NotImplementedError

    @classmethod
    def depth_on(cls, state):
        raise NotImplementedError

    @classmethod
    def reset_on(cls, state):
        raise NotImplementedError

    @classmethod
    def push_fragment_on(cls, state):
        raise NotImplementedError
