\ run.fth   includes all necessary files

.( Loading run.fth ...) cr

\ -----------------------------------------------------------------------------
\ included files

decimal

: getsecs  time&date 2drop 2drop 60 * + ;
variable seconds

s" ansify.fth" included
s" xmini_oof.fth" included
s" sets.fth" included
s" shellsort.fth" included
s" syntaxtree.fth" included
s" transitiontable.fth" included
s" lexarrays.fth" included
s" savetables.fth" included
s" userinterface.fth" included

\ -----------------------------------------------------------------------------
\ User defined files

s" anstokens.fth" included                  \ Holds token values
getsecs seconds !
s" lexinput.fth" included  \ The regular expressions

\ Conditional 3600 + allows for the run straddling the hour
cr .( Time taken: )
getsecs seconds @ - dup 0< [if] 3600 + [then] . .( seconds) cr  

\ -----------------------------------------------------------------------------
cr .( Checking the output file)

variable #line

create ref-buf 132 allot
create stt-buf 132 allot

s" ref.tt"  r/o open-file throw value ref-fid
s" stt.fth" r/o open-file throw value stt-fid

: compare-files
   cr 0 #line !
   begin
      1 #line +!
      ref-buf dup 128 ref-fid read-line throw >r ( -- ca1 u1 ) ( R: -- f1 )
      stt-buf dup 128 stt-fid read-line throw    ( -- ca1 u1 ca2 u2 f2 )
      r@ and
      if compare
         if ." Error in generated file in line " #line @ . cr abort then
      else
         r@ abort" Generated file too short"
      then
      r> 0=
   until
   2drop 2drop
   ." Output file is correct" cr
;

compare-files

ref-fid close-file throw
stt-fid close-file throw

\ -----------------------------------------------------------------------------

.( run.fth completed successfully. ) .s
