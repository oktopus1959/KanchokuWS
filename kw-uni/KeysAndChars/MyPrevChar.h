#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// MyCharNode - 自キー文字を返す
class MyCharNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    MyCharNode();

    ~MyCharNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("・")); }

};

// -------------------------------------------------------------------
// PrevCharNode - 直前キー文字を返す
class PrevCharNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    PrevCharNode();

    ~PrevCharNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("・")); }
};

// -------------------------------------------------------------------
// MyCharNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class MyCharNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

// -------------------------------------------------------------------
// PrevCharNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class PrevCharNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

