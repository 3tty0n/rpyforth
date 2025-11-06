\ Mandelbrot ASCII in gforth (2012 core + FLOATING)

\ --- Tunables ---------------------------------------------------------------
VARIABLE width    80 width !
VARIABLE height   40 height !
VARIABLE maxiter  64 maxiter !

-2e0 FCONSTANT x-min
 1e0 FCONSTANT x-max
-1e0 FCONSTANT y-min
 1e0 FCONSTANT y-max

FVARIABLE dx
FVARIABLE dy

\ 10 shades (lighter -> darker)
CREATE lut
  BL      C,  CHAR . C,  CHAR : C,  CHAR - C,  CHAR = C,
  CHAR + C,   CHAR * C,  CHAR # C,  CHAR % C,  CHAR @ C,

\ --- Setup ------------------------------------------------------------------
: setup ( -- )
  x-max x-min F-  width  @ 1- S>F F/  dx F!
  y-max y-min F-  height @ 1- S>F F/  dy F! ;

\ --- Iteration (z <- z^2 + c) ----------------------------------------------
FVARIABLE cx   FVARIABLE cy
FVARIABLE zx   FVARIABLE zy
FVARIABLE tmp
VARIABLE  cnt

: mandel ( f: cx cy -- u )
  cy F!  cx F!
  F0. zx F!   F0. zy F!
  0 cnt !
  BEGIN
    \ |z|^2 < 4 ?
    zx F@ zx F@ F*   zy F@ zy F@ F*  F+  4e0 F<
    cnt @ maxiter @ <
    AND
  WHILE
    \ tmp = zx*zx - zy*zy + cx
    zx F@ zx F@ F*   zy F@ zy F@ F*  F-   cx F@ F+  tmp F!
    \ zy  = 2*zx*zy + cy
    2e0 zx F@ F*  zy F@ F*  cy F@ F+  zy F!
    \ zx  = tmp
    tmp F@ zx F!
    1 cnt +!
  REPEAT
  cnt @ ;

\ --- Mapping & shading ------------------------------------------------------
: shade ( u -- c )
  10 *  maxiter @ /          \ -> index 0..(about)10
  DUP 9 > IF DROP 9 THEN     \ clamp to 0..9
  lut + C@ ;                 \ fetch char

: pixel ( ix iy -- c )
  >R                         \ save iy
  S>F dx F@ F* x-min F+      \ f: cx
  R> S>F dy F@ F* y-min F+   \ f: cx cy
  mandel shade ;

\ --- Render -----------------------------------------------------------------
: render ( -- )
  setup
  height @ 0 DO              \ outer: y
    width @ 0 DO             \ inner: x
      I J pixel EMIT
    LOOP
    CR
  LOOP ;

\ You can tweak:
\   width  120 width !   height  60 height !   maxiter 100 maxiter !
\ then run:  render

