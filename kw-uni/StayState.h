#pragma once

#include "Logger.h"
#include "State.h"

// 常駐状態のベースクラス
class StayState : public State{
    DECLARE_CLASS_LOGGER;

public:

    // HOTKEY処理の前半部
    void DoHotkeyPreProc(int hotkey);

    // 常駐状態か
    bool IsStay() const {
        return true;;
    }
};

