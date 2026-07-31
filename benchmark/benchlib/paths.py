"""Repository paths, git revision, and log-directory naming.

Every harness derives its locations from here so that a moved tree or a
renamed baseline directory is a one-line change.
"""

from __future__ import annotations

import subprocess
from pathlib import Path
from typing import List, Optional

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
BENCHMARK_DIR = REPO_ROOT / "benchmark"
SHOOTOUT_DIR = REPO_ROOT / "shootout"
APPBENCH_DIR = REPO_ROOT / "appbench" / "appbench-1.4"
COREMARK_DIR = BENCHMARK_DIR / "coremark-src"
GFORTH_DIR = REPO_ROOT / "gforth-0.7.9"
GFORTH_SETUP = APPBENCH_DIR / "setup" / "gforth.fs"
LOGS_DIR = REPO_ROOT / "logs"


def git_revision(repo_root: Path = REPO_ROOT) -> str:
    """Short git revision, with a -dirty suffix when the tree has local edits.

    Benchmark outputs carry this so a result set is traceable to a commit.
    """
    def _git(cmd: List[str]) -> str:
        return subprocess.check_output(
            ["git"] + cmd, cwd=str(repo_root), stderr=subprocess.DEVNULL
        ).decode().strip()

    try:
        rev = _git(["rev-parse", "--short", "HEAD"])
    except (subprocess.CalledProcessError, OSError):
        return "unknown"
    try:
        if _git(["status", "--porcelain"]):
            rev += "-dirty"
    except (subprocess.CalledProcessError, OSError):
        pass
    return rev


def log_dir(
    base: Path,
    tool: str,
    revision: Optional[str] = None,
    rev_first: bool = False,
) -> Path:
    """Create and return the output directory for one harness run.

    Layout is `<base>/<tool>/<rev>` by default. `rev_first` selects the
    `<base>/<rev>/<tool>` layout that run_appbench has always written, kept so
    the existing log archive stays where the plotting tools expect it.
    `tool` may be empty for harnesses that write straight into `<base>/<rev>`.
    """
    rev = revision or git_revision()
    if not tool:
        out = Path(base) / rev
    elif rev_first:
        out = Path(base) / rev / tool
    else:
        out = Path(base) / tool / rev
    out.mkdir(parents=True, exist_ok=True)
    return out
