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
#include "History//HistoryResidentState.h"

#include "Zenkaku.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughZenkaku)

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // 全角変換機能クラス
    class ZenkakuState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        ZenkakuState(ZenkakuNode* pN) {
            LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~ZenkakuState() {
            LOG_INFO(_T("CALLED: DESTRUCTOR"));
        };

#define MY_NODE ((ZenkakuNode*)pNode)

        // 状態が生成されたときに実行する処理 (その状態をチェインする場合は true を返す)
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("ENTER"));

            STATE_COMMON->SetZenkakuModeMarkerShowFlag();

            // 前状態にチェインする
            LOG_DEBUG(_T("LEAVE: CHAIN ME"));

            return true;
        }

         // Strokeキー を処理する
        void handleStrokeKeys(int deckey) {
            LOG_DEBUG(_T("CALLED: {}: deckey={:x}H({})"), Name, deckey, deckey);
            if (deckey >= FUNC_DECKEY_START) {
                // 機能キーだったら処理をキャンセルする
                LOG_DEBUG(_T("CANCELED"));
                cancelMe();
                return;
            }
            STATE_COMMON->ClearOrigString();
            outputZenkakuCharFromDeckey(deckey);
        }

         // Shiftキーで修飾されたキーを処理する
        void handleShiftKeys(int deckey) {
            LOG_DEBUG(_T("CALLED: {}: deckey={:x}H({}), char={}"), Name, deckey, deckey);
            STATE_COMMON->ClearOrigString();
            outputZenkakuCharFromDeckey(deckey);
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

        // Space キーの処理 -- origChar を出力してキャンセル
        //void handleSpaceKey() {
        //    STATE_COMMON->OutputOrigString();
        //    cancelMe();
        //}

        // FullEscape の処理 -- 処理のキャンセル
        void handleFullEscape() {
            LOG_DEBUG(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            LOG_DEBUG(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // ZenkakuConversionの処理 - 処理のキャンセル
        void handleZenkakuConversion() {
            LOG_DEBUGH(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // モード標識文字を返す
        mchar_t GetModeMarker() {
            return utils::safe_front(MY_NODE->getString());
        }

    protected:
        void outputZenkakuCharFromDeckey(int deckey) {
            wchar_t wch = 0;
            if (deckey == DECKEY_TO_CHARS->GetYenPos()) {
                wch = 0xffe5;
            } else {
                wchar_t ch = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
                if (ch > 0) wch = make_fullwide_char(ch);
            }
            STATE_COMMON->SetZenkakuModeMarkerShowFlag();
            if (wch > 0) {
                //STATE_COMMON->SetOutString(wch, 0);
                HISTORY_RESIDENT_STATE->SetTranslatedOutString(MString(1, wch), 0);
            }
        }

        void cancelMe() {
            MarkUnnecessary();
            STATE_COMMON->SetZenkakuModeMarkerClearFlag();
        }
    };
    DEFINE_CLASS_LOGGER(ZenkakuState);

    // -------------------------------------------------------------------
    // 1文字全角変換機能クラス
    class ZenkakuOneState : public ZenkakuState {
        DECLARE_CLASS_LOGGER;

    public:
        ZenkakuOneState(ZenkakuOneNode* pN) : ZenkakuState(pN) {
            LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
            Name = logger.ClassNameT();
        }

        // 不要な状態になったか
        void DoIntermediateCheck() {
            _LOG_DEBUGH(_T("ENTER: {}"), Name);
            // 1文字処理したら自状態は不要になる
            cancelMe();
        }

        // Space キーの処理 -- origChar を出力してキャンセル
        void handleSpaceKey() {
            STATE_COMMON->OutputOrigString();
            cancelMe();
        }

    };
    DEFINE_CLASS_LOGGER(ZenkakuOneState);

} // namespace

// -------------------------------------------------------------------
// ZenkakuNode - 全角変換ノード
DEFINE_CLASS_LOGGER(ZenkakuNode);

// コンストラクタ
ZenkakuNode::ZenkakuNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
ZenkakuNode::~ZenkakuNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* ZenkakuNode::CreateState() {
    LOG_INFOH(_T("CALLED"));
    return new ZenkakuState(ZenkakuNode::Singleton.get());
}

std::unique_ptr<ZenkakuNode> ZenkakuNode::Singleton;

void ZenkakuNode::CreateSingleton() {
    LOG_INFO(_T("CALLED"));
    if (ZenkakuNode::Singleton == 0) {
        ZenkakuNode::Singleton.reset(new ZenkakuNode());
    }
}

// -------------------------------------------------------------------
// ZenkakuOneNode - 1文字全角変換ノード
DEFINE_CLASS_LOGGER(ZenkakuOneNode);

// コンストラクタ
ZenkakuOneNode::ZenkakuOneNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
ZenkakuOneNode::~ZenkakuOneNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* ZenkakuOneNode::CreateState() {
    LOG_INFOH(_T("CALLED"));
    return new ZenkakuOneState(this);
}

// -------------------------------------------------------------------
// ZenkakuNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(ZenkakuNodeBuilder);

Node* ZenkakuNodeBuilder::CreateNode() {
    LOG_INFO(_T("CALLED"));
    return new ZenkakuNode();
}

// -------------------------------------------------------------------
// ZenkakuOneNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(ZenkakuOneNodeBuilder);

Node* ZenkakuOneNodeBuilder::CreateNode() {
    LOG_INFO(_T("CALLED"));
    return new ZenkakuOneNode();
}

