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

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

DEFINE_CLASS_LOGGER(State);

#define NAME_PTR    Name.c_str()

// デストラクタのデフォルト
State::~State() {
    LOG_DEBUG(_T("ENTER: Destructor: %s"), NAME_PTR);
    // 後続状態を削除する
    delete pNext;
    pNext = nullptr;
    LOG_DEBUG(_T("LEAVE: Destructor: %s"), NAME_PTR);
}

// 状態の再アクティブ化 (何かやりたい場合はオーバーライドする)
void State::Reactivate() {
    // デフォルトでは何もしない
    if (pNext) pNext->Reactivate();
}

// DECKEY 処理の流れ
// 新ノードが未処理の場合は、ここで NULL 以外が返されるので、親状態で処理する
void State::HandleDeckey(int deckey) {
    _LOG_DEBUGH(_T("ENTER: %s: deckey=%xH(%d), NextNode=%s"), NAME_PTR, deckey, deckey, NODE_NAME_PTR(NextNodeMaybe()));
    // 事前チェック
    DoPreCheck();
    // 前処理
    DoDeckeyPreProc(deckey);
    // 中間チェック
    DoIntermediateCheck();
    // 後処理
    DoDeckeyPostProc();
    _LOG_DEBUGH(_T("LEAVE: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(NextNodeMaybe()));
    //return pNextNodeMaybe;
}

// DECKEY処理の前半部のデフォルト処理。
// 後続状態があればそちらに移譲。なければここでホットキーをディスパッチ。
// 自前で DECKEY 処理をやる場合にはオーバーライドしてもよい
void State::DoDeckeyPreProc(int deckey) {
    _LOG_DEBUGH(_T("ENTER: %s: deckey=%xH(%d), NextNode=%s"), NAME_PTR, deckey, deckey, NODE_NAME_PTR(NextNodeMaybe()));
    if (IsModeState()) {
        // モード状態(HistoryStayState や TranslationState など)のための前処理
        // まだ後続状態が無く、自身が StrokeState ではなく、deckey はストロークキーである場合は、ルートストローク状態を生成して後続させる
        if (!pNext) {
            if ((!pNode || !pNode->isStrokeTableNode()) && isStrokeKeyOrShiftedKeyOrModeFuncKey(deckey)) {
                LOG_DEBUGH(_T("CREATE: RootStrokeNode"));
                pNext = ROOT_STROKE_NODE->CreateState();
                pNext->SetPrevState(this);
            }
        }
    }
    //pNextNodeMaybe = nullptr;
    ClearNextNodeMaybe();
    if (pNext) {
        // 後続状態があれば、そちらを呼び出す ⇒ 新しい後続ノードがあればそれを一時的に記憶しておく(後半部で処理する)
        //pNextNodeMaybe = pNext->HandleDeckey(deckey);
        pNext->HandleDeckey(deckey);
        SetNextNodeMaybe(pNext->NextNodeMaybe());
    } else {
        // 後続状態がなければ、ここでDECKEYをディスパッチする
        dispatchDeckey(deckey);
    }
    _LOG_DEBUGH(_T("LEAVE: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(NextNodeMaybe()));
}

// DECKEY処理の後半部のデフォルト処理。
// 不要になった後続状態の削除と、新しい後続状態の生成とチェイン。
// 自前で DECKEY 処理をやる場合にはオーバーライドしてもよい
void State::DoDeckeyPostProc() {
    _LOG_DEBUGH(_T("ENTER: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(NextNodeMaybe()));
    // 後続状態チェインに対して事後チェック
    DoPostCheckChain();
    //// 不要な後続状態を削除
    //DeleteUnnecessarySuccessorState();
    if (NextNodeMaybe() && !IsUnnecessary()) {
        // 新しい後続ノードが生成されており、自身が不要状態でないならば、ここで後続ノードの処理を行う
        // (自身が不要状態ならば、この後、前接状態に戻り、そこで後続ノードが処理される)
        LOG_DEBUG(_T("nextNode: %s"), NODE_NAME_PTR(NextNodeMaybe()));
        // 後続状態を作成
        State* ps = NextNodeMaybe()->CreateState();
        // 状態が生成されたときに処理を実行
        // ストロークノード以外は、ここで何らかの出力処理をするはず
        if (ps->DoProcOnCreated()) {
            // 必要があれば後続ノードから生成した新状態をチェインする
            LOG_DEBUG(_T("%s: appendSuccessorState: %s"), NAME_PTR, ps->NAME_PTR);
            pNext = ps;
            ps->pPrev = this;
        } else {
            delete ps;
        }
        //pNextNodeMaybe = nullptr;   // 新ノードを処理したので、親には渡さない。参照をクリアしておく
        ClearNextNodeMaybe();       // 新ノードを処理したので、親には渡さない。参照をクリアしておく
    }
    _LOG_DEBUGH(_T("LEAVE: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(NextNodeMaybe()));
}

// 後続状態チェインに対して事後チェック
void State::DoPostCheckChain() {
    _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
    if (pNext) {
        pNext->DoPostCheckChain();
        CheckNextState();
        DeleteUnnecessarySuccessorState();
    } else if (_LOG_DEBUGH_FLAG) {
        _LOG_DEBUGH(_T("STOP: %s"), NAME_PTR);
    }
    _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
}

// 状態が生成されたときに実行する処理 (その状態をチェインする場合は true を返す)
bool State::DoProcOnCreated() {
    // Do nothing
    LOG_DEBUG(_T("CALLED: %s: DEFAULT"), NAME_PTR);
    return false;
}

// 文字列を変換
MString State::TranslateString(const MString& outStr) {
    return outStr;
}

// 「最終的な出力履歴が整ったところで呼び出される処理」を先に次状態に対して実行する
void State::DoOutStringProcChain() {
    LOG_DEBUG(_T("ENTER: %s"), NAME_PTR);
    if (pNext) pNext->DoOutStringProcChain();
    if (!STATE_COMMON->IsOutStringProcDone()) DoOutStringProc();
    LOG_DEBUG(_T("LEAVE: %s"), NAME_PTR);
}

// 最終的な出力履歴が整ったところで呼び出される処理
void State::DoOutStringProc() {
    LOG_DEBUG(_T("ENTER: %s"), NAME_PTR);
    // 何もしない
    LOG_DEBUG(_T("LEAVE: %s"), NAME_PTR);
}

// ノードから生成した状態を後接させ、その状態を常駐させる
void State::ChainAndStay(Node* np) {
    _LOG_DEBUGH(_T("ENTER: %s, nextNode: %s"), NAME_PTR, NODE_NAME_PTR(NextNodeMaybe()));
    if (np) {
        if (pNext) {
            pNext->ChainAndStay(np);
        } else {
            auto ps = np->CreateState();
            ps->DoProcOnCreated();
            LOG_DEBUG(_T("%s: appendSuccessorState: %s"), NAME_PTR, ps->NAME_PTR);
            pNext = ps;
            ps->pPrev = this;
        }
    }
    _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
}

// 居残っている一時状態の削除(デコーダのOFF->ON時に呼ばれる)
void State::DeleteRemainingState() {
    LOG_DEBUG(_T("ENTER: %s: next=%s"), NAME_PTR, (pNext ? pNext->NAME_PTR : _T("NULL")));
    if (pNext) {
        pNext->DeleteRemainingState();
        if (!pNext->IsStay()) {
            LOG_DEBUG(_T("delete next=%s"), pNext->NAME_PTR);
            delete pNext;
            pNext = 0;
        }
    }
    LOG_DEBUG(_T("LEAVE: %s"), NAME_PTR);
}

bool State::IsStay() const {
    LOG_DEBUG(_T("CALLED: false"));
    return false;
}

#define UNNECESSARY_PTR (utils::boolToString(bUnnecessary).c_str())

// 不要になった状態か
// 必要に応じてオーバーライドすること。
bool State::IsUnnecessary() {
    LOG_DEBUG(_T("CALLED: %s: %s"), NAME_PTR, UNNECESSARY_PTR);
    return bUnnecessary;
}

// この状態以降を不要としてマークする
void State::MarkUnnecessaryFromThis() {
    LOG_INFO(_T("CALLED: %s"), NAME_PTR);
    bUnnecessary = true;
    if (pNext) pNext->MarkUnnecessaryFromThis();
}

// 次状態をチェックして、自身の状態を変更させるのに使う。DECKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
void State::CheckNextState() {
    LOG_DEBUG(_T("CALLED: %s: false"), NAME_PTR);
}

//// ストローク機能をすべて削除するか
//bool State::IsToRemoveAllStroke() const {
//    LOG_DEBUG(_T("CALLED: %s: false"), NAME_PTR);
//    return false;
//}

// 不要とマークされた後続状態を削除する
void State::DeleteUnnecessarySuccessorState() {
    _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
    if (pNext) {
        if (pNext->IsUnnecessary()) {
            _LOG_DEBUGH(_T("DELTE NEXT: %s"), pNext->Name.c_str());
            delete pNext;
            pNext = nullptr;
        }
    }
    _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
}

// 入力・変換モード標識を連結して返す
MString State::JoinModeMarker() {
    LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
    MString modeMarker;
    JoinModeMarker(modeMarker);
    return modeMarker;
}

void State::JoinModeMarker(MString& modeMarkers) {
    LOG_DEBUG(_T("ENTER: %s, marker=%s"), NAME_PTR, MAKE_WPTR(modeMarkers));
    mchar_t mch = GetModeMarker();
    if (mch != 0) modeMarkers.push_back(mch);
    if (pNext) pNext->JoinModeMarker(modeMarkers);
    LOG_DEBUG(_T("LEAVE: %s, marker=%s"), NAME_PTR, MAKE_WPTR(modeMarkers));
}

// モード標識文字を返す
mchar_t State::GetModeMarker() {
    return 0;
}

// DECKEY はストロークキーか
bool State::isStrokeKey(int deckey) {
    return deckey >= 0 && deckey < NORMAL_DECKEY_NUM;
}

// DECKEY はShift修飾キーか
bool State::isShiftedKey(int deckey) {
    return deckey >= SHIFT_DECKEY_START && deckey < SHIFT_DECKEY_END;
}

// DECKEY はモード機能キーか
bool State::isModeFuncKey(int deckey) {
    switch (deckey) {
    case HANZEN_DECKEY:
    case CAPS_DECKEY:
    case ALNUM_DECKEY:
    case NFER_DECKEY:
    case XFER_DECKEY:
    case KANA_DECKEY:
        return true;
    default:
        return false;
    }
    //return deckey >= FUNC_DECKEY_START && deckey < FUNC_DECKEY_END;
}

// DECKEY はCtrl飾修キーか
bool State::isCtrledKey(int deckey) {
    return deckey >= CTRL_DECKEY_START && deckey < TOTAL_DECKEY_NUM;
}

// DECKEY はストロークキーまたはShift修飾かまたは機能キーか
bool State::isStrokeKeyOrShiftedKeyOrModeFuncKey(int deckey) {
    return isStrokeKey(deckey) || isShiftedKey(deckey) || isModeFuncKey(deckey);
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
    LOG_DEBUG(_T("LEAVE: %s, len=%d"), NAME_PTR, len);
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
    _LOG_DEBUGH(_T("CALLED: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
    //pStateResult->Iniitalize();
    if (isStrokeKey(deckey)) {
        if (deckey == STROKE_SPACE_DECKEY) {
            handleSpaceKey();
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
    } else {
        if (handleFunctionKeys(deckey)) return;

        switch (deckey) {
        case LEFT_TRIANGLE_DECKEY:
            handleLeftTriangle();
            break;
        case RIGHT_TRIANGLE_DECKEY:
            handleRightTriangle();
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
                handleShiftKeys(deckey);
                break;
            } else if (isCtrledKey(deckey)) {
                handleCtrlKeys(deckey);
            } else {
                // 半全, 英数/Caps, 無変換, 変換, ひらがな
                handleStrokeKeys(deckey);
            }
            break;
        }
    }
}

//-----------------------------------------------------------------------
// ストロークキーデフォルトハンドラ
void State::handleStrokeKeys(int hk) { LOG_INFO(_T("CALLED: deckey=%xH(%d)"), hk, hk); }

// スペースキーハンドラ
void State::handleSpaceKey() { LOG_INFO(_T("CALLED")); handleStrokeKeys(STROKE_SPACE_DECKEY); }

//-----------------------------------------------------------------------
// 特殊キーデフォルトハンドラ
void State::handleSpecialKeys(int /*deckey*/) { setThroughDeckeyFlag(); }

// FullEscape デフォルトハンドラ
void State::handleFullEscape() { LOG_INFOH(_T("CALLED")); handleSpecialKeys(FULL_ESCAPE_DECKEY); }

// Unblock デフォルトハンドラ
void State::handleUnblock() { LOG_INFOH(_T("CALLED")); handleSpecialKeys(UNBLOCK_DECKEY); }

// handleNextCandTrigger デフォルトハンドラ
void State::handleNextCandTrigger() { LOG_INFOH(_T("CALLED")); handleSpecialKeys(HISTORY_NEXT_SEARCH_DECKEY); }

// handlePrevCandTrigger デフォルトハンドラ
void State::handlePrevCandTrigger() { LOG_INFOH(_T("CALLED")); handleSpecialKeys(HISTORY_PREV_SEARCH_DECKEY); }

//-----------------------------------------------------------------------
// DecoderOff デフォルトハンドラ
void State::handleDecoderOff() { LOG_INFOH(_T("CALLED")); }

//-----------------------------------------------------------------------
// 機能キー前処理ハンドラ
// 一括で何かをしたい場合にオーバーライドする。その後、個々の処理を続ける場合は、 false を返すこと
bool State::handleFunctionKeys(int
#ifdef _DEBUG
    hk
#endif
) {
    _LOG_DEBUGH(_T("CALLED: deckey=%xH(%d)"), hk, hk);
    return false;
}

//-----------------------------------------------------------------------
// Ctrlキー デフォルトハンドラ
void State::handleCtrlKeys(int /*deckey*/) { setThroughDeckeyFlag(); }

//-----------------------------------------------------------------------
// Shiftキー デフォルトハンドラ
void State::handleShiftKeys(int /*deckey*/) { STATE_COMMON->OutputDeckeyChar(); }

// < ハンドラ
void State::handleLeftTriangle() { handleShiftKeys(LEFT_TRIANGLE_DECKEY); }

// > ハンドラ
void State::handleRightTriangle() { handleShiftKeys(RIGHT_TRIANGLE_DECKEY); }

// ? ハンドラ
void State::handleQuestion() { handleShiftKeys(QUESTION_DECKEY); }

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
    LOG_DEBUG(_T("Enter"));
    STATE_COMMON->SetAppendBackspaceStopperFlag();
    handleSpecialKeys(ENTER_DECKEY);
}

// ESC ハンドラ
void State::handleEsc() { LOG_DEBUG(_T("Esc")); handleSpecialKeys(ESC_DECKEY); }
    
// BS ハンドラ
void State::handleBS() { LOG_DEBUG(_T("BackSpace")); setCharDeleteInfo(1); }

// TAB ハンドラ
void State::handleTab() { handleSpecialKeys(TAB_DECKEY); }

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

