\ console-codes.fs
\ 	$Id: console-codes.fs,v 1.1.1.1 2009-06-09 18:28:50 anton Exp $	


\ ****************************************************************
\ dependencies:

[UNDEFINED] push-key [IF]	INCLUDE keybuf.fs 			[THEN]

s" lists.fs" REQUIRED

\ ****************************************************************

decimal


\ Cursor:

[defined] noterm [if]
    : at? 1 1 ;
    : at-xy 2drop ;
[else]
[UNDEFINED] at? drop TRUE [IF] \ bigFORTH has it but it is behaves different.
: at? ( -- x y )		\ gives cursor position 'at? at-xy' is a noop
\ see man console_codes:
\      ECMA-48 Status Report Commands
\      ESC [ 6 n
\      Cursor position report (CPR): Answer is ESC [ y ; x
\      R, where x,y is the cursor location.
    BEGIN key? WHILE key push-key REPEAT
    27 emit ." [6n"
    BEGIN key 27 = UNTIL	\ wait until ESC arrives
    key	dup [char] [ <> IF
	cr ." keycode was wrong"
	push-key
	0 0 EXIT	\ ####### how to recover here?
    ELSE drop THEN

    0 BEGIN			\ get y
	key
	dup [char] ; <> WHILE	\ ';' is delimiter
	>r 10 * r>
	[char] 0 -   +
    REPEAT drop 1-
    0 BEGIN			\ get x
	key
	dup [char] R <> WHILE	\ 'R' is delimiter
	>r 10 * r>
	[char] 0 -   +
    REPEAT drop 1- swap ;
[THEN]
[then]

: cursor-visible	27 emit ." [?25h" ;
: cursor-off		27 emit ." [?25l" ;

false [IF] \ not using escape sequences for cursor movement any more
    \ inspired by a suggestion from Marcel Hendrix I leave work to 'at?'.
    \ see display.fs

    \ my old version using escape sequences (just in case...)
    : cursor-up			27 emit ." [A" ;
    : cursor-down		27 emit ." [B" ;
    : cursor-right  		27 emit ." [C" ;
    : cursor-left	  	27 emit ." [D" ;
[THEN]


\ Colors;

\ the common 8 colors list:
8 CONSTANT colors
0 CONSTANT black
1 CONSTANT red
2 CONSTANT green
3 CONSTANT brown
4 CONSTANT blue
5 CONSTANT magenta
6 CONSTANT cyan
7 CONSTANT white

\ default-color does not exist on all systems
9 CONSTANT default-color

LIST: color-list
' black color-list >list
' red color-list >list
' green color-list >list
' brown color-list >list
' blue color-list >list
' magenta color-list >list
' cyan color-list >list
' white color-list >list
' default-color color-list >list
