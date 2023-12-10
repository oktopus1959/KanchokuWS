#include "Logger.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "KanchokuIni.h"
#include "Constants.h"
#include "DeckeyToChars.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "Node.h"
#include "State.h"
#include "OutputStack.h"

#include "History/History.h"
#include "StrokeTable.h"
#include "Merger.h"
#include "Lattice.h"

#if 1
#undef LOG_INFO
#undef LOG_DEBUGH
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#endif

namespace {
    // -------------------------------------------------------------------
    // 
    class StrokeStream {
        DECLARE_CLASS_LOGGER;

        std::shared_ptr<State> pState;
        Node* pNextNode = 0;    // Node のライフタイムは別に管理されている
        size_t nStroke = 0;

        MStringApplyResult getXlatString() {
            std::unique_ptr<State> p;
            p.reset(pNextNode->CreateState());  // 文字列状態の生成
            return p->ApplyResultString();
        }

        String nextNodeType() const {
            return NODE_NAME(pNextNode);
        }

    public:
        // コンストラクタ
        StrokeStream() : pNextNode(0), nStroke(0) {
            _LOG_DEBUGH(_T("CALLED: Default Constructor"));
        }

        // コンストラクタ
        StrokeStream(StrokeTableNode* pRootNode) {
            _LOG_DEBUGH(_T("CALLED: Constructor: Newly created RootNode passed"));
            pState.reset(pRootNode->CreateState());
        }

        // デストラクタ
        ~StrokeStream() {
            _LOG_DEBUGH(_T("CALLED: Destructor"));
            //if (pState) {
            //    pState->DeleteAllStates();
            //    delete pState;
            //    pState = nullptr;
            //}
        }

        size_t StrokeTableChainLength() const {
            return pState ? pState->StrokeTableChainLength() : 0;
        }

        String GetJoinedName() const {
            return pState->JoinedName();
        }

        Node* NextNode() {
            return pNextNode;
        }

        void HandleDeckeyChain(int decKey) {
            _LOG_DEBUGH(_T("ENTER: nStroke={}"), nStroke);
            if (pState) {
                pState->HandleDeckeyChain(decKey);
                ++nStroke;
            }
            _LOG_DEBUGH(_T("LEAVE: nStroke={}, NextNode.type={}"), nStroke, nextNodeType());
        }

        void DoDeckeyPostProcChain() {
            _LOG_DEBUGH(_T("ENTER"));
            if (pState) {
                pState->DoDeckeyPostProcChain();
                pNextNode = pState->NextNodeMaybe();
            }
            _LOG_DEBUGH(_T("LEAVE: NextNode.type={}, string={}"), nextNodeType(), pNextNode ? to_wstr(pNextNode->getString()) : _T(""));
        }

        bool IsUnnecessary() const {
            return !pState || pState->IsUnnecessary();
        }

        bool DeleteUnnecessaryState() {
            //bool result = !pState || AbstractBaseState::DeleteUnnecessaryState(pState);
            bool result = false;
            if (pState) {
                pState->DeleteUnnecessarySuccessorStateChain();
                if (pState->IsUnnecessary()) {
                    pState.reset();
                    result = true;
                }
            }
            _LOG_DEBUGH(_T("CALLED: result={}, NextNode.type={}"), result, nextNodeType());
            return result;
        }

        void AppendWordPiece(std::vector<WordPiece>& pieces, bool bExcludeHiragana) {
            _LOG_DEBUGH(_T("CALLED: NextNode.type={}, String={}, bExcludeHiragana={}"),
                nextNodeType(), pNextNode ? to_wstr(pNextNode->getString()) : _T("empty"), bExcludeHiragana);
            if (pNextNode && pNextNode->isStringLikeNode() && !pNextNode->getString().empty() && (!bExcludeHiragana || !utils::is_hiragana(pNextNode->getString()[0]))) {
                _LOG_DEBUGH(_T("ENTER: string={}"), to_wstr(pNextNode->getString()));
                auto applyResult = getXlatString();
                pieces.push_back(WordPiece(applyResult.resultStr, nStroke, applyResult.rewritableLen, applyResult.numBS));
                _LOG_DEBUGH(_T("LEAVE: piece=({}, {})"), to_wstr(pieces.back().pieceStr), pieces.back().strokeLen);
            }
        }
    };
    DEFINE_CLASS_LOGGER(StrokeStream);

