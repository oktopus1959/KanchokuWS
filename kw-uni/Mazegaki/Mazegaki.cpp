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

#if 0 || defined(_DEBUG)
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
            LOG_INFO(_T("ENTER: yomiFull={}"), to_wstr(yomiFull));
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
                    _LOG_DEBUGH(_T("CALL: yomi={}"), to_wstr(yomi));
                    bool bWild = false;
                    if (is_wildcard(yomi[0])) {
                        // ワイルドカード文字が読みの先頭に来るのは、直前の文字がひながな、カタカナ、漢字以外の文字のときだけ
                        if (len < maxlen && utils::is_japanese_char_except_nakaguro(yomiFull[maxlen - (len + 1)])) {
                            _LOG_DEBUGH(_T("SKIP: yomi={}"), to_wstr(yomi));
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
            _LOG_DEBUGH(_T("LEAVE: mazeCandidates={}"), to_wstr(utils::join(MazeResult::ToMStringVector(mazeCandidates), '/', 20)));
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
        void SelectNth(size_t n, bool bJustOneOrManualSelected) {
            LOG_INFO(_T("CALLED: n={}, bJustOneOrManualSelected={}, mazeCandidates.size()={}"), n, bJustOneOrManualSelected, mazeCandidates.size());
            if (n < mazeCandidates.size()) {
                auto resultStr = mazeCandidates[n].resultStr;
                size_t len = GetYomiLen(mazeCandidates[n].resultStr);
                LOG_INFO(_T("resultStr={}, firstCandYomi={}, yomiLen={}"), to_wstr(resultStr), to_wstr(firstCandYomi), len);
                // 再検索して、変換履歴への登録と優先辞書に追加する
                LOG_INFO(_T("GetCandidates({})"), to_wstr(utils::last_substr(firstCandYomi, len)));
                MAZEGAKI_DIC->GetCandidates(utils::last_substr(firstCandYomi, len));
                MAZEGAKI_DIC->SelectCandidate(resultStr, bJustOneOrManualSelected);
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
            LOG_INFO(_T("CALLED: ctor"));
            Initialize(logger.ClassNameT(), pN);
            MAZEGAKI_INFO->Initialize(true);
        }

        ~MazegakiState() {
            MAZEGAKI_INFO->Initialize(false);
            MAZEGAKI_INFO->SetJustAfterPrevXfer();
        };

#define MY_NODE ((MazegakiNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        void DoProcOnCreated() override {
            _LOG_DEBUGH(_T("ENTER"));

            if (!MAZEGAKI_DIC) return;

            _LOG_DEBUGH(_T("A:ReXferMode: {}, mazegakiSelectFirstCand: {}"), MAZEGAKI_INFO->IsReXferMode(), SETTINGS->mazegakiSelectFirstCand);

            // ブロッカーがシフトされた直後であれば、変換処理は行わない
            if (MAZEGAKI_INFO->IsJustAfterBlockerShifted()) {
                _LOG_DEBUGH(_T("JUST AFTER BLOCKER SHIFTED"));
                if (!SETTINGS->mazegakiSelectFirstCand || MAZEGAKI_INFO->IsReXferMode()) {
                    // 先頭候補の直接出力モードでなければ、仮想鍵盤に候補を表示する
                    setCandidatesVKB();
                    MarkNecessary();
                    _LOG_DEBUGH(_T("LEAVE: CHAINED"));
                    return;
                }
                _LOG_DEBUGH(_T("LEAVE: RELEASED"));
                return;
            }

            _LOG_DEBUGH(_T("B:IsReXferMode: {}, mazegakiSelectFirstCand: {}"), MAZEGAKI_INFO->IsReXferMode(), SETTINGS->mazegakiSelectFirstCand);

            bool prevXfered = false;    // 直前に変換していたか
            MString prevYomi;           // 直前の変換の時の読み
            size_t prevLeadLen = 0;     // 直前のリード部の長さ
            size_t prevOutLen = 0;      // 直前の出力形の長さ

            //// 先頭候補出力モードで、交ぜ書き変換直後なら、元に戻して再変換 ⇒ これはやらない(ブロッカー解除には OutputStack の操作が必要だし、Escの後に再変換すればよいので)
            //if (SETTINGS->mazegakiSelectFirstCand && MAZEGAKI_INFO->IsJustAfterPrevXfer()) {
            //    MAZEGAKI_INFO->RevertPrevXfer();
            //}
            // 再変換モードなら、直前の変換をやり直す
            if (MAZEGAKI_INFO->IsReXferMode()) {
                prevLeadLen = MAZEGAKI_INFO->GetPrevLeadLen();
                prevOutLen = MAZEGAKI_INFO->GetPrevYomiInfo(prevYomi);
                prevXfered = !prevYomi.empty() && prevOutLen > 0;
                _LOG_DEBUGH(_T("RE XFER MODE: prevLeadLen={}, prevOutLen={}, prevXfered={}"), prevLeadLen, prevOutLen, prevXfered);
            }
            _LOG_DEBUGH(_T("C:IsReXferMode: {}, mazegakiSelectFirstCand: {}"), MAZEGAKI_INFO->IsReXferMode(), SETTINGS->mazegakiSelectFirstCand);

            size_t tailYomiLen = prevXfered ? MAZEGAKI_INFO->GetShiftedTailYomiLen() : 1000;
            _LOG_DEBUGH(_T("prevXfered={}, prevYomi={}, prevOutLen={}, tailYomiLen={}"), prevXfered, to_wstr(prevYomi), prevOutLen, tailYomiLen);
            _LOG_DEBUGH(_T("D:IsReXferMode: {}, mazegakiSelectFirstCand: {}"), MAZEGAKI_INFO->IsReXferMode(), SETTINGS->mazegakiSelectFirstCand);

            // 最大読み長までの長さの読みに対する交ぜ書き候補を全て取得する
            OUTPUT_STACK->unsetMazeBlocker();
            auto getPrevFullYomi = [prevYomi, prevOutLen, tailYomiLen]() {
                if (prevYomi.size() >= tailYomiLen) {
                    return utils::tail_substr(prevYomi, tailYomiLen);
                } else {
                    if (prevOutLen == 0) return OUTPUT_STACK->BackStringUptoMazeOrHistBlockerOrPunct(SETTINGS->mazeYomiMaxLen);
                    // prevLeadLen が1以上の場合は、直前の状態に戻れる
                    size_t prevLeadLen = tailYomiLen > prevOutLen ? tailYomiLen - prevOutLen : 0;
                    auto leadYomi = OUTPUT_STACK->BackStringUptoMazeOrHistBlockerOrPunct(prevLeadLen + prevOutLen);
                    _LOG_DEBUGH(_T("prevLeadLen={}, prevOutLen={}, leadYomi={}"), prevLeadLen, prevOutLen, to_wstr(leadYomi));
                    return leadYomi.substr(0, prevLeadLen) + prevYomi;
                }
            };
            MString fullYomi = prevXfered ? getPrevFullYomi() : OUTPUT_STACK->BackStringUptoMazeOrHistBlockerOrPunct(SETTINGS->mazeYomiMaxLen);
            if (fullYomi.size() < tailYomiLen) tailYomiLen = fullYomi.size();
            _LOG_DEBUGH(_T("fullYomi='{}', tailYomiLen={}"), to_wstr(fullYomi), tailYomiLen);

            // 変換候補の取得
            const std::vector<MazeResult>* pCands = nullptr;
            pCands = &candsByLen.GetAllCandidates(fullYomi);
            _LOG_DEBUGH(_T("pCands->size={}"), pCands->size());
            if (pCands->empty()) {
                // 候補が得られなかった
                // チェイン不要
                _LOG_DEBUGH(_T("LEAVE: no candidate"));
                return;
            }

            // 変換候補の読みの長さ
            size_t candYlen = candsByLen.GetFirstCandidateYomi().size();
            if (candYlen > tailYomiLen) candYlen = tailYomiLen;
            _LOG_DEBUGH(_T("candYlen={}, tailYomiLen={}, prevYomi.size={}"), candYlen, tailYomiLen, prevYomi.size());
            if (pCands->size() == 1 || (!MAZEGAKI_INFO->IsReXferMode() && !MAZEGAKI_INFO->IsJustAfterPrevXfer() && SETTINGS->mazegakiSelectFirstCand)) {
                // 読みの長さ候補が１つしかなかった、または先頭候補の自動出力モードなのでそれを選択して出力
                const auto& cand = pCands->front();
                size_t candLen = cand.resultStr.size();
                _LOG_DEBUGH(_T("candLen={}"), candLen);
                if (prevXfered) {
                    MString lead;
                    MString yomi;
                    size_t numBS = prevOutLen;
                    if (prevYomi.size() >= tailYomiLen) {
                        lead = prevYomi.substr(0, prevYomi.size() - candYlen);
                        yomi = utils::last_substr(prevYomi, candYlen);
                    } else {
                        yomi = utils::last_substr(fullYomi, candYlen);
                        lead = fullYomi.substr(0, fullYomi.size() - candYlen);
                        numBS += prevLeadLen;
                        size_t maxYomiLen = OUTPUT_STACK->TailSizeUptoMazeOrHistBlockerOrPunct();
                        _LOG_DEBUGH(_T("yomi={}, lead={}, prevYomi={}, tailYomiLen={}, numBS={}, prevOutLen={}"), to_wstr(yomi), to_wstr(lead), to_wstr(prevYomi), tailYomiLen, numBS, prevOutLen);
                        if (numBS > maxYomiLen) numBS = maxYomiLen;
                    }
                    // 「がかなる」⇒「画家なる」⇒">"⇒「がか奈留」のケース
                    // prevYomi=がかなる
                    // tailYomiLen=3 (かなる)
                    // candYlen = 2 (なる)
                    // leadStr=がか
                    // candStr=奈留
                    // numBS=prevOutLen=4 (画家なる)
                    // 今回の読み=なる
                    // candLen=2
                    _LOG_DEBUGH(_T("PREV_XFERED: numBS={}, prevOutLen={}"), numBS, prevOutLen);
                    outputStringAndPostProc(lead, cand, numBS, &yomi, candLen);
                } else {
                    if (SETTINGS->mazeRemoveHeadSpace && fullYomi[0] == ' ') {
                        // 全読みの先頭の空白を削除
                        _LOG_DEBUGH(_T("REMOVE_HEAD_SPACE: one cand or select first"));
                        MString leadStr;
                        if ((1 + candYlen) < fullYomi.size()) leadStr = fullYomi.substr(1, fullYomi.size() - (1 + candYlen));
                        outputStringAndPostProc(leadStr, cand, fullYomi.size(), nullptr, 0);
                    } else {
                        _LOG_DEBUGH(_T("CANDS_SIZE={}, SELECT_FIRST={}"), pCands->size(), SETTINGS->mazegakiSelectFirstCand);
                        outputStringAndPostProc(EMPTY_MSTR, cand, candYlen, nullptr, 0);
                    }
                }
                // 先頭候補を選択しておく
                _LOG_DEBUGH(_T("CALL: candsByLen.SelectNth(): pCands->size()={}"), pCands->size());
                candsByLen.SelectNth(0, pCands->size() == 1);
                // チェイン不要
                //MAZEGAKI_INFO->SetJustAfterPrevXfer();
                _LOG_DEBUGH(_T("LEAVE: one candidate"));
                return;
            }

            // 直前の変換があればそれを取り消す
            if (prevXfered) {
                resultStr.setResult(prevYomi, prevOutLen);
            }
            {
                // 今回の結果を元に戻すための情報を保存(ブロッカーや読み開始位置のシフトで必要になる)
                size_t outLen = fullYomi.size() < candYlen ? fullYomi.size() : candYlen;
                size_t leadLen = fullYomi.size() - outLen;
                _LOG_DEBUGH(_T("SET_YOMI_INFO: yomi={}, leadLen={}, outputLen={}"), to_wstr(fullYomi.substr(leadLen)), leadLen, outLen);
                MAZEGAKI_INFO->SetYomiInfo(fullYomi.substr(leadLen), leadLen, outLen);
            }
            // 候補があったので仮想鍵盤に表示
            setCandidatesVKB();
            // 前状態にチェインする
            MarkNecessary();
            _LOG_DEBUGH(_T("LEAVE: {} candidates"), pCands->size());
        }

        // 出力文字を取得する
        //void GetResultStringChain(MStringResult& resultOut) override {
        //    LOG_DEBUGH(_T("ENTER: {}: resultStr={}, numBS={}"), Name, to_wstr(resultStr.resultStr()), resultStr.numBS());
        //    //if (NextState()) NextState()->GetResultStringChain(resultOut);
        //    if (!resultStr.isDefault()) {
        //        resultOut.setResult(resultStr);
        //    }
        //    LOG_DEBUGH(_T("LEAVE: {}: resultStr={}, numBS={}"), Name, to_wstr(resultOut.resultStr()), resultOut.numBS());
        //}

         // Strokeキー を処理する
        void handleStrokeKeys(int deckey) {
            _LOG_DEBUGH(_T("CALLED: {}: deckey={:x}H({})"), Name, deckey, deckey);
            //if (deckey == SHIFT_SPACE_DECKEY) deckey = 0; // Shift+Spaceの場合を想定
            if (deckey <= STROKE_SPACE_DECKEY) {
                size_t n = (winIdx * LONG_VKEY_NUM) + (deckey % LONG_VKEY_NUM);
                const auto& cands = candsByLen.GetMazeCandidates();
                if (n < cands.size()) {
                    const auto& cand = cands[n];
                    const MString& fullYomi = candsByLen.GetFullYomi();
                    size_t candYlen = candsByLen.GetYomiLen(cand.resultStr);
                    _LOG_DEBUGH(_T("fullYomi='{}', candYlen={}"), to_wstr(fullYomi), candYlen);
                    if (SETTINGS->mazeRemoveHeadSpace && fullYomi[0] == ' ') {
                        // 全読みの先頭の空白を削除
                        _LOG_DEBUGH(_T("REMOVE_HEAD_SPACE: multi cands && don't select first"));
                        MString leadStr;
                        if ((1 + candYlen) < fullYomi.size()) leadStr = fullYomi.substr(1, fullYomi.size() - (1 + candYlen));
                        outputStringAndPostProc(leadStr, cand, fullYomi.size(), nullptr, 0);
                    } else {
                        _LOG_DEBUGH(_T("CANDS_SIZE={}, SELECT_FIRST={}"), cands.size(), SETTINGS->mazegakiSelectFirstCand);
                        outputStringAndPostProc(EMPTY_MSTR, cand, candYlen, nullptr, 0);
                    }
                    candsByLen.SelectNth(n, true);
                    return;
                }
            }
            setCandidatesVKB();
        }

         // Shiftキーで修飾されたキー
        void handleShiftKeys(int deckey) {
            _LOG_DEBUGH(_T("CALLED: {}: deckey={:x}H({}), char={}"), Name, deckey, deckey);
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

        // < ハンドラ
        void handleLeftTriangle() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            // 縦列鍵盤が表示されているときは、読み開始位置のシフトとなる
            handleLeftRightMazeShift(LEFT_SHIFT_MAZE_START_POS_DECKEY);
        }

        // > ハンドラ
        void handleRightTriangle() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            // 縦列鍵盤が表示されているときは、読み開始位置のシフトとなる
            handleLeftRightMazeShift(RIGHT_SHIFT_MAZE_START_POS_DECKEY);
        }

        // left/right maze shift keys
        void handleLeftRightMazeShift(int deckey) {
            _LOG_DEBUGH(_T("CALLED: {}, deckey={:x}H({})"), Name, deckey, deckey);
            // 交ぜ書き変換中は、ブロッカー移動を読み開始位置シフトとして扱う
            if (deckey == LEFT_SHIFT_BLOCKER_DECKEY) deckey = LEFT_SHIFT_MAZE_START_POS_DECKEY;
            if (deckey == RIGHT_SHIFT_BLOCKER_DECKEY) deckey = RIGHT_SHIFT_MAZE_START_POS_DECKEY;
            // 読み開始位置を左右にシフト
            if (!MAZEGAKI_INFO->LeftRightShiftBlockerOrStartPos(deckey, [this]() { DoProcOnCreated();})) {
                // シフトできなかった場合
                setCandidatesVKB();
            }
        }

        //// Ctrl-Space の処理 -- 第1候補を返す
        //void handleCtrlSpace() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleStrokeKeys(0);
        //}

        //// Shift-Space の処理 -- 第1候補を返す
        //void handleShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleStrokeKeys(0);
        //}

        //// Ctrl-Shift-Space の処理 -- 第1候補を返す
        //void handleCtrlShiftSpace() {
        //    _LOG_DEBUGH(_T("CALLED: {}"), Name);
        //    handleStrokeKeys(0);
        //}

        // NextCandTrigger の処理 -- 第1候補を返す
        void handleNextCandTrigger() {
            LOG_DEBUG(_T("CALLED: {}"), Name);
            handleStrokeKeys(0);
        }

        // PrevCandTrigger の処理 -- 第1候補を返す
        void handlePrevCandTrigger() {
            LOG_DEBUG(_T("CALLED: {}"), Name);
            handleStrokeKeys(0);
        }

        // RET/Enter の処理 -- 第1候補を返す
        void handleEnter() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleStrokeKeys(0);
        }

        // BS の処理 -- 処理のキャンセル
        void handleBS() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleKeyPostProc();
        }

        // Esc の処理 -- 処理のキャンセル
        void handleEsc() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleKeyPostProc();
        }

        // FullEscape の処理 -- 処理のキャンセル
        void handleFullEscape() {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
            handleKeyPostProc();
        }

        // CommitState の処理 -- 処理のコミット
        void handleCommitState() override {
            _LOG_DEBUGH(_T("CALLED: {}"), Name);
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
            _LOG_DEBUGH(_T("CALLED: leadStr={}, resultStr={}, resultXlen={}, numBS={}, yomi={}, outputLen={}"), to_wstr(leadStr), to_wstr(mazeResult.resultStr), mazeResult.xferLen, numBS, yomi ? to_wstr(*yomi) : _T("null"), outputLen);
            MString outStr = leadStr + mazeResult.resultStr;

            // 今回の結果を元に戻すための情報を保存
            // yomi は、再変換をする際の元の読みになる
            size_t leadLen = leadStr.size();
            if (outputLen == 0) {
                outputLen = outStr.size();
                leadLen = 0;
            }
            _LOG_DEBUGH(_T("SET_YOMI_INFO: {}, outputLen={}"), to_wstr(yomi ? *yomi : OUTPUT_STACK->GetLastOutputStackStr(numBS)), outputLen);
            MAZEGAKI_INFO->SetYomiInfo(yomi ? *yomi : OUTPUT_STACK->GetLastOutputStackStr(numBS), leadLen, outputLen);

            // 変換形の出力
            _LOG_DEBUGH(_T("SET_OUT_STRING: {}, numBS={}"), to_wstr(outStr), numBS);
            resultStr.setResult(outStr, numBS);
            // ブロッカー設定
            //_LOG_DEBUGH(_T("SET_MAZE_BLOCKER: pos={}"), SETTINGS->mazeBlockerTail ? 0 : outStr.size() - (leadStr.size() + mazeResult.xferLen));
            //STATE_COMMON->SetMazegakiBlockerPosition(SETTINGS->mazeBlockerTail ? 0 : outStr.size() - (leadStr.size() + mazeResult.xferLen));
            _LOG_DEBUGH(_T("SET_MAZE_BLOCKER: pos={}"), SETTINGS->mazeBlockerTail ? 0 : mazeResult.resultStr.size() - mazeResult.xferLen);
            STATE_COMMON->SetMazegakiBlockerPosition(SETTINGS->mazeBlockerTail ? 0 : mazeResult.resultStr.size() - mazeResult.xferLen);
            handleKeyPostProc();
        }

        void handleKeyPostProc() {
            STATE_COMMON->ClearVkbLayout();
            //STATE_COMMON->RemoveFunctionState();
            MarkUnnecessary();
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
                _LOG_DEBUGH(_T("{}: OutStringProcDone={}"), Name, STATE_COMMON->IsOutStringProcDone());
            } else {
                LOG_DEBUGH(_T("No MazeCands"));
            }
            MAZEGAKI_INFO->SetJustAfterPrevXfer();
        }

    };
    DEFINE_CLASS_LOGGER(MazegakiState);

} // namespace

