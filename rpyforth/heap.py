from rpython.rtyper.lltypesystem import lltype, rffi
from rpython.rlib.rarithmetic import intmask
from rpython.rlib.objectmodel import we_are_translated

from rpyforth.objects import W_FloatObject, make_int

import os

DICT_SIZE_BYTES = 1 << 23
ALLOC_BASE = DICT_SIZE_BYTES
HEAP_SIZE_BYTES = DICT_SIZE_BYTES + (1 << 20)
HEAP_CELL_COUNT = HEAP_SIZE_BYTES >> 3

CELL_BYTES = 8

def _alloc_region_bytes():
    raw = os.environ.get("RPYFORTH_ALLOC_MB")
    mb = 0
    if raw is not None and raw != "":
        n = 0
        ok = True
        for ch in raw:
            if "0" <= ch <= "9":
                n = n * 10 + (ord(ch) - ord("0"))
            else:
                ok = False
                break
        if ok:
            mb = n
    if mb <= 0:
        mb = _default_alloc_mb(we_are_translated())
    return mb << 20


def _default_alloc_mb(translated):
    if translated:
        return 64
    return 1


class Heap(object):
    _immutable_fields_ = ["size", "raw"]

    def __init__(self, size):
        assert size & 7 == 0
        self.size = size
        self.raw = lltype.malloc(rffi.CCHARP.TO, size, flavor='raw', zero=True)

    def __del__(self):
        lltype.free(self.raw, flavor='raw')

    def _ensure_addr(self, addr, span):
        assert 0 <= addr
        assert addr + span <= self.size

    def cell_store(self, addr, intval):
        self._ensure_addr(addr, CELL_BYTES)
        p = rffi.cast(rffi.SIGNEDP, rffi.ptradd(self.raw, addr))
        p[0] = rffi.cast(rffi.SIGNED, intval)

    def cell_fetch_int(self, addr):
        self._ensure_addr(addr, CELL_BYTES)
        p = rffi.cast(rffi.SIGNEDP, rffi.ptradd(self.raw, addr))
        return intmask(p[0])

    def cell_fetch(self, addr):
        return make_int(self.cell_fetch_int(addr))

    def char_store(self, addr, intval):
        self._ensure_addr(addr, 1)
        self.raw[addr] = chr(intval & 0xFF)

    def char_fetch(self, addr):
        self._ensure_addr(addr, 1)
        return ord(self.raw[addr])

    def fill_bytes(self, addr, count, b):
        if count <= 0:
            return
        self._ensure_addr(addr, count)
        c = chr(b & 0xFF)
        i = 0
        while i < count:
            self.raw[addr + i] = c
            i += 1

    def move_bytes(self, src, dst, count):
        """memmove semantics: overlap-safe byte copy."""
        if count <= 0 or src == dst:
            return
        self._ensure_addr(src, count)
        self._ensure_addr(dst, count)
        if dst < src:
            i = 0
            while i < count:
                self.raw[dst + i] = self.raw[src + i]
                i += 1
        else:
            i = count - 1
            while i >= 0:
                self.raw[dst + i] = self.raw[src + i]
                i -= 1

    def float_store(self, addr, value):
        self._ensure_addr(addr, CELL_BYTES)
        p = rffi.cast(rffi.DOUBLEP, rffi.ptradd(self.raw, addr))
        p[0] = value

    def float_fetch_float(self, addr):
        self._ensure_addr(addr, CELL_BYTES)
        p = rffi.cast(rffi.DOUBLEP, rffi.ptradd(self.raw, addr))
        return p[0]

    def float_fetch(self, addr):
        return W_FloatObject(self.float_fetch_float(addr))
