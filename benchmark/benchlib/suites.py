"""Which benchmarks exist, in what order, and how each one has to be fed.

The name lists here are what a report iterates and what a sweep measures, so a
benchmark cannot be measured by one and dropped by the other. Discovery has one
rule about what counts as a benchmark: shootout/curve/ is the same set of
programs under a warm-up driver, and the engine-specific rewrites under
shootout/vfxforth/ and shootout/swiftforth/ are not benchmarks at all.
"""

from __future__ import annotations

from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence

from .paths import REPO_ROOT, SHOOTOUT_DIR

# --------------------------------------------------------------------------
# shootout
# --------------------------------------------------------------------------

# Display order for the full shootout suite.
SHOOTOUT_PROGRAMS = [
    "ack", "ary", "callheavy", "composite", "except", "fibo", "hash", "hash2",
    "heap", "hello", "lists", "matrix", "methcall", "moments", "nestedloop",
    "objinst", "random", "recurse", "reversefile", "sieve", "spellcheck",
    "strcat", "sumcol", "wc", "wordfreq",
]

# The compute kernels: self-contained, no stdin, long enough to warm up. Sweeps
# and the ablation ladder use this subset because a sweep runs every cell.
SHOOTOUT_KERNELS = [
    "ack", "ary", "callheavy", "composite", "except", "fibo", "heap",
    "matrix", "methcall", "nestedloop", "random", "recurse", "sieve",
]

# Benchmarks that read stdin. Paths are relative to the repo root; the runner
# opens them and hands the file to the child as stdin. Feeding these from
# /dev/null (as the ablation and RQ harnesses used to) measures an empty
# workload, not the benchmark.
STDIN_FILES: Dict[str, str] = {
    "sumcol": "shootout/data/sumcol.txt",
    "wc": "shootout/data/wc.txt",
    "reversefile": "shootout/data/reversefile.txt",
    "spellcheck": "shootout/data/spellcheck.txt",
    "moments": "shootout/data/moments.txt",
    "wordfreq": "shootout/data/wordfreq.txt",
}

# Engine-specific rewrites of the shootout sources live in these subdirectories
# and are selected by the wrapper scripts; they are not separate benchmarks.
VARIANT_DIRS = frozenset({"vfxforth", "swiftforth"})


def shootout_stem(name) -> str:
    """Bare benchmark name from a path, display name, or stem.

    "shootout/curve/fibo.fs", "shootout/fibo.fs" and "fibo" all map to "fibo",
    so a curve run and a single-shot run of the same benchmark join up.
    """
    return Path(str(name)).stem


def stdin_path_for(name) -> Optional[Path]:
    """Absolute path of the stdin fixture for a benchmark, if it needs one."""
    rel = STDIN_FILES.get(shootout_stem(name))
    return None if rel is None else REPO_ROOT / rel


def discover_shootout(
    root: Path = SHOOTOUT_DIR,
    include_curve: bool = True,
) -> List[Path]:
    """Every shootout .fs file, sorted, minus the engine-specific variants."""
    if not root.is_dir():
        raise RuntimeError("shootout/ directory not found at %s" % root)
    found = []
    for path in sorted(root.rglob("*.fs")):
        if not path.is_file():
            continue
        parts = path.relative_to(root).parts[:-1]
        if any(part in VARIANT_DIRS for part in parts):
            continue
        if not include_curve and "curve" in parts:
            continue
        found.append(path)
    return found


def shootout_display_name(path: Path, repo_root: Path = REPO_ROOT) -> str:
    """The "shootout/..." name used in reports and log headers."""
    try:
        return str(Path("shootout") / Path(path).relative_to(repo_root / "shootout"))
    except ValueError:
        return str(Path(path).relative_to(repo_root))


# --------------------------------------------------------------------------
# appbench
# --------------------------------------------------------------------------

# Display and measurement order for the appbench suite. coremark is part of it:
# an earlier copy of this list omitted it, so it was measured and then dropped
# from the report.
APPBENCH_PROGRAMS = [
    "cd16sim", "brainless", "fcp", "benchgc", "coremark", "lexex",
]

# lexex rebuilds its FSM tables every iteration and is an order of magnitude
# slower than the rest, so sweeps give it fewer iterations. One value now;
# it used to be 6, 15, or 20 depending on which sweep you ran.
LEXEX_ITERATIONS = 15


def iterations_for(
    program: str,
    default: int,
    lexex_iterations: Optional[int] = None,
) -> int:
    """Per-program iteration count for a sweep, honouring a caller's override."""
    if program != "lexex":
        return default
    return LEXEX_ITERATIONS if lexex_iterations is None else lexex_iterations


# --------------------------------------------------------------------------
# ordering helpers, shared by every report and plot
# --------------------------------------------------------------------------

SUITE_PROGRAMS = {
    "shootout": SHOOTOUT_PROGRAMS,
    "appbench": APPBENCH_PROGRAMS,
    "all": SHOOTOUT_PROGRAMS + APPBENCH_PROGRAMS,
}


def suite_of(program: str) -> Optional[str]:
    name = shootout_stem(program)
    if name in APPBENCH_PROGRAMS:
        return "appbench"
    if name in SHOOTOUT_PROGRAMS:
        return "shootout"
    return None


def sort_programs(programs: Iterable[str]) -> List[str]:
    """Suite order first (shootout then appbench), unknown names last."""
    order = {name: i for i, name in enumerate(SUITE_PROGRAMS["all"])}
    return sorted(programs, key=lambda p: (order.get(shootout_stem(p), 10_000), str(p)))


def select_programs(requested: Optional[str], available: Sequence[str]) -> List[str]:
    """Resolve a comma-separated --programs value against what exists.

    An unknown name is an error rather than a silently empty run.
    """
    if not requested:
        return list(available)
    wanted = [p.strip() for p in str(requested).split(",") if p.strip()]
    unknown = [p for p in wanted if p not in available]
    if unknown:
        raise SystemExit(
            "unknown program(s): %s (available: %s)"
            % (", ".join(unknown), ", ".join(available))
        )
    return wanted
