"""Parsing and summarising the per-iteration warm-up curve.

Every driver prints one `i,elapsed_usec` line per timed iteration. The parser is
deliberately tolerant: a data row is any line whose first comma-separated field
is a digit string and whose second parses as an integer, extra fields and
surrounding blank lines included. Several workloads write to stdout while they
run, and a stricter match turns that into a silent zero-sample result rather
than into an error anyone would notice.
"""

from __future__ import annotations

import statistics
from typing import List, Optional, Sequence

# Fraction of the curve treated as converged. The steady-state number every
# report quotes is the median of this tail.
STEADY_TAIL_FRAC = 0.5


def parse_curve_output(stdout: str) -> List[int]:
    """Extract per-iteration timings from a driver's CSV output."""
    times: List[int] = []
    for line in stdout.splitlines():
        line = line.strip()
        if not line or line.lower().startswith("iteration"):
            continue
        parts = [p.strip() for p in line.split(",")]
        if len(parts) >= 2 and parts[0].isdigit():
            try:
                times.append(int(parts[1]))
            except ValueError:
                continue
    return times


def steady_onset_index(times: Sequence[int], frac: float = STEADY_TAIL_FRAC) -> int:
    """Index at which the converged tail begins (for marking a chart)."""
    if not times:
        return 0
    return int(len(times) * (1.0 - frac))


def steady_tail(times: Sequence[int], frac: float = STEADY_TAIL_FRAC) -> List[int]:
    """The converged tail itself; the whole series when it is too short."""
    if not times:
        return []
    return list(times[steady_onset_index(times, frac):]) or list(times)


def steady_state_tail(
    times: Sequence[int],
    frac: float = STEADY_TAIL_FRAC,
) -> Optional[float]:
    """Median of the converged tail. None (not 0, not a raise) on empty input."""
    tail = steady_tail(times, frac)
    if not tail:
        return None
    return float(statistics.median(tail))


def steady_state_tail_usec(
    times: Sequence[int],
    frac: float = STEADY_TAIL_FRAC,
) -> Optional[int]:
    """steady_state_tail truncated to whole microseconds, as written to JSON."""
    med = steady_state_tail(times, frac)
    return None if med is None else int(med)


def cold_usec(times: Sequence[int]) -> Optional[int]:
    """First iteration: the cold, fully unwarmed run."""
    return int(times[0]) if times else None


def warm_drift_pct(
    times: Sequence[int],
    frac: float = STEADY_TAIL_FRAC,
) -> Optional[float]:
    """Drift across the converged tail, as a percentage of its median.

    A large value means the run never actually converged, so the steady-state
    number is not trustworthy.
    """
    tail = steady_tail(times, frac)
    if len(tail) < 4:
        return None
    med = statistics.median(tail)
    if med == 0:
        return None
    half = len(tail) // 2
    first = statistics.median(tail[:half])
    last = statistics.median(tail[half:])
    return 100.0 * (last - first) / med
