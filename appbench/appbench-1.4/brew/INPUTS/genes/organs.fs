\ organs.fs
\ 	$Id: organs.fs,v 1.3 2002/05/21 21:03:34 f Exp $	

decimal

: define-organ-aliases ( probability -- )
    32 stringbuf-open

    nuc-organs# 0 ?DO	( probability handle )
	\ Construct name:
	(name-buf) >r r@ stringbuf-empty
	s" organ-" r> cat
	[char] A i +  (name-buf) char-cat

	\ Compile gene alias:
	dup >r
	r@ stringbuf-empty
	s" ' " r@ cat
	(name-buf) string@ 2dup r@ cat
	s"  GENE-ALIAS: " r@ cat  r@ cat
	s" -a" r@ string@ EVALUATE

	\ Get internals xt:
	r@ stringbuf-empty
	s" internal' " r@ cat
	(name-buf) string@ r@ cat
	over r> string@ EVALUATE	( probability handle prob xt )

	dup >body  0  i gene-n'th-mask-or!	\ set diversification bit mask
	to-gene-pool				\ put it into pool
    LOOP

    stringbuf-close
    drop ;

10000 define-organ-aliases
