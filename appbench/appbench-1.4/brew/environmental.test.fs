\ environmental.test.fs
\ 	$Id: environmental.test.fs,v 1.2 2003/02/17 06:49:44 f Exp $	

\ Check for some environmental dependencies:
\ *  'Brew' depends on allocated memory to be dfaligned.
\ *  'Brew' depends on a separate float stack.

MARKER forget-it

: allocate-dfaligned? ( -- error-flag )
    100 1 DO
	i allocate ABORT" allocate-dfaligned?: Could not allocate"
	dup dup faligned <> IF  drop unloop TRUE EXIT  THEN
    LOOP

    100 1 DO
	free ABORT" allocate-dfaligned?: Could not free."
    LOOP

    FALSE ;

: .my-email-address ( -- )
    cr ." You reach me under the following address:"
    cr ." Robert Epprecht <epprecht@solnet.ch>" ;

allocate-dfaligned? dup [IF]
    bell cr cr
    .(    UNMET ENVIRONMENTAL DEPENDENCY ALERT: ) cr
    .( 'Brew' depends on allocated memory to be dfaligned! ) cr
    .( Please *do* inform the author about the problem and which OS you use.)
    cr
[THEN]	( allocation-error-flag )

: separate-float-stack? ( -- error-flag )
    depth >r
    0e0
    r@ 1+ depth <> IF
	BEGIN drop depth r@ = UNTIL
	rdrop TRUE EXIT
    ELSE fdrop THEN

    -1 pi -1 <> IF
	BEGIN drop depth r@ = UNTIL
	rdrop TRUE EXIT
    ELSE fdrop rdrop THEN
    
    FALSE ;

separate-float-stack? dup [IF]
    bell cr cr
    .(    UNMET ENVIRONMENTAL DEPENDENCY ALERT: ) cr
    .( 'Brew' depends on a separate float stack.) cr
    .( Please *do* inform the author about the problem and which Forth system you use.)
    cr
[THEN]

or [IF] .my-email-address cr cr bye [THEN]

forget-it
