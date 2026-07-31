\ http://www.bagley.org/~doug/shootout/
\ Word / line / char count (wc.gforth)

variable nn  0 nn !
variable nw  0 nw !
variable nc  0 nc !
variable in_word  0 in_word !

10 constant nl_ch
9  constant tab_ch
32 constant space_ch

4096 constant MAXREAD
create buff MAXREAD allot

: scanbuff  ( n -- )
  dup nc +!
  buff + buff
  ?do
    i c@
    case
      nl_ch    of  0 in_word !  1 nn +!  endof
      tab_ch   of  0 in_word !  endof
      space_ch of  0 in_word !  endof
      in_word @ 0= if  1 in_word !  1 nw +!  then
    endcase
  loop
;

: wc  ( -- )
  begin
    buff MAXREAD stdin read-file throw dup
  while
    scanbuff
  repeat
  drop
;

UTIME 2>R
wc nn @ . nw @ . nc @ 1 u.r cr
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
