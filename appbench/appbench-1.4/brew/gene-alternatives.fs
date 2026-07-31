\ gene-alternatives.fs
\ 	$Id: gene-alternatives.fs,v 1.1 2002/05/17 22:00:29 f Exp $	

\ Gene alternatives: genes having the same compiled (and interpreted) xt,
\ but different stack effect like  drop  and  drop(a-).  Mutation knows
\ which one to take, but building the internals gene from user input code
\ must guess which one is meant.  So I put alternatives in a list uf sublists.
\ The second data item of the top list nodes are the xt's that will be found by
\ internal' <name>  which are used as keys to search:

\ Search list nodes for key being in the second data field:
: second-data? ( key list -- node | FALSE )
    dup nodes 0 ?DO
	next-node
	over over cell+ @ = IF  nip unloop EXIT  THEN
    LOOP
    2drop FALSE ;

2 nLIST: (gene-alternatives)
\ Test if internal xt has registered alternatives:
: gene-has-alternatives? ( xt -- alternatives-sublist | FALSE )
    (gene-alternatives) second-data?
    dup 0= IF  ( FALSE ) EXIT  THEN

    @ ;

\ Set gene internal xt as an alternative of the second internal xt.
: is-gene-alternative ( xt-alternative xt-basic-gene -- )
    dup gene-has-alternatives? dup 0= IF
	drop
	1 deflist (gene-alternatives) list>list
	dup (gene-alternatives) last-node cell+ !
	dup gene-has-alternatives?
    THEN nip
    >list ;

: as-alternative'' ( "name-alternative name-basic-gene" -- )	\ parsing word
    internal' internal' is-gene-alternative ;

\ Test internal xt for stack match:
: xt-stack-match? ( xt -- flag )  >body  dup >gene-stack-in 2@ symbols-match ;

\ Test if a gene matches or has an alternative that matches stack:
: matching-gene-alternative? ( xt -- xt' TRUE | FALSE )
    dup xt-stack-match? IF  TRUE  EXIT  THEN

    gene-has-alternatives? dup IF
	dup nodes 0 ?DO
	    next-node
	    dup @ xt-stack-match? IF
		@ TRUE unloop EXIT
	    THEN
	LOOP
	drop FALSE
    THEN ;
