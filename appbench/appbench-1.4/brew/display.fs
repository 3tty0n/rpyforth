\ display.fs
\ 	$Id: display.fs,v 1.41 2005/04/23 06:14:06 f Exp $	

\ ****************************************************************
\ Compile options
\ 	lower-right-scrolls
\ 	color-foreground
\ 	color-background

\ ****************************************************************
\ file dependencies

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

[UNDEFINED] colors [IF]   s" console-codes.fs" INCLUDED			[THEN]

[UNDEFINED] l-s [IF]   s" screen-size.fs" INCLUDED			[THEN]

\ ****************************************************************



[UNDEFINED] .tab [IF]	: .tab	9 emit ;	[THEN]	\ prints a tab
[UNDEFINED] .bs  [IF]	: .bs	8 emit ;	[THEN]	\ prints a backspace


\ Cursor position:

: this-line ( -- y )   at? nip ;

: at-x? ( -- x )  at? drop ;
: at-x ( x -- )	 this-line at-xy ;
: at-y ( y -- )	 0 swap at-xy ;

\ put cursor on last line of screen
l-s 1- CONSTANT	last-line
: last-left ( -- )   0 last-line at-xy ;
: last-right ( -- )  c-l 1-  last-line  at-xy ;

\ put cursor in the middle of the screen
: mid-screen ( -- )   c-l 2/ l-s 2/ at-xy ;


s" bigFORTH" environment? [IF]	\ strange, but needs to set it twice sometimes.
    2drop
: screen-column ( column columns -- )  c-l swap /  *  dup at-x at-x ;

[ELSE] \ not on bigFORTH

\ set cursor to 'column' of 'columns' screen columns.
: screen-column ( column columns -- )  c-l swap /  *  at-x ;

[THEN] \ bigFORTH


\ Positioning output strings:

\ Print a string centered on current line.
: .centered ( addr count -- )   c-l over - 2/  at-x  type ;

\ Word to type a string, but only until end of line:
: ?type ( addr count -- )
    at-x?
    dup c-l 1- = IF  drop 2drop EXIT  THEN
    >r r@ + c-l min r> - type ;

\ If there's remaining space on the current line, print a space:
: ?space ( -- )   at-x? 1 c-l 1- within IF bl emit THEN ;

\ Type a string (or the part that fits on the current line) and maybe a space,
\ but don't wrap to next line:
: ?type_ ( addr count -- )   ?type ?space ;

\ Word to type a string on the current line. If the string would not fit,
\ start more to the left.
: type-on-same-line ( addr count -- )
    at-x? over + c-l - dup 0> IF
	at-x? swap - at-x
    ELSE drop THEN
    type ;

\ Output a number ensuring that it will fit on the line:
: .num-on-same-line ( n -- )   num>string type-on-same-line ;

: page-type ( addr count -- )	page type ;

\ If cursor is left from the given screen fraction do 'screen-column'
\ else print a tab, then display the string:
: .screen-column-min ( adr count column columns -- )
    2dup c-l swap /  *  at-x? > IF
	screen-column
    ELSE
	2drop
	.tab
    THEN
    type ;


\ React on cursor keys:
\ I use a "DEFERed" at-xy for cursor movements using brew-at-xy-xt
\ This is more flexible regarding screen borders:
\ limiting to brews screen size
\ stopping/wrapping
\ maybe scrolling...
\ (This words are designed for the cursor keys, and not needed in most
\  other cases. Standard 'at-xy' can be used).
VARIABLE brew-at-xy-xt	' at-xy brew-at-xy-xt !	\ trivial default

: cursor-up             at? 1-       brew-at-xy-xt @ EXECUTE ;
: cursor-down           at? 1+       brew-at-xy-xt @ EXECUTE ;
: cursor-right          at? >r 1+ r> brew-at-xy-xt @ EXECUTE ;
: cursor-left           at? >r 1- r> brew-at-xy-xt @ EXECUTE ;

\ position cursor within brews screen, stopp at screen borders
: at-xy-stopping ( x y -- )			\ stops at border
    swap			\ limit x
    0 max	c-l 1- min
    swap			\ limit y
    0 max	l-s 1- min
    at-xy ;

