\ spot-properties.fs
\ 	$Id: spot-properties.fs,v 1.4 2001/08/26 11:35:27 f Exp $	

\ Read only variant of spot qualities.
\ Normal spot variables, but the cells have only read access.

\ Designed for human readable output files.
\ I weight that more than efficiency here.

decimal

: define-property-genes ( probability -- )
    32 stringbuf-open
    32 stringbuf-open

    spot-properties# 0 ?DO	( probability handle-name handle-eval )
	over stringbuf-empty
	i [char] A +	third char-cat
	s" -property@"	fourth cat
	s" : "		third string!	\ build evaluation buffer
	over string@	third cat
	bl over		char-cat
	over string@ 1- third cat
	s"  @ ; "	third cat
	dup string@	EVALUATE	\ compile it
	over string@ get-xt  s" -n" rot as-gene	\ internal

	>r
	2dup string@ gene-internals search-wordlist drop to-gene-pool
	r>
    LOOP

    stringbuf-close
    stringbuf-close
    drop ;

10000 define-property-genes
