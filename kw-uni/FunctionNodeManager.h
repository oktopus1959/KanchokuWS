#pragma once
// 機能ノード作成のためのフレームワーク

#include "Logger.h"
#include "Node.h"
#include "FunctionNodeBuilder.h"

class FunctionNodeManager {
    DECLARE_CLASS_LOGGER;

    static std::map<tstring, FunctionNodeBuilder*> funcNodeMap;

    static std::map<tstring, FunctionNodeBuilder*> funcNameMap;

    static void addFunctionNodeBuilder(const tstring&, const tstring&, FunctionNodeBuilder*);

public:
    static void AddFunctionNodeBuilders();

    static Node* CreateFunctionNode(const tstring& funcSpec);

    static Node* CreateFunctionNodeByName(const tstring& funcName);

    static inline FunctionNodeBuilder* GetFuncNodeBuilder(const tstring& name) {
        auto iter = funcNameMap.find(name);
        return iter == funcNameMap.end() ? nullptr : iter->second;
    }
};
