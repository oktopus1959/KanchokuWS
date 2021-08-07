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
Node* State::HandleDeckey(int deckey) {
    _LOG_DEBUGH(_T("ENTER: %s: deckey=%xH(%d), NextNode=%s"), NAME_PTR, deckey, deckey, NODE_NAME_PTR(TemporaryNextNode()));
    // 前処理
    DoDeckeyPreProc(deckey);
    // 後処理
    DoDeckeyPostProc();
    _LOG_DEBUGH(_T("LEAVE: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(TemporaryNextNode()));
    return pTemporaryNextNode;
}

// DECKEY処理の前半部のデフォルト処理。
// 後続状態があればそちらに移譲。なければここでホットキーをディスパッチ。
// 自前で DECKEY 処理をやる場合にはオーバーライドしてもよい
void State::DoDeckeyPreProc(int deckey) {
    _LOG_DEBUGH(_T("ENTER: %s: deckey=%xH(%d), NextNode=%s"), NAME_PTR, deckey, deckey, NODE_NAME_PTR(TemporaryNextNode()));
    pTemporaryNextNode = nullptr;
    if (pNext) {
        // 後続状態があれば、そちらを呼び出す ⇒ 新しい後続ノードが生成されたらそれを一時的に記憶しておく(後半部で処理する)
        pTemporaryNextNode = pNext->HandleDeckey(deckey);
    } else {
        // 後続状態がなく、ストロークキー以外であれば、ここでDECKEYをディスパッチする
        dispatchDeckey(deckey);
    }
    _LOG_DEBUGH(_T("LEAVE: %s, NextNode=%s"), NAME_PTR, NODE_NAME_PTR(TemporaryNextNode()));
}

// DECKEY処理の後半部のデフォルト処理。
// 不要になった後続状態の削除と、新しい後続状態の生成とチェイン。
// 自前で DECKEY 処理をやる場合にはオーバーライドしてもよい
void State::DoDeckeyPostProc() {
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

// DECKEY はストロークキーか
bool State::isStrokeKey(int deckey) {
    return deckey >= 0 && deckey < NORMAL_DECKEY_NUM;
}

// DECKEY はShift飾修キーか
bool State::isShiftedKey(int deckey) {
    return deckey >= SHIFT_DECKEY_START && deckey < STROKE_DECKEY_NUM;
}

// DECKEY はストロークキーまたはShift飾修か
bool State::isStrokeKeyOrShiftedKey(int deckey) {
    return isStrokeKey(deckey)
        || (isShiftedKey(deckey)
            && (deckey != SHIFT_SPACE_DECKEY
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
    } else if (deckey == NEXT_CAND_TRIGGER_DECKEY) {
        handleNextCandTrigger();
    } else if (deckey == PREV_CAND_TRIGGER_DECKEY) {
        handlePrevCandTrigger();
    } else {
        if (handleFunctionKeys(deckey)) return;

        if (isShiftedKey(deckey)) {
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
            case SHIFT_SPACE_DECKEY:
                //handleShiftSpace();
                handleShiftKeys(deckey);
                break;
            default:
                handleShiftKeys(deckey);
                break;
            }
        } else if (deckey >= CTRL_FUNC_DECKEY_ID_BASE && deckey <= CTRL_FUNC_DECKEY_ID_END) {
            switch (deckey) {
            case CTRL_A_DECKEY:
                handleCtrlA();
                break;
            case CTRL_B_DECKEY:
                handleCtrlB();
                break;
            case CTRL_C_DECKEY:
                handleCtrlC();
                break;
            case CTRL_D_DECKEY:
                handleCtrlD();
                break;
            case CTRL_E_DECKEY:
                handleCtrlE();
                break;
            case CTRL_F_DECKEY:
                handleCtrlF();
                break;
            case CTRL_G_DECKEY:
                handleCtrlG();
                break;
            case CTRL_H_DECKEY:
                handleCtrlH();
                break;
            case CTRL_I_DECKEY:
                handleCtrlI();
                break;
            case CTRL_J_DECKEY:
                handleCtrlJ();
                break;
            case CTRL_K_DECKEY:
                handleCtrlK();
                break;
            case CTRL_L_DECKEY:
                handleCtrlL();
                break;
            case CTRL_M_DECKEY:
                handleCtrlM();
                break;
            case CTRL_N_DECKEY:
                handleCtrlN();
                break;
            case CTRL_O_DECKEY:
                handleCtrlO();
                break;
            case CTRL_P_DECKEY:
                handleCtrlP();
                break;
            case CTRL_Q_DECKEY:
                handleCtrlQ();
                break;
            case CTRL_R_DECKEY:
                handleCtrlR();
                break;
            case CTRL_S_DECKEY:
                handleCtrlS();
                break;
            case CTRL_T_DECKEY:
                handleCtrlT();
                break;
            case CTRL_U_DECKEY:
                handleCtrlU();
                break;
            case CTRL_V_DECKEY:
                handleCtrlV();
                break;
            case CTRL_W_DECKEY:
                handleCtrlW();
                break;
            case CTRL_X_DECKEY:
                handleCtrlX();
                break;
            case CTRL_Y_DECKEY:
                handleCtrlY();
                break;
            case CTRL_Z_DECKEY:
                handleCtrlZ();
                break;
            default:
                handleCtrlKeys(deckey);
                break;
            }
        } else {
            switch (deckey) {
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
            case CTRL_SPACE_DECKEY:
                handleCtrlSpace();
                break;
            case CTRL_SHIFT_SPACE_DECKEY:
                handleCtrlShiftSpace();
                break;
            //case CTRL_SHIFT_G_DECKEY:
            //    handleUnblock();
            //    break;

            default:
                break;
            }
        }
    }
}

//-----------------------------------------------------------------------
// ストロークキーデフォルトハンドラ
void State::handleStrokeKeys(int hk) { LOG_INFO(_T("CALLED: deckey=%xH(%d)"), hk, hk); }

// スペースキーハンドラ
void State::handleSpaceKey() { LOG_INFO(_T("CALLED")); handleStrokeKeys(STROKE_SPACE_DECKEY); }

//-----------------------------------------------------------------------
// FullEscape デフォルトハンドラ
void State::handleFullEscape() { LOG_INFOH(_T("CALLED")); setThroughDeckeyFlag(); }

// Unblock デフォルトハンドラ
void State::handleUnblock() { LOG_INFOH(_T("CALLED")); setThroughDeckeyFlag(); }

// handleNextCandTrigger デフォルトハンドラ
void State::handleNextCandTrigger() { LOG_INFOH(_T("CALLED")); setThroughDeckeyFlag(); }

// handlePrevCandTrigger デフォルトハンドラ
void State::handlePrevCandTrigger() { LOG_INFOH(_T("CALLED")); setThroughDeckeyFlag(); }

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

//(2021/7/21)やっぱり ctrl-B,F,H,N,Pは機能キーに変換しておく
// ここで Ctrl-B...などが呼ばれるのは、グローバルに Ctrl-B などが有効になっているが、
// PuTTY など、矢印キーへの変換が無効になっている場合。
// でも、デコーダ内部では矢印キーとして働くことに問題はない。
// スルーの場合は元のCtrl-Bなどとしてアプリに渡るので問題ない。
void State::handleCtrlA() { handleCtrlKeys(CTRL_A_DECKEY); }
//void State::handleCtrlB() { handleCtrlKeys(CTRL_B_DECKEY); }
void State::handleCtrlB() { handleLeftArrow(); }
void State::handleCtrlC() { handleCtrlKeys(CTRL_C_DECKEY); }
void State::handleCtrlD() { handleCtrlKeys(CTRL_D_DECKEY); }
void State::handleCtrlE() { handleCtrlKeys(CTRL_E_DECKEY); }
//void State::handleCtrlF() { handleCtrlKeys(CTRL_F_DECKEY); }
void State::handleCtrlF() { handleRightArrow(); }
void State::handleCtrlG() { handleCtrlKeys(CTRL_G_DECKEY); }
//void State::handleCtrlG() { handleFullEscape(); }
//void State::handleCtrlH() { handleCtrlKeys(CTRL_H_DECKEY); }
void State::handleCtrlH() { handleBS(); }
void State::handleCtrlI() { handleCtrlKeys(CTRL_I_DECKEY); }
void State::handleCtrlJ() { handleCtrlKeys(CTRL_J_DECKEY); }
//void State::handleCtrlJ() { handleEnter(); }
void State::handleCtrlK() { handleCtrlKeys(CTRL_K_DECKEY); }
void State::handleCtrlL() { handleCtrlKeys(CTRL_L_DECKEY); }
void State::handleCtrlM() { handleCtrlKeys(CTRL_M_DECKEY); }
//void State::handleCtrlM() { handleEnter(); }
//void State::handleCtrlN() { handleCtrlKeys(CTRL_N_DECKEY); }
void State::handleCtrlN() { handleDownArrow(); }
void State::handleCtrlO() { handleCtrlKeys(CTRL_O_DECKEY); }
//void State::handleCtrlP() { handleCtrlKeys(CTRL_P_DECKEY); }
void State::handleCtrlP() { handleUpArrow(); }
void State::handleCtrlQ() { handleCtrlKeys(CTRL_Q_DECKEY); }
void State::handleCtrlR() { handleCtrlKeys(CTRL_R_DECKEY); }
void State::handleCtrlS() { handleCtrlKeys(CTRL_S_DECKEY); }
void State::handleCtrlT() { handleCtrlKeys(CTRL_T_DECKEY); }
void State::handleCtrlU() { handleCtrlKeys(CTRL_U_DECKEY); }
void State::handleCtrlV() { handleCtrlKeys(CTRL_V_DECKEY); }
void State::handleCtrlW() { handleCtrlKeys(CTRL_W_DECKEY); }
void State::handleCtrlX() { handleCtrlKeys(CTRL_X_DECKEY); }
void State::handleCtrlY() { handleCtrlKeys(CTRL_Y_DECKEY); }
void State::handleCtrlZ() { handleCtrlKeys(CTRL_Z_DECKEY); }

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
// 特殊キーデフォルトハンドラ
void State::handleSpecialKeys(int /*deckey*/) { setThroughDeckeyFlag(); }

// Shift+Space ハンドラ
// isStrokeKeyOrShiftedKey() にも注意すること
void State::handleShiftSpace() {
    _LOG_DEBUGH(_T("Shift+Space"));
    if (SETTINGS->histSearchByShiftSpace) {
        handleNextCandTrigger();
    } else if (SETTINGS->handleShiftSpaceAsNormalSpace) {
        handleShiftSpaceAsNormalSpace();
    } else {
        handleSpecialKeys(SHIFT_SPACE_DECKEY);
    }
}

void State::handleShiftSpaceAsNormalSpace() {
    _LOG_DEBUGH(_T("ShiftSpaceAsNormalSpace"));
    STATE_COMMON->SetOutString(' ');
}

// Ctrl+Space ハンドラ
//void State::handleCtrlSpace() { LOG_DEBUG(_T("Ctrl+Space")); handleSpecialKeys(CTRL_SPACE_DECKEY);}
void State::handleCtrlSpace() {
    _LOG_DEBUGH(_T("Ctrl+Space"));
    if (SETTINGS->histSearchByCtrlSpace) {
        handleNextCandTrigger();
    } else {
        handleSpecialKeys(CTRL_SPACE_DECKEY);
    }
}

// Ctrl+Shift+Space ハンドラ
//void State::handleCtrlShiftSpace() { LOG_DEBUG(_T("Ctrl+Shift+Space")); handleSpecialKeys(CTRL_SHIFT_SPACE_DECKEY);}
void State::handleCtrlShiftSpace() {
    _LOG_DEBUGH(_T("Ctrl+Shift+Space"));
    if (SETTINGS->histSearchByCtrlSpace || SETTINGS->histSearchByShiftSpace) {
        handlePrevCandTrigger();
    } else {
        handleSpecialKeys(CTRL_SHIFT_SPACE_DECKEY);
    }
}

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

