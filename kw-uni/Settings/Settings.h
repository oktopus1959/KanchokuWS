#pragma once

#include "string_type.h"

struct Settings {
    bool firstUse;              // 最初の起動か

    tstring workDir;            // 作業ディレクトリ
    tstring tableFile;          // ストロークテーブル
    tstring charsDefFile;       // Hotkey から文字への変換
    tstring easyCharsFile;      // 簡易打鍵文字ファイル
    tstring bushuFile;          // 部首合成辞書
    tstring bushuAssocFile;     // 部首連想辞書
    tstring mazegakiFile;       // 交ぜ書き辞書
    tstring historyFile;        // 履歴
    tstring historyUsedFile;    // 使用順
    tstring historyExcludeFile; // 履歴排除
    tstring historyNgramFile;   // Nグラム履歴

    int backFileRotationGeneration;         // 辞書ファイル保存世代数

    size_t histKanjiWordMinLength = 4;      // 履歴登録対象となる漢字文字列の最小長
    size_t histKatakanaWordMinLength = 4;   // 履歴登録対象となるカタカナ文字列の最小長
    size_t histKanjiWordMinLengthEx = 2;    // 履歴登録対象となる難打鍵文字を含む漢字文字列の最小長

    size_t histHiraganaKeyLength = 2;       // ひらがな履歴検索キーの長さ
    size_t histKatakanaKeyLength = 2;       // カタカナ履歴検索キーの長さ
    size_t histKanjiKeyLength = 1;          // 漢字履歴検索キーの長さ

    size_t abbrevKeyMaxLength = 16;         // 短縮履歴キーの最大長

    bool autoHistSearchEnabled = false;     // 自動履歴検索を行う
    bool histSearchByCtrlSpace = false;     // Ctrl-Space で履歴検索を行う
    bool histSearchByShiftSpace = false;    // Shift-Space で履歴検索を行う
    bool selectFirstCandByEnter = false;    // 履歴の第1候補をEnterキーで選択する
    int histDelHotkeyId = 41;               // 履歴削除のHotKeyID
    int histNumHotkeyId = 45;               // 履歴文字数指定のHotKeyID

    bool useArrowKeyToSelectCandidate = true;   // 矢印キーで履歴候補選択を行う
    bool handleShiftSpaceAsNormalSpace = true;    // Shift-Space を通常の Space として扱う (ただし histSearchByShiftSpace が優先)

    size_t mazeYomiMaxLen = 10;             // 交ぜ書き変換時の最長入力読み長
    size_t mazeGobiMaxLen = 3;              // 語尾あり交ぜ書きの最長語尾長

    bool convertShiftedHiraganaToKatakana = false; // Shift入力された平仮名をカタカナに変換する
    bool convertJaPeriod = false;           // 「。」と「．」を相互変換する
    bool convertJaComma = false;            // 「、」と「，」を相互変換する

    // for Debug
    bool debughState = false;               // State モジュールで DebugH を有効にする
    bool debughMazegaki = false;            // mazegaki モジュールで DebugH を有効にする
    bool debughHistory = false;             // history モジュールで DebugH を有効にする
    bool debughStrokeTable = false;         // strokeTable モジュールで DebugH を有効にする
    bool debughBushu = false;               // bushuComp/bushuAssoc モジュールで DebugH を有効にする
    bool debughZenkaku = false;             // Zenkaku モジュールで DebugH を有効にする
    bool debughKatakana = false;            // Katakana モジュールで DebugH を有効にする
    bool debughMyPrevChar = false;          // MyChar/PrevChar モジュールで DebugH を有効にする

public:
    void SetValues(const std::map<tstring, tstring>&);

    static std::unique_ptr<Settings> Singleton;
};

#define SETTINGS  (Settings::Singleton)