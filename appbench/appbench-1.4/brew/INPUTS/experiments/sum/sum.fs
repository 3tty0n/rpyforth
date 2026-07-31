\ sum.fs
\ 	$Id: sum.fs,v 1.5 2002/09/20 12:53:04 f Exp $	

\ Defines some words for the 'sum' experiment.
\ Don't load this directly, load a corresponding initialisation file instead.

false [IF] \ Comment:

    Let's try something very simple:
    The cells must sum up two numbers 'A = B + C'
    We rate on the absolute miss.

    In the beginning of the experiment A B C are small integer numbers.
    They are drifting randomly away from zero.
    Nucs (Cells) that happen to have a triple with a small absolute value of
    B + C - A have an advantage.
    They will get more energy, food or whatever, reproduce earlier and
    increase in number.
    The others tend to disappear.

    But A B C are of three very different qualities:

    A and B are part of the nuc.
    They are herditary, and can get varied a bit during reproduction.

    C is beyond the control of the nuc,
    it's an variable of the virtual spot the nuc is sitting on.

    So a child will normally have similar A and B, but very different
    C value than the mother cell.

    The genome starts by doing noop and gets mutatet then.
    While the gene primitives can read all three values A B C
    they provide write access only to the A variable.
    Mutation has it's chance to find out that it can manipulate A
    in a way that will be useful for the cell.

    As C drifts away from zero it becomes more and more important
    to find an effective algoritm how to set A.

    Brew sees A B C as follows:
    A is organ-A
    B is parameter-B
    C is (spot) C-property
    (I kept the letters A B and C for mnemonic reasons).

    a possible solution ;-)
    A = B + C

    My proposed cheat:
    s" -" GENE: cheat-sum   parameter-B@ C-property@ + organ-A ! ;gene

    This is what brew comes up with in the current initialisation:

    \ spot:587 step:168 mother ID:20606
    : mutation.168:587.GI:87.to-GI:207 ( -- )
      C-property@	( n)
      parameter-B@	( nn)
      +		( n)
      organ-A	( na)
      !		( )
    ;

    ****************************************************************

    Typical results (of an older initialisation):

	MUTATION:  based on ID:2117 GI:0  at spot 1492  in step 17
	==> new genome built:
	C-property@ organ-A ! ;gene
	code-cost: 400
	MUTATION: step 17: nuc at spot 1492       mutated to child at 1413

	MUTATION:  based on ID:11758 GI:0  at spot 340  in step 97
	==> new genome built:
	parameter-B@ organ-A ! ;gene
	code-cost: 400
	MUTATION: step 97: nuc at spot 340        mutated to child at 339

	MUTATION:  based on ID:108377 GI:27  at spot 1208  in step 934
	==> new genome built:
	C-property@ parameter-B@ + organ-A ! ;gene 
	code-cost: 600
	MUTATION: step 934: nuc at spot 1208	  mutated to child at 1288

[THEN] \ End comment, code follows:


\ ****************************************************************
\ ***************************  sum  ******************************
\ ****************************************************************

\ Compile only if the needed variables are defined:
nuc-organs#  0 >
nuc-parameters# 1 > AND
spot-properties# 2 >  AND [IF]

\ a possible solution:  A = B + C
s" -" GENE: cheat-sum   parameter-B@ C-property@ + organ-A ! ;gene

internal' cheat-sum wake-me-actions >list

: score-sum ( -- -score )
    parameter-B @ C-property @ +  organ-A @ -  abs negate ;

: eat-sum ( -- )
    parameter-B @ C-property @ +  organ-A @ -  abs eat-scored ;
' score-sum  ' eat-sum  eat-actions 2>list

INDIVIDUAL: sum-up ( -- )
' sum-up individuals >list

' eat-sum	eat-xt !
' cell-division	reproduce-xt !
' <look-at>	show-me-xt !
div-organ-A div-parameter-B or my-diversifctn-mask !
1000		reprodctn-threshold !
2		age-threshold !

s" 'sum.fs' included.  Load initialisation file or do it by hand."  1 >message

[ELSE]
    bell
    cr .( Compile options not fit for 'sum.fs'. )
    cr .( See source. )
    cr
    8000 wait-until
[THEN]

\ ****************************************************************
\ end	sum
