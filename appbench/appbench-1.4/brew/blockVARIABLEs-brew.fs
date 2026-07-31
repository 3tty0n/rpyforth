\ blockVARIABLEs-brew.fs
\ 	$Id: blockVARIABLEs-brew.fs,v 1.2 2001/09/14 20:54:00 f Exp $	

\ EXPERIMENTAL

\ block-VARIABLE:  Putting important data together for cache consistency.
\ Allows padding and alignement for speed.

\ Redefining just about all variables from 'brew.fs' depending on
\ compile option 'dummy-block-variables' (see below).
\ State as in 'brew-transit_22'.  This will be redone.
\ 'brew-transit_22' is only to do get benchmark results.


\ Tristate compile time switch 'dummy-block-variables':

\ Do use block variables:
\ FALSE CONSTANT dummy-block-variables
\
\ Do use normal variables defined when registered for the blocks:
\ 1 CONSTANT dummy-block-variables
\
\ Do use normal variables defined when commented out in the code
\ with '\VARIABLE' '\2VARIABLE' '\FVARIABLE' (Fvars unused).
\ 2 CONSTANT dummy-block-variables


init-var-block

define-block-variables
