\ symbols-stack.fs
\ 	$Id: symbols-stack.fs,v 1.14 2002/11/16 09:49:12 f Exp $	

s" stringbuf-0.4.fs" REQUIRED

decimal

\ The mutation process selects it's genes doing simple type checking
\ of the stack data that the genes work with.

\ It seems natural to use the stack to implement stack type checking:
\ I put symbols like 'n' for a number or 'a' for an address on the
\ symbols stack that represent the stack requirements of the genes.

\ Now we can play with these symbols like the genes would do with real data.

\ I have used the data stack for the gene symbols up to now.

\ As things evolve I'd like to use the stack more for the words that do
\ the mutation process, but having the symbols on the stack they often
\ get in the way, making things become a bit difficult.

\ Thinking about i realized, that many things ( like stack matching )
\ could probably be implemented much more efficient with string manipulations
\ than with a proper stack.
\ But I like the idea of a symbol stack too much to give it up ;-(

\ So i concluded to arrange things that i can use it both ways:
\ there is still a symbols stack, but
\ it's inner working makes it possible to perform string operations on it.

\ It can be seen both ways:

\ string:	pointer 2@ ( -- addr count )
\		(cell at pointer +2 cells holds the size of the string buffer)

\ stack:	At the address pointer refers to there is a structure:
\		stack-pointer-as-offset stack-bottom size

\ Stack items are always symbolised by bytes (chars),
\ so words working on the symbol stack can use string words to implement
\ their function.  Symbol stack matching comes to mind.
\ Symbol stack matching is important to select suitable genes during mutation.
\ Being called a lot is it worth implementing it efficient.

\ From outside it still looks like a symbol stack.

\ We can have multiple symbol stacks,
\ or a stack of symbol stacks...

\ stringbuf-0.3.fs has now s-buf's:
\ Simple buffer words without immanent allocation and size control.
\ That's the programmers job...

decimal
4096 S-BUF: symbols-stack

: open-symbol-stack ( -- )
    symbols-stack	\ opens it, if needed
    off ;		\ in case it was opened already reset count to zero

: symbols-as-string ( -- addr count )   symbols-stack s-buf>string ;

: clear-symbols ( -- )   symbols-stack s-buf-clear ;

: push-symbol-string ( addr count -- )   symbols-stack s-buf-cat ;

: #symbols-on-stack ( -- u )   symbols-stack s-buf-count ;

: drop-symbol ( -- )   -1 symbols-stack +! ;

\ Word to match wildcards in an input string.
\ Given a wildcard string and a symbol string return a buffered string
\ with the wildcards replaced.
\ All three strings have the same length.
\ Wildcards are '0' '1' '2' '3' '4' '5' '6' '7' '8' '9'.
\
\ Store the meaning of matched wildcards:
CREATE (wildcard-matches)   10 allot align  \ to store matched wildcards values

max-stack-effect S-BUF: (symbols-scratch)

: replace-in-wildcards ( addr count symbols-addr symbols-count -- addr' count')
    (symbols-scratch) s-buf-clear
    drop >r  0 >r	( addr count  r: symbols-addr 0 )

    swap		( count addr  r: symbols-addr 0 )
    BEGIN		( count addr  r: symbols-addr index )
	over r@ <> WHILE
	dup r@ + c@					\ get char from string

	dup [char] 0 [ char 9 1+ ] literal within IF	\ wildcard?
	    [char] 0 -					\ index in match field
	    (wildcard-matches) +			\ address in field
	    2r@ + c@ >r					\ real symbol
	    r@ swap !					\ store matching symbol
	    r>		( count addr real-symbol  r: symbols-addr index )
	THEN

	(symbols-scratch) s-buf-char-cat		\ build output string

	r> 1+ >r					\ next index
    REPEAT   2drop 2rdrop

    (symbols-scratch) s-buf>string ;

: symbols-match ( body addr count -- flag )
    dup IF
	dup >r
	symbols-as-string
	r@ -
	dup 0< IF 2drop 2drop drop rdrop false EXIT THEN \ not enough symbols 
	+ r>				\ cut string to relevant tail
		( body addr count symbols-addr' symbols-count' )

	4 pick >gene-flags @ symbol-wildcard AND IF
	    2over replace-in-wildcards 2>r 2>r 2drop 2r> 2r>
	THEN

	compare 0=
    ELSE
	2drop true
    THEN nip ;

: symbols-tos-match? ( c -- flag )
    symbols-as-string
    dup IF
	1- + c@ =
    ELSE
	2drop drop false
    THEN ;

\ Some testing:
false [if]

    open-symbol-stack

    s" " symbols-match cr .( empty string matches empty string? ) .
    s" 1st-item " push-symbol-string
    s" 2nd-item " push-symbol-string
    symbols-as-string cr type
    clear-symbols

    char a symbols-tos-match? cr cr .( empty stack tos matches 'a'? ) .

    cr
    s" nn" push-symbol-string
    symbols-as-string cr .( now on stack: ) type
    bl emit #symbols-on-stack .
    s" "  cr 2dup .( ') type  .( ' matche s 'nn'? ) symbols-match .
    s" n"  cr 2dup .( ') type  .( ' matches 'nn'? ) symbols-match .
    s" nn"  cr 2dup .( ') type .( ' matches 'nn'? ) symbols-match .
    s" nnn"  cr 2dup .( ') type .( ' matches 'nn'? ) symbols-match .
    s" aa"  cr 2dup .( ') type .( ' matches 'nn'? ) symbols-match .

    char a symbols-tos-match? cr .( tos matches 'a' ) .
    char n symbols-tos-match? cr .( tos matches 'n' ) .

    cr cr bye
[then]
