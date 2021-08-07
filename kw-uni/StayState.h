#pragma once

#include "Logger.h"
#include "State.h"
#include "ModeState.h"

// 常駐状態のベースクラス
class StayState : public State, public ModeState {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    StayState() : ModeState(this) {
        LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
    }

    // DECKEY処理の前半部
    void DoDeckeyPreProc(int deckey);

    // 常駐状態か
    bool IsStay() const {
        return true;;
    }

    // Esc の処理
    void handleEsc();
};

