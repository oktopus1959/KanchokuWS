#include "Logger.h"

#include "RomanToKatakana.h"

#if 1
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

namespace RomanToKatakana {
    DEFINE_NAMESPACE_LOGGER(RomanToKatakana);

    std::map<wstring, wstring> romanKatakanaTbl = {
            {_T("-"), _T("ー")},
            {_T("A"), _T("ア")},
            {_T("I"), _T("イ")},
            {_T("U"), _T("ウ")},
            {_T("E"), _T("エ")},
            {_T("O"), _T("オ")},
            {_T("XA"), _T("ァ")},
            {_T("XI"), _T("ィ")},
            {_T("XU"), _T("ゥ")},
            {_T("XE"), _T("ェ")},
            {_T("XO"), _T("ォ")},
            {_T("XY$"), _T("クシー")},
            {_T("KA"), _T("カ")},
            {_T("KY$"), _T("キー")},
            {_T("KY"), _T("カイ")},
            {_T("KI"), _T("キ")},
            {_T("KU"), _T("ク")},
            {_T("KE"), _T("ケ")},
            {_T("KO"), _T("コ")},
            {_T("CA"), _T("カ")},
            {_T("CY$"), _T("シー")},
            {_T("CY"), _T("サイ")},
            {_T("CI"), _T("シ")},
            {_T("CU"), _T("ク")},
            {_T("CE"), _T("セ")},
            {_T("CO"), _T("コ")},
            {_T("QA"), _T("カ")},
            {_T("QI"), _T("キ")},
            {_T("QU"), _T("ク")},
            {_T("QE"), _T("ケ")},
            {_T("QO"), _T("コ")},
            {_T("GA"), _T("ガ")},
            {_T("GI"), _T("ギ")},
            {_T("GU"), _T("グ")},
            {_T("GE"), _T("ゲ")},
            {_T("GO"), _T("ゴ")},
            {_T("SA"), _T("サ")},
            {_T("SY$"), _T("シー")},
            {_T("SY"), _T("シ")},
            {_T("SI"), _T("シ")},
            {_T("SHI"), _T("シ")},
            {_T("SU"), _T("ス")},
            {_T("SE"), _T("セ")},
            {_T("SO"), _T("ソ")},
            {_T("ZA"), _T("ザ")},
            {_T("ZY$"), _T("ジー")},
            {_T("ZY"), _T("ザイ")},
            {_T("ZI"), _T("ジ")},
            {_T("JI"), _T("ジ")},
            {_T("GY$"), _T("ジー")},
            {_T("GY"), _T("ジ")},
            {_T("JY$"), _T("ジー")},
            {_T("JY"), _T("ジャイ")},
            {_T("ZU"), _T("ズ")},
            {_T("ZE"), _T("ゼ")},
            {_T("ZO"), _T("ゾ")},
            {_T("TA"), _T("タ")},
            {_T("TI"), _T("チ")},
            {_T("TY"), _T("ティ")},
            {_T("CHI"), _T("チ")},
            {_T("TS"), _T("ッツ")},
            {_T("TZ"), _T("ッツ")},
            {_T("TU"), _T("ツ")},
            {_T("XT"), _T("ッ")},
            {_T("XTU"), _T("ッ")},
            {_T("TSA"), _T("ツァ")},
            {_T("THA"), _T("ツァ")},
            {_T("TSI"), _T("ツィ")},
            {_T("THI"), _T("ティ")},
            {_T("TSU"), _T("ツ")},
            {_T("XTSU"), _T("ッ")},
            {_T("THU"), _T("テュ")},
            {_T("TSE"), _T("ツェ")},
            {_T("THE"), _T("ツェ")},
            {_T("TSO"), _T("ツォ")},
            {_T("TZO"), _T("ツォ")},
            {_T("THO"), _T("ツォ")},
            {_T("TE"), _T("テ")},
            {_T("TO"), _T("ト")},
            {_T("DA"), _T("ダ")},
            {_T("DI"), _T("ヂ")},
            {_T("DHI"), _T("ディ")},
            {_T("DYNA"), _T("ダイナ")},
            {_T("DY"), _T("ディ")},
            {_T("DU"), _T("ヅ")},
            {_T("DHU"), _T("デュ")},
            {_T("DE"), _T("デ")},
            {_T("DO"), _T("ド")},
            {_T("DHO"), _T("ドゥ")},
            {_T("NA"), _T("ナ")},
            {_T("NY$"), _T("ニィ")},
            {_T("NY"), _T("ナイ")},
            {_T("NI"), _T("ニ")},
            {_T("NU"), _T("ヌ")},
            {_T("NE"), _T("ネ")},
            {_T("NO"), _T("ノ")},
            //{_T("NN"), _T("ン")},
            {_T("HA"), _T("ハ")},
            {_T("HY$"), _T("ヒー")},
            {_T("HY"), _T("ハイ")},
            {_T("HI"), _T("ヒ")},
            {_T("HU"), _T("フ")},
            {_T("HE"), _T("ヘ")},
            {_T("HO"), _T("ホ")},
            {_T("FA"), _T("ファ")},
            {_T("FY$"), _T("フィ")},
            {_T("FY"), _T("ファイ")},
            {_T("FI"), _T("フィ")},
            {_T("FU"), _T("フ")},
            {_T("FE"), _T("フェ")},
            {_T("FO"), _T("フォ")},
            {_T("PHA"), _T("ファ")},
            {_T("PHI"), _T("フィ")},
            {_T("PHU"), _T("フ")},
            {_T("PHE"), _T("フェ")},
            {_T("PHO"), _T("フォ")},
            {_T("BA"), _T("バ")},
            {_T("BI"), _T("ビ")},
            {_T("BY$"), _T("ビー")},
            {_T("BY"), _T("バイ")},
            {_T("BU"), _T("ブ")},
            {_T("BE"), _T("ベ")},
            {_T("BO"), _T("ボ")},
            {_T("PA"), _T("パ")},
            {_T("PI"), _T("ピ")},
            {_T("PY$"), _T("ピー")},
            {_T("PY"), _T("パイ")},
            {_T("PU"), _T("プ")},
            {_T("PE"), _T("ペ")},
            {_T("PO"), _T("ポ")},
            {_T("MA"), _T("マ")},
            {_T("MY$"), _T("ミー")},
            {_T("MY"), _T("マイ")},
            {_T("MI"), _T("ミ")},
            {_T("MU"), _T("ム")},
            {_T("ME"), _T("メ")},
            {_T("MO"), _T("モ")},
            {_T("YA"), _T("ヤ")},
            {_T("YI"), _T("イ")},
            {_T("YU"), _T("ユ")},
            {_T("YE"), _T("エ")},
            {_T("YO"), _T("ヨ")},
            {_T("RA"), _T("ラ")},
            {_T("RY$"), _T("リー")},
            {_T("RY"), _T("ライ")},
            {_T("RI"), _T("リ")},
            {_T("RU"), _T("ル")},
            {_T("RE"), _T("レ")},
            {_T("RO"), _T("ロ")},
            {_T("LA"), _T("ラ")},
            {_T("LY$"), _T("リー")},
            {_T("LY"), _T("リ")},
            {_T("LI"), _T("リ")},
            {_T("LU"), _T("ル")},
            {_T("LE"), _T("レ")},
            {_T("LO"), _T("ロ")},
            {_T("WA"), _T("ワ")},
            {_T("WI"), _T("ヰ")},
            {_T("WU"), _T("ウ")},
            {_T("WE"), _T("ヱ")},
            {_T("WO"), _T("ヲ")},
            {_T("WHA"), _T("ウァ")},
            {_T("WHI"), _T("ウィ")},
            {_T("WHE"), _T("ウェ")},
            {_T("WHO"), _T("ウォ")},
            {_T("KYA"), _T("キャ")},
            {_T("KYU"), _T("キュ")},
            {_T("KYE"), _T("キェ")},
            {_T("KYO"), _T("キョ")},
            {_T("GYA"), _T("ギャ")},
            {_T("GYU"), _T("ギュ")},
            {_T("GYE"), _T("ギェ")},
            {_T("GYO"), _T("ギョ")},
            {_T("SYA"), _T("シャ")},
            {_T("SYU"), _T("シュ")},
            {_T("SYE"), _T("シェ")},
            {_T("SYO"), _T("ショ")},
            {_T("SHA"), _T("シャ")},
            {_T("SHU"), _T("シュ")},
            {_T("SHE"), _T("シェ")},
            {_T("SHO"), _T("ショ")},
            {_T("ZYA"), _T("ジャ")},
            {_T("ZYU"), _T("ジュ")},
            {_T("ZYE"), _T("ジェ")},
            {_T("ZYO"), _T("ジョ")},
            {_T("JYA"), _T("ジャ")},
            {_T("JYU"), _T("ジュ")},
            {_T("JYE"), _T("ジェ")},
            {_T("JYO"), _T("ジョ")},
            {_T("JA"), _T("ジャ")},
            {_T("JU"), _T("ジュ")},
            {_T("JE"), _T("ジェ")},
            {_T("JO"), _T("ジョ")},
            {_T("TYA"), _T("チャ")},
            {_T("TYU"), _T("チュ")},
            {_T("TYE"), _T("チェ")},
            {_T("TYO"), _T("チョ")},
            {_T("CHA"), _T("チャ")},
            {_T("CHU"), _T("チュ")},
            {_T("CHE"), _T("チェ")},
            {_T("CHO"), _T("チョ")},
            {_T("NYA"), _T("ニャ")},
            {_T("NYU"), _T("ニュ")},
            {_T("NYE"), _T("ニェ")},
            {_T("NYO"), _T("ニョ")},
            {_T("HYA"), _T("ヒャ")},
            {_T("HYU"), _T("ヒュ")},
            {_T("HYE"), _T("ヒェ")},
            {_T("HYO"), _T("ヒョ")},
            {_T("BYA"), _T("ビャ")},
            { _T("BYU"), _T("ビュ") },
            { _T("BYE"), _T("ビェ") },
            { _T("BYO"), _T("ビョ") },
            { _T("PYA"), _T("ピャ") },
            { _T("PYU"), _T("ピュ") },
            { _T("PYE"), _T("ピェ") },
            { _T("PYO"), _T("ピョ") },
            { _T("MYA"), _T("ミャ") },
            { _T("MYU"), _T("ミュ") },
            { _T("MYE"), _T("ミェ") },
            { _T("MYO"), _T("ミョ") },
            { _T("RYA"), _T("リャ") },
            { _T("RYU"), _T("リュ") },
            { _T("RYE"), _T("リェ") },
            { _T("RYO"), _T("リョ") },
            { _T("VA"), _T("ヴァ") },
            { _T("VY$"), _T("ヴィ") },
            { _T("VY"), _T("ヴァイ") },
            { _T("VI"), _T("ヴィ") },
            { _T("VU"), _T("ヴ") },
            { _T("VE"), _T("ヴェ") },
            { _T("VO"), _T("ヴォ") },
    };

