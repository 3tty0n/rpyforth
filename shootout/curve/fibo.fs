\ Fibonacci benchmark for JIT analysis

30 constant NUM
50 constant ITERATIONS

: get-time ( -- d ) utime ;
: diff-time ( d-start d-end -- d-diff ) 2swap d- ;

: fib ( n -- n )
    recursive
    dup 2 <
    if
        drop 1
    else
        dup
        2 - fib
        swap
        1 - fib
        +
    then ;

: run-benchmark
    ." Iteration,Time(usec)" cr
    ITERATIONS 0 do
        get-time
        NUM fib drop
        get-time
        diff-time
        i . ." ," d. cr
    loop ;

run-benchmark
bye
