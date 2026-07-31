"""The single engine registry: ids, binaries, argv, colors, labels, ordering.

Anything that names an engine — a harness building a command line, a report
picking a legend color, a log reader resolving a binary path back to an id —
goes through here, so the four-way comparison means the same four things
everywhere.
"""

from __future__ import annotations

from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence

from .paths import APPBENCH_DIR, GFORTH_DIR, REPO_ROOT

ENGINE_GFORTH = "gforth"
ENGINE_GFORTH_FAST = "gforth-fast"
ENGINE_RPYFORTH = "rpyforth"
ENGINE_VFXFORTH = "vfxforth"
ENGINE_SWIFTFORTH = "swiftforth"

# The paper's 4-way comparison, in legend / left-to-right order. Plain gforth
# is a known engine but is not part of the headline figure.
PRIMARY_ENGINES = (
    ENGINE_RPYFORTH,
    ENGINE_GFORTH_FAST,
    ENGINE_VFXFORTH,
    ENGINE_SWIFTFORTH,
)
ENGINES = list(PRIMARY_ENGINES)
ALL_ENGINES = list(PRIMARY_ENGINES) + [ENGINE_GFORTH]

# Everything rpyforth is measured against.
REFERENCE_ENGINE = ENGINE_GFORTH_FAST
BASELINE_ENGINES = (ENGINE_GFORTH_FAST, ENGINE_VFXFORTH, ENGINE_SWIFTFORTH)
AOT_ENGINES = (ENGINE_VFXFORTH, ENGINE_SWIFTFORTH)

ENGINE_BINARY = {
    ENGINE_RPYFORTH: REPO_ROOT / "rpyforth-c-stkfrag",
    ENGINE_GFORTH_FAST: GFORTH_DIR / "gforth-fast",
    ENGINE_GFORTH: GFORTH_DIR / "gforth",
    ENGINE_VFXFORTH: REPO_ROOT / "vfxforth.sh",
    ENGINE_SWIFTFORTH: REPO_ROOT / "swiftforth.sh",
}

# Engines that take a bare `<binary> <driver.fs>` command line. gforth needs
# `-m <mem>` and the appbench setup shim in front of the driver.
DIRECT_ARGV_ENGINES = (ENGINE_RPYFORTH, ENGINE_VFXFORTH, ENGINE_SWIFTFORTH)

# gforth dataspace (`-m`), overridable per program. Sized for the hungriest
# workload rather than per suite: lexex's per-iteration table build allots
# ~3.6 MB and needs a roomy dictionary, and a gforth that runs out of dataspace
# in one harness but not another is not the same baseline.
GFORTH_MEM = "256M"

# Per-engine setup shims, mirroring appbench-1.4/run's `include ../setup/<engine>`.
ENGINE_SETUP_INCLUDE = {
    ENGINE_VFXFORTH: APPBENCH_DIR / "setup" / "vfx.fth",
    ENGINE_SWIFTFORTH: APPBENCH_DIR / "setup" / "sf.f",
}

ENGINE_COLORS = {
    ENGINE_RPYFORTH: "#d62728",
    "rpyforth-c-stkfrag": "#d62728",
    "stkfrag": "#d62728",
    "rpyforth-c": "#8c564b",
    "contiguous": "#8c564b",
    "rpyforth-c-novirt": "#e377c2",
    "novirt": "#e377c2",
    ENGINE_GFORTH_FAST: "#1f77b4",
    ENGINE_GFORTH: "#2ca02c",
    ENGINE_VFXFORTH: "#9467bd",
    ENGINE_SWIFTFORTH: "#ff7f0e",
}

# Colors for ablation ladder steps and other non-engine series. Deliberately
# disjoint from ENGINE_COLORS so an ablation variant is never drawn in
# swiftforth's or vfxforth's color.
EXTRA_COLORS = ["#4c72b0", "#55a868", "#c44e52", "#8172b2", "#937860"]

# Used when an engine id is not in the table at all.
_FALLBACK_PALETTE = ["#8c564b", "#e377c2", "#7f7f7f", "#bcbd22", "#17becf"]

