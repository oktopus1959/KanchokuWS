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

    static void CreateSingleton();

    static std::unique_ptr<KatakanaNode> Singleton;
};
#define KATAKANA_NODE (KatakanaNode::Singleton)

// -------------------------------------------------------------------
// KatakanaNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class KatakanaNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

