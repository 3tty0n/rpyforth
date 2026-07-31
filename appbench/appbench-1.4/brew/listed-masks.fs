\ listed-masks.fs
\ 	$Id: listed-masks.fs,v 1.5 2003/08/27 18:14:13 f Exp $	

\ Bit-masks that can be written to files as text, i.e. Forth source:
\ Puts the xt of each named mask in a list.

\ Compile option
\	display-compiled-words

\ ****************************************************************
\ file dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" stringbuf-0.4.fs" REQUIRED

\ ****************************************************************
\ LICENSE:

\ listed-masks.fs
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


: LISTED-MASK: ( list bit-position -- list bit-position+1 )
    >r
    save-input
    r>
    MASK:
    >r
    restore-input
    ABORT" LISTED-MASK: Troubles restoring input source"
    bl word find IF
	over  to-list		\ put xt into list
    ELSE bell THEN
    r> ;

\ ****************************************************************
\ some extensions and tools:

\ Handle a buffer with the FORTH source representation of a bitmask pattern.
: listed-mask-string ( bitpattern list-addr -- handle )	\ please close buffer
    [ decimal ] 80 stringbuf-open >r

    s" 0 " r@ string!
    BEGIN			( bitmask actual-node  r: handle )
	next-node
    dup WHILE
	dup @ EXECUTE		( bitmask actual-node actual-mask  r: handle )
	third and IF		( bitmask actual-node  r: handle )
	    dup @ xt>string r@ cat
	    s"  OR " r@ cat
	THEN			( bitmask actual-node  r: handle )
    REPEAT
    2drop
    r> ;

[UNDEFINED] display-compiled-words [IF]
    \ Should some words automatically generating a word family display the
    \ names them when compiling?
    TRUE CONSTANT display-compiled-words
[THEN]

\ Compile two words for each listed mask:
\ 'xxx!' setting the bit
\ 'xxx?' asking if the bit is set
: compile-listed-?-and-! ( base-variable-xt list -- )
    >r		( base-variable-xt  r: list )

[ display-compiled-words ] [IF]
    cr ." compile-listed-?-and-! defining: "
    cr
[THEN]

    [ decimal ]
    32 stringbuf-open
    256 stringbuf-open

    r> dup nodes 0 ?DO	( variable-xt handle-name hndl-evaluation actual-node )
	next-node

	\ define 'xxx! words ( -- )
	s" : " fourth string!
	third >r
	dup @ xt>string		r@ string!
	[char] !		r@ char-cat	\ 'xxx! as name'
	r> string@
[ display-compiled-words ] [IF]
	2dup type	bl emit			\ say what you're doing
[THEN]
	fourth 			cat
	bl			third char-cat
	fourth xt>string	fourth cat
	s"  dup @ "		fourth cat
	third string@ 1-	fourth cat
	s"  or swap ! ; "	fourth cat	\ evaluation string ok
	over string@ EVALUATE			\ compile 'xxx!'

	\ define 'xxx? words ( -- flag )
	s" : " fourth string!
	third >r
	dup @ xt>string		r@ string!
	[char] ?		r@ char-cat	\ 'xxx?' as name
	r> string@
[ display-compiled-words ] [IF]
	2dup type	bl emit			\ say what you're doing
[THEN]
	fourth 			cat
	bl			third char-cat
	fourth xt>string	fourth cat
	s"  @ "			fourth cat
	third string@ 1-	fourth cat
	s"  and 0<> ; "		fourth cat	\ evaluation string ok
	over string@ EVALUATE			\ compile 'xxx?'

    LOOP

[ display-compiled-words ] [IF]
    cr						\ see: say what you're doing
[THEN]

    drop
    stringbuf-close
    stringbuf-close
    drop ;

\ Define a couple ( u ) of listed-masks with a char prepended to
\ the given string as name.
\ Char starts from 'A', increasing alphabetically.
: LISTED-MASKS-pre-char ( list bit addr count u  -- list bit+1)
    >r 2swap r> 0 ?DO
	i [char] A +
	[ decimal ] 32 stringbuf-open >r
	s" LISTED-MASK: " r@ string!
	r@ char-cat
	2over r@ cat
	r@ string@ EVALUATE
	r> stringbuf-close
    LOOP
    2swap 2drop ;
    
\ Define a couple ( u ) of listed-masks with a char appended to
\ the given string as name.
\ Char starts from 'A', increasing alphabetically.
: LISTED-MASKS-append-char ( list bit addr count u  -- list bit+1)
    >r 2swap r> 0 ?DO
	[ decimal ] 32 stringbuf-open >r
	s" LISTED-MASK: " r@ string!
	2over r@ cat
	r> i [char] A + swap >r		\ make i possible
	r@ char-cat
	r@ string@ EVALUATE
	r> stringbuf-close
    LOOP
    2swap 2drop ;
