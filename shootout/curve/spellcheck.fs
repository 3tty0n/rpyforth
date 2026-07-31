\ Spell checker benchmark for JIT analysis

wordlist constant dict
32 constant max-word
50 constant ITERATIONS
create line max-word 2 + allot

: read-dict  ( -- )
  get-current dict set-current
  s" shootout/data/Usr.Dict.Words" r/o open-file throw >r
  begin
    line max-word r@ read-line throw
  while
    line swap nextname create
  repeat
  drop r> close-file throw
  set-current ;

: spellcheck-file  ( fid -- n )
  >r 0
  begin
    line max-word r@ read-line throw
  while
    line swap 2dup dict search-wordlist if
      2drop drop
    else
      2drop 1+
    then
  repeat
  drop r> drop
;

read-dict

: spellcheck-bench  ( -- )
  s" shootout/data/spellcheck.txt" r/o open-file throw >r
  r@ spellcheck-file drop
  r> close-file throw ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    spellcheck-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
