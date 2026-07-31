#!/usr/bin/env sh

PYTHONPATH=`pwd`:`pwd`/pypy pypy rpyforth/targetrpyforth.py $1
