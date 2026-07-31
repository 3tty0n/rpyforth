\ mutation-0.3.fs

: mutation-version ( -- addr count )
    cvs" 	$Id: mutation-0.3.fs,v 1.40 2005/04/05 14:12:39 f Exp $	" ;

decimal

\ I need a good tool to debug mutation ;-)
\ If 'log-mask' is true at compile time there will be more places
\ where it is possible to log the inner working of mutation.
\ It will still be run time switchable, but brew will run a bit slower.
\ Setting it to a positive value will log some extra information.
\ This makes only sense for heavy debugging and is only occasionally supported.
[UNDEFINED] log-mask [IF]  	\ if not defined in compile-options.fs
    VARIABLE log-mask		\ compile time switch
    log-mask OFF
[THEN]

\ stack control:
\ I see two good ways to control the stack effect of mutated genes:

\ No control at all at mutation time, combined with a more or less strict
\ control at run time. Stack violations could be made mortal, or be tolerated
\ to a certain degree.

\ Control at mutation time. This allowes some type checking too.
\ I don't like type checking too much, but I want my mutations to be save.
\ I wouldn't like my cells to be able to try changing random addresses
\ or the like.  So I want to treat at least addresses and other data different
\ Some more distinctions between data types might accelerate evolution,
\ like having a special type for flags or directions.
\ In these cases type checking could be made tolerant in an adjustable degree.

\ I like to use the stack for stack type checking:
\ Genes can be executed in a special way, that they work on symbols on the
\ stack in analogy they would work normally.  So a gene taking two numbers
\ from the stack and leaving a flag there, would check and swallow two
\ number symbols ( which are the ASCII code of the char 'n') and leave a
\ flag symbol there.

[UNDEFINED] max-stack-effect [IF]
    &64 constant max-stack-effect	\ no genom should ever go beyond that
[THEN]

\ gene-follow is called sucessively on each xt of a gene sequence
\ it can be used reading out genes or during the mutation process.
\ what it does:
\ - keep track of stack symbols, or abort if they do not fit.
\ - do depth-watch.
\ - follow IF-ELSE-THEN structures keeping track of nesting levels,
\   stack information and stack requirements of the two branches.
\ - follow any type of sequences, as long as they are contained in each other.
\ - let's add a frame type tag.  it is a one word string.
\	we could even execute the string during gene compilation... ;-)
32 CONSTANT frame-tag-max-length#

VARIABLE (follow-frames)	32 (follow-frames) !	\ default
0
OFFSET: >stack-aim	( addr-of-count )	\ first, because it's fastest
cell+			( pointer )
max-stack-effect +	\ this reserves in fact four times as much, intended.

OFFSET: >initial-stack	( addr-of-count )
cell+			( pointer )
max-stack-effect +	\ this reserves in fact four times as much, intended.

OFFSET: >frame-tag	( addr-of-count )
cell+			( pointer )
frame-tag-max-length# +		\ here the tag gets stored

OFFSET: >minimal-depth
OFFSET: >peak-level
OFFSET: >frame-cost
CONSTANT frame-size#
frame-size# cell mod [IF]
    cr cr .( Compile time error: 'frame-size# CELL MOD' must be zero! ) cr
    quit
[THEN]

frame-size# (follow-frames) @ * STRINGBUF-HANDLE: (follow-data)

VARIABLE frame#		frame# off


0 VALUE (frame)		\ gives startaddress of actual frame

: aim ( -- addr )		(frame) >stack-aim ;
: initial ( -- addr )		(frame) >initial-stack ;
: frame-tag ( -- addr )		(frame) >frame-tag ;
: minimal-depth ( -- addr)	(frame) >minimal-depth ;
: peak-level ( -- addr) 	(frame) >peak-level ;
: frame-cost ( -- addr) 	(frame) >frame-cost ;

DEFER <all-frames-init-pointers>
: set-frame-actual ( level -- )
    dup 0< ABORT" set-frame-actual: Level negative."

    dup frame# !

    (follow-data) buffer-data-addr
    frame# @ frame-size# *  +  to (frame)

    (follow-frames) @ < 0= IF			\ enough (follow-frames)?
	(follow-data) double-stringbuf
	(follow-frames) dup @ 2* swap !		\ double if not enough

	(follow-data) buffer-data-addr
	frame# @ frame-size# *  +  to (frame)	\ (frame) must be reset
	<all-frames-init-pointers>		\ initialise stringpointers
    THEN ;


\ I use some stringpointers of the form
\ addr			holds count	\ 2@ on addr gives ( string-addr count)
\ addr cell+		holds pointer
\ addr cell+ cell+	start of string
\ 'init-stringpointer'  initialises the pointer, but does *not* change count.
: init-stringpointer ( addr -- )
    cell+	\ address of stringpointer
    dup cell+	\ string starts just behind
    swap ! ;	\ initialise pointer

: frame-init-stringpointers ( -- )
    aim		init-stringpointer
    initial	init-stringpointer
    frame-tag	init-stringpointer ;

: all-frames-init-stringpointers ( -- )
    frame# @			\ to restore it when done
    (follow-frames) @ 0 DO
	i set-frame-actual  frame-init-stringpointers
    LOOP
    set-frame-actual  ;			\ restore frame
' all-frames-init-stringpointers IS <all-frames-init-pointers>
all-frames-init-stringpointers

: frame-store-string ( from-addr count to-address -- )
	>r
	dup r@ !	\ store length
	r> cell+ cell+	\ skip count and pointer
	swap move ;	\ move string into frame data

3 CONSTANT frame-indent#
: indent-frame ( -- )
    frame# @  frame-indent# *  0 ?DO s"  " cat-log LOOP ;

: log-frame-name&stack ( addr count -- )
    indent-frame
    ( addr count -- )	cat-log
    s"  "		cat-log
    frame# @		log-number
    s" 	( "		cat-log
    symbols-as-string	cat-log
    s"  )"		0 log-it ;

: ?mutation-type>code-file ( xt-of-mutation-type -- )
    code-file-mask @ >r
    r@ write-code-file AND 0=	  IF rdrop drop EXIT THEN
    r@ file-mutating   AND 0=	  IF rdrop drop EXIT THEN
    r> file-mutation-type  AND 0= IF       drop EXIT THEN

    \ yes, mutation type get's included in code file:
    indent-code
    s" \ mutation type: "	cat>code-file
    ( xt ) xt>string		cat>code-file
    code-file-write-line ;

: ?stack>code-file ( -- )
    file-stack  code-file-mask @  AND IF
	code-next-2-tab
	s" ( "			cat>code-file
	symbols-as-string	cat>code-file
	s" )"			cat>code-file
    THEN ;

VARIABLE (item-number)
: ?item-number>code-file ( -- )
    file-item-number  code-file-mask @  AND IF
	(item-number) dup @ 1+ swap !
	s"  	\ item number "		cat>code-file
	(item-number) @ num>string	cat>code-file
    THEN ;

: ?depth&cost>code-file ( -- )
    file-depth&cost code-file-mask @ AND IF
	indent-code
	s" \ depth-min: "		cat>code-file
	minimal-depth @ num>string	cat>code-file
	s"  max: "			cat>code-file
	peak-level @ num>string		cat>code-file
	s"  cost: "			cat>code-file
	frame-cost @ num>string		cat>code-file
	code-file-write-line
    THEN ;

: frame-tag&stack&depth&cost>file ( addr count -- )
    indent-code
    s" \ "			cat>code-file
    ( addr count )		cat>code-file
    frame-tag 2@		cat>code-file
    s"  "			cat>code-file
    frame# @ num>string		cat>code-file
    ?stack>code-file
    code-file-write-line

    ?depth&cost>code-file ;

: push-frame ( tag-addr tag-count -- )	\ stack symbols must be there already
    frame# @  1+  set-frame-actual	\ new horizon

    [ log-mask @ ] [IF]	\ conditional compiling
	log-m-much? IF
	    s" PUSHing frame "	cat-log
	    2dup log-frame-name&stack

	    frame# @ ?dup IF
		s"     frames below: " cat-log
		0 BEGIN
		    2dup <> WHILE
		    dup set-frame-actual
		    frame-tag 2@ cat-log s"  " cat-log
		    1+
		REPEAT
		drop s"  " 0 log-it
		set-frame-actual
	    THEN

	THEN
    [THEN]

    frame-tag frame-store-string	\ store tag in frame data
    symbols-as-string initial frame-store-string \ store initial stack symbols
    #symbols-on-stack minimal-depth !	\ init depth and peak watch
    #symbols-on-stack peak-level !
    frame-cost off

    write-code-file  code-file-mask @  AND IF
	file-mutating  code-file-mask @  AND IF
	    file-frames code-file-mask @ AND IF
		s" push frame: " frame-tag&stack&depth&cost>file
	    THEN
	THEN
    THEN ;

: pop-frame ( -- )	\ we could check the tag here, I don't for efficiency.
    minimal-depth @
    peak-level @
    frame-cost @
    frame# @  1-  set-frame-actual
    frame-cost +!				\ add cost
    #symbols-on-stack max			\ is this needed? #######
    peak-level >r r@ @ max r> !
    #symbols-on-stack min			\ is this needed? #######
    minimal-depth >r r@ min r> !

    [ log-mask @ ] [IF]	\ conditional compiling
	log-m-much? IF
	    s" POPping frame "		cat-log
	    frame-tag 2@		log-frame-name&stack

	    s"               "		cat-log
	    s" new depth-min: "		cat-log
	    minimal-depth @		log-number
	    s"  max:"			cat-log
	    peak-level @ num>string	0 log-it
	THEN
    [THEN]

    write-code-file  code-file-mask @  AND IF
	file-mutating  code-file-mask @  AND IF
	    file-frames code-file-mask @ AND IF
		s" pop frame:  " frame-tag&stack&depth&cost>file
	    THEN
	THEN
    THEN ;

