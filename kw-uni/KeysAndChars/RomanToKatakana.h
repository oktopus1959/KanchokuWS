#include "string_utils.h"

namespace RomanToKatakana {
    // ローマ字定義ファイルを読み込む
    void ReadRomanDefFile(const wstring& defFilePath);

    // ローマ字をカタカタナに変換する
    MString convertRomanToKatakana(const MString& s);
}

