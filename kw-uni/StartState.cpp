#include "Logger.h"

#include "StartNode.h"
#include "StartState.h"
//#include "ResidentState.h"
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
class StartStateImpl : public StartState {
    DECLARE_CLASS_LOGGER;
public:
    StartStateImpl(StartNode* pN) {
        LOG_INFO(_T("CALLED: ctor"));
        Initialize(logger.ClassNameT(), pN);
    }

    // DECKEY 処理の流れ
    void HandleDeckey(int deckey) override {
        LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({}), totalCount={}, NextNode={}, outStr={}"),
            Name, deckey, deckey, STATE_COMMON->GetTotalDecKeyCount(), NODE_NAME(NextNodeMaybe()), to_wstr(STATE_COMMON->OutString()));

        // 前処理
        if (NextState()) {
            LOG_DEBUGH(_T("CALL NextState()->HandleDeckeyChain()"));
            // 後続状態があれば、そちらを呼び出す
            NextState()->HandleDeckeyChain(deckey);
        }
        // 中間チェック
        LOG_DEBUGH(_T("CALL DoIntermediateCheckChain()"));
        DoIntermediateCheckChain();
        // 後処理(新状態生成処理)
        //LOG_DEBUGH(_T("CALL DoDeckeyPostProcChain()"));
        //DoDeckeyPostProcChain();
        // 後処理(新状態生成処理)
        CreateNewStateChain();
        // 出力文字を取得する
        MStringResult result;
        GetResultStringChain(result);
        // チェーンをたどって不要とマークされた後続状態を削除する
        DeleteUnnecessarySuccessorStateChain();

        LOG_DEBUGH(_T("LEAVE: {}, NextNode={}, outStr={}"), Name, NODE_NAME(NextNodeMaybe()), to_wstr(STATE_COMMON->OutString()));
        //return pNextNodeMaybe;
    }

    //void handleBS() { LOG_DEBUG(_T("BackSpace")); setCharDeleteInfo(1); /*STATE_COMMON->SetSpecialDeckeyOnStartStateFlag();*/ }

    //void handleEnter() {
    //    LOG_DEBUG(_T("Enter: {}"), Name);
    //    HISTORY_RESIDENT_STATE->AddNewHistEntryOnEnter();
    //    State::handleEnter();
    //}
};

DEFINE_CLASS_LOGGER(StartStateImpl);

DEFINE_CLASS_LOGGER(StartNode);

// 開始ノード
// 当ノードを処理する State インスタンスを作成する
State* StartNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new StartStateImpl(this);
}

