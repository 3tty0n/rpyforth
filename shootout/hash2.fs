\ http://www.bagley.org/~doug/shootout/
\ Hashes, Part II - portable rewrite (scaled SIZE for stack safety)

20 constant NUM
200 constant SIZE

wordlist constant hash1
wordlist constant hash2
create namepad 64 allot
variable wlen

: foo-name  ( i -- c-addr u )
  0 <# #s 95 hold 111 hold 111 hold 102 hold #> ;

: build-hash1  ( -- )
  get-current hash1 set-current
  SIZE 0 do
    i foo-name nextname create i ,
  loop
  set-current ;

: ensure-hash2  ( c-addr u -- addr )
  dup wlen !
  namepad swap move
  namepad wlen @ hash2 search-wordlist if
    execute
  else
    namepad wlen @ nextname
    get-current hash2 set-current create 0 , set-current
    namepad wlen @ hash2 search-wordlist drop execute
  then ;

: add-i  ( i -- )
  foo-name
  dup wlen !
  namepad swap move
  namepad wlen @ hash1 search-wordlist 0= if exit then
  execute @
  namepad wlen @ ensure-hash2
  +! ;

: build2  ( -- )  SIZE 0 do  i add-i  loop ;

: show1  ( i -- )
  foo-name hash1 search-wordlist if execute @ . else ." ? " then ;
: show2  ( i -- )
  foo-name hash2 search-wordlist if execute @ . else ." ? " then ;

: main  ( -- )
  build-hash1
  NUM 0 do  build2  loop
  1 show1  199 show1
  1 show2  199 show2  cr
;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
