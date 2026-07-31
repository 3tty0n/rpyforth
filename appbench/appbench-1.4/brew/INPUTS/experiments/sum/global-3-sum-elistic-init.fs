\ global-3-sum-elistic-init..fs
\ 	$Id: global-3-sum-elistic-init.fs,v 1.6 2005/04/10 16:20:25 f Exp $	

\ initialisation file for global-3-sum.fs

false [IF] \ results
\ Elitisme gives better results here.
\ Another example of code simplification.

GENE: g-2587
    integer-D@ integer-D@ integer-B@ + + dup integer-B@ integer-C@ negate
    negate negate negate negate - organ-A -! dup integer-D@ negate + - -
    integer-C@ negate - organ-A !
;gene
    
GENE: g-3594
    integer-C@ integer-B@ negate dup dup - + integer-D@ negate + - organ-A !
;gene

GENE: g-4056
    integer-C@ integer-D@ integer-B@ + + organ-A !
;gene

[THEN]

\ 'brew' recorded evolutionary session playback file.
\
\  created with brew version:
\ 	brew.fs,v 1.385 2002/07/12 14:43:40 f 
\ 	genes-0.3.fs,v 1.18 2002/05/22 12:09:17 f 
\ 	mutation-0.3.fs,v 1.35 2002/05/24 13:36:18 f 

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


\ starting from scratch:

\ restart with an unpopulated world:
free-field 	\ in world: World 0

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
-654970927 seed-BRODIE !	 \ gets recorded changed or not 80

\ world:

\ Save world parameters : world# 0  named: World 0
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

\ global variables:	integers:9   / dfloats:9 
0 integer-A !
0 integer-B !
0 integer-C !
0 integer-D !
0 integer-E !
0 integer-F !
0 integer-G !
0 integer-H !
0 integer-I !
' dfloat-A buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' dfloat-B buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' dfloat-C buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' dfloat-D buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' dfloat-E buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' dfloat-F buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' dfloat-G buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' dfloat-H buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' dfloat-I buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
0 |A OR |B OR |C OR |D OR |E OR  global-f-organ-div-mask !
0 |A OR |B OR |C OR  global-f-parameter-div-mask !
0 |A OR  global-f-invisible-div-mask !
' nuc-f-diversification-rate buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' nuc-f-diversification-range buffered" 50e0  0 0 0 0 0 0 49 40 " buffered-float!
' nuc-f-diversification-factor buffered" 2e0  0 0 0 0 0 0 0 40 " buffered-float!
' f-sporadic-value-rate buffered" 0.01e0  7B 14 AE 47 E1 7A 84 3F " buffered-float!
' f-sporadic-value-range buffered" 100000e0  0 0 0 0 0 6A F8 40 " buffered-float!
0 C-property-div OR  spot-diversification-mask !
500 spot-diversification-range !
2 spot-diversifictn-closeness !
0  f-qualities-div-mask !
0  f-properties-div-mask !
0  f-secrets-div-mask !
' spot-f-diversification-range buffered" 50e0  0 0 0 0 0 0 49 40 " buffered-float!
' spot-f-diversification-factor buffered" 1e0  0 0 0 0 0 0 F0 3F " buffered-float!
0 |B OR |C OR |D OR  global-diversification-mask !
1 1 global-i-diversifictn-rate 2!
500 globals-diversifictn-range !
2 globals-divers-closeness !
0  global-df-div-mask !
0 1 global-f-diversifctn-rate 2!
' global-f-diversifctn-range buffered" 50e0  0 0 0 0 0 0 49 40 " buffered-float!
' global-f-diversifctn-factor buffered" 2e0  0 0 0 0 0 0 0 40 " buffered-float!
0 spot-display-on OR step-snapshots OR  display-switch !

