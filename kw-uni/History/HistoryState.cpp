#include "Logger.h"
#include "string_utils.h"

#include "Settings.h"
#include "StrokeHelp.h"

#include "State.h"
#include "HistoryStateBase.h"
#include "History.h"
#include "Merger.h"

#if 1
#undef LOG_INFO
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#endif

namespace {

    // -------------------------------------------------------------------
    // 履歴入力機能状態クラス
    class HistoryState : public State {
        DECLARE_CLASS_LOGGER;

        //bool bWaitingForNum = false;

    protected:
        //int candLen = 0;
        std::unique_ptr<HistoryStateBase> histBase;

    protected:
        //MStringResult& resultString() override { return resultStr; }

    public:
        // コンストラクタ
        HistoryState(HistoryNode* pN) : histBase(HistoryStateBase::createInstance(pN)) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~HistoryState() { };

        // 機能状態に対して生成時処理を実行する
        // ここに来る場合には、以下の3つの状態がありえる:
        // ①まだ履歴検索がなされていない状態
        // ②検索が実行されたが、出力文字列にはキーだけが表示されている状態
        // ③横列のどれかの候補が選択されて出力文字列に反映されている状態
        void DoProcOnCreated() override {
            _LOG_DEBUGH(_T("ENTER"));

            if (!HISTORY_DIC) return;

            // 過去の履歴候補選択の結果を反映しておく
            HIST_CAND->DelayedPushFrontSelectedWord();

            // 末尾1文字の登録
            auto ws = OUTPUT_STACK->GetLastOutputStackStrUptoNL(1);
            if (ws.size() == 1 && ws[0] >= 0x100 && !STROKE_HELP->Find(ws[0])) {
                // 末尾文字がストローク可能文字でなければ登録する
                HISTORY_DIC->AddNewEntry(ws);
            }

            MString key;
            if (HIST_CAND->IsHistInSearch()) {
                _LOG_DEBUGH(_T("History in Search"));
                // 検索を実行済みなら、前回の履歴検索キー取得と出力スタックの巻き戻し予約(numBackSpacesに値をセット)
                key = histBase->getLastHistKeyAndRewindOutput(resultStr);
            }
            if (key.empty()) {
                _LOG_DEBUGH(_T("History key is EMPTY: CALL OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey()"));
                // まだ検索していなければ、出力文字列から、検索キーを取得(ひらがな交じりやASCIIもキーとして取得する)
                key = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>(SETTINGS->histMapKeyMaxLength);
                //key = STATE_COMMON->GetLastKanjiOrKatakanaKey();
            }
            _LOG_DEBUGH(_T("new Japanese key={}"), to_wstr(key));

            // 履歴入力候補の取得
            //candLen = 0;
            histBase->setCandidatesVKB(VkbLayout::Vertical, 0 /*HIST_CAND->GetCandWords(key, false, candLen)*/, key);

            // 検索キーの設定
            STROKE_MERGER_NODE->SetPrevHistKeyState(HIST_CAND->GetOrigKey());

            // 未選択状態にセットする
            _LOG_DEBUGH(_T("Set Unselected"));
            STATE_COMMON->SetWaitingCandSelect(-1);

            MarkNecessary();
            _LOG_DEBUGH(_T("LEAVE: Chain"));
        }

        // 最終的な出力履歴が整ったところで呼び出される処理
        void DoLastHistoryProc() override {
            _LOG_DEBUGH(_T("ENTER: {}"), Name);

            histBase->DoLastHistoryProc();

            STATE_COMMON->SetOutStringProcDone();
            _LOG_DEBUGH(_T("LEAVE: {}, IsOutStringProcDone={}"), Name, STATE_COMMON->IsOutStringProcDone());
        }

        // 履歴検索を初期化する状態か
        bool IsHistoryReset() override {
            bool result = (NextState() && NextState()->IsHistoryReset());
            _LOG_DEBUGH(_T("CALLED: {}: result={}"), Name, result);
            return result;
        }

