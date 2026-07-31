\ worlds.fs

: world/time-version ( -- addr count )
    cvs" 	$Id: worlds.fs,v 1.35 2005/05/10 12:25:40 f Exp $	" ;

\ Playground for our 'Cells':  virtual space and time.

\ This version can cope with multiple worlds each one having her own
\ set of world local variables like dimensions, time-planes, qualities, etc.


\ The structure of spot data, though compile time configurable, will be
\ the same in all worlds of a given brew incarnation.


\ The actual world is referenced by a pointer value:
0 VALUE this-world

LIST: worlds

: worlds# ( -- u )  worlds nodes ;

: world# ( -- u )
    worlds
    dup nodes 0 ?DO
	next-node
	dup @  this-world = IF  drop i unloop EXIT  THEN
    LOOP
    -1 ;	\ thats dangerous

\ ########################## will be world variables:
VARIABLE (mutated-max)	\ Just to locate fat genes in the log file:
\ How many genes have survived trial phase and have been compiled:
VARIABLE compiled-genes
VARIABLE nuc-do-cost
VARIABLE code-price
  


\ Compute # of integer planes af the world:
1			\ cp pointer plane
1+			\ food plane
spot-qualities# +	\ qualities, ...
spot-properties# +
spot-secrets# +
\ # of different integer planes of the world: cp food qualities, properties,...
CONSTANT field-i-planes#

\ Compute # of dfloat planes af the world:
0
spot-f-qualities# +
spot-f-properties# +
spot-f-secrets# +
CONSTANT field-df-planes#	\ # of different dfloat planes of the world


5 CONSTANT max-dimensions#		\ seems plenty

\ Word to create world local integer variables:
\ (including descriptor variables).

