\ http://www.bagley.org/~doug/shootout/
\ Random Number Generator benchmark
\ Linear congruential generator; print the last value scaled to ppm
\ so the result is an exact integer comparable across engines.

60000000 constant NUM

139968 constant IM
  3877 constant IA
 29573 constant IC

variable SEED
42 SEED !

: gen-random  ( fmax -- fr )
  SEED @ IA * IC + IM mod dup SEED !
  s>f IM s>f f/ f* ;

: main  ( -- )
  0e
  NUM 0 do
    fdrop 100e gen-random
  loop ;

UTIME 2>R
main
1e6 f* f>d d. cr
UTIME 2R> D- ." Elapsed: " D. ." usec" CR

bye
