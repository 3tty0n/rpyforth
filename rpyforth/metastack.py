"""Metastack constants and the three-tier data-stack layout:

  * the top NTOP cells in scalar fields (stack caching, Ertl 1995),
  * the next FRAME_SIZE cells in a small virtualizable frame array,
  * everything deeper in the shared spill area.
"""

from rpyforth.config import (
    DEFAULT_NTOP,
    STACK_LAYOUT,
    STACK_LAYOUT_PLAIN,
    STACK_LAYOUT_FRAGMENT,
    STACK_LAYOUT_FRAME_ONLY,
    STACK_LAYOUT_NTOP2,
    STACK_LAYOUT_NTOP4,
    STACK_LAYOUT_NTOP8,
    STACK_LAYOUT_NTOP16,
    STACK_LAYOUT_FRAGMENT_FLOAT,
    FRAME_SIZE,
    EFFECTIVE_NTOP,
    USE_STACK_FRAGMENT,
    USE_FRAME_ONLY,
    USE_NTOP_VARIANT,
    USE_FLOAT_FRAGMENT,
)
from rpyforth.vm_constants import FRAG_DEPTH, SPILL_SIZE

NTOP = DEFAULT_NTOP
ACTIVE_MAX = NTOP + FRAME_SIZE

CALL_WINDOW = NTOP

FRAGMENT_SIZE = 256
TOP_CACHE_SIZE = 4
STACK_SIZE = FRAGMENT_SIZE

class DataStackOverflow(Exception):
    pass


class DSFragment(object):
    pass


class DSMetaStack(object):
    pass

if USE_FRAME_ONLY:
    _INT_CACHE_VIRTUALIZABLES = ["cache_depth", "frame[*]", "spill_ptr"]
elif USE_NTOP_VARIANT:
    _INT_CACHE_VIRTUALIZABLES = (["t%d" % i for i in range(EFFECTIVE_NTOP)] +
                                 ["cache_depth", "frame[*]", "spill_ptr"])
else:
    _INT_CACHE_VIRTUALIZABLES = ["t0", "t1", "cache_depth", "frame[*]",
                                 "spill_ptr"]

_STACK_FRAGMENT_VIRTUALIZABLES_BASE = _INT_CACHE_VIRTUALIZABLES + [
    "rs_ptr",
    "cs_pcs", "cs_ptr", "cs_base",
    "li",
    "cell_size", "cell_size_bytes", "base",
    "ds_ptr_locals",
]

if USE_FLOAT_FRAGMENT:
    STACK_FRAGMENT_VIRTUALIZABLES = _STACK_FRAGMENT_VIRTUALIZABLES_BASE + [
        "ft0", "ft1", "fdep", "fframe[*]",
        "fspill_ptr",
    ]
else:
    STACK_FRAGMENT_VIRTUALIZABLES = _STACK_FRAGMENT_VIRTUALIZABLES_BASE + [
        "ds_floats", "ds_ptr_floats",
    ]


def push_ds_fragments(state):
    if USE_STACK_FRAGMENT:
        state.push_fragment_on()
        if USE_FLOAT_FRAGMENT:
            state.push_float_fragment_on()


def reset_ds_fragments(state):
    if USE_STACK_FRAGMENT:
        state.reset_on()
        if USE_FLOAT_FRAGMENT:
            state.freset_on()
