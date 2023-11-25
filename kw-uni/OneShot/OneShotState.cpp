#include "Logger.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "Node.h"
#include "State.h"
#include "OutputStack.h"

#include "OneShot.h"

// -------------------------------------------------------------------
// 様々なワンショット機能を集めたファイル

namespace {

    // -------------------------------------------------------------------
    // ブロッカー設定
    class BlockerSetterState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        BlockerSetterState(Node* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~BlockerSetterState() { };

#define MY_NODE ((TemplateNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("ENTER"));

            // ブロッカーをセット/リセットする
            //STATE_COMMON->SetHistoryBlockFlag();
            OUTPUT_STACK->toggleLastBlocker();

            // チェイン不要
            LOG_DEBUG(_T("LEAVE: NO CHAIN"));

            return false;
        }

    };
    DEFINE_CLASS_LOGGER(BlockerSetterState);

} // namespace

// -------------------------------------------------------------------
// BlockerSetterNode
DEFINE_CLASS_LOGGER(BlockerSetterNode);

// コンストラクタ
BlockerSetterNode::BlockerSetterNode() {
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
BlockerSetterNode::~BlockerSetterNode() {
    LOG_DEBUGH(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* BlockerSetterNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new BlockerSetterState(this);
}

// -------------------------------------------------------------------
// BlockerSetterNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(BlockerSetterNodeBuilder);

Node* BlockerSetterNodeBuilder::CreateNode() {
    return new BlockerSetterNode();
}

