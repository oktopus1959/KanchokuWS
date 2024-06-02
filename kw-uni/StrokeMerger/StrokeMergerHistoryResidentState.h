#pragma once

#include "ResidentState.h"

// -------------------------------------------------------------------
// 履歴入力(常駐)機能状態(抽象)クラス
class StrokeMergerHistoryResidentState : public ResidentState {
protected:
    // 履歴常駐状態の事前チェック
    //void DoHistoryResidentPreCheck() override = 0;
    int HandleDeckeyPreProc(int deckey) override = 0;

public:
    // Enter時の新しい履歴の追加
    virtual void AddNewHistEntryOnEnter() = 0;

    // 何か文字が入力されたときの新しい履歴の追加
    virtual void AddNewHistEntryOnSomeChar() = 0;

    // 文字列を変換して出力、その後、履歴の追加
    virtual void SetTranslatedOutString(const MString& outStr, size_t rewritableLen, bool bBushuComp = true, int numBS = -1) = 0;

    virtual void handleFullEscapeResidentState() = 0;

    virtual void handleEisuDecapitalize() = 0;

    virtual void commitHistory() = 0;

public:
    // 唯一のインスタンスを指すポインタ (寿命管理は CreateState() を呼び出したところがやる)
    static StrokeMergerHistoryResidentState* Singleton;
};

#define MERGER_HISTORY_RESIDENT_STATE (StrokeMergerHistoryResidentState::Singleton)
