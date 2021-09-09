#include "Logger.h"
#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "Constants.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "OutputStack.h"
#include "StrokeHelp.h"
#include "EasyChars.h"
#include "BushuDic.h"
#include "BushuAssoc.h"
#include "BushuAssocDic.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughBushu)

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

namespace {
    // -------------------------------------------------------------------
    // 部首合成辞書の実装クラス
    class BushuDicImpl : public BushuDic {
    private:
        DECLARE_CLASS_LOGGER;

        // 部品のペアを表すクラス
        // 部品は wchar_t で表現しているので、 surrogate pair は部品にできない
        class parts_t {
            uint32_t data;

        public:
            parts_t() : data(0) { }

            parts_t(wchar_t a, wchar_t b) : data((a << 16) + b) { }

            inline bool operator==(uint32_t p) const { return data == p; }

            inline bool operator==(parts_t p) const { return data == p.data; }

            inline bool operator<(parts_t p) const { return data < p.data; }

            inline wchar_t a() const { return data >> 16; }
            inline wchar_t b() const { return data & 0xffff; }

            inline bool empty() const { return data == 0; }
            inline bool notEmpty() const { return data != 0; }
        };

        const wchar_t FirstKanji = FIRST_KANJI;

        // 本体から部品へのマップ
        std::map<mchar_t, parts_t> entries;

        // ストローク可能文字か否かを示すマップ
        std::map<mchar_t, bool> strokableMap;

        // 等価マップ
        std::map<mchar_t, mchar_t> equivMap;

        // 合成辞書
        std::map<parts_t, mchar_t> compMap;

        // 部品から本体集合(キー文字を部品とする本体文字の集合)へのマップ
        std::map<mchar_t, std::set<mchar_t>> partsBodiesMap;

        // 保存時に辞書ファイルに追加されるエントリ
        std::vector<wstring> addedEntries;

        // 戻値用の空集合
        std::set<mchar_t> emptySet;

        // 部首のパーツを返すためのリスト
        std::vector<wchar_t> strList;

        // 文字リスト用(文字リストから除外する文字の集合)
        std::set<wchar_t> firstLevel;

        // 自動部首合成用辞書
        std::map<MString, mchar_t> autoBushuDict;

        // 自動部首合成用辞書が修正された
        bool bAutoDirty = false;

        // 当文字を構成する部品に分解する
        inline parts_t findParts(mchar_t c) {
            auto it = entries.find(c);
            return it == entries.end() ? parts_t() : it->second;
        }
        inline mchar_t findEquiv(mchar_t c) {
            auto it = equivMap.find(c);
            return it == equivMap.end() ? c : it->second;
        }
        inline mchar_t findComp(mchar_t a, mchar_t b) {
            if (a == 0 || b == 0) return 0;
            auto it = compMap.find(parts_t((wchar_t)a, (wchar_t)b));
            return it == compMap.end() ? 0 : it->second;
        }
        inline void addBody(mchar_t ch, mchar_t body) {
            auto it = partsBodiesMap.find(ch);
            if (it == partsBodiesMap.end()) {
                partsBodiesMap[ch] = std::set<mchar_t>();
            }
            partsBodiesMap[ch].insert(body);
        }
        inline const std::set<mchar_t>& getBodies(mchar_t ch) {
            auto it = partsBodiesMap.find(ch);
            return it == partsBodiesMap.end() ? emptySet : it->second;
        }

        //inline UINT32 partsToUint32(wchar_t a, wchar_t b) { return ((UINT32)a << 16) + ((UINT32)b & 0xffff); }
        //inline wchar_t uint32ToA(UINT32 val) { return (wchar_t)(val >> 16); }
        //inline wchar_t uint32ToB(UINT32 val) { return (wchar_t)(val & 0xffff); }

    public:
        BushuDicImpl() {
        }

    public:
        void ReadFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("ENTER: lines num=%d"), lines.size());

            // 作業領域
            std::wregex reComment(_T("^# "));

            // 各マップをクリアしておく(再読み込みのため)
            entries.clear();
            strokableMap.clear();
            equivMap.clear();
            compMap.clear();
            partsBodiesMap.clear();
            addedEntries.clear();

            wstring lastLine;

            for (auto& line : lines) {
                if (line.empty() || std::regex_match(line, reComment)) continue;   // 空行や "# " で始まる行は読み飛ばす

                addBushuEntry(line);

                if (Logger::IsInfoEnabled()) lastLine = line;
            }

