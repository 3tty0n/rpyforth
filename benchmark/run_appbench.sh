#!/usr/bin/env bash
# Run the appbench-1.4 suite under multiple Forth engines and save logs.
#
# This is a thin shell wrapper around benchmark/run_appbench.py that:
#   * runs every program under rpyforth-c-stkfrag, gforth-fast, gforth,
#     vfxforth, and swiftforth (configurable with --engines),
#   * writes per-iteration CSV curves and a JSON summary under
#     logs/<rev>/appbench/ (or the directory given with --output).
#
# The generated logs are consumed by benchmark/plot_warmup_boxplot.py and
# benchmark/plot_blog_boxplots.py to produce warm-up curves and boxplots.
#
# Usage: ./benchmark/run_appbench.sh [OPTIONS] [program ...]
#
# Options:
#   --engines ENGINES    comma-separated engines
#                        (default: rpyforth,gforth-fast,gforth,vfxforth,swiftforth)
#   --iterations N       iterations per (program, engine)
#                        (default: 50 steady, 3 func)
#   --timeout N          per-run timeout in seconds (default: 600)
#   --pin N              pin runs to this CPU core
#   --mode steady|func   measurement mode (default: steady)
#   --output DIR         log output directory (default: logs)
#   --pdf PATH           steady-mode warm-up-curve PDF
#   --chart PATH         func-mode status+timing chart
#   --rpyforth PATH      override rpyforth binary
#   --gforth PATH        override gforth binary
#   --gforth-fast PATH   override gforth-fast binary
#   --vfxforth PATH      override vfxforth runner
#   --swiftforth PATH    override swiftforth runner
#   -h, --help           show this help
#
# Examples:
#   ./benchmark/run_appbench.sh
#   ./benchmark/run_appbench.sh --engines rpyforth,gforth-fast cd16sim
#   ./benchmark/run_appbench.sh --mode func --iterations 5

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

if [[ -z "${PYTHON3:-}" && -x "${REPO_ROOT}/.venv/bin/python" ]]; then
    PYTHON3="${REPO_ROOT}/.venv/bin/python"
else
    PYTHON3="${PYTHON3:-python3}"
fi

ENGINES="rpyforth,gforth-fast,gforth,vfxforth,swiftforth"
ITERATIONS=""
TIMEOUT=600
PIN=""
MODE="steady"
OUTPUT_DIR="logs"
PDF=""
CHART=""

# Engine binary overrides
RPYFORTH=""
GFORTH=""
GFORTH_FAST=""
VFXFORTH=""
SWIFTFORTH=""

PROGRAMS=()

usage() {
    cat <<'EOF'
Usage: ./benchmark/run_appbench.sh [OPTIONS] [program ...]

Run the appbench-1.4 suite under multiple Forth engines and save logs.

Options:
  --engines ENGINES    comma-separated engines
                       (default: rpyforth,gforth-fast,gforth,vfxforth,swiftforth)
  --iterations N       iterations per (program, engine)
                       (default: 50 steady, 3 func)
  --timeout N          per-run timeout in seconds (default: 600)
  --pin N              pin runs to this CPU core
  --mode steady|func   measurement mode (default: steady)
  --output DIR         log output directory (default: logs)
  --pdf PATH           steady-mode warm-up-curve PDF
  --chart PATH         func-mode status+timing chart
  --rpyforth PATH      override rpyforth binary
  --gforth PATH        override gforth binary
  --gforth-fast PATH   override gforth-fast binary
  --vfxforth PATH      override vfxforth runner
  --swiftforth PATH    override swiftforth runner
  -h, --help           show this help

Examples:
  ./benchmark/run_appbench.sh
  ./benchmark/run_appbench.sh --engines rpyforth,gforth-fast cd16sim
  ./benchmark/run_appbench.sh --mode func --iterations 5
EOF
    exit "${1:-0}"
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --engines)
            ENGINES="$2"
            shift 2
            ;;
        --iterations)
            ITERATIONS="$2"
            shift 2
            ;;
        --timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        --pin)
            PIN="$2"
            shift 2
            ;;
        --mode)
            MODE="$2"
            shift 2
            ;;
        --output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --pdf)
            PDF="$2"
            shift 2
            ;;
        --chart)
            CHART="$2"
            shift 2
            ;;
        --rpyforth)
            RPYFORTH="$2"
            shift 2
            ;;
        --gforth)
            GFORTH="$2"
            shift 2
            ;;
        --gforth-fast)
            GFORTH_FAST="$2"
            shift 2
            ;;
        --vfxforth)
            VFXFORTH="$2"
            shift 2
            ;;
        --swiftforth)
            SWIFTFORTH="$2"
            shift 2
            ;;
        -h|--help)
            usage 0
            ;;
        --)
            shift
            PROGRAMS+=("$@")
            break
            ;;
        -*)
            echo "Unknown option: $1" >&2
            usage 1
            ;;
        *)
            PROGRAMS+=("$1")
            shift
            ;;
    esac
