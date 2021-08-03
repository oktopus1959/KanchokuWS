#include "Logger.h"

#include "StrokeTable.h"
#include "StayState.h"
#include "Mazegaki/Mazegaki.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

#define NAME_PTR    Name.c_str()

// HOTKEY処理の前半部
void StayState::DoHotkeyPreProc(int hotkey) {
    LOG_DEBUGH(_T("ENTER: %s: hotkey=%xH(%d)"), NAME_PTR, hotkey, hotkey);
    ModeState::DoHotkeyPreProc(hotkey);
    LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
}


// Esc の処理
void StayState::handleEsc() {
    LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
    ModeState::handleEsc();
}

DEFINE_CLASS_LOGGER(StayState);

