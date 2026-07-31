40 constant NUM

\ compute fibonacci numbers
: fib  recursive
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

UTIME 2>R
NUM fib 1 u.r cr
UTIME 2R> D- ." Elapsed: " D. ." usec" CR

bye
