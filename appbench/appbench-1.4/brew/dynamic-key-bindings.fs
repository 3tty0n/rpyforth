\ dynamic-key-bindings.fs
\ 	$Id: dynamic-key-bindings.fs,v 1.4 2005/04/20 11:24:58 f Exp $	

\ Dynamically setting key bindings based on defined words to bind.
\ This file is interpreted by  .brew  . It's a hack, but it works...

\ [DEFINED] show-A [IF]
\     s" 1"	' show-A	>look-at	do-after-2	menu-key-entry
\ [THEN]

[DEFINED] show-sign-A [IF]
    s" p"	' show-sign-A	>look-at	do-after-2	menu-key-entry
[THEN]

\ [DEFINED] show-ABC*. [IF]
\     s" *"	' show-ABC*.	>look-at	do-after-2	menu-key-entry
\ [THEN]

\ [DEFINED] show-ABC [IF]
\     s" y"	' show-ABC	>look-at	do-after-2	menu-key-entry
\ [THEN]

[DEFINED] show-ABC-X| [IF]
    s" |"	' show-ABC-X|	>look-at	do-after-2	menu-key-entry
[THEN]

[DEFINED] show-~|'.* [IF]
    s" .'"	' show-~|'.*	>look-at	do-after-2	menu-key-entry
[THEN]
