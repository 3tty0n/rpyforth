\ Word count benchmark for JIT analysis

4096 constant MAXREAD
50 constant ITERATIONS

variable nn
variable nw
variable nc
variable in_word

10 constant nl_ch
9  constant tab_ch
32 constant space_ch

create buff MAXREAD allot

: scanbuff  ( n -- )
  dup nc +!
  buff + buff
  ?do
    i c@
    case
      nl_ch    of  0 in_word !  1 nn +!  endof
      tab_ch   of  0 in_word !  endof
      space_ch of  0 in_word !  endof
      in_word @ 0= if  1 in_word !  1 nw +!  then
    endcase
  loop
;

: wc-file  ( fid -- )
  >r
  0 nn !  0 nw !  0 nc !  0 in_word !
  begin
    buff MAXREAD r@ read-file throw dup
  while
    scanbuff
  repeat
  drop r> drop
;

: wc-bench  ( -- )
  s" shootout/data/wc.txt" r/o open-file throw >r
  r@ wc-file
  r> close-file throw ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    wc-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
