#!/bin/bash

_color=33
if [ "$1" == "-r" ]; then
    _color=31
    shift
elif [ "$1" == "-g" ]; then
    _color=32
    shift
elif [ "$1" == "-y" ]; then
    _color=33
    shift
elif [ "$1" == "-b" ]; then
    _color=34
    shift
elif [ "$1" == "-c" ]; then
    _color=36
    shift
fi

patt="$1"
if [ -z "$patt" ]; then
    $0 '^diff .*' | $0 -r '^-[^-].*' | $0 -g '^\+[^+].*' | $0 -c '^@@ .*'
else
    sed -u -r "s/($patt)/\\x1b[$_color;1m\\1\\x1b[0m/g"
fi

