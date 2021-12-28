#include "Logger.h"
#include "string_type.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "Constants.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "OutputStack.h"
#include "HistoryDic.h"
#include "EasyChars.h"
#include "StrokeHelp.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughHistory)

#if 1
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

namespace {
    // -------------------------------------------------------------------
    typedef size_t HashVal;

    // -------------------------------------------------------------------
    // ハッシュ値から文字列集合へのマップ
    class HashToStrMap {
        std::map<HashVal, std::set<MString>> dic;

        std::set<MString> emptySet;

    public:
        // 文字列(単語)の追加
        void Insert(const MString& s) {
            auto hsh = utils::get_hash(s);
            auto iter = dic.find(hsh);
            if (iter == dic.end()) {
                dic[hsh] = utils::make_one_element_set(s);
            } else {
                iter->second.insert(s);
            }
        }

        // 単語の削除
        void Remove(const MString& s) {
            auto iter = dic.find(utils::get_hash(s));
            if (iter != dic.end()) {
                iter->second.erase(s);
            }
        }

        // 単語は既に登録済みか
        bool FindWord(const MString& s) const {
            auto iter = dic.find(utils::get_hash(s));
            return (iter != dic.end() && iter->second.find(s) != iter->second.end());
        }

        // 指定されたハッシュ値を持つ単語の集合を取得する
        const std::set<MString>& GetSet(HashVal hsh) {
            auto iter = dic.find(hsh);
            return iter != dic.end() ? iter->second : emptySet;
        }

        const std::set<MString> GetAllWords() {
            std::set<MString> result;
            for (const auto& pr : dic) {
                utils::apply_union(result, pr.second);
            }
            return result;
        }

        //bool IsEmpty() const {
        //    return dic.empty();
        //}
    };

    // インスタンス
    HashToStrMap hashToStrMap;

    // -------------------------------------------------------------------
    // 単語中の文字から、それを含む文字列のハッシュ値集合へのマップ
    class HistCharDic {
        std::map<mchar_t, std::set<HashVal>> dic;

        void insert(mchar_t mch, size_t hsh) {
            auto iter = dic.find(mch);
            if (iter == dic.end()) {
                dic[mch] = utils::make_one_element_set(hsh);
            } else {
                iter->second.insert(hsh);
            }
        }

    public:
        void Insert(mchar_t mch, const MString& s) {
            if (!hashToStrMap.FindWord(s)) {
                insert(mch, utils::get_hash(s));
            }
        }

        std::set<MString> GetSet(mchar_t mch) {
            std::set<MString> result;
            auto iter = dic.find(mch);
            if (iter != dic.end()) {
                for (auto hsh : iter->second) {
                    const std::set<MString>& set_ = hashToStrMap.GetSet(hsh);
                    if (!set_.empty()) result.insert(set_.begin(), set_.end());
                }
            }
            return result;
        }
    };

    // 単語の先頭4文字を含むハッシュ値集合へのマップ
    class Hist4CharsDic {
        DECLARE_CLASS_LOGGER;

        // 0～3文字目に指定文字を含む文字列ハッシュ集合のリスト
        std::vector<HistCharDic> histCharDics;

    public:
        Hist4CharsDic() {
            histCharDics.resize(4);
        }

        void Insert(const MString& word) {
            for (size_t i = 0; i < histCharDics.size() && i < word.size(); ++i) {
                histCharDics[i].Insert(word[i], word);
            }
        }

        // key の末尾n文字にマッチする文字列集合を取得する('?' も考慮, ただし少なくとも1文字は'?'以外を含む)
        std::set<MString> GetSet(const MString& key, size_t n) {
            std::set<MString> result;
            size_t start = n >= key.size() ? 0 : key.size() - n;
            size_t nkey = key.size() - start;
            for (size_t i = 0; i < histCharDics.size() && i < nkey; ++i) {
                auto mch = key[start + i];
                if (mch == '?') continue; // '?' なら全部にマッチするとみなす
                auto iter = histCharDics[i].GetSet(mch);
                if (result.empty())
                    result = histCharDics[i].GetSet(mch);
                else
                    utils::apply_intersection(result, histCharDics[i].GetSet(mch));
                if (result.empty()) break;
            }
            _LOG_DEBUGH(_T("result.size=%d"), result.size());
            return result;
        }

