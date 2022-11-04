//#include "pch.h"
#include "Logger.h"
#include "string_type.h"
#include "file_utils.h"
#include "path_utils.h"
#include "misc_utils.h"

#include "KanchokuIni.h"
#include "Constants.h"
#include "deckey_id_defs.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "Node.h"
#include "StayState.h"
#include "OutputStack.h"
#include "StrokeHelp.h"
#include "BushuComp/BushuComp.h"

#include "History.h"
#include "HistoryDic.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughHistory)

#if 1 || defined(_DEBUG)
#define IS_LOG_DEBUGH_ENABLED true
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

// 縦列鍵盤または横列鍵盤の数
#define LONG_KEY_NUM 10

#define CAND_LEN_THRESHOLD 10

namespace {
    // -------------------------------------------------------------------
    // 履歴入力リストのクラス
    class HistCandidates {
        DECLARE_CLASS_LOGGER;

        // 履歴入力候補のリスト
        HistResultList histCands;

        // 候補単語列
        //std::vector<MString> histWords;
        std::vector<HistResult> histResults;

        HistResult emptyResult;

        // 履歴検索中か
        bool isHistInSearch = false;

        // 現在、履歴選択に使われているキー
        MString currentKey;

        int currentLen = 0;

        // 選択位置 -- -1 は未選択状態を表す
        mutable int selectPos = -1;

        // 未選択状態に戻す
        inline void resetSelectPos() {
            selectPos = -1;
        }

        inline void setSelectPos(size_t n) const {
            size_t x = min(histCands.Size(), SETTINGS->histHorizontalCandMax);
            selectPos = n >= 0 && n < x ? n : -1;
        }

        // 選択位置をインクリメント //(一周したら未選択状態に戻る)
        inline void incSelectPos() const {
            size_t x = min(histCands.Size(), SETTINGS->histHorizontalCandMax);
            selectPos = selectPos < 0 ? 0 : x <= 0 ? -1 : (selectPos + 1) % x;
        }

        // 選択位置をデクリメント //(一周したら未選択状態に戻る)
        inline void decSelectPos() const {
            int x = min((int)histCands.Size(), SETTINGS->histHorizontalCandMax);
            selectPos = selectPos <= 0 ? x - 1 : x <=0 ? -1 : (selectPos - 1) % x;
        }

        inline int getSelectPos() const {
            return selectPos;
        }

        inline bool isSelecting() const {
            return selectPos > 0 && selectPos < (int)histResults.size();
        }

        inline const HistResult getSelectedHist() const {
            int n = getSelectPos();
            int x = min((int)histCands.Size(), SETTINGS->histHorizontalCandMax);
            return n >= 0 && n < x ? histCands.GetNthHist(n) : emptyResult;
        }

        inline const MString& getSelectedWord() const {
            int n = getSelectPos();
            return n >= 0 && n < (int)histResults.size() ? histResults[n].Word : EMPTY_MSTR;
        }

    public:
        // 履歴検索キー設定をクリアする
        void ClearKeyInfo() {
            histCands.ClearKeyInfo();
            currentKey.clear();
            isHistInSearch = false;
        }

        bool IsHistInSearch() {
            _LOG_DEBUGH(_T("CALLED: HistInSearch=%s"), BOOL_TO_WPTR(isHistInSearch));
            return isHistInSearch;
        }

        const MString& GetOrigKey() {
            return histCands.GetOrigKey();
        }

        // 指定のキーで始まる候補を取得する (len > 0 なら指定の長さの候補だけを取得, len < 0 なら Abs(len)以下の長さの候補を取得)
        const std::vector<HistResult>& GetCandidates(const MString& key, bool bCheckMinKeyLen, int len) {
            isHistInSearch = true;
            DelayedPushFrontSelectedWord();
            currentLen = len;
            histCands = HISTORY_DIC->GetCandidates(key, currentKey, bCheckMinKeyLen, len);  // ここで currentKey は変更される (currentKey = resultKey)
            histResults.clear();
            utils::append(histResults, histCands.GetHistories());
            _LOG_DEBUGH(_T("cands num=%d, new currentKey=%s"), histResults.size(), MAKE_WPTR(currentKey));
            return histResults;
        }

        const std::vector<MString> GetCandWords(const MString& key, bool bCheckMinKeyLen, int len) {
            _LOG_DEBUGH(_T("CALLED: key=%s, bCheckMinKeyLen=%s, len=%d"), MAKE_WPTR(key), BOOL_TO_WPTR(bCheckMinKeyLen), len);
            GetCandidates(key, bCheckMinKeyLen, len);
            return GetCandWords();
        }

        // 取得済みの候補列を返す
        //const std::vector<HistResult>& GetCandidates() const {
        //    return histResults;
        //}

        const std::vector<MString> GetCandWords() const {
            _LOG_DEBUGH(_T("CALLED"));
            std::vector<MString> words;
            utils::transform_append(histResults, words, [](const HistResult& res) { return res.Word; });
            return words;
        }

        const MString& GetCurrentKey() const {
            return currentKey;
        }

        // 次の履歴を選択する
        const HistResult GetNext() const {
            incSelectPos();
            return getSelectedHist();
        }

        // 前の履歴を選択する
        const HistResult GetPrev() const {
            decSelectPos();
            return getSelectedHist();
        }

        // 選択された単語を取得する
        const MString& GetSelectedWord() const {
            _LOG_DEBUGH(_T("CALLED: selectPos=%d"), selectPos);
            return getSelectedWord();
        }

        // 選択されている位置を返す -- 未選択状態なら -1を返す
        int GetSelectPos() const {
            _LOG_DEBUGH(_T("CALLED: nextSelect=%d"), selectPos);
            return getSelectPos();
        }

        // 選択位置を初期化(未選択状態)する
        const HistResult ClearSelectPos() {
            _LOG_DEBUGH(_T("CALLED: nextSelect=%d"), selectPos);
            resetSelectPos();
            return emptyResult;
        }

