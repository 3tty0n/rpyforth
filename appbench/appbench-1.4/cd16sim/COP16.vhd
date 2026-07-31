\ DSP coprocessor for CD16
( The CPU has already been defined. This module plugs into it.)
( Resolves DEFERed words: decCP, CPA, CPO )
\ When ctrl(6)=1, the coprocessor is active and data memory is addressed by CPA.
\ Address for data RAM
\ Output to stack
\ Input from data memory
\ Input from stack
\ Control
r: CA  r: CB         \ input latches
r: acc  r: acch      \ accumulator
w: cal  w: cbl       \ latch input registers
w: accl              \ latch acc
                     \ acc input
w: adsel             \ select YB for adder input
r: CidxA r: CidxB    \ data memory address
r: idxsel w: iset w: iclr
w: cosel
$ ca_p $ ca_l4 $ ca_r4 $ ca_r1 $ ca_lh $ ca_ll
w: casel
w: cidl  w: cien     \ Cidx latch, select
r: offset  r: index
w: ofsl  w: bump
\ sign extension
: index1 ( -- n ) offset @@ index @@ + 07F AND ;
: index2 ( -- n ) index1 22 - 07F AND ;
: sx    ( n -- n' ) DUP 8000 AND 0<> FFFF0000 AND OR ;
: prod  ( -- n ) CA @@ sx CB @@ sx * ;
: Cidx  ( -- n ) idxsel @@ IF CidxB @@ index @@ + 
                 ELSE CidxA @@ THEN ;
:noname ( -- n ) Cidx ; is CPA
: addin ( -- n ) adsel @ IF YB sx ELSE prod THEN ;
: acc@  ( -- d ) acc @@ acch @@ DUP 10 AND 0<> -10 AND OR ;
: acci  ( -- d ) casel @ CASE
     ca_p  OF acc@ addin S>D d+      ENDOF
     ca_l4 OF acc@ d2* d2* d2* d2*   ENDOF
     ca_r4 OF acc@ d2/ d2/ d2/ d2/   ENDOF
     ca_r1 OF acc@ d2/               ENDOF
 ca_lh OF acc @@ FFFF AND YB 10 LSHIFT OR 0 ENDOF
 ca_ll OF YB 0                   ENDOF
abort" Invalid casel " ENDCASE 01F AND ;
:noname ( -- n ) cosel @ CASE
     3 OF prod 10 RSHIFT    ENDOF
     0 OF acc @@            ENDOF
     1 OF acc @@ 10 rshift  ENDOF
     2 OF Cidx              ENDOF
abort" Invalid cosel " ENDCASE &cell ; is CPO
: process ( -- ) process
     CPctrl 3 AND cosel !
     0 cal !   0 cbl !   0 accl !  
     0 cidl !  0 cien !  0 adsel !
     ca_p casel !
     0 iset !  0 iclr !  
     0 ofsl !  0 bump !
     CPctrl 2 RSHIFT 0F AND CASE
 2 OF 1 accl ! 1 adsel !         ENDOF
 3 OF 1 accl ! ca_l4 casel !     ENDOF
 4 OF 1 accl ! ca_r4 casel !     ENDOF
 5 OF 1 accl ! ca_r1 casel !     ENDOF
 6 OF 1 accl ! ca_lh casel !     ENDOF
 7 OF 1 accl ! ca_ll casel !     ENDOF
 8 OF                            ENDOF
 9 OF  1 cidl !                  ENDOF \ load Cidx
 0A OF 1 iclr !                  ENDOF
 0B OF 1 iset !                  ENDOF
 0C OF 1 cal !  1 accl ! 
       1 iset ! 
       1 cidl ! 1 cien !         ENDOF
 0D OF 1 cbl !  1 bump !       
       1 iclr !                  ENDOF
 0E OF 1 ofsl !                  ENDOF               
      1 cal ! 1 cbl ! 1 accl !  \ clock MAC
      CPctrl 4 bit? cien !
      CPctrl 4 bit? cidl !
 ENDCASE
reset IF
  0 CA !!
  0 CB !!
  0 acc !! 0 acch !!
  0 CidxA !!
  0 CidxB !!
  0 idxsel !!
  0 index !!
  0 offset !!
ELSE CPctrl 40 AND IF
  iset @ IF
    1 idxsel !!
  ELSE iclr @ IF
    0 idxsel !!
  THEN THEN
  cidl @ IF
    cien @
    IF   idxsel @@
       IF   Cidx 1+ &da CidxB !!
       ELSE Cidx 1+ &da CidxA !!
       THEN
    ELSE idxsel @@
       IF   YB CidxB !!
       ELSE YB CidxA !!
       THEN
    THEN
  THEN
  cal @ IF
    DI CA !! 
  THEN
  cbl @ IF
    YB CB !!
  THEN
  accl @ IF
    acci acch !! acc !!
  THEN
  ofsl @ IF
    YB 03F AND offset !!
    0 index !!
  THEN
  bump @ IF  
      index2 40 bit?
IF   index1
ELSE index2
      THEN 03F AND index !!
  THEN
THEN THEN
;
