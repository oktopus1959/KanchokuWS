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

    // 変換結果を元に戻すための出力文字列の長さ
    static size_t prevOutputLen;

    // 前回変換時のホットキーカウント
    static size_t deckeyCount;

    // 先頭候補の自動選択を一時的に中止する
    static bool selectFirstCandDisabled;

    // シフトされた読み長
    static size_t shiftedYomiLen;

    // ブロッカーがシフトされた
    static bool blockerShifted;

 public:
     MazegakiNode();

     ~MazegakiNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("○")); }

    void ClearBlockerShiftFlag() {
        blockerShifted = false;
    }

    // ブロッカーを左シフトする
    bool LeftShiftBlocker() {
        blockerShifted = IsJustAfterPrevXfer();
        if (blockerShifted) OUTPUT_STACK->leftShiftMazeBlocker();
        return blockerShifted;
    }

    // ブロッカーを右シフトする
    bool RightShiftBlocker() {
        blockerShifted = IsJustAfterPrevXfer();
        if (blockerShifted) OUTPUT_STACK->rightShiftMazeBlocker();
        return blockerShifted;
    }

    // ブロッカーがシフトされたか
    bool IsBlockerShifted() {
        bool shifted = IsJustAfterPrevXfer() && blockerShifted;
        if (shifted) {
            // 続けてシフトできるようにするため、次も交ぜ書き変換直後という扱いにする
            deckeyCount = STATE_COMMON->GetTotalDecKeyCount();
        }
        return shifted;
    }

    // 今回の結果を元に戻すための情報を保存 (yomi は、再変換をする際の元の読みになる)
    void SetYomiInfo(const MString& yomi, size_t outputLen) {
        prevYomi = yomi;
        prevOutputLen = outputLen;
        shiftedYomiLen = yomi.size();
        deckeyCount = STATE_COMMON->GetTotalDecKeyCount();
        selectFirstCandDisabled = false;
    }

    // 前回の出力長を返す
    size_t GetPrevOutputLen() {
        return (STATE_COMMON->GetTotalDecKeyCount() <= deckeyCount + 4) ? prevOutputLen : 0;
    }

    // n打鍵によるMaze呼び出し用に情報をセットする(4ストロークまでOK)⇒前回の出力長を返す
    size_t GetPrevYomiInfo(MString& yomi) {
        if (STATE_COMMON->GetTotalDecKeyCount() <= deckeyCount + 4) {
            selectFirstCandDisabled = true;
            yomi = prevYomi;
            return prevOutputLen;
        }
        selectFirstCandDisabled = false;
        return 0;
    }

    // Esc用
    size_t GetPrevYomiInfoIfJustAfterMaze(MString& yomi) {
        if (IsJustAfterPrevXfer()) {
            selectFirstCandDisabled = true;
            yomi = prevYomi;
            size_t resultLen = prevOutputLen;
            prevOutputLen = 0;
            return resultLen;
        }
        selectFirstCandDisabled = false;
        return 0;
    }

    // 先頭候補の自動選択が一時的に中止されているか
    bool IsSelectFirstCandDisabled() {
        return selectFirstCandDisabled;
    }

    // シフトされた読み長の取得
    size_t GetShiftedYomiLen() {
        if (shiftedYomiLen < prevYomi.size()) selectFirstCandDisabled = false;
        return shiftedYomiLen;
    }

    // 読み長を長くする(読み開始位置を左にシフトする) (前回の変換の直後でなければ false を返す)
    bool LeftShiftYomiStartPos() {
        if (IsJustAfterPrevXfer()) {
            ++shiftedYomiLen;
            return true;
        }
        shiftedYomiLen = 1000;
        return false;
    }

    // 読み開始位置を右にシフトする (前回の変換の直後でなければ false を返す)
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
        return STATE_COMMON->GetTotalDecKeyCount() <= deckeyCount + 1;
    }

    // 交ぜ書き実行時の直後状態にセット
    void SetJustAfterPrevXfer() {
        deckeyCount = STATE_COMMON->GetTotalDecKeyCount();
    }

public:
    // 全 MazegakiState から参照される共有ノード
    static std::unique_ptr<MazegakiNode> CommonNode;
};
#define MAZEGAKI_NODE (MazegakiNode::CommonNode)

#define HANDLE_ESC_FOR_MAZEGAKI() \
    LOG_DEBUGH(_T("HANDLE_ESC_FOR_MAZEGAKI: %s"), NAME_PTR); \
    if (MAZEGAKI_NODE) { \
        MString prevYomi; \
        size_t prevOutLen = MAZEGAKI_NODE->GetPrevYomiInfoIfJustAfterMaze(prevYomi); \
        LOG_DEBUGH(_T("MAZEGAKI ESC: prevYomi=%s, prevOutLen=%d"), MAKE_WPTR(prevYomi), prevOutLen); \
        if (prevOutLen > 0) { \
            MAZEGAKI_NODE->SetJustAfterPrevXfer(); /* 続けて交ぜ書き関連の操作を受け付けるようにする */ \
            STATE_COMMON->SetOutString(prevYomi, prevOutLen); \
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

