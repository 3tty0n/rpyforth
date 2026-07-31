\ world-loop.fs
\ 	$Id: world-loop.fs,v 1.18 2005/04/20 15:06:19 f Exp $	

\ Looping over all spots of all dimensions of the world.
\ Call all spot and nuc related functions and display if appropriate.

\ In normal (sequential) brew operation it runs in a loop for each dimension.
\ Each loop determines a visibility tristate flag which can be 'invisible'
\ 'maybe-visible' or 'visible' and passes that to the next dimensions loop.
0			\ defining visibility flag values:
ENUM: invisible		\ definitely invisible
ENUM: maybe-visible	\ currently invisible, but could switch on 
ENUM: visible		\ visible, but could switch off
drop

\ Set a spot actual and do all spot and (eventually) all nuc actions:
: spot-do ( spot -- )
    >spot!		\ set spot actual
    ?diversify-inhabited
    spot-do-xt @ EXECUTE	\ optional actions
    fcp @ dup IF		\ hello, somebody here?
	cp!			\ yes, set cell as actual one

	log-mask @ IF \ checking logging (for speed reasons)
	    log-cat-step&spot
	    log-cat-id
	    on-trial? IF
		s"  on trial"
		[ log-spot log-trial or ] literal log-it
	    ELSE
		s" " log-spot log-it
	    THEN
	THEN

	feed-individual \ feed the food only given to living cells

	cell-do-before-xt @ EXECUTE	\ you can put something in here
	nuc-do-all			\ and wake her up
	cell-do-after-xt @ EXECUTE	\ you can put something in here
    ELSE					\ noone here
	drop
	spot-display-on? IF
	    show-background
	THEN
[ log-mask @ ] [IF]
	log-mask @ 0= IF  EXIT  THEN \ checking logging (for speed reasons)
	log-mask @ log-empty-spots and IF
	    log-cat-step&spot
	    s"  empty" 0 log-it
	    s" after: " log-spot-variables
	THEN	
[THEN]
    THEN ;

: next-spot-do ( -- )   spot @ 1+ spot-do ;
   

\ The lowest dimensions loop does the real work:

\ Do a (lowest dimensions) row invisible:
: do-row-invisible ( -- )
    brew-depth-adjust		\ adjust depth for debugging

    display-switch  dup @ >r  [ spot-display-on invert ] literal r@ and swap !

    dimension-ranges @ 0 ?DO	\ lowest dimension forwards loop
	next-spot-do
    LOOP

    r> display-switch !
    
    brew-depth-reset ;

\ Do a (lowest dimensions) row visible:
: do-row-visible ( -- )
    brew-depth-adjust		\ adjust depth for debugging

    dimension-ranges @ 0 ?DO	\ lowest dimension forwards loop
	next-spot-do
    LOOP

    brew-depth-reset ;

