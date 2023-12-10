// StrokeTable を処理するデコーダ状態のクラス
#include "Logger.h"
#include "misc_utils.h"
#include "Constants.h"

#include "OneShot/PostRewriteOneShot.h"

#include "State.h"
#include "StrokeTable.h"
#include "VkbTableMaker.h"
#include "Settings.h"
#include "KeysAndChars/MyPrevChar.h"
#include "KeysAndChars/Eisu.h"

#include "DeckeyToChars.h"
#include "History/HistoryResidentState.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughStrokeTable)

#if 1 || defined(_DEBUG)
#undef LOG_DEBUGH
#undef _LOG_DEBUGH
#define LOG_DEBUGH LOG_INFO
#define _LOG_DEBUGH LOG_INFO
#endif

namespace {
    // -------------------------------------------------------------------
    // ストロークテーブル状態クラス
    class StrokeTableState : public State {
    private:
        DECLARE_CLASS_LOGGER;

        // 全打鍵状態を削除するか (checkNextState()により前状態に伝播)
        bool bRemoveAllStroke = false;

    protected:
        inline StrokeTableNode* myNode() const { return (StrokeTableNode*)pNode; }

        // 全打鍵状態を削除する
        void setToRemoveAllStroke() {
            MarkUnnecessary();
            bRemoveAllStroke = true;
        }

        // 全打鍵状態を削除するか
        bool isToRemoveAllStroke() {
            return bRemoveAllStroke;
        }

        int origDeckey = -1;
        //wchar_t shiftedOrigChar = 0;

        // ルートキーは UNSHIFT されているか
        //virtual bool IsRootKeyUnshifted() {
        //    auto p = dynamic_cast<StrokeTableState*>(PrevState());
        //    return p != nullptr ? p->IsRootKeyUnshifted() : false;
        //}

        // ルートキーは平仮名化されているか
        virtual bool IsRootKeyHiraganaized() {
            auto p = dynamic_cast<StrokeTableState*>(PrevState());
            return p != nullptr ? p->IsRootKeyHiraganaized() : false;
        }

        // ルートキーは同時打鍵キーか
        virtual bool IsRootKeyCombination() {
            auto p = dynamic_cast<StrokeTableState*>(PrevState());
            return p != nullptr ? p->IsRootKeyCombination() : false;
        }

    public:
        // コンストラクタ
        StrokeTableState(StrokeTableNode* pN) {
            LOG_INFO(_T("CALLED: ctor"));
            Initialize(logger.ClassNameT(), pN);
        }

        // DECKEY 処理の流れ
        void HandleDeckeyChain(int deckey) override {
            LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({}), totalCount={}, NextNode={}, outStr={}"),
                Name, deckey, deckey, STATE_COMMON->GetTotalDecKeyCount(), NODE_NAME(NextNodeMaybe()), to_wstr(STATE_COMMON->OutString()));
            // 前処理
            State::HandleDeckeyChain(deckey);   // ここで dispatchDeckey() → handleStrokeKeys() が呼ばれる

            // 次状態の処理
            checkNextState();

            // 不要になった後続状態を削除
            DeleteUnnecessarySuccessorState();

            LOG_DEBUGH(_T("LEAVE: {}, NextNode={}, outStr={}"), Name, NODE_NAME(NextNodeMaybe()), to_wstr(STATE_COMMON->OutString()));
        }

#define DEPTH           (myNode() ? myNode()->depth(): -1)
#define NEXT_NODE(n)    (myNode() ? myNode()->getNth(n) : 0)
#define NUM_CHILDREN    (myNode() ? myNode()->numChildren() : 0)

