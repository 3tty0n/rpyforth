\ float-more-arithmetics.fs
\ 	$Id: float-more-arithmetics.fs,v 1.3 2002/03/19 13:55:50 f Exp $	
\ More floating point operator primitives.
\  fnegate, fabs, fmax, fmin, f2*, f2/, 1/f.


s" r-r"	 ' fnegate  as-gene
4000 to-gene-pool' fnegate

s" r-r"  ' fabs  as-gene
2000 to-gene-pool' fabs

s" rr-r"  ' fmax  as-gene
2000 to-gene-pool' fmax

s" rr-r"  ' fmin  as-gene
2000 to-gene-pool' fmin

s" r-r" ' f2*  as-gene
500 to-gene-pool' f2*

s" r-r" ' f2/  as-gene
500 to-gene-pool' f2/

s" r-r" ' 1/f  as-gene
2500 to-gene-pool' 1/f
