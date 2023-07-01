#pragma once

#include "Logger.h"

class State;

// 変換系状態のクラス
class ModalState {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    ModalState() {
        LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
    }

protected:
    // モード状態の処理
    bool HandleModalState(State* pState, int deckey);

};

