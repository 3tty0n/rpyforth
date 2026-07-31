\ statistics.fs
\ 	$Id: statistics.fs,v 1.35 2005/05/15 05:11:52 f Exp $	
decimal

false					\ true for testing, false otherwise

\ Words to break down integer or dfloat data to show a ascii graf of it.

\ ****************************************************************
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" advanced.fs" REQUIRED
s" display.fs" REQUIRED

\ ****************************************************************

0
OFFSET: >statistics-slices#				\ integer
dfloat-OFFSET: >statistics-data-range-min		\ integer or dfloat
dfloat-OFFSET: >statistics-data-range			\ integer or dfloat
dup CONSTANT statistics-descriptor-length#
OFFSET: >statistics-data	drop	\ start of slice counters

: statistic-array-size ( slices# -- size )
    cells 2* statistics-descriptor-length# + ;

: n'th-slice-counter ( n array-addr -- n'th-count-addr )
    >statistics-data  swap cells + ;

: n'th-vertical-boundary ( n array-addr -- n'th-vertical-boundary-addr )
    tuck
    n'th-slice-counter
    swap >statistics-slices# @ cells + ;

\ Word called before reading in data's. Range of data must be known.
\ Note:
\ For reaction on selecting statistical data display as a menu item
\ we must be able to get some data at a time the array is already gone.
\ The following variables are a hack to be able to do that:
VARIABLE (last-stat-type)		type-unknown% (last-stat-type) !
dfVARIABLE (last-stat-min)		\ integer or dfloat
dfVARIABLE (last-stat-range)		\ integer or dfloat
VARIABLE (last-stat-slices)		\ integer

