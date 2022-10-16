#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// EisuNode
class EisuNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    EisuNode();

    ~EisuNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("Ａ")); }

    static void CreateSingleton();

    static std::unique_ptr<EisuNode> Singleton;
};
#define EISU_NODE (EisuNode::Singleton)

// -------------------------------------------------------------------
// EisuNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class EisuNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

