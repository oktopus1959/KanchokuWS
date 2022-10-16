#include "Logger.h"

#include "History/HistoryDic.h"
#include "Settings.h"

#include "StringNode.h"
#include "State.h"
#include "History/HistoryStayState.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughString)

#if 0 || defined(_DEBUG)
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
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
        Initialize(logger.ClassNameT(), pN);
    }

    // 文字列状態に対して生成時処理を実行する
    bool DoProcOnCreated() {
        _LOG_DEBUGH(_T("ENTER: StringState: str=%s, rewLen=%d"), MAKE_WPTR(myNode()->getString()), myNode()->getRewritableLen());
        HISTORY_STAY_STATE->SetTranslatedOutString(xlat(myNode()->getString()), myNode()->getRewritableLen());
        _LOG_DEBUGH(_T("LEAVE: StringState"));
        // チェイン不要
        return false;
    }
};
DEFINE_CLASS_LOGGER(StringState);

// 開始ノード
// 当ノードを処理する State インスタンスを作成する
State* StringNode::CreateState() { return new StringState(this); }

