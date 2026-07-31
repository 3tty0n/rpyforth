\ checksums.fs
\ 	$Id: checksums.fs,v 1.2 2002/04/17 21:12:05 f Exp $	

\ Tool to write binary checksum files and to compare them to the checksums
\ produced later under changed circumstances.

\ Find step where something differs, write spot checksum file for this step,
\ check it determining the critical spot and do write logs for it.

\ A step checksums file is produced by saying
\ step-check-writing-on
\ After that brewing (i.e. from an included session file) will write checksums.
\ step-check-file-name determines the file name.

\ See crash-test-README for further usage.


\ Works together with INPUTS/extensions/debugging/brew-crash-test.fs
\ see INPUTS/extensions/debugging/crash-test-README
[DEFINED] brew-crash-test [IF]	\ interface to special usage case
: step-check-file-name ( -- addr count )
    s" INPUTS/extensions/debugging/crash-test-step.dat" ;
[THEN]


\ Build a checksum over all spots, nucs and some variables:
: step-checksum ( -- n )
    world-checksum
    nucs-checksum +

    which-random-seed CASE
	1 OF EXECUTE @ + ENDOF
	2 OF EXECUTE 2@ + + ENDOF
    ENDCASE

    step @ +
    cloned @ +
    living @ +
    nuc-do-cost @ +
    code-price @ +
    (mutated-max) @ +
    compiled-genes @ +
    world-do-direction @ +
    additive-stress @ +
    stress-rate 2@ + +
    code-additive-stress @ +
    code-stress-rate 2@ + +
    food-share/spot @ +
    individual-fixed-food-share @ + ;

VARIABLE (step-checksum-file-id)	(step-checksum-file-id) off

[UNDEFINED] step-check-file-name [IF] \ optional compile option
: step-check-file-name ( -- addr count )
    [ tmp-dir s" STEP-CHECKSUMS.dat" file-name-cat  dup string@ ] sliteral
    [ stringbuf-close ] ;
[THEN]

\ Word to hook into step-do-before-xt and/or step-do-after-xt to write the
\ checksum to the checksum file:
: write-step-checksum ( -- )
    (step-checksum-file-id) @ 0= IF
	step-check-file-name w/o create-named-file  (step-checksum-file-id) !
    THEN

    step-checksum pad !  pad cell (step-checksum-file-id) @ write-file
    ABORT" write-step-checksum: Could not write-file"

    [ flush-files ] [IF]
	(step-checksum-file-id) @ flush-file drop
    [THEN] ;

[UNDEFINED] brew-crash-test [IF] \ don't override file crash-step-checksums
    ' write-step-checksum step-do-actions >list
[THEN]

VARIABLE (step-fail-id)		(step-fail-id) off

[UNDEFINED] step-fail-file-name [IF] \ optional compile option
: step-fail-file-name ( -- addr count )
    [ tmp-dir  s" STEP-FAIL.fs"  file-name-cat  dup string@ ] sliteral
    [ stringbuf-close ] ;
[THEN]

VARIABLE step-to-check		-99 step-to-check !	\ impossible value

\ Write current step to file (step-fail-id):
: write-step-fail-file ( -- )
    [ decimal ] 32 stringbuf-open >r
    step @ num>string	r@ cat
    s"  step-to-check !"	r@ cat
    r@ string@ (step-fail-id) @ WRITE-LINE
    ABORT" write-step-fail-file: Could not write-line."
    r> stringbuf-close
    (step-fail-id) @ flush-file
    ABORT" write-step-fail-file: Could not flush-file." ;

