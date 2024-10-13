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

#if 1
#undef IS_LOG_DEBUGH_ENABLED
#define IS_LOG_DEBUGH_ENABLED true
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
    int AllowedStrokeRange = 3;

    // 末尾がここで設定した長さ以上に同じ候補は、先頭だけを残して削除
    int LastSameLen = 5;

    // 非優先候補に与えるペナルティ
    int NON_PREFERRED_PENALTY = 1000000;

    // 漢字orカタカナが連続する場合のボーナス
    int KANJI_CONSECUTIVE_BONUS = 1000;

    // 末尾がひらがなの連続場合のボーナス
    int TAIL_HIRAGANA_BONUS = 0; //1000;

    // 「漢字+の+漢字」の場合のボーナス
    int KANJI_NO_KANJI_BONUS = 1000;

    // cost ファイルに登録がある場合のデフォルトのボーナス
    int DEFAULT_WORD_BONUS = 1000;

    // 2文字以上の形態素で漢字を含む場合のボーナス
    int MORPH_ANY_KANJI_BONUS = 5000;

    // 3文字以上の形態素ですべてひらがなの場合のボーナス
    int MORPH_ALL_HIRAGANA_BONUS = 1000;

    // 2文字以上の形態素ですべてカタカナの場合のボーナス
    int MORPH_ALL_KATAKANA_BONUS = 3000;

    int MAX_COST = 1000;

    int STROKE_COST = 150;

    std::map<MString, int> wordCosts;

    void _loadCostFile(StringRef costFile) {
        auto path = utils::joinPath(SETTINGS->rootDir, costFile);
        _LOG_DEBUGH(_T("LOAD: {}"), path.c_str());
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            for (const auto& line : reader.getAllLines()) {
                auto items = utils::split(utils::replace_all(utils::strip(line), L" +", L"\t"), '\t');
                if (items.size() == 2) {
                    wordCosts[to_mstr(items[0])] = std::stoi(items[1]);
                } else if (items.size() == 1) {
                    wordCosts[to_mstr(items[0])] = -DEFAULT_WORD_BONUS;       // userword.cost のデフォルトは -DEFAULT_WORD_BONUS
                }
            }
        }
    }

    void loadCostFile(bool onlyUserFile = false) {
        if (!onlyUserFile) {
            wordCosts.clear();
#ifdef NDEBUG
            _loadCostFile(_T("wikipedia.cost.txt"));
#endif
        }
        _loadCostFile(_T("userword.cost.txt"));
    }

    int getWordCost(const MString& str) {
        if (str.empty()) return 0;
        if (str.size() == 1 && str == MSTR_SPACE) return MAX_COST * 3;          // 1 space の場合のコスト
        auto iter = wordCosts.find(str);
        return iter != wordCosts.end() ? iter->second : MAX_COST;
    }

    int getWordConnCost(const MString& s1, const MString& s2) {
        return getWordCost(utils::last_substr(s1, 1) + utils::safe_substr(s2, 0, 1)) / 2;
    }

