\ conditionals.fs
\ 	$Id: conditionals.fs,v 1.3 2001/09/13 16:57:17 f Exp $	


VARIABLE conditional-token-price
(default-gene-cost#) 3 / conditional-token-price !	\ just a default

\ Define stubs for 'if' 'else' 'then' and '[if]' '[else]' '[then]'
\ (They just lend their name for the real ones).
get-current  also stubs definitions
previous	\ we must hide stub ';'
[UNDEFINED] original-: [IF] \ normal case ':' ';' not redefined:
: IF ;
: ELSE ;
: THEN ;
[ELSE] \ ':' ';' have been replaced (profiling): we must use the original ones.
    original-: IF   ;-original
    original-: ELSE ;-original
    original-: THEN ;-original
[THEN]
set-current

get-current
also genes definitions  also stubs

s" C-" ' IF as-gene
internal' IF >body
' [IF]  over >gene-evaluated-xt !
conditional-token-price @ over >gene-cost !
>gene-flags dup @ frame-pushing or swap !    

s" -" ' ELSE as-gene
internal' ELSE >body
' [ELSE]  over >gene-evaluated-xt !
conditional-token-price @ over >gene-cost !
>gene-flags dup @ frame-popping or frame-pushing or swap !    

s" -" ' THEN as-gene
internal' THEN >body
' [THEN]  over >gene-evaluated-xt !
conditional-token-price @ over >gene-cost !
>gene-flags dup @ frame-popping or swap !    

previous  previous  set-current


s" n-C"		' 0=	 as-gene
2000 to-gene-pool' 0=

s" n-C"		' 0<	 GENE-ALIAS: 0<
5000 to-gene-pool' 0<

s" n-C"		' 0>	 GENE-ALIAS: 0>
5000 to-gene-pool' 0>

s" nn-C"	' = 	 GENE-ALIAS: =
1000 to-gene-pool' =

s" nn-C"	' >	 GENE-ALIAS: >
10000 to-gene-pool' >

s" nn-C"	' <	 GENE-ALIAS: <
10000 to-gene-pool' <

s" nnn-C"	' within GENE-ALIAS: within
8000 to-gene-pool' within

s" CC-C"	' AND	 GENE-ALIAS: AND
10000 to-gene-pool' AND

s" CC-C"	' OR	 GENE-ALIAS: OR
10000 to-gene-pool' OR

s" CC-C"	' XOR	 GENE-ALIAS: XOR
10000 to-gene-pool' XOR

\ g-IF-ELSE-THEN is a pseudo gen, which is never executed,
\ but triggers insertion of a IF ELSE THEN structure.
get-current  also genes definitions
: g-IF-ELSE-THEN ;
s" C-"  ' g-IF-ELSE-THEN  as-gene
internal' g-IF-ELSE-THEN >body >gene-flags dup @ special building or or swap !
10000 to-gene-pool' g-IF-ELSE-THEN
previous  set-current