// -------------------------------------------------------------------
// MazegakiNode - 交ぜ書き機能ノード
DEFINE_CLASS_LOGGER(MazegakiNode);

// コンストラクタ
MazegakiNode::MazegakiNode() {
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
MazegakiNode::~MazegakiNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* MazegakiNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new MazegakiState(this);
}

// -------------------------------------------------------------------
// MazegakiCommonInfo - 交ぜ書き共有情報
DEFINE_CLASS_LOGGER(MazegakiCommonInfo);

std::unique_ptr<MazegakiCommonInfo> MazegakiCommonInfo::CommonInfo;

// MazegakiCommonInfo - 交ぜ書き共有情報の作成(辞書もロード)
void MazegakiCommonInfo::CreateCommonInfo() {
    LOG_INFO(_T("ENTER"));
    if (CommonInfo == 0) {
        // 交ぜ書き辞書ファイル名
        auto mazeFile = SETTINGS->mazegakiFile;
        LOG_INFO(_T("mazeFile={}"), mazeFile);
        if (!mazeFile.empty()) {
            // 交ぜ書き辞書の読み込み
            LOG_INFO(_T("CALLED: mazegakiFile={}"), mazeFile);
            if (!mazeFile.empty()) {
                MazegakiDic::CreateMazegakiDic(mazeFile);
            }
        }
        //else {
        //    ERROR_HANDLER->Warn(_T("「mazegaki=(ファイル名)」の設定がまちがっているようです"));
        //}
        // 共有情報インスタンスを生成する(このノードは、終了時に delete される)
        CommonInfo.reset(new MazegakiCommonInfo());
        // 共有ノードを生成する(このノードは、終了時に delete される)
        CommonInfo->CommonNode.reset(new MazegakiNode());
    }
    LOG_INFO(_T("LEAVE"));
}

