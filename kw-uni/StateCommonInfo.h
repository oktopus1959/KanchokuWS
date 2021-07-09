#pragma once

#include "string_type.h"
#include "Logger.h"

#include "hotkey_id_defs.h"
#include "HotkeyToChars.h"
#include "OutputStack.h"

class State;

// 仮想鍵盤レイアウトタイプ
enum class VkbLayout {
    None = 0,
    TwoSides = 2,
    Vertical = 10,
    BushuCompHelp = 11,
    Horizontal = 20,
    Normal = 50,
    StrokeHelp = 51,
};

// resultFlags の詳細
enum class ResultFlags
{
    // HOTKEYを仮想キーに変換してアクティブウィンドウに対して送信する
    HotkeyToVkey = 1,

    // Ctrl-H や BS などの特殊キーをHOTKEYで受け取る必要あり
    SpecialHotkeyRequired = 2,

    // 全角モード標識の表示が必要
    ShowZenkakuModeMarker = 4,

    // 全角モード標識の表示をやめる
    ClearZenkakuModeMarker = 8,

    // 出力履歴で、BS による削除を止める標識を追加する
    AppendBackspaceStopper = 32,

    // 出力履歴で、履歴検索のための文字列取得のストップ標識を付加する
    SetHistoryBlockFlag = 64,

    //// 待ち受け画面の切り替え
    //ToggleInitialStrokeHelp = 128,

    // 仮想鍵盤を移動しない
    DontMoveVirtualKeyboard = 0x100,

    // カタカナモード標識の表示が必要
    ShowKatakanaModeMarker = 0x200,

    // カタカナモード標識の表示をやめる
    ClearKatakanaModeMarker = 0x400,

};

/// <summary>
/// 次の入力で期待されるキーの型
/// </summary>
enum class ExpectedKeyType
{
    // 特になし
    None = 0,

    // 第2ストローク
    SecondStroke = 1,

    // 交ぜ書き変換候補選択中
    MazeCandSelecting = 2,

    // 履歴候補選択中
    HistCandSelecting = 3,

    // 連想候補選択中
    AssocCandSelecting = 4,

    // その他の状態
    OtherStatus = 5,

};

// 長い鍵盤の数
#define LONG_VKEY_NUM 10

//-------------------------------------------------------------------------------------------------
// デコーダ状態に共有される情報
// 入出力の両方で使われる
class StateCommonInfo {
    DECLARE_CLASS_LOGGER;

    // 打鍵されたホットキーの総カウント
    size_t totalHotKeyCount = 0;

    // 直前のホットキー
    int prevHotKey = -1;

    // 同じホットキーが続けて入力された回数
    size_t sameHotKeyCount = 0;

    // 送信するBSの数(1 for surrogate pair)
    size_t numBackSpaces = 0;

    // 様々な結果フラグ (1 なら入力された HOTKEY をVKEYに変換してアクティブWinに送る)
    UINT32 resultFlags = 0;

    // 次の入力で期待されるキー (第2ストローク、履歴入力でのEnter、など)
    ExpectedKeyType nextExpectedKeyType = ExpectedKeyType::None;

    // 何か文字を直接入力するための現在の打鍵数
    int strokeCount = 0;

    // 次の選択候補位置
    int nextSelectPos = 0;

    // HOTKEYを発行した元の文字キーの列 (打鍵途中でスペースが打たれた時に送られる)
    MString origString;

    // 出力文字列 (UI側に送られる)
    MString outString;

    // 処理後に表示する仮想鍵盤のレイアウト
    VkbLayout layout;

    // 仮想鍵盤に出力される文字(列)
    wstring centerString;
    mchar_t faces[NUM_STROKE_HOTKEY] = { 0 };
    std::vector<wstring> longVkeyCandidates;

public:
    StateCommonInfo()
        : layout(VkbLayout::None)
    {
        longVkeyCandidates.resize(LONG_VKEY_NUM);
    }

    inline void IncrementTotalHotKeyCount() {
        ++totalHotKeyCount;
    }

