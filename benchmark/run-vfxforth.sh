#!/bin/bash
# Thin wrapper kept for backward compatibility; the real runner is vfxforth.sh.
exec "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/vfxforth.sh" "$@"
