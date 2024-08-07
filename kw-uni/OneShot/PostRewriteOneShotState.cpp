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

#include "RewriteString.h"
#include "PostRewriteOneShot.h"

#if 1
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
    // 濁音
    std::map<String, String> dakuonDic = {
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

    std::map<String, String> handakuonDic = {
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
            LOG_INFO(_T("CALLED: constructor: this={:p}, NodePtr={:p}"), (void*)this, (void*)pN);
            Initialize(logger.ClassNameT(), pN);
        }

        ~PostRewriteOneShotState() {
            LOG_INFO(_T("CALLED: destructor: ptr={:p}"), (void*)this);
        };

#define MY_NODE ((PostRewriteOneShotNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            _LOG_DEBUGH(_T("ENTER: {}"), MY_NODE->getDebugString());

            const RewriteInfo* rewInfo;
            size_t numBS;
            std::tie(rewInfo, numBS) = MY_NODE->matchWithTailString();
            if (rewInfo) {
                HISTORY_RESIDENT_STATE->SetTranslatedOutString(rewInfo->rewriteStr, rewInfo->rewritableLen, false, numBS);
                if (rewInfo->subTable) {
                    SetNextNodeMaybe(rewInfo->subTable);
                }
            } else {
                HISTORY_RESIDENT_STATE->SetTranslatedOutString(MY_NODE->getString(), MY_NODE->getRewritableLen());
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

#include "RewriteString.h"

// コンストラクタ
PostRewriteOneShotNode::PostRewriteOneShotNode(StringRef s, bool bBare)
{
    LOG_DEBUGH(_T("ENTER: constructor: ptr={:p}, s={}, bBare={}"), (void*)this, s, bBare);
    String rewStr = s;
    size_t rewLen = 0;
    if (bBare) {
        // 出力定義文字列を解析して、分離記号の '/' を取り除き、書き換え対象文字列の長さを得る
        rewStr = RewriteString::AnalyzeRewriteString(s, rewLen);
    }
    myRewriteInfo.rewriteStr = to_mstr(rewStr);
    myRewriteInfo.rewritableLen = rewLen;
    LOG_DEBUGH(_T("LEAVE: myStr={}, myRewriteLen={}"), rewStr, rewLen);
}

// デストラクタ
PostRewriteOneShotNode::~PostRewriteOneShotNode() {
    LOG_DEBUGH(_T("CALLED: destructor: ptr={:p}"), (void*)this);
    for (auto p : subTables) {
        delete p;
    }
}

// 当ノードを処理する State インスタンスを作成する
State* PostRewriteOneShotNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new PostRewriteOneShotState(this);
}

void PostRewriteOneShotNode::addRewritePair(StringRef key, StringRef value, bool bBare, StrokeTableNode* pNode) {
    LOG_DEBUGH(_T("ENTER: key={}, value={}, bBare={}, pNode={:p}"), key, value, bBare, (void*)pNode);
    String rewStr = value;
    size_t rewLen = 0;
    if (bBare) {
        // 出力定義文字列を解析して、分離記号の '/' を取り除き、書き換え対象文字列の長さを得る
        rewStr = RewriteString::AnalyzeRewriteString(value, rewLen);
    }
    if (pNode) {
        subTables.push_back(pNode);
    }

    rewriteMap[to_mstr(key)] = RewriteInfo(to_mstr(rewStr), rewLen, pNode);

    LOG_DEBUGH(_T("LEAVE: rewStr={}, rewLen={}"), rewStr, rewLen);
}

// 末尾文字列にマッチする RewriteInfo を取得する
std::tuple<const RewriteInfo*, size_t> PostRewriteOneShotNode::matchWithTailString() const {
    size_t maxlen = SETTINGS->kanaTrainingMode && ROOT_STROKE_NODE->hasOnlyUsualRewriteNdoe() ? 0 : 8;     // かな入力練習モードで濁点のみなら書き換えをやらない
    while (maxlen > 0) {
        _LOG_DEBUGH(_T("maxlen={}"), maxlen);
        const MString targetStr = SETTINGS->googleCompatible ? OUTPUT_STACK->backStringWhileOnlyRewritable(maxlen) : OUTPUT_STACK->BackStringUptoNewLine(maxlen);
        _LOG_DEBUGH(_T("targetStr={}"), to_wstr(targetStr));
        if (targetStr.empty()) break;

        const RewriteInfo* rewInfo = getRewriteInfo(targetStr);
        if (rewInfo) {
            _LOG_DEBUGH(_T("REWRITE_INFO found: outStr={}, rewritableLen={}, subTable={:p}"), to_wstr(rewInfo->rewriteStr), rewInfo->rewritableLen, (void*)rewInfo->subTable);
            return { rewInfo, targetStr.size() };
        }

        maxlen = targetStr.size() - 1;
    }
    return { 0, 0 };
}

const String PostRewriteOneShotNode::getDebugString() const {
    String result = _T("myStr: \"");
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
DakutenOneShotNode::DakutenOneShotNode(String mkstr)
    : PostRewriteOneShotNode(mkstr, false), markStr(to_mstr(mkstr)), postfix(mkstr)
{
    LOG_DEBUGH(_T("CALLED: constructor"));
    const auto& dic = mkstr == _T("゛") ? dakuonDic : handakuonDic;
    for (auto pair : dic) {
        addRewritePair(pair.first, pair.second, true, 0);
    }
}

// デストラクタ
DakutenOneShotNode::~DakutenOneShotNode() {
    LOG_DEBUGH(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* DakutenOneShotNode::CreateState() {
    LOG_INFO(_T("CALLED"));
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

