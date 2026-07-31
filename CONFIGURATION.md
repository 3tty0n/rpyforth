# RPyForth configuration

`rpyforth/config.py` is the single boundary between external environment
variables and interpreter state. It reads the environment once, during import
(and therefore during RPython translation), validates or normalizes the values,
and exposes simple string, integer, and boolean constants. Other production
modules must not read `RPYFORTH_*` variables directly.

## Settings

| Environment variable | Default | Resolved constant | Purpose |
|---|---:|---|---|
| `RPYFORTH_STACK_LAYOUT` | `plain` | `STACK_LAYOUT` | Selects `plain`, `fragment`, `frame-only`, `ntop2/4/8/16`, or `fragment-float` |
| `RPYFORTH_FRAME_SIZE` | `8` | `FRAME_SIZE` | Cached frame cells; values above 64 are clamped to 64 |
| `RPYFORTH_VIRTUALIZE` | unset | `USE_VIRTUALIZATION` | Enables the original whole-stack virtualizable mode for the plain layout |
| `RPYFORTH_ALLOC_MB` | automatic | `ALLOC_MB` | Size of the separate `ALLOCATE` region in MiB |
| `RPYFORTH_EXE_NAME` | `rpyforth-%(backend)s` | `EXE_NAME` | Output name used by the RPython translation driver |

The stack-fragment layouts already define their own virtualizable state, so
they take precedence over `RPYFORTH_VIRTUALIZE`. Internally,
`USE_STACK_FRAGMENT` and `USE_VIRTUALIZATION` are therefore mutually exclusive.

The old `RPYFORTH_STACK_FRAGMENT`, `RPYFORTH_FRAME_ONLY`, `RPYFORTH_NTOP`, and
`RPYFORTH_FLOAT_FRAGMENT` variables remain supported. If
`RPYFORTH_STACK_LAYOUT` is set, it takes precedence over all four.

Malformed numeric values preserve the historical behavior: an invalid or
non-positive frame size uses 8, a frame size above 64 is clamped, and an invalid
or non-positive allocation size selects the automatic translated/untranslated
default.

## Inspecting a configuration

Run the configuration module with the same environment intended for the build:

```sh
RPYFORTH_STACK_LAYOUT=fragment \
RPYFORTH_FRAME_SIZE=8 \
RPYFORTH_ALLOC_MB=256 \
PYTHONPATH=. python -m rpyforth.config
```

It prints the fully resolved values:

```text
stack_layout=fragment
frame_size=8
effective_ntop=2
virtualize=no
alloc_mb=256
exe_name=rpyforth-%(backend)s
```
