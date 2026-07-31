\ stringbuf-0.4.fs

: stringbuf-version ( -- addr count )
    cvs" 	$Id: stringbuf-0.4.fs,v 1.16 2003/08/27 18:14:19 f Exp $	" ;

\ String package for concatenating strings.

\ Concatenating two strings can lead to the need for a new (bigger)
\ buffer for the result.  So it's not convenient to refer to the
\ result string through the string address, because this might change.

\ While 'stringbuf-0.3.fs' used handles to refer to buffers this version
\ passes a allocation pointer address, which contents might be changed
\ during operation.

\ Get a temporary pointer by saying 'stringbuf-open ( size -- handled-pointer)'
\ or define a named stringbuf handle, automtically allocating on first usage
\ with 'STRINGBUF-HANDLE: ( "name" initial-size -- )'

\ While allocation address and size are stored at the allocation pointer,
\ string descriptor and the string itself are in the allocated memory.
\ Strings are stored just after their descriptor.


\ The allocation pointer adress is like a handle for string usage.
\ Its content is the address of the string descriptor or false.


\ ****************************************************************
\ LICENSE:

\ stringbuf-0.4.fs
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


\ String buffer memory gets allocated through 'allocation-pointers.fs'
\ 'stringbuf-open ( size -- handled-pointer)' uses 'open-allocate'
\ see 'allocation-pointers.fs'.
s" allocation-pointers.fs" REQUIRED

\ ****************************************************************


\ Defining string buffer descriptor:
\ This is the start of a allocated string buffer.
\ You can do '2@' at this address to get the string.
\ Don't do '2!'...
0
OFFSET: >string-count				\ length of the buffered string
OFFSET: >string-address				\ pointer to the string
OFFSET: >string-usable-size			\ usable size of string buffer
cell+	\ unused padding (might speed up).
dup CONSTANT string-descriptor-length#
OFFSET: >string-start
drop

false [IF] \ debugging/testing helper words
    : .hex ( n -- )
	base dup @ 2>r
	hex .
	r> r> ! ;

    \ Print hex address and value
    : .a? ( addr -- )   dup .hex  9 emit  ? ;

    \ Print hex address and hex value
    : .a.a ( addr -- )   dup .hex  9 emit @ .hex ;

    \ Show descriptor addresses and values:
    : show ( addr-of-pointer -- )
	dup cr ." allocation pointer (adr/value):	" .a.a
	dup @ 0= IF
	    cr ." stringbuffer not opened. " .a.a cr
	    EXIT
	THEN
	cr ." allocated size:	(addr/value):	" dup pointer>size .a?

	@ >r
	cr ." string count	(addr/value):	" r@ >string-count .a?
	cr ." string address	(addr/value):	" r@ >string-address .a.a
	cr ." usable string size (adr/val):	" r> >string-usable-size .a?
	cr ;
[THEN]

\ Init stringbuf descriptor, without changing buffered length:
: (stringbuf-descriptor-init) ( usable-buffer-size allocated-address -- )
    >r
    r@ >string-usable-size !				\ maximal string size
    r@ >string-start  r> >string-address  ! ;		\ string address

\ Init stringbuf descriptor, setting buffered length to zero:
: stringbuf-descriptor-init ( usable-buffer-size allocated-address -- )
    dup >string-count off				\ empty
    (stringbuf-descriptor-init) ;			\ init

\ Low level working on the pointer only.
: allocate-string-buffer ( usable-size handled-pointer -- )
    >r
    dup string-descriptor-length# + r@ allocate-to-pointer
    r> @ stringbuf-descriptor-init ;

\ Low level working on the pointer only.
: free-string-buffer ( handled-pointer -- )	\ same as 'free-allocated'
    dup @ free
    ABORT" free-string-buffer: Couldn't free"	\ just another error message.
    off ;

\ Allocate a buffer and return a pointer to be used as a handle:
: stringbuf-open ( size -- handled-pointer )
    dup string-descriptor-length# + open-allocate >r
    r@ @ stringbuf-descriptor-init
    r> ;

