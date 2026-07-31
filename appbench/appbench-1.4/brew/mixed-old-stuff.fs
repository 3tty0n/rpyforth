\ mixed-old-stuff.fs
\ 	$Id: mixed-old-stuff.fs,v 1.1 2002/05/23 13:35:20 f Exp $	

\ Factored out from brew.fs
\ Old stuff which well be obsolete soon, when the old benchmars, demos
\ and benchmarks will get dropped.

\ ****************************************************************
\ ********************  mixed old stuff  *************************
\ ****************************************************************

nuc-organs# [IF] \ at least organ-A defined.

\ color plays:
: eat-red ( -- )
    <food> @ organ-A @ min	\ doesn't eat more than organ-A
    A-quality @ +		\ red makes hungry
    0 max			\ no vomiting (one could leave this out)

    \ this doesn't belong here, but it's simplest
    dup 10 / negate
    dup future A-quality +!		\ store at future *and* present to be
    present A-quality +!		\ independent of future-quality-change

    dup energy +!
    negate <food> +! ;
' eat-red eat-actions >list

: eat-blue ( -- )
    <food> @ organ-A @ min	\ doesn't eat more than organ-A
    A-quality @ -		\ blue makes hungry
    0 max			\ no vomiting (one could leave this out)

    \ this doesn't belong here, but it's simplest
    dup 10 /
    dup future A-quality +!	\ store at future *and* present to be
    present A-quality +!	\ independent of future-quality-change

    dup energy +!
    negate <food> +! ;
' eat-blue eat-actions >list

INDIVIDUAL: red-eater
' red-eater individuals >list

' cell-division reproduce-xt !

\ ' diversify?-A IS <diversify>
1 my-diversifctn-mask !
50 diversification-range !
1 4 diversification-rate 2!
' noop is <diversify>

' eat-red eat-xt !
100 organ-A !
char * appearance !
' show-ascii show-me-xt !	100 2-ascii-scale !
200 food-share/spot !
695 reprodctn-threshold !
10  age-threshold !


INDIVIDUAL: blue-eater
' blue-eater individuals >list

' cell-division reproduce-xt !

\ ' diversify?-A IS <diversify>
1 my-diversifctn-mask !
50 diversification-range !
1 4 diversification-rate 2!
' noop is <diversify>

' eat-blue eat-xt !
100 organ-A !
char . appearance !
' show-ascii show-me-xt !	100 2-ascii-scale !
200 food-share/spot !
695 reprodctn-threshold !
10  age-threshold !

spot-display-on display-switch !
1 additive-stress !

false [IF]
    INDIVIDUAL: quick-red-eater
    ' quick-red-eater individuals >list

    ' cell-division reproduce-xt !
\    ' diversify?-A IS <diversify>
    1 my-diversifctn-mask !
    50 diversification-range !
    1 4 diversification-rate 2!
    ' eat-red eat-xt !
    100 organ-A !
    char * appearance !
    ' show-ascii show-me-xt !	100 2-ascii-scale !
    200 food-share/spot !
    343 reprodctn-threshold !
    4  age-threshold !


    INDIVIDUAL: quick-blue-eater
    ' quick-blue-eater individuals >list

    ' cell-division reproduce-xt !
\    ' diversify?-A IS <diversify>
    1 my-diversifctn-mask !
    50 diversification-range !
    1 4 diversification-rate 2!
    ' eat-blue eat-xt !
    100 organ-A !
    char . appearance !
    ' show-ascii show-me-xt !	100 2-ascii-scale !
    200 food-share/spot !
    343 reprodctn-threshold !
    4  age-threshold !
[THEN]
[THEN] \ at least organ-A defined.

false [IF]
    INDIVIDUAL: wanderer
    ' wanderer individuals >list
    new-gene [IF]
	internal' move?  setup-wake-me
    [ELSE]
	' move? setup-wake-me
    [THEN]
    ' noop reproduce-xt !

    ' noop is <diversify>
    ' eat-all eat-xt !
    0 my-diversifctn-mask !
    char * appearance !
    ' show-ascii show-me-xt !
    200 food-share/spot !
    343 reprodctn-threshold !
    1000  age-threshold !
    \ nuc-do-cost off
[THEN]

nuc-organs# [IF] \ at least organ-A defined.