        // 候補が選択されていれば、それを使用履歴の先頭にpushする -- selectPos は未選択状態に戻る
        void DelayedPushFrontSelectedWord() {
            _LOG_DEBUGH(_T("ENTER"));
            if (isSelecting()) {
                HISTORY_DIC->UseWord(GetSelectedWord());
            }
            ClearSelectPos();
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // 取得済みの履歴入力候補リストから指定位置の候補を返す
        // 選択された候補は使用履歴の先頭に移動する
        const HistResult SelectNth(size_t n) {
            _LOG_DEBUGH(_T("ENTER: n=%d, histResults=%d"), n, histResults.size());
            ClearSelectPos();
            if (n >= histResults.size()) {
                _LOG_DEBUGH(_T("LEAVE: empty"));
                return emptyResult;
            }

            HistResult result = histResults[n];
            HISTORY_DIC->UseWord(result.Word);
            GetCandidates(currentKey, false, currentLen);
            _LOG_DEBUGH(_T("LEAVE: OrigKey=%s, Key=%s, Word=%s"), MAKE_WPTR(result.OrigKey), MAKE_WPTR(result.Key), MAKE_WPTR(result.Word));
            return result;
        }

        inline void DeleteNth(size_t n) {
            _LOG_DEBUGH(_T("ENTER"));
            DelayedPushFrontSelectedWord();
            if (n < histCands.Size()) {
                HISTORY_DIC->DeleteEntry(histCands.GetNthWord(n));
                GetCandidates(currentKey, false, currentLen);
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

    public:
        static std::unique_ptr<HistCandidates> Singleton;
    };
    DEFINE_CLASS_LOGGER(HistCandidates);

    std::unique_ptr<HistCandidates> HistCandidates::Singleton;

#define HIST_CAND (HistCandidates::Singleton)


#define CAND_DISP_LONG_VKEY_LEN  20

    // -------------------------------------------------------------------
    // 履歴入力機能状態基底クラス
    class HistoryStateBase {
        DECLARE_CLASS_LOGGER;

        const Node* pNode_ = 0;

        tstring BaseName;

    protected:
        // 履歴入力候補のリスト
        //HistCandidates histCands;

        int candLen = 0;
        size_t candDispVerticalPos = 0;
        size_t candDispHorizontalPos = 0;

        bool bDeleteMode = false;

    public:
        // コンストラクタ
        HistoryStateBase(const Node* pN)
            : pNode_(pN), BaseName(logger.ClassNameT()) {
            LOG_INFO(_T("CALLED"));
        }

        ~HistoryStateBase() { };

#define BASE_NAME_PTR (BaseName.c_str())

    protected:
        // 履歴検索文字列の遡及ブロッカーをセット
        void setBlocker() {
            _LOG_DEBUGH(_T("CALLED: %s"), BASE_NAME_PTR);
            STATE_COMMON->SetAppendBackspaceStopperFlag();
            STATE_COMMON->SetHistoryBlockFlag();
            STATE_COMMON->ClearDecKeyCount();
        }

        // 選択された履歴候補を出力(これが呼ばれた時点で、すでにキーの先頭まで巻き戻すように plannedNumBS が設定されていること)
        void setOutString(const HistResult& result) {
            _LOG_DEBUGH(_T("CALLED: result.OrigKey=%s, result.Key=%s, result.Word=%s, keyLen=%d, wildKey=%s, prevOutStr=%s, prevKey=%s, plannedNumBS=%d"), \
                MAKE_WPTR(result.OrigKey), MAKE_WPTR(result.Key), MAKE_WPTR(result.Word), result.KeyLen(), BOOL_TO_WPTR(result.WildKey), \
                MAKE_WPTR(HISTORY_STAY_NODE->GetPrevOutString()), MAKE_WPTR(HISTORY_STAY_NODE->GetPrevKey()), STATE_COMMON->GetBackspaceNum());

            MString outStr = result.Word;
            MString outKey = result.Key;
            if (outStr.empty()) {
                // 未選択状態だったら、出力文字列を元に戻す
                outKey = HISTORY_STAY_NODE->GetPrevKey();
                outStr = HISTORY_STAY_NODE->GetPrevOutString();
                if (outStr.empty()) outStr = outKey;
            } else {
                size_t pos = outStr.find(VERT_BAR);     // '|' を含むか
                _LOG_DEBUGH(_T("pos=%d, histMapKeyMaxLength=%d"), pos, SETTINGS->histMapKeyMaxLength);
                if (pos <= SETTINGS->histMapKeyMaxLength) {
                    // histMap候補
                    if (pos + 1 < outStr.size() && outStr[pos + 1] == VERT_BAR) ++pos;  // '||' だったら1つ進める(HistoryDicで既に対処済みなので、多分、ここでは不要のはず)
                    if (pos + 1 < outStr.size() && outStr[pos + 1] == HASH_MARK) ++pos;  // '|#' だったら1つ進める(# はローマ字変換の印)
                    outStr = utils::safe_substr(outStr, pos + 1);
                    _LOG_DEBUGH(_T("histMap: outStr=%s, outKey=%s"), MAKE_WPTR(outStr), MAKE_WPTR(outKey));
                    if (outKey.size() > pos) {
                        // 変換キー('|'より前の部分)よりも入力された文字列キーが長い場合(例: "にら|韮" に対して「にらちされ」が入力されたような場合)
                        outStr.append(utils::safe_substr(outKey, pos));
                        _LOG_DEBUGH(_T("histMap: outKey Appended: outStr=%s"), MAKE_WPTR(outStr));
                    }
                }
                if (outKey.size() < result.OrigKey.size()) {
                    // 変換キーが元キーよりも短い場合(「あわなだ」が元キーで「わなだ」が変換キーのケース)
                    auto leadStr = result.OrigKey.substr(0, result.OrigKey.size() - outKey.size());
                    outStr = leadStr + outStr;
                    outKey = leadStr + outKey;
                    _LOG_DEBUGH(_T("histMap: leadStr Appended: leadStr=%s"), MAKE_WPTR(leadStr));
                }
            }
            _LOG_DEBUGH(_T("outStr=%s, outKey=%s"), MAKE_WPTR(outStr), MAKE_WPTR(outKey));

            STATE_COMMON->SetOutString(outStr);
            HISTORY_STAY_NODE->SetPrevHistState(outStr, outKey);

            //_LOG_DEBUGH(_T("prevOutString=%s, isPrevHistKeyUsed=%s"), MAKE_WPTR(HISTORY_STAY_NODE->GetPrevOutString()), BOOL_TO_WPTR(HISTORY_STAY_NODE->IsPrevHistKeyUsed()));
            _LOG_DEBUGH(_T("prevOutString=%s"), MAKE_WPTR(HISTORY_STAY_NODE->GetPrevOutString()));
        }

        // 前回の履歴検索の出力と現在の出力文字列(改行以降)の末尾を比較し、同じであれば前回の履歴検索のキーを取得する
        // この時、出力スタックは、キーだけを残し、追加出力部分は巻き戻し予約される(numBackSpacesに値をセット)
        // 前回が空キーだった場合は、返値も空キーになるので、HISTORY_STAY_NODE->PrevKeyLen == 0 かどうかで前回と同じキーであるか否かを判断すること
        // ここに来る場合には、以下の3つの状態がありえる:
        // ①まだ履歴検索がなされていない状態
        // ②検索が実行されたが、出力文字列にはキーだけが表示されている状態
        // ③横列のどれかの候補が選択されて出力文字列に反映されている状態
        MString getLastHistKeyAndRewindOutput() {
            // 前回の履歴検索の出力
            //bool bPrevHistUsed = HISTORY_STAY_NODE->IsPrevHistKeyUsed();
            const auto& prevKey = HISTORY_STAY_NODE->GetPrevKey();
            const auto& prevOut = HISTORY_STAY_NODE->GetPrevOutString();
            //_LOG_DEBUGH(_T("isPrevHistUsed=%s, prevOut=%s, prevKey=%s"), BOOL_TO_WPTR(bPrevHistUsed), MAKE_WPTR(prevOut), MAKE_WPTR(prevKey));
            _LOG_DEBUGH(_T("prevOut=%s, prevKey=%s"), MAKE_WPTR(prevOut), MAKE_WPTR(prevKey));

            if (prevKey.empty()) {
                // ①まだ履歴検索がなされていない状態
                // empty key を返す
                _LOG_DEBUGH(_T("NOT YET HIST USED"));
            } else if (prevOut.empty()) {
                // ②検索が実行されたが、出力文字列にはキーだけが表示されている状態
                _LOG_DEBUGH(_T("CURRENT: SetOutString(str=%s, numBS=%d)"), MAKE_WPTR(prevKey), prevKey.size());
                STATE_COMMON->SetOutString(prevKey, prevKey.size());
                HISTORY_STAY_NODE->SetPrevHistState(prevKey, prevKey);
                _LOG_DEBUGH(_T("CURRENT: prevKey=%s"), MAKE_WPTR(prevKey));
            } else {
                // ③横列のどれかの候補が選択されて出力文字列に反映されている状態
                _LOG_DEBUGH(_T("REVERT and NEW HIST: SetOutString(str=%s, numBS=%d)"), MAKE_WPTR(prevKey), prevOut.size());
                STATE_COMMON->SetOutString(prevKey, prevOut.size());
                HISTORY_STAY_NODE->SetPrevHistState(prevKey, prevKey);
                _LOG_DEBUGH(_T("REVERT and NEW HIST: prevKey=%s"), MAKE_WPTR(prevKey));
            }

            _LOG_DEBUGH(_T("last Japanese key=%s"), MAKE_WPTR(prevKey));
            return prevKey;
        }

        // 前回の履歴選択の出力と現在の出力文字列(改行以降)の末尾が同一であるか
        bool isLastHistOutSameAsCurrentOut() {
            // 前回の履歴選択の出力
            MString prevOut = HISTORY_STAY_NODE->GetPrevOutString();
            // 出力スタックから、上記と同じ長さの末尾文字列を取得
            auto lastJstr = OUTPUT_STACK->GetLastJapaneseStr<MString>(prevOut.size());
            bool result = !prevOut.empty() && lastJstr == prevOut;
            _LOG_DEBUGH(_T("RESULT: %s: prevOut=%s, lastJapaneseStr=%s"), BOOL_TO_WPTR(result), MAKE_WPTR(prevOut), MAKE_WPTR(lastJstr));
            return result;
        }

        // 履歴入力候補を鍵盤にセットする
        void setCandidatesVKB(VkbLayout layout, const std::vector<MString>& cands, const MString& key, bool bShrinkWord = false) {
            _LOG_DEBUGH(_T("ENTER: layout=%d, cands.size()=%d, key=%s"), layout, cands.size(), MAKE_WPTR(key));
            auto mark = pNode_->getString();
            size_t maxlen = 0;
            for (const auto& w : cands) { if (maxlen < w.size()) maxlen = w.size(); }

            _LOG_DEBUGH(_T("maxlen=%d, candDispVerticalPos=%d, candDispHorizontalPos=%d"), maxlen, candDispVerticalPos, candDispHorizontalPos);

            if (maxlen <= CAND_DISP_LONG_VKEY_LEN) {
                candDispVerticalPos = 0;
            } else if (candDispVerticalPos >= maxlen) {
                candDispVerticalPos -= CAND_DISP_LONG_VKEY_LEN;
            }
            size_t p = candDispHorizontalPos;
            if (p >= cands.size()) {
                p = p >= LONG_KEY_NUM ? p - LONG_KEY_NUM : 0;
                candDispHorizontalPos = p;
            }
            size_t q = p + (layout == VkbLayout::Horizontal ? SETTINGS->histHorizontalCandMax : LONG_KEY_NUM);
            if (q > cands.size()) q = cands.size();

            _LOG_DEBUGH(_T("p=%d, q=%d, candDispVerticalPos=%d, candDispHorizontalPos=%d"), p, q, candDispVerticalPos, candDispHorizontalPos);

            std::vector<MString> words;
            for (size_t i = p; i < q; ++i) {
                words.push_back(
                    bShrinkWord ? utils::str_shrink(cands[i], CAND_DISP_LONG_VKEY_LEN)
                    : utils::safe_substr(cands[i], candDispVerticalPos, CAND_DISP_LONG_VKEY_LEN));
            }
            STATE_COMMON->SetVirtualKeyboardStrings(layout, mark + utils::str_shrink(key, 5), words);

            if (HIST_CAND->GetSelectPos() >= 0) STATE_COMMON->SetDontMoveVirtualKeyboard();

            _LOG_DEBUGH(_T("LEAVE"));
        }

        // 中央鍵盤の色付け、矢印キー有効、縦列鍵盤の色付けあり
        void setHistSelectColorAndBackColor() {
            // 「候補選択」の色で中央鍵盤の色付け
            STATE_COMMON->SetHistCandSelecting();
            // 矢印キーを有効にして、背景色の色付けあり
            _LOG_DEBUGH(_T("Set Unselected"));
            STATE_COMMON->SetWaitingCandSelect(-1);
        }

        // 中央鍵盤の文字出力と色付け、矢印キー有効、縦列鍵盤の色付けなし
        void setCenterStringAndBackColor(const wstring& ws) {
            // 中央鍵盤の文字出力
            STATE_COMMON->SetCenterString(ws);
            // 「その他の状態」の色で中央鍵盤の色付け
            STATE_COMMON->SetOtherStatus();
            // 矢印キーを有効にして、背景色の色付けなし
            _LOG_DEBUGH(_T("Set Unselected=-2"));
            STATE_COMMON->SetWaitingCandSelect(-2);
        }

        // モード標識文字を返す
        mchar_t GetModeMarker() {
            return utils::safe_front(pNode_->getString());
        }

    };
    DEFINE_CLASS_LOGGER(HistoryStateBase);

    // -------------------------------------------------------------------
#define NAME_PTR (Name.c_str())

    // 履歴入力機能状態クラス
    class HistoryState : public State, public HistoryStateBase {
        DECLARE_CLASS_LOGGER;

        bool bWaitingForNum = false;

    public:
        // コンストラクタ
        HistoryState(HistoryNode* pN) : HistoryStateBase(pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~HistoryState() { };

        // 機能状態に対して生成時処理を実行する
        // ここに来る場合には、以下の3つの状態がありえる:
        // ①まだ履歴検索がなされていない状態
        // ②検索が実行されたが、出力文字列にはキーだけが表示されている状態
        // ③横列のどれかの候補が選択されて出力文字列に反映されている状態
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER"));

            if (!HISTORY_DIC) return false;

            // 過去の履歴候補選択の結果を反映しておく
            HIST_CAND->DelayedPushFrontSelectedWord();

            // 末尾1文字の登録
            auto ws = OUTPUT_STACK->GetLastOutputStackStrUptoNL(1);
            if (ws.size() == 1 && ws[0] >= 0x100 && !STROKE_HELP->Find(ws[0])) {
                // 末尾文字がストローク可能文字でなければ登録する
                HISTORY_DIC->AddNewEntry(ws);
            }

            MString key;
            if (HIST_CAND->IsHistInSearch()) {
                _LOG_DEBUGH(_T("History in Search"));
                // 検索を実行済みなら、前回の履歴検索キー取得と出力スタックの巻き戻し予約(numBackSpacesに値をセット)
                key = getLastHistKeyAndRewindOutput();
            }
            if (key.empty()) {
                _LOG_DEBUGH(_T("History key is EMPTY: CALL OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey()"));
                // まだ検索していなければ、出力文字列から、検索キーを取得(ひらがな交じりやASCIIもキーとして取得する)
                key = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>();
                //key = STATE_COMMON->GetLastKanjiOrKatakanaKey();
            }
            _LOG_DEBUGH(_T("new Japanese key=%s"), MAKE_WPTR(key));

            // 履歴入力候補の取得
            candLen = 0;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);

            // 検索キーの設定
            HISTORY_STAY_NODE->SetPrevHistKeyState(HIST_CAND->GetOrigKey());

            // 未選択状態にセットする
            _LOG_DEBUGH(_T("Set Unselected"));
            STATE_COMMON->SetWaitingCandSelect(-1);

            // 前状態にチェインする
            _LOG_DEBUGH(_T("LEAVE: Chain"));
            return true;
        }

        // 最終的な出力履歴が整ったところで呼び出される処理
        void DoOutStringProc() {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);

            //if (pNext) pNext->DoOutStringProc();
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(), HIST_CAND->GetCurrentKey());
            if (bDeleteMode) {
                // 中央鍵盤の文字出力と色付け、矢印キー有効、縦列鍵盤の色付けなし
                setCenterStringAndBackColor(_T("削除"));
            } else if (bWaitingForNum) {
                // 中央鍵盤の文字出力と色付け、矢印キー有効、縦列鍵盤の色付けなし
                setCenterStringAndBackColor(_T("文字数指定"));
            } else {
                // 矢印キーを有効にして、先頭候補の背景色を色付け
                setHistSelectColorAndBackColor();
            }
            STATE_COMMON->SetOutStringProcDone();
            _LOG_DEBUGH(_T("LEAVE: %s, IsOutStringProcDone=%s"), NAME_PTR, BOOL_TO_WPTR(STATE_COMMON->IsOutStringProcDone()));
        }

