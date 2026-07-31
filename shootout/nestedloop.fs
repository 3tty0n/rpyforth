30 constant NUM

: nestedloops
  NUM 0 do
    NUM 0 do
      NUM 0 do
        NUM 0 do
          NUM 0 do
            NUM 0 do
              1+
            loop
          loop
        loop
      loop
    loop
  loop ;

\ run test and print result
utime 2>R
0 nestedloops 1 U.R CR
utime 2R> D- ." Elapsed: " D. ." usec" CR

bye
