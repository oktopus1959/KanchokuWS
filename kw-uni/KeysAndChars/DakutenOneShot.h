#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// DakutenOneShotNode - ノードのテンプレート
class DakutenOneShotNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;

    MString markStr;

    wstring postfix;

public:
    DakutenOneShotNode(wstring markStr);

    ~DakutenOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return markStr; }

    wstring getPostfix() const { return postfix; }
};

// -------------------------------------------------------------------
// DakutenOneShotNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class DakutenOneShotNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

class HanDakutenOneShotNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

