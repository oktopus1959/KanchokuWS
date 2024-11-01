#include "Logger.h"
#include "string_utils.h"
#include "misc_utils.h"
#include "path_utils.h"
#include "file_utils.h"
#include "transform_utils.h"
#include "Constants.h"

#include "settings.h"
#include "StateCommonInfo.h"
#include "Lattice.h"
#include "MStringResult.h"
#include "BushuComp/BushuComp.h"
#include "BushuComp/BushuDic.h"

#include "MorphBridge.h"
#include "Llama/LlamaBridge.h"

#define _LOG_DETAIL LOG_DEBUG
#define _LOG_INFOH LOG_INFOH
#if 1
#undef IS_LOG_DEBUGH_ENABLED
#define IS_LOG_DEBUGH_ENABLED true
#if 1
#undef _LOG_DETAIL
#define _LOG_DETAIL LOG_WARN
#endif
#undef _LOG_INFOH
#undef LOG_INFO
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#define _LOG_INFOH LOG_WARN
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#endif

namespace lattice2 {
    DEFINE_LOCAL_LOGGER(lattice);

    // ビームサイズ
    size_t BestKSize = 5;

    // 多ストロークの範囲 (stroke位置的に組み合せ不可だったものは、strokeCount が範囲内なら残しておく)
    int AllowedStrokeRange = 5;

    // 末尾がここで設定した長さ以上に同じ候補は、先頭だけを残して削除
    int LastSameLen = 5;

    // 非優先候補に与えるペナルティ
    int NON_PREFERRED_PENALTY = 1000000;

    //// 漢字と長音が連続する場合のペナルティ
    //int KANJI_CONSECUTIVE_PENALTY = 1000;

    //// 漢字が連続する場合のボーナス
    //int KANJI_CONSECUTIVE_BONUS = 0;

    //// カタカナが連続する場合のボーナス
    //int KATAKANA_CONSECUTIVE_BONUS = 1000;

    //// 末尾がひらがなの連続にボーナスを与える場合のひらがな長
    //int TAIL_HIRAGANA_LEN = 0;  // 4

    //// 末尾がひらがなの連続場合のボーナス
    //int TAIL_HIRAGANA_BONUS = 0; //1000;

    //// 「漢字+の+漢字」の場合のボーナス
    //int KANJI_NO_KANJI_BONUS = 1500;

    // cost ファイルに登録がある場合のデフォルトのボーナス
    int DEFAULT_WORD_BONUS = 1000;

    // cost ファイルに登録がある unigram のデフォルトのボーナスカウント
    int DEFAULT_UNIGRAM_BONUS_COUNT = 10000;

    // 2文字以上の形態素で漢字を含む場合のボーナス
    int MORPH_ANY_KANJI_BONUS = 5000;

    // 3文字以上の形態素ですべてひらがなの場合のボーナス
    int MORPH_ALL_HIRAGANA_BONUS = 1000;

    // 2文字以上の形態素ですべてカタカナの場合のボーナス
    int MORPH_ALL_KATAKANA_BONUS = 3000;

    // 2文字の形態素で先頭が高頻度助詞の場合のボーナス
    int HEAD_HIGH_FREQ_JOSHI_BONUS = 3000;

    // SingleHitの高頻度助詞が、マルチストロークに使われているケースのコスト
    int SINGLE_HIT_HIGH_FREQ_JOSHI_KANJI_COST = 5000;

    // 孤立したカタカナのコスト
    int ISOLATED_KATAKANA_COST = 3000;

    // 孤立した漢字のコスト
    int ONE_KANJI_COST = 1000;

    // 1スペースのコスト
    int ONE_SPACE_COST = 3000;

    // デフォルトの最大コスト
    int DEFAULT_MAX_COST = 1000;

    // Ngramコストに対する係数
    int NGRAM_COST_FACTOR = 5;

    // Online Ngram のカウントを水増しする係数
    int ONLINE_FREQ_BOOST_FACTOR = 10;

    //// Online 3gram のカウントからボーナス値を算出する際の係数
    //int ONLINE_TRIGRAM_BONUS_FACTOR = 100;

    //int TIER1_NUM = 5;
    //int TIER2_NUM = 10;

    // int STROKE_COST = 150;

    // Ngram統計によるコスト
    // 1-2gramはポジティブコスト(そのまま計上)、3gram以上はネガティブコスト(DEFAULT_MAX_COST を引いて計上)として計算される
    std::map<MString, int> ngramCosts;

    // 利用者が手作業で作成した単語コスト(3gram以上、そのまま計上)
    std::map<MString, int> userWordCosts;

    // 利用者が手作業で作成したNgram統計
    std::map<MString, int> userNgram;
    int userMaxFreq = 0;

    // システムで用意したNgram統計
    std::map<MString, int> systemNgram;
    int systemMaxFreq = 0;

    // 利用者が入力した文字列から抽出したNgram統計
    std::map<MString, int> onlineNgram;
    int onlineMaxFreq = 0;

    // 常用対数でNgram頻度を補正した値に掛けられる係数
    int ngramLogFactor = 50;

    // Ngram頻度がオンラインで更新されたか
    bool onlineNgram_updated = false;

    inline bool isDecimalString(StringRef item) {
        return utils::reMatch(item, L"[+\\-]?[0-9]+");
    }

    inline void _updateNgramCost(const MString& word, int sysCount, int usrCount, int onlCount) {
        if (word.size() <= 2) {
            // 1-2 gram は正のコスト
            int total = sysCount + usrCount + onlCount * ONLINE_FREQ_BOOST_FACTOR;
            if (total > 0) ngramCosts[word] = DEFAULT_MAX_COST - (int)(std::log(total) * ngramLogFactor);
        } else if (word.size() == 3) {
            // 3gramは負のコスト
            int tier1 = 0;
            int tier2 = 0;
            int tier3 = 0;
            int bonumFactor = SETTINGS->onlineTrigramBonusFactor;
            int nTier1 = SETTINGS->onlineTrigramTier1Num;
            int nTier2 = SETTINGS->onlineTrigramTier2Num;
            if (onlCount >= nTier1 + nTier2) {
                tier3 = onlCount - nTier1 + nTier2;
                tier2 = nTier2;
                tier1 = nTier1;
            } else if (onlCount >= nTier1) {
                tier2 = onlCount - nTier1;
                tier1 = nTier1;
            }
            ngramCosts[word] = -(tier1 * (bonumFactor*10) + tier2 * bonumFactor + (tier3 + sysCount) * (bonumFactor/10));

            //// 上のやり方は、間違いの方の影響も拡大してしまうので、結局、あまり意味が無いと思われる
            //ngramCosts[word] = -(onlCount * bonumFactor + (sysCount + usrCount) * (bonumFactor/10));
        }
        if (ngramCosts[word] < -100000) {
            LOG_WARNH(L"ABNORMAL COST: word={}, cost={}, sysCount={}, usrCount={}, onlCount={}", to_wstr(word), ngramCosts[word], sysCount, usrCount, onlCount);
        }
    }

    // オンラインでのNgram更新
    void _updateOnlineNgramByWord(const MString& word) {
        int count = onlineNgram[word] += 1;
        _updateNgramCost(word, 0, 0, count);
        onlineNgram_updated = true;
    }

    // ngramCosts の初期作成
    void makeInitialNgramCostMap() {
        ngramLogFactor = (int)((DEFAULT_MAX_COST - 50) / std::log(systemMaxFreq + userMaxFreq + onlineMaxFreq * ONLINE_FREQ_BOOST_FACTOR));
        _LOG_DETAIL(L"ENTER: systemMaxFreq={}, onlineMaxFreq={}, userMaxFreq={}, ngramLogFactor={}", systemMaxFreq, onlineMaxFreq, userMaxFreq, ngramLogFactor);
        ngramCosts.clear();
        for (auto iter = systemNgram.begin(); iter != systemNgram.end(); ++iter) {
            const MString& word = iter->first;
            int sysCount = iter->second;
            auto iterOnl = onlineNgram.find(iter->first);
            int onlCount = iterOnl != onlineNgram.end() ? iterOnl->second : 0;
            auto iterUsr = userNgram.find(iter->first);
            int usrCount = iterUsr != userNgram.end() ? iterUsr->second : 0;
            _updateNgramCost(word, sysCount, usrCount, onlCount);
        }
        for (auto iter = userNgram.begin(); iter != userNgram.end(); ++iter) {
            const MString& word = iter->first;
            int usrCount = iter->second;
            if (usrCount == 0 || (usrCount < 0 && word.size() == 1)) {
                // カウント(orコスト)が 0 のもの、1文字で負のカウント(orコスト)のものは無視
                continue;
            }
            if (ngramCosts.find(word) == ngramCosts.end()) {
                // 未登録
                auto iterOnl = onlineNgram.find(iter->first);
                int onlCount = iterOnl != onlineNgram.end() ? iterOnl->second : 0;
                _updateNgramCost(word, 0, usrCount, onlCount);
            }
        }
        for (auto iter = onlineNgram.begin(); iter != onlineNgram.end(); ++iter) {
            const MString& word = iter->first;
            int onlCount = iter->second;
            if (ngramCosts.find(word) == ngramCosts.end()) {
                // 未登録
                _updateNgramCost(word, 0, 0, onlCount);
            }
        }
        _LOG_DETAIL(L"LEAVE: ngramCosts.size={}", ngramCosts.size());
    }

#define SYSTEM_NGRAM_FILE L"mixed_all.ngram.txt"
#define ONLINE_NGRAM_FILE L"online.ngram.txt"
#define USER_COST_FILE    L"userword.cost.txt"

