#include "file_utils.h"
#include "string_utils.h"
#include "path_utils.h"

#include "deckey_id_defs.h"
#include "ErrorHandler.h"
#include "KanchokuIni.h"
#include "Settings.h"
#include "OutputStack.h"
#include "State.h"
#include "BushuComp.h"
#include "BushuAssoc.h"
#include "BushuAssocDic.h"
#include "History/HistoryDic.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughBushu)

#define EX_NODE BUSHU_ASSOC_EX_NODE
#define SAFE_CHAR(ch) (ch > 0 ? ch : ' ')

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

namespace {
    // -------------------------------------------------------------------
#define N_SUB_LIST 10

    // 現在処理中の部首連想入力候補情報
    class CurrentAssocList {
        DECLARE_CLASS_LOGGER;

    private:
        BushuAssocEntry* currentEntry = 0;  // 現在処理中の部首連想エントリ
        size_t row = 0;                     // ターゲットが10個を超えるときに、1行10個として何行目に当たるか
        std::vector<MString> strList;       // 10個の候補の配列。BushuAssocEntry は mchar_t で管理しているが、MString に変換しておいたほうが便利
        MString emptyStr;


        void clearStrList() {
            for (auto& s : strList) s.clear();
        }

        void initailize() {
            currentEntry = 0;
            row = 0;
            clearStrList();
        }

    public:
        CurrentAssocList() {
            strList.resize(N_SUB_LIST);
        }

        // ch から連想される文字のリストを取得する
        bool FindEntry(mchar_t ch) {
            initailize();
            if (BUSHU_ASSOC_DIC) {
                currentEntry = BUSHU_ASSOC_DIC->GetEntry(ch);
                if (currentEntry) {
                    currentEntry->CopySubList(strList, 0, N_SUB_LIST);
                    return true;
                }
            }
            return false;
        }

        // 連想リストの元となったキー文字を取得する
        inline mchar_t GetKey() const {
            return currentEntry ? currentEntry->GetKey() : 0;
        }

        // chをキーとする連想リストを取得し、その中から指定された tgt を選択する。tgt が存在しなければ末尾に追加する
        void SelectTarget(mchar_t ch, mchar_t tgt) {
            if (FindEntry(ch)) {
                currentEntry->SelectTarget(tgt);
            }
        }

        // 現在処理中の10個の枠から (n % 10) 番目を選択して返す
        const MString SelectNthTarget(size_t n) {
            n %= N_SUB_LIST;
            MString result = strList[n];
            if (currentEntry) {
                currentEntry->SelectNthTarget(n + row * N_SUB_LIST);
                //currentEntry->CopySubList(strList, row * N_SUB_LIST, N_SUB_LIST); // これは不要
            }
            return result;
        }

        // 次の10個の枠を処理対象とする
        void NextCandidates() {
            if (currentEntry) {
                if (currentEntry->CopySubList(strList, (row + 1) * N_SUB_LIST, N_SUB_LIST)) {
                    ++row;
                }
            }
        }

        // 前の10個の枠を処理対象とする
        void PrevCandidates() {
            if (currentEntry) {
                if (row > 0) {
                    --row;
                    currentEntry->CopySubList(strList, row * N_SUB_LIST, N_SUB_LIST);
                }
            }
        }

        // 現在処理中の候補枠に格納されている文字の配列を取得する(1文字をMStringで表している)
        void CopyStrList(std::vector<MString>& list) {
            for (size_t i = 0; i < list.size(); ++i) {
                list[i] = (i < strList.size()) ? strList[i] : emptyStr;
            }
        }

        // 現在処理中の候補枠に格納されている文字の配列を取得する(1文字をMStringで表している)
        inline const std::vector<MString>& GetStrList() const {
            return strList;
        }

    };
    DEFINE_CLASS_LOGGER(CurrentAssocList);

    // -------------------------------------------------------------------
    // 部首連想入力機能状態クラス (最初から候補一覧表示)
    class BushuAssocState : public State {
        DECLARE_CLASS_LOGGER;

    protected:
        CurrentAssocList currentList;

    public:
        // コンストラクタ
        BushuAssocState(BushuAssocNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~BushuAssocState() { };

#define MY_NODE ((BushuAssocNode*)pNode)
#define NAME_PTR (Name.c_str())

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);

            size_t totalCnt = STATE_COMMON->GetTotalDecKeyCount();
            mchar_t outChar = OUTPUT_STACK->isLastOutputStackCharBlocker() ? 0 : OUTPUT_STACK->LastOutStackChar();