        // '*' をはさんで、前半部の key1 と後半部の key2 にマッチする文字列集合を取得。key1のうちマッチした部分の長さを返す
        size_t FindMatchingWords(const MString& key1, const MString& key2, std::set<MString>& result) {
            std::set<MString> temp_set;
            auto key0 = utils::last_substr(key1, histCharDics.size());    // 前半キーの末尾4文字(以下)だけをキーとする
            result.clear();
            size_t start = 0;
            while (start < key0.size()) {
                for (size_t i = 0; i < key0.size() - start; ++i) {
                    auto mch = key0[start + i];
                    if (mch == '?') continue; // '?' なら全部にマッチするとみなす
                    if (temp_set.empty())
                        temp_set = histCharDics[i].GetSet(mch);
                    else
                        utils::apply_intersection(temp_set, histCharDics[i].GetSet(mch));
                    if (temp_set.empty()) break;
                }
                if (!temp_set.empty()) {
                    result = utils::filter(temp_set, [key2](const MString& w) {return utils::endsWithWildKey(w, key2);});
                    if (!result.empty()) {
                        size_t key_size = key0.size() - start;
                        _LOG_DEBUGH(_T("result.size=%d, keyMatchLen=%d"), result.size(), key_size);
                        return key_size;
                    }
                }
                ++start;
            }
            _LOG_DEBUGH(_T("result.size=0, keyMatchLen=0"));
            return 0;
        }
    };
    DEFINE_CLASS_LOGGER(Hist4CharsDic);

    // -------------------------------------------------------------------
    // 使用された順に並べたリスト
    class HistUsedList {
        DECLARE_CLASS_LOGGER;

        const size_t MAX_SIZE = 10000;
        const size_t EXTRA_SIZE = 1000;

        std::vector<MString> usedList;

        bool bDirty = false;

        // 順序復元用の位置
        size_t revertPos = 0;

        void clearRevertPos() {
            revertPos = 0;
        }

        void setRevertPos(const std::vector<MString>::iterator iter) {
            revertPos = iter - usedList.begin() + 1;
        }

