\ store.fs
\ 	$Id: store.fs,v 1.2 2001/03/21 07:01:13 f Exp $	

\ attention, these genes tend to make overflow problems...

get-current  also genes  definitions

s" na-"	' ! as-gene
2000 to-gene-pool' !

: -! ( n a -- )  swap negate swap +! ;
s" na-"  ' -!  as-gene
2000 to-gene-pool' -!

: swap! ( a n -- )  swap ! ;
s" an-"  ' swap!  as-gene
2000 to-gene-pool' swap!

previous  set-current
