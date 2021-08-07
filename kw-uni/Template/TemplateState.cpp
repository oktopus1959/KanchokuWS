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

#define NAME_PTR (Name.c_str())
#define MY_NODE ((TemplateNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("ENTER"));

            // 前状態にチェインする
            LOG_DEBUG(_T("LEAVE: CHAIN ME"));

            return true;
        }

         // Strokeキー を処理する
        void handleStrokeKeys(int deckey) {
            LOG_DEBUG(_T("CALLED: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
            STATE_COMMON->SetOutString(make_fullwide_char(DECKEY_TO_CHARS->GetCharFromDeckey(deckey)), 0);
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            bUnnecessary = true;
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

