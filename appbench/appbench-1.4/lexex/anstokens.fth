\ ANSTokens.fth for an ANS Forth word scanner

\ Contains the complete set of ANS Forth words (359 words)
\ Duplicates are not included e.g. state is in both the core word set
\ and the the tools extension word set but is included only once here

0
\ Words with special actions

\ parse next word as a <name> and define it

dup 1+ constant minToken
dup 1+ constant minBlock

1+ dup constant "blk"               \ block
1+ dup constant "block"             \ block
1+ dup constant "buffer"            \ block
1+ dup constant "flush"             \ block
1+ dup constant "load"              \ block
1+ dup constant "save-buffers"      \ block
1+ dup constant "update"            \ block

dup 1+ constant maxBlock
dup 1+ constant minBlockExt

1+ dup constant "empty-buffers"     \ blockext
1+ dup constant "list"              \ blockext
1+ dup constant "scr"               \ blockext
1+ dup constant "thru"              \ blockext

dup 1+ constant maxBlockExt
dup 1+ constant minCore

1+ dup constant "!"                 \ core
1+ dup constant "#"                 \ core
1+ dup constant "#>"                \ core
1+ dup constant "#s"                \ core
1+ dup constant "'"                 \ core
1+ dup constant "("                 \ core
1+ dup constant "*"                 \ core
1+ dup constant "*/"                \ core
1+ dup constant "*/mod"             \ core
1+ dup constant "+"                 \ core
1+ dup constant "+!"                \ core
1+ dup constant "+loop"             \ core
1+ dup constant ","                 \ core
1+ dup constant "-"                 \ core
1+ dup constant "."                 \ core
1+ dup constant ".""	               \ core
1+ dup constant "/"                 \ core
1+ dup constant "/mod"              \ core
1+ dup constant "0<"                \ core
1+ dup constant "0="                \ core
1+ dup constant "1+"                \ core
1+ dup constant "1-"                \ core
1+ dup constant "2!"                \ core
1+ dup constant "2*"                \ core
1+ dup constant "2/"                \ core
1+ dup constant "2@"                \ core
1+ dup constant "2drop"             \ core
1+ dup constant "2dup"              \ core
1+ dup constant "2over"             \ core
1+ dup constant "2swap"             \ core
1+ dup constant ":"                 \ core
1+ dup constant ";"                 \ core
1+ dup constant "<"                 \ core
1+ dup constant "<#"                \ core
1+ dup constant "="                 \ core
1+ dup constant ">"                 \ core
1+ dup constant ">body"             \ core
1+ dup constant ">in"               \ core
1+ dup constant ">number"           \ core
1+ dup constant ">r"                \ core
1+ dup constant "?dup"              \ core
1+ dup constant "@"                 \ core
1+ dup constant "abort"             \ core
1+ dup constant "abort""	         \ core
1+ dup constant "abs"               \ core
1+ dup constant "accept"            \ core
1+ dup constant "align"             \ core
1+ dup constant "aligned"           \ core
1+ dup constant "allot"             \ core
1+ dup constant "and"               \ core
1+ dup constant "base"              \ core
1+ dup constant "begin"             \ core
1+ dup constant "bl"                \ core
1+ dup constant "c!"                \ core
1+ dup constant "c,"                \ core
1+ dup constant "c@"                \ core
1+ dup constant "cell+"             \ core
1+ dup constant "cells"             \ core
1+ dup constant "char"              \ core
1+ dup constant "char+"             \ core
1+ dup constant "chars"             \ core
1+ dup constant "constant"          \ core
1+ dup constant "count"             \ core
1+ dup constant "cr"                \ core
1+ dup constant "create"            \ core
1+ dup constant "decimal"           \ core
1+ dup constant "depth"             \ core
1+ dup constant "do"                \ core
1+ dup constant "does>"             \ core
1+ dup constant "drop"              \ core
1+ dup constant "dup"               \ core
1+ dup constant "else"              \ core
1+ dup constant "emit"              \ core
1+ dup constant "environment?"      \ core
1+ dup constant "evaluate"          \ core
1+ dup constant "execute"           \ core
1+ dup constant "exit"              \ core
1+ dup constant "fill"              \ core
1+ dup constant "find"              \ core
1+ dup constant "fm/mod"            \ core
1+ dup constant "here"              \ core
1+ dup constant "hold"              \ core
1+ dup constant "i"                 \ core
1+ dup constant "if"                \ core
1+ dup constant "immediate"         \ core
1+ dup constant "invert"            \ core
1+ dup constant "j"                 \ core
1+ dup constant "key"               \ core
1+ dup constant "leave"             \ core
1+ dup constant "literal"           \ core
1+ dup constant "loop"              \ core
1+ dup constant "lshift"            \ core
1+ dup constant "m*"                \ core
1+ dup constant "max"               \ core
1+ dup constant "min"               \ core
1+ dup constant "mod"               \ core
1+ dup constant "move"              \ core
1+ dup constant "negate"            \ core
1+ dup constant "or"                \ core
1+ dup constant "over"              \ core
1+ dup constant "postpone"          \ core
1+ dup constant "quit"              \ core
1+ dup constant "r>"                \ core
1+ dup constant "r@"                \ core
1+ dup constant "recurse"           \ core
1+ dup constant "repeat"            \ core
1+ dup constant "rot"               \ core
1+ dup constant "rshift"            \ core
1+ dup constant "s""	               \ core
1+ dup constant "s>d"               \ core
1+ dup constant "sign"              \ core
1+ dup constant "sm/rem"            \ core
1+ dup constant "source"            \ core
1+ dup constant "space"             \ core
1+ dup constant "spaces"            \ core
1+ dup constant "state"             \ core
1+ dup constant "swap"              \ core
1+ dup constant "then"              \ core
1+ dup constant "type"              \ core
1+ dup constant "u."                \ core
1+ dup constant "u<"                \ core
1+ dup constant "um*"               \ core
1+ dup constant "um/mod"            \ core
1+ dup constant "unloop"            \ core
1+ dup constant "until"             \ core
1+ dup constant "variable"          \ core
1+ dup constant "while"             \ core
1+ dup constant "word"              \ core
1+ dup constant "xor"               \ core
1+ dup constant "["                 \ core
1+ dup constant "[']"               \ core
1+ dup constant "[char]"            \ core
1+ dup constant "]"                 \ core

