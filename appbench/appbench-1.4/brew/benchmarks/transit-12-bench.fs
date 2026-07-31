\ 'brew' benchmark file transit-12-bench.fs

: bench-version ( -- addr count )
    cvs" 	$Id: transit-12-bench.fs,v 1.30 2005/04/10 16:20:33 f Exp $	" ;

\  created with brew version brew-transit_12.

\ brew was compiled with the following nuc compile time values:
\ 7 TO nuc-organs#
\ 3 TO nuc-parameters#
\ 3 TO nuc-invisibles#
\ 1 TO nuc-secrets#

\ brew was compiled with the following world compile time values:
\ 3 TO spot-qualities#
\ 1 TO spot-secrets#
\ 3 TO spot-properties#


page
cr .( 'brew' running benchmark 'transit-12-bench.fs' )
cr .tab bench-version type
cr
.brew-version

playing-bench!
no-info-line on
display-slots off
display-switch off
single-step off


\ starting from scratch:

\ restart with an unpopulated world:
free-field

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

\ for benchmarking, you might want to comment the following out:
\ 0  log-mask !

\ for benchmarking, you might want to comment the following out:
0 file-code OR file-stack OR  code-file-mask !
\ random generator:
' random-BRODIE random-xt !
272958469 0 (random-generalized) 2!
1075118612 seed-BRODIE !	 \ gets recorded changed or not 1

\ food, costs, population control:
0 world-food-supply !
200 food-share/spot !
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
1 10 score-rate 2!

\ diversification:
0 div-organ-A OR div-organ-B OR div-organ-C OR 
div-organ-D OR div-organ-E OR div-organ-F OR 
div-organ-G OR div-parameter-A OR div-parameter-B OR 
div-parameter-C OR  diversification-mask !
50 diversification-range !
1 4 diversification-rate 2!
2 nuc-diversification-closeness !
65536 sporadic-value-range !
1 100 sporadic-value-rate 2!
spot-diversification-mask off

\ for benchmarking, you might want to comment the following out:
\ 0 spot-display-on OR  display-switch !

\ display:
' show-generation look-at-xt !
100 2-ascii-scale !

\ colours:
' food>color background-color-xt !
' age>color foreground-color-xt !
' red color-selected-fg-xt !
\ ' genome-id (nuc-var-for-color) !
\ 0 (nuc-value-for-color) !
\ ' = (color-condition-xt) !
100 food>color-scale !
20 age>color-scale !
200 A-quality>color-scale !
200 B-quality>color-scale !
200 C-quality>color-scale !

\ for benchmarking, you might want to comment the following out:
\ 4 display-slots !
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

\ for benchmarking, you might want to comment the following out:

\ save-step-display-settings:
2 step-display-items !
0 (scan-index) !
' continuous-display (scan-xt) !
8 (scan-detail) !
24 (scan-lines) !
' blue scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
1 (scan-flags) !
1 (scan-index) !
' spot-scan-display (scan-xt) !
0 (scan-detail) !
12 (scan-lines) !
' blue scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
1 5 horizontal-zoom-scale 2!
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
0 (scan-flags) !
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
10 trial-phase !
4 max-if-items !
33 conditional-token-price !
0 reset-nuc-masks? !
1 resolve-flags !

\  set probabilities in xt probability pool mutation-types:
mutation-types nul-all-probabilities
1000 ' top-level-insertion	mutation-types set-one
1000 ' top-level-replacement	mutation-types set-one
3000 ' snip-types	mutation-types set-as-sublist
1000 ' top-level-address-replacemnt	mutation-types set-one
1000 ' top-level-token-replace	mutation-types set-one
1000 ' restart-from-scratch	mutation-types set-one

