\ UserInterface.fth provides the definitions for the LexGen user

\ Copyright (C) Gerry Jackson 2006, 2008

\ This software is free; you can redistribute it and/or modify it in
\ any way provided you acknowledge the original source and copyright

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
\ ------------------------------------------------------------------

.( UserInterface.fth loading ...) cr

\ -----------------------------------------------------------------------------
\ Interface provided for the user:
\
\ setOutputFile   ( caddr u -- ) set the output file name
\ setMaxChar   ( u -- ) Specifies the maximum character value to be used
\        Default is 127. If used it must occur before definition of any
\        character sets
\        
\ [..]   ( c1 c2 -- set )      creates a character set including c1 to c2
\ [..+]  ( set c1 c2 -- set' ) adds characters c1 to c2 to an existing set
\ [+]    ( set c -- set' )     adds character c to set
\ [-]    ( set c -- set' )     removes character c from set
\ [new]  ( c -- set )          creates a new character set containing c
\
\ charClass ( set "<spaces>name" -- ) creates a character class called name.
\        name execution: ( -- tree ) creates a 1 node tree for the set
\
\ 'char' ( "<spaces>name" -- tree ) creates a 1 node tree for the first
\           character in name (the rest of name is discarded as for char)
\
\ 'lit'  ( u -- tree )  creates a one node tree for u, u must be in the range
\        0 to the value set by setMaxChar
\
\ Regular expression operators (reverse Polish notation):
\ <.>    ( tree1 tree2 -- tree3 ) concatenates two syntax trees
\ <|>    ( tree1 tree2 -- tree3 ) or's two syntax trees ( | operator )
\ <*>    ( tree1 -- tree2 )       0 or more repetitions of tree1
\ <+>    ( tree1 -- tree2 )       1 or more repetitions of tree1
\ <?>    ( tree1 -- tree2 )       0 or 1 occurrence of tree1
\
\ Example: conventional notation        would be written as
\              ab*                        a b <*> <.>
\              (ab)*                      a b <.> <*>
\
\ regexp ( tree "<spaces>name" -- ) creates a name for a regular expression
\        name execution ( -- tree ). Name is used in other regular expressions
\        and by denotes
\
\ case-insensitive  ( -- ) Sets a flag to make following symbols case
\        insensitive e.g program = PROGRAM = PrOgRaM. Applies to symbols only,
\        not character sets or single characters
\
\ case-sensitive (-- ) Clears the case sensitivity flag for following symbols
\        i.e. program <> PROGRAM. This is the default state. Note the case
\        sensitivity flag may be switched as often as needed for different
\        symbols
\
\ begin-symbols   ( -- )  starts the association list of symbols and regular
\        expressions with tokens
\
\ symbol ( [tree1] token "<spaces>name" -- tree ) constructs a syntax tree for
\        name, token is the value to be returned by the lexer when name is
\        recognised (see the note below).
\
\ denotes ( [tree1] token "<spaces>name" -- tree )
\        Associates the token with the regular expression called name and
\        incorporates it into the overall syntax tree (see the note below).
\
\ yields  ( [tree1] token tree2 -- tree )
\        Associates the token with tree2 and incorporates tree2 into the
\        overall syntax tree (see the note below).
\
\ Note that the first use of symbol, denotes or yields does not require a tree
\ on the stack. Also symbol, denotes and yields should only be used between
\ begin-symbols and end-symbols.
\
\ end-symbols     ( tree "<spaces>name" -- ) ends the list of symbols and
\        creates a name that, when executed leaves tree on the stack
\
\ lexgen    ( tree -- ) Starts the whole process of generating lex tables
\
\ token  ( "<spaces>name" -- ) For the user to declare token values in
\        tokens.fth. Use is not mandatory.
\
\ -----------------------------------------------------------------------------
\ For definition of character sets

0 value CharSet   \ Loaded by a user call to setMaxChar

: [+]    ( set c -- set' ) over add-member ;

: [-]    ( set c -- set' ) over drop-member ;

: [new]  ( c -- set ) CharSet new tuck add-member ;

: [..+]    ( set c1 c2 -- set' )
   1+ swap           ( -- set c2+1 c1 )
   do i [+] loop
;

: [..] ( c1 c2 -- set )
   CharSet new -rot [..+]
;

\ charClass is used to define a name that, when executed, will create
\ a leaf node containing the set

: charClass ( set "<spaces>name" -- )
   create ,
   does>    ( -- node )
      @ LeafCharSetNode new
;

\ Usage example of the above:
\        char a char z CharSet [..] char 0 char 9 [..+] charClass alphanum

\ -----------------------------------------------------------------------------
\ 'lit' and 'char' provide the ability to include a single integer or character
\ value in a regular expression.

: 'lit'  ( u -- node ) LeafCharNode new ;

\ e.g.  'char' x 'char' y <.> 'char' z <.> will concatenate xyz

: 'char'  ( "<spaces>name" -- node )  char 'lit' ;

\ -----------------------------------------------------------------------------
\ Regular expression operator definitions

\ To avoid problems with Forth words with the same name, regular expression
\ operators * | + ? will be replaced with <*> etc with <.> for concatenation
\ Given regular expressions a and b, and empty terminal e, the operators mean:
\     a b <.>   specifies a followed by b
\     a b <|>   specifies a or b
\     a <*>     specifies 0 or more occurences of a
\     a <+>     specifies 1 or more occurrences of a, = a a <*> <.>
\     a <?>     specifies 0 or 1 occurrence of a,     = a empty <|>

: <.>    ( tree1 tree2 -- tree3 ) CatNode  new ;
: <|>    ( tree1 tree2 -- tree3 ) OrNode   new ;
: <*>    ( tree1 -- tree2 )       StarNode new ;
: <+>    ( tree1 -- tree2 )       dup PlusStarNode new <.> ;
: <?>    ( tree1 -- tree2 )       OptNode new ;

\ Inserts an acceptor node in the syntax tree. For use at the end of
\ all symbols and regular expressions. The end of expression character #
\ in Aho, Sethi and Ullman 

: acceptor   ( n tree1 -- tree2 )
   swap ##                    ( -- tree1 n ch )
   AcceptorLeafNode new <.>
;

\ Variable firstDef is a boolean flag used by symbol etc to define
\ whether a <|> should be executed. Set true by begin-symbols

variable firstDef

\ Factor of symbol and friends to compile a <|> or not to avoid the
\ need to create an empty tree at the start of a set of definitions

: ?<|>
   firstDef @
   if
      false firstDef !
   else
      <|>
   then
;

\ Case sensitivity

false value caseInsens

: case-sensitive   ( -- ) false to caseInsens ;
: case-insensitive ( -- ) true  to caseInsens ;

char a char A - abs constant chara-A     \ = 32
char Z char A -     constant charZ-A     \ = 25

: >upper			( ch -- CH )
   dup [char] a - charZ-A u>
	if exit then
	chara-A -	            ( -- CH )
;

: >lower			( CH -- ch )
   dup [char] A - charZ-A u>
	if exit then
   chara-A +	            ( -- ch )
;

: newKWNode    ( caddr u -- caddr+1 u-1 tree )
   over c@
\ *************************************************************
\ 1+ [char] a -           \ Temporary for testing. Map to a=1 etc
\ *************************************************************
   >r 1 /string r>
   caseInsens
   if
      >lower dup [new] swap >upper [+]    ( -- set )
      LeafCharSetNode new
   else
      LeafCharNode new
   then
;

\ yields is used when a name is not required for a regular expression
\ or symbol

: yields  ( [tree1] token tree2 -- tree )
   acceptor ?<|>
;

\ symbol parses a target keyword and builds a syntax tree of concatenated
\ character nodes. It does not need tree on the stack if it is the first
\ time that symbol or denotes is executed after begin-symbols 

: symbol   ( [tree] n -- tree2 )
   parse-name                 ( -- [tree] n caddr u )
   dup 0= abort" No string for symbol to parse"
   newKWNode >r               ( -- [tree] n caddr' u' ) ( R: -- tree3 )
   begin
      dup
   while
      newKWNode r> swap <.> >r
   repeat
   2drop                      ( -- [tree] n )
   r> yields                  ( -- tree2 )
;

\ regexp gives a name to a regular expression e.g. for an identifier.
\ When the name is executed it either leaves its tree (first use)
\ or a clone of its tree (second and subsequent uses) on the stack

: regexp    ( tree -- )
   create , 0 ,
   does>    ( -- tree )
      dup @ swap cell+ dup @  ( -- tree ad f )
      if
         drop clone           ( -- tree2 )
      else
         -1 swap !            ( -- tree ) \ Ensure will be cloned in future
      then
;

\ denotes is used to associate a token with a previously defined regular
\ expression and to incorporate that regular expression's syntax tree into 
\ the overall syntax tree. It does not need tree1 on the stack if it is the
\ first time that symbol or denotes is executed after begin-symbols.

: denotes   ( [tree] token -- tree2 )
   ' execute                  ( -- [tree] token tree3 )
   yields                     ( -- tree2 )
;

\ -----------------------------------------------------------------------------
\ These are used to bracket the set of translation rules from lexeme to token

: begin-symbols cr cr ." Creating the syntax tree..." true firstDef ! ;

\ end-symbols uses the tree as the pattern to be returned, this is just for
\ convenience as it will never be used in the run-time fsm (I think?)

: end-symbols       ( tree "<spaces>name" -- )
   dup acceptor               ( -- tree2 )
   constant                   ( -- )
;

\ -----------------------------------------------------------------------------
\  Called by the user to initialise various items

: setMaxChar   ( u -- ) \ u is the maximum character value to be used
   1+ dup to ##
   dup s" :set (cs) (cs) to CharSet" evaluate
   1+ setRowSize
   0 to TransTable
;

127 setMaxChar    \ default value for ASCII

\ -----------------------------------------------------------------------------
\ Can be used to declare token values in tokens.fth (not mandatory but useful
\ as token can be defined differently in the user application, e.g. see the
\ BNF to Gray code converter)

variable tokenval 1 tokenval !

: token ( "<spaces>name" -- ) ( use: token name ) ( name: -- n )
   tokenval @ constant
   1 tokenval +!
;

\ -----------------------------------------------------------------------------
\ The word to be called to generate the lex tables

: lexgen    ( tree -- )
   s" createPositionSet (PSC)" evaluate
   cr ." Decorating the syntax tree..."
   dup updateSyntaxTree
   dup createLeafMap
   dup updateFollowPos
   cr ." Building the transition table..."
   buildTransTable         ( -- )
   cr ." Number of FSM states: " #states @ .
   loadLexTokens
   cr ." Generating output data..."
   buildLexArrays

\ *** Testing only ***
\ .s quit

   cr ." Saving data to file..."
   saveAllTables
   cr ." Run completed" cr
;

\ -----------------------------------------------------------------------------

.( UserInterface.fth loaded. ) .s

