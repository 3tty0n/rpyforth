\ Nested Loop benchmark for JIT analysis

10 constant NUM
50 constant ITERATIONS

: get-time ( -- d ) utime ;
: diff-time ( d-start d-end -- d-diff ) 2swap d- ;

: nestedloops ( n -- n )
  NUM 0 do
    NUM 0 do
      NUM 0 do
        NUM 0 do
          NUM 0 do
            NUM 0 do
              1+
            loop
          loop
        loop
      loop
    loop
  loop ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    get-time
    0 nestedloops drop
    get-time
    diff-time
    i . ." ," d. cr
  loop ;

run-benchmark
bye
