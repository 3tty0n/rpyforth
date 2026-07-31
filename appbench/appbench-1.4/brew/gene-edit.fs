\ gene-edit.fs
\ 	$Id: gene-edit.fs,v 1.10 2005/03/31 15:25:48 f Exp $	

\ Editing genes by hand.

s" string-replace.fs" REQUIRED

\ DISPLAYING DECOMPILED GENES or writing them to a file with somehow sensible
\ line breaks.  First build a list of line strings.

4 VALUE indentation-step

\ When building  decompiled-gene-list  this word starts a new line as sublist:
: start-decompilation-line ( ind pos body list -- ind pos' body list )
    1 deflist over list>list
    fourth over to-last-nodes-sublist!		\ indent as first sublist node
    2>r drop dup indentation-step * 2r> ;	\ indentation is new position

\ Build a list of sublists, each one representing a line in the display of
\ a decompiled gene. Node zero has the indentation level, the others the xt's.
\ ( Menu scrolling must know how many lines the gene display takes.)
: decompiled-gene-list ( internal-body -- list ) \ remove-list-recursively ...
    >r
    1 dup indentation-step * r>			( indent position body )
    1 deflist					( indent position body list )
    start-decompilation-line
    over >gene-tokens @ 0 ?DO			( indent position body list )
	over i gene-body>n'th-xt-addr @ >r	( ind pos body list  r: xt )
	r@ >body >r				( ind pos body list r: xt bod2)

	\ Change indentation level?
	r@ >gene-flags @ >r			( ...  r: ... gene-flags )
	r@ frame-popping and IF
	    2>r >r  1- 0 max  r> 2r>		\ decrease indentation
	THEN

	\ Start new structure on a new line?	( ind pos body list r: xt bod2)
	r> [ frame-pushing frame-popping or ] literal and IF
	    start-decompilation-line		\ start structure on new line
	THEN
	rdrop					( ind pos body list  r: xt )

	\ break line?
	third r@ xt>string
	nip + c-l 2 - > IF
	    start-decompilation-line		\ line break (line full)
	THEN

	\ Add the genes internal xt to the sublist:
	r@ over to-last-nodes-sublist!		\ add xt to sublist
	r@ xt>string nip >r rot r> + 1+ -rot	\ adjust position

	r> >body >gene-flags @ >r		( ...  r: ... gene-flags )
	r@ frame-pushing and IF
	    2>r >r  1+  r> 2r>			\ increase indentation
	THEN
	r> [ frame-pushing frame-popping or ] literal and IF
	    start-decompilation-line		\ line break after structure
	THEN
    LOOP
    >r 2drop drop r> ;				\ return list

\ Write the genes of the list to (outfile-id), remove list:
: genes>file ( list -- )
    >r

    r@ r@ nodes 0 ?DO	( current-node )
	next-node
	dup @		( current-node sublist )	\ sublist for each line
	0 over n'th-node @ indentation-step * 0 ?DO
	    bl char2out
	LOOP
	dup nodes >r  next-node  r> 1 ?DO	( top-list-node sub-list-node )
	    next-node
	    dup @ xt>string cat2out
	    bl char2out
	LOOP
	drop		( handle current-node )
	out-line
    LOOP
    drop

    r> remove-list-recursively ;

\ Translation table to the names of the internal genes to find xt's:
BEGIN-string-table
s" [IF]"   table-string!	\ the first string gets replaced
s" IF"     table-string!	\ by the second
s" [ELSE]" table-string!
s" ELSE"   table-string!
s" [THEN]" table-string!
s" THEN"   table-string!
s" [if]"   table-string!
s" IF"     table-string!
s" [else]" table-string!
s" ELSE"   table-string!
s" [then]" table-string!
s" THEN"   table-string!

s" GENE:"  table-string!	\ trick to skip GENE: line
s" \"      table-string!
s" gene:"  table-string!
s" \"      table-string!
s" ;GENE"  table-string!	\ trick to skip ;GENE line, will be re-added
s" \"      table-string!
s" ;gene"  table-string!
s" \"      table-string!
s" ;"      table-string!	\ trick to skip ; line
s" \"      table-string!
BUILD-string-table VALUE 2internal-table

\ Translate an evaluated genes name to the internal genes name:
: translate2internal ( addr count -- addr' count' )
    2internal-table translate-string ;

\ Separate words in the string, translate to internal and add to the buffer.
\ Skip comments.
: string2internal ( addr count handle -- )
    >r

    BEGIN	( addr count  r: handle )
	bl-skip
    dup WHILE	\ while something is left
	-1
	BEGIN	( addr count tested-length  r: handle )
	    1+
	    third over + c@ bl? IF
		TRUE
	    ELSE
		2dup =
	    THEN
	UNTIL	( addr count tested-length  r: handle )

	third over	( addr count tested-length  addr tested-length  r: h )
	translate2internal		\ replace interpreted names
	dup 1 = IF			\ test for '(' and '\' comments
	    over c@ CASE
		[char] \ OF
		    2drop 2drop
		    0 FALSE
		ENDOF
		[char] ( OF
		    2drop
		    /string
		    [char] )  skip-until-char
		    FALSE
		ENDOF
		TRUE
	    ENDCASE
	ELSE TRUE THEN

	IF
	    r@ cat  bl r@ char-cat		\ and write to buffer
	    /string
	THEN
    REPEAT
    2drop rdrop ;

\ Build a temporary gene file name:
: gene-tmp-file-name ( addr count -- handle )	\ close buffer please
    tmp-dir s" gene-" file-name-cat >r
    ( addr count ) r@ cat
    unique-identity-string dup string@ r@ cat stringbuf-close
    s" .fs" r@ cat
    r> ;

\ Write a decompiled gene list to a temporary gene file:
: write-gene-tmp-file ( xt -- )
    dup xt>string gene-tmp-file-name
    dup string@ r/w CREATE-tmp-FILE
    ABORT" write-gene-tmp-file: Could not create file."
    (outfile-id) @ >r  set-outfile
    stringbuf-close

    s" GENE: " cat2out
    dup xt>string cat2out out-line

    >body decompiled-gene-list genes>file
    (outfile-id) @ CLOSE-FILE drop
    [ flush-files 0= ] [IF] (outfile-id) @ flush-file drop [THEN]  \ needed
    r> (outfile-id) ! ;

\ Search for the internal xt based on the name:
: string2internal-xt ( addr count -- xt TRUE | FALSE )
    [ decimal ] 64 stringbuf-open >r
    s" internal' " r@ string!
    r@ cat
    r@ string@ ['] EVALUATE CATCH
    r> stringbuf-close
    IF   2drop FALSE
    ELSE TRUE	THEN ;

\ Build internals data from a Forth string:
: compile-from-string ( addr count cp@ -- internals-handle TRUE | FALSE )
    cp!

    initialise-mutation >r
    s" TOP" push-frame
    BEGIN	( current-inp-addr current-inp-count  r: internals-handle )
	next-word dup WHILE	\ something left?
	2dup string2internal-xt 0= IF
	    bell
	    cr ." compile-from-string:"
	    cr type ."   is not an internal gene name." wait
	    2drop r> stringbuf-close
	    FALSE EXIT
	THEN

	dup matching-gene-alternative? IF nip THEN
	r@ ['] follow-&-add CATCH
	dup IF
	    |stack-symbols-mismatch = IF
		bell
		2drop 2drop 2drop
		r> stringbuf-close
		cr wait			\ let the user read follow-&-add errors
		FALSE EXIT
	    ELSE
		bell
		true ABORT" compile-from-string: follow-&-add unknown THROW"
	    THEN
	THEN
	drop 2drop
    REPEAT \ THEN
    drop

    [internal'] ;GENE r@ follow-&-add
    pop-frame

    r> TRUE ;

\ Remove 'GENE: xxx' and ';gene' or similar from a string:
: remove-gene-delimiters ( addr count -- addr' count' )
    s" : " search IF
	next-word 2drop next-word 2drop
    THEN
    bl-skip

    2dup [char] ; char-search-backwards IF nip THEN ;

\ Version removing 'GENE: xxx' and ';gene' or similar.
: |compile-from-string| ( handle --  internals-handle TRUE | FALSE )
    >r
    r@ string@ remove-gene-delimiters cp@ compile-from-string
    r> stringbuf-close ;

\ Build a 'decompiled' gene list from a Forth string:
: trial-string-2-gene-list ( addr count -- list TRUE | FALSE )
    [ decimal ] 256 stringbuf-open >r
    r@ string2internal
    r@ string@ cp@ compile-from-string
    r> stringbuf-close
    0= IF  FALSE EXIT  THEN

    >r r@ buffer-data-addr decompiled-gene-list
    r> stringbuf-close
    TRUE ;

: out-trial-gene ( addr-trial-string count -- )
    2dup trial-string-2-gene-list
    IF
	s" GENE: unnamed" (outfile-id) @ write-line drop
	genes>file
	2drop
    ELSE
	( addr-trial-string count ) (outfile-id) @ write-line
	ABORT" out-trial-gene: Could not write-line." 
	(outfile-id) @ flush-file
	ABORT" out-trial-gene: Could not flush-file."
    THEN ;

\ Write a genome on trial to a temporary gene file:
: write-trial-gene-tmp-file ( addr-trial-string count -- )
    s" trial" gene-tmp-file-name
    dup string@ r/w CREATE-tmp-FILE
    ABORT" write-trial-gene-tmp-file: Could not create file."
    (outfile-id) @ >r  set-outfile
    stringbuf-close

    out-trial-gene

    r> (outfile-id) ! ;

\ Load a temporary gene file and try to build the internal data:
: load-tmp-file2internal ( -- FALSE| handle TRUE)
    reopen-last-tmp-file IF  drop FALSE  THEN
    512 stringbuf-open			\ output buffer for internal names
    file-line-max# stringbuf-open >r	\ line input buffer
    r@ buffer-data-addr file-line-max# 
    third >r
    BEGIN	( output-handle input-buffer-addr file-line-max#  r: handle )
	2dup (last-tmp-file-id) @ READ-LINE
	ABORT" load-tmp-file2internal: Could not read-line."
    WHILE	\ file not exhausted	 ( handle addr length read-count )
	third swap r@ string2internal
    REPEAT
    2drop drop rdrop  r> stringbuf-close
    TRUE ;

\ Load a temporary gene file, try to compile internals data and set up trial:
: compile-from-last-tmp-file ( -- flag )
    load-tmp-file2internal IF
	dup string@ cp@ compile-from-string IF
	    set-up-trial TRUE
	ELSE
	    bell FALSE
	THEN
	swap stringbuf-close
    ELSE
	bell FALSE
    THEN ;

: ?log-edit-genome ( -- )
    log-user? 0= IF  EXIT  THEN

    0 log-out-line	\ empty line
    s" User changed genome to:"		0 log-it
    (last-tmp-file-name) string@ r/o OPEN-FILE
    IF
	2drop
	s" ?log-edit-gene: Could not open "	cat-log
	(last-tmp-file-name) string@		0 log-it
	EXIT
    THEN

    file-line-max# stringbuf-open >r r@ buffer-data-addr >r
    BEGIN
	r@ file-line-max# third READ-LINE
	IF
	    2drop
	    s" ?log-edit-gene: Could not read-line." 0 log-it
	    true
	ELSE
	    IF
		r@ swap 0 log-it
		false
	    ELSE
		drop true
	    THEN
	THEN
    UNTIL

    rdrop r> stringbuf-close
    CLOSE-FILE drop ;

\ Write genome to a file, open editor on it, re-load changed file and try
\ to set up nuc with the new genome:
: edit-genome ( -- )
    on-trial? IF
	wake-me-xt @
	dup eb>length @ swap eb>sequence swap write-trial-gene-tmp-file
    ELSE
	wake-me-internal @ write-gene-tmp-file
    THEN

    (last-tmp-file-name) string@ create-backup-file+

    editor s" emacs" search IF	\ remove emacs backup file on exit
	(last-tmp-file-name) string@
	string!! [char] ~ over char-cat
	dup string@ (tmp-file-list-id) @ write-line drop
	(tmp-file-list-id) @ flush-file drop
	stringbuf-close
    THEN 2drop
    editor string!!	bl over char-cat
    (last-tmp-file-name) string@ third cat
    dup string@ <system>
    swap stringbuf-close
    IF  bell  EXIT  THEN

    \ Is there a change?
    (last-tmp-file-name) string@ compare-to-backup 0= IF  EXIT  THEN

    ?log-edit-genome

    compile-from-last-tmp-file IF EXIT THEN
    bell

    log-user? IF	\ error
	s" User input could not be compiled, genome reset." 0 log-it
	0 log-out-line	\ empty line
    THEN ;

\ Menu to inspect and edit gene code of a nuc:
MENU: gene-edit-men
: .gene-edit-menu ( -- )
    help-node" Gene edit menu"
    s" Menu gene edit:" start-title-entry clear-line-to-end

    on-trial? IF
	false  wake-me-xt @
    ELSE
	true wake-me-internal @
    THEN

    17 at-x
    over IF	\ compiled genome?
	dup xt>string type
	1 3 screen-column ." Sub genes: "  dup >body >gene-tokens @ .
	2 3 screen-column s" .gene-info"
	redisplay  third >stack	 menu-wait	['] .gene-info	 menu-entry
	s" .il" menu-same-key-entry
    ELSE
	." genome on trial"
    THEN
    end-title

    4 keep-but-scroll-rest

    menu-scrolled @ 0= IF
	from-here ." GENE: "
	over IF	\ compiled genome?
	    dup xt>string
	ELSE
	    s" unnamed (on trial)"
	THEN
	menu-done	redisplay	['] edit-genome		menu-entry
    THEN
    s" e"	redisplay	['] edit-genome		menu-key-entry

    over IF \ compiled genome?
	dup >body decompiled-gene-list
    ELSE
	dup eb>sequence  over eb>length @ trial-string-2-gene-list
	0= IF
	    bell
	    1 unnest-menus
	    EXIT
	THEN
    THEN >r				( compiled-flag xt  r: list )
    
    r@ r@ nodes 0  scrolled-range  ?DO	( compiled-flag xt list )
	cr
	i over n'th-node @		( compiled-flag xt list sublist )

	\ A sublist for each line
	0 over n'th-node @ indentation-step * at-x	\ indentation in 0 node
	dup nodes 1 ?DO
	    i over n'th-node @ >r
	    r@ xt>string
	    r@ >body >gene-flags @ [ primitive ] literal and IF
		r@ >stack  redisplay menu-wait	['] .gene-info	menu-entry
	    ELSE
		type
	    THEN
	    rdrop
	    bl emit
	LOOP
	drop
    LOOP
    drop
    2drop
    r> remove-list-recursively

    <common-menu-entries> ;

\ Edit genome of this nuc:
:NONAME \ : gene-edit-menu ( --- )
    gene-edit-men

    ['] .gene-edit-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop ; IS gene-edit-menu

: |gene-edit-menu| ( cp@ -- )
    cp@ >r
    cp!  gene-edit-menu
    r> cp! ;
