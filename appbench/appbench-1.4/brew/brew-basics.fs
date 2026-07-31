\ brew-basics.fs
\ 	$Id: brew-basics.fs,v 1.114 2005/05/14 05:13:45 f Exp $	

\ doing some very basic definitions first makes life easier...

\ ****************************************************************
\ file dependencies:
\ *Inside* brew this dependencies are met anyway...

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" lists.fs" REQUIRED
s" listed-masks.fs" REQUIRED
s" compile-options.fs" REQUIRED
s" simple-stringbuf.fs" REQUIRED
s" advanced.fs" REQUIRED
s" display.fs" REQUIRED

\ ****************************************************************
\ Compile options:
\ 	preset-run-mode
\       color-offset

\ ****************************************************************

decimal


VARIABLE run-mode	run-mode off		\ mode like recording and such
[DEFINED] preset-run-mode [IF]
    preset-run-mode run-mode !
[THEN]

LIST: run-mode-masks
run-mode-masks
0
LISTED-MASK: mutating
LISTED-MASK: no-code-cost	\ for 'gene-follow-mute'
LISTED-MASK: writing-code	\ writing code *only*
LISTED-MASK: recording
LISTED-MASK: write-diff	\ writing diff files (for recording or logging)
LISTED-MASK: playback
LISTED-MASK: linear-mode	\ should become world local
LISTED-MASK: elitism		\ should become world local
LISTED-MASK: making-bench
LISTED-MASK: playing-bench
LISTED-MASK: store-genomes	\ put compiled genomes and usage in a list
LISTED-MASK: compiled		\ mutated genome is already compiled (rebirth)
LISTED-MASK: redisplaying
LISTED-MASK: defining-rectangle	\ defining a spot rectangle
LISTED-MASK: defining-bar-range	\ defining a column bar range
2drop

' run-mode run-mode-masks compile-listed-?-and-!

: NOT-recording? ( -- flag )  run-mode @ recording and 0= ;

: world-mode? ( -- flag )   run-mode @ linear-mode and  0= ;

: elitism-off ( -- )  run-mode dup @  [ elitism invert ] literal and swap ! ;

\ do something not recording it
: not-recording ( xt -- )
    run-mode dup @
    dup >r			\ preserve run-mode
    recording invert and swap !
    EXECUTE
    r> run-mode ! ;

64 CONSTANT max-stack-effect	\ no genom should ever go beyond that
max-stack-effect cell mod [IF]
    cr cr .( Compile time error: 'max-stack-effect CELL MOD' must be zero! ) cr
    QUIT
[THEN]

DEFER <food> ( -- addr )		\ gives addr where the food is

\ dummy function to indicate missing selection in some xt lists
: (none) ;


\ I/O stuff:

\ Display two numbers separated by a '/':
: ./. ( n2 n1 -- )   swap .  .bs  [char] / emit  . ;

VARIABLE display-switch			\ switches at run time

\ If you define (uncomment) 'never-use-colors' in 'compile-options.fs'
\ there will be no colors used. (Most color stuff will still get compiled
\ in though for the sake of simplicity.)

\ bit masks for 'display-switch':
LIST: display-switch-masks
display-switch-masks
0
LISTED-MASK: spot-display-on		\ spot display on
LISTED-MASK: spot-foreground-coloring	\ spot foreground color on
LISTED-MASK: spot-background-coloring	\ spot background color on
LISTED-MASK: step-display-on		\ step statistic info, nuc/world scans
LISTED-MASK: step-foreground-coloring	\ spot foreground color on
LISTED-MASK: step-background-coloring	\ spot background color on
\ snapshot bits only have an influence if both ordinary display types are off:
LISTED-MASK: step-snapshots		\ switches step snapshots on
LISTED-MASK: spot-snapshots		\ switches spot snapshots on
LISTED-MASK: continuous-display-used	\ does *not* switch continuous display
LISTED-MASK: scan-display-used		\ does *not* switch scan display
LISTED-MASK: text-display-used		\ does *not* switch text display
2drop

' display-switch display-switch-masks compile-listed-?-and-!

spot-display-on step-snapshots or display-switch !