        // StrokeTableNode を処理する
        void handleStrokeKeys(int deckey) {
            bool isRootCombo = IsRootKeyCombination();
            wchar_t myChar = DECKEY_TO_CHARS->GetCharFromDeckey(origDeckey >= 0 ? origDeckey : deckey);
            LOG_DEBUGH(_T("ENTER: {}: origDeckey={:x}H({}), deckey={:x}H({}), face={}, isRootCombo={}, nodeDepth={}"), Name, origDeckey, origDeckey, deckey, deckey, myChar, isRootCombo, DEPTH);
            if (!isRootCombo) {
                // RootStrokeTableState が作成されたときに OrigString はクリアされている。この処理は @^ などへの対応のために必要
                // ただしRootStrokeTableStateが同時打鍵の開始だった場合は、OrigStringを返さない
                STATE_COMMON->AppendOrigString(myChar);
            }

            if (!myNode()->isRootStrokeTableNode() && !IsRootKeyCombination()) {
                // 自身がRootStrokeNodeでなく、かつRootStrokeKeyが同時打鍵キーでなければ通常面に落としこむ
                // 同時打鍵の場合は、重複回避のため、第２キーはシフト化されてくる場合がある。その場合は、UNSHIFTしない
                deckey = UNSHIFT_DECKEY(deckey);
                LOG_DEBUGH(_T("UNSHIFT_DECKEY: {}: deckey={:x}H({})"), Name, deckey, deckey);
            }
            if (STATE_COMMON->IsDecodeKeyboardCharMode()) {
                // キーボードフェイス文字を返すモード
                LOG_DEBUGH(_T("SetOutString"));
                STATE_COMMON->SetOutString(myChar, 0);
            } else if (SETTINGS->eisuModeEnabled && !STATE_COMMON->IsUpperRomanGuideMode() && myNode()->isRootStrokeTableNode() && myChar >= 'A' && myChar <= 'Z') {
                // 英数モード
                LOG_DEBUGH(_T("SetNextNodeMaybe: Eisu"));
                STATE_COMMON->SetOutString(myChar, 0);
                if (EISU_NODE) EISU_NODE->blockerNeeded = true; // 入力済み末尾にブロッカーを設定する
                SetNextNodeMaybe(EISU_NODE.get());
            } else {
                Node* np = NEXT_NODE(deckey);
                StrokeTableNode* tp = np ? dynamic_cast<StrokeTableNode*>(np) : 0;
                PostRewriteOneShotNode* rp = tp ? tp->getRewriteNode() : 0;
                if (rp) {
                    const RewriteInfo* rewInfo;
                    std::tie(rewInfo, std::ignore) = rp->matchWithTailString();
                    if (rewInfo) {
                        // 入力文字列にマッチする書き換えノードを持つ
                        np = rp;
                        LOG_DEBUGH(_T("REWRITE node matched"));
                    }
                }
                LOG_DEBUGH(_T("SetNextNodeMaybe"));
                SetNextNodeMaybe(np);
            }
            if (!NextNodeMaybe() || !NextNodeMaybe()->isStrokeTableNode()) {
                // 次ノードがストロークノードでないか、ストロークテーブルノード以外(文字ノードや機能ノードなど)ならば、全ストロークを削除対象とする
                LOG_DEBUGH(_T("{}: RemoveAllStroke: NEXT_NODE={}, DecodeKeyboardCharMode={}"), Name, NODE_NAME(NextNodeMaybe()), STATE_COMMON->IsDecodeKeyboardCharMode());
                setToRemoveAllStroke();
            }
            if (deckey < NORMAL_DECKEY_NUM && IsRootKeyHiraganaized()) {
                _LOG_DEBUGH(_T("{}, rootKeyHiraganaized={}"), Name, IsRootKeyHiraganaized());
                STATE_COMMON->SetHiraganaToKatakana();   // 通常面の平仮名を片仮名に変換するモード
            }
            LOG_DEBUGH(_T("LEAVE"));
        }

        // Shift飾修されたキー
        void handleShiftKeys(int deckey) {
            _LOG_DEBUGH(_T("ENTER: {}, deckey={:x}({}), rootKeyHiraganaized={}"), Name, deckey, deckey, IsRootKeyHiraganaized());
            if (origDeckey < 0) origDeckey = deckey;
            //handleStrokeKeys(UNSHIFT_DECKEY(deckey));
            handleStrokeKeys(deckey);
            //if (IsRootKeyUnshifted()) {
            //    // シフト入力された平仮名を片仮名に変換するモードとかの場合
            //    //shiftedOrigChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
            //    handleStrokeKeys(UNSHIFT_DECKEY(deckey));
            //} else {
            //    //State::handleShiftKeys(deckey);
            //    //// 自身を捨てて前打鍵を出力
            //    //SetNextNodeMaybe(PrevCharNode::Singleton());
            //    //setToRemoveAllStroke();
            //    // 2打鍵目以降は、Unshiftして処理する
            //    handleStrokeKeys(UNSHIFT_DECKEY(deckey));
            //}
            _LOG_DEBUGH(_T("LEAVE: {}"), Name);
        }

