\ block-var-speed-test.fs

\ You must copy this to the brew directory to have the path right
\ ( Some FORTHs do not seem to like ../paths...  ;-)

\ ****************************************************************
\ Some timings:

\  \ GForth 0.5.0	gforth
\  Comparing speed of block-VARIABLEs with ordinary VARIABLEs
\  using  allocate-memory  to get memory blocks

\  test-blocked-vars       1000000 iterations
\  real    0m26.070s
\  user    0m26.070s
\  sys     0m0.000s

\  test-variables          1000000 iterations
\  real    1m9.485s
\  user    1m9.480s
\  sys     0m0.010s

\  test-values             1000000 iterations
\  real    0m48.853s
\  user    0m48.790s
\  sys     0m0.010s


\  \ GForth 0.5.0	gforth-fast
\  Comparing speed of block-VARIABLEs with ordinary VARIABLEs
\  using  allocate-memory  to get memory blocks

\  test-blocked-vars       1000000 iterations
\  real    0m23.585s
\  user    0m23.590s
\  sys     0m0.000s

\  test-variables          1000000 iterations
\  real    1m7.909s
\  user    1m7.780s
\  sys     0m0.010s

\  test-values             1000000 iterations
\  real    0m42.270s
\  user    0m42.230s
\  sys     0m0.000s


\  \ ANS bigFORTH 386-Linux rev. 2.0.3
\  Comparing speed of block-VARIABLEs with ordinary VARIABLEs
\  using  ALLOCATE-MEMORY  to get memory blocks

\  test-blocked-vars       1000000 iterations
\  real    0m0.732s
\  user    0m0.730s
\  sys     0m0.010s

\  test-variables          1000000 iterations
\  real    0m0.732s
\  user    0m0.730s
\  sys     0m0.000s

\  test-values             1000000 iterations
\  real    0m1.092s
\  user    0m1.090s
\  sys     0m0.000s
    

\  \ Portable Forth Environment 0.30.97 (Jul 13 2001 17:10:17)
\  Comparing speed of block-VARIABLEs with ordinary VARIABLEs
\  using  allocate-memory  to get memory blocks

\  test-blocked-vars       1000000 iterations
\  real    0m29.279s
\  user    0m29.260s
\  sys     0m0.010s

\  test-variables          1000000 iterations
\  real    0m29.105s
\  user    0m29.080s
\  sys     0m0.010s

\  test-values             1000000 iterations
\  real    0m23.398s
\  user    0m23.380s
\  sys     0m0.020s
    

\  This is probably *without* the better cache consistency for block-vars!

\ ****************************************************************



: cvs" ( "CVS ID" -- addr count )
    [char] " word count swap 6 + swap 12 - 2 max POSTPONE sliteral ; IMMEDIATE

\ You must copy this to the brew directory to have the path right
\ ( Some FORTHs do not seem to like ../paths...  ;-)

s" system-dependent.fs" INCLUDED
s" compile-options.fs" INCLUDED
s" basics.fs" INCLUDED
s" lists.fs" INCLUDED
s" stringbuf-0.4.fs" INCLUDED
s" screen-size.fs" INCLUDED
s" block-variables.fs" INCLUDED

page
cr
.( Comparing speed of block-VARIABLEs with ordinary VARIABLEs )
cr
.( using  )  open-memory-block-xt @ xt>string type  .(   to get memory blocks)
cr

