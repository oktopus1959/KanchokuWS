#include "Logger.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

//#include "KanchokuIni.h"
#include "Constants.h"
#include "DeckeyToChars.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "Node.h"
#include "State.h"
//#include "OutputStack.h"
//#include "StrokeHelp.h"

#include "BushuComp/BushuComp.h"
#include "Eisu.h"
#include "Zenkaku.h"
#include "ModalStateUtil.h"

#include "FunctionNodeManager.h"

#include "History/HistoryStateBase.h"
#include "History/History.h"
//#include "StrokeTable.h"
#include "Merger.h"
#include "StrokeMergerHistoryResidentState.h"
#include "Lattice.h"

#if 1
#undef LOG_INFO
#undef LOG_DEBUGH
#undef _LOG_DEBUGH
#undef LOG_DEBUG
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#endif

namespace {

    // -------------------------------------------------------------------
    // 
    class StrokeStream : public State {
        DECLARE_CLASS_LOGGER;

        //std::shared_ptr<State> pState;
        //Node* pNextNode = 0;    // Node のライフタイムは別に管理されている
        size_t cntStroke = 0;

        //MStringApplyResult getXlatString() {
        //    if (NextNodeMaybe()) {
        //        std::unique_ptr<State> p;
        //        p.reset(NextNodeMaybe()->CreateState());  // 文字列状態の生成
        //        return p->ApplyResultString();
        //    } else {
        //        return MStringApplyResult();
        //    }
        //}

        String nextNodeType() const {
            return NODE_NAME(NextNodeMaybe());
        }

        //String nextNodeString() const {
        //    return NextNodeMaybe() ? to_wstr(NextNodeMaybe()->getString()) : _T("");
        //}

    public:
        // コンストラクタ
        StrokeStream() : cntStroke(STATE_COMMON->GetTotalDecKeyCount()-1) {
            _LOG_DEBUGH(_T("CALLED: Default Constructor"));
            Initialize(_T("StrokeStream"), 0);
            MarkNecessary();
        }

        // コンストラクタ
        StrokeStream(StrokeTableNode* pRootNode) : StrokeStream() {
            _LOG_DEBUGH(_T("CALLED: Constructor: Newly created RootNode passed"));
            //pState.reset(pRootNode->CreateState());
            //SetNextState(pRootNode->CreateState());
            SetNextNodeMaybe(pRootNode);
            CreateNewState();
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

        //// 状態が生成されたときに実行する処理 (特に何もせず、前状態にチェインする)
        //void DoProcOnCreated() override {
        //    _LOG_DEBUGH(_T("CALLED: Chained"));
        //    MarkNecessary();
        //}

        //size_t StrokeTableChainLength() const {
        //    return pState ? pState->StrokeTableChainLength() : 0;
        //}

        //String GetJoinedName() const {
        //    return pState->JoinedName();
        //}

        //Node* NextNode() {
        //    return pNextNode;
        //}

        //// DECKEY 処理
        //void HandleDeckeyChain(int decKey) override {
        //    _LOG_DEBUGH(_T("ENTER: cntStroke={}"), cntStroke);
        //    if (pState) {
        //        pState->HandleDeckeyChain(decKey);
        //        ++cntStroke;
        //    }
        //    _LOG_DEBUGH(_T("LEAVE: cntStroke={}, NextNode.type={}"), cntStroke, nextNodeType());
        //}

        //void DoDeckeyPostProcChain() {
        //    _LOG_DEBUGH(_T("ENTER"));
        //    if (pState) {
        //        pState->DoDeckeyPostProcChain();
        //        pNextNode = pState->NextNodeMaybe();
        //    }
        //    _LOG_DEBUGH(_T("LEAVE: NextNode.type={}, string={}"), nextNodeType(), pNextNode ? to_wstr(pNextNode->getString()) : _T(""));
        //}

        //bool IsUnnecessary() const {
        //    return !pState || pState->IsUnnecessary();
        //}

        //bool DeleteUnnecessaryState() {
        //    //bool result = !pState || AbstractBaseState::DeleteUnnecessaryState(pState);
        //    bool result = false;
        //    if (pState) {
        //        pState->DeleteUnnecessarySuccessorStateChain();
        //        if (pState->IsUnnecessary()) {
        //            pState.reset();
        //            result = true;
        //        }
        //    }
        //    _LOG_DEBUGH(_T("CALLED: result={}, NextNode.type={}"), result, nextNodeType());
        //    return result;
        //}

        void AppendWordPiece(std::vector<WordPiece>& pieces, bool /*bExcludeHiragana*/) {
            if (NextState()) {
                _LOG_DEBUGH(_T("ENTER"));
                MStringResult result;
                State::GetResultStringChain(result);
                if (!result.isDefault()) {
                    _LOG_DEBUGH(_T("ADD WORD: rewriteStr={}, string={}, numBS={}"),
                        to_wstr(result.getRewriteNode() ? result.getRewriteNode()->getString() : EMPTY_MSTR), to_wstr(result.resultStr()), result.numBS());
                    int strokeLen = STATE_COMMON->GetTotalDecKeyCount() - cntStroke;
                    if (result.getRewriteNode()) {
                        pieces.push_back(WordPiece(result.getRewriteNode(), strokeLen));
                    } else {
                        pieces.push_back(WordPiece(result.resultStr(), strokeLen, result.rewritableLen(), result.numBS()));
                    }
                } else {
                    _LOG_DEBUGH(_T("NOT TERMINAL"));
                }
                _LOG_DEBUGH(_T("LEAVE"));
            }
            //auto nextStr = nextNodeString();
            //_LOG_DEBUGH(_T("CALLED: NextNode.type={}, String={}, bExcludeHiragana={}"), nextNodeType(), nextStr, bExcludeHiragana);
            //if (NextNodeMaybe() && NextNodeMaybe()->isStringLikeNode() && !nextStr.empty() && (!bExcludeHiragana || !utils::is_hiragana(nextStr[0]))) {
            //    _LOG_DEBUGH(_T("ENTER: string={}"), nextStr);
            //    auto applyResult = getXlatString();
            //    pieces.push_back(WordPiece(applyResult.resultStr, cntStroke, applyResult.rewritableLen, applyResult.numBS));
            //    _LOG_DEBUGH(_T("LEAVE: piece=({}, {})"), to_wstr(pieces.back().pieceStr), pieces.back().strokeLen);
            //}
        }

        // 新しい状態作成のチェイン(状態チェーンの末尾でのみ新状態の作成を行う)
        void DoCreateNewStateChain() {
            CreateNewStateChain();
        }

        // チェーンをたどって不要とマークされた後続状態を削除する
        void DoDeleteUnnecessarySuccessorStateChain() {
            DeleteUnnecessarySuccessorStateChain();
            // 後続状態が無くなったら自身も削除する
            if (!NextState()) MarkUnnecessary();
        }
    };
    DEFINE_CLASS_LOGGER(StrokeStream);

    typedef std::unique_ptr<StrokeStream> StrokeStreamUptr;

    // 複数のStrokeStreamを管理するクラス
    // たとえばT-Codeであっても、1ストロークずれた2つの入力ストリームが並存する可能性がある
    class StrokeStreamList {
    private:
        DECLARE_CLASS_LOGGER;

        String name;

        // RootStrokeState用の状態集合
        std::vector<StrokeStreamUptr> strokeStreamList;

        void addStrokeStream(StrokeTableNode* pRootNode) {
            strokeStreamList.push_back(std::make_unique<StrokeStream>(pRootNode));
        }

        void forEach(std::function<void(const StrokeStreamUptr&)> func) const {
            for (const auto& pStream : strokeStreamList) {
                func(pStream);
            }
        }

