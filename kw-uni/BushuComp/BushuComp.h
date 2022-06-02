//#include "../pch.h"
#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// BushuCompNode - 後置部首合成機能ノード
class BushuCompNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
 public:
     BushuCompNode();

     ~BushuCompNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("●")); }

public:
    mchar_t PrevBushu1 = 0;     // 直前の部首合成で使われた第1部首
    mchar_t PrevBushu2 = 0;     // 直前の部首合成で使われた第2部首
    mchar_t PrevComp = 0;       // 直前の部首合成の結果
    bool IsPrevAuto = false;    // 直前に実行されたのは自動部首合成か
    bool IsPrevAutoCancel = false;    // 直前に実行されたのは自動部首合成のキャンセルか
    //time_t PrevCompSec = 0;     // 直前の部首合成実行の時刻
    size_t PrevTotalCount = 0;  // 直前の部首合成実行時のトータルカウント

    // 部首合成の実行
    MString ReduceByBushu(mchar_t m1, mchar_t m2, mchar_t prev = 0);

    // 自動部首合成の実行
    bool ReduceByAutoBushu(const MString& ms);

public:
    // 後置部首合成機能ノードのSingleton
    static std::unique_ptr<BushuCompNode> Singleton;

    static void CreateSingleton();
};
#define BUSHU_COMP_NODE (BushuCompNode::Singleton)

// 直前の自動部首合成文字と比較して、やり直しをする
#define HANDLE_ESC_FOR_AUTO_COMP() \
    if (BUSHU_DIC && BUSHU_COMP_NODE && (BUSHU_COMP_NODE->IsPrevAuto || BUSHU_COMP_NODE->IsPrevAutoCancel)) { \
        LOG_DEBUGH(_T("HANDLE_ESC_FOR_AUTO_COMP: %s"), NAME_PTR); \
        size_t totalCnt = STATE_COMMON->GetTotalDecKeyCount(); \
        if (totalCnt <= BUSHU_COMP_NODE->PrevTotalCount + 2) { \
            mchar_t outChar = OUTPUT_STACK->isLastOutputStackCharBlocker() ? 0 : OUTPUT_STACK->LastOutStackChar(); \
            mchar_t m1 = BUSHU_COMP_NODE->PrevBushu1; \
            mchar_t m2 = BUSHU_COMP_NODE->PrevBushu2; \
            if (BUSHU_COMP_NODE->IsPrevAutoCancel && outChar == m2 && OUTPUT_STACK->LastOutStackChar(1) == m1) { \
                /* 直前が自動部首合成のキャンセルで、現在の出力文字がキャンセルされた自動部首合成の元文字だったら、その自動部首合成の定義を無効にする */ \
                if (m1 != 0 && m2 != 0) BUSHU_DIC->AddAutoBushuEntry(m1, m2, '-'); \
                BUSHU_COMP_NODE->PrevTotalCount = totalCnt; \
                return; \
            } else if (BUSHU_COMP_NODE->PrevComp == outChar) { \
                if (BUSHU_COMP_NODE->IsPrevAuto) { \
                    /* 直前の処理は自動部首合成だったので、合成元の2文字に戻す \
                       出力文字列と削除文字のセット */ \
                    STATE_COMMON->SetOutString(make_mstring(m1, m2), 1); \
                    BUSHU_COMP_NODE->PrevTotalCount = totalCnt; \
                    BUSHU_COMP_NODE->IsPrevAuto = false; \
                    BUSHU_COMP_NODE->IsPrevAutoCancel = true; \
                    return; \
                } \
            } \
        } \
    }

// -------------------------------------------------------------------
// BushuCompNodeBuilder - 後置部首合成機能ノードビルダー
#include "FunctionNodeBuilder.h"

class BushuCompNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