init-var-block
block-VARIABLE: b1 
block-VARIABLE: b2 
block-VARIABLE: b3 
block-VARIABLE: b4 
block-VARIABLE: b5 
block-VARIABLE: b6 
block-VARIABLE: b7 
block-VARIABLE: b8 
block-VARIABLE: b9 
block-VARIABLE: b0 
block-VARIABLE: ba1
block-VARIABLE: ba2
block-VARIABLE: ba3
block-VARIABLE: ba4
block-VARIABLE: ba5
block-VARIABLE: ba6
block-VARIABLE: ba7
block-VARIABLE: ba8
block-VARIABLE: ba9
block-VARIABLE: ba0
block-VARIABLE: bb1
block-VARIABLE: bb2
block-VARIABLE: bb3
block-VARIABLE: bb4
block-VARIABLE: bb5
block-VARIABLE: bb6
block-VARIABLE: bb7
block-VARIABLE: bb8
block-VARIABLE: bb9
block-VARIABLE: bb0
block-VARIABLE: bx1
block-VARIABLE: bx2
block-VARIABLE: bx3
block-VARIABLE: bx4
block-VARIABLE: bx5
block-VARIABLE: bx6
block-VARIABLE: bx7
block-VARIABLE: bx8
block-VARIABLE: bx9
block-VARIABLE: bx0
block-VARIABLE: bc1
block-VARIABLE: bc2
block-VARIABLE: bc3
block-VARIABLE: bc4
block-VARIABLE: bc5
block-VARIABLE: bc6
block-VARIABLE: bc7
block-VARIABLE: bc8
block-VARIABLE: bc9
block-VARIABLE: bc0
block-VARIABLE: bd1
block-VARIABLE: bd2
block-VARIABLE: bd3
block-VARIABLE: bd4
block-VARIABLE: bd5
block-VARIABLE: bd6
block-VARIABLE: bd7
block-VARIABLE: bd8
block-VARIABLE: bd9
block-VARIABLE: bd0
block-VARIABLE: be1
block-VARIABLE: be2
block-VARIABLE: be3
block-VARIABLE: be4
block-VARIABLE: be5
block-VARIABLE: be6
block-VARIABLE: be7
block-VARIABLE: be8
block-VARIABLE: be9
block-VARIABLE: be0
block-VARIABLE: bf1
block-VARIABLE: bf2
block-VARIABLE: bf3
block-VARIABLE: bf4
block-VARIABLE: bf5
block-VARIABLE: bf6
block-VARIABLE: bf7
block-VARIABLE: bf8
block-VARIABLE: bf9
block-VARIABLE: bf0
block-VARIABLE: bg1
block-VARIABLE: bg2
block-VARIABLE: bg3
block-VARIABLE: bg4
block-VARIABLE: bg5
block-VARIABLE: bg6
block-VARIABLE: bg7
block-VARIABLE: bg8
block-VARIABLE: bg9
block-VARIABLE: bg0
block-VARIABLE: bh1
block-VARIABLE: bh2
block-VARIABLE: bh3
block-VARIABLE: bh4
block-VARIABLE: bh5
block-VARIABLE: bh6
block-VARIABLE: bh7
block-VARIABLE: bh8
block-VARIABLE: bh9
block-VARIABLE: bh0
define-block-variables

b1  off
b2  off
b3  off
b4  off
b5  off
b6  off
b7  off
b8  off
b9  off
b0  off
ba1 off
ba2 off
ba3 off
ba4 off
ba5 off
ba6 off
ba7 off
ba8 off
ba9 off
ba0 off
bb1 off
bb2 off
bb3 off
bb4 off
bb5 off
bb6 off
bb7 off
bb8 off
bb9 off
bb0 off
bx1 off
bx2 off
bx3 off
bx4 off
bx5 off
bx6 off
bx7 off
bx8 off
bx9 off
bx0 off
bc1 off
bc2 off
bc3 off
bc4 off
bc5 off
bc6 off
bc7 off
bc8 off
bc9 off
bc0 off
bd1 off
bd2 off
bd3 off
bd4 off
bd5 off
bd6 off
bd7 off
bd8 off
bd9 off
bd0 off
be1 off
be2 off
be3 off
be4 off
be5 off
be6 off
be7 off
be8 off
be9 off
be0 off
bf1 off
bf2 off
bf3 off
bf4 off
bf5 off
bf6 off
bf7 off
bf8 off
bf9 off
bf0 off
bg1 off
bg2 off
bg3 off
bg4 off
bg5 off
bg6 off
bg7 off
bg8 off
bg9 off
bg0 off
bh1 off
bh2 off
bh3 off
bh4 off
bh5 off
bh6 off
bh7 off
bh8 off
bh9 off
bh0 off

