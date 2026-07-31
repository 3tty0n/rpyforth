\ $Id: ary3.gforth,v 1.1 2001/05/31 02:27:48 doug Exp $
\ http://www.bagley.org/~doug/shootout/

\ decimal

\ read NUM from last command line argument4\ 0. argc @ 1- arg >number 2drop drop constant NUM
30096 constant NUM

variable X
NUM cells allocate drop X !
variable Y
NUM cells allocate drop Y !

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

UTIME 2>R
ary
Y @ @ 1 u.r ."  " NUM 1 - cells Y @ + @ 1 u.r cr
UTIME 2R> D- ." Elapsed: " D. ." usec" CR


bye \ th-th-that's all folks!
