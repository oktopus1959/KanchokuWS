// pch.h: プリコンパイル済みヘッダー ファイルです。
// 次のファイルは、その後のビルドのビルド パフォーマンスを向上させるため 1 回だけコンパイルされます。
// コード補完や多くのコード参照機能などの IntelliSense パフォーマンスにも影響します。
// ただし、ここに一覧表示されているファイルは、ビルド間でいずれかが更新されると、すべてが再コンパイルされます。
// 頻繁に更新するファイルをここに追加しないでください。追加すると、パフォーマンス上の利点がなくなります。

#ifndef PCH_H
#define PCH_H

// プリコンパイルするヘッダーをここに追加します
#include "framework.h"
#include <iostream>
#include <fstream>
#include <array>
#include <vector>
#include <map>
#include <set>
#include <algorithm>
#include <functional>
#include <memory>
#include <locale.h>
#include <iterator>
#include <tuple>
#include <stdio.h>
#include <stdarg.h>
#include <time.h>
#include <regex>

#include "langedge/array_size.hpp"
#include "langedge/ctypeutil.hpp"
#include "langedge/exception.hpp"
#include "langedge/pathname.hpp"
#include "langedge/pathutil.hpp"
#include "langedge/tstring.hpp"

#endif //PCH_H
