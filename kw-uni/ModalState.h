#pragma once

#include "Logger.h"
#include "State.h"

// 変換系状態のクラス
class ModalState {
    DECLARE_CLASS_LOGGER;

public:
    //// ModalStateの前処理
    //int DoModalStatePreProc(int deckey) override;
    // 入力された DECKEY を処理する(前処理)
    static int ModalStatePreProc(State* pState, int deckey, bool isStrokable);

};