// 初期化
void MazegakiCommonInfo::Initialize(bool bMazegakiMode) {
    LOG_INFO(_T("ENTER: bMazegakiMode={}"), bMazegakiMode);
    inMazegakiMode = bMazegakiMode;
    //blockerShiftedDeckeyCount = 0;
    //shiftedTailYomiLen = 1000;
    //prevShiftedTailYomiLen = 1000;
    //prevDeckeyCount = 0;
    if (!bMazegakiMode) reXferMode = false;
    LOG_INFO(_T("LEAVE"));
}

// 交ぜ書き変換中か
bool MazegakiCommonInfo::IsInMazegakiMode() {
    _LOG_DEBUGH(_T("IsInMazegakiMode={}"), inMazegakiMode);
    return inMazegakiMode;
}

// 交ぜ書き変換終了の直後か
bool MazegakiCommonInfo::IsJustAfterPrevXfer() {
    size_t totalDecKeyCount = STATE_COMMON->GetTotalDecKeyCount();
    size_t firstStrokeCnt = STATE_COMMON->GetFirstStrokeKeyCount();
    bool result = totalDecKeyCount <= prevDeckeyCount + 1 || firstStrokeCnt == prevDeckeyCount + 1;
    _LOG_DEBUGH(_T("RESULT={}: totalDecKeyCount={}, firstStrokeCnt={}, prevDeckeyCount={}"), result, totalDecKeyCount, firstStrokeCnt, prevDeckeyCount);
    return result;
}

