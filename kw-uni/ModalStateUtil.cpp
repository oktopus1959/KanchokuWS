#include "ModalStateUtil.h"
#include "State.h"
#include "Mazegaki/Mazegaki.h"
#include "Eisu.h"
#include "Zenkaku.h"
#include "Katakana.h"
#include "StrokeTable.h"
#include "Settings.h"
#include "StrokeMerger/Merger.h"
//#include "History/HistoryResidentState.h"
#include "StrokeMerger/StrokeMergerHistoryResidentState.h"

#if 1 || defined(_DEBUG)
#undef _DEBUG_SENT
#undef _DEBUG_FLAG
#undef LOG_INFO
#undef LOG_DEBUGH
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#undef _LOG_DEBUGH_COND
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

DEFINE_CLASS_LOGGER(ModalStateUtil);

// モード状態(HistoryResidentState や KatakanaState, EisuState など)のための前処理
// 後続処理が不要な場合は -1 を返す
int ModalStateUtil::ModalStatePreProc(State* pState, int deckey, bool isStrokable) {
    _LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({}), strokable={}, NextState={}, NextNode={}"),
        pState->GetName(), deckey, deckey, isStrokable, STATE_NAME(pState->NextState()), NODE_NAME(pState->NextNodeMaybe()));

    // まだ後続状態が無く、自身が StrokeState ではなく、deckey はストロークキーである場合は、ルートストローク状態を生成して後続させる
    // つまり、状態チェーンの末端であって、打鍵中でない場合
    if (!pState->NextState()) {
        _LOG_DEBUGH(_T("NextNode FOUND"));
        // 交ぜ書き状態から抜けた直後にブロッカーや変換開始位置のシフトをやる場合のための処理
        if (MAZEGAKI_INFO && !MAZEGAKI_INFO->IsInMazegakiMode()) {
            _LOG_DEBUGH(_T("PATH-C"));
            // ブロッカーや読み開始位置を左右にシフト -- 読み位置がシフトされて再変換モードになったら、交ぜ書き状態を生成する
            if (MAZEGAKI_INFO->LeftRightShiftBlockerOrStartPos(deckey, [pState]() {if (MAZEGAKI_INFO->IsReXferMode()) pState->SetNextNodeMaybe(MAZEGAKI_NODE_PTR);})) {
                //シフトできた場合
                _LOG_DEBUGH(_T("LEAVE: true: LeftRightShiftBlockerOrStartPos: SUCCEEDED\nLEAVE: {}, NextNode={}"), pState->GetName(), NODE_NAME(pState->NextNodeMaybe()));
                return deckey;
            }
            _LOG_DEBUGH(_T("PATH-D"));
            //MAZEGAKI_INFO->ClearBlockerShiftFlag();
        }
        _LOG_DEBUGH(_T("PATH-E"));

        Node* pNode = pState->MyNode();
        if (deckey >= 0 && (pNode == 0 || dynamic_cast<EisuNode*>(pNode) == 0)) {
            // 英数ノードでない場合
            if (pNode && dynamic_cast<ZenkakuNode*>(pNode) == 0 && deckey == TOGGLE_ZENKAKU_CONVERSION_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: ZenkakuState"));
                //SetNextState(ZENKAKU_NODE->CreateState())->DoProcOnCreated();
                pState->SetNextNodeMaybe(ZENKAKU_NODE);
                deckey = -1;    // この後は deckey の処理をやらない
            }
            else if (pNode && dynamic_cast<KatakanaNode*>(pNode) == 0 && deckey == TOGGLE_KATAKANA_CONVERSION_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: KatakanaState"));
                //SetNextState(KATAKANA_NODE->CreateState())->DoProcOnCreated();
                pState->SetNextNodeMaybe(KATAKANA_NODE.get());
                deckey = -1;    // この後は deckey の処理をやらない
            }
            else if (pNode && dynamic_cast<EisuNode*>(pNode) == 0 && deckey == EISU_MODE_TOGGLE_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: EisuState"));
                //SetNextState(EISU_NODE->CreateState())->DoProcOnCreated();
                pState->SetNextNodeMaybe(EISU_NODE);
                deckey = -1;    // この後は deckey の処理をやらない
            }
            else if ((!pNode || !pNode->isStrokeTableNode()) && isStrokable) {
                if (SETTINGS->multiStreamMode && STROKE_MERGER_NODE) {
                    // 配列融合状態の生成
                    LOG_INFO(_T("CREATE: StrokeMergerState"));
                    //SetNextState(STROKE_MERGER_NODE->CreateState());
                    pState->SetNextNodeMaybe(STROKE_MERGER_NODE.get());
                }
                else if (ROOT_STROKE_NODE) {
                    // ルートストローク状態の生成
                    LOG_INFO(_T("CREATE: RootStrokeTableState"));
                    //SetNextState(ROOT_STROKE_NODE->CreateState());
                    pState->SetNextNodeMaybe(ROOT_STROKE_NODE);
                }
            }
            // 必要なら新状態を生成する
            pState->CreateNewState();
        }
    }

    _LOG_DEBUGH(_T("LEAVE: {}: deckey={}, NextNode={}"), pState->GetName(), deckey, NODE_NAME(pState->NextNodeMaybe()));
    return deckey;
}

// その他の特殊キー (常駐の履歴機能があればそれを呼び出す)
void ModalStateUtil::handleSpecialKeys(State* pState, int deckey) {
    _LOG_DEBUGH(_T("CALLED: {}, deckey={}"), pState->GetName(), deckey);
    if (MERGER_HISTORY_RESIDENT_STATE) {
        // 常駐の履歴機能があればそれを呼び出す//
        MERGER_HISTORY_RESIDENT_STATE->dispatchDeckey(deckey);
    } else {
        pState->State::handleSpecialKeys(deckey);
    }
}
