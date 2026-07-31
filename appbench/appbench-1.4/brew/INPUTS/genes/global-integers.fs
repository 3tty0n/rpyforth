\ genes/global-integers.fs
\ 	$Id: global-integers.fs,v 1.1 2002/07/11 15:24:54 f Exp $	

\ Gene primitives to read global integer variables.

decimal

\ Normally we want read only access:
: define-global-int@-genes ( probability -- )
    32 stringbuf-open
    32 stringbuf-open
    global-integer-variables# 0 ?DO	( probability handle-name handle-eval )
	s" integer-"		fourth string!	\ build name
	i [char] A +		third char-cat
	[char] @		third char-cat	\ name ok
	s" : "			third string!	\ build evaluation buffer
	over string@		third cat
	bl over			char-cat
	over string@ 1-		third cat
	s"  @ ; "		third cat
	dup string@		EVALUATE	\ compile it
	over string@ get-xt  s" -n" rot as-gene	\ internal

	-rot			( handle-eval probability handle-name )
	2dup string@ gene-internals search-wordlist drop
	to-gene-pool
	rot			( probability handle-name handle-eval )
    LOOP

    stringbuf-close
    stringbuf-close
    drop ;

0 define-global-int@-genes