    int _loadNgramFile(StringRef ngramFile, std::map<MString, int>& ngramMap) {
        auto path = utils::joinPath(SETTINGS->rootDir, ngramFile);
        _LOG_INFOH(_T("LOAD: {}"), path.c_str());
        int maxFreq = 0;
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            for (const auto& line : reader.getAllLines()) {
                auto items = utils::split(utils::replace_all(utils::strip(line), L" +", L"\t"), '\t');
                if (items.size() == 2 &&
                    items[0].size() >= 1 && items[0].size() <= 3 &&         // 1-3gramに限定
                    items[0][0] != L'#' && isDecimalString(items[1])) {

                    int count = std::stoi(items[1]);
                    MString word = to_mstr(items[0]);
                    ngramMap[word] = count;
                    if (maxFreq < count) maxFreq = count;
                }
            }
        }
        return maxFreq;
    }

    void _loadUserCostFile() {
        auto path = utils::joinPath(SETTINGS->rootDir, USER_COST_FILE);
        _LOG_DEBUGH(_T("LOAD: {}"), path.c_str());
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            userWordCosts.clear();
            userMaxFreq = 0;
            for (const auto& line : reader.getAllLines()) {
                auto items = utils::split(utils::replace_all(utils::strip(utils::reReplace(line, L"#.*$", L"")), L"[ \t]+", L"\t"), '\t');
                if (!items.empty() && !items[0].empty() && items[0][0] != L'#') {
                    MString word = to_mstr(items[0]);
                    if (word.size() == 1) {
                        // unigram
                        int count = DEFAULT_UNIGRAM_BONUS_COUNT;
                        if (items.size() >= 2 && isDecimalString(items[1])) {
                            count = std::stoi(items[1]);
                        }
                        userNgram[word] = count;
                        if (userMaxFreq < count) userMaxFreq = count;
                    } else {
                        if (items.size() >= 2 && isDecimalString(items[1])) {
                            userWordCosts[word] = std::stoi(items[1]);
                        } else if (items.size() == 1) {
                            userWordCosts[word] = -DEFAULT_WORD_BONUS;       // userword.cost のデフォルトは -DEFAULT_WORD_BONUS
                        }
                    }
                }
            }
        }
    }

    void loadCostFile(bool onlyUserFile = false) {
        _LOG_INFOH(L"CALLED: onlyUserFile={}", onlyUserFile);
        if (!onlyUserFile) {
#ifdef NDEBUG
            systemMaxFreq = _loadNgramFile(SYSTEM_NGRAM_FILE, systemNgram);
#endif
            onlineMaxFreq = _loadNgramFile(ONLINE_NGRAM_FILE, onlineNgram);

        }
        _loadUserCostFile();
        makeInitialNgramCostMap();
    }

    void saveOnlineCostFile() {
        _LOG_INFOH(L"CALLED: onlineNgram_updated={}", onlineNgram_updated);
        auto path = utils::joinPath(SETTINGS->rootDir, ONLINE_NGRAM_FILE);
        if (onlineNgram_updated) {
            if (utils::moveFileToBackDirWithRotation(path, SETTINGS->backFileRotationGeneration)) {
                _LOG_INFOH(_T("SAVE: {}"), path.c_str());
                utils::OfstreamWriter writer(path);
                if (writer.success()) {
                    for (const auto& pair : onlineNgram) {
                        String line;
                        line.append(to_wstr(pair.first));           // 単語
                        line.append(_T("\t"));
                        line.append(std::to_wstring(pair.second));  // カウント
                        writer.writeLine(utils::utf8_encode(line));
                    }
                    onlineNgram_updated = false;
                }
            }
        }
    }

    inline bool is_space_or_vbar(mchar_t ch) {
        return ch == ' ' || ch == '|';
    }

    void updateOnlineNgram(const MString& str) {
        _LOG_DETAIL(L"CALLED: str={}, collectOnlineNgram={}", to_wstr(str), SETTINGS->collectOnlineNgram);
        if (!SETTINGS->collectOnlineNgram) return;

        int strlen = (int)str.size();
        for (int pos = 0; pos < strlen; ++pos) {
            //// 1gramなら漢字以外は無視
            //if (utils::is_kanji(str[pos])) {
            //    _updateOnlineNgramByWord(str.substr(pos, 1));
            //}

            if (!utils::is_japanese_char_except_nakaguro(str[pos])) continue;
            // 1-gram
            _updateOnlineNgramByWord(str.substr(pos, 1));

            if (pos + 1 >= strlen || !utils::is_japanese_char_except_nakaguro(str[pos + 1])) continue;
            // 2-gram
            _updateOnlineNgramByWord(str.substr(pos, 2));

            if (pos + 2 >= strlen || !utils::is_japanese_char_except_nakaguro(str[pos + 2])) continue;
            // 3-gram
            _updateOnlineNgramByWord(str.substr(pos, 3));

            //if (pos + 3 >= strlen || !utils::is_japanese_char_except_nakaguro(str[pos + 3])) continue;
            //_updateOnlineNgramByWord(str.substr(pos, 4));
        }
    }

    void updateOnlineNgram() {
        updateOnlineNgram(OUTPUT_STACK->backStringUptoPunctWithFlag());
    }

    // 2～4gramに対する利用者定義コストを計算
    int getUserWordCost(const MString& word) {
        auto iter = userWordCosts.find(word);
        if (iter != userWordCosts.end()) {
            int xCost = iter->second;
            // 利用者が定義したコストはそのまま返す
            _LOG_DETAIL(L"{}: userWordCost={}", to_wstr(word), xCost);
            return xCost;
        }
        return 0;
    }

    // 1～2gramに対する(ポジティブ)コストを計算
    int get_base_ngram_cost(const MString& word) {
        if (word.empty()) return 0;
        if (word.size() == 1 && word == MSTR_SPACE) return ONE_SPACE_COST;          // 1 space の場合のコスト
        int cost = word.size() == 2 ? getUserWordCost(word) : 0;
        if (cost != 0) return cost;
        auto iter = ngramCosts.find(word);
        cost = iter != ngramCosts.end() ? iter->second : DEFAULT_MAX_COST;
        if (cost < -100000) {
            _LOG_DETAIL(L"ABNORMAL COST: word={}, cost={}", to_wstr(word), cost);
        }
        return cost;
    }

    // 2～4gramに対する追加(ネガティブ)コストを計算
    int getExtraNgramCost(const MString& word) {
        int xCost = getUserWordCost(word);
        if (xCost == 0 && word.size() > 2) {
            auto iter = ngramCosts.find(word);
            if (iter != ngramCosts.end()) {
                // システムによるNgramのコストはネガティブコスト
                xCost = iter->second;
                // 正のコストが設定されている場合(wikipedia.costなど)は、 DEFAULT BONUS を引いたコストにする; つまり負のコストになる
                if (xCost > 0 && xCost < DEFAULT_WORD_BONUS) xCost -= DEFAULT_WORD_BONUS;
                _LOG_DETAIL(L"{}: ngramCosts={}", to_wstr(word), xCost);
            }
        }
        return xCost;
    }

    //int getWordConnCost(const MString& s1, const MString& s2) {
    //    return get_base_ngram_cost(utils::last_substr(s1, 1) + utils::safe_substr(s2, 0, 1)) / 2;
    //}

    int findKatakanaLen(const MString& s, int pos) {
        int len = 0;
        for (; (size_t)(pos + len) < s.size(); ++len) {
            if (!utils::is_katakana(s[pos + len])) break;
        }
        return len;
    }

    int getKatakanaStringCost(const MString& str, int pos, int katakanaLen) {
        int restlen = katakanaLen;
        auto katakanaWord = to_wstr(utils::safe_substr(str, pos, katakanaLen));
        int xCost = 0;
        int totalCost = 0;
        while (restlen > 5) {
            // 6->4:3, 7->4:4, 8->4:3:3, 9->4:4:3, 10->4:4:4, ...
            auto word = utils::safe_substr(str, pos, 4);
            xCost = getExtraNgramCost(word);
            _LOG_DETAIL(L"KATAKANA: extraWord={}, xCost={}", to_wstr(word), xCost);
            if (xCost == 0) {
                word = utils::safe_substr(str, pos, 3);
                xCost = getExtraNgramCost(word);
                _LOG_DETAIL(L"KATAKANA: extraWord={}, xCost={}", to_wstr(word), xCost);
            }
            if (xCost == 0) {
                totalCost += xCost;
                _LOG_DETAIL(L"FOUND: katakana={}, totalCost={}", katakanaWord, totalCost);
            }
            pos += word.size() - 1;
            restlen -= word.size() - 1;
        }
        if (restlen == 5) {
            // 5->3:3
            auto word = utils::safe_substr(str, pos, 3);
            xCost = getExtraNgramCost(word);
            _LOG_DETAIL(L"KATAKANA: extraWord={}, xCost={}", to_wstr(word), xCost);
            totalCost += xCost;
            _LOG_DETAIL(L"FOUND: katakana={}, totalCost={}", katakanaWord, totalCost);
            pos += 2;
            restlen -= 2;
        }
        if (restlen == 4) {
            auto word = utils::safe_substr(str, pos, restlen);
            xCost = getExtraNgramCost(word);
            _LOG_DETAIL(L"KATAKANA: extraWord={}, xCost={}", to_wstr(word), xCost);
            if (xCost != 0) {
                totalCost += xCost;
                _LOG_DETAIL(L"FOUND: katakana={}, totalCost={}", katakanaWord, totalCost);
                pos += 3;
                restlen -= 3;
            } else {
                word = utils::safe_substr(str, pos, 3);
                xCost = getExtraNgramCost(word);
                _LOG_DETAIL(L"KATAKANA: extraWord={}, xCost={}", to_wstr(word), xCost);
                if (xCost != 0) {
                    totalCost += xCost;
                    _LOG_DETAIL(L"FOUND: katakana={}, totalCost={}", katakanaWord, totalCost);
                    pos += 1;
                    restlen -= 1;
                }
            }
        }
        if (restlen == 3) {
            auto word = utils::safe_substr(str, pos, restlen);
            xCost = getExtraNgramCost(word);
            _LOG_DETAIL(L"KATAKANA: extraWord={}, xCost={}", to_wstr(word), xCost);
            if (xCost != 0) {
                totalCost += xCost;
                _LOG_DETAIL(L"FOUND: katakana={}, totalCost={}", katakanaWord, totalCost);
            }
        }
        return totalCost;
    }


    MString substringBetweenPunctuations(const MString& str) {
        int endPos = (int)(str.size());
        // まず末尾の句点をスキップ
        for (; endPos > 0; --endPos) {
            if (str[endPos - 1] != L'。') break;
        }
        // 前方に向かって句点を検索
        int startPos = endPos - 1;
        for (; startPos > 0; --startPos) {
            if (str[startPos - 1] == L'。') break;
        }
        return utils::safe_substr(str, startPos, endPos - startPos);
    }

    inline bool isHighFreqJoshi(mchar_t mc) {
        return mc == L'が' || mc == L'を' || mc == L'に' || mc == L'の' || mc == L'で' || mc == L'は';
    }

