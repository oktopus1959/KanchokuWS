#!/bin/bash

BINDIR=$(dirname $0)

if [ -z "$1" ]; then
    echo "log file path required" >&2
    exit
fi

_opt="-f"
if [[ "$1" == -* ]]; then
    _opt=
fi

tail $_opt $* | \
    $BINDIR/colorcat.sh -y '====.*(START|TERMINATE).*====| WARN | OUTPUT|\bDecoderImpl\.HandleHotkey\b| states=[^ ]*' | \
    $BINDIR/colorcat.sh -r ' ERROR ' | \
    $BINDIR/colorcat.sh -c ' (INFO|INFOH|WARN2) ' | \
    $BINDIR/colorcat.sh -g 'ENTER|CALLED' | \
    $BINDIR/colorcat.sh -b 'LEAVE' | \
    grep -v ' DEBUG[1-9] ' --line-buffered
