#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// EisuNode
class EisuNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
public:
    EisuNode();

    ~EisuNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return to_mstr(_T("Ａ")); }

    String getNodeName() const { return _T("EisuNode"); }

    // 前回の状態のときの大文字入力時のDeckeyカウント
    size_t prevCapitalDeckeyCount = 0;

    // 前回の履歴検索呼び出し時のDeckeyカウント
    size_t prevHistSearchDeckeyCount = 0;

    // 状態開始時に、末尾にブロッカーを設定するか
    // 「Space」「Key」と分けて入力した時に、「K」の入力時に「Space」の末尾にブロッカーを設定したい
    bool blockerNeeded = false;

    static void CreateSingleton();

    static std::unique_ptr<EisuNode> Singleton;
};
#define EISU_NODE (EisuNode::Singleton)

// -------------------------------------------------------------------
// EisuNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class EisuNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

