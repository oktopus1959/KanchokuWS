#include "Logger.h"

#include "EscapeNode.h"
#include "State.h"

#include "DeckeyToChars.h"

namespace {
    // エスケープ状態
    class EscapeState : public State {
        DECLARE_CLASS_LOGGER;

        bool bUnncessary = false;

        wchar_t outputChar = '\0';

    private:
        inline const EscapeNode* myNode() const { return (const EscapeNode*)pNode; }

    public:
        EscapeState(EscapeNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        // 出力文字を取得する
        void GetResultStringChain(MStringResult& result) override {
            LOG_DEBUGH(_T("ENTER: {}: resultStr={}, numBS={}"), Name, to_wstr(result.resultStr), result.numBS);
            if (outputChar != '\0') {
                result.resultStr = MString(1, outputChar);
                outputChar = '\0';
            }
            LOG_DEBUGH(_T("LEAVE: {}: resultStr={}, numBS={}"), Name, to_wstr(result.resultStr), result.numBS);
        }

        void handleStrokeKeys(int deckey) {
            wchar_t myChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
            LOG_DEBUG(_T("CALLED: {}: deckey={:x}H({}), face={}"), Name, deckey, deckey, myChar);
            outputChar = myChar;
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
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
EscapeNode::~EscapeNode() {
    LOG_DEBUGH(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* EscapeNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new EscapeState(this);
}

// -------------------------------------------------------------------
// EscapeNodeBuilder - 1文字履歴機能ノードビルダー
DEFINE_CLASS_LOGGER(EscapeNodeBuilder);

Node* EscapeNodeBuilder::CreateNode() {
    return new EscapeNode();
}

