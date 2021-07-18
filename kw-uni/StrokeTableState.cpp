// StrokeTable を処理するデコーダ状態のクラス
//#include "pch.h"
#include "Logger.h"
#include "misc_utils.h"
#include "Constants.h"

#include "State.h"
#include "StrokeTable.h"
#include "VkbTableMaker.h"
#include "Settings.h"

#include "HotkeyToChars.h"
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
        void handleStrokeKeys(int hotkey) {
            wchar_t myChar = shiftedOrigChar != 0 ? shiftedOrigChar : HOTKEY_TO_CHARS->GetCharFromHotkey(hotkey);
            LOG_DEBUG(_T("CALLED: %s: hotkey=%xH(%d), face=%c, nodeDepth=%d"), NAME_PTR, hotkey, hotkey, myChar, DEPTH);
            STATE_COMMON->AppendOrigString(myChar); // RootStrokeTableState が作成されたときに OrigString はクリアされている

            SetTemporaryNextNode(NEXT_NODE(hotkey));
            if (!TemporaryNextNode() || !TemporaryNextNode()->isStrokeTableNode()) {
                LOG_DEBUG(_T("%s: Next node="), NAME_PTR, NODE_NAME_PTR(TemporaryNextNode()));
                setToRemoveAllStroke();
            }
        }

        // Shift飾修されたキー
        void handleShiftKeys(int hotkey) {
            _LOG_DEBUGH(_T("ENTER: %s, hotkey=%x(%d)"), NAME_PTR, hotkey, hotkey);
            if (IsRootKeyUnshifted()) {
                shiftedOrigChar = HOTKEY_TO_CHARS->GetCharFromHotkey(hotkey);
                handleStrokeKeys(UNSHIFT_HOTKEY(hotkey));
            } else {
                State::handleShiftKeys(hotkey);
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

        // 次状態をチェックして、自身の状態を変更させるのに使う。HOTKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
        void CheckNextState() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            if (pNext) {
                if (((StrokeTableState*)pNext)->isToRemoveAllStroke()) {
                    // ストローククテーブルチェイン全体の削除
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
            LOG_DEBUG(_T("ENTER: %s"), NAME_PTR);
            // 打鍵ヘルプをセットする
            setNormalStrokeHelpVkb();
            LOG_DEBUG(_T("LEAVE: %s"), NAME_PTR);
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

        // StrokeTableNode を処理する
        void handleStrokeKeys(int hotkey) {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            StrokeTableState::handleStrokeKeys(hotkey);
        }

        // Shift飾修されたキー
        void handleShiftKeys(int hotkey) {
            _LOG_DEBUGH(_T("ENTER: %s, hotkey=%xH(%d), hiraConv=%s"), NAME_PTR, hotkey, hotkey, BOOL_TO_WPTR(SETTINGS->convertShiftedHiraganaToKatakana));
            if (SETTINGS->convertShiftedHiraganaToKatakana && utils::contains(VkbTableMaker::GetHiraganaFirstHotkeys(), UNSHIFT_HOTKEY(hotkey))) {
                // 後でShift入力された平仮名をカタカナに変換する
                bUnshifted = true;
                shiftedOrigChar = HOTKEY_TO_CHARS->GetCharFromHotkey(hotkey);
                handleStrokeKeys(UNSHIFT_HOTKEY(hotkey));
            } else {
                bUnnecessary = true;            // これをやらないと、RootStrokeTable が残ってしまう
                State::handleShiftKeys(hotkey);
            }
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
        }

        // Shift+Space を通常Spaceとして扱う
        void handleShiftSpaceAsNormalSpace() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            bUnnecessary = true;            // これをやらないと、RootStrokeTable が残ってしまう
            State::handleShiftKeys(SHIFT_SPACE_HOTKEY);
        }

        //// 第1打鍵でSpaceが入力された時の処理
        //void handleSpaceKey() {
        //    LOG_DEBUG(_T("CALLED: %s: outString = MSTR_SPACE"), NAME_PTR);
        //    STATE_COMMON->SetOrigString(' '); // 空白文字を返す
        //    StrokeTableState::handleSpaceKey();
        //}

        // 第1打鍵でSpaceが入力された時の処理
        void handleSpaceKey() {
            LOG_DEBUG(_T("CALLED: %"), NAME_PTR);
            handleStrokeKeys(HOTKEY_STROKE_SPACE);
        }

        void handleBS() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            setCharDeleteInfo(1);
            bUnnecessary = true;
        }

        // 次状態をチェックして、自身の状態を変更させるのに使う。HOTKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
        void CheckNextState() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            StrokeTableState::CheckNextState();
            if (pNext && pNext->IsUnnecessary()) {
                // 次状態が不要になったらルートストロークテーブルも不要
                bUnnecessary = true;
                if (bUnshifted) STATE_COMMON->SetShiftedHiraganaToKatakana();   // Shift入力された平仮名だった
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

