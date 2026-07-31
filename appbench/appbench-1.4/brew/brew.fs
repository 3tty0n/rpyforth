\ ****************************************************************
\ brew.fs		started: in the beginning of march 2000
\ ****************************************************************

\ Word to avoid cvs id's from being expanded in other files:
: cvs" ( "CVS ID" -- addr count )
    [char] " parse swap 6 + swap 12 - 2 max POSTPONE sliteral ; IMMEDIATE

: brew-version ( -- addr count )
    cvs" 	$Id: brew.fs,v 1.429 2005/06/01 07:45:32 f Exp $	" ;

\ ****************************************************************
page
cr .( ==> 'Cells' alias 'brew' <== )
cr .( 'The Evolutionary Programmers Playground' )
cr

\ ****************************************************************
\ LICENSE:

\ 'brew' an experiment with evolutionary programming written in Forth.
\ Copyright (C) 2001, 2002 by Robert Epprecht <epprecht@solnet.ch>

\ This program is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation; either version 2
\ of the License, or (at your option) any later version.
\ 
\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.
\ 
\ You should have received a copy of the GNU General Public License along
\ with this program; if not, write to the Free Software Foundation, Inc.,
\ 59 Temple Place, Suite 330, Boston, MA 02111-1307, USA.


\ ****************************************************************
\ This is for word usage tests only:  EXPERIMENTAL
\ If 'COUNTING-WORDS' is defined (from command line) words will get
\ defined in a self counting way, to get a word usage profile.
\ (Helps me to optimise speed).
\ System menu has entries to use this feature.
\ 'goodbye' writes the file automatically.
: found? ( "name<bl>" -- flag )   bl word find nip ; 
found? COUNTING-WORDS [IF] \ Check if 'COUNTING-WORDS' is defined
    INCLUDE word-usage.fs
[THEN]


\ ****************************************************************
\ Memory alignement and padding:
\ I put this first so you can change settings system dependent or as options.
s" memory-speed-align.fs" INCLUDED

\ Aligning and padding FORTH data space.
\ s" dp-speed-align.fs" INCLUDED

\ System dependent stuff comes *before* compile time options.
s" system-dependent.fs" INCLUDED
s" common-words.fs" INCLUDED

\ Check for some environmental dependencies:
\ *  'Brew' depends on allocated memory to be dfaligned.
\ *  'Brew' depends on a separate float stack.
s" environmental.test.fs" INCLUDED

\ Compile time options:
s" compile-options.fs"  ' INCLUDED  CATCH
?dup [IF]
    7 emit
    -38 = [IF]
	7 emit
	cr .( compile-options.fs not present. ) cr 2000 ms
    [ELSE]
	cr .( Error loading compile-options.fs )
	cr
	cr .( <press a key> ) cr key drop throw
    [THEN]
[THEN]

\ Avoid 'changing' a genome to an identical new one over and over again.
\ New feature, can be switched off for compatibility
\ (I don't see another use of doing so.)
[UNDEFINED] mutation-must-differ [IF]
    VARIABLE mutation-must-differ	mutation-must-differ on
[THEN]


nuc-f-organs# nuc-f-parameters# + nuc-f-invisibles# + nuc-f-secrets# +
CONSTANT nuc-floats#

spot-f-qualities# spot-f-properties# + spot-f-secrets# + CONSTANT spot-floats#

localise-spot-data 0<>				\ no localised spot floats
spot-floats# and [IF] \ spot floats not (yet) supported
    bell
    FALSE CONSTANT spot-f-qualities#
    FALSE CONSTANT spot-f-properties#
    FALSE CONSTANT spot-f-secrets#
    FALSE CONSTANT spot-floats#
[THEN]

\ Private options
s" my-compile-options.fs"  ' INCLUDED  CATCH
?dup [IF]
    -38 <> [IF]
	7 emit
	cr .( Error loading my-compile-options.fs )
	cr .( <press a key> ) cr key drop throw
    [THEN]
[THEN]

\ Compatibility with old benchmarks (EXPERIMENTAL)
[DEFINED] transit-11-bench-A
[DEFINED] transit-12-bench OR
[DEFINED] brew-crash-test  OR
[DEFINED] startup-bench    OR
[UNDEFINED] old-bench-compatible-mode? [IF]	\ normally defined here
    CONSTANT old-bench-compatible-mode?		\ *compile time* switch
[ELSE] drop [THEN]

old-bench-compatible-mode? [IF]
    mutation-must-differ off
[THEN]


VOCABULARY brew-words
also brew-words

s" basics.fs" INCLUDED		\ don't use 'INCLUDE basics.fs' here, see below

\ Redefine INCLUDE using INCLUDED
\ There where some difficulties with REQUIRE not working on some Forth versions
\ I want to encapsulate in the ANS word INCLUDED to simplify remedie.
\ I prefer the syntax of INCLUDE over INCLUDED though, that's why I redefine:
: INCLUDE   get-word INCLUDED ;

INCLUDE lists.fs
INCLUDE key-mapping.fs

\ simple key buffering:
[UNDEFINED] push-key [IF]	INCLUDE keybuf.fs 			[THEN]

INCLUDE association-lists.fs
INCLUDE sorted-lists.fs

[UNDEFINED] at? [IF]		INCLUDE console-codes.fs 		[THEN]
\ get cursor position, colors.		\ '[UNDEFINED] at?' could go wrong...
[UNDEFINED] cursor-visible [IF]	INCLUDE console-codes.fs 		[THEN]
[UNDEFINED] black [IF]		INCLUDE console-codes.fs 		[THEN]

[UNDEFINED] color-list [IF]
    LIST: color-list
    ' black color-list >list
    ' red color-list >list
    ' green color-list >list
    ' brown color-list >list
    ' blue color-list >list
    ' magenta color-list >list
    ' cyan color-list >list
    ' white color-list >list
    ' default-color color-list >list
[THEN]

\ screen size configuration
old-bench-compatible-mode? 0= [IF] \ EXPERIMENTAL condition

    s" my-console.conf.fs" file-exists? [IF] \ create with configure-console.fs
	cr .( Loading console screen size configuration my-console.conf.fs ) cr
	s" my-console.conf.fs"  ' INCLUDED  CATCH
	?dup [IF]
	    -38 <> [IF]
		7 emit
		cr .( Error loading my-console.conf.fs )
		cr .( <press a key> ) cr key drop throw 
	    [THEN]
	[THEN]
    [THEN]

[THEN]
\ include anyway because of file dependencies (see menu.fs)
INCLUDE screen-size.fs		\ conservative size defaults for undefined
				\ stuff (make sure everything is defined now)

INCLUDE allocation-pointers.fs
INCLUDE stringbuf-0.4.fs 	\ allocated string buffers, 'cat' strings

INCLUDE simple-stringbuf.fs


\ experimental:
\ INCLUDE block-variables.fs
\ define essential variables as block-variables
\ INCLUDE blockVARIABLEs-brew.fs

[UNDEFINED] (scratch-buf) [IF]
    decimal 128 STRINGBUF-HANDLE: (scratch-buf)
[THEN]

INCLUDE listed-masks.fs

INCLUDE display.fs

INCLUDE user-IO.fs

\ doing some very basic definitions first makes life easier...
INCLUDE brew-basics.fs

INCLUDE manual.fs 		\ context sensitive help

INCLUDE debuging.fs 		\ debugging help

brew-words definitions
INCLUDE reporting.fs 		\ writing logs to the hd

also FORTH definitions previous
INCLUDE mouse.fs 		\ faked mouse interface ##########
INCLUDE menu.fs 		\ user interaction interface
definitions

INCLUDE random.fs 		\ random functions
INCLUDE probability-lists.fs

\ INCLUDE string-lists.fs 	\ lists of string handles
\ INCLUDE sorted-string-lists.fs 
\ INCLUDE string-replace.fs 	\ string tables and string translation tables

\ Number of bit masks that mutation sets in the nuc based on masks in
\ internals genes data.  Used (in this brew version) for diversification
\ bit masks.
nuc-floats# [IF] 4 [ELSE] 1 [THEN]  CONSTANT nuc-bitmasks#

INCLUDE genes-0.3.fs 		\ mutation-0.3.fs later on.

INCLUDE statistics.fs

decimal

\ Quitting brew and Forth:
\ For additional actions like writing brew profile before leaving brew:
DEFER <goodbye-actions>		' noop IS <goodbye-actions>

: goodbye ( -- )	\ Does 'reset-colours' now, thanks Aljoscha :-)
    reset-colours
    <goodbye-actions>
    playing-bench? 0= IF
	created-files @ IF page ELSE cr clear-line-to-end THEN
	cr brew-version type ."     Identity: " (identity) @ .
	clear-line-to-end
	?remove-tmp-files
	?list-created-files
	cr  clear-line-to-end
    THEN
    0 at-x
    cursor-visible
    bye ;

: bye ( -- )   goodbye ;

: |goodbye| ( -- )
    last-left clear-line-to-end
    last-left ." Quit brew y/n? "
    key CASE
	[char] y OF goodbye ENDOF	\ EXITS brew
	[char] Y OF goodbye ENDOF	\ EXITS brew
    ENDCASE
    last-left clear-line-to-end ;


\ And now the real stuff:

\ ****************************************************************
\ *****************  Defining nuc structure:  ********************
\ ****************************************************************

0 VALUE cp			\ values are faster than variables
\ VARIABLE cp		cp off	\ 'cell pointer' points to the actual cell
\ The 'cell pointer' variable cp is very basic:
\ it's value is the base address of the actual nucleus.
\ That means, that a whole group of operations doing something with a cell
\ will work on the one cp@ points to.
\ It makes the nuc pointed to to the center of attention for all nuc based
\ operations.
\ cp is never accessed directly. always use cp@ and cp!.

nuc-floats# [IF]
    \ Define pointers to the start of the nuc float arrays:
    \ These pointers are set by cp! based on the offset values in the nuc.
    \ Prepared for nucs of varying length.
    nuc-f-organs#	[IF]	0 VALUE nuc-f-organ-base	[THEN]
    nuc-f-parameters#	[IF]	0 VALUE nuc-f-parameter-base	[THEN]
    nuc-f-invisibles#	[IF]	0 VALUE nuc-f-invisible-base	[THEN]
    nuc-f-secrets#	[IF]	0 VALUE nuc-f-secret-base	[THEN]
[THEN]

\ Nuc variables:
\ As we need the names i put the xt's in a list at compile time.
LIST: nuc-var-xts

\ Slow, but universal words for display and such:
: nuc-var-xt ( index -- xt )  nuc-var-xts n'th-node @ ;
: nuc-var-name ( index -- addr count )   nuc-var-xt xt>string ;
\ : nuc-X-var-addr ( index -- a )  nuc-var-xt EXECUTE ;


alternative-nuc-vars [IF]

: NUC-VAR: ( "name"  n -- n+1 )
    get-name >r
    cells
    ['] cp swap  BASE+OFFSET:  nip
    cell /
    r@ string@ get-xt nuc-var-xts >list	\ xt to list
    r> stringbuf-close ;

[ELSE]

: NUC-VAR: ( n -- n+1 )		\ creates a named variable in the nuc
    get-name >r			( "name" n  r: handle-of-name-buffer ) 
    CREATE dup cells , 1+
    r@ string@ get-xt nuc-var-xts >list	\ xt to list
    r> stringbuf-close
    DOES> @ cp@ + ;

[THEN]

\ ************************  NUC STRUCTURE  **************************
\  Define named variables: 'define-nuc-organs' 'define-nuc-parameters'
\  action pointers first: (see source)
\  genes	\ there is a double cell for each gene: gene-xt internals-xt
\		(with the old genes 'internals-xt' is not there)
\  CONSTANT nuc-genes-limit	nuc indizes
\  CONSTANT nuc-xt's
\  xt's
\  CONSTANT nuc-xt-limit
\  CONSTANT nuc-variables
\  nuc intern data
\  CONSTANT nuc-diversificable-area
\  CONSTANT nuc-organs       			\ index of first organ
\  named organs configurable by VALUE nuc-organs#
\  the cells can read and write them
\  CONSTANT nuc-parameters			\ base index
\  named parameters configurable by VALUE nuc-parameters#
\  the cells can read but not change them
\  [ compile option ] VALUE nuc-invisibles#	\ invisible, diversified
\  ( # ) CONSTANT nuc-diversificable-items
\  [ compile option ] VALUE nuc-secrets#	\ invisible, not diversified
\  ( # ) CONSTANT nuc-scan-limit
\  ***************** End of integer nuc vars *********************
\  dfaligned
\ ********** Start of the FLOATING POINT NUC AREA. ***************
\  ( index ) CONSTANT nuc-float-start-index
\  ( ... ) dfaligned CONSTANT nuc-float-offset#
\  ( index ) CONSTANT nuc-f-organs-i
\  nuc-f-organs#
\  ( index ) CONSTANT nuc-f-parameters-i
\  nuc-f-parameters#
\  ( index ) CONSTANT nuc-f-invisibles-i
\  nuc-f-invisibles#
\  ( ... ) CONSTANT nuc-f-diversificable-items  UNUSED
\  ( index ) CONSTANT nuc-f-secrets-i
\  ( index ) CONSTANT nuc-df-scan-limit
\  nuc-float-offset# nuc-floats# dfloats + CONSTANT nuc-length#
\  *******************  End of nuc structure  *********************


\ Build nuc structure (as shown above):
\ ******************** Start of  NUC STRUCTURE  ***********************
0				\ counter, must be on stack at compile time

\ action pointers first		\ each one containing an xt
\ genes that can be mutated:
\ two cells each: gene-xt internal-xt
\
\ 'wake-me-xt' holds a gene.  It's the genome of the cell.  It get's mutated.
NUC-VAR: wake-me-xt		\ main entry point: do a life action. genome
NUC-VAR: wake-me-internal	\ internals xt of the genome

dup CONSTANT nuc-genes-limit	\ index limit

\ these hold xt's of actions that can't be mutated:
dup CONSTANT nuc-xt's
NUC-VAR: eat-xt			\ the task, the genome should solve
NUC-VAR: reproduce-xt		\ how to reproduce
NUC-VAR: show-me-xt		\ how to show up in world view (spot display)
dup CONSTANT nuc-xt-limit	\ index limit

\ some variables
dup CONSTANT nuc-variables	\ base index of cell sized nuc variables
\ First the variables excluded from '+nuc-checksum'
\ Allow different nuc structure and id's, without changing checksum:
NUC-VAR: id			\ big brother is watching
NUC-VAR: genome-id		\ big brother is watching
nuc-floats# [IF]
    NUC-VAR: f-organ-offset		\ offsets set at nuc creation time
    NUC-VAR: f-parameter-offset
    NUC-VAR: f-invisible-offset
    NUC-VAR: f-secret-offset
[THEN]
\ must be last in this block	See 'save-nuc'.
NUC-VAR: length			\ holds length of whole nucleus in bytes

dup CONSTANT nuc-checksum-start
NUC-VAR: nuc-supplements	\ how many cells data follow the nuc
NUC-VAR: nuc-flags
0
MASK: nuc-on-trial		\ wake me action is freshly mutated, on trial
				\ must be first, see  trial>color
MASK: nuc-is-word		\ nucleus is in dictionary
				\ must be cleared when cloning a nuc.
MASK: nuc-is-selected		\ to select a number of nucs
\ MASK: nuc-is-marked		\ temporally set mark. Remove after use. 
drop
NUC-VAR: age			\ how many times was the cell awakened
NUC-VAR: generation		\ counts generations since start of breeding
NUC-VAR: genome-generation	\ counts generations since last mutation
NUC-VAR: code-cost		\ sum of code cost of all subgenes
NUC-VAR: energy			\ storage of energy, food or whatever
\ 'energy' can only be stored or moved, transformed, used, or even be lost,
\ but never be produced by any action of a cell.
\ It is something like a 'real' stuff.
\ This reality only applies to the cells,
\ the universe can produce any reallity it happens to enjoy ;-)
\ (and you can break this rule if you feel like ;-)

\ NUC-VAR: nuc-error
NUC-VAR: reprodctn-threshold	\ how much energy to trigger reproduction?
NUC-VAR: age-threshold ( --addr)	\ does't get older than this
NUC-VAR: appearance			\ how it looks

\ hmm, we could make the (over) next 32 cells subject to diversification
\ and storing a mask which addresses to diversify in the cell called
\ my-diversifctn-mask
\ (even this one could be diversified xor'ing a few bits.)
\ (there can be more than one bitmask for different diversification strategies)
\
\ take care to limit the range of addresses that are diversified...
NUC-VAR: my-diversifctn-mask
nuc-floats# [IF]
    NUC-VAR: f-organ-div-mask		\ a bit for each: diversification flags
    NUC-VAR: f-param-div-mask
    NUC-VAR: f-invisibl-div-mask
\     4 CONSTANT nuc-bitmasks#		\ defined earlier
\ [ELSE]
\     1 CONSTANT nuc-bitmasks#
[THEN]

dup CONSTANT nuc-diversificable-area	\ base index

\ some named organs
\ the most archaic 'organs' are part of the nuc
\ these 'organs' can be used in any way by wake-me
\ they are hereditary, get passed on to the children
\ they can be made subject to diversification
dup CONSTANT nuc-organs	\ index of first organ
nuc-organs# [IF] \ compile time value
    \ organs are nuc variables that the genome can use freely, diversified.

\ I define them as named words
\ It seems more efficient and more readable
: define-nuc-organs ( -- )
    nuc-organs		( base-index )
    nuc-organs# 0 ?DO	( current-nuc-var-index )
	(scratch-buf) stringbuf-empty
	s" NUC-VAR: organ-" 	(scratch-buf) cat
	i [char] A +		(scratch-buf) char-cat
	(scratch-buf) string@ EVALUATE
    LOOP drop ;

define-nuc-organs
nuc-organs# +

[THEN]

: organ? ( index -- flag )   nuc-organs  dup nuc-organs# +  within ;
    

dup CONSTANT nuc-parameters	\ base index of nuc-parameters
nuc-parameters# [IF] \ compile time value
    \ read only variant of organs, diversified:

: define-nuc-parameters ( -- )
    nuc-parameters		( base-parameter-index )
    nuc-parameters# 0 ?DO	( current-nuc-var-index )
	(scratch-buf) stringbuf-empty
	s" NUC-VAR: parameter-" (scratch-buf) cat
	i [char] A +		   (scratch-buf) char-cat
	(scratch-buf) string@ EVALUATE
    LOOP drop ;

define-nuc-parameters
nuc-parameters# +

: n'th-nuc-parameter ( u - a )
    dup 0<  over nuc-parameters# < 0= or
    ABORT" n'th-nuc-parameter: Index out of range."
    cells parameter-A + ;

[THEN]

: nuc-parameter? ( index -- flag )
    nuc-parameters  dup nuc-parameters# +  within ;


dup CONSTANT nuc-invisibles	\ base index
nuc-invisibles# [IF] \ compile time value
    \ not visible to the cells, but diversified.

: define-nuc-invisibles ( -- )
    nuc-invisibles		( base-invisible-index )
    nuc-invisibles# 0 ?DO	( current-nuc-var-index )
	(scratch-buf) stringbuf-empty
	s" NUC-VAR: invisible-" (scratch-buf) cat
	i [char] A +		(scratch-buf) char-cat
	(scratch-buf) string@ EVALUATE
    LOOP drop ;

define-nuc-invisibles
nuc-invisibles# +

: n'th-nuc-invisible ( u - a )
    dup 0<  over nuc-invisibles# < 0= or
    ABORT" n'th-nuc-invisible: Index out of range."
    cells invisible-A + ;

[THEN]

: nuc-invisible? ( index -- flag )
    nuc-invisibles  dup nuc-invisibles# +  within ;


\ ******** The diversificable integer area stops here. *********
dup nuc-diversificable-area - CONSTANT nuc-diversificable-items


dup CONSTANT nuc-secrets	\ base index
nuc-secrets# [IF] \ compile time value
    \ not visible to the cells, and *not* diversified.

: define-nuc-secrets ( -- )
    nuc-secrets		( base-secret-index )
    nuc-secrets# 0 ?DO	( current-nuc-var-index )
	(scratch-buf) stringbuf-empty
	s" NUC-VAR: secret-" (scratch-buf) cat
	i [char] A +		    (scratch-buf) char-cat
	(scratch-buf) string@ EVALUATE
    LOOP drop ;

define-nuc-secrets
nuc-secrets# +

: n'th-nuc-secret ( u - a )
    dup 0<  over nuc-secrets# < 0= or
    ABORT" n'th-nuc-secret: Index out of range."
    cells secret-A + ;

[THEN]

: secret? ( index -- flag )   nuc-secrets  dup nuc-secrets# +  within ;


\ potencies
\ special organs, which are a bit 'more real' than the ordinary organs
\ this variables contain energy, which can be moved from one place to the other
\ we can make exceptions for reproduction, if we want ... hmm
\ not used in the moment

\ cell-variables
\ nuc-var cell-variables	\ pointer to other data or zero 
\ these datas can be anywhere
\ good place to have shared datas
\ not used in the moment

dup CONSTANT nuc-scan-limit	\ limit for integer nuc scans

LIST: integer-nuc-vars
nuc-var-xts integer-nuc-vars copy-simple-list-elements

\ NUC-VAR: nuc-link		\ link to other cells on the same spot
\ makes multi-celled organismes possible
\ not used yet
\  ***************** End of integer nuc vars *********************

\ dfaligned

\ ********** Start of the FLOATING POINT NUC AREA. ***************

\ As I want nuc-length# to be dfaligned I define nuc-float-offset# anyway:
dup cells dfaligned CONSTANT nuc-float-offset#

LIST: dfloat-nuc-vars

nuc-floats# [IF] \ if floating point nuc variables are present

    ( index ) dup CONSTANT nuc-float-start-index

\ Slow, only for display and such:
: n'th-df-nuc-var-xt ( i -- xt )  dfloat-nuc-vars n'th-node @ ;

: NUC-f-VAR: ( "name" xt-giving-base index -- )
    get-name >r
    dfloats (base+offset:) 2drop
    r@ string@ get-xt
    dup nuc-var-xts >list	\ xt to nuc-var-xts list
    dfloat-nuc-vars >list	\ xt to dfloat-nuc-vars list
    r> stringbuf-close ;

\ Define a group of named nuc float vars:
: define-float-var-family ( xt-giving-base addr count u -- )
    0 ?DO ( xt-giving-offset addr count )
	s" NUC-f-VAR: " 	(scratch-buf) string!
	2dup			(scratch-buf) cat
	i [char] A +		(scratch-buf) char-cat
	third i  (scratch-buf) string@ EVALUATE
    LOOP
    2drop drop ;


\ Some named float organs.
\ These floating point 'organs' can be used in any way by wake-me.
\ They are hereditary, get passed on to the children.
\ They can be made subject to diversification.

dup CONSTANT nuc-f-organs-i	\ nuc var base index of float nuc organs
nuc-f-organs# [IF] \ compile time value
: define-nuc-f-organs ( -- )
    ['] nuc-f-organ-base  s" f-organ-"
    nuc-f-organs#  define-float-var-family ;

define-nuc-f-organs  nuc-f-organs# +
[THEN]


dup CONSTANT nuc-f-parameters-i	\ nuc var base index float nuc parameters
nuc-f-parameters# [IF] \ compile time value
: define-nuc-f-parameters ( -- )
    ['] nuc-f-parameter-base  s" f-parameter-"
    nuc-f-parameters#  define-float-var-family ;

define-nuc-f-parameters  nuc-f-parameters# +
[THEN]


dup CONSTANT nuc-f-invisibles-i	\ nuc var base index float nuc invisibles
nuc-f-invisibles# [IF] \ compile time value
    \ not visible to the cells, but diversified.
: define-nuc-f-invisibles ( -- )
    ['] nuc-f-invisible-base  s" f-invisible-"
    nuc-f-invisibles#  define-float-var-family ;

define-nuc-f-invisibles  nuc-f-invisibles# +

[THEN]

\ dup nuc-float-start-index - CONSTANT nuc-f-diversificable-items   \ unused
\ *********** The DIVERSIFICABLE FLOAT AREA STOPs here. ************

dup CONSTANT nuc-f-secrets-i	\ nuc var base index float nuc secrets
nuc-f-secrets# [IF] \ compile time value
    \ not visible to the cells, and *not* diversified.

: define-nuc-f-secrets ( -- )
    ['] nuc-f-secret-base  s" f-secret-"
    nuc-f-secrets#  define-float-var-family ;

define-nuc-f-secrets  nuc-f-secrets# +
[THEN]

( index ) CONSTANT nuc-df-scan-limit

\ ****************  the NUC IS COMPLETE.  ********************
\ nuc-float-offset# nuc-floats# dfloats + CONSTANT nuc-length#


: set-nuc-offsets ( -- ) \ just for now... (still globally fixed nuc structure)
    nuc-float-offset#
    dup f-organ-offset !	nuc-f-organs# dfloats +
    dup f-parameter-offset !	nuc-f-parameters# dfloats +
    dup f-invisible-offset !	nuc-f-invisibles# dfloats +
    f-secret-offset ! ;


\ Slow, but universal words for display and such:
: f-var-xt? ( nuc-base-index index-limit organ-index -- xt TRUE | FALSE )
    swap >r
    dup 0 r> WITHIN IF
	+ nuc-var-xt  TRUE EXIT
    ELSE
	2drop FALSE
    THEN ;

: f-organ-xt? ( organ-index -- xt TRUE | FALSE )
    >r nuc-f-organs-i nuc-f-organs# r> f-var-xt? ;

: f-parameter-xt? ( parameter-index -- xt TRUE | FALSE )
    >r nuc-f-parameters-i nuc-f-parameters# r> f-var-xt? ;

: f-invisible-xt? ( invisible-index -- xt TRUE | FALSE )
    >r nuc-f-invisibles-i nuc-f-invisibles# r> f-var-xt? ;

: f-secret-xt? ( secret-index -- xt TRUE | FALSE )
    >r nuc-f-secrets-i nuc-f-secrets# r> f-var-xt? ;

: n'th-dfloat-nuc-var ( i -- addr )	\ index from zero to nuc-floats#-1
    dfloats nuc-f-organ-base + ;

\ Words to check all dfloat nuc variables for exceptions:
: nuc-all-real? ( -- flag )
    nuc-floats# 0 ?DO
	i n'th-dfloat-nuc-var df@ real? 0= IF  UNLOOP  FALSE EXIT  THEN
    LOOP
    TRUE ;

: nuc-has-unreal? ( -- flag )  nuc-all-real? 0= ;

: nuc-with-inf? ( -- flag )
    nuc-floats# 0 ?DO
	i n'th-dfloat-nuc-var df@ infinity? IF  UNLOOP  TRUE EXIT  THEN
    LOOP
    FALSE ;

: nuc-with-neg-inf? ( -- flag )
    nuc-floats# 0 ?DO
	i n'th-dfloat-nuc-var df@ infinity? -1 = IF  UNLOOP  TRUE EXIT  THEN
    LOOP
    FALSE ;

: nuc-with-pos-inf? ( -- flag )
    nuc-floats# 0 ?DO
	i n'th-dfloat-nuc-var df@ infinity? 1 = IF  UNLOOP  TRUE EXIT  THEN
    LOOP
    FALSE ;

: nuc-with-nan? ( -- flag )
    nuc-floats# 0 ?DO
	i n'th-dfloat-nuc-var df@ is-NaN? IF  UNLOOP  TRUE EXIT  THEN
    LOOP
    FALSE ;

[ELSE] \ no floating point nuc variables.
    drop
    nuc-scan-limit CONSTANT nuc-df-scan-limit	\ must be defined anyway
[THEN]

\ nuc length is dfaligned
nuc-float-offset# nuc-floats# dfloats + CONSTANT nuc-length#

\ ****************************************************************
\ end	nuc structure



\ ****************************************************************
\ ***********************  nuc pointers:  ************************ 
\ ****************************************************************

\ : cp@ ( -- addr-of-actual-cell )  cp ;
: cp@ ( -- addr-of-actual-cell )  POSTPONE cp ;  IMMEDIATE
: |cp@| ( -- addr-of-actual-cell )  cp ;	\ use this when interpreting
\ : cp@ ( -- addr-of-actual-cell )  cp @ ;
\ cp@ gives the address of the cells nucleus.
\ Each cell is represented by it's nucleus, containing pointers to his genes,
\ cell intern variables and other cell data.
\ Each nucleus starts with the nuc. The nuc is 'The Hard Core' of each nucleus.
\ The structure of the nuc is the same for all nuclei.
\ It contains nuc-var's and execution tokens for different actions.
\ After the nuc there can be a structure containing other things.

nuc-floats# [IF]

: cp! ( addr -- )
    dup to cp
    IF			\ don't try to set pointers if there's nobody.

	[ nuc-f-organs# ] [IF]
	    cp f-organ-offset @ + to nuc-f-organ-base
	[THEN]

	[ nuc-f-parameters# ] [IF]
	    cp f-parameter-offset @ + to nuc-f-parameter-base
	[THEN]

	[ nuc-f-invisibles# ] [IF]
	    cp f-invisible-offset @ + to nuc-f-invisible-base
	[THEN]

	[ nuc-f-secrets# ] [IF]
	    cp f-secret-offset @ + to nuc-f-secret-base
	[THEN]
	EXIT
    THEN

    \ cp is FALSE: programmers aesthetics...
    [ nuc-f-organs# ]		[IF]	0 to nuc-f-organ-base		[THEN]
    [ nuc-f-parameters# ]	[IF]	0 to nuc-f-parameter-base	[THEN]
    [ nuc-f-invisibles# ]	[IF]	0 to nuc-f-invisible-base	[THEN]
    [ nuc-f-secrets# ]		[IF]	0 to nuc-f-secret-base		[THEN]
;

[ELSE]
    : cp! ( addr -- )   to cp ;
    \ Storing the address of a nuc in cp makes it the actual one.
    \ Always using cp! leaves the option to add other things here.
[THEN]


\ ****************************************************************
\ end	nuc pointers



\ ****************************************************************
\ *****************  Some basic nuc words:  ********************** 
\ ****************************************************************

\ Get address of the i'th *integer* nuc-var. *Not* for floats.
: nuc-addr ( i -- a )   cells cp@ + ;

nuc-floats# [IF]
\ Nuc continuous index (not starting at zero for floats)
: nuc-dfloat-addr ( i -- a )
    nuc-float-start-index - dfloats nuc-f-organ-base + ;

: nuc-var-is-float? ( index -- flag )	\ index must be within range
    [ nuc-float-start-index 1- ] literal > ;

: |nuc-addr| ( i -- a )			\ generic, for integers and dfloats
    dup nuc-var-is-float? IF  nuc-dfloat-addr  ELSE  nuc-addr  THEN ;

[ELSE] \ no nuc floats, dummy words:
: nuc-var-is-float? ( index -- false )   drop FALSE ;

: |nuc-addr| ( i -- a )   nuc-addr ;
[THEN]

\ Some action list's for selection in menus or by the mutation process:
2 nLIST: eat-actions	\ includes related scoring function.
			\ See:  guess-scoring-function
LIST: reproduce-actions
LIST: show-me-actions
LIST: wake-me-actions
internal' noop wake-me-actions >list

: on-trial? ( -- flag )   nuc-flags @ nuc-on-trial and ; \ *not* normalized
: selected? ( -- flag )   nuc-flags @ nuc-is-selected and 0<> ;

VARIABLE (id)	(id) off	\ Produce unique id's
: new-id ( -- id )	(id) @ 1+ dup (id) ! ;

VARIABLE (genome-id)	(genome-id) off
: new-genome-id ( -- genome-id )	(genome-id) @ 1+ dup (genome-id) ! ;

: new-nucleus ( length -- addr flag | false )
\ allocates memory and erases it, (but does *not* set cp yet)
    dup >r		\ length is on return stack
    allocate	( addr ior )
    IF	rdrop drop cr ." couldn't allocate space for a nucleus!" 1000 ms cr
	false		\ flag
    ELSE     ( addr )
	dup r@ erase	\ initiate everything to zero
	cp@ swap	\ ( cp@ addr ) keep old cp on stack
	dup cp!		\ temporary set cp
	r> length !	\ (length works, because cp is set now)
[ nuc-floats# ]	[IF] set-nuc-offsets [THEN]
	swap cp!	\ ( addr ) restore cp
	true		\ ( -- addr true )
    THEN ;

: new-nucleus-as-word	( length -- addr TRUE | * ) \ always true, or ABORT
\ similar to new-nucleus, but creates the nucleus in the dictionary
[ nuc-floats# ] [IF]	\ must be dfaligned to get same nuc-float-offset# 
    here dup dfaligned swap - allot
[THEN]
    dup >r			\ length is on return stack
    here swap	( here length )
    allot	( here )
    dup r@ erase		\ initiate everything to zero
    cp@ swap	( cp@ addr ) 	\ keep old cp on stack
    dup cp!			\ temporary set cp
    r> length !			\ (length works, because cp is set now)
    nuc-flags dup @ nuc-is-word or swap !
[ nuc-floats# ]	[IF] set-nuc-offsets [THEN]
    swap cp!	( addr )		\ restore cp
    true ;	( -- addr true )	\ true, don't get here if allot aborts

: log-cat-id ( -- )
    s" ID:"			cat-log
    id @ num>string		cat-log
    s"  GI:"			cat-log
    genome-id @ num>string	cat-log
    s"  "			cat-log ;

\ These need to be predefined, so let's do it here
: select-nuc ( -- )	\ cp must be set
    nuc-flags dup @ nuc-is-selected or swap ! ;

: de-select-nuc ( -- )	\ cp must be set
    nuc-flags dup @ [ nuc-is-selected invert ] literal and swap ! ;

: toggle-selection ( -- )   nuc-flags dup @ nuc-is-selected xor swap ! ;

\ ****************************************************************
\ end	basic nuc words



\ ****************************************************************
\ *****************  How the cells show up:  *********************
\ ****************************************************************

\ Appearance: how does a cell show on the screen

\ Scaling factor for integer values that are displayed as ascii
VARIABLE 2-ascii-scale
decimal
1000 2-ascii-scale !

nuc-floats# [IF]
    dfVARIABLE f-2-ascii-scale
    1e-3 f-2-ascii-scale df!
[THEN]

\ normalization to the continuous range of printable ASCII codes '!' to '~'
: 2-ascii ( n -- n' )
    [char] ! -		\ shift range for normalization 
    [ decimal ]
    94 mod		\ 93 printable ascii codes in a row: '!' to '~'
    dup 0< IF 94 + THEN
    [char] ! + ;	\ printable codes start with '!'

: .ascii ( n -- )   2-ascii emit ;	\ prints as normalized ASCII

: .ascii-num ( n -- )   [char] 0 +  .ascii ;

: .scaled-ascii ( n -- )   2-ascii-scale @ /  .ascii-num ;

nuc-floats# [IF]
    : .float-scaled-ascii ( r -- )   f-2-ascii-scale df@ f* f>s .ascii-num ;
[THEN]

\ not finally edited:
\ words to determine a string with caracters to be shown by show-me-xt:
: ranged-char ( u addr count -- c )
    rot					( addr count u )
    over mod				( addr count mod )
    dup 0< IF + ELSE nip THEN		( addr mod' )
    + c@ ;

: selected-chars ( u -- c)
    s" *+.o:xØ°æ÷ø%,-|/\~¡¢¥§@#$&'0;<=>O[¿^_`s{}ª«¬-±·º»]" ranged-char ;

: pretty-char ( u - c )
    s" #$%&'()*+,-./08:;<=>@IOS[\]^_`losx{|}~¢¥§ª«¬-°²µ¶·,º»Øæç÷ø"
    ranged-char ;

: round-char ( u - c )	s" %*+.0@O`aox¢«°·º»Øæ÷ø" ranged-char ;

: dots ( u - c )	s" '*+,-.:;@O^`ox~«°·ºØæ÷ø" ranged-char ;

: show-ascii	appearance @					.ascii ;
' show-ascii show-me-actions >list
: show-age	age @ .ascii-num ;
' show-age  show-me-actions >list
: show-energy	energy @ .scaled-ascii ;
' show-energy show-me-actions >list
: show-generation generation @ .ascii-num ;
' show-generation show-me-actions >list
: show-genome	genome-id @ .ascii-num ;
' show-genome show-me-actions >list
: show-genome-b	genome-id @  selected-chars emit ;
' show-genome-b show-me-actions >list
: show-genome-generation genome-generation @ .ascii-num ;
' show-genome-generation show-me-actions >list
: show-code-length
    code-cost @
    (default-gene-cost#) >r
    r@ 1- +			\ to catch conditionals
    r> /
    1-				\ don't count ;gene
    .ascii-num ;		\ start with '0'
' show-code-length show-me-actions >list

\ Words to show a integer nuc vars value coded as ASCII
VARIABLE show-int-nuc-var-xt	' energy show-int-nuc-var-xt !

: show-integer-nuc-var ( -- )   show-int-nuc-var-xt @ EXECUTE @ .scaled-ascii ;
' show-integer-nuc-var show-me-actions >list

VARIABLE show-sign-tolerance	9 show-sign-tolerance !

: show-integer-var-sign ( -- )
    show-int-nuc-var-xt @ EXECUTE @ >r
    show-sign-tolerance @
    r@ over        > IF [char] + ELSE
    r@ over negate < IF [char] - ELSE
	[char] o THEN THEN rdrop
    .ascii
    drop ;
' show-integer-var-sign show-me-actions >list

\ 'show-A' and 'show-sign-A' are obsolete, defined for compatibility:
nuc-organs# [IF] \ at least organ-A defined.
    : show-A	organ-A @ .scaled-ascii ;
    ' show-A show-me-actions >list

    : show-sign-A
    	organ-A @ >r
	show-sign-tolerance @
	r@ over        > IF [char] + ELSE
	r@ over negate < IF [char] - ELSE
	    [char] o THEN THEN rdrop
	.ascii
	drop ;
    ' show-sign-A show-me-actions >list
[THEN] \ at least organ-A defined.

nuc-floats# [IF]

    VARIABLE show-float-nuc-var-xt
    nuc-float-start-index nuc-var-xt show-float-nuc-var-xt !

    : show-float-nuc-var ( -- )
	show-float-nuc-var-xt @ EXECUTE df@ .float-scaled-ascii ;
    ' show-float-nuc-var show-me-actions >list

    dfVARIABLE float-show-sign-tolerance      9e0 float-show-sign-tolerance df!

    : show-float-var-sign ( -- )
	show-float-nuc-var-xt @ EXECUTE df@
	float-show-sign-tolerance df@
	fover fover f> IF      [char] +
	ELSE fnegate
	fover fover f< IF      [char] -
	ELSE fover is-NaN? IF  [char] ?
	ELSE                   [char] o
	THEN THEN THEN
        .ascii
	fdrop fdrop ;
' show-float-var-sign show-me-actions >list

[THEN]

\ <look-at> is a showing function which can be changed by the user during
\ the experiment
VARIABLE look-at-xt	' show-genome-b look-at-xt !
: <look-at> ( -- )	look-at-xt @ EXECUTE ;  \ works like a deferred word
' <look-at> show-me-actions >list

: show-background ( -- )	paint-background space ;

\ ****************************************************************
\ end	show up



\ ****************************************************************
\ ****************  The World: Spot and Time.  *******************
\ ****************************************************************

INCLUDE worlds.fs

\ compatibility to old benchmarks:
[DEFINED] transit-11-bench-A
[DEFINED] transit-12-bench OR
[DEFINED] brew-crash-test  OR [IF]	INCLUDE bcompat-11-12.fs 	[THEN]

: spot-var-xt ( index -- xt )
    dup IF
	1- spot-var-xts n'th-node @  EXIT
    THEN
    drop
    ['] fcp ;

: spot-var-name ( index -- addr count )   spot-var-xt xt>string ;

: log-spot-variables ( addr count -- )
    ( addr count -- ) 2dup				  cat-log

    spot-qualities# dup IF
	s" qualities: "				  cat-log
	0 DO
	    i 2 + n'th-spot-variable @ num>string cat-log
	    s"  "				  cat-log
	LOOP
    ELSE drop THEN

    spot-properties# dup IF
	s" properties: "			 cat-log
	0 DO
	    i [ 2 spot-qualities# + ] literal +
	    n'th-spot-variable @ num>string	 cat-log
	    s"  "				 cat-log
	LOOP
    ELSE drop THEN

    spot-secrets# dup IF
	s" secrets: "				cat-log
	0 DO
	    i [ 2 spot-qualities# + spot-properties# + ] literal + 
	    n'th-spot-variable @ num>string	cat-log
	    s"  "				cat-log
	LOOP
    ELSE drop THEN

    s"  <food>: "				cat-log
    <food> @ num>string	0 log-it

[ spot-f-qualities# ] [IF]
    2dup			cat-log
    s" f-qualities:"		cat-log
    spot-f-qualities# 0 DO
	s"  " cat-log
	i n'th-spot-f-variable df@ float-display-width float>string cat-log
    LOOP
    s" " cat-log 0 log-out-line
[THEN]

[ spot-f-properties# ] [IF]
    2dup			cat-log
    s" f-properties:"		cat-log
    spot-f-qualities# dup spot-f-properties# + swap DO
	s"  " cat-log
	i n'th-spot-f-variable df@ float-display-width float>string cat-log
    LOOP
    s" " cat-log 0 log-out-line
[THEN]

[ spot-f-secrets# ] [IF]
    2dup			cat-log
    s" f-secrets:"		cat-log
    spot-f-qualities# spot-f-properties# + dup spot-f-secrets# + swap DO
	s"  " cat-log
	i n'th-spot-f-variable df@ float-display-width float>string cat-log
    LOOP
    s" " cat-log 0 log-out-line
[THEN]

    2drop ;

spot-floats# [IF]  \ Words to check all dfloat spot variables for exceptions:
: spot-all-real? ( -- flag )
    spot-floats# 0 ?DO
	i n'th-spot-f-variable df@ real? 0= IF  UNLOOP  FALSE EXIT  THEN
    LOOP
    TRUE ;

: spot-has-unreal? ( -- flag )  spot-all-real? 0= ;

: spot-with-inf? ( -- flag )
    spot-floats# 0 ?DO
	i n'th-spot-f-variable df@ infinity? IF  UNLOOP  TRUE EXIT  THEN
    LOOP
    FALSE ;

: spot-with-neg-inf? ( -- flag )
    spot-floats# 0 ?DO
	i n'th-spot-f-variable df@ infinity? -1 = IF  UNLOOP  TRUE EXIT  THEN
    LOOP
    FALSE ;

: spot-with-pos-inf? ( -- flag )
    spot-floats# 0 ?DO
	i n'th-spot-f-variable df@ infinity? 1 = IF  UNLOOP  TRUE EXIT  THEN
    LOOP
    FALSE ;

: spot-with-nan? ( -- flag )
    spot-floats# 0 ?DO
	i n'th-spot-f-variable df@ is-NaN? IF  UNLOOP  TRUE EXIT  THEN
    LOOP
    FALSE ;
[THEN]

\ Build a string of the form 'world# 2 named: NAME' and return handle:
: world-string ( -- handle )		\ Please do close buffer
    s" world# "		string!! >r
    world# num>string	r@ cat
    s"   named: "	r@ cat
    world-name2@	r@ cat
    r> ;

: log-random-generator ( addr count -- )
    ( addr count -- )	cat-log
    random-xt @ xt>string cat-log	s"  " cat-log
    random-xt @ CASE
	['] random-generalized OF
	    (random-generalized) 2@ swap
	    num>string 	cat-log	s"  " cat-log
	    num>string 	cat-log
	ENDOF
	['] random-BRODIE OF
	    seed-BRODIE @ num>string cat-log
	ENDOF
    ENDCASE
    s" " 0 log-it ;

: random-qualities ( n mask -- )
    swap  1+ >r		( mask  r: n+1)
    0
    BEGIN		( mask index  r: n+1)
	2dup rshift 1 and IF
	    r@ random-ranged
	    2 random-ranged 0= IF negate THEN
	    over 2 + n'th-spot-variable ! \ 'A-quality'='2 n'th-spot-variable'
	THEN
	1+ dup spot-qualities# =
    UNTIL 2drop rdrop ;

\ Do something on all spots (sequential)
\ (see 'do-everywhere-maybe-nuc' for a more generic version).
VARIABLE (do-everywhere-xt)
: do-everywhere ( xt -- )
    (do-everywhere-xt) !
    spot @ >r				\ maybe it's worth saving?
    spots 0 DO				\ loop over all spots
	i >spot!
	(do-everywhere-xt) @ EXECUTE	\ and do your job
    LOOP
    r> >spot! ;				\ you never know what's good for...

VARIABLE (linear-index)		(linear-index) off
: linear-free-spot? ( -- i' true | false )
    (linear-index) @ dup spots + swap DO
	i spots mod dup someone-here? 0= IF
	    dup future someone-here? present 0= IF
		dup 1+ (linear-index) !
		TRUE unloop EXIT
	    ELSE drop THEN
	ELSE drop THEN
    LOOP
    FALSE ;

: free-neighbour-spot? ( -- i' true | false )
    world-mode? IF
	world-free-neighbour-spot? EXIT
    THEN
    linear-free-spot? ;

: nuc-unlink ( -- )			\ remove actual cell from the world
    future				\ if it's called with an individual
    fcp off
    present
    fcp off ;		\ hmm, I'm not sure about this... ######

: free-spots ( -- n )			\ how many spots are not inhabited?
    0					\ counter
    spots 0 DO				\ loop over all spots
	i someone-here? 0= IF 1+ THEN	\ count
    LOOP ;

: spot-vars@ ( -- spot-var-n spot-var-n-1 ... spot-var0 )
    0 field-i-planes# 1- ?DO
	i n'th-spot-variable @
    -1 +LOOP ;

: spot-vars! ( spot-var-n spot-var-n-1 ... spot-var0  addr -- )
    >r
    r@ field-i-planes# cells +  r> ?DO
	i !
    cell +LOOP ;

spot-floats# [IF]
: spot-df-vars@ ( -- spot-df-var-n spot-df-var-n-1 ... spot-df-var0 )
    0 spot-floats# 1- ?DO
	i n'th-spot-f-variable df@
    -1 +LOOP ;

: spot-df-vars! ( spot-df-var-n spot-df-var-n-1 ... spot-drf-var0  addr -- )
    >r
    r@ [ spot-floats# dfloats ] literal +  r> ?DO
	i df!
    [ 1 dfloats ] literal +LOOP ;
[THEN]


: xy>spot ( x y -- spot )	\ DADA MARK: scrolling
    0 (dim-spots) @  *  +  ;

: spot>xy ( spot -- x y )	\ DADA MARK: scrolling
    0 (dim-spots) @  /mod ;

: spot-at ( -- )   spot @  spot>xy  at-xy ;	\ set the cursor on spot


VARIABLE child-spot			\ handy ;-)     spot of an actual child

: log-cat-step&spot ( -- )
    world# num>string	cat-log
    s" :"		cat-log
    spot 2@ swap ( spot step )
    num>string		cat-log
    s" :"		cat-log
    num>string		cat-log
    s"  "		cat-log ;

\ ****************************************************************
\ end	world



\ ****************************************************************
\ ***********  Reproduction (1), genes, genomes:  ****************
\ ****************************************************************

\ Setting wake me action:
: setup-wake-me ( internal-xt -- )
    dup wake-me-internal !			\ store internals xt
    dup [internal'] noop = IF  genome-id off  THEN
    >body	( internals-body )
    dup >gene-compiled-xt @ wake-me-xt !	\ store genes xt
    dup >gene-evaluated-xt @ wake-me-xt !	\ store genes xt
    >gene-cost @ code-cost ! ;			\ set >gene-cost

: nuc-does-nothing ( -- )   [internal'] noop  setup-wake-me ;

INCLUDE symbols-stack.fs

: include-genes ( c-addr count -- )
    genes-dir 2swap file-name-cat
    dup string@ INCLUDED
    stringbuf-close ;

    s" conditionals.fs" include-genes
    INCLUDE mutation-0.3.fs

\ Cloning a nucleus:
\ VARIABLE cloned		cloned off		\ counter
: clone ( -- addr flag | false )		\ clones the actual nucleus
    \ produces an exact clone of the current nuc. same id, (should be set)
    \ does not change cp, the actual cell stays the same

    \ genom on trial? works on the mother cell, before data is copied
    on-trial? IF				\ genom on trial?
	trial-phase @ genome-generation @ > IF	\ trial continues? 
	    wake-me-xt @ eb>counter
	    dup @ 1+ swap !			\ increase counter
	ELSE
	    end-trial-phase	    
	THEN
    THEN

    length @ new-nucleus		\ tries to allocate a new nucleus
    IF ( addr )
	cp@ over length @ move		\ clone all data from actual cell
	1 cloned +!			\ increase counter

	\ we must clear the nuc-is-word flag:
	cp@ >r  dup cp!			\ temporary switch in to new nuc
	nuc-flags dup @ [ nuc-is-word invert ] literal and swap !
	r> cp!				\ switch back

	true				\ :) birth announcement
    ELSE ." couldn't clone the nucleus" cr	\ cloning failure
	false
    THEN ;


false [IF]			\ not used any more
VARIABLE clone-cost	clone-cost off		\ how much energy for a new one
: cell-division-cloning ( -- )			\ cell-division by cloning
						\ starting two fresh lives
    clone					\ tries to clone
    IF ( addr )					\ ok?
	age off					\ cloning starts 2 *new* lifes
	new-id id !				\ with new individual id's
	1 generation +!				\ next generation
	1 genome-generation +!			\ next genome-generation
	energy @ clone-cost @ - energy !	\ pay a price
	energy @ 2/ dup energy !		\ share the remaining energy
\	nuc-error off

	swap cp!				\ make the clone the actual one
						\ now whe're in the newly born
	energy !				\ give it it's share
	age off					\ cloning starts 2 *new* lifes
	new-id id !				\ with new individual id's
	1 generation +!				\ next generation
\	nuc-error off
	1 genome-generation +!			\ next genome-generation
    ELSE ." couldn't clone the nucleus" cr 	\ cloning failure
    THEN ;
' cell-division-cloning reproduce-actions >list
[THEN]

\ Set field element i to a cloned nuc of the actual one
: set-field-element ( i -- addr=flag )	\ returns addr of the clone or 0
    dup someone-here?			\ is it free?
    IF drop false			\ no, sorry
    ELSE				\ ( i )
	spot @ >r    >spot!		\ ( --   R:old-cp@, spot)
	clone				\ ( addr flag | false  R:old-cp@, spot)
	IF				\ cloning was possible
	    dup fcp !
	    spot @ child-spot !		\ spot of youngest child
	ELSE false THEN			\ cloning didn't work
	r> >spot!
    THEN ;

: ?increase-genome-probability ( -- )
    store-genomes? 0=	IF  EXIT  THEN
    on-trial?		IF  EXIT  THEN

    1
    wake-me-internal @  current-genome-pool-xt @ execute  change-one ;

: fertile? ( -- flag )				\ enough energy for children?
    energy @ reprodctn-threshold @ > ;

: ?reproduce ( -- )				\ maybe reproduce
    fertile? IF
	reproduce-xt @ EXECUTE			\ yes, try it
    THEN ;

: see-compiled-genome ( gene-xt -- )
    dup					\ save xt ('see' bug)
    [ decimal ] 48 stringbuf-open >r
    s" see " r@ cat
    xt>string  r@ cat
    s"  "    r@ cat
    also genes   r@ string@
    ['] EVALUATE CATCH  previous			\ catch see bug
    r> stringbuf-close
    IF							\ 'see' did throw,
	cr ." NOT DONE YET" DADA			\  do it yourself.#####
    ELSE drop THEN ;

: see-genome-on-trial ( evaluation-buffer-address -- )
    cr
    ." genome is on trial." cr
    dup
    eb>length @
    swap eb>sequence swap type ;    

\ see genome dealing with trial and (Gforth) 'see' bug
: see-genome ( -- )
    page
    wake-me-xt @
    on-trial? IF				\ on trial?
	see-genome-on-trial
    ELSE					\ compiled: try 'see'
	see-compiled-genome
    THEN ;

: is-gene? drop FALSE ;	\ heavy cheating ;-)

\ : page-see ( xt -- )
:NONAME ( xt -- )
    dup is-gene?	\ dummy FALSE at the moment
    IF   see-genome
    ELSE
	page
	dup					\ save xt ('see' bug)
	[ decimal ] 48 stringbuf-open >r
	s" see "	r@ cat
	xt>string	r@ cat
	s"  "		r@ cat
	also genes	r@ string@
	['] EVALUATE CATCH  previous		\ catch 'see' bug
	r> stringbuf-close
	IF					\ 'see' throw
	    bell  ." page-see: Couldn't see "
	    xt>string dup IF
		type  [char] . emit
	    ELSE drop 2drop THEN		\ probably Gforth specific
	ELSE drop THEN
    THEN ; IS <page-see>

\ ****************************************************************
\ end	reproduction (1)



\ ****************************************************************
\ *************************  Death:  *****************************
\ ****************************************************************

\ Switch leaving energy on spot when dying:
VARIABLE leave-energy-after-death	leave-energy-after-death off

\ VARIABLE died					\ counter
: die ( -- )					\ death
    energy @ >r					\ my energy can survive
    nuc-is-word nuc-flags @ and IF
	." Cells in the dictionary can't really die. " cr
	0
    ELSE
	on-trial? IF				\ genom on trial?
	    log-mask @ IF
		log-cat-id
		s" : died on trial "  log-death log-trial or  log-it
	    THEN
	    wake-me-xt @ decrease-eb-count	\ decrease counter
	THEN
	log-death? IF
	    log-cat-id
	    elitism? IF
		s" : removed." 0 log-it
	    ELSE
		s" : died"		cat-log
		energy @ dup 1 < IF
		    s"  starved ("	cat-log
		    num>string	cat-log
		    s" )"		cat-log
		ELSE drop THEN
		age @ age-threshold @ > IF
		    s"  old"	cat-log
		THEN
		s" ." 0 log-it
	    THEN
	THEN
	cp@ free				\ free  memory
    THEN
    0= IF					\ ready to die?
	leave-energy-after-death @ IF		\ what about my energy?
	    r@ <food> +!			\ leave it back
	THEN
	nuc-unlink				\ unlink from field
	false cp!				\ I'm gone
	1 died +!				\ counter
    ELSE		\ strange...
	log-cat-id
	s" cell at "			cat-log
	cp@ . num>string		cat-log
	s"  could not die in peace"	(log) string@ cr type
	log-it
    THEN
    rdrop ;

: will-die? ( -- flag )
    age @ 0=		IF FALSE EXIT THEN	\ we let newborns survive

    energy @ 1 <	IF TRUE EXIT THEN	\ burnt out?

    age-threshold @ IF				\ is there a highest age?
	age @ age-threshold @ >	EXIT		\ too old?
    THEN

    FALSE ;

: ?die ( -- )					\ maybe die, maybe not
    will-die? IF die THEN ;			\ sorry, your time has come

: remove-nuc ( -- )   die ;		\ just another name for the menus

\ Let's define them here:
VARIABLE (world-scanned-at-step)
: world-not-scanned ( -- )   -2 (world-scanned-at-step) ! ;
world-not-scanned

VARIABLE (nucs-scanned-at-step)
: nucs-not-scanned ( -- )  -2 (nucs-scanned-at-step) ! ;
nucs-not-scanned

:NONAME \ : (free-field) ( -- )		\ lets everybody die and cleans up
    this-world IF
	spots 0 DO			\ everywhere
	    i >spot!  fcp @		\ who's here?
	    dup IF			\ somebody?
		cp!			\ set as actual nuc
		die			\ sorry
	    ELSE drop THEN
	LOOP
	\   cloned off			\ I'm not clear if I want that.
	erase-field			\ clean everything up
    THEN
    nucs-not-scanned
    world-not-scanned ; IS (free-field)

DEFER ?record-free-field
: free-field ( -- )   (free-field)  ?record-free-field ;

\ ****************************************************************
\ end	death



\ ****************************************************************
\ ******************  A day in the life:  ************************
\ ****************************************************************

\  VARIABLE nuc-do-cost	decimal 1000 nuc-do-cost !
\  VARIABLE code-price	code-price off
2VARIABLE code-price-scale	2 100 code-price-scale 2!
\ switch if cells can change the qualities of the future
VARIABLE future-quality-change		future-quality-change on

future-change-individal [IF]		\ copy quality changes individually?
    \ Word to transfer quality changes by a cell individually to the future
    spot-qualities# [IF]
	: copy-qualities2future ( -- )
	    [ 2 spot-qualities# + ] literal 2 ?DO
		i n'th-spot-variable @
	    LOOP
	    future
	    2  [ 2 spot-qualities# + 1- ] literal ?DO
		i n'th-spot-variable !
	    -1 +LOOP
	    present ;
    [ELSE]
	: copy-qualities2future ( -- ) ;
    [THEN]
[THEN] \ future-change-individal

\ We need this logging words here:

\ Log 'u' integer values from nuc index (and a space) in the log buffer:
: log-nuc-i-values ( base-index u -- )
    dup 0= IF  2drop EXIT  THEN
    bounds ?DO
	dup i + nuc-addr @ num>string cat-log  s"  " cat-log
    LOOP
    0 log-out-line ;

nuc-floats# spot-floats# + [IF]
    \ Put 'u' floating point values from addr (and a space) in the log buffer:
    : log-df-values ( base-addr u -- )
	dup 0= IF  2drop EXIT  THEN
	0 DO	( base-addr )
	    i dfloats over + df@ float-display-width float>string cat-log
	    s"  " cat-log
	LOOP drop
	0 log-out-line ;
[THEN]

: log-nuc-variables ( addr count -- )	\ not checking

[ nuc-organs# ] [IF] \ at least organ-A defined.
    2dup			cat-log
    s" organs:	"		cat-log
    nuc-organs nuc-organs# log-nuc-i-values
[THEN] \ at least organ-A defined.

    2dup			cat-log
    s" diversification: "	cat-log
    my-diversifctn-mask @	log-bitmask
    s" 	energy: "		cat-log
    energy @ num>string		0 log-it

[ nuc-parameters# ] [IF]
    2dup			cat-log
    s" nuc-parameters:	"	cat-log
    nuc-parameters nuc-parameters# log-nuc-i-values
[THEN]

[ nuc-invisibles# ] [IF]
    2dup			cat-log
    s" nuc-invisibles:	"	cat-log
    nuc-invisibles nuc-invisibles# log-nuc-i-values
[THEN]

[ nuc-secrets# ] [IF]
    2dup			cat-log
    s" nuc-secrets:	"	cat-log
    nuc-secrets nuc-secrets# log-nuc-i-values
[THEN]

[ nuc-floats# ] [IF]

[ nuc-f-organs# ] [IF]
    2dup			cat-log
    s" f-organs: "		cat-log
    nuc-f-organ-base nuc-f-organs# log-df-values
    2dup			cat-log
    s" f-organ-div-mask: "	cat-log  f-organ-div-mask @ log-bitmask
    0 log-out-line
[THEN]

[ nuc-f-parameters# ] [IF]
    2dup			cat-log
    s" nuc-f-parameters: "	cat-log
    nuc-f-parameter-base nuc-f-parameters# log-df-values
    2dup			cat-log
    s" f-param-div-mask: "	cat-log   f-param-div-mask @ log-bitmask
    0 log-out-line
[THEN]

[ nuc-f-invisibles# ] [IF]
    2dup				cat-log
    s" nuc-f-invisibles: "		cat-log
    nuc-f-invisible-base nuc-f-invisibles#	log-df-values
    2dup			cat-log
    s" f-invisibl-div-mask: "	cat-log   f-invisibl-div-mask @ log-bitmask
    0 log-out-line
[THEN]

[ nuc-f-secrets# ] [IF]
    2dup			cat-log
    s" nuc-f-secrets: "		cat-log
    nuc-f-secret-base nuc-f-secrets# log-df-values
[THEN]

[THEN] \ nuc-floats#
    2drop ;

: ?log-nuc-&-spot-state ( addr-preamble count-preamble -- )
    log-mask @ dup 0= IF drop 2drop EXIT THEN

    >r	( addr count  r: mask )
    r@ log-organs and IF
	2dup log-nuc-variables
    THEN
    r@ log-spot-vars and IF
	2dup log-spot-variables
    THEN
    r> log-random and IF
	2dup log-random-generator
    THEN
    2drop ;

1 VALUE (brew-depth)		\ depth control in brew
0 VALUE (brew-depth-offset)

: brew-depth-adjust ( ... -- ... )
    depth (brew-depth) - to (brew-depth-offset) ;

: brew-depth-reset ( -- )   0 to (brew-depth-offset) ;

: brew-depth ( -- u )    (brew-depth) (brew-depth-offset) + ;

\ Switching background visibility in 3D and higher worlds:
VARIABLE (background-off)	(background-off) off
VARIABLE (background-skipped)	(background-skipped) off

\ Get some energy and pay some taxes:
: eat-and-pay ( -- )			\ brew0 mode only
    energy @				\ old energy (for reporting)

    eat-xt @ EXECUTE					\ eats

    log-meal? IF					\ log meal?
	s" Meal: "			cat-log
	energy @ over - num>string	cat-log
	s"  	Day cost: "	cat-log
	nuc-do-cost @ num>string	0 log-it
    THEN drop

    energy dup @ nuc-do-cost @ - swap !			\ and consumes energy

    code-price @ dup IF				\ price to pay for the code?
	code-cost @  (default-gene-cost#) */
	code-price-scale 2@ */

	log-mask @ dup IF
	    [ log-costs log-meal or ] literal and IF
		s" actual code costs : "	cat-log
		dup num>string			log-costs log-it
	    THEN
	ELSE drop THEN

	negate energy +!
    ELSE drop THEN ;

\ Let a cell do it's trick:
: wake-nuc ( --) \ Wake the genome.
    s" before:	" ?log-nuc-&-spot-state

    on-trial? IF			\ on trial?
	1 trial +!
	wake-me-xt @ dup
	eb>length @			\ get length
	swap eb>sequence swap		\ skip length
	also genes
	EVALUATE			\ evaluate gene-string
	previous
    ELSE
	wake-me-xt @ EXECUTE		\ triggers the life action
    THEN				( addr-of-cost )

    1 age +!				\ encreases the counter

[ future-change-individal ] [IF]	\ copy quality changes individually?
    \ can cells change qualities of the future?
    future-quality-change @ IF copy-qualities2future THEN
[THEN]
;

: ?show-nuc ( -- )
    spot-display-on? 0= IF  EXIT  THEN

    set-colors				\ I do like colors
    show-me-xt @ EXECUTE ;		\ show up

\ Do a life step in 'darvinistic' 'pseudo-biological' 'eat&consume' brew0 mode.
\ Population is controlled by getting energy from (scored) eating of food,
\ and paying taxes. Reproduction and death are triggered by energy level
\ (and age) during this individual life step.
: nuc-do-&-live ( -- )	\ 'eat&consume' 'darvinistic' 'biological' mode
    ?die					\ maybe die
    cp@ IF					\ still alife?
	1 living +!				\ count living cells

	wake-nuc
	eat-and-pay
	?show-nuc

	s" after:	" ?log-nuc-&-spot-state

	?reproduce				\ maybe reproduce

    ELSE					\ must show background if dead
	spot-display-on? IF
	    (background-off) @ IF
		1 (background-skipped) +!	\ count skipped empty spots
	    ELSE
		show-background
	    THEN
	THEN
    THEN ;


\ Elitistic mode:

\ Different implementation of the basic cycle and of population control:
\ During a life step each cell is scored and a sorted score-list is built.

\ Reproduction and death does not happen individually at each cells life step,
\ but happens collectively after one life step of *all* individuals, based on
\ the score-list.

: scoring-xt-UNDEFINED ( -- dummy=0 )
    bell  cr ." scoring-xt is undefined!  "  wait
    0 ;	\ return dummy

VARIABLE scoring-xt		' scoring-xt-UNDEFINED scoring-xt !
\ (planed to be used in both (eat&consume and elitistic) mode).
\ Planed to be switchable between nuc-var, world-var and global variable.

\ Get scoring *without any taxes*:
: scoring ( -- score )   scoring-xt @ EXECUTE ;

: code-tax ( -- tax )   code-cost @  code-price-scale 2@ */ ;

\ Score value after code length penalty (gain):
\ (Other code related penaltys, like nuc var usage, could be added here).
: score ( -- score )   scoring code-tax - ;

\ Score a nuc and insert it's (negative) score and it's location (spot)
\ into  score-list .  To have the fittest cell *first* in the list,
\ I use the *negative* score as sort key.
: score-and-list ( -- )   spot @  score negate  score-list @  2-insert-sorted ;

: |score-and-list| ( -- )	\ same, reporting
    score-and-list
    log-mask @  [ log-meal log-costs or ] literal and 0= IF  EXIT  THEN

    log-cat-id
    s"  scoring:"	cat-log
    scoring
    dup num>string	cat-log
    s"  - code-tax:"	cat-log
    code-tax
    dup num>string	cat-log
    s"  ="		cat-log
    - num>string	0 log-it ;

: nuc-do-&-list ( -- )		\ used when  elitism?  is on.
    1 living +!			\ count living cells
    wake-nuc
    ?show-nuc
    |score-and-list|
    s" after:	" ?log-nuc-&-spot-state ;

\ 'nuc-do-all' is the main routine for doing a cells action
: nuc-do-all ( -- )

\      \ depth control to catch bugs:
\      depth brew-depth <> IF
\  	bell		\ Thats not tolerated.  Warn the user.
\  	cr ." stack violation before waking nuc"  .s

\  	log-mask @ IF	\ Error logged if *anything* else get's logged.
\  	    s" stack violation (before) ID:"	cat-log
\  	    id @ num>string		cat-log
\  	    s"  depth="			cat-log
\  	    depth num>string		cat-log
\  	    depth IF
\  		s"   tos="		cat-log
\  		dup num>string	cat-log
\  	    THEN
\  	    s" "			0 log-it
\  	THEN
\      THEN

    elitism? IF		\ maybe better:	nuc-do-all-xt @ EXECUTE
	nuc-do-&-list
    ELSE
	nuc-do-&-live
    THEN

    \ depth control to catch bugs:
    depth brew-depth <> IF
	bell		\ Thats not tolerated.  Warn the user.
	cr ." stack violation after waking nuc: "  .s
	cr ." instead of " brew-depth .
	cr

	log-mask @ IF	\ Error logged if *anything* else get's logged.
	    s" stack violation (after) ID:"	cat-log
	    id @ num>string		cat-log
	    s"  depth="			cat-log
	    depth num>string		cat-log
	    depth IF
		s"   tos="		cat-log
		dup num>string	cat-log
	    THEN
	    s" "			0 log-it
	    \ log-stack		#####################
	THEN
    THEN ;

\ ****************************************************************
\ end	day in the life



\ ****************************************************************
\ *************************  Sow nucs:  **************************
\ ****************************************************************

DEFER <diversify>	' noop IS <diversify>

defer ?record-cloned
FALSE [IF] \ new version didn't bring much.

VARIABLE (sow-diversified)	(sow-diversified) off
defer ?record-sow ( n -- )
: sow ( n -- n' )	\ sow n clones of the actual nuc into the field
\ like in real life sow does *not* insist if the spot is not ok
\ it sows n seeds, and dosn't care, if they do not fall on fertile ground
\ it returns n', the number of succesfully sowed nucs
    this-world 0= IF
	." sorry, no gardening before the big bang " cr	drop false EXIT
    THEN
    dup 0> IF
	dup ?record-sow
	dup >r				\ ( R: n )
	dup 0 DO			( n )	\ try it n times, no more
	    spots random-ranged		( n i )			\ random spot
	    future			\ we're working always for the future
	    dup set-field-element	( n i flag=addr )   \ set if possible
	    dup IF					\ yes?
		cp@ >r   cp!	( n i  r: old-cp@ )
		?increase-genome-probability

		new-id id !
		(sow-diversified) @ IF  <diversify>  THEN

		\ to avoid troubles in record files we set at present too
		\ we don't need a time-step any more
		present	someone-here?		\ think this shouldn't happen
		ABORT" sow: Spot occupied at present, free at future."	\ !
		cp@ fcp !

		r> cp!
		1-				\ count decreasing n
	    ELSE 2drop THEN	( n' )
	LOOP	( n' )
	present				\ back in the present
	r> swap -			\ calculate success number
	(sow-diversified) off
	nucs-not-scanned
	?record-cloned			\ fix assertion of benchmarks results
    ELSE drop false THEN ;		\ n < 1, error

[ELSE] \ old
VARIABLE (sow-diversified)	(sow-diversified) off
defer ?record-sow ( n -- )
: sow ( n -- n' )	\ sow n clones of the actual nuc into the field
\ like in real life sow does *not* insist if the spot is not ok
\ it sows n seeds, and dosn't care, if they do not fall on fertile ground
\ it returns n', the number of succesfully sowed nucs
    this-world 0= IF
	." sorry, no gardening before the big bang " cr	drop false EXIT
    THEN
    dup 0> IF
	dup ?record-sow
	dup >r				\ ( R: n )
	future				\ we're working always for the future
	dup 0 DO			( n )	\ try it n times, no more
	    spots random-ranged	( n i )		\ random spot
	    set-field-element	( n flag )	\ set if possible
	    dup IF				\ yes?
		cp@ >r   cp!	( n   r: old-cp@ )
		?increase-genome-probability
		new-id id !
		(sow-diversified) @ IF
		    <diversify>
		THEN

		r> cp!
		1-				\ count decreasing n
	    ELSE drop THEN	( n' )
	LOOP	( n' )
	present				\ back in the present
	r> swap -			\ calculate success number
	(sow-diversified) off
	nucs-not-scanned
	?record-cloned			\ fix assertion of benchmarks results
    ELSE drop false THEN ;		\ n < 1, error
[THEN]

: sow-diversified ( n -- n' )	(sow-diversified) on	sow ;

: sow-some ( diversify? -- )
    page ." How many do you want to be sown? "
    0 num-in IF
	swap IF			\ diversify?
	    sow-diversified
	ELSE
	    sow
	THEN
	drop
	time-step
    THEN ;

\ To give meaningful output with 'show-key-bindings'
: sow-some-clones ( -- )   false sow-some ;
: sow-some-diversified ( -- )  true sow-some ;

\ ****************************************************************
\ end	sow



\ ****************************************************************
\ **************  Reproduction (2):  cell-division  **************
\ ****************************************************************

\ cell division with possible diversification and mutation of one part
\ DEFER <mutate>
\ ' noop IS <mutate>
VARIABLE cell-division-moves-both	cell-division-moves-both off
VARIABLE cell-division-diversify-both	cell-division-diversify-both off
VARIABLE cell-division-mutate-both	cell-division-mutate-both off
: cell-division ( -- )		\ mimics cell division in biology
    free-neighbour-spot?		( i' true | false )
    0= IF  EXIT  THEN		\ :-( no adjacent spot free, done

    \ If somebody else wants to go here too, let's be kind and let him...
    dup future someone-here? present IF  drop EXIT  THEN

    energy @ 2/ energy !	\ share energies
    age dup @ >r off		\ remember age and reset it to zero
\   nuc-error dup @ >r off
    1 generation +!
    1 genome-generation +!
    future ( i ) set-field-element present	\ ( addr=flag )

    ?dup IF     ( addr)		\ :-)   congratulations!

	\ log birth?
	log-mask @ IF
	    log-cat-id
	    s" : reproduction: cell division" log-birth log-it
	THEN

	cp@ >r			\ remember actual cell
	cp!			\ switch to the newborn child
	<mutate>		\ mutate it?
	<diversify>		\ diversify?
	new-id id !
	?increase-genome-probability
	log-mask @ IF
	    s" child as "		cat-log   log-cat-id
	    s"  to spot "		cat-log
	    child-spot @ num>string	log-birth log-it
	THEN
	r> cp!			\ restore cp
	rdrop			\ forget the age, I'm new born :)
\	rdrop			\ forgive errors

	\ Move mother cell?
	cell-division-moves-both @ IF	\ move mother cell too?
	    world-mode? IF
		free-neighbour-spot? IF	\ ( i')
		    dup future someone-here? present 0= IF
			future ( i ) set-field-element present \ a=flag
			IF     		\ :-)   congratulations!
			    nuc-unlink	\ remove at old spot
			THEN
		    ELSE drop
		    THEN
		THEN
	    THEN
	THEN

	cell-division-mutate-both @ IF
	    <mutate>
	THEN
	cell-division-diversify-both @ IF
	    <diversify>
	THEN
	?increase-genome-probability

	EXIT
    ELSE 		\ :-(	cloning didn't work, reset values

	energy @ 2* energy !	\ disregarding rounding errors
\	r> nuc-error !
	r> age !
	-1 generation +!
	-1 genome-generation +!
    THEN ;

' cell-division reproduce-actions >list

\ ****************************************************************
\ end	reproduction (2)


\ ****************************************************************
\ ********************  Global variables  ************************
\ ****************************************************************

\ For many experiments it seems a bit exagerated to use spot local
\ variables, so I introduce global variables.

LIST: global-int-variables

: define-global-int-vars ( u -- )
    s" VARIABLE integer-"
    rot 0 ?DO
	2dup string!!
	[char] A i + over char-cat
	dup string@ EVALUATE
	dup string@ 9 /string get-xt global-int-variables >list
	stringbuf-close
    LOOP
    2drop ;

global-integer-variables# define-global-int-vars

LIST: global-dfloat-variables

: define-global-dfloat-vars ( u -- )
    s" dfVARIABLE dfloat-"
    rot 0 ?DO
	2dup string!!
	[char] A i + over char-cat
	dup string@ EVALUATE
	dup string@ 11 /string get-xt global-dfloat-variables >list
	stringbuf-close
    LOOP
    2drop ;

global-dfloat-variables# define-global-dfloat-vars


MENU: global-variables-men
VARIABLE (menu-global-vars-show-dfloats)
(menu-global-vars-show-dfloats) on

DEFER |menu-diversify-global-vars|

: all-dfloat-globals-equal ( -- )	\ set them all to the value of the 1st
    global-dfloat-variables# >r
    r@ 2 < IF  rdrop EXIT  THEN

    global-dfloat-variables next-node
    dup @ EXECUTE df@
    r> 1 DO
	next-node
	fdup  dup @ EXECUTE df!
    LOOP
    drop fdrop ;

: all-dfloat-integers-equal ( -- )	\ set them all to the value of the 1st
    global-integer-variables# >r
    r@ 2 < IF  rdrop EXIT  THEN

    global-int-variables next-node
    dup @ EXECUTE @ swap
    r> 1 DO
	next-node
	2dup @ EXECUTE !
    LOOP
    2drop ;

: .menu-global-variables ( -- )
    help-node" Menu global variables"

    page
    s" Menu global variables:" menu-title-entry

    from-here ." Showing global "
    (menu-global-vars-show-dfloats) @
    IF s" FLOAT"  ELSE s" INTEGER"  THEN type-bright
    s"  variables."  ['] (menu-global-vars-show-dfloats) >stack	redisplay
    ['] toggle-named	 menu-entry cr
    s" otifIF" menu-same-key-entry

    7 keep-but-scroll-rest

    cr
    (menu-global-vars-show-dfloats) @ IF
	global-dfloat-variables
	dup nodes
	dup IF
	    0 scrolled-range DO
		i over n'th-node @
		from-here  dup xt>string  2dup + 1- c@ >r  type 
		1 6 screen-column
		s" "   rot	simple-dfloat-variable-entry
		r> #key-same-entry

		i 0= IF
		    1 2 screen-column
		    s" set them all to this value"	redisplay
		    ['] all-dfloat-globals-equal	menu-entry
		    [char] = #key-same-entry
		THEN
		cr
	    LOOP
	ELSE
	    drop
	    s" No global dfloat variables defined." type-other-colour cr
	    s" Set 'global-dfloat-variables#' in file 'my-compile-options.fs'."
	    type-other-colour cr
	THEN
	drop
    ELSE
	global-int-variables
	dup nodes
	dup IF
	    0 scrolled-range DO
		i over n'th-node @
		from-here  dup xt>string  2dup + 1- c@ >r  type 
		1 6 screen-column
		s" "   rot	simple-menu-entry-variable
		r> #key-same-entry

		i 0= IF
		    1 2 screen-column
		    s" set them all to this value"	redisplay
		    ['] all-dfloat-integers-equal	menu-entry
		    [char] = #key-same-entry
		THEN
		cr
	    LOOP
	ELSE
	    drop
	    s" No global integer variables defined." type-other-colour cr
	    s" Set 'global-integer-variables#' in file 'my-compile-options.fs'."
	    type-other-colour cr
	THEN
	drop
    THEN

    cr
    s" Diversification of global variables."	redisplay
    ['] |menu-diversify-global-vars|		menu-entry cr
    s" dDgG" menu-same-key-entry

    <common-menu-entries> ;

: menu-global-variables ( -- )
    global-variables-men
    ['] .menu-global-variables menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default

    do-menu-loop
    free-menus ;
' menu-global-variables function-key-actions >list

\ ****************************************************************
\ end	Global variables


\ ****************************************************************
\ *********  Loading genes and adapting probabilities:  **********
\ ****************************************************************

INCLUDE gene-alternatives.fs

\ s" conditionals.fs"		include-genes
s" basic-stack.fs"		include-genes
s" basic-arithmetics.fs"	include-genes
s" fetch.fs"			include-genes

TRUE [IF] \ just a quick fix to deal with overflow.  not more.
    s" store-normalised.fs"	include-genes
    s" store.fs"		include-genes
[ELSE] \ overflow is nice, the cells like it ;-)
    s" store.fs"		include-genes
[THEN]

s" off.fs"		include-genes

s" organs.fs"		include-genes
s" nuc-parameters.fs"	include-genes
s" qualities.fs"	include-genes
s" spot-properties.fs"	include-genes
s" global-integers.fs"	include-genes
s" global-dfloats.fs"	include-genes
s" insight.fs"		include-genes

nuc-floats# [IF]
    s" float-organs.fs"		include-genes
    s" float-parameters.fs"	include-genes
[THEN]
spot-floats# [IF]
    s" spot-df-qualities.fs"	include-genes
    s" spot-df-properties.fs"	include-genes
[THEN]
nuc-floats# spot-floats# + [IF]
    s" dfloat-fetch.fs"		include-genes
    s" dfloat-store.fs"		include-genes
    s" float-stack.fs"		include-genes
    s" float-basic-arithmetics.fs"	include-genes
    s" float-more-arithmetics.fs"	include-genes
    s" float-exp.fs"		include-genes
    s" float-trigonometry.fs"	include-genes
    s" mixed-maths.fs"		include-genes
    s" transform.fs"		include-genes
    s" float-comparison.fs"	include-genes
[THEN]

0 current-genome-pool-xt @ actual-genepool-xt @ execute set-as-sublist

: more-n-consumer-genes ( +n -- )	\ puts 11*n genes in the pool
    0 ?DO
	1000 [internal'] !(some)  	actual-genepool-xt @ execute change-one
	1000 [internal'] +!(some) 	actual-genepool-xt @ execute change-one
	1000 [internal'] -!(some) 	actual-genepool-xt @ execute change-one
	2000 [internal'] +		actual-genepool-xt @ execute change-one
	2000 [internal'] -		actual-genepool-xt @ execute change-one
	2000 [internal'] *		actual-genepool-xt @ execute change-one
	2000 [internal'] ?/		actual-genepool-xt @ execute change-one
    LOOP ;

\ quick hack to get balanced stack type probabilities:
nuc-parameters# more-n-consumer-genes
spot-properties# more-n-consumer-genes
3 more-n-consumer-genes		\ for insight.fs

nuc-floats# spot-floats# + [IF]
    : more-r-consumer-genes ( +n -- )	\ puts 11*n genes in the pool
    0 ?DO
	1000 [internal'] df!  	actual-genepool-xt @ execute change-one
	 750 [internal'] df+!  	actual-genepool-xt @ execute change-one
	 750 [internal'] df-!  	actual-genepool-xt @ execute change-one
	1500 [internal'] f+  	actual-genepool-xt @ execute change-one
	1500 [internal'] f-  	actual-genepool-xt @ execute change-one
	1500 [internal'] f*  	actual-genepool-xt @ execute change-one
	1500 [internal'] f/  	actual-genepool-xt @ execute change-one
	1000 [internal'] fmax  	actual-genepool-xt @ execute change-one
	1000 [internal'] fmin  	actual-genepool-xt @ execute change-one
	 500 [internal'] f>s  	actual-genepool-xt @ execute change-one
    LOOP ;

\ Quick hack to get balanced stack type probabilities:
nuc-f-parameters# more-r-consumer-genes
spot-f-properties# more-r-consumer-genes
spot-f-properties# more-r-consumer-genes

[THEN]

\ ****************************************************************
\ end	genes and probabilities



\ hmm, let's prepare some food:
\ ****************************************************************
\ **************************  Food:  *****************************
\ ****************************************************************

\ DEFER <food> ( -- addr )		\ gives addr where the food is

: all-score ( -- dummy=0 )   0 ;

: eat-all ( -- )  <food> @ energy +!  <food> off ;
' all-score  ' eat-all  eat-actions 2>list

nuc-organs# [IF] \ at least organ-A defined.

: score-A ( -- score )   organ-A @  0 max ;

: eat-part ( -- )
    <food> @ organ-A @ min	\ doesn't eat more than organ-A
    0 max			\ no vomiting (one could leave this out)
    dup energy +!
    negate <food> +! ;
' score-A  ' eat-part  eat-actions 2>list

[THEN] \ at least organ-A defined.

\ food that's placed on the spot not taking into account, if there's somebody:
VARIABLE food-share/spot	100 food-share/spot !
: feed-spot ( -- )  food-share/spot @ <food> +! ; \ put on spot or in the bowl

: feed-world ( -- )		\ give food everywhere
    spots 0 DO i >spot! feed-spot LOOP ;

defer ?record-feed-world
: |feed-world| ( -- )	\ version for user invocation, possibly recording.
    ?record-feed-world
    feed-world ;
' |feed-world| function-key-actions >list

\ food that's fed to all living cells, but not put on empty spots:
VARIABLE individual-fixed-food-share	individual-fixed-food-share off

\ food that's shared between all living beings in one world
\ (will be compiled in the world's section)
\ is good for population control
\ the amount will be *added* to the other sources of food
\ and be put in the following variable:
VARIABLE food-common-share			\ how much each one gets

: feed-individual ( -- )
    individual-fixed-food-share @
    food-common-share @   +
    <food> +! ;

\ World food supply:
\ food that's shared between all living beings in one world
\ (will soon be made local to the world we're in)
\ that's good for population control
\ the amount will be *added* to the other sources of food

VARIABLE world-food-supply	100000 world-food-supply !
\ this supply will be divided by the number of living cells and be put
\ into the following variable:
\ VARIABLE food-common-share			\ how much each one gets
: determine-food-share ( -- )			\ sets food-common-share
    living @ 0= IF
	spots free-spots - living ! 
    THEN
    living @ IF
	world-food-supply @  living @  /  food-common-share !
    \ ELSE					\ don't know what to do here
    THEN ;

\ food-menu comes later on.

2VARIABLE score-rate	1 1 score-rate 2!	\ scoring gets rated by this

: eat-scored-logged ( +error -- )	\ log-mask checked already
    s" eat-scored:	error: "	cat-log
    dup num>string			cat-log
    score-rate 2@ */ >r
    s" 	rated: "			cat-log
    r@ num>string			cat-log
    <food> @	( food  r: rated-+error )
    s" 	food: "				cat-log
    dup num>string			cat-log
    dup r> -	( food earned )
    10 max	( food 10-or-more )
    min		( real-available-share )
    s" 	feed: "				cat-log
    dup num>string			0 log-it
    dup energy +!	\ feed the cell
    negate <food> +! ;	\ this much was eaten away

: eat-scored ( +error -- )
\   dup nuc-error +!
    log-meal? IF eat-scored-logged EXIT THEN
    score-rate 2@ */ >r
    <food> @	( food  r: rated-+error )
    dup r> -	( food earned )
    10 max	( food 10-or-more )
    min		( real-available-share )
    dup energy +!	\ feed the cell
    negate <food> +! ;	\ this much was eaten away

\ ****************************************************************
\ end	food



\ ****************************************************************
\ *********  Do something on the worlds inhabitants:  ************
\ ****************************************************************

\ do something with every living cell in the world (sequential):
false VALUE (do-xt)
: do-with-everybody ( xt -- )
    to (do-xt)
    spot @ >r				\ maybe it's worth saving?
    cp@ >r				\ ditto
    spots 0 DO				\ loop over all spots
	i someone-here? dup IF		\ inhabited?
	    cp!				\ yes: set temporally as actual
	    i >spot!			\      same for the spot
	    (do-xt) EXECUTE		\      and do your job
	ELSE drop THEN
    LOOP
    r> cp!				\ restore everything
    r> >spot! ;				\ you never know what's good for...

: do-with-random-nucs ( u xt -- )	\ we try it on u spots
    to (do-xt)
    spot @ >r				\ maybe it's worth saving?
    cp@ >r				\ ditto
    0 ?DO				\ loop
	spots random-ranged		\ random spot
	dup someone-here? dup IF		\ inhabited?
	    cp!				\ yes: set temporally as actual
	    dup >spot!			\      same for the spot
	    (do-xt) EXECUTE		\      and do your job
	ELSE drop THEN drop
    LOOP
    r> cp!				\ restore everything
    r> >spot! ;				\ you never know what's good for...

: count-living ( -- n )   0  ['] 1+ do-with-everybody ;


\ Count on all living cells how many times a condition is true
\ Stack effect:  test-xt EXECUTE ( -- flag )
\
\ Do one single test and increase counter on success:
: (test&count) ( counter test-xt -- counter' )
    dup EXECUTE
    IF swap 1+ swap THEN ;

: test-and-count-everybody ( test-xt -- count )
    0 swap	( counter test-xt )
    ['] (test&count) do-with-everybody
    drop ;

false [IF] \ not used yet
    : test-and-count-everywhere ( test-xt -- count )
	0 swap	( counter test-xt )
	['] (test&count) do-everywhere
	drop ;
[THEN]


\ User interface to do quite arbitrary stuff on selected conditions:
\ (Used also for colouring, sometimes).
INCLUDE maybe-do.fs

DEFER (brew-redisplay)
: show-fg-bg-coloured ( xxx>fg-color-xt  xxx>bg-color-xt -- )
    2>r

    foreground-color-xt @
    background-color-xt @
    display-switch @

    2r@  background-color-xt !  foreground-color-xt !	\ set color xt's
    spot-display-on					\ build display-switch
    r> ['] default-color <> IF
	spot-background-coloring or
    THEN
    r> ['] default-color <> IF
	spot-foreground-coloring or
    THEN
    display-switch !

    (brew-redisplay)

    display-switch !
    background-color-xt !
    foreground-color-xt ! ; 

: show-fg-coloured ( xxx>fg-color-xt -- )
    ['] default-color show-fg-bg-coloured ;

: show-bg-coloured ( xxx>bg-color-xt -- )
    ['] default-color swap show-fg-bg-coloured ;

: generic-hit>fg-color ( -- col )	\ dependent on active maybe-do-field
    generic-maybe? IF
	color-selected-fg-xt @ EXECUTE  EXIT
    THEN
    color-miss-fg-xt @ EXECUTE ;

: generic-range>fg-color ( -- col )  \ dependent on active maybe-do-field
    (maybe-do-type-xt) @  ['] maybe-do = IF
	(expression-xt) @ CASE
	    ['] variable-within OF
		(expr-xt-1) @ EXECUTE @ dup
		(expr-parameter) @  (expr-parameter-2) @  within IF
		    drop color-selected-fg-xt @ EXECUTE  EXIT
		THEN
		(expr-parameter) @ < IF
		    color-below-fg-xt @ EXECUTE  EXIT
		THEN
		color-above-fg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] function-within OF
		(expr-xt-1) @ EXECUTE dup
		(expr-parameter) @  (expr-parameter-2) @  within IF
		    drop color-selected-fg-xt @ EXECUTE  EXIT
		THEN
		(expr-parameter) @ < IF
		    color-below-fg-xt @ EXECUTE  EXIT
		THEN
		color-above-fg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] variable-number OF
		(expr-xt-1) @ EXECUTE @
		dup (expr-parameter) @ < IF
		    drop
		    color-below-fg-xt @ EXECUTE  EXIT
		THEN
		(expr-parameter) @ > IF
		    color-above-fg-xt @ EXECUTE  EXIT
		THEN
		color-selected-fg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] function-number OF
		(expr-xt-1) @ EXECUTE
		dup (expr-parameter) @ < IF
		    drop
		    color-below-fg-xt @ EXECUTE  EXIT
		THEN
		(expr-parameter) @ > IF
		    color-above-fg-xt @ EXECUTE  EXIT
		THEN
		color-selected-fg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] 2-variables OF
		(expr-xt-1) @ EXECUTE @  (expr-xt-2) @ EXECUTE @ -
		dup 0= IF
		    drop color-selected-fg-xt @ EXECUTE		EXIT
		THEN
		0< IF
		    color-below-fg-xt @ EXECUTE   EXIT
		THEN
		color-above-fg-xt @ EXECUTE   EXIT
	    ENDOF
	    ['] df-variable-within OF
		(expr-df-xt-1) @ EXECUTE df@ fdup
		(expr-df-parameter) df@  (expr-df-parameter-2) df@  fwithin IF
		    fdrop color-selected-fg-xt @ EXECUTE  EXIT
		THEN
		(expr-df-parameter) df@ f< IF
		    color-below-fg-xt @ EXECUTE  EXIT
		THEN
		color-above-fg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] df-variable-number OF
		(expr-df-xt-1) @ EXECUTE df@
		fdup (expr-df-parameter) df@ f< IF
		    fdrop
		    color-below-fg-xt @ EXECUTE  EXIT
		THEN
		(expr-df-parameter) df@ f> IF
		    color-above-fg-xt @ EXECUTE  EXIT
		THEN
		color-selected-fg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] df-function-within OF
		(expr-df-xt-1) @ EXECUTE fdup
		(expr-df-parameter) df@  (expr-df-parameter-2) df@  fwithin IF
		    fdrop color-selected-fg-xt @ EXECUTE  EXIT
		THEN
		(expr-df-parameter) df@ f< IF
		    color-below-fg-xt @ EXECUTE  EXIT
		THEN
		color-above-fg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] df-function-number OF
		(expr-df-xt-1) @ EXECUTE
		fdup (expr-df-parameter) df@ f< IF
		    fdrop
		    color-below-fg-xt @ EXECUTE  EXIT
		THEN
		(expr-df-parameter) df@ f> IF
		    color-above-fg-xt @ EXECUTE  EXIT
		THEN
		color-selected-fg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] 2-df-variables OF
		(expr-df-xt-1) @ EXECUTE df@  (expr-df-xt-2) @ EXECUTE df@ f-
		fdup f0= IF
		    fdrop color-selected-fg-xt @ EXECUTE		EXIT
		THEN
		f0< IF
		    color-below-fg-xt @ EXECUTE   EXIT
		THEN
		color-above-fg-xt @ EXECUTE   EXIT
	    ENDOF

	    bell cr ." generic-range>fg-color: unknown (expression-xt) "
	    xt>string type
	    ABORT
	ENDCASE
    THEN

    cr bell ." generic-range>fg-color: Wrong (maybe-do-type-xt): "
    xt>string type ABORT ;

: coloured-on-range-possible? ( -- flag )
    (maybe-do-type-xt) @  ['] maybe-do <> IF FALSE EXIT THEN

    (expression-xt) @ CASE
	['] variable-within	OF  TRUE EXIT  ENDOF
	['] variable-number	OF  TRUE EXIT  ENDOF
	['] function-number	OF  TRUE EXIT  ENDOF
	['] function-within	OF  TRUE EXIT  ENDOF
	['] df-variable-within	OF  TRUE EXIT  ENDOF
	['] df-variable-number	OF  TRUE EXIT  ENDOF
	['] df-function-within	OF  TRUE EXIT  ENDOF
	['] df-function-number	OF  TRUE EXIT  ENDOF
	['] 2-variables    OF (condition-xt) @ ['] =  = IF TRUE EXIT THEN ENDOF
	['] 2-df-variables OF (condition-xt) @ ['] f= = IF TRUE EXIT THEN ENDOF
    ENDCASE
    FALSE ;

: show-fg-coloured-on-hit ( maybe-do-field-xt -- )
    EXECUTE
    c-l stringbuf-open >r
    s" Showing nucs with "		r@ cat
    maybe-generic-string dup string@	r@ cat
    stringbuf-close
    ['] generic-hit>fg-color  show-fg-coloured
    last-left r@ string@ ?type
    color-selected-fg-xt @ EXECUTE color-foreground s"   hit  " ?type
    color-miss-fg-xt @ EXECUTE     color-foreground s" miss" ?type
    default-foreground clear-line-to-end

    r> stringbuf-close ;

: show-fg-coloured-on-range ( maybe-do-field-xt -- )
    EXECUTE
    c-l stringbuf-open >r
    s" Showing nucs with "		r@ cat
    maybe-generic-string dup string@	r@ cat
    stringbuf-close
    ['] generic-range>fg-color  show-fg-coloured
    last-left r@ string@ ?type
    color-below-fg-xt @ EXECUTE    color-foreground s"   below" ?type
    color-selected-fg-xt @ EXECUTE color-foreground s"   WITHIN" ?type
    color-above-fg-xt @ EXECUTE    color-foreground s"   above" ?type
    default-foreground clear-line-to-end
    r> stringbuf-close ;


\ ****************************************************************
\ end	do on inhabitants



\ ****************************************************************
\ **************************  Menus:  ****************************
\ ****************************************************************

VARIABLE (continuous-column)	(continuous-column) off	\ global
\ to prevent menu contents (including spot display) and such to override
\ continuous display space.
\ 'continuous-display-used' was introduced to prevent the changed
\ (continuous-column) to get recorded, when not needed.
: ?reset-continuous-column ( -- )
    continuous-display-used? IF
	(continuous-column) off		\ get screen for cont display cleared
    THEN ;

: (common-menu-entries) ( -- )
    [char] !  redisplay	['] do-FORTH			#key-menu-entry
    [char] k  redisplay	['] show-key-bindings		#key-menu-entry
    [char] q  menu-done	['] quit-menu			#key-menu-entry
    [char] ?  redisplay	['] context-help		#key-menu-entry
    [char] `  redisplay	['] toggle-highlite-active	#key-menu-entry
    F1%	      redisplay	['] context-help		#key-menu-entry ;

\ : common-menu-entries ( -- )
:NONAME ( -- )
    (common-menu-entries)
    9			['] goto-next-menu-item 	#key-menu-entry
    ['] redisplay default-function-keys
    .menu-short-help

    \ It's a convenient place to do the following trick
    ?reset-continuous-column ; IS <common-menu-entries>

: .ok-done ( -- )
    page
    mid-screen  s" OK, done." type-other-colour
    200 wait-until ;

\ ****************************************************************
\ end	menus



\ ****************************************************************
\ *****************  edit-probabilities-menu  ********************
\ ****************************************************************

MENU: edit-probabilities-men

\ we must be able to call sublists recursively:
DEFER edit-probabilities-menu ( list-xt -- )
VARIABLE edit-probabilities-nesting	edit-probabilities-nesting off
: nest-probabilities-menu ( list-xt -- )  \ calling sublists
    1 edit-probabilities-nesting +!
    edit-probabilities-menu ;

VARIABLE (see)		(see) on	\ can you 'SEE' the items definition?

: .edit-probabilities-menu ( list-xt -- a-o-p-t-p-xt)
    dup EXECUTE
    page
    .menu-title

    cr
    4 keep-but-scroll-rest

    dup @ how-many 0 scrolled-range ?DO
	dup @ i this-node >r
	s" " r@ >probability simple-menu-entry-value 
	1 10 screen-column
	r> >data @ dup xt>string
	(see) @ IF
	    third >stack  ['] <page-see>  menu-wait  redisplay	menu-entry 
	ELSE
	    type  up-to-here
	THEN
	drop

	dup @ i this-is-list? IF
	    1 2 screen-column
	    \ title is set in 'edit-probabilities-menu'
	    dup @ i this-data @ >stack	redisplay
	    s" * nested list * " ['] nest-probabilities-menu	menu-entry
	    s" n*" menu-same-key-entry
	THEN
	cr
    LOOP

    @ update
    <common-menu-entries> ;

\ : edit-probabilities-menu ( list-xt -- )
:NONAME ( list-xt -- )
    \ Try to guess 'help-node"' (needed after unnesting):
    dup CASE
	actual-genepool-xt @ OF
	    help-node" Actual pool menu"
	    (see) on
	ENDOF
	['] mutation-types OF
	    help-node" Mutation types menu"
	    (see) on			\ why not? ;-)
	ENDOF
	current-genome-pool-xt @ OF
	    help-node" Genome pool"
	    (see) on
	ENDOF
    ENDCASE

    \ set proper menu title:
    c-l stringbuf-open >r
    dup xt>string			r@ string!
    s"   items: "			r@ cat
    dup EXECUTE @ how-many num>string	r@ cat
    edit-probabilities-nesting @ IF
	s"     Sublist: edit relative probabilities."
    ELSE
	s"     Edit relative probabilities."
    THEN				r@ cat
    r@ string@ menu-title!
    menu-selected >menu-any-data 2@ (title) 2!

    edit-probabilities-men
    ['] .edit-probabilities-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default

    do-menu-loop drop
    r> stringbuf-close

    edit-probabilities-nesting @ IF	\ recursion: coming back from sublist
	-1 edit-probabilities-nesting +!

	\ now call the menu again on prior level:
	dup edit-probabilities-menu
    THEN
    \ free-menus ;
; IS edit-probabilities-menu ( list-xt -- )

\ ****************************************************************
\ end	edit-probabilities-menu



\ ****************************************************************
\ *********************  Scanning nucs:  *************************
\ ****************************************************************

\ This simple menu is used to actually display a scan and give
\ menu interface to a few functions.

MENU: scan-display-men
: .scan-display-menu ( display-xt -- display-xt )
    help-node" ASCII bar graphics"
    at? 2>r
    page
    dup EXECUTE
    last-left s" " menu-done noop-entry  last-right up-to-here

    (common-menu-entries)		\ some basic functionality

    2r> at-xy ;


DEFER bar-ranged-subset ( column-low column-high -- )	\ both *inclusive*

: scan-display-reaction ( -- )
    mousek@ 2drop	( column )
    dup bar-ranged-subset ;	\ just one bar

: scan-display-menu ( display-xt -- )
    scan-display-men
    ['] noop clear-screen-xt !
    ['] .scan-display-menu menu-display-xt !	\ display-xt will be on stack
    menu-done	['] noop	menu-key-default
    ['] scan-display-reaction	menu-default
    c-l 2/  last-line 1-  at-xy
    do-menu-loop

    drop ;

\ ****************************************************************



[UNDEFINED] max-step-display-items# [IF]
    12 CONSTANT max-step-display-items#
[THEN]
VARIABLE (scan-index)	0 (scan-index) !	\ index of actual scan display

: scan-ARRAY: ( -- )
    CREATE
	max-step-display-items# cells allot
    DOES> ( -- addr )
	(scan-index) @ cells + ;

: scan-2ARRAY: ( -- )
    CREATE
	max-step-display-items# 2* cells allot
    DOES> ( -- addr )
	(scan-index) @ 2* cells + ;

\ Scan array of dfaligned records of +n bytes length:
: dfaligned-scan-n-ARRAY: ( +n -- )	\ record size in bytes
    CREATE
	dfaligned >r	( r: dfaligned-record-size )
	r@ ,
	here cell+ dfaligned ,
	r> max-step-display-items# * allot
    DOES> ( -- addr )
	2@ (scan-index) @ * + ;

scan-ARRAY: (scan-xt)			\ display xt or zero
scan-ARRAY: (scan-detail)		\ detail to be displayed:
					\ index of nuc/spot vars,
					\ xt of functions.
scan-ARRAY: (scan-lines)		\ displayed lines including status line
scan-2ARRAY: (scan-min-max)		\ integer scans only
scan-2ARRAY: (last-scan-min-max)	\ integer scans only
float-check-field-length# dfaligned-scan-n-ARRAY: (dfloat-check-data)	\ float
float-check-field-length# dfaligned-scan-n-ARRAY: (last-dfloat-check-data) \ "
scan-2ARRAY: horizontal-zoom-scale
scan-2ARRAY: vertical-zoom-scale
' vertical-zoom-scale is <vertical-zoom-scale>
scan-ARRAY: (vertical-display-range)
' (vertical-display-range) is <vertical-display-range>
scan-ARRAY: scan-background-xt
scan-ARRAY: scan-foreground-xt
scan-ARRAY: (scan-flags)		\ used in continuous-display

\ Masks for '(scan-flags)'	one for a displayed *set* of parameters
LIST: scan-flags
scan-flags 0
LISTED-MASK: cont-scan-nucs
LISTED-MASK: cont-scan-spots
LISTED-MASK: fixed-horizontal-range
LISTED-MASK: dfloat-scan
2drop

\ : scan-horizontal-zoom? ( -- flag )
:NONAME ( -- flag )
    (scan-flags) @ dup fixed-horizontal-range and
    0= IF  drop TRUE EXIT  THEN			\ range not fixed: zoom

    \ fixed range is disregarded if range is zero:
    dfloat-scan and IF				\ dfloats
	(last-dfloat-check-data)
	dup >dfloat-max df@  >dfloat-min df@  f=
	EXIT
    THEN
    
    (last-scan-min-max) 2@ =			\ integers
; IS scan-horizontal-zoom?

LIST: scan-background-colors
color-list scan-background-colors copy-simple-list-elements
LIST: scan-foreground-colors
color-list scan-foreground-colors copy-simple-list-elements

: init-scan-array ( -- )
    max-step-display-items# 0 DO
	i (scan-index) !
	default-horizontal-zoom-scale horizontal-zoom-scale 2!
	\ default-vertical-zoom-scale vertical-zoom-scale 2!
	['] default-color scan-foreground-xt !
	['] default-color scan-background-xt !
   LOOP ;
init-scan-array

\ startup defaults of first two indizes (for step scan display)
0 (scan-index) !
8 (scan-detail) !
l-s 1- 2/ (scan-lines) !
' blue scan-background-xt !
1 (scan-index) !
l-s 1- 2/ (scan-lines) !
' blue scan-background-xt !

VARIABLE (nuc-scan-data)	(nuc-scan-data) off	\ data pointer
\   scan-min			\ four pseudonuclei for data storage...
\   scan-max
\   2scan-sum	\ scanning: double sum	\ nuc-scan-average: computed results
\     Do *not* use nuc floats in the third pseudonucleus, they get overwritten
\     by the double integer sums!  The fourth are save for dfloat sums, though.
\   counts for -inf	a cell for each nuc dfloat variable
\   counts for real	a cell for each nuc dfloat variable
\   counts for +inf	a cell for each nuc dfloat variable
\   counts for nan	a cell for each nuc dfloat variable
VARIABLE nucs-scanned		nucs-scanned off	\ flag and counter
2VARIABLE nuc-i-scan-range				\ integer scanned range
nuc-floats# [IF]
    2VARIABLE nuc-df-scan-range				\ dfloat scanned range
[THEN]

: nuc-type-counter ( float-index float-type -- addr )
    1+ nuc-floats# *  + cells
    (nuc-scan-data) @  [ nuc-length# 4 * ] literal + + ;

: init-nuc-scan ( -- )
    \ first free previously allocated memory
    (nuc-scan-data) @ ?dup IF free ABORT" init-nuc-scan: Couldn't free." THEN

    nucs-scanned off
    \ We need 4 times nuc-length# for min, max and *double* sum.
    nuc-length# 4 *		\ see above
    nuc-floats# 4 * cells +	\ counts for -inf real +inf nan above 4 nucs
    new-nucleus		( length -- addr flag|false )
    0= IF  bell cr ." init-nuc-scan failed" 1200 ms  EXIT  THEN

    (nuc-scan-data) !
    nuc-scan-limit nuc-variables nuc-i-scan-range 2!

    \ Initialise max and min values:
    (nuc-scan-data) @
    nuc-i-scan-range 2@ DO
	highest-integer#  over i cells + >r r@ !	\ Initialise min
	lowest-integer#   r> nuc-length# +  !	\ Initialise max
    LOOP

[ nuc-floats# ] [IF]
    nuc-floats# 0  nuc-df-scan-range 2!

    \ Initialise max and min values:
    nuc-float-offset# +
    nuc-df-scan-range 2@ DO
	dup  i dfloats  + >r
	+infinity  r@  df!
	-infinity  r> nuc-length# +  df!
    LOOP
[THEN]
    drop

    trial off
    selected off ;

: scan-nuc ( -- )	\ scanning the actual nuc
    nuc-i-scan-range 2@ DO
	i nuc-addr @		\ read a nuc var in the current nuc
	(nuc-scan-data) @ i cells +	\ address of actual minimum
	dup >r
	@ over min r@ !		\ store new minimum
	r> nuc-length# + dup >r	\ address of actual maximum
	@ over max r> !		\ store new maximum
	s>d				\ convert to double
	(nuc-scan-data) @ nuc-length# 2* + \ start of summation area
	i cells 2* + dup >r		\ address of actual double sum
	2@ d+	r> 2!		\ double precision sum
    LOOP

[ nuc-floats# ] [IF]
    (nuc-scan-data) @  nuc-float-offset# +	\ assuming separate float stack
    nuc-df-scan-range 2@ DO
	i n'th-dfloat-nuc-var df@

	fdup float-type
	\ increase type count:
	dup 1+ nuc-floats# * i + cells
	(nuc-scan-data) @  [ nuc-length# 4 * ] literal + + +1!	\ count type

	\ add reals to sum in the fourth pseudonuc:
	( float-type ) real% = IF
	    fdup
	    dup  i dfloats + >r
	    fdup  r@ df@  fmin  r@ df!	\ store new min
	    fdup  r> nuc-length# + >r
	    r@ df@ fmax  r> df!		\ store new max
	    dup  [ nuc-length# 3 * ] literal +  i dfloats +
	    df+!				\ add to sum
	THEN
	fdrop
    LOOP
    drop
[THEN]

    nuc-flags @
    dup nuc-on-trial and IF  1 trial +!     THEN	\ on trial?
    nuc-is-selected  and IF  1 selected +!  THEN	\ selected?
    1 nucs-scanned +! ;			\ counter

: nuc-scan-average ( -- )
    nucs-scanned @ IF
	nuc-i-scan-range 2@ DO
	    (nuc-scan-data) @ nuc-length# 2* +  dup   \ start of summation area
	    i cells 2* +	2@		\ read double precision sum
	    nucs-scanned @  m/			\ divide by counter
	    swap i cells +	!		\ store as one cell
	LOOP		\ now we have a cell array with the averages

[ nuc-floats# ] [IF]
    (nuc-scan-data) @
    nuc-df-scan-range 2@ DO
	dup [ nuc-length# 4 * nuc-floats# cells + ] literal +	\ real counts
	i cells + @						\ real count
	over [ nuc-length# 3 * nuc-float-offset# + ] literal +  i dfloats + >r
	r@ df@
	( real-count ) s>f f/  r> nuc-length# - df!
    LOOP
    drop				\ now we have a df array of averages
[THEN]
    THEN ;

DEFER do-with-selected-nucs ( to-do-xt -- )
\ Don't use '(do-with-whom-xt)' directly,
\ use ['] do-with-selected-nucs scan-whom-xt! / reset-scan-whom and the like.
\ Possible values of (do-with-whom-xt):
\	' do-with-everybody	default value
\	' do-with-selected-nucs
\	' maybe-do-with-everybody
\	' simple-maybe-do-with-everybody
VARIABLE (do-with-whom-xt)	' do-with-everybody (do-with-whom-xt) !

\ Use this for scanning subsets, reset with 'reset-scan-whom' in the same word!
: scan-whom-xt! ( do-with-whom-xt -- )
    (do-with-whom-xt) !
    nucs-not-scanned ;

: reset-scan-whom ( -- )   ['] do-with-everybody scan-whom-xt! ;

: do-a-nuc-scan ( -- )
    init-nuc-scan
    ['] scan-nuc (do-with-whom-xt) @ EXECUTE	\ see above
    nuc-scan-average
    step @ (nucs-scanned-at-step) ! ;

: ?rescan-nucs ( -- )
    (nucs-scanned-at-step) @ step @ = IF  EXIT  THEN
    do-a-nuc-scan ;

2VARIABLE (scan-addr&index)
: (scan-nuc-int-detail) ( -- )
    (scan-addr&index) 2@   ( addr-of-result-buffer index-of-nuc-var-to-scan )
    nuc-addr @		   ( addr-of-result-buffer value-of-scanned-nuc-var )
    swap data2slice ;

nuc-floats# [IF]
: (scan-nuc-dfloat-detail) ( -- )
    (scan-addr&index) 2@   ( addr-of-result-buffer index-of-nuc-var-to-scan )
    nuc-dfloat-addr df@	   ( addr-of-result-buffer F: value-of-scanned-nuc-var)
    float-data2slice ;
[THEN]

: (scan-nuc-int-function)
    (scan-addr&index) 2@   ( addr-of-result-buffer xt-of-function-to-scan )
    EXECUTE		   ( addr-of-result-buffer result-of-function )
    swap data2slice ;

VARIABLE (last-scanned-xt)	\ used in 'bar-ranged-subset'

: .range-zero? ( variable-name-addr count prescan-data-addr float-flag -- flag)
    IF
	dup >dfloat-max df@  dup >dfloat-min df@ f=
	0= IF  drop 2drop FALSE EXIT  THEN
	dup >dfloat-max df@
    ELSE
	dup 2@ =
	0= IF  drop 2drop  FALSE EXIT  THEN
	dup @ s>f
    THEN
    drop
    0e0 (last-stat-range) df!	\ switches 'bar-ranged-subset' and such
				\ (works also for integers).

    (scan-lines) @ 1- 0 ?DO 0 at-x clear-line-to-end cr LOOP
    statistic-status-bg-color @ color-background
    scan-x spaces  .scan-word  type
    ."  all values are " .float clear-line-to-end
    default-background

    TRUE ;

\ Bar graph scan (for both, integer and dfloats):
: nuc-detailed-scan ( index-of-nuc-var-to-scan  addr-of-prescan-data -- )
    \ Check for range zero first.
    over >r  r@ nuc-var-name  third  r> nuc-var-is-float?  .range-zero? IF
	2drop EXIT
    THEN

    over nuc-var-xt (last-scanned-xt) !

    c-l statistic-array-size allocate			\ buffer for results
    ABORT" nuc-detailed-scan: Couldn't allocate." >r	( r: addr )
    r@ (scan-addr&index) cell+ !

    swap dup (scan-addr&index) ! ( addr-of-min-max index  r: addr-result-buf )

[ nuc-floats# ] [IF]
    nuc-var-is-float? IF
	dup >dfloat-min df@  >dfloat-max df@ c-l r@ init-statistic-array-dfloat
	\ read the data in:  See '(do-with-whom-xt)'.
	['] (scan-nuc-dfloat-detail) (do-with-whom-xt) @ EXECUTE
	r@  (scan-lines) @  (scan-addr&index) @ nuc-var-name
	statistic-display-float
    ELSE
[ELSE] drop [THEN]
        2@  c-l r@  ( min max slices# addr -- ) init-statistic-array-int

	\ read the data in:  See '(do-with-whom-xt)'.
	['] (scan-nuc-int-detail) (do-with-whom-xt) @ EXECUTE
	r@  (scan-lines) @  (scan-addr&index) @ nuc-var-name
	statistic-display-int
[ nuc-floats# ] [IF]
    THEN
[THEN]

    r> free ABORT" nuc-detailed-scan: Couldn't free." ;

: nuc-function-scan ( function-xt  addr-of-prescan-data -- )
    \ Check for range zero first.
    over xt>string  third  false  .range-zero? IF
	2drop EXIT
    THEN

    over (last-scanned-xt) !

    c-l statistic-array-size allocate			\ buffer for results
    ABORT" nuc-function-scan: Couldn't allocate." >r	( r: addr )
    r@ (scan-addr&index) cell+ !

    swap (scan-addr&index) ! ( addr-of-min-max index  r: addr-result-buf )

    2@  c-l r@  ( min max slices# addr -- ) init-statistic-array-int

    \ read the data in:  See '(do-with-whom-xt)'.
    ['] (scan-nuc-int-function) (do-with-whom-xt) @ EXECUTE
    r@  (scan-lines) @  (scan-addr&index) @ xt>string
    statistic-display-int

    r> free ABORT" nuc-function-scan: Couldn't free." ;


\ Use this after getting max and min values by 'do-a-nuc-scan'.
\ (this version works on both now, integers and dfloats).
: nuc-detail-extreems! ( index-of-nuc-var-to-scan -- addr )
    dup (scan-addr&index) !

    \ Get min and max values from previous nuc scan:
    cp@ >r

[ nuc-floats# ] [IF]
    dup nuc-var-is-float? IF
	(nuc-scan-data) @ cp!
	(dfloat-check-data) >r
	dup nuc-dfloat-addr df@				\ min
	r@ >dfloat-min df!

	(nuc-scan-data) @ nuc-length# + cp!
	dup nuc-dfloat-addr df@				\ max
	r@ >dfloat-max df!

	dup nuc-float-start-index +			\ type counters
	dup -inf% nuc-type-counter @  r@ >-inf-count !
	dup real% nuc-type-counter @  r@ >real-count !
	dup +inf% nuc-type-counter @  r@ >+inf-count !
	     nan% nuc-type-counter @  r@ >nan-count  !

	r>
    ELSE
[THEN]
	(nuc-scan-data) @ cp!			\ min
	dup nuc-addr @

	(nuc-scan-data) @ nuc-length# + cp!
	over nuc-addr @				\ max


	\ Store both at the address that will be returned:
	(scan-min-max) >r  r@ 2!  r>
[ nuc-floats# ] [IF]
   THEN
[THEN]

    r> cp!
    nip ;

\ use this after getting max and min values by 'do-a-nuc-scan'
: nuc-detailed-scan-prescanned ( index-of-nuc-var-to-scan -- )
    0 (scan-index) !				\ I just take the first one
    (scan-lines) @ >r			( index  r: old-lines )
    l-s (scan-lines) !			\ full screen
    dup					( index index  r: old-lines )
    (scan-detail) dup @ >r ( i i detail-index-addr  r: o-lin old-detail)
    !					( i  r: o-l old-detail)	\ set detail
    dup nuc-detail-extreems!	( index-of-nuc-var-to-scan addr-min-max r: l d)
    page
    nuc-detailed-scan		( r: old-lines old-detail-index )

    r> (scan-detail) !			\ restore index
    r> (scan-lines) ! ;				\ restore lines

\ scanning an integer nuc variable
: nuc-int-detail-2-min-max ( -- )
    (scan-detail) @ nuc-addr @ (scan-min-max) data2min-max ;

\ Scanning an integer function result. Can be used for nucs *and* spots.
\ instead of separate nuc-int-function-2-min-max spot-int-function-2-min-max
: int-function-2-min-max ( -- )
    (scan-detail) @ EXECUTE  (scan-min-max) data2min-max ;


nuc-floats# [IF]
    
\ scanning a float nuc variable
: nuc-df-detail-2-min-max ( -- )
    (scan-detail) @ nuc-dfloat-addr df@
    (dfloat-check-data) float-data-check-in ;

\ scanning a float function result (nuc local function)
: nuc-df-function-2-min-max ( -- )
    (scan-detail) @ EXECUTE  (dfloat-check-data) float-data-check-in ;

[THEN]

\ let the user know when zooming has changed
[UNDEFINED] (zoomed) [IF]
    VARIABLE (zoomed)
    (zoomed) off
[THEN]

[UNDEFINED] horizontal [IF]
    1 CONSTANT horizontal
[THEN]

: notify-zoom-change ( horizontal|vertical -- )
    [ FALSE ] [IF] \ notifying through >message
	drop
	s" °  °° °°° °°°° °°°°°°°°  ========= < zooming! > =========  °°°°°°°°°°° °°° °°  °"
	1 >message
    [ELSE]
	(zoomed) dup @ rot or swap !  
    [THEN] ;
' notify-zoom-change is <notify-zoom-change>

\ Horizontal zoom control: i don't want scan range to change with each step
\ Integer version.
: ?zoom-int-scan-range ( -- )
    scan-horizontal-zoom? 0= IF EXIT THEN			\ fixed range
    (scan-min-max) 2@ (last-scan-min-max) 2@ d=	IF EXIT THEN	\ same range

    \ first make sure data is within displayed range:
    \   looking at botton:
    (scan-min-max) >min @ dup	( new-minimum new-minimum )
    (last-scan-min-max)   >min @	( new-minimum new-minimum old-minimum )
    < IF						\ shrunken?
	(last-scan-min-max) >min !
	horizontal notify-zoom-change
    ELSE drop THEN

    \   looking at top:
    (scan-min-max) >max @ dup	( new-maximum new-maximum )
    (last-scan-min-max)   >max @	( new-maximum new-maximum old-maximum )
    > IF							\ grown?
	(last-scan-min-max) >max !
	horizontal notify-zoom-change
    ELSE drop THEN

    \ now check if real range has not shrunken too much:
    (last-scan-min-max)   2@  swap -	( displayed-range-size )
    (scan-min-max) 2@  swap -		( displayed real-range-size )
    swap horizontal-zoom-scale 2@ */ < IF		\ too small!
	(scan-min-max) 2@ (last-scan-min-max) 2!	\ take real siz
	horizontal notify-zoom-change
    THEN ;

\ Horizontal zoom control, dfloat version.
: ?zoom-dfloat-scan-range ( -- )
    scan-horizontal-zoom? 0= IF EXIT THEN			\ fixed range

    (last-dfloat-check-data) (dfloat-check-data)	( old-check-a check-a )
    dup >dfloat-max df@  over >dfloat-max df@ f= IF		\ same range?
	dup >dfloat-min df@  over >dfloat-min df@ f= IF  2drop EXIT  THEN \ yes
    THEN

    \ first make sure data is within displayed range:
    \   looking at botton:
    dup >dfloat-min df@ fdup  over >dfloat-min df@	( min min old-min )
    f< IF						\ shrunken?
	over >dfloat-min df!
	horizontal notify-zoom-change
    ELSE fdrop THEN

    \   looking at top:
    dup >dfloat-max df@ fdup  over >dfloat-max df@	( max max old-max)
    f> IF						\ grown?
	over >dfloat-max df!
	horizontal notify-zoom-change
    ELSE fdrop THEN

    \ now check if real range has not shrunken too much:
    dup >dfloat-max df@   dup >dfloat-min df@ f-	\ real range
    over >dfloat-max df@  over >dfloat-min df@ f-	\ displayed range
    horizontal-zoom-scale 2@ f*/ f< IF			\ too small?
	dup >dfloat-max df@  over >dfloat-max df!	\ take real size
	dup >dfloat-min df@  over >dfloat-min df!
	horizontal notify-zoom-change
    THEN
    2drop ;

: nuc-int-scan-display ( -- )
    (scan-min-max) min-max-init				\ init min-max
    ['] nuc-int-detail-2-min-max do-with-everybody	\ get min and max data

    ?zoom-int-scan-range

    (scan-detail) @ (last-scan-min-max) nuc-detailed-scan ;

: nuc-scan-int-funct-display ( -- )
    (scan-min-max) min-max-init				\ init min-max
    ['] int-function-2-min-max do-with-everybody	\ get min and max data

    ?zoom-int-scan-range

    (scan-detail) @ (last-scan-min-max) nuc-function-scan ;

nuc-floats# [IF]
: nuc-dfloat-scan-display ( -- )
    (dfloat-check-data) float-min-max-init
    ['] nuc-df-detail-2-min-max do-with-everybody

    ?zoom-dfloat-scan-range

    (scan-detail) @  (last-dfloat-check-data) nuc-detailed-scan ;
[THEN]

\ Use this for step display of a nuc detail
nuc-floats# [IF]
: nuc-scan-display ( -- )
    (scan-detail) @ nuc-var-is-float? IF
	nuc-dfloat-scan-display
    ELSE
	nuc-int-scan-display
    THEN ;
[ELSE]
: nuc-scan-display ( -- )  nuc-int-scan-display ;
[THEN]

\ Shorter named alias (for display)
: nuc-scan-func-dspl ( -- )  nuc-scan-int-funct-display ;

LIST: step-display-xt's
: >step-display ( xt index -- )   (scan-index) !  (scan-xt) ! ;

' nuc-scan-display step-display-xt's >list
' nuc-scan-display 0 >step-display


' nuc-scan-func-dspl step-display-xt's >list

: .title-scanned-which ( -- )
    maybe-generic-string  dup string@ ?type-bright  stringbuf-close ;

: .title-scanned-some ( -- )   ." Scanning nucs if " .title-scanned-which ;


VARIABLE (scan-locality)	\ nuc-local% or spot-local%

2VARIABLE (display-scan-xt)
: display-scan ( -- )   (display-scan-xt) 2@ EXECUTE ;
: |nuc-detailed-scan-prescanned| ( index-of-nuc-var-to-scan -- )
    ['] nuc-detailed-scan-prescanned (display-scan-xt) 2!
    nuc-local% (scan-locality) !
    ['] display-scan scan-display-menu ;

VARIABLE (nuc-menus-show-dfloats)	\ used by nuc-menu and .menu-nuc-scan
nuc-floats# [IF]
    (nuc-menus-show-dfloats) on
[ELSE]
    (nuc-menus-show-dfloats) off
[THEN]

VARIABLE (show-float-type-counts)	(show-float-type-counts) off
DEFER simple-nuc-subset ( simple-expression-xt -- )
DEFER dfloat-type-nuc-subset ( df-nuc-var-xt test-xt -- )
DEFER dfloat-value-nuc-subset ( addr-of-df-value df-nuc-var-xt -- )
DEFER int-value-nuc-subset ( value nuc-var-xt -- )

MENU: nuc-scan-men
: .menu-nuc-scan ( -- )
    help-node" Scanning nucs"
    s" Nuc scan results:" start-title-entry clear-line-to-end up-to-here

    ?rescan-nucs

[ nuc-floats# ] [IF]
    20 at-x
    (nuc-menus-show-dfloats) @ IF
	s" dFLOAT "
    ELSE
	s" INTEGER"
    THEN
    <bright-colours>
    redisplay	['] (nuc-menus-show-dfloats) >stack
    ['] toggle-named  menu-entry
    title-colors
    s" dfFiIot" menu-same-key-entry
[THEN]

    29 at-x from-here
    (do-with-whom-xt) @ CASE
	['] do-with-everybody       OF
	    ." Scanning all nucs."
	ENDOF
	['] simple-maybe-do-with-everybody OF
	    .title-scanned-some
	ENDOF
	['] maybe-do-with-everybody OF
	    .title-scanned-some
	ENDOF
	['] do-with-selected-nucs   OF
	    ." Scanning only "
	    s" SELECTED " type-bright
	    ." nucs: "  selected @ . ."  of " count-living .
	ENDOF
	true ABORT" .menu-nuc-scan: Unknown '(do-with-whom-xt)'."
    ENDCASE
    s" "	redisplay	['] context-help	menu-entry
    reset-colours

    cr
    count-living
    ." Nucs scanned: " nucs-scanned @ dup .	( living scanned )
    over <> IF
	." of " .
    ELSE drop THEN
    at-x? 1+ 20 max at-x ." nucs on trial: " trial @ .
    .tab ." compiled: " compiled-genes @ . cr

    5 keep-but-scroll-rest		\ initialize scrolling

    cr
[ nuc-floats# ] [IF]
    (nuc-menus-show-dfloats) @ IF	\ showing dfloats
	(show-float-type-counts) @ IF
	    s" Type counts:"	redisplay
	    ['] (show-float-type-counts) >stack	['] toggle-named    menu-entry
	    s" .-r+to" menu-same-key-entry

	    20 80 screen-column s" -infinity:"	redisplay
	    ['] nuc-with-neg-inf? >stack  ['] simple-nuc-subset	  menu-entry
	    35 80 screen-column s" real:"	redisplay
	    ['] nuc-all-real? >stack	['] simple-nuc-subset	  menu-entry
	    50 80 screen-column s" +infinity:"	redisplay
	    ['] nuc-with-pos-inf? >stack  ['] simple-nuc-subset   menu-entry
	    65 80 screen-column s" nan:"	redisplay
	    ['] nuc-with-nan? >stack    ['] simple-nuc-subset     menu-entry cr

	    cr
	    -2 menu-scroll-lines +!
	    nuc-df-scan-range 2@
	    >r nuc-float-start-index + r> nuc-float-start-index +
	    scrolled-range DO
		i >stack   redisplay	i nuc-var-name	over c@ >r
		['] |nuc-detailed-scan-prescanned|	menu-entry cr
		r> #key-same-entry
	    LOOP

	    4 0 DO
		0 5 at-xy
		(nuc-scan-data) @ [ nuc-length# 4 * ] literal +
		i nuc-floats# * cells +
		nuc-df-scan-range 2@ scrolled-range DO
		    j 15 * 20 + 80 screen-column
		    dup  i cells + @ num>string
		    i n'th-df-nuc-var-xt >stack
		    j CASE
			0 OF  ['] df-var-neg-inf?  ENDOF
			1 OF  ['] df-var-real?     ENDOF
			2 OF  ['] df-var-pos-inf?  ENDOF
			3 OF  ['] df-var-nan?      ENDOF
		    ENDCASE >stack-2
		    ['] dfloat-type-nuc-subset	redisplay	menu-entry cr
		LOOP
		drop
	    LOOP
	ELSE \ (show-float-type-counts) is off
	    nuc-df-scan-range 2@
	    >r nuc-float-start-index + r> nuc-float-start-index +
	    scrolled-range DO
		i >stack   redisplay	i nuc-var-name	over c@ >r
		['] |nuc-detailed-scan-prescanned|	menu-entry cr
		r> #key-same-entry
	    LOOP

	    float-display-width >r
	    12 to float-display-width
	    cp@ >r
	    3 0 DO
		0 3 at-xy
		(nuc-scan-data) @ nuc-length# i * + cp!
		nuc-df-scan-range 2@ scrolled-range DO
		    j 18 * 20 + at-x from-here
		    j CASE
			0 OF ." min: " ENDOF
			1 OF ." max: " ENDOF
			2 OF ." avr: " ENDOF
		    ENDCASE
		    i n'th-dfloat-nuc-var  dup df@ .float
		    >stack	i n'th-df-nuc-var-xt >stack-2	redisplay
		    s" "	['] dfloat-value-nuc-subset	menu-entry cr
		LOOP
	    LOOP
	    r> cp!  r> to float-display-width

	    0 3 at-xy
	    (nuc-scan-data) @ [ nuc-length# 4 * ] literal +
	    nuc-df-scan-range 2@ scrolled-range DO
		[ c-l 4 - ] literal at-x  from-here
		dup i cells +
		dup @ 0= IF [char] . ELSE [char] - THEN emit
		[ nuc-floats# cells ] literal +
		dup @ 0= IF [char] . ELSE [char] r THEN emit
		[ nuc-floats# cells ] literal +
		dup @ 0= IF [char] . ELSE [char] + THEN emit
		[ nuc-floats# cells ] literal +
		@ 0= IF [char] . ELSE [char] ? THEN emit
		s" "  redisplay	['] (show-float-type-counts) >stack
		['] toggle-named	menu-entry
		s" .-r+" menu-same-key-entry
		cr
	    LOOP
	    drop
	THEN \ (show-float-type-counts)
    ELSE				\ showing integers
[THEN]
	nuc-i-scan-range 2@ scrolled-range DO
	    i >stack   redisplay	i nuc-var-name	over c@ >r
	    ['] |nuc-detailed-scan-prescanned|	menu-entry cr
	    r> #key-same-entry
	LOOP

	cp@ >r
	3 0 DO
	    0 3 at-xy
	    (nuc-scan-data) @ nuc-length# i * + cp!
	    nuc-i-scan-range 2@ scrolled-range DO
		j 18 * 20 + at-x from-here
		j CASE
		    0 OF ." min: "     ENDOF
		    1 OF ." max: "     ENDOF
		    2 OF ." average: " ENDOF
		ENDCASE
		i nuc-addr @  dup .num-on-same-line
		>stack	i nuc-var-xt >stack-2		redisplay
		s" "	['] int-value-nuc-subset	menu-entry cr
	    LOOP
	LOOP
	r> cp!
[ nuc-floats# ] [IF]
    THEN
[THEN]

    <common-menu-entries> ;

: nuc-scan-menu ( -- )
    page
    do-a-nuc-scan
    nucs-scanned @ IF
	nuc-scan-men
	['] .menu-nuc-scan menu-display-xt !
	menu-done	['] noop	menu-key-default
	menu-done	['] noop	menu-default
	do-menu-loop
\	free-menus
    ELSE bell THEN ;
' nuc-scan-menu function-key-actions >list
' nuc-scan-menu F4-xt !

\ For simple cases, not with 'maybe-do' or 'maybe-do-simple'.
\ Use 'scan-this-subset' there.
: scan-only-some-nucs ( scan-whom-xt -- )
    scan-whom-xt!
    nuc-scan-menu
    reset-scan-whom ;

\ : var-value-eq-scan-only ( nuc/spot-var-xt value -- )
\     ['] =  variable-number!
\     ['] maybe-do-with-everybody scan-only-some-nucs ;

\ ****************************************************************
\ end	scanning nucs



\ ****************************************************************
\ ***************  Scan the World.  Spot scan.  ******************
\ ****************************************************************

VARIABLE (scan-spots)		(scan-spots) off	\ result data pointer
\ Array of four cell entries for each integer spot variable:
\ base address: (scan-spots) @
\ min
\ max
\ sum (double)	later: average (single)
\ Array of three dfloat entries for each dfloat spot variable:
\ base address: (scan-spots) @  field-i-planes# 4 * cells  +
\ dfloat min
\ dfloat max
\ dfloat sum, later dfloat average
\ Array of four integer type counters for each dfloat spot variable:
\ base address:
\ (scan-spots) @  field-i-planes# 4 * cells  field-df-planes# 3 * dfloats +  +
\ -inf count
\ real count
\ +inf count
\ nan count

VARIABLE spots-scanned		spots-scanned off	\ flag and counter

: init-spot-scan ( -- )
    (scan-spots) @ dup IF				\ free last memory
	free ABORT" init-spot-scan: Could not free"
    ELSE drop THEN

    \ compute data length at compile time
    [ field-i-planes# 4 * cells		\ integer min max d-sum
    field-df-planes# 3 * dfloats +	\ (it's dfaligned)  dfloat  min max sum
    field-df-planes# 4 * cells + ]	\ type counts for -inf real +inf nan
    literal >r
    r@ allocate ABORT" init-spot-scan: Could not allocate."
    (scan-spots) !

    spots-scanned off
    (scan-spots) @ r> erase

    (scan-spots) @
    field-i-planes# 0 DO
	dup i 2* 2* cells + >r			\ address of minimum
	highest-integer# r@ !			\ initialise minimum
	lowest-integer#  r> cell+ !		\ initialise maximum
    LOOP

[ spot-floats# ] [IF]
    [ field-i-planes# 4 * cells ] literal +	\ base addr of dfloat data
    field-df-planes# dfloats 0 DO
	dup i 3 * + >r
	+infinity r@ df!				\ initialize minimum
	-infinity r> [ 1 dfloats ] literal + df!	\ initialize maximum
    [ 1 dfloats ] literal +LOOP
[THEN]

    drop ;

: scan-spot ( -- )
    field-i-planes# p0 DO	\ skip pointer plane?
	i n'th-spot-variable @	\ read a quality of the actual spot
	(scan-spots) @ i 2* 2* cells +	\ address of actual minimum
	dup >r
	@ over min r@ !			\ store new minimum
	r> cell+ dup >r			\ address of actual maximum
	@ over max r> !			\ store new maximum
	s>d					\ convert to double
	(scan-spots) @ i 2* 2* 2 + cells +	\ address summation double cell
	dup >r
	2@ d+ r> 2!				\ double precision sum
    LOOP

[ spot-floats# ] [IF]
    (scan-spots) @ [ field-i-planes# 4 * cells ] literal + \ dfloat data addr
    dup [ field-df-planes# 3 * dfloats ] literal +	   \ type counter addr
    field-df-planes# 0 DO	( dfloat-data-addr type-counter-addr )
	i n'th-spot-f-variable df@			\ sample value
	fdup float-type
	dup 1+ i 2* 2* + cells third + +1!		\ count type
	real% = IF
	    over i 3 * dfloats + >r			\ address of minimum
	    fdup r@ df@ fmin r@ df!			\ store new minimum
	    r> [ 1 dfloats ] literal + >r		\ address of maximum
	    fdup r@ df@ fmax r@ df!			\ store new maximum
	    r> [ 1 dfloats ] literal + df+!		\ sum up
	ELSE fdrop THEN
    LOOP
    2drop
[THEN]

    1 spots-scanned +! ;		\ counter

: spot-scan-average ( -- )
    spots-scanned @ IF
	field-i-planes# p0 DO			\ skip pointer plane?
	    (scan-spots) @ i 2* 2* cells +	\ data for this quality
	    2 cells +	dup >r			\ summation double cell
	    2@
	    1 spots-scanned @  	m*/	d>s	\ divide by counter
	    r> !				\ store as single cell
	LOOP

[ spot-floats# ] [IF]
	(scan-spots) @				\ scan data base address
	[ field-i-planes# 4 * cells ] literal + \ dfloat data base address
	dup [ field-df-planes# 3 * dfloats ] literal +	\ type counter addr
	field-df-planes# 0 DO	( dfloat-data-addr type-counter-addr )
	    i 2* 2* 1+ cells  over + @			\ real count#
	    dup IF
		third  i 3 * 2 + dfloats  +  >r		( .. r: sum/avrge-adr )
		r@ df@  s>f  f/ r> df!			\ store average
	    ELSE drop THEN
	LOOP
	2drop
[THEN]

    ELSE bell THEN ;

\ Conditional scan of a spot subset (1):
VARIABLE (do-where-xt)		' do-everywhere (do-where-xt) !
\ Don't use '(do-where-xt)' directly,
\ use 'scan-where-xt!' and 'reset-scan-where' instead.

: scan-where-xt! ( do-where-xt -- )   (do-where-xt) !  world-not-scanned ;

: reset-scan-where ( -- )   ['] do-everywhere scan-where-xt! ;

: do-spot-scan ( -- )
    init-spot-scan
    ['] scan-spot (do-where-xt) @ EXECUTE
    spot-scan-average
    step @ (world-scanned-at-step) ! ;

: ?rescan-spots ( -- )
    (world-scanned-at-step) @ step @ = IF  EXIT  THEN
    do-spot-scan ;

\ ****************************************************************
\ end	spot scan



\ ****************************************************************
\ ***************  Detailed scan of spot data.  ******************
\ ****************************************************************

\ Detailed scan of spots:
\ index of the spot variable to be displayed in spot-scan-display
\ VARIABLE (spot-index-to-scan)	1 (spot-index-to-scan) ! ##########
: (scan-int-spot-detail) ( -- )
    (scan-addr&index) 2@   ( addr-of-result-buffer index-of-spot-var-to-scan )
    n'th-spot-variable @   ( addr-of-result-buffer value-of-scanned-spot-var )
    swap data2slice ;

: (scan-df-spot-detail) ( -- )
[ spot-floats# ] [IF]
    (scan-addr&index) 2@   ( addr-of-result-buffer index-of-spot-var-to-scan )
    spot-float-start-index - n'th-spot-f-variable df@	( addr-result F: value)
    float-data2slice
[THEN] ;

: spot-detailed-scan ( index-of-spot-var-to-scan  addr-of-min-max -- )
    \ Check for range zero first.
    over >r  r@ spot-var-name  third  r> spot-var-is-float?  .range-zero? IF
	2drop EXIT
    THEN

    over spot-var-xt (last-scanned-xt) !
    c-l statistic-array-size allocate			\ buffer for results
    ABORT" spot-detailed-scan: Couldn't allocate." >r	( r: addr )
    r@ (scan-addr&index) cell+ !

    swap dup (scan-addr&index) ! ( addr-of-min-max index  r: addr-result-buf )

    spot-var-is-float? IF
	dup >dfloat-min df@  >dfloat-max df@ c-l r@ init-statistic-array-dfloat
	['] (scan-df-spot-detail) (do-where-xt) @ EXECUTE	\ read data in

	r@  (scan-lines) @  (scan-addr&index) @ spot-var-name
	statistic-display-float
    ELSE
	2@  c-l r@  ( min max slices# addr -- ) init-statistic-array-int
	['] (scan-int-spot-detail) (do-where-xt) @ EXECUTE	\ read data in

	r@ (scan-lines) @ (scan-addr&index) @ spot-var-name
	statistic-display-int
    THEN

    r> free ABORT" spot-detailed-scan: Couldn't free." ;

: spot-int-detail-2-min-max ( -- )
    (scan-detail) @ n'th-spot-variable @ (scan-min-max) data2min-max ;

: spot-df-detail-2-min-max ( -- )
    (scan-detail) @ spot-dfloat-addr df@
    (dfloat-check-data) float-data-check-in ;

: spot-int-scan-display ( -- )
    (scan-min-max) min-max-init			\ init min-max
    ['] spot-int-detail-2-min-max do-everywhere	\ get min and max data

    ?zoom-int-scan-range

    (scan-detail) @ (last-scan-min-max) spot-detailed-scan ;

: spot-dfloat-scan-display ( -- )
    (dfloat-check-data) float-min-max-init
    ['] spot-df-detail-2-min-max do-everywhere

    ?zoom-dfloat-scan-range

    (scan-detail) @  (last-dfloat-check-data) spot-detailed-scan ;

\ Use this for step display of a spot detail
: spot-scan-display ( -- )
    (scan-detail) @ spot-var-is-float? IF
	spot-dfloat-scan-display
    ELSE
	spot-int-scan-display
    THEN ;
' spot-scan-display step-display-xt's >list
' spot-scan-display 1 >step-display

: |(spot-scan-display)| ( index -- )
    >r
    (scan-index) off		\ we take the first one...
    (scan-detail) @	\ preserve old values
    (scan-lines) @
    r> (scan-detail) !
    l-s (scan-lines) !		\ full screen display
    page spot-scan-display

    (scan-lines) !		\ restore old values
    (scan-detail) ! ;

: |spot-scan-display| ( index -- )
    ['] |(spot-scan-display)| (display-scan-xt) 2!
    spot-local% (scan-locality) !
    ['] display-scan scan-display-menu ;

: .title-scanned-where ( -- )
    ." Scanning spots if "
    .title-scanned-which ;

VARIABLE (spot-menus-show-dfloats)	\ .menu-edit-spot and .menu-spot-scan
spot-floats# [IF]
    (spot-menus-show-dfloats) on
[ELSE]
    (spot-menus-show-dfloats) off
[THEN]

DEFER simple-spot-subset ( simple-expression-xt -- )
DEFER dfloat-type-spot-subset ( df-spot-var-xt test-xt -- )
DEFER dfloat-value-spot-subset ( addr-of-df-value df-spot-var-xt -- )
DEFER int-value-spot-subset ( value spot-var-xt -- )

MENU: spot-scan-men
: .menu-spot-scan ( -- )
    help-node" Scanning spots"
    s" World scan results:" start-title-entry clear-line-to-end up-to-here

    ?rescan-spots

[ nuc-floats# ] [IF]
    20 at-x
    (spot-menus-show-dfloats) @ IF
	s" dFLOAT "
    ELSE
	s" INTEGER"
    THEN
    <bright-colours>
    redisplay	['] (spot-menus-show-dfloats) >stack
    ['] toggle-named  menu-entry
    title-colors
    s" dfFiIot" menu-same-key-entry
[THEN]

    29 at-x from-here
    (do-where-xt) @ CASE
	['] do-everywhere	OF
	    ." Scanning whole world."
	ENDOF
	.title-scanned-where
    ENDCASE
    s" "	redisplay	['] context-help	menu-entry
    reset-colours

    cr
    ." Scanned spots: " spots-scanned @ dup .
    spots <> IF ." of " spots . THEN cr

    5 keep-but-scroll-rest		\ initialize scrolling

    cr
[ spot-floats# ] [IF]
    (spot-menus-show-dfloats) @ IF	\ showing dfloats
	(show-float-type-counts) @ IF
	    s" Type counts:"	redisplay
	    ['] (show-float-type-counts) >stack	['] toggle-named    menu-entry
	    s" .-r+to" menu-same-key-entry

	    20 80 screen-column s" -infinity:"	redisplay
	    ['] spot-with-neg-inf? >stack  ['] simple-spot-subset  menu-entry
	    35 80 screen-column s" real:"	redisplay
	    ['] spot-all-real? >stack	['] simple-spot-subset	   menu-entry
	    50 80 screen-column s" +infinity:"	redisplay
	    ['] spot-with-pos-inf? >stack  ['] simple-spot-subset  menu-entry
	    65 80 screen-column s" nan:"	redisplay
	    ['] spot-with-nan? >stack    ['] simple-spot-subset   menu-entry cr

	    cr
	    -2 menu-scroll-lines +!
	    (scan-spots) @
	    [ field-i-planes# 4 * cells			  \ addr of dfloat data
	      field-df-planes# 3 * dfloats + ] literal +  \ addr of type counts
	    0 5 at-xy
	    field-df-planes# 0 scrolled-range DO ( df-data-addr counters-addr )
		i n'th-spot-f-var-xt xt>string
		>r dup r>			\ 1st letter addr for key entry
		i >stack  redisplay	['] |spot-scan-display|	menu-entry
		c@ #key-same-entry		\ first letter key menu entry

		4 0 DO
		    i 15 * 20 + 80 screen-column from-here
		    dup j 2* 2* i + cells + @ .
		    j n'th-spot-f-var-xt >stack
		    i CASE
			0 OF ['] df-var-neg-inf? ENDOF
			1 OF ['] df-var-real?    ENDOF
			2 OF ['] df-var-pos-inf? ENDOF
			3 OF ['] df-var-nan?     ENDOF
		    ENDCASE >stack-2
		    s" " redisplay ['] dfloat-type-spot-subset menu-entry
		LOOP
		cr
	    LOOP
	    drop

	ELSE \ (show-float-type-counts) is off
	    float-display-width >r
	    12 to float-display-width

	    (scan-spots) @
	    [ field-i-planes# 4 * cells ] literal +	\ addr of dfloat data
	    dup [ field-df-planes# 3 * dfloats ] literal +	\ type counts
	    field-df-planes# 0 scrolled-range DO ( df-data-addr counters-addr )
		i n'th-spot-f-var-xt xt>string
		>r dup r>			\ 1st letter addr for key entry
		i spot-float-start-index + >stack	redisplay
		['] |spot-scan-display|	menu-entry
		c@ #key-same-entry		\ first letter key menu entry

		over i [ 3 dfloats ] literal * +
		18 80 screen-column from-here
		." min: " dup df@ .float
		dup >stack	i n'th-spot-f-var-xt >stack-2	redisplay
		s" " ['] dfloat-value-spot-subset		menu-entry
		36 80 screen-column from-here
		." max: " dup [ 1 dfloats ] literal + df@ .float
		dup >stack	i n'th-spot-f-var-xt >stack-2	redisplay
		s" " ['] dfloat-value-spot-subset		menu-entry
		54 80 screen-column from-here
		." average: " dup [ 2 dfloats ] literal + df@ .float
		>stack		i n'th-spot-f-var-xt >stack-2	redisplay
		s" " ['] dfloat-value-spot-subset		menu-entry

		c-l 4 - at-x from-here
		dup i 2* 2* cells +		\ current type counters address
		dup @ IF [char] - ELSE [char] . THEN emit
		dup cell+ @ IF [char] r ELSE [char] . THEN emit
		dup [ 2 cells ] literal + @ IF [char] + ELSE [char] . THEN emit
		[ 3 cells ] literal + @ IF [char] ? ELSE [char] . THEN emit
		s" "	redisplay	['] (show-float-type-counts) >stack
		['] toggle-named	menu-entry cr
		s" .-r+" menu-same-key-entry
	    LOOP
	    2drop

	    r> to float-display-width
	THEN \ (show-float-type-counts)
    ELSE				\ showing integers
[THEN]
	field-i-planes# p0 scrolled-range ?DO	\ skip pointer plane?
	    i spot-var-name  >r dup r>		\ 1st letter addr for key entry
	    i >stack	 redisplay	['] |spot-scan-display|	menu-entry

	    i spot-var-xt
	    (scan-spots) @ i 2* 2* cells + >r	\ start data for this quality
	    1 4 screen-column from-here
	    ." min: "		r@	     @  dup .	>stack	dup >stack-2
	    s" "	['] int-value-spot-subset  menu-entry
	    2 4 screen-column from-here
	    ." max: "		r@ cell+     @  dup .	>stack	dup >stack-2
	    s" "	redisplay	['] int-value-spot-subset  menu-entry
	    3 4 screen-column from-here
	    ." average: "	r> 2 cells + @  dup .num-on-same-line	>stack
	    >stack-2
	    s" "	redisplay	['] int-value-spot-subset  menu-entry
	    c@ #key-same-entry			\ first letter key menu entry
	    cr
	LOOP cr
[ spot-floats# ] [IF]
    THEN
[THEN]

    <common-menu-entries> ;

: spot-scan-menu ( -- )
    page
    do-spot-scan
    spots-scanned @ IF
	spot-scan-men
	['] .menu-spot-scan menu-display-xt !
	menu-done	['] noop	menu-key-default
	menu-done	['] noop	menu-default
	do-menu-loop
\	free-menus
    ELSE bell THEN ;

' spot-scan-menu function-key-actions >list
' spot-scan-menu F3-xt !


\ Conditional scan of a spot subset (2):
: scan-some-spots ( maybe-do-field-xt -- )
    EXECUTE					\ make maybe-do-field actual
    ['] maybe-do-this-everywhere scan-where-xt!
    spot-scan-menu
    reset-scan-where ;


\ ****************************************************************
\ end	detailed spot scan



\ ****************************************************************
\ ******************  menu-select-nuc-var  ***********************
\ ****************************************************************

MENU: select-nuc-var-men
VARIABLE (index-selected)
: .menu-select-nuc-var ( -- )
\   help-node" "
    page

    title-colors ." Select nuc variable:" end-title	\ ###### context-help

    4 keep-but-scroll-rest

    cr
    nuc-df-scan-limit nuc-variables scrolled-range DO
	i dup >stack	['] (index-selected) >stack-2	menu-done
	nuc-var-name	over c@ >r	['] n-named!	menu-entry cr
	r> #key-same-entry
    LOOP

    <common-menu-entries> ;

: menu-select-nuc-var ( -- )
    -1 (index-selected) !	\ impossible value
    select-nuc-var-men
    ['] .menu-select-nuc-var menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop ;

\ set variable type flag:
: set-scan-var-type ( -- )
    (scan-xt) @ CASE
	['] nuc-scan-display  OF
	    (scan-flags) dup @ 
	    (scan-detail) @ nuc-var-is-float? IF
		dfloat-scan or
	    ELSE
		[ dfloat-scan invert ] literal and
	    THEN swap !
	ENDOF
	['] spot-scan-display  OF
	    (scan-flags) dup @ 
	    (scan-detail) @ spot-var-is-float? IF
		dfloat-scan or
	    ELSE
		[ dfloat-scan invert ] literal and
	    THEN swap !
	ENDOF
	['] nuc-scan-func-dspl OF
	    (scan-flags) dup @ [ dfloat-scan invert ] literal and swap !
	ENDOF
    ENDCASE ;

: select-scanned-nuc-var ( -- )	\ sets (scan-detail)
    menu-select-nuc-var
    (index-selected) @ dup -1 <> IF
	(scan-detail) !
	set-scan-var-type
    ELSE drop THEN ;

: select-nuc-var-to-addr ( addr -- )
    menu-select-nuc-var
    (index-selected) @ dup -1 <> IF	\ selected something?
	swap !
    ELSE 2drop THEN ;

: select-nuc-xt-to-addr ( addr -- )
    menu-select-nuc-var
    (index-selected) @ dup -1 <> IF	\ selected something?
	nuc-var-xt swap !
    ELSE 2drop THEN ;

\ ****************************************************************
\ end	menu-select-nuc-var



\ ****************************************************************
\ *****************  menu menu-select-spot-var  ******************
\ ****************************************************************

MENU: select-spot-var-men
: .menu-select-spot-var ( -- )
    help-node" Variable names"
    page

    title-colors ." Select a spot variable:" end-title	\ ###### context-help

    4 keep-but-scroll-rest	\ initialize scrolling

    cr
    field-i-planes# field-df-planes# +  p0  scrolled-range DO
	i >stack	['] (index-selected) >stack-2
	i spot-var-name  over >r		\ 1st letter addr for key entry
	menu-done	['] n-named!	menu-entry cr
	r> c@ #key-same-entry			\ first letter key menu entry
    LOOP

    <common-menu-entries> ;

: menu-select-spot-var ( -- )
    select-spot-var-men
    -1 (index-selected) !		\ impossible value
    ['] .menu-select-spot-var menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop ;

: |menu-select-spot-var| ( -- )		\ sets (scan-detail)
    menu-select-spot-var
    (index-selected) @ dup -1 <> IF
	(scan-detail) !
	set-scan-var-type
    ELSE drop THEN ;

: select-spot-var-to-addr ( addr -- )
    menu-select-spot-var
    (index-selected) @ dup -1 <> IF	\ selected something?
	swap !
    ELSE 2drop THEN ;

\ ****************************************************************
\ end	menu-select-spot-var



\ ****************************************************************
\ *******************  Continuous Display  ***********************
\ ****************************************************************

\ Continuous display:  There are different types and sub types.
\ * displaying value of normal (world) variables like 'living'
\ * displaying value of values that need a nuc or spot scan with subtypes
\   minimal average and maximal
\ For each displayed item we need separate range control and zooming.
\ separate foreground color would be nice too.
\ These data (for each displayed parameter) could be stored in a list.
\ So for continuous display (scan-detail) could store the list xt.
\ VARIABLE (continuous-node)

\ VARIABLE (continuous-column)	(continuous-column) off	\ global

\ structure for each displayed parameter:
0
\ >cont-xt MUST be first. Word must be ( item -- value float-flag )
OFFSET: >cont-xt
OFFSET: >cont-item
dfloat-OFFSET: >cont-lower		\ integer or dfloat
dfloat-OFFSET: >cont-upper		\ integer or dfloat

OFFSET: >cont-var-type			\ type-int-addr%  or  type-df-addr%
\ scratch variable to hold variable type while processing an item:
VARIABLE (cont-var-type)	type-unknown% (cont-var-type) !

OFFSET: >cont-foreground-xt
OFFSET: >cont-char
OFFSET: >cont-redisplay-data		\ each one byte y, one byte zoomed flag
cell -
c-l 2* +
cell /
nLIST: continuous-display-list	\ we have only one for the moment

decimal 12 CONSTANT max-continuous-items#

LIST: cont-variables
' living	cont-variables >list
' newborn	cont-variables >list
' cloned	cont-variables >list
' died		cont-variables >list
' nuc-do-cost	cont-variables >list
' code-price	cont-variables >list
' selected	cont-variables >list

LIST: cont-functions

\ dummy function to indicate missing selection
\ : (none) ;
' (none) cont-functions >list

\ Fetch a dfloat or an integer value depending (cont-var-type)
: cont@ ( addr -- n FALSE | r TRUE )
    (cont-var-type) @ type-df-addr% = >r
    r@ IF  df@  ELSE  @  THEN
    r> ;

: get-variable ( xt -- n|r float-flag )   EXECUTE cont@ ;
' get-variable cont-functions >list

: nuc-min ( i -- n|r float-flag )   (nuc-scan-data) @ cp! |nuc-addr| cont@ ;
' nuc-min cont-functions >list

: nuc-max ( i -- n|r float-flag )
    (nuc-scan-data) @ nuc-length# + cp!
    |nuc-addr| cont@ ;
' nuc-max cont-functions >list

: nuc-range ( i -- n|r float-flag )
    >r  r@ nuc-max
    IF				\ float
	r> nuc-min f-
    ELSE			\ integer
	r> nuc-min >r - r>
    THEN ;
' nuc-range cont-functions >list

: nuc-average ( i -- n|r float-flag )
    (nuc-scan-data) @ [ nuc-length# 2* ] literal + cp!
    |nuc-addr| cont@ ;
' nuc-average cont-functions >list

\ Helper words for spot dfloat data:
: spot-i-2-dfloat-min ( i - addr )
    spot-float-start-index -
    [ 3 dfloats ] literal *			\ df-data plane offset
    [ field-i-planes# 4 * cells ] literal  +	\ dfloat base offset
    (scan-spots) @ + ;				\ scan data base address

: spot-i-2-dfloat-max ( i - addr )
    spot-i-2-dfloat-min  [ 1 dfloats ] literal + ;

: spot-i-2-dfloat-avr ( i - addr )
    spot-i-2-dfloat-min  [ 2 dfloats ] literal + ;

: spot-min ( i -- n|r float-flag )
    (cont-var-type) @ type-df-addr% = >r
    r@ IF				\ dfloat
	spot-i-2-dfloat-min df@
    ELSE				\ integer
	2* 2* cells (scan-spots) @ + @
    THEN
    r> ;
' spot-min cont-functions >list

: spot-max ( i -- n|r float-flag )
    (cont-var-type) @ type-df-addr% = >r
    r@ IF				\ dfloat
	spot-i-2-dfloat-max df@
    ELSE				\ integer
	2* 2* 1+ cells (scan-spots) @ + @
    THEN
    r> ;
' spot-max cont-functions >list

: spot-range ( i -- n|r float-flag )
    >r  r@ spot-max
    IF					\ dfloat
	r> spot-min f-
    ELSE				\ integer
	r> spot-min  >r - r>
    THEN ;
' spot-range cont-functions >list

: spot-average ( i -- n|r float-flag )
    (cont-var-type) @ type-df-addr% = >r
    r@ IF				\ dfloat
	spot-i-2-dfloat-avr df@
    ELSE				\ integer
	2* 2* 2 + cells (scan-spots) @ + @
    THEN
    r> ;
' spot-average cont-functions >list

: continuous-display-type ( xt -- code=0|1|2|-1 )
    CASE
	['] (none)	 OF -1 ENDOF	\ nothing 
	['] get-variable OF  0 ENDOF	\ variable
	['] nuc-min	 OF  1 ENDOF	\ nuc
	['] nuc-max      OF  1 ENDOF	\ nuc
	['] nuc-range    OF  1 ENDOF	\ nuc
	['] nuc-average  OF  1 ENDOF	\ nuc
	['] spot-min	 OF  2 ENDOF	\ spot
	['] spot-max	 OF  2 ENDOF	\ spot
	['] spot-range   OF  2 ENDOF	\ spot
	['] spot-average OF  2 ENDOF	\ spot

	true ABORT" continuous-display-type: Unknown continuous display function"
    ENDCASE ;

\ When the user changes cont-var-type we must reset range.
\ (range will be initialised inside  check-continuous-display-sanity ).
: cont-var-type! ( type node-address -- )
    >r  r@ >cont-var-type >r	( new-type  r: node-address type-address )
    r@ @
    over r> !			( new-type old-type  r: node )	\ store new
    = IF  rdrop EXIT  THEN				\ no change?

    0e0 r@ >cont-lower df!    0e0 r> >cont-upper df! ;	\ for int *and* dfloat

\ Word to check (and fix) continuous display inconsistencies after user input:
\ check if the selected variable exists, check and fix ranges,
\ set  (scan-flags) , set type,  initialize  char, colours..
: check-continuous-display-sanity ( -- )
    (scan-flags)
    dup @ [ cont-scan-nucs cont-scan-spots or invert ] literal and swap !

    continuous-display-list
    dup nodes 0 ?DO
	next-node >r				( r: node )

	r@ >cont-xt @ continuous-display-type	( type-code   r: node )
	CASE
	    0 OF	\ (world) variable
		BEGIN
		    r@ >cont-item @
		    cont-variables
		    dup nodes 0 DO
			next-node
			2dup @ = IF drop TRUE LEAVE THEN
		    LOOP
		    nip true <>
		WHILE
		    s" Select variable:" menu-title!
		    ['] cont-variables  r@ >cont-item  choose-xt-menu
		REPEAT

		\ range from 0 to spots:
		r@ >cont-item @
		dup  ['] living =
		dup IF
		    r@ >cont-upper dup @ 0= IF spots swap ! ELSE drop THEN
		THEN
		over ['] selected = or
		over ['] newborn = or
		over ['] cloned = or
		swap ['] died = or
		IF
		    r@ >cont-lower dup @ 0 max swap !
		    r@ >cont-upper dup @ spots min swap !
		THEN

		type-int-addr% r@ cont-var-type!		\ no floats here yet.
	    ENDOF
	    1 OF	\ nuc variable
		(scan-flags) dup @ cont-scan-nucs or swap !
		r@ >cont-item @  nuc-variables  nuc-df-scan-limit  within 0= IF
		    menu-select-nuc-var (index-selected) @ dup -1 <> IF
			r@ >cont-item !
		    ELSE drop THEN
		THEN

		r@ >cont-item @ nuc-var-is-float? IF
		    type-df-addr%  ELSE  type-int-addr%
		THEN  r@ cont-var-type!
	    ENDOF
	    2 OF	\ spot variable
		(scan-flags) dup @ cont-scan-spots or swap !
		r@ >cont-item @	 p0  field-i-planes# field-df-planes# +
		within 0= IF
		    menu-select-spot-var (index-selected) @ dup -1 <> IF
			r@ >cont-item !
		    ELSE drop THEN
		THEN

		r@ >cont-item @ spot-var-is-float? IF
		    type-df-addr%  ELSE  type-int-addr%
		THEN  r@ cont-var-type!
	    ENDOF
	ENDCASE

	r@ >cont-var-type @ type-df-addr% = IF			\ dfloat
	    r@ >cont-xt @ dup ['] nuc-range = swap  ['] spot-range = or IF
		r@ >cont-lower dup df@ 0e0 fmax df!
	    THEN

	    r@ >cont-lower df@  r@ >cont-upper df@ f< 0= IF
		r@ >cont-lower df@ 1e0 f+	\ arbitrary range
		r@ >cont-upper df!
	    THEN
	ELSE							\ integer
	    r@ >cont-xt @ dup ['] nuc-range = swap  ['] spot-range = or IF
		r@ >cont-lower dup @ 0 max swap !
	    THEN

	    r@ >cont-lower @  r@ >cont-upper @ < 0= IF
		r@ >cont-lower @ 20 + r@ >cont-upper !	\ arbitrary range
	    THEN
	THEN

	r@ >cont-char dup @ 0= IF [char] * swap ! ELSE drop THEN

	r@ >cont-foreground-xt dup @ 0= IF
	    ['] white swap !
	ELSE drop THEN

	r>
    LOOP drop ;

: cont-item-name ( node -- addr count )
    >r
    r@ >cont-xt @ continuous-display-type CASE
	-1 OF	s" "				ENDOF
	 0 OF 	r@ >cont-item @ xt>string	ENDOF
	 1 OF 	r@ >cont-item @ nuc-var-name	ENDOF
	 2 OF 	r@ >cont-item @ spot-var-name	ENDOF
     ENDCASE
     rdrop ;

: -3-norm ( addr count --- addr' count' )
    s" -" search IF
	>r 1+ r> 1- 3 min
    ELSE
	8 min
    THEN ;

VARIABLE (cont-y)			\ start row
: .cont-status-line ( -- )
    statistic-status-bg-color @ color-background

    0  (cont-y) @ (scan-lines) @ + 1-  at-xy
    continuous-display-list
    dup nodes 0 ?DO
	next-node >r					( r: node )
	r@ >cont-xt @
	dup ['] (none) <> IF
	    r@ >cont-foreground-xt @ EXECUTE color-foreground
	    r@ >cont-char @ pad c! pad 1 ?type_
	    default-foreground
	    xt>string -3-norm ?type_
	    r@ cont-item-name ?type_
	    ?space
	ELSE drop THEN
	r>
    LOOP
    drop
    clear-line-to-end
    default-background ;

\ Redisplay continuous display:
\ Continuous display can not be restored from current data only.
\ For redisplay we have to save all data for this screen.
VARIABLE (cont-drawn-at-step)		-2 (cont-drawn-at-step) !
: redisplay-cont-display ( -- )
    continuous-display-list
    dup nodes 0 ?DO
	next-node
	dup >cont-xt @ ['] (none) <> IF
	    dup >cont-foreground-xt @ EXECUTE color-foreground
	    dup >cont-char @
	    (continuous-column) @  0 ?DO	  ( current-node char )
		over >cont-redisplay-data i 2* +  ( node char display-data-adr)
		i over c@ at-xy
		1+ c@ IF			  ( current-node char )
		    cyan color-foreground
		    dup emit
		    over >cont-foreground-xt @ EXECUTE color-foreground
		ELSE
		    dup emit
		THEN
	    LOOP
	    drop
	THEN
    LOOP drop
    default-foreground
    .cont-status-line ;

2VARIABLE cont-zoom-up-scale		2 1 cont-zoom-up-scale 2!
: continuous-display ( -- )
    redisplaying? IF	\ maybe it's only a redisplay?
	(continuous-column) @ IF
	    (cont-drawn-at-step) @ 1+ step @ = IF	\ simple, but not save
		redisplay-cont-display  EXIT
	    ELSE
		(continuous-column) off
	    THEN
	THEN
    THEN

    (scan-flags) @
    dup cont-scan-nucs and IF		\ we need a nuc scan?
	step @ (nucs-scanned-at-step) @ <> IF
	    do-a-nuc-scan
	THEN
    THEN
    cont-scan-spots and IF		\ we need a spot scan?
	step @ (world-scanned-at-step) @ <> IF
	    do-spot-scan
	THEN
    THEN

    at? (cont-y) ! drop

    (continuous-column) @ 0= IF		\ clear screen
	(cont-y) @ dup (scan-lines) @ + 1- swap ?DO
	    0 i at-xy clear-line-to-end
	LOOP
	.cont-status-line
	step-background-coloring? IF  \ correct bg color
	    scan-background-xt @ EXECUTE color-background
	THEN
    THEN

    continuous-display-list
    dup nodes 0 ?DO
	next-node >r					( r: node )
	r@ >cont-item @  r@ >cont-xt @
	dup ['] (none) <> IF
	    r@ >cont-var-type @  (cont-var-type) !	\ float or integer?
	    EXECUTE				( value float-flag  r: node )

	    IF					\ dfloat
		\ check range:
		fdup r@ >cont-lower df@ f< IF
		    r@ >cont-upper df@  fdup  r@ >cont-lower df@ f-
		    cont-zoom-up-scale 2@ f*/  f-
		    fover fmin r@ >cont-lower df!
		    check-continuous-display-sanity		\ range check
		    (zoomed) on
		THEN
		fdup r@ >cont-upper df@ f> IF
		    r@ >cont-lower df@  fdup  r@ >cont-upper df@ f-
		    cont-zoom-up-scale 2@ f*/  f-
		    fover fmax r@ >cont-upper df!
		    check-continuous-display-sanity		\ range check
		    (zoomed) on
		THEN

		r@ >cont-upper df@  fswap f-
		(scan-lines) @ 2 - s>f f*
		r@ >cont-upper df@  r@ >cont-lower df@  f-  f/  f>s
	    ELSE				\ integer
		\ check range:
		dup r@ >cont-lower @ < IF
		    r@ >cont-upper @  dup  r@ >cont-lower @ -
		    cont-zoom-up-scale 2@ */  -
		    over min r@ >cont-lower !
		    check-continuous-display-sanity		\ range check
		    (zoomed) on
		THEN
		dup r@ >cont-upper @ > IF
		    r@ >cont-lower @  dup  r@ >cont-upper @ -
		    cont-zoom-up-scale 2@ */  -
		    over max r@ >cont-upper !
		    check-continuous-display-sanity		\ range check
		    (zoomed) on
		THEN

		r@ >cont-upper @  swap -
		(scan-lines) @ 2 -
		r@ >cont-upper @  r@ >cont-lower @  -   */
	    THEN

	    (cont-y) @ +  (continuous-column) @
	    2dup 2* r@ >cont-redisplay-data +  dup 1+ >r  c!	\ for redisplay
	    swap at-xy

	    (zoomed) @ IF
		-1 r> c!		\ set zoom flag for redisplay
		cyan color-foreground
		(zoomed) off
	    ELSE
		0 r> c!			\ clear zoom flag for redisplay
		r@ >cont-foreground-xt @ EXECUTE color-foreground
	    THEN

	    r@ >cont-char @ emit

	    default-foreground
	ELSE
	    2drop
	THEN

	r>
    LOOP drop
    (continuous-column) dup @ 1+ c-l mod swap !
    step @  (cont-drawn-at-step) ! ;
' continuous-display step-display-xt's >list

: add-continuous-entry ( -- )
    \ 'check-continuous-display-sanity' does the rest
    continuous-display-list nodes max-continuous-items# < IF
	(index-selected) off		\ abused variable (index-selected)
	s" Add a new continuous display function:" menu-title!
	['] cont-functions  ['] (index-selected)  choose-xt-to-var
	(index-selected) @ dup IF
	    continuous-display-list new-node dup >r !
	    ['] white r> >cont-foreground-xt !
	ELSE drop THEN
    ELSE
	page bell cr cr <other-colour>
	." Not more than " max-continuous-items# . ." items possible."
	reset-colours
	2500 ms
    THEN ;


LIST: cont-display-colors
color-list cont-display-colors copy-simple-list-elements

MENU: continuous-display-men
: .menu-continuous-display ( -- )
    check-continuous-display-sanity

    help-node" Menu continuous display"
    s" Setup continuous display:" start-title-entry
    step-display-on? 0= IF
	(highlite-active) dup dup @ 2>r off
	<other-colour>
	s"   Step display is off, nothing will be shown!"	redisplay
	['] step-display-on >stack	display-switch >stack-2
	['] named-xor! menu-entry
	reset-colours
	r> r> !
    THEN
    end-title

    cr
\   (scan-detail) @ EXECUTE	\ get list addr
    continuous-display-list
    dup nodes 0 ?DO
	next-node >r			( r: node )

	s" Set display function of this item:" menu-title!
	['] cont-functions  r@ >cont-xt  choose-xt-entry

	r@ >cont-xt @ ['] (none) <> IF
	    r@ >cont-var-type @  (cont-var-type) !	\ float or integer? 
	    1 5 screen-column
	    r@ >cont-xt @ continuous-display-type	( type-code   r: node )
	    CASE
		0 OF	\ (world) variables
		    s" Select variable:" menu-title!
		    ['] cont-variables  r@ >cont-item  choose-xt-entry
		ENDOF
		1 OF	\ nuc variables
		    s" Select nuc variable:" menu-title!
		    r@ >cont-item dup >stack	redisplay
		    @ nuc-var-name	['] select-nuc-var-to-addr   menu-entry
		ENDOF
		2 OF	\ spot variables
		    s" Select spot variable:" menu-title!
		    r@ >cont-item dup >stack  redisplay   @ spot-var-name
		    ['] select-spot-var-to-addr menu-entry
		ENDOF
	    ENDCASE

	    2 5 screen-column
	    \ >xy
	    r@ >cont-char simple-menu-entry-char ."   "

	    s" Choose color for this char:" menu-title!
	    ['] cont-display-colors  r@ >cont-foreground-xt  choose-xt-entry

	    (cont-var-type) @  type-df-addr%  = IF
		3 5 screen-column s" low: "  r@ >cont-lower
		simple-menu-entry-df-value
		4 5 screen-column s" high: " r@ >cont-upper
		simple-menu-entry-df-value
	    ELSE
		3 5 screen-column s" low: "  r@ >cont-lower
		simple-menu-entry-value
		4 5 screen-column s" high: " r@ >cont-upper
		simple-menu-entry-value
	    THEN
	THEN
	cr

	r>
    LOOP drop

    continuous-display-list nodes max-continuous-items# < IF
	cr
	<other-colour>
	s" add new entry"  redisplay  ['] add-continuous-entry	menu-entry cr
	s" ane" menu-same-key-entry
	reset-colours
    THEN

    cr
    s" Zoom up scale: "		redisplay
    ['] cont-zoom-up-scale simple-menu-entry-scale cr
    <common-menu-entries> ;

: menu-continuous-display ( -- )
    continuous-display-men
    ['] .menu-continuous-display menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop ;

\ ' menu-continuous-display function-key-actions >list

\ ****************************************************************
\ end	continuous display



\ ****************************************************************
\ ******  Text display: nuc-text-display world-text-display  *****
\ ****************************************************************

: nuc-text-display ( -- )			\ displays one line
    step @ (nucs-scanned-at-step) @ <> IF	\ data already scanned?
	do-a-nuc-scan
    THEN

    clear-line-to-end 0 at-x

    cp@
    (scan-detail) @	( cp@ nuc-var-index )
    dup nuc-var-name type
    3 0 DO			( cp@ nuc-var-index )
	(nuc-scan-data) @ nuc-length# i * + cp!
	i 18 * 24 + at-x
	i CASE
	    0 OF ." min: " ENDOF
	    1 OF ." max: " ENDOF
	    2 OF ." average: " ENDOF
	ENDCASE

[ nuc-floats# ] [IF]
	dup nuc-var-is-float? IF
	    dup nuc-dfloat-addr df@ float>short-string type
	ELSE
	    dup nuc-addr @ .
	THEN
[ELSE]
	dup nuc-addr @ .
[THEN]
    LOOP
    drop
    cp! ;
' nuc-text-display step-display-xt's >list

: world-text-display
    step @ (world-scanned-at-step) @ <> IF	\ data already scanned?
	do-spot-scan
    THEN

    clear-line-to-end 0 at-x

    (scan-detail) @
    dup spot-var-name type
    2* 2* cells (scan-spots) @ + >r	\ start data for this quality
    24 at-x ." min: "		r@		@ .
    42 at-x ." max: "		r@ cell+	@ .
    60 at-x ." average: "	r> 2 cells +	@ .  ;
' world-text-display step-display-xt's >list

\ ****************************************************************
\ end	text display



\ ****************************************************************
\ **********************  Step Display:  *************************
\ ****************************************************************
\ step display: scanning nuc or spot data or doing continuous display
\ This version allows multiple sub screens.

\ ****************************************************************
\ ******************  Step Display Menus:  ***********************
\ ****************************************************************
\ *******************  menu-step-presets  ************************
\ *******************  step-display-menu  ************************
\ ****************  menu-step-display-item  **********************
\ ******************   menu-step-display  ************************
\ ****************************************************************

VARIABLE step-display-items	2 step-display-items !

: step-display ( -- )
    0	\ actual starting line
    step-display-items @ 0 ?DO
	i (scan-index) !
	(scan-xt) @ dup IF

	    \ you want colors?
	    step-background-coloring? IF
		scan-background-xt @ EXECUTE color-background
	    THEN
	    dup ['] continuous-display <> IF
		step-foreground-coloring? IF
		    scan-foreground-xt @ EXECUTE color-foreground
		THEN
	    THEN

	    over 0 swap at-xy
	    EXECUTE

	    (scan-lines) @ +
	ELSE drop THEN
    LOOP
    drop ;

\ step display menu:
\ submenu  presets:
MENU: men-step-presets
: .step-display-presets ( -- )
    help-node" Step display presets"
    s" Presets for step display" menu-title-entry

    cr
    cr
    ." SORRY, NO PRESETS YET" cr

    <common-menu-entries> ;

: menu-step-presets ( -- )
    men-step-presets
    ['] .step-display-presets menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop ;

: displayed-items-name ( -- addr count)	\ (scan-index) must be set
    (scan-xt) @ CASE
	['] nuc-scan-display  OF
	    (scan-detail) @ nuc-var-name
	ENDOF
	['] spot-scan-display OF
	    (scan-detail) @ spot-var-name
	ENDOF
	['] nuc-text-display  OF
	    (scan-detail) @ nuc-var-name
	ENDOF
	['] world-text-display OF
	    (scan-detail) @ spot-var-name
	ENDOF
	['] nuc-scan-func-dspl OF
	    (scan-detail) @ xt>string
	ENDOF
	0 OF
	    s" (no display)"
	ENDOF
	cr ." displayed-items-name: Unknown " xt>string type ABORT
    ENDCASE ;

LIST: nuc-int-functions
' scoring nuc-int-functions >list
' score   nuc-int-functions >list

: select-nuc-int-function ( -- )
    ['] nuc-int-functions  ['] (scan-detail)  choose-xt-to-var ;

\ user interface to set '(scan-detail)' (according to displayed type)
: choose-displayed-item ( index -- )
    (scan-index) !
    (scan-xt) @ CASE
	['] nuc-scan-display  	OF select-scanned-nuc-var	ENDOF
	['] nuc-text-display  	OF select-scanned-nuc-var	ENDOF
	['] spot-scan-display 	OF |menu-select-spot-var|	ENDOF
	['] world-text-display	OF |menu-select-spot-var|	ENDOF
	['] continuous-display	OF menu-continuous-display	ENDOF
	['] nuc-scan-func-dspl  OF select-nuc-int-function	ENDOF
	0			OF bell				ENDOF
	cr ." choose-displayed-item: Unknown " xt>string type ABORT
    ENDCASE ;    

: step-display-lines ( -- lines ) 
    0
    step-display-items @ 0 ?DO
	i (scan-index) !
	(scan-xt) @ IF
	    (scan-lines) @ +
	THEN
    LOOP ;

\ Get number of fixed lines, current number of adaptable lines and
\ a flag if the step display item is adaptable in size.
\ The fixed line count includes a minimal display area.
: step-display-item-sizes ( -- fixed-lines adaptable-lines adaptable-flag )
    \ (scan-index) must be set
    (scan-xt) @ CASE
	['] nuc-scan-display	OF  2  TRUE  ENDOF
	['] spot-scan-display	OF  2  TRUE  ENDOF
	['] continuous-display	OF  2  TRUE  ENDOF
	['] nuc-text-display	OF  1  FALSE ENDOF
	['] world-text-display	OF  1  FALSE ENDOF
	['] nuc-scan-func-dspl  OF  2  TRUE  ENDOF
	0			OF  0  FALSE ENDOF	\ undefined
	bell
	cr ." step-display-item-sizes: Unknown step display type "
	xt>string type
	ABORT
    ENDCASE
    >r
    r@ IF
	(scan-lines) @  over -
    ELSE
	0
    THEN
    r> ;

: item-adaptable? ( -- flag )   step-display-item-sizes nip nip ;

: step-display-used-fixed ( -- fixed-lines )
    0
    step-display-items @ 0 ?DO	( lines )
	i (scan-index) !
	step-display-item-sizes 2drop
	+
    LOOP ;

: used-adaptable-lines ( -- adaptable-lines )
    0
    step-display-items @ 0 ?DO	( lines )
	i (scan-index) !
	step-display-item-sizes drop nip
	+
    LOOP ;

: available-adaptable-lines ( -- lines )
    l-s 1-				\ total-area
    step-display-used-fixed - ;

: step-display-adaptable-items ( -- adaptable-items-count )
    0
    step-display-items @ 0 ?DO	( count )
	i (scan-index) !
	step-display-item-sizes nip nip
	IF 1+ THEN
    LOOP ;

\ Increase or decrease adaptional step display item sizes
\ conserving relative proportions.
\ The result will more or less, but not exactly fill the screen.
: adapt-proportional ( -- )
    available-adaptable-lines
    dup 0= IF  drop EXIT  THEN
    s>f

    used-adaptable-lines
    dup 0= IF  drop fdrop EXIT  THEN
    s>f

    fover fover f= IF  fdrop fdrop EXIT  THEN
    
    f/
    step-display-items @ 0 ?DO	\ ( F: factor )
	i (scan-index) !
	step-display-item-sizes ( fixed adaptable-lines adaptable-flag F: fact)
	IF
	    + s>f fover f* 5e-1 f+
	    f>s (scan-lines) !
	ELSE
	    2drop
	THEN
    LOOP
    fdrop ;

: fine-tune-size ( -- )
    step-display-adaptable-items 0= IF  EXIT  THEN

    l-s 1-
    step-display-lines -		( diff )
    dup 0= IF  drop EXIT  THEN

    BEGIN
	\ bottom to top loop parameters
	0
	step-display-items @   dup 0= IF  2drop EXIT  THEN
	1-	\ starting with last item
	DO	( diff )
	    i (scan-index) !
	    item-adaptable? IF
		dup 0< IF -1 ELSE 1 THEN
		dup (scan-lines) +!
		-
		dup 0= IF
		    drop UNLOOP EXIT
		THEN
	    THEN
	-1 +LOOP
    AGAIN ;

: step-display-adapt-size ( -- )   adapt-proportional  fine-tune-size ;

\ count undefined step display items
: step-display-undefined ( -- undefined-items )
    0
    step-display-items @ 0 ?DO
	i (scan-index) !
	(scan-xt) @ 0= IF
	    1+
	THEN
    LOOP ;

: ?step-display-sanity ( -- )
    \ reset 'continuous-display-used' 'scan-display-used' 'text-display-used':
    display-switch dup @
    [ continuous-display-used scan-display-used OR text-display-used OR invert
    ] literal  and swap !
    step-display-on? 0= IF EXIT THEN

    step-display-items dup @ max-step-display-items# min 1 max swap !

    0
    step-display-items @ 0 ?DO
	i (scan-index) !
	(scan-xt) @ dup IF
	    \ set continuous-display-used scan-display-used text-display-used
	    \ and check if '(scan-detail)' is within range:
	    CASE \ on xt
		['] continuous-display OF
		    display-switch dup @ continuous-display-used or swap !
		    check-continuous-display-sanity	\ convenient place...
		    lowest-integer#	highest-integer#
		ENDOF
		['] nuc-scan-display  OF
		    display-switch dup @ scan-display-used or swap !
		    nuc-variables	nuc-df-scan-limit 1-
		    \ ^ lower limit	^ upper limit
		ENDOF
		['] spot-scan-display OF
		    display-switch dup @ scan-display-used or swap !
		    p0		field-i-planes# field-df-planes# + 1- ( low up)
		ENDOF
		['] nuc-text-display  OF
		    display-switch dup @ text-display-used or swap !
		    nuc-variables	nuc-df-scan-limit 1-
		    \ ^ lower limit	^ upper limit
		    1 (scan-lines) !	\ just convenient to do that here
		ENDOF
		['] world-text-display OF
		    display-switch dup @ text-display-used or swap !
		    p0		field-i-planes# field-df-planes# + 1- ( low up)
		    1 (scan-lines) !	\ just convenient to do that here
		ENDOF
		['] nuc-scan-func-dspl OF
		    display-switch dup @ scan-display-used or swap !
		    lowest-integer#	highest-integer#
		ENDOF

		xt>string cr type
		true ABORT" ?step-display-sanity: unknown xt."
	    ENDCASE
	    (scan-detail) >r r@ @ min  swap max r> !

	    set-scan-var-type			\ set variable type flag:

	    \ check if '(scan-lines)' must be initialized:
	    (scan-lines) @ 0= IF
		last-line step-display-items @ / (scan-lines) !
	    THEN

	    \ check if 'horizontal-zoom-scale' must be initialized:
	    horizontal-zoom-scale 2@ 0= swap 0= or IF
		default-horizontal-zoom-scale horizontal-zoom-scale 2!
	    THEN

	    \ check if 'vertical-zoom-scale' must be initialized:
	    vertical-zoom-scale 2@ 0= swap 0= or IF
		default-vertical-zoom-scale vertical-zoom-scale 2!
	    THEN

	    \ check if 'scan-background-xt' must be initialized:
	    scan-background-xt @ 0= IF		\ we only check for zero
		['] default-color scan-background-xt !
	    THEN

	    \ check if 'scan-foreground-xt' must be initialized:
	    scan-foreground-xt @ 0= IF		\ we only check for zero
		['] default-color scan-foreground-xt !
	    THEN

	    (scan-lines) @ +	\ sum up lines
	ELSE drop THEN
	
    LOOP

    \ vertical size correction (here only if too big)
    last-line > IF
	step-display-adapt-size
    THEN
;


: define-step-display-item ( index -- )
    (scan-index) !
    s" Select type of this step display item:" menu-title!
    ['] step-display-xt's  ['] (scan-xt)  choose-xt-to-var
    (scan-xt) @ IF
	s" Select displayed parameter:" menu-title!
	(scan-index) @ choose-displayed-item
    THEN ;

: |define-step-display-item| ( -- )
    step-display-items @ 1 -		\ try to guess index ;-)
    define-step-display-item ;


: .message ( addr count -- )
    at? 2>r
    last-left clear-line-to-end last-left
    type-alert
    1000 wait-until
    last-left clear-line-to-end
    2r> at-xy ;

: .sorry ( -- )   s" Sorry, you can't change this value..." .message ;

: .sorry-compile-option ( -- )
    s" Sorry, you can't change this value now.  (It's a compile time option). "
    .message ;

MENU: step-display-men
DEFER toggle-step-display
VARIABLE (step-more-info)	(step-more-info) off  \ switches amount of info

\ factored out .menu-step-display parts:
: step-display-show-lines ( -- )		\ show lines, no selection
    3 5 screen-column
    s" lines:  "	['] .sorry	redisplay	menu-entry
    (scan-lines) @ . up-to-here cr ;

: lines-and-v-range-entry ( -- )
    3 5 screen-column
    s" lines:  "  (scan-lines)  simple-menu-entry-value
    3 4 screen-column
    s" v-range: " (vertical-display-range)  simple-menu-entry-value cr ;

: ?step-zoom-entries ( -- )
    (step-more-info) @ 0= IF EXIT THEN

    1 4 screen-column
    from-here
    s" zoom starts horiz: "  horizontal-zoom-scale change-scale-at-addr-entry
    .tab
    s" vertic: "	     vertical-zoom-scale change-scale-at-addr-entry
    .tab
    (scan-flags) @ fixed-horizontal-range and IF
	s" fixed"
    ELSE
	s" zooming"
    THEN
    fixed-horizontal-range >stack	(scan-flags) >stack-2
    redisplay	['] xor!	menu-entry
    cr ;

: ?step-color-entries ( -- )
    (step-more-info) @ 0= IF EXIT THEN

    1 4 screen-column
    s" Select background color for this item" menu-title!
    from-here ." bg color: "
    ['] scan-background-colors scan-background-xt choose-xt-entry
    3 5 screen-column
    s" Select foreground color for this item" menu-title!
    from-here ." fg: "
    ['] scan-foreground-colors scan-foreground-xt choose-xt-entry
    cr ;

: step-nuc-or-spot-scan-entries ( -- )
    from-here  ." scanning: " displayed-items-name  redisplay
    (scan-index) @ >stack	['] choose-displayed-item	menu-entry
    lines-and-v-range-entry
    ?step-zoom-entries
    ?step-color-entries ;

: .menu-step-display-item ( scan-xt -- )	\ factored out for readability
    dup 0= IF drop
	from-here <alert-colours> ." (undefined)"
	1 4 screen-column s" (define new display item)"
	(scan-index) @ >stack	['] define-step-display-item	menu-entry cr
	reset-colours
	EXIT
    THEN

    dup xt>string (scan-index) @ >stack
    ['] define-step-display-item  menu-entry

    1 4 screen-column
    CASE
	['] nuc-scan-display	OF step-nuc-or-spot-scan-entries ENDOF
	['] nuc-scan-func-dspl	OF step-nuc-or-spot-scan-entries ENDOF
	['] spot-scan-display	OF step-nuc-or-spot-scan-entries ENDOF
	['] continuous-display	OF
	    s" (edit continuous display)"
	    ['] menu-continuous-display	    redisplay	menu-entry
	    s" ce" menu-same-key-entry
	    lines-and-v-range-entry
	    ?step-zoom-entries
	    ?step-color-entries
	ENDOF
	['] nuc-text-display	OF
	    displayed-items-name	redisplay
	    (scan-index) @ >stack	['] choose-displayed-item   menu-entry
	    step-display-show-lines
	    ?step-color-entries
	ENDOF
	['] world-text-display	OF
	    displayed-items-name	redisplay
	    (scan-index) @ >stack	['] choose-displayed-item   menu-entry
	    step-display-show-lines
	    ?step-color-entries
	ENDOF
	cr ." .menu-step-display-item: Unknown step display type "
	xt>string type
	ABORT
    ENDCASE ;

\ Don't let the user leave step display as long as there are undefined items.
\ sorry for the ugly hack ;-)
VARIABLE (don't-leave)		(don't-leave) off	\ for display only
\ step-menu-leave? gets called when the user tries to leave the menu
\ It's the only way out of here...
: step-menu-leave? ( -- )	\ flag is 0, 1, 2, 3,...
    step-display-undefined IF
	1 (don't-leave) +!	\ remember if the user insists :-)
    ELSE
	(don't-leave) off
	1 menu-leave !
    THEN ;

: .menu-step-display ( -- )
    ?step-display-sanity		\ sets xxx-display-used bits

    help-node" Menu step display"
    s" Step display menu: " start-title-entry

    s"  Step display "	redisplay
    ['] toggle-step-display	menu-entry
    step-display-on? .ON-off-entry

    6 spaces from-here
    (step-more-info) @ IF ." Less" ELSE ." More" THEN
    s"  infos"  ['] (step-more-info) >stack	 redisplay
    ['] toggle-named menu-entry
    s" mlto+- " menu-same-key-entry
    end-title

    cr
    s" Items displayed in step display: "
    ['] step-display-items simple-menu-entry-variable
    s" in" menu-same-key-entry

    1 2 screen-column ." Color: "
    s" foreground "	redisplay
    ['] step-foreground-coloring >r  r@ >stack
    ['] display-switch >r  r@ >stack-2		['] named-xor!	menu-entry
    2r> @ and .ON-off-entry  ."  "
    s" f" menu-same-key-entry

    s"  backgr "	redisplay
    ['] step-background-coloring >r  r@ >stack
    ['] display-switch >r  r@ >stack-2		['] named-xor!	menu-entry
    2r> @ and .ON-off-entry cr
    s" b" menu-same-key-entry
    
    cr
    (step-more-info) @ IF 20 ELSE 9 THEN keep-but-scroll-rest
    step-display-items @ 0 scrolled-range ?DO
	i (scan-index) !
	(scan-xt) @ .menu-step-display-item
    LOOP

    \ add or remove entries:
    step-display-items @ >r
    r@ max-step-display-items# <   r@ 1 > OR IF
	cr
	r@  max-step-display-items# < IF
	    <other-colour>
	    s" add new item.	    "	redisplay	do-after  1 >stack
	    ['] step-display-items >stack-2	['] named+!	menu-entry
	    s" an+" menu-same-key-entry
	    reset-colours
	THEN
	r@  1 > IF
	    <other-colour>
	    s" remove last item."	redisplay	-1 >stack
	    ['] step-display-items >stack-2	['] named+!	menu-entry
	    reset-colours
	    s" r-" menu-same-key-entry
	THEN
	.tab .tab
	<other-colour>
	s" presets"	redisplay	['] menu-step-presets	menu-entry cr
	s" pP" menu-same-key-entry
	reset-colours
    THEN rdrop

    \ check if theres not more than one continuous display:
    continuous-display-used? IF
	cr
	0
	step-display-items @ 0 ?DO	\ ( continuous-display-counter)
	    i (scan-index) !
	    (scan-xt) @ ['] continuous-display = IF 1+ THEN
	LOOP
	1 > IF
	    bell
	    <other-colour>
	    s" Please set just *one* continuous display."
	    ping	['] noop menu-entry cr
	    reset-colours
	THEN
    THEN

    \ is the screen filled?
    last-line step-display-lines -
    dup 0> IF
	display-switch @
	[ scan-display-used continuous-display-used OR ] literal and IF
	    <other-colour>	s" fill the screen ("
	    ['] step-display-adapt-size	redisplay	menu-entry
	    dup . ." lines unused)." up-to-here cr
	    reset-colours
	    s" sf=" menu-same-key-entry
	THEN
     THEN drop

     \ insist on defining all items
     step-display-undefined IF
	 (don't-leave) @ dup IF
	     cr s" Please define undefined items." type-alert cr
	     dup 1 > IF bell THEN	\ nerve the user 
	 THEN
	 drop
     ELSE
	 (don't-leave) off		\ cosmetics
     THEN

     <common-menu-entries>
     [char] q  redisplay	['] step-menu-leave?	#key-menu-entry
;

: menu-step-display ( -- )
    (don't-leave) off	\ for display only
    step-display-men
    ['] |define-step-display-item| to-do-after-xt !
    ['] .menu-step-display menu-display-xt !
    ['] step-menu-leave?	redisplay	menu-key-default
    ['] step-menu-leave?	redisplay	menu-default
    do-menu-loop
    (don't-leave) off	\ just in case
;
' menu-step-display function-key-actions >list

\ toggle-step-display ( -- )
:NONAME ( -- )
    display-switch >r
    r@ @ step-display-on xor r@ !
    r> @ step-display-on and IF			\ step display switched on
	menu-step-display			\ allow setup
    THEN ; IS toggle-step-display

\ Toggle between spot and step display:
\ (see: 'switch-display-type' for toggling between real time and snapshots).
VARIABLE (prior-display-type)	spot-display-on (prior-display-type) !
\ possible values: 'spot-display-on', 'step-display-on'.
: toggle-display-type ( -- )
    display-switch dup @
    [ spot-display-on step-display-on or ] literal >r
    r@ xor
    dup r@ and 0=			\ neither step or spot display on
    over r@ and r@ =			\ step AND spot display on
    or IF
	[ spot-display-on step-display-on or invert ] literal and
	(prior-display-type) @ or	\ back to last single selected
    THEN
    dup r> and (prior-display-type) !
    swap !
    ?reset-continuous-column
    ?step-display-sanity ;

: step-snapshot-on ( -- )
    step-snapshots!
    display-switch dup @
    [ step-display-on spot-display-on spot-snapshots or or invert ] literal and
    swap ! ;

: spot-snapshot-on ( -- )
    spot-snapshots!
    display-switch dup @
    [ step-display-on spot-display-on step-snapshots or or invert ] literal and
    swap ! ;

: display-off ( -- )
    display-switch dup @
    [ step-display-on spot-display-on spot-snapshots step-snapshots or or or
    invert ] literal and
    swap ! ;

: step-OR-spot
    display-switch @ >r			( -- r: actual-display-switch )
    spot-display-on step-display-on or	( display-type-mask    r: switch)
    r@ and				( display-type         r: switch)
    CASE \ type
	spot-display-on OF toggle-display-type ENDOF
	step-display-on OF toggle-display-type ENDOF
	\ else we put spot display on and go to step display setup:
	r@ spot-display-on or	step-display-on invert and   display-switch !
    ENDCASE rdrop

    step-display-on? IF
	menu-step-display			\ allow setup
    THEN ;

\ ****************************************************************
\ end	step display



\ ****************************************************************
\ ********************  menu-nuc-subsets  ************************
\ ****************************************************************

MAYBE-DO-FIELD: maybe-do-on-subset-field
' genome-id (expr-xt-1) !			\ just a default
' energy (expr-xt-2) !
init-df-expr-xts-nuc
init-df-do-xts-nuc
' energy (xt-do-it) !
' maybe-do-simple (maybe-do-type-xt) !

: toggle-do-type ( maybe-do-field-body -- ) \ ' maybe-do  or  ' maybe-do-simple
    TO (maybe-do-field)

    (maybe-do-type-xt)
    dup @ ['] maybe-do-simple = IF
	['] maybe-do
    ELSE
	['] maybe-do-simple
    THEN
    swap ! ;

: maybe-do-type-entry ( -- )
    (maybe-do-type-xt) @ ['] maybe-do = IF
	s" Extended condition:  "
    ELSE
	s" Simple condition:    "
    THEN
    redisplay	(maybe-do-field) >stack	 ['] toggle-do-type	menu-entry ;

: choose-nuc-var-xt-entry ( addr -- )
    s" Choose nuc variable:" menu-title!
    from-here ."   "
    ['] integer-nuc-vars swap choose-xt-entry ;

: choose-df-nuc-var-xt-entry ( addr -- )
    s" Choose nuc variable:" menu-title!
    from-here ."   "
    ['] dfloat-nuc-vars swap choose-xt-entry ;

LIST: dfloat-nuc-functions

: choose-df-nuc-funct-xt-entry ( addr -- )
    s" Choose nuc local function:" menu-title!
    from-here ."   "
    ['] dfloat-nuc-functions swap choose-xt-entry ;

: choose-spot-var-xt-entry ( addr -- )
    s" Choose spot variable:" menu-title!
    from-here ."   "
    ['] integer-spot-vars swap choose-xt-entry ;

: choose-df-spot-var-xt-entry ( addr -- )
    s" Choose spot variable:" menu-title!
    from-here ."   "
    ['] dfloat-spot-vars swap choose-xt-entry ;

LIST: dfloat-spot-functions

: choose-df-spot-funct-xt-entry ( addr -- )
    s" Choose spot local function:" menu-title!
    from-here ."   "
    ['] dfloat-spot-functions swap choose-xt-entry ;

\ The all-lists get re-built on each usage to make sure it is up to date
LIST: (all-int-var-xts)
: all-int-var-xts ( -- list )
    (all-int-var-xts) >r
    integer-spot-vars integer-nuc-vars 2 r@ sum-lists-simple
    r> ;

: choose-nuc&spot-var-xt-entry ( addr -- )
    s" Choose nuc or spot variable:" menu-title!
    from-here ."   "
    ['] all-int-var-xts swap choose-xt-entry ;

\ The all-lists get re-built on each usage to make sure it is up to date
LIST: (all-dfloat-var-xts)
: all-dfloat-var-xts ( -- list )
    (all-dfloat-var-xts) >r
    dfloat-spot-vars dfloat-nuc-vars 2 r@ sum-lists-simple
    r> ;

: choose-nuc&spot-df-var-xt-entry ( addr -- )
    s" Choose nuc or spot variable:" menu-title!
    from-here ."   "
    ['] all-dfloat-var-xts swap choose-xt-entry ;

LIST: (all-dfloat-funct-xts)
: all-dfloat-functs-xts ( -- list )
    (all-dfloat-funct-xts) >r
    dfloat-spot-functions dfloat-nuc-functions 2 r@ sum-lists-simple
    r> ;

: choose-n&s-df-funct-xt-entry ( addr -- )
    s" Choose nuc or spot function:" menu-title!
    from-here ."   "
    ['] all-dfloat-functs-xts swap choose-xt-entry ;

VARIABLE (selection-mask)	(selection-mask) off
0
MASK: select-nuc-related
MASK: select-spot-related
drop

: nuc-related-selections ( -- )   select-nuc-related (selection-mask) ! ;

: spot-related-selections ( -- )  select-spot-related (selection-mask) ! ;

: nuc&spot-related-selections ( -- )
    [ select-nuc-related select-spot-related or ] literal
    (selection-mask) ! ;

: choose-var-xt-entry ( addr -- )
    (selection-mask) @ >r

    r@ [ select-nuc-related select-spot-related or dup ] literal and
    literal = IF
	choose-nuc&spot-var-xt-entry
    ELSE
	r@ select-nuc-related and IF
	    choose-nuc-var-xt-entry
	ELSE
	    \ Check for exception, where nuc variables are possible:
	    (maybe-do-type-xt) @  ['] maybe-do-simple =
	    (simple-expression-xt) @ ['] inhabited? = AND IF	\ exception:
		choose-nuc&spot-var-xt-entry	\ nuc variables are possible
	    ELSE
		choose-spot-var-xt-entry
	    THEN
	THEN
    THEN
    rdrop ;

: choose-nuc-function-xt-entry ( -- )
    s" Choose functions to be run in nucs:" menu-title!
    from-here ."   "
    ['] nuc-int-functions swap choose-xt-entry ;

LIST: spot-int-functions

: choose-spot-function-xt-entry ( -- )
    s" Choose functions to be run on spots:" menu-title!
    from-here ."   "
    ['] spot-int-functions swap choose-xt-entry ;

\ The all-lists get re-built on each usage to make sure it is up to date
LIST: (all-int-functions)
: all-int-functions ( -- list )
    (all-int-functions) >r
    spot-int-functions nuc-int-functions 2 r@ sum-lists-simple
    r> ;

: choose-n&s-function-xt-entry ( -- )
    s" Choose nuc or spot function:" menu-title!
    from-here ."   "
    ['] all-int-functions swap choose-xt-entry ;

: choose-function-xt-entry ( addr -- )
    (selection-mask) @ >r

    r@ [ select-nuc-related select-spot-related or dup ] literal and
    literal = IF
	choose-n&s-function-xt-entry
    ELSE
	r@ select-nuc-related and IF
	    choose-nuc-function-xt-entry
	ELSE
	    \ Check for exception, where nuc variables are possible:
	    (maybe-do-type-xt) @  ['] maybe-do-simple =
	    (simple-expression-xt) @ ['] inhabited? = AND IF	\ exception:
		choose-n&s-function-xt-entry	\ nuc variables are possible
	    ELSE
		choose-spot-function-xt-entry
	    THEN
	THEN
    THEN
    rdrop ;

: choose-df-var-xt-entry ( addr -- )
    (selection-mask) @ >r

    r@ [ select-nuc-related select-spot-related or dup ] literal and
    literal = IF
	choose-nuc&spot-df-var-xt-entry
    ELSE
	r@ select-nuc-related and IF
	    choose-df-nuc-var-xt-entry
	ELSE
	    \ Check for exception, where nuc variables are possible:
	    (maybe-do-type-xt) @  ['] maybe-do-simple =
	    (simple-expression-xt) @ ['] inhabited? = AND IF	\ exception:
		choose-nuc&spot-df-var-xt-entry	\ nuc variables are possible
	    ELSE
		choose-df-spot-var-xt-entry
	    THEN
	THEN
    THEN
    rdrop ;

: choose-df-funct-xt-entry ( addr -- )
    (selection-mask) @ >r

    r@ [ select-nuc-related select-spot-related or dup ] literal and
    literal = IF
	choose-n&s-df-funct-xt-entry
    ELSE
	r@ select-nuc-related and IF
	    choose-df-nuc-funct-xt-entry
	ELSE
	    \ Check for exception, where nuc variables are possible:
	    (maybe-do-type-xt) @  ['] maybe-do-simple =
	    (simple-expression-xt) @ ['] inhabited? = AND IF	\ exception:
		\ nuc variables are possible
		choose-n&s-df-funct-xt-entry
	    ELSE
		choose-df-spot-funct-xt-entry
	    THEN
	THEN
    THEN
    rdrop ;



LIST: simple-expressions-all
\ simple-expressions-nuc  simple-expressions-all copy-simple-list-elements
\ simple-expressions-spot simple-expressions-all copy-simple-list-elements

\ Must be preceded by one of these:
\	'nuc-related-selections'
\	'spot-related-selections'
\	'nuc&spot-related-selections' 
: choose-simple-condition-entry ( -- )
    (selection-mask) @ >r

    s" Choose condition:" menu-title!
    r@ [ select-nuc-related select-spot-related or dup ] literal and
    literal = IF
	['] simple-expressions-all
    ELSE r@ select-nuc-related and IF
	['] simple-expressions-nuc
    ELSE
	['] simple-expressions-spot
    THEN THEN
    ['] (simple-expression-xt) choose-xt-to-var-entry
    rdrop ;

\ As a side effect of the menu entry I do check consistency:
: needs-float-condition? ( -- flag )	\ helper word
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF  FALSE EXIT  ENDOF
	['] maybe-do	    OF
	    (expression-xt) @ CASE
		['] 2-variables		OF  FALSE EXIT  ENDOF
		['] variable-number	OF  FALSE EXIT  ENDOF
		['] variable-within	OF  FALSE EXIT  ENDOF
		['] evaluate-expr	OF  FALSE EXIT  ENDOF
		['] 2-df-variables	OF  TRUE  EXIT  ENDOF
		['] df-variable-number	OF  TRUE  EXIT  ENDOF
		['] df-variable-within	OF  FALSE EXIT  ENDOF
		['] df-function-number	OF  TRUE  EXIT  ENDOF
		['] df-function-within	OF  FALSE EXIT  ENDOF
		['] evaluate-df-expr	OF  TRUE  EXIT  ENDOF
		['] df-var-real?	OF  FALSE EXIT  ENDOF
		['] df-var-inf?		OF  FALSE EXIT  ENDOF
		['] df-var-pos-inf?	OF  FALSE EXIT  ENDOF
		['] df-var-neg-inf?	OF  FALSE EXIT  ENDOF
		['] df-var-nan?		OF  FALSE EXIT  ENDOF
		['] function-number	OF  FALSE EXIT  ENDOF
		['] function-within	OF  FALSE EXIT  ENDOF
		cr ." needs-float-condition?: Unknown expression "
		xt>string type
		ABORT
	    ENDCASE
	ENDOF
	true ABORT" needs-float-condition?: Unknown type."
    ENDCASE ;

\ Word to call in situations where the user edits a maybe-do field.
\ Make some needed consistency checks before first usage of the field.
: maybe-fix-condition ( -- )
    needs-float-condition?
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF  drop EXIT  ENDOF
	['] maybe-do OF
	    (condition-xt) @ CASE
		['] <   OF   IF ['] f< (condition-xt) !    THEN	  ENDOF
		['] >   OF   IF ['] f> (condition-xt) !    THEN	  ENDOF
		['] =   OF   IF ['] f= (condition-xt) !    THEN   ENDOF
		['] <>  OF   IF ['] f<> (condition-xt) !   THEN	  ENDOF
		['] >=  OF   IF ['] f>= (condition-xt) !   THEN	  ENDOF
		['] <=  OF   IF ['] f<= (condition-xt) !   THEN	  ENDOF

		['] f<  OF   0= IF ['] < (condition-xt) !  THEN	  ENDOF
		['] f>  OF   0= IF ['] > (condition-xt) !  THEN	  ENDOF
		['] f=  OF   0= IF ['] = (condition-xt) !  THEN	  ENDOF
		['] f<> OF   0= IF ['] <> (condition-xt) ! THEN	  ENDOF
		['] f>= OF   0= IF ['] >= (condition-xt) ! THEN	  ENDOF
		['] f<= OF   0= IF ['] <= (condition-xt) ! THEN	  ENDOF
		true ABORT" maybe-fix-condition: Unknown condition."
	    ENDCASE
	ENDOF
	true ABORT" maybe-fix-condition: Unknown type."
    ENDCASE ;

: determine-types ( xt -- locality-code type-code true | false )
    >r

    r@ integer-nuc-vars		listed?
    IF rdrop nuc-local% type-int-addr% TRUE EXIT THEN

    r@ dfloat-nuc-vars		listed?
    IF rdrop nuc-local% type-df-addr%  TRUE EXIT THEN

    r@ integer-spot-vars	listed? IF
	rdrop spot-local% type-int-addr% TRUE EXIT
    THEN

    r@ dfloat-spot-vars		listed? IF
	rdrop spot-local% type-df-addr%  TRUE EXIT
    THEN

    r@ nuc-int-functions	listed? IF
	rdrop nuc-local% type-int%      TRUE EXIT
    THEN

    r@ spot-int-functions	listed? IF
	rdrop spot-local% type-int%      TRUE EXIT
    THEN

    r@ global-int-variables	listed? IF
	rdrop global-locality% type-int-addr% TRUE EXIT
    THEN

    r@ global-dfloat-variables	listed? IF
	rdrop global-locality% type-df-addr%  TRUE EXIT
    THEN

    \ (ABORT can be removed later on)
    cr ." determine-type: Unknown " r@ xt>string type  ABORT

    rdrop
    FALSE ;

false [if] \ testing
: .type ( xt -- )
    dup >r
    determine-types IF
	rdrop
	var-type-string type ."  " locality-string type
    ELSE
	 ." don't know "  r> xt>string
    THEN ;
[then]

: determine-type ( xt -- var-type true | false)
    determine-types 0= IF false EXIT THEN

    nip TRUE ;

: type-mismatch? ( --  FALSE | expr-type condition-type TRUE )
    (maybe-do-type-xt) @  ['] maybe-do-simple  = IF  FALSE EXIT  THEN

    (expr-xt-1) @
    determine-type 0= IF FALSE EXIT THEN		\ unknown, maybe OK
    \							( expr-type )

    (expression-xt) @ CASE
	['] function-within	OF  type-int%       ENDOF
	['] function-number	OF  type-int%       ENDOF
	['] 2-variables		OF  type-int-addr%  ENDOF
	['] variable-number	OF  type-int-addr%  ENDOF
	['] variable-within	OF  type-int-addr%  ENDOF
	['] 2-df-variables	OF  type-df-addr%   ENDOF
	['] df-variable-number	OF  type-df-addr%   ENDOF
	['] df-variable-within	OF  type-df-addr%   ENDOF
	['] df-function-number	OF  type-df%	    ENDOF
	['] df-function-within	OF  type-df%	    ENDOF
	['] df-var-real?	OF  type-df-addr%   ENDOF
	['] df-var-inf?		OF  type-df-addr%   ENDOF
	['] df-var-pos-inf?	OF  type-df-addr%   ENDOF
	['] df-var-neg-inf?	OF  type-df-addr%   ENDOF
	['] df-var-nan?		OF  type-df-addr%   ENDOF
	['] evaluate-expr	OF  type-unknown%   ENDOF
	['] evaluate-df-expr	OF  type-unknown%   ENDOF

	type-unknown% swap
    ENDCASE	( expr-type condition-type )

    
    dup type-unknown% = IF 2drop FALSE EXIT THEN		\ maybe OK
    2dup = IF 2drop FALSE EXIT THEN				\ OK

    TRUE ;	( -- expr-type condition-type TRUE )		\ mismatch

\ fix type mismatches by supplying a matching default dummy
fvariable dummy		0e0 dummy df!	\ for type-int-addr% and type-df-addr%
: dummy-int ( -- dummy )   dummy @ ;	\ type-int-addr%
: dummy-float ( -- F: dummy )   dummy df@ ; \ type-df%

: maybe-fix-type-mismatch ( -- )
    type-mismatch? IF	\ hack hack ugly hack!
	(expr-xt-1) @ >r
	(expr-df-xt-1) @ >r
	nip ( expr-type ) CASE
	    type-int% OF
		(selection-mask) @ select-nuc-related and IF
		    ['] score
		ELSE
		    ['] dummy-int
		THEN
		(expr-xt-1) !
	    ENDOF
	    type-int-addr% OF
		(selection-mask) @ select-nuc-related and IF
[ nuc-organs# ] [IF]	   ['] organ-A     [ELSE]   ['] dummy   [THEN]
		ELSE
[ spot-qualities# ] [IF]   ['] A-quality   [ELSE]   ['] dummy   [THEN]
		THEN
		(expr-xt-1) !
	    ENDOF
	    type-df% OF
		['] dummy-float  (expr-df-xt-1) !
	    ENDOF
	    type-df-addr% OF
		(selection-mask) @ select-nuc-related and IF
[ nuc-f-organs# ] [IF]	   ['] f-organ-A     [ELSE]   ['] dummy   [THEN]
		ELSE
[ spot-f-qualities# ] [IF] ['] A-f-quality   [ELSE]   ['] dummy   [THEN]
		THEN
		(expr-df-xt-1) !
	    ENDOF

	    cr ." maybe-fix-type-mismatch: Unknown " var-type-string type
	    ABORT
	ENDCASE

	(expr-df-xt-1) @  r@ <> IF
	    (expr-df-xt-1) @  r>  TRUE
	    rdrop
	ELSE
	    rdrop
	    (expr-xt-1) @  r@ <> IF
		(expr-xt-1) @  r>  TRUE
	    ELSE
		rdrop FALSE
	    THEN
	THEN
	IF
	    bell
	    page cr cr cr
	    s" Type mismatch, replaced " type-other-colour
	    xt>string			 type-other-colour
	    s"  by "			 type-other-colour
	    xt>string			 type-alert cr
	    1200 wait-until
	THEN
    THEN ;

\ Choose expression and check for consistency
: choose-expression ( -- )
    s" Choose type of condition expression." menu-title!
    ['] maybe-do-expressions ['] (expression-xt) choose-xt-to-var

    \ The user may have switched between variable and function testing
    maybe-fix-type-mismatch ;


\ Must be preceded by one of these:
\	'nuc-related-selections'
\	'spot-related-selections'
\	'nuc&spot-related-selections' 
\ As a side effect of the menu entry I do check consistency:
: conditional-expression-entries ( -- )
    maybe-fix-condition			\ check consistency as side effect

    (maybe-do-type-xt) @		\ must be set properly.
    CASE
	['] maybe-do-simple	OF	\ only expression
	    choose-simple-condition-entry
	ENDOF
	['] maybe-do		OF	\ expression and condition
	    (expression-xt) @ xt>string ['] choose-expression menu-entry

	    s" ="  redisplay	['] = >stack	['] (condition-xt) >stack-2
	    ['] name-named! menu-key-entry
	    s" <"  redisplay	['] < >stack	['] (condition-xt) >stack-2
	    ['] name-named! menu-key-entry
	    s" >"  redisplay	['] > >stack	['] (condition-xt) >stack-2
	    ['] name-named! menu-key-entry

	    (expression-xt) @ CASE
		['] 2-variables		OF
		    (expr-xt-1) choose-var-xt-entry
		    (expr-xt-2) choose-var-xt-entry
		ENDOF
		['] variable-number	OF
		    (expr-xt-1) choose-var-xt-entry
		    s"   "  ['] (expr-parameter)  simple-menu-entry-variable
		ENDOF
		['] variable-within	OF
		    (expr-xt-1) choose-var-xt-entry
		    s"   " ['] (expr-parameter)   simple-menu-entry-variable
		    s"  "  ['] (expr-parameter-2) simple-menu-entry-variable
		    s"  WITHIN true"	noop-entry
		ENDOF
		['] evaluate-expr	OF
		    from-here ."   EVALUTE " [char] " emit
		    (expression-handle) @ string@  redisplay
		    (maybe-do-field) >stack
		    ['] write-evaluate-buffer-expr menu-entry
		    [char] " emit up-to-here
		ENDOF
		['] 2-df-variables		OF
		    (expr-df-xt-1) choose-df-var-xt-entry
		    (expr-df-xt-2) choose-df-var-xt-entry
		ENDOF
		['] df-variable-number	OF
		    (expr-df-xt-1) choose-df-var-xt-entry
		    s"   "  ['] (expr-df-parameter)
		    simple-dfloat-variable-entry
		ENDOF
		['] df-variable-within	OF
		    (expr-df-xt-1) choose-df-var-xt-entry
		    s"   " ['] (expr-df-parameter)
		    simple-dfloat-variable-entry
		    s"  "  ['] (expr-df-parameter-2)
		    simple-dfloat-variable-entry
		    s"  FWITHIN true"	noop-entry
		ENDOF
		['] df-function-number	OF
		    (expr-df-xt-1) choose-df-funct-xt-entry
		    s"   "  ['] (expr-df-parameter)
		    simple-dfloat-variable-entry
		ENDOF
		['] df-function-within	OF
		    (expr-df-xt-1) choose-df-funct-xt-entry
		    s"   " ['] (expr-df-parameter)
		    simple-dfloat-variable-entry
		    s"  "  ['] (expr-df-parameter-2)
		    simple-dfloat-variable-entry
		    s"  FWITHIN true"	noop-entry
		ENDOF
		['] evaluate-df-expr	OF
		    from-here ."   EVALUTE " [char] " emit
		    (expression-handle) @ string@  redisplay
		    (maybe-do-field) >stack
		    ['] write-evaluate-buffer-expr menu-entry
		    [char] " emit up-to-here
		ENDOF
		['] df-var-real?	OF
		    (expr-df-xt-1) choose-df-var-xt-entry
		ENDOF
		['] df-var-inf?		OF
		    (expr-df-xt-1) choose-df-var-xt-entry
		ENDOF
		['] df-var-pos-inf?	OF
		    (expr-df-xt-1) choose-df-var-xt-entry
		ENDOF
		['] df-var-neg-inf?	OF
		    (expr-df-xt-1) choose-df-var-xt-entry
		ENDOF
		['] df-var-nan?		OF
		    (expr-df-xt-1) choose-df-var-xt-entry
		ENDOF
		['] function-within	OF
		    (expr-xt-1) choose-function-xt-entry
		    s"   " ['] (expr-parameter)   simple-menu-entry-variable
		    s"  "  ['] (expr-parameter-2) simple-menu-entry-variable
		    s"  WITHIN true"	noop-entry
		ENDOF		    
		['] function-number	OF
		    (expr-xt-1) choose-function-xt-entry
		    s"   "  ['] (expr-parameter)  simple-menu-entry-variable
		ENDOF

		cr ." conditional-expression-entries: Unknown expression "
		xt>string type
		ABORT
	    ENDCASE

	    s" Choose condition:" menu-title!
	    from-here ."   "
	    ['] condition-words ['] (condition-xt) choose-xt-to-var-entry
	    ."  " up-to-here
	ENDOF
	true ABORT" conditional-expression-entries: Unknown function to do."
    ENDCASE ;

LIST: do-on-nuc-xts

\ Must be preceded by one of these:
\	'nuc-related-selections'
\	'spot-related-selections'
\	'nuc&spot-related-selections' 
: do-what-entry ( -- )
    s" Choose type of action:" menu-title!
    \ Which actions are possible in this context?
    (selection-mask) @ [ select-spot-related select-nuc-related or ] literal >r
    r@ and r> = IF
	['] do-on-nuc-xts
    ELSE
	(selection-mask) @ select-spot-related and IF
	    (maybe-do-type-xt) @  ['] maybe-do-simple =
	    (simple-expression-xt) @ ['] inhabited? = AND IF	\ exception
		['] do-on-nuc-xts
	    ELSE
		['] do-it-xt's
	    THEN
	ELSE
	    ['] do-on-nuc-xts
	THEN
    THEN
    ['] (do-it-xt) choose-xt-to-var-entry

    s" +" redisplay  ['] add-to-variable >stack		['] (do-it-xt) >stack-2
    ['] name-named! menu-key-entry
    s" -" redisplay  ['] sub-from-variable >stack	['] (do-it-xt) >stack-2
    ['] name-named! menu-key-entry
    s" */" redisplay ['] scale-variable >stack		['] (do-it-xt) >stack-2
    ['] name-named! menu-key-entry

    (do-it-xt) @ CASE
	\ cases without further parameters:
	['] noop		OF ENDOF
	['] remove-nuc		OF ENDOF
	['] select-nuc		OF ENDOF
	['] de-select-nuc	OF ENDOF
	['] toggle-selection	OF ENDOF
	\ cases that work on a nuc variable:
	['] set-variable	OF
	    (xt-do-it) choose-var-xt-entry
	    s"   to " noop-entry
	    s"  " ['] (do-it-parameter) simple-menu-entry-variable
	ENDOF
	['] add-to-variable	OF
	    (xt-do-it) choose-var-xt-entry
	    s"   + " noop-entry
	    s"  " ['] (do-it-parameter) simple-menu-entry-variable
	ENDOF
	['] sub-from-variable	OF
	    (xt-do-it) choose-var-xt-entry
	    s"   - " noop-entry
	    s"  "  ['] (do-it-parameter)  simple-menu-entry-variable
	ENDOF
	['] scale-variable	OF
	    (xt-do-it) choose-var-xt-entry
	    from-here ."   with "
	    s"  " ['] (do-it-scale) simple-menu-entry-scale
	ENDOF
	['] set-df-variable	OF
	    (df-xt-do-it) choose-df-var-xt-entry
	    s"   to " noop-entry
	    s"  " ['] (do-it-df-parameter) simple-dfloat-variable-entry
	ENDOF
	['] add-to-df-variable	OF
	    (df-xt-do-it) choose-df-var-xt-entry
	    s"   f+ " noop-entry
	    s"  " ['] (do-it-df-parameter) simple-dfloat-variable-entry
	ENDOF
	['] sub-from-df-variable OF
	    (df-xt-do-it) choose-df-var-xt-entry
	    s"   f- " noop-entry
	    s"  "  ['] (do-it-df-parameter)  simple-dfloat-variable-entry
	ENDOF
	['] multiply-df-variable OF
	    (df-xt-do-it) choose-df-var-xt-entry
	    from-here ."   with "
	    s"  " ['] (do-it-df-parameter) simple-dfloat-variable-entry
	ENDOF
	['] evaluate-do		OF
	    from-here ."   EVALUTE " [char] " emit
	    (maybe-do-handle) @ string@	redisplay
	    (maybe-do-field) >stack
	    ['] write-evaluate-buffer-do menu-entry
	    [char] " emit up-to-here
	ENDOF
	cr ." do-what-entry: Unknown action xt: " dup . .tab xt>string type cr
	TRUE ABORT
    ENDCASE ;

: scan-nuc-subset ( maybe-do-field-xt -- )
    EXECUTE					\ make maybe-do-field actual
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF
	    ['] simple-maybe-do-with-everybody
	ENDOF
	['] maybe-do	    OF
	    ['] maybe-do-with-everybody
	ENDOF
	true ABORT" scan-nuc-subset: Unknown type."
    ENDCASE
    scan-whom-xt!
    nuc-scan-menu
    reset-scan-whom ;

: scan-spot-subset ( maybe-do-field-xt -- )
    EXECUTE					\ make maybe-do-field actual
    (maybe-do-type-xt) @ CASE \ 'maybe-do-everywhere-generic' not fitting here.
	['] maybe-do-simple OF
	    ['] simple-maybe-do-everywhere
	ENDOF
	['] maybe-do	    OF
	    ['] maybe-do-everywhere
	ENDOF
	true ABORT" scan-spot-subset: Unknown type."
    ENDCASE
    scan-where-xt!
    spot-scan-menu
    reset-scan-where ;

\ Field used for programmable colouring functions:
MAYBE-DO-FIELD: fg-colour-field
' maybe-do (maybe-do-type-xt) !
' variable-number (expression-xt) !
' = (condition-xt) !
' genome-id (expr-xt-1) !
' genome-id (expr-xt-2) !
init-df-expr-xts-nuc
init-df-do-xts-nuc
' generic-hit>fg-color (do-it-xt) !
0 (expr-parameter) !

MAYBE-DO-FIELD: bg-colour-field
' maybe-do (maybe-do-type-xt) !
' variable-number (expression-xt) !
' = (condition-xt) !
' food (expr-xt-1) !
' food (expr-xt-2) !
init-df-expr-xts-spot
init-df-do-xts-spot
\ ' generic-hit>bg-color (do-it-xt) !	\ later on
0 (expr-parameter) !

: condition>fg-colour ( -- colour )
    (maybe-do-field) >r
    fg-colour-field
    (do-it-xt)  @ EXECUTE
    r> TO (maybe-do-field) ;
' condition>fg-colour x>fg-color >list

: condition>bg-colour ( -- colour )
    (maybe-do-field) >r
    bg-colour-field
    (do-it-xt)  @ EXECUTE
    r> TO (maybe-do-field) ;
' condition>bg-colour x>bg-color >list

VARIABLE colour-condition	\ only for 'display-map-menu' return
: ?set-as-colour-condition ( display-xt fg/bg-flag -- )
    colour-condition @ 0= IF 2drop EXIT THEN

    (maybe-do-field) >r
    preserve-maybe-do-field
    swap IF
	fg-colour-field
	spot-foreground-coloring!
	['] condition>fg-colour foreground-color-xt !
    ELSE
	bg-colour-field
	spot-background-coloring!
	['] condition>bg-colour background-color-xt !
    THEN
    restore-maybe-do-field
    ( display-xt ) (do-it-xt) !		\ convenient place to store that
    r> TO (maybe-do-field) ;

: ?set-as-fg-colour-condition ( display-xt -- )
    TRUE ?set-as-colour-condition ;

: ?set-as-bg-colour-condition ( display-xt -- )
    FALSE ?set-as-colour-condition ;

2VARIABLE (2map)	\ holding 2 map data (and xt) items
DEFER display-map-menu ( display-xt -- )
: display-map ( -- )   (2map) 2@ EXECUTE ;
: |show-fg-coloured-on-hit| ( maybe-do-field-xt -- )
    ['] show-fg-coloured-on-hit (2map) 2!
    help-node" Colouring on equality"
    ['] display-map display-map-menu
    ['] generic-hit>fg-color true ?set-as-colour-condition ;

: |show-fg-coloured-on-range| ( maybe-do-field-xt -- )
    ['] show-fg-coloured-on-range (2map) 2!
    help-node" Colouring on difference"
    ['] display-map display-map-menu
    ['] generic-range>fg-color true ?set-as-colour-condition ;

\ Show a 'below' 'WITHIN' 'above' type map with a single value as border.
\ (note that some parts of the maybe-do fields must be set properly before). 
: |show-fg-coloured-hit-diff| ( maybe-do-field-xt -- )
    >r
    preserve-maybe-do-field

    ['] variable-number  (expression-xt) !
    r> |show-fg-coloured-on-range|

    restore-maybe-do-field ;

\ Only on integers.  Probably obsolete.
: show-coloured-on-nuc-var-eq ( value nuc-var-xt -- )
    (maybe-do-field) >r		\ don't change active field

    fg-colour-field			\ don't reset field (for reuse).
    ['] maybe-do (maybe-do-type-xt) !
    ['] variable-number (expression-xt) !
    ['] = (condition-xt) !
    (expr-xt-1) !
    (expr-parameter) !

    ['] noop |show-fg-coloured-on-hit|	\ ' noop as dummy ' maybe-do-xxx-field 

    r> to (maybe-do-field) ;

\ Only on integers.  Probably obsolete.
: show-coloured-on-nuc-var-diff ( value nuc-var-xt -- )
    (maybe-do-field) >r		\ don't change active field

    fg-colour-field			\ don't reset field (for reuse).
    ['] maybe-do (maybe-do-type-xt) !
    ['] variable-number (expression-xt) !
    ['] = (condition-xt) !
    (expr-xt-1) !
    (expr-parameter) !

    ['] noop |show-fg-coloured-hit-diff|    \ noop as dummy maybe-do-xxx-field 

    r> to (maybe-do-field) ;

\ How many nucs fit the condition?
: count-fitting-nucs ( -- n )
    0
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF
	    ['] 1+ simple-maybe-do-with-everybody
	ENDOF
	['] maybe-do	    OF
	    ['] 1+ maybe-do-with-everybody
	ENDOF
	true ABORT" count-fitting-nucs: Unknown type."
    ENDCASE ;

MENU: men-nuc-subsets
: .menu-nuc-subsets ( -- )
    help-node" Menu nuc subsets"

    maybe-do-on-subset-field maybe-fix-condition
    s" Menu nuc subsets:    members: " start-title-entry
    count-fitting-nucs .
    end-title up-to-here cr

    count-living 0= IF
	cr
	<other-colour>
	s" No nucs, no subset..."	menu-done	['] noop menu-entry cr
	reset-colours

	<common-menu-entries>
	EXIT
    THEN

    cr
    maybe-do-type-entry
    nuc&spot-related-selections conditional-expression-entries cr

    from-here ." What to do:          "
    nuc&spot-related-selections do-what-entry cr

    cr
    s" DO IT "	redisplay	['] maybe-do-on-subset-field xt>stack
    do-after	['] |maybe-do-on-everybody-generic|	menu-entry
    s"  (Please use with care)." type-bright  up-to-here cr
    s" D" menu-same-key-entry

    cr
    s" Show subset "  redisplay		['] maybe-do-on-subset-field xt>stack
    ['] |show-fg-coloured-on-hit|	menu-entry
    s" s" menu-same-key-entry

    coloured-on-range-possible? IF
	.tab .tab
	s" Show on range"  redisplay	['] maybe-do-on-subset-field xt>stack
	['] |show-fg-coloured-on-range|	menu-entry
	s" r" menu-same-key-entry
    THEN
    cr

    count-living IF
	s" Scan this subset "  redisplay  ['] maybe-do-on-subset-field xt>stack
	['] scan-nuc-subset					menu-entry
	s" S" menu-same-key-entry
	.tab
	s" Scan nucs "	['] nuc-scan-menu	redisplay	menu-entry
	s" n" menu-same-key-entry
	.tab
    THEN
    s" Scan world "	['] spot-scan-menu	redisplay	menu-entry
    s" w" menu-same-key-entry
    cr

    <common-menu-entries> ;

: menu-nuc-subsets ( -- )
    men-nuc-subsets
    ['] .menu-nuc-subsets menu-display-xt !
    ['] .ok-done to-do-after-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' menu-nuc-subsets function-key-actions >list

:NONAME \ : simple-nuc-subset ( simple-expression-xt -- )
    ['] menu-nuc-subsets swap
    ['] maybe-do-on-subset-field do-in-simple-subset ; IS simple-nuc-subset

\ : dfloat-type-nuc-subset ( df-nuc-var-xt type-test-xt -- )
:NONAME
    2>r  ['] menu-nuc-subsets  2r>    ['] maybe-do-on-subset-field
    do-in-float-type-subset ; IS dfloat-type-nuc-subset

\ : dfloat-value-nuc-subset ( addr-of-df-value df-nuc-var-xt -- )
:NONAME
    2>r  ['] menu-nuc-subsets  2r>
    ['] maybe-do-on-subset-field do-in-dfloat-value-subset
; IS dfloat-value-nuc-subset

\ : int-value-nuc-subset ( value nuc-var-xt -- )
:NONAME
    2>r  ['] menu-nuc-subsets
    2r>  ['] maybe-do-on-subset-field  do-in-int-value-subset
; IS int-value-nuc-subset

\ ****************************************************************
\ end	menu-nuc-subsets



\ ****************************************************************
\ ********************  menu-spot-subsets  ***********************
\ ****************************************************************

: generic-hit>bg-color ( -- col )	\ dependent on active maybe-do-field
    generic-maybe? IF
	color-selected-bg-xt @ EXECUTE  EXIT
    THEN
    color-miss-bg-xt @ EXECUTE ;

bg-colour-field
' generic-hit>bg-color (do-it-xt) !

: generic-range>bg-color ( -- col )  \ dependent on active maybe-do-field
    (maybe-do-type-xt) @  ['] maybe-do = IF
	(expression-xt) @ CASE
	    ['] variable-within OF
		(expr-xt-1) @ EXECUTE @ dup
		(expr-parameter) @  (expr-parameter-2) @  within IF
		    drop color-selected-bg-xt @ EXECUTE  EXIT
		THEN
		(expr-parameter) @ < IF
		    color-below-bg-xt @ EXECUTE  EXIT
		THEN
		color-above-bg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] function-within OF
		(expr-xt-1) @ EXECUTE dup
		(expr-parameter) @  (expr-parameter-2) @  within IF
		    drop color-selected-bg-xt @ EXECUTE  EXIT
		THEN
		(expr-parameter) @ < IF
		    color-below-bg-xt @ EXECUTE  EXIT
		THEN
		color-above-bg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] variable-number OF
		(expr-xt-1) @ EXECUTE @
		dup (expr-parameter) @ < IF
		    drop
		    color-below-bg-xt @ EXECUTE  EXIT
		THEN
		(expr-parameter) @ > IF
		    color-above-bg-xt @ EXECUTE  EXIT
		THEN
		color-selected-bg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] function-number OF
		(expr-xt-1) @ EXECUTE
		dup (expr-parameter) @ < IF
		    drop
		    color-below-bg-xt @ EXECUTE  EXIT
		THEN
		(expr-parameter) @ > IF
		    color-above-bg-xt @ EXECUTE  EXIT
		THEN
		color-selected-bg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] 2-variables OF
		(expr-xt-1) @ EXECUTE @  (expr-xt-2) @ EXECUTE @ -
		dup 0= IF
		    drop color-selected-bg-xt @ EXECUTE		EXIT
		THEN
		0< IF
		    color-below-bg-xt @ EXECUTE   EXIT
		THEN
		color-above-bg-xt @ EXECUTE   EXIT
	    ENDOF
	    ['] df-variable-within OF
		(expr-df-xt-1) @ EXECUTE df@ fdup
		(expr-df-parameter) df@  (expr-df-parameter-2) df@  fwithin IF
		    fdrop color-selected-bg-xt @ EXECUTE  EXIT
		THEN
		(expr-df-parameter) df@ f< IF
		    color-below-bg-xt @ EXECUTE  EXIT
		THEN
		color-above-bg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] df-variable-number OF
		(expr-df-xt-1) @ EXECUTE df@
		fdup (expr-df-parameter) df@ f< IF
		    fdrop
		    color-below-bg-xt @ EXECUTE  EXIT
		THEN
		(expr-df-parameter) df@ f> IF
		    color-above-bg-xt @ EXECUTE  EXIT
		THEN
		color-selected-bg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] df-function-within OF
		(expr-df-xt-1) @ EXECUTE fdup
		(expr-df-parameter) df@  (expr-df-parameter-2) df@  fwithin IF
		    fdrop color-selected-bg-xt @ EXECUTE  EXIT
		THEN
		(expr-df-parameter) df@ f< IF
		    color-below-bg-xt @ EXECUTE  EXIT
		THEN
		color-above-bg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] df-function-number OF
		(expr-df-xt-1) @ EXECUTE
		fdup (expr-df-parameter) df@ f< IF
		    fdrop
		    color-below-bg-xt @ EXECUTE  EXIT
		THEN
		(expr-df-parameter) df@ f> IF
		    color-above-bg-xt @ EXECUTE  EXIT
		THEN
		color-selected-bg-xt @ EXECUTE  EXIT
	    ENDOF
	    ['] 2-df-variables OF
		(expr-df-xt-1) @ EXECUTE df@  (expr-df-xt-2) @ EXECUTE df@ f-
		fdup f0= IF
		    fdrop color-selected-bg-xt @ EXECUTE   EXIT
		THEN
		f0< IF
		    color-below-bg-xt @ EXECUTE   EXIT
		THEN
		color-above-bg-xt @ EXECUTE   EXIT
	    ENDOF
	    cr bell ." generic-range>bg-color: unknown (expression-xt): "
	    xt>string type ABORT
	ENDCASE
    THEN
    true ABORT" generic-range>bg-color: Wrong (maybe-do-type-xt)." ;

: show-bg-coloured-on-hit ( maybe-do-field-xt -- )
    EXECUTE
    c-l stringbuf-open >r
    s" Showing spots with "		r@ cat
    maybe-generic-string dup string@	r@ cat
    stringbuf-close
    ['] generic-hit>bg-color  show-bg-coloured
    last-left r@ string@ ?type
    color-selected-bg-xt @ EXECUTE color-foreground s"   hit" ?type
    color-miss-bg-xt @ EXECUTE     color-foreground s"   miss" ?type
    default-foreground clear-line-to-end

    r> stringbuf-close ;

: show-bg-coloured-on-range ( maybe-do-field-xt -- )
    EXECUTE
    c-l stringbuf-open >r
    s" Showing spots with "		r@ cat
    maybe-generic-string dup string@	r@ cat
    stringbuf-close
    ['] generic-range>bg-color  show-bg-coloured
    last-left r@ string@ ?type
    color-below-bg-xt @ EXECUTE    color-foreground s"   below" ?type
    color-selected-bg-xt @ EXECUTE color-foreground s"   WITHIN" ?type
    color-above-bg-xt @ EXECUTE    color-foreground s"   above" ?type
    default-foreground clear-line-to-end
    r> stringbuf-close ;

: |show-bg-coloured-on-hit| ( maybe-do-field-xt -- )
    ['] show-bg-coloured-on-hit (2map) 2!
    help-node" Colouring on equality"
    ['] display-map display-map-menu
    ['] generic-hit>bg-color false ?set-as-colour-condition ;

: |show-bg-coloured-on-range| ( maybe-do-field-xt -- )
    ['] show-bg-coloured-on-range (2map) 2!
    help-node" Colouring on difference"
    ['] display-map display-map-menu
    ['] generic-range>bg-color false ?set-as-colour-condition ;


MAYBE-DO-FIELD: maybe-do-spot-subset-field
' food (expr-xt-1) !			\ just a default
' food (expr-xt-2) !
' food (xt-do-it) !
init-df-expr-xts-spot
init-df-do-xts-spot
' maybe-do-simple (maybe-do-type-xt) !

\ How many spots fit the condition?
: count-fitting-spots ( -- n )
    0
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF 
	    ['] 1+ simple-maybe-do-everywhere
	ENDOF
	['] maybe-do	    OF
	    ['] 1+ maybe-do-everywhere
	ENDOF
	true ABORT" count-fitting-spots: Unknown type."
    ENDCASE ;

MENU: men-spot-subsets
: .menu-spot-subsets ( -- )
    help-node" Menu spot subsets"

    maybe-do-spot-subset-field maybe-fix-condition
    s" Menu spot subsets:    members: " start-title-entry
    count-fitting-spots .
    end-title up-to-here cr

    cr
    maybe-do-type-entry
    spot-related-selections conditional-expression-entries cr

    from-here ." What to do:          "
    spot-related-selections do-what-entry cr

    cr
    s" DO IT "	redisplay	['] maybe-do-spot-subset-field xt>stack
    do-after	['] |maybe-do-everywhere-generic|	menu-entry
    s"  (Please use with care)." type-bright  up-to-here cr
    s" D" menu-same-key-entry

    cr
    s" Show subset "  redisplay		['] maybe-do-spot-subset-field xt>stack
    ['] |show-bg-coloured-on-hit|	menu-entry
    s" s" menu-same-key-entry

    coloured-on-range-possible? IF
	.tab .tab
	s" Show on range"  redisplay	['] maybe-do-spot-subset-field xt>stack
	['] |show-bg-coloured-on-range|	menu-entry
	s" r" menu-same-key-entry
    THEN
    cr

    s" Scan this subset "   redisplay	['] maybe-do-spot-subset-field xt>stack
    ['] scan-spot-subset	menu-entry
    s" S" menu-same-key-entry
    .tab
    s" Scan nucs "	['] nuc-scan-menu	redisplay	menu-entry
    s" n" menu-same-key-entry

    .tab
    s" Scan world "	['] spot-scan-menu	redisplay	menu-entry
    s" w" menu-same-key-entry
    cr

    <common-menu-entries> ;

: menu-spot-subsets ( -- )
    men-spot-subsets
    ['] .menu-spot-subsets menu-display-xt !
    ['] .ok-done to-do-after-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' menu-spot-subsets function-key-actions >list

:NONAME \ : simple-spot-subset ( simple-expression-xt -- )
    ['] menu-spot-subsets swap
    ['] maybe-do-spot-subset-field do-in-simple-subset ; IS simple-spot-subset

\ : dfloat-type-spot-subset ( df-spot-var-xt type-test-xt -- )
:NONAME
    2>r  ['] menu-spot-subsets  2r>    ['] maybe-do-spot-subset-field
    do-in-float-type-subset ; IS dfloat-type-spot-subset

\ : dfloat-value-spot-subset ( addr-of-df-value df-spot-var-xt -- )
:NONAME
    2>r  ['] menu-spot-subsets  2r>
    ['] maybe-do-spot-subset-field do-in-dfloat-value-subset
; IS dfloat-value-spot-subset

\ : int-value-spot-subset ( value spot-var-xt -- )
:NONAME
    2>r  ['] menu-spot-subsets
    2r>  ['] maybe-do-spot-subset-field  do-in-int-value-subset
; IS int-value-spot-subset

\ ****************************************************************
\ end	menu-spot-subsets



\ ****************************************************************
\ ************  Selecting nucs:  menu-select-nucs  ***************
\ ****************************************************************


\ These have been defined earlier:
\   select-nuc
\   de-select-nuc
\   toggle-selection

\ LIST: do-on-nuc-xts
do-it-xt's do-on-nuc-xts copy-simple-list-elements
' select-nuc do-on-nuc-xts >list
' de-select-nuc do-on-nuc-xts >list
' toggle-selection do-on-nuc-xts >list
' remove-nuc do-on-nuc-xts >list		\ (have it here in the list).

defer ?record-invert-selection
: invert-selections ( -- )
    ?record-invert-selection
    ['] toggle-selection do-with-everybody
    nucs-not-scanned ;

defer ?record-de-select-all
: de-select-all-nucs ( -- )
    ?record-de-select-all
    ['] de-select-nuc do-with-everybody
    nucs-not-scanned ;

: selected>fg-color ( -- col )
    selected? IF
	color-selected-fg-xt @ EXECUTE  EXIT
    THEN
    color-miss-fg-xt @ EXECUTE ;
' selected>fg-color x>fg-color >list

: selected>bg-color ( -- col )
    spot @ someone-here? IF
	selected? IF  color-selected-bg-xt @ EXECUTE  EXIT  THEN
    THEN
    color-miss-bg-xt @ EXECUTE ;
' selected>bg-color x>bg-color >list

: (show-selected) ( -- )
    ['] selected>fg-color show-fg-coloured
    s" Shows selected nucs." .last-line ;

: show-selected ( -- )
    help-node" Colouring on equality"
    ['] (show-selected) display-map-menu
    ['] selected>fg-color true ?set-as-colour-condition ;

MAYBE-DO-FIELD: maybe-select-field
' select-nuc (do-it-xt) !
' maybe-do (maybe-do-type-xt) !
' variable-number (expression-xt) !
' genome-id (expr-xt-1) !			\ just a default
' energy (expr-xt-2) !
init-df-expr-xts-nuc
init-df-do-xts-nuc
' = (condition-xt) !

: in/exclude/toggle-selection ( --)
    maybe-select-field
    (do-it-xt) dup @ CASE
	['] select-nuc		OF
	    ['] de-select-nuc
	ENDOF
	['] de-select-nuc	OF
	    ['] toggle-selection
	ENDOF
	['] toggle-selection	OF
	    ['] select-nuc
	ENDOF
    ENDCASE
    swap ! ;

MAYBE-DO-FIELD: maybe-do-on-selected-field
' maybe-do-simple (maybe-do-type-xt) ! 
' selected? (simple-expression-xt) !
' scale-variable (do-it-xt) !
' energy (xt-do-it) !
init-df-do-xts-nuc

\ : do-with-selected-nucs ( to-do-xt -- )
:NONAME ( to-do-xt -- )
    maybe-do-on-selected-field simple-maybe-do-with-everybody
; IS do-with-selected-nucs

: scan-selected ( -- )
    ['] do-with-selected-nucs scan-whom-xt!
    nuc-scan-menu
    reset-scan-whom ;

defer ?record-change-selections
: do-change-selections ( -- )
    maybe-select-field
    log-user? IF
	s" " 0 log
	s" user changed selections:" 0 log
	all-maybe-string dup string@ 0 log
	stringbuf-close
	s" " 0 log
    THEN
    ?record-change-selections
    ['] maybe-do-generic do-with-everybody ;

defer ?record-do-on-selected-nucs
: do-on-selected-nucs ( -- )
    maybe-do-on-selected-field
    log-user? IF
	s" " 0 log
	s" user did on selected nucs:" 0 log
	all-maybe-string dup string@ 0 log
	stringbuf-close
	s" " 0 log
    THEN
    ?record-do-on-selected-nucs
    ['] maybe-do-generic do-with-everybody ;

MENU: men-select-nucs
: .menu-select-nucs ( -- )
    count-living

    help-node" Menu select nucs"
    s" Menu select nucs:" start-title-entry
    ."                        Selected: " selected @ .
    ."       others: "
    ( count-living ) dup selected @ - . end-title up-to-here
    cr

    ( count-living ) 0= IF
	<other-colour>
	s" No nucs, no selections."	menu-done	['] noop menu-entry cr
	reset-colours

	<common-menu-entries>
	EXIT
    THEN

    \ Show selected nucs, invert or clear selections,
    \ choose 'color-selected-fg-xt':
    s" ito"	redisplay	do-after	do-after-2
    ['] invert-selections	menu-key-entry
    selected @ IF
	s" show selected "  redisplay   ['] show-selected	menu-entry
	s" s" menu-same-key-entry
	1 4 screen-column
	s" deselect all"	redisplay	do-after	do-after-2
	['] de-select-all-nucs menu-entry
	s" d0" menu-same-key-entry
	2 4 screen-column
	s" toggle selections"	redisplay	do-after	do-after-2
	['] invert-selections menu-entry cr
	from-here ." color "
	['] color-list ['] color-selected-fg-xt choose-xt-to-var-entry cr
    THEN

    cr
    maybe-select-field maybe-fix-condition

    from-here ." Change selections:   "	(do-it-xt) @ xt>string	redisplay
    ['] in/exclude/toggle-selection				menu-entry cr

    maybe-do-type-entry
    nuc&spot-related-selections conditional-expression-entries cr 

    s" DO IT"	redisplay	do-after	do-after-2
    ['] do-change-selections	menu-entry cr
    s" D" menu-same-key-entry

    \ Do something on selected nucs:
    selected @ IF
	maybe-do-on-selected-field maybe-fix-condition
	cr
	s" DO on selected nucs: "	redisplay	do-after  do-after-2
	['] do-on-selected-nucs		menu-entry
	s" (Please use very carefully...)" type-bright  up-to-here cr

	from-here ." What to do:          "
	nuc&spot-related-selections do-what-entry cr

	cr
	s" scan SELECTED nucs"	redisplay   ['] scan-selected	menu-entry
	s" NS" menu-same-key-entry
	1 2 screen-column
    ELSE cr
    THEN

    s" scan ALL nucs"  do-after	['] nuc-scan-menu	menu-entry cr
    s" naA" menu-same-key-entry

    <common-menu-entries> ;

: menu-select-nucs ( -- )
    page
    do-a-nuc-scan
    men-select-nucs
    ['] .menu-select-nucs menu-display-xt !
    ['] do-a-nuc-scan to-do-after-xt !
    ['] .ok-done to-do-after-2-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' menu-select-nucs function-key-actions >list

\ ****************************************************************
\ end	menu-select-nucs



\ ****************************************************************
\ ***********  menu-this-genome, menu-current-genomes  ***********
\ ****************************************************************

\ List of all current genomes:
0
OFFSET: >genome-id
OFFSET: >genome-count		\ count of *all* cells using this genome
OFFSET: >genome-compiled#	\ separate counter for compiled genomes
OFFSET: >genome-trial#		\ separate counter for genomes on trial
OFFSET: >genome-cost
OFFSET: >max-genome-generation
OFFSET: >genome-xt		\ xt if genome is compiled
OFFSET: >genome-eb-addr		\ "xt" if genome is on trial
OFFSET: >genome-internal-xt
\ OFFSET: >genome-flags
( u ) cell / 1- ASSOCIATION-LIST: genomes

: add-nucs-genome ( -- )	\ cp must be set
    genome-id @ genomes key-to-list >r	( r: data-addr)	\ genome ID as key
    1 r@ >genome-count +!				\ count them

    wake-me-xt @
    on-trial? IF
	1 r@ >genome-trial# +!
	r@ >genome-eb-addr !
    ELSE
	1 r@ >genome-compiled# +!
	r@ >genome-xt !
	wake-me-internal @  r@ >genome-internal-xt !
    THEN

    \    nuc-flags @  r@ >genome-flags !
    genome-generation @  r@ >max-genome-generation >r  r@ @ max r> !
    code-cost @  r> >genome-cost ! ;

: build-genomes-list ( -- )
    genomes empty-list
    ['] add-nucs-genome do-with-everybody ;

\ see genome dealing with trial and (Gforth) 'see' bug
: see-this-nodes-genome ( node -- )
    page
    dup >genome-id @ title-colors ." Showing genome GID: " . end-title

    dup >genome-compiled# @ IF			\ at least *one* compiled
	>genome-xt @  see-compiled-genome	\ compiled: try 'see'
    ELSE
	>genome-eb-addr @  see-genome-on-trial
    THEN ;

MAYBE-DO-FIELD: maybe-do-this-genome-field
' maybe-do (maybe-do-type-xt) !
' variable-number (expression-xt) !
' genome-id (expr-xt-1) !
' = (condition-xt) !
init-df-do-xts-nuc

\ Recording is done in |maybe-do-on-everybody-generic|
: do-with-this-genome ( do-xt -- )
    maybe-do-this-genome-field
    (do-it-xt) !
    ['] maybe-do-this-genome-field  |maybe-do-on-everybody-generic| ;

Menu: men-this-genome
0 VALUE (this-node)	\ to pass parameters to the display word
: .menu-this-genome ( -- )
    (this-node) >r		( r:  node )
    maybe-do-this-genome-field maybe-fix-condition
    r@ >genome-id @ (expr-parameter) !

    help-node" Menu this genome"
    s" Menu this genome." start-title-entry clear-line-to-end
    1 3 screen-column
    r@ >genome-compiled# @ IF
	 r@ >genome-xt @ xt>string type ."  compiled."
    ELSE
	." GID:"  r@ >genome-id @ .  ."  on trial."
    THEN
    2 3 screen-column
    r@ >genome-count @ .
    ." nucs with this genome."
    end-title up-to-here

    cr
    s" genome GID = "	r@ >stack	redisplay	menu-wait
    ['] see-this-nodes-genome	menu-entry
    r@ >genome-id @ . up-to-here
    s" gGl" menu-same-key-entry

    r@ >genome-compiled# @ IF
	1 3 screen-column
	r@ >genome-internal-xt @ xt>stack
	menu-wait   redisplay       ['] .gene-info	name-menu-entry
	s" .iI" menu-same-key-entry
    THEN

    cr
    s" show cells"		['] maybe-do-this-genome-field xt>stack
    ['] |show-fg-coloured-on-hit|	menu-entry
    s" s" menu-same-key-entry
    \ ALTERNATIVE SOURCE: does pretty much the same
    \     s" show cells"	r@ >genome-id @ >stack	['] genome-id >stack-2
    \     ['] show-coloured-on-nuc-var-eq	redisplay	menu-entry

    1 3 screen-column
    s" show younger/older"  redisplay	['] maybe-do-this-genome-field xt>stack
    ['] |show-fg-coloured-hit-diff|	menu-entry cr
    \ ALTERNATIVE SOURCE: does pretty much the same
    \ s" show younger/older"   r@ >genome-id @ >stack  ['] genome-id xt>stack-2
    \ ['] show-coloured-on-nuc-var-diff		redisplay	menu-entry cr
    s" yr" menu-same-key-entry

    cr
    s" Scan these nucs."	redisplay	do-after
    ['] maybe-do-this-genome-field xt>stack  ['] scan-nuc-subset    menu-entry
    s" N" menu-same-key-entry
    s" n"  ['] nuc-scan-menu	redisplay	menu-key-entry
    s" w"  ['] spot-scan-menu	redisplay	menu-key-entry
    1 3 screen-column
    s" Scan all nucs."		redisplay	do-after
    ['] nuc-scan-menu		menu-entry cr

    cr
    s" Select these nucs"	redisplay	do-after	do-after-2
    ['] select-nuc >stack	['] do-with-this-genome  menu-entry
\   s" S" menu-same-key-entry
    selected @ IF
	1 4 screen-column
	s" clear selection"	redisplay	do-after	do-after-2
	['] de-select-nuc >stack	['] do-with-this-genome  menu-entry
	s" c" menu-same-key-entry

	2 4 screen-column
	s" toggle selection"	redisplay	do-after	do-after-2
	['] toggle-selection >stack	['] do-with-this-genome  menu-entry
	s" to" menu-same-key-entry
    THEN cr

    cr
    cr
    from-here ." Do something on these nucs: "
    <other-colour> s" (Please use with care)."		do-after-2
    ['] maybe-do-this-genome-field xt>stack	redisplay	do-after
    ['] |maybe-do-on-everybody-generic|		menu-entry cr
    reset-colours
    from-here ." What to do:  "
    nuc&spot-related-selections do-what-entry cr

    <common-menu-entries>
    rdrop ;

: menu-this-genome ( node -- )
    to (this-node)	\ pass it to the display function
    do-a-nuc-scan

    men-this-genome
    ['] .menu-this-genome menu-display-xt !
    ['] do-a-nuc-scan to-do-after-xt !
    ['] .ok-done to-do-after-2-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop ;

MENU: men-current-genomes

VARIABLE (genome-sort-index)	0 (genome-sort-index) !
VARIABLE (sort-upwards)		(sort-upwards) on
0 VALUE sorted-genomes
: sort-genomes-list ( -- )
    sorted-genomes dup IF
	remove-list
    ELSE drop THEN

    (genome-sort-index) @ genomes copy-to-sorted-list to sorted-genomes ;

: .genome-list-node ( node -- )
    >r				( r: node )

    r@ >genome-id @ num>string
    r@ >stack	redisplay	['] menu-this-genome	menu-entry

    1 6 screen-column
    r@ >stack

    [ decimal ] 20 stringbuf-open
    r@ >genome-compiled# @ IF
	r@ >genome-xt @ xt>string third cat
	r@ >genome-trial# @ dup IF
	    over >r
	    s"  t:"		 r@ cat
	    num>string		 r> cat
	ELSE drop THEN
    ELSE
	s" on trial"		 third cat
    THEN
    dup string@
    redisplay	menu-wait	['] see-this-nodes-genome  menu-entry
    stringbuf-close

    2 6	screen-column  r@ >genome-count @ num>string
    r@ >genome-id @ >stack	['] genome-id >stack-2
    ['] show-coloured-on-nuc-var-eq	redisplay	menu-entry

    3 6	screen-column  r@ >genome-cost @ num>string
    r@ >genome-cost @ >stack	['] code-cost >stack-2
    ['] show-coloured-on-nuc-var-diff	redisplay	menu-entry

    4 6	screen-column  r@ >max-genome-generation @ num>string
    r@ >max-genome-generation @ >stack	['] genome-generation >stack-2
    ['] show-coloured-on-nuc-var-diff	redisplay	menu-entry

    rdrop ;

\ Space goes through current order of genomes, one by one:
VARIABLE (next-genome-index)	(next-genome-index) off
: check-genome-index ( -- )
    0  sorted-genomes nodes  (next-genome-index) @ within 0= IF
	(next-genome-index) off
    THEN ;

: next-genome-node ( -- node )

    (sort-upwards) @ IF
	(next-genome-index) @
    ELSE
	sorted-genomes nodes 1- (next-genome-index) @ -
    THEN
    sorted-genomes n'th-node

    1 (next-genome-index) +! ;

\ Go through the list and call 'menu-this-genome'
: goto-next-genome ( -- )   next-genome-node menu-this-genome ;

\ Show next genome in current order:
: show-next-genome ( -- )   next-genome-node see-this-nodes-genome ;

\ Call '.gene-info' on next genome,
\ or 'see-genome-on-trial' on genomes on trial instead. .gene-info \ #########
: show-next-gene-info ( -- )
    next-genome-node
    dup >genome-compiled# @ IF			\ at least *one* compiled
	>genome-internal-xt @ .gene-info	\ compiled
    ELSE
	>genome-eb-addr @			\ trial
	page see-genome-on-trial
    THEN ;

: change-genome-sorting ( index -- )
    (genome-sort-index) @ over <> IF
	(genome-sort-index) !
    ELSE
	drop
	['] (sort-upwards) toggle-named
    THEN
    (next-genome-index) off ;

: genome-sort-entry ( addr count offset-xt -- )
    0 swap EXECUTE cell /	( addr count index -- )
    >stack	redisplay	do-after
    ['] change-genome-sorting	menu-entry ;

: mark-next-index ( index -- )
    (next-genome-index) @ <> IF EXIT THEN
    
    c-l 6 / 2 - at-x  [char] * emit ;

: .menu-current-genomes ( -- )
    help-node" Menu current genomes"
    s" Menu current genomes.                  " start-title-entry
    ." Genomes of living cells: "  genomes nodes .  end-title up-to-here

    check-genome-index

    sorted-genomes nodes 0= IF
	cr s" No cells, no genes." type-other-colour cr
	<common-menu-entries>
	EXIT
    THEN

    \ titles:
    cr			s" genome-id:"
    ['] >genome-id genome-sort-entry
    s" ig" menu-same-key-entry
    1 6	screen-column	s" genome:"		['] noop	menu-entry
    2 6	screen-column	s" count:"
    ['] >genome-count genome-sort-entry
    s" c" menu-same-key-entry
    3 6	screen-column	s" cost:"
    ['] >genome-cost genome-sort-entry
    s" o$" menu-same-key-entry
    4 6	screen-column	s" max generations:"
    ['] >max-genome-generation genome-sort-entry cr
    s" m" menu-same-key-entry

    7 keep-but-scroll-rest

    cr
    sorted-genomes
    (sort-upwards) @ IF
	dup nodes 0 scrolled-range ?DO
	    i over n'th-node .genome-list-node
	    i mark-next-index
	    cr
	LOOP
	drop
    ELSE
	dup nodes 0 scrolled-range ?DO
	    dup nodes 1- i -  over n'th-node .genome-list-node
	    i mark-next-index
	    cr
	LOOP
	drop
    THEN

    s"  "	redisplay		['] goto-next-genome	menu-key-entry
    s" ln"	redisplay   menu-wait	['] show-next-genome	menu-key-entry
    s" ."	redisplay   menu-wait	['] show-next-gene-info	menu-key-entry

    <common-menu-entries> ;

: menu-current-genomes ( -- )
    page
    build-genomes-list
    sort-genomes-list
    (next-genome-index) off

    men-current-genomes
    ['] .menu-current-genomes menu-display-xt !
    ['] sort-genomes-list to-do-after-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop ;

' menu-current-genomes function-key-actions >list

\ ****************************************************************
\ end	genome menus



\ ****************************************************************
\ ************************  food-menu  ***************************
\ ****************************************************************

\ food menu, we go to 'The Restaurant' now:
MENU: food-men
: .menu-food ( -- )
    help-node" Food menu"
    s" Food menu:" menu-title-entry

    cr
    s" World food supply: " ['] world-food-supply simple-menu-entry-variable cr
    s" w" menu-same-key-entry

    world-food-supply @ IF
	."    (divided among all " living @ ?dup IF . THEN
	." living beings giving "
	determine-food-share food-common-share @ . ." for each one)" cr
    THEN

    cr
    s" Each living individual get's in addition: "
    ['] individual-fixed-food-share simple-menu-entry-variable cr
    s" i" menu-same-key-entry

    cr
    s" On each spot, inhabited or not, is further deposed: "
    ['] food-share/spot simple-menu-entry-variable cr
    s" s" menu-same-key-entry

    cr
    s" Actually each step in live costs: "
    ['] nuc-do-cost simple-menu-entry-variable cr
    s" n" menu-same-key-entry

    cr
    s" Price to pay for code usage: "
    ['] code-price simple-menu-entry-variable
    s" c" menu-same-key-entry

    .tab .tab
    s" rating scale "	['] code-price-scale	simple-menu-entry-scale cr
    s" r" menu-same-key-entry

    cr
    cr cr cr cr cr cr cr
    s" Give a extra food portion: feed world manually"
    do-after	redisplay	['] |feed-world|	menu-entry cr
    s" ex" menu-same-key-entry

    cr
    s" Scan world "	['] spot-scan-menu	redisplay	menu-entry
    count-living IF
	.tab
	s" Scan nucs "	['] nuc-scan-menu	redisplay	menu-entry
    THEN
    cr

    <common-menu-entries> ;

: food-menu ( -- )
    food-men
    ['] .menu-food menu-display-xt !
    ['] .ok-done to-do-after-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' food-menu function-key-actions >list

\ ****************************************************************
\ end	food-menu



\ ****************************************************************
\ *****  world-do:  Run the world trough one step in time.  ******
\ ****************************************************************

\ Run the world trough one step in time
\ The spots are activated in selectable order
VARIABLE spot-do-xt		' noop spot-do-xt !
VARIABLE cell-do-before-xt	' noop cell-do-before-xt !
VARIABLE cell-do-after-xt	' noop cell-do-after-xt !
VARIABLE step-do-before-xt	' noop step-do-before-xt !
VARIABLE step-do-after-xt	' noop step-do-after-xt !
VARIABLE world-do-direction	1 world-do-direction !

LIST: spot-do-actions
' noop spot-do-actions >list

LIST: cell-do-actions
' noop cell-do-actions >list

LIST: step-do-actions
' noop step-do-actions >list

DEFER ?diversify-spots
DEFER ?diversify-inhabited
DEFER ?diversify-globals

INCLUDE world-loop.fs

: change-world-do-direction ( -- )
    world-do-direction @ CASE
	1  OF -1 ENDOF
	-1 OF  0 ENDOF
	0  OF  2 ENDOF
	2  OF -2 ENDOF
	-2 OF  1 ENDOF
    ENDCASE world-do-direction ! ;

\ ****************************************************************
\ end	world-do



\ ****************************************************************
\ **********************  Individuals:  **************************
\ ****************************************************************

\ word to create a named individual in the dictionary
: INDIVIDUAL:		\ 'INDIVIDUAL: name' creates a nuc named 'name'
    CREATE		\ compiling:      create the dictionary entry
    nuc-length# new-nucleus-as-word drop	\ make the nucleus
    cp!			\ compiling:      the newborn gets center of attention
    nuc-does-nothing				\ default initialisation
[UNDEFINED] transit-12-bench [IF]	\ backwards compatibility hack
    [UNDEFINED] brew-crash-test [IF]	\ backwards compatibility hack
	new-genome-id genome-id !
    [THEN]
[THEN]
  DOES> cp! ;		\ at run time:    returns addr and does cp!

LIST: individuals

\ dummy individual to indicate missing selection
\ : (none) ;
' (none) individuals >list


INDIVIDUAL: template	' template individuals >list
' eat-all		eat-xt !	\ but very hungry
' cell-division 	reproduce-xt !
' <look-at>		show-me-xt !
100	reprodctn-threshold !
10	age-threshold !


\ Make another prototype to experiment with:
INDIVIDUAL: prototype
' prototype individuals >list

' eat-all	eat-xt !
\ there will be many soon
20	reprodctn-threshold !
' cell-division reproduce-xt !
\ how he shows himself
' <look-at>	show-me-xt !
\ his lifetime is very short
3	age-threshold !


nuc-organs# [IF] \ at least organ-A defined.

INDIVIDUAL: fertile

' cell-division reproduce-xt !
60 reprodctn-threshold !
2   age-threshold !
char * char 0 -   organ-A !
0 my-diversifctn-mask !
' eat-all eat-xt !
' <look-at>     show-me-xt !
' fertile individuals >list

[THEN]

\ ****************************************************************
\ end	individuals



\ ****************************************************************
\ ************************  info-line  ***************************
\ ****************************************************************

\ .info-line with display-slots:
10 CONSTANT max-display-slots#				\ up to this many slots
CREATE display-slots					\ number and pointers
max-display-slots# 1+ cells allot			\ space for pointers
4 display-slots !					\ slots in info-line

: display-slot ( slot -- addr )
    dup max-display-slots# >= abort" Display slot out of range"
    1+ cells display-slots + ;		\ pointers start at 2'nd cell

: start-help ( -- )
    s" Start <SPACE>  Select CURSOR, <RET>  Manual <?>  Menus <B>  Keys <k>  Demos <d>"
    1 >message ;

VARIABLE single-step	single-step off
2VARIABLE (.burden-at)	0. (.burden-at) 2! \ cosmetics see automation, .burden
2VARIABLE (.code-price-at) 0. (.code-price-at) 2! \ cosmetics see automation
VARIABLE no-info-line	no-info-line off		\ used in benchmarks
: .info-line ( -- )					\ displays info line
    no-info-line @ IF EXIT THEN

    reset-colours
    0. (.burden-at) 2!		\ little trick to track if .burden is in a slot
    0. (.code-price-at) 2!	\ same

    spot-display-on?  (manually-selected-cell) @ and IF
	last-left
	s" Clone nuc by selecting an empty spot." type-other-colour
	clear-line-to-end
	help-node" Cloning nucs to a spot"
	EXIT
    THEN

    message-count @ IF		\ special case: message to display
	\ in single step modus display only once,
	\ the recording thing is a bit difficult to get right...
	NOT-recording? single-step @ and IF
	    message-count off
	ELSE
	    -1 message-count +!	\ else as many times as specified
	THEN
	message-fg-color-xt @ dup IF EXECUTE color-foreground ELSE drop THEN
	message-bg-color-xt @ dup IF EXECUTE color-background ELSE drop THEN
	last-left clear-line-to-end last-left
	(message) string@ type
	message-fg-color-xt @ IF
	    default-foreground
	THEN
	message-bg-color-xt @ IF
	    default-background
	THEN
	message-count @ 0= IF
	    message-fg-color-xt off
	    message-bg-color-xt off
	THEN
    ELSE
	message-fg-color-xt off
	message-bg-color-xt off

	display-slots @ IF
	    c-l display-slots @ /		\ size of a slot
	    display-slots @ 0 DO
		dup i * last-line at-xy
		i display-slot ?perform
						\ insert spaces up to next slot
[ lower-right-scrolls ] [IF]	
		key? 0= IF			\ scroll protected version
		    dup i 1+ * 1-			\ start of next slot
		    i display-slots @ 1- <> IF 1+ THEN	\ not for last slot
		    at-x? - spaces			\ fill with spaces
		THEN
[ELSE]
		key? 0= IF			\ no scroll protection needed
		    dup i 1+ *			\ start of next slot
		    at-x? - spaces		\ fill with spaces
		THEN
[THEN]
	    LOOP drop
[ lower-right-scrolls 0= ] [IF]
	    at-x? c-l 1- < IF
		clear-line-to-end		\ get rid of menu background
	    THEN
[THEN]

	ELSE
	    last-left clear-line-to-end		\ clear info line
	THEN
    THEN ;

: empty-display-slots ( -- )
    max-display-slots# 0 DO  ['] noop  i display-slot !	LOOP ;
empty-display-slots

\ display functions to put in the slots:
LIST: slot-display-words

' noop slot-display-words >list
: .step ( -- )	." step: " step @ . ;
' .step   0 display-slot !
' .step slot-display-words >list

: .cells ( -- )	." cells: " cloned @ . ;
' .cells  1 display-slot !
' .cells slot-display-words >list

: .burden ( -- )
    at? (.burden-at) 2!		."  burden: " nuc-do-cost @ . ;
' .burden 3 display-slot !
' .burden slot-display-words >list

: .code-price ( -- )
    at? (.code-price-at) 2!		."  code price: " code-price @ . ;
' .code-price slot-display-words >list

: .living ( -- ) ." living: " living @ . ;
' .living 2 display-slot !
' .living slot-display-words >list

: .selected ( -- ) ." selected: " selected @ . ;
' .selected slot-display-words >list

: .newborn ( -- ) ." newborn: " newborn @ . ;
' .newborn slot-display-words >list

: .died ( -- ) ." died: " died @ . ;
' .died slot-display-words >list

: .trial ( -- ) ." trial: " trial @ . ;
' .trial slot-display-words >list

: .mutations ( -- ) ." mutated: " mutations @ . ;
' .mutations slot-display-words >list

: .compiled-genes ( -- ) ." compiled: " compiled-genes @ . ;
' .compiled-genes slot-display-words >list

: .mutation-max-items ( -- ) ." mut.items: " (mutated-max) @ . ;
' .mutation-max-items slot-display-words >list

: .age-threshold ( -- )
    ." max age: "
    cp@ IF
	age-threshold @ .
    THEN ;
' .age-threshold slot-display-words >list

: .score ( -- )
    ." score: "
    score-list @ dup IF
	dup nodes IF
	    0 swap n'th-node @ negate .
	    EXIT
	THEN
    THEN
    drop ." -- " ;
' .score slot-display-words >list

: .scoring ( -- )
    ." scoring: "
    score-list @ dup IF
	dup nodes IF
	    cp@ >r
	    0 swap n'th-node
	    cell+ @ >spot!  fcp @ cp!
	    scoring .
	    r> cp!
	    EXIT
	THEN
    THEN
    drop ." -- " ;
' .scoring slot-display-words >list


: hit? ( -- flag )   scoring 0= ;

: .hits ( -- )   ['] hit? test-and-count-everybody  ." hits: " . ;
' .hits slot-display-words >list

\ ****************************************************************
\ end	info-line



\ ****************************************************************
\ ********  Population control:  menu-population-control  ********
\ ****************************************************************

\ Population control:  mean, automatic production of stress:
: ?log-nuc-do-cost ( -- )
    log-costs? 0= IF EXIT THEN

    s" nuc-do-cost: "		cat-log
    nuc-do-cost @ num>string	cat-log
    s" 		code-price: "	cat-log
    code-price @ num>string	log-costs log-it ;

VARIABLE additive-stress	2 additive-stress !
VARIABLE additive-release	4 additive-release !
2VARIABLE stress-rate		1 1 stress-rate 2!
VARIABLE multiplicative-release 4 multiplicative-release !
VARIABLE code-additive-stress	1 code-additive-stress !
2VARIABLE code-stress-rate	1 1 code-stress-rate 2!

decimal 1920 TO spots		\ default to initialise some marks

VARIABLE high-water-mark	spots 3 / high-water-mark !
\ spots 4 / high-water-mark !
VARIABLE flood-mark		spots 90 100 */ flood-mark !
2VARIABLE flood-stress-rate	110 100 flood-stress-rate 2!
2VARIABLE flood-kill-rate	0 1 flood-kill-rate 2!
2VARIABLE flood-energy-rate	0 1 flood-energy-rate 2!
2VARIABLE flood-food-rate	9 10 flood-food-rate 2!
: flood-decrease-energy ( -- )  energy flood-energy-rate addr-rate ;
VARIABLE sos-mark		spots  4 100 */ sos-mark !

0 TO spots			\ restore spots

2VARIABLE sos-release-rate	1 2 sos-release-rate 2!
VARIABLE sos-reproduction-push	1 sos-reproduction-push ! \ energy for children
: sos-push-reproduction ( -- )	\ giving multiples of reprodctn-threshold
    energy dup @
    reprodctn-threshold @ 1+  sos-reproduction-push @  *
    max swap ! ;

VARIABLE sos-sow		1 sos-sow !		\ SOS: how many to sow
: (sos-do-sow) ( -- )   sos-sow @ sow drop ;
: sos-do-sow ( -- )   ['] (sos-do-sow) not-recording ;
VARIABLE low-water-mark		100 low-water-mark !
\ in the very beginning low-water-mark and sos-mark do not apply:
VARIABLE up-regulation-start	1000 up-regulation-start !  \ after which step?
VARIABLE nuc-cost-can-be-help?		nuc-cost-can-be-help? on
VARIABLE code-price-can-be-help?	code-price-can-be-help? on

: ?log-after-pop-control ( flag=false|1|2 -- )
    dup 0= IF drop EXIT THEN
    log-pop-control? 0= IF drop EXIT THEN
 
    s" after poplation control:"	0 log-it
    ['] nuc-do-cost log-variable
    ['] code-price log-variable

    1- IF	\ emergency?
	['] additive-stress log-variable
	['] code-additive-stress log-variable
	['] food-share/spot log-variable
	['] individual-fixed-food-share log-variable
	['] world-food-supply log-variable

	['] flood-energy-rate log-scale
	s" survivors: "		cat-log
	living @ num>string	0 log-it
    THEN ;

: .+/-info-indicators ( +flag -- )
    no-info-line @ IF drop EXIT THEN

    IF  [char] +  ELSE  [char] -  THEN

    (.burden-at) 2@ d0= 0= IF  \ d0= if .burden is not active
	.info-line
	(.burden-at) 2@ at-xy
	dup emit
    THEN
    (.code-price-at) 2@ d0= 0= IF \ d0= if .code-price not active
	.info-line
	(.code-price-at) 2@ at-xy
	dup emit
    THEN
    drop ;

: population-control ( -- )	\ brew0 mode

    additive-stress @       stress-rate 2@ <>      OR
    code-additive-stress @  code-stress-rate 2@ <> OR
    OR IF				\ we want any population control?
	false >r			( r: log-flag )
	living @ high-water-mark @ > IF			\ too many?
	    log-mask @ dup IF
		log-pop-control and IF
		    r> 1+ >r
		    s" "				0 log-it
		    s" high-water-mark exeeded step "	cat-log
		    step @ num>string			cat-log
		    s" 	living: "			cat-log
		    living @ num>string			0 log-it
		THEN
	    ELSE drop THEN

	    additive-stress @ nuc-do-cost +!
	    code-additive-stress @ code-price +!
	    nuc-do-cost dup @ stress-rate 2@ */ swap !
	    code-price dup @ code-stress-rate 2@ */ swap !
	    living @ flood-mark @ > IF

		log-mask @ dup IF
		    [ log-pop-control log-emergency or ] literal and IF
			r> 1+ >r
			s" POPULATION FLOOD.  Step: "	cat-log
			step @ num>string		cat-log
			s"   living: "			cat-log
			living @ num>string		0 log-it
		    THEN
		ELSE drop THEN
		s" Population flood." 1 >message ['] cyan message-fg-color-xt !

[ true ] [IF] \ new version

		\ increase stress:
		additive-stress dup @ abs flood-stress-rate 2@ */ 1+ swap !
		code-additive-stress dup @ abs flood-stress-rate 2@ */ 1+ swap !
		nuc-do-cost dup @ abs flood-stress-rate 2@ */ swap !
		code-price dup @ abs flood-stress-rate 2@ */ swap !

		\ decrease food:
		food-share/spot			flood-food-rate addr-rate
		individual-fixed-food-share	flood-food-rate addr-rate
		world-food-supply		flood-food-rate addr-rate

		spots living @ -		\ individuals too many
		flood-kill-rate 2@ */		\ some should go?
		leave-energy-after-death dup @ >r off	\ energy vanishes
		['] die do-with-random-nucs		\ some must die
		r> leave-energy-after-death !

		flood-energy-rate 2@ <> IF
		    ['] flood-decrease-energy do-with-everybody
		THEN

[ELSE] \ old version. I keep it for the old demos.
		additive-stress dup @ abs 2* swap !
		code-additive-stress dup @ abs 2* swap !
		nuc-do-cost dup @ abs 3 2 */ swap !
		code-price dup @ abs 3 2 */ swap !
		s" flood" log-emergency log
		s" Population flood!" 1 >message
		['] cyan message-fg-color-xt !
[THEN]
	    THEN
	    true .+/-info-indicators
	THEN

	living @ low-water-mark  @ < IF				\ too few?
	    step @  up-regulation-start @  > IF			\ start phase?
		log-mask @ dup IF
		    log-pop-control and IF
			r> 1+ >r
			s" "				0 log-it
			s" below low-water-mark step "	cat-log
			step @ num>string		cat-log
			s" 	living: "		cat-log
			living @ num>string		0 log-it
		    THEN
		ELSE drop THEN

		\ release stress some additive steps
		additive-stress @  additive-release @  *
		abs negate nuc-do-cost +!
		\ same for code stress
		code-additive-stress @  additive-release @  *
		abs negate code-price +!

		\ release stress multiplicative
		nuc-do-cost dup @ dup 0> IF
		    multiplicative-release @ 0 ?DO
			stress-rate 2@ swap */
		    LOOP
		ELSE
		    multiplicative-release @ 0 ?DO
			stress-rate 2@ */
		    LOOP
		THEN swap !
		\ same for code
		code-price dup @ dup 0> IF
		    multiplicative-release @ 0 ?DO
			code-stress-rate 2@ swap */
		    LOOP
		ELSE
		    multiplicative-release @ 0 ?DO
			code-stress-rate 2@ */
		    LOOP
		THEN swap !

		living @ sos-mark @ < IF
		    nuc-do-cost dup @ sos-release-rate 2@ */ swap !
		    additive-stress dup @ dup 0> IF
			sos-release-rate 2@ */
			1 max
		    THEN swap !

		    code-price dup @ sos-release-rate 2@ */ swap !
		    code-additive-stress dup @ dup 0> IF
			sos-release-rate 2@ */
			1 max
		    THEN swap !

		    ['] sos-push-reproduction do-with-everybody
		    sos-sow @ dup IF
			['] sos-do-sow do-with-everybody
		    THEN drop

		    log-mask @ dup IF
			[ log-pop-control log-emergency or ] literal and IF
			    r> 1+ >r
			    s" S.O.S. POPULATION BREAKDOWN step: " cat-log
			    step @ num>string	cat-log
			    s"   living: "	cat-log
			    living @ num>string	0 log-it
			THEN
		    ELSE drop THEN
		    s" S.O.S. Population emergency!" 1 >message
		    ['] red message-fg-color-xt !
		THEN
		false .+/-info-indicators
	    THEN
	THEN

	nuc-cost-can-be-help? @ 0= IF
	    nuc-do-cost dup @  0 max  swap !
	THEN
	code-price-can-be-help? @ 0= IF
	    code-price dup @  0 max  swap !
	THEN

	?log-nuc-do-cost
	r> ?log-after-pop-control
    THEN ;


\ Population control based on an elite:

\ Make sure there's an empty  score-list :
:NONAME ( -- )	\ : ?init-score-list ( -- )
    score-list @ IF  score-list @ remove-list   THEN
    2 deflist score-list ! ; IS ?init-score-list

\ Fill population with mutated elite:
: fill-population ( -- )
    score-list @
    dup nodes  elite @  min >r		( list  r: elite-ok )
    1 r@ > IF  drop rdrop EXIT  THEN	\ no elite to deal with

    r@					( list counter=elite  r: elite-ok )
    BEGIN
	over				( list counter list  r: elite-ok )
	r@ 0 ?DO			( list counter current-node )
	    over fixed-population-size @ < 0= IF
		drop 2drop unloop rdrop EXIT
	    THEN
	    next-node
	    dup cell+ @ >spot!  fcp @ cp!
	    reproduce-xt @ EXECUTE
	    swap 1+ swap
	LOOP
	drop
    AGAIN ;

: ?log-pop-control-elitism ( -- )
    log-mask @       0= IF  EXIT  THEN
    log-pop-control? 0= IF  EXIT  THEN

    s" Population control step: "	cat-log
    step @ num>string			cat-log
    s" 	individuals: "			cat-log
    score-list @ nodes num>string	cat-log
    s"   	elite: "		cat-log
    elite @ num>string			0 log-it ;

\ Population control: Only keep elite and fill with mutations of it:
:NONAME ( -- )	\ : ?elitism-pop-control ( -- )
    elitism? 0= IF EXIT THEN

    ?log-pop-control-elitism

    \ remove weak individuals:
    score-list @ >r	( r: score-list )
    r@ nodes  elite @  > IF
	elite @  r@ n'th-node
	BEGIN			( current-weak-node  r: score-list )
	    dup cell+ @ >spot!
	    fcp @ cp! die
	    next-node
	    dup 0=
	UNTIL drop		( r: score-list )
	elite @ r@ remove-node&following	\ remove from score-list too
    THEN
    rdrop

    \ refill population with mutated elite:
    fill-population

    spot-display-on? IF
	cursor-off	\ ########################
	(brew-redisplay)
	cursor-off		\ quick cosmetic bug fix ;-)
    THEN
; IS ?elitism-pop-control

\ hack: guess scoring-xt	########################
\ Old brew did not use scoring-xt and so it is often not set correctly.
\
\ This words checks for the  eat-xt  of the active cell in  eat-actions  list.
\ If found it checkes if there is a corresponding scoring function in the list.
\ If so, it sets  scoring-xt  accordingly.
: guess-scoring-function ( -- )	\ hack
    cp@ 0= IF  EXIT  THEN

    eat-actions
    dup nodes 0 ?DO
	next-node
	dup @  eat-xt @ = IF
	    dup cell+ @ dup IF
		scoring-xt !
		drop unloop EXIT
	    ELSE drop THEN
	THEN
    LOOP drop ;


MENU: population-men
: .menu-population-brew0 ( -- )
    cr
    s" Live stress, additive step: "
    ['] additive-stress simple-menu-entry-variable
    1 2 screen-column
    s" Stress rate:            "  ['] stress-rate  simple-menu-entry-scale cr

    s" Code price, additive step:  "
    ['] code-additive-stress simple-menu-entry-variable
    1 2 screen-column
    s" Code price rate         " ['] code-stress-rate simple-menu-entry-scale
    cr

    cr
    s" High water mark:     "  ['] high-water-mark  simple-menu-entry-variable
    s" h" menu-same-key-entry

    high-water-mark @ spots < IF
	1 2 screen-column
	s" Flood-mark:             "  ['] flood-mark simple-menu-entry-variable
	cr
	s" f" menu-same-key-entry

	flood-mark @ spots < IF
	    s" Flood stress rate:   "
	    ['] flood-stress-rate simple-menu-entry-scale
	    1 2 screen-column
	    s" Fl. energy rate:        "
	    ['] flood-energy-rate simple-menu-entry-scale
	    s" e" menu-same-key-entry
	    cr
	    s" Flood food rate:     "
	    ['] flood-food-rate simple-menu-entry-scale
	    1 2 screen-column
	    s" Flood kill rate:        "
	    ['] flood-kill-rate simple-menu-entry-scale
	    s" k" menu-same-key-entry
	THEN
    THEN cr

    cr
    s" Low water mark:      "  ['] low-water-mark  simple-menu-entry-variable
    s" l" menu-same-key-entry

    low-water-mark @ 0> IF
	1 2 screen-column
	s" is active after step:   "
	['] up-regulation-start simple-menu-entry-variable cr
	s" p" menu-same-key-entry

	s" Additive release:    "
	['] additive-release  simple-menu-entry-variable
	1 2 screen-column
	s" Multiplicative release: "
	['] multiplicative-release  simple-menu-entry-variable cr

	s" SOS mark:            "  ['] sos-mark	 simple-menu-entry-variable
	s" sS" menu-same-key-entry

	sos-mark @ 0> IF
	    1 2 screen-column
	    s" SOS release rate:       "
	    ['] sos-release-rate simple-menu-entry-scale cr
	    s" Reproduction energy: "
	    ['] sos-reproduction-push simple-menu-entry-variable
	    1 2 screen-column
	    s" SOS sow:                " ['] sos-sow simple-menu-entry-variable
	THEN
    THEN cr

    cr
    s" Current energy cost of each life step: "
    ['] nuc-do-cost  simple-menu-entry-variable cr
    s" n" menu-same-key-entry

    s" Can this 'cost' become a source of help in emergency situations? "
    ['] nuc-cost-can-be-help? >stack	['] toggle-named   redisplay menu-entry
    nuc-cost-can-be-help? @ .YES-NO-entry cr
    cr

    s" Current energy cost of code usage: "
    ['] code-price  simple-menu-entry-variable
    s" c" menu-same-key-entry

    1 2 screen-column
    s" rating scale "	['] code-price-scale	simple-menu-entry-scale cr
    s" r" menu-same-key-entry

    s" Can this 'cost' become a source of help in emergency situations? "
    ['] code-price-can-be-help? >stack	['] toggle-named  redisplay menu-entry
    code-price-can-be-help? @ .YES-NO-entry cr

    s" Scoring rate: "	redisplay	['] score-rate simple-menu-entry-scale
    s"   (Not all eating words use this currently)." noop-entry cr

    cr
    s" Food supply is another way of population control."
    ['] food-menu			menu-entry cr
    s" f" menu-same-key-entry ;

: fix-elitism-setup ( -- )	\ ################# hack
    1 1 mutation-rate 2!
    1 trial-phase ! ;

: .menu-population-elitistic ( -- )
    cr

    fixed-population-size @		\ assure range
    dup 0 spots 1+ within 0= IF
	bell
	0 max  spots min  fixed-population-size !
    ELSE drop THEN
    s" Population size: "
    ['] fixed-population-size	simple-menu-entry-variable cr
    s" pPs" menu-same-key-entry

    elite @				\ assure range
    dup 0 spots 1+ within 0= IF
	bell
	0 max  spots min  elite !
    ELSE drop THEN
    s" Kept elite:      "
    ['] elite	simple-menu-entry-variable
    s" e" menu-same-key-entry
    elite @  fixed-population-size @ > IF	\ bad value?
	.tab
	s" No, you can't keep more individuals than there are..." type-alert
    THEN
    cr

    cr
    s" Code price scale:        " ['] code-price-scale simple-menu-entry-scale
    cr
    s" cpsr" menu-same-key-entry

    mutation-rate 2@ <>
    trial-phase @ 1 <> OR IF
	cr
	cr from-here
	s" In this brew version it is advised to set mutation rate to 1/1 "
	type-other-colour cr
	s" and trial phase to one if you want to use elitism: "
	type-other-colour
	s" "	['] fix-elitism-setup	redisplay	menu-entry cr

	cr
	s" How often to mutate: "
	['] mutation-rate  simple-menu-entry-scale cr
	s" m" menu-same-key-entry

	cr
	s" Trial phase: "	['] trial-phase	 simple-menu-entry-variable cr
	s" pr" menu-same-key-entry
	s" After how many generations will genes be compiled and put into the gene pool."
	same-menu-entry cr
    THEN ;

: toggle-elitism ( -- )   run-mode dup @ elitism xor swap ! ;

: .menu-population ( -- )
    help-node" Population control"
    s" Population control.  Mean, automatic stress producer!   "
    start-title-entry

    living @ dup IF
	(highlite-active) dup dup @ 2>r off
	>r s" (actually alive: " r> ['] .sorry menu-entry-value
	.bs ." )"
	r> r> !
    ELSE drop THEN
    end-title

    <bright-colours>
    elitism? IF
	s" ELITISM"
    ELSE
	s" EAT AND CONSUME"
    THEN 
    redisplay	['] toggle-elitism	menu-entry
    reset-colours
    elitism? IF
	s" 		Only the fittest individuals are kept.  Fixed population size."
    ELSE
	s" 	Brews pseudo biological population control mechanism."
    THEN
    type-other-colour up-to-here cr
    s" toDE" menu-same-key-entry

    elitism? IF
	.menu-population-elitistic
    ELSE
	.menu-population-brew0
    THEN
    
    <common-menu-entries> ;

: menu-population-control
    population-men
    ['] .menu-population menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' menu-population-control function-key-actions >list

\ ****************************************************************
\ end	population control



\ ****************************************************************
\ **********  Diversification:  diversification-menu  ************
\ ****************************************************************

VARIABLE diversification-range		\ up to how much
2VARIABLE diversification-rate		\ how often.
1 4 diversification-rate 2!
VARIABLE nuc-diversification-closeness	\ the bigger, the less likely are
					\ big deviations
2 nuc-diversification-closeness !	\ 2 was default before
VARIABLE sporadic-value-range		65536 sporadic-value-range !
2VARIABLE sporadic-value-rate		\ how often to set any new values
1 100 sporadic-value-rate 2!
: diversify ( addr -- )			\ diversifies contents of addr
    diversification-range @		\ never more than this
    nuc-diversification-closeness @ 0 ?DO	\ looping randomization
	1+ random-ranged		\ loop: big deviations get rarer
    LOOP
    2 random-ranged IF negate THEN	\ sign
    swap +! ;

: sporadic-replace-value ( addr -- )
    sporadic-value-range @ 1+ random-ranged
    2 random-ranged IF negate THEN	\ sign
    swap +! ;

: diversify? ( addr -- )		\ maybe diversify contents of addr
    dup diversification-rate rated-flag IF  diversify           ELSE drop THEN
    sporadic-value-rate  rated-flag IF  sporadic-replace-value  ELSE drop THEN
;

: diversify?-which ( mask -- )
    diversification-rate rated-flag IF
	my-diversifctn-mask @ and
	nuc-diversificable-area dup nuc-diversificable-items + swap DO
	    dup  1  i nuc-diversificable-area -  lshift  and IF
		i nuc-addr diversify
	    THEN
	LOOP
    THEN
    drop ;

VARIABLE diversification-mask
nuc-organs# nuc-parameters# + nuc-invisibles# +
set-n-low-bits diversification-mask !

LIST: nuc-div-masks
nuc-div-masks 0
s" div-organ-" nuc-organs#  LISTED-MASKS-append-char
s" div-parameter-" nuc-parameters#  LISTED-MASKS-append-char
s" div-invisible-" nuc-invisibles#  LISTED-MASKS-append-char
2drop

nuc-floats# [IF]
    VARIABLE global-f-organ-div-mask
    nuc-f-organs# set-n-low-bits global-f-organ-div-mask !

    VARIABLE global-f-parameter-div-mask
    nuc-f-parameters# set-n-low-bits global-f-parameter-div-mask !

    VARIABLE global-f-invisible-div-mask
    nuc-f-invisibles# set-n-low-bits global-f-invisible-div-mask !
[THEN]

spot-floats# [IF]
    VARIABLE f-qualities-div-mask
    f-qualities-div-mask off
    \ spot-f-qualities# set-n-low-bits f-qualities-div-mask ! \ no, default off

    VARIABLE f-properties-div-mask
    f-properties-div-mask off
    \ spot-f-properties# set-n-low-bits f-properties-div-mask !       \ no, off

    VARIABLE f-secrets-div-mask
    f-secrets-div-mask off
    \ spot-f-secrets# set-n-low-bits f-secrets-div-mask !     \ no, default off
[THEN]

LIST: item-masks		\ new: same masks for everything
item-masks 0
s" |" 32 LISTED-MASKS-append-char
2drop

dfVARIABLE nuc-f-diversification-rate	0.25e0 nuc-f-diversification-rate df!
dfVARIABLE nuc-f-diversification-range	50e0 nuc-f-diversification-range df!
dfVARIABLE nuc-f-diversification-factor	2e0 nuc-f-diversification-factor df!

dfVARIABLE spot-f-diversification-range	50e0 spot-f-diversification-range df!
dfVARIABLE spot-f-diversification-factor
1e0 spot-f-diversification-factor df!

dfVARIABLE f-sporadic-value-rate	1e-2 f-sporadic-value-rate df!
dfVARIABLE f-sporadic-value-range	1e5 f-sporadic-value-range df!

: random-closeness-factor ( closeness -- r<=1e0 )
    1e0
    ( closeness ) 0 ?DO	\ looping randomization
	frandom	f*			\ loop: big deviations get rarer
    LOOP ;

: df-diversify-relative ( closeness df-addr  f: factor -- )
    >r
    ( f: factor ) 1e0 f-
    ( closeness ) random-closeness-factor f*
    1e0 f+
    2 random-ranged IF 1/f THEN			\ multiply or divide
    r@ df@  f*  r> df! ;

nuc-floats# [IF]

: nuc-df-diversify-additive ( addr -- )
    >r
    nuc-f-diversification-range df@
    nuc-diversification-closeness @ random-closeness-factor f*
    2 random-ranged IF fnegate THEN      \ sign
    r@ df@ f+ r> df! ;

: nuc-df-diversify-relative ( df-addr -- )
    nuc-diversification-closeness @ swap
    nuc-f-diversification-factor df@
    df-diversify-relative ;

: nuc-df-diversify ( addr -- )
    2 random-ranged IF
	nuc-df-diversify-additive
	EXIT
    THEN
    nuc-df-diversify-relative ;

: nuc-df-sporadic ( addr -- )
    >r
    f-sporadic-value-range df@  frandom f*
    2 random-ranged IF fnegate THEN      \ sign
    r@ df@ f+ r> df! ;
    
: nuc-df-diversify? ( addr -- )
    nuc-f-diversification-rate df@ f-rated-flag IF nuc-df-diversify EXIT THEN
    f-sporadic-value-rate df@ f-rated-flag IF nuc-df-sporadic EXIT THEN
    drop ;

: diversify?-df-items ( base mask u -- )
    over 0= IF  drop 2drop  EXIT  THEN

    0 ?DO	( base mask )
	dup  1 i lshift  and IF
	    over i dfloats + nuc-df-diversify?
	THEN
    LOOP 2drop ;

nuc-f-organs# [IF]
: diversify?-f-organs ( -- )
    nuc-f-organ-base
    global-f-organ-div-mask @ f-organ-div-mask @ and
    nuc-f-organs# diversify?-df-items ;
[THEN]

nuc-f-parameters# [IF]
: diversify?-f-parameters ( -- )
    nuc-f-parameter-base
    global-f-parameter-div-mask @ f-param-div-mask @ and
    nuc-f-parameters# diversify?-df-items ;
[THEN]

nuc-f-invisibles# [IF]
: diversify?-f-invisibles ( -- )
    nuc-f-invisible-base
    global-f-invisible-div-mask @ f-invisibl-div-mask @ and
    nuc-f-invisibles# diversify?-df-items ;
[THEN]

[THEN] \ nuc-floats#

: diversify?-some ( -- )
[ nuc-organs# nuc-parameters# nuc-invisibles# + + ] [IF]
    diversification-mask @ diversify?-which
[THEN]

[ nuc-floats# ] [IF]
    nuc-f-diversification-rate df@
    fdup f0= IF fdrop ELSE		\ don't change random seed if zero
	f-rated-flag IF			\ entry level test
	    [ nuc-f-organs# ] [IF]
		diversify?-f-organs
	    [THEN]

	    [ nuc-f-parameters# ] [IF]
		diversify?-f-parameters
	    [THEN]

	    [ nuc-f-invisibles# ] [IF]
		diversify?-f-invisibles
	    [THEN]
	THEN
    THEN
[THEN]
;

' diversify?-some is <diversify>

\ Spot diversification:
\ If 'inhabited-only-div' (first bit of 'spot-diversification-mask')
\ is not set, spot diversification is done on the whole spot variable
\ planes.  Otherwise it's done separately and only on inhabited spots.
VARIABLE spot-diversification-mask	spot-diversification-mask off
LIST: spot-div-masks
spot-div-masks 0
LISTED-MASK: inhabited-only-div
LISTED-MASK: food-div
s" -quality-div"  spot-qualities#  LISTED-MASKS-pre-char
s" -property-div" spot-properties# LISTED-MASKS-pre-char
s" -secret-div"	  spot-secrets#    LISTED-MASKS-pre-char
2drop

VARIABLE spot-diversification-range	50 spot-diversification-range !
VARIABLE spot-diversifictn-closeness	2 spot-diversifictn-closeness !

: spot-integer-diversify ( addr -- )		\ diversifies contents of addr
    spot-diversification-range @		\ never more than this
    spot-diversifictn-closeness @ 0 ?DO	\ looping randomization
	1+ random-ranged			\ loop: big deviations rarer
    LOOP
    2 random-ranged IF negate THEN		\ sign
    swap +! ;

spot-floats# [IF]

: spot-df-diversify-additive ( addr -- )
    >r
    spot-f-diversification-range df@
    spot-diversifictn-closeness @ random-closeness-factor f*
    2 random-ranged IF fnegate THEN      \ sign
    r@ df@ f+ r> df! ;

: spot-df-diversify-relative ( df-addr -- )
    spot-diversifictn-closeness @ swap
    spot-f-diversification-factor df@
    df-diversify-relative ;

: spot-df-diversify ( addr -- )
    2 random-ranged IF
	spot-df-diversify-additive
	EXIT
    THEN
    spot-df-diversify-relative ;

\ : spot-df-family-diversify ( mask base count -- )
\     third 0= IF  2drop drop EXIT  THEN

\     0 ?DO	( mask current-addr )
\ 	1 i lshift third and IF
\ 	    dup spot-df-diversify
\ 	THEN
\ 	[ 1 dfloats ] literal +
\     LOOP
\     2drop ;

\ Check for zero mask from calling word, as this word is used *a lot*.
: spot-df-family-diversify ( mask start-index count -- )
\   third 0= IF  2drop drop EXIT  THEN		\ no, check from calling word

    0 ?DO	( mask start-index )
	over 1 i lshift and IF
	    dup i + n'th-spot-f-variable spot-df-diversify
	THEN
    LOOP
    2drop ;

spot-f-qualities# [IF]
: f-qualities-diversify ( -- )
    f-qualities-div-mask @
    dup 0= IF drop EXIT THEN	\ check here for efficiency reasons

    0  spot-f-qualities#  spot-df-family-diversify ;
[THEN]

spot-f-qualities# [IF]
: f-properties-diversify ( -- )
    f-properties-div-mask @
    dup 0= IF drop EXIT THEN	\ check here for efficiency reasons

    spot-f-qualities#  spot-f-properties#
    spot-df-family-diversify ;
[THEN]

spot-f-properties# [IF]
: f-secrets-diversify ( -- )
    f-secrets-div-mask @
    dup 0= IF drop EXIT THEN	\ check here for efficiency reasons

    [ spot-f-qualities#  spot-f-properties# + ] literal
    spot-secrets#  spot-df-family-diversify ;
[THEN]

[THEN] \ spot-floats#

\ : ?diversify-spots ( -- )
:NONAME ( -- )
    spot-diversification-mask @
    dup inhabited-only-div and IF drop EXIT THEN

    spot @ >r

    spots 0 DO
	i >spot!

	dup IF
	    field-i-planes# 1 ?DO
		1 i lshift over and IF
		    i n'th-spot-variable spot-integer-diversify
		THEN
	    LOOP
	THEN

[ spot-f-qualities# ] [IF]
	f-qualities-diversify
[THEN]

[ spot-f-properties# ] [IF]
	f-properties-diversify
[THEN]

[ spot-f-secrets# ] [IF]
	f-secrets-diversify
[THEN]

    LOOP
    drop

    r> >spot!
; IS ?diversify-spots

\ : ?diversify-inhabited ( -- )
:NONAME ( -- )
    spot-diversification-mask @
    dup inhabited-only-div and 0= IF drop EXIT THEN

    dup IF
	field-i-planes# 1 ?DO
	    1 i lshift over and IF
		i n'th-spot-variable spot-integer-diversify
	    THEN
	LOOP
    THEN
    drop

[ spot-f-qualities# ] [IF]
    f-qualities-diversify
[THEN]

[ spot-f-properties# ] [IF]
    f-properties-diversify
[THEN]

[ spot-f-secrets# ] [IF]
    f-secrets-diversify
[THEN]
; IS ?diversify-inhabited

2VARIABLE global-i-diversifictn-rate
0 1 global-i-diversifictn-rate 2!
VARIABLE globals-diversifictn-range	500 globals-diversifictn-range !
VARIABLE globals-divers-closeness
2 globals-divers-closeness !
VARIABLE global-diversification-mask	global-diversification-mask off
VARIABLE global-df-div-mask		global-df-div-mask off
2VARIABLE global-f-diversifctn-rate
0 1 global-f-diversifctn-rate 2!
dfVARIABLE global-f-diversifctn-range
50e0 global-f-diversifctn-range df!
dfVARIABLE global-f-diversifctn-factor
2e0 global-f-diversifctn-factor df!

: ?diversify-global-integers ( -- )
    global-diversification-mask @      dup 0= IF  drop EXIT  THEN
    global-i-diversifictn-rate 2@  over 0= IF 2drop EXIT  THEN
    global-int-variables
    dup nodes 0 ?DO ( div-mask rate1 rate2 current-node )
	next-node
	fourth 1 i lshift and IF			\ check mask
	    third third random-ranged > IF		\ check rate
		dup @ EXECUTE
		globals-diversifictn-range @		\ never more than this
		globals-divers-closeness @ 0 ?DO	\ looping randomization
		    1+ random-ranged	\ loop: big deviations get rarer
		LOOP
		2 random-ranged IF negate THEN		\ sign
		swap +!
	    THEN
	THEN
    LOOP
    2drop 2drop ;

: ?diversify-global-floats ( -- )
    global-df-div-mask @		dup  0= IF  drop EXIT  THEN
    global-f-diversifctn-rate 2@	over 0= IF 2drop EXIT  THEN
    global-dfloat-variables
    dup nodes 0 ?DO ( div-mask rate1 rate2 current-node )
	next-node
	fourth 1 i lshift and IF			\ check mask
	    third third random-ranged > IF		\ check rate
		dup @ EXECUTE
		global-f-diversifctn-range df@		\ additive
		globals-divers-closeness @ random-closeness-factor f*
		2 random-ranged IF fnegate THEN
		dup df+!

		globals-divers-closeness @ swap	\ multiplicative
		global-f-diversifctn-factor df@
		df-diversify-relative
	    THEN
	THEN
    LOOP
    2drop 2drop ;

:NONAME  \ : ?diversify-globals ( -- )
    ?diversify-global-integers
    ?diversify-global-floats ; IS ?diversify-globals

nuc-floats# spot-floats# + [IF]

: items-bitmask-entry ( bitmask-variable-xt u addr count -- )
    third 0= IF  2drop 2drop EXIT  THEN
    noop-entry
    >r dup EXECUTE @	( bitmask-variable-xt bitmask  r: u )
    r> 0 DO
	dup  1 i lshift  and IF  [char] A  ELSE  [char] a  THEN i +
	pad c!
	1 i lshift >stack	over ( xt ) >stack-2
	pad 1   ['] n-named-xor!	redisplay	menu-entry
	\ [char] A i + #key-same-entry
    LOOP
    2drop ;

[THEN]

nuc-floats# [IF]
: nuc-global-f-div-entries ( -- )
    s" Global nuc float diversification mask:" noop-entry cr

    ['] global-f-organ-div-mask nuc-f-organs#
    s" f-organs: " items-bitmask-entry cr

    ['] global-f-parameter-div-mask nuc-f-parameters#
    s" f-parameters: " items-bitmask-entry cr

    ['] global-f-invisible-div-mask nuc-f-invisibles#
    s" f-invisibles: " items-bitmask-entry cr ;
[THEN]

: integer-nuc-item-flags-entry ( item-mask-variable-xt -- )
    dup EXECUTE @	( item-mask-variable-xt item-mask )
    nuc-organs# IF
	s" organs: " noop-entry
	nuc-organs# 0 DO  ( item-mask-variable-xt item-mask )
	    dup  1 i lshift  and IF  [char] A  ELSE  [char] a  THEN i +
	    pad c!
	    1 i lshift >stack	over >stack-2
	    pad 1   ['] n-named-xor!	redisplay	menu-entry
	    \ [char] A i + #key-same-entry
	LOOP
	cr
    THEN

    nuc-parameters# IF
	s" parameters: " noop-entry
	nuc-parameters# 0 DO  ( item-mask-variable-xt item-mask )
	    dup  1 i nuc-organs# + lshift
	    and IF  [char] A  ELSE  [char] a  THEN i + pad c!
	    1 i nuc-organs# + lshift >stack	over >stack-2
	    pad 1   ['] n-named-xor!	redisplay	menu-entry
	LOOP
	cr
    THEN

    nuc-invisibles# IF
	s" invisibles: " noop-entry
	nuc-invisibles# 0 DO  ( item-mask-variable-xt item-mask )
	    dup  1 i nuc-organs# + nuc-parameters# + lshift
	    and IF  [char] A  ELSE  [char] a  THEN i + pad c!
	    1 i nuc-organs# + nuc-parameters# + lshift >stack	over >stack-2
	    pad 1    ['] n-named-xor!	redisplay	menu-entry
	LOOP
	cr
    THEN

    2drop ;

: nuc-global-i-div-entries ( -- )
    s" Global nuc integer diversification mask:" noop-entry cr
    ['] diversification-mask integer-nuc-item-flags-entry ;

MENU: diversification-men
: .nuc-diversification-menu ( -- )
    help-node" NUC diversification"
    cr

    s" INTEGER nuc variables:" noop-entry cr
    s" rate:     "  ['] diversification-rate	simple-menu-entry-scale
    s" r"	menu-same-key-entry
    1 3 screen-column
    nuc-diversification-closeness @ 0< IF  \ range control during redisplay ;-)
	bell s" Closeness can't be negative, reset." type-alert cr
	1 nuc-diversification-closeness !
    THEN
    s" closeness:  "
    ['] nuc-diversification-closeness	simple-menu-entry-variable
    s"   (fewer big deviations)" .menu-expansion cr
    s" c" menu-same-key-entry

    s" ± range:  "  ['] diversification-range	simple-menu-entry-variable cr
    s" md" menu-same-key-entry
    s" sporadic: "  ['] sporadic-value-rate	simple-menu-entry-scale
    s" sp" menu-same-key-entry
    1 3 screen-column
    s" ± range:    "  ['] sporadic-value-range	simple-menu-entry-variable cr

    cr
    nuc-global-i-div-entries

[ nuc-floats# ] [IF]
    cr
    s" FLOATING point nuc variables:" noop-entry cr
    s" rate:     "  ['] nuc-f-diversification-rate simple-dfloat-variable-entry
    s" r"	menu-same-key-entry
    1 3 screen-column	s" closeness:  "
    ['] nuc-diversification-closeness	simple-menu-entry-variable
    s"   (fewer big deviations)" .menu-expansion cr
    s" c" menu-same-key-entry

    s" ± range:  "
    ['] nuc-f-diversification-range simple-dfloat-variable-entry
    s" md" menu-same-key-entry
    1 3 screen-column
    s" * relative: "
    ['] nuc-f-diversification-factor		simple-dfloat-variable-entry cr
    s" *" menu-same-key-entry
    s" sporadic: "
    ['] f-sporadic-value-rate	simple-dfloat-variable-entry
    s" sp" menu-same-key-entry
    1 3 screen-column
    s" ± range:    "
    ['] f-sporadic-value-range	simple-dfloat-variable-entry cr

    cr
    nuc-global-f-div-entries

[THEN] \ nuc-floats#
;

: integer-spot-item-flags-entry ( item-mask-variable-xt -- )
    dup EXECUTE @	( item-mask-variable-xt item-mask )
    spot-qualities# IF
	s" qualities:  " noop-entry
	spot-qualities# 2 + 2 DO  ( item-mask-variable-xt item-mask )
	    dup  1 i lshift  and IF  [char] A  ELSE  [char] a  THEN i 2 - +
	    pad c!
	    1 i lshift >stack	over >stack-2
	    pad 1   ['] n-named-xor!	redisplay	menu-entry
	LOOP
	cr
    THEN

    spot-properties# IF
	s" properties: " noop-entry
	[ 2 spot-qualities# + ] literal >r
	r@ spot-properties# + r> DO  ( item-mask-variable-xt item-mask )
	    dup  1 i lshift
	    and IF  [char] A  ELSE  [char] a  THEN
	    i [ 2 spot-qualities# + ] literal - + pad c!
	    1 i lshift >stack	over >stack-2
	    pad 1   ['] n-named-xor!	redisplay	menu-entry
	LOOP
	cr
    THEN

    spot-secrets# IF
	s" secrets:    " noop-entry
	[ 2 spot-qualities# + spot-properties# + ] literal >r
	r@ spot-secrets# + r> DO  ( item-mask-variable-xt item-mask )
	    dup  1 i lshift
	    and IF  [char] A  ELSE  [char] a  THEN
	    i [ 2 spot-qualities# + spot-properties# + ] literal - + pad c!
	    1 i lshift >stack	over >stack-2
	    pad 1    ['] n-named-xor!	redisplay	menu-entry
	LOOP
	cr
    THEN

    2drop ;


spot-floats# [IF]
: spot-f-div-entries ( -- )
    s" Spot float diversification mask: " noop-entry cr

[ spot-f-qualities# ] [IF]
    ['] f-qualities-div-mask spot-f-qualities#
    s" f-qualities:  " items-bitmask-entry cr
[THEN]

[ spot-f-properties# ] [IF]
    ['] f-properties-div-mask spot-f-properties#
    s" f-properties: " items-bitmask-entry cr
[THEN]

[ spot-f-secrets# ] [IF]
    ['] f-secrets-div-mask spot-f-secrets#
    s" f-secrets:    " items-bitmask-entry cr
[THEN]
;
[THEN]

: .spot-diversification-menu ( -- )
    cr
    from-here
    spot-diversification-mask @ inhabited-only-div and IF
	." diversifying only "
	s" INHABITED" type-bright
    ELSE
	." diversifying inhabited and "
	s" EMPTY" type-bright
    THEN
    s"  spots."
    ['] inhabited-only-div >stack	['] spot-diversification-mask >stack-2
    redisplay	['] named-xor! menu-entry cr
    s" iIeE" menu-same-key-entry

    cr
    s" Diversification of INTEGER spot variables:" noop-entry cr

    s" range:    "  ['] spot-diversification-range  simple-menu-entry-variable
    s" md" menu-same-key-entry
    1 4 screen-column
    \ range control during redisplay ;-)
    spot-diversifictn-closeness @ 0< IF
	bell cr s" Closeness can't be negative, reset." type-alert cr
	spot-diversifictn-closeness off
    THEN
    s" closeness: "
    ['] spot-diversifictn-closeness		simple-menu-entry-variable
    s"   (fewer big deviations)" .menu-expansion cr
    s" c" menu-same-key-entry

    cr
    s" Spot integer diversification mask:" noop-entry cr
    ['] spot-diversification-mask integer-spot-item-flags-entry
    
[ spot-floats# ] [IF]
    cr
    s" FLOATING point spot variables: " noop-entry
\    s" rate:     "  ['] spot-f-diversification-rate simple-dfloat-variable-entry
\    s" r"	menu-same-key-entry
    .tab
    s" closeness:  "  ['] spot-diversifictn-closeness  simple-menu-entry-variable
    s"   (fewer big deviations)" .menu-expansion cr
    s" c" menu-same-key-entry

    s" ± range:  "
    ['] spot-f-diversification-range simple-dfloat-variable-entry
    s" md" menu-same-key-entry
    1 3 screen-column
    s" * relative: "
    ['] spot-f-diversification-factor		simple-dfloat-variable-entry cr
    s" *" menu-same-key-entry
\     s" sporadic: "
\     ['] f-sporadic-value-rate	simple-dfloat-variable-entry
\     s" sp" menu-same-key-entry
\     1 3 screen-column
\     s" ± range:    "
\     ['] f-sporadic-value-range	simple-dfloat-variable-entry cr

    cr
    spot-f-div-entries

[THEN] \ spot-floats#
;

: .globals-diversification-menu ( -- )
    cr cr
    s" Diversification of global INTEGER variables:" noop-entry cr

    s" rate:     "
    ['] global-i-diversifictn-rate simple-menu-entry-scale
    s" r"	menu-same-key-entry
    1 4 screen-column
    \ range control during redisplay ;-)
    globals-divers-closeness @ 0< IF
	bell cr s" Closeness can't be negative, reset." type-alert cr
	globals-divers-closeness off
    THEN
    s" closeness: "
    ['] globals-divers-closeness simple-menu-entry-variable
    s"   (fewer big deviations)" .menu-expansion cr
    s" c" menu-same-key-entry
    s" range:    " ['] globals-diversifictn-range simple-menu-entry-variable
    s" md" menu-same-key-entry
    cr

    cr
    s" Diversification mask for global integer variables:" noop-entry
    .tab
    s" all"	redisplay
    [ 0 global-integer-variables# 0 set-bitrange ] literal >stack
    global-diversification-mask >stack-2	['] !		menu-entry
    .tab
    s" off"	redisplay
    ['] global-diversification-mask >stack	['] named-off	menu-entry cr
    ['] global-diversification-mask global-integer-variables#
    s" " items-bitmask-entry cr
    
    cr cr
    s" Diversification of global FLOATING point variables: " noop-entry cr
    s" rate:     "
    ['] global-f-diversifctn-rate simple-menu-entry-scale
    s" r"	menu-same-key-entry
    .tab
    s" closeness:  "
    ['] globals-divers-closeness simple-menu-entry-variable
    s"   (fewer big deviations)" .menu-expansion cr
    s" c" menu-same-key-entry

    s" ± range:  "
    ['] global-f-diversifctn-range simple-dfloat-variable-entry
    s" md" menu-same-key-entry
    1 3 screen-column
    s" * relative: "
    ['] global-f-diversifctn-factor		simple-dfloat-variable-entry cr
    s" *" menu-same-key-entry

    cr
    s" Diversification mask for global float variables:" noop-entry
    .tab
    s" all"	redisplay
    [ 0 global-dfloat-variables# 0 set-bitrange ] literal >stack
    global-df-div-mask >stack-2		['] !			menu-entry
    .tab
    s" off"	redisplay
    ['] global-df-div-mask >stack	['] named-off		menu-entry cr
    ['] global-df-div-mask global-dfloat-variables# s" " items-bitmask-entry
    cr ;

VARIABLE (diversification-menu-type)
LIST: diversification-menu-types

global-integer-variables# global-dfloat-variables# + [IF]
    global-locality% diversification-menu-types >list
    global-locality% (diversification-menu-type) !
[THEN]

spot-qualities# spot-properties# spot-secrets# + +
spot-f-qualities# spot-f-properties# spot-f-secrets# + +
+ [IF]
    spot-local% diversification-menu-types >list
    spot-local% (diversification-menu-type) !
[THEN]

nuc-organs# nuc-parameters# nuc-invisibles# + +
nuc-f-organs# nuc-f-parameters# nuc-f-invisibles# + +
+ [IF]
    nuc-local% diversification-menu-types >list
    nuc-local% (diversification-menu-type) !
[THEN]

: cycle-div-menu-type ( -- )
    (diversification-menu-type) @  diversification-menu-types key>list-index IF
	1+  diversification-menu-types nodes mod
    ELSE
	0
    THEN
    diversification-menu-types n'th-node @  (diversification-menu-type) ! ;

: .diversification-menu ( -- )
    help-node" Diversification menu"
    s" Diversification menu:" menu-title-entry

    from-here ." Showing diversification of "
    (diversification-menu-type) @ CASE
	global-locality% OF
	    s" GLOBAL" type-bright
	    s"  variables:"	    redisplay
	    ['] cycle-div-menu-type	menu-entry cr
	    s" wnNsSto" menu-same-key-entry
	    .globals-diversification-menu
	ENDOF
	nuc-local% OF
	    s" NUC" type-bright
	    s"  variables:"	    redisplay
	    ['] cycle-div-menu-type	menu-entry cr
	    s" wnNsSto" menu-same-key-entry
	    .nuc-diversification-menu
	ENDOF
	spot-local% OF
	    s" SPOT" type-bright
	    s"  variables:"    redisplay
	    ['] cycle-div-menu-type	menu-entry cr
	    s" wnNsSto" menu-same-key-entry
	    .spot-diversification-menu
	ENDOF
	true ABORT" .diversification-menu: Unknown locality type."
    ENDCASE

    <common-menu-entries> ;

: diversification-menu ( -- )
    diversification-men
    ['] .diversification-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' diversification-menu function-key-actions >list

:NONAME \ : |menu-diversify-global-vars| ( -- )
    global-locality% (diversification-menu-type) !
    diversification-menu ; IS |menu-diversify-global-vars|

\ ****************************************************************
\ end	diversification



\ ****************************************************************
\ *************************  Colors:  ****************************
\ ****************************************************************

\ Some color related stuff:

MENU: conditional-colouring-men

LIST: cond-fg-colour-functions
' generic-hit>fg-color		cond-fg-colour-functions >list
' generic-range>fg-color	cond-fg-colour-functions >list
\ Tolerated, but not offered as a selection possibility:
\ ' selected>fg-color		cond-fg-colour-functions >list

LIST: cond-bg-colour-functions
' generic-hit>bg-color		cond-bg-colour-functions >list
' generic-range>bg-color	cond-bg-colour-functions >list

: .conditional-colouring-menu ( -- )
    help-node" Conditional colouring"
    s" Conditional colouring menu:" menu-title-entry

    cr from-here
    (selection-mask) @ select-nuc-related and IF 
	." Conditional "  s" foreground" type-bright  s" colouring: "
	redisplay	['] spot-related-selections		menu-entry
	fg-colour-field
	nuc-related-selections
    ELSE
	." Conditional "  s" background" type-bright  s"  colouring: "
	redisplay	['] nuc-related-selections		menu-entry
	bg-colour-field
	spot-related-selections
    THEN maybe-fix-condition
    cr cr
    s" o"	menu-same-key-entry

    \ make sure selections make sense:
    (do-it-xt) @ CASE
	['] generic-hit>fg-color OF ENDOF
	['] generic-hit>bg-color OF ENDOF
	['] generic-range>fg-color OF
	    ['] = (condition-xt) !
	    coloured-on-range-possible? 0= IF
		bell
		['] variable-number (expression-xt) !
	    THEN
	ENDOF
	['] generic-range>bg-color OF
	    ['] = (condition-xt) !
	    coloured-on-range-possible? 0= IF
		bell
		['] variable-number (expression-xt) !
	    THEN
	ENDOF
	['] selected>fg-color OF ENDOF	\ we tolerate that here
	ABORT" .conditional-colouring-menu: Unknown '(do-it-xt)'."
    ENDCASE

    s" Choose type of conditional coulouring:" menu-title!
    (selection-mask) @ select-nuc-related and IF 
	['] cond-fg-colour-functions >stack
    ELSE
	['] cond-bg-colour-functions >stack
    THEN
    ['] (do-it-xt) >stack-2
    s" Type: "	['] choose-xt-to-var	menu-entry
    s" tT" menu-same-key-entry
    .tab
    (do-it-xt) @	dup >stack	menu-wait	redisplay
    xt>string		['] <page-see>			menu-entry cr

    cr
    conditional-expression-entries cr

    <common-menu-entries> ;

: conditional-colouring-menu ( fg/bg-flag -- )
    IF nuc-related-selections ELSE spot-related-selections THEN
    conditional-colouring-men
    ['] .conditional-colouring-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus

    \ Let's assume the user wants colours if he visits this menu:
    (selection-mask) @ select-nuc-related and IF
	spot-foreground-coloring!
    ELSE
	spot-background-coloring!
    THEN ;

VARIABLE age>color-scale	20 age>color-scale !
: age>color ( -- col )
    age @ colors *
    age-threshold @		\ scaling: if there's an age-threshold
    ?dup 0= IF			\          take this one
	age>color-scale @	\          otherwise take age>color-scale
    THEN			\	   as length of the color cycle
    /				\ proportion: colors/cycle
    2-color ;			\ normalized
' age>color foreground-color-xt !
' age>color x>fg-color >list

: generation>color ( -- col )	generation @ 2-color ;
' generation>color  x>fg-color >list

: genome>color ( -- col )   genome-id @ 1+ 2-color ;
' genome>color x>fg-color >list

: genome-generation>color ( -- col )	genome-generation @ 2-color ;
' genome-generation>color  x>fg-color >list

: code>color ( -- col )   code-cost @ (default-gene-cost#) / 2-color ;
' code>color x>fg-color >list

: scoring>color ( -- col )   scoring 1- 2-color ;	\ scoring zero visible
' scoring>color x>fg-color >list

: scoring-hit>bg-color ( -- col )
    spot @ someone-here? IF
	scoring 0= IF  color-selected-bg-xt @ EXECUTE  EXIT  THEN
    THEN
    color-miss-bg-xt @ EXECUTE ;
' scoring-hit>bg-color x>bg-color >list

: score>color ( -- col )   score 2-color ;
' score>color x>fg-color >list

: trial>color ( -- col )
    nuc-flags @ nuc-on-trial and	( 0|1 )
    2* 1- 2-color ;
' trial>color x>fg-color >list

: show-on-trial ( -- )
    ['] trial>color  show-fg-coloured
    s" Shows nucs in trial phase." .last-line ;

nuc-floats# [IF]
: nuc-all-real>color ( -- col )
    nuc-all-real? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' nuc-all-real>color  x>fg-color >list

: nuc-has-unreal>color ( -- col )
    nuc-has-unreal? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' nuc-has-unreal>color  x>fg-color >list

: nuc-inf?>color ( -- col )
    nuc-with-inf? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' nuc-inf?>color  x>fg-color >list

: nuc-neg-inf?>color ( -- col )
    nuc-with-neg-inf? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' nuc-neg-inf?>color  x>fg-color >list

: nuc+inf?>color ( -- col )
    nuc-with-pos-inf? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' nuc+inf?>color  x>fg-color >list

: nuc-nan?>color ( -- col )
    nuc-with-nan? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' nuc-nan?>color  x>fg-color >list
[THEN] \ nuc-floats#

spot-floats# [IF]
: spot-all-real>color ( -- col )
    spot-all-real? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' spot-all-real>color  x>bg-color >list

: spot-has-unreal>color ( -- col )
    spot-has-unreal? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' spot-has-unreal>color  x>bg-color >list

: spot-inf?>color ( -- col )
    spot-with-inf? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' spot-inf?>color  x>bg-color >list

: spot-neg-inf?>color ( -- col )
    spot-with-neg-inf? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' spot-neg-inf?>color  x>bg-color >list

: spot+inf?>color ( -- col )
    spot-with-pos-inf? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' spot+inf?>color  x>bg-color >list

: spot-nan?>color ( -- col )
    spot-with-nan? IF  color-selected-fg-xt  ELSE  color-miss-fg-xt  THEN
    @ EXECUTE ;
' spot-nan?>color  x>bg-color >list
[THEN] \ spot-floats#

CREATE color-scales	field-i-planes# cells allot

: color-scale ( i -- addr )  cells color-scales + ;

\ Define two words for every integer field variable:
\ A word	NAME>color-scale ( -- addr )
\ and a word	NAME>color ( -- col )
\ Put the latter in the list 'x>bg-color'.
: define-bg>color-words ( default -- )
    c-l stringbuf-open	\ buffer for names
    c-l stringbuf-open	\ evaluate buffer
    field-i-planes# 0 ?DO   ( default handle-name handle-evaluate )
	i spot-var-name		fourth string!
	s" >color-scale"	fourth cat		\ name of scale adress
	i cells color-scales +
	num>string		third string!
	s"  CONSTANT "		third cat
	over string@		third cat
	dup string@ EVALUATE				\ compile scale address
	over string@ get-xt EXECUTE >r third r> !	\ initialise

	s" : "			third string!	\ build NAME>color definition
	i spot-var-name		third cat	\ build NAME>color
	s" >color "		third cat
	i spot-var-name		third cat
	s"  @ "			third cat
	over string@		third cat
	s"  @ / 2-color ;"	third cat
	dup string@		EVALUATE

	i spot-var-name		fourth string!
	s" >color"		fourth cat
	over string@ get-xt x>bg-color >list
    LOOP
    stringbuf-close
    stringbuf-close
    drop ;
200 define-bg>color-words

VARIABLE scaling-range		1 scaling-range !
: calibrate-bg-color-scales ( -- )
    do-spot-scan
	field-i-planes# p0 DO			\ skip pointer plane?
	    (scan-spots) @ i 2* 2* cells + >r	\ start data for this quality
	    r@ cell+	@			\ max
	    r>		@	-		\ min -
	    colors	1-	/		\ divided by number of colors
	    scaling-range @	*		\ scaling-range
	    dup IF				\ scales should never be zero
		i color-scale !
	    ELSE drop THEN
	LOOP ;
: cal-bg-color-scales-full
    1 scaling-range !
    calibrate-bg-color-scales ;
: cal-bg-color-scales-half
    2 scaling-range !
    calibrate-bg-color-scales ;
: cal-bg-color-scales-two
    colors 2/ scaling-range !
    calibrate-bg-color-scales ;

LIST: color-list
' black		color-list >list
' red		color-list >list   
' green		color-list >list
' brown		color-list >list
' blue		color-list >list
' magenta	color-list >list
' cyan		color-list >list
' white		color-list >list
[DEFINED] default-color [IF]
' default-color	color-list >list
[THEN]

MENU: color-men

: .color-name ( color -- )	\ what to do if the user chooses a color sample
    color-list n'th-node @	\ get xt of color
    cursor-off
    xt>string  34 2 at-xy type
    800 ms
    cursor-visible ;

: (|menu-step-display|) ( -- )	\ to set colors there
    (step-more-info) >r  r@ @  r@ on
    menu-step-display
    r> ! ;

\ If the user changes a colour function, I assume he wants colouring:
: select-fg-colour ( -- )
    ['] x>fg-color
    ['] foreground-color-xt
    dup EXECUTE @ >r
    choose-xt-to-var
    foreground-color-xt @ r> <> IF
	spot-foreground-coloring!
    THEN ;

\ If the user changes a colour function, I assume he wants colouring:
: select-bg-colour ( -- )
    ['] x>bg-color
    ['] background-color-xt
    dup EXECUTE @ >r
    choose-xt-to-var
    background-color-xt @ r> <> IF
	spot-background-coloring!
    THEN ;

: .color-menu ( -- )
    help-node" Color menu"
    s" Color menu:" menu-title-entry

[UNDEFINED] never-use-colors [IF]	\ compiled with color support
    (highlite-active) dup @ >r off
    cr
    colors 0 DO
	i color-background
	i >stack redisplay	s"     "    ['] .color-name	menu-entry
    LOOP
    0 color-background cr cr
    r> (highlite-active) !

    spot-display-on? IF
	s" Set foreground color:" menu-title!
	s" Foreground color does: "	['] select-fg-colour	menu-entry
	s" F" menu-same-key-entry
	.tab  menu-wait	 redisplay	foreground-color-xt @ dup >stack
	xt>string			['] <page-see>		menu-entry
	foreground-color-xt @ ['] condition>fg-colour = IF
	    ."   "
	    s" EDIT"	redisplay	true >stack
	    ['] conditional-colouring-menu	menu-entry
	THEN
	2 3 screen-column
	s" Fg coloring: "  redisplay	['] spot-foreground-coloring >stack
	['] display-switch >stack-2	['] named-xor!		menu-entry
	spot-foreground-coloring? .ON-off-entry cr
	s" f" menu-same-key-entry

	s" Set background color:" menu-title!
	s" Background color does: "	['] select-bg-colour	menu-entry
	s" B" menu-same-key-entry
	.tab	menu-wait	redisplay	background-color-xt @
	dup >stack	xt>string		['] <page-see>	menu-entry
	background-color-xt @ ['] condition>bg-colour = IF
	    ."   "
	    s" EDIT"	redisplay	false >stack
	    ['] conditional-colouring-menu	menu-entry
	    s" Ee" menu-same-key-entry
	THEN
	2 3 screen-column
	s" Bg coloring: "  redisplay	['] spot-background-coloring >stack
	['] display-switch >stack-2	['] named-xor!		menu-entry
	spot-background-coloring? .ON-off-entry cr
	s" b" menu-same-key-entry

	12 keep-but-scroll-rest
	cr
	field-i-planes# p0 scrolled-range ?DO
	    from-here
	    i spot-var-name type
	    s" >color-scale	"	i color-scale	simple-menu-entry-value
	    cr
	LOOP

	cr
	s" Calibrate background color scales to full scale  "
	['] cal-bg-color-scales-full	redisplay	menu-entry
	s" c" menu-same-key-entry
	bl emit
	s"  half scale "
	['] cal-bg-color-scales-half	redisplay	menu-entry
	s" h" menu-same-key-entry
	bl emit
	s"  two colors "
	['] cal-bg-color-scales-two	redisplay	menu-entry cr
	s" t2" menu-same-key-entry
    THEN

    step-display-on? IF
	spot-display-on? IF cr THEN	\ separate by a blanc line

	s" Step foreground coloring: "	['] step-foreground-coloring >stack
	['] display-switch >stack-2	['] named-xor! redisplay menu-entry
	step-foreground-coloring? dup .ON-off-entry-coloured
	s" fF" menu-same-key-entry

	spot-display-on? 0= IF		\ avoid scrolling
	    cr cr
	ELSE
	    1 2 screen-column
	THEN

	s" Step background coloring: "	['] step-background-coloring >stack
	['] display-switch >stack-2	['] named-xor! redisplay menu-entry
	step-background-coloring? dup .ON-off-entry-coloured cr
	s" bB" menu-same-key-entry

	cr
	OR IF
	    spot-display-on? 0= IF	\ avoid scrolling
		s" Set the colors of text display items in menu text display."
		redisplay	['] (|menu-step-display|)	menu-entry cr
	    THEN
	THEN

    ELSE spot-display-on? 0= IF		\ neither step or spot
	." Switch either step or spot display on, then choose colors."
    THEN THEN

[ELSE]		\ compiled without color support
    cr ." This version was compiled without color support."
    cr ." Uncomment 'never-use-colors' in 'compile-options.fs' if you want colors."
[THEN]
    <common-menu-entries> ;

: color-menu ( -- )
    color-men
    ['] .color-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' color-menu function-key-actions >list

\ ****************************************************************
\ end	colors



\ ****************************************************************
\ ********************  mixed old stuff  *************************
\ ****************************************************************

INCLUDE mixed-old-stuff.fs

\ ****************************************************************
\ end	mixed old stuff



\ ****************************************************************
\ ****************  basic linear equation system  ****************
\ ****************************************************************

\ Compile only if the needed variables are defined:
nuc-organs# 1 >  spot-properties# 3 >  AND [IF]

\ Let's try a very simple equation system:

\ A - Bc = 0
\ A - d  = 0

\ I rate on the error |A - Bc| + |A -d|

\ While c and d are given and keep changing,
\ the cells should learn how to set A and B accordingly.

\ brew representation:
\ A :  organ-A		B :  organ-B		r/w nuc variables
\ c :  C-property	d :  D-property		r/o spot variables

\ That's my cheat:
s" -" GENE: cheat-basic-linear
	D-property@ dup organ-A !
	C-property@ ?/ organ-B !  ;gene

internal' cheat-basic-linear wake-me-actions >list

: basic-linear-error ( -- +n )		\ 2/ --> less overflow tricks here...
    organ-A @   organ-B @  C-property@ *  -  2/ abs
    organ-A @	D-property@ -		     2/ abs
    + ;

: score-basic-linear ( -- -score )   basic-linear-error negate ;

: eat-basic-linear ( -- )   basic-linear-error eat-scored ;
' score-basic-linear  ' eat-basic-linear  eat-actions 2>list



\ That's what brew found when I first tried:
\
\  : g-7535
\      D-property@ D-property@ C-property@ ?/ organ-B ! organ-A ! ;
\
\  Trying around I realize I was quite lucky with the parameters
\  I happened to take first ;-)


\ Some other genomes I have seen:
\  : g-59
\      organ-B @ C-property@ ?/ organ-B ! ;
\
\  : g-1352
\      organ-A @ C-property@ ?/ organ-B ! ;
\
\  this one made me smile: ;-)
\  : g-8890
\      organ-A take D-property@ organ-A ! C-property@ ?/ organ-B ! ;
\
\ this one came up over and over:
\ : g-5315
\      organ-B take organ-A ! ;


[THEN]

\ ****************************************************************
\ end	basic linear



\ ****************************************************************
\ ***************  simple linear equation system  ****************
\ ****************************************************************

\ Compile only if the needed variables are defined:
nuc-organs# 1 >  spot-properties# 2 >  AND [IF]

\ Let's test a simple linear equation system of the form
\ x + a*y = 0
\ x + b*y + c = 0

\ We rate on the added absolute miss of the two equations.

\ The cells have to find out to put the solutions x and y in
\ their first two organs.

\ Parameters a,b,c are spot properties.
\ They will be set randomly each time before the cell is activated.
\ So on each life step the conditions will change heavily and the
\ cells have to find out how to adjust x and y (inner organs A and B )
\ accordingly.


\ So we have:
\ x + a*y = 0
\ x + b*y + c = 0

\ brew sees it like this:
\ x = organ-A		y = organ-B				\ r/w nuc vars
\ a = A-property	b = B-property		c = C-property	\ r/o spot vars

\ organ-A @  A-property @ organ-B @ *  +		 "=0"
\ organ-A @  B-property @ organ-B @ *  +  C-property @ + "=0"
\ 
\ a possible solution ;-)
\ x = ac / (b - a)
\ y = c  / (a - b)
\ 
s" -" GENE: cheat-simple-linear
    A-property@ C-property@ *  B-property@ A-property@ -  ?/  organ-A !
    C-property@  A-property@ B-property@ -  ?/  organ-B ! ;gene

internal' cheat-simple-linear wake-me-actions >list

\ I use 2/  --> less overflow tricks here...
: simple-linear-error ( -- +n )
    organ-A @   organ-B @  A-property@ *  + 2/ abs
    organ-A @	organ-B @  B-property@ *  +  C-property@ +  2/ abs  + ;

: score-simple-linear ( -- -score )   simple-linear-error negate ;

: eat-simple-linear ( -- )   simple-linear-error eat-scored ;
' score-simple-linear  ' eat-simple-linear  eat-actions 2>list


INDIVIDUAL: simple-linear ( -- )
' simple-linear individuals >list

' eat-simple-linear	eat-xt !
' cell-division	reproduce-xt !
' <look-at>	show-me-xt !
div-organ-A div-organ-B or my-diversifctn-mask !
1000		reprodctn-threshold !
2		age-threshold !

[THEN]


\ ****************************************************************
\ end	simple linear equation system



\ ****************************************************************
\ ******************  linear equation system  ********************
\ ****************************************************************

\ Compile only if the needed variables are defined:
nuc-organs# 1 >  spot-properties# 3 >  AND [IF]

\ Let's test a linear equation system of the form
\ x + a*y + b = 0
\ x + c*y + d = 0

\ We rate on the added absolute miss of the two equations.

\ The cells have to find out to put the solutions x and y in
\ their first two organs.

\ For the parameters a,b,c,d spot properties get used.
\ They will be set randomly each time before the cell is activated.
\ So on each life step the conditions will change heavily and the
\ cells have to find out how to adjust x and y (inner organs A and B )
\ accordingly.

\ So we have:
\ x + a*y + b = 0
\ x + c*y + d = 0
\ brew sees it like this:
\ x = organ-A		y = organ-B		\ inner r/w nuc variables
\ a = A-property	b = B-property		\ r/o values of the spot
\ c = C-property	d = D-property
\ organ-A @   organ-B @  A-property@ *  +  B-property@ +  "= 0"
\ organ-A @   organ-B @  C-property@ *  +  D-property@ +  "= 0"

\ a possible solution ;-)
\ x = ((b - c) / (a - b)) * a - b
\ y = (c - b) / (a - b)
\ brew sees this as:
\ organ-A @ "should be equal"
\ B-property@ C-property@ -  A-property@ *   A-property@ B-property@ -  ?/   B-property@ +
\ organ-B @ "should be equal"
\ C-property@ B-property@ -  A-property@ B-property@ -  ?/
\
s" -" GENE: cheat-linear
    B-property@ C-property@ -  A-property@ *   A-property@ B-property@ -  ?/
    B-property@ -  organ-A !

    C-property@ B-property@ -  A-property@ B-property@ -  ?/
    organ-B ! ;gene

internal' cheat-linear wake-me-actions >list

: equation-1-error ( -- +n )
    organ-A @   organ-B @  A-property@ *  +   B-property@ +   abs ;

: equation-2-error ( -- +n )
    organ-A @   organ-B @  C-property@ *  +   D-property@ +  abs ;

\ I use 2/  --> no overflow tricks here...
: score-linear ( -- -score )
    equation-1-error	2/	( +error-equation-1 )
    equation-2-error	2/	( +error-equation-1 +error-equation-2 ) +
    negate ;

\ I use 2/  --> no overflow tricks here...
: eat-linear ( -- )
    equation-1-error	2/	( +error-equation-1 )
    equation-2-error	2/	( +error-equation-1 +error-equation-2 )
    + eat-scored ;
' score-linear  ' eat-linear  eat-actions 2>list


INDIVIDUAL: linear-equation-system ( -- )
' linear-equation-system individuals >list

' eat-linear	eat-xt !
' cell-division	reproduce-xt !
' <look-at>	show-me-xt !
1000		reprodctn-threshold !
2		age-threshold !

[THEN]


\ ****************************************************************
\ end	linear equation systems



\ ****************************************************************
\ ***************************  sum  ******************************
\ ****************************************************************

\ Compile only if the needed variables are defined:
nuc-organs#  0 >
nuc-parameters# 1 > AND
spot-properties# 2 >  AND [IF]

\ Let's try something very simple:
\ The cells must sum up two numbers 'A = B + C'
\ We rate on the absolute miss.

\ In the beginning of the experiment A B C are small integer numbers.
\ They drifting randomly away from zero.
\ Nucs (Cells) that happen to have a triple with a small absolute value of
\ B + C - A have an advantage.
\ They will get more energy, food or whatever, reproduce earlier and
\ increase in number.
\ The others tend to disappear.

\ But A B C are of three very different qualities:

\ A and B are part of the nuc.
\ They are herditary, and can get varied a bit during reproduction.

\ C is beyond the control of the nuc,
\ it's an variable of the virtual spot the nuc is sitting on.

\ So a child will normally have similar A and B, but very different
\ C value than the mother cell.

\ The genome starts by doing noop and gets mutatet then.
\ While the gene primitives can read all three values A B C
\ they provide write access only to the A variable.
\ Mutation has it's chance to find out that it can manipulate A
\ in a way that will be useful for the cell.

\ As C drifts away from zero it becomes more and more important
\ to find an effective algoritm how to set A.

\ Brew sees A B C as follows:
\ A is organ-A
\ B is parameter-B
\ C is (spot) C-property
\ (I kept the letters A B and C for mnemonic reasons).

\ a possible solution ;-)
\ A = B + C
s" -" GENE: cheat-sum   parameter-B@ C-property@ + organ-A ! ;gene

\ And look what's happening:

\  MUTATION:  based on ID:2117 GI:0  at spot 1492  in step 17
\  ==> new genome built:
\  C-property@ organ-A ! ;gene
\  code-cost: 400
\  MUTATION: step 17: nuc at spot 1492       mutated to child at 1413

\  MUTATION:  based on ID:11758 GI:0  at spot 340  in step 97
\  ==> new genome built:
\  parameter-B@ organ-A ! ;gene
\  code-cost: 400
\  MUTATION: step 97: nuc at spot 340        mutated to child at 339

\  MUTATION:  based on ID:108377 GI:27  at spot 1208  in step 934
\  ==> new genome built:
\  C-property@ parameter-B@ + organ-A ! ;gene 
\  code-cost: 600
\  MUTATION: step 934: nuc at spot 1208	  mutated to child at 1288

internal' cheat-sum wake-me-actions >list

: score-sum ( -- -score )
    parameter-B @ C-property @ +  organ-A @ -  abs negate ;

: eat-sum ( -- )   parameter-B @ C-property @ +  organ-A @ -  abs eat-scored ;
' score-sum  ' eat-sum  eat-actions 2>list

INDIVIDUAL: sum-up ( -- )
' sum-up individuals >list

' eat-sum	eat-xt !
' cell-division	reproduce-xt !
' <look-at>	show-me-xt !
div-organ-A div-parameter-B or my-diversifctn-mask !
1000		reprodctn-threshold !
2		age-threshold !

[THEN]


\ ****************************************************************
\ end	sum



\ ****************************************************************
\ *************************  moving  *****************************
\ ****************************************************************

\ moving
: cell-move ( i -- flag)			\ can't call it 'move'
    dup someone-here? IF			\ shouldn't happen here!
	drop false				\ :-(
    ELSE			( i )
	\ check for dying cells first, they would make troubles if moved
	will-die? IF
	    drop false				\ I don't move dying cells
	    s" cell is about to die, not moved" log-movement log
	ELSE
	    fcp @ >r				\ current cell on return stack
	    future
	    dup someone-here? IF		\ already taken?
		drop rdrop
		present
		false				\ :-(
	    ELSE
		false fcp !			\ off
		spot @		( i old-spot )	\ cautiously remembering spot
		swap >spot!	( old-spot )
		r> fcp !			\ displacing the cell

		log-mask @ IF
		    log-cat-id
		    s" : moved to spot " cat-log
		    spot @ num>string log-movement log-it
		THEN

		present
		>spot!
		true				\ :-)
	    THEN
	THEN
    THEN ;

\  : move? ( -- )
\      free-neighbour-spot? IF		( i )
\  	cell-move drop
\      THEN ;
\  ' move? wake-me-actions >list

\ also genes definitions
\ GENE: g-move?	move?	;gene s" -"	>stackdata! ##### last-gene-into-pool
\ also brew-words definitions previous

\ ****************************************************************
\ end	moving



\ ****************************************************************
\ ***********************  mutation-menu  ************************
\ ****************************************************************

MENU: mutation-men
: .mutation-menu ( -- )
    help-node" Mutation menu"		\ see 'edit-probabilities-menu'
    s" Mutation menu:" menu-title-entry

    cr
    s" How often to mutate: "
    ['] mutation-rate  simple-menu-entry-scale cr
    s" mo" menu-same-key-entry

    cr
    \ title is set in 'edit-probabilities-menu'
    s" Mutation TYPES "		redisplay
    ['] mutation-types >stack	['] edit-probabilities-menu	menu-entry cr
    s" tTyY" menu-same-key-entry

    cr
    s" Stack turning point: "
    ['] stack-turning-point   simple-menu-entry-variable cr
    s" S" menu-same-key-entry

    s" If stack depth is off so much, only genes that won't make it worse"
    same-menu-entry cr
    s" will be inserted. Influences the possible complexity of mutations."
    same-menu-entry cr

    cr
    s" Mutations threshold: "
    ['] mutations-threshold  simple-menu-entry-variable cr
    s" After so many mutations we try to come to an end," same-menu-entry cr
    s" respecting stack turning point and such." same-menu-entry cr

    cr
    s" Size limit for genomes in items: "
    ['] mutation-max-ollowed-items	simple-menu-entry-variable
    s" sl" menu-same-key-entry
    .tab
    (exceeding-size-ring) @ IF
	s" RING"
    ELSE
	s" quiet"
    THEN
    ['] (exceeding-size-ring) >stack	redisplay
    ['] toggle-named  menu-entry cr
    s" R" menu-same-key-entry

    cr
    s" Trial phase: "		['] trial-phase	 simple-menu-entry-variable cr
    s" pr" menu-same-key-entry
    s" After how many generations will genes be compiled and put into the gene pool."
    same-menu-entry cr

    cr
    s" Include produced genomes as genes: "
    ['] store-genomes >stack	['] run-mode >stack-2
    ['] named-xor! redisplay menu-entry
    store-genomes? .ON-off-entry cr
    s" iIg" menu-same-key-entry

    cr
    reset-nuc-masks? @ IF
	s" Set nuc bit masks absolutely."
    ELSE
	s" 'OR' nuc bit masks."
    THEN
    redisplay	['] reset-nuc-masks? >stack   ['] toggle-named	  menu-entry

    1 2 screen-column
    s" Diversification of variables."	redisplay
    ['] diversification-menu	menu-entry cr
    s" dDv" menu-same-key-entry

    <common-menu-entries> ;

: mutation-menu ( -- )
    mutation-men
    ['] .mutation-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' mutation-menu function-key-actions >list

\ ****************************************************************
\ end	mutation-menu



\ ****************************************************************
\ ********************  menu-edit-spot  **************************
\ ****************************************************************

create spot-before field-i-planes# cells allot
create spot-after  field-i-planes# cells allot
spot-floats# [IF]
    create spot-df-before spot-floats# dfloats allot
    create spot-df-after  spot-floats# dfloats allot
[THEN]

MENU: edit-spot-men
defer ?record-edit-spot? ( -- changed-flag )

: .menu-edit-spot
    help-node" Edit spot menu"
    s" Edit the selected spot:  # " start-title-entry
    spot @ .  end-title up-to-here

    from-here ." Showing "
   (spot-menus-show-dfloats) @ IF s" FLOAT"  ELSE s" INTEGER"  THEN type-bright
    s"  spot variables."  ['] (spot-menus-show-dfloats) >stack	redisplay
    ['] toggle-named	 menu-entry cr
    s" otifIF" menu-same-key-entry

    5 keep-but-scroll-rest

    (spot-menus-show-dfloats) @ 0= IF \ integers
	cr
\	field-i-planes# p0 scrolled-range ?DO	\ no!
	field-i-planes#  1 scrolled-range ?DO	\ not allowing pointers here!
	    from-here
	    i spot-var-name type
	    1 6 screen-column
	    s" " i n'th-spot-variable  simple-menu-entry-value cr
	LOOP
[ spot-floats# ] [IF]
    ELSE \ dfloats
	dfloat-spot-vars
	dup nodes 0 scrolled-range ?DO
	    next-node
	    dup @
	    cr from-here  dup xt>string type
	    1 5 screen-column	s" "	rot simple-dfloat-variable-entry
	LOOP
	drop
	cr
[THEN] \ spot-floats#
    THEN

    <common-menu-entries> ;

: menu-edit-spot ( -- )
    edit-spot-men
    ['] .menu-edit-spot menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default

    spot-vars@ spot-before spot-vars!

[ spot-floats# ] [IF]
    spot-df-vars@ spot-df-before spot-df-vars!
[THEN]

    do-menu-loop
    free-menus

    \ test for changes and maybe record them:
    ?record-edit-spot? IF \ changed?
	world-not-scanned
    THEN ;

\ ****************************************************************
\ end	menu-edit-spot



\ ****************************************************************
\ ***********************  world-menu  ***************************
\ ****************************************************************

MAYBE-DO-FIELD: do-on-world-field
' <food> (xt-do-it) !			\ just some defaults:
spot-qualities# [IF]
    ' A-quality (expr-xt-1) !
    spot-qualities# 1 > [IF]
	' B-quality (expr-xt-2) !
    [THEN]
[ELSE]
    spot-properties# [IF]
	' A-property (expr-xt-1) !
	spot-properties# 1 > [IF]
	    ' B-property (expr-xt-2) !
	[THEN]
    [THEN]
[THEN]
init-df-expr-xts-spot
init-df-do-xts-spot


\ Menu entry for visibility ranges of all the dimensions:
: dimensions-visibility-entry ( -- )
    this-world 0= IF  EXIT  THEN

    world-dimensions @ cells 0 DO
	cr from-here
	i cell / 1+ . ." dimension  spots: "  dimension-ranges i + @ .
	s" 	visible from: "	visibility-on i +	simple-menu-entry-value
	."  " up-to-here .tab
	s" to: "	visibility-off i +		simple-menu-entry-value
	i cell / 2 = IF
	    ."     "
	    s" backgr.off: "	backgound-off		simple-menu-entry-value
	THEN
    CELL +LOOP

    world-dimensions @ 1 > IF
	cr s" Delay displaying layers:	"
	layer-delay					simple-menu-entry-value
    THEN

    cr ;

\ Menu interface to '(big-bang)':
DEFER ?record-big-bang
DEFER ?log-big-bang
DEFER ?record-brew-changes
DEFER save-brew-before
: |big-bang| ( -- )
    ?record-brew-changes

    (time-planes) @
    0  (dimensions) @ 1- DO
	i (dim-spots) @
    -1 +LOOP
    (dimensions) @  (big-bang)
    ?record-big-bang
    ?log-big-bang
    (dimensions) off
    save-brew-before ;

: clone-geometry ( -- )
    world-dimensions @
    dup (dimensions) !
    0 DO
	dimension-ranges i cells + @  i (dim-spots) !
    LOOP ;

MENU: big-bang-men
: .big-bang-menu ( -- )
    help-node" Big Bang menu"
    s" Big bang menu:  Create a new universe and make it actual."
    menu-title-entry

    cr
    this-world IF
	from-here
	." Current world	spots: "    spots num>string	noop-entry
	dimensions-visibility-entry cr
    THEN

    cr
    from-here  <other-colour>  ." Create another world:"

    true			\ ok flag
    (dimensions) @ 1 < IF
	.tab s" Select dimensionality first." type-other-colour
	(dimensions) off
	drop false
    THEN
    (dimensions) @ max-dimensions# > IF
	bell <alert-colours>
	.tab ." No more than " max-dimensions# . ." dimensions possible." cr
	s" Set max-dimensions# if you really want more." type-other-colour cr
	max-dimensions# (dimensions) !
    THEN
    dup IF
	false			\ ranges undefined flag
	(dimensions) @ 0 DO
	    i (dim-spots) @ 0= IF drop true THEN
	LOOP
	IF
	    <other-colour> ." 	define all ranges first."
	    drop false
	THEN
    THEN
    ( ok-flag ) IF
	s" 	ok, make a Big Bang."	do-after
	redisplay	menu-done	['] |big-bang|	menu-entry
	s" cCwbB" menu-same-key-entry
    ELSE
	s" "	ping	noop-entry
	s" wbB" menu-same-key-entry
	.tab  reset-colours
	this-world IF
	    s" Clone geometry."	  redisplay	['] clone-geometry   menu-entry
	    s" cCg" menu-same-key-entry
	THEN
    THEN
    cr
    reset-colours

    cr
    s" Dimensions:	    "  ['] (dimensions)	 simple-menu-entry-variable cr
    s" dD" menu-same-key-entry

    (dimensions) @ 0 ?DO
	from-here
	i 1+ num>string type	s" . dimension range: "
	i (dim-spots)		simple-menu-entry-value cr
	i 1+ num>string menu-same-key-entry
    LOOP

    <common-menu-entries> ;

: big-bang-menu ( -- )
    worlds#
    big-bang-men
    ['] .big-bang-menu menu-display-xt !

    0 (dim-spots) max-dimensions# cells erase
    c-l 0 (dim-spots) !
    l-s 1- 1 (dim-spots) !
    (dimensions) off
    2 (time-planes) !

    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    ['] .ok-done to-do-after-xt !

    do-menu-loop
    free-menus
    worlds# = IF
	0 at-x at? page at-xy
	s" No world created. " type-other-colour
	800 wait-until
    THEN ;
' big-bang-menu function-key-actions >list

\ Enter n'th world from worlds list, recording and logging:
: |set-n'th-world| ( u -- )
    set-n'th-world
    log-user? IF
	0 log-out-line
	s" User selected "		cat-log
	world-string dup string@	0 log-it  stringbuf-close
    THEN ;

: .geometry ( -- )
    world-dimensions @ dup . .bs ." D: "
    cells 0 DO
	dimension-ranges i + @ num>string ?type s" *" ?type
    CELL +LOOP
    .bs bl emit ;		\ overwrite last '*'

\ Set n'th world name, but do not change this-world:
: n'th-world-name-2! ( addr count u -- )
    this-world >r
    (set-n'th-world)
    world-name @ string!
    r> enter-world ;

DEFER ?record-world-name ( u -- )
: input-n'th-world-name ( u -- )
    this-world >r
    dup (set-n'th-world)
    world-name2@ string!! >r
    world-name @
    accept>stringbuf
    r@ string@ world-name2@ compare IF
	recording? IF
	    dup ?record-world-name
	THEN
	log-user? IF
	    s" User changed name of world# " cat-log
	    world#		log-number
	    s"  from: "		cat-log
	    r@ string@		cat-log
	    s"  to: "		cat-log
	    world-name2@	0 log-it
	THEN
    THEN
    drop
    r> stringbuf-close
    r> enter-world ;

: ?create-a-world ( -- )
    this-world IF  EXIT  THEN

    cr cr  reset-colours		\ I call it within the menu line
    s" No world exists, please create one." type-other-colour
    2000 wait-until
    big-bang-menu ;

: |remove-world| ( -- )
    log-user? IF
	s" User removed "		cat-log
	world-string dup string@	0 log-it  stringbuf-close
    THEN
    remove-world ;

\ User interface to remove current world,
\ assuring that at least one world exists thereafter:
: user-remove-world ( u -- )
    dup world# <> IF  set-n'th-world  ELSE  drop  THEN

    bell
    page
    world-string dup string@ type cr  stringbuf-close
    cr s" Do you want to delete this world and all cells in it? " type-alert
    ."  y/n "
    key [char] y over = swap [char] Y = or
    0= IF	\ only 'y' and 'Y' accepted as 'yes'
	s"  no." type-other-colour 300 wait-until
	EXIT
    THEN

    |remove-world|
    .ok-done
    worlds# dup IF
	1- set-n'th-world EXIT
    THEN drop

    BEGIN
	page
	?create-a-world
    this-world 0= WHILE
	bell
    REPEAT ;

DEFER |save-world|
: |save-world-n| ( u -- )
    >r
    r@ (set-n'th-world) |save-world|
    r> (set-n'th-world) ;

\ After cloning a world the new universe is populated by cells
\ mirrored from the source world. Replace one by a separate clone:
: replace-by-clone ( -- )
    clone 0= ABORT" replace-by-clone: Could not clone nuc."
    dup fcp !
    cp!
    new-id id ! ;

\ Clone world and all nucs and enter cloned universe:
: (clone-world) ( -- )
    world-name @ string@ string!!
    this-world >r		( source-world-name-handle  r: source-world )
    world-time-planes @
    0 world-dimensions @ 1- DO
	dimension-ranges i cells + @
    -1 +LOOP
    world-dimensions @  (big-bang)

    s"  cloned" third cat  world-name !		(  r: source-world )

    this-world			( new-world  r: source-world )
    r> enter-world (spot-data-field)
    over enter-world (spot-data-field)
    total-list-length @ move

    ['] replace-by-clone  do-with-everybody
    drop rdrop ;

DEFER ?record-clone-world-n ( u -- )
: clone-world-n ( u -- )
    dup ?record-clone-world-n
    (set-n'th-world)
    (clone-world)
    log-user? IF
	s" User cloned world to "	cat-log
	world-string dup string@	0 log-it  stringbuf-close
    THEN ;

MENU: world-list-men
: .world-list-menu ( -- )
    help-node" World list menu"
    s" Menu world list:" menu-title-entry
    cr

    4 keep-but-scroll-rest

    this-world >r
    world#
    worlds# 0 ?DO	( current-world-index )
	i (set-n'th-world)			\ not recording

	from-here i .
	dup i = IF s" * " ELSE s"   " THEN
	i >stack   menu-done  ['] |set-n'th-world|	menu-entry
	[char] 0 i + #key-same-entry

	world-name2@	redisplay	i >stack
	['] input-n'th-world-name	menu-entry

	s" " 2dup  1 4 .screen-column-min  from-here .geometry
	redisplay	['] .sorry	menu-entry

	bl emit
	s" spots: "	['] .sorry	menu-entry	spots . up-to-here

	s" " 6 9 .screen-column-min
	s" save"  redisplay  i >stack	 ['] |save-world-n|	menu-entry
	dup i = IF  s" s" menu-same-key-entry  THEN

	s" " 7 9 .screen-column-min
	s" clone"   redisplay  i >stack	 ['] clone-world-n	menu-entry
	dup i = IF  s" c" menu-same-key-entry  THEN

	s" " 8 9 .screen-column-min
	s" remove"  redisplay  i >stack	 ['] user-remove-world	menu-entry
	dup i = IF  s" r" menu-same-key-entry  THEN

	cr
    LOOP
    drop
    r> enter-world

    this-line last-line 4 - < IF
	cr
	s" Big bang"	redisplay	['] big-bang-menu	menu-entry cr
	s" bB" menu-same-key-entry
    THEN

    <common-menu-entries> ;

: world-list-menu ( -- )
    world-list-men
    ['] .world-list-menu menu-display-xt !

    menu-done   ['] noop        menu-key-default
    menu-done   ['] noop        menu-default
    do-menu-loop
    free-menus ;
' world-list-menu function-key-actions >list
' world-list-menu F5-xt !


: |free-field| ( -- )
    free-field
    log-user? 0= IF  EXIT  THEN

    0 log-out-line
    s" User did 'free-field'." 0 log-it ;

MENU: world-men
: .world-menu ( -- )
    help-node" World menu"
    s" World menu:  " start-title-entry

    ?create-a-world  this-world 0= IF  bell EXIT  THEN

    world-name2@ type-bright title-colors
    s"   " ?type  .geometry s" sized." ?type end-title up-to-here

    cr from-here
    world-name2@ type-bright
    s" "  redisplay  world# >stack  ['] input-n'th-world-name	menu-entry
    13 80 screen-column from-here
    world# worlds# ./.
    s" "   redisplay	['] world-list-menu	menu-entry
    s" oW" menu-same-key-entry
    1 4 screen-column
    s" SELECT"	['] world-list-menu	redisplay	menu-entry
    s" S" menu-same-key-entry
    ."   "
    s" REMOVE"	world# >stack
    ['] user-remove-world	redisplay	menu-entry
    2 4 screen-column
    s" spots:    "	['] .sorry-compile-option	menu-entry
    spots . up-to-here
    3 4 screen-column
    s" living:   "	['] .sorry	menu-entry	living ?   up-to-here

    cr
    s" on trial: "
    trial @ IF
	redisplay	menu-wait	['] show-on-trial
    ELSE
	['] .sorry
    THEN	menu-entry
    13 80 screen-column trial ?    up-to-here

    1 4 screen-column	s" selected: "
    selected @ IF
	redisplay	['] show-selected
    ELSE
	['] .sorry
    THEN	menu-entry	selected ? up-to-here

    2 4 screen-column
    s" compiled: "	['] .sorry	menu-entry	compiled-genes ?
    up-to-here
    3 4 screen-column
    s" max items: "	['] .sorry	menu-entry	(mutated-max) ?
    cr

    cr
    s" Compiled with the following world structure:"
    ['] .sorry-compile-option				menu-entry
    ." 	integer/dfloats" up-to-here
    cr
    s" qualities: "	['] .sorry-compile-option	menu-entry
    spot-qualities# spot-f-qualities# ./. up-to-here
    1 4 screen-column
    s" properties: "	['] .sorry-compile-option	menu-entry
    spot-properties# spot-f-properties# ./. up-to-here
    2 4 screen-column
    s" secrets:  "	['] .sorry-compile-option	menu-entry
    spot-secrets# spot-f-secrets# ./. up-to-here
    cr

    dimensions-visibility-entry

    cr
    s" Choose what to do on each spot (before eventually waking a nuc) :"
    menu-title!
    s" s"		s" spot do "
    ['] spot-do-actions	['] spot-do-xt		choose-xt-entry-ext cr

    s" Choose what to do before waking up a nuc." menu-title!
    s" bB"		s" nuc do before"
    ['] cell-do-actions	['] cell-do-before-xt	choose-xt-entry-ext

    1 2 screen-column
    s" Choose what to do before each step." menu-title!
    s" bB"		s" step do before"
    ['] step-do-actions	['] step-do-before-xt	choose-xt-entry-ext cr

    s" Choose what to do after a nuc has done his thing." menu-title!
    s" aA"		s" nuc do after"
    ['] cell-do-actions	['] cell-do-after-xt	choose-xt-entry-ext

    1 2 screen-column
    s" Choose what to do after each step." menu-title!
    s" bB"		s" step do after"
    ['] step-do-actions	['] step-do-after-xt	choose-xt-entry-ext cr

    cr
    s" Scan world"		['] spot-scan-menu		menu-entry
    s" w"	menu-same-key-entry
    1	\ position
    living @ IF
	dup 4 screen-column  1+
	s" Scan nucs"		['] nuc-scan-menu		menu-entry
	s" n"	menu-same-key-entry
    THEN

    dup ( position ) 4 screen-column	1+
    s" Delete all"	redisplay	do-after	do-after-2
    ['] |free-field|	menu-entry
    s" x" menu-same-key-entry

    dup ( position ) 4 screen-column	1+
    s" Big bang"	redisplay	do-after
    ['] big-bang-menu	menu-entry
    s" B" menu-same-key-entry
    drop
    cr

    cr
    do-on-world-field maybe-fix-condition
    check-ok-for-spot-maybe? dup IF
	s" DO something on some spots: "	redisplay	do-after
	['] do-on-world-field xt>stack		do-after-2
	['] |maybe-do-everywhere-generic|  menu-entry
	1 = IF
	    s" YOU DO THAT AT YOUR OWN RISK..."
	ELSE
	    s" Please use carefully..."
	THEN
	type-bright up-to-here
    ELSE
	drop
	from-here s" Setup condition and action first." type-other-colour
    THEN
    cr

    maybe-do-type-entry
    spot-related-selections conditional-expression-entries cr

    from-here ." What to do:          "
    spot-related-selections do-what-entry cr

    (maybe-do-type-xt) @ ['] maybe-do-simple =
    (simple-expression-xt) @ ['] false = and invert IF
	cr
	s" Scan conditionally "	 redisplay	do-after
	['] do-on-world-field >stack	['] scan-some-spots	menu-entry

	1 4 screen-column
	s" Show coloured "	redisplay	['] do-on-world-field xt>stack
	['] |show-bg-coloured-on-hit|				menu-entry

	coloured-on-range-possible? IF
	    2 4 screen-column
	    s" Coloured on range "  redisplay	['] do-on-world-field xt>stack
	    ['] |show-bg-coloured-on-range|			menu-entry
	    s" r" menu-same-key-entry
	THEN
    THEN

    <common-menu-entries> ;

: world-menu ( -- )
    page
    ?create-a-world  this-world 0= IF  bell EXIT  THEN

    do-spot-scan

    world-men
    ['] .world-menu menu-display-xt !
    ['] do-spot-scan	to-do-after-xt !
    ['] .ok-done to-do-after-2-xt !

    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' world-menu function-key-actions >list


\ ****************************************************************
\ end	world-menu


\ ****************************************************************
\ ************************  nuc-menu  ****************************
\ ****************************************************************

VARIABLE (nuc-spot-known)	(nuc-spot-known) off \ also removed flag
defer ?record-remove-cell
: |remove-cell| ( -- )
    bell
    page cr
    cr s" Do you want this cell to be taken out of the play? " type-alert
    ." (k) y/n "
    <other-colour>
    cr cr
    key [char] y over = swap [char] k = or IF	\ 'y' and 'k' accepted as 'yes'
	log-mask @ IF
	    log-cat-id
	    s" : removed by user" log-death log-it
	THEN
	?record-remove-cell
	die
	1 (nuc-spot-known) !	\ magic flag for removed cell
	." cell removed."
	nucs-not-scanned
	(manually-selected-cell) off
    ELSE ." not removed" THEN
    reset-colours
    cr 1500 ms ;

MENU: nuc-men
nuc-floats# [IF]
: nuc-local-f-div-entries ( -- )
    s" Diversify"	['] diversification-menu	menu-entry
    s" d" menu-same-key-entry

    ['] f-organ-div-mask nuc-f-organs#
    s"  f-organs: " items-bitmask-entry

    ['] f-param-div-mask nuc-f-parameters#
    s"  f-parameters: " items-bitmask-entry

    ['] f-invisibl-div-mask nuc-f-invisibles#
    s"  f-invisibles: " items-bitmask-entry ;
[THEN]

: nuc-local-i-div-entries ( -- )
    s" Diversify (nuc local mask):"
    ['] diversification-menu	menu-entry cr
    s" d" menu-same-key-entry
    ['] my-diversifctn-mask integer-nuc-item-flags-entry ;

DEFER gene-edit-menu
\ VARIABLE (nuc-menus-show-dfloats)	\ used by nuc-menu and .menu-nuc-scan
VARIABLE (nuc-menu-visible-floats)	(nuc-menu-visible-floats) on
: .nuc-menu ( -- )
    help-node" Nuc menu"
    nuc-menu-id menu-id !
    s" Nucleus menu:" menu-title-entry

    cp@ IF					\ cp seems valid
	on-trial? 0= IF
	    wake-me-internal @ setup-wake-me \ so cost is always up to date
	THEN

	cr
	on-trial? IF				\ trial phase: genom string
	    s" wake-me: does a recently mutated gene which is on trial."
	    ['] gene-edit-menu		redisplay	menu-entry
	    s" gwl." menu-same-key-entry
	ELSE
	    s" Choose genome of the cell.  (These genes can get mutated)."
	    menu-title!
	    ['] wake-me-actions	>stack	['] wake-me-internal >stack-2
	    s" wake-me   does: "	['] choose-xt-to-var	menu-entry
	    s" w" menu-same-key-entry
	    1 4 screen-column
	    wake-me-xt @ xt>string	redisplay
	    ['] gene-edit-menu				menu-entry
	    s" lg" menu-same-key-entry
	    2 4 screen-column
	    wake-me-internal @ xt>stack
	    menu-wait	redisplay	['] .gene-info	name-menu-entry
	    s" i." menu-same-key-entry
	THEN
	(nuc-spot-known) @ IF
	    3 4 screen-column
	    s" SPOT" redisplay	['] menu-edit-spot	menu-entry
	THEN
	cr

	s" Choose the way the cells get rated and rewarded.  Eat action:"
	menu-title!
	['] eat-actions	>stack	['] eat-xt >stack-2
	s" eat       does: "	['] choose-xt-to-var	menu-entry
	s" E" menu-same-key-entry
	1 4 screen-column
	eat-xt @	menu-wait	redisplay
	dup >stack	xt>string	['] <page-see>		menu-entry

	2 4 screen-column
	scoring-xt @ IF		\ condition should not be necessary #########
	    s" score : "  ping	score	['] .sorry	menu-entry-value
	ELSE
	    s" score : unknown"				noop-entry
	THEN

	(nuc-spot-known) @ IF
	    3 4 screen-column
	    s" remove cell" menu-done ['] |remove-cell|	menu-entry
	    \ s" kr" menu-same-key-entry
	THEN

	cr
	s" Choose how to reproduce:" menu-title!
	['] reproduce-actions >stack	['] reproduce-xt >stack-2
	s" reproduce does: "	['] choose-xt-to-var	menu-entry
	s" R" menu-same-key-entry
	1 4 screen-column
	reproduce-xt @	menu-wait	redisplay
	dup >stack	xt>string	['] <page-see>		menu-entry
	2 4 screen-column
	s" sow some"
	menu-done		['] sow-some-clones		menu-entry
	3 4 screen-column
	s" sow "
	menu-done		['] sow-some-diversified	menu-entry
	s" diversified"		['] diversification-menu	menu-entry
	s" d" menu-same-key-entry

	cr
	s" How does the cell show up in spot display?" menu-title!
	['] show-me-actions >stack	['] show-me-xt >stack-2
	s" show-me   does: "	['] choose-xt-to-var	menu-entry
	s" s" menu-same-key-entry
	1 4 screen-column
	show-me-xt @
	menu-wait	redisplay
	dup >stack	xt>string	['] <page-see>	menu-entry
	3 4 screen-column
	selected? IF s" SELECTED" ELSE s" select" THEN
	redisplay	['] toggle-selection	menu-entry cr
	s" tTS" menu-same-key-entry

	cr
	s" nuc id: "	ping	['] id		simple-menu-entry-variable
	1 4 screen-column
	s" genome id : " ping	['] genome-id	simple-menu-entry-variable
	2 4 screen-column
	s" code cost: " code-cost @ ['] .sorry ping menu-entry-value
	3 4 screen-column
	s" nuc length: "  length @  ['] .sorry ping menu-entry-value cr

	s" age   : "		['] age		simple-menu-entry-variable
	1 4 screen-column
	s" age limit : "	['] age-threshold  simple-menu-entry-variable
	s" a" menu-same-key-entry
	2 4 screen-column
	s" generation: " ping	['] generation	simple-menu-entry-variable
	3 4 screen-column
	s" genome gen: " ping
	['] genome-generation			simple-menu-entry-variable cr

	s" energy: "		['] energy	simple-menu-entry-variable
	s" e" menu-same-key-entry
	1 4 screen-column
	appearance >stack   redisplay   s" char      : "   appearance @
	['] select-char-to-addr menu-entry-value
	bl emit menu-highlite-on appearance @ .ascii up-to-here
	>last-xy
	s" c" menu-same-key-entry
	2 4 screen-column
	s" reproduction energy: "
	['] reprodctn-threshold		simple-menu-entry-variable cr
	s" r" menu-same-key-entry

	cr
[ nuc-floats# ] [IF]
	from-here
	(nuc-menus-show-dfloats) @ IF
	    s" FLOATING POINT"
	ELSE
	    s" INTEGER"
	THEN
	type-bright
	s"  nuc variables  "
	['] (nuc-menus-show-dfloats) >stack	redisplay
	['] toggle-named  menu-entry
	s" oIFt" menu-same-key-entry

	(nuc-menus-show-dfloats) @ IF 
	    from-here
	    (nuc-menu-visible-floats) @ IF
		s" VISIBLE" type-bright
		s"  to the nuc through mutation."
	    ELSE
		s" HIDDEN" type-bright
		s"  from mutation.
	    THEN
	    ['] (nuc-menu-visible-floats) >stack	redisplay
	    ['] toggle-named  menu-entry
	    s" vVhH" menu-same-key-entry
	THEN
	cr
[ELSE]
	s" Integer nuc variables (no floats): "	noop-entry cr
	(nuc-menus-show-dfloats) off
[THEN]
	20 keep-but-scroll-rest
	at?
	(nuc-menus-show-dfloats) @ 0= IF \ display integers
	    [ nuc-organs# nuc-parameters# max nuc-invisibles# max nuc-secrets#
	    max ] literal nuc-organs + nuc-organs scrolled-range ?DO
		i organ? IF
		    from-here  i nuc-var-name type
		    s" : "  i nuc-var-xt	simple-menu-entry-variable
		    [char] A i nuc-organs - + #key-same-entry
		THEN
		i nuc-organs# +
		dup nuc-parameter? IF
		    >r
		    1 4 screen-column
		    from-here  r@ nuc-var-name type
		    s" : "  r> nuc-addr		simple-menu-entry-value
		ELSE drop THEN
		i [ nuc-organs# nuc-parameters# + ] literal +
		dup nuc-invisible? IF
		    >r
		    2 4 screen-column
		    from-here  r@ nuc-var-name type
		    s" : "  r> nuc-addr	simple-menu-entry-value
		ELSE drop THEN
		i [ nuc-organs# nuc-parameters# + nuc-invisibles# + ] literal +
		dup secret? IF
		    >r
		    3 4 screen-column
		    from-here  r@ nuc-var-name type
		    s" : "  r> nuc-addr	simple-menu-entry-value
		ELSE drop THEN
		cr
	    LOOP
	    cr
	    nuc-local-i-div-entries cr
[ nuc-floats# ] [IF]
	ELSE \ display floats

	    (nuc-menu-visible-floats) @ IF
		nuc-f-organs# nuc-f-parameters# max
	    ELSE
		nuc-f-invisibles# nuc-f-secrets# max
	    THEN
	    0 scrolled-range ?DO
		(nuc-menu-visible-floats) @ IF
		    i f-organ-xt? IF
			from-here dup xt>string type
			s" :	" rot simple-dfloat-variable-entry
			[char] A i + #key-same-entry
		    THEN
		    i f-parameter-xt? IF
			1 2 screen-column
			from-here dup xt>string type
			s" :	" rot simple-dfloat-variable-entry
			[char] A i + #key-same-entry
		    THEN
		ELSE
		    i f-invisible-xt? IF
			from-here dup xt>string type
			s" :	" rot simple-dfloat-variable-entry
			[char] A i + #key-same-entry
		    THEN
		    i f-secret-xt? IF
			1 2 screen-column
			from-here dup xt>string type
			s" :	" rot simple-dfloat-variable-entry
			[char] A i + #key-same-entry
		    THEN
		THEN
		cr
	    LOOP
	    nuc-local-f-div-entries cr
[THEN] \ nuc-floats#
	THEN

	at-xy
    ELSE					\ cp not valid
	cr
	." no living beeing here..."
	cr
    THEN

    <common-menu-entries> ;

: nuc-menu ( -- )
    nuc-men
    ['] .nuc-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    cp@ IF wake-me-internal @ THEN		\ remember old genome

    do-menu-loop
    free-menus
    cp@ IF
	wake-me-internal @ <> IF		\ has genome changed?
	    new-genome-id genome-id !		\ yes: new-genome-id
	THEN
    THEN
    guess-scoring-function	\ hack
    nucs-not-scanned ;

\ ****************************************************************
\ end	nuc-menu



\ ****************************************************************
\ *********************  actual-pool-menu  ***********************
\ ****************************************************************

: actual-pool-menu ( -- )
     help-node" Actual pool menu"		\ see 'edit-probabilities-menu'
     actual-genepool-xt @ edit-probabilities-menu ;

' actual-pool-menu function-key-actions >list

\ ****************************************************************
\ end	actual-pool-menu



\ ****************************************************************
\ *********************  gene-pool-menu  *************************
\ ****************************************************************

MENU: gene-pool-men
: .gene-pool-menu ( -- )
    help-node" Gene pools menu"
    s" Gene pools menu:" menu-title-entry

    cr
    s" Select actual gene pool:" menu-title!
    ['] gene-pools >stack	['] actual-genepool-xt >stack-2
    s" Actual gene pool: "	['] choose-xt-to-var	menu-entry
    s" a" menu-same-key-entry
    actual-genepool-xt @ xt>string ['] actual-pool-menu	menu-entry cr

    cr
    s" Edit " ['] actual-pool-menu   menu-entry
    actual-genepool-xt @ xt>string .menu-expansion cr
    s" eE" menu-same-key-entry

    <common-menu-entries> ;

: gene-pool-menu ( -- )
    gene-pool-men
    ['] .gene-pool-menu menu-display-xt !

    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' gene-pool-menu function-key-actions >list

\ ****************************************************************
\ end	gene-pool-menu



\ ****************************************************************
\ ********************  individuals-menu  ************************
\ ****************************************************************

\ Pseudo individual taking the one on spot:
: individual-on-spot ( -- )
    (manually-selected-cell) @ dup IF  cp! EXIT  THEN

    drop true ABORT" individual-on-spot: Cell undefined." ;
\ This one does not go to the individuals list...

MENU: individuals-men
VARIABLE selected-individual-xt		' (none) selected-individual-xt !
: .individuals-menu ( -- )
    help-node" Individuals menu"
    selected-individual-xt ?perform	\ makes the selected one the actual one
    s" Individuals menu:" menu-title-entry

    cr
    s" Select an individual:" menu-title!
    ['] individuals >stack	['] selected-individual-xt >stack-2
    s" Selected Individual: "	['] choose-xt-to-var	menu-entry
    s" sSiI" menu-same-key-entry

    .tab selected-individual-xt @ xt>string
    selected-individual-xt @ ['] (none) = IF
	type cr
	cr
	<other-colour>
	s" Press 'i' to select an individual."	same-menu-entry
	reset-colours
	cr
    ELSE
	['] nuc-menu	redisplay		menu-entry cr

	s" See and edit it."	redisplay	['] nuc-menu	menu-entry cr
	s" sSe" menu-same-key-entry

	cr
	s" Sow some clones"			menu-done
	['] sow-some-clones			do-after	menu-entry cr
	s" c" menu-same-key-entry
	s" Sow some diversified copies"		menu-done
	['] sow-some-diversified		do-after	menu-entry cr
	s" d" menu-same-key-entry

	s" (Diversification is done like specified in the diversification menu)"
	['] diversification-menu			menu-entry cr
	s" D" menu-same-key-entry

	cr
	<other-colour>
	s" Now you can set this individual by pressing <RETURN> in the main screen."
	menu-done	['] noop			menu-entry cr
	reset-colours
    THEN

    <common-menu-entries> ;

: individuals-menu ( -- )
    individuals-men
    ['] .individuals-menu menu-display-xt !
    ['] .ok-done to-do-after-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default

    selected-individual-xt @  ['] individual-on-spot = IF
	(manually-selected-cell) @ 0= IF
	    ['] (none) selected-individual-xt !
	THEN
    THEN

    do-menu-loop
    free-menus

    ['] (none) selected-individual-xt @ <> IF
	selected-individual-xt @ EXECUTE
	cp@ (manually-selected-cell) !
	guess-scoring-function			\ hack
    THEN ;
' individuals-menu function-key-actions >list

\ ****************************************************************
\ end	individuals-menu



\ ****************************************************************
\ ********************  function-key-menu  ***********************
\ ****************************************************************

VARIABLE (brew-show-or-go)	(brew-show-or-go) off
: brew-show-or-go ( -- )	(brew-show-or-go) on ;

: toggle-display-&-go ( -- )
    menu-id @ main-sceen-id <> IF  bell EXIT  THEN

    page	\ safety net
    toggle-display-type menu-redisplay on ;
' toggle-display-&-go function-key-actions >list
' toggle-display-&-go F2-xt !

' do-FORTH function-key-actions >list
' do-FORTH F9-xt !

VARIABLE anything		anything off
: toggle-anything ( -- )	-1 anything xor! ;	\ switch something
' toggle-anything function-key-actions >list
' toggle-anything F11-xt !

' |goodbye| F12-xt !

MENU: function-key-men
: sfk ( -- )    s" Select action for this function key" menu-title! ;
\ braindead cut&paste code ;-)
: .function-key-menu ( -- )
    help-node" Menu function keys"
    s" Select actions of the function keys:" menu-title-entry

    cr
    sfk
    from-here ." F1   does: "
    ['] function-key-actions  ['] F1-xt  choose-xt-to-var-entry
    s" 1" menu-same-key-entry
    1 2 screen-column
    sfk
    from-here ." shift-F1  does: "
    ['] function-key-actions  ['] shift-F1-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F2   does: "
    ['] function-key-actions  ['] F2-xt  choose-xt-to-var-entry
    s" 2" menu-same-key-entry
    1 2 screen-column
    sfk
    from-here ." shift-F2  does: "
    ['] function-key-actions  ['] shift-F2-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F3   does: "
    ['] function-key-actions  ['] F3-xt  choose-xt-to-var-entry
    s" 3" menu-same-key-entry
    1 2 screen-column
    sfk
    from-here ." shift-F3  does: "
    ['] function-key-actions  ['] shift-F3-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F4   does: "
    ['] function-key-actions  ['] F4-xt  choose-xt-to-var-entry
    s" 4" menu-same-key-entry
    1 2 screen-column
    sfk
    from-here ." shift-F4  does: "
    ['] function-key-actions  ['] shift-F4-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F5   does: "
    ['] function-key-actions  ['] F5-xt  choose-xt-to-var-entry
    s" 5" menu-same-key-entry
    1 2 screen-column
    sfk
    from-here ." shift-F5  does: "
    ['] function-key-actions  ['] shift-F5-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F6   does: "
    ['] function-key-actions  ['] F6-xt  choose-xt-to-var-entry
    s" 6" menu-same-key-entry
    1 2 screen-column
    sfk
    from-here ." shift-F6  does: "
    ['] function-key-actions  ['] shift-F6-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F7   does: "
    ['] function-key-actions  ['] F7-xt  choose-xt-to-var-entry
    s" 7" menu-same-key-entry
    1 2 screen-column
    sfk
    from-here ." shift-F7  does: "
    ['] function-key-actions  ['] shift-F7-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F8   does: "
    ['] function-key-actions  ['] F8-xt  choose-xt-to-var-entry
    s" 8" menu-same-key-entry
    1 2 screen-column
    sfk
    from-here ." shift-F8  does: "
    ['] function-key-actions  ['] shift-F8-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F9   does: "
    ['] function-key-actions  ['] F9-xt  choose-xt-to-var-entry
    s" 9" menu-same-key-entry

    cr
    sfk
    from-here ." F10  does: "
    ['] function-key-actions  ['] F10-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F11  does: "
    ['] function-key-actions  ['] F11-xt  choose-xt-to-var-entry

    cr
    sfk
    from-here ." F12  does: "
    ['] function-key-actions  ['] F12-xt  choose-xt-to-var-entry
    cr

    ['] redisplay default-function-keys

    <common-menu-entries> ;

: function-key-menu ( -- )
    function-key-men
    ['] .function-key-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' function-key-menu function-key-actions >list

\ ****************************************************************
\ end	function-key-menu



\ ****************************************************************
\ **********************  display-menu  **************************
\ ****************************************************************

VARIABLE snapshot-frequency	127 snapshot-frequency !    \ Must be (2^n - 1)

: cycle-snapshot-type ( -- )	\ (when ordinary display is off)
    display-switch dup @
    step-snapshots? IF
	step-snapshots xor
	spot-snapshots or
    ELSE
	spot-snapshots? IF
	    spot-snapshots xor
	ELSE
	    step-snapshots xor
	THEN
    THEN
    swap ! ;

\ Switch between real time display and snapshots:
\ (see: 'toggle-display-type' to switch real time spot/step display).
: (switch-display-type) ( -- redisplay-flag )
    false >r			( r: redisplay-flag )
    display-switch dup @	( display-switch bits  r: redisplay-flag)

    \ make sure that not both types are on:
    [ spot-display-on step-display-on or ] literal >r
    dup r@ and r> = IF					\ exception: both on
	(prior-display-type) @ CASE
	    spot-display-on OF  step-display-on  ENDOF
	    step-display-on OF  spot-display-on  ENDOF
	ENDCASE
	invert and
    ELSE						\ normal case
	dup [ step-display-on spot-display-on or ] literal and dup IF
	    dup (prior-display-type) !			\ show snapshots
	    invert and

	    rdrop  TRUE >r	( display-switch bits  r: redisplay-flag=TRUE )
	ELSE drop
	    (prior-display-type) @ or			\ real time display
	THEN
    THEN

    swap !

    step-display-on? step-snapshots? or IF
	?reset-continuous-column
	?step-display-sanity
    THEN

    r> ;

\ Switch between real time and snapshot display, no redisplay:
: |switch-display-type|   (switch-display-type) drop ;

MENU: display-men
: choose-integer-nuc-var-entry ( -- )
    ['] integer-nuc-vars >stack	['] show-int-nuc-var-xt >stack-2
    show-int-nuc-var-xt @ xt>string	['] choose-xt-to-var	redisplay
    menu-entry ;

: sign-tolerance-entry ( -- )
    ."   " s" ± "	['] show-sign-tolerance   simple-menu-entry-variable
    s" ±" menu-same-key-entry ;

: 2-ascii-scale-entry ( -- )
    ."   "	s" / "	['] 2-ascii-scale	simple-menu-entry-variable
    s" /" menu-same-key-entry ;

nuc-floats# [IF]
: choose-dfloat-nuc-var-entry ( -- )
    ['] dfloat-nuc-vars >stack	['] show-float-nuc-var-xt >stack-2
    show-float-nuc-var-xt @ xt>string	['] choose-xt-to-var	redisplay
    menu-entry ;
[THEN]

: .display-menu ( -- )
    help-node" Display menu"
    s" Display menu:" menu-title-entry

    cr
    from-here
    step-display-on? IF
	s" STEP" type-bright ."  OR"
    ELSE
	." step OR"
    THEN
    spot-display-on? IF
	s"  SPOT" type-bright
    ELSE
	."  spot"
    THEN
    s"  "	redisplay	['] step-OR-spot	menu-entry
    s" oO" menu-same-key-entry
    [ step-display-on spot-display-on or ] literal dup display-switch @ and =
    IF
	s"  SETTING STEP *AND* SPOT DISPLAY IS NOT RECOMMENDED..."
	type-other-colour up-to-here
    ELSE

	1 4 screen-column
	s" step-snapshots"   redisplay	['] step-snapshot-on	menu-entry
	2 4 screen-column
	s" spot-snapshots"   redisplay	['] spot-snapshot-on	menu-entry
	3 4 screen-column
	s" off"		     redisplay	['] display-off		menu-entry
    THEN cr

    display-switch @
    [ step-display-on spot-display-on or ] literal and 0= IF
	step-snapshots? IF
	    s" STEP SNAPSHOTS"
	    true >r
	ELSE
	    spot-snapshots? IF
		s" SPOT SNAPSHOTS"
		true >r
	    ELSE
		s" DISPLAY OFF, no snapshots."
		false >r
	    THEN
	THEN
	['] cycle-snapshot-type		redisplay	menu-entry
	r> IF
	    1 4 screen-column
	    [ decimal ] 2
	    12 0 DO
		from-here
		dup 1- snapshot-frequency @ = >r
		r@ IF [char] * emit THEN
		dup num>string type
		r> IF [char] * emit THEN
		s" "	redisplay	third 1- >stack
		['] snapshot-frequency	>stack-2  ['] n-named!	menu-entry
		."   "
		2*
	    LOOP
	    drop
	THEN
	cr
    THEN

    cr
    s" Step display "	redisplay	['] toggle-step-display	menu-entry
    s" e" menu-same-key-entry
    step-display-on? dup .ON-off-entry-coloured
    IF
	s" displaying "  ['] step-display-items  simple-menu-entry-variable
	s"  items." .menu-expansion cr

	s" Edit step display" redisplay ['] menu-step-display menu-entry
	s" e" menu-same-key-entry

	step-display-undefined IF
	    s"   Please define undefined step display items." type-alert
	    up-to-here
	THEN
	
\  	s" Zoom steadiness, horizontal: "
\  	['] horizontal-zoom-scale	simple-menu-entry-scale
\  	s"   vertical: "  ['] vertical-zoom-scale	simple-menu-entry-scale
    THEN cr

    cr
    s" Spot display "	['] spot-display-on >stack	redisplay
    ['] display-switch >stack-2		['] named-xor!	menu-entry
    s" p" menu-same-key-entry
    spot-display-on? dup .ON-off-entry-coloured cr
    IF
	s" What does 'look up' show?" menu-title!
	['] show-me-actions >stack	['] look-at-xt >stack-2
	s" <look-at> function: "	['] choose-xt-to-var
	redisplay						menu-entry
	s" l<" menu-same-key-entry
	1 4 screen-column
	menu-wait	redisplay	look-at-xt @	dup >stack
	xt>string	['] <page-see>				menu-entry

	world-mode? IF
	    look-at-xt @ CASE
		['] show-integer-nuc-var OF
		    s" Which integer nuc var does 'look up' show?" menu-title!
		    ."   " choose-integer-nuc-var-entry
		    2-ascii-scale-entry
		ENDOF
		['] show-integer-var-sign OF
		    s" Sign of which integer nuc var does 'look up' show?"
		    menu-title!
		    ."   " choose-integer-nuc-var-entry
		    sign-tolerance-entry
		ENDOF
		['] show-energy	OF 2-ascii-scale-entry ENDOF
		['] show-A	OF 2-ascii-scale-entry ENDOF
		['] show-sign-A	OF sign-tolerance-entry ENDOF

[ nuc-floats# ] [IF]
		['] show-float-nuc-var OF
		    s" Which float nuc var does 'look up' show?" menu-title!
		    ."    " choose-dfloat-nuc-var-entry .tab
		    s" scale: "
		    ['] f-2-ascii-scale simple-dfloat-variable-entry
		ENDOF
		['] show-float-var-sign OF
		    s" Sign of which float nuc var does 'look up' show?"
		    menu-title!	    ."    " choose-dfloat-nuc-var-entry .tab
		    s" tolerance: "
		    ['] float-show-sign-tolerance simple-dfloat-variable-entry
		ENDOF
[THEN]

	    ENDCASE
	THEN
	cr

	
	look-at-xt @ ['] <look-at> = IF		\ uups!
	    s" ==> No, don't set the function to itself, thats nonsense!"
	    type-alert bell cr
	    ['] show-generation look-at-xt !
	THEN

	s" Color menu "	['] color-menu	menu-done do-after	menu-entry
	s" cC" menu-same-key-entry
	cr
    THEN

    spot-display-on? IF
	dimensions-visibility-entry
    THEN

    display-slots @	dup >r		\ assure range for display-slots
    0 max	max-display-slots# min
    dup display-slots !
    r> <> IF bell cr ." Slot number was out of range, corrected." THEN

    cr
    s" The last line can display different infos.  slots: "
    ['] display-slots  simple-menu-entry-variable cr
    s" nsi" menu-same-key-entry

    display-slots @ 6 > IF
	." (caution, too many slots might mess up your display)"
	up-to-here cr
\	['] display-slots >stack redisplay
\	['] change-named-variable menu-entry cr
    THEN

    display-slots @ 0 ?DO
	['] slot-display-words >stack
	display-slots	i 1+ cells +	>stack-2
	s" Slot does: "	redisplay	['] choose-xt-menu	menu-entry
	s" What to display in this slot?" menu-title!
	i [char] 1 + pad c!  pad 1 menu-same-key-entry
	.tab i 1+ cells display-slots + @
	menu-wait	redisplay
	dup >stack	xt>string		['] <page-see>	menu-entry cr
    LOOP

    [char] t	redisplay	['] |switch-display-type|	#key-menu-entry

    <common-menu-entries> ;

: display-menu ( -- )
    display-men
    ['] .display-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus

    \ increasing step-display-items might leave undefined ones
    step-display-undefined IF	\ no undefined step display items allowed
	menu-step-display	\ insists on defining or removing them
    THEN
;
' display-menu function-key-actions >list

\ ****************************************************************
\ end	display-menu



\ ****************************************************************
\ **********************  code-file-menu  ************************
\ ****************************************************************

\ only use inside menus.
: .code-file-ON-off ( mask -- )   code-file-mask @ and .ON-off-entry ;

MENU: code-file-men
: .code-file-menu ( -- )
    help-node" Code file menu"
    s" Code file menu:" menu-title-entry

    cr
    s" Writing a human readable code file to the hd "	redisplay
    ['] write-code-file >stack	['] code-file-mask >stack-2
    ['] named-xor!  menu-entry
    write-code-file .code-file-ON-off cr
    s" cwWhofa" menu-same-key-entry
    write-code-file  code-file-mask @  AND IF
	cr
	s" Code file: "	(code-file-id) >stack	(code-file-name) >stack-2
	redisplay	['] change-handled-file		menu-entry >last-xy
	(code-file-name) string@ .menu-expansion cr
	s" cCf" menu-same-key-entry

	cr
	s" file on trial SUCCESS "	['] file-end-trial >stack  redisplay
	['] code-file-mask >stack-2	['] named-xor! menu-entry
	.tab 		file-end-trial	.code-file-ON-off cr
	s" tsS"	menu-same-key-entry
	s" file ALL MUTATIONs "		['] file-mutating >stack   redisplay
	['] code-file-mask >stack-2	['] named-xor! menu-entry
	.tab 		file-mutating	.code-file-ON-off cr
	s" aAmM"	menu-same-key-entry
	cr

	[ file-mutating file-end-trial or ] literal code-file-mask @ and IF
	    cr
	    ." What to put in the code file: "		redisplay
	    .tab s" ALL   "  ['] code-file-mask >stack	['] named-on
	    menu-entry
	    s" aA"	menu-same-key-entry
	    s" nothing   "
	    ['] code-file-mask >stack	['] named-off	redisplay menu-entry
	    s" n"	menu-same-key-entry
	    s" CODE & stack   "	redisplay
	    [ write-code-file file-end-trial file-code file-stack or or or ]
	    literal >stack
	    ['] code-file-mask >stack-2   ['] n-named!	 menu-entry
	    s" cC"	menu-same-key-entry
	    s" STRUCTURE only"	redisplay
	    code-file-mask @ [ file-mutating file-end-trial or ] literal and
	    [ write-code-file file-structure or ] literal or >stack
	    ['] code-file-mask >stack-2   ['] n-named!	 menu-entry cr
	THEN

	[ file-mutating file-end-trial or ] literal code-file-mask @ and IF
	    s" code: "	['] file-code >stack	redisplay
	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
	    .tab .tab	file-code .code-file-ON-off cr
	    \ s" c"	menu-same-key-entry

	    s" stack: "	['] file-stack >stack	redisplay
	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
	    .tab .tab	file-stack .code-file-ON-off cr 
	    \ s" s"		menu-same-key-entry

\ 	    s" scoring: "	['] file-scoring >stack		redisplay
\ 	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
\ 	    .tab	file-scoring .code-file-ON-off cr 
\ 	    \ s" s"		menu-same-key-entry

	    s" mutation-type: "	['] file-mutation-type >stack	redisplay
	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
	    .tab	file-mutation-type .code-file-ON-off cr 
	    \ s" t"		menu-same-key-entry

	    s" structures: "	['] file-structure >stack	redisplay
	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
	    .tab		file-structure .code-file-ON-off cr 
	    \ s" S"		menu-same-key-entry
	THEN

	file-mutating code-file-mask @ and IF
	    s" frames: "	['] file-frames >stack		redisplay
	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
	    .tab		file-frames .code-file-ON-off cr 
	    s" f"		menu-same-key-entry
	    s" depth&cost: "	['] file-depth&cost >stack	redisplay
	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
	    .tab		file-depth&cost	.code-file-ON-off cr 
	    s" d"		menu-same-key-entry
	    s" step,spot,id: "	['] file-step&spot&id >stack	redisplay
	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
	    .tab		file-step&spot&id .code-file-ON-off cr 
	    s" i"		menu-same-key-entry
	    s" item-numbers: "	['] file-item-number >stack	redisplay
	    ['] code-file-mask >stack-2		['] named-xor!	menu-entry
	    .tab		file-item-number .code-file-ON-off cr
	THEN
    THEN
    <common-menu-entries> ;

: code-file-menu ( -- )
    code-file-men
    ['] .code-file-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' code-file-menu function-key-actions >list

\ ****************************************************************
\ end	code-file-menu



\ ****************************************************************
\ ************************  old Demos  ***************************
\ ****************************************************************

\ these are old demos
\ they are not guaranteed to work on this version
\ newer demos use the playback feature, these here don't

LIST: demos

VARIABLE (demo)

: demo-menu ( -- )		\ technically speaking it's a pseudo menu
    s" old-demos.fs" REQUIRED

    help-node" Menu Demos"
    s" Choose a demo:" menu-title!
    (demo) off
    ['] demos  ['] (demo)  choose-xt-to-var
    cursor-off
    (demo) @ IF start-help THEN
    (demo) ?perform ;

\ ****************************************************************
\ end	old demos



\ ****************************************************************
\ ***********************  File output  **************************
\ ****************************************************************

decimal
256 STRINGBUF-HANDLE: (out-buffer)
: cat2out ( addr count -- )   (out-buffer) cat ;
: char2out ( c -- )   (out-buffer) char-cat ;
: num2out ( n -- )   num>string cat2out  bl char2out ;

\ I want some variables to be set in play files anyway, even if te're equal.
\ the trick is to make them look different by adding a unique comment
VARIABLE (record-anyway)	(record-anyway) off

decimal 64 STRINGBUF-HANDLE: (unique)
: unique-start ( -- addr count )	s" 	\ unique: " ;
: ?cat-unique-comment ( -- )
    (unique) string@ dup IF
	unique-start cat2out
	cat2out
	(unique) stringbuf-empty
    ELSE 2drop THEN

    (record-anyway) @ IF		\ obsolete old style
	s" 	 \ gets recorded changed or not "	cat2out
	unique-string					cat2out
	(record-anyway) off
    THEN ;

: ?record-anyway ( flag -- )   IF (record-anyway) on THEN ;

VARIABLE (outfile-id)
: set-outfile ( id -- )   (outfile-id) ! ;

: close-outfile ( -- )
    (outfile-id) @ close-file
    IF bell cr s" close-outfile: Could not close file." type-alert 2000 ms THEN
    (outfile-id) off ;	\ programmers aesthetics
    
VARIABLE (out-lines)	(out-lines) off
\ VARIABLE (out-type)	(out-type) off	\ out to string, .... buffer, screen
: out-line ( -- )	\ write string in (out-buffer) as line to (outfile-id)
    ?cat-unique-comment
    (out-buffer) string@ (outfile-id) @ write-line
    IF
	bell
	cr ." out-line: Error writing to file."
	1000 ms
    ELSE
	1 (out-lines) +!	\ useful when writing diffs
    THEN
    [ flush-files ] [IF]
	(outfile-id) @ flush-file drop
    [THEN]
    (out-buffer) stringbuf-empty ;

\ 'out-line' but not doing empty lines:
: ?out-line ( -- )  (out-buffer) buffered-length IF  out-line  THEN ;

: cat-and-out ( addr count -- )   cat2out out-line ;

debugging @ [IF]
\ Debugging: write the stack to outfile in the form Gforth .s does
: .o ( ... -- ... )
    [char] <			char2out
    depth dup num>string	cat2out
    [char] >			char2out
    bl				char2out
    dup
    0 ?DO
	dup i - pick num>string	cat2out
	bl			char2out
    LOOP
    drop
    out-line ;

    [DEFINED] <.o> [IF] ' .o IS <.o> [THEN]
[THEN]

\ ****************************************************************
\ end	File output


INCLUDE gene-edit.fs 		\ edit genes by hand


\ ****************************************************************
\ ************************  Save brew  ***************************
\ ****************************************************************

\ Words to save variables and the like to files
\ They are used to store the state of all relevant variables
\
\ Also used while recording to determine changes the user has made
\ by comparing two saved states.
\ In this case *all* variables are saved.
\ After comparing all lines that differ will be written to the record file.

: ?uncomment ( -- )
    making-bench? 0= IF EXIT THEN

    out-line
    s" \ for benchmarking, you might want to comment the following out:"
    cat-and-out ;

: save-variable ( xt -- )	\ saves variable as FORTH code in (outfile-id)
    >r				\ 'dup' would not work with offsets
    r@ EXECUTE @ num>string	cat2out
    s"  "			cat2out
    r> xt>string		cat2out
    s"  !"			cat-and-out ;

\ save a variable given in the 'base >offset' form.
\ 'base' must be on stack when saving *and* when reading the file.
: save-offset-variable ( base-addr xt -- base-addr )
    >r dup r@ EXECUTE @		( base-addr value   r: xt )
    num>string			cat2out
    s"  over "			cat2out
    r> xt>string		cat2out
    s"  !"			cat-and-out ;

: save-2variable ( xt -- )	\ saves 2variable as FORTH code in (outfile-id)
    >r r@ EXECUTE 2@ swap	\ works for offsets too like this
    num>string			cat2out
    s"  "			cat2out
    num>string			cat2out
    s"  "			cat2out
    r> xt>string		cat2out
    s"  2!"			cat-and-out ;

: save-xt-variable ( xt -- )	\ saves xt-variable as FORTH in (outfile-id)
    \ like save-variable for variables containing xt's    

    \ dup EXECUTE @	( xt value=xt2|0 )		\ not ok for offsets
    >r r@ EXECUTE @ r> swap	( xt value=xt2|0 )	\ offsets also
    dup 0= IF drop save-variable EXIT THEN	\ empty xt-variable

    s" ' "			cat2out
    xt>string			cat2out
    bl				char2out
    xt>string			cat2out
    s"  !"			cat-and-out ;

\ save a xt variable given in the 'base >offset' form.
\ 'base' must be on stack when saving *and* when reading the file.
: save-offset-xt-variable ( base-addr xt -- base-addr )
    >r dup r@ EXECUTE @		( base-addr value-xt|0   r: xt )
    dup 0= IF drop r> save-offset-variable EXIT THEN

    s" ' "			cat2out
    xt>string			cat2out
    s"  over "			cat2out
    r> xt>string		cat2out
    s"  !"			cat-and-out ;

: save-value ( xt -- )		\ saves value as FORTH code in (outfile-id)
    dup EXECUTE num>string	cat2out
    s"  TO "			cat2out
    xt>string			cat-and-out ;

: save-xt-value ( xt -- )	\ saves xt-value as FORTH code in (outfile-id)
    s" ' "			cat2out
    dup EXECUTE xt>string	cat2out
    s"  TO "			cat2out
    xt>string			cat-and-out ;

\ Open a stringbuffer and put the string into it.  Please close after use.
: buffered" ( "text text<char"> -- handle )	\ please close the buffer
    [char] " parse  string!! ;

\ Cat string as  buffered" STRING"  to the out buffer:
: out-buffered ( addr count -- )
    s" buffered"	cat2out
    [char] "		char2out
    bl			char2out
    ( addr count )	cat2out
    [char] "		char2out
    bl			char2out ;

precision 16 max CONSTANT max-float-string-length#
CREATE (floatbuf)
max-float-string-length# allot

\ Put a float as string in a buffer and return its handle.
\ Please do close the buffer after use.
: float>buffer-string ( r -- handle )	\ please do close the buffer later
    max-dfloat-display-width float>string
    dup 4 + stringbuf-open >r
    r@ string!
    r@ string@ >r dup r> fix-float-string -trailing r@ string!
    r> ;

\ Cat the hex bytes representation of the float value at r-addr to the buffer.
: cat-float-bytes ( r-addr handle -- )
    base @ >r  >r		( r-addr  r: old-base handle )
    hex

    0
    BEGIN	( r-addr offset  r: old-base handle )
	2dup + c@ num>string r@ cat
	bl r@ char-cat
	1+
	dup dfcell =
    UNTIL

    2drop rdrop

    r> base ! ;

\ Reads the float representation used by brew in the record files
\ from the buffer, store as dfloat at addr and close the buffer.
: buffered-dfloat-addr! ( handle addr -- )
    base @ >r		( handle dfloat-addr  r: old-base )
    dup [ 1 dfloats 1- ] literal +	\ get +LOOP indices right
    2>r	( handle  r: old-base lower-addr-limit upper-address-l)

    >r r@ string@ s"  " search
    0= IF  true ABORT" buffered-dfloat-addr!: Malformed string."  THEN

    hex
    EVALUATE		( hex0 hex1 ... hex7  r: base lower upper handle )
    r> stringbuf-close

    2r> DO
	i c!
    -1 +LOOP

    r> base ! ;

\ Reads the float representation used by brew in the record files
\ from the buffer, sets the float variable accordingly and closes the buffer.
: buffered-float! ( float-variable-xt handle -- )
    swap EXECUTE  buffered-dfloat-addr! ;

\ Put the dfloat value at the address in the outbuffer.  Use  buffered"
\ to represent it.
: out-dfloat-buffered ( dfloat-addr -- )
    s"  buffered"	cat2out
    [char] "		char2out
    bl			char2out

    dup df@ float>buffer-string >r
    s"   "	r@ cat
    r@		cat-float-bytes

    r@ string@		cat2out
    r> stringbuf-close
    [char] "		char2out
    bl			char2out ;

\ Saves a float variable both as human readable string and as internal
\ hex byte representation. The human readable string could be used on
\ systems with different float representation.  Result could differ slightly.
: save-dfloat-variable ( float-xt -- )
    s" ' "		cat2out
    dup xt>string	cat2out
    EXECUTE out-dfloat-buffered
    s" buffered-float!" cat-and-out ;


\ Words related to save stringbuffers:

\ Store the string of the first buffer in the second one and close the first.
: string!-x ( handle-of-buffer-to-cat addr-of-result-buffer-handle -- )
    @ >r
    dup string@ r> string!
    stringbuf-close ;

: save-stringbuf ( xt-of-buffer-pointer -- )
    s" buffered"		cat2out
    [char] "			char2out
    bl				char2out
    dup EXECUTE @ string@	cat2out
    [char] "			char2out
    bl				char2out
    xt>string			cat2out
    s"  !"			cat-and-out ;

\ Diff blocks:
: begin-string ( -- addr count )   s" \ BEGIN	" ;
: end-string ( -- addr count )   s" \ END	" ;

: ?diff-block-begin ( addr count -- )
    write-diff? 0= IF 2drop EXIT THEN

    begin-string		cat2out
    ( addr count )		cat2out
    unique-start		cat2out
    unique-string		cat-and-out ;

: ?diff-block-end ( addr count -- )
    write-diff? 0= IF 2drop EXIT THEN

    end-string			cat2out
    ( addr count )		cat-and-out ;

\ : ?diff-item-begin ( -- )
\     write-diff? 0= IF EXIT THEN

\     begin-string	cat2out
\     s" item"		cat-and-out ;

\ : ?diff-item-end ( -- )
\     write-diff? 0= IF EXIT THEN

\     end-string		cat2out
\     s" item"		cat-and-out ;

\ Save a bitmask as readable text
: save-listed-mask ( xt-of-bitmask-variable list-addr -- )
    swap >r			( list   r: xt )
    r@ EXECUTE @ swap		( bitmask node   r: xt )

    s" diff-block-as-unit" ?diff-block-begin
    s" 0 "		cat2out
    dup nodes 0 ?DO		( bitmask node )
	next-node >r		( bitmask  r: node )
	r@ @ EXECUTE		( bitmask actual-mask  r: node )
	over and IF		( bitmask actual-mask  r: node )
	    r@ @ xt>string cat2out
	    s"  OR "	cat2out
	THEN		( bitmask  r: node )
	r>			( bitmask node )
	(out-buffer) buffered-length [ c-l 2/ ] literal >	\ long line?
	IF out-line THEN				\ write short lines
    LOOP			( bitmask node  r: xt )
    2drop
    s"  "		cat2out
    r> xt>string	cat2out	( - r: - )
    s"  !"		cat2out
    out-line

    s" diff-block-as-unit" ?diff-block-end ;

: save-listed-enum-variable ( xt-of-enum-variable list-addr -- )
    over EXECUTE @  swap
    listed-enum>string	cat2out
    bl			char2out
    xt>string		cat2out
    s"  !"		cat2out		out-line ;

: save-display-slots ( flag-if-to-save-all -- )	\ display-slots to (outfile-id)
    base @ >r  decimal

    ['] display-slots save-variable

    IF   max-display-slots#	\ while recording we save them all
    ELSE display-slots @ 	\ otherwise only the ones that are displayed
    THEN 0 DO
	s" ' "				cat2out
	i display-slot @ xt>string	cat2out
	s"  "				cat2out
	i num>string			cat2out
	s"  display-slot !"		cat-and-out
    LOOP
    r> base ! ;

: which-random-seed ( -- xt u )		\  xt and cells
    random-xt @ CASE
	['] random-generalized OF
	    ['] (random-generalized)  2
	ENDOF
	['] random-BRODIE OF
	    ['] seed-BRODIE  1
	ENDOF
	true ABORT" which-random-seed: Unknown random generator."
    ENDCASE ;

\ save kind and state of the actual random generator as line to (outfile-id)
\ or if flag is true save them all.
: save-random-generator ( flag-if-to-save-all -- )
    base @ >r  decimal

    s" \ random generator:"		cat-and-out
    IF	\ while recording we save them all to catch differences
	['] random-xt			save-xt-variable
	\ do save the appropriate random seed anyway, changed or not:
	random-xt @ ['] random-generalized = IF true ?record-anyway THEN
	['] (random-generalized)	save-2variable
	random-xt @ ['] random-BRODIE	   = IF true ?record-anyway THEN
	['] seed-BRODIE			save-variable
    ELSE
	['] random-xt			save-xt-variable
	random-xt @ CASE
	    ['] random-generalized OF
		['] (random-generalized) save-2variable
	    ENDOF
	    ['] random-BRODIE OF
		['] seed-BRODIE		save-variable
	    ENDOF
	ENDCASE
    THEN
    r> base ! ;

: save-color-scales ( -- )
    base @ >r  decimal

    c-l stringbuf-open
    field-i-planes# 0 ?DO
	i spot-var-name	third string!
	s" >color-scale"	third cat	\ name of scale adress
	dup string@ get-xt	save-variable
    LOOP
    stringbuf-close
    r> base ! ;


\ Accumulate all the words in the following lines until  accumulate-end-mark
\ in a stringbuf, return handle.
\ Text following 'accumulate' (same line) is disregarded.
: accumulate-end-mark ( -- addr count )   s" END-accumulate" ;
: accumulate ( -- handle )
    [ decimal ] 512 stringbuf-open >r
    refill drop				\ skip 'accumulate' line

    BEGIN
	source >in @ - dup 0= IF
	    2drop
	    refill
	    0= ABORT" accumulate: End mark missing."
	    source accumulate-end-mark search nip nip IF
		refill drop
		r> EXIT
	    THEN
	    source
	THEN

	BEGIN
	    bl-skip_ >in +!
	    next-word dup WHILE		\ something left?
	    dup >in +!
	    r@ cat
	    bl r@ char-cat
	REPEAT drop

	true WHILE
    REPEAT
    r> ;

: save-nucs-genes-trial ( -- )
    s" accumulate" cat-and-out out-line
    wake-me-xt @  dup  eb>length @  swap eb>sequence swap  out-trial-gene
    accumulate-end-mark  cat-and-out
    s" |compile-from-string| [IF]"  cat-and-out
    s"     set-up-trial" cat-and-out
    s" [ELSE]  bell  [THEN]" cat-and-out ;

: save-nucs-genes ( -- )
    base @ >r  decimal
    s" diff-block-as-unit" ?diff-block-begin

    on-trial? IF
	save-nucs-genes-trial
    ELSE
	nuc-genes-limit 0 DO
	    s" gene' "		cat2out		\ save gene
	    i nuc-addr @ xt>string	cat2out
	    s"     	"		cat2out
	    i nuc-var-name		cat2out
	    s"  !"			cat-and-out

	    s" internal' "		cat2out		\ save internal
	    i 1+ nuc-addr @ xt>string	cat2out
	    s"     	"		cat2out
	    i 1+ nuc-var-name	cat2out
	    s"  !"			cat-and-out
	    2 +LOOP
    THEN

    s" diff-block-as-unit" ?diff-block-end

    r> base ! ;


nuc-floats# [IF]
: save-dfloat-nuc-vars ( -- )
    dfloat-nuc-vars dup nodes 0 ?DO
	next-node
	dup @ save-dfloat-variable
    LOOP drop ;
[THEN]

: save-nuc ( -- )	\ saves actual nuc to file (outfile-id)
    base @ >r  decimal

    \ If the user has set a nuc from a selected individual 'nuc-is-word'
    \ would be set.  It *must* get cleared:
    nuc-flags @ >r
    nuc-flags dup @ [ nuc-is-word invert ] literal and swap !

    out-line						\ start with empty line
    s" \ define nuc and set it actual:"	cat-and-out	\ comment
\   length @ num>string		cat2out	\ error when different nuc structure!
    s" nuc-length# new-nucleus DROP cp!"	cat-and-out
    write-diff? 0= IF
	?record-cloned
    THEN

    save-nucs-genes				\ genes

    nuc-xt-limit nuc-xt's DO			\ save xt's
	s" ' "			cat2out
	i nuc-addr @ xt>string	cat2out
	s"     	"		cat2out
	i nuc-var-name		cat2out
	s"  !"			cat-and-out
    LOOP

    \ length should work in benchmarks, when it is set to another value
    \ because of changed nuc structure.  I deliberately set it twice:
    nuc-checksum-start nuc-variables DO			\ save first block
	i nuc-addr @ num>string	cat2out
	s"  "			cat2out
	i nuc-var-name		cat2out
	s"  !"			cat-and-out
    LOOP
    s" nuc-length# length !"	cat-and-out		\ correct length

    nuc-scan-limit nuc-checksum-start DO		\ save other variables
	i nuc-addr @ num>string	cat2out
	s"  "			cat2out
	i nuc-var-name		cat2out
	s"  !"			cat-and-out
    LOOP

    \ We do this one again in more readable form:
    ['] my-diversifctn-mask nuc-div-masks save-listed-mask
[ nuc-floats# ] [IF]
    ['] f-organ-div-mask item-masks save-listed-mask
    ['] f-param-div-mask item-masks save-listed-mask
    ['] f-invisibl-div-mask item-masks save-listed-mask

    save-dfloat-nuc-vars
[THEN]

    r> nuc-flags !
    r> base ! ;

: save-xt-probability-pool ( pool-xt -- )	\ saves to (outfile-id)
    base @ >r  decimal

    (name-buf) >r  r@ stringbuf-empty
    dup xt>string r@ cat			\ pool name in (name-buf)

    out-line					\ start with empty line
    s" \  set probabilities in xt probability pool " cat2out	\ start comment
    r@ string@ cat2out  [char] : char2out  out-line		\ end comment

    r> string@ cat2out  s"  nul-all-probabilities" cat2out  out-line

    64 stringbuf-open swap	( handle xt )
    execute @			( handle list-addr )
    dup how-many 0 ?DO
	dup i this-node >r	( handle list-addr  r: actual-node )

	r@ >probability @ num>string	cat2out
	s"  ' " cat2out  r@ >data @ xt>string cat2out
	s" 	" cat2out  (name-buf) string@ cat2out  bl char2out
	r@ >prob-flags @ is-list and IF
	    r@ >data @ pad !   pad cell  3 pick cat	\ xt for recursion
	    s" set-as-sublist"
	ELSE
	    s" set-one"
	THEN
	cat-and-out
	rdrop
    LOOP
    drop		( handle )

    dup string@ 0 ?DO
	dup i + @ RECURSE
    cell +LOOP
    drop
    stringbuf-close
    r> base ! ;

: save-actual-gene-pool ( -- )	\ saves to (outfile-id)
    base @ >r  decimal

    out-line						\ start with empty line
    s" \  set probabilities in actual pool:"	cat-and-out      \ comment
    ['] actual-genepool-xt			save-xt-variable

    s" actual-genepool-xt @ execute nul-all-probabilities" cat2out
    \ As last line must be included in diff's while recording when pool changes
    \ I add a comment containing the pools name:
    recording? IF
	s" 	\ "				cat2out
	actual-genepool-xt @ xt>string		cat2out		\ trick...
    THEN					out-line

    actual-genepool-xt @ execute @
    dup how-many 0 ?DO
	dup i this-node >r	( r: actual-node )		
	r@ >probability @ num>string	cat2out
	s"  	internal+' "		cat2out		\ find nested pools too
	r> >data @ xt>string		cat2out  s" 	" cat2out
	s" actual-genepool-xt @ execute set-one" cat-and-out
    LOOP
    drop
    r> base ! ;

: (save-genome-pool) ( pool-xt -- list-of-sub-pools )	\ saves to (outfile-id)
    1 deflist swap	( list-of-sub-pools pool-xt )
    out-line						\ start with empty line
    s" diff-genome-pool" 2>r   2r@ ?diff-block-begin
    s" \ set probabilities in genome-pool "	cat2out
    dup xt>string				cat-and-out	\ comment

    dup xt>string				cat2out
    s"  nul-all-probabilities"			cat-and-out

    dup EXECUTE @
    dup how-many 0 ?DO		( list-of-sub-pools pool-xt pool-addr )
	dup i this-node >r	( list pool-xt pool-addr  r: actual-node )

	r@ >probability @ num>string	cat2out
	s"  internal+' "		cat2out		\ find nested pools too
	r@ >data @ xt>string		cat2out
	bl				char2out
	over xt>string			cat2out
	s"  set-one"			cat-and-out

	r@ >genome-usage @ num>string	cat2out
	s"  internal+' "		cat2out		\ find nested pools too
	r@ >data @ xt>string		cat2out
	bl				char2out
	over xt>string			cat2out
	s"  it's-node >genome-usage !"	cat-and-out

	r@ >prob-flags @ is-list and IF
	    r@ >data @ third >list		\ xt of sub-pool to list 
	THEN
	rdrop
    LOOP
    2drop

    2r> ?diff-block-end ;

: save-genome-pool ( pool-xt -- )   \ saves to (outfile-id)

    base @ >r  decimal

    (save-genome-pool)		( list-of-sub-pools   r: old-base )
    dup nodes 0 ?DO
	next-node
	dup @ RECURSE
    LOOP
    remove-list

    r> base ! ;

: save-current-genome-pool ( -- )	\ saves to (outfile-id)
    \ start with empty line and comment
    out-line
    s" \ set probabilities in current-genome-pool "	cat2out
    current-genome-pool-xt @ xt>string			cat-and-out

    ['] current-genome-pool-xt	save-xt-variable

    current-genome-pool-xt @ save-genome-pool ;

: other-nodes-(none) ( node -- )	\ inserts ' (none) in all other nodes
    BEGIN
	next-node
    dup WHILE
	['] (none) over ( >cont-xt ) !
    REPEAT drop ;

: cont-node-out ( i -- )
    num>string cat2out
    s"  continuous-display-list n'th-or-new-node " cat2out ;

: cont-n-out ( i n -- )
    num>string  cat2out  s"  		" cat2out  cont-node-out ;

: cont-float-out ( i dfloat-addr -- )
    out-dfloat-buffered  s" 		" cat2out  cont-node-out ;

: cont-xt-out ( i xt -- )
    s" ' " cat2out  xt>string cat2out
    s"  	" cat2out cont-node-out ;

: save-continuous-display ( -- )	\ we don't need the flag
    write-diff?	( flag )		\ as we can determine it here
    base @ >r  decimal

    out-line
    s" \ save-continuous-display:" cat-and-out 
    ['] (continuous-column)	save-variable	\ not sure about that one
    ['] cont-zoom-up-scale	save-2variable

    \ start with a empty list when playing back a recorded data set
    \ ( in diffs this does not get included )
    s" continuous-display-list empty-list " cat-and-out

    continuous-display-list
    dup nodes 0 ?DO		( flag list )
	i over n'th-node	( flag list node )
	dup >cont-xt @
	i over cont-xt-out  s"  >cont-xt ! " cat-and-out

	over >cont-item @ swap
	( flag list node item cont-xt ) ['] get-variable = IF
	    i swap cont-xt-out
	ELSE
	    i swap cont-n-out
	THEN
	s"  >cont-item ! " cat-and-out

	i over >cont-var-type @  variable-types  listed-enum>string cat2out
	s"  	" cat2out  cont-node-out s"  >cont-var-type ! " cat2out
	out-line

	dup >cont-var-type @  type-df-addr% = IF
	    i over >cont-lower cont-float-out
	    s" >cont-lower buffered-dfloat-addr! " cat-and-out
	    i over >cont-upper cont-float-out
	    s" >cont-upper buffered-dfloat-addr! " cat-and-out
	ELSE
	    i over >cont-lower @ cont-n-out  s"  >cont-lower ! " cat2out
	    out-line
	    i over >cont-upper @ cont-n-out  s"  >cont-upper ! " cat2out
	    out-line
	THEN

	i over >cont-foreground-xt @ cont-xt-out s"  >cont-foreground-xt ! "
	cat-and-out
	i swap >cont-char @ cont-n-out s"  >cont-char ! " cat-and-out
    LOOP drop			( flag )

    \ we need same number of lines in the diff files:
    ( flag ) IF
	max-continuous-items#  continuous-display-list nodes - 0 ?DO
	    s" (none)	\ placeholders:"	cat-and-out
	    5 0 DO
		s" (none)"			cat-and-out
	    LOOP
	LOOP
    THEN
    r> base ! ;

\ Words to save dfloat check data: I define a couple of words that look like
\ ordinaray variables:
: df-inf-count ( -- addr )   (dfloat-check-data) >-inf-count ;
: df-real-count ( -- addr )  (dfloat-check-data) >real-count ;
: df+inf-count ( -- addr )   (dfloat-check-data) >+inf-count ;
: df-nan-count ( -- addr )   (dfloat-check-data) >nan-count ;
: df-max ( -- df-addr )	     (dfloat-check-data) >dfloat-max ;
: df-min ( -- df-addr )	     (dfloat-check-data) >dfloat-min ;
: last-df-inf-count ( -- addr )   (last-dfloat-check-data) >-inf-count ;
: last-df-real-count ( -- addr )  (last-dfloat-check-data) >real-count ;
: last-df+inf-count ( -- addr )   (last-dfloat-check-data) >+inf-count ;
: last-df-nan-count ( -- addr )   (last-dfloat-check-data) >nan-count ;
: last-df-max ( -- df-addr )	  (last-dfloat-check-data) >dfloat-max ;
: last-df-min ( -- df-addr )	  (last-dfloat-check-data) >dfloat-min ;

: dfloat-cat-unique ( df-addr handle -- )
    [ 1 dfloats ] literal 0 DO
	over i + @ num>string third cat
    cell +LOOP
    2drop ;

: step-unique ( -- )	\ builds a unique string from the parameters
    base @ hex
    (unique) >r

    (scan-xt) @			num>string r@ cat
    (scan-detail) @	num>string r@ cat
    (scan-lines) @		num>string r@ cat
    (vertical-display-range) @	num>string r@ cat
    scan-background-xt @	num>string r@ cat
    scan-foreground-xt @	num>string r@ cat
    (scan-flags) @		num>string r@ cat

    (scan-min-max) 2@		num>string r@ cat  num>string r@ cat
    (last-scan-min-max) 2@	num>string r@ cat  num>string r@ cat
    df-inf-count @		num>string r@ cat
    df-real-count @		num>string r@ cat
    df+inf-count @		num>string r@ cat
    df-nan-count @		num>string r@ cat
    df-max			r@ dfloat-cat-unique
    df-min			r@ dfloat-cat-unique
    last-df-inf-count @		num>string r@ cat
    last-df-real-count @	num>string r@ cat
    last-df+inf-count @		num>string r@ cat
    last-df-nan-count @		num>string r@ cat
    last-df-max			r@ dfloat-cat-unique
    last-df-min			r@ dfloat-cat-unique

    horizontal-zoom-scale 2@	num>string r@ cat  num>string r@ cat
    vertical-zoom-scale 2@	num>string r@ cat  num>string r@ cat

    rdrop
    base ! ;

: save-step-display-settings ( flag -- )	\ save all or only relevant?
    step-display-on? 0= over 0= and
    step-snapshots? 0= and
    IF drop EXIT THEN

    base @ >r  decimal

    out-line
    s" \ save-step-display-settings:" cat-and-out 
    ['] step-display-items	save-variable

    dup IF max-step-display-items# ELSE step-display-items @ THEN
    0 ?DO			( flag )
	i (scan-index) !

	write-diff? step-display-on? OR step-snapshots? OR IF
	    step-unique
	THEN

	['] (scan-index)		save-variable

	['] (scan-xt)			save-xt-variable
	(scan-xt) @ ['] continuous-display = IF drop TRUE THEN	\ set flag

	(scan-xt) @  ['] nuc-scan-func-dspl = IF	\ hacky....
	    ['] (scan-detail)		save-xt-variable
	ELSE
	    ['] (scan-detail)		save-variable
	THEN

	['] (scan-lines)		save-variable
	['] scan-background-xt		save-xt-variable
	['] scan-foreground-xt		save-xt-variable
	['] (scan-min-max)		save-2variable
	['] (last-scan-min-max)		save-2variable

	\ Save (dfloat-check-data):
	['] df-inf-count		save-variable
	['] df-real-count		save-variable
	['] df+inf-count		save-variable
	['] df-nan-count		save-variable
	['] df-max			save-dfloat-variable
	['] df-min			save-dfloat-variable

	\ Save (last-dfloat-check-data):
	['] last-df-inf-count		save-variable
	['] last-df-real-count		save-variable
	['] last-df+inf-count		save-variable
	['] last-df-nan-count		save-variable
	['] last-df-max			save-dfloat-variable
	['] last-df-min			save-dfloat-variable

	['] horizontal-zoom-scale	save-2variable
	['] vertical-zoom-scale		save-2variable
	['] (vertical-display-range)	save-variable
	['] (scan-flags)  scan-flags	save-listed-mask
	out-line
    LOOP

    ['] (step-more-info)		save-variable

    IF save-continuous-display THEN
    r> base ! ;

\ Word to preserve information about compile time values in record files.
\ Can be misused for constant too.
: save-commented-value ( xt -- )   s" \ " cat2out save-value ;

: save-nuc-compile-options ( -- )
    base @ >r  decimal

    out-line
    s" \ brew was compiled with the following nuc compile time values:"
    cat-and-out

    ['] nuc-organs#		save-commented-value
    ['] nuc-f-organs#		save-commented-value
    ['] nuc-parameters#		save-commented-value
    ['] nuc-f-parameters#	save-commented-value
    ['] nuc-invisibles#		save-commented-value
    ['] nuc-f-invisibles#	save-commented-value
    ['] nuc-secrets#		save-commented-value
    ['] nuc-f-secrets#		save-commented-value
    r> base ! ;

: save-world-compile-options ( -- )
    base @ >r  decimal

    out-line
    s" \ brew was compiled with the following world compile time values:"
    cat-and-out

    ['] spot-qualities#	 save-commented-value
    ['] spot-f-qualities#	 save-commented-value
    ['] spot-secrets#	 save-commented-value
    ['] spot-f-secrets#	 save-commented-value
    ['] spot-properties# save-commented-value
    ['] spot-f-properties# save-commented-value
    
    r> base ! ;

: save-menu-configuration ( -- )
    base @ >r  decimal

    out-line
    s" \ menu configuration data:"	cat-and-out
    ['] (genome-sort-index)		save-variable
    ['] (sort-upwards)			save-variable
    ['] (diversification-menu-type)  locality-types  save-listed-enum-variable
    ['] (nuc-menus-show-dfloats)	save-variable
    ['] (show-float-type-counts)	save-variable
    ['] (nuc-menu-visible-floats)	save-variable
    ['] (spot-menus-show-dfloats)	save-variable
    ['] (menu-global-vars-show-dfloats)	save-variable
    r> base ! ;

: ?f ( handle -- ) \ "?save-field-name"
    string@ dup IF
	cat2out
    ELSE 2drop THEN ;
: save-maybe-do-field ( xt -- )
    base @ >r  decimal

    out-line						\ start with empty line
    s" \  save maybe-do-field: "	cat2out
    dup xt>string			cat-and-out	\ comment

    [ decimal ] 32 stringbuf-open >r
    write-diff? IF			\ while writing diff we prepend
	dup xt>string  r@ cat		\   every line with fields name
	s"  	"    r@ cat		\   to have the right field active...
    THEN
    dup EXECUTE						\ activate field
    xt>string				cat-and-out	\ field name
    r@ ?f ['] (expression-xt)		save-xt-variable
    r@ ?f ['] (condition-xt)		save-xt-variable
    r@ ?f ['] (simple-expression-xt)	save-xt-variable
    r@ ?f ['] (do-it-xt)		save-xt-variable
    r@ ?f ['] (expr-parameter)		save-variable
    r@ ?f ['] (expr-parameter-2)	save-variable
    r@ ?f ['] (expr-xt-1)		save-xt-variable
    r@ ?f ['] (expr-xt-2)		save-xt-variable
    r@ ?f ['] (expr-df-xt-1)		save-xt-variable
    r@ ?f ['] (expr-df-xt-2)		save-xt-variable
    r@ ?f ['] (xt-do-it)		save-xt-variable
    r@ ?f ['] (df-xt-do-it)		save-xt-variable
    r@ ?f ['] (do-it-parameter)		save-variable
    r@ ?f ['] (do-it-parameter-2)	save-variable
    r@ ?f ['] (do-it-scale)		save-2variable
    r@ ?f ['] (maybe-do-type-xt)	save-xt-variable
    r@ ?f ['] (expression-handle)	save-stringbuf
    r@ ?f ['] (maybe-do-handle)		save-stringbuf

    r@ ?f ['] (expr-df-parameter)	save-dfloat-variable
    r@ ?f ['] (expr-df-parameter-2)	save-dfloat-variable
    r@ ?f ['] (do-it-df-parameter)	save-dfloat-variable
    r@ ?f ['] (do-it-df-parameter-2)	save-dfloat-variable

    r> stringbuf-close
    r> base ! ;

\ Saving world variables having an entry for each dimension:
: save-for-each-dimension ( xt flag -- )
    IF max-dimensions# ELSE world-dimensions @ THEN >r
    dup EXECUTE
    r> 0 ?DO		( xt base-address )
	dup i cells + @		num2out
	over xt>string		cat2out
	bl			char2out
	i			num2out
	s" cells + ! "		cat-and-out
    LOOP
    2drop ;

\ Save configurable world parameters like visibility and such
: save-world-parameters ( flag -- )	\ save all or only relevant?
    base @ >r  decimal

    out-line
    s" \ Save world parameters : "	cat2out
    world-string dup string@		cat-and-out  stringbuf-close

    ['] dimension-ranges over	save-for-each-dimension
    ['] visibility-on	 over	save-for-each-dimension
    ['] visibility-off   over	save-for-each-dimension
    dup world-dimensions @ 2 > or IF
	['] backgound-off	save-variable
    THEN

    ['] fixed-population-size	save-variable
    ['] elite			save-variable

    drop
    r> base ! ;

: save-listed-int-variables ( list -- )
    base @ >r

    dup nodes 0 ?DO
	next-node
	dup @ save-variable
    LOOP
    drop
    r> base ! ;

: save-listed-dfloat-variables ( list -- )
    base @ >r

    dup nodes 0 ?DO
	next-node
	dup @ save-dfloat-variable
    LOOP
    drop
    r> base ! ;

: save-brew-variables ( -- )	\ save all variables to (outfile-id)
    base @ >r  decimal

    out-line						\ empty line
    s" \ Save brew variables:"	cat-and-out		\ comment
    s" base @  decimal"		cat-and-out

    \ for the sake of simplicity we out *all* variables while recording
    \ (this asks for special treatment of the rare case, when we save
    \ variables while recording)
    \ I do the same with log files now.
    run-mode @ [ recording write-diff or ] literal and
    0<> >r	( r: flag-if-we-are-recording )

    out-line
    s" \ brew general settings:"	cat-and-out		\ comment
    ['] world-do-direction		save-variable
    ['] (linear-index)			save-variable
    ['] spot-do-xt			save-xt-variable
    ['] cell-do-before-xt		save-xt-variable
    ['] cell-do-after-xt		save-xt-variable
    ['] step-do-before-xt		save-xt-variable
    ['] step-do-after-xt		save-xt-variable
    ['] future-quality-change		save-variable
    ['] cell-division-moves-both	save-variable
    ['] cell-division-diversify-both	save-variable
    ['] cell-division-mutate-both	save-variable

    ?uncomment
    ['] log-mask log-masks		save-listed-mask

    ?uncomment
    ['] code-file-mask code-file-masks	save-listed-mask

    r@ save-random-generator

    out-line
    s" \ world:"			cat-and-out		\ comment
    r@ save-world-parameters

    out-line
    s" \ global variables:	integers:"	cat2out		\ comment
    global-integer-variables#			num2out
    s"   / dfloats:"				cat2out
    global-dfloat-variables#			num2out out-line

    global-int-variables save-listed-int-variables
    global-dfloat-variables save-listed-dfloat-variables

    out-line
    s" \ food, costs, population control:"	cat-and-out	\ comment

    elitism? IF  s" elitism!"  ELSE  s" elitism-off"  THEN cat2out out-line

    ['] world-food-supply		save-variable
    ['] food-share/spot			save-variable
    ['] individual-fixed-food-share	save-variable
    ['] nuc-do-cost			save-variable
    ['] code-price			save-variable
    ['] code-price-scale		save-2variable
    \ ['] clone-cost			save-variable
    ['] leave-energy-after-death	save-variable

    ['] additive-stress			save-variable
    ['] additive-release		save-variable
    ['] stress-rate			save-2variable
    ['] multiplicative-release		save-variable
    ['] code-additive-stress		save-variable
    ['] code-stress-rate		save-2variable
    ['] high-water-mark			save-variable
    ['] flood-mark			save-variable
    ['] flood-stress-rate		save-2variable
    ['] flood-kill-rate			save-2variable
    ['] flood-energy-rate		save-2variable
    ['] flood-food-rate			save-2variable
    ['] sos-mark			save-variable
    ['] sos-sow				save-variable
    ['] sos-release-rate		save-2variable
    ['] sos-reproduction-push		save-variable
    ['] low-water-mark			save-variable
    ['] up-regulation-start		save-variable
    ['] nuc-cost-can-be-help?		save-variable
    ['] code-price-can-be-help?		save-variable
    ['] score-rate			save-2variable

    out-line
    s" \ diversification:"		cat-and-out		\ comment
    ['] diversification-mask nuc-div-masks save-listed-mask
    ['] diversification-range		save-variable
    ['] diversification-rate		save-2variable
    ['] nuc-diversification-closeness	save-variable
    ['] sporadic-value-range		save-variable
    ['] sporadic-value-rate		save-2variable

[ nuc-floats# ] [IF]
    ['] global-f-organ-div-mask item-masks save-listed-mask
    ['] global-f-parameter-div-mask item-masks save-listed-mask
    ['] global-f-invisible-div-mask item-masks save-listed-mask
    ['] nuc-f-diversification-rate	save-dfloat-variable
    ['] nuc-f-diversification-range	save-dfloat-variable
    ['] nuc-f-diversification-factor	save-dfloat-variable
    ['] f-sporadic-value-rate		save-dfloat-variable
    ['] f-sporadic-value-range		save-dfloat-variable
[THEN]

    ['] spot-diversification-mask spot-div-masks save-listed-mask
    ['] spot-diversification-range	save-variable
    ['] spot-diversifictn-closeness	save-variable

[ spot-floats# ] [IF]
    ['] f-qualities-div-mask	item-masks  save-listed-mask
    ['] f-properties-div-mask	item-masks  save-listed-mask
    ['] f-secrets-div-mask	item-masks  save-listed-mask
\    ['] spot-f-diversification-rate	save-dfloat-variable
    ['] spot-f-diversification-range	save-dfloat-variable
    ['] spot-f-diversification-factor	save-dfloat-variable
[THEN]

    ['] global-diversification-mask	item-masks	save-listed-mask
    ['] global-i-diversifictn-rate	save-2variable
    ['] globals-diversifictn-range	save-variable
    ['] globals-divers-closeness	save-variable
    ['] global-df-div-mask		item-masks	save-listed-mask
    ['] global-f-diversifctn-rate	save-2variable
    ['] global-f-diversifctn-range	save-dfloat-variable
    ['] global-f-diversifctn-factor	save-dfloat-variable

    ?uncomment
    ['] display-switch display-switch-masks save-listed-mask

    out-line
    s" \ display:"			cat-and-out		\ comment
    ['] look-at-xt			save-xt-variable

    ['] snapshot-frequency		save-variable
    ['] 2-ascii-scale			save-variable
[ nuc-floats# ] [IF]
    ['] f-2-ascii-scale			save-dfloat-variable
[THEN]
    ['] show-int-nuc-var-xt		save-xt-variable
    ['] show-sign-tolerance		save-variable
[ nuc-floats# ] [IF]
    ['] show-float-nuc-var-xt		save-xt-variable
    ['] float-show-sign-tolerance	save-dfloat-variable
[THEN]

    ['] (prior-display-type)		save-variable

    ?uncomment
    r@ save-display-slots

    out-line
    s" \ colours:"			cat-and-out		\ comment
    ['] background-color-xt		save-xt-variable
    ['] foreground-color-xt		save-xt-variable
    ['] color-selected-fg-xt		save-xt-variable
    ['] color-below-fg-xt		save-xt-variable
    ['] color-above-fg-xt		save-xt-variable
    ['] color-miss-fg-xt 		save-xt-variable
    ['] color-selected-bg-xt		save-xt-variable
    ['] color-below-bg-xt		save-xt-variable
    ['] color-above-bg-xt		save-xt-variable
    ['] color-miss-bg-xt 		save-xt-variable
    ['] age>color-scale			save-variable
    save-color-scales

    ?uncomment
    r@ save-step-display-settings

    out-line
    s" \ mutation:"			cat-and-out		\ comment
    ['] mutation-rate			save-2variable
    mutation-rate @ 0<> r@ or IF
	['] stack-turning-point		save-variable
	['] mutations-threshold		save-variable
	['] mutation-max-ollowed-items	save-variable
	['] trial-phase			save-variable
	['] max-if-items		save-variable
	['] conditional-token-price	save-variable
	['] resolve-flags		save-variable
	['] reset-nuc-masks?		save-variable
	['] (exceeding-size-ring)	save-variable
	['] mutation-types save-xt-probability-pool
	out-line
    THEN

    save-actual-gene-pool
    save-current-genome-pool

    out-line
    s" \ conditional execution:"	cat-and-out		\ comment
    ['] maybe-do-on-subset-field	save-maybe-do-field
    ['] maybe-do-spot-subset-field	save-maybe-do-field
    ['] maybe-select-field		save-maybe-do-field
    ['] maybe-do-on-selected-field	save-maybe-do-field
    ['] maybe-do-this-genome-field	save-maybe-do-field
    ['] do-on-world-field		save-maybe-do-field
    ['] fg-colour-field			save-maybe-do-field
    ['] bg-colour-field			save-maybe-do-field
    
    out-line
    s" \ I/O:"				cat-and-out		\ comment
    ?uncomment
    out-line

    ['] brew-at-xy-xt			save-xt-variable
    out-line

    ?uncomment
    s" \ save function keys:"		cat-and-out 
    ['] F1-xt      			save-xt-variable
    ['] F2-xt      			save-xt-variable
    ['] F3-xt      			save-xt-variable
    ['] F4-xt      			save-xt-variable
    ['] F5-xt      			save-xt-variable
    ['] F6-xt      			save-xt-variable
    ['] F7-xt      			save-xt-variable
    ['] F8-xt      			save-xt-variable
    ['] F9-xt      			save-xt-variable
    ['] F10-xt     			save-xt-variable
    ['] F11-xt     			save-xt-variable
    ['] F12-xt     			save-xt-variable
    ['] shift-F1-xt			save-xt-variable
    ['] shift-F2-xt			save-xt-variable
    ['] shift-F3-xt			save-xt-variable
    ['] shift-F4-xt			save-xt-variable
    ['] shift-F5-xt			save-xt-variable
    ['] shift-F6-xt			save-xt-variable
    ['] shift-F7-xt			save-xt-variable
    ['] shift-F8-xt			save-xt-variable

    ?uncomment
    save-menu-configuration

    s" base !"				cat-and-out

    rdrop
    r> base ! ;

\ Ask user for a filename to save something (described by the string) in.
\ Create the file if it does not exist already, else warn the user.
\ set-outfile  on success and return a flag:
: |open-new-outfile| ( addr count -- flag )	\ clear screen before use
    cr ." Give a filename to save " type ."  in "
    pad 80 accept
    dup 0= IF cr ." not saved " bell 1200 ms  EXIT THEN

    pad swap
    file-names-length# stringbuf-open >r
    out-dir		r@ cat
    ( pad count )	r@ cat
    r@ string@ r/o open-file
    IF			\ test if file exists already
	drop
    ELSE
	cr ." File exists, not saved." 2000 ms
	close-file
	r> stringbuf-close
	drop FALSE EXIT
    THEN

    r@ string@ w/o CREATE-FILE+
    IF
	drop
	bell cr ." |open-new-outfile|: Error creating "
	r@ string@ type 2000 ms
	r> stringbuf-close
	FALSE EXIT
    THEN

    cr r@ string@ type
    r> stringbuf-close
    set-outfile
    TRUE ;

: |save-brew-variables| ( -- )
    page
    s" brew variables" |open-new-outfile|
    0= IF  bell  EXIT  THEN

    save-brew-variables
    close-outfile ;

: save-spot ( -- )	\ spot must be set
    out-line
    s" \ saving spot "	cat2out
    spot @ num>string
    2dup		cat-and-out

    ( addr count )	cat2out
    s" 	>spot!"		cat-and-out

    integer-spot-vars >r
    r@ next-node	\ skip fcp
    r> nodes 1- 0 ?DO
	next-node
	dup @ save-variable
    LOOP drop

    dfloat-spot-vars dup nodes 0 ?DO
	next-node
	dup @ save-dfloat-variable
    LOOP drop ;

: (save-world) ( -- )
    base @ >r  decimal
    out-line
    s" \ save all spots data: "	cat2out
    world-string dup string@	cat-and-out  stringbuf-close
    ['] save-spot do-everywhere
    r> base ! ;

: save-world ( -- )  true save-world-parameters  (save-world) ;

:NONAME \ : |save-world| ( -- )
    page cr
    world-string dup string@ type cr  stringbuf-close
    s" Save all spot data as Forth text file, writes a huge file..."
    type-other-colour cr
    s" all spot data" |open-new-outfile| 0= IF  bell EXIT  THEN

    cr s" saving" type-other-colour cr
    save-world
    close-outfile ; IS |save-world|

: save-nuc&spot ( -- )
    save-nuc
    spot @ num>string			cat2out
    s" 	>spot!"				cat-and-out
    s" inhabited? [IF] die [THEN]"	cat-and-out
    s" |cp@| fcp !"			cat-and-out
    s" ?increase-genome-probability"	cat-and-out
    s" nucs-not-scanned"		cat-and-out ;

: save-all-nucs ( -- )
    count-living 0= IF EXIT THEN

    base @ >r  decimal
    out-line
    s" \ Saving all nucs :"	cat-and-out
    ['] save-nuc&spot do-with-everybody

    r> base ! ;

: |save-all-nucs| ( -- )
    count-living 0= IF EXIT THEN

    page cr
    s" Save all nucs as a Forth text file:" type-other-colour cr
    s" all nucs data" |open-new-outfile| 0= IF  bell EXIT  THEN

    cr s" saving" type-other-colour cr
    save-all-nucs
    close-outfile ;

\ ****************************************************************
\ end	save brew



\ ****************************************************************
\ ******************  Record and Playback  ***********************
\ ****************************************************************

decimal
128 STRINGBUF-HANDLE: (record-file-name)
VARIABLE (record-file-id)

: record-as-outfile ( -- )   (record-file-id) @ set-outfile ;

\ : ?record-free-field ( -- )
:NONAME ( -- )
    NOT-recording? IF EXIT THEN		\ only if we do recording

    record-as-outfile
    out-line							\ empty line
    s" \ restart with an unpopulated world:" cat-and-out	\ comment
    s" free-field 	\ in world: "	cat2out
    world-name2@			cat-and-out
; IS ?record-free-field

\ : ?record-sow ( n -- )
:NONAME  ( n -- )
    NOT-recording? IF drop EXIT THEN		\ only if we do record

    record-as-outfile
    save-nuc					\ save actual nuc

    out-line					\ empty line
    s" \ sow above nuc:"      cat2out  out-line	\ comment

    false save-random-generator			\ save actual random generator
    ['] (sow-diversified)	save-variable	\ diversified?
    num>string			cat2out		\ number to sow
    s"  sow drop time-step"	cat-and-out
    s" nucs-not-scanned"	cat-and-out
; IS ?record-sow

\ : ?record-cloned ( -- )		\ fix assertion of benchmarks results
:NONAME ( -- )
    NOT-recording? IF EXIT THEN		\ only if we do recording

\   record-as-outfile			\ no! don't do that here!
    s" \ needed for assertion of benchmark result:" cat-and-out
    ['] cloned		save-variable
; IS ?record-cloned

\ ?record-remove-cell
:NONAME ( -- )
    NOT-recording? IF EXIT THEN		\ only if we do recording

    record-as-outfile
    out-line						\ empty line
    s" \ cell removed by user:" cat-and-out		\ comment
    spot @ num>string		cat2out
    s"  >spot! fcp @ cp! die "	cat-and-out
    s" nucs-not-scanned"	cat-and-out
; IS ?record-remove-cell

\ ?record-edit-spot?
:NONAME ( -- changed-flag )
    \ check for changes:
    spot-vars@ spot-after spot-vars!
    spot-before  field-i-planes# cells  spot-after over  compare
    IF 1 ELSE 0 THEN

[ spot-floats# ] [IF]
    spot-df-vars@ spot-df-after spot-df-vars!
    spot-df-before spot-floats# dfloats  spot-df-after over  compare
    IF 2 OR THEN
[THEN]
    
    dup 0= IF EXIT THEN			\ no changes?
    NOT-recording? IF EXIT THEN		\ do recording?

    record-as-outfile
    out-line						\ empty line
    s" \ spot edited by user:"  cat-and-out		\ comment
    spot @ num>string		cat2out
    s"  >spot! "		cat-and-out

    dup 1 and IF \ integer spot var change?
	field-i-planes# 1 ?DO		\ we leave pointers out
	    spot-before i cells + @
	    spot-after  i cells + @ <> IF
		spot-after  i cells + @ num>string	cat2out
		s"  "					cat2out
		i spot-var-name				cat2out
		s"  ! "					cat-and-out
	    THEN
	LOOP
    THEN

[ spot-floats# ] [IF]
    dup 2 and IF \ dfloat spot var change?
	spot-floats# 0 ?DO
	    spot-df-before i dfloats + df@
	    spot-df-after  i dfloats + df@  f- f0= 0= IF
		i n'th-spot-f-var-xt save-dfloat-variable
	    THEN
	LOOP
    THEN
[THEN]

    s" world-not-scanned"	cat-and-out
; IS ?record-edit-spot?

\ : ?record-feed-world ( -- )
:NONAME ( -- )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    out-line
    s" \ user interaction: feed-world"	cat-and-out	\ comment
    s" feed-world"			cat-and-out
; IS ?record-feed-world

\ : ?record-invert-selection ( -- )
:NONAME ( -- )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    out-line					\ empty line
    s" invert-selections"	cat-and-out
; IS ?record-invert-selection

\ : ?record-de-select-all ( -- )
:NONAME ( -- )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    out-line					\ empty line
    s" de-select-all-nucs"	cat-and-out
; IS ?record-de-select-all

\ : ?record-?do-everybody-generic ( maybe-do-field-xt -- maybe-do-field-xt )
:NONAME ( maybe-do-field-xt -- maybe-do-field-xt )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    out-line
    s" \ user invocated '|maybe-do-on-everybody-generic|'"  cat-and-out

    \ saving the field makes it more readable:
    dup save-maybe-do-field
    s" ' "				cat2out
    dup xt>string			cat2out
    s"  maybe-do-on-everybody-generic"	cat-and-out
; IS ?record-?do-everybody-generic

\ : ?record-?do-everywhere-generic ( maybe-do-field-xt -- maybe-do-field-xt )
:NONAME ( maybe-do-field-xt -- maybe-do-field-xt )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    out-line
    s" \ user invocated '|maybe-do-everywhere-generic|'"  cat-and-out

    \ saving the field makes it more readable:
    dup save-maybe-do-field
    s" ' "				cat2out
    dup xt>string			cat2out
    s"  maybe-do-everywhere-generic"	cat-and-out
    s"  present-initializes-future"	cat-and-out
; IS ?record-?do-everywhere-generic

\ : ?record-change-selections ( -- )
:NONAME ( -- )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    out-line
    s" \ user invocated 'do-change-selections'"	cat-and-out

    \ saving the field makes it more readable:
    ['] maybe-select-field			save-maybe-do-field

    s" do-change-selections"			cat-and-out
; IS ?record-change-selections

\ : ?record-do-on-selected-nucs ( -- )
:NONAME ( -- )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    out-line
    s" \ user invocated 'do-on-selected-nucs'"	cat-and-out

    \ saving the field makes it more readable:
    ['] maybe-do-on-selected-field		save-maybe-do-field

    s" do-on-selected-nucs"			cat-and-out
; IS ?record-do-on-selected-nucs

: big-bang-2out ( -- )
    out-line
    s" \ user invocated '|big-bang|'" cat-and-out
    
    (time-planes) @		num2out
    0  (dimensions) @ 1- DO
	i (dim-spots) @		num2out
    -1 +LOOP
    (dimensions) @		num2out
    s" (big-bang) 	\ "	cat2out
    world-name2@		cat-and-out ;

\ : ?record-big-bang ( -- )
:NONAME ( -- )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    big-bang-2out ; IS ?record-big-bang

\ : ?log-big-bang ( -- )
:NONAME ( -- )
    log-user? 0= IF EXIT THEN

    (record-file-id) @ >r
    ?open-log-file
    (log-file-id) @ set-outfile

    big-bang-2out

    r> set-outfile ; IS ?log-big-bang

:NONAME \ : ?record-world-name ( u -- )
    NOT-recording? IF  drop EXIT  THEN

    record-as-outfile
    world-name2@		out-buffered
    s"  dup string@  "		cat2out
    num>string			cat2out
    s"  n'th-world-name-2!  stringbuf-close"	cat-and-out
; IS ?record-world-name

:NONAME \ : ?record-remove-world ( -- )
    NOT-recording? IF EXIT THEN

    record-as-outfile
    out-line
    s" \ user invocated 'remove-world'" cat-and-out
    s" remove-world"			cat2out
    s" 	\ "				cat2out
    world-name2@			cat-and-out
; IS ?record-remove-world

:NONAME \ : ?record-remove-all-wolds ( -- )
    NOT-recording? IF  EXIT  THEN

    record-as-outfile
    out-line
    s" remove-all-worlds" cat-and-out
; IS ?record-remove-all-wolds

:NONAME \ : ?record-set-n'th-world ( u -- )
    NOT-recording? IF  drop EXIT  THEN

    record-as-outfile
    num>string			cat2out
    s"  set-n'th-world 	\ "	cat2out
    world-name2@		cat-and-out
; IS ?record-set-n'th-world

:NONAME \ ?record-clone-world-n ( u -- )
    NOT-recording? IF  drop EXIT  THEN

    record-as-outfile
    out-line
    num>string				cat2out
    s" 	clone-world-n 	\ clone "	cat2out
    world-string dup string@		cat-and-out  stringbuf-close
; IS ?record-clone-world-n

: before/after-file-name ( save-xt before-flag -- handle )	\ close buffer
    >r >r
    tmp-dir r> xt>string  file-name-cat
    r> IF
	s" -before"
    ELSE
	s" -after"
    THEN third cat
    indentity-string third cat
    s" .fs" third cat ;

: save-before/after ( save-xt before-flag -- )
    >r			( save-xt  r: before-flag )

    dup r> before/after-file-name >r	( save-xt  r: handle)
    r@ string@ r/w CREATE-tmp-FILE
    IF
	cr
	r@ string@ type
	true ABORT" save-before/after: Error opening file."
    THEN
    r> stringbuf-close		( save-xt wfileid )
    set-outfile
    write-diff!
    EXECUTE
    run-mode dup @ write-diff invert and swap ! ;

:NONAME \ : save-brew-before ( -- )
    ['] save-brew-variables true save-before/after ; IS save-brew-before

: save-brew-after ( -- )   ['] save-brew-variables false save-before/after ;

: save-nuc-before ( -- )   ['] save-nuc true save-before/after ;

: save-nuc-after ( -- )   ['] save-nuc false save-before/after ;

\  : save-actual-pool-before ( -- )
\      ['] save-actual-gene-pool true save-before/after ;

\  : save-actual-pool-after ( -- )
\      ['] save-actual-gene-pool false save-before/after ;

: remove-unique ( addr count -- addr count' )
    2dup unique-start search IF
	nip -
    ELSE 2drop THEN ;

VARIABLE (after)		(after) off
: read-line-after ( addr-after -- count-after line-read-flag )
    file-line-max#  (after) @	( addr-after max-length after-file-id )
    read-line			( count-after flag wior )
    ABORT" read-line-after: Read error." ;

VARIABLE (before)		(before) off	\ hold file id
: read-line-before ( addr-before -- count-before line-read-flag )
    file-line-max#  (before) @	( addr-before max-length before-file-id )
    read-line			( count-before flag wior )
    ABORT" read-line-before: Read error." ;

: block-begin? ( addr count -- xt TRUE | FALSE )
    over c@ [char] \ <> IF 2drop FALSE EXIT THEN
    dup 0= IF 2drop FALSE EXIT THEN		\ I had errors without that...

    [ begin-string nip ] literal >r
    over r@  begin-string  compare IF
	rdrop 2drop FALSE EXIT
    THEN
    swap  r@ +  swap  r> -  get-xt
    TRUE ;

: block-end? ( addr count -- xt TRUE | FALSE )
    over c@ [char] \ <> IF 2drop FALSE EXIT THEN

    [ end-string nip ] literal >r
    over r@  end-string  compare IF
	rdrop 2drop FALSE EXIT
    THEN
    swap  r@ +  swap  r> -  get-xt
    TRUE ;

: record-changes ( file-id save-xt -- )
    dup CASE
	['] save-brew-variables	OF
	    s" brew"
	ENDOF
	['] save-nuc		OF
	    s" nuc"
	ENDOF
	ABORT" record-changes: Unknown save function."
    ENDCASE
    dup stringbuf-open >r r@ cat r> -rot    ( scratch-handle file-id save-xt )

    dup false save-before/after	\ save present state to file

    \ allocate two buffers to keep two lines to compare
    file-line-max# allocate
    ABORT" record-changes: Couldn't allocate."
    file-line-max# allocate
    ABORT" record-changes: Couldn't allocate."
    2>r		( scratch-handle file-id save-xt  r: addr-after addr-before )

    \ open the files:
    dup true before/after-file-name >r
    r@ string@ r/o open-file
    IF
	cr r> string@ type
	true ABORT" record-changes: Error opening file."
    THEN
    (before) !
    r> stringbuf-close
    false before/after-file-name >r
    r@ string@ r/o open-file
    IF
	cr r> string@ type
	true ABORT" record-changes: Error opening file."
    THEN
    (after) !
    r> stringbuf-close

    ( file-id ) set-outfile

    (scratch) on	\ flag: add start comment
    BEGIN
	2r@		( h addr-before addr-after  r: addr-before addr-after)
	read-line-after	( h addr-before count-after read-flag  r: a-before a-a)
    WHILE
	r@ over block-begin? IF
	    nip
	    2r@ drop read-line-before
	    IF	\ before line read?
		2r@ drop swap block-begin? IF
		    2dup = IF			\ do block xt's match?
			drop nip
			2r@ rot EXECUTE
		    ELSE	\ block xt mismatch
			cr ." record-changes: can't handle this yet. ######## "
			true ABORT" record-changes: Block mismatch!"
		    THEN
		ELSE
		    cr ." record-changes: can't handle this yet. ######### "
		    true ABORT" record-changes: no before block"
		THEN
	    ELSE	\ no input line left
		cr ." record-changes: can't handle this yet. ########### "
		true ABORT" Input exhausted while reding a block."
	    THEN
	ELSE
	    >r		( h addr-before   r: addr-before a-after count-after )
	    dup read-line-before   ( h a-before count flag  r: bef aft count-a)
	    0= ABORT" record-changes: Error, maybe lines not balanced."
	    dup file-line-max# =
	    ABORT" record-changes: Increase 'file-line-max#'."
	    2r@ compare IF ( handle --   r: addr-before addr-after count-after)
		(scratch) @ IF	\ first time add start comment
		    out-line				\ empty line
		    s" \ changed "	cat2out		\ comment
		    dup string@		cat2out
		    s"  parameters:"	cat-and-out
		    (scratch) off			\ only first time
		THEN
		2r@ remove-unique cat2out
		?out-line
	    THEN rdrop
	THEN
    REPEAT  2drop

    r> free r> free or ABORT" record-changes: Couldn't free."
    stringbuf-close ;

\ Skip all lines until end of block mark (including) :
: (skip-block) ( addr max-len file-id -- )	\ file is r/o opened
    2>r				( addr  r: file-line-max# file-id )
    BEGIN			( addr  r: file-line-max# file-id )
	dup dup 2r@ read-line
	ABORT" (skip-block): Could not read-line."
    WHILE
	block-end? IF 2drop 2rdrop EXIT THEN
    REPEAT

    \ end of file reached before end mark
    2drop 2rdrop ;

: skip-block ( file-id -- )      \ file is r/o opened
    >r
    file-line-max# allocate ABORT" skip-block: Could not allocate."
    dup file-line-max# r> (skip-block)
    free ABORT" skip-block: Could not free." ;

\ Copy lines to '(outfile-id)' until an end of block mark (excluding):
: copy-block-until-end ( in-file-id -- )	\ file opened
    file-line-max# allocate ABORT" copy-block-until-end: Could not allocate."
    file-line-max# 2>r
    BEGIN
	2r@
	>r dup r> fourth read-line
	ABORT" copy-block-until-end: Could not read-line."
    WHILE
	2dup block-end? IF
	    drop 2drop TRUE
	ELSE
	    (outfile-id) @ write-line
	    ABORT" copy-block-until-end: Could not write-line."
	    FALSE
	THEN
    UNTIL
    ELSE
	true ABORT" copy-block-until-end: Input exhausted."
    THEN
    2r> drop free ABORT" copy-block-until-end: Could not free."
    drop ;

\ Read a line from file into the buffer and check if block end mark is reached.
\ Return line as string and a TRUE flag if it's a line in the block.
\ Return TRUE FALSE if the end of file is reached before the block ends.
\ Return xt of the word following the end mark and FALSE FALSE for block end.
\ Abort in case of file read errors.
: read-line-&-test ( stack comment see below )
    ( addr file-id -- addr count TRUE | EOF=TRUE FALSE | xt FALSE FALSE)
    >r dup file-line-max# r> read-line
    ABORT" read-line-&-test: Could not read-line."	\ read error, abort.

    0= IF drop TRUE FALSE EXIT THEN			\ EOF

    2dup block-end? IF
	-rot 2drop FALSE FALSE EXIT
    THEN

    TRUE ;

: read-before-&-test ( addr -- addr count TRUE | xt FALSE FALSE | TRUE FALSE )
    (before) @ read-line-&-test ;	\			  ^^^^=EOF

: read-after-&-test ( addr -- addr count TRUE | xt FALSE FALSE | TRUE FALSE )
    (after) @ read-line-&-test ;	\			 ^^^^=EOF

\ Compare lines from '(before)' and '(after)' files,
\ output differing lines (in the '(after)' version) to '(outfile-id)',
\ stop if both files reach the end mark, returning FALSE.
\ If '(after)' has more lines in the block, copy them out, return FALSE.
\ If '(before)' has more lines return the string of the first line that
\ was not treated and TRUE. *You have to free the buffer then!*
: diff-block-until-end ( -- FALSE | addr count TRUE )
    \ allocate two buffers to keep two lines to compare
    file-line-max# allocate
    ABORT" diff-block-until-end: Couldn't allocate."
    file-line-max# allocate
    ABORT" diff-block-until-end: Couldn't allocate."
    >r		( addr-after  r: addr-before )

    BEGIN	( addr-after  r: addr-before )
	dup read-after-&-test
    WHILE \ 'after' block line succesfully read in
	( addr-after addr-after count-after  r: addr-before )
	r@ read-before-&-test
	IF	\ 'before' line succesfully read in
	    2over compare IF
		cat2out ?out-line		\ write differing lines
	    ELSE 2drop THEN
	    FALSE	( addr-after FALSE  r: addr-before )	\ continue
	ELSE					\ 'before' no more block line
	    ABORT" diff-block-until-end: 'before' file EOF inside block."
	    TRUE
	    ( addr-after addr-after count-after before-xt TRUE  r: addr-before)
	THEN
    UNTIL \ 'before' end of file or end of block:
	( addr-after addr-after count-after before-xt  r: addr-before )
	-rot cat2out ?out-line			\ give out last 'after' line
	IF	\ end of 'before' block before block 'after' ended.
	    read-after-&-test IF	\ 'after' block longer
		(after) @ copy-block-until-end
	    ELSE
		ABORT" diff-block-until-end: 'after' EOF inside long block."
	    THEN
	ELSE	\ end of 'before' file before block 'after' ended.
	    true ABORT" diff-block-until-end: 'before' file EOF inside block."
	THEN
    ELSE	\ 'after' end of block or file:
	( addr-after { xt FALSE | TRUE }  r: addr-before )
	ABORT" diff-block-until-end: 'after' file EOF inside block."
	( addr-after xt  r: addr-before )
	drop
	r@ read-before-&-test IF		\ 'before' block longer
	    rdrop
	    rot free ABORT" diff-block-until-end: could not free short block."
	    TRUE EXIT
	ELSE
	    ABORT" diff-block-until-end: 'before' file EOF inside long block."
	    drop
	THEN
    THEN
    free  r> free  or ABORT" diff-block-until-end: could not free."
    FALSE ;

\ Free 2 buffers and reset file position in '(before)' and '(after)':
: reset-before&after ( d-position-before d-position-after addr1 addr2 -- )
    2 0 DO free ABORT" blocks-differ?: Could not free." LOOP
    (after) @ reposition-file
    ABORT" blocks-differ?: Couldn't reposition (after) file."
    (before) @ reposition-file
    ABORT" blocks-differ?: Couldn't reposition (before) file." ;

\ Read and compare lines from '(before)' and '(after)' files,
\ stop if both files reach the end mark, returning FALSE.
\ If a difference is found reset both file positions and return TRUE.
: blocks-differ? ( -- flag )
    \ remember file positions
    (before) @ file-position
    ABORT" blocks-differ?: Couldn't read '(before)' file position"
    (after) @ file-position
    ABORT" blocks-differ?: Couldn't read '(after)' file position"
    ( d-position-before d-position-after )

    \ allocate two buffers to keep two lines to compare
    file-line-max# allocate
    ABORT" blocks-differ?: Couldn't allocate."
    file-line-max# allocate
    ABORT" blocks-differ?: Couldn't allocate."
    >r		( ... addr-after  r: addr-before )

    BEGIN	( ... addr-after  r: addr-before )
	dup read-after-&-test
    WHILE \ 'after' block line succesfully read in
	( addr-after addr-after count-after  r: addr-before )
	r@ read-before-&-test
	IF	\ 'before' block line succesfully read in
	    compare IF			\ Block line differs!
		r> reset-before&after
		TRUE EXIT			\ Difference found, done.
	    THEN
	ELSE					\ 'before' no more block line
	    ABORT" blocks-differ?: 'before' file EOF inside block."
	    ( addr-after addr-after count-after before-xt  r: addr-before)
	    drop 2drop r> reset-before&after
	    TRUE EXIT				\ 'before' block ends earlier
	THEN
    REPEAT
    ABORT" blocks-differ?: 'after' file EOF inside block."
    ( ... addr-after xt-after  r: addr-before )
    r@ read-before-&-test
    IF	\ 'before' block is longer.
	2drop drop
	r> reset-before&after
	TRUE EXIT
    THEN
    ABORT" blocks-differ?: 'before' file EOF inside block."
    ( ... addr-after xt-after xt-before  r: addr-before )
    <> ABORT" blocks-differ?: end block type mismatch."
    r> 2 0 DO free ABORT" blocks-differ?: Could not free." LOOP
    ( d-file-position1 d-file-position2 ) 2drop 2drop
    FALSE ;

\ Compare two blocks. If there's a difference output the entire 'after'
\ block (without delimiters). 
: diff-block-as-unit ( addr-before addr-after -- )
    2drop
    blocks-differ? IF
	(after) @ copy-block-until-end
	(before) @ skip-block
	EXIT
    THEN ;


: diff-genome-pool ( addr-before addr-after -- )
    \ block starts are already read in
    2>r			( r: addr-before addr-after )

    \ treat starting line (after):
    s" \ set probabilities in genome-pool "	\ must start with this pattern
    r@ dup read-line-after ( a c addr-after count-after line-read-flag )
    0= ABORT" diff-genome-pool: Block after does not exist"
    2over 2over 2swap search
    0= ABORT" diff-genome-pool: Format error in 1st after line."
    2drop
    >r over + r> third -	\ name of genome pool (after) as string
    2swap		( addr-after-name count string-a str-cnt  r: a-b a-a )

    \ treat starting line (before):
    r@ dup read-line-before ( a c addr-before count-before line-read-flag )
    0= ABORT" diff-genome-pool: Block before does not exist"
    2over 2over 2swap search
    0= ABORT" diff-genome-pool: Format error in 1st before line."
    2drop
    >r over + r> third -	\ name of genome pool (before) as string
    2swap 2drop		( after-name-addr a-n-count before-addr b-count  r:...)

    \ compare genome pool name
    2over 2over compare IF	\ pools name has changed!
	(before) @ skip-block	\ skip 'before' block
	2drop			\ forget 'before' pool name

	\ Start output of 'after' state:
	s" \ set probabilities in genome-pool "	cat2out
	2dup					cat-and-out
	( addr count )				cat2out
	s"  nul-all-probabilities"		cat-and-out

	\ Copy 'after' pool
	(after) @ copy-block-until-end
	2rdrop EXIT
    THEN
    2drop 2drop

    \ Same genome pool as before:  Output diff lines until end of block.
    diff-block-until-end IF	\ 'before' block had more lines
	cr ." NOT IMPLEMENTED YET" DADA
    THEN
    2rdrop ;

: record-brew-changes ( file-id -- )
    ['] save-brew-variables record-changes ;

:NONAME \ ?record-brew-changes ( -- )
    recording? 0= IF  EXIT  THEN

    (record-file-id) @ record-brew-changes ; IS ?record-brew-changes

: record-nuc-changes ( file-id -- )
    ['] save-nuc record-changes
    s" nucs-not-scanned" cat-and-out ;

: nuc-changed? ( -- flag )
    tmp-dir s" nuc-changed" file-name-cat >r
    unique-identity-string  dup string@ r@ cat  stringbuf-close
    s" .fs" r@ cat
    r@ string@ r/w CREATE-tmp-FILE
    IF
	cr r@ string@ type
	true ABORT" nuc-changed?: Error opening file."
    THEN
    r> stringbuf-close
    dup record-nuc-changes
    dup file-size ABORT" nuc-changed?: Error with 'file-size'."
    or swap close-file ABORT" nuc-changed?: Error closing file."
    0<> ;


\ Words related to assert state of a recorded session or a benchmark:

\ This words are mainly for the creation of benchmarks.
\ Call assert state from record/playback menu for result validation of a
\ recorded session.  Or if you want to add it later, put 'assert-state-entry'
\ into the record file at the appropriate place and replace that by the
\ produced FORTH code to check state when playing it back.

\ Words that will be interpreted when playing back assert statements
\ created by 'assert-variable-entry' or 'assert-2variable-entry'.
\ As it get's the xt of the variable it can give error diagnostics.
: assert@= ( n xt -- flag )
    2dup EXECUTE @ = IF
	2drop TRUE
	EXIT
    THEN

    dup xt>string cr type
    ."  is " EXECUTE @ . ." instead of " .
    FALSE ;
	
: assert2@= ( d xt -- flag )
    >r
    2dup
    r@ EXECUTE 2@ d= IF
	2drop rdrop TRUE
	EXIT
    THEN

    r@ xt>string cr type
    ."  is " r> EXECUTE 2@ swap . . ." instead of " swap . .
    FALSE ;

: assert-variable-entry ( xt -- )
    dup EXECUTE @ num>string	cat2out
    s"  	' "		cat2out
    xt>string			cat2out
    s"  	assert@=  AND"	cat-and-out ;

: assert-2variable-entry ( xt -- )
    dup EXECUTE 2@
    swap num>string		cat2out
    bl				char2out
    num>string			cat2out
    s"  	' "		cat2out
    xt>string			cat2out
    s"  	assert2@=  AND"	cat-and-out ;

\ Word that will be interpreted when playing back assert statements
\ created by 'assert-function-entry'.  
\ As it get's the xt of the function it can give error diagnostics.
: assert-do= ( n do-xt -- flag )
    2dup EXECUTE = IF
	2drop TRUE
	EXIT
    THEN

    dup xt>string cr type
    ."  gives " EXECUTE . ." instead of " .
    FALSE ;

\ Make assert entry for a word giving one output oarameter:
: assert-function-entry ( xt -- )
    dup EXECUTE num>string	cat2out
    s" 	' "			cat2out
    xt>string			cat2out
    s" 	assert-do=  AND"	cat-and-out ;

\ Checksums over nucs and world:
\ Note that the checksums depend on cell size, but brew evolutions will
\ possibly do anyway because of integer overflows.

\ Add checksum over most nuc variables (including floats) of a nuc:
\ 'id' and 'genome-id' are excluded so you can start an evolution
\ at a different starting point.
\ 'length' is excluded so you can assert nucs with a different length.
\ Note that the whole length is scanned anyway but variables containing zero
\ have no effect.
: +nuc-checksum ( n -- n' )
    cp@ length @ +  nuc-checksum-start nuc-addr DO
	i @ +
    cell +LOOP ;

\ Give added checksum over all living nucs:
: nucs-checksum ( -- n )   0  ['] +nuc-checksum do-with-everybody ;

\ Build a checksum over all variables of the cells world.
\ : world-checksum ( -- n )		\ Defined in 'worlds.fs'.

\ Produce FORTH code to check the current state of brew.
\ When the code is run it assures the same state as when it was produced.
\ Used to check validity of benchmark results.
: assert-state-entry ( -- )
    page cr

    (outfile-id) @			( old-outfile-id )

    cr ." Saving code to assert current brew state to: "
    NOT-recording? IF
	file-names-length# stringbuf-open >r
	tmp-dir			r@ cat
	s" ASSERTIONS.fs"	r@ cat
	r@ string@ type cr
	r@ string@ w/o CREATE-FILE+  r> stringbuf-close
	IF
	    bell
	    cr ." assert-state-entry: Couldn't create file."
	    2000 ms
	    drop
	    EXIT
	THEN
	set-outfile
	true			\ flag: do close file when done
    ELSE
	(record-file-name) string@ type cr
	(record-file-id) @
	dup set-outfile
	record-brew-changes
	false			\ flag: do not close file
    THEN

    cr
    cr ." You can give a comment:" cr
    c-l stringbuf-open
    dup accept>stringbuf cr

    out-line
    s" \ assert-state-entry: Assert expected results:" cat-and-out
    s" true"	cat-and-out
    which-random-seed CASE		( old-id xt )
	1 OF assert-variable-entry ENDOF
	2 OF assert-2variable-entry ENDOF
	true ABORT" assert-state-entry: Error saving random seed."
    ENDCASE

    ['] step		assert-variable-entry
    ['] cloned		assert-variable-entry
    ['] living		assert-variable-entry
    ['] nuc-do-cost	assert-variable-entry
    ['] code-price	assert-variable-entry
    ['] (mutated-max)	assert-variable-entry
    ['] compiled-genes	assert-variable-entry

    ['] world-checksum	assert-function-entry

    ['] nucs-checksum	assert-function-entry

    s" cr"				cat-and-out
    dup string@ dup IF
	s" .( "				cat2out
	( addr count )			cat2out
	s" )"				cat-and-out
    ELSE 2drop THEN
    stringbuf-close
	
    s" [IF]"				cat-and-out
    s"     .( 	Result is valid. )"	cat-and-out
    s" [ELSE]"				cat-and-out
    s"     bell .( 	Unexpected result. Not comparable to other systems! )"
    cat-and-out
    s"     playing-bench? 0= [IF]"	cat-and-out
    s"         wait"			cat-and-out
    s"     [THEN]"			cat-and-out
    s" [THEN]"				cat-and-out
    out-line

    ( flag ) IF close-outfile THEN

    set-outfile
    1500 wait-until ;

\ Anton Ertl showed me that to be able to redirect output to /dev/null in
\ benchmarks we have to avoid 'at?' (which gets used in 'screen-column').
\ Type a string and two integers as n1/n2 and fill with spaces up to width:
\ (Works also with output redirection to a file).
: type./. ( addr count n1 n2 width -- )
    >r  swap 2>r
    string!!
    r> num>string third cat
    [char] /	  over char-cat
    r> num>string third cat
    dup buffered-length >r
    dup string@ type
    stringbuf-close
    2r> - spaces ;

\ Give information about versions and some compile options, i.e. for benchmarks
: .var-families ( -- )
    [ c-l 4 / ] literal >r
    cr
    s" organs int/f: "  nuc-organs# nuc-f-organs#	  r@ type./.
    s" parameters: "	nuc-parameters# nuc-f-parameters# r@ type./.
    s" invisibles: "	nuc-invisibles# nuc-f-invisibles# r@ type./.
    s" secrets: "	nuc-secrets# nuc-f-secrets#       r@ 1- type./.

    cr
    s" qualities:    "	spot-qualities# spot-f-qualities#   r@ type./.
    s" properties: "	spot-properties# spot-f-properties# r@ 2* type./.
    s" secrets: "	spot-secrets# spot-f-secrets#       r@ 1- type./.
    rdrop ;

\ Print version and compile-time infos in a form suited for benchmarks.
\ See '.version' for a menu like form variant.
: .brew-version ( -- )
    cr ." running on 'brew' version: "
    cr .tab brew-version type
    cr .tab genes-version type
    cr .tab mutation-version type
    cr .tab world/time-version type
    cr .tab world-spot-version type

    cr
    .var-families

    cr cr
    localise-spot-data IF
	." 'localise-spot-data' is TRUE "
    ELSE
	." 'localise-spot-data' is FALSE "
    THEN
    .tab ." spot-alignement#: " spot-alignement# .

    cr
    [ alternative-nuc-vars ] [IF]
	." 'alternative-nuc-vars' is TRUE "
    [ELSE]
	." 'alternative-nuc-vars' is FALSE "
    [THEN]
    cr

\     [ dummy-block-variables 0= ] [IF]
\ 	." USING block variables."
\     [ELSE]
\ 	[ dummy-block-variables 1 = ] [IF]
\ 	    ." using VARIABLEs defined one after the other as blocks."
\ 	[ELSE]
\ 	    ." using VARIABLEs defined scattered in source, no blocks."
\ 	[THEN]
\     [THEN]
\     cr
;

: insert-benchmark-header ( -- )
    out-line
    s" \ 'brew' benchmark file."		cat-and-out
    out-line
    s" \ please accomodate the following to your wishes:"
    cat-and-out
    s" \ especially look for all lines following a line like the next one:"
    cat-and-out
    ?uncomment
    out-line

    s" page"					cat-and-out
    s" cr .( 'brew' running benchmark )"	cat-and-out
    s" cr"					cat-and-out
    s" cr .( running on 'brew' version: )"	cat-and-out
    s" cr .tab brew-version type"		cat-and-out
    s" cr .tab genes-version type"		cat-and-out
    s" cr .tab mutation-version type"		cat-and-out
    s" cr"					cat-and-out
    out-line

    s" playing-bench!"				cat-and-out
    s" no-info-line on"				cat-and-out
    s" display-slots off"			cat-and-out
    s" display-switch off"			cat-and-out
    \ make it possible to include the bench from a menu (for tests):
    s" single-step off"				cat-and-out
    out-line ;

: out-tab-comment ( addr count -- )
    s" \ 	" cat2out
    ( addr count) cat-and-out ;

: insert-record-file-header ( -- )
    s" \ 'brew' recorded evolutionary session playback file."
    cat-and-out
    s" \"					cat-and-out
    s" \  created with brew version:"		cat-and-out
    brew-version				out-tab-comment
    genes-version				out-tab-comment
    mutation-version				out-tab-comment
    save-nuc-compile-options
    save-world-compile-options
    out-line ;

: record-on/off ( -- )			\ toggles recording
    run-mode
    dup @ recording xor over !

    @ recording and IF			\ start recording
	page cr

	base @ [ decimal ] 10 <> IF	\ switch to decimal
	    s" Sorry, recording only with decimal base.  Switched to decimal."
	    type-alert bell cr cr
	    decimal
	THEN

	." Start from scratch:"

	cr cr
	s" Please give record file name: " type-other-colour
	(record-file-name) >r
	r@ stringbuf-empty s" brew-recorded.fs" r@ cat		\ default
	r@ string@ type  bl emit
	r@ accept>stringbuf

	r> string@						\ rec-play-dir
	file-names-length# stringbuf-open >r
	rec-play-dir r@ cat r@ cat
	r@ string@  r/w  CREATE-FILE+				\ create file
	r> stringbuf-close
	IF bell  cr ." record-on/off: Couldn't create record file."
	    1000 ms  drop EXIT
	THEN
	dup set-outfile   (record-file-id) !

	making-bench? IF
	    insert-benchmark-header
	THEN

	insert-record-file-header

	out-line						\ empty line
	s" \ starting from scratch:"	cat-and-out		\ comment

	(remove-other-worlds)
	free-field						\ clear up

	\ start by recording all variables
	save-brew-variables					\ save all

	save-brew-before					\ remember all
	save-brew-after

	cr
	cr s" Now you should select an individual and set it to a spot."
	type-bright cr
	1300 wait-until
	individuals-menu
	selected-individual-xt @ ['] (none) <> IF
	    count-living 0= IF
		page cr
		s" Now go back to the main screen and set the individual."
		type-bright cr
		1300 wait-until
	    THEN
	THEN
\   ELSE				\ stop recording
    THEN ;

decimal
64 STRINGBUF-HANDLE: (playback-file-name)
32 STRINGBUF-HANDLE: (playback-file-short-name)
s" brew-recorded.fs" (playback-file-short-name) string!


VARIABLE (playback-file-id)

: play ( wfileid -- )
    base @ >r  decimal

    run-mode dup @ [ recording invert ] literal and swap !	\ just in case!

    dup (playback-file-id) !
    single-step off
    world-not-scanned
    nucs-not-scanned

    include-file

    world-not-scanned
    nucs-not-scanned
    single-step on

    r> base ! ;

use-fileselect [IF]
    INCLUDE fileselect.fs
[THEN]

\ Variable to check if a playback file has been played without interruption
\ and without errors to the end. If so it is possible to continue recording.
VARIABLE (just-played)		(just-played) off

: playback-on/off ( -- )
    (just-played) off
    run-mode dup @ playback xor swap !

    playback? IF				\ switch to playback

	recording? IF
	    run-mode dup @ recording xor swap !
	    cr ." Simultaneous recording and playback is not implemented."
	    cr ." Recording put off."
	THEN

[ use-fileselect ] [IF] \ use fileselect
	rec-play-dir open-directory 0= IF bell EXIT THEN
	s" Select playback file: " fileselect-menu
	close-current-dir
	( fileselect-menu-return-flag ) 0= IF
	    (playback-file-short-name) string@ 2dup nip IF
		rec-play-dir 2swap file-name-cat
		dup string@ (scratch-buf) string!
		stringbuf-close
		(scratch-buf) string@
		2dup cr ." taking " type
		1000 wait-until
	    ELSE
		bell 2drop EXIT
	    THEN
	THEN
	2dup (playback-file-name) string!

	\ Get playback name without path:
	2dup [char] / char-search-backwards IF
	    >r 2dup r>
	    1+ 2>r r@ + 2r> -
	ELSE
	    2dup
	THEN (playback-file-short-name) string!

	r/o OPEN-FILE
[ELSE] \ don't use fileselect
	cr
	cr ." Please give playback file name: "			\ file name
	(playback-file-short-name) >r				\ default
	r@ string@ type  bl emit
	r@ accept>stringbuf
	r@ string@ (playback-file-short-name) string!

	r> string@						\ rec-play-dir
	file-names-length# stringbuf-open >r
	rec-play-dir r@ cat r@ cat
	r@ string@
	2dup (playback-file-name) string!
	r/o  OPEN-FILE				\ open file
	r> stringbuf-close
[THEN]

	IF bell  cr ." playback-on/off: Couldn't open playback file."
	    1000 ms  drop EXIT
	THEN

	>r ( r: file-id )

[ TRUE ] [IF] \ catching play.  Switch this for debugging playback problems.
	r@ ['] play CATCH
	cursor-visible
	CASE
	    0 OF  (just-played) on  ENDOF		\ not interrupted

	    |playback-quit OF
		drop
		cr ." Playback interrupted."
		2000 ms
	    ENDOF

	    \ Default: let's say the user selected nonsence...
	    drop
	    bell cr ." Something went wrong with playing. "
	    2000 ms

	    page	\ debugging help.
	    ." error playing "
	    (playback-file-name) string@ type
	    .tab ." step " step @ . cr
	    cr
	    ." Should I do it again to show the error?  "
	    s" (This will probably crash!)" type-alert cr
	    ." y/n? "
	    key [char] y = IF
\ 		r@ CLOSE-FILE
\ 		IF bell  cr ." playback-on/off: Couldn't close playback file."
\ 		    1000 ms  \ drop
\ 		THEN

		clearstack	\ risky, but we have an error situation anyway.
		(playback-file-name) string@ r/w OPEN-FILE
		IF bell  cr ." playback-on/off: Couldn't open playback file."
		    1000 ms  drop rdrop EXIT
		THEN
		play
	    ELSE
		run-mode dup @ playback xor swap !	\ playback off
	    THEN
	ENDCASE

[ELSE] \ not catching play TO DEBUG ERRORS
	r@ play
	cursor-visible
[THEN]
    rdrop
    \ ELSE						\ playback off
    THEN ;

: make-a-benchmark ( -- )
    making-bench!
    record-on/off ;

: finish-benchmark ( -- )
    assert-state-entry
    record-as-outfile
    s" cr bye"	cat-and-out
    run-mode dup @ [ recording making-bench or invert ] literal and swap !

    cr
    s" Shall I append code for benchmark start from command line?  y/n "
    type-other-colour cr
    key CASE
	[char] y OF ENDOF
	[char] Y OF ENDOF
	drop EXIT
    ENDCASE

    ." OK, let us do it..." cr
    [ decimal ] 32 stringbuf-open >r	( r: benchmark-name-handle )
    (record-file-name) string@	( name-addr count  r: benchmark-name-h)
    2dup r@ cat

    cr
    dup IF
	dup BEGIN	( name-addr count actual-position r: benchmark-name-h )
	    1-
	    third over + c@ [char] . <>
	WHILE
	    dup 0=
	UNTIL
	    drop
	    2drop
	ELSE
	    nip
	    r@ string!
	THEN
    THEN
    <other-colour>  ." Give the benchmark name: " r@ string@ type cr
    reset-colours r@ accept>stringbuf
    cr r@ string@ type cr
    s" maybe-run-benchmark.fs" r/w open-file
    IF
	bell
	cr ." finish-benchmark: Could not open maybe-run-benchmark.fs" cr
	DROP
    ELSE			( file-id   r: benchmark-name-handle )
	c-l stringbuf-open	( file-id scratch-handle  r: bench-name-handle)
	s" [DEFINED] "			third string!
	r@ string@			third cat
	s"  [IF] INCLUDE benchmarks/"	third cat
	(record-file-name) string@	third cat
	s"  	[THEN]"			third cat
	cr ." I'm appending the following line to maybe-run-benchmark.fs" cr
	>r r@ string@
	2dup type cr
	third append-to-file
	close-file
	IF
	    bell
	    cr ." finish-benchmark: Couldn't close file."
	ELSE
	    cr ." I copy the file to the benchmark directory: " cr
	    s" benchmarks/"		r@ string!
	    (record-file-name) string@	r@ cat
	    r@ string@ type cr
	    rec-play-dir (record-file-name) string@ file-name-cat
	    dup string@ r@ string@ clone-file
	    stringbuf-close
	THEN
	r> stringbuf-close
    THEN
    r> stringbuf-close
    5000 wait-until ;

: record-comment ( -- )
    page
    cr cr
    s" Give a comment to be included in the record file: " type-other-colour cr
    cr

    c-l stringbuf-open
    dup accept>stringbuf
    dup buffered-length IF
	record-as-outfile
	out-line
	s" \ "		cat2out
	dup string@	cat-and-out
	out-line
    THEN

    stringbuf-close ;

: save-brew-recording ( -- )
    record-as-outfile
    save-brew-variables ;

: message-input ( -- )
    page
    cr cr
    s" Give a message to be displayed during playback: " type-other-colour  cr
    cr

    c-l stringbuf-open
    dup accept>stringbuf
    cr
    dup buffered-length IF
	cr
	s" For how many steps do you want the message to be displayed?"
	type-other-colour cr

	1 num-in over 0> and IF
	    over string@ third >message	\ recording time

	    record-as-outfile		\ playback time
	    out-line
	    s" : message-string-"	cat2out
	    (unique#) @ num>string	cat2out
	    s"  s"			cat2out
	    [char] "			char2out
	    bl				char2out
	    over string@		cat2out
	    [char] "			char2out
	    s"  ;"			cat-and-out
	    s" message-string-"		cat2out
	    unique-string		cat2out
	    bl				char2out
	    num>string			cat2out
	    s"  >message"		cat-and-out
	    out-line
	ELSE drop THEN
    THEN

    stringbuf-close ;    

\ Continue recording appending to a just played file:
: record-after-play ( -- )
    (playback-file-name) string@ r/w OPEN-FILE+
    IF bell  cr
	." record-after-play: Couldn't open file "
	(playback-file-name) string@ type bl emit
	1000 ms  drop EXIT
    THEN
    dup (record-file-id) !
    (playback-file-name) string@ (record-file-name) string!

    \ Adjust file r/w pointer:
    dup FILE-SIZE  ABORT" record-after-play: Could not determine file size."
    third REPOSITION-FILE
    ABORT" record-after-play: Could not 'reposition-file'."

    \ Insert comment:
    s" " third WRITE-LINE
    ABORT" record-after-play: Could not 'write-line'."
    s" \ Continued recording in brew session " string!! >r
    (identity) @ num>string r@ cat
    r@ string@ third WRITE-LINE
    ABORT" record-after-play: Could not 'write-line'."
    r> stringbuf-close
    dup FLUSH-FILE  ABORT" record-after-play: Could not 'flush-file'."
    set-outfile

    recording!
    save-brew-before					\ remember all
    save-brew-after

    insert-record-file-header

    0 at-x at? page at-xy
    s" OK, continue recording now." type-other-colour cr
    700 wait-until
    1 unnest-menus ;

MENU: rec/play-men
: .rec/play-menu ( -- )
    help-node" Record and Playback"
    s" Record and Playback menu:" menu-title-entry

    cr
    playback? 0= IF
	s" Record "	    ['] record-on/off	redisplay	menu-entry
	recording? dup .ON-off-entry-coloured
	s" rR" menu-same-key-entry
	( recording? ) IF
	    menu-highlite-on
	    1 4 screen-column
	    ." Record file: "
	    (record-file-name) string@ type
	    menu-highlite-off
	    s" oO" menu-same-key-entry
	ELSE
	    s"   Switching it on DELETES EVERYTHING and restarts from scratch!"
	    type-other-colour up-to-here
	THEN
	cr
    ELSE
	(just-played) @ IF			\ Only *just* after playback.
	    s" Record, appending to "	redisplay
	    ['] record-after-play	menu-entry
	    (playback-file-short-name) string@ type up-to-here cr
	    s" rRa" menu-same-key-entry
	    cr
	THEN
    THEN

    making-bench? IF
	recording? IF
	    <bright-colours>
	    s" Finish benchmark "	redisplay	do-after
	    ['] finish-benchmark	menu-entry cr
	    reset-colours
	THEN
    ELSE
	NOT-recording? playback? 0= and IF
	    cr
	    s" Record benchmark "	redisplay
	    ['] make-a-benchmark	menu-entry
	    s"   DELETES EVERYTHING and restarts from scratch!"
	    type-other-colour up-to-here
	    cr
	THEN
    THEN

    recording? IF
	<other-colour>

	cr
	s" go to brew main screen, recording."  ['] to-top-menu	 menu-entry cr

	menus-on-stack @ 1 > IF
	    cr
	    s" go back and do something, recording it all."
	    menu-done 	noop-entry cr
	THEN

	cr
	s" assert state (for benchmark creation)."	do-after
	redisplay   ['] assert-state-entry	menu-entry cr
	s" a" menu-same-key-entry

	cr
	s" save state (normally only diffs get saved)."		redisplay
	['] save-brew-recording		do-after		menu-entry cr
	s" s" menu-same-key-entry

	cr
	s" record comment " 	redisplay	do-after
	['] record-comment	menu-entry cr
	s" c" menu-same-key-entry

	cr
	s" message input " 	redisplay	do-after
	['] message-input	menu-entry cr
	s" m" menu-same-key-entry

	reset-colours
    THEN

    recording? 0= IF
	playback? 0= IF cr THEN					\ playback
	s" Playback "	['] playback-on/off	redisplay	menu-entry
	playback? .ON-off-entry-coloured
	s" pP" menu-same-key-entry
	playback? IF
	    s" oO" menu-same-key-entry
	    1 4 screen-column
	    ." playback file: "
	    (playback-file-name) string@ type
	ELSE
	    s"     (Play a recorded file)." type-other-colour
	THEN
	cr
    THEN

    s" iI"	redisplay	['] individuals-menu	menu-key-entry
    <common-menu-entries>
    run-mode dup @ [ playback invert ] literal and swap !
    (just-played) off ;		\ rigid but secure...

: rec/play-menu ( -- )
    menu-id @ nuc-menu-id = IF  bell EXIT  THEN		\ crash prone...
    (just-played) off

    rec/play-men
    ['] .rec/play-menu menu-display-xt !
    ['] .ok-done to-do-after-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    \ free-menus
    (just-played) off ;
' rec/play-menu function-key-actions >list


\ ****************************************************************
\ end	record and play



\ ****************************************************************
\ *************************  log-menu  ***************************
\ ****************************************************************

: .log-ON-off ( mask -- )  log-mask @ and .ON-off ;

: fix-log-flags ( -- )
    log-mask dup @	( log-mask-addr log-mask )

    [ log-mask @ 0> ] [IF] \ log extra information
	dup log-m-extra and IF
	    log-m-much or
	THEN
    [THEN]

    [ log-mask @ ] [IF]
	dup log-m-much and IF
	    log-m-more or
	THEN
	dup log-m-more and IF
	    log-m-some or
	THEN
    [THEN]

    dup log-m-some and IF
	log-m-type or
    THEN
    dup log-m-type and IF
	log-mutation or
    THEN

    swap ! ;

: log-state-entry ( -- )
    NOT-recording?		( NOT-recording? )
    run-mode dup @ 2>r		\ trick to get all entries...
    (outfile-id) dup @ 2>r	\ remember for restoring
    ?open-log-file
    (log-file-id) @ (outfile-id) !

    out-line
    s" \ logging current brew state:"	cat-and-out
    ( NOT-recording? ) IF
	s" \ (recording put on temporally)." cat-and-out
    THEN
\    ['] (id)		save-variable
\    ['] (genome-id)	save-variable
    save-brew-variables
    out-line

    page cr cr ." State logged in "
    (log-file-name) string@ type cr
    1000 wait-until

    r> r> !			\ restore (outfile-id)
    r> r> ! ;			\ restore run mode

: log-most! ( -- )
    [ -1
    [DEFINED] log-empty-spots	[IF]  log-empty-spots invert and	[THEN]
    [DEFINED] log-m-extra	[IF]  log-m-extra invert and		[THEN]
    ] literal  log-mask ! ;

MENU: log-men
: .log-menu ( -- )
    fix-log-flags

    help-node" Log files menu"
    s" Menu logging: Logs can grow huge, don't turn this on unless you really want to!" menu-title-entry

    [ log-mask @ ] [IF]	\ compiled with fine grain mutation debugging
	5 keep-but-scroll-rest
    [THEN]

    cr
    this-line	( start-line )
    log-masks
    dup nodes 0 [ log-mask @ ] [IF] scrolled-range [THEN]  ?DO
	i over n'th-node @ >r	( list  r: xt )
	r@ xt>string 4 /string			\ title for menu-entry
	r@ >stack				\ mask-xt >stack
	['] log-mask >stack-2
	redisplay	['] named-xor! menu-entry
	12 at-x r@ execute menu-highlite-on .log-ON-off up-to-here cr
	r> xt>string drop 4 + c@ #key-same-entry
    LOOP
    drop

    cr
    s" Log file: "	(log-file-id) >stack	(log-file-name) >stack-2
    redisplay		['] change-handled-file		menu-entry >last-xy
    (log-file-name) string@ .menu-expansion
    s" f" menu-same-key-entry

    .tab
    s" Writing code file: "	redisplay	['] code-file-menu menu-entry
    write-code-file .code-file-ON-off up-to-here
    s" wWc" menu-same-key-entry
    .tab
    s" Log state."	redisplay	['] log-state-entry	menu-entry cr
    s" lL" menu-same-key-entry
    at? 2>r

    ( start-line ) 0 swap at-xy 1 2 screen-column
    s" All  "	['] log-mask >stack	['] named-on	redisplay   menu-entry
    s" aA"	menu-same-key-entry
    s"  Most  " redisplay	['] log-most!			  menu-entry
    s" mM"	menu-same-key-entry
    s"  Nothing "  ['] log-mask >stack	['] named-off	redisplay menu-entry
    s" nN"	menu-same-key-entry
    s"  Some "	redisplay
    [ log-spot  log-birth or  log-death or  log-meal or  log-costs or
    log-trial or  log-emergency or  log-step or  log-user or  log-mutation or
    log-m-type or  log-m-some or ] literal >stack
    ['] log-mask >stack-2  ['] n-named! menu-entry
    s" S"	menu-same-key-entry

    2r> at-xy	\ position cursor
    <common-menu-entries> ;

: log-menu ( -- )
    log-men
    ['] .log-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' log-menu function-key-actions >list

\ ****************************************************************
\ end	log-menu



\ ****************************************************************
\ *************************  Menu elite  *************************
\ ****************************************************************

\ Build a world local score-list with the fittest individual in node 0:
: build-score-list ( -- )
    ?init-score-list
    ['] score-and-list do-with-everybody ;

MENU: elite-men
: .elite-menu ( -- )
    help-node" Menu elite"
    s" Menu elite:  " start-title-entry
    score-list @ nodes . ." individuals scored." end-title

    7 keep-but-scroll-rest

    cr
    s" rang" noop-entry
    6 80 screen-column
    s" genome" noop-entry
    24 80 screen-column
    s" score" noop-entry
    36 80 screen-column
    s" scoring" noop-entry
    48 80 screen-column
    s" code-tax" noop-entry
    60 80 screen-column
    s" genome-generation" noop-entry
    cr

    cr
    score-list @
    dup nodes 0 scrolled-range ?DO	( list )
	i over n'th-node		( list current-node )
	dup cell+ @ >spot!  fcp @ cp!	\ activate spot and nuc

	from-here
	i 1+ .					\ rang
	6 80 screen-column
	on-trial? IF				\ genome
	    genome-id @ . s" on trial"
	ELSE
	    wake-me-xt @ xt>string
	THEN
	cp@ >stack	['] |gene-edit-menu|	redisplay	menu-entry

	24 80 screen-column
	@ negate .		( list )	\ score ( negated in the list )
	36 80 screen-column
	scoring .				\ scoring
	48 80 screen-column
	code-tax .				\ code-tax
	60 80 screen-column
	genome-generation @ .			\ genome-generation
	up-to-here
	cr
    LOOP
    drop

    <common-menu-entries> ;

\ Test for a  score-list , that must have at least one node:
: score-list? ( -- flag=nodes )
    score-list @
    dup 0= IF  ( false )  EXIT  THEN	\ list does not even exist...
    nodes ;				\ nodes used as flag

: menu-elite ( -- )
    score-list? 0= IF
	build-score-list
    THEN

    elite-men
    ['] .elite-menu	menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop ;
' menu-elite function-key-actions >list

: show-elite-genome ( -- )
    score-list? 0= IF
	build-score-list
    THEN

    score-list @ >r
    r@ nodes IF
	0 r@ n'th-node cell+ @
	>spot!  fcp @ cp!

	gene-edit-menu
    ELSE bell THEN
    rdrop ;

\ ****************************************************************
\ end	menu elite



\ ****************************************************************
\ ***********************  System Menu:  *************************
\ ****************************************************************

: toggle-linear-mode ( -- )
    run-mode dup @ linear-mode xor swap !

    display-switch dup @
    world-mode? IF
	[ step-display-on invert ] literal and
	spot-display-on or
    ELSE
	[ spot-display-on invert ] literal and
	step-display-on or
   THEN
   swap ! ;

\ Print version and compile option info on a menu like screen.
\ See '.brew-version' for a variant suited for benchmarks and the like.
: .version ( -- )
    cursor-off
    page
    title-colors ." Info about versions and some compile time options:"
    end-title

    cr s" *  Brew version  *" type-bright
    cr .tab brew-version type

    cr
    cr s" *  Genes version  *" type-bright
    cr .tab genes-version type

    cr
    cr s" *  Mutation version  *" type-bright
    cr .tab mutation-version type

    cr
    cr s" *  World version  *" type-bright
    cr .tab world/time-version type
    cr .tab world-spot-version type
    cr .tab ." spot-alignement: "  spot-alignement# .
    cr

    cr
    s" *  Compiled with the following world structure  *" type-bright
[ spot-floats# ] [IF] ."     integer / dfloats"				[THEN]
    cr
    ." qualities: " spot-qualities# .
[ spot-f-qualities# ] [IF]  ." / " spot-f-qualities# .			[THEN]
    1 4 screen-column
    ." properties: " spot-properties# .
[ spot-f-properties# ] [IF]  ." / " spot-f-properties# .		[THEN]
    2 4 screen-column
    ." secrets:  " spot-secrets# .
[ spot-f-secrets# ] [IF]  ." / " spot-f-secrets# .			[THEN]
    cr

    cr
    s" *  Compiled with the following nuc structure  *" type-bright
[ nuc-floats# ] [IF] ."     integer / dfloats"				[THEN]
    cr
    ." organs: " nuc-organs# .
[ nuc-f-organs# ] [IF]  ." / " nuc-f-organs# .				[THEN]
    1 4 screen-column
    ." parameters: " nuc-parameters# .
[ nuc-f-parameters# ] [IF]  ." / " nuc-f-parameters# .			[THEN]
    2 4 screen-column
    ." invisible: " nuc-invisibles# .
[ nuc-f-invisibles# ] [IF]  ." / " nuc-f-invisibles# .			[THEN]
    3 4 screen-column
    ." secrets: " nuc-secrets# .
[ nuc-f-secrets# ] [IF]  ." / " nuc-f-secrets# .			[THEN]
    cr ;

: read-init-files ( -- )
    base @ >r  decimal
    s" brew-defaults.fs" INCLUDED
    free-field
    s" brew-init.fs" INCLUDED
    r> base ! ;

false [IF] \ unused
\ On selecting a README file the text can get displayed in pages by using ?.(
: ?.(						\ ( " text)"  -- )
    this-line last-line 1- = IF
	[ c-l 7 - ] literal at-x
	title-colors ." (more)" reset-colours
	wait page
    THEN

    POSTPONE .( ;
[THEN]

use-fileselect [IF]
: ?record|include-file| ( addr count -- )
    NOT-recording? IF 2drop EXIT THEN		\ only if we do record

    record-as-outfile
    out-line							\ empty line
    s" \ user did '|include-file|': "	cat-and-out		\ comment
    s" INCLUDE "			cat2out
    ( addr count )			cat2out
    bl					char2out out-line ;

: |include-file| ( -- )
    s" ." open-directory 0= IF bell EXIT THEN
    base @ >r  decimal

    inputs-dir open-directory 0= IF bell THEN

    s" Include file:    *at your own risk*" fileselect-menu
    IF
	2>r
	2r@ r/o open-file IF
	    bell cr ." |include-file|: Could not open file."
	    drop
	ELSE
	    page
	    >r
	    r@ ['] include-file CATCH
	    IF
		bell cr ." Error in included file."
	    THEN
	    rdrop	\    r> close-file IF bell THEN	\ gave errors later on!
	    cr s" File included. " type-other-colour  1200 wait-until
	    close-current-dir
	    2r@ ?record|include-file|
	    guess-scoring-function	\ hack
	THEN
	2rdrop
    THEN

    r> base ! ;
[THEN]


[DEFINED] COUNTING-WORDS [IF]
    INCLUDE profiling.fs
[THEN]

: toggle-documentation-type ( -- )		\ quick hack ###########
    manual-type dup @ CASE
	info-as-manual OF  html-as-manual  ENDOF
	html-as-manual OF  info-as-manual  ENDOF
    ENDCASE
    swap ! ;

: .docu-reader ( -- )
    page
    title-colors ." Information on how the documentation reader get's called:"
    end-title
    cr
    ." Documentation type is set to: "
    s" To invoque brew documentation the following string gets passed to '<system>':"
    manual-type @ CASE
	info-as-manual OF
	    ." info" cr
	    cr
	    type cr
	    call-info-string type cr
	    cr
	    ." Define 'call-info-string'"
	ENDOF
	html-as-manual OF
	    ." html" cr
	    cr
	    type cr
	    call-browser-string type cr
	    cr
	    ." Define 'call-browser-string'"
	ENDOF
    ENDCASE
    ."  as compile option in file 'my-compile-options'" cr
    ." if you want something else." cr
    cr
    ." Switch between info or html formats from system menu," cr
    ." Or edit file 'my-brew-options.fs'." cr

    cr cr
    ." The node would be determined by appending a string like:" cr
    manual-type @ CASE
	info-as-manual OF
	    [ decimal ] 32 stringbuf-open
	    dup cat-info-node-string
	    dup string@ type
	    stringbuf-close
	ENDOF
	html-as-manual OF
	    node-as-html type cr
	    cr
	    ." (Let me know if that does not work for you)."
	ENDOF
    ENDCASE
    cr ;
' .docu-reader IS <.docu-reader>

: |configure-console| ( -- )
    page
    cr
    ." Configure size of your console screen.  This application will exit brew."
    cr
    cr s" All session data will be lost." type-alert cr
    cr s" Proceed and then exit brew session y/n? " type-other-colour
    key CASE
	[char] y OF true ENDOF
	[char] Y OF true ENDOF
	false swap
    ENDCASE
    IF s" configure-console.fs" INCLUDED THEN ;		\ EXITS brew

MENU: system-men
: .system-menu ( -- )
    help-node" System menu"
    s" Brew system menu:" start-title-entry clear-line-to-end
    1 2 screen-column ." Running brew incarnation # " (identity) @ .
    end-title

    cr
    from-here ." Running in "
    world-mode? IF
	s" WORLD"
    ELSE
	s" LINEAR"
    THEN type-bright 
    s"  mode."	redisplay	['] toggle-linear-mode	menu-entry
    s" to" menu-same-key-entry

    3 10 screen-column
    s" Reset brew"	menu-done	['] read-init-files	menu-entry

    1 2 screen-column
    s" Brew version "  redisplay  menu-wait	['] .version	menu-entry
    s" v" menu-same-key-entry
    cr

    cr
    s" Choose random generator:" menu-title!
    ['] random-generators >stack	['] random-xt >stack-2
    s" Random generator: "		['] choose-xt-to-var	menu-entry
    s" rR" menu-same-key-entry
    .tab random-xt @ dup >stack	xt>string  ['] <page-see>	menu-entry cr
    
    random-xt @ CASE
	['] random-BRODIE OF
	    .tab .tab .tab
	    s" Random seed: "  ['] seed-BRODIE  simple-menu-entry-variable cr
	ENDOF
	['] random-generalized OF
	    .tab .tab .tab
	    s" Random seed:      "
	    ['] (random-generalized)  simple-menu-entry-variable cr
	    .tab .tab .tab
	    s" Random generator: "
		(random-generalized) cell+ simple-menu-entry-value cr
	ENDOF
    ENDCASE

    cr
    s" Can cells change qualities of the future?  "
    ['] future-quality-change >stack  ['] toggle-named  redisplay    menu-entry
    future-quality-change @ .YES-NO-entry cr

    world-mode? IF
	s" Cell division moves cell mother too?       "
	['] cell-division-moves-both >stack	redisplay
	['] toggle-named menu-entry
	cell-division-moves-both @ .YES-NO-entry cr
    THEN

    s" Cell division diversifies cell mother too? "
    ['] cell-division-diversify-both >stack	redisplay
    ['] toggle-named  menu-entry
    cell-division-diversify-both @ .YES-NO-entry cr
    
    s" Cell division mutates cell mother too?     "
    ['] cell-division-mutate-both >stack	redisplay
    ['] toggle-named  menu-entry
    cell-division-mutate-both @ .YES-NO-entry cr

    world-mode? IF
	cr
	s" Order to call the spots: "
	['] change-world-do-direction	redisplay	menu-entry
	menu-highlite-on
	world-do-direction @ CASE
	    0  OF
		s" randomly calling as many spots as there are. Slow."
	    ENDOF
	    1  OF
		s" sequential calling spots forwards. Fastest."
	    ENDOF
	    -1 OF
		s" sequential calling spots backwards. Slow."
	    ENDOF
	    2  OF
		s" alternate direction, starting forwards. Quite slow."
	    ENDOF
	    -2 OF
		s" alternate direction, starting backwards. Quite slow."
	    ENDOF
	ENDCASE
	.menu-expansion cr
    THEN

    cr
    recording? IF
	<other-colour>
	s" Recording disables saving variables." noop-entry
	reset-colours
    ELSE    
	s" Save brew variables "
	['] |save-brew-variables| 	redisplay	menu-entry
	s" s" menu-same-key-entry

	1 3 screen-column
	s" Save world"	redisplay	['] |save-world|	menu-entry

	2 3 screen-column
	s" Save all nucs"  redisplay	['] |save-all-nucs|	menu-entry
    THEN
    cr

[DEFINED] |include-file| [IF] \ uses 'fileselect-menu'
    s" Include file"	['] |include-file|	redisplay	menu-entry
    s" iI" menu-same-key-entry
[THEN]

    1 3 screen-column
    s" World scan"	['] spot-scan-menu	redisplay	menu-entry
    s" wW" menu-same-key-entry

    count-living IF
	2 3 screen-column
	s" Nuc scan" ['] nuc-scan-menu redisplay	menu-entry
	s" nN" menu-same-key-entry
    THEN
    cr

    cr
    l-s 25 > IF cr THEN
    s" Function keys" redisplay ['] function-key-menu	menu-entry
    s" fFK" menu-same-key-entry

[DEFINED] use-ekey [IF]
    1 3 screen-column
    s" Use ekey for key input: "	redisplay
    ['] use-ekey >stack	['] toggle-named		menu-entry
    use-ekey @ .ON-off up-to-here
    s" euU" menu-same-key-entry
[THEN]
    cr

    from-here ." Cursor "
    brew-at-xy-xt @ ['] at-xy-wrapping = IF  ." wraps" ELSE ." stops" THEN
    s"  at borders"	['] toggle-cursor-wrapping  redisplay	menu-entry cr

    l-s 25 > IF cr THEN
    s" Fix screen size"	['] |configure-console|	    redisplay	menu-entry
[DEFINED] calibrate-cur-esc-wait [IF]
    1 3 screen-column
    s" Fix cursor moving problems (beeping)"
    ['] calibrate-cur-esc-wait	redisplay	menu-entry
    s" bBcpm" menu-same-key-entry
[THEN]
    cr

    cr
    s" Manual"  redisplay	['] manual	menu-entry

    1 3 screen-column
    s" Select type of external documentation reader:" menu-title!
    s" Documentation type: "		redisplay
    ['] docu-types >stack	['] manual-type >stack-2
    ['] set-from-list					menu-entry
    s" dD" menu-same-key-entry
    manual-type @ CASE
	info-as-manual OF  ." info "  ENDOF
	html-as-manual OF  ." html "  ENDOF
    ENDCASE
    up-to-here
    ."  "
    s" reader"	redisplay  menu-wait	['] .docu-reader	menu-entry cr

[ [DEFINED] COUNTING-WORDS ] [IF]
    cr
    <other-colour>
    s" Write brew profile"		menu-wait
    ['] write-usage-profile		redisplay	menu-entry

    1 3 screen-column
    s" see VARs "			menu-wait
    ['] .variable-usage			redisplay	menu-entry

    1 2 screen-column
    s" COLONs "				menu-wait
    ['] .colon-usage			redisplay	menu-entry

    2 3 screen-column
    s" DOES "				menu-wait
    ['] .create-does>-usage		redisplay	menu-entry

    5 6 screen-column
    s" ALL "				menu-wait
    ['] .all-usage			redisplay	menu-entry cr

    reset-colours
[ELSE]
    l-s 25 > IF cr THEN
[THEN]

    cr
    s" QUIT brew"		['] |goodbye|			menu-entry
    s" Q" menu-same-key-entry
    cr

    <common-menu-entries> ;

: system-menu ( -- )
    system-men
    ['] .system-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    free-menus ;
' system-menu function-key-actions >list

\ ****************************************************************
\ end	system menu



\ ****************************************************************
\ ************************  menus-menu  **************************
\ ****************************************************************

MENU: menus-men
: .menus-menu ( -- )
    help-node" Menus menu"
    s" Menus menu:" menu-title-entry

    cr
    s" menu Individuals	"	['] individuals-menu		menu-entry
    s" iI"	menu-same-key-entry
    2 5 screen-column
    s" menu System	"	['] system-menu			menu-entry
    s" sS"	menu-same-key-entry
    3 4 screen-column
    s" menu World  "		['] world-menu			menu-entry cr
    s" wW"	menu-same-key-entry

    s" menu Worlds list "	['] world-list-menu		menu-entry
    2 5 screen-column
    s" menu global Variables "	['] menu-global-variables	menu-entry cr
    s" V"	menu-same-key-entry

    cr
    s" menu Mutation"		['] mutation-menu		menu-entry
    s" mM"	menu-same-key-entry
    2 5 screen-column
    s" menu Diversification "	['] diversification-menu	menu-entry cr
    s" d"	menu-same-key-entry
    s" menu Gene pools"		['] gene-pool-menu		menu-entry
    s" gG"	menu-same-key-entry
    2 5 screen-column
    s" menu Actual pool"	['] actual-pool-menu		menu-entry cr
    s" aA"	menu-same-key-entry
    s" menu Current Genomes "	['] menu-current-genomes	menu-entry cr
    s" uU"	menu-same-key-entry

    cr
    s" menu Population control"	['] menu-population-control	menu-entry
    s" pP"	menu-same-key-entry
    2 5 screen-column
    s" menu Food	"	['] food-menu			menu-entry
    s" fF"	menu-same-key-entry
    3 4 screen-column
    s" menu Elite	"	['] menu-elite	redisplay	menu-entry cr
    s" eE" menu-same-key-entry

    cr
    s" menu Scan nucs"		['] nuc-scan-menu		menu-entry
    s" nN"	menu-same-key-entry
    2 5 screen-column
    s" menu Scan world"		['] spot-scan-menu		menu-entry cr
\   s" w"	menu-same-key-entry
    s" menu Select nucs "	['] menu-select-nucs		menu-entry
    s" tT" menu-same-key-entry
    2 5 screen-column
    s" menu Nuc Subsets"	['] menu-nuc-subsets		menu-entry
    s" bB" menu-same-key-entry
    3 4 screen-column
    s" menu Spot Subsets"	['] menu-spot-subsets		menu-entry cr
    cr
    s" menu Display	"	['] display-menu		menu-entry
    s" D"	menu-same-key-entry
    2 5 screen-column
    s" menu Color	"	['] color-menu			menu-entry cr
    s" C"	menu-same-key-entry
    s" menu Step Display	" ['] menu-step-display		menu-entry
\   s" sS"	menu-same-key-entry
    2 5 screen-column
    s" menu Continuous Display	" ['] menu-continuous-display	menu-entry cr
\   s" c"	menu-same-key-entry

    cr
    s" menu Log files"		['] log-menu			menu-entry
    s" lLR"	menu-same-key-entry
    2 5 screen-column
    s" menu Code files"		['] code-file-menu		menu-entry cr
    s" c"	menu-same-key-entry

    cr
    s" menu Record Playback"	['] rec/play-menu		menu-entry cr
    s" rR"	menu-same-key-entry
    l-s 25 > IF cr THEN

    cr
    s" menu Function Keys"	['] function-key-menu		menu-entry cr
    s" kK"	menu-same-key-entry

    cr
    s" menu Demos	"	['] demo-menu	menu-done	menu-entry
    2 5 screen-column

    s" menu Big Bang"		['] big-bang-menu		menu-entry
    s" B"	menu-same-key-entry

    3 4 screen-column  s" Manual"  redisplay	['] manual	menu-entry cr

    <common-menu-entries> ;

: menus-menu ( -- )
    menus-men
    ['] .menus-menu menu-display-xt !
    menu-done	['] noop	menu-key-default
    menu-done	['] noop	menu-default
    do-menu-loop
    \ free-menus
;
' menus-menu function-key-actions >list
' menus-menu shift-F1-xt !

\ ****************************************************************
\ end	menus-menu



\ ****************************************************************
\ ***************************  Brew  *****************************
\ ****************************************************************

' |goodbye| function-key-actions >list

VARIABLE minimal-dictionary-space
10000 minimal-dictionary-space !
: sanity-checks ( -- )
    unused minimal-dictionary-space @ < IF
	s" Running out of dictionary space! (" 1 >message 
	unused num>string (message) cat
	s"  address units remaining)" (message) cat
	['] red message-fg-color-xt !
	single-step on
    THEN ;

\ brewing the gene soup: 'brew ( steps -- steps' )'
\ 'steps' counts down while brewing
\ starting with a positive value it does the given number of steps or less
\ starting with zero it counts the steps (downwards)
\ it stops,	when 'steps' reaches zero
\		when a key is pressed
\		or when the population dies out
: brew ( max-steps|0 -- steps-made )
    \ depth to (brew-depth)		\ easy stack control. insecure.
    dup >r

    ?step-display-sanity \ do this anyway, to set 'continuous-display-used'
    step-display-adapt-size
    (manually-selected-cell) off	\ deselect, could have died...
					\ must be done before '.info-line'

    display-switch @
    [ spot-display-on step-display-on or ] literal and IF
	cursor-off
	spot-display-on? IF	\ if the screen is not filled clear the screen
	    world-dimensions @ 2 < IF
		page
	    ELSE
		dimension-ranges
		dup @ c-l <			\ DADA MARK: scrolling ????
		swap cell+ @ l-s 1- <  or	\ DADA MARK: scrolling ????
		IF page THEN
	    THEN
	THEN
    THEN

    BEGIN ( steps )
	log-mask @ IF	\ checking logging (for speed reasons)
	    s" step " step @ log-step log-string-and-number
	THEN

	world-do
	sanity-checks

	display-switch @ 
	[ spot-display-on step-display-on or ] literal and 0= IF 
	    step @ snapshot-frequency @ and 0= IF
		spot-snapshots? IF
		    .world
		THEN
		step-snapshots? IF
		    step-display
		THEN
		cursor-off		\ quick cosmetic bug fix ;-)
	    THEN
	THEN

	\ display some infos
	.info-line

	elitism? 0= IF
	    population-control
	THEN

	depth brew-depth <> IF
	    bell 0 l-s 4 - at-xy ." stack violation! " .s ."    "
	    wait
	    do-FORTH
	THEN

	\ check if we're not done:
	1-  dup 0=				\ decrease counter, zero?
	single-step @ 0<> or			\ or single step?
	is-key? 0<> or				\ or key pressed?
	living @ 0= or				\ or no cells left!
    UNTIL					\ done? else repeat.

    r> swap - ;		( steps-made )


\ normally the display is shown while running the evolution
\ 'brew-redisplay' displays the world without brewing.
: ?set-cursor-after-redisplay ( -- )	\ factored out
    \ make: it possible to include rec files from outside of brew-menu
    menus-on-stack @ 0= IF EXIT THEN

    \ set cursor if spot display (only) is on:
    display-switch @
    dup step-display-on and IF drop EXIT THEN
    spot-display-on and IF
	menu-2-at 2@ at-xy		\ cursor reset to where it was before
    THEN ;

: (step-redisplay) ( -- )
    redisplaying!
    step-display
    run-mode dup @ [ redisplaying invert ] literal and swap ! ;

\ : (brew-redisplay) ( -- )	\ redisplay but no info line and cursor reset
:NONAME ( -- )
    spot-display-on? dup IF  .world  ELSE  page  THEN
    step-display-on? dup IF  (step-redisplay)  THEN
    or IF  EXIT  THEN

    step-snapshots? IF  (step-redisplay)  THEN
    spot-snapshots? IF  .world  THEN
; IS (brew-redisplay)

\ Redisplay with '.info-line' and cursor positioning.
: brew-redisplay ( -- )
    at?
    (brew-redisplay)
    .info-line
    at-xy
    ?set-cursor-after-redisplay
    cursor-visible ;

\ ****************************************************************
\ end	brew



\ ****************************************************************
\ **********************  Editing nucs:  *************************
\ ****************************************************************

: edit-nuc-this-spot ( -- )
    recording? IF
	save-nuc-before
    THEN

    (nuc-spot-known) on
    nuc-menu

    recording? IF
	(nuc-spot-known) @ 1 <> IF		\ only if not removed by user
	    nuc-changed? IF
		record-as-outfile

		out-line			\ empty line and comment
		s" \ changing nuc parameters of nuc at spot: " cat-and-out

		spot @ num>string		cat2out
		s"   >spot!"			cat-and-out

		s" fcp @ cp!"			cat-and-out
		(record-file-id) @ record-nuc-changes
	    THEN
	THEN
    THEN
    (nuc-spot-known) off ;

: clone-to-here ( cell-addr -- )
    cp!

    clone IF
	dup cp!			\ set actual				
	new-id id !		\ new id
	fcp !			\ put into field
	?increase-genome-probability

	recording? IF
	    record-as-outfile
	    out-line					\ empty line, comment
	    s" \ define a nuc and set to spot:" cat-and-out
	    save-nuc

	    out-line					\ empty line, comment
	    s" \ now set defined nuc to spot:"	cat-and-out
	    spot @ num>string			cat2out
	    s"  >spot!"				cat-and-out
	    s" |cp@| fcp !"			cat-and-out
	    s" ?increase-genome-probability"	cat-and-out
	    s" nucs-not-scanned"		cat-and-out
	    ?uncomment
	    s" brew-redisplay"			cat-and-out
	THEN

	brew-redisplay		\ show it
    THEN ;

\ ****************************************************************
\ end	editing nucs



\ ****************************************************************
\ *************************  Brew Menu  **************************
\ ****************************************************************

: mouse-in-field? ( x y k -- x y k false | spot k true )
   >r 2dup >r >r	( x y  r: k y x ) 
   xy>spot
   dup spots <
   IF           ( spot )
     rdrop rdrop
     r> true    ( spot k true )
   ELSE drop
     r> r>  r>  false
 THEN ;

\ Get start line of a step display item:
: step-display-line ( index -- start-line )
    (scan-index) @ >r			\ preserve (scan-index)

    0
    swap 0 ?DO			( lines-so-far )
	i (scan-index) !
	(scan-lines) @ +
    LOOP
    r> (scan-index) ! ;

\ Lower border of a slice in a ASCII bar display:
\ We have to compute backwards from the upper end, so that rounding errors
\ give the same pattern as in 'data2slice'.
: slice-border-int ( i -- n )
    (last-stat-range) @ 1+ >r	( i  r: limit )
    (last-stat-slices) @ swap - r@ (last-stat-slices) @ */  r> swap -
    (last-stat-min) @ + ;

: slice-border-float ( i -- F: r )
    s>f  (last-stat-range) df@  (last-stat-slices) @ s>f f/  f*
    (last-stat-min) df@ f+ ;

\ border slices in fixed ranges need another (expression-xt)
: set-border-expression ( -- )	\ for integer types
    (expression-xt) @ CASE
	['] variable-within	OF  ['] variable-number  ENDOF
	['] function-within	OF  ['] function-number  ENDOF

	cr bell ." set-border-expression: Unknown " xt>string type ABORT
    ENDCASE
    (expression-xt) ! ;


\ 'bar-ranged-subset' gets called if the user selects a bar in a scan
\ display and goes to the subset menu on the range in question.
\ The bar graphics must have been drawn as the last one for this to work.
\ column-low and column-high are both *inclusive* and in the right order
\ : bar-ranged-subset ( column-low column-high -- )

false [IF] \ unused
: upper-slice-border ( -- n )
    (last-stat-range) @ dup 1+ swap 1- (last-stat-slices) @ */
    (last-stat-min) @ + ;
[THEN]

: (bar-ranged-subset-int) ( column-low column-high expression-xt -- )
    (expression-xt) !
    (last-scanned-xt) @ (expr-xt-1) !
    ['] = (condition-xt) !

    swap		( column-high column-low )

    \ Compute low end:
    dup slice-border-int (expr-parameter) !

    \ Special case: lower end is lowest slice in fixed range:
    0= IF					\ lowest slice
	scan-horizontal-zoom? 0= IF		\ fixed range?
	    set-border-expression
	    ['] < (condition-xt) !
	    dup 1+ slice-border-int (expr-parameter) !
	ENDIF
    THEN

    \ Compute high end:
    1+					\ range border (exclusive)
    dup slice-border-int (expr-parameter-2) !

    \ Special cases: uppermost slice is top slice in fixed range:
    (last-stat-slices) @  = IF			\ last slice is top
	scan-horizontal-zoom? 0= IF		\ fixed range?
	    set-border-expression
	    -1 (expr-parameter) +!
	    ['] > (condition-xt) !
	ENDIF
    THEN ;

: bar-ranged-subset-int ( column-low column-high -- )
    ['] variable-within (bar-ranged-subset-int) ;

: bar-ranged-subset-int-funct ( column-low column-high -- )
    ['] function-within (bar-ranged-subset-int) ;


: set-border-expression-float ( -- )	\ dfloat types
    (expression-xt) @ CASE
	['] df-variable-within	OF  ['] df-variable-number  ENDOF
	['] df-function-within	OF  ['] df-function-number  ENDOF

	cr bell ." set-border-expression-float: Unknown " xt>string type ABORT
    ENDCASE
    (expression-xt) ! ;

: (bar-ranged-subset-dfloat) ( column-low column-high expression-xt -- )
    (expression-xt) !
    (last-scanned-xt) @ (expr-df-xt-1) !
    ['] = (condition-xt) !

    swap		( column-high column-low )

    \ Compute low end:
    dup slice-border-float (expr-df-parameter) df!

    \ Special case: lower end is lowest slice in fixed range:
    0= IF					\ lowest slice
	scan-horizontal-zoom? 0= IF		\ fixed range?
	    set-border-expression-float
	    ['] f< (condition-xt) !
	    dup 1+ slice-border-float (expr-df-parameter) df!
	ENDIF
    THEN

    \ Compute high end:
    1+					\ range border (exclusive)
    dup slice-border-float (expr-df-parameter-2) df!

    \ Special cases: uppermost slice is top slice in fixed range:
    (last-stat-slices) @  = IF			\ last slice is top
	scan-horizontal-zoom? 0= IF		\ fixed range?
	    set-border-expression-float
	    ['] f>= (condition-xt) !
	ENDIF
    THEN ;

: bar-ranged-subset-dfloat ( column-low column-high -- )
    ['] df-variable-within (bar-ranged-subset-dfloat) ;

\ : bar-ranged-subset-df-funct ( column-low column-high -- )
\     ['] df-function-within (bar-ranged-subset-dfloat) ;


: zero-range? ( -- flag )
    (last-stat-type) @ >r

    r@ is-int? IF
	rdrop
	(last-stat-range) @  0= IF  TRUE EXIT THEN
	FALSE EXIT
    THEN

    r@ is-dfloat? IF
	rdrop
	(last-stat-range) df@ f0= IF  TRUE EXIT THEN
	FALSE EXIT
    THEN

    r@ type-unknown% IF	\ this should not happen
	rdrop
	bell		\ so we nerve the user
	FALSE EXIT
    THEN

    cr ." zero-range?: Unknown variable type: "
    var-type-string type ABORT ;

\ : bar-ranged-subset ( column -- )
:NONAME ( column-low column-high -- )
    zero-range? IF  2drop EXIT  THEN

    (scan-locality) @ CASE
	nuc-local%  OF maybe-do-on-subset-field   ENDOF
	spot-local% OF maybe-do-spot-subset-field ENDOF

	cr bell ." bar-ranged-subset: Unknown '(scan-locality)' "
	locality-string type
	ABORT
    ENDCASE
    preserve-maybe-do-field >r

    ['] maybe-do (maybe-do-type-xt) !
    ['] noop (do-it-xt) !

    (last-stat-type) @
    dup is-int? IF
	drop
	(scan-xt) @  ['] nuc-scan-func-dspl = IF
	    bar-ranged-subset-int-funct
	ELSE
	    bar-ranged-subset-int
	THEN
    ELSE
	is-dfloat? IF
	    bar-ranged-subset-dfloat
	ELSE
	    cr bell ." bar-ranged-subset: Unknown data type "
	    dup var-type-string type
	    ABORT
	THEN
    THEN

    (scan-locality) @ CASE
	nuc-local%  OF  menu-nuc-subsets   ENDOF
	spot-local% OF  menu-spot-subsets  ENDOF

	cr bell ." bar-ranged-subset: Unknown locality "
	(scan-locality) @ locality-string type
	ABORT
    ENDCASE

    r> restore-maybe-do-field
; IS bar-ranged-subset

\ Manually selected scan range borders must *not* be equal
\ (other range errors are corrected elsewhere).
: change-int-scan-border ( addr -- )
    (last-scan-min-max) 2@ 2>r		\ preserve old values
    change-value-at-addr
    (last-scan-min-max) 2@ = IF		\ borders equal error?
	2r@ (last-scan-min-max) 2!	\ restore old values
	bell
    ELSE				\ ok
	(scan-flags) dup @ fixed-horizontal-range or swap !
    THEN
    2rdrop ;

: change-float-scan-border ( addr -- )
    \ preserve old values, just in case.
    (last-dfloat-check-data)  dup >dfloat-min df@  >dfloat-max df@
    
    change-df-value-at-addr

    \ check if borders are nor equal:
    (last-dfloat-check-data) dup >dfloat-min df@ >dfloat-max df@ f= IF
	\ borders equal error: restore old values
	(last-dfloat-check-data)  dup >dfloat-max df!  >dfloat-min df!
	bell
    ELSE
	(scan-flags) dup @ fixed-horizontal-range or swap !
	fdrop fdrop
    THEN ;

: scan-status-line-reaction ( column -- )
    dup scan-x < IF	 				\ lower border
	(last-stat-type) @ dup is-int? IF
	    2drop
	    (last-scan-min-max) >min change-int-scan-border EXIT
	ELSE
	    dup is-dfloat? IF
		2drop
		(last-dfloat-check-data) >dfloat-min change-float-scan-border
	    ELSE
		cr bell ." scan-status-line-reaction: Unknown "
		var-type-string type
		ABORT
	    THEN
	    EXIT
	THEN
    THEN

    dup c-l 2/ 5 - < IF					\ choose item
	(scan-index) @ choose-displayed-item
	drop EXIT
    THEN

    dup v-range-x < IF					\ zooming
	(scan-flags) dup @ fixed-horizontal-range xor swap !
	drop EXIT
    THEN

    c-l 12 - < IF					\ vertical range
	(vertical-display-range) change-value-at-addr EXIT
    THEN

    (last-stat-type) @ dup is-int? IF			\ upper border
	drop
	(last-scan-min-max) >max change-int-scan-border
    ELSE
	is-dfloat? IF
	    (last-dfloat-check-data) >dfloat-max change-float-scan-border
	ELSE
	    cr bell ." scan-status-line-reaction: Unknown "
	    var-type-string type
	    ABORT
	THEN
    THEN ;


\ defining rectangle range in spot display mode:
2VARIABLE (range-low)
2VARIABLE (range-high)
: spot-in-rectangle? ( -- flag )
    spot @ spot>xy
    (range-low) @  (range-high) @ 1+  WITHIN
    0= IF drop FALSE EXIT THEN

    (range-low) cell+ @  (range-high) cell+ @ 1+  WITHIN ;
' spot-in-rectangle?  simple-expressions-nuc >list
' spot-in-rectangle?  simple-expressions-spot >list

: rectangle>bg-col ( -- )
    spot-in-rectangle?
    IF color-selected-bg-xt ELSE color-miss-bg-xt THEN @ EXECUTE ;

2VARIABLE (bg-col-was)	\ background-color-xt @  spot-background-coloring?  2!
: define-rectangle ( -- )
    at? 0 mouse-in-field? nip IF
	drop
	at?
	defining-rectangle? IF		( x y )
	    \ which corner is closer?
	    (range-low) 2@  2over		( x y x-low y-low x y )
	    rot - dup *  -rot - dup * + >r	( x y   r: from-low^2)
	    (range-high) 2@  2over		( x y x-high y-high x y )
	    rot - dup *  -rot - dup * +
	    r> > IF
		(range-low)
	    ELSE
		(range-high)
	    THEN
	    2!
	ELSE
	    2dup (range-low) 2!	(range-high) 2!
	    background-color-xt @ spot-background-coloring? (bg-col-was) 2!
	    ['] rectangle>bg-col  background-color-xt !
	    spot-background-coloring!
	    defining-rectangle!
	THEN

	\ make sure 'low' means lower than 'high'
	(range-low) 2@  (range-high) 2@
	rot  2dup < IF swap THEN  (range-low) !  (range-high) !
	2dup < IF swap THEN  (range-low) cell+ !  (range-high) cell+ !

    ELSE 2drop THEN ;

: stop-defining-rectangle ( -- )    \ Switches run-mode only.
    \ World display rectangles must use define-rectangle-off instead.
    run-mode dup @ [ defining-rectangle invert ] literal and swap ! ;

: define-rectangle-off ( -- )
    (bg-col-was) 2@
    IF
	spot-background-coloring!
    ELSE
	display-switch
	dup @ [ spot-background-coloring invert ] literal and  swap ! 
    THEN
    background-color-xt !
    stop-defining-rectangle ;


: rectangle-subset ( -- )	\ Subset menu over the rectangle region
    maybe-do-on-subset-field
    preserve-maybe-do-field >r

    ['] maybe-do-simple (maybe-do-type-xt) !
    ['] noop (do-it-xt) !
    ['] spot-in-rectangle? (simple-expression-xt) !
    ['] = (condition-xt) !

    menu-nuc-subsets

    r> restore-maybe-do-field ;


\ Set (scan-index) scan index according the given screen line:
: line>step-item? ( line -- flag )
    step-display-items @ 0 ?DO	( resting-line )
	i (scan-index) !
	dup (scan-lines) @ < IF		\ within this items display region?
	    drop TRUE unloop EXIT
	ELSE
	    (scan-lines) @ -		\ skip lines
	THEN
    LOOP
    drop
    FALSE ;

\ defining bar slice range in scan displays:
\ VARIABLE (slice-range-low)	\ both *inclusive*
\ VARIABLE (slice-range-high)	\ both *inclusive*

\ Is the current step display item a bar graph?
: is-bar-graph? ( -- flag )
    (scan-xt) @ CASE
	['] nuc-scan-display	OF TRUE EXIT ENDOF
	['] spot-scan-display	OF TRUE EXIT ENDOF
	['] nuc-scan-func-dspl	OF TRUE EXIT ENDOF
    ENDCASE
    FALSE ;

: defining-bar-range-off ( -- )
    run-mode dup @ [ defining-bar-range invert ] literal and swap ! ;


\ define-bar-range  gets called if the user presses '*'
VARIABLE (index)	\ only for user interface/display logic
: define-bar-range ( -- )
    at?			( x y )
    \ Check if cursor is in a step display region
    line>step-item? 0= IF  drop bell defining-bar-range-off EXIT  THEN	( x )
    \ Check if the item is a bar graph
    is-bar-graph?   0= IF  drop bell defining-bar-range-off EXIT  THEN

    \ (scan-index) is set now	( x )
    defining-bar-range? IF	\ define other border

	\ Check if the ccursor is still in the same step display item
	(index) @ (scan-index) @ <> IF	\ wrong!
	    drop  bell  defining-bar-range-off  (step-redisplay) EXIT
	THEN

	\ Change border: which one is closer?
	(slice-range-low) @  over - abs
	(slice-range-high) @ third - abs
	< IF
	    (slice-range-low)
	ELSE
	    (slice-range-high)
	THEN
	!
    ELSE	\ Begin range definition: set first border (set both equal).
	dup (slice-range-low) !  (slice-range-high) !
	defining-bar-range!
	(scan-index) @  (index) !
    THEN

    \ Make sure 'low' means lower than 'high'
    (slice-range-low) @  (slice-range-high) @  > IF
	(slice-range-low) @  (slice-range-high) @
	(slice-range-low) !  (slice-range-high) !
    THEN

    \ Now display bar graph with coloured region
    at? 2>r					\ save cursor position
    0  (scan-index) @ step-display-line  at-xy	\ set cursor at start
    (scan-xt) @ EXECUTE				\ display scan with region
    2r> at-xy ;					\ restore cursor position


\ Call the right subset depending defining-bar-range, either a one bar subset,
\ or the one over the defined range
: x-bar-ranged-subset ( column -- )
    defining-bar-range? IF
	drop
	(slice-range-low) @  (slice-range-high) @
	defining-bar-range-off
    ELSE
	dup
    THEN
    bar-ranged-subset ;

\ Reaction on mouse or <RETURN> in step display:
: step-display-reaction ( x y k -- )
    >r				( column line  r: mouse-key )

    \ Determine display item:
    step-display-items @ 0 ?DO	( column resting-line  r: mouse-key )
	i (scan-index) !
	dup (scan-lines) @ < IF		\ within this items display region?
	    LEAVE
	ELSE
	    (scan-lines) @ -		\ skip lines
	THEN
    LOOP
    \	( column line-within-region  r: mouse-key )  \ (scan-index) is set now

    (scan-lines) @ swap - 1-	( column line-[upwards-counting]  r: mouse-key)

    (scan-xt) @ CASE				\ What type of item?
	['] continuous-display OF
	    menu-continuous-display
	ENDOF
	['] nuc-scan-display  OF
	    \ Display again to have all data:
	    at? 2>r					\ save cursor position
	    0  (scan-index) @ step-display-line  at-xy	\ set cursor
	    nuc-scan-display				\ display again
	    nuc-local% (scan-locality) !
	    2r> at-xy					\ restore cursor

	    dup IF	\ Cursor in bar graphics: 'menu-nuc-subsets' on range
		over x-bar-ranged-subset		\ subset menu
	    ELSE	\ Cursor on status line
		over scan-status-line-reaction
	    THEN
	ENDOF
	['] spot-scan-display OF
	    \ Display again to have all data:
	    at? 2>r					\ save cursor position
	    0  (scan-index) @ step-display-line  at-xy	\ set cursor
	    spot-scan-display				\ display again
	    spot-local% (scan-locality) !
	    2r> at-xy					\ restore cursor

	    dup IF	\ Cursor in bar graphics: 'menu-spot-subsets' on range
		over x-bar-ranged-subset
	    ELSE	\ Cursor on status line
		over scan-status-line-reaction
	    THEN
	ENDOF
	['] nuc-text-display  OF
	    (scan-index) @ choose-displayed-item
	ENDOF
	['] world-text-display OF
	    (scan-index) @ choose-displayed-item
	ENDOF
	['] nuc-scan-func-dspl OF
	    \ Display again to have all data:
	    at? 2>r					\ save cursor position
	    0  (scan-index) @ step-display-line  at-xy	\ set cursor
	    nuc-scan-func-dspl
	    nuc-local% (scan-locality) !
	    2r> at-xy					\ restore cursor

	    dup IF	\ Cursor in bar graphics: 'menu-nuc-subsets' on range
		over x-bar-ranged-subset			\ subset menu
	    ELSE	\ Cursor on status line
		over scan-status-line-reaction
	    THEN
	ENDOF

	\ not recognized:
	cr ." step-display-reaction: Unknown " xt>string type
	ABORT
    ENDCASE

    rdrop 2drop ;

\ mouse interaction on a screen after (or while) brewing
\ (or from 'brew-redisplay')
\ cheques info? step? spot? 
\ interactions outside range are treated as noop's
: react-on-mouse ( -- ) \ mouse reaction info-line nuc-scan-display spot
    mousek@		( x y k )

    over last-line = IF	\ on info line?
	drop 2drop	( -- )
	display-menu EXIT	\ allow change of display characteristics
    THEN

    \ step display overwrites spot display, so we check for step first:
    step-display-on?
    spot-display-on? 0= step-snapshots? and
    or IF				\ on step display?
	step-display-reaction
	brew-redisplay EXIT
    THEN

    spot-display-on?
    dup 0= spot-snapshots? and
    or IF				\ on spot display?
	defining-rectangle? IF		\ defining rectangle range?
	    drop 2drop
	    define-rectangle-off
	    rectangle-subset
	    brew-redisplay
	    EXIT
	THEN
	mouse-in-field? IF swap  ( k spot )	\ in the field?
	    >spot!
	    drop		\ mousek
	    fcp @ dup IF   			\ occupied?
		dup cp!				\ set as actual nuc
		(manually-selected-cell) !
		edit-nuc-this-spot			\ does recording too
	    ELSE					\ spot was free
		drop
		(manually-selected-cell) @ IF		\ a cell preselected?
		    (manually-selected-cell) @ clone-to-here	\ clone cell
		    nucs-not-scanned
		    ?increase-genome-probability
		ELSE
		    menu-edit-spot				\ look at spot
		    single-step on		\ don't make a step please
		THEN
	    THEN
	ELSE  ( x y k )		\ outside of the cell region (spot display)
	    drop 2drop
	THEN
	EXIT
    THEN

    \ Display was off:
    drop 2drop
    display-menu ;

\ see genome at current spot, or spot if empty
: ?|see-genome-or-spot| ( -- )
    at?  2dup menu-2-at 2!			\ so cursor will be restored
    0 mouse-in-field? nip IF
	dup someone-here? dup IF	( i addr|false )
	    nip
	    cp!
	    gene-edit-menu
	    menu-redisplay on
	    (manually-selected-cell) >r
	    r@ @ cp@ <> IF
		r@ off		\ would be rather confusing without that
	    THEN
	    rdrop
	ELSE	( i false )
	    drop
	    >spot!
	    menu-edit-spot
	THEN
    ELSE 2drop THEN ;

: |individuals-menu| ( -- )
    at?  2dup menu-2-at 2!			\ so cursor will be restored
    0 mouse-in-field? nip IF
	someone-here? dup IF	( addr|false )
	    dup cp!
	    (manually-selected-cell) !
	    ['] individual-on-spot selected-individual-xt !
	ELSE
	    drop
	    (manually-selected-cell) @ IF
		['] individual-on-spot selected-individual-xt !
	    THEN
	THEN

	individuals-menu

	['] selected-individual-xt @  ['] (none)  = IF
	    (manually-selected-cell) off
	THEN
	menu-redisplay on
    ELSE 2drop THEN ;

: >look-at ( xt -- )
    >stack	redisplay	['] look-at-xt >stack-2  ['] name-named! ;

\ Switch between real time display and snapshots:
\ Redisplay if appropriate.
\ (see: 'toggle-display-type' to switch real time spot/step display).
: switch-display-type ( -- )   (switch-display-type) IF (brew-redisplay) THEN ;

: .keybindings ( -- )
    page
    cursor-off

    title-colors
    ." Some main screen keybindings:"
    ."   Use  k  to see them all,  K  for function keys."
    end-title

    cr
    ." <SPC>	 stop'n go"
    1 3 screen-column
    ." s        step"
    2 3 screen-column
    ." l  edit genome or spot"
    cr
    ." <arrow>	 move to spot"
    1 3 screen-column
    ." <RETURN> select"
    2 3 screen-column
    ." !  FORTH interpreter"
    cr
    ." ?        manual"
    1 3 screen-column
    ." 0 1...   swich world"
    2 3 screen-column
    ." w / n   scan world / nuc's"
    cr

    cr
    title-colors
    ." Menus:  ( B m will port you to Brews Super Menu )" end-title

    cr
    ." M  Mutation menu"
    1 3 screen-column
    ." F  Food menu"
    2 3 screen-column
    ." P  Population control"
    cr
    ." D  Display menu"
    1 3 screen-column
    ." C  Color menu"
    2 3 screen-column
    ." I  Individuals menu"
    cr
    ." L  Logs menu"
    1 3 screen-column
    ." S  System menu"
    2 3 screen-column
    ." R  Record & Playback"
    cr
    ." G  Gene pool"
    1 3 screen-column
    ." A  Actual pool"
    2 3 screen-column
    ." K  Function Keys"
    cr
    ." W  World menu"
    1 3 screen-column
    ." O  Other worlds"
    2 3 screen-column
    ." U  Current genomes"
    cr cr
    title-colors ." Display:" end-title
    cr
    ." t  toggle step/spot"
    1 3 screen-column
    ." o  on/off (speedup)"
    2 3 screen-column
    ." r  redisplay"
    cr
    ." f  foreground color"
    1 3 screen-column
    ." b  background color"
    2 3 screen-column
    ." c  colors back&forg"
    cr

    cr
    title-colors ." Keys to change what to look up:" end-title
    cr

    ." a  age"
    1 3 screen-column
    ." g  generation"
    2 3 screen-column
    ." e  energy"
    cr

    ." y  ABC"
    1 3 screen-column
    ." |  ABC-X|"
    2 3 screen-column
    ." p  sign-organ-A"
    cr

    cr
    ." x  restart"
    1 3 screen-column
    ." d  demos"
    2 3 screen-column
    ." QX leave menu to FORTH"

    ?reset-continuous-column ;

: playback-interaction ( max-steps steps-made -- steps-to-make )
    0 0 at-xy
    ." Interrupted " dup . ." steps of " over .
    ."     'c' continue    'q' leave    "

    key CASE
	[char] c OF
	    -
	ENDOF
	[char] q OF
	    |playback-quit THROW
	ENDOF
	-	\ DADA
    ENDCASE ;


\ brew version used in playback to make user interaction possible:
: |brew| ( max-steps -- )
    >r		( r: max-steps )	\ keep data stack untouched
    r@ brew >r
    2r@ <> IF
	is-key? IF get-key drop THEN	\ drop key that interrupted brew
\	2r@ - RECURSE
	2r@ playback-interaction
	dup IF
	    RECURSE
	ELSE drop THEN
    THEN
    2rdrop ;

\ word called just after brewing to make record entry if appropriate
: ?record-brew ( steps -- )
    NOT-recording? IF drop EXIT THEN		\ only if we do record

    record-as-outfile
    out-line						\ empty line
    s" \ brew the gene soup until step "	cat2out	\ comment
    step @ num>string		cat-and-out

    num>string			cat2out
    s"  |brew|"			cat-and-out ;		\ brew

\ Reaction on 'q' key in brews main screen:
\ Stop any special mode if one is active,
\ else display user information about quitting brew
: .quit-brew-or-stop-modes ( -- )
    single-step on

    \ end clone mode:
    spot-display-on?  (manually-selected-cell) @ and IF
	(manually-selected-cell) off
	EXIT
    THEN

    \ normal case (no special mode was active):
    s" Brew main screen:    Leave to FORTH with 'Q'.      Press '?' for instant help."
    1 >message   ['] green message-fg-color-xt !
    single-step on ;

: ?switch-world ( world# -- )
    world# over = IF  drop EXIT  THEN

    page cr

    dup 0 worlds# within IF
	<other-colour>
	." Entering world# "  dup . cr  reset-colours  800 wait-until
	|set-n'th-world|
	EXIT
    THEN

    cr ." Sorry, world# " . ." does not exist." 
    cr
    cr s" (Create worlds from  Big Bang menu.)" type-other-colour cr
    1500 wait-until ;

: make-one-step ( -- )
    single-step on
    (brew-show-or-go) off ;	\ needed in very rare cases (after cloning).

: spots-visible? ( -- flag )	\ factored out for better structuring
    step-display-on? IF FALSE EXIT THEN
    spot-display-on? IF TRUE  EXIT THEN
    spot-snapshots? ;

\  VARIABLE (brew-show-or-go)	(brew-show-or-go) off
\  : brew-show-or-go ( -- )	(brew-show-or-go) on ;

MENU: brew-men
\ Menu for brews main display:
: .brew ( -- )
    \ the way things are set up here 'redisplay' lets brewing continue
    \ while 'do-after' just shows the world and the info line again
    help-node" Brew main screen"
    main-sceen-id menu-id !

    defining-rectangle? IF
	menu-selected >menu-xt @ ['] define-rectangle <> IF
	    define-rectangle-off
	    (brew-redisplay)
	THEN
    THEN

    defining-bar-range? IF
	menu-selected >menu-xt @ ['] define-bar-range <> IF
	    defining-bar-range-off
	    (brew-redisplay)
	THEN
    THEN

    s"  "   ['] single-step >stack	['] toggle-named
		redisplay		do-after-2		menu-key-entry
    s" s"   ['] make-one-step	redisplay			menu-key-entry
    s" XQ"  menu-done		['] noop			menu-key-entry
    s" x"   redisplay		['] |free-field|		menu-key-entry
    s" hH"	wait-n'go   	['] .keybindings
				do-after-2	redisplay	menu-key-entry
    s" !"	['] do-FORTH			do-after	menu-key-entry
    spots-visible? IF
	s" *"	['] define-rectangle do-after-2	redisplay	menu-key-entry
    ELSE
	s" *"	['] define-bar-range do-after-2	( redisplay )	menu-key-entry
    THEN
    s" r"	['] brew-redisplay				menu-key-entry
    spots-visible? IF
	s" l"	['] ?|see-genome-or-spot|	do-after-2	menu-key-entry
    ELSE
	s" l"	ping				['] noop	menu-key-entry
    THEN
    s" w"	['] spot-scan-menu 		do-after-2	menu-key-entry
    s" n"	['] nuc-scan-menu 		do-after-2	menu-key-entry
    s" o"	['] toggle-display-&-go		do-after-2	menu-key-entry
    s" t"	['] switch-display-type		redisplay	menu-key-entry
    s" v"	['] .version	menu-wait	do-after	menu-key-entry
\   s" ?"	['] start-help			do-after	menu-key-entry
    s" ?"	['] context-help		do-after	menu-key-entry

    10 0 DO	\ s" 0123456789"  switch to world n, if it exists.
	i [char] 0 +   i >stack	  do-after  ['] ?switch-world	#key-menu-entry
    LOOP

    s" L"	['] log-menu			do-after-2	menu-key-entry
    s" M"	['] mutation-menu		do-after-2	menu-key-entry
    s" BmS"	['] menus-menu			do-after-2	menu-key-entry
    s" E"	['] menu-elite			do-after-2	menu-key-entry
    s" F"	['] food-menu			do-after-2	menu-key-entry
    s" G"	['] gene-pool-menu		do-after-2	menu-key-entry
    s" A"	['] actual-pool-menu		do-after-2	menu-key-entry
    s" K"	['] function-key-menu		do-after-2	menu-key-entry
    s" O"	['] world-list-menu		do-after-2	menu-key-entry
    s" P"	['] menu-population-control	do-after-2	menu-key-entry
    s" R"	['] rec/play-menu		do-after-2	menu-key-entry
    s" Uu"	['] menu-current-genomes	do-after-2	menu-key-entry
    s" V"	['] menu-global-variables	do-after-2	menu-key-entry
    s" W"	['] world-menu			do-after-2	menu-key-entry
    s" G"	['] gene-pool-menu		do-after-2	menu-key-entry

    world-mode?  spot-display-on? or IF
	s" D"	['] display-menu		do-after-2	menu-key-entry
    ELSE
	s" D"	['] menu-step-display		do-after-2	menu-key-entry
    THEN

    s" C"	['] color-menu			do-after-2	menu-key-entry
    s" S"	['] system-menu			do-after-2	menu-key-entry
    s" T"	['] menu-select-nucs		do-after-2	menu-key-entry
    s" I"	['] |individuals-menu|		do-after-2	menu-key-entry

    s" c"   ['] toggle-colorizing do-after-2	redisplay	menu-key-entry
    s" b"   ['] toggle-background-colorizing
				do-after-2	redisplay	menu-key-entry
    s" f"   ['] toggle-foreground-colorizing
				do-after-2	redisplay	menu-key-entry
    s" d"			['] demo-menu	redisplay	menu-key-entry

    s" g"	['] show-generation  >look-at	do-after-2	menu-key-entry
    s" a"	['] show-age	>look-at	do-after-2	menu-key-entry
\   s" c"	['] show-ascii	>look-at	do-after-2	menu-key-entry
    elitism? IF
	s" e"	['] show-elite-genome	do-after-2  redisplay	menu-key-entry
    ELSE
	s" e"	['] show-energy	>look-at	do-after-2	menu-key-entry
    THEN
    s" k"	['] show-key-bindings  do-after-2  redisplay	menu-key-entry
    s" q"	['] .quit-brew-or-stop-modes 	redisplay	do-after-2
								menu-key-entry
    s" y"	['] |goodbye|	redisplay	do-after-2	menu-key-entry

    \ hack: adding key bindings if functions are defined by interpreting file:
    s" dynamic-key-bindings.fs" INCLUDED

    ['] do-after-2 default-function-keys

    do-not-cancel-keys on	\ otherwise the key will be cancelled in 'what'

    (brew-show-or-go) @ single-step @ and IF
	brew-redisplay		\ special: just show but don't make a live step
    ELSE			\ normal case: now we brew key? until
	log-user? IF
	    ?open-log-file
	    (log-file-id) @ record-brew-changes
	THEN
	recording? IF
	    (record-file-id) @ record-brew-changes
	THEN
	0 brew  ( steps )
	?record-brew
	save-brew-before	\ save variables before user interaction
    THEN

    (brew-show-or-go) off
    mid-screen			\ cursor in the middle of the screen
    cursor-visible ;

: brew-menu ( -- )
    single-step on
    start-help
    brew-men
    ['] .brew menu-display-xt !
    ['] noop  clear-screen-xt !
    ['] brew-redisplay to-do-after-xt !
    ['] brew-show-or-go to-do-after-2-xt !
    ['] react-on-mouse	do-after-2  menu-default
    ['] .keybindings  wait-n'go	redisplay do-after-2 menu-key-default
    do-menu-loop
    free-menus ;

\ ****************************************************************
\ end	brew menu



\ ****************************************************************
\ *********************  display-map-menu  ***********************
\ ****************************************************************

\ This simple menu is used to display a world map coloured on certain
\ criteria and give a menu interface to a few functions.

MENU: display-map-men
: .display-map-menu ( display-xt -- display-xt )
    at? 2>r
    page
    dup EXECUTE
    last-left s" " menu-done noop-entry  last-right up-to-here

    s" l"	redisplay	['] ?|see-genome-or-spot|	menu-key-entry
    s" c"	['] colour-condition >stack
    ['] toggle-named	menu-key-entry
    (common-menu-entries)		\ some basic functionality

    2r> at-xy ;

: map-display-reaction ( -- )
    mousek@ mouse-in-field? nip ( x y false | spot true )
    0= IF  2drop EXIT  THEN		\ not in the field, done.

    >spot!
    fcp @ dup IF   			\ occupied?
	cp!				\ set as actual nuc
	edit-nuc-this-spot		\ does recording too
    ELSE				\ spot was free
	drop
	menu-edit-spot			\ look at spot
    THEN ;

\ : display-map-menu ( display-xt -- )
:NONAME ( display-xt -- )
    colour-condition off \ switch setting current condition as colour condition
    display-map-men
    ['] .display-map-menu menu-display-xt !	\ display-xt will be on stack
    ['] noop clear-screen-xt !
    menu-done	['] noop	menu-key-default
    ['] map-display-reaction	menu-default
    mid-screen
    do-menu-loop

    drop ; IS display-map-menu

\ ****************************************************************
\ end   display-map-menu



\ ****************************************************************
\ ***********************  Start brew:  **************************
\ ****************************************************************

' diversify?-some is <diversify>

log-mask off	\ was compile time switch, now run time switch

\ ok, we start now:

old-bench-compatible-mode? [IF] \ EXPERIMENTAL condition
    big-bang
[ELSE]
    screen-sized-big-bang
[THEN]


' INCLUDE CATCH brew-defaults.fs
?dup [IF]
    dup -38 = [IF]
	drop
	cr .( brew-defaults.fs not present. )
    [ELSE]
	cr .( Error loading brew-defaults.fs )
	cr
	cr .( <press a key> ) cr key drop throw
    [THEN]
[THEN]

free-field
INCLUDE brew-init.fs

\ Run brew as a benchmark?  See 'maybe-run-benchmark.fs' for infos.
INCLUDE maybe-run-benchmark.fs 		\ Benchmarks do not return here.

' INCLUDE CATCH brew-options.fs
?dup [IF]
    dup -38 = [IF]
	drop
	cr .( brew-options.fs not present. )
    [ELSE]
	cr .( Error loading brew-options.fs )
	cr
	cr .( <press a key> ) cr key drop throw
    [THEN]
[THEN]


' INCLUDE CATCH my-brew-options.fs
?dup [IF]
    -38 <> [IF] bell
	cr .( Error loading my-brew-options.fs )
	cr
	cr .( <press a key> ) cr key drop throw
    [THEN]
[THEN]

simple-expressions-nuc  simple-expressions-all copy-simple-list-elements
simple-expressions-spot simple-expressions-all copy-simple-list-elements

guess-scoring-function	\ hack
save-brew-before	\ save variables before user interaction at startup

page
brew-menu
single-step off

\ ****************************************************************
\ end	start brew



\ ****************************************************************
\ ******************  Leaving brew to Forth:  ********************
\ ****************************************************************

page
cr .( You're in FORTH now.)
cr
cr .( 	Type 'bye' to leave.)
cr
cr .( 	To go back to brew type 'brew-menu'.)
cr .( 	All your brew data is still there.)
cr cr cr

\ ****************************************************************
\ end	leaving brew
