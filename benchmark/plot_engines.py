"""Backward-compatible view of the engine registry.

The registry itself now lives in benchlib/engines.py. This module stays so that
dashboard/build_data.py and the plotting scripts keep importing the names they
always have.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from benchlib.engines import (  # noqa: E402,F401
    ENGINE_COLORS,
    PRIMARY_ENGINES,
    colors_for_configs,
    engine_color,
    engine_display_name,
    normalize_engine,
    sort_engines,
)

__all__ = [
    "ENGINE_COLORS",
    "PRIMARY_ENGINES",
    "colors_for_configs",
    "engine_color",
    "engine_display_name",
    "normalize_engine",
    "sort_engines",
]
