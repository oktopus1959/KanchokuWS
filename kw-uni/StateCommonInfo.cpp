
#include "StateCommonInfo.h"

DEFINE_CLASS_LOGGER(StateCommonInfo);

#if 0
#define IS_LOG_DEBUGH_ENABLED true
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_DEBUGH LOG_INFO
#define LOG_DEBUG LOG_INFO
#define _LOG_DEBUGH LOG_INFO
#define _LOG_DEBUGH_COND LOG_INFO_COND
#endif

String StateCommonInfo::GetVkbLayoutStr(VkbLayout lo) {
    switch (lo) {
    case VkbLayout::None:
        return L"None";
    case VkbLayout::TwoSides:
        return L"TwoSides";
    case VkbLayout::Vertical:
        return L"Vertical";
    case VkbLayout::BushuCompHelp:
        return L"BushuCompHelp";
    case VkbLayout::Horizontal:
        return L"Horizontal";
    case VkbLayout::Normal:
        return L"Normal";
    case VkbLayout::StrokeHelp:
        return L"StrokeHelp";
    default:
        return L"None";
    }
}

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
    LOG_DEBUG(_T("layout={}"), GetVkbLayoutStr(lo));
    SetVkbLayout(lo);
    for (int i = 0; i < NORMAL_DECKEY_NUM; ++i) faces[i] = fcs ? fcs[i] : 0;
    for (auto& s : longVkeyCandidates) s.clear();
}

// 仮想鍵盤と受渡しするための文字をセットする
// lo : レイアウト, longKeys: 縦列または横列鍵盤にセットする文字列
void StateCommonInfo::setVirtualKeyboardStrings(VkbLayout lo, const std::vector<MString>& verticals, size_t pos) {
    LOG_DEBUG(_T("layout={}"), GetVkbLayoutStr(lo));
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
// pState == null なら削除のみ行う
bool StateCommonInfo::AddOrEraseRunningState(StringRef stateName, State* pState) {
    auto iter = runningStates.find(stateName);
    if (iter != runningStates.end()) {
        if (iter->second) iter->second->MarkUnnecessaryFromThis();
        runningStates.erase(iter);
        return false;
    } else {
        if (pState) runningStates[stateName] = pState;
        return true;
    }
}

bool StateCommonInfo::FindRunningState(StringRef stateName) {
    return runningStates.find(stateName) != runningStates.end();
}

void StateCommonInfo::ClearRunningStates() {
    runningStates.clear();
}

#include "StrokeHelp.h"

//仮想鍵盤にストロークヘルプの情報を設定する
void StateCommonInfo::CopyStrokeHelpToVkbFaces(wchar_t ch) {
    SetCenterString(ch);
    ClearFaces();
    if (STROKE_HELP->copyStrokeHelpToVkbFacesStateCommon(ch, GetFaces(), FacesSize())) {
        SetStrokeHelpVkbLayout();
    } else {
        ClearVkbLayout();
    }
}

//仮想鍵盤にストロークヘルプの情報を設定する(outStringの先頭文字)
void StateCommonInfo::CopyStrokeHelpToVkbFaces() {
    if (!OutString().empty()) {
        CopyStrokeHelpToVkbFaces((wchar_t)GetFirstOutChar());
    }
}

