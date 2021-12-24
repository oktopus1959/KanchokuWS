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

    // 前回変換時のデコーダキーカウント
    size_t prevDeckeyCount = 0;

    // 先頭候補の自動選択を一時的に中止する
    bool selectFirstCandDisabled = false;

    // シフトされた読み長
    size_t shiftedTailYomiLen = 0;

    // 前回のシフトされた読み長
    size_t prevShiftedTailYomiLen = 0;

    // 交ぜ書き中
    bool inMazegakiMode = false;

    // ブロッカーがシフトされた
    bool blockerShifted = false;

    // 再変換モード
    bool reXferMode = false;

public:
    // 初期化
    void Initialize(bool bMazegakiMode = false);

    // 交ぜ書き変換終了の直後か
    bool IsJustAfterPrevXfer();

    // 交ぜ書き変換実行直後状態にセット
    void SetJustAfterPrevXfer();

    // 再変換モードか
    bool IsReXferMode();

    // 再変換モードにセット
    void SetReXferMode();

    // ブロッカーフラグをクリアする
    void ClearBlockerShiftFlag();

    // ブロッカーを左シフトする
    bool LeftShiftBlocker();

    // ブロッカーを右シフトする
    bool RightShiftBlocker();

    // ブロッカーがシフトされたか
    bool IsBlockerShifted();

    // 今回の結果を元に戻すための情報を保存 (yomi は、再変換をする際の元の読みになる)
    void SetYomiInfo(const MString& yomi, size_t leadLen, size_t outputLen);

    // n打鍵によるMaze呼び出し用に情報をセットする(4ストロークまでOK)⇒前回の出力長を返す
    size_t GetPrevYomiInfo(MString& yomi);

    // 前回の出力長を返す
    size_t GetPrevOutputLen();

    // 前回のリード部長を返す
    size_t GetPrevLeadLen();

    // 先頭候補の自動選択を一時的に中止する
    void DisableSelectFirstCand();

    // 先頭候補の自動選択が一時的に中止されているか
    bool IsSelectFirstCandDisabled();

    // シフトされた読み長の取得
    size_t GetShiftedTailYomiLen();

    // 読み長を長くする(読み開始位置を左にシフトする) (前回の変換の直後でなければ false を返す)
    bool LeftShiftYomiStartPos();

    // 読み開始位置を右にシフトする (前回の変換の直後でなければ false を返す)
    bool RightShiftYomiStartPos();

    // ブロッカーや読み開始位置を左右にシフト -- 左右シフトを実行したら callback を呼んで true を返す。そうでなければ false を返す
    bool LeftRightShiftBlockerOrStartPos(int deckey, std::function<void ()> callback);

    // Esc用 -- 直前の交ぜ書き状態に戻す
    size_t GetPrevYomiInfoIfJustAfterMaze(MString& yomi);

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

// 交ぜ書き変換結果を元に戻す
#define HANDLE_ESC_FOR_MAZEGAKI() \
    LOG_DEBUGH(_T("HANDLE_ESC_FOR_MAZEGAKI: %s"), NAME_PTR); \
    if (MAZEGAKI_INFO) { \
        MString prevYomi; \
        size_t prevOutLen = MAZEGAKI_INFO->GetPrevYomiInfoIfJustAfterMaze(prevYomi); \
        LOG_DEBUGH(_T("MAZEGAKI ESC: prevYomi=%s, prevOutLen=%d"), MAKE_WPTR(prevYomi), prevOutLen); \
        if (prevOutLen > 0) { \
            MAZEGAKI_INFO->SetReXferMode(); /* 再変換モードにセット */ \
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

