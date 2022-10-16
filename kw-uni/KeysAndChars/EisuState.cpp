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

#include "Eisu.h"

#if 0 || defined(_DEBUG)
#define IS_LOG_DEBUGH_ENABLED true
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // 英数入力機能クラス
    class EisuState : public StrokeTranslationState {
        DECLARE_CLASS_LOGGER;

        bool isPrevSpaceKey = false;

    public:
        // コンストラクタ
        EisuState(EisuNode* pN) {
            LOG_INFO(_T("CALLED: CONSTRUCTOR"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~EisuState() {
            LOG_INFO(_T("CALLED: DESTRUCTOR"));
        };

#define NAME_PTR (Name.c_str())
#define MY_NODE ((EisuNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER"));

            if (!STATE_COMMON->AddOrEraseRunningState(Name, this)) {
                LOG_INFO(_T("Already same function had been running. Mark it unnecessary."));
                // すでに同じ機能が走っていたのでそれ以降に不要マークを付けた
                return false;
            }

            //setEisuModeMarker();

            // 前状態にチェインする
            _LOG_DEBUGH(_T("LEAVE: CHAIN ME"));

            return true;
        }

        // 履歴検索を初期化する状態か
        bool IsHistoryReset() {
            bool result = (pNext && pNext->IsHistoryReset());
            _LOG_DEBUGH(_T("CALLED: %s: result=%s"), NAME_PTR, BOOL_TO_WPTR(result));
            return result;
        }

    public:
        // StrokeKey を処理する
        void handleStrokeKeys(int deckey) {
            wchar_t myChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
            LOG_INFO(_T("ENTER: %s: deckey=%xH(%d), face=%c"), NAME_PTR, deckey, deckey, myChar);
            STATE_COMMON->AppendOrigString(myChar);

            // キーボードフェイス文字を返す
            LOG_INFO(_T("SetOutString"));
            STATE_COMMON->SetOutString(myChar, 0);

            isPrevSpaceKey = false;
            LOG_INFO(_T("LEAVE"));
        }

        // Shift飾修されたキー
        void handleShiftKeys(int deckey) {
            _LOG_DEBUGH(_T("ENTER: %s, deckey=%x(%d)"), NAME_PTR, deckey, deckey);
            handleStrokeKeys(deckey);
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
        }

        // FullEscape の処理 -- HISTORYを呼ぶ
        void handleFullEscape() override {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            //cancelMe();
            HISTORY_STAY_STATE->handleFullEscapeStayState();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() override {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            cancelMe();
        }

        // Space の処理 -- 処理のキャンセル
        void handleSpaceKey() override {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            if (isPrevSpaceKey) {
                // 2回続けて呼ばれたらキャンセル
                cancelMe();
            } else {
                handleStrokeKeys(STROKE_SPACE_DECKEY);
                isPrevSpaceKey = true;
            }
        }

        // EisuCancel - 処理のキャンセル
        void handleEisuCancel() override {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            cancelMe();
        }

        // モード標識文字を返す
        mchar_t GetModeMarker() override {
            return utils::safe_front(MY_NODE->getString());
        }

    protected:
        //void setEisuModeMarker() {
        //    STATE_COMMON->SetEisuModeMarkerShowFlag();
        //}

        void cancelMe() {
            STATE_COMMON->AddOrEraseRunningState(Name, 0);  // 削除
            bUnnecessary = true;
            //STATE_COMMON->SetEisuModeMarkerClearFlag();
        }
    };
    DEFINE_CLASS_LOGGER(EisuState);

} // namespace

// -------------------------------------------------------------------
// EisuNode - 英数変換ノード
DEFINE_CLASS_LOGGER(EisuNode);

// コンストラクタ
EisuNode::EisuNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
EisuNode::~EisuNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* EisuNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new EisuState(this);
}

std::unique_ptr<EisuNode> EisuNode::Singleton;

// Decoder から初期化時に呼ぶ必要あり
void EisuNode::CreateSingleton() {
    LOG_INFO(_T("CALLED"));
    if (EisuNode::Singleton == 0) {
        EisuNode::Singleton.reset(new EisuNode());
    }
}

// -------------------------------------------------------------------
// EisuNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(EisuNodeBuilder);

Node* EisuNodeBuilder::CreateNode() {
    LOG_INFO(_T("CALLED"));
    return new EisuNode();
}

