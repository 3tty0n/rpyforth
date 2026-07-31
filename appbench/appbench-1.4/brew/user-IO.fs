\ user-IO.fs
\ 	$Id: user-IO.fs,v 1.1 2002/11/15 23:18:00 f Exp $	

\ Advanced keyboard I/O words.

\ ****************************************************************
\ file dependencies
s" display.fs" REQUIRED

\ ****************************************************************


\ Wait some time (at least) if key? gets true
\ (on my system it waits about 3 times as long, but I don't care)
: await-key? ( milliseconds -- true|false )
    BEGIN	( milliseconds )
	key? 0= IF  2 ms THEN
	key? 0= IF  3 ms THEN
	key? 0= IF  5 ms THEN
	key? IF drop true EXIT THEN
	10 -
	dup 1 < IF drop  false EXIT THEN
    AGAIN ;

\ Wait interruptable.
: wait-until ( time -- )   await-key? drop
    BEGIN
	10 ms
	key? WHILE
	key drop
    REPEAT ;

: (do-FORTH)				\ FORTH interpreter for menus
   last-left clear-line-to-end
   last-left ." FORTH: "
   pad dup 80 7 - ACCEPT  EVALUATE ;
: do-FORTH
    ['] (do-FORTH) CATCH IF bell THEN
    key
    last-left clear-line-to-end
    bl = IF RECURSE THEN ;

: (accept-evaluate)   pad dup 80 ACCEPT  EVALUATE ;

: accept-evaluate ( ... -- ... error-code )
    ['] (accept-evaluate) CATCH dup IF bell THEN ;


: .ON-off ( flag -- )   IF ." is ON " ELSE ." is off" THEN ;

: .YES-NO ( flag -- )   IF ." YES" ELSE ." NO" THEN ;