        // これがあると、TUT-Code など、第2打鍵のSpaceキーが記号に割り当てられているコード系で問題になる
        //void handleSpaceKey() {
        //    LOG_DEBUG(_T("CALLED: {}, origString=\"{}\""), Name, to_wstr(STATE_COMMON->OrigString()));
        //    setToRemoveAllStroke();
        //    STATE_COMMON->OutputOrigString();
        //    HISTORY_RESIDENT_STATE->AddNewHistEntryOnSomeChar();
        //}

        void handleBS() {
            LOG_DEBUG(_T("CALLED: {}"), Name);
            if (SETTINGS->removeOneByBS) {
                // 自ステートだけを削除(上位のストロークステートは残す)
                MarkUnnecessary();
            } else {
                // 全打鍵の取り消し
                setToRemoveAllStroke();
            }
        }

        // FullEscapeの処理 -- 履歴検索文字列の遡及ブロッカーをセット
        void handleFullEscape() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            STATE_COMMON->SetBothHistoryBlockFlag();
            setToRemoveAllStroke();
        }

        void handleClearStroke() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            setToRemoveAllStroke();
        }

        void handleEsc() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            setToRemoveAllStroke();
        }

        // CommitState の処理 -- 処理のコミット
        void handleCommitState() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            setToRemoveAllStroke();
        }

        void handleEnter() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            // 前打鍵を出力する
            SetNextNodeMaybe(PREV_CHAR_NODE);
            setToRemoveAllStroke();
        }

        //void handleCtrlU() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleFullEscape();
        //    State::handleCtrlU();
        //}

        //void handleCtrlH() {
        //    LOG_DEBUG(_T("CALLED: {}"), Name);
        //    handleBS();
        //}

        // 不要になったら自身を削除する
        bool IsUnnecessary() override {
            LOG_DEBUG(_T("CALLED: {}: {}"), Name, State::IsUnnecessary());
            return State::IsUnnecessary() || isToRemoveAllStroke();
        }

        // 次状態の処理
        // ストロークの末尾まで到達して、ストロークチェイン全体が不要になった
        // 次ストロークが取り消されたので、自ストロークも初期状態に戻す
        virtual void checkNextState() {
            LOG_DEBUG(_T("CALLED: {}"), Name);
            if (NextState()) {
                auto ps = dynamic_cast<StrokeTableState*>(NextState());
                if (ps && ps->isToRemoveAllStroke()) {
                    // ストロークテーブルチェイン全体の削除
                    setToRemoveAllStroke();
                    _LOG_DEBUGH(_T("REMOVE ALL: {}"), Name);
                } else if (NextState()->IsUnnecessary()) {
                    // 次状態が取り消されたら、origString を縮めておく
                    STATE_COMMON->PopOrigString();
                    // ルートでなければ打鍵ヘルプを再セットする
                    if (myNode()->depth() > 0) setNormalStrokeHelpVkb();
                }
            }
        }

        // ストローク状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER: {}"), Name);
            // 打鍵ヘルプをセットする
            setNormalStrokeHelpVkb();
            _LOG_DEBUGH(_T("LEAVE: {}"), Name);
            // 前状態にチェインする
            return true;
        }

        // ストロークテーブルチェインの長さ(テーブルのレベル)
        size_t StrokeTableChainLength() const {
            size_t len = myNode()->depth() + 1;
            if (NextState()) {
                len = NextState()->StrokeTableChainLength();
            }
            LOG_DEBUG(_T("LEAVE: {}, len={}"), Name, len);
            return len;
        }

    private:
        void setNormalStrokeHelpVkb() {
            _LOG_DEBUGH(_T("ENTER"));
            // 打鍵ヘルプをセットする
            STATE_COMMON->SetNormalVkbLayout();
            auto tblNode = myNode();
            if (tblNode) {
                tblNode->CopyChildrenFace(STATE_COMMON->GetFaces(), STATE_COMMON->FacesSize());
            } else {
                STATE_COMMON->ClearFaces();
            }
            //STATE_COMMON->SetWaiting2ndStroke();
            _LOG_DEBUGH(_T("LEAVE"));
        }

    };
    DEFINE_CLASS_LOGGER(StrokeTableState);

    // -------------------------------------------------------------------
    // ルートストロークテーブル状態クラス
    class RootStrokeTableState : public StrokeTableState {
    private:
        DECLARE_CLASS_LOGGER;

        //bool bUnshifted = false;
        bool bHiraganaized = false;
        bool bCombination = false;

    protected:
        // ルートキーは UNSHIFT されているか
        //bool IsRootKeyUnshifted() override {
        //    _LOG_DEBUGH(_T("CALLED: {}, unshifted={}"), Name, bUnshifted);
        //    return bUnshifted;
        //}

        // ルートキーは平仮名化されているか
        bool IsRootKeyHiraganaized() override {
            _LOG_DEBUGH(_T("CALLED: {}, hiraganaized={}"), Name, bHiraganaized);
            return bHiraganaized;
        }

        // ルートキーは同時打鍵キーか
        bool IsRootKeyCombination() override {
            _LOG_DEBUGH(_T("CALLED: {}, combination={}"), Name, bCombination);
            return bCombination;
        }

    public:
        // コンストラクタ
        RootStrokeTableState(StrokeTableNode* pN)
            : StrokeTableState(pN) {
            LOG_INFO(_T("CALLED: ctor"));
            Name = logger.ClassNameT();
            STATE_COMMON->ClearOrigString();
        }

        // StrokeTableNode を処理する
        void handleStrokeKeys(int deckey) {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            STATE_COMMON->SyncFirstStrokeKeyCount();    // 第1ストロークキーカウントの同期
            if (deckey >= COMBO_DECKEY_START && deckey < EISU_COMBO_DECKEY_END) {
                bCombination = true;
            }
            if (!bHiraganaized && deckey < NORMAL_DECKEY_NUM && SETTINGS->hiraToKataNormalPlane) {
                bHiraganaized = true;
                STATE_COMMON->SetHiraganaToKatakana();   // 通常面の平仮名を片仮名に変換するモード
            }
            StrokeTableState::handleStrokeKeys(deckey);
            //if (!NextNodeMaybe() && State::isStrokableFuncKey(deckey)) {
            //    // 次ノードがなく、拡張修飾キーの類なら、入力された拡張修飾類キーをそのまま返す
            //    setThroughDeckeyFlag();
            //}
        }

        // Shift飾修されたキー
        void handleShiftKeys(int deckey) {
            _LOG_DEBUGH(_T("ENTER: {}, deckey={:x}H({}), hiraConvPlane={}"), Name, deckey, deckey, SETTINGS->hiraToKataShiftPlane);
            STATE_COMMON->SyncFirstStrokeKeyCount();    // 第1ストロークキーカウントの同期
            if (origDeckey < 0) origDeckey = deckey;
            if (SETTINGS->hiraToKataShiftPlane > 0 &&
                DECKEY_TO_SHIFT_PLANE(deckey) == SETTINGS->hiraToKataShiftPlane &&
                utils::contains(VkbTableMaker::GetHiraganaFirstDeckeys(), UNSHIFT_DECKEY(deckey))) {
                // 後でShift入力された平仮名をカタカナに変換する
                bHiraganaized = true;
                _LOG_DEBUGH(_T("SET SHIFTED HIRAGANA: {}"), Name);
                STATE_COMMON->SetHiraganaToKatakana();   // Shift入力された平仮名だった
                //shiftedOrigChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
                //_LOG_DEBUGH(_T("Unshifted: shiftedOrigChar={}"), shiftedOrigChar);
                handleStrokeKeys(UNSHIFT_DECKEY(deckey));
            } else {
                // その他の(拡張)シフト
                StrokeTableState::handleStrokeKeys(deckey);
            }
            _LOG_DEBUGH(_T("LEAVE: {}"), Name);
        }

        // Shift+Space を通常Spaceとして扱う
        //void handleShiftSpaceAsNormalSpace() {
        //    LOG_DEBUG(_T("CALLED: {}"), Name);
        //    MarkUnnecessary();            // これをやらないと、RootStrokeTable が残ってしまう
        //    State::handleShiftKeys(SHIFT_SPACE_DECKEY);
        //}

        //// 第1打鍵でSpaceが入力された時の処理
        //void handleSpaceKey() {
        //    LOG_DEBUG(_T("CALLED: {}: outString = MSTR_SPACE"), Name);
        //    STATE_COMMON->SetOrigString(' '); // 空白文字を返す
        //    StrokeTableState::handleSpaceKey();
        //}

        // 第1打鍵でSpaceが入力された時の処理
        void handleSpaceKey() {
            LOG_DEBUG(_T("CALLED: %"), Name);
            handleStrokeKeys(STROKE_SPACE_DECKEY);
        }

        void handleBS() {
            LOG_DEBUG(_T("CALLED: {}"), Name);
            setCharDeleteInfo(1);
            MarkUnnecessary();
        }

        // 次状態をチェックして、自身の状態を変更させるのに使う。DECKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
        // 例：ストロークの末尾まで到達して、ストロークチェイン全体が不要になった
        // 例：次ストロークが取り消されたので、自ストロークも初期状態に戻す
        void checkNextState() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            StrokeTableState::checkNextState();
            if (bHiraganaized) {
                _LOG_DEBUGH(_T("SET SHIFTED HIRAGANA: {}"), Name);
                STATE_COMMON->SetHiraganaToKatakana();   // Shift入力された平仮名だった
            }
            if (NextState() && NextState()->IsUnnecessary()) {
                // 次状態が不要になったらルートストロークテーブルも不要
                MarkUnnecessary();
                _LOG_DEBUGH(_T("REMOVE ALL: {}"), Name);
            }
        }

    };
    DEFINE_CLASS_LOGGER(RootStrokeTableState);

} // namespace

