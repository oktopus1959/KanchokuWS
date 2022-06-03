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

#define _LOG_DEBUGH_FLAG (false)
#if 1
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
    // 状態クラス
    class PostRewriteOneShotState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        PostRewriteOneShotState(PostRewriteOneShotNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~PostRewriteOneShotState() { };

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
                    _LOG_DEBUGH(_T("REWRITE: outStr=%s, rewritableLen=%d, numBS=%d"), MAKE_WPTR(rewInfo->rewriteStr), rewInfo->rewritableLen, numBS);
                    HISTORY_STAY_STATE->SetTranslatedOutString(rewInfo->rewriteStr, rewInfo->rewritableLen, numBS);
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
// PostRewriteOneShotNode - ノードのテンプレート
DEFINE_CLASS_LOGGER(PostRewriteOneShotNode);

// コンストラクタ
PostRewriteOneShotNode::PostRewriteOneShotNode(const wstring& s, bool bBare)
{
    LOG_INFO(_T("CALLED: constructor: s=%s, bBare=%s"), s.c_str(), BOOL_TO_WPTR(bBare));
    wstring rewStr = s;
    myRewriteLen = 0;
    if (bBare) {
        rewStr = utils::replace(rewStr, _T("/"), _T(""));
        size_t pos = s.find('/', 0);
        myRewriteLen = pos < rewStr.size() ? rewStr.size() - pos : rewStr.empty() ? 0 : 1;
    }
    myStr = to_mstr(rewStr);
    LOG_INFO(_T("LEAVE: myStr=%s, myRewriteLen=%d"), MAKE_WPTR(myStr), myRewriteLen);
}

// デストラクタ
PostRewriteOneShotNode::~PostRewriteOneShotNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* PostRewriteOneShotNode::CreateState() {
    return new PostRewriteOneShotState(this);
}

void PostRewriteOneShotNode::addRewritePair(const wstring& key, const wstring& value, bool bBare) {
    wstring rewStr = value;
    size_t rewLen = 0;
    if (bBare) {
        rewStr = utils::replace(rewStr, _T("/"), _T(""));
        size_t pos = value.find('/', 0);
        rewLen = pos < rewStr.size() ? rewStr.size() - pos : rewStr.empty() ? 0 : 1;
    }
    rewriteMap[to_mstr(key)] = RewriteInfo(to_mstr(rewStr), rewLen);
}

const wstring PostRewriteOneShotNode::getDebugString() const {
    wstring result = _T("myStr: ");
    result.append(to_wstr(myStr)).append(_T(", rewriteMap="));
    bool bFirst = true;
    for (auto pair : rewriteMap) {
        if (!bFirst) result.append(_T(", "));
        result.append(to_wstr(pair.first)).append(_T(":")).append(to_wstr(pair.second.rewriteStr)).append(_T(":")).append(std::to_wstring(pair.second.rewritableLen));
        bFirst = false;
    }
    return result;
}