\ store frame#, all frame data and actual stack symbols:
frame-size# OFFSET: >preserved-stack drop
frame-size# cell+ cell+ max-stack-effect + OFFSET: >stored-frame#
OFFSET: >stored-code-indent drop
\ ==> please don't forget to 'free' it later!
: store-actual-stack-and-frame ( -- data-address ) \ 'FREE' it yourself.
    [ frame-size#				\ space for frame data
    cell+ cell+ max-stack-effect +		\ count addr stack-symbol strng
    cell+ cell+ ] literal			\ frame#
    allocate	( addr ior )			\ 'free' it later, please.
    ABORT" frame-preserve-entry: Couldn't allocate."
    (frame) over frame-size# move

    \ initialised stringpointers let us read strings from here, if we need that
    dup >stack-aim	init-stringpointer
    dup >initial-stack	init-stringpointer
    dup >frame-tag	init-stringpointer

    \ store actual stack
    symbols-as-string >r	( data-address addr-symbols  r: count-symbols )
    over >preserved-stack r@ swap !		\ store symbols count
    over >preserved-stack init-stringpointer	\ init symbols pointer
    over >preserved-stack cell+ cell+ r> move	\ store actual stack symbols

    frame# @  over >stored-frame# !		\ store frame#
    (code-indent) @  over >stored-code-indent !	\ store (code-indent)
;

\ restore frame#, all frame data and stack symbols
: restore-stack-and-frame ( data-pointer -- )
    dup >stored-frame# @ set-frame-actual	\ reset old frame# actual

    dup (frame) frame-size# move		\ reset frame data
    frame-init-stringpointers			\ init stringpointers

    clear-symbols
    dup >stored-code-indent @  (code-indent) !	\ restore (code-indent)
    >preserved-stack 2@ push-symbol-string ;	\ restore stack symbols

: depth-min-watch ( count -- )   minimal-depth >r r@ @ min r> ! ;

: depth-max-watch ( count -- )   peak-level    >r r@ @ max r> ! ;

