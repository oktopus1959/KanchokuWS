#! /bin/bash

. ~/bin/debug_util.sh

TXTDIR=$(dirname $0)/../..
SRCTXT=$TXTDIR/eelll.txt
OUTORIGTXT=$TXTDIR/eelll.reloc.orig.txt
OUTTXT=$TXTDIR/eelll.reloc.txt

KANJIGROUP=$(cat ../reloc-kanji.txt | ruby -e "result=''; while line=gets; result+=line.strip; end; puts result")

RUN_CMD -m "grep -v 'Lesson-chars' $SRCTXT | grep '[$KANJIGROUP].*[$KANJIGROUP].*[$KANJIGROUP]' > $OUTORIGTXT"

RUN_CMD -m "cp $OUTORIGTXT $OUTTXT"