         // Strokeキー を処理する
        void handleStrokeKeys(int deckey) override {
            _LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({})"), Name, deckey, deckey);
            if (histBase->handleStrokeKeys(deckey, resultStr)) {
                _LOG_DEBUGH(_T("CALL handleKeyPostProc"));
                handleKeyPostProc();
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        //void handleSpaceKey() {
        //    LOG_DEBUG(_T("CALLED"));
        //    STATE_COMMON->OutputOrigString();
        //    handleKeyPostProc();
        //}

        // 機能キーだったときの一括処理(false を返すと、この後、個々の機能キーのハンドラが呼ばれる)
        bool handleFunctionKeys(int deckey) override {
            _LOG_DEBUGH(_T("CALLED"));
            return histBase->handleFunctionKeys(deckey);
        }

        void handleDownArrow() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            histBase->handleDownArrow();
        }

        void handleUpArrow() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            histBase->handleUpArrow();
        }

        void handleLeftArrow() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            histBase->handleLeftArrow();
        }

        void handleRightArrow() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            histBase->handleRightArrow();
        }

        // RET/Enter の処理 -- 第1候補を返す
        void handleEnter() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleStrokeKeys(20);   // 'a' -- 縦列候補の左端を示す
            handleKeyPostProc();
        }

        //// Shift+Space の処理 -- 第1候補を返す
        //void handleShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleEnter();
        //}

        //// Ctrl+Space の処理 -- 第1候補を返す
        //void handleCtrlSpace() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleEnter();
        //}

        // NextCandTrigger の処理 -- 第1候補を返す
        void handleNextCandTrigger() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleEnter();
        }

        // PrevCandTrigger の処理 -- 第1候補を返す
        void handlePrevCandTrigger() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleEnter();
        }


        // FullEscapeの処理 -- 処理のキャンセル
        void handleFullEscape() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            histBase->setBlocker();
            handleKeyPostProc();
        }

        // Ctrl-H/BS の処理 -- 処理のキャンセル
        void handleBS() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleKeyPostProc();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleKeyPostProc();
        }

        // ストロークのクリア -- 処理のキャンセル
        void handleClearStroke() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleKeyPostProc();
        }

    protected:
        void handleKeyPostProc() {
            _LOG_DEBUGH(_T("CALLED: handleKeyPostProc"));
            STROKE_MERGER_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();
            STATE_COMMON->ClearVkbLayout();
            //STATE_COMMON->RemoveFunctionState();
            MarkUnnecessary();
        }

    };
    DEFINE_CLASS_LOGGER(HistoryState);

    // -------------------------------------------------------------------
    // 2～3文字履歴機能状態クラス
    class HistoryFewCharsState : public HistoryState {
        DECLARE_CLASS_LOGGER;
    public:
        // コンストラクタ
        HistoryFewCharsState(HistoryFewCharsNode* pN) : HistoryState(pN) {
            LOG_INFO(_T("CALLED"));
            Name = logger.ClassNameT();
        }

        ~HistoryFewCharsState() { };

        // 機能状態に対して生成時処理を実行する
        void DoProcOnCreated() override {
            _LOG_DEBUGH(_T("CALLED"));

            if (!HISTORY_DIC) return;

            // 前回履歴キーのクリア
            STROKE_MERGER_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();

            // 2～3文字履歴の取得
            MString key;
            //candLen = -3;
            histBase->setCandidatesVKB(VkbLayout::Vertical, -3 /*HIST_CAND->GetCandWords(key, false, candLen)*/, key);

            MarkNecessary();
        }

    };
    DEFINE_CLASS_LOGGER(HistoryFewCharsState);

    // -------------------------------------------------------------------
    // 1文字履歴機能状態クラス
    class HistoryOneCharState : public HistoryState {
        DECLARE_CLASS_LOGGER;
    public:
        // コンストラクタ
        HistoryOneCharState(HistoryOneCharNode* pN) : HistoryState(pN) {
            LOG_INFO(_T("CALLED"));
            Name = logger.ClassNameT();
        }

        ~HistoryOneCharState() { };

        // 機能状態に対して生成時処理を実行する
        void DoProcOnCreated() override {
            _LOG_DEBUGH(_T("CALLED"));

            if (!HISTORY_DIC) return;

            // 前回履歴キーのクリア
            STROKE_MERGER_NODE->ClearPrevHistState();
            HIST_CAND->ClearKeyInfo();

            // 1文字履歴の取得
            MString key;
            //candLen = 1;
            histBase->setCandidatesVKB(VkbLayout::Vertical, 1 /*HIST_CAND->GetCandWords(key, false, candLen)*/, key);

            MarkNecessary();
        }

    };
    DEFINE_CLASS_LOGGER(HistoryOneCharState);

} // namespace