done

if [[ "$MODE" != "steady" && "$MODE" != "func" ]]; then
    echo "Invalid mode: $MODE (expected steady or func)" >&2
    exit 1
fi

# Build the python command.
CMD=("${PYTHON3}" "./benchmark/run_appbench.py" "$MODE")

read -ra ENGINES_ARR <<< "$(echo "$ENGINES" | tr ',' ' ')"
CMD+=("--engines" "${ENGINES_ARR[@]}")
CMD+=("--timeout" "$TIMEOUT")
CMD+=("--output" "$OUTPUT_DIR")

if [[ -n "$ITERATIONS" ]]; then
    CMD+=("--iterations" "$ITERATIONS")
fi
if [[ -n "$PIN" ]]; then
    CMD+=("--pin" "$PIN")
fi
if [[ ${#PROGRAMS[@]} -gt 0 ]]; then
    if [[ "$MODE" == "steady" ]]; then
        CMD+=("--programs" "$(IFS=,; echo "${PROGRAMS[*]}")")
    else
        # func mode only supports a single program via --only.
        if [[ ${#PROGRAMS[@]} -gt 1 ]]; then
            echo "WARNING: func mode only supports one program; using ${PROGRAMS[0]}" >&2
        fi
        CMD+=("--only" "${PROGRAMS[0]}")
    fi
fi
if [[ -n "$PDF" ]]; then
    CMD+=("--pdf" "$PDF")
fi
if [[ -n "$CHART" ]]; then
    CMD+=("--chart" "$CHART")
fi
if [[ -n "$RPYFORTH" ]]; then
    CMD+=("--rpyforth" "$RPYFORTH")
fi
if [[ -n "$GFORTH" ]]; then
    CMD+=("--gforth" "$GFORTH")
fi
if [[ -n "$GFORTH_FAST" ]]; then
    CMD+=("--gforth-fast" "$GFORTH_FAST")
fi
if [[ -n "$VFXFORTH" ]]; then
    CMD+=("--vfxforth" "$VFXFORTH")
fi
if [[ -n "$SWIFTFORTH" ]]; then
    CMD+=("--swiftforth" "$SWIFTFORTH")
fi

TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
LOG_BASE="${OUTPUT_DIR}/${TIMESTAMP}"
mkdir -p "${LOG_BASE}"
RUN_LOG="${LOG_BASE}/run_appbench_${MODE}.log"

echo "Running appbench in ${MODE} mode"
echo "Engines: ${ENGINES}"
echo "Log base: ${LOG_BASE}"
echo "Command: ${CMD[*]}"
echo "---"

set +e
"${CMD[@]}" 2>&1 | tee "${RUN_LOG}"
RC=${PIPESTATUS[0]}
set -e

echo "---"
echo "Run log: ${RUN_LOG}"
exit "$RC"
