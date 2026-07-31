#!/usr/bin/env bash
# Run one appbench program directly with an RPyForth binary.
# This is intentionally not the multi-engine benchmark harness.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENGINE="rpyforth"

usage() {
    cat <<'EOF'
Usage: ./appbench.sh [--engine ENGINE] PROGRAM
       ./appbench.sh ENGINE PROGRAM
       ./appbench.sh [--engine ENGINE] ENTRY.fs

Run one appbench entry directly.  PROGRAM is one of:
  cd16sim  brainless  fcp  lexex  benchgc  coremark

Examples:
  ./appbench.sh benchgc
  ./appbench.sh gforth-fast fcp
  ./appbench.sh --engine vfxforth cd16sim
  ./appbench.sh --engine ./rpyforth-c appbench/entries/lexex.fs

Named engines:
  rpyforth  rpyforth-c-stkfrag  rpyforth-c
  gforth  gforth-fast  vfxforth  swiftforth

`rpyforth` is an alias for `rpyforth-c-stkfrag`.

The wrapper changes to the program's working directory before starting
RPyForth, because appbench sources use working-directory-relative INCLUDEs.
EOF
}

if [[ "${1:-}" == "--engine" ]]; then
    if [[ $# -lt 3 ]]; then
        usage >&2
        exit 2
    fi
    ENGINE="$2"
    shift 2
elif [[ $# -ge 2 ]]; then
    case "$1" in
        rpyforth|rpyforth-c-stkfrag|rpyforth-c|gforth|gforth-fast|vfxforth|swiftforth)
            ENGINE="$1"
            shift
            ;;
    esac
fi

if [[ $# -ne 1 || "$1" == "-h" || "$1" == "--help" ]]; then
    usage
    [[ $# -eq 1 ]] && exit 0
    exit 2
fi

ENGINE_KIND="custom"
case "$ENGINE" in
    rpyforth|rpyforth-c-stkfrag)
        ENGINE_KIND="rpyforth"
        ENGINE="${REPO_ROOT}/rpyforth-c-stkfrag"
        ;;
    rpyforth-c)
        ENGINE_KIND="rpyforth"
        ENGINE="${REPO_ROOT}/rpyforth-c"
        ;;
    gforth)
        ENGINE_KIND="gforth"
        ENGINE="${REPO_ROOT}/gforth-0.7.9/gforth"
        ;;
    gforth-fast)
        ENGINE_KIND="gforth"
        ENGINE="${REPO_ROOT}/gforth-0.7.9/gforth-fast"
        ;;
    vfxforth)
        ENGINE_KIND="vfxforth"
        ENGINE="${REPO_ROOT}/vfxforth.sh"
        ;;
    swiftforth)
        ENGINE_KIND="swiftforth"
        ENGINE="${REPO_ROOT}/swiftforth.sh"
        ;;
    /*) ;;
    *) ENGINE="$(pwd)/${ENGINE}" ;;
esac

if [[ ! -x "$ENGINE" ]]; then
    echo "appbench.sh: engine is not executable: ${ENGINE}" >&2
    exit 1
fi

PROGRAM_OR_ENTRY="$1"
case "$PROGRAM_OR_ENTRY" in
    cd16sim|brainless|fcp|lexex|benchgc)
        PROGRAM="$PROGRAM_OR_ENTRY"
        ENTRY="${REPO_ROOT}/appbench/entries/${PROGRAM}.fs"
        WORKDIR="${REPO_ROOT}/appbench/appbench-1.4/${PROGRAM}"
        ;;
    coremark)
        PROGRAM="coremark"
        ENTRY="${REPO_ROOT}/appbench/entries/coremark.fs"
        WORKDIR="${REPO_ROOT}/benchmark/coremark-src"
        ;;
    *)
        if [[ "$PROGRAM_OR_ENTRY" = /* ]]; then
            ENTRY="$PROGRAM_OR_ENTRY"
        else
            ENTRY="$(pwd)/${PROGRAM_OR_ENTRY}"
        fi
        if [[ ! -f "$ENTRY" ]]; then
            echo "appbench.sh: entry file not found: ${ENTRY}" >&2
            exit 1
        fi
        ENTRY_DIR="$(cd "$(dirname "$ENTRY")" && pwd)"
        ENTRY="${ENTRY_DIR}/$(basename "$ENTRY")"
        PROGRAM="$(basename "$ENTRY" .fs)"
        if [[ "$ENTRY_DIR" == "${REPO_ROOT}/appbench/entries" ]]; then
            case "$PROGRAM" in
                cd16sim|brainless|fcp|lexex|benchgc)
                    WORKDIR="${REPO_ROOT}/appbench/appbench-1.4/${PROGRAM}"
                    ;;
                coremark)
                    WORKDIR="${REPO_ROOT}/benchmark/coremark-src"
                    ;;
                *) WORKDIR="$ENTRY_DIR" ;;
            esac
        else
            WORKDIR="$ENTRY_DIR"
        fi
        ;;
esac

if [[ ! -f "$ENTRY" ]]; then
    echo "appbench.sh: entry file not found: ${ENTRY}" >&2
    exit 1
fi
if [[ ! -d "$WORKDIR" ]]; then
    echo "appbench.sh: program directory not found: ${WORKDIR}" >&2
    exit 1
fi

# benchgc reserves a large heap. Preserve an explicit user setting.
if [[ "$PROGRAM" == "benchgc" ]]; then
    export RPYFORTH_ALLOC_MB="${RPYFORTH_ALLOC_MB:-256}"
fi

cd "$WORKDIR"
case "$ENGINE_KIND" in
    gforth)
        # Gforth resolves a nested INCLUDE relative to the source file passed as
        # an argv file.  Evaluate the small entry as an expression instead, so
        # appbench's nested includes resolve from WORKDIR like the other engines.
        FORTH_EXPR="$(<"$ENTRY")"
        exec "$ENGINE" -m 16M \
            "${REPO_ROOT}/appbench/appbench-1.4/setup/gforth.fs" \
            -e "$FORTH_EXPR"
        ;;
    *)
        exec "$ENGINE" "$ENTRY"
        ;;
esac
