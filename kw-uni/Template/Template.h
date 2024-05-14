#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// TemplateNode - ノードのテンプレート
class TemplateNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    TemplateNode();

    ~TemplateNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("Ｔ")); }

    String getNodeName() const { return _T("TemplateNode"); }

};

// -------------------------------------------------------------------
// TemplateNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class TemplateNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