    public:
        // UTF8で書かれた辞書ソースを読み込む
        void ReadFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("ENTER: %d lines"), lines.size());
            std::set<wstring> used;
            for (const auto& w : lines) {
                if (!utils::contains(used, w)) {
                    usedList.push_back(to_mstr(w));
                    used.insert(w);
                    if (usedList.size() >= MAX_SIZE) break;
                }
            }
            bDirty = false;
            LOG_INFO(_T("LEAVE"));
        }

        void PushEntry(const MString& word, size_t minlen = 2) {
            LOG_INFO(_T("CALLED: word=%s, minlen=%d"), MAKE_WPTR(word), minlen);
            clearRevertPos();
            if (word.size() >= minlen) {
                if (!usedList.empty()) {
                    if (usedList[0] == word) return;
                    utils::erase(usedList, word);
                }
                usedList.insert(usedList.begin(), word);
                if (usedList.size() >= MAX_SIZE + EXTRA_SIZE) {
                    _LOG_DEBUGH(_T("EXTRA entries erasing...: size=%d"), usedList.size());
                    usedList.erase(usedList.begin() + MAX_SIZE, usedList.end());
                    _LOG_DEBUGH(_T("EXTRA entries erased: size=%d"), usedList.size());
                }
                bDirty = true;
            }
        }

        // 指定の単語と先頭単語の入れ替え。指定単語が存在しなければ先頭に追加
        void SwapEntry(const MString& word) {
            LOG_DEBUG(_T("CALLED: word=%s"), MAKE_WPTR(word));
            clearRevertPos();
            if (!word.empty()) {
                if (!usedList.empty()) {
                    if (usedList[0] == word) return;
                    auto iter = std::find(usedList.begin(), usedList.end(), word);
                    if (iter != usedList.end()) {
                        setRevertPos(iter);
                        iter->assign(usedList[0]);
                        usedList[0].assign(word);
                        return;
                    }
                }
                usedList.insert(usedList.begin(), word);
                bDirty = true;
            }
        }

        // 先頭単語を復元位置に移動する
        void RevertEntry() {
            LOG_DEBUG(_T("CALLED"));
            if (revertPos > 0 && revertPos <= usedList.size()) {
                auto w = usedList[0];
                LOG_DEBUG(_T("word=%s"), MAKE_WPTR(w));
                usedList.insert(usedList.begin() + revertPos, w);
                usedList.erase(usedList.begin());
                bDirty = true;
            }
            clearRevertPos();
        }

        void RemoveEntry(const MString& word) {
            clearRevertPos();
            LOG_DEBUG(_T("CALLED: word=%s"), MAKE_WPTR(word));
            if (!usedList.empty()) {
                utils::erase(usedList, word);
                bDirty = true;
            }
        }

        // set_ および usedList に含まれるものから下記を満たすものを outvec に返す
        // outvec_ に格納されたものは set_ から取り除く
        // keylen = キー長
        // ・単語長が wlen に一致する
        // ・wlen == 0 なら単語長 >= 2
        // ・wlen >= 9 なら単語長 >= 9
        // ・ただし、キーが1文字(keylen==1)なら、候補列から1文字単語は除く
        void ExtractUsedWords(size_t keylen, HistResultList& outvec, std::set<MString>& set_, size_t wlen = 0) {
            LOG_DEBUG(_T("CALLED: keylen=%d, wlen=%d"), keylen, wlen);
            for (const auto& w : usedList) {
                if ((w.size() == wlen || (wlen == 0 && w.size() >= 2) || (wlen >= 9 && w.size() > 9)) && utils::contains(set_, w)) {
                    if (keylen != 1 || w.size() >= 2) {
                        // キーが1文字なら、候補列から1文字単語は除く
                        outvec.PushHistory(w);
                        set_.erase(w);
                    }
                }
            }
        }

        // usedList に含まれるものから下記を満たすものを返す(最大 n 個)
        // ・単語長が minlen 以上、maxlen 以下
        // ・maxlen == 0 なら単語長 >= 2
        // ・1 <= maxlen <= 3 なら難打鍵文字を含むものだけ
        // ・maxlen >= 9 なら単語長 >= 9
        void ExtractUsedWords(HistResultList& outvec, size_t n, size_t minlen = 0, size_t maxlen = 0) {
            LOG_DEBUG(_T("CALLED: size=%d, minlen=%d, maxlen=%d"), n, minlen, maxlen);
            auto checkCond = [minlen, maxlen](const MString& w) {
                if (w.size() >= minlen && w.size() <= maxlen && (maxlen > 3 || !EASY_CHARS->AllContainedIn(w))) return true;
                if (maxlen == 0 && w.size() >= 2) return true;
                if (maxlen >= 9 && w.size() > 9) return true;
                return false;
            };
            size_t i = 0;
            for (const auto& w : usedList) {
                if (checkCond(w)) {
                    outvec.PushHistory(w);
                    if (++i >= n) break;
                }
            }
        }

        // 辞書内容の書き込み
        void WriteFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED"));
            std::set<MString> used;
            for (const auto& word : usedList) {
                if (!utils::contains(used, word)) {
                    writer.writeLine(utils::utf8_encode(to_wstr(word)));
                    used.insert(word);
                }
            }
            bDirty = false;
        }

        //// 辞書が空か
        //bool IsEmpty() const {
        //    return usedList.empty();
        //}

        // 辞書が更新されているか
        bool IsDirty() const {
            return bDirty;
        }

    };
    DEFINE_CLASS_LOGGER(HistUsedList);

    // -------------------------------------------------------------------
    // 除外する履歴を並べたリスト
    class HistExcludeList {
        DECLARE_CLASS_LOGGER;
        std::set<MString> exclSet;

        bool bDirty = false;

    public:
        // UTF8で書かれた辞書ソースを読み込む
        void ReadFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("ENTER: %d lines"), lines.size());
            for (const auto& w : lines) {
                AddEntry(to_mstr(w));
            }
            bDirty = false;
            LOG_INFO(_T("LEAVE"));
        }

        void AddEntry(const MString& word) {
            exclSet.insert(word);
            bDirty = true;
        }

        void RemoveEntry(const MString& word) {
            auto iter = exclSet.find(word);
            if (iter != exclSet.end()) {
                exclSet.erase(iter);
                bDirty = true;
            }
        }

        bool Find(const MString& word) {
            return exclSet.find(word) != exclSet.end();
        }

        // 辞書内容の書き込み
        void WriteFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED"));
            for (const auto& word : exclSet) {
                writer.writeLine(utils::utf8_encode(to_wstr(word)));
            }
            bDirty = false;
        }

        //// 辞書が空か
        //bool IsEmpty() const {
        //    return exclSet.empty();
        //}

        // 辞書が更新されているか
        bool IsDirty() const {
            return bDirty;
        }
    };
    DEFINE_CLASS_LOGGER(HistExcludeList);

    // -------------------------------------------------------------------
    // Nグラム頻度辞書
    class NgramFreqDic {
        DECLARE_CLASS_LOGGER;
        std::map<MString, size_t> ngramFreqMap;

        std::set<MString> seenNgrams;

        bool bDirty = false;

    public:
        // UTF8で書かれた辞書ソースを読み込む
        void ReadFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("ENTER: %d lines"), lines.size());
            for (const auto& line : lines) {
                auto items = utils::split(to_mstr(line), ',');
                if (items.size() >= 2) {
                    const auto& w = items[0];
                    size_t freq = utils::strToInt(items[1]);
                    if (!w.empty() && freq > 0) {
                        ngramFreqMap[w] = freq;
                    }
                }
            }
            bDirty = false;
            LOG_INFO(_T("LEAVE"));
        }


        // 辞書内容の書き込み
        void WriteFile(utils::OfstreamWriter& writer) {
            for (const auto& pair : ngramFreqMap) {
                if (pair.first.size() >= 2 && pair.second > 0) {
                    writer.writeLine(utils::utf8_encode(utils::format(_T("%s,%d"), MAKE_WPTR(pair.first), pair.second)));
                }
            }
            bDirty = false;
        }

        //// 辞書が空か
        //bool IsEmpty() const {
        //    return ngramFreqMap.empty();
        //}

        // 辞書が更新されているか
        bool IsDirty() const {
            return bDirty;
        }

