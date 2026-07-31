"""Standard-library words defined in Forth and loaded at startup.

Words derivable from existing primitives live here rather than as RPython
primitives; straight-line ones are inlined at their call sites. The text is
embedded as a constant so the binary needs no external file."""

PRELUDE = """\
: TRUE -1 ;
: FALSE 0 ;
: TUCK SWAP OVER ;
: U> SWAP U< ;
: WITHIN OVER - >R - R> U< ;
: ERASE 0 FILL ;
: /STRING DUP >R - SWAP R> + SWAP ;
: BLANK BL FILL ;
: -TRAILING BEGIN dup WHILE 2dup + 1- c@ bl = IF 1- ELSE EXIT THEN REPEAT ;
: DNEGATE 0 0 2SWAP D- ;
: D0= OR 0= ;
: D2* 2DUP D+ ;
: CELL- 1 CELLS - ;
: M+ S>D D+ ;
: D= D- D0= ;
: D<> D= 0= ;
: D< D- NIP 0< ;
: D> 2SWAP D< ;
: D0< NIP 0< ;
CREATE pad-buf 512 ALLOT
: PAD pad-buf ;
: NALIGNED 1- TUCK + SWAP INVERT AND ;
1 CHARS 0 2CONSTANT struct
1 ALIGNED 1 CELLS 2CONSTANT cell%
1 CHARS 1 CHARS 2CONSTANT char%
1 FLOATS 1 FLOATS 2CONSTANT float%
: %alignment DROP ;
: %size NIP ;
: %align DROP NALIGNED DROP ;
: %allot NIP ALLOT ;
: %alloc NIP ALLOCATE THROW ;
: RDROP R> DROP ;
: BOUNDS OVER + SWAP ;
: PERFORM @ EXECUTE ;
: SEE ' xt>string type space ;
: ON -1 SWAP ! ;
: OFF 0 SWAP ! ;
: D>S DROP ;
: NOOP ;
: WORDS ;
: LOOK -1 ;
: $? 0 ;
: SYSTEM 2DROP 0 ;
: EKEY 0 ;
: EKEY>CHAR TRUE ;
: EKEY? FALSE ;
0 CONSTANT k-left    1 CONSTANT k-right   2 CONSTANT k-up      3 CONSTANT k-down
4 CONSTANT k-home    5 CONSTANT k-end     6 CONSTANT k-prior   7 CONSTANT k-next
8 CONSTANT k-insert  9 CONSTANT k-delete
10 CONSTANT K1  11 CONSTANT K2  12 CONSTANT K3  13 CONSTANT K4
14 CONSTANT K5  15 CONSTANT K6  16 CONSTANT K7  17 CONSTANT K8
18 CONSTANT K9  19 CONSTANT K10 20 CONSTANT K11 21 CONSTANT K12
"""


def load_prelude(outer):
    for line in PRELUDE.split("\n"):
        outer.interpret_line(line)
