\ float-stack.fs
\ 	$Id: float-stack.fs,v 1.3 2002/05/21 21:04:19 f Exp $	

\ Floating point stack manipulation gene primitives.


s" r-rr"  ' fdup  as-gene
6000 to-gene-pool' fdup

s" r-" ' fdrop  as-gene
2000 to-gene-pool' fdrop

s" A-" ' drop  GENE-ALIAS: drop(float-pointer)
2000 to-gene-pool' drop(float-pointer)
as-alternative'' drop(float-pointer) drop

s" rr-rr" ' fswap  as-gene
8000 to-gene-pool' fswap

s" rr-rrr" ' fover  as-gene
7000 to-gene-pool' fover

s" rrr-rrr" ' frot  as-gene
0 to-gene-pool' frot