#define NGRAM_FREQ_THRESHOLD 3
        bool AddNgramEntry(const MString& ngram) {
            LOG_DEBUG(_T("addNgramEntry=%s"), MAKE_WPTR(ngram));
            if (utils::is_kanji_or_katakana_str(ngram)) return false;

            size_t count = 0;
            //auto iter = ngramFreqMap.find(ngram);
            //if (iter == ngramFreqMap.end()) {
            //    count = 1;
            //    ngramFreqMap[ngram] = count;
            //} else {
            //    count = iter->second + 1;
            //    iter->second = count;
            //}
            //bDirty = true;
            return count >= NGRAM_FREQ_THRESHOLD;
        }
#undef NGRAM_FREQ_THRESHOLD

#define NGRAM_MIN_LEN 5
#define NGRAM_MAX_LEN 10
        // Nグラム登録
        std::vector<MString> AddNgramEntries(const MString& word) {
            LOG_DEBUG(_T("AddNgramEntries=%s"), MAKE_WPTR(word));
            std::vector<MString> entryTargets;
            if (word.size() >= NGRAM_MIN_LEN && seenNgrams.find(word) == seenNgrams.end()) {
                seenNgrams.insert(word);
                size_t maxlen = word.size();
                if (maxlen > NGRAM_MAX_LEN) maxlen = NGRAM_MAX_LEN;
                for (size_t n = NGRAM_MIN_LEN; n <= maxlen; ++n) {
                    MString w = utils::last_substr(word, n);
                    if (AddNgramEntry(w))
                        entryTargets.push_back(w);
                }
                bDirty = true;
            } else {
                LOG_DEBUG(_T("word=\"%s\" is already seen."), MAKE_WPTR(word));
            }
            return entryTargets;
        }
