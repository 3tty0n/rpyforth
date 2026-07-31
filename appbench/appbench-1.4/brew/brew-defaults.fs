\ brew-defaults.fs
\ 	$Id: brew-defaults.fs,v 1.25 2005/04/10 16:23:22 f Exp $	

\ 'brew-defaults.fs' sets startup defaults (except living cells).

\ This is the start up initialization file included after compiling brew.
\ It contains all brew defaults, but does not populate the current world yet.
\ (This is done by 'brew-init.fs')

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

\ Save brew variables:

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
672302367 seed-BRODIE !	 \ gets recorded changed or not 1

\ food, costs, population control:
elitism-off
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
4 display-slots !
' .step 0 display-slot !
' .cells 1 display-slot !
' .living 2 display-slot !
' .hits 3 display-slot !
' noop 4 display-slot !
' noop 5 display-slot !
' noop 6 display-slot !
' noop 7 display-slot !
' noop 8 display-slot !
' noop 9 display-slot !

\ colours:
' scoring-hit>bg-color background-color-xt !
' age>color foreground-color-xt !
' default-color color-selected-fg-xt !
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
4 step-display-items !
0 (scan-index) !
' nuc-scan-display (scan-xt) !
6 (scan-detail) !
8 (scan-lines) !
' blue scan-background-xt !
' default-color scan-foreground-xt !
114 534 (scan-min-max) 2!
0 534 (last-scan-min-max) 2!
0 df-inf-count !
0 df-real-count !
0 df+inf-count !
0 df-nan-count !
' df-max buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' df-min buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
0 last-df-inf-count !
0 last-df-real-count !
0 last-df+inf-count !
0 last-df-nan-count !
' last-df-max buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' last-df-min buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
1 5 horizontal-zoom-scale 2!
1 4 vertical-zoom-scale 2!
649 (vertical-display-range) !
0 cont-scan-nucs OR  (scan-flags) !

1 (scan-index) !
' nuc-scan-display (scan-xt) !
17 (scan-detail) !
7 (scan-lines) !
' blue scan-background-xt !
' default-color scan-foreground-xt !
600 7196 (scan-min-max) 2!
100 8100 (last-scan-min-max) 2!
0 df-inf-count !
0 df-real-count !
0 df+inf-count !
0 df-nan-count !
' df-max buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' df-min buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
0 last-df-inf-count !
0 last-df-real-count !
0 last-df+inf-count !
0 last-df-nan-count !
' last-df-max buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' last-df-min buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
1 5 horizontal-zoom-scale 2!
1 4 vertical-zoom-scale 2!
690 (vertical-display-range) !
0 fixed-horizontal-range OR  (scan-flags) !

2 (scan-index) !
' nuc-scan-display (scan-xt) !
34 (scan-detail) !
7 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
-2196 2279 (scan-min-max) 2!
-2000 2000 (last-scan-min-max) 2!
0 df-inf-count !
0 df-real-count !
0 df+inf-count !
0 df-nan-count !
' df-max buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' df-min buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
0 last-df-inf-count !
0 last-df-real-count !
0 last-df+inf-count !
0 last-df-nan-count !
' last-df-max buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' last-df-min buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
1 5 horizontal-zoom-scale 2!
1 4 vertical-zoom-scale 2!
31 (vertical-display-range) !
0 fixed-horizontal-range OR  (scan-flags) !

3 (scan-index) !
' nuc-scan-func-dspl (scan-xt) !
' score (scan-detail) !		\ altered by hand DADA
7 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
-17439 -12 (scan-min-max) 2!
-79 0 (last-scan-min-max) 2!
0 df-inf-count !
0 df-real-count !
0 df+inf-count !
0 df-nan-count !
' df-max buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' df-min buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
0 last-df-inf-count !
0 last-df-real-count !
0 last-df+inf-count !
0 last-df-nan-count !
' last-df-max buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' last-df-min buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
1 5 horizontal-zoom-scale 2!
1 4 vertical-zoom-scale 2!
652 (vertical-display-range) !
0 fixed-horizontal-range OR  (scan-flags) !

