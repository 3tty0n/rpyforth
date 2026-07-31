\ Object instantiation benchmark for JIT analysis

100000 constant NUM
50 constant ITERATIONS

variable t-state
: t-construct  ( f -- )  t-state ! ;

variable n-state
variable n-counter
variable n-max
: n-construct  ( max f -- )
  n-state !  n-max !  0 n-counter ! ;

: objinst-bench  ( -- )
  NUM 0 do  -1 t-construct  loop
  NUM 0 do  3 -1 n-construct  loop ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    objinst-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