_ALIASES = {
    "rpyforth": ENGINE_RPYFORTH,
    "rpyforth-c": ENGINE_RPYFORTH,
    "rpyforth-c-stkfrag": ENGINE_RPYFORTH,
    "rpyforth-c-novirt": ENGINE_RPYFORTH,
    "stkfrag": ENGINE_RPYFORTH,
    "contiguous": ENGINE_RPYFORTH,
    "novirt": ENGINE_RPYFORTH,
    "gforth-fast": ENGINE_GFORTH_FAST,
    "gforth": ENGINE_GFORTH,
    "vfxforth": ENGINE_VFXFORTH,
    "vfx": ENGINE_VFXFORTH,
    "swiftforth": ENGINE_SWIFTFORTH,
    "swift": ENGINE_SWIFTFORTH,
    "sf": ENGINE_SWIFTFORTH,
    "sf64": ENGINE_SWIFTFORTH,
}

# rpyforth build variants keep their own label so ablation charts can tell them
# apart; normalize_engine still folds them all to "rpyforth".
_VARIANT_LABELS = {
    "rpyforth-c-stkfrag": "stkfrag",
    "stkfrag": "stkfrag",
    "rpyforth-c": "contiguous",
    "contiguous": "contiguous",
    "rpyforth-c-novirt": "novirt",
    "novirt": "novirt",
}


def _basename(name) -> str:
    """Strip directory and a wrapper's .sh suffix from an engine-ish string."""
    base = Path(str(name).strip()).name.lower()
    if base.endswith(".sh"):
        base = base[: -len(".sh")]
    return base


def normalize_engine(name: str) -> str:
    """Map a binary path, wrapper script, or config label to a canonical id."""
    base = _basename(name)
    if base in _ALIASES:
        return _ALIASES[base]
    # Labels may still carry an argv tail ("gforth-fast -m 16M").
    head = base.split()[0] if base else base
    return _ALIASES.get(head, head or str(name).strip())


def engine_color(engine: str) -> str:
    raw = _basename(engine)
    if raw in ENGINE_COLORS:
        return ENGINE_COLORS[raw]
    key = normalize_engine(engine)
    if key in ENGINE_COLORS:
        return ENGINE_COLORS[key]
    return _FALLBACK_PALETTE[sum(ord(c) for c in key) % len(_FALLBACK_PALETTE)]


def engine_display_name(engine: str) -> str:
    raw = _basename(engine)
    if raw in _VARIANT_LABELS:
        return _VARIANT_LABELS[raw]
    return normalize_engine(engine)


def sort_engines(engines: Iterable[str]) -> List[str]:
    """Canonical ids, PRIMARY_ENGINES first and the rest alphabetically."""
    unique: List[str] = []
    seen = set()
    for eng in engines:
        key = normalize_engine(eng)
        if key not in seen:
            seen.add(key)
            unique.append(key)
    primary = [e for e in PRIMARY_ENGINES if e in seen]
    rest = sorted(e for e in unique if e not in PRIMARY_ENGINES)
    return primary + rest


def colors_for_configs(
    config_ids: Sequence[str],
    labels: Optional[Dict[str, str]] = None,
) -> Dict[str, str]:
    """Map run_shootout config ids (A/B/C/...) to colors via their labels."""
    labels = labels or {}
    return {cid: engine_color(labels.get(cid, cid)) for cid in config_ids}


def color_map_for(keys: Sequence[str]) -> Dict[str, str]:
    """Color per series key: known engines keep their color, the rest draw from
    EXTRA_COLORS. Replaces the ad-hoc per-file dicts that raised KeyError the
    moment a vfxforth or swiftforth row appeared."""
    out: Dict[str, str] = {}
    extra = 0
    for key in keys:
        norm = normalize_engine(key)
        if _basename(key) in ENGINE_COLORS or norm in ENGINE_COLORS:
            out[key] = engine_color(key)
        else:
            out[key] = EXTRA_COLORS[extra % len(EXTRA_COLORS)]
            extra += 1
    return out


def build_engine_cmd(
    engine: str,
    driver_path,
    gforth_mem: str = GFORTH_MEM,
    gforth_setup=None,
) -> List[str]:
    """argv for running one driver file under `engine`.

    `gforth_setup` is the appbench compatibility shim. It is a per-suite choice,
    not a per-harness accident: appbench programs are written against it, while
    the shootout sources are standalone and must not have extra words injected
    over them. Callers pass GFORTH_SETUP for appbench and nothing for shootout.
    """
    binary = ENGINE_BINARY[engine]
    if engine in DIRECT_ARGV_ENGINES:
        return [str(binary), str(driver_path)]
    argv = [str(binary), "-m", gforth_mem]
    if gforth_setup is not None:
        argv.append(str(gforth_setup))
    argv.append(str(driver_path))
    return argv
