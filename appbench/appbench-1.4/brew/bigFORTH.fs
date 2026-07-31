\ bigFORTH.fs
\ 	$Id: bigFORTH.fs,v 1.13 2005/05/22 05:02:17 f Exp $	

\ bigFORTH specific things.     Work in progress...

\ Note that there's another modification 'screen-column' in 'brew-basics.fs'

\ bigforth forth-system !

\ Check for bug in v2.1.00
s" bigFORTH" environment? [IF]
    100 2 d= [IF]		\ Version 2.1.00 Bug: Zero on stack
	depth [IF]		\ The user might have fixed the bug
	    DROP		\ drop the zero on stack
	[THEN]
    [THEN]
[THEN]

also DOS
: flush-file flush-file ;
previous

: xt>string  >name count $1F and ;	\ Bernd Paysan

\ From: Guido Draheim
\ Message-ID: <3A4D2BD5.83D3DCC0@gmx.de>
: [defined] [compile] defined? ; immediate
: [undefined] [compile] [defined] 0= ; immediate

\ TRUE CONSTANT lower-right-scrolls	\ does not help. use my 'at?'

\ does not work as bigForth counts i.e. tabs as one.
\ : at?  ( -- x y )   at? swap ;

s" 12" drop 1+ 0  over 1+ 0 compare [IF] \ check for bigFORTH 2.0.0 COMPARE bug
    cr .( bigForth bug in 'COMPARE'.)
    cr .( Redefined to fix it.)
    2000 ms cr
: compare ( addr1 count1 addr2 count2 -- n )
    dup    IF compare EXIT THEN
    2 pick IF compare EXIT THEN
    2drop 2drop false ;
[THEN]

\ check for 'search' bug in bigFORTH rev. 2.0.2
s" ORGAN-F" s" PARAMETER-" search [IF]
    cr .( bigForth bug in 'SEARCH'.)
    cr .( Redefined to fix it.)
    2000 ms cr
: search ( addr1 count1 addr2 count2 - addr3 count3 flag )
    2 pick over < IF 2drop FALSE EXIT THEN
    search ;
[THEN] 2drop

[DEFINED] open-dir [IF]	\ from version 2.0.3
    TRUE CONSTANT use-fileselect			\ experimental
[THEN]


\ Word to pass a string to the OS shell returning an error flag:
\ This is used to call context sensitive help.
: <system> ( addr count -- error-flag )
    >r pad r@ move	\ bigForth system takes a 0" string as parameter
    pad 0 over r> + c!
    [ also DOS ] system [ previous ] ;


\ Floating point:
INCLUDE float.fb 	( also ) FLOAT also FORTH