#if 1
    int getNgramCost(const MString& str) {
        int cost = 0;
        // unigram
        for (size_t i = 0; i < str.size(); ++i) {
            if (utils::is_katakana(str[i])) {
                if ((i == 0 || !utils::is_katakana(str[i-1])) && (i + 1 == str.size() || !utils::is_katakana(str[i+1]))) {
                    // 孤立したカタカナは高いコストを設定
                    cost += 3000;
                    continue;
                }
            }
            cost += getWordCost(utils::safe_substr(str, i, 1));
        }
        if (str.size() == 1) {
            if (utils::is_kanji(str[0])) cost += MAX_COST;    // 漢字１文字の場合のコスト
        } else if (str.size() > 1) {
            // bigram
            int lastKanjiPos = -1;
            for (int i = 0; i < (int)str.size() - 1; ++i) {
                cost += getWordCost(utils::safe_substr(str, i, 2));
                if (i > lastKanjiPos && utils::is_kanji(str[i]) && utils::is_kanji(str[i + 1]) || utils::is_katakana(str[i]) && utils::is_katakana(str[i + 1])) {
                    // 漢字orカタカナが2文字連続する場合のボーナス
                    cost -= KANJI_CONSECUTIVE_BONUS;
                    lastKanjiPos = i + 1;   // 3文字以上続くときに、重複は計上しない
                }
            }
            if (str.size() > 2) {
                // trigram
                for (size_t i = 0; i < str.size() - 2; ++i) {
                    auto iter = wordCosts.find(utils::safe_substr(str, i, 3));
                    if (iter != wordCosts.end()) {
                        int triCost = iter->second;
                        if (triCost > 0) triCost -= DEFAULT_WORD_BONUS;       // 正のコストが設定されている場合(wikipedia.costなど)は、 DEFAULT BONUS を引いたコストにする; つまり負のコストになる
                        cost += triCost;
                    }
                    // 「漢字+の+漢字」のような場合はボーナス
                    if ((str[i+1] == L'が' || str[i+1] == L'の' || str[i+1] == L'を') && !utils::is_hiragana(str[i]) && !utils::is_hiragana(str[i+2])) {
                        cost -= KANJI_NO_KANJI_BONUS;
                    }
                }
                if (str.size() > 3) {
                    // quadgram
                    for (size_t i = 0; i < str.size() - 3; ++i) {
                        auto iter = wordCosts.find(utils::safe_substr(str, i, 4));
                        if (iter != wordCosts.end()) {
                            int quadCost = iter->second;
                            if (quadCost > 0) quadCost -= DEFAULT_WORD_BONUS;       // 正のコストが設定されている場合(wikipedia.costなど)は、 DEFAULT BONUS を引いたコストにする; つまり負のコストになる
                            cost += quadCost;
                        }
                        if ((i == str.size() - 4) && utils::is_hiragana_str(utils::safe_substr(str, i, 4))) {
                            // 末尾がひらがな4文字連続の場合のボーナス
                            cost -= TAIL_HIRAGANA_BONUS;
                        }
                    }
                }
            }
        }
        return cost;
    }
