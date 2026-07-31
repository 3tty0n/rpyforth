\ String concatenation benchmark for JIT analysis

10000 constant NUM
50 constant ITERATIONS

variable hsiz    32                       hsiz !
variable hbuf    hsiz @ allocate throw    hbuf !
variable hoff    0                        hoff !

: STUFF s" hello." ;

: strcat  ( c-addr u -- )
  dup hsiz @ hoff @ - >
  if
    hsiz @ 2* hsiz !
    hbuf @ hsiz @ resize throw hbuf !
  then
  swap over
  hbuf @ hoff @ + swap cmove>
  hoff @ + hoff !
;

: strcat-bench  ( -- )
  0 hoff !
  NUM 0 do  STUFF strcat  loop
;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    strcat-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
