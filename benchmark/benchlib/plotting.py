"""matplotlib bootstrap and the chart primitives every report reuses.

Importing matplotlib is deferred: the measurement harnesses must run on a
machine that has no plotting stack, and several of them used to fail at import
time because a chart helper pulled pyplot in at module scope.
"""

from __future__ import annotations

import math
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple

from .engines import engine_color, engine_display_name

# One resolution for every raster output. It used to be 120, 150, or 300
# depending on which module drew the figure.
DPI = 150

# Bar-group geometry, shared so grouped charts line up across reports.
GROUP_WIDTH = 0.8


def pyplot():
    """Return the pyplot module with the Agg backend selected.

    Raises RuntimeError with an actionable message rather than ImportError, so
    a harness invoked without --chart still runs on a headless box.
    """
    try:
        import matplotlib
    except ImportError as exc:
        raise RuntimeError(
            "matplotlib is required for charts; install it into .venv "
            "(pip install matplotlib)"
        ) from exc
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    return plt


def pdf_pages(path):
    """Return a PdfPages context manager for `path`."""
    pyplot()  # ensures the Agg backend is selected first
    from matplotlib.backends.backend_pdf import PdfPages
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    return PdfPages(str(path))


def save_figure(fig, target, dpi: int = DPI, bbox_inches: str = "tight") -> None:
    """Write one figure to a PdfPages, a path, or a file object, then close it.

    A PdfPages exposes savefig; a BytesIO exposes write and is handed straight
    to fig.savefig, which is how the HTML report embeds figures inline.
    """
    plt = pyplot()
    if hasattr(target, "savefig") and not hasattr(target, "write"):
        target.savefig(fig, bbox_inches=bbox_inches)
    elif hasattr(target, "write"):
        fig.savefig(target, dpi=dpi, bbox_inches=bbox_inches)
    else:
        Path(target).parent.mkdir(parents=True, exist_ok=True)
        fig.savefig(str(target), dpi=dpi, bbox_inches=bbox_inches)
    plt.close(fig)


def output_paths(base, formats: Iterable[str]) -> List[Path]:
    """Sibling output paths for a base path and a set of extensions."""
    base = Path(base)
    stem = base.with_suffix("")
    return [stem.with_suffix("." + fmt.lstrip(".")) for fmt in formats]


def group_offsets(n_series: int, width: float = GROUP_WIDTH) -> List[float]:
    """Offsets of each series within one category slot, plus the bar width.

    Returns the offsets; the bar width is `width / n_series`.
    """
    if n_series <= 0:
        return []
    bar = width / n_series
    return [-width / 2 + bar * (i + 0.5) for i in range(n_series)]


def bar_width(n_series: int, width: float = GROUP_WIDTH) -> float:
    return width / n_series if n_series > 0 else width


def engine_style(engines: Sequence[str]) -> Dict[str, Dict[str, str]]:
    """Color and legend label per engine, from the one engine registry."""
    return {
        eng: {"color": engine_color(eng), "label": engine_display_name(eng)}
        for eng in engines
    }


def add_geomean_separator(ax, n_categories: int, horizontal: bool = True) -> None:
    """Rule between the per-benchmark rows and the trailing geomean row."""
    pos = n_categories - 1.5
    if horizontal:
        ax.axhline(pos, color="gray", linewidth=0.8, linestyle=":")
    else:
        ax.axvline(pos, color="gray", linewidth=0.8, linestyle=":")


def style_boxplot(bp, colors: Sequence[str], alpha: float = 0.7) -> None:
    """Apply per-box face colors to a patch_artist boxplot."""
    for box, color in zip(bp.get("boxes", []), colors):
        box.set_facecolor(color)
        box.set_alpha(alpha)


def legend_patches(ax, entries: Sequence[tuple], alpha: float = 0.7, **kwargs):
    """Attach a legend built from (label, color) pairs."""
    from matplotlib.patches import Patch
    handles = [
        Patch(facecolor=color, alpha=alpha, label=label) for label, color in entries
    ]
    return ax.legend(handles=handles, **kwargs)


