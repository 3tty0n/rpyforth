\ -*- mode: forth -*-
\ Heapsort benchmark rewritten in (mostly) ANSI Forth style
\ Target: Gforth 2012 core + standard FP/memory extensions

\ 0. argc @ 1- arg >number 2drop drop constant NUM
8000 constant NUM

139968 constant IM
  3877 constant IA
 29573 constant IC

variable SEED
42 SEED !

\ -------------------------------
\ Random number generator
\ gen-random: ( f -- f )  multiplies argument by random in (0,1)
\ -------------------------------

: gen-random  ( f -- f )
  SEED @ IA * IC + IM mod dup SEED !
  s>f IM s>f f/ f* ;

\ -------------------------------
\ Heap array base + helpers
\ We store the base address in a variable to avoid locals
\ Array is 1-based: a[1..NUM]; a[0] unused (sentinel slot)
\ -------------------------------

variable heap-base   \ base address (float array)
variable heap-l      \ l index (integer)
variable heap-ir     \ ir index (integer)
fvariable rra        \ temporary float value

: a@   ( i -- f )        \ fetch a[i]
  heap-base @ swap floats + f@ ;

: a!   ( f i -- )        \ store f at a[i]
  heap-base @ swap floats + f! ;

: set-rra    ( i -- )    \ rra := a[i]
  a@ rra f! ;

: store-rra  ( i -- )    \ a[i] := rra
  rra f@ a! ;

\ -------------------------------
\ Heapsort on a[1..n] (floats)
\ Uses classic Numerical Recipes-style heapsort
\ -------------------------------

: heap-sort  ( n -- )
  dup heap-ir !                  \ ir := n, keep n on stack
  2/ 1+ heap-l !                 \ l := n/2 + 1
  BEGIN
    heap-l @ 1 > IF
      \ l > 1: decrease l, take element from a[l]
      heap-l @ 1- dup heap-l !   \ l := l-1
      set-rra                    \ rra := a[l]
    ELSE
      \ l <= 1: take element from end and move root to end
      heap-ir @ set-rra          \ rra := a[ir]
      1 a@                       \ f = a[1], on float stack
      heap-ir @ a!               \ a[ir] := a[1]
      heap-ir @ 1- dup heap-ir ! \ ir := ir-1, keep ir' on stack
      dup 1 = IF                   \ compare but keep ir' for potential drop
        drop
        1 store-rra              \ a[1] := rra
        EXIT
      THEN
      drop                        \ drop ir' that we kept for the IF
      1 heap-l !                 \ l := 1
    THEN

    \ sift down from position l with temp in rra
    heap-l @ 2*                  \ j := 2*l
    BEGIN
      dup heap-ir @ <=           \ j <= ir ?
    WHILE
      \ if (j < ir) and (a[j] < a[j+1]) then j++
      dup heap-ir @ < IF
        dup a@                   \ FP: a[j], stack: j
        dup 1+ a@                \ FP: a[j] a[j+1], stack: j
        f< IF
          1+                     \ j := j+1
        THEN
      THEN

      \ if (rra < a[j]) then
      rra f@ dup a@ f< IF
        \ a[l] := a[j]
        dup a@                   \ FP: a[j], int stack: j
        heap-l @ a!              \ store at index l (a! pops l, uses float)
        \ l := j
        dup heap-l !
        \ j := 2*j
        2*
      ELSE
        \ break: force loop termination
        drop heap-ir @ 1+
      THEN
    REPEAT
    drop                          \ drop j
    heap-l @ store-rra            \ a[l] := rra
  AGAIN ;

\ -------------------------------
\ Main
\ -------------------------------

: main  ( -- )
  NUM 1+ floats allocate throw    \ allocate (NUM+1) floats
  heap-base !                     \ remember base

  \ Fill a[1..NUM] with random numbers
  NUM 0 DO
    1e gen-random                 \ same signature as original
    heap-base @ I 1+ floats + f!
  LOOP

  NUM heap-sort                   \ sort a[1..NUM]

  \ Print a[NUM] as parts-per-million integer for cross-engine comparison
  heap-base @ NUM floats + f@
  1e6 f* f>d d. cr ;

10 set-precision
UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye

