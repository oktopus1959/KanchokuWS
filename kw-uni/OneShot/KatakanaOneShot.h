#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// KatakanaOneShotNode - ノードのテンプレート
class KatakanaOneShotNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    KatakanaOneShotNode();

    ~KatakanaOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("ナ")); }

    String getNodeName() const { return _T("KatakanaOneShotNode"); }
};

// -------------------------------------------------------------------
// KatakanaOneShotNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class KatakanaOneShotNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

