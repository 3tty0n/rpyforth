\ system-dependent.fs
\ 	$Id: system-dependent.fs,v 1.1.1.1 2009-06-09 18:28:50 anton Exp $	
\ switch system dependent behaviour
\ this comes *before* compile time options. See 'compile-options.fs'.


\ VARIABLE forth-system
\ 1 CONSTANT gforth
\ 2 CONSTANT bigforth

cr .( including system-dependent.fs)

\ symmetric division
s" floored" environment? drop [IF]
    cr .( Using symmetric division 'sm/rem' for '/' )
    : /   >r s>d r> sm/rem nip ;
[ELSE]
\      \ very likely to fail...
\      cr .( Using floored division 'fm/mod' for '/' )
\      : /   >r s>d r> fm/mod nip ;
[THEN]


\ check for different FORTHs:

s" PFE-DEBUG" environment? [IF] \ must be *before* gforth
    drop
    cr .( we're running 'PFE' )
    s" pfe.fs" INCLUDED

[ELSE] s" gforth" environment? [IF] \ pfe seams to do that too...
    cr .( running on: Gforth ) type cr
    s" gforth.fs" INCLUDED

[ELSE] s" bigFORTH" environment? [IF]
    cr .( we're running bigFORTH ) . 8 emit char . emit . cr	\ version nr 
    s" bigFORTH.fs" INCLUDED
    \ Note that there's another modification 'screen-column' in brew-basics.fs

[ELSE] s" IFORTH" ENVIRONMENT? [IF] drop
    cr .( we're running iForth )
    s" iForth.fs" INCLUDED	 	\ Many thanks to Marcel Hendrix

[ELSE]
    cr .( Running a FORTH system, brew was not adapted for. )
    cr .( Please report success or failure.  Thank you. )
    cr

[THEN] [THEN] [THEN] [THEN]
\ FORTH system specific issues done.


\ cell size:
1 cells 4 = [IF]
    base @ hex
    -80000000 CONSTANT lowest-integer#
     7FFFFFFF CONSTANT highest-integer#
    base !
[ELSE]
    1 cells 8 = [IF]
	base @ hex
	-8000000000000000 CONSTANT lowest-integer#
	 7FFFFFFFFFFFFFFF CONSTANT highest-integer#
	base !
    [ELSE]
	7 emit
	cr .( Uncommon Forth cell size ) 1 cells .
	cr .( Please define lowest-integer# and highest-integer# in 'system-dependent.fs')
	cr 5000 ms
    [THEN]
[THEN]


\ check for some definitions:

[UNDEFINED] xt>string [IF]
    7 emit
    cr .( file system-dependent.fs:  Please define   xt>string )
    char ( emit
    .(  xt -- addr length )
    char ) emit
    cr .(                            You send me a copy for your system? )
    8000 ms cr
[THEN]

[UNDEFINED] see [IF]
    7 emit
    cr .( File system-dependent.fs:  Please do define   SEE )
    cr
    cr .( `see'       <spaces>name )
    char ( emit  .(  -- )  char ) emit
    cr .( Locate NAME using the current search order. Display the definition of NAME.)
    cr .( Coming brew versions might not need 'see' any more, this one does. )
    cr
    8000 ms
[THEN]

\ Check if xt>string preserves lower case names:
MARKER forget-it	: lowercase ;
: lowercase? ( -- flag )	\ Don't want to rely on interpreted s"
    ['] lowercase xt>string s" lowercase" compare 0= ;
lowercase?	forget-it
CONSTANT forth-with-lowercase

\ <system> ( addr count -- flag ) Word to call OS shell
\ This word gets (only) used to call the online help
\ and for configure-console.fs
[UNDEFINED] <system> [IF]	\ Will bark when you try to use it...
: <system> ( addr count -- flag )
    2drop
    7 emit
    page cr
    ." Please define a word called <system> ( addr count -- flag )" cr
    ." which takes a string and passes it to the OS shell" cr
    ." and returns an error flag." cr
    cr
    ." On many Forth systems you can just use:" cr
    ." : <system> ( addr count -- flag )  system ;" cr
    cr
    ." Put the definition in the system dependent file for your Forth system."
    cr key drop
    true ;	\ signaling error
[THEN]

[UNDEFINED] REQUIRED [IF]
    cr .( including file 'required.fs'  )
    s" required.fs" INCLUDED
[THEN]

[UNDEFINED] parse-word [IF]	\ Gforth and pfe have it.
    cr .( including file 'parse-word.fs'  )
    s" parse-word.fs" INCLUDED
[THEN]

[undefined] endif [if]
: endif postpone then ; immediate
[then]

CREATE SYSTEM-DEPENDENT.FS		\ mark, as REQUIRED might not work yet
					\ in files depending on this one
