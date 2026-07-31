\ CD16.VHD
\ Dual language RTL model of the CD16 CPU.
\ by: Brad Eckert  brad@tinyboot.com
\ revision: 1
\ This model can be simulated with either a 32-bit ANS Forth or a VHDL simulator. The code to the left of '--'
\ is intended to be ignored by the Forth interpreter. The code to the right is ignored by the VHDL tool.
\ Structures are expressed side by side in both languages so as to comment each other. The Forth model
\ represents bits and vectors in a less rigorous way, so it will simulate much faster than the VHDL model.
\ MS Word 8pt Courier allows 115 columns on Letter paper. Let VHDL comments begin at column 49, leaving 67
\ columns for Forth. A screen resolution of 1024x768 or better is desirable when editing or browsing this file.
\ reset the CPU, master clock
' undef in: hold  \ insert wait states
' undef in: int   \ interrupt trigger
\ Data from Stack DPRAM (already defined)
\ Data to Stack DPRAM
\ Stack DPRAM address
\ Stack DPRAM write enables
\ Stack DPRAM read enables
\ Data to program space
' undef in: pi    \ Data from program space
\ Program space address
\ write to program memory, sync write
\ Data to data space
' undef in: di    \ Data from data space
\ Data space address
\ write to data memory, sync write
\ data mem read-enable, sync read
' undef in: CPA
' undef in: CPO
w: CPctl
\ debugging vectors
n 2^n          mask &sign   ( sign bit mask        )  -1 &sign 1- CONSTANT maxint
n 3 + 2^n      mask &carry  ( ALU carry-out mask   )  n 3 - 2^n 1- mask &brdisp
n 1+ 2^n 1-    mask &cell   ( cell-wide mask       )  n 4 - 2^n CONSTANT brsign
n 1+ 2^n       mask &sign+1 ( W register sign bit  )  n 2 + 2^n 1- mask &cell+1
    \ 0
\ registers may be changed to arrays for instant context switching
r: SP  r: RP \ stack pointers
r: P                 \ program counter
r: IR                \ instruction reg
r: W                 \ W reg
r: cf   r: ov        \ carry flag, overflow flag
r: sleep             \ sleep until interrupt
\ interrupt logic
w: drowsy            \ trigger sleep mode
w: iack              \ interrupt just acknowledged
r: intd  r: IRQpend
\ stack addressing logic
w: wrena w: wrenb    \ stack memory write enable
w: rdena w: rdenb    \ stack memory read enable
w: ssel              \ select 0=SP, 1=RP (addresses A)
w: predec            \ predec selected stack pointer
w: postinc           \ postinc selected stack pointer
w: xbump             \ postinc by signed offset
                     \ latch enable SP/RP, select +/- 1
w: yax               \ route YA to XP if enabled
w: rex               \ enable extended Rstack addressing
\ sign extension for SP displacement
\ sign extension for constant +/- 1
\ selected ptr for A
\ inter-mux busses
$ ia_p  $ ia_w $ ia_xp $ ia_c $ ia_uo
w: iasel                $ ia_pi $ ia_di
$ ib_uo $ ib_cp $ ib_ya
w: ibsel
w: div               \ enable divider latch
\ P register
                     \ P in, signed branch displacement
$ p_bump $ p_ir $ p_ya
w: psel              \ src = P+1, P+IR, IR, YA
w: bran              \ 1: P adder uses displacement brdis
w: stall             \ 1: don't bump PC
w: repen             \ 1: load REP counter
r: reps              \ REP counter
\ W register
                     \ pending W (if wen=1)
w: wm                \ W source
w: wen               \ latch enable W
\ ALU inputs A and B                                             \ ALU inputs A and B
w: ubm               \ B = shiftop(YB,ubm)
w: uam               \ A = const, YA1
w: uas               \ YA1 = YA, YA>>8
w: mul               \ input A is in multiplier mode
w: sub               \ force ALU carry in to '1'
w: aluop             \ +, C+, +C, C+C, A, A&B, A!B, A^B
\ adder result with carry in and carry out
\ 14-bit A constant sign extension
w: acon              \ A constant = 0, 1, -2, -1
                     \ A possible inputs