dup 1+ constant maxCore
dup 1+ constant minCoreExt

1+ dup constant "#tib"              \ coreext
1+ dup constant ".("                \ coreext
1+ dup constant ".r"                \ coreext
1+ dup constant "0<>"               \ coreext
1+ dup constant "0>"                \ coreext
1+ dup constant "2>r"               \ coreext
1+ dup constant "2r>"               \ coreext
1+ dup constant "2r@"               \ coreext
1+ dup constant ":noname"           \ coreext
1+ dup constant "<>"                \ coreext
1+ dup constant "?do"               \ coreext
1+ dup constant "again"             \ coreext
1+ dup constant "c""	               \ coreext
1+ dup constant "case"              \ coreext
1+ dup constant "compile,"          \ coreext
1+ dup constant "convert"           \ coreext
1+ dup constant "endcase"           \ coreext
1+ dup constant "endof"             \ coreext
1+ dup constant "erase"             \ coreext
1+ dup constant "expect"            \ coreext
1+ dup constant "false"             \ coreext
1+ dup constant "hex"               \ coreext
1+ dup constant "marker"            \ coreext
1+ dup constant "nip"               \ coreext
1+ dup constant "of"                \ coreext
1+ dup constant "pad"               \ coreext
1+ dup constant "parse"             \ coreext
1+ dup constant "pick"              \ coreext
1+ dup constant "query"             \ coreext
1+ dup constant "refill"            \ coreext
1+ dup constant "restore-input"     \ coreext
1+ dup constant "roll"              \ coreext
1+ dup constant "save-input"        \ coreext
1+ dup constant "source-id"         \ coreext
1+ dup constant "span"              \ coreext
1+ dup constant "tib"               \ coreext
1+ dup constant "to"                \ coreext
1+ dup constant "true"              \ coreext
1+ dup constant "tuck"              \ coreext
1+ dup constant "u.r"               \ coreext
1+ dup constant "u>"                \ coreext
1+ dup constant "unused"            \ coreext
1+ dup constant "value"             \ coreext
1+ dup constant "within"            \ coreext
1+ dup constant "[compile]"         \ coreext
1+ dup constant "\"	               \ coreext

dup 1+ constant maxCoreExt
dup 1+ constant minDouble

