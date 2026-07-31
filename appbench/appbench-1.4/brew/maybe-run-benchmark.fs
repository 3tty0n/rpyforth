\ 	$Id: maybe-run-benchmark.fs,v 1.8 2002/11/16 09:49:05 f Exp $	

\ *********************************************************************
\ Run brew as a benchmark?
\ *********************************************************************

\ If your FORTH has an option to pass a string which is evaluate'd
\ during startup start benchmarks by defining a word with the
\ benchmarks name (without the '.fs' suffix).

\ So in Gforth you'd say:
\ time  gforth-fast --dictionary-size 2M  -e "CREATE startup-bench"   brew.fs

\ In bigFORTH you say:
\ time  bigforth -e "CREATE startup-bench"  brew.fs
\
\ The other benchmarks need more memory, so say something like:
\ time bigforth  --mem-size 4M  --dictionary-size 2M\
\	       -e "CREATE transit-12-bench" brew.fs


\ PFE also has the -e" option now:
\ time pfe -e "CREATE transit-11-bench-A" brew.fs


\ ****************************************************************

[DEFINED] startup-bench   [IF] INCLUDE benchmarks/startup-bench.fs 	[THEN]

[DEFINED] transit-11-bench-A
[IF] INCLUDE benchmarks/transit-11-bench-A.fs 				[THEN]

[DEFINED] transit-12-bench [IF] INCLUDE benchmarks/transit-12-bench.fs 	[THEN]

\ *********************************************************************
\ If your FORTH does not have such an option you can uncomment one of
\ the following 'INCLUDE xxx' lines and use starting commands given below:
\ *********************************************************************

\ INCLUDE benchmarks/startup-bench.fs
\		Startup only.

\ INCLUDE benchmarks/transit-11-bench-A.fs
\ INCLUDE benchmarks/transit-12-bench.fs

\ ****************************************************************
\ Please do not use old z9 benchmarks on this version.
\ ****************************************************************
