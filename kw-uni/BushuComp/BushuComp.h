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
    //time_t PrevCompSec = 0;     // 直前の部首合成実行の時刻
    size_t PrevTotalCount = 0;  // 直前の部首合成実行時のトータルカウント

    // 部首合成の実行
    MString ReduceByBushu(mchar_t m1, mchar_t m2, mchar_t prev = 0);

    // 自動部首合成の実行
    void ReduceByAutoBushu(const MString& ms);

public:
    // 後置部首合成機能ノードのSingleton
    static BushuCompNode* Singleton;
};
#define BUSHU_COMP_NODE (BushuCompNode::Singleton)

// -------------------------------------------------------------------
// BushuCompNodeBuilder - 後置部首合成機能ノードビルダー
#include "FunctionNodeBuilder.h"

class BushuCompNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

