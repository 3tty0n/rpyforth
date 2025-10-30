# Coverage (30.10.2025)

 - Derived the gforth 2012 CORE wordset (132 words) from the Pygments Forth lexer plus manual additions for `'`, `(`, .`"`, `[`, `]`, `+!`, `*`/, `*/MOD`;
   only 34 of those words are currently implemented (98 remain missing) beyond the existing
   basics like `0=`, `DUP`, `+/-/*`, `!/@`, base selectors, the pictured numeric output quartet, and the minimal branch/loop primitives.

## Unimplemented Words

 - Control & compilation: `', (, +LOOP, DOES>, IMMEDIATE, J, LEAVE, LITERAL, POSTPONE, QUIT, RECURSE, STATE, UNLOOP, [, ['], [CHAR], ]`.
 - Stack & return stack: `?DUP, 2DROP, 2DUP, 2OVER, 2SWAP, >R, DEPTH, R>, R@, ROT`.
 - Arithmetic & comparison: `*/, */MOD, +!, /, /MOD, 1+, 1-, 2*, 2/, <, >, ABS, AND, FM/MOD, INVERT, LSHIFT, M*, MAX, MIN, MOD, NEGATE, OR, RSHIFT, S>D, SIGN, SM/REM, U., U<, UM*, UM/MOD, XOR`.
 - Memory & data space: `,, 2!, 2@, ALLOT, ALIGN, ALIGNED, BASE, BL, C!, C,, C@, CELL+, CELLS, CHAR, CHAR+, CHARS, COUNT, FILL, HERE, MOVE`.
 - Dictionary & environment: `>BODY, >IN, >NUMBER, ABORT, ABORT", CREATE, ENVIRONMENT?, EVALUATE, EXECUTE, FIND, WORD`.
 - I/O & text: `.", S", ACCEPT, CR, EMIT, KEY, SOURCE, SPACE, SPACES`.
