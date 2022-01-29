#! /bin/bash

diff -w <(tools/make_toc.sh) <(sed -n '1,/## 元祖漢直窓/ p' README.md)
