\  \                                                      09may97re
\  \ \needs get_mouse     #include atari.str

\  get_mouse ( -- x y k ) ########

\  2VARIABLE old_mouse_coordinates
2VARIABLE keyed_mouse_coordinates
0 VALUE mouse_key_was

: mousek! ( x y k -- )
   to mouse_key_was
   keyed_mouse_coordinates 2! ;

: mousek@ ( x y k -- )
   keyed_mouse_coordinates 2@ 
   mouse_key_was ;

\  \                                                      09may97re

\  : new_mouse_coordinates? ( x y -- f )
\     old_mouse_coordinates 2@ d= 0= ;

\  : new_mouse_coordinates?? ( x y -- xf yf true | false )
\     old_mouse_coordinates 2@ rot - 0= 0= >r  - 0= 0= dup r@ or
\     IF r> true ELSE rdrop THEN  ;

\  : new_mouse_coordinates!?? ( x y -- xf yf true | false ) ( 2! )
\     2dup >r >r  new_mouse_coordinates??
\     r> r>  2 pick IF old_mouse_coordinates 2!
\            ELSE 2drop THEN ;

\  \                                                      04jun97re

\  : mouse_news?  ( -- false | xf yf key true )
\     get_mouse   ?dup IF >r   2dup r@  mousek!   ELSE 0 >r THEN
\     new_mouse_coordinates!?? dup
\     r@ or 0= IF rdrop EXIT THEN \ mouse has not changed
\     0= IF false false THEN      \ only mousekey pop cord-flags
\     r> true ;

\  : mousekey ( -- 0 | x y k )   get_mouse
\     dup IF  mousek! mousek@  ELSE  -rot 2drop  THEN ;

\  : no_mousek ( -- ) BEGIN get_mouse -rot 2drop WHILE REPEAT  ;

\  false [if]
\  dec
\  : mouse_controller ( adrIDx-y -- ) >r
\     mouse_news? IF IF ( key ) 2drop rdrop EXIT THEN

\  : mouseslider
\    CREATE 2, DOES> 2@ mouse_controller ;
\  [then]
