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

#include "KatakanaOneShot.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughMazegaki)

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // 状態テンプレートクラス
    class KatakanaOneShotState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        KatakanaOneShotState(KatakanaOneShotNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~KatakanaOneShotState() { };

#define MY_NODE ((KatakanaOneShotNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER"));

            auto outStr = OUTPUT_STACK->GetLastHiraganaStr<MString>(true);
            size_t numBS = outStr.size();
            _LOG_DEBUGH(_T("H->K: outStr={}, numBS={}"), to_wstr(outStr), numBS);
            if (SETTINGS->mazeRemoveHeadSpace && numBS > 0 && outStr[0] == ' ') {
                // 全読みの先頭の空白を削除
                _LOG_DEBUGH(_T("REMOVE_HEAD_SPACE"));
                outStr = outStr.substr(1);
            }
            if (!outStr.empty()) {
                // カタカナに変換して置換する
                STATE_COMMON->SetOutString(utils::convert_hiragana_to_katakana(outStr), numBS);
                OUTPUT_STACK->setHistBlockerAt(outStr.size());
            } else {
                outStr = OUTPUT_STACK->GetLastKatakanaStr<MString>();
                numBS = outStr.size();
                _LOG_DEBUGH(_T("K->H: outStr={}, numBS={}"), to_wstr(outStr), numBS);
                if (!outStr.empty()) {
                    // ひらがなに変換して置換する
                    STATE_COMMON->SetOutString(utils::convert_katakana_to_hiragana(outStr), numBS);
                }
            }

            // チェイン不要
            _LOG_DEBUGH(_T("LEAVE: NO CHAIN"));

            return false;
        }

    };
    DEFINE_CLASS_LOGGER(KatakanaOneShotState);

} // namespace

// -------------------------------------------------------------------
// KatakanaOneShotNode - ノードのテンプレート
DEFINE_CLASS_LOGGER(KatakanaOneShotNode);

// コンストラクタ
KatakanaOneShotNode::KatakanaOneShotNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
KatakanaOneShotNode::~KatakanaOneShotNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* KatakanaOneShotNode::CreateState() {
    return new KatakanaOneShotState(this);
}

// -------------------------------------------------------------------
// KatakanaOneShotNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(KatakanaOneShotNodeBuilder);

Node* KatakanaOneShotNodeBuilder::CreateNode() {
    return new KatakanaOneShotNode();
}

