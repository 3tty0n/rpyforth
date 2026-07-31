\ global-3-sum.fs
\ 	$Id: global-3-sum.fs,v 1.1 2002/09/20 16:53:15 f Exp $	

\ The cells must add three global integer variables
\ and put the result in organ-A:
\ A = B + C + D

\ a possible solution:  organ-A = B + C + D
s" -" GENE: cheat-global-3-sum
    integer-B@ integer-C@ integer-D@ + + organ-A !
;gene

internal' cheat-global-3-sum wake-me-actions >list

: score-global-3-sum ( -- -score )
    integer-B @ integer-C @ integer-D @ + + organ-A @ - abs negate ;
' score-global-3-sum scoring-xt !

: eat-global-3-sum ( -- )
    integer-B@ integer-C@ integer-D@ + +
    organ-A @ - abs eat-scored ;
' score-global-3-sum  ' eat-global-3-sum  eat-actions 2>list


INDIVIDUAL: sum-global-3-up ( -- )
' sum-global-3-up individuals >list

' eat-global-3-sum	eat-xt !
' cell-division	reproduce-xt !
' <look-at>	show-me-xt !
0 my-diversifctn-mask !
1000		reprodctn-threshold !
2		age-threshold !

s" 'global-3-sum.fs' included.  Load initialisation file or do it by hand."
1 >message
