#include "Logger.h"

#include "HistoryStateBase.h"

#include "Settings.h"
#include "StrokeMerger/Merger.h"

#if 1
#undef _LOG_DEBUGH
#define _LOG_DEBUGH LOG_INFOH
#endif

#define CAND_LEN_THRESHOLD 10

namespace {

    // -------------------------------------------------------------------
    // 履歴入力機能状態基底クラス
    class HistoryStateBaseImpl : public HistoryStateBase {
        DECLARE_CLASS_LOGGER;

        const Node* pNode_ = 0;

        String BaseName;

    protected:
        // 履歴入力候補のリスト
        //HistCandidates histCands;

        int candLen = 0;
        size_t candDispVerticalPos = 0;
        size_t candDispHorizontalPos = 0;

        bool bWaitingForNum = false;
        bool bDeleteMode = false;

    public:
        // コンストラクタ
        HistoryStateBaseImpl(const Node* pN)
            : pNode_(pN), BaseName(logger.ClassNameT()) {
            LOG_DEBUGH(_T("CALLED"));
        }

        ~HistoryStateBaseImpl() override { };

    public:
        // 履歴検索文字列の遡及ブロッカーをセット
        void setBlocker() override {
            _LOG_DEBUGH(_T("CALLED: {}"), BaseName);
            STATE_COMMON->SetAppendBackspaceStopperFlag();
            STATE_COMMON->SetHistoryBlockFlag();
            STATE_COMMON->ClearDecKeyCount();
        }

        // 選択された履歴候補を出力(これが呼ばれた時点で、すでにキーの先頭まで巻き戻すように plannedNumBS が設定されていること)
        void setOutString(const HistResult& result, MStringResult& resultStr) override {
            _LOG_DEBUGH(_T("ENTER: result.OrigKey={}, result.Key={}, result.Word={}, keyLen={}, wildKey={}, prevOutStr={}, prevKey={}, plannedNumBS={}"), \
                to_wstr(result.OrigKey), to_wstr(result.Key), to_wstr(result.Word), result.KeyLen(), result.WildKey, \
                to_wstr(STROKE_MERGER_NODE->GetPrevOutString()), to_wstr(STROKE_MERGER_NODE->GetPrevKey()), resultStr.numBS());

            MString outStr = result.Word;
            MString outKey = result.Key;
            if (outStr.empty()) {
                // 未選択状態だったら、出力文字列を元に戻す
                outKey = STROKE_MERGER_NODE->GetPrevKey();
                outStr = STROKE_MERGER_NODE->GetPrevOutString();
                if (outStr.empty()) outStr = outKey;
            } else {
                size_t pos = outStr.find(VERT_BAR);     // '|' を含むか
                _LOG_DEBUGH(_T("pos={}, histMapKeyMaxLength={}"), pos, SETTINGS->histMapKeyMaxLength);
                if (pos <= SETTINGS->histMapKeyMaxLength) {
                    // histMap候補
                    if (pos + 1 < outStr.size() && outStr[pos + 1] == VERT_BAR) ++pos;  // '||' だったら1つ進める(HistoryDicで既に対処済みなので、多分、ここでは不要のはず)
                    if (pos + 1 < outStr.size() && outStr[pos + 1] == HASH_MARK) ++pos;  // '|#' だったら1つ進める(# はローマ字変換の印)
                    outStr = utils::safe_substr(outStr, pos + 1);
                    _LOG_DEBUGH(_T("histMap: outStr={}, outKey={}"), to_wstr(outStr), to_wstr(outKey));
                    if (outKey.size() > pos) {
                        // 変換キー('|'より前の部分)よりも入力された文字列キーが長い場合(例: "にら|韮" に対して「にらちされ」が入力されたような場合)
                        outStr.append(utils::safe_substr(outKey, pos));
                        _LOG_DEBUGH(_T("histMap: outKey Appended: outStr={}"), to_wstr(outStr));
                    }
                }
                if (outKey.size() < result.OrigKey.size()) {
                    // 変換キーが元キーよりも短い場合(「あわなだ」が元キーで「わなだ」が変換キーのケース)
                    auto leadStr = result.OrigKey.substr(0, result.OrigKey.size() - outKey.size());
                    outStr = leadStr + outStr;
                    outKey = leadStr + outKey;
                    _LOG_DEBUGH(_T("histMap: leadStr Appended: leadStr={}"), to_wstr(leadStr));
                }
            }
            _LOG_DEBUGH(_T("outStr={}, outKey={}"), to_wstr(outStr), to_wstr(outKey));

            resultStr.setResult(outStr);
            STROKE_MERGER_NODE->SetPrevHistState(outStr, outKey);

            //_LOG_DEBUGH(_T("prevOutString={}, isPrevHistKeyUsed={}"), to_wstr(STROKE_MERGER_NODE->GetPrevOutString()), STROKE_MERGER_NODE->IsPrevHistKeyUsed());
            _LOG_DEBUGH(_T("LEAVE: prevOutString={}"), to_wstr(STROKE_MERGER_NODE->GetPrevOutString()));
        }

