#!/usr/bin/env sh

PYTHONPATH=`pwd`:`pwd`/pypy python rpyforth/targetrpyforth.py $1
