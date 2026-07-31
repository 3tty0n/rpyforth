\ maybe-do.fs
\ 	$Id: maybe-do.fs,v 1.10 2005/04/23 12:41:17 f Exp $	

\ Generic functions to do something on nucs or spots based on a condition.
\ They are designed to be used by 'do-with-everybody' and the like.

\ This is quite hairy stuff intended to give a flexible user interface.
\ Brew does not use that much for it's own goals.


\ Parameters can't be passed on the stack here, so we store them in
\ scratch values.  I prefer values over variables for speed reasons, here.
\ Recording is the duty of a higher level word.
false VALUE (maybe-do-field)	\ actual base pointer of parameter field

\ Field variables to store xt's and parameters used *in* the expression
\ and in the '(do-it-xt)' words:
: MAYBE-DO-TERM: ( n -- n+1 )
    CREATE
	dup cells ,
	1+
    DOES> ( -- addr )
	@ (maybe-do-field) + ;

1 dfloats  2 cells over = [IF] \ 32 bit cell
    : MAYBE-DO-df-TERM: ( n -- n+2 )   MAYBE-DO-TERM:  1+ ;
[ELSE] \ not 32 bits
    cell over = [IF] \ 64 bit cell
	: MAYBE-DO-df-TERM: ( n -- n+1 )   MAYBE-DO-TERM: ;
    [ELSE] \ unknown cell
	page bell
	cr .( Unknown cell or dfloat size. )
	cr .( Please inform author. )
    [THEN]
[THEN] drop


0	\ index
\ Main xt's for expression, condition and ( maybe ) executed word:
MAYBE-DO-TERM: (expression-xt)
MAYBE-DO-TERM: (condition-xt)
MAYBE-DO-TERM: (simple-expression-xt)	\ it's convenient to have them separate
MAYBE-DO-TERM: (do-it-xt)
\ Variables for 'maybe-do' expressions:
MAYBE-DO-TERM: (expr-parameter)
MAYBE-DO-TERM: (expr-parameter-2)
MAYBE-DO-TERM: (expr-xt-1)
MAYBE-DO-TERM: (expr-xt-2)
MAYBE-DO-TERM: (expr-df-xt-1)		\ it's convenient to have them separate
MAYBE-DO-TERM: (expr-df-xt-2)		\ it's convenient to have them separate
\ Variables for (do-it-xt) words:
MAYBE-DO-TERM: (xt-do-it)
MAYBE-DO-TERM: (df-xt-do-it)		\ it's convenient to have them separate
MAYBE-DO-TERM: (do-it-parameter)
MAYBE-DO-TERM: (do-it-parameter-2)
MAYBE-DO-TERM: (do-it-scale)		\ double cell
1+
\ Sometimes it's convenient to store the type of maybe-do/maybe-do-simple:
\ Possible values:
\	' maybe-do
\	' maybe-do-simple
MAYBE-DO-TERM: (maybe-do-type-xt)	\ not always used, set it the same...
MAYBE-DO-TERM: (expression-handle)	\ buffer for evaluating expressions
MAYBE-DO-TERM: (maybe-do-handle)	\ buffer for evaluating actions

cells dfaligned cell /			\ dfalign float area

MAYBE-DO-df-TERM: (expr-df-parameter)
MAYBE-DO-df-TERM: (expr-df-parameter-2)
MAYBE-DO-df-TERM: (do-it-df-parameter)
MAYBE-DO-df-TERM: (do-it-df-parameter-2)

cells CONSTANT maybe-do-field-length#

: maybe? ( -- flag )
    (expression-xt) @ EXECUTE
    (condition-xt) @  EXECUTE ;

: maybe-do ( -- )
    maybe? IF
	(do-it-xt) @ EXECUTE
    THEN ;

: maybe-do-simple ( -- )
    (simple-expression-xt) @ EXECUTE
    IF
	(do-it-xt) @  EXECUTE
    THEN ;

LIST: simple-expressions-nuc
' true simple-expressions-nuc	   >list
' selected? simple-expressions-nuc >list
' on-trial? simple-expressions-nuc >list
' fertile? simple-expressions-nuc  >list
' will-die? simple-expressions-nuc >list
nuc-floats# [IF]
' nuc-all-real?     simple-expressions-nuc >list
' nuc-has-unreal?   simple-expressions-nuc >list
' nuc-with-inf?     simple-expressions-nuc >list
' nuc-with-neg-inf? simple-expressions-nuc >list
' nuc-with-pos-inf? simple-expressions-nuc >list
' nuc-with-nan?     simple-expressions-nuc >list
[THEN]

LIST: simple-expressions-spot
: everywhere ( -- TRUE )   true ;
' everywhere simple-expressions-spot >list

: inhabited? ( -- flag )       spot @ someone-here? ;	\ flag *not* normalised
' inhabited? simple-expressions-spot >list

: empty? ( -- flag )   inhabited? 0= ;
' empty? simple-expressions-spot >list