#undef NGRAM_MIN_LEN
#undef NGRAM_MAX_LEN

        // 登録済みNグラム集合をクリアする
        void ClearNgramSet() {
            LOG_INFO(_T("CALLED"));
            seenNgrams.clear();
        }

    };
    DEFINE_CLASS_LOGGER(NgramFreqDic);

    // -------------------------------------------------------------------
    // 履歴辞書の実装クラス
    class HistoryDicImpl : public HistoryDic {
    private:
        DECLARE_CLASS_LOGGER;
        // 0～3文字目に指定文字を含む文字列ハッシュ集合のリスト
        Hist4CharsDic hist4CharsDic;

        HistUsedList usedList;

        HistExcludeList exclList;

        // 結果を保持しておくリスト
        //std::vector<HistResult> resultList;
        HistResultList resultList;

        NgramFreqDic ngramDic;

        bool bDirty = false;

    private:
        // 一行の辞書ソース文字列を解析して辞書に登録する
        bool addHistDicEntry(const MString& line, size_t minlen = 2, bool bForce = false) {
            LOG_DEBUG(_T("CALLED: %s, minlen=%d"), MAKE_WPTR(line), minlen);
            auto word = utils::strip(line);
            // 空白行または1文字以下、あるいは強制登録でなくて、先頭が '#' or ';' の場合は、何もしない
            if (word.size() < minlen || (!bForce && (word[0] == '#' || word[0] == ';'))) return false;

            if (!hashToStrMap.FindWord(word)) {
                //histDic1.Insert(word);
                //histDic2.Insert(word);
                //histDic3.Insert(word);
                //histDic4.Insert(word);
                hist4CharsDic.Insert(word);
                hashToStrMap.Insert(word);
            }
            bDirty = true;
            return true;
        }

    public:
        HistoryDicImpl() {
        }

        // UTF8で書かれた辞書ソースを読み込む
        void ReadFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("ENTER: %d lines"), lines.size());
            int logLevel = Logger::LogLevel;
            Logger::LogLevel = 0;
            for (const auto& line : lines) {
                addHistDicEntry(to_mstr(line), 1);
            }
            bDirty = false;
            Logger::LogLevel = logLevel;
            LOG_INFO(_T("LEAVE"));
        }

    private:
        inline void addNewEntry(const MString& word, bool bForce = false, size_t minlen = 2) {
            LOG_DEBUG(_T("CALLED: word=%s, bForce=%d"), MAKE_WPTR(word), bForce);
            if (bForce || !exclList.Find(word)) {
                usedList.PushEntry(word, minlen);
                addHistDicEntry(word, minlen, bForce);
            }
        }

        inline void addNewHistDicEntry(const MString& word, bool bForce = false, size_t minlen = 2) {
            LOG_DEBUG(_T("CALLED: word=%s, bForce=%d"), MAKE_WPTR(word), bForce);
            if (bForce || !exclList.Find(word)) {
                addHistDicEntry(word, minlen);
            }
        }

        void addNgramEntry(const MString& ngram) {
            if (ngramDic.AddNgramEntry(ngram)) {
                LOG_INFO(_T("addNewGgramEntry=%s"), MAKE_WPTR(ngram));
                addNewEntry(ngram);
            }
        }

    public:
        // 新規登録
        void AddNewEntry(const MString& word) {
            LOG_DEBUG(_T("CALLED: word=%s"), MAKE_WPTR(word));
            if (word.empty()) return;
            if (!STROKE_HELP->Find(utils::safe_back(word))) {
                // 末尾文字がストローク可能文字でなければ、履歴に登録しておく
                if (word.size() == 1) {
                    // 自身も1文字なら、usedListにも反映させて終わり
                    addNewEntry(utils::last_substr(word, 1), false, 1);
                    return;
                } else {
                    // 履歴だけに反映させる
                    addNewHistDicEntry(utils::last_substr(word, 1), false, 1);
                }
            }
            if (word.size() >= SETTINGS->histKatakanaWordMinLength ||
                (utils::is_kanji(word[0]) &&
                    (word.size() >= SETTINGS->histKanjiWordMinLength ||
                    (word.size() >= SETTINGS->histKanjiWordMinLengthEx && !EASY_CHARS->AllContainedIn(word))))) {
                //std::wregex reEntry(_T(".*(.{3,}).*\\1.*"));
                //if (std::regex_match(word, reEntry)) {
                //    LOG_DEBUG(_T("REGEX_MATCH! Maybe garbage"));
                //} else {
                //    addNewEntry(word);
                //}
                addNewEntry(word);
            }
        }

        // 新規登録(条件なし)
        void AddNewEntryAnyway(const MString& word) {
            addNewEntry(word, true);
            exclList.RemoveEntry(word);
        }

        // Nグラム登録
        void AddNgramEntries(const MString& word) {
            for (const auto& w : ngramDic.AddNgramEntries(word)) {
                addNewEntry(w);
            }
        }

        // 登録済みNグラム集合をクリアする
        void ClearNgramSet() {
            ngramDic.ClearNgramSet();
        }

        // 指定の見出し語のエントリを削除する
        void DeleteEntry(const MString& word) {
            LOG_INFO(_T("CALLED: %s"), MAKE_WPTR(word));
            usedList.RemoveEntry(word);
            hashToStrMap.Remove(word);
            exclList.AddEntry(word);
            bDirty = true;
        }

    private:
        // resultList に最近使ったものから取得した候補を格納し、pasts には set_ に含まれるものでそれ以外の候補を格納する
        // wlen > 0 なら、その長さの候補だけを返す
        void extract_and_copy(const MString& key, bool bWild, std::set<MString>& set_, HistResultList& pastList, size_t wlen = 0) {
            resultList.SetKeyInfo(key, bWild);
            size_t keylen = key.size();
            usedList.ExtractUsedWords(keylen, resultList, set_, wlen);
            for (const auto& s : set_) {
                // keylen == 1 なら1文字単語は対象外
                if ((wlen > 0 && s.size() == wlen) || (wlen == 0 && (keylen != 1 || s.size() >= 2))) {
                    pastList.PushHistory(s);
                }
            }
        }

        // '*' をはさんで、前半部の key1 と後半部の key2 にマッチする文字列集合を取得
        // resultList に最近使ったものから取得した候補を格納し、pastList にはそれ以外の候補を格納する
        void extract_and_copy_for_wildecard_included(const MString& key, HistResultList& pastList) {
            auto items = utils::split(key, '*');
            size_t itemsSize = items.size();
            if (itemsSize >= 2 && !items[itemsSize - 1].empty() && items[itemsSize - 1].size() <= 4) {
                // key が '*' を含み、最後の '*' の後が4文字以下
                std::set<MString> set_;
                const MString& key1 = items[itemsSize - 2];
                const MString& key2 = items[itemsSize - 1];
                size_t matchLen = hist4CharsDic.FindMatchingWords(key1, key2, set_) + key2.size() + 1;
                _LOG_DEBUGH(_T("extract_and_copy(keyLen=%d, set=hist4CharsDic.FindMatchingWords(%s, %s), pastList=(empty), wlen=0, bWild=true"), matchLen, MAKE_WPTR(key1), MAKE_WPTR(key2));
                extract_and_copy(utils::last_substr(key, matchLen), true, set_, pastList, 0);
                _LOG_DEBUGH(_T("resultList.size()=%d, pastList.size()=%d"), resultList.Size(), pastList.Size());
            }
        }

        // key の pos 位置から4文字(i.e., key[pos, pos+4])にマッチする候補の取得
        // resultList に最近使ったものから取得した候補を格納し、pastList にはそれ以外の候補を格納する
        // wlen > 0 なら、その長さの候補だけを返す
        void extract_and_copy_for_longer_than_4(const MString& key, size_t wlen, size_t pos, HistResultList& pastList) {
            auto subStr = key.substr(pos);
            auto subKey = subStr.substr(0, 4);
            std::set<MString> set_ = utils::filter(hist4CharsDic.GetSet(subKey, 4), [subStr](const auto& s) {return utils::startsWithWildKey(s, subStr);});
            _LOG_DEBUGH(_T("extract_and_copy(keyLen=%d, set=filter(hist4CharsDic.GetSet(%s, 4)), pastList=(empty), wlen=%d"), subStr.size(), MAKE_WPTR(subKey), wlen);
            extract_and_copy(subStr, subStr.find('?') != MString::npos, set_, pastList, wlen);
            _LOG_DEBUGH(_T("filter(hist4CharsDic, %d-4): resultList.size()=%d, pastList.size()=%d"), pos, resultList.Size(), pastList.Size());
        }

        // keyの末尾n文字にマッチする候補を取得して out に返す
        // wlen は候補文字列の長さに関する制約
        void get_extract_and_copy(const MString& key, size_t n, HistResultList& out, size_t wlen = 0) {
            std::set<MString> set_ = hist4CharsDic.GetSet(key, n);
            MString tailKey = utils::last_substr(key, n);
            extract_and_copy(tailKey, tailKey.find('?') != MString::npos, set_, out, wlen);
        }

    public:
        // 指定の部分文字列に対する変換候補のリストを取得する (len > 0 なら指定の長さの候補だけを取得, len < 0 なら 2～abs(len)の範囲の長さの候補を取得)
        // key.size() == 0 なら 最近使用した単語列を返す
        // key.size() == 1 なら 2文字以上の候補列を返す
        // key.size() >= 2 なら key.size() 文字以上の候補を返す
        // checkMinKeyLen = false なら、キー長チェックをやらない
        const HistResultList& GetCandidates(const MString& key, MString& resultKey, bool checkMinKeyLen, int len)
        {
            _LOG_DEBUGH(_T("ENTER: key=%s, checkMinKeyLen=%s, len=%d"), MAKE_WPTR(key), BOOL_TO_WPTR(checkMinKeyLen), len);
            // 結果を返すためのリストをクリアしておく
            resultList.Clear();
            size_t resultKeyLen = 0;
            size_t minlen = len >= 0 ? len : 2;
            size_t maxlen = len >= 0 ? len : max(minlen, (size_t)abs(len));
            if (key.empty()) {
                // ここでは len < 0 の場合も考慮
                usedList.ExtractUsedWords(resultList, 100, minlen, maxlen);
                resultKey = key;
            } else {
                // ここでは maxlen は無視する
                // 直近以外の過去に使った履歴のリスト
                HistResultList pastList;
                // key が '*' を含んでいる場合にワイルドカードのマッチングを行う
                extract_and_copy_for_wildecard_included(key, pastList);
                // Phase-A
                size_t keySize = key.size();
                // keyが5文字以上の場合に、先頭の4文字についても試す(末尾4文字は、この後の Phase-B で試される
                if (resultList.Empty() && pastList.Empty() && keySize >= 5) {
                    extract_and_copy_for_longer_than_4(key, minlen, 0, pastList);
                }
                // keyが6文字の場合に、中間の4文字についても試す(末尾4文字は、この後の Phase-B で試される
                if (resultList.Empty() && pastList.Empty() && keySize == 6) {
                    resultKeyLen = 5;
                    extract_and_copy_for_longer_than_4(key, minlen, 1, pastList);
                }
                // keyが7文字以上の場合に、末尾側の中間の4文字についても試す(最末尾4文字は、この後の Phase-B で試される
                if (resultList.Empty() && pastList.Empty() && keySize >= 7) {
                    resultKeyLen = 6;
                    extract_and_copy_for_longer_than_4(key, minlen, keySize - 6, pastList);
                    if (resultList.Empty() && pastList.Empty()) {
                        resultKeyLen = 5;
                        extract_and_copy_for_longer_than_4(key, minlen, keySize - 5, pastList);
                    }
                }

                // Phase-B
                if (resultList.Empty() && pastList.Empty()) {
                    size_t minKana = SETTINGS->histHiraganaKeyLength;
                    size_t minKata = SETTINGS->histKatakanaKeyLength;
                    size_t minKanj = SETTINGS->histKanjiKeyLength;
                    _LOG_DEBUGH(_T("minKana=%d, minKata=%d, minKanj=%d"), minKana, minKata, minKanj);
                    auto checkFunc = [key, checkMinKeyLen, minKana, minKata, minKanj](size_t len) {
                        return !checkMinKeyLen ||
                            ((minKana <= len || !utils::is_hirakana(utils::safe_back(key, len))) &&
                             (minKata <= len || !utils::is_katakana(utils::safe_back(key, len))) &&
                             (minKanj <= len || !utils::is_kanji(utils::safe_back(key, len))));
                    };

                    resultKeyLen = 4;
                    _LOG_DEBUGH(_T("get_extract_and_copy(key=%s, n=4, len=%d"), MAKE_WPTR(key), minlen);
                    get_extract_and_copy(key, 4, pastList, minlen);
                    _LOG_DEBUGH(_T("histDic4: resultList.size()=%d, pastList.size()=%d"), resultList.Size(), pastList.Size());
                    if (resultList.Empty() && pastList.Empty()) {
                        if (checkFunc(3)) {
                            resultKeyLen = 3;
                            _LOG_DEBUGH(_T("get_extract_and_copy(key=%s, n=3, len=%d"), MAKE_WPTR(key), minlen);
                            get_extract_and_copy(key, 3, pastList, minlen);
                            _LOG_DEBUGH(_T("histDic3: resultList.size()=%d, pastList.size()=%d"), resultList.Size(), pastList.Size());
                        }
                        if (resultList.Empty() && pastList.Empty()) {
                            if (checkFunc(2)) {
                                resultKeyLen = 2;
                                _LOG_DEBUGH(_T("get_extract_and_copy(key=%s, n=2, len=%d"), MAKE_WPTR(key), minlen);
                                get_extract_and_copy(key, 2, pastList, minlen);
                                _LOG_DEBUGH(_T("histDic2: resultList.size()=%d, pastList.size()=%d"), resultList.Size(), pastList.Size());
                            }
                            if (resultList.Empty() && pastList.Empty()) {
                                if (checkFunc(1)) {
                                    resultKeyLen = 1;
                                    _LOG_DEBUGH(_T("get_extract_and_copy(key=%s, n=1, len=%d"), MAKE_WPTR(key), minlen);
                                    get_extract_and_copy(key, 1, pastList, minlen);
                                    _LOG_DEBUGH(_T("histDic1: resultList.size()=%d, pastList.size()=%d"), resultList.Size(), pastList.Size());
                                }
                            }
                        }
                    }
                }
                resultList.Append(pastList);  // 最近使ったもの以外を追加する
                resultKey = resultKeyLen == 0 ? key : utils::last_substr(key, resultKeyLen);
                _LOG_DEBUGH(_T("resultKey=%s, resultList.size()=%d"), MAKE_WPTR(resultKey), resultList.Size());
            }

            if (SETTINGS->histMoveShortestAt2nd) {
                // 最短語を少なくとも先頭から2番目に移動する
                resultList.MoveShortestHistAt2nd();
            }
            _LOG_DEBUGH(_T("LEAVE: resultKey=%s, resultKeyLen=%d"), MAKE_WPTR(resultKey), resultKeyLen);
            return resultList;
        }

        // 使用した単語をリストの先頭に追加or移動
        void UseWord(const MString& word) {
            usedList.PushEntry(word, 0);    // 1文字の単語についても優先順の移動をするため、ここの minlen は1以下にしておく
        }

        // 指定の単語と先頭単語の入れ替え。指定単語が存在しなければ先頭に追加
        void SwapWord(const MString& word) {
            usedList.SwapEntry(word);
        }

        // 先頭単語を元の位置に戻す
        void RevertWord() {
            usedList.RevertEntry();
        }

        // 辞書内容の保存
        void WriteFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED"));
            for (const auto& word : hashToStrMap.GetAllWords()) {
                writer.writeLine(utils::utf8_encode(to_wstr(word)));
            }
            bDirty = false;
        }

        // 辞書が空か
        bool IsHistDicDirty() const {
            return bDirty;
        }

        // 使用辞書の読み込み
        void ReadUsedFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("CALLED"));
            usedList.ReadFile(lines);
        }

        // 使用辞書内容の保存
        void WriteUsedFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED"));
            usedList.WriteFile(writer);
        }

        bool IsUsedDicDirty() const {
            return usedList.IsDirty();
        }

        // 除外辞書の読み込み
        void ReadExcludeFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("CALLED"));
            exclList.ReadFile(lines);
        }

        // 除外辞書内容の保存
        void WriteExcludeFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED"));
            exclList.WriteFile(writer);
        }

        bool IsExcludeDicDirty() const {
            return exclList.IsDirty();
        }

        // Nグラム辞書の読み込み
        void ReadNgramFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("CALLED"));
            ngramDic.ReadFile(lines);
        }

        // Nグラム辞書内容の保存
        void WriteNgramFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED"));
            ngramDic.WriteFile(writer);
        }

        bool IsNgramDicDirty() const {
            return ngramDic.IsDirty();
        }

    private:
    };
    DEFINE_CLASS_LOGGER(HistoryDicImpl);

} // namespace

