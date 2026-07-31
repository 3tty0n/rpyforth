\ gene-pool.fs
\ 	$Id: gene-pool.fs,v 1.13 2002/10/07 06:06:11 f Exp $	

\ List of (internals) gene xt's and relative probabilities to be selected.
\ 'probability-lists.fs' makes this file trivial:

: GENE-POOL: ( initial-size-in-genes -- )	\ (size doubled when needed)
    1 PROBABILITY-LIST: ;

decimal 128 GENE-POOL: gene-primitives		\ size is not critical

LIST: gene-pools

' gene-primitives gene-pools >list


0 >data cell+	OFFSET: >genome-usage
drop

\ Define a named genome pool, size will be doubled when needed:
: GENOME-POOL: ( "name" initial-size-in-genomes -- )
    2 PROBABILITY-LIST: ;

decimal 256 GENOME-POOL: genomes-used

VARIABLE current-genome-pool-xt
' genomes-used current-genome-pool-xt !

: init-genome-pool ( pool-xt -- )	\ 'genomes-used' *must* have a node
    >r
    0  [internal'] noop  r> EXECUTE  change-one ;
current-genome-pool-xt @ init-genome-pool
