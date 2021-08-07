#include "Logger.h"

#include "StrokeTable.h"
#include "StayState.h"
#include "Mazegaki/Mazegaki.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

#define NAME_PTR    Name.c_str()

// DECKEY処理の前半部
void StayState::DoDeckeyPreProc(int deckey) {
    LOG_DEBUGH(_T("ENTER: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
    ModeState::DoDeckeyPreProc(deckey);
    LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
}


// Esc の処理
void StayState::handleEsc() {
    LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
    ModeState::handleEsc();
}

DEFINE_CLASS_LOGGER(StayState);

