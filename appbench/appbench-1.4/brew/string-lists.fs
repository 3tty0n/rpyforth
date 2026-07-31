\ string-lists.fs
\ 	$Id: string-lists.fs,v 1.6 2002/11/15 23:14:15 f Exp $	

\ Lists of string buffer handles.

\ ****************************************************************
\ dependencies:

s" lists.fs" REQUIRED
s" stringbuf-0.4.fs" REQUIRED

\ ****************************************************************


: string>list ( addr count list-addr -- )
    new-node >r
    dup stringbuf-open
    dup r> !
    string! ;

: n'th-handle ( u list -- handle )  n'th-node @ ;
: n'th-string@ ( u list -- addr count )  n'th-handle string@ ;

: remove-string-list ( list -- )	\ don't do this on named lists...
    >r
    r@ r@ nodes 0 ?DO
	next-node
	dup @ stringbuf-close
    LOOP drop
    r> remove-list ;
