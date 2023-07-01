#include "ModalState.h"
#include "State.h"
#include "Mazegaki/Mazegaki.h"
#include "Eisu.h"
#include "Zenkaku.h"
#include "Katakana.h"
#include "StrokeTable.h"

#if 1 || defined(_DEBUG)
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

DEFINE_CLASS_LOGGER(ModalState);

// モード状態の処理
// 状態チェーンをたどる。後続状態があればそちらに移譲。なければここでホットキーをディスパッチ。
bool ModalState::HandleModalState(State* pState, int deckey) {
    auto pNext = pState->NextState();
    _LOG_DEBUGH(_T("ENTER: {}: deckey={:x}H({}), NextState={}, NextNode={}"), pState->Name, deckey, deckey, STATE_NAME(pNext), NODE_NAME(pState->NextNodeMaybe()));
    // モード状態(HistoryResidentState や TranslationState など)のための前処理
    // まだ後続状態が無く、自身が StrokeState ではなく、deckey はストロークキーである場合は、ルートストローク状態を生成して後続させる
    // つまり、状態チェーンの末端であって、打鍵中でない場合
    if (!pNext) {
        _LOG_DEBUGH(_T("NextNode FOUND"));
        // 交ぜ書き状態から抜けた直後にブロッカーや変換開始位置のシフトをやる場合のための処理
        if (MAZEGAKI_INFO && !MAZEGAKI_INFO->IsInMazegakiMode()) {
            _LOG_DEBUGH(_T("PATH-C"));
            // ブロッカーや読み開始位置を左右にシフト -- 読み位置がシフトされて再変換モードになったら、交ぜ書き状態を生成する
            if (MAZEGAKI_INFO->LeftRightShiftBlockerOrStartPos(deckey, [pState]() {if (MAZEGAKI_INFO->IsReXferMode()) pState->SetNextNodeMaybe(MAZEGAKI_NODE_PTR);})) {
                //シフトできた場合
                _LOG_DEBUGH(_T("LEAVE: true: LeftRightShiftBlockerOrStartPos: SUCCEEDED\nLEAVE: {}, NextNode={}"), pState->Name, NODE_NAME(pState->NextNodeMaybe()));
                return true;
            }
            _LOG_DEBUGH(_T("PATH-D"));
            //MAZEGAKI_INFO->ClearBlockerShiftFlag();
        }
        _LOG_DEBUGH(_T("PATH-E"));

        auto pNode = pState->MyNode();
        if (pNode == 0 || dynamic_cast<EisuNode*>(pNode) == 0) {
            // 英数ノードでない場合
            if (pNode && dynamic_cast<ZenkakuNode*>(pNode) == 0 && deckey == TOGGLE_ZENKAKU_CONVERSION_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: ZenkakuState"));
                pNext = pState->SetNextState(ZENKAKU_NODE->CreateState());
                pNext->SetPrevState(pState);
                pNext->DoProcOnCreated();
                deckey = -1;    // この後は dekcey の処理をやらない
            }
            else if (pNode && dynamic_cast<KatakanaNode*>(pNode) == 0 && deckey == TOGGLE_KATAKANA_CONVERSION_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: KatakanaState"));
                pNext = pState->SetNextState(KATAKANA_NODE->CreateState());
                pNext->SetPrevState(pState);
                pNext->DoProcOnCreated();
                deckey = -1;    // この後は dekcey の処理をやらない
            }
            else if (pNode && dynamic_cast<EisuNode*>(pNode) == 0 && deckey == EISU_MODE_TOGGLE_DECKEY) {
                _LOG_DEBUGH(_T("CREATE: EisuState"));
                pNext = pState->SetNextState(EISU_NODE->CreateState());
                pNext->SetPrevState(pState);
                pNext->DoProcOnCreated();
                deckey = -1;    // この後は dekcey の処理をやらない
            }
            else if ((!pNode || !pNode->isStrokeTableNode()) && pState->isStrokableKey(deckey)) {
                // ルートストロークノードの生成
                _LOG_DEBUGH(_T("CREATE: RootStrokeState"));
                if (ROOT_STROKE_NODE) {
                    pNext = pState->SetNextState(ROOT_STROKE_NODE->CreateState());
                    pNext->SetPrevState(pState);
                }
            }
        }
    }
    _LOG_DEBUGH(_T("LEAVE: false: {}, NextNode={}"), pState->Name, NODE_NAME(pState->NextNodeMaybe()));
    return false;
}
