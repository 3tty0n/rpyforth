\ ****************************************************************
\ memory-speed-align.fs
\ ****************************************************************
\ 	$Id: memory-speed-align.fs,v 1.4 2001/10/04 20:25:39 f Exp $	

\ Memory allocation taking care of alignement and
\ pre/after padding requirements.
\ Allocates more than the required memory to have space for that.


\ On many processor you can gain a lot of speed advantage by generously
\ aligning/padding code and/or data.

\ This file works on allocated memory.
\ (see 'dp-speed-align.fs' for FORTH data space 'DP').

\ I don't have much knowledge or experience, just want to try a bit...


\ Usage:
\
\	2VARIABLE memory-pointer
\	1000 allocate-for-speed memory-pointer 2!
\		(here you use the memory).
\	memory-pointer 2@ drop free ( -- error-flag )
\
\ (See usage example and test at the end of the file).

\ ****************************************************************
decimal

VARIABLE memory-alignement	32 memory-alignement !
VARIABLE memory-pre-pad		32 memory-pre-pad !
VARIABLE memory-after-pad	32 memory-after-pad !

\ Align an address upwards to the next address dividable by 'alignement':
\ This works on any positive alignement including zero for benching.
: n-aligned ( address +alignement -- aligned-address )
    dup 0= IF drop EXIT THEN

    2dup mod
    dup IF
	- +
    ELSE 2drop THEN ;

\ Align an address upwards on the value stored in 'memory-alignement':
: aligned-address ( address -- aligned-address )
    memory-alignement @ n-aligned ;

\ Allocate memory including enough space for padding and alignement.
\ Return the addresses of the allocated memory and an aligned pointer
\ into this block.
\ The two addresses are in the order to use 2! on them:
\ 	2VARIABLE memory-pointer
\ 	size aligned-address memory-pointer 2!
\ 	memory-pointer @ ( -- aligned-address )
: allocate-for-speed ( size -- allocation-address base-address )
    memory-pre-pad @ +
    memory-alignement @ +
    memory-after-pad @ +
    allocate
    ABORT" allocate-for-speed: Could not allocate."

    dup aligned-address ;

\ ****************************************************************
false [IF] \ Usage example and test:
    page cr
    cr .( Testing 'memory-speed-align.fs'. )
    
    2VARIABLE memory-pointer
    1000 allocate-for-speed memory-pointer 2!
    memory-pointer @ cr .( Pointer received: ) .
    memory-pointer 2@ drop free
    [IF]
	bell
	cr .( memory-pointer free error! )
    [ELSE]
	cr .( free memory-pointer, done. )
    [THEN]
    cr QUIT
[THEN]
\ ****************************************************************
