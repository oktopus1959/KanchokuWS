#pragma once

#include "Logger.h"
#include "State.h"

// 変換系状態のUtilクラス
class ModalStateUtil {
    DECLARE_CLASS_LOGGER;

public:
    // 入力された DECKEY を処理する(前処理)
    static int ModalStatePreProc(State* pState, int deckey, bool isStrokable);

    // その他の特殊キー (常駐の履歴機能があればそれを呼び出す)
    static void handleSpecialKeys(State* pState, int deckey);
};

