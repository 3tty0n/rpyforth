# RPyForth

This repository is a publication snapshot prepared for the July 2026 PyPy
blog post. It is not a continuously updated development branch.

A Forth interpreter written in [RPython](https://rpython.readthedocs.io/). It is
translated with the PyPy toolchain into a native binary with a meta-tracing
JIT compiler.

The interpreter is written as an indirect-threaded VM. The key runtime structure is
a three-area-layout *metastack*: the top data-stack cells live in registers as known as the stack-caching technique,
the next cells in a small JIT-virtualizable frame array, and everything deeper in one
shared spill area.

The repository also contains the benchmark infrastructure used to compare against gforth,
gforth-fast, VFX Forth, and SwiftForth on the shootout and appbench-1.4 suites.

## Requirements

- Linux x86-64
- `python3` (bootstrap and benchmark harnesses)
- `make`, `gcc`, and the usual C toolchain (for RPython translation)

The comparison engines (gforth/gforth-fast, VFX Forth, SwiftForth) are only
needed for benchmarks, not for building or using RPyForth.

## License

The original RPyForth implementation, tests, and benchmark infrastructure are
available under the [MIT License](LICENSE), unless a file states otherwise.
Third-party benchmark materials retain their own terms. Some vendored benchmark
sources have provenance information but no redistribution grant; the MIT
License does not apply to them. See [THIRD_PARTY.md](THIRD_PARTY.md) before
redistributing the benchmark trees.

## Building

```sh
make build-jit-stkfrag
```

The first build downloads a `pypy2` binary into `_pypy_binary/`,
clones the PyPy source into `pypy/`, and then translates the interpreter
(a few minutes on a typical machine).

The resultis a self-contained native binary:

```sh
./rpyforth-c-stkfrag program.fs
```

The translation-time stack representation has one canonical setting:

```sh
RPYFORTH_STACK_LAYOUT=plain
RPYFORTH_STACK_LAYOUT=fragment       # t0/t1 + frame + spill
RPYFORTH_STACK_LAYOUT=frame-only
RPYFORTH_STACK_LAYOUT=ntop4          # also ntop2, ntop8, ntop16
RPYFORTH_STACK_LAYOUT=fragment-float
```

See [`CONFIGURATION.md`](CONFIGURATION.md) for the complete setting
list, defaults, and precedence.

## Running the tests

The suite runs untranslated on the PyPy2 interpreter. Run it in both stack
configurations:

```sh
make test
RPYFORTH_STACK_LAYOUT=fragment PYTHONPATH=. ./_pypy_binary/bin/python2 ./pypy/pytest.py rpyforth/test -q
```

`make test-factor` runs the rpyfactor suite.

## Benchmarks

Comparison engines are the follows:

- gforth-fast 0.7.9_20260610 (Makefile pins the snapshot URL)
- VFX Forth 64 5.43 [build 0199] (2023-11-09, Linux x64; download URL is unversioned, pinned by SHA256)
- SwiftForth x64-Linux 4.1.8 (05-Jul-2026, evaluation; download URL is unversioned, pinned by SHA256)

Everything runs from the project root through `make`.

You can run the two benchmark suites as follows:

```sh
make bench-shootout        # shootout micro-benchmarks -> compare.pdf
make bench-shootout-curve  # shootout warm-up curves   -> warmup.pdf
make bench-appbench        # appbench cold functional + performance grid -> appbench.pdf
make bench-appbench-curve  # appbench warm steady-state + warm-up curves -> appbench-curve.pdf
```
