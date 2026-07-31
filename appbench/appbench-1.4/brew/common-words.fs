\ common-words.fs
\ 	$Id: common-words.fs,v 1.10 2005/04/23 12:40:41 f Exp $	

\ This file defines some words commonly found in many Forth systems
\ if they are not defined yet.

\ ****************************************************************

decimal

[UNDEFINED] 1-   [IF]		: 1-   1 - ;				[THEN]
[UNDEFINED] CELL [IF]		1 cells CONSTANT CELL			[THEN]
[UNDEFINED] 0<=  [IF]		: 0<= ( n -- f )   1 < ;		[THEN]
[UNDEFINED] >=   [IF]		: >= ( n1 n2 -- f )   1- > ;		[THEN]
[UNDEFINED] <=   [IF]		: <= ( n1 n2 -- f )   1+ < ;		[THEN]
[UNDEFINED] d<>  [IF]		: d<> ( d1 d2 -- f )  d= 0= ;		[THEN]
[UNDEFINED] -ROT [IF]		: -ROT ( w1 w2 w3 -- w3 w1 w2) rot rot ; [THEN]
[UNDEFINED] clearstack [IF]	: clearstack   depth 0 ?DO drop LOOP ;	[THEN]

[UNDEFINED] maxaligned [IF]
    : maxaligned ( addr -- addr' )   aligned dfaligned ; \ hope that's right
[THEN]

\ [UNDEFINED] under+ [IF]
\     : under+ ( n1 n2 n3 -- n1+n3 n2 )  2>r r> + r> ;
\ [THEN]

[UNDEFINED] bell [IF]		: bell  7 emit ;			[THEN]
[UNDEFINED] ?	 [IF]		: ? ( addr -- )  @ . ;			[THEN]


[UNDEFINED] rdrop [IF]
    [UNDEFINED] r>drop [IF]
	: rdrop ( r: x -- r: - )   POSTPONE r>  POSTPONE drop ; IMMEDIATE
    [ELSE] \ Guido Draheim
	: rdrop ( r: x -- r: - )   POSTPONE r>drop ; IMMEDIATE
    [THEN]
[THEN]

[UNDEFINED] 2rdrop [IF]
    [UNDEFINED] 2r>drop [IF]
	: 2rdrop ( r: x1 x2 -- r: - ) POSTPONE rdrop POSTPONE rdrop ; IMMEDIATE
    [ELSE] \ Guido Draheim
	: 2rdrop ( r: x1 x2 -- r: - ) POSTPONE 2r>drop ; IMMEDIATE
    [THEN]
[THEN]

[UNDEFINED] 1/f  [IF]	: 1/f ( r -- r' )   1 fswap f/ ;		[THEN]
[UNDEFINED] f2*  [IF]	: f2* ( r -- r' )   2e0 f* ;			[THEN]
[UNDEFINED] f2/  [IF]	: f2/ ( r -- r' )   2e0 f/ ;			[THEN]
[UNDEFINED] fnip [IF]	: fnip ( r1 r2 -- r2 )   fswap fdrop ;		[THEN]
[UNDEFINED] pi   [IF]	: pi ( -- r )   3.14159265358979323e0 ;		[THEN]
[UNDEFINED] f>   [IF]	: f> ( r1 r2 -- flag )   fswap f< ;		[THEN]
[UNDEFINED] f0>  [IF]	: f0> ( r --flag )   0e0 f> ;			[THEN]
[UNDEFINED] f=   [IF]	: f= ( r1 r2 -- flag )  0e0 f~ ;		[THEN]
[UNDEFINED] f<>  [IF]	: f<> ( r1 r2 -- flag )  f= 0= ;		[THEN]
[UNDEFINED] f>=  [IF]
    : f>= ( r1 r2 -- flag )
	fover fover f> IF fdrop fdrop TRUE EXIT THEN
	f= ;
[THEN]
[UNDEFINED] f<=  [IF]
    : f<= ( r1 r2 -- flag )
	fover fover f< IF fdrop fdrop TRUE EXIT THEN
	f= ;
[THEN]

CREATE COMMON-WORDS.FS		\ mark, as REQUIRED might not work yet
