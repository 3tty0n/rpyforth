\ menu.fs
\ 	$Id: menu.fs,v 1.71 2005/04/20 11:29:59 f Exp $	

\ ****************************************************************
\ Tool to build a kind of active text menu as user interface.

\ User input like pressing a key or selecting a location can trigger actions.
\ Locations can be selected by moving the cursor and pressing <enter>.
\ If 'mouse-supported' is on, the mouse can be used too.

\ Menus can be nested.

\ There are two types of menus: reactions on keys, and reactions on
\ choosing locations (by the mouse, or with cursor keys and RETURN)
\ Other menus, like reacting on MIDI input could be constructed easily.

\ See the tests at the end of the file for a usage example.


\ 'show-key-bindings' added and a lot of related words.

\ To give 'show-key-bindings' the possibility to give meaningful
\ output I pass arguments as xt's when possible.


\ ****************************************************************
\ LICENSE:

\ menu.fs
\ This file was written as a part of 'brew',
\ an experiment with evolutionary programming written in Forth.

\ Copyright (C) 2001, 2002 by Robert Epprecht <epprecht@solnet.ch>

\ This program is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation; either version 2
\ of the License, or (at your option) any later version.
\ 
\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.
\ 
\ You should have received a copy of the GNU General Public License along
\ with this program; if not, write to the Free Software Foundation, Inc.,
\ 59 Temple Place, Suite 330, Boston, MA 02111-1307, USA.

\ ****************************************************************
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" lists.fs" REQUIRED
s" screen-size.fs" REQUIRED	\ see comment in brew.fs
s" listed-masks.fs" REQUIRED
s" display.fs" REQUIRED
s" user-IO.fs" REQUIRED
s" manual.fs" REQUIRED

\ ****************************************************************
\ compile options:	in brew set in file 'compile-options.fs'.
\
\ Test switch: Not all dependencies resolved any more. Use it with 'brew.fs'
FALSE		\ true for testing, false normally
dup [IF]
    [UNDEFINED] allowed-key-codes# [IF]
	decimal 276 constant allowed-key-codes#
    [THEN]
    INCLUDE system-dependent.fs
[THEN]
\
\ Real mouse support or only cursor keys and <return> ?
\ The switch is here, but mouse support is not done yet.  Help appreciated.
[UNDEFINED] mouse-supported [IF]
    FALSE CONSTANT mouse-supported
[THEN]
\
\ scope of 'ekey' and 'ekey?'
\ ekey-cursor-support
[UNDEFINED] ekey-cursor-support [IF]
    VARIABLE ekey-cursor-support	ekey-cursor-support off
[THEN]
\
\ ekey-function-keys-support
[UNDEFINED] ekey-function-keys-support [IF]
    VARIABLE ekey-function-keys-support	ekey-function-keys-support off
[THEN]

\ see also 'float extensions'

\ ****************************************************************
[UNDEFINED] cursor-visible [IF]	   INCLUDE console-codes.fs 	[THEN]

[UNDEFINED] c-l [IF]		   INCLUDE basics.fs 		[THEN]
\ ****************************************************************

\ Defining THROW codes:
1
[UNDEFINED] |menu-input-error [IF]
    ENUM: |menu-input-error
[ELSE] 1+ [THEN]
drop

\ ****************************************************************

\ The idea is to have an array entry for each key and each location
\ describing the action to take if the user selects it.
\ I call such an entry record a menu cell.
\ Data common to all cells of a menu are stored in the menu descriptor.

\ Define menu *cell* structure:
0
OFFSET:	>menu-xt
OFFSET:	>menu-cell-flags
OFFSET:	>menu-parameter-1
OFFSET:	>menu-parameter-2
OFFSET:	>menu-parameter-3
OFFSET:	>menu-input-at	\ x-y of input (and 16 bit unused)
OFFSET: >menu-any-data	\ can be used for anything (like a string pointer)
OFFSET: >menu-any-data2
CONSTANT menu-cell-length#

\ Defining menu entries takes too many parameters to pass them all on stack.
\ So we put them in a scratch menu cell.  Used for prefix notation.
\ It's also used to store location of user input.
CREATE (menu-scratch-cell)   menu-cell-length# allot

: menu-clear-prefixes ( -- )  (menu-scratch-cell) menu-cell-length# erase ;
menu-clear-prefixes

\ Store data in menu cell at 'addr' respecting prefixes.
: menu! ( xt addr -- )		\ uses and clears prefixes
    >r					( xt  r: addr )
    (menu-scratch-cell) tuck >menu-xt !
    r> menu-cell-length# move
    menu-clear-prefixes ;

\ menu@ reads menu cell at addr, puts xt on stack, sets the prefixes
: menu@ ( addr -- xt )
    >r						( r: addr )
    r@ (menu-scratch-cell) menu-cell-length# move
    r> >menu-xt	@ ;

0 VALUE (actual-menu)	\ address of actual menu descriptor
			\ or zero if no menu is actual

: MDE: \ 'menu-descriptor-entry'
    CREATE ( offset-in-cells - offset+1 ) 
	dup cells , 1+
    DOES> ( -- addr )
	@ (actual-menu) + ;

\ each menu descriptor starts with the following structure

0 \ offset in cells
MDE: menu-id	\ defaults to zero
\ A menu can set a menu-id <>0, so words can check if they get called
\ from the right context.  This is intended for common menu entries like
\ cursor arrows or function keys.
MDE: menu-display-xt	\ xt of display-routine which should also set the keys
MDE: clear-screen-xt	\ xt of clear-screen action
MDE: to-do-before-xt	\ xt of action to be taken in advance
MDE: to-do-after-xt	\ xt of action to be made after
MDE: to-do-after-2-xt	\ xt of second action to be made after
MDE: menu-exit		\ shall we exit menu after this action?
MDE: menu-redisplay	\ must the display be renewed after this action?
MDE: menu-selected	\ data of selected item is copied here
\ you can never know if an action selected by the user (i.e. a submenu ) did
\ not corrupt the menu data in the field.
\ So I save all data of the selected item here.
\
\ 'menu-selected' is also used during construction of the menu entries
\ as each menu-entry does 'menu-selected menu!' the datas of the
\ last menu entry remains there and can be reused

\ we need one menu cell length: add extra length:
menu-cell-length# cell / 1- +

\ after that there is a structure like this for the screen-menu:
\ MDE: screen-menu-items#	\ not really needed yet
0 VALUE screen-menu-items#	\ so we have it like this for now
\
MDE: menu-2-at			\ double cell storage of cursor position
1+				\ second cell
MDE: menu-scroll-lines		\ defined items, switches scrolling too.
MDE: menu-scrolled		\ display offset in scrolling menus
MDE: screen-menu-defaults	\ (one menu-cell-length# length)
menu-cell-length# cell / 1- +		\ add extra length

\ if there's a key-menu, then there is a structure like this:

MDE: key-menu-defaults		\ (one menu-cell-length# length)
menu-cell-length# cell / 1- +		\ add extra length

cells CONSTANT menu-descriptor-length 

VARIABLE screen-menu-only	screen-menu-only off
VARIABLE key-menu-only \ very likely to make problems with nested menus! DADA##
         key-menu-only off

VARIABLE screen-menu-array	screen-menu-array off	\ array base pointer

: screen-menu-free  ( -- ) 
    screen-menu-array @ ?dup IF 
	free  IF
	    bell  cr ." screen-menu-free: Couldn't free screen menu "	800 ms
	THEN
	screen-menu-array off 
    THEN ;

VARIABLE key-menu-array		key-menu-array off	\ array base pointer

: key-menu-free  ( -- )
    key-menu-array @ ?dup IF
	free  IF bell cr ." key-menu-free: Couldn't free key menu " 800 ms THEN
	key-menu-array off 
    THEN ;

: free-menus ( -- )   screen-menu-free  key-menu-free ;

: ?menus-clear
    screen-menu-array @ ?dup IF
	screen-menu-items# menu-cell-length# * erase
    THEN
    key-menu-array @ ?dup IF
	allowed-key-codes# menu-cell-length# * erase
    THEN ;

: ?menus-allocate
    screen-menu-array @ 0= IF	\ screen array not yet allocated?
	key-menu-only @ 0= IF	\ but needed
	    c-l l-s *		\ for each character on the screen
	    dup menu-cell-length# *		\ is a menu item needed
	    allocate   abort" no memory for menus "	\ allocate
	    swap to screen-menu-items#
	    screen-menu-array !
	THEN
    THEN
    key-menu-array @ 0= IF	\ key array not yet allocated?
	screen-menu-only @ 0= IF	\ but needed
	    allowed-key-codes#  menu-cell-length# *
	    allocate   abort"  no memory for menus "	\ allocate
	    key-menu-array !
	THEN
    THEN 
    ?menus-clear ;

: xy># ( -- i# ) at? c-l * + ;
: #>addr ( i#-- addr ) menu-cell-length# *   screen-menu-array @  + ;


\ Words to highlite active menu fields:
[UNDEFINED] menu-colors-xt [IF]
    2VARIABLE menu-colors-xt
    ' cyan ' default-color menu-colors-xt 2!
\   ' default-color ' magenta menu-colors-xt 2!
[THEN]
VARIABLE (highlite-active)	(highlite-active) off

: menu-highlite-on ( -- )		\ set colors of active fields
    (highlite-active) @ 0= IF EXIT THEN

    menu-colors-xt 2@ EXECUTE color-background EXECUTE color-foreground ;

: menu-highlite-off ( -- )
    (highlite-active) @ 0= IF EXIT THEN

    reset-colours ;


\ Words to extend the active range of menu entries:

2VARIABLE	(2last-entry-range)			\ range of last entry

\ Sometimes we want the active range start earlier on screen as the entry:
VARIABLE	(from-here)	-1 (from-here) !	\ start point
: from-here ( -- )					\ use this to set start
    xy># (from-here) !
    menu-highlite-on ;

\ Sometimes we want the active range expand after the menu entry:
: up-to-here ( -- )
    (2last-entry-range) cell+ @ xy># swap ?DO
	menu-selected  i #>addr  menu-cell-length# move
    LOOP
    menu-highlite-off ;

\ Print a string as expansion of the previous menu entry:
: .menu-expansion ( addr count -- )   menu-highlite-on type up-to-here ;


: start-xy># ( -- i# )
    (from-here) @ -1 = IF
	xy>#
    ELSE
	(from-here) @
	-1 (from-here) !
    THEN ;

\ Can be defined *before* 'menu-entry'and be reused there  ;-)
: same-menu-entry ( addr count -- )
    menu-highlite-on
    start-xy># >r   type   xy>#   r>
    2dup (2last-entry-range) 2! ?DO
	menu-selected  i #>addr  menu-cell-length#  move
    LOOP
    menu-highlite-off ;

\ This is the main word to display a string and setup menu to do something
\ when it gets selected:
: menu-entry ( addr count xt -- )
    menu-selected menu!
    same-menu-entry ;

\ Display a string as inactive menu item (avoiding default actions):
: noop-entry ( addr count -- )
    (highlite-active) dup dup @ 2>r off
    ['] noop menu-entry
    r> r> ! ;


\ Menu entry displaying a string and a value,
\ make menu entries to do something on selecting it.
: menu-entry-value ( addr count n action-xt -- )
    swap >r		( addr count xt	 r: n )
    menu-selected menu!	( addr count	 r: n )
    r>			( addr count n )

    \ display string and value:
    menu-highlite-on
    start-xy># >r >r  type r> .num-on-same-line  xy>#   r>
    2dup (2last-entry-range) 2! ?DO
	menu-selected  i #>addr  menu-cell-length#  move
    LOOP
    menu-highlite-off ;

\ Menu entry to do something on the value at a given named address-xt.
\ String and value get displayed.
: menu-entry-variable ( addr count variable-xt xt-action -- )
    swap >r		( addr count action-xt  r: var-xt )
    menu-selected menu!	( addr count  r: var-xt )
    r>			( addr count var-xt )
    menu-highlite-on
    start-xy># >r >r  type r> EXECUTE @ .num-on-same-line  xy>#   r>
    2dup (2last-entry-range) 2! ?DO
	menu-selected  i #>addr  menu-cell-length#  move
    LOOP
    menu-highlite-off ;

\ Display the name of a word executed when selected:
: name-menu-entry ( xt -- )	\ name of word as visible menu entry
    dup >r
    xt>string
    r> menu-entry ;

\ Set default actions when a undefined spot get's selected.
: menu-default ( xt -- )
    screen-menu-defaults menu! 
    screen-menu-items# 0 DO
	screen-menu-defaults   i #>addr  menu-cell-length#  move
    LOOP ;

: char>addr ( c -- addr )
    dup allowed-key-codes# >=  over 0<  OR
    ABORT" char>addr: Key out of range."

    menu-cell-length# *   key-menu-array @ + ;

\ Set default key reaction for keys not otherwise defined:
: menu-key-default ( xt -- )
    key-menu-defaults menu!
    key-menu-defaults
    allowed-key-codes# 0 DO
	dup   i char>addr  menu-cell-length#  move 
    LOOP drop ;

\ Set actions for pressing one of the keys in the given string:
: menu-key-entry ( addr count xt -- )
    menu-selected menu!
    bounds DO menu-selected  i c@ char>addr  menu-cell-length#  move LOOP ;

\ Set the action of all the given string chars to the last defined one:
: menu-same-key-entry ( addr count -- )	\ same menu entries as entered before
					\ for the keys in the string
    menu-selected menu@ menu-key-entry ;

\ Set action of a key given as ascii number:
: #key-menu-entry ( key% xt -- )
    menu-selected menu!
    menu-selected swap char>addr menu-cell-length# move ;

\ Set the action of the key given as ascii number to the last defined one:
: #key-same-entry ( key% -- )  menu-selected menu@  #key-menu-entry ;


: menus-default-restore		\ restores menu and menu-keys defaults
    screen-menu-defaults  menu@  menu-default
    key-menu-defaults     menu@  menu-key-default ;

\ Bit masks to switch a couple of actions executed when a menu is selected
0
MASK: |don't
MASK: |cls
MASK: |do-before
MASK: |do-after
MASK: |do-after-2
MASK: |ping
MASK: |redisplay
MASK: |menu-wait
MASK: |wait-n'go
MASK: |put-cursor
MASK: |menu-done
MASK: |>stack
MASK: |stack-1-is-xt	\ for 'show-key-bindings'
MASK: |>stack-2
MASK: |stack-2-is-xt	\ for 'show-key-bindings'
MASK: |>stack-3
MASK: |stack-3-is-xt	\ for 'show-key-bindings'
drop


: m-prefix 
    CREATE ( mask -- ) ,
    DOES>  ( -- )
	(menu-scratch-cell) >menu-cell-flags >r
	@  r@ @  or r> ! ;

\ Prefixes to set menu-flags, (they are off by default).
\ Use these prior to a menu entry.

|don't		m-prefix don't
|>stack		m-prefix >stack)	\ see below
|stack-1-is-xt  m-prefix stack-1-is-xt	\ for 'show-key-bindings'
|>stack-2	m-prefix >stack-2)	\ see below
|stack-2-is-xt  m-prefix stack-2-is-xt	\ for 'show-key-bindings'
|>stack-3	m-prefix >stack-3)	\ see below
|stack-3-is-xt  m-prefix stack-3-is-xt	\ for 'show-key-bindings'
|cls		m-prefix cls
|do-before	m-prefix do-before
|do-after	m-prefix do-after
|do-after-2	m-prefix do-after-2
|ping		m-prefix ping
|redisplay	m-prefix redisplay
|menu-wait	m-prefix menu-wait
|wait-n'go	m-prefix wait-n'go	\ waits for key? doesn't destroy input
|put-cursor	m-prefix put-cursor
|menu-done	m-prefix menu-done

