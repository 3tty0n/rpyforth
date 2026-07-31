\ Random Number Generator benchmark for JIT analysis

600000 constant NUM
50 constant ITERATIONS

139968 constant IM
  3877 constant IA
 29573 constant IC

variable SEED

: gen-random  ( fmax -- fr )
  SEED @ IA * IC + IM mod dup SEED !
  s>f IM s>f f/ f* ;

: rng-bench ( -- )
  42 SEED !
  0e
  NUM 0 do
    fdrop 100e gen-random
  loop
  fdrop ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    rng-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
