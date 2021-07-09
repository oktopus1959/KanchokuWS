#pragma once

#include "string_utils.h"
#include "misc_utils.h"

namespace VkbTableMaker {
    // ひらがな50音図配列を作成する (あかさたなはまやらわ、ぁがざだばぱゃ)
    void MakeVkbHiraganaTable(wchar_t* table);

    // カタカナ50音図配列を作成する (アカサタナハマヤラワ、ァガザダバパャヮ)
    void MakeVkbKatakanaTable(wchar_t* table);

    // 指定の文字配列をストロークキー配列に変換
    void MakeStrokeKeysTable(wchar_t* table, const wchar_t* targetChars);

    // 指定の文字配列を第1ストロークの位置に従って並べかえる
    void ReorderByFirstStrokePosition(wchar_t* table, const wchar_t* targetChars);

    // 外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
    void MakeExtraCharsStrokePositionTable(wchar_t* faces);

    // 初期打鍵表(下端機能キー以外は空白)の作成
    void MakeInitialVkbTable(wchar_t* faces);

    // ひらがなに到る第1打鍵集合を取得する
    const std::set<int>& GetHiraganaFirstHotkeys();
}