    inline size_t GetTotalHotKeyCount() {
        return totalHotKeyCount;
    }

    void CountSameHotKey(int hotKeyId) {
        if (prevHotKey == hotKeyId) {
            ++sameHotKeyCount;
        } else {
            sameHotKeyCount = 1;
            prevHotKey = hotKeyId;
        }
    }

    void ClearHotKeyCount() {
        sameHotKeyCount = 0;
        prevHotKey = -1;
    }

    inline size_t GetSameHotKeyCount() {
        return sameHotKeyCount;
    }

    int GetHotkey() {
        return prevHotKey;
    }

    wchar_t GetHotkeyChar() {
        return HOTKEY_TO_CHARS->GetCharFromHotkey(prevHotKey);
    }

    // HOTKEY 処理ごとに呼び出される初期化
    void ClearStateInfo() {
        numBackSpaces = 0;
        resultFlags = 0;
        nextExpectedKeyType = ExpectedKeyType::None;
        strokeCount = 0;
        nextSelectPos = 0;
        outString.clear();
        layout = VkbLayout::None;
        centerString.clear();
        outStringProcDone = false;
        convertShiftedHiraganaToKatakana = false;
    }

    // デコーダのON時に呼び出される初期化
    void ClearAllStateInfo() {
        ClearRunningStates();
        ClearStateInfo();
        ClearHotKeyCount();
        IncrementTotalHotKeyCount();
    }

    void ClearFaces() {
        wchar_t spc = _T(" ")[0];
        for (size_t i = 0; i < utils::array_length(faces); ++i) {
            faces[i] = spc;
        }
    }

    inline UINT32 GetResultFlags() { return (UINT32)resultFlags; }
    inline int GetNextExpectedKeyType() { return (int)nextExpectedKeyType; }
    inline int GetStrokeCount() { return strokeCount; }
    inline int GetNextSelectPos() { return nextSelectPos; }

    inline void SetResultFlag(ResultFlags flag) { resultFlags |= (UINT32)flag; }
    inline void ResetResultFlag(ResultFlags flag) { resultFlags &= ~(UINT32)flag; }
    inline void SetResultFlag(UINT32 flag) { resultFlags |= flag; }
    inline void SetHotkeyToVkeyFlag() { SetResultFlag(ResultFlags::HotkeyToVkey); }
    inline void SetSpecialHotkeyRequiredFlag() { SetResultFlag(ResultFlags::SpecialHotkeyRequired); }
    inline void SetZenkakuModeMarkerShowFlag() { SetResultFlag(ResultFlags::ShowZenkakuModeMarker); }
    inline void SetZenkakuModeMarkerClearFlag() { ResetResultFlag(ResultFlags::ShowZenkakuModeMarker); SetResultFlag(ResultFlags::ClearZenkakuModeMarker); }
    inline void SetKatakanaModeMarkerShowFlag() { SetResultFlag(ResultFlags::ShowKatakanaModeMarker); }
    inline void SetKatakanaModeMarkerClearFlag() { ResetResultFlag(ResultFlags::ShowKatakanaModeMarker); SetResultFlag(ResultFlags::ClearKatakanaModeMarker); }
    inline void SetAppendBackspaceStopperFlag() { SetResultFlag(ResultFlags::AppendBackspaceStopper); }
    inline void SetHistoryBlockFlag() { SetResultFlag(ResultFlags::SetHistoryBlockFlag); }
    inline void SetBothHistoryBlockFlag() { SetResultFlag((UINT32)ResultFlags::AppendBackspaceStopper | (UINT32)ResultFlags::SetHistoryBlockFlag); }
    //inline void SetToggleInitialStrokeHelp() { SetResultFlag(ResultFlags::ToggleInitialStrokeHelp); }
    inline void SetDontMoveVirtualKeyboard() { SetResultFlag(ResultFlags::DontMoveVirtualKeyboard); }

