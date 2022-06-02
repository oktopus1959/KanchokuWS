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
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

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
        bool DoProcOnCreated() {
            wchar_t m1 = (wchar_t)OUTPUT_STACK->LastOutStackChar(1);
            wchar_t m2 = (wchar_t)OUTPUT_STACK->LastOutStackChar(0);
            if (m1 == '-' || m1 == ' ') {
                // 先頭文字が '-' なら、その前の文字と直前の文字との組合せに対して自動部首合成を無効化する
                if (BUSHU_DIC) {
                    wchar_t m0 = (wchar_t)OUTPUT_STACK->LastOutStackChar(2);
                    if (m0 != 0) BUSHU_DIC->AddAutoBushuEntry(m0, m2, '-');
                }
            } else {
                MString comp = BUSHU_COMP_NODE->ReduceByBushu(m1, m2);
                LOG_DEBUG(_T("COMP: %s"), MAKE_WPTR(comp));
                STATE_COMMON->SetOutString(comp);
                if (!comp.empty()) {
                    setCharDeleteInfo(2);
                    copyStrokeHelpToVkbFaces();
                    //合成した文字を履歴に登録
                    if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(utils::last_substr(comp, 1));
                }
            }
            // チェイン不要
            return false;
        }

    };
    DEFINE_CLASS_LOGGER(BushuCompState);

} // namespace

// -------------------------------------------------------------------
// BushuCompNode - 後置部首合成機能ノード
DEFINE_CLASS_LOGGER(BushuCompNode);

// コンストラクタ
BushuCompNode::BushuCompNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
BushuCompNode::~BushuCompNode() {
}

// 当ノードを処理する State インスタンスを作成する
State* BushuCompNode::CreateState() {
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
        _LOG_DEBUGH(_T("CALLED: m1=%c, m2=%c, prev=%c, prevTotalCount=%d, prevCnt=%d, outChar=%c, PrevComp=%c, PrevAuto=%s"), \
            VALIDATE_CHAR(m1), VALIDATE_CHAR(m2), VALIDATE_CHAR(prev), totalCnt, prevCnt, VALIDATE_CHAR(outChar), VALIDATE_CHAR(PrevComp), BOOL_TO_WPTR(prevAuto));
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
bool BushuCompNode::ReduceByAutoBushu(const MString& mstr) {
    if (BUSHU_DIC && !mstr.empty()) {
        size_t prevTotalCnt = PrevTotalCount;
        PrevTotalCount = STATE_COMMON->GetTotalDecKeyCount();
        size_t firstStrokeCnt = STATE_COMMON->GetFirstStrokeKeyCount();
        _LOG_DEBUGH(_T("CALLED: mstr=%s, prevTotalCount=%d, firstStrokeKeyCount=%d"), MAKE_WPTR(mstr), prevTotalCnt, firstStrokeCnt);
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
            _LOG_DEBUGH(_T("m1=%c, m2=%c, m=%c"), m1, m2, m);
            if (m != 0) {
                MString ms = to_mstr(m);
                STATE_COMMON->SetOutString(ms);
                STATE_COMMON->SetBackspaceNum(1);
                STATE_COMMON->CopyStrokeHelpToVkbFaces();
                IsPrevAuto = true;
                //合成した文字を履歴に登録
                if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(ms);
                return true;
            }
        }
    }
    return false;
}

// 後置部首合成機能ノードのSingleton
// unique_ptr による管理は下記 BushuCompNodeBuilder の呼び出し側で行う
std::unique_ptr<BushuCompNode> BushuCompNode::Singleton;

// Singletonノードの生成
void BushuCompNode::CreateSingleton() {
    if (!Singleton) {
        Singleton.reset(new BushuCompNode());
    }
}

// -------------------------------------------------------------------
// BushuCompNode - 後置部首合成機能ノードビルダー
DEFINE_CLASS_LOGGER(BushuCompNodeBuilder);

Node* BushuCompNodeBuilder::CreateNode() {
    LOG_INFO(_T("CALLED"));
    // 部首合成辞書の読み込み(ファイルが指定されていなくても、辞書は構築する)
    // 部首合成辞書ファイル名
    auto bushuFile = SETTINGS->bushuFile;
    auto auotBushuFile = SETTINGS->autoBushuFile;
    LOG_INFO(_T("bushuFile=%s, autoBushuFile=%s"), bushuFile.c_str(), auotBushuFile.c_str());

    //if (bushuFile.empty()) {
    //    ERROR_HANDLER->Warn(_T("「bushu=(ファイル名)」の設定がまちがっているようです"));
    //}
    // この中では、Singleton によって一回だけ生成されるようになっている
    BushuDic::CreateBushuDic(bushuFile, auotBushuFile);

    return new BushuCompNode();
}

