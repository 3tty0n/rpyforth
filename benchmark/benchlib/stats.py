"""Summary statistics shared by every harness and plot.

The aggregate a report prints has to mean the same thing in every report, down
to how it handles a missing measurement — hence one definition of geomean, one
of the bootstrap CI, and one of "how much faster".
"""

from __future__ import annotations

import math
import random
import statistics
from typing import Iterable, List, Optional, Sequence, Tuple


def geomean(values: Iterable[Optional[float]]) -> Optional[float]:
    """Geometric mean of the positive, non-None values; None when there are none.

    None means "nothing to aggregate" and must be rendered as n/a. It is not
    1.0 (which would read as "no difference") and not 0.0 (which would read as
    an infinitely fast result).
    """
    vals = [float(v) for v in values if v is not None and v > 0]
    if not vals:
        return None
    return math.exp(sum(math.log(v) for v in vals) / len(vals))


def median_ci(
    samples: Sequence[float],
    confidence: float = 0.90,
    resamples: int = 2000,
) -> Tuple[Optional[float], float]:
    """Median and the relative half-width (%) of a bootstrap CI of the median.

    The RNG is seeded so a rerun over the same samples reports the same CI.
    """
    if not samples:
        return (None, 0.0)
    med = statistics.median(samples)
    if len(samples) == 1 or med == 0:
        return (med, 0.0)
    rng = random.Random(20240624)
    n = len(samples)
    boot = sorted(
        statistics.median(samples[rng.randrange(n)] for _ in range(n))
        for _ in range(resamples)
    )
    lo = boot[int((1.0 - confidence) / 2 * resamples)]
    hi = boot[min(resamples - 1, int((1.0 + confidence) / 2 * resamples))]
    return (med, 100.0 * (hi - lo) / 2.0 / med)


def speedup(reference: Optional[float], value: Optional[float]) -> Optional[float]:
    """reference / value: how many times faster `value` is than `reference`.

    None when either side is missing or non-positive, so a missing measurement
    never turns into a 1.0x "tie".
    """
    if reference is None or value is None:
        return None
    if reference <= 0 or value <= 0:
        return None
    return float(reference) / float(value)


def geomean_speedup(
    pairs: Iterable[Tuple[Optional[float], Optional[float]]],
) -> Optional[float]:
    """Geomean of per-benchmark speedups from (reference, value) pairs."""
    return geomean([speedup(ref, val) for ref, val in pairs])


def fmt_usec(v: Optional[float]) -> str:
    if v is None:
        return "  n/a"
    if v >= 1000:
        return "%.1f ms" % (v / 1000.0)
    return "%d us" % v


def fmt_ratio(v: Optional[float], width: int = 0) -> str:
    text = "n/a" if v is None else "%.2fx" % v
    return text.rjust(width) if width else text


def mean(values: Sequence[float]) -> Optional[float]:
    vals = [v for v in values if v is not None]
    return statistics.mean(vals) if vals else None


def median(values: Sequence[float]) -> Optional[float]:
    vals = [v for v in values if v is not None]
    return statistics.median(vals) if vals else None


def stdev(values: Sequence[float]) -> Optional[float]:
    vals = [v for v in values if v is not None]
    return statistics.stdev(vals) if len(vals) > 1 else None


def relative_stdev_pct(values: Sequence[float]) -> Optional[float]:
    """Coefficient of variation in percent; None below two samples."""
    vals = [v for v in values if v is not None]
    if len(vals) < 2:
        return None
    avg = statistics.mean(vals)
    if avg == 0:
        return None
    return 100.0 * statistics.stdev(vals) / avg


def bounded_list(values: Iterable[Optional[float]]) -> List[float]:
    """Drop None and non-finite entries; used before feeding matplotlib."""
    out: List[float] = []
    for v in values:
        if v is None:
            continue
        f = float(v)
        if math.isfinite(f):
            out.append(f)
    return out