: WORLD-VARIABLE: ( "name"  offset -- offset+cell )
    ['] this-world swap  BASE+OFFSET:  nip ;

\ 'this-world' points to a world descriptor:
0
\ It's convenient for loading a saved world to have these datas first:
\ Don't rearrange them.
WORLD-VARIABLE: world-length		\ total descriptor, locals, spot data
WORLD-VARIABLE: world-version		\ for saving

WORLD-VARIABLE: world-allocated		\ address of allocated memory
	 				\ (can be padded for alignement/speed)
WORLD-VARIABLE: world-name		\ handle of world name stringbuffer
\ WORLD-VARIABLE: w-max-dimensions	\ hmm...
\ WORLD-VARIABLE: spot-integers		\ pointers, food, qualities, ...
\ WORLD-VARIABLE: spot-floats		\ number of spot float variables
WORLD-VARIABLE: spot-data-size		\ including padding
WORLD-VARIABLE: world-time-planes	\ present, future, ...
WORLD-VARIABLE: world-dimensions	\ number of dimensions
WORLD-VARIABLE: dimension-ranges	\ (positive) range of first dimension
max-dimensions# 1- cells +		\	     range for each dimension
WORLD-VARIABLE: visibility-on		\ start of visibility range
max-dimensions# 1- cells +		\	     for each dimension
WORLD-VARIABLE: visibility-off		\ end of visibility range
max-dimensions# 1- cells +		\	     for each dimension
WORLD-VARIABLE: backgound-off		\ switching background in 3rd dimension
WORLD-VARIABLE: neighbour-vectors	\ pointer to neighbour vectors table

\ Scratch area:
WORLD-VARIABLE: shuffling-offsets	\ scratch area for shuffling
max-dimensions# 1- cells +

\ Coordinate of highest dimension gets stored at lowest address.
\ ('coordinates' get only set when used).'
WORLD-VARIABLE: coordinates		\ scratch cells for coordinates
max-dimensions# 1- cells +		\	     for each dimension

\ The following variables can be calculated from the previous data,
\ but it's convenient to store them here:

WORLD-VARIABLE: directions		\ number of directions
WORLD-VARIABLE:	(spots)			\ number of spots in this world
WORLD-VARIABLE: time-plane-length	\ all spot data of one moment in time
localise-spot-data 0= [IF]
    WORLD-VARIABLE: (i-array-length)	\ length of *one* basic integer array
    WORLD-VARIABLE: (f-array-length)	\ length of one basic float data field
    WORLD-VARIABLE: (float-offset)	\ skip all integer arrays and falign
[THEN]
WORLD-VARIABLE: total-list-length	\ spot data for all times

WORLD-VARIABLE: dim-step		\ spot dim-step + goes to next spot
max-dimensions# 1- cells +		\	     for each dimension

true [IF]				\ might influence speed.
    spot-alignement# n-ALIGN
[THEN]

\ dup CONSTANT world-descriptor-length#	\ I don't need it, I think.

\ Then we have a set of local world variables:

\ Local cell wide world variables:
WORLD-VARIABLE: (time-index)		\ cyclic index of the present
					\ *ONLY* time-step should change it
					\ for internal use *ONLY* ( tp )

WORLD-VARIABLE:	spot			\ actual spot index
WORLD-VARIABLE:	step			\ current time (counter)
					\ to get both: spot 2@ ( -- step spot )

WORLD-VARIABLE: fixed-population-size	\ used for elitism
WORLD-VARIABLE: elite			\ ditto
WORLD-VARIABLE: score-list		\ scratch for list address (elitism).
					\ (not saved!)
WORLD-VARIABLE: living
WORLD-VARIABLE: newborn
WORLD-VARIABLE: trial
WORLD-VARIABLE: selected
WORLD-VARIABLE: cloned
WORLD-VARIABLE: died
\ WORLD-VARIABLE: (mutated-max)	\ Just to locate fat genes in the log file:
\ How many genes have survived trial phase and have been compiled:
\ WORLD-VARIABLE: compiled-genes
\ WORLD-VARIABLE: nuc-do-cost
\ WORLD-VARIABLE: code-price

\ Local float world variables:
\ faligned				\ pad for floats

\ dup CONSTANT used-world-header-length#	\ could become handy

\ Align and store the length of this field that every world starts with:
spot-alignement# n-ALIGN
dup CONSTANT world-header-length#

WORLD-VARIABLE: (spot-data-field)	\ not a variable, start of data field
drop

\ Some of these world variables could have a corresponding 'normal'
\ variable which is used while in a certain world for speed reasons.
\ If you enter or leave a world these variables must be copied into
\ each other which would be done in 'enter-world' and 'leave-world'.

\ How many spots make a step in this dimension?  See '(big-bang)'
: initialise-dim-steps ( -- )
    1
    world-dimensions @ cells  0 DO
	dup dim-step i + !
	dimension-ranges i + @ *
    CELL +LOOP
    drop ;


\ Coordinates and directions:

\ Store coordinates at addr:
\ Coordinate of highest dimension gets stored at lowest address.
: coordinates! ( c0 ... cn addr -- )
    dup world-dimensions @ cells +  swap DO
	i !
    CELL +LOOP ;

\ Fetch coordinates at address:
: coordinates@ ( addr -- c0 ... cn )
    dup world-dimensions @ 1- cells + DO
	i @
    [ cell negate ] literal +LOOP ;

\ Coordinates of a given spot index:
: spot>coordinates ( spot -- coordinate-0 ... coordinate-dim-1 )
    world-dimensions @ cells 0 DO
	dimension-ranges i + @  /mod
    cell +LOOP
    drop ;

\ Spot index of a set of coordinates:
: coordinates>spot ( coordinate-0 ... coordinate-dim-1 -- spot )
    0
    0 world-dimensions @ 1- cells DO
	swap
	dim-step i + @ * +
    [ -1 cells ] literal +LOOP ;

\ Add a direction vector and a set of coordinates, normalise:
\ Each dimension wraps to itself.
: coordinates+ ( c1 ... c1n  c2 ... c2n  -- c3 ... c3n )
    world-dimensions @ 1-
    BEGIN
	dup 2 + roll
	rot +
	over cells dimension-ranges + @ mod
	dup 0< IF over cells dimension-ranges + @ + THEN
	>r
	1-
	dup 0<
    UNTIL
    drop

    0
    BEGIN  r> swap  1+ world-dimensions @ over = UNTIL
    drop ;

: 3^n ( n -- 3^n )   1   swap 0 ?DO  3 * LOOP ;		\ take it easy..

\ Build an array of direction vectors with
\ all possible combinations of -1, 0, +1 combinations of all dimensions:
: init-neighbour-vectors ( addr -- )
    directions @ 0 DO
	i
	world-dimensions @ cells 0 DO
	    3 /mod swap 1-  third i + !
	CELL +LOOP
	drop

	world-dimensions @ cells +
    LOOP
    drop ;

\ Allocate memory for the neighbour vectors.
: allocate-neighbour-vectors ( -- allocated-address )
    world-dimensions @  directions @ *  cells allocate
    ABORT" setup-neighbour-vectors: Couldn't allocate" ;

\ Pointers to all initialised neighbour vector tables for each n-dimensionality
\ '(big-bang)' takes care about initialising.
CREATE neighbour-vectors-pointers
max-dimensions# cells allot
neighbour-vectors-pointers  max-dimensions# cells  erase

\ Allocate and initialize neighbour vector table, set pointer:
: setup-neighbour-vectors ( -- addr )
    allocate-neighbour-vectors
    dup init-neighbour-vectors
    dup  neighbour-vectors-pointers  world-dimensions @ 1- cells + ! ;

\ Give the address of the neighbour vectors table, makes sure it's initialised:
: neighbour-vectors-address ( -- addr )
    neighbour-vectors-pointers  world-dimensions @ 1- cells + @
    dup IF EXIT ELSE drop THEN

    setup-neighbour-vectors ;

\ Searching all neighbour spots in a random order cannot just sequentially
\ follow the neighbour vectors. As each one dimension changes faster or slower
\ a spot towards a fast changing dimension would be found much more frequently.

\ I avoid this by shuffling the mapping of the coordinates to the dimensions.
\ Before searching i.e. a free neighbour spot a shuffling pattern is set up.
\ This is used (only) for this one search.

\ Initialise shuffling of vector coordinate to dimension mapping
\ This is done once before searching all neighbour cells of a spot. 
: prepare-shuffling ( -- )
    world-dimensions @ >r

    r@ 0 ?DO i cells LOOP

    BEGIN
	r@ WHILE
	r@ random-ranged roll
	r> 1- >r
	shuffling-offsets r@ cells + !
    REPEAT
    rdrop ;

false [IF] \ currently not used
\ Shuffling coordinates of a vector:
: shuffle-vector ( ... ... )		\ this word does it on stack items
    world-dimensions @
    BEGIN
	1-
	shuffling-offsets over cells + @ cell / 1+ pick >r
	dup 0=
    UNTIL
    drop

    world-dimensions @ 0 DO drop LOOP

    0
    BEGIN
	r>
	swap
	1+
	dup world-dimensions @ =
    UNTIL
    drop ;
[THEN]

\ Fetch coordinates stored at addr shuffling them
\ in the way set up by 'prepare-shuffling':
: shuffled@ ( addr -- c0 c1 ... cn-1 )
    world-dimensions @ cells 0 DO
	dup shuffling-offsets i + @ + @
	swap
    CELL +LOOP
    drop ;

false [IF] \ left over from testing
: t
    prepare-shuffling
    world-dimensions @ cells
    neighbour-vectors @
    over directions @ * 0 DO
	dup i + shuffled@ cr . . .
    over +LOOP
    2drop ;
[THEN]

: world-name2@ ( -- addr count )   world-name @ string@ ;


\ Spot data:

\ Each spot has his own set of variables like a poiter to a nuc living there,
\ food, spot qualities, spot properties, spot secrets and the like.

LIST: spot-var-xts		\ list of xt's of all     spot variables
LIST: integer-spot-vars		\ list of xt's of integer spot variables 
LIST: dfloat-spot-vars		\ list of xt's of dfloat  spot variables 

\ Depending on 'localise-spot-data' this data is organized as a number
\ of arrays or as localized records in so called hot-spots.

localise-spot-data [IF]
    \ Spot datas are localized for cache consistency.
    \ Does *not* speed up things here though...
    INCLUDE world-spots-localized.fs

: spot-var-is-float? ( index -- flag )  drop FALSE ;

[ELSE]
    \ Spot data are organized in arrays, one for each variable.
    \ On my system it's faster.
    INCLUDE world-spots-arrayed.fs

: spot-var-is-float? ( index -- flag )	\ index must be within range
    [ spot-float-start-index 1- ] literal > ;

[THEN]

\ Enter n'th world from worlds list:
: (set-n'th-world) ( u -- )   worlds n'th-node @ enter-world ;

\ Enter n'th world from worlds list, recording:
\ See  |set-n'th-world|  for logging version.
DEFER ?record-set-n'th-world ( u -- )
: set-n'th-world ( u -- )   dup (set-n'th-world)  ?record-set-n'th-world ;


\ Test if a spot is inhabited:
: someone-here? ( i -- addr|false)	\ checks if a spot is occupied
    spot @ swap	( spot i )		\ remember spot
    >spot! fcp @  ( spot a|false )	\ return value on TOS
    swap >spot! ;			\ go back to actual spot

\ Try to find a random free neighbour spot:
: world-free-neighbour-spot? ( -- spot' true | false )
    spot @ spot>coordinates coordinates coordinates!

    prepare-shuffling

    world-dimensions @ cells			( vector-size )
    directions @				( vector-size vectors# )
    neighbour-vectors @				( size # base-addr )
    over random-ranged >r			( size # base  r: start-index )
    2 random-ranged IF				\ loop upwards
	over r@ + r> DO				( size # base )
	    i third mod fourth *  over +	( size # a actual-vector-addr )
	    shuffled@  coordinates coordinates@  coordinates+
	    coordinates>spot dup someone-here? 0= IF
		unloop
		>r drop 2drop r>
		TRUE EXIT
	    ELSE drop THEN
	LOOP
    ELSE					\ loop downwards
	r@ third - 1+ r> DO			( size # base )
	    over i + third mod fourth *  over +	( size # a actual-vector-addr )
	    shuffled@  coordinates coordinates@  coordinates+
	    coordinates>spot dup someone-here? 0= IF
		unloop
		>r drop 2drop r>
		TRUE EXIT
	    ELSE drop THEN
	-1 +LOOP
    THEN
    2drop drop
    FALSE ;

\ Erase all spot data:
: erase-field ( -- )
    world-header-length# >r
    this-world r@ +
    world-length @ r> - erase		\ clean up the whole universe
    living off		\ needed for re-playing recorded sessions...

\    compiled-genes off ######################## must think about these...
\    (mutated-max) off ########################
\    nuc-do-cost off ########################
\    code-price off ########################

    time-step
    -1 step !				\ hmm? not really sure about that
    false cp! ;

\ Free allocated memory of the actual world:
: free-world-memory ( -- )
    world-allocated @ free 0= IF  false to this-world  EXIT THEN

    bell cr ." free-world-memory: Couldn't free memory! " 1500 ms ;

DEFER (free-field)
: (remove-world) ( -- )	\ not recording nor logging.
    this-world 0= IF  EXIT  THEN

    world-name @ stringbuf-close
    world# worlds unlink
    (free-field)
    free-world-memory ;

DEFER ?record-remove-world
: remove-world ( -- )   ?record-remove-world (remove-world) ;

DEFER ?record-remove-all-wolds ( -- )
: remove-all-worlds ( -- )
    worlds# 0 ?DO
	i (set-n'th-world) (remove-world)
    LOOP
    ?record-remove-all-wolds ;

\ Remove all worlds but this one:
: (remove-other-worlds) ( -- )
    this-world
    0 worlds# 1- DO
	i (set-n'th-world)
	this-world over <> IF
	    (remove-world)
	THEN
    -1 +LOOP
    enter-world ;

\ old conservative 80 25 console screen version
: big-bang ( -- )
    2 24 80 2 (big-bang)
    80 visibility-off !			\ above range, not switching off
    24 visibility-off cell+ ! ;		\ above range, not switching off

\ Create a 2 dimensional world that fits on screen:
\ (c-l l-s aware alternative to conservative 80 25 big-bang).
: screen-sized-big-bang ( -- )
    2  l-s 1-  c-l  2 (big-bang)
    c-l visibility-off !
    l-s 1- visibility-off cell+ ! ;

: 3D-simple-big-bang ( u -- )
    2 over 24 80 3 (big-bang)
    1 backgound-off !
    80 visibility-off !			\ above range, not switching off
    24 visibility-off cell+ !   	\ above range, not switching off
    ( u ) visibility-off cell+ cell+ ! ; \ above range, not switching off

: small-plane-bang ( -- )
    2 12 40 2 (big-bang)
    39 visibility-off !
    12 visibility-off cell+ ! ;		\ above range, not switching off

\ : big-bang small-plane-bang ;

: big-plane-bang ( -- )
    2 50 190 2 (big-bang)
    80 visibility-off !
    24 visibility-off cell+ ! ;

\ : big-bang big-plane-bang ;
