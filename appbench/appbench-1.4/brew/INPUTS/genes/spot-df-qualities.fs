\ spot-df-qualities.fs
\ 	$Id: spot-df-qualities.fs,v 1.1 2002/02/16 07:01:27 f Exp $	

decimal

: define-f-quality-aliases ( probability u -- )
    32 stringbuf-open

    swap 0 ?DO	( probability handle )
	\ Construct name:
	[char] A i +
	(name-buf) >r r@ stringbuf-empty  r@ char-cat
	s" -f-quality" r> cat

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
	to-gene-pool
    LOOP

    stringbuf-close
    drop ;

10000 spot-f-qualities# define-f-quality-aliases
