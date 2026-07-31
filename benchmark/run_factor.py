#!/usr/bin/env python3
"""Forwarder: Factor harness lives at benchmark/factor/run_factor.py."""
import runpy
import sys
from pathlib import Path

sys.argv[0] = str(Path(__file__).resolve().parent / "factor" / "run_factor.py")
runpy.run_path(str(Path(__file__).resolve().parent / "factor" / "run_factor.py"),
               run_name="__main__")
