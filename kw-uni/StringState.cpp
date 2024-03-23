#include "Logger.h"

#include "History/HistoryDic.h"
#include "Settings.h"

#include "StringNode.h"
#include "OneShot/RewriteString.h"
#include "State.h"
#include "History/HistoryResidentState.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughString)

#if 1 || defined(_DEBUG)
#undef LOG_INFO
#undef LOG_DEBUGH
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#endif

// 文字列状態
class StringState : public State {
    DECLARE_CLASS_LOGGER;

private:
    inline const StringNode* myNode() const { return (const StringNode*)pNode; }

public:
    StringState(StringNode* pN) {
        LOG_INFO(_T("CALLED: ctor"));
        Initialize(logger.ClassNameT(), pN);
    }

    //// 文字列を変換
    //MString TranslateString(const MString& outStr) override {
    //    _LOG_DEBUGH(_T("CALLED"));
    //    return RewriteString::TranslateMiscChars(outStr);
    //}

    // 出力文字を取得する
    void GetResultStringChain(MStringResult& resultOut) override {
        LOG_DEBUGH(_T("ENTER: {}: resultStr={}, numBS={}"), Name, to_wstr(resultOut.resultStr()), resultOut.numBS());
        auto xlatStr = RewriteString::TranslateMiscChars(myNode()->getString());
        _LOG_DEBUGH(_T("CALLED: {}: myStr={}, xlatStr={}, rewLen={}"), Name, to_wstr(myNode()->getString()), to_wstr(xlatStr), myNode()->getRewritableLen());
        resultOut.setResultWithRewriteLen(xlatStr, myNode()->getRewritableLen());
        LOG_DEBUGH(_T("LEAVE: {}: resultStr={}, numBS={}"), Name, to_wstr(resultOut.resultStr()), resultOut.numBS());
    }

};
DEFINE_CLASS_LOGGER(StringState);

DEFINE_CLASS_LOGGER(StringNode);

// 文字列ノード
// コンストラクタ
StringNode::StringNode(StringRef s, bool bRewritable) : rewritableLen(0) {
    if (s.empty()) {            // 文字列がない場合
         str.clear();
    } else if (bRewritable) {   // 文字列がある場合 - 文字列を保存する
        // 出力定義文字列を解析して、分離記号の '/' を取り除き、書き換え対象文字列の長さを得る
        String ws = RewriteString::AnalyzeRewriteString(s, rewritableLen);
        str = to_mstr(ws);
        //str = to_mstr(utils::replace(s, _T("/"), _T("")));
        //size_t pos = s.find('/', 0);
        //rewritableLen = pos <= str.size() ? str.size() - pos : str.empty() ? 0 : 1;
    } else {
        str = to_mstr(s);
    }
}

StringNode::StringNode(wchar_t ch) : rewritableLen(0){
    str.clear();
    if (ch != 0) {
        str.push_back(ch);
    }
}

// 開始ノード
// 当ノードを処理する State インスタンスを作成する
State* StringNode::CreateState() {
    LOG_INFO(_T("CALLED"));
    return new StringState(this);
}

