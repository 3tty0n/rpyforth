\ spot-df-properties.fs
\ 	$Id: spot-df-properties.fs,v 1.1 2002/02/16 07:01:47 f Exp $	

\ Read only variant of spot qualities.
\ Normal spot variables, but the cells have only read access.

\ Designed for human readable output files.
\ I weight that more than efficiency here.

decimal

: define-df-property-genes ( probability u -- )
    32 stringbuf-open
    32 stringbuf-open

    rot 0 ?DO	( probability handle-name handle-eval )
	over stringbuf-empty
	i [char] A +		third char-cat
	s" -f-property@"	fourth cat
	s" : "			third string!	\ build evaluation buffer
	over string@		third cat
	bl over			char-cat
	over string@ 1- 	third cat
	s"  df@ ; "		third cat
	dup string@	EVALUATE	\ compile it
	over string@ get-xt  s" -r" rot as-gene	\ internal

	>r
	2dup string@ gene-internals search-wordlist drop to-gene-pool
	r>
    LOOP

    stringbuf-close
    stringbuf-close
    drop ;

10000 spot-f-properties# define-df-property-genes
