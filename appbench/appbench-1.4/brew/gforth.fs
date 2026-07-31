\ gforth.fs
\ 	$Id: gforth.fs,v 1.15 2005/03/31 17:46:06 f Exp $	

\ Gforth specific things.
    
\ require etags.fs		\ do this by hand if required
\ gforth forth-system !

bl parse [defined] dup pad c! pad char+ swap chars move pad find nip 0=
[IF] \ versions 0.6 do have it
\ From: Guido Draheim
\ Message-ID: <3A4D2BD5.83D3DCC0@gmx.de>
: [defined] [compile] defined ; immediate
: [undefined] [compile] [defined] 0= ; immediate
[THEN]

\ Word to pass a string to the OS shell returning an error flag:
\ This is used to call context sensitive help.
: <system> ( addr count -- error-flag )  system $? ;

\ Bernd Paysan
: xt>string ( xt -- addr length )   look IF name>string THEN ;

TRUE CONSTANT use-fileselect			\ experimental

\ note that this is compile *and* run time switch.
VARIABLE use-ekey			use-ekey on

VARIABLE ekey-cursor-support		ekey-cursor-support on
VARIABLE ekey-function-keys-support	ekey-function-keys-support on

use-ekey @  [IF]

    $D CONSTANT <return>

    ekey-cursor-support @ [IF]
	k-left  CONSTANT <left> 
	k-right CONSTANT <right>
	k-up    CONSTANT <up>   
	k-down  CONSTANT <down> 
    [THEN]

    ekey-function-keys-support @ [IF]
	K1  CONSTANT <F1>
	K2  CONSTANT <F2>
	K3  CONSTANT <F3>
	K4  CONSTANT <F4>
	K5  CONSTANT <F5>
	K6  CONSTANT <F6>
	K7  CONSTANT <F7>
	K8  CONSTANT <F8>
	K9  CONSTANT <F9>
	K10 CONSTANT <F10>
	K11 CONSTANT <F11>
	K12 CONSTANT <F12>

	-1 CONSTANT <shift-F1>	\ these are not here
	-1 CONSTANT <shift-F2>
	-1 CONSTANT <shift-F3>
	-1 CONSTANT <shift-F4>
	-1 CONSTANT <shift-F5>
	-1 CONSTANT <shift-F6>
	-1 CONSTANT <shift-F7>
	-1 CONSTANT <shift-F8>
    [THEN]

    k-home CONSTANT <home>
    k-end  CONSTANT <end>

    \ These are correct for my Debian woody system, maybe not elsewhere...
    \ Include '../make-ekey-map.fs'  from system menu, if there are problems.
\     $40175720 CONSTANT <page-up>
\     $40175738 CONSTANT <page-down>
    $401768F8 CONSTANT <page-up>
    $40176910 CONSTANT <page-down>

[THEN]
