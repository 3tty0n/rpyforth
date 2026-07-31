\ Takeuchi (tak) benchmark for JIT analysis

24 constant NUM
50 constant ITERATIONS

: get-time ( -- d ) utime ;
: diff-time ( d-start d-end -- d-diff ) 2swap d- ;

: tak ( x y z -- n ) recursive
    2 pick 2 pick > if
        2 pick 1- 2 pick 2 pick tak
        2 pick 1- 2 pick 5 pick tak
        2 pick 1- 5 pick 5 pick tak
        tak
        nip nip nip
    else
        nip nip
    then ;

: run-benchmark
    ." Iteration,Time(usec)" cr
    ITERATIONS 0 do
        get-time
        NUM  NUM 2* 3 /  NUM 3 /  tak drop
        get-time
        diff-time
        i . ." ," d. cr
    loop ;

run-benchmark
bye
