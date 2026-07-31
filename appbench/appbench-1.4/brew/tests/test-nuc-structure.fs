\ 	$Id: test-nuc-structure.fs,v 1.2 2001/11/06 17:25:33 f Exp $	

\ There was a problem that let me write this to be sure everything is
\ like it should be.

\ It was, but who knows if it could become handy some day...

: test-nuc-structure ( -- )
    page
    ." Test of the current nuc structure: " cr

    cr
    0 cp!

    nuc-var-xts
    dup nodes 0 ?DO
	i . .tab
	next-node
	dup @
	dup xt>string
	2dup type
	2 4 screen-column
	EVALUATE . .tab
	EXECUTE . .tab
	i nuc-addr .
	i CASE
	    nuc-organs	OF ." nuc-organs"	ENDOF
	    nuc-parameters	OF ." nuc-parameters"	ENDOF
	    nuc-invisibles	OF ." nuc-invisibles"	ENDOF
	    nuc-secrets	OF ." nuc-secrets"	ENDOF
	ENDCASE
	cr
    LOOP
    drop

    nuc-var-xts nodes cells ." Expected nuc length:	" . cr
    nuc-length# ." Nominal nuc length:	" . ;

test-nuc-structure
