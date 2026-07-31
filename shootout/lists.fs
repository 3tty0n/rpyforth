\ http://www.bagley.org/~doug/shootout/
\ List operations - portable lists.gforth spirit (SIZE nodes, NUM trials).

10 constant NUM
10000 constant SIZE

struct
  cell% field list-next
  cell% field list-val
end-struct list%

: make-list  ( -- list )
  0 SIZE
  begin
    dup 0>
  while
    1- >r
    list% %alloc
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

: main  ( -- n )
  0
  NUM 0 do
    drop
    make-list
    dup list-length SIZE <> if ." bad len" cr bye then
    dup nreverse nreverse
    dup list-length SIZE <> if ." bad rev" cr bye then
    list-sum
  loop
;

UTIME 2>R
main 0 .r cr
UTIME 2R> D- ." Elapsed: " D. ." usec" CR
bye