\ display:
' show-genome-b look-at-xt !
127 snapshot-frequency !
100 2-ascii-scale !
' f-2-ascii-scale buffered" 0.001e0  FC A9 F1 D2 4D 62 50 3F " buffered-float!
' energy show-int-nuc-var-xt !
9 show-sign-tolerance !
' f-organ-A show-float-nuc-var-xt !
' float-show-sign-tolerance buffered" 9e0  0 0 0 0 0 0 22 40 " buffered-float!
1 (prior-display-type) !
5 display-slots !
' .step 0 display-slot !
' .cells 1 display-slot !
' .living 2 display-slot !
' .scoring 3 display-slot !
' .score 4 display-slot !
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
0 (scan-index) !	\ unique: 4016C7E06C04013F8D84013F9401000000000000000000005141
' nuc-scan-display (scan-xt) !
6 (scan-detail) !
12 (scan-lines) !
' blue scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 (vertical-display-range) !
0 cont-scan-nucs OR  (scan-flags) !
1 (scan-index) !	\ unique: 4016C7E0DC04013F8D84013F9400000000000000000000005141
' nuc-scan-display (scan-xt) !
17 (scan-detail) !
12 (scan-lines) !
' blue scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
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
0 (vertical-display-range) !
0 fixed-horizontal-range OR  (scan-flags) !

2 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
3 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
4 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
5 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
6 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
7 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
8 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
9 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
10 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
0 0 vertical-zoom-scale 2!
0 (vertical-display-range) !
0  (scan-flags) !
11 (scan-index) !	\ unique: 00004013F9404013F9400000000000000000000005100
0 (scan-xt) !
0 (scan-detail) !
0 (scan-lines) !
' default-color scan-background-xt !
' default-color scan-foreground-xt !
0 0 (scan-min-max) 2!
0 0 (last-scan-min-max) 2!
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
type-unknown% 	0 continuous-display-list n'th-or-new-node  >cont-var-type ! 
98 		0 continuous-display-list n'th-or-new-node  >cont-lower ! 
160 		0 continuous-display-list n'th-or-new-node  >cont-upper ! 
' red 	0 continuous-display-list n'th-or-new-node  >cont-foreground-xt ! 
42 		0 continuous-display-list n'th-or-new-node  >cont-char ! 
' get-variable 	1 continuous-display-list n'th-or-new-node  >cont-xt ! 
' living 	1 continuous-display-list n'th-or-new-node  >cont-item ! 
type-unknown% 	1 continuous-display-list n'th-or-new-node  >cont-var-type ! 
0 		1 continuous-display-list n'th-or-new-node  >cont-lower ! 
1920 		1 continuous-display-list n'th-or-new-node  >cont-upper ! 
' white 	1 continuous-display-list n'th-or-new-node  >cont-foreground-xt ! 
120 		1 continuous-display-list n'th-or-new-node  >cont-char ! 