VARIABLE v1		v1  off	
VARIABLE v2 		v2  off
VARIABLE v3 		v3  off
VARIABLE v4 		v4  off
VARIABLE v5 		v5  off
VARIABLE v6 		v6  off
VARIABLE v7 		v7  off
VARIABLE v8 		v8  off
VARIABLE v9 		v9  off
VARIABLE v0 		v0  off
VARIABLE va1		va1 off
VARIABLE va2		va2 off
VARIABLE va3		va3 off
VARIABLE va4		va4 off
VARIABLE va5		va5 off
VARIABLE va6		va6 off
VARIABLE va7		va7 off
VARIABLE va8		va8 off
VARIABLE va9		va9 off
VARIABLE va0		va0 off
VARIABLE vb1		vb1 off
VARIABLE vb2		vb2 off
VARIABLE vb3		vb3 off
VARIABLE vb4		vb4 off
VARIABLE vb5		vb5 off
VARIABLE vb6		vb6 off
VARIABLE vb7		vb7 off
VARIABLE vb8		vb8 off
VARIABLE vb9		vb9 off
VARIABLE vb0		vb0 off
VARIABLE vx1		vx1 off
VARIABLE vx2		vx2 off
VARIABLE vx3		vx3 off
VARIABLE vx4		vx4 off
VARIABLE vx5		vx5 off
VARIABLE vx6		vx6 off
VARIABLE vx7		vx7 off
VARIABLE vx8		vx8 off
VARIABLE vx9		vx9 off
VARIABLE vx0		vx0 off
VARIABLE vc1		vc1 off
VARIABLE vc2		vc2 off
VARIABLE vc3		vc3 off
VARIABLE vc4		vc4 off
VARIABLE vc5		vc5 off
VARIABLE vc6		vc6 off
VARIABLE vc7		vc7 off
VARIABLE vc8		vc8 off
VARIABLE vc9		vc9 off
VARIABLE vc0		vc0 off
VARIABLE vd1		vd1 off
VARIABLE vd2		vd2 off
VARIABLE vd3		vd3 off
VARIABLE vd4		vd4 off
VARIABLE vd5		vd5 off
VARIABLE vd6		vd6 off
VARIABLE vd7		vd7 off
VARIABLE vd8		vd8 off
VARIABLE vd9		vd9 off
VARIABLE vd0		vd0 off
VARIABLE ve1		ve1 off
VARIABLE ve2		ve2 off
VARIABLE ve3		ve3 off
VARIABLE ve4		ve4 off
VARIABLE ve5		ve5 off
VARIABLE ve6		ve6 off
VARIABLE ve7		ve7 off
VARIABLE ve8		ve8 off
VARIABLE ve9		ve9 off
VARIABLE ve0		ve0 off
VARIABLE vf1		vf1 off
VARIABLE vf2		vf2 off
VARIABLE vf3		vf3 off
VARIABLE vf4		vf4 off
VARIABLE vf5		vf5 off
VARIABLE vf6		vf6 off
VARIABLE vf7		vf7 off
VARIABLE vf8		vf8 off
VARIABLE vf9		vf9 off
VARIABLE vf0		vf0 off
VARIABLE vg1		vg1 off
VARIABLE vg2		vg2 off
VARIABLE vg3		vg3 off
VARIABLE vg4		vg4 off
VARIABLE vg5		vg5 off
VARIABLE vg6		vg6 off
VARIABLE vg7		vg7 off
VARIABLE vg8		vg8 off
VARIABLE vg9		vg9 off
VARIABLE vg0		vg0 off
VARIABLE vh1		vh1 off
VARIABLE vh2		vh2 off
VARIABLE vh3		vh3 off
VARIABLE vh4		vh4 off
VARIABLE vh5		vh5 off
VARIABLE vh6		vh6 off
VARIABLE vh7		vh7 off
VARIABLE vh8		vh8 off
VARIABLE vh9		vh9 off
VARIABLE vh0		vh0 off