: stringbuf-close ( address-of-handled-pointer -- )  close-allocated ;


\ Double usable size of the buffer handled:
: double-stringbuf ( handled-pointer -- )
    >r

    \ Compute new usable size:
    r@ @ >string-usable-size @ 2*	( new-usable-size   r: pointer )
    cell max				\ not less then a cell
    dup string-descriptor-length# +	( usable real-size  r: pointer )
    r@ resize-allocated			( usable  r: pointer )
    r> @ (stringbuf-descriptor-init) ;


\ Empty string:
: stringbuf-empty ( handled-pointer -- )  @ >string-count off ;

\ Append the string 'addr count' to the handled buffer, resize if needed:
: cat ( addr count handled-pointer -- )
    >r				( addr count  r: handled-pointer )

    \ Test size, increase if needed.
    r@ @ >string-count @ over +	( addr count new-count  r: pointer )
    BEGIN
	dup  r@ @ >string-usable-size @ >
    WHILE
	r@ double-stringbuf	( addr count new-count new-addr r: pointer )
    REPEAT			( addr count new-count  r: pointer )

    r> @ >r			( addr count new-count  r: buffer-address )
    r@ >string-count @ swap r@ >string-count !		\ store new count
				( addr count old-count  r: ptr buffer-address )
    r> >string-address @ + swap move ;			\ move string

: char-cat ( c handled-pointer -- )
    >r  (scratch) >r
    r@ c!
    r> 1 r> cat ;

: cat-n ( handled-pointer n -- )   (scratch) !  >r (scratch) cell r> cat ;

: string! ( addr count handled-pointer -- )  dup stringbuf-empty  cat ;

\ Put string in a new stringbuffer and return the handle:
: string!! ( addr count -- handled-pointer )
    dup stringbuf-open >r  r@ cat  r> ;

: string@ ( handled-pointer -- addr count )   @ >string-count 2@ ;

: buffer-data-addr ( handled-pointer -- addr )  @ >string-start ;

: buffered-length ( handled-pointer -- count )   @ >string-count @ ;

\ Compare two strings given as handles:
: string-compare ( handle-1 handle-2 -- -1 | 0 | +1 )
    >r string@  r> string@  compare ;

\ Caution: use only if there's space!
: string-size! ( size handle -- )   @ >string-count ! ;

\ Named string pointers automatically allocating when used first time:
: STRINGBUF-HANDLE: ( "name" initial-size -- )
    CREATE
	alloc-ptr-items# 0 DO
	    0 ,
	LOOP
	,					\ initial size
    DOES> ( -- handled-pointer )
	[ alloc-ptr-items# 1- cells ] literal +
	dup @ IF EXIT THEN			\ initialised: done
	dup cell+ @ over allocate-string-buffer	\ allocate buffer
    ;

false [IF] \ debugging/testing
    page .( Testing stringbuf-0.4.fs )
    cr

    4 stringbuf-open
    dup show
    s" Testing handled buffers." third string!
    dup string@ cr type
    dup show
    s"   Cat some more text..." third cat
    dup string@ cr type
    dup show KEY DROP
    cr .( after 'double-stringbuf' )
    dup double-stringbuf
    dup string@ cr type
    dup show
    stringbuf-close KEY DROP

    page
    cr .( Testing named buffers. )
    8 STRINGBUF-HANDLE: pointer
    pointer show

    s" hallo!" pointer cat
    pointer string@ cr type
    pointer show

    s"  here I am..." pointer cat
    pointer string@ cr type
    pointer show KEY DROP

    s" new string!" pointer string!
    pointer string@ cr type
    pointer show
    pointer free-string-buffer

    4 STRINGBUF-HANDLE: named-pointer
    named-pointer drop
    s" String stored in a named pointer" named-pointer string!
    named-pointer string@ cr type
    named-pointer show
    cr .s KEY DROP
[THEN]
