#!/bin/bash
# Run a benchmark file on the locally installed VFXForth.
# Preserves the caller's working directory so that relative includes resolve
# correctly (e.g. for appbench programs that load local data files).
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VFX_BIN="${REPO_ROOT}/vfxforth/_install/bin/vfxforth"

if [ ! -x "$VFX_BIN" ]; then
    echo "vfxforth.sh: VFXForth not installed; run: make setup-vfxforth" >&2
    exit 127
fi

BENCH_FILE="${1:?usage: vfxforth.sh <benchmark.fs>}"

# Resolve the benchmark file against the caller's cwd for relative paths,
# otherwise use it as-is (absolute paths are already fine).
if [ -f "$BENCH_FILE" ]; then
    BENCH_FILE="$(cd "$(dirname "$BENCH_FILE")" && pwd)/$(basename "$BENCH_FILE")"
fi

# Prefer a hand-tuned shootout adaptation when present; otherwise auto-adapt
# gforth shootout sources. Appbench / other drivers are run as-is.
VFX_BENCH_FILE="${BENCH_FILE/${REPO_ROOT}\/shootout\//${REPO_ROOT}/shootout/vfxforth/}"

is_shootout=0
case "$BENCH_FILE" in
    */shootout/*) is_shootout=1 ;;
esac

TMP_DRIVER=""
TMP_OUT=""
TMP_ERR=""
VFX_PID=""
cleanup() {
    if [ -n "$VFX_PID" ] && kill -0 "$VFX_PID" 2>/dev/null; then
        kill -9 "$VFX_PID" 2>/dev/null || true
    fi
    if [ -n "$TMP_DRIVER" ] && [ -f "$TMP_DRIVER" ]; then
        rm -f "$TMP_DRIVER"
    fi
    if [ -n "$TMP_OUT" ] && [ -f "$TMP_OUT" ]; then
        rm -f "$TMP_OUT"
    fi
    if [ -n "$TMP_ERR" ] && [ -f "$TMP_ERR" ]; then
        rm -f "$TMP_ERR"
    fi
}
trap cleanup EXIT

if [ -f "$VFX_BENCH_FILE" ] && [ "$VFX_BENCH_FILE" != "$BENCH_FILE" ]; then
    DRIVER="$VFX_BENCH_FILE"
elif [ "$is_shootout" -eq 1 ]; then
    TMP_DRIVER="$(mktemp "${TMPDIR:-/tmp}/vfxshootout.XXXXXX.fs")"
    python3 "${REPO_ROOT}/benchmark/adapt_shootout_for_engine.py" vfxforth "$BENCH_FILE" > "$TMP_DRIVER"
    DRIVER="$TMP_DRIVER"
else
    DRIVER="$BENCH_FILE"
fi

# Write VFX stdout to a temp file (not a pipe). Lexex prints huge progress
# dot streams; capturing via $(cmd | sed) deadlocks once the pipe fills.
TMP_OUT="$(mktemp "${TMPDIR:-/tmp}/vfxout.XXXXXX")"
TMP_ERR="$(mktemp "${TMPDIR:-/tmp}/vfxerr.XXXXXX")"

start_ns=$(date +%s%N)
set +e
EDITOR=: VISUAL=: VFX_BATCH_MODE=1 \
    "$VFX_BIN" "0 to EditOnError?  include ${DRIVER}" \
    < /dev/null >"$TMP_OUT" 2>"$TMP_ERR" &
VFX_PID=$!

# VFX's SIGSEGV handler prompts "Press E to exit" and ignores /dev/null,
# hanging forever. Watch the log and kill if that prompt appears.
rc=0
while kill -0 "$VFX_PID" 2>/dev/null; do
    if grep -q 'Press E to exit' "$TMP_OUT" 2>/dev/null; then
        echo "vfxforth.sh: VFX stuck on 'Press E to exit' (SIGSEGV/error); killing" >&2
        kill -9 "$VFX_PID" 2>/dev/null || true
        wait "$VFX_PID" 2>/dev/null || true
        VFX_PID=""
        rc=1
        break
    fi
    sleep 0.15
done
if [ -n "$VFX_PID" ]; then
    wait "$VFX_PID"
    rc=$?
    VFX_PID=""
fi
set -e
end_ns=$(date +%s%N)

if [ -s "$TMP_ERR" ]; then
    sed -e '/^Vim:/d' "$TMP_ERR" >&2 || true
fi

# Strip banner / include noise. Collapse lexex progress-dot runs for harness pipes.
sed -e '/^VFX Forth /d' \
    -e '/^© /d' \
    -e '/^[[:space:]]*$/d' \
    -e 's/^Including [^[:cntrl:]]*//' \
    -e '/ is redefined/d' \
    -e '/ contains a reference to /d' \
    -e '/^DATA STACK$/d' \
    -e '/^empty stack$/d' \
    -e '/^Press E to exit/d' \
    -e '/^Signal number /d' \
    -e '/^CS=/d' \
    -e 's/\.\{10,\}/.../g' \
    "$TMP_OUT"

if [ "$rc" -ne 0 ]; then
    exit "$rc"
fi
if grep -qE 'Err#|ERROR on command line|SIGSEGV|Undefined word|Press E to exit|Control structure mismatch' "$TMP_OUT"; then
    exit 1
fi
elapsed_us=$(( (end_ns - start_ns) / 1000 ))
echo "Elapsed: ${elapsed_us} usec"
