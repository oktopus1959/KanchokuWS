// 機能ノード作成のためのフレームワーク
#include "Logger.h"

#include "FunctionNodeManager.h"

DEFINE_CLASS_LOGGER(FunctionNodeManager);

// これの要素は void FunctionNodeManager::AddFunctionNodeBuilders() で追加される
// AddFunctionNodeBuilders() は FunctionNodeBuilderList.h で定義されている。
// 新しい機能を追加する場合は、上記ヘッダーを修正すること。
std::map<tstring, FunctionNodeBuilder*> FunctionNodeManager::funcNodeMap;

std::map<tstring, FunctionNodeBuilder*> FunctionNodeManager::funcNameMap;

void FunctionNodeManager::addFunctionNodeBuilder(const tstring& spec, const tstring& name, FunctionNodeBuilder* builder) {
    funcNodeMap[spec] = builder;
    funcNameMap[name] = builder;
}

Node* FunctionNodeManager::CreateFunctionNode(const tstring& funcSpec) {
    auto builder = funcNodeMap.find(funcSpec);
    if (builder != funcNodeMap.end()) {
        return builder->second->CreateNode();
    } else {
        return 0;
    }
}

Node* FunctionNodeManager::CreateFunctionNodeByName(const tstring& funcName) {
    auto builder = funcNameMap.find(funcName);
    if (builder != funcNameMap.end()) {
        return builder->second->CreateNode();
    } else {
        return 0;
    }
}

#include "FunctionNodeBuilderList.h"
