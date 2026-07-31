\ reporting.fs
\ 	$Id: reporting.fs,v 1.49 2002/11/10 12:47:12 f Exp $	

\ reporting means writing a log file to the hd
\ it is more meant as a debugging help than a regular feature, as these
\ files grow enourmously big...
decimal

\ compile time switches:
[UNDEFINED] flush-files [IF]
    false CONSTANT flush-files			\ flush files after writing?
[THEN]

[UNDEFINED] log-mask [IF]	VARIABLE log-mask	log-mask off	[THEN]


VARIABLE (log-file-id)	(log-file-id) off

\ buffer for the name of the log file:
128 STRINGBUF-HANDLE: (log-file-name)
s" brew.log" (log-file-name) cat

: ?open-log-file ( -- )
    (log-file-id) @ IF EXIT THEN			\ log file opened?

    file-names-length# stringbuf-open >r
    (log-file-name) buffer-data-addr c@ [char] / <> IF	\ UNIX specific...
	log-dir			r@ cat
    THEN
    (log-file-name) string@	r@ cat
    r@ string@ w/o CREATE-FILE+
    IF  drop bell			\ error opening log file
	<other-colour>
	cr ." ?open-log-file: Error creating log-file "
	r@ string@ type
	cr ." proceeding without logging" cr
	reset-colours
	2000 wait-until
	log-mask off
	2drop
    ELSE (log-file-id) !		\ log file ok
    THEN
    r> stringbuf-close ;

: log ( c-addr u mask -- )			\ string and mask as arguments
    dup 0= swap					\ mask of null: emergency
    log-mask @ and or IF			\ logging active?
	?open-log-file
	(log-file-id) @ write-line
	IF  bell
	    cr ." log: Error writing to "
	    (log-file-name) string@ type
	    cr ." proceeding without logging" cr
	    key drop
	    log-mask off
[ flush-files ] [IF]
	ELSE
	    (log-file-id) @ flush-file drop
[THEN]
	THEN

    ELSE 2drop					\ no logging desired
    THEN ;

: log-string-and-number ( c-addr u n mask -- ) \ string and value logging
    >r
    num>string 2swap		( c-addr2 u2 c-addr u )
    dup >r pad swap move	( c-addr2 u2  R: u )
    tuck pad r@ + swap move	( u2  R: u )
    r> + pad swap		( pad u3 )
    r> log ;

80 STRINGBUF-HANDLE: (log)
: cat-log ( addr count -- )   (log) cat ;
: log-number ( n -- )     num>string cat-log ;
: forget-log ( -- )   (log) stringbuf-empty ;
: log-out-line ( mask -- )
    (log) string@ rot log
    forget-log ;

: log-it ( addr count mask -- )
    -rot cat-log			\ cat last string
    log-out-line ;			\ log and forget

: log-variable ( xt -- )	\ check log mask on a higher level
    dup xt>string		cat-log
    s" : "			cat-log
    EXECUTE @ num>string	0 log-it ;

: log-scale ( xt -- )	\ check log mask on a higher level
    dup xt>string		cat-log
    s" : "			cat-log
    EXECUTE 2@
    swap num>string		cat-log
    s" /"			cat-log
    num>string			0 log-it ;

: log-bitmask ( mask -- )   base @ >r 2 base !  num>string cat-log  r> base ! ;

\ if a (possibly not empty) buffer is handled to a word, which adds
\ something to the end of the string, this words help logging the part
\ that was added.
\ This works only, if the buffer never shrinks, and it's not reentrant...
\ In this case a stacked variable could make sense...
VARIABLE (log-offset)	\
: offset>log ( addr count -- addr' count' )
    >r (log-offset) @ + r> (log-offset) @ - ;

LIST: log-masks
log-masks
0
LISTED-MASK: log-spot
LISTED-MASK: log-birth
LISTED-MASK: log-death
LISTED-MASK: log-organs
LISTED-MASK: log-meal
LISTED-MASK: log-costs
LISTED-MASK: log-trial
LISTED-MASK: log-movement
LISTED-MASK: log-emergency
LISTED-MASK: log-pop-control
LISTED-MASK: log-step
LISTED-MASK: log-spot-vars
LISTED-MASK: log-random
LISTED-MASK: log-user		\ log parameters changed by the user

