//#include "pch.h"
#include "Logger.h"
#include "string_type.h"
#include "file_utils.h"
#include "path_utils.h"

#include "KanchokuIni.h"
#include "Constants.h"
#include "deckey_id_defs.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "Node.h"
#include "State.h"
#include "OutputStack.h"
#include "History/HistoryDic.h"

#include "Mazegaki.h"
#include "MazegakiDic.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughMazegaki)

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

namespace {

    // -------------------------------------------------------------------
    // 交ぜ書き候補リストのクラス
    class MazeCandidates {
        DECLARE_CLASS_LOGGER;

        // 全候補リスト
        std::vector<MString> mazeCandidates;

        std::map<MString, size_t> cand2len;

        MString firstCandYomi;

    public:
        // 読みに対する全交ぜ書き候補を取得する
        const std::vector<MString>& GetAllCandidates(const MString& yomiFull) {
            LOG_INFOH(_T("ENTER: yomiFull=%s"), MAKE_WPTR(yomiFull));
            mazeCandidates.clear();
            cand2len.clear();
            firstCandYomi.clear();

            size_t maxlen = yomiFull.size();
            if (maxlen > 0) {
                for (size_t len = SETTINGS->mazeYomiMaxLen; len > 0; --len) {
                    if (len > maxlen) continue;

                    // 指定の長さの読みに対する交ぜ書き候補を取得して追加
                    MString yomi = utils::last_substr(yomiFull, len);
                    _LOG_DEBUGH(_T("CALL: yomi=%s"), MAKE_WPTR(yomi));
                    bool bWild = false;
                    if (is_wildcard(yomi[0])) {
                        // ワイルドカード文字が読みの先頭に来るのは、直前の文字がひながな、カタカナ、漢字以外の文字のときだけ
                        if (len < maxlen && utils::is_japanese_char_except_nakaguro(yomiFull[maxlen - (len + 1)])) {
                            _LOG_DEBUGH(_T("SKIP: yomi=%s"), MAKE_WPTR(yomi));
                            break;
                        }
                        bWild = true;   // ワイルドカードより後は処理を行わない(この後でループから抜ける)
                    }
                    //const auto& cands = MAZEGAKI_DIC->GetCandidates(yomi);
                    //utils::append(mazeCandidates, cands);
                    //for (const auto& w : cands) { cand2len[w] = len; }
                    //if (firstCandYomi.empty() && !cands.empty()) {
                    //    firstCandYomi.assign(yomi);
                    //}
                    for (const auto& w : MAZEGAKI_DIC->GetCandidates(yomi)) {
                        if (utils::find(mazeCandidates, w) == 0) {
                            mazeCandidates.push_back(w);
                            cand2len[w] = len;
                            if (firstCandYomi.empty()) firstCandYomi.assign(yomi);
                        }
                    }

                    // 先頭がワイルドカードだったら、それ以降はやらない
                    if (bWild) break;
                }
            }
            _LOG_DEBUGH(_T("LEAVE: mazeCandidates=%s"), MAKE_WPTR(utils::join(mazeCandidates, '/')));
            return mazeCandidates;
        }

        // 取得済みの交ぜ書き候補リストを返す
        inline const std::vector<MString>& GetMazeCandidates() const {
            return mazeCandidates;
        }

        //// 取得済みの交ぜ書き候補リストの n番目のウィンドウを返す
        //inline const std::vector<MString>& GetMazeCandidatesByWindow(size_t n) const {
        //    return mazeCandidates;
        //}

        // 最初の候補に対する読みを返す
        inline const MString& GetFirstCandidateYomi() const {
            return firstCandYomi;
        }

        // 指定の候補に対する読みの長さを返す
        size_t GetYomiLen(const MString& cand) const {
            auto iter = cand2len.find(cand);
            return (iter != cand2len.end()) ? iter->second : 0;
        }

        // n番目の候補を選択 ⇒ 必要ならユーザー辞書に追加
        void SelectNth(size_t n) {
            if (n > 0 && n < mazeCandidates.size()) {
                size_t len = GetYomiLen(mazeCandidates[n]);
                if (GetYomiLen(mazeCandidates[n - 1]) == len) {
                    // 直前の候補と読み長が同じ、つまり、同じ読みの中で先頭ではなかった
                    // 再検索して、ユーザー辞書に追加する
                    MAZEGAKI_DIC->GetCandidates(utils::last_substr(firstCandYomi, len));
                    MAZEGAKI_DIC->SelectCadidate(mazeCandidates[n]);
                }
            }
        }
    };
    DEFINE_CLASS_LOGGER(MazeCandidates);


    // -------------------------------------------------------------------
    // 交ぜ書き機能状態クラス
    class MazegakiState : public State {
        DECLARE_CLASS_LOGGER;

