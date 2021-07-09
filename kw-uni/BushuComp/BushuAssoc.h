#pragma once
// -------------------------------------------------------------------
#include "string_type.h"
#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// BushuAssocNode - 部首連想入力ノード
class BushuAssocNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
 public:
     BushuAssocNode();

     ~BushuAssocNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("▼")); }

};

// -------------------------------------------------------------------
// BushuAssocNodeBuilder - 部首連想入力機能ノードビルダー
#include "FunctionNodeBuilder.h"

class BushuAssocNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

// -------------------------------------------------------------------
// BushuAssocExNode - 拡張部首連想入力ノード
class BushuAssocExNode : public BushuAssocNode {
    DECLARE_CLASS_LOGGER;
 public:
     BushuAssocExNode();

     ~BushuAssocExNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("▽")); }

    mchar_t PrevKey;
    mchar_t PrevAssoc;
    //time_t PrevAssocSec = 0;     // 直前の部首連想実行の時刻
    size_t PrevTotalCount = 0;  // 直前の部首連想実行時のトータルカウント
    size_t Count = 0;           // 1回目または2回目の呼び出しであることをチェックするためのカウント

public:
    // Singleton (生存管理は呼び出し側の StrokeNode のほうでやる)
    static BushuAssocExNode* Singleton;
};
#define BUSHU_ASSOC_EX_NODE (BushuAssocExNode::Singleton)

// -------------------------------------------------------------------
// BushuAssocNodeBuilder - 拡張部首連想入力機能ノードビルダー
#include "FunctionNodeBuilder.h"

class BushuAssocExNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

