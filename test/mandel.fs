\ Mandelbrot Set in Forth
\ Displays the Mandelbrot set as ASCII art

\ Configuration constants
80 CONSTANT WIDTH       \ Screen width in characters
40 CONSTANT HEIGHT      \ Screen height in characters
256 CONSTANT MAX-ITER   \ Maximum iterations

\ Mandelbrot viewing window
-2.5E0 FCONSTANT XMIN
 1.0E0 FCONSTANT XMAX
-1.25E0 FCONSTANT YMIN
 1.25E0 FCONSTANT YMAX

\ Compute scaling factors
XMAX XMIN F- WIDTH S>F F/ FCONSTANT XSCALE
YMAX YMIN F- HEIGHT S>F F/ FCONSTANT YSCALE

\ Variables for computation
FVARIABLE CX
FVARIABLE CY
FVARIABLE ZX
FVARIABLE ZY
FVARIABLE ZX2
FVARIABLE ZY2
FVARIABLE TEMP

: PRINTLN
    10 EMIT ;

\ Convert pixel coordinates to complex number coordinates
: pixel>complex ( col row -- ) ( F: -- cx cy )
  S>F YSCALE F* YMIN F+  CY F!
  S>F XSCALE F* XMIN F+  CX F! ;

\ Mandelbrot iteration: iterate z = z^2 + c
\ Returns number of iterations before escape (or MAX-ITER)
: mandelbrot ( -- iter )
  0.0E0 ZX F!
  0.0E0 ZY F!
  0                      ( iteration counter )
  BEGIN
    DUP MAX-ITER <       ( iter < MAX-ITER? )
  WHILE
    \ Calculate zx^2 and zy^2
    ZX F@ FDUP F*  ZX2 F!
    ZY F@ FDUP F*  ZY2 F!

    \ Check if escaped: zx^2 + zy^2 > 4.0
    ZX2 F@ ZY2 F@ F+ 4.0E0 F>
    IF
      \ Escaped - return iteration count
      EXIT
    THEN

    \ Compute new zy = 2*zx*zy + cy
    ZX F@ ZY F@ F* 2.0E0 F* CY F@ F+  ZY F!

    \ Compute new zx = zx^2 - zy^2 + cx
    ZX2 F@ ZY2 F@ F- CX F@ F+  ZX F!

    1+                   ( increment counter )
  REPEAT
;

\ Convert iteration count to ASCII character
: iter>char ( iter -- char )
  DUP MAX-ITER = IF
    DROP 32              \ Space for points in the set
  ELSE
    \ Map iterations to gradient:  .:-=+*#%@
    DUP 4 < IF
      DROP 32            \ ' '
    ELSE DUP 8 < IF
      DROP 46            \ '.'
    ELSE DUP 12 < IF
      DROP 58            \ ':'
    ELSE DUP 16 < IF
      DROP 45            \ '-'
    ELSE DUP 24 < IF
      DROP 61            \ '='
    ELSE DUP 32 < IF
      DROP 43            \ '+'
    ELSE DUP 48 < IF
      DROP 42            \ '*'
    ELSE DUP 64 < IF
      DROP 35            \ '#'
    ELSE DUP 96 < IF
      DROP 37            \ '%'
    ELSE
      64                 \ '@'
    THEN THEN THEN THEN THEN THEN THEN THEN THEN
  THEN
;

\ Draw one row of the Mandelbrot set
: draw-row ( row -- )
  WIDTH 0 DO
    I OVER pixel>complex
    mandelbrot
    iter>char
    EMIT
  LOOP
  DROP
  PRINTLN
;

\ Draw the complete Mandelbrot set
: mandelbrot-set
  PRINTLN
  ." Computing Mandelbrot set..." PRINTLN
  HEIGHT 0 DO
    I draw-row           \ Each draw-row ends with CR
  LOOP
  PRINTLN
  ." Done!" CR
;

\ Run it
." Mandelbrot Set Viewer"
." ===================" CR CR
mandelbrot-set
