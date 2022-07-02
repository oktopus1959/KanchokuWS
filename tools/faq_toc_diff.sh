#! /bin/bash

md=FAQ/FAQ-${1}.md
diff -w <(tools/make_toc.sh $md) <(sed -n '/## 目次/,/## [^目]/ p' $md)
