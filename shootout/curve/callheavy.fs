\ Call-heavy benchmark for JIT analysis

200000 constant NUM
50 constant ITERATIONS

: get-time ( -- d ) utime ;
: diff-time ( d-start d-end -- d-diff ) 2swap d- ;

: f0 ( n -- n' )  dup 0< if drop 0 then 1+ ;
: f1 ( n -- n' )  f0 f0 ;
: f2 ( n -- n' )  f1 f1 ;
: f3 ( n -- n' )  f2 f2 ;
: f4 ( n -- n' )  f3 f3 ;

: run-benchmark
    ." Iteration,Time(usec)" cr
    ITERATIONS 0 do
        get-time
        0 NUM 0 do f4 loop drop
        get-time
        diff-time
        i . ." ," d. cr
    loop ;

run-benchmark
bye
