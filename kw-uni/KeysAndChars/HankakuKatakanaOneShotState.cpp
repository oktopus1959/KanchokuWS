#include "Logger.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "KanchokuIni.h"
#include "Constants.h"
#include "HotkeyToChars.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "Node.h"
#include "State.h"
#include "OutputStack.h"

#include "HankakuKatakanaOneShot.h"

namespace {

    size_t katakana_to_hankaku(MString& result, mchar_t ch) {
        switch (ch) {
        case 0x30f2: result.push_back(0xff66); return 1;
        case 0x30a1: result.push_back(0xff67); return 1;
        case 0x30a3: result.push_back(0xff68); return 1;
        case 0x30a5: result.push_back(0xff69); return 1;
        case 0x30a7: result.push_back(0xff6a); return 1;
        case 0x30a9: result.push_back(0xff6b); return 1;
        case 0x30e3: result.push_back(0xff6c); return 1;
        case 0x30e5: result.push_back(0xff6d); return 1;
        case 0x30e7: result.push_back(0xff6e); return 1;
        case 0x30c3: result.push_back(0xff6f); return 1;
        case 0x30fc: result.push_back(0xff70); return 1;
        case 0x30a2: result.push_back(0xff71); return 1;
        case 0x30a4: result.push_back(0xff72); return 1;
        case 0x30a6: result.push_back(0xff73); return 1;
        case 0x30a8: result.push_back(0xff74); return 1;
        case 0x30aa: result.push_back(0xff75); return 1;
        case 0x30ab: result.push_back(0xff76); return 1;
        case 0x30ad: result.push_back(0xff77); return 1;
        case 0x30af: result.push_back(0xff78); return 1;
        case 0x30b1: result.push_back(0xff79); return 1;
        case 0x30b3: result.push_back(0xff7a); return 1;
        case 0x30b5: result.push_back(0xff7b); return 1;
        case 0x30b7: result.push_back(0xff7c); return 1;
        case 0x30b9: result.push_back(0xff7d); return 1;
        case 0x30bb: result.push_back(0xff7e); return 1;
        case 0x30bd: result.push_back(0xff7f); return 1;
        case 0x30bf: result.push_back(0xff80); return 1;
        case 0x30c1: result.push_back(0xff81); return 1;
        case 0x30c4: result.push_back(0xff82); return 1;
        case 0x30c6: result.push_back(0xff83); return 1;
        case 0x30c8: result.push_back(0xff84); return 1;
        case 0x30ca: result.push_back(0xff85); return 1;
        case 0x30cb: result.push_back(0xff86); return 1;
        case 0x30cc: result.push_back(0xff87); return 1;
        case 0x30cd: result.push_back(0xff88); return 1;
        case 0x30ce: result.push_back(0xff89); return 1;
        case 0x30cf: result.push_back(0xff8a); return 1;
        case 0x30d2: result.push_back(0xff8b); return 1;
        case 0x30d5: result.push_back(0xff8c); return 1;
        case 0x30d8: result.push_back(0xff8d); return 1;
        case 0x30db: result.push_back(0xff8e); return 1;
        case 0x30de: result.push_back(0xff8f); return 1;
        case 0x30df: result.push_back(0xff90); return 1;
        case 0x30e0: result.push_back(0xff91); return 1;
        case 0x30e1: result.push_back(0xff92); return 1;
        case 0x30e2: result.push_back(0xff93); return 1;
        case 0x30e4: result.push_back(0xff94); return 1;
        case 0x30e6: result.push_back(0xff95); return 1;
        case 0x30e8: result.push_back(0xff96); return 1;
        case 0x30e9: result.push_back(0xff97); return 1;
        case 0x30ea: result.push_back(0xff98); return 1;
        case 0x30eb: result.push_back(0xff99); return 1;
        case 0x30ec: result.push_back(0xff9a); return 1;
        case 0x30ed: result.push_back(0xff9b); return 1;
        case 0x30ef: result.push_back(0xff9c); return 1;
        case 0x30f3: result.push_back(0xff9d); return 1;
        case 0x30ac: result.push_back(0xff76); result.push_back(0xff9e); return 2;
        case 0x30ae: result.push_back(0xff77); result.push_back(0xff9e); return 2;
        case 0x30b0: result.push_back(0xff78); result.push_back(0xff9e); return 2;
        case 0x30b2: result.push_back(0xff79); result.push_back(0xff9e); return 2;
        case 0x30b4: result.push_back(0xff7a); result.push_back(0xff9e); return 2;
        case 0x30b6: result.push_back(0xff7b); result.push_back(0xff9e); return 2;
        case 0x30b8: result.push_back(0xff7c); result.push_back(0xff9e); return 2;
        case 0x30ba: result.push_back(0xff7d); result.push_back(0xff9e); return 2;
        case 0x30bc: result.push_back(0xff7e); result.push_back(0xff9e); return 2;
        case 0x30be: result.push_back(0xff7f); result.push_back(0xff9e); return 2;
        case 0x30c0: result.push_back(0xff80); result.push_back(0xff9e); return 2;
        case 0x30c2: result.push_back(0xff81); result.push_back(0xff9e); return 2;
        case 0x30c5: result.push_back(0xff82); result.push_back(0xff9e); return 2;
        case 0x30c7: result.push_back(0xff83); result.push_back(0xff9e); return 2;
        case 0x30c9: result.push_back(0xff84); result.push_back(0xff9e); return 2;
        case 0x30d0: result.push_back(0xff8a); result.push_back(0xff9e); return 2;
        case 0x30d3: result.push_back(0xff8b); result.push_back(0xff9e); return 2;
        case 0x30d6: result.push_back(0xff8c); result.push_back(0xff9e); return 2;
        case 0x30d9: result.push_back(0xff8d); result.push_back(0xff9e); return 2;
        case 0x30dc: result.push_back(0xff8e); result.push_back(0xff9e); return 2;
        case 0x30d1: result.push_back(0xff8a); result.push_back(0xff9f); return 2;
        case 0x30d4: result.push_back(0xff8b); result.push_back(0xff9f); return 2;
        case 0x30d7: result.push_back(0xff8c); result.push_back(0xff9f); return 2;
        case 0x30da: result.push_back(0xff8d); result.push_back(0xff9f); return 2;
        case 0x30dd: result.push_back(0xff8e); result.push_back(0xff9f); return 2;
        default: result.push_back(ch); return 1;
        }
    }

