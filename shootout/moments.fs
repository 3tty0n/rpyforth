\ http://www.bagley.org/~doug/shootout/
\ Statistical moments - integer-input portable port (moments.gforth spirit)

create nums  4096 floats allot
variable #nums  0 #nums !

1024 constant max-line
create line max-line 2 + allot

: push-int  ( n -- )
  s>f  nums #nums @ floats + f!  1 #nums +!
;

: parse-int  ( c-addr u -- n )
  0 0 2swap >number 2drop d>s ;

: input-ints  ( fid -- )
  >r
  begin
    line max-line r@ read-line throw
  while
    line swap parse-int push-int
  repeat
  r> drop drop
;

: mean  ( -- r )
  0e
  #nums @ 0 do  nums i floats + f@ f+  loop
  #nums @ s>f f/
;

: variance  ( rmean -- rvar )
  0e
  #nums @ 0 do
    nums i floats + f@ fover f- fdup f* f+
  loop
  fswap fdrop
  #nums @ 1- s>f f/
;

: main  ( -- )
  stdin input-ints
  ." n: " #nums @ 0 .r cr
  mean fdup ." mean: " f. cr
  variance ." variance: " f. cr
;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
