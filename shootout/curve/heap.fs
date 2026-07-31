\ Heapsort benchmark for JIT analysis

3500 constant NUM
50 constant ITERATIONS

139968 constant RNG-IM
 3877 constant RNG-IA
29573 constant RNG-IC

variable SEED
variable heap-base
variable heap-l
variable heap-ir
variable heap-done
variable heap-n
fvariable rra

: gen-random  ( f -- f )
  SEED @ RNG-IA * RNG-IC + RNG-IM mod dup SEED !
  s>f RNG-IM s>f f/ f* ;

: heap@   ( i -- f )
  heap-base @ swap floats + f@ ;

: heap!   ( f i -- )
  heap-base @ swap floats + f! ;

: set-rra    ( i -- )
  heap@ rra f! ;

: store-rra  ( i -- )
  rra f@ heap! ;

: heap-sort  ( n -- )
  heap-n !
  0 heap-done !
  heap-n @ dup heap-ir !
  2/ 1+ heap-l !
  BEGIN
    heap-done @ 0= WHILE
      heap-l @ 1 > IF
        heap-l @ 1- dup heap-l !
        set-rra
      ELSE
        heap-ir @ set-rra
        1 heap@
        heap-ir @ heap!
        heap-ir @ 1- dup heap-ir !
        dup 1 = IF
          drop
          1 store-rra
          1 heap-done !
        ELSE
          drop
          1 heap-l !
        THEN
      THEN

      heap-done @ 0= IF
        heap-l @ 2*
        BEGIN
          dup heap-ir @ <=
        WHILE
          dup heap-ir @ < IF
            dup heap@
            dup 1+ heap@
            f< IF
              1+
            THEN
          THEN

          rra f@ dup heap@ f< IF
            dup heap@
            heap-l @ heap!
            dup heap-l !
            2*
          ELSE
            drop heap-ir @ 1+
          THEN
        REPEAT
        drop
        heap-l @ store-rra
      THEN
    REPEAT ;

: heap-bench ( -- )
  42 SEED !
  NUM 0 DO
    1e gen-random
    heap-base @ I 1+ floats + f!
  LOOP
  NUM heap-sort ;

: run-benchmark
  NUM 1+ floats allocate throw heap-base !
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    heap-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

10 set-precision
run-benchmark
bye
