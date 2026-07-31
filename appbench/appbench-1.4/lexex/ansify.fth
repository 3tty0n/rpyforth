\ Provides ANS compatibility for some common but non-standard words

s" [undefined]" pad c! pad char+ pad c@ move 
pad find nip 0=
[if]
   : [undefined]  ( "name" -- flag )
      bl word find nip 0=
   ; immediate
[then]

[undefined] [defined]
[if] : [defined] postpone [undefined] 0= ; immediate [then]

[undefined] -rot  [if] : -rot rot rot ; [then]

[undefined] <=    [if] : <= > 0= ; [then]

[undefined] >=    [if] : >= < 0= ; [then]

[undefined] endif [if] : endif postpone then ; immediate [then]

[undefined] on    [if] : on  ( ad -- )  -1 swap ! ; [then]

[undefined] off   [if] : off ( ad -- )   0 swap ! ; [then]

[undefined] parse-name
[if]   \ From Forth 200X web site
   : isspace? ( c -- f ) bl 1+ u< ;

   : isnotspace? ( c -- f ) isspace? 0= ;

   : xt-skip   ( addr1 n1 xt -- addr2 n2 ) \ gforth
      \ skip all characters satisfying xt ( c -- f )
      >r
      begin
         dup
      while
         over c@ r@ execute
      while
         1 /string
      repeat  then
      r> drop
   ;

   : parse-name ( "name" -- c-addr u )
      source >in @ /string
      ['] isspace? xt-skip over >r
      ['] isnotspace? xt-skip ( end-word restlen r: start-word )
      2dup 1 min + source drop - >in !
      drop r> tuck -
   ;
[then]

[undefined] 1Cell [if] 1 cells constant 1Cell [then]

