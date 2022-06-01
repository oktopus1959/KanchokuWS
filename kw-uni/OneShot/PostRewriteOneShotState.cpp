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

#include "PostRewriteOneShot.h"

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
            _LOG_DEBUGH(_T("ENTER"));

            bool bRewrited = false;
            for (size_t len = 3; len > 0; --len) {
                const MString& rewStr = MY_NODE->getRewriteStr(OUTPUT_STACK->OutputStackBackStr(len));
                if (!rewStr.empty()) {
                    _LOG_DEBUGH(_T("REWRITE: outStr=%s, numBS=%d"), MAKE_WPTR(rewStr), len);
                    STATE_COMMON->SetOutString(rewStr, len);
                    bRewrited = true;
                    break;
                }
            }
            if (!bRewrited) {
                STATE_COMMON->SetOutString(MY_NODE->getString(), 0);
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
PostRewriteOneShotNode::PostRewriteOneShotNode(const wstring& s)
    : myStr(to_mstr(s))
{
    LOG_INFO(_T("CALLED: constructor: myStr=%s"), s.c_str());
}

// デストラクタ
PostRewriteOneShotNode::~PostRewriteOneShotNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* PostRewriteOneShotNode::CreateState() {
    return new PostRewriteOneShotState(this);
}

