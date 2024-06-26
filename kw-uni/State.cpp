// デコーダ状態の基底クラス
//#include "pch.h"
#include "Logger.h"

#include "Constants.h"
#include "State.h"
#include "deckey_id_defs.h"
#include "Node.h"
#include "History/HistoryDic.h"
#include "StrokeHelp.h"
#include "Settings.h"
#include "StrokeTable.h"
#include "Mazegaki/Mazegaki.h"
#include "KeysAndChars/Eisu.h"
#include "KeysAndChars/Katakana.h"
#include "KeysAndChars/Zenkaku.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

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

DEFINE_CLASS_LOGGER(State);

// デストラクタのデフォルト
State::~State() {
    LOG_DEBUG(_T("ENTER: Destructor: {}"), Name);
    DeleteNextState();
    LOG_DEBUG(_T("LEAVE: Destructor: {}"), Name);
}

// 次の状態をセット
State* State::SetNextState(State* p) {
    LOG_DEBUG(_T("ENTER: {}, NextState={}"), Name, p->GetName());
    if (pNext) {
        LOG_WARNH(L"Next State is NOT Empty: {}", pNext->GetName());
    }
    pNext = p;
    if (p) p->pPrev = this;
    // 二重の状態生成処理を避けるために、次のノードをクリアしておく
    ClearNextNodeMaybe();
    LOG_DEBUG(_T("LEAVE: {}: NextNode cleared"), Name);
    return pNext;
}

// 後続状態を削除する
void State::DeleteNextState() {
    LOG_DEBUG(_T("ENTER: {}, NextState={}"), Name, pNext ? pNext->GetName() : L"none");
    delete pNext;
    pNext = nullptr;
    LOG_DEBUG(_T("LEAVE: {}"), Name);
}

// 状態の再アクティブ化 (何かやりたい場合はオーバーライドする)
void State::Reactivate() {
    // デフォルトでは何もしない
    if (pNext) pNext->Reactivate();
}

// DECKEY 処理の前半部
void State::HandleDeckeyChain(int deckey) {
    LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({}), totalCount={}, NextNode={}, outStr={}"),
        Name, deckey, deckey, STATE_COMMON->GetTotalDecKeyCount(), NODE_NAME(NextNodeMaybe()), to_wstr(STATE_COMMON->OutString()));
    // 履歴常駐状態の事前チェック(デフォルトでは何もしない)
    DoHistoryResidentPreCheck();

    // ModalStateの前処理(デフォルトでは何もしない)
    // deckey < 0 で返ってきたら、後続処理をやらない
    deckey = DoModalStatePreProc(deckey);

    ClearNextNodeMaybe();

    _LOG_DEBUGH(_T("NextState={}"), STATE_NAME(pNext));
    if (pNext) {
        _LOG_DEBUGH(_T("NextState: FOUND"));
        // 後続状態があれば、そちらを呼び出す
        pNext->HandleDeckeyChain(deckey);
    } else {
        _LOG_DEBUGH(_T("NextState: NOT FOUND"));
        _LOG_DEBUGH(_T("deckey={}"), deckey);
        // 後続状態がなければ、ここでDECKEYをディスパッチする
        if (deckey >= 0) dispatchDeckey(deckey);
    }

    LOG_DEBUGH(_T("LEAVE: {}, NextNode={}, outStr={}"), Name, NODE_NAME(NextNodeMaybe()), to_wstr(STATE_COMMON->OutString()));
}

// 履歴常駐状態の事前チェック
void State::DoHistoryResidentPreCheck() {
    /* デフォルトでは何もしない */
    _LOG_DEBUGH(_T("CALLED: {}: DEFAULT"), Name);
}

// ModalStateの前処理
int State::DoModalStatePreProc(int deckey) {
    /* デフォルトでは何もしない */
    _LOG_DEBUGH(_T("CALLED: {}: DEFAULT: deckey={}"), Name, deckey);
    return deckey;
}

