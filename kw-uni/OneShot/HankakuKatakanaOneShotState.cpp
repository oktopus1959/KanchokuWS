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

#include "HankakuKatakanaOneShot.h"

namespace {

    size_t katakana_to_hankaku(MString& result, mchar_t ch) {
        switch (ch) {
        case 0x30fb: result.push_back(0xff65); return 1;        // ・
        case 0x30f2: result.push_back(0xff66); return 1;        // ヲ
        case 0x30a1: result.push_back(0xff67); return 1;        // ァ
        case 0x30a3: result.push_back(0xff68); return 1;        // ィ
        case 0x30a5: result.push_back(0xff69); return 1;        // ゥ
        case 0x30a7: result.push_back(0xff6a); return 1;        // ェ
        case 0x30a9: result.push_back(0xff6b); return 1;        // ォ
        case 0x30e3: result.push_back(0xff6c); return 1;        // ヤ
        case 0x30e5: result.push_back(0xff6d); return 1;        // ュ
        case 0x30e7: result.push_back(0xff6e); return 1;        // ョ
        case 0x30c3: result.push_back(0xff6f); return 1;        // ッ
        case 0x30fc: result.push_back(0xff70); return 1;        // ー
        case 0x30a2: result.push_back(0xff71); return 1;        // ア
        case 0x30a4: result.push_back(0xff72); return 1;        // イ
        case 0x30a6: result.push_back(0xff73); return 1;        // ウ
        case 0x30a8: result.push_back(0xff74); return 1;        // エ
        case 0x30aa: result.push_back(0xff75); return 1;        // オ
        case 0x30ab: result.push_back(0xff76); return 1;        // カ
        case 0x30ad: result.push_back(0xff77); return 1;        // キ
        case 0x30af: result.push_back(0xff78); return 1;        // ク
        case 0x30b1: result.push_back(0xff79); return 1;        // ケ
        case 0x30b3: result.push_back(0xff7a); return 1;        // コ
        case 0x30b5: result.push_back(0xff7b); return 1;        // サ
        case 0x30b7: result.push_back(0xff7c); return 1;        // シ
        case 0x30b9: result.push_back(0xff7d); return 1;        // ス
        case 0x30bb: result.push_back(0xff7e); return 1;        // セ
        case 0x30bd: result.push_back(0xff7f); return 1;        // ソ
        case 0x30bf: result.push_back(0xff80); return 1;        // タ
        case 0x30c1: result.push_back(0xff81); return 1;        // チ
        case 0x30c4: result.push_back(0xff82); return 1;        // ツ
        case 0x30c6: result.push_back(0xff83); return 1;        // テ
        case 0x30c8: result.push_back(0xff84); return 1;        // ト
        case 0x30ca: result.push_back(0xff85); return 1;        // ナ
        case 0x30cb: result.push_back(0xff86); return 1;        // ニ
        case 0x30cc: result.push_back(0xff87); return 1;        // ヌ
        case 0x30cd: result.push_back(0xff88); return 1;        // ネ
        case 0x30ce: result.push_back(0xff89); return 1;        // ノ
        case 0x30cf: result.push_back(0xff8a); return 1;        // ハ
        case 0x30d2: result.push_back(0xff8b); return 1;        // ヒ
        case 0x30d5: result.push_back(0xff8c); return 1;        // フ
        case 0x30d8: result.push_back(0xff8d); return 1;        // へ
        case 0x30db: result.push_back(0xff8e); return 1;        // ホ
        case 0x30de: result.push_back(0xff8f); return 1;        // マ
        case 0x30df: result.push_back(0xff90); return 1;        // ミ
        case 0x30e0: result.push_back(0xff91); return 1;        // ム
        case 0x30e1: result.push_back(0xff92); return 1;        // メ
        case 0x30e2: result.push_back(0xff93); return 1;        // モ
        case 0x30e4: result.push_back(0xff94); return 1;        // ヤ
        case 0x30e6: result.push_back(0xff95); return 1;        // ユ
        case 0x30e8: result.push_back(0xff96); return 1;        // ヨ
        case 0x30e9: result.push_back(0xff97); return 1;        // ラ
        case 0x30ea: result.push_back(0xff98); return 1;        // リ
        case 0x30eb: result.push_back(0xff99); return 1;        // ル
        case 0x30ec: result.push_back(0xff9a); return 1;        // レ
        case 0x30ed: result.push_back(0xff9b); return 1;        // ロ
        case 0x30ef: result.push_back(0xff9c); return 1;        // ワ
        case 0x30f3: result.push_back(0xff9d); return 1;        // ン
        case 0x30ac: result.push_back(0xff76); result.push_back(0xff9e); return 2;      // ガ
        case 0x30ae: result.push_back(0xff77); result.push_back(0xff9e); return 2;      // ギ
        case 0x30b0: result.push_back(0xff78); result.push_back(0xff9e); return 2;      // グ
        case 0x30b2: result.push_back(0xff79); result.push_back(0xff9e); return 2;      // ゲ
        case 0x30b4: result.push_back(0xff7a); result.push_back(0xff9e); return 2;      // ゴ
        case 0x30b6: result.push_back(0xff7b); result.push_back(0xff9e); return 2;      // ザ
        case 0x30b8: result.push_back(0xff7c); result.push_back(0xff9e); return 2;      // ジ
        case 0x30ba: result.push_back(0xff7d); result.push_back(0xff9e); return 2;      // ズ
        case 0x30bc: result.push_back(0xff7e); result.push_back(0xff9e); return 2;      // ゼ
        case 0x30be: result.push_back(0xff7f); result.push_back(0xff9e); return 2;      // ゾ
        case 0x30c0: result.push_back(0xff80); result.push_back(0xff9e); return 2;      // ダ
        case 0x30c2: result.push_back(0xff81); result.push_back(0xff9e); return 2;      // ヂ
        case 0x30c5: result.push_back(0xff82); result.push_back(0xff9e); return 2;      // ヅ
        case 0x30c7: result.push_back(0xff83); result.push_back(0xff9e); return 2;      // デ
        case 0x30c9: result.push_back(0xff84); result.push_back(0xff9e); return 2;      // ド
        case 0x30d0: result.push_back(0xff8a); result.push_back(0xff9e); return 2;      // バ
        case 0x30d3: result.push_back(0xff8b); result.push_back(0xff9e); return 2;      // ビ
        case 0x30d6: result.push_back(0xff8c); result.push_back(0xff9e); return 2;      // ブ
        case 0x30d9: result.push_back(0xff8d); result.push_back(0xff9e); return 2;      // ベ
        case 0x30dc: result.push_back(0xff8e); result.push_back(0xff9e); return 2;      // ボ
        case 0x30d1: result.push_back(0xff8a); result.push_back(0xff9f); return 2;      // パ
        case 0x30d4: result.push_back(0xff8b); result.push_back(0xff9f); return 2;      // ピ
        case 0x30d7: result.push_back(0xff8c); result.push_back(0xff9f); return 2;      // プ
        case 0x30da: result.push_back(0xff8d); result.push_back(0xff9f); return 2;      // ペ
        case 0x30dd: result.push_back(0xff8e); result.push_back(0xff9f); return 2;      // ポ
        default: result.push_back(ch); return 1;
        }
    }

