#pragma once

#include "string_type.h"
#include "Logger.h"

#include "deckey_id_defs.h"
#include "DeckeyToChars.h"
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

// inputFlags の詳細
enum class InputFlags
{
    // 打鍵されたキーの文字をそのまま返す
    DecodeKeyboardChar = 1,

    // 英大文字ロ－マ字による打鍵ガイドモード
    UpperRomanGuideMode = 2,

    // ロールオーバーされている打鍵
    RollOverStroke = 4,
};

// resultFlags の詳細
enum class ResultFlags
{
    // DECKEYを仮想キーに変換してアクティブウィンドウに対して送信する
    DeckeyToVkey = 1,

    // Ctrl-H や BS などの特殊キーをDECKEYで受け取る必要あり
    SpecialDeckeyRequired = 2,

    // 全角モード標識の表示が必要
    ShowZenkakuModeMarker = 4,

    // 全角モード標識の表示をやめる
    ClearZenkakuModeMarker = 8,

    // 出力履歴で、BS による削除を止める標識を追加する
    AppendBackspaceStopper = 32,

    // 出力履歴で、履歴検索のための文字列取得のストップ標識を付加する
    SetHistoryBlockFlag = 64,

    // 出力履歴で、交ぜ書きのための読み文字列取得のストップ標識を付加する
    SetMazegakiBlockFlag = 128,

    //// 待ち受け画面の切り替え
    //ToggleInitialStrokeHelp = 128,

    // 仮想鍵盤を移動しない
    DontMoveVirtualKeyboard = 0x100,

    // カタカナモード標識の表示が必要
    ShowKatakanaModeMarker = 0x200,

    // カタカナモード標識の表示をやめる
    ClearKatakanaModeMarker = 0x400,

    // 現在カタカナモード
    CurrentModeIsKatakana = 0x800,

    // 現在英数モード
    CurrentModeIsEisu = 0x1000,

    // 現在配列融合モードの入力中
    CurrentModeIsMultiStreamInput = 0x2000,

    // 溜っている出力文字列をフラッシュする
    FlushOutputString = 0x40000,

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

    // 部首合成ヘルプ
    BushuCompHelp = 5,

    // その他の状態
    OtherStatus = 6,

};

// 長い鍵盤の数
#define LONG_VKEY_NUM 10

//-------------------------------------------------------------------------------------------------
// デコーダ状態に共有される情報
// 入出力の両方で使われる
class StateCommonInfo {
    DECLARE_CLASS_LOGGER;

    // 打鍵されたデコーダキーの総カウント
    size_t totalDecKeyCount = 0;

    // 第1ストローク時のキーカウント
    size_t firstStrokeKeyCount = 0;

    // 現在のデコーダキー
    int currentDecKey = -1;

    // 直前のデコーダキー
    int prevDecKey = -1;

    // 同じデコーダキーが続けて入力された回数
    size_t sameDecKeyCount = 0;

    // 様々な結果フラグ (1 なら入力された DECKEY をVKEYに変換してアクティブWinに送る)
    UINT32 resultFlags = 0;

    // 次の入力で期待されるキー (第2ストローク、履歴入力でのEnter、など)
    ExpectedKeyType nextExpectedKeyType = ExpectedKeyType::None;

    // 文字入力中の打鍵数
    int strokeCount = 0;

    // 次の選択候補位置
    int nextSelectPos = 0;

    // DECKEYを発行した元の文字キーの列 (打鍵途中でスペースが打たれた時に送られる)
    MString origString;

    // 処理後に表示する仮想鍵盤のレイアウト
    VkbLayout layout;

    // 仮想鍵盤に出力される文字(列)
    String centerString;
    mchar_t faces[NORMAL_DECKEY_NUM] = { 0 };
    std::vector<String> longVkeyCandidates;

    // 交ぜ書きブロッカーの設定位置(末尾からのオフセット; SetMazegakiBlockFlag とともに用いられる)
    size_t mazeBlockerPos = 0;

    // キーボード文字へのデコードを行う
    bool decodeKeyboardChar = false;

