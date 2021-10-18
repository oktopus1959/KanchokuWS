// StrokeTable を処理するデコーダ状態のクラス
//#include "pch.h"
#include "Logger.h"
#include "misc_utils.h"
#include "Constants.h"

#include "State.h"
#include "StrokeTable.h"
#include "VkbTableMaker.h"
#include "Settings.h"
#include "KeysAndChars/MyPrevChar.h"

#include "DeckeyToChars.h"
#include "History/HistoryStayState.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughStrokeTable)

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

namespace {
    // -------------------------------------------------------------------
    // ストロークテーブル状態クラス
    class StrokeTableState : public State {
    private:
        DECLARE_CLASS_LOGGER;

    protected:
        inline StrokeTableNode* myNode() const { return (StrokeTableNode*)pNode; }

        void setToRemoveAllStroke() {
            bUnnecessary = true;
            myNode()->setToRemoveAllStroke();
        }

        bool isToRemoveAllStroke() {
            return myNode()->isToRemoveAllStroke();
        }

#define REMOVE_ALL_PTR (utils::boolToString(myNode()->isToRemoveAllStroke()).c_str())

        wchar_t shiftedOrigChar = 0;

        // ルートキーは UNSHIFT されているか
        virtual bool IsRootKeyUnshifted() {
            auto p = dynamic_cast<StrokeTableState*>(pPrev);
            return p != nullptr ? p->IsRootKeyUnshifted() : false;
        }

    public:
        // コンストラクタ
        StrokeTableState(StrokeTableNode* pN) {
            Initialize(logger.ClassNameT(), pN);
        }

#define DEPTH           (myNode() ? myNode()->depth(): -1)
#define NEXT_NODE(n)    (myNode() ? myNode()->getNth(n) : 0)
#define NUM_CHILDREN    (myNode() ? myNode()->numChildren() : 0)

#define NAME_PTR (Name.c_str())

        // StrokeTableNode を処理する
        void handleStrokeKeys(int deckey) {
            wchar_t myChar = shiftedOrigChar != 0 ? shiftedOrigChar : DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
            _LOG_DEBUGH(_T("CALLED: %s: deckey=%xH(%d), face=%c, nodeDepth=%d"), NAME_PTR, deckey, deckey, myChar, DEPTH);
            STATE_COMMON->AppendOrigString(myChar); // RootStrokeTableState が作成されたときに OrigString はクリアされている

            if (STATE_COMMON->IsDecodeKeyboardCharMode()) {
                // キーボードフェイス文字を返すモード
                STATE_COMMON->SetOutString(myChar, 0);
            } else {
                SetNextNodeMaybe(NEXT_NODE(deckey));
            }
            if (!NextNodeMaybe() || !NextNodeMaybe()->isStrokeTableNode()) {
                // 次ノードがストロークノードでないか、キーボードフェイス文字を返すモードならば、全ストロークを削除対象とする
                _LOG_DEBUGH(_T("%s: RemoveAllStroke: Next node=%p, DecodeKeyboardCharMode=%s"), NAME_PTR, NODE_NAME_PTR(NextNodeMaybe()), BOOL_TO_WPTR(STATE_COMMON->IsDecodeKeyboardCharMode()));
                setToRemoveAllStroke();
            }
        }

        // Shift飾修されたキー
        void handleShiftKeys(int deckey) {
            _LOG_DEBUGH(_T("ENTER: %s, deckey=%x(%d), rootKeyUnshifted=%s"), NAME_PTR, deckey, deckey, BOOL_TO_WPTR(IsRootKeyUnshifted()));
            if (IsRootKeyUnshifted()) {
                // シフト入力された平仮名を片仮名に変換するモードとかの場合
                shiftedOrigChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
                handleStrokeKeys(UNSHIFT_DECKEY(deckey));
            } else {
                //State::handleShiftKeys(deckey);
                //// 自身を捨てて前打鍵を出力
                //SetNextNodeMaybe(PrevCharNode::Singleton());
                //setToRemoveAllStroke();
                // 2打鍵目以降は、Unshiftして処理する
                handleStrokeKeys(UNSHIFT_DECKEY(deckey));
            }
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
        }

