#! /bin/bash

MD=README.md

diff -w <(tools/make_toc.sh $MD) <(sed -n '/## 目次/,/## 動作環境と注意/ p' $MD)