// 交ぜ書き変換実行直後状態にセット
void MazegakiCommonInfo::SetJustAfterPrevXfer() {
    _LOG_DEBUGH(_T("CALLED: SetJustAfterPrevXfer"));
    prevDeckeyCount = STATE_COMMON->GetTotalDecKeyCount();
}

// 再変換モードか
bool MazegakiCommonInfo::IsReXferMode() {
    _LOG_DEBUGH(_T("IsReXferMode={}"), reXferMode);
    return reXferMode;
}

// 再変換モードにセット
void MazegakiCommonInfo::SetReXferMode() {
    _LOG_DEBUGH(_T("CALLED: SetReXferMode"));
    reXferMode = true;
}

// ブロッカーフラグをクリアする
void MazegakiCommonInfo::ClearBlockerShiftFlag() {
    _LOG_DEBUGH(_T("CALLED: ClearBlockerShiftFlag"));
    blockerShiftedDeckeyCount = 0;
}

// ブロッカーを左シフトする
bool MazegakiCommonInfo::LeftShiftBlocker() {
    _LOG_DEBUGH(_T("LeftShiftBlocker: IsJustAfterBlockerShifted={}"), IsJustAfterBlockerShifted());
    if (IsJustAfterBlockerShifted() || IsJustAfterPrevXfer()) {
        OUTPUT_STACK->leftShiftMazeBlocker();
    } else {
        // 交ぜ書きで無い状態での最初の左シフトは、末尾から1つ左の位置にブロッカーを置く
        _LOG_DEBUGH(_T("FIRST CALL: setMazeBlocker(1)"));
        OUTPUT_STACK->setMazeBlocker(1);
    }
    blockerShiftedDeckeyCount = STATE_COMMON->GetTotalDecKeyCount();
    _LOG_DEBUGH(_T("blockerShiftedDeckeyCount={}"), blockerShiftedDeckeyCount);
    return IsJustAfterPrevXfer();
}

