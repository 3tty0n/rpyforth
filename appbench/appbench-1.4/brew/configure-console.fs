\ configure-console.fs
\ 	$Id: configure-console.fs,v 1.2 2005/04/18 18:43:15 f Exp $	

\ Helper application to determine and configure console screen size for brew.

\ ****************************************************************
\ Please do use a *text console* to run brew.
\ Configure it's size with this program.
\ Let it run on the same text console you want to use for brew.
\ ****************************************************************

\ ****************************************************************
s" system-dependent.fs" INCLUDED
s" basics.fs" INCLUDED
\ ****************************************************************

page
decimal



\ ****************************************************************
\ I/O words:

\ 
: TMP-file-name ( -- addr count )   s" OUTPUT/tmp/configure-console.OUT" ;

TMP-file-name r/w CREATE-FILE
[IF]
    bell
    cr .( configure-console.fs: )
    cr .( Couldn't create-file: ) TMP-file-name type
    cr
    cr .( Maybe make directory, or change path in configure-console.fs )
    cr
    BYE
[THEN]

CONSTANT outfile-id
false VALUE output?	\ was there output?

: out ( addr count -- )
    outfile-id write-file THROW
    outfile-id FLUSH-FILE drop
    true to output? ;

: out-line ( addr count -- )
    outfile-id write-line THROW
    outfile-id FLUSH-FILE drop
    true to output? ;

: cleanup-files ( -- )
    outfile-id close-file drop
    TMP-file-name delete-file drop
    s" TMP-scr-conf.OUT" file-exists? IF
	s" rm TMP-scr-conf.OUT"  <system> drop
    THEN ;

: yes-no? ( addr count -- yes-no-flag )
    BEGIN
	2dup type ."  y/n? "
	key CASE
	    [char] y OF true  true ENDOF
	    [char] Y OF true  true ENDOF
	    [char] n OF false true ENDOF
	    [char] N OF false true ENDOF
	    false swap
	ENDCASE
	dup false = IF
	    bell cr ." Please type 'y' or 'n'. " cr
	THEN
    UNTIL
    nip nip ;


\ ****************************************************************
\ Words to get information with 'tput parameter'
\ see man terminfo(5)

: tput-working? ( -- flag )
    s" tput cols > TMP-scr-conf.OUT 2>&1" <system> 0= ;

: tput-determine-cols? ( -- tput-cols true | false )
    s" tput cols > TMP-scr-conf.OUT" <system>
    IF FALSE EXIT THEN
    s" TMP-scr-conf.OUT" INCLUDED
    TRUE ;

: tput-determine-lines? ( -- tput-lines true | false )
    s" tput lines > TMP-scr-conf.OUT" <system>
    IF FALSE EXIT THEN
    s" TMP-scr-conf.OUT" INCLUDED
    TRUE ;

: tput-determine-colors? ( -- tput-colors true | false )
    s" tput colors > TMP-scr-conf.OUT" <system>
    IF FALSE EXIT THEN
    s" TMP-scr-conf.OUT" INCLUDED
    TRUE ;

false VALUE tput-cols
false VALUE tput-lines
: tput-determine-screen-size ( -- flag )	\ sets tput-cols and tput-lines

    tput-determine-cols? 0= IF
	false to tput-cols
	false to tput-lines
	FALSE EXIT
    ELSE
	to tput-cols
    THEN
    tput-determine-lines? 0= IF
	false to tput-lines
	FALSE EXIT
    THEN

    to tput-lines
    TRUE ;

: |tput-determine-screen-size| ( -- )
    ." |tput-determine-screen-size| tries to determine screen size parameters  "
    tput-determine-screen-size
    0= IF   ." FAILURE" cr  EXIT  THEN

    ." SUCCESS"

    cr ."   cols  = " tput-cols .
    cr ."   lines = " tput-lines .
    cr
    1000 ms

    cr s" Save these values " yes-no? IF
	."  Saving console size data. "
	s" \ console screen size data written by configure-console.fs" out-line
	s" \ values from 'tput cols' and 'tput lines' "                out-line
	tput-cols num>string out
	s"  CONSTANT c-l        \ cells (or caracters) in a line "     out-line
	tput-lines num>string out
	s"  CONSTANT l-s        \ lines on the screen "                out-line
	s" " out-line
    ELSE
	."  data not saved. "
	false to tput-cols
	false to tput-lines
    THEN cr
    1000 ms ;


\ ****************************************************************
\ words for test display and interactive size configuration:

: .top-or-botton-line ( rows -- )
    [char] X emit
    2 - 0 DO [char] * emit LOOP
    [char] X emit ;

: .fill-line ( rows -- )  [char] * emit  2 - spaces  [char] * emit ;

: .start-text-line ( rows addr count -- rows-remaining )
    2>r
    dup
    1- [char] * emit
    r@ - 2/  dup spaces  -
    r@ -  2r> type ;

: .end-text-line ( rows -- )   2 - spaces  [char] * emit ;

\ switching line display for simple test version
: .text-or-empty-line ( rows lines/2 line -- rows lines/2 )
    >r		( rows lines/2  r: line )

    r@ over 3 - = IF
	over s" Test screen size." .start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 1- = IF
	over s" You should see a screen bordered by stars and with X's in all four corners." .start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 1+ = IF
	over s" Is this right y/n? " .start-text-line .end-text-line
	rdrop EXIT
    THEN

    over .fill-line
    rdrop ;

: .simple-test-screen ( rows lines -- )		\ simple yes/no version
    2 - >r			( rows  r: lines-2 )
    page
    dup .top-or-botton-line
    r@ 2/			\ middle line for text output
    r@ 0 DO  ( rows middle-line )
	i .text-or-empty-line
    LOOP
    drop
    .top-or-botton-line
    r> drop ;

: test-screen-size ( rows lines -- ok-flag )	\ simple yes/no version
    BEGIN
	2dup .simple-test-screen
	key page
	CASE
	    [char] y OF  true  true  ENDOF
	    [char] Y OF  true  true  ENDOF
	    [char] n OF  false true  ENDOF
	    [char] N OF  false true  ENDOF
	    bell ." Wrong key! " 1500 ms
	    false swap
	ENDCASE
	0= WHILE
    REPEAT
nip nip ;

\ switching line display for interactive version
: .menu-text-or-empty-line ( rows lines/2 line -- rows lines/2 )
    >r		( rows lines/2  r: line )

    r@ over 5 - = IF
	over s" Test screen size." .start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 3 - = IF
	over s" You should see a screen bordered by stars and with X's in all four corners." .start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 1 - = IF
	over s" Is this right?" .start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 2 + = IF
	over s" Press <RETURN> to accept          "
	.start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 3 + = IF
	over s" Press +        to increase width  "
	.start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 4 + = IF
	over s" Press -        to decrease width  "
	.start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 5 + = IF
	over s" Press H        to increase high   "
	.start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 6 + = IF
	over s" Press h        to decrease high   "
	.start-text-line .end-text-line
	rdrop EXIT
    THEN

    r@ over 7 + = IF
	over s" Press q        to quit            "
	.start-text-line .end-text-line
	rdrop EXIT
    THEN

    over .fill-line
    rdrop ;

: .interactive-test-screen ( rows lines -- )
    2 - >r			( rows  r: lines-2 )
    page
    dup .top-or-botton-line
    r@ 2/			\ middle line for text output
    r@ 0 DO  ( rows middle-line )
	i .menu-text-or-empty-line
    LOOP
    drop
    .top-or-botton-line
    r> drop ;

: adjust-screen-size.reaction ( rows lines -- rows' lines' ok-flag done-flag )
    key >r

    r@ 13 = IF		\ OK
	true true  rdrop EXIT
    THEN

    r@ [char] + = IF	\ increase width
	swap 1+ swap
	false false  rdrop EXIT
    THEN

    r@ [char] - = IF	\ decrease width
	swap 1- swap
	false false  rdrop EXIT
    THEN

    r@ [char] H = IF	\ increase high
	1+
	false false  rdrop EXIT
    THEN

    r@ [char] h = IF	\ decrease high
	1-
	false false  rdrop EXIT
    THEN

    r@ [char] q = IF	\ quit
	false true  rdrop EXIT
    THEN

    bell cr ." Wrong input key! " 1500 ms  
    false false  rdrop ;

: (adjust-screen-size) ( rows lines -- rows' lines' ok-flag )
    BEGIN
	2dup .interactive-test-screen
	adjust-screen-size.reaction
	0= WHILE
	drop
    REPEAT ;

: adjust-screen-size ( rows lines -- )	\ manual adjustion top level word
    2dup 2>r
    (adjust-screen-size)		( rows' lines' ok-flag  r: rows lines) 
    IF		\ OK
	2dup 2r@ d= IF
	    cr cr
	    s" No manual change done, save data anyway " yes-no?
	ELSE
	    true
	THEN
	IF	\ save size data
	    page
	    ." Save new screen size data."

    	    s" \ screen size data written by configure-console.fs " out-line
	    s" \ values found by user interaction "	            out-line
	    over num>string out
	    s"  CONSTANT c-l        \ cells (or caracters) in a line " out-line
	    dup num>string out
	    s"  CONSTANT l-s        \ lines on the screen "         out-line
	    s" " out-line
	THEN
    ELSE	\ no size accepted
	cr cr ." Screen size data not changed."
    THEN
    2drop 2rdrop  1000 ms ;


\ ****************************************************************
\ words to save configuration

: use-new-configuration ( -- )
    page
    outfile-id close-file THROW
    TMP-file-name s" my-console.conf.fs" rename-file THROW
    ." Wrote new my-console.conf.fs " cr ;

: show-oldconf ( -- )   ." OLD CONFIFURATION: " cr
    s" cat my-console.conf.fs" <system> drop ;

: show-newconf ( -- )   ." NEW CONFIFURATION: " cr
    s" cat OUTPUT/tmp/configure-console.OUT" <system> drop ;

: append-new-to-old-configuration ( -- )
    s" cat OUTPUT/tmp/configure-console.OUT >> my-console.conf.fs"
    <system> THROW
    ." Appended new code to old my-console.conf.fs " cr ;

: choose-old-new-append-conf ( -- )
    BEGIN
	page
	show-oldconf
	cr cr
	show-newconf
	." Keep old configuration, Replace with new, Append new to old    k/r/a ?"
	key CASE
	    [char] k OF 1 true ENDOF
	    [char] K OF 1 true ENDOF
	    [char] r OF 2 true ENDOF
	    [char] R OF 2 true ENDOF
	    [char] a OF 3 true ENDOF
	    [char] A OF 3 true ENDOF

	    bell cr ." Please type 'k' or 'r' or 'a'. " cr
	    2000 ms
	    false swap
	ENDCASE
    UNTIL
    page
    CASE
	1 OF  ." Kept old configuration " cr	ENDOF
	2 OF  use-new-configuration     	ENDOF
	3 OF  append-new-to-old-configuration	ENDOF
    ENDCASE ;

: maybe-save-new-configuration ( -- )
    output? 0= IF  EXIT  THEN

    s" my-console.conf.fs" file-exists? IF
	choose-old-new-append-conf
	EXIT
    THEN

    cr s" Create new configuration file " yes-no? IF
	use-new-configuration
    THEN
;

: (configure-console) ( -- )	\ configures and saves
    page
    tput-working? IF
	|tput-determine-screen-size|

	tput-determine-colors? IF
	    dup 8 <> IF
		page ." It seams you have " . ." colors on this console, "
		cr ." but only 8 colors are supported by this application. "
		cr
		cr ." Color configuration should probably be changed. "
		cr ." See file 'console-codes.fs'."
		cr ." ('configure-console.fs' only deals with screen size). "
		cr
		cr ." (press a key to continue with size configuration)"
		key drop
	    ELSE
		drop
	    THEN
	THEN
    THEN

    tput-lines IF	\ used as flag here
	tput-cols tput-lines test-screen-size IF
	    maybe-save-new-configuration
	    EXIT
	THEN

	tput-cols tput-lines
    ELSE
	80 25	\ conservative default...
    THEN

    ( col row ) adjust-screen-size
    maybe-save-new-configuration
;

\ configure-console top level word
\ ****************************************************************
: configure-console ( -- )
    page
    cr ." configure-console.fs "
    cr
    cr ." brew is designed tu run on a Linux *text* console."
    cr ." Brews pseudo graphical user interface must know your text screen size."
    cr
    cr ." This program here helps to configure the size of the console screen."
    cr ." Please run it on the text console you want to use for brew. "
    cr ." (I don't know if it runs on anything else than Linux.) "
    cr
    cr
    s" Continue " yes-no? IF

	(configure-console)

	s" my-console.conf.fs" file-exists? IF
	    cr ." Current configuration in file 'my-console.conf.fs': "
	    cr
	    cr s" cat my-console.conf.fs" <system> drop
	ELSE
	    cr ." No file 'my-console.conf.fs'.  Using default values. " cr
	THEN

    THEN
    cr
    cleanup-files ;

configure-console
BYE

\ ****************************************************************
\ ****************************************************************
\ ****************************************************************

\ 80 CONSTANT		c-l		\ cells (or caracters) in a line
\ 25 CONSTANT		l-s		\ lines on a screen


\ only `Tektronix-like' terminals supported
\ see man terminfo(5)

\ Color Handling
\     Most  color  terminals are either `Tektronix-like' or `HP-
\     like'.  Tektronix-like terminals have a predefined set  of
\     N  colors  (where N usually 8), and can set character-cell
\     foreground and background characters independently, mixing
\     them  into  N  * N color-pairs.  On HP-like terminals, the
\     use must set each color pair up separately (foreground and
\     background  are  not  independently  settable).   Up  to M
\     color-pairs may be  set  up  from  2*M  different  colors.
\     ANSI-compatible terminals are Tektronix-like.

