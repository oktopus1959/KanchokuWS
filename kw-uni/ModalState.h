#pragma once

#include "Logger.h"
#include "State.h"

// 変換系状態のクラス
class ModalState : public State {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    ModalState() {
        LOG_INFO(_T("CALLED: CONSTRUCTOR"));
    }

protected:
    //// ModalStateの前処理
    //int DoModalStatePreProc(int deckey) override;
    // 入力された DECKEY を処理する(前処理)
    int ModalStatePreProc(int deckey);

};

