\ sorted-lists.fs
\ 	$Id: sorted-lists.fs,v 1.7 2003/08/27 18:14:17 f Exp $	

\ Build a sorted copy of a list.
\ The list can have multiple data fields.
\ Sorting is done numerically on one of the data fields. 
\ Very simple minded implementation.  Insertion sort.

\ ****************************************************************
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" lists.fs" REQUIRED

\ ****************************************************************


: last-node-not-bigger ( key offset list -- node )
    dup dup 2>r		( key offset list  r: list list )

    BEGIN		( key offset actual-node  r: list last-node )
	next-node
	dup 0= IF drop 2drop r> rdrop EXIT THEN
	2dup + @	( key offset actual-node actual-value  r: list last )
	3 pick > IF		\ this node's value is too high
	    2drop drop
	    r> rdrop EXIT	\ return last node
	THEN		( key offset actual-node  r: list last-node )
	rdrop dup >r
    AGAIN ;

\ Insert node data into list, sorted numerically based on field u of node:
\ The node must have at least the same number of data fields as the list.
\ If identical sort keys are given, the new node is inserted after the others.
: insert-node-sorted ( node-address u list-address -- )
    >r			 ( node-address u  r: list-address )
    cells >r		 ( node-address    r: list-address offset )
    dup r@ + @		 ( node-address key-data  r: list-address offset )
    r> r@ last-node-not-bigger	( node-address last-node   r: list-address )
    r@ insert-after-node	( node-addr inserted-node  r: list-address )
    r> data-fields cells move ;

\ Insert two values sorted into a list, storing the key as first data cell:
: 2-insert-sorted ( data2 key list-address -- )
    >r					( data2 key  r: list-address )
    dup 0 r@ last-node-not-bigger	( data2 key node  r: list-address )
    r> insert-after-node 2! ;

\ Build a list with identical node data, sorted based on data field u.
\ Don't forget to 'remove-list'.
\ Very simple minded implementation, slow...
: copy-to-sorted-list ( u list-address -- new-sorted-list-address )
    dup data-fields deflist >r	( u from-list  r: new-list )
    BEGIN		( u actual-from-list-node  r: new-list )
	next-node
    dup WHILE
	2dup swap r@ insert-node-sorted
    REPEAT
    2drop
    r> ;		\ don't forget to 'remove-list' later


false [IF]	\ testing

    page cr .( testing sorted-lists.fs )

    [UNDEFINED] .all-list-data [IF]
    : .all-node-data ( list-addr node -- )	\ for testing
	swap data-fields	( node data-fields )
	dup 1 = IF
	    drop  @ .
	ELSE
	    cr
	    0 ?DO
		i [char] 0 + emit ." :"
		dup i cells + @ .
	    LOOP
	    drop
	THEN ;

    : .all-list-data ( list-addr -- )	\ for testing
	dup
	dup nodes 0 ?DO		( list-addr current-node )
	    next-node
	    dup node-is-list?
	    dup IF
		cr ." list as element " i . ." " cr
		RECURSE
		cr ." continue with old list:" cr
	    ELSE drop
		2dup .all-node-data
	    THEN
	LOOP 2drop cr ." done" ;

    [THEN]

    LIST: unsorted

    cr
    cr .( testing 'insert-node-sorted' )
    1 unsorted >list
    3 unsorted >list
    variable zero	zero off
    variable two	2 two !
    variable four	4 four !
    two 0 unsorted insert-node-sorted
    four 0 unsorted insert-node-sorted
    zero 0 unsorted insert-node-sorted
    cr unsorted .all-list-data
    unsorted empty-list

    [UNDEFINED] random-ranged [IF]	INCLUDE random.fs		[THEN]

    : fill-unsorted ( u -- )
	unsorted
	swap 0 ?DO
	    10 random-ranged over >list
	LOOP
    drop ;

    cr
    cr .( testing 'copy-to-sorted-list')
    cr .( unsorted: )
    cr
    25 fill-unsorted
    unsorted .all-list-data
    cr .( sorted: )
    cr
    0 unsorted copy-to-sorted-list
    dup .all-list-data
    remove-list

    cr
    unsorted empty-list
    5000
    cr .( preparing data for speed test )
    dup fill-unsorted
    cr .( testing slowness, iterations: ) .
    cr .( 'copy-to-sorted-list' )
    bell
    0 unsorted copy-to-sorted-list
    bell
    remove-list

    cr .( stack:	) .s

    2 nLIST: 2unsorted

    cr
    cr .( testing multiple data sort:)
    3 8  2unsorted new-node 2!
    1 6  2unsorted new-node 2!
    5 0  2unsorted new-node 2!
    9 2  2unsorted new-node 2!
    7 4  2unsorted new-node 2!

    cr .( unsorted: )
    cr 2unsorted .all-list-data

    cr
    cr .( sorted on first: )
    0 2unsorted copy-to-sorted-list
    cr dup .all-list-data remove-list

    cr
    cr .( sorted on second: )
    1 2unsorted copy-to-sorted-list
    cr dup .all-list-data remove-list

    cr
    cr .( stack:	) .s

    cr cr bye
[THEN]