#if 1
    // Ngramコストの取得
    int getNgramCost(const MString& str) {
        _LOG_DETAIL(L"ENTER: str={}", to_wstr(str));
        int cost = 0;
        int strLen = (int)(str.size());

        if (strLen <= 0) return 0;

        // unigram
        for (int i = 0; i < strLen; ++i) {
            if (utils::is_katakana(str[i])) {
                if ((i == 0 || !utils::is_katakana(str[i-1])) && (i + 1 == strLen || !utils::is_katakana(str[i+1]))) {
                    // 孤立したカタカナは高いコストを設定
                    cost += ISOLATED_KATAKANA_COST;
                    continue;
                }
            //} else if ((i == 0 || !utils::is_hiragana(str[i - 1])) && strLen == i + 2 && isHighFreqJoshi(str[i]) && utils::is_hiragana(str[i + 1])) {
                //// 先頭または漢字・カタカナの直後の2文字のひらがなで、1文字目が高頻度の助詞(が、を、に、の、で、は)なら、ボーナスを与付して、ひらがな2文字になるようにする
                // こちらはいろいろと害が多い(からです⇒朝です、食べさせると青⇒食べ森書がの、など)
            } else if ((i == 0) && strLen == i + 2 && isHighFreqJoshi(str[i]) && utils::is_hiragana(str[i + 1])) {
                // 先頭の2文字のひらがなで、1文字目が高頻度の助詞(が、を、に、の、で、は)なら、ボーナスを与付して、ひらがな2文字になるようにする
                cost -= HEAD_HIGH_FREQ_JOSHI_BONUS;
            }
            // 通常の unigram コストの計上
            cost += get_base_ngram_cost(utils::safe_substr(str, i, 1));
        }
        _LOG_DETAIL(L"Unigram cost={}", cost);

        if (strLen == 1) {
            if (utils::is_kanji(str[0])) {
                cost += ONE_KANJI_COST;    // 文字列が漢字１文字の場合のコスト
                _LOG_DETAIL(L"Just one Kanji (+{}): cost={}", ONE_KANJI_COST, cost);
            }
        } else if (strLen > 1) {
            // bigram
            //int lastKanjiPos = -1;
            for (int i = 0; i < strLen - 1; ++i) {
                // 通常の bigram コストの計上
                cost += get_base_ngram_cost(utils::safe_substr(str, i, 2));

                //// 漢字が2文字連続する場合のボーナス
                //if (i > lastKanjiPos && utils::is_kanji(str[i]) && utils::is_kanji(str[i + 1])) {
                //    cost -= KANJI_CONSECUTIVE_BONUS;
                //    lastKanjiPos = i + 1;   // 漢字列が3文字以上続くときは、重複計上しない
                //}

                //if (utils::is_katakana(str[i]) && utils::is_katakana(str[i + 1])) {
                //    // カタカナが2文字連続する場合のボーナス
                //    cost -= KATAKANA_CONSECUTIVE_BONUS;
                //    int katakanaLen = findKatakanaLen(str, i);
                //    if (katakanaLen > 0) {
                //        // カタカナ連なら、次の文字種までスキップ
                //        i += katakanaLen - 1;
                //        continue;
                //    }
                //}

                //if ((utils::is_kanji(str[i]) && str[i + 1] == CHOON) || (str[i] == CHOON && utils::is_kanji(str[i + 1]))) {
                //    // 漢字と「ー」の隣接にはペナルティ
                //    cost += KANJI_CONSECUTIVE_PENALTY;
                //}
            }
            _LOG_DETAIL(L"Bigram cost={}", cost);

            if (strLen > 2) {
                // trigram
                int i = 0;
                while (i < strLen - 2) {
                    // 末尾に3文字以上残っている
                    //bool found = false;
                    int katakanaLen = findKatakanaLen(str, i);
                    if (katakanaLen >= 3) {
                        // カタカナの3文字以上の連続
                        cost += getKatakanaStringCost(str, i, katakanaLen);
                        // カタカナ連の末尾に飛ばす
                        i += katakanaLen - 1;
                        continue;
                        //found = true;
                    }
                    if (i < strLen - 3) {
                        // 4文字連
                        //if (TAIL_HIRAGANA_LEN >= 4 && (i == strLen - TAIL_HIRAGANA_LEN) && utils::is_hiragana_str(utils::safe_substr(str, i, TAIL_HIRAGANA_LEN))) {
                        //    // 末尾がひらがな4文字連続の場合のボーナス
                        //    cost -= TAIL_HIRAGANA_BONUS;
                        //    _LOG_DETAIL(L"TAIL HIRAGANA:{}, cost={}", to_wstr(utils::safe_substr(str, i, TAIL_HIRAGANA_LEN)), cost);
                        //    break;
                        //}
                        int len = 4;
                        auto word = utils::safe_substr(str, i, len);
                        int xCost = getExtraNgramCost(word);
                        _LOG_DETAIL(L"len={}, extraWord={}, xCost={}", len, to_wstr(word), xCost);
                        if (xCost != 0) {
                            // コスト定義があれば、利用者定義によるもの
                            cost += xCost;
                            _LOG_DETAIL(L"FOUND: extraWord={}, xCost={}, cost={}", to_wstr(word), xCost, cost);
                            //i += len - 1;
                            //continue;
                            //found = true;
                        }
                    }
                    {
                        // 3文字連
                        int len = 3;
                        auto word = utils::safe_substr(str, i, len);
                        int xCost = getExtraNgramCost(word);
                        _LOG_DETAIL(L"len={}, extraWord={}, xCost={}", len, to_wstr(word), xCost);
                        if (xCost != 0) {
                            // コスト定義がある
                            cost += xCost;
                            _LOG_DETAIL(L"FOUND: extraWord={}, xCost={}, cost={}", to_wstr(word), xCost, cost);
                            // 次の位置へ
                            //i += len;
                            //found = true;
                        }
                    }
#if 0
                    {
                        // 3文字連
                        // 「漢字+の+漢字」のような場合はボーナス
                        if (SETTINGS->kanjiNoKanjiBonus > 0) {
                            if ((str[i + 1] == L'が' || str[i + 1] == L'の' /* || str[i + 1] == L'で' */ || str[i + 1] == L'を') && !utils::is_hiragana(str[i]) && !utils::is_hiragana(str[i + 2])) {
                                cost -= KANJI_NO_KANJI_BONUS;
                                _LOG_DETAIL(L"KANJI-NO-KANJI:{}, cost={}", to_wstr(utils::safe_substr(str, i, 3)), cost);
                            }
                        }
                    }
#endif
                    ++i;
                }
            }
        }
        _LOG_DETAIL(L"LEAVE: cost={}", cost);
        return cost;
    }
