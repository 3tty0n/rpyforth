"""Running engine processes: pinning, environment, timeouts, and cleanup.

The two entry points differ only in how a timeout is handled. `run_capture`
uses subprocess.run and is right for anything that exits on its own.
`run_popen` puts the child in its own session and kills the whole group, which
is what the wrapper scripts need: vfxforth.sh otherwise leaves a VFX binary
parked on its "Press E to exit" prompt after a SIGSEGV.
"""

from __future__ import annotations

import os
import platform
import shutil
import subprocess
import time
from pathlib import Path
from typing import Dict, List, Optional, Sequence

# Core the benchmarks are pinned to by default. One value across the harnesses,
# so "pinned to a quiet core" is a property of the machine setup rather than of
# whichever tool happens to be running.
DEFAULT_PIN = 3

# Wall-clock ceiling for a single engine invocation.
DEFAULT_TIMEOUT = 300
STEADY_DEFAULT_TIMEOUT = 600

_TASKSET_WARNED = False


def pin_prefix(pin: Optional[int]) -> List[str]:
    """argv prefix that pins the child to one core, when that is possible.

    Degrades to no pinning with a warning instead of failing, so a container
    without util-linux still produces (noisier) numbers.
    """
    global _TASKSET_WARNED
    if pin is None:
        return []
    if shutil.which("taskset") is None:
        if not _TASKSET_WARNED:
            print("warning: taskset not found; running unpinned")
            _TASKSET_WARNED = True
        return []
    return ["taskset", "-c", str(pin)]


def engine_env(
    extra: Optional[Dict[str, str]] = None,
    pypylog: Optional[str] = None,
) -> Dict[str, str]:
    """A child environment: the current one plus per-engine overrides.

    `pypylog` is the full PYPYLOG value ("<section>:<path>"), so the caller
    picks the section rather than each harness hardcoding its own.
    """
    env = os.environ.copy()
    if extra:
        env.update(extra)
    if pypylog:
        env["PYPYLOG"] = pypylog
    return env


def pypylog_value(section: str, path) -> str:
    """PYPYLOG spec for one log section, e.g. jit-summary / gc / jit-log-opt."""
    return "%s:%s" % (section, path)


class RunOutcome:
    """Result of one engine invocation.

    `rc` is the child's exit status, or -1 when it was killed on timeout.
    stdout is preserved in that case: a timed-out curve run still carries the
    iterations it managed to print, which is what makes a timeout diagnosable.
    """

    __slots__ = ("cmd", "rc", "stdout", "stderr", "wall", "timed_out", "missing")

    def __init__(self, cmd, rc, stdout, stderr, wall, timed_out, missing=False):
        self.cmd = list(cmd)
        self.rc = rc
        self.stdout = stdout
        self.stderr = stderr
        self.wall = wall
        self.timed_out = timed_out
        # True when the binary itself was not found (rc -2), which is a setup
        # problem rather than a benchmark failure.
        self.missing = missing

    @property
    def ok(self) -> bool:
        return self.rc == 0 and not self.timed_out and not self.missing

    def as_dict(self) -> Dict:
        return {
            "cmd": self.cmd,
            "rc": self.rc,
            "wall": self.wall,
            "timed_out": self.timed_out,
            "missing": self.missing,
            "stderr": self.stderr,
        }


def _decode(value) -> str:
    """PyPy hands back bytes even with text=True; normalise defensively."""
    if value is None:
        return ""
    if isinstance(value, bytes):
        return value.decode("utf-8", "replace")
    return value


def run_capture(
    cmd: Sequence[str],
    cwd=None,
    env: Optional[Dict[str, str]] = None,
    timeout: int = DEFAULT_TIMEOUT,
    pin: Optional[int] = None,
    stdin_path=None,
) -> RunOutcome:
    """Run one command to completion, capturing stdout and stderr."""
    argv = pin_prefix(pin) + [str(c) for c in cmd]
    stdin_fh = None
    t0 = time.perf_counter()
    try:
        if stdin_path is not None:
            stdin_fh = open(stdin_path, "rb")
        try:
            proc = subprocess.run(
                argv,
                capture_output=True,
                text=True,
                timeout=timeout,
                env=env,
                cwd=None if cwd is None else str(cwd),
                stdin=stdin_fh if stdin_fh is not None else subprocess.DEVNULL,
            )
            return RunOutcome(
                argv, proc.returncode, _decode(proc.stdout), _decode(proc.stderr),
                time.perf_counter() - t0, False,
            )
        except subprocess.TimeoutExpired as exc:
            return RunOutcome(
                argv, -1, _decode(exc.stdout), _decode(exc.stderr),
                time.perf_counter() - t0, True,
            )
        except FileNotFoundError as exc:
            return RunOutcome(
                argv, -2, "", str(exc), time.perf_counter() - t0, False, missing=True,
            )
    finally:
        if stdin_fh is not None:
            stdin_fh.close()


def run_popen(
    cmd: Sequence[str],
    cwd=None,
    env: Optional[Dict[str, str]] = None,
    timeout: int = STEADY_DEFAULT_TIMEOUT,
    pin: Optional[int] = None,
) -> RunOutcome:
    """Run one command in its own session; kill the whole group on timeout."""
    argv = pin_prefix(pin) + [str(c) for c in cmd]
    t0 = time.perf_counter()
    try:
        proc = subprocess.Popen(
            argv,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            env=env,
            cwd=None if cwd is None else str(cwd),
            stdin=subprocess.DEVNULL,
            start_new_session=True,
        )
    except FileNotFoundError as exc:
        return RunOutcome(argv, -2, "", str(exc), time.perf_counter() - t0,
                          False, missing=True)

    timed_out = False
    try:
        stdout, stderr = proc.communicate(timeout=timeout)
        rc = proc.returncode
    except subprocess.TimeoutExpired:
        timed_out = True
        try:
            os.killpg(proc.pid, 9)
        except (ProcessLookupError, PermissionError):
            proc.kill()
        stdout, stderr = proc.communicate()
        rc = -1
    return RunOutcome(argv, rc, _decode(stdout), _decode(stderr),
                      time.perf_counter() - t0, timed_out)


def capture_environment(pin: Optional[int] = None) -> str:
    """One-line description of the measurement machine, for reproducibility."""
    cpu = platform.processor() or platform.machine()
    try:
        for line in Path("/proc/cpuinfo").read_text().splitlines():
            if line.startswith("model name"):
                cpu = line.split(":", 1)[1].strip()
                break
    except OSError:
        pass
    try:
        gov = Path(
            "/sys/devices/system/cpu/cpu0/cpufreq/scaling_governor"
        ).read_text().strip()
    except OSError:
        gov = "?"
    try:
        load1 = "%.2f" % os.getloadavg()[0]
    except (OSError, AttributeError):
        load1 = "?"
    pin_s = "core %d" % pin if pin is not None else "unpinned"
    return "env: %s | governor %s | load1 %s | %s" % (cpu, gov, load1, pin_s)
