\ float-organs.fs
\ 	$Id: float-organs.fs,v 1.2 2002/05/21 21:02:05 f Exp $	

\ Define named dfloat organ gene primitives.


decimal

: define-f-organ-aliases ( probability -- )
    32 stringbuf-open

    nuc-f-organs# 0 ?DO	( probability handle )
	\ Construct name:
	s" f-organ-" (name-buf) string!
	[char] A i +  (name-buf) char-cat

	\ Compile gene alias:
	dup >r
	r@ stringbuf-empty
	s" ' " r@ cat
	(name-buf) string@ 2dup r@ cat
	s"  GENE-ALIAS: " r@ cat  r@ cat
	s" -A" r@ string@ EVALUATE

	\ Put it into pool:
	r@ stringbuf-empty
	s" internal' " r@ cat
	(name-buf) string@ r@ cat
	over r> string@ EVALUATE	( probability handle prob xt )
	dup >body  1  i gene-n'th-mask-or!		\ set div bit mask
	to-gene-pool
    LOOP

    stringbuf-close
    drop ;


10000 define-f-organ-aliases
