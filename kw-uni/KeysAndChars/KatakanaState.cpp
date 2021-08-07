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
#include "StrokeTable.h"
#include "OutputStack.h"
#include "TranslationState.h"
#include "History//HistoryStayState.h"

#include "Katakana.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughKatakana)

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // カタカナ変換機能クラス
    class KatakanaState : public StrokeTranslationState {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        KatakanaState(KatakanaNode* pN) {
            LOG_INFO(_T("CALLED: CONSTRUCTOR"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~KatakanaState() { };

#define NAME_PTR (Name.c_str())
#define MY_NODE ((KatakanaNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("ENTER"));

            if (!STATE_COMMON->AddOrEraseRunningState(Name, this)) {
                LOG_INFO(_T("Already same function had been running. Mark it unnecessary."));
                // すでに同じ機能が走っていたのでそれ以降に不要マークを付けた
                return false;
            }

            setKatakanaModeMarker();

            // 前状態にチェインする
            LOG_DEBUG(_T("LEAVE: CHAIN ME"));

            return true;
        }

    private:
        MString translate(const MString& str) {
            MString result = str;
            for (size_t i = 0; i < result.size(); ++i) {
                mchar_t ch = (wchar_t)result[i];
                if (utils::is_hiragana(ch)) { result[i] = utils::hiragana_to_katakana(ch); }
            }
            return result;
        }

    public:
        // 文字列を変換
        MString TranslateString(const MString& outStr) {
            LOG_DEBUGH(_T("ENTER: %s: outStr=%s"), NAME_PTR, MAKE_WPTR(outStr));
            MString result;
            if (pNext) {
                result = translate(pNext->TranslateString(outStr));
            } else {
                result = translate(outStr);
                setKatakanaModeMarker();
            }
            LOG_DEBUGH(_T("LEAVE: %s, translated=%s"), NAME_PTR, MAKE_WPTR(result));
            return result;
        }

        // モード標識文字を返す
        mchar_t GetModeMarker() {
            return utils::safe_front(MY_NODE->getString());
        }

    protected:
        void setKatakanaModeMarker() {
            STATE_COMMON->SetKatakanaModeMarkerShowFlag();
        }

        void cancelMe() {
            bUnnecessary = true;
            STATE_COMMON->SetKatakanaModeMarkerClearFlag();
        }
    };
    DEFINE_CLASS_LOGGER(KatakanaState);

} // namespace

// -------------------------------------------------------------------
// KatakanaNode - カタカナ変換ノード
DEFINE_CLASS_LOGGER(KatakanaNode);

// コンストラクタ
KatakanaNode::KatakanaNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
KatakanaNode::~KatakanaNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* KatakanaNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new KatakanaState(this);
}

// -------------------------------------------------------------------
// KatakanaNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(KatakanaNodeBuilder);

Node* KatakanaNodeBuilder::CreateNode() {
    LOG_INFO(_T("CALLED"));
    return new KatakanaNode();
}