#else
    int getNgramCost(const MString& str) {
        int cost = 0;
        for (size_t i = 0; i < str.size() - 1; ++i) {
            int bigramCost = get_base_ngram_cost(utils::safe_substr(str, i, 2));
            if (bigramCost >= DEFAULT_MAX_COST) {
                bigramCost = get_base_ngram_cost(utils::safe_substr(str, i, 1)) + get_base_ngram_cost(utils::safe_substr(str, i + 1, 1));
            }
            cost += bigramCost;
        }
        return cost;
    }
#endif

    // 候補文字列
    class CandidateString {
        MString _str;
        int _strokeLen;
        int _cost;
        int _penalty;
        //float _llama_loss = 0.0f;

        // 末尾文字列にマッチする RewriteInfo を取得する
        std::tuple<const RewriteInfo*, int> matchWithTailString(const PostRewriteOneShotNode* rewriteNode) const {
            size_t maxlen = SETTINGS->kanaTrainingMode && ROOT_STROKE_NODE->hasOnlyUsualRewriteNdoe() ? 0 : 8;     // かな入力練習モードで濁点のみなら書き換えをやらない
            //bool bAllKeyUp = false; //OUTPUT_STACK->isAllKeyUp();
            bool bAllKeyUp = OUTPUT_STACK->isAllKeyUp();
            while (maxlen > 0) {
                _LOG_DEBUGH(_T("maxlen={}"), maxlen);
                const MString targetStr = utils::safe_tailstr(_str, maxlen);
                _LOG_DEBUGH(_T("targetStr={}"), to_wstr(targetStr));
                if (targetStr.empty()) break;

                const RewriteInfo* rewInfo = 0;
                if (!bAllKeyUp) rewInfo = rewriteNode->getRewriteInfo(targetStr + MSTR_PLUS);        // ロールオーバー打ちのときは"+"を付加したエントリを検索
                if (!rewInfo) rewInfo = rewriteNode->getRewriteInfo(targetStr);
                if (rewInfo) {
                    _LOG_DEBUGH(_T("REWRITE_INFO found: outStr={}, rewritableLen={}, subTable={:p}"), to_wstr(rewInfo->rewriteStr), rewInfo->rewritableLen, (void*)rewInfo->subTable);
                    return { rewInfo, (int)targetStr.size() };
                }

                maxlen = targetStr.size() - 1;
            }
            return { 0, 0 };
        }

    public:
        CandidateString() : _strokeLen(0), _cost(0), _penalty(0) {
        }

        CandidateString(const MString& s, int len, int cost, int penalty) : _str(s), _strokeLen(len), _cost(cost), _penalty(penalty) {
        }

        CandidateString(const CandidateString& cand, int strokeDelta) : _str(cand._str), _strokeLen(cand._strokeLen+strokeDelta), _cost(cand._cost), _penalty(cand._penalty) {
        }

        std::tuple<MString, int> applyAutoBushu(const WordPiece& piece, int strokeCount) const {
            MStringResult resultOut;
            if (_strokeLen + piece.strokeLen() == strokeCount) {
                if (SETTINGS->autoBushuCompMinCount > 0 && BUSHU_DIC) {
                    if (_str.size() > 0 && piece.getString().size() == 1) {
                        // 自動部首合成の実行
                        BUSHU_COMP_NODE->ReduceByAutoBushu(_str.back(), piece.getString().front(), resultOut);
                        if (!resultOut.resultStr().empty()) {
                            MString s(_str);
                            s.back() = resultOut.resultStr().front();
                            return { s, 1 };
                        }
                        //mchar_t m = BUSHU_DIC->FindAutoComposite(_str.back(), piece.getString().front());
                        ////if (m == 0) m = BUSHU_DIC->FindComposite(_str.back(), piece.getString().front(), 0);
                        //_LOG_DEBUGH(_T("BUSHU_DIC->FindAutoComposite({}, {}) -> {}"),
                        //    to_wstr(utils::safe_tailstr(_str, 1)), to_wstr(utils::safe_substr(piece.getString(), 0, 1)), to_wstr(m));
                        //if (m != 0) {
                        //    MString s(_str);
                        //    s.back() = m;
                        //    return { s, 1 };
                        //}
                    }
                }
            }
            // 空を返す
            return { resultOut.resultStr(), resultOut.numBS()};
        }

        MString applyBushuComp() const {
            if (BUSHU_DIC) {
                if (_str.size() >= 2) {
                    // 部首合成の実行
                    MString ms = BUSHU_COMP_NODE->ReduceByBushu(_str[_str.size() - 2], _str[_str.size() - 1]);
                    _LOG_DEBUGH(_T("BUSHU_COMP_NODE->ReduceByBushu({}, {}) -> {}"),
                        to_wstr(_str[_str.size() - 2]), to_wstr(_str[_str.size() - 1]), to_wstr(ms));
                    if (!ms.empty()) {
                        MString s(_str.substr(0, _str.size() - 2));
                        s.append(1, ms[0]);
                        return s;
                    }
                    //mchar_t m = BUSHU_DIC->FindComposite(_str[_str.size() - 2], _str[_str.size() - 1], '\0');
                    //_LOG_DEBUGH(_T("BUSHU_DIC->FindComposite({}, {}) -> {}"),
                    //    to_wstr(_str[_str.size() - 2]), to_wstr(_str[_str.size() - 1]), to_wstr(m));
                    //if (m != 0) {
                    //    MString s(_str.substr(0, _str.size() - 2));
                    //    s.append(1, m);
                    //    return s;
                    //}
                }
            }
            return EMPTY_MSTR;
        }

        // 単語素片を末尾に適用してみる
        std::tuple<MString, int> applyPiece(const WordPiece& piece, int strokeCount) const {
            if (_strokeLen + piece.strokeLen() == strokeCount) {
                // 素片のストローク数が適合した
                int numBS;
                if (piece.rewriteNode()) {
                    const RewriteInfo* rewInfo;
                    std::tie(rewInfo, numBS) = matchWithTailString(piece.rewriteNode());

                    if (rewInfo) {
                        return { utils::safe_substr(_str, 0, -numBS) + rewInfo->rewriteStr, numBS };
                    } else {
                        return { _str + piece.rewriteNode()->getString(), 0 };
                    }

                } else {
                    numBS = piece.numBS();
                    if (numBS > 0) {
                        if ((size_t)numBS < _str.size()) {
                            return { utils::safe_substr(_str, 0, (int)(_str.size() - numBS)), numBS };
                        } else {
                            return { EMPTY_MSTR, numBS };
                        }
                    } else {
                        return { _str + piece.getString(), 0 };
                    }
                }
            }
            return { EMPTY_MSTR, 0 };
        }

        const MString& string() const {
            return _str;
        }

        const int strokeLen() const {
            return _strokeLen;
        }

        int totalCost() const {
            return _cost + _penalty;
        }

        void cost(int cost) {
            _cost = cost;
        }

        //void zeroCost() {
        //    _penalty = -_cost;
        //}

        int penalty() const {
            return _penalty;
        }

        void penalty(int penalty) {
            _penalty = penalty;
        }

        void zeroPenalty() {
            _penalty = 0;
        }

        //float llama_loss() const {
        //    return _llama_loss;
        //}

        //void llama_loss(float loss) {
        //    _llama_loss = loss;
        //}

        String debugString() const {
            return to_wstr(_str)
                + _T(" (totalCost=") + std::to_wstring(totalCost())
                + _T("(_cost=") + std::to_wstring(_cost)
                + _T(",_penalty=") + std::to_wstring(_penalty)
                //+ _T(",_llama_loss=") + std::to_wstring(_llama_loss)
                + _T("), strokeLen=") + std::to_wstring(_strokeLen) + _T(")");
        }
    };

    // 先頭候補以外に、非優先候補ペナルティを与える (先頭候補のペナルティは 0 にする)
    void arrangePenalties(std::vector<CandidateString>& candidates, size_t nSameLen) {
        _LOG_DETAIL(_T("CALLED"));
        candidates.front().zeroPenalty();
        for (size_t i = 1; i < nSameLen; ++i) {
            candidates[i].penalty(NON_PREFERRED_PENALTY * (int)i);
        }
    }

    // ストローク長の同じ候補の数を返す
    size_t getNumOfSameStrokeLen(const std::vector<CandidateString>& candidates) {
        size_t nSameLen = 0;
        if (candidates.size() > 1) {
            int strokeLen = candidates.front().strokeLen();
            ++nSameLen;
            for (auto iter = candidates.begin() + 1; iter != candidates.end() && iter->strokeLen() == strokeLen; ++iter) {
                ++nSameLen;
            }
        }
        return nSameLen;
    }

    // 先頭候補を最優先候補にする
    void selectFirst(std::vector<CandidateString>& candidates) {
        size_t nSameLen = getNumOfSameStrokeLen(candidates);
        if (nSameLen > 1) {
            arrangePenalties(candidates, nSameLen);
            _LOG_INFOH(_T("CALLED: First candidate preferred."));
        }
    }

    // K-best な文字列を格納する
    class KBestList {

        std::vector<CandidateString> _candidates;

        bool _prevBS = false;

        // 次のストロークをスキップする候補文字列
        std::set<MString> _kanjiPreferredNextCands;

        static bool _isEmpty(const std::vector<CandidateString> cands) {
            //return cands.empty() || cands.size() == 1 && cands.front().string().empty();
            return cands.empty() || cands.front().string().empty();
        }

        String _debugLog;

    private:
        std::vector<bool> _highFreqJoshiStroke;
        std::vector<bool> _rollOverStroke;

        void setHighFreqJoshiStroke(int count, mchar_t ch) {
            if (count >= 0 && count < 1024) {
                if (count >= (int)_highFreqJoshiStroke.size()) {
                    _highFreqJoshiStroke.resize(count + 1);
                }
                if (isHighFreqJoshi(ch)) _highFreqJoshiStroke[count] = true;
            }
        }
        void setRollOverStroke(int count, bool flag) {
            if (count >= 0 && count < 1024) {
                if (count >= (int)_rollOverStroke.size()) {
                    _rollOverStroke.resize(count + 1);
                }
                _rollOverStroke[count] = flag;
            }
        }

        bool isSingleHitHighFreqJoshi(int count) const {
            return (size_t)count < _highFreqJoshiStroke.size() && (size_t)count < _rollOverStroke.size() ? _highFreqJoshiStroke[count] &&  !_rollOverStroke[count] : false;
        }

    public:
        void setPrevBS(bool flag) {
            _prevBS = flag;
        }

        bool isPrevBS() const {
            return _prevBS;
        }

        void clear(bool clearAll) {
            //_LOG_INFOH(L"CALLED: clearAll={}", clearAll);
            _candidates.clear();
            if (clearAll) _kanjiPreferredNextCands.clear();
        }

        void removeOtherThanKBest() {
            if (_candidates.size() > BestKSize) {
                _candidates.erase(_candidates.begin() + BestKSize, _candidates.end());
            }
        }

        void removeSecondOrLesser() {
            if (_candidates.size() > 0) {
                _candidates.erase(_candidates.begin() + 1, _candidates.end());
                _candidates.front().zeroPenalty();
            }
        }

        bool isEmpty() const {
            return _isEmpty(_candidates);
        }

        int size() const {
            return _candidates.size();
        }

        MString getTopString() {
            return _candidates.empty() ? MString() : _candidates[0].string();
        }

        String debugKBestString(size_t maxLn = 100000) const {
            String result = L"kanjiPreferredNextCands=" + kanjiPreferredNextCandsDebug() + L"\n\n";
            result.append(_debugLog);
            result.append(L"\nKBest:\n");
            for (size_t i = 0; i < _candidates.size() && i < maxLn; ++i) {
                result.append(std::to_wstring(i));
                result.append(_T(": "));
                result.append(_candidates[i].debugString());
                result.append(_T("\n"));
            }
            return result;
        }

    private:
        int calcMorphCost(const MString& s, std::vector<MString>& words) {
            int cost = 0;
            if (!s.empty()) {
                cost = MorphBridge::morphCalcCost(s, words);
                _LOG_DETAIL(L"{}: orig morphCost={}", to_wstr(s), cost);
                for (auto iter = words.begin(); iter != words.end(); ++iter) {
                    const MString& w = *iter;
                    if (w.size() >= 2 && std::any_of(w.begin(), w.end(), [](mchar_t c) { return utils::is_kanji(c); })) {
                        cost -= MORPH_ANY_KANJI_BONUS * (int)(w.size() - 1);
                    }
                    if (w.size() >= 3 && std::all_of(w.begin(), w.end(), [](mchar_t c) { return utils::is_hiragana(c); })) {
                        cost -= MORPH_ALL_HIRAGANA_BONUS;
                    }
                    if (w.size() >= 2 && std::all_of(w.begin(), w.end(), [](mchar_t c) { return utils::is_katakana(c); })) {
                        const MString& w2 = *(iter + 1);
                        if (!((iter + 1) != words.end() && std::all_of(w2.begin(), w2.end(), [](mchar_t c) { return utils::is_katakana(c); }) && w.size() + w2.size() <= 5)) {
                            // 次がカタカナ連でないか、合計で6文以上なら
                            cost -= MORPH_ALL_KATAKANA_BONUS;
                        }
                    }
                }
            }
            return cost;
        }
#if 0
        int totalCostWithMorph(const MString& candStr) {
            std::vector<MString> words;
            int morphCost = calcMorphCost(candStr, words);
            return morphCost;
        }
#endif

        float calcLlamaLoss(const MString& s) {
            std::vector<float> logits;
            auto loss = LlamaBridge::llamaCalcCost(s, logits);
            _LOG_INFOH(L"{}: loss={}, logits={}", to_wstr(s), loss, utils::join_primitive(logits, L","));
            return loss;
        }

        int calcLlamaCost(const MString& s) {
            std::vector<float> logits;
            auto loss = s.size() > 1 ? LlamaBridge::llamaCalcCost(s, logits) : 2.0f;
            int cost = (int)((loss / s.size()) * 1000);
            _LOG_INFOH(L"{}: loss={}, cost={}, logits={}", to_wstr(s), loss, cost, utils::join_primitive(logits, L","));
            return cost;
        }

        // 2つの文字列の末尾文字列の共通部分が指定の長さより長いか、または全く同じ文字列か
        bool hasLongerCommonSuffixThanOrSameStr(const MString& str1, const MString& str2, int len) {
            _LOG_DEBUGH(_T("ENTER: str1={}, str2={}, len={}"), to_wstr(str1), to_wstr(str2), len);
            int n1 = (int)str1.size() - 1;
            int n2 = (int)str2.size() - 1;
            while (n1 >= 0 && n2 >= 0 && len > 0) {
                if (str1[n1] != str2[n2]) break;
                --n1;
                --n2;
                --len;
            }
            _LOG_DEBUGH(_T("LEAVE: remainingLen: str1={}, str2={}, common={}"), n1, n2, len);
            return len == 0 || (n1 == 0 && n2 == 0);
        }

        // 新しい候補を追加
        bool addCandidate(std::vector<CandidateString>& newCandidates, CandidateString& newCandStr, bool isStrokeBS) {
            _LOG_INFOH(_T("ENTER: newCandStr={}, isStrokeBS={}"), newCandStr.debugString(), isStrokeBS);
            bool bAdded = false;
            bool bIgnored = false;
            const MString& candStr = newCandStr.string();
            MString subStr = substringBetweenPunctuations(candStr);

            std::vector<MString> words;
            int morphCost = !SETTINGS->useMorphAnalyzer || subStr.empty() ? 0 : calcMorphCost(subStr, words);

            int ngramCost = subStr.empty() ? 0 : getNgramCost(subStr) * NGRAM_COST_FACTOR;
            //int morphCost = 0;
            //int ngramCost = candStr.empty() ? 0 : getNgramCost(candStr);
            //int llamaCost = candStr.empty() ? 0 : calcLlamaCost(candStr) * NGRAM_COST_FACTOR;
            int llamaCost = 0;
            int candCost = morphCost + ngramCost + llamaCost;
            newCandStr.cost(candCost);

            //// 「漢字+の+漢字」のような場合はペナルティを解除
            //size_t len = candStr.size();
            //if (len >= 3 && candStr[len - 2] == L'の' && !utils::is_hiragana(candStr[len - 3]) && !utils::is_hiragana(candStr[len - 1])) {
            //    newCandStr.zeroPenalty();
            //}

            int totalCost = newCandStr.totalCost();

            _LOG_INFOH(_T("CALLED: candStr={}, totalCost={}, candCost={} (morph={}[{}], ngram={})"),
                to_wstr(candStr), totalCost, candCost, morphCost, to_wstr(utils::join(words, ' ')), ngramCost);

            if (IS_LOG_DEBUGH_ENABLED) {
                if (!isStrokeBS) _debugLog.append(std::format(L"candStr={}, totalCost={}, candCost={} (morph={} [{}] , ngram = {})\n",
                    to_wstr(candStr), totalCost, candCost, morphCost, to_wstr(utils::join(words, ' ')), ngramCost));
            }

            if (!newCandidates.empty()) {
                for (auto iter = newCandidates.begin(); iter != newCandidates.end(); ++iter) {
                    int otherCost = iter->totalCost();
                    _LOG_DEBUGH(_T("    otherStr={}, otherCost={}"), to_wstr(iter->string()), otherCost);
                    if (totalCost < otherCost) {
                        iter = newCandidates.insert(iter, newCandStr);    // iter は挿入したノードを指す
                        bAdded = true;
                        // 下位のノードで末尾文字列の共通部分が指定の長さより長いものを探し、あればそれを削除
                        for (++iter; iter != newCandidates.end(); ++iter) {
                            if (hasLongerCommonSuffixThanOrSameStr(candStr, iter->string(), LastSameLen)) {
                                // 末尾文字列の共通部分が指定の長さより長いか、同じ文字列
                                newCandidates.erase(iter);
                                _LOG_DEBUGH(_T("    REMOVE second best or lesser candidate"));
                                break;
                            }
                        }
                        break;
                    } else if (hasLongerCommonSuffixThanOrSameStr(candStr, iter->string(), LastSameLen)) {
                        // 末尾文字列の共通部分が指定の長さより長いか、同じ文字列
                        bIgnored = true;
                        break;
                    }
                }
            }
            if (!bAdded && !bIgnored && newCandidates.size() < BestKSize) {
                // 余裕があれば末尾に追加
                newCandidates.push_back(newCandStr);
                bAdded = true;
            }
            if (newCandidates.size() > BestKSize) {
                // kBestサイズを超えたら末尾を削除
                newCandidates.resize(BestKSize);
                _LOG_DETAIL(_T("    REMOVE OVERFLOW ENTRY"));
            }
            if (bAdded) {
                _LOG_DETAIL(_T("    ADD candidate: {}"), to_wstr(candStr));
            } else {
                _LOG_DETAIL(_T("    ABANDON candidate: {}, totalCost={}"), to_wstr(candStr), totalCost);
            }
            _LOG_INFOH(_T("LEAVE: {}, newCandidates.size={}"), bAdded ? L"ADD" : L"ABANDON", newCandidates.size());
            return bAdded;
        }

    private:
        // 素片のストロークと適合する候補だけを追加
        void addOnePiece(std::vector<CandidateString>& newCandidates, const WordPiece& piece, int strokeCount) {
            _LOG_DETAIL(_T("ENTER: _candidates.size={}, piece={}"), _candidates.size(), piece.debugString());
            bool bAutoBushuFound = false;           // 自動部首合成は一回だけ実行する
            bool isStrokeBS = piece.numBS() > 0;
            const MString& pieceStr = piece.getString();
            //int topStrokeLen = -1;

            if (piece.getString().size() == 1) setHighFreqJoshiStroke(strokeCount, piece.getString()[0]);

            int singleHitHighFreqJoshiCost = piece.strokeLen() > 1 && isSingleHitHighFreqJoshi(strokeCount - (piece.strokeLen() - 1)) ? SINGLE_HIT_HIGH_FREQ_JOSHI_KANJI_COST : 0;

            for (const auto& cand : _candidates) {
                //if (topStrokeLen < 0) topStrokeLen = cand.strokeLen();
                _LOG_DETAIL(_T("cand.strokeLen={}, piece.strokeLen()={}, strokeCount={}"), cand.strokeLen(),piece.strokeLen(), strokeCount);
                if (cand.strokeLen() + piece.strokeLen() == strokeCount) {
                    // 素片のストロークと適合する候補
                    int penalty = cand.penalty();
                    _LOG_DETAIL(L"cand.string()=\"{}\", contained in kanjiPreferred={}", to_wstr(cand.string()), _kanjiPreferredNextCands.contains(cand.string()));
                    if (singleHitHighFreqJoshiCost > 0) {
                        // 複数ストロークによる入力で、2打鍵目がロールオーバーでなかったらペナルティ
                        penalty += singleHitHighFreqJoshiCost;
                        _LOG_DETAIL(L"Non rollover multi stroke penalty, total penalty={}", penalty);
                    }
                    if (!isStrokeBS && /*cand.strokeLen() == topStrokeLen && */ !pieceStr.empty()
                        && (piece.strokeLen() == 1 || std::all_of(pieceStr.begin(), pieceStr.end(), [](mchar_t c) { return utils::is_hiragana(c);}))
                        && _kanjiPreferredNextCands.contains(cand.string())) {
                        // 漢字優先
                        _LOG_DETAIL(_T("add NON_PREFERRED_PENALTY"));
                        penalty += NON_PREFERRED_PENALTY;
                    }
                    MString s;
                    int numBS;
                    if (!bAutoBushuFound) {
                        std::tie(s, numBS) = cand.applyAutoBushu(piece, strokeCount);  // 自動部首合成
                        if (!s.empty()) {
                            CandidateString newCandStr(s, strokeCount, 0, penalty);
                            addCandidate(newCandidates, newCandStr, isStrokeBS);
                            bAutoBushuFound = true;
                        }
                    }
                    std::tie(s, numBS) = cand.applyPiece(piece, strokeCount);
                    if (!s.empty() || numBS > 0) {
                        CandidateString newCandStr(s, strokeCount, 0, penalty);
                        addCandidate(newCandidates, newCandStr, isStrokeBS);
                    }
                }
            }
            _LOG_DETAIL(_T("LEAVE"));
        }

        // 末尾から、指定の長さより以前の部分を確定させる
        void commitLeaderBeforeTailLen() {
            if (!_candidates.empty()) {
                MString firstStr = _candidates.front().string();
                if ((int)firstStr.size() > SETTINGS->commitBeforeTailLen) {
                    MString leaderStr = firstStr.substr(0, firstStr.size() - SETTINGS->commitBeforeTailLen);
                    std::vector<CandidateString> tempCands;
                    auto iter = _candidates.begin();
                    tempCands.push_back(*iter++);
                    for (; iter != _candidates.end(); ++iter) {
                        if (utils::startsWith(iter->string(), leaderStr)) {
                            // 先頭部分が一致する候補だけを残す
                            tempCands.push_back(*iter);
                        }
                    }
                    _candidates = std::move(tempCands);
                }
            }
        }

        // CurrentStroke の候補を削除する
        void removeCurrentStrokeCandidates(std::vector<CandidateString>& newCandidates, int strokeCount) {
            _LOG_INFOH(L"ENTER: _candidates.size={}, prevBS={}, strokeCount={}", _candidates.size(), _prevBS, strokeCount);
            if (!_prevBS) {
                if (!_candidates.empty()) {
                    const auto& firstCand = _candidates.front();
                    int delta = 2;
                    for (const auto& cand : _candidates) {
                        if (cand.strokeLen() + delta <= strokeCount) {
                            if (cand.string() == firstCand.string()) {
                                // 先頭と同じ文字列の候補だったら、そのストローク数の他の候補も残さない
                                ++delta;
                                continue;
                            }
                            // 1ストローク以上前の候補を残す
                            CandidateString newCand(cand, delta);
                            _LOG_DETAIL(L"add cand={}", newCand.debugString());
                            newCandidates.push_back(newCand);
                        }
                    }
                }
            }
            _LOG_INFOH(L"LEAVE: {} candidates", newCandidates.size());
        }

        //// Strokeを一つ進める
        //void advanceStroke(std::vector<CandidateString>& newCandidates) {
        //    _LOG_INFOH(L"ENTER: _candidates.size={}", _candidates.size());
        //    for (const auto& cand : _candidates) {
        //        CandidateString newCand(cand, 1);
        //        _LOG_DETAIL(L"add cand={}", newCand.debugString());
        //        newCandidates.push_back(newCand);
        //    }
        //    _LOG_INFOH(L"LEAVE: {} candidates", newCandidates.size());
        //}

        // llama-loss の小さい順に候補を並べ直す
        //void sortByLlamaLoss(std::vector<CandidateString>& newCandidates) {
        //    std::for_each(newCandidates.begin(), newCandidates.end(), [this](CandidateString& c) {
        //        c.llama_loss(calcLlamaLoss(c.string()));
        //    });
        //    std::sort(newCandidates.begin(), newCandidates.end(), [](const CandidateString& a, const CandidateString& b) {
        //        return a.llama_loss() < b.llama_loss();
        //    });
        //}

        bool isKanjiKatakanaConsecutive(const CandidateString& cand) {
            MString str = cand.string();
            size_t len = str.size();
            return len >= 2 && (utils::is_kanji(str[len - 1]) && utils::is_kanji(str[len - 2]) || utils::is_katakana(str[len - 1]) && utils::is_katakana(str[len - 2]));
        }

        // return: strokeBack による戻しがあったら、先頭を優先する
        std::vector<CandidateString> _updateKBestList_sub(const std::vector<WordPiece>& pieces, int strokeCount, bool strokeBack) {
            std::vector<CandidateString> newCandidates;
            if (strokeBack) {
                // strokeBackの場合
                //_LOG_DETAIL(L"strokeBack");
                removeCurrentStrokeCandidates(newCandidates, strokeCount);
                if (!newCandidates.empty()) {
                    // 戻した先頭を優先しておく
                    lattice2::selectFirst(newCandidates);
                    return newCandidates;
                }
                // 以前のストロークの候補が無ければ、通常のBSの動作とする
                removeSecondOrLesser();
            }
            bool isBSpiece = pieces.size() == 1 && pieces.front().isBS();
            _prevBS = isBSpiece;
            // BS でないか、以前の候補が無くなっていた
            for (const auto& piece : pieces) {
                // 素片のストロークと適合する候補だけを追加
                addOnePiece(newCandidates, piece, strokeCount);
            }

            //sortByLlamaLoss(newCandidates);

            // stroke位置的に組み合せ不可だったものは、strokeCount が範囲内なら残しておく
            if (!isBSpiece && !_isEmpty(newCandidates)) {     // isEmpty()だったら、BSなどで先頭のものだけが残されたということ
                for (const auto& cand : _candidates) {
                    if (cand.strokeLen() + AllowedStrokeRange > strokeCount) {
                        newCandidates.push_back(cand);
                    }
                }
            }
            return newCandidates;
        }

    public:
        // strokeCount: lattice に最初に addPieces() した時からの相対的なストローク数
        void updateKBestList(const std::vector<WordPiece>& pieces, int strokeCount, bool strokeBack) {
            _LOG_DETAIL(_T("ENTER: strokeCount={}, strokeBack={}"), strokeCount, strokeBack);
            _debugLog.clear();

            setRollOverStroke(strokeCount - 1, STATE_COMMON->IsRollOverStroke());

            // 候補リストが空の場合は、追加される piece と組み合わせるための、先頭を表すダミーを用意しておく
            addDummyCandidate();

            _candidates = std::move(_updateKBestList_sub(pieces, strokeCount, strokeBack));

            //// 漢字またはカタカナが2文字以上連続したら、その候補を優先する
            //if (!_candidates.empty()) {
            //    if (isKanjiKatakanaConsecutive(_candidates.front())) selectFirst();
            //}
#if 0
            // 末尾から、指定の長さより以前の部分を確定させる
            commitLeaderBeforeTailLen();
#endif
            _LOG_DETAIL(_T("LEAVE"));
        }

    private:
        // ストローク長の同じ候補の数を返す
        size_t getNumOfSameStrokeLen() const {
            //size_t nSameLen = 0;
            //if (_candidates.size() > 1) {
            //    int strokeLen = _candidates.front().strokeLen();
            //    ++nSameLen;
            //    for (auto iter = _candidates.begin() + 1; iter != _candidates.end() && iter->strokeLen() == strokeLen; ++iter) {
            //        ++nSameLen;
            //    }
            //}
            //return nSameLen;
            return lattice2::getNumOfSameStrokeLen(_candidates);
        }

        // 先頭候補以外に、非優先候補ペナルティを与える (先頭候補のペナルティは 0 にする)
        void arrangePenalties(size_t nSameLen) {
            //_candidates.front().zeroPenalty();
            //for (size_t i = 1; i < nSameLen; ++i) {
            //    _candidates[i].penalty(NON_PREFERRED_PENALTY * (int)i);
            //}
            return lattice2::arrangePenalties(_candidates, nSameLen);
        }

    public:
        void setKanjiPreferredNextCands() {
            //_LOG_INFOH(L"ENTER");
            _kanjiPreferredNextCands.clear();
            if (!_candidates.empty()) {
                int topStrokeCnt = _candidates.front().strokeLen();
                for (const auto& c : _candidates) {
                    if (c.strokeLen() != topStrokeCnt) break;
                    _kanjiPreferredNextCands.insert(c.string());
                }
            }
            if (_kanjiPreferredNextCands.empty()) _kanjiPreferredNextCands.insert(EMPTY_MSTR);
            //_LOG_INFOH(L"LEAVE: kanjiPreferredNextCands={}", kanjiPreferredNextCandsDebug());
        }

        String kanjiPreferredNextCandsDebug() const {
            return std::to_wstring(_kanjiPreferredNextCands.size()) + L":[" + to_wstr(utils::join(_kanjiPreferredNextCands, L',')) + L"]";
        }

        // 先頭を表すダミーを用意しておく
        void addDummyCandidate() {
            if (_candidates.empty()) {
                _candidates.push_back(CandidateString());
            }
        }

        // 先頭候補を最優先候補にする
        void selectFirst() {
            _LOG_DETAIL(_T("CALLED"));
            //size_t nSameLen = getNumOfSameStrokeLen();
            //if (nSameLen > 1) {
            //    arrangePenalties(nSameLen);
            //    _LOG_INFOH(_T("CALLED: First candidate preferred."));
            //}
            lattice2::selectFirst(_candidates);
        }

        // 次候補を最優先候補にする
        void selectNext() {
            _LOG_DETAIL(_T("CALLED"));
            size_t nSameLen = getNumOfSameStrokeLen();
            if (nSameLen > 1) {
                auto begin = _candidates.begin();
                std::rotate(begin, begin + 1, begin + nSameLen);
                arrangePenalties(nSameLen);
            }
        }

        // 前候補を最優先候補にする
        void selectPrev() {
            _LOG_DETAIL(_T("CALLED"));
            size_t nSameLen = getNumOfSameStrokeLen();
            if (nSameLen > 1) {
                auto begin = _candidates.begin();
                std::rotate(begin, begin + nSameLen - 1, begin + nSameLen);
                arrangePenalties(nSameLen);
            }
        }

        // 部首合成
        void updateByBushuComp() {
            _LOG_DETAIL(_T("CALLED"));
            if (!_candidates.empty()) {
                MString s = _candidates.front().applyBushuComp();
                if (!s.empty()) {
                    CandidateString newCandStr(s, _candidates.front().strokeLen(), 0, 0);
                    _candidates.insert(_candidates.begin(), newCandStr);
                    size_t nSameLen = getNumOfSameStrokeLen();
                    arrangePenalties(nSameLen);
                }
            }
        }
    };

    // Lattice
    class LatticeImpl : public Lattice2 {
        DECLARE_CLASS_LOGGER;
    private:
        // 打鍵開始位置
        int _startStrokeCount = 0;

        // K-best な文字列を格納する
        KBestList _kBestList;

        // 前回生成された文字列
        MString _prevOutputStr;

        // 漢字優先設定時刻
        time_t _kanjiPreferredSettingDt;

        // 前回生成された文字列との共通する先頭部分の長さ
        size_t calcCommonPrefixLenWithPrevStr(const MString& outStr) {
            _LOG_DEBUGH(_T("ENTER: outStr={}, prevStr={}"), to_wstr(outStr), to_wstr(_prevOutputStr));
            size_t n = 0;
            while (n < outStr.size() && n < _prevOutputStr.size()) {
                if (outStr[n] != _prevOutputStr[n]) break;
                ++n;
            }
            _LOG_DEBUGH(_T("LEAVE: commonLen={}"), n);
            return n;
        }

        Deque<String> _debugLogQueue;

        String formatStringOfWordPieces(const std::vector<WordPiece>& pieces) {
            return utils::join(utils::select<String>(pieces, [](WordPiece p){return p.debugString();}), _T("|"));
        }

        // すべての単語素片が1文字で、それが漢字・ひらがな・カタカナ以外か
        bool areAllPiecesNonJaChar(const std::vector<WordPiece>& pieces) {
            for (const auto iter : pieces) {
                MString s = pieces.front().getString();
                //_LOG_INFOH(_T("s: len={}, str={}"), s.size(), to_wstr(s));
                if (s.size() != 1 || utils::is_japanese_char_except_nakaguro(s.front())) {
                    //_LOG_INFOH(_T("FALSE"));
                    return false;
                }
            }
            return true;
        }

    public:
        // コンストラクタ
        LatticeImpl() : _kanjiPreferredSettingDt(0) {
            _LOG_DETAIL(_T("CALLED: Constructor"));
        }

        // デストラクタ
        ~LatticeImpl() override {
            _LOG_DETAIL(_T("CALLED: Destructor"));
            clear();
        }

        void clearAll() override {
            _LOG_DETAIL(_T("CALLED"));
            _startStrokeCount = 0;
            _prevOutputStr.clear();
            _kBestList.clear(true);
        }

        void clear() override {
            _LOG_DETAIL(_T("CALLED"));
            _startStrokeCount = 0;
            _prevOutputStr.clear();
            _kBestList.clear(utils::diffTime(_kanjiPreferredSettingDt) >= 3.0);     // 前回の設定時刻から３秒以上経過していたらクリアできる
        }

        void removeOtherThanKBest() override {
            _LOG_DETAIL(_T("CALLED"));
            _kBestList.removeOtherThanKBest();
        }

        void removeSecondOrLesser() override {
            _LOG_DETAIL(_T("CALLED"));
            _kBestList.removeSecondOrLesser();
        }

        bool isEmpty() override {
            //_LOG_DETAIL(_T("CALLED: isEmpty={}"), _kBestList.isEmpty());
            return _kBestList.isEmpty();
        }

        // 先頭候補を最優先候補にする
        void selectFirst() override {
            _LOG_DETAIL(_T("CALLED"));
            _kBestList.selectFirst();
        }

        // 次候補を最優先候補にする
        void selectNext() override {
            _LOG_DETAIL(_T("CALLED"));
            _kBestList.selectNext();
        }

        // 前候補を最優先候補にする
        void selectPrev() override {
            _LOG_DETAIL(_T("CALLED"));
            _kBestList.selectPrev();
        }

        void updateByBushuComp() override {
            _LOG_DETAIL(_T("CALLED"));
            _kBestList.updateByBushuComp();
        }

    public:
        // 単語素片リストの追加(単語素片が得られなかった場合も含め、各打鍵ごとに呼び出すこと)
        // 単語素片(WordPiece): 打鍵後に得られた出力文字列と、それにかかった打鍵数
        LatticeResult addPieces(const std::vector<WordPiece>& pieces, bool kanjiPreferredNext, bool strokeBack) override {
            int totalStrokeCount = (int)(STATE_COMMON->GetTotalDecKeyCount());
            if (_startStrokeCount == 0) _startStrokeCount = totalStrokeCount;
            int currentStrokeCount = totalStrokeCount - _startStrokeCount + 1;

            //_LOG_DEBUGH(_T("ENTER: currentStrokeCount={}, pieces: {}\nkBest:\n{}"), currentStrokeCount, formatStringOfWordPieces(pieces), _kBestList.debugString());
            _LOG_INFOH(_T("ENTER: _kBestList.size={}, totalStroke={}, currentStroke={}, kanjiPref={}, strokeBack={}, rollOver={}, pieces: {}"),
                _kBestList.size(), totalStrokeCount, currentStrokeCount, kanjiPreferredNext, strokeBack, STATE_COMMON->IsRollOverStroke(), formatStringOfWordPieces(pieces));
            // endPos における空の k-best path リストを取得

            if (pieces.size() == 1) {
                auto s = pieces.front().getString();
                if (s.size() == 1 && utils::is_punct(s[0])) {
                    // 前回の句読点から末尾までの出力文字列に対して Ngram解析を行う
                    _LOG_DETAIL(L"CALL lattice2::updateOnlineNgram()");
                    lattice2::updateOnlineNgram();
                }
            }

            if (kanjiPreferredNext) {
                _LOG_DETAIL(L"KANJI PREFERRED NEXT");
                // 現在の先頭候補を最優先に設定し、
                selectFirst();
                // 次のストロークを漢字優先とする
                _kBestList.setKanjiPreferredNextCands();
                if (_startStrokeCount == 1) _startStrokeCount = 0;  // 先頭での漢字優先なら、0 クリアしておく(この後、clear()が呼ばれるので、それと状態を合わせるため)
                _kanjiPreferredSettingDt = utils::getSecondsFromEpochTime();
            }

            _LOG_DETAIL(L"_kBestList.size={}", _kBestList.size());

            //// すべての単語素片が1文字で、それが漢字・ひらがな・カタカナ以外だったら、現在の先頭候補を優先させる
            //if (!pieces.empty() && areAllPiecesNonJaChar(pieces)) {
            //    _LOG_DETAIL(L"ALL PIECES NON JA CHAR");
            //    selectFirst();
            //}

            _LOG_DETAIL(L"_kBestList.size={}", _kBestList.size());

            // 候補リストの更新
            _kBestList.updateKBestList(pieces, currentStrokeCount, strokeBack);

            //_LOG_DEBUGH(_T(".\nresult kBest:\n{}"), pKBestList->debugString());
            size_t numBS = 0;
            MString outStr = _kBestList.getTopString();
            size_t commonLen = calcCommonPrefixLenWithPrevStr(outStr);
            numBS = _prevOutputStr.size() - commonLen;
            _prevOutputStr = outStr;
            outStr = utils::safe_substr(outStr, commonLen);
            _LOG_INFOH(_T("LEAVE: OUTPUT: {}, numBS={}\n\n{}"), to_wstr(outStr), numBS, _kBestList.debugKBestString());
            if (IS_LOG_DEBUGH_ENABLED) {
                while (_debugLogQueue.size() >= 10) _debugLogQueue.pop_front();
                _debugLogQueue.push_back(std::format(L"========================================\nENTER: currentStrokeCount={}, pieces: {}\n",
                    currentStrokeCount, formatStringOfWordPieces(pieces)));
                if (pieces.back().numBS() <= 0) {
                    _debugLogQueue.push_back(std::format(L"\n{}\nOUTPUT: {}, numBS={}\n\n", _kBestList.debugKBestString(10), to_wstr(outStr), numBS));
                }
            }
            return LatticeResult(outStr, numBS);
        }

        void saveCandidateLog() override {
            _LOG_INFOH(_T("ENTER"));
            String result;
            while (!_debugLogQueue.empty()) {
                result.append(_debugLogQueue.front());
                _debugLogQueue.pop_front();
            }
            _LOG_INFOH(L"result: {}", result);
            utils::OfstreamWriter writer(utils::joinPath(SETTINGS->rootDir, SETTINGS->mergerCandidateFile));
            if (writer.success()) {
                writer.writeLine(utils::utf8_encode(result));
                _LOG_INFOH(_T("result written"));
            }
            _LOG_INFOH(_T("LEAVE"));
        }
    };
    DEFINE_CLASS_LOGGER(LatticeImpl);

} // namespace lattice2


std::unique_ptr<Lattice2> Lattice2::Singleton;

void Lattice2::createLattice() {
    lattice2::loadCostFile();
    Singleton.reset(new lattice2::LatticeImpl());
}

void Lattice2::reloadCostFile() {
    lattice2::loadCostFile();
}

void Lattice2::reloadUserCostFile() {
    lattice2::loadCostFile(true);
}

void Lattice2::updateOnlineNgram() {
    lattice2::updateOnlineNgram();
}

//void Lattice2::updateOnlineNgram(const MString& str) {
//    lattice2::updateOnlineNgram(str);
//}

void Lattice2::saveOnlineCostFile() {
    lattice2::saveOnlineCostFile();
}
