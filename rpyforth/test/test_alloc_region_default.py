import os
import subprocess
import sys

from rpyforth.heap import DICT_SIZE_BYTES, _default_alloc_mb
from rpyforth.outer_interp import OuterInterpreter
from rpyforth.inner_interp import InnerInterpreter


def alloc_region_bytes_with_env(value):
    env = dict(os.environ)
    env.pop("RPYFORTH_ALLOC_MB", None)
    if value is not None:
        env["RPYFORTH_ALLOC_MB"] = value
    env["PYTHONPATH"] = os.pathsep.join([p for p in sys.path if p])
    code = "from rpyforth.heap import _alloc_region_bytes; print(_alloc_region_bytes())"
    out = subprocess.check_output(
        [sys.executable, "-c", code],
        env=env,
        cwd=os.getcwd(),
        stderr=subprocess.STDOUT,
    )
    return int(out.strip())


def test_env_override_wins():
    assert alloc_region_bytes_with_env("64") == 64 << 20


def test_untranslated_default_is_small():
    # Untranslated buffer creation is O(size), so the suite default must stay at 1 MB.
    assert alloc_region_bytes_with_env(None) == 1 << 20


def test_invalid_override_preserves_default_behavior():
    assert alloc_region_bytes_with_env("invalid") == 1 << 20


def test_env_read_at_runtime_not_import_time():
    from rpyforth.heap import _alloc_region_bytes
    saved = os.environ.get("RPYFORTH_ALLOC_MB")
    try:
        os.environ["RPYFORTH_ALLOC_MB"] = "3"
        assert _alloc_region_bytes() == 3 << 20
        os.environ["RPYFORTH_ALLOC_MB"] = "5"
        assert _alloc_region_bytes() == 5 << 20
    finally:
        if saved is None:
            os.environ.pop("RPYFORTH_ALLOC_MB", None)
        else:
            os.environ["RPYFORTH_ALLOC_MB"] = saved


def test_translated_default_is_generous():
    # brainless ttable.fs allocates several MB; lazily-mapped calloc pages mean a generous default costs nothing.
    assert _default_alloc_mb(translated=True) >= 64
    assert _default_alloc_mb(translated=False) == 1


def test_unused_reports_dictionary_space():
    inner = InnerInterpreter()
    outer = OuterInterpreter(inner)
    outer.interpret_line("UNUSED")
    unused = inner.pop_ds_int()
    assert 0 < unused <= DICT_SIZE_BYTES
