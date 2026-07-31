1 cells 4 = [if]
    requires fpmath
[then]
\ for brew
: xt>string >name count ;

\ for fcp
counter constant counter-start
: ms@ ( -- ums )
  counter counter-start - ;
