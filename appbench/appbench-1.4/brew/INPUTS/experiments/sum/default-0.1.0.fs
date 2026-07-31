\ default-0.1.0.fs
\ 	$Id: default-0.1.0.fs,v 1.8 2005/04/17 06:56:50 f Exp $	

\ ****************************************************************
\ This is a simple demo related to brew-0.1.0 default initialisation.
\ adapted to run on later versions...
\ ****************************************************************

\ ****************************************************************
decimal


\ ****************************************************************
\ Compatibility:
\ ****************************************************************
mutation-must-differ off


\ Do *not* use 'old-bench-compatible-mode?' here. It's a compile time switch.


\ The demo was created for a 80 25 text console screen.
\ If run on another configuration it will create a 80 25 demo world
\ and inform the user about it.
\ ****************************************************************
c-l l-s  80 25  d= 0=		\ flag if on another screen size
dup VALUE demo-world-created	\ store flag in demo-world-created
[IF]  big-bang  [THEN]		\ create a demo screen
\ ****************************************************************

page .( This demo shows a very simple experiment:)
cr   s"   (same as 'brew-0.1.0' default initialisation)" type
cr
cr
cr .( The task is to solve the equation  A = B + C)
cr
cr .(   Gene primitives give read/write access to variable A,)
cr .(   read/only access to B and C.)
cr
cr .(   In this example I use a spot local variable for C, 'C-property')
cr .(   but a nuc local variable for B 'parameter-B'.)
cr .(   So B is hereditary, which would open the possibility to 'cheat',)
cr .(   by selecting individuals with B close to zero.)
cr .(   A is 'organ-A', a r/w nuc variable.)
cr
cr .(   Brew does not know what it has to do, )
cr .(   nor that it should put it's result in variable A.)
cr
cr .(   The setup is not optimised to the task,)
cr .(   but to give a little demo.)
cr .(   We could have results quicker, of course.)
cr
cr
last-left s" (press a key to start)" type
wait

single-step off
elitism-off

\ 'brew' recorded evolutionary session playback file.
\
\  created with brew version:
\ 	brew.fs,v 1.396 2002/11/07 15:07:30 f 
\ 	genes-0.3.fs,v 1.19 2002/10/07 06:08:17 f 
\ 	mutation-0.3.fs,v 1.36 2002/10/07 06:13:09 f 

\ brew was compiled with the following nuc compile time values:
\ 7 TO nuc-organs#
\ 5 TO nuc-f-organs#
\ 5 TO nuc-parameters#
\ 3 TO nuc-f-parameters#
\ 3 TO nuc-invisibles#
\ 1 TO nuc-f-invisibles#
\ 1 TO nuc-secrets#
\ 1 TO nuc-f-secrets#

\ brew was compiled with the following world compile time values:
\ 5 TO spot-qualities#
\ 5 TO spot-f-qualities#
\ 1 TO spot-secrets#
\ 1 TO spot-f-secrets#
\ 5 TO spot-properties#
\ 5 TO spot-f-properties#


\ brew-defaults.fs  from brew-0.1.0.fs
\ 'brew-defaults.fs' sets startup defaults (except living cells).

\ This is the start up initialization file included after compiling brew.
\ It contains all brew defaults, but does not populate the current world yet.
\ (This is done by 'brew-init.fs')

\ quick&dirty initialisation for sum up experiment:

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
12 (scan-lines) !
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

\ starting from scratch:

\ restart with an unpopulated world:
free-field 	\ in world: World 0

\ brew-init.fs

\ default brew initialization of brew-0.1.0

\ 'brew-init.fs' defines individuals and sets them into the current world.

\ quick&dirty initialisation for sum up experiment:

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

\ Continued recording in brew session 2639

\ changed brew parameters:
-1449510237 seed-BRODIE !	 \ gets recorded changed or not 109

\ brew the gene soup until step 
30 |brew|
s"           Evolving code cells, seen like bacteria in a Petri shell."
40 >message
50 |brew|

\ changed brew parameters:
-953324638 seed-BRODIE !	 \ gets recorded changed or not 163
0 step-display-on OR step-snapshots OR scan-display-used OR 
 display-switch !
8 (prior-display-type) !

s"            Switch to a scan display:  Watch new genomes come and go."
60 >message
60 |brew|

s"                           A good genome has emerged."
60 >message
60 |brew|

\ ****************************************************************
\ Continued recording in brew session 2685

\ changed brew parameters:
-1344923017 seed-BRODIE !	 \ gets recorded changed or not 109

0 (scan-index) !
16 (scan-detail) !
0 0 (scan-min-max) 2!
0 160 (last-scan-min-max) 2!
\ 178 (vertical-display-range) !
60 (vertical-display-range) !

\ 1 (scan-index) !
\ 18 (scan-detail) !
\ 352 1021 (scan-min-max) 2!
\ 400 1100 (last-scan-min-max) 2!
\ 36 (vertical-display-range) !

1 (scan-index) !
34 (scan-detail) !
-1430 1508 (scan-min-max) 2!
-2500 2500 (last-scan-min-max) 2!
84 (vertical-display-range) !

\ brew the gene soup until step 300
s"                 Scanning nuc parameters while evolution continues."
80 >message
100 |brew|

1 (scan-index) !
34 (scan-detail) !
-1430 1508 (scan-min-max) 2!
-2500 2500 (last-scan-min-max) 2!
84 (vertical-display-range) !

\ brew the gene soup until step 450
s"                        Scan whatever you find interesting."
80 >message
50 |brew|

\ Continued recording in brew session 2692

\ changed brew parameters:
-79246976 seed-BRODIE !	 \ gets recorded changed or not 109

0 (scan-index) !
6 (scan-detail) !
86 407 (scan-min-max) 2!
0 407 (last-scan-min-max) 2!
108 (vertical-display-range) !
208 (vertical-display-range) !
\ 308 (vertical-display-range) !

1 (scan-index) !
17 (scan-detail) !
600 3398 (scan-min-max) 2!
0 8100 (last-scan-min-max) 2!
553 (vertical-display-range) !

\ brew the gene soup until step 400
s"                   Meanwhile brew has found good solutions."
40 >message
50 |brew|

\ brew the gene soup until step 800
s"     Evolving shorter genomes of the same quality avoids code length penalty."
80 >message
100 |brew|

s"                            Evolution takes it's time."
80 >message
100 |brew|

s"                            Reduced gene code length."
80 >message
100 |brew|

s"                       Brew found some good short genomes."
80 >message
100 |brew|

page
cr .( Let's have a look at the results:)
cr
cr
cr .( The task was to solve the equation  A = B + C)
cr
cr .( brew generated:)
cr
cr .( : g-182)
cr .(     C-property@ parameter-B@ + organ-A ! ;)
cr
cr .( in plain English,)
cr s" (forgetting about organs, parameters and properties for now):" type
cr
cr .( Genome #182:
cr .(     fetch C)
cr .(     fetch B)
cr .(     add B+C)
cr .(     store result in A)
cr
cr .( ok)
last-left s" (press a key to continue)" type
wait

\ ****************************************************************
\ a different setup:

page
cr .( Watch a similar example with a different setup now:)
cr
cr .( This evolution generates code solving the task around step 110.)
cr .( The code get's simplified by eliminating bloat then.)
cr
cr
cr s" (As the first example this setup is choosen to give a nice demo," type
cr s"  not for efficiency)." type

last-left s" (press a key to start)" type
wait

\ a possible solution ;-)
\ A = B + C

s" -" GENE: cheat-sum-B   B-property@ C-property@ + organ-A ! ;gene
internal' cheat-sum-B wake-me-actions >list

: score-sum-B ( -- -score )
    B-property @ C-property @ +  organ-A @ -  abs negate ;

: eat-sum-B ( -- )   score-sum-B negate eat-scored ;
' score-sum-B  ' eat-sum-B  eat-actions 2>list
score-sum-B scoring-xt !

sum-up				\ manipulating individual from before
' eat-sum-B	eat-xt !
guess-scoring-function
div-organ-A my-diversifctn-mask !

\ Continued recording in brew session 2717

\ restart with an unpopulated world:
free-field 	\ in world: World 0

\ changed brew parameters:
2068556821 seed-BRODIE !	 \ gets recorded changed or not 109
0 spot-display-on OR step-snapshots OR  display-switch !
1 (prior-display-type) !

\ define a nuc and set to spot:

\ define nuc and set it actual:
nuc-length# new-nucleus DROP cp!
\ needed for assertion of benchmark result:
93714 cloned !
gene' noop    	wake-me-xt !
internal' noop    	wake-me-internal !
' eat-sum-B    	eat-xt !
' cell-division    	reproduce-xt !
' <look-at>    	show-me-xt !
93784 id !
0 genome-id !
168 f-organ-offset !
208 f-parameter-offset !
232 f-invisible-offset !
240 f-secret-offset !
248 length !
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
1 my-diversifctn-mask !
0 f-organ-div-mask !
0 f-param-div-mask !
0 f-invisibl-div-mask !
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
0 div-organ-A OR  my-diversifctn-mask !
0  f-organ-div-mask !
0  f-param-div-mask !
0  f-invisibl-div-mask !
' f-organ-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-organ-B buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-organ-C buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-organ-D buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-organ-E buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-parameter-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-parameter-B buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-parameter-C buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-invisible-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-secret-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

\ now set defined nuc to spot:
1000 >spot!
|cp@| fcp !
?increase-genome-probability
nucs-not-scanned
brew-redisplay

\ changed brew parameters:
-1368135019 seed-BRODIE !	 \ gets recorded changed or not 163
0 nuc-do-cost !
0 code-price !
0 B-property-div OR C-property-div OR  spot-diversification-mask !
0 spot-display-on OR spot-background-coloring OR 
step-snapshots OR  display-switch !
' scoring-hit>bg-color background-color-xt !

\ Continued recording in brew session 2718

\ changed brew parameters:
-1368135019 seed-BRODIE !	 \ gets recorded changed or not 109
0 	internal+' 0=	actual-genepool-xt @ execute set-one
0 	internal+' 0<	actual-genepool-xt @ execute set-one
0 	internal+' 0>	actual-genepool-xt @ execute set-one
0 	internal+' =	actual-genepool-xt @ execute set-one
0 	internal+' >	actual-genepool-xt @ execute set-one
0 	internal+' <	actual-genepool-xt @ execute set-one
0 	internal+' within	actual-genepool-xt @ execute set-one
0 	internal+' AND	actual-genepool-xt @ execute set-one
0 	internal+' OR	actual-genepool-xt @ execute set-one
0 	internal+' XOR	actual-genepool-xt @ execute set-one
0 	internal+' g-IF-ELSE-THEN	actual-genepool-xt @ execute set-one
0 	internal+' dup	actual-genepool-xt @ execute set-one
0 	internal+' 2dup	actual-genepool-xt @ execute set-one
0 	internal+' drop	actual-genepool-xt @ execute set-one
500 	internal+' drop(a-)	actual-genepool-xt @ execute set-one
0 	internal+' nip	actual-genepool-xt @ execute set-one
0 	internal+' nip(aa-a)	actual-genepool-xt @ execute set-one
0 	internal+' nip(an-n)	actual-genepool-xt @ execute set-one
0 	internal+' nip(na-a)	actual-genepool-xt @ execute set-one
0 	internal+' tuck	actual-genepool-xt @ execute set-one
0 	internal+' swap	actual-genepool-xt @ execute set-one
0 	internal+' over	actual-genepool-xt @ execute set-one
0 	internal+' over(an-ana)	actual-genepool-xt @ execute set-one
2000 	internal+' +	actual-genepool-xt @ execute set-one
2000 	internal+' -	actual-genepool-xt @ execute set-one
0 	internal+' negate	actual-genepool-xt @ execute set-one
1000 	internal+' *	actual-genepool-xt @ execute set-one
1000 	internal+' ?/	actual-genepool-xt @ execute set-one
0 	internal+' @	actual-genepool-xt @ execute set-one
0 	internal+' take	actual-genepool-xt @ execute set-one
0 	internal+' !(some)	actual-genepool-xt @ execute set-one
0 	internal+' +!(some)	actual-genepool-xt @ execute set-one
0 	internal+' -!(some)	actual-genepool-xt @ execute set-one
0 	internal+' swap!(some)	actual-genepool-xt @ execute set-one
0 	internal+' take-some	actual-genepool-xt @ execute set-one
4000 	internal+' !	actual-genepool-xt @ execute set-one
0 	internal+' -!	actual-genepool-xt @ execute set-one
0 	internal+' swap!	actual-genepool-xt @ execute set-one
0 	internal+' off	actual-genepool-xt @ execute set-one
4000 	internal+' organ-A	actual-genepool-xt @ execute set-one
0 	internal+' organ-B	actual-genepool-xt @ execute set-one
0 	internal+' parameter-A@	actual-genepool-xt @ execute set-one
0 	internal+' parameter-B@	actual-genepool-xt @ execute set-one
0 	internal+' A-quality	actual-genepool-xt @ execute set-one
0 	internal+' B-quality	actual-genepool-xt @ execute set-one
0 	internal+' A-property@	actual-genepool-xt @ execute set-one
2000 	internal+' B-property@	actual-genepool-xt @ execute set-one
2000 	internal+' C-property@	actual-genepool-xt @ execute set-one

\ \ scan display:
\ 0 step-display-on OR step-snapshots OR scan-display-used OR
\ display-switch !

\ \ world display: b/w
\ 0 spot-display-on OR step-snapshots OR display-switch !

\ Continued recording in brew session 2747

\ changed brew parameters:
-1368135019 seed-BRODIE !	 \ gets recorded changed or not 109

80 |brew|			\ start

\ step 80
s" Mutated genomes have taken over." 30 >message 
30 |brew|

\ step 110
s" HEURECA!" 50 >message	\ HEURECA
90 |brew|

\ step 200	switch to scan display:
0 spot-background-coloring OR step-display-on OR 
step-snapshots OR scan-display-used OR  display-switch !
8 (prior-display-type) !
80 |brew|

\ step 280
s" Exploring code mutations." 100 >message
140 |brew|

\ step 420
s" Avoiding code length penalty favorizes shorter genomes." 100 >message
160 |brew|

\ step 580
s" Shortest useful code wins." 100 >message
220 |brew|

\ step 800	end

\ : g-1222
\     B-property@ C-property@ + organ-A ! ;

\ ****************************************************************
\ Elitism:

page
cr .( A task as simple as adding two numbers will usually be solved much)
cr .( more efficiently by applying harsh elitistic population control:)
cr
cr .( Without further adaptions it takes 8 steps to find an optimal solution)
cr s"   (I let it run longer to test, if the winner does take over, though)."
type

last-left s" (press a key to start)" type
wait


elitism!

\ Continued recording in brew session 2758

\ restart with an unpopulated world:
free-field 	\ in world: World 0

\ changed brew parameters:
-233888362 seed-BRODIE !	 \ gets recorded changed or not 109
0 spot-display-on OR spot-background-coloring OR 
step-snapshots OR  display-switch !
1 (prior-display-type) !
0 (scan-index) !
1108 1754 (scan-min-max) 2!
1013 1754 (last-scan-min-max) 2!
329 (vertical-display-range) !
1 (scan-index) !
416 (vertical-display-range) !

\ define a nuc and set to spot:

\ define nuc and set it actual:
nuc-length# new-nucleus DROP cp!
\ needed for assertion of benchmark result:
182146 cloned !
gene' noop    	wake-me-xt !
internal' noop    	wake-me-internal !
' eat-sum-B    	eat-xt !
' cell-division    	reproduce-xt !
' <look-at>    	show-me-xt !
182215 id !
0 genome-id !
168 f-organ-offset !
208 f-parameter-offset !
232 f-invisible-offset !
240 f-secret-offset !
248 length !
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
1 my-diversifctn-mask !
0 f-organ-div-mask !
0 f-param-div-mask !
0 f-invisibl-div-mask !
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
0 div-organ-A OR  my-diversifctn-mask !
0  f-organ-div-mask !
0  f-param-div-mask !
0  f-invisibl-div-mask !
' f-organ-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-organ-B buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-organ-C buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-organ-D buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-organ-E buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-parameter-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-parameter-B buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-parameter-C buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-invisible-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' f-secret-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

\ now set defined nuc to spot:
1000 >spot!
|cp@| fcp !
?increase-genome-probability
nucs-not-scanned
brew-redisplay

\ changed brew parameters:
761022102 seed-BRODIE !	 \ gets recorded changed or not 163
1 1 mutation-rate 2!
1 trial-phase !

\ brew the gene soup until step 16
8 |brew|

s" Task solved" 6 >message
8 |brew|

page
cr
cr .( Using elitism as population control mechanism.)
cr .( Brew has generated the same gene code as before.)
last-left s" (press a key to exit demo)" type wait

demo-world-created [IF]
    page
    cr
    cr
    cr
    cr .( As this demo was programmed for a 80 25 sized screen I had to create)
    cr .( a world of this dimensions for you. )
    cr
    cr .( After leaving the demo you will be left there and brew will behave)
    cr .( different as before. Maybe you want to quit brew and restart. )
    cr
    cr .( Select your prior ) c-l . l-s . .( world from world menu if you want.)
    cr s" (you can reach the world menu by pressing uppercase 'W')." type cr
    cr

    last-left s" (Key to exit demo. Then '<F12> to quit, 'W' for world menu if you want)"
    type wait
[THEN]

single-step on
mutation-must-differ on
