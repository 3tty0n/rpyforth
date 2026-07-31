\ world-spots-localized.fs

: world-spot-version ( -- addr count )
    cvs" 	$Id: world-spots-localized.fs,v 1.17 2002/11/05 15:19:11 f Exp $	" ;

\ This file is loaded from 'worlds.fs' if 'localise-spot-data' is TRUE.

\ All spot data (of a given time) is localised for CPU cache consistency.
\ The actual spot pointer of the actual time is called 'hot-spot'.

0 VALUE hot-spot

: SPOT-VARIABLE: ( "name"  offset -- offset+cell )
    ['] hot-spot swap  BASE+OFFSET:  nip ;

\ Word to compile a named spot cell variable with char prefixes ( 'A' 'B' ...)
\ and put them in list 'spot-var-xts':
: define-named-spot-vars ( offset number name-addr name-count -- offset' )
    (scratch-buf) string!
    [ decimal ]
    32 stringbuf-open
    16 stringbuf-open
    rot 0 ?DO	( offset evaluation-buffer name-buffer )
	dup stringbuf-empty				\ building name
	i [char] A +		over char-cat		\ building name
	(scratch-buf) string@	third cat		\ building name

	s" SPOT-VARIABLE: "	fourth string!	\ building evaluation buffer
	dup string@	fourth cat		\ name to evaluation buffer
	>r >r
	r@ string@	EVALUATE		\ evaluate buffer, compile.
	r> r>
	dup string@ get-xt  spot-var-xts  >list	\ add to list
    LOOP
    stringbuf-close
    stringbuf-close ;

\ Generic access to integer spot variables:
\ 0 are the pointers ('fcp'), 1 food, 2 A-quality, 3 B-quality, ...
: n'th-spot-variable ( i -- addr )
    [ debugging @ ] [IF]
	dup 0<  over field-i-planes# < 0= or
	ABORT" n'th-spot-variable: Index out of range."
    [THEN]
    cells hot-spot + ;


\ Defining spot data structure:
0
SPOT-VARIABLE: fcp		\ fcp = 'field cell pointer'
SPOT-VARIABLE: food
' food spot-var-xts >list
' food IS <food>

\ IF YOU CHANGE ORDER HERE, YOU HAVE TO ADAPT 'copy-qualities2future' TOO!
spot-qualities#   s" -quality"	define-named-spot-vars
spot-properties#  s" -property"	define-named-spot-vars
spot-secrets#	  s" -secret"	define-named-spot-vars

spot-var-xts integer-spot-vars copy-simple-list-elements

\ Float spot variables:		NOT IMPLEMENTED FOR THIS VARIANT YET ##########
\ faligned

dup CONSTANT used-spot-data-size#
spot-alignement# n-ALIGN
CONSTANT spot-data-size#	\ size of field entry for one spot


\ Time:

\ There are time-planes structures like this for the different times:
\ one for now, one for the next time, ...

\ The idea is to work from present to future:
\ if something is changing, it changes in the future plain.
\ A time-step makes it appear, then.
\ 'time-step' is switching a cyclic pointer.

0 VALUE time-pointer		\ points to the spots data of selected time

: set-hot-spot ( spot -- )  spot-data-size# *  time-pointer +  to hot-spot ;

\ Different times:

\ Make the present be the time seen:
: present ( -- )
    (spot-data-field)
    (time-index) @  time-plane-length @ *  +
    to time-pointer
    spot @ set-hot-spot ;

\ Make the future be the time seen:
: future  ( -- )
    (spot-data-field)
    (time-index) @ 1+  world-time-planes @ mod  time-plane-length @ *  +
    to time-pointer
    spot @ set-hot-spot ;

\ Step from present to future:
: time-step ( -- )			\ time as a cycle, dizzy?
    (time-index)  dup @ 1+  world-time-planes @  mod  swap !
    1 step +!
    present ;

: present-initializes-future ( -- )	\ initializes the future plain
    time-pointer			\ with the data from the present
    future time-pointer
    time-plane-length @  move
    present ;				\ stay tuned to the present

: >spot! ( i -- )  dup spot !  set-hot-spot ;

0 VALUE spots			\ number of spots in this universe

: enter-world ( addr -- )
    to this-world
    (spots) @ to spots
    present ;		\ initialize to the present.  Does 'set-hot-spot' too.

\ For backward compatibility,
\ Does not make too much sense within new worlds...
future-change-individal 0= [IF]		\ copy all quality changes to future?
\ Copy all spot variables (except food) for all spots from present to future:
: qualities>future ( -- )		\ copy present qualities to future
    spot @
    spots 0 DO
	i >spot!
	2 n'th-spot-variable				\ from address
	future 2 n'th-spot-variable			\ to address
	[ used-spot-data-size# 2 cells - ] literal	\ size
	move
	present
    LOOP
    >spot! ;
[THEN]

\ cells work on *present* food plane, 'world-do' does copy it to the future.
\ ###### WHY?   ^^^^^^^^^  ######################
: food>future ( -- )			\ copy present food to future
    spot @
    spots 0 DO
	i >spot!   food @  future food !  present
    LOOP
    >spot! ;

\ Build a checksum over all integer variables of the cells world:
\ Used by 'assert-state-entry'.

\ Add checksum for one spot (depends on cell size):
: add-spot-sum ( n -- n' )
    [ field-i-planes# 1- ] literal n'th-spot-variable  food DO
	i @ +
    cell +LOOP ;

\ Do it over all spots (depends on cell size):
: world-checksum ( -- n )
    0
    spots 0 DO
	i >spot!
	add-spot-sum
    LOOP ;

\ Preparing the big bang:	\ not very FORTH like internal variables...

CREATE (dim-spots) max-dimensions# cells allot
: (dim-spots) ( u -- addr )   cells (dim-spots) + ;

VARIABLE (dimensions)
VARIABLE (time-planes)

\ Create a new world and make it actual:
: (big-bang) ( time-planes dim-sizeN ... dim-size1 dimensions -- )
    \ calculate size, temporarily storing intermediate results:
    dup (dimensions) !
    0 DO				\ loops over dimensions
	i (dim-spots) !			\ stores # of spots of the dimension
    LOOP
    (time-planes) !

    \ now calculate:
    1
    (dimensions) @ 0 DO  i (dim-spots) @ *  LOOP
    dup >r					( r: spots )
    spot-data-size# *		dup >r		( r: spots time-plane-length )
    (time-planes) @ *		dup >r		( r: spots plane field-length )
    world-header-length# +	dup >r		( r: spots plane field total )
    spot-alignement# +				( size  r: spts plne fld total)
    allocate ( -- adr ior ) 0= IF		( addr   r: ...  )
	dup spot-alignement# n-ALIGN		( addr aligned-a  r: ... )
	dup to this-world
	dup worlds >list
	r@ erase				( addr  r: ... )
	world-allocated !			( r: spots plane field total )
	r> world-length !			( r: spots plane field )
	2 world-version !
	r> total-list-length !			( r: spots plane )
	r> time-plane-length !			( r: spots )
	r> (spots) !				( r: -- )
	spot-data-size# spot-data-size !
	(dimensions) @
	dup world-dimensions !
	dup 3^n directions !
	0 DO
	    i (dim-spots) @   dimension-ranges i cells +  !
	LOOP
	(time-planes) @  world-time-planes !	\ # of times: now, later

	neighbour-vectors-address neighbour-vectors !
	initialise-dim-steps

	this-world enter-world

	\ set default name:
	s" World " string!!  worlds# 1- num>string third cat  world-name !

	-1 step !
	-1 (time-index) !   time-step

	\ Default visibility:
	0 (dim-spots) @ c-l min visibility-off !
	(dimensions) @ 1 > IF
	    1 (dim-spots) @  l-s 1-  min  visibility-off cell+ !
	THEN
	(dimensions) @ 2 > IF
	    1 backgound-off !
	    (dimensions) @ cells  [ 2 cells ] literal DO
		dimension-ranges i + @ visibility-off i + !
	    CELL +LOOP
	THEN

    ELSE rdrop rdrop rdrop rdrop
	drop   false to this-world		\ uups, no space there?
	bell   true ABORT" (big-bang): Big bang failure. nothing to do..."
    THEN ;

