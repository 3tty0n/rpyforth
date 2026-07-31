DECIMAL
15 CONSTANT n        \ 16-bit cells, max n is 29.
 8 CONSTANT spwidth  \ stack space (dual port RAM)
14 CONSTANT pawidth  \ 16K of program RAM
14 CONSTANT dawidth  \ 16K of data ROM/RAM

\ Discrete Event Simulation -------------------------------------------------
DECIMAL 100 CONSTANT maxregs                 \ # of regs allowed in model
CREATE currnt  maxregs CELLS ALLOT           \ current state of registers
CREATE pending maxregs CELLS ALLOT           \ state after the next clock
VARIABLE REGISTERS   0 REGISTERS !           \ registers in the list
: undef  -1 abort" Undefined I/O signal detected" ;
currnt maxregs CELLS -1 fill pending maxregs CELLS -1 fill
: process ;
: clkmem ;                                   \ processes to build on
: clkregs    ( -- ) pending currnt registers @ MOVE ; \ clock registers
: r:  ( <name> -- ) registers @ CONSTANT     \ register
    1 CELLS registers +!  registers @ maxregs CELLS = IF ABORT THEN ;
: w:  ( <name> -- ) CREATE -1 , DOES> ;      \ wire
: !!   ( val n -- ) pending + ! ;            \ pending value
: @@   ( n -- val ) currnt + @ ;             \ current value
: ~   ( <name> -- ) ' , ;                    \ add to table
: $   ( <name> -- ) VARIABLE ;               \ for enumerated types

\ the use of POSTPONE IS below is not standard-conforming and does not work as intended on Gforth
\ : in: ( xt <name> -- ) >IN @ >R DEFER R> >IN ! \ an input signal
\      ( exec: -- n ) POSTPONE IS ;            \ xt is an action for the signal
\ : ev: ( <name> -- ) >IN @ >R DEFER R> >IN !  \ an undefined event
\      ( exec: n -- ) ['] DROP  POSTPONE IS ;

\ standard-conforming replacements:
: in: ( xt <name> -- )
  >in @ defer >in ! ' defer! ;
: ev: ( <name> -- )
  ['] drop in: ;
         
: 2^n     ( n -- 2^n ) 1 SWAP LSHIFT ;       \ memory sizing
: mask ( n <name> -- ) CREATE , DOES> @ AND ; HEX
spwidth 2^n 1- mask &sa                      \ stack address mask
dawidth 2^n 1- mask &da                      \ data address mask
pawidth 2^n 1- mask &pa                      \ program address mask

: step ( -- ) s" process clkmem clkregs" evaluate ;



