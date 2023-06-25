#include "Logger.h"
#include "file_utils.h"
#include "path_utils.h"
#include "Settings.h"

#include "RomanToKatakana.h"

#if 0
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_DEBUGH LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

namespace RomanToKatakana {
    DEFINE_NAMESPACE_LOGGER(RomanToKatakana);

    std::vector<String> defaultRomanDef = {
            _T("-\tー"),
            _T("A\tア"),
            _T("I\tイ"),
            _T("U\tウ"),
            _T("E\tエ"),
            _T("O\tオ"),
            _T("XA\tァ"),
            _T("XI\tィ"),
            _T("XU\tゥ"),
            _T("XE\tェ"),
            _T("XO\tォ"),
            _T("XY$\tクシー"),
            _T("KA\tカ"),
            _T("KY$\tキー"),
            _T("KY\tカイ"),
            _T("KI\tキ"),
            _T("KU\tク"),
            _T("KE\tケ"),
            _T("KO\tコ"),
            _T("CA\tカ"),
            _T("CY$\tシー"),
            _T("CY\tサイ"),
            _T("CI\tシ"),
            _T("CU\tク"),
            _T("CE\tセ"),
            _T("CO\tコ"),
            _T("QA\tカ"),
            _T("QI\tキ"),
            _T("QU\tク"),
            _T("QE\tケ"),
            _T("QO\tコ"),
            _T("GA\tガ"),
            _T("GI\tギ"),
            _T("GU\tグ"),
            _T("GE\tゲ"),
            _T("GO\tゴ"),
            _T("SA\tサ"),
            _T("SY$\tシー"),
            _T("SY\tシ"),
            _T("SI\tシ"),
            _T("%@SH$\tッシュ"),
            _T("SHI\tシ"),
            _T("SU\tス"),
            _T("TH$\tス"),
            _T("SE\tセ"),
            _T("SO\tソ"),
            _T("ZA\tザ"),
            _T("ZY$\tジー"),
            _T("ZY\tザイ"),
            _T("ZI\tジ"),
            _T("JI\tジ"),
            _T("GY$\tジー"),
            _T("GY\tジ"),
            _T("JY$\tジー"),
            _T("JY\tジャイ"),
            _T("ZU\tズ"),
            _T("ZE\tゼ"),
            _T("ZO\tゾ"),
            _T("TA\tタ"),
            _T("TI\tチ"),
            _T("TY\tティ"),
            _T("CHI\tチ"),
            _T("%@TS\tッツ"),
            _T("%@TZ\tッツ"),
            _T("%@TCH$\tッチ"),
            _T("TU\tツ"),
            _T("XT\tッ"),
            _T("XTU\tッ"),
            _T("TSA\tツァ"),
            _T("THA\tツァ"),
            _T("TSI\tツィ"),
            _T("THI\tティ"),
            _T("TSU\tツ"),
            _T("XTSU\tッ"),
            _T("THU\tテュ"),
            _T("TSE\tツェ"),
            _T("THE\tツェ"),
            _T("TSO\tツォ"),
            _T("TZO\tツォ"),
            _T("THO\tツォ"),
            _T("TE\tテ"),
            _T("TO\tト"),
            _T("DA\tダ"),
            _T("DI\tヂ"),
            _T("DHI\tディ"),
            _T("DYNA\tダイナ"),
            _T("DY\tディ"),
            _T("DU\tヅ"),
            _T("DHU\tデュ"),
            _T("DE\tデ"),
            _T("DO\tド"),
            _T("DHO\tドゥ"),
            _T("NA\tナ"),
            _T("NY$\tニィ"),
            _T("NY\tナイ"),
            _T("NI\tニ"),
            _T("NU\tヌ"),
            _T("NE\tネ"),
            _T("NO\tノ"),
            //_T("NN\tン"),
            _T("HA\tハ"),
            _T("HY$\tヒー"),
            _T("HY\tハイ"),
            _T("HI\tヒ"),
            _T("HU\tフ"),
            _T("HE\tヘ"),
            _T("HO\tホ"),
            _T("FA\tファ"),
            _T("FY$\tフィ"),
            _T("FY\tファイ"),
            _T("FI\tフィ"),
            _T("FU\tフ"),
            _T("FE\tフェ"),
            _T("FO\tフォ"),
            _T("PHA\tファ"),
            _T("PHI\tフィ"),
            _T("PHU\tフ"),
            _T("PHE\tフェ"),
            _T("PHO\tフォ"),
            _T("BA\tバ"),
            _T("BI\tビ"),
            _T("BY$\tビー"),
            _T("BY\tバイ"),
            _T("BU\tブ"),
            _T("BE\tベ"),
            _T("BO\tボ"),
            _T("PA\tパ"),
            _T("PI\tピ"),
            _T("PY$\tピー"),
            _T("PY\tパイ"),
            _T("PU\tプ"),
            _T("PE\tペ"),
            _T("PO\tポ"),
            _T("MA\tマ"),
            _T("MY$\tミー"),
            _T("MY\tマイ"),
            _T("MI\tミ"),
            _T("MU\tム"),
            _T("ME\tメ"),
            _T("MO\tモ"),
            _T("YA\tヤ"),
            _T("YI\tイ"),
            _T("YU\tユ"),
            _T("YE\tエ"),
            _T("YO\tヨ"),
            _T("RA\tラ"),
            _T("RY$\tリー"),
            _T("RY\tライ"),
            _T("RI\tリ"),
            _T("RU\tル"),
            _T("RE\tレ"),
            _T("RO\tロ"),
            _T("LA\tラ"),
            _T("LY$\tリー"),
            _T("LY\tリ"),
            _T("LI\tリ"),
            _T("LU\tル"),
            _T("LE\tレ"),
            _T("LO\tロ"),
            _T("WA\tワ"),
            _T("WI\tウィ"),
            _T("WU\tウ"),
            _T("WE\tウェ"),
            _T("WO\tヲ"),
            _T("WHA\tウァ"),
            _T("WHI\tウィ"),
            _T("WHE\tウェ"),
            _T("WHU\tウ"),
            _T("WHO\tウォ"),
            _T("WYI\tヰ"),
            _T("WYE\tヱ"),
            _T("KYA\tキャ"),
            _T("KYU\tキュ"),
            _T("KYE\tキェ"),
            _T("KYO\tキョ"),
            _T("GYA\tギャ"),
            _T("GYU\tギュ"),
            _T("GYE\tギェ"),
            _T("GYO\tギョ"),
            _T("SYA\tシャ"),
            _T("SYU\tシュ"),
            _T("SYE\tシェ"),
            _T("SYO\tショ"),
            _T("SHA\tシャ"),
            _T("SHU\tシュ"),
            _T("SHE\tシェ"),
            _T("SHO\tショ"),
            _T("ZYA\tジャ"),
            _T("ZYU\tジュ"),
            _T("ZYE\tジェ"),
            _T("ZYO\tジョ"),
            _T("JYA\tジャ"),
            _T("JYU\tジュ"),
            _T("JYE\tジェ"),
            _T("JYO\tジョ"),
            _T("JA\tジャ"),
            _T("JU\tジュ"),
            _T("JE\tジェ"),
            _T("JO\tジョ"),
            _T("TYA\tチャ"),
            _T("TYU\tチュ"),
            _T("TYE\tチェ"),
            _T("TYO\tチョ"),
            _T("CHA\tチャ"),
            _T("CHU\tチュ"),
            _T("CHE\tチェ"),
            _T("CHO\tチョ"),
            _T("NYA\tニャ"),
            _T("NYU\tニュ"),
            _T("NYE\tニェ"),
            _T("NYO\tニョ"),
            _T("HYA\tヒャ"),
            _T("HYU\tヒュ"),
            _T("HYE\tヒェ"),
            _T("HYO\tヒョ"),
            _T("BYA\tビャ"),
            _T("BYU\tビュ"),
            _T("BYE\tビェ"),
            _T("BYO\tビョ"),
            _T("PYA\tピャ"),
            _T("PYU\tピュ"),
            _T("PYE\tピェ"),
            _T("PYO\tピョ"),
            _T("MYA\tミャ"),
            _T("MYU\tミュ"),
            _T("MYE\tミェ"),
            _T("MYO\tミョ"),
            _T("RYA\tリャ"),
            _T("RYU\tリュ"),
            _T("RYE\tリェ"),
            _T("RYO\tリョ"),
            _T("VA\tヴァ"),
            _T("VY$\tヴィ"),
            _T("VY\tヴァイ"),
            _T("VI\tヴィ"),
            _T("VU\tヴ"),
            _T("VE\tヴェ"),
            _T("VO\tヴォ"),

            _T("%@C$\tック"),
            _T("C\tク"),
            _T("%@K$\tック"),
            _T("K\tク"),
            _T("%@Q$\tック"),
            _T("Q\tク"),
            _T("%@X$\tックス"),
            _T("X\tクス"),
            _T("%@G$\tッグ"),
            _T("G\tグ"),
            _T("S\tス"),
            _T("Z\tズ"),
            _T("J\tジ"),
            _T("%@T$\tット"),
            _T("T\tト"),
            _T("%@D$\tッド"),
            _T("D\tド"),
            _T("F\tフ"),
            _T("B\tブ"),
            _T("V\tヴ"),
            _T("%@P$\tップ"),
            _T("P\tプ"),
            _T("Y\tイ"),
            _T("R\tル"),
            _T("L\tル"),
            _T("W\tウ"),

            _T("H\tー"),
            _T("N[N]\tン"),
            _T("N\tン"),
            _T("MB\tム"),
            _T("M[BMP]\tン"),
            _T("M\tム"),
    };