            // 直前の出力文字と比較して、部首連想のやり直しをする
            _LOG_DEBUGH(_T("DeckeyCount=%d, PrevTotalCount=%d, AssocCount=%d, outChar=%c, PrevAssoc=%c, PrevKey=%c"), totalCnt, EX_NODE->PrevTotalCount, EX_NODE->Count, SAFE_CHAR(outChar), SAFE_CHAR(EX_NODE->PrevAssoc), SAFE_CHAR(EX_NODE->PrevKey));
            if (EX_NODE->PrevKey != 0 && totalCnt <= EX_NODE->PrevTotalCount + 2 && EX_NODE->PrevAssoc == outChar) {
                outChar = EX_NODE->PrevKey;
                STATE_COMMON->SetOutString(outChar, 1);  // 出力文字も元に戻す
            } else {
                EX_NODE->Count = 0;
            }

            // outChar から連想される文字のリストを取得する
            if (outChar != 0 && currentList.FindEntry(outChar)) {
                setVkbCandidatesList();
                // 前状態にチェインする
                // STATE_COMMON->SetOutStringProcDone();        // ここでやってもよいが、「最終的な出力履歴が整ったところで呼び出される処理」のところでも必要になる
                _LOG_DEBUGH(_T("LEAVE: %s: CHAIN"), NAME_PTR);
                return true;
            }

            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
            // チェイン不要
            return false;
        }

        // Strokeキー を処理する
        void handleStrokeKeys(int deckey) {
            _LOG_DEBUGH(_T("CALLED: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
            //EX_NODE->PrevAssocSec = utils::getSecondsFromEpochTime();
            EX_NODE->PrevTotalCount = STATE_COMMON->GetTotalDecKeyCount();
            EX_NODE->Count = 10;    // 10 は最大値の意味で使っている
            //const MString& word = currentList.SelectNthTarget(deckey >= STROKE_SPACE_DECKEY ? 0 : deckey);    // スペース以上なら先頭を選択
            if (deckey >= STROKE_SPACE_DECKEY) {
                // スペース以上ならそのまま
                setVkbCandidatesList();
                return;
            }
            MString word = currentList.SelectNthTarget(deckey);
            STATE_COMMON->SetOutString(word);
            if (!word.empty()) {
                STATE_COMMON->SetBackspaceNum(1);
                //選択した文字を履歴に登録
                if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(utils::last_substr(word, 1));
            }
            // ストロークヘルプも表示(true)
            handleKeyPostProc(true);
            EX_NODE->PrevKey = currentList.GetKey();
            EX_NODE->PrevAssoc = utils::safe_front(word);
        }

        //void handleSpaceKey() {
        //    LOG_DEBUG(_T("CALLED"));
        //    STATE_COMMON->OutputOrigString();
        //    handleKeyPostProc();
        //}

        //// Ctrl-Space の処理 -- 第1候補を返す
        //void handleCtrlSpace() {
        //    LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
        //    handleStrokeKeys(20);   // 'a'
        //    handleKeyPostProc();
        //}

        //// Shift-Space の処理 -- 第1候補を返す
        //void handleShiftSpace() {
        //    LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
        //    handleStrokeKeys(20);   // 'a'
        //    handleKeyPostProc();
        //}

        //// Ctrl-Shift-Space の処理 -- 第1候補を返す
        //void handleCtrlShiftSpace() {
        //    LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
        //    handleStrokeKeys(20);   // 'a'
        //    handleKeyPostProc();
        //}

        // NextCandTrigger の処理 -- 第1候補を返す
        void handleNextCandTrigger() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            handleStrokeKeys(20);   // 'a'
            handleKeyPostProc();
        }

        // PrevCandTrigger の処理 -- 第1候補を返す
        void handlePrevCandTrigger() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            handleStrokeKeys(20);   // 'a'
            handleKeyPostProc();
        }

        // RET/Enter の処理 -- 第1候補を返す
        void handleEnter() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            handleStrokeKeys(20);   // 'a'
            handleKeyPostProc();
        }

        void handleLeftArrow() {
            currentList.PrevCandidates();
            setVkbCandidatesList();
        }

        void handleRightArrow() {
            currentList.NextCandidates();
            setVkbCandidatesList();
        }

        // Ctrl-H/BS の処理 -- 処理のキャンセル
        void handleBS() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

        //void handleCtrlH() {
        //    LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
        //    handleBS();
        //}

        // FullEscapeの処理 -- 処理のキャンセル
        void handleFullEscape() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

        void handleCtrlKeys(int /*deckey*/) {
            setVkbCandidatesList();
        }

        void handleSpecialKeys(int /*deckey*/) {
            setVkbCandidatesList();
        }

        // 最終的な出力履歴が整ったところで呼び出される処理
        void DoOutStringProc() {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
            STATE_COMMON->SetOutStringProcDone();   // 何かキー入力により再表示の可能性があるので、ここでも必要(この後は、もはや履歴検索などは不要)
            _LOG_DEBUGH(_T("LEAVE: %s, IsOutStringProcDone=%s"), NAME_PTR, BOOL_TO_WPTR(STATE_COMMON->IsOutStringProcDone()));
        }

    protected:
        void handleKeyPostProc(bool bStrokeHelp = false) {
            STATE_COMMON->ClearVkbLayout();
            //STATE_COMMON->RemoveFunctionState();
            bUnnecessary = true;
            if (bStrokeHelp) copyStrokeHelpToVkbFaces();
        }

    protected:
        // 候補一覧を仮想鍵盤にセットする
        void setVkbCandidatesList() {
            STATE_COMMON->SetVirtualKeyboardStrings(VkbLayout::Vertical, MY_NODE->BushuAssocNode::getString() + currentList.GetKey(), currentList.GetStrList());
            // 中央鍵盤背景色の設定 
            STATE_COMMON->SetAssocCandSelecting();
            // この処理は、GUI側で矢印キーをホットキーにするために必要(-1 を指定しているので先頭候補の背景色を変更)
            STATE_COMMON->SetWaitingCandSelect(-1);
        }
    };
    DEFINE_CLASS_LOGGER(BushuAssocState);

