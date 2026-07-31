\ Statistical moments benchmark for JIT analysis

4096 constant max-nums
1024 constant max-line
50 constant ITERATIONS

create nums max-nums floats allot
variable #nums
create line max-line 2 + allot

: push-int  ( n -- )
  #nums @ max-nums >= if drop exit then
  s>f  nums #nums @ floats + f!  1 #nums +!
;

: parse-int  ( c-addr u -- n )
  0 0 2swap >number 2drop d>s ;

: input-ints  ( fid -- )
  >r
  begin
    line max-line r@ read-line throw
  while
    line swap parse-int push-int
  repeat
  r> drop drop
;

: mean  ( -- r )
  0e
  #nums @ 0 do  nums i floats + f@ f+  loop
  #nums @ s>f f/
;

: variance  ( rmean -- rvar )
  0e
  #nums @ 0 do
    nums i floats + f@ fover f- fdup f* f+
  loop
  fswap fdrop
  #nums @ 1- s>f f/
;

: moments-bench  ( -- )
  0 #nums !
  s" shootout/data/moments.txt" r/o open-file throw >r
  r@ input-ints
  r> close-file throw
  mean variance fdrop ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    moments-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
