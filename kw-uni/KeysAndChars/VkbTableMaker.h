#pragma once

#include "string_utils.h"
#include "misc_utils.h"

class StrokeTableNode;

namespace VkbTableMaker {
    const size_t OUT_TABLE_SIZE = 200;
    const size_t VKB_TABLE_SIZE = 50;

    // ひらがな50音図配列を作成する (あかさたなはまやらわ、ぁがざだばぱゃ)
    void MakeVkbHiraganaTable(wchar_t* table);

    // カタカナ50音図配列を作成する (アカサタナハマヤラワ、ァガザダバパャヮ)
    void MakeVkbKatakanaTable(wchar_t* table);

    // 指定の文字配列をストロークキー配列に変換
    void MakeStrokeKeysTable(wchar_t* table, const wchar_t* targetChars);

    // 指定の文字配列を第1ストロークの位置に従って並べかえる
    void ReorderByFirstStrokePosition(wchar_t* table, const wchar_t* targetChars, int tableId);

    // 指定の文字配列をストロークの位置に従って並べかえる
    // node: ストロークテーブルノード, table: 出力先のテーブル, targetChars: 並べ替えたい文字配列
    void ReorderByStrokePosition(StrokeTableNode* node, wchar_t* table, const wstring& targetChars, int tableId);

    // 外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
    void MakeExtraCharsStrokePositionTable(wchar_t* faces);

    // 主テーブルに対して指定されたシフト面の単打ストローク表を作成する
    void MakeShiftPlaneKeyCharsStrokePositionTable1(wchar_t* faces, size_t shiftPlane);

    // 副テーブルに対して指定されたシフト面の単打ストローク表を作成する
    void MakeShiftPlaneKeyCharsStrokePositionTable2(wchar_t* faces, size_t shiftPlane);

    // 通常面のキー文字を集めたストローク表を作成する
    void MakeKeyCharsStrokePositionTable(wchar_t* faces);

    // 第2テーブルから通常面のキー文字を集めたストローク表を作成する
    void MakeKeyCharsStrokePositionTable2(wchar_t* faces);

    // 同時打鍵面のキー文字を集めたストローク表を作成する
    void MakeCombinationKeyCharsStrokePositionTable(wchar_t* faces);

    // 初期打鍵表(下端機能キー以外は空白)の作成
    void MakeInitialVkbTable(wchar_t* faces);

    // ひらがなに到る第1打鍵集合を取得する
    const std::set<int>& GetHiraganaFirstDeckeys();

    // 打鍵列からローマ字テーブルを作成してファイルに書き出す
    void SaveRomanStrokeTable(const wchar_t* = 0, const wchar_t* = 0);

    // eelll/JS用テーブルを作成してファイルに書き出す
    void SaveEelllJsTable();

    // デバッグ用テーブルを作成してファイルに書き出す
    void SaveDebugTable();
}

