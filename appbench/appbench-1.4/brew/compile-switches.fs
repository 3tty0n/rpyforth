\ compile-switches.fs
\ 	$Id: compile-switches.fs,v 1.7 2003/08/27 18:14:10 f Exp $	

\ This file provides words for extended conditional compiling.
\ Words can be either compiled, disregarded,
\ or compiled in a run time condition (based on named bit vectors).
\ An alternative else-part can be compiled depending on a compile time or
\ a run time condition.

\ Please see the usage example at the end of this file.


\ ****************************************************************
\ LICENSE:

\ compile-switches.fs
\ This file was written as a part of 'brew',
\ an experiment with evolutionary programming written in Forth.

\ Copyright (C) 2002 by Robert Epprecht <epprecht@solnet.ch>

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


\ compile-switches.fs  provides words for conditional compiling.

\ Words can be switched to get either disregarded, compiled,
\ or compiled in a run time switchable condition,
\ depending on individual bits in switch masks.

\ It is possible to give an else-part which will alternatively get executed.

\ As the tool is designed to switch individual features it compiles *named*
\ words for each mask, test, setting and compile test for each feature.

\ Related individual features can get grouped
\ into separate named masks and lists.

\ See test at the botton for a usage example.

\ ****************************************************************
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" advanced.fs" REQUIRED

s" lists.fs" REQUIRED

[UNDEFINED] display-compiled-words [IF]
    \ Should some words automatically generating a word family display the
    \ names them when compiling?  Currently used by:
    \ * compile-listed-?-and-!
    \ * compile-listed-?-!-and-??
    false VALUE display-compiled-words
[THEN]

\ ****************************************************************


\ For logs and such I keep all the compile switches in a list:
LIST: compile-switches

\ Word to define triple cell variables to hold the bitmasks of a group
\ of switches:
\
\ There can be up to as many individual switches in one such group,
\ as there are bits in a cell:
\
\ The switche groups get listed in  compile-switches .
: SWITCH: ( "name<space>" -- )
    get-name
    CREATE
    dup string@ get-xt compile-switches >list
    stringbuf-close

    0 ,		\ compile time: main compile switches	run time: run switches
    0 ,		\ compile time: main compile switches	run time: save them
    0 ,	;	\ compile time: no-questions switches	run time: save them


