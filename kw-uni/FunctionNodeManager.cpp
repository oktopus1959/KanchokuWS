// 機能ノード作成のためのフレームワーク
#include "Logger.h"

#include "FunctionNodeManager.h"

DEFINE_CLASS_LOGGER(FunctionNodeManager);

// これの要素は void FunctionNodeManager::AddFunctionNodeBuilders() で追加される
// AddFunctionNodeBuilders() は FunctionNodeBuilderList.h で定義されている。
// 新しい機能を追加する場合は、上記ヘッダーを修正すること。
std::map<String, FunctionNodeBuilder*> FunctionNodeManager::funcNodeMap;

std::map<String, FunctionNodeBuilder*> FunctionNodeManager::funcNameMap;

void FunctionNodeManager::addFunctionNodeBuilder(StringRef spec, StringRef name, FunctionNodeBuilder* builder) {
    funcNodeMap[spec] = builder;
    funcNameMap[name] = builder;
}

Node* FunctionNodeManager::CreateFunctionNode(StringRef funcSpec) {
    auto builder = funcNodeMap.find(funcSpec);
    if (builder != funcNodeMap.end()) {
        return builder->second->CreateNode();
    } else {
        return 0;
    }
}

Node* FunctionNodeManager::CreateFunctionNodeByName(StringRef funcName) {
    auto builder = funcNameMap.find(funcName);
    if (builder != funcNameMap.end()) {
        return builder->second->CreateNode();
    } else {
        return 0;
    }
}

#include "FunctionNodeBuilderList.h"