        // 前回の履歴検索の出力と現在の出力文字列(改行以降)の末尾を比較し、同じであれば前回の履歴検索のキーを取得する
        // この時、出力スタックは、キーだけを残し、追加出力部分は巻き戻し予約される(numBackSpacesに値をセット)
        // 前回が空キーだった場合は、返値も空キーになるので、STROKE_MERGER_NODE->PrevKeyLen == 0 かどうかで前回と同じキーであるか否かを判断すること
        // ここに来る場合には、以下の3つの状態がありえる:
        // ①まだ履歴検索がなされていない状態
        // ②検索が実行されたが、出力文字列にはキーだけが表示されている状態
        // ③横列のどれかの候補が選択されて出力文字列に反映されている状態
        MString getLastHistKeyAndRewindOutput(MStringResult& resultStr) override {
            // 前回の履歴検索の出力
            //bool bPrevHistUsed = STROKE_MERGER_NODE->IsPrevHistKeyUsed();
            const auto& prevKey = STROKE_MERGER_NODE->GetPrevKey();
            const auto& prevOut = STROKE_MERGER_NODE->GetPrevOutString();
            //_LOG_DEBUGH(_T("isPrevHistUsed={}, prevOut={}, prevKey={}"), bPrevHistUsed, to_wstr(prevOut), to_wstr(prevKey));
            _LOG_DEBUGH(_T("prevOut={}, prevKey={}"), to_wstr(prevOut), to_wstr(prevKey));

            if (prevKey.empty()) {
                // ①まだ履歴検索がなされていない状態
                // empty key を返す
                _LOG_DEBUGH(_T("NOT YET HIST USED"));
            } else if (prevOut.empty()) {
                // ②検索が実行されたが、出力文字列にはキーだけが表示されている状態
                _LOG_DEBUGH(_T("CURRENT: SetOutString(str={}, numBS={})"), to_wstr(prevKey), prevKey.size());
                resultStr.setResult(prevKey, (int)(prevKey.size()));
                STROKE_MERGER_NODE->SetPrevHistState(prevKey, prevKey);
                _LOG_DEBUGH(_T("CURRENT: prevKey={}"), to_wstr(prevKey));
            } else {
                // ③横列のどれかの候補が選択されて出力文字列に反映されている状態
                _LOG_DEBUGH(_T("REVERT and NEW HIST: SetOutString(str={}, numBS={})"), to_wstr(prevKey), prevOut.size());
                resultStr.setResult(prevKey, (int)(prevOut.size()));
                STROKE_MERGER_NODE->SetPrevHistState(prevKey, prevKey);
                _LOG_DEBUGH(_T("REVERT and NEW HIST: prevKey={}"), to_wstr(prevKey));
            }

            _LOG_DEBUGH(_T("last Japanese key={}"), to_wstr(prevKey));
            return prevKey;
        }

