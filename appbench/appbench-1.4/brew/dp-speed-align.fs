\ dp-speed-align.fs
\ 	$Id: dp-speed-align.fs,v 1.2 2001/09/09 15:52:49 f Exp $	

\ On many processor you can gain a lot of speed advantage by generously
\ aligning/padding code and/or data.

\ I don't have much knowledge about it, just want to try a bit.

\ This file works on the FORTH dictionary pointer 'DP'.

\ I don't know if aligning the dp does any good at all, but let's try.
\ Padding does, AFAIK.

\ (See 'memory-speed-align.fs' for allocated memory).


\ ****************************************************************
\ Usage:

\    put these words before and after important code or data definition blocks:
\    Keep code and data definitions separated.

\ dp-start-code-block	does padding and alignement
\ dp-pad-pre-code-xt	does the alignement part
\ dp-pad-after-code	padding after a block

\ dp-start-data-block	does padding and alignement
\ dp-pad-pre-data-xt    does the alignement part
\ dp-pad-after-data	padding after a block


\ Defining deferred functions for alignement and padding DP:
\ Initialise them as noops.

\ DP CODE alignement and padding:

\ 'dp-start-code-block' can be used before compiling important code blocks:
VARIABLE dp-start-code-block-xt		' noop dp-start-code-block-xt !
VARIABLE dp-pad-pre-code-xt		' noop dp-pad-pre-code-xt !
VARIABLE (last-code-block-dp)
: dp-start-code-block ( -- )		\ does padding and alignement
    dp-pad-pre-code-xt @ EXECUTE
    dp-start-code-block-xt @ EXECUTE
    here (last-code-block-dp) ! ;

\ 'dp-align-code-item' could be used before compiling items inside code blocks:
false [IF] \ unused
    VARIABLE dp-align-code-item-xt	' noop dp-align-code-item-xt !
    VARIABLE (last-code-item-dp)
    : dp-align-code-item ( -- )
	dp-align-code-item-xt @ EXECUTE
	here (last-code-item-dp) ! ;
[THEN]

\ 'dp-pad-after-code' can be used after compiling important code blocks:
VARIABLE dp-pad-after-code-xt		' noop dp-pad-after-code-xt !
: dp-pad-after-code ( -- )   dp-pad-after-code-xt @ EXECUTE ;


\ DP DATA alignement and padding:

\ 'dp-start-data-block' can be used before compiling important data blocks:
VARIABLE dp-start-data-block-xt		' noop dp-start-data-block-xt !
VARIABLE dp-pad-pre-data-xt		' noop dp-pad-pre-data-xt !
VARIABLE (last-data-block-xt)
: dp-start-data-block ( -- )		\ does padding and alignement
    dp-pad-pre-data-xt @ EXECUTE
    dp-start-data-block-xt @ EXECUTE
    here (last-data-block-xt) ! ;

\ 'dp-align-data-item' could be used before compiling items inside data blocks:
false [IF] \ unused
    VARIABLE dp-align-data-item-xt		' noop dp-align-data-item-xt !
    VARIABLE (last-data-item)
    : dp-align-data-item ( -- )
	dp-align-data-item-xt @ EXECUTE
	here (last-data-item) ! ;
[THEN]

\ 'dp-pad-after-data' can be used after compiling important data blocks:
VARIABLE dp-pad-after-data-xt		' noop dp-pad-after-data-xt !
: dp-pad-after-data ( -- )   dp-pad-after-data-xt @ EXECUTE ;



\ ****************************************************************
\ Trying out concrete functions to put in the just defined words:
\ ****************************************************************

decimal
VARIABLE dp-speed-alignement	32 dp-speed-alignement !

\ Align dp to the next address dividable by 'alignement':
: dp-align ( alignement -- )
    dup 0= IF drop EXIT THEN

    here over mod
    dup IF
	- allot
    ELSE 2drop THEN ;

\ Align dp to the next address dividable by 'dp-speed-alignement':
: align-dp-for-speed ( -- )   dp-speed-alignement @ dp-align ;


\ Pad between data and code that may use it.

\ : dp-speed-pad ( -- )  align-dp-for-speed ;		\ don't know which one
\ : dp-speed-pad ( -- )  dp-speed-alignement @ allot ;	\ don't know which one
: dp-speed-pad ( -- )					\ don't know which one
    dp-speed-alignement @ allot  align-dp-for-speed ;	\ please test it! #####

' align-dp-for-speed
dup dp-start-code-block-xt !
dup dp-start-data-block-xt !
drop

' dp-speed-pad
dup dp-pad-pre-code-xt !
dup dp-pad-after-code-xt !
dup dp-pad-pre-data-xt !
dup dp-pad-after-data-xt !
drop
