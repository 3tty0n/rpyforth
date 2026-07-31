\ sorted-string-lists.fs
\ 	$Id: sorted-string-lists.fs,v 1.2 2002/11/15 23:18:58 f Exp $	

\ Build alphabetically sorted string lists.
\ Very simple insertion sort.

\ ****************************************************************
s" string-lists.fs" REQUIRED
\ ****************************************************************


: last-string-node-not-bigger ( handle string-list -- node )
    swap >r		( string-list  r: handle )

    BEGIN		( actual-node  r: handle )
	dup next-node	( previous-node actual-node  r: handle )
	dup 0= IF  drop rdrop  EXIT THEN
	dup @ r@ string-compare 0> IF
	    drop rdrop EXIT
	THEN
	nip
    AGAIN ;

: insert-string-sorted ( addr count list -- )
    >r
    string!!
    dup r@ last-string-node-not-bigger
    r> insert-after-node
    ! ;

false [IF]	\ testing

    page .( testing sorted-string-lists.fs )

    LIST: test-list

    s" b"  test-list insert-string-sorted
    s" x"  test-list insert-string-sorted
    s" bb" test-list insert-string-sorted
    s" 1"  test-list insert-string-sorted
    s" r"  test-list insert-string-sorted
    s" b"  test-list insert-string-sorted
    s" "   test-list insert-string-sorted
    s" a"  test-list insert-string-sorted
    s" ."  test-list insert-string-sorted
    s" ba" test-list insert-string-sorted
    s" c"  test-list insert-string-sorted
    s" .." test-list insert-string-sorted
    s" A"  test-list insert-string-sorted

: .string-list ( string-list -- )
    dup nodes 0 ?DO
	next-node
	cr i .
	dup @ string@ type
    LOOP
    drop ;

    cr test-list .string-list

    cr
    cr .( stack:	) .s

    cr cr bye
[THEN]
