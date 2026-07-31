\ allocation-pointers.fs
\ 	$Id: allocation-pointers.fs,v 1.10 2003/08/27 18:14:07 f Exp $	

\ Deals with allocated memory blocks that will possibly get rezised.


\ Allocation pointers are the upper one of a cell pair used to store
\ the allocation address of allocated memory blocks.
\ The cell below keeps the allocated size (see 'pointer>size').

\ This gives a mechanism to manage pointer size pairs describing memory blocks.

\ I leave memory allocation to the OS.
\ As far as I have seen it does it much better than I can ;-)

\ There's a named allocation pointer variant, see 'ALLOCATION-POINTER:'.
\ At first usage named memory gets allocated automatically.

\ This package gives a mechanisms to allocate memory and to get a
\ initialized pointer handled out of a list of linked pointer arrays.
\ The pointers replace the handles used in previous versions.

\ Usage:
\      256 open-allocate	( initial-size -- handled-pointer )
\      ...
\      \ here you would use the buffer
\      ...
\      close-allocated		( pointer -- )

\ ****************************************************************
\ dependencies:

\ Make sure basics.fs (which defines the mark BASICS.FS) is loaded:
bl parse BASICS.FS dup pad c! pad char+ swap chars move pad find nip 0=
[IF]	s" basics.fs" INCLUDED		[THEN]

s" lists.fs" REQUIRED

\ ****************************************************************


\ Please note that (unlike for strings) the pointer address is
\ the address of the *address* of the allocated memory.
\ This is used much more often than the size.

\ The size of the allocated memory block get's stored in the cell below.
\ 'pointer pointer>size @' gives the allocated size.
\ 'pointer pointer>size 2@' ( -- allocated-address allocated-size).
\ : pointer>size ( addr -- addr-cell )   [ -1 cells ] literal + ;
: pointer>size ( addr -- addr-cell )
    -1 cells POSTPONE literal  POSTPONE + ;  IMMEDIATE

\ Allocate memory and save address and size data at the pointer:
: allocate-to-pointer ( size pointer-address -- )
    >r

    dup allocate
    ABORT" allocate-to-pointer: Couldn't allocate."
    swap r> pointer>size 2! ;

\ Free memory and clear pointer.
\ Use 'free-allocated' only for allocation pointers stored separately
\ like named pointers, *not* for handled ones.
\
\ Note that the size field does *not* get changed when a pointer gets cleared.
\ The allocation address is the key.
: free-allocated ( address-of-pointer -- )
    dup @ free
    ABORT" free-allocated: Couldn't free."
    off ;


\ Arrays of pointers for 'open-allocate':

\ Array structure and size:
2 CONSTANT alloc-ptr-items#			\ size cell and address cell
128 2 - CONSTANT default-pointers#		\ # pointers in one node

\ Each array starts with a descriptor:
\ (Other arrays will get allocated and linked to the last one if needed).
\
\ To speed uf the search for a free pointer there is a count of free pointers
\ in each array, and when one is de allocated, it gets remembered as a free one
\
0
OFFSET: >ptrs-unused				\ count (for speed)
OFFSET: >unused-pointer				\ maybe a free pointer or zero
OFFSET: >ptrs-arrays-link			\ link to next array
dup CONSTANT ptrs-descriptor-length#

\ Double cell data field:
\ Pointer is the *upper* adress cell.
alloc-ptr-items# 1- cells +			\ skip size field
OFFSET: >ptrs-start				\ 1st pointer data
default-pointers# 1- alloc-ptr-items# * cells +	\ skip other pointers/sizes
OFFSET: >ptrs-end				\ array border
drop

