\ Composite benchmark for JIT analysis

6     constant ACK-N
35    constant FIB-N
8192  constant SIEVE-SIZE
200   constant SIEVE-ITERS
20    constant NL-N
512   constant ARY-N
100   constant ARY-REPS
3500  constant HEAP-N
50    constant ITERATIONS

: ack  recursive
    dup 0=
    if   drop 1+
    else swap dup 0=
         if   drop 1- 1 swap ack
         else 1- over 1- rot rot swap ack swap ack
         then
    then ;

: fib  recursive
    dup 2 <
    if   drop 1
    else dup 2 - fib swap 1 - fib +
    then ;

create Flags SIEVE-SIZE allot
Flags SIEVE-SIZE + constant EndFlags
: flagmults  do 0 i c! dup +loop ;
: primes
    Flags SIEVE-SIZE 1 fill
    0 2
    EndFlags Flags
    do
        i c@
        if  dup i + dup EndFlags <
            if   EndFlags swap flagmults
            else drop
            then
            swap 1+ swap
        then
        1+
    loop
    drop ;
: sieve-bench  0 SIEVE-ITERS 0 do primes nip loop ;

: nestedloops
    NL-N 0 do NL-N 0 do NL-N 0 do NL-N 0 do NL-N 0 do NL-N 0 do
        1+
    loop loop loop loop loop loop ;

variable X
ARY-N cells allocate drop X !
variable Y
ARY-N cells allocate drop Y !
: ary
    ARY-N 0 do
        1 i + i cells X @ + !
    loop
    ARY-REPS 0 do
        ARY-N 0 do
            i cells Y @ + dup @ i cells X @ + @ + swap !
        loop
    loop ;

139968 constant IM
  3877 constant IA
 29573 constant IC
variable SEED
42 SEED !
: gen-random  ( f -- f )
    SEED @ IA * IC + IM mod dup SEED !
    s>f IM s>f f/ f* ;
variable heap-base
variable heap-l
variable heap-ir
fvariable rra
: a@   ( i -- f )   heap-base @ swap floats + f@ ;
: a!   ( f i -- )   heap-base @ swap floats + f! ;
: set-rra    ( i -- )   a@ rra f! ;
: store-rra  ( i -- )   rra f@ a! ;
: heap-sort  ( n -- )
    dup heap-ir !
    2/ 1+ heap-l !
    BEGIN
        heap-l @ 1 > IF
            heap-l @ 1- dup heap-l !
            set-rra
        ELSE
            heap-ir @ set-rra
            1 a@
            heap-ir @ a!
            heap-ir @ 1- dup heap-ir !
            dup 1 = IF
                drop
                1 store-rra
                EXIT
            THEN
            drop
            1 heap-l !
        THEN
        heap-l @ 2*
        BEGIN
            dup heap-ir @ <=
        WHILE
            dup heap-ir @ < IF
                dup a@
                dup 1+ a@
                f< IF 1+ THEN
            THEN
            rra f@ dup a@ f< IF
                dup a@
                heap-l @ a!
                dup heap-l !
                2*
            ELSE
                drop heap-ir @ 1+
            THEN
        REPEAT
        drop
        heap-l @ store-rra
    AGAIN ;
: heap-run  ( -- n )
    HEAP-N 1+ floats allocate throw
    heap-base !
    HEAP-N 0 DO
        1e gen-random
        heap-base @ I 1+ floats + f!
    LOOP
    HEAP-N heap-sort
    heap-base @ HEAP-N floats + f@
    1e6 f* f>d drop ;

: composite-bench  ( -- sum )
    0
    ACK-N 3 ack +
    FIB-N fib +
    sieve-bench +
    0 nestedloops +
    ary Y @ @ +
    heap-run +
;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    composite-bench drop
    utime 2R> d-
    i . ." ," d. cr
  loop ;

10 set-precision
run-benchmark
bye
