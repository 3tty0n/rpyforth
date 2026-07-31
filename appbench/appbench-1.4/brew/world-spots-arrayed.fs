\ world-spots-arrayed.fs

: world-spot-version ( -- addr count )
    cvs" 	$Id: world-spots-arrayed.fs,v 1.22 2002/11/05 15:19:10 f Exp $	" ;

\ This file is loaded from 'worlds.fs' if 'localise-spot-data' is FALSE.
\ This version is a lot faster on my system.

\ time-pointer  Pointer to the present times data fields:
\ Always use time-pointer! to set it together with f-time-pointer.
0 VALUE time-pointer	\ pointer to the base of the 1st spot integer field
0 VALUE f-time-pointer	\ pointer to the base of the 1st spot float data field

\ Length of one basic array of integers (one for each spot):
\ Will be initialized to (i-array-length) entering world.
0 VALUE i-array-length

\ Length of one basic array of dfloats (one for each spot):
\ Will be initialized to (f-array-length) entering world.
0 VALUE f-array-length

\ Skip length of all integer arrays of a time and falign.  float-offset
\ Will be initialized to (float-offset) entering world.
0 VALUE float-offset

\ Offsets to spot data in a basic array get set by >spot!
0 VALUE spot-i-offset	\ offset of the spot within a given plane for integers
0 VALUE spot-f-offset	\ offset of the spot for dfloats

0 VALUE spots		\ number of spots in this universe

: time-pointer! ( addr -- )
    dup to time-pointer
    float-offset + to f-time-pointer ;

: compile-multiple-offsets ( u offset-xt -- )
    >r
    dup CASE
	0 OF drop ENDOF
	1 OF
	    drop
	    r@ COMPILE,  POSTPONE +
	ENDOF
	2 OF
	    drop
	    r@ COMPILE,  POSTPONE 2*  POSTPONE +
	ENDOF
	3 OF
	    drop
	    r@ COMPILE,
	    POSTPONE >r
	    POSTPONE r@  POSTPONE 2*  POSTPONE r>  POSTPONE +
	    POSTPONE +
	ENDOF
	4 OF
	    drop
	    r@ COMPILE,  POSTPONE 2*  POSTPONE 2*  POSTPONE +
	ENDOF
	8 OF
	    drop
	    r@ COMPILE,  POSTPONE 2*  POSTPONE 2*  POSTPONE 2*
	    POSTPONE +
	ENDOF

	\ default:
	r@ COMPILE,  POSTPONE literal  POSTPONE *  POSTPONE +
    ENDCASE
    rdrop ;

