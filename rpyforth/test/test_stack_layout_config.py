"""Translation-time environment configuration tests."""

import os
import subprocess
import sys

import pytest


_LEGACY_FLAGS = (
    "RPYFORTH_STACK_FRAGMENT",
    "RPYFORTH_FRAME_ONLY",
    "RPYFORTH_NTOP",
    "RPYFORTH_FLOAT_FRAGMENT",
)

_ALL_FLAGS = _LEGACY_FLAGS + (
    "RPYFORTH_STACK_LAYOUT",
    "RPYFORTH_FRAME_SIZE",
    "RPYFORTH_VIRTUALIZE",
    "RPYFORTH_ALLOC_MB",
    "RPYFORTH_EXE_NAME",
)


def _config_env(values=None):
    env = dict(os.environ)
    for name in _ALL_FLAGS:
        env.pop(name, None)
    if values:
        env.update(values)
    env["PYTHONPATH"] = os.pathsep.join([p for p in sys.path if p])
    return env


def _layout_env(layout=None, legacy=None):
    values = {}
    if layout is not None:
        values["RPYFORTH_STACK_LAYOUT"] = layout
    if legacy:
        values.update(legacy)
    return _config_env(values)


def _read_layout(layout=None, legacy=None):
    code = (
        "from rpyforth.metastack import "
        "STACK_LAYOUT, EFFECTIVE_NTOP, USE_STACK_FRAGMENT, USE_FRAME_ONLY, "
        "USE_NTOP_VARIANT, USE_FLOAT_FRAGMENT\n"
        "print('%s %d %d %d %d %d' % "
        "(STACK_LAYOUT, EFFECTIVE_NTOP, USE_STACK_FRAGMENT, USE_FRAME_ONLY, "
        "USE_NTOP_VARIANT, USE_FLOAT_FRAGMENT))\n"
    )
    out = subprocess.check_output(
        [sys.executable, "-c", code],
        env=_layout_env(layout, legacy),
        cwd=os.getcwd(),
        stderr=subprocess.STDOUT,
    )
    return out.decode("utf-8").strip()


@pytest.mark.parametrize("layout, expected", [
    ("plain", "plain 2 0 0 0 0"),
    ("fragment", "fragment 2 1 0 0 0"),
    ("frame-only", "frame-only 2 1 1 0 0"),
    ("ntop2", "ntop2 2 1 0 1 0"),
    ("ntop4", "ntop4 4 1 0 1 0"),
    ("ntop8", "ntop8 8 1 0 1 0"),
    ("ntop16", "ntop16 16 1 0 1 0"),
    ("fragment-float", "fragment-float 2 1 0 0 1"),
])
def test_canonical_layouts(layout, expected):
    assert _read_layout(layout) == expected


@pytest.mark.parametrize("alias, expected", [
    ("fixed", "plain 2 0 0 0 0"),
    ("stkfrag", "fragment 2 1 0 0 0"),
    ("frame", "frame-only 2 1 1 0 0"),
    ("floatfrag", "fragment-float 2 1 0 0 1"),
])
def test_readable_aliases(alias, expected):
    assert _read_layout(alias) == expected


def test_new_layout_overrides_legacy_flags():
    legacy = {
        "RPYFORTH_STACK_FRAGMENT": "1",
        "RPYFORTH_FRAME_ONLY": "1",
        "RPYFORTH_NTOP": "16",
        "RPYFORTH_FLOAT_FRAGMENT": "1",
    }
    assert _read_layout("fragment", legacy) == "fragment 2 1 0 0 0"


@pytest.mark.parametrize("legacy, expected", [
    ({}, "plain 2 0 0 0 0"),
    ({"RPYFORTH_STACK_FRAGMENT": "1"}, "fragment 2 1 0 0 0"),
    ({
        "RPYFORTH_STACK_FRAGMENT": "1",
        "RPYFORTH_FRAME_ONLY": "1",
    }, "frame-only 2 1 1 0 0"),
    ({
        "RPYFORTH_STACK_FRAGMENT": "1",
        "RPYFORTH_NTOP": "8",
    }, "ntop8 8 1 0 1 0"),
    ({
        "RPYFORTH_STACK_FRAGMENT": "1",
        "RPYFORTH_FLOAT_FRAGMENT": "1",
    }, "fragment-float 2 1 0 0 1"),
])
def test_legacy_flags_remain_compatible(legacy, expected):
    assert _read_layout(legacy=legacy) == expected


def test_invalid_layout_fails_with_allowed_values():
    proc = subprocess.Popen(
        [sys.executable, "-c", "import rpyforth.metastack"],
        env=_layout_env("fragmetn"),
        cwd=os.getcwd(),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    stdout, stderr = proc.communicate()
    assert proc.returncode != 0
    text = (stdout + stderr).decode("utf-8")
    assert "invalid RPYFORTH_STACK_LAYOUT" in text
    assert "frame-only" in text


def _read_config(values=None):
    code = (
        "from rpyforth.config import "
        "FRAME_SIZE, USE_VIRTUALIZATION, ALLOC_MB, EXE_NAME\n"
        "print('%d %d %d %s' % "
        "(FRAME_SIZE, USE_VIRTUALIZATION, ALLOC_MB, EXE_NAME))\n"
    )
    out = subprocess.check_output(
        [sys.executable, "-c", code],
        env=_config_env(values),
        cwd=os.getcwd(),
        stderr=subprocess.STDOUT,
    )
    return out.decode("utf-8").strip()


@pytest.mark.parametrize("raw, expected", [
    (None, 8),
    ("12", 12),
    ("0", 8),
    ("invalid", 8),
    ("999", 64),
])
def test_frame_size_resolution(raw, expected):
    values = {}
    if raw is not None:
        values["RPYFORTH_FRAME_SIZE"] = raw
    assert _read_config(values).split()[0] == str(expected)


def test_non_stack_settings_are_resolved_once():
    values = {
        "RPYFORTH_FRAME_SIZE": "16",
        "RPYFORTH_VIRTUALIZE": "1",
        "RPYFORTH_ALLOC_MB": "256",
        "RPYFORTH_EXE_NAME": "custom-forth",
    }
    assert _read_config(values) == "16 1 256 custom-forth"


def test_stack_fragment_mode_takes_precedence_over_old_virtualization():
    values = {
        "RPYFORTH_STACK_LAYOUT": "fragment",
        "RPYFORTH_VIRTUALIZE": "1",
    }
    assert _read_config(values).split()[1] == "0"


def test_non_stack_settings_have_readable_defaults():
    assert _read_config() == "8 0 -1 rpyforth-%(backend)s"


def test_config_module_prints_resolved_settings():
    out = subprocess.check_output(
        [sys.executable, "-m", "rpyforth.config"],
        env=_config_env({"RPYFORTH_STACK_LAYOUT": "fragment"}),
        cwd=os.getcwd(),
        stderr=subprocess.STDOUT,
    ).decode("utf-8")
    assert "stack_layout=fragment" in out
    assert "frame_size=8" in out
    assert "alloc_mb=auto" in out