// DECKEY処理の後半部の処理(非仮想関数)。
// 後続状態の処理チェイン。
void State::DoDeckeyPostProcChain() {
    _LOG_DEBUGH(_T("ENTER: {}, NextNode={}"), Name, NODE_NAME(NextNodeMaybe()));
    if (pNext) {
        // 先に状態チェーンの末尾の方の処理をやる
        pNext->DoDeckeyPostProcChain();
    }
    DoDeckeyPostProc();
    _LOG_DEBUGH(_T("LEAVE: {}, NextNode={}"), Name, NODE_NAME(NextNodeMaybe()));
}

// 不要になった後続状態の削除と、新しい後続状態の生成
void State::DoDeckeyPostProc() {
    _LOG_DEBUGH(_T("ENTER: {}, NextNode={}"), Name, NODE_NAME(NextNodeMaybe()));
    // 不要な後続状態を削除
    DeleteUnnecessarySuccessorState();

    // 新しい後続状態の生成
    while (NextNodeMaybe() && !IsUnnecessary()) {
        _LOG_DEBUGH(_T("PATH-0: NextNodeMaybe={}"), NODE_NAME(NextNodeMaybe()));
        // 新しい後続ノードが生成されており、自身が不要状態でないならば、ここで後続ノードの処理を行う
        // (自身が不要状態ならば、この後、前接状態に戻り、そこで後続ノードが処理される)
        // 後続状態を作成
        State* ps = NextNodeMaybe()->CreateState();
        _LOG_DEBUGH(_T("PATH-A"));
        ClearNextNodeMaybe();       // 新状態を生成したので、親には渡さない。参照をクリアしておく
        _LOG_DEBUGH(_T("PATH-B"));
        // 状態が生成されたときに処理を実行
        // ストロークノード以外は、ここで何らかの出力処理をするはず
        if (ps->DoProcOnCreated()) {
            // 必要があれば後続ノードから生成した新状態をチェインする
            _LOG_DEBUGH(_T("PATH-C"));
            _LOG_DEBUGH(_T("{}: appendSuccessorState: {}"), Name, ps->Name);
            SetNextState(ps);
            ps->pPrev = this;
        } else {
            // 後続状態の生成時処理の結果、後続状態は不要になった
            _LOG_DEBUGH(_T("PATH-D"));
            SetNextNodeMaybe(ps->NextNodeMaybe());   // 新しい後続ノードがあるかもしれないのでここでセットしておく
            _LOG_DEBUGH(_T("NextNodeMaybe={:p}"), (void*)NextNodeMaybe());
            delete ps;  // 不要になった後続状態を削除する
        }
        _LOG_DEBUGH(_T("PATH-E"));
    }
    _LOG_DEBUGH(_T("LEAVE: {}, NextNode={}"), Name, NODE_NAME(NextNodeMaybe()));
}

// 中間チェック
void State::DoIntermediateCheckChain() {
    _LOG_DEBUGH(_T("ENTER: {}"), Name);
    if (pNext) pNext->DoIntermediateCheckChain();
    DoIntermediateCheck();
    _LOG_DEBUGH(_T("LEAVE: {}"), Name);
}

void State::DoIntermediateCheck() {
    // Do nothing in Default
    _LOG_DEBUGH(_T("CALLED: {}: DEFAULT"), Name);
}

// 状態が生成されたときに実行する処理 (その状態をチェインする場合は true を返す)
bool State::DoProcOnCreated() {
    // Do nothing
    _LOG_DEBUGH(_T("CALLED: {}: DEFAULT"), Name);
    return false;
}

// 文字列を変換
MString State::TranslateString(const MString& outStr) {
    return outStr;
}

// 「最終的な出力履歴が整ったところで呼び出される処理」を先に次状態に対して実行する
void State::DoOutStringProcChain() {
    LOG_DEBUGH(_T("ENTER: {}: outStr={}"), Name, to_wstr(STATE_COMMON->OutString()));
    if (pNext) pNext->DoOutStringProcChain();
    if (!STATE_COMMON->IsOutStringProcDone()) DoOutStringProc();
    LOG_DEBUGH(_T("LEAVE: {}: outStr={}"), Name, to_wstr(STATE_COMMON->OutString()));
}