// ブロッカーを右シフトする
bool MazegakiCommonInfo::RightShiftBlocker() {
    _LOG_DEBUGH(_T("RightShiftBlocker: IsJustAfterBlockerShifted={}"), IsJustAfterBlockerShifted());
    if (IsJustAfterBlockerShifted() || IsJustAfterPrevXfer()) {
        OUTPUT_STACK->rightShiftMazeBlocker();
    } else {
        // 交ぜ書きで無い状態での最初の右シフトは、末尾にブロッカーを置く
        _LOG_DEBUGH(_T("FIRST CALL: setMazeBlocker()"));
        OUTPUT_STACK->setMazeBlocker();
    }
    blockerShiftedDeckeyCount = STATE_COMMON->GetTotalDecKeyCount();
    _LOG_DEBUGH(_T("blockerShiftedDeckeyCount={}"), blockerShiftedDeckeyCount);
    return IsJustAfterPrevXfer();
}

// ブロッカーがシフトされた直後か
bool MazegakiCommonInfo::IsJustAfterBlockerShifted() {
    bool result = STATE_COMMON->GetTotalDecKeyCount() <= blockerShiftedDeckeyCount + 1;
    _LOG_DEBUGH(_T("RESULT={}: blockerShiftedDeckeyCount={}, totalDeckeyCount={}"), result, blockerShiftedDeckeyCount, STATE_COMMON->GetTotalDecKeyCount());
    return result;
}

