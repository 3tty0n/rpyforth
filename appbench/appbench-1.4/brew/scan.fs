\ scan.fs

\ 	$Id: scan.fs,v 1.1 2005/04/14 13:24:59 f Exp $	

\ new scanning mechanism
\ a bit tested, but not used in brew yet
\ this first version does all the scanning in float
\ (I think I want int too).  DADA ############

VARIABLE (scan-data)	\ will be in a field

' (scan-data) 0
POINTER+OFFSET: scan-detail-xt    \ i.e. organ-A B-parameter score 
POINTER+OFFSET: scan-fetch-xt     \ i.e. noop @ df@
POINTER+OFFSET: scan-detail-type  \ i.e. type-int% type-df% type-int-addr%
POINTER+OFFSET: scan-fetch-type   \ i.e. type-int% type-df%
POINTER+OFFSET: scan-locality     \ i.e. nuc-local% spot-local%
POINTER+OFFSET: scan-loop-xt	     \ i.e. do-with-everybody do-everywhere

\ prescan data:
dup -rot

POINTER+OFFSET: prescan-count
POINTER+dfloatOFFSET: prescan-min
POINTER+dfloatOFFSET: prescan-max
POINTER+dfloatOFFSET: prescan-average	\ sum during prescan

rot over swap - CONSTANT prescan-data-size#

\ slice data
POINTER+OFFSET: slices
POINTER+OFFSET: out-of-range
POINTER+dfloatOFFSET: slice-min
POINTER+dfloatOFFSET: slices-range
maxaligned POINTER+OFFSET: slice-data-pointer	\ start address of slices
cell -
CONSTANT scan-data-size#			\ *without* slices
drop


\ Allocate and erase memory for scans, set the data pointer, initialize slices.
: allocate-scan-data ( pointer-addr slices -- )
    >r
    r@ cells  scan-data-size#  + allocate-clean
    swap !
    r> slices ! ;

(scan-data) c-l allocate-scan-data


: fetch-int-as-float ( -- r )   scan-detail-xt @ EXECUTE s>f ;

: fetch-int-addr-as-float ( -- r )   scan-detail-xt @ EXECUTE @ s>f ;

: set-detail ( detail-xt detail-type -- )
    dup scan-detail-type !
    CASE
	type-int% OF
	    ['] fetch-int-as-float scan-fetch-xt !
	    type-int% scan-fetch-type !
	ENDOF
	type-int-addr% OF
	    ['] fetch-int-addr-as-float scan-fetch-xt !
	    type-int% scan-fetch-type !
	ENDOF

	ABORT
    ENDCASE
    scan-detail-xt ! ;

: set-locality ( locality-code -- )
    dup scan-locality !
    CASE
	nuc-local% OF
	    ['] do-with-everybody scan-loop-xt !
	    EXIT
	ENDOF
	spot-local%	OF
	    ['] do-everywhere scan-loop-xt !
	    EXIT
	ENDOF
	ABORT
    ENDCASE ;

: +count ( -- )   1 prescan-count +! ;

: prescan-process-float ( F: r -- )
    fdup  prescan-average  dup >r  df@ f+ r> df! \ sum up in prescan-average
    fdup  prescan-max  dup >r  df@  fmax  r> df! \ max
    prescan-min  dup >r  f@  fmin  r> f!	 \ min
    +count ;

: prescan-fetch&store-1 ( -- )
    scan-fetch-xt @ EXECUTE
    prescan-process-float ;

: init-prescan ( -- )
    0 prescan-count !
    0e0 prescan-average df!	\ used as sum during prescan
    +infinity prescan-min df!
    -infinity  prescan-max df! ;

: compute-average ( -- )
    prescan-average dup >r
    df@  fdup f0= IF  fdrop rdrop EXIT  THEN
    prescan-count @ s>f  f/  r> df! ;

: prescan ( -- )
    init-prescan
    ['] prescan-fetch&store-1  scan-loop-xt @  EXECUTE
    compute-average ;


: (set-int-var) ( xt -- )   type-int-addr%  set-detail ;

: set-nuc-int-var ( xt -- )   (set-int-var)  nuc-local% set-locality ;

: set-spot-int-var ( xt -- )   (set-int-var)  spot-local% set-locality ;


: (set-int-funct) ( xt -- )   type-int%  set-detail ;

: set-nuc-int-funct ( xt -- )   (set-int-funct)  nuc-local% set-locality ;

: set-spot-int-funct ( xt -- )   (set-int-funct)  spot-local% set-locality ;


: init-slicing ( -- )		\ data must be prescanned
    \ slices			\ set by allocate-scan-data

    \    erase ################# DADA
    out-of-range off
    prescan-max df@
    prescan-min df@
    fdup slice-min df!		\ zooming???? DADA
    f-

    \ fix broken ranges
    scan-fetch-type @ CASE
	type-int% OF
	    \ range smaller than screen width; take a one to one relationship
	    c-l 1- s>f fmax
	ENDOF
	type-df% OF
	    fdup f0= IF  fdrop  1e0  THEN
	ENDOF
	ABORT
    ENDCASE

    slices-range df! ;

: n'th-slice ( n -- n'th-count-addr )   cells  slice-data-pointer  + ;

: data-to-slice ( F: r -- )
    slice-min df@ f-		( F: offset-from-min )
    slices-range df@ f/
    slices @ s>f f* f>s		( slice )

    dup 0< IF			\ below range
	1 out-of-range +!
	drop 0
    THEN

    dup slices @ 1- > IF	\ above range
	1 out-of-range +!
	drop
	slices @ 1-
    THEN

    1 swap n'th-slice +! ;

: fetch&slice-1 ( -- )   scan-fetch-xt @ EXECUTE  data-to-slice ;

: slice-up ( -- )
    init-slicing
    ['] fetch&slice-1  scan-loop-xt @  EXECUTE ;

: do-scan ( -- )   prescan slice-up ;

true [if] \ testing

    : .xt ( xt -- )
	dup 0= IF ." uninitialized xt" drop EXIT THEN
	xt>string type ;

    : .scandata ( -- )
	scan-detail-xt @	cr ." scan-detail-xt       " .xt
	scan-fetch-xt @		cr ." scan-fetch-xt        " .xt
	scan-detail-type @	cr ." scan-detail-type     "
	var-type-string type
	scan-fetch-type @	cr ." scan-fetch-type      "
	var-type-string type
	scan-locality @		cr ." scan-locality        "
	locality-string type
	scan-loop-xt @		cr ." scan-loop-xt         " .xt
	prescan-count @		cr ." prescan-count        " .
	prescan-min df@		cr ." prescan-min          " f.
	prescan-max df@		cr ." prescan-max          " f.
	prescan-average df@	cr ." prescan-average      " f.
	slice-data-pointer      cr ." slice-data-pointer   " .
	slices @		cr ." slices               " .
	out-of-range @		cr ." out-of-range         " .
	slice-min df@		cr ." slice-min            " f.
	slices-range df@        cr ." slices-range         " f.

	cr
	slices @ 0 DO   i n'th-slice @ .   LOOP
    ;

    : ttt
	page
	['] organ-A set-nuc-int-var
	prescan
	do-scan
	.scandata
	;

[then]
