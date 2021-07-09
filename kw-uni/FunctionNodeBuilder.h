#pragma once
// 機能ノード作成のためのフレームワーク

#include "Node.h"

class FunctionNodeBuilder {
public:
    virtual Node* CreateNode() = 0;
};