// -------------------------------------------------------------------
// StrokeTableNode - ストロークテーブルの連鎖となるノード

// デストラクタ
StrokeTableNode::~StrokeTableNode() {
    _LOG_DEBUGH(_T("CALLED: destructor: ptr={:p}"), (void*)this);
    delete rewriteNode;
    for (auto p : children) {
        delete p;       // 子ノードの削除 (デストラクタ)
    }
}

// 当ノードを処理する State インスタンスを作成する (depth == 0 ならルートStateを返す)
State* StrokeTableNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return depth() == 0 ? new RootStrokeTableState(this) : new StrokeTableState(this);
}

// 子ノード列の文字をコピーする
void StrokeTableNode::CopyChildrenFace(mchar_t* faces, size_t facesSize) {
    _LOG_DEBUGH(_T("ENTER"));
    for (size_t n = 0; n < facesSize; ++n) {
        const Node* child = getNth(n);
        const auto& s = child ? child->getString() : MString();
        faces[n] = s.empty() ? 0 : is_ascii_pair(s) ? make_mchar((wchar_t)s[0], (wchar_t)s[1]) : s[0];  // "12" のような半角文字のペアも扱う
    }
    _LOG_DEBUGH(_T("LEAVE"));
}

// 後置書き換えノードを取得
PostRewriteOneShotNode* StrokeTableNode::getRewriteNode() {
    return rewriteNode;
}

