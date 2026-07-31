\ linear-1-float.fs
\ 	$Id: linear-1-float.fs,v 1.4 2002/09/21 05:43:22 f Exp $	

\ ****************************************************************
\ ***************  simple linear equation system  ****************
\ ****************************************************************

\ dfloat version of linear-1.fs

\ Compile only if the needed variables are defined:
nuc-f-organs# 1 >  spot-f-properties# 3 >  AND [IF]

\ Let's try a very simple equation system:

\ A - Bc = 0
\ A - d  = 0

\ I rate on the error |A - Bc| + |A -d|

\ While c and d are given and keep changing,
\ the cells should learn how to set A and B accordingly.

\ brew representation:
\ A :  f-organ-A	B :  f-organ-B		r/w nuc variables
\ c :  C-f-property	d :  D-f-property	r/o spot variables

\ That's my cheat:
s" -" GENE: cheat-linear-1-float
	D-f-property@  fdup f-organ-A df!
	C-f-property@  f/   f-organ-B df!  ;gene

internal' cheat-linear-1-float wake-me-actions >list

: linear-1-float-error ( -- +n )
    f-organ-A df@  f-organ-B df@  C-f-property@ f*  f-  fabs-replace-NaN
    f-organ-A df@  D-f-property@  f-  fabs-replace-NaN
    f+ f>s-limited ;

: score-linear-1-float ( -- -score )   linear-1-float-error negate ;

: eat-linear-1-float ( -- )   linear-1-float-error eat-scored ;
' score-linear-1-float  ' eat-linear-1-float  eat-actions 2>list


INDIVIDUAL: linear-1-float ( -- )
' linear-1-float individuals >list

' eat-linear-1-float	eat-xt !
' cell-division	reproduce-xt !
' <look-at>	show-me-xt !
0 my-diversifctn-mask !
1000		reprodctn-threshold !
2		age-threshold !
-1 (genome-id) +!	\ cosmetic, nothing more...

s" 'linear-1-float.fs' included.  Load initialisation file now."  1 >message

[ELSE]
    bell
    cr .( Compile options not fit for 'linear-1-float.fs'. )
    cr .( See source. )
    cr
    8000 wait-until
[THEN]