// -------------------------------------------------------------------
DEFINE_CLASS_LOGGER(HistoryDic);

std::unique_ptr<HistoryDic> HistoryDic::Singleton;

namespace {
    DEFINE_NAMESPACE_LOGGER(HistoryDic_Local);

    wstring replaceStar(const wstring& path, size_t pos, const wchar_t* name) {
        return path.substr(0, pos) + name + path.substr(pos + 1);
    }

    typedef void (HistoryDic::* READ_FUNC)(const std::vector<wstring>& lines);

    // 履歴ファイルの読み込み
    void readFile(const wstring& path, READ_FUNC func) {
        LOG_INFO(_T("open hist file: %s"), path.c_str());
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            // ファイル読み込み
            (HISTORY_DIC.get()->*func)(reader.getAllLines());
            LOG_INFO(_T("close hist file: %s"), path.c_str());
        } else {
            LOG_WARN(_T("Can't read hist file: %s"), path.c_str());
        }
    };

    typedef void (HistoryDic::* WRITE_FUNC)(utils::OfstreamWriter&);

    // 辞書ファイルの内容の書き出し
    void writeFile(const wstring& path, WRITE_FUNC func) {
        LOG_INFO(_T("CALLED: path=%s"), path.c_str());
        if (!path.empty() && HISTORY_DIC) {
            utils::OfstreamWriter writer(path);
            if (writer.success()) {
                (HISTORY_DIC.get()->*func)(writer);
            }
        }
    }

}

