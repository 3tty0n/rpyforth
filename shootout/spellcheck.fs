\ http://www.bagley.org/~doug/shootout/
\ Spell checker (spellcheck.gforth)

wordlist constant dict
32 constant max-word
create line max-word 2 + allot

: read-dict  ( -- )
  get-current dict set-current
  s" shootout/data/Usr.Dict.Words" r/o open-file throw >r
  begin
    line max-word r@ read-line throw
  while
    line swap nextname create
  repeat
  drop r> drop
  set-current ;

: spellcheck  ( -- )
  begin
    line max-word stdin read-line throw
  while
    line swap 2dup dict search-wordlist if
      2drop drop
    else
      type cr
    then
  repeat
  drop ;

UTIME 2>R
read-dict spellcheck
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
