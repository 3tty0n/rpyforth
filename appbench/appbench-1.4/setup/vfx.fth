1 cells 4 = [if]
    include /usr/share/doc/VfxForth/Lib/Ndp387.fth
[then]
'.' dp-char !
'.' fp-char !
: ms@ ticks ; 
: xt>string >name name>string ;

[undefined] cs-roll [if]
cr .( defining absent standard word CS-ROLL )
: cs-roll roll ;
[then]