    inline void SetWaiting2ndStroke() { nextExpectedKeyType = ExpectedKeyType::SecondStroke; }
    inline void SetMazeCandSelecting() { nextExpectedKeyType = ExpectedKeyType::MazeCandSelecting; }
    inline void SetHistCandSelecting() { nextExpectedKeyType = ExpectedKeyType::HistCandSelecting; }
    inline void SetAssocCandSelecting() { nextExpectedKeyType = ExpectedKeyType::AssocCandSelecting; }
    inline void SetOtherStatus() { nextExpectedKeyType = ExpectedKeyType::OtherStatus; }

    inline void SetStrokeCount(int cnt) { strokeCount = cnt; }

    // 次の選択位置として pos を設定する。
    // pos < 0 なら未選択状態。ただし Horizontal なら先頭候補を優先候補として色付け
    // Vertical の場合は、pos == -1 なら先頭が優先候補、 pos < -1 なら色付けなし
    inline void SetWaitingCandSelect(int pos) {
        LOG_DEBUGH(_T("CALLED: pos=%d"), pos);
        nextSelectPos = pos;
    }

    inline bool IsResultFlagOn(ResultFlags flag) const { return (resultFlags & (UINT32)flag) != 0; }
    inline bool IsResultFlagOn(UINT32 flag) const { return (resultFlags & flag) != 0; }
    inline bool IsHotkeyToVkey() const { return IsResultFlagOn(ResultFlags::HotkeyToVkey); }
    inline bool IsSpecialHotkeyRequired() const { return IsResultFlagOn(ResultFlags::SpecialHotkeyRequired); }
    inline bool IsAppendBackspaceStopper() const { return IsResultFlagOn(ResultFlags::AppendBackspaceStopper); }
    inline bool IsSetHistoryBlockFlag() const { return IsResultFlagOn(ResultFlags::SetHistoryBlockFlag); }
    inline bool IsSetEitherHistoryBlockFlag() const { return IsResultFlagOn((UINT32)ResultFlags::AppendBackspaceStopper | (UINT32)ResultFlags::SetHistoryBlockFlag); }
    //inline bool IsSetToggleInitialStrokeHelp() const { return IsResultFlagOn(ResultFlags::ToggleInitialStrokeHelp); }

    inline bool IsWaiting2ndStroke() const { return nextExpectedKeyType == ExpectedKeyType::SecondStroke; }
    inline bool IsMazeCandSelecting() const { return nextExpectedKeyType == ExpectedKeyType::MazeCandSelecting; }
    inline bool IsHistCandSelecting() const { return nextExpectedKeyType == ExpectedKeyType::HistCandSelecting; }
    inline bool IsAssocCandSelecting() const { return nextExpectedKeyType == ExpectedKeyType::AssocCandSelecting; }
    inline bool IsOtherStatus() const { return nextExpectedKeyType == ExpectedKeyType::OtherStatus; }

    inline void SetOutString(const mchar_t ch, int numBS = -1) {
        outString = ch;
        if (numBS >= 0) numBackSpaces = numBS;
    }
    inline void SetOutString(const MString& s, int numBS = -1) {
        outString = s;
        if (numBS >= 0) numBackSpaces = numBS;
    }
    inline const MString& OutString() const { return outString; }
    inline mchar_t GetFirstOutChar() const { return outString.empty() ? 0 : outString[0]; }
    inline mchar_t GetLastOutChar() const { return outString.empty() ? 0 : outString.back(); }

    inline void OutputHotkeyChar(/*int numBS = -1*/) { SetOutString(GetHotkeyChar()); }
    //inline void OutputOrigChar(int numBS = -1) { SetOutString(origString, numBS); }
    inline void OutputOrigString(int numBS = -1) { SetOutString(origString, numBS); }

    inline void ClearOrigString() { origString.clear(); }
    inline void SetOrigString(mchar_t ch) { origString = ch; }
    inline void AppendOrigString(mchar_t ch) { origString.push_back(ch); }
    inline void PopOrigString() { if (!origString.empty()) origString.pop_back(); }
    inline const MString& OrigString() { return origString; }

