\ Hello World benchmark for JIT analysis

50 constant ITERATIONS

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    1000 0 do loop
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
