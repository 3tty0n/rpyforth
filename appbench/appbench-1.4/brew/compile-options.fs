\  compile-options.fs
\ 	$Id: compile-options.fs,v 1.77 2005/03/30 15:22:03 f Exp $	

\ This file is included early, but after 'system-dependent.fs'

\ You can change this file or make a 'my-compile-options.fs' to change
\ compile time options.  It gets included just after this one.

\ Note that some switches can be compile time switches *and* run time switches.
\ Compile swich policy:
\ * CREATEd names are switching by their existence.
\   Be careful with names that could be predefined in a system...
\ * CONSTANTs are pure compile time switches too.
\ * a VALUE can be used instead, in cases where it might be reset.
\ * If a VARIABLE is defined it gets compiled as run time switch.
\   It's value gets the default run time value.
\   (There are exceptions, see 'ekey-cursor-support')
\ * Words that do something can be defined system dependent,
\   if the source checks with [UNDEFINED]
\   Tell me, if other words should be checked for.  epprecht@solnet.ch


\ NUC AND SPOT STRUCTURE:

\ Integer nuc variables:
7 VALUE nuc-organs#	\ # of read/write, diversified integer nuc parameters.
0 VALUE nuc-parameters#	\ # of read only, diversified integer nuc parameters.
0 VALUE nuc-invisibles#	\ # of invisible, diversified integer nuc parameters.
0 VALUE nuc-secrets#	\ # of invisible, not diversified integer nuc constants

\ dfloat nuc variables:
5 VALUE nuc-f-organs#
3 VALUE nuc-f-parameters#
1 VALUE nuc-f-invisibles#
1 VALUE nuc-f-secrets#

\ Integer spot variables:
3 VALUE spot-qualities#		\ # of different integer read/write qualities
3 VALUE spot-properties#	\ # of read only integer spot properties
0 VALUE spot-secrets#		\ # of hidden integer spot variables

\ dfloat spot variables:
5 VALUE spot-f-qualities#
5 VALUE spot-f-properties#
1 VALUE spot-f-secrets#

\ Global variables:
9 VALUE global-integer-variables#
9 VALUE global-dfloat-variables#


\ BASIC DESIGN SWITCHES:	\ different basic designs of some brew parts

\ This compile switch is just for now, it will probably disappear again.
TRUE CONSTANT alternative-nuc-vars	\ using 'BASE+OFFSET:'

\ Spot data (pointers, food, qualities,...) can be organized as
\ localized data records (for cache consistency) or as arrays.
\ Try out which is faster on your machine.
FALSE CONSTANT localise-spot-data

\ ALIGNEMENT:
0 VALUE spot-alignement#	\ does *not* speed up things here.


\ CODE AND DATA ALIGNEMENT:
\ (see 'dp-speed-align.fs' and 'memory-speed-align.fs').

\ experimental:
\ block-VARIABLE:  Putting important data together for cache consistency.
\ Allows padding and alignement for speed.
\
\ Do use block variables:
\ FALSE CONSTANT dummy-block-variables
\
\ Do use normal variables defined when registered for the blocks:
1 CONSTANT dummy-block-variables
\
\ Do use normal variables defined when commented out in the code
\ with '\VARIABLE' '\2VARIABLE' '\FVARIABLE'
\ 2 CONSTANT dummy-block-variables


\ BREW:

\ normally run-mode defaults to zero.
\ sometimes it can make sense to change that.
\ ( give run-mode as a number ) CONSTANT preset-run-mode
\ 1 CONSTANT preset-run-mode	\ record

\ if 'future-quality-change' is on, cells can change future qualities
\ this can happen individually or after a step from 'world-do'
\ TRUE CONSTANT future-change-individal
FALSE CONSTANT future-change-individal

\ maximal number of scans displayed in step display.
\ (on a bigger screen it could make sense to increase that).
\ 12 CONSTANT max-step-scans#

\ Include pointer plane in spot scans?
\ 0 VALUE p0		\ watch memory allocation ;-)
1 VALUE p0		\ normal usage


\ KEYS:

\ VARIABLE use-ekey
\ If 'use-ekey' is defined at compile time it gets compiled as run time switch
\ Define it in the forth system specific files.

\ scope of ekey and ekey?   Both depend on 'use-ekey', see below.
\ I use a variable here, because it might get reset.
[UNDEFINED] ekey-cursor-support [IF]
    VARIABLE ekey-cursor-support	ekey-cursor-support off
[THEN]
[UNDEFINED] ekey-function-keys-support [IF]
    VARIABLE ekey-function-keys-support	ekey-function-keys-support off
[THEN]
\ some switches depend on 'use-ekey':
[DEFINED] use-ekey [IF]
    use-ekey @ 0= [IF]
	ekey-cursor-support off
	ekey-function-keys-support off
    [THEN]
