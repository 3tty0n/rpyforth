\ dfloat-fetch.fs
\ 	$Id: dfloat-fetch.fs,v 1.6 2002/11/14 13:32:05 f Exp $	

\ Fetch dfloat gene primitives.

GET-CURRENT  ALSO genes  DEFINITIONS	\ good place for this definitions

s" A-r"	' df@  as-gene
20000 to-gene-pool' df@

: f-take ( a -- r )   dup >r  df@   0e0 r> df! ;

s" A-r"  ' f-take  as-gene
10000 to-gene-pool' f-take

PREVIOUS  SET-CURRENT
