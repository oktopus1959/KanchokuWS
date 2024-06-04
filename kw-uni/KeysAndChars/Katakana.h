#pragma once

#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// KatakanaNode
class KatakanaNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    KatakanaNode();

    ~KatakanaNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("カ")); }

    String getNodeName() const { return _T("KatakanaNode"); }

    //static void CreateSingleton();

    static KatakanaNode* Singleton();

private:
    static std::unique_ptr<KatakanaNode> _singleton;
};
#define KATAKANA_NODE (KatakanaNode::Singleton())

// -------------------------------------------------------------------
// KatakanaNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class KatakanaNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

