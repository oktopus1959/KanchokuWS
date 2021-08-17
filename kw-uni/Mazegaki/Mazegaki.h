//#include "../pch.h"
#include "Logger.h"

#include "FunctionNode.h"

#include "StateCommonInfo.h"

// -------------------------------------------------------------------
// MazegakiNode - 交ぜ書き機能ノード
class MazegakiNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
private:
    // 変換結果を元に戻すための変換前の読み
    static MString prevYomi;

    // 変換結果を元に戻すための変換形の長さ
    static size_t prevXferLen;

    // 前回変換時のホットキーカウント
    static size_t deckeyCount;

    // 先頭候補の自動選択を一時的に中止する
    static bool selectFirstCandDisabled;

    // シフトされた読み長
    static size_t shiftedYomiLen;

 public:
     MazegakiNode();

     ~MazegakiNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("○")); }

    static void SetYomiInfo(const MString& yomi, size_t xferLen) {
        prevYomi = yomi;
        prevXferLen = xferLen;
        shiftedYomiLen = yomi.size();
        deckeyCount = STATE_COMMON->GetTotalDecKeyCount();
        selectFirstCandDisabled = false;
    }

    // n打鍵によるMaze呼び出し用(4ストロークまでOK)
    size_t GetPrevYomiInfo(MString& yomi) {
        if (STATE_COMMON->GetTotalDecKeyCount() <= deckeyCount + 4) {
            selectFirstCandDisabled = true;
            yomi = prevYomi;
            return prevXferLen;
        }
        selectFirstCandDisabled = false;
        return 0;
    }

    // Esc用
    size_t GetPrevYomiInfoIfJustAfterMaze(MString& yomi) {
        if (IsJustAfterPrevXfer()) {
            selectFirstCandDisabled = true;
            yomi = prevYomi;
            return prevXferLen;
        }
        selectFirstCandDisabled = false;
        return 0;
    }

    bool IsSelectFirstCandDisabled() {
        return selectFirstCandDisabled;
    }

    // シフトされた読み長の取得
    size_t GetShiftedYomiLen() {
        if (shiftedYomiLen < prevYomi.size()) selectFirstCandDisabled = false;
        return shiftedYomiLen;
    }

    // 読み長を長くする(読み開始位置を右にシフトする) (前回の変換の直後でなければ false を返す)
    bool LeftShiftYomiStartPos() {
        if (IsJustAfterPrevXfer()) {
            ++shiftedYomiLen;
            return true;
        }
        shiftedYomiLen = 1000;
        return false;
    }

    // 読み長を短くする(読み開始位置を左にシフトする) (前回の変換の直後でなければ false を返す)
    bool RightShiftYomiStartPos() {
        if (IsJustAfterPrevXfer()) {
            if (shiftedYomiLen > 1) --shiftedYomiLen;
            return true;
        }
        shiftedYomiLen = 1000;
        return false;
    }

    // 前回の実行時の直後か
    bool IsJustAfterPrevXfer() {
        return STATE_COMMON->GetTotalDecKeyCount() == deckeyCount + 1;
    }

public:
    static MazegakiNode* Singleton;
};
#define MAZEGAKI_NODE (MazegakiNode::Singleton)

#define HANDLE_ESC_FOR_MAZEGAKI() \
    LOG_DEBUGH(_T("HANDLE_ESC_FOR_MAZEGAKI: %s"), NAME_PTR); \
    if (MAZEGAKI_NODE) { \
        MString prevYomi; \
        size_t prevXferLen = MAZEGAKI_NODE->GetPrevYomiInfoIfJustAfterMaze(prevYomi); \
        LOG_DEBUGH(_T("MAZEGAKI ESC: prevYomi=%s, prevXferLen=%d"), MAKE_WPTR(prevYomi), prevXferLen); \
        if (prevXferLen > 0) { \
            STATE_COMMON->SetOutString(prevYomi, prevXferLen); \
            return; \
        } \
    } \
    State::handleEsc();

// -------------------------------------------------------------------
// MazegakiNodeBuilder - 交ぜ書き機能ノードビルダ
#include "FunctionNodeBuilder.h"

class MazegakiNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

