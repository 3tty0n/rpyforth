\ transform.fs
\ 	$Id: transform.fs,v 1.1 2002/02/14 12:10:31 f Exp $	

\ Transform float to single integer (and vice versa) gene primitives.


s" r-n" ' f>s  as-gene
5000 to-gene-pool' f>s

s" n-r" ' s>f  as-gene
5000 to-gene-pool' s>f
