\ lists.fs
\ 	$Id: lists.fs,v 1.41 2005/05/29 07:34:21 f Exp $	

\ Version with list reference to list descriptor data field
\ (so next-node works also on the descriptor node, giving node zero).

\ Nodes are referenced by the data's address.

\ ****************************************************************
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

\ ****************************************************************
\ LICENSE:

\ lists.fs
\ This file was written as a part of 'brew',
\ an experiment with evolutionary programming written in Forth.

\ Copyright (C) 2001, 2002 by Robert Epprecht <epprecht@solnet.ch>

\ This program is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation; either version 2
\ of the License, or (at your option) any later version.
\ 
\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.
\ 
\ You should have received a copy of the GNU General Public License along
\ with this program; if not, write to the Free Software Foundation, Inc.,
\ 59 Temple Place, Suite 330, Boston, MA 02111-1307, USA.

\ ****************************************************************


false [if] \ usage comments:
\ ****************************************************************
\ usage (these are the most important words):

\ Define and initiate a list with u data cells each node:
\ The list is referenced by the address of the descriptor nodes data field.
deflist ( u -- list )

\ Define a named list with u data cells each node:
nLIST: ( u -- )

\ Define a named list with one data cell each node:
\ The created list name puts the descriptor node on stack.
LIST: ( compile time: "name" -- )( run time: -- list )

\ Return number of nodes of a list:
nodes ( list -- u )

\ Return number of cell wide data fields of the nodes of a list:
data-fields ( list -- u )

