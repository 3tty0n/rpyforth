\ keybuf.fs
\ 	$Id: keybuf.fs,v 1.12 2005/04/04 13:00:39 f Exp $	

\ High level key interface for 'menu.fs'.

\ Get keys by  'key' 'key?'  or  by 'ekey' 'ekey?'

\ ****************************************************************
\ Compile option:
\ 	use-ekey
\ If 'use-ekey' is defined at compile time it gets compiled as run time switch
\ you never know...

\ ****************************************************************
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" key-mapping.fs" REQUIRED

\ ****************************************************************


decimal

[UNDEFINED] bell [IF]		: bell ( -- )  7 emit ;			[THEN]


256 CONSTANT keybuf-size
CREATE keybuf			keybuf-size allot
VARIABLE key-write-pointer	key-write-pointer off
VARIABLE key-read-pointer	key-read-pointer off

true [IF] \ If full, clear *all* keys:
: push-key ( key -- )
    key-write-pointer @ >r
    keybuf r@ + c!
    r> 1+ keybuf-size mod
    dup key-write-pointer !
    key-read-pointer @ = IF
	cr bell ." keystack full!  cleared." 1500 ms
    THEN ;
[ELSE] \ If full, clear all but the last key:
: push-key ( key -- )
    key-write-pointer @ >r
    keybuf r@ + c!
    r@ 1+ keybuf-size mod
    dup key-write-pointer !
    key-read-pointer @ = IF
	cr bell ." keystack full!  Cleared all but last key." 2500 ms
	r@ key-read-pointer !
    THEN
    rdrop ;
[THEN]

: pop-key ( -- key )
    key-read-pointer @ dup key-write-pointer @ <> IF
	dup keybuf + c@ swap
	1+ keybuf-size mod
	key-read-pointer !
    ELSE
	drop
	cr bell ." keystack empty!" 1000 ms
    THEN ;

[DEFINED] use-ekey [IF]
\ Get a key by 'ekey' and map it to a key index within range:
\ |ekey| used by 'get-key'
: ekey-outside ( ekey -- )
    base @ >r hex
    bell
    cr ." Unknown ekey code " . ."  hex"
    cr ." Consider including  ../make-ekey-map.fs  from system menu. " 1000 ms
    r> base ! ;

\ 'ekey' codes must be mapped system specific.
\ 'map-ekeys' does it. Range is checked anyway.
\  Mapping to -1 means unknown.

\ Get a key by 'ekey' and map it to a key index within range.
\ Unknown keys can return zero.  (Zero gives noop or key-defaults.)
: |ekey| ( -- normalized-ekey )
    ekey
    map-ekeys
    dup 0< IF ekey-outside 0 EXIT THEN		\ negative, error
    dup allowed-key-codes# < IF EXIT THEN	\ ok
    ekey-outside 0 ;				\ too high, error
[THEN]

: get-key ( -- key )
    key-read-pointer @ key-write-pointer @ <> IF
	pop-key
    ELSE
[DEFINED] use-ekey [IF]
	use-ekey @ IF  |ekey|  ELSE  key  THEN
[ELSE]
	key [ hex ] FF and
[THEN]
    THEN ;

decimal

: is-key? ( -- flag )
    key-read-pointer @ key-write-pointer @ <> IF TRUE EXIT THEN
[DEFINED] use-ekey [IF]
    use-ekey @ IF  ekey?  ELSE  key?  THEN
[ELSE]
    key?
[THEN]
;
