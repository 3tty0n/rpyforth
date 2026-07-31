\ http://www.bagley.org/~doug/shootout/
\ Method Calls benchmark
\ Toggle flips its state on every activation; NthToggle flips only once
\ every COUNT activations.  We drive each NUM times and print the final
\ state (1 = true, 0 = false).

50000000 constant NUM

variable t-state
: t-activate  ( -- )    t-state @ 0= t-state ! ;
: t-value     ( -- f )  t-state @ ;

variable n-state
variable n-counter
variable n-max
: n-activate  ( -- )
  n-counter @ 1+ n-counter !
  n-counter @ n-max @ = if
    n-state @ 0= n-state !
    0 n-counter !
  then ;
: n-value     ( -- f )  n-state @ ;

: main  ( -- )
  -1 t-state !
  NUM 0 do  t-activate  loop
  t-value abs .
  -1 n-state !  0 n-counter !  3 n-max !
  NUM 0 do  n-activate  loop
  n-value abs .
  cr ;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR

bye
