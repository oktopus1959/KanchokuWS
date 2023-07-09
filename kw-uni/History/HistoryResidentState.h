#pragma once

#include "ResidentState.h"

// -------------------------------------------------------------------
// 履歴入力(常駐)機能状態(抽象)クラス
class HistoryResidentState : public ResidentState {
public:
    // 事前チェック
    void DoPreCheck() override = 0;

    // Enter時の新しい履歴の追加
    virtual void AddNewHistEntryOnEnter() = 0;

    // 何か文字が入力されたときの新しい履歴の追加
    virtual void AddNewHistEntryOnSomeChar() = 0;

    // 文字列を変換して出力、その後、履歴の追加
    virtual void SetTranslatedOutString(const MString& outStr, size_t rewritableLen, int numBS = -1) = 0;

    // Ctrl-H ハンドラ
    //void handleCtrlH() { setCharDeleteInfo(1); }

    virtual void handleFullEscapeResidentState() = 0;

    virtual void handleEisuDecapitalize() = 0;

    virtual void commitHistory() = 0;

public:
    // 唯一のインスタンスを指すポインタ (寿命管理は CreateState() を呼び出したところがやる)
    static HistoryResidentState* Singleton;
};

#define HISTORY_RESIDENT_STATE (HistoryResidentState::Singleton)
