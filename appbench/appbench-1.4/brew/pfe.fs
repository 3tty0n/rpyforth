\ pfe.fs
\ 	$Id: pfe.fs,v 1.19 2003/08/27 17:58:26 f Exp $	

\ pfe specific stuff.


\ also FORTH definitions	\ 0.30.35 doesn't work without that
				\ 0.30.38 does

\ Make gforth style directory access available.  Needed with PFE 0.32.48
s" gforth-ext" environment? [IF]	\ Guido Draheim
    drop
[ELSE]
    cr .( No gforth module in this PFE version. )
[THEN]

also gforth'			\ have directory words available
TRUE CONSTANT use-fileselect

VARIABLE use-ekey		\ defining it is a compile time option
use-ekey on			\ run time default

[UNDEFINED] rdrop [IF]
: rdrop ( r: x -- r: - )   POSTPONE r>drop ; IMMEDIATE
[THEN]

: xt>string ( xt -- addr count ) >name count ;

\ Word to pass a string to the OS shell returning an error flag:
\ This is used to call context sensitive help.
: <system> ( addr count -- error-flag )  system ;

\ sm/rem does not throw when dividing by zero.
: /   dup IF / ELSE 2drop 0 -10 THROW THEN ;

VARIABLE ekey-cursor-support	ekey-cursor-support on
VARIABLE ekey-function-keys-support	ekey-function-keys-support on

\ compile constants with inreasing values
[UNDEFINED] enum: [IF]		: ENUM: ( n -- n+1 ) dup CONSTANT 1+ ;	[THEN]

hex

D CONSTANT <return>

100
ENUM: <F1>
ENUM: <F2>
ENUM: <F3>
ENUM: <F4>
ENUM: <F5>
ENUM: <F6>
ENUM: <F7>
ENUM: <F8>
ENUM: <F9>
ENUM: <F10>
ENUM: <F11>
ENUM: <F12>
ENUM: <shift-F1>
ENUM: <shift-F2>
ENUM: <shift-F3>
ENUM: <shift-F4>
ENUM: <shift-F5>
ENUM: <shift-F6>
ENUM: <shift-F7>
ENUM: <shift-F8>

ENUM: <left>
ENUM: <right>
ENUM: <up>
ENUM: <down>

ENUM: <home> 		\ 118
1+
ENUM: <page-down>	\ 11A
ENUM: <page-up>		\ 11B
drop

: map-ekeys ; IMMEDIATE \ no mapping required here :-)

decimal

\ Load float words. Needed with PFE 0.32.48
s" floating-ext" environment? [IF]
    drop
[ELSE]
    cr .( No 'floating-ext' in this pfe version. )
[THEN]


\ These words are synonymes of others in newer PFE versions.
\ To make sure xt>string returns the right string I redefine them here:
: off off ;
: on on ;

\ keep case sensitivity happy:
\ : INCLUDED  included ;
\ : OFF off ;
\ : ON  on ;

\ hack to get the intended ORDER when calling BREW-MENU after quitting brew:
: brew-menu ( -- )   s" also brew-words definitions brew-menu" EVALUATE ;

\ I had some problems with pfe REQUIRED, so i do
s" required.fs" INCLUDED