// 最終的な出力履歴が整ったところで呼び出される処理
void State::DoOutStringProc() {
    LOG_DEBUGH(_T("ENTER: {}"), Name);
    // 何もしない
    LOG_DEBUGH(_T("LEAVE: {}"), Name);
}

// ノードから生成した状態を後接させ、その状態を常駐させる
void State::CreateStateAndStayResidentAtEndOfChain(Node* np) {
    _LOG_DEBUGH(_T("ENTER: {}, nextNode: {}"), Name, NODE_NAME(NextNodeMaybe()));
    if (np) {
        if (pNext) {
            pNext->CreateStateAndStayResidentAtEndOfChain(np);
        } else {
            auto ps = np->CreateState();
            ps->DoProcOnCreated();
            LOG_DEBUG(_T("{}: appendSuccessorState: {}"), Name, ps->Name);
            SetNextState(ps);
            ps->pPrev = this;
        }
    }
    _LOG_DEBUGH(_T("LEAVE: {}"), Name);
}

// 居残っている一時状態の削除(デコーダのOFF->ON時に呼ばれる)
void State::DeleteRemainingState() {
    LOG_DEBUG(_T("ENTER: {}: next={}"), Name, (pNext ? pNext->Name : _T("NULL")));
    if (pNext) {
        pNext->DeleteRemainingState();
        if (!pNext->IsResident()) {
            LOG_DEBUG(_T("delete next={}"), pNext->Name);
            DeleteNextState();       // 居残っている一時状態の削除(デコーダのOFF->ON時に呼ばれる)
        }
    }
    LOG_DEBUG(_T("LEAVE: {}"), Name);
}

bool State::IsResident() const {
    LOG_DEBUG(_T("CALLED: false"));
    return false;
}

// 履歴検索を初期化する状態か
bool State::IsHistoryReset() {
    _LOG_DEBUGH(_T("CALLED: {}: True (default)"), Name);
    return true;
}

// 不要フラグをセット
void State::MarkUnnecessary() {
    LOG_DEBUG(_T("CALLED: {}"), Name);
    bUnnecessary = true;
}

// 不要になった状態か
// 必要に応じてオーバーライドすること。
bool State::IsUnnecessary() {
    LOG_DEBUG(_T("CALLED: {}: {}"), Name, bUnnecessary);
    return bUnnecessary;
}

// この状態以降を不要としてマークする
void State::MarkUnnecessaryFromThis() {
    LOG_DEBUGH(_T("CALLED: {}"), Name);
    bUnnecessary = true;
    if (pNext) pNext->MarkUnnecessaryFromThis();
}

// 不要とマークされた後続状態を削除する
void State::DeleteUnnecessarySuccessorState() {
    _LOG_DEBUGH(_T("ENTER: {}"), Name);
    if (pNext) {
        if (pNext->IsUnnecessary()) {
            _LOG_DEBUGH(_T("DELETE NEXT: {}"), pNext->Name);
            // 次ノードがあれば、それを移動しておく
            if (pNext->NextNodeMaybe() && !NextNodeMaybe()) SetNextNodeMaybe(pNext->NextNodeMaybe());
            DeleteNextState();       // 不要とマークされた後続状態を削除する
        }
    }
    _LOG_DEBUGH(_T("LEAVE: {}"), Name);
}

// 入力・変換モード標識を連結して返す
MString State::JoinModeMarker() {
    LOG_DEBUG(_T("CALLED: {}"), Name);
    MString modeMarker;
    JoinModeMarker(modeMarker);
    return modeMarker;
}

void State::JoinModeMarker(MString& modeMarkers) {
    LOG_DEBUG(_T("ENTER: {}, marker={}"), Name, to_wstr(modeMarkers));
    mchar_t mch = GetModeMarker();
    if (mch != 0) modeMarkers.push_back(mch);
    if (pNext) pNext->JoinModeMarker(modeMarkers);
    LOG_DEBUG(_T("LEAVE: {}, marker={}"), Name, to_wstr(modeMarkers));
}

