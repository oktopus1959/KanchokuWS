#include "string_utils.h"

#include "RewriteString.h"

namespace {
    inline bool isKogaki(wchar_t ch) {
        switch (ch) {
        case L'ゃ':
        case L'ゅ':
        case L'ょ':
        case L'ぁ':
        case L'ぃ':
        case L'ぅ':
        case L'ぇ':
        case L'ぉ':
        case L'ゎ':
            return true;
        default:
            return false;
        }
    }
}

namespace RewriteString {
    // 出力定義文字列を解析して、分離記号の '/' を取り除き、書き換え対象文字列の長さを得る
    // 小書き文字(拗音など)は直前の文字も書き換え対象に含める
    String AnalyzeRewriteString(StringRef s, size_t& rewritableLen) {
        auto ws = utils::replace(s, _T("/"), _T(""));\
        size_t _pos = s.find('/', 0);\
        rewritableLen = _pos <= ws.size() ? ws.size() - _pos : ws.empty() ? 0 : ws.size() == 1 || !isKogaki(ws.back()) ? 1 : 2;
        return ws;
    }
}

