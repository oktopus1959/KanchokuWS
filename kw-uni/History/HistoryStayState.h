#pragma once

#include "StayState.h"

// -------------------------------------------------------------------
// 履歴入力(常駐)機能状態(抽象)クラス
class HistoryStayState : public StayState {
public:
    // Enter時の新しい履歴の追加
    virtual void AddNewHistEntryOnEnter() = 0;

    // 何か文字が入力されたときの新しい履歴の追加
    virtual void AddNewHistEntryOnSomeChar() = 0;

    // 文字列を変換して出力、その後、履歴の追加
    virtual void SetTranslatedOutString(const MString& outStr) = 0;

    // Ctrl-H ハンドラ
    //void handleCtrlH() { setCharDeleteInfo(1); }

    virtual void handleFullEscapeStayState() = 0;

public:
    // 唯一のインスタンスを指すポインタ (寿命管理は CreateState() を呼び出したところがやる)
    static HistoryStayState* Singleton;
};

#define HISTORY_STAY_STATE (HistoryStayState::Singleton)
