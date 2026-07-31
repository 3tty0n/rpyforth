\ http://www.bagley.org/~doug/shootout/
\ Matrix Multiplication benchmark
\ Multiply two SIZE x SIZE integer matrices NUM times, print four cells.

  30 constant SIZE
SIZE SIZE * constant ELEMS
3000 constant NUM

create m1 ELEMS cells allot
create m2 ELEMS cells allot
create m3 ELEMS cells allot

variable rowi

: addr  ( base r c -- a )  swap SIZE * + cells + ;

: mkmatrix  ( base -- )
  1
  SIZE 0 do
    SIZE 0 do
      over j i addr
      over swap !
      1+
    loop
  loop
  2drop ;

: mmult  ( -- )
  SIZE 0 do
    i rowi !
    SIZE 0 do
      0
      SIZE 0 do
        m1 rowi @ i addr @
        m2 i j addr @
        * +
      loop
      m3 rowi @ i addr !
    loop
  loop ;

: main  ( -- )
  m1 mkmatrix
  m2 mkmatrix
  NUM 0 do  mmult  loop
  m3 0 0 addr @ 1 u.r ."  "
  m3 2 3 addr @ 1 u.r ."  "
  m3 3 2 addr @ 1 u.r ."  "
  m3 SIZE 1- dup addr @ 1 u.r cr ;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR

bye