    inline void SetBackspaceNum(size_t numBS) { numBackSpaces = numBS; }
    inline size_t GetBackspaceNum() const { return numBackSpaces; }

    inline const wstring& CenterString() { return centerString; }
    inline void SetCenterString(mchar_t ch) { centerString = wchar_t(ch); }
    inline void SetCenterString(const wstring& ws) { centerString = ws; }

    inline mchar_t* GetFaces() { return faces; }
    inline size_t FacesSize() { return utils::array_length(faces); }

    inline VkbLayout GetLayout() { return layout; }
    inline int GetLayoutInt() { return (int)layout; }

    inline void SetVkbLayout(VkbLayout lo) { layout = lo;; }

    inline void ClearVkbLayout() { SetVkbLayout(VkbLayout::None); }

    inline void SetNormalVkbLayout() { SetVkbLayout(VkbLayout::Normal); }
    inline void SetStrokeHelpVkbLayout() { SetVkbLayout(VkbLayout::StrokeHelp); }

    inline const std::vector<wstring>& LongVkeyCandidates() { return longVkeyCandidates; }

private:
    void setCenterString(mchar_t center);

    inline void setCenterString(const MString& center) { centerString = to_wstr(center); }

    // 仮想鍵盤と受渡しするための文字をセットする
    // lo : レイアウト, fcs:左右鍵盤にセットする文字列
    void setVirtualKeyboardStrings(VkbLayout lo, const mchar_t* fcs);

    // 仮想鍵盤と受渡しするための文字をセットする
    // lo : レイアウト, longKeys: 縦列または横列鍵盤にセットする文字列
    void setVirtualKeyboardStrings(VkbLayout lo, const std::vector<MString>& longKeys, size_t pos);

public:
    // 仮想鍵盤と受渡しするための文字をセットする
    // lo : レイアウト, center: 中央鍵盤にセットする文字; fcs:左右鍵盤にセットする文字列
    inline void SetVirtualKeyboardStrings(VkbLayout lo, mchar_t center, const mchar_t* fcs) {
        setCenterString(center);
        setVirtualKeyboardStrings(lo, fcs);
    }

    // 仮想鍵盤と受渡しするための文字をセットする
    // lo : レイアウト, center: 中央鍵盤にセットする文字列; longKeys: 縦列または横列鍵盤にセットする文字列
    inline void SetVirtualKeyboardStrings(VkbLayout lo, const MString& center, const std::vector<MString>& longKeys, size_t pos = 0) {
        setCenterString(center);
        setVirtualKeyboardStrings(lo, longKeys, pos);
    }

private:
    // 実行されている状態
    std::map<wstring, State*> runningStates;

public:
    // 指定の名前の状態が実行されているか
    // 既に実行されていれば、それを削除して true を返す
    bool AddOrEraseRunningState(const wstring& stateName, State* p);

    void ClearRunningStates() { runningStates.clear(); };

private:
    // 「最終的な出力履歴が整ったところで呼び出される処理」が実行済みか(実行不要の場合もこれをtrueにセットする)
    bool outStringProcDone = false;

public:
    inline bool IsOutStringProcDone() const { return outStringProcDone; }

    inline void ClearOutStringProcDone() { outStringProcDone = false; }

    inline void SetOutStringProcDone() { outStringProcDone = true; }

private:
    // Shift入力された平仮名をカタカナに変換する
    bool convertShiftedHiraganaToKatakana = false;

public:
    inline bool IsShiftedHiraganaToKatakana() const { return convertShiftedHiraganaToKatakana; }

    inline void ClearShiftedHiraganaToKatakana() { convertShiftedHiraganaToKatakana = false; }

    inline void SetShiftedHiraganaToKatakana() { convertShiftedHiraganaToKatakana = true; }


public:
    static std::unique_ptr<StateCommonInfo> Singleton;

    static void CreateSingleton();
};

#define STATE_COMMON (StateCommonInfo::Singleton)
#undef LOG_DEBUGH_FLAG