        // これがあると、TUT-Code など、第2打鍵のSpaceキーが記号に割り当てられているコード系で問題になる
        //void handleSpaceKey() {
        //    LOG_DEBUG(_T("CALLED: %s, origString=\"%s\""), NAME_PTR, MAKE_WPTR(STATE_COMMON->OrigString()));
        //    setToRemoveAllStroke();
        //    STATE_COMMON->OutputOrigString();
        //    HISTORY_STAY_STATE->AddNewHistEntryOnSomeChar();
        //}

        void handleBS() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            if (SETTINGS->removeOneStrokeByBackspace) {
                // 自ステートだけを削除(上位のストロークステートは残す)
                bUnnecessary = true;
            } else {
                // 全打鍵の取り消し
                setToRemoveAllStroke();
            }
        }

        // FullEscapeの処理 -- 履歴検索文字列の遡及ブロッカーをセット
        void handleFullEscape() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            STATE_COMMON->SetBothHistoryBlockFlag();
            setToRemoveAllStroke();
        }

        void handleEsc() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            setToRemoveAllStroke();
        }

        //void handleCtrlU() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleFullEscape();
        //    State::handleCtrlU();
        //}

        //void handleCtrlH() {
        //    LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
        //    handleBS();
        //}

#define UNNECESSARY_PTR (utils::boolToString(bUnnecessary).c_str())

        // 不要になったら自身を削除する
        bool IsUnnecessary() {
            LOG_DEBUG(_T("CALLED: %s: %s"), NAME_PTR, UNNECESSARY_PTR);
            return bUnnecessary || myNode()->isToRemoveAllStroke();
        }

        // 次状態をチェックして、自身の状態を変更させるのに使う。DECKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
        void CheckNextState() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            if (pNext) {
                auto ps = dynamic_cast<StrokeTableState*>(pNext);
                if (ps && ps->isToRemoveAllStroke()) {
                    // ストロークテーブルチェイン全体の削除
                    setToRemoveAllStroke();
                    _LOG_DEBUGH(_T("REMOVE ALL: %s"), NAME_PTR);
                } else if (pNext->IsUnnecessary()) {
                    // 次状態が取り消されたら、origString を縮めておく
                    STATE_COMMON->PopOrigString();
                    // ルートでなければ打鍵ヘルプを再セットする
                    if (myNode()->depth() > 0) setNormalStrokeHelpVkb();
                }
            }
        }

        // ストローク状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
            // 打鍵ヘルプをセットする
            setNormalStrokeHelpVkb();
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
            // 前状態にチェインする
            return true;
        }

        // ストロークテーブルチェインの長さ(テーブルのレベル)
        size_t StrokeTableChainLength() {
            size_t len = myNode()->depth() + 1;
            if (pNext) {
                len = pNext->StrokeTableChainLength();
            }
            LOG_DEBUG(_T("LEAVE: %s, len=%d"), NAME_PTR, len);
            return len;
        }

    private:
        void setNormalStrokeHelpVkb() {
            _LOG_DEBUGH(_T("ENTER"));
            // 打鍵ヘルプをセットする
            STATE_COMMON->SetNormalVkbLayout();
            mchar_t* faces = STATE_COMMON->GetFaces();
            size_t facesSize = STATE_COMMON->FacesSize();
            size_t numChildren = NUM_CHILDREN;
            for (size_t n = 0; n < facesSize; ++n) {
                mchar_t ch = 0;
                if (n < numChildren) {
                    const Node* child = NEXT_NODE(n);
                    const auto& s = child ? child->getString() : MString();
                    ch = s.empty() ? 0 : is_ascii_pair(s) ? make_mchar((wchar_t)s[0], (wchar_t)s[1]) : s[0];  // "12" のような半角文字のペアも扱う
                }
                faces[n] = ch;
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

        bool bUnshifted = false;

    protected:
        // ルートキーは UNSHIFT されているか
        virtual bool IsRootKeyUnshifted() {
            _LOG_DEBUGH(_T("CALLED: %s, unshifted=%s"), NAME_PTR, BOOL_TO_WPTR(bUnshifted));
            return bUnshifted;
        }

    public:
        // コンストラクタ
        RootStrokeTableState(StrokeTableNode* pN)
            : StrokeTableState(pN) {
            Name = logger.ClassNameT();
            STATE_COMMON->ClearOrigString();
        }

        //// StrokeTableNode を処理する
        //void handleStrokeKeys(int deckey) {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    StrokeTableState::handleStrokeKeys(deckey);
        //}

        // Shift飾修されたキー
        void handleShiftKeys(int deckey) {
            _LOG_DEBUGH(_T("ENTER: %s, deckey=%xH(%d), hiraConvPlane=%d"), NAME_PTR, deckey, deckey, SETTINGS->hiraganaToKatakanaShiftPlane);
            if (SETTINGS->hiraganaToKatakanaShiftPlane > 0 &&
                DECKEY_TO_SHIFT_PLANE(deckey) == SETTINGS->hiraganaToKatakanaShiftPlane &&
                utils::contains(VkbTableMaker::GetHiraganaFirstDeckeys(), UNSHIFT_DECKEY(deckey))) {
                // 後でShift入力された平仮名をカタカナに変換する
                bUnshifted = true;
                shiftedOrigChar = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
                handleStrokeKeys(UNSHIFT_DECKEY(deckey));
            } else {
                //bUnnecessary = true;            // これをやらないと、RootStrokeTable が残ってしまう
                //State::handleShiftKeys(deckey);
                StrokeTableState::handleStrokeKeys(deckey);
            }
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
        }

        // Shift+Space を通常Spaceとして扱う
        //void handleShiftSpaceAsNormalSpace() {
        //    LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
        //    bUnnecessary = true;            // これをやらないと、RootStrokeTable が残ってしまう
        //    State::handleShiftKeys(SHIFT_SPACE_DECKEY);
        //}

        //// 第1打鍵でSpaceが入力された時の処理
        //void handleSpaceKey() {
        //    LOG_DEBUG(_T("CALLED: %s: outString = MSTR_SPACE"), NAME_PTR);
        //    STATE_COMMON->SetOrigString(' '); // 空白文字を返す
        //    StrokeTableState::handleSpaceKey();
        //}

        // 第1打鍵でSpaceが入力された時の処理
        void handleSpaceKey() {
            LOG_DEBUG(_T("CALLED: %"), NAME_PTR);
            handleStrokeKeys(STROKE_SPACE_DECKEY);
        }

        void handleBS() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            setCharDeleteInfo(1);
            bUnnecessary = true;
        }

        // 次状態をチェックして、自身の状態を変更させるのに使う。DECKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
        void CheckNextState() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            StrokeTableState::CheckNextState();
            if (bUnshifted) {
                _LOG_DEBUGH(_T("SET SHIFTED HIRAGANA: %s"), NAME_PTR);
                STATE_COMMON->SetShiftedHiraganaToKatakana();   // Shift入力された平仮名だった
            }
            if (pNext && pNext->IsUnnecessary()) {
                // 次状態が不要になったらルートストロークテーブルも不要
                bUnnecessary = true;
                _LOG_DEBUGH(_T("REMOVE ALL: %s"), NAME_PTR);
            }
        }

    };
    DEFINE_CLASS_LOGGER(RootStrokeTableState);

} // namespace

// -------------------------------------------------------------------
// StrokeTableNode - ストロークテーブルの連鎖となるノード

// 当ノードを処理する State インスタンスを作成する (depth == 0 ならルートStateを返す)
State* StrokeTableNode::CreateState() {
    bRemoveAllStroke = false;
    return depth() == 0 ? new RootStrokeTableState(this) : new StrokeTableState(this);
}

// -------------------------------------------------------------------
std::unique_ptr<StrokeTableNode> StrokeTableNode::RootStrokeNode;

