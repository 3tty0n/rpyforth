\ Ackermann function benchmark for JIT analysis

7 constant NUM
50 constant ITERATIONS

: ack ( n m -- res )
    recursive
    dup 0= if
        drop 1+
    else
        swap dup 0= if
            drop 1- 1 swap ack
        else
            1- over 1- rot rot swap ack swap ack
        then
    then ;

: run-benchmark
    ." Iteration,Time(usec)" cr
    ITERATIONS 0 do
        utime 2>R
        NUM 3 ack drop
        utime 2R> d-
        i . ." ," d. cr
    loop ;

run-benchmark
bye