0 VALUE VAL-1 
0 VALUE VAL-2 
0 VALUE VAL-3 
0 VALUE VAL-4 
0 VALUE VAL-5 
0 VALUE VAL-6 
0 VALUE VAL-7 
0 VALUE VAL-8 
0 VALUE VAL-9 
0 VALUE VAL-0 
0 VALUE VAL-a1
0 VALUE VAL-a2
0 VALUE VAL-a3
0 VALUE VAL-a4
0 VALUE VAL-a5
0 VALUE VAL-a6
0 VALUE VAL-a7
0 VALUE VAL-a8
0 VALUE VAL-a9
0 VALUE VAL-a0
0 VALUE VAL-b1
0 VALUE VAL-b2
0 VALUE VAL-b3
0 VALUE VAL-b4
0 VALUE VAL-b5
0 VALUE VAL-b6
0 VALUE VAL-b7
0 VALUE VAL-b8
0 VALUE VAL-b9
0 VALUE VAL-b0
0 VALUE VAL-x1
0 VALUE VAL-x2
0 VALUE VAL-x3
0 VALUE VAL-x4
0 VALUE VAL-x5
0 VALUE VAL-x6
0 VALUE VAL-x7
0 VALUE VAL-x8
0 VALUE VAL-x9
0 VALUE VAL-x0
0 VALUE VAL-c1
0 VALUE VAL-c2
0 VALUE VAL-c3
0 VALUE VAL-c4
0 VALUE VAL-c5
0 VALUE VAL-c6
0 VALUE VAL-c7
0 VALUE VAL-c8
0 VALUE VAL-c9
0 VALUE VAL-c0
0 VALUE VAL-d1
0 VALUE VAL-d2
0 VALUE VAL-d3
0 VALUE VAL-d4
0 VALUE VAL-d5
0 VALUE VAL-d6
0 VALUE VAL-d7
0 VALUE VAL-d8
0 VALUE VAL-d9
0 VALUE VAL-d0
0 VALUE VAL-e1
0 VALUE VAL-e2
0 VALUE VAL-e3
0 VALUE VAL-e4
0 VALUE VAL-e5
0 VALUE VAL-e6
0 VALUE VAL-e7
0 VALUE VAL-e8
0 VALUE VAL-e9
0 VALUE VAL-e0
0 VALUE VAL-f1
0 VALUE VAL-f2
0 VALUE VAL-f3
0 VALUE VAL-f4
0 VALUE VAL-f5
0 VALUE VAL-f6
0 VALUE VAL-f7
0 VALUE VAL-f8
0 VALUE VAL-f9
0 VALUE VAL-f0
0 VALUE VAL-g1
0 VALUE VAL-g2
0 VALUE VAL-g3
0 VALUE VAL-g4
0 VALUE VAL-g5
0 VALUE VAL-g6
0 VALUE VAL-g7
0 VALUE VAL-g8
0 VALUE VAL-g9
0 VALUE VAL-g0
0 VALUE VAL-h1
0 VALUE VAL-h2
0 VALUE VAL-h3
0 VALUE VAL-h4
0 VALUE VAL-h5
0 VALUE VAL-h6
0 VALUE VAL-h7
0 VALUE VAL-h8
0 VALUE VAL-h9
0 VALUE VAL-h0


100 VALUE iterations
8 value times

