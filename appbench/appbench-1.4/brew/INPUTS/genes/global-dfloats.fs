\ genes/global-dfloats.fs
\ 	$Id: global-dfloats.fs,v 1.1 2002/07/11 15:24:05 f Exp $	

\ Gene primitives to read global dfloat variables.

decimal

\ Normally we want read only access:

: define-global-dfloat-df@-genes ( probability -- )
    32 stringbuf-open
    32 stringbuf-open
    global-dfloat-variables# 0 ?DO	( probability handle-name handle-eval )
	s" dfloat-"	fourth string!	\ build name
	i [char] A +		third char-cat
	s" -f@"			fourth cat	\ name ok
	s" : "			third string!	\ build evaluation buffer
	over string@		third cat
	bl over			char-cat
	over string@ 3 -	third cat
	s"  df@ ; "		third cat
	dup string@		EVALUATE	\ compile it
	over string@ get-xt  s" -r" rot as-gene	\ internal

	-rot			( handle-eval probability handle-name )
	2dup string@ gene-internals search-wordlist drop
	to-gene-pool
	rot			( probability handle-name handle-eval )
    LOOP

    stringbuf-close
    stringbuf-close
    drop ;

0 define-global-dfloat-df@-genes

