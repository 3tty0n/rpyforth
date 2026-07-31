\ sum-init.fs
\ 	$Id: sum-init.fs,v 1.14 2005/04/10 16:20:29 f Exp $	

\ This is an initialisation file corresponding to 'sum.fs'.

\ Check if the needed variables are defined:
nuc-organs#  0 >
nuc-parameters# 1 > AND
spot-properties# 2 >  AND [IF]

s" INPUTS/experiments/sum/sum.fs" required

false [IF] \ This is what brew comes up with in the current initialisation:

    \ spot:587 step:168 mother ID:20606
    : mutation.168:587.GI:87.to-GI:207 ( -- )
      C-property@	( n)
      parameter-B@	( nn)
      +		( n)
      organ-A	( na)
      !		( )
    ;

    While good code is found in mutation.168:587.GI:87.to-GI:207 at step:168
    it takes a total of over 800 steps until it becomes the predominant winner.

    Initialization and selection conditions could probably be much better.

    ****************************************************************
    \ This is the resulting geneology:
    \ (ID's might have a constant offset).

    : mutation.65:261.GI:31.to-GI:87 ( -- ) \ spot:261 step:65 mother ID:8217
      \ mutation type: restart-from-scratch
      C-property@	( n)
      parameter-B@	( nn)
      +		( n)
      parameter-B@	( nn)
      parameter-A@	( nnn)
      C-property@	( nnnn)
      +		( nnn)
      +		( nn)
      C-property@	( nnn)
      ?/		( nn)
      -		( n)
      organ-A	( na)
      !		( )
    ;		( )

    \ This is the final code produced.
    \ Not very successful ancestor of the winner.
    : mutation.168:587.GI:87.to-GI:207 ( -- ) \ spot:587 step:168 mother 20606
      \ mutation type: top-level-random-snip
      C-property@	( n)
      parameter-B@	( nn)
      +		( n)
      organ-A	( na)
      !		( )
    ;		( )

    \ Same genome reproduced by a noop mutation happened to be more successful:
    \ spot:1704 step:311 mother ID:37181
    : mutation.311:1704.GI:207.to-GI:373 ( -- )
      \ mutation type: top-level-replacement
      C-property@	( n)
      parameter-B@	( nn)
      +		( n)
      organ-A	( na)
      !		( )
    ;		( )

[THEN]

\ 'brew' recorded evolutionary session playback file.
\
\  created with brew version:
\ 	brew.fs,v 1.300 2002/01/05 15:49:22 f 
\ 	genes-0.3.fs,v 1.13 2001/12/07 22:06:10 f 
\ 	mutation-0.3.fs,v 1.30 2001/12/31 10:37:15 f 

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

\ Save brew variables:
base @  decimal

\ brew general settings:
1 world-do-direction !
0 (linear-index) !
' noop spot-do-xt !
' noop cell-do-before-xt !
' noop cell-do-after-xt !
' noop step-do-before-xt !
' noop step-do-after-xt !
-1 future-quality-change !
0 cell-division-moves-both !
0 cell-division-diversify-both !
0 cell-division-mutate-both !
0  log-mask !
0 file-code OR file-stack OR  code-file-mask !
\ random generator:
' random-BRODIE random-xt !
272958469 0 (random-generalized) 2!
-1449510237 seed-BRODIE !	 \ gets recorded changed or not 3

\ world:
80 dimension-ranges 0 cells + ! 
24 dimension-ranges 1 cells + ! 
0 dimension-ranges 2 cells + ! 
0 dimension-ranges 3 cells + ! 
0 dimension-ranges 4 cells + ! 
0 visibility-on 0 cells + ! 
0 visibility-on 1 cells + ! 
0 visibility-on 2 cells + ! 
0 visibility-on 3 cells + ! 
0 visibility-on 4 cells + ! 
80 visibility-off 0 cells + ! 
24 visibility-off 1 cells + ! 
0 visibility-off 2 cells + ! 
0 visibility-off 3 cells + ! 
0 visibility-off 4 cells + ! 
0 backgound-off !

\ food, costs, population control:
100000 world-food-supply !
10 food-share/spot !
0 individual-fixed-food-share !
0 nuc-do-cost !
0 code-price !
2 100 code-price-scale 2!
0 leave-energy-after-death !
1 additive-stress !
4 additive-release !
1 1 stress-rate 2!
4 multiplicative-release !
1 code-additive-stress !
1 1 code-stress-rate 2!
640 high-water-mark !
1728 flood-mark !
110 100 flood-stress-rate 2!
0 1 flood-kill-rate 2!
0 1 flood-energy-rate 2!
9 10 flood-food-rate 2!
76 sos-mark !
1 sos-sow !
1 2 sos-release-rate 2!
1 sos-reproduction-push !
100 low-water-mark !
1000 up-regulation-start !
-1 nuc-cost-can-be-help? !
-1 code-price-can-be-help? !
1 1 score-rate 2!

120 fixed-population-size !
40 elite !

\ diversification:
0 div-organ-A OR div-organ-B OR div-organ-C OR 
div-organ-D OR div-organ-E OR div-organ-F OR 
div-organ-G OR div-parameter-A OR div-parameter-B OR 
div-parameter-C OR div-parameter-D OR div-parameter-E OR 
div-invisible-A OR div-invisible-B OR div-invisible-C OR 
 diversification-mask !
500 diversification-range !
1 4 diversification-rate 2!
2 nuc-diversification-closeness !
65536 sporadic-value-range !
1 100 sporadic-value-rate 2!
0 C-property-div OR  spot-diversification-mask !
500 spot-diversification-range !
2 spot-diversifictn-closeness !
0 spot-display-on OR step-snapshots OR  display-switch !

\ display:
' show-genome-b look-at-xt !
127 snapshot-frequency !
100 2-ascii-scale !
1 (prior-display-type) !
4 display-slots !
' .step 0 display-slot !
' .cells 1 display-slot !
' .living 2 display-slot !
' .burden 3 display-slot !
' noop 4 display-slot !
' noop 5 display-slot !
' noop 6 display-slot !
' noop 7 display-slot !
' noop 8 display-slot !
' noop 9 display-slot !

\ colours:
' black background-color-xt !
' age>color foreground-color-xt !
' default-color color-selected-fg-xt !
' magenta color-below-fg-xt !
' cyan color-above-fg-xt !
' blue color-miss-fg-xt !
' cyan color-selected-bg-xt !
' magenta color-below-bg-xt !
' blue color-above-bg-xt !
' blue color-miss-bg-xt !
20 age>color-scale !
200 fcp>color-scale !
200 food>color-scale !
200 A-quality>color-scale !
200 B-quality>color-scale !
200 C-quality>color-scale !
200 D-quality>color-scale !
200 E-quality>color-scale !
200 A-property>color-scale !
200 B-property>color-scale !
200 C-property>color-scale !
200 D-property>color-scale !
200 E-property>color-scale !
200 A-secret>color-scale !

\ save-step-display-settings:
2 step-display-items !
0 (scan-index) !
' nuc-scan-display (scan-xt) !
6 (scan-detail) !
12 (scan-lines) !
' blue scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
1 4 vertical-zoom-scale 2!
0 (vertical-display-range) !
0 cont-scan-nucs OR  (scan-flags) !
1 (scan-index) !
' nuc-scan-display (scan-xt) !
17 (scan-detail) !
12 (scan-lines) !
' blue scan-background-xt !
' default-color scan-foreground-xt !
100 100 (scan-min-max) 2!
100 8100 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
1 4 vertical-zoom-scale 2!
36 (vertical-display-range) !
0 fixed-horizontal-range OR  (scan-flags) !
2 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
3 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
4 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
5 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
6 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
7 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
8 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
9 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
10 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
11 (scan-index) !
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
0 (step-more-info) !

\ save-continuous-display:
0 (continuous-column) !
2 1 cont-zoom-up-scale 2!
continuous-display-list empty-list 
' nuc-average 	0 continuous-display-list n'th-or-new-node  >cont-xt ! 
18 		0 continuous-display-list n'th-or-new-node  >cont-item ! 
98 		0 continuous-display-list n'th-or-new-node  >cont-lower ! 
160 		0 continuous-display-list n'th-or-new-node  >cont-upper ! 
' red 	0 continuous-display-list n'th-or-new-node  >cont-foreground-xt ! 
42 		0 continuous-display-list n'th-or-new-node  >cont-char ! 
' get-variable 	1 continuous-display-list n'th-or-new-node  >cont-xt ! 
' living 	1 continuous-display-list n'th-or-new-node  >cont-item ! 
0 		1 continuous-display-list n'th-or-new-node  >cont-lower ! 
1920 		1 continuous-display-list n'th-or-new-node  >cont-upper ! 
' white 	1 continuous-display-list n'th-or-new-node  >cont-foreground-xt ! 
120 		1 continuous-display-list n'th-or-new-node  >cont-char ! 

\ mutation:
1 100 mutation-rate 2!
3 stack-turning-point !
1 mutations-threshold !
50000 mutation-max-ollowed-items !
10 trial-phase !
4 max-if-items !
33 conditional-token-price !
0 reset-nuc-masks? !
1 resolve-flags !
-1 (exceeding-size-ring) !

\  set probabilities in xt probability pool mutation-types:
mutation-types nul-all-probabilities
1000 ' top-level-insertion	mutation-types set-one
1000 ' top-level-replacement	mutation-types set-one
3000 ' snip-types	mutation-types set-as-sublist
1000 ' top-level-address-replacemnt	mutation-types set-one
1000 ' top-level-token-replace	mutation-types set-one
1000 ' restart-from-scratch	mutation-types set-one
0 ' rebirth	mutation-types set-one

\  set probabilities in xt probability pool snip-types:
snip-types nul-all-probabilities
1000 ' top-level-random-snip	snip-types set-one
1000 ' top-level-short-snip	snip-types set-one
1000 ' top-level-long-snip	snip-types set-one


\  set probabilities in actual pool:
' gene-primitives actual-genepool-xt !
actual-genepool-xt @ execute nul-all-probabilities	\ gene-primitives
2000 	internal+' 0=	actual-genepool-xt @ execute set-one
5000 	internal+' 0<	actual-genepool-xt @ execute set-one
5000 	internal+' 0>	actual-genepool-xt @ execute set-one
1000 	internal+' =	actual-genepool-xt @ execute set-one
10000 	internal+' >	actual-genepool-xt @ execute set-one
10000 	internal+' <	actual-genepool-xt @ execute set-one
8000 	internal+' within	actual-genepool-xt @ execute set-one
10000 	internal+' AND	actual-genepool-xt @ execute set-one
10000 	internal+' OR	actual-genepool-xt @ execute set-one
10000 	internal+' XOR	actual-genepool-xt @ execute set-one
10000 	internal+' g-IF-ELSE-THEN	actual-genepool-xt @ execute set-one
6000 	internal+' dup	actual-genepool-xt @ execute set-one
2000 	internal+' 2dup	actual-genepool-xt @ execute set-one
2000 	internal+' drop	actual-genepool-xt @ execute set-one
2000 	internal+' drop(a-)	actual-genepool-xt @ execute set-one
2000 	internal+' nip	actual-genepool-xt @ execute set-one
2000 	internal+' nip(aa-a)	actual-genepool-xt @ execute set-one
2000 	internal+' nip(an-n)	actual-genepool-xt @ execute set-one
2000 	internal+' nip(na-a)	actual-genepool-xt @ execute set-one
4000 	internal+' tuck	actual-genepool-xt @ execute set-one
8000 	internal+' swap	actual-genepool-xt @ execute set-one
7000 	internal+' over	actual-genepool-xt @ execute set-one
6000 	internal+' over(an-ana)	actual-genepool-xt @ execute set-one
50000 	internal+' +	actual-genepool-xt @ execute set-one
50000 	internal+' -	actual-genepool-xt @ execute set-one
20000 	internal+' negate	actual-genepool-xt @ execute set-one
30000 	internal+' *	actual-genepool-xt @ execute set-one
30000 	internal+' ?/	actual-genepool-xt @ execute set-one
60000 	internal+' @	actual-genepool-xt @ execute set-one
10000 	internal+' take	actual-genepool-xt @ execute set-one
10000 	internal+' !(some)	actual-genepool-xt @ execute set-one
10000 	internal+' +!(some)	actual-genepool-xt @ execute set-one
10000 	internal+' -!(some)	actual-genepool-xt @ execute set-one
10000 	internal+' swap!(some)	actual-genepool-xt @ execute set-one
10000 	internal+' take-some	actual-genepool-xt @ execute set-one
80000 	internal+' !	actual-genepool-xt @ execute set-one
20000 	internal+' -!	actual-genepool-xt @ execute set-one
2000 	internal+' swap!	actual-genepool-xt @ execute set-one
2000 	internal+' off	actual-genepool-xt @ execute set-one
40000 	internal+' organ-A	actual-genepool-xt @ execute set-one
30000 	internal+' organ-B	actual-genepool-xt @ execute set-one
0 	internal+' organ-C	actual-genepool-xt @ execute set-one
0 	internal+' organ-D	actual-genepool-xt @ execute set-one
0 	internal+' organ-E	actual-genepool-xt @ execute set-one
0 	internal+' organ-F	actual-genepool-xt @ execute set-one
0 	internal+' organ-G	actual-genepool-xt @ execute set-one
10000 	internal+' parameter-A@	actual-genepool-xt @ execute set-one
40000 	internal+' parameter-B@	actual-genepool-xt @ execute set-one
0 	internal+' parameter-C@	actual-genepool-xt @ execute set-one
0 	internal+' parameter-D@	actual-genepool-xt @ execute set-one
0 	internal+' parameter-E@	actual-genepool-xt @ execute set-one
10000 	internal+' A-quality	actual-genepool-xt @ execute set-one
10000 	internal+' B-quality	actual-genepool-xt @ execute set-one
0 	internal+' C-quality	actual-genepool-xt @ execute set-one
0 	internal+' D-quality	actual-genepool-xt @ execute set-one
0 	internal+' E-quality	actual-genepool-xt @ execute set-one
10000 	internal+' A-property@	actual-genepool-xt @ execute set-one
10000 	internal+' B-property@	actual-genepool-xt @ execute set-one
50000 	internal+' C-property@	actual-genepool-xt @ execute set-one
0 	internal+' D-property@	actual-genepool-xt @ execute set-one
0 	internal+' E-property@	actual-genepool-xt @ execute set-one
0 	internal+' age@	actual-genepool-xt @ execute set-one
0 	internal+' age-threshold@	actual-genepool-xt @ execute set-one
0 	internal+' energy@	actual-genepool-xt @ execute set-one
0 	internal+' reproduction-threshold@	actual-genepool-xt @ execute set-one
0 	internal+' genomes-used	actual-genepool-xt @ execute set-one

\ set probabilities in current-genome-pool genomes-used
' genomes-used current-genome-pool-xt !

\ set probabilities in genome-pool genomes-used
genomes-used nul-all-probabilities
0 internal+' noop genomes-used set-one
0 internal+' noop genomes-used it's-node >genome-usage !
0 internal+' noop genomes-used set-one
0 internal+' noop genomes-used it's-node >genome-usage !

\ conditional execution:

\  save maybe-do-field: maybe-do-on-subset-field
maybe-do-on-subset-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' noop (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' energy (expr-xt-2) !
' energy (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\  save maybe-do-field: maybe-do-spot-subset-field
maybe-do-spot-subset-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' noop (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' food (expr-xt-1) !
' food (expr-xt-2) !
' food (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\  save maybe-do-field: maybe-select-field
maybe-select-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' select-nuc (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' energy (expr-xt-2) !
' noop (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\  save maybe-do-field: maybe-do-on-selected-field
maybe-do-on-selected-field
' variable-number (expression-xt) !
' = (condition-xt) !
' selected? (simple-expression-xt) !
' scale-variable (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' noop (expr-xt-1) !
' noop (expr-xt-2) !
' energy (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' noop (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' noop (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\  save maybe-do-field: do-on-world-field
do-on-world-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' noop (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' A-quality (expr-xt-1) !
' B-quality (expr-xt-2) !
' food (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\  save maybe-do-field: fg-colour-field
fg-colour-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' generic-hit>fg-color (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' genome-id (expr-xt-2) !
' noop (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\  save maybe-do-field: bg-colour-field
bg-colour-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' generic-hit>bg-color (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' food (expr-xt-1) !
' food (expr-xt-2) !
' noop (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\ I/O:

\ save function keys:
' |context-help| F1-xt !
' toggle-display-&-go F2-xt !
' spot-scan-menu F3-xt !
' nuc-scan-menu F4-xt !
' noop F5-xt !
' noop F6-xt !
' noop F7-xt !
' noop F8-xt !
' do-FORTH F9-xt !
' noop F10-xt !
' toggle-anything F11-xt !
' |goodbye| F12-xt !
' menus-menu shift-F1-xt !
' noop shift-F2-xt !
' noop shift-F3-xt !
' noop shift-F4-xt !
' noop shift-F5-xt !
' noop shift-F6-xt !
' noop shift-F7-xt !
' noop shift-F8-xt !

\ menu configuration data:
0 (genome-sort-index) !
-1 (sort-upwards) !
spot-local% (diversification-menu-type) !


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
36 sow drop time-step
nucs-not-scanned
\ needed for assertion of benchmark result:
36 cloned !

\ changed brew parameters:
-1449510237 seed-BRODIE !	 \ gets recorded changed or not 4

base !

s" Initialisation file 'sum-init.fs' included.  Experiment ready."  1 >message

menu-leave on		\ go to top level menu = brew main screen
single-step on		\ might be off from before

[ELSE]
    bell
    cr .( Compile options do not fit this experiment.)
    cr .( See source for informations. )
    cr 10000 wait-until
[THEN]