: test-blocked-vars
    cr ." test-blocked-vars 	" iterations . ." iterations"
    iterations 0 DO
	b1  @ i + b1  !  b1  off
	b2  @ i + b2  !  b2  off
	b3  @ i + b3  !  b3  off
	b4  @ i + b4  !  b4  off
	b5  @ i + b5  !  b5  off		
	b6  @ i + b6  !  b6  off
	b7  @ i + b7  !  b7  off
	b8  @ i + b8  !  b8  off
	b9  @ i + b9  !  b9  off
	b0  @ i + b0  !  b0  off
	ba1 @ i + ba1 !  ba1 off
	ba2 @ i + ba2 !  ba2 off
	ba3 @ i + ba3 !  ba3 off
	ba4 @ i + ba4 !  ba4 off
	ba5 @ i + ba5 !  ba5 off
	ba6 @ i + ba6 !  ba6 off
	ba7 @ i + ba7 !  ba7 off
	ba8 @ i + ba8 !  ba8 off
	ba9 @ i + ba9 !  ba9 off
	ba0 @ i + ba0 !  ba0 off
	bb1 @ i + bb1 !  bb1 off
	bb2 @ i + bb2 !  bb2 off
	bb3 @ i + bb3 !  bb3 off
	bb4 @ i + bb4 !  bb4 off
	bb5 @ i + bb5 !  bb5 off
	bb6 @ i + bb6 !  bb6 off
	bb7 @ i + bb7 !  bb7 off
	bb8 @ i + bb8 !  bb8 off
	bb9 @ i + bb9 !  bb9 off
	bb0 @ i + bb0 !  bb0 off
	bx1 @ i + bx1 !  bx1 off
	bx2 @ i + bx2 !  bx2 off
	bx3 @ i + bx3 !  bx3 off
	bx4 @ i + bx4 !  bx4 off
	bx5 @ i + bx5 !  bx5 off
	bx6 @ i + bx6 !  bx6 off
	bx7 @ i + bx7 !  bx7 off
	bx8 @ i + bx8 !  bx8 off
	bx9 @ i + bx9 !  bx9 off
	bx0 @ i + bx0 !  bx0 off
	bc1 @ i + bc1 !  bc1 off
	bc2 @ i + bc2 !  bc2 off
	bc3 @ i + bc3 !  bc3 off
	bc4 @ i + bc4 !  bc4 off
	bc5 @ i + bc5 !  bc5 off
	bc6 @ i + bc6 !  bc6 off
	bc7 @ i + bc7 !  bc7 off
	bc8 @ i + bc8 !  bc8 off
	bc9 @ i + bc9 !  bc9 off
	bc0 @ i + bc0 !  bc0 off
	bd1 @ i + bd1 !  bd1 off
	bd2 @ i + bd2 !  bd2 off
	bd3 @ i + bd3 !  bd3 off
	bd4 @ i + bd4 !  bd4 off
	bd5 @ i + bd5 !  bd5 off
	bd6 @ i + bd6 !  bd6 off
	bd7 @ i + bd7 !  bd7 off
	bd8 @ i + bd8 !  bd8 off
	bd9 @ i + bd9 !  bd9 off
	bd0 @ i + bd0 !  bd0 off
	be1 @ i + be1 !  be1 off
	be2 @ i + be2 !  be2 off
	be3 @ i + be3 !  be3 off
	be4 @ i + be4 !  be4 off
	be5 @ i + be5 !  be5 off
	be6 @ i + be6 !  be6 off
	be7 @ i + be7 !  be7 off
	be8 @ i + be8 !  be8 off
	be9 @ i + be9 !  be9 off
	be0 @ i + be0 !  be0 off
	bf1 @ i + bf1 !  bf1 off
	bf2 @ i + bf2 !  bf2 off
	bf3 @ i + bf3 !  bf3 off
	bf4 @ i + bf4 !  bf4 off
	bf5 @ i + bf5 !  bf5 off
	bf6 @ i + bf6 !  bf6 off
	bf7 @ i + bf7 !  bf7 off
	bf8 @ i + bf8 !  bf8 off
	bf9 @ i + bf9 !  bf9 off
	bf0 @ i + bf0 !  bf0 off
	bg1 @ i + bg1 !  bg1 off
	bg2 @ i + bg2 !  bg2 off
	bg3 @ i + bg3 !  bg3 off
	bg4 @ i + bg4 !  bg4 off
	bg5 @ i + bg5 !  bg5 off
	bg6 @ i + bg6 !  bg6 off
	bg7 @ i + bg7 !  bg7 off
	bg8 @ i + bg8 !  bg8 off
	bg9 @ i + bg9 !  bg9 off
	bg0 @ i + bg0 !  bg0 off
	bh1 @ i + bh1 !  bh1 off
	bh2 @ i + bh2 !  bh2 off
	bh3 @ i + bh3 !  bh3 off
	bh4 @ i + bh4 !  bh4 off
	bh5 @ i + bh5 !  bh5 off
	bh6 @ i + bh6 !  bh6 off
	bh7 @ i + bh7 !  bh7 off
	bh8 @ i + bh8 !  bh8 off
	bh9 @ i + bh9 !  bh9 off
	bh0 @ i + bh0 !  bh0 off
    LOOP ;