    size_t dakuten_handakuten(MString& result, mchar_t zch, mchar_t hch)
    {
        if (hch == 0xff9e) {
            // 濁点
            switch (zch) {
            case 0x30ab: result.push_back(0x30ac); return 2;        // ガ
            case 0x30ad: result.push_back(0x30ae); return 2;        // ギ
            case 0x30af: result.push_back(0x30b0); return 2;        // グ
            case 0x30b1: result.push_back(0x30b2); return 2;        // ゲ
            case 0x30b3: result.push_back(0x30b4); return 2;        // ゴ
            case 0x30b5: result.push_back(0x30b6); return 2;        // ザ
            case 0x30b7: result.push_back(0x30b8); return 2;        // ジ
            case 0x30b9: result.push_back(0x30ba); return 2;        // ズ
            case 0x30bb: result.push_back(0x30bc); return 2;        // ゼ
            case 0x30bd: result.push_back(0x30be); return 2;        // ゾ
            case 0x30bf: result.push_back(0x30c0); return 2;        // ダ
            case 0x30c1: result.push_back(0x30c2); return 2;        // ヂ
            case 0x30c4: result.push_back(0x30c5); return 2;        // ヅ
            case 0x30c6: result.push_back(0x30c7); return 2;        // デ
            case 0x30c8: result.push_back(0x30c9); return 2;        // ド
            case 0x30cf: result.push_back(0x30d0); return 2;        // バ
            case 0x30d2: result.push_back(0x30d3); return 2;        // ビ
            case 0x30d5: result.push_back(0x30d6); return 2;        // ブ
            case 0x30d8: result.push_back(0x30d9); return 2;        // ベ
            case 0x30db: result.push_back(0x30dc); return 2;        // ボ
            default: break;
            }
        } else if (hch == 0xff9f) {
            // 半濁点
            switch (zch) {
            case 0x30cf: result.push_back(0x30d1); return 2;        // バ
            case 0x30d2: result.push_back(0x30d4); return 2;        // ビ
            case 0x30d5: result.push_back(0x30d7); return 2;        // ブ
            case 0x30d8: result.push_back(0x30da); return 2;        // ベ
            case 0x30db: result.push_back(0x30dd); return 2;        // ボ
            default: break;
            }
        }
        result.push_back(zch);
        return 1;
    }