\ Word to hook into step-do-before-xt and/or step-do-after-xt to read
\ checksums from a checksum file and continuously check it against the
\ current brew state:
: check-step-sums ( -- )
    (step-checksum-file-id) @ 0= IF
	step-check-file-name r/o open-named-file
	(step-checksum-file-id) !
    THEN

    step-checksum
    pad cell (step-checksum-file-id) @ READ-FILE
    ABORT" check-step-sums: Could not READ-FILE"
    cell <> IF
	drop
	bell
	last-left s" Checksum file ended.  Step " type-alert  step @ .
	clear-line-to-end
	wait
	single-step on
	EXIT
    THEN
    pad @ = IF EXIT THEN

    bell
    last-left s" CHECKSUM DIFFERENCE IN STEP " type-alert  step @ .
    clear-line-to-end
    write-step-fail-file

    key drop

    ['] noop step-do-before-xt !
    ['] noop step-do-after-xt !
    single-step on ;

' check-step-sums step-do-actions >list

\ Switch writing of data to a step checksum file on.  Start brewing after this.
: step-check-writing-on ( -- )
    ['] write-step-checksum step-do-before-xt !
    ['] write-step-checksum step-do-after-xt ! ;

\ Switch comparing of step check data on (used before brewing).
: step-check-on ( -- )
    step-fail-file-name w/o open-named-file  (step-fail-id) !
    ['] check-step-sums step-do-before-xt !
    ['] check-step-sums step-do-after-xt ! ;


\ Build a spot checksum, including nuc data on inhabited spots:
: spot-checksum ( -- n )
    spot @

    \ Loop over all integer spot variables starting with index 1 (food)
    spot-qualities# spot-properties# + spot-secrets# + 1+  1 DO
	i n'th-spot-variable @ +
    LOOP

    \ Add dfloats as two integers:
    spot-floats# 0 ?DO
	i n'th-spot-f-variable 2@ + +
    LOOP

    inhabited? dup IF cp! +nuc-checksum ELSE drop THEN ;

VARIABLE (spot-checksum-file-id)	(spot-checksum-file-id) off

[UNDEFINED] spot-check-file-name [IF] \ optional compile option
: spot-check-file-name ( -- addr count )
    [ tmp-dir s" SPOT-CHECKSUMS.dat" file-name-cat  dup string@ ] sliteral
    [ stringbuf-close ] ;
[THEN]

\ Word to hook into spot-do-xt to write the spot checksum to the checksum file:
: write-spot-checksum ( -- )
    (spot-checksum-file-id) @ 0= IF
	spot-check-file-name w/o create-named-file  (spot-checksum-file-id) !
    THEN

    spot-checksum pad !  pad cell (spot-checksum-file-id) @ write-file
    ABORT" write-spot-checksum: Could not write-file"

    [ flush-files ] [IF]
	(spot-checksum-file-id) @ flush-file drop
    [THEN] ;

' write-spot-checksum spot-do-actions >list

\ Create and write a spot checksum file on  step-to-check
\ This word is designed to be hooked into  spot-do-xt
: ?write-spot-check ( -- )
    step @ step-to-check @ <> IF  EXIT  THEN	\ wait for the right step

    step-to-check @ -99 = IF			\ avoid troubles...
	bell cr
	." step-to-check was not set before!" cr
	." spot-do-xt cleared." cr
	['] noop spot-do-xt !
	wait EXIT
    THEN

    write-spot-checksum ;	\ write spot data   (maybe create file before)

' ?write-spot-check spot-do-actions >list

\ Switch spot check file writing on:
: write-spot-check-on ( -- )
    step-fail-file-name ['] INCLUDED
    CATCH IF 2drop bell cr
	." write-spot-check-on: File "
	step-fail-file-name type ."  not found!" cr
	wait EXIT
    THEN
    spot-check-file-name w/o create-named-file  (spot-checksum-file-id) !
    ['] ?write-spot-check spot-do-xt ! ;


VARIABLE (spot-fail-id)		(spot-fail-id) off

[UNDEFINED] spot-fail-file-name [IF] \ optional compile option
: spot-fail-file-name ( -- addr count )
    [ tmp-dir  s" SPOT-FAIL.fs"  file-name-cat  dup string@ ] sliteral
    [ stringbuf-close ] ;
[THEN]

VARIABLE spot-to-check		-99 spot-to-check !	\ impossible value

\ Create a file tmp/SPOT-FAIL.fs and write number of the spot in question to it
: spot-to-spot-fail ( -- )
    [ decimal ] 64 stringbuf-open >r
    step @ num>string		r@ cat
    s"  step-to-check !  "	r@ cat
    spot @ num>string		r@ cat
    s"  spot-to-check !"	r@ cat
    r@ string@ (spot-fail-id) @ WRITE-LINE
    ABORT" spot-to-spot-fail: Could not write-line."
    r> stringbuf-close
    (spot-fail-id) @ flush-file
    ABORT" spot-to-spot-fail: Could not flush-file." ;

\ Word to hook into spot-do-xt to read spot checksums from a checksum file
\ and continuously check it against the current brew state on all steps:
: check-spot-sums ( -- )
    (spot-checksum-file-id) @ 0= IF
	spot-check-file-name r/o open-named-file
	(spot-checksum-file-id) !
    THEN

    spot-checksum
    pad cell (spot-checksum-file-id) @ READ-FILE
    ABORT" check-spot-sums: Could not READ-FILE"
    cell <> IF
	drop
	bell
	last-left s" Checksum file ended.  Spot " type-alert  spot @
	clear-line-to-end
	wait
	single-step on
	EXIT
    THEN
    pad @ = IF EXIT THEN

    bell
    last-left s" CHECKSUM DIFFERENCE AT SPOT " type-alert  spot @ .
    s" STEP " type-alert step @ .
    inhabited? IF  ." (inhabited)"  ELSE  ." (empty)"  THEN
    clear-line-to-end
    spot-to-spot-fail

    key drop
    single-step on ;

' check-spot-sums spot-do-actions >list

\ Switch spot check on (on all steps).
: spot-check-on ( -- )
    spot-fail-file-name w/o create-named-file  (spot-fail-id) !
    ['] check-spot-sums spot-do-xt ! ;

\ Word to hook into spot-do-xt that waits for the critical step to read spot
\ checksums from a checksum file and check them against the current brew state:
: spot-check-on-critical-step ( -- )
    step @ step-to-check @
    2dup dup 2 + within 0= IF  2drop EXIT  THEN 

    = IF  check-spot-sums EXIT  THEN

    \ step after the critical one
    cr ." SPOT CHECK DONE        "
    cr wait ;

\ Determing the critical spot at the critical step:
: find-critical-spot-in-step ( -- )
    step-fail-file-name  ['] INCLUDED
    CATCH IF
	bell cr
	." find-critical-spot-in-step: "
	step-fail-file-name type ."   not found." cr
	2drop
	wait
	EXIT
    THEN
    
    spot-fail-file-name w/o create-named-file  (spot-fail-id) !
    ['] spot-check-on-critical-step spot-do-xt ! ;

\ Word to hook into  spot-do-xt  to switch logging of the critical spot
\ in the step before and at the critical step:
: log-critical-spot ( -- )
    step @
    dup step-to-check @  1-  dup 3 +  within 0= IF  drop EXIT  THEN
    step-to-check @ 1+ = IF  log-mask off  EXIT  THEN

    spot @  spot-to-check @ 1- = IF  log-mask on  ELSE log-mask off  THEN ;

' log-critical-spot spot-do-actions >list

\ Switch logging for the critical spot on:
: log-critical-spot-on ( -- )
    spot-fail-file-name  ['] INCLUDED
    CATCH IF
	bell cr
	." log-critical-spot-on: " spot-fail-file-name type ."   not found." cr
	2drop
	wait
    ELSE
	['] log-critical-spot spot-do-xt !
    THEN ;
