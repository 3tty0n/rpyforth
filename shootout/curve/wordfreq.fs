\ Word frequency benchmark for JIT analysis

1024 constant max-line
50 constant ITERATIONS
create line max-line 2 + allot
create namepad 256 allot
variable wlen
variable freq-wl
create word-pointers 30000 cells allot
variable endwp

: count-word  ( addr u -- )
  dup 0= if 2drop exit then
  2dup namepad swap move
  namepad over freq-wl @ search-wordlist if
    nip nip execute 1 swap +!
  else
    dup wlen !
    namepad wlen @ nextname
    get-current freq-wl @ set-current
    create 1 , drop
    set-current
    namepad wlen @ freq-wl @ search-wordlist drop
    endwp @ !  endwp @ cell+ endwp !
    drop
  then ;

: letter?  ( c -- f )
  dup [char] a >= swap [char] z <= and ;

: process-line  ( addr u -- )
  over + >r
  dup
  begin
    dup r@ <
  while
    dup c@ $20 or over c!
    dup c@ letter? 0= if
      2dup over - count-word
      1+ nip dup
    else
      1+
    then
  repeat
  r> drop
  over - count-word ;

: process-file  ( fid -- )
  >r
  begin
    line max-line r@ read-line throw
  while
    line swap process-line
  repeat
  drop r> drop ;

: wordfreq-bench  ( -- )
  wordlist freq-wl !
  word-pointers endwp !
  s" shootout/data/wordfreq.txt" r/o open-file throw >r
  r@ process-file
  r> close-file throw ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    wordfreq-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