        // 履歴検索を初期化する状態か
        bool IsHistoryReset() {
            bool result = (pNext && pNext->IsHistoryReset());
            _LOG_DEBUGH(_T("CALLED: %s: result=%s"), NAME_PTR, BOOL_TO_WPTR(result));
            return result;
        }

         // Strokeキー を処理する
        void handleStrokeKeys(int deckey) {
            _LOG_DEBUGH(_T("ENTER: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
            if (deckey == SETTINGS->histDelDeckeyId) {
                // 削除モードに入る
                _LOG_DEBUGH(_T("LEAVE: DELETE MODE"));
                bDeleteMode = true;
                return;
            }
            if (bDeleteMode) {
                // 削除モードのとき
                if (deckey == SETTINGS->histDelDeckeyId) {
                    bDeleteMode = false;
                    _LOG_DEBUGH(_T("LEAVE DELETE MODE"));
                } else if (deckey < STROKE_SPACE_DECKEY) {
                    HIST_CAND->DeleteNth((deckey % LONG_KEY_NUM) + candDispHorizontalPos);
                    bDeleteMode = false;
                    //const wstring key = STATE_COMMON->GetLastKanjiOrKatakanaKey();
                    // ひらがな交じりやASCIIもキーとして取得する
                    const auto key = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>();
                    _LOG_DEBUGH(_T("key=%s"), MAKE_WPTR(key));
                    candLen = 0;
                    HIST_CAND->GetCandidates(key, false, candLen);
                    _LOG_DEBUGH(_T("LEAVE DELETE MODE"));
                }
                return;
            }
            if (deckey == SETTINGS->histNumDeckeyId) {
                // 履歴文字数指定
                _LOG_DEBUGH(_T("ENTER: NUM MODE"));
                bWaitingForNum = true;
                return;
            }
            if (bWaitingForNum) {
                // 履歴文字数指定のとき
                bWaitingForNum = false;
                if (deckey >= 0 && deckey < CAND_LEN_THRESHOLD) {
                    // '1'〜'0' (1〜10文字のものだけを表示)
                    _LOG_DEBUGH(_T("ENTER JUST LEN MODE"));
                    //指定の長さのものだけを残して仮想鍵盤に表示
                    candDispHorizontalPos = 0;
                    candDispVerticalPos = 0;
                    auto key = HIST_CAND->GetCurrentKey();
                    candLen = (deckey + 1) % LONG_KEY_NUM;
                    setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);
                }
                _LOG_DEBUGH(_T("LEAVE: forNum"));
                return;
            }

            // 下記は不要。いったん出力履歴バッファをクリアしてから履歴入力を行えばよいため
            //if (deckey == DECKEY_STROKE_44) {
            //    // '@' : 全使用リストから取得する
            //    //setCandidatesVKB(HIST_CAND->GetCandidates(_T("")), _T(""));
            //    HIST_CAND->GetCandidates(_T(""));
            //    return;
            //}

            // 候補の選択
            _LOG_DEBUGH(_T("HIST_CAND->SelectNth()"));
            HistResult result = HIST_CAND->SelectNth((deckey >= STROKE_SPACE_DECKEY ? 0 : deckey % LONG_KEY_NUM) + candDispHorizontalPos);
            _LOG_DEBUGH(_T("result.Word=%s, result.KeyLen=%d"), MAKE_WPTR(result.Word), result.KeyLen());
            if (!result.Word.empty()) {
                getLastHistKeyAndRewindOutput();    // 前回の履歴検索キー取得と出力スタックの巻き戻し予約(numBackSpacesに値をセット)
                setOutString(result);               // 選択された候補の出力
                HIST_CAND->ClearKeyInfo();
                //if (result.KeyLen() >= 2) STATE_COMMON->SetHistoryBlockFlag();  // 1文字の場合は履歴検索の対象となる
                // 出力された履歴に対しては、履歴の再検索の対象としない(変換形履歴の場合を除く)
                if (result.Word.find(VERT_BAR) == MString::npos) STATE_COMMON->SetHistoryBlockFlag();
            }
            handleKeyPostProc();
            _LOG_DEBUGH(_T("LEAVE"));
        }

        //void handleSpaceKey() {
        //    LOG_DEBUG(_T("CALLED"));
        //    STATE_COMMON->OutputOrigString();
        //    handleKeyPostProc();
        //}

        // 機能キーだったときの一括処理(false を返すと、この後、個々の機能キーのハンドラが呼ばれる)
        bool handleFunctionKeys(int deckey) {
            _LOG_DEBUGH(_T("CALLED"));
            switch (deckey) {
            case LEFT_ARROW_DECKEY:
            case RIGHT_ARROW_DECKEY:
            case UP_ARROW_DECKEY:
            case DOWN_ARROW_DECKEY:
                return false;
            default:
                if (bDeleteMode || bWaitingForNum) {
                    // 矢印キーでなくて、削除モードまたは数字入力モードなら、それを抜ける
                    bDeleteMode = false;
                    bWaitingForNum = false;
                    return true;
                } else {
                    return false;
                }
            }
        }

        void handleDownArrow() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            //candDispVerticalPos += CAND_DISP_LONG_VKEY_LEN;
            candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
            auto key = HIST_CAND->GetCurrentKey();
            //指定の長さのものだけを残して仮想鍵盤に表示
            candLen = candLen < 0 ? abs(candLen) : (candLen + 1) % CAND_LEN_THRESHOLD;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);
            return;
        }

