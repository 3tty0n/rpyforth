\ float-exp.fs
\ 	$Id: float-exp.fs,v 1.1 2002/02/14 11:43:47 f Exp $	

\ Exponential, root and logarithm gene primitives.


s" rr-r" ' f**  as-gene
1000 to-gene-pool' f**

s" r-r" ' fsqrt  as-gene
1000 to-gene-pool' fsqrt

s" r-r" ' fexp  as-gene
1000 to-gene-pool' fexp

s" r-r" ' fexpm1  as-gene
500 to-gene-pool' fexpm1

s" r-r" ' fln  as-gene
1000 to-gene-pool' fln

s" r-r" ' flnp1  as-gene
1000 to-gene-pool' flnp1

s" r-r" ' flog  as-gene
200 to-gene-pool' flog

s" r-r" ' falog  as-gene
200 to-gene-pool' falog
