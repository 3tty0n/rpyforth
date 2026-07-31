\ float-trigonometry.fs
\ 	$Id: float-trigonometry.fs,v 1.1 2002/02/14 11:51:14 f Exp $	

\ Trigonometric gene primitives.


s" r-r" ' fsin  as-gene
500 to-gene-pool' fsin

s" r-r" ' fcos  as-gene
500 to-gene-pool' fcos

s" r-rr" ' fsincos  as-gene
500 to-gene-pool' fsincos

s" r-r" ' ftan  as-gene
500 to-gene-pool' ftan

s" r-r" ' fasin  as-gene
500 to-gene-pool' fasin

s" r-r" ' facos  as-gene
500 to-gene-pool' facos

s" r-r" ' fatan  as-gene
500 to-gene-pool' fatan

s" rr-r" ' fatan2  as-gene
200 to-gene-pool' fatan2

s" r-r" ' fsinh  as-gene
200 to-gene-pool' fsinh

s" r-r" ' fcosh  as-gene
200 to-gene-pool' fcosh

s" r-r" ' ftanh  as-gene
200 to-gene-pool' ftanh

s" r-r" ' fasinh  as-gene
200 to-gene-pool' fasinh

s" r-r" ' facosh  as-gene
200 to-gene-pool' facosh

s" r-r" ' fatanh  as-gene
200 to-gene-pool' fatanh

s" -r" ' pi  as-gene
200 to-gene-pool' pi