    // 英大文字による入力ガイドモードか
    bool upperRomanGuideMode = false;

    // ロールオーバーされている打鍵か
    bool rollOverStroke = false;

public:
    StateCommonInfo()
        : layout(VkbLayout::None)
    {
        longVkeyCandidates.resize(LONG_VKEY_NUM);
    }

    inline void SetCurrentDecKey(int deckey) {
        currentDecKey = deckey;
    }

    inline int CurrentDecKey() const {
        return currentDecKey;
    }

    inline void IncrementTotalDecKeyCount() {
        ++totalDecKeyCount;
    }

    inline void SyncFirstStrokeKeyCount() {
        firstStrokeKeyCount = totalDecKeyCount;
    }

    inline size_t GetTotalDecKeyCount() {
        return totalDecKeyCount;
    }

    inline size_t GetFirstStrokeKeyCount() {
        return firstStrokeKeyCount;
    }

    void CountSameDecKey(int decKeyId) {
        if (prevDecKey == decKeyId) {
            ++sameDecKeyCount;
        } else {
            sameDecKeyCount = 1;
            prevDecKey = decKeyId;
        }
    }

    void ClearDecKeyCount() {
        sameDecKeyCount = 0;
        prevDecKey = -1;
    }

    inline size_t GetSameDecKeyCount() {
        return sameDecKeyCount;
    }

    int GetDeckey() {
        return prevDecKey;
    }

    wchar_t GetDeckeyChar() {
        return DECKEY_TO_CHARS->GetCharFromDeckey(prevDecKey);
    }

    // DECKEY 処理ごとに呼び出される初期化
    void ClearStateInfo() {
        currentDecKey = -1;
        resultFlags = 0;
        nextExpectedKeyType = ExpectedKeyType::None;
        strokeCount = 0;
        nextSelectPos = 0;
        layout = VkbLayout::None;
        centerString.clear();
        outStringProcDone = false;
        convertHiraganaToKatakana = false;
        decodeKeyboardChar = false;
        upperRomanGuideMode = false;
        rollOverStroke = false;
    }

