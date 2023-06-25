#pragma once
// 機能ノード作成のためのフレームワーク

#include "Logger.h"
#include "Node.h"
#include "FunctionNodeBuilder.h"

class FunctionNodeManager {
    DECLARE_CLASS_LOGGER;

    static std::map<String, FunctionNodeBuilder*> funcNodeMap;

    static std::map<String, FunctionNodeBuilder*> funcNameMap;

    static void addFunctionNodeBuilder(StringRef, StringRef, FunctionNodeBuilder*);

public:
    static void AddFunctionNodeBuilders();

    static Node* CreateFunctionNode(StringRef funcSpec);

    static Node* CreateFunctionNodeByName(StringRef funcName);

    static inline FunctionNodeBuilder* GetFuncNodeBuilder(StringRef name) {
        auto iter = funcNameMap.find(name);
        return iter == funcNameMap.end() ? nullptr : iter->second;
    }
};
