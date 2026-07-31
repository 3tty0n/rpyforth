\ basics.fs
\ 	$Id: basics.fs,v 1.78 2005/04/09 19:50:44 f Exp $	

\ Some basic definitions I use in my programs.

\ ****************************************************************
\ LICENSE:

\ basics.fs
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
\ File dependencies:

\ Used outside of brew there could be unresolved file dependencies.
\ Please note that REQUIRED might not be usable yet.

\ test if  found?  is defined:
bl parse found? dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	: found? ( "name<bl>" -- flag )   bl word find nip ;	[THEN]

: not-found? ( "name<bl>" -- flag )   found? 0= ;

\ Check for 'system-dependent.fs' loaded (which defines SYSTEM-DEPENDENT.FS)
bl parse SYSTEM-DEPENDENT.FS dup pad c! pad char+ swap chars move pad find nip
0= [IF]  s" system-dependent.fs" INCLUDED  [THEN]
\ BTW:  REQUIRED should work now.

\ Check for 'common-words.fs' loaded (which defines COMMON-WORDS.FS)
bl parse COMMON-WORDS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]  s" common-words.fs" INCLUDED		[THEN]

\ Test if  cvs"  is defined:
\ (not really basic, but makes file dependencies easier to manage)
bl parse cvs" dup pad c! pad char+ swap chars move pad find nip 0=
[IF]
\ Word to avoid cvs id's from being expanded in other files:
: cvs" ( "CVS ID" -- addr count )
    [char] " parse swap 6 + swap 12 - 2 max POSTPONE sliteral ; IMMEDIATE
[THEN]

\ ****************************************************************

decimal


cell 8 * CONSTANT bits/cell
\ Compiler syntax:

\ Offsets:  As a naming convention I use '>name' for offsets.

