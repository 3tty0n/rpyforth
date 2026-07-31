\ http://www.bagley.org/~doug/shootout/
\ Object instantiation (Bagley shootout / objinst.gforth)
\ Portable stand-in for gforth objects.fs: same Toggle / NthToggle
\ workload as methcall.fs, plus NUM heap-style construct/destroy cycles.

1000000 constant NUM

variable t-state
: t-construct  ( f -- )  t-state ! ;
: t-activate   ( -- )    t-state @ 0= t-state ! ;
: t-value      ( -- f )  t-state @ ;

variable n-state
variable n-counter
variable n-max
: n-construct  ( max f -- )
  n-state !  n-max !  0 n-counter ! ;
: n-activate  ( -- )
  n-counter @ 1+ n-counter !
  n-counter @ n-max @ >= if
    n-state @ 0= n-state !
    0 n-counter !
  then ;
: n-value  ( -- f )  n-state @ ;

: flag.  ( f -- )  if ." true" else ." false" then cr ;

: toggle-loop  ( -- )
  -1 t-construct
  5 0 do  t-activate t-value flag.  loop ;

: nth-loop  ( -- )
  3 -1 n-construct
  8 0 do  n-activate n-value flag.  loop ;

: main  ( -- )
  toggle-loop
  NUM 0 do  -1 t-construct  loop
  cr
  nth-loop
  NUM 0 do  3 -1 n-construct  loop
;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
