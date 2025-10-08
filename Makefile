.PHONY: setup-pypy
setup-pypy:
	git clone https://github.com/pypy/pypy.git --dept=1


_pypy_binary/bin/python:  ## Download a PyPy binary
	mkdir -p _pypy_binary
	python3 get_pypy_to_download.py
	tar -C _pypy_binary --strip-components=1 -xf pypy.tar.bz2
	rm pypy.tar.bz2
	./_pypy_binary/bin/python -m ensurepip
	./_pypy_binary/bin/python -mpip install "hypothesis<4.40" junit_xml coverage==5.5 "pdbpp==0.10.3"