    MString convert_zenkaku_to_hankaku(const MString& mstr) {
        MString result;
        for (auto mc : mstr) {
            katakana_to_hankaku(result, mc);
        }
        return result;
    }

    size_t hankaku_to_zenkaku(MString& result, mchar_t ch1, mchar_t ch2) {
        switch (ch1) {
        case 0xff65: return dakuten_handakuten(result, 0x30fb, ch2);        // ・
        case 0xff66: return dakuten_handakuten(result, 0x30f2, ch2);        // ヲ
        case 0xff67: return dakuten_handakuten(result, 0x30a1, ch2);        // ァ
        case 0xff68: return dakuten_handakuten(result, 0x30a3, ch2);        // ィ
        case 0xff69: return dakuten_handakuten(result, 0x30a5, ch2);        // ゥ
        case 0xff6a: return dakuten_handakuten(result, 0x30a7, ch2);        // ェ
        case 0xff6b: return dakuten_handakuten(result, 0x30a9, ch2);        // ォ
        case 0xff6c: return dakuten_handakuten(result, 0x30e3, ch2);        // ヤ
        case 0xff6d: return dakuten_handakuten(result, 0x30e5, ch2);        // ュ
        case 0xff6e: return dakuten_handakuten(result, 0x30e7, ch2);        // ョ
        case 0xff6f: return dakuten_handakuten(result, 0x30c3, ch2);        // ッ
        case 0xff70: return dakuten_handakuten(result, 0x30fc, ch2);        // ー
        case 0xff71: return dakuten_handakuten(result, 0x30a2, ch2);        // ア
        case 0xff72: return dakuten_handakuten(result, 0x30a4, ch2);        // イ
        case 0xff73: return dakuten_handakuten(result, 0x30a6, ch2);        // ウ
        case 0xff74: return dakuten_handakuten(result, 0x30a8, ch2);        // エ
        case 0xff75: return dakuten_handakuten(result, 0x30aa, ch2);        // オ
        case 0xff76: return dakuten_handakuten(result, 0x30ab, ch2);        // カ
        case 0xff77: return dakuten_handakuten(result, 0x30ad, ch2);        // キ
        case 0xff78: return dakuten_handakuten(result, 0x30af, ch2);        // ク
        case 0xff79: return dakuten_handakuten(result, 0x30b1, ch2);        // ケ
        case 0xff7a: return dakuten_handakuten(result, 0x30b3, ch2);        // コ
        case 0xff7b: return dakuten_handakuten(result, 0x30b5, ch2);        // サ
        case 0xff7c: return dakuten_handakuten(result, 0x30b7, ch2);        // シ
        case 0xff7d: return dakuten_handakuten(result, 0x30b9, ch2);        // ス
        case 0xff7e: return dakuten_handakuten(result, 0x30bb, ch2);        // セ
        case 0xff7f: return dakuten_handakuten(result, 0x30bd, ch2);        // ソ
        case 0xff80: return dakuten_handakuten(result, 0x30bf, ch2);        // タ
        case 0xff81: return dakuten_handakuten(result, 0x30c1, ch2);        // チ
        case 0xff82: return dakuten_handakuten(result, 0x30c4, ch2);        // ツ
        case 0xff83: return dakuten_handakuten(result, 0x30c6, ch2);        // テ
        case 0xff84: return dakuten_handakuten(result, 0x30c8, ch2);        // ト
        case 0xff85: return dakuten_handakuten(result, 0x30ca, ch2);        // ナ
        case 0xff86: return dakuten_handakuten(result, 0x30cb, ch2);        // ニ
        case 0xff87: return dakuten_handakuten(result, 0x30cc, ch2);        // ヌ
        case 0xff88: return dakuten_handakuten(result, 0x30cd, ch2);        // ネ
        case 0xff89: return dakuten_handakuten(result, 0x30ce, ch2);        // ノ
        case 0xff8a: return dakuten_handakuten(result, 0x30cf, ch2);        // ハ
        case 0xff8b: return dakuten_handakuten(result, 0x30d2, ch2);        // ヒ
        case 0xff8c: return dakuten_handakuten(result, 0x30d5, ch2);        // フ
        case 0xff8d: return dakuten_handakuten(result, 0x30d8, ch2);        // へ
        case 0xff8e: return dakuten_handakuten(result, 0x30db, ch2);        // ホ
        case 0xff8f: return dakuten_handakuten(result, 0x30de, ch2);        // マ
        case 0xff90: return dakuten_handakuten(result, 0x30df, ch2);        // ミ
        case 0xff91: return dakuten_handakuten(result, 0x30e0, ch2);        // ム
        case 0xff92: return dakuten_handakuten(result, 0x30e1, ch2);        // メ
        case 0xff93: return dakuten_handakuten(result, 0x30e2, ch2);        // モ
        case 0xff94: return dakuten_handakuten(result, 0x30e4, ch2);        // ヤ
        case 0xff95: return dakuten_handakuten(result, 0x30e6, ch2);        // ユ
        case 0xff96: return dakuten_handakuten(result, 0x30e8, ch2);        // ヨ
        case 0xff97: return dakuten_handakuten(result, 0x30e9, ch2);        // ラ
        case 0xff98: return dakuten_handakuten(result, 0x30ea, ch2);        // リ
        case 0xff99: return dakuten_handakuten(result, 0x30eb, ch2);        // ル
        case 0xff9a: return dakuten_handakuten(result, 0x30ec, ch2);        // レ
        case 0xff9b: return dakuten_handakuten(result, 0x30ed, ch2);        // ロ
        case 0xff9c: return dakuten_handakuten(result, 0x30ef, ch2);        // ワ
        case 0xff9d: return dakuten_handakuten(result, 0x30f3, ch2);        // ン
        default: result.push_back(ch1); return 1;
        }
    }