    bool isVowel(wchar_t ch) {
        return ch == 'A' || ch == 'I' || ch == 'U' || ch == 'E' || ch == 'O'; // || ch == 'N' || ch == 'M' || ch == 'H' || ch == '-';
    }

    // ローマ字をカタカタナに変換する
    MString convertRomanToKatakana(const MString& str) {
        LOG_DEBUGH(_T("ENTER: str=%s"), MAKE_WPTR(str));
        MString result;
        if (!str.empty()) {
            wstring ws = utils::toUpperFromMS(str);
            ws.append(_T("$"));
            size_t pos = 0;
            while (pos < ws.size()) {
                bool found = false;
                for (size_t n = 4; n >= 1; --n) {
                    if (n <= ws.size() - pos) {
                        auto iter = romanKatakanaTbl.find(ws.substr(pos, n));
                        if (iter != romanKatakanaTbl.end()) {
                            result.append(to_mstr(iter->second));
                            pos += n;
                            found = true;
                            break;
                        }
                    }
                }
                if (found) {
                    if (pos == ws.size() && ws[pos - 1] == 'Y' && result.back() != _T("ィ")[0]) result.append(MSTR_CHOON);
                } else {
                    wchar_t prevChar = pos > 0 ? ws[pos - 1] : 0;
                    wchar_t nextChar = pos + 1 < ws.size() ? ws[pos + 1] : 0;
                    bool prevVowel = isVowel(prevChar);
                    bool tsu = !result.empty() && result.back() == _T("ッ")[0];
                    bool tailConsonant = prevVowel && !tsu && !is_alphabet(nextChar);
                    const wchar_t* wp = _T("ッ");
#define HANDLE_CONSONANT(c, s, t) case c: if (tailConsonant) { wp = _T(s); } else if (c != nextChar) { wp = _T(t); }; break
                    switch (ws[pos]) {
                    HANDLE_CONSONANT('C', "ック", "ク");
                    HANDLE_CONSONANT('K', "ック", "ク");
                    HANDLE_CONSONANT('Q', "ック", "ク");
                    HANDLE_CONSONANT('X', "ックス", "クス");
                    HANDLE_CONSONANT('G', "グ", "グ");
                    HANDLE_CONSONANT('S', "ス", "ス");
                    HANDLE_CONSONANT('Z', "ズ", "ズ");
                    HANDLE_CONSONANT('J', "ジ", "ジ");
                    HANDLE_CONSONANT('T', "ット", "ト");
                    HANDLE_CONSONANT('D', "ッド", "ド");
                    HANDLE_CONSONANT('F', "フ", "フ");
                    HANDLE_CONSONANT('B', "ブ", "ブ");
                    HANDLE_CONSONANT('V', "ヴ", "ヴ");
                    HANDLE_CONSONANT('P', "ップ", "プ");
                    HANDLE_CONSONANT('Y', "イ", "イ");
                    HANDLE_CONSONANT('R', "ル", "ル");
                    HANDLE_CONSONANT('L', "ル", "ル");
                    HANDLE_CONSONANT('W', "ウ", "ウ");
                    case 'H':
                        if (nextChar != 'H') {
                            wp = _T("ー");
                        }
                        break;
                    case 'N': wp = _T("ン"); break;
                    case 'M':
                        if (nextChar == 'B' && (pos + 2 >= ws.size() || !isVowel(ws[pos + 2]))) {
                            // Lambda
                            wp = _T("ム");
                            pos += 1;
                        } else {
                            wp = nextChar == 'P' || nextChar == 'B' || nextChar == 'M' ? _T("ン") : _T("ム");
                        }
                        break;
                    default: wp = 0; break;
                    }
                    if (wp) result.append(to_mstr(wp));
                    pos += 1;
                }
            }
        }
        LOG_DEBUGH(_T("LEAVE: result=%s"), MAKE_WPTR(result));
        return result;
    }
}