            LOG_INFO(_T("LEAVE: last line=%s"), lastLine.c_str());
        }

        // 辞書内容の保存
        void WriteFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED: addedEntries.size()=%d"), addedEntries.size());
            for (const auto& line : addedEntries) {
                writer.writeLine(utils::utf8_encode(line));
            }
        }

        bool IsDirty() {
            return !addedEntries.empty();
        }

        // 自動部首合成用辞書の読み込み
        void ReadAutoDicFile(const std::vector<wstring>& lines) {
            LOG_INFO(_T("ENTER: lines num=%d"), lines.size());

            // 作業領域
            std::wregex reComment(_T("^# "));

            // 各マップをクリアしておく(再読み込みのため)
            autoBushuDict.clear();
            bAutoDirty = false;

            for (auto& line : lines) {
                if (line.empty() || std::regex_match(line, reComment)) continue;   // 空行や "# " で始まる行は読み飛ばす
                addAutoBushuEntry(line);
            }

            LOG_INFO(_T("LEAVE"));
        }

        // 自動合成辞書内容の保存
        void WriteAutoDicFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED: autoBushuDict.size()=%d"), autoBushuDict.size());
            for (const auto& pair : autoBushuDict) {
                writer.writeLine(utils::utf8_encode(to_wstr(MString(1, pair.second) + pair.first)));
            }
        }

        bool IsAutoDirty() {
            return bAutoDirty;
        }

        void MakeStrokableMap() {
            for (auto pair : partsBodiesMap) {
                strokableMap[pair.first] = STROKE_HELP->Find(pair.first);
            }
        }

        // 1エントリの追加 (ここで追加したエントリは、保存時に辞書ファイルに追記される)
        void AddBushuEntry(const wstring& line) {
            addedEntries.push_back(line);
            addBushuEntry(line);
        }

        // 1自動エントリの追加 (ここで追加したエントリは、保存時に辞書ファイルに追記される)
        void AddAutoBushuEntry(const wstring& line) {
            bAutoDirty = true;
            addAutoBushuEntry(line);
        }

        // a と b を組み合わせてできる自動合成文字を探す。
        mchar_t FindAutoComposite(mchar_t ca, mchar_t cb) {
            auto finder = [this](mchar_t a, mchar_t b) {
                MString key;
                key.push_back(a);
                key.push_back(b);
                _LOG_DEBUGH(_T("key=%s"), MAKE_WPTR(key));
                auto iter = autoBushuDict.find(key);
                _LOG_DEBUGH(_T("iter->second=%c"), ((iter != autoBushuDict.end()) ? iter->second : 0x20));
                return (iter != autoBushuDict.end()) ? iter->second : 0;
            };
            mchar_t result = finder(ca, cb);
            if (result != 0) return result;
            return finder(cb, ca);
        }

    private:
        // エントリの追加
        void addBushuEntry(const wstring& line) {
            auto mline = to_mstr(line);
            size_t pos = 0;
            mchar_t c = pos < mline.size() ? mline[pos++] : 0;
            wchar_t a = (wchar_t)(pos < mline.size() ? mline[pos++] : 0);
            wchar_t b = (wchar_t)(pos < mline.size() ? mline[pos++] : 0);

            if (Logger::IsDebugEnabled() && is_paired_mchar(c)) LOG_DEBUG(_T("Surrogate pair=%s"), line.c_str());

            if (c != 0 && a != 0) {
                addBody(a, c);
                if (b == wchar_t('\r') || b == wchar_t('\n') || b == 0) {
                    // "CA" → A ≡ C (等価文字)
                    equivMap[c] = a;
                    equivMap[a] = c;
                } else {
                    // CAB → C = A + B (部品)
                    parts_t comp = parts_t(a, b);
                    // 分解用のエントリは、同じ本体文字については先勝ち登録とする
                    // 例: 「爆火暴」「爆バク」という2つのエントリがあったら、先に記述されている「爆火暴」を優先する
                    if (entries.find(c) == entries.end()) entries[c] = comp;
                    // 合成用エントリはすべて登録(もし同じ組み合わせがあれば、後勝ちになる)
                    compMap[comp] = c;
                    addBody(b, c);
                }
            }
        }

        // 自動エントリの追加
        void addAutoBushuEntry(const wstring& line) {
            auto mline = to_mstr(line);
            if (mline.size() >= 3) {
                autoBushuDict[mline.substr(1)] = mline[0];
            }
        }

        void addAutoBushuEntry(mchar_t a, mchar_t b, mchar_t c) {
            if (a != 0 && b != 0 && c != 0) {
                bAutoDirty = true;
                MString key(2, 0);
                key[0] = a;
                key[1] = b;
                _LOG_DEBUGH(_T("key=%s, val=%c"), MAKE_WPTR(key), c);
                autoBushuDict[key] = c;
            }
        }

        // kanji を 2 つの部品に分解する(頻度の低い方を優先する)
        void decompose(mchar_t kanji, wchar_t& c1, wchar_t& c2) {
            auto parts = findParts(kanji);
            if (parts.notEmpty()) {
                wchar_t x = parts.a();
                wchar_t y = parts.b();
                // 頻度の低い方を優先する
                if (getBodies(x).size() < getBodies(y).size()) {
                    c1 = x;
                    c2 = y;
                } else {
                    c1 = y;
                    c2 = x;
                }
            } else {
                // not found
                c1 = c2 = 0;
            }
        }

        // 連想辞書に登録する
        void setAssocTarget(mchar_t m, mchar_t tgt)
        {
            if (BUSHU_ASSOC_DIC) {
                BUSHU_ASSOC_DIC->SelectTarget(m, tgt);
            }
        }

        // 連想辞書に登録する
        void setAssocTarget(mchar_t ca, mchar_t cb, mchar_t tgt)
        {
            setAssocTarget(ca, tgt);
            setAssocTarget(cb, tgt);
        }