\ Use these for stack entries (prior to a menu entry).
: >stack   ( parameter-1 -- )
    (menu-scratch-cell) >menu-parameter-1 !   >stack) ;

: xt>stack ( parameter-1 -- )   >stack stack-1-is-xt ;	\ 'show-key-bindings'

: >stack-2 ( parameter-2 -- )
    (menu-scratch-cell) >menu-parameter-2 !   >stack-2) ;

: xt>stack-2 ( parameter-2 -- )   >stack stack-2-is-xt ;  \ 'show-key-bindings'

: >stack-3 ( parameter-3 -- )
    (menu-scratch-cell) >menu-parameter-3 !   >stack-3) ;

: xt>stack-3 ( parameter-3 -- )   >stack stack-3-is-xt ;  \ 'show-key-bindings'

\ cursor control, where to put the cursor when user input will be required
: >xy ( -- )	\ save current cursor position
    put-cursor	\ it will be remembered in the *coming* menu entry
    (menu-scratch-cell) >menu-input-at >r
    at?   r@ 1+ c!   r> c! ;

: >last-xy	\ current cursor position is remembered in the entries of
		\ the *preceeding* menu-entry
    at?
    menu-selected >menu-input-at 1+ c! 
    menu-selected >menu-input-at    c!
    menu-selected >menu-cell-flags dup @ |put-cursor or swap ! 
    (2last-entry-range) 2@ DO
	menu-selected  i #>addr  menu-cell-length#  move
    LOOP ;

\ menu stack to make menus nestable
hex 12 CONSTANT menus#		\ how many menus can be nested
\ cr .( ==> allows menu nestings through ) menus# . .( levels) cr

VARIABLE menus-on-stack       \ stackpointer
       0 menus-on-stack !

CREATE menu-stack
   menus# cells allot		\ for the stack, menus and menu-2-at

\ User words may decide that after this action the menu should unnest n levels.
\ Use 'menu-leave' for that.  -1 unnests up to top level.
VARIABLE menu-leave	menu-leave off

\ Word called from a menu to unnest some menu levels:
: unnest-menus ( levels -- )   menu-leave ! ;

\ Word called from a menu to unnest to top level:
: to-top-menu ( -- )  -1 unnest-menus ;

: push-menu ( -- )            \ pushes menu on the stack
    menus-on-stack @ menus# < 0=
    ABORT" push-menu: menu stack full, increase 'menus#' in 'menu.fs'"

    (actual-menu) ?dup IF	\ noop if there was no menu
	menu-stack menus-on-stack @ cells + !
	1 menus-on-stack +!
	0 to (actual-menu)	\ (actual-menu) is not initialised any more
    THEN
    (highlite-active) off ; \ don't highlite active fields

: pop-menu ( -- )             \ goes back to previos menu
    menus-on-stack @
    dup IF
	1-
	dup menus-on-stack !
	dup 0= IF menu-leave off THEN
	cells menu-stack + @ to (actual-menu)	\ set menu descriptor  
	(actual-menu) IF ?menus-allocate THEN
	?menus-clear
	menus-default-restore
	menu-redisplay on			\ to have the screen displayed
[DEFINED] help-node" [IF]  help-node" "  [THEN] \ clear help context
    ELSE
	( 0 )  to (actual-menu)			\ hmmm?
    THEN
    (highlite-active) off ; \ don't highlite active fields