// モード標識文字を返す
mchar_t State::GetModeMarker() {
    return 0;
}

// DECKEY は通常文字ストロークキーか
bool State::isNormalStrokeKey(int deckey) {
    return deckey >= 0 && deckey < NORMAL_DECKEY_NUM;
}

// DECKEY はShift修飾キーか
bool State::isShiftedKey(int deckey) {
    return deckey >= SHIFT_DECKEY_START && deckey < TOTAL_SHIFT_DECKEY_END;
}

// DECKEY はストロークキーとして扱われる機能キーか
bool State::isStrokableFuncKey(int deckey) {
    //return deckey >= FUNC_DECKEY_START && deckey < FUNC_DECKEY_END;
    switch (deckey) {
    case HANZEN_DECKEY:
    case CAPS_DECKEY:
    case ALNUM_DECKEY:
    case NFER_DECKEY:
    case XFER_DECKEY:
    case KANA_DECKEY:
    case RIGHT_SHIFT_DECKEY:
    case INS_DECKEY:
        return true;
    default:
        return false;
    }
}

// DECKEY は同時打鍵Shift修飾キーか
bool State::isComboShiftedKey(int deckey) {
    return deckey >= COMBO_DECKEY_START && deckey < EISU_COMBO_DECKEY_END;
}
// DECKEY はCtrl飾修キーか
bool State::isCtrledKey(int deckey) {
    return deckey >= CTRL_DECKEY_START && deckey < TOTAL_DECKEY_NUM;
}

// DECKEY はストロークキーとして扱われるキーか
bool State::isStrokableKey(int deckey) {
    return isNormalStrokeKey(deckey) || isShiftedKey(deckey) || isStrokableFuncKey(deckey) || isComboShiftedKey(deckey);
        //|| (isShiftedKey(deckey)
        //    && (deckey != SHIFT_SPACE_DECKEY
        //        || (!SETTINGS->histSearchByShiftSpace && SETTINGS->handleShiftSpaceAsNormalSpace)));
}

// ストロークテーブルチェインの長さ(テーブルのレベル)
size_t State::StrokeTableChainLength() {
    size_t len = 0;
    if (pNext) {
        len = pNext->StrokeTableChainLength();
    }
    LOG_DEBUG(_T("LEAVE: {}, len={}"), Name, len);
    return len;
}

//仮想鍵盤にストロークヘルプの情報を設定する
void State::copyStrokeHelpToVkbFaces(wchar_t ch) {
    STATE_COMMON->SetCenterString(ch);
    STATE_COMMON->ClearFaces();
    if (STROKE_HELP->copyStrokeHelpToVkbFacesStateCommon(ch, STATE_COMMON->GetFaces())) {
        STATE_COMMON->SetStrokeHelpVkbLayout();
    } else {
        STATE_COMMON->ClearVkbLayout();
    }
}

//仮想鍵盤にストロークヘルプの情報を設定する(outStringの先頭文字)
void State::copyStrokeHelpToVkbFaces() {
    if (!STATE_COMMON->OutString().empty()) {
        copyStrokeHelpToVkbFaces((wchar_t)STATE_COMMON->GetFirstOutChar());
    }
}

