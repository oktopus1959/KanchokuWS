#include "Logger.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "KanchokuIni.h"
#include "Constants.h"
#include "DeckeyToChars.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "Node.h"
#include "State.h"
#include "OutputStack.h"

#include "DakutenOneShot.h"

#define _LOG_DEBUGH_FLAG (false)
#if 0
#define IS_LOG_DEBUGH_ENABLED true
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

namespace {

    // -------------------------------------------------------------------
    // 濁音
    std::map<wstring, wstring> dakuonDic = {
        { _T("か゛"), _T("が")},
        { _T("き゛"), _T("ぎ")},
        { _T("く゛"), _T("ぐ")},
        { _T("け゛"), _T("げ")},
        { _T("こ゛"), _T("ご")},
        { _T("さ゛"), _T("ざ")},
        { _T("し゛"), _T("じ")},
        { _T("す゛"), _T("ず")},
        { _T("せ゛"), _T("ぜ")},
        { _T("そ゛"), _T("ぞ")},
        { _T("た゛"), _T("だ")},
        { _T("ち゛"), _T("ぢ")},
        { _T("つ゛"), _T("づ")},
        { _T("て゛"), _T("で")},
        { _T("と゛"), _T("ど")},
        { _T("は゛"), _T("ば")},
        { _T("ひ゛"), _T("び")},
        { _T("ふ゛"), _T("ぶ")},
        { _T("へ゛"), _T("べ")},
        { _T("ほ゛"), _T("ぼ")},
        { _T("は゜"), _T("ぱ")},
        { _T("ひ゜"), _T("ぴ")},
        { _T("ふ゜"), _T("ぷ")},
        { _T("へ゜"), _T("ぺ")},
        { _T("ほ゜"), _T("ぽ")},
    };

    // -------------------------------------------------------------------
    // 状態クラス
    class DakutenOneShotState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        DakutenOneShotState(DakutenOneShotNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~DakutenOneShotState() { };

#define NAME_PTR (Name.c_str())
#define MY_NODE ((DakutenOneShotNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER"));

            wstring str;
            str.push_back((wchar_t)OUTPUT_STACK->OutputStackLastChar());
            str.append(MY_NODE->getPostfix());
            auto iter = dakuonDic.find(str);
            if (iter != dakuonDic.end()) {
                _LOG_DEBUGH(_T("Dakuon: outStr=%s, numBS=%d"), iter->second.c_str(), 1);
                STATE_COMMON->SetOutString(to_mstr(iter->second), 1);
            } else {
                STATE_COMMON->SetOutString(MY_NODE->getString(), 0);
            }

            // チェイン不要
            _LOG_DEBUGH(_T("LEAVE: NO CHAIN"));

            return false;
        }

    };
    DEFINE_CLASS_LOGGER(DakutenOneShotState);

} // namespace

// -------------------------------------------------------------------
// DakutenOneShotNode - ノードのテンプレート
DEFINE_CLASS_LOGGER(DakutenOneShotNode);

// コンストラクタ
DakutenOneShotNode::DakutenOneShotNode(wstring mkstr)
    : markStr(to_mstr(mkstr)), postfix(mkstr)
{
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
DakutenOneShotNode::~DakutenOneShotNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* DakutenOneShotNode::CreateState() {
    return new DakutenOneShotState(this);
}

// -------------------------------------------------------------------
// DakutenOneShotNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(DakutenOneShotNodeBuilder);

Node* DakutenOneShotNodeBuilder::CreateNode() {
    return new DakutenOneShotNode(_T("゛"));
}

DEFINE_CLASS_LOGGER(HanDakutenOneShotNodeBuilder);

Node* HanDakutenOneShotNodeBuilder::CreateNode() {
    return new DakutenOneShotNode(_T("゜"));
}