1+ dup constant "2constant"         \ double
1+ dup constant "2literal"          \ double
1+ dup constant "2variable"         \ double
1+ dup constant "d+"                \ double
1+ dup constant "d-"                \ double
1+ dup constant "d."                \ double
1+ dup constant "d.r"               \ double
1+ dup constant "d0<"               \ double
1+ dup constant "d0="               \ double
1+ dup constant "d2*"               \ double
1+ dup constant "d2/"               \ double
1+ dup constant "d<"                \ double
1+ dup constant "d="                \ double
1+ dup constant "d>s"               \ double
1+ dup constant "dabs"              \ double
1+ dup constant "dmax"              \ double
1+ dup constant "dmin"              \ double
1+ dup constant "dnegate"           \ double
1+ dup constant "m*/"               \ double
1+ dup constant "m+"                \ double

dup 1+ constant maxDouble
dup 1+ constant minDoubleExt

1+ dup constant "2rot"              \ doubleext
1+ dup constant "du<"               \ doubleext

dup 1+ constant maxDoubleExt
dup 1+ constant minException

1+ dup constant "catch"             \ exception
1+ dup constant "throw"             \ exception

dup 1+ constant maxException
dup 1+ constant minFacility

1+ dup constant "at-xy"             \ facility
1+ dup constant "key?"              \ facility
1+ dup constant "page"              \ facility

dup 1+ constant maxFacility
dup 1+ constant minFacilityExt

1+ dup constant "ekey"              \ facilityext
1+ dup constant "ekey>char"         \ facilityext
1+ dup constant "ekey?"             \ facilityext
1+ dup constant "emit?"             \ facilityext
1+ dup constant "ms"                \ facilityext
1+ dup constant "time&date"         \ facilityext

dup 1+ constant maxFacilityExt
dup 1+ constant minFile

1+ dup constant "bin"               \ file
1+ dup constant "close-file"        \ file
1+ dup constant "create-file"       \ file
1+ dup constant "delete-file"       \ file
1+ dup constant "file-position"     \ file
1+ dup constant "file-size"         \ file
1+ dup constant "included"          \ file
1+ dup constant "include-file"      \ file
1+ dup constant "open-file"         \ file
1+ dup constant "r/o"               \ file
1+ dup constant "r/w"               \ file
1+ dup constant "read-file"         \ file
1+ dup constant "read-line"         \ file
1+ dup constant "reposition-file"   \ file
1+ dup constant "resize-file"       \ file
1+ dup constant "w/o"               \ file
1+ dup constant "write-file"        \ file
1+ dup constant "write-line"        \ file

dup 1+ constant maxFile
dup 1+ constant minFileExt

1+ dup constant "file-status"       \ fileext
1+ dup constant "flush-file"        \ fileext
1+ dup constant "rename-file"       \ fileext

dup 1+ constant maxFileExt
dup 1+ constant minFloating

1+ dup constant ">float"            \ floating
1+ dup constant "d>f"               \ floating
1+ dup constant "f!"                \ floating
1+ dup constant "f*"                \ floating
1+ dup constant "f+"                \ floating
1+ dup constant "f-"                \ floating
1+ dup constant "f/"                \ floating
1+ dup constant "f0<"               \ floating
1+ dup constant "f0="               \ floating
1+ dup constant "f<"                \ floating
1+ dup constant "f>d"               \ floating
1+ dup constant "f@"                \ floating
1+ dup constant "falign"            \ floating
1+ dup constant "faligned"          \ floating
1+ dup constant "fconstant"         \ floating
1+ dup constant "fdepth"            \ floating
1+ dup constant "fdrop"             \ floating
1+ dup constant "fdup"              \ floating
1+ dup constant "fliteral"          \ floating
1+ dup constant "float+"            \ floating
1+ dup constant "floats"            \ floating
1+ dup constant "floor"             \ floating
1+ dup constant "fmax"              \ floating
1+ dup constant "fmin"              \ floating
1+ dup constant "fnegate"           \ floating
1+ dup constant "fover"             \ floating
1+ dup constant "frot"              \ floating
1+ dup constant "fround"            \ floating
1+ dup constant "fswap"             \ floating
1+ dup constant "fvariable"         \ floating
1+ dup constant "represent"         \ floating

dup 1+ constant maxFloating
dup 1+ constant minFloatingExt

