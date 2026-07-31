\ http://www.bagley.org/~doug/shootout/
\ String concatenation (Bagley shootout / strcat.gforth)
\ Append "hello." NUM times; print final length.

50000 constant NUM

variable hsiz    32                       hsiz !
variable hbuf    hsiz @ allocate throw    hbuf !
variable hoff    0                        hoff !

: STUFF s" hello." ;

: strcat  ( c-addr u -- )
  dup hsiz @ hoff @ - >
  if
    hsiz @ 2* hsiz !
    hbuf @ hsiz @ resize throw hbuf !
  then
  swap over
  hbuf @ hoff @ + swap cmove>
  hoff @ + hoff !
;

: main  ( -- )
  NUM 0 do  STUFF strcat  loop
  hbuf @ hoff @
  1 u.r cr drop
;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