\ I wonder if this symbiotic species couldn't emerge all by themselves?
: eat-violet ( -- )
    organ-A @ 0< 2* 1+ >r	\ sign of organ-A as +1/-1 on return stack
    <food> @ organ-A @ abs min	\ doesn't eat more than abs(organ-A)
    A-quality @ r@ * -
    10 max			\ but not less than 10
    dup energy +!
    dup negate <food> +!

    \ this doesn't belong here, but it's simplest
    r> * 10 /
    dup future A-quality +!	\ store at future *and* present to be
    present A-quality +! ;	\ independent of future-quality-change
' eat-violet eat-actions >list

: eat-lila ( -- )
    organ-A @ 0< 2* 1+ >r	\ sign of organ-A as +1/-1 on return stack
    <food> @ organ-A @ abs min	\ doesn't eat more than abs(organ-A)
    A-quality @ r@ * -
    <food> @ min		\ if there's enough food at all...
    10 max			\ but not less than 10
    dup energy +!
    dup negate <food> +!

    \ this doesn't belong here, but it's simplest
    r> * 10 /
    dup future A-quality +!	\ store at future *and* present to be
    present A-quality +! ;	\ independent of future-quality-change
' eat-lila eat-actions >list

INDIVIDUAL: violet-eater
' violet-eater individuals >list

' show-sign-A show-me-xt !
' cell-division reproduce-xt !
\ ' diversify?-A IS <diversify>
1 my-diversifctn-mask !
50 diversification-range !
1 4 diversification-rate 2!
' eat-violet eat-xt !
0 organ-A !
200 food-share/spot !
100 reprodctn-threshold !
10  age-threshold !

false [IF]
    160 low-water-mark !
    big-bang
    1 sow drop
\ wow! it's working like expected! I had to increase the low water mark a bit.
[THEN]

VARIABLE (A-scale)		100 (A-scale) !
: eat-violet-remade ( -- )
    eat-all
    organ-A @  A-quality @  (A-scale) @ */
    energy +!
    organ-A @ negate
    dup future A-quality +! present A-quality +! ;	\ changes A-quality
' eat-violet-remade eat-actions >list

: eat-simple-violet-remade ( -- )
    eat-all
    organ-A @  A-quality @  *   0< 2* 1+ >r		\ same sign?
    organ-A @ abs  A-quality @ abs  min			\ the smaller one
    1 max						\ to let things start
    r> *
    energy +!
    organ-A @ negate
    dup future A-quality +! present A-quality +! ;	\ changes A-quality
' eat-simple-violet-remade eat-actions >list

[THEN] \ at least organ-A defined.

nuc-organs# 2 > [IF] \ at least three organs defined.

VARIABLE (B-scale)		100 (B-scale) !
VARIABLE (C-scale)		100 (C-scale) !
: eat-rainbow-remade ( -- )
    eat-all
    organ-A @  A-quality @  (A-scale) @ */
    organ-B @  B-quality @  (B-scale) @ */
    organ-C @  C-quality @  (C-scale) @ */
    + + energy +!
    organ-A @ negate
    dup future A-quality +! present A-quality +!	\ changes A-quality
    organ-B @ negate
    dup future B-quality +! present B-quality +!	\ changes B-quality
    organ-C @ negate
    dup future C-quality +! present C-quality +! ;	\ changes C-quality
' eat-rainbow-remade eat-actions >list

: eat-cyclic-remade ( -- )
    eat-all
    organ-A @  A-quality @  (A-scale) @ */
    organ-B @  B-quality @  (B-scale) @ */
    organ-C @  C-quality @  (C-scale) @ */
    + + energy +!
    organ-A @ negate
    dup future B-quality +! present B-quality +!	\ changes B-quality
    organ-B @ negate
    dup future C-quality +! present C-quality +!	\ changes C-quality
    organ-C @ negate
    dup future A-quality +! present A-quality +! ;	\ changes A-quality
' eat-cyclic-remade eat-actions >list

: eat-cyclic-back-clock ( -- )
    eat-all
    organ-A @  A-quality @  (A-scale) @ */
    organ-B @  B-quality @  (B-scale) @ */
    organ-C @  C-quality @  (C-scale) @ */
    + + energy +!
    future
    organ-A @ negate
    dup future C-quality +! present C-quality +!	\ changes C-quality
    organ-B @ negate
    dup future A-quality +! present A-quality +!	\ changes A-quality
    organ-C @ negate
    dup future B-quality +! present B-quality +! ;	\ changes B-quality
' eat-cyclic-back-clock eat-actions >list

\ wow, this symbiotic biotop was fascinating!
\ want to try more komplex things:

: eat-rainbow ( -- )
    organ-A @ 0< 2* 1+ pad !		\ sign of organ-A as +1/-1 at pad
    organ-B @ 0< 2* 1+ pad cell+ !	\ sign of organ-B as +1/-1 at pad +cell
    organ-C @ 0< 2* 1+ pad 2 cells + !	\ sign of organ-C as +1/-1 at pad
    <food> @
    organ-A @ abs min			\ doesn't eat more than abs(organ-A)
    organ-B @ abs min			\ and not more than abs(organ-B)
    organ-C @ abs min			\ and not more than abs(organ-B)

    A-quality @ pad           @ * -
    B-quality @ pad cell+     @ * -
    C-quality @ pad 2 cells + @ * -

    10 max			\ but not less than 10
    dup energy +!
    dup negate <food> +!

    dup pad	      @ * 10 /
    dup future A-quality +! present A-quality +!
    dup pad   cell+   @ * 10 /
    dup future B-quality +! present B-quality +!
    pad 2 cells + @ * 10 /
    dup future C-quality +! present C-quality +! ;

\ to be honest: I have no idea what it does...
\ I just made it up like this. let's try:
' eat-rainbow eat-actions >list

: show-ABC
    organ-A @ abs organ-B @ abs > IF
	organ-A @ abs organ-C @ abs > IF
	    [char] A organ-A @
	ELSE
	    [char] C organ-C @
	THEN
    ELSE
	organ-B @ abs organ-C @ abs > IF
	    [char] B organ-B @
	ELSE
	    [char] C organ-C @
	THEN
    THEN
    dup 0= IF drop [char] 0 THEN	\ all three organs are zero
    0< IF 32 + THEN		\ capitals or not? (sign of strongest quality)
    .ascii ;
' show-ABC show-me-actions >list

\ more beauty, please
: show-ABC*.
    organ-A @ abs organ-B @ abs > IF
	organ-A @ abs organ-C @ abs > IF
	    [char] .
	    \ organ-A @
	ELSE
	    [char] *
	    \ organ-C @
	THEN
    ELSE
	organ-B @ abs organ-C @ abs > IF
	    [char] ,
	    \ organ-B @
	ELSE
	    [char] *
	    \ organ-C @
	THEN
    THEN
    dup 0= IF drop [char] 0 THEN	\ all three organs are zero
\    0< IF 32 + THEN		\ capitals or not? (sign of strongest quality)
    .ascii ;
' show-ABC*. show-me-actions >list

\ one more
: show-ABC-X|
    organ-A @ abs organ-B @ abs > IF
	organ-A @ abs organ-C @ abs > IF
	    [char] -
	    \ organ-A @
	ELSE
	    [char] +
	    \ organ-C @
	THEN
    ELSE
	organ-B @ abs organ-C @ abs > IF
	    [char] '
	    \ organ-B @
	ELSE
	    [char] X
	    \ organ-C @
	THEN
    THEN
    dup 0= IF drop [char] 0 THEN	\ all three organs are zero
\    0< IF 32 + THEN		\ capitals or not? (sign of strongest quality)
    .ascii ;
' show-ABC-X| show-me-actions >list

\ another attempt
: show-~|'.*
    organ-A @ abs organ-B @ abs > IF
	organ-A @ abs organ-C @ abs > IF
	    organ-A @ 0< IF [char] -
	    ELSE	    [char] ~
	    THEN
	ELSE
	    organ-C @ 0< IF [char] '
	    ELSE	    [char] |
	    THEN
	THEN
    ELSE
	organ-B @ abs organ-C @ abs > IF
	    organ-B @ 0< IF [char] .
	    ELSE	    [char] *
	    THEN
	ELSE
	    organ-C @ 0< IF [char] '
	    ELSE	    [char] |
	    THEN
	THEN
    THEN
    .ascii ;
' show-~|'.* show-me-actions >list

INDIVIDUAL: rainbow-eater
' rainbow-eater individuals >list

' show-ABC show-me-xt !
' cell-division reproduce-xt !
\ :noname diversify?-A diversify?-B diversify?-C ; IS <diversify>
7 my-diversifctn-mask !
50 diversification-range !
1 4 diversification-rate 2!
' eat-rainbow eat-xt !

0 organ-A !

false [IF]
    200 food-share/spot !
    100 reprodctn-threshold !
    10  age-threshold !
    \ nuc-do-cost off
    big-bang
[THEN]

