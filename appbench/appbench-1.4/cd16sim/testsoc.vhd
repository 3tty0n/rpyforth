\ Top of VHDL description
include cd16pkg.vhd
' undef in: reset
' undef in: p0_in
r: p0out
\ debugging vectors
\ ---------------------------------------------------------------------------
\ Stack memory
include DPRAM.vhd
\ CPU
include CD16.vhd
\ insert wait state(s)
\ interrupt triggers
\ Data from Stack DPRAM (already defined)
\ Data to Stack DPRAM
\ Stack DPRAM address
\ Stack DPRAM write enables
\ Stack DPRAM read enables
\ Data to program space
\ Data from program space
\ Program space address
\ write to program memory, sync write
\ Data to data space
\ Data from data space
\ Data space address
\ data mem read/write enable
\ Data address from coprocessor
\ Coprocessor output
\ Coprocessor instruction
\ debugging vectors
\ Coprocessor
include COP16.vhd
\ Address for data RAM
\ Output to stack
\ Input from stack
\ Input from data memory
\ Control
\ Data memory space
include DMEM.vhd
\ Program memory space
include PMEM.vhd 
\ Xilinx Spartan implementation
\ uses a 2x clock for Block RAM use.
\ 2x PLL for Spartan Block RAM
\ prog space
\ data space
\ delayed data read address
\ stack space
\ I/O chip selects
\ decoder adddess lines
r: ticktimer \ timebase
\ interrupts
\ Comment this section out for simulation using single clock
\   U_CLK: dll_doubler port map(clock,clk2,locked);
\   ck: process (clk2) begin            -- \ generate clk from 2x clock
\       if (clk2'event and clk2='1') then
\          if clk='1' then clk<='0';
\                     else clk<='1'; end if;
\       end if;
\   end process;
\ Timebase interrupts
:noname ticktimer @@ 200 bit? 7 LSHIFT
        ticktimer @@  80 bit? 6 LSHIFT OR
; is int \ tick interrupt
\ Stack memory: dual port, async read, sync write.
\ May use synchronous read starting at negative clk edge.
\ User Coprocessor
\ CPU
' false is hold \ no wait states for test setup
\ ====================================================================
\ ======== Program memory space ======================================
\ ====================================================================
' p_out is pi
' py is p_in
' wp is p_w
\ ====================================================================
\ ======== Data memory space =========================================
\ ====================================================================
\ 0000..7FFF = RAM
: dataram?  da 8000 AND 0= ;
:noname dataram? wd AND ; is d_wr
:noname dataram? rd AND ; is d_rd
: io? ( pt -- f ) 08000 OR da 0C007 AND = ;
: io=p0?  0 io? ;
\ others...
:noname da_r 8000 AND 0= IF d_out EXIT THEN
   io=p0? IF p0_in EXIT THEN
                 -1 ; is di
' dy is d_in                     \ Read from I/O ports
\ di <= p0_in when (iocs(0) and rd)='1' else (others=>'Z');
\ Writes to I/O ports and clocked events
: process ( -- ) process
  reset IF
     0 p0out !!
     0 ticktimer !!
  ELSE
     ticktimer @@ 1+
     ticktimer !!
     wd io=p0? AND
     IF dy p0out !! THEN
  THEN ;
: p0_out ( -- n ) p0out @@ ;