// 後置書き換えノードをマージ
void StrokeTableNode::mergeRewriteNode(PostRewriteOneShotNode* node) {
    if (rewriteNode) {
        rewriteNode->merge(*node);
        delete(node);
    } else {
        rewriteNode = node;
    }
    rewriteNode->clearRewriteString();
}

// 指定文字に至るストローク列を返す
bool StrokeTableNode::getStrokeListSub(const MString& target, std::vector<int>& list, bool bFull) {
    for (size_t i = 0; i < children.size(); ++i) {
        if (!bFull && i >= NORMAL_DECKEY_NUM) break;
        Node* p = children[i];
        if (p) {
            StrokeTableNode* pn = dynamic_cast<StrokeTableNode*>(p);
            if (pn) {
                list.push_back((int)i);
                if (pn->getStrokeListSub(target, list, bFull)) return true;
                list.pop_back();
            } else {
                Node* cn = dynamic_cast<StringNode*>(p);
                if (!cn) cn = dynamic_cast<PostRewriteOneShotNode*>(p);
                if (cn && target == cn->getString()) {
                    list.push_back((int)i);
                    return true;
                }
            }
        }
    }
    return false;
}

// ストロークガイドの構築
void StrokeTableNode::MakeStrokeGuide(StringRef targetChars, int tableId) {
    std::vector<wchar_t> strokeGuide(VkbTableMaker::OUT_TABLE_SIZE);
    VkbTableMaker::ReorderByStrokePosition(this, strokeGuide.data(), targetChars, tableId);
    for (size_t i = 0; i * 2 < strokeGuide.size() && i < children.size(); ++i) {
        auto ch = strokeGuide[i * 2];
        //Node* child = children[i].get();
        Node* child = getNth(i);
        if (ch != 0 && child && child->isStrokeTableNode()) {
            StrokeTableNode* tblNode = dynamic_cast<StrokeTableNode*>(child);
            if (tblNode) tblNode->nodeMarker[0] = ch;
        }
    }
}

// 後置書き換え子ノードありか
bool StrokeTableNode::hasPostRewriteNode() {
    if (iHasPostRewriteNode == 0) {
        iHasPostRewriteNode = findPostRewriteNode(-1);
        LOG_INFO(_T("iHasPostRewriteNode: {}"), iHasPostRewriteNode);
    }
    return iHasPostRewriteNode > 0;
}

