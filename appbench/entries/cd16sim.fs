\ rpyforth entry point for appbench cd16sim
\ This file is copied into the cd16sim workdir and run from there.

: 3drop 2drop drop ;
include bench.f
1000000 benchmark
bye
