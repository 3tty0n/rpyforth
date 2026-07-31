\ dfloat-store.fs
\ 	$Id: dfloat-store.fs,v 1.4 2002/11/14 13:32:20 f Exp $	

\ dfloat store gene primitives.
\ Attention, these genes tend to produce overflow.

GET-CURRENT  ALSO genes  DEFINITIONS	\ good place for this definitions


s" rA-"	' df! as-gene
10000 to-gene-pool' df!

\ : df+! ( r addr -- )   >r  r@ df@ f+ r> df! ;		\ defined in basics.fs
s" rA-" ' df+! as-gene
4000 to-gene-pool' df+!

: df-! ( r addr -- )   >r  r@ df@ f- r> df! ;
s" rA-" ' df-! as-gene
4000 to-gene-pool' df-!


PREVIOUS  SET-CURRENT
