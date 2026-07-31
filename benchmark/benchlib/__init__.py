"""Shared foundation for the benchmark harnesses under benchmark/.

Import the submodules rather than reaching into a sibling harness:

    paths     repo layout, git revision, log-directory naming
    engines   engine ids, binaries, argv, colors, labels, ordering
    suites    benchmark/program lists, discovery, stdin fixtures
    procs     pinning, child environment, timeouts, process-group cleanup
    curves    warm-up curve parsing and steady-state summarisation
    drivers   Forth driver generation for both suites
    stats     geomean, bootstrap CI, speedups, formatting
    plotting  matplotlib bootstrap and shared chart primitives
"""

from __future__ import annotations

from . import curves, drivers, engines, paths, plotting, procs, stats, suites

__all__ = [
    "curves",
    "drivers",
    "engines",
    "paths",
    "plotting",
    "procs",
    "stats",
    "suites",
]
