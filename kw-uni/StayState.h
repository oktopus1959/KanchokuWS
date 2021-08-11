#pragma once

#include "Logger.h"
#include "State.h"

// 常駐状態のベースクラス
class StayState : public State {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    StayState() {
        LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
    }

    // 常駐状態か
    bool IsStay() const {
        return true;;
    }

    // Esc の処理
    void handleEsc();

protected:
    // モード状態か
    bool IsModeState() { return true; }

};

