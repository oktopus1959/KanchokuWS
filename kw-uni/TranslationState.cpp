#pragma once

#include "Logger.h"
#include "History/HistoryStayState.h"
#include "StrokeTable.h"
#include "Mazegaki/Mazegaki.h"

#include "TranslationState.h"

#define NAME_PTR (Name.c_str())

// 変換系状態のベースクラス
DEFINE_CLASS_LOGGER(TranslationState);

// Esc の処理
void TranslationState::handleEsc() {
    HANDLE_ESC_FOR_MAZEGAKI();
}

// その他の特殊キー
void TranslationState::handleSpecialKeys(int deckey) {
    LOG_DEBUG(_T("CALLED: %s, deckey=%d"), NAME_PTR, deckey);
    if (HISTORY_STAY_STATE) {
        // 常駐の履歴機能があればそれを呼び出す
        HISTORY_STAY_STATE->dispatchDeckey(deckey);
    } else {
        State::handleSpecialKeys(deckey);
    }
}

// 最終的な出力履歴が整ったところで呼び出される処理
void TranslationState::DoOutStringProc() {
    LOG_INFO(_T("ENTER: %s: outStr=%s"), NAME_PTR, MAKE_WPTR(STATE_COMMON->OutString()));
    if (pNext) pNext->DoOutStringProc();
    LOG_INFO(_T("LEAVE: %s: outStr=%s"), NAME_PTR, MAKE_WPTR(STATE_COMMON->OutString()));
}

// ストローク状態が後続する変換系状態のベースクラス
DEFINE_CLASS_LOGGER(StrokeTranslationState);

