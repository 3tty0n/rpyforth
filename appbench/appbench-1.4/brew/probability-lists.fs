\ probability-lists.fs
\ 	$Id: probability-lists.fs,v 1.21 2005/04/13 08:18:19 f Exp $	

\ ****************************************************************
\ Select a data set out of many based on a relative probabilities.
\ Probability lists can be nested.
\ ****************************************************************

\ A common usage would be having xt's as data.

\ Optimize speed for searching: compute summed up probabilities.
\ These must be recomputed if something changes.  Uses a dirty flag.
\ There can be nested sub lists, indicated by the 'prob-is-list' flag.
\ (Data ist the list pointers address then).

\ ****************************************************************
\ LICENSE:

\ probability-lists.fs
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
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]


s" random.fs" REQUIRED

\ ****************************************************************


\ Changing size is not required too often, so we allocate memory blocks.

\ Block structure:
\ * descriptor
\ * nodes

\ Descriptor field offsets:
0
OFFSET: >prob-flags	\ *must* be first, used in the nodes too.
OFFSET: >top-sum	\ the probability-summed data of the top element
			\ is copied here for efficency.
OFFSET: >members	\ actual members. index of actual top data set+1
			\ (sub lists are counted as one member)
OFFSET: >limit		\ highest possible index+1
OFFSET: >node-length	\ length of a node in adress units (bytes)
CONSTANT probability-descriptor-size#

0		\ masks for descriptor-flags, node flag uses prob-is-list too
MASK: dirty
MASK: prob-is-list
drop

: dirty? ( list-addr -- flag )   >prob-flags @ dirty and ;    \ not normalized

: dirty! ( list-addr -- )   >prob-flags dup @ dirty or swap ! ;

: this-node ( list-addr index -- addr )
    dup 0< ABORT" this-node: Index negative"
    over >limit @ over <= ABORT" this-node: Index out of range"

    over >node-length @ *	( list-addr offset-in-nodes-field )
    probability-descriptor-size# + + ;

: top-node ( list-addr -- top-node-addr )   dup >members @ 1- this-node ;

: this-is-list? ( list-addr index -- flag )	\ flag is *not* normalized
    this-node >prob-flags @ prob-is-list and ;