// 入力履歴ファイルを読み込んで、履歴辞書を作成する
// ファイルに名は * を含むこと(例: xxxx.*.yyy)。
// * の部分を {entry,recent,exclude,ngram} に置換したファイルが読み込まれる
// エラーがあったら例外を投げる
int HistoryDic::CreateHistoryDic(const tstring& histFile) {
    LOG_INFO(_T("ENTER: histFile=%s"), histFile.c_str());

    if (Singleton != 0) {
        LOG_INFO(_T("already created: hist file: %s"), histFile.c_str());
        return 0;
    }

    // 辞書ファイルが無くても辞書インスタンスは作成する
    Singleton.reset(new HistoryDicImpl());

    if (!histFile.empty()) {
        wstring filename = histFile;
        if (!utils::contains(filename, _T("*"))) {
            // エラーメッセージを表示
            LOG_WARN(_T("hist file should be a wildcard form such as 'xxxx.*.yyy': %s"), filename.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("入力履歴ファイル(%s)はワイルドード文字(*)を含む形式である必要があります。\r\nデフォルトのファイル名パターン 'kwhist.*.txt' を使用します。"), filename.c_str()));
            filename = _T("kwhist.*.txt");
        }
        auto path = utils::joinPath(SETTINGS->rootDir, filename);
        LOG_INFO(_T("open history file: %s"), path.c_str());

        size_t pos = path.find(_T("*"));
        readFile(replaceStar(path, pos, _T("entry")), &HistoryDic::ReadFile);
        readFile(replaceStar(path, pos, _T("recent")), &HistoryDic::ReadUsedFile);
        readFile(replaceStar(path, pos, _T("exclude")), &HistoryDic::ReadExcludeFile);
        //readFile(replaceStar(path, pos, _T("ngram")), &HistoryDic::ReadNgramFile);
    }
    LOG_INFO(_T("LEAVE"));
    return 0;
}

