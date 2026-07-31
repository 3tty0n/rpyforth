\ parse-word.fs
\ 	$Id: parse-word.fs,v 1.1 2005/06/01 07:49:07 f Exp $	

\ Courtesy Anton Ertl
\ Original version is Public Domain

\ See
\ From: "Anton Ertl" <anton@mips.complang.tuwien.ac.at>
\ Subject: [forth200x] RfD: PARSE-NAME
\ To: forth200x@yahoogroups.com (forth200x)
\ Message-Id: <E1DQ7kr-0005qJ-3x@a4.complang.tuwien.ac.at>
\ Date: Mon, 25 Apr 2005 19:52:21 +0200 (CEST)

\ * Reference implementation:
\ http://www.complang.tuwien.ac.at/forth/ansforth/reference-implementations/parse-name.fs

\ : isspace? ( c -- f )   bl 1+ u< ;		\ original version

: isspace? ( c -- f )   [ bl 1+ ] literal u< ;	\ my version (slightly faster)

\ : isnotspace? ( c -- f )   isspace? 0= ;	\ original version

: isnotspace? ( c -- f )   bl u> ;		\ my version (slightly faster)


: xt-skip   ( addr1 n1 xt -- addr2 n2 ) \ gforth
    \ skip all characters satisfying xt ( c -- f )
    >r
    BEGIN
	dup
    WHILE
	over c@ r@ EXECUTE
    WHILE
	1 /string
    REPEAT  THEN
    r> drop ;

: parse-word ( "name" -- c-addr u )
    source >in @ /string
    ['] isspace? xt-skip over >r
    ['] isnotspace? xt-skip ( end-word restlen r: start-word )
    2dup 1 min + source drop - >in !
    drop r> tuck - ;
