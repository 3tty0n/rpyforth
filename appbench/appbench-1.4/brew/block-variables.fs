\ block-variables.fs
\ 	$Id: block-variables.fs,v 1.5 2001/09/09 20:53:54 f Exp $	

\ ****************************************************************
\ Putting important data together for cache consistency.
\ Allows padding and alignement for speed.
\ ****************************************************************
\ This implementation compiles the address of the variables as constants.
\ Test with 'benchmarks/block-var-speed-test.fs'.
\ ****************************************************************


\ Compile switch 'dummy-block-variables':
\ FALSE		Do use block variables.
\ 1		Do use normal variables defined when registered for the blocks.
\ 2		Do use normal variables defined when commented out in the code
\ 		with '\VARIABLE' '\2VARIABLE' '\FVARIABLE'


\ ****************************************************************
dummy-block-variables 0= [IF] \ Can be replaced by dummys for benchmarking.

\ Start a block of variable definitions:
\ Use this once for starting defining a block of variables.
: init-var-block ( -- list-addr offset=0 )   2 deflist  0 ;

\ Register a named-address within such a block for later definition:
: (block-VARIABLE:) ( "name" list-addr offset -- list-addr offset )
    over >r	( "name" list-addr offset  r: list-address )
    bl word count
    dup stringbuf-open >r r@ cat
    dup r> r> 2>list ;

\ Register a cell variable in a block for later definition:
\ The variable will *not* be defined until concluding 'define-block-variables'.
: block-VARIABLE: ( "name" list-addr offset -- list-addr offset+cell )
    (block-VARIABLE:) cell+ ;

\ Register a double cell variable in a block for later definition:
\ The variable will *not* be defined until concluding 'define-block-variables'.
: block-2VARIABLE: ( "name" list-addr offset -- list-addr offset+cell )
    (block-VARIABLE:) cell+ cell+ ;

[DEFINED] floats [IF]
    : f-block-VARIABLE: ( "name" list-addr offset -- list-addr offset+fcell )
	(block-VARIABLE:) [ 1 floats ] literal + ;
[THEN]

\ Data blocks can be in data space, in allocated memory,...
\ I do not close them.
VARIABLE open-memory-block-xt		\ execute ( u -- address )

\ Having named functions for it has some advantages...
FALSE [IF] \ using new 'allocate-for-speed' instead.
    : allot-data-block ( u -- address=old-here )   here swap allot ;
    \ ' allot-data-block open-memory-block-xt !
    
    : allocate-memory ( u -- allocated-address )
	allocate  ABORT" allocate-memory: Couldn't allocate" ;
    \ ' allocate-memory open-memory-block-xt !

    \ Using insider knowledge of the current string buffer implementation...
    \ This should work with 'stringbuf-0.4.fs'
    \ Don't try to resize or close it please.
    \ No warranties.
    : open-stringbuf-memory ( u -- address-of-stringbuffer )
	stringbuf-open @ ;
    \ ' open-stringbuf-memory open-memory-block-xt !
[THEN]

\ Using new 'memory-speed-align.fs':
\ 'allocate-for-speed' allocates more than the required memory to take care
\ of alignement and pre/after padding requirements.
\ (see 'memory-speed-align.fs').
: allocate-for-speed ( u -- aligned-and-padded-address )
    allocate-for-speed nip ;
\ ' allocate-for-speed open-memory-block-xt !

\ Choose at compile time where memory should be taken from (see above):
\ (uncomment just one):
\ ' allot-data-block open-memory-block-xt !
\ ' allocate-memory open-memory-block-xt !
\ ' open-stringbuf-memory open-memory-block-xt !
' allocate-for-speed open-memory-block-xt !

\ End a variable declaration block and actually do compile the variables:
\ (this version compiles the addresses as CONSTANTs).
: define-block-variables ( list-addr offset -- )
    2>r				( r: list-address offset=size )

    r> ( offset=size ) open-memory-block-xt @ EXECUTE	( base-addr  r: list-a)

    r@  dup nodes  0 ?DO	( base-addr actual-node )
	c-l stringbuf-open >r		\ scratch evaluation buffer
	s" CONSTANT " r@ string!
	next-node
	dup @ string@ r@ cat		\ cat variable name
	dup @ stringbuf-close		\ close listed name buffer
	dup cell+ @ third +	( base-addr actual-node base-addr+offset )
	r@ string@ EVALUATE		\ define variable address as CONSTANT
	r> stringbuf-close		\ close scratch buffer
    LOOP
    2drop

    r> remove-list ;

    \ leftovers from initial testing:
    false [IF] \ testing a bit
	init-var-block
	block-VARIABLE: var-1
	block-VARIABLE: var-2
	9 cells +			\ var-2 is an array of 10 cells
	block-VARIABLE: var-3
	block-VARIABLE: var-4
	define-block-variables

	var-1 cr .
	var-2 cr .
	var-3 cr .
	see var-1
	see var-2
	see var-3
	see var-4
	cr .s cr
	key drop
	QUIT
    [THEN]

\ '\VARIABLE' '\2VARIABLE' are used to comment variables out at the point
\ they where defined in the source before using block variables for them.
\ This leaves the possibility to switch back to normal variables
\ defined at the original place to compare speed.
: \VARIABLE    bl word drop ;	IMMEDIATE
: \2VARIABLE   bl word drop ;	IMMEDIATE
[DEFINED] floats [IF]
    : \FVARIABLE    bl word drop ;	IMMEDIATE
[THEN]


[ELSE] \ don't use block variables

    \ Dummy functions.
    : init-var-block ( dummy dummy )   0 0 ;
    : define-block-variables ( dummy dummy -- )   2drop ;
    VARIABLE open-memory-block-xt	\ dummy, for benchmarking
    ' noop open-memory-block-xt !

    \ When and where will be the ordinary variables defined?

    \ Variables defined when registered for the blocks:
    dummy-block-variables 1 = [IF]

	: block-VARIABLE: ( "name" -- )    VARIABLE ;
	: block-2VARIABLE: ( "name" -- )   2VARIABLE ;
	: \VARIABLE    bl word drop ;	IMMEDIATE
	: \2VARIABLE   bl word drop ;	IMMEDIATE
	[DEFINED] floats [IF]
	    : f-block-VARIABLE: ( "name" -- )  FVARIABLE ;
	    : \FVARIABLE   bl word drop ;	IMMEDIATE
	[THEN]

    [ELSE]	\ variables defined when commented in the code

	: block-VARIABLE:    bl word drop ; IMMEDIATE
	: block-2VARIABLE:   bl word drop ; IMMEDIATE
        : \VARIABLE ( "name" -- )   VARIABLE ;
        : \2VARIABLE ( "name" -- )  2VARIABLE ;
	[DEFINED] floats [IF]
	    : f-block-VARIABLE:    bl word drop ; IMMEDIATE
	    : \FVARIABLE ( "name" -- )  FVARIABLE ;
	[THEN]

    [THEN] \ 'dummy-block-variables' values 1 or 2
[THEN] \ 'dummy-block-variables'
