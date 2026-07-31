\ http://www.bagley.org/~doug/shootout/
\ Hash (associative array) access - Anton Ertl / hash.gforth

10000 constant NUM

wordlist constant x

: build  ( -- )
  get-current x set-current
  base @ hex
  NUM 0 do
    i 0 <# #s #> nextname i constant
  loop
  base ! set-current ;

: countdecs  ( -- n )
  0
  NUM 0 do
    i 0 <# #s #> x search-wordlist if  drop 1+  then
  loop ;

: main  ( -- )  build countdecs 0 .r cr ;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
