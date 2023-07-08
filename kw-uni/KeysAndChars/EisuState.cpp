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

#include "Eisu.h"

#if 0 || defined(_DEBUG)
#define IS_LOG_DEBUGH_ENABLED true
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

namespace {

#define MAX_YOMI_LEN 10

    // -------------------------------------------------------------------
    // 英数入力機能クラス
    class EisuState : public State {
        DECLARE_CLASS_LOGGER;

        size_t firstTotalCnt = 0;
        size_t firstSpaceKeyCnt = 0;
        size_t prevSpaceKeyCnt = 0;
        size_t prevLowerHeadCnt = 0;
        size_t capitalCharCnt = 1;      // 状態が生成されたときはすでに先頭文字が入力されている

    public:
        // コンストラクタ
        EisuState(EisuNode* pN) {
            LOG_INFO(_T("CALLED: CONSTRUCTOR"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~EisuState() {
            LOG_INFO(_T("CALLED: DESTRUCTOR"));
        };

#define MY_NODE ((EisuNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() override {
            firstTotalCnt = STATE_COMMON->GetTotalDecKeyCount();
            auto prevCapitalCnt = MY_NODE->prevCapitalDeckeyCount;  // 前回の状態のときの大文字入力時のDeckeyカウント
            MY_NODE->prevCapitalDeckeyCount = firstTotalCnt;

            LOG_INFO(_T("ENTER: totalDeckeyCount={}, prevCapitalCount={}"), firstTotalCnt, prevCapitalCnt);

            // ブロッカーフラグを先に取得しておく
            bool blockerNeeded = MY_NODE->blockerNeeded;
            MY_NODE->blockerNeeded = false;

            if (firstTotalCnt == prevCapitalCnt + 1) {
                // 英大文字を連続して入力している状態なので、即抜ける
                // この処理は、次の STATE_COMMON->AddOrEraseRunningState() よりも先にやっておく必要がある
                LOG_INFO(_T("Continuously input capital chars. prevCapitalDeckeyCount={}"), MY_NODE->prevCapitalDeckeyCount);
                return false;
            }

            if (!STATE_COMMON->AddOrEraseRunningState(Name, this)) {
                LOG_INFO(_T("Already same function had been running. Mark it unnecessary."));
                // すでに同じ機能が走っていたのでそれ以降に不要マークを付けた
                return false;
            }

            // 必要ならブロッカーを設定する
            if (blockerNeeded) OUTPUT_STACK->setHistBlocker();
            //setEisuModeMarker();

            // 英数モードフラグの設定
            CheckMyState();

            // 前状態にチェインする
            LOG_INFO(_T("LEAVE: CHAIN ME"));

            return true;
        }

        // 自身の状態をチェックして後処理するのに使う。DECKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
        void CheckMyState() override {
            LOG_DEBUGH(_T("CALLED: {}, Unnecessary={}"), Name, IsUnnecessary());
            // 英数モードフラグの設定
            if (!IsUnnecessary()) STATE_COMMON->SetCurrentModeIsEisu();
        }

        // 履歴検索を初期化する状態か
        bool IsHistoryReset() {
            bool result = (pNext && pNext->IsHistoryReset());
            _LOG_DEBUGH(_T("CALLED: {}: result={}"), Name, result);
            return result;
        }

    public:
        // StrokeKey を処理する
        void handleStrokeKeys(int deckey) override {
            size_t totalCnt = STATE_COMMON->GetTotalDecKeyCount();
            wchar_t myChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
            _LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({}), face={}"), Name, deckey, deckey, myChar);
            if (myChar == SETTINGS->eisuHistSearchChar && is_lower_alphabet(OUTPUT_STACK->back())) {
                // 履歴検索の実行(末尾文字が英小文字でないと発動させない; "CO" の後の場合は、'O' がキーになるが、この場合は発動させない)
                HISTORY_RESIDENT_STATE->handleNextCandTrigger();
                MY_NODE->prevHistSearchDeckeyCount = totalCnt;
            } else if (deckey < NORMAL_DECKEY_NUM || (deckey >= SHIFT_DECKEY_START && deckey < (SHIFT_DECKEY_START + NORMAL_DECKEY_NUM))) {
                STATE_COMMON->AppendOrigString(myChar);

                // キーボードフェイス文字を返す
                _LOG_DEBUGH(_T("SetOutString"));
                STATE_COMMON->SetOutString(myChar, 0);

                //if (myChar >= 'A' && myChar <= 'Z' && STATE_COMMON->GetTotalDecKeyCount() == firstTotalCnt + 1) {
                //    // 2文字目も英大文字だったら、英数モードを終了する
                //    cancelMe();
                //}
                if (myChar >= 'A' && myChar <= 'Z') {
                    MY_NODE->prevCapitalDeckeyCount = totalCnt;
                    if (++capitalCharCnt >= SETTINGS->eisuExitCapitalCharNum) {
                        // N文字続けて英大文字だったら、英数モードを終了する
                        cancelMe();
                    }
                } else {
                    capitalCharCnt = 0;
                }
                _LOG_DEBUGH(_T("capitalCharCnt={}"), capitalCharCnt);
            } else {
                // 通常キーでもシフトキーでもなかった
                setThroughDeckeyFlag();
                cancelMe();
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // Shift飾修されたキー
        void handleShiftKeys(int deckey) override {
            _LOG_DEBUGH(_T("ENTER: {}, deckey={:x}({})"), Name, deckey, deckey);
            handleStrokeKeys(deckey);
            _LOG_DEBUGH(_T("LEAVE: {}"), Name);
        }

        // 先頭文字の小文字化
        void handleEisuDecapitalize() override {
            _LOG_DEBUGH(_T("ENTER: {}"), Name);
            size_t checkCnt = prevLowerHeadCnt + 1;
            prevLowerHeadCnt = STATE_COMMON->GetTotalDecKeyCount();
            if (checkCnt == prevLowerHeadCnt) {
                // 2回続けて呼ばれたらキャンセル
                cancelMe();
            } else {
                //auto romanStr = OUTPUT_STACK->GetLastAsciiKey<MString>(17);
                //if (!romanStr.empty() && romanStr.size() <= 16) {
                //    if (is_upper_alphabet(romanStr[0])) {
                //        romanStr[0] = to_lower(romanStr[0]);
                //        STATE_COMMON->SetOutString(romanStr, romanStr.size());
                //    }
                //}
                HISTORY_RESIDENT_STATE->handleEisuDecapitalize();
            }
            _LOG_DEBUGH(_T("LEAVE: {}"), Name);
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

        // EisuModeのトグル - 処理のキャンセル
        void handleEisuMode() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // handleUndefinedKey ハンドラ - 処理のキャンセル
        void handleUndefinedDeckey(int ) override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            cancelMe();
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

        // Space の処理 -- 処理のキャンセル
        void handleSpaceKey() override {
            size_t totalCnt = STATE_COMMON->GetTotalDecKeyCount();
            size_t exitSpaceNum = SETTINGS->eisuExitSpaceNum;
            _LOG_DEBUGH(_T("CALLED: {}: totalCnt={}, firstCnt={}, prevCnt={}, exitNum={}"), Name, totalCnt, firstSpaceKeyCnt, prevSpaceKeyCnt, exitSpaceNum);
            if (exitSpaceNum <= 1) {
                if (exitSpaceNum == 1) {
                    // Spaceの出力
                    handleStrokeKeys(STROKE_SPACE_DECKEY);
                }
                // 1回のSpaceキー入力で、英数モードを抜ける
                cancelMe();
            } else if (totalCnt == prevSpaceKeyCnt + 1) {
                // Spaceキーが連続入力された
                prevSpaceKeyCnt = totalCnt;
                if (firstSpaceKeyCnt + exitSpaceNum - 1 == totalCnt) {
                    // 英数モードを抜ける回数に到達
                    cancelMe();
                } else {
                    handleStrokeKeys(STROKE_SPACE_DECKEY);
                }
            } else {
                // 直前はSpaceキーでなかった
                firstSpaceKeyCnt = prevSpaceKeyCnt = totalCnt;
                handleStrokeKeys(STROKE_SPACE_DECKEY);
            }
        }

        // EisuCancel - 処理のキャンセル
        void handleEisuCancel() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            cancelMe();
        }

        // CommitState の処理 -- 処理のコミット
        void handleCommitState() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleEisuCancel();
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
            _LOG_DEBUGH(_T("CALLED"));
            STATE_COMMON->AddOrEraseRunningState(Name, 0);  // 削除
            MarkUnnecessary();
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

