#!/bin/bash
# Run shootout and/or appbench benchmarks under multiple Forth engines and save logs.
#
# Usage:
#   ./benchmark/runbench.sh [OPTIONS]
#
# Options:
#   --shootout   Run shootout benchmarks only
#   --appbench   Run appbench-1.4 benchmarks only
#   --curve      Run shootout curve benchmarks only (for warmup analysis)
#   --all        Run both standard and curve shootout benchmarks
#   --no-memory  Disable memory profiling (shootout only)
#   --pin N      Pin runs to CPU core N
#   -h, --help   Show this help
#
# Without --shootout or --appbench, both suites are run.
#
# The logs are written to logs/<timestamp>/ and are read back by
# run_shootout.load_log; benchmark/plot_warmup_boxplot.py turns them into
# warm-up curves and boxplots.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

SHOOTOUT_DIR="shootout"

BENCHMARKS=(fibo.fs ack.fs nestedloop.fs sieve.fs heap.fs ary.fs)
CURVE_BENCHMARKS=(fibo.fs ack.fs nestedloop.fs sieve.fs heap.fs ary.fs)
COMMANDS=(gforth ./gforth-fast.sh ./rpyforth.sh ./vfxforth.sh ./swiftforth.sh)
WARMUP_RUNS=${WARMUP_RUNS:-5}
MEASURE_RUNS=${MEASURE_RUNS:-100}
BENCH_TIMEOUT=${BENCH_TIMEOUT:-60}

APPBENCH_ITERATIONS=${APPBENCH_ITERATIONS:-50}
APPBENCH_TIMEOUT=${APPBENCH_TIMEOUT:-600}

if [[ -x "${REPO_ROOT}/.venv/bin/python" ]]; then
    PYTHON3="${REPO_ROOT}/.venv/bin/python"
else
    PYTHON3="${PYTHON3:-python3}"
fi

# Parse command line arguments
MODE="standard"  # standard, curve, or all (shootout mode)
ENABLE_MEMORY=true
RUN_SHOOTOUT=false
RUN_APPBENCH=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --shootout)
            RUN_SHOOTOUT=true
            shift
            ;;
        --appbench)
            RUN_APPBENCH=true
            shift
            ;;
        --curve)
            MODE="curve"
            shift
            ;;
        --all)
            MODE="all"
            shift
            ;;
        --no-memory)
            ENABLE_MEMORY=false
            shift
            ;;
        --pin)
            PIN="$2"
            shift 2
            ;;
        -h|--help)
            sed -n '/^# /,/^# /p' "${BASH_SOURCE[0]}" | sed 's/^# //'
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Default: run both suites when neither is explicitly selected.
if [[ "$RUN_SHOOTOUT" == false && "$RUN_APPBENCH" == false ]]; then
    RUN_SHOOTOUT=true
    RUN_APPBENCH=true
fi

trap 'echo -e "\n\n[Aborted by user] Exiting..."; exit 1' INT

# Create a unique directory for this batch
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
LOG_DIR="logs/${TIMESTAMP}"
mkdir -p "$LOG_DIR"

echo "---------------------------------------------------"
echo "Starting Benchmark Batch: $TIMESTAMP"
echo "Shootout: $RUN_SHOOTOUT (mode: $MODE)"
echo "Appbench: $RUN_APPBENCH"
echo "Memory profiling: $ENABLE_MEMORY"
echo "Logs directory: $LOG_DIR"
echo "---------------------------------------------------"

# Normalize a command path into a short engine/config name.
config_name() {
    local cmd="$1"
    local name
    name=$(basename "${cmd}" .sh)
    # Strip leading ./ if present.
    name="${name#./}"
    echo "$name"
}

# Resolve the benchmark file path for a given target.
# VFXForth and SwiftForth use Forth-specific adaptations under shootout/<forth>/.
resolve_bench_file() {
    local cmd_name="$1"
    local bm="$2"
    case "$cmd_name" in
        run-vfxforth|vfxforth)
            if [ -f "shootout/vfxforth/${bm}" ]; then
                echo "shootout/vfxforth/${bm}"
            else
                echo "shootout/${bm}"
            fi
            ;;
        run-swiftforth|swiftforth)
            if [ -f "shootout/swiftforth/${bm}" ]; then
                echo "shootout/swiftforth/${bm}"
            else
                echo "shootout/${bm}"
            fi
            ;;
        *)
            echo "shootout/${bm}"
            ;;
    esac
}

