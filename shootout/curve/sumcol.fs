\ Sum column benchmark for JIT analysis

256 constant max-line
50 constant ITERATIONS
create line-buffer max-line 1+ allot

: parse-int  ( c-addr u -- n )
  0 0 2swap >number 2drop d>s ;

: sumcol-file  ( fid -- n )
  >r 0
  begin
    line-buffer max-line r@ read-line throw
  while
    >r line-buffer r> parse-int +
  repeat
  r> drop drop
;

: sumcol-bench  ( -- )
  s" shootout/data/sumcol.txt" r/o open-file throw >r
  r@ sumcol-file drop
  r> close-file throw ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    sumcol-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
