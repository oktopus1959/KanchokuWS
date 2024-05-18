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
#include "Mecab/MecabBridge.h"
#include "BushuComp/BushuDic.h"

#if 1
#undef IS_LOG_DEBUGH_ENABLED
#define IS_LOG_DEBUGH_ENABLED true
#undef _LOG_INFOH
#undef LOG_INFO
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#if 0
#define _LOG_INFOH LOG_INFOH
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#else
#define _LOG_INFOH LOG_WARN
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#endif
#endif

namespace lattice2 {
    DEFINE_LOCAL_LOGGER(lattice);

    // ビームサイズ
    size_t BestKSize = 8;

    // 多ストロークの範囲
    int StrokeRange = 4;

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
                    wordCosts[to_mstr(items[0])] = -1000;       // userword.cost のデフォルトは -1000
                }
            }
        }
    }

    void loadCostFile() {
        wordCosts.clear();
        _loadCostFile(_T("wikipedia.cost.txt"));
        _loadCostFile(_T("userword.cost.txt"));
    }

    int getWordCost(const MString& str) {
        if (str.empty()) return 0;
        if (str.size() == 1 && str == MSTR_SPACE) return MAX_COST * 3;
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
        if (str.size() > 1) {
            // bigram
            for (size_t i = 0; i < str.size() - 1; ++i) {
                cost += getWordCost(utils::safe_substr(str, i, 2));
            }
            if (str.size() > 2) {
                // trigram
                for (size_t i = 0; i < str.size() - 2; ++i) {
                    auto iter = wordCosts.find(utils::safe_substr(str, i, 3));
                    if (iter != wordCosts.end()) {
                        int triCost = iter->second;
                        if (triCost > 0) triCost -= 1000;       // 正のコストが設定されている場合(wikipedia.costなど)は、 1000 を引いたコストにする; つまり負のコストになる
                        cost += triCost;
                    }
                }
                if (str.size() > 3) {
                    // quadgram
                    for (size_t i = 0; i < str.size() - 3; ++i) {
                        auto iter = wordCosts.find(utils::safe_substr(str, i, 4));
                        if (iter != wordCosts.end()) {
                            int triCost = iter->second;
                            if (triCost > 0) triCost -= 1000;       // 正のコストが設定されている場合(wikipedia.costなど)は、 1000 を引いたコストにする; つまり負のコストになる
                            cost += triCost;
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

        // 末尾文字列にマッチする RewriteInfo を取得する
        std::tuple<const RewriteInfo*, int> matchWithTailString(const PostRewriteOneShotNode* rewriteNode) const {
            size_t maxlen = SETTINGS->kanaTrainingMode && ROOT_STROKE_NODE->hasOnlyUsualRewriteNdoe() ? 0 : 8;     // かな入力練習モードで濁点のみなら書き換えをやらない
            bool bAllKeyUp = false; //OUTPUT_STACK->isAllKeyUp();
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

        CandidateString(const MString& s, int len, int cost, int penalty = 0) : _str(s), _strokeLen(len), _cost(cost), _penalty(penalty) {
        }

        std::tuple<MString, int> apply(const WordPiece& piece, int strokeCount, bool bAutoBushu) const {
            if (_strokeLen + piece.strokeLen()  == strokeCount) {
                if (bAutoBushu) {
                    if (SETTINGS->autoBushuCompMinCount > 0 && BUSHU_DIC) {
                        if (_str.size() > 0 && piece.getString().size() == 1) {
                            // 自動部首合成の実行
                            mchar_t m = BUSHU_DIC->FindAutoComposite(_str.back(), piece.getString().front());
                            //if (m == 0) m = BUSHU_DIC->FindComposite(_str.back(), piece.getString().front(), 0);
                            _LOG_DEBUGH(_T("BUSHU_DIC->FindComposite({}, {}) -> {}"),
                                to_wstr(utils::safe_tailstr(_str, 1)), to_wstr(utils::safe_substr(piece.getString(), 0, 1)), String(1, (wchar_t)m));
                            if (m != 0) {
                                MString s(_str);
                                s.back() = m;
                                return { s, 1 };
                            }
                        }
                    }
                } else {
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
                                return { utils::safe_substr(_str, 0, _str.size() - numBS), numBS };
                            } else {
                                return { EMPTY_MSTR, numBS };
                            }
                        } else {
                            return { _str + piece.getString(), 0 };
                        }
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

        int cost() const {
            return _cost + _penalty;
        }

        void cost(int cost) {
            _cost = cost;
        }

        void zeroCost() {
            _penalty = -_cost;
        }

        int penalty() const {
            return _penalty;
        }

        void penalty(int penalty) {
            _penalty = penalty;
        }

        String debugString() const {
            return to_wstr(_str)
                + _T(" (totalCost=") + std::to_wstring(cost())
                + _T("(_cost=") + std::to_wstring(_cost)
                + _T(",_penalty=") + std::to_wstring(_penalty)
                + _T("), strokeLen = ") + std::to_wstring(_strokeLen) + _T(")");
        }
    };

    // K-best な文字列を格納する
    class KBestList {

        std::map<MString, int> _mecabCache;

        std::vector<CandidateString> _candidates;

        static bool _isEmpty(const std::vector<CandidateString> cands) {
            return cands.empty() || cands.size() == 1 && cands.front().string().empty();
        }

    public:
        void clear() {
            _mecabCache.clear();
            _candidates.clear();
        }

        void removeOtherThanKBest() {
            if (_candidates.size() > BestKSize) {
                _candidates.erase(_candidates.begin() + BestKSize, _candidates.end());
            }
        }

        void removeSecondOrLesser() {
            if (_candidates.size() > 0) {
                _candidates.erase(_candidates.begin() + 1, _candidates.end());
                _candidates.front().penalty(0);
            }
        }

        bool isEmpty() const {
            return _isEmpty(_candidates);
        }

        MString getTopString() {
            return _candidates.empty() ? MString() : _candidates[0].string();
        }

        String debugString() const {
            String result;
            for (size_t i = 0; i < _candidates.size(); ++i) {
                if (i > 0) result.append(_T("\n"));
                result.append(std::to_wstring(i));
                result.append(_T(": "));
                result.append(_candidates[i].debugString());
            }
            return result;
        }

    private:
        int calcMecabCost(const MString& s, std::vector<MString>& words) {
            int cost = 0;
            if (!s.empty()) {
                auto iter = _mecabCache.find(s);
                if (iter == _mecabCache.end()) {
                    cost = MecabBridge::mecabCalcCost(s, words);
                    _mecabCache[s] = cost;
                } else {
                    cost = iter->second;
                }
            }
            return cost;
        }
#if 0
        int totalCostWithMecab(const MString& candStr) {
            std::vector<MString> words;
            int mecabCost = calcMecabCost(candStr, words);
            return mecabCost;
        }
#endif
        // 新しい候補を追加
        bool addCandidate(std::vector<CandidateString>& newCandidates, CandidateString& newCandStr) {
            bool bAdded = false;
            bool bIgnored = false;
            std::vector<MString> words;
            const MString& candStr = newCandStr.string();
            int mecabCost = candStr.empty() ? 0 : calcMecabCost(candStr, words);
            int ngramCost = candStr.empty() ? 0 : getNgramCost(candStr) * 5;
            //int mecabCost = 0;
            //int ngramCost = candStr.empty() ? 0 : getNgramCost(candStr);
            int candCost = mecabCost + ngramCost;
            _LOG_INFOH(_T("CALLED: candStr={}, candCost={} (mecab={}[{}], ngram={})"), to_wstr(candStr), candCost, mecabCost, to_wstr(utils::join(words, ' ')), ngramCost);

            newCandStr.cost(candCost);
            int totalCost = newCandStr.cost();

            if (!newCandidates.empty()) {
                for (auto iter = newCandidates.begin(); iter != newCandidates.end(); ++iter) {
                    int otherCost = iter->cost();
                    _LOG_DEBUGH(_T("    otherStr={}, otherCost={}"), to_wstr(iter->string()), otherCost);
                    if (totalCost < otherCost) {
                        iter = newCandidates.insert(iter, newCandStr);    // iter は挿入したノードを指す
                        bAdded = true;
                        // 下位のノードで同じ文字列のものを探し、あればそれを削除
                        for (++iter; iter != newCandidates.end(); ++iter) {
                            if (candStr == iter->string()) {
                                // 同じ文字列なので、古い候補は削除
                                newCandidates.erase(iter);
                                _LOG_DEBUGH(_T("    REMOVE second best or lesser candidate"));
                                break;
                            }
                        }
                        break;
                    } else if (candStr == iter->string()) {
                        // 同じ文字列なので、当候補は無視
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
        void addOnePiece(std::vector<CandidateString>& newCandidates, const WordPiece& piece, int strokeCount) {
            _LOG_DEBUGH(_T("CALLED: piece={}"), piece.debugString());
            for (const auto& cand : _candidates) {
                MString s;
                int numBS;
                std::tie(s, numBS) = cand.apply(piece, strokeCount, true);  // 自動部首合成
                if (!s.empty()) {
                    CandidateString newCandStr(s, strokeCount, 0, cand.penalty());
                    addCandidate(newCandidates, newCandStr);
                }
                std::tie(s, numBS) = cand.apply(piece, strokeCount, false);
                if (!s.empty() || numBS > 0) {
                    CandidateString newCandStr(s, strokeCount, 0, cand.penalty());
                    addCandidate(newCandidates, newCandStr);
                }
            }
        }

    public:
        // strokeCount: lattice に最初に addPieces() した時からの相対的なストローク数
        void updateKBestList(const std::vector<WordPiece>& pieces, int strokeCount) {
            _LOG_DEBUGH(_T("ENTER: strokeCount={}"), strokeCount);
            // 追加される piece と組み合わせるための、先頭を表すダミーを用意しておく
            if (_candidates.empty()) {
                _candidates.push_back(CandidateString());
            }

            std::vector<CandidateString> newCandidates;
            for (const auto& piece : pieces) {
                addOnePiece(newCandidates, piece, strokeCount);
            }

            // 組み合せ不可だったものは、strokeCount が範囲内なら残しておく
            if (!_isEmpty(newCandidates)) {     // isEmpty()だったら、BSなどで先頭のものだけが残されたということ
                for (const auto& cand : _candidates) {
                    if (cand.strokeLen() + StrokeRange > strokeCount) {
                        newCandidates.push_back(cand);
                    }
                }
            }

            _candidates = std::move(newCandidates);
            _LOG_DEBUGH(_T("LEAVE"));
        }

    private:
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

        void arrangePenalties(size_t nSameLen) {
            _candidates.front().zeroCost();
            for (size_t i = 1; i < nSameLen; ++i) {
                _candidates[i].penalty(1000000 * (int)i);
            }
        }

    public:
        void selectFirst() {
            size_t nSameLen = getNumOfSameStrokeLen();
            if (nSameLen > 1) {
                arrangePenalties(nSameLen);
            }
        }

        void selectNext() {
            size_t nSameLen = getNumOfSameStrokeLen();
            if (nSameLen > 1) {
                auto begin = _candidates.begin();
                std::rotate(begin, begin + 1, begin + nSameLen);
                arrangePenalties(nSameLen);
            }
        }

        void selectPrev() {
            size_t nSameLen = getNumOfSameStrokeLen();
            if (nSameLen > 1) {
                auto begin = _candidates.begin();
                std::rotate(begin, begin + nSameLen - 1, begin + nSameLen);
                arrangePenalties(nSameLen);
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

        size_t calcCommonLenWithPrevStr(const MString& outStr) {
            _LOG_DEBUGH(_T("ENTER: outStr={}, prevStr={}"), to_wstr(outStr), to_wstr(_prevOutputStr));
            size_t n = 0;
            while (n < outStr.size() && n < _prevOutputStr.size()) {
                if (outStr[n] != _prevOutputStr[n]) break;
                ++n;
            }
            _LOG_DEBUGH(_T("LEAVE: commonLen={}"), n);
            return n;
        }

#if IS_LOG_DEBUGH_ENABLED
        String formatStringOfWordPieces(const std::vector<WordPiece>& pieces) {
            return utils::join(utils::select<String>(pieces, [](WordPiece p){return p.debugString();}), _T("|"));
        }
#endif

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

        void selectFirst() override {
            _kBestList.selectFirst();
        }

        void selectNext() override {
            _kBestList.selectNext();
        }

        void selectPrev() override {
            _kBestList.selectPrev();
        }

        // 単語素片リストの追加(単語素片が得られなかった場合も含め、各打鍵ごとに呼び出すこと)
        // 単語素片(WordPiece): 打鍵後に得られた出力文字列と、それにかかった打鍵数
        LatticeResult addPieces(const std::vector<WordPiece>& pieces) override {
            int strokeCount = STATE_COMMON->GetTotalDecKeyCount();
            if (_startStrokeCount == 0) _startStrokeCount = strokeCount;

            //_LOG_DEBUGH(_T("ENTER: strokeCount={}, pieces: {}\nkBest:\n{}"), strokeCount, formatStringOfWordPieces(pieces), _kBestList.debugString());
            _LOG_INFOH(_T("ENTER: strokeCount={}, pieces: {}"), strokeCount, formatStringOfWordPieces(pieces));
            // endPos における空の k-best path リストを取得

            _kBestList.updateKBestList(pieces, strokeCount - _startStrokeCount + 1);

            //_LOG_DEBUGH(_T(".\nresult kBest:\n{}"), pKBestList->debugString());
            size_t numBS = 0;
            MString outStr = _kBestList.getTopString();
            size_t commonLen = calcCommonLenWithPrevStr(outStr);
            numBS = _prevOutputStr.size() - commonLen;
            _prevOutputStr = outStr;
            outStr = utils::safe_substr(outStr, commonLen);
            _LOG_INFOH(_T("LEAVE: OUTPUT: {}, numBS={}\nkBest:\n{}"), to_wstr(outStr), numBS, _kBestList.debugString());
            return LatticeResult(outStr, numBS);
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