    // デコーダのON時に呼び出される初期化
    void ClearAllStateInfo() {
        ClearRunningStates();
        ClearStateInfo();
        ClearDecKeyCount();
        IncrementTotalDecKeyCount();
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
    inline void SetDeckeyToVkeyFlag() { SetResultFlag(ResultFlags::DeckeyToVkey); }
    inline void SetSpecialDeckeyRequiredFlag() { SetResultFlag(ResultFlags::SpecialDeckeyRequired); }
    inline void SetZenkakuModeMarkerShowFlag() { SetResultFlag(ResultFlags::ShowZenkakuModeMarker); }
    inline void SetZenkakuModeMarkerClearFlag() { ResetResultFlag(ResultFlags::ShowZenkakuModeMarker); SetResultFlag(ResultFlags::ClearZenkakuModeMarker); }
    inline void SetKatakanaModeMarkerShowFlag() { SetResultFlag(ResultFlags::ShowKatakanaModeMarker); }
    inline void SetKatakanaModeMarkerClearFlag() { ResetResultFlag(ResultFlags::ShowKatakanaModeMarker); SetResultFlag(ResultFlags::ClearKatakanaModeMarker); }
    inline void SetAppendBackspaceStopperFlag() { SetResultFlag(ResultFlags::AppendBackspaceStopper); }
    inline void SetHistoryBlockFlag() { SetResultFlag(ResultFlags::SetHistoryBlockFlag); }
    //inline void SetMazegakiBlockFlag() { SetResultFlag(ResultFlags::SetMazegakiBlockFlag); }
    inline void SetBothHistoryBlockFlag() { SetResultFlag((UINT32)ResultFlags::AppendBackspaceStopper | (UINT32)ResultFlags::SetHistoryBlockFlag); }
    //inline void SetToggleInitialStrokeHelp() { SetResultFlag(ResultFlags::ToggleInitialStrokeHelp); }
    inline void SetDontMoveVirtualKeyboard() { SetResultFlag(ResultFlags::DontMoveVirtualKeyboard); }
    inline void SetCurrentModeIsKatakana() { SetResultFlag(ResultFlags::CurrentModeIsKatakana); }
    inline void SetCurrentModeIsEisu() { SetResultFlag(ResultFlags::CurrentModeIsEisu); }
    inline void ClearCurrentModeIsEisu() { ResetResultFlag(ResultFlags::CurrentModeIsEisu); }
    inline void SetCurrentModeIsMultiStreamInput() { SetResultFlag(ResultFlags::CurrentModeIsMultiStreamInput); }
    inline void ClearCurrentModeIsMultiStreamInput() { ResetResultFlag(ResultFlags::CurrentModeIsMultiStreamInput); }
    inline void SetFlushOutputString() { SetResultFlag(ResultFlags::FlushOutputString); }

    inline void SetWaiting2ndStroke() { nextExpectedKeyType = ExpectedKeyType::SecondStroke; }
    inline void SetMazeCandSelecting() { nextExpectedKeyType = ExpectedKeyType::MazeCandSelecting; }
    inline void SetHistCandSelecting() { nextExpectedKeyType = ExpectedKeyType::HistCandSelecting; }
    inline void SetAssocCandSelecting() { nextExpectedKeyType = ExpectedKeyType::AssocCandSelecting; }
    inline void SetOtherStatus() { nextExpectedKeyType = ExpectedKeyType::OtherStatus; }

    inline void SetStrokeCount(int cnt) { strokeCount = cnt; }

    inline void SetMazegakiBlockerPosition(size_t pos) {
        SetResultFlag(ResultFlags::SetMazegakiBlockFlag);
        mazeBlockerPos = pos;
    }

    inline size_t GetMazegakiBlockerPosition() {
        return mazeBlockerPos;
    }

    inline void SetDecodeKeyboardCharMode() { decodeKeyboardChar = true; }
    inline bool IsDecodeKeyboardCharMode() { return decodeKeyboardChar; }

    inline void SetUpperRomanGuideMode() { upperRomanGuideMode = true; }
    inline bool IsUpperRomanGuideMode() { return upperRomanGuideMode; }

    inline void SetRollOverStroke() { rollOverStroke = true; }
    inline bool IsRollOverStroke() { return rollOverStroke; }

    // 次の選択位置として pos を設定する。
    // pos < 0 なら未選択状態。ただし Horizontal なら先頭候補を優先候補として色付け
    // Vertical の場合は、pos == -1 なら先頭が優先候補、 pos < -1 なら色付けなし
    inline void SetWaitingCandSelect(int pos) {
        LOG_DEBUGH(_T("CALLED: pos={}"), pos);
        nextSelectPos = pos;
    }

    inline bool IsResultFlagOn(ResultFlags flag) const { return (resultFlags & (UINT32)flag) != 0; }
    inline bool IsResultFlagOn(UINT32 flag) const { return (resultFlags & flag) != 0; }
    inline bool IsDeckeyToVkey() const { return IsResultFlagOn(ResultFlags::DeckeyToVkey); }
    inline bool IsSpecialDeckeyRequired() const { return IsResultFlagOn(ResultFlags::SpecialDeckeyRequired); }
    inline bool IsAppendBackspaceStopper() const { return IsResultFlagOn(ResultFlags::AppendBackspaceStopper); }
    inline bool IsSetHistoryBlockFlag() const { return IsResultFlagOn(ResultFlags::SetHistoryBlockFlag); }
    inline bool IsSetMazegakiBlockFlag() const { return IsResultFlagOn(ResultFlags::SetMazegakiBlockFlag); }
    inline bool IsSetEitherHistoryBlockFlag() const { return IsResultFlagOn((UINT32)ResultFlags::AppendBackspaceStopper | (UINT32)ResultFlags::SetHistoryBlockFlag); }
    //inline bool IsSetToggleInitialStrokeHelp() const { return IsResultFlagOn(ResultFlags::ToggleInitialStrokeHelp); }

    inline bool IsWaiting2ndStroke() const { return nextExpectedKeyType == ExpectedKeyType::SecondStroke; }
    inline bool IsMazeCandSelecting() const { return nextExpectedKeyType == ExpectedKeyType::MazeCandSelecting; }
    inline bool IsHistCandSelecting() const { return nextExpectedKeyType == ExpectedKeyType::HistCandSelecting; }
    inline bool IsAssocCandSelecting() const { return nextExpectedKeyType == ExpectedKeyType::AssocCandSelecting; }
    inline bool IsOtherStatus() const { return nextExpectedKeyType == ExpectedKeyType::OtherStatus; }

//    inline void OutputDeckeyChar(/*int numBS = -1*/) { SetOutString(GetDeckeyChar()); }
//    inline void OutputOrigChar(int numBS = -1) { SetOutString(origString, numBS); }
//    inline void OutputOrigString(int numBS = -1) { SetOutString(origString, numBS); }

    inline void ClearOrigString() { origString.clear(); }
    inline void SetOrigString(mchar_t ch) { origString = ch; }
    inline void AppendOrigString(mchar_t ch) { origString.push_back(ch); }
    inline void PopOrigString() { if (!origString.empty()) origString.pop_back(); }
    inline const MString& OrigString() { return origString; }
    inline const mchar_t OrigChar() { return origString.empty() ? '\0' : origString[0]; }

    inline StringRef CenterString() { return centerString; }
    inline void SetCenterString(mchar_t ch) { centerString = wchar_t(ch); }
    inline void SetCenterString(StringRef ws) { centerString = ws; }

    inline mchar_t* GetFaces() { return faces; }
    inline size_t FacesSize() { return utils::array_length(faces); }

    inline VkbLayout GetLayout() { return layout; }
    inline int GetLayoutInt() { return (int)layout; }

    inline void SetVkbLayout(VkbLayout lo) { layout = lo;; }

    inline void ClearVkbLayout() { SetVkbLayout(VkbLayout::None); }

    inline void SetNormalVkbLayout() { SetVkbLayout(VkbLayout::Normal); }
    inline void SetStrokeHelpVkbLayout() { SetVkbLayout(VkbLayout::StrokeHelp); }

    inline const std::vector<String>& LongVkeyCandidates() { return longVkeyCandidates; }

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
    std::map<String, State*> runningStates;

public:
    // 指定の名前の状態が実行されているか
    // 既に実行されていれば、それを削除して true を返す
    // p == null なら削除のみ行う
    bool AddOrEraseRunningState(StringRef stateName, State* p);

    bool FindRunningState(StringRef stateName);

    void ClearRunningStates();

private:
    // 「最終的な出力履歴が整ったところで呼び出される処理」が実行済みか(実行不要の場合もこれをtrueにセットする)
    bool outStringProcDone = false;

public:
    inline bool IsOutStringProcDone() const { return outStringProcDone; }

    inline void ClearOutStringProcDone() { outStringProcDone = false; }

    inline void SetOutStringProcDone() { outStringProcDone = true; }

private:
    // Shift入力された平仮名をカタカナに変換する
    bool convertHiraganaToKatakana = false;

public:
    inline bool IsHiraganaToKatakana() const { return convertHiraganaToKatakana; }

    inline void ClearHiraganaToKatakana() { convertHiraganaToKatakana = false; }

    inline void SetHiraganaToKatakana() { convertHiraganaToKatakana = true; }

public:
    //仮想鍵盤にストロークヘルプの情報を設定する
    void CopyStrokeHelpToVkbFaces(wchar_t ch);

    //仮想鍵盤にストロークヘルプの情報を設定する(outStringの先頭文字)
    //void CopyStrokeHelpToVkbFaces();

public:
    static String GetVkbLayoutStr(VkbLayout);

    static std::unique_ptr<StateCommonInfo> Singleton;

    static void CreateSingleton();
};

#define STATE_COMMON (StateCommonInfo::Singleton)
#undef LOG_DEBUGH_FLAG