// ブロッカーがシフトされた直後の状態にする
void MazegakiCommonInfo::SetJustAfterBlockerShifted() {
    _LOG_DEBUGH(_T("CALLED: SetJustAfterBlockerShifted"));
    blockerShiftedDeckeyCount = STATE_COMMON->GetTotalDecKeyCount();
}

// 今回の結果を元に戻すための情報を保存 (yomi は、再変換をする際の元の読みになる)
void MazegakiCommonInfo::SetYomiInfo(const MString& yomi, size_t leadLen, size_t outputLen) {
    prevYomi = yomi;
    prevLeadLen = leadLen;
    prevOutputLen = outputLen;
    shiftedTailYomiLen = yomi.size();
    SetJustAfterPrevXfer();
    ClearBlockerShiftFlag();
    _LOG_DEBUGH(_T("prevYomi={}, prevLeadLen={}, prevOutputLen={}, shiftedTailYomiLen={}, prevShiftedTailYomiLen={}"), to_wstr(prevYomi), prevLeadLen, prevOutputLen, shiftedTailYomiLen, prevShiftedTailYomiLen);
}

// 前回の出力長を返す
size_t MazegakiCommonInfo::GetPrevOutputLen() {
    return IsJustAfterPrevXfer() ? prevOutputLen : 0;
}

