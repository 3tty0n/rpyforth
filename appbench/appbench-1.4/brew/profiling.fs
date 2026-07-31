\ profiling.fs
\ 	$Id: profiling.fs,v 1.7 2005/04/18 05:35:29 f Exp $	

\ If 'COUNTING-WORDS'is defined (i.e. from command line) this file
\ get's included and provides the possibility to write word usage count
\ profiling statistics to a file.

\ It's used only for the development of the program,
\ helping to find out which words to choose to optimise speed.

[UNDEFINED] COUNTING-WORDS [IF]
    page
    bell
    cr .( profiling.fs: )
    cr
    cr .( No code for word usage profiling included.)
    cr .( Start brew with -e" CREATE COUNTING-WORDS" to have it available.)
    cr
    wait
[ELSE]

also profiling

\ Write usage counts and names of a sorted list to '(outfile-id)':
: write-sorted-usage-list ( list -- )

    dup nodes
    s" Usage count for "	cat2out
    dup				num2out
    s" words:"			cat2out out-line
    0 ?DO
	next-node
	dup cell+ @	num2out
	BEGIN
	    (out-buffer) buffered-length 11 <
	WHILE
	    bl		char2out
	REPEAT
	dup @ xt>string	cat2out out-line
    LOOP
    drop
    out-line ;

\ Setup, sort and write  usage counts and names of a list to '(outfile-id)':
: write-usage-counts ( list -- )
    dup counts>list
    1 swap copy-to-sorted-list
    dup write-sorted-usage-list
    remove-list ;

\ Write usage count and name of all VARIABLEs, 2VARIABLEs, VALUES,
\ CONSTANTs ans 2CONSTANTs to '(outfile-id)':
: write-variable-usage ( -- )
    s" Profiling usage of VARIABLEs, 2VARIABLEs, VALUES, CONSTANTs and 2CONSTANTs:"
    2dup cr type
    cat2out out-line
    variables write-usage-counts ;

\ Write a list of all colon words (e.a.) usage counts and names to
\ '(outfile-id)':
: write-colon-usage ( -- )
    s" Profiling usage of colon words (e.a.):"
    2dup cr type
    cat2out out-line
    colon-definitions write-usage-counts ;

: write-CREATE-DOES>-usage ( -- )
    s" Profiling usage of CREATE DOES> words:"
    2dup cr type
    cat2out out-line
    create-does-words write-usage-counts ;

\ Write a list of all counted words to '(outfile-id)':
: write-all-usage ( -- )
    s" Profiling usage of all profiled words:"
    2dup cr type
    cat2out out-line

    variables counts>list
    colon-definitions counts>list
    create-does-words counts>list

    2 deflist

    variables
    dup nodes 0 ?DO
	next-node
	dup 1 fourth insert-node-sorted
    LOOP
    drop

    colon-definitions
    dup nodes 0 ?DO
	next-node
	dup 1 fourth insert-node-sorted
    LOOP
    drop

    create-does-words
    dup nodes 0 ?DO
	next-node
	dup 1 fourth insert-node-sorted
    LOOP
    drop

    dup write-sorted-usage-list
    remove-list
    cr ;

VARIABLE (profile-file-id)
: ?open-profile-file ( -- )
    (profile-file-id) @ IF EXIT THEN

    file-names-length# stringbuf-open >r
    tmp-dir				r@ string!
    s" brew-profile"			r@ cat
    indentity-string			r@ cat
    r@ string@ w/o CREATE-FILE+
    IF drop
	bell
	green color-foreground
	cr ." ?open-profile-file: Error creating profile file "
	r@ string@ type
	cr ." not writing brew-profile file."
	cr default-foreground
	2000 wait-until
	FALSE
    THEN
    (profile-file-id) !
    r> stringbuf-close ;

: write-usage-profile ( -- )
    ?open-profile-file
    (profile-file-id) @ dup IF
	(outfile-id) !
	page
	." write-usage-profile: this might take a while..."
	cr
	s" Profiling word usage, created by 'write-usage-profile'."
	cat2out out-line
	out-line
	write-variable-usage
	write-colon-usage
	write-CREATE-DOES>-usage
	write-all-usage
	." ok, done. "
	cr
	cr ." See file " tmp-dir type ." brew-profile"  
	cr
    ELSE
	drop
	cr ." write-usage-profile: Could not write brew-profile."
	cr
	2000 wait-until
    THEN ;

' write-usage-profile IS <goodbye-actions>

previous

page
cr .( 'profiling.fs': )
cr cr
.( type: write-usage-profile to write a profile to 'OUTPUT/tmp/brew-profile')
cr

[THEN]
