: TEST-TIMING
    UTIME 2>R              \ save start time on return stack
    1000000 0 DO LOOP      \ your code here
    UTIME 2R> D-           \ get end time, subtract start
    ." Elapsed: " D. ." microseconds" CR
;
TEST-TIMING
