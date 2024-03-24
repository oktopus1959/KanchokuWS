#pragma once

#include "Logger.h"
#include "State.h"
#include "ModalState.h"

// 常駐状態のベースクラス
class ResidentState : public State {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    ResidentState() {
        LOG_INFO(_T("CALLED: CONSTRUCTOR"));
    }

    // 不要になった状態か
    bool IsUnnecessary() override {
        return false;
    }

    // 常駐状態か
    bool IsResident() const {
        return true;;
    }

    // Esc の処理
    void handleEsc() override;

};