// 前回のリード部長を返す
size_t MazegakiCommonInfo::GetPrevLeadLen() {
    return IsJustAfterPrevXfer() ? prevLeadLen : 0;
}

// シフトされた読み長の取得
size_t MazegakiCommonInfo::GetShiftedTailYomiLen() {
    return shiftedTailYomiLen;
}

// 前回の読みと出力長を返す
size_t MazegakiCommonInfo::GetPrevYomiInfo(MString& yomi) {
    if (IsJustAfterPrevXfer()) {
        yomi = prevYomi;
        return prevOutputLen;
    }
    return 0;
}

// 交ぜ書き変換結果を元に戻す
bool MazegakiCommonInfo::RevertPrevXfer(MStringResult& resultOut) {
    _LOG_DEBUGH(_T("prevYomi={}, prevOutputLen={}"), to_wstr(prevYomi), prevOutputLen);
    if (IsJustAfterPrevXfer() && prevOutputLen > 0) {
        MAZEGAKI_INFO->SetReXferMode();         // 再変換モードにセット
        MAZEGAKI_INFO->SetJustAfterPrevXfer();  // 続けて交ぜ書き関連の操作を受け付けるようにする
        resultOut.setResult(prevYomi, prevOutputLen);
        prevLeadLen = 0;
        prevOutputLen = 0;
        _LOG_DEBUGH(_T("MAZEGAKI REVERTED"));
        return true;
    }
    return false;
}

// 読み長を長くする(読み開始位置を左にシフトする) (前回の変換の直後でなければ false を返す)
bool MazegakiCommonInfo::LeftShiftYomiStartPos() {
    if (IsJustAfterPrevXfer()) {
        ++shiftedTailYomiLen;
        if (shiftedTailYomiLen < prevShiftedTailYomiLen) shiftedTailYomiLen = prevShiftedTailYomiLen;
        _LOG_DEBUGH(_T("shiftedTailYomiLen={}, prevShiftedTailYomiLen={}, result=True"), shiftedTailYomiLen, prevShiftedTailYomiLen);
        SetReXferMode();
        return true;
    }
    shiftedTailYomiLen = 1000;
    _LOG_DEBUGH(_T("shiftedTailYomiLen=1000, prevShiftedTailYomiLen={}, result=False"), prevShiftedTailYomiLen);
    return false;
}

