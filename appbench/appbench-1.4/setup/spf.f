: INCLUDE ;

lib/include/defer.f \ DEFER
lib/ext/case.f \ CASE 
lib/include/facil.f \ TIME&DATE
lib/include/string.f \ /STRING
lib/include/core-ext.f \ 0>
lib/include/double.f \ 2CONSTANT
lib/include/float.f
lib/ext/caseins.f

: bounds OVER + SWAP ;

\ from <h7h9mo$2ns$1@aioe.org> for fcp
\ $10
: NOTFOUND 
  OVER C@ [CHAR] $ = 
  IF BASE @ >R HEX 1 /STRING ['] ?SLITERAL CATCH R> BASE ! THROW EXIT THEN
  NOTFOUND ;
