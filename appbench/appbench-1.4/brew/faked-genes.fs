\ faked-genes.fs
\ 	$Id: faked-genes.fs,v 1.3 2002/11/16 09:49:01 f Exp $	

\ Some genes produced different result on different FORTHs.
\ This file is for debugging such genes, providing a faked test environment.
\ You can give the gene and parameters in the form the log file provides them.

s" system-dependent.fs" REQUIRED

VARIABLE A-quality
VARIABLE B-quality
VARIABLE C-quality

: qualities! ( A B C -- )	\ cut&paste A B C from log file.
    C-quality !
    B-quality !
    A-quality ! ;

VARIABLE organ-A
VARIABLE organ-B
VARIABLE organ-C
VARIABLE organ-D
VARIABLE organ-E
VARIABLE organ-F
VARIABLE organ-G

: organs! ( A B C D E F G -- )	\ cut&paste A B C D E F G from log file.
    organ-G !
    organ-F !
    organ-E !
    organ-D !
    organ-C !
    organ-B !
    organ-A ! ;
    
VARIABLE <food>
VARIABLE energy

: skip-string ( -- )   [char] " parse 2drop ;

: start" ( -- )   skip-string ; immediate
: end" ( -- )   skip-string ; immediate
: instack" ( -- )   skip-string ; immediate
: outstack" ( -- )   skip-string ; immediate

: .results
    cr s" organs: "
    organ-A @ .
    organ-B @ .
    organ-C @ .
    organ-D @ .
    organ-E @ .
    organ-F @ .
    organ-G @ .
    ." 	energy w/o costs: " energy @ .
    cr s" qualities: "
    A-quality @ .
    B-quality @ .
    C-quality @ .
    ." 	<food>: " <food> @ .
    cr ;

: GENE:			: ;		immediate
: ;gene			POSTPONE ; ;	immediate
: >internal		2drop ;
: primitive>internal	2drop ;
: last-gene-into-pool	drop ;
: ?/ ( n n -- n|0 )   ['] / CATCH IF 2drop 0 THEN ;

INCLUDE genes/genes-conditionals.fs
INCLUDE genes/genes-basic-stack.fs
INCLUDE genes/genes-basic-arithmetics.fs
INCLUDE genes/genes-organs.fs
INCLUDE genes/genes-qualities.fs
INCLUDE genes/genes-fetch.fs
INCLUDE genes/genes-store-normalised.fs
INCLUDE genes/genes-store.fs

false [if] \ genes/insight.fs needs some more variables:
    VARIABLE age
    VARIABLE age-threshold
    VARIABLE reprodctn-threshold

    INCLUDE genes/insight.fs
[then]

: ;gene ; \ redefined to noop

false [if] \ you could use this to cut&paste for initialization

    organ-A !
    organ-B !
    organ-C !
    organ-D !
    organ-E !
    organ-F !
    organ-G !

    A-quality !
    B-quality !
    C-quality !

    <food> !
    energy !

    false [if] \ genes/insight.fs needs some more variables:
	age !
	age-threshold !
	reprodctn-threshold !
    [then]

[then]

\ How to use it:
\ Do initialization (see 'before:' tag in log file):
\ A B C qualities!
\ <food> !
\ A B C D E F G organs!
\ energy !

\ Insert gene here ( from log file ):

\ Insert eating procedure here:

\ Display results:
.results bye
