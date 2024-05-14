#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// HankakuKatakanaOneShotNode - ノードのテンプレート
class HankakuKatakanaOneShotNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    HankakuKatakanaOneShotNode();

    ~HankakuKatakanaOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("半")); }

    String getNodeName() const { return _T("HankakuKatakanaOneShotNode"); }
};

// -------------------------------------------------------------------
// HankakuKatakanaOneShotNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class HankakuKatakanaOneShotNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

