#pragma once

#include "Logger.h"
#include "History/HistoryStayState.h"
#include "StrokeTable.h"

#include "TranslationState.h"

#define NAME_PTR (Name.c_str())

// 変換系状態のベースクラス
DEFINE_CLASS_LOGGER(TranslationState);

// その他の特殊キー
void TranslationState::handleSpecialKeys(int hotkey) {
    LOG_DEBUG(_T("CALLED: %s, hotkey=%d"), NAME_PTR, hotkey);
    if (HISTORY_STAY_STATE) {
        // 常駐の履歴機能があればそれを呼び出す
        HISTORY_STAY_STATE->dispatchHotkey(hotkey);
    } else {
        State::handleSpecialKeys(hotkey);
    }
}

// 最終的な出力履歴が整ったところで呼び出される処理
void TranslationState::DoOutStringProc() {
    LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
    if (pNext) pNext->DoOutStringProc();
    LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
}

// ストローク状態が後続する変換系状態のベースクラス
DEFINE_CLASS_LOGGER(StrokeTranslationState);

// HOTKEY処理の前半部
void StrokeTranslationState::DoHotkeyPreProc(int hotkey) {
    LOG_DEBUG(_T("ENTER: %s: hotkey=%xH(%d)"), NAME_PTR, hotkey, hotkey);
    ModeState::DoHotkeyPreProc(hotkey);
    LOG_DEBUG(_T("LEAVE: %s"), NAME_PTR);
}

