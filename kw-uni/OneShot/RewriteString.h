#pragma once

namespace RewriteString {
    // 出力定義文字列を解析して、分離記号の '/' を取り除き、書き換え対象文字列の長さを得る
    String AnalyzeRewriteString(StringRef s, size_t& rewritableLen);
}
