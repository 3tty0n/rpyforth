\ basic-stack.fs
\ 	$Id: basic-stack.fs,v 1.3 2005/04/05 11:20:44 f Exp $	


\ For things like 2dup we need a whole zoo of type variants
\ horrible, but...  let's tolerate it for the moment

\ To allow user input I define all type variants as alternatives,
\ including those which are not visible to mutation.

s" n-nn"  ' dup  GENE-ALIAS: dup
6000 to-gene-pool' dup

s" a-aa" ' dup  GENE-ALIAS: dup(a)
\ 4000 to-gene-pool' dup(a)	\ not in the pool, but defined for user input
as-alternative'' dup(a) dup


s" nn-nnnn" ' 2dup  GENE-ALIAS: 2dup
2000 to-gene-pool' 2dup

s" na-nana" ' 2dup  GENE-ALIAS: 2dup(na)
\ 1000 to-gene-pool' 2dup(na)			\ only for user input
as-alternative'' 2dup(na) 2dup

s" aa-aaaa" ' 2dup  GENE-ALIAS: 2dup(aa)
\ 1000 to-gene-pool' 2dup(aa)			\ only for user input
as-alternative'' 2dup(aa) 2dup

s" an-anan" ' 2dup  GENE-ALIAS: 2dup(an)
\ 1000 to-gene-pool' 2dup(an)			\ only for user input
as-alternative'' 2dup(an) 2dup


s" n-" ' drop  GENE-ALIAS: drop
2000 to-gene-pool' drop

s" a-" ' drop  GENE-ALIAS: drop(a-)
2000 to-gene-pool' drop(a-)
as-alternative'' drop(a-) drop

\ Mutation type 'top-level-skip-IF-ELSE-branch' needs drop(C-)
internal'? drop(C-) 0= [IF]
s" C-" ' drop  GENE-ALIAS: drop(C-)
[THEN]
0 to-gene-pool' drop(C-)
as-alternative'' drop(C-) drop

s" nn-n" ' nip  GENE-ALIAS: nip
2000 to-gene-pool' nip

s" aa-a" ' nip  GENE-ALIAS: nip(aa-a)
2000 to-gene-pool' nip(aa-a)
as-alternative'' nip(aa-a) nip

s" an-n" ' nip  GENE-ALIAS: nip(an-n)
2000 to-gene-pool' nip(an-n)
as-alternative'' nip(an-n) nip

s" na-a" ' nip  GENE-ALIAS: nip(na-a)
2000 to-gene-pool' nip(na-a)
as-alternative'' nip(na-a) nip

s" nn-nnn" ' tuck  GENE-ALIAS: tuck
4000 to-gene-pool' tuck

s" aa-aaa" ' tuck  GENE-ALIAS: tuck(aa-aaa)
\ 1000 to-gene-pool' tuck(aa-aaa)			\ only for user input
as-alternative'' tuck(aa-aaa) tuck

s" na-ana" ' tuck  GENE-ALIAS: tuck(na-ana)
\ 1000 to-gene-pool' tuck(na-ana)			\ only for user input
as-alternative'' tuck(na-ana) tuck

s" an-nan" ' tuck  GENE-ALIAS: tuck(an-nan)
\ 2000 to-gene-pool' tuck(an-nan)			\ only for user input
as-alternative'' tuck(an-nan) tuck


s" nn-nn" ' swap  GENE-ALIAS: swap
8000 to-gene-pool' swap

s" aa-aa" ' swap  GENE-ALIAS: swap(aa-aa)
\ 8000 to-gene-pool' swap(aa-aa)			\ only for user input
as-alternative'' swap(aa-aa) swap

s" nn-nnn" ' over  GENE-ALIAS: over
7000 to-gene-pool' over

s" na-nan" ' over  GENE-ALIAS: over(na-nan)
\ 7000 to-gene-pool' over(na-nan)			\ only for user input
as-alternative'' over(na-nan) over

s" an-ana" ' over  GENE-ALIAS: over(an-ana)
6000 to-gene-pool' over(an-ana)
as-alternative'' over(an-ana) over

s" aa-aaa" ' over  GENE-ALIAS: over(aa-aaa)
\ 4000 to-gene-pool' over(aa-aaa)			\ only for user input
as-alternative'' over(aa-aaa) over