    public:
        StrokeStreamList(StringRef name) : name(name) {
        }

        ~StrokeStreamList() {
            _LOG_DEBUGH(_T("CALLED: Destructor: {}"), name);
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
            forEach([&len](const StrokeStreamUptr& pStream) {
                size_t ln = pStream->StrokeTableChainLength();
                if (ln > len) len = ln;
                });
            return len;
        }

        String ChainLengthString() const {
            std::vector<int> buf;
            //            std::transform(strokeChannelList.begin(), strokeChannelList.end(), std::back_inserter(buf), [](const StrokeStream* pState) { return (int)pState->StrokeTableChainLength(); });
            std::transform(strokeStreamList.begin(), strokeStreamList.end(), std::back_inserter(buf), [](const StrokeStreamUptr& pState) { return (int)pState->StrokeTableChainLength(); });
            return _T("[") + utils::join(buf, _T(",")) + _T("]");
        }

        Node* GetNonStringNode() {
            for (const auto& pState : strokeStreamList) {
                Node* p = pState->NextNodeMaybe();
                if (p && !p->isStringLikeNode()) {
                    return p;
                }
            }
            return 0;
        }

        void HandleDeckeyProc(StrokeTableNode* pRootNode, int decKey, int comboStrokeCnt) {
            _LOG_DEBUGH(_T("ENTER: {}: pRootNode={:p}, decKey={}, comboStrokeCnt={}"), name, (void*)pRootNode, decKey, comboStrokeCnt);
            if (pRootNode) {
                // 同時打鍵列に入ったら、先頭打鍵の時(comboStrokeCnt==0)だけ stream を追加できる
                if ((comboStrokeCnt < 1 || (comboStrokeCnt == 1 && Count() < 1)) && State::isStrokableKey(decKey)) addStrokeStream(pRootNode);
                _LOG_DEBUGH(_T("{}: strokeStateList.Count={}"), name, Count());
                int count = 1;
                forEach([decKey, &count, this](const StrokeStreamUptr& pStream) {
                    _LOG_DEBUGH(_T("{}: {}: IsUnnecessary: BEFORE: {}"), name, count, pStream->IsUnnecessary());
                    pStream->HandleDeckeyChain(decKey);
                    _LOG_DEBUGH(_T("{}: {}: IsUnnecessary: AFTER: {}"), name, count, pStream->IsUnnecessary());
                    ++count;
                    });
            }
            _LOG_DEBUGH(_T("LEAVE: {}: strokeStateList.Count={}"), name, Count());
        }

        //void DoDeckeyPostProc() {
        //    _LOG_DEBUGH(_T("ENTER"));
        //    //for (const auto& pState : strokeChannelList) {
        //    //    //_LOG_DEBUGH(_T("IsUnnecessary: BEFORE: {}"), pState->IsUnnecessary());
        //    //    pState->DoDeckeyPostProc();
        //    //    //_LOG_DEBUGH(_T("IsUnnecessary: AFTER: {}"), pState->IsUnnecessary());
        //    //}
        //    forEach([](const StrokeStreamUptr& pStream) {
        //        //_LOG_DEBUGH(_T("IsUnnecessary: BEFORE: {}"), pState->IsUnnecessary());
        //        pStream->DoDeckeyPostProcChain();
        //        //_LOG_DEBUGH(_T("IsUnnecessary: AFTER: {}"), pState->IsUnnecessary());
        //    });
        //    _LOG_DEBUGH(_T("LEAVE"));
        //}

        // 新しい状態作成
        void CreateNewStates() {
            _LOG_DEBUGH(_T("ENTER: {}: size={}"), name, Count());
            forEach([](const StrokeStreamUptr& pStream) {
                pStream->DoCreateNewStateChain();
            });
            _LOG_DEBUGH(_T("LEAVE: {}"), name);
        }

        //// 出力文字を取得する
        //void GetPreOutputs() {
        //    _LOG_DEBUGH(_T("ENTER: size={}"), Count());
        //    forEach([&pieces, bExcludeHiragana](const StrokeStreamUptr& pStream) {
        //        pStream->AppendWordPiece(pieces, bExcludeHiragana);
        //    });
        //    return GetPreOutput(pNext ? pNext->GetResultStringChain() : EMPTY_MSTR);
        //    _LOG_DEBUGH(_T("LEAVE"));
        //}

        void DeleteUnnecessaryNextStates() {
            _LOG_DEBUGH(_T("ENTER: {}: strokeStateList.Count={}"), name, Count());
            auto iter = strokeStreamList.begin();
            while (iter != strokeStreamList.end()) {
                (*iter)->DoDeleteUnnecessarySuccessorStateChain();
                if ((*iter)->IsUnnecessary()) {
                    iter = strokeStreamList.erase(iter);
                } else {
                    ++iter;
                }
            }
            _LOG_DEBUGH(_T("LEAVE: {}: strokeStateList.Count={}"), name, Count());
        }

        // 出力文字を取得する
        void AddWordPieces(std::vector<WordPiece>& pieces, bool bExcludeHiragana) {
            _LOG_DEBUGH(_T("ENTER: {}: streamNum={}"), name, Count());
            //for (const auto& pState : strokeChannelList) {
            //    pState->AppendWordPiece(pieces, bExcludeHiragana);
            //}
            forEach([&pieces, bExcludeHiragana](const StrokeStreamUptr& pStream) {
                pStream->AppendWordPiece(pieces, bExcludeHiragana);
            });
            _LOG_DEBUGH(_T("LEAVE: {}"), name);
        }
        
        void DebugPrintStatesChain(StringRef label) {
            if (Reporting::Logger::IsInfoHEnabled()) {
                if (strokeStreamList.empty()) {
                    _LOG_DEBUGH(_T("{}: {}=empty"), name, label);
                } else {
                    forEach([label, this](const StrokeStreamUptr& pStream) {
                        _LOG_DEBUGH(_T("{}: {}={}"), name, label, pStream->JoinedName());
                    });
                }
            }
        }
    };
    DEFINE_CLASS_LOGGER(StrokeStreamList);

    // -------------------------------------------------------------------
    // ストロークテーブルからの出力のマージ機能
    class StrokeMergerHistoryResidentStateImpl : public StrokeMergerHistoryResidentState {
    private:
        DECLARE_CLASS_LOGGER;

        std::unique_ptr<HistoryStateBase> histBase;

        // 同時打鍵中なら1以上で、同時打鍵列の位置を示す
        int _comboStrokeCount = 0;

        int _strokeCountBS = -1;

        // RootStrokeState1用の状態集合
        StrokeStreamList _streamList1;

        // RootStrokeState2用の状態集合
        StrokeStreamList _streamList2;

