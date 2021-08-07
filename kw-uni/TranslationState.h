#pragma once

#include "Logger.h"
#include "State.h"
#include "ModeState.h"

// 変換系状態のベースクラス
class TranslationState : public State, public ModeState {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    TranslationState() : ModeState(this) {
        LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
    }

    // その他の特殊キー
    void handleSpecialKeys(int deckey);

    // 最終的な出力履歴が整ったところで呼び出される処理
    void DoOutStringProc();

};

// ストローク状態が後続する変換系状態のベースクラス
class StrokeTranslationState : public TranslationState {
    DECLARE_CLASS_LOGGER;

public:
    StrokeTranslationState() {
        LOG_INFO(_T("CALLED: CONSTRUCTOR"));
    }

protected:
    // DECKEY処理の前半部
    void DoDeckeyPreProc(int deckey);

};

