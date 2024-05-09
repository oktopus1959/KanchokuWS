#include "Logger.h"
#include "string_utils.h"
#include "misc_utils.h"
#include "path_utils.h"
#include "file_utils.h"
#include "transform_utils.h"
#include "Constants.h"

#include "settings.h"
#include "Lattice.h"
#include "Mecab/MecabBridge.h"

#if 1
#undef IS_LOG_DEBUGH_ENABLED
#undef LOG_INFO
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#define IS_LOG_DEBUGH_ENABLED true
#if 0
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#else
#define LOG_INFO LOG_WARN
#define LOG_DEBUG LOG_WARN
#define _LOG_DEBUGH LOG_WARN
#endif
#endif

namespace lattice {
    DEFINE_LOCAL_LOGGER(lattice);

    // ビームサイズ
    size_t BestKSize = 5;

    int MAX_COST = 1000;

    int STROKE_COST = 150;

    std::map<MString, int> wordCosts;

    void loadCostFile() {
        auto path = utils::joinPath(SETTINGS->rootDir, _T("wikipedia.cost.txt"));
        _LOG_DEBUGH(_T("LOAD: {}"), path.c_str());
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            for (const auto& line : reader.getAllLines()) {
                auto items = utils::split(line, '\t');
                if (items.size() == 2) {
                    wordCosts[to_mstr(items[0])] = std::stoi(items[1]);
                }
            }
        }
    }

    int getWordCost(const MString& str) {
        if (str.empty()) return 0;
        auto iter = wordCosts.find(str);
        return iter != wordCosts.end() ? iter->second : MAX_COST;
    }

    int getWordConnCost(const MString& s1, const MString& s2) {
        return getWordCost(utils::last_substr(s1, 1) + utils::safe_substr(s2, 0, 1)) / 2;
    }

    // Lattice Node (特定打鍵位置における文字ノード)
    // 打鍵数を保持しており、その打鍵数だけ前の位置のノードに接続する。
    class _LatticeNode {
        MString _str;

        // 打鍵数
        size_t _strokeLen = 0;

        // 自ノードのコスト
        int _cost = MAX_COST;

        // 当ノードに至るパスのうちの最小コスト
        //int minCost;

    public:
        // 打鍵数
        inline size_t strokeLen() const { return _strokeLen; }

        _LatticeNode() { }

        _LatticeNode(const MString& s, size_t len, int cost, int strokeCost = 0)
            : _str(s), _strokeLen(len)
        {
            _cost = (cost >= 0 ? cost : getWordCost(s)) + strokeCost;
            //minCost = INT_MAX;
        }

        _LatticeNode(const WordPiece& piece)
            : _LatticeNode(piece.pieceStr, piece.strokeLen, -1, piece.strokeLen > 1 ? STROKE_COST * (piece.strokeLen - 1) : 0)
        {
        }

        const MString& str() const {
            return _str;
        }

        int cost() const {
            return _cost;
        }

        String toString() const {
            return std::format(_T("({},{})"), to_wstr(_str), _strokeLen);
        }
    };

    // 各打鍵位置を終了位置とする _LatticeNode の集合と生存管理
    class LatticeNodeListByEndPos {
        size_t pos;

    public:
        std::vector<_LatticeNode*> latticeNodes;

        LatticeNodeListByEndPos(size_t pos) : pos(pos) {
        }

        ~LatticeNodeListByEndPos() {
            for (auto p : latticeNodes) {
                delete p;
            }
        }

        size_t getPos() const { return pos; }

        void addLatticeNode(_LatticeNode* p) {
            latticeNodes.push_back(p);
        }
    };

    // 逆順パスノード(逆順パスと同一視される)
    // 前ノードへのポインタを持ち、それのつながりで逆順パスを表現する
    class ReversePathNode {
    private:
        _LatticeNode* pNode;

        // 前ノード(これのつながりで逆順パスを表現する)
        ReversePathNode* _prevNode;

        MString _pathString;

        // パス全体のコスト
        int _totalCost;

        void calcCost() {
            if (_prevNode) {
                connCost = pNode->str().empty() ? 0 : getWordConnCost(_prevNode->pNode->str(), pNode->str());
                _totalCost = _prevNode->totalCost() + connCost + pNode->cost();
            } else {
                connCost = 0;
                _totalCost = pNode->cost();
            }
        }

        MString reverseListString() {
            if (pNode->str().empty()) return _prevNode ? _prevNode->pathString() : EMPTY_MSTR;

            //return (_prevNode) ? _prevNode->pathString() + MSTR_SPACE + pNode->str() : pNode->str();
            return (_prevNode) ? _prevNode->pathString() + pNode->str() : pNode->str();
        }

        static int calcMecabCost(std::map<MString, int>& mecabCache, const MString& s, std::vector<MString> words) {
            int cost = 0;
            if (!s.empty()) {
                auto iter = mecabCache.find(s);
                if (iter == mecabCache.end()) {
                    cost = MecabBridge::mecabCalcCost(s, words);
                    mecabCache[s] = cost;
                } else {
                    cost = iter->second;
                }
            }
            return cost;
        }

        static int calcMecabCost(const MString& s, std::vector<MString> words) {
            int cost = 0;
            if (!s.empty()) {
                cost = MecabBridge::mecabCalcCost(s, words);
            }
            return cost;
        }

    public:
        ReversePathNode(_LatticeNode* pNode, ReversePathNode* prevNode) : pNode(pNode), _prevNode(prevNode) {
            _pathString = reverseListString();
            //_totalCost = MecabBridge::mecabCalcCost(_pathString);
            calcCost();
        }

        // 前接ノードとの接続コスト
        int connCost;

        int totalCost() const {
            return _totalCost;
        }

        int totalCostWithMecab(std::map<MString, int>& mecabCache) const {
            //return (int)(_totalCost * (pathString().length() <= 10 ? 4.0 : 40.0 / pathString().length()) + calcMecabCost(mecabCache, pathString()));
            std::vector<MString> words;
            int mecabCost = calcMecabCost(mecabCache, pathString(), words);
            //return _totalCost * 4 + mecabCost;
            return mecabCost;
        }

        int calcCostWithMecab() const {
            std::vector<MString> words;
            int mecabCost = calcMecabCost(pathString(), words);
            return _totalCost * 4 + mecabCost;
        }

        const MString& pathString() const {
            return _pathString;
        }

        const MString normalString() const {
            //return utils::replace_all(_pathString, MSTR_SPACE, EMPTY_MSTR);
            return _pathString;
        }

        ReversePathNode* prevNode() const {
            return _prevNode;
        }

        MString toString() const {
            return pNode->str();
        }

        String formatString() const {
            return std::format(_T("{}(len={},tc={})"), to_wstr(pNode->str()), pNode->strokeLen(), totalCost());
        }

        String formatStringOfReverseList() const {
            std::vector<String> result;
            auto* p = this;
            while (p) {
                result.insert(result.begin(), p->formatString());
                p = p->_prevNode;
            }
            return utils::join(result, _T(" | "));
        }

    };

    String formatStringOfReverseNodeList(const ReversePathNode* p) {
        return p ? p->formatStringOfReverseList() : _T("No reverse path");
    }

    // k-best なパスのリスト(コストの小さい順に並んでいる)
    class KBestPathList {
        DECLARE_CLASS_LOGGER;
        size_t pos;

        std::vector<ReversePathNode*> reverseNodeList;

    public:

        KBestPathList(size_t pos) : pos(pos) {
        }

        ~KBestPathList() {
        }

        size_t getPos() const { return pos; }

        size_t length() const { return reverseNodeList.size(); }

        const std::vector<ReversePathNode*>& getKBestList() const { return reverseNodeList; }

        MString getTopPathString() {
            return reverseNodeList.empty() ? MString() : reverseNodeList[0]->pathString();
        }

#if 0
        // MeCab による最良パスの文字列を取得
        MString getTopPathStringByMecab(std::map<MString, int>& mecabCache) {
            MString result;
            int topCost = INT_MAX;
            for (auto* p : reverseNodeList) {
                MString str = p->pathString();
                _LOG_DEBUGH(_T("str={}"), to_wstr(str));
                if (str != result) {
                    //int cost = MecabBridge::mecabCalcCost(str) + p->totalCost() * 5;
                    int cost = p->totalCostWithMecab(mecabCache);
                    if (cost < topCost) {
                        result = str;
                        topCost = cost;
                    }
                }
            }
            return result;
        }
#endif

        // 新しいPathを追加
        bool addPathNode(ReversePathNode* pathNode, std::map<MString, int>& mecabCache) {
            bool bAdded = false;
            bool bIgnored = false;
            const MString& myStr = pathNode->pathString();
            const MString& myNormalStr = pathNode->normalString();
            //int myCost = mecab(myStr) + pathNode->totalCost();
            int myCost = pathNode->totalCostWithMecab(mecabCache);
            _LOG_DEBUGH(_T("myStr={}, normalStr={}, myCost={}"), to_wstr(myStr), to_wstr(myNormalStr), myCost);
            if (!reverseNodeList.empty()) {
                // top-Kと比較して、コストが小さければ、これを追加
                //auto mecab = [&mecabCache](const MString& s) {
                //    int cost = 0;
                //    auto iter = mecabCache.find(s);
                //    if (iter == mecabCache.end()) {
                //        cost = MecabBridge::mecabCalcCost(s);
                //        mecabCache[s] = cost;
                //    } else {
                //        cost = iter->second;
                //    }
                //    return cost;
                //};
                for (auto iter = reverseNodeList.begin(); iter != reverseNodeList.end(); ++iter) {
                    //const auto* myPrev = pathNode->prevNode();
                    //const auto* otherPrev = (*iter)->prevNode();
                    //bool bSame = myPrev && otherPrev && myPrev->pathString() == otherPrev->pathString();
                    //const MString& otherStr = (*iter)->pathString();
                    //int otherCost = mecab(otherStr) + (*iter)->totalCost();
                    int otherCost = (*iter)->totalCostWithMecab(mecabCache);
                    _LOG_DEBUGH(_T("    otherStr={}, otherNormalStr={}, otherCost={}"), to_wstr((*iter)->pathString()), to_wstr((*iter)->normalString()), otherCost);
                    //if (pathNode->totalCost() < (*iter)->totalCost()) 
                    if (myCost < otherCost) {
                        iter = reverseNodeList.insert(iter, pathNode);    // iter は挿入したノードを指す
                        bAdded = true;
                        // 下位のノードで同じ文字列のものを探し、あればそれを削除
                        for (++iter; iter != reverseNodeList.end(); ++iter) {
                            if (myNormalStr == (*iter)->normalString()) {
                                // 同じ文字列なので、古い pahtNode は削除
                                const auto* pRemoved = *iter;
                                reverseNodeList.erase(iter);
                                _LOG_DEBUGH(_T("    REMOVE second best or lesser reversePath: {}"), formatStringOfReverseNodeList(pRemoved));
                                break;
                            }
                        }
                        //// 同じLatticeNodeを持つやつがあれば、それを削除(最小コストのだけを残しておけばよいので) ⇒ いや、それはダメ(後で2位以下が復活する可能性がある)
                        //++iter;
                        //for (; iter != reverseNodeList.end(); ++iter) {
                        //    if ((*iter)->pNode == pathNode->pNode) {
                        //        const auto* pRemoved = *iter;
                        //        reverseNodeList.erase(iter);
                        //        _LOG_DEBUGH(_T("REMOVE second best or lesser reversePath: {}"), formatStringOfReverseNodeList(pRemoved));
                        //        break;
                        //    }
                        //}
                        break;
                    } else if (myNormalStr == (*iter)->normalString()) {
                        // 同じ文字列なので、当 pahtNode は無視
                        bIgnored = true;
                        break;
                    }
                }
            }
            if (!bAdded && !bIgnored && reverseNodeList.size() < BestKSize) {
                // 余裕があれば末尾に追加
                reverseNodeList.push_back(pathNode);
                bAdded = true;
            }
            if (reverseNodeList.size() > BestKSize) {
                // kBestサイズを超えたら末尾を削除
                reverseNodeList.resize(BestKSize);
                _LOG_DEBUGH(_T("    REMOVE OVERFLOW ENTRY"));
            }
            if (bAdded) {
                _LOG_DEBUGH(_T("    ADD reversePath: {}"), formatStringOfReverseNodeList(pathNode));
            } else {
                _LOG_DEBUGH(_T("    ABANDON reversePath: {}, totalCost={}"), formatStringOfReverseNodeList(pathNode), pathNode->totalCost());
            }
            return bAdded;
        }

        String toString() const {
            String result;
            for (size_t i = 0; i < reverseNodeList.size(); ++i) {
                if (i > 0) result.append(_T("\n"));
                result.append(std::to_wstring(i));
                result.append(_T(": "));
                result.append(formatStringOfReverseNodeList(reverseNodeList[i]));
            }
            return result;
        }

        int getMinTotalCost() {
            return !reverseNodeList.empty() ? reverseNodeList[0]->totalCost() : 0;
        }

        int getMaxTotalCost() {
            return !reverseNodeList.empty() ? reverseNodeList.back()->totalCost() : 0;
        }

    };
    DEFINE_CLASS_LOGGER(KBestPathList);

    // create した T を後で delete するために使う
    template<class T> class ObjectStore {
        std::vector<T*> store;

    public:
        ~ObjectStore() {
            clear();
        }

        void clear() {
            for (auto p : store) {
                delete p;
            }
            store.clear();
        }

        T* addObject(T* p) {
            store.push_back(p);
            return p;
        }

        size_t getSize() const {
            return store.size();
        }

        T* getObjectByPos(size_t pos) {
            if (pos >= getSize()) {
                for (size_t i = getSize(); i < pos; ++i) addObject(new T(i));
                addObject(new T(pos));
            }
            return store[pos];
        }
    };

    // 打鍵終了位置ごとの LatticeNode のリストおよび生存管理
    class LatticeNodeListStore {
        DECLARE_CLASS_LOGGER;

        ObjectStore<LatticeNodeListByEndPos> store;

    public:
        LatticeNodeListByEndPos* addEmptyLatticeNodeList() {
            LatticeNodeListByEndPos* p = new LatticeNodeListByEndPos(store.getSize());
            store.addObject(p);
            return p;
        }

        LatticeNodeListByEndPos* getLatticeNodeListByPos(size_t pos) {
            return store.getObjectByPos(pos);
        }

        // 自ノードによる1gramノードおよび1つ前の1-2gramノードと合体させて2-3gramノードを作成する
        std::vector<_LatticeNode*> createNgramLatticeNode(const WordPiece& piece, size_t endPos) {
            _LOG_DEBUGH(_T("ENTER: piece={}, endPos={}"), piece.toString(), endPos);
            std::vector<_LatticeNode*> result;
            auto* pEndList = getLatticeNodeListByPos(endPos);
            {
                auto* p = new _LatticeNode(piece);
                pEndList->addLatticeNode(p);
                result.push_back(p);
                _LOG_DEBUGH(_T("SELF: {}"), to_wstr(p->str()));
            }

            if (piece.strokeLen <= 2 && piece.strokeLen <= endPos) {
                size_t connPos = endPos - piece.strokeLen;
                const auto& nodes = getLatticeNodeListByPos(connPos)->latticeNodes;
                _LOG_DEBUGH(_T("CONN-POS={}, nodes.size()={}"), connPos, nodes.size());
                for (auto pp : nodes) {
                    MString str = pp->str() + piece.pieceStr;
                    size_t strkLen = pp->strokeLen() + piece.strokeLen;
                    _LOG_DEBUGH(_T("prevNode: {}, strlen={}"), to_wstr(pp->str()), str.size());
                    if (str.size() <= 3) {
                        auto iter = wordCosts.find(str);
                        if (iter != wordCosts.end()) {
                            auto* p = new _LatticeNode(str, strkLen, iter->second);
                            pEndList->addLatticeNode(p);
                            result.push_back(p);
                            _LOG_DEBUGH(_T("NEW Ngram: {}, cost={}"), to_wstr(p->str()), p->cost());
                        }
                    }
                }
            }
            _LOG_DEBUGH(_T("LEAVE: created Ngram num={}"), result.size());
            return result;
        }

        void clear() {
            store.clear();
        }
    };
    DEFINE_CLASS_LOGGER(LatticeNodeListStore);

    // ReversePathNode の生存管理
    class ReversePathNodeStore {
        DECLARE_CLASS_LOGGER;

        ObjectStore<ReversePathNode> store;

    public:
        ReversePathNode* createReversePathNode(_LatticeNode* lNode, ReversePathNode* pPrev) {
            ReversePathNode* p = new ReversePathNode(lNode, pPrev);
            store.addObject(p);
            return p;
        }

        void clear() {
            store.clear();
        }
    };
    DEFINE_CLASS_LOGGER(ReversePathNodeStore);

    // 打鍵終了位置ごとの k-Best なパスのリストの生存管理
    class KBestPathListStore {
        DECLARE_CLASS_LOGGER;

        ObjectStore<KBestPathList> store;

    public:
        KBestPathList* addEmptyKBestPathList() {
            KBestPathList* p = new KBestPathList(store.getSize());
            store.addObject(p);
            return p;
        }

        KBestPathList* getKBestPathListByPos(size_t pos) {
            return store.getObjectByPos(pos);
        }

        void clear() {
            store.clear();
        }

        String debugString() {
            std::vector<String> buf;
            for (size_t i = 0; i < store.getSize(); ++i) {
                buf.push_back(std::to_wstring(i) + _T(":") + std::to_wstring(store.getObjectByPos(i)->length()));
            }
            return utils::join(buf, _T(", "));
        }
    };
    DEFINE_CLASS_LOGGER(KBestPathListStore);

    // Lattice
    class LatticeImpl : public Lattice {
        DECLARE_CLASS_LOGGER;
    private:
        // 打鍵終了位置ごとの LatticeNode のリストおよび生存管理
        LatticeNodeListStore lnodeListStore;

        // 打鍵終了位置ごとの k-Best なパスのリストおよび生存管理
        KBestPathListStore kBestPathListStore;

        // ReversePathNode の生存管理
        ReversePathNodeStore  reversePathNodeStore;

        // 前回生成された文字列
        MString prevOutputStr;

        size_t calcCommonLenWithPrevStr(const MString& outStr) {
            _LOG_DEBUGH(_T("ENTER: outStr={}, prevStr={}"), to_wstr(outStr), to_wstr(prevOutputStr));
            size_t n = 0;
            while (n < outStr.size() && n < prevOutputStr.size()) {
                if (outStr[n] != prevOutputStr[n]) break;
                ++n;
            }
            _LOG_DEBUGH(_T("LEAVE: commonLen={}"), n);
            return n;
        }

        void tryNewReversePathList(KBestPathList* pathList, _LatticeNode* lNode, ReversePathNode* pPrev, std::map<MString, int>& mecabCache) {
            auto* pRPNode = reversePathNodeStore.createReversePathNode(lNode, pPrev);
            //pRPNode->calcCost();
            pathList->addPathNode(pRPNode, mecabCache);
            //if (pRPNode->totalCost < lNode->minCost) {
            //    lNode->minCost = pRPNode->totalCost;
            //    _LOG_DEBUGH(_T("TRY reversePath: {}"), formatStringOfReverseNodeList(pRPNode));
            //    pathList->addPathNode(pRPNode);
            //} else {
            //    _LOG_DEBUGH(_T("ABANDON reversePath: {}, minCost={}"), formatStringOfReverseNodeList(pRPNode), lNode->minCost);
            //}
        }

        // 新規ノードの追加による k-Best の作り直し
        // pathList:新規ノード位置における初期PathList(空)
        void remakeKBestList(KBestPathList* pathList, size_t endPos, _LatticeNode* lNode, std::map<MString, int>& mecabCache) {
            _LOG_DEBUGH(_T("ENTER: pathList.size()={}, minCost={}, maxCost={}, endPos={}, lNode={}"),
                pathList->getKBestList().size(), pathList->getMinTotalCost(), pathList->getMaxTotalCost(), endPos, lNode->toString());
            if (lNode->strokeLen() > endPos) {
                tryNewReversePathList(pathList, lNode, 0, mecabCache);
            } else {
                size_t pos = endPos - lNode->strokeLen();
                //auto* pListByPos = getKBestPathListByPos(pos);
                auto* pListByPos = kBestPathListStore.getKBestPathListByPos(pos);
                _LOG_DEBUGH(_T("KBestPathList: pos={}, PathNum={}"), pos, pListByPos ? pListByPos->getKBestList().size() : -1);
                if (pListByPos) {
                    for (auto* pPrev : pListByPos->getKBestList()) {
                        tryNewReversePathList(pathList, lNode, pPrev, mecabCache);
                    }
                }
            }
            _LOG_DEBUGH(_T("LEAVE: pathList.size()={}"), pathList->getKBestList().size());
        }

#if IS_LOG_DEBUGH_ENABLED
        String formatStringOfWordPieces(const std::vector<WordPiece>& pieces) {
            return utils::join(utils::select<String>(pieces, [](WordPiece p){return p.toString();}), _T("|"));
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

        // 単語素片リストの追加(単語素片が得られなかった場合も含め、各打鍵ごとに呼び出すこと)
        // 単語素片(WordPiece): 打鍵後に得られた出力文字列と、それにかかった打鍵数
        LatticeResult addPieces(const std::vector<WordPiece>& pieces) override {
            _LOG_DEBUGH(_T("ENTER: kBest=[{}], pieces: {}"), kBestPathListStore.debugString(), formatStringOfWordPieces(pieces));
            // endPos における空のノード リストを追加
            size_t endPos = lnodeListStore.addEmptyLatticeNodeList()->getPos();
            _LOG_DEBUGH(_T("endPos={}"), endPos);
            // endPos における空の k-best path リストを取得
            std::map<MString, int> mecabCache;
            auto* pathList = kBestPathListStore.getKBestPathListByPos(endPos);
            for (auto& piece : pieces) {
                for (auto* pLnode : lnodeListStore.createNgramLatticeNode(piece, endPos)) {
                    // 新規ノードの追加による k-Best の作り直し
                    remakeKBestList(pathList, endPos, pLnode, mecabCache);
                }
            }
            _LOG_DEBUGH(_T(".\npathList:\n{}"), pathList->toString());
            size_t numBS = 0;
            //MString outStr = pathList->getTopPathStringByMecab();
            MString outStr = pathList->getTopPathString();
            if (!outStr.empty()) {
                size_t len = calcCommonLenWithPrevStr(outStr);
                numBS = prevOutputStr.size() - len;
                prevOutputStr = outStr;
                outStr = utils::safe_substr(outStr, len);
            }
            _LOG_DEBUGH(_T("LEAVE: OUTPUT: {}, numBS={}, kBest=[{}]"), to_wstr(outStr), numBS, kBestPathListStore.debugString());
            return LatticeResult(outStr, numBS);
        }

        void clear() override {
            _LOG_DEBUGH(_T("CALLED"));
            prevOutputStr.clear();
            reversePathNodeStore.clear();
            kBestPathListStore.clear();
            lnodeListStore.clear();
        }
    };
    DEFINE_CLASS_LOGGER(LatticeImpl);

}


std::unique_ptr<Lattice> Lattice::Singleton;

void Lattice::createLattice() {
    lattice::loadCostFile();
    Singleton.reset(new lattice::LatticeImpl());
}

//void Lattice::loadCostFile() {
//    lattice::loadCostFile();
//}

#if 0
DEFINE_CLASS_LOGGER(LatticeState);

std::unique_ptr<LatticeState> LatticeState::latticeState;

LatticeState::LatticeState() {
    _LOG_DEBUGH(_T("CALLED: Constructor"));
}

LatticeState::~LatticeState() {
    _LOG_DEBUGH(_T("CALLED: Destructor"));
}

void LatticeState::CreateSingleton() {
    latticeState.reset(new LatticeState());
}
#endif