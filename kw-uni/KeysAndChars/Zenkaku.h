#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// ZenkakuNode - ノードのテンプレート
class ZenkakuNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    ZenkakuNode();

    ~ZenkakuNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("全")); }

};

// -------------------------------------------------------------------
// ZenkakuOneNode - ノードのテンプレート
class ZenkakuOneNode : public ZenkakuNode {
    DECLARE_CLASS_LOGGER;
public:
    ZenkakuOneNode();

    ~ZenkakuOneNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("１")); }
};

// -------------------------------------------------------------------
// ZenkakuNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class ZenkakuNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

// -------------------------------------------------------------------
// ZenkakuOneNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class ZenkakuOneNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