    bool isVowel(wchar_t ch) {
        switch (ch) {
        case 'A':
        case 'I':
        case 'U':
        case 'E':
        case 'O':
            return true;
        default:
            return false;
        }
    }

    class RomanRewriteInfo {
    private:
        std::vector<String> preChars;      // 対象文字列より前の部分の文字クラス列
        std::vector<String> postChars;     // 対象文字列より後の部分の文字クラス列

        bool matchCharClass(wchar_t ch, StringRef cc) const {
            LOG_DEBUGH(_T("CALLED: ch={}, cc={}"), ch, cc);
            if (cc.empty()) return true;
            wchar_t co = ch == '$' ? '$' : isVowel(ch) ? '@' : '%';
            if (cc[0] == '^') {
                return cc.find(ch, 1) == String::npos && cc.find(co, 1) == String::npos;
            } else {
                return cc.find(ch) != String::npos || cc.find(co) != String::npos;
            }
        }

    public:
        String katakanaStr;                // 変換後のカタカナ

    public:
        RomanRewriteInfo() {
        }

        RomanRewriteInfo(const RomanRewriteInfo& info)
            : preChars(info.preChars), postChars(info.postChars), katakanaStr(info.katakanaStr) {
        }

        const RomanRewriteInfo& operator=(const RomanRewriteInfo& info) {
            preChars = info.preChars;
            postChars = info.postChars;
            katakanaStr = info.katakanaStr;
            return *this;
        }