[ELSE]
    ekey-cursor-support off
    ekey-function-keys-support off
[THEN]


\ MOUSE:

\ Switch for real mouse support or only cursor keys and <return>?
\ The switch is here, but no mouse support yet.  Help appreciated.
FALSE CONSTANT mouse-supported


\ DISPLAY:
1 5 2CONSTANT default-horizontal-zoom-scale
1 4 2CONSTANT default-vertical-zoom-scale


\ CONSOLE:

\ There are systems where writing to the very last screen position does
\ automatic scrolling.  Set 'lower-right-scrolls' to true in that case.
[UNDEFINED] lower-right-scrolls [IF]  false CONSTANT lower-right-scrolls [THEN]

\ Linux console can use color code 9 and gets a default color.
\ This works on both, background and foreground, giving two different colors.
\ Marcel Hendrix reportet that this is not portable. 
\ On systems where this mechanism does not work do the following:
\ * Define 'default-background' and 'default-foreground'
\   I would take black and white.
\ * Define 'default-color' 'color-foreground' and 'color-background'
\   in a system dependent way, such that they will do The Right Thing.
\   I'd be interested to get a mail with your adaptions epprecht@solnet.ch

\ COLORS:

\ for systems where colors don't work:
\ CREATE never-use-colors

\ if colors don't start at zero (but have continuous range)
\ ( lowest-color-code ) CONSTANT color-offset

\ 'title-colors-xt' for menu title colors. See 'brew.fs'


\ FILES:
\ basics.fs
decimal 256  CONSTANT file-names-length#	\ defaults to &256
decimal 4048 CONSTANT file-line-max#		\ I/O line buffer size
TRUE CONSTANT flush-files			\ flush files after writing?

\ brew will put all it's output relative to out-dir, if the user does not
\ select another directory. Make the string end with a directory separator.
\ : out-dir ( -- addr count )    s" OUTPUT/" ;	\ see brew-basics.fs

[UNDEFINED] use-fileselect [IF] \ work in progress
    false CONSTANT use-fileselect
[THEN]


\ COMPILATION FEEDBACK:

\ Should some words automatically generating a word family display the
\ names them when compiling?  Currently used by:
\ * compile-listed-?-and-!
\ * compile-listed-?-!-and-??
false VALUE display-compiled-words


\ MISC:

: editor ( -- addr count )   s" emacs" ;


\ DEBUG:

\ If 'log-mask' is true at compile time there will be more places
\ where it is possible to log the inner working of mutation.
\ It will still be run time switchable, but brew will run a bit slower.
\ Setting it to a positive value will log some extra information.
\ This makes only sense for heavy debugging and is only occasionally supported.
VARIABLE log-mask	log-mask ON	\ compile time *and* run time switch

log-mask ON		\ possibility to log more mutation infos. a bit slower
\ log-mask OFF		\ possibility to log usual amount of infos	fastest
\ 1 log-mask !		\ extra info.				  could be slow
\ log-mask get's reset after compiling.

VARIABLE debugging	debugging ON	\ 'debug' as name is taken


\ CURRENT DEFAULTS:

\ Fake conservative settings:
FALSE [IF] \ with these the old demos and benchmarks should still run.
    7 TO nuc-organs#
    0 TO nuc-parameters#
    3 TO spot-qualities#
    0 TO spot-properties#
    1 TO p0
[THEN]

FALSE [IF] \ current guess-A experiment
    3 TO nuc-organs#
    1 TO spot-properties#
    0 TO nuc-parameters#
[THEN]

FALSE [IF] \ transit-13
    7 TO nuc-organs#
    3 TO nuc-parameters#
    3 TO nuc-invisibles#
    1 TO nuc-secrets#

    3 TO spot-qualities#
    3 TO spot-properties#
    1 TO spot-secrets#
[THEN]

TRUE [IF] \ transit-14 to transit_17
    7 TO nuc-organs#
    5 TO nuc-parameters#
    3 TO nuc-invisibles#
    1 TO nuc-secrets#

    5 TO spot-qualities#
    5 TO spot-properties#
    1 TO spot-secrets#
[THEN]

true [IF]			\ compile nuc float variables
    5 TO nuc-f-organs#
    3 TO nuc-f-parameters#
    1 TO nuc-f-invisibles#
    1 TO nuc-f-secrets#
[ELSE]				\ no nuc float variables
    0 TO nuc-f-organs#
    0 TO nuc-f-parameters#
    0 TO nuc-f-invisibles#
    0 TO nuc-f-secrets#
[THEN]

true [IF]			\ compile spot float variables
    5 TO spot-f-qualities#
    5 TO spot-f-properties#
    1 TO spot-f-secrets#
[ELSE]				\ no spot float variables
    0 TO spot-f-qualities#
    0 TO spot-f-properties#
    0 TO spot-f-secrets#
[THEN]
