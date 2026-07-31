\ Test nested loops with I and J
1000 CONSTANT NUM

: NESTED
    NUM 0 DO
        NUM 0 DO
            NUM 0 DO
                I J +
                DROP
            LOOP
        LOOP
    LOOP ;

NESTED bye
