\ store-normalised.fs
\ 	$Id: store-normalised.fs,v 1.2 2001/03/21 07:00:59 f Exp $	

\ simple attempt to scale and/or normalize integer values
\ to deal with integer overflow when adding and so.

decimal
1024 VALUE gene-normalize-max		\ VALUE's are faster than variables

\ a word to normalize integers somehow
true [if]

gene-normalize-max 2/ VALUE gene-normalize/2
gene-normalize/2   2/ VALUE gene-normalize/4

: some ( n -- n' )
    dup 0< >r	( n  r: sign )

    abs
    dup gene-normalize/2 > IF
	2/ gene-normalize/4 +		\ increases half as steep
	abs gene-normalize-max min	\ just cut at abs gene-normalize-max
    THEN

    r> IF negate THEN ;
[else]		\ simple version
: some ( n -- n' )
    dup 0< >r	( n  r: sign )
    abs gene-normalize-max min	\ just cut at abs gene-normalize-max
    r> IF negate THEN ;
[then]

false [if] 	\ some tests
    cr 0 some .
    512 some .
    1024 some .
    9999 some .
    cr
    -512 some .
    -1024 some .
    -9999 some .
    cr
[then]

: $$ ( n X -- n' X )   >r some r> ;	\ normalizes *second* stack item

get-current  also genes  definitions

: !(some) ( n a -- )   $$ ! ;
s" na-"  ' !(some)  as-gene
10000 to-gene-pool' !(some)

: +!(some) ( n a -- )  $$ +! ;
s" na-"  ' +!(some)  as-gene
10000 to-gene-pool' +!(some)

: -!(some) ( n a -- )  swap some negate swap +! ;
s" na-"  ' -!(some)  as-gene
10000 to-gene-pool' -!(some)

: swap!(some) ( a n -- )   some swap ! ;
s" an-"  ' swap!(some)  as-gene
10000 to-gene-pool' swap!(some)

\ ok, it's not storing but fetching, but might be interesting anyway:
\ taking an normalized amount off at a spezified address.
: take-some ( a -- n )   dup >r @ some dup negate r> +! ;
s" a-n"  ' take-some  as-gene
10000 to-gene-pool' take-some

previous  set-current
