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
        std::vector<HistResult> histCands;

        // 候補単語列
        std::vector<MString> histWords;

        HistResult emptyResult;

        MString currentKey;

        size_t currentLen = 0;

        // 選択位置 -- -1 は未選択状態を表す
        mutable int selectPos = -1;

        // 未選択状態に戻す
        inline void resetSelectPos() {
            selectPos = -1;
        }

        inline void setSelectPos(size_t n) const {
            //selectPos = n >= 0 && n < histCands.size() ? n : -1;
            //size_t x = min(histCands.size(), LONG_KEY_NUM);
            size_t x = min(histCands.size(), SETTINGS->histHorizontalCandMax);
            selectPos = n >= 0 && n < x ? n : -1;
        }

        // 選択位置をインクリメント //(一周したら未選択状態に戻る)
        inline void incSelectPos() const {
            //selectPos = selectPos < 0 ? 0 : (selectPos + 1) % histCands.size();
            //size_t x = min(histCands.size(), LONG_KEY_NUM);
            size_t x = min(histCands.size(), SETTINGS->histHorizontalCandMax);
            selectPos = selectPos < 0 ? 0 : x <= 0 ? -1 : (selectPos + 1) % x;
        }

        // 選択位置をデクリメント //(一周したら未選択状態に戻る)
        inline void decSelectPos() const {
            //selectPos = selectPos <= 0 ? histCands.size() - 1 : (selectPos - 1) % histCands.size();
            //int x = min((int)histCands.size(), LONG_KEY_NUM);
            int x = min((int)histCands.size(), SETTINGS->histHorizontalCandMax);
            selectPos = selectPos <= 0 ? x - 1 : x <=0 ? -1 : (selectPos - 1) % x;
        }

        inline int getSelectPos() const {
            return selectPos;
        }

        inline bool isSelecting() const {
            return selectPos > 0 && selectPos < (int)histWords.size();
        }

        inline const HistResult& getSelectedHist() const {
            int n = getSelectPos();
            //int x = min((int)histCands.size(), LONG_KEY_NUM);
            int x = min((int)histCands.size(), SETTINGS->histHorizontalCandMax);
            //return n >= 0 && n < (int)histCands.size() ? histCands[n] : emptyResult;
            return n >= 0 && n < x ? histCands[n] : emptyResult;
        }

        inline const MString& getSelectedWord() const {
            int n = getSelectPos();
            return n >= 0 && n < (int)histWords.size() ? histWords[n] : EMPTY_MSTR;
        }

    public:
        // 指定のキーで始まる候補を取得する (len > 0 なら指定の長さの候補だけを取得)
        const std::vector<MString>& GetCandidates(const MString& key, bool checkMinKeyLen, size_t len) {
            PushFrontSelectedWord();
            currentLen = len;
            histCands = HISTORY_DIC->GetCandidates(key, currentKey, checkMinKeyLen, len);
            histWords.clear();
            utils::transform_append(histCands, histWords, [](const HistResult& r) { return r.Word;});
            LOG_INFO(_T("cands num=%d, currentKey=%s"), histWords.size(), MAKE_WPTR(currentKey));
            return histWords;
        }

        const std::vector<MString>& GetCandidates() const {
            return histWords;
        }

        const MString& GetCurrentKey() const {
            return currentKey;
        }

        //// n番目の履歴を選択する (n < 0 なら選択のリセット)
        //const HistResult& GetNth(int n) const {
        //    setSelectPos(n);
        //    return getSelectedHist();
        //}

        // 次の履歴を選択する
        const HistResult& GetNext() const {
            incSelectPos();
            return getSelectedHist();
        }

        // 前の履歴を選択する
        const HistResult& GetPrev() const {
            decSelectPos();
            return getSelectedHist();
        }

        //// 選択された履歴を取得する
        //const HistResult& GetSelectedHist() const {
        //    LOG_DEBUG(_T("CALLED: selectPos=%d"), selectPos);
        //    return getSelectedHist();
        //}

        // 選択された単語を取得する
        const MString& GetSelectedWord() const {
            LOG_DEBUG(_T("CALLED: selectPos=%d"), selectPos);
            return getSelectedWord();
        }

        // 選択されている位置を返す -- 未選択状態なら -1を返す
        int GetSelectPos() const {
            LOG_DEBUG(_T("CALLED: nextSelect=%d"), selectPos);
            return getSelectPos();
        }

        // 選択位置を初期化(未選択状態)する
        const HistResult& ClearSelectPos() {
            LOG_DEBUG(_T("CALLED: nextSelect=%d"), selectPos);
            resetSelectPos();
            return emptyResult;
        }

        // 候補が選択されていれば、それを使用履歴の先頭にpushする -- selectPos は未選択状態に戻る
        void PushFrontSelectedWord() {
            if (isSelecting()) {
                HISTORY_DIC->UseWord(GetSelectedWord());
            }
            ClearSelectPos();
        }

        // 取得済みの履歴入力候補リストから指定位置の候補を返す
        // 選択された候補は使用履歴の先頭に移動する
        const HistResult& SelectNth(size_t n) {
            LOG_DEBUG(_T("CALLED: n=%d, histWords=%d"), n, histWords.size());
            ClearSelectPos();
            if (n >= histWords.size()) {
                return emptyResult;
            }

            HISTORY_DIC->UseWord(histWords[n]);
            GetCandidates(currentKey, false, currentLen);
            return histCands[0];
        }

        inline void DeleteNth(size_t n) {
            LOG_DEBUG(_T("CALLED"));
            PushFrontSelectedWord();
            if (n < histCands.size()) {
                HISTORY_DIC->DeleteEntry(histCands[n].Word);
                GetCandidates(currentKey, false, currentLen);
            }
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

        size_t candLen = 0;
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
            LOG_DEBUG(_T("CALLED: %s"), BASE_NAME_PTR);
            STATE_COMMON->SetAppendBackspaceStopperFlag();
            STATE_COMMON->SetHistoryBlockFlag();
            STATE_COMMON->ClearDecKeyCount();
        }

        // 選択された履歴候補を出力 (abbrev なら true を返す)
        bool setOutString(const HistResult& result, size_t numBS = 0) {
            _LOG_DEBUGH(_T("CALLED: word=%s, keyLen=%d, numBS=%d"), MAKE_WPTR(result.Word), result.KeyLen, numBS);
            bool flag = false;
            size_t pos = result.Word.find('|');
            if (pos < SETTINGS->abbrevKeyMaxLength) {
                // abbrev
                flag = true;
                ++pos;                      // '|' まで削除する必要あり
                numBS += result.KeyLen;     // abbreなのでkeyも削除する必要あり
                STATE_COMMON->SetOutString(utils::safe_substr(result.Word, pos), numBS);
                HISTORY_STAY_NODE->prevOutString.clear();
                HISTORY_STAY_NODE->prevKeyLen = 0;
                HISTORY_STAY_NODE->prevKey.clear();
            } else {
                STATE_COMMON->SetOutString(utils::safe_substr(result.Word, result.KeyLen), numBS);
                HISTORY_STAY_NODE->prevOutString = result.Word;
                HISTORY_STAY_NODE->prevKeyLen = (int)result.KeyLen;
                HISTORY_STAY_NODE->prevKey = utils::safe_substr(result.Word, result.KeyLen);
            }
            _LOG_DEBUGH(_T("prevOutString=%s, prevKeyLen=%d, numBS=%d"), MAKE_WPTR(HISTORY_STAY_NODE->prevOutString), HISTORY_STAY_NODE->prevKeyLen, numBS);
            return flag;
        }

        // 前回の履歴検索の出力と現在の出力文字列(改行以降)の末尾を比較し、同じであれば前回の履歴検索のキーを取得する
        // この時、出力スタックは、キーだけを残し、追加出力部分は巻き戻される(numBackSpacesに値をセット)
        // 前回が空キーだった場合は、返値も空キーになるので、HISTORY_STAY_NODE->prevKeyLen == 0 かどうかで前回と同じキーであるか否かを判断すること
        MString getLastHistKey() {
            MString key;

            // 前回の履歴検索の出力
            const auto& prevOut = HISTORY_STAY_NODE->prevOutString;
            size_t prevKeyLen = size_t(HISTORY_STAY_NODE->prevKeyLen > 0 ? HISTORY_STAY_NODE->prevKeyLen : 0);
            _LOG_DEBUGH(_T("prevOut=%s, prevKeyLen=%d"), MAKE_WPTR(prevOut), prevKeyLen);

            if (prevOut.size() > prevKeyLen) {
                auto lastJstr = OUTPUT_STACK->GetLastJapaneseStr<MString>(prevOut.size());
                _LOG_DEBUGH(_T("lastJapaneseStr=%s"), MAKE_WPTR(lastJstr));
                if (lastJstr == prevOut) {
                    //前回の履歴検索の出力が、現在の出力と同じであれば、直前の履歴入力に戻す
                    STATE_COMMON->SetBackspaceNum(prevOut.size() - prevKeyLen);
                    key = utils::safe_substr(prevOut, 0, prevKeyLen);
                    _LOG_DEBUGH(_T("REVERT: key=%s"), MAKE_WPTR(key));
                }
            } else {
                key = HISTORY_STAY_NODE->prevKey;
                _LOG_DEBUGH(_T("Use prevKey=%s"), MAKE_WPTR(key));
            }

            _LOG_DEBUGH(_T("last Japanese key=%s"), MAKE_WPTR(key));
            return key;
        }

        // 前回の履歴選択の出力と現在の出力文字列(改行以降)の末尾を比較し、同じであれば前回の履歴選択の出力を取得する
        // 異なっていれば空文字列を返す
        MString getLastHistOutIfSameAsCurrent() {
            MString outStr;

            // 前回の履歴選択の出力
            const auto& prevOut = HISTORY_STAY_NODE->prevOutString;
            // 出力スタックから、上記と同じ長さの末尾文字列を取得
            auto lastJstr = OUTPUT_STACK->GetLastJapaneseStr<MString>(prevOut.size());
            _LOG_DEBUGH(_T("prevOut=%s, lastJapaneseStr=%s"), MAKE_WPTR(prevOut), MAKE_WPTR(lastJstr));
            if (lastJstr == prevOut) {
                //前回の履歴選択の出力が、現在の出力と同じなので、それを返す
                outStr.assign(prevOut);
                _LOG_DEBUGH(_T("REVERT"));
            }

            _LOG_DEBUGH(_T("last Japanese outStr=%s"), MAKE_WPTR(outStr));
            return outStr;
        }

        // 履歴入力候補を鍵盤にセットする
        void setCandidatesVKB(VkbLayout layout, const std::vector<MString>& cands, const MString& key, bool bShrinkWord = false) {
            _LOG_DEBUGH(_T("layout=%d, cands.size()=%d, key=%s"), layout, cands.size(), MAKE_WPTR(key));
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
        }

        // 中央鍵盤の色付け、矢印キー有効、縦列鍵盤の色付けあり
        void setHistSelectColorAndBackColor() {
            // 「候補選択」の色で中央鍵盤の色付け
            STATE_COMMON->SetHistCandSelecting();
            // 矢印キーを有効にして、背景色の色付けなし
            STATE_COMMON->SetWaitingCandSelect(-1);
        }

        // 中央鍵盤の文字出力と色付け、矢印キー有効、縦列鍵盤の色付けなし
        void setCenterStringAndBackColor(const wstring& ws) {
            // 中央鍵盤の文字出力
            STATE_COMMON->SetCenterString(ws);
            // 「その他の状態」の色で中央鍵盤の色付け
            STATE_COMMON->SetOtherStatus();
            // 矢印キーを有効にして、背景色の色付けなし
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
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER"));

            if (!HISTORY_DIC) return false;

            // 過去の履歴候補選択の結果を反映しておく
            HIST_CAND->PushFrontSelectedWord();

            // 末尾1文字の登録
            auto ws = OUTPUT_STACK->GetLastOutputStackStrUptoNL(1);
            if (ws.size() == 1 && ws[0] >= 0x100 && !STROKE_HELP->Find(ws[0])) {
                // 末尾文字がストローク可能文字でなければ登録する
                HISTORY_DIC->AddNewEntry(ws);
            }
            // 前回の履歴検索キー取得と出力スタックの巻き戻し(numBackSpacesに値をセット)
            auto key = getLastHistKey();

            if (key.empty() && HISTORY_STAY_NODE->prevKeyLen != 0) {
                // 前回履歴検索とは一致しなかった
                // ひらがな交じりやASCIIもキーとして取得する
                key = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>();
                //key = STATE_COMMON->GetLastKanjiOrKatakanaKey();
                _LOG_DEBUGH(_T("new Japanese key=%s"), MAKE_WPTR(key));
            }

            // 履歴入力候補の取得
            candLen = 0;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandidates(key, false, candLen), key);
            // 未選択状態にセットする
            STATE_COMMON->SetWaitingCandSelect(-1);

            // 前状態にチェインする
            _LOG_DEBUGH(_T("LEAVE: Chain"));
            return true;
        }

        // 最終的な出力履歴が整ったところで呼び出される処理
        void DoOutStringProc() {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);

            //if (pNext) pNext->DoOutStringProc();
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandidates(), HIST_CAND->GetCurrentKey());
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

         // Strokeキー を処理する
        void handleStrokeKeys(int deckey) {
            LOG_DEBUG(_T("ENTER: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
            if (deckey == SETTINGS->histDelDeckeyId) {
                // 削除モードに入る
                LOG_DEBUG(_T("LEAVE: DELETE MODE"));
                bDeleteMode = true;
                return;
            }
            if (bDeleteMode) {
                // 削除モードのとき
                if (deckey == SETTINGS->histDelDeckeyId) {
                    bDeleteMode = false;
                    LOG_DEBUG(_T("LEAVE DELETE MODE"));
                } else if (deckey < STROKE_SPACE_DECKEY) {
                    HIST_CAND->DeleteNth((deckey % LONG_KEY_NUM) + candDispHorizontalPos);
                    bDeleteMode = false;
                    //const wstring key = STATE_COMMON->GetLastKanjiOrKatakanaKey();
                    // ひらがな交じりやASCIIもキーとして取得する
                    const auto key = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>();
                    LOG_DEBUG(_T("key=%s"), MAKE_WPTR(key));
                    candLen = 0;
                    HIST_CAND->GetCandidates(key, false, candLen);
                    LOG_DEBUG(_T("LEAVE DELETE MODE"));
                }
                return;
            }
            if (deckey == SETTINGS->histNumDeckeyId) {
                // 履歴文字数指定
                LOG_DEBUG(_T("ENTER: NUM MODE"));
                bWaitingForNum = true;
                return;
            }
            if (bWaitingForNum) {
                // 履歴文字数指定のとき
                bWaitingForNum = false;
                if (deckey >= 0 && deckey < CAND_LEN_THRESHOLD) {
                    // '1'〜'0' (1〜10文字のものだけを表示)
                    LOG_DEBUG(_T("ENTER JUST LEN MODE"));
                    //指定の長さのものだけを残して仮想鍵盤に表示
                    candDispHorizontalPos = 0;
                    candDispVerticalPos = 0;
                    auto key = HIST_CAND->GetCurrentKey();
                    candLen = (deckey + 1) % LONG_KEY_NUM;
                    setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandidates(key, false, candLen), key);
                }
                LOG_DEBUG(_T("LEAVE: forNum"));
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
            const auto& result = HIST_CAND->SelectNth((deckey >= STROKE_SPACE_DECKEY ? 0 : deckey % LONG_KEY_NUM) + candDispHorizontalPos);
            LOG_DEBUG(_T("result.Word=%s, result.KeyLen=%d"), MAKE_WPTR(result.Word), result.KeyLen);
            if (!result.Word.empty()) {
                setOutString(result);
                if (result.KeyLen >= 2) STATE_COMMON->SetHistoryBlockFlag();  // 1文字の場合は履歴検索の対象となる
            }
            handleKeyPostProc();
            LOG_DEBUG(_T("LEAVE"));
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
            //candDispVerticalPos += CAND_DISP_LONG_VKEY_LEN;
            candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
            auto key = HIST_CAND->GetCurrentKey();
            //指定の長さのものだけを残して仮想鍵盤に表示
            candLen = (candLen + 1) % CAND_LEN_THRESHOLD;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandidates(key, false, candLen), key);
            return;
        }

        void handleUpArrow() {
            //if (candDispVerticalPos >= CAND_DISP_LONG_VKEY_LEN)
            //    candDispVerticalPos -= CAND_DISP_LONG_VKEY_LEN;
            //else
            //    candDispVerticalPos = 0;
            candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
            auto key = HIST_CAND->GetCurrentKey();
            //指定の長さのものだけを残して仮想鍵盤に表示
            candLen = candLen <= 0 ? 9 : candLen - 1;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandidates(key, false, candLen), key);
            return;
        }

        void handleLeftArrow() {
            if (candDispHorizontalPos >= LONG_KEY_NUM)
                candDispHorizontalPos -= LONG_KEY_NUM;
            else
                candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
        }

        void handleRightArrow() {
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

    protected:
        void handleKeyPostProc() {
            STATE_COMMON->ClearVkbLayout();
            //STATE_COMMON->RemoveFunctionState();
            bUnnecessary = true;
        }

    };
    DEFINE_CLASS_LOGGER(HistoryState);

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

            // 1文字履歴の取得
            MString key;
            candLen = 1;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandidates(key, false, candLen), key);

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
            HISTORY_STAY_NODE->prevKeyLen = -1;     // 負の値で、まだ履歴検索が行われていないということを表す
        }


    public:
        // Enter時の新しい履歴の追加
        void AddNewHistEntryOnEnter() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            if (HISTORY_DIC) {
                HIST_CAND->PushFrontSelectedWord();
                STATE_COMMON->SetBothHistoryBlockFlag();
                if (OUTPUT_STACK->isLastOutputStackCharKanjiOrKatakana()) {
                    // これまでの出力末尾が漢字またはカタカナであるなら
                    // 出力履歴の末尾の漢字列またはカタカナ列を取得して、それを履歴辞書に登録する
                    HISTORY_DIC->AddNewEntry(OUTPUT_STACK->GetLastKanjiOrKatakanaStr<MString>());
                } else if (OUTPUT_STACK->isLastOutputStackCharHirakana()) {
                    // 漢字・カタカナ以外なら5〜10文字の範囲でNグラム登録する
                    HISTORY_DIC->AddNgramEntries(OUTPUT_STACK->GetLastJapaneseStr<MString>(10));
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
        void SetTranslatedOutString(const MString& outStr) {
            _LOG_DEBUGH(_T("ENTER: %s: outStr=%s"), NAME_PTR, MAKE_WPTR(outStr));
            if (pNext) {
                STATE_COMMON->SetOutString(pNext->TranslateString(outStr));
            } else if (SETTINGS->autoBushuComp) {
                BUSHU_COMP_NODE->ReduceByAutoBushu(outStr);
            } else {
                STATE_COMMON->SetOutString(outStr);
            }
            AddNewHistEntryOnSomeChar();
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
        }

    protected:
        // 事前チェック
        void DoPreCheck() {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
            maybeEditedBySubState = false;
            // 常駐モード
            if (pNext && pNext->GetName().find(_T("History")) == wstring::npos) {
                // 履歴機能ではない次状態(StrokeStateなど)があれば、それが何かをしているはずなので、戻ってきたら新たに候補の再取得を行うために、ここで maybeEditedBySubState を true にセットしておく
                //prevKey.clear();
                _LOG_DEBUGH(_T("Set Reinitialized=true"));
                maybeEditedBySubState = true;
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
        // 直前キーが空でなく、候補が1つ以上あり、第1候補または第2候補がキー文字列から始まっていて、かつ同じではないか
        // たとえば、直前に「竈門」を交ぜ書きで出力したような場合で、これまでの出力履歴が「竈門」だけなら履歴候補の表示はやらない。
        // 他にも「竈門炭治郎」の出力履歴があるなら、履歴候補の表示をする。
        bool isHotCandidateReady(const MString& prevKey, const std::vector<MString>& cands) {
            bool result = (!prevKey.empty() &&
                           ((cands.size() > 0 && utils::startsWith(cands[0], prevKey) && cands[0] != prevKey) ||
                            (cands.size() > 1 && utils::startsWith(cands[1], prevKey) && cands[1] != prevKey)));
            if (IS_LOG_DEBUGH_ENABLED) {
                size_t candsSize = cands.size();
                MString cands0 = candsSize > 0 ? cands[0] : MString();
                LOG_DEBUGH(_T("RESULT=%s, prevKey=%s, cands.size=%d, cands[0]=%s"), BOOL_TO_WPTR(result), MAKE_WPTR(prevKey), candsSize, MAKE_WPTR(cands0));
            }
            return result;
        }

        // 一時的にこのフラグを立てることにより、履歴検索を行わないようになる
        bool bNoHistTemporary = false;

        // 一時的にこのフラグを立てることにより、自動モードでなくても履歴検索が実行されるようになる
        bool bManualTemporary = false;

        // 前回の履歴検索との比較、新しい履歴検索の開始 (bManual=trueなら自動モードでなくても履歴検索を実行する)
        void historySearch(bool bManual) {
            // 前回の履歴選択の出力と現在の出力文字列(改行以降)の末尾を比較し、同じであれば前回の履歴選択の出力を取得する
            // たとえば前回「中」で履歴検索し「中納言家持」が履歴出力されており、現在の出力スタックが「・・・中納言家持」なら「中納言持家」が返る
            auto prevOut = getLastHistOutIfSameAsCurrent();
            LOG_INFOH(_T("PATH 7: prevOut=%s, auto=%s, manual=%s, maybeEditedBySubState=%s"), MAKE_WPTR(prevOut), BOOL_TO_WPTR(SETTINGS->autoHistSearchEnabled), BOOL_TO_WPTR(bManual), BOOL_TO_WPTR(maybeEditedBySubState));
            // 前回履歴出力が取得できた、つまり出力文字列の末尾が前回の履歴選択と同じ出力だったら、出力文字列をキーとした履歴検索はやらない
            // これは、たとえば「中」で履歴検索し、「中納言家持」を選択した際に、キーとして返される「中納言家持」の末尾の「持」を拾って「持統天皇」を履歴検索してしまうことを防ぐため。
            // ただし、交ぜ書き変換など何か後続状態により出力がなされた場合(maybeEditedBySubState)は、履歴検索を行う。
            if (SETTINGS->autoHistSearchEnabled || bManual) {
                // 履歴検索可能状態であって
                _LOG_DEBUGH(_T("PATH 11: Auto or Manual"));
                if (prevOut.empty() || maybeEditedBySubState) {
                    _LOG_DEBUGH(_T("PATH 12A: prevOut is Empty"));
                    // 現在の出力文字列は履歴選択したものではなかった
                    // 出力文字から、ひらがな交じりやASCIIもキーとして取得する
                    auto key = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>();
                    _LOG_DEBUGH(_T("LastJapaneseKey=%s"), MAKE_WPTR(key));
                    if (!key.empty()) {
                        // キーが取得できた
                        //bool isAscii = is_ascii_char((wchar_t)utils::safe_back(key));
                        _LOG_DEBUGH(_T("PATH 8: key=%s, prevKey=%s, maybeEditedBySubState=%s"), MAKE_WPTR(key), MAKE_WPTR(HISTORY_STAY_NODE->prevKey), utils::boolToString(maybeEditedBySubState).c_str());
                        auto func = [this](const std::vector<MString>& words, const MString& ky) {
                            _LOG_DEBUGH(_T("FUNC: words.size()=%d, key=%s"), words.size(), MAKE_WPTR(ky));
                            if (words.empty() || (words.size() == 1 && (words[0].empty() || words[0] == ky))) {
                                _LOG_DEBUGH(_T("PATH A: cands size <= 1"));
                                // 候補が1つだけで、keyに一致するときは履歴選択状態にはしない
                            } else {
                                _LOG_DEBUGH(_T("PATH B"));
                                setCandidatesVKB(VkbLayout::Horizontal, words, ky);
                            }
                        };
                        if (key != HISTORY_STAY_NODE->prevKey || maybeEditedBySubState || bManual) {
                            _LOG_DEBUGH(_T("PATH 9: different key"));
                            //bool checkMinKeyLen = !bManual && utils::is_hiragana(key[0]);       // 自動検索かつ、キー先頭がひらがなならキー長チェックをやる
                            bool checkMinKeyLen = !bManual;                                     // 自動検索ならキー長チェックをやる
                            func(HIST_CAND->GetCandidates(key, checkMinKeyLen, 0), key);
                            // キーが短くなる可能性があるので再取得
                            key = HIST_CAND->GetCurrentKey();
                            _LOG_DEBUGH(_T("currentKey=%s"), MAKE_WPTR(key));
                        } else {
                            // 前回の履歴検索と同じキーだった
                            _LOG_DEBUGH(_T("PATH 10: Same as prev hist key"));
                            func(HIST_CAND->GetCandidates(), key);
                        }
                    }
                    HISTORY_STAY_NODE->prevKey = key;
                    _LOG_DEBUGH(_T("PATH 12B: prevKey=%s"), MAKE_WPTR(key));
                } else {
                    _LOG_DEBUGH(_T("PATH 12C: prevOut is same as current out"));
                }
            } else {
                // 履歴検索状態ではないので、前回キーをクリアしておく。
                // こうしておかないと、自動履歴検索OFFのとき、たとえば、
                // 「エッ」⇒Ctrl+Space⇒「エッセンス」⇒Esc⇒「エッ」⇒「セ」追加⇒出力「エッセ」、キー=「エッ」のまま⇒再検索⇒「エッセセンス」となる
                _LOG_DEBUGH(_T("PATH 13: Clear PrevKey"));
                HISTORY_STAY_NODE->prevKey.clear();
            }
            // この処理は、GUI側で候補の背景色を変更するために必要
            if (isHotCandidateReady(HISTORY_STAY_NODE->prevKey, HIST_CAND->GetCandidates())) {
                _LOG_DEBUGH(_T("PATH 14"));
                // 何がしかの文字出力があり、それをキーとする履歴候補があった場合 -- 未選択状態にセットする
                STATE_COMMON->SetWaitingCandSelect(-1);
                bCandSelectable = true;
            }
            maybeEditedBySubState = false;
        }

    public:
        // 最終的な出力履歴が整ったところで呼び出される処理
        void DoOutStringProc() {
            _LOG_DEBUGH(_T("\nENTER: %s: %s"), NAME_PTR, MAKE_WPTR(OUTPUT_STACK->OutputStackBackStrUpto(10)));
            _LOG_DEBUGH(_T("PATH 2: bCandSelectable=%s"), BOOL_TO_WPTR(bCandSelectable));

            if (bCandSelectable && wasCandSelectCalled()) {
                _LOG_DEBUGH(_T("PATH 3: by SelectionKey"));
                // 履歴選択キーによる処理だった場合
                if (bCandSelectable) {
                    _LOG_DEBUGH(_T("PATH 4"));
                    // この処理は、GUI側で候補の背景色を変更するのと矢印キーをホットキーにするために必要
                    STATE_COMMON->SetWaitingCandSelect(HIST_CAND->GetSelectPos());
                }
            } else {
                _LOG_DEBUGH(_T("PATH 5: by Other Input"));
                // その他の文字出力だった場合
                HIST_CAND->PushFrontSelectedWord();
                bCandSelectable = false;

                if (OUTPUT_STACK->isLastOutputStackCharBlocker()) {
                    _LOG_DEBUGH(_T("PATH 6"));
                    HISTORY_DIC->ClearNgramSet();
                }

                // 前回の履歴検索との比較、新しい履歴検索の開始
                if (!bNoHistTemporary) historySearch(bManualTemporary);
                bNoHistTemporary = false;
                bManualTemporary = false;
            }

            _LOG_DEBUGH(_T("LEAVE: %s\n"), NAME_PTR);
        }

        // (Ctrl or Shift)+Space の処理 -- 履歴検索の開始、次の候補を返す
        void handleNextOrPrevCandTrigger(bool bNext) {
            _LOG_DEBUGH(_T("\nCALLED: %s: selectPos=%d, bNext=%s"), NAME_PTR, HIST_CAND->GetSelectPos(), BOOL_TO_WPTR(bNext));
            // これにより、前回のEnterによる改行点挿入やFullEscapeによるブロッカーフラグが削除される
            OUTPUT_STACK->clearFlagAndPopNewLine();
            // 今回、履歴選択用ホットキーだったことを保存
            setCandSelectIsCalled();

            if (!bCandSelectable) {
                // 履歴候補選択可能状態でなければ、前回の履歴検索との比較、新しい履歴検索の開始
                historySearch(true);
            }
            if (bCandSelectable) {
                _LOG_DEBUGH(_T("CandSelectable"));
                if (bNext)
                    getNextCandidate();
                else
                    getPrevCandidate();
            } else {
                //func();
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
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
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
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
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
            HIST_CAND->PushFrontSelectedWord();
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

        // Tab の処理 -- 履歴選択状態から抜けて、履歴検索文字列の遡及ブロッカーをセット
        void handleTab() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            //HIST_CAND->PushFrontSelectedWord();
            //HistoryStateBase::setBlocker();
            //LOG_DEBUG(_T("LEAVE"));
            // Enter と同じ扱いにする
            AddNewHistEntryOnEnter();
            HistoryStayState::handleTab();
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
            _LOG_DEBUGH(_T("CALLED: %s: selectPos=%d"), NAME_PTR, HIST_CAND->GetSelectPos());
            if (SETTINGS->selectFirstCandByEnter && bCandSelectable && HIST_CAND->GetSelectPos() < 0) {
                // 選択可能状態かつ候補未選択なら第1候補を返す。
                getNextCandidate();
            } else {
                AddNewHistEntryOnEnter();
                HistoryStayState::handleEnter();
            }
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
        //            HIST_CAND->PushFrontSelectedWord();
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
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            outputHistResult(HIST_CAND->GetNext());
        }

        // 前の候補を返す処理
        void getPrevCandidate() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            outputHistResult(HIST_CAND->GetPrev());
        }

        // 選択のリセット
        void resetCandSelect() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            outputHistResult(HIST_CAND->ClearSelectPos());
        }

        void outputHistResult(const HistResult& result) {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
            auto key = getLastHistKey();    // 前回の履歴検索キー取得と出力スタックの巻き戻し(numBackSpacesに値をセット)
            size_t numBS = STATE_COMMON->GetBackspaceNum();
            _LOG_DEBUGH(_T("lastHistKey=%s, numBS=%d"), MAKE_WPTR(key), numBS);

            bool bAbbrev = setOutString(result, numBS); // numBackSpaces には getLastHistKey() によって、キー以降の文字長が格納されている
            if (!result.Word.empty()) {
                // emptyの場合は元に戻ったので、ブロッカーを設定してはならない (@TODO: ちょっと意味不明)
                STATE_COMMON->SetHistoryBlockFlag();
            }
            if (!bAbbrev) setCandidatesVKB(VkbLayout::Horizontal, HIST_CAND->GetCandidates(), HIST_CAND->GetCurrentKey());

            _LOG_DEBUGH(_T("LEAVE: prevOut=%s, prevKeyLen=%d, numBS=%d"), MAKE_WPTR(HISTORY_STAY_NODE->prevOutString), HISTORY_STAY_NODE->prevKeyLen, numBS);
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
HistoryStayNode* HistoryStayNode::CreateNode() {
    // 履歴入力辞書ファイル名
    auto histFile = SETTINGS->historyFile;
    LOG_INFO(_T("histFile=%s"), histFile.c_str());
    // 履歴入力辞書の読み込み(ファイル名の指定がなくても辞書自体は構築する)
    LOG_INFO(_T("CALLED: histFile=%s"), histFile.c_str());
    HistoryDic::CreateHistoryDic(histFile);

    HIST_CAND.reset(new HistCandidates());
    HISTORY_STAY_NODE.reset(new HistoryStayNode());
    return HISTORY_STAY_NODE.get();
}

// Singleton
std::unique_ptr<HistoryStayNode> HistoryStayNode::Singleton;