: init-statistic-array-int ( min max slices# addr -- )
    >r					( min max slices#   r:addr )
    r@ over statistic-array-size erase				\ erase all
    dup (last-stat-slices) !					\ hack s. above
    r@ >statistics-slices# !		( min max   r:addr )	\ set slices#
    over -							\ real range

    \ if range is smaller than screen width we take a one to one relationship
    c-l 1- max							\ actual range

    dup (last-stat-range) !					\ hack s. above
    r@ >statistics-data-range !					\ store range
    dup (last-stat-min) !					\ hack s. above
    r> >statistics-data-range-min !				\ store min 

    type-int-addr% (last-stat-type) ! ;

: init-statistic-array-dfloat ( F: min max  D: slices# addr -- )
    >r					( min max slices#   r:addr )
    r@ over statistic-array-size erase				\ erase all
    dup (last-stat-slices) !					\ hack s. above
    r@ >statistics-slices# !		( F: min max   r:addr )	\ set slices#
    fover f-							\ real range

    \ If range is smaller than screen width we take a one to one relationship
    \ I fake that also for dfloats when range is zer0:
    fdup f0= IF  fdrop  1e0  THEN				\ actual range

    fdup (last-stat-range) df!					\ hack s. above
    r@ >statistics-data-range df!				\ store range
    fdup (last-stat-min) df!					\ hack s. above
    r> >statistics-data-range-min df!				\ store min 

    type-df-addr% (last-stat-type) ! ;


\ word to count data within slices
\ new version gathering data out of range in border slices
VARIABLE data-out-of-range		\ counts data out of range
: data2slice ( data addr -- )
    >r					( data   r: addr )
    r@ >statistics-data-range-min @ -	( data-off-from-min     r: addr )
    r@ >statistics-slices# @		( data' slices#         r: addr )
    r@ >statistics-data-range @	1+	( data' slices# range+1 r: addr )
    */					( slice-OK   r: addr )

\ old version. code left here for tests
\      dup 0< ABORT" data2slice: data below range"
\      dup r@ >statistics-slices# @ > ABORT" data2slice: data out of range"
  
    dup 0< IF					\ below range
  	1 data-out-of-range +!
	drop 0
    THEN
    dup r@ >statistics-slices# @ 1- > IF	\ above range
	1 data-out-of-range +!
	drop
	r@ >statistics-slices# @ 1-
    THEN

    r> n'th-slice-counter
    1 swap +! ;			\ increase count of data in this slice

: float-data2slice ( F: data  D: addr -- )
    >r					  ( data   r: addr )
    r@ >statistics-data-range-min df@ f-  ( data-off-from-min     r: addr )
    r@ >statistics-data-range df@ f/
    r@ >statistics-slices# @ s>f f* f>s	  ( slice-OK   r: addr )

    dup 0< IF					\ below range
	1 data-out-of-range +!
	drop 0
    THEN
    dup r@ >statistics-slices# @ 1- > IF	\ above range
	1 data-out-of-range +!
	drop
	r@ >statistics-slices# @ 1-
    THEN

    r> n'th-slice-counter
    1 swap +! ;			\ increase count of data in this slice

\ Words to determine max and min of a series of integer values at a double
\ cell at addr
\ 2VARIABLE (min&max)		\ (min&max) 2@ gives ( -- min max )
0
OFFSET: >max
OFFSET: >min	drop

: min-max-init ( 2addr -- )
    >r
    lowest-integer#  r@ >max !
    highest-integer# r> >min ! ;

: data2min-max ( n 2addr -- )
    >r
    dup r@ >max @ max r@ >max !
    r> >min dup @ rot min swap ! ;

\ Words to count types and max/min values of float data:
0
OFFSET: >-inf-count
OFFSET: >real-count
OFFSET: >+inf-count
OFFSET: >nan-count
\ dfloat-OFFSET: >dfloat-sum
dfloat-OFFSET: >dfloat-max
dfloat-OFFSET: >dfloat-min
CONSTANT float-check-field-length#

: float-min-max-init ( addr-of-check-field -- )
    >r
    -infinity r@ >dfloat-max df!
    +infinity r> >dfloat-min df! ;

: float-data-check-in ( F: sample addr-of-check-field -- )
    >r
    fdup float-type 1+ cells r@ + +1!			\ increase type counter
    fdup  r@ >dfloat-max df@  fmax  r@ >dfloat-max df!	\ new maximum
    r@ >dfloat-min  df@  fmin  r> >dfloat-min df! ;	\ new minimum

\ sometimes we want a steady zoom
DEFER <notify-zoom-change>
DEFER <vertical-zoom-scale>
DEFER <vertical-display-range>

1
ENUM: horizontal
ENUM: vertical
drop

: vertical-zoom-control ( range -- display-range )
    >r

    <vertical-display-range> @			( displayed-range r: range )
    dup IF					\ initialized?
	dup r@ < IF					\ big enough?
	    r@ <vertical-display-range> !		\ take real size
	    vertical <notify-zoom-change>
	THEN

	r@ swap <vertical-zoom-scale> 2@ */  < IF	\ too small!
	    r@ <vertical-display-range> !		\ take real size
	    vertical <notify-zoom-change>
	THEN
    ELSE					\ was not initialized
	r@ <vertical-display-range> !			\ take real size
	default-vertical-zoom-scale <vertical-zoom-scale> 2!
	\ vertical <notify-zoom-change>
	drop
    THEN

    rdrop
    <vertical-display-range> @ ;

\ after reading data in, we set up vertical slices
\ note that the slices count is off by one, so the status line will fit too.
: statistic-slice-up-vertical ( vertical-slices addr -- max-slice-count )
    >r				( slices#   r: addr )
    r@ >statistics-slices# @ cells swap	( record-size slices#   r: addr )

    \ get maximal count:
    lowest-integer#
    r@ >statistics-data				\ lower loop limit
    dup r@ >statistics-slices# @ cells +	\ upper loop limit
    swap 2dup 2>r				\ preserve loop limits
    ?DO	( record slices# actual-maximal-slice-count )
	i @ max					\ loop over counters, max
    cell +LOOP
    \	( record slices# maximal-slice-count   r: addr 2loop-limits )


    \ if max-count is less than the number of slices, we count one to one:
    over max

    \ sometimes we want a steady zoom
    vertical-zoom-control			\ handle zoom

    2r> ?DO ( record-size slices# max-slice-count )
	2dup i @ ( size slices# max slices# max-slice-count slice-count)
	swap */	 ( size slices# max-slice-count slice-OK )
	i 4 pick + !				\ store y boundary index
    cell +LOOP

    rdrop >r 2drop r> ;

[UNDEFINED] (zoomed) [IF]	VARIABLE (zoomed)	(zoomed) off	[THEN]
VARIABLE statistic-status-bg-color	blue statistic-status-bg-color !
DEFER scan-horizontal-zoom? ( -- flag )
' true IS scan-horizontal-zoom?

: .zoom ( -- )
    scan-horizontal-zoom? 0= IF
	<bright-colours>
	c-l 2/ 4 - at-x ."  <fixed> "
    THEN

    (zoomed) @ dup IF		\ show if zoom has changed. (see above)
	<bright-colours>
	dup horizontal and IF
	    c-l 2/ 5 - at-x
	    scan-horizontal-zoom? IF ." -" ELSE ." x" THEN
	THEN
	scan-horizontal-zoom? IF
	    c-l 2/ 4 - at-x ." <zooming>"
	THEN
	dup vertical and IF
	    c-l 2/ 5 + at-x ." |"
	THEN
	(zoomed) off
    THEN drop
    reset-colours ;

12 CONSTANT scan-x
: .scan-word ( -- )   scan-x at-x  ." scan: " ;

52 CONSTANT v-range-x
: .v-range ( -- )   v-range-x at-x ." v-range: " ;

: statistics-status-line-int ( addr max-slice-count title-addr title-count -- )
    2>r >r

    statistic-status-bg-color @ color-background
    0 this-line  2dup at-xy  clear-line-to-end  at-xy		\ clean display
    dup >statistics-data-range-min @
    dup highest-integer# = IF
	." No data."
	drop
	(zoomed) off	\ cosmetics
    ELSE
	dup .
	over >statistics-data-range @ + num>string dup c-l swap -
	[ lower-right-scrolls ] [IF]  1-  [THEN]
	at-x type
	\ this would be the logical place to display zoom change, but this
	\ gives problems with colors, if foreground color is not default
	.v-range  <vertical-display-range> @ .		\ vertical range
    THEN rdrop drop
    .scan-word 2r> type

    .zoom ;

: statistics-status-line-float ( addr max-slice-count title-a title-count -- )
    2>r >r

    statistic-status-bg-color @ color-background
    0 this-line  2dup at-xy  clear-line-to-end  at-xy		\ clean display
    dup >statistics-data-range-min df@
    fdup +infinity f= IF
	." No data."
	fdrop
	(zoomed) off	\ cosmetics
    ELSE
	fdup float>short-string type 
	dup >statistics-data-range df@ f+
	float>short-string dup c-l swap -
	[ lower-right-scrolls ] [IF]  1-  [THEN]
	at-x type
	\ this would be the logical place to display zoom change, but this
	\ gives problems with colors, if foreground color is not default
	.v-range  <vertical-display-range> @ .		\ vertical range
    THEN rdrop drop
    .scan-word 2r> type

    .zoom ;


\ Special case of (statistic-display) that colours a range of slices
\ during range definition: (stat-displ-coloured-range)
\
\ Variables for the low and high border slice. Both *inside* the range.
\ 'low' *must* be lower or equal to 'high'
VARIABLE (slice-range-low)	\ both *inclusive*
VARIABLE (slice-range-high)	\ both *inclusive*
\
: (stat-displ-coloured-range) ( addr lines -- )  
    0 swap 2 - DO	\ lines loop
	default-color color-background
	c-l 0 DO	\ caracter loop
	    i (slice-range-low) @ = IF	\ range starts
		color-selected-bg-xt @ EXECUTE color-background
	    THEN

	    i over n'th-vertical-boundary @ j > IF
		[char] *
	    ELSE
		bl

		\ in the last row, we want to show if there's something or not:
		j 0= IF					\ last row?
		    i  2 pick  n'th-slice-counter @ IF	\ something there?
			drop [char] .
		    THEN
		THEN
	    THEN emit

	    i (slice-range-high) @ = IF	\ range off, after displaying top
		default-color color-background
	    THEN
	LOOP
    -1 +LOOP ;


\ Bar graphic display of 'lines' lines without status line.
\ Used for ints *and* floats.  Set cursor before calling it.
: (statistic-display) ( addr lines -- max-slice-count )
    2dup swap statistic-slice-up-vertical >r
    \			( addr lines  r: max-slice-count )

    \ Special case: coloring a slide range during range definition
    defining-bar-range? IF
	(stat-displ-coloured-range)  r> EXIT		\ done
    THEN

    0 swap 2 - DO
	c-l 0 DO
	    i over n'th-vertical-boundary @ j > IF
		[char] *
	    ELSE
		bl

		\ in the last row, we want to show if there's something or not:
		j 0= IF					\ last row?
		    i  2 pick  n'th-slice-counter @ IF	\ something there?
			drop [char] .
		    THEN
		THEN
	    THEN emit
	LOOP
	cr	\ needed if console screen is wider than brews screen
    -1 +LOOP

    r> ;

\ Bar graphic display of 'lines' lines including status line.
\ Set cursor before calling it.
: statistic-display-int ( addr lines addr-of-title count-of-title -- )
    2>r (statistic-display)  2r> statistics-status-line-int ;

\ Bar graphic display of 'lines' lines including status line. Float version.
\ Set cursor before calling it.
: statistic-display-float ( addr lines addr-title count-title -- )
    2>r (statistic-display)  2r> statistics-status-line-float ;

[IF]

    2VARIABLE (min&max)           \ (min&max) 2@ gives ( -- min max )

    defer <data>
    2variable vertical-zoom-scale	     1 4 vertical-zoom-scale 2!
    ' vertical-zoom-scale is <vertical-zoom-scale>
    variable (vertical-display-range)    (vertical-display-range) off
    ' (vertical-display-range) is <vertical-display-range>

: t
    c-l statistic-array-size ( slices# -- size )
    allocate drop >r

    (min&max) min-max-init
\    (data) s-buf-clear
    c-l 0 DO i <data> (min&max) data2min-max LOOP
    (min&max) 2@ . . key drop
    (min&max) 2@ c-l r@ init-statistic-array-int

    r@
    c-l 0 DO
	i <data> over data2slice
    LOOP
    drop

    r@ l-s 1- s" test data." statistic-display-int
    r> free drop ;

\	c-l random-ranged
\	over data2slice

\	0 over data2slice
\	c-l 2/ over data2slice
\	c-l over data2slice

:NONAME c-l 2/ - ; IS <data>
:NONAME dup c-l 3 / MOD - ; IS <data>
:NONAME drop 0 ; IS <data>
:NONAME ; IS <data>

t
\ (data) s-buf-close
key drop \ bye

[THEN]

false [IF]
    float-check-field-length# cell+ allocate drop dfaligned
    VALUE float-check-field

    float-check-field float-check-field-length# erase
    float-check-field float-min-max-init

    100001 VALUE floats#

\ : n>float-data ( n -- float )  s>f ;

: n>float-data ( n -- float )  s>f fdup f* ;

\ : n>float-data ( n -- float )  s>f fdup fdup fdup f* f* f2/ f- ;

\ : n>float-data ( n -- float )
\     s>f
\     pi 2e0 f* floats# s>f f/ f* fsin ;

: fill-float-test-datas ( u -- )
    0 DO
	i n>float-data float-check-field float-data-check-in
    LOOP ;

    floats# fill-float-test-datas

    80 statistic-array-size allocate drop
    dup 80 statistic-array-size erase
    VALUE float-slice-data

    float-check-field >dfloat-min df@
    float-check-field >dfloat-max df@
    c-l
    float-slice-data
    init-statistic-array-dfloat ( F: min max  D: slices# addr -- )

    VARIABLE vertical-range	vertical-range off
    ' vertical-range IS <vertical-display-range>
    2VARIABLE vertical-zoom-scale	1 5 vertical-zoom-scale 2!
    ' vertical-zoom-scale IS <vertical-zoom-scale>
    VARIABLE vertical-display-range	vertical-display-range off
    ' vertical-display-range IS <vertical-display-range>

: slice-float-test-datas ( u -- )
    0 DO
	i n>float-data float-slice-data float-data2slice
    LOOP ;

    floats# slice-float-test-datas

    page
    float-slice-data
    24
    s" FLOAT DATA"
    statistic-display-float ( addr lines addr-of-title count-of-title -- )
CR BYE
[THEN]
