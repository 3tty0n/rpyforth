\ genes/nuc-parameters.fs
\ 	$Id: nuc-parameters.fs,v 1.5 2002/05/21 21:03:08 f Exp $	

\ Read only variant of organs.

\ Designed for human readable output files.
\ I weight that more than efficiency here.

decimal

: define-parameter-genes ( probability -- )
    32 stringbuf-open
    32 stringbuf-open
    nuc-parameters# 0 ?DO	( probability handle-name handle-eval )
	s" parameter-"		fourth string!	\ build name
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
	dup >body  0  i nuc-organs# +  gene-n'th-mask-or!    \ set div bit mask
	to-gene-pool
	rot			( probability handle-name handle-eval )
    LOOP

    stringbuf-close
    stringbuf-close
    drop ;

10000 define-parameter-genes
