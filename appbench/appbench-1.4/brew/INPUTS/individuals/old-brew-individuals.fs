\ old-brew-individuals.fs
\ 	$Id: old-brew-individuals.fs,v 1.1 2002/05/23 13:38:12 f Exp $	

\ Relicts from very early brew development.
\ Some very old demos and benchmarks use it.

nuc-organs# [IF] \ at least organ-A defined.
    INDIVIDUAL: Yuppie
    ' Yuppie individuals >list

    ' cell-division reproduce-xt !
    1 my-diversifctn-mask !
    ' eat-part eat-xt !
    100 organ-A !
    ' show-A show-me-xt !	100 2-ascii-scale !
    2099	reprodctn-threshold !
    20	age-threshold !
[THEN] \ at least organ-A defined.