# Write log file with metadata headers compatible with run_shootout.load_log.
write_log() {
    local log_file="$1"
    local bench_name="$2"
    local config="$3"
    local iteration="$4"
    local cmd="$5"
    local stdout="$6"
    local stderr="$7"
    local rc="$8"
    local timed_out="$9"

    local status="ok"
    if [ "$rc" -ne 0 ] || [ "$timed_out" = "true" ]; then
        status="error"
    fi

    mkdir -p "$(dirname "$log_file")"
    {
        echo "# benchmark: ${bench_name}"
        echo "# config: ${config}"
        echo "# iteration: ${iteration}"
        echo "# command: ${cmd}"
        echo "# status: ${status}"
        echo "# return code: ${rc}"
        if [ "$timed_out" = "true" ]; then
            echo "# timed_out: true"
        fi
        echo "# --- stdout ---"
        echo "$stdout"
        if [ -n "$stderr" ]; then
            echo "# --- stderr ---"
            echo "$stderr"
        fi
    } > "$log_file"
}

# Function to run a single benchmark with optional memory profiling
run_benchmark() {
    local cmd="$1"
    local bench_file="$2"
    local log_file="$3"
    local mem_log_file="$4"
    local bench_name="$5"
    local config="$6"
    local iteration="$7"

    local stdout=""
    local stderr=""
    local rc=0
    local timed_out="false"

    local run_cmd=()
    if [ -n "${PIN:-}" ]; then
        run_cmd+=("taskset" "-c" "$PIN")
    fi
    run_cmd+=(${cmd})

    if [ "$ENABLE_MEMORY" = true ]; then
        # Use /usr/bin/time -v to capture memory metrics; timeout prevents hangs.
        local tmp_out
        tmp_out=$(mktemp)
        local tmp_err
        tmp_err=$(mktemp)
        timeout "${BENCH_TIMEOUT}" /usr/bin/time -v "${run_cmd[@]}" "${bench_file}" > "$tmp_out" 2> "$tmp_err"
        rc=$?
        if [ $rc -eq 124 ]; then
            echo "TIMEOUT after ${BENCH_TIMEOUT}s" >> "$tmp_out"
            timed_out="true"
        fi
        stdout=$(cat "$tmp_out")
        stderr=$(cat "$tmp_err")
        # Extract memory info and append to mem_log_file, keep stderr output
        grep -E "(Maximum resident set size|Minor|Major|Voluntary|Involuntary|Page size|Elapsed)" "$tmp_err" > "${mem_log_file}" 2>/dev/null || true
        rm -f "$tmp_out" "$tmp_err"
    else
        local tmp_out
        tmp_out=$(mktemp)
        local tmp_err
        tmp_err=$(mktemp)
        timeout "${BENCH_TIMEOUT}" "${run_cmd[@]}" "${bench_file}" > "$tmp_out" 2> "$tmp_err"
        rc=$?
        if [ $rc -eq 124 ]; then
            echo "TIMEOUT after ${BENCH_TIMEOUT}s" >> "$tmp_out"
            timed_out="true"
        fi
        stdout=$(cat "$tmp_out")
        stderr=$(cat "$tmp_err")
        rm -f "$tmp_out" "$tmp_err"
    fi

    write_log "$log_file" "$bench_name" "$config" "$iteration" "${cmd} ${bench_file}" "$stdout" "$stderr" "$rc" "$timed_out"
}

