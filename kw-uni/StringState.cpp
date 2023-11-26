#include "Logger.h"

#include "History/HistoryDic.h"
#include "Settings.h"

#include "StringNode.h"
#include "OneShot/RewriteString.h"
#include "State.h"
#include "History/HistoryResidentState.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughString)

#if 0 || defined(_DEBUG)
#undef _DEBUG_SENT
#undef _DEBUG_FLAG
#undef LOG_INFO
#undef LOG_DEBUGH
#undef LOG_DEBUG
#undef _LOG_DEBUGH
#undef _LOG_DEBUGH_COND
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

namespace {
    void convertZenkakuPeriod(MString& ms) {
        for (size_t i = 0; i < ms.size(); ++i) {
            if (ms[i] == 0x3002) ms[i] = 0xff0e;    // 。→．
            else if (ms[i] == 0xff0e) ms[i] = 0x3002;    // ．→。
        }
    }

    void convertZenkakuComma(MString& ms) {
        for (size_t i = 0; i < ms.size(); ++i) {
            if (ms[i] == 0x3001) ms[i] = 0xff0c;    // 。→．
            else if (ms[i] == 0xff0c) ms[i] = 0x3001;    // ．→。
        }
    }

    MString xlat(const MString& ms) {
        MString result = STATE_COMMON->IsHiraganaToKatakana() ? utils::convert_hiragana_to_katakana(ms) : ms;
        if (SETTINGS->convertJaPeriod) convertZenkakuPeriod(result);
        if (SETTINGS->convertJaComma) convertZenkakuComma(result);
        return result;
    };

}

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

    // 文字列状態に対して生成時処理を実行する
    bool DoProcOnCreated() {
        _LOG_DEBUGH(_T("ENTER: StringState: str={}, rewLen={}"), to_wstr(myNode()->getString()), myNode()->getRewritableLen());
        HISTORY_RESIDENT_STATE->SetTranslatedOutString(xlat(myNode()->getString()), myNode()->getRewritableLen());
        _LOG_DEBUGH(_T("LEAVE: StringState"));
        // チェイン不要
        return false;
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

