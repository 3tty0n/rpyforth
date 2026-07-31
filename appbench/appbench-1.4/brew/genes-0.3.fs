\ genes-0.3.fs

: genes-version ( -- addr count )
    cvs" 	$Id: genes-0.3.fs,v 1.22 2005/06/02 15:17:07 f Exp $	" ;


\ Genes are ordinary Forth words.
\ They have an associated word in gene-internals though, which contains info
\ needed mostly at mutation time.

\ This version builds the internal gene data prior to gene compiling.
\ All string representations of the gene, like evaluate string,
\ compile string and code string can be constructed from the internals
\ data.

\ I dropped my plans to change the string evaluating during trial phase.
\ So genes are still evaluated as strings during trial.

\ The internal data is kept in a string buffer during trial.

decimal


\ Gene internals data structure:
\ (in a compiled internals word it starts at pfa)
0
OFFSET: >gene-flags	( addr -- addr' )	\ flags
OFFSET: >gene-stack-in	( addr -- addr' )	\ count of input string
cell+						\ address of input string
OFFSET: >gene-stack-out	( addr -- addr' )	\ count of output string
cell+						\ address of output string
OFFSET: >gene-compiled-xt ( addr -- addr' )	\ xt of executed gene word
						\ or xt of special gene con-
						\ structing word if 'special'
						\ flag is set
OFFSET: >gene-evaluated-xt ( addr -- addr' )	\ xt of evaluated word
OFFSET: >gene-follow-xt ( addr -- addr' )	\ 0|xt of gene-follow word
OFFSET: >gene-cost	( addr -- addr' )	\ sum of costs of all sub genes
OFFSET: >gene-tokens	( addr -- addr' )	\ number of xt's, literals...

OFFSET: >set-mask0	( addr -- addr' )	\ set my-diversifctn-mask
nuc-bitmasks# 1- cells +			\ other nuc intern masks
\ OFFSET: >gene-used	( addr -- addr' )	\ usage counter possible
OFFSET: >gene-start	( addr -- addr' )	\ the xt array starts here
cell - CONSTANT gene-descriptor-length#		\ gene-start not counted

\ masks for gene-flags:
LIST: gene-flags
gene-flags 0	( list bit-position )
LISTED-MASK: in-address		\ in tos is address
LISTED-MASK: out-address	\ out tos is address
LISTED-MASK: in-df-address      \ in tos is dfloat address
LISTED-MASK: out-df-address     \ out tos is dfloat address
LISTED-MASK: primitive		\ is it a gene primitive?
LISTED-MASK: set-mask		\ does it set bitmasks?
LISTED-MASK: frame-pushing	\ gene pushes a frame
LISTED-MASK: frame-popping	\ gene pops a frame
LISTED-MASK: building		\ not a real gene, but a gene building word
LISTED-MASK: special		\ special 'gene-follow' behaviour
2drop

VOCABULARY genes			\ holds genes
: gene' ( "genes-name" -- genes-xt )
    also genes  bl word find  previous 
    0= ABORT" gene': Gene not found." ;

wordlist CONSTANT gene-internals	\ holds words with the gene internals

: gene-flag! ( internals-body mask -- )  swap >gene-flags or! ;
: special! ( internals-body -- )    special gene-flag! ;
\ : building! ( internals-body -- )   building gene-flag! ;
: gene-set-mask! ( internals-body -- )  set-mask gene-flag! ;

: set-mask? ( internals-body -- mask-as-flag )   >gene-flags @ set-mask and ;

\ Set indexed bit in n'th mask of the gene internals data:
: gene-n'th-mask-or! ( internals-body mask-number bit-index -- )
    >r
    over gene-set-mask!
    cells  swap >set-mask0 +
    1 r> lshift  swap or! ;

: internal' ( "internals-name" -- internals-xt )
    parse-word gene-internals search-wordlist
    0= ABORT" internal': Internal word not defined." ;

\ Test if an internal word is defined
: internal'? ( "internals-name" -- flag )
    parse-word gene-internals search-wordlist
    IF  drop  TRUE  ELSE  FALSE  THEN ;

: [internal']   internal'  POSTPONE literal ; IMMEDIATE

\ Tick, searching 'gene-internals' first, then through search order:
\ (used to find gene internals, but also sublists).
: internal+' ( "name" -- internals-xt|other-xt )
    bl word >r
    r@ count gene-internals search-wordlist IF
	rdrop EXIT
    THEN
    r> find 0= ABORT" internal+' Word not found." ;

\ Gene internal data can be built in buffers or in the body of internals words.
: gene-initialize ( addr -- )   gene-descriptor-length# erase ;

\ When declaring gene primitives the stack effect is given in a string
\ representation.  Stack input and output items are symbolized with single
\ letters indicating the type of the stack item.
\ The string is of the form  s" ab-c" similar to the Forth stack comments.
\ The following words deal with these strings:

: string-tos-is-address? ( addr count -- flag )
    dup IF
	1- + c@ [char] a =
    ELSE nip THEN ;

: string-tos-is-df-address? ( addr count -- flag )
    dup IF
	1- + c@ [char] A =
    ELSE nip THEN ;

\ Compute address of stack in string,
\ initialize pointer in the internals data,
\ return pointer.
: set-stack-in-pointer ( body -- pointer-to-stack-in )
    >r
    r@ >gene-start  r@ >gene-tokens @ cells +		\ skip xt chain
    dup r> >gene-stack-in cell+ ! ;			\ initialize pointer

\ Store stack-in data string and flags in internals gene data.
\ *must* be called before 'string>stack-out'
\ Does *not* do memory allocation.
: string>stack-in ( body addr count -- body )
    third >r
    dup r@ >gene-stack-in !	\ store length
    r@ set-stack-in-pointer	\ compute pointer, set it
    swap move			\ move string

    r> >gene-stack-in 2@
    2dup string-tos-is-address? IF
	third >gene-flags dup @ in-address or swap !	\ flag if tos is addr.
    THEN
    string-tos-is-df-address? IF
	dup >gene-flags dup @ in-df-address or swap !	\ flag if tos is f-addr
    THEN ;

\ Compute address of stack out string,
\ initialize pointer in the internals data,
\ return pointer.
: set-stack-out-pointer ( body -- pointer-to-stack-out )
    >r
    r@ >gene-start  r@ >gene-tokens @ cells +		\ skip xt chain
    r@ >gene-stack-in @ +				\ skip stack in string
    dup r> >gene-stack-out cell+ ! ;			\ initialize pointer

\ Set stack-out data string and flags in a internals data structure.
\ Must be called *after* 'string>stack-in'
\ Does *not* do memory allocation.
: string>stack-out ( body addr count -- )
    third >r
    dup r@ >gene-stack-out !	\ store length
    r@ set-stack-out-pointer	\ compute pointer, set it
    swap move			\ move string

    r> >gene-stack-out 2@
    2dup string-tos-is-address? IF
	third >gene-flags dup @ out-address or swap !	\ flag if tos is addr.
    THEN
    string-tos-is-df-address? IF
	dup >gene-flags dup @ out-df-address or swap !	\ flag if tos is addr.
    THEN
    drop ;

\ Word to set stackdata of a internals word from a string.
\ Must be called before making any dictionary changes, because it allot's.
\ The string has the form s" xxx-yyy" where 'xxx' is stack-in data, 'yyy' out
: string>stackdata! ( addr count body -- )
    -rot 2dup		( body addr count addr count )
    s" -" search 0= ABORT" string>stackdata! string does not contain '-'"
    2>r			( body addr count        r: addr2 count2 )
    r@ -		( body addr-in count-in  r: addr2 count2 )
    dup allot
    string>stack-in
    r> 1- r> 1+ swap	\ skip '-' in the remaining string
    dup allot align
    string>stack-out ;

100 CONSTANT (default-gene-cost#)	\ default gene cost of a primitive

\ 32   STRINGBUF-HANDLE: (name-buf)

\ Word to create a named internal gene header.
\ Create internals word and initialise descriptor:
: CREATE-gene-header: ( "name" -- body )
    get-current >r  gene-internals set-current
    CREATE
    here
    dup gene-descriptor-length# allot
    gene-initialize
    r> set-current ;

\ Word to compile the most basic gene type, a single forth word as a primitive:
\ This creates a corresponding internals word (only).
\ Gene primitives can *not* be changed by the mutation process.
\ Note that the given xt must correspond to a *named* word (no :noname).
\ You must set bit masks by hand, where they are needed.
: GENE-ALIAS: ( "name" addr-stack-string count xt -- )
    CREATE-gene-header:  >r		 ( addr count xt  r: body )

    \ Set descriptor data:
    primitive r@ >gene-flags !
    0 r@ >gene-tokens !
    (default-gene-cost#) r@ >gene-cost !
    dup r@ >gene-compiled-xt !
    r@ >gene-evaluated-xt !
    r> string>stackdata! ;

\ s" -"  ' noop  GENE-ALIAS: noop

\ Create a internals gene (primitive) under the same name:
\ You must set bit masks by hand, where they are needed.
: as-gene ( addr-stack-string count xt -- )
    [ decimal ] 32 stringbuf-open >r
    s" GENE-ALIAS: "  r@ cat
    dup xt>string     r@ cat
    r@ string@ EVALUATE
    r> stringbuf-close ;

\ Define stub for ';gene'
get-current
vocabulary stubs  also stubs definitions
: ;gene ;
: ; ;
set-current
s" -" ' ;gene as-gene
internal' ;gene >body
\ dup >gene-cost off				\ no, we leave a base cost
dup >gene-flags dup @ frame-popping or swap !	\ frame-popping?
' ; swap >gene-compiled-xt !
previous

: gene-xt!! ( body xt -- )
    swap			( xt body )
    2dup >gene-compiled-xt !
    >gene-evaluated-xt ! ;

\ if the last compiled gene has symbol wildcards
\ this must be called after >internal
\ : symbol-wildcards-on ( -- )
\     (last-internal) @  >gene-flags dup @ symbol-wildcard OR swap ! ;

: gene-body>n'th-xt-addr ( body-addr n -- addr )   cells  swap >gene-start + ;

: gene-n'th-xt-addr ( internals-xt n -- addr )
    cells  swap >body >gene-start  + ;

: gene-stack-effect ( body-addr -- n )
    >r r@ >gene-stack-out @  r> >gene-stack-in @  - ;

: .gene-info ( internal-xt -- )	\ currently just for a quick test...
    page
    dup xt>string		cr type
    EXECUTE >r
    r@ >gene-flags @ [ primitive ] literal and IF
	." 		primitive"
    THEN
    cr
    r@ >gene-stack-in 2@	cr ." stack in:  	" type
    r@ >gene-stack-out 2@	cr ." stack out: 	" type
    r@ gene-stack-effect	cr ." stack effect:	" .
    r@ >gene-tokens @		cr ." tokens:    	" .
    r@ >gene-cost @		cr ." cost:      	" .
    r@ >gene-flags @
\   dup				cr ." flags      	" .bin
    dup IF
	dup set-mask and IF
	    r@ >set-mask0
	    nuc-bitmasks# 0 ?DO
		dup i + @  dup IF
		    cr i . ." set div mask	" .bin
		ELSE drop THEN
	    LOOP
	    drop
	THEN

	gene-flags listed-mask-string
	dup string@		cr ." flags      	" type
	stringbuf-close
    ELSE drop THEN
    cr rdrop ;

: cat-name ( xt handle -- )   >r  xt>string r@ cat   bl r> char-cat ;

: cat-gene-string ( internals-body handle -- )
    swap	( handle body )
    dup >gene-flags @ primitive and IF
	>gene-compiled-xt @ over cat-name
    ELSE
	dup >gene-start		( handle body start-of-xt-chain )
	swap >gene-tokens @ 0 ?DO	( handle start-of-xt-chain )
	    dup i cells + @
	    EXECUTE >gene-compiled-xt @  third cat-name
	LOOP
	drop
    THEN
    drop ;

: internals-string ( internal-body -- handle )	\ please do close buffer.
    [ decimal ] 128 stringbuf-open swap		( handle body )
    dup >gene-start				( handle body start-addr )
    swap >gene-tokens @ cells over +		( handle start-addr stop-addr )
    swap ?DO
	i @ xt>string third cat
	bl over char-cat
    cell +LOOP ;

: cat-evaluate-string ( internals-body handle -- )
    swap	( handle body )
    dup >gene-flags @ primitive and IF
	>gene-evaluated-xt @ swap cat-name EXIT
    THEN

    dup >gene-start			( handle body start-of-xt-chain )
    swap >gene-tokens @ 1- 0 ?DO	( handle start-of-xt-chain )
	dup i cells + @
	EXECUTE >gene-evaluated-xt @  third cat-name
    LOOP
    2drop ;



\ During mutation and trial phase internals data is kept in a stringbuffer.
\ Open a buffer and create a internal data descriptor there.
\ The buffer must be closed later on.
: initialise-buffered-internal ( -- handle )
    gene-descriptor-length# 8 cells + stringbuf-open
    gene-descriptor-length# over string-size!
    dup buffer-data-addr gene-initialize ;

: ?set-internal-masks ( source-body destination-body -- )
    over set-mask? 0= IF  2drop EXIT  THEN

    dup gene-set-mask!
    >set-mask0 swap >set-mask0
[ log-mask @ 0> ] [IF] \ do it logging extra information?
    log-mask @ log-m-extra and IF
	nuc-bitmasks# 0 ?DO	( destination-set-mask0 source-set-mask0 )
	    s" change mask "	cat-log
	    i num>string	cat-log
	    s"  from: "		cat-log
	    base @ -rot  2 base !

	    dup i cells + @  third i cells +  >r
	    r@ @ num>string cat-log
	    r@ or!
	    s"  to: "	cat-log
	    r> @ num>string 0 log-it
	    rot base !
	LOOP
    THEN
[ELSE]
    nuc-bitmasks# 0 ?DO	( destination-set-mask0 source-set-mask0 )
	dup i cells + @  third i cells +  or!
    LOOP
[THEN]
    2drop ;

\ Word to chain xt in buffered internals data:
\ ?log, count, sum cost, set bit masks and chain xt.
: xt-to-internals-buffer ( handle xt -- )
    [ log-mask @ ] [IF]
	log-m-much? IF
	    s" adding: "	cat-log
	    dup xt>string	0 log-it
	THEN
    [THEN]

    over buffer-data-addr >r		( handle xt  r: destination-addr )
    1 r@ >gene-tokens +!
    dup >body				( handle xt body  r: destination-addr )
    dup >gene-cost @  r@ >gene-cost +!
    r> ?set-internal-masks
    cat-n ;

: compile-gene ( internal-xt -- )
    >r get-current r>  also genes definitions
    [ decimal ] 1028 stringbuf-open >r

    s" : " r@ cat
    dup r@ cat-name
    bl r@ char-cat
    >body r@ cat-gene-string
    r@ string@ EVALUATE

    r> stringbuf-close
    previous set-current ;

: get-internals-xt ( addr count -- xt )
    [ decimal ] 32 stringbuf-open >r
    s" internal' " r@ cat  r@ cat  r@ string@ EVALUATE
    r> stringbuf-close ;

: get-gene-xt ( addr count -- xt )  also genes  get-xt  previous ;

: compile-buffered-internals ( handle addr-name count-name -- internal-xt )
    \ Create internals header:
    get-current >r gene-internals set-current	( h addr count  r: old-current)
    [ decimal ] 32 stringbuf-open >r		( h a c  r: old-current handle)
    s" CREATE " r@ cat
    2dup r@ cat r@ string@ EVALUATE
    r> stringbuf-close  r> set-current		( handle addr-name count  r:--)

    \ Allot space and move data
    rot >r  here			( addr-name count-name body  r: handle)
    r@ string@ >r r@ allot align  swap r> move	( addr-name count-name)
    get-internals-xt
    rdrop ;

CREATE scratch-masks	\ scratch area for bit masks during  GENE:  definition
nuc-bitmasks# cells allot

: clear-scratch-masks ( -- )   scratch-masks nuc-bitmasks# cells erase ; 

: scratch-masks? ( -- flag-not-normalised )	\ bits set?
    scratch-masks 0
    nuc-bitmasks# 0 ?DO
	over i cells + @ or
    LOOP
    nip ;

\ GENE: creates a gene that can be mutated.
\ It defines both the internals word and the gene word.
\ The building genes must all be defined as internals.
: GENE: ( addr-of-stack-string count -- )
    clear-scratch-masks
    get-name >r

    CREATE-gene-header:			( addr count body  r: handle-name )

    BEGIN	( addr count body  r: handle-name )
	\ scan next word, refill if required.
	BEGIN \ refill loop
	    parse-word
	    dup IF
		dup
	    ELSE
		nip
		refill 0= ABORT" GENE: expected ';gene' not reached."
	    THEN
	UNTIL \ refilled and word scanned

	\ Search internals for the ingredients of the gene:
	gene-internals search-wordlist
	0=  ABORT" GENE: Word not found."
	( addr count body xt )
	dup ,						\ chain xt
	dup >body >gene-cost @  third >gene-cost +!	\ sum costs
	dup >body dup set-mask? IF			\ set bitmasks?
	    >set-mask0
	    nuc-bitmasks# 0 ?DO
		dup i cells + @  scratch-masks i cells + or!
	    LOOP
	    drop
	ELSE drop THEN
	1 third >gene-tokens +!				\ count tokens
	[internal'] ;gene =
    UNTIL

    >r			( addr-stack-string count body  r: handle-name body )
    r@ string>stackdata!
    r>			( body  r: handle-name )
    r@ string@		( body addr-name count-name  r: handle-name )
    2dup get-internals-xt
    dup compile-gene

    scratch-masks? IF		\ set bitmasks?
	>body  dup gene-set-mask!
	>set-mask0  scratch-masks swap  nuc-bitmasks# cells  move
    ELSE drop THEN

    get-gene-xt gene-xt!!
    r> stringbuf-close ;

s" -" GENE: noop ;gene


\ Search a genome 'intnl-xt' for a gene 'searched-intnl-xt' through top level
\ starting with item 'start'.
\ Return (first) 'item-number TRUE' on success or 'FALSE' on failure.
: scan-genome-for-gene ( intnl-xt searched-int-xt start -- item true | false)
    rot >r	( searched-internal-xt start-index   r: internal-genome-xt )

    \ check range:
    dup  0  r@ >body >gene-tokens @  WITHIN 0= IF
	rdrop 2drop FALSE EXIT				\ outside range
    THEN

    r> dup >body >gene-tokens @	rot ( searched xt tokens=end-index start r: )
    ?DO	( searched-xt xt-genome )
	dup i gene-n'th-xt-addr @  third = IF
	    2drop
	    i TRUE
	    unloop EXIT
	THEN
    LOOP

    2drop FALSE ;	\ not found

\ Test if a genome 'internal-genome-xt' has a gene 'searched-internal-xt'
\ (*top* level scan only)
: genome-has-gene? ( internal-genome-xt searched-internal-xt -- flag )
    0 scan-genome-for-gene   dup IF nip THEN ;



\ leftovers to do somewhere else: ############

VARIABLE actual-genepool-xt
INCLUDE gene-pool.fs
' gene-primitives actual-genepool-xt !

: to-gene-pool ( probability xt -- )  actual-genepool-xt @ execute set-one ;

: to-gene-pool' ( "name"  probability -- )  internal' to-gene-pool ;


true [IF] \ some testing

    false [IF]
	s" a-n" ' @ GENE-ALIAS: @
	s" na-" ' ! GENE-ALIAS: !
	s" aa-" GENE: gene-1  @ ! ;gene
	internal' gene-1 .gene-info
	wait

	s" aaa-n" GENE: gene-2  gene-1 @ ;gene
	internal' gene-2 .gene-info
	wait
    [THEN]

: symbol-wildcard FALSE ;	\ eliminate later on #############

[THEN]
