\ Matrix Multiplication benchmark for JIT analysis

  30 constant SIZE
SIZE SIZE * constant ELEMS
40 constant NUM
50 constant ITERATIONS

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

: matrix-bench ( -- )
  NUM 0 do mmult loop ;

: run-benchmark
  m1 mkmatrix
  m2 mkmatrix
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    matrix-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
