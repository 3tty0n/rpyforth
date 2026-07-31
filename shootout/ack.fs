\ $Id: ackermann.gforth,v 1.2 2001/05/25 16:43:25 doug Exp $
\ ackermann's function
\ http://www.bagley.org/~doug/shootout/

8 constant NUM

: ack  recursive
    dup 0=
    if
    drop 1+
    else
    swap dup 0=
    if
        drop 1- 1 swap ack
    else
        1- over 1- rot rot swap ack swap ack
    then
    then ;
\ END ACK

\ run ack(3, NUM) and print result from stack
: main ." Ack: " NUM 3 ack 4 u.r CR ;

utime 2>R
main
utime 2R> D- ." Elapsed: " D. ." usec" CR

bye
