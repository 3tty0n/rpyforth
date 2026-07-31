\ make-ekey-map.fs
\ 	$Id: make-ekey-map.fs,v 1.4 2003/08/27 17:52:10 f Exp $	

\ Writes ekey-mapping to "OUTPUT/tmp/ekey-mapping.fs"

MARKER forget-it

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" brew-basics.fs" REQUIRED

: out-one-ekey-mapping ( addr count wid -- )
    >r

    page cr
    ." Please press " 2dup type
    at?
    cr   ."              (or press <RETURN> if it does not react)"
    at-xy

    ekey
    dup [ decimal ] 13 = IF drop -1 THEN
    num>string r@ write-file drop
    s"  CONSTANT " r@ write-file drop
    ( addr count ) r@ write-line drop
    r> flush-file drop ;

: make-ekey-map ( -- )
    base @ >r hex
    page cr
    ." make-ekey-map:" cr cr
    ." This function helps you write the ekey mapping of your system. " cr
    3000 ms

    s" OUTPUT/tmp/ekey-mapping.fs" w/o
    [UNDEFINED] brew 			\ test if running standalone
    [IF]
	CREATE-FILE			\ standalone
    [ELSE]
	CREATE-FILE+			\ inside brew
    [THEN]
    ABORT" make-ekey-map: Couldn't create-file."
    >r

    s" \ This ekey mapping was created by including file 'make-ekey-map.fs'."
    r@ write-line drop
    s" \ Append it to your system dependent file or to file 'my-compile-options.fs'."
    r@ write-line drop
    s" \ You will have to restart brew to make it work."
    r@ write-line drop
    s" \ (If this does not help try switching use-ekey off)."
    r@ write-line drop
    s" " r@ write-line drop

    s" base @ hex" r@ write-line drop

    page cr
    ." Please press the <RETURN> key."
    ekey num>string r@ write-file drop
    s"  CONSTANT <return>" r@ write-line drop
    r@ flush-file drop

    page cr
    ." Please press the indicated cursor keys:" 2500 ms

    s" <left>"	r@ out-one-ekey-mapping
    s" <right>"	r@ out-one-ekey-mapping
    s" <up>"	r@ out-one-ekey-mapping
    s" <down>"	r@ out-one-ekey-mapping

    page cr
    ." Please enter your (unshifted) function keys."
    2000 ms

    s" <F1>"	r@ out-one-ekey-mapping
    s" <F2>"	r@ out-one-ekey-mapping
    s" <F3>"	r@ out-one-ekey-mapping
    s" <F4>"	r@ out-one-ekey-mapping
    s" <F5>"	r@ out-one-ekey-mapping
    s" <F6>"	r@ out-one-ekey-mapping
    s" <F7>"	r@ out-one-ekey-mapping
    s" <F8>"	r@ out-one-ekey-mapping
    s" <F9>"	r@ out-one-ekey-mapping
    s" <F10>"	r@ out-one-ekey-mapping
    s" <F11>"	r@ out-one-ekey-mapping
    s" <F12>"	r@ out-one-ekey-mapping

    
    page cr
    ." Please enter the following keys: "
    1500 ms

    s" <home>"	r@ out-one-ekey-mapping
    s" <end>"	r@ out-one-ekey-mapping
    s" <page-up>"	r@ out-one-ekey-mapping
    s" <page-down>"	r@ out-one-ekey-mapping

    s" base !" r@ write-line drop
    r> flush-file drop

    page
    cr
    ." Please append the file 'OUTPUT/tmp/ekey-mapping' to your" cr
    ." system specific init file (or to my-compile-options.fs)" cr
    cr cr cr ."     (Press any key to leave)" cr

    key drop
    r> base ! ;

make-ekey-map
forget-it
