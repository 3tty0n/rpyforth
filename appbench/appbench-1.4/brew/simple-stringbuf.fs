\ simple-stringbuf.fs
\ 	$Id: simple-stringbuf.fs,v 1.5 2003/08/25 21:45:47 f Exp $	

\ s-buffs factored out from 'stringbuf-0.3.fs'.

\ Simplified, faster set of words, without length checking, handles, etc.

\ naming: prefix 's-' like 'simple',
\                     or 'symbols' (from brews symbols stack).

\ A s-buf is represented by the address of it's descriptor.


\ ****************************************************************
\ LICENSE:

\ simple-stringbuf.fs
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

\ ****************************************************************

\ Compile options:
\ 	s-bufs-IMMEDIATE
\ Attention: because of the IMMEDIATE statements
\ some s-buf words are *compile only* if compiled with s-bufs-IMMEDIATE off!
\ Define compile option  s-bufs-IMMEDIATE  if not found
bl parse s-bufs-IMMEDIATE dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	true CONSTANT s-bufs-IMMEDIATE	[THEN]

\ ****************************************************************


\ s-buf-descriptor structure:
\ 	count	stack-pointer-as-offset
\ 	addr	stack-bottom
\ 	size


s-bufs-IMMEDIATE [IF]

: s-buf-count ( s-buf -- count )   POSTPONE @ ; IMMEDIATE
: s-buf>string ( s-buf -- addr count )   POSTPONE 2@ ; IMMEDIATE
: s-buf-clear ( s-buf -- )   POSTPONE off ; IMMEDIATE

[ELSE] \ s-bufs-IMMEDIATE is OFF

: s-buf-count ( s-buf -- count )   @ ;
: s-buf>string ( s-buf -- addr count )   2@ ;
: s-buf-clear ( s-buf -- )   off ;

[THEN] \ s-bufs-IMMEDIATE

: s-buf>addr ( s-buf -- addr )   cell+ @ ;

: s-buf>size ( s-buf -- size )   [ 2 cells ] literal + @ ;


\ Remember: it's your job to make sure the buffer is allocated and big enough!
\ this version does *not* do any size or allocation control. 
: s-buf-cat ( addr count s-buf )
    >r
    r@ 2@ + swap  dup >r  move
    r> r>  +! ;

: s-buf-char-cat ( c s-buf -- )
    swap over s-buf>string + c!
    1 swap +! ;

: s-string! ( addr count s-buf -- )   dup s-buf-clear  s-buf-cat ;

\ why not have named s-bufs?
\ Calling them by name *does* allocation if needed and gives the descriptor
\ (the descriptor is at body address)
\ After that the pointer can be kept without calling the name again.
\ Or you can work directly on the string, if you want to
: S-BUF: ( fixed-size -- )
    CREATE
	0 ,		\ descriptor: count
	0 ,		\ not allocated
	,		\ size
    DOES> ( -- s-buf )
	dup cell+ @	IF EXIT THEN	\ done, if already allocated
	>r		( r: body )
	r@ cell+ cell+ @ allocate
	ABORT" Named s-buf couldn't allocate."
	r@ cell+ !		\ allocated address
	r@ off			\ count off
	r> ;	( -- s-buf=body )

\ Always close s-bufs before a SAVESYSTEM
: s-buf-close ( s-buf -- )
    dup cell+ @  ?dup IF
	free ABORT" s-buf-close: Couldn't free."
	>r 0. r> 2!
    ELSE
	drop
    THEN ;

\ ****************************************************************

false [if] \ testing and usage example
    cr .( testing s-buf's...)
    cr

    s-bufs-IMMEDIATE [IF]
	cr .( ATTENTION: because of the IMMEDIATE statements)
	cr .( some s-buf words are *compile only* if compiled with s-bufs-IMMEDIATE on.)
	cr .( Turn it off *before* compiling.)
	cr
    [THEN]

    &80 S-BUF: s-test

: .t  s-test s" |<-- count= " type s-buf-count . ;

: t
    cr ." opened:" 
    s-test s-buf>string cr type .t
    s" s-buf-cat: first" s-test s-buf-cat
    s-test s-buf>string cr type .t
    s"  second" s-test s-buf-cat
    s-test s-buf>string cr type .t
    bl s-test s-buf-char-cat
    [char] a s-test s-buf-char-cat
    [char] b s-test s-buf-char-cat
    [char] c s-test s-buf-char-cat
    s-test s-buf>string cr type .t
    s-test s-buf-close cr ." closed:" cr .t
    cr .s cr cr ;

    t bye
[then]