        void handleUpArrow() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            //if (candDispVerticalPos >= CAND_DISP_LONG_VKEY_LEN)
            //    candDispVerticalPos -= CAND_DISP_LONG_VKEY_LEN;
            //else
            //    candDispVerticalPos = 0;
            candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
            auto key = HIST_CAND->GetCurrentKey();
            //指定の長さのものだけを残して仮想鍵盤に表示
            candLen = candLen < 0 ? abs(candLen) - 1 : (candLen == 0 ? CAND_LEN_THRESHOLD : candLen) - 1;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);
            return;
        }

        void handleLeftArrow() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            if (candDispHorizontalPos >= LONG_KEY_NUM)
                candDispHorizontalPos -= LONG_KEY_NUM;
            else
                candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
        }

        void handleRightArrow() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            candDispHorizontalPos += LONG_KEY_NUM;
            candDispVerticalPos = 0;
        }

        // RET/Enter の処理 -- 第1候補を返す
        void handleEnter() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleStrokeKeys(20);   // 'a'
            handleKeyPostProc();
        }

        //// Shift+Space の処理 -- 第1候補を返す
        //void handleShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleEnter();
        //}

        //// Ctrl+Space の処理 -- 第1候補を返す
        //void handleCtrlSpace() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleEnter();
        //}

        // NextCandTrigger の処理 -- 第1候補を返す
        void handleNextCandTrigger() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleEnter();
        }

        // PrevCandTrigger の処理 -- 第1候補を返す
        void handlePrevCandTrigger() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleEnter();
        }


        // FullEscapeの処理 -- 処理のキャンセル
        void handleFullEscape() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            HistoryStateBase::setBlocker();
            handleKeyPostProc();
        }

        // Ctrl-H/BS の処理 -- 処理のキャンセル
        void handleBS() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

        // ストロークのクリア -- 処理のキャンセル
        void handleClearStroke() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

    protected:
        void handleKeyPostProc() {
            _LOG_DEBUGH(_T("CALLED: handleKeyPostProc"));
            HISTORY_STAY_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();
            STATE_COMMON->ClearVkbLayout();
            //STATE_COMMON->RemoveFunctionState();
            bUnnecessary = true;
        }

    };
    DEFINE_CLASS_LOGGER(HistoryState);

    // -------------------------------------------------------------------
    // 2～3文字履歴機能状態クラス
    class HistoryFewCharsState : public HistoryState {
        DECLARE_CLASS_LOGGER;
    public:
        // コンストラクタ
        HistoryFewCharsState(HistoryFewCharsNode* pN) : HistoryState(pN) {
            LOG_INFO(_T("CALLED"));
            Name = logger.ClassNameT();
        }

        ~HistoryFewCharsState() { };

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("CALLED"));

            if (!HISTORY_DIC) return false;

            // 前回履歴キーのクリア
            HISTORY_STAY_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();

            // 2～3文字履歴の取得
            MString key;
            candLen = -3;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);

            // 前状態にチェインする
            return true;
        }

    };
    DEFINE_CLASS_LOGGER(HistoryFewCharsState);

    // -------------------------------------------------------------------
    // 1文字履歴機能状態クラス
    class HistoryOneCharState : public HistoryState {
        DECLARE_CLASS_LOGGER;
    public:
        // コンストラクタ
        HistoryOneCharState(HistoryOneCharNode* pN) : HistoryState(pN) {
            LOG_INFO(_T("CALLED"));
            Name = logger.ClassNameT();
        }

        ~HistoryOneCharState() { };

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("CALLED"));

            if (!HISTORY_DIC) return false;

            // 前回履歴キーのクリア
            HISTORY_STAY_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();

            // 1文字履歴の取得
            MString key;
            candLen = 1;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);

            // 前状態にチェインする
            return true;
        }

    };
    DEFINE_CLASS_LOGGER(HistoryOneCharState);

    // -------------------------------------------------------------------
    // 履歴入力(常駐)機能状態クラス
    class HistoryStayStateImpl : public HistoryStayState, public HistoryStateBase {
        DECLARE_CLASS_LOGGER;

        //MString prevKey;

        int candSelectDeckey = -1;


        /// 今回の履歴候補選択ホットキーを保存
        /// これにより、DoOutStringProc() で継続的な候補選択のほうに処理が倒れる
        void setCandSelectIsCalled() { candSelectDeckey = STATE_COMMON->GetDeckey(); }

        // 状態管理のほうで記録している最新ホットキーと比較し、今回が履歴候補選択キーだったか
        bool wasCandSelectCalled() { return candSelectDeckey >= 0 && candSelectDeckey == STATE_COMMON->GetDeckey(); }

        // 後続状態で出力スタックが変更された可能性あり
        bool maybeEditedBySubState = false;

        // Shift+Space等による候補選択が可能か
        bool bCandSelectable = false;

    public:
        // コンストラクタ
        HistoryStayStateImpl(HistoryStayNode* pN) : HistoryStateBase(pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~HistoryStayStateImpl() { };

        //// 常駐状態か
        //bool IsStay() const {
        //    return true;
        //}

        // 状態の再アクティブ化
        void Reactivate() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            if (pNext) pNext->Reactivate();
            // ちょっと下以の意図が不明
            //maybeEditedBySubState = true;
            //DoOutStringProc();
            // 初期化という意味で、下記のように変更しておく(2021/5/31)
            maybeEditedBySubState = false;
            bCandSelectable = false;
            _LOG_DEBUGH(_T("bCandSelectable=False"));
            HISTORY_STAY_NODE->ClearPrevHistState();     // まだ履歴検索が行われていないということを表す
            HIST_CAND->ClearKeyInfo();      // まだ履歴検索が行われていないということを表す
        }

        // 履歴検索を初期化する状態か
        bool IsHistoryReset() {
            bool result = (pNext && pNext->IsHistoryReset());
            _LOG_DEBUGH(_T("CALLED: %s: result=%s"), NAME_PTR, BOOL_TO_WPTR(result));
            return result;
        }

    public:
        // Enter時の新しい履歴の追加
        void AddNewHistEntryOnEnter() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            if (HISTORY_DIC) {
                HIST_CAND->DelayedPushFrontSelectedWord();
                STATE_COMMON->SetBothHistoryBlockFlag();
                if (OUTPUT_STACK->isLastOutputStackCharKanjiOrKatakana()) {
                    // これまでの出力末尾が漢字またはカタカナであるなら
                    // 出力履歴の末尾の漢字列またはカタカナ列を取得して、それを履歴辞書に登録する
                    HISTORY_DIC->AddNewEntry(OUTPUT_STACK->GetLastKanjiOrKatakanaStr<MString>());
                } else if (OUTPUT_STACK->isLastOutputStackCharHirakana()) {
                    //// 漢字・カタカナ以外なら5〜10文字の範囲でNグラム登録する
                    //HISTORY_DIC->AddNgramEntries(OUTPUT_STACK->GetLastJapaneseStr<MString>(10));
                }
            }
        }

        // 何か文字が入力されたときの新しい履歴の追加
        void AddNewHistEntryOnSomeChar() {
            auto ch1 = STATE_COMMON->GetFirstOutChar();
            auto ch2 = OUTPUT_STACK->GetLastOutputStackChar();
            if (ch1 != 0 && HISTORY_DIC) {
                // 今回の出力の先頭が漢字以外であり、これまでの出力末尾が漢字であるか、
                if ((!utils::is_kanji(ch1) && (utils::is_kanji(ch2))) ||
                    // または、今回の出力の先頭がカタカナ以外であり、これまでの出力末尾がカタカナであるなら、
                    (!utils::is_katakana(ch1) && (utils::is_katakana(ch2)))) {
                    LOG_DEBUG(_T("Call AddNewEntry"));
                    // 出力履歴の末尾の漢字列またはカタカナ列を取得して、それを履歴辞書に登録する
                    HISTORY_DIC->AddNewEntry(OUTPUT_STACK->GetLastKanjiOrKatakanaStr<MString>());
                } else if (utils::is_japanese_char_except_nakaguro((wchar_t)ch1)) {
                    //LOG_DEBUG(_T("Call AddNgramEntries"));
                    //// 出力末尾が日本語文字なら5〜10文字の範囲でNグラム登録する
                    //HISTORY_DIC->AddNgramEntries(OUTPUT_STACK->GetLastJapaneseStr<MString>(9) + ch1);
                }
            }
        }

        // 文字列を変換して出力、その後、履歴の追加
        void SetTranslatedOutString(const MString& outStr, size_t rewritableLen, int numBS = -1) {
            _LOG_DEBUGH(_T("ENTER: %s: outStr=%s, rewritableLen=%d, numBS=%d"), NAME_PTR, MAKE_WPTR(outStr), rewritableLen, numBS);
            if (pNext) {
                MString xlatStr = pNext->TranslateString(outStr);
                _LOG_DEBUGH(_T("%s: SetOutStringWithRewritableLen(%s, %d, %d)"), NAME_PTR, MAKE_WPTR(xlatStr), xlatStr == outStr ? rewritableLen : 0, numBS);
                STATE_COMMON->SetOutStringWithRewritableLen(xlatStr, xlatStr == outStr ? rewritableLen : 0, numBS);
            } else {
                if (!SETTINGS->autoBushuComp || !BUSHU_COMP_NODE->ReduceByAutoBushu(outStr)) {
                    _LOG_DEBUGH(_T("%s: SetOutStringWithRewritableLen(%s, %d, %d)"), NAME_PTR, MAKE_WPTR(outStr), rewritableLen, numBS);
                    STATE_COMMON->SetOutStringWithRewritableLen(outStr, rewritableLen, numBS);
                }
            }
            AddNewHistEntryOnSomeChar();
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
        }

        void handleFullEscapeStayState() {
            handleFullEscape();
        }

    protected:
        // 事前チェック
        void DoPreCheck() {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
            maybeEditedBySubState = false;
            // 常駐モード
            //if (pNext && pNext->GetName().find(_T("History")) == wstring::npos)
            if (IsHistoryReset()) {
                // 履歴機能ではない次状態(StrokeStateなど)があれば、それが何かをしているはずなので、戻ってきたら新たに候補の再取得を行うために、ここで maybeEditedBySubState を true にセットしておく
                //prevKey.clear();
                _LOG_DEBUGH(_T("Set Reinitialized=true"));
                maybeEditedBySubState = true;
                HISTORY_STAY_NODE->ClearPrevHistState();    // まだ履歴検索が行われていない状態にしておく
                HIST_CAND->ClearKeyInfo();      // まだ履歴検索が行われていないということを表す
            }
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
        }

        //// ノードから生成した状態を後接させ、その状態を常駐させる(ここでは 0 が渡ってくるはず)
        //void ChainAndStay(Node* ) {
        //    // 前状態にチェインする
        //    LOG_DEBUG(_T("Chain: %s"), NAME_PTR);
        //    STATE_COMMON->ChainMe();
        //}

    private:
        bool matchWildcardKey(const MString& cand, const MString& wildKey) {
            _LOG_DEBUGH(_T("cand=%s, wildKey=%s"), MAKE_WPTR(cand), MAKE_WPTR(wildKey));
            auto keys = utils::split(wildKey, '*');
            if (keys.size() == 2) {
                const MString& key0 = keys[0];
                const MString& key1 = keys[1];
                if (!key0.empty() && !key1.empty()) {
                    if (cand.size() >= key0.size() + key1.size()) {
                        // wildcard key なので、'|' の語尾は気にしなくてよい('|' を含むやつにはマッチさせないので)
                        _LOG_DEBUGH(_T("startsWithWildKey(%s, %s, 0) && utils::endsWithWildKey(%s, %s)"), MAKE_WPTR(cand), MAKE_WPTR(key0), MAKE_WPTR(cand), MAKE_WPTR(key1));
                        return utils::startsWithWildKey(cand, key0, 0) && utils::endsWithWildKey(cand, key1);
                    }
                }
            }
            return false;
        }

        // 直前キーが空でなく、候補が1つ以上あり、第1候補または第2候補がキー文字列から始まっていて、かつ同じではないか
        // たとえば、直前に「竈門」を交ぜ書きで出力したような場合で、これまでの出力履歴が「竈門」だけなら履歴候補の表示はやらない。
        // 他にも「竈門炭治郎」の出力履歴があるなら、履歴候補の表示をする。
        bool isHotCandidateReady(const MString& prevKey, const std::vector<MString>& cands) {
            size_t gobiLen = SETTINGS->histMapGobiMaxLength;
            size_t candsSize = cands.size();
            MString cand0 = candsSize > 0 ? cands[0] : MString();
            MString cand1 = candsSize > 1 ? cands[1] : MString();
            _LOG_DEBUGH(_T("ENTER: prevKey=%s, cands.size=%d, cand0=%s, cand1=%s, gobiLen=%d"), MAKE_WPTR(prevKey), candsSize, MAKE_WPTR(cand0), MAKE_WPTR(cand1), gobiLen);

            bool result = (!prevKey.empty() &&
                           ((!cand0.empty() && (utils::startsWithWildKey(cand0, prevKey, gobiLen) || matchWildcardKey(cand0, prevKey)) && cand0 != prevKey) ||
                            (!cand1.empty() && (utils::startsWithWildKey(cand1, prevKey, gobiLen) || matchWildcardKey(cand1, prevKey)) && cand1 != prevKey)));

            _LOG_DEBUGH(_T("LEAVE: result=%s"), BOOL_TO_WPTR(result));
            return result;
        }

        // 一時的にこのフラグを立てることにより、履歴検索を行わないようになる
        bool bNoHistTemporary = false;

        // 一時的にこのフラグを立てることにより、自動モードでなくても履歴検索が実行されるようになる
        bool bManualTemporary = false;

        // 前回の履歴検索との比較、新しい履歴検索の開始 (bManual=trueなら自動モードでなくても履歴検索を実行する)
        void historySearch(bool bManual) {
            LOG_INFO(_T("ENTER: auto=%s, manual=%s, maybeEditedBySubState=%s, histInSearch=%s"), \
                BOOL_TO_WPTR(SETTINGS->autoHistSearchEnabled), BOOL_TO_WPTR(bManual), BOOL_TO_WPTR(maybeEditedBySubState), BOOL_TO_WPTR(HIST_CAND->IsHistInSearch()));
            if (!SETTINGS->autoHistSearchEnabled && !bManual) {
                // 履歴検索状態ではないので、前回キーをクリアしておく。
                // こうしておかないと、自動履歴検索OFFのとき、たとえば、
                // 「エッ」⇒Ctrl+Space⇒「エッセンス」⇒Esc⇒「エッ」⇒「セ」追加⇒出力「エッセ」、キー=「エッ」のまま⇒再検索⇒「エッセセンス」となる
                _LOG_DEBUGH(_T("Not Hist Search mode: Clear PrevKey"));
                HISTORY_STAY_NODE->ClearPrevHistState();
                HIST_CAND->ClearKeyInfo();
            } else {
                // 履歴検索可能状態である
                _LOG_DEBUGH(_T("Auto or Manual"));
                // 前回の履歴選択の出力と現在の出力文字列(改行以降)の末尾を比較する。
                // たとえば前回「中」で履歴検索し「中納言家持」が履歴出力されており、現在の出力スタックが「・・・中納言家持」なら true が返る
                bool bSameOut = !bManual && isLastHistOutSameAsCurrentOut();
                LOG_INFO(_T("bSameOut=%s, maybeEditedBySubState=%s, histInSearch=%s"), \
                    BOOL_TO_WPTR(bSameOut), BOOL_TO_WPTR(maybeEditedBySubState), BOOL_TO_WPTR(HIST_CAND->IsHistInSearch()));
                if (bSameOut && !maybeEditedBySubState && HIST_CAND->IsHistInSearch()) {
                    // 前回履歴出力が取得できた、つまり出力文字列の末尾が前回の履歴選択と同じ出力だったら、出力文字列をキーとした履歴検索はやらない
                    // これは、たとえば「中」で履歴検索し、「中納言家持」を選択した際に、キーとして返される「中納言家持」の末尾の「持」を拾って「持統天皇」を履歴検索してしまうことを防ぐため。
                    _LOG_DEBUGH(_T("Do nothing: prevOut is same as current out"));
                } else {
                    // ただし、交ぜ書き変換など何か後続状態により出力がなされた場合(maybeEditedBySubState)は、履歴検索を行う。
                    _LOG_DEBUGH(_T("DO HistSearch: prevOut is diff with current out or maybeEditedBySubState or not yet HistInSearch"));
                    // 現在の出力文字列は履歴選択したものではなかった
                    // キー取得用 lambda
                    auto keyGetter = []() {
                        // まず、ワイルドカードパターンを試す
                        auto key9 = OUTPUT_STACK->GetLastOutputStackStrUptoBlocker(9);
                        _LOG_DEBUGH(_T("HistSearch: key9=%s"), MAKE_WPTR(key9));
                        if (key9.empty() || key9.back() == ' ') {
                            return EMPTY_MSTR;
                        }
                        auto items = utils::split(key9, '*');
                        size_t nItems = items.size();
                        if (nItems >= 2) {
                            size_t len0 = items[nItems - 2].size();
                            size_t len1 = items[nItems - 1].size();
                            if (len0 > 0 && len1 > 0 && len1 <= 4) {
                                _LOG_DEBUGH(_T("WILDCARD: key=%s"), MAKE_WPTR(utils::last_substr(key9, len1 + 5)));
                                return utils::last_substr(key9, len1 + 5);
                            }
                        }
                        // ワイルドカードパターンでなかった
                        _LOG_DEBUGH(_T("NOT WILDCARD, GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey"));
                        // 出力文字から、ひらがな交じりやASCIIもキーとして取得する
                        auto jaKey = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>();
                        _LOG_DEBUGH(_T("HistSearch: jaKey=%s"), MAKE_WPTR(jaKey));
                        if (jaKey.size() >= 9 || (!jaKey.empty() && is_ascii_char(jaKey.back()))) {
                            // 同種の文字列で9文以上取れたか、またはASCIIだったので、これをキーとする
                            return jaKey;
                        }
                        // 最終的には末尾8文字をキーとする('*' は含まない。'?' は含んでいる可能性あり)
                        _LOG_DEBUGH(_T("HistSearch: tail_substr(key9, 8)=%s"), MAKE_WPTR(utils::tail_substr(key9, 8)));
                        return utils::tail_substr(key9, 8);
                    };
                    // キーの取得
                    MString key = keyGetter();
                    _LOG_DEBUGH(_T("HistSearch: LastJapaneseKey=%s"), MAKE_WPTR(key));
                    if (!key.empty()) {
                        // キーが取得できた
                        //bool isAscii = is_ascii_char((wchar_t)utils::safe_back(key));
                        _LOG_DEBUGH(_T("HistSearch: PATH 8: key=%s, prevKey=%s, maybeEditedBySubState=%s"),
                            MAKE_WPTR(key), MAKE_WPTR(HISTORY_STAY_NODE->GetPrevKey()), BOOL_TO_WPTR(maybeEditedBySubState));
                        auto histCandsChecker = [this](const std::vector<MString>& words, const MString& ky) {
                            _LOG_DEBUGH(_T("HistSearch: CANDS CHECKER: words.size()=%d, key=%s"), words.size(), MAKE_WPTR(ky));
                            if (words.empty() || (words.size() == 1 && (words[0].empty() || words[0] == ky))) {
                                _LOG_DEBUGH(_T("HistSearch: CANDS CHECKER-A: cands size <= 1"));
                                // 候補が1つだけで、keyに一致するときは履歴選択状態にはしない
                            } else {
                                _LOG_DEBUGH(_T("HistSearch: CANDS CHECKER-B"));
                                setCandidatesVKB(VkbLayout::Horizontal, words, ky);
                            }
                        };
                        if (key != HISTORY_STAY_NODE->GetPrevKey() || maybeEditedBySubState || bManual) {
                            _LOG_DEBUGH(_T("HistSearch: PATH 9: different key"));
                            //bool bCheckMinKeyLen = !bManual && utils::is_hiragana(key[0]);       // 自動検索かつ、キー先頭がひらがなならキー長チェックをやる
                            bool bCheckMinKeyLen = !bManual;                                     // 自動検索ならキー長チェックをやる
                            histCandsChecker(HIST_CAND->GetCandWords(key, bCheckMinKeyLen, 0), key);
                            // キーが短くなる可能性があるので再取得
                            key = HIST_CAND->GetCurrentKey();
                            _LOG_DEBUGH(_T("HistSearch: PATH 10: currentKey=%s"), MAKE_WPTR(key));
                        } else {
                            // 前回の履歴検索と同じキーだった
                            _LOG_DEBUGH(_T("HistSearch: PATH 11: Same as prev hist key"));
                            histCandsChecker(HIST_CAND->GetCandWords(), key);
                        }
                    }
                    _LOG_DEBUGH(_T("HistSearch: SetPrevHistKeyState(key=%s)"), MAKE_WPTR(key));
                    HISTORY_STAY_NODE->SetPrevHistKeyState(key);
                    _LOG_DEBUGH(_T("DONE HistSearch"));
                }
            }

            // この処理は、GUI側で候補の背景色を変更するために必要
            if (isHotCandidateReady(HISTORY_STAY_NODE->GetPrevKey(), HIST_CAND->GetCandWords())) {
                _LOG_DEBUGH(_T("PATH 14"));
                // 何がしかの文字出力があり、それをキーとする履歴候補があった場合 -- 未選択状態にセットする
                _LOG_DEBUGH(_T("Set Unselected"));
                STATE_COMMON->SetWaitingCandSelect(-1);
                bCandSelectable = true;
                _LOG_DEBUGH(_T("bCandSelectable=True"));
            }
            maybeEditedBySubState = false;

            LOG_INFO(_T("LEAVE"));
        }

    public:
        // 最終的な出力履歴が整ったところで呼び出される処理
        void DoOutStringProc() {
            LOG_INFO(_T("\nENTER: %s: %s"), NAME_PTR, OUTPUT_STACK->OutputStackBackStrForDebug(10).c_str());
            LOG_INFO(_T("PATH 2: bCandSelectable=%s"), BOOL_TO_WPTR(bCandSelectable));

            if (bCandSelectable && wasCandSelectCalled()) {
                LOG_INFO(_T("PATH 3: by SelectionKey"));
                // 履歴選択キーによる処理だった場合
                if (bCandSelectable) {
                    LOG_INFO(_T("PATH 4"));
                    // この処理は、GUI側で候補の背景色を変更するのと矢印キーをホットキーにするために必要
                    LOG_INFO(_T("Set selectedPos=%d"), HIST_CAND->GetSelectPos());
                    STATE_COMMON->SetWaitingCandSelect(HIST_CAND->GetSelectPos());
                }
            } else {
                LOG_INFO(_T("PATH 5: by Other Input"));
                // その他の文字出力だった場合
                HIST_CAND->DelayedPushFrontSelectedWord();
                bCandSelectable = false;

                LOG_INFO(_T("PATH 6: bCandSelectable=%s, bNoHistTemporary=%s"), BOOL_TO_WPTR(bCandSelectable), BOOL_TO_WPTR(bNoHistTemporary));
                if (OUTPUT_STACK->isLastOutputStackCharBlocker()) {
                    LOG_INFO(_T("PATH 7"));
                    HISTORY_DIC->ClearNgramSet();
                }

                // 前回の履歴検索との比較、新しい履歴検索の開始
                if (bNoHistTemporary) {
                    // 一時的に履歴検索が不可になっている場合は、キーと出力文字列を比較して、異った状態になっていたら可に戻す
                    MString prevKey = HISTORY_STAY_NODE->GetPrevKey();
                    MString outStr = OUTPUT_STACK->GetLastOutputStackStrUptoBlocker(prevKey.size());
                    bNoHistTemporary = OUTPUT_STACK->GetLastOutputStackStrUptoBlocker(prevKey.size()) == prevKey;
                    LOG_INFO(_T("PATH 8: bNoHistTemporary=%s: prevKey=%s, outStr=%s"), BOOL_TO_WPTR(bNoHistTemporary), MAKE_WPTR(prevKey), MAKE_WPTR(outStr));
                }

                LOG_INFO(_T("PATH 9: bNoHistTemporary=%s"), BOOL_TO_WPTR(bNoHistTemporary));
                if (!bNoHistTemporary) {
                    historySearch(bManualTemporary);
                }
                //bNoHistTemporary = false;
                //bManualTemporary = false;
            }

            bNoHistTemporary = false;
            bManualTemporary = false;

            LOG_INFO(_T("LEAVE: %s\n"), NAME_PTR);
        }

        // (Ctrl or Shift)+Space の処理 -- 履歴検索の開始、次の候補を返す
        void handleNextOrPrevCandTrigger(bool bNext) {
            LOG_INFO(_T("\nCALLED: %s: bCandSelectable=%s, selectPos=%d, bNext=%s"), NAME_PTR, BOOL_TO_WPTR(bCandSelectable), HIST_CAND->GetSelectPos(), BOOL_TO_WPTR(bNext));
            // これにより、前回のEnterによる改行点挿入やFullEscapeによるブロッカーフラグが削除される⇒(2021/12/18)workしなくなっていたので、いったん削除
            //OUTPUT_STACK->clearFlagAndPopNewLine();
            // 今回、履歴選択用ホットキーだったことを保存
            setCandSelectIsCalled();

            if (!bCandSelectable) {
                // 履歴候補選択可能状態でなければ、前回の履歴検索との比較、新しい履歴検索の開始
                historySearch(true);
            }
            if (bCandSelectable) {
                LOG_INFO(_T("CandSelectable: bNext=%s"), BOOL_TO_WPTR(bNext));
                if (bNext)
                    getNextCandidate();
                else
                    getPrevCandidate();
            } else {
                LOG_INFO(_T("NOP"));
            }
        }

        //// Shift+Space の処理 -- 履歴検索の開始、次の候補を返す
        //void handleShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleNextOrPrevCandTrigger(true);
        //}

        //// Ctrl+Space の処理 -- 履歴検索の開始、次の候補を返す
        //void handleCtrlSpace() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleNextOrPrevCandTrigger(true);
        //}

        //// Ctrl+Shift+Space の処理 -- 履歴検索の開始、前の候補を返す
        //void handleCtrlShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleNextOrPrevCandTrigger(false);
        //}

        // NextCandTrigger の処理 -- 履歴検索の開始、次の候補を返す
        void handleNextCandTrigger() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleNextOrPrevCandTrigger(true);
        }

        // PrevCandTrigger の処理 -- 履歴検索の開始、前の候補を返す
        void handlePrevCandTrigger() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleNextOrPrevCandTrigger(false);
        }

        // ↓の処理 -- 次候補を返す
        void handleDownArrow() {
            _LOG_DEBUGH(_T("CALLED: %s: bCandSelectable=%s"), NAME_PTR, BOOL_TO_WPTR(bCandSelectable));
            if (SETTINGS->useArrowKeyToSelectCandidate && bCandSelectable) {
                setCandSelectIsCalled();
                getNextCandidate();
            } else {
                _LOG_DEBUGH(_T("candSelectDeckey=%x"), candSelectDeckey);
                HistoryStayState::handleDownArrow();
            }
        }

        // ↑の処理 -- 前候補を返す
        void handleUpArrow() {
            _LOG_DEBUGH(_T("CALLED: %s: bCandSelectable=%s"), NAME_PTR, BOOL_TO_WPTR(bCandSelectable));
            if (SETTINGS->useArrowKeyToSelectCandidate && bCandSelectable) {
                setCandSelectIsCalled();
                getPrevCandidate();
            } else {
                _LOG_DEBUGH(_T("candSelectDeckey=%x"), candSelectDeckey);
                HistoryStayState::handleUpArrow();
            }
        }

        // FullEscapeの処理 -- 履歴選択状態から抜けて、履歴検索文字列の遡及ブロッカーをセット
        void handleFullEscape() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            HIST_CAND->DelayedPushFrontSelectedWord();
            HistoryStateBase::setBlocker();
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // Unblock の処理 -- 改行やブロッカーの除去
        void handleUnblock() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            // ブロッカー設定済みならそれを解除する
            OUTPUT_STACK->clearFlagAndPopNewLine();
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // Tab の処理 -- 次の候補を返す
        void handleTab() {
            _LOG_DEBUGH(_T("CALLED: %s: bCandSelectable=%s"), NAME_PTR, BOOL_TO_WPTR(bCandSelectable));
            if (SETTINGS->selectHistCandByTab && bCandSelectable) {
                setCandSelectIsCalled();
                getNextCandidate();
            } else {
                HistoryStayState::handleTab();
            }
        }

        // ShiftTab の処理 -- 前の候補を返す
        void handleShiftTab() {
            _LOG_DEBUGH(_T("CALLED: %s: bCandSelectable=%s"), NAME_PTR, BOOL_TO_WPTR(bCandSelectable));
            if (SETTINGS->selectHistCandByTab && bCandSelectable) {
                setCandSelectIsCalled();
                getPrevCandidate();
            } else {
                HistoryStayState::handleShiftTab();
            }
        }

        // Ctrl-H/BS の処理 -- 履歴検索の初期化
        void handleBS() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            HISTORY_STAY_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();
            HistoryStayState::handleBS();
        }

        // DecoderOff の処理
        void handleDecoderOff() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            // Enter と同じ扱いにする
            AddNewHistEntryOnEnter();
            HistoryStayState::handleDecoderOff();
        }
        
        // RET/Enter の処理
        void handleEnter() {
            _LOG_DEBUGH(_T("CALLED: %s: bCandSelectable=%s, selectPos=%d"), NAME_PTR, BOOL_TO_WPTR(bCandSelectable), HIST_CAND->GetSelectPos());
            if (SETTINGS->selectFirstCandByEnter && bCandSelectable && HIST_CAND->GetSelectPos() < 0) {
                // 選択可能状態かつ候補未選択なら第1候補を返す。
                _LOG_DEBUGH(_T("CALL: getNextCandidate()"));
                getNextCandidate();
            } else if (bCandSelectable && HIST_CAND->GetSelectPos() >= 0) {
                _LOG_DEBUGH(_T("CALL: HISTORY_STAY_NODE->ClearPrevHistState(); HIST_CAND->ClearKeyInfo(); bManualTemporary = true"));
                // どれかの候補が選択されている状態なら、それを確定し、履歴キーをクリアしておく
                HISTORY_STAY_NODE->ClearPrevHistState();
                HIST_CAND->ClearKeyInfo();
                // 一時的にマニュアル操作フラグを立てることで、DoOutStringProc() から historySearch() を呼ぶときに履歴再検索が実行されるようにする
                bManualTemporary = true;
            } else {
                // それ以外は通常のEnter処理
                _LOG_DEBUGH(_T("CALL: AddNewHistEntryOnEnter()"));
                AddNewHistEntryOnEnter();
                HistoryStayState::handleEnter();
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        //// Ctrl-J の処理 -- 選択可能状態かつ候補未選択なら第1候補を返す。候補選択済みなら確定扱い
        //void handleCtrlJ() {
        //    _LOG_DEBUGH(_T("\nCALLED: %s: selectPos=%d"), NAME_PTR, HIST_CAND->GetSelectPos());
        //    //setCandSelectIsCalled();
        //    if (bCandSelectable) {
        //        if (HIST_CAND->GetSelectPos() < 0) {
        //            // 選択可能状態かつ候補未選択なら第1候補を返す。
        //            getNextCandidate();
        //        } else {
        //            // 確定させる
        //            HIST_CAND->DelayedPushFrontSelectedWord();
        //            HistoryStateBase::setBlocker();
        //        }
        //    } else {
        //        // Enterと同じ扱い
        //        AddNewHistEntryOnEnter();
        //        HistoryStayState::handleCtrlJ();
        //    }
        //}

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            _LOG_DEBUGH(_T("CALLED: %s, bCandSelectable=%s, SelectPos=%d"), NAME_PTR, BOOL_TO_WPTR(bCandSelectable), HIST_CAND->GetSelectPos());
            if (bCandSelectable && HIST_CAND->GetSelectPos() >= 0) {
                // どれかの候補が選択されている状態なら、選択のリセット
                resetCandSelect();
                // 一時的にマニュアル操作フラグを立てることで、DoOutStringProc() から historySearch() を呼ぶときに履歴再検索が実行されるようにする
                bManualTemporary = true;
            } else {
                // 一時的にこのフラグを立てることにより、履歴検索を行わないようにする
                bNoHistTemporary = true;
                // Esc処理が必要なものがあればそれをやる。なければアクティブウィンドウにEscを送る
                StayState::handleEsc();
                //// 何も候補が選択されていない状態なら履歴選択状態から抜ける
                //STATE_COMMON->SetHistoryBlockFlag();
                //HistoryStayState::handleEsc();
                //// 完全に抜ける
                //handleFullEscape();
            }
        }

        //// Ctrl-U
        //void handleCtrlU() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    STATE_COMMON->SetBothHistoryBlockFlag();
        //    State::handleCtrlU();
        //}

    private:
        // 次の候補を返す処理
        void getNextCandidate() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            outputHistResult(HIST_CAND->GetNext());
        }

        // 前の候補を返す処理
        void getPrevCandidate() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            outputHistResult(HIST_CAND->GetPrev());
        }

        // 選択のリセット
        void resetCandSelect() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            outputHistResult(HIST_CAND->ClearSelectPos());
            STATE_COMMON->SetWaitingCandSelect(-1);
        }

        void outputHistResult(const HistResult& result) {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
            getLastHistKeyAndRewindOutput();    // 前回の履歴検索キー取得と出力スタックの巻き戻し予約(numBackSpacesに値をセット)

            setOutString(result);
            if (!result.Word.empty() && result.Word.find(VERT_BAR) == MString::npos) {
                // 何か履歴候補(変換形履歴以外)が選択されたら、ブロッカーを設定する (emptyの場合は元に戻ったので、ブロッカーを設定しない)
                STATE_COMMON->SetHistoryBlockFlag();
            }
            setCandidatesVKB(VkbLayout::Horizontal, HIST_CAND->GetCandWords(), HIST_CAND->GetCurrentKey());

            // 英数モードはキャンセルする
            if (pNext) pNext->handleEisuCancel();

            _LOG_DEBUGH(_T("LEAVE: prevOut=%s, numBS=%d"), MAKE_WPTR(HISTORY_STAY_NODE->GetPrevOutString()), STATE_COMMON->GetBackspaceNum());
        }

    };
    DEFINE_CLASS_LOGGER(HistoryStayStateImpl);

} // namespace