// 辞書ファイルの内容の書き出し
void HistoryDic::WriteHistoryDic(const tstring& histFile) {
    LOG_INFO(_T("CALLED: path=%s"), histFile.c_str());
    if (Singleton) {
        auto path = utils::joinPath(SETTINGS->rootDir, utils::contains(histFile, _T("*")) ? histFile : _T("kwhist.*.txt"));
        size_t pos = path.find(_T("*"));
        if (Singleton->IsHistDicDirty() || SETTINGS->firstUse) {
            auto pathEntry = replaceStar(path, pos, _T("entry"));
            if (utils::moveFileToBackDirWithRotation(pathEntry, SETTINGS->backFileRotationGeneration)) {
                writeFile(pathEntry, &HistoryDic::WriteFile);
            }
        }
        if (Singleton->IsUsedDicDirty()) writeFile(replaceStar(path, pos, _T("recent")), &HistoryDic::WriteUsedFile);
        if (Singleton->IsExcludeDicDirty()) writeFile(replaceStar(path, pos, _T("exclude")), &HistoryDic::WriteExcludeFile);
        //if (Singleton->IsNgramDicDirty()) writeFile(replaceStar(path, pos, _T("ngram")), &HistoryDic::WriteNgramFile);
    }
}

// 辞書ファイルの内容の書き出し
void HistoryDic::WriteHistoryDic() {
    WriteHistoryDic(SETTINGS->historyFile);
}

