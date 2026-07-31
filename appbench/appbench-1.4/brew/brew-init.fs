\ brew-init.fs
\ 	$Id: brew-init.fs,v 1.24 2005/04/10 16:23:23 f Exp $	
\ default brew initialization

\ 'brew-init.fs' defines individuals and sets them into the current world.

\ Uncomment to generate the data below:
\ run-mode dup @ recording or swap !	|save-brew-variables|	goodbye
\ Then split result to 'brew-defaults' and 'brew-init.fs'
\ * 'brew-defaults' get's all defaults except individuals and living cells.
\ * 'brew-init.fs'  defines population.

\ ****************************************************************

\ quick&dirty initialisation for sum up experiment:

\ 'brew' recorded evolutionary session playback file.

\ brew was compiled with the following nuc compile time values:
\ 7 TO nuc-organs#
\ 5 TO nuc-parameters#
\ 3 TO nuc-invisibles#
\ 1 TO nuc-secrets#

\ brew was compiled with the following world compile time values:
\ 5 TO spot-qualities#
\ 1 TO spot-secrets#
\ 5 TO spot-properties#


\ starting from scratch:

\ restart with an unpopulated world:
free-field

\ define nuc and set it actual:
nuc-length# new-nucleus DROP cp!
\ needed for assertion of benchmark result:
0 cloned !
gene' noop    	wake-me-xt !
internal' noop    	wake-me-internal !
' eat-sum    	eat-xt !
' cell-division    	reproduce-xt !
' <look-at>    	show-me-xt !
0 id !
0 genome-id !
140 length !
nuc-length# length !
0 nuc-supplements !
0 nuc-flags !
0 age !
0 generation !
0 genome-generation !
100 code-cost !
0 energy !
1000 reprodctn-threshold !
2 age-threshold !
0 appearance !
257 my-diversifctn-mask !
0 organ-A !
0 organ-B !
0 organ-C !
0 organ-D !
0 organ-E !
0 organ-F !
0 organ-G !
0 parameter-A !
0 parameter-B !
0 parameter-C !
0 parameter-D !
0 parameter-E !
0 invisible-A !
0 invisible-B !
0 invisible-C !
0 secret-A !
0 div-organ-A OR div-parameter-B OR  my-diversifctn-mask !

\ sow above nuc:
\ random generator:
' random-BRODIE random-xt !
672302369 seed-BRODIE !
-1 (sow-diversified) !
9 sow drop time-step
nucs-not-scanned
\ needed for assertion of benchmark result:
36 cloned !

\ changed brew parameters:
-1449510237 seed-BRODIE !	 \ gets recorded changed or not 4
