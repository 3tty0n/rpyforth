\ Reverse file benchmark for JIT analysis

4096 constant max-lines
256 constant max-len
50 constant ITERATIONS

create lines max-lines max-len * allot
create lens max-lines cells allot
variable nlines
create line-buf max-len 1+ allot
variable infid

: save-line  ( c-addr u -- )
  nlines @ max-lines >= if 2drop exit then
  dup nlines @ cells lens + !
  lines nlines @ max-len * + swap move
  1 nlines +! ;

: reverse-walk  ( -- n )
  0 nlines @ 0 ?do
    i cells lens + @ +
  loop ;

: reversefile-bench  ( -- )
  0 nlines !
  s" shootout/data/reversefile.txt" r/o open-file throw infid !
  begin
    line-buf max-len infid @ read-line throw
  while
    >r line-buf r> save-line
  repeat
  drop infid @ close-file throw
  reverse-walk drop ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    reversefile-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
