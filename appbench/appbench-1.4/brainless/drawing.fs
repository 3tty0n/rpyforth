\
\ Drawing the chessboard
\

TRUE option ansi-terminal?	\ use ANSI Terminal codes (GForth only)
TRUE option color-terminal?	\ also use terminal colors? (GForth only)

true [if] \ for benchmarking
    false set-option ansi-terminal?
    false set-option color-terminal?
[then]

    
0 VALUE white-field?
0 VALUE white-piece?
3 VALUE field-width
2 VALUE field-height

: small-board  ( -- )  2 to field-width  1 to field-height ;
: normal-board  ( -- )  3 to field-width  2 to field-height ;
: huge-board  ( -- )  5 to field-width  3 to field-height ;

\
\ Chessboard color display
\
gforth? ansi-terminal? AND [IF] \ GForth' ANSI Terminal routines

   : no-attr  ( -- )  <A A> ATTR! ;
   : white-field-attr   color-terminal? IF WHITE >B ELSE INVERS THEN ;
   : black-field-attr   color-terminal? IF BLACK >B THEN ;
   : field-attr  
      white-field? IF white-field-attr ELSE black-field-attr THEN ;
   : white-piece-attr  
      color-terminal? IF   RED >F BOLD field-attr
      ELSE   BOLD  THEN ;
   : black-piece-attr  
      color-terminal? IF  BLUE >F BOLD field-attr THEN ;
   : piece-attr
      white-piece? IF white-piece-attr ELSE black-piece-attr THEN ;
   : field-spaces  ( n -- )  <A field-attr A> ATTR! SPACES no-attr ;
   : .piece  ( piece -- )  <A piece-attr A> ATTR! piece>char EMIT no-attr ;

[ELSE] iforth? [IF] \ iforth color code -- thanks to Marcel Hendrix
   
   : GRCOLOR  ( x "name" -- )
      CREATE ,  DOES> @ 0 SWAP SYSCALL DROP ;

   #64 GRCOLOR black	#65 GRCOLOR blue
   #66 GRCOLOR green	#68 GRCOLOR red
   #78 GRCOLOR yellow	#79 GRCOLOR white

   : no-attr  ( -- )  black TO TextBGColor  white TO TextFGColor  BARE ;
   : field-attr  ( -- )
      white-field? IF white ELSE black THEN TO TextBGColor ;
   : piece-attr  ( -- )
      white-piece? IF red ELSE blue THEN TO TextFGColor  field-attr ;
   : field-spaces  ( n -- )  field-attr SetTerm SPACES no-attr ;
   : .piece  ( piece -- )  piece-attr SetTerm piece>char EMIT no-attr ;

[ELSE] \ no colors in other Forth systems (yet?) :-(
   
   : field-spaces  ( n -- )
      white-field? IF BL ELSE [CHAR] : THEN
      SWAP 0 ?DO DUP EMIT LOOP   DROP ;
   : .piece  ( piece -- )  piece>char EMIT ;

[THEN] [THEN]

\
\ Display the chessboard slice by slice
\
: .field-slice  ( piece slice -- )
   OVER f-piece AND 0<>   SWAP field-height 2/ = AND
   IF
      field-width 2/ TUCK field-spaces   .piece
      field-width 1- SWAP - field-spaces
   ELSE   DROP   field-width field-spaces THEN ;
: .vborder-slice  ( y slice -- )
   SPACE   field-height 2/ = IF  1+ . ELSE  DROP 2 SPACES THEN ;
: .board-line  ( y -- )
   field-height 0 DO
      DUP I .vborder-slice
      8 0 DO
	 I OVER square-white? TO white-field?
	 I OVER xy-board@ DUP f-white AND 0<> TO white-piece?
	 J .field-slice
      LOOP
      DUP I .vborder-slice  CR
   LOOP DROP ;
: .hborder  ( -- )
   3 SPACES  8 0 DO
      field-width 2/ DUP SPACES   I [CHAR] A + EMIT
      field-width 1- SWAP - SPACES
   LOOP CR ;
: look  ( -- )
   CR .hborder   0 7 DO I .board-line   -1 +LOOP   .hborder ;

