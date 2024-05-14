#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// 様々なワンショット機能を集めたファイル
// 
// -------------------------------------------------------------------
// ブロッカー設定
class BlockerSetterNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    BlockerSetterNode();

    ~BlockerSetterNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("壁")); }

    String getNodeName() const { return _T("BlockerSetterNode"); }

};

// -------------------------------------------------------------------
// BlockerSetterNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class BlockerSetterNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

