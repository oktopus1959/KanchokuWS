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
#include "TranslationState.h"
#include "History//HistoryStayState.h"

#include "MyPrevChar.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughMyPrevChar)

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // 自キー文字出力機能クラス
    class MyCharState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        MyCharState(MyCharNode* pN) {
            LOG_DEBUG(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~MyCharState() { };

#define MY_NODE ((MyCharNode*)pNode)

        // 状態が生成されたときに実行する処理 (その状態をチェインする場合は true を返す)
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("CALLED"));
            STATE_COMMON->OutputOrigString();
            // チェイン不要
            return false;
        }
    };
    DEFINE_CLASS_LOGGER(MyCharState);

    // -------------------------------------------------------------------
    // 前キー文字出力機能クラス
    class PrevCharState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        PrevCharState(PrevCharNode* pN) {
            LOG_DEBUG(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~PrevCharState() { };

#define MY_NODE ((MyCharNode*)pNode)

        // 状態が生成されたときに実行する処理 (その状態をチェインする場合は true を返す)
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("CALLED"));
            if (STATE_COMMON->OrigString().size() >= 2) {
                STATE_COMMON->PopOrigString();
            }
            STATE_COMMON->OutputOrigString();
            // チェイン不要
            return false;
        }
    };
    DEFINE_CLASS_LOGGER(PrevCharState);

} // namespace

// -------------------------------------------------------------------
// MyCharNode - 自キー文字出力ノード
DEFINE_CLASS_LOGGER(MyCharNode);

// コンストラクタ
MyCharNode::MyCharNode() {
    LOG_DEBUG(_T("CALLED: constructor"));
}

// デストラクタ
MyCharNode::~MyCharNode() {
    LOG_DEBUG(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* MyCharNode::CreateState() {
    LOG_DEBUG(_T("CALLED"));
    return new MyCharState(this);
}

// -------------------------------------------------------------------
// PrevCharNode - 前キー文字出力ノード
DEFINE_CLASS_LOGGER(PrevCharNode);

// コンストラクタ
PrevCharNode::PrevCharNode() {
    LOG_DEBUG(_T("CALLED: constructor"));
}

// デストラクタ
PrevCharNode::~PrevCharNode() {
    LOG_DEBUG(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* PrevCharNode::CreateState() {
    LOG_DEBUG(_T("CALLED"));
    return new PrevCharState(this);
}

void PrevCharNode::CreateSingleton() {
    if (!_singleton) {
        _singleton.reset(new PrevCharNode());
    }
}

std::unique_ptr<PrevCharNode> PrevCharNode::_singleton;

// -------------------------------------------------------------------
// MyCharNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(MyCharNodeBuilder);

Node* MyCharNodeBuilder::CreateNode() {
    LOG_DEBUG(_T("CALLED"));
    return new MyCharNode();
}

// -------------------------------------------------------------------
// PrevCharNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(PrevCharNodeBuilder);

Node* PrevCharNodeBuilder::CreateNode() {
    LOG_DEBUG(_T("CALLED"));
    return new PrevCharNode();
}