\  set probabilities in xt probability pool snip-types:
snip-types nul-all-probabilities
1000 ' top-level-random-snip	snip-types set-one
1000 ' top-level-short-snip	snip-types set-one
1000 ' top-level-long-snip	snip-types set-one


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
28000 	internal' +	actual-genepool-xt @ execute set-one
28000 	internal' -	actual-genepool-xt @ execute set-one
8000 	internal' negate	actual-genepool-xt @ execute set-one
22000 	internal' *	actual-genepool-xt @ execute set-one
22000 	internal' ?/	actual-genepool-xt @ execute set-one
10000 	internal' @	actual-genepool-xt @ execute set-one
10000 	internal' take	actual-genepool-xt @ execute set-one
19000 	internal' !(some)	actual-genepool-xt @ execute set-one
19000 	internal' +!(some)	actual-genepool-xt @ execute set-one
19000 	internal' -!(some)	actual-genepool-xt @ execute set-one
10000 	internal' swap!(some)	actual-genepool-xt @ execute set-one
10000 	internal' take-some	actual-genepool-xt @ execute set-one
2000 	internal' off	actual-genepool-xt @ execute set-one
10000 	internal' organ-A	actual-genepool-xt @ execute set-one
10000 	internal' organ-B	actual-genepool-xt @ execute set-one
10000 	internal' organ-C	actual-genepool-xt @ execute set-one
10000 	internal' organ-D	actual-genepool-xt @ execute set-one
10000 	internal' organ-E	actual-genepool-xt @ execute set-one
10000 	internal' organ-F	actual-genepool-xt @ execute set-one
10000 	internal' organ-G	actual-genepool-xt @ execute set-one
10000 	internal' parameter-A@	actual-genepool-xt @ execute set-one
10000 	internal' parameter-B@	actual-genepool-xt @ execute set-one
10000 	internal' parameter-C@	actual-genepool-xt @ execute set-one
10000 	internal' A-quality	actual-genepool-xt @ execute set-one
10000 	internal' B-quality	actual-genepool-xt @ execute set-one
10000 	internal' C-quality	actual-genepool-xt @ execute set-one
10000 	internal' A-property@	actual-genepool-xt @ execute set-one
10000 	internal' B-property@	actual-genepool-xt @ execute set-one
10000 	internal' C-property@	actual-genepool-xt @ execute set-one
6000 	internal' age@	actual-genepool-xt @ execute set-one
3000 	internal' age-threshold@	actual-genepool-xt @ execute set-one
6000 	internal' energy@	actual-genepool-xt @ execute set-one
6000 	internal' reproduction-threshold@	actual-genepool-xt @ execute set-one

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
' <food> (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !

\  save maybe-do-field: fg-colour-field
fg-colour-field
fg-colour-field 	' variable-number (expression-xt) !
fg-colour-field 	' = (condition-xt) !
fg-colour-field 	' false (simple-expression-xt) !
fg-colour-field 	' noop (do-it-xt) !
fg-colour-field 	0 (expr-parameter) !
fg-colour-field 	0 (expr-parameter-2) !
fg-colour-field 	' genome-id (expr-xt-1) !
fg-colour-field 	' noop (expr-xt-2) !
fg-colour-field 	' noop (xt-do-it) !
fg-colour-field 	0 (do-it-parameter) !
fg-colour-field 	0 (do-it-parameter-2) !
fg-colour-field 	1 1 (do-it-scale) 2!
fg-colour-field 	' maybe-do (maybe-do-type-xt) !
fg-colour-field 	buffered" " (expression-handle) !
fg-colour-field 	buffered" " (maybe-do-handle) !

\ I/O:

\ for benchmarking, you might want to comment the following out:

\ save function keys:
' menus-menu F1-xt !
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
' goodbye F12-xt !
' noop shift-F1-xt !
' noop shift-F2-xt !
' noop shift-F3-xt !
' noop shift-F4-xt !
' noop shift-F5-xt !
' noop shift-F6-xt !
' noop shift-F7-xt !
' noop shift-F8-xt !

\ for benchmarking, you might want to comment the following out:

\ menu configuration data:
0 (genome-sort-index) !
-1 (sort-upwards) !

\ define nuc and set it actual:
nuc-length# new-nucleus DROP cp!
\ needed for assertion of benchmark result:
0 cloned !
gene' noop    	wake-me-xt !
internal' noop    	wake-me-internal !
' eat-violet    	eat-xt !
' cell-division    	reproduce-xt !
' show-sign-A    	show-me-xt !
\ 132 length !	\ make it independent of nuc structure...
0 nuc-flags !
0 nuc-supplements !
0 id !
0 genome-id !
0 age !
0 generation !
0 genome-generation !
100 code-cost !
0 energy !
100 reprodctn-threshold !
10 age-threshold !
0 appearance !
0 div-organ-A OR  my-diversifctn-mask !
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
0 invisible-A !
0 invisible-B !
0 invisible-C !
0 secret-A !

