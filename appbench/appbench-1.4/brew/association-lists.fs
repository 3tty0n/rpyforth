\ association-lists.fs
\ 	$Id: association-lists.fs,v 1.7 2003/03/12 22:20:49 f Exp $	

\ Lists of key's and associated data.
\ The first data field stores the key.

\ ****************************************************************
\ dependencies:
s" lists.fs" REQUIRED

\ ****************************************************************


: ASSOCIATION-LIST: ( data-fields -- )	1+ nLIST: ;	\ field for key

cell
OFFSET: >associated-data ( node-addr -- associated-data-address )
drop

\ Return first node with key in it's first data field, or false.
: key-is-in? ( key list-addr -- node|false )
    dup nodes 0 ?DO	( key actual-node )
	next-node
	2dup @ = IF	\ key found
	    unloop nip EXIT
	THEN
    LOOP
    2drop
    FALSE ;

\ Search list for key, add a node if not there already.
\ Initialize data fields of new nodes to zero, and store key in the first one.
: key-to-list ( key list-addr -- node )
    >r			( key  r: list-addr )
    dup r@ key-is-in? dup IF			\ key was in already
	rdrop
	nip
	EXIT
    THEN

    \ new key:
    drop
    r@ new-node	( key node  r: list-addr )
    dup  r> data-fields cells erase		\ erase data fields
    >r r@ !		( r: node )		\ store key
    r> ;

false [IF]	\ testing

\ (I moved this to testing, because I don't seem to need it now)

\ Counting occurances of key:

\ association list to count occurences of keys:
: count-LIST: ( -- )   1 ASSOCIATION-LIST: ;

cell
OFFSET: >count
drop

\ Increases first associated data field of key,
\ creating a new node if key was not in already:
: count-key ( key list-addr -- )   key-to-list >count 1 swap +! ;


    page cr
    cr .( Testing association lists: )
    cr
    1 ASSOCIATION-LIST: a-list
    99 a-list key-is-in?	cr .( Key not in:	) .
    99 a-list key-to-list	cr .( Key now in:	) dup .
    .( 	associated data:	) >associated-data @ .
    99 a-list count-key
    99 a-list key-to-list	cr .( Key counted:	) >count @ .
    99 a-list count-key
    99 a-list key-to-list	cr .( counted again:	) >count @ .
    88 a-list count-key
    88 a-list key-to-list	cr .( new counted:	) >count @ .
    a-list nodes		cr .( nodes:		) .
    a-list empty-list
    a-list nodes		cr .( nodes gone:	) .
    cr .( Stack:		) .s
    cr

    cr bye
[THEN] \ testing
