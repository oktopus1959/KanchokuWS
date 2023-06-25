#pragma once

// 機能ノードの基底
#include "Node.h"

class FunctionNode : public Node {
public:
    NodeType getNodeType() const { return NodeType::FunctionT; }
};