0 (step-more-info) !

\ #####################################################
\ \ save-step-display-settings:
\ 2 step-display-items !
\ 0 (scan-index) !
\ ' continuous-display (scan-xt) !
\ 8 (scan-detail) !
\ \ 24 (scan-lines) !		\ old 80 25 version
\ l-s 1- (scan-lines) !
\ ' blue scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 1 (scan-flags) !
\ 1 (scan-index) !
\ ' spot-scan-display (scan-xt) !
\ 0 (scan-detail) !
\ \ 12 (scan-lines) !		\ old 80 25 version
\ l-s 1- 2/ (scan-lines) !
\ ' blue scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 2 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 3 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 4 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 5 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 6 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 7 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 8 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 9 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 10 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 11 (scan-index) !
\ 0 (scan-xt) !
\ 0 (scan-detail) !
\ 0 (scan-lines) !
\ ' default-color scan-background-xt !
\ ' default-color scan-foreground-xt !
\ 0 0 (scan-min-max) 2!
\ 0 0 (last-scan-min-max) 2!
\ 1 5 horizontal-zoom-scale 2!
\ 0 0 vertical-zoom-scale 2!
\ 0 (vertical-display-range) !
\ 0 (scan-flags) !
\ 0 (step-more-info) !

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
4000 ' snip-types	mutation-types set-as-sublist
1000 ' top-level-address-replacemnt	mutation-types set-one
1000 ' top-level-token-replace	mutation-types set-one
1000 ' restart-from-scratch	mutation-types set-one

\  set probabilities in xt probability pool snip-types:
snip-types nul-all-probabilities
1000 ' top-level-random-snip	snip-types set-one
1000 ' top-level-short-snip	snip-types set-one
1000 ' top-level-long-snip	snip-types set-one
1000 ' top-level-snip-IF-ELSE-branch    snip-types set-one


