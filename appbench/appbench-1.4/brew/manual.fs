\ manual.fs
\ 	$Id: manual.fs,v 1.12 2002/11/15 23:22:26 f Exp $	

\ Display manual sections related to the current context.
\ Uses info or html format depending on 'manual-type'.

\ ****************************************************************
\ dependencies:

s" stringbuf-0.4.fs" REQUIRED
s" display.fs" REQUIRED

\ ****************************************************************

decimal


32 STRINGBUF-HANDLE: (help-node)
: help-node! ( addr count -- )   (help-node) string! ;
: help-node@ ( -- addr count )   (help-node) string@ ;

\ Word to compile the context node:
: help-node" ( "node name"  -- )
    [char] " parse POSTPONE sliteral  POSTPONE help-node! ; IMMEDIATE


\ Switch between formats:
LIST: docu-types
docu-types
0
listed-ENUM: info-as-manual	\ manual type identifiers
listed-ENUM: html-as-manual
2drop

VARIABLE manual-type		\ info or html manual type switch
info-as-manual manual-type !	\ info is default

\ Words to use info as manual reader:
[UNDEFINED] call-info-string [IF]
    \ The string used to call info brew:
    : call-info-string ( -- addr count )   s" info --file=texi/brew.info" ;
[THEN]

[UNDEFINED] info-node-string-prefix [IF]
: info-node-string-prefix ( -- addr count )   s" '--node=" ;
[THEN]
[UNDEFINED] info-node-string-ending [IF]
: info-node-string-ending ( -- addr count )   s" '" ;
[THEN]

: cat-info-node-string ( handle -- )
    >r
    bl				r@ char-cat
    info-node-string-prefix	r@ cat
    help-node@			r@ cat
    info-node-string-ending	r> cat ;

: see-info-node ( -- error-flag )
    [ decimal ] 128 stringbuf-open >r
    call-info-string  r@ string!
    r@ cat-info-node-string
    r@ string@ <system>
    r> stringbuf-close ;


\ Words to use a html browser as manual reader:
[UNDEFINED] call-browser-string [IF]
    : call-browser-string ( -- addr count )   s" lynx texi/brew.html" ;
[THEN]

[UNDEFINED] (scratch-buf) [IF]
    decimal 128 STRINGBUF-HANDLE: (scratch-buf)
[THEN]

: node-as-html ( -- addr count )
    (scratch-buf)
    s" #" third string!
    help-node@ over + swap ?DO
	i c@
	dup bl = IF
	    drop
	    s" %20" third cat
	ELSE
	    over char-cat
	THEN
    LOOP
    string@ ;

: html-browse-node ( -- error-flag )
    [ decimal ] 128 stringbuf-open >r
    call-browser-string		r@ string!
    node-as-html		r@ cat
    r@ string@ <system>
    r> stringbuf-close ;

DEFER <.docu-reader>
: context-help ( -- )
    manual-type @ CASE
	info-as-manual OF  see-info-node     ENDOF
	html-as-manual OF  html-browse-node  ENDOF
    ENDCASE

    IF
	bell
	<.docu-reader>
	cr s" Something went wrong calling 'context-help'." type-alert cr
	key drop
    ELSE page THEN ;	\ brew might not clear a part of the screen sometimes.

: manual ( -- )   help-node" Top"  context-help ;
