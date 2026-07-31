\ Many shallow, high-frequency colon-word calls. f0 contains a branch so it is
\ not inlined and stays a real call; the chain issues 16 f0 calls per loop
\ iteration.

1000000 constant NUM

: f0 ( n -- n' )  dup 0< if drop 0 then 1+ ;
: f1 ( n -- n' )  f0 f0 ;
: f2 ( n -- n' )  f1 f1 ;
: f3 ( n -- n' )  f2 f2 ;
: f4 ( n -- n' )  f3 f3 ;

: main  ." Calls: "  0  NUM 0 do f4 loop  9 u.r CR ;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR

bye
