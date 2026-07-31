\ key-mapping.fs
\ 	$Id: key-mapping.fs,v 1.5 2003/08/27 15:47:29 f Exp $	
\ (this file was misnamed 'ekey-mapping.fs' before).

\ Maps special key events to key menu codes.
\ key menu codes for key-menu
\ ekey treatment

\ '<F1>' is the system dependent ekey return from pressing function key 1.
\ 'F1%'  is the key code key menu gets when function key 1 is pressed.
\ It's mapped from either escape sequence or ekey's '<F1>' to 'F1%'


\ key-menu codes for some special keys:
\ These key codes apply anyway, with ekey or escape sequence.

hex
D CONSTANT RETURN%	\ iForth returns decimal 10 instead of 13

100
ENUM: F1%	\ key menu code, for the first function key
ENUM: F2%
ENUM: F3%
ENUM: F4%
ENUM: F5%
ENUM: F6%
ENUM: F7%
ENUM: F8%
ENUM: F9%
ENUM: F10%
ENUM: F11%
ENUM: F12%
ENUM: shift-F1%
ENUM: shift-F2%
ENUM: shift-F3%
ENUM: shift-F4%
ENUM: shift-F5%
ENUM: shift-F6%
ENUM: shift-F7%
ENUM: shift-F8%

ENUM: left%		\ key menu codes for the cursor arrows
ENUM: right%
ENUM: up%
ENUM: down%

ENUM: home% 		\ 118
ENUM: end%		\ I don't know if it's ok here. (pfe does not have it)
ENUM: page-down%	\ 11A
ENUM: page-up%		\ 11B

\ Accepted range of ekey mapping goes from 0 to allowed-key-codes# -1
CONSTANT allowed-key-codes#
decimal


\ ekey related:
[DEFINED] use-ekey [IF]

    [UNDEFINED] map-ekeys [IF] \ can be system dependent. see pfe.fs

    \ <ekey-return> to key-menu-code% translation
    : map-ekeys ( ekey -- ekey-return )
    	dup CASE
	    -1    OF drop  -1   ENDOF
\	<ekey-return> to key-menu-code% translation
	    <return> OF drop RETURN% ENDOF		\ for iForth
	    <F1>  OF drop  F1%  ENDOF
    	    <F2>  OF drop  F2%  ENDOF
    	    <F3>  OF drop  F3%  ENDOF
    	    <F4>  OF drop  F4%  ENDOF
    	    <F5>  OF drop  F5%  ENDOF
    	    <F6>  OF drop  F6%  ENDOF
    	    <F7>  OF drop  F7%  ENDOF
    	    <F8>  OF drop  F8%  ENDOF
    	    <F9>  OF drop  F9%  ENDOF
    	    <F10> OF drop  F10% ENDOF
    	    <F11> OF drop  F11% ENDOF
    	    <F12> OF drop  F12% ENDOF

    	    <shift-F1> OF drop shift-F1% ENDOF
    	    <shift-F2> OF drop shift-F2% ENDOF
    	    <shift-F3> OF drop shift-F3% ENDOF
    	    <shift-F4> OF drop shift-F4% ENDOF
    	    <shift-F5> OF drop shift-F5% ENDOF
    	    <shift-F6> OF drop shift-F6% ENDOF
    	    <shift-F7> OF drop shift-F7% ENDOF
    	    <shift-F8> OF drop shift-F8% ENDOF
    
    	    <left>     OF drop left%  ENDOF
    	    <right>    OF drop right% ENDOF
    	    <up>       OF drop up%    ENDOF  
    	    <down>     OF drop down%  ENDOF 

	    <home>	OF drop home%      ENDOF
	    <end>	OF drop end%	   ENDOF
	    <page-down>	OF drop page-down% ENDOF
	    <page-up>	OF drop page-up%   ENDOF

	ENDCASE ;
    [THEN]

[THEN] \ use-ekey