        // 交ぜ書き候補のリスト
        MazeCandidates candsByLen;

        // ウィンドウ番号 (ウィンドウとは10個ごとの候補の枠のこと)
        size_t winIdx = 0;

    public:
        // コンストラクタ
        MazegakiState(MazegakiNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~MazegakiState() { };

#define NAME_PTR (Name.c_str())
#define MY_NODE ((MazegakiNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER"));

            if (!MAZEGAKI_DIC) return false;

            // 直前に変換していたか
            MString prevYomi;
            size_t prevXferLen = MAZEGAKI_NODE->GetPrevYomiInfo(prevYomi);

            bool prevXfered = !prevYomi.empty() && prevXferLen > 0;

            size_t shiftedYomiLen = prevXfered ? MAZEGAKI_NODE->GetShiftedYomiLen() : 1000;
            _LOG_DEBUGH(_T("prevYomi=%s, prevXferLen=%d, shiftedYomiLen=%d"), MAKE_WPTR(prevYomi), prevXferLen, shiftedYomiLen);

            // 最大読み長までの長さの読みに対する交ぜ書き候補を全て取得する
            OUTPUT_STACK->unsetMazeBlocker();
            const auto& cands = candsByLen.GetAllCandidates(
                prevXfered ? utils::tail_substr(prevYomi, shiftedYomiLen) : OUTPUT_STACK->BackStringUptoMazeOrHistBlockerOrPunct(SETTINGS->mazeYomiMaxLen));
            if (cands.empty()) {
                // チェイン不要
                _LOG_DEBUGH(_T("LEAVE: no candidate"));
                return false;
            }
            LOG_INFOH(_T("mazegakiSelectFirstCand: %s"), BOOL_TO_WPTR(SETTINGS->mazegakiSelectFirstCand));
            if (cands.size() == 1 || (!MAZEGAKI_NODE->IsSelectFirstCandDisabled() && SETTINGS->mazegakiSelectFirstCand)) {
                // 読みの長さ候補が１つしかなかった、または先頭候補の自動出力モードなのでそれを選択して出力
                if (prevXfered)
                    outputStringAndPostProc(prevYomi.substr(0, prevYomi.size() - shiftedYomiLen) + cands.front(), prevXferLen, &prevYomi);
                else
                    outputStringAndPostProc(cands.front(), candsByLen.GetFirstCandidateYomi().size(), 0);
                // チェイン不要
                _LOG_DEBUGH(_T("LEAVE: one candidate"));
                return false;
            }
            // 直前の変換があればそれを取り消す
            if (prevXfered) {
                STATE_COMMON->SetOutString(prevYomi, prevXferLen);
            }
            // 候補があったので仮想鍵盤に表示
            setCandidatesVKB();
            // 前状態にチェインする
            _LOG_DEBUGH(_T("LEAVE: %d candidates"), cands.size());
            return true;
        }

         // Strokeキー を処理する
        void handleStrokeKeys(int deckey) {
            _LOG_DEBUGH(_T("CALLED: %s: deckey=%xH(%d)"), NAME_PTR, deckey, deckey);
            //if (deckey == SHIFT_SPACE_DECKEY) deckey = 0; // Shift+Spaceの場合を想定
            if (deckey <= STROKE_SPACE_DECKEY) {
                size_t n = (winIdx * LONG_VKEY_NUM) + (deckey % LONG_VKEY_NUM);
                const auto& cands = candsByLen.GetMazeCandidates();
                if (n < cands.size()) {
                    outputStringAndPostProc(cands[n], candsByLen.GetYomiLen(cands[n]), 0);
                    candsByLen.SelectNth(n);
                    return;
                }
            }
            setCandidatesVKB();
        }

         // Shiftキーで修飾されたキー
        void handleShiftKeys(int deckey) {
            _LOG_DEBUGH(_T("CALLED: %s: deckey=%xH(%d), char=%c"), NAME_PTR, deckey, deckey);
            //handleKeyPostProc();
            //State::handleShiftKeys(deckey);
            handleStrokeKeys(UNSHIFT_DECKEY(deckey));
        }

        void handleLeftArrow() {
            if (winIdx > 0) --winIdx;
            setCandidatesVKB();
        }

        void handleRightArrow() {
            if ((winIdx + 1) * 10 < candsByLen.GetMazeCandidates().size()) ++winIdx;
            setCandidatesVKB();
        }

        void handleUpArrow() {
            handleLeftArrow();
        }

        void handleDownArrow() {
            handleRightArrow();
        }

        //// Ctrl-Space の処理 -- 第1候補を返す
        //void handleCtrlSpace() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleStrokeKeys(0);
        //}

