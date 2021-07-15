// デコーダ状態の基底クラス
//#include "pch.h"
#include "Logger.h"

#include "Constants.h"
#include "State.h"
#include "hotkey_id_defs.h"
#include "Node.h"
#include "History/HistoryDic.h"
#include "StrokeHelp.h"
#include "Settings.h"

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

// HOTKEY 処理の流れ
// 新ノードが未処理の場合は、ここで NULL 以外が返されるので、親状態で処理する
Node* State::HandleHotkey(int hotkey) {
    _LOG_DEBUGH(_T("ENTER: %s: hotkey=%xH(%d), NextNode=%s"), NAME_PTR, hotkey, hotkey, NODE_NAME_PTR(TemporaryNextNode()));
    // 前処理
    DoHotkeyPreProc(hotkey);
    // 後処理
    DoHotkeyPostProc();
    _LOG_DEBUGH(_T("LEAVE: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(TemporaryNextNode()));
    return pTemporaryNextNode;
}

// HOTKEY処理の前半部のデフォルト処理。
// 後続状態があればそちらに移譲。なければここでホットキーをディスパッチ。
// 自前で HOTKEY 処理をやる場合にはオーバーライドしてもよい
void State::DoHotkeyPreProc(int hotkey) {
    _LOG_DEBUGH(_T("ENTER: %s: hotkey=%xH(%d), NextNode=%s"), NAME_PTR, hotkey, hotkey, NODE_NAME_PTR(TemporaryNextNode()));
    pTemporaryNextNode = nullptr;
    if (pNext) {
        // 後続状態があれば、そちらを呼び出す ⇒ 新しい後続ノードが生成されたらそれを一時的に記憶しておく(後半部で処理する)
        pTemporaryNextNode = pNext->HandleHotkey(hotkey);
    } else {
        // 後続状態がなく、ストロークキー以外であれば、ここでHOTKEYをディスパッチする
        dispatchHotkey(hotkey);
    }
    _LOG_DEBUGH(_T("LEAVE: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(TemporaryNextNode()));
}

// HOTKEY処理の後半部のデフォルト処理。
// 不要になった後続状態の削除と、新しい後続状態の生成とチェイン。
// 自前で HOTKEY 処理をやる場合にはオーバーライドしてもよい
void State::DoHotkeyPostProc() {
    _LOG_DEBUGH(_T("ENTER: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(TemporaryNextNode()));
    // 不要な後続状態を削除
    DeleteUnnecessarySuccessorState();
    if (pTemporaryNextNode && !IsUnnecessary()) {
        // 新しい後続ノードが生成されており、自身が不要状態でない
        LOG_DEBUG(_T("nextNode: %s"), NODE_NAME_PTR(pTemporaryNextNode));
        // 後続状態を作成
        auto ps = pTemporaryNextNode->CreateState();
        // 状態が生成されたときに処理を実行
        // ストロークノード以外は、ここで何らかの出力処理をするはず
        if (ps->DoProcOnCreated()) {
            // 必要があれば後続ノードから生成した新状態をチェインする
            LOG_DEBUG(_T("%s: appendSuccessorState: %s"), NAME_PTR, ps->NAME_PTR);
            pNext = ps;
            ps->pPrev = this;
        }
        pTemporaryNextNode = nullptr;   // 新ノードを処理したので、親には渡さない。参照をクリアしておく
    }
    _LOG_DEBUGH(_T("LEAVE: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(TemporaryNextNode()));
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
    _LOG_DEBUGH(_T("ENTER: %s, nextNode: %s"), NAME_PTR, NODE_NAME_PTR(pTemporaryNextNode));
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

// 次状態をチェックして、自身の状態を変更させるのに使う。HOTKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
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
    LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
    if (pNext) {
        pNext->DeleteUnnecessarySuccessorState();
        CheckNextState();
        if (pNext->IsUnnecessary()) {
            delete pNext;
            pNext = nullptr;
        }
    }
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

// HOTKEY はストロークキーか
bool State::isStrokeKey(int hotkey) {
    return hotkey >= 0 && hotkey < NUM_STROKE_HOTKEY;
}

// HOTKEY はShift飾修キーか
bool State::isShiftedKey(int hotkey) {
    return hotkey >= SHIFT_FUNC_HOTKEY_ID_BASE && hotkey < FUNCTIONAL_HOTKEY_ID_BASE;
}

// HOTKEY はストロークキーまたはShift飾修か
bool State::isStrokeKeyOrShiftedKey(int hotkey) {
    return isStrokeKey(hotkey)
        || (isShiftedKey(hotkey)
            && (hotkey != SHIFT_SPACE_HOTKEY
                || (!SETTINGS->histSearchByShiftSpace && SETTINGS->handleShiftSpaceAsNormalSpace)));
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

// 入力された HOTKEY をディスパッチする
void State::dispatchHotkey(int hotkey) {
    _LOG_DEBUGH(_T("CALLED: %s: hotkey=%xH(%d)"), NAME_PTR, hotkey, hotkey);
    //pStateResult->Iniitalize();
    if (isStrokeKey(hotkey)) {
        if (hotkey == HOTKEY_STROKE_SPACE) {
            handleSpaceKey();
            return;
        }
        handleStrokeKeys(hotkey);
    } else if (hotkey >= ACTIVE_HOTKEY && hotkey <= INACTIVE2_HOTKEY) {
        handleDecoderOff();
    } else if (hotkey == FULL_ESCAPE_HOTKEY) {
        handleFullEscape();
    } else if (hotkey == UNBLOCK_HOTKEY) {
        handleUnblock();
    } else if (hotkey == NEXT_CAND_TRIGGER_HOTKEY) {
        handleNextCandTrigger();
    } else if (hotkey == PREV_CAND_TRIGGER_HOTKEY) {
        handlePrevCandTrigger();
    } else {
        if (handleFunctionKeys(hotkey)) return;

        if (isShiftedKey(hotkey)) {
            switch (hotkey) {
            case LEFT_TRIANGLE_HOTKEY:
                handleLeftTriangle();
                break;
            case RIGHT_TRIANGLE_HOTKEY:
                handleRightTriangle();
                break;
            case QUESTION_HOTKEY:
                handleQuestion();
                break;
            case SHIFT_SPACE_HOTKEY:
                handleShiftSpace();
                break;
            default:
                handleShiftKeys(hotkey);
                break;
            }
        } else if (hotkey >= CTRL_FUNC_HOTKEY_ID_BASE && hotkey <= CTRL_FUNC_HOTKEY_ID_END) {
            switch (hotkey) {
            case CTRL_A_HOTKEY:
                handleCtrlA();
                break;
            case CTRL_B_HOTKEY:
                handleCtrlB();
                break;
            case CTRL_C_HOTKEY:
                handleCtrlC();
                break;
            case CTRL_D_HOTKEY:
                handleCtrlD();
                break;
            case CTRL_E_HOTKEY:
                handleCtrlE();
                break;
            case CTRL_F_HOTKEY:
                handleCtrlF();
                break;
            case CTRL_G_HOTKEY:
                handleCtrlG();
                break;
            case CTRL_H_HOTKEY:
                handleCtrlH();
                break;
            case CTRL_I_HOTKEY:
                handleCtrlI();
                break;
            case CTRL_J_HOTKEY:
                handleCtrlJ();
                break;
            case CTRL_K_HOTKEY:
                handleCtrlK();
                break;
            case CTRL_L_HOTKEY:
                handleCtrlL();
                break;
            case CTRL_M_HOTKEY:
                handleCtrlM();
                break;
            case CTRL_N_HOTKEY:
                handleCtrlN();
                break;
            case CTRL_O_HOTKEY:
                handleCtrlO();
                break;
            case CTRL_P_HOTKEY:
                handleCtrlP();
                break;
            case CTRL_Q_HOTKEY:
                handleCtrlQ();
                break;
            case CTRL_R_HOTKEY:
                handleCtrlR();
                break;
            case CTRL_S_HOTKEY:
                handleCtrlS();
                break;
            case CTRL_T_HOTKEY:
                handleCtrlT();
                break;
            case CTRL_U_HOTKEY:
                handleCtrlU();
                break;
            case CTRL_V_HOTKEY:
                handleCtrlV();
                break;
            case CTRL_W_HOTKEY:
                handleCtrlW();
                break;
            case CTRL_X_HOTKEY:
                handleCtrlX();
                break;
            case CTRL_Y_HOTKEY:
                handleCtrlY();
                break;
            case CTRL_Z_HOTKEY:
                handleCtrlZ();
                break;
            default:
                handleCtrlKeys(hotkey);
                break;
            }
        } else {
            switch (hotkey) {
            case ENTER_HOTKEY:
                handleEnter();
                break;
            case ESC_HOTKEY:
                handleEsc();
                break;
            case BS_HOTKEY:
                handleBS();
                break;
            case TAB_HOTKEY:
                handleTab();
                break;
            case DEL_HOTKEY:
                handleDelete();
                break;
            case HOME_HOTKEY:
                handleHome();
                break;
            case END_HOTKEY:
                handleEnd();
                break;
            case PAGE_UP_HOTKEY:
                handlePageUp();
                break;
            case PAGE_DOWN_HOTKEY:
                handlePageDown();
                break;
            case LEFT_ARROW_HOTKEY:
                handleLeftArrow();
                break;
            case RIGHT_ARROW_HOTKEY:
                handleRightArrow();
                break;
            case UP_ARROW_HOTKEY:
                handleUpArrow();
                break;
            case DOWN_ARROW_HOTKEY:
                handleDownArrow();
                break;
            case CTRL_LEFT_ARROW_HOTKEY:
                handleCtrlLeftArrow();
                break;
            case CTRL_RIGHT_ARROW_HOTKEY:
                handleCtrlRightArrow();
                break;
            case CTRL_UP_ARROW_HOTKEY:
                handleCtrlLeftArrow();
                break;
            case CTRL_DOWN_ARROW_HOTKEY:
                handleCtrlRightArrow();
                break;
            case CTRL_SPACE_HOTKEY:
                handleCtrlSpace();
                break;
            case CTRL_SHIFT_SPACE_HOTKEY:
                handleCtrlShiftSpace();
                break;
            case CTRL_SHIFT_G_HOTKEY:
                handleUnblock();
                break;

            default:
                break;
            }
        }
    }
}

//-----------------------------------------------------------------------
// ストロークキーデフォルトハンドラ
void State::handleStrokeKeys(int hk) { LOG_INFO(_T("CALLED: hotkey=%xH(%d)"), hk, hk); }

// スペースキーハンドラ
void State::handleSpaceKey() { LOG_INFO(_T("CALLED")); handleStrokeKeys(HOTKEY_STROKE_SPACE); }

//-----------------------------------------------------------------------
// FullEscape デフォルトハンドラ
void State::handleFullEscape() { LOG_INFOH(_T("CALLED")); setThroughHotkeyFlag(); }

// Unblock デフォルトハンドラ
void State::handleUnblock() { LOG_INFOH(_T("CALLED")); setThroughHotkeyFlag(); }

// handleNextCandTrigger デフォルトハンドラ
void State::handleNextCandTrigger() { LOG_INFOH(_T("CALLED")); setThroughHotkeyFlag(); }

// handlePrevCandTrigger デフォルトハンドラ
void State::handlePrevCandTrigger() { LOG_INFOH(_T("CALLED")); setThroughHotkeyFlag(); }

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
    _LOG_DEBUGH(_T("CALLED: hotkey=%xH(%d)"), hk, hk);
    return false;
}

//-----------------------------------------------------------------------
// Ctrlキー デフォルトハンドラ
void State::handleCtrlKeys(int /*hotkey*/) { setThroughHotkeyFlag(); }

void State::handleCtrlA() { handleCtrlKeys(CTRL_A_HOTKEY); }
void State::handleCtrlB() { handleCtrlKeys(CTRL_B_HOTKEY); }
//void State::handleCtrlB() { handleLeftArrow(); }
void State::handleCtrlC() { handleCtrlKeys(CTRL_C_HOTKEY); }
void State::handleCtrlD() { handleCtrlKeys(CTRL_D_HOTKEY); }
void State::handleCtrlE() { handleCtrlKeys(CTRL_E_HOTKEY); }
void State::handleCtrlF() { handleCtrlKeys(CTRL_F_HOTKEY); }
//void State::handleCtrlF() { handleRightArrow(); }
void State::handleCtrlG() { handleCtrlKeys(CTRL_G_HOTKEY); }
//void State::handleCtrlG() { handleFullEscape(); }
void State::handleCtrlH() { handleCtrlKeys(CTRL_H_HOTKEY); }
//void State::handleCtrlH() { handleBS(); }
void State::handleCtrlI() { handleCtrlKeys(CTRL_I_HOTKEY); }
void State::handleCtrlJ() { handleCtrlKeys(CTRL_J_HOTKEY); }
//void State::handleCtrlJ() { handleEnter(); }
void State::handleCtrlK() { handleCtrlKeys(CTRL_K_HOTKEY); }
void State::handleCtrlL() { handleCtrlKeys(CTRL_L_HOTKEY); }
void State::handleCtrlM() { handleCtrlKeys(CTRL_M_HOTKEY); }
//void State::handleCtrlM() { handleEnter(); }
void State::handleCtrlN() { handleCtrlKeys(CTRL_N_HOTKEY); }
//void State::handleCtrlN() { handleDownArrow(); }
void State::handleCtrlO() { handleCtrlKeys(CTRL_O_HOTKEY); }
void State::handleCtrlP() { handleCtrlKeys(CTRL_P_HOTKEY); }
//void State::handleCtrlP() { handleUpArrow(); }
void State::handleCtrlQ() { handleCtrlKeys(CTRL_Q_HOTKEY); }
void State::handleCtrlR() { handleCtrlKeys(CTRL_R_HOTKEY); }
void State::handleCtrlS() { handleCtrlKeys(CTRL_S_HOTKEY); }
void State::handleCtrlT() { handleCtrlKeys(CTRL_T_HOTKEY); }
void State::handleCtrlU() { handleCtrlKeys(CTRL_U_HOTKEY); }
void State::handleCtrlV() { handleCtrlKeys(CTRL_V_HOTKEY); }
void State::handleCtrlW() { handleCtrlKeys(CTRL_W_HOTKEY); }
void State::handleCtrlX() { handleCtrlKeys(CTRL_X_HOTKEY); }
void State::handleCtrlY() { handleCtrlKeys(CTRL_Y_HOTKEY); }
void State::handleCtrlZ() { handleCtrlKeys(CTRL_Z_HOTKEY); }

//-----------------------------------------------------------------------
// Shiftキー デフォルトハンドラ
void State::handleShiftKeys(int /*hotkey*/) { STATE_COMMON->OutputHotkeyChar(); }

// < ハンドラ
void State::handleLeftTriangle() { handleShiftKeys(LEFT_TRIANGLE_HOTKEY); }

// > ハンドラ
void State::handleRightTriangle() { handleShiftKeys(RIGHT_TRIANGLE_HOTKEY); }

// ? ハンドラ
void State::handleQuestion() { handleShiftKeys(QUESTION_HOTKEY); }

//-----------------------------------------------------------------------
// 特殊キーデフォルトハンドラ
void State::handleSpecialKeys(int /*hotkey*/) { setThroughHotkeyFlag(); }

// Shift+Space ハンドラ
// isStrokeKeyOrShiftedKey() にも注意すること
void State::handleShiftSpace() {
    _LOG_DEBUGH(_T("Shift+Space"));
    if (SETTINGS->histSearchByShiftSpace) {
        handleNextCandTrigger();
    } else if (SETTINGS->handleShiftSpaceAsNormalSpace) {
        handleShiftSpaceAsNormalSpace();
    } else {
        handleSpecialKeys(SHIFT_SPACE_HOTKEY);
    }
}

void State::handleShiftSpaceAsNormalSpace() {
    _LOG_DEBUGH(_T("ShiftSpaceAsNormalSpace"));
    STATE_COMMON->SetOutString(' ');
}

// Ctrl+Space ハンドラ
//void State::handleCtrlSpace() { LOG_DEBUG(_T("Ctrl+Space")); handleSpecialKeys(CTRL_SPACE_HOTKEY);}
void State::handleCtrlSpace() {
    _LOG_DEBUGH(_T("Ctrl+Space"));
    if (SETTINGS->histSearchByCtrlSpace) {
        handleNextCandTrigger();
    } else {
        handleSpecialKeys(CTRL_SPACE_HOTKEY);
    }
}

// Ctrl+Shift+Space ハンドラ
//void State::handleCtrlShiftSpace() { LOG_DEBUG(_T("Ctrl+Shift+Space")); handleSpecialKeys(CTRL_SHIFT_SPACE_HOTKEY);}
void State::handleCtrlShiftSpace() {
    _LOG_DEBUGH(_T("Ctrl+Shift+Space"));
    if (SETTINGS->histSearchByCtrlSpace || SETTINGS->histSearchByShiftSpace) {
        handlePrevCandTrigger();
    } else {
        handleSpecialKeys(CTRL_SHIFT_SPACE_HOTKEY);
    }
}

// RET/Enter ハンドラ
void State::handleEnter() {
    LOG_DEBUG(_T("Enter"));
    STATE_COMMON->SetAppendBackspaceStopperFlag();
    handleSpecialKeys(ENTER_HOTKEY);
}

// ESC ハンドラ
void State::handleEsc() { LOG_DEBUG(_T("Esc")); handleSpecialKeys(ESC_HOTKEY); }
    
// BS ハンドラ
void State::handleBS() { LOG_DEBUG(_T("BackSpace")); setCharDeleteInfo(1); }

// TAB ハンドラ
void State::handleTab() { handleSpecialKeys(TAB_HOTKEY); }

// Delete ハンドラ
void State::handleDelete() { handleSpecialKeys(DEL_HOTKEY); }

// Home ハンドラ
void State::handleHome() { handleSpecialKeys(HOME_HOTKEY); }

// End ハンドラ
void State::handleEnd() { handleSpecialKeys(END_HOTKEY); }

// PageUp ハンドラ
void State::handlePageUp() { handleSpecialKeys(PAGE_UP_HOTKEY); }

// PageDown ハンドラ
void State::handlePageDown() { handleSpecialKeys(PAGE_DOWN_HOTKEY); }

// ← ハンドラ
void State::handleLeftArrow() { handleSpecialKeys(LEFT_ARROW_HOTKEY); }

// Ctrl ← ハンドラ
void State::handleCtrlLeftArrow() { handleSpecialKeys(LEFT_ARROW_HOTKEY); }

// → ハンドラ
void State::handleRightArrow() { handleSpecialKeys(RIGHT_ARROW_HOTKEY); }

// Ctrl → ハンドラ
void State::handleCtrlRightArrow() { handleSpecialKeys(RIGHT_ARROW_HOTKEY); }

// ↑ ハンドラ
void State::handleUpArrow() { handleSpecialKeys(UP_ARROW_HOTKEY); }

// Ctrl ↑ ハンドラ
void State::handleCtrlUpArrow() { handleSpecialKeys(UP_ARROW_HOTKEY); }

// ↓ ハンドラ
void State::handleDownArrow() { handleSpecialKeys(DOWN_ARROW_HOTKEY); }

// Ctrl ↓ ハンドラ
void State::handleCtrlDownArrow() { handleSpecialKeys(DOWN_ARROW_HOTKEY); }