LISTED-MASK: log-mutation	\ log mutations 
LISTED-MASK: log-m-type		\ log mutation type
LISTED-MASK: log-m-some		\ log some basic mutation info

\ additional log entries are only possible if 'log-mask' is set at compile time
log-mask @ [IF] \ compile time switch
    LISTED-MASK: log-m-more	\ log more mutation info, why was it done so?
    LISTED-MASK: log-m-much	\ log a lot of inner mutation working

    log-mask @ 0> [IF]
	LISTED-MASK: log-m-extra	\ log extra information
    [ELSE] 1+ [THEN]			\ only occasionally supported.

    LISTED-MASK: log-empty-spots
[THEN]
2drop

' log-mask log-masks compile-listed-?-and-!

\ ****************************************************************

\ words to create a readable output of gene code:
VARIABLE code-file-mask			\ what to include in the code file
VARIABLE (code-file-id)		(code-file-id) off

\ buffer for the name of the code file:
16 STRINGBUF-HANDLE: (code-file-name)
s" gene-code.fs" (code-file-name) cat		\ default

1024 STRINGBUF-HANDLE: (code-file-buffer)
\ cat a string to the code-file buffer without checking the mask
: cat>code-file ( addr count -- )   (code-file-buffer) cat ;
: char-cat>code-file ( char -- )   (code-file-buffer) char-cat ;

LIST: code-file-masks
code-file-masks
0			\ bit masks what to include in code files
LISTED-MASK: write-code-file	\ on/off switch
LISTED-MASK: file-mutating
LISTED-MASK: file-end-trial
LISTED-MASK: file-code
LISTED-MASK: file-stack
\ LISTED-MASK: file-scoring	\ unused in brew-0.1.0
LISTED-MASK: file-mutation-type
LISTED-MASK: file-structure
LISTED-MASK: file-frames
LISTED-MASK: file-depth&cost
LISTED-MASK: file-step&spot&id
LISTED-MASK: file-item-number
2drop

file-code file-stack or code-file-mask !

\ check the mask and cat accordingly
: >code-file ( addr count mask -- )
    code-file-mask @ AND IF		\ want to write it?
	cat>code-file
    ELSE 2drop THEN ;

: clear-code-file-buffer ( -- )
    (code-file-buffer) stringbuf-empty ;

: code-file-write-line ( -- )
    (code-file-id) @ 0= IF			\ code file opened?
	file-names-length# stringbuf-open >r
	(code-file-name) buffer-data-addr c@ [char] / <> IF	\ UNIX
	    FORTH-dir			r@ cat
	THEN
	(code-file-name) string@	r@ cat
	r@ string@ w/o CREATE-FILE+
	IF  drop bell			\ error opening file
	    cr ." Error opening gene-code file "
	    r@ string@ type cr
	    ." Proceeding without creating that file." cr
	    key drop
	    code-file-mask off
	    r> stringbuf-close
	    2drop EXIT
	ELSE (code-file-id) !		\ code file ok
	THEN
	r> stringbuf-close
    THEN

    (code-file-buffer) string@  (code-file-id) @  write-line
    IF  bell
	cr ." Error writing to "
	(code-file-name) string@ type
	cr ." Proceeding without writing to the file." cr
	key drop
	code-file-mask off
	[ flush-files ] [IF]
    ELSE    (code-file-id) @ flush-file drop
        [THEN]
    THEN

    clear-code-file-buffer ;

: ?code-file-write-line ( mask -- )
    code-file-mask @ AND IF			\ want to write it?
	code-file-write-line
\   ELSE
\	clear-code-file-buffer			\ we might need this?
    THEN ;

2 CONSTANT code-indent#
VARIABLE (code-indent)
: indent-code ( -- )
    (code-indent) @  code-indent# *  0 ?DO s"  " cat>code-file LOOP ;

\ puts 1 or 2 tabs in the (code-file-buffer) to arrive at a (double) tab stop
: code-next-2-tab ( -- )
    (code-file-buffer) >r
    r@ buffered-length
    dup c-l 12 - < IF		 	\ not too far to the right?
	16 mod			 		\ I assume tab width = 8
	8 < IF s" 	" r@ cat THEN		\ two tabs?
	s" 	" r> cat
    ELSE drop				\ near end of line:
	bl r> char-cat				\ only a space
    THEN ;
