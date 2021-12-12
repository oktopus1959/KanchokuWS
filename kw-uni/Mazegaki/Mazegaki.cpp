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
        std::vector<MazeResult> mazeCandidates;

        std::map<MString, size_t> cand2len;

        // 最初の変換候補に対する読み
        MString firstCandYomi;

        // 読みの全体
        MString fullYomi;

    public:
        // 読みに対する全交ぜ書き候補を取得する
        const std::vector<MazeResult>& GetAllCandidates(const MString& yomiFull) {
            LOG_INFOH(_T("ENTER: yomiFull=%s"), MAKE_WPTR(yomiFull));
            mazeCandidates.clear();
            cand2len.clear();
            firstCandYomi.clear();
            fullYomi = yomiFull;

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
                        if (utils::find(mazeCandidates, [w](auto c) {return c.resultStr == w.resultStr;}) == 0) {
                            mazeCandidates.push_back(w);
                            cand2len[w.resultStr] = len;
                            if (firstCandYomi.empty()) firstCandYomi.assign(yomi);
                        }
                    }

                    // 先頭がワイルドカードだったら、それ以降はやらない
                    if (bWild) break;
                }
            }
            _LOG_DEBUGH(_T("LEAVE: mazeCandidates=%s"), MAKE_WPTR(utils::join(MazeResult::ToMStringVector(mazeCandidates), '/')));
            return mazeCandidates;
        }

        // 取得済みの交ぜ書き候補リストを返す
        inline const std::vector<MazeResult>& GetMazeCandidates() const {
            return mazeCandidates;
        }

        //// 取得済みの交ぜ書き候補リストの n番目のウィンドウを返す
        //inline const std::vector<MString>& GetMazeCandidatesByWindow(size_t n) const {
        //    return mazeCandidates;
        //}

        // 全読みを返す
        inline const MString& GetFullYomi() const {
            return fullYomi;
        }

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
                size_t len = GetYomiLen(mazeCandidates[n].resultStr);
                if (GetYomiLen(mazeCandidates[n - 1].resultStr) == len) {
                    // 直前の候補と読み長が同じ、つまり、同じ読みの中で先頭ではなかった
                    // 再検索して、ユーザー辞書に追加する
                    MAZEGAKI_DIC->GetCandidates(utils::last_substr(firstCandYomi, len));
                    MAZEGAKI_DIC->SelectCandidate(mazeCandidates[n].resultStr);
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

            _LOG_DEBUGH(_T("A:IsSelectFirstCandDisabled: %s, mazegakiSelectFirstCand: %s"), BOOL_TO_WPTR(MAZEGAKI_NODE->IsSelectFirstCandDisabled()), BOOL_TO_WPTR(SETTINGS->mazegakiSelectFirstCand));
            // ブロッカーがシフトされた直後か
            if (MAZEGAKI_NODE->IsBlockerShifted()) {
                _LOG_DEBUGH(_T("JUST AFTER BLOCKER SHIFTED"));
                return false;
            }
            _LOG_DEBUGH(_T("B:IsSelectFirstCandDisabled: %s, mazegakiSelectFirstCand: %s"), BOOL_TO_WPTR(MAZEGAKI_NODE->IsSelectFirstCandDisabled()), BOOL_TO_WPTR(SETTINGS->mazegakiSelectFirstCand));

            bool prevXfered = false;    // 直前に変換していたか
            MString prevYomi;           // 直前の変換の時の読み
            size_t prevLeadLen = 0;     // 直前のリード部の長さ
            size_t prevOutLen = 0;      // 直前の出力形の長さ

            // 末尾がブロッカーなら、直前の変換をやり直す
            if (OUTPUT_STACK->isLastMazeBlocker()) {
                prevLeadLen = MAZEGAKI_NODE->GetPrevLeadLen();
                prevOutLen = MAZEGAKI_NODE->GetPrevYomiInfo(prevYomi);
                prevXfered = !prevYomi.empty() && prevOutLen > 0;
                _LOG_DEBUGH(_T("LAST MAZE BLOCKER: prevLeadLen=%d, prevOutLen=%d, prevXfered=%s"), prevLeadLen, prevOutLen, BOOL_TO_WPTR(prevXfered));
            }
            _LOG_DEBUGH(_T("C:IsSelectFirstCandDisabled: %s, mazegakiSelectFirstCand: %s"), BOOL_TO_WPTR(MAZEGAKI_NODE->IsSelectFirstCandDisabled()), BOOL_TO_WPTR(SETTINGS->mazegakiSelectFirstCand));

            size_t shiftedYomiLen = prevXfered ? MAZEGAKI_NODE->GetShiftedYomiLen() : 1000;
            _LOG_DEBUGH(_T("prevXfered=%s, prevYomi=%s, prevOutLen=%d, shiftedYomiLen=%d"), BOOL_TO_WPTR(prevXfered), MAKE_WPTR(prevYomi), prevOutLen, shiftedYomiLen);
            _LOG_DEBUGH(_T("D:IsSelectFirstCandDisabled: %s, mazegakiSelectFirstCand: %s"), BOOL_TO_WPTR(MAZEGAKI_NODE->IsSelectFirstCandDisabled()), BOOL_TO_WPTR(SETTINGS->mazegakiSelectFirstCand));

            // 最大読み長までの長さの読みに対する交ぜ書き候補を全て取得する
            OUTPUT_STACK->unsetMazeBlocker();
            auto getPrevFullYomi = [prevYomi, prevLeadLen, prevOutLen, shiftedYomiLen]() {
                if (prevYomi.size() >= shiftedYomiLen) {
                    return utils::tail_substr(prevYomi, shiftedYomiLen);
                } else {
                    if (prevOutLen == 0) return OUTPUT_STACK->BackStringUptoMazeOrHistBlockerOrPunct(SETTINGS->mazeYomiMaxLen);
                    // prevLeadLen が1以上の場合は、直前の状態に戻れる
                    auto leadYomi = OUTPUT_STACK->BackStringUptoMazeOrHistBlockerOrPunct(prevLeadLen + prevOutLen);
                    _LOG_DEBUGH(_T("prevLeadLen=%d, prevOutLen=%d, leadYomi=%s"), prevLeadLen, prevOutLen, MAKE_WPTR(leadYomi));
                    return leadYomi.substr(0, prevLeadLen) + prevYomi;
                }
            };
            MString fullYomi0 = prevXfered ? getPrevFullYomi() : OUTPUT_STACK->BackStringUptoMazeOrHistBlockerOrPunct(SETTINGS->mazeYomiMaxLen);
            _LOG_DEBUGH(_T("fullYomi0='%s'"), MAKE_WPTR(fullYomi0));
            if (fullYomi0.size() < shiftedYomiLen) shiftedYomiLen = fullYomi0.size();
            const std::vector<MazeResult>* pCands = nullptr;
            MString fullYomi = fullYomi0;
            while (true) {
                _LOG_DEBUGH(_T("fullYomi='%s'"), MAKE_WPTR(fullYomi));
                pCands = &candsByLen.GetAllCandidates(fullYomi);
                _LOG_DEBUGH(_T("pCands->size=%d"), pCands->size());
                if (pCands->empty()) {
                    // チェイン不要
                    _LOG_DEBUGH(_T("LEAVE: no candidate"));
                    MAZEGAKI_NODE->SetJustAfterPrevXfer();
                    return false;
                }
                LOG_INFOH(_T("E:IsSelectFirstCandDisabled: %s, mazegakiSelectFirstCand: %s"), BOOL_TO_WPTR(MAZEGAKI_NODE->IsSelectFirstCandDisabled()), BOOL_TO_WPTR(SETTINGS->mazegakiSelectFirstCand));
                //if (!MAZEGAKI_NODE->IsSelectFirstCandDisabled() && SETTINGS->mazegakiSelectFirstCand) {
                //    // 先頭候補の自動出力モードの場合
                //    const auto& cand = pCands->front();
                //    if (!cand.resultStr.empty() && cand.resultStr == fullYomi.substr(0, cand.resultStr.size())) {
                //        // 先頭候補の変換形が読みと一致していた、つまり変化していなかった ⇒ 変換形の(語尾を含んだ)長さのところから再変換する
                //        _LOG_DEBUGH(_T("SAME XFER as yomi: '%s'"), MAKE_WPTR(cand.resultStr));
                //        fullYomi = fullYomi.substr(cand.xferLen);
                //        if (fullYomi.empty()) {
                //            if (prevXfered) {
                //                // 変換位置をずらした別の変換形が得られなかった場合でも、再変換のときはそれを採用する
                //                // 例：「ひどい目にあう」⇒「ひど色目にあう」⇒ '>' ⇒「ひど色|目にあう」<- いまココ
                //                //      なので、「目にあう」を採用し、「ひどい|目にあう」に直す必要あり
                //                break;
                //            }
                //        }
                //        continue;
                //    }
                //}
                break;
            }

            if (pCands->size() == 1 || (!MAZEGAKI_NODE->IsSelectFirstCandDisabled() && SETTINGS->mazegakiSelectFirstCand)) {
                // 読みの長さ候補が１つしかなかった、または先頭候補の自動出力モードなのでそれを選択して出力
                const auto& cand = pCands->front();
                size_t candLen = cand.resultStr.size();
                size_t candYlen = candsByLen.GetFirstCandidateYomi().size();
                _LOG_DEBUGH(_T("candLen=%d, candYlen=%d"), candLen, candYlen);
                if (prevXfered) {
                    MString lead;
                    MString yomi;
                    size_t numBS = prevOutLen;
                    if (prevYomi.size() >= shiftedYomiLen) {
                        lead = prevYomi.substr(0, prevYomi.size() - candYlen);
                        yomi = utils::last_substr(prevYomi, candYlen);
                    } else {
                        yomi = utils::last_substr(fullYomi0, candYlen);
                        lead = fullYomi0.substr(0, fullYomi0.size() - candYlen);
                        numBS += prevLeadLen;
                        size_t maxYomiLen = OUTPUT_STACK->TailSizeUptoMazeOrHistBlockerOrPunct();
                        _LOG_DEBUGH(_T("yomi=%s, lead=%s, prevYomi=%s, shiftedYomiLen=%d, numBS=%d, prevOutLen=%d"), MAKE_WPTR(yomi), MAKE_WPTR(lead), MAKE_WPTR(prevYomi), shiftedYomiLen, numBS, prevOutLen);
                        if (numBS > maxYomiLen) numBS = maxYomiLen;
                    }
                    // 「がかなる」⇒「画家なる」⇒">"⇒「がか奈留」のケース
                    // prevYomi=がかなる
                    // shiftedYomiLen=3 (かなる)
                    // candYlen = 2 (なる)
                    // leadStr=がか
                    // candStr=奈留
                    // numBS=prevOutLen=4 (画家なる)
                    // 今回の読み=なる
                    // candLen=2
                    _LOG_DEBUGH(_T("PREV_XFERED: numBS=%d, prevOutLen=%d"), numBS, prevOutLen);
                    outputStringAndPostProc(lead, cand, numBS, &yomi, candLen);
                } else {
                    if (SETTINGS->mazeRemoveHeadSpace && fullYomi[0] == ' ') {
                        // 全読みの先頭の空白を削除
                        _LOG_DEBUGH(_T("REMOVE_HEAD_SPACE: one cand or select first"));
                        MString leadStr;
                        if ((1 + candYlen) < fullYomi.size()) leadStr = fullYomi.substr(1, fullYomi.size() - (1 + candYlen));
                        outputStringAndPostProc(leadStr, cand, fullYomi.size(), nullptr, 0);
                    } else {
                        _LOG_DEBUGH(_T("CANDS_SIZE=%d, SELECT_FIRST=%s"), pCands->size(), BOOL_TO_WPTR(SETTINGS->mazegakiSelectFirstCand));
                        outputStringAndPostProc(EMPTY_MSTR, cand, candYlen, nullptr, 0);
                    }
                }
                // チェイン不要
                _LOG_DEBUGH(_T("LEAVE: one candidate"));
                return false;
            }
            // 直前の変換があればそれを取り消す
            if (prevXfered) {
                STATE_COMMON->SetOutString(prevYomi, prevOutLen);
            }
            // 候補があったので仮想鍵盤に表示
            setCandidatesVKB();
            // 前状態にチェインする
            _LOG_DEBUGH(_T("LEAVE: %d candidates"), pCands->size());
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
                    const auto& cand = cands[n];
                    const MString& fullYomi = candsByLen.GetFullYomi();
                    size_t candYlen = candsByLen.GetYomiLen(cand.resultStr);
                    _LOG_DEBUGH(_T("fullYomi='%s', candYlen=%d"), MAKE_WPTR(fullYomi), candYlen);
                    if (SETTINGS->mazeRemoveHeadSpace && fullYomi[0] == ' ') {
                        // 全読みの先頭の空白を削除
                        _LOG_DEBUGH(_T("REMOVE_HEAD_SPACE: multi cands && don't select first"));
                        MString leadStr;
                        if ((1 + candYlen) < fullYomi.size()) leadStr = fullYomi.substr(1, fullYomi.size() - (1 + candYlen));
                        outputStringAndPostProc(leadStr, cand, fullYomi.size(), nullptr, 0);
                    } else {
                        _LOG_DEBUGH(_T("CANDS_SIZE=%d, SELECT_FIRST=%s"), cands.size(), BOOL_TO_WPTR(SETTINGS->mazegakiSelectFirstCand));
                        outputStringAndPostProc(EMPTY_MSTR, cand, candYlen, nullptr, 0);
                    }
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
        void outputStringAndPostProc(const MString& leadStr, const MazeResult& mazeResult, size_t numBS, const MString* yomi, size_t outputLen) {
            _LOG_DEBUGH(_T("CALLED: leadStr=%s, resultStr=%s, resultXlen=%d, numBS=%d, yomi=%s, outputLen=%d"), MAKE_WPTR(leadStr), MAKE_WPTR(mazeResult.resultStr), mazeResult.xferLen, numBS, yomi ? MAKE_WPTR(*yomi) : _T("null"), outputLen);
            MString outStr = leadStr + mazeResult.resultStr;
            MazegakiNode* pn = dynamic_cast<MazegakiNode*>(pNode);
            if (pn) {
                // 今回の結果を元に戻すための情報を保存
                // yomi は、再変換をする際の元の読みになる
                size_t leadLen = leadStr.size();
                if (outputLen == 0) {
                    outputLen = outStr.size();
                    leadLen = 0;
                }
                _LOG_DEBUGH(_T("SET_YOMI_INFO: %s, outputLen=%d"), MAKE_WPTR(yomi ? *yomi : OUTPUT_STACK->GetLastOutputStackStr(numBS)), outputLen);
                pn->SetYomiInfo(yomi ? *yomi : OUTPUT_STACK->GetLastOutputStackStr(numBS), leadLen, outputLen);
            }
            // 変換形の出力
            _LOG_DEBUGH(_T("SET_OUT_STRING: %s, numBS=%d"), MAKE_WPTR(outStr), numBS);
            STATE_COMMON->SetOutString(outStr, numBS);
            // ブロッカー設定
            //_LOG_DEBUGH(_T("SET_MAZE_BLOCKER: pos=%d"), SETTINGS->mazeBlockerTail ? 0 : outStr.size() - (leadStr.size() + mazeResult.xferLen));
            //STATE_COMMON->SetMazegakiBlockerPosition(SETTINGS->mazeBlockerTail ? 0 : outStr.size() - (leadStr.size() + mazeResult.xferLen));
            _LOG_DEBUGH(_T("SET_MAZE_BLOCKER: pos=%d"), SETTINGS->mazeBlockerTail ? 0 : mazeResult.resultStr.size() - mazeResult.xferLen);
            STATE_COMMON->SetMazegakiBlockerPosition(SETTINGS->mazeBlockerTail ? 0 : mazeResult.resultStr.size() - mazeResult.xferLen);
            handleKeyPostProc();
            //選択した候補を履歴に登録
            // いったん無効にしておく
            //if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(utils::strip(outStr, _T("、。")));
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
                STATE_COMMON->SetVirtualKeyboardStrings(VkbLayout::Vertical, center, MazeResult::ToMStringVector(cands), winIdx * 10);
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

// 変換結果を元に戻すための変換前の読み
MString MazegakiNode::prevYomi;

// 変換結果を元に戻すためのリード文字列の長さ
// 「ひど|い目にあった」⇒「ひどい目に|遭った」のときの「い目に」の長さ)
size_t MazegakiNode::prevLeadLen = 0;

// 変換結果を元に戻すための変換形の長さ
size_t MazegakiNode::prevOutputLen = 0;

// 前回変換時のホットキーカウント
size_t MazegakiNode::deckeyCount = 0;

// 先頭候補の自動選択を一時的に中止する
bool MazegakiNode::selectFirstCandDisabled = false;

// シフトされた読み長
size_t MazegakiNode::shiftedYomiLen = 0;

// ブロッカーがシフトされた
bool MazegakiNode::blockerShifted = false;

// CommonNode
std::unique_ptr<MazegakiNode> MazegakiNode::CommonNode;

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
    if (MazegakiNode::CommonNode == 0) {
        // 交ぜ書き辞書ファイル名
        auto mazeFile = SETTINGS->mazegakiFile;
        LOG_INFO(_T("mazeFile=%s"), mazeFile.c_str());
        if (!mazeFile.empty()) {
            // 交ぜ書き辞書の読み込み
            LOG_INFO(_T("CALLED: mazegakiFile=%s"), mazeFile.c_str());
            if (!mazeFile.empty()) {
                MazegakiDic::CreateMazegakiDic(mazeFile);
            }
        }
        //else {
        //    ERROR_HANDLER->Warn(_T("「mazegaki=(ファイル名)」の設定がまちがっているようです"));
        //}
        // 共有ノードを生成する(このノードは、終了時に delete される)
        MazegakiNode::CommonNode.reset(new MazegakiNode());
    }
    // StrokeTable では unique_ptr で保持しているため、別のダミーノードを生成して返す
    return new MazegakiNode();
}

