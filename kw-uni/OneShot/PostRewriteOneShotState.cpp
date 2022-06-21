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
#include "History/History.h"

#include "PostRewriteOneShot.h"

#if 0
#define _LOG_DEBUGH_FLAG (false)
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
        { _T("か"), _T("が")},
        { _T("き"), _T("ぎ")},
        { _T("く"), _T("ぐ")},
        { _T("け"), _T("げ")},
        { _T("こ"), _T("ご")},
        { _T("さ"), _T("ざ")},
        { _T("し"), _T("じ")},
        { _T("す"), _T("ず")},
        { _T("せ"), _T("ぜ")},
        { _T("そ"), _T("ぞ")},
        { _T("た"), _T("だ")},
        { _T("ち"), _T("ぢ")},
        { _T("つ"), _T("づ")},
        { _T("て"), _T("で")},
        { _T("と"), _T("ど")},
        { _T("は"), _T("ば")},
        { _T("ひ"), _T("び")},
        { _T("ふ"), _T("ぶ")},
        { _T("へ"), _T("べ")},
        { _T("ほ"), _T("ぼ")},
    };

    std::map<wstring, wstring> handakuonDic = {
        { _T("は"), _T("ぱ")},
        { _T("ひ"), _T("ぴ")},
        { _T("ふ"), _T("ぷ")},
        { _T("へ"), _T("ぺ")},
        { _T("ほ"), _T("ぽ")},
    };

    // -------------------------------------------------------------------
    // 状態クラス
    class PostRewriteOneShotState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        PostRewriteOneShotState(PostRewriteOneShotNode* pN) {
            LOG_INFO(_T("CALLED: constructor: this=%p, NodePtr=%p"), this, pN);
            Initialize(logger.ClassNameT(), pN);
        }

        ~PostRewriteOneShotState() {
            LOG_INFO(_T("CALLED: destructor: ptr=%p"), this);
        };

#define NAME_PTR (Name.c_str())
#define MY_NODE ((PostRewriteOneShotNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER: %s"), MY_NODE->getDebugString().c_str());

            bool bRewrited = false;
            const MString targetStr = OUTPUT_STACK->backStringWhileOnlyRewritable(5);
            _LOG_DEBUGH(_T("targetStr=%s"), MAKE_WPTR(targetStr));
            for (size_t pos = 0; pos < targetStr.size(); ++pos) {
                _LOG_DEBUGH(_T("subStr=%s, pos=%d"), MAKE_WPTR(targetStr.substr(pos)), pos);
                const RewriteInfo* rewInfo = MY_NODE->getRewriteInfo(targetStr.substr(pos));
                if (rewInfo) {
                    int numBS = targetStr.size() - pos;
                    _LOG_DEBUGH(_T("REWRITE: outStr=%s, rewritableLen=%d, subTable=%p, numBS=%d"), MAKE_WPTR(rewInfo->rewriteStr), rewInfo->rewritableLen, rewInfo->subTable, numBS);
                    HISTORY_STAY_STATE->SetTranslatedOutString(rewInfo->rewriteStr, rewInfo->rewritableLen, numBS);
                    if (rewInfo->subTable) {
                        SetNextNodeMaybe(rewInfo->subTable);
                    }
                    bRewrited = true;
                    break;
                }
            }
            if (!bRewrited) {
                HISTORY_STAY_STATE->SetTranslatedOutString(MY_NODE->getString(), MY_NODE->getRewritableLen());
            }

            // チェイン不要
            _LOG_DEBUGH(_T("LEAVE: NO CHAIN"));

            return false;
        }

    };
    DEFINE_CLASS_LOGGER(PostRewriteOneShotState);

} // namespace

// -------------------------------------------------------------------
// PostRewriteOneShotNode - 書き換えノード
DEFINE_CLASS_LOGGER(PostRewriteOneShotNode);

// コンストラクタ
PostRewriteOneShotNode::PostRewriteOneShotNode(const wstring& s, bool bBare)
{
    LOG_INFO(_T("ENTER: constructor: ptr=%p, s=%s, bBare=%s"), this, s.c_str(), BOOL_TO_WPTR(bBare));
    wstring rewStr = s;
    size_t rewLen = 0;
    if (bBare) {
        rewStr = utils::replace(rewStr, _T("/"), _T(""));
        size_t pos = s.find('/', 0);
        rewLen = pos < rewStr.size() ? rewStr.size() - pos : rewStr.empty() ? 0 : 1;
    }
    myRewriteInfo.rewriteStr = to_mstr(rewStr);
    myRewriteInfo.rewritableLen = rewLen;
    LOG_INFO(_T("LEAVE: myStr=%s, myRewriteLen=%d"), rewStr.c_str(), rewLen);
}

// デストラクタ
PostRewriteOneShotNode::~PostRewriteOneShotNode() {
    LOG_INFO(_T("CALLED: destructor: ptr=%p"), this);
    for (auto p : subTables) {
        delete p;
    }
}

// 当ノードを処理する State インスタンスを作成する
State* PostRewriteOneShotNode::CreateState() {
    return new PostRewriteOneShotState(this);
}

void PostRewriteOneShotNode::addRewritePair(const wstring& key, const wstring& value, bool bBare, StrokeTableNode* pNode) {
    LOG_INFO(_T("CALLED: key=%s, value=%s, bBare=%s, pNode=%p"), key.c_str(), value.c_str(), BOOL_TO_WPTR(bBare), pNode);
    wstring rewStr = value;
    size_t rewLen = 0;
    if (bBare) {
        rewStr = utils::replace(rewStr, _T("/"), _T(""));
        size_t pos = value.find('/', 0);
        rewLen = pos < rewStr.size() ? rewStr.size() - pos : rewStr.empty() ? 0 : 1;
    }
    if (pNode) {
        subTables.push_back(pNode);
    }

    rewriteMap[to_mstr(key)] = RewriteInfo(to_mstr(rewStr), rewLen, pNode);
}

const wstring PostRewriteOneShotNode::getDebugString() const {
    wstring result = _T("myStr: \"");
    result.append(myRewriteInfo.getDebugStr()).append(_T("\", rewriteMap="));
    bool bFirst = true;
    for (auto pair : rewriteMap) {
        if (!bFirst) result.append(_T(", "));
        result.append(_T("\"")).append(to_wstr(pair.first)).append(_T(":")).append(pair.second.getDebugStr()).append(_T("\""));
        bFirst = false;
    }
    return result;
}

// -------------------------------------------------------------------
// DakutenOneShotNode - 濁点書き換えノード
DEFINE_CLASS_LOGGER(DakutenOneShotNode);

// コンストラクタ
DakutenOneShotNode::DakutenOneShotNode(wstring mkstr)
    : PostRewriteOneShotNode(mkstr, false), markStr(to_mstr(mkstr)), postfix(mkstr)
{
    LOG_INFO(_T("CALLED: constructor"));
    const auto& dic = mkstr == _T("゛") ? dakuonDic : handakuonDic;
    for (auto pair : dic) {
        addRewritePair(pair.first, pair.second, true, 0);
    }
}

// デストラクタ
DakutenOneShotNode::~DakutenOneShotNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* DakutenOneShotNode::CreateState() {
    return new PostRewriteOneShotState(this);
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

