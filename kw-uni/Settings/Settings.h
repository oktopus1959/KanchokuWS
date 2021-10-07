#pragma once

#include "string_type.h"

struct Settings {
    bool firstUse;              // 最初の起動か

    tstring rootDir;            // ルートフォルダ
    tstring tableFile;          // ストロークテーブル
    tstring charsDefFile;       // Deckey から文字への変換
    tstring easyCharsFile;      // 簡易打鍵文字ファイル
    tstring bushuFile;          // 部首合成辞書
    tstring autoBushuFile;      // 自動部首合成辞書
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
    //bool histSearchByCtrlSpace = false;     // Ctrl-Space で履歴検索を行う
    //bool histSearchByShiftSpace = false;    // Shift-Space で履歴検索を行う
    bool selectFirstCandByEnter = false;    // 履歴の第1候補をEnterキーで選択する
    int histDelDeckeyId = 41;               // 履歴削除のDecKeyID
    int histNumDeckeyId = 45;               // 履歴文字数指定のDecKeyID
    size_t histHorizontalCandMax = 5;       // 履歴候補の横列鍵盤表示の際の最大数

    bool histMoveShortestAt2nd = false;     // 最短長履歴文字列を2番目に表示する

    bool useArrowKeyToSelectCandidate = true;   // 矢印キーで履歴候補選択を行う
    //bool handleShiftSpaceAsNormalSpace = true;    // Shift-Space を通常の Space として扱う (ただし histSearchByShiftSpace が優先)

    //bool mazegakiByShiftSpace = true;       // Shift-Space で交ぜ書き変換
    bool mazegakiSelectFirstCand = false;   // 交ぜ書き変換で先頭の候補を自動選択
    bool mazeBlockerTail = true;            // 交ぜ書き変換で、変換後のブロッカーの位置
    bool mazeRemoveHeadSpace = true;        // 交ぜ書き変換で、変換開始位置の空白を削除
    bool mazeRightShiftYomiPos = true;      // 交ぜ書き変換で、読みの開始位置を右移動する
    size_t mazeYomiMaxLen = 10;             // 交ぜ書き変換時の最長入力読み長
    size_t mazeGobiMaxLen = 3;              // 語尾あり交ぜ書きの最長語尾長
    size_t mazeNoIfxGobiMaxLen = 4;         // 交ぜ書きでの無活用語の語尾の最大長
    size_t mazeGobiLikeTailLen = 2;         // 交ぜ書き変換で、語尾に含めてしまう末尾の長さ

    bool convertShiftedHiraganaToKatakana = false; // Shift入力された平仮名をカタカナに変換する
    bool convertJaPeriod = false;           // 「。」と「．」を相互変換する
    bool convertJaComma = false;            // 「、」と「，」を相互変換する

    bool removeOneStrokeByBackspace = false; // BS で直前打鍵のみを取り消す

    bool autoBushuComp = false;             // 自動部首合成を行う

    // for Debug
    bool debughState = false;               // State モジュールで DebugH を有効にする
    bool debughMazegaki = false;            // mazegaki モジュールで DebugH を有効にする
    bool debughMazegakiDic = false;         // mazegakiDic モジュールで DebugH を有効にする
    bool debughHistory = false;             // history モジュールで DebugH を有効にする
    bool debughStrokeTable = false;         // strokeTable モジュールで DebugH を有効にする
    bool debughBushu = false;               // bushuComp/bushuAssoc モジュールで DebugH を有効にする
    bool debughString = false;              // String モジュールで DebugH を有効にする
    bool debughZenkaku = false;             // Zenkaku モジュールで DebugH を有効にする
    bool debughKatakana = false;            // Katakana モジュールで DebugH を有効にする
    bool debughMyPrevChar = false;          // MyChar/PrevChar モジュールで DebugH を有効にする
    bool bushuDicLogEnabled = false;        // bushuDic で InfoH を有効にする

public:
    void SetValues(const std::map<tstring, tstring>&);

    static std::unique_ptr<Settings> Singleton;
};

#define SETTINGS  (Settings::Singleton)
