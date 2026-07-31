\ global-5-sum.fs
\ 	$Id: global-5-sum.fs,v 1.2 2002/11/05 15:19:18 f Exp $	

\ The cell must add five global integer variables and put the result in organ-A
\ The tast is surprisingly difficult for brew, elitism does give a solution.

\ a possible solution:  organ-A = A + B + C + D + E
s" -" GENE: cheat-global-5-sum
    integer-A@ integer-B@ integer-C@ integer-D@ integer-E@
    + + + + organ-A !
;gene

internal' cheat-global-5-sum wake-me-actions >list

: score-global-5-sum ( -- -score )
    integer-B @ integer-C @ integer-D @ integer-E @ integer-F @ + + + +
    organ-A @ - abs negate ;
' score-global-5-sum scoring-xt !

: eat-global-5-sum ( -- )
    integer-B@ integer-C@ integer-D@ integer-E@ integer-F@ + + + +
    organ-A @ - abs eat-scored ;
' score-global-5-sum  ' eat-global-5-sum  eat-actions 2>list


INDIVIDUAL: sum-global-5-up ( -- )
' sum-global-5-up individuals >list

' eat-global-5-sum	eat-xt !
' cell-division	reproduce-xt !
' <look-at>	show-me-xt !
0 my-diversifctn-mask !
1000		reprodctn-threshold !
2		age-threshold !

s" 'global-5-sum.fs' included.  Load initialisation file or do it by hand."
1 >message