\ sow above nuc:
\ random generator:
' random-BRODIE random-xt !
1075118612 seed-BRODIE !
-1 (sow-diversified) !
33 sow drop time-step
nucs-not-scanned
\ needed for assertion of benchmark result:
33 cloned !

\ changed brew parameters:
-902387990 seed-BRODIE !	 \ gets recorded changed or not 4

\ brew the gene soup until step 65
65 brew drop


\ changed brew parameters:
-1313293690 seed-BRODIE !	 \ gets recorded changed or not 6
2 100 mutation-rate 2!
2 stack-turning-point !
6 mutations-threshold !
12 trial-phase !

\ brew the gene soup until step 102
37 brew drop


\ changed brew parameters:
49211153 seed-BRODIE !	 \ gets recorded changed or not 8
0 (scan-index) !
-317 1006 (scan-min-max) 2!
-317 1006 (last-scan-min-max) 2!
1 4 vertical-zoom-scale 2!
316 (vertical-display-range) !

\ brew the gene soup until step 130
28 brew drop


\ user invocated 'do-change-selections'

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
do-change-selections

\ user invocated 'do-on-selected-nucs'

\  save maybe-do-field: maybe-do-on-selected-field
maybe-do-on-selected-field
' variable-number (expression-xt) !
' = (condition-xt) !
' selected? (simple-expression-xt) !
' remove-nuc (do-it-xt) !
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
do-on-selected-nucs


\ changed brew parameters:
-2039970736 seed-BRODIE !	 \ gets recorded changed or not 10
' show-ABC look-at-xt !
maybe-do-on-selected-field 	' remove-nuc (do-it-xt) !

\ brew the gene soup until step 148
18 brew drop


\ changed brew parameters:
1836504373 seed-BRODIE !	 \ gets recorded changed or not 12
maybe-do-this-genome-field 	' scale-variable (do-it-xt) !
maybe-do-this-genome-field 	111 (expr-parameter) !
maybe-do-this-genome-field 	' organ-A (xt-do-it) !
maybe-do-this-genome-field 	10 1 (do-it-scale) 2!

\ brew the gene soup until step 180
32 brew drop


\ spot edited by user:
830 >spot! 
1000000 food       ! 
world-not-scanned


\ changed brew parameters:
1603483573 seed-BRODIE !	 \ gets recorded changed or not 14

\ brew the gene soup until step 236
56 brew drop


\ user invocated '|maybe-do-everywhere-generic|'