#undef MY_NODE
#undef NAME_PTR

    // -------------------------------------------------------------------
    // 拡長部首連想入力機能状態クラス (最初と2回目までは候補の選択、3回目で一覧表示)
    class BushuAssocExState : public BushuAssocState {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        BushuAssocExState(BushuAssocExNode* pN) : BushuAssocState(pN) {
            LOG_INFO(_T("CALLED"));
            Name = logger.ClassNameT();
        }

        ~BushuAssocExState() { };

#define NAME_PTR (Name.c_str())
#ifndef _DEBUG
#define ALLOWANCE_SEC 5
#else
#define ALLOWANCE_SEC 3600
#endif
        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER: %s"), NAME_PTR);
            size_t totalCnt = STATE_COMMON->GetTotalDecKeyCount();
            //_LOG_DEBUGH(_T("ENTER: %s, count=%d"), NAME_PTR, cnt);

            mchar_t outChar = OUTPUT_STACK->isLastOutputStackCharBlocker() ? 0 : OUTPUT_STACK->LastOutStackChar();

            if (outChar < 0x100) {
                STATE_COMMON->OutputDeckeyChar();                 // 自分自身を出力
                //if (outChar == ' ') STATE_COMMON->SetBackspaceNum(1);   // 直前文字がスペースの場合は、それを削除する ⇒やめた。Ctrl-Gまたは'\'の後に':'を打てばよい(2021/6/3)
            } else {
                // 直前の部首合成文字と比較して、やり直しをする
                //time_t now = utils::getSecondsFromEpochTime();
                if (BUSHU_COMP_NODE) _LOG_DEBUGH(_T("DeckeyCount=%d, PrevTotalCount=%d, outChar=%c, PrevComp=%c"), totalCnt, BUSHU_COMP_NODE->PrevTotalCount, SAFE_CHAR(outChar), SAFE_CHAR(BUSHU_COMP_NODE->PrevComp));
                if (BUSHU_COMP_NODE && totalCnt <= BUSHU_COMP_NODE->PrevTotalCount + 2 && BUSHU_COMP_NODE->PrevComp == outChar) {
                    mchar_t m1 = BUSHU_COMP_NODE->PrevBushu1;
                    mchar_t m2 = BUSHU_COMP_NODE->PrevBushu2;
                    if (BUSHU_COMP_NODE->IsPrevAuto) {
                        // 直前の処理は自動部首合成だったので、合成元の2文字に戻す
                        // 出力文字列と削除文字のセット
                        STATE_COMMON->SetOutString(make_mstring(m1, m2), 1);
                        // チェインなし
                        return false;
                    } else {
                        MString cs = BUSHU_COMP_NODE->ReduceByBushu(m1, m2, outChar);   // outChar を探し、さらにその次の候補を返す
                        if (!cs.empty()) {
                            // 出力文字列と削除文字のセット
                            STATE_COMMON->SetOutString(cs, 1);
                            copyStrokeHelpToVkbFaces();
                            //やり直し合成した文字を履歴に登録
                            if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(utils::last_substr(cs, 1));
                            _LOG_DEBUGH(_T("LEAVE: %s: Reduce by using swapped bushu"), NAME_PTR);
                            return false;
                        }
                    }
                }

                // 直前の出力文字と比較して、部首連想のやり直しをする
                _LOG_DEBUGH(_T("DeckeyCount=%d, PrevTotalCount=%d, AssocCount=%d, outChar=%c, PrevAssoc=%c, PrevKey=%c"), totalCnt, EX_NODE->PrevTotalCount, EX_NODE->Count, SAFE_CHAR(outChar), SAFE_CHAR(EX_NODE->PrevAssoc), SAFE_CHAR(EX_NODE->PrevKey));
                if (EX_NODE->PrevKey != 0 && totalCnt <= EX_NODE->PrevTotalCount + 2 && EX_NODE->PrevAssoc == outChar) {
                    outChar = EX_NODE->PrevKey;
                } else {
                    EX_NODE->Count = 0;
                }
                _LOG_DEBUGH(_T("Count=%d, outChar=%c, "), EX_NODE->Count, SAFE_CHAR(outChar));

                if (outChar != 0 && currentList.FindEntry(outChar)) {
                    size_t cnt = EX_NODE->Count;
                    if (cnt < 2) {
                        _LOG_DEBUGH(_T("SELECT HEAD: count=%d"), cnt);
                        // 2回目までなら先頭または2文字目を返す(この中でカウントが10に設定されしまう)
                        handleStrokeKeys(cnt);
                        // カウントを更新
                        EX_NODE->Count = cnt + 1;
                    } else {
                        _LOG_DEBUGH(_T("REVERT: %c"), outChar);
                        if (EX_NODE->Count == 2) currentList.SelectNthTarget(1);         // 後ろに回った文字を選択することで、優先順を元に戻しておく
                        currentList.FindEntry(outChar);
                        //STATE_COMMON->outString.resize(1);
                        STATE_COMMON->SetOutString(outChar, 1);  // 出力文字も元に戻す

                        setVkbCandidatesList();

                        // 前状態にチェインする
                        _LOG_DEBUGH(_T("CHAIN"));
                        return true;
                    }
                }
            }
            _LOG_DEBUGH(_T("LEAVE: %s"), NAME_PTR);
            // チェイン不要
            return false;
        }

        //// Strokeキー を処理する
        //void handleStrokeKeys(int deckey) {
        //    _LOG_DEBUGH(_T("CALLED: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
        //    //bool bRetry = EX_NODE->PrevKey == currentList.GetKey();
        //    //const MString& word = currentList.SelectNthTarget(deckey >= STROKE_SPACE_DECKEY ? (bRetry ? 1 : 0) : deckey);    // スペース以上なら先頭を選択
        //    //STATE_COMMON->SetOutString(word);
        //    //if (!word.empty()) {
        //    //    STATE_COMMON->SetBackspaceNum(1);
        //    //    //選択した文字を履歴に登録
        //    //    if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(utils::last_substr(word, 1));
        //    //}
        //    //EX_NODE->PrevKey = currentList.GetKey();
        //    //EX_NODE->PrevAssoc = utils::safe_front(word);
        //    //handleKeyPostProc(true);
        //}

    };
    DEFINE_CLASS_LOGGER(BushuAssocExState);

} // namespace

