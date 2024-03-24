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
#include "History//HistoryResidentState.h"

#include "Katakana.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughKatakana)

#if 0 || defined(_DEBUG)
#undef LOG_INFO
#undef LOG_DEBUGH
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#endif

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // カタカナ変換機能クラス
    class KatakanaState : public State {
        DECLARE_CLASS_LOGGER;

        bool bInitialized = true;

    public:
        // コンストラクタ
        KatakanaState(KatakanaNode* pN) {
            LOG_INFO(_T("CALLED: CONSTRUCTOR"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~KatakanaState() {
            LOG_DEBUGH(_T("CALLED: DESTRUCTOR"));
            STATE_COMMON->AddOrEraseRunningState(Name, 0);  // 削除
        };

#define MY_NODE ((KatakanaNode*)pNode)

        // DECKEY 処理の前半部(ディスパッチまで)
        void HandleDeckeyChain(int deckey) {
            LOG_DEBUGH(_T("ENTER: {}"), Name);

            ModalState::ModalStatePreProc(this, deckey);
            State::HandleDeckeyChain(deckey);

            LOG_DEBUGH(_T("LEAVE: {}"), Name);
        }

        // 機能状態に対して生成時処理を実行する
        void DoProcOnCreated() override {
            _LOG_DEBUGH(_T("ENTER"));

            if (!STATE_COMMON->AddOrEraseRunningState(Name, this)) {
                LOG_DEBUGH(_T("Already same function had been running. Mark it unnecessary."));
                // すでに同じ機能が走っていたのでそれ以降に不要マークを付けた
                return;
            }

            setKatakanaModeMarker();

            MarkNecessary();
            _LOG_DEBUGH(_T("LEAVE: CHAIN ME"));
        }

        // 中間チェック
        void DoIntermediateCheck() override {
            LOG_INFO(_T("CALLED: {}: Clear bInitialized"), Name);
            bInitialized = false;
        }

        // 履歴検索を初期化する状態か
        bool IsHistoryReset() {
            bool result = (NextState() && NextState()->IsHistoryReset());
            _LOG_DEBUGH(_T("CALLED: {}: result={}"), Name, result);
            return result;
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
        MString TranslateString(const MString& outStr) override {
            _LOG_DEBUGH(_T("ENTER: {}: outStr={}"), Name, to_wstr(outStr));
            MString result;
            if (NextState()) {
                result = translate(NextState()->TranslateString(outStr));
            } else {
                result = translate(outStr);
                setKatakanaModeMarker();
            }
            _LOG_DEBUGH(_T("LEAVE: {}, translated={}"), Name, to_wstr(result));
            return result;
        }

        // FullEscape の処理 -- HISTORYを呼ぶ
        void handleFullEscape() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            //cancelMe();
            HISTORY_RESIDENT_STATE->handleFullEscapeResidentState();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // その他の特殊キー (常駐の履歴機能があればそれを呼び出す)
        void handleSpecialKeys(int deckey) {
            LOG_DEBUG(_T("CALLED: {}, deckey={}"), Name, deckey);
            if (HISTORY_RESIDENT_STATE) {
                // 常駐の履歴機能があればそれを呼び出す
                HISTORY_RESIDENT_STATE->dispatchDeckey(deckey);
            } else {
                State::handleSpecialKeys(deckey);
            }
        }

        // KatakanaConversionの処理 - 処理のキャンセル
        void handleKatakanaConversion() override {
            LOG_DEBUGH(_T("CALLED: {}: Initialized={}"), Name, bInitialized);
            if (!bInitialized) {
                cancelMe();
            }
        }

        // CommitState の処理 -- 処理のコミット
        void handleCommitState() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // モード標識文字を返す
        mchar_t GetModeMarker() override {
            return utils::safe_front(MY_NODE->getString());
        }

    protected:
        void setKatakanaModeMarker() {
            STATE_COMMON->SetKatakanaModeMarkerShowFlag();
        }

        void cancelMe() {
            STATE_COMMON->AddOrEraseRunningState(Name, 0);  // 削除
            MarkUnnecessary();
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
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
KatakanaNode::~KatakanaNode() {
    LOG_DEBUGH(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* KatakanaNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new KatakanaState(this);
}

std::unique_ptr<KatakanaNode> KatakanaNode::Singleton;

void KatakanaNode::CreateSingleton() {
    LOG_DEBUGH(_T("CALLED"));
    if (KatakanaNode::Singleton == 0) {
        KatakanaNode::Singleton.reset(new KatakanaNode());
    }
}

// -------------------------------------------------------------------
// KatakanaNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(KatakanaNodeBuilder);

Node* KatakanaNodeBuilder::CreateNode() {
    LOG_DEBUGH(_T("CALLED"));
    return new KatakanaNode();
}