    public:
        // コンストラクタ
        StrokeMergerHistoryResidentStateImpl(StrokeMergerHistoryNode* pN)
            : histBase(HistoryStateBase::createInstance(pN))
            , _streamList1(_T("streamList1"))
            , _streamList2(_T("streamList2")) {
            _LOG_DEBUGH(_T("CALLED: Constructor"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~StrokeMergerHistoryResidentStateImpl() override {
            _LOG_DEBUGH(_T("CALLED: Destructor"));
        }

        // 状態が生成されたときに実行する処理 (特に何もせず、前状態にチェインする)
        void DoProcOnCreated() override {
            _LOG_DEBUGH(_T("CALLED: Chained"));
            MarkNecessary();
        }

        // DECKEY 処理の流れ
        void HandleDeckeyChain(int deckey) override {
            LOG_INFO(_T("ENTER: deckey={:x}H({}), totalCount={}, statesNum=({},{})"), deckey, deckey, STATE_COMMON->GetTotalDecKeyCount(), _streamList1.Count(), _streamList2.Count());

            resultStr.clear();
            myChar = '\0';

            _LOG_DEBUGH(_T("NextState={}"), STATE_NAME(NextState()));
            if (NextState()) {
                _LOG_DEBUGH(_T("NextState: FOUND"));
                // 後続状態があれば、そちらを呼び出す
                NextState()->HandleDeckeyChain(deckey);
            } else {
                _streamList1.DebugPrintStatesChain(_T("ENTER: streamList1"));
                _streamList2.DebugPrintStatesChain(_T("ENTER: streamList2"));

                if (deckey != CLEAR_STROKE_DECKEY && ((deckey >= FUNC_DECKEY_START && deckey < FUNC_DECKEY_END) || deckey >= CTRL_DECKEY_START)) {
                    _streamList1.Clear();
                    _streamList2.Clear();
                    switch (deckey) {
                        //case ENTER_DECKEY:
                        //    _LOG_DEBUGH(_T("EnterKey: clear streamList"));
                        //    WORD_LATTICE->clear();
                        //    //MarkUnnecessary();
                        //    State::handleEnter();
                        //    break;
                    case MULTI_STREAM_COMMIT_DECKEY:
                        _LOG_DEBUGH(_T("EnterKey: clear streamList"));
                        WORD_LATTICE->clear();
                        //MarkUnnecessary();
                        break;
                    case BS_DECKEY:
                        _LOG_DEBUGH(_T("BS"));
                        _strokeCountBS = (int)STATE_COMMON->GetTotalDecKeyCount();
                        WORD_LATTICE->selectFirst();
                        if (WORD_LATTICE->isEmpty()) State::handleBS();
                        break;
                    case MULTI_STREAM_NEXT_CAND_DECKEY:
                        _LOG_DEBUGH(_T("MULTI_STREAM_NEXT_CAND: select next candidate"));
                        WORD_LATTICE->selectNext();
                        break;
                    case MULTI_STREAM_PREV_CAND_DECKEY:
                        _LOG_DEBUGH(_T("MULTI_STREAM_PREV_CAND: select prev candidate"));
                        WORD_LATTICE->selectPrev();
                        break;
                    case MULTI_STREAM_SELECT_FIRST_DECKEY:
                        _LOG_DEBUGH(_T("MULTI_STREAM_SELECT_FIRST: commit first candidate"));
                        WORD_LATTICE->selectFirst();
                        break;
                    case HISTORY_FULL_CAND_DECKEY:
                        _LOG_DEBUGH(_T("HISTORY_FULL_CAND"));
                        if (!NextNodeMaybe()) {
                            SetNextNodeMaybe(HISTORY_NODE);
                        }
                        break;
                    case TOGGLE_ZENKAKU_CONVERSION_DECKEY:
                        _LOG_DEBUGH(_T("TOGGLE_ZENKAKU_CONVERSION"));
                        if (!NextNodeMaybe()) {
                            WORD_LATTICE->clear();
                            SetNextNodeMaybe(ZENKAKU_NODE);
                        }
                        break;
                    case EISU_MODE_TOGGLE_DECKEY:
                        _LOG_DEBUGH(_T("EISU_MODE_TOGGLE"));
                        if (!NextNodeMaybe()) {
                            WORD_LATTICE->clear();
                            EISU_NODE->blockerNeeded = true; // 入力済み末尾にブロッカーを設定する
                            EISU_NODE->eisuExitCapitalCharNum = 0;
                            SetNextNodeMaybe(EISU_NODE);
                        }
                        break;
                    case BUSHU_COMP_DECKEY:
                        _LOG_DEBUGH(_T("BUSHU_COMP"));
                        WORD_LATTICE->updateByBushuComp();
                        break;
                    default:
                        _LOG_DEBUGH(_T("OTHER"));
                        WORD_LATTICE->clear();
                        //MarkUnnecessary();
                        State::dispatchDeckey(deckey);
                        break;
                    }
                } else {
                    if (deckey >= COMBO_DECKEY_START && deckey < COMBO_DECKEY_END) {
                        // 同時打鍵の始まりなので、いったん streamList はクリア
                        // 同時打鍵中は、処理を分岐させない
                        _streamList1.Clear();
                        _streamList2.Clear();
                        _comboStrokeCount = 1;
                    }
                    if (SETTINGS->eisuModeEnabled && _comboStrokeCount == 0
                        && deckey >= SHIFT_DECKEY_START && deckey < SHIFT_DECKEY_END && !STATE_COMMON->IsUpperRomanGuideMode()) {
                        myChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
                        if (myChar >= 'A' && myChar <= 'Z') {
                            // 英数モード
                            LOG_DEBUGH(_T("SetNextNodeMaybe: Eisu"));
                            _streamList1.Clear();
                            _streamList2.Clear();
                            WORD_LATTICE->clear();
                            EISU_NODE->blockerNeeded = true; // 入力済み末尾にブロッカーを設定する
                            EISU_NODE->eisuExitCapitalCharNum = SETTINGS->eisuExitCapitalCharNum;
                            SetNextNodeMaybe(EISU_NODE);
                            return;
                        }
                    }
                    // 前処理(ストローク木状態の作成と呼び出し)
                    _LOG_DEBUGH(_T("streamList1: doDeckeyPreProc"));
                    _streamList1.HandleDeckeyProc(StrokeTableNode::RootStrokeNode1.get(), deckey, _comboStrokeCount);
                    _LOG_DEBUGH(_T("streamList2: doDeckeyPreProc"));
                    _streamList2.HandleDeckeyProc(StrokeTableNode::RootStrokeNode2.get(), deckey, _comboStrokeCount);
                    if (_comboStrokeCount > 0) ++_comboStrokeCount;     // 同時打鍵で始まった時だけ
                    if (deckey < SHIFT_DECKEY_START) {
                        // 同時打鍵列の終わり
                        _comboStrokeCount = 0;
                    }
                }
            }

            LOG_INFO(_T("LEAVE"));
        }

        // 新しい状態作成のチェイン
        void CreateNewStateChain() override {
            _LOG_DEBUGH(_T("ENTER: {}"), Name);
            _LOG_DEBUGH(_T("streamList1: CreateNewStates"));
            _streamList1.CreateNewStates();
            _LOG_DEBUGH(_T("streamList2: CreateNewStates"));
            _streamList2.CreateNewStates();
            State::CreateNewStateChain();
            _LOG_DEBUGH(_T("LEAVE: {}"), Name);
        }

        // 出力文字を取得する
        void GetResultStringChain(MStringResult& resultOut) override {
            _LOG_DEBUGH(_T("\nENTER: {}, resultStr={}"), Name, resultStr.debugString());

            _streamList1.DebugPrintStatesChain(_T("ENTER: streamList1"));
            _streamList2.DebugPrintStatesChain(_T("ENTER: streamList2"));

            STATE_COMMON->SetCurrentModeIsMultiStreamInput();

            if (resultStr.isModified()) {
                _LOG_DEBUGH(_T("resultStr={}"), resultStr.debugString());
                resultOut.setResult(resultStr);
            } else if (NextState()) {
                NextState()->GetResultStringChain(resultOut);
            } else {
                //if (IsUnnecessary()) {
                //    _LOG_DEBUGH(_T("LEAVE: {}"), Name);
                //    _LOG_DEBUGH(_T("ClearCurrentModeIsMultiStreamInput"));
                //    STATE_COMMON->ClearCurrentModeIsMultiStreamInput();
                //    return;
                //}

                // 単語素片の収集
                std::vector<WordPiece> pieces;
                if ((int)STATE_COMMON->GetTotalDecKeyCount() == _strokeCountBS) {
                    _LOG_DEBUGH(_T("add WordPiece for BS."));
                    pieces.push_back(WordPiece::BSPiece());
                } else {
                    _LOG_DEBUGH(_T("streamList1: AddWordPieces"));
                    _streamList1.AddWordPieces(pieces, false);
                    _LOG_DEBUGH(_T("streamList2: AddWordPieces"));
                    _streamList2.AddWordPieces(pieces, false);

                    Node* pNextNode1 = _streamList1.GetNonStringNode();
                    Node* pNextNode2 = _streamList2.GetNonStringNode();
                    if (pNextNode1 || pNextNode2) {
                        // 文字列ノード以外が返ったら、状態をクリアする
                        _LOG_DEBUGH(_T("NonStringNode FOUND: pNextNode1={:p}, pNextNode2={:p}"), (void*)pNextNode1, (void*)pNextNode2);
                        _streamList1.Clear();
                        _streamList2.Clear();
                        SetNextNodeMaybe(pNextNode1 ? pNextNode1 : pNextNode2);
                    }
#if 0
                    // TODO: 以下は不要のはず
                    if (_streamList1.Empty() && _streamList2.Empty()) {
                        // 全てのストローク状態が削除されたので、後処理してマージノードも削除
                        _LOG_DEBUGH(_T("REMOVE ALL"));
                        SetNextNodeMaybe(pNextNode1 ? pNextNode1 : pNextNode2);
                        //MarkUnnecessary();
                    }
#endif
                    if (!IsUnnecessary() && pieces.empty()) {
                        _LOG_DEBUGH(_T("pieces is empty. add EmptyWordPiece."));
                        pieces.push_back(WordPiece::emptyPiece());
                    }
                }

                // Lattice処理
                auto result = WORD_LATTICE->addPieces(pieces);

                // 新しい文字列が得られたらそれを返す
                if (!result.outStr.empty() || result.numBS > 0) {
                    resultOut.setResult(result.outStr, result.numBS);
                    SetTranslatedOutString(resultOut);
                } else {
                    _LOG_DEBUGH(_T("NO resultOut"));
                }
            }

            if (_streamList1.Empty() && _streamList2.Empty() && WORD_LATTICE->isEmpty()) {
                WORD_LATTICE->clear();
                //MarkUnnecessary();
                _LOG_DEBUGH(_T("ClearCurrentModeIsMultiStreamInput"));
                STATE_COMMON->ClearCurrentModeIsMultiStreamInput();
            }

            _streamList1.DebugPrintStatesChain(_T("LEAVE: streamList1"));
            _streamList2.DebugPrintStatesChain(_T("LEAVE: streamList2"));

            //if (_streamList1.Empty() && _streamList2.Empty()) {
            //    _LOG_DEBUGH(_T("ClearCurrentModeIsMultiStreamInput"));
            //    STATE_COMMON->ClearCurrentModeIsMultiStreamInput();
            //} else {
            //    _LOG_DEBUGH(_T("SetCurrentModeIsMultiStreamInput"));
            //    STATE_COMMON->SetCurrentModeIsMultiStreamInput();
            //}
            _LOG_DEBUGH(_T("LEAVE: {}: resultStr=[{}]\n"), Name, resultOut.debugString());
        }

        // チェーンをたどって不要とマークされた後続状態を削除する
        void DeleteUnnecessarySuccessorStateChain() override {
            _LOG_DEBUGH(_T("ENTER: {}, IsUnnecessary={}"), Name, IsUnnecessary());
            _LOG_DEBUGH(_T("streamList1: deleteUnnecessaryNextState"));
            _streamList1.DeleteUnnecessaryNextStates();
            _LOG_DEBUGH(_T("streamList2: deleteUnnecessaryNextState"));
            _streamList2.DeleteUnnecessaryNextStates();

            _streamList1.DebugPrintStatesChain(_T("LEAVE: streamList1"));
            _streamList2.DebugPrintStatesChain(_T("LEAVE: streamList2"));

            State::DeleteUnnecessarySuccessorStateChain();

            _LOG_DEBUGH(_T("LEAVE: {}, NextNode={}"), Name, NODE_NAME(NextNodeMaybe()));
        }

        // ストロークテーブルチェインの長さ(テーブルのレベル)
        size_t StrokeTableChainLength() const override {
            size_t len1 = _streamList1.StrokeTableChainLength();
            size_t len2 = _streamList2.StrokeTableChainLength();
            return len1 < len2 ? len2 : len1;
        }

        String JoinedName() const override {
            auto myName = Name + _T("(") + _streamList1.ChainLengthString() + _T("/") + _streamList2.ChainLengthString() + _T(")");
            if (NextState()) myName += _T("-") + NextState()->JoinedName();
            return myName;
        }

        // -------------------------------------------------------------------
        // 以下、履歴機能
    private:
        int candSelectDeckey = -1;

        /// 今回の履歴候補選択ホットキーを保存
        /// これにより、DoLastHistoryProc() で継続的な候補選択のほうに処理が倒れる
        void setCandSelectIsCalled() { candSelectDeckey = STATE_COMMON->GetDeckey(); }

        // 状態管理のほうで記録している最新ホットキーと比較し、今回が履歴候補選択キーだったか
        bool wasCandSelectCalled() { return candSelectDeckey >= 0 && candSelectDeckey == STATE_COMMON->GetDeckey(); }

        // 呼び出されたのは編集用キーか
        bool isEditingFuncDecKey() {
            int deckey = STATE_COMMON->GetDeckey();
            return deckey >= HOME_DECKEY && deckey <= RIGHT_ARROW_DECKEY;
        }

        // 呼び出されたのはENTERキーか
        bool isEnterDecKey() {
            return STATE_COMMON->GetDeckey() == ENTER_DECKEY;
        }

        // 後続状態で出力スタックが変更された可能性あり
        bool maybeEditedBySubState = false;

        // Shift+Space等による候補選択が可能か
        bool bCandSelectable = false;

    public:
        // 状態の再アクティブ化
        void Reactivate() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            if (NextState()) NextState()->Reactivate();
            // ちょっと以下の意図が不明
            //maybeEditedBySubState = true;
            //DoLastHistoryProc();
            // 初期化という意味で、下記のように変更しておく(2021/5/31)
            maybeEditedBySubState = false;
            bCandSelectable = false;
            _LOG_DEBUGH(_T("bCandSelectable=False"));
            resultStr.clear();
            STROKE_MERGER_NODE->ClearPrevHistState();     // まだ履歴検索が行われていないということを表す
            HIST_CAND->ClearKeyInfo();      // まだ履歴検索が行われていないということを表す
        }

        // 履歴検索を初期化する状態か
        bool IsHistoryReset() override {
            bool result = (NextState() && NextState()->IsHistoryReset());
            _LOG_DEBUGH(_T("CALLED: {}: result={}"), Name, result);
            return result;
        }

    public:
        // Enter時の新しい履歴の追加
        void AddNewHistEntryOnEnter() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            if (HISTORY_DIC) {
                HIST_CAND->DelayedPushFrontSelectedWord();
                STATE_COMMON->SetBothHistoryBlockFlag();
                _LOG_DEBUGH(_T("SetBothHistoryBlocker"));
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
        void AddNewHistEntryOnSomeChar() override {
            //auto ch1 = STATE_COMMON->GetFirstOutChar();
            auto ch1 = utils::safe_front(resultStr.resultStr());
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
        void SetTranslatedOutString(const MStringResult& result) {
            SetTranslatedOutString(result.resultStr(), result.rewritableLen(), result.isBushuComp(), result.numBS());
        }

        // 文字列を変換して出力、その後、履歴の追加
        void SetTranslatedOutString(const MString& outStr, size_t rewritableLen, bool bBushuComp = true, int numBS = -1) override {
            _LOG_DEBUGH(_T("ENTER: {}: outStr={}, rewritableLen={}, bushuComp={}, numBS={}"), Name, to_wstr(outStr), rewritableLen, bBushuComp, numBS);
            MString xlatStr;
            if (NextState()) {
                // Katakana など
                _LOG_DEBUGH(_T("Call TranslateString of NextStates={}"), JoinedName());
                xlatStr = NextState()->TranslateString(outStr);
            }
            if (!xlatStr.empty() && xlatStr != outStr) {
                resultStr.setResultWithRewriteLen(xlatStr, xlatStr == outStr ? rewritableLen : 0, numBS);
            } else {
                resultStr.clear();
                if (bBushuComp && SETTINGS->autoBushuCompMinCount > 0) {
                    // 自動部首合成
                    _LOG_DEBUGH(_T("Call AutoBushu"));
                    BUSHU_COMP_NODE->ReduceByAutoBushu(outStr, resultStr);
                }
                if (!resultStr.isModified()) {
                    _LOG_DEBUGH(_T("Set outStr"));
                    resultStr.setResultWithRewriteLen(outStr, rewritableLen, numBS);
                }
            }
            AddNewHistEntryOnSomeChar();
            _LOG_DEBUGH(_T("LEAVE: {}: resultStr={}, numBS={}"), Name, to_wstr(resultStr.resultStr()), resultStr.numBS());
        }

        void handleFullEscapeResidentState() override {
            handleFullEscape();
        }

        // 先頭文字の小文字化
        void handleEisuDecapitalize() override {
            _LOG_DEBUGH(_T("ENTER: {}"), Name);
            auto romanStr = OUTPUT_STACK->GetLastAsciiKey<MString>(SETTINGS->histMapKeyMaxLength + 1);
            if (!romanStr.empty() && romanStr.size() <= SETTINGS->histMapKeyMaxLength) {
                if (is_upper_alphabet(romanStr[0])) {
                    romanStr[0] = to_lower(romanStr[0]);
                    resultStr.setResult(romanStr, romanStr.size());
                }
            }
            _LOG_DEBUGH(_T("LEAVE: {}"), Name);
        }

        // CommitState の処理 -- 処理のコミット
        void handleCommitState() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            commitHistory();
        }

        void commitHistory() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            // 候補が選択されていれば、それを使用履歴の先頭にpushする
            HIST_CAND->DelayedPushFrontSelectedWord();
            // どれかの候補が選択されている状態なら、それを確定し、履歴キーをクリアしておく
            STROKE_MERGER_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();
        }

    protected:
        // 履歴常駐状態の事前チェック
        int HandleDeckeyPreProc(int deckey) override {
            _LOG_DEBUGH(_T("ENTER: {}"), Name);
            resultStr.clear();
            deckey = ModalStateUtil::ModalStatePreProc(this, deckey,
                State::isStrokableKey(deckey) && (!bCandSelectable || deckey >= 10 || !SETTINGS->selectHistCandByNumberKey));
            maybeEditedBySubState = false;
            // 常駐モード
            //if (pNext && pNext->GetName().find(_T("History")) == String::npos)
            if (IsHistoryReset()) {
                // 履歴機能ではない次状態(StrokeStateなど)があれば、それが何かをしているはずなので、戻ってきたら新たに候補の再取得を行うために、ここで maybeEditedBySubState を true にセットしておく
                //prevKey.clear();
                _LOG_DEBUGH(_T("Set Reinitialized=true"));
                maybeEditedBySubState = true;
                STROKE_MERGER_NODE->ClearPrevHistState();    // まだ履歴検索が行われていない状態にしておく
                HIST_CAND->ClearKeyInfo();      // まだ履歴検索が行われていないということを表す
            }
            _LOG_DEBUGH(_T("LEAVE: {}"), Name);

            return deckey;
        }

        //// ノードから生成した状態を後接させ、その状態を常駐させる(ここでは 0 が渡ってくるはず)
        //void ChainAndStayResident(Node* ) {
        //    // 前状態にチェインする
        //    LOG_DEBUG(_T("Chain: {}"), Name);
        //    STATE_COMMON->ChainMe();
        //}

    private:
        bool matchWildcardKey(const MString& cand, const MString& wildKey) {
            _LOG_DEBUGH(_T("cand={}, wildKey={}"), to_wstr(cand), to_wstr(wildKey));
            auto keys = utils::split(wildKey, '*');
            if (keys.size() == 2) {
                const MString& key0 = keys[0];
                const MString& key1 = keys[1];
                if (!key0.empty() && !key1.empty()) {
                    if (cand.size() >= key0.size() + key1.size()) {
                        // wildcard key なので、'|' の語尾は気にしなくてよい('|' を含むやつにはマッチさせないので)
                        _LOG_DEBUGH(_T("startsWithWildKey({}, {}, 0) && utils::endsWithWildKey({}, {})"), to_wstr(cand), to_wstr(key0), to_wstr(cand), to_wstr(key1));
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
            _LOG_DEBUGH(_T("ENTER: prevKey={}, cands.size={}, cand0={}, cand1={}, gobiLen={}"), to_wstr(prevKey), candsSize, to_wstr(cand0), to_wstr(cand1), gobiLen);

            bool result = (!prevKey.empty() &&
                           ((!cand0.empty() && (utils::startsWithWildKey(cand0, prevKey, gobiLen) || matchWildcardKey(cand0, prevKey)) && cand0 != prevKey) ||
                            (!cand1.empty() && (utils::startsWithWildKey(cand1, prevKey, gobiLen) || matchWildcardKey(cand1, prevKey)) && cand1 != prevKey)));

            _LOG_DEBUGH(_T("LEAVE: result={}"), result);
            return result;
        }

        // 一時的にこのフラグを立てることにより、履歴検索を行わないようになる
        bool bNoHistTemporary = false;

        // 一時的にこのフラグを立てることにより、自動モードでなくても履歴検索が実行されるようになる
        bool bManualTemporary = false;

        // 前回の履歴検索との比較、新しい履歴検索の開始 (bManual=trueなら自動モードでなくても履歴検索を実行する)
        void historySearch(bool bManual) {
            LOG_DEBUGH(_T("ENTER: auto={}, manual={}, maybeEditedBySubState={}, histInSearch={}"), \
                SETTINGS->autoHistSearchEnabled, bManual, maybeEditedBySubState, HIST_CAND->IsHistInSearch());
            if (!SETTINGS->autoHistSearchEnabled && !bManual) {
                // 履歴検索状態ではないので、前回キーをクリアしておく。
                // こうしておかないと、自動履歴検索OFFのとき、たとえば、
                // 「エッ」⇒Ctrl+Space⇒「エッセンス」⇒Esc⇒「エッ」⇒「セ」追加⇒出力「エッセ」、キー=「エッ」のまま⇒再検索⇒「エッセセンス」となる
                _LOG_DEBUGH(_T("Not Hist Search mode: Clear PrevKey"));
                STROKE_MERGER_NODE->ClearPrevHistState();
                HIST_CAND->ClearKeyInfo();
            } else {
                // 履歴検索可能状態である
                _LOG_DEBUGH(_T("Auto or Manual"));
                // 前回の履歴選択の出力と現在の出力文字列(改行以降)の末尾を比較する。
                // たとえば前回「中」で履歴検索し「中納言家持」が履歴出力されており、現在の出力スタックが「・・・中納言家持」なら true が返る
                bool bSameOut = !bManual && histBase->isLastHistOutSameAsCurrentOut();
                LOG_DEBUGH(_T("bSameOut={}, maybeEditedBySubState={}, histInSearch={}"), \
                    bSameOut, maybeEditedBySubState, HIST_CAND->IsHistInSearch());
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
                        _LOG_DEBUGH(_T("HistSearch: key9={}"), to_wstr(key9));
                        if (key9.empty() || key9.back() == ' ') {
                            return EMPTY_MSTR;
                        }
                        auto items = utils::split(key9, '*');
                        size_t nItems = items.size();
                        if (nItems >= 2) {
                            size_t len0 = items[nItems - 2].size();
                            size_t len1 = items[nItems - 1].size();
                            if (len0 > 0 && len1 > 0 && len1 <= 4) {
                                _LOG_DEBUGH(_T("WILDCARD: key={}"), to_wstr(utils::last_substr(key9, len1 + 5)));
                                return utils::last_substr(key9, len1 + 5);
                            }
                        }
                        // ワイルドカードパターンでなかった
                        _LOG_DEBUGH(_T("NOT WILDCARD, GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey"));
                        // 出力文字から、ひらがな交じりやASCIIもキーとして取得する
                        auto jaKey = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>(SETTINGS->histMapKeyMaxLength);
                        _LOG_DEBUGH(_T("HistSearch: jaKey={}"), to_wstr(jaKey));
                        if (jaKey.size() >= 9 || (!jaKey.empty() && is_ascii_char(jaKey.back()))) {
                            // 同種の文字列で9文以上取れたか、またはASCIIだったので、これをキーとする
                            return jaKey;
                        }
                        // 最終的には末尾8文字をキーとする('*' は含まない。'?' は含んでいる可能性あり)
                        _LOG_DEBUGH(_T("HistSearch: tail_substr(key9, 8)={}"), to_wstr(utils::tail_substr(key9, 8)));
                        return utils::tail_substr(key9, 8);
                    };
                    // キーの取得
                    MString key = keyGetter();
                    _LOG_DEBUGH(_T("HistSearch: LastJapaneseKey={}"), to_wstr(key));
                    if (!key.empty() && key.find(MSTR_CMD_HEADER) > key.size()) {
                        // キーが取得できた
                        //bool isAscii = is_ascii_char((wchar_t)utils::safe_back(key));
                        _LOG_DEBUGH(_T("HistSearch: PATH 8: key={}, prevKey={}, maybeEditedBySubState={}"),
                            to_wstr(key), to_wstr(STROKE_MERGER_NODE->GetPrevKey()), maybeEditedBySubState);
                        auto histCandsChecker = [this](const std::vector<MString>& words, const MString& ky) {
                            _LOG_DEBUGH(_T("HistSearch: CANDS CHECKER: words.size()={}, key={}"), words.size(), to_wstr(ky));
                            if (words.empty() || (words.size() == 1 && (words[0].empty() || words[0] == ky))) {
                                _LOG_DEBUGH(_T("HistSearch: CANDS CHECKER-A: cands size <= 1"));
                                // 候補が1つだけで、keyに一致するときは履歴選択状態にはしない
                            } else {
                                _LOG_DEBUGH(_T("HistSearch: CANDS CHECKER-B"));
                                if (SETTINGS->autoHistSearchEnabled || SETTINGS->showHistCandsFromFirst) {
                                    // 初回の履歴選択でも横列候補表示を行う
                                    histBase->setCandidatesVKB(VkbLayout::Horizontal, words, ky);
                                }
                            }
                        };
                        if (key != STROKE_MERGER_NODE->GetPrevKey() || maybeEditedBySubState || bManual) {
                            _LOG_DEBUGH(_T("HistSearch: PATH 9: different key"));
                            //bool bCheckMinKeyLen = !bManual && utils::is_hiragana(key[0]);       // 自動検索かつ、キー先頭がひらがなならキー長チェックをやる
                            bool bCheckMinKeyLen = !bManual;                                     // 自動検索ならキー長チェックをやる
                            histCandsChecker(HIST_CAND->GetCandWords(key, bCheckMinKeyLen, 0), key);
                            // キーが短くなる可能性があるので再取得
                            key = HIST_CAND->GetCurrentKey();
                            _LOG_DEBUGH(_T("HistSearch: PATH 10: currentKey={}"), to_wstr(key));
                        } else {
                            // 前回の履歴検索と同じキーだった
                            _LOG_DEBUGH(_T("HistSearch: PATH 11: Same as prev hist key"));
                            histCandsChecker(HIST_CAND->GetCandWords(), key);
                        }
                    }
                    _LOG_DEBUGH(_T("HistSearch: SetPrevHistKeyState(key={})"), to_wstr(key));
                    STROKE_MERGER_NODE->SetPrevHistKeyState(key);
                    _LOG_DEBUGH(_T("DONE HistSearch"));
                }
            }

            // この処理は、GUI側で候補の背景色を変更するために必要
            if (isHotCandidateReady(STROKE_MERGER_NODE->GetPrevKey(), HIST_CAND->GetCandWords())) {
                _LOG_DEBUGH(_T("PATH 14"));
                // 何がしかの文字出力があり、それをキーとする履歴候補があった場合 -- 未選択状態にセットする
                _LOG_DEBUGH(_T("Set Unselected"));
                STATE_COMMON->SetWaitingCandSelect(-1);
                bCandSelectable = true;
                _LOG_DEBUGH(_T("bCandSelectable=True"));
            }
            maybeEditedBySubState = false;

            LOG_DEBUGH(_T("LEAVE"));
        }

    public:
        // 最終的な出力履歴が整ったところで呼び出される処理
        void DoLastHistoryProc() override {
            LOG_DEBUGH(_T("\nENTER: {}: {}"), Name, OUTPUT_STACK->OutputStackBackStrForDebug(10));
            LOG_DEBUGH(_T("PATH 2: bCandSelectable={}"), bCandSelectable);

            if (bCandSelectable && wasCandSelectCalled()) {
                LOG_DEBUGH(_T("PATH 3: by SelectionKey"));
                // 履歴選択キーによる処理だった場合
                if (isEnterDecKey()) {
                    LOG_DEBUGH(_T("PATH 4-A: handleEnter"));
                    bCandSelectable = false;
                    OUTPUT_STACK->pushNewLine();
                } else {
                    LOG_DEBUGH(_T("PATH 4-B: handleArrow"));
                    // この処理は、GUI側で候補の背景色を変更するのと矢印キーをホットキーにするために必要
                    LOG_DEBUGH(_T("Set selectedPos={}"), HIST_CAND->GetSelectPos());
                    STATE_COMMON->SetWaitingCandSelect(HIST_CAND->GetSelectPos());
                }
            } else {
                LOG_DEBUGH(_T("PATH 5: by Other Input"));
                // その他の文字出力だった場合
                HIST_CAND->DelayedPushFrontSelectedWord();
                bCandSelectable = false;

                LOG_DEBUGH(_T("PATH 6: bCandSelectable={}, bNoHistTemporary={}"), bCandSelectable, bNoHistTemporary);
                if (OUTPUT_STACK->isLastOutputStackCharBlocker()) {
                    LOG_DEBUGH(_T("PATH 7: LastOutputStackChar is Blocker"));
                    HISTORY_DIC->ClearNgramSet();
                }

                // 前回の履歴検索との比較、新しい履歴検索の開始
                if (bNoHistTemporary) {
                    // 一時的に履歴検索が不可になっている場合は、キーと出力文字列を比較して、異った状態になっていたら可に戻す
                    MString prevKey = STROKE_MERGER_NODE->GetPrevKey();
                    MString outStr = OUTPUT_STACK->GetLastOutputStackStrUptoBlocker(prevKey.size());
                    bNoHistTemporary = OUTPUT_STACK->GetLastOutputStackStrUptoBlocker(prevKey.size()) == prevKey;
                    LOG_DEBUGH(_T("PATH 8: bNoHistTemporary={}: prevKey={}, outStr={}"), bNoHistTemporary, to_wstr(prevKey), to_wstr(outStr));
                }

                LOG_DEBUGH(_T("PATH 9: bNoHistTemporary={}"), bNoHistTemporary);
                if (!bNoHistTemporary) {
                    if (isEditingFuncDecKey()) {
                        // 編集用キーが呼び出されたので、全ブロッカーを置く
                        OUTPUT_STACK->pushNewLine();
                    } else {
                        historySearch(bManualTemporary);
                    }
                }
                //bNoHistTemporary = false;
                //bManualTemporary = false;
            }

            bNoHistTemporary = false;
            bManualTemporary = false;

            LOG_DEBUGH(_T("LEAVE: {}\n"), Name);
        }

        // (Ctrl or Shift)+Space の処理 -- 履歴検索の開始、次の候補を返す
        void handleNextOrPrevCandTrigger(bool bNext) {
            LOG_DEBUGH(_T("\nCALLED: {}: bCandSelectable={}, selectPos={}, bNext={}"), Name, bCandSelectable, HIST_CAND->GetSelectPos(), bNext);
            // これにより、前回のEnterによる改行点挿入やFullEscapeによるブロッカーフラグが削除される⇒(2021/12/18)workしなくなっていたので、いったん削除
            //OUTPUT_STACK->clearFlagAndPopNewLine();
            // 今回、履歴選択用ホットキーだったことを保存
            setCandSelectIsCalled();

            // 自動履歴検索が有効になっているか、初回から履歴候補の横列表示をするか、または2回目以降の履歴検索の場合は、履歴候補の横列表示あり
            bool bShowHistCands = SETTINGS->autoHistSearchEnabled || SETTINGS->showHistCandsFromFirst || bCandSelectable;

            if (!bCandSelectable) {
                // 履歴候補選択可能状態でなければ、前回の履歴検索との比較、新しい履歴検索の開始
                historySearch(true);
            }
            if (bCandSelectable) {
                LOG_DEBUGH(_T("CandSelectable: bNext={}"), bNext);
                if (bNext)
                    getNextCandidate(bShowHistCands);
                else
                    getPrevCandidate(bShowHistCands);
            } else {
                LOG_DEBUGH(_T("NOP"));
            }
            LOG_DEBUGH(_T("LEAVE\n"));
        }

        // 0～9 を処理する
        void handleStrokeKeys(int deckey) {
            _LOG_DEBUGH(_T("ENTER: {}: deckey={}, bCandSelectable={}"), Name, deckey, bCandSelectable);
            if (bCandSelectable) {
                setCandSelectIsCalled();
                getPosCandidate((size_t)deckey);
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        //// Shift+Space の処理 -- 履歴検索の開始、次の候補を返す
        //void handleShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleNextOrPrevCandTrigger(true);
        //}

        //// Ctrl+Space の処理 -- 履歴検索の開始、次の候補を返す
        //void handleCtrlSpace() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleNextOrPrevCandTrigger(true);
        //}

        //// Ctrl+Shift+Space の処理 -- 履歴検索の開始、前の候補を返す
        //void handleCtrlShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleNextOrPrevCandTrigger(false);
        //}

        // NextCandTrigger の処理 -- 履歴検索の開始、次の候補を返す
        void handleNextCandTrigger() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleNextOrPrevCandTrigger(true);
        }

        // PrevCandTrigger の処理 -- 履歴検索の開始、前の候補を返す
        void handlePrevCandTrigger() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleNextOrPrevCandTrigger(false);
        }

        // ↓の処理 -- 次候補を返す
        void handleDownArrow() override {
            _LOG_DEBUGH(_T("ENTER: {}: bCandSelectable={}"), Name, bCandSelectable);
            if (SETTINGS->useArrowToSelCand && bCandSelectable) {
                setCandSelectIsCalled();
                getNextCandidate();
            } else {
                _LOG_DEBUGH(_T("candSelectDeckey={:x}"), candSelectDeckey);
                State::handleDownArrow();
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // ↑の処理 -- 前候補を返す
        void handleUpArrow() override {
            _LOG_DEBUGH(_T("ENTER: {}: bCandSelectable={}"), Name, bCandSelectable);
            if (SETTINGS->useArrowToSelCand && bCandSelectable) {
                setCandSelectIsCalled();
                getPrevCandidate();
            } else {
                _LOG_DEBUGH(_T("candSelectDeckey={:x}"), candSelectDeckey);
                State::handleUpArrow();
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // FullEscapeの処理 -- 履歴選択状態から抜けて、履歴検索文字列の遡及ブロッカーをセット
        void handleFullEscape() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            HIST_CAND->DelayedPushFrontSelectedWord();
            histBase->setBlocker();
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // Unblock の処理 -- 改行やブロッカーの除去
        void handleUnblock() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            // ブロッカー設定済みならそれを解除する
            OUTPUT_STACK->clearFlagAndPopNewLine();
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // Tab の処理 -- 次の候補を返す
        void handleTab() override {
            _LOG_DEBUGH(_T("CALLED: {}: bCandSelectable={}"), Name, bCandSelectable);
            if (SETTINGS->selectHistCandByTab && bCandSelectable) {
                setCandSelectIsCalled();
                getNextCandidate();
            } else {
                State::handleTab();
            }
        }

        // ShiftTab の処理 -- 前の候補を返す
        void handleShiftTab() override {
            _LOG_DEBUGH(_T("CALLED: {}: bCandSelectable={}"), Name, bCandSelectable);
            if (SETTINGS->selectHistCandByTab && bCandSelectable) {
                setCandSelectIsCalled();
                getPrevCandidate();
            } else {
                State::handleShiftTab();
            }
        }

        // Ctrl-H/BS の処理 -- 履歴検索の初期化
        void handleBS() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            STROKE_MERGER_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();
            if (WORD_LATTICE->isEmpty()) {
                State::handleBS();
            }
        }

        // DecoderOff の処理
        void handleDecoderOff() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            // Enter と同じ扱いにする
            AddNewHistEntryOnEnter();
            State::handleDecoderOff();
        }
        
        // RET/Enter の処理
        void handleEnter() override {
            _LOG_DEBUGH(_T("ENTER: {}: bCandSelectable={}, selectPos={}"), Name, bCandSelectable, HIST_CAND->GetSelectPos());
            if (SETTINGS->selectFirstCandByEnter && bCandSelectable && HIST_CAND->GetSelectPos() < 0) {
                // 選択可能状態かつ候補未選択なら第1候補を返す。
                _LOG_DEBUGH(_T("CALL: getNextCandidate(false)"));
                setCandSelectIsCalled();
                getNextCandidate(false);
            } else if (bCandSelectable && HIST_CAND->GetSelectPos() >= 0) {
                _LOG_DEBUGH(_T("CALL: STROKE_MERGER_NODE->ClearPrevHistState(); HIST_CAND->ClearKeyInfo(); bManualTemporary = true"));
                // どれかの候補が選択されている状態なら、それを確定し、履歴キーをクリアしておく
                STROKE_MERGER_NODE->ClearPrevHistState();
                HIST_CAND->ClearKeyInfo();
                // 一時的にマニュアル操作フラグを立てることで、DoLastHistoryProc() から historySearch() を呼ぶときに履歴再検索が実行されるようにする
                bManualTemporary = true;
                if (SETTINGS->newLineWhenHistEnter) {
                    // 履歴候補選択時のEnterではつねに改行するなら、確定後、Enter処理を行う
                    State::handleEnter();
                }
            } else {
                // それ以外は通常のEnter処理
                _LOG_DEBUGH(_T("CALL: AddNewHistEntryOnEnter()"));
                AddNewHistEntryOnEnter();
                State::handleEnter();
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        //// Ctrl-J の処理 -- 選択可能状態かつ候補未選択なら第1候補を返す。候補選択済みなら確定扱い
        //void handleCtrlJ() {
        //    _LOG_DEBUGH(_T("\nCALLED: {}: selectPos={}"), Name, HIST_CAND->GetSelectPos());
        //    //setCandSelectIsCalled();
        //    if (bCandSelectable) {
        //        if (HIST_CAND->GetSelectPos() < 0) {
        //            // 選択可能状態かつ候補未選択なら第1候補を返す。
        //            getNextCandidate();
        //        } else {
        //            // 確定させる
        //            HIST_CAND->DelayedPushFrontSelectedWord();
        //            histBase->setBlocker();
        //        }
        //    } else {
        //        // Enterと同じ扱い
        //        AddNewHistEntryOnEnter();
        //        State::handleCtrlJ();
        //    }
        //}

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() override {
            _LOG_DEBUGH(_T("CALLED: {}, bCandSelectable={}, SelectPos={}, EisuPrevCount={}, TotalCount={}"),
                Name, bCandSelectable, HIST_CAND->GetSelectPos(), EISU_NODE->prevHistSearchDeckeyCount, STATE_COMMON->GetTotalDecKeyCount());
            if (bCandSelectable && HIST_CAND->GetSelectPos() >= 0) {
                _LOG_DEBUGH(_T("Some Cand Selected"));
                // どれかの候補が選択されている状態なら、選択のリセット
                if (STATE_COMMON->GetTotalDecKeyCount() == EISU_NODE->prevHistSearchDeckeyCount + 1) {
                    // 直前に英数モードから履歴検索された場合
                    _LOG_DEBUGH(_T("SetNextNode: EISU_NODE"));
                    resetCandSelect(false);     // false: 仮想鍵盤表示を履歴選択モードにしない
                    // 一時的にこのフラグを立てることにより、履歴検索を行わないようにする
                    bNoHistTemporary = true;
                    // 再度、英数モード状態に入る
                    SetNextNodeMaybe(EISU_NODE);
                    //STATE_COMMON->SetNormalVkbLayout();
                } else {
                    resetCandSelect(true);
                    // 一時的にマニュアル操作フラグを立てることで、DoLastHistoryProc() から historySearch() を呼ぶときに履歴再検索が実行されるようにする
                    bManualTemporary = true;
                }
            } else {
                _LOG_DEBUGH(_T("No Cand Selected"));
                // 一時的にこのフラグを立てることにより、履歴検索を行わないようにする
                bNoHistTemporary = true;
                // Esc処理が必要なものがあればそれをやる。なければアクティブウィンドウにEscを送る
                ResidentState::handleEsc();
                //// 何も候補が選択されていない状態なら履歴選択状態から抜ける
                //STATE_COMMON->SetHistoryBlockFlag();
                //State::handleEsc();
                //// 完全に抜ける
                //handleFullEscape();
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        //// Ctrl-U
        //void handleCtrlU() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    STATE_COMMON->SetBothHistoryBlockFlag();
        //    State::handleCtrlU();
        //}

    private:
        // 次の候補を返す処理
        void getNextCandidate(bool bSetVkb = true) {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            outputHistResult(HIST_CAND->GetNext(), bSetVkb);
        }

        // 前の候補を返す処理
        void getPrevCandidate(bool bSetVkb = true) {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            outputHistResult(HIST_CAND->GetPrev(), bSetVkb);
        }

        // 次の候補を返す処理
        void getPosCandidate(size_t pos, bool bSetVkb = true) {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            outputHistResult(HIST_CAND->GetPositionedHist(pos), bSetVkb);
        }

        // 選択のリセット
        void resetCandSelect(bool bSetVkb) {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            outputHistResult(HIST_CAND->ClearSelectPos(), bSetVkb);
            STATE_COMMON->SetWaitingCandSelect(-1);
        }

        // 履歴結果出力 (bSetVKb = false なら、仮想鍵盤表示を履歴選択モードにしない; 英数モードから履歴検索をした直後のESCのケース)
        void outputHistResult(const HistResult& result, bool bSetVkb) {
            _LOG_DEBUGH(_T("ENTER: {}: bSetVkb={}"), Name, bSetVkb);
            histBase->getLastHistKeyAndRewindOutput(resultStr);    // 前回の履歴検索キー取得と出力スタックの巻き戻し予約(numBackSpacesに値をセット)

            histBase->setOutString(result, resultStr);
            if (!result.Word.empty() && (result.Word.find(VERT_BAR) == MString::npos || utils::contains_ascii(result.Word))) {
                // 何か履歴候補(英数字を含まない変換形履歴以外)が選択されたら、ブロッカーを設定する (emptyの場合は元に戻ったので、ブロッカーを設定しない)
                _LOG_DEBUGH(_T("SetHistoryBlocker"));
                STATE_COMMON->SetHistoryBlockFlag();
            }
            if (bSetVkb) histBase->setCandidatesVKB(VkbLayout::Horizontal, HIST_CAND->GetCandWords(), HIST_CAND->GetCurrentKey());

            // 英数モードはキャンセルする
            if (NextState()) NextState()->handleEisuCancel();

            _LOG_DEBUGH(_T("LEAVE: prevOut={}, numBS={}"), to_wstr(STROKE_MERGER_NODE->GetPrevOutString()), resultStr.numBS());
        }

    };
    DEFINE_CLASS_LOGGER(StrokeMergerHistoryResidentStateImpl);

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

// 履歴入力(常駐)機能状態インスタンスの Singleton
std::unique_ptr<StrokeMergerHistoryResidentState> StrokeMergerHistoryResidentState::_singleton;

void StrokeMergerHistoryResidentState::SetSingleton(StrokeMergerHistoryResidentState* pState) {
    _singleton.reset(pState);
}

StrokeMergerHistoryResidentState* StrokeMergerHistoryResidentState::Singleton() {
    return _singleton.get();
}

// -------------------------------------------------------------------
// StrokeMergerHistoryNode - マージ履歴機能 常駐ノード
DEFINE_CLASS_LOGGER(StrokeMergerHistoryNode);

// コンストラクタ
StrokeMergerHistoryNode::StrokeMergerHistoryNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
StrokeMergerHistoryNode::~StrokeMergerHistoryNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// StrokeMergerNode::Singleton
std::unique_ptr<StrokeMergerHistoryNode> StrokeMergerHistoryNode::Singleton;

void StrokeMergerHistoryNode::CreateSingleton() {
    Singleton.reset(new StrokeMergerHistoryNode());
}

// マージ履歴機能常駐ノードの初期化
void StrokeMergerHistoryNode::Initialize() {
    // 履歴入力辞書ファイル名
    auto histFile = SETTINGS->historyFile;
    LOG_DEBUGH(_T("histFile={}"), histFile);
    // 履歴入力辞書の読み込み(ファイル名の指定がなくても辞書自体は構築する)
    LOG_DEBUGH(_T("CALLED: histFile={}"), histFile);
    HistoryDic::CreateHistoryDic(histFile);

    HistCandidates::CreateSingleton();

    FunctionNodeManager::CreateFunctionNodeByName(_T("history"));

    StrokeMergerHistoryNode::CreateSingleton();
}

// -------------------------------------------------------------------
// 当ノードを処理する State インスタンスを作成する
State* StrokeMergerHistoryNode::CreateState() {
    StrokeMergerHistoryResidentState::SetSingleton(new StrokeMergerHistoryResidentStateImpl(this));
    return MERGER_HISTORY_RESIDENT_STATE;
}

// テーブルファイルを読み込んでストローク木を作成する
void StrokeMergerHistoryNode::createStrokeTrees(bool bForceSecondary) {
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

