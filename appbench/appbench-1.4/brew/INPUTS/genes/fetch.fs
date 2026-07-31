\ fetch.fs
\ 	$Id: fetch.fs,v 1.2 2001/03/21 07:00:31 f Exp $	

get-current  also genes definitions

s" a-n"	' @ GENE-ALIAS: @
10000 to-gene-pool' @

: take ( a -- n )  dup @ swap off ;
s" a-n"  ' take  as-gene
10000 to-gene-pool' take

previous  set-current
