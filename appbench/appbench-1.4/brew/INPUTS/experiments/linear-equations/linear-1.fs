\ linear-1.fs
\ 	$Id: linear-1.fs,v 1.3 2002/09/21 04:28:36 f Exp $	

\ ****************************************************************
\ ***************  simple linear equation system  ****************
\ ****************************************************************

\ Compile only if the needed variables are defined:
nuc-organs# 1 >  spot-properties# 3 >  AND [IF]

\ Let's try a very simple equation system:

\ A - Bc = 0
\ A - d  = 0

\ I rate on the error |A - Bc| + |A -d|

\ While c and d are given and keep changing,
\ the cells should learn how to set A and B accordingly.

\ brew representation:
\ A :  organ-A		B :  organ-B		r/w nuc variables
\ c :  C-property	d :  D-property		r/o spot variables

\ That's my cheat:
s" -" GENE: cheat-linear-1
	D-property@ dup organ-A !
	C-property@ ?/ organ-B !  ;gene

internal' cheat-linear-1 wake-me-actions >list

: linear-1-error ( -- +n )		\ 2/ --> less overflow tricks here...
    organ-A @   organ-B @  C-property@ *  -  2/ abs
    organ-A @	D-property@ -		     2/ abs
    + ;

: score-linear-1 ( -- -score )   linear-1-error negate ;

: eat-linear-1 ( -- )   linear-1-error eat-scored ;
' score-linear-1  ' eat-linear-1  eat-actions 2>list


\ That's what brew found when I first tried:
\
\  : g-7535
\      D-property@ D-property@ C-property@ ?/ organ-B ! organ-A ! ;
\
\  Trying around I realize I was quite lucky with the parameters
\  I happened to take first ;-)


\ Some other genomes I have seen:
\  : g-59
\      organ-B @ C-property@ ?/ organ-B ! ;
\
\  : g-1352
\      organ-A @ C-property@ ?/ organ-B ! ;
\
\  this one made me smile: ;-)
\  : g-8890
\      organ-A take D-property@ organ-A ! C-property@ ?/ organ-B ! ;
\
\ this one came up over and over:
\ : g-5315
\      organ-B take organ-A ! ;


s" 'linear-1.fs' included.  Load initialisation file now."  1 >message

[ELSE]
    bell
    cr .( Compile options not fit for 'linear-1.fs'. )
    cr .( See source. )
    cr
    8000 wait-until
[THEN]
