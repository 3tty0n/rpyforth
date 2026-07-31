\ Dual Port RAM
\ Stack memory for CD16
' undef in: aa  ' undef in: ab       \ address
                                     \ output data
' undef in: ia  ' undef in: ib       \ input data
' undef in: wa  ' undef in: wb       \ write enables
' undef in: ra  ' undef in: rb       \ read enables
\ This memory is asynchronous read, synchronous write. clk2 is provided for
\ FPGA implementations using Block RAM.
\ clk ________--------________--------____
\ aa  .........xxxxxAAAAAAAAAAAxxxxx......
\ dy  ..............DDDDDDDDDDD........... asynchronous read
\ di  .........xxxxxDDDDDDDDDDDxxxxx...... synchronous write
\                             ^----------- clk 0>1 latches data
\ If the RAM must be synchronous read, you can latch the read address on the falling clk edge.
CREATE DPRAM -1 &sa 1+ CELLS ALLOT
: 'SMEM ( n -- addr ) &sa CELLS DPRAM + ;
\ synchronous write
: clkmem  ( -- ) clkmem
    wa IF IA AA 'SMEM ! THEN
\ write port A
    wb IF IB AB 'SMEM ! THEN
\ write port B
\ Write collision not allowed.
wa wb AND AA AB = AND ABORT" DPRAM write collision"
    ;
\ fake an asynchronous read    
\   rd: process(clk) begin              -- \ fake an asynchronous read
\       if falling_edge(clk) then
\           aa_d <= aa;
\           ab_d <= ab;
\       end if;
\   end process rd;
\ The read-enabled version isn't modeled in the Forth model. In theory, it shouldn't matter
\ since the read enables are only used to reduce superfluous data switching.
\ asynchronous read
: ya  ( -- n ) aa 'SMEM @ ;
: yb  ( -- n ) ab 'SMEM @ ;
