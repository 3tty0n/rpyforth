#!/usr/bin/env python3
"""Forwarder: Joy harness lives at benchmark/joy/run_joy.py."""
import runpy
import sys
from pathlib import Path

sys.argv[0] = str(Path(__file__).resolve().parent / "joy" / "run_joy.py")
runpy.run_path(str(Path(__file__).resolve().parent / "joy" / "run_joy.py"),
               run_name="__main__")
