\ CD16 simulator that runs ANS Forths

\ Uses stripped out VHD files

ONLY FORTH ALSO DEFINITIONS
include testsoc.vhd
decimal


0 value tally
0 value status
variable trace  0 trace !
0 value rfile

: romname       ( -- addr len )  s" ROM.HEX" ;

: .stats        ( -- )  \ display status of internal wires
        cr ." P=" P @@ . ." IR=" ir @@ . ." SP=" sp @@ . ." RP=" rp @@ .
        cr ." wa=" wa . ." aa=" aa . ." ia=" ia . ." ya=" ya .
           ."  C=" cf@ . ." N=" w(n+1) . ." Z=" nz 0= 1 and . ." condition=" condition .
           ." iacond=" iacond .
        cr ." wb=" wb . ." ab=" ab . ." ib=" ib . ." yb=" yb .
           ."  cen=" cen @ . ." cin=" cin .
           ." uci=" uci . ." ua=" ua . ." ub=" ub . ." uo=" uo .
        cr ." pw=" pw @ . ." pa=" pa . ." pi=" pi . ." py=" py .
           ." xen=" xen . ." xfb=" xfb . ." XP=" XP . ." spin=" spin .
           ."  wen=" wen @ . ." win=" win . ." pin=" pin .
        cr ." dw=" dw @ . ." da=" da . ." di=" di . ." dy=" dy .
           ."  aconst=" aconst . ." uol=" uol . ." uoa=" uoa .
           ." ubc=" ubc . ." uasel=" uasel . ." ya1=" ya1 .
        cr ." xpsx=" xpsx . ." para=" para . ." offa=" offa . ." xpxs=" xpxs .
           ." xpx=" xpx . ." ybn@=" ybn@ . ." dissx=" dissx . ." brdis=" brdis .
        cr
        ;


: CPUstep ( -- ) process clkmem clkregs tally 1+ to tally trace @ if .stats then ;

: steps 0 ?do CPUstep loop ;

variable _reset  :noname _reset @ ; is reset    \ set level on reset wire

: ?badformat    ( f -- )        abort" Bad file format" ;
: nextnibl      ( a -- a+1 n )  count [char] 0 - dup 9 > 7 and - ;
: nextbyte      ( a -- a+2 n )  nextnibl 4 lshift >r nextnibl r> + ;
: nextword      ( a -- a+4 n )  nextbyte 8 lshift >r nextbyte r> + ;

: progc!        ( c addr -- )
                2 /mod 'PMEM  swap 1 xor +  c! ;  \ assume host is little endian

create hline 260 allot

: hload         ( -- )
\ Load intel .HEX file using the currently open file
                PMEM -1 &pa 1+ CELLS -1 fill    \ unfilled ROM is -1
        begin   hline 256 rfile read-line ?badformat
                nip                             \ don't need length
        while   hline count [char] : <> ?badformat
                nextbyte swap                   ( #bytes a )
                nextword swap                   ( #bytes org a )
                nextbyte 0=                     \ accept only data type 0
                if      -rot swap bounds        ( a . . )
                        ?do     nextbyte i progc! \ load data
                        loop    drop            \ ignore checksum
                else    3drop
                then
        repeat  ;

: clear         ( -- )
                1 _reset ! CPUstep CPUstep      \ sync reset
                0 _reset !
                256 0 do 0 i 'smem ! loop       \ clear the stacks
                0 to tally 0 to status
                romname r/o open-file           \ load ROM image from file
        if      drop cr ." Couldn't find " romname type
        else    to rfile  hload
                rfile close-file drop
        then    ;


: .ss           ( -- )  \ dump stack contents
                64
        begin   dup SP @@ >
        while   1- dup 'smem @ .
        repeat  drop ;

cr clear 2981 steps .ss .( should be 22136 4660 4131 0 )

: secs time&date 2drop 2drop 60 * + ;

: benchmark     clear secs swap steps secs swap - .
                ." seconds elapsed" ;