        void addCharClass(StringRef cc, bool bPre) {
            if (bPre) {
                preChars.push_back(cc);
            } else {
                postChars.push_back(cc);
            }
        }

        void addTailConsonant() {
            if (postChars.empty()) {
                postChars.push_back(_T("%$"));
            }
        }

        bool match(StringRef key, StringRef w, size_t pos) const {
            LOG_DEBUGH(_T("CALLED: key={}, word={}, pos={}, pre={}, post={}"), key, w, pos, utils::join(preChars, _T(":")), utils::join(postChars, _T(":")));
            if (pos < preChars.size()) return false;
            if (pos + key.size() + postChars.size() > w.size()) return false;
            if (!preChars.empty()) {
                LOG_DEBUGH(_T("Check preChars"));
                size_t p = pos - preChars.size();
                for (size_t i = 0; i < preChars.size(); ++i) {
                    if (!matchCharClass(w[p + i], preChars[i])) return false;
                }
            }
            if (!postChars.empty()) {
                LOG_DEBUGH(_T("Check postChars"));
                size_t p = pos + key.size();
                for (size_t i = 0; i < postChars.size(); ++i) {
                    if (!matchCharClass(w[p + i], postChars[i])) return false;
                }
            }
            LOG_DEBUGH(_T("MATCH"));
            return true;
        }

        String toString() {
            return std::format(_T("pre={}, post={}, katakana={}"), utils::join(preChars, _T(":")), utils::join(postChars, _T(":")), katakanaStr);
        }
    };

    std::map<String, std::vector<RomanRewriteInfo>> romanKatakanaTbl;

    std::vector<String> _split(StringRef s) {
        std::vector<String> items;
        size_t p = s.find_first_not_of(_T(" \t"));
        if (p < s.size()) {
            size_t q = s.find_first_of(_T(" \t"), p + 1);
            if (q < s.size()) {
                items.push_back(s.substr(p, q - p));
                p = s.find_first_not_of(_T(" \t"), q + 1);
                if (p < s.size()) {
                    q = s.find_first_of(_T(" \t"), p);
                    items.push_back(s.substr(p, q - p));
                }
            }
        }
        return items;
    }

