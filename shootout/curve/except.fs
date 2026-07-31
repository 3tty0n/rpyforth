\ Exception Mechanisms benchmark for JIT analysis

4000 constant NUM
50 constant ITERATIONS

1 constant LO-EX
2 constant HI-EX

variable curn
variable hi-count
variable lo-count

: blowup  ( -- )
  curn @ 1 and if  HI-EX throw  else  LO-EX throw  then ;

: lo-function  ( -- )
  ['] blowup catch
  ?dup if
    dup LO-EX = if  drop 1 lo-count +!  else  throw  then
  then ;

: hi-function  ( -- )
  ['] lo-function catch
  ?dup if
    dup HI-EX = if  drop 1 hi-count +!  else  throw  then
  then ;

: some-function  ( -- )
  ['] hi-function catch
  ?dup if  ." unexpected exception" cr  bye  then ;

: except-bench ( -- )
  0 hi-count !  0 lo-count !
  NUM 0 do  i curn !  some-function  loop ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    except-bench
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
