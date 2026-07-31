\ fileselect.fs
\ 	$Id: fileselect.fs,v 1.27 2005/03/31 15:25:17 f Exp $	

\ Simple file selection menu
\ This version sorts files alphabetically, directories first.

\ ****************************************************************
\ dependencies:

s" sorted-string-lists.fs" REQUIRED
s" simple-stringbuf.fs" REQUIRED
s" menu.fs" REQUIRED

\ ****************************************************************
\ LICENSE:

\ fileselect.fs
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

decimal


[UNDEFINED] file-names-length#	[IF] 256 CONSTANT file-names-length#	[THEN]


VARIABLE (current-directory)
file-names-length# S-BUF: (current-directory-name)

: normalise-current-dir-name ( -- )	\ works on (current-directory-name)
    (current-directory-name) s-buf>string
    dup 0= IF 2drop EXIT THEN		\ leave empty strings alone

    \ append a '/' on non empty names, if it's not there already
    + 1- c@ [char] / <> IF
	[char] / (current-directory-name) s-buf-char-cat
    THEN

    \ don't include starting './' in (current-directory-name)
    (current-directory-name) s-buf>string 2 min s" ./" compare 0= IF
	(current-directory-name) s-buf>string 2 - >r 2 + r@
	(current-directory-name) s-string!
	r> 0= IF EXIT THEN
    THEN

    \ don't include '../' in (current-directory-name)
    (current-directory-name) s-buf>string
    s" ../"  search IF
	1+ >r (current-directory-name) dup @ r> - 0 max swap !
	drop
	(current-directory-name) s-buf>string
	[char] / char-search-backwards IF
	    1+ (current-directory-name) !
	ELSE
	    (current-directory-name) s-buf-clear EXIT
	THEN
    ELSE 2drop THEN

    (current-directory-name) s-buf>string
    s" ./" search IF
	1+ >r (current-directory-name) dup @ r> - 0 max swap !
	drop
    ELSE 2drop THEN ;

\ Open a directory, set (current-directory) and (current-directory-name):
: open-directory ( addr count -- flag )
    dup 0= IF
	2drop
	s" ./"
    THEN
    2dup open-dir	( c_addr u wdirid wior )
    0= >r		( c_addr u wdirid   r: flag )
    r@ IF
	(current-directory) !
	(current-directory-name) s-string!
	normalise-current-dir-name
    ELSE
	drop 2drop
    THEN
    r> ;

file-names-length# S-BUF: (next-filename)

: read-dir-next ( -- addr count true | false )
    file-names-length# stringbuf-open >r
    r@ string@ drop file-names-length#  (current-directory) @
    read-dir
    ABORT" read-dir-next: Couldn't read-dir."
    IF
	r@ string-size!
	(next-filename) >r	r@ s-buf-clear
	(current-directory-name) s-buf>string r> s-buf-cat
	r@ string@
	(next-filename)	s-buf-cat
	(next-filename) s-buf>string
	TRUE
    ELSE
	drop FALSE
    THEN
    r> stringbuf-close ;

\ Close current directory if it was opened.  Else stay quiet.
: close-current-dir ( -- )  (current-directory) @ close-dir drop ;

: open-current-dir ( -- )
    (current-directory-name) s-buf>string open-directory
    0= ABORT" open-current-dir: Couldn't open-dir." ;

\ Test a file name string for being a directory:
: is-directory? ( addr count -- flag )
    dup 0= IF 2drop TRUE THEN		\ empty string is seen as './'

    open-dir 0=
    dup IF
	swap close-dir
	ABORT" is-directory?: Couldn't close-dir."
    ELSE
	nip
    THEN ;

list-user-flag#
MASK: is-directory
drop

: set-directory-flag ( node -- )
    >node-descriptor >flags dup @ is-directory or swap ! ;

: node-is-directory? ( node -- flag-not-normalised )
    >node-descriptor >flags @ is-directory and ;

\ Build a sorted string-list of files in the current directory,
\ directories first:
: build-current-directory-list ( -- string-list )
    close-current-dir
    open-current-dir

    \ build sorted directories list and sorted file list
    1 deflist
    1 deflist
    BEGIN			( directories-list files-list )
	read-dir-next
    WHILE
	2dup is-directory? IF	( directories-list files-list addr count )
	    fourth
	ELSE
	    third
	THEN
	insert-string-sorted
    REPEAT			( directories-list files-list )

    \ set is-directory mask in directories list:
    over
    dup nodes 0 ?DO
	next-node
	dup set-directory-flag
    LOOP
    drop

    \ append files to directories list
    dup
    dup nodes 0 ?DO		( directories-list files-list current-node )
	next-node
	dup @ fourth >list
    LOOP
    drop
    remove-list ;

MENU: fileselect-men
0 VALUE files-list

: open-listed-directory ( n -- )
    files-list n'th-string@ 2dup is-directory?
    0= ABORT" open-listed-directory: File isn't a directory."
    open-directory
    0= ABORT" open-listed-directory: Couldn't open-directory"
    menu-scrolled off ;

: filename-1st-char ( addr count -- char )
    dup 0= ABORT" filename-1st-char: Zero length string."

    \ remove trailing '/':
    2dup  [char] /  -trailing-char
    dup 0= ABORT" filename-1st-char: Zero length string left."

    [char] /  char-search-backwards IF
	nip 1+
	+ c@
    ELSE
	drop c@
    THEN ;

: scroll-to-line ( n -- )   menu-scrolled !  0 2 menu-2-at 2! ;

: .fileselect-menu ( -- )
    page

    files-list remove-string-list
    build-current-directory-list to files-list

    title-colors (title) 2@ noop-entry
    ."     files: " files-list nodes . up-to-here
    end-title

    4 keep-but-scroll-rest

    cr
    0	\ current first char of file base-name
    files-list nodes 0 scrolled-range ?DO
	i files-list n'th-string@
	i >stack
	i files-list n'th-node node-is-directory? IF
	    ['] open-listed-directory	redisplay	menu-entry
	    [char] /  emit		up-to-here
	ELSE
	    files-list >stack-2	 ['] n'th-string@   do-after	menu-entry
	THEN
	cr

	\ key entry if first char of file base-name comes first time:
	i files-list n'th-string@ filename-1st-char
	2dup <> IF
	    nip
	    dup	  redisplay   i >stack	 ['] scroll-to-line    #key-menu-entry 
	ELSE drop THEN
    LOOP
    drop

    cr
    <common-menu-entries>

    \ override 'q' to return false on exit: 
    s" q"	menu-done	['] false	menu-key-entry ;

: fileselect-menu ( addr-title count-title -- addr count true | false)
    (title) 2!

    1 deflist to files-list

    fileselect-men
    menu-scrolled off
    ['] .fileselect-menu menu-display-xt !
    ['] true to-do-after-xt !
    menu-done	['] false	menu-key-default
    menu-done	['] false	menu-default
    do-menu-loop
    free-menus

    files-list remove-string-list
    0 to files-list ;

false [IF] \ testing a bit
: t
    s" OUTPUT/tmp" open-directory IF cr
	s" Select a file:" fileselect-menu
	page
	IF   ." selected: " type  ELSE  ." no selection."  THEN cr
	." current-directory-name: "
	(current-directory-name) s-buf>string ." ->|" type ." |<-" cr
	2000 ms
	close-current-dir
    ELSE 7 emit THEN ; t bye
[THEN]