    typedef std::unique_ptr<StrokeStream> StrokeStreamUptr;

    // 複数のStrokeStateを管理するクラス
    // たとえばT-Codeであっても、1ストロークずれた2つの状態が並存する可能性がある
    class StrokeStateList {
    private:
        DECLARE_CLASS_LOGGER;

        // RootStrokeState用の状態集合
        std::vector<StrokeStreamUptr> strokeStreamList;

        void addStrokeStream(StrokeTableNode* pRootNode) {
            strokeStreamList.push_back(std::make_unique<StrokeStream>(pRootNode));
        }

        void forEach(std::function<void(const StrokeStreamUptr&)> func) const {
            for (const auto& pCh : strokeStreamList) {
                func(pCh);
            }
        }

    public:
        ~StrokeStateList() {
            _LOG_DEBUGH(_T("CALLED: Destructor"));
            //for (auto* p : strokeChannelList) delete p;
        }

        size_t Count() const {
            return strokeStreamList.size();
        }

        size_t Empty() const {
            return Count() == 0;
        }

        void Clear() {
            strokeStreamList.clear();
        }

        size_t StrokeTableChainLength() const {
            size_t len = 0;
            //for (const auto& pState : strokeChannelList) {
            //    size_t ln = pState->StrokeTableChainLength();
            //    if (ln > len) len = ln;
            //}
            forEach([&len](const StrokeStreamUptr& pCh) {
                size_t ln = pCh->StrokeTableChainLength();
                if (ln > len) len = ln;
            });
            return len;
        }

        String ChainLengthString() const {
            std::vector<int> buf;
//            std::transform(strokeChannelList.begin(), strokeChannelList.end(), std::back_inserter(buf), [](const StrokeStream* pState) { return (int)pState->StrokeTableChainLength(); });
            std::transform(strokeStreamList.begin(), strokeStreamList.end(), std::back_inserter(buf), [](const StrokeStreamUptr& pState) { return (int)pState->StrokeTableChainLength(); });
            return utils::join(buf, _T(","));
        }

        Node* GetNonStringNode() {
            for (const auto& pState : strokeStreamList) {
                Node* p = pState->NextNode();
                if (p && !p->isStringLikeNode()) {
                    return p;
                }
            }
            return 0;
        }

        void DoDeckeyPreProc(StrokeTableNode* pRootNode, int decKey, bool bMerge) {
            _LOG_DEBUGH(_T("ENTER"));
            if (Count() < 1 || bMerge) addStrokeStream(pRootNode);
            _LOG_DEBUGH(_T("strokeStateList.Count={}"), Count());
            forEach([decKey](const StrokeStreamUptr& pCh) {
                _LOG_DEBUGH(_T("IsUnnecessary: BEFORE: {}"), pCh->IsUnnecessary());
                pCh->HandleDeckeyChain(decKey);
                _LOG_DEBUGH(_T("IsUnnecessary: AFTER: {}"), pCh->IsUnnecessary());
            });
            _LOG_DEBUGH(_T("LEAVE: strokeStateList.Count={}"), Count());
        }

        void DoPostCheckChain() {
            _LOG_DEBUGH(_T("ENTER"));
            //for (const auto& pState : strokeChannelList) {
            //    //_LOG_DEBUGH(_T("IsUnnecessary: BEFORE: {}"), pState->IsUnnecessary());
            //    pState->DoPostCheckChain();
            //    //_LOG_DEBUGH(_T("IsUnnecessary: AFTER: {}"), pState->IsUnnecessary());
            //}
            forEach([](const StrokeStreamUptr& pCh) {
                //_LOG_DEBUGH(_T("IsUnnecessary: BEFORE: {}"), pState->IsUnnecessary());
                pCh->DoDeckeyPostProcChain();
                //_LOG_DEBUGH(_T("IsUnnecessary: AFTER: {}"), pState->IsUnnecessary());
            });
            _LOG_DEBUGH(_T("LEAVE"));
        }