    void loadRomanDefLines(const std::vector<String>& lines) {
        romanKatakanaTbl.clear();
        for (const auto& line : lines) {
            if (!line.empty()) {
                auto items = _split(utils::toUpper(line));
                if (items.size() == 2 && !items[0].empty() && !items[1].empty() && items[0][0] != '#' ) {
                    RomanRewriteInfo info;
                    info.katakanaStr = items[1];
                    String def = items[0];
                    String key;
                    size_t i = 0;
                    size_t p = 0;
                    while (i < def.size()) {
                        switch (def[i]) {
                        case '[':
                            ++i;
                            p = def.find(']', i);
                            if (p > def.size()) p = def.size();
                            info.addCharClass(def.substr(i, p - i), key.empty());
                            i = p + 1;
                            break;
                        case '%':
                        case '@':
                        case '$':
                            info.addCharClass(def.substr(i, 1), key.empty());
                            ++i;
                            break;
                        default:
                            p = def.find_first_of(_T("%$@[]"), i);
                            key = def.substr(i, p - i);
                            i = p;
                            break;
                        }
                    }
                    if (!key.empty()) {
                        if (!isVowel(key.back())) info.addTailConsonant();
                        LOG_DEBUGH(_T("ADD: line={} {}, key={}, info={}"), items[0], items[1], key, info.toString());
                        auto iter = romanKatakanaTbl.find(key);
                        if (iter == romanKatakanaTbl.end()) {
                            romanKatakanaTbl[key] = std::vector<RomanRewriteInfo>();
                            romanKatakanaTbl[key].push_back(info);
                        } else {
                            iter->second.push_back(info);
                        }
                    }
                }
            }
        }

    }

    // ローマ字定義ファイルを読み込む
    void ReadRomanDefFile(StringRef defFile) {
        LOG_INFOH(_T("open roman def file: {}"), defFile);
        auto path = utils::joinPath(SETTINGS->rootDir, defFile);
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            loadRomanDefLines(reader.getAllLines());
            LOG_INFOH(_T("close roman def: {}"), path);
        } else {
            //// ファイルがなかったらデフォルトを使う
            //LOG_WARN(_T("Can't read roman def file: {}"), path);
            //LOG_WARN(_T("Use default roman defs"));
            //loadRomanDefLines(defaultRomanDef);
            // ファイルがなかったら定義テーブルをクリアする
            //LOG_WARN(_T("Can't read roman def file: {}"), path);
            //LOG_WARN(_T("Use default roman defs"));
            //loadRomanDefLines(defaultRomanDef);
            LOG_WARN(_T("Can't read roman def file: {}"), path);
            LOG_WARN(_T("Clear roman defs"));
            romanKatakanaTbl.clear();
        }
    }

    // ローマ字をカタカタナに変換する
    // 文字クラス: %=子音, @=母音, $=単語末, ^=文字クラスに含まれないもの
    MString convertRomanToKatakana(const MString& str) {
        LOG_DEBUGH(_T("ENTER: str={}"), to_wstr(str));
        MString result;
        if (!str.empty()) {
            if (romanKatakanaTbl.empty()) {
                result = str;
            } else {
                // ws: strをwstringに変換して前後に $ を付ける
                String ws = _T("$");
                ws.append(utils::toUpperFromMS(str));
                ws.append(_T("$"));
                size_t pos = 1;     // 先頭の $ は読み飛ばす
                LOG_DEBUGH(_T("CHECK START: ws={}, pos={}"), ws, pos);
                while (pos < ws.size()) {
                    bool found = false;
                    for (size_t n = 4; !found && n >= 1; --n) {
                        LOG_DEBUGH(_T("CHECK: ws.size={}, pos={}, n={}"), ws.size(), pos, n);
                        if (n <= ws.size() - pos) {
                            String key = ws.substr(pos, n);
                            LOG_DEBUGH(_T("CHECK: key={}"), key);
                            auto iter = romanKatakanaTbl.find(key);
                            if (iter != romanKatakanaTbl.end()) {
                                for (const auto& info : iter->second) {
                                    if (info.match(key, ws, pos)) {
                                        result.append(to_mstr(info.katakanaStr));
                                        pos += n;
                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (!found) {
                        // ローマ字に解釈できない文字があったら、先頭からその文字までを変換せずにそのまま使用する
                        // 例: "AA:Roma" → 「AA:ローマ」になる
                        LOG_DEBUGH(_T("NOT MATCH: pos={}, char={}"), pos, ws[pos]);
                        if (pos > 0 && pos < ws.size() - 1) {
                            result = str.substr(0, pos);
                        }
                        ++pos;
                    }
                }
            }
        }
        LOG_DEBUGH(_T("LEAVE: result={}"), to_wstr(result));
        return result;
    }
}