    MString convert_katakana_to_hankaku(const MString& mstr) {
        MString result;
        for (auto mc : mstr) {
            katakana_to_hankaku(result, mc);
        }
        return result;
    }

    // -------------------------------------------------------------------
    // 状態テンプレートクラス
    class HankakuKatakanaOneShotState : public State {
        DECLARE_CLASS_LOGGER;

    public:
        // コンストラクタ
        HankakuKatakanaOneShotState(HankakuKatakanaOneShotNode* pN) {
            LOG_INFO(_T("CALLED"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~HankakuKatakanaOneShotState() { };

#define NAME_PTR (Name.c_str())
#define MY_NODE ((HankakuKatakanaOneShotNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("ENTER"));

            auto outStr = OUTPUT_STACK->GetLastKatakanaStr<MString>();
            if (!outStr.empty()) {
                // 半角カタカナに変換して置換する
                STATE_COMMON->SetOutString(convert_katakana_to_hankaku(outStr), outStr.size());
            }

            // チェイン不要
            LOG_DEBUG(_T("LEAVE: NO CHAIN"));

            return false;
        }

    };
    DEFINE_CLASS_LOGGER(HankakuKatakanaOneShotState);

} // namespace

// -------------------------------------------------------------------
// HankakuKatakanaOneShotNode - ノードのテンプレート
DEFINE_CLASS_LOGGER(HankakuKatakanaOneShotNode);

// コンストラクタ
HankakuKatakanaOneShotNode::HankakuKatakanaOneShotNode() {
    LOG_INFO(_T("CALLED: constructor"));
}

// デストラクタ
HankakuKatakanaOneShotNode::~HankakuKatakanaOneShotNode() {
    LOG_INFO(_T("CALLED: destructor"));
}

// 当ノードを処理する State インスタンスを作成する
State* HankakuKatakanaOneShotNode::CreateState() {
    return new HankakuKatakanaOneShotState(this);
}

// -------------------------------------------------------------------
// HankakuKatakanaOneShotNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(HankakuKatakanaOneShotNodeBuilder);

Node* HankakuKatakanaOneShotNodeBuilder::CreateNode() {
    return new HankakuKatakanaOneShotNode();
}

