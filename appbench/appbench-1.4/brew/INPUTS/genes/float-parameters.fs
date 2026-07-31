\ float-parameters.fs
\ 	$Id: float-parameters.fs,v 1.2 2002/05/21 21:02:34 f Exp $	

\ Define dfloat parameter read gene primitives.


decimal

: define-f-parameter-genes ( probability -- )
    32 stringbuf-open
    32 stringbuf-open
    nuc-f-parameters# 0 ?DO	( probability handle-name handle-eval )
	s" f-parameter-"	fourth string!	\ build name
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
	dup >body  2  i nuc-f-organs# + gene-n'th-mask-or!   \ set div bit mask
	to-gene-pool
	rot			( probability handle-name handle-eval )
    LOOP

    stringbuf-close
    stringbuf-close
    drop ;


10000 define-f-parameter-genes