        // 前回の履歴選択の出力と現在の出力文字列(改行以降)の末尾が同一であるか
        bool isLastHistOutSameAsCurrentOut() override {
            // 前回の履歴選択の出力
            MString prevOut = STROKE_MERGER_NODE->GetPrevOutString();
            // 出力スタックから、上記と同じ長さの末尾文字列を取得
            auto lastJstr = OUTPUT_STACK->GetLastJapaneseStr<MString>(prevOut.size());
            bool result = !prevOut.empty() && lastJstr == prevOut;
            _LOG_DEBUGH(_T("RESULT: {}: prevOut={}, lastJapaneseStr={}"), result, to_wstr(prevOut), to_wstr(lastJstr));
            return result;
        }

        // 履歴入力候補を鍵盤にセットする
        void setCandidatesVKB(VkbLayout layout, int canLen, const MString& key, bool bShrinkWord = false) override {
            candLen = canLen;
            setCandidatesVKB(layout, HIST_CAND->GetCandWords(key, false, canLen), key, bShrinkWord);
        }

        // 履歴入力候補を鍵盤にセットする
        void setCandidatesVKB(VkbLayout layout, const std::vector<MString>& cands, const MString& key, bool bShrinkWord = false) override {
            _LOG_DEBUGH(_T("ENTER: layout={}, cands.size()={}, key={}"), StateCommonInfo::GetVkbLayoutStr(layout), cands.size(), to_wstr(key));
            auto mark = pNode_->getString();
            size_t maxlen = 0;
            for (const auto& w : cands) { if (maxlen < w.size()) maxlen = w.size(); }

            _LOG_DEBUGH(_T("maxlen={}, candDispVerticalPos={}, candDispHorizontalPos={}"), maxlen, candDispVerticalPos, candDispHorizontalPos);

            if (maxlen <= CAND_DISP_LONG_VKEY_LEN) {
                candDispVerticalPos = 0;
            } else if (candDispVerticalPos >= maxlen) {
                candDispVerticalPos -= CAND_DISP_LONG_VKEY_LEN;
            }
            size_t p = candDispHorizontalPos;
            if (p >= cands.size()) {
                p = p >= LONG_KEY_NUM ? p - LONG_KEY_NUM : 0;
                candDispHorizontalPos = p;
            }
            size_t q = p + (layout == VkbLayout::Horizontal ? SETTINGS->histHorizontalCandMax : LONG_KEY_NUM);
            if (q > cands.size()) q = cands.size();

            _LOG_DEBUGH(_T("p={}, q={}, candDispVerticalPos={}, candDispHorizontalPos={}"), p, q, candDispVerticalPos, candDispHorizontalPos);

            std::vector<MString> words;
            for (size_t i = p; i < q; ++i) {
                words.push_back(
                    bShrinkWord ? utils::str_shrink(cands[i], CAND_DISP_LONG_VKEY_LEN)
                    : utils::safe_substr(cands[i], candDispVerticalPos, CAND_DISP_LONG_VKEY_LEN));
            }
            STATE_COMMON->SetVirtualKeyboardStrings(layout, mark + utils::str_shrink(key, 5), words);

            if (HIST_CAND->GetSelectPos() >= 0) STATE_COMMON->SetDontMoveVirtualKeyboard();

            _LOG_DEBUGH(_T("LEAVE"));
        }

        // 中央鍵盤の色付け、矢印キー有効、縦列鍵盤の色付けあり
        void setHistSelectColorAndBackColor() override {
            // 「候補選択」の色で中央鍵盤の色付け
            STATE_COMMON->SetHistCandSelecting();
            // 矢印キーを有効にして、背景色の色付けあり
            _LOG_DEBUGH(_T("Set Unselected"));
            STATE_COMMON->SetWaitingCandSelect(-1);
        }

