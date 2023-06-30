#pragma once

#include "Logger.h"
#include "State.h"

// 常駐状態のベースクラス
class ResidentState : public State {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    ResidentState() {
        LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
    }

    // 常駐状態か
    bool IsResident() const {
        return true;;
    }

    // Esc の処理
    void handleEsc();

protected:
    // モード状態か
    bool IsModeState() { return true; }

};

