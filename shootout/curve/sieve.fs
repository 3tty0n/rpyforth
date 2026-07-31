\ Sieve of Eratosthenes benchmark for JIT analysis

\ Batch size: Run primes 1000 times per measurement iteration
1000 constant BATCH
50 constant ITERATIONS

\ Sieve size
8192 constant SIZE

create Flags SIZE allot
Flags SIZE + constant EndFlags

: flagmults ( step -- step )
  do
    0 i c!
    dup
  +loop ;

: primes ( -- n )
  Flags SIZE 1 fill
  0 2
  EndFlags Flags do
    i c@ if
      dup i + dup EndFlags < if
        EndFlags swap flagmults
      else
        drop
      then
      swap 1+ swap
    then
    1+
  loop
  drop ;

: get-time ( -- d ) utime ;
: diff-time ( d-start d-end -- d-diff ) 2swap d- ;

: run-benchmark
  ." Iteration,Time(usec)" cr
  ITERATIONS 0 do
    get-time

    \ Execute Batch
    BATCH 0 do
      primes drop
    loop

    get-time
    diff-time
    i . ." ," d. cr
  loop ;

run-benchmark
bye
