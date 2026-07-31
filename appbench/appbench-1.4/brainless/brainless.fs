\
\ Brainless -- public domain ANS Forth chess program
\
\ by David Kuehling
\
\ You can contact me via email:
\    dvdkhlng@gmx.de
\    dvdkhlng@cs.tu-berlin.de (read less frequently)
\
\ or snail mail:
\    David Kuehling
\    Bansiner Str. 27A
\    D-12619 Berlin
\    GERMANY
\ 
\

.( Loading Brainless v0.0.2...) CR
.( This is public domain software. No warranty!) CR

: file-prefix  ( -- c-addr u )  S" " ;
: file-suffix  ( -- c-addr u )  S" .fs" ;

: append  ( c-addr1 u1 c-addr2 u2 -- c-addr3 u3 ) \ append str2 to str1 -> str3
   2>R 2DUP +  2R@ ROT SWAP MOVE 2R> NIP + ;

: load-part  ( "filename" -- )
   PAD 0
   file-prefix append
   BL WORD COUNT append
   file-suffix append
   2DUP ." [" TYPE SPACE
   INCLUDED
   ." ]" ;

0 VALUE compilation-finished?

load-part environ
load-part options
load-part utils
load-part board
load-part hash
load-part drawing
load-part string
load-part epd
load-part threats
load-part searchdefs
load-part repeat
load-part moves
load-part ttable
load-part eval
load-part flyeval
load-part movegen
\ load-part sglmove
load-part tmovegen
load-part quiescence
load-part null
load-part moveconv
load-part killer
load-part sorting
load-part search
load-part tui

TRUE TO compilation-finished?

init-board

CR
