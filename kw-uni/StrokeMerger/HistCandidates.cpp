#include "Logger.h"

#include "Settings.h"
#include "State.h"

#include "HistCandidates.h"

#if 1
#undef _LOG_DEBUGH
#define _LOG_DEBUGH LOG_INFOH
#endif

namespace {
    // -------------------------------------------------------------------
    // 履歴入力リストのクラス
    class HistCandidatesImpl : public HistCandidates {
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
            size_t x = std::min(histCands.Size(), SETTINGS->histHorizontalCandMax);
            selectPos = n >= 0 && n < x ? n : -1;
        }

        // 選択位置をインクリメント //(一周したら未選択状態に戻る)
        inline void incSelectPos() const {
            size_t x = std::min(histCands.Size(), SETTINGS->histHorizontalCandMax);
            selectPos = selectPos < 0 ? 0 : x <= 0 ? -1 : (selectPos + 1) % x;
        }

        // 選択位置をデクリメント //(一周したら未選択状態に戻る)
        inline void decSelectPos() const {
            int x = std::min(histCands.Size(), SETTINGS->histHorizontalCandMax);
            selectPos = selectPos <= 0 ? x - 1 : x <= 0 ? -1 : (selectPos - 1) % x;
        }

        inline int getSelectPos() const {
            return selectPos;
        }

        inline bool isSelecting() const {
            return selectPos > 0 && selectPos < (int)histResults.size();
        }

        inline const HistResult getSelectedHist() const {
            int n = getSelectPos();
            int x = std::min(histCands.Size(), SETTINGS->histHorizontalCandMax);
            return n >= 0 && n < x ? histCands.GetNthHist(n) : emptyResult;
        }

        inline const MString& getSelectedWord() const {
            int n = getSelectPos();
            return n >= 0 && n < (int)histResults.size() ? histResults[n].Word : EMPTY_MSTR;
        }

    public:
        ~HistCandidatesImpl() {
        }

    public:
        // 履歴検索キー設定をクリアする
        void ClearKeyInfo() override {
            histCands.ClearKeyInfo();
            currentKey.clear();
            isHistInSearch = false;
        }

        bool IsHistInSearch() override {
            //_LOG_DEBUGH(_T("CALLED: HistInSearch={}"), isHistInSearch);
            return isHistInSearch;
        }

        const MString& GetOrigKey() override {
            return histCands.GetOrigKey();
        }

        // 指定のキーで始まる候補を取得する (len > 0 なら指定の長さの候補だけを取得, len < 0 なら Abs(len)以下の長さの候補を取得)
        const std::vector<HistResult>& GetCandidates(const MString& key, bool bCheckMinKeyLen, int len) override {
            isHistInSearch = true;
            DelayedPushFrontSelectedWord();
            currentLen = len;
            histCands = HISTORY_DIC->GetCandidates(key, currentKey, bCheckMinKeyLen, len);  // ここで currentKey は変更される (currentKey = resultKey)
            histResults.clear();
            utils::append(histResults, histCands.GetHistories());
            _LOG_DEBUGH(_T("cands num={}, new currentKey={}"), histResults.size(), to_wstr(currentKey));
            return histResults;
        }

        const std::vector<MString> GetCandWords(const MString& key, bool bCheckMinKeyLen, int len) override {
            _LOG_DEBUGH(_T("CALLED: key={}, bCheckMinKeyLen={}, len={}"), to_wstr(key), bCheckMinKeyLen, len);
            GetCandidates(key, bCheckMinKeyLen, len);
            return GetCandWords();
        }

        // 取得済みの候補列を返す
        //const std::vector<HistResult>& GetCandidates() const override {
        //    return histResults;
        //}

        const std::vector<MString> GetCandWords() const override {
            _LOG_DEBUGH(_T("CALLED"));
            std::vector<MString> words;
            utils::transform_append(histResults, words, [](const HistResult& res) { return res.Word; });
            return words;
        }

        const MString& GetCurrentKey() const override {
            return currentKey;
        }

        // 次の履歴を選択する
        const HistResult GetNext() const override {
            incSelectPos();
            return getSelectedHist();
        }

        // 前の履歴を選択する
        const HistResult GetPrev() const override {
            decSelectPos();
            return getSelectedHist();
        }

        // 選択された単語を取得する
        const HistResult GetPositionedHist(size_t pos) const override {
            _LOG_DEBUGH(_T("CALLED: selectPos={}"), pos);
            setSelectPos(pos);
            return getSelectedHist();
        }

        // 選択された単語を取得する
        const MString& GetSelectedWord() const override {
            _LOG_DEBUGH(_T("CALLED: selectPos={}"), selectPos);
            return getSelectedWord();
        }

        // 選択されている位置を返す -- 未選択状態なら -1を返す
        int GetSelectPos() const override {
            _LOG_DEBUGH(_T("CALLED: nextSelect={}"), selectPos);
            return getSelectPos();
        }

        // 選択位置を初期化(未選択状態)する
        const HistResult ClearSelectPos() override {
            _LOG_DEBUGH(_T("CALLED: nextSelect={}"), selectPos);
            resetSelectPos();
            return emptyResult;
        }

        // 候補が選択されていれば、それを使用履歴の先頭にpushする -- selectPos は未選択状態に戻る
        void DelayedPushFrontSelectedWord() override {
            _LOG_DEBUGH(_T("ENTER"));
            if (isSelecting()) {
                HISTORY_DIC->UseWord(GetSelectedWord());
            }
            ClearSelectPos();
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // 取得済みの履歴入力候補リストから指定位置の候補を返す
        // 選択された候補は使用履歴の先頭に移動する
        const HistResult SelectNth(size_t n) override {
            _LOG_DEBUGH(_T("ENTER: n={}, histResults={}"), n, histResults.size());
            ClearSelectPos();
            if (n >= histResults.size()) {
                _LOG_DEBUGH(_T("LEAVE: empty"));
                return emptyResult;
            }

            HistResult result = histResults[n];
            HISTORY_DIC->UseWord(result.Word);
            GetCandidates(currentKey, false, currentLen);
            _LOG_DEBUGH(_T("LEAVE: OrigKey={}, Key={}, Word={}"), to_wstr(result.OrigKey), to_wstr(result.Key), to_wstr(result.Word));
            return result;
        }

        void DeleteNth(size_t n) override {
            _LOG_DEBUGH(_T("ENTER"));
            DelayedPushFrontSelectedWord();
            if (n < histCands.Size()) {
                HISTORY_DIC->DeleteEntry(histCands.GetNthWord(n));
                GetCandidates(currentKey, false, currentLen);
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

    };
    DEFINE_CLASS_LOGGER(HistCandidatesImpl);

} // namespace

// HistCandidates::Singleton
std::unique_ptr<HistCandidates> HistCandidates::Singleton;

void HistCandidates::CreateSingleton() {
    Singleton.reset(new HistCandidatesImpl());
}
