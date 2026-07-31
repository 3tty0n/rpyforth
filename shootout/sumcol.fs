\ http://www.bagley.org/~doug/shootout/
\ Sum a column of integers (sumcol.gforth)

256 constant max-line
create line-buffer max-line 1+ allot

: parse-int  ( c-addr u -- n )
  0 0 2swap >number 2drop d>s ;

: sumcol  ( -- )
  0
  begin
    line-buffer max-line stdin read-line throw
  while
    >r line-buffer r> parse-int +
  repeat
  drop
  1 u.r cr
;

UTIME 2>R
sumcol
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
