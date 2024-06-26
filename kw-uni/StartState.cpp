#include "Logger.h"

#include "StartNode.h"
#include "ResidentState.h"
#include "History/HistoryResidentState.h"

#if 0 || defined(_DEBUG)
#undef LOG_INFO
#undef LOG_DEBUGH
#undef LOG_DEBUG
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#endif

// 始状態 -- 仮想鍵盤のモード管理も行う
class StartState : public ResidentState {
    DECLARE_CLASS_LOGGER;
public:
    StartState(StartNode* pN) {
        LOG_INFO(_T("CALLED: ctor"));
        Initialize(logger.ClassNameT(), pN);
    }

    // DECKEY 処理の流れ
    // 新ノードが未処理の場合は、ここで NULL 以外が返されるので、親状態で処理する
    void HandleDeckeyChain(int deckey) override {
        LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({}), totalCount={}, NextNode={}, outStr={}"),
            Name, deckey, deckey, STATE_COMMON->GetTotalDecKeyCount(), NODE_NAME(NextNodeMaybe()), to_wstr(STATE_COMMON->OutString()));
        // 前処理
        LOG_DEBUGH(_T("CALL HandleDeckeyChain()"));
        State::HandleDeckeyChain(deckey);
        // 中間チェック
        LOG_DEBUGH(_T("CALL DoIntermediateCheckChain()"));
        DoIntermediateCheckChain();
        // 後処理
        LOG_DEBUGH(_T("CALL DoDeckeyPostProcChain()"));
        DoDeckeyPostProcChain();
        LOG_DEBUGH(_T("LEAVE: {}, NextNode={}, outStr={}"), Name, NODE_NAME(NextNodeMaybe()), to_wstr(STATE_COMMON->OutString()));
        //return pNextNodeMaybe;
    }

    //void handleBS() { LOG_DEBUG(_T("BackSpace")); setCharDeleteInfo(1); /*STATE_COMMON->SetSpecialDeckeyOnStartStateFlag();*/ }

    void handleEnter() {
        LOG_DEBUG(_T("Enter: {}"), Name);
        HISTORY_RESIDENT_STATE->AddNewHistEntryOnEnter();
        State::handleEnter();
    }
};

DEFINE_CLASS_LOGGER(StartState);

DEFINE_CLASS_LOGGER(StartNode);

// 開始ノード
// 当ノードを処理する State インスタンスを作成する
State* StartNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new StartState(this);
}

