\ bcompat-11-12.fs
\ 	$Id: bcompat-11-12.fs,v 1.2 2005/05/10 12:25:38 f Exp $	

\ Downwards compatibility to run
\ 'transit-11-bench-A' and 'transit-12-bench'

\ Orientation in space:
\ Normalise spot index.  This universe wraps at it's borders.
: i-normalize ( i offset -- i' )  + spots mod ;

: left-spot  ( i -- i' )    spots 1-	i-normalize ;	\ one step to the left
: right-spot ( i -- i' )    1		i-normalize ;	\ one step to the right
: upper-spot ( i -- i' )    spots c-l -	i-normalize ;	\ one step up
: lower-spot ( i -- i' )    c-l        	i-normalize ;	\ one step down

8 CONSTANT directions			\ we have 8 directions
directions 8 = [IF]			\ 4 or 8 directions?
\ 8 CONSTANT directions			\ we have 8 directions

: direction>index ( direction -- i')	\ spot must be set
    directions mod			\ ( normalized-direction )
    spot @ swap				\ ( actual-index normalized-direction )
    CASE
	0 OF upper-spot			ENDOF
	1 OF upper-spot	right-spot	ENDOF
	2 OF right-spot			ENDOF
	3 OF right-spot	lower-spot	ENDOF
	4 OF lower-spot			ENDOF
	5 OF lower-spot	left-spot	ENDOF
	6 OF left-spot			ENDOF
	left-spot	upper-spot
    ENDCASE ;
[THEN]

: someone-here? ( i -- addr|false)	\ checks if a spot is occupied
    spot @ swap	( spot i )		\ remember spot
    >spot! fcp @  ( spot a|false )	\ return value on TOS
    swap >spot! ;			\ go back to actual spot

: world-free-neighbour-spot? ( -- i' true | false ) \ free neighboring spot?
    directions random-ranged >r			\ ( R: random-direction )
    0						\ ( z=0 )
    BEGIN					\ ( z ) test all directions
	dup r@ +				\ ( z z+random-direction )
	direction>index				\ ( z i' )
	dup someone-here?			\ ( z i' flag )
	0= IF					\ ( z i'ok )
	    dup future someone-here? present 0= IF
		rdrop nip true EXIT		\ RETURN ( -- i' true )
	    THEN
	THEN drop				\ ( z )
	1+ dup directions < WHILE		\ ( z+1 )
    REPEAT drop
    rdrop false ;				\ RETURN ( -- false )
