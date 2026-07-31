\ my-brew-options.fs
\ 	$Id: my-brew-options.fs,v 1.5 2002/11/16 09:49:09 f Exp $	

\ Use this file for your private brew options.

\ You can change options here or in 'brew-options.fs' file,
\ which get's included just before this one.


\ To select default type of the documentation reader
\ uncomment one of the following lines:
\ info-as-manual manual-type !	\ Please do check 'call-info-string'
\ html-as-manual manual-type !	\ Please do check 'call-browser-string'
\
\ See compile options 'call-info-string' and 'call-browser-string'
\ in 'my-compile-options.fs' to call the external programmes.

\ These colours get used when showing nucs (foreground) or spots (background)
\ coloured on a certain criteria like lying below, inside or above a given
\ range (uncomment and edit as you like):
\ ' default-color color-selected-fg-xt !  \ 'default-color' foreground is white
\ ' magenta color-below-fg-xt !
\ ' cyan color-above-fg-xt !
\ ' blue color-miss-fg-xt !
\ ' cyan color-selected-bg-xt !
\ ' magenta color-below-bg-xt !
\ ' blue color-above-bg-xt !
\ ' blue color-miss-bg-xt !


\ Uncomment for debugging.  Writes huge log and code files then.
\ Do set 'log-mask' as *compile* option too.
\ log-mask ON		code-file-mask ON


\ Uncomment for brew crash test and the like.
\ (see file INPUTS/extensions/debugging/crash-test-README)
\ INCLUDE INPUTS/extensions/debugging/checksums.fs