// 入力された DECKEY をディスパッチする
void State::dispatchDeckey(int deckey) {
    _LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({})"), Name, deckey, deckey);
    if (deckey < 0) {
        _LOG_DEBUGH(_T("LEAVE: DO NOTHING: {}: deckey={:x}H({}), outStr={}"), Name, deckey, deckey, to_wstr(STATE_COMMON->OutString()));
        return;
    }
    //pStateResult->Iniitalize();
    if (isNormalStrokeKey(deckey)) {
        if (deckey == STROKE_SPACE_DECKEY) {
            handleSpaceKey();
            _LOG_DEBUGH(_T("LEAVE: {}: SpaceKey handled, outStr={}"), Name, to_wstr(STATE_COMMON->OutString()));
            return;
        }
        handleStrokeKeys(deckey);
    } else if (deckey >= ACTIVE_DECKEY && deckey <= DEACTIVE2_DECKEY) {
        handleDecoderOff();
    } else if (deckey == FULL_ESCAPE_DECKEY) {
        handleFullEscape();
    } else if (deckey == UNBLOCK_DECKEY) {
        handleUnblock();
    } else if (deckey == HISTORY_NEXT_SEARCH_DECKEY) {
        handleNextCandTrigger();
    } else if (deckey == HISTORY_PREV_SEARCH_DECKEY) {
        handlePrevCandTrigger();
    } else if (deckey == TOGGLE_ZENKAKU_CONVERSION_DECKEY) {
        handleZenkakuConversion();
    } else if (deckey == TOGGLE_KATAKANA_CONVERSION_DECKEY) {
        handleKatakanaConversion();
    } else if (deckey == SOFT_ESCAPE_DECKEY) {
        handleEsc();
    } else if (deckey == CLEAR_STROKE_DECKEY) {
        handleClearStroke();
    } else if (deckey == COMMIT_STATE_DECKEY) {
        handleCommitState();
    } else if (deckey == TOGGLE_BLOCKER_DECKEY) {
        handleToggleBlocker();
    } else if (deckey == CANCEL_POST_REWRITE_DECKEY) {
        OUTPUT_STACK->cancelRewritable();
    } else if (deckey == EISU_MODE_TOGGLE_DECKEY) {
        handleEisuMode();
    } else if (deckey == EISU_MODE_CANCEL_DECKEY) {
        handleEisuCancel();
    } else if (deckey == EISU_DECAPITALIZE_DECKEY) {
        handleEisuDecapitalize();
    } else if (deckey == UNDEFINED_DECKEY) {
        handleUndefinedDeckey(deckey);
    } else {
        if (handleFunctionKeys(deckey)) {
            _LOG_DEBUGH(_T("LEAVE: {}: FunctionKey handled, outStr={}"), Name, to_wstr(STATE_COMMON->OutString()));
            return;
        }

        _LOG_DEBUGH(_T("DISPATH FUNCTION KEY: {}: deckey={:x}H({})"), Name, deckey, deckey);
        switch (deckey) {
        case LEFT_TRIANGLE_DECKEY:
            handleLeftTriangle();
            break;
        case RIGHT_TRIANGLE_DECKEY:
            handleRightTriangle();
            break;
        case LEFT_SHIFT_BLOCKER_DECKEY:
        case RIGHT_SHIFT_BLOCKER_DECKEY:
        case LEFT_SHIFT_MAZE_START_POS_DECKEY:
        case RIGHT_SHIFT_MAZE_START_POS_DECKEY:
            handleLeftRightMazeShift(deckey);
            break;
        case QUESTION_DECKEY:
            handleQuestion();
            break;
        //case SHIFT_SPACE_DECKEY:
        //    //handleShiftSpace();
        //    handleShiftKeys(deckey);
        //    break;
        case ENTER_DECKEY:
            handleEnter();
            _LOG_DEBUGH(_T("{}: Enter Key handled"), Name);
            break;
        case ESC_DECKEY:
            handleEsc();
            break;
        case BS_DECKEY:
            handleBS();
            break;
        case TAB_DECKEY:
            handleTab();
            break;
        case SHIFT_TAB_DECKEY:
            handleShiftTab();
            break;
        case DEL_DECKEY:
            handleDelete();
            break;
        case HOME_DECKEY:
            handleHome();
            break;
        case END_DECKEY:
            handleEnd();
            break;
        case PAGE_UP_DECKEY:
            handlePageUp();
            break;
        case PAGE_DOWN_DECKEY:
            handlePageDown();
            break;
        case LEFT_ARROW_DECKEY:
            handleLeftArrow();
            break;
        case RIGHT_ARROW_DECKEY:
            handleRightArrow();
            break;
        case UP_ARROW_DECKEY:
            handleUpArrow();
            break;
        case DOWN_ARROW_DECKEY:
            handleDownArrow();
            break;
        case CTRL_LEFT_ARROW_DECKEY:
            handleCtrlLeftArrow();
            break;
        case CTRL_RIGHT_ARROW_DECKEY:
            handleCtrlRightArrow();
            break;
        case CTRL_UP_ARROW_DECKEY:
            handleCtrlLeftArrow();
            break;
        case CTRL_DOWN_ARROW_DECKEY:
            handleCtrlRightArrow();
            break;
        //case CTRL_SPACE_DECKEY:
        //    handleCtrlSpace();
        //    break;
        //case CTRL_SHIFT_SPACE_DECKEY:
        //    handleCtrlShiftSpace();
        //    break;
        default:
            if (isShiftedKey(deckey)) {
                _LOG_DEBUGH(_T("SHIFTED: {}: deckey={:x}H({})"), Name, deckey, deckey);
                handleShiftKeys(deckey);
                break;
            } else if (isCtrledKey(deckey)) {
                _LOG_DEBUGH(_T("CTRLKEY: {}: deckey={:x}H({})"), Name, deckey, deckey);
                handleCtrlKeys(deckey);
            } else if (deckey >= 0) {
                _LOG_DEBUGH(_T("DEFAULT: {}: deckey={:x}H({})"), Name, deckey, deckey);
                // 半全, 英数/Caps, 無変換, 変換, ひらがな
                handleStrokeKeys(deckey);
            } else {
                _LOG_DEBUGH(_T("DO NOTHING: {}: deckey={:x}H({})"), Name, deckey, deckey);
            }
            break;
        }
    }

    if (isThroughDeckey()) handleUndefinedDeckey(deckey);

    _LOG_DEBUGH(_T("LEAVE: {}: deckey={:x}H({}), outStr={}"), Name, deckey, deckey, to_wstr(STATE_COMMON->OutString()));
}