\ Maybe change visibility while doing a row (lowest dimension).
\ Maybe set cursor.
: row-set-visibility ( display-sw visibility coordinate -- disp-sw visibility')
    >r

    CASE \ on visibility

	visible OF
	    visibility-off @ r> <> IF	\ visibility switches off?
		visible
	    ELSE
		dup [ spot-display-on invert ] literal and display-switch !
		invisible
	    THEN
	ENDOF

	maybe-visible OF
	    visibility-on @ r> = IF	\ visibility switches on
		dup display-switch !
		visible
	    ELSE
		maybe-visible
	    THEN
	ENDOF

	\ default: invisible
	invisible
	rdrop
    ENDCASE ;

\ Do a row with visibility (possibly) switching on and off again:
: do-row-maybe-visible ( -- )
    brew-depth-adjust			\ adjust depth for debugging
    (brew-depth-offset) 2 + TO (brew-depth-offset)	\ two more (see below)

    display-switch @
    maybe-visible

    dimension-ranges @ 0 ?DO	( old-display-switch actual-visibility )
	i row-set-visibility
	next-spot-do
    LOOP
    drop
    display-switch !

    brew-depth-reset ;

\ Do a row of a worlds lowest dimension depending it's visibility:
: field-do-row ( visibility-flag -- )
    CASE
	invisible	OF  do-row-invisible	 ENDOF

	(background-skipped) off

	maybe-visible	OF  do-row-maybe-visible ENDOF
	visible		OF  do-row-visible	 ENDOF
    ENDCASE ;

\ Check if visibility changes at this spot of a dimension:
\ (The lowest dimension uses 'row-set-visibility' insted).
\ In the second dimension also set cursor when visibility switches.
\ In the third dimension do background visibility switching.
: maybe-change-visibility ( dimension coordinate visibility -- dim c visblty')
    dup CASE
	invisible OF
	    EXIT
	ENDOF

	\ In the third dimension we might need to switch background visibility
	fourth 2 = IF
	    third  backgound-off @ = IF
		(background-off) ON
	    THEN
	THEN

	maybe-visible OF
	    third cells visibility-on + @  third = IF  \ visibility switches on
		drop
		visible

		\ When visibility switches on in second dimension: set cursor
		third 1 = IF
		    0 0 at-xy
		THEN
	    THEN
	    EXIT
	ENDOF

	visible	OF
	    third cells visibility-off + @  third = IF  \ visibility off
		drop
		invisible
	    ELSE
		\ Second dimension: set cursor
		third 1 = IF
		    0 third at-xy
		THEN
	    THEN
	ENDOF
    ENDCASE ;


\ Loop over the coordinates of one dimension taking care of visibility.
\ Recursive call looping over all lower dimensions.
\ The lowest dimensions loop will actually brew and display.
: this-dimension-loop ( dimension visibility-flag -- )
    over 0= IF  nip field-do-row  EXIT THEN	\ lowest dimension?

    0 swap	 ( dimension coordinate visibility-flag )
    third cells dimension-ranges + @  0 ?DO
	maybe-change-visibility
	third 1-  over 1- 0 max RECURSE
	>r 1+ r>
    LOOP

    drop 2drop ;

\ Entry point to start looping over all spots on the highest used dimension:
: world-loop ( -- )
    (background-off) off

    world-dimensions @

\      \ Does the display fill the screen, or must we clean it before?
\      spot-display-on? IF
\  	####################
\      THEN

    1-				\ start at highest dimension
    spot-display-on? IF		\ need to determine visibility at all?
	maybe-visible
    ELSE
	invisible
    THEN		( start-dimension visibility-flag )
    this-dimension-loop ;

\ Display world without doing nuc and spot actions:

\ Show a spot: show nuc's and maybe background:
: spot-show ( -- )
    (background-off) @ IF 		\ Displaying nucs *only* no background?
	spot @ someone-here? dup IF		\ if there's someone
	    cp!					\ make it the actual one
	    set-colors

	    (background-skipped) dup @ dup IF	\ skip empty spots
		at? >r swap + r> at-xy
		off
	    ELSE 2drop THEN

	    show-me-xt @ EXECUTE		\ and show his face
	ELSE					\ empty spot
	    drop
	    1 (background-skipped) +!		\ count skipped empty spots
	THEN
	EXIT
    THEN

    \ Displaying nucs *and* background:
    spot @ someone-here? dup IF			\ if there's someone
	cp!					\ make it the actual one
	set-colors
	show-me-xt @ EXECUTE			\ and show his face
    ELSE
	drop
	show-background				\ empty, show background
    THEN ;

\ Proceed to next spot and maybe show it:
: ?next-spot-show ( visibility -- )
    spot @ 1+ >spot!
    visible <> IF  EXIT  THEN
    spot-show ;

\ Display a row with visibility possibly switching on and off again:
: display-row-maybe-visible ( -- )
    display-switch @
    maybe-visible

    dimension-ranges @ 0 ?DO	( old-display-switch actual-visibility )
	i row-set-visibility
	dup ?next-spot-show
    LOOP
    drop

    display-switch ! ;

\ Show next spot unconditionally:
: next-spot-show ( -- )   spot @ 1+ spot-show ;

\ Unconditionally display a lowest dimensions row:
: display-row-visible ( -- )
    dimension-ranges @ 0 DO
	next-spot-show
    LOOP ;

\ Different ways of displaying a row depending on visibility:
: field-display-row ( visibility-flag -- )
    CASE
	invisible	OF  dimension-ranges @ spot +!	ENDOF

	(background-skipped) off

	maybe-visible	OF  display-row-maybe-visible	ENDOF
	visible		OF  display-row-visible		ENDOF
    ENDCASE ;

\ When displaying a 3D (or higher) world brew can wait after each plane
\ to let you grasp world structure:
VARIABLE layer-delay		layer-delay off

\ Loop over the range of one dimension recursively calling the lower ones.
\ Do visibility and cursor control and display in the lowest dimensions loop.
: this-dimension-display ( dimension visibility-flag -- )
    over 0= IF  nip field-display-row  EXIT THEN	\ lowest dimension?

    0 swap		( dimension coordinate visibility-flag )
    third cells dimension-ranges + @  0 ?DO
	maybe-change-visibility
	dup invisible = IF
	    drop 2drop unloop EXIT
	THEN
	third 1-  over 1- 0 max  RECURSE
	>r 1+ r>
    LOOP

    layer-delay @ IF
	third 1 = IF
	    last-left  s" Showing each layer " type-other-colour
	    clear-line-to-end
	    layer-delay @ wait-until
	THEN
    THEN

    drop 2drop ;

\ Display the world (without brewing).
: .world ( -- )
    this-world 0= IF  EXIT  THEN		\ check if there's something

    cursor-off
    (background-off) off
    -1 spot !

    world-dimensions @ 1-	\ start at highest dimension
    maybe-visible		( start-dimension visibility-flag)
    this-dimension-display
    set-default-colors
    cursor-visible ;

: world-loop-backwards ( -- )
    cr ." NOT DONE YET!" DADA ;	\ #################

: world-do-random ( -- )
    cr ." NOT DONE YET!" DADA ;	\ #################

DEFER ?init-score-list
DEFER ?elitism-pop-control

\ world-do-direction :
\ 1 forward -1 backward
\ 0 random
\ 2 -2 alternate
: world-do ( -- )
    world-do-direction @ >r	\ preserve, as linear mode might change it
    world-mode? 0= IF
	1 world-do-direction !
    THEN

    step-do-before-xt @ EXECUTE
    ?init-score-list

    feed-world			\ puts food everywhere
    determine-food-share 	\ how much aditional food for living cells
    ?diversify-spots
    ?diversify-globals
    present-initializes-future \ if nothing changes the future, nothing changes
    living off			\ counter
    selected off		\ ditto
    died off			\ ditto
    trial off			\ ditto
    mutations off		\ ditto
    cloned @ newborn !		\ temporally used as storage

    -1 spot !

    world-do-direction @ CASE
	1 OF
	    world-loop
	ENDOF
	2 OF
	    world-loop
	    -2 world-do-direction !
	ENDOF
	-1 OF
	    world-loop-backwards
	ENDOF
	-2 OF
	    world-loop-backwards
	ENDOF
	\ default: 0
	world-do-random
    ENDCASE

\      world-do-direction @ ?dup IF 
\  	0> IF
\  	    [ spot-display-on step-display-on or ] literal
\  	    display-switch @ and IF 0 0 at-xy THEN

\  	    world-loop
\  	ELSE
\  	    0 spots 1- DO
\  		i >spot!		\ set spot actual
\  		spot-at			\ set cursor
\  		?diversify-inhabited
\  		spot-do-xt @ EXECUTE	\ optional actions
\  		fcp @ dup IF		\ hello, somebody here?
\   		    cp!			\ yes, set cell as actual one

\  		    log-mask @ IF \ checking logging (for speed reasons)
\  			spot @ num>string cat-log s" : " cat-log
\  			log-cat-id   s" " log-spot log-it
\  		    THEN

\  		    feed-individual \ feed the food only given to living cells

\  		    cell-do-before-xt @ EXECUTE	\ you can put something in here
\  		    nuc-do-all			\ and wake her up
\  		    cell-do-after-xt @ EXECUTE	\ you can put something in here
\  		ELSE					\ noone here
\  		    drop
\  		    spot-display-on? IF
\  			show-background
\  		    THEN
\  		THEN
\  	   -1 +LOOP
\  	THEN
\  	world-do-direction @ dup
\  	abs 2 = IF negate world-do-direction ! ELSE drop THEN \ alternate
\      ELSE			\ call randomly choosen spots
\  	spots 0 DO
\  	    spots random-ranged
\  	    >spot!			\ set spot actual
\  	    spot-at			\ set cursor		
\  	    ?diversify-inhabited
\  	    spot-do-xt @ EXECUTE	\ optional actions
\  	    fcp @ dup IF		\ hello, somebody here?
\  		cp!			\ yes, set cell as actual one

\  		log-mask @ IF \ checking logging (for speed reasons)
\  		    spot @ num>string cat-log s" : " cat-log
\  		    log-cat-id   s" " log-spot log-it
\  		THEN

\  		feed-individual \ feed the food only given to living cells

\  		cell-do-before-xt @ EXECUTE	\ you can put something in here
\  		nuc-do-all			\ and wake her up
\  		cell-do-after-xt @ EXECUTE	\ you can put something in here
\  	    ELSE					\ noone here
\  		drop
\  		spot-display-on? IF
\  		    show-background
\  		THEN
\  	    THEN
\  	LOOP	    
\      THEN

    spot-display-on? IF  set-default-colors  THEN \ ##########

    food>future

[ future-change-individal 0= ] [IF]	\ copy all quality changes in one move?
    future-quality-change @ IF qualities>future THEN
[THEN]

    cloned @ newborn @ - newborn !

    ?elitism-pop-control

    step-display-on? IF		\ we want a scan display?
	step-display
    THEN

    step-do-after-xt @ EXECUTE

    time-step

    r> world-do-direction ! ;	\ restore as linear-mode-might have changed it