#define _LOG_INFOH(...) if (SETTINGS->bushuDicLogEnabled || SETTINGS->debughBushu) logger.InfoH(utils::format(__VA_ARGS__).c_str(), __func__, __FILE__, __LINE__)
    public:
        // a と b を組み合わせてできる合成文字を探す。
        // prev != 0 なら、まず prev を探し、さらにその次の合成文字を探してそれを返す(やり直し用)。
        // 見つからなかった場合は 0 を返す。
        // ここでは元祖漢直窓の山辺アルゴリズムを基本として、いくつか改良を加えてある。
        mchar_t FindComposite(mchar_t ca, mchar_t cb, mchar_t prev) {
            _LOG_INFOH(_T("ENTER: ca=%c, cb=%c, prev=%c"), ca, cb, prev);
            mchar_t c = findCompSub((wchar_t)ca, (wchar_t)cb, prev);
            if (c == 0 && prev != 0) c = findCompSub((wchar_t)ca, (wchar_t)cb, 0);    // retry from the beginning
            if (c != 0) {
                setAssocTarget(ca, cb, c);
                addAutoBushuEntry(ca, cb, c);
            }
            _LOG_INFOH(_T("LEAVE: result=%c"), c);
            return c;
        }

    private:
        mchar_t findCompSub(wchar_t ca, wchar_t cb, mchar_t prev) {

            std::set<mchar_t> skipChars;

            bool bFound = prev == 0;

            mchar_t r;

#define _NC(x) (x?x:0x20)
#define CHECK_AND_RETURN(tag, x) {\
        r = (x);\
        if (r != 0 && r != ca && r != cb) { \
            if (r == prev) { \
                bFound = true; \
            } else if (bFound && skipChars.find(r) == skipChars.end()) { \
                _LOG_INFOH(_T(tag ## "result=%s"), MAKE_WPTR(r)); \
                return (mchar_t)r; \
            } \
            skipChars.insert(r); \
        }\
    }

            // それぞれの等価文字
            wchar_t eqa = (wchar_t)findEquiv(ca);
            wchar_t eqb = (wchar_t)findEquiv(cb);

            // まず、足し算(逆順の足し算も先にやっておく)
            CHECK_AND_RETURN("A1", findComp(ca, cb));
            CHECK_AND_RETURN("A2", findComp(cb, ca));

            _LOG_INFOH(_T("EQUIV: eqa=%c, eqb=%c"), eqa, eqb);

            // 等価文字を使って足し算
            if (eqb != cb) CHECK_AND_RETURN("B1", findComp(ca, eqb));
            if (eqa != ca) CHECK_AND_RETURN("B2", findComp(eqa, cb));
            if (eqb != cb) CHECK_AND_RETURN("B3", findComp(eqb, ca));
            if (eqa != ca) CHECK_AND_RETURN("B4", findComp(cb, eqa));

            // 等価文字同士で足し算
            if (eqa != ca || eqb != cb) CHECK_AND_RETURN("C1", findComp(eqa, eqb));
            if (eqa != ca || eqb != cb) CHECK_AND_RETURN("C2", findComp(eqb, eqa));

            // ここまでで合成文字が見つからなければ、部品を使う
            wchar_t a1, a2, b1, b2;
            decompose(ca, a1, a2);       // a := a1 + a2
            decompose(cb, b1, b2);       // b := b1 + b2

            _LOG_INFOH(L"PARTS: a1=%c, a2=%c, b1=%c, b2=%c", _NC(a1), _NC(a2), _NC(b1), _NC(b2));

            // 引き算
            if (a1 && a2) {
                if (a2 == cb || a2 == eqb) CHECK_AND_RETURN("F1", a1);     // a1 = ca - cb(eqb)
                if (a1 == cb || a1 == eqb) CHECK_AND_RETURN("F2", a2);     // a2 = ca - cb(eqb)
            }
            // 引き算(逆順)
            if (b1 && b2) {
                if (b2 == ca || b2 == eqa) CHECK_AND_RETURN("G1", b1);     // b1 = cb - ca(eqa)
                if (b1 == ca || b1 == eqa) CHECK_AND_RETURN("G2", b2);     // b2 = cb - ca(eqa)
            }

            // YAMANOBE_ADD
            // たとえば、準 = 淮十、隼 = 隹十 のとき、シ隼 ⇒ 準 を出したい
#define YAMANOBE_ADD_A(tag, x1, x2, y) { \
            _LOG_INFOH(_T(tag ## "1-Y-ADD: x1=%c, x2=%c, y=%c"), _NC(x1), _NC(x2), _NC(y)); \
            mchar_t z; \
            if ((z = findComp(x1, y)) != 0) { \
                _LOG_INFOH(_T(tag ## "1-Y-ADD(x1+y)=%c"), z); \
                CHECK_AND_RETURN(tag ## "1-Y-ADD((x1+y)+x2):", findComp(z, x2)); /* X = X1 + X2 のとき、 (X1 + Y) + X2 を出したい */ \
                CHECK_AND_RETURN(tag ## "1-Y-ADD(x2+(x1+y)):", findComp(x2, z)); /* X = X1 + X2 のとき、 X2 + (X1 + Y) を出したい */ \
            } \
            if ((z = findComp(x2, y)) != 0) { \
                _LOG_INFOH(_T(tag ## "1-Y-ADD(x2+y)=%c"), z); \
                CHECK_AND_RETURN(tag ## "1-Y-ADD(x1+(x2+y)):", findComp(x1, z)); /* X = X1 + X2 のとき、 X1 + (X2 + Y) を出したい */ \
                CHECK_AND_RETURN(tag ## "1-Y-ADD((x2+y)+x1):", findComp(z, x1)); /* X = X1 + X2 のとき、 (X2 + Y) + X1 を出したい */ \
            } \
        }
#define YAMANOBE_ADD_B(tag, x, y1, y2) { \
            _LOG_INFOH(_T(tag ## "2-Y-ADD: x=%c, y1=%c, y2=%c"), _NC(x), _NC(y1), _NC(y2)); \
            mchar_t z; \
            if ((z = findComp(x, y1)) != 0) { \
                _LOG_INFOH(_T(tag ## "2-Y-ADD(x+y1)=%c"), z); \
                CHECK_AND_RETURN(tag ## "2-Y-ADD((x+y1)+y2):", findComp(z, y2)); /* Y = Y1 + Y2 のとき、 (X + Y1) + Y2 を出したい */ \
                CHECK_AND_RETURN(tag ## "2-Y-ADD(y2+(x+y1)):", findComp(y2, z)); /* Y = Y1 + Y2 のとき、 Y2 + (X + Y1) を出したい */ \
            } \
            if ((z = findComp(x, y2)) != 0) { \
                _LOG_INFOH(_T(tag ## "2-Y-ADD(x+y2)=%c"), z); \
                CHECK_AND_RETURN(tag ## "2-Y-ADD(y1+(x+y2)):", findComp(y1, z)); /* Y = Y1 + Y2 のとき、 Y1 + (X + Y2) を出したい */ \
                CHECK_AND_RETURN(tag ## "2-Y-ADD((x+y2)+y1):", findComp(z, y1)); /* Y = Y1 + Y2 のとき、 (X + Y2) + Y1 を出したい */ \
            } \
        }

            if (a1 && a2) {
                YAMANOBE_ADD_A("D", a1, a2, cb);   // (A1 + B) + A2 または A1 + (A2 + B) または A2 + (
            }
            if (b1 && b2) {
                YAMANOBE_ADD_B("D", ca, b1, b2);   // (A + B1) + B2 または B1 + (A + B2)
            }
            // YAMANOBE_ADD 逆順
            if (b1 && b2) {
                YAMANOBE_ADD_A("E", b1, b2, ca);   // (B1 + A) + B2 または B1 + (B2 + A)
            }
            if (a1 && a2) {
                YAMANOBE_ADD_B("E", cb, a1, a2);   // (B + A1) + A2 または A1 + (B + A2)
            }
            
            // YAMANOBE_SUBTRACT
            // たとえば、準 = 淮十、隼 = 隹十 のとき、シ準 ⇒ 隼 を出したい
#define YAMANOBE_SUBTRACT(tag, x, x1, x2, y, y1, y2, z1, z2) \
            _LOG_INFOH(_T(tag ## "-Y-SUB: x=%c, x1=%c, x2=%c, y=%c, y1=%c, y2=%c, z1=%c, z2=%c"), _NC(x), _NC(x1), _NC(x2), _NC(y), _NC(y1), _NC(y2), _NC(z1), _NC(z2)); \
            if ((z1 != 0 && x2 == z1) || (z2 != 0 && x2 == z2)) CHECK_AND_RETURN(tag ## "-Y-SUB(x1,y):", findComp(x1, y));  /* A := (X1 + X2) + Y && X2 == B ならば X1 + Y (= A - X2) を出したい */ \
            if ((z1 != 0 && x1 == z1) || (z2 != 0 && x1 == z2)) CHECK_AND_RETURN(tag ## "-Y-SUB(x2,y):", findComp(x2, y));  /* A := (X1 + X2) + Y && X1 == B ならば X2 + Y (= A - X1) を出したい */ \
            if ((z1 != 0 && y2 == z1) || (z2 != 0 && y2 == z2)) CHECK_AND_RETURN(tag ## "-Y-SUB(x,y1):", findComp(x, y1));  /* A := X + (Y1 + Y2) && Y2 == B ならば X + Y1 (= A - Y2) を出したい */ \
            if ((z1 != 0 && y1 == z1) || (z2 != 0 && y1 == z2)) CHECK_AND_RETURN(tag ## "-Y-SUB(x,y2):", findComp(x, y2));  /* A := X + (Y1 + Y2) && Y1 == B ならば X + Y2 (= A - Y1) を出したい */

            if (a1 && a2) {
                wchar_t a11, a12, a21, a22;
                decompose(a1, a11, a12);
                decompose(a2, a21, a22);
                YAMANOBE_SUBTRACT("H", a1, a11, a12, a2, a21, a22, cb, 0);
            }
            // YAMANOBE_SUBTRACT 逆順
            if (b1 && b2) {
                wchar_t b11, b12, b21, b22;
                decompose(b1, b11, b12);
                decompose(b2, b21, b22);
                YAMANOBE_SUBTRACT("I", b1, b11, b12, b2, b21, b22, ca, 0);

            }

            // 一方が部品による足し算(直後に逆順をやる)
            CHECK_AND_RETURN("J1", findComp(ca, b1));
            CHECK_AND_RETURN("K5", findComp(b1, ca));

            CHECK_AND_RETURN("J2", findComp(ca, b2));
            CHECK_AND_RETURN("K6", findComp(b2, ca));

            CHECK_AND_RETURN("J3", findComp(eqa, b1));
            CHECK_AND_RETURN("K7", findComp(b1, eqa));

            CHECK_AND_RETURN("J4", findComp(eqa, b2));
            CHECK_AND_RETURN("K8", findComp(b2, eqa));

            CHECK_AND_RETURN("J5", findComp(a1, cb));
            CHECK_AND_RETURN("K1", findComp(cb, a1));

            CHECK_AND_RETURN("J6", findComp(a2, cb));
            CHECK_AND_RETURN("K2", findComp(cb, a2));

            CHECK_AND_RETURN("J7", findComp(a1, eqb));
            CHECK_AND_RETURN("K3", findComp(eqb, a1));

            CHECK_AND_RETURN("J8", findComp(a2, eqb));
            CHECK_AND_RETURN("K4", findComp(eqb, a2));

            // YAMANOBE_ADD (Bが部品)
            if (a1 && a2 && b1) {
                YAMANOBE_ADD_A("L1", a1, a2, b1);   // (A1 + B1) + A2 または A1 + (A2 + B1)
            }
            if (a1 && a2 && b2) {
                YAMANOBE_ADD_A("L2", a1, a2, b2);   // (A1 + B2) + A2 または A1 + (A2 + B2)
            }
            if (b1 && b2 && a1) {
                YAMANOBE_ADD_B("L3", a1, b1, b2);   // (A1 + B1) + B2 または B1 + (A1 + B2)
            }
            if (b1 && b2 && a2) {
                YAMANOBE_ADD_B("L4", a2, b1, b2);   // (A2 + B1) + B2 または B1 + (A2 + B2)
            }
            // YAMANOBE_ADD 逆順(Aが部品)
            if (b1 && b2 && a1) {
                YAMANOBE_ADD_A("M1", b1, b2, a1);   // (B1 + A1) + B2 または B1 + (B2 + A1)
            }
            if (b1 && b2 && a2) {
                YAMANOBE_ADD_A("M2", b1, b2, a2);   // (B1 + A2) + B2 または B1 + (B2 + A2)
            }
            if (a1 && a2 && b1) {
                YAMANOBE_ADD_B("M3", b1, a1, a2);   // (B1 + A1) + A2 または A1 + (B1 + A2)
            }
            if (a1 && a2 && b2) {
                YAMANOBE_ADD_B("M4", b2, a1, a2);   // (B2 + A1) + A2 または A1 + (B2 + A2)
            }

            // 両方が部品による足し算(直後に逆順をやる)
            CHECK_AND_RETURN("N1", findComp(a1, b1));
            CHECK_AND_RETURN("O1", findComp(b1, a1));

            CHECK_AND_RETURN("N1", findComp(a1, b2));
            CHECK_AND_RETURN("O1", findComp(b2, a1));

            CHECK_AND_RETURN("N1", findComp(a2, b1));
            CHECK_AND_RETURN("O1", findComp(b1, a2));

            CHECK_AND_RETURN("N1", findComp(a2, b2));
            CHECK_AND_RETURN("O1", findComp(b2, a2));

            // 部品による引き算
            if (a2 == b1 || a2 == b2) CHECK_AND_RETURN("P1", a1);
            if (a1 == b1 || a1 == b2) CHECK_AND_RETURN("P1", a2);

            // 部品による引き算(逆順)
            if (b2 == a1 || b2 == a2) CHECK_AND_RETURN("Q1", a1);
            if (b1 == a1 || b1 == a2) CHECK_AND_RETURN("Q1", a2);

            // YAMANOBE_SUBTRACT (Bが部品)
            if (a1 && a2) {
                wchar_t a11, a12, a21, a22;
                decompose(a1, a11, a12);
                decompose(a2, a21, a22);
                YAMANOBE_SUBTRACT("R", a1, a11, a12, a2, a21, a22, b1, b2);
            }
            // YAMANOBE_SUBTRACT 逆順(Aが部品)
            if (b1 && b2) {
                wchar_t b11, b12, b21, b22;
                decompose(b1, b11, b12);
                decompose(b2, b21, b22);
                YAMANOBE_SUBTRACT("S", b1, b11, b12, b2, b21, b22, a1, a2);
            }

            // 再帰足し算(一方の文字から合成できる文字を使って足し算)
            // (瞳=目+童 という定義があるとき、目+立 または 目+里 で 瞳 を合成する)
            // ただし、等価文字およびひらがな部品は除く
#define ADD_BY_COMPOSITE(tag, cz, z, x) \
            _LOG_INFOH(_T(tag ## ": cz=%c, z=%c, x=%c"), _NC(cz), _NC(z), _NC(x)); \
            if (cz) CHECK_AND_RETURN(tag ## "(cz,x)", findComp(cz, x)); \
            if (z && z != cz) CHECK_AND_RETURN(tag ## "(z,x)", findComp(z, x)); \
            if (cz) CHECK_AND_RETURN(tag ## "(x,cz)", findComp(x, cz)); \
            if (z && z != cz) CHECK_AND_RETURN(tag ## "(x,z)", findComp(x, z))

#define _PARTS_MAX 100

            if (!utils::is_hiragana(cb) && getBodies(cb).size() <= _PARTS_MAX) {
                for (auto x : getBodies(cb)) {
                    ADD_BY_COMPOSITE("T1", ca, eqa, x);
                }
            }
            if (eqb && eqb != cb && !utils::is_hiragana(eqb) && getBodies(eqb).size() <= _PARTS_MAX) {
                for (auto x : getBodies(eqb)) {
                    ADD_BY_COMPOSITE("T2", ca, eqa, x);
                }
            }

            // 同、逆順
            if (!utils::is_hiragana(ca) && getBodies(ca).size() <= _PARTS_MAX) {
                for (auto x : getBodies(ca)) {
                    ADD_BY_COMPOSITE("T3", cb, eqb, x);
                }
            }
            if (eqa && eqa != ca && !utils::is_hiragana(eqa) && getBodies(eqa).size() <= _PARTS_MAX) {
                for (auto x : getBodies(eqa)) {
                    ADD_BY_COMPOSITE("T4", cb, eqb, x);
                }
            }

            // 部品との合成を試す
            if (!utils::is_hiragana(cb) && getBodies(cb).size() <= _PARTS_MAX) {
                for (auto x : getBodies(cb)) {
                    ADD_BY_COMPOSITE("U1", a1, a2, x);   // Aの部品との合成を試す
                }
            }
            if (eqb && eqb != cb && !utils::is_hiragana(eqb) && getBodies(eqb).size() <= _PARTS_MAX) {
                for (auto x : getBodies(eqb)) {
                    ADD_BY_COMPOSITE("U2", a1, a2, x);   // Aの部品との合成を試す
                }
            }

            // 同、逆順
            if (!utils::is_hiragana(ca) && getBodies(ca).size() <= _PARTS_MAX) {
                for (auto x : getBodies(ca)) {
                    ADD_BY_COMPOSITE("U3", b1, b2, x);   // Bの部品との合成を試す
                }
            }
            if (eqa && eqa != ca && !utils::is_hiragana(eqa) && getBodies(eqa).size() <= _PARTS_MAX) {
                for (auto x : getBodies(eqa)) {
                    ADD_BY_COMPOSITE("U4", b1, b2, x);   // Bの部品との合成を試す
                }
            }

            // 同じ文字同士の合成の場合は、等価文字を出力する
            if (ca == cb && eqa != 0 && eqa != ca) CHECK_AND_RETURN("V", eqa);

            LOG_DEBUGH(_T("result: NULL"));
            return 0;
        }
#undef _NC
#undef _LOG_INFOH
#undef CHECK_AND_RETURN
#undef YAMANOBE_ADD_A
#undef YAMANOBE_ADD_B
#undef YAMANOBE_SUBTRACT
#undef ADD_BY_COMPOSITE

    public:
        // 文字 m から合成可能な文字を list に追加する(m およびその部品のうち頻度の低いほうを使う)
        // m の部品文字は先頭に追加する
        // 記号類は末尾に追加する
        // firstLevel の文字はさらにその後に追加する。
        void GatherDerivedMoji(mchar_t m, std::vector<mchar_t>& list)
        {
            //int origLen = list.size();
            std::vector<mchar_t> symList;
            std::vector<mchar_t> fsList;
            std::set<mchar_t> ms(list.begin(), list.end());

            auto addIfAbsent = [](mchar_t mc, std::vector<mchar_t>& list, std::set<mchar_t>& ms) {
                if (ms.find(mc) == ms.end()) {
                    list.push_back(mc);
                    ms.insert(mc);
                }
            };

            wchar_t a, b;
            decompose(m, a, b);
            // m 自身と、それの部品のうち頻度の低い方でやる
            mchar_t chrs[] = { m, a };
            //// 両方の部品を使う
            //mchar_t chrs[] = { m, a, b };
            if (a != 0 && a != m) addIfAbsent(a, list, ms);
            if (b != 0 && b != a && b != m) addIfAbsent(b, list, ms);
            size_t num = utils::array_length(chrs);
            for (size_t i = 0; i < num && chrs[i] != 0; ++i) {
                for (auto c : getBodies(chrs[i])) {
                    //wchar_t ch = (wchar_t)c;
                    if (c < FirstKanji) {
                        // 記号類は末尾に追加
                        if (std::find(list.begin(), list.end(), c) == list.end() &&
                            std::find(symList.begin(), symList.end(), c) == symList.end()) symList.push_back(c);
                    } else if (firstLevel.find((wchar_t)c) != firstLevel.end()) {
                        // 第1級文字は末尾に追加
                        if (std::find(list.begin(), list.end(), c) == list.end() &&
                            std::find(fsList.begin(), fsList.end(), c) == fsList.end()) fsList.push_back(c);
                    } else {
                        addIfAbsent(c, list, ms);
                    }
                }
            }
            if (symList.size() > 0) list.insert(list.end(), symList.begin(), symList.end());
            if (fsList.size() > 0) list.insert(list.end(), fsList.begin(), fsList.end());
        }

        //仮想鍵盤に部首合成ヘルプの情報を設定する
        bool CopyBushuCompHelpToVkbFaces(mchar_t ch, wchar_t* faces, size_t kbLen, size_t kbNum, bool bSetAssoc) {
            auto gatherBodies = [this](mchar_t x) {
                std::vector<wchar_t> result;
                std::vector<wchar_t> temp;
                for (auto ch : getBodies(x)) {
                    mchar_t c = (mchar_t)ch;
                    if (EASY_CHARS->IsFirstLevel(c)) {
                        LOG_DEBUG(_T("FirstLevel: %c"), c);
                        result.push_back((wchar_t)c);
                    } else if (STROKE_HELP->Find(c)) {
                        LOG_DEBUG(_T("SecondLevel: %c"), c);
                        temp.push_back((wchar_t)c);
                    }
                }
                utils::append(result, temp);
                return result;
            };

            // クリアしておく
            size_t numFaces = kbLen * kbNum;
            for (size_t i = 0; i < numFaces; i += kbLen) {
                faces[i] = faces[i + 1] = 0;
            }

            auto parts = findParts(ch);
            if (parts.empty()) {
                for (auto c : gatherBodies(ch)) {
                    parts = findParts(c);
                    faces[kbLen * 4] = c;
                    wchar_t a = parts.a();
                    wchar_t b = parts.b();
                    faces[kbLen * 5] = a == ch ? b : a;
                    return true;
                }
                return false;
            }

            wchar_t a = parts.a();
            wchar_t b = parts.b();

            if (bSetAssoc) setAssocTarget(a, b, ch);

            size_t leftIn = (kbNum / 2) - 1;
            size_t rightIn = leftIn + 1;
            faces[kbLen * leftIn] = a; faces[kbLen * leftIn + 1] = 0;
            faces[kbLen * rightIn] = b; faces[kbLen * rightIn + 1] = 0;

            size_t pos = kbLen * (leftIn - 1);
            if (!EASY_CHARS->IsFirstLevel(a)) {
                for (auto c : gatherBodies(a)) {
                    faces[pos] = c; faces[pos + 1] = 0;
                    if (pos == 0) break;
                    pos -= kbLen;
                }
            }
            for (; pos > 0; pos -= kbLen) faces[pos] = 0;

            pos = kbLen * (rightIn + 1);
            if (!EASY_CHARS->IsFirstLevel(b)) {
                for (auto c : gatherBodies(b)) {
                    faces[pos] = c; faces[pos + 1] = 0;
                    if (pos == numFaces) break;
                    pos += kbLen;
                }
            }
            for (; pos < numFaces; pos += kbLen) faces[pos] = 0;

            return true;
        }

    };
    DEFINE_CLASS_LOGGER(BushuDicImpl);

} // namespace

// -------------------------------------------------------------------
DEFINE_CLASS_LOGGER(BushuDic);

std::unique_ptr<BushuDic> BushuDic::Singleton;

namespace {
    DEFINE_LOCAL_LOGGER(BushuDic);

    bool readBushuFile(const tstring& path, BushuDicImpl* pDic) {
        LOG_INFO(_T("open bushu file: %s"), path.c_str());
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            pDic->ReadFile(reader.getAllLines());
            LOG_INFO(_T("close bushu file: %s"), path.c_str());
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_WARN(_T("Can't read bushu file: %s"), path.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("部首合成入力辞書ファイル(%s)が開けません"), path.c_str()));
            return false;
        }
        return true;
    }

    bool writeBushuFile(const tstring& path, BushuDicImpl* pDic) {
        LOG_INFO(_T("write bushu file: %s, dirty=%s"), path.c_str(), BOOL_TO_WPTR(pDic && pDic->IsDirty()));
        if (!path.empty() && pDic) {
            if (pDic->IsDirty()) {
                if (utils::copyFileToBackDirWithRotation(path, SETTINGS->backFileRotationGeneration)) {
                    utils::OfstreamWriter writer(path, true);
                    if (writer.success()) {
                        pDic->WriteFile(writer);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    bool readAutoBushuFile(const tstring& path, BushuDicImpl* pDic) {
        LOG_INFO(_T("open auto bushu file: %s"), path.c_str());
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            pDic->ReadAutoDicFile(reader.getAllLines());
            LOG_INFO(_T("close bushu file: %s"), path.c_str());
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_WARN(_T("Can't read auto bushu file: %s"), path.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("自動部首合成入力辞書ファイル(%s)が開けません"), path.c_str()));
            return false;
        }
        return true;
    }

    bool writeAutoBushuFile(const tstring& path, BushuDicImpl* pDic) {
        LOG_INFO(_T("write auto bushu file: %s, dirty=%s"), path.c_str(), BOOL_TO_WPTR(pDic && pDic->IsAutoDirty()));
        if (!path.empty() && pDic) {
            if (pDic->IsAutoDirty()) {
                if (utils::copyFileToBackDirWithRotation(path, SETTINGS->backFileRotationGeneration)) {
                    utils::OfstreamWriter writer(path, true);
                    if (writer.success()) {
                        pDic->WriteAutoDicFile(writer);
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

// 部首合成辞書を作成する(ファイルが指定されていなくても作成する)
// エラーがあったら例外を投げる
int BushuDic::CreateBushuDic(const tstring& bushuFile) {
    LOG_INFO(_T("ENTER"));

    if (Singleton != 0) {
        LOG_INFO(_T("already created: bushu file: %s"), bushuFile.c_str());
        return 0;
    }

    int result = 0;

    auto pImpl = new BushuDicImpl();
    if (bushuFile.empty() || readBushuFile(bushuFile, pImpl)) {
        Singleton.reset(pImpl);
    } else {
        result = -1;
    }

    LOG_INFO(_T("LEAVE: result=%d"), result);
    return result;
}

// 部首合成辞書を読み込む
void BushuDic::ReadBushuDic(const tstring& path) {
    LOG_INFO(_T("CALLED: path=%s"), path.c_str());
    if (!path.empty() && Singleton) {
        readBushuFile(path, (BushuDicImpl*)Singleton.get());
    }
}

// 部首合成辞書ファイルに書き込む
void BushuDic::WriteBushuDic(const tstring& path) {
    LOG_INFO(_T("CALLED: path=%s"), path.c_str());
    writeBushuFile(path, (BushuDicImpl*)Singleton.get());
}

// 自動部首合成辞書を読み込む
void BushuDic::ReadAutoBushuDic(const tstring& path) {
    LOG_INFO(_T("CALLED: path=%s"), path.c_str());
    if (!path.empty() && Singleton) {
        readBushuFile(path, (BushuDicImpl*)Singleton.get());
    }
}

// 自動部首合成辞書ファイルに書き込む
void BushuDic::WriteAutoBushuDic(const tstring& path) {
    LOG_INFO(_T("CALLED: path=%s"), path.c_str());
    writeBushuFile(path, (BushuDicImpl*)Singleton.get());
}

// 部首合成辞書ファイルに書き込む
void BushuDic::WriteBushuDic() {
    WriteBushuDic(SETTINGS->bushuFile);
}