\ Word used internally by the other offset creating words:
TRUE [IF] \ Which is faster?
: (offset:) ( "name"  offset -- offset )
    CREATE
	dup ,
    DOES> ( addr -- addr' )
	@ + ;
[ELSE]
: (offset:) ( "name"  offset -- offset )
    dup >r
    : ( addr -- addr' )   r> POSTPONE literal  POSTPONE +  POSTPONE ;
;
[THEN]

false [IF] \ Simple version
: OFFSET: ( "name"  offset -- offset'=offset+cell )   (offset:) cell+ ;

[ELSE] \ Special treatment for zero offset.

: (zero-offset:) ( "name"  0 -- 0 )  :  POSTPONE ;   ;

\ Internal offset creating word, replacing zero offset by special version
\ (does not change parameter)
: (smart-offset:) ( "name"  offset -- offset )
    dup IF
	(offset:)
    ELSE
	(zero-offset:) immediate
    THEN ;

\ Create a cell wide (smart) offset variable:
: OFFSET: ( "name"  n -- n+cell )   (smart-offset:) cell+ ;
[THEN]

\ Create a df-offset-variable. The base (at offset zero) *must* be dfaligned!
: dfloat-OFFSET: ( "name"  offset-in-bytes -- offset' )
    dfaligned
    (smart-offset:)  [ 1 dfloats ] literal + ;

\ Words to compile named offsets to a given base referenced by a xt:
: (base+offset:) ( "name"  xt offset -- xt offset )
    2dup 2>r
    :   r> r> compile,
    dup IF				\ normal case, non zero offset:
	POSTPONE literal   POSTPONE +
    ELSE drop THEN			\ special case, zero offset:

    POSTPONE ;
;

: BASE+OFFSET: ( "name"  xt offset -- xt offset+cell )   (base+offset:) cell+ ;

: BASE+dfloatOFFSET: ( "name"  xt offset -- xt offset+df-cell )
    dfaligned
    (base+offset:) [ 1 dfloats ] literal + ;


\ Words to compile named offsets to a given base referenced by a pointer xt:
: (pointer+offset:) ( "name"  pointer-xt offset -- xt offset )
    2dup 2>r
    :   r> r> compile, POSTPONE @
    dup IF				\ normal case, non zero offset:
	POSTPONE literal   POSTPONE +
    ELSE drop THEN			\ special case, zero offset:

    POSTPONE ;
;

\ Compile a cell variable at offset from the address returned by xt:
: POINTER+OFFSET: ( "name"  pointer-xt offset -- xt offset+cell )
    (pointer+offset:) cell+ ;

\ Compile a dfloat variable at offset from the address returned by xt:
: POINTER+dfloatOFFSET: ( "name"  pointer-xt offset -- xt offset+cell )
    (pointer+offset:) [ 1 dfloats ] literal + ;


: MASK: ( bit-position -- bit-position+1 )
    dup 1 swap lshift CONSTANT
    1+
    dup bits/cell > ABORT" MASK: Too many bits in bit-mask" ;

\ Compile constants with inreasing values
[UNDEFINED] enum: [IF]		: ENUM: ( n -- n+1 ) dup CONSTANT 1+ ;	[THEN]

\ 'ENUM:' constants that can be written to files as readable text:
\ Puts the xt of each enum constant in a list.
DEFER to-list
: LISTED-ENUM: ( list value -- list value+1 )
    >r
    save-input
    r>
    ENUM:
    >r
    restore-input
    ABORT" LISTED-ENUM: Troubles restoring input source"
    bl word find IF
	over  to-list		\ put xt into list
    ELSE bell THEN
    r> ;


\ Alignement:

\ Align 'n' (upwards) to 'alignement', which must be a power of two: 
: n-ALIGN ( n alignement -- n' )
    dup 0= IF drop EXIT THEN

    2dup mod IF    
	>r  r@ negate AND  r> +
    ELSE drop THEN ;


\ Stack:

: third ( x1 x2 x3 -- x1 x2 x3 x1 )  2 pick ;
: fourth ( x1 x2 x3 x4 -- x1 x2 x3 x4 x1 )  3 pick ;
: fifth ( x1 x2 x3 x4 x5 -- x1 x2 x3 x4 x5 x1 )  4 pick ;


\ Arithmetics:

: +1! ( addr -- )  1 swap +! ;

: m/ ( d n -- )    1 swap m*/ d>s ;

\ As CATCHing 0 / (and others) can give error messages we
\ might need to do this system specific.
[UNDEFINED] ?/ [IF]

\ use this if / 0 and other malformed combinations dont give error messages.
: ?/ ( n n -- n|0 )   ['] / CATCH IF 2drop 0 THEN ;

    FALSE [IF] \ Handmade version. Trying to avoid crashes:
	\ Doing it by hand is not trivial, because you might have to
	\ watch for malformed combinations here.  Overflow or so
	\ can produce such sometimes.  I can't exclude that there are
	\ other combinations that should be included here.
	\ I guess it's processor dependent too.
	: ?/ ( n n -- n )
	    dup IF
		over [ decimal ] 2147483648 = IF	\ check for NAN
		    dup -1 = IF			\ this combination crashes!
			2drop 0			\ meaningless result...
			EXIT
		    THEN
		THEN
		/
	    ELSE 2drop 0 THEN ;

	\   old handmade version, NOT identical, ended like this:
	\   ELSE drop THEN ;	\ result differed from catch version!
    [THEN]

[THEN]

\ Use '*/' to scale a value at addr1 by the rate at addr2
: addr-rate ( addr-of-value-to-rate addr-of-rating-factor -- )
    >r
    r@ 2@	 = IF drop rdrop EXIT THEN	\ ratio = 1
    r@ cell+ @ 0 = IF off  rdrop EXIT THEN	\ ratio = 0
    dup @ r> 2@ */ swap ! ;			\ others


\ Number representation:

: num>string ( n -- c-addr u )	dup abs 0 <# #s rot sign #> ;

: dec-num? ( char -- flag )   [char] 0 [ char 9 1+ ] literal WITHIN ;

: .bin ( n -- )   base dup @ 2>r   2 base !  .   r> r> ! ;
: .hex ( n -- )   base dup @ 2>r   hex .   r> r> ! ;


\ Bit masks and bit ranges:

: or! ( mask addr )  dup @ rot or swap ! ;

\ Word to set a range of bits:
: set-bitrange ( mask upper+1 lower -- mask' )
    ?DO
	1 i lshift OR
    LOOP ;

\ Word to set the n lowest bits:
\ Only for 2 complement and 8 bit byte.
: set-n-low-bits ( u -- bit-pattern )
    dup [ cell 8 * ] literal < IF
	1 swap lshift 1- EXIT
    THEN
    drop
    -1 ;


\ Misc things:

\ In the following usage example passing an xt (to set menu prefixes) to
\ 'default-function-keys' saying ' NOOP default-function-keys would give
\ a wrong impression.  So using FALSE instead is allowed:
: ?EXECUTE ( xt|0 -- ) dup IF EXECUTE EXIT THEN drop ;

\ Resize an allocated area at addr of old-length to the double
\ and erase new upper half:
: double-allocated ( addr old-length -- addr ior )
    dup >r
    2* resize
    dup 0= IF
	over r@ + r@ erase	\ erase new area
    THEN rdrop ;

\ allocate and erase memory
: allocate-clean ( u -- addr )
    dup allocate
    ABORT" allocate-clean: Couldnt allocate memory."	( u addr )
    dup rot erase ;					( addr )

VARIABLE (scratch)		\ use with caution!


\ Strings:

: char-search-backwards ( addr count char -- count' true | false )
    over 0= IF drop nip  EXIT THEN	\ empty string
    >r

    1-
    BEGIN	( addr count  r: char )
	2dup + c@ r@ <>
    WHILE
	1-
	dup 0<
    UNTIL
	2drop FALSE
    ELSE
	nip TRUE
    THEN
    rdrop ;

\ Count occurances of char in a string:
: count-char ( addr count char -- u )
    0 2swap
    bounds ?DO   ( char count )
	over i c@ = IF 1+ THEN
    LOOP
    nip ;

: +trailing ( addr count -- addr' count' )	\ remove leading blancs
    BEGIN
	over c@ bl =
    WHILE
	1 /string
    REPEAT ;

\ remove all occurences 'char'
: -trailing-char ( addr count char -- addr' count' )
    over 1 < IF drop EXIT THEN

    -rot
    0 over 1- DO	( char addr count )
	over i + c@ fourth <> IF
	    LEAVE
	THEN
	1-
    -1 +LOOP
    rot drop ;

\ Test a char if it's a space or tab:
: bl? ( char -- flag )   dup bl = swap  9 =  or ;

\ Skip leading spaces and tabs:
: bl-skip ( addr count -- addr' count' )
    BEGIN
	dup 0= IF  EXIT  THEN
	over c@ bl?
    WHILE
	1 /string
    REPEAT ;

\ Skip leading spaces and tabs, counting
: bl-skip_ ( addr count -- addr' count' n )
    0 >r
    BEGIN
	dup 0= IF  r> EXIT  THEN
	over c@ bl?
    WHILE
	1 /string
	r> 1+ >r
    REPEAT
    r> ;

: control-char? ( c -- flag )   bl < ;

\ Return next word from the parse area:
: (get-word) ( "<spaces>ccc<space>"  -- addr count )   bl parse bl-skip ;

\ There might be tabs or other control tokens in this.
\ Remove them from the first one (including) and reset >IN
: get-word
    (get-word)
    dup 0 ?DO	( addr count )
	over i + c@ control-char? IF
	    i - negate >IN +!
	    i UNLOOP EXIT
	THEN
    LOOP ;

\ Skip until char (including).
: skip-until-char ( addr count char -- addr' count' )
    >r
    BEGIN
	dup WHILE
	over c@ r@ <> WHILE
	1 /string
    REPEAT 1 /string THEN
    rdrop ;

\ Separate next word from a string:
\ The string stays on stack and get's adjusted.
\ The separated word is returned on top of it. Leading delimiters get skipped.
\ When all done with the input string drop it and return false.
: next-word ( inp-addr inp-count --  inp-addr' inp-count' addr count |FALSE )
    bl-skip
    dup 0= IF  2drop FALSE EXIT  THEN

    -1
    BEGIN	( addr count tested-length  r: handle )
	1+
	third over + c@ bl? IF
	    TRUE
	ELSE
	    2dup =
	THEN
    UNTIL	( addr count tested-length )

    third >r >r	( addr count  r: addr tested-length )
    r@ /string
    2r> ;


\ Files:

: file-exists? ( addr count -- flag )
    r/o open-file IF  drop FALSE EXIT  THEN
    close-file drop true ;

\ Inside brew these are defined in file  compile-options.fs

\ Flush files after writing?
bl parse flush-files dup pad c! pad char+ swap chars move pad find nip 0=
[IF]   TRUE CONSTANT flush-files		[THEN]

\ I/O line buffer size:
bl parse file-line-max# dup pad c! pad char+ swap chars move pad find nip 0=
[IF]   decimal 4048 CONSTANT file-line-max#	[THEN]


: append-to-file ( addr count id -- )	\ file must be opened and writable
    >r
    r@ file-size
    ABORT" append-to-file: Couldn't do file-size."
    r@ reposition-file
    ABORT" append-to-file: Couldn't reposition-file."
    r@ write-line
    ABORT" append-to-file: Couldn't write line."
[ flush-files ] [IF]
    r> flush-file
    ABORT" append-to-file: Couldn't flush-file."
[ELSE]
    rdrop
[THEN] ;

\ Clone a file line by line, appending a linefeed if there isn't one.
\ An empty file get's created, but no linefeed inserted.
: clone-file ( source-name-addr source-name-count dest-addr dest-count -- )
    2dup w/o CREATE-FILE	\ create destination file
    IF
	bell
	drop cr type
	cr ." clone-file: Couldn't create destination file " type cr
	3000 ms
	EXIT
    THEN
    >r 2drop	( source-name-addr source-name-count  r: destination-id )

    2dup r/o OPEN-FILE		\ open source file
    IF
	bell
	drop
	cr type
	cr ." clone-file: Couldn't open source file " type cr
	r> close-file
	IF ." clone-file: Couldn't close destination file" cr THEN
	3000 ms
	EXIT
    THEN
    >r 2drop r>		( source-file-id  r: destination-file-id )

    file-line-max# allocate
    ABORT" clone-file: Couldn't allocate."
    BEGIN	( source-file-id buffer-addr  r: destination-file-id )
	dup file-line-max# fourth read-line
	0=
    WHILE	( source-file-id buffer-addr count flag  r: dest-file-id )
	IF
	    dup file-line-max# =
	    ABORT" clone-file: Increase 'file-line-max#'."
	    over swap r@ write-line
	    ABORT" clone-file: Couldn't write line."
	    false
	ELSE
	    true
	THEN
    UNTIL
    ELSE
	." clone-file: Couldn't read input line."
    THEN
    drop
    free ABORT" clone-file: Couldn't free."
    close-file ABORT" clone-file: Couldn't close source file."
    r> close-file ABORT" clone-file: Couldn't close destination file" ;


\ Floats:
decimal 1 dfloats CONSTANT dfcell

: dfVARIABLE ( compilation: "name"   execution: -- addr )
    CREATE
    0e0 here
    dfcell allot
    df! ;

: df+! ( r addr -- )   >r  r@ df@ f+ r> df! ;

: f>s ( r -- n )   f>d d>s ;
: s>f ( n -- r )   s>d d>f ;

\ Floating point range check: behaves like integer within:
\ (Defined unconditionally, so I know what it does).
: fwithin ( F:r1 r2 r3 -- flag )
    frot fover fover fswap f- f0< 0= IF
	fdrop fdrop fdrop
	FALSE EXIT
    THEN fswap fdrop

    fswap f- f0< 0= ;

: is-NaN? ( F:r -- flag )   fdup f= 0= ;

 1E0 0E0 f/ FCONSTANT +infinity
-1E0 0E0 f/ FCONSTANT -infinity
: nan ( -- F: nan )   0e0 0e0 f/ ;

\ Codes for different float types:
-1
ENUM: -inf%
ENUM: real%
ENUM: +inf%
ENUM: nan%
drop

: infinity? ( F:r -- -1|0|+1 )			\ returns FALSE or sign
    fdup +infinity f= IF fdrop +inf% EXIT THEN
    -infinity f= ;				\ assuming TRUE = -1

: real? ( F:r -- flag )				\ real value and not infinity?
    fdup is-NaN? IF  fdrop FALSE EXIT  THEN
    infinity? 0= ;

: float-type ( F:r -- code )	\ code is -inf%  real%  +inf%  or nan%
    fdup is-NaN? IF  fdrop nan% EXIT  THEN
    infinity? ;

\ fabs replacing NaNs by +inf
\ Used to sum up errors.
: fabs-replace-NaN ( r -- r' )
    fdup is-NaN? IF  fdrop +infinity  THEN
    fabs ;

\ Do a float to cell wide integer conversation.
\ Limit to a (fixed) fraction of the possible range to allow a number of values
\ to be added thereafter.
: f>s-limited ( r -- n )
    fdup [ highest-integer# 64 / s>f ] fliteral f< IF
	fdup [ lowest-integer# 64 / s>f ] fliteral f> IF
	    f>s  EXIT
	ELSE fdrop					\ very low values, -inf
	    [ lowest-integer# 64 / ] literal  EXIT	\ any low value...
	THEN
    THEN fdrop

    \ very high values and +infinity
    [ highest-integer# 64 / ] literal ;			\ any high value...

\ Don't forget alignement.
\ : BASE+dFLOAT-OFFSET: ( "name"  xt offset -- xt offset+fcell )
\     (base+offset:) dfcell + ;


CREATE BASICS.FS 		\ mark, as REQUIRED might not work yet