//-----------------------------------------------------------------------
// ストロークキーデフォルトハンドラ
void State::handleStrokeKeys(int _DEBUG_SENT(hk)) {
    _LOG_DEBUGH(_T("DO NOTHING: setThroughDeckeyFlag: deckey={:x}H({})"), hk, hk);
    setThroughDeckeyFlag();
}

// スペースキーハンドラ
void State::handleSpaceKey() { _LOG_DEBUGH(_T("CALLED")); handleStrokeKeys(STROKE_SPACE_DECKEY); }

//-----------------------------------------------------------------------
// 特殊キーデフォルトハンドラ
void State::handleSpecialKeys(int /*deckey*/) {
    LOG_INFO(_T("THROUGH"));
    setThroughDeckeyFlag();
}

// FullEscape デフォルトハンドラ
void State::handleFullEscape() { LOG_INFO(_T("CALLED")); handleSpecialKeys(FULL_ESCAPE_DECKEY); }

// Unblock デフォルトハンドラ
void State::handleUnblock() { LOG_INFO(_T("CALLED")); handleSpecialKeys(UNBLOCK_DECKEY); }

// handleNextCandTrigger デフォルトハンドラ
void State::handleNextCandTrigger() { LOG_INFO(_T("CALLED")); handleSpecialKeys(HISTORY_NEXT_SEARCH_DECKEY); }

// handlePrevCandTrigger デフォルトハンドラ
void State::handlePrevCandTrigger() { LOG_INFO(_T("CALLED")); handleSpecialKeys(HISTORY_PREV_SEARCH_DECKEY); }

// handleZenkakuConversion デフォルトハンドラ
void State::handleZenkakuConversion() { LOG_INFO(_T("CALLED")); handleSpecialKeys(TOGGLE_ZENKAKU_CONVERSION_DECKEY); }