        // 中央鍵盤の文字出力と色付け、矢印キー有効、縦列鍵盤の色付けなし
        void setCenterStringAndBackColor(StringRef ws) override {
            // 中央鍵盤の文字出力
            STATE_COMMON->SetCenterString(ws);
            // 「その他の状態」の色で中央鍵盤の色付け
            STATE_COMMON->SetOtherStatus();
            // 矢印キーを有効にして、背景色の色付けなし
            _LOG_DEBUGH(_T("Set Unselected=-2"));
            STATE_COMMON->SetWaitingCandSelect(-2);
        }

        void setCandDispHorizontalPos(size_t pos) override {
            candDispHorizontalPos = pos;
        }

        // モード標識文字を返す
        mchar_t GetModeMarker() override {
            return utils::safe_front(pNode_->getString());
        }

        // 最終的な出力履歴が整ったところで呼び出される処理
        void DoLastHistoryProc() override {
            _LOG_DEBUGH(_T("ENTER"));

            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(), HIST_CAND->GetCurrentKey());
            if (bDeleteMode) {
                // 中央鍵盤の文字出力と色付け、矢印キー有効、縦列鍵盤の色付けなし
                setCenterStringAndBackColor(_T("削除"));
            } else if (bWaitingForNum) {
                // 中央鍵盤の文字出力と色付け、矢印キー有効、縦列鍵盤の色付けなし
                setCenterStringAndBackColor(_T("文字数指定"));
            } else {
                // 矢印キーを有効にして、先頭候補の背景色を色付け
                setHistSelectColorAndBackColor();
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // Strokeキー を処理する
        bool handleStrokeKeys(int deckey, MStringResult& resultStr) override {
            _LOG_DEBUGH(_T("ENTER: deckey={:x}H({})"), deckey, deckey);
            if (deckey == SETTINGS->histDelDeckeyId) {
                // 削除モードに入る
                _LOG_DEBUGH(_T("LEAVE: DELETE MODE"));
                bDeleteMode = true;
                return false;
            }
            if (bDeleteMode) {
                // 削除モードのとき
                if (deckey == SETTINGS->histDelDeckeyId) {
                    bDeleteMode = false;
                    _LOG_DEBUGH(_T("LEAVE DELETE MODE"));
                } else if (deckey < STROKE_SPACE_DECKEY) {
                    HIST_CAND->DeleteNth((deckey % LONG_KEY_NUM) + candDispHorizontalPos);
                    bDeleteMode = false;
                    //const String key = STATE_COMMON->GetLastKanjiOrKatakanaKey();
                    // ひらがな交じりやASCIIもキーとして取得する
                    const auto key = OUTPUT_STACK->GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey<MString>(SETTINGS->histMapKeyMaxLength);
                    _LOG_DEBUGH(_T("key={}"), to_wstr(key));
                    candLen = 0;
                    HIST_CAND->GetCandidates(key, false, candLen);
                    _LOG_DEBUGH(_T("LEAVE DELETE MODE"));
                }
                return false;
            }
            if (deckey == SETTINGS->histNumDeckeyId) {
                // 履歴文字数指定
                _LOG_DEBUGH(_T("ENTER: NUM MODE"));
                bWaitingForNum = true;
                return false;
            }
            if (bWaitingForNum) {
                // 履歴文字数指定のとき
                bWaitingForNum = false;
                if (deckey >= 0 && deckey < CAND_LEN_THRESHOLD) {
                    // '1'〜'0' (1〜10文字のものだけを表示)
                    _LOG_DEBUGH(_T("ENTER JUST LEN MODE"));
                    //指定の長さのものだけを残して仮想鍵盤に表示
                    candDispHorizontalPos = 0;
                    candDispVerticalPos = 0;
                    auto key = HIST_CAND->GetCurrentKey();
                    candLen = (deckey + 1) % LONG_KEY_NUM;
                    setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);
                }
                _LOG_DEBUGH(_T("LEAVE: forNum"));
                return false;
            }

            // 下記は不要。いったん出力履歴バッファをクリアしてから履歴入力を行えばよいため
            //if (deckey == DECKEY_STROKE_44) {
            //    // '@' : 全使用リストから取得する
            //    //histBase->setCandidatesVKB(HIST_CAND->GetCandidates(_T("")), _T(""));
            //    HIST_CAND->GetCandidates(_T(""));
            //    return false;
            //}

            // 候補の選択
            _LOG_DEBUGH(_T("HIST_CAND->SelectNth()"));
            HistResult result = HIST_CAND->SelectNth((deckey >= STROKE_SPACE_DECKEY ? 0 : deckey % LONG_KEY_NUM) + candDispHorizontalPos);
            _LOG_DEBUGH(_T("result.Word={}, result.KeyLen={}"), to_wstr(result.Word), result.KeyLen());
            if (!result.Word.empty()) {
                getLastHistKeyAndRewindOutput(resultStr);    // 前回の履歴検索キー取得と出力スタックの巻き戻し予約(numBackSpacesに値をセット)
                setOutString(result, resultStr);               // 選択された候補の出力
                HIST_CAND->ClearKeyInfo();
                //if (result.KeyLen() >= 2) STATE_COMMON->SetHistoryBlockFlag();  // 1文字の場合は履歴検索の対象となる
                // 出力された履歴に対しては、履歴の再検索の対象としない(変換形履歴の場合を除く)
                if (result.Word.find(VERT_BAR) == MString::npos) {
                    _LOG_DEBUGH(_T("SetHistoryBlocker"));
                    STATE_COMMON->SetHistoryBlockFlag();
                }
            }
            //handleKeyPostProc();
            _LOG_DEBUGH(_T("LEAVE: True"));
            return true;
        }

