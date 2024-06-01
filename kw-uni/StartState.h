#include "ResidentState.h"

// 始状態 -- 仮想鍵盤のモード管理も行う
class StartState : public ResidentState {
public:
    // DECKEY 処理の流れ
    virtual void StartHandleDeckey(int deckey) = 0;

    virtual const MStringResult& GetResultString() = 0;
};