// -------------------------------------------------------------------
// HistoryNode - 履歴入力機能ノード
DEFINE_CLASS_LOGGER(HistoryNode);

// コンストラクタ
HistoryNode::HistoryNode() {
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
HistoryNode::~HistoryNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* HistoryNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    if (SETTINGS->multiStreamMode) return 0;
    return new HistoryState(this);
}

HistoryNode* HistoryNode::Singleton;

// -------------------------------------------------------------------
// HistoryNodeBuilder - 履歴入力機能ノードビルダー
DEFINE_CLASS_LOGGER(HistoryNodeBuilder);

Node* HistoryNodeBuilder::CreateNode() {
    //// 履歴入力辞書ファイル名
    //auto histFile = SETTINGS->historyFile;
    //LOG_DEBUGH(_T("histFile={}"), histFile);
    ////if (histFile.empty()) {
    ////    ERROR_HANDLER->Warn(_T("「history=(ファイル名)」の設定がまちがっているようです"));
    ////}
    //// 履歴入力辞書の読み込み(ファイル名の指定がなくても辞書自体は構築する)
    //LOG_DEBUGH(_T("CALLED: histFile={}"), histFile);
    //HistoryDic::CreateHistoryDic(histFile);

    HISTORY_NODE = new HistoryNode();
    return HISTORY_NODE;
}

// -------------------------------------------------------------------
// HistoryFewCharsNode - 2～3文字履歴機能ノード
DEFINE_CLASS_LOGGER(HistoryFewCharsNode);

// コンストラクタ
HistoryFewCharsNode::HistoryFewCharsNode() {
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
HistoryFewCharsNode::~HistoryFewCharsNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* HistoryFewCharsNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    if (SETTINGS->multiStreamMode) return 0;
    return new HistoryFewCharsState(this);
}

// -------------------------------------------------------------------
// HistoryFewCharsNodeBuilder - 2～3文字履歴機能ノードビルダー
DEFINE_CLASS_LOGGER(HistoryFewCharsNodeBuilder);

Node* HistoryFewCharsNodeBuilder::CreateNode() {
    return new HistoryFewCharsNode();
}

// -------------------------------------------------------------------
// HistoryOneCharNode - 1文字履歴機能ノード
DEFINE_CLASS_LOGGER(HistoryOneCharNode);

// コンストラクタ
HistoryOneCharNode::HistoryOneCharNode() {
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
HistoryOneCharNode::~HistoryOneCharNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* HistoryOneCharNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    if (SETTINGS->multiStreamMode) return 0;
    return new HistoryOneCharState(this);
}

// -------------------------------------------------------------------
// HistoryOneCharNodeBuilder - 1文字履歴機能ノードビルダー
DEFINE_CLASS_LOGGER(HistoryOneCharNodeBuilder);

Node* HistoryOneCharNodeBuilder::CreateNode() {
    return new HistoryOneCharNode();
}