// handleKatakanaConversion デフォルトハンドラ
void State::handleKatakanaConversion() { LOG_INFO(_T("CALLED")); handleSpecialKeys(TOGGLE_KATAKANA_CONVERSION_DECKEY); }

// handleEisuMode デフォルトハンドラ
void State::handleEisuMode() { LOG_INFO(_T("CALLED")); handleSpecialKeys(EISU_MODE_TOGGLE_DECKEY); }

// handleEisuCancel デフォルトハンドラ
void State::handleEisuCancel() {
    LOG_INFO(_T("DO NOTHING"));
    if (pNext) {
        pNext->handleEisuCancel();
    }
}

// handleEisuDecapitalize デフォルトハンドラ
void State::handleEisuDecapitalize() { LOG_INFO(_T("DO NOTHING")); }

// handleClearStroke デフォルトハンドラ
void State::handleClearStroke() { LOG_INFO(_T("DO NOTHING")); }

// handleCommitState デフォルトハンドラ
void State::handleCommitState() { LOG_INFO(_T("DO NOTHING")); }

// handleToggleBlocker デフォルトハンドラ
void State::handleToggleBlocker() {
    LOG_INFO(_T("CALLED"));
    // ブロッカーをセット/リセットする
    OUTPUT_STACK->toggleLastBlocker();
}

// handleUndefinedKey デフォルトハンドラ
void State::handleUndefinedDeckey(int ) {
    // 何もしない
    LOG_INFO(_T("DO NOTHING"));
}

//-----------------------------------------------------------------------
// DecoderOff デフォルトハンドラ
void State::handleDecoderOff() { LOG_INFO(_T("CALLED")); }

//-----------------------------------------------------------------------
// 機能キー前処理ハンドラ
// 一括で何かをしたい場合にオーバーライドする。その後、個々の処理を続ける場合は、 false を返すこと
bool State::handleFunctionKeys(int
_DEBUG_SENT(hk)
) {
    _LOG_DEBUGH(_T("DO NOTHING: deckey={:x}H({})"), hk, hk);
    return false;
}

//-----------------------------------------------------------------------
// Ctrlキー デフォルトハンドラ
void State::handleCtrlKeys(int /*deckey*/) { LOG_INFO(_T("CALLED")); setThroughDeckeyFlag(); }

//-----------------------------------------------------------------------
// Shiftキー デフォルトハンドラ
void State::handleShiftKeys(int /*deckey*/) { LOG_INFO(_T("DEFAULT")); STATE_COMMON->OutputDeckeyChar(); }

// < ハンドラ
void State::handleLeftTriangle() { handleShiftKeys(LEFT_TRIANGLE_DECKEY); }

// > ハンドラ
void State::handleRightTriangle() { handleShiftKeys(RIGHT_TRIANGLE_DECKEY); }

// ? ハンドラ
void State::handleQuestion() { handleShiftKeys(QUESTION_DECKEY); }

// left/right maze shift keys
void State::handleLeftRightMazeShift(int
#if defined(_DEBUG)
    deckey
#endif
) {
#if defined(_DEBUG)
    LOG_DEBUGH(_T("CALLED: deckey={:x}H({})"), deckey, deckey);
#endif
}

//-----------------------------------------------------------------------
//// Shift+Space ハンドラ
//// isStrokeKeyOrShiftedKey() にも注意すること
//void State::handleShiftSpace() {
//    _LOG_DEBUGH(_T("Shift+Space"));
//    if (SETTINGS->histSearchByShiftSpace) {
//        handleNextCandTrigger();
//    } else if (SETTINGS->handleShiftSpaceAsNormalSpace) {
//        handleShiftSpaceAsNormalSpace();
//    } else {
//        handleSpecialKeys(SHIFT_SPACE_DECKEY);
//    }
//}

//void State::handleShiftSpaceAsNormalSpace() {
//    _LOG_DEBUGH(_T("ShiftSpaceAsNormalSpace"));
//    STATE_COMMON->SetOutString(' ');
//}

