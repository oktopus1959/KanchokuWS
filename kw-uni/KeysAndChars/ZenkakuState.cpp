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

#include "Zenkaku.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughZenkaku)

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // 全角変換機能クラス
    class ZenkakuState : public TranslationState {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        ZenkakuState(ZenkakuNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~ZenkakuState() { };

#define NAME_PTR (Name.c_str())
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
            LOG_DEBUG(_T("CALLED: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
            STATE_COMMON->ClearOrigString();
            outputZenkakuCharFromDeckey(deckey);
        }

         // Shiftキーで修飾されたキーを処理する
        void handleShiftKeys(int deckey) {
            LOG_DEBUG(_T("CALLED: %s: deckey=%xH(%d), char=%c"), NAME_PTR, deckey, deckey);
            STATE_COMMON->ClearOrigString();
            outputZenkakuCharFromDeckey(deckey);
        }

        // Space キーの処理 -- origChar を出力してキャンセル
        //void handleSpaceKey() {
        //    STATE_COMMON->OutputOrigString();
        //    cancelMe();
        //}

        // FullEscape の処理 -- 処理のキャンセル
        void handleFullEscape() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            cancelMe();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
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
                HISTORY_STAY_STATE->SetTranslatedOutString(MString(1, wch));
            }
        }

        void cancelMe() {
            bUnnecessary = true;
            STATE_COMMON->SetZenkakuModeMarkerClearFlag();
        }
    };
    DEFINE_CLASS_LOGGER(ZenkakuState);

    // -------------------------------------------------------------------
    // 1文字全角変換機能クラス
    class ZenkakuOneState : public ZenkakuState {
        DECLARE_CLASS_LOGGER;

    public:
        ZenkakuOneState(ZenkakuOneNode* pN) : ZenkakuState(pN) { }

        void DoDeckeyPreProc(int deckey) {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
            State::DoDeckeyPreProc(deckey);
            // 1文字処理したら自状態は不要になる
            cancelMe();
        }

        //void DoDeckeyPostProc() {
        //    _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
        //    // 1文字処理したら抜ける
        //    cancelMe();
        //    State::DoDeckeyPostProc();
        //}

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
    LOG_INFO(_T("CALLED"));
    return new ZenkakuState(this);
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
    LOG_INFO(_T("CALLED"));
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

