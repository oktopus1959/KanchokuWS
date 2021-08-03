/*
* モード状態を表す Mix-In クラス<br/>
* 使い方については、TranslationState.{cpp,h} を参照
*/
#include "Logger.h"

#include "StrokeTable.h"
#include "ModeState.h"
#include "Mazegaki/Mazegaki.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

DEFINE_CLASS_LOGGER(ModeState);

#define NAME_PTR    pFriendState->Name.c_str()

// HOTKEY処理の前半部
void ModeState::DoHotkeyPreProc(int hotkey) {
    LOG_DEBUGH(_T("ENTER: %s: hotkey=%xH(%d)"), NAME_PTR, hotkey, hotkey);
    // まだ後続状態が無く、自身が StrokeState ではなく、hotkey はストロークキーである場合は、ルートストローク状態を生成して後続させる
    if (!pFriendState->pNext) {
        if ((!pFriendState->pNode || !pFriendState->pNode->isStrokeTableNode()) && pFriendState->isStrokeKeyOrShiftedKey(hotkey)) {
            LOG_DEBUGH(_T("CREATE: RootStrokeNode"));
            pFriendState->pNext = ROOT_STROKE_NODE->CreateState();
            pFriendState->pNext->SetPrevState(pFriendState);
        }
    }
    pFriendState->State::DoHotkeyPreProc(hotkey);
    LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
}


// Esc の処理
void ModeState::handleEsc() {
    LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
    if (MAZEGAKI_NODE) {
        MString prevYomi;
        size_t prevXferLen = MAZEGAKI_NODE->GetPrevYomiInfo(prevYomi, STATE_COMMON->GetTotalHotKeyCount());
        if (prevXferLen > 0) {
            STATE_COMMON->SetOutString(prevYomi, prevXferLen);
            return;
        }
    }
    // アクティブウィンドウにEscを送る
    pFriendState->State::handleEsc();
}

