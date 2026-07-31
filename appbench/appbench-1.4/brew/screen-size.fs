\ screen-size.fs
\ 	$Id: screen-size.fs,v 1.3 2005/04/18 18:42:08 f Exp $	

\ brew runs on Linux text console.

\ if my-console.conf.fs exists it is read before
\ screen-size.fs just gives conservative defaults for undefined constants.
\ (default is a conservative 80 25 console).

\ ****************************************************************
decimal

\ cells (or caracters) in a line
[UNDEFINED] c-l [IF]	80 CONSTANT c-l					[THEN]

\ lines on a screen
[UNDEFINED] l-s [IF]	25 CONSTANT l-s					[THEN]