// 読み開始位置を右にシフトする (前回の変換の直後でなければ false を返す)
bool MazegakiCommonInfo::RightShiftYomiStartPos() {
    if (IsJustAfterPrevXfer()) {
        if (shiftedTailYomiLen > 1) {
            prevShiftedTailYomiLen = prevLeadLen + shiftedTailYomiLen;
            --shiftedTailYomiLen;
        }
        _LOG_DEBUGH(_T("shiftedTailYomiLen={}, prevShiftedTailYomiLen={}, result=True"), shiftedTailYomiLen, prevShiftedTailYomiLen);
        SetReXferMode();
        return true;
    }
    shiftedTailYomiLen = 1000;
    _LOG_DEBUGH(_T("shiftedTailYomiLen=1000, prevShiftedTailYomiLen={}, result=False"), prevShiftedTailYomiLen);
    return false;
}

// ブロッカーや読み開始位置を左右にシフト -- 左右シフトを実行したら callback を呼んで true を返す。そうでなければ false を返す
bool MazegakiCommonInfo::LeftRightShiftBlockerOrStartPos(int deckey, std::function<void ()> callback) {
    _LOG_DEBUGH(_T("CALLED: deckey={:x}H({})"), deckey, deckey);
    switch (deckey) {
    case RIGHT_SHIFT_BLOCKER_DECKEY:
        _LOG_DEBUGH(_T("RIGHT_SHIFT_BLOCKER_DECKEY"));
        if (!SETTINGS->mazegakiSelectFirstCand || !IsJustAfterPrevXfer() || IsJustAfterBlockerShifted()) {
            if (MAZEGAKI_INFO->RightShiftBlocker()) {
                _LOG_DEBUGH(_T("right shift BLOCKER"));
                callback();
                _LOG_DEBUGH(_T("SHIFTED"));
                return true;
            }
            return false;
        }
        // Through Down: 第1候補出力モードで、交ぜ書き直後で、最初のシフト操作のときは、読み開始位置の右シフトとして扱う
    case RIGHT_SHIFT_MAZE_START_POS_DECKEY:
        _LOG_DEBUGH(_T("RIGHT_SHIFT_MAZE_START_POS_DECKEY"));
        if (MAZEGAKI_INFO->RightShiftYomiStartPos()) {
            _LOG_DEBUGH(_T("yomi START POS right shift"));
            MAZEGAKI_INFO->ClearBlockerShiftFlag();
            callback();
            _LOG_DEBUGH(_T("SHIFTED"));
            return true;
        }
        return false;
    case LEFT_SHIFT_BLOCKER_DECKEY:
        _LOG_DEBUGH(_T("LEFT_SHIFT_BLOCKER_DECKEY"));
        if (MAZEGAKI_INFO->LeftShiftBlocker()) {
            _LOG_DEBUGH(_T("left shift BLOCKER"));
            callback();
            _LOG_DEBUGH(_T("SHIFTED"));
            return true;
        }
        return false;
    case LEFT_SHIFT_MAZE_START_POS_DECKEY:
        _LOG_DEBUGH(_T("LEFT_SHIFT_MAZE_START_POS_DECKEY"));
        if (MAZEGAKI_INFO->LeftShiftYomiStartPos()) {
            _LOG_DEBUGH(_T("yomi START POS left shift"));
            MAZEGAKI_INFO->ClearBlockerShiftFlag();
            callback();
            _LOG_DEBUGH(_T("SHIFTED"));
            return true;
        }
        return false;
    default:
        return false;
    }
}

// -------------------------------------------------------------------
// MazegakiNodeBuilder - 交ぜ書き機能ノードビルダー

DEFINE_CLASS_LOGGER(MazegakiNodeBuilder);

Node* MazegakiNodeBuilder::CreateNode() {
    // MazegakiCommonInfo - 交ぜ書き共有情報の作成(辞書もロード)
    MazegakiCommonInfo::CreateCommonInfo();
    // StrokeTable では unique_ptr で保持しているため、別のダミーノードを生成して返す
    return new MazegakiNode();
}

