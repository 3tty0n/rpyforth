\ Method Calls benchmark for JIT analysis

450000 constant NUM
50 constant ITERATIONS

variable t-state
: t-activate  ( -- )    t-state @ 0= t-state ! ;

variable n-state
variable n-counter
variable n-max
: n-activate  ( -- )
  n-counter @ 1+ n-counter !
  n-counter @ n-max @ = if
    n-state @ 0= n-state !
    0 n-counter !
  then ;

: methcall-bench ( -- )
  -1 t-state !
  NUM 0 do  t-activate  loop
  -1 n-state !  0 n-counter !  3 n-max !
  NUM 0 do  n-activate  loop ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    methcall-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
