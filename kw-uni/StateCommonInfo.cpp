
#include "StateCommonInfo.h"

DEFINE_CLASS_LOGGER(StateCommonInfo);

std::unique_ptr<StateCommonInfo> StateCommonInfo::Singleton;

void StateCommonInfo::CreateSingleton() {
    Singleton.reset(new StateCommonInfo());
}

void StateCommonInfo::setCenterString(mchar_t center) {
    if (center == 0) {
        centerString.clear();
    } else {
        centerString.resize(1);
        centerString[0] = (wchar_t)center;
    }
}

// 仮想鍵盤と受渡しするための文字をセットする
// lo : レイアウト, fcs:左右鍵盤にセットする文字列
void StateCommonInfo::setVirtualKeyboardStrings(VkbLayout lo, const mchar_t* fcs) {
    LOG_DEBUG(_T("layout=%d"), lo);
    SetVkbLayout(lo);
    for (int i = 0; i < NORMAL_DECKEY_NUM; ++i) faces[i] = fcs ? fcs[i] : 0;
    for (auto& s : longVkeyCandidates) s.clear();
}

// 仮想鍵盤と受渡しするための文字をセットする
// lo : レイアウト, longKeys: 縦列または横列鍵盤にセットする文字列
void StateCommonInfo::setVirtualKeyboardStrings(VkbLayout lo, const std::vector<MString>& verticals, size_t pos) {
    LOG_DEBUG(_T("layout=%d"), lo);
    SetVkbLayout(lo);
    for (int i = 0; i < NORMAL_DECKEY_NUM; ++i) faces[i] = 0;
    for (size_t n = 0; n < longVkeyCandidates.size(); ++n) {
        if (pos + n < verticals.size())
            longVkeyCandidates[n] = to_wstr(verticals[pos + n]);
        else
            longVkeyCandidates[n].clear();
    }
}

#include "State.h"

// 指定の名前の状態が実行されているか
// 既に実行されていれば、それを削除して false を返す
// 実行されていなければ、map に追加する (true を返す)
bool StateCommonInfo::AddOrEraseRunningState(const wstring& stateName, State* pState) {
    auto iter = runningStates.find(stateName);
    if (iter != runningStates.end()) {
        if (iter->second) iter->second->MarkUnnecessaryFromThis();
        runningStates.erase(iter);
        return false;
    } else {
        runningStates[stateName] = pState;
        return true;
    }
}
