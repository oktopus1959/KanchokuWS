#include "ModalState.h"
#include "State.h"
#include "Mazegaki/Mazegaki.h"
#include "Eisu.h"
#include "Zenkaku.h"
#include "Katakana.h"
#include "StrokeTable.h"

#if 0 || defined(_DEBUG)
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

DEFINE_CLASS_LOGGER(ModalState);

// モード状態(HistoryResidentState や KatakanaState, EisuState など)のための前処理
int ModalState::DoModalStatePreProc(int deckey) {
    _LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({}), NextState={}, NextNode={}"), Name, deckey, deckey, STATE_NAME(NextState()), NODE_NAME(NextNodeMaybe()));

    // まだ後続状態が無く、自身が StrokeState ではなく、deckey はストロークキーである場合は、ルートストローク状態を生成して後続させる
    // つまり、状態チェーンの末端であって、打鍵中でない場合
    if (!NextState()) {
        _LOG_DEBUGH(_T("NextNode FOUND"));
        // 交ぜ書き状態から抜けた直後にブロッカーや変換開始位置のシフトをやる場合のための処理
        if (MAZEGAKI_INFO && !MAZEGAKI_INFO->IsInMazegakiMode()) {
            _LOG_DEBUGH(_T("PATH-C"));
            // ブロッカーや読み開始位置を左右にシフト -- 読み位置がシフトされて再変換モードになったら、交ぜ書き状態を生成する
            if (MAZEGAKI_INFO->LeftRightShiftBlockerOrStartPos(deckey, [this]() {if (MAZEGAKI_INFO->IsReXferMode()) SetNextNodeMaybe(MAZEGAKI_NODE_PTR);})) {
                //シフトできた場合
                _LOG_DEBUGH(_T("LEAVE: true: LeftRightShiftBlockerOrStartPos: SUCCEEDED\nLEAVE: {}, NextNode={}"), Name, NODE_NAME(NextNodeMaybe()));
                return deckey;
            }
            _LOG_DEBUGH(_T("PATH-D"));
            //MAZEGAKI_INFO->ClearBlockerShiftFlag();
        }
        _LOG_DEBUGH(_T("PATH-E"));

        if (deckey >= 0 && (pNode == 0 || dynamic_cast<EisuNode*>(pNode) == 0)) {
            // 英数ノードでない場合
            if (pNode && dynamic_cast<ZenkakuNode*>(pNode) == 0 && deckey == TOGGLE_ZENKAKU_CONVERSION_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: ZenkakuState"));
                SetNextState(ZENKAKU_NODE->CreateState())->DoProcOnCreated();
                deckey = -1;    // この後は deckey の処理をやらない
            }
            else if (pNode && dynamic_cast<KatakanaNode*>(pNode) == 0 && deckey == TOGGLE_KATAKANA_CONVERSION_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: KatakanaState"));
                SetNextState(KATAKANA_NODE->CreateState())->DoProcOnCreated();
                deckey = -1;    // この後は deckey の処理をやらない
            }
            else if (pNode && dynamic_cast<EisuNode*>(pNode) == 0 && deckey == EISU_MODE_TOGGLE_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: EisuState"));
                SetNextState(EISU_NODE->CreateState())->DoProcOnCreated();
                deckey = -1;    // この後は deckey の処理をやらない
            }
            else if ((!pNode || !pNode->isStrokeTableNode()) && isStrokableKey(deckey)) {
                // ルートストロークノードの生成
                _LOG_DEBUGH(_T("CREATE: RootStrokeTableState"));
                if (ROOT_STROKE_NODE) {
                    SetNextState(ROOT_STROKE_NODE->CreateState());
                }
            }
        }
    }

    _LOG_DEBUGH(_T("LEAVE: {}: deckey={}, NextNode={}"), Name, deckey, NODE_NAME(NextNodeMaybe()));
    return deckey;
}