\  save maybe-do-field: do-on-world-field
do-on-world-field
' variable-number (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' add-to-variable (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' <food> (expr-xt-1) !
' B-quality (expr-xt-2) !
' <food> (xt-do-it) !
10000 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' do-on-world-field maybe-do-everywhere-generic


\ changed brew parameters:
-80148954 seed-BRODIE !	 \ gets recorded changed or not 16
0 (scan-index) !
-1507 971983 (scan-min-max) 2!
-4096 971983 (last-scan-min-max) 2!
1919 (vertical-display-range) !
do-on-world-field 	' < (condition-xt) !
do-on-world-field 	' add-to-variable (do-it-xt) !
do-on-world-field 	' <food> (expr-xt-1) !
do-on-world-field 	10000 (do-it-parameter) !
do-on-world-field 	' maybe-do (maybe-do-type-xt) !

\ brew the gene soup until step 333
97 brew drop


\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' scale-variable (do-it-xt) !
1054 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic


\ changed brew parameters:
1186003975 seed-BRODIE !	 \ gets recorded changed or not 18
0 (scan-index) !
-1142 -952 (scan-min-max) 2!
36 (vertical-display-range) !
maybe-do-this-genome-field 	1054 (expr-parameter) !
maybe-do-this-genome-field 	' organ-C (xt-do-it) !
maybe-do-this-genome-field 	-1 1 (do-it-scale) 2!

\ brew the gene soup until step 371
38 brew drop


\ user invocated 'do-change-selections'

\  save maybe-do-field: maybe-select-field
maybe-select-field
' variable-number (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' select-nuc (do-it-xt) !
1165 (expr-parameter) !
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
do-change-selections

\ user invocated 'do-change-selections'

\  save maybe-do-field: maybe-select-field
maybe-select-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' toggle-selection (do-it-xt) !
1165 (expr-parameter) !
0 (expr-parameter-2) !
' organ-A (expr-xt-1) !
' organ-B (expr-xt-2) !
' noop (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
do-change-selections

\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' toggle-selection (do-it-xt) !
1254 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic

\ user invocated 'do-on-selected-nucs'

\  save maybe-do-field: maybe-do-on-selected-field
maybe-do-on-selected-field
' variable-number (expression-xt) !
' = (condition-xt) !
' selected? (simple-expression-xt) !
' remove-nuc (do-it-xt) !
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
do-on-selected-nucs


\ changed brew parameters:
-1088103226 seed-BRODIE !	 \ gets recorded changed or not 20
fg-colour-field 	1254 (expr-parameter) !
maybe-select-field 	' 2-variables (expression-xt) !
maybe-select-field 	' < (condition-xt) !
maybe-select-field 	' toggle-selection (do-it-xt) !
maybe-select-field 	1165 (expr-parameter) !
maybe-select-field 	' organ-A (expr-xt-1) !
maybe-select-field 	' organ-B (expr-xt-2) !
maybe-do-this-genome-field 	' toggle-selection (do-it-xt) !
maybe-do-this-genome-field 	1254 (expr-parameter) !

\ brew the gene soup until step 422
51 brew drop


\ user invocated 'do-change-selections'

\  save maybe-do-field: maybe-select-field
maybe-select-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' toggle-selection (do-it-xt) !
1165 (expr-parameter) !
0 (expr-parameter-2) !
' organ-A (expr-xt-1) !
' organ-B (expr-xt-2) !
' noop (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
do-change-selections

\ user invocated 'do-change-selections'

\  save maybe-do-field: maybe-select-field
maybe-select-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' toggle-selection (do-it-xt) !
1165 (expr-parameter) !
0 (expr-parameter-2) !
' organ-A (expr-xt-1) !
' organ-C (expr-xt-2) !
' noop (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
do-change-selections

\ user invocated 'do-change-selections'

\  save maybe-do-field: maybe-select-field
maybe-select-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' toggle-selection (do-it-xt) !
1165 (expr-parameter) !
0 (expr-parameter-2) !
' organ-B (expr-xt-1) !
' organ-C (expr-xt-2) !
' noop (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
do-change-selections

\ user invocated 'do-on-selected-nucs'

\  save maybe-do-field: maybe-do-on-selected-field
maybe-do-on-selected-field
' variable-number (expression-xt) !
' = (condition-xt) !
' selected? (simple-expression-xt) !
' evaluate-do (do-it-xt) !
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
buffered" organ-A @ negate organ-b +!" (maybe-do-handle) !
do-on-selected-nucs

\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' toggle-selection (do-it-xt) !
1254 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic

\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' toggle-selection (do-it-xt) !
1434 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic

invert-selections

\ user invocated 'do-on-selected-nucs'

\  save maybe-do-field: maybe-do-on-selected-field
maybe-do-on-selected-field
' variable-number (expression-xt) !
' = (condition-xt) !
' selected? (simple-expression-xt) !
' remove-nuc (do-it-xt) !
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
buffered" organ-A @ negate organ-b +!" (maybe-do-handle) !
do-on-selected-nucs

\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' scale-variable (do-it-xt) !
1254 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic

\ user invocated '|maybe-do-everywhere-generic|'

\  save maybe-do-field: do-on-world-field
do-on-world-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' add-to-variable (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' B-quality (expr-xt-1) !
' C-quality (expr-xt-2) !
' <food> (xt-do-it) !
100000 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' do-on-world-field maybe-do-everywhere-generic


\ changed brew parameters:
1139843065 seed-BRODIE !	 \ gets recorded changed or not 22
maybe-select-field 	' organ-B (expr-xt-1) !
maybe-select-field 	' organ-C (expr-xt-2) !
maybe-do-on-selected-field 	buffered" organ-A @ negate organ-b +!" (maybe-do-handle) !
maybe-do-this-genome-field 	' scale-variable (do-it-xt) !
do-on-world-field 	' 2-variables (expression-xt) !
do-on-world-field 	' B-quality (expr-xt-1) !
do-on-world-field 	' C-quality (expr-xt-2) !
do-on-world-field 	100000 (do-it-parameter) !

\ brew the gene soup until step 513
91 brew drop


\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' remove-nuc (do-it-xt) !
1747 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic

\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' remove-nuc (do-it-xt) !
1754 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic

\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' remove-nuc (do-it-xt) !
1658 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic

\ user invocated '|maybe-do-on-everybody-generic|'

\  save maybe-do-field: maybe-do-this-genome-field
maybe-do-this-genome-field
' variable-number (expression-xt) !
' = (condition-xt) !
' false (simple-expression-xt) !
' remove-nuc (do-it-xt) !
1837 (expr-parameter) !
0 (expr-parameter-2) !
' genome-id (expr-xt-1) !
' noop (expr-xt-2) !
' organ-C (xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
-1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' maybe-do-this-genome-field maybe-do-on-everybody-generic


\ changed brew parameters:
1467599884 seed-BRODIE !	 \ gets recorded changed or not 24
fg-colour-field 	1658 (expr-parameter) !
maybe-do-this-genome-field 	' remove-nuc (do-it-xt) !
maybe-do-this-genome-field 	1837 (expr-parameter) !
1 (genome-sort-index) !
0 (sort-upwards) !

\ brew the gene soup until step 688
175 brew drop


\ user invocated '|maybe-do-everywhere-generic|'

\  save maybe-do-field: do-on-world-field
do-on-world-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' evaluate-do (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' B-quality (expr-xt-1) !
' C-quality (expr-xt-2) !
' <food> (xt-do-it) !
100000 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" a-quality @ a-property !" (maybe-do-handle) !
' do-on-world-field maybe-do-everywhere-generic

\ user invocated '|maybe-do-everywhere-generic|'

\  save maybe-do-field: do-on-world-field
do-on-world-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' false (simple-expression-xt) !
' add-to-variable (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' B-quality (expr-xt-1) !
' C-quality (expr-xt-2) !
' <food> (xt-do-it) !
100000 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" a-quality @ a-property !" (maybe-do-handle) !
' do-on-world-field maybe-do-everywhere-generic

\ user invocated '|maybe-do-everywhere-generic|'

\  save maybe-do-field: do-on-world-field
do-on-world-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' inhabited? (simple-expression-xt) !
' add-to-variable (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' B-quality (expr-xt-1) !
' A-quality (expr-xt-2) !
' <food> (xt-do-it) !
100000 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" a-quality @ a-property !" (maybe-do-handle) !
' do-on-world-field maybe-do-everywhere-generic

\ changed brew parameters:
1754649471 seed-BRODIE !	 \ gets recorded changed or not 26
0 (scan-index) !
-39444 -39109 (scan-min-max) 2!
49 (vertical-display-range) !
do-on-world-field 	' inhabited? (simple-expression-xt) !
do-on-world-field 	' A-quality (expr-xt-2) !
do-on-world-field 	' maybe-do-simple (maybe-do-type-xt) !
do-on-world-field 	buffered" a-quality @ a-property !" (maybe-do-handle) !

\ brew the gene soup until step 727
39 brew drop


\ define nuc and set it actual:
nuc-length# new-nucleus DROP cp!
\ needed for assertion of benchmark result:
158441 cloned !
gene' noop    	wake-me-xt !
internal' noop    	wake-me-internal !
' eat-violet    	eat-xt !
' cell-division    	reproduce-xt !
' show-sign-A    	show-me-xt !
\ 132 length !	\ make it independent of nuc structure...
0 nuc-flags !
0 nuc-supplements !
0 id !
0 genome-id !
0 age !
0 generation !
0 genome-generation !
100 code-cost !
0 energy !
100 reprodctn-threshold !
10 age-threshold !
0 appearance !
0 div-organ-A OR  my-diversifctn-mask !
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
0 invisible-A !
0 invisible-B !
0 invisible-C !
0 secret-A !

\ sow above nuc:
\ random generator:
' random-BRODIE random-xt !
-907019200 seed-BRODIE !
-1 (sow-diversified) !
1000 sow drop time-step
nucs-not-scanned
\ needed for assertion of benchmark result:
159204 cloned !


\ changed brew parameters:
2078103138 seed-BRODIE !	 \ gets recorded changed or not 28

\ brew the gene soup until step 855
127 brew drop


\ user invocated '|maybe-do-everywhere-generic|'

\  save maybe-do-field: do-on-world-field
do-on-world-field
' 2-variables (expression-xt) !
' < (condition-xt) !
' everywhere (simple-expression-xt) !
' add-to-variable (do-it-xt) !
0 (expr-parameter) !
0 (expr-parameter-2) !
' B-quality (expr-xt-1) !
' A-quality (expr-xt-2) !
' <food> (xt-do-it) !
100000 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" a-quality @ a-property !" (maybe-do-handle) !
' do-on-world-field maybe-do-everywhere-generic


\ define nuc and set it actual:
nuc-length# new-nucleus DROP cp!
\ needed for assertion of benchmark result:
167731 cloned !
gene' noop    	wake-me-xt !
internal' noop    	wake-me-internal !
' eat-violet    	eat-xt !
' cell-division    	reproduce-xt !
' show-sign-A    	show-me-xt !
\ 132 length !	\ make it independent of nuc structure...
0 nuc-flags !
0 nuc-supplements !
0 id !
0 genome-id !
0 age !
0 generation !
0 genome-generation !
100 code-cost !
0 energy !
100 reprodctn-threshold !
10 age-threshold !
0 appearance !
0 div-organ-A OR  my-diversifctn-mask !
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
0 invisible-A !
0 invisible-B !
0 invisible-C !
0 secret-A !

\ sow above nuc:
\ random generator:
' random-BRODIE random-xt !
-49480160 seed-BRODIE !
-1 (sow-diversified) !
543 sow drop time-step
nucs-not-scanned
\ needed for assertion of benchmark result:
168197 cloned !

\ changed brew parameters:
1245622954 seed-BRODIE !	 \ gets recorded changed or not 30
do-on-world-field 	' everywhere (simple-expression-xt) !

\ brew the gene soup until step 892
36 brew drop


\ define nuc and set it actual:
nuc-length# new-nucleus DROP cp!
\ needed for assertion of benchmark result:
171642 cloned !
gene' noop    	wake-me-xt !
internal' noop    	wake-me-internal !
' eat-violet    	eat-xt !
' cell-division    	reproduce-xt !
' show-sign-A    	show-me-xt !
\ 132 length !	\ make it independent of nuc structure...
0 nuc-flags !
0 nuc-supplements !
0 id !
0 genome-id !
0 age !
0 generation !
0 genome-generation !
100 code-cost !
0 energy !
100 reprodctn-threshold !
10 age-threshold !
0 appearance !
0 div-organ-A OR  my-diversifctn-mask !
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
0 invisible-A !
0 invisible-B !
0 invisible-C !
0 secret-A !

\ sow above nuc:
\ random generator:
' random-BRODIE random-xt !
-853562049 seed-BRODIE !
-1 (sow-diversified) !
543 sow drop time-step
nucs-not-scanned
\ needed for assertion of benchmark result:
172125 cloned !


\ changed brew parameters:
536467541 seed-BRODIE !	 \ gets recorded changed or not 32
0 nuc-do-cost !
0 code-price !

\ brew the gene soup until step 1503
610 brew drop


\ assert-state-entry: Assert expected results:
seed-BRODIE @ 460619090 =
1503 	' step 	assert@=  AND
320565 	' cloned 	assert@=  AND
499 	' living 	assert@=  AND
1572 	' nuc-do-cost 	assert@=  AND
1572 	' code-price 	assert@=  AND
2513 	' (mutated-max) 	assert@=  AND
343 	' compiled-genes 	assert@=  AND
1795166	' world-checksum	assert-do=  AND
1270650	' nucs-checksum	assert-do=  AND
cr
[IF]
    .( 	Result is valid. )
[ELSE]
    bell .( 	Unexpected result. Not comparable to other systems! )
    playing-bench? 0= [IF]
        wait
    [THEN]
[THEN]

BYE
