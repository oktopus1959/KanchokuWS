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

        bool IsEmpty() const {
            return dic.empty();
        }
    };

    // インスタンス
    HashToStrMap hashToStrMap;

    // -------------------------------------------------------------------
    // 先頭N文字から、元文字列のハッシュ値の集合へのマップ
    // この集合から、さらに HashToStrMap を経由してアグリゲートすると、最終的な文字列集合が得られる
    template<size_t N>
    class HistStrDic {
        std::map<MString, std::set<HashVal>> dic;

    public:
        void Insert(const MString& s) {
            if (!hashToStrMap.FindWord(s)) {
                if (s.size() >= N) {
                    auto hsh = utils::get_hash(s);
                    auto ss = s.substr(0, N);
                    auto iter = dic.find(ss);
                    if (iter == dic.end()) {
                        dic[ss] = utils::make_one_element_set(hsh);
                    } else {
                        iter->second.insert(hsh);
                    }
                }
            }
        }

        std::set<MString> GetSet(const MString& key) {
            std::set<MString> result;
            if (key.size() >= N) {
                auto iter = dic.find(utils::last_substr(key, N));
                if (iter != dic.end()) {
                    for (auto hsh : iter->second) {
                        const std::set<MString>& st = hashToStrMap.GetSet(hsh);
                        if (!st.empty()) result.insert(st.begin(), st.end());
                    }
                }
            }
            return result;
        }
    };

    // -------------------------------------------------------------------
    // 単語中の文字から、それを含む文字列のハッシュ値集合へのマップ
    class HistCharDic {
        std::map<wchar_t, std::set<HashVal>> dic;

    public:
        void Insert(const MString& s) {
            if (!hashToStrMap.FindWord(s)) {
                std::set<wchar_t> seen;
                auto hsh = utils::get_hash(s);
                for (auto mch : s) {
                    wchar_t ch = (wchar_t)mch;
                    if (!utils::contains(seen, ch)) {
                        seen.insert(ch);
                        auto iter = dic.find(ch);
                        if (iter == dic.end()) {
                            dic[ch] = utils::make_one_element_set(hsh);
                        } else {
                            iter->second.insert(hsh);
                        }
                    }
                }
            }
        }

        std::set<MString> GetSet(wchar_t key) {
            std::set<MString> result;
            auto iter = dic.find(key);
            if (iter != dic.end()) {
                for (auto hsh : iter->second) {
                    const std::set<MString>& st = hashToStrMap.GetSet(hsh);
                    if (!st.empty()) result.insert(st.begin(), st.end());
                }
            }
        }
    };

    // -------------------------------------------------------------------
    // 使用された順に並べたリスト
    class HistUsedList {
        DECLARE_CLASS_LOGGER;

        std::vector<MString> usedList;

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
                }
            }
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
            }
            clearRevertPos();
        }

        void RemoveEntry(const MString& word) {
            clearRevertPos();
            LOG_DEBUG(_T("CALLED: word=%s"), MAKE_WPTR(word));
            if (!usedList.empty()) {
                utils::erase(usedList, word);
            }
        }

        // list および usedList に含まれるものから下記を満たすものを返す
        // ・単語長が len に一致する
        // ・len == 0 なら単語長 >= 2
        // ・len >= 9 なら単語長 >= 9
        // ・ただし、キーが1文字(keylen==1)なら、候補列から1文字単語は除く
        void ExtractUsedWords(size_t keylen, std::vector<HistResult>& outvec, std::set<MString>& list, size_t wlen = 0) {
            LOG_DEBUG(_T("CALLED: keylen=%d, wlen=%d"), keylen, wlen);
            for (const auto& w : usedList) {
                if ((w.size() == wlen || (wlen == 0 && w.size() >= 2) || (wlen >= 9 && w.size() > 9)) && utils::contains(list, w)) {
                    if (keylen != 1 || w.size() >= 2) {
                        // キーが1文字なら、候補列から1文字単語は除く
                        outvec.push_back(HistResult{ keylen, w });
                        list.erase(w);
                    }
                }
            }
        }

        // usedList に含まれるものから下記を満たすものを返す(最大 n 個)
        // ・単語長が len に一致する
        // ・len == 0 なら単語長 >= 2
        // ・len >= 9 なら単語長 >= 9
        void ExtractUsedWords(std::vector<HistResult>& outvec, size_t n, size_t len = 0) {
            LOG_DEBUG(_T("CALLED: size=%d, len=%d"), n, len);
            size_t i = 0;
            for (const auto& w : usedList) {
                if (w.size() == len || (len == 0 && w.size() >= 2) || (len >= 9 && w.size() > 9)) {
                    outvec.push_back(HistResult{ 0, w });
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
        }

        // 辞書が空か
        bool IsEmpty() const {
            return usedList.empty();
        }

    };
    DEFINE_CLASS_LOGGER(HistUsedList);

    // -------------------------------------------------------------------
    // 除外する履歴を並べたリスト
    class HistExcludeList {
        DECLARE_CLASS_LOGGER;
        std::set<MString> exclSet;

    public:
        // UTF8で書かれた辞書ソースを読み込む
        void ReadFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("ENTER: %d lines"), lines.size());
            for (const auto& w : lines) {
                AddEntry(to_mstr(w));
            }
            LOG_INFO(_T("LEAVE"));
        }

        void AddEntry(const MString& word) {
            exclSet.insert(word);
        }

        void RemoveEntry(const MString& word) {
            auto iter = exclSet.find(word);
            if (iter != exclSet.end()) exclSet.erase(iter);
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
        }

        // 辞書が空か
        bool IsEmpty() const {
            return exclSet.empty();
        }

    };
    DEFINE_CLASS_LOGGER(HistExcludeList);

    // -------------------------------------------------------------------
    // Nグラム頻度辞書
    class NgramFreqDic {
        DECLARE_CLASS_LOGGER;
        std::map<MString, size_t> ngramFreqMap;

        std::set<MString> seenNgrams;

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
            LOG_INFO(_T("LEAVE"));
        }


        // 辞書内容の書き込み
        void WriteFile(utils::OfstreamWriter& writer) {
            for (const auto& pair : ngramFreqMap) {
                if (pair.first.size() >= 2 && pair.second > 0) {
                    writer.writeLine(utils::utf8_encode(utils::format(_T("%s,%d"), MAKE_WPTR(pair.first), pair.second)));
                }
            }
        }

        // 辞書が空か
        bool IsEmpty() const {
            return ngramFreqMap.empty();
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
        HistStrDic<1> histDic1;
        HistStrDic<2> histDic2;
        HistStrDic<3> histDic3;
        HistStrDic<4> histDic4;
        HistCharDic histCharDic;

        HistUsedList usedList;

        HistExcludeList exclList;

        std::vector<HistResult> resultList;

        NgramFreqDic ngramDic;

    private:
        // 一行の辞書ソース文字列を解析して辞書に登録する
        bool AddHistDicEntry(const MString& line, size_t minlen = 2, bool bForce = false) {
            LOG_DEBUG(_T("CALLED: %s, minlen=%d"), MAKE_WPTR(line), minlen);
            auto word = utils::strip(line);
            // 空白行または1文字以下、あるいは強制登録でなくて、先頭が '#' or ';' の場合は、何もしない
            if (word.size() < minlen || (!bForce && (word[0] == '#' || word[0] == ';'))) return false;

            if (!hashToStrMap.FindWord(word)) {
                histDic1.Insert(word);
                histDic2.Insert(word);
                histDic3.Insert(word);
                histDic4.Insert(word);
                histCharDic.Insert(word);
                hashToStrMap.Insert(word);
            }
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
                AddHistDicEntry(to_mstr(line), 1);
            }
            Logger::LogLevel = logLevel;
            LOG_INFO(_T("LEAVE"));
        }

    private:
        inline void addNewEntry(const MString& word, bool bForce = false, size_t minlen = 2) {
            LOG_DEBUG(_T("CALLED: word=%s, bForce=%d"), MAKE_WPTR(word), bForce);
            if (bForce || !exclList.Find(word)) {
                usedList.PushEntry(word, minlen);
                AddHistDicEntry(word, minlen, bForce);
            }
        }

        inline void addNewHistDicEntry(const MString& word, bool bForce = false, size_t minlen = 2) {
            LOG_DEBUG(_T("CALLED: word=%s, bForce=%d"), MAKE_WPTR(word), bForce);
            if (bForce || !exclList.Find(word)) {
                AddHistDicEntry(word, minlen);
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
        }

    private:
        // resultList に最近使ったものから取得した候補を、pasts にそれ以外の候補を格納する
        // wlen > 0 なら、その長さの候補だけを返す
        void extract_and_copy(size_t klen, std::set<MString>& st, std::vector<HistResult>& pasts, size_t wlen = 0) {
            usedList.ExtractUsedWords(klen, resultList, st, wlen);
            //utils::transform_append(st, pasts, [klen](const auto& s) {return HistResult{ klen, s };});
            for (const auto& s : st) {
                // keylen == 1 なら1文字単語は対象外
                if ((wlen > 0 && s.size() == wlen) || (wlen == 0 && (klen != 1 || s.size() >= 2))) {
                    pasts.push_back(HistResult{ klen, s });
                }
            }
        }

        template<size_t N>
        void get_extract_and_copy(const MString& key, HistStrDic<N>& dic, std::vector<HistResult>& out, size_t wlen = 0) {
            std::set<MString> st = dic.GetSet(key);
            extract_and_copy(N, st, out, wlen);
        }

    public:
        // 指定の部分文字列に対する変換候補のリストを取得する (len > 0 なら指定の長さの候補だけを取得)
        // key.size() == 0 なら 最近使用した単語列を返す
        // key.size() == 1 なら 2文字以上の候補列を返す
        // key.size() >= 2 なら key.size() 文字以上の候補を返す
        // checkMinKeyLen = false なら、キー長チェックをやらない
        const std::vector<HistResult>& GetCandidates(const MString& key, MString& resultKey, bool checkMinKeyLen, size_t len)
        {
            LOG_DEBUG(_T("ENTER: key=%s, checkMinKeyLen=%s, len=%d"), MAKE_WPTR(key), BOOL_TO_WPTR(checkMinKeyLen), len);
            resultList.clear();
            size_t resultKeyLen = 0;
            if (key.empty()) {
                usedList.ExtractUsedWords(resultList, 100, len);
                resultKey = key;
            } else {
                std::vector<HistResult> pastList;
                if (key.size() > 4) {
                    std::set<MString> st = utils::filter(histDic4.GetSet(key.substr(0, 4)), [key](const auto& s) {return utils::startsWith(s, key);});
                    extract_and_copy(4, st, pastList, len);
                }

                if (resultList.empty() && pastList.empty()) {
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
                    get_extract_and_copy(key, histDic4, pastList, len);
                    if (resultList.empty() && pastList.empty()) {
                        if (checkFunc(3)) {
                            resultKeyLen = 3;
                            get_extract_and_copy(key, histDic3, pastList, len);
                        }
                        if (resultList.empty() && pastList.empty()) {
                            if (checkFunc(2)) {
                                resultKeyLen = 2;
                                get_extract_and_copy(key, histDic2, pastList, len);
                            }
                            if (resultList.empty() && pastList.empty()) {
                                if (checkFunc(1)) {
                                    resultKeyLen = 1;
                                    get_extract_and_copy(key, histDic1, pastList, len);
                                }
                            }
                        }
                    }
                }
                utils::append(resultList, pastList);  // 最近使ったもの以外を追加する
                resultKey = resultKeyLen == 0 ? key : utils::last_substr(key, resultKeyLen);
            }
            LOG_DEBUG(_T("LEAVE: resultKey=%s, resultKeyLen=%d"), MAKE_WPTR(resultKey), resultKeyLen);
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
        }

        // 辞書が空か
        bool IsHistDicEmpty() const {
            return hashToStrMap.IsEmpty();
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

        bool IsUsedDicEmpty() const {
            return usedList.IsEmpty();
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

        bool IsExcludeDicEmpty() const {
            return exclList.IsEmpty();
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

        bool IsNgramDicEmpty() const {
            return ngramDic.IsEmpty();
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
        auto path = utils::joinPath(SETTINGS->workDir, filename);
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
        auto path = utils::joinPath(SETTINGS->workDir, utils::contains(histFile, _T("*")) ? histFile : _T("kwhist.*.txt"));
        size_t pos = path.find(_T("*"));
        if (!Singleton->IsHistDicEmpty() || SETTINGS->firstUse) {
            auto pathEntry = replaceStar(path, pos, _T("entry"));
            if (utils::moveFileToBackDirWithRotation(pathEntry, SETTINGS->backFileRotationGeneration)) {
                writeFile(pathEntry, &HistoryDic::WriteFile);
            }
        }
        if (!Singleton->IsUsedDicEmpty()) writeFile(replaceStar(path, pos, _T("recent")), &HistoryDic::WriteUsedFile);
        if (!Singleton->IsExcludeDicEmpty()) writeFile(replaceStar(path, pos, _T("exclude")), &HistoryDic::WriteExcludeFile);
        if (!Singleton->IsNgramDicEmpty()) writeFile(replaceStar(path, pos, _T("ngram")), &HistoryDic::WriteNgramFile);
    }
}

// 辞書ファイルの内容の書き出し
void HistoryDic::WriteHistoryDic() {
    WriteHistoryDic(SETTINGS->historyFile);
}


