#! /bin/bash

md=FAQ.md
diff -w <(tools/make_toc.sh $md) <(sed -n '/## 目次/,/## 設定ダイアログ/ p' $md)