// -------------------------------------------------------------------
// BushuCompExNode - 部首連想入力機能ノード
DEFINE_CLASS_LOGGER(BushuAssocExNode);

// コンストラクタ
BushuAssocExNode::BushuAssocExNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
BushuAssocExNode::~BushuAssocExNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* BushuAssocExNode::CreateState() {
    return new BushuAssocExState(this);
}

BushuAssocExNode* BushuAssocExNode::Singleton;

// -------------------------------------------------------------------
// BushuAssocNodeBuilder - 拡張部首連想入力機能ノードビルダー
DEFINE_CLASS_LOGGER(BushuAssocExNodeBuilder);

Node* BushuAssocExNodeBuilder::CreateNode() {
    LOG_INFO(_T("CALLED"));
    EX_NODE = new BushuAssocExNode();
    return EX_NODE;
}

// -------------------------------------------------------------------
// BushuCompNode - 部首連想入力機能ノード
DEFINE_CLASS_LOGGER(BushuAssocNode);

// コンストラクタ
BushuAssocNode::BushuAssocNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
BushuAssocNode::~BushuAssocNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* BushuAssocNode::CreateState() {
    return new BushuAssocState(this);
}


// -------------------------------------------------------------------
// BushuAssocNodeBuilder - 拡張部首連想入力機能ノードビルダー
DEFINE_CLASS_LOGGER(BushuAssocNodeBuilder);

Node* BushuAssocNodeBuilder::CreateNode() {
    LOG_INFO(_T("CALLED"));
    // 部首連想辞書の読み込み(ファイルが指定されていなくても、辞書は構築する)
    // 部首連想入力辞書ファイル名
    auto bushuAssocFile = SETTINGS->bushuAssocFile;
    LOG_INFO(_T("bushuAssoc=%s"), bushuAssocFile.c_str());
    //if (bushuAssocFile.empty()) {
    //    ERROR_HANDLER->Warn(_T("「bushuAssoc=(ファイル名)」の設定がまちがっているようです"));
    //}
    BushuAssocDic::CreateBushuAssocDic(bushuAssocFile);
    return new BushuAssocNode();
}
