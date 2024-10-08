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
#include "StrokeMerger/StrokeMergerHistoryResidentState.h"

#include "Zenkaku.h"

#if 0 || defined(_DEBUG)
#undef _DEBUG_SENT
#undef _DEBUG_FLAG
#undef LOG_INFO
#undef LOG_DEBUGH
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#undef _LOG_DEBUGH_COND
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif
#define _LOG_DEBUGH_FLAG (SETTINGS->debughZenkaku)

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // 全角変換機能クラス
    class ZenkakuState : public State {
        DECLARE_CLASS_LOGGER;

        bool bInitialized = true;

        wchar_t zenkakuChar = '\0';

    public:
        // コンストラクタ
        ZenkakuState(ZenkakuNode* pN) {
            LOG_INFO(_T("CALLED: CONSTRUCTOR"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~ZenkakuState() {
            LOG_DEBUGH(_T("CALLED: DESTRUCTOR"));
        };

#define MY_NODE ((ZenkakuNode*)pNode)

        // 状態が生成されたときに実行する処理 (その状態をチェインする場合は true を返す)
        void DoProcOnCreated() override {
            LOG_INFO(_T("ENTER"));

            STATE_COMMON->SetZenkakuModeMarkerShowFlag();

            // 前状態にチェインする
            MarkNecessary();
            LOG_INFO(_T("LEAVE: CHAIN ME"));
        }

        // 中間チェック
        void DoIntermediateCheck() override {
            LOG_INFO(_T("CALLED: {}: Clear bInitialized"), Name);
            bInitialized = false;
        }

        // 出力文字を取得する
        void GetResultStringChain(MStringResult& resultOut) override {
            LOG_DEBUGH(_T("ENTER: {}: resultStr={}, numBS={}"), Name, to_wstr(resultOut.resultStr()), resultOut.numBS());
            if (!resultStr.isDefault()) {
                resultOut.setResult(resultStr);
            } else if (zenkakuChar != '\0') {
                resultOut.setResult(zenkakuChar);
                zenkakuChar = '\0';
            }
            LOG_DEBUGH(_T("LEAVE: {}: resultStr={}, numBS={}"), Name, to_wstr(resultOut.resultStr()), resultOut.numBS());
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
            LOG_DEBUG(_T("CALLED: {}: deckey={:x}H({})"), Name, deckey, deckey);
            STATE_COMMON->ClearOrigString();
            outputZenkakuCharFromDeckey(deckey);
        }

        // その他の特殊キー (常駐の履歴機能があればそれを呼び出す)
        void handleSpecialKeys(int deckey) {
            MERGER_HISTORY_RESIDENT_STATE->dispatchDeckey(deckey);
        }

        // Space キーの処理 -- origChar を出力してキャンセル
        //void handleSpaceKey() {
        //    STATE_COMMON->OutputOrigString();
        //    cancelMe();
        //}

        // FullEscape の処理 -- 処理のキャンセル
        void handleFullEscape() {
            LOG_DEBUGH(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            LOG_DEBUGH(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // ZenkakuConversionの処理 - 処理のキャンセル
        void handleZenkakuConversion() {
            LOG_DEBUGH(_T("CALLED: {}: Initialized={}"), Name, bInitialized);
            if (!bInitialized) {
                cancelMe();
            }
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
                zenkakuChar = wch;
            }
        }

        void outputOrigChar(mchar_t origChar) {
            zenkakuChar = (wchar_t)origChar;
        }

        void cancelMe() {
            LOG_DEBUGH(_T("CALLED: {}"), Name);
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
            LOG_INFO(_T("CALLED: CONSTRUCTOR"));
            Name = logger.ClassNameT();
        }

        // 不要な状態になったか
        void DoIntermediateCheck() override {
            _LOG_DEBUGH(_T("ENTER: {}"), Name);
            // 1文字処理したら自状態は不要になる
            cancelMe();
        }

        //// Space キーの処理 -- origChar を出力してキャンセル
        //void handleSpaceKey() {
        //    //STATE_COMMON->OutputOrigString();
        //    outputOrigChar(STATE_COMMON->OrigChar());
        //    cancelMe();
        //}

    };
    DEFINE_CLASS_LOGGER(ZenkakuOneState);

} // namespace

// -------------------------------------------------------------------
// ZenkakuNode - 全角変換ノード
DEFINE_CLASS_LOGGER(ZenkakuNode);

// コンストラクタ
ZenkakuNode::ZenkakuNode() {
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
ZenkakuNode::~ZenkakuNode() {
    LOG_DEBUGH(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* ZenkakuNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new ZenkakuState(ZENKAKU_NODE);
}

std::unique_ptr<ZenkakuNode> ZenkakuNode::_singleton;

ZenkakuNode* ZenkakuNode::Singleton() {
    if (!_singleton) _singleton.reset(new ZenkakuNode());
    return _singleton.get();
}

//void ZenkakuNode::CreateSingleton() {
//    LOG_DEBUGH(_T("CALLED"));
//    if (_singleton == 0) {
//        ZenkakuNode::Singleton.reset(new ZenkakuNode());
//    }
//}

// -------------------------------------------------------------------
// ZenkakuOneNode - 1文字全角変換ノード
DEFINE_CLASS_LOGGER(ZenkakuOneNode);

// コンストラクタ
ZenkakuOneNode::ZenkakuOneNode() {
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
ZenkakuOneNode::~ZenkakuOneNode() {
    LOG_DEBUGH(_T("CALLED: destructor"));
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
    LOG_DEBUGH(_T("CALLED"));
    return new ZenkakuNode();
}

// -------------------------------------------------------------------
// ZenkakuOneNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(ZenkakuOneNodeBuilder);

Node* ZenkakuOneNodeBuilder::CreateNode() {
    LOG_DEBUGH(_T("CALLED"));
    return new ZenkakuOneNode();
}

