#include "Logger.h"

#include "StrokeTable.h"
#include "StayState.h"
#include "Mazegaki/Mazegaki.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

#define NAME_PTR    Name.c_str()

// HOTKEY処理の前半部
void StayState::DoHotkeyPreProc(int hotkey) {
    LOG_DEBUGH(_T("ENTER: %s: hotkey=%xH(%d)"), NAME_PTR, hotkey, hotkey);
    // まだ後続状態が無く、自身が StrokeState ではなく、hotkey はストロークキーである場合は、ルートストローク状態を生成して後続させる
    if (!pNext) {
        if ((!pNode || !pNode->isStrokeTableNode()) && isStrokeKeyOrShiftedKey(hotkey)) {
            LOG_DEBUGH(_T("CREATE: RootStrokeNode"));
            pNext = ROOT_STROKE_NODE->CreateState();
            pNext->SetPrevState(this);
        }
    }
    State::DoHotkeyPreProc(hotkey);
    LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
}


// Esc の処理
void StayState::handleEsc() {
    if (MAZEGAKI_NODE) {
        MString prevYomi;
        size_t prevXferLen = MAZEGAKI_NODE->GetPrevYomiInfo(prevYomi, STATE_COMMON->GetTotalHotKeyCount());
        if (prevXferLen > 0) {
            STATE_COMMON->SetOutString(prevYomi, prevXferLen);
            return;
        }
    }
    // アクティブウィンドウにEscを送る
    State::handleEsc();
}

DEFINE_CLASS_LOGGER(StayState);

