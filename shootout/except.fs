\ http://www.bagley.org/~doug/shootout/
\ Exception Mechanisms benchmark
\ blowup throws Lo on even n and Hi on odd n.  lo-function catches only
\ Lo and lets Hi propagate; hi-function catches Hi.  Count each kind.
\ The current n is held in a variable so every catch frame has an empty
\ data stack and leaves exactly one item (throw code, or 0 on success).

1000000 constant NUM

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
  ?dup if  ." unexpected exception " . cr  bye  then ;

: main  ( -- )
  0 hi-count !  0 lo-count !
  NUM 0 do  i curn !  some-function  loop
  hi-count @ 1 u.r ."  " lo-count @ 1 u.r cr ;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR

bye
