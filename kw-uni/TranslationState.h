#pragma once

#include "Logger.h"
#include "State.h"
#include "ModalState.h"

// 変換系状態のベースクラス
class TranslationState : public State, public ModalState {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    TranslationState() {
        LOG_INFOH(_T("CALLED: CONSTRUCTOR"));
    }

    // Esc の処理
    void handleEsc();

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
    // モード状態の処理
    bool DoModalStateProc(int deckey) override { return HandleModalState(this, deckey); }

};

