\ mixed-maths.fs
\ 	$Id: mixed-maths.fs,v 1.2 2002/11/14 13:32:32 f Exp $	

\ Some mixed integer/float basic arithmetic operator gene primitives.

GET-CURRENT  ALSO genes DEFINITIONS	\ good place for this definitions


: f+i ( r n -- r' )   s>f f+ ;
s" rn-r"  ' f+i  as-gene
5000 to-gene-pool' f+i

: f-i ( r n -- r' )   s>f f- ;
s" rn-r"  ' f-i  as-gene
5000 to-gene-pool' f-i

: f*i ( r n -- r' )   s>f f* ;
s" rn-r"  ' f*i  as-gene
5000 to-gene-pool' f*i

: f/i ( r n -- r' )   s>f f/ ;
s" rn-r"  ' f/i  as-gene
5000 to-gene-pool' f/i

: i/i ( n n -- r )   >r s>f r> s>f f/ ;
s" nn-r"  ' i/i  as-gene
2000 to-gene-pool' i/i


dfVARIABLE (f-scratch)

\ Not the fastest, but standard definitions ;-)
\ It would be better to check for separate float stack...

: i+f ( n r -- r' )   (f-scratch) df!  s>f  (f-scratch) df@  f+ ;
s" nr-r"  ' i+f  as-gene
5000 to-gene-pool' i+f

: i-f ( n r -- r' )   (f-scratch) df!  s>f  (f-scratch) df@  f- ;
s" nr-r"  ' i-f  as-gene
5000 to-gene-pool' i-f

: i*f ( n r -- r' )   (f-scratch) df!  s>f  (f-scratch) df@  f* ;
s" nr-r"  ' i*f  as-gene
5000 to-gene-pool' i*f

: i/f ( n r -- r' )   (f-scratch) df!  s>f  (f-scratch) df@  f/ ;
s" nr-r"  ' i/f  as-gene
5000 to-gene-pool' i/f


: f/f ( r r -- n )   f/ f>s ;
s" rr-n"  ' f/f  as-gene
2000 to-gene-pool' f/f

: f*f ( r r -- n )   f* f>s ;
s" rr-n"  ' f*f  as-gene
2000 to-gene-pool' f*f


: i*f>i ( n r -- n' )   (f-scratch) df!  s>f  (f-scratch) df@  f*  f>s ;
s" nr-n"  ' i*f>i  as-gene
1000 to-gene-pool' i*f>i

: i/f>i ( n r -- n' )   (f-scratch) df!  s>f  (f-scratch) df@  f/  f>s ;
s" nr-n"  ' i/f>i  as-gene
1000 to-gene-pool' i/f>i


PREVIOUS  SET-CURRENT
