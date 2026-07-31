\ random.fs
\ 	$Id: random.fs,v 1.8 2002/11/15 23:12:08 f Exp $	

\ ****************************************************************

decimal


VARIABLE random-xt		\ xt of actual random number generator

\ random ( from BRODIE )
VARIABLE seed-BRODIE	here seed-BRODIE !
1075118612 seed-BRODIE !   \ default to make it reproducable...

: random-BRODIE    seed-BRODIE @ 31421 * 6927 + dup seed-BRODIE ! ;
' random-BRODIE random-xt !


2VARIABLE (random-generalized)
\ generator seed (random-generalized) 2!	\ sets seed and generator
\	    seed (random-generalized) !		\ sets seed only
hex
10450405 0 (random-generalized) 2!
decimal
: random-generalized ( -- n )
    (random-generalized) 2@ swap  um* drop 1+ dup   (random-generalized) ! ;

[DEFINED] LIST: [IF]
LIST: random-generators
' random-BRODIE		random-generators >list
' random-generalized	random-generators >list
[THEN]

false [IF] page .( testing speed of random, normal and deferred) cr
    defer deferred	' random-BRODIE IS deferred
    2000000 constant iterations
    8 constant beeps

    VARIABLE xt	' random-BRODIE xt !
: execute-xt-in-variable xt @ execute ;

: t
    cr ."  normal random"
    beeps 0 DO
	iterations 0 DO
	    random-BRODIE
	    drop
	LOOP
	bell
    LOOP
    \
    cr ."  deferred"
    beeps 0 DO
	iterations 0 DO
	    deferred
	    drop
	LOOP
	bell
    LOOP
    \
    cr ."  execute-xt-in-variable"
    beeps 0 DO
	iterations 0 DO
	    execute-xt-in-variable
	    drop
	LOOP
	bell
    LOOP cr ;

t bye
[THEN]

: random-ranged ( u1 -- u2 ) random-xt @ execute   um* nip ;

\ 2addr points to a rate
\ give a random flag which is true as often as a the rate says
: rated-flag ( 2addr -- flag )  2@ random-ranged > ;


\ The floating point random generator is based on the integer one:
false [IF] \ The algorithm comes from:
    From: Wil Baden <wilbaden@netcom8.netcom.com>
    Newsgroups: comp.lang.forth
    Subject: Re: Millions and Millions of Random Numbers
    Date: 18 May 2000 14:47:54 GMT
    Message-ID: <8g0vqq$mml$1@slb1.atl.mindspring.net>
[THEN]

\ : frandom  ( f: -- 0. <= r < 1. )
\     random-xt @ EXECUTE
\     0 d>f 2.3283064365386963e-10 f* ;

: frandom  ( f: -- 0. <= r <= 1. )
    random-xt @ EXECUTE
    0 d>f 2.3283064370807974e-10 f* ;

\ Give a flag which is true as often as 'r' ( 0<= r <=1 ) says:
\ Not the fastest possible, but a standard implementation...
: f-rated-flag ( r -- flag )   frandom f- fnegate f0< ;