\ Go from a node to the next one (if it exists):
\ (next-node applied to the descriptor node gives node zero)
next-node ( node -- node'|0)

\ Go through all the nodes of a list:
( list )
BEGIN	( current-node )
    next-node
dup WHILE
    ( node ) \ do something, leave node on stack...
REPEAT drop

\ or:
( list )
dup nodes 0 ?DO	( current-node )
    next-node
    ( node ) \ do something, leave node on stack...
LOOP drop

\ Append a new node to a list and store a cell value in it:
>list ( n list -- )

\ Append a new node to a list and store a two cell value in it:
2>list ( d list -- )

\ Get n'th node of a list, slow:
n'th-node ( u list -- node )

empty-list ( list -- )

remove-list ( list -- )

remove-list-recursively ( list -- )

\ Append a new node to a list:
new-node ( list -- new-node)

\ Insert a new node after current node:
insert-after-node ( node list -- inserted-node )

\ Insert a new node after node u (which must exist):
insert-node ( u list -- inserted-node )

\ Do 'something ( node --)' on all nodes of a list:
do-with-all-nodes ( xt list -- )

last-node ( list -- last-node )

unlink ( u list -- )

\ Copy first data cell of each node of from-list to (newly created) nodes
\ of to-list:
copy-simple-list-elements ( from-list to-list -- )

\ Link an existing list as sublist:
list>list ( sublist list -- )

\ Test if a node points to a sub-list:
node-is-list? ( node - list'|false)

\ Test recursively if a value is in the list as the first data element:
listed? ( key list -- node|false )

\ ****************************************************************
[then] \ usage
\ ****************************************************************
\ ****************************************************************


\ Structure of each node:
0
OFFSET: >link	\ *must* be first, because it is used in nodes *and* descriptor
OFFSET: >flags	\ a cell for flags ( I'm generous ;-)
dup CONSTANT node-descriptor-length
( offset )	\  for the list descriptor offsets

\ node flag masks:
0
MASK: is-descriptor
MASK: is-list
\ MASK: <application-flag-name>		\ Use these as ever you like
CONSTANT list-user-flag#		\ start bit for available user flags

\ Note that  >node-descriptor  and  >list-descriptor  are identical.
\ I decided to give them separate definitions here.
: >node-descriptor ( node -- node-descriptor-addr )
    node-descriptor-length - ;


\ Structure of list descriptor:

\ The list descriptor has the same structure as a node with three data cells.
\ The 1st data is the number of cells of the other nodes.
\ The 2nd is the number of nodes, not counting the descriptor node.
\ The 3rd stores the last nodes address for efficiency reasons.
\ Flag is-descriptor describes it as list descriptor.

\ List descriptor structure:
\ 0
\ OFFSET: >link		already compiled
\ OFFSET: >flags	already compiled
( offset )		\ offset is on stack from the node-descriptor offsets
\ ====> the descriptor node is referenced by next address (1st data cell addr)
OFFSET: >data-cells#	\ number of data cells in each node
OFFSET: >nodes#		\ element counter
OFFSET: >last-node	\ last nodes address (empty list: descriptor node addr)
CONSTANT list-descriptor-length

\ Note that  >list-descriptor  and  >node-descriptor  are identical.
\ I decided to give them separate definitions here.
: >list-descriptor ( list -- list-descriptor-start-addr )
    \  starting with an offset, we must use *node* length here
    node-descriptor-length - ;

: list>flags ( list -- list-flags-addr )
    [ 0 >list-descriptor >flags ] literal  + ;	\ encapsulate >flags

\ List descriptors and nodes can be allocated, in buffers, in the data space...
DEFER <list-allocate> ( u -- addr )	\ allocate a list buffer
: here-list-allot ( u -- addr )
    dup              ( u u )
    here swap allot  ( u addr )
    dup rot erase ;  ( addr )
\ ' here-list-allot IS <list-allocate>
DEFER <list-free> ( addr -- )		\ free a list buffer
\ ' drop IS <list-free>			\ cheating a bit...

: allocated-list-allocate ( u -- addr )
    dup allocate ABORT" allocated-list-allocate: Couldnt allocate list."	( u addr )
    dup rot erase ;				( addr )
' allocated-list-allocate IS <list-allocate>

: free-allocated-list ( addr -- )
    free ABORT" free-allocated-list: Couldn't free list." ;
' free-allocated-list IS <list-free>

\ Define and initiate a list with u data cells each node:
\ deflist ( data-cells -- list )	\ list = addr of zero node
\ The list is referenced by the address of the descriptor nodes data field
: deflist ( u -- list )
    dup 1 < ABORT" deflist: lists must have at least one data cell."
    list-descriptor-length <list-allocate> ( u list-descriptor-start )
    dup  >link		off
    dup  >flags is-descriptor is-list or swap !
    tuck >data-cells#	!
    dup  >nodes#	off
    dup  >last-node >r
    node-descriptor-length +	\ same structure as nodes
    dup r> ! ;			\ list address itself as last-node

\ Define a named list with u data cells each node:
\ The created list name puts the descriptor node on stack.
: nLIST: ( "name"  u -- )
    deflist
    CREATE ,
    DOES> @ ; ( -- list )

\ Define a named list with one data cell each node:
\ The created list name puts the descriptor node on stack.
\ ( compile time: "name" -- ) ( run time: -- list )
: LIST: ( "name" -- )   1 nLIST: ;

\ Return number of nodes of a list:
: nodes ( list -- u )
    [ 0 >list-descriptor >nodes# ] literal +  @ ;

\ Return number of cell wide data fields of the nodes of a list:
: data-fields ( list -- u )
    [ 0 >list-descriptor >data-cells# ] literal +  @ ;

\ Go from a node to the next one (if it exists):
\ (next-node applied to the descriptor node gives node zero)
: next-node ( node -- node'|0)
    [ 0 >node-descriptor >link ] literal +  @ ;

: next-node-and-value ( node -- node' n true | false )
    next-node  dup IF dup @ true THEN  ;

\ Get n'th node of a list, slow:
: n'th-node ( u list -- node )
    >list-descriptor
    2dup >nodes# @ 1- > ABORT" n'th-node: node does not exist."
    >link @   swap 0 ?DO next-node LOOP ;

\ Return last node of a list.
\ An empty list returns the descriptor node (so it returns itself).
: last-node ( list -- last-node )
    [ 0 >list-descriptor >last-node ] literal + @ ;

\ Determine last node (slow function going through the whole chain):
\ An empty list returns the descriptor node (so it returns itself).
: get-last-node ( list -- last-node )
    dup nodes 0 ?DO  next-node  LOOP ;

\ Fix link and last-node-pointer after removing previous last node:
: update-last-node ( list -- )
    dup get-last-node						\ get node
    dup [ 0 >node-descriptor >link ] literal + off		\ fix link
    swap  [ 0 >list-descriptor >last-node ] literal +  ! ;	\ fix pointer

: unlink-last-node ( list -- )
    dup nodes 0= IF  drop EXIT  THEN

    dup >list-descriptor >r		( list  r: list-descriptor-start )
    r@ >last-node @ node-descriptor-length - <list-free>
    -1 r> >nodes# +!			( list )
    update-last-node ;

: unlink ( u list -- )			\ node must exists.
    swap >r				( list  r: u )

    dup nodes r@ = IF  rdrop  unlink-last-node  EXIT   THEN

    \ decrease node count:
    -1  over [ 0 >list-descriptor >nodes# ] literal +  +!	\ count nodes

    \ get node before (check for zero node first: avoid node -1):
    r@ dup IF
	1- swap n'th-node	( previous-node  r: u )
	dup >node-descriptor	( previous-node prev-descriptor  r: u )
    ELSE drop				\ unlinking node zero
	dup >list-descriptor	( list list-desciptor  r: u ) 
    THEN rdrop			( previous-node previous-descriptor )
    >link swap			( broken-link-addr previous-node )
    next-node			( broken-link-addr node-to-unlink )
    >node-descriptor dup >link @ rot !	\ fix previous link
    <list-free> ;			\ free node

\ Free all nodes of a list.
\ (note that sublists are *not* touched themselves).
: empty-list ( list -- )
    >r

    r@  [ 0 >list-descriptor >nodes# ] literal +
    dup @ 0= IF  drop rdrop EXIT  THEN			\ empty list: done

    ( nodes#-addr ) off					\ nodes = 0
    r@ r@ [ 0 >list-descriptor >last-node ] literal + !	\ last node = list

    \ free node memory:
    r@ next-node
    BEGIN
	dup next-node
	swap >node-descriptor <list-free>
	dup 0=
    UNTIL
    drop

    r>  [ 0 >list-descriptor >link ] literal +  off ;	\ fix link: off

\ Remove a list freeing menory:
: remove-list ( list -- )		\ don't do this on named lists...
    dup empty-list
    >list-descriptor <list-free> ;

\ Remove node and all following nodes.
: remove-node&following ( first-node-to-remove# list -- )
    >r
    dup 0= IF  drop r> empty-list EXIT  THEN
    dup r@ nodes > ABORT" remove-node&following: Node does not exist."

    dup r@  [ 0 >list-descriptor >nodes# ] literal +  !

    1- r@ n'th-node		( last-node-to-preserve  r: list )
    dup r> [ 0 >list-descriptor >last-node ] literal +  !
    				( last-node-to-preserve )
    dup next-node swap		( 1st-node-to-remove last-node-to-preserve )
    \ fix link:
    [ 0 >node-descriptor >link ] literal +  off	( 1st-node-to-remove )
    BEGIN
	dup next-node swap	( next-node current-node )
	>node-descriptor <list-free>
	dup 0=
    UNTIL
    drop ;

\ Test if a node points to a sub-list:
: node-is-list? ( node - list'|false)
    dup [ 0 >node-descriptor >flags ] literal +  @ is-list and IF
	@
    ELSE
	drop false
    THEN ;

: remove-list-recursively ( list -- )	\ don't do this on named lists.
    >r

    r@ r@ nodes 0 ?DO
	next-node
	dup node-is-list? dup IF
	    RECURSE
	ELSE drop THEN
    LOOP drop

    r> remove-list ;

: allocate-node ( data-cells -- node )
    cells node-descriptor-length + <list-allocate> node-descriptor-length + ;

\ Append a new node to a list:
: new-node ( list -- new-node)
    >list-descriptor >r			( r: list-descriptor )

    r@ >last-node @ [ 0 >node-descriptor >link ] literal +
					( last-link-addr  r: list-descriptor )
    r@ >data-cells# @ allocate-node	( link-addr new-node )
    dup  r@ >last-node  !
    dup rot !	( new-node )		\ set link
    1 r> >nodes# +! ;			\ increase node count

\ Go to the next node if it exists, else create one:
: next-or-new-node ( node list -- node )
    >r
    next-node				\ switch to next node
    dup 0= IF
	drop
	r@ new-node			\ adding one if none left
    THEN
    rdrop ;

\ Give n'th node if it exists, add one if u = nodes, else abort.
: n'th-or-new-node ( u list -- node )
    2dup nodes < IF  n'th-node EXIT  THEN

    tuck nodes <> ABORT" n'th-or-new-node: Index error."
    new-node ;

\ Append a new node to a list and store a cell value in it:
: >list ( n list -- )   new-node ! ;	\ makes a new node and stores n
[DEFINED] to-list [IF]
    ' >list is to-list
[THEN]

\ Append a new node to a list and store two cell value in it:
: 2>list ( d list -- )   new-node 2! ;	\ makes a new node and stores d

\ Do 'something ( node --)' on all nodes of a list:
: do-with-all-nodes ( xt list -- )
    dup nodes 0 ?DO ( xt actual-node )
	next-node
	2dup swap EXECUTE
    LOOP
    2drop ;

\ Copy first data cell of each node of from-list to (newly created) nodes
\ of to-list:
: copy-simple-list-elements ( from-list to-list -- )
    >r
    BEGIN
	next-node-and-value
    WHILE
	 r@ >list
    REPEAT
    rdrop ;

\ Link an existing list as sublist:
: list>list ( sublist list -- )
    new-node
    dup [ 0 >node-descriptor >flags ] literal +  dup @ is-list or swap !
    ! ;

\ Insert a new node after current node:
: insert-after-node ( node list -- inserted-node )
    >r						( node  r: list )
    [ 0 >node-descriptor >link ] literal +	( link-addr  r: list )
    dup @ dup 0= IF			( link-addr linked-to  r: list )
	2drop
	r@ new-node			\ was last node, add one, done.
	r> update-last-node
	EXIT
    THEN				( link-addr linked-to  r: list )

    \ Insert a new node after node:
    1 r@  [ 0 >list-descriptor >nodes# ] literal +  +! 
    r> data-fields allocate-node >r	( link-addr linked-to  r: new-node )
    \ link next:
    r@ [ 0 >node-descriptor >link ] literal + !	  ( link-addr  r: new-node )
    r@ swap !				\ link new node
    r> ;				\ return new node

\ Insert a new node after node u (which must exist):
: insert-node ( u list -- inserted-node )
    >r  r@ n'th-node	( node  r: list )
    r> insert-after-node ;

\ Test recursively if a value is in the list as the first data element:
: listed? ( key list -- node|false )
    dup nodes 0 ?DO	( key actual-node )
	next-node
	dup @ third = IF  nip unloop EXIT  THEN   \ works on whole sublists too
	dup node-is-list? dup IF
	    >r over r> RECURSE dup IF
		>r 2drop r> unloop EXIT
	    ELSE drop THEN
	ELSE drop THEN
    LOOP
    2drop
    FALSE ;

\ Test if key is as first data element in the list and return node *index*
\ and TRUE. If key is not found return FALSE. This is *not* recursive.
\ See listed? for a similar recursive word giving node address as result. 
: key>list-index ( key list -- index TRUE | false )
    dup nodes 0 ?DO	( key current-node )
	next-node
	dup @ third = IF
	    2drop
	    i TRUE
	    UNLOOP EXIT
	THEN
    LOOP

    2drop FALSE ;

\ Add data to the sublist in the last node of top list
: to-last-nodes-sublist! ( data top-list -- )   last-node @ >list ; 

\ Copy simple list elements from list-0 list-1 ... list-n to list-all
: concat-lists-simple ( list-0 list-1 ... list-n n list-all -- )
    >r		( list-0 list-1 ... list-n' u  r: list-all )
    BEGIN
	dup WHILE
	    1- swap 
	    r@ copy-simple-list-elements
    REPEAT
    drop rdrop ;

\ Empty list-all and copy-simple-list-elements from a couple of other lists:
: sum-lists-simple ( list-0 list-1 ... list-n n list-all ... )
    dup empty-list concat-lists-simple ;



false [IF] \ testing.
    LIST: top
    LIST: sub
    222 sub >list
    sub top list>list

    cr .( Testing list as sublist: )
    sub cr .( sub=) .
    top dbg next-node dup .(  top-node=) .  .( top-node-data=) ? 

    222 sub listed?
    cr [IF] .( OK: item listed.) [ELSE] bell .( BUG: item not listed.) [THEN]

    777 sub listed?
    cr [IF]
	bell .( BUG: missing item listed.)
    [ELSE]
	.( OK: missing item not listed.)
    [THEN]

    sub top listed?
    cr [IF] .( OK: sublist listed.) [ELSE] .( BUG: sublist not listed.) [THEN]

    222 top listed?
    cr [IF]
	.( OK: sublist item listed.)
    [ELSE]
	.( BUG: sublist item not listed.)
    [THEN]
    CR BYE
[THEN]

false [IF] \ Words needed only for testing and debugging:

: .list-descriptor-data ( list -- )
    cr ." list address:			" dup .
    ." 	descriptor-length: " list-descriptor-length .
    >list-descriptor >r

    cr ." list-descriptor address:	" r@ .
    cr ." >link				" r@ >link dup . @ .
    cr ." >flags				" r@ >flags dup . @ .
    cr ." >data-cells#  (list address)	" r@ >data-cells# dup . @ .
    cr ." >nodes#				" r@ >nodes# dup . @ .
    cr ." >last-node			" r@ >last-node dup . @ .
    rdrop
    cr ;

: .all-node-data ( list node -- )	\ for testing
    swap data-fields	( node data-fields )
    dup 1 = IF
	drop  @ .
    ELSE
	cr
	0 ?DO
	    i [char] 0 + emit ." :"
	    dup i cells + @ .
	LOOP
	drop
    THEN ;

: .all-list-data ( list -- )	\ for testing
    dup
    dup nodes 0 ?DO		( list current-node )
	next-node
	dup node-is-list?
	dup IF
	    cr ." list as element " i . ." " cr
	    RECURSE
	    cr ." continue with old list:" cr
	ELSE drop
	    2dup .all-node-data
	THEN
    LOOP 2drop cr ." done" ;

page
.( testing unlink-last-node: )
cr

LIST: testlist
testlist .list-descriptor-data

testlist unlink-last-node
testlist .list-descriptor-data

22 testlist >list
testlist .list-descriptor-data

testlist unlink-last-node
testlist .list-descriptor-data

55 testlist >list
77 testlist >list
testlist unlink-last-node
testlist .list-descriptor-data
testlist unlink-last-node

\ testlist .all-list-data

\ page
.( testing >list and empty-list: )
cr

33 testlist >list
44 testlist >list
55 testlist >list
testlist .list-descriptor-data

testlist empty-list
testlist .list-descriptor-data

[THEN]

false [IF] \ testing.  Needs the above test words too.
    cr
    cr .( Testing unnamed lists: )
    1 deflist
    dup new-node 0 swap !
    dup new-node 1 swap !
    dup new-node 2 swap !
    dup 3 swap >list
    dup nodes
    cr 4 over = [IF] .( OK:) [ELSE] bell .( BUG:) [THEN] .(  nodes=) .

    cr .( DATA: )
    0 over n'th-node @ .
    1 over n'th-node @ .
    2 over n'th-node @ .
    3 over n'th-node @ . cr
    cr .( Adding a new node as list: )

    1 deflist VALUE another-list
    cr .( testing list>list )
    99999 another-list >list
    another-list over list>list
    cr another-list over listed? [IF]
	.( OK: sublist as list listed.)
    [ELSE]
	.( BUG: sublist as list *not* listed.)
    [THEN]

    cr 99999 over listed? [IF]
	.( OK: sublist item listed.)
    [ELSE]
	bell .( BUG: sublist item not listed.)
    [THEN]

    cr .( Adding another node:)
    dup new-node 5 swap !

    dup nodes cr .( nodes: ) .

    cr cr .( .all-list-data) cr
    dup .all-list-data

    cr
    cr .( Testing 'remove-list' depth before:	) depth .
    remove-list
    cr .( Testing 'remove-list' depth after:	) depth .

    cr
    cr .( testing LIST: and >list : )
    LIST: testlist
    0  testlist >list
    10 testlist >list
    20 testlist >list
    30 testlist >list
    42 testlist >list
    cr testlist .all-list-data

    cr
    cr .( testing 'insert-after-node')
    testlist next-node next-node next-node testlist insert-after-node 25 swap !
    cr testlist .all-list-data
    testlist next-node next-node next-node next-node next-node
    testlist insert-after-node 35 swap !
    cr testlist .all-list-data

    cr
    cr .( Nodes:	)
    testlist nodes .
    cr .( testing 'insert-node' )
    1 testlist insert-node 15 swap !
    testlist cr .all-list-data cr

    cr .( Testing 'unlink' )
    2 testlist unlink
    cr testlist .all-list-data
    3 testlist unlink
    0 testlist unlink
    cr testlist .all-list-data
    testlist nodes 1- testlist unlink
    cr testlist .all-list-data
    testlist empty-list
    testlist empty-list

    cr .( stack:	) .s

    cr cr BYE
[THEN]

false [IF] \ benchmarking >list

    10000000 CONSTANT times
    LIST: bench-list

: t ( -- )
    cr ." benchmarking >list  n=" times .
    bench-list
    times 0 ?DO
	i over >list
    LOOP
    drop ;

t BYE

\ \ lists.fs,v 1.32		  \ lists.fs,v 1.26		
\ time gforth-fast lists.fs	  time gforth-fast lists.fs	
\ running on: Gforth 0.5.0	  running on: Gforth 0.5.0	
				  				
\ benchmarking >list  n=10000	  benchmarking >list  n=10000	
\ real    0m0.064s		  real    0m6.999s		
\ user    0m0.060s		  user    0m6.960s		
\ sys     0m0.000s		  sys     0m0.020s		
				  				
\ benchmarking >list  n=50000	  benchmarking >list  n=50000	
\ real    0m0.136s		  real    2m53.081s		
\ user    0m0.130s		  user    2m52.700s		
\ sys     0m0.000s		  sys     0m0.050s		
				  				
\ benchmarking >list  n=100000	  benchmarking >list  n=100000	
\ real    0m0.227s		  real    11m32.099s		
\ user    0m0.210s		  user    11m30.140s		
\ sys     0m0.020s		  sys     0m0.100s		
				  				
\ benchmarking >list  n=200000	  benchmarking >list  n=200000	
\ real    0m0.409s		  real    46m5.834s		
\ user    0m0.390s		  user    45m59.200s		
\ sys     0m0.020s		  sys     0m0.250s		

\ benchmarking >list  n=1000000
\ real    0m1.883s
\ user    0m1.770s
\ sys     0m0.110s

\ benchmarking >list  n=10000000
\ real    0m18.657s
\ user    0m17.530s
\ sys     0m1.000s

[THEN] \ benchmarking >list