\ these switches work on *spot* display: (currently)
: toggle-foreground-colorizing ( -- )
    display-switch dup @ spot-foreground-coloring xor swap ! ;
: toggle-background-colorizing ( -- )
    display-switch dup @ spot-background-coloring xor swap ! ;

: toggle-colorizing ( -- )
    display-switch dup @		( switch-addr switch-bitmask )

    dup spot-display-on and IF		\ spot display on
	[ spot-foreground-coloring spot-background-coloring + ] literal >r
	dup r@ and r@ = IF			\ full colored spot display?
	    spot-foreground-coloring xor	\ colors off
	    spot-background-coloring xor
	ELSE r@ or THEN rdrop			\ colors on
    THEN

    dup step-display-on and IF		\ step display on
    THEN

    swap ! ;

\ Transform any given number in an allowed color code.
[UNDEFINED] 2-color [IF] \ could be defined system dependent.

\ Assuming continuous range from zero (or 'color-offset') upwards.
\ compile option: 'color-offset' not used, never tested.
\ could give problems in other places (data visualization).
: 2-color ( col -- col' )
    colors mod
    dup 0< IF colors + THEN

    [DEFINED] color-offset [IF]	\ color-offset is the lowest color code
	[ color-offset ] literal +  \ for systems where colors don't start at 0
    [THEN]
;
[THEN]

VARIABLE background-color-xt	' black background-color-xt !
: <background-color> ( -- col )		background-color-xt @ EXECUTE ;

VARIABLE foreground-color-xt	' white foreground-color-xt !
: <foreground-color> ( -- col )		foreground-color-xt @ EXECUTE ;

: set-colors ( -- )	\ for spot display
    spot-display-on? IF
	spot-foreground-coloring? IF
	    <foreground-color> color-foreground
	THEN
	spot-background-coloring? IF
	    <background-color> color-background
	THEN
    THEN ;

LIST: x>bg-color		\ words that set color depending on something
LIST: x>fg-color		\ words that set color depending on something

\ first the trivial cases:
' black		dup x>bg-color >list	x>fg-color >list
' red		dup x>bg-color >list	x>fg-color >list
' green		dup x>bg-color >list	x>fg-color >list
' brown		dup x>bg-color >list	x>fg-color >list
' blue		dup x>bg-color >list	x>fg-color >list
' magenta	dup x>bg-color >list	x>fg-color >list
' cyan		dup x>bg-color >list	x>fg-color >list
' white		dup x>bg-color >list	x>fg-color >list
' default-color	dup x>bg-color >list	x>fg-color >list

[DEFINED] default-foreground [IF]
    ' default-foreground x>fg-color >list
[THEN]
[DEFINED] default-background [IF]
    ' default-background x>bg-color >list
[THEN]

: paint-background ( -- )
    spot-background-coloring? IF
	<background-color> color-background
    THEN ;

: set-default-colors ( -- )	\ same, but checks if needed
    display-switch @
    dup spot-foreground-coloring and IF default-foreground THEN
	spot-background-coloring and IF default-background THEN ;



\ Messages:
    
c-l STRINGBUF-HANDLE: (message)		\ messages to be desplayed in info line
VARIABLE message-count			\ how many steps to display message
: >message ( addr count n -- )
    message-count !
    (message) string! ;

VARIABLE message-fg-color-xt	message-fg-color-xt off
VARIABLE message-bg-color-xt	message-bg-color-xt off

VARIABLE (manually-selected-cell)	(manually-selected-cell) off

\ brew will look for inputs relative to input-dir, if the user does not
\ select another directory. Make the string end with a directory separator.
[UNDEFINED] inputs-dir [IF]
    : inputs-dir ( -- addr count )   s" INPUTS/" ;
[THEN]

\ word to put input-dir in front of a subdirectory name:
\ attention: there is only *one* buffer used, don't use multiple strings.
file-names-length# S-BUF: (in-sub-dir)
: inputs-sub-dir ( addr count -- addr' count' )
    (in-sub-dir) >r
    r@ s-buf-clear
    inputs-dir r@ s-buf-cat
    ( addr count ) r@ s-buf-cat
    r> s-buf>string ;

: genes-dir ( -- addr count )	s" genes/" inputs-sub-dir ;

: experiments-dir ( -- addr count )   s" experiments/" inputs-sub-dir ;

: individuals-dir ( -- addr count )   s" individuals/" inputs-sub-dir ;

\ brew will put all it's output relative to out-dir, if the user does not
\ select another directory. Make the string end with a directory separator.
[UNDEFINED] out-dir [IF]   : out-dir ( -- addr count )   s" OUTPUT/" ;	[THEN]

\ word to put out-dir in front of a subdirectory name:
\ attention: there is only *one* buffer used, don't use multiple strings.
file-names-length# S-BUF: (out-sub-dir)
: out-sub-dir ( addr count -- addr' count' )
    (out-sub-dir) >r
    r@ s-buf-clear
    out-dir r@ s-buf-cat
    ( addr count ) r@ s-buf-cat
    r> s-buf>string ;

: rec-play-dir ( -- addr count )	s" rec-play/"	out-sub-dir ;
: log-dir ( -- addr count )		s" log/"	out-sub-dir ;
: FORTH-dir ( -- addr count )		s" FORTH/"	out-sub-dir ;
: tmp-dir ( -- addr count )		s" tmp/"	out-sub-dir ;

\ cat *two* strings to a filename and handle a buffer.
\ Please close the buffer yourself.
: file-name-cat ( addr1 count1 addr2 count2 -- handle )
    file-names-length# stringbuf-open >r
    2swap r@ cat r@ cat
    r> ;

\ Change a file name in a buffer.
\ If the user changes it close the old file and clear the id variable.
\ Does *not* open or create the new one.
: change-handled-file ( addr-of-id-variable name-handle -- )
    dup string@				\ old name
    dup stringbuf-open >r  r@ cat	\ kept in a buffer

    dup accept>stringbuf	\ let the user change the name
    dup string@  r@ string@  compare IF \ changed?
	over @ dup IF			\ old file was opened?
	    close-file			\ close it
	    IF
		bell
		cr ." change-file: Couldn't close file "
		r@ string@ type cr
		key drop		\ could be something serious
	    THEN
	    over off			\ and set id to zero
	ELSE drop THEN
    THEN

    r> stringbuf-close
    2drop ;

\ Brew uses temorary files which could lead to problems if there is
\ more than one brew running. So I give each brew an identity which
\ is included in the file names.
VARIABLE (identity)
: define-brew-identity ( -- n )
    tmp-dir s" brew-identity" file-name-cat
    dup string@ ['] included CATCH IF  2drop -1 THEN
    1+
    dup (identity) !

    over string@ r/w CREATE-FILE IF
	bell 2drop
	cr ." Could not create file " over string@ type cr
	2000 ms
	99	\ dummy
    ELSE
	over num>string third WRITE-LINE drop
	CLOSE-FILE drop
    THEN
    swap stringbuf-close ;

: indentity-string ( -- addr count )
    [ 16 stringbuf-open
    char _ over char-cat
    define-brew-identity num>string  third cat   dup string@ ]
    sliteral
    [ stringbuf-close ] ;

VARIABLE (unique#)		-1 (unique#) !
\ Build a string based on the value of  (unique#)  and increase this value:
: unique-string ( -- addr count )	\ increases  (unique#)
    (unique#) >r
    r@ @ num>string
    1 r> +! ;

: unique-identity-string ( -- handle )	\ Please do close buffer
    [ decimal ] 16 stringbuf-open >r
    [char] _ r@ char-cat
    unique-string r@ cat
    indentity-string r@ cat
    r> ;

\ Keep track of temporary files for removal on exit:
VARIABLE (tmp-file-list-id)
VARIABLE tmp-files		tmp-files off	\ flag (counter)

\ tmp-files-list  will be created on first usage to avoid it's creation
\ on benchmarks.
TRUE [IF] \ Individual tmp file list files for each brew incarnation.
    : tmp-files-list ( -- handle )	\ please do close buffer.
	tmp-dir s" tmp-files" file-name-cat
	indentity-string third cat ;

    : ?create-tmp-file-list ( -- )
	tmp-files @ IF  EXIT  THEN

	tmp-files-list dup string@ w/o CREATE-FILE IF
	    drop bell
	    cr ." Could not create file " over string@ type clear-line-to-end
	    2000 ms
	ELSE
	    (tmp-file-list-id) !
	THEN
	stringbuf-close ;
	
[ELSE] \ only one tmp file list: one brew could remove the others tmp files...
    : tmp-files-list ( -- handle )	\ please do close buffer.
	tmp-dir s" tmp-files" file-name-cat ;

    : ?create-tmp-file-list ( -- )
	tmp-files @ IF  EXIT  THEN

	tmp-files-list dup string@ r/w OPEN-FILE  IF	\ try to use old file
	    drop
	    dup string@ r/w CREATE-FILE IF		\ re-create file
		bell drop
		cr ." Could not create file " over string@ type
		clear-line-to-end
		2000 ms
	    ELSE
		(tmp-file-list-id) !
	    THEN
	ELSE	\ no file exists
	    dup file-size drop third reposition-file drop
	    (tmp-file-list-id) !
	THEN
	stringbuf-close ;
[THEN] \ tmp file list separated or not for each brew

\ Put a file name into file  (tmp-file-list-id) :
: to-tmp-file-list ( addr count -- )
    ?create-tmp-file-list
    (tmp-file-list-id) @ write-line drop
    (tmp-file-list-id) @ flush-file drop
    1 tmp-files +! ;

\ Create a file, on success put the name into  (tmp-file-list-id)  for later
\ removal, and in  (last-tmp-file-name)  for reference:
\ The id is in  (last-tmp-file-id)
file-names-length# STRINGBUF-HANDLE: (last-tmp-file-name)
VARIABLE (last-tmp-file-id)	(last-tmp-file-id) off
: CREATE-tmp-FILE ( c-addr u wfam - wfileid wior )
    third third 2>r
    CREATE-FILE dup 0= IF
	2r@ to-tmp-file-list
	2r@ (last-tmp-file-name) string!
	over (last-tmp-file-id) !
    THEN
    2rdrop ;

: reopen-last-tmp-file ( -- wior )
\    (last-tmp-file-id) @ CLOSE-FILE drop ############
    (last-tmp-file-name) string@ r/w OPEN-FILE
    dup IF
	(last-tmp-file-id) off		\ programmers aesthetics...
    ELSE
	swap (last-tmp-file-id) !	\ on success set  (last-tmp-file-id)
    THEN ;

: ?remove-tmp-files ( -- )
    tmp-files @ 0= IF  EXIT  THEN

    (tmp-file-list-id) @ CLOSE-FILE drop
    tmp-files-list
    dup string@ r/o OPEN-FILE IF
	bell drop
	cr ." Could not open file " dup string@ type clear-line-to-end
	stringbuf-close
	2000 ms EXIT
    THEN
    cr ." Removing tmp files." clear-line-to-end
    file-line-max# allocate ABORT" remove-tmp-files: Could not allocate."
    >r
    BEGIN
	r@ file-line-max# third READ-LINE
	ABORT" remove-tmp-files: Could not read-line."
    WHILE
	r@ swap DELETE-FILE drop
    REPEAT
    drop
    close-file drop
    r> free ABORT" remove-tmp-files: Could not free."
    dup string@ DELETE-FILE drop	\ remove file list itself
    stringbuf-close ;

\ Keep trace of produced output files:
: created-files-list ( -- handle )	\ please do close buffer.
    tmp-dir s" created-files" file-name-cat
    indentity-string third cat ;

VARIABLE created-files		created-files off	\ counter
VARIABLE (created-files-list-ID)	(created-files-list-ID) off

\ Avoid  created-files-list  in benchmarks, don't create file until first use:
: ?create-created-files-list ( -- )	\ Created on first usage
    created-files @ IF  EXIT  THEN
    
    created-files-list dup string@ w/o CREATE-FILE IF
	bell
	cr ." Could not create file " over string@ type clear-line-to-end
	2000 ms
    ELSE
	(created-files-list-ID) !
	dup string@ (created-files-list-ID) @ WRITE-LINE drop
	(created-files-list-ID) @ FLUSH-FILE drop
    THEN stringbuf-close ;

: to-created-file-list ( addr count -- )
    ?create-created-files-list
    (created-files-list-ID) @ write-line drop
    (created-files-list-ID) @ flush-file drop
    1 created-files +! ;

\ Create a file and put it in  created-files-list , count in  created-files :
: CREATE-FILE+ ( addr count wfam - wfileid wior )
    third third 2>r
    CREATE-FILE
    dup 0= IF  2r@ to-created-file-list  THEN
    2rdrop ;

\ Open a file and put it in  created-files-list , count in  created-files :
: OPEN-FILE+ ( addr count wfam - wfileid wior )
    third third 2>r
    OPEN-FILE
    dup 0= IF  2r@ to-created-file-list  THEN
    2rdrop ;

\ Build name of a backup file and return buffer:
: backup-file-name ( addr count -- handle )	\ Please close buffer.
    string!!
    s" .bak" third cat ;

\ Create a backup file and put it in  created-files-list ,
\ count in  created-files :
: create-backup-file+ ( addr count -- )
    2dup backup-file-name >r
    r@ string@
    2dup to-tmp-file-list
    clone-file
    r> stringbuf-close ;

\ Compare two files and return a flag which is TRUE if there's a difference:
: compare-files ( addr1 count1 addr2 count2 -- diff-flag )
    2dup r/o OPEN-FILE IF
	bell
	cr ." compare-files: Could not open-file  " type
	true ABORT" compare-files: ABORTED."
    ELSE
	>r 2drop	( addr1 count1  r: file-id-2 )
    THEN
    2dup r/o OPEN-FILE IF
	bell
	cr ." compare-files: Could not open-file  " type
	true ABORT" compare-files: ABORTED."
    ELSE
	nip nip	( file-id-1  r: file-id-2 )
    THEN

    dup FILE-SIZE
    ABORT" compare-files: Could not determine file-size on first file."
    r@ FILE-SIZE
    ABORT" compare-files: Could not determine file-size on second file."
    2dup d>s >r
    d= IF			\ same size
	( file-id-1  r: file-id-2 file-size )
	r@ stringbuf-open
	dup buffer-data-addr	( f-id-1 handle buffer-addr-1  r: f-id-2 size )
	r@ fourth READ-FILE
	ABORT"  compare-files: Could not read-file #1."
	over buffer-data-addr swap	( id1 h1 addr1 count1  r: f-id-2 size )

	r@ stringbuf-open dup buffer-data-addr
	r> r@ READ-FILE
	ABORT"  compare-files: Could not read-file #2."
	over buffer-data-addr swap
	rot >r		( id1 h1 addr1 count1 a2 c2  r: f-id-2 h2 )
	compare >r
	stringbuf-close r> r> stringbuf-close	( f-id1 flag  r: f-id-2 )
	0<>
    ELSE  rdrop TRUE  THEN	\ size differs
    swap CLOSE-FILE
    ABORT" compare-files: Could not close-file #1."
    r> CLOSE-FILE
    ABORT" compare-files: Could not close-file #2." ;

\ Compare a file to it's backup file and return difference flag:
: compare-to-backup ( addr count -- flag )
    2dup backup-file-name >r
    r@ string@ compare-files
    r> stringbuf-close ;

\ Create a named file, abort on error.
\ Put the name in  created-files-list  and count in  created-files :
: create-named-file ( addr count wfam -- fid )
    third third 2>r
    CREATE-FILE+
    dup IF
	bell cr ." create-named-file: Error creating "
	2r@ type cr
	THROW
    THEN drop
    2rdrop ;

\ Open a named file, abort on error.
: open-named-file ( addr count wfam -- fid )
    third third 2>r
    OPEN-FILE
    dup IF
	bell cr ." open-named-file: Error opening "
	2r@ type cr
	THROW
    THEN drop
    2rdrop ;

: ?list-created-files ( -- )
    created-files @ 0= IF
	cr ." No output files produced.    "  clear-line-to-end
	cr clear-line-to-end	0 at-x
	EXIT
    THEN

    (created-files-list-ID) @ CLOSE-FILE drop
    created-files-list dup string@ r/w OPEN-FILE IF
	drop
	cr  ." File  " dup string@ type ."  not found." clear-line-to-end
	stringbuf-close
	EXIT
    THEN

    cr
    cr ." You created the following output files:"
    file-line-max# stringbuf-open >r  r@ buffer-data-addr >r
    BEGIN  ( name-handle created-files-list-ID  r: buffer-handle buffer-addr )
	r@ file-line-max# third READ-LINE
	IF
	    bell drop
	    stringbuf-close
	    rdrop
	    r> stringbuf-close EXIT
	THEN
    WHILE
	cr r@ swap type
    REPEAT
    drop

    cr
    cr ." File  "
    created-files-list dup string@ type stringbuf-close
    ."   is a list of your output files."
    cr ." Please remove them when you're done." cr

    CLOSE-FILE drop
    stringbuf-close
    rdrop  r> stringbuf-close ;


\ Defining menu-id's:
1
ENUM: main-sceen-id
ENUM: nuc-menu-id
drop


\ Translate a value from an enum list to a readable text string:
\ The list *must* consist of consecutive values in increasing order,
\ but needn't start at zero:
: listed-enum>string ( value-of-enum-constant list-addr -- addr count )
    >r
    0 r@ n'th-node @ EXECUTE -	\ first constant as offset, can start anywhere.
    r> n'th-node @ xt>string ;


\ Define variable types:
LIST: variable-types

variable-types 0
LISTED-ENUM: type-unknown%
LISTED-ENUM: type-int%
\ LISTED-ENUM: type-double%
LISTED-ENUM: type-df%
LISTED-ENUM: type-int-addr%	\ pointer to int
\ LISTED-ENUM: type-double-addr%	\ pointer to double int cell
LISTED-ENUM: type-df-addr%	\ pointer to dfloat
\ LISTED-ENUM: type-xt%		\ xt (general)
\ LISTED-ENUM: type-xt(-n)%	\ xt ( -- n )
\ LISTED-ENUM: type-xt(-a)%	\ xt ( -- a )
\ LISTED-ENUM: type-xt(-f)%	\ xt ( -- flag )
\ LISTED-ENUM: type-bitmap%
2drop

: var-type-string ( var-type-code - addr count )
    variable-types listed-enum>string ;

: is-int? ( type-code -- flag )
    CASE
	type-int-addr%	OF  TRUE  EXIT  ENDOF
	type-int%	OF  TRUE  EXIT  ENDOF
    ENDCASE
    FALSE ;

: is-dfloat? ( type-code -- flag )
    CASE
	type-df-addr%	OF  TRUE  EXIT  ENDOF
	type-df%	OF  TRUE  EXIT  ENDOF
    ENDCASE
    FALSE ;


\ Defining scan locality type's:
LIST: locality-types
locality-types
0
LISTED-ENUM: unknown-locality%
LISTED-ENUM: global-locality%
LISTED-ENUM: world-local%
LISTED-ENUM: spot-local%
LISTED-ENUM: nuc-local%
\ LISTED-ENUM: nuc-and-spot-local% \ inhabited spots and nucs
\ LISTED-ENUM: world-local%
2drop

: locality-string ( locality-code -- addr count )
    locality-types listed-enum>string ;


\ Defining THROW codes:
1
[UNDEFINED] |menu-input-error [IF]
    ENUM: |menu-input-error
[ELSE] 1+ [THEN]
ENUM: |genome-too-long
ENUM: |playback-quit
ENUM: |stack-symbols-mismatch
drop


\ Defining item masks for nuc and spot vars:
LIST: item-masks

\ Define bitmasks named |A |B |C ...
: define-item-masks ( u -- )
    >r
    item-masks 0
    r> 0 ?DO
	s" LISTED-MASK: |X"		\ the 'X' will be overwritten...
	2dup 1- +			\ address of the 'X'
	i [char] A +  swap c!
	EVALUATE
    LOOP
    2drop ;
decimal 32 define-item-masks

\ A word to use the old integer scales on floats:
: f*/ ( F: r  D: n1 n2 -- F: r*n1/n2 )   s>f f/  s>f f* ;