# Standard benchmark mode
run_standard_benchmarks() {
    for bm in "${BENCHMARKS[@]}"; do
        for cmd in "${COMMANDS[@]}"; do
            cmd_name=$(config_name "${cmd}")
            bench_file=$(resolve_bench_file "${cmd_name}" "${bm}")
            bench_name="shootout/${bm}"

            echo "[Target: ${cmd_name} | Bench: ${bm}]"

            # 1. Warmup Phase (JIT Training)
            echo "  - Warming up (${WARMUP_RUNS} runs)..."
            for i in $(seq 1 $WARMUP_RUNS); do
                run_benchmark "${cmd}" "${SHOOTOUT_DIR}/${bm}" \
                    "${LOG_DIR}/${bm}_${cmd_name}_warmup_${i}.log" \
                    "${LOG_DIR}/${bm}_${cmd_name}_warmup_${i}.mem" \
                    "${bench_name}" "${cmd_name}" "$i"
            done

            # 2. Measurement Phase (Steady State)
            echo "  - Measuring (${MEASURE_RUNS} runs)..."
            for i in $(seq 1 $MEASURE_RUNS); do
                run_benchmark "${cmd}" "${SHOOTOUT_DIR}/${bm}" \
                    "${LOG_DIR}/${bm}_${cmd_name}_run_${i}.log" \
                    "${LOG_DIR}/${bm}_${cmd_name}_run_${i}.mem" \
                    "${bench_name}" "${cmd_name}" "$i"
            done
        done
    done
}

# Curve benchmark mode for warmup analysis
run_curve_benchmarks() {
    echo "Running curve benchmarks for warmup analysis..."
    for bm in "${CURVE_BENCHMARKS[@]}"; do
        for cmd in "${COMMANDS[@]}"; do
            cmd_name=$(config_name "${cmd}")
            curve_file="${SHOOTOUT_DIR}/curve/${bm}"

            if [ ! -f "${curve_file}" ]; then
                echo "  [Skip] Curve benchmark not found: ${curve_file}"
                continue
            fi

            echo "[Curve: ${cmd_name} | Bench: ${bm}]"
            bench_name="shootout/curve/${bm}"

            # Run curve benchmark (single run, outputs per-iteration times)
            run_benchmark "${cmd}" "${curve_file}" \
                "${LOG_DIR}/${bm}_${cmd_name}_curve.log" \
                "${LOG_DIR}/${bm}_${cmd_name}_curve.mem" \
                "${bench_name}" "${cmd_name}" "1"
        done
    done
}

# Derive a comma-separated engine list for run_appbench.py from COMMANDS.
appbench_engines() {
    local engines=""
    for cmd in "${COMMANDS[@]}"; do
        if [ -n "$engines" ]; then
            engines="${engines},"
        fi
        engines="${engines}$(config_name "$cmd")"
    done
    echo "$engines"
}

# Run the appbench-1.4 suite via run_appbench.py steady mode.
run_appbench_suite() {
    echo ""
    echo "Running appbench-1.4 suite..."

    local engines
    engines=$(appbench_engines)

    local appbench_cmd=(
        "${PYTHON3}" "./benchmark/run_appbench.py" "steady"
        "--engines" $(echo "$engines" | tr ',' ' ')
        "--iterations" "$APPBENCH_ITERATIONS"
        "--timeout" "$APPBENCH_TIMEOUT"
        "--output" "$LOG_DIR"
        "--pdf" "$LOG_DIR/appbench_warmup_curves.pdf"
    )

    if [ -n "${PIN:-}" ]; then
        appbench_cmd+=("--pin" "$PIN")
    fi

    echo "Command: ${appbench_cmd[*]}"
    set +e
    "${appbench_cmd[@]}" 2>&1 | tee "$LOG_DIR/run_appbench_steady.log"
    local rc=${PIPESTATUS[0]}
    set -e
    if [ $rc -ne 0 ]; then
        echo "WARNING: appbench suite exited with code $rc" >&2
    fi
}

# Execute based on selected suites
if [ "$RUN_SHOOTOUT" = true ]; then
    case $MODE in
        standard)
            run_standard_benchmarks
            ;;
        curve)
            run_curve_benchmarks
            ;;
        all)
            run_standard_benchmarks
            echo ""
            run_curve_benchmarks
            ;;
    esac
fi

if [ "$RUN_APPBENCH" = true ]; then
    run_appbench_suite
fi

echo ""
echo "Done. All logs saved to ${LOG_DIR}"
