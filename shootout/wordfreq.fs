\ http://www.bagley.org/~doug/shootout/
\ Word frequency - portable (unsorted counts)

wordlist constant word-counts
create word-pointers 30000 cells allot
variable endwp  word-pointers endwp !
variable wlen
1024 constant max-line
create line max-line 2 + allot
create namepad 256 allot

: count-word  ( addr u -- )
  dup 0= if 2drop exit then
  2dup namepad swap move
  namepad over word-counts search-wordlist if
    \ IF consumed flag; stack is (addr u xt)
    nip nip execute 1 swap +!
  else
    \ flag already consumed by IF; stack is (addr u)
    dup wlen !
    namepad wlen @ nextname
    get-current word-counts set-current
    create 1 , drop
    set-current
    namepad wlen @ word-counts search-wordlist drop
    endwp @ !  endwp @ cell+ endwp !
    drop
  then ;

: letter?  ( c -- f )
  dup [char] a >= swap [char] z <= and ;

: process-line  ( addr u -- )
  over + >r
  dup
  begin
    dup r@ <
  while
    dup c@ $20 or over c!
    dup c@ letter? 0= if
      2dup over - count-word
      1+ nip dup
    else
      1+
    then
  repeat
  r> drop
  over - count-word ;

: process-file  ( fid -- )
  >r
  begin
    line max-line r@ read-line throw
  while
    line swap process-line
  repeat
  drop r> drop ;

: output  ( -- )
  word-pointers
  begin
    dup endwp @ <
  while
    dup @ dup execute @ 7 .r 9 emit
    xt>string type cr
    cell+
  repeat
  drop ;

UTIME 2>R
stdin process-file output
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
