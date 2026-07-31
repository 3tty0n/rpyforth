\ Array Access Benchmark for JIT analysis

512 constant NUM
50 constant ITERATIONS

variable X
NUM cells allocate drop X !
variable Y
NUM cells allocate drop Y !

: get-time ( -- d ) utime ;
: diff-time ( d-start d-end -- d-diff ) 2swap d- ;

: ary
    NUM 0 do
        1 i + i cells X @ + !
    loop
    1000 0 do
        NUM 0 do
            i cells Y @ +
            dup @
            i cells X @ +
            @ + swap !
        loop
    loop ;

: run-benchmark
    ." Iteration,Time(usec)" cr
    ITERATIONS 0 do
        get-time
        ary
        get-time
        diff-time
        i . ." ," d. cr
    loop ;

run-benchmark
bye