\  set probabilities in actual pool:
' gene-primitives actual-genepool-xt !
actual-genepool-xt @ execute nul-all-probabilities	\ gene-primitives
2000 	internal' 0=	actual-genepool-xt @ execute set-one
5000 	internal' 0<	actual-genepool-xt @ execute set-one
5000 	internal' 0>	actual-genepool-xt @ execute set-one
1000 	internal' =	actual-genepool-xt @ execute set-one
10000 	internal' >	actual-genepool-xt @ execute set-one
10000 	internal' <	actual-genepool-xt @ execute set-one
8000 	internal' within	actual-genepool-xt @ execute set-one
10000 	internal' AND	actual-genepool-xt @ execute set-one
10000 	internal' OR	actual-genepool-xt @ execute set-one
10000 	internal' XOR	actual-genepool-xt @ execute set-one
10000 	internal' g-IF-ELSE-THEN	actual-genepool-xt @ execute set-one
6000 	internal' dup	actual-genepool-xt @ execute set-one
2000 	internal' 2dup	actual-genepool-xt @ execute set-one
2000 	internal' drop	actual-genepool-xt @ execute set-one
2000 	internal' drop(a-)	actual-genepool-xt @ execute set-one
2000 	internal' nip	actual-genepool-xt @ execute set-one
2000 	internal' nip(aa-a)	actual-genepool-xt @ execute set-one
2000 	internal' nip(an-n)	actual-genepool-xt @ execute set-one
2000 	internal' nip(na-a)	actual-genepool-xt @ execute set-one
4000 	internal' tuck	actual-genepool-xt @ execute set-one
8000 	internal' swap	actual-genepool-xt @ execute set-one
7000 	internal' over	actual-genepool-xt @ execute set-one
6000 	internal' over(an-ana)	actual-genepool-xt @ execute set-one
50000 	internal' +	actual-genepool-xt @ execute set-one
50000 	internal' -	actual-genepool-xt @ execute set-one
20000 	internal' negate	actual-genepool-xt @ execute set-one
30000 	internal' *	actual-genepool-xt @ execute set-one
30000 	internal' ?/	actual-genepool-xt @ execute set-one
60000 	internal' @	actual-genepool-xt @ execute set-one
10000 	internal' take	actual-genepool-xt @ execute set-one
10000 	internal' !(some)	actual-genepool-xt @ execute set-one
10000 	internal' +!(some)	actual-genepool-xt @ execute set-one
10000 	internal' -!(some)	actual-genepool-xt @ execute set-one
10000 	internal' swap!(some)	actual-genepool-xt @ execute set-one
10000 	internal' take-some	actual-genepool-xt @ execute set-one
80000 	internal' !	actual-genepool-xt @ execute set-one
20000 	internal' -!	actual-genepool-xt @ execute set-one
2000 	internal' swap!	actual-genepool-xt @ execute set-one
2000 	internal' off	actual-genepool-xt @ execute set-one
40000 	internal' organ-A	actual-genepool-xt @ execute set-one
30000 	internal' organ-B	actual-genepool-xt @ execute set-one
0 	internal' organ-C	actual-genepool-xt @ execute set-one
0 	internal' organ-D	actual-genepool-xt @ execute set-one
0 	internal' organ-E	actual-genepool-xt @ execute set-one
0 	internal' organ-F	actual-genepool-xt @ execute set-one
0 	internal' organ-G	actual-genepool-xt @ execute set-one
10000 	internal' parameter-A@	actual-genepool-xt @ execute set-one
40000 	internal' parameter-B@	actual-genepool-xt @ execute set-one
0 	internal' parameter-C@	actual-genepool-xt @ execute set-one
0 	internal' parameter-D@	actual-genepool-xt @ execute set-one
0 	internal' parameter-E@	actual-genepool-xt @ execute set-one
10000 	internal' A-quality	actual-genepool-xt @ execute set-one
10000 	internal' B-quality	actual-genepool-xt @ execute set-one
0 	internal' C-quality	actual-genepool-xt @ execute set-one
0 	internal' D-quality	actual-genepool-xt @ execute set-one
0 	internal' E-quality	actual-genepool-xt @ execute set-one
10000 	internal' A-property@	actual-genepool-xt @ execute set-one
10000 	internal' B-property@	actual-genepool-xt @ execute set-one
50000 	internal' C-property@	actual-genepool-xt @ execute set-one
0 	internal' D-property@	actual-genepool-xt @ execute set-one
0 	internal' E-property@	actual-genepool-xt @ execute set-one
0 	internal' age@	actual-genepool-xt @ execute set-one
0 	internal' age-threshold@	actual-genepool-xt @ execute set-one
0 	internal' energy@	actual-genepool-xt @ execute set-one
0 	internal' reproduction-threshold@	actual-genepool-xt @ execute set-one

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

' at-xy-wrapping brew-at-xy-xt !

\ save function keys:
' |context-help| F1-xt !
' toggle-display-&-go F2-xt !
' spot-scan-menu F3-xt !
' nuc-scan-menu F4-xt !
' world-list-menu F5-xt !
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
0 (nuc-menus-show-dfloats) !
0 (spot-menus-show-dfloats) !

\ CHANGED LATER ON:
0 (scan-index) !
' nuc-scan-display (scan-xt) !
6 (scan-detail) !
\ 12 (scan-lines) !		\ old 80 25 version
\ l-s 1- 2/ (scan-lines) !
1 4 vertical-zoom-scale 2!

1 (scan-index) !
' nuc-scan-display (scan-xt) !
17 (scan-detail) !
100 100 (scan-min-max) 2!
100 8100 (last-scan-min-max) 2!
0 fixed-horizontal-range OR  (scan-flags) !
1 4 vertical-zoom-scale 2!

[DEFINED] nuc-f-diversification-rate [IF]	\ benchmark compatibility
    0e0 nuc-f-diversification-rate df!		\ The only one necessary...
[THEN]