    MString convert_hankaku_to_zenkaku(const MString& mstr) {
        MString result;
        size_t idx = 0;
        while (idx < mstr.size()) {
            auto ch1 = mstr[idx];
            auto ch2 = idx + 1 < mstr.size() ? mstr[idx + 1] : 0;
            idx += hankaku_to_zenkaku(result, ch1, ch2);
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
            LOG_INFOH(_T("CALLED: ctor"));
            Initialize(logger.ClassNameT(), pN);
        }

        ~HankakuKatakanaOneShotState() { };

#define MY_NODE ((HankakuKatakanaOneShotNode*)pNode)

        // 機能状態に対して生成時処理を実行する
        bool DoProcOnCreated() {
            LOG_DEBUG(_T("ENTER"));

            auto outStr = OUTPUT_STACK->GetLastKatakanaStr<MString>();
            if (!outStr.empty()) {
                // 全角を半角カタカナに置換
                STATE_COMMON->SetOutString(convert_zenkaku_to_hankaku(outStr), outStr.size());
                OUTPUT_STACK->setHistBlockerAt(outStr.size());
            } else {
                outStr = OUTPUT_STACK->GetLastHankakuKatakanaStr<MString>();
                if (!outStr.empty()) {
                    // 半角を全角カタカナに置換
                    STATE_COMMON->SetOutString(convert_hankaku_to_zenkaku(outStr), outStr.size());
                }
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
    LOG_INFOH(_T("CALLED"));
    return new HankakuKatakanaOneShotState(this);
}

// -------------------------------------------------------------------
// HankakuKatakanaOneShotNodeBuilder - ノードビルダー

DEFINE_CLASS_LOGGER(HankakuKatakanaOneShotNodeBuilder);

Node* HankakuKatakanaOneShotNodeBuilder::CreateNode() {
    return new HankakuKatakanaOneShotNode();
}