#else
    int getNgramCost(const MString& str) {
        int cost = 0;
        for (size_t i = 0; i < str.size() - 1; ++i) {
            int bigramCost = getWordCost(utils::safe_substr(str, i, 2));
            if (bigramCost >= MAX_COST) {
                bigramCost = getWordCost(utils::safe_substr(str, i, 1)) + getWordCost(utils::safe_substr(str, i + 1, 1));
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
        float _llama_loss = 0.0f;

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

        float llama_loss() const {
            return _llama_loss;
        }

        void llama_loss(float loss) {
            _llama_loss = loss;
        }

        String debugString() const {
            return to_wstr(_str)
                + _T(" (totalCost=") + std::to_wstring(totalCost())
                + _T("(_cost=") + std::to_wstring(_cost)
                + _T(",_penalty=") + std::to_wstring(_penalty)
                + _T(",_llama_loss=") + std::to_wstring(_llama_loss)
                + _T("), strokeLen = ") + std::to_wstring(_strokeLen) + _T(")");
        }
    };

    // K-best な文字列を格納する
    class KBestList {

        std::vector<CandidateString> _candidates;

        // 次のストロークをスキップする候補文字列
        std::set<MString> _skipStrokeCands;

        static bool _isEmpty(const std::vector<CandidateString> cands) {
            //return cands.empty() || cands.size() == 1 && cands.front().string().empty();
            return cands.empty() || cands.size() > 0 && cands.front().string().empty();
        }

        String _debugLog;

    public:
        void clear() {
            _candidates.clear();
            _skipStrokeCands.clear();
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

        MString getTopString() {
            return _candidates.empty() ? MString() : _candidates[0].string();
        }

        String debugKBestString(size_t maxLn = 100000) const {
            String result = L"skipNextStrokeCands=[" + to_wstr(skipNextStrokeCands()) + L"]\n\n";
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
                for (const auto& w : words) {
                    if (w.size() >= 2 && std::any_of(w.begin(), w.end(), [](mchar_t c) { return utils::is_kanji(c); })) {
                        cost -= MORPH_ANY_KANJI_BONUS * (int)(w.size() - 1);
                    }
                    if (w.size() >= 3 && std::all_of(w.begin(), w.end(), [](mchar_t c) { return utils::is_hiragana(c); })) {
                        cost -= MORPH_ALL_HIRAGANA_BONUS;
                    }
                    if (w.size() >= 2 && std::all_of(w.begin(), w.end(), [](mchar_t c) { return utils::is_katakana(c); })) {
                        cost -= MORPH_ALL_KATAKANA_BONUS;
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
            bool bAdded = false;
            bool bIgnored = false;
            std::vector<MString> words;
            const MString& candStr = newCandStr.string();
            int morphCost = candStr.empty() ? 0 : calcMorphCost(candStr, words);
            int ngramCost = candStr.empty() ? 0 : getNgramCost(candStr) * 5;
            //int morphCost = 0;
            //int ngramCost = candStr.empty() ? 0 : getNgramCost(candStr);
            //int llamaCost = candStr.empty() ? 0 : calcLlamaCost(candStr) * 5;
            int llamaCost = 0;
            int candCost = morphCost + ngramCost + llamaCost;
            _LOG_INFOH(_T("CALLED: candStr={}, candCost={} (morph={}[{}], ngram={}, llama={})"),
                to_wstr(candStr), candCost, morphCost, to_wstr(utils::join(words, ' ')), ngramCost, llamaCost);
#if IS_LOG_DEBUGH_ENABLED
            if (!isStrokeBS) _debugLog.append(std::format(L"candStr = {}, candCost = {} (morph = {} [{}] , ngram = {}, llama={})\n",
                to_wstr(candStr), candCost, morphCost, to_wstr(utils::join(words, ' ')), ngramCost, llamaCost));
#endif
            newCandStr.cost(candCost);

            //// 「漢字+の+漢字」のような場合はペナルティを解除
            //size_t len = candStr.size();
            //if (len >= 3 && candStr[len - 2] == L'の' && !utils::is_hiragana(candStr[len - 3]) && !utils::is_hiragana(candStr[len - 1])) {
            //    newCandStr.zeroPenalty();
            //}

            int totalCost = newCandStr.totalCost();

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
                _LOG_DEBUGH(_T("    REMOVE OVERFLOW ENTRY"));
            }
            if (bAdded) {
                _LOG_DEBUGH(_T("    ADD candidate: {}"), to_wstr(candStr));
            } else {
                _LOG_DEBUGH(_T("    ABANDON candidate: {}, totalCost={}"), to_wstr(candStr), totalCost);
            }
            return bAdded;
        }

    private:
        // 素片のストロークと適合する候補だけを追加
        void addOnePiece(std::vector<CandidateString>& newCandidates, const WordPiece& piece, int strokeCount) {
            _LOG_DEBUGH(_T("CALLED: piece={}"), piece.debugString());
            bool bAutoBushuFound = false;           // 自動部首合成は一回だけ実行する
            bool isStrokeBS = piece.numBS() > 0;
            int topStrokeLen = -1;
            for (const auto& cand : _candidates) {
                if (topStrokeLen < 0) topStrokeLen = cand.strokeLen();
                if (cand.strokeLen() + piece.strokeLen() == strokeCount) {
                    // 素片のストロークと適合する候補
                    int penalty = cand.penalty();
                    if (!isStrokeBS && cand.strokeLen() == topStrokeLen && !piece.getString().empty() && piece.strokeLen() == 1 && _skipStrokeCands.contains(cand.string())) {
                        penalty += NON_PREFERRED_PENALTY;
                    }
                    // 次の素片長が1でないか、ストロークのスキップ対象でもない
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
        }

        // 末尾から、指定の長さより以前の部分を確定させる
        void commitLeaderBeforeTailLen() {
            if (!_candidates.empty()) {
                MString firstStr = _candidates.front().string();
                if (firstStr.size() > SETTINGS->commitBeforeTailLen) {
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

        // llama-loss の小さい順に候補を並べ直す
        void sortByLlamaLoss(std::vector<CandidateString>& newCandidates) {
            std::for_each(newCandidates.begin(), newCandidates.end(), [this](CandidateString& c) {
                c.llama_loss(calcLlamaLoss(c.string()));
            });
            std::sort(newCandidates.begin(), newCandidates.end(), [](const CandidateString& a, const CandidateString& b) {
                return a.llama_loss() < b.llama_loss();
            });
        }

        bool isKanjiKatakanaConsecutive(const CandidateString& cand) {
            MString str = cand.string();
            size_t len = str.size();
            return len >= 2 && (utils::is_kanji(str[len - 1]) && utils::is_kanji(str[len - 2]) || utils::is_katakana(str[len - 1]) && utils::is_katakana(str[len - 2]));
        }

    public:
        // strokeCount: lattice に最初に addPieces() した時からの相対的なストローク数
        void updateKBestList(const std::vector<WordPiece>& pieces, int strokeCount) {
            _LOG_DEBUGH(_T("ENTER: strokeCount={}"), strokeCount);
            _debugLog.clear();

            // 追加される piece と組み合わせるための、先頭を表すダミーを用意しておく
            if (_candidates.empty()) {
                _candidates.push_back(CandidateString());
            }

            std::vector<CandidateString> newCandidates;
            for (const auto& piece : pieces) {
                // 素片のストロークと適合する候補だけを追加
                addOnePiece(newCandidates, piece, strokeCount);
            }

            //sortByLlamaLoss(newCandidates);

            // stroke位置的に組み合せ不可だったものは、strokeCount が範囲内なら残しておく
            if (!_isEmpty(newCandidates)) {     // isEmpty()だったら、BSなどで先頭のものだけが残されたということ
                for (const auto& cand : _candidates) {
                    if (cand.strokeLen() + AllowedStrokeRange > strokeCount) {
                        newCandidates.push_back(cand);
                    }
                }
            }
            _candidates = std::move(newCandidates);

            //// 漢字またはカタカナが2文字以上連続したら、その候補を優先する
            //if (!_candidates.empty()) {
            //    if (isKanjiKatakanaConsecutive(_candidates.front())) selectFirst();
            //}

            // 末尾から、指定の長さより以前の部分を確定させる
            commitLeaderBeforeTailLen();

            _LOG_DEBUGH(_T("LEAVE"));
        }

    private:
        // ストローク長の同じ候補の数を返す
        size_t getNumOfSameStrokeLen() const {
            size_t nSameLen = 0;
            if (_candidates.size() > 1) {
                int strokeLen = _candidates.front().strokeLen();
                ++nSameLen;
                for (auto iter = _candidates.begin() + 1; iter != _candidates.end() && iter->strokeLen() == strokeLen; ++iter) {
                    ++nSameLen;
                }
            }
            return nSameLen;
        }

        // 先頭候補以外に、非優先候補ペナルティを与える (先頭候補のペナルティは 0 にする)
        void arrangePenalties(size_t nSameLen) {
            _candidates.front().zeroPenalty();
            for (size_t i = 1; i < nSameLen; ++i) {
                _candidates[i].penalty(NON_PREFERRED_PENALTY * (int)i);
            }
        }

    public:
        void setSkipNextStrokeCands() {
            _skipStrokeCands.clear();
            if (!_candidates.empty()) {
                int topStrokeCnt = _candidates.front().strokeLen();
                for (const auto& c : _candidates) {
                    if (c.strokeLen() != topStrokeCnt) break;
                    _skipStrokeCands.insert(c.string());
                }
            }
        }

        MString skipNextStrokeCands() const {
            return utils::join(_skipStrokeCands, L',');
        }

        // 先頭候補を最優先候補にする
        void selectFirst() {
            size_t nSameLen = getNumOfSameStrokeLen();
            if (nSameLen > 1) {
                arrangePenalties(nSameLen);
                _LOG_INFOH(_T("CALLED: First candidate preferred."));
            }
        }

        // 次候補を最優先候補にする
        void selectNext() {
            size_t nSameLen = getNumOfSameStrokeLen();
            if (nSameLen > 1) {
                auto begin = _candidates.begin();
                std::rotate(begin, begin + 1, begin + nSameLen);
                arrangePenalties(nSameLen);
            }
        }

        // 前候補を最優先候補にする
        void selectPrev() {
            size_t nSameLen = getNumOfSameStrokeLen();
            if (nSameLen > 1) {
                auto begin = _candidates.begin();
                std::rotate(begin, begin + nSameLen - 1, begin + nSameLen);
                arrangePenalties(nSameLen);
            }
        }

        // 部首合成
        void updateByBushuComp() {
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
        LatticeImpl() {
            _LOG_DEBUGH(_T("CALLED: Constructor"));
        }

        // デストラクタ
        ~LatticeImpl() override {
            _LOG_DEBUGH(_T("CALLED: Destructor"));
            clear();
        }

        void clear() override {
            _LOG_DEBUGH(_T("CALLED"));
            _startStrokeCount = 0;
            _prevOutputStr.clear();
            _kBestList.clear();
        }

        void removeOtherThanKBest() override {
            _LOG_DEBUGH(_T("CALLED"));
            _kBestList.removeOtherThanKBest();
        }

        void removeSecondOrLesser() override {
            _LOG_DEBUGH(_T("CALLED"));
            _kBestList.removeSecondOrLesser();
        }

        bool isEmpty() override {
            _LOG_DEBUGH(_T("CALLED: isEmpty={}"), _kBestList.isEmpty());
            return _kBestList.isEmpty();
        }

        // 先頭候補を最優先候補にする
        void selectFirst() override {
            _kBestList.selectFirst();
        }

        // 次候補を最優先候補にする
        void selectNext() override {
            _kBestList.selectNext();
        }

        // 前候補を最優先候補にする
        void selectPrev() override {
            _kBestList.selectPrev();
        }

        void updateByBushuComp() override {
            _kBestList.updateByBushuComp();
        }

    public:
        // 単語素片リストの追加(単語素片が得られなかった場合も含め、各打鍵ごとに呼び出すこと)
        // 単語素片(WordPiece): 打鍵後に得られた出力文字列と、それにかかった打鍵数
        LatticeResult addPieces(const std::vector<WordPiece>& pieces, bool skipNextStroke) override {
            int totalStrokeCount = (int)(STATE_COMMON->GetTotalDecKeyCount());
            if (_startStrokeCount == 0) _startStrokeCount = totalStrokeCount;
            int currentStrokeCount = totalStrokeCount - _startStrokeCount + 1;

            //_LOG_DEBUGH(_T("ENTER: currentStrokeCount={}, pieces: {}\nkBest:\n{}"), currentStrokeCount, formatStringOfWordPieces(pieces), _kBestList.debugString());
            _LOG_INFOH(_T("ENTER: currentStrokeCount={}, skip={}, pieces: {}"), currentStrokeCount, skipNextStroke, formatStringOfWordPieces(pieces));
            // endPos における空の k-best path リストを取得

            if (skipNextStroke) {
                // 次のストロークをスキップする
                _kBestList.setSkipNextStrokeCands();
            }

            // すべての単語素片が1文字で、それが漢字・ひらがな・カタカナ以外だったら、現在の先頭候補を優先させる
            if (!pieces.empty() && areAllPiecesNonJaChar(pieces)) {
                selectFirst();
            }

            // 候補リストの更新
            _kBestList.updateKBestList(pieces, currentStrokeCount);

            //_LOG_DEBUGH(_T(".\nresult kBest:\n{}"), pKBestList->debugString());
            size_t numBS = 0;
            MString outStr = _kBestList.getTopString();
            size_t commonLen = calcCommonPrefixLenWithPrevStr(outStr);
            numBS = _prevOutputStr.size() - commonLen;
            _prevOutputStr = outStr;
            outStr = utils::safe_substr(outStr, commonLen);
            _LOG_INFOH(_T("LEAVE: OUTPUT: {}, numBS={}\n\n{}"), to_wstr(outStr), numBS, _kBestList.debugKBestString());
#if IS_LOG_DEBUGH_ENABLED
            _debugLogQueue.push_back(std::format(L"========================================\nENTER: currentStrokeCount={}, pieces: {}\n",
                currentStrokeCount, formatStringOfWordPieces(pieces)));
            if (pieces.back().numBS() <= 0) {
                if (_debugLogQueue.size() >= 10) _debugLogQueue.pop_front();
                _debugLogQueue.push_back(std::format(L"\n{}\nOUTPUT: {}, numBS={}\n\n", _kBestList.debugKBestString(10), to_wstr(outStr), numBS));
            }
#endif
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

