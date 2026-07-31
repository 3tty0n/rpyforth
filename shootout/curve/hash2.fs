\ Hash build2 benchmark for JIT analysis

200 constant SIZE
50 constant ITERATIONS

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

build-hash1

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    build2
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
