\ my-compile-options.fs
\ 	$Id: my-compile-options.fs,v 1.3 2002/04/13 15:00:02 f Exp $	

\ Use this file for your private brew compile options.


\ If you don't like the defaults on which external documentation reader
\ gets used, change it here:
\
\ 'manual-type' switches between 'info' and 'html' format.
\ See file 'brew-options.fs' to set default as you like it.
\
\ If 'manual-type' is 'info-as-manual'
\ the following string is used to call 'info' on 'brew.info':
\ : call-info-string ( -- addr count )   s" info --file=texi/brew.info" ;
\ Uncomment and edit to set another external info reader.
\
\ If 'manual-type' is 'html-as-manual'
\ the following string is used to call the html browser on brew.html:
\ : call-browser-string ( -- addr count )   s" lynx texi/brew.html" ;
\ Uncomment and edit to set another external html browser.
\
\ Set default 'manual-type' in file 'brew-options.fs'.


\ Uncomment to run brew-crash-test.fs (downwards compatibility).
\ (see file INPUTS/extensions/debugging/crash-test-README)
\ CREATE brew-crash-test