//// Ctrl+Space ハンドラ
////void State::handleCtrlSpace() { LOG_DEBUG(_T("Ctrl+Space")); handleSpecialKeys(CTRL_SPACE_DECKEY);}
//void State::handleCtrlSpace() {
//    _LOG_DEBUGH(_T("Ctrl+Space"));
//    if (SETTINGS->histSearchByCtrlSpace) {
//        handleNextCandTrigger();
//    } else {
//        handleSpecialKeys(CTRL_SPACE_DECKEY);
//    }
//}

// Ctrl+Shift+Space ハンドラ
//void State::handleCtrlShiftSpace() { LOG_DEBUG(_T("Ctrl+Shift+Space")); handleSpecialKeys(CTRL_SHIFT_SPACE_DECKEY);}
//void State::handleCtrlShiftSpace() {
//    _LOG_DEBUGH(_T("Ctrl+Shift+Space"));
//    if (SETTINGS->histSearchByCtrlSpace || SETTINGS->histSearchByShiftSpace) {
//        handlePrevCandTrigger();
//    } else {
//        handleSpecialKeys(CTRL_SHIFT_SPACE_DECKEY);
//    }
//}

// RET/Enter ハンドラ
void State::handleEnter() {
    _LOG_DEBUGH(_T("{}: Enter"), Name);
    STATE_COMMON->SetAppendBackspaceStopperFlag();
    handleSpecialKeys(ENTER_DECKEY);
}

// ESC ハンドラ
void State::handleEsc() {
    _LOG_DEBUGH(_T("{}: Esc: currentDeckey={}"), Name, STATE_COMMON->CurrentDecKey());
    if (STATE_COMMON->CurrentDecKey() == ESC_DECKEY) handleSpecialKeys(ESC_DECKEY);
}
    
// BS ハンドラ
void State::handleBS() { _LOG_DEBUGH(_T("BackSpace")); setCharDeleteInfo(1); }

// TAB ハンドラ
void State::handleTab() { _LOG_DEBUGH(_T("Tab")); OUTPUT_STACK->setLastBlocker(); handleSpecialKeys(TAB_DECKEY); }

// Shift+TAB ハンドラ
void State::handleShiftTab() { _LOG_DEBUGH(_T("Shift+Tab")); OUTPUT_STACK->setLastBlocker(); handleSpecialKeys(SHIFT_TAB_DECKEY); }

// Delete ハンドラ
void State::handleDelete() { handleSpecialKeys(DEL_DECKEY); }

// Home ハンドラ
void State::handleHome() { handleSpecialKeys(HOME_DECKEY); }

// End ハンドラ
void State::handleEnd() { handleSpecialKeys(END_DECKEY); }

// PageUp ハンドラ
void State::handlePageUp() { handleSpecialKeys(PAGE_UP_DECKEY); }

// PageDown ハンドラ
void State::handlePageDown() { handleSpecialKeys(PAGE_DOWN_DECKEY); }

// ← ハンドラ
void State::handleLeftArrow() { handleSpecialKeys(LEFT_ARROW_DECKEY); }

// Ctrl ← ハンドラ
void State::handleCtrlLeftArrow() { handleSpecialKeys(LEFT_ARROW_DECKEY); }

// → ハンドラ
void State::handleRightArrow() { handleSpecialKeys(RIGHT_ARROW_DECKEY); }

// Ctrl → ハンドラ
void State::handleCtrlRightArrow() { handleSpecialKeys(RIGHT_ARROW_DECKEY); }

// ↑ ハンドラ
void State::handleUpArrow() { handleSpecialKeys(UP_ARROW_DECKEY); }

// Ctrl ↑ ハンドラ
void State::handleCtrlUpArrow() { handleSpecialKeys(UP_ARROW_DECKEY); }

// ↓ ハンドラ
void State::handleDownArrow() { handleSpecialKeys(DOWN_ARROW_DECKEY); }

// Ctrl ↓ ハンドラ
void State::handleCtrlDownArrow() { handleSpecialKeys(DOWN_ARROW_DECKEY); }

