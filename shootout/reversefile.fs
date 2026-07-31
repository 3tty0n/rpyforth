\ http://www.bagley.org/~doug/shootout/
\ Reverse lines from stdin

create lines  4096 cells allot
variable nlines 0 nlines !

: save-line  ( c-addr u -- )
  here >r
  dup ,
  here over allot swap move
  r> nlines @ cells lines + !
  1 nlines +! ;

: reversefile  ( -- )
  begin
    here 256 allot here 256 -   \ temporary buffer at HERE
    dup 256 stdin read-line throw
  while
    ( buf u )
    save-line
  repeat
  2drop
  nlines @
  begin
    dup 0= if drop exit then
    1-
    dup cells lines + @
    dup @ swap cell+ swap type cr
  again ;

UTIME 2>R
reversefile
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