def log_ticks(lo: float, hi: float) -> List[float]:
    """Decade ticks covering [lo, hi], with a 1-2-5 step when the span is narrow.

    For axes that carry absolute magnitudes (microseconds, bytes), where the
    range routinely spans several decades.
    """
    if lo <= 0 or hi <= 0 or hi < lo:
        return []
    lo_exp = int(math.floor(math.log10(lo)))
    hi_exp = int(math.ceil(math.log10(hi)))
    steps = [1.0, 2.0, 5.0] if hi_exp - lo_exp <= 2 else [1.0]
    ticks = []
    for exp in range(lo_exp, hi_exp + 1):
        for step in steps:
            value = step * (10.0 ** exp)
            if lo <= value <= hi:
                ticks.append(value)
    return ticks


# Tick positions for a ratio axis (speedup, normalized time). Ratios cluster
# around 1.0 and rarely leave [0.05, 50], so a hand-picked ladder reads better
# than decades: it keeps 1.0 on the axis and gives the 1x-3x band the
# resolution that is actually being compared.
RATIO_TICK_CANDIDATES = [
    0.05, 0.1, 0.2, 0.3, 0.5, 0.7, 1.0, 1.5, 2.0, 3.0, 4.0, 5.0,
    7.0, 10.0, 15.0, 20.0, 30.0, 50.0,
]


def ratio_log_ticks(lo: float, hi: float) -> Tuple[List[float], List[str]]:
    """Tick positions and labels for a log-scaled ratio axis."""
    ticks = [t for t in RATIO_TICK_CANDIDATES if lo <= t <= hi]
    if not ticks:
        ticks = [lo, 1.0, hi] if lo < 1.0 < hi else [lo, hi]
    labels = [
        "%d" % int(round(t)) if t >= 10 or abs(t - round(t)) < 1e-9 else "%.1f" % t
        for t in ticks
    ]
    return ticks, labels


def nice_log_limits(
    values: Sequence[float],
    floor: Optional[float] = None,
    pad_lo: float = 0.85,
    pad_hi: float = 1.15,
) -> Optional[Tuple[float, float]]:
    """Padded (lo, hi) for a log axis, or None when there is nothing to plot."""
    finite = [v for v in values if v is not None and v > 0 and math.isfinite(v)]
    if not finite:
        return None
    lo = min(finite) * pad_lo
    hi = max(finite) * pad_hi
    if floor is not None:
        lo = max(floor, lo)
    return lo, hi


def apply_log_axis(
    ax,
    values: Sequence[float],
    axis: str = "y",
    ticks: Optional[Sequence[float]] = None,
    labels: Optional[Sequence[str]] = None,
    floor: Optional[float] = None,
    pad_lo: float = 0.85,
    pad_hi: float = 1.15,
) -> None:
    """Log-scale one axis with readable ticks and no minor-tick clutter."""
    from matplotlib.ticker import NullFormatter, NullLocator
    if axis == "y":
        ax.set_yscale("log")
        target_axis, set_lim = ax.yaxis, ax.set_ylim
    else:
        ax.set_xscale("log")
        target_axis, set_lim = ax.xaxis, ax.set_xlim

    target_axis.set_minor_locator(NullLocator())
    target_axis.set_minor_formatter(NullFormatter())

    limits = nice_log_limits(values, floor=floor, pad_lo=pad_lo, pad_hi=pad_hi)
    if limits is None:
        return
    lo, hi = limits
    set_lim(lo, hi)

    if ticks is None:
        ticks = log_ticks(lo, hi)
        labels = ["%g" % t for t in ticks]
    if ticks:
        target_axis.set_ticks(list(ticks))
        target_axis.set_ticklabels(list(labels or ["%g" % t for t in ticks]))


def annotate_bars(ax, bars, labels: Sequence[str], horizontal: bool = True,
                  fontsize: int = 7, offset: float = 0.01) -> None:
    """Write a value label at the tip of each bar."""
    for bar, text in zip(bars, labels):
        if not text:
            continue
        if horizontal:
            x = bar.get_width()
            ax.text(x * (1 + offset) if x > 0 else offset,
                    bar.get_y() + bar.get_height() / 2, text,
                    va="center", ha="left", fontsize=fontsize)
        else:
            y = bar.get_height()
            ax.text(bar.get_x() + bar.get_width() / 2,
                    y * (1 + offset) if y > 0 else offset, text,
                    ha="center", va="bottom", fontsize=fontsize)