: empty-actions
    menu-clear-prefixes			\ just in case
    ['] noop to-do-before-xt !		\ more for aesthetics
    ['] noop to-do-after-xt  !		\ ditto
    ['] noop to-do-after-2-xt  !	\ ditto
    ['] page clear-screen-xt !		\ no crashes please  
    ['] noop menu-display-xt !		\ ditto
    -1 -1 menu-2-at 2! ;		\ -1 -1 means no cursor position saved

: noopmenus
    empty-actions
    ['] noop menu-default
    ['] noop menu-key-default ;

\ MENU:  create a named menu descriptor:
\ The descriptor word will manage allocation of the arrays automatically
: MENU: ( -- )
    CREATE
	menu-descriptor-length allot
    DOES> ( -- )
	push-menu		\ pushes the old menu
	to (actual-menu)	\ sets (actual-menu)
	?menus-allocate		\ do allocation if needed
	empty-actions ;		\ don't crash please...

\ If there are troubles moving the cursor with the arrow key's
\ try setting the following variable. decimal 120 cursor-code-escape-wait !
VARIABLE cursor-code-esc-wait		decimal 18 cursor-code-esc-wait ! hex
VARIABLE cur-esc-wait-calibrate		cur-esc-wait-calibrate off


\ Function keys:

LIST: function-key-actions
' noop function-key-actions >list
[DEFINED] context-help [IF]
: |context-help| ( -- )   context-help  menu-redisplay on ;
' |context-help| function-key-actions >list
[THEN]

VARIABLE F1-xt		' noop F1-xt !
[DEFINED] |context-help|	[IF]	' |context-help| F1-xt !	[THEN]

VARIABLE F2-xt		' noop F2-xt !
VARIABLE F3-xt		' noop F3-xt !
VARIABLE F4-xt		' noop F4-xt !
VARIABLE F5-xt		' noop F5-xt !
VARIABLE F6-xt		' noop F6-xt !
VARIABLE F7-xt		' noop F7-xt !
VARIABLE F8-xt		' noop F8-xt !
VARIABLE F9-xt		' noop F9-xt !
VARIABLE F10-xt		' noop F10-xt !
VARIABLE F11-xt		' noop F11-xt !
VARIABLE F12-xt		' noop F12-xt !
VARIABLE shift-F1-xt	' noop shift-F1-xt !
VARIABLE shift-F2-xt	' noop shift-F2-xt !
VARIABLE shift-F3-xt	' noop shift-F3-xt !
VARIABLE shift-F4-xt	' noop shift-F4-xt !
VARIABLE shift-F5-xt	' noop shift-F5-xt !
VARIABLE shift-F6-xt	' noop shift-F6-xt !
VARIABLE shift-F7-xt	' noop shift-F7-xt !
VARIABLE shift-F8-xt	' noop shift-F8-xt !
\  these do not exist on my system:
\  VARIABLE shift-F9-xt	' noop shift-F9-xt !
\  VARIABLE shift-F10-xt	' noop shift-F10-xt !
\  VARIABLE shift-F11-xt	' noop shift-F11-xt !
\  VARIABLE shift-F12-xt	' noop shift-F12-xt !

\ 'menu-preset-xt' is meant to set menu prefixes.
\ 'false' is allowed as ' noop default-function-keys gives a wrong impression.
: default-function-keys ( menu-preset-xt -- )
    >r

    F1%		F1-xt @		r@ ?EXECUTE  #key-menu-entry
    F2%		F2-xt @		r@ ?EXECUTE  #key-menu-entry
    F3%		F3-xt @		r@ ?EXECUTE  #key-menu-entry
    F4%		F4-xt @		r@ ?EXECUTE  #key-menu-entry
    F5%		F5-xt @		r@ ?EXECUTE  #key-menu-entry
    F6%		F6-xt @		r@ ?EXECUTE  #key-menu-entry
    F7%		F7-xt @		r@ ?EXECUTE  #key-menu-entry
    F8%		F8-xt @		r@ ?EXECUTE  #key-menu-entry
    F9%		F9-xt @		r@ ?EXECUTE  #key-menu-entry
    F10%	F10-xt @	r@ ?EXECUTE  #key-menu-entry
    F11%	F11-xt @	r@ ?EXECUTE  #key-menu-entry
    F12%	F12-xt @	r@ ?EXECUTE  #key-menu-entry
    shift-F1%	shift-F1-xt @	r@ ?EXECUTE  #key-menu-entry
    shift-F2%	shift-F2-xt @	r@ ?EXECUTE  #key-menu-entry
    shift-F3%	shift-F3-xt @	r@ ?EXECUTE  #key-menu-entry
    shift-F4%	shift-F4-xt @	r@ ?EXECUTE  #key-menu-entry
    shift-F5%	shift-F5-xt @	r@ ?EXECUTE  #key-menu-entry
    shift-F6%	shift-F6-xt @	r@ ?EXECUTE  #key-menu-entry
    shift-F7%	shift-F7-xt @	r@ ?EXECUTE  #key-menu-entry
    shift-F8%	shift-F8-xt @	r@ ?EXECUTE  #key-menu-entry
    rdrop ;

mouse-supported [IF]
: release-mousek ( -- )
    BEGIN get-mouse >r 2drop r> 0= IF EXIT THEN bell AGAIN ;
[ELSE]
    \ define some mouse words as noops:
    : mousekey 0 ;
    : release-mousek ;	IMMEDIATE
    : showmouse	 ;	IMMEDIATE
    : hidemouse	 ;	IMMEDIATE
[THEN]

\ Clear all pending keys and force the user to release keys.
: release-key ( -- )
    BEGIN
	BEGIN is-key? WHILE get-key drop REPEAT
	[ decimal ] 10 ms		\ estimated value
	is-key?
    WHILE
	get-key drop
    REPEAT

    BEGIN
	[ decimal ] 70 ms		\ estimated value
	is-key?
    WHILE bell			\ ring the bell if there's still a key pressed
	get-key drop
	BEGIN is-key? WHILE get-key drop bell REPEAT
    REPEAT ;
hex

: release-keys ( -- )
    release-key release-mousek
    release-key release-mousek ;

\ Wait for key or mousekey and store it in 'wait-key-was'.
VARIABLE wait-key-was			\ if you ever need it...
: wait ( -- )
    BEGIN
	mousekey dup IF
	    wait-key-was !
	    2drop EXIT
	ELSE drop THEN
	is-key?  IF get-key
	    wait-key-was !
	    EXIT
	THEN
	10 ms				\ be kind and release CPU ;-)
    AGAIN ;

[UNDEFINED] mousek! [IF]	INCLUDE mouse.fs 	[THEN]

\ In some cases the key was pressed *before* entering the menu loop
\ so we need a switch to shut off key canceling:
\       (could be put in 'release-key' instead)
\ Also used for cursor arrow keys.
VARIABLE do-not-cancel-keys	do-not-cancel-keys off

\ Words to actually move the cursor, keeping menus happy. Beware of key repeat.
: menu-cursor-move:
    CREATE ( xt-of-display-cursor-move -- )
	,
    DOES> ( -- )
	do-not-cancel-keys on	\ beware of key repeat
	@ execute		\ acually move the cursor on the display
	-1 -1 menu-2-at 2!	\ sometimes cursor would be set back again
    ;

' cursor-up	menu-cursor-move: |cursor-up|    
' cursor-down	menu-cursor-move: |cursor-down| 
' cursor-left	menu-cursor-move: |cursor-left| 
' cursor-right	menu-cursor-move: |cursor-right|

VARIABLE (total-scroll-range)
: scroll-sanity ( -- )
    menu-scrolled dup @ 0 max swap !	\ stop at zero
    menu-scrolled dup @			\ end not beyond range:
    menu-scroll-lines @ (total-scroll-range) @ swap - min swap !
    menu-scroll-lines @ (total-scroll-range) @ >= IF	\ whole range visible
	menu-scrolled off
    THEN ;

: cursor-up-?scroll ( -- )
    menu-scroll-lines @ 0= IF |cursor-up| EXIT THEN	\ menu does not scroll
    this-line IF
	|cursor-up|
    ELSE
	-1 menu-scrolled +!
	scroll-sanity
	menu-redisplay on
	at? menu-2-at 2!
    THEN ;

: cursor-down-?scroll ( -- )
    menu-scroll-lines @ 0= IF |cursor-down| EXIT THEN	\ menu does not scroll
    this-line last-line < IF
	|cursor-down|
    ELSE
	1 menu-scrolled +!
	scroll-sanity
	menu-redisplay on
	at? menu-2-at 2!
    THEN ;

: scroll-page-up ( -- )
    menu-scroll-lines @
    dup 0= IF drop EXIT THEN		\ menu does not scroll
    negate menu-scrolled +!
    scroll-sanity
    menu-redisplay on
    at? menu-2-at 2! ;

: scroll-page-down ( -- )
    menu-scroll-lines @
    dup 0= IF drop EXIT THEN		\ menu does not scroll
    menu-scrolled +!
    scroll-sanity
    menu-redisplay on
    at? menu-2-at 2! ;

: scroll-home ( -- )
    menu-scroll-lines @ 0= IF EXIT THEN	\ menu does not scroll
    menu-scrolled off
    menu-redisplay on
    at? menu-2-at 2! ;

: scroll-end ( -- )
    menu-scroll-lines @
    dup 0= IF drop EXIT THEN		\ menu does not scroll

    (total-scroll-range) @ swap -  menu-scrolled !
    scroll-sanity
    menu-redisplay on
    at? menu-2-at 2! ;

\ make key menu entries for cursor movement words.
\ the scrolling version does not bite when not scrolling, so it's used always.
: cursor-entries ( -- )
    up%		['] cursor-up-?scroll	#key-menu-entry
    down%	['] cursor-down-?scroll	#key-menu-entry
    left%	['] |cursor-left|	#key-menu-entry
    right%	['] |cursor-right|	#key-menu-entry
    page-up%	['] scroll-page-up	#key-menu-entry
    page-down%	['] scroll-page-down	#key-menu-entry
    home%	['] scroll-home		#key-menu-entry
    end%	['] scroll-end		#key-menu-entry ;

\ get normalized scroll offset
: scrolled ( range -- +n )
    >r		( r: range )
    r@ 0> IF			\ range defined?
	menu-scrolled @  r@ mod		\ within range
	dup 0< IF r@ + THEN		\ positive
    ELSE
	false
    THEN
    rdrop ;

\ loop parameter mapping from possible range and displayed lines
\ to loop parameters for display.

: scrolled-range ( upper lower -- upper' lower' )
    2dup - (total-scroll-range) !	\ range for scroll-end
    2dup -				( upper lower range	 )
    scrolled				( upper lower scrolled	 )
    +					( upper lower'		 )
    swap >r				( lower'   r: upper )
    dup menu-scroll-lines @ + r> min	( lower' upper' )
    swap ;				( upper' lower' )

\ cyclic-range would be nice to have too ############


\ Reserve a number of lines and set scrolling for the rest of the screen:
: keep-but-scroll-rest ( reserved-lines -- )
    l-s swap -   menu-scroll-lines ! ;


\ Try to get escape sequences not captured by ekey:
\ Returns a (mapped) key% or zero.  Zero means 'what' waits for other input.
true [IF] \ not echoing escape sequences. new version for scrolling.

: .unknown-escape-sequence ( last-key addr count -- FALSE )
    bell cr ." escape-sequence: Unknown escape sequence: esc "
    type	\ key string of preceeding characters, if any.
    emit	\ last key
    BEGIN
	cursor-code-esc-wait @ await-key?
    WHILE
	get-key emit
    REPEAT
    1000 ms
    FALSE ;

\ Map escape sequences to key menu % codes.	New version for scrolling.
\ No echoing of escape sequences.
\ Displays unrecognized escape sequences.
\
\ sorry for the horrible design...
: escape-sequence ( -- key-code|0 )
    \ first $1B already received ?
    is-key? IF get-key
	[char] [ over = IF drop
	    get-key CASE \ key
		[char] A OF up%    ENDOF	\ cursor up
		[char] B OF down%  ENDOF	\ cursor down
		[char] C OF right% ENDOF	\ cursor right
		[char] D OF left%  ENDOF	\ cursor left
		[char] [ OF			\ F1,F2,F3,F4,F5
		    get-key CASE \ next key code
			[char] A OF F1% ENDOF
			[char] B OF F2% ENDOF
			[char] C OF F3% ENDOF
			[char] D OF F4% ENDOF
			[char] E OF F5% ENDOF
			s" [[" .unknown-escape-sequence
			FALSE
		    ENDCASE
		ENDOF
		[char] 1 OF			\ F6,F7,F8
		    get-key CASE \ next key code
			[char] 7 OF
			    get-key CASE \ next key code
				[char] ~ OF F6% ENDOF
				s" [17" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 8 OF
			    get-key CASE \ next key code
				[char] ~ OF F7% ENDOF
				s" [18" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 9 OF
			    get-key CASE \ next key code
				[char] ~ OF F8% ENDOF
				s" [19" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] ~ OF home% ENDOF
			s" [1" .unknown-escape-sequence
			FALSE
		    ENDCASE
		ENDOF
		[char] 2 OF	\ F9,F10,F11,F12 <shift-F1> to <shift F4> 
		    get-key CASE \ next key code
			[char] 0 OF
			    get-key CASE \ next key code
				[char] ~ OF F9% ENDOF
				s" [20" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 1 OF
			    get-key CASE \ next key code
				[char] ~ OF F10% ENDOF
				s" [21" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 3 OF
			    get-key CASE \ next key code
				[char] ~ OF F11% ENDOF
				s" [23" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 4 OF
			    get-key CASE \ next key code
				[char] ~ OF F12% ENDOF
				s" [24" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 5 OF		\ <shift-F1>
			    get-key CASE \ next key code
				[char] ~ OF shift-F1% ENDOF
				s" [25" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 6 OF		\ <shift-F1>
			    get-key CASE \ next key code
				[char] ~ OF shift-F2% ENDOF
				s" [26" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 8 OF		\ <shift-F1>
			    get-key CASE \ next key code
				[char] ~ OF shift-F3% ENDOF
				s" [28" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 9 OF		\ <shift-F1>
			    get-key CASE \ next key code
				[char] ~ OF shift-F4% ENDOF
				s" [29" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			s" [2" .unknown-escape-sequence
			FALSE
		    ENDCASE
		ENDOF
		[char] 3 OF		\ <shift-F5> to <shift F8> 
		    get-key CASE \ next key code
			[char] 1 OF
			    get-key CASE \ next key code
				[char] ~ OF shift-F5% ENDOF
				s" [31" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 2 OF
			    get-key CASE \ next key code
				[char] ~ OF shift-F6% ENDOF
				s" [32" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 3 OF
			    get-key CASE \ next key code
				[char] ~ OF shift-F7% ENDOF
				s" [33" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			[char] 4 OF
			    get-key CASE \ next key code
				[char] ~ OF shift-F8% ENDOF
				s" [34" .unknown-escape-sequence
				FALSE
			    ENDCASE
			ENDOF
			s" [3" .unknown-escape-sequence
			FALSE
		    ENDCASE
		ENDOF
		[char] 4 OF
		    get-key CASE \ next key code
			[char] ~ OF end% ENDOF
			s" [4" .unknown-escape-sequence
			FALSE
		    ENDCASE
		ENDOF
		[char] 5 OF
		    get-key CASE \ next key code
			[char] ~ OF page-up% ENDOF
			s" [5" .unknown-escape-sequence
			FALSE
		    ENDCASE
		ENDOF
		[char] 6 OF
		    get-key CASE \ next key code
			[char] ~ OF page-down% ENDOF
			s" [6" .unknown-escape-sequence
			FALSE
		    ENDCASE
		ENDOF
		s" [" .unknown-escape-sequence
		FALSE
	    ENDCASE

\  	    BEGIN \ ???? ##########
\  		cursor-code-esc-wait @ await-key? \ maybe not necessary?
\  		( key? ) WHILE
\  		get-key emit
\  	    REPEAT
	ELSE \ not a '<esc> [' sequence
	    s" " .unknown-escape-sequence
\	    FALSE
	THEN
    ELSE \ escape only
	1B
    THEN

    cur-esc-wait-calibrate @ IF	\ to calibrate the wait time
	drop
	bell
	cursor-code-esc-wait @ 1+
	dup .
	cursor-code-esc-wait !
	false
    THEN
;

[ELSE] \ echoing escape sequences. No 'ekey' or such.  No scrolling done!
: escape-sequence ( -- key-code|0 )
    \ first $1B already received
    is-key? IF get-key
	dup [char] [ = IF 1B emit emit	\ output cursor codes
	    get-key dup CASE \ key
		[char] A OF emit 0 ENDOF	\ cursor up
		[char] B OF emit 0 ENDOF	\ cursor down
		[char] C OF emit 0 ENDOF	\ cursor right
		[char] D OF emit 0 ENDOF	\ cursor left
		[char] [ OF			\ F1,F2,F3,F4,F5
		    emit
		    get-key dup CASE \ next key code
			[char] A OF emit F1% ENDOF
			[char] B OF emit F2% ENDOF
			[char] C OF emit F3% ENDOF
			[char] D OF emit F4% ENDOF
			[char] E OF emit F5% ENDOF
			emit
		    ENDCASE
		ENDOF
		[char] 1 OF			\ F6,F7,F8
		    emit
		    get-key dup CASE \ next key code
			[char] 7 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit F6% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 8 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit F7% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 9 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit F8% ENDOF
				emit
			    ENDCASE
			ENDOF
			emit
		    ENDCASE
		ENDOF
		[char] 2 OF	\ F9,F10,F11,F12 <shift-F1> to <shift F4> 
		    emit
		    get-key dup CASE \ next key code
			[char] 0 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit F9% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 1 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit F10% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 3 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit F11% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 4 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit F12% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 5 OF emit	\ <shift-F1>
			    get-key dup CASE \ next key code
				[char] ~ OF emit shift-F1% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 6 OF emit	\ <shift-F1>
			    get-key dup CASE \ next key code
				[char] ~ OF emit shift-F2% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 8 OF emit	\ <shift-F1>
			    get-key dup CASE \ next key code
				[char] ~ OF emit shift-F3% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 9 OF emit	\ <shift-F1>
			    get-key dup CASE \ next key code
				[char] ~ OF emit shift-F4% ENDOF
				emit
			    ENDCASE
			ENDOF
			emit
		    ENDCASE
		ENDOF
		[char] 3 OF		\ <shift-F5> to <shift F8> 
		    emit
		    get-key dup CASE \ next key code
			[char] 1 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit shift-F5% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 2 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit shift-F6% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 3 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit shift-F7% ENDOF
				emit
			    ENDCASE
			ENDOF
			[char] 4 OF emit
			    get-key dup CASE \ next key code
				[char] ~ OF emit shift-F8% ENDOF
				emit
			    ENDCASE
			ENDOF
			emit
		    ENDCASE
		ENDOF
		emit
	    ENDCASE

	    BEGIN
		cursor-code-esc-wait @ await-key? \ maybe not necessary?
		( key? ) WHILE
		get-key emit
	    REPEAT
	ELSE
	    bell cr ." unknown escape sequence: esc " emit
	    BEGIN
		cursor-code-esc-wait @ await-key?
	    WHILE
		get-key emit
	    REPEAT
	    1000 ms
	    FALSE
	THEN
    ELSE \ escape only
	1B
    THEN

    cur-esc-wait-calibrate @ IF	\ to calibrate the wait time
	drop
	bell
	cursor-code-esc-wait @ 1+
	dup .
	cursor-code-esc-wait !
	false
    THEN
;
[THEN]

true [IF] \ passing % codes for cursor movement, not calling cursor movement words

\ passing % codes for cursor movement, not calling cursor movement words here
: what ( -- x y mousekey | normalized-key )
    do-not-cancel-keys @ IF		\ see above
	release-mousek			\ only
    ELSE	
	release-keys			\ keyboard and mouse
    THEN

    BEGIN
	mousekey ?dup IF hidemouse EXIT THEN
	is-key? IF
	    get-key
	    dup CASE
		0  OF drop FALSE ENDOF	\ no keys < 4	(reserved for mouse)
		1  OF drop FALSE ENDOF	\ no keys < 4	(reserved for mouse)
		2  OF drop FALSE ENDOF	\ no keys < 4	(reserved for mouse)
		3  OF drop FALSE ENDOF	\ no keys < 4	(reserved for mouse)
		1B OF drop escape-sequence ?dup ENDOF

		dup		\ ordinary keys including cursor array codes
	    ENDCASE
	ELSE false 10 ms	\ be kind to other users...
	THEN
    UNTIL

    dup RETURN% = IF drop		\ catch <RETURN> and fake a mouse event
	at?
	2dup menu-2-at 2!
	1 mousek!			\ fake x y mouse-key=1
	mousek@
    THEN

    do-not-cancel-keys off	\ here because of calibrate-cur-esc-wait
    hidemouse ;

[ELSE] \ doing cursor moving here.
: what ( -- x y mousekey | normalized-key )
    do-not-cancel-keys @ IF		\ see above
	release-mousek			\ only
    ELSE	
	release-keys			\ keyboard and mouse
    THEN

    BEGIN
	mousekey ?dup IF hidemouse EXIT THEN
	is-key? IF
	    get-key
	    dup CASE
		0 OF drop FALSE ENDOF	\ don't accept keys < 4
		1 OF drop FALSE ENDOF	\ don't accept keys < 4
		2 OF drop FALSE ENDOF	\ don't accept keys < 4
		3 OF drop FALSE ENDOF	\ don't accept keys < 4
	[ ekey-cursor-support @ ] [IF]
		up%	OF drop cursor-up	0 ENDOF
		down%	OF drop cursor-down	0 ENDOF
		left%	OF drop cursor-left	0 ENDOF
		right%	OF drop cursor-right	0 ENDOF
	[THEN]
		1B	OF drop escape-sequence ?dup ENDOF
		dup	\ ordinary keys
	    ENDCASE
	ELSE false
	THEN
    UNTIL

    dup D = IF drop		\ catch return and fake a mouse event
	at?
	2dup menu-2-at 2!
	1 mousek!		\ fake x y mouse-key=1
	mousek@
    THEN


    do-not-cancel-keys off	\ here because of calibrate-cur-esc-wait
    hidemouse ;
[THEN]

: calibrate-cur-esc-wait
    cur-esc-wait-calibrate on	\ switch calibration on
    cursor-code-esc-wait off	\ start at zero
    do-not-cancel-keys on	\ calibration doesnt work else
    page ." Calibrating wait time for cursor movement escapes. "
    cr
    cr ." Press a cursor array until the beeping stops."
    cr ." Make sure it only beeps when you press or release a arrow key."
    cr ." Then press <return>."
    cr
    what
    clearstack
    cur-esc-wait-calibrate off ;

: ?perform  ( addr -- )   @ dup IF execute ELSE drop THEN ;

: ?at-xy ( d -- )    dup -1 = IF 2drop ELSE at-xy THEN ;

hex
: do-menu ( -- )
    BEGIN	\ wait for a user input that needs a reaction
	cursor-entries
	what
	dup 0= IF
	    TRUE		\ TRUE, continue
	ELSE
	    dup 4 < IF
		drop  
		c-l * +   #>addr
	    ELSE
		dup IF
		    char>addr
		ELSE
		    drop EXIT
		THEN 
	    THEN
	    dup menu@ menu-selected menu! \ save selected data
	    >menu-cell-flags @       \ get flags and check
	    dup |don't and
	THEN
    WHILE
	drop	\ bad choice or noop 
    REPEAT	( flags )
    dup |cls		and IF clear-screen-xt ?perform THEN
    dup |do-before	and IF to-do-before-xt ?perform THEN
    dup |ping		and IF bell         THEN
    dup |redisplay	and menu-redisplay swap IF on ELSE off THEN
    dup |put-cursor	and IF		\ set cursor?
	menu-selected >menu-input-at dup c@ swap 1+ c@ at-xy
    THEN

    >r
    r@ |>stack and IF
	menu-selected >menu-parameter-1 @	\ parameter-1 on stack
    THEN
    r@ |>stack-2 and IF
	menu-selected >menu-parameter-2 @	\ parameter-2 on stack
    THEN
    r> |>stack-3 and IF
	menu-selected >menu-parameter-3 @	\ parameter-3 on stack
    THEN

    menu-selected >menu-xt ?perform

    menu-selected >menu-cell-flags @			\ get flags again
    dup	|menu-wait and IF wait THEN			\ now *before* do-after
    dup |wait-n'go and IF BEGIN is-key? UNTIL THEN	\ key still available
    dup |do-after and IF to-do-after-xt ?perform THEN
    dup |do-after-2 and IF to-do-after-2-xt ?perform THEN
    |menu-done and IF menu-exit on ELSE menu-exit off THEN	\ set it here
    menu-leave @ IF
	menu-exit on
	-1 menu-leave +!
    THEN
    menu-2-at 2@ ?at-xy ;

: do-menu-loop ( -- )
    menu-redisplay on
    cursor-visible
    BEGIN
	menu-redisplay @ IF 
	    menu-redisplay off  
	    ?menus-allocate        \ a submenu might have free'd it...
	    menus-default-restore
	    clear-screen-xt ?perform
	    menu-display-xt ?perform
	    menu-2-at 2@ ?at-xy
	THEN
	showmouse do-menu 
    menu-exit @ UNTIL
    pop-menu ;

: (num-in) ( default -- n )		\ allows using FORTH. Might THROW.
    depth >r >r				\ remembering depth and default
    0 0 0 0 0 0	r>			\ buffering with zeros, default on top
    depth >r				\ remembering paded depth
    [ decimal ]
    pad dup 80  accept  evaluate	\ evaluating FORTH
    [ hex ]
    depth r> < IF |menu-input-error THROW THEN	\ less stack items? error
    r> swap >r >r			\ swap value and depth on return stack
    BEGIN r@ depth < WHILE drop REPEAT rdrop	\ restoring stack
    r> ;				\ result

: num-in ( default -- n true | false )
    ['] (num-in) CATCH IF	\ |menu-input-error or something
	bell drop false
    ELSE true THEN ;

: num-in-to-addr ( addr -- )   dup @ num-in IF swap ! ELSE drop THEN ;

\ Word to guess the meaning of thing like '2 *' or '10 /' in scale inputs:
VARIABLE (scale-input)		(scale-input) off	\ switches it on.

\ Factored out removing a char ('/' or '*') from the start of a string,
\ and appending it after a blanc to the string:
: operator-to-end ( addr count char --- addr+1 count+1 )
    >r			( addr count  r: char )

    2dup + >r		( addr count  r: char first-addr-after-string )
    bl r@ c!			\ append a blanc
    2r> 1+ c!			\ copy operator to the end
    >r 1+ r>			\ skip old operator
    1+ ;			\ adjust length of string

: ?scale-adaptions ( addr count -- addr' count' )
    (scale-input) dup @ 0= IF drop EXIT then	\ not a scale, done
    off

    \ Some chars (as first one) escape all adaptions:  % ! " '
    over c@ CASE
	[char] % OF  >r 1+  r> 1-  EXIT ENDOF	\ skip escape and leave
	[char] ! OF  >r 1+  r> 1-  EXIT ENDOF	\ skip escape and leave
	[char] " OF  >r 1+  r> 1-  EXIT ENDOF	\ skip escape and leave
	[char] ' OF  >r 1+  r> 1-  EXIT ENDOF	\ skip escape and leave
    ENDCASE

    -trailing				\ remove trailing blancs
    +trailing				\ remove leading blancs
    dup 0= IF EXIT THEN			\ nothing left, done

    \ Catch things like '* 2' '*3' '/4' (even if they are wrong)
    \ I replace it by the version with the operator at the end,
    \ that well be treated later on:
    \ It's a little bit risky to append to the string, but I think
    \ we shouldn't get into troubles with it...
    over c@ CASE
	[char] / OF
	    [char] / operator-to-end
        ENDOF
	[char] * OF
	    [char] * operator-to-end
	ENDOF
    ENDCASE

    \ Treat trailing '*' and '/'. I assume the user means the contrary ;-)
    2dup + 1- >r  r@ c@ CASE
	[char] / OF
	    bl r@ c!
	    [char] * r@ 1+ c!
	    1+
	ENDOF
	[char] * OF
	    s"  rot * swap " rot over + 1- -rot
	    r@ swap move
	ENDOF
    ENDCASE rdrop

    \ Now we replace all '/' by spaces, so '3/4' well be '3 4'.
    \ It might get wrong, but will normally be what the user wants.
    2dup +  third ?DO
	i c@ [char] / = IF
	    bl i c!
	THEN
    LOOP
;

: (2num-in) ( default-d -- d true | false ) \ allows using FORTH
    depth >r 2>r			\ remembering depth and default
    0 0 0 0 0 0	2r>			\ buffering with zeros, default on top
    depth >r				\ remembering paded depth
    [ decimal ]
    pad dup 80  accept
    ?scale-adaptions			\ guess meaning of scale inputs ;-)
    EVALUATE				\ evaluating FORTH
    [ hex ]
    depth r> < IF |menu-input-error THROW THEN	\ less stack items? error
    r> -rot 2>r >r			\ swap value and depth on return stack
    BEGIN r@ depth 1+ < WHILE drop REPEAT rdrop	\ restoring stack
    2r> ;				\ result

: 2num-in ( default-d -- d true | false )
    ['] (2num-in) CATCH IF	\ |menu-input-error or something
	bell 2drop false
    ELSE true THEN ;

: 2num-in-to-addr ( addr -- )   dup 2@ 2num-in IF rot 2! ELSE drop THEN ;

: change-value-at-addr ( addr -- )
    at-x? c-l 12 - min at-x
    ."  new:       " .bs .bs .bs .bs .bs .bs
    num-in-to-addr ;

\ see 'show-key-bindings'
: change-named-variable ( xt -- )  EXECUTE change-value-at-addr ;

: change-2value-at-addr ( addr -- )
    at-x? c-l 16 - min at-x
    ."  new:       " .bs .bs .bs .bs .bs .bs
    2num-in-to-addr ;

: change-scale-at-addr ( addr -- )	\ checks for divider zero
    >r				( r: addr )
    r@ 2@			( initial-d-value  r: addr )
    (scale-input) on
    r@ change-2value-at-addr
    (scale-input) off
    r@ @ IF
	2drop		\ divider ok
    ELSE		\ zero,
\	r@ 2!		\ restore old value
\	bell ." divider zero." 600 ms
	2drop 0 1 r@ 2!	\ set ratio to 0/1  ;-)
	." ratio set to zero." 300 ms
    THEN rdrop ;

: change-named-scale ( xt -- )  EXECUTE change-scale-at-addr ;

\ Menu entry to display a string and a value at a given address.
\ Set up menu entries to change the value at the given address.
: simple-menu-entry-value ( addr u addr2 -- )
    dup >stack		\ addr2 of value >stack
    redisplay		\ will show the changed value
    @			\ value on top of string
    ['] change-value-at-addr menu-entry-value >last-xy ;

\ Display a string and the value of a named variable.
\ Set up menu entries to change the variable value.
: simple-menu-entry-variable ( addr u xt-of-variable -- )
    dup >stack		\ variable xt will be on stack
    redisplay		\ will show the changed value
    ['] change-named-variable menu-entry-variable >last-xy ;

: select-char-to-addr ( addr -- )
    ( menu-2-at 2@ at-xy ) ." :"
    get-key [ hex ] FF and 
    dup D = IF   \ 'RETURN' is a noop
	2drop
    ELSE
	swap !
    THEN ;

: simple-menu-entry-char ( addr -- )	\ note that the char is stored as cell
    dup @ pad c!
    >stack  redisplay	pad 1	['] select-char-to-addr  menu-entry  >last-xy ;

\ words to deal with scales (double cells to be used with '*/' for scaling)
: .scale ( d -- )   swap . .bs ." /" . ;

\ Menu entry to print a string, display a integer scale value at a named 2addr
\ and set up menu entries to do something with it when selected.
: menu-entry-scale ( addr count xt-of-scale-variable action-xt -- )
    swap >r			( addr count xt  r: xt-of-scale-variable )
    menu-selected menu!		( addr count  r: xt-of-scale-variable )
    r> EXECUTE 2@		( addr count d  r: -- )
    menu-highlite-on
    start-xy># >r 2>r  type 2r> .scale  xy>#   r>
    menu-highlite-off
    2dup (2last-entry-range) 2! DO
	menu-selected  i #>addr  menu-cell-length#  move
    LOOP ;

\ Menu entry to display a string and the integer scale at a named 2variable
\ Set up menu entries to change the scale when selected.
: simple-menu-entry-scale ( addr u xt-of-scale-variable -- )
    dup >stack
    redisplay		\ will show the changed value
    ['] change-named-scale menu-entry-scale >last-xy ;

: change-scale-at-addr-entry ( addr-title count addr-scale -- )
    >r
    r@ >stack	['] change-scale-at-addr	redisplay	menu-entry
    r> 2@ .scale up-to-here ;

TRUE [IF] \ float extensions
base @ decimal

\ Words to allow user input of a float value in everydays syntax,
\ without blocking the use of the Forth text interpreter for calculations.

\ Check (partially) if the string contains a string which the user could
\ have entered meaning a float:
: (maybe-float?) ( addr count -- flag )
    dup 0= IF  2drop FALSE EXIT  THEN		\ empty string
 
    over c@ [char] - = IF  1 /string  THEN	\ skip '-' sign
    dup CASE \ on length			\ very short strings
	0 OF 2drop FALSE EXIT ENDOF
	1 OF
	    over c@ dec-num? IF
		2drop TRUE EXIT
	    ELSE
		2drop FALSE EXIT
	    THEN
	ENDOF
    ENDCASE

    over c@ [char] . = -rot	( starts-with-dot-flag addr count )
    0 DO			( starts-with-dot-flag addr )
	dup i + c@
	dup dec-num? IF drop ELSE
	    dup [char] E = IF  drop [char] e  THEN
	    CASE
		[char] e OF		\ 'e' and 'E'
		    over 0= IF  2drop unloop FALSE EXIT  THEN	\ looks ok
		    \ starts with dot: check char to left of 'e'
		    dup i 1- + c@ dup dec-num? IF drop ELSE
			[char] . <> IF  2drop unloop FALSE EXIT  THEN
		    THEN
		ENDOF
		[char] . OF		\ decimal point
		    i IF
			over IF  2drop unloop FALSE EXIT  THEN	\ second dot
			\ decimal point: check char to left of it
			dup i 1- + c@ dup dec-num? IF drop ELSE
			    [char] - <> IF  2drop unloop FALSE EXIT  THEN
			THEN
		    THEN
		ENDOF
		[char] - OF		\ minus sign
		    i IF				\ must be first or
			dup i 1- + c@ [char] e <> IF	\ just after 'e'
			    2drop unloop FALSE EXIT
			THEN
		    THEN
		ENDOF
		\ all other chars:
		drop 2drop unloop FALSE EXIT
	    ENDCASE
	THEN
    LOOP
    2drop TRUE ;

\ Check if the string contains a string which the user could have entered
\ in everydays syntax, meaning a float.
\ Correct floats do return FALSE, as do strings that cannot be floats anyway.
\ The heuristics are not perfect, but should be good enough, I think.
: maybe-float? ( addr count -- flag )
    2>r
    2r@ (maybe-float?) 0= IF  2rdrop FALSE EXIT  THEN

    2r@ 1- + c@  dup dec-num? IF drop ELSE	\ check last char
	CASE
	    [char] e OF ENDOF
	    [char] E OF ENDOF
	    drop 2rdrop FALSE EXIT
	ENDCASE
    THEN
    2r@ [char] . count-char 1 > IF  2rdrop FALSE EXIT  THEN	\ count '.'
    2r@ [char] - count-char 2 > IF  2rdrop FALSE EXIT  THEN	\ count '-'
    2r@ [char] e count-char  2r> [char] E count-char  +  1 > IF FALSE EXIT THEN
    TRUE ;

[UNDEFINED] (float-string) [IF]   32 STRINGBUF-HANDLE: (float-string)	[THEN]

\ Fixes a string to a correct float string in Forth syntax in place.
\ Attention: Result string can be up to 3 bytes longer than the original!
: fix-float-string ( addr count -- count' )
    over >r

    (float-string) stringbuf-empty
    over c@ [char] - = IF
	[char] - (float-string) char-cat
	1 /string
    THEN

    over c@ [char] . = IF
	[char] 0 (float-string) char-cat
    THEN

    2dup (float-string) cat

    2>r
    2r@ [char] e count-char
    2r@ [char] E count-char + 0= IF	\ add missing 'e'?
	2r@ 1- + c@ dec-num? IF		\ don't do it on nan +inf and such
	    s" e0" (float-string) cat
	THEN
    THEN
    2rdrop

    (float-string) string@  r> swap  >r r@ move
    r> ;

\ Return last word in a string of space separated words:
: last-word ( addr count -- addr' count' )
    bl -trailing-char
    dup 0= IF EXIT THEN

    BEGIN
	s"  " search WHILE
	>r 1+ r> 1-
    REPEAT ;

\ If the last word in the string looks like an attempt to input a float
\ in everydays syntax, replace it by a string in correct Forth syntax.
\ Attention: This may write up to three bytes over the end of the original!
: maybe-replace-last-as-float ( addr count -- addr count' )
    -trailing
    dup 0= IF  EXIT  THEN

    2dup last-word 2dup maybe-float? IF
	dup >r
	fix-float-string r> - +
    ELSE  2drop  THEN ;


: (float-in) ( - f: default -- f: r )	\ allows using FORTH. Might THROW.
    fdepth >r		( f: r=default  r: fdepth )
    6 0 DO 0e0 fswap LOOP		\ buffering with zeros, default on top
    fdepth >r		( f: 0 0 0 0 0 0 default  r: fdepth0 padded-fdepth )
    pad dup 80  accept
    maybe-replace-last-as-float			\ accept everydays float syntax
    base @ >r decimal EVALUATE r> base !	\ evaluating FORTH
    fdepth r> < IF |menu-input-error THROW THEN	\ less stack items? error
    BEGIN r@ fdepth < WHILE fnip REPEAT rdrop ;	\ restoring stacks

: float-in ( r=default -- r true | false )
    ['] (float-in) CATCH IF	\ |menu-input-error or something
	bell fdrop false
    ELSE true THEN ;

: dfloat-in-to-addr ( addr -- )
    >r
    r@ df@ float-in IF r@ df! THEN
    rdrop ;


\ Words to build an ASCII representation from a floating point value:

\ Put float representation into '(scratch-buf)' and exponent into '(scratch)'
\ and return results:
[UNDEFINED] (scratch-buf) [IF]
    decimal 128 STRINGBUF-HANDLE: (scratch-buf)
[THEN]
: float-scratch-represent ( r u -- exponent negative-flag success-flag )
    (scratch-buf)
    dup stringbuf-empty
    2dup string-size!
    buffer-data-addr swap represent
    third (scratch) ! ;		\ exponent for subsequent use

\ Cut trailing zeroes from the string at (scratch-buf), leaving enough
\ to display an integer with exponent 'exp'.
\ If this would result in an empty string, store '0' instead.
\ Returns the string:
: float-scratch/zeroes ( exp -- addr count' ) \ string in (scratch-buf)
    >r
    (scratch-buf) string@ [char] 0 -trailing-char
    dup 0= IF \ zero?
	drop 1
	rdrop
    ELSE
	r> max
    THEN
    dup (scratch-buf) string-size! ;

\ If the float value ASCII representation just fits into u characters return
\ it as string and TRUE, else return FALSE.
\ (use 'float-fits?' for floats fitting in a shorter string).
: float-just-fits? ( r u -- addr count TRUE | FALSE )
    dup >r
    float-scratch-represent nip
    0= IF drop rdrop FALSE EXIT THEN
    r@ = IF
	(scratch-buf) buffer-data-addr r> (float-string) cat
	(float-string) string@
	TRUE EXIT
    THEN
    rdrop FALSE ;

\ Build a ASCII representation string of a float value that can be shown as
\ mantissa only within u characters.
: build-as-mantissa ( r u -- addr count )
    >r

    fdup r@ float-scratch-represent
    0= ABORT" build-as-mantissa: Can't handle this"
    (float-string) stringbuf-empty
    IF  [char] - (float-string) char-cat  THEN
    dup float-scratch/zeroes 2drop

    \ negative exponents: add point and leading zeroes:
    dup 0< IF
	[char] . (float-string) char-cat
	0 swap DO  [char] 0 (float-string) char-cat  LOOP
	fdup r@ float-scratch-represent nip
	0= ABORT" build-as-mantissa: Can't handle this"
	dup float-scratch/zeroes 2drop
    THEN

    r> (scratch-buf) buffered-length min 0 DO
	dup i = IF  [char] . (float-string) char-cat  THEN
	(scratch-buf) buffer-data-addr i + c@ (float-string) char-cat
    LOOP drop
    fdrop
    (float-string) string@ ;

\ Return ASCII representation of the float value and TRUE if it fits as
\ mantissa only (without exponent) in *less* than u characters.
\ (use 'float-just-fits?' for floats *just* fitting).
: float-fits? ( r u -- addr count TRUE | FALSE )
    >r
    fdup r@ float-scratch-represent nip
    0= IF 2drop FALSE EXIT THEN		( r exp  r: u )

    dup float-scratch/zeroes nip	( r exp representation-length  r: u )
    swap 0 min negate +			\ space for .0[...]
    r@ <= IF
	r> build-as-mantissa
	TRUE EXIT
    THEN
    rdrop fdrop FALSE ;

\ Build a string of maximal u characters from real value r.
\ 'r' must be a real number, no infinities, no nan.
: real>string ( r u -- addr count )
    >r					( f: r  r: u=length )
    (float-string) stringbuf-empty

    fdup f0< IF				\ negative?
	s" -" (float-string) string!	\ '-' sign
	r> 1- >r			\ adjust length
    THEN

    fdup r@ float-just-fits? IF		\ just fitting integer
	fdrop rdrop EXIT
    THEN

    r> 1- >r				\ decimal point: adjust length

    fdup r@ float-fits? IF		\ fits without exponent?
	fdrop rdrop EXIT
    THEN

    \ subtract exponent string length:
    r> 1-	( r length )	\ 'e'
    (scratch) @ 1-		\ exponent (as integer)
    dup 0< IF			\ exponent sign?
	>r 1- r> negate
    THEN
    num>string nip -		\ exponent number length

    float-scratch-represent nip		( exponent success-flag )
    0= ABORT" real>string: Can't handle this number."

    (scratch-buf) buffer-data-addr c@ (float-string) char-cat	\ first number
    1-					\ new exponent
    (scratch-buf) string@ [char] 0 -trailing-char
    >r 1+ r> 1-				\ start with second character
    dup IF				\ decimal point if mantissa fraction
	[char] . (float-string) char-cat
    THEN
    (float-string) cat

    [char] e (float-string) char-cat
    dup 0< IF  [char] - (float-string) char-cat  THEN	\ exponent sign
    abs num>string (float-string) cat
    (float-string) string@ ;

\ Top level word to build a ASCII representation of maximal length 'u' from
\ a floating point value.
\ Make the used (short) strings be valid words:
: +inf ( -- F: +infinity )   +infinity ;
: -inf ( -- F: +infinity )   -infinity ;
[UNDEFINED] nan [IF]	: nan ( -- F: nan )   0e0 0e0 f/ ;		[THEN]
\ 0e0 0e0 f/ FCONSTANT nan		\ does *not* work in bigFORTH 2.0.5

: float>string ( r u -- addr count )
    >r
    fdup real? IF \ real value and not infinity
	r> real>string
    ELSE \ infinity or NAN
	fdup infinity? CASE
	    1  OF
		fdrop  r> 8 > IF s" +infinity" ELSE s" +inf" THEN  EXIT
	    ENDOF
	    -1 OF
		fdrop  r> 8 > IF s" -infinity" ELSE s" -inf" THEN  EXIT
	    ENDOF
	    0  OF
		is-NaN? IF
		    rdrop s" nan" EXIT
		ELSE
		    true ABORT" float>string: Format not recognised."
		THEN
	    ENDOF
	ENDCASE
    THEN ;

: float>short-string ( r -- addr count )   [ c-l 8 / ] literal float>string ;

12 VALUE float-display-width
17 VALUE max-dfloat-display-width

\ Standardised floating point output:
: .float ( r -- )   float-display-width float>string type  bl emit ;

\ Floating point output with maximal (sensible) display width:
: .float-wide ( r -- )   max-dfloat-display-width float>string type  bl emit ;

: change-df-value-at-addr ( addr -- )
    last-left clear-line-to-end last-left
    s" Change float value from: " type-other-colour
    dup df@ .float
    s" to: " type-other-colour
    dfloat-in-to-addr ;

\ see 'show-key-bindings'
: change-named-dfloat-var ( xt -- )
    last-left clear-line-to-end last-left
    s" Change " type-other-colour
    dup xt>string type
    s"  from: " type-other-colour
    EXECUTE
    dup df@ .float-wide
    s" to: " type-other-colour
    dfloat-in-to-addr ;

\ Output a floating point value ensuring that it will fit on the line:
: .float-on-same-line ( r -- )
    float-display-width float>string type-on-same-line ;

\ Menu entry to do something on the float value at a given named address-xt.
\ String and value get displayed.
: menu-entry-dfloat-variable ( addr count variable-xt xt-action -- )
    swap >r		( addr count action-xt  r: var-xt )
    menu-selected menu!	( addr count  r: var-xt )
    r>			( addr count var-xt )
    menu-highlite-on
    start-xy># >r >r  type r> EXECUTE df@ .float-on-same-line  xy>#   r>
    2dup (2last-entry-range) 2! ?DO
	menu-selected  i #>addr  menu-cell-length#  move
    LOOP
    menu-highlite-off ;

\ Display a string and the value of a named variable.
\ Set up menu entries to change the variable value.
: simple-dfloat-variable-entry ( addr u xt-of-variable -- )
    dup >stack		\ variable xt will be on stack
    redisplay		\ will show the changed value
    ['] change-named-dfloat-var menu-entry-dfloat-variable >last-xy ;

\ Menu entry displaying a string and a float value,
\ make menu entries to do something on selecting it.
: menu-entry-df-value ( addr count action-xt r -- )
    menu-selected menu!	( addr count r )

    \ display string and value:
    menu-highlite-on
    start-xy># >r  type  .float-on-same-line  xy>#   r>
    2dup (2last-entry-range) 2! ?DO
	menu-selected  i #>addr  menu-cell-length#  move
    LOOP
    menu-highlite-off ;

\ Menu entry to display a string and a dfloat value at a given address.
\ Set up menu entries to change the value at the given address.
: simple-menu-entry-df-value ( addr u addr2 -- )
    dup >stack		\ addr2 of value >stack
    redisplay		\ will show the changed value
    df@			\ value on top of string
    ['] change-df-value-at-addr menu-entry-df-value >last-xy ;

base !

[THEN] \ end float extensions

: toggle-addr ( addr -- )  dup @ -1 xor swap ! ;

\ XOR the contents of address with the mask and store at address:
: xor! ( mask addr -- )   dup @ rot xor swap ! ;

\ XOR the contents of named address given as xt with the mask and store back:
: n-named-xor! ( mask xt-giving-addr -- )  EXECUTE xor! ;

\ A named mask and a named address are *both* given as xt's.
\ XOR the contents of address with the mask and store at address:
: named-xor! ( xt-of-mask xt-giving-addr -- )   >r EXECUTE  r> EXECUTE  xor! ;


[UNDEFINED] title-colors-xt [IF]
    2VARIABLE title-colors-xt
    ' default-color ' blue title-colors-xt 2!
[THEN]

: title-colors ( -- )
    title-colors-xt 2@ execute color-background execute color-foreground ;

: end-title ( -- )   clear-line-to-end reset-colours cr ;

\ Print the menu title set by 'menu-title!' as noop-entry.
2VARIABLE (title)
: .menu-title ( -- )
    title-colors
    (title) 2@ noop-entry
    end-title ;

\ Set menu title string.  Prefix.  Must be used *before* menu entry. 
: menu-title! ( addr count -- )
    2dup menu-selected  >menu-any-data 2!	\ for direct call 
    (menu-scratch-cell) >menu-any-data 2! ;	\ for menu call

\ Start a fresh menu screen and display a title that calls context help if
\ selected.
\ Take this form if you want to include other displayed items in the title.
\ Call 'end-title' later and maybe 'up-to-here'.
: start-title-entry ( addr count -- )
    page
    title-colors	redisplay	['] context-help	menu-entry ;

\ Start a fresh menu screen with a title string.
\ The title line calls context documentation when selected.
: menu-title-entry ( addr count -- )
    start-title-entry end-title up-to-here ;


\ Cooose an xt out of a list:  'choose-xt-menu'

: select-named-item ( -- ) ;	\ just a name for 'show-key-bindings'

DEFER <common-menu-entries>	' noop is <common-menu-entries>
: quit-menu ( -- ) ;		\ just a name for 'show-key-bindings'

: looks-like-named-xt? ( n -- flag )
    depth >r

    ['] xt>string CATCH IF drop rdrop FALSE EXIT THEN
    depth r> 1+ <> IF drop FALSE EXIT THEN	\ good luck...

    nip 0<> ;

\ Word to enter an xt by hand (can only be used from choose-xt-menu):
\ The xt is selected *and* added to the list.
: xt-user-input ( list -- xt | -- )
    depth >r

    page cr
    s" Here you can enter a Forth word by hand." type-other-colour cr
    s" The word will be selected *and* added to the list."
    type-other-colour cr cr
    s" We need the execution token of the word, so either type"
    type-other-colour cr
    s" ' NAME" type-other-colour cr
    s" or give the execution token of the word to use" type-other-colour cr
    s" (the xt must belong to a *named* word)." type-other-colour cr
    cr
    s" This is a very dangerous feature that will probably crash brew,"
    type-alert cr
    s" if you don't know exactly what you do here..." type-alert cr
    cr
    s" (just type <RETURN> to quit savely)" type-other-colour cr
    
[DEFINED] recording? [IF]
    \ Recording capability in brew gives a warning:
    \ We just switch it off elsewhere.
    recording? IF
	cr
	s" Attention: your interaction here will *not* get directly recorded."
	type-alert cr
	s" Please take care yourself (maybe use FORTH input, key '!')."
	type-other-colour cr
    THEN
[THEN]

    cr accept-evaluate IF  rdrop drop bell EXIT  THEN

    depth r@ 1+ <> IF
	bell
	BEGIN			
	    depth r@ > WHILE
	    drop				\ good luck...
	REPEAT
	drop rdrop EXIT
    THEN
    rdrop

    dup looks-like-named-xt? IF
	dup rot >list
	1 unnest-menus EXIT			\ leave menu
    THEN

    2drop bell
    cr cr s" Sorry, could not find a name for that xt. " type-alert cr
    1000 wait-until ;

MENU: choose-xt-men
: .choose-xt-menu ( addr-of-list -- addr-of-list )
    page
    .menu-title

    cr
    [ decimal ] 5 keep-but-scroll-rest
    dup nodes 0 scrolled-range ?DO
	i over n'th-node @
	dup >stack  menu-done	xt>string
	over c@ >r			\ first character
	['] select-named-item  menu-entry cr
	r> #key-same-entry    
    LOOP

    [char] '  over >stack   redisplay	['] xt-user-input	#key-menu-entry

    <common-menu-entries>
    \ override common 'q' entry:
    [char] q  0 >stack  menu-done  ['] noop  #key-menu-entry ;

: choose-xt-menu ( list-xt addr-to-store-xt -- )
    menu-selected >menu-any-data 2@ (title) 2!

    >r	EXECUTE		\ ( addr-of-list   R: addr-to-store-xt)
    choose-xt-men
    ['] .choose-xt-menu menu-display-xt !
    0 >stack menu-done	['] noop	menu-default
    0 >stack menu-done	['] noop	menu-key-default
    do-menu-loop	\ ( addr-of-list -- addr-of-list xt|false )
    ?dup IF
	r> ! drop
    ELSE rdrop drop THEN ;

\ see 'show-key-bindings'
: choose-xt-to-var ( list-xt xt-giving-address -- )  EXECUTE choose-xt-menu ;


\ Words to make the menu entries for choosing an xt out of a list:

\ Make a meny entry that allows selecting an xt out of a list
\ and store it to a given address:
: choose-xt-entry ( list-xt pointer-to-store-xt -- )
    >r			( list-xt  r: pointer-to-xt )
    >stack
    r@ @ xt>string	r> >stack-2	redisplay
    ['] choose-xt-menu	menu-entry ;

\ Make a meny entry that allows selecting an xt out of a list
\ and store it to a variable:
: choose-xt-to-var-entry ( list-xt xt-giving-address-to-store-xt -- )
    >r			( list-xt  r: xt-of-variable )
    >stack
    r@ EXECUTE @ xt>string	r> >stack-2	redisplay
    ['] choose-xt-to-var	menu-entry ;

DEFER <page-see>
\ In brew this will get redefined to see genomes.
\ page-see  ( xt -- )
:NONAME ( xt -- )
	page
	dup					\ save xt ('see' bug)
	[ decimal ] 48 stringbuf-open >r
	s" see "	r@ cat
	xt>string	r@ cat
	bl		r@ char-cat
	r@ string@  ['] EVALUATE CATCH		\ catch 'see' bug
	r> stringbuf-close
	IF					\ 'see' throw
	    bell  ." page-see: Couldn't see "
	    xt>string dup IF
		type  [char] . emit
	    ELSE drop 2drop THEN		\ probably Gforth specific
	ELSE drop THEN ; is <page-see>

\ Make a entry to choose an xt from a list, show the xt's name,
\ and make a entry to <page-see> the names code:
: choose-xt-entry-ext ( key-addr key-count addr count list-xt xt-pointer )
    at? 2>r	( key-addr key-count addr count list-xt xt-pointer  r: x y )
    >r		( key-addr key-count addr count list-xt  r: x y xt-pointer)
    ( list-xt ) >stack	r@ ( xt-pointer ) >stack-2
    ( addr count )	['] choose-xt-to-var	menu-entry
    ( key-addr key-count ) dup IF
	menu-same-key-entry
    ELSE 2drop THEN
    r> r> r>	( xt-pointer y x )
    [ c-l 4 / ] literal + swap at-xy
    ( xt-pointer ) EXECUTE @	menu-wait	redisplay
    dup >stack		xt>string		['] <page-see>	menu-entry ;


\ Choose an xt from a list, execute the selected xt and store result in
\ a variable given as xt (to be used with listed masks, enum's and such):
: set-from-list ( list-xt xt-giving-store-addr -- )
    EXECUTE
    dup dup @ 2>r	( list-xt store-addr  r: store-addr old-value )
    choose-xt-menu
    2r> over @		(  store-addr old-value new-value )
    = IF
	drop
    ELSE
	dup @ EXECUTE swap !
    THEN ;



\ Print 'YES' or 'NO' depending the flag and expand the last menu entry:
: .YES-NO-entry ( flag -- )   menu-highlite-on .YES-NO up-to-here ;

\ Print 'is ON' or 'is off' depending the flag and expand the last menu entry:
: .ON-off-entry ( flag -- )   menu-highlite-on .ON-off up-to-here ;

\ coloured .ON-off-entry variant:
: .ON-off-entry-coloured ( flag -- )
    menu-highlite-on
    ." is "   IF  s" ON " type-bright  ELSE  ." off"   THEN	up-to-here ;

\ Set the cursor to the next active menu field.
\ Note that this does *not* scroll scrolling menus.
: goto-next-menu-item ( -- )
    menu-cell-length# allocate
    ABORT" goto-next-menu-item: Couldn't allocate"  >r	( r: scratch-addr)

    \ Copy actual menu cell to a scratch buffer for later comparison:
    xy>#  dup #>addr  r@  menu-cell-length#  move

    BEGIN	\ Search first cell having a different menu entry:
	1+  [ l-s c-l * ] literal mod
	dup #>addr menu-cell-length#  r@ menu-cell-length#  compare
    UNTIL

    r> free  ABORT" goto-next-menu-item: Couldn't free."	\ free scratch

    c-l /mod at-xy	\ set cursor
    -1 -1 menu-2-at 2!	\ prohibit resetting of the cursor after a selection

    \ Check if it's not a undefined (default) cell: 
    xy># #>addr menu-cell-length#  screen-menu-defaults menu-cell-length#
    compare IF EXIT THEN	\ OK, done

    RECURSE ;	\ if it's a location with default behaviour try again.

\ Show active key bindings:

\ These words would not be needed but give a more readable output for
\ 'show-key-bindings':

: n-named! ( n xt -- )		EXECUTE ! ;
: name-named! ( xt1 xt2 -- )	EXECUTE ! ;
: named-on ( xt -- )		EXECUTE on ;
: named-off ( xt -- )		EXECUTE off ;
: toggle-named ( xt -- )	EXECUTE toggle-addr ;
: named+! ( n xt -- )		EXECUTE +! ;

\ '.ON-off' variant used in 'show-key-bindings' compatible menus:
: named.ON-off ( adress-xt mask-xt -- )
    >r  EXECUTE @  r> EXECUTE  and .ON-off ;

: .x" ( " text to print" -- )
    [char] " parse POSTPONE sliteral
    2 POSTPONE literal 5 POSTPONE literal POSTPONE .screen-column-min
; IMMEDIATE

: keybinding-reaction ( -- exit-flag )
    help-node" Key bindings"
    wait-key-was @ CASE
	[char] q OF  TRUE  ENDOF
	F1%      OF  context-help false  ENDOF
	[char] ? OF  context-help false  ENDOF
	false swap
    ENDCASE ;

\ Show all active key bindings in this menu:
\ Try to give a description of what each key does.
\ The word does not insist too much to understand what the keys do though.
: show-key-bindings ( -- )
    key-menu-array @ dup IF
	page
	key-menu-defaults >menu-xt @		( key-menu-array default-xt )
	title-colors
	." Active keybindings:             default: " dup xt>string type
	key-menu-defaults >menu-cell-flags @ |menu-done and IF
	    ."    EXIT MENU"
	THEN
	end-title

	256 bl ?DO				( key-menu-array default-xt )
	    over menu-cell-length# i *  +	( array default cell-base )
	    dup >menu-xt @ third over <> IF	\ only for not default actions
		cr i emit ."    "
		\				( array default cell-base xt )
		CASE
		    ['] n-named! OF
			dup >menu-parameter-1 @ .
			dup >menu-parameter-2 @ xt>string type
			.x" store (integer)."
		    ENDOF
		    ['] name-named! OF
			dup >menu-parameter-1 @ xt>string ." ' " type
			dup >menu-parameter-2 @ xt>string ."   " type
			.x" store."
		    ENDOF
		    ['] named+! OF
			dup >menu-parameter-1 @ .
			dup >menu-parameter-2 @ xt>string type .x" +!"
		    ENDOF
		    ['] named-on OF
			dup >menu-parameter-1 @ xt>string type .x" on."
		    ENDOF
		    ['] change-named-variable OF
			dup >menu-parameter-1 @ xt>string type
			.x" enter new value (integer)."
		    ENDOF

[DEFINED] change-named-dfloat-var [IF]
		    ['] change-named-dfloat-var OF
			dup >menu-parameter-1 @ xt>string type
			.x" enter new value (floating point)."
		    ENDOF
[THEN]

		    ['] change-named-scale OF
			dup >menu-parameter-1 @ xt>string type
			.x" enter new scale (two integers)."
		    ENDOF
		    ['] named-off OF
			dup >menu-parameter-1 @ xt>string type .x" off."
		    ENDOF
		    ['] n-named-xor! OF
			dup >menu-parameter-1 @ .bin
			dup >menu-parameter-2 @ xt>string type
			.x" invert bit(s)."
		    ENDOF
		    ['] named-xor! OF
			dup >menu-parameter-1 @ xt>string type bl emit
			dup >menu-parameter-2 @ xt>string type
			.x" invert flag(s)."
		    ENDOF
		    ['] toggle-named OF
			dup >menu-parameter-1 @
			dup xt>string type
			.x" switch to "
			EXECUTE @ IF  ." off."  ELSE  ." on."  THEN
		    ENDOF
		    ['] choose-xt-menu OF
			dup >menu-parameter-1 @ xt>string type bl emit
			dup >menu-parameter-2 @ . .x" choose xt from list."
		    ENDOF
		    ['] choose-xt-to-var OF
			dup >menu-parameter-1 @ xt>string type bl emit
			dup >menu-parameter-2 @ xt>string type
			.x" choose from list and store."
		    ENDOF
		    ['] select-named-item OF
			dup >menu-parameter-1 @
			xt>string type  .x" select this item."
		    ENDOF
		    ['] set-from-list OF
			dup >menu-parameter-1 @ xt>string type bl emit
			dup >menu-parameter-2 @ xt>string type bl emit
			.x" set variable with value from list."
		    ENDOF

		    \ default:
		    over >menu-cell-flags @ >r
		    r@ |>stack and IF
			over >menu-parameter-1 @
			r@ |stack-1-is-xt and IF
			    ." ' " xt>string type bl emit
			ELSE
			    . .tab
			THEN
		    THEN
		    r@ |>stack-2 and IF
			over >menu-parameter-2 @
			r@ |stack-2-is-xt and IF
			    ." ' " xt>string type bl emit
			ELSE
			    . .tab
			THEN
		    THEN
		    r@ |>stack-3 and IF
			over >menu-parameter-3 @ . .tab
			r@ |stack-3-is-xt and IF
			    ." ' " xt>string type bl emit
			ELSE
			    . .tab
			THEN
		    THEN
		    rdrop

		    dup xt>string type
		ENDCASE
		>menu-cell-flags @ |menu-done and IF .x" EXIT MENU" THEN

		this-line last-line = IF
		    [ c-l 7 - ] literal at-x
		    title-colors ." (more)" reset-colours
		    wait keybinding-reaction IF
			2drop unloop EXIT
		    THEN
		    page
		THEN
	    ELSE 2drop THEN
	LOOP drop
    THEN drop
    [ c-l 7 - ] literal at-x ."  (all)"
    wait keybinding-reaction drop ;
' show-key-bindings function-key-actions >list

: toggle-highlite-active ( -- )   (highlite-active) toggle-addr ;

: goto-menu-docu ( -- )   help-node" Using menus"  context-help ;
: goto-scroll-docu ( -- )   help-node" Scrolling menus"  context-help ;

\ Display some menu usage hints if there are unused lines at screen bottom.
\ Cursor must be after the last displayed menu text and gets restored.
: .menu-short-help ( -- )
    at?  2dup 2>r  swap	( y x  r: x-original y-original )

    \ If cursor is not at the start of a line, start at next line:
    IF				\ check if there's text on the line
	1+			\ start on next line
	dup last-line > IF  drop 2rdrop  EXIT  THEN
    THEN		( first-free-line   r: x-original y-original )

    >r			( -- r: x-original y-original first-free-line )

    \ We display last line first:
    last-line		( current-line  r: x y first-free-line )
    blue color-background
    last-left
    menu-scroll-lines @ dup IF			\ Scrolling menu?
	(total-scroll-range) @ < IF		
	    white color-foreground
	    s" This menu is a scrolling one.  <PageUp>  <PageDown>  <home>  <end>  <arrows>"
	    redisplay	['] goto-scroll-docu	menu-entry
	    clear-line-to-end up-to-here
	    1-  0 over at-xy			\ new current line
	    dup r@ 1+ < IF			\ space?  '1+' blanc line 
		rdrop drop  reset-colours
		2r> at-xy  EXIT			\ no space left, done.
	    THEN
	    r> 1+ >r				\ let's leave a blanc line
	THEN
    ELSE  drop  THEN		( current-line  r: x y first-free-line )

    cyan color-foreground
    (highlite-active) @ IF		\ make a highlite entry?
	s" " ['] toggle-highlite-active redisplay menu-entry
	white color-foreground blue color-background
	." Highliting activated: "
	menu-highlite-on ."   Active menu text gets displayed like this.  "
	cyan color-foreground blue color-background clear-line-to-end
	up-to-here  cyan color-foreground  blue color-background
	1-  0 over at-xy		\ new current line
	dup r@ < IF
	    rdrop drop  reset-colours
	    2r> at-xy  EXIT		\ no space left, done.
	THEN
    THEN			( current-line  r: x y first-free-line )

    dup r@ > IF
	(highlite-active) dup @ >r off
	s" (Often there are also key bindings to the first letter of a word.  Press 'k')"
	redisplay  ['] show-key-bindings  menu-entry	clear-line-to-end
	r> (highlite-active) !
	1-  0 over at-xy		\ new current line
    THEN
    rdrop drop			( --  r: x-original y-original )

    (highlite-active) dup @ >r off
    s" Select with <TAB>, cursor arrows and <RETURN>, leave with 'q' (or empty spot)"
    redisplay	['] goto-menu-docu	menu-entry	clear-line-to-end
    up-to-here
    r> (highlite-active) !

    reset-colours
    2r> at-xy ;

\ : common-menu-entries ( -- )
:NONAME ( -- )
    [char] k  redisplay	['] show-key-bindings		#key-menu-entry
    [char] q  menu-done	['] noop			#key-menu-entry
    9			['] goto-next-menu-item 	#key-menu-entry	\ tab
    [char] `  redisplay	['] toggle-highlite-active	#key-menu-entry
    false default-function-keys
    .menu-short-help
; is <common-menu-entries>

dup [IF] \ test and usage example	syntax not up to date ############
decimal

: .something ." *SOMETHING*" ;				\ dummy action

\ display routine that displays the menu and sets actions and the like

MENU: (testmenu)
: .testmenu ( -- )
    help-node" "
    s" This is a test menu" menu-title-entry

    cr
    cr s" Here's something hidden"	['] .something		menu-entry
    cr s" Here something hides"		['] .something
					menu-wait redisplay	menu-entry
    cr s" Something Ringing The Bell"   ping
    ['] .something	menu-entry
    cr s" Cute Bell"   	7 >stack	['] emit		menu-entry
    cr 			redisplay	['] words menu-wait	name-menu-entry
    cr					['] quit		name-menu-entry
    cr s" 1 2 3 "	1 >stack	2 >stack-2	3 >stack-3
    ['] .s	redisplay	menu-wait	menu-entry cr
    cr s" one two tree"		same-menu-entry cr
    cr s" leave the menu" menu-done	['] noop		menu-entry cr
    F1%		['] .something		#key-menu-entry
    s" bB"		ping		['] noop		menu-key-entry
    s" sShH"				['] .something		menu-key-entry
    s" qQxX"		menu-done	['] noop		menu-key-entry
    s" 1"  1 >stack	menu-wait	['] .			menu-key-entry
    s" 2"		menu-wait	['] .s			menu-key-entry
;

: test-menu ( -- )
    (testmenu)
    ['] .testmenu menu-display-xt !
    do-menu-loop
; \    free-menus ;
\ test-menu

: adding-an-entry ( -- )
    .tab .tab .tab .tab s" something is here now"
    ['] .something menu-entry ;

VARIABLE testvariable	999 testvariable !
VARIABLE testvariable2	888 testvariable2 !
MENU: top-test-men
: .top-test-menu
    help-node" "
    s" This is a menu test, toplevel." menu-title-entry

    cr
    s" Here you go to a lower level"	['] test-menu  redisplay menu-entry cr
    s" lLdD" menu-same-key-entry
    s" Here you go and leave then"	['] test-menu  menu-done menu-entry cr
    s" mM" menu-same-key-entry
    s" Klick here to add something"	['] adding-an-entry	 menu-entry cr
    s" Want to change this number? " testvariable   simple-menu-entry-value cr
    from-here >xy testvariable2 @ .	redisplay
    s" or this one?" testvariable2 >stack
    ['] change-value-at-addr menu-entry cr
\    s" X" testvariable >stack redisplay
\    ['] change-value-at-addr menu-entry testvariable . up-to-here cr
    s" Crazy number " testvariable2 >stack redisplay
    ['] change-value-at-addr menu-entry testvariable2 @ 2/ 17 + . up-to-here cr
    s" Leave the menu"	menu-done	['] noop	menu-entry cr
    cls ['] bye name-menu-entry
    F1%		['] .something		#key-menu-entry
    [char] s	['] .something		#key-menu-entry
    s" qQxX"		menu-done	['] noop	menu-key-entry ;

: top-test-menu
    top-test-men
    ['] .top-test-menu menu-display-xt !
    do-menu-loop
    free-menus ;

top-test-menu
cr cr .( You left the menu now) cr

[THEN]

[IF]

MENU: test-men

: .test-menu ( -- )
    help-node" "
    s" Test menu:" menu-title-entry

    <common-menu-entries> ;

: test-menu ( -- )
    test-men
    ['] .test-menu menu-display-xt !
    menu-done   ['] noop        menu-key-default
    menu-done   ['] noop        menu-default
    do-menu-loop ;

test-menu

[THEN]

decimal