\ Offset to the compile time masks:
\
\ Offset to the mask if a feature should be compiled at all:
: >compile-switch ( addr -- addr' )  cell+ ;
\
\ Offset to the mask if a feature should be compiled without a run time switch:
: >no-questions-sw ( addr -- addr' )  >compile-switch cell+ ;


\ Words to set individual switches:

VARIABLE (switch!-xt)
: set-switch ( mask switch-addr -- )   (switch!-xt) @ EXECUTE ;

\ Internal word to set a run time switch:
: (run-time!) ( mask switch-addr -- )   or! ;
\
\ Reset afte setting another (compile time) switch:
: (reset-switch!) ( -- )   ['] (run-time!) (switch!-xt) ! ;
(reset-switch!)

\ Internal words to set other switches: 
: (do-compile) ( mask switch-addr -- )
    2dup >compile-switch >r   r@ @ or r> !		\ set compile flag
    >no-questions-sw >r  r@ @ or r> !			\ set no-questions flag
    (reset-switch!) ;

: (don't-compile) ( mask switch-addr -- )
    >r invert	( inverted-mask  r: switch-addr )
    r@ >no-questions-sw >r  r@ @ over and r> !		\ clear no-questions
    r> >compile-switch >r    r@ @ and r> !		\ clear compile flag
    (reset-switch!) ;

: (compile-switchable)  ( mask switch-addr -- )
    2dup >compile-switch >r   r@ @ or r> !		\ set compile flag
    >no-questions-sw >r      invert r@ @ and r> !	\ clear no-questions
    (reset-switch!) ;

\ User interface words to set (the kind of) individual switches:
: do-compile ( -- )   ['] (do-compile)  (switch!-xt) ! ;
: don't-compile ( -- )   ['] (don't-compile)  (switch!-xt) ! ;
: compile-switchable ( -- )   ['] (compile-switchable)  (switch!-xt) ! ;


\ Internal word to prepare parameters for conditional compiling:
\ There can be three distinct (hairy) stack effects:
\ ( base-variable-xt mask-xt -- FALSE )		Do *not* compile.
\ ( base-variable-xt mask-xt -- TRUE TRUE )	Compile without run time switch
\ ( base-xt mask-xt  -- handle FALSE TRUE )	Compile run time switch,
\						give the name of the run time
\						test condition as a string.
: (xxx??) ( base-variable-xt mask-xt -- ... )
    >r
    EXECUTE			( base-variable-addr  r: mask-xt )
    dup >compile-switch @	( base-var-addr compile-switch  r: mask-xt )

    \ Test main compile switch:
    r@ EXECUTE >r	( base-var-addr compile-switch  r: mask-xt mask-value )
    r@ and 0= IF	( base-var-addr                 r: mask-xt mask-value )
	2rdrop drop
	FALSE  EXIT		\ Return ( ... -- FALSE )
    THEN

    \ Test no-questions-sw:
    >no-questions-sw @ r> and IF  ( -- r: mask-xt )
	rdrop
	TRUE TRUE  EXIT		\ Return ( ... -- TRUE TRUE )
    THEN

    \ Pass run time test code to the compiling definition:
    \ It has proven to be difficult to get the xt of the test,
    \ probably because this is used inside an immediate word.
    \ So I pass it's name in string buffer, which must be closed:
    r> xt>string string!! >r
    [char] ? r@ char-cat
    r> FALSE TRUE ;		\ Return ( ... -- handle FALSE FALSE )


\ Compile three named words for each listed mask:
\
\ 'xxx!' setting the bit:
\ These words  can work on the different compile switches too, if one of the
\ following words preceeds it:
\ * do-compile
\ * don't-compile
\ * compile-switchable
\
\ 'xxx?' asking if the bit is set in the run time switch.
\
\ 'xxx??' preparing parameters for ??COMPILE'
\ See  (xxx??) .
: compile-listed-?-!-and-?? ( base-variable-xt list -- )
    >r		( base-variable-xt  r: list )

[ display-compiled-words ] [IF]
    cr ." compile-listed-?-!-and-?? defining: "
    cr
[THEN]

    [ decimal ]
    32 stringbuf-open
    256 stringbuf-open

    r> dup nodes 0 ?DO	( variable-xt handle-name hndl-evaluation actual-node )
	next-node

	\ define 'xxx!' words ( -- )
	s" : " fourth string!
	third >r
	dup @ xt>string		r@ string!
	[char] !		r@ char-cat	\ 'xxx! as name'
	r> string@
[ display-compiled-words ] [IF]
	2dup type	bl emit			\ say what you're doing
[THEN]
	fourth 			cat
	bl			third char-cat
	third string@ 1-	fourth cat
	bl			third char-cat
	fourth xt>string	fourth cat
	s"  set-switch ;"	fourth cat	\ evaluation string ok
	over string@ EVALUATE			\ compile 'xxx!'

	\ define 'xxx?' words ( -- flag )
	s" : " fourth string!
	third >r
	dup @ xt>string		r@ string!
	[char] ?		r@ char-cat	\ 'xxx?' as name
	r> string@
[ display-compiled-words ] [IF]
	2dup type	bl emit			\ say what you're doing
[THEN]
	fourth 			cat
	bl			third char-cat
	fourth xt>string	fourth cat
	s"  @ "			fourth cat
	third string@ 1-	fourth cat
	s"  and 0<> ; "		fourth cat	\ evaluation string ok
	over string@ EVALUATE			\ compile 'xxx?'

	\ define 'xxx??' words
	s" : " fourth string!
	third >r
	dup @ xt>string		r@ string!
	s" ??"			r@ cat		\ 'xxx??' as name
	r> string@
[ display-compiled-words ] [IF]
	2dup type	bl emit			\ say what you're doing
[THEN]
	fourth 			cat
	s"  ['] "		fourth cat
	fourth xt>string	fourth cat	\ base variable
	s"  ['] "		fourth cat
	dup @ xt>string		fourth cat	\ mask
	s"  (xxx??) ; IMMEDIATE" fourth cat
	over string@ EVALUATE

    LOOP

[ display-compiled-words ] [IF]
    cr						\ see: say what you're doing
[THEN]

    drop
    stringbuf-close
    stringbuf-close
    drop ;

\ Word to compile the next word conditionally (immediate):
\ It expects the stack parameters in the format as the name??  type words give.
: ??COMPILE'	\ stack comments see below, see  (xxx??) .
    ( "word<space>" false -- )			\ disregard next word.
    ( "word<space>" ... true -- )		\ compile word as shown below:
    ( "word<space>" true true -- )		\ compile word unconditionally
    ( "word<space>" handle false true -- )	\ compile word conditionally:
						\ condition IF word THEN
    ' >r

    \ Deal with topmost flag: 'compile'
    ( "word<space>" false -- )		\ disregard next word.
    0= IF  rdrop EXIT  THEN		\ don't compile 'word'

    \ Deal with second flag: 'no-questions'
    ( "word<space>" true true -- )	\ compile word unconditionally
    IF	r> COMPILE,  EXIT  THEN		\ compile 'world' unconditionally

    \ If both flags are false: compile runtime switchable:
    ( condition-handle   r: action-xt )
    dup string@ EVALUATE	\ compile condition
    stringbuf-close
    POSTPONE IF
	r> COMPILE,
    POSTPONE THEN
; IMMEDIATE

\ Word to compile the next two words conditionally (immediate):
\ Expects the stack parameters in the format as the name??  type words give.
\ The first used word get's compiled as true-part, the second word (else-part)
\ will get executed whenever the first is not. The condition can be evaluated
\ at run-time or at compile-time depending on the flags.
: ??COMPILE'-ELSE'	\ stack comments see below, see  (xxx??) .
    ( "word1<space>word2<space>" false -- )
    \ disregard word1 but compile word2

    ( "word1<space>word2<space>" true true -- )
    \ compile word1, disregard word2.

    ( "word1<space>word2<space>" handle false true -- )
    \ Compile the phrase 'condition? IF word1 ELSE word2 THEN'.
    \ Either word1 or word2 will be executed based on the run time condition
    \ given as a Forth code string handled in a string-buffer.

    ' ' >r >r	( else-part-xt true-part-xt )

    \ Deal with topmost flag: 'compile'
    ( "word1<space>word2<space>" false -- )	\ compile else-part only
    0= IF
	rdrop  r> COMPILE,  EXIT
    THEN

    \ Deal with second flag: 'no-questions'
    ( "word1<space>word2<space>" true true -- )	\ compile word1 unconditionally
    IF	r> COMPILE,  rdrop EXIT  THEN		\ compile 'world1' only

    \ If both flags are false: compile runtime switchable:
    ( condition-handle   r:  else-part-xt true-part-xt )
    dup string@ EVALUATE	\ compile condition
    stringbuf-close
    POSTPONE IF
	r> COMPILE,
    POSTPONE ELSE
	r> COMPILE,
    POSTPONE THEN
; IMMEDIATE


false [IF] \ debugging tools

: dump-sw ( switch-addr -- )    cr  3 cells dump ;

s" display.fs" REQUIRED		\ define  .tab

: .switches ( switch-variable-xt list -- )
    cr  over xt>string type
    ."  run time switch value: "
    swap EXECUTE swap		( switch-variable-addr list -- )
    over @ .
    over dump-sw

    cr
    dup nodes 0 ?DO
	next-node
	dup @
	dup EXECUTE >r		( var-addr current-node mask-xt  r: mask )
	r@ . .tab
	xt>string type .tab	( var-addr current-node  r: mask )

	\ Run time switch:  relevant or not?
	over >compile-switch @  third >no-questions-sw @ invert and r@ and
	0= IF  ." ("  ELSE  bl emit  THEN
	over @			( var-addr node switch-value  r: mask )
	r@ and IF		( var-addr node  r: mask )
	    ." ON"
	ELSE
	    ." off"
	THEN
	over >compile-switch @  third >no-questions-sw @ invert and r@ and
	0= IF  ." )"  THEN
	.tab

	over >compile-switch @ r@ and IF
	    ." COMPILE" .tab .tab
	    over >no-questions-sw @ r@ and IF
		." do ALWAYS, no questions."
	    ELSE
		." SWITCH at run time."
	    THEN .tab
	ELSE
	    ." NOT compiled"
	THEN
	rdrop
	cr
    LOOP
    2drop ;


false [IF] \ Test and usage example (uses the debugging tools from above).
    page cr .( Testing compile time switches: )

    \ Define a triple variable for the bitmasks:
    SWITCH: switches

    \ Define list for the xt's:
    LIST: masks

    \ Define individual named switches:
    s" listed-masks.fs" REQUIRED

    masks 0
    LISTED-MASK: switch-0
    LISTED-MASK: switch-1
    LISTED-MASK: switch-2   
    2drop

    cr
    \ Now compile all the named words to set, test or compile each switch:
    ' switches masks compile-listed-?-!-and-??

    \ switch run time switches on:
    \ switch-0!
    \ switch-1!
    switch-2!
    \ switches dump-sw

    \ set compile time switches
    don't-compile switch-0!
    do-compile switch-1!
    compile-switchable switch-2!
    \ switches dump-sw

    \ just for the test:
    : .sw ( -- )   ['] switches masks .switches ;
    .sw

    \ Dummy actions to be compiled into 'test-definition':
    : don't-do-that	cr ." I should not do that! " ;
    : do-that		cr ." I do what I must. " ;
    : do-if-you-want-to	cr ." Do it or leave it... " ;
    : do-something-else cr ." Let's do something else." ;

\ Compile test definitions:
: test-definition
    switch-0?? ??COMPILE' don't-do-that
    switch-1?? ??COMPILE' do-that
    switch-2?? ??COMPILE' do-if-you-want-to ;
SEE TEST-DEFINITION
\ cr test-definition

: test-if-else-definition
    switch-0?? ??COMPILE'-ELSE' don't-do-that do-something-else
    switch-1?? ??COMPILE'-ELSE' do-that do-something-else
    switch-2?? ??COMPILE'-ELSE' do-if-you-want-to do-something-else ;
cr SEE TEST-IF-ELSE-DEFINITION
\ cr test-if-else-definition

\ switches dup @ switch-2 invert and swap !	\ switch-2 switched off now:
\ cr test-if-else-definition

QUIT

[THEN] \ test and usage example
[THEN] \ debugging tools
