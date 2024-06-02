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
//#include "History//HistoryResidentState.h"

#include "MyPrevChar.h"

#if 1 || defined(_DEBUG)
#undef LOG_INFO
#undef LOG_DEBUGH
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#endif

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
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~MyCharState() { };

//#define MY_NODE ((MyCharNode*)pNode)

        // 出力文字を取得する
        void GetResultStringChain(MStringResult& resultOut) override {
            const State* prev = PrevState();
            if (prev) {
                mchar_t myCh = prev->getMyChar();
                if (myCh != '\0') {
                    resultOut.setResult(to_mstr(myCh));
                }
            }
            _LOG_DEBUGH(_T("CALLED: {}: resultStr={}"), Name, to_wstr(resultOut.resultStr()));
        }

    };
    DEFINE_CLASS_LOGGER(MyCharState);

    // -------------------------------------------------------------------
    // 前キー文字出力機能クラス
    class PrevCharState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        PrevCharState(PrevCharNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~PrevCharState() { };

//#define MY_NODE ((PrevCharNode*)pNode)

        // 出力文字を取得する
        void GetResultStringChain(MStringResult& resultOut) override {
            const State* prev = PrevState();
            if (prev) {
                prev = prev->PrevState();
                if (prev) {
                    mchar_t myCh = prev->getMyChar();
                    if (myCh != '\0') {
                        resultOut.setResult(to_mstr(myCh));
                    }
                }
            }
            //if (STATE_COMMON->OrigString().size() >= 2) {
            //    STATE_COMMON->PopOrigString();
            //}
            //resultOut.setResult(STATE_COMMON->OrigString());
            _LOG_DEBUGH(_T("CALLED: {}: resultStr={}"), Name, to_wstr(resultOut.resultStr()));
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
    LOG_INFO(_T("CALLED"));
    return new MyCharState(this);
}

void MyCharNode::CreateSingleton() {
    if (!_singleton) {
        _singleton.reset(new MyCharNode());
    }
}

std::unique_ptr<MyCharNode> MyCharNode::_singleton;

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
    LOG_INFO(_T("CALLED"));
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

