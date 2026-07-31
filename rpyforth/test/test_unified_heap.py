from rpyforth.heap import Heap, HEAP_SIZE_BYTES
from rpyforth.objects import CELL_SIZE_BYTES


def test_cell_size_is_eight_bytes():
    assert CELL_SIZE_BYTES == 8


def test_char_and_cell_share_storage_little_endian():
    h = Heap(1024)
    h.char_store(16, 0x34)
    h.char_store(17, 0x12)
    assert h.cell_fetch_int(16) == 0x1234


def test_cell_store_visible_through_char_fetch():
    h = Heap(1024)
    h.cell_store(8, 0x0102030405060708)
    assert h.char_fetch(8) == 0x08
    assert h.char_fetch(15) == 0x01


def test_high_byte_roundtrip():
    h = Heap(1024)
    h.char_store(7, 0xAB)
    assert h.char_fetch(7) == 0xAB
    h.char_store(7, 0x00)
    assert h.cell_fetch_int(0) == 0


def test_negative_cell_roundtrip():
    h = Heap(1024)
    h.cell_store(0, -1)
    assert h.cell_fetch_int(0) == -1
    assert h.char_fetch(3) == 0xFF


def test_unaligned_cell_roundtrip():
    h = Heap(1024)
    h.cell_store(3, 0x1122334455667788)
    assert h.cell_fetch_int(3) == 0x1122334455667788
    assert h.char_fetch(3) == 0x88


def test_adjacent_cells_do_not_collide():
    h = Heap(1024)
    h.cell_store(0, 111)
    h.cell_store(8, 222)
    h.cell_store(16, 333)
    assert h.cell_fetch_int(0) == 111
    assert h.cell_fetch_int(8) == 222
    assert h.cell_fetch_int(16) == 333


def test_fill_bytes_and_cell_view():
    h = Heap(1024)
    h.fill_bytes(8, 16, 0x7F)
    assert h.char_fetch(8) == 0x7F
    assert h.char_fetch(23) == 0x7F
    assert h.char_fetch(24) == 0
    assert h.cell_fetch_int(8) == 0x7F7F7F7F7F7F7F7F


def test_fill_bytes_unaligned_range():
    h = Heap(1024)
    h.fill_bytes(5, 7, 0x01)
    for a in range(5, 12):
        assert h.char_fetch(a) == 1
    assert h.char_fetch(4) == 0
    assert h.char_fetch(12) == 0


def test_move_bytes_forward_and_backward_overlap():
    h = Heap(1024)
    for k in range(8):
        h.char_store(32 + k, k + 1)
    h.move_bytes(32, 34, 8)          # overlapping, dst > src
    assert [h.char_fetch(34 + k) for k in range(8)] == [1, 2, 3, 4, 5, 6, 7, 8]
    h.move_bytes(34, 33, 8)          # overlapping, dst < src
    assert [h.char_fetch(33 + k) for k in range(8)] == [1, 2, 3, 4, 5, 6, 7, 8]


def test_unwritten_memory_reads_zero():
    h = Heap(1024)
    assert h.cell_fetch_int(64) == 0
    assert h.char_fetch(999) == 0
    assert h.cell_fetch(64).getvalue() == 0


def test_float_mem_indexed_by_cell():
    h = Heap(1024)
    h.float_store(16, 2.5)
    assert h.float_fetch_float(16) == 2.5
    assert h.float_fetch_float(24) == 0.0


def test_heap_size_covers_large_programs():
    # cd16sim allots ~48k cells of program/data memory.
    assert HEAP_SIZE_BYTES >= (1 << 20)
