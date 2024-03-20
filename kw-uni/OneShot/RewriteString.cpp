#include "string_utils.h"

#include "RewriteString.h"
#include "StateCommonInfo.h"
#include "Settings.h"

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

    void convertZenkakuPeriod(MString& ms) {
        for (size_t i = 0; i < ms.size(); ++i) {
            if (ms[i] == 0x3002) ms[i] = 0xff0e;    // 。→．
            else if (ms[i] == 0xff0e) ms[i] = 0x3002;    // ．→。
        }
    }

    void convertZenkakuComma(MString& ms) {
        for (size_t i = 0; i < ms.size(); ++i) {
            if (ms[i] == 0x3001) ms[i] = 0xff0c;    // 、→，
            else if (ms[i] == 0xff0c) ms[i] = 0x3001;    // ，→、
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

    // ひらがな→カタカナ、句読点の変換
    MString TranslateMiscChars(const MString& ms) {
        MString result = STATE_COMMON->IsHiraganaToKatakana() ? utils::convert_hiragana_to_katakana(ms) : ms;
        if (SETTINGS->convertJaPeriod) convertZenkakuPeriod(result);
        if (SETTINGS->convertJaComma) convertZenkakuComma(result);
        return result;
    }
}

