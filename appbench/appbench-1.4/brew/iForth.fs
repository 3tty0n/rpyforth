\ iForth.fs
\ 	$Id: iForth.fs,v 1.21 2005/04/18 18:42:49 f Exp $	
\ Work in progress...


\ Many thanks to Marcel Hendrix from whom is most of this file.
\ From: mhx@iae.nl (Marcel Hendrix)

INCLUDE iforth.prf
NEEDS -terminal

\ as I don't use it I put it here.  It's about the same, but not quite...
\ [defined] -work [if] -work [then] MARKER -work

\ : cursor-visible	CURON ;
: cursor-visible	POSTPONE CURON ; IMMEDIATE

\ : cursor-off		CUROFF ;
: cursor-off		POSTPONE CUROFF ; IMMEDIATE

: cursor-up		?AT 1- AT-XY ;
: cursor-down		?AT 1+ AT-XY ;
: cursor-right  	?AT SWAP 1+ SWAP AT-XY ;
: cursor-left	  	?AT SWAP 1- SWAP AT-XY ;

\ writing to the very last screen position does scroll:
TRUE CONSTANT lower-right-scrolls

\ Marcel, as you said, the color syntax seems not so good for portability.
\ Let's try another color syntax, which looks more portable to me:
\ blue color-background
\ I have made a quick guess what could work on your system based on your
\ suggestions.  Hope I didn't mess it up too much ;-)   see below.

#64 CONSTANT black
#65 CONSTANT blue
#66 CONSTANT green
#67 CONSTANT brown
#68 CONSTANT red
#69 CONSTANT magenta
#70 CONSTANT cyan
#78 CONSTANT yellow
#79 CONSTANT white

\ Number of colors including unnamed ones.  Set it to the full range.
#16 CONSTANT colors
#64 CONSTANT color-offset



-1 CONSTANT default-color		\ set it to a impossible color
black CONSTANT default-background	\ will be used instead of default-color
white CONSTANT default-foreground	\ will be used instead of default-color

\ CREATE never-use-colors       \ must be defined here *if* you want this.

\ Marcel, looking at your definitions you gave for the last syntax
\ I make a quick guess what could work now...

[UNDEFINED] never-use-colors [IF]
: color-foreground ( color -- )
    dup default-color = IF drop default-foreground THEN
    0 swap syscall drop
    TO TextFGColor
    SetTerm ;

: color-background ( color -- )
    dup default-color = IF drop default-background THEN
    0 swap syscall drop
    TO TextBGColor
    SetTerm ;
[ELSE]
: color-foreground ( color -- ) drop ;
: color-background ( color -- ) drop ;
[THEN]


: ?/ ( n n -- n )
    dup IF  2DUP $80000000 -1   \ check for NAN
	D= IF 2drop 0 EXIT  \ meaningless result...
	ENDIF
	/
    ELSE  2drop 0
    ENDIF ;

: AT? ( -- x y ) POSTPONE ?AT ; IMMEDIATE

\ I prefer c/l over c-l but run into troubles with that. Can't remember what
\ it was. Will enquire later.
\ : c-l C/L ;
\ : l-s L/SCR ;

C/L CONSTANT c-l
L/SCR CONSTANT l-s

: .tab	Tab emit ;	\ prints a tab
: .bs	BS  emit ;
: cursor-visible   CURON ;

\ Marcel Hendrix: My terminal has about 120 columns by 40 lines.
\ Marcel Hendrix: C/L and L/SCR give terminal size even when I resize the window.
\ Robert Epprecht: I don't think this will work on this version.
\                  Could you try on 80 25 before we move on?
\                  I think brew does *only* work on *text* console.
\                  I understand next to nothing about terminals, xterm, ... :-(
decimal c-l 80 <>  l-s 25 <> OR [IF]
    bell
    cr .( This version of brew is designed for a 80x25 text console. )
    cr .( There could be problems to run it on another screen size. )
    cr
    5000 ms
[THEN]

NEEDS -see

: xt>string ( xt -- addr length ) >HEAD ID$ ROT DROP ;

\ ekey stuff:
\ note that this is compile *and* run time switch.
VARIABLE use-ekey			use-ekey on

\ key mapping:	iForth KEY and EKEY do return decimal 10 on pressing <RETURN>
\		on Linux...

decimal 10 CONSTANT <return>	\ on Linux
\ decimal 13 CONSTANT <return>	\ on DOS/Windows

: key ( -- char )
    key dup CASE
	<return> OF drop [ decimal ] 13 ENDOF
    ENDCASE ;

[DEFINED] use-ekey [IF]
VARIABLE ekey-cursor-support		ekey-cursor-support on
VARIABLE ekey-function-keys-support	ekey-function-keys-support on

\ ekey mapping:
use-ekey @  [IF]
    ekey-cursor-support @ [IF]
	<-- CONSTANT <left>
	--> CONSTANT <right>
	--^ CONSTANT <up>
	--v CONSTANT <down>
    [THEN]

    ekey-function-keys-support @ [IF]
	F1  CONSTANT <F1>
	F2  CONSTANT <F2>
	F3  CONSTANT <F3>
	F4  CONSTANT <F4>
	F5  CONSTANT <F5>
	F6  CONSTANT <F6>
	F7  CONSTANT <F7>
	F8  CONSTANT <F8>
	F9  CONSTANT <F9>
	F10 CONSTANT <F10>
	F11 CONSTANT <F11>
	F12 CONSTANT <F12>
	 -1 CONSTANT <shift-F1>	\ these are not here
	 -1 CONSTANT <shift-F2>
	 -1 CONSTANT <shift-F3>
	 -1 CONSTANT <shift-F4>
	 -1 CONSTANT <shift-F5>
	 -1 CONSTANT <shift-F6>
	 -1 CONSTANT <shift-F7>
	 -1 CONSTANT <shift-F8>
    [THEN]

    Hme  CONSTANT <home>
    End  CONSTANT <end>
    PgDn CONSTANT <page-down>
    PgUp CONSTANT <page-up>

[THEN]

[THEN]

\ Word to pass a string to the OS shell returning an error flag:
\ This is used to call context sensitive help.
: <system> ( addr count -- error-flag )  system  RETURNCODE @ ;
