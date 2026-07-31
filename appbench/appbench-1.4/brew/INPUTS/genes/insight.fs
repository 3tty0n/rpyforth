\ insight.fs
\ 	$Id: insight.fs,v 1.3 2002/04/17 11:48:22 f Exp $	

\ these genes give the possibility to read (only) some nuc-var's
\ so a cell can adapt it's behaviour to it's age, energy, reproduction-thres-
\ hold and the like.

get-current  also genes  definitions

: age@ ( -- n )   age @ ;
s" -n"  ' age@  as-gene
6000 to-gene-pool' age@

: age-threshold@ ( -- n )   age-threshold @ ;
s" -n"  ' age-threshold@  as-gene
3000 to-gene-pool' age-threshold@

: energy@ ( -- n )   energy @ ;
s" -n"  ' energy@  as-gene
6000 to-gene-pool' energy@

: reproduction-threshold@ ( -- n )  reprodctn-threshold @ ;
s" -n"  ' reproduction-threshold@  as-gene
6000 to-gene-pool' reproduction-threshold@

previous  set-current