1+ dup constant "df!"               \ floatingext
1+ dup constant "df@"               \ floatingext
1+ dup constant "dfalign"           \ floatingext
1+ dup constant "dfaligned"         \ floatingext
1+ dup constant "dfloat+"           \ floatingext
1+ dup constant "dfloats"           \ floatingext
1+ dup constant "f**"               \ floatingext
1+ dup constant "f."                \ floatingext
1+ dup constant "fabs"              \ floatingext
1+ dup constant "facos"             \ floatingext
1+ dup constant "facosh"            \ floatingext
1+ dup constant "falog"             \ floatingext
1+ dup constant "fasin"             \ floatingext
1+ dup constant "fasinh"            \ floatingext
1+ dup constant "fatan"             \ floatingext
1+ dup constant "fatan2"            \ floatingext
1+ dup constant "fatanh"            \ floatingext
1+ dup constant "fcos"              \ floatingext
1+ dup constant "fcosh"             \ floatingext
1+ dup constant "fe."               \ floatingext
1+ dup constant "fexp"              \ floatingext
1+ dup constant "fexpm1"            \ floatingext
1+ dup constant "fln"               \ floatingext
1+ dup constant "flnp1"             \ floatingext
1+ dup constant "flog"              \ floatingext
1+ dup constant "fs."               \ floatingext
1+ dup constant "fsin"              \ floatingext
1+ dup constant "fsincos"           \ floatingext
1+ dup constant "fsinh"             \ floatingext
1+ dup constant "fsqrt"             \ floatingext
1+ dup constant "ftan"              \ floatingext
1+ dup constant "ftanh"             \ floatingext
1+ dup constant "f~"                \ floatingext
1+ dup constant "precision"         \ floatingext
1+ dup constant "set-precision"     \ floatingext
1+ dup constant "sf!"               \ floatingext
1+ dup constant "sf@"               \ floatingext
1+ dup constant "sfalign"           \ floatingext
1+ dup constant "sfaligned"         \ floatingext
1+ dup constant "sfloat+"           \ floatingext
1+ dup constant "sfloats"           \ floatingext

dup 1+ constant maxFloatingExt
dup 1+ constant minLocal

1+ dup constant "(local)"           \ local

dup 1+ constant maxLocal
dup 1+ constant minLocalExt

1+ dup constant "locals|"           \ localext

dup 1+ constant maxLocalExt
dup 1+ constant minMemory

1+ dup constant "allocate"          \ memory
1+ dup constant "free"              \ memory
1+ dup constant "resize"            \ memory

dup 1+ constant maxMemory
dup 1+ constant minSearch

1+ dup constant "definitions"       \ search
1+ dup constant "forth-wordlist"    \ search
1+ dup constant "get-current"       \ search
1+ dup constant "get-order"         \ search
1+ dup constant "search-wordlist"   \ search
1+ dup constant "set-current"       \ search
1+ dup constant "set-order"         \ search
1+ dup constant "wordlist"          \ search

dup 1+ constant maxSearch
dup 1+ constant minSearchExt

1+ dup constant "also"              \ searchext
1+ dup constant "forth"             \ searchext
1+ dup constant "only"              \ searchext
1+ dup constant "order"             \ searchext
1+ dup constant "previous"          \ searchext

dup 1+ constant maxSearchExt
dup 1+ constant minString

1+ dup constant "-trailing"         \ string
1+ dup constant "/string"           \ string
1+ dup constant "blank"             \ string
1+ dup constant "cmove"             \ string
1+ dup constant "cmove>"            \ string
1+ dup constant "compare"           \ string
1+ dup constant "search"            \ string
1+ dup constant "sliteral"          \ string

dup 1+ constant maxString
dup 1+ constant minTools

1+ dup constant ".s"                \ tools
1+ dup constant "?"                 \ tools
1+ dup constant "dump"              \ tools
1+ dup constant "see"               \ tools
1+ dup constant "words"             \ tools

dup 1+ constant maxTools
dup 1+ constant minToolsExt

1+ dup constant ";code"             \ toolsext
1+ dup constant "ahead"             \ toolsext
1+ dup constant "assembler"         \ toolsext
1+ dup constant "bye"               \ toolsext
1+ dup constant "code"              \ toolsext
1+ dup constant "cs-pick"           \ toolsext
1+ dup constant "cs-roll"           \ toolsext
1+ dup constant "editor"            \ toolsext
1+ dup constant "forget"            \ toolsext
1+ dup constant "[else]"            \ toolsext
1+ dup constant "[if]"              \ toolsext
1+ dup constant "[then]"            \ toolsext

dup 1+ constant maxToolsExt

1+ dup constant "forth-word"        \ Character string without spaces
1+ dup constant "ws"                \ Not ANS Forth
1+ dup constant "number"            \ Number with digits 0 to 9 sign .
1+ dup constant "floating"          \ Floating point number
1+ constant #tokens