: ?code-entry ( internals-xt -- )
    write-code-file  code-file-mask @  AND 0= IF drop EXIT THEN	 \ not filing

    file-mutating file-end-trial OR code-file-mask @ AND   \ time for an entry?
    0= IF drop EXIT THEN				   \ else nothing to do

    writing-code? 0<>				\ making code entries?
						\ or
    mutating? 0<>				\ are we mutating?
						\ and want entries then?
    code-file-mask @ file-mutating file-structure or and 0<> AND
    OR 0= IF drop EXIT THEN			\ no?  nothing to do

    file-code  code-file-mask @  AND		\ we want code entries?

    dup 0= IF					\ no? maybe structures?
	file-structure code-file-mask @  AND IF	\ yes
	    over CASE
		[internal'] if    OF drop true ENDOF
		[internal'] else  OF drop true ENDOF
		[internal'] then  OF drop true ENDOF
		[internal'] ;gene OF drop true ENDOF
	    ENDCASE
	THEN
    THEN

    IF
	indent-code
	>body >gene-compiled-xt @ xt>string cat>code-file
	?stack>code-file
	writing-code? 0= IF		\ not for trial entries
	    ?item-number>code-file
	THEN
	code-file-write-line

	writing-code? 0= IF		\ not for trial entries
	    ?depth&cost>code-file
	THEN

    ELSE drop THEN ;

[UNDEFINED] (name-buf) [IF]	32   STRINGBUF-HANDLE: (name-buf)	[THEN]
: new-gene-name ( -- addr count )	\ sets (name-buf) and returns string.
    (name-buf) stringbuf-empty
    s" g-" (name-buf) cat
    genome-id @ num>string (name-buf) cat
    bl (name-buf) char-cat
    (name-buf) string@ ;

: code-file-gene-name ( -- addr count )
    (name-buf) >r
    r@ stringbuf-empty
    s" mutation."		r@ cat
    step @ num>string		r@ cat
    [char] :			r@ char-cat
    spot @ num>string		r@ cat
    s" .GI:"			r@ cat
    genome-id @ num>string	r@ cat
    s" .to-GI:"			r@ cat
    (genome-id) @ 1+ num>string	r@ cat
    bl				r@ char-cat
    r> string@ ;

: start-to-follow ( -- )
    clear-symbols			\ opens symbols stack, if needed
    (code-indent) off
    (item-number) off

    -1 frame# !
    s" BOTTOM" push-frame ;

\ Some genes like IF/ELSE/THEN have special gene-follow behaviour.
\ Let's define it here:

: set-follow-behaviour ( action-xt internals-xt -- )
    >body
    dup special!
    >gene-follow-xt ! ;

:NONAME	\ 'IF'
    s" CONDITION" push-frame ; internal' if  set-follow-behaviour

:NONAME	\ 'ELSE'
    \ push-aim:		actual symbols as aim for else branche
    symbols-as-string aim frame-store-string

    \ dup initial	reset stack symbols to the state saved by 'IF'
    clear-symbols  initial 2@  push-symbol-string
; internal' else  set-follow-behaviour

:NONAME	\ 'THEN'
    pop-frame ; internal' then  set-follow-behaviour 

\  :NONAME	\ ;gene
\          -1 (code-indent) +! ; internal' ;gene >gene-follow-xt !

: gene-follow ( internal-xt -- )		\ can THROW
    dup >body	( internal-xt internal-body )

    \ get input symbols and make sure they match:
    dup dup >gene-stack-in 2@
    dup >r			( xt body body addr count -- r: in-count)
    symbols-match		( xt body r: in-count )
    0= IF
	cr ." gene-follow: Input stack symbols mismatch in  "
	over xt>string type
	cr ." on stack: " symbols-as-string type
	cr ." expected: " >gene-stack-in 2@ type
	|stack-symbols-mismatch THROW			\ TROWS on ERROR
    THEN

    no-code-cost? 0= IF			\ add gene-cost unless muted
	dup >gene-cost @  frame-cost +!
    THEN

    \ cut input symbols away:
    symbols-as-string			\ addr count of stack symbols
    r> -					\ cut consumed symbols

    \ How deep did we ever go within this sequence?
    \ (we need to know to determine input requirements of the sequence).
    dup depth-min-watch

    \ store intermediate stack in stack symbol stringbuf
    clear-symbols push-symbol-string

    \ now add output symbols.		( xt body )
    dup >gene-stack-out 2@  push-symbol-string
    #symbols-on-stack depth-max-watch

    \ Some genes have a special follow behaviour:
    dup >gene-flags @ special and IF	( xt body )
	dup >gene-follow-xt @ EXECUTE
    THEN

    \ Indentation in code file entries (popping): ######### move to ?code-entry
    dup >gene-flags @ frame-popping and IF -1 (code-indent) +! THEN

    \ Code file entry?
    swap ( body xt ) ?code-entry

    \ Indentation in code file entries (pushing): ######### move to ?code-entry
    >gene-flags @ frame-pushing and IF 1 (code-indent) +! THEN ;

: gene-follow-mute ( internal-xt -- )
    code-file-mask dup dup @ 2>r off
    run-mode dup @ no-code-cost or swap !
    gene-follow
    run-mode dup @ [ no-code-cost invert ] literal and swap !
    r> r> ! ;

VARIABLE mutation-max-ollowed-items	50000 mutation-max-ollowed-items !
VARIABLE (mutation-items)
: |xt-to-internals-buffer| ( handle xt -- )
    1 (mutation-items) +!
    (mutation-items) @ mutation-max-ollowed-items @ > IF
	|genome-too-long THROW
    THEN

    xt-to-internals-buffer ;

VARIABLE (mutated)
: follow-&-add ( xt handle -- )
    >r		( xt  r: handle )

    \ Check for pseudo genes (i.e. IF-ELSE-THEN building word):
    dup >body >gene-flags @ building and IF
	r> over >body >gene-compiled-xt @ EXECUTE EXIT
    THEN

    dup gene-follow

    1 (mutated) +!

    r> swap |xt-to-internals-buffer| ;

: cut-to-used-symbols ( addr count -- addr' count' )
    minimal-depth @
    over min >r			\ length got negative without 'min' sometimes
    swap r@ +			\ start of used part
    swap r> - ;			\ used length

: symbols-play ( start-addr n -- )
    cells over + swap	( stop-addr start-addr )
    BEGIN	( stop-addr actual-addr )
	2dup = IF 2drop EXIT THEN	\ end of chain reached? done
	dup @				\ get xt
	gene-follow			\ do the play
	dup @ [internal'] ;gene <>	\ end of gene reached?
    WHILE				\ then end
	cell+
    REPEAT
    2drop ;

\ do it without code file entries and without adding costs:
: symbols-play-mute ( start-addr n -- )
    code-file-mask dup dup @ 2>r off	\ no code file entries, please
    run-mode dup @ no-code-cost or swap !
    symbols-play
    run-mode dup @ [ no-code-cost invert ] literal and swap !
    r> r> ! ;				\ restore code-file-mask

: play-symbols-to-end ( tail-addr )	\ xt chain, ['] ;gene terminated

    [ log-mask @ ] [IF]
	log-m-more? IF
	    s" playing tail symbols:"	0 log
	THEN
    [THEN]

    highest-integer# cell / symbols-play ;

: random-gene-picking ( -- internal-xt)
    actual-genepool-xt @ execute	\ get list pointer
    pick-one @ ;

VARIABLE (depth-should-change)		\ to avoid depth drifting off too far
VARIABLE resolve-flags	1 resolve-flags !

: done-with-depth-change? ( body -- flag )
    (depth-should-change) @ >r

    \ if (depth-should-change) is off, we're done
    r@ 0= IF  rdrop drop TRUE EXIT THEN

    \ don't care about (depth-should-change) if depth does not change
    dup gene-stack-effect 0= IF drop rdrop TRUE EXIT THEN

    \ if (depth-should-change) is negative we only care if there are
    \ at least three symbols on stack
    r@ 0< IF	#symbols-on-stack 3 < IF

	[ log-mask @ ] [IF]	\ conditional compiling
	    log-m-more? IF
		s" selected-gene-picking: "		cat-log
		s" too few symbols to care about depth"	0 log-it
	    THEN
	[THEN]

	rdrop drop TRUE EXIT
    THEN THEN

    \ now (depth-should-change) must be respected
    gene-stack-effect r> * 0>		\ goes in the right direction?

    [ log-mask @ ] [IF]	\ conditional compiling
	log-m-more? IF
	    dup IF		\ goes in the right direction?
		s" selected-gene-picking: trying to guide stack back"
		0 log
	    THEN
	THEN
    [THEN]

    dup 0=   [char] C symbols-tos-match?  AND IF
	0=			\ accept conditions when depth should change

	[ log-mask @ ] [IF]	\ conditional compiling
	    log-m-much? IF
		s" selected-gene-picking: condition accepted, might fix depth"
		0 log
	    THEN
	[THEN]

    THEN
;

\ pick a random, but preselected gene matching the symbol stack
\ addresses on tos will always get matched first
\ otherwise (depth-should-change) is respected if
\   * depth changes at all
\   * it's positive, or it's negative and at least two symbols on stack
: selected-gene-picking ( -- internal-xt)
    BEGIN
	random-gene-picking dup >body		( internal-xt body )

	dup dup >gene-stack-in 2@ symbols-match IF	\ input match?

	    \ addresses get matched first
	    [char] a symbols-tos-match? IF	\ address to match?
		\ addresses on tos must always be matched
		dup >gene-stack-in @			\ gen requires input?
		over done-with-depth-change? AND IF	\ and depth change OK?

		    [ log-mask @ ] [IF]	\ conditional compiling
			log-m-much? IF
			    s" selected-gene-picking: address resolved"
			    0 log
			THEN
		    [THEN]

		    drop EXIT		\ address matched, done

		ELSE			\ gen requires no input,
		    2drop RECURSE EXIT	\ start again
		THEN
	    THEN
	    [char] A symbols-tos-match? IF	\ dfloat-address to match?
		\ addresses on tos must always be matched
		dup >gene-stack-in @			\ gen requires input?
		over done-with-depth-change? AND IF	\ and depth change OK?

		    [ log-mask @ ] [IF]	\ conditional compiling
			log-m-much? IF
			    s" selected-gene-picking: dfloat-address resolved"
			    0 log
			THEN
		    [THEN]

		    drop EXIT		\ address matched, done

		ELSE			\ gen requires no input,
		    2drop RECURSE EXIT	\ start again
		THEN
	    THEN
	    \ no address to match

	    [char] C symbols-tos-match? IF
		resolve-flags @ random-ranged 0= IF
		    dup >gene-stack-in @		\ gen requires input?
		    over done-with-depth-change? AND IF	\ and depth change OK?

			[ log-mask @ ] [IF]	\ conditional compiling
			    log-m-much? IF
				s" selected-gene-picking: flag resolved"
				0 log
			    THEN
			[THEN]

			drop EXIT		\ flag resolved, done
		    ELSE
			2drop RECURSE EXIT	\ start again
		    THEN

		    [ log-mask @ ] [IF]	\ conditional compiling
			log-m-much? IF
			    s" selected-gene-picking: flag resolving delayed"
			    0 log
			THEN
		    [THEN]

		THEN
	    THEN

	    dup done-with-depth-change? IF drop EXIT THEN

	THEN	\ stack mismatch

	2drop		\ did not fit
    AGAIN		\ try again
;


log-mask @ [IF]		\ I just redefine selected-gene-picking ;-)
: selected-gene-picking ( -- internal-xt)
    selected-gene-picking ( -- internal-xt)
    log-m-much? IF
	s" selected-gene-picking picked: "	cat-log
	dup xt>string				0 log-it
    THEN ;
[THEN]

\ words to deal with provisoric gene sequences kept as string in an
\ evaluation buffer ['eb']
0
OFFSET:	eb>length		\ length of the string to evaluate
OFFSET:	eb>compiled		\ zero or xt of compiled version
OFFSET:	eb>counter		\ how many living cells use this sequence
OFFSET: eb>internals		\ handle of internals buffer. xt later on #############
\ eb>sequence *must* be last!   It *is* the starting address, not a pointer.
OFFSET:	eb>sequence		\ start of sequence string
CONSTANT eb-header-length	\ how many entries at the beginning of buffer

: decrease-eb-count ( "xt" -- )	\ the "xt" is the allocation address ##########
    dup >r
    eb>counter				\ decrease counter
    dup @ 1- dup IF			\ if >0
	swap !				\ store it
    ELSE
	2drop
	r@ eb>compiled @ 0= IF			\ not compiled yet.
	    r@ eb>internals @ stringbuf-close	\ so close internals buffer.
	THEN
	r@ free				\ else free memory
	IF
	    cr ." problem free'ing trial gene sequence."
	    bell 1500 ms
	THEN
    THEN rdrop ;

log-mask @ [IF]
\ logs stack symbols after a starting string.  log-mask is checked elsewhere.
: log-stack-symbols  ( addr count -- )
    #symbols-on-stack over + stringbuf-open >r r@ cat	\ r: handle
    symbols-as-string r@ cat
    r@ string@ 0 log
    r> stringbuf-close ;
[THEN]

VARIABLE mutations-threshold \ after so many mutations we try to come to an end
1 mutations-threshold !

\ if stack depth is more then that off, only genes that won't make it worse
\ will be inserted. Strong influence on the complexity of mutations.
VARIABLE stack-turning-point	3 stack-turning-point !

: log-chained-names ( start-index stop-index internals-body -- )
    >gene-start >r		( start-index stop-index  r: internals-body ) 
    cells r@ +  swap cells r> + ?DO	( -- )
	i @ xt>string cat-log  s"  " cat-log
    cell +LOOP
    s" " 0 log-it ;

: log-chained-names-tail ( start-index internals-body -- )
    >r  r@ >gene-tokens @  r> log-chained-names ;

\ Build a new segment and append it to the given string buffer.
\ The buffer holds the Forth code as a string
\ (that can be evaluate'd and compiled).
\
\ Respect the type of stack data using the actual symbols stack.
\ Guide the stack data flow to the state given in addr-aim count-aim.
\ The symbols stack will contain this string after execution.
\
\ The sequence might use only a part of input data, so:
\ Keep track of the input requirements of the sequence and the output produced,
\ Adjust depth-watch to be reused by the calling word.
: build-new-segment ( addr-aim count-aim handle -- )
    dup buffer-data-addr >gene-tokens @ >r	( ...  r: offset-index-for-log)
    >r				( addr-aim count-aim  r: offset handle )

    s" build-new-segment starting..."	log-m-some log

    s" SEGMENT" push-frame

    \ preserve desired stack condition (aim)
    ( addr-aim count-aim ) aim frame-store-string

    [ log-mask @ ] [IF]			\ conditional compiling
	log-m-more? IF
	    s" stack at start: "	log-stack-symbols
	    s" stack aim     : "	cat-log
	    aim 2@			0 log-it	    
	THEN
    [THEN]

    \			( r: handle of the buffer to put the xt's in )

    (depth-should-change) off
    BEGIN
	(mutated) @ mutations-threshold @ > IF
	    \ set desired direction of stack depth change:
	    aim @				( aim-depth )
	    #symbols-on-stack - (depth-should-change) !

	    (depth-should-change) @ abs  stack-turning-point @  - 0> 0= IF
		(depth-should-change) off
		[ log-mask @ ] [IF]		\ conditional compiling
		    log-m-more? IF
			s" stack-turning-point not reached "
			stack-turning-point @	0 log-string-and-number
		    THEN
	    ELSE
		    log-m-more? IF
			s" stack-turning-point passed "	0 log
		    THEN
		[THEN]
	    THEN

	    [ log-mask @ ] [IF]		\ conditional compiling
		log-m-more? IF
		    s" (depth-should-change) set to "
		    (depth-should-change) @    0 log-string-and-number
		THEN
	    [THEN]

	THEN

	selected-gene-picking r@ follow-&-add

	[ log-mask @ ] [IF]		\ conditional compiling
	    log-m-more? IF
		s" new stack: " log-stack-symbols
		s" aim is   : " cat-log
		aim 2@		0 log-it
	    THEN
	[THEN]

	aim 2@  symbols-as-string  compare 0=	\ aim reached?
	[ log-mask @ ] [IF]		\ conditional compiling
	    log-m-more? IF
		dup IF
		    s" aim reached :-)"		0 log
		THEN
	    THEN
	[THEN]

    UNTIL

    pop-frame

    log-m-some? IF
	s" ==> new-segment built:"	0 log
	2r@ buffer-data-addr log-chained-names-tail
    THEN

    2rdrop ;

\ Build a '[IF] ... [ELSE] ... [THEN]' structure in the buffer handled.
\ Respect the type of stack data using the actual symbols stack,
\ and make sure, both parts deliver the symbol stack in the same state.
\ The symbols stack will contain this state after execution.
\
\ The sequence might use only a part of input data, so:
\ Keep track of the input requirements of the sequence and the output produced,
\ put results as comment in the code string of the form
\ ... ... CODE ... ... instack" nc" outstack" nna"
\ These can be used to compile the code as a separate gene when combining
\ sequences to longer sequences.
\ Do depth-watch-restart with the minimum of the previous and the actual value,
\ to be reused by the calling word.

\ up to how many items shall we put in the first if branch?
VARIABLE max-if-items		4 max-if-items !
: build-if-else-then ( dummy-xt handle -- )
    nip
    dup buffer-data-addr >gene-tokens @ >r	( handle  r: offset-index-log )
    >r						( r: offset handle )

    [ log-mask @ ] [IF]
	log-m-more? IF
	    s" build-if-else-then starting..."	0 log
	    s" stack at start: "		log-stack-symbols
	    s" Building 'IF' branch:"		0 log
	THEN
    [THEN]

    [internal'] if  r@  follow-&-add

    max-if-items @ random-ranged   0  BEGIN
	selected-gene-picking r@ follow-&-add
	2dup <> WHILE
	1+
    REPEAT 2drop

    [ log-mask @ ] [IF]			\ conditional compiling
	log-m-much? IF
	    s" Building 'ELSE' branch:" 0 log
	THEN
    [THEN]

    [internal'] else  r@ follow-&-add
    aim 2@ r@ build-new-segment		\ build '[ELSE]' branch

    [internal'] then  r@ follow-&-add

    [ log-mask @ ] [IF]
	log-m-more? IF
	    s" ==> Conditional branching built:"	0 log
	    2r@ buffer-data-addr log-chained-names-tail
	THEN
    [THEN]

    2rdrop ;
' build-if-else-then  internal' g-IF-ELSE-THEN >body >gene-compiled-xt !

\ Nuc bit masks get set by mutation based on masks in the intenals data. 
\ The will either be ORed or set absolutely based on  reset-nuc-masks?
VARIABLE reset-nuc-masks?	reset-nuc-masks? off
: ?set-nuc-masks ( source-body -- )
    dup set-mask? 0= IF  drop EXIT  THEN

    >set-mask0
    my-diversifctn-mask
    reset-nuc-masks? @ IF
	nuc-bitmasks# 0 ?DO
	    over i cells + @  over i cells +  !
	LOOP
    ELSE
	nuc-bitmasks# 0 ?DO
	    over i cells + @  over i cells +  or!
	LOOP
    THEN
    2drop ;

\ Set up everything for trial phase including changes in the actual nuc.
\ Open a eb buffer and set it up
\ Build string to evaluate during trial phase, setup everything.
\ The handled buffer holds the internals data.
: set-up-trial ( internals-handle -- )
    dup >r
    [ decimal ] 64 stringbuf-open >r		( handle  r: handle-code )
    dup buffer-data-addr r@ cat-evaluate-string

    r@ buffered-length  eb-header-length +	\ length of buffer needed 
    allocate	( internals-handle allocated flag  r: code-h )
    IF
	cr ." set-up-trial: Couldn't allocate memory for mutated gene." bell
	drop 2drop \ ############
    ELSE ( internals-handle addr-allocated  r: handle-code )

	swap over eb>internals !	\ store internals handle
	dup wake-me-xt !	( addr-allocated  r: handle-code )

	\ set up entries in the evaluation buffer:
	dup eb>length r@ buffered-length swap ! \ store string length
	dup eb>compiled off		\ not compiled yet
	dup eb>counter 1 swap !		\ init counter to 1

	\ move string there, just after the eb-entries
	eb>sequence	( to )		\ start address of sequence string
	r@ string@	( to from count )
	>r swap r>	( from to count ) move

	\ setting up the nuc
	new-genome-id genome-id !
	nuc-flags @ nuc-on-trial or nuc-flags !
	genome-generation off

	\ copy cost to the nuc-var including cost of ;gene
	frame-cost @ code-cost !

	\ set nuc bitmasks:
	2r@ drop  buffer-data-addr ?set-nuc-masks
    THEN

    r> stringbuf-close rdrop ;

: ?mutation-start-code-entry ( -- )
    code-file-mask @
    dup write-code-file AND 0= IF drop EXIT THEN
    dup file-mutating   AND 0= IF drop EXIT THEN
    file-code  file-structure OR AND IF
	s" \ mutated genome, set on trial:" cat>code-file
	code-file-write-line
    THEN ;

: ?start-definition-code-entry ( -- )
    code-file-mask @
    dup write-code-file AND 0= IF drop EXIT THEN
    dup file-mutating   AND 0= IF
	dup file-structure and 0= IF drop EXIT THEN
    THEN
    file-code file-structure OR AND IF
	s" : "			cat>code-file
	code-file-gene-name	cat>code-file
	s" ( -- ) "		cat>code-file

	file-step&spot&id  code-file-mask @  AND IF
	    s" \ spot:"		cat>code-file
	    spot 2@ num>string	cat>code-file
	    s"  step:"		cat>code-file
	    num>string		cat>code-file
	    s"  mother ID:"	cat>code-file
	    id @ num>string	cat>code-file
	THEN

	code-file-write-line
	1 (code-indent) +!
    THEN ;

: ?end-definition-code-entry ( -- )
    (code-indent) off
    [internal'] ;gene ?code-entry ;

: initialise-mutation ( -- handle-of-internals-data )
    (mutated) off
    (mutation-items) off
    (depth-should-change) off
    ?mutation-start-code-entry
    start-to-follow
    ?start-definition-code-entry
    initialise-buffered-internal ;

: play-segment ( start-item stop-item internals-xt -- )
    >body >gene-start >r	( internals-handle start stop  r: base-addr )
    cells r@ +  swap  cells r> + ?DO
	i @ gene-follow
    cell +LOOP ;

: play-segment-mute ( start-item stop-item internals-xt -- )	\ no code file
    code-file-mask dup dup @ 2>r off
    run-mode dup @ no-code-cost or swap !
    play-segment
    run-mode dup @ [ no-code-cost invert ] literal and swap !
    r> r> ! ;

: play-&-add ( internals-handle start-item stop-item xt -- )
    \ report? #############

    >body >gene-start >r	( internals-handle start stop  r: base-addr )
    cells r@ +  swap  cells r> + ?DO	( internals-handle )
	i @ >r				( handle  r: xt)
	r@ gene-follow
	dup r@ |xt-to-internals-buffer|
	r> [internal'] ;gene = IF	\ special case: end of gene reached?
	    drop unloop EXIT
	THEN
    cell +LOOP				\ loop until stop
    drop ;

: head-play-&-add ( internals-handle u xt -- )   2>r 0 2r> play-&-add ;

: tail-play-&-add ( internals-handle tail-start-index xt -- )
    >r highest-integer# cell / r> play-&-add ;

: log-head-segment-tail ( head-items segment-items xt -- )
    \ check log-mask before calling this word.
    >body >r

    \ Title line:
    s" head=" cat-log  over log-number
    s" , segment=" cat-log  dup log-number
    s" , tail=" cat-log
    2dup +  r@ >gene-tokens @  - negate  log-number
    s" , each as one line:" 0 log-it

    \ Head, segment and tail as a line each:
    0 third r@ log-chained-names			\ head
    2dup over + r@ log-chained-names			\ segment
    +  r@ >gene-tokens @  r> log-chained-names ;	\ tail

: top-level-insertion ( internals-handle internal-xt -- TRUE )
    >r		( internals-handle  r: internal-xt )

    \ Random insertion point:
    r@ >body >gene-tokens @ random-ranged	( internals-handle head-items )

    log-m-some? IF
	s" insertion after item "		cat-log
	dup 					log-number
	s"  of "				cat-log
	r@ >body >gene-tokens @ num>string	0 log-it

	dup 0 r@ log-head-segment-tail

	[ log-mask @ ] [IF]
	    log-m-more? IF

		s" adding and playing head symbols..."	0 log
	    THEN
	[THEN]
    THEN

    \ play-head symbols:		( internals-handle head-items  r: xt )
    2dup r@ head-play-&-add

    \ Build a matching segment with actual stack symbols as aim:
    over  symbols-as-string  rot build-new-segment

    r> tail-play-&-add			\ add tail

    TRUE ;


\ Word to skip a structure and return skipped items.
\ addr points to a chain of xt's,
\ 1'st xt is 'addr start-index cells + @'
\
\ does:	'IF ... ELSE ... THEN's
\ Aborts if called starting at a xt, that unnests a structure.
\ Should not reach ';gene', checks for that.
\ no other structural checks yet.
\
\ If called with a starting xt that does'nt change nesting structure,
\ it returns with index unchanged.
: skip-structures ( start-index addr -- last-skipped-item )
    >r			( start-index             r: addr )
    0 swap		( nesting=0 start-index   r: addr )
    cells		( nesting=0 start-offset  r: addr )

    [ log-mask @ ] [IF]
	log-m-much? IF
	    s" Skipping structures starting:" 0 log
	THEN
    [THEN]

    BEGIN		( current-nesting current-offset  r: addr )
	\ get xt:
	dup r@ + @	( current-nesting current-offset xt r: addr )

	[ log-mask @ ] [IF]
	    log-m-much? IF
		over cell /		log-number
		s"  	"		cat-log
		s" checking "		cat-log
		dup xt>string		cat-log
		s" ... "		cat-log
	    ELSE
		dup xt>string 2drop	\ does NOT work without! ???
		\ on rare occasions there's a crash without xt>string
		\ i have no idea why... ###########
	    THEN
	[ELSE]
	    dup xt>string 2drop	\ does NOT work without! ???
	[THEN]

	CASE	\ on xt		( current-nesting current-offset  r: addr )
	    \ check if it is a structure starting gene:
	    [internal'] if OF
		>r 1+ r>		\ increase nesting level

		[ log-mask @ ] [IF]
		    log-m-much? IF
			s"  We skip from 'IF' to 'THEN' level: "  cat-log
			over num>string				  0 log-it
		    THEN
		[THEN]

	    ENDOF

	    \ check if it is a structure terminating gene:
	    [internal'] then OF

		[ log-mask @ ] [IF]
		    log-m-much? IF
			s"  'THEN' reached, level: "	cat-log
			over num>string			0 log-it
		    THEN
		[THEN]

		>r 1- r>		\ decrease nesting level
		over 0= IF		\ level zero reached?
		    cell+		\ skip last end token too
		THEN
	    ENDOF

	    [internal'] ;gene OF		\ savety check
		cr ." Level: " over .
		true ABORT"  skip-structures: ';gene' reached!"
	    ENDOF

	    
	    \ ordinary xt's

	    [ log-mask @ ] [IF]
		log-m-much? IF
		    s" skipped." 0 log-it
		THEN
	    [THEN]

	ENDCASE	  ( current-nesting current-offset  r: addr )

	over 0= IF			\ zero nesting?
	    nip  cell /	( skipped-items )

	    [ log-mask @ ] [IF]
		log-m-much? IF
		    s" Skipping structures completed: "	cat-log
		    dup					log-number
		    s"  items skipped."			0 log-it
		THEN
	    [THEN]

	    rdrop EXIT
	THEN				\ done

	cell+				\ next item
    AGAIN ;

: check-items-to-replace ( max-items-to-replace start-addr -- items-ok )
    >r			( max-items-to-replace  r: start-addr )

    0	\ count checked items
    BEGIN ( max-items-to-replace  checked-items  r: start-addr )
	2dup > 0= IF nip rdrop EXIT THEN
	dup cells r@ + @	( max-items-to-replace  checked-items xt )

	[ log-mask @ ] [IF]
	    log-m-much? IF
		over			log-number
		s" 	checking "	cat-log
		dup xt>string		cat-log
		s" : "			cat-log
	    THEN
	[THEN]

	CASE
	    [internal'] else OF

		[ log-mask @ ] [IF]
		    log-m-much? IF
			s" we stop at 'ELSE' " 0 log-it
		    THEN
		[THEN]

		nip  rdrop EXIT	\ 'else' is EXcluded
	    ENDOF

	    [internal'] then OF

		[ log-mask @ ] [IF]
		    log-m-much? IF
			s" we stop before 'THEN' " 0 log-it
		    THEN
		[THEN]

		nip  rdrop EXIT	\ 'then' gets EXcluded
	    ENDOF

	    [internal'] if OF	\ Let's skip the whole condition branch
		r@ skip-structures ( start-index addr -- last-skipped-item )
		1-			\ will be added again
	    ENDOF

	    \ default case: ordinary genes
	    [ log-mask @ ] [IF]
		log-m-much? IF
		    s"    	skipped."	0 log-it
		THEN
	    [THEN]

	ENDCASE
	1+
    AGAIN ;

: top-level-replacement ( internals-handle internal-xt -- TRUE )
    >r		 ( internals-handle  r: internal-xt )

    \ Random snippet:
    r@ >body >gene-tokens @ dup
    random-ranged swap random-ranged
    2dup > IF swap THEN		( handle head-items tail-start-item      r: xt)
    over -			( handle head-items max-items-to-replace r: xt)

    log-m-some? IF
	s" replacement after item "		cat-log
	over					log-number
	s"  of "				cat-log
	r@ >body >gene-tokens @ num>string	0 log-it

	log-m-more? IF
	    s" trying to replace up to "	cat-log
	    dup					log-number
	    s"  items. Let's check:"		0 log-it
	THEN

    THEN

    r@ >body 2 pick gene-body>n'th-xt-addr
    ( handle head-items max-items-to-replace start-addr  r: xt )
    check-items-to-replace ( handle head-items items-ok  r: xt )

    log-m-some? IF
	dup IF
	    s" I will replace "	cat-log
	    dup			log-number
	    s"  items."		0 log-it

	    2dup r@ log-head-segment-tail
	    s" (Segment will be replaced)."  0 log
	ELSE
	    s" Zero items to replace, giving up." 0 log
	THEN
    THEN

    \ Give up if segment has length zero:
    dup 0= IF 2drop drop rdrop FALSE EXIT THEN

    \ Add head genes:	( handle head-items segment-items-ok  r: xt )
    third third r@ head-play-&-add

    \ remember frame and stack as start of new segment to be built:
    store-actual-stack-and-frame -rot ( h data-addr head segment-items  r: xt )

    \ play old segments items to get the aim:
    2dup over + r@ play-segment-mute
    symbols-as-string				\ symbols as stack aim
    dup stringbuf-open >r r@ cat		\ preserved in a buffer
    + swap		( h tail-index data-addr  r: xt stack-handle )
    dup restore-stack-and-frame free ( h tail)	\ restore start
    ABORT" top-level-replacement: Couldn't free stored frame data."

    \ Now construct the new segments code:	( handle tail  r: xt stack-h )

    write-code-file  code-file-mask @  AND IF
	file-mutating  code-file-mask @  AND IF
	    file-frames code-file-mask @ AND IF
		indent-code
		s" \ replacement begins" cat>code-file code-file-write-line
	    THEN
	THEN
    THEN

    r@ string@				\ segment's end given as aim

    \ Check if the aim is not already reached:	( h tail addr count r: xt h-st)
    2dup symbols-as-string compare IF	\ aim not reached? build replacement.
	3 pick build-new-segment
    ELSE	\ aim was reached, (no replacement needed).
	2drop
	log-m-some? IF
	    dup IF
		s" stack matches: Segment just snipped."  0 log
	    THEN
	THEN
    THEN
    r> stringbuf-close			\ close intermediate stack buffer

    write-code-file  code-file-mask @  AND IF
	file-mutating  code-file-mask @  AND IF
	    file-frames code-file-mask @ AND IF
		indent-code
		s" \ replacement ended." cat>code-file code-file-write-line
	    THEN
	THEN
    THEN

    r> ( handle tail xt ) tail-play-&-add

    TRUE ;

\ 'follow-item-or-structure' follows next item, or structure.
\ Word to skip a structure and return skipped items.
\ addr points to a chain of xt's,
\ 1'st xt is 'addr start-index cells + @'
\
\ does:	'IF ... ELSE ... THEN's
\ If a unnesting word or ';gene' is encountered it returns items=0
\ no other structural checks yet.
\ Does *not* consume but *change* stack symbols, to be used in a loop.
: follow-item-or-structure ( start-addr current-offset -- a o' followed-items )
    dup 0 2>r	( start-addr current-offset   r: start-offset nesting=0 )
    BEGIN		( addr current-offset   r: start-offset nesting )
	2dup + @	( addr current-offset xt  r: start-offset nesting )

	[ log-mask @ ] [IF]
	    log-m-much? IF
		s" follow-item-or-structure: level="	cat-log
		r@					log-number
		s" 	( "				cat-log
		symbols-as-string			cat-log
		s"  ) 	"				cat-log
		dup xt>string				0 log-it
	    THEN
	[THEN]

	CASE	\ on xt
	    \ check if it is a structure starting gene:
	    [internal'] if OF	r> 1+ >r ENDOF	\ increase nesting level

	    \ check if it is a structure terminating gene:
	    [internal'] then OF
		r> 1- >r			\ decrease nesting level
		r@ CASE \ on nesting
		    -1 OF			\ 'THEN' without 'IF'
			2rdrop FALSE EXIT	\ no luck
		    ENDOF
		ENDCASE
	    ENDOF

	    \ check if it is a structure dividing gene:
	    [internal'] else OF
		r@ 0= IF			\ level zero?
		    2rdrop FALSE EXIT		\ 'ELSE' without 'IF'
		THEN
	    ENDOF

	    [internal'] ;gene OF ( addr current-offset r: start-offset nesting)
		2r> nip		( addr current-offset nesting )
		dup IF
		    cr
		    ." follow-item-or-structure: ';gene' encountered on level "
		    . cr
		    ABORT" follow-item-or-structure: gave up..."
		ELSE drop THEN
		FALSE EXIT 
	    ENDOF
	ENDCASE

	2dup + @ gene-follow-mute	\ follow
	cell+				\ one item done

	\ check nesting:
	r@ 0=	( addr current-offset flag  r: start-offset nesting )
    UNTIL				\ done if nesting is zero

    dup 2r> drop	( addr current-offset current-offset start-offset )
    - cell / ;	( addr current-offset followed-items )

\ word to follow a chain of xt's until stack reaches the given aim,
\ or give up, if ';gene' is reached, or enter level structure is left.
: next-stack-match ( start-addr addr-aim count-aim -- snipped-items )
    2>r			( start-addr   r: addr-aim count-aim )
    0			( start-addr offset=0  r: addr-aim count-aim )
    BEGIN		( start-addr current-offset  r: addr-aim count-aim )
	\ follow current item or structure increasing current-offset:
	follow-item-or-structure   ( start-addr current-offset followed-items) 
	0= IF				\ ';gene' or leaving entering level

	    [ log-mask @ ] [IF]
		log-m-more? IF
		    s" next-stack-match: no match found till end."
		    0 log
		THEN
	    [THEN]

	    2drop 2rdrop FALSE EXIT	\ no luck
	THEN

	2r@ symbols-as-string compare 0=	\ stack match?
    UNTIL		( start-addr current-offset  r: addr-aim count-aim )
    2rdrop nip cell /	( followed-items )

    [ log-mask @ ] [IF]
	log-m-more? IF
	    s" next-stack-match: next stack match after "	cat-log
	    dup							log-number
	    s"  tokens."					0 log-it
	THEN
    [THEN]
;

\ Search all matches in structure and stack given as 'addr-aim count-aim'.
\ 'addr' points into a xt chain.
\ Return the handle of a buffer with addresses of the matches and their number.
\ Please *close the buffer* in the calling word.
: all-stack-matches ( start-addr addr-aim count-aim -- handle matches# )
    2>r				( start-addr   r: addr-aim count-aim )
    [ decimal ]
    1024 stringbuf-open swap	( handle start-addr   r: addr-aim count-aim )
    BEGIN
	dup 2r@ next-stack-match ( handle start-addr snipped-items   r: a u )
    dup WHILE
	cells +
	over >r sp@ cell r> cat		\ cat address of match to buffer
    REPEAT
    2drop 2rdrop		( handle   r:-- )
    dup buffered-length cell /	( handle matches# )

    [ log-mask @ ] [IF]
	log-m-more? IF
	    s" all-stack-matches: found "	cat-log
	    dup					log-number
	    s"  matches."			0 log-it
	THEN
    [THEN] ;	( handle matches# )	\ please close buffer..

\ snip out a random sequence that does not harm structure or stack matching
\ and omit it
0 CONSTANT random-snip
1 CONSTANT short-snip
2 CONSTANT long-snip
VARIABLE (snip-type)		random-snip (snip-type) !
\
: top-level-snip ( handle xt -- u )	\ returns number of snipped items
    >r		( internals-handle  r: internal-xt )

    r@ >body >gene-start		( h addr-of-xt-chain  r: internal-xt )

    r@ >body >gene-tokens @
    dup 2 < IF				\ nothing to snip?
	s" top-level-snip: There's nothing to skip." log-m-some log
	2drop drop rdrop FALSE EXIT		\ nothing to snip, done
    THEN

    random-ranged			( h addr start-token  r: internal-xt )

    log-m-some? IF
	s" top-snip type "			cat-log
	(snip-type) @ CASE
	    random-snip	OF s" random"	ENDOF
	    short-snip	OF s" short"	ENDOF
	    long-snip	OF s" long"	ENDOF
	ENDCASE					cat-log
	s"  from: "				cat-log
	r@ xt>string				cat-log
	s"  items: "				cat-log
	r@ >body >gene-tokens @ num>string	0 log-it
	dup >r s" try snipping after item " r>	0 log-string-and-number

	[ log-mask @ ] [IF]
	    log-m-more? IF
		s" following head genes to get stack and frame:" 0 log
	    THEN
	[THEN]

    THEN			( h addr start-token  r: internal-xt )

    0 over r@ play-segment-mute			\ get stack after head

    [ log-mask @ ] [IF]
	log-m-more? IF
	    s" top-level-snip: Stack at start of snip is "	cat-log
	    symbols-as-string					0 log-it
	THEN
    [THEN]

    \ seek stack matches
    2dup cells +	( handle start head-items first-snip-addr  r: xt )
    symbols-as-string				\ symbols as stack aim
    dup stringbuf-open >r r@ cat		\ preserved in a buffer
    r@ string@		( h start head-items snip-addr addr-aim count-aim )
    (snip-type) @ short-snip = IF		\ we need only first match
	next-stack-match	( start head-items followed-items )
    ELSE					\ we need all matches
	all-stack-matches	( h start head handle matches# ) \ opens buffer
	dup IF					\ matches found
	    \ what type?
	    (snip-type) @ random-snip = IF	\ random match
		random-ranged
	    ELSE 1-				\ longest match
	    THEN		( h start head handle selected-match# )

	    [ log-mask @ ] [IF]
		log-m-much? IF
		    s" I take match "	cat-log
		    dup num>string	0 log-it
		THEN
	    [THEN]		( h start head handle selected-match# )

	    over buffer-data-addr	( h start head handle match# data-addr)
	    swap cells + @		( h start head# h2 address-of-match)
	    swap stringbuf-close	\ close buffer from 'all-stack-matches'
	    \	( start head# address-of-match)
	    >r 2dup cells + r> swap - cell / ( h start head# snipped-items)
	ELSE				\ no match found
	    ( h start head handle 0 ) swap stringbuf-close \ close buffer
	THEN
    THEN	( handle start head-items snipped-items  r: xt stack-handle )

    r> stringbuf-close				\ close stack buffer

    rot drop	( handle head-items snipped-items  r: xt )
    dup IF					\ match found?

	log-m-some? IF
	    dup			log-number
	    s"  items snipped."	0 log-it
	THEN

	log-m-some? IF
	    s" (Segment gets snipped)."  0 log
	    2dup r@ log-head-segment-tail
	THEN

	( handle head-items snipped-items  r: xt )
	clear-symbols
	1 (code-indent) !
	third third r@ head-play-&-add		\ add head

	write-code-file  code-file-mask @  AND IF
	    file-mutating  code-file-mask @  AND IF
		file-frames code-file-mask @ AND IF
		    indent-code
		    s" \ "			cat>code-file
		    dup num>string		cat>code-file
		    s"  words snipped."	cat>code-file  code-file-write-line
		THEN
	    THEN
	THEN

	dup >r -rot r> ( snipped-items handle head-items snipped-items  r: xt )
	+ r> tail-play-&-add	( snipped-items  r: -- )
    ELSE					\ no match found
	2drop drop rdrop FALSE
    THEN ;

: top-level-random-snip ( xt -- u )  random-snip (snip-type) ! top-level-snip ;
: top-level-short-snip ( xt -- u )   short-snip (snip-type) !  top-level-snip ;
: top-level-long-snip ( xt -- u )    long-snip (snip-type) !   top-level-snip ;


\ Simplifying structures IF-ELSE-THEN and IF-THEN
\ Words needed for 'top-level-snip-IF-ELSE-branch' mutation type.

\ I use a list of the structure relevant gene position inside a genome.
\ A gene sequence (i.e. a genome) can be read in storing (only) structure
\ relevant words like IF ELSE THEN, the index into the sequence and nesting
\ level. I call it a structure list.
\ It could be used for many other porposes.

\ offsets inside a node
0
OFFSET: >index-in-gene-sequence
OFFSET: >gene-xt
OFFSET: >nesting-level
CELL / CONSTANT (gene-struct-list-size)

\ Put a gene in the list (used inside a loop)
: gene>structure-list ( list nesting token-xt token-index -- list nesting )
    fourth new-node >r	( list nesting token-xt token-index  r: new-node )
    r@ >index-in-gene-sequence !
    r@ >gene-xt !
    dup r> >nesting-level ! ;

\ Generate structure list of a genome
: build-structure-list ( internals-xt -- list )
    (gene-struct-list-size) deflist  0		( internals-xt list nesting )

    over empty-list
    third >body >gene-tokens @ 0 ?DO		( internals-xt list nesting )
	third i gene-n'th-xt-addr @ CASE
	    [internal'] if OF
		[internal'] if  i gene>structure-list
		1+
	    ENDOF
	    [internal'] else OF
		1-
		[internal'] else  i gene>structure-list
		1+
	    ENDOF
	    [internal'] then OF
	        1-
	        [internal'] then  i gene>structure-list
	    ENDOF
	    [internal'] ;gene OF
		1-
	    ENDOF
	ENDCASE
    LOOP

    -1 <> ABORT" build-structure-list: unstructured genome."
    nip ;

\ Count occurances of xt in a structure list
: count-xt ( xt list -- count )
    2>r 0 2r>
    dup nodes 0 ?DO	( count xt node )
	next-node
	dup >gene-xt @  third = IF
	    2>r 1+ 2r>
	THEN
    LOOP
    2drop ;


false [IF] \ testing
: .structure-list ( list -- )
    page
    dup nodes 0 ?DO
	cr
	next-node
	dup >index-in-gene-sequence @ ." index: " .
	dup >gene-xt @ ." 	" xt>string type
	dup >nesting-level @ ." 	nesting: " .
    LOOP
    drop ;
[THEN]


\ Return list-index of n'th occurance of xt in the list
: (n'th-occurance) ( u xt list -- if-list-index )
    >r swap	( xt u  r: list )

    0 swap r@	( xt current-list-index searched-if-index list  r: list )
    r> nodes 0 DO	( xt current-list-index searched-if-index list )
	fourth i third n'th-node >gene-xt @ = IF
	    swap dup 0= IF
		2drop 2drop
		i UNLOOP EXIT
	    THEN
	    1- swap
	THEN
    LOOP

    2drop 2drop
    cr bell ." (n'th-occurance): Found no " xt>string type ABORT ;

\ Return list-index of next occurance of xt of a given level
: (next-xt-on-level) ( xt level list start -- list-index true | false )
    over nodes swap DO	( xt level list )
	i over n'th-node >gene-xt @  fourth = IF
	    i over n'th-node >nesting-level @  third = IF
		drop 2drop
		i TRUE UNLOOP EXIT
	    THEN
	THEN
    LOOP

    drop 2drop
    FALSE ;

\ Return all three LIST-indices of an IF-ELSE-THEN structure
\ If no ELSE is present, the ELSE index is zero
: find-n'th-if-structure ( u list -- THEN-list-index ELSE-li-index IF-li-index)
    >r

    [internal'] if r@ (n'th-occurance)		( n'th-if-index  r: list )
    dup r@ n'th-node >nesting-level @		( if-index nesting r: list )

    \ remember list and start index (just next after 'IF')
    r> third 1+ 2>r			( if-index nesting  r:list start-index)
	
    [internal'] else  over  2r@  (next-xt-on-level)
    0= IF  FALSE  THEN    swap		( if-i else-i=flag nesting r: list )

    [internal'] then  over  2r>  (next-xt-on-level)
    0= ABORT" find-n'th-if-structure: Unstructured (no 'THEN')."

    nip	( if-index else-index then-index )

    \ check if the right 'ELSE' was found
    \ (when the 'IF' had no 'ELSE' a later one could be found).
    2dup < 0= IF nip FALSE swap THEN

    swap rot ; 

\ Same, but returning *GENOME*-indices
\ If there is no ELSE the ELSE-index is zero.
: find-n'th-if-in-genome ( u list -- THEN-genome-index ELSE-g-index IF-g-index)
    dup >r
    find-n'th-if-structure
    r@ n'th-node >index-in-gene-sequence @ rot
    r@ n'th-node >index-in-gene-sequence @ rot
    r> n'th-node >index-in-gene-sequence @ rot ;

\ top-level-snip-IF-ELSE-branch just drops the condition flag with drop(C-)
\ The elimination of the flag producing code is left to other (snip) types.
internal'? drop(C-) 0= [IF]
    \ top-level-snip-IF-ELSE-branch needs drop(C-) to skip conditional branches
    s" C-" ' drop  GENE-ALIAS: drop(C-)
    0 to-gene-pool' drop(C-)
    \ as-alternative'' drop(C-) drop
[THEN]

\ Drop condition flag of an IF-THEN or IF-ELSE-THEN clause and execute one of
\ the branches unconditionally, snipping the other.
\ In case of an IF-THEN branch drop condition flag and either do the IF branch
\ unconditionally or snip it out of the genome.
: top-level-snip-IF-ELSE-branch ( handle xt -- flag )
    >r	( internals-handle  r: internal-xt )

    \ Only when there is at least one IF structure
    r@  [internal'] if  genome-has-gene? 0= IF
	s" top-level-snip-IF-ELSE-branch: No conditional branch found."
	log-m-some log
	drop rdrop FALSE EXIT
    THEN

    \ build a list with IF-ELSE-THEN structures
    r@ build-structure-list		( handle list  r: internal-xt )
    
    \ randomly select one IF-ELSE-THEN structure
    [internal'] if  over count-xt
    random-ranged ( handle list if-number  r: xt )

    \ find the 3 indices in genome sequence
    over find-n'th-if-in-genome	( h list THEN-index ELSE-i IF-i  r: xt)

    2>r >r
    remove-list
    r> 2r>				( h THEN-index ELSE-i IF-i  r: xt)

    log-m-some? IF
	s" top-level-snip-IF-ELSE-branch positions: "	cat-log
	s" IF: "					cat-log
	dup						log-number
	over IF
	    s"   ELSE: "				cat-log
	    over					log-number
	ELSE
	    s"   (no 'ELSE')"				cat-log
	THEN
	s"   THEN: "					cat-log
	third						log-number
	s" "						0 log-it
    THEN

    fourth  over  r@ head-play-&-add
    [internal'] drop(C-)  fifth  follow-&-add

    \ decide what to do (skipping 'IF' or 'ELSE' branch?)
    \ leave start-item stop-item on stack
    \					( h THEN-index ELSE-i IF-i  r: xt)
    2 random-ranged IF	\ keep 'IF' branch (unconditionally)

	log-m-some? IF
	    s" Executing 'IF' branch unconditionally."	0 log-it
	THEN

	dup 1+		\ start item
	third dup 0= IF
	    drop fourth
	THEN		\ stop item

    ELSE		\ keep 'ELSE' branch, if there's one

	log-m-some? IF
	    over IF
		s" Executing 'ELSE' branch unconditionally."
	    ELSE
		s" 'IF' branch deleted, 'ELSE' branch does not exist."
	    THEN
	    0 log-it
	THEN

	over IF
	    over 1+	\ start item
	ELSE
	    third 1+	\ start item
	THEN
	fourth		\ stop item
    THEN	( h THEN-index ELSE-i IF-i start stop  r: xt)

    \ add 'IF' or 'ELSE' branch
    2>r fourth 2r> r@ play-&-add	( h THEN-index ELSE-i IF-i  r: xt)
    2drop				( h THEN-index  r: xt)

    log-m-some? IF
	s" Adding remaining sequence tail from position "	cat-log
	dup 1+							log-number
	s" "							0 log-it
    THEN

    1+ r> tail-play-&-add
    TRUE ;


: do-address-replacement ( internals-handle token internals-xt  -- )
    \ no checks done here.
    >r			( handle token  r: internals-xt )

    log-m-some? IF
	s" replacing token "		cat-log
	dup				log-number
	s"  (replacing segment)."	0 log-it

	dup 1 r@ log-head-segment-tail
    THEN

    2dup r@ head-play-&-add

    BEGIN			( handle token  r: internals-xt )
	random-gene-picking
	dup >body >gene-flags @ out-address and IF
	    dup >body
	    dup >gene-stack-in @ 0=
	    swap >gene-stack-out 2@ s" a" compare 0= AND
	    dup 0= IF nip THEN	    
	ELSE drop false THEN
    UNTIL	( handle token replacement-xt   r: internals-xt )

    log-m-some? IF
	s" replacing by "	cat-log
	dup xt>string		0 log-it
    THEN

    third follow-&-add		( handle token  r: internals-xt )

    1+ r> tail-play-&-add ;

: top-level-address-replacemnt ( internals-handle internal-xt -- flag )
    >r					( internals-handle  r: internal-xt )

    r@ >body >gene-tokens @		( handle tokens   r: internal-xt )
    
    dup 2 < IF
	s" top-level-address-replacemnt: ';gene' only." log-m-type log
	2drop rdrop FALSE EXIT		\ nothing to do, done
    THEN

				( handle tokens   r: internal-xt )
    random-ranged		( handle random-token   r: internal-xt )
    r@ >body >gene-start	( handle random-token start-addr     r: i-xt )
    >r				( handle random-t    r: i-xt start-addr)
    \ search from random token to the end
    dup
    BEGIN			( h random-t actual-t	r: i-xt start-addr)
	dup cells r@ + @	( h random-t actual-t act-xt  r: xt start-a )
	dup [internal'] ;gene <>
    WHILE			( h random-t actual-t act-xt  r: xt start-a )
	>body			( h random-t actual-t body  r: xt start-addr)
	dup >gene-flags @ out-address and IF
	    dup >gene-stack-in @ 0=
	    ( handle random-t actual-token body flag-zero-in r: xt start-addr)
	    swap >gene-stack-out 2@ s" a" compare 0= AND IF
		nip rdrop r> do-address-replacement	\ address giver found
		TRUE EXIT
	    THEN
	ELSE drop
	THEN
	1+			( h random-t actual-token   r: xt start-addr)
    REPEAT
    2drop			( h random-token   r: internal-xt start-addr )

    \ no address giver from random token 'til ;gene
    \ so we search from start to random
    0				( h random-token 0       r: xt start-addr )
    BEGIN			( h end-token actual-token    r: xt start-addr)
	2dup >
    WHILE
	dup cells r@ + @	( h end-token actual-token xt r: xt start-addr)
	dup >body >gene-flags @ out-address and IF
	    dup >body	( h end-token actual-token xt body r: xt start-a)
	    dup >gene-stack-in @ 0=
	    ( end-token actual-token xt body zero-in-flag   r: xt start-a)
	    swap >gene-stack-out 2@ s" a" compare 0= AND IF		\ found
		( h end-token actual-token xt   r: xt start-address )
		drop nip  rdrop r>  do-address-replacement
		TRUE EXIT
	    THEN
	THEN
	drop			( h end-token actual-token   r: xt start-addr )
	1+
    REPEAT 2rdrop 2drop	drop	( -- )

    s" no address found."	log-m-type log-it

    FALSE ;

\ Test functions to get a flag based on certain internals data.
\ Can be used on chained or pooled internal xt's.
: replacable? ( internals-body -- flag )
    >gene-flags @
    [ frame-pushing frame-popping special or or ] literal and 0= ;
    \ I don't know if checking 'special' will always be appropriate,
    \ but currently it is (could be omitted, probably).


\ Search chained xt's from start-item to stop-item (excluding) for the
\ first internal to fulfill the given test.    
: search-chain ( test-xt internals-body stop start-index -- item true | false )
    ?DO   ( test-xt internals-body )
	dup i gene-body>n'th-xt-addr @ >body third EXECUTE	\ test item
	dup IF
	    >r 2drop r>  i swap unloop  ( -- item result ) EXIT	\ found.
	ELSE drop THEN
    LOOP
    2drop FALSE ;


\ The following three words look for a successfull test in an xt chain:

: search-tail ( test-xt internals-body start-index -- item true | false )
    over >gene-tokens @ swap search-chain ;

: search-head ( test-xt internals-body stop -- item true | false )
    0 search-chain ;

\ Search from start-index to end, and from begin to start-index:
: cyclic-search ( internals-body start-index test-xt -- item true | false )
    -rot  2>r		( test-xt   r: internals-body start-index )

    dup 2r@ search-tail dup IF
	2rdrop rot drop EXIT
    ELSE drop THEN

    2r> search-head ;

: (do-token-replacement) ( handle replacement-xt token internals-xt -- )
    >r		( handle replacement-xt token   r: internals-xt )

    log-m-some? IF
	s" replacing token "		cat-log
	dup				log-number
	s" . Replacing segment by "	cat-log
	over xt>string			0 log-it
	dup 1 r@ log-head-segment-tail
    THEN

    ( handle replacement-xt token  r: internals-xt )
    third over r@ head-play-&-add
    swap third follow-&-add
    1+ r> tail-play-&-add ;

: do-token-replacement ( handle token internals-xt  -- flag ) \ no checks
    >r				( handle token  r: internals-xt )

    dup cells r@ >body >gene-start + @	( handle token xt    r: internals-xt )
    >body				( handle token body  r: internals-xt )
    dup >gene-stack-in 2@ 2>r ( h token body  r: internals-xt addr-in count-in)
    >gene-stack-out 2@ 2r>    ( h tok a-out c-out addr-in count-in  r: ints-xt)
    10000 0 DO		\ try it many times
	random-gene-picking >r	( h tok a-out c-out a-in c-in  r: i-xt xt' )

	r@ >body >r		( h tok a-out c-out a-in c-in  r: i-xt xt' bod)
	2dup r@ >gene-stack-in 2@ compare 0= IF	  \ stack-in match?
	    2over r@ >gene-stack-out 2@ compare 0= IF \ stack-out match?
		\ no gene building pseudo genes allowed here, too complicated!
		2r@ drop >body >gene-flags @ building and 0= IF
		    2drop 2drop rdrop r> swap	( h r xt' token r: xt loop )
		    unloop r> (do-token-replacement) TRUE EXIT
		THEN
	    THEN
	THEN
	2rdrop
    LOOP
    2drop 2drop 2drop  2rdrop

    s" no replacement found. "	log-m-some log-it

    FALSE ;

: top-level-token-replace ( internals-handle internals-xt -- flag )
    >r					( internals-handle  r: internal-xt )

    r@ >body dup >gene-tokens @		( handle body tokens   r: internal-xt )

    dup 2 < IF
	log-m-some? IF
	    s" top-level-token-replace: ';gene' only." 0 log
	THEN

	2drop drop rdrop FALSE EXIT		\ nothing to do, done
    THEN

				( handle body tokens   r: internal-xt )
    random-ranged		( handle body random-token   r: internal-xt )
    ['] replacable? cyclic-search ( handle item true | handle false  r: xt )
    0= IF		\ no replacable item.
	rdrop drop
	s" no replacable item found."		log-m-some log-it
	FALSE EXIT
    THEN	( handle item  r: xt )

    r> do-token-replacement ;

VARIABLE mutations					\ counter

\ Word that ignores the current genome, but builds a fresh one from scatch.
\ Give new designs a chance to pop up in a later state of an evolution.
\ Used to let new genes pop up sporadically just like in the very beginning
\ of an evolution.
: restart-from-scratch ( internals-handle dummy-xt -- flag )
    drop	\ dummy xt to let it look like the other mutation types

    s" restart-from-scratch: insert into empty gene: " log-m-type log

    1 mutations +!
    genome-generation off
    [internal'] noop  top-level-insertion ;

: ?code-compiled-comment ( -- )
    code-file-mask @ >r
    write-code-file r@ AND 0= IF rdrop EXIT THEN
    file-mutating   r@ AND 0= IF rdrop EXIT THEN
    file-code	    r@ AND 0= IF rdrop EXIT THEN
    rdrop

    s" \ genome is compiled. No trial phase." cat>code-file
    code-file-write-line ;

: rebirth ( internals-handle internals-xt -- flag=true )
    2drop

    current-genome-pool-xt @ EXECUTE pick-one @		( new-internals-xt )
    dup setup-wake-me
    log-m-some? IF
	s" rebirth picked: "	cat-log
	dup xt>string		0 log-it
    THEN

    ?code-entry						( -- )
    ?end-definition-code-entry
    ?code-compiled-comment

    compiled!
    1 (mutated) !
    true ;

2VARIABLE mutation-rate		1 100 mutation-rate 2!	\ how often to mutate

: MUTATION-TYPE-POOL: ( initial-items )   1 PROBABILITY-LIST: ;

decimal
4 MUTATION-TYPE-POOL: snip-types	\ sub list for snip types
1000 ' top-level-random-snip	snip-types set-one
1000 ' top-level-short-snip	snip-types set-one
1000 ' top-level-long-snip	snip-types set-one
1000 ' top-level-snip-IF-ELSE-branch    snip-types set-one

16 MUTATION-TYPE-POOL: mutation-types
1000 ' top-level-insertion mutation-types set-one
1000 ' top-level-replacement mutation-types set-one
3000 ' snip-types mutation-types set-as-sublist
1000 ' top-level-address-replacemnt mutation-types set-one
1000 ' top-level-token-replace mutation-types set-one
1000 ' restart-from-scratch mutation-types set-one
100  ' rebirth mutation-types set-one

: stack-data-buffered! ( internals-handle -- )
    buffer-data-addr
    initial 2@ string>stack-in
    symbols-as-string string>stack-out ;

: ?aborted>code-file ( -- )
    code-file-mask @
    dup write-code-file AND 0=	IF drop EXIT THEN
    file-mutating   AND 0=	IF	EXIT THEN

    s" \ mutation ABORTED." cat>code-file code-file-write-line
    code-file-write-line ;

: (mutate) ( internals-xt -- new-internals-handle )
    BEGIN
	initialise-mutation swap	( internals-handle internal-xt )
	2dup

	dup >body >gene-tokens @ 2 < IF
	    ['] top-level-insertion
	ELSE
	    \ Select a random type based on probability lists:
	    mutation-types pick-one @
	THEN	( handle mother-xt handle mother-xt mutation-type-xt )

	\ Log:
	log-m-type? IF
	    dup xt>string 2dup cat-log  s"  STARTING...	" cat-log  0 log-it
	THEN

	dup ?mutation-type>code-file

	s" TOP" push-frame
	run-mode dup @ mutating or swap !	\ set run mode on mutating
	EXECUTE							\ try to mutate
	run-mode dup @ [ mutating invert ] literal and swap !	\ mutating off
	pop-frame
    0= WHILE
	?aborted>code-file
	swap stringbuf-close
    REPEAT
    drop

    1 mutations +!
    genome-generation off

    compiled? IF  ( -- new-internals-handle ) EXIT  THEN
    dup stack-data-buffered! ;  ( -- new-internals-handle )


VARIABLE (open-buffers)
VARIABLE (exceeding-size-ring)	(exceeding-size-ring) on

: mutate ( internal-xt -- new-internals-data-handle )
 
    log-m-some? IF
	s" Mother genome: "	cat-log
	dup xt>string		0 log-it
	[ decimal ] 128 stringbuf-open >r
	dup >body		r@ cat-gene-string
	r@ string@		log-m-some log
	r> stringbuf-close
    THEN

[ TRUE ] [IF] \ normal use: CATCH too long mutations

    BEGIN  \ build mutation
	opened-buffers-to-list (open-buffers) !
	dup ['] (mutate) CATCH	( internals-handle 0 | trow-code )
	dup IF
	    dup |genome-too-long = IF
		nip
		(open-buffers) @ close-not-listed-buffers

		s" Mutation length exceeded 'mutation-max-ollowed-items'."
		log-m-type? IF
		    2dup 0 log
		    s" MUTATION ABORTED." 0 log
		THEN

		playing-bench? 0= IF
		    (exceeding-size-ring) @ IF bell THEN
		    at? 2>r
		    2dup last-left type clear-line-to-end
		    2r> at-xy
		THEN
		8 >message

		?aborted>code-file

	    ELSE THROW THEN
	THEN
	0=
    UNTIL	( xt internals-handle )
    nip
    (open-buffers) @ remove-list

[ELSE] \ debugging: give better error messages
    (mutate)
[THEN]

    write-code-file  code-file-mask @  AND IF
	file-mutating file-structure OR  code-file-mask @  AND IF
	    file-code file-structure OR  code-file-mask @  AND IF
		code-file-write-line			\ add an empty line
	    THEN
	THEN
    THEN

    compiled? IF  EXIT  THEN    \ mutated genome is already compiled (rebirth)

    log-mutation? IF		\ log genome and code cost
	s" ==> new genome built:"	0 log
	dup buffer-data-addr internals-string ( internal-h handle-genome-words)
	dup string@			0 log
	stringbuf-close
	log-m-some? IF
	    s" code-cost: "		cat-log
	    code-cost @ num>string	0 log-it
	THEN
    THEN

    (mutated) @ (mutated-max) @ > IF
	(mutated) @ (mutated-max) !

	[ log-mask @ ] [IF]
	    log-m-more? IF
		s" new maximal mutated gene items= "
		(mutated) @	0 log-string-and-number
	    THEN
	[THEN]
    THEN
;

: mutate-nuc ( -- )
\   on-trial? IF EXIT THEN	\ trial time: no mutation

    \ Log *before* mutation:
    log-mutation? IF
	s" "				0 log	\ empty line
	log-cat-step&spot
	s" MUTATION based on "	cat-log
	log-cat-id
	s" to GI:"			cat-log
	(genome-id) @ 1+ num>string	0 log-it

	log-m-some? IF
	    s" total items: "					cat-log
	    wake-me-internal @ >body >gene-tokens @ num>string	0 log-it
	THEN
    THEN

    wake-me-internal @ mutate

    mutation-must-differ @ IF	\ check for unaltered genome
	\ mutated string of xt's
	dup buffer-data-addr  dup >gene-tokens @ cells >r  >gene-start  r> 
	\ unaltered string of xt's
	wake-me-internal @ >body  dup >gene-tokens @ cells >r  >gene-start  r>
	compare 0= IF
	    log-mutation? IF
		s" MUTATION:  produced unchanged genome. Refused."  0 log-it
	    THEN
	    stringbuf-close	\ close unused internals buffer
	    EXIT
	THEN
    THEN

    compiled? IF
	run-mode dup @ [ compiled invert ] literal and swap !
	stringbuf-close		\ close unused internals buffer
    ELSE
	set-up-trial
    THEN

    \ Log *after* mutation:
    (mutated) @ IF	\ don't log if not mutated (if this happens).
	log-mutation? IF
	    log-cat-step&spot
	    s" MUTATION:  Nuc mutated to child at "	cat-log
	    child-spot @ num>string			cat-log
	    s"  as ID:"					cat-log
	    (id) @ 1+ num>string			cat-log \ next ID
	    s"  GI:"					cat-log
	    genome-id @ num>string			0 log-it
	    s" "					0 log	\ empty line
	THEN
    THEN ;

: mutate? ( -- )			\ shall we mutate?
    on-trial? IF EXIT THEN		\ trial time: no mutation

    mutation-rate cell+ @ IF
	mutation-rate rated-flag IF
	    mutate-nuc			\ do mutation on the nuc
	THEN
    THEN ;
DEFER <mutate>
' mutate? IS <mutate>


\ output the code of xt to the code file:
: xt-write-code ( internals-xt -- )
    run-mode dup @ writing-code or swap !
    1 (code-indent) !
    s" : "		cat>code-file
    dup xt>string	cat>code-file
    s"  ( -- ) "	cat>code-file	code-file-write-line

    \ the ingredients:	( internals-xt )
    >body >r
    r@ >gene-start  dup r> >gene-tokens @ cells +  swap ?DO
	i @ gene-follow
    cell +LOOP

    code-file-write-line		\ trailing empty line.
    run-mode dup @ writing-code invert and swap ! ;


\ how many generations is trial phase?
VARIABLE trial-phase	10 trial-phase !

decimal
36 STRINGBUF-HANDLE: (get-xt)

\ Compile genes on trial and put them into the gene pool
: end-trial-phase ( -- )
    wake-me-xt @ >r			( r: eb-buffer-addr )
    log-cat-id  s" end of trial phase " log-trial log-it

    \ Check if genome got already compiled:
    r@ eb>compiled @ dup IF		\ buffer already compiled?
	nuc-flags dup @ nuc-on-trial xor swap ! \ end trial phase
	wake-me-xt @ decrease-eb-count		\ decrease counter
	wake-me-xt !				\ set wake-me-xt in the nuc
	r@ eb>internals @ wake-me-internal !	\ set wake-me-internal in nuc
	s" xt's set to compiled gene and internals" log-trial log
	rdrop EXIT				\ EXIT nothing else to do
    ELSE drop THEN

    \ Compile internals word and close internals buffer:
    r@ eb>internals @				( internals-handle  r: eb )
    dup new-gene-name compile-buffered-internals ( handle internal-xt  r: eb )
    swap stringbuf-close			( internal-xt  r: eb )
    dup r@ eb>internals !			\ set internals xt in eb
    dup wake-me-internal !			\ set internal xt of the nuc
    1 compiled-genes +!				\ bookkeeping

    \ Compile gene word and get it's xt:
    dup compile-gene
    dup xt>string  also genes  get-xt  previous	( i-xt gene-xt  r: addr-eb-buf)
    dup r@ eb>compiled !			\ xt marking buffer as compiled
    dup wake-me-xt !				\ set wake-me-xt in the nuc
    \ store gene xt in internals word:
    >r  dup >body  r> gene-xt!!		( internals-xt  r: addr-eb-buffer )

    r> decrease-eb-count			\ decrease counter
    nuc-flags dup @ nuc-on-trial xor swap !	\ end trial phase


\	last-gene-into-pool

    log-trial? IF
	s" compiled as: "	cat-log
	(name-buf) string@	log-trial log-it
    THEN

    write-code-file  code-file-mask @  AND IF
	file-end-trial code-file-mask @ AND IF
	    s" \ survived trial phase:" cat>code-file code-file-write-line
	    dup xt-write-code
	THEN
    THEN
    drop ;
