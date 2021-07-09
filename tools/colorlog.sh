#! /bin/bash

BINDIR=$(dirname $0)

tail -n 1000 $1 | \
$BINDIR/colorcat.sh -y ' WARN ' | \
$BINDIR/colorcat.sh -r ' ERROR ' | \
$BINDIR/colorcat.sh -c ' DEBUG ' | \
$BINDIR/colorcat.sh -g 'ENTER|CALLED' | \
$BINDIR/colorcat.sh -b 'LEAVE' | \
less -R