: test-variables
    cr ." test-variables 		" iterations . ." iterations"
    iterations 0 DO
	v1  @ i +  v1 !    v1   off
	v2  @ i +  v2 !    v2 	 off
	v3  @ i +  v3 !    v3 	 off
	v4  @ i +  v4 !    v4 	 off
	v5  @ i +  v5 !    v5 	 off
	v6  @ i +  v6 !    v6 	 off
	v7  @ i +  v7 !    v7 	 off
	v8  @ i +  v8 !    v8 	 off
	v9  @ i +  v9 !    v9 	 off
	v0  @ i +  v0 !    v0 	 off
	va1 @ i +  va1     !    va1	 off
	va2 @ i +  va2     !    va2	 off
	va3 @ i +  va3     !    va3	 off
	va4 @ i +  va4     !    va4	 off
	va5 @ i +  va5     !    va5	 off
	va6 @ i +  va6     !    va6	 off
	va7 @ i +  va7     !    va7	 off
	va8 @ i +  va8     !    va8	 off
	va9 @ i +  va9     !    va9	 off
	va0 @ i +  va0     !    va0	 off
	vb1 @ i +  vb1     !    vb1	 off
	vb2 @ i +  vb2     !    vb2	 off
	vb3 @ i +  vb3     !    vb3	 off
	vb4 @ i +  vb4     !    vb4	 off
	vb5 @ i +  vb5     !    vb5	 off
	vb6 @ i +  vb6     !    vb6	 off
	vb7 @ i +  vb7     !    vb7	 off
	vb8 @ i +  vb8     !    vb8	 off
	vb9 @ i +  vb9     !    vb9	 off
	vb0 @ i +  vb0     !    vb0	 off
	vx1 @ i +  vx1     !    vx1	 off
	vx2 @ i +  vx2     !    vx2	 off
	vx3 @ i +  vx3     !    vx3	 off
	vx4 @ i +  vx4     !    vx4	 off
	vx5 @ i +  vx5     !    vx5	 off
	vx6 @ i +  vx6     !    vx6	 off
	vx7 @ i +  vx7     !    vx7	 off
	vx8 @ i +  vx8     !    vx8	 off
	vx9 @ i +  vx9     !    vx9	 off
	vx0 @ i +  vx0     !    vx0	 off
	vc1 @ i +  vc1     !    vc1	 off
	vc2 @ i +  vc2     !    vc2	 off
	vc3 @ i +  vc3     !    vc3	 off
	vc4 @ i +  vc4     !    vc4	 off
	vc5 @ i +  vc5     !    vc5	 off
	vc6 @ i +  vc6     !    vc6	 off
	vc7 @ i +  vc7     !    vc7	 off
	vc8 @ i +  vc8     !    vc8	 off
	vc9 @ i +  vc9     !    vc9	 off
	vc0 @ i +  vc0     !    vc0	 off
	vd1 @ i +  vd1     !    vd1	 off
	vd2 @ i +  vd2     !    vd2	 off
	vd3 @ i +  vd3     !    vd3	 off
	vd4 @ i +  vd4     !    vd4	 off
	vd5 @ i +  vd5     !    vd5	 off
	vd6 @ i +  vd6     !    vd6	 off
	vd7 @ i +  vd7     !    vd7	 off
	vd8 @ i +  vd8     !    vd8	 off
	vd9 @ i +  vd9     !    vd9	 off
	vd0 @ i +  vd0     !    vd0	 off
	ve1 @ i +  ve1     !    ve1	 off
	ve2 @ i +  ve2     !    ve2	 off
	ve3 @ i +  ve3     !    ve3	 off
	ve4 @ i +  ve4     !    ve4	 off
	ve5 @ i +  ve5     !    ve5	 off
	ve6 @ i +  ve6     !    ve6	 off
	ve7 @ i +  ve7     !    ve7	 off
	ve8 @ i +  ve8     !    ve8	 off
	ve9 @ i +  ve9     !    ve9	 off
	ve0 @ i +  ve0     !    ve0	 off
	vf1 @ i +  vf1     !    vf1	 off
	vf2 @ i +  vf2     !    vf2	 off
	vf3 @ i +  vf3     !    vf3	 off
	vf4 @ i +  vf4     !    vf4	 off
	vf5 @ i +  vf5     !    vf5	 off
	vf6 @ i +  vf6     !    vf6	 off
	vf7 @ i +  vf7     !    vf7	 off
	vf8 @ i +  vf8     !    vf8	 off
	vf9 @ i +  vf9     !    vf9	 off
	vf0 @ i +  vf0     !    vf0	 off
	vg1 @ i +  vg1     !    vg1	 off
	vg2 @ i +  vg2     !    vg2	 off
	vg3 @ i +  vg3     !    vg3	 off
	vg4 @ i +  vg4     !    vg4	 off
	vg5 @ i +  vg5     !    vg5	 off
	vg6 @ i +  vg6     !    vg6	 off
	vg7 @ i +  vg7     !    vg7	 off
	vg8 @ i +  vg8     !    vg8	 off
	vg9 @ i +  vg9     !    vg9	 off
	vg0 @ i +  vg0     !    vg0	 off
	vh1 @ i +  vh1     !    vh1	 off
	vh2 @ i +  vh2     !    vh2	 off
	vh3 @ i +  vh3     !    vh3	 off
	vh4 @ i +  vh4     !    vh4	 off
	vh5 @ i +  vh5     !    vh5	 off
	vh6 @ i +  vh6     !    vh6	 off
	vh7 @ i +  vh7     !    vh7	 off
	vh8 @ i +  vh8     !    vh8	 off
	vh9 @ i +  vh9     !    vh9	 off
	vh0 @ i +  vh0     !    vh0      off
    LOOP ;

