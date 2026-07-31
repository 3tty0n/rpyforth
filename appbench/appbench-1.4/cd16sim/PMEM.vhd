\ Program memory for CD16: Synthesizable ROM
CREATE PMEM  -1 &pa 1+ CELLS ALLOT
: 'PMEM ( n -- addr ) &pa CELLS PMEM + ;
: p_out ( -- n ) pa 'PMEM @ ;
' undef in: p_in
' undef in: p_w
r: rom_a
: clkmem ( -- ) p_w IF p_in pa 'PMEM ! THEN
  pa rom_a !! clkmem ;
: p_out ( -- n ) rom_a @@ 'PMEM @ &cell ;

