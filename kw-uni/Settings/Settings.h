#pragma once

#include "string_type.h"

struct Settings {
    bool firstUse;              // 最初の起動か

    bool isJPmode = true;       // キーボードがJPモードか

    String rootDir;            // ルートフォルダ
    String tableFile;          // ストロークテーブル
    String tableFile2;         // ストロークテーブル2
    String tableFile3;         // ストロークテーブル3
    String charsDefFile;       // Deckey から文字への変換
    String easyCharsFile;      // 簡易打鍵文字ファイル
    String kanjiYomiFile;      // 漢字読みファイル
    String bushuFile;          // 部首合成辞書
    String autoBushuFile;      // 自動部首合成辞書
    String bushuAssocFile;     // 部首連想辞書
    String mazegakiFile;       // 交ぜ書き辞書
    String historyFile;        // 履歴
    String historyUsedFile;    // 使用順
    String historyExcludeFile; // 履歴排除
    String historyNgramFile;   // Nグラム履歴

    int backFileRotationGeneration;         // 辞書ファイル保存世代数

    size_t histMaxLength = 10;              // 自動履歴登録可能な履歴文字列の最大長

    size_t histKanjiWordMinLength = 4;      // 自動履歴登録対象となる漢字文字列の最小長
    size_t histKanjiWordMaxLength = 5;      // 自動履歴登録対象となる漢字文字列の最大長
    size_t histKatakanaWordMinLength = 4;   // 自動履歴登録対象となるカタカナ文字列の最小長
    size_t histKatakanaWordMaxLength = 8;   // 自動履歴登録対象となるカタカナ文字列の最大長
    size_t histKanjiWordMinLengthEx = 2;    // 自動履歴登録対象となる難打鍵文字を含む漢字文字列の最小長

    size_t histHiraganaKeyLength = 2;       // ひらがな履歴検索キーの長さ
    size_t histKatakanaKeyLength = 2;       // カタカナ履歴検索キーの長さ
    size_t histKanjiKeyLength = 1;          // 漢字履歴検索キーの長さ
    size_t histRomanKeyLength = 4;          // ローマ字履歴検索キーの長さ
    //size_t histMapKeyLength = 1;            // 変換履歴検索キーの長さ

    size_t histMapKeyMaxLength = 16;        // 変換履歴キーの最大長
    size_t histMapGobiMaxLength = 3;        // 変換履歴キーに付加できる語尾の最大長

    bool autoHistSearchEnabled = false;     // 自動履歴検索を行う
    //bool histSearchByCtrlSpace = false;     // Ctrl-Space で履歴検索を行う
    //bool histSearchByShiftSpace = false;    // Shift-Space で履歴検索を行う
    bool selectFirstCandByEnter = false;    // 履歴の第1候補をEnterキーで選択する
    bool newLineWhenHistEnter = false;      // 履歴候補選択時のEnterではつねに改行する
    int histDelDeckeyId = 41;               // 履歴削除のDecKeyID
    int histNumDeckeyId = 45;               // 履歴文字数指定のDecKeyID
    size_t histHorizontalCandMax = 5;       // 履歴候補の横列鍵盤表示の際の最大数

    bool histMoveShortestAt2nd = false;     // 最短長履歴文字列を2番目に表示する
    bool showHistCandsFromFirst = true;     // 最初の履歴選択から横列候補表示を行うか

    bool useArrowToSelCand = true;          // 矢印キーで履歴候補選択を行う
    bool selectHistCandByTab = true;        // Tabキーで履歴候補選択を行う
    //bool handleShiftSpaceAsNormalSpace = true;    // Shift-Space を通常の Space として扱う (ただし histSearchByShiftSpace が優先)

    bool histGatherAllCandidates = true;    // キーの開始位置をずらして全履歴候補を取得するか
    bool mazeHistRegisterAnyway = false;    // 交ぜ書き変換での選択を強制的に履歴登録する(除外登録されていたら復活する)
    size_t mazeHistRegisterMinLen = 2;      // 交ぜ書き変換での選択を履歴登録する際の最小長

    //bool mazegakiByShiftSpace = true;       // Shift-Space で交ぜ書き変換
    bool mazegakiSelectFirstCand = false;   // 交ぜ書き変換で先頭の候補を自動選択
    bool mazeBlockerTail = true;            // 交ぜ書き変換で、変換後のブロッカーの位置
    bool mazeRemoveHeadSpace = true;        // 交ぜ書き変換で、変換開始位置の空白を削除
    bool mazeRightShiftYomiPos = true;      // 交ぜ書き変換で、読みの開始位置を右移動する
    bool mazeNoIfxConnectKanji = false;     // 交ぜ書き変換で、無活用語の語尾に漢字を許可する
    bool mazeNoIfxConnectAny = true;        // 交ぜ書き変換で、無活用語の語尾は何でもOKとする
    size_t mazeYomiMaxLen = 10;             // 交ぜ書き変換時の最長入力読み長
    size_t mazeGobiMaxLen = 3;              // 語尾あり交ぜ書きの最長語尾長
    size_t mazeNoIfxGobiMaxLen = 4;         // 交ぜ書きでの無活用語の語尾の最大長
    size_t mazeGobiLikeTailLen = 2;         // 交ぜ書き変換で、語尾に含めてしまう末尾の長さ

    size_t bushuAssocSelectCount = 1;       // 部首連想直接出力の回数

    int hiraToKataShiftPlane = 0;   // Shift入力された平仮名をカタカナに変換する面
    bool hiraToKataNormalPlane = false;   // 通常面の平仮名をカタカナに変換する
    bool convertJaPeriod = false;           // 「。」と「．」を相互変換する
    bool convertJaComma = false;            // 「、」と「，」を相互変換する

    bool eisuModeEnabled = false;           // 英大文字入力による英数モード移行が有効か
    wchar_t eisuHistSearchChar = '\0';      // 英数モードから履歴検索を呼び出す文字
    size_t eisuExitCapitalCharNum = 3;      // 英数モードを自動的に抜けるまでの大文字数
    size_t eisuExitSpaceNum = 2;            // 英数モードを自動的に抜けるまでのSpace数

    bool removeOneByBS = false;             // BS で直前打鍵のみを取り消す

    bool yamanobeEnabled = false;           // YAMANOBEアルゴリズムを有効にするか
    //bool autoBushuComp = false;             // 自動部首合成を行う
    size_t autoBushuCompMinCount = 1;       // 自動首部合成を有効にする最小合成回数

    String romanBushuCompPrefix;           // ローマ字テーブル出力時の部首合成用プレフィックス
    String romanSecPlanePrefix;            // 裏面定義文字に対するローマ字出力時のプレフィックス

    bool kanaTrainingMode = false;          // かな入力練習モードか

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
    void SetValues(const std::map<String, String>&);

    static std::unique_ptr<Settings> Singleton;
};

#define SETTINGS  (Settings::Singleton)
