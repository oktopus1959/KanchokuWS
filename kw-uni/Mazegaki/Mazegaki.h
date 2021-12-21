//#include "../pch.h"
#include "Logger.h"

#include "FunctionNode.h"

#include "StateCommonInfo.h"

// -------------------------------------------------------------------
// MazegakiNode - 交ぜ書き機能ノード
class MazegakiNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
     MazegakiNode();

     ~MazegakiNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("○")); }
};

// -------------------------------------------------------------------
// MazegakiCommonInfo - 交ぜ書き共有情報
class MazegakiCommonInfo {
    DECLARE_CLASS_LOGGER;
private:
    // 変換結果を元に戻すための変換前の読み
    MString prevYomi;

    // 変換結果を元に戻すためのリード文字列の長さ
    // 「ひど|い目にあった」⇒「ひどい目に|遭った」のときの「い目に」の長さ)
    size_t prevLeadLen = 0;

    // 変換結果を元に戻すための出力文字列の長さ
    size_t prevOutputLen;

    // 前回変換時のホットキーカウント
    size_t deckeyCount = 0;

    // 先頭候補の自動選択を一時的に中止する
    bool selectFirstCandDisabled = false;

    // シフトされた読み長
    size_t shiftedYomiLen = 0;

    // ブロッカーがシフトされた
    bool blockerShifted = false;

 public:
    void ClearBlockerShiftFlag() {
        blockerShifted = false;
    }

    // ブロッカーを左シフトする
    bool LeftShiftBlocker();

    // ブロッカーを右シフトする
    bool RightShiftBlocker();

    // ブロッカーがシフトされたか
    bool IsBlockerShifted();

    // 今回の結果を元に戻すための情報を保存 (yomi は、再変換をする際の元の読みになる)
    void SetYomiInfo(const MString& yomi, size_t leadLen, size_t outputLen);

    // 前回の出力長を返す
    size_t GetPrevOutputLen();

    // 前回のリード部長を返す
    size_t GetPrevLeadLen();

    // n打鍵によるMaze呼び出し用に情報をセットする(4ストロークまでOK)⇒前回の出力長を返す
    size_t GetPrevYomiInfo(MString& yomi);

    // Esc用
    size_t GetPrevYomiInfoIfJustAfterMaze(MString& yomi);

    // 先頭候補の自動選択が一時的に中止されているか
    bool IsSelectFirstCandDisabled() {
        return selectFirstCandDisabled;
    }

    // シフトされた読み長の取得
    size_t GetShiftedYomiLen() {
        //if (shiftedYomiLen < prevYomi.size()) selectFirstCandDisabled = false;
        return shiftedYomiLen;
    }

    // 読み長を長くする(読み開始位置を左にシフトする) (前回の変換の直後でなければ false を返す)
    bool LeftShiftYomiStartPos();

    // 読み開始位置を右にシフトする (前回の変換の直後でなければ false を返す)
    bool RightShiftYomiStartPos();

    // 前回の実行時の直後か
    bool IsJustAfterPrevXfer();

    // 交ぜ書き実行時の直後状態にセット
    void SetJustAfterPrevXfer();

public:
    // 共有ノード
    std::unique_ptr<MazegakiNode> CommonNode;

    // 共有情報のSingletonインスタンス
    static std::unique_ptr<MazegakiCommonInfo> CommonInfo;

    // MazegakiCommonInfo - 交ぜ書き共有情報の作成
    static void CreateCommonInfo();
};
#define MAZEGAKI_INFO (MazegakiCommonInfo::CommonInfo)
#define MAZEGAKI_NODE_PTR (MAZEGAKI_INFO->CommonNode.get())

#define HANDLE_ESC_FOR_MAZEGAKI() \
    LOG_DEBUGH(_T("HANDLE_ESC_FOR_MAZEGAKI: %s"), NAME_PTR); \
    if (MAZEGAKI_INFO) { \
        MString prevYomi; \
        size_t prevOutLen = MAZEGAKI_INFO->GetPrevYomiInfoIfJustAfterMaze(prevYomi); \
        LOG_DEBUGH(_T("MAZEGAKI ESC: prevYomi=%s, prevOutLen=%d"), MAKE_WPTR(prevYomi), prevOutLen); \
        if (prevOutLen > 0) { \
            MAZEGAKI_INFO->SetJustAfterPrevXfer(); /* 続けて交ぜ書き関連の操作を受け付けるようにする */ \
            STATE_COMMON->SetOutString(prevYomi, prevOutLen); \
            return; \
        } \
    }

// -------------------------------------------------------------------
// MazegakiNodeBuilder - 交ぜ書き機能ノードビルダ
#include "FunctionNodeBuilder.h"

class MazegakiNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

