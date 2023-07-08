#pragma once

#include "Logger.h"
#include "State.h"

// 変換系状態のクラス
class ModalState : public State {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    ModalState() {
        LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
    }

protected:
    // DECKEY処理の前半部
    void DoDeckeyPreProc(int deckey) override;

};

