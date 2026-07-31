Lexex Benchmark
~~~~~~~~~~~~~~~

The file lexex.zip contains a portable application benchmark for ANS
Forth systems. System resources needed are an ANS Forth system with at
least 4 MBytes of dataspace and 2K cells available on both the data
stack and return stacks. To run the benchmark:

1. Unzip lexex.zip into a suitable directory
2. Start the Forth system
3. Set the working directory to that containing the lexex files (this is
system specific so no advice can be given here)
4. Type  s" run.fth" included

The benchmark should then be compiled and run with the time taken
displayed. Correct operation will result in the generation of a file
called stt.fth in the working directory. Provided the working directory
has been set correctly, the contents of the file will be checked.

The following table shows the systems that lexex has been run on and the
time taken to run it on a PC running under Windows XP with an Athlon 64
3200+ processor clocked at 2000 MHz.

System              Time     Relative to
                    taken    VFX Forth
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
VFX Forth 4.21      14 secs    1.0
SwiftForth 3.1.11   24 secs    1.7
GForth-fast 0.7.0   27 secs    1.9
BigForth 2.2.0      30 secs    2.1
GForth 0.7.0        43 secs    3.1
Win32 Forth 6.12.0  84 secs    6.0
GForth-itc 0.7.0   153 secs   10.9

Times are a little imprecise as TIME&DATE was used to get the elapsed
time. The time calculation fails if the run takes more than 1 hour.

Any problems or results to report please email to 
gerry@jackson9000.fsnet.co.uk. 

The lexex benchmark is based on the LexGen tool available at:

http://www.qlikz.org/forth

