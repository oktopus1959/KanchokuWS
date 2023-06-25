#include "Logger.h"

#include "EscapeNode.h"
#include "State.h"

#include "DeckeyToChars.h"

namespace {
    // エスケープ状態
    class EscapeState : public State {
        DECLARE_CLASS_LOGGER;

        bool bUnncessary = false;

    private:
        inline const EscapeNode* myNode() const { return (const EscapeNode*)pNode; }

    public:
        EscapeState(EscapeNode* pN) {
            Initialize(logger.ClassNameT(), pN);
        }

        // 状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("CALLED: EscapeState"));
            // チェイン
            return true;
        }

        void handleStrokeKeys(int deckey) {
            wchar_t myChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
            LOG_DEBUG(_T("CALLED: {}: deckey={:x}H({}), face={}"), Name, deckey, deckey, myChar);
            STATE_COMMON->SetOutString(myChar);
            bUnncessary = true;
        }

        bool IsUnnecessary() {
            LOG_DEBUG(_T("CALLED"));
            return bUnncessary;
        }

    };
    DEFINE_CLASS_LOGGER(EscapeState);
}

// -------------------------------------------------------------------
// EscapeNode - 1文字履歴機能ノード
DEFINE_CLASS_LOGGER(EscapeNode);

// コンストラクタ
EscapeNode::EscapeNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
EscapeNode::~EscapeNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* EscapeNode::CreateState() { return new EscapeState(this); }

// -------------------------------------------------------------------
// EscapeNodeBuilder - 1文字履歴機能ノードビルダー
DEFINE_CLASS_LOGGER(EscapeNodeBuilder);

Node* EscapeNodeBuilder::CreateNode() {
    return new EscapeNode();
}

