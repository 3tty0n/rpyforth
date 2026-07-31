\ float-comparison.fs
\ 	$Id: float-comparison.fs,v 1.2 2002/03/20 17:04:56 f Exp $	

\ Float comparison gene primitives.


s" rr-C" ' f< as-gene
2000 to-gene-pool' f<

s" rr-C" ' f> as-gene
2000 to-gene-pool' f>

s" r-C" ' f0< as-gene
2000 to-gene-pool' f0<

s" r-C" ' f0= as-gene
200 to-gene-pool' f0=
