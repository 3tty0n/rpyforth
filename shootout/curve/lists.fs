\ List operations benchmark for JIT analysis

1 constant NUM
2000 constant SIZE
50 constant ITERATIONS

\ Portable 2-cell nodes: [next | val]
2 cells constant list%
: list-next ( list -- addr ) ;
: list-val  ( list -- addr ) cell+ ;
: list-alloc ( -- list ) list% allocate throw ;

: make-list  ( -- list )
  0 SIZE
  begin
    dup 0>
  while
    1- >r
    list-alloc
    r@ over list-val !
    tuck list-next !
    r>
  repeat
  drop
;

: list-length  ( list -- n )
  0 begin  over  while  1+ swap list-next @ swap  repeat  nip
;

: nreverse  ( list -- list' )
  0 swap
  begin
    dup
  while
    dup list-next @ >r
    tuck list-next !
    r>
  repeat
  drop
;

: list-sum  ( list -- n )
  0 begin  over  while  over list-val @ +  swap list-next @ swap  repeat  nip
;

: lists-bench  ( -- n )
  0
  NUM 0 do
    drop
    make-list
    dup list-length SIZE <> if drop exit then
    dup nreverse nreverse
    dup list-length SIZE <> if drop exit then
    list-sum
  loop
;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    utime 2>R
    lists-bench drop
    utime 2R> d-
    i . ." ," d. cr
  loop ;

run-benchmark
bye