\ position cursor within brews screen, wrap at screen borders
: at-xy-wrapping ( x y -- )			\ wraps at border
    swap   c-l +  c-l mod	\ limit x
    swap   l-s +  l-s mod	\ limit y
    at-xy ;

: toggle-cursor-wrapping ( -- )
    brew-at-xy-xt @ ['] at-xy-stopping = IF
	['] at-xy-wrapping brew-at-xy-xt !
	EXIT
    THEN

    ['] at-xy-stopping brew-at-xy-xt ! ;

toggle-cursor-wrapping toggle-cursor-wrapping	\ toggle twice to initialize



\ Erase line end:
: clear-line-to-end ( -- )	\ ATTENTION can get redefined, see below
    at-x? >r
    r@ c-l 1- = IF rdrop EXIT THEN	\ cursor at end of line?
    c-l  r> - spaces ;

\ There are systems where writing to the very last screen position does
\ automatic scrolling.  Set 'lower-right-scrolls' to true in that case.
[UNDEFINED] lower-right-scrolls [IF]  false CONSTANT lower-right-scrolls [THEN]
\
lower-right-scrolls [IF]		\ there are some terminals that do
    : clear-line-to-end ( -- )		\ automatic scrolling when writing
	this-line last-line <> IF	\ to the very last screen position.
	    clear-line-to-end
	ELSE
	    c-l 1-  at-x? -  1-		\ don't touch last position.
	    dup 0> IF spaces ELSE drop THEN
	THEN ;
[THEN]

: .last-line ( addr count -- )   last-left  type  clear-line-to-end ;


\ Colours:

\ You like bluescreens?  say: 'blue color-background' :)
[UNDEFINED] color-foreground [IF]
: color-foreground ( color -- )
    [UNDEFINED] never-use-colors [IF]
	27 emit  ." [3"  [char] 0 + emit  ." m"
    [ELSE] drop [THEN]	;
[THEN]

[UNDEFINED] color-background [IF]
: color-background ( color -- )
    [UNDEFINED] never-use-colors [IF]
	27 emit  ." [4"  [char] 0 + emit  ." m"
    [ELSE] drop [THEN]	;
[THEN]

: default-foreground ( -- )   default-color color-foreground ;

: default-background ( -- )   default-color color-background ;

: reset-colours ( -- )	default-foreground  default-background ;


\ Colours used in menus or so are defined deferred to be configurable:
    
\ Colours for highlighting text like important menu switches:
DEFER <bright-colours>	\ always terminate with reset-colours or somesuch
:NONAME  cyan color-foreground ; IS <bright-colours>

: type-bright ( addr count -- )   <bright-colours> type reset-colours ;

: ?type-bright ( addr count -- )   <bright-colours> ?type reset-colours ;


\ Colours used for alerts, errors and important warnings:
DEFER <alert-colours>	\ always terminate with reset-colours or somesuch
:NONAME  red color-foreground ; IS <alert-colours>

: type-alert ( addr count -- )   <alert-colours> type reset-colours ;


\ Colours used for hints, options, informations:
DEFER <other-colour>	\ always terminate with reset-colours or somesuch
:NONAME  green color-foreground ; IS <other-colour>

: type-other-colour ( addr count -- )   <other-colour> type reset-colours ;


\ These colours get used when showing nucs or spots coloured to show
\ a certain quality like lying below, inside or above a given range:

VARIABLE color-selected-fg-xt	' default-color color-selected-fg-xt !
VARIABLE color-below-fg-xt	' magenta color-below-fg-xt !
VARIABLE color-above-fg-xt	' cyan color-above-fg-xt !
VARIABLE color-miss-fg-xt	' blue color-miss-fg-xt !
VARIABLE color-selected-bg-xt	' cyan color-selected-bg-xt !
VARIABLE color-below-bg-xt	' magenta color-below-bg-xt !
VARIABLE color-above-bg-xt	' blue color-above-bg-xt !
VARIABLE color-miss-bg-xt	' blue color-miss-bg-xt !
