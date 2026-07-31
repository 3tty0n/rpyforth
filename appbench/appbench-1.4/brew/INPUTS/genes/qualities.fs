\ qualities.fs
\ 	$Id: qualities.fs,v 1.2 2001/08/26 11:35:22 f Exp $	

decimal

: define-quality-aliases ( probability -- )
    32 stringbuf-open

    spot-qualities# 0 ?DO	( probability handle )
	\ Construct name:
	[char] A i +
	(name-buf) >r r@ stringbuf-empty  r@ char-cat
	s" -quality" r> cat

	\ Compile gene alias:
	dup >r
	r@ stringbuf-empty
	s" ' " r@ cat
	(name-buf) string@ 2dup r@ cat
	s"  GENE-ALIAS: " r@ cat  r@ cat
	s" -a" r@ string@ EVALUATE

	\ Put it into pool:
	r@ stringbuf-empty
	s" internal' " r@ cat
	(name-buf) string@ r@ cat
	over r> string@ EVALUATE	( probability handle prob xt )
	to-gene-pool
    LOOP

    stringbuf-close
    drop ;

10000 define-quality-aliases
