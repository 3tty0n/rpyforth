\ old-demos.fs
\ 	$Id: old-demos.fs,v 1.6 2005/04/17 06:17:29 f Exp $	

\ ****************************************************************
\ ************************  old Demos  ***************************
\ ****************************************************************

\ These are old demos.
\ They are not guaranteed to work on this version.
\ Newer demos use the playback feature, these here don't.

individuals-dir s" old-brew-individuals.fs" file-name-cat
dup string@ REQUIRED  stringbuf-close

\ LIST: demos

\ hack including demo on default initialisation
: default-0.1.0 ( -- )
    s" INPUTS/experiments/sum/default-0.1.0.fs"  ['] INCLUDED
    CATCH IF 2drop THEN ;
' default-0.1.0 demos >list

nuc-organs# [IF] \ at least organ-A defined.
: symbiotic-waves ( -- )	\ demo
    page
    ." Demo symbiotic behavior:	" cr
    cr 
    ." 2 species of cells, one increasing one decreasing the A-quality" cr
    ." of the spot they live on, whatever this A-quality might be." cr
    cr
    ." But the species that decreases the A-quality likes high A," cr
    ." while the other one wants low A, but produces A." cr
    ." Hmm, sounds familiar, somehow." cr
    cr
    ." The cells have an inner organ which controlls the strength of" cr
    ." the A-quality dependence. It's called organ-A."
    cr
    ." As parameters are set, both species just can survive and reproduce," cr
    ." if they start on a zero 'A' level." cr
    cr
    ." But on a spot, another cell of the same species was living before," cr
    ." a cell will not be able to reproduce." cr
    ." They will do very well on spots, where individuals of the other" cr
    ." species have left a changed A-quality before. " cr
    cr
    ." Selection lets this effect increase a lot over time by prefering" cr
    ." individuals with high organ-A value." cr
    cr
    cr
    ." So the cell population will always move and follow each other." cr
    ." The two species make waves and whirles of find repetive patterns." cr
    ." If one species vanishes, the other one will vanish quickly too." cr
    cr
    ." Press 'o' or <F2> to switch between world and scan display." cr
    ." Press 'b' to toggle background colour." cr
    cr
    ." Give any number as seed for the random generator: "
    seed-BRODIE @ .  ['] seed-BRODIE change-named-variable

    elitism-off
    0 spot-display-on OR step-snapshots OR  display-switch !

    \ colours:
    ['] A-quality>color background-color-xt !
    ['] generation>color foreground-color-xt !
    ['] default-color color-selected-fg-xt !
    ['] magenta color-below-fg-xt !
    ['] cyan color-above-fg-xt !
    ['] blue color-miss-fg-xt !
    ['] cyan color-selected-bg-xt !
    ['] magenta color-below-bg-xt !
    ['] blue color-above-bg-xt !
    ['] blue color-miss-bg-xt !
    2498 food>color-scale !
    63 A-quality>color-scale !

    code-price off
    0 nuc-do-cost !
    1 additive-stress !
    1 1 stress-rate 2!
    free-field
    \    1085634929 seed-BRODIE !
    \    1234567890 seed-BRODIE !
    \ 0 seed-BRODIE !
    \ 34567 seed-BRODIE !
    red-eater 3 sow drop
    blue-eater 5 sow drop
    time-step
    1		world-do-direction !
    0		world-food-supply !
    200		food-share/spot !
    0		individual-fixed-food-share !
    0		leave-energy-after-death !
    1		additive-stress !
    1 1		stress-rate 2!
    640		high-water-mark !
    spots	flood-mark !
    0		sos-mark !
    100		low-water-mark !
    1000	up-regulation-start !
    33		diversification-range !
    1 4		diversification-rate 2!
    2		nuc-diversification-closeness !
    diversification-mask dup @ 1 or swap !
    0 1		mutation-rate 2!
    ['] A-quality>color		background-color-xt !
    ['] generation>color	foreground-color-xt !
    4 display-slots !
    ['] .step 0 display-slot !
    ['] .cells 1 display-slot !
    ['] .burden 2 display-slot !
    ['] .living 3 display-slot !
    (scan-index) off
    14 (scan-detail) !
    ['] toggle-display-&-go F2-xt !

    \ save-step-display-settings:
    2 step-display-items !
    0 (scan-index) !
    ['] spot-scan-display (scan-xt) !
    2 (scan-detail) !
    12 (scan-lines) !
    ['] blue scan-background-xt !
    ['] default-color scan-foreground-xt !
    -200 200 (scan-min-max) 2!
    -200 200 (last-scan-min-max) 2!
    1 5 horizontal-zoom-scale 2!
    1 4 vertical-zoom-scale 2!
    120 (vertical-display-range) !
    0 cont-scan-nucs OR  (scan-flags) !

    1 (scan-index) !
    ['] nuc-scan-display (scan-xt) !
    26 (scan-detail) !
    12 (scan-lines) !
    ['] blue scan-background-xt !
    ['] default-color scan-foreground-xt !
    0 300 (scan-min-max) 2!
    0 300 (last-scan-min-max) 2!
    1 5 horizontal-zoom-scale 2!
    1 4 vertical-zoom-scale 2!
    120 (vertical-display-range) !
    0  (scan-flags) !
    0 (step-more-info) !

    0 spot-display-on OR spot-background-coloring OR
    step-snapshots OR  display-switch ! ;
' symbiotic-waves demos >list

[THEN] \ at least organ-A defined.


nuc-organs# 2 > [IF] \ at least three organs defined.

: nomadic-bursts ( -- )		\ demo
    page cr
    ." This demo needs some moments to develop." cr
    ." After a few hundred steps islands of resident cell cultures emerge." cr cr
    ." As food is spread everywhere, food level increases in the empty spaces."
    cr
    ." This leads to sudden bursts of nomadic mutants, eating that food up." cr
    cr
    ." You can watch food level by pressing 'b'." cr
    ." Calibrate the food to color scale in the color menu 'C'." cr
    cr
    ." The waves of moving cells change some qualities of the places they pass by" cr
    ." in a mutation specific way." cr
    ." These changed qualities influence subsequent waves of nomadic cells." cr
    cr
    ." Change what background color shows to 'A','B', or 'C-quality' to see it." cr cr
    ." Sometimes some nomades settle down and form a new inhabited island." cr
    cr
    ." Try changing food policy in the food menu, reached by pressing 'F'."
    cr cr
    ." <SPACE> starts and stops the demo." cr
    cr
    ." You can give a number as seed for the random generator: " cr
    seed-BRODIE @ .  ['] seed-BRODIE change-named-variable

    elitism-off
    0 spot-display-on OR step-snapshots OR  display-switch !

    \ save-step-display-settings:
    1 step-display-items !
    0 (scan-index) !
    ['] spot-scan-display (scan-xt) !
    1 (scan-detail) !
    24 (scan-lines) !
    ['] blue scan-background-xt !
    ['] default-color scan-foreground-xt !
    0 2000 (scan-min-max) 2!
    0 2000 (last-scan-min-max) 2!
    1 5 horizontal-zoom-scale 2!
    1 2 vertical-zoom-scale 2!
    1920 (vertical-display-range) !
    0 cont-scan-nucs OR  (scan-flags) !
    
    code-price off
    0 nuc-do-cost !
    1 additive-stress !
    1 1 stress-rate 2!
    free-field
    rainbow-eater
    ['] <look-at> show-me-xt !
    ['] show-ABC*. look-at-xt !
    3 sow drop
    ['] show-ABC-X| show-me-xt !
    4 sow drop
    time-step
    1		world-do-direction !
    0 		world-food-supply !
    10 		food-share/spot !
    0 		individual-fixed-food-share !
    0 		leave-energy-after-death !
    -1 		nuc-cost-can-be-help? !
    0 		leave-energy-after-death !
    1 		additive-stress !
    1 1		stress-rate 2!
    320 	high-water-mark !
    spots	flood-mark !
    0		sos-mark !
    100 	low-water-mark !
    1000 	up-regulation-start !
    -1 		nuc-cost-can-be-help? !
    50 		diversification-range !
    1 4		diversification-rate 2!
    2 		nuc-diversification-closeness !
    diversification-mask dup @ 7 or swap !
    0 1 mutation-rate 2!
    ['] food>color 	 background-color-xt !
    ['] generation>color 	 foreground-color-xt !
    \ ['] show-~|'.* 	 look-at-xt !
    600 		 food>color-scale !
    300 		 A-quality>color-scale !
    300 		 B-quality>color-scale !
    300 		 C-quality>color-scale !
    4 display-slots !
    ['] .step 0 display-slot !
    ['] .cells 1 display-slot !
    ['] .burden 2 display-slot !
    ['] .living 3 display-slot ! ;
' nomadic-bursts demos >list

[THEN] \ at least three organs defined.


nuc-organs# [IF] \ at least organ-A defined.

: set-yeast-display ( -- )
    \ save-step-display-settings:
    2 step-display-items !
    0 (scan-index) !
    ['] spot-scan-display (scan-xt) !
    ['] blue scan-background-xt !
    ['] default-color scan-foreground-xt !
    1 (scan-detail) !
    12 (scan-lines) !
    0 100 (scan-min-max) 2!
    0 100 (last-scan-min-max) 2!
    1 5 horizontal-zoom-scale 2!
    1 4 vertical-zoom-scale 2!
    474 (vertical-display-range) !
    0 cont-scan-nucs OR  (scan-flags) !

    1 (scan-index) !
    ['] nuc-scan-display (scan-xt) !
    ['] blue scan-background-xt !
    ['] default-color scan-foreground-xt !
    18 (scan-detail) !
    12 (scan-lines) !
    0 200 (scan-min-max) 2!
    0 200 (last-scan-min-max) 2!
    1 5 horizontal-zoom-scale 2!
    1 4 vertical-zoom-scale 2!
    58 (vertical-display-range) !
    0  (scan-flags) !

    0 (step-more-info) ! ;

: yeast1 ( -- )		\ demo
    page cr
    ." Demo population control." cr
    cr
    ." Fertile but quite short living cells showing an explosive population growth." cr
    cr
    ." Population is controlled by food sharing and increasing the life cost 'burden'," cr
    ." until there are no more individuals than the high water mark." cr
    cr
    ." Mutation does only change the look of these cells, not the behavior." cr
    wait

    set-yeast-display
    elitism-off
    0 spot-display-on OR step-snapshots OR  display-switch !

    code-price off
    0 nuc-do-cost !
    1 additive-stress !
    1 1 stress-rate 2!
    free-field

    prototype
    [char] * [char] 0 - organ-A !
    ['] show-A look-at-xt !
    1 2-ascii-scale !
    1 food-share/spot !
    ['] food>color background-color-xt !
    12 food>color-scale !
    5 individual-fixed-food-share !
    2000 world-food-supply !
    1 additive-stress !
    1 1 stress-rate 2!
    420 high-water-mark !
    spots flood-mark !
    0 sos-mark !
    100 low-water-mark !
    100 up-regulation-start !
    diversification-mask dup @ 1 or swap !
    1 my-diversifctn-mask !
    0 leave-energy-after-death !
    -1 nuc-cost-can-be-help? !
    0 leave-energy-after-death !
    0 1 mutation-rate 2!

    ['] <look-at> show-me-xt !
    ['] show-A look-at-xt !

    ['] random-BRODIE random-xt !
    1334532601 seed-BRODIE !
    
    3 sow drop
    time-step
    1 world-do-direction !
    1 100 diversification-rate 2!
    2 diversification-range !
    1 nuc-diversification-closeness !
    1 2-ascii-scale !
    4 display-slots !
    ['] .step 0 display-slot !
    ['] .cells 1 display-slot !
    ['] .burden 2 display-slot !
    ['] .living 3 display-slot ! ;
' yeast1 demos >list

: yeast2 ( -- )		\ demo
    page cr
    ." Demo population control." cr
    cr
    ." Very fertile but short living cells showing an explosive population growth." cr
    cr
    ." Population is controlled only by the fact, that most food is divided equally" cr
    ." among all living cells. A small amount of food is spread everywhere," cr
    ." to let the cell cultures move." cr
    cr
    ." Mutation does only change the look of these cells, not the behavior." cr
    wait

    set-yeast-display
    1 world-do-direction !
    2000 world-food-supply !
    1 food-share/spot !
\    7 individual-fixed-food-share !
    0 individual-fixed-food-share !
    0 nuc-do-cost !
    0 leave-energy-after-death !
    1 additive-stress !
    1 1 stress-rate 2!
    640 high-water-mark !
    spots flood-mark !
    0 sos-mark !
    100 low-water-mark !
    100 up-regulation-start !
    -1 nuc-cost-can-be-help? !
    diversification-mask dup @ 1 or swap !
    ['] food>color background-color-xt !
    ['] generation>color foreground-color-xt !
    ['] show-A look-at-xt !
    1 2-ascii-scale !
    9 food>color-scale !
    ['] random-BRODIE random-xt !
    1334532601 seed-BRODIE !
    0 cell-division-moves-both !
    0 cell-division-diversify-both !
    0 cell-division-mutate-both !
    0 1 mutation-rate 2!
    spot-display-on display-switch !
    3 display-slots !
    ['] .step 0 display-slot !
    ['] .cells 1 display-slot !
    ['] .living 2 display-slot !

    elitism-off
    0 spot-display-on OR step-snapshots OR  display-switch !

    code-price off
    0 nuc-do-cost !
    1 additive-stress !
    1 1 stress-rate 2!
    free-field
    fertile
    ['] <look-at> show-me-xt !
    ['] show-A look-at-xt !

    1 my-diversifctn-mask !
    5 diversification-range !
    1 nuc-diversification-closeness !
    3 sow-diversified drop
    time-step
    2 diversification-range !
    1 100 diversification-rate 2! ;
' yeast2 demos >list

: selecting-appetite ( -- )
    page
    ." This demo shows a very simple selective adaption." cr
    cr
    ." There is abundance of food here." cr
    ." But these cells eat only a limited amount, controlled by the value in organ-A." cr
    ." This appetite is diversified in mutations." cr
    ." The numbers shown in the beginning are appetite divided by 100." cr
    cr
    ." The high water mark is set to 100 individuals, so very soon the conditions" cr
    ." will automatically get harder.  Individuals that eat more survive better." cr
    ." So there is a constant positive selection of the hungriest cells, which" cr
    ." lets the value in organ-A increase constantly.  Press 'o' or <F2> to see." cr
    cr
    ." You can see a summary of the values in all living cells by pressing 'n'." cr
    cr
    ." As food supply is not the problem here the cell cultures have no need to move." cr
    ." As the appetite grows the cells eat up all the food that summed up and the cell" cr
    ." cultures start to move more and more.  Finally they form a repeating pattern" cr
    ." (or eventually die out)." cr
    cr
    ." Things could develop similar in the real world if we get too greedy..." cr
    cr
    ." Here you can change it by increasing the food amount in the food menu reached" cr
    ." by pressing 'F'."
    wait

    elitism-off
    \ save-step-display-settings:
    2 step-display-items !
    0 (scan-index) !
    ['] nuc-scan-display (scan-xt) !
    14 (scan-detail) !
    12 (scan-lines) !
    ['] blue scan-background-xt !
    ['] default-color scan-foreground-xt !
    0 12 (scan-min-max) 2!
    0 12 (last-scan-min-max) 2!
    1 5 horizontal-zoom-scale 2!
    1 4 vertical-zoom-scale 2!
    54 (vertical-display-range) !
    0 cont-scan-nucs OR  (scan-flags) !
    
    1 (scan-index) !
    ['] nuc-scan-display (scan-xt) !
    26 (scan-detail) !
    12 (scan-lines) !
    ['] blue scan-background-xt !
    ['] default-color scan-foreground-xt !
    0 600 (scan-min-max) 2!
    0 600 (last-scan-min-max) 2!
    1 5 horizontal-zoom-scale 2!
    1 4 vertical-zoom-scale 2!
    90 (vertical-display-range) !
    0  (scan-flags) !
    
    0 (step-more-info) !
    0 spot-display-on OR step-snapshots OR  display-switch !

    code-price off
    0 nuc-do-cost !
    1 additive-stress !
    1 1 stress-rate 2!
    free-field
    Yuppie
    5 sow drop
    time-step

    1 world-do-direction !
    0 world-food-supply !
    200 food-share/spot !
    0 individual-fixed-food-share !
    0 nuc-do-cost !
    0 leave-energy-after-death !
    1 additive-stress !
    1 1 stress-rate 2!
    100 high-water-mark !
    spots flood-mark !
    0 sos-mark !
    40 low-water-mark !
    1000 up-regulation-start !
    -1 nuc-cost-can-be-help? !
    50 diversification-range !
    1 4 diversification-rate 2!
    2 nuc-diversification-closeness !
    diversification-mask dup @ 1 or swap !
    ['] food>color background-color-xt !
    ['] generation>color foreground-color-xt !
    100 2-ascii-scale !
    0 cell-division-moves-both !
    0 cell-division-diversify-both !
    0 cell-division-mutate-both !
    0 1 mutation-rate 2!
    spot-display-on display-switch !
    4 display-slots !
    ['] .step 0 display-slot !
    ['] .cells 1 display-slot !
    ['] .burden 2 display-slot !
    ['] .living 3 display-slot !
    (scan-index) off
    14 (scan-detail) !
    ['] toggle-display-&-go F2-xt ! ;
' selecting-appetite demos >list

[THEN] \ at least organ-A defined.

\ ****************************************************************
\ end	old demos
