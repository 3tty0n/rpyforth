#!/bin/bash
# Run a benchmark file on the locally installed SwiftForth.
# Preserves the caller's working directory so that relative includes resolve
# correctly (e.g. for appbench programs that load local data files).
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SF_BIN="${REPO_ROOT}/swiftforth/SwiftForth/bin/linux/sf64"

if [ ! -x "$SF_BIN" ]; then
    echo "swiftforth.sh: SwiftForth not installed; run: make setup-swiftforth" >&2
    exit 127
fi

BENCH_FILE="${1:?usage: swiftforth.sh <benchmark.fs>}"

# Resolve the benchmark file against the caller's cwd for relative paths,
# otherwise use it as-is (absolute paths are already fine).
if [ -f "$BENCH_FILE" ]; then
    BENCH_FILE="$(cd "$(dirname "$BENCH_FILE")" && pwd)/$(basename "$BENCH_FILE")"
fi

# Prefer a hand-tuned shootout adaptation when present; otherwise auto-adapt
# gforth shootout sources. Appbench / other drivers are run as-is (adapting
# them injects shootout-only preambles and breaks absolute includes).
SF_BENCH_FILE="${BENCH_FILE/${REPO_ROOT}\/shootout\//${REPO_ROOT}/shootout/swiftforth/}"

is_shootout=0
case "$BENCH_FILE" in
    */shootout/*) is_shootout=1 ;;
esac

TMP_DRIVER=""
cleanup() {
    if [ -n "$TMP_DRIVER" ] && [ -f "$TMP_DRIVER" ]; then
        rm -f "$TMP_DRIVER"
    fi
}
trap cleanup EXIT

if [ -f "$SF_BENCH_FILE" ] && [ "$SF_BENCH_FILE" != "$BENCH_FILE" ]; then
    DRIVER="$SF_BENCH_FILE"
elif [ "$is_shootout" -eq 1 ]; then
    TMP_DRIVER="$(mktemp "${TMPDIR:-/tmp}/sfshootout.XXXXXX.fs")"
    python3 "${REPO_ROOT}/benchmark/adapt_shootout_for_engine.py" swiftforth "$BENCH_FILE" > "$TMP_DRIVER"
    DRIVER="$TMP_DRIVER"
else
    DRIVER="$BENCH_FILE"
fi

start_ns=$(date +%s%N)
set +e
OUT=$("$SF_BIN" "include ${DRIVER}" 2>/tmp/swiftforth-$$.err)
rc=$?
set -e
end_ns=$(date +%s%N)

if [ -s /tmp/swiftforth-$$.err ]; then
    cat /tmp/swiftforth-$$.err >&2 || true
fi
rm -f /tmp/swiftforth-$$.err

printf '%s\n' "$OUT"
if [ "$rc" -ne 0 ]; then
    exit "$rc"
fi
if printf '%s' "$OUT" | grep -qE 'Error:|Undefined|>>>'; then
    exit 1
fi
elapsed_us=$(( (end_ns - start_ns) / 1000 ))
echo "Elapsed: ${elapsed_us} usec"
