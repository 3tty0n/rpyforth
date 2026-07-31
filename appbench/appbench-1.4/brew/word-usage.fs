\ word-usage.fs
\ 	$Id: word-usage.fs,v 1.10 2005/04/18 04:36:45 f Exp $	

\ Redefining classes of words to count how many times they got used:
\	'VARIABLE' '2VARIABLE'
\	'VALUE' 'TO'
\	'CONSTANT' '2CONSTANT'
\	':' ';'
\	':NONAME'
\	'DEFER' 'IS'

\ CREATE DOES> is experimental, still buggy, work in progress...

\ usage:
\ ' name xt>usage @ .		to see the count for a word named 'name'.
\ .variable-usage		to see all variables and such.
\ .colon-usage			for colon definitions, nonames, deferred words.
\ .all-usage			display results for all counted words.

\ ****************************************************************

\ switch counting on and off:
VARIABLE (don't-count-words)		(don't-count-words) off
: count? ( counter-addr -- counter-addr )
    (don't-count-words) @ IF  EXIT  THEN
    1 over +! ;

\ Get the count:
: xt>usage ( xt -- usage-counter-address )  >body ;

' DOES> CONSTANT DOES>-xt	\ use only interpreting!  


\ Hide the words from the included files to have them included again
\ in a counting version:
vocabulary profiling
also profiling definitions

s" system-dependent.fs" INCLUDED
s" compile-options.fs" INCLUDED
s" basics.fs" INCLUDED
s" lists.fs" INCLUDED
s" allocation-pointers.fs" INCLUDED
s" stringbuf-0.4.fs" INCLUDED
s" key-mapping.fs" INCLUDED
s" console-codes.fs" INCLUDED
s" sorted-lists.fs" INCLUDED

[UNDEFINED] get-name [IF] \ gets defined twice, but I don't mind...
    \ Get next word from input buffer and save it in a buffer, restore input.
    \ Don't forget to close the buffer.
    : get-name ( "name" -- "name"  handle )		\ Buffer must be closed
	save-input
	bl word count
	dup stringbuf-open >r r@ cat
	restore-input  ABORT" get-name: Error restoring input source."
	r> ;
[THEN]

[UNDEFINED] get-xt [IF] \ gets defined twice, but I don't mind...
    : get-xt ( addr count -- xt )
	[ decimal ] 32 stringbuf-open >r
	s" ' " r@ cat  r@ cat  r@ string@ EVALUATE
	r> stringbuf-close ;
[THEN]


\ ****************************************************************
\ Copy usage counts into list:
: counts>list ( list -- )
    dup nodes 0 ?DO			( actual-node )
	next-node
	dup @				( actual-node xt )
	xt>usage @ over cell+ !		\ usage counter to list [2]
    LOOP
    drop ;

\ Display usage counts and names of a sorted list:
: display-sorted-usage-list ( list -- )
    dup nodes 0 ?DO
	next-node
	cr dup cell+ @ .
	dup @ xt>string 11 at? nip at-xy type
    LOOP
    drop ;

\ Setup, sort and do display usage counts and names of a list:
: .usage-counts ( list -- )
    dup counts>list
    1 swap copy-to-sorted-list
    dup display-sorted-usage-list
    remove-list
    cr ;


\ ****************************************************************
\ Counting how many times VARIABLEs, 2VARIABLEs, or VALUEs get used:

\ List to hold the xt's and (if '.variable-usage' is used) the counts.
2 nLIST: variables

\ A VARIABLE that counts how many times it was executed:
\ The xt's get stored in a list for later analysis.
: counting-VARIABLE: ( "name" -- )
    get-name >r		( "name"  r: name-handle )
    CREATE
	0 ,				\ usage counter
	0 ,				\ variable
	r@ string@ get-xt variables >list
	r> stringbuf-close
    DOES> ( -- address )
	count?
	cell+ ;

\ 2VARIABLE counting how many time it was executed:
: counting-2VARIABLE: ( "name" -- )
    get-name >r		( "name"  r: name-handle )
    CREATE
	0 ,				\ usage counter
	0 ,				\ variable
	0 ,				\ second cell
	r@ string@ get-xt variables >list
	r> stringbuf-close
    DOES> ( -- address )
	count?
	cell+ ;


\ A VALUE that counts how many times it was executed:
\ The xt's get stored in the same list as VARIABLEs for later analysis.
: counting-VALUE: ( "name" value -- )
    get-name >r		( "name"  r: name-handle )
    CREATE
	0 ,				\ usage counter
	,				\ value
	r@ string@ get-xt variables >list
	r> stringbuf-close
    DOES> ( -- value )
	count?
	cell+ @ ;

\ Corresponding 'TO':
: counting-TO ( "name" value -- )
    bl word find IF
	xt>usage
	count?
	cell+
	state @ IF
	    POSTPONE literal POSTPONE !
	ELSE
	    !
	THEN
    ELSE
	ABORT" redefined 'TO' couldn't find word."
    THEN ;

\ A constant, that counts usage:
: counting-CONSTANT: ( "name" value -- )   counting-VALUE: ;

\ 2CONSTANT that counts how many times it was executed:
\ The xt's get stored in the same list as VARIABLEs for later analysis.
: counting-2CONSTANT: ( "name" d-value -- )
    get-name >r		( "name"  r: name-handle )
    CREATE
	0 ,				\ usage counter
	,				\ value
	,
	r@ string@ get-xt variables >list
	r> stringbuf-close
    DOES> ( -- d-value )
        count?
	cell+ 2@ ;


\ ****************************************************************
\ Counting colon definitions and such:

VARIABLE (last-name-buffer)	\ preserves name from ':' to ';'

\ Preserve original ':' and ';'
: ;-original POSTPONE ; ;  IMMEDIATE
: original-: : ;

\ List to hold the xt's and (if '.variable-usage' is used) the counts.
2 nLIST: colon-definitions

\ Start a counting colon definition:
: counting-: ( "name"  -- )   get-name (last-name-buffer) !  : ;

\ Corresponding ';'
: counting-; ( colon-sys -- )
    POSTPONE ;
    80 stringbuf-open
    s" original-CREATE "				third string!
    (last-name-buffer) @ string@			third cat
    s"  0 , "						third cat
    (last-name-buffer) @ string@ get-xt num>string	third cat
    s"  , DOES>-xt EXECUTE count? cell+ @ EXECUTE " third cat
    s" ;-original "					third cat
    dup string@ EVALUATE
    stringbuf-close
    (last-name-buffer) @ dup string@ 2dup get-xt colon-definitions >list
    s" NONAME-" search IF
	get-xt swap
    ELSE 2drop THEN
    stringbuf-close
; IMMEDIATE

\ Produce unique names for former unnamed :NONAME definitions:
VARIABLE (noname-count)		(noname-count) off

\ Counting :NONAME definitions:
\ They actually *do* get a name here...
: :NONAME-counting
    80 stringbuf-open
    s" counting-: NONAME-"	third string!
    (noname-count) @ num>string	third cat
    bl				over char-cat
    >r r@ string@ EVALUATE
    r> stringbuf-close
    (noname-count) count? drop ;

\ Counting 'DEFER':
\ Not only the DEFERred word get's counted, but also the executed one.    
: counting-DEFER ( "name" -- )
    80 stringbuf-open >r
    s" counting-: "		r@ string!
    bl word count		r@ cat
    s"  counting-; "		r@ cat
    r@ string@ EVALUATE
    r> stringbuf-close ;

\ Corresponding 'IS' ( same as counting 'TO').
: counting-IS ( "name" xt -- )
    bl word find IF
	xt>usage
	count?
	cell+
	state @ IF
	    POSTPONE literal POSTPONE !
	ELSE
	    !
	THEN
    ELSE
	ABORT" redefined 'IS' couldn't find word."
    THEN ;


\ ****************************************************************
\ I use a pending stack to compile counting versions of DOES> words
\ It could be used for other types of word xt's too
VARIABLE (pending-data)
VARIABLE (pending-stack-pointer)	(pending-stack-pointer) off

128 CONSTANT pending-stack-size#
pending-stack-size# cells allocate THROW (pending-data) !

: push-pending ( xt -- )
    (pending-stack-pointer) @  pending-stack-size# =
    ABORT" word-usage.fs push-pending stack is full!"

    (pending-data) @  (pending-stack-pointer) @ cells  + !
    1 (pending-stack-pointer) +! ;

: pop-pending? ( -- xt true | false )
    (pending-stack-pointer) @ 0= IF  FALSE EXIT  THEN

    -1 (pending-stack-pointer) +!
    (pending-data) @  (pending-stack-pointer) @ cells  + @
    TRUE ;

2 nLIST: create-does-words

\ Compile counting versions of all the words that have xt's left
\ on the pending stack and put the xt's of the counting version
\ in the create-does-words list.
: compile-pending ( -- )
    (pending-stack-pointer) @  0= IF EXIT THEN

    BEGIN
	pop-pending? 
    WHILE
	>r
	80 stringbuf-open
	s" original-CREATE "				third string!
	r@ xt>string					third cat
	s"  0 , "					third cat
	r@ num>string					third cat
	s"  , DOES>-xt EXECUTE count? cell+ @ EXECUTE "	third cat
	s" ;-original "					third cat
	dup string@ EVALUATE
	stringbuf-close
	r> xt>string get-xt create-does-words >list
    REPEAT ;


\ ****************************************************************
\ Counting CREATE DOES> words:

VARIABLE (last-created-name)
\ 2 nLIST: create-does-words


also FORTH definitions		\ words visible from outside:

: CREATE
    state @ 0= IF  CREATE  EXIT THEN

    POSTPONE get-name POSTPONE (last-created-name) POSTPONE !
    POSTPONE CREATE ;  IMMEDIATE


: DOES>
    POSTPONE (last-created-name) POSTPONE @
    POSTPONE string@  POSTPONE get-xt
    POSTPONE push-pending
    POSTPONE DOES> ;  IMMEDIATE


\ ****************************************************************


\ ****************************************************************
\ These must be visible from outside:
: ;-original POSTPONE ;-original ; IMMEDIATE
: counting-; POSTPONE counting-; ; IMMEDIATE
: original-CREATE ( "name"  -- )   CREATE ; IMMEDIATE


: counting-: counting-: ;
: original-: original-: ;

\ Disable 'MARKER' to avoid problems with forgotten words.
\ 'MARKER' defining a noop word.
: MARKER ( "name" -- )
    get-name >r
    80 stringbuf-open
    s" : "		third string!
    r@ string@		third cat
    s"  ; "		third cat
    dup string@ EVALUATE
    stringbuf-close
    r> stringbuf-close ;


\ ****************************************************************
\ User words:
\ xt>usage	defined above, visible.

\ Display usage count and name of all VARIABLEs, 2VARIABLEs, VALUES,
\ CONSTANTs ans 2CONSTANTs:
: .variable-usage ( -- )
    page
    ." Displaying usage of VARIABLEs, 2VARIABLEs, VALUES, CONSTANTs ans 2CONSTANTs:"
    cr
    variables .usage-counts ;

\ Display a list of all colon words (e.a.) usage counts and names:
: .colon-usage ( -- )
    page
    ." Displaying usage counts of colon words (e.a.):"
    cr
    colon-definitions .usage-counts ;

\ Display a list of all CREATE DOES> words (e.a.) usage counts and names:
: .create-does>-usage ( -- )
    page
    ." Displaying usage counts of CREATE DOES> words:"
    cr
    create-does-words .usage-counts ;

\ Display a list of all counted words:
: .all-usage ( -- )
    page
    ." Displaying usage count of all profiled words:"
    cr
    
    2 deflist

    variables counts>list
    variables
    dup nodes 0 ?DO
	next-node
	dup 1 fourth insert-node-sorted
    LOOP
    drop

    colon-definitions counts>list
    colon-definitions
    dup nodes 0 ?DO
	next-node
	dup 1 fourth insert-node-sorted
    LOOP
    drop

    create-does-words counts>list
    create-does-words
    dup nodes 0 ?DO
	next-node
	dup 1 fourth insert-node-sorted
    LOOP
    drop

    dup display-sorted-usage-list
    remove-list
    cr ;


\ ****************************************************************
\ Actually replace some FORTH standard words now with counting variations:
: VARIABLE ( "name" -- )    	   compile-pending counting-VARIABLE: ;
: 2VARIABLE ( "name" -- )   	   compile-pending counting-2VARIABLE: ;
: VALUE ( "name" value -- )        compile-pending counting-VALUE: ;
: TO ( "name" value -- )   	   compile-pending counting-TO ; IMMEDIATE
: CONSTANT ( "name" value -- )     compile-pending counting-CONSTANT: ;
: 2CONSTANT ( "name" d-value -- )  compile-pending counting-2CONSTANT: ;

: DEFER ( "name" -- )		   compile-pending counting-DEFER ;
: IS ( "name" xt -- )		   compile-pending counting-IS ; IMMEDIATE
: :NONAME ( "name" -- )		   compile-pending :NONAME-counting ;

: ; POSTPONE counting-; compile-pending ; IMMEDIATE
: : compile-pending counting-: ;-original

\ ****************************************************************
previous previous definitions
