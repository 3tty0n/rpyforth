\ Single port data memory for CD16.
\ Data memory, synchronous read/write
' undef in: d_in     \ data from CPU
                     \ data to CPU
                     \ address from CPU
' undef in: d_wr     \ write enable
' undef in: d_rd     \ read enable
CREATE DMEM  -1 &da 1+ CELLS ALLOT       
: 'DMEM ( n -- addr ) &da CELLS DMEM + ;
r: da_r
: clkmem ( -- )
  d_wr IF dy da 'DMEM ! THEN
  d_rd IF da da_r !! THEN
  clkmem ;
: d_out ( -- n )        \ data to CPU
da_r @@ 'DMEM @ &cell ;
