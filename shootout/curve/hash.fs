\ Hash lookup benchmark for JIT analysis

10000 constant NUM
50 constant ITERATIONS

wordlist constant x

: build  ( -- )
  get-current x set-current
  base @ hex
  NUM 0 do
    i 0 <# #s #> nextname i constant
  loop
  base ! set-current ;

: countdecs  ( -- n )
  0
  NUM 0 do
    i 0 <# #s #> x search-wordlist if  drop 1+  then
  loop ;

build

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    countdecs drop
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
