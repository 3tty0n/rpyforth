\ Takeuchi (tak) function -- a deeply recursive benchmark (Gabriel suite).
\ tak(x,y,z) = y<x ? tak(tak(x-1,y,z), tak(y-1,z,x), tak(z-1,x,y)) : z

27 constant NUM   \ tak(NUM, 2/3 NUM, 1/3 NUM); canonical light size is (18,12,6)

: tak ( x y z -- n ) recursive
    2 pick 2 pick > if
        2 pick 1- 2 pick 2 pick tak
        2 pick 1- 2 pick 5 pick tak
        2 pick 1- 5 pick 5 pick tak
        tak
        nip nip nip
    else
        nip nip
    then ;

: main  ." Tak: "  NUM  NUM 2* 3 /  NUM 3 /  tak  4 u.r CR ;

UTIME 2>R
main
UTIME 2R> D- ." Elapsed: " D. ." usec" CR

bye