: SPOT-VARIABLE: ( "name"  plane-index  -- plane-index+1 )
    dup >r
    :
	POSTPONE time-pointer
	r>  ['] i-array-length  compile-multiple-offsets
	POSTPONE spot-i-offset  POSTPONE +
    POSTPONE ;
    1+ ;

: df-SPOT-VARIABLE: ( "name"  plane-index  -- plane-index+1 )
    get-name >r
    dup >r
    :
	POSTPONE f-time-pointer
	r>  ['] f-array-length  compile-multiple-offsets
	POSTPONE spot-f-offset  POSTPONE +
    POSTPONE ;
    r@ string@ get-xt dfloat-spot-vars >list
    r> stringbuf-close
    1+ ;

false [IF] \ testing
    0
    SPOT-VARIABLE: A0
    SPOT-VARIABLE: A1
    SPOT-VARIABLE: A2
    SPOT-VARIABLE: A3
    SPOT-VARIABLE: A4
    SPOT-VARIABLE: A5
    1+ 1+
    SPOT-VARIABLE: A8
    see A0
    see A1
    see A2
    see A3
    see A4
    see A5
    see A8
    cr .s drop cr bye
[THEN]

\ Word to compile a family of named spot variables with char prefixes
\ ( 'A' 'B' ...)  and put them in list 'spot-var-xts':
\ Used by both define-named-i-spot-vars and define-named-f-spot-vars
: define-named-spot-vars ( defining-xt index u name-addr name-count -- index' )
    (scratch-buf) string!
    [ decimal ]
    32 stringbuf-open
    16 stringbuf-open
    rot 0 ?DO	( index evaluation-buffer name-buffer )
	dup stringbuf-empty				\ building name
	i [char] A +		over char-cat		\ building name
	(scratch-buf) string@	third cat		\ building name

	fourth xt>string	fourth string!	\ building evaluation buffer
	bl third		char-cat
	dup string@		fourth cat	\ name to evaluation buffer
	>r >r
	r@ string@	EVALUATE		\ evaluate buffer, compile.
	r> r>
	dup string@ get-xt  spot-var-xts  >list	\ add to list
    LOOP
    stringbuf-close
    stringbuf-close
    nip ;

\ Word to compile a family of named integer spot variables with char prefixes
\ and put them in list 'spot-var-xts':
: define-named-i-spot-vars ( defining-xt index u name-addr count -- index' )
    2>r 2>r  ['] SPOT-VARIABLE:  2r> 2r>  define-named-spot-vars ;

\ Word to compile a family of named dfloat spot variables with char prefixes
\ and put them in list 'spot-var-xts':
: define-named-f-spot-vars ( defining-xt index u name-addr count -- index' )
    2>r 2>r  ['] df-SPOT-VARIABLE:  2r> 2r>  define-named-spot-vars ;


\ Generic access to integer spot variables:
\ 0 are the pointers ('fcp'), 1 food, 2 A-quality, 3 B-quality, ...
: n'th-spot-variable ( i -- addr )
    i-array-length *  time-pointer +  spot-i-offset +  ;

\ Generic access to dfloat spot variables:
: n'th-spot-f-variable ( i -- addr )
    f-array-length *  f-time-pointer +  spot-f-offset +  ;

: n'th-spot-f-var-xt ( df-spot-var-index -- xt )
    dfloat-spot-vars n'th-node @ ;

\ Defining spot integer data structure:
0
SPOT-VARIABLE: fcp		\ fcp = 'field cell pointer'
SPOT-VARIABLE: food
' food spot-var-xts >list
' food IS <food>

\ IF YOU CHANGE ORDER HERE, YOU HAVE TO ADAPT 'copy-qualities2future' TOO!
spot-qualities#   s" -quality"	define-named-i-spot-vars
spot-properties#  s" -property"	define-named-i-spot-vars
spot-secrets#	  s" -secret"	define-named-i-spot-vars
CONSTANT spot-float-start-index

spot-var-xts integer-spot-vars copy-simple-list-elements

0
spot-f-qualities#  s" -f-quality"	define-named-f-spot-vars
spot-f-properties# s" -f-property"	define-named-f-spot-vars
spot-f-secrets#	   s" -f-secret"	define-named-f-spot-vars
drop

: spot-dfloat-addr ( continuous-spot-var-index -- addr )
    spot-float-start-index - n'th-spot-f-variable ;


\ Time:

\ There are time-planes structures like this for the different times:
\ one for now, one for the next time, ...

\ The idea is to work from present to future:
\ if something is changing, it changes in the future plain.
\ A time-step makes it appear, then.
\ 'time-step' is switching a cyclic pointer.

\ Different times:

\ Make the present be the time seen:
: present ( -- )
    (spot-data-field)
    (time-index) @  time-plane-length @ *  +
    time-pointer! ;

\ Make the future be the time seen:
: future  ( -- )
    (spot-data-field)
    (time-index) @ 1+  world-time-planes @ mod  time-plane-length @ *  +
    time-pointer! ;

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

: >spot! ( i -- )  dup spot !
    dup cells to spot-i-offset
    dfloats to spot-f-offset ;

\ compiled above for cache consistency
\ 0 VALUE spots			\ number of spots in this universe

: enter-world ( addr -- )
    to this-world

    \ On my sytem values are faster than 'variable @' so I copy it to values:
    (spots) @ to spots
    (i-array-length) @ to i-array-length
    (f-array-length) @ to f-array-length
    (float-offset) @ to float-offset
    present ;					\ initialize to the present.

\ For backward compatibility,
future-change-individal 0= [IF]		\ copy all quality changes to future?
\ Copy all spot variables (except food) for all spots from present to future:
: qualities>future ( -- )		\ copy present qualities to future
    i-array-length 2* >r				\ offset to time-pointr
    time-pointer r@ +					\ from address
    future time-pointer r> +  present			\ to address
    i-array-length [ field-i-planes# 2 - ] literal *	\ length
    move						\ move integers

    f-time-pointer					\ from address
    future f-time-pointer present			\ to address
    f-array-length field-df-planes# *  move ;		\ move dfloats
[THEN]

\ cells work on *present* food plane, 'world-do' does copy it to the future.
\ ###### WHY?   ^^^^^^^^^  ######################
: food>future ( -- )			\ copy present food to future
    i-array-length >r

    time-pointer r@ +			\ from address
    future time-pointer r@ +  present	\ to address
    r> move ;

\ Build a checksum over all variables of the cells world:
\ Used by 'assert-state-entry'.
: world-checksum ( -- n )
    0						\ init sum
    time-pointer >r
    field-i-planes# i-array-length *  r@ +	\ end address
    i-array-length r> +				\ start address
    DO
	i @ +
    cell +LOOP

[ spot-floats# ] [IF]
    f-time-pointer >r
    field-df-planes# f-array-length *  r@ +		\ end address
    r>							\ start address
    DO
	i @ +
    cell +LOOP						\ dfloats as 2 cells
[THEN]
;

\ Preparing the big bang:	\ not very FORTH like internal variables...

CREATE (dim-spots) max-dimensions# cells allot
: (dim-spots) ( u -- addr )   cells (dim-spots) + ;

VARIABLE (dimensions)
VARIABLE (time-planes)

\ Create a new world and make it actual:
: (big-bang) ( time-planes dim-sizeN ... dim-size1 dimensions -- )
    \ Calculate size, temporarily storing intermediate results:
    dup (dimensions) !
    0 DO				\ loops over dimensions
	i (dim-spots) !			\ stores # of spots of the dimension
    LOOP
    (time-planes) !

    \ Now calculate:
    1
    (dimensions) @ 0 DO  i (dim-spots) @ *  LOOP
    dup >r					( r: spots )
    [ field-i-planes# cells
    dfaligned field-df-planes# dfloats +
    ] literal *  dup >r				( r: spots time-plane-length )
    (time-planes) @ *		dup >r		( r: spots tplane field-length)
    world-header-length# +	dup >r		( r: spots t-plane field total)
    spot-alignement# +				( size  r: spts tpln fld total)
    allocate ( -- adr ior ) 0= IF		( addr   r: ...  )
	dup spot-alignement# n-ALIGN		( addr aligned-a  r: ... )
	dup to this-world
	dup worlds >list
	r@ erase				( addr  r: ... )
	world-allocated !			( r: spots t-plane field total)
	r> world-length !			( r: spots t-plane field )
	2 world-version !
	r> total-list-length !			( r: spots t-plane )
	r> time-plane-length !			( r: spots )
	r@ cells				( i-array-length  r: spots )
	dup (i-array-length) !
	field-i-planes# *
	dfaligned (float-offset) !		( r: spots )
	r@ dfloats (f-array-length) !
	r> (spots) !				( r: -- )
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
