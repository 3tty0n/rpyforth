\ 	$Id: brew-crash-test.fs,v 1.8 2002/04/11 20:21:22 f Exp $	

\ This is 'transit-12-bench.fs' with all the assertions left in.
\ Log mask is not altered to make debugging possible.
\ It is not running as benchmark but as normal brew session.
\ 'bye' is deactivated.

\ The file is basically replaced by the file
\ INPUTS/extensions/debugging/brew-crash-test.fs
\ The new version gets included from here after resetting some xt's.

[UNDEFINED] brew-crash-test [IF]	\ backwards compatibility hack.
    page
    cr .( Please define 'brew-crash-test' before compiling:)
    cr
    cr .( Say CREATE brew-crash-test from command line or in file my-compile-options.fs)
    cr
    bell quit
[THEN]

' noop spot-do-xt !
' noop cell-do-before-xt !
' noop cell-do-after-xt !
' noop step-do-before-xt !
' noop step-do-after-xt !

s" INPUTS/extensions/debugging/brew-crash-test.fs" INCLUDED
