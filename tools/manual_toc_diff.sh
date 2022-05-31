#! /bin/bash

md=MANUAL.md
diff -w <(tools/make_toc.sh $md) <(sed -n '/## 目次/,/## 元祖漢直窓から受け継いだ機能/ p' $md)