// 履歴入力(常駐)機能状態インスタンスの Singleton
HistoryStayState* HistoryStayState::Singleton;

// -------------------------------------------------------------------
// HistoryNode - 履歴入力機能ノード
DEFINE_CLASS_LOGGER(HistoryNode);

// コンストラクタ
HistoryNode::HistoryNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
HistoryNode::~HistoryNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* HistoryNode::CreateState() {
    return new HistoryState(this);
}

HistoryNode* HistoryNode::Singleton;

// -------------------------------------------------------------------
// HistoryNodeBuilder - 履歴入力機能ノードビルダー
DEFINE_CLASS_LOGGER(HistoryNodeBuilder);

Node* HistoryNodeBuilder::CreateNode() {
    //// 履歴入力辞書ファイル名
    //auto histFile = SETTINGS->historyFile;
    //LOG_INFO(_T("histFile=%s"), histFile.c_str());
    ////if (histFile.empty()) {
    ////    ERROR_HANDLER->Warn(_T("「history=(ファイル名)」の設定がまちがっているようです"));
    ////}
    //// 履歴入力辞書の読み込み(ファイル名の指定がなくても辞書自体は構築する)
    //LOG_INFO(_T("CALLED: histFile=%s"), histFile.c_str());
    //HistoryDic::CreateHistoryDic(histFile);

    HISTORY_NODE = new HistoryNode();
    return HISTORY_NODE;
}