\ Allocate one of these arrays:
\ (It's actually a node in a array list).
: allocate-pointer-array ( -- address )
    [ default-pointers# alloc-ptr-items# * cells ] literal
    ptrs-descriptor-length# + >r
    r@ allocate
    ABORT" allocate-pointer-array: Couldn't allocate."
    dup r> erase
    default-pointers# over >ptrs-unused ! ;

\ Give a unused, not initialized  pointer from a linked list of arrays:
\ (This will allocate and link new arrays dynamically, if needed).
: (get-a-pointer) ( start-array -- new-pointer )
    >r

    \ Check if there's a free pointer in this array: 
    r@ >ptrs-unused @ IF			\ a pointer free in this array?
	-1 r@ >ptrs-unused +!		\ use one

	\ If we're lucky there's one remembered from last 'free-pointer'
	r@ >unused-pointer @ dup IF		\ maybe there's one saved?
	    r> >unused-pointer off		\	now it's used...
	    EXIT				\	done
	ELSE drop THEN

	\ No, we have to search for it:
	r> >ptrs-start dup [ default-pointers# 2* cells ] literal + swap DO
	    i @ 0= IF
		i unloop EXIT
	    THEN
	[ alloc-ptr-items# cells ] literal +LOOP
    THEN

    \ This array is full, test new one:
    r> >ptrs-arrays-link >r

    \ Is there a next array?
    r@ @ IF r> @ RECURSE EXIT THEN		\ next array exists, recurse

    \ Open a new array and link it, then recurse.
    allocate-pointer-array  dup r> !

    RECURSE ;

\ Give a pointer back as unused.
: (close-pointer) ( pointer start-array -- )
    >r

    \ Check if the pointer lies within this nodes array:
    dup  r@ >ptrs-start  r@ >ptrs-end within IF	\ pointer is within this array
	dup off
	1 r@ >ptrs-unused +!		\ adjust count
	r> >unused-pointer !		\ remember to speed up next request
	EXIT				\ done
    THEN

    \ Check link:
    r> >ptrs-arrays-link @		\ address linked to
    dup IF
	RECURSE EXIT
    ELSE  drop  THEN

    true ABORT" (close-pointer): Pointer not found in list." ;


\ Allocate and store the start address somewhere:
\ (you could build several lists of linked arays).
allocate-pointer-array
VALUE pointer-arrays

\ Get a free uninitialised pointer:
: get-a-pointer ( -- new-pointer )  pointer-arrays (get-a-pointer) ;

\ Give back unused pointer:
: close-pointer ( pointer-address -- )  pointer-arrays (close-pointer) ;  

\ Do memory allocation and return a initialised pointer:
: open-allocate ( size -- address-of-handled-pointer )
    get-a-pointer dup >r allocate-to-pointer r> ;

\ Free memory giving back the pointer:
\ Always use 'close-allocated' to deallocate and give back a handled pointer.
\ (*not* for single ones or named allocation pointers).
: close-allocated ( address-of-handled-pointer -- )
    dup free-allocated
    close-pointer ;

\ Resize memory represented by a allocation pointer, adjust pointer data:
: resize-allocated ( new-size address-of-pointer -- )
    >r
    r@ @ over resize
    ABORT" resize-allocated: Couldn't resize."
    swap r> pointer>size 2! ;

false [IF] \ test and usage example
    256 open-allocate
    \ here you would use the buffer
    512 over resize-allocated
    close-allocated
[THEN]


\ Named allocation pointer variant:
\ At first usage memory gets allocated automatically.
\
\ Usage of the named variant:
\	128 ALLOCATION-POINTER: named-pointer
\	\ now use the pointer doing things like:
\	s" This ist the start of a string. "  named-pointer string!
\	s" This gets appended to the string." named-pointer cat
\	named-pointer free-allocated
\ Named variant: single allocation pointer, *not* in a array list:
\ (Opens automatically at first usage).

: ALLOCATION-POINTER: ( default-size -- )
    CREATE
	alloc-ptr-items# 0 DO
	    0 ,
	LOOP
	,				\ store default size
    DOES> ( -- address-of-pointer )
	[ alloc-ptr-items# 1- cells ] literal +
	dup @ IF EXIT THEN		\ test if allocated, done

	dup cell+ @			( pointer default-size )
	over allocate-to-pointer ;

false [IF] \ test and usage example
    128 ALLOCATION-POINTER: named-allocation-pointer
    \ dbg named-allocation-pointer drop
    named-allocation-pointer free-allocated
    \ dbg named-allocation-pointer drop
[THEN]



\ Words intended for error recovery:
\ 'opened-buffers-to-list ( -- list )' 'close-not-listed-buffers ( list -- )'

\ Sometimes you can't know which buffers have been opened during a process
\ when something goes wrong.  These words make it possible to preserve
\ opened buffers in a list before the process starts, and close all buffers
\ that have been opened by the process later (in case of an error).
\ Note that the process itself must *not* close buffers that have been
\ opened before for this to work.

: opened-buffer-this-array ( array-address -- pointer0 pointer1 ... )
    >r
    r@ >ptrs-end r> >ptrs-start DO
	i @ IF i THEN
    [ alloc-ptr-items# cells ] literal +LOOP ;
    
\ Give the pointers of all open buffers and their number:
: which-buffers-opened ( -- handle-0 handle-1 ... handle-n-1 n )
    depth >r

    pointer-arrays >r
    r@ opened-buffer-this-array
    BEGIN
	r> >ptrs-arrays-link @
    dup WHILE
	>r
	r@ opened-buffer-this-array
    REPEAT
    drop

    depth r> - ;

\ Return a list of all opened handles:
: opened-buffers-to-list ( -- list )
    1 deflist
    dup (scratch) !
    which-buffers-opened 0 ?DO
	(scratch) @ >list
    LOOP ;

\ Close all buffers of this array that are *not* in a list:
: close?-new-bufs-this-array ( list array-address -- list )
    >r

    r@ >ptrs-end r> >ptrs-start DO
	i @ IF
	    i over listed? 0= IF i close-allocated THEN
	THEN
    [ alloc-ptr-items# cells ] literal +LOOP ;

\ Close all buffers not included in the list and remove list:
: close-not-listed-buffers ( list -- )
    pointer-arrays >r
    r@ close?-new-bufs-this-array
    BEGIN
	r> >ptrs-arrays-link @
    dup WHILE
	>r
	r@ close?-new-bufs-this-array
    REPEAT
    drop
    remove-list ;
