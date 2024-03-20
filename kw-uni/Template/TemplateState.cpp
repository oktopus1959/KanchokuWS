#include "Logger.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "KanchokuIni.h"
#include "Constants.h"
#include "DeckeyToChars.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "Node.h"
#include "State.h"
#include "OutputStack.h"

#include "Template.h"

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // 状態テンプレートクラス
    class TemplateState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        TemplateState(TemplateNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~TemplateState() { };

#define MY_NODE ((TemplateNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        void DoProcOnCreated() override {
            LOG_DEBUG(_T("ENTER"));
            MarkNecessary();
            LOG_DEBUG(_T("LEAVE: CHAIN ME"));
        }

         // Strokeキー を処理する
        void handleStrokeKeys(int deckey) {
            LOG_INFO(_T("CALLED: {}: deckey={:x}H({})"), Name, deckey, deckey);
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            LOG_DEBUG(_T("CALLED: {}"), Name);
            MarkUnnecessary();
        }

    };
    DEFINE_CLASS_LOGGER(TemplateState);

} // namespace

// -------------------------------------------------------------------
// TemplateNode - ノードのテンプレート
DEFINE_CLASS_LOGGER(TemplateNode);

// コンストラクタ
TemplateNode::TemplateNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
TemplateNode::~TemplateNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* TemplateNode::CreateState() {
    return new TemplateState(this);
}

// -------------------------------------------------------------------
// TemplateNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(TemplateNodeBuilder);

Node* TemplateNodeBuilder::CreateNode() {
    return new TemplateNode();
}