spot-floats# [IF]
' spot-all-real?     simple-expressions-spot >list
' spot-has-unreal?   simple-expressions-spot >list
' spot-with-inf?     simple-expressions-spot >list
' spot-with-neg-inf? simple-expressions-spot >list
' spot-with-pos-inf? simple-expressions-spot >list
' spot-with-nan?     simple-expressions-spot >list
[THEN]

: maybe-do-generic ( -- )
    (maybe-do-type-xt) @ EXECUTE ;

: generic-maybe? ( -- flag )
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF
	    (simple-expression-xt) @ EXECUTE
	ENDOF
	['] maybe-do	    OF
	    maybe?
	ENDOF
    ENDCASE ;

LIST: maybe-do-expressions

\ words to put in (expression), integers:
: 2-variables ( -- n1 n2 )
    (expr-xt-1) @ EXECUTE @
    (expr-xt-2) @ EXECUTE @ ;
' 2-variables maybe-do-expressions >list

: variable-number ( -- n1 n2 )
    (expr-xt-1) @ EXECUTE @
    (expr-parameter) @ ;
' variable-number maybe-do-expressions >list

: variable-within ( -- within-flag true )
    (expr-xt-1) @ EXECUTE @
    (expr-parameter) @  (expr-parameter-2) @  within  true ;
' variable-within maybe-do-expressions >list

: function-number ( -- n1 n2 )
    (expr-xt-1) @ EXECUTE
    (expr-parameter) @ ;
' function-number maybe-do-expressions >list

: function-within ( -- within-flag true )
    (expr-xt-1) @ EXECUTE
    (expr-parameter) @  (expr-parameter-2) @  within  true ;
' function-within maybe-do-expressions >list

\ words to put in (expression), floating point:
: 2-df-variables ( -- n1 n2 )
    (expr-df-xt-1) @ EXECUTE df@
    (expr-df-xt-2) @ EXECUTE df@ ;
' 2-df-variables maybe-do-expressions >list

: df-variable-number ( -- n1 n2 )
    (expr-df-xt-1) @ EXECUTE df@
    (expr-df-parameter) df@ ;
' df-variable-number maybe-do-expressions >list

: df-variable-within ( -- within-flag true )
    (expr-df-xt-1) @ EXECUTE df@
    (expr-df-parameter) df@  (expr-df-parameter-2) df@  fwithin  true ;
' df-variable-within maybe-do-expressions >list

: df-function-number ( -- n1 n2 )
    (expr-df-xt-1) @ EXECUTE
    (expr-df-parameter) df@ ;
' df-function-number maybe-do-expressions >list

: df-function-within ( -- within-flag true )
    (expr-df-xt-1) @ EXECUTE
    (expr-df-parameter) df@  (expr-df-parameter-2) df@  fwithin  true ;
' df-function-within maybe-do-expressions >list

: df-var-real? ( -- flag TRUE  )
    (expr-df-xt-1) @ EXECUTE df@ real? TRUE ;
' df-var-real? maybe-do-expressions >list

: df-var-inf? ( -- flag TRUE  )
    (expr-df-xt-1) @ EXECUTE df@ infinity? 0= 0= TRUE ;
' df-var-inf? maybe-do-expressions >list

: df-var-pos-inf? ( -- flag TRUE  )
    (expr-df-xt-1) @ EXECUTE df@ infinity? 1 = TRUE ;
' df-var-pos-inf? maybe-do-expressions >list

: df-var-neg-inf? ( -- flag TRUE  )
    (expr-df-xt-1) @ EXECUTE df@ infinity? -1 = TRUE ;
' df-var-neg-inf? maybe-do-expressions >list

: df-var-nan? ( -- flag TRUE  )
    (expr-df-xt-1) @ EXECUTE df@ is-NaN? TRUE ;
' df-var-nan? maybe-do-expressions >list

\ Words related to evaluating strings in (do-it-xt):
: (evaluate-do) ( -- )   (maybe-do-handle) @ string@ EVALUATE ;

: evaluate-do ( -- )
    depth fdepth 2>r
    ['] (evaluate-do) CATCH
    depth 1- fdepth 2r@ d= 0= or
    IF
	bell
	page
	cr ." evaluate-do: Error evaluating string."
	cr (maybe-do-handle) @ string@ type

	depth r@ - dup IF
	    cr cr ." This function should not alter stack depth."
	    cr ." Number of stack parameters was off by "
	    dup 0> IF [char] + emit THEN dup . cr
	    dup 0> IF
		0 DO drop LOOP
		." Integer parameters drop'ed."
	    ELSE
		drop
		cr ." Situation can not be fixed!"
	    THEN
	ELSE drop THEN

	fdepth 2r@ drop - dup IF
	    cr cr ." This function should not alter float stack depth."
	    cr ." Number of float stack parameters was off by "
	    dup 0> IF [char] + emit THEN dup . cr
	    dup 0> IF
		0 DO fdrop LOOP
		." Floating point parameters drop'ed."
	    ELSE
		drop
		cr ." Situation can not be fixed!"
	    THEN
	ELSE drop THEN

	(maybe-do-handle) @ stringbuf-empty
	cr ." String cleared."
	1000 ms bell
	wait
    THEN
    2rdrop ;
\ ' evaluate-do do-it-xt's >list	\ later on

\ Buffer for very simle string input:
here  c-l  dup allot  2CONSTANT (evaluate-scratch-pointer)
0 VALUE (evaluate-count)
\ Very, very simple word to "edit" evaluate buffer
: write-evaluate-buffer ( addr count -- addr count' )
    >r dup r>
    page
    blue color-background
    ." Give string to evaluate: "
    s" ( Please use very carefully! )." type-alert
    clear-line-to-end
    cr cr
    accept ;	

: write-evaluate-buffer-do ( maybe-do-field-body -- )
    TO (maybe-do-field)
    (evaluate-scratch-pointer) write-evaluate-buffer
    (maybe-do-handle) @ string! ;

: write-evaluate-buffer-expr ( maybe-do-field-body -- )
    TO (maybe-do-field)
    (evaluate-scratch-pointer) write-evaluate-buffer
    (expression-handle) @ string! ;

: (evaluate-expr) ( -- )   (expression-handle) @ string@ EVALUATE ;

: evaluate-expr ( -- )
    depth >r
    ['] (evaluate-expr) CATCH	\ Catching evaluate errors:
    depth r@ - 3 - or IF
	page
	bell
	cr ." evaluate-expr: Error evaluating string.    "
	cr (expression-handle) @ string@ type

	depth r@ - 2 - dup IF
	    cr cr ." Number of integer stack parameters was off by "
	    dup 0> IF [char] + emit THEN dup . cr
	    dup 0> IF
		0 DO drop LOOP
		." Integer parameters drop'ed."
	    ELSE
		drop
		cr ." Situation can not be fixed!"
	    THEN
	ELSE drop THEN

	(expression-handle) @ stringbuf-empty

	\ We must give the parameters for the coming expression:
	\ An empty string would crash the next invocation, as the
	\ condition would not find the expected stack items.
	\ So we must set a dummy string, forcing a 'FALSE' result:

	(maybe-do-type-xt) @ CASE
	    ['] maybe-do-simple OF s" false" ENDOF
	    ['] maybe-do OF
		(condition-xt) @ CASE
		    ['] <  OF s" 99 0"  ENDOF
		    ['] >  OF s" 0 99"  ENDOF
		    ['] =  OF s" 0 99"  ENDOF
		    ['] <> OF s" 99 99" ENDOF
		    true ABORT" evaluate-expr: Couldn't fix error, unknown condition."
		ENDCASE
	    ENDOF
	    true ABORT" evaluate-expr: Couldn't fix error, unknown do type."
	ENDCASE
	2dup (expression-handle) @ cat		\ manipulate string
	cr ." String manipulated to force a 'FALSE' result: "
	type ."    " cr
	1000 ms bell
	wait
	RECURSE		\ to give condition parameters this time already.
    THEN
    rdrop ;
' evaluate-expr maybe-do-expressions >list

: evaluate-df-expr ( -- )
    fdepth >r
    ['] (evaluate-expr) CATCH	\ Catching evaluate errors:
    fdepth r@ - 2 - or IF
	page
	bell
	cr ." evaluate-df-expr: Error evaluating string.    "
	cr (expression-handle) @ string@ type

	fdepth r@ - 2 - dup IF
	    cr cr ." Number of floating point stack parameters was off by "
	    dup 0> IF [char] + emit THEN dup . cr
	    dup 0> IF
		0 DO fdrop LOOP
		." Float parameters drop'ed."
	    ELSE
		drop
		cr ." Situation can not be fixed!"
	    THEN
	ELSE drop THEN

	(expression-handle) @ stringbuf-empty

	\ We must give the parameters for the coming expression:
	\ An empty string would crash the next invocation, as the
	\ condition would not find the expected stack items.
	\ So we must set a dummy string, forcing a 'FALSE' result:

	(maybe-do-type-xt) @ CASE
	    ['] maybe-do-simple OF s" false" ENDOF
	    ['] maybe-do OF
		(condition-xt) @ CASE
		    ['] f<  OF s" 99e0 0e0"  ENDOF
		    ['] f>  OF s" 0e0 99e0"  ENDOF
		    ['] f=  OF s" 0e0 99e0"  ENDOF
		    ['] f<> OF s" 99e0 99e0" ENDOF
		    true ABORT" evaluate-df-expr: Couldn't fix error, unknown condition."
		ENDCASE
	    ENDOF
	    true ABORT" evaluate-df-expr: Couldn't fix error, unknown do type."
	ENDCASE
	2dup (expression-handle) @ cat		\ manipulate string
	cr ." String manipulated to force a 'FALSE' result: "
	type ."    " cr
	1000 ms bell
	wait
	RECURSE		\ to give condition parameters this time already.
    THEN
    rdrop ;
' evaluate-df-expr maybe-do-expressions >list

LIST: do-it-xt's  \ words to put in (do-it-xt), usable on nucs *and* spots 

: set-variable ( -- )   (do-it-parameter) @  (xt-do-it) @ EXECUTE ! ;
' set-variable do-it-xt's >list

: add-to-variable ( -- )
    (do-it-parameter) @  (xt-do-it) @ EXECUTE +! ;
' add-to-variable do-it-xt's >list

: sub-from-variable ( -- )
    (do-it-parameter) @ negate  (xt-do-it) @ EXECUTE +! ;
' sub-from-variable do-it-xt's >list

: scale-variable ( -- )
    (xt-do-it) @ EXECUTE dup @  (do-it-scale) 2@ */  swap ! ;
' scale-variable do-it-xt's >list

: set-df-variable ( -- )
    (do-it-df-parameter) df@  (df-xt-do-it) @ EXECUTE df! ;
' set-df-variable do-it-xt's >list

: add-to-df-variable ( -- )
    (do-it-df-parameter) df@  (df-xt-do-it) @ EXECUTE df+! ;
' add-to-df-variable do-it-xt's >list

: sub-from-df-variable ( -- )
    (do-it-df-parameter) df@ fnegate  (df-xt-do-it) @ EXECUTE df+! ;
' sub-from-df-variable do-it-xt's >list

: multiply-df-variable ( -- )
    (df-xt-do-it) @ EXECUTE >r
    (do-it-df-parameter) df@ r@ df@ f* r> df! ;
' multiply-df-variable do-it-xt's >list

' evaluate-do do-it-xt's >list	\ I want it here in the list

LIST: condition-words
' <	condition-words >list
' <=	condition-words >list
' >	condition-words >list
' >=	condition-words >list
' =	condition-words >list
' <>	condition-words >list
' f<	condition-words >list
' f<=	condition-words >list
' f>	condition-words >list
' f>=	condition-words >list
' f=	condition-words >list
' f<>	condition-words >list

\ Float to string word used for maybe-string:
\ : f>$ ( F:r -- addr count )   12 float>string ;
: f>$ ( F:r -- addr count )   8 float>string ;

: maybe-string ( -- handle )	\ please close buffer!
    c-l 2/ stringbuf-open >r
    (expression-xt) @ CASE
	['] 2-variables  OF
	    (expr-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-xt-2) @ xt>string		r@ cat
	ENDOF
	['] variable-number OF
	    (expr-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-parameter) @ num>string	r@ cat
	ENDOF
	['] function-number OF
	    (expr-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-parameter) @ num>string	r@ cat
	ENDOF
	['] variable-within OF
	    (expr-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-parameter) @ num>string	r@ cat
	    bl					r@ char-cat
	    (expr-parameter-2) @ num>string	r@ cat
	    s"  WITHIN true "			r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] function-within OF
	    (expr-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-parameter) @ num>string	r@ cat
	    bl					r@ char-cat
	    (expr-parameter-2) @ num>string	r@ cat
	    s"  WITHIN true "			r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] evaluate-expr OF
	    (expression-handle) @ string@	r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] 2-df-variables  OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-df-xt-2) @ xt>string		r@ cat
	ENDOF
	['] df-variable-number OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter) df@ f>$		r@ cat
	ENDOF
	['] df-variable-within OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter) df@ f>$		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter-2) df@ f>$	r@ cat
	    s"  WITHIN true "			r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-function-number OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter) df@ f>$		r@ cat
	ENDOF
	['] df-function-within OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter) df@ f>$		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter-2) df@ f>$	r@ cat
	    s"  WITHIN true "			r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] evaluate-df-expr OF
	    (expression-handle) @ string@	r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-var-real? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  real? "				r@ cat
	ENDOF
	['] df-var-inf? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  infinity? "			r@ cat
	ENDOF
	['] df-var-pos-inf? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  +inf f= "			r@ cat
	ENDOF
	['] df-var-neg-inf? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  -inf f= "			r@ cat
	ENDOF
	['] df-var-nan? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  is-NaN? "			r@ cat
	ENDOF

	s" maybe-string: Can't handle "		r@ cat
	xt>string				r@ cat
	s"   "					r@ cat
    ENDCASE
    r> ;

\ Handle a buffer with the maybe condition as Forth code:
\ The code is not used for evaluation, but for logging and such.
\ (Floats are not displayed too accurately).
: maybe-FORTH-string ( -- handle )	\ please close buffer!
    c-l 2/ stringbuf-open >r
    (expression-xt) @ CASE
	['] 2-variables  OF
	    (expr-xt-1) @ xt>string		r@ cat
	    s"  @ "				r@ cat
	    (expr-xt-2) @ xt>string		r@ cat
	    s"  @ "				r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] variable-number OF
	    (expr-xt-1) @ xt>string		r@ cat
	    s"  @ "				r@ cat
	    (expr-parameter) @ num>string	r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] function-number OF
	    (expr-xt-1) @ xt>string		r@ cat
	    s"  "				r@ cat
	    (expr-parameter) @ num>string	r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] variable-within OF
	    (expr-xt-1) @ xt>string		r@ cat
	    s"  @ "				r@ cat
	    (expr-parameter) @ num>string	r@ cat
	    bl					r@ char-cat
	    (expr-parameter-2) @ num>string	r@ cat
	    s" within true "			r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] function-within OF
	    (expr-xt-1) @ xt>string		r@ cat
	    s"  "				r@ cat
	    (expr-parameter) @ num>string	r@ cat
	    bl					r@ char-cat
	    (expr-parameter-2) @ num>string	r@ cat
	    s" within true "			r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] evaluate-expr OF
	    (expression-handle) @ string@	r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] 2-df-variables  OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  df@ "				r@ cat
	    (expr-df-xt-2) @ xt>string		r@ cat
	    s"  df@ "				r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-variable-number OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  df@ "				r@ cat
	    (expr-df-parameter) df@ f>$		r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-variable-within OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  df@ "				r@ cat
	    (expr-df-parameter) df@ f>$		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter-2) df@ f>$	r@ cat
	    s" within true "			r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-function-number OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter) df@ f>$		r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-function-within OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter) df@ f>$		r@ cat
	    bl					r@ char-cat
	    (expr-df-parameter-2) df@ f>$	r@ cat
	    s" fwithin true "			r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] evaluate-df-expr OF
	    (expression-handle) @ string@	r@ cat
	    bl					r@ char-cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-var-real? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  df@ real? true "		r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-var-inf? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  df@ infinity? 0= 0= true "	r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-var-pos-inf? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  df@ +inf f= true "		r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-var-neg-inf? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  df@ -inf f= true "		r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF
	['] df-var-nan? OF
	    (expr-df-xt-1) @ xt>string		r@ cat
	    s"  df@ is-NaN? true "		r@ cat
	    (condition-xt) @ xt>string		r@ cat
	ENDOF

	s" maybe-FORTH-string: Can't handle "	r@ cat
	xt>string				r@ cat
	s"   "					r@ cat
    ENDCASE
    r> ;

: maybe-simple-string ( -- handle )	\ please close buffer!
    c-l 2/ stringbuf-open
    (simple-expression-xt) @ xt>string third cat ;

: maybe-generic-string ( -- handle )	\ please close buffer!
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF
	    maybe-simple-string
	ENDOF
	['] maybe-do	    OF
	    maybe-string
	ENDOF
	true ABORT" maybe-generic-string: Unknown type."
    ENDCASE ;

\ Handle a string buffer describing maybe do action:
: maybe-action-string ( -- handle )	\ please close buffer
    c-l stringbuf-open >r

    (do-it-xt) @ CASE
	\ cases without further parameters:
	['] noop		OF (do-it-xt) @ xt>string r@ cat ENDOF
	['] remove-nuc		OF (do-it-xt) @ xt>string r@ cat ENDOF
	['] select-nuc		OF (do-it-xt) @ xt>string r@ cat ENDOF
	['] de-select-nuc	OF (do-it-xt) @ xt>string r@ cat ENDOF
	['] toggle-selection	OF (do-it-xt) @ xt>string r@ cat ENDOF
	\ cases that work on a nuc variable:
	['] set-variable	OF
	    (do-it-parameter) @ num>string	r@ cat	bl r@ char-cat
	    (xt-do-it) @ xt>string		r@ cat
	    s"  !"
	ENDOF
	['] add-to-variable	OF
	    (do-it-parameter) @ num>string	r@ cat	bl r@ char-cat
	    (xt-do-it) @ xt>string		r@ cat
	    s"  +!"
	ENDOF
	['] sub-from-variable	OF
	    (do-it-parameter) @ num>string	r@ cat
	    s"  negate "			r@ cat
	    (xt-do-it) @ xt>string		r@ cat
	    s"  +!"
	ENDOF
	['] scale-variable	OF
	    (xt-do-it) @ xt>string		r@ cat
	    s"  @ "				r@ cat
	    (do-it-scale) 2@ swap num>string	r@ cat	bl r@ char-cat
	    num>string				r@ cat
	    s"  */ "				r@ cat
	    (xt-do-it) @ xt>string		r@ cat
	    s"  !"				r@ cat
	ENDOF
	['] set-df-variable	OF
	    (do-it-df-parameter) df@ f>$	r@ cat	bl r@ char-cat
	    (df-xt-do-it) @ xt>string		r@ cat
	    s"  df!"
	ENDOF
	['] add-to-df-variable	OF
	    (do-it-df-parameter) df@ f>$	r@ cat	bl r@ char-cat
	    (df-xt-do-it) @ xt>string		r@ cat
	    s"  df+!"
	ENDOF
	['] sub-from-df-variable OF
	    (do-it-df-parameter) df@ f>$	r@ cat
	    s"  fnegate "			r@ cat
	    (df-xt-do-it) @ xt>string		r@ cat
	    s"  df+!"
	ENDOF
	['] multiply-df-variable OF
	    (df-xt-do-it) @ xt>string		r@ cat
	    s"  df@ "				r@ cat
	    (do-it-df-parameter) df@ f>$	r@ cat
	    s"  df@ f*  "			r@ cat
	    (df-xt-do-it) @ xt>string		r@ cat
	    s"  df!"				r@ cat
	ENDOF
	['] evaluate-do		OF
	    (maybe-do-handle) @ string@ r@ cat
	ENDOF
	cr ." maybe-action-string: Unknown action xt: "
	(do-it-xt) @ xt>string type cr
	true ABORT
    ENDCASE
    r> ;

\ Handle a string buffer describing maybe do type, condition and action:
\ Used for logging user actions.
: all-maybe-string ( -- handle )	\ please close buffer
    c-l stringbuf-open >r

    (maybe-do-type-xt) @ xt>string		r@ string!
    s" :  "					r@ cat
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF
	    maybe-simple-string
	ENDOF
	['] maybe-do	    OF
	    maybe-FORTH-string
	ENDOF
	true ABORT" all-maybe-string: Unknown type."
    ENDCASE
    dup string@	r@ cat
    stringbuf-close

    s"  IF  "					r@ cat
    maybe-action-string dup string@	r@ cat
    stringbuf-close
    s"   THEN"					r@ cat
    r> ;


: maybe-do-with-everybody ( do-xt -- )
    (do-it-xt)  dup @ >r  !
    ['] maybe-do do-with-everybody
    r> (do-it-xt) ! ;

\ Set '(simple-expression-xt)' before.
: simple-maybe-do-with-everybody ( do-xt -- )
    (do-it-xt)  dup @ >r  !
    ['] maybe-do-simple do-with-everybody
    r> (do-it-xt) ! ;

\ Top level generic function *not* recording:
: maybe-do-on-everybody-generic ( maybe-do-field-xt -- )
    EXECUTE			\ make maybe-do-field actual
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF
	    ['] maybe-do-simple do-with-everybody
	ENDOF
	['] maybe-do	    OF
	    ['] maybe-do do-with-everybody
	ENDOF
	true ABORT" maybe-do-on-everybody-generic: Unknown type."
    ENDCASE ;

\ Same, but recording:
defer ?record-?do-everybody-generic ( maybe-do-field-xt -- maybe-do-field-xt )
: |maybe-do-on-everybody-generic| ( maybe-do-field-xt -- )
    log-user? IF
	dup EXECUTE
	s" " 0 log
	s" user did |maybe-do-on-everybody-generic|" 0 log
	all-maybe-string dup string@ 0 log
	stringbuf-close
	s" " 0 log
    THEN
    ?record-?do-everybody-generic

    maybe-do-on-everybody-generic ;

\ Do something on all spots (sequential)
\ If the spot is inhabited do cp!
: do-everywhere-maybe-nuc ( xt -- )
    (do-everywhere-xt) !
    spot @ >r	cp@ >r			\ maybe it's worth saving?
    spots 0 DO				\ loop over all spots
	i >spot!
	inhabited? IF fcp @ cp! THEN
	(do-everywhere-xt) @ EXECUTE	\ and do your job
    LOOP
    r> cp!	r> >spot! ;		\ you never know what's good for...


\ I needed these two works for spot scans.
\ (consider using 'maybe-do-everywhere-generic' instead where possible).
true [IF] \ not for nucs:

\ set '(simple-expression-xt)' before.
: simple-maybe-do-everywhere ( do-xt -- )
    (do-it-xt)  dup @ >r  !
    ['] maybe-do-simple do-everywhere
    r> (do-it-xt) ! ;

: maybe-do-everywhere ( do-xt -- )
    (do-it-xt)  dup @ >r  !
    ['] maybe-do do-everywhere
    r> (do-it-xt) ! ;

[ELSE] \ can work on nucs too:	DISABLED!

\ set '(simple-expression-xt)' before.
: simple-maybe-do-everywhere ( do-xt -- )
    (do-it-xt)  dup @ >r  !
    ['] maybe-do-simple do-everywhere-maybe-nuc
    r> (do-it-xt) ! ;

: maybe-do-everywhere ( do-xt -- )
    (do-it-xt)  dup @ >r  !
    ['] maybe-do do-everywhere-maybe-nuc
    r> (do-it-xt) ! ;

[THEN]

\ Top level generic function *not* recording:
\ This version temporally sets cp on inhabited spots.
: maybe-do-everywhere-generic ( maybe-do-field-xt -- )
    EXECUTE			\ make maybe-do-field actual
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF
	    ['] maybe-do-simple do-everywhere-maybe-nuc
	ENDOF
	['] maybe-do	    OF
	    ['] maybe-do do-everywhere-maybe-nuc
	ENDOF
	true ABORT" maybe-do-everywhere-generic: Unknown type."
    ENDCASE ;

\ Same, but recording:
defer ?record-?do-everywhere-generic ( maybe-do-field-xt -- maybe-do-field-xt )
: |maybe-do-everywhere-generic| ( maybe-do-field-xt -- )
    ?record-?do-everywhere-generic
    maybe-do-everywhere-generic ;

\ Same, working on current field, but taking do-xt from stack (Handy for scans)
\ Temporally sets nuc on inhabited spots.
\ Don't use for recording.
: maybe-do-this-everywhere ( do-xt -- )
    (do-it-xt)  dup @ >r  !
    ['] noop maybe-do-everywhere-generic	\ ;-)
    r> (do-it-xt) ! ;

FVARIABLE (no-one)	0e0 (no-one) f!		\ indicating missing selection

: MAYBE-DO-FIELD: ( "name"  -- )
    CREATE
	here dfaligned TO (maybe-do-field)	\ handy for initialisation
	maybe-do-field-length# dfaligned allot

	\ Default initialization (can be changed later):
	['] variable-number (expression-xt) !
	['] = (condition-xt) !
	['] false (simple-expression-xt) !
	['] noop (do-it-xt) !
	['] (no-one) (expr-xt-1) !
	['] (no-one) (expr-xt-2) !
	['] (no-one) (expr-df-xt-1) !
	['] (no-one) (expr-df-xt-2) !
	['] (none) (xt-do-it) !
	['] (none) (df-xt-do-it) !
	1 1 (do-it-scale) 2!

	[ decimal ] 32 dup			\ open two evaluation buffers
	stringbuf-open (expression-handle) !	\ expression string buffer
	stringbuf-open (maybe-do-handle) !	\ action string buffer

	['] maybe-do-simple (maybe-do-type-xt) !	\ used only sometimes
    DOES> ( -- )
	dfaligned TO (maybe-do-field) ;

\ Preserve al maybe-do settings of the current field and pass a handle
\ to be used by 'restore-maybe-do-field'.
: preserve-maybe-do-field ( -- address=handle )
    maybe-do-field-length# 2 cells +	\ field and handles
    allocate
    ABORT" preserve-maybe-do-field: Couldn't allocate."
    (maybe-do-field) over maybe-do-field-length# move

    \ Save clones of the expression buffers:
    (expression-handle) @ string@
    dup stringbuf-open >r
    r@ string!					\ clone expression buffer
    r>  over maybe-do-field-length# + !		\ save handle

    (maybe-do-handle) @ string@
    dup stringbuf-open >r
    r@ string!					\ clone maybe-do evaluation buf
    r>  over maybe-do-field-length# + cell+ ! ;	\ save handle

\ Set all data (including evaluation buffers) of the *current* field to the
\ values preserved by 'preserve-maybe-do-field'.
\ Free memory and close buffers used to store the data.
: restore-maybe-do-field ( address=handle -- )
    dup (maybe-do-field) maybe-do-field-length# move

    dup maybe-do-field-length# + @
    dup string@  (expression-handle) @  string!
    stringbuf-close

    dup maybe-do-field-length# + cell+ @
    dup string@  (maybe-do-handle) @  string!
    stringbuf-close

    free ABORT" restore-maybe-do-field: Could not free." ;

: init-df-expr-xts-nuc ( -- )
[ nuc-floats# ] [IF]
    nuc-floats# CASE
	0 OF  ENDOF
	1 OF
	    0 n'th-df-nuc-var-xt
	    dup (expr-df-xt-1) !  (expr-df-xt-2) !
	ENDOF
	0 n'th-df-nuc-var-xt (expr-df-xt-1) !
	1 n'th-df-nuc-var-xt (expr-df-xt-2) !
    ENDCASE
[THEN] ;

: init-df-do-xts-nuc ( -- )
[ nuc-floats# ] [IF]
    0 n'th-df-nuc-var-xt (df-xt-do-it) !
[THEN] ;


: init-df-expr-xts-spot ( -- )
[ spot-floats# ] [IF]
    spot-floats# CASE
	0 OF  ENDOF
	1 OF
	    0 n'th-spot-f-var-xt
	    dup (expr-df-xt-1) !  (expr-df-xt-2) !
	ENDOF
	0 n'th-spot-f-var-xt (expr-df-xt-1) !
	1 n'th-spot-f-var-xt (expr-df-xt-2) !
    ENDCASE
[THEN] ;

: init-df-do-xts-spot ( -- )
[ spot-floats# ] [IF]
    0 n'th-spot-f-var-xt (df-xt-do-it) !
[THEN] ;

: (no-nuc-variable?) ( xt -- flag )
    dup spot-var-xts listed? IF drop TRUE  EXIT THEN
    dup nuc-var-xts listed?  IF drop FALSE bell EXIT THEN
    dup ['] noop =           IF drop FALSE EXIT THEN
    cr ." check-ok-for-spot-do?: Unknown variable: "
    xt>string type
    true ABORT" (no-nuc-variable?): Fix code first..." ;

\ Check if maybe do on spots would not use nuc parameters on empty spots:
\ Check do action:
: check-ok-for-spot-do? ( -- flag=TRUE|FALSE|1 )
    (do-it-xt) @
    dup  ['] evaluate-do = IF drop 1 EXIT THEN \ do at own risk...

    dup do-it-xt's listed? 0= IF drop FALSE EXIT THEN

    CASE
	['] set-variable	OF
	    (xt-do-it) @ (no-nuc-variable?) EXIT
	ENDOF
	['] add-to-variable	OF
	    (xt-do-it) @ (no-nuc-variable?) EXIT
	ENDOF
	['] sub-from-variable	OF
	    (xt-do-it) @ (no-nuc-variable?) EXIT
	ENDOF
	['] scale-variable	OF
	    (xt-do-it) @ (no-nuc-variable?) EXIT
	ENDOF
	['] set-df-variable	OF
	    (df-xt-do-it) @ (no-nuc-variable?) EXIT
	ENDOF
	['] add-to-df-variable	OF
	    (df-xt-do-it) @ (no-nuc-variable?) EXIT
	ENDOF
	['] sub-from-df-variable OF
	    (df-xt-do-it) @ (no-nuc-variable?) EXIT
	ENDOF
	['] multiply-df-variable OF
	    (df-xt-do-it) @ (no-nuc-variable?) EXIT
	ENDOF
	['] noop		OF false EXIT ENDOF
	true ABORT" check-ok-for-spot-do?: Unknown action."
    ENDCASE
    drop FALSE ;

\ Check condition and maybe do action:
: check-ok-for-spot-maybe? ( -- flag=TRUE|FALSE|1 )
    (maybe-do-type-xt) @ CASE
	['] maybe-do-simple OF
	    (simple-expression-xt) @ CASE		\ exception: nuc ok
		['] inhabited? OF true EXIT ENDOF
		(do-it-xt) @ ['] evaluate-do = IF	\ do at own risk
		    drop 1 EXIT
		THEN
	    ENDCASE
	ENDOF
	['] maybe-do	    OF
	    (expression-xt) @ ['] evaluate-expr = IF
		1 EXIT
	    THEN
	ENDOF
	true ABORT" check-ok-for-spot-do?: Unknown type."
    ENDCASE
    check-ok-for-spot-do? ;

\ Do something (like a subset menu) based on a simple expression
\ in a maybe-do-on-subset-field.  Restore the field.
: do-in-simple-subset ( do-xt simple-expression-xt field-xt -- )
    (maybe-do-field) >r
    EXECUTE					\ set field active
    preserve-maybe-do-field >r

    ['] maybe-do-simple (maybe-do-type-xt) !
    (simple-expression-xt) !

    EXECUTE					\ do the menu (or whatever)

    r> restore-maybe-do-field
    r> to (maybe-do-field) ;

\ Do something (like a subset menu) in a subset field checking value types
\ in a given dfloat variable.  Use the given field and restore it.
: do-in-float-type-subset ( do-xt df-nuc-var-xt type-test-xt field-xt -- )
    (maybe-do-field) >r
    EXECUTE					\ activate field
    preserve-maybe-do-field >r

    ['] maybe-do (maybe-do-type-xt) !
    ['] = (condition-xt) !
    (expression-xt) !  (expr-df-xt-1) !

    EXECUTE

    r> restore-maybe-do-field
    r> to (maybe-do-field) ;

\ Do something in a subset where a dfloat variable has a certain value:
\ Use and restore the given field.
: do-in-dfloat-value-subset ( do-xt addr-df-value df-nuc-var-xt field-xt -- )
    (maybe-do-field) >r
    EXECUTE					\ activate field
    preserve-maybe-do-field >r

    ['] maybe-do (maybe-do-type-xt) !
    ['] df-variable-number (expression-xt) !
    (expr-df-xt-1) !
    df@ (expr-df-parameter) df!
    ['] f= (condition-xt) !

    EXECUTE

    r> restore-maybe-do-field
    r> to (maybe-do-field) ;

\ Do something in a subset where an integer variable has a certain value:
\ ( maybe-do-on-subset-field used and restored).
: do-in-int-value-subset ( do-xt value nuc-var-xt field-xt -- )
    (maybe-do-field) >r
    EXECUTE					\ activate field
    preserve-maybe-do-field >r

    ['] maybe-do (maybe-do-type-xt) !
    ['] variable-number (expression-xt) !
    (expr-xt-1) !
    (expr-parameter) !
    ['] = (condition-xt) !

    EXECUTE

    r> restore-maybe-do-field
    r> to (maybe-do-field) ;