        void DeleteUnnecessaryNextState() {
            _LOG_DEBUGH(_T("ENTER: strokeStateList.Count={}"), Count());
            auto iter = strokeStreamList.begin();
            while (iter != strokeStreamList.end()) {
                if ((*iter)->DeleteUnnecessaryState()) {
                    iter = strokeStreamList.erase(iter);
                } else {
                    ++iter;
                }
            }
            _LOG_DEBUGH(_T("LEAVE: strokeStateList.Count={}"), Count());
        }

        void AddWordPieces(std::vector<WordPiece>& pieces, bool bExcludeHiragana) {
            _LOG_DEBUGH(_T("ENTER: size={}"), Count());
            //for (const auto& pState : strokeChannelList) {
            //    pState->AppendWordPiece(pieces, bExcludeHiragana);
            //}
            forEach([&pieces, bExcludeHiragana](const StrokeStreamUptr& pCh) {
                pCh->AppendWordPiece(pieces, bExcludeHiragana);
            });
            _LOG_DEBUGH(_T("LEAVE"));
        }
        
        void DebugPrintStatesChain(StringRef label) {
            if (Reporting::Logger::IsInfoHEnabled()) {
                forEach([label](const StrokeStreamUptr& pCh) {
                    _LOG_DEBUGH(_T("{}={}"), label, pCh->GetJoinedName());
                });
            }
        }
    };
    DEFINE_CLASS_LOGGER(StrokeStateList);

    // -------------------------------------------------------------------
    // ストロークテーブルからの出力のマージ機能
    class StrokeMergerState : public State {
    private:
        DECLARE_CLASS_LOGGER;

        // RootStrokeState1用の状態集合
        StrokeStateList stateList1;

        // RootStrokeState2用の状態集合
        StrokeStateList stateList2;

