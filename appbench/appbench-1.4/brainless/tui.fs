\
\ Terminal user interface
\
: clear  ( -- )  empty-board ;
: ?square  ( square -- square )
   DUP a1 h8 1+ WITHIN 0= ABORT" Invalid square!" ;
: ?empty  ( square -- square )
   DUP empty? 0= ABORT" Square not empty! (use `remove' first)" ;
: ?not-empty  ( field -- )
   DUP empty? ABORT" Square is empty!" ;
: add  ( piece color square -- )
   ?square ?empty   -ROT OR 
   OVER initial-board @ full-piece-mask AND OVER =   f-unmoved AND   OR
   f-piece OR SWAP board !
   update-board ;
: remove  ( square -- )
   ?square ?not-empty   remove-piece   update-board ;
: epdsave  ( "name" -- )
   BL WORD COUNT 2DUP file-exists? IF
      epd-append-to-file
      ." Current position appended to file as entry " .
   ELSE
      epd-create-file
      ." Created new file, current position is entry 0"
   THEN ;
: epdload  ( index "name" -- ) \ load entry (index 0 = 1st) from file
   BL WORD COUNT epd-read-file ;

: ?find-move  ( i*x from to -- i*x index | )
   find-move 0= ABORT" Invalid move!" ;
: ?valid-move  ( i*x from to --  |i*x )
   generate-moves find-move forget-moves 0= ABORT" Invalid move!"   DROP ;
: m  ( from to -- ) \ perform a move
   2DUP ?valid-move
   generate-moves eval-moves ?find-move DUP display-move SPACE
   get-move do-move forget-moves ;
: cm  ( -- ) \ let the computer move
   ." Hmm..."  generate-moves eval-moves calculate-move
   CR ." my move is " DUP display-move SPACE
   get-move do-move forget-moves ;
: lm  ( -- ) \ print list of moves
   generate-moves ?eval-moves .emoves forget-moves ;
: best  ( -- ) \ print evaluated and sorted list of moves
   generate-moves sort-moves .emoves forget-moves ;
: demo  ( -- )
   2 2 DO
      CR I 2 / 3 .R ." : " cm look
      KEY? IF LEAVE THEN
   LOOP ;