\ mutation:
1 1 mutation-rate 2!
3 stack-turning-point !
1 mutations-threshold !
50000 mutation-max-ollowed-items !
1 trial-phase !
4 max-if-items !
33 conditional-token-price !
1 resolve-flags !
0 reset-nuc-masks? !
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
1000 	internal+' dup	actual-genepool-xt @ execute set-one
0 	internal+' 2dup	actual-genepool-xt @ execute set-one
0 	internal+' drop	actual-genepool-xt @ execute set-one
0 	internal+' drop(a-)	actual-genepool-xt @ execute set-one
0 	internal+' nip	actual-genepool-xt @ execute set-one
0 	internal+' nip(aa-a)	actual-genepool-xt @ execute set-one
0 	internal+' nip(an-n)	actual-genepool-xt @ execute set-one
0 	internal+' nip(na-a)	actual-genepool-xt @ execute set-one
0 	internal+' tuck	actual-genepool-xt @ execute set-one
0 	internal+' swap	actual-genepool-xt @ execute set-one
0 	internal+' over	actual-genepool-xt @ execute set-one
0 	internal+' over(an-ana)	actual-genepool-xt @ execute set-one
1000 	internal+' +	actual-genepool-xt @ execute set-one
1000 	internal+' -	actual-genepool-xt @ execute set-one
1000 	internal+' negate	actual-genepool-xt @ execute set-one
0 	internal+' *	actual-genepool-xt @ execute set-one
0 	internal+' ?/	actual-genepool-xt @ execute set-one
1000 	internal+' @	actual-genepool-xt @ execute set-one
0 	internal+' take	actual-genepool-xt @ execute set-one
0 	internal+' !(some)	actual-genepool-xt @ execute set-one
0 	internal+' +!(some)	actual-genepool-xt @ execute set-one
0 	internal+' -!(some)	actual-genepool-xt @ execute set-one
0 	internal+' swap!(some)	actual-genepool-xt @ execute set-one
0 	internal+' take-some	actual-genepool-xt @ execute set-one
1000 	internal+' !	actual-genepool-xt @ execute set-one
1000 	internal+' -!	actual-genepool-xt @ execute set-one
0 	internal+' swap!	actual-genepool-xt @ execute set-one
0 	internal+' off	actual-genepool-xt @ execute set-one
1000 	internal+' organ-A	actual-genepool-xt @ execute set-one
0 	internal+' organ-B	actual-genepool-xt @ execute set-one
0 	internal+' organ-C	actual-genepool-xt @ execute set-one
0 	internal+' organ-D	actual-genepool-xt @ execute set-one
0 	internal+' organ-E	actual-genepool-xt @ execute set-one
0 	internal+' organ-F	actual-genepool-xt @ execute set-one
0 	internal+' organ-G	actual-genepool-xt @ execute set-one
0 	internal+' parameter-A@	actual-genepool-xt @ execute set-one
0 	internal+' parameter-B@	actual-genepool-xt @ execute set-one
0 	internal+' parameter-C@	actual-genepool-xt @ execute set-one
0 	internal+' parameter-D@	actual-genepool-xt @ execute set-one
0 	internal+' parameter-E@	actual-genepool-xt @ execute set-one
0 	internal+' A-quality	actual-genepool-xt @ execute set-one
0 	internal+' B-quality	actual-genepool-xt @ execute set-one
0 	internal+' C-quality	actual-genepool-xt @ execute set-one
0 	internal+' D-quality	actual-genepool-xt @ execute set-one
0 	internal+' E-quality	actual-genepool-xt @ execute set-one
0 	internal+' A-property@	actual-genepool-xt @ execute set-one
0 	internal+' B-property@	actual-genepool-xt @ execute set-one
0 	internal+' C-property@	actual-genepool-xt @ execute set-one
0 	internal+' D-property@	actual-genepool-xt @ execute set-one
0 	internal+' E-property@	actual-genepool-xt @ execute set-one
0 	internal+' integer-A@	actual-genepool-xt @ execute set-one
1000 	internal+' integer-B@	actual-genepool-xt @ execute set-one
1000 	internal+' integer-C@	actual-genepool-xt @ execute set-one
1000 	internal+' integer-D@	actual-genepool-xt @ execute set-one
0 	internal+' integer-E@	actual-genepool-xt @ execute set-one
0 	internal+' integer-F@	actual-genepool-xt @ execute set-one
0 	internal+' integer-G@	actual-genepool-xt @ execute set-one
0 	internal+' integer-H@	actual-genepool-xt @ execute set-one
0 	internal+' integer-I@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-A-f@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-B-f@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-C-f@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-D-f@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-E-f@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-F-f@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-G-f@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-H-f@	actual-genepool-xt @ execute set-one
0 	internal+' dfloat-I-f@	actual-genepool-xt @ execute set-one
0 	internal+' age@	actual-genepool-xt @ execute set-one
0 	internal+' age-threshold@	actual-genepool-xt @ execute set-one
0 	internal+' energy@	actual-genepool-xt @ execute set-one
0 	internal+' reproduction-threshold@	actual-genepool-xt @ execute set-one
0 	internal+' f-organ-A	actual-genepool-xt @ execute set-one
0 	internal+' f-organ-B	actual-genepool-xt @ execute set-one
0 	internal+' f-organ-C	actual-genepool-xt @ execute set-one
0 	internal+' f-organ-D	actual-genepool-xt @ execute set-one
0 	internal+' f-organ-E	actual-genepool-xt @ execute set-one
0 	internal+' f-parameter-A-f@	actual-genepool-xt @ execute set-one
0 	internal+' f-parameter-B-f@	actual-genepool-xt @ execute set-one
0 	internal+' f-parameter-C-f@	actual-genepool-xt @ execute set-one
0 	internal+' A-f-quality	actual-genepool-xt @ execute set-one
0 	internal+' B-f-quality	actual-genepool-xt @ execute set-one
0 	internal+' C-f-quality	actual-genepool-xt @ execute set-one
0 	internal+' D-f-quality	actual-genepool-xt @ execute set-one
0 	internal+' E-f-quality	actual-genepool-xt @ execute set-one
0 	internal+' A-f-property@	actual-genepool-xt @ execute set-one
0 	internal+' B-f-property@	actual-genepool-xt @ execute set-one
0 	internal+' C-f-property@	actual-genepool-xt @ execute set-one
0 	internal+' D-f-property@	actual-genepool-xt @ execute set-one
0 	internal+' E-f-property@	actual-genepool-xt @ execute set-one
0 	internal+' df@	actual-genepool-xt @ execute set-one
0 	internal+' f-take	actual-genepool-xt @ execute set-one
0 	internal+' df!	actual-genepool-xt @ execute set-one
0 	internal+' df+!	actual-genepool-xt @ execute set-one
0 	internal+' df-!	actual-genepool-xt @ execute set-one
0 	internal+' fdup	actual-genepool-xt @ execute set-one
0 	internal+' fdrop	actual-genepool-xt @ execute set-one
0 	internal+' drop(float-pointer)	actual-genepool-xt @ execute set-one
0 	internal+' fswap	actual-genepool-xt @ execute set-one
0 	internal+' fover	actual-genepool-xt @ execute set-one
0 	internal+' frot	actual-genepool-xt @ execute set-one
0 	internal+' f+	actual-genepool-xt @ execute set-one
0 	internal+' f-	actual-genepool-xt @ execute set-one
0 	internal+' f*	actual-genepool-xt @ execute set-one
0 	internal+' f/	actual-genepool-xt @ execute set-one
0 	internal+' fnegate	actual-genepool-xt @ execute set-one
0 	internal+' fabs	actual-genepool-xt @ execute set-one
0 	internal+' fmax	actual-genepool-xt @ execute set-one
0 	internal+' fmin	actual-genepool-xt @ execute set-one
0 	internal+' f2*	actual-genepool-xt @ execute set-one
0 	internal+' f2/	actual-genepool-xt @ execute set-one
0 	internal+' 1/f	actual-genepool-xt @ execute set-one
0 	internal+' f**	actual-genepool-xt @ execute set-one
0 	internal+' fsqrt	actual-genepool-xt @ execute set-one
0 	internal+' fexp	actual-genepool-xt @ execute set-one
0 	internal+' fexpm1	actual-genepool-xt @ execute set-one
0 	internal+' fln	actual-genepool-xt @ execute set-one
0 	internal+' flnp1	actual-genepool-xt @ execute set-one
0 	internal+' flog	actual-genepool-xt @ execute set-one
0 	internal+' falog	actual-genepool-xt @ execute set-one
0 	internal+' fsin	actual-genepool-xt @ execute set-one
0 	internal+' fcos	actual-genepool-xt @ execute set-one
0 	internal+' fsincos	actual-genepool-xt @ execute set-one
0 	internal+' ftan	actual-genepool-xt @ execute set-one
0 	internal+' fasin	actual-genepool-xt @ execute set-one
0 	internal+' facos	actual-genepool-xt @ execute set-one
0 	internal+' fatan	actual-genepool-xt @ execute set-one
0 	internal+' fatan2	actual-genepool-xt @ execute set-one
0 	internal+' fsinh	actual-genepool-xt @ execute set-one
0 	internal+' fcosh	actual-genepool-xt @ execute set-one
0 	internal+' ftanh	actual-genepool-xt @ execute set-one
0 	internal+' fasinh	actual-genepool-xt @ execute set-one
0 	internal+' facosh	actual-genepool-xt @ execute set-one
0 	internal+' fatanh	actual-genepool-xt @ execute set-one
0 	internal+' pi	actual-genepool-xt @ execute set-one
0 	internal+' f+i	actual-genepool-xt @ execute set-one
0 	internal+' f-i	actual-genepool-xt @ execute set-one
0 	internal+' f*i	actual-genepool-xt @ execute set-one
0 	internal+' f/i	actual-genepool-xt @ execute set-one
0 	internal+' i/i	actual-genepool-xt @ execute set-one
0 	internal+' i+f	actual-genepool-xt @ execute set-one
0 	internal+' i-f	actual-genepool-xt @ execute set-one
0 	internal+' i*f	actual-genepool-xt @ execute set-one
0 	internal+' i/f	actual-genepool-xt @ execute set-one
0 	internal+' f/f	actual-genepool-xt @ execute set-one
0 	internal+' f*f	actual-genepool-xt @ execute set-one
0 	internal+' i*f>i	actual-genepool-xt @ execute set-one
0 	internal+' i/f>i	actual-genepool-xt @ execute set-one
0 	internal+' f>s	actual-genepool-xt @ execute set-one
0 	internal+' s>f	actual-genepool-xt @ execute set-one
0 	internal+' f<	actual-genepool-xt @ execute set-one
0 	internal+' f>	actual-genepool-xt @ execute set-one
0 	internal+' f0<	actual-genepool-xt @ execute set-one
0 	internal+' f0=	actual-genepool-xt @ execute set-one
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
' f-organ-A (expr-df-xt-1) !
' f-organ-B (expr-df-xt-2) !
' energy (xt-do-it) !
' f-organ-A (df-xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' (expr-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (expr-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
' A-f-quality (expr-df-xt-1) !
' B-f-quality (expr-df-xt-2) !
' food (xt-do-it) !
' A-f-quality (df-xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' (expr-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (expr-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
' f-organ-A (expr-df-xt-1) !
' f-organ-B (expr-df-xt-2) !
' noop (xt-do-it) !
' f-organ-A (df-xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' (expr-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (expr-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
' (no-one) (expr-df-xt-1) !
' (no-one) (expr-df-xt-2) !
' energy (xt-do-it) !
' f-organ-A (df-xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' (expr-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (expr-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
' (no-one) (expr-df-xt-1) !
' (no-one) (expr-df-xt-2) !
' noop (xt-do-it) !
' f-organ-A (df-xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' (expr-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (expr-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
' A-f-quality (expr-df-xt-1) !
' B-f-quality (expr-df-xt-2) !
' food (xt-do-it) !
' A-f-quality (df-xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do-simple (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' (expr-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (expr-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
' f-organ-A (expr-df-xt-1) !
' f-organ-B (expr-df-xt-2) !
' noop (xt-do-it) !
' f-organ-A (df-xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' (expr-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (expr-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
' A-f-quality (expr-df-xt-1) !
' B-f-quality (expr-df-xt-2) !
' noop (xt-do-it) !
' A-f-quality (df-xt-do-it) !
0 (do-it-parameter) !
0 (do-it-parameter-2) !
1 1 (do-it-scale) 2!
' maybe-do (maybe-do-type-xt) !
buffered" " (expression-handle) !
buffered" " (maybe-do-handle) !
' (expr-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (expr-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!
' (do-it-df-parameter-2) buffered" 0e0  0 0 0 0 0 0 0 0 " buffered-float!

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
0 (diversification-menu-type) !
0 (nuc-menus-show-dfloats) !
0 (show-float-type-counts) !
-1 (nuc-menu-visible-floats) !
-1 (spot-menus-show-dfloats) !
0 (menu-global-vars-show-dfloats) !
base !

\ user did '|include-file|': 
INCLUDE INPUTS/experiments/sum/global-3-sum.fs 

\ define a nuc and set to spot:

\ define nuc and set it actual:
nuc-length# new-nucleus DROP cp!
\ needed for assertion of benchmark result:
71 cloned !
gene' noop    	wake-me-xt !
internal' noop    	wake-me-internal !
' eat-global-3-sum    	eat-xt !
' cell-division    	reproduce-xt !
' <look-at>    	show-me-xt !
71 id !
11 genome-id !
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
0 my-diversifctn-mask !
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
0  my-diversifctn-mask !
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

elitism!

s" Initialisation file 'global-3-sum-elistic-init.fs' included.  Experiment ready."
1 >message

menu-leave on		\ go to top level menu = brew main screen
single-step on		\ might be off from before
