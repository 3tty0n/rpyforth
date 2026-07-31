"""Translation-time configuration for RPyForth.
"""

import os


DEFAULT_EXE_NAME = "rpyforth-%(backend)s"
DEFAULT_FRAME_SIZE = 8
MIN_FRAME_SIZE = 1
MAX_FRAME_SIZE = 64
DEFAULT_NTOP = 2

STACK_LAYOUT_PLAIN = "plain"
STACK_LAYOUT_FRAGMENT = "fragment"
STACK_LAYOUT_FRAME_ONLY = "frame-only"
STACK_LAYOUT_NTOP2 = "ntop2"
STACK_LAYOUT_NTOP4 = "ntop4"
STACK_LAYOUT_NTOP8 = "ntop8"
STACK_LAYOUT_NTOP16 = "ntop16"
STACK_LAYOUT_FRAGMENT_FLOAT = "fragment-float"


def _raw(name):
    return os.environ.get(name)


def _env_set(name):
    return bool(_raw(name))


def _uint_env(name):
    raw = _raw(name)
    if not raw:
        return -1
    value = 0
    for ch in raw:
        if not ("0" <= ch <= "9"):
            return -1
        value = value * 10 + (ord(ch) - ord("0"))
    return value


def _canonical_layout(raw):
    name = raw.strip().lower().replace("_", "-")
    if name in ("plain", "fixed", "array"):
        return STACK_LAYOUT_PLAIN
    if name in ("fragment", "stkfrag", "stack-fragment"):
        return STACK_LAYOUT_FRAGMENT
    if name in ("frame", "frame-only", "frameonly"):
        return STACK_LAYOUT_FRAME_ONLY
    if name == "ntop2":
        return STACK_LAYOUT_NTOP2
    if name == "ntop4":
        return STACK_LAYOUT_NTOP4
    if name == "ntop8":
        return STACK_LAYOUT_NTOP8
    if name == "ntop16":
        return STACK_LAYOUT_NTOP16
    if name in ("fragment-float", "float-fragment", "floatfrag"):
        return STACK_LAYOUT_FRAGMENT_FLOAT
    raise ValueError(
        "invalid RPYFORTH_STACK_LAYOUT %r; expected plain, fragment, "
        "frame-only, ntop2/4/8/16, or fragment-float" % raw)


def _legacy_stack_layout():
    if not _env_set("RPYFORTH_STACK_FRAGMENT"):
        return STACK_LAYOUT_PLAIN
    if _raw("RPYFORTH_FRAME_ONLY") == "1":
        return STACK_LAYOUT_FRAME_ONLY
    if _env_set("RPYFORTH_NTOP"):
        ntop = _uint_env("RPYFORTH_NTOP")
        if ntop == 4:
            return STACK_LAYOUT_NTOP4
        if ntop == 8:
            return STACK_LAYOUT_NTOP8
        if ntop == 16:
            return STACK_LAYOUT_NTOP16
        return STACK_LAYOUT_NTOP2
    if _raw("RPYFORTH_FLOAT_FRAGMENT") == "1":
        return STACK_LAYOUT_FRAGMENT_FLOAT
    return STACK_LAYOUT_FRAGMENT


def _resolve_stack_layout():
    raw = _raw("RPYFORTH_STACK_LAYOUT")
    if raw:
        return _canonical_layout(raw)
    return _legacy_stack_layout()


def _resolve_frame_size():
    value = _uint_env("RPYFORTH_FRAME_SIZE")
    if value < MIN_FRAME_SIZE:
        return DEFAULT_FRAME_SIZE
    if value > MAX_FRAME_SIZE:
        return MAX_FRAME_SIZE
    return value


def _resolve_effective_ntop(layout):
    if layout == STACK_LAYOUT_NTOP4:
        return 4
    if layout == STACK_LAYOUT_NTOP8:
        return 8
    if layout == STACK_LAYOUT_NTOP16:
        return 16
    return DEFAULT_NTOP


STACK_LAYOUT = _resolve_stack_layout()
FRAME_SIZE = _resolve_frame_size()
EFFECTIVE_NTOP = _resolve_effective_ntop(STACK_LAYOUT)

USE_STACK_FRAGMENT = STACK_LAYOUT != STACK_LAYOUT_PLAIN
USE_FRAME_ONLY = STACK_LAYOUT == STACK_LAYOUT_FRAME_ONLY
USE_NTOP_VARIANT = STACK_LAYOUT in (
    STACK_LAYOUT_NTOP2,
    STACK_LAYOUT_NTOP4,
    STACK_LAYOUT_NTOP8,
    STACK_LAYOUT_NTOP16,
)
USE_FLOAT_FRAGMENT = STACK_LAYOUT == STACK_LAYOUT_FRAGMENT_FLOAT

VIRTUALIZATION_REQUESTED = _env_set("RPYFORTH_VIRTUALIZE")
USE_VIRTUALIZATION = VIRTUALIZATION_REQUESTED and not USE_STACK_FRAGMENT
ALLOC_MB = _uint_env("RPYFORTH_ALLOC_MB")
EXE_NAME = _raw("RPYFORTH_EXE_NAME")
if EXE_NAME is None:
    EXE_NAME = DEFAULT_EXE_NAME


def format_config():
    """Return a compact description useful when checking a build setup."""
    alloc_mb = "auto"
    if ALLOC_MB > 0:
        alloc_mb = str(ALLOC_MB)
    return (
        "stack_layout=%s\n"
        "frame_size=%d\n"
        "effective_ntop=%d\n"
        "virtualize=%s\n"
        "alloc_mb=%s\n"
        "exe_name=%s"
        % (
            STACK_LAYOUT,
            FRAME_SIZE,
            EFFECTIVE_NTOP,
            "yes" if USE_VIRTUALIZATION else "no",
            alloc_mb,
            EXE_NAME,
        )
    )


if __name__ == "__main__":
    print(format_config())