\ carry flag
w: cm                \ src = cf, carry(A+B), YB(n), YB(0)
w: cen               \ carry latch enable
\ IR and branch
w: flush             \ discard data on the instruction bus
w: flushIR           \ the pending "nop"
\ condition(IR11:IR8)
\ sign extension for branch displacement
\ Program memory
w: pasel             \ program address
w: pw                \ prog write enable
\ Data memory
$ a_cp $ a_yb $ a_pi
w: dasel             \ select data memory address
w: dw  w: drd        \ data write enable, read enable
\ interrupt priority encoder: level 1 is highest priority
\ : ipl  ( -- n ) 0
\   8 1 DO IRQpend @@
\     1 I LSHIFT AND
\     IF DROP I LEAVE THEN
\   LOOP ;
: ipl  ( -- n ) IRQpend @@ DUP IF
   DUP  0F0 AND 0<> 4 AND
   OVER 0CC AND 0<> 2 AND +
   SWAP 0AA AND 0<> 1 AND +
   THEN ;
: nz      ( -- bit )    W @@ -2 AND 0<> 1 AND ;   \ '1' if W<>0
: w(n+1)  ( -- bit )    W @@ &sign+1 0<> 1 AND ;    \ sign of W
: w(n)    ( -- bit )    W @@ &sign 0<> 1 AND ;    \ sign of W/2
: cf@     ( -- bit )    cf @@ 1 AND ;             \ carry latch
: inv     ( bit -- !bit ) INVERT 1 AND ;             \ flip bit
: hialu   ( n bit -- n' ) n 2 + lshift OR ;      \ adder hi bit
: bit?    ( mask -- bit ) AND 0<> 1 AND ;
: repeating ( -- f )  reps @@ 0<> 1 AND ;
: xen     ( -- f )    predec @ postinc @ xbump @ OR OR ;
: XP      ( -- ptr )  ssel @ IF RP ELSE SP THEN @@ ;
: xpsx    ( -- sext ) xbump @ 0<> IR @@ 200 AND 0<> AND ;
: para ( -- n )    IR @@ 4 RSHIFT  rex @
IF 03F AND xpsx -40 AND OR ELSE 0F AND THEN &sa ;
: offa    ( -- n )    postinc @ xbump @ 0= AND predec @ OR
          IF predec @ IF -1 ELSE 1 THEN &sa ELSE para THEN ;
: xfb     ( -- n )    XP offa + &sa ;
: xpxs ( -- f )    IR @@ 80 bit?  xen 0= AND  rex @ 0= AND ;

: xpx     ( --f )  xpxs IF IR @@ 4 RSHIFT 7 AND ELSE XP THEN ;
:noname ( -- n ) iack @ IF ipl 7 XOR 8 OR 
ELSE postinc @ 0= xpxs 0= and
          IF xfb ELSE xpx THEN THEN ;                is aa
:noname   ( -- n )    IR @@  DUP 8 AND     \ address of B param
          IF 7 AND ELSE 7 AND SP @@ + THEN ;         is ab
: ybn@    ( -- bit )  yb &sign 0<> 1 AND ;     \ sign bit of YB
: dissx   ( -- n ) IR @@ brsign AND 0<> -1 &brdisp INVERT AND ;
: brdis   ( -- disp ) bran @ IF IR @@ &brdisp dissx OR
          ELSE stall @ reset or repeating or 0= 1 AND THEN ;
: yan@    ( -- bit )  ya &sign 0<> 1 AND ;     \ sign bit of YA
: pin     ( -- n ) psel @ CASE                        \ P input
          p_bump OF P @@ brdis +             ENDOF
        p_ir   OF IR @@ maxint AND 2*      ENDOF
          p_ya   OF ya                       ENDOF
          ABORT" Invalid PSEL" ENDCASE ;
: ubc     ( -- bit ) ubm @ 3 and CASE     \ shifter carry input
          0 OF 0                             ENDOF
          1 OF cf@                           ENDOF
          2 OF w(n+1)                        ENDOF
          3 OF ybn@                          ENDOF
          ENDCASE ;
: ub      ( -- n ) ubm @ 2 RSHIFT CASE          \ ALU 'A' input
          0 OF YB                            ENDOF
          1 OF YB INVERT &cell               ENDOF
        2 OF YB 2* ubc OR &cell            ENDOF
        3 OF YB 1 RSHIFT ubc n LSHIFT OR   ENDOF
          ABORT" Invalid UBM" ENDCASE ;
: uasel   ( -- bit ) mul @ IF w(n) ELSE uam @ THEN ;
CREATE aconsts 0 , 1 , 2 , -1 ,
: aconst  ( -- n ) acon @ 3 AND CELLS aconsts + @ &cell ;
: swhalf  ( n -- n' ) 2 n 2/ LSHIFT DUP >R /MOD SWAP R> * + ;
: ya1     ( -- n ) YA  uas @              \ swap hi & lo halves
          IF swhalf THEN ;
: ua      ( -- n ) uasel                        \ ALU 'B' input
          IF ya1 ELSE aconst THEN ;
: uol     ( -- n ) aluop @ 3 AND CASE       \ logic part of ALU
          0 OF ub                            ENDOF
          1 OF ub ua AND                     ENDOF
          2 OF ub ua OR                      ENDOF
          3 OF ub ua XOR                     ENDOF
          ENDCASE ;
: uci     ( -- bit ) aluop @ 3 AND 3 = cf@ AND sub @ OR ;
: uoa     ( -- n ) ub 2* 1 + div @ hialu    \ adder part of ALU
          ua 2* uci +
          div @ inv cf@ OR hialu + ;
: uo      ( -- n ) aluop @ 4 AND IF uol         \ output of ALU
                   ELSE uoa 2/ &cell THEN ;
: cin     ( -- n ) cm @ CASE                         \ cf input
          0 OF 0                             ENDOF
          1 OF uoa &carry 0<> 1 AND          ENDOF
          2 OF ybn@                          ENDOF
          3 OF YB 1 AND                      ENDOF
          ABORT" Invalid CM" ENDCASE ;
: condition ( -- f ) IR @@ 9 RSHIFT 7 AND CASE
  0 OF 0                             ENDOF
  1 OF cf@ nz and inv                ENDOF
  2 OF cf@                           ENDOF
  3 OF nz inv                        ENDOF
  4 OF ov @@                         ENDOF
  5 OF w(n+1)                        ENDOF
  6 OF ov @@ w(n+1) xor              ENDOF
  7 OF ov @@ w(n+1) xor nz inv or    ENDOF
  ENDCASE IR @@ 100 AND 0<> XOR 1 AND ;
: iacond  ( -- n ) condition 0<> &cell ;
: spin    ( -- n ) yax @ IF YA ELSE xfb THEN ;     \ pending XP
: da      ( -- n ) dasel @ CASE
          a_pi  OF PI                        ENDOF
          a_cp  OF CPA                       ENDOF
          a_yb  OF YB                        ENDOF
          ABORT" Invalid DASEL" ENDCASE ;
: pa      ( -- n ) pasel @ IF YB else pin THEN ;
:noname   ( -- n ) iasel @ CASE                 \ DPRAM A input
          ia_pi OF PI                        ENDOF
          ia_di OF DI                        ENDOF
          ia_uo OF uo                        ENDOF
          ia_c  OF iacond                    ENDOF
          ia_xp OF XP  ( unsigned )          ENDOF
          ia_w  OF W @@ 1 RSHIFT             ENDOF
          ia_p  OF P @@                      ENDOF
          ABORT" Invalid IASEL" ENDCASE ;            is ia
:noname   ( -- n ) ibsel @ CASE                 \ DPRAM B input
          ib_uo OF uo                        ENDOF
          ib_cp OF CPO                       ENDOF
          ib_ya OF YA                        ENDOF
          ABORT" Invalid IBSEL" ENDCASE ;            is ib
: win     ( -- n )    wm @ CASE                       \ W input
          0 OF uo 2*                         ENDOF
          1 OF uo                            ENDOF
2 OF W @@ cf@ + 2*                 ENDOF
3 OF W @@ cf@ + 2* ybn@ +          ENDOF
          ABORT" Invalid WM" ENDCASE &cell+1 ;
: dy      ( -- n ) YA ;
:noname   ( -- bit ) uoa &carry 0<>
          div @ AND wrenb @ OR ;                     is wb
:noname   ( -- bit ) wrena @ ;                       is wa
:noname   ( -- bit ) rdena @ ;                       is ra
:noname   ( -- bit ) rdenb @ ;                       is rb
: py      ( -- n ) ya ;
: wp      ( -- n ) pw @ ;
: wd      ( -- n ) dw @ ;
: rd      ( -- n ) drd @ ;
: CPctrl  ( -- n ) CPctl @ ;
DEFER opcodes DEFER miscops
: getopcd ( -- op )
0 rdena !  0 rdenb !
0 uam !  0 ubm !  0 wm !    \ default wire settings
0 aluop ! 0 acon ! 0 rex !
0 predec ! 0 postinc !
0 wrena ! 0 wrenb !
0 flush ! 0 flushIR !
0 pasel ! 0 pw !
a_yb dasel ! 0 dw !
p_bump psel !
ia_p iasel ! ib_uo ibsel !
0 xbump ! 0 yax ! 0 drd !
0 div ! 0 mul ! 0 stall !
0 bran ! 0 sub !
0 ssel ! 0 wen ! 0 repen !
0 cm ! 0 cen ! 0 uas !
0 iack ! 0 drowsy !
IR @@ 6 RSHIFT 3F AND CPctl !
IR @@ n 3 - RSHIFT 7 AND ; \ 8 main instruction types
: CPUdecode ( -- ) getopcd IR @@ &sign
IF   p_ir psel !  DROP    \ CALL: load new P
     1 ssel !             \ select RP
     1 predec ! 1 wrena ! \ push at next clock
     1 flush !            \ ignore next instruction
ELSE CELLS opcodes + @ EXECUTE process
THEN      ;
\ MISC                                      0000 cccc aaaa Sooo
: op0 IR @@ 8 bit? ssel !                 \ 0000 --aa aaaa Sooo
     IR @@ 7 AND CELLS miscops + @ EXECUTE ;
: mo0 condition flush !           \ 0000 cccc --p- Z000
      IR @@ 08 bit? drowsy !            \ sleep pending
      IR @@ 20 bit? postinc ! ;
: mo1 p_ya psel ! 
      1 rdena !
 ipl                         \ 0000 -f-- ---- s001
      IF   1 iack !                   \ acknowledge irq
     ELSE 1 postinc !                   \ RET and RETD
      THEN
      IR @@ 400 bit? flush ! ;
: mo2 IR @@ 100 bit? yax !        \ 0000 dfwy aaaa s010
      IR @@ 200 bit? wrena !
      IR @@ 400 bit? stall !
      IR @@ 400 bit? flush !
      1 rdena !
   IR @@ 800 bit?
   IF   ia_di iasel !
   ELSE ia_pi iasel !
   THEN ;
: mo3 ia_c iasel !                \ 0000 cccc aaaa s011
      1 wrena ! ;
: mo4 1 postinc !                 \ 0000 r-aa aaaa s100
      1 xbump !               \ latch XP = XP + IR[9:4]
      IR @@ 800 bit? rex ! ;
: mo5 ia_w iasel !                \ 0000 rpx- aaaa s101
      IR @@ 800 bit? rex !                \ IA(ext) = W
      IR @@ 400 bit? predec !
                        \ IA(ext) = XP
      IR @@ 200 bit? if ia_xp iasel ! then
      1 wrena ! ;
: mo6 1 flush !  1 wrena !          \ IA(ext) = literal
      IR @@ 400 bit?                   \ rp-- aaaa s110
      ia_pi iasel !
      1 rdena !
 IF IR @@ 200 bit?
      IF  1 drd !
          a_pi dasel !
          0 wrena !
     IR @@ 100 bit?
     IF   1 dw !
     ELSE 1 drd !         \ mem read
          IR @@ 00F8 AND 0A02 OR flushIR !
           THEN
       THEN
       IR @@ 800 bit? predec !
  ELSE IR @@ 800 bit? rex !
      THEN ;
: mo7 ia_p iasel !                \ 0000 rfaa aaaa s111
      p_ya psel !                      \ IA = P  P = YA
      1 rdena !
      IR @@ 800 bit? rex !
      IR @@ 400 bit? flush !
      1 wrena ! ;       \ EXECUTE = 0000 0100 0000 1111
CREATE mojmp ~ mo0 ~ mo1 ~ mo2 ~ mo3 ~ mo4 ~ mo5 ~ mo6 ~ mo7
' mojmp IS miscops
\ BRANCH                                    0001 dddd dddd dddd
: op1 1 bran ! 1 flush ! ;
\ COPROCESSOR                               0010 ???? ??pw bbbb
: op2   a_cp dasel !                      \ data mem addr = CPA
     ib_cp ibsel !
     IR @@ 10 bit? wrenb !
     IR @@ 20 bit? postinc !
     CPctl @ 40 OR CPctl !
     1 rdenb !
     1 drd ! ;
\ RSTACK                                    0011 ooaa aaaa bbbb
: op3   1 rex !  1 ssel !               \ select RP = long addr
     4 aluop !
     IR @@ 0C00 AND CASE
   0000 OF ia_uo iasel !                          \ A := B
           4 aluop !
           1 rdenb !
           1 wrena !            ENDOF
   0400 OF 1 wrenb !                              \ B := A
           1 rdena !
           ib_ya ibsel !        ENDOF
   0800 OF ia_uo iasel !          \ push B to return stack
           4 aluop !
           1 rdenb !
           1 predec !
           1 wrena !            ENDOF
   0C00 OF 1 wrenb !             \ pop B from return stack
           1 rdena !
           ib_ya ibsel !
           1 postinc !          ENDOF
ENDCASE ;
\ ARITH                                     0100 duuu aaaa bbbb
: op4   IR @@ 8 RSHIFT 7 AND aluop ! \ 0 d = A + B
     aluop @ 1 =                     \ 1 d = A - B, save CF
 IF 4 ubm ! 1 sub !              \ 2 d = A + B, save CF
        1 cm !  1 cen !              \ 3 d = A + B + CF, saveCF
     THEN                            \ 4 d = B
     aluop @ 6 AND 2 =               \ 5 d = A and B
  IF 1 cm ! 1 cen !               \ 6 d = A or B
     THEN                            \ 7 d = A xor B
     1 wen !  ia_uo iasel !
     IR @@ 800 AND
     IF   1 wrena !
     THEN
     1 rdena ! 1 rdenb !
     1 uam ! ;
\ SHIFT                                     0101 ssss aaaa bbbb
: op5   IR @@ 8 RSHIFT 0F AND ubm !
     1 rdenb !
     1 wrena !                                \ A = shift_op(B)
     IR @@ 800 AND                            \   + constant
     IF   ubm @ 2 RSHIFT cm !
          1 cen !
          1 rdena !
    ELSE ubm @ 3 AND acon !
     THEN
     1 wen !  ia_uo iasel ! ;                       \ copy to W
\ MEMORY                                    0110 oodp aaaa bbbb
: op6 IR @@ 100 bit? wrenb !              \ post inc/dec if p=1
     IR @@ 200 bit? 2* 1+ acon !              ( uo = YB +/- 1 )
     1 rdenb !
  IR @@ 800 AND IF
     1 rdena !
     IR @@ 400 AND IF                        \ 11xx = Write D
          1 dw !
ELSE 1 pasel ! 1 pw !                   \ 10xx = Write P
     1 stall ! 1 flush !
     THEN
  ELSE                                       \ Read Operation:
     IR @@ 400 AND IF
          IR @@ 8 rshift 3 and 2 <>          \ 0110 = pre-read
          IF 1 wrena ! THEN
          ia_di iasel !                      \ 01xx = Read D[b]
          1 drd !
     ELSE 1 pasel !                          \ 00xx = Read P[b]
          1 flush !
          1 stall !
                                \ xx=01 postinc
          IR @@ 8 rshift 3 and 2 =           \ xx=11 postdec
          IF 1 predec ! THEN                 \ 0010 = push P[b]
          IR @@ 00F0 AND 0602 OR flushIR !
     THEN 
  THEN ;
\ MATH                                      0111 oooo aaaa bbbb
: op7 1 rdenb !
  IR @@ 800 AND IF                        \ W operation:
     IR @@ 8 RSHIFT 3 AND wm !            \ 1-00 W=B
     4 aluop !                            \ 1-01 W=B/-2
     1 wen !  ia_uo iasel !               \ 1-10 W=(W+CF)*2+YBN
     1 cen !   ( clear carry )
  ELSE
     1 rdena !                    
     IR @@ 400 AND IF
 1 cen !
 IR @@ 200 AND IF                 \ 0110 Multiply step
     3 wm !  1 wen !              \ 0111 CRC step
     1 mul ! 1 cm !
    1 wrenb ! 08 ubm !
     IR @@ 100 AND IF
         7 aluop !                   \ XOR instead of +
         0 wm !
     THEN
 ELSE
     IR @@ 100 AND IF                 \ 0101 Div step 2
         1 div ! 1 cm !
         1 uam !
     ELSE                             \ 0100 Div step 1
         1 wrenb !
         0A ubm !
         1 cm !
         1 wen ! 2 wm !
     THEN
 THEN
     ELSE
     IR @@ 200 AND IF                     \ A = (A | swapA) & B
 IR @@ 100 bit? uas !
 5 aluop !
 1 wen ! 1 uam !
 ia_uo iasel !
 1 wrena !
ELSE
IR @@ 1C0 AND 100 = IF    \ 7100: load REP count
  1 repen !
         THEN
\ room for other instructions here.                                
        THEN
     THEN
  THEN ;
CREATE opcodz ~ op0 ~ op1 ~ op2 ~ op3 ~ op4 ~ op5 ~ op6 ~ op7
' opcodz IS opcodes
\ -------------------------------------------------------------------------------------------------------------
( synchonous processes )
: process ( -- ) CPUdecode
reset IF
  0 cf !! 0 ov !!
  0 P  !!
  0 W  !!
  0 SP !!
  0 RP !!
  0 IR !!
  0 IRQpend !!               \ clear interupt logic
  -1 intd !!
  0 sleep !!
  0 reps  !!
ELSE
  ipl IF 0 sleep !!
  ELSE drowsy @ IF 1 sleep !!
  THEN THEN
( clock the interrupt logic )
  int intd !!
  intd @@ INVERT  int AND
  IRQpend @@ OR IRQpend !!
  iack @ IF
\ decode irq and clear request
 1 ipl LSHIFT INVERT
 IRQpend @@ AND
 IRQpend !!
  THEN
  hold 0= sleep @@ 0= AND IF
    flush @
    IF   flushIR @ IR !!     \ insert a NOP or special instr.
    ELSE repeating 0=
       IF PI IR !! THEN      \ fetch next instruction
    THEN
    wen @
    IF win W !! THEN

    xen yax @ OR
   IF spin
      ssel @ IF RP ELSE SP THEN !! \ update selected ptr
    THEN
    repeating
    IF  reps @@ 1- reps !!
    ELSE pin P !!            \ next P
       repen @               \ load REP counter?
       IF  IR @@ 20 bit?
      IF    W @@ 2/
      ELSE  IR @@
           THEN  1F and reps !!
       THEN
    THEN
    cen @
    IF cin cf !!             \ latch carry and overflow
       uoa &sign+1 0<> uoa &carry 0= AND yan@ 0= AND ybn@ AND
       uoa &sign+1 0= uoa &carry 0<> AND ybn@ 0= AND yan@ AND
       OR 1 AND ov !!
    THEN
  THEN
THEN
;
\ debugging instrumentation
\ revision history:
\ 0: pre-release
\ 1: inverted CF in condition codes 2 & 3 to correct unsigned comparisons
