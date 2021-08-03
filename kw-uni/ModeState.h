#pragma once

#include "Logger.h"
#include "State.h"

// モード状態のMix-Inクラス
class ModeState {
    DECLARE_CLASS_LOGGER;

    State* pFriendState = 0;

public:
    ModeState(State* pState) : pFriendState(pState) {
        LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
    }

    // HOTKEY処理の前半部
    void DoHotkeyPreProc(int hotkey);

    // Esc の処理
    void handleEsc();
};

