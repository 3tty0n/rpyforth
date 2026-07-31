\ advanced.fs
\ 	$Id: advanced.fs,v 1.7 2005/06/02 15:14:05 f Exp $	

\ Basics (that could not yet be defined in 'basics.fs'):

\ ****************************************************************
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" stringbuf-0.4.fs" REQUIRED

[UNDEFINED] .bs  [IF]	: .bs	8 emit ;	[THEN]	\ prints a backspace

\ ****************************************************************


\ Get next word from input buffer skipping leading white space,
\ save it in a stringbuf, restore input and return handle.
\ Don't forget to close the string buffer.
: get-name ( "name" -- "name"  handle )	\ stringbuf must be closed.
    save-input
    parse-word
    dup stringbuf-open >r r@ cat
    restore-input  ABORT" get-name: Error restoring input source."
    r> ;


\ Search a word given as a string in the current search order and return xt:
\ (no checks)
: get-xt ( addr count -- xt )
    [ decimal ] 32 stringbuf-open >r
    s" ' " r@ cat  r@ cat  r@ string@ EVALUATE
    r> stringbuf-close ;


\ ACCEPT user input (up to 80 chars) to a given stringbuf:
: (accept>stringbuf) ( handle max-count -- )
    >r
    pad dup r> ACCEPT
    ?dup IF
	rot dup stringbuf-empty cat
    ELSE 2drop THEN ;

: accept>stringbuf ( handle -- )
    ." new:            " .bs .bs .bs .bs .bs .bs .bs .bs .bs .bs .bs
    [ decimal ] 80 (accept>stringbuf) ;