// -------------------------------------------------------------------
// HistoryFewCharsNode - 2～3文字履歴機能ノード
DEFINE_CLASS_LOGGER(HistoryFewCharsNode);

// コンストラクタ
HistoryFewCharsNode::HistoryFewCharsNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
HistoryFewCharsNode::~HistoryFewCharsNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* HistoryFewCharsNode::CreateState() {
    return new HistoryFewCharsState(this);
}

// -------------------------------------------------------------------
// HistoryFewCharsNodeBuilder - 2～3文字履歴機能ノードビルダー
DEFINE_CLASS_LOGGER(HistoryFewCharsNodeBuilder);

Node* HistoryFewCharsNodeBuilder::CreateNode() {
    return new HistoryFewCharsNode();
}

// -------------------------------------------------------------------
// HistoryOneCharNode - 1文字履歴機能ノード
DEFINE_CLASS_LOGGER(HistoryOneCharNode);

// コンストラクタ
HistoryOneCharNode::HistoryOneCharNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
HistoryOneCharNode::~HistoryOneCharNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* HistoryOneCharNode::CreateState() {
    return new HistoryOneCharState(this);
}

// -------------------------------------------------------------------
// HistoryOneCharNodeBuilder - 1文字履歴機能ノードビルダー
DEFINE_CLASS_LOGGER(HistoryOneCharNodeBuilder);

