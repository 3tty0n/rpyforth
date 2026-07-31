\ debuging.fs
\ 	$Id: debuging.fs,v 1.11 2002/11/14 13:29:53 f Exp $	
\ help debugging ;-)

: DADA ( ... -- ... )		\ private debugging help
    bell cr ." DADA" cr
    base @ >r  decimal .s cr  r> base !
    [ decimal ] 1024 allocate ABORT" DADA: Could not allocate." >r
    BEGIN
	cr
	r@ 1024 accept
	cr
    dup WHILE
	r@ swap EVALUATE
    REPEAT
    drop
    r> free ABORT" dada: Could not free." ;

[UNDEFINED] debugging [IF]
    VARIABLE debugging	debugging off	\ 'debug' as name is taken
[THEN]

VARIABLE halt		halt off

FALSE [IF]
    \ watch for a given step and stop, and set halt there:
    2variable (watch-spot)		-1. (watch-spot) 2!
    : watch-off ( -- )
	-1. (watch-spot) 2!
	s" ' noop IS <spot-do> " EVALUATE ;	\ just in case there is no [IS]

    : >watch-spot ( step spot )
	(watch-spot) 2!
	s" ' watch-spot IS <spot-do> " EVALUATE ;    \ in case there is no [IS]

    : watch-spot ( -- )  step @  spot @  (watch-spot) 2@  d= IF halt on THEN ;
[THEN]