        //// Shift-Space の処理 -- 第1候補を返す
        //void handleShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleStrokeKeys(0);
        //}

        //// Ctrl-Shift-Space の処理 -- 第1候補を返す
        //void handleCtrlShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
        //    handleStrokeKeys(0);
        //}

        // NextCandTrigger の処理 -- 第1候補を返す
        void handleNextCandTrigger() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            handleStrokeKeys(0);
        }

        // PrevCandTrigger の処理 -- 第1候補を返す
        void handlePrevCandTrigger() {
            LOG_DEBUG(_T("CALLED: %s"), NAME_PTR);
            handleStrokeKeys(0);
        }

        // RET/Enter の処理 -- 第1候補を返す
        void handleEnter() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleStrokeKeys(0);
        }

        // BS の処理 -- 処理のキャンセル
        void handleBS() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

        // FullEscape の処理 -- 処理のキャンセル
        void handleFullEscape() {
            _LOG_DEBUGH(_T("CALLED: %s"), NAME_PTR);
            handleKeyPostProc();
        }

        // その他のCtrlキー -- 処理のキャンセル
        void handleCtrlKeys(int /*deckey*/) {
            handleKeyPostProc();
        }

        // その他の特殊キー -- 処理のキャンセル
        void handleSpecialKeys(int /*deckey*/) {
            handleKeyPostProc();
        }

    private:
        void outputStringAndPostProc(const MString& str, size_t numBS, const MString* prevYomi) {
            MazegakiNode* pn = dynamic_cast<MazegakiNode*>(pNode);
            if (pn) {
                // 今回の結果を元に戻すための情報を保存
                // prevYomi は、再変換をしたときの元の読み
                pn->SetYomiInfo(prevYomi ? *prevYomi : OUTPUT_STACK->GetLastOutputStackStr(numBS), str.size());
            }
            STATE_COMMON->SetOutString(str, numBS);
            STATE_COMMON->SetMazegakiBlockFlag();
            handleKeyPostProc();
            //選択した候補を履歴に登録
            if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(utils::strip(str, _T("、。")));
        }

        void handleKeyPostProc() {
            STATE_COMMON->ClearVkbLayout();
            //STATE_COMMON->RemoveFunctionState();
            bUnnecessary = true;
        }

        // 交ぜ書き候補を鍵盤にセットする
        void setCandidatesVKB() {
            auto center = pNode->getString() + candsByLen.GetFirstCandidateYomi();
            const auto& cands = candsByLen.GetMazeCandidates();
            if (!cands.empty()) {
                STATE_COMMON->SetVirtualKeyboardStrings(VkbLayout::Vertical, center, cands, winIdx * 10);
                // 「交ぜ書き変換」の色で中央鍵盤の色付け
                STATE_COMMON->SetMazeCandSelecting();
                // 未選択状態にセットし、矢印キーを有効にする
                STATE_COMMON->SetWaitingCandSelect(-1);
                STATE_COMMON->SetOutStringProcDone();   // この後は、もはや履歴検索などは不要
                _LOG_DEBUGH(_T("%s: OutStringProcDone=%s"), NAME_PTR, BOOL_TO_WPTR(STATE_COMMON->IsOutStringProcDone()));
            } else {
                LOG_INFO(_T("No MazeCands"));
            }
        }

    };
    DEFINE_CLASS_LOGGER(MazegakiState);

} // namespace

// -------------------------------------------------------------------
// MazegakiNode - 交ぜ書き機能ノード
DEFINE_CLASS_LOGGER(MazegakiNode);

// Singleton
MazegakiNode* MazegakiNode::Singleton = 0;

// コンストラクタ
MazegakiNode::MazegakiNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
MazegakiNode::~MazegakiNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* MazegakiNode::CreateState() {
    return new MazegakiState(this);
}

// -------------------------------------------------------------------
// MazegakiNodeBuilder - 交ぜ書き機能ノードビルダー

DEFINE_CLASS_LOGGER(MazegakiNodeBuilder);

Node* MazegakiNodeBuilder::CreateNode() {
    // 交ぜ書き辞書ファイル名
    auto mazeFile = SETTINGS->mazegakiFile;
    LOG_INFO(_T("mazeFile=%s"), mazeFile.c_str());
    if (!mazeFile.empty()) {
        // 交ぜ書き辞書の読み込み
        LOG_INFO(_T("CALLED: mazegaiFile=%s"), mazeFile.c_str());
        if (!mazeFile.empty()) {
            MazegakiDic::CreateMazegakiDic(mazeFile);
        }
    }
    //else {
    //    ERROR_HANDLER->Warn(_T("「mazegaki=(ファイル名)」の設定がまちがっているようです"));
    //}
    return MazegakiNode::Singleton = new MazegakiNode();
}

