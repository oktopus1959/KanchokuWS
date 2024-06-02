#pragma once

#include "string_utils.h"

#include "StateCommonInfo.h"    // for VkbLayout
#include "HistCandidates.h"     // for HistResult
#include "Node.h"
#include "MStringResult.h"

// 縦列鍵盤または横列鍵盤の数
#define LONG_KEY_NUM 10

// 縦列鍵盤での表示文字列の長さ
#define CAND_DISP_LONG_VKEY_LEN  20

// -------------------------------------------------------------------
// 履歴入力機能状態基底クラス
class HistoryStateBase {
public:
    virtual ~HistoryStateBase() { };

public:
    // 履歴検索文字列の遡及ブロッカーをセット
    virtual void setBlocker() = 0;

    // 選択された履歴候補を出力(これが呼ばれた時点で、すでにキーの先頭まで巻き戻すように plannedNumBS が設定されていること)
    virtual void setOutString(const HistResult& result, MStringResult& resultStr) = 0;

    // 前回の履歴検索の出力と現在の出力文字列(改行以降)の末尾を比較し、同じであれば前回の履歴検索のキーを取得する
    virtual MString getLastHistKeyAndRewindOutput(MStringResult& resultStr) = 0;

    // 前回の履歴選択の出力と現在の出力文字列(改行以降)の末尾が同一であるか
    virtual bool isLastHistOutSameAsCurrentOut() = 0;

    // 履歴入力候補を鍵盤にセットする
    virtual void setCandidatesVKB(VkbLayout layout, int candLen, const MString& key, bool bShrinkWord = false) = 0;

    // 履歴入力候補を鍵盤にセットする
    virtual void setCandidatesVKB(VkbLayout layout, const std::vector<MString>& cands, const MString& key, bool bShrinkWord = false) = 0;

    // 中央鍵盤の色付け、矢印キー有効、縦列鍵盤の色付けあり
    virtual void setHistSelectColorAndBackColor() = 0;

    // 中央鍵盤の文字出力と色付け、矢印キー有効、縦列鍵盤の色付けなし
    virtual void setCenterStringAndBackColor(StringRef ws) = 0;

    // 横列鍵盤上での候補位置
    virtual void setCandDispHorizontalPos(size_t pos) = 0;

    // モード標識文字を返す
    virtual mchar_t GetModeMarker() = 0;

    // 最終的な出力履歴が整ったところで呼び出される処理
    virtual void DoLastHistoryProc() = 0;

    // Strokeキー を処理する
    virtual bool handleStrokeKeys(int deckey, MStringResult& resultStr) = 0;

    // 機能キーだったときの一括処理(false を返すと、この後、個々の機能キーのハンドラが呼ばれる)
    virtual bool handleFunctionKeys(int deckey) = 0;

    virtual void handleDownArrow() = 0;
    virtual void handleUpArrow() = 0;
    virtual void handleLeftArrow() = 0;
    virtual void handleRightArrow() = 0;

public:
    static HistoryStateBase* createInstance(const Node* pN);
};