// (半)濁点のみの後置書き換え子ノードがあるか
bool StrokeTableNode::hasOnlyUsualRewriteNdoe() {
    if (iHasPostRewriteNode == 0) {
        iHasPostRewriteNode = findPostRewriteNode(-1);
        LOG_INFO(_T("iHasPostRewriteNode: {}"), iHasPostRewriteNode);
    }
    return iHasPostRewriteNode == 1;
}

int StrokeTableNode::findPostRewriteNode(int result) {
    for (Node* p : children) {
        if (p) {
            StrokeTableNode* pn = dynamic_cast<StrokeTableNode*>(p);
            if (pn) {
                if (pn->getRewriteNode()) return 2;
                result = pn->findPostRewriteNode(result);
                if (result > 1) return 2;
            } else {
                if (dynamic_cast<DakutenOneShotNode*>(p)) {
                    LOG_INFO(_T("DakutenOneShotNode FOUND"));
                    result = 1;
                } else if (dynamic_cast<PostRewriteOneShotNode*>(p)) {
                    LOG_INFO(_T("LEAVE: 2"));
                    return 2;
                }
            }
        }
    }
    LOG_INFO(_T("LEAVE: {}"), result);
    return result;
}

// -------------------------------------------------------------------
// 主・副ストローク木の入れ替え
int StrokeTableNode::ExchangeStrokeTable() {
    if (RootStrokeNode == RootStrokeNode1.get()) {
        if (RootStrokeNode2.get() != nullptr) RootStrokeNode = RootStrokeNode2.get();
    } else {
        RootStrokeNode = RootStrokeNode1.get();
    }
    return GetCurrentStrokeTableNum();
}

// 主ストローク木の使用
int StrokeTableNode::UseStrokeTable1() {
    RootStrokeNode = RootStrokeNode1.get();
    return GetCurrentStrokeTableNum();
}

// 副ストローク木の使用
int StrokeTableNode::UseStrokeTable2() {
    if (RootStrokeNode2.get() != nullptr) RootStrokeNode = RootStrokeNode2.get();
    return GetCurrentStrokeTableNum();
}

// 第3ストローク木の使用
int StrokeTableNode::UseStrokeTable3() {
    if (RootStrokeNode3.get() != nullptr) RootStrokeNode = RootStrokeNode3.get();
    return GetCurrentStrokeTableNum();
}

// 現在のストローク木の番号
int StrokeTableNode::GetCurrentStrokeTableNum() { 
    return RootStrokeNode == RootStrokeNode3.get() ? 3
        : RootStrokeNode == RootStrokeNode2.get() ? 2
        : 1;
}

std::unique_ptr<StrokeTableNode> StrokeTableNode::RootStrokeNode1;
std::unique_ptr<StrokeTableNode> StrokeTableNode::RootStrokeNode2;
std::unique_ptr<StrokeTableNode> StrokeTableNode::RootStrokeNode3;
StrokeTableNode* StrokeTableNode::RootStrokeNode;

// -------------------------------------------------------------------
// ストローク木のトラバーサ
StrokeTreeTraverser::StrokeTreeTraverser(class StrokeTableNode* p, bool full) : bFull(full) {
    tblList.push_back(p);
    path.push_back(-1);
}

Node* StrokeTreeTraverser::getNext() {
    while (!tblList.empty()) {
        int nodePos = 0;
        if (bRewriteTable) {
            path.push_back(nodePos);
            bRewriteTable = false;
        } else {
            nodePos = path.back() + 1;
        }
        StrokeTableNode* pn = tblList.back();
        while (pn && nodePos < (int)pn->numChildren()) {
            if (!bFull && nodePos >= NORMAL_DECKEY_NUM * 2) break;

            Node* p = pn->getNth(nodePos);
            if (p) {
                path.back() = nodePos;
                if (!p->isStrokeTableNode()) return p;

                StrokeTableNode* pp = dynamic_cast<StrokeTableNode*>(p);
                if (pp) {
                    pn = pp;
                    tblList.push_back(pn);
                    nodePos = 0;
                    // 後置書き換えノードを持つテーブルノード
                    if (pp->getRewriteNode()) {
                        bRewriteTable = true;
                        return pp->getRewriteNode();
                    }
                    path.push_back(nodePos);
                    continue;
                }
            }
            ++nodePos;
        }
        tblList.pop_back();
        path.pop_back();
    }
    return 0;
}

