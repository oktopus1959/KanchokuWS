#include "Logger.h"

#include "StartNode.h"
#include "StayState.h"
#include "History/HistoryStayState.h"

// 始状態 -- 仮想鍵盤のモード管理も行う
class StartState : public StayState {
    DECLARE_CLASS_LOGGER;
public:
    StartState(StartNode* pN) {
        Initialize(logger.ClassNameT(), pN);
    }

#define NAME_PTR    Name.c_str()

    //void handleBS() { LOG_DEBUG(_T("BackSpace")); setCharDeleteInfo(1); /*STATE_COMMON->SetSpecialDeckeyOnStartStateFlag();*/ }

    void handleEnter() {
        LOG_DEBUG(_T("Enter: %s"), NAME_PTR);
        HISTORY_STAY_STATE->AddNewHistEntryOnEnter();
        State::handleEnter();
    }
};

DEFINE_CLASS_LOGGER(StartState);

// 開始ノード
// 当ノードを処理する State インスタンスを作成する
State* StartNode::CreateState() { return new StartState(this); }