: eat-cyclic-rainbow ( -- )
    organ-A @ 0< 2* 1+ pad !		\ sign of organ-A as +1/-1 at pad
    organ-B @ 0< 2* 1+ pad cell+ !	\ sign of organ-B as +1/-1 at pad +cell
    organ-C @ 0< 2* 1+ pad 2 cells + !	\ sign of organ-C as +1/-1 at pad
    <food> @
    organ-A @ abs min			\ doesn't eat more than abs(organ-A)
    organ-B @ abs min			\ and not more than abs(organ-B)
    organ-C @ abs min			\ and not more than abs(organ-B)
    
    A-quality @ pad cell+     @ * -	\ takes deliberately the wrong sign
    B-quality @ pad 2 cells + @ * -
    C-quality @ pad	      @ * -

    10 max			\ but not less than 10
    dup energy +!
    dup negate <food> +!

    dup pad 2 cells + @ * 10 /	\ and the third wrong sign
    dup future A-quality +! present A-quality +!
    dup pad	      @ * 10 /
    dup future B-quality +! present B-quality +!
    pad       cell+   @ * 10 /
    dup future C-quality +! present C-quality +! ;

\ uups, my imagination is too restricted to understand what it does ;-)
' eat-cyclic-rainbow eat-actions >list
' eat-cyclic-rainbow eat-xt !

: eat-crazy-rainbow ( -- )
    organ-A @ 0< 2* 1+ pad !		\ sign of organ-A as +1/-1 at pad
    organ-B @ 0< 2* 1+ pad cell+ !	\ sign of organ-B as +1/-1 at pad +cell
    organ-C @ 0< 2* 1+ pad 2 cells + !	\ sign of organ-C as +1/-1 at pad
    <food> @
    organ-A @ pad 2 cells + @ * +	\ just a set of crazy rules ;-)
    organ-B @ pad	    @ * +
    organ-C @ pad   cell+   @ * +
    
    A-quality @ pad cell+     @ *	pad           @ *  -	\ crazy, crazy
    B-quality @ pad 2 cells + @ *	pad   cell+   @ *  -
    C-quality @ pad	      @ *	pad 2 cells + @ *  -

    10 max			\ but not less than 10
    dup energy +!
    dup negate <food> +!

    dup pad 2 cells + @ * 10 /
    dup future A-quality +! present A-quality +!
    dup pad	      @ * 10 /
    dup future B-quality +! present B-quality +!
        pad   cell+   @ * 10 /
    dup future C-quality +! present C-quality +! ;

\ wonder what we will get!
' eat-crazy-rainbow eat-actions >list
\ ' eat-crazy-rainbow eat-xt !

[THEN] \ at least three organs defined.


nuc-organs# [IF] \ at least organ-A defined.

: violet-eater-2
    A-quality @   dup >r   organ-A @   dup >r -
    dup future A-quality ! present A-quality !
    r> 0< 2* 1+   r> 0< 2* 1+  * ( sign )
    organ-A @ *  negate <food> @ + energy +!
    <food> off ;
' violet-eater-2 eat-actions >list

: violet-eater-3
    <food> @ energy +!
    <food> off
    A-quality @   0< 2* 1+
    organ-A @ *   energy +!
    organ-A @ negate
    dup future A-quality +! present A-quality +! ;
' violet-eater-3 eat-actions >list

: violet-eater-4
    <food> @   organ-A @ abs   -   energy +!
    organ-A @ 0< 2* 1+
    A-quality @ *  energy +!
    <food> off
    organ-A @ negate
    dup future A-quality +! present A-quality +! ;
' violet-eater-4 eat-actions >list

[THEN] \ at least organ-A defined.


nuc-organs# 2 > [IF] \ at least three organs defined.

: violet-eater-5
    <food> @ energy +!
    <food> off
    organ-A @ abs negate energy +!
    organ-A @ 0< 2* 1+ ( signA)    A-quality @ *  energy +!
    organ-B @ abs negate energy +!
    organ-B @ 0< 2* 1+ ( signB)    B-quality @ *  energy +!
    organ-C @ abs negate energy +!
    organ-C @ 0< 2* 1+ ( signC)    C-quality @ *  energy +!

    organ-A @ negate
    dup future A-quality +! present A-quality +!
    organ-B @ negate
    dup future B-quality +! present B-quality +!
    organ-C @ negate
    dup future C-quality +! present C-quality +! ;
' violet-eater-5 eat-actions >list

' violet-eater-5 eat-xt !
\ :noname diversify?-A diversify?-B diversify?-C ; IS <diversify>
0 organ-A !

false [IF]
    big-bang
    1 sow drop
    10 food-share/spot !
    \ this one has a fascinating result (after a long time)
    ' generation>color foreground-color-xt !
[THEN]

[THEN] \ at least three organs defined.

\ ****************************************************************
\ end	mixed old stuff