: test-values
    cr ." test-values 		" iterations . ." iterations"
    iterations 0 DO
	v1  @ i +  v1 !    v1   off
	VAL-1   i + TO   VAL-1       0 TO  VAL-1 
	VAL-2   i + TO   VAL-2       0 TO  VAL-2 
	VAL-3   i + TO   VAL-3       0 TO  VAL-3 
	VAL-4   i + TO   VAL-4       0 TO  VAL-4 
	VAL-5   i + TO   VAL-5       0 TO  VAL-5 
	VAL-6   i + TO   VAL-6       0 TO  VAL-6 
	VAL-7   i + TO   VAL-7       0 TO  VAL-7 
	VAL-8   i + TO   VAL-8       0 TO  VAL-8 
	VAL-9   i + TO   VAL-9       0 TO  VAL-9 
	VAL-0   i + TO   VAL-0       0 TO  VAL-0 
	VAL-a1  i + TO   VAL-a1      0 TO  VAL-a1
	VAL-a2  i + TO   VAL-a2      0 TO  VAL-a2
	VAL-a3  i + TO   VAL-a3      0 TO  VAL-a3
	VAL-a4  i + TO   VAL-a4      0 TO  VAL-a4
	VAL-a5  i + TO   VAL-a5      0 TO  VAL-a5
	VAL-a6  i + TO   VAL-a6      0 TO  VAL-a6
	VAL-a7  i + TO   VAL-a7      0 TO  VAL-a7
	VAL-a8  i + TO   VAL-a8      0 TO  VAL-a8
	VAL-a9  i + TO   VAL-a9      0 TO  VAL-a9
	VAL-a0  i + TO   VAL-a0      0 TO  VAL-a0
	VAL-b1  i + TO   VAL-b1      0 TO  VAL-b1
	VAL-b2  i + TO   VAL-b2      0 TO  VAL-b2
	VAL-b3  i + TO   VAL-b3      0 TO  VAL-b3
	VAL-b4  i + TO   VAL-b4      0 TO  VAL-b4
	VAL-b5  i + TO   VAL-b5      0 TO  VAL-b5
	VAL-b6  i + TO   VAL-b6      0 TO  VAL-b6
	VAL-b7  i + TO   VAL-b7      0 TO  VAL-b7
	VAL-b8  i + TO   VAL-b8      0 TO  VAL-b8
	VAL-b9  i + TO   VAL-b9      0 TO  VAL-b9
	VAL-b0  i + TO   VAL-b0      0 TO  VAL-b0
	VAL-x1  i + TO   VAL-x1      0 TO  VAL-x1
	VAL-x2  i + TO   VAL-x2      0 TO  VAL-x2
	VAL-x3  i + TO   VAL-x3      0 TO  VAL-x3
	VAL-x4  i + TO   VAL-x4      0 TO  VAL-x4
	VAL-x5  i + TO   VAL-x5      0 TO  VAL-x5
	VAL-x6  i + TO   VAL-x6      0 TO  VAL-x6
	VAL-x7  i + TO   VAL-x7      0 TO  VAL-x7
	VAL-x8  i + TO   VAL-x8      0 TO  VAL-x8
	VAL-x9  i + TO   VAL-x9      0 TO  VAL-x9
	VAL-x0  i + TO   VAL-x0      0 TO  VAL-x0
	VAL-c1  i + TO   VAL-c1      0 TO  VAL-c1
	VAL-c2  i + TO   VAL-c2      0 TO  VAL-c2
	VAL-c3  i + TO   VAL-c3      0 TO  VAL-c3
	VAL-c4  i + TO   VAL-c4      0 TO  VAL-c4
	VAL-c5  i + TO   VAL-c5      0 TO  VAL-c5
	VAL-c6  i + TO   VAL-c6      0 TO  VAL-c6
	VAL-c7  i + TO   VAL-c7      0 TO  VAL-c7
	VAL-c8  i + TO   VAL-c8      0 TO  VAL-c8
	VAL-c9  i + TO   VAL-c9      0 TO  VAL-c9
	VAL-c0  i + TO   VAL-c0      0 TO  VAL-c0
	VAL-d1  i + TO   VAL-d1      0 TO  VAL-d1
	VAL-d2  i + TO   VAL-d2      0 TO  VAL-d2
	VAL-d3  i + TO   VAL-d3      0 TO  VAL-d3
	VAL-d4  i + TO   VAL-d4      0 TO  VAL-d4
	VAL-d5  i + TO   VAL-d5      0 TO  VAL-d5
	VAL-d6  i + TO   VAL-d6      0 TO  VAL-d6
	VAL-d7  i + TO   VAL-d7      0 TO  VAL-d7
	VAL-d8  i + TO   VAL-d8      0 TO  VAL-d8
	VAL-d9  i + TO   VAL-d9      0 TO  VAL-d9
	VAL-d0  i + TO   VAL-d0      0 TO  VAL-d0
	VAL-e1  i + TO   VAL-e1      0 TO  VAL-e1
	VAL-e2  i + TO   VAL-e2      0 TO  VAL-e2
	VAL-e3  i + TO   VAL-e3      0 TO  VAL-e3
	VAL-e4  i + TO   VAL-e4      0 TO  VAL-e4
	VAL-e5  i + TO   VAL-e5      0 TO  VAL-e5
	VAL-e6  i + TO   VAL-e6      0 TO  VAL-e6
	VAL-e7  i + TO   VAL-e7      0 TO  VAL-e7
	VAL-e8  i + TO   VAL-e8      0 TO  VAL-e8
	VAL-e9  i + TO   VAL-e9      0 TO  VAL-e9
	VAL-e0  i + TO   VAL-e0      0 TO  VAL-e0
	VAL-f1  i + TO   VAL-f1      0 TO  VAL-f1
	VAL-f2  i + TO   VAL-f2      0 TO  VAL-f2
	VAL-f3  i + TO   VAL-f3      0 TO  VAL-f3
	VAL-f4  i + TO   VAL-f4      0 TO  VAL-f4
	VAL-f5  i + TO   VAL-f5      0 TO  VAL-f5
	VAL-f6  i + TO   VAL-f6      0 TO  VAL-f6
	VAL-f7  i + TO   VAL-f7      0 TO  VAL-f7
	VAL-f8  i + TO   VAL-f8      0 TO  VAL-f8
	VAL-f9  i + TO   VAL-f9      0 TO  VAL-f9
	VAL-f0  i + TO   VAL-f0      0 TO  VAL-f0
	VAL-g1  i + TO   VAL-g1      0 TO  VAL-g1
	VAL-g2  i + TO   VAL-g2      0 TO  VAL-g2
	VAL-g3  i + TO   VAL-g3      0 TO  VAL-g3
	VAL-g4  i + TO   VAL-g4      0 TO  VAL-g4
	VAL-g5  i + TO   VAL-g5      0 TO  VAL-g5
	VAL-g6  i + TO   VAL-g6      0 TO  VAL-g6
	VAL-g7  i + TO   VAL-g7      0 TO  VAL-g7
	VAL-g8  i + TO   VAL-g8      0 TO  VAL-g8
	VAL-g9  i + TO   VAL-g9      0 TO  VAL-g9
	VAL-g0  i + TO   VAL-g0      0 TO  VAL-g0
	VAL-h1  i + TO   VAL-h1      0 TO  VAL-h1
	VAL-h2  i + TO   VAL-h2      0 TO  VAL-h2
	VAL-h3  i + TO   VAL-h3      0 TO  VAL-h3
	VAL-h4  i + TO   VAL-h4      0 TO  VAL-h4
	VAL-h5  i + TO   VAL-h5      0 TO  VAL-h5
	VAL-h6  i + TO   VAL-h6      0 TO  VAL-h6
	VAL-h7  i + TO   VAL-h7      0 TO  VAL-h7
	VAL-h8  i + TO   VAL-h8      0 TO  VAL-h8
	VAL-h9  i + TO   VAL-h9      0 TO  VAL-h9
	VAL-h0  i + TO   VAL-h0      0 TO  VAL-h0
    LOOP ;

: test-all	\ rhythmical comparison of both
    cr ." testing block-variable vs variable speed"
    cr ." using  "  open-memory-block-xt @ xt>string type
    ."   to get memory blocks" 

    cr
    cr ." rhythmical comparison: 	"
    iterations . ." iterations "    times . ." times."

    cr
    times 0 DO test-variables	  bell LOOP
    times 0 DO test-blocked-vars  bell LOOP
    times 0 DO test-values	  bell LOOP ;

\ ****************************************************************

40000 to iterations
8 to times

\ test-variables
\ test-blocked-vars
\ test-values

test-all
cr

\ ****************************************************************
\ cr BYE