    public:
        // コンストラクタ
        StrokeMergerState(StrokeMergerNode* pN) {
            _LOG_DEBUGH(_T("CALLED: Constructor"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~StrokeMergerState() override {
            _LOG_DEBUGH(_T("CALLED: Destructor"));
        }

        // 状態が生成されたときに実行する処理 (特に何もせず、前状態にチェインする)
        bool DoProcOnCreated() override {
            _LOG_DEBUGH(_T("CALLED: Chained"));
            return true;
        }

        // DECKEY 処理の流れ
        void HandleDeckeyChain(int deckey) override {
            LOG_INFO(_T("ENTER: deckey={:x}H({}), totalCount={}, statesNum=({},{})"), deckey, deckey, STATE_COMMON->GetTotalDecKeyCount(), stateList1.Count(), stateList2.Count());

            if (SETTINGS->multiStreamMode)
                handleDeckey_lattice(deckey);
            else
                handleDeckey_single(deckey);
            LOG_INFO(_T("LEAVE"));
        }

    private:
        // DECKEY 処理の流れ
        void handleDeckey_lattice(int deckey) {
            LOG_INFO(_T("ENTER: deckey={:x}H({}), totalCount={}, states=({},{})"), deckey, deckey, STATE_COMMON->GetTotalDecKeyCount(), stateList1.Count(), stateList2.Count());

            stateList1.DebugPrintStatesChain(_T("ENTER: stateList1"));
            stateList2.DebugPrintStatesChain(_T("ENTER: stateList2"));

            // 前処理(ストローク木状態の作成と呼び出し)
            _LOG_DEBUGH(_T("stateList1: doDeckeyPreProc"));
            stateList1.DoDeckeyPreProc(StrokeTableNode::RootStrokeNode1.get(), deckey, true);
            _LOG_DEBUGH(_T("stateList2: doDeckeyPreProc"));
            stateList2.DoDeckeyPreProc(StrokeTableNode::RootStrokeNode2.get(), deckey, true);

            // 後続状態に対して事後チェック
            //DoPostCheckChain();
            _LOG_DEBUGH(_T("stateList1: doPostCheck"));
            stateList1.DoPostCheckChain();
            _LOG_DEBUGH(_T("stateList2: doPostCheck"));
            stateList2.DoPostCheckChain();

            // 単語素片の収集
            std::vector<WordPiece> pieces;
            _LOG_DEBUGH(_T("stateList1: AddWordPieces"));
            stateList1.AddWordPieces(pieces, false);
            _LOG_DEBUGH(_T("stateList2: AddWordPieces"));
            stateList2.AddWordPieces(pieces, false);

            // 不要になったストローク状態の削除
            _LOG_DEBUGH(_T("stateList1: deleteUnnecessaryNextState"));
            stateList1.DeleteUnnecessaryNextState();
            _LOG_DEBUGH(_T("stateList2: deleteUnnecessaryNextState"));
            stateList2.DeleteUnnecessaryNextState();

            Node* pNextNode1 = stateList1.GetNonStringNode();
            Node* pNextNode2 = stateList2.GetNonStringNode();
            if (pNextNode1 || pNextNode2) {
                // 文字列ノード以外が返ったら、状態をクリアする
                _LOG_DEBUGH(_T("NonStringNode FOUND: pNextNode1={:p}, pNextNode2={:p}"), (void*)pNextNode1, (void*)pNextNode2);
                stateList1.Clear();
                stateList2.Clear();
            }
            if (stateList1.Empty() && stateList2.Empty()) {
                // 全てのストローク状態が削除されたので、後処理してマージノードも削除
                _LOG_DEBUGH(_T("REMOVE ALL"));
                SetNextNodeMaybe(pNextNode1 ? pNextNode1 : pNextNode2);
                MarkUnnecessary();
            }
            
            if (!IsUnnecessary() || !pieces.empty()) {
                // Lattice処理
                auto result = WORD_LATTICE->addPieces(pieces);

                // 新しい文字列が得られたら履歴状態に送る
                if (!result.outStr.empty()) {
                    _LOG_DEBUGH(_T("HISTORY_RESIDENT_STATE->SetTranslatedOutString({}, 0, false, {})"), to_wstr(result.outStr), result.numBS);
                    HISTORY_RESIDENT_STATE->SetTranslatedOutString(result.outStr, 0, false, result.numBS);
                }
            }

            stateList1.DebugPrintStatesChain(_T("LEAVE: stateList1"));
            stateList2.DebugPrintStatesChain(_T("LEAVE: stateList2"));

            LOG_INFO(_T("LEAVE: states=({},{})\n"), stateList1.Count(), stateList2.Count());
        }

        // DECKEY 処理の流れ(単一テーブル版)
        void handleDeckey_single(int deckey) {
            LOG_INFO(_T("ENTER: deckey={:x}H({}), totalCount={}, states=({},{})"), deckey, deckey, STATE_COMMON->GetTotalDecKeyCount(), stateList1.Count(), stateList2.Count());

            // 前処理(ストローク木状態の作成と呼び出し)
            _LOG_DEBUGH(_T("ROOT_STROKE_NODE: doDeckeyPreProc: CurrentStrokeTable={}"), StrokeTableNode::GetCurrentStrokeTableNum());
            stateList1.DoDeckeyPreProc(ROOT_STROKE_NODE, deckey, false);

            // 後続状態に対して事後チェック
            //DoPostCheckChain();
            _LOG_DEBUGH(_T("ROOT_STROKE_NODE: doPostCheck"));
            stateList1.DoPostCheckChain();

            // 単語素片の収集
            std::vector<WordPiece> pieces;
            _LOG_DEBUGH(_T("ROOT_STROKE_NODE: AddWordPieces"));
            stateList1.AddWordPieces(pieces, false);

            // 不要になったストローク状態の削除
            _LOG_DEBUGH(_T("ROOT_STROKE_NODE: deleteUnnecessaryNextState"));
            stateList1.DeleteUnnecessaryNextState();

            Node* pNextNode1 = stateList1.GetNonStringNode();
            _LOG_DEBUGH(_T("pNextNode1={}"), NODE_NAME(pNextNode1));
            if (pNextNode1) {
                // 文字列ノード以外が返ったら、状態をクリアする
                _LOG_DEBUGH(_T("NonStringNode FOUND: pNextNode1={}"), NODE_NAME(pNextNode1));
                stateList1.Clear();
            }
            if (stateList1.Empty()) {
                // 全てのストローク状態が削除されたので、後処理してマージノードも削除
                _LOG_DEBUGH(_T("REMOVE ALL: SetNextNodeMaybe(pNextNode1={})"), NODE_NAME(pNextNode1));
                SetNextNodeMaybe(pNextNode1);
                MarkUnnecessary();
            }
            
            if (!pieces.empty()) {
                // 新しい文字列が得られたら履歴状態に送る
                auto result = pieces[0];
                if (!result.pieceStr.empty()) {
                    _LOG_DEBUGH(_T("HISTORY_RESIDENT_STATE->SetTranslatedOutString(wordPiece={})"), result.toString());
                    HISTORY_RESIDENT_STATE->SetTranslatedOutString(result.pieceStr, result.rewritableLen, false, result.numBS);
                }
            }

            LOG_INFO(_T("LEAVE: states=({},{}), NextNodeMaybe={}\n"), stateList1.Count(), stateList2.Count(), NODE_NAME(NextNodeMaybe()));
        }

    public:
        // ストロークテーブルチェインの長さ(テーブルのレベル)
        size_t StrokeTableChainLength() const override {
            size_t len1 = stateList1.StrokeTableChainLength();
            size_t len2 = stateList2.StrokeTableChainLength();
            return len1 < len2 ? len2 : len1;
        }

        String JoinedName() const override {
            return Name + _T("(") + stateList1.ChainLengthString() + _T("/") + stateList2.ChainLengthString()  + _T(")");
        }

    };
    DEFINE_CLASS_LOGGER(StrokeMergerState);

    DEFINE_LOCAL_LOGGER(StrokeMerger);

    // テーブルファイルを読み込んでストローク木を作成する
    void createStrokeTree(StringRef tableFile, void(*treeCreator)(StringRef, std::vector<String>&)) {
        LOG_INFO(_T("ENTER: tableFile={}"), tableFile);

        utils::IfstreamReader reader(tableFile);
        if (reader.success()) {
            //auto lines = utils::IfstreamReader(tableFile).getAllLines();
            auto lines = reader.getAllLines();
            // ストロークノード木の構築
            treeCreator(tableFile, lines);
            LOG_INFO(_T("close table file: {}"), tableFile);
        } else {
            // エラー
            LOG_ERROR(_T("Can't read table file: {}"), tableFile);
            ERROR_HANDLER->Error(std::format(_T("テーブルファイル({})が開けません"), tableFile));
        }

        LOG_INFO(_T("LEAVE"));
    }

} // namespace

// -------------------------------------------------------------------
// StrokeMergerNode - マージ入力機能 常駐ノード
DEFINE_CLASS_LOGGER(StrokeMergerNode);

// コンストラクタ
StrokeMergerNode::StrokeMergerNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
StrokeMergerNode::~StrokeMergerNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* StrokeMergerNode::CreateState() {
    return new StrokeMergerState(this);
}

// テーブルファイルを読み込んでストローク木を作成する
void StrokeMergerNode::createStrokeTrees(bool bForceSecondary) {
    // テーブルファイル名
    if (SETTINGS->tableFile.empty()) {
        // エラー
        ERROR_HANDLER->Error(_T("「tableFile=(ファイル名)」の設定がまちがっているようです"));
    } else {

#define STROKE_TREE_CREATOR(F) [](StringRef file, std::vector<String>& lines) {F(file, lines);}

        // 主テーブルファイルの構築
        createStrokeTree(utils::joinPath(SETTINGS->rootDir, _T("tmp\\tableFile1.tbl")), STROKE_TREE_CREATOR(StrokeTableNode::CreateStrokeTree));

        if (bForceSecondary || !SETTINGS->tableFile2.empty()) {
            // 副テーブルファイルの構築
            createStrokeTree(utils::joinPath(SETTINGS->rootDir, _T("tmp\\tableFile2.tbl")), STROKE_TREE_CREATOR(StrokeTableNode::CreateStrokeTree2));
        }

        if (!SETTINGS->tableFile3.empty()) {
            // 第3テーブルファイルの構築
            createStrokeTree(utils::joinPath(SETTINGS->rootDir, _T("tmp\\tableFile3.tbl")), STROKE_TREE_CREATOR(StrokeTableNode::CreateStrokeTree3));
        }
    }
}

// マージ機能常駐ノードの生成
void StrokeMergerNode::CreateSingleton() {
    STROKE_MERGER_NODE.reset(new StrokeMergerNode());
}

// Singleton
std::unique_ptr<StrokeMergerNode> StrokeMergerNode::Singleton;