Node* HistoryOneCharNodeBuilder::CreateNode() {
    return new HistoryOneCharNode();
}

// -------------------------------------------------------------------
// HistoryStayNode - 履歴入力機能 常駐ノード
DEFINE_CLASS_LOGGER(HistoryStayNode);

// コンストラクタ
HistoryStayNode::HistoryStayNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
HistoryStayNode::~HistoryStayNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* HistoryStayNode::CreateState() {
    HISTORY_STAY_STATE = new HistoryStayStateImpl(this);
    return HISTORY_STAY_STATE;
}

// 履歴機能常駐ノードの生成
void HistoryStayNode::CreateSingleton() {
    // 履歴入力辞書ファイル名
    auto histFile = SETTINGS->historyFile;
    LOG_INFO(_T("histFile=%s"), histFile.c_str());
    // 履歴入力辞書の読み込み(ファイル名の指定がなくても辞書自体は構築する)
    LOG_INFO(_T("CALLED: histFile=%s"), histFile.c_str());
    HistoryDic::CreateHistoryDic(histFile);

    HIST_CAND.reset(new HistCandidates());
    HISTORY_STAY_NODE.reset(new HistoryStayNode());
}

// Singleton
std::unique_ptr<HistoryStayNode> HistoryStayNode::Singleton;

