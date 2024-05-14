#pragma once

#include "Node.h"

// 開始ノード
class StartNode : public Node {
    DECLARE_CLASS_LOGGER;

public:
    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 表示用文字列を返す
    MString getString() const { return to_mstr(_T("始")); }

    // ノード型を返す
    NodeType getNodeType() const { return NodeType::Start; }

    String getNodeName() const { return _T("StartNode"); }
};

