//#include "pch.h"

#include "Node.h"

// -------------------------------------------------------------------
// EscapeNode - 次打鍵を直接に出力する
class EscapeNode : public Node {
    DECLARE_CLASS_LOGGER;
public:
    EscapeNode();

    ~EscapeNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    MString getString() const { return to_mstr(_T("＼")); }

    NodeType getNodeType() const { return NodeType::Function; }
};

#include "FunctionNodeBuilder.h"

class EscapeNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

