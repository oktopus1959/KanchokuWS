//#include "pch.h"
#include "Logger.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "KanchokuIni.h"
#include "Constants.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "Node.h"
#include "State.h"
#include "OutputStack.h"

#include "BushuComp.h"
#include "BushuDic.h"
#include "History/HistoryDic.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughBushu)

#if 0
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
    // 後置部首合成機能状態クラス
    class BushuCompState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        BushuCompState(BushuCompNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~BushuCompState() { };

#define MY_NODE ((BushuCompNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        void DoProcOnCreated() override {
            wchar_t m1 = (wchar_t)OUTPUT_STACK->LastOutStackChar(1);
            wchar_t m2 = (wchar_t)OUTPUT_STACK->LastOutStackChar(0);
            LOG_DEBUG(_T("m1={}, m2={}"), m1, m2);
            if (m1 == '-' || m1 == ' ') {
                // 先頭文字が '-' なら、その前の文字と直前の文字との組合せに対して自動部首合成を無効化する
                if (BUSHU_DIC) {
                    wchar_t m0 = (wchar_t)OUTPUT_STACK->LastOutStackChar(2);
                    if (m0 != 0) BUSHU_DIC->AddAutoBushuEntry(m0, m2, '-');
                }
            } else {
                auto result = BUSHU_COMP_NODE->ReduceByBushu(m1, m2);
                LOG_DEBUG(_T("COMP: {}"), to_wstr(result));
                if (!result.empty()) {
                    resultStr.setResult(result, 2);
                    copyStrokeHelpToVkbFaces();
                    //合成した文字を履歴に登録
                    if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(utils::last_substr(result, 1));
                }
            }
        }

        //// 出力文字を取得する
        //void GetResultStringChain(MStringResult& result) override {
        //    result.setResult(resultStr);
        //}

    };
    DEFINE_CLASS_LOGGER(BushuCompState);

} // namespace

// -------------------------------------------------------------------
// BushuCompNode - 後置部首合成機能ノード
DEFINE_CLASS_LOGGER(BushuCompNode);

// コンストラクタ
BushuCompNode::BushuCompNode() {
    LOG_DEBUGH(_T("CALLED: constructor"));
}

// デストラクタ
BushuCompNode::~BushuCompNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* BushuCompNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new BushuCompState(this);
}

#define VALIDATE_CHAR(c) (c == 0 ? ' ' : c)

// 部首合成の実行
MString BushuCompNode::ReduceByBushu(mchar_t m1, mchar_t m2, mchar_t prev) {
    if (BUSHU_DIC) {
        size_t prevCnt = PrevTotalCount;
        size_t totalCnt = STATE_COMMON->GetTotalDecKeyCount();
        PrevTotalCount = totalCnt;
        bool prevAuto = IsPrevAuto;
        IsPrevAuto = false;
        mchar_t outChar = OUTPUT_STACK->isLastOutputStackCharBlocker() ? 0 : OUTPUT_STACK->LastOutStackChar();
        _LOG_DEBUGH(_T("CALLED: m1={}, m2={}, prev={}, prevTotalCount={}, prevCnt={}, outChar={}, PrevComp={}, PrevAuto={}"), \
            VALIDATE_CHAR(m1), VALIDATE_CHAR(m2), VALIDATE_CHAR(prev), totalCnt, prevCnt, VALIDATE_CHAR(outChar), VALIDATE_CHAR(PrevComp), prevAuto);
        if (!prevAuto || totalCnt > prevCnt + 2 || outChar == 0 || outChar != PrevComp) {
            mchar_t m = BUSHU_DIC->FindComposite(m1, m2, prev);
            PrevBushu1 = m1;
            PrevBushu2 = m2;
            PrevComp = m;
            IsPrevAutoCancel = false;
            //PrevCompSec = utils::getSecondsFromEpochTime();
            return to_mstr(m);
        }
    }
    return EMPTY_MSTR;
}

// 自動部首合成の実行
bool BushuCompNode::ReduceByAutoBushu(const MString& mstr, MStringResult& resultOut) {
    if (BUSHU_DIC && !mstr.empty()) {
        size_t prevTotalCnt = PrevTotalCount;
        PrevTotalCount = STATE_COMMON->GetTotalDecKeyCount();
        size_t firstStrokeCnt = STATE_COMMON->GetFirstStrokeKeyCount();
        _LOG_DEBUGH(_T("ENTER: mstr={}, prevTotalCount={}, firstStrokeKeyCount={}"), to_wstr(mstr), prevTotalCnt, firstStrokeCnt);
        if (prevTotalCnt + 1 == firstStrokeCnt) {
            mchar_t m1 = OUTPUT_STACK->LastOutStackChar(0);
            mchar_t m2 = mstr[0];
            mchar_t m = BUSHU_DIC->FindAutoComposite(m1, m2);
            PrevBushu1 = m1;
            PrevBushu2 = m2;
            PrevComp = m;
            IsPrevAuto = false;
            IsPrevAutoCancel = false;
            //PrevCompSec = utils::getSecondsFromEpochTime();
            _LOG_DEBUGH(_T("m1={}, m2={}, m={}"), (wchar_t)m1, (wchar_t)m2, (wchar_t)m);
            if (m != 0) {
                _LOG_DEBUGH(_T("resultOut(m={}, numBS=1)"), (wchar_t)m);
                MString ms = to_mstr(m);
                resultOut.setResult(ms, 1);
                STATE_COMMON->CopyStrokeHelpToVkbFaces((wchar_t)m);
                IsPrevAuto = true;
                //合成した文字を履歴に登録
                if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(ms);
                _LOG_DEBUGH(_T("LEAVE: true"));
                return true;
            }
        }
    }
    _LOG_DEBUGH(_T("LEAVE: false"));
    return false;
}

// 後置部首合成機能ノードのSingleton
// unique_ptr による管理は下記 BushuCompNodeBuilder の呼び出し側で行う
std::unique_ptr<BushuCompNode> BushuCompNode::_singleton;

BushuCompNode* BushuCompNode::Singleton() {
    if (!_singleton) _singleton.reset(new BushuCompNode());
    return _singleton.get();
}

// -------------------------------------------------------------------
// BushuCompNode - 後置部首合成機能ノードビルダー
DEFINE_CLASS_LOGGER(BushuCompNodeBuilder);

Node* BushuCompNodeBuilder::CreateNode() {
    LOG_DEBUGH(_T("CALLED"));

    return new BushuCompNode();
}

