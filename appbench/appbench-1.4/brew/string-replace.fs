\ string-replace.fs
\ 	$Id: string-replace.fs,v 1.5 2002/11/15 23:15:54 f Exp $	

\ Build tables of strings and use them for string replacements.

\ ****************************************************************
\ dependencies:
s" string-lists.fs" REQUIRED

\ ****************************************************************


\ Word to start building a string table during compilation:
: BEGIN-string-table ( -- list )   1 deflist ;

\ Add astring to the table by adding it to the temporary string list:
: table-string! ( list addr count -- list )   third string>list ;

\ Compute length of aligned string datas for allocation:
: string-table>size ( list -- size )
    2 cells		( list size )
    over nodes 2* cells +
    over nodes 0 ?DO
	i third n'th-handle buffered-length aligned +
    LOOP
    nip ;

\ Actually build the string table in allocated memory during compilation:
: BUILD-string-table ( list -- addr )
    >r

    r@ string-table>size allocate
    ABORT" build-translation-table: Could not allocate."
    r@ nodes over !			\ store string count
    dup cell+ cell+  over cell+ !	\ store start of string pointers

    dup r@ nodes 1+ 2* cells +		( addr start-of-string-data  r: list )
    r@ dup nodes 0 ?DO			( addr write-pointer node )
	next-node
	dup @			( addr write node handle )
	buffered-length  fourth  i 1+ 2* cells +  dup >r  !	\ pointer count
	over r> cell+ !						\ pointer addr

	dup @ >r		( addr write node  r: handle )
	r@ buffer-data-addr  third  r@ buffered-length  move
	swap r>  buffered-length aligned +  swap		\ write pointer
    LOOP 2drop
    r> remove-string-list ;

\ Replace some strings, leaving the others untouched.
\ A translation table is a string table with alternating look up and replace
\ strings:
\ BEGIN-string-table
\ s" string-1"      table-string!
\ s" replacement-1" table-string!
\ s" string-2"      table-string!
\ s" replacement-2" table-string!
\ BUILD-string-table VALUE translation-table
\
\ Translate strings based on translation-table:
: translate-string ( addr count translation-table -- addr' count' )
    2@ 2/ 0 ?DO		( addr count pointer-base-address )
	dup i 2* 2* cells + >r third third r> 2@ compare 0= IF
	    >r 2drop r> i 2* 1+ 2* cells + 2@
	    UNLOOP EXIT
	THEN
    LOOP
    drop ;

false [IF] \ unused
: n'th-table-string ( n table -- addr count )
    >r
    dup 0 r@ @ WITHIN 0= ABORT" n'th-table-string: index out of range."
    1+ 2* cells r> + 2@ ;

: translate-back ( addr count translation-table -- addr' count' )
    2@ 2/ 0 ?DO		( addr count pointer-base-address )
	dup i 2* 1+ 2* cells + >r third third r> 2@ compare 0= IF
	    >r 2drop r> i 2* 2* cells + 2@
	    UNLOOP EXIT
	THEN
    LOOP
    drop ;
[THEN]

false [IF] \ testing
    BEGIN-string-table
    s" chatz" table-string!
    s" cat"   table-string!
    s" hund"  table-string!
    s" dog"   table-string!
    \ dup string-table>size cr .
    \ remove-string-list
    BUILD-string-table VALUE translation-table

    s" chatz" translation-table translate-string   cr type
    s" cat"   translation-table translate-string   cr type
    s" hund"  translation-table translate-string   cr type
    s" what?" translation-table translate-string   cr type

\    3 translation-table n'th-table-string cr type

    s" cat" translation-table translate-back cr type
    s" dog" translation-table translate-back cr type

    translation-table free drop
    cr .s
[THEN]