\ node structure:
\ 0 OFFSET: >prob-flags		\ *must* be first, used in descriptors too.
cell
OFFSET: >summed-up		\ precomputed data.   see 'dirty'.
OFFSET: >probability		\ relative probability to be selected
OFFSET: >data			\ this IS the data start
\ OFFSET: >other-data-types	\ define later, as appropriate
drop	\ (we can't know the node size here).

: this-probability ( list-addr index -- addr )   this-node >probability ;

: this-data ( list-addr index -- addr )   this-node >data ;

: this-summed ( list-addr index -- addr )   this-node >summed-up ;

: update ( list-addr -- )
    \ Update summed probability data.
    \ (Sub lists get updated when picking from them).

    0			( list-addr sum=0 )
    over >members @ 0 ?DO	( list-addr actual-sum )
	over i this-probability @ +		\ add probabilities
	2dup swap i this-summed !		\ set summed up values
    LOOP
    over >top-sum !					\ top sum in descriptor
    >prob-flags dup @ [ dirty invert ] literal and swap ! ;   \ clear dirty bit

: more-nodes ( pointer-to-old-list-addr -- )
    \ Increase size of the allocated memory for twice as much nodes to fit.
    \ Changes the pointer to the list stored at the given addr.

    >r
    r@ @ >r	( r: pointer-to-list-addr old-list-addr )

    \ compute new length:
    probability-descriptor-size#		\ descriptor length
    r@ >limit @ 2*	( desc new-items#  r: pointer-to-addr old-list-addr )
    r@ >node-length @ *  +			\ nodes
    ( new-size  r: pointer-to-list-addr old-list-addr )

    r> swap resize ABORT" more-nodes: Couldn't resize"
    dup >limit dup @ 2* swap !			\ double limit
    r> ! ;					\ correct pointer

\ Add a new node.  If a new block must be allocated (resize) this can possibly
\ change the contents of the address storing the list pointer.  Beware of that!
: add-one ( pointer-to-list-addr -- node-base )
    >r
    r@ @	( actual-list-pointer  r: pointer-to-list-addr )
    dup >members @ 1+  over >limit @ = IF
	drop
	r@ more-nodes
	r@ @
    THEN
    rdrop		( actual-list-pointer )

    dup >members >r	( actual-list-pointer  r: addr-members )
    dup r@ @ this-node  over >node-length @ erase	\ erase new node
    1 r> +!						\ increase members
    dup dirty!						\ could go without
    top-node ;


\ ****************************************************************
\  Words to be used from outside:
\  As memory blocks might change behind your back you have to give
\  a pointers address as argument.  Beware of pointer changes!
\ ****************************************************************

: setup-probability-list ( items data-fields -- addr )
    \ Note that you have to store the address in a pointer.
    \ See above.
    3 +		\ cells for >prob-flags >summed-up and >probability
    2dup * cells			\ length of nodes
    probability-descriptor-size# +	( items data-fields length )
    dup allocate ABORT" setup-probability-list: Couldn't allocate"
    >r
    r@ swap erase			( items data-fields  r: addr )
    cells r@ >node-length !
    r@ >limit !
    r> ;

: PROBABILITY-LIST: ( initial-items data-fields -- )
    CREATE
	2dup setup-probability-list
	,				\ current list address.  Might change!
	\ In case we want to free memory and reconstruct we save these here:
	,				\ data fields
	,				\ initial items, dynamically doubled
    DOES> ;  ( addr )

false [IF] \ currently unused
    : re-init-probability-list ( addr-of-list-addr -- )
	>r
	r@ @ free ABORT" re-init-probability-list: Couldn't free."
	r@ cell+ 2@ setup-probability-list r> ! ;
[THEN]

TRUE [IF] \ new version searching backwards
: is-in? ( data-as-key pointer-to-list-addr -- node-addr|0 )
    @				( data=key list-addr )
    dup >members @
    dup IF				\ members?
	0 swap 1- ?DO	( data=key list-addr )
	    \ Check if the key matches:
	    \ (it matches whole lists as pointers too)
	    dup i this-data @ 2 pick = IF
		nip i this-node  unloop  EXIT
	    THEN

	    \ Check if it's a list, and descend if so:
	    dup i this-is-list? IF
		2dup i this-data @ EXECUTE RECURSE dup IF
		    >r 2drop r> unloop EXIT
		ELSE drop THEN
	    THEN
	-1 +LOOP
    ELSE drop THEN
    2drop FALSE ;
[ELSE] \ old version searching forwards
: is-in? ( data-as-key pointer-to-list-addr -- node-addr|0 )
    @				( data=key list-addr )
    dup >members @ 0 ?DO	( data=key list-addr )
	\ Check if the key matches:   (it matches whole lists as pointers too)
	dup i this-data @ 2 pick = IF
	    nip i this-node  unloop  EXIT
	THEN

	\ Check if it's a list, and descend if so:
	dup i this-is-list? IF
	    2dup i this-data @ EXECUTE RECURSE dup IF
		>r 2drop r> unloop EXIT
	    ELSE drop THEN
	THEN
    LOOP
    2drop FALSE ;
[THEN]

: it's-node ( data-key pointer-addr -- node )
    \ Give the node addr of a key, adding new keys.
    \ If key is not found in the top list add a new node with probability zero.
    \ As buffer address might change you have to provide a pointers address.
    \ Beware of pointer changes!

    >r			( data-key    r: addr-of-pointer-to-list )
    dup r@ is-in?	( key node|false  r: addr-of-pointer-to-list )
    dup IF
	rdrop
	nip		( node )
    ELSE drop
	r> add-one	( key added-node )
	>r		( key  r: added-node )
	r@ >data !
	r>
    THEN ;

: set-one ( probability data-as-key addr-of-pointer-to-list-addr -- )
    \ Set probability of selecting data key, creating a new node if necessary.
    \ As list address might change you must give a pointers address.
    \ Beware of pointer changes!

    dup @ dirty!
    it's-node >probability ! ;

: set-as-sublist ( probability xt-sublist-pointer addr-of-list-pointer -- )
    \ Set probability of selecting data key, creating a new node if necessary.
    \ As list address might change you must give a pointers xt.
    \ Beware of pointer changes!

    dup @ dirty!
    it's-node				\ adds new node automatically
    dup >prob-flags dup @ [ prob-is-list dirty or ] literal or swap !
    >probability ! ;

: change-one ( probability-diff data-as-key addr-of-pointer-to-list-addr -- )
    \ Change probability of selecting data key, creating a new node if missing.
    \ As list address might change you must give a pointers address.
    \ Beware of pointer changes!

    dup @ dirty!
    it's-node >probability >r r@ +!
    r@ @ 0 max r> ! ;		\ do not accept negative probabilities...

: pick-one ( addr-of-tree-pointer -- selected-data-addr )
    \ Check dirty bit and update sums if necessary, then
    \ pick a item of the tree based on relative probabilities.
    \ Return it's data address.
    \
    \ This word relies on things being set up properly, no checks...
    \
    \ Here we could use the list address instead of the pointer.
    \ I use the pointer address for consistency.

    @ >r			( r: addr )

    r@ >members @ 0= ABORT" pick-one: No one set."

    r@ dirty? IF r@ update THEN

    r@ >node-length @			( node-length        r: addr )
    r@ probability-descriptor-size# +	( length first-node  r: addr )
    r@ >top-sum @ random-ranged
    1 max				\ if random gives zero, the first node
					\ would be taken independent if it's
					\ probability (being zero possibly).
    >r					( length first-node  r: addr random )
    BEGIN				( length actual-node  r: addr random )
	dup >summed-up @ r@ <
    WHILE
	over +				\ next node
    REPEAT				( length actual-node  r: addr random )
    2rdrop nip				( selected-node )
    dup >data swap			( data-addr selected-node )
    >prob-flags @ prob-is-list and IF
	@ EXECUTE RECURSE
    THEN ;

\ how many items in a list (top level, not recursive)
\ note that this word takes the list address, not a pointer.
: how-many ( list-addr -- u )   >members @ ;

: nul-all-probabilities ( pointer-to-list-addr -- )
    @
    dup dirty!
    dup how-many 0 ?DO
	dup i this-probability off
    LOOP
    drop ;
    
false [IF]	\ some testing
    page
    20 constant test-items#
    CREATE test-counters
    here test-items# cells erase
    test-items# cells allot

    cr
    1 1 PROBABILITY-LIST: test
    \ That's mean, to give only one node...  test 'more-nodes'.

    \      test @ 0 this-node . cr
    \      test @ 1 this-node . cr
    \      test @ 2 this-node . cr

    100 0 test set-one
    cr .( 0 test is-in? : )
    0 test is-in? .
    cr .( 99 test is-in? : )
    99 test is-in? .

    16 1 PROBABILITY-LIST: sublist
    \ cr .( sublist: ) sublist dup . @ .
    200  2 sublist set-one
    300  3 sublist set-one
    400  4 sublist set-one
    400  ' sublist  test set-as-sublist

    cr .( sublist test is-in? : )
    sublist test is-in? .

    16 1 PROBABILITY-LIST: sub-sub
    600  6 sub-sub set-one
    800  8 sub-sub set-one
    100  ' sub-sub  sublist set-as-sublist 

    16 1 PROBABILITY-LIST: sub-sub-sub
    1000  ' sub-sub-sub  sub-sub set-as-sublist
    10 10 sub-sub-sub set-one
    100 11 sub-sub-sub set-one
    1000 12 sub-sub-sub set-one

    16 1 PROBABILITY-LIST: sub-4
    10  ' sub-4  sub-sub-sub set-as-sublist
    1 13 sub-4 set-one
    10 14 sub-4 set-one
    100 15 sub-4 set-one
    1000 16 sub-4 set-one
    10000 17 sub-4 set-one
    100000 18 sub-4 set-one

    test @ how-many cr . cr

: t ( u -- )
    cr ." testing: " dup . cr
    0 ?DO
	1
	test pick-one @
	\ dup .
	cells test-counters +  +!
    LOOP

    test-counters
    test-items# 0 DO
	dup i cells + @
	cr i . .tab .
    LOOP
    drop
    cr ;

    1000000 t
    cr bye
[THEN]


\ ****************************************************************
\ Debugging help: '.xt-pool'
\ ****************************************************************

FALSE [IF]

\ Print all data of an xt probability pool:
: .xt-pool ( list-pointer-addr -- )
    @ >r

    page
    r@ >prob-flags @ ." flags:		" . .tab
    r@ >prob-flags @ dirty and IF ." DIRTY" ELSE ." clean" THEN .tab
    r@ >prob-flags @ prob-is-list and IF ." IS LIST" ELSE ." data" THEN .tab
    r@ >top-sum @ ." top sum:	" . cr
    r@ >members @ ." members:	" . .tab
    r@ >limit @ ." limit:	" . .tab
    r@ >node-length @ ." node length:	" . cr

    cr
    ." node:" .tab ." kind:" .tab .tab ." sum:" .tab ." prob:" .tab ." xt:" cr
    r@
    r> >members @ 0 ?DO	( list-addr )
	i . .tab
	dup i this-is-list? IF
	    ." SUBLIST" .tab
	    dup i this-node >data @ EXECUTE @ dirty? IF
		." DIRTY"
	    ELSE
		." clean"
	    THEN 
	ELSE
	    ." data" .tab
	THEN .tab
	dup i this-node
	dup >summed-up @ . .tab
	dup >probability @ . .tab
	>data @
	dup base @ >r hex . r> base ! .tab
	xt>string type cr
    LOOP
    drop ;

[THEN]
