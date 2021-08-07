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
            MString comp = BUSHU_COMP_NODE->ReduceByBushu(m1, m2);
            LOG_DEBUG(_T("COMP: %s"), MAKE_WPTR(comp));
            STATE_COMMON->SetOutString(comp);
            if (!comp.empty()) {
                setCharDeleteInfo(2);
                copyStrokeHelpToVkbFaces();
                //合成した文字を履歴に登録
                if (HISTORY_DIC) HISTORY_DIC->AddNewEntry(utils::last_substr(comp, 1));
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

// 部首合成の実行
MString BushuCompNode::ReduceByBushu(mchar_t m1, mchar_t m2, mchar_t prev) {
    if (BUSHU_DIC) {
        mchar_t m = BUSHU_DIC->FindComposite(m1, m2, prev);
        PrevBushu1 = m1;
        PrevBushu2 = m2;
        PrevComp = m;
        //PrevCompSec = utils::getSecondsFromEpochTime();
        PrevTotalCount = STATE_COMMON->GetTotalDecKeyCount();
        return to_mstr(m);
    }
    return EMPTY_MSTR;
}

// 後置部首合成機能ノードのSingleton
// unique_ptr による管理は下記 BushuCompNodeBuilder の呼び出し側で行う
BushuCompNode* BushuCompNode::Singleton;

// -------------------------------------------------------------------
// BushuCompNode - 後置部首合成機能ノードビルダー
DEFINE_CLASS_LOGGER(BushuCompNodeBuilder);

Node* BushuCompNodeBuilder::CreateNode() {
    LOG_INFO(_T("CALLED"));
    // 部首合成辞書の読み込み(ファイルが指定されていなくても、辞書は構築する)
    // 部首合成辞書ファイル名
    auto bushuFile = SETTINGS->bushuFile;
    LOG_INFO(_T("bushuFile=%s"), bushuFile.c_str());

    //if (bushuFile.empty()) {
    //    ERROR_HANDLER->Warn(_T("「bushu=(ファイル名)」の設定がまちがっているようです"));
    //}
    BushuDic::CreateBushuDic(bushuFile);

    BUSHU_COMP_NODE = new BushuCompNode();
    return BUSHU_COMP_NODE;
}