        // 機能キーだったときの一括処理(false を返すと、この後、個々の機能キーのハンドラが呼ばれる)
        bool handleFunctionKeys(int deckey) override {
            _LOG_DEBUGH(_T("CALLED"));
            switch (deckey) {
            case LEFT_ARROW_DECKEY:
            case RIGHT_ARROW_DECKEY:
            case UP_ARROW_DECKEY:
            case DOWN_ARROW_DECKEY:
                return false;
            default:
                if (bDeleteMode || bWaitingForNum) {
                    // 矢印キーでなくて、削除モードまたは数字入力モードなら、それを抜ける
                    bDeleteMode = false;
                    bWaitingForNum = false;
                    return true;
                } else {
                    return false;
                }
            }
        }

        void handleDownArrow() override {
            //candDispVerticalPos += CAND_DISP_LONG_VKEY_LEN;
            candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
            auto key = HIST_CAND->GetCurrentKey();
            //指定の長さのものだけを残して仮想鍵盤に表示
            candLen = candLen < 0 ? abs(candLen) : (candLen + 1) % CAND_LEN_THRESHOLD;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);
            return;
        }

        void handleUpArrow() override {
            //if (candDispVerticalPos >= CAND_DISP_LONG_VKEY_LEN)
            //    candDispVerticalPos -= CAND_DISP_LONG_VKEY_LEN;
            //else
            //    candDispVerticalPos = 0;
            candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
            auto key = HIST_CAND->GetCurrentKey();
            //指定の長さのものだけを残して仮想鍵盤に表示
            candLen = candLen < 0 ? abs(candLen) - 1 : (candLen == 0 ? CAND_LEN_THRESHOLD : candLen) - 1;
            setCandidatesVKB(VkbLayout::Vertical, HIST_CAND->GetCandWords(key, false, candLen), key);
            return;
        }

        void handleLeftArrow() override {
            if (candDispHorizontalPos >= LONG_KEY_NUM)
                candDispHorizontalPos -= LONG_KEY_NUM;
            else
                candDispHorizontalPos = 0;
            candDispVerticalPos = 0;
        }

        void handleRightArrow() override {
            candDispHorizontalPos += LONG_KEY_NUM;
            candDispVerticalPos = 0;
        }

    };
    DEFINE_CLASS_LOGGER(HistoryStateBaseImpl);
} // namespace

HistoryStateBase* HistoryStateBase::createInstance(const Node* pN) {
    return new HistoryStateBaseImpl(pN);
}
