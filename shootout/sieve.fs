\ $Id: sieve.bigforth,v 1.1 2001/06/19 16:20:46 doug Exp $
\ http://www.bagley.org/~doug/shootout/
\ adapted from a program in the gforth distribution
\ modified and annotated by doug bagley

\ find and count all primes from 2 to 8192

\ read NUM from last command line argument
10000 constant NUM

\ we search for primes up to this SIZE
8192 constant SIZE

\ Flags is an array of chars of length SIZE
\ we'll mark all non-prime indexes in this array as false
\ the remaining indexes will be prime numbers
create Flags SIZE allot

\ EndFlags points to end of array Flags
Flags SIZE + constant EndFlags

\ FLAGMULTS
\ flag all multiples of n in array as not prime
\ array has address range: fromaddr toaddr
\ starting value for fromaddr should be
\   arraystart n n + +
: flagmults
    do
    0 i c! dup
    +loop ;
\ END FLAGMULTS


\ PRIMES
\ find all primes from 2 to SIZE
: primes
\ fill array Flags with 1's
    Flags SIZE 1 fill
    0 2
    \ index i ranges from Flags to EndFlags
    EndFlags Flags
    do
    i c@
    \ If the current Flags[i] is true (i.e. i is prime)
    if
        dup i + dup EndFlags <
        \ If we aren't at end of flags array yet
        if
        EndFlags swap flagmults
        else
        drop
            then
        \ Increment our Count of Primes
            swap 1+ swap
    then
    1+
    loop
    drop \ your pants!
    ;
\ END PRIMES (Returns: Count)

\ BENCHMARK
\ run the test NUM times
: benchmark  0 NUM 0 do  primes nip loop ;


\ now print count of how many Flags are now "true"
UTIME 2>R
." Count: " benchmark  1 u.r cr
UTIME 2R> D- ." Elapsed: " D. ." usec" CR


\ PPRIMES
\ for testing, we can print out all the prime numbers
: pprimes
    SIZE 0 do Flags i + c@ if i 2 + . then loop cr ;

\ uncomment the following to print the primes or debug
\ pprimes
\ flags 100 dump

bye
