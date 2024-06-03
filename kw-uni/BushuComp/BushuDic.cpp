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
#include "StrokeTable.h"
#include "BushuDic.h"
#include "BushuAssoc.h"
#include "BushuAssocDic.h"
#include "VkbTableMaker.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughBushu)

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

            wchar_t parts[2] = { 0, 0 };

        public:
            parts_t() : data(0) { }

            parts_t(wchar_t a, wchar_t b) : data((a << 16) + b) {
                parts[0] = a;
                parts[1] = b;
            }

            inline void set(wchar_t a, wchar_t b) {
                data = (a << 16) + b;
                parts[0] = a;
                parts[1] = b;
            }
            inline void exchange() { set(parts[1], parts[0]); }

            inline bool operator==(uint32_t p) const { return data == p; }

            inline bool operator==(parts_t p) const { return data == p.data; }

            inline bool operator<(parts_t p) const { return data < p.data; }

            inline wchar_t a() const { return parts[0]; }
            inline wchar_t b() const { return parts[1]; }

            inline bool empty() const { return data == 0; }
            inline bool notEmpty() const { return data != 0; }
        };

        const wchar_t FirstKanji = FIRST_KANJI;

        // 本体から部品へのマップ
        std::map<mchar_t, parts_t> entries;

        // ストローク可能文字か否かを示すマップ
        std::map<mchar_t, bool> strokableMap;

        // 等価マップ
        std::map<mchar_t, std::set<wchar_t>> equivMap;

        std::set<wchar_t> emptyEquiv;

        inline const std::set<wchar_t>& findEquiv(mchar_t c) {
            auto it = equivMap.find(c);
            return it == equivMap.end() ? emptyEquiv : it->second;
        }

        inline void addEquiv(mchar_t a, mchar_t b) {
            auto it = equivMap.find(a);
            if (it == equivMap.end()) {
                equivMap[a] = std::set<wchar_t>();
            }
            equivMap[a].insert((wchar_t)b);
        }

        // 合成辞書
        std::map<parts_t, mchar_t> compMap;

        // 部品から本体集合(キー文字を部品とする本体文字の集合)へのマップ
        std::map<mchar_t, std::set<mchar_t>> partsBodiesMap;

        // 保存時に辞書ファイルに追加されるエントリ
        std::vector<String> addedEntries;

        // 戻値用の空集合
        std::set<mchar_t> emptySet;

        // 部首のパーツを返すためのリスト
        std::vector<wchar_t> strList;

        // 文字リスト用(文字リストから除外する文字の集合)
        std::set<wchar_t> firstLevel;

        struct AutoBushuTarget {
            mchar_t target;
            size_t count;
        };

        // 自動部首合成用辞書
        std::map<MString, AutoBushuTarget> autoBushuDict;

        // 自動部首合成用辞書が修正された
        bool bAutoDirty = false;

        // 当文字を構成する部品に分解する
        inline parts_t findParts(mchar_t c) {
            auto it = entries.find(c);
            return it == entries.end() ? parts_t() : it->second;
        }

        inline mchar_t findComp(wchar_t a, wchar_t b) {
            if (a == 0 || b == 0) return 0;
            auto it = compMap.find(parts_t(a, b));
            return it == compMap.end() ? 0 : it->second;
        }

        inline mchar_t findComp(const std::set<wchar_t>& as, wchar_t b) {
            for (auto a : as) {
                auto it = compMap.find(parts_t(a, b));
                return it == compMap.end() ? 0 : it->second;
            }
            return 0;
        }

        inline mchar_t findComp(wchar_t a, const std::set<wchar_t>& bs) {
            for (auto b : bs) {
                auto it = compMap.find(parts_t(a, b));
                return it == compMap.end() ? 0 : it->second;
            }
            return 0;
        }

        inline mchar_t findComp(const std::set<wchar_t>& as, const std::set<wchar_t>& bs) {
            for (auto a : as) {
                for (auto b : bs) {
                    auto it = compMap.find(parts_t(a, b));
                    return it == compMap.end() ? 0 : it->second;
                }
            }
            return 0;
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

#define _PARTS_MAX 100

        inline bool isComposableParts(mchar_t ch) {
            return (!utils::is_hiragana(ch) && !utils::is_katakana(ch) && getBodies(ch).size() <= _PARTS_MAX);
        }

        //inline UINT32 partsToUint32(wchar_t a, wchar_t b) { return ((UINT32)a << 16) + ((UINT32)b & 0xffff); }
        //inline wchar_t uint32ToA(UINT32 val) { return (wchar_t)(val >> 16); }
        //inline wchar_t uint32ToB(UINT32 val) { return (wchar_t)(val & 0xffff); }

    public:
        BushuDicImpl() {
        }

    public:
        void ReadFile(const std::vector<String>& lines) {
            LOG_INFO(_T("ENTER: lines num={}"), lines.size());

            // 作業領域
            std::wregex reComment(_T("^# "));

            // 各マップをクリアしておく(再読み込みのため)
            entries.clear();
            strokableMap.clear();
            equivMap.clear();
            compMap.clear();
            partsBodiesMap.clear();
            addedEntries.clear();

            String lastLine;

            for (auto& line : lines) {
                if (line.empty() || std::regex_match(line, reComment)) continue;   // 空行や "# " で始まる行は読み飛ばす

                addBushuEntry(line);

                if (Reporting::Logger::IsInfoEnabled()) lastLine = line;
            }

            LOG_INFO(_T("LEAVE: last line={}"), lastLine);
        }

        // 辞書内容の保存
        void WriteFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED: addedEntries.size()={}"), addedEntries.size());
            for (const auto& line : addedEntries) {
                writer.writeLine(utils::utf8_encode(line));
            }
            addedEntries.clear();
        }

        bool IsDirty() {
            return !addedEntries.empty();
        }

        // 自動部首合成用辞書の読み込み
        void ReadAutoDicFile(const std::vector<String>& lines) {
            LOG_INFO(_T("ENTER: lines num={}"), lines.size());

            // 作業領域
            std::wregex reComment(_T("^# "));

            // 各マップをクリアしておく(再読み込みのため)
            autoBushuDict.clear();
            bAutoDirty = false;

            for (auto& line : lines) {
                if (line.empty() || std::regex_match(line, reComment)) continue;   // 空行や "# " で始まる行は読み飛ばす
                auto items = utils::split(line, '\t');
                if (!items.empty()) {
                    size_t cnt = items.size() > 1 ? utils::strToInt(items[1], 1) : 1;
                    addAutoBushuEntry(to_mstr(items[0]), cnt, false, false);
                }
            }

            LOG_INFO(_T("LEAVE"));
        }

        // 自動合成辞書内容の保存
        void WriteAutoDicFile(utils::OfstreamWriter& writer) {
            LOG_INFO(_T("CALLED: autoBushuDict.size()={}"), autoBushuDict.size());
            std::set<String> set_;
            for (const auto& pair : autoBushuDict) {
                set_.insert(std::format(_T("{}\t{}"), to_wstr(MString(1, pair.second.target) + pair.first), pair.second.count));
            }
            for (const auto& s : set_) {
                writer.writeLine(utils::utf8_encode(s));
            }
            bAutoDirty = false;
        }

        bool IsAutoDirty() {
            return bAutoDirty;
        }

        void MakeStrokableMap() override {
            for (auto pair : partsBodiesMap) {
                strokableMap[pair.first] = STROKE_HELP->Find(pair.first);
            }
        }

        // 1エントリの追加 (ここで追加したエントリは、保存時に辞書ファイルに追記される)
        void AddBushuEntry(StringRef line) override {
            addedEntries.push_back(line);
            addBushuEntry(line);
        }

        // 1自動エントリの強制追加 (無効化も兼ねる; ここで追加したエントリは、保存時に辞書ファイルに追記される)
        void AddAutoBushuEntry(StringRef line) override {
            MString mline = to_mstr(line);
            if (mline.size() == 3) {
                addAutoBushuEntry(mline, SETTINGS->autoBushuCompMinCount, true, true);
            }
        }

        // a と b を組み合わせてできる自動合成文字を探す。
        mchar_t FindAutoComposite(mchar_t ca, mchar_t cb) override {
            auto finder = [this](mchar_t a, mchar_t b) {
                MString key;
                key.push_back(a);
                key.push_back(b);
                _LOG_DEBUGH(_T("key={}"), to_wstr(key));
                auto iter = autoBushuDict.find(key);
                _LOG_DEBUGH(_T("iter->second.target={}"), ((iter != autoBushuDict.end()) ? iter->second.target : 0x20));
                if (iter != autoBushuDict.end() && iter->second.target != '-' && iter->second.count >= SETTINGS->autoBushuCompMinCount) {
                    // 参照回数が閾値以上のエントリが見つかったので、さらに参照回数をインクリメントしておく
                    bAutoDirty = true;
                    iter->second.count++;
                    return iter->second.target;
                }
                return (mchar_t)0; // ターゲットが '-' だったり、最小呼び出し回数に達していなければ変換しない
            };
            //mchar_t result = finder(ca, cb);
            //if (result != 0) return result;
            //return finder(cb, ca);
            // 正順だけをやる
            return finder(ca, cb);
        }

    private:
        // エントリの追加
        void addBushuEntry(StringRef line) {
            auto mline = to_mstr(line);
            size_t pos = 0;
            mchar_t c = pos < mline.size() ? mline[pos++] : 0;
            wchar_t a = (wchar_t)(pos < mline.size() ? mline[pos++] : 0);
            wchar_t b = (wchar_t)(pos < mline.size() ? mline[pos++] : 0);

            if (Reporting::Logger::IsDebugEnabled() && is_paired_mchar(c)) LOG_DEBUG(_T("Surrogate pair={}"), line);

            if (c != 0 && a != 0) {
                addBody(a, c);
                if (b == wchar_t('\r') || b == wchar_t('\n') || b == 0) {
                    // "CA" → A ≡ C (等価文字)
                    addEquiv(a, c);
                    addEquiv(c, a);
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

        bool isInvalidAutoBushuKey(const MString& key) {
            auto iter = autoBushuDict.find(key);
            return iter != autoBushuDict.end() && iter->second.target == '-';
        }

        // 自動エントリの追加 (形式は CAB)
        void addAutoBushuEntry(const MString& line, size_t cnt, bool bForce, bool bDirty) {
            if (line.size() >= 3) {
                mchar_t target = line[0];
                auto key = line.substr(1);
                auto iter = autoBushuDict.find(key);
                if (bForce || target == '-' || iter == autoBushuDict.end() || iter->second.target != '-') {
                    // 強制または無効化または組合せが存在しないまたはターゲットが '-' でない組み合わせの場合に再登録する
                    if (target == '-') cnt = 0;
                    if (!bForce && iter != autoBushuDict.end()) {
                        if (iter->second.target == target) {
                            iter->second.count += cnt;
                        } else {
                            iter->second.count = cnt;
                        }
                    } else {
                        autoBushuDict[key] = AutoBushuTarget{ line[0], cnt };
                    }
                    if (bDirty) bAutoDirty = true;
                }
            }
        }

        void AddAutoBushuEntry(mchar_t a, mchar_t b, mchar_t c) override {
            if (a != 0 && b != 0 && c != 0) {
                _LOG_DEBUGH(_T("key={}, val={}"), to_wstr(make_mstring(a, b)), to_wstr(c));
                addAutoBushuEntry(make_mstring(c, a, b), 1, false, true);
                //MString key = make_mstring(a, b);
                //if (!isInvalidAutoBushuKey(key)) {
                //    // ターゲットが '-' である組み合わせは、再登録しない(無視される)
                //    autoBushuDict[key] = c;
                //    bAutoDirty = true;
                //}
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

//#define _LOG_INFO(...) if (SETTINGS->bushuDicLogEnabled || SETTINGS->debughBushu) logger.InfoH(std::format(__VA_ARGS__), __func__, __FILE__, __LINE__)
#define _LOG_INFO(...) if (SETTINGS->bushuDicLogEnabled || SETTINGS->debughBushu) {}
    public:
        // a と b を組み合わせてできる合成文字を探す。
        // prev != 0 なら、まず prev を探し、さらにその次の合成文字を探してそれを返す(やり直し用)。
        // 見つからなかった場合は 0 を返す。
        // ここでは元祖漢直窓の山辺アルゴリズムを基本として、いくつか改良を加えてある。
        mchar_t FindComposite(mchar_t ca, mchar_t cb, mchar_t prev) override {
            _LOG_INFO(_T("ENTER: ca={}, cb={}, prev={}"), ca, cb, prev);
            mchar_t c = findCompSub((wchar_t)ca, (wchar_t)cb, prev);
            if (c == 0 && prev != 0) c = findCompSub((wchar_t)ca, (wchar_t)cb, 0);    // retry from the beginning
            if (c != 0) {
                setAssocTarget(ca, cb, c);
                if (!(utils::is_punct(ca) || utils::is_punct(cb) || (utils::is_hiragana(ca) && utils::is_hiragana(cb)) || (utils::is_katakana(ca) && utils::is_katakana(cb)))) {
                    // 句読点、平仮名同士または片仮名同士でない場合は、自動部首合成登録を行う
                    AddAutoBushuEntry(ca, cb, c);
                }
            }
            _LOG_INFO(_T("LEAVE: result={}"), c);
            return c;
        }

    private:
        mchar_t findCompSub(wchar_t ca, wchar_t cb, mchar_t prev) {

            std::set<mchar_t> skipChars;

            bool bFound = prev == 0;

            mchar_t r;

#define _NC(x) (x?x:0x20)
#define MAKE_LSTR(x, y) L #x #y
#define CHECK_AND_RETURN(tag, x) {\
        r = (x);\
        if (r != 0 && r != ca && r != cb) { \
            if (r == prev) { \
                bFound = true; \
            } else if (bFound && skipChars.find(r) == skipChars.end()) { \
                _LOG_INFO(MAKE_LSTR(tag, "result={}"), to_wstr(r)); \
                return (mchar_t)r; \
            } \
            skipChars.insert(r); \
        }\
    }

            // それぞれの等価文字
            const std::set<wchar_t>& eqa = findEquiv(ca);
            const std::set<wchar_t>& eqb = findEquiv(cb);

            // まず、足し算(逆順の足し算も先にやっておく)
            CHECK_AND_RETURN("A1", findComp(ca, cb));
            CHECK_AND_RETURN("A2", findComp(cb, ca));

            _LOG_INFO(_T("EQUIV: eqa={}, eqb={}"), eqa.empty() ? '-' : *eqa.begin(), eqb.empty() ? '-' : *eqb.begin());

            // 等価文字を使って足し算
            CHECK_AND_RETURN("B1", findComp(ca, eqb));
            CHECK_AND_RETURN("B2", findComp(eqa, cb));
            CHECK_AND_RETURN("B3", findComp(eqb, ca));
            CHECK_AND_RETURN("B4", findComp(cb, eqa));

            // 等価文字同士で足し算
            CHECK_AND_RETURN("C1", findComp(eqa, eqb));
            CHECK_AND_RETURN("C2", findComp(eqb, eqa));

            // ここまでで合成文字が見つからなければ、部品を使う
            wchar_t a1, a2, b1, b2;
            decompose(ca, a1, a2);       // a := a1 + a2
            decompose(cb, b1, b2);       // b := b1 + b2

            _LOG_INFO(L"PARTS: a1={}, a2={}, b1={}, b2={}", _NC(a1), _NC(a2), _NC(b1), _NC(b2));

            // 引き算
            if (a1 && a2) {
                if (a2 == cb || eqb.find(a2) != eqb.end()) CHECK_AND_RETURN("F1", a1);     // a1 = ca - cb(eqb)
                if (a1 == cb || eqb.find(a1) != eqb.end()) CHECK_AND_RETURN("F2", a2);     // a2 = ca - cb(eqb)
            }
            // 引き算(逆順)
            if (b1 && b2) {
                if (b2 == ca || eqa.find(b2) != eqa.end()) CHECK_AND_RETURN("G1", b1);     // b1 = cb - ca(eqa)
                if (b1 == ca || eqa.find(b1) != eqa.end()) CHECK_AND_RETURN("G2", b2);     // b2 = cb - ca(eqa)
            }

            if (SETTINGS->yamanobeEnabled) {
                // YAMANOBE_ADD
                // たとえば、準 = 淮十、隼 = 隹十 のとき、シ隼 ⇒ 準 を出したい
#define YAMANOBE_ADD_A(tag, x1, x2, y) { \
                    _LOG_INFO(MAKE_LSTR(tag, "1-Y-ADD: x1={}, x2={}, y={}"), _NC(x1), _NC(x2), _NC(y)); \
                    mchar_t z; \
                    if ((z = findComp(x1, y)) != 0) { \
                        _LOG_INFO(MAKE_LSTR(tag, "1-Y-ADD(x1+y)={}"), (wchar_t)z); \
                        CHECK_AND_RETURN(#tag "1-Y-ADD((x1+y)+x2):", findComp((wchar_t)z, x2)); /* X = X1 + X2 のとき、 (X1 + Y) + X2 を出したい */ \
                        CHECK_AND_RETURN(#tag  "1-Y-ADD(x2+(x1+y)):", findComp(x2, (wchar_t)z)); /* X = X1 + X2 のとき、 X2 + (X1 + Y) を出したい */ \
                    } \
                    if ((z = findComp(x2, y)) != 0) { \
                        _LOG_INFO(MAKE_LSTR(tag, "1-Y-ADD(x2+y)={}"), (wchar_t)z); \
                        CHECK_AND_RETURN(#tag  "1-Y-ADD(x1+(x2+y)):", findComp(x1, (wchar_t)z)); /* X = X1 + X2 のとき、 X1 + (X2 + Y) を出したい */ \
                        CHECK_AND_RETURN(#tag  "1-Y-ADD((x2+y)+x1):", findComp((wchar_t)z, x1)); /* X = X1 + X2 のとき、 (X2 + Y) + X1 を出したい */ \
                    } \
                }
#define YAMANOBE_ADD_B(tag, x, y1, y2) { \
                    _LOG_INFO(MAKE_LSTR(tag, "2-Y-ADD: x={}, y1={}, y2={}"), _NC(x), _NC(y1), _NC(y2)); \
                    mchar_t z; \
                    if ((z = findComp(x, y1)) != 0) { \
                        _LOG_INFO(MAKE_LSTR(tag, "2-Y-ADD(x+y1)={}"), (wchar_t)z); \
                        CHECK_AND_RETURN(#tag  "2-Y-ADD((x+y1)+y2):", findComp((wchar_t)z, y2)); /* Y = Y1 + Y2 のとき、 (X + Y1) + Y2 を出したい */ \
                        CHECK_AND_RETURN(#tag  "2-Y-ADD(y2+(x+y1)):", findComp(y2, (wchar_t)z)); /* Y = Y1 + Y2 のとき、 Y2 + (X + Y1) を出したい */ \
                    } \
                    if ((z = findComp(x, y2)) != 0) { \
                        _LOG_INFO(MAKE_LSTR(tag, "2-Y-ADD(x+y2)={}"), (wchar_t)z); \
                        CHECK_AND_RETURN(#tag  "2-Y-ADD(y1+(x+y2)):", findComp(y1, (wchar_t)z)); /* Y = Y1 + Y2 のとき、 Y1 + (X + Y2) を出したい */ \
                        CHECK_AND_RETURN(#tag  "2-Y-ADD((x+y2)+y1):", findComp((wchar_t)z, y1)); /* Y = Y1 + Y2 のとき、 (X + Y2) + Y1 を出したい */ \
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
                _LOG_INFO(L #tag "-Y-SUB: x={}, x1={}, x2={}, y={}, y1={}, y2={}, z1={}, z2={}", _NC(x), _NC(x1), _NC(x2), _NC(y), _NC(y1), _NC(y2), _NC(z1), _NC(z2)); \
                if ((z1 != 0 && x2 == z1) || (z2 != 0 && x2 == z2)) CHECK_AND_RETURN(#tag  "-Y-SUB(x1,y):", findComp(x1, y));  /* A := (X1 + X2) + Y && X2 == B ならば X1 + Y (= A - X2) を出したい */ \
                if ((z1 != 0 && x1 == z1) || (z2 != 0 && x1 == z2)) CHECK_AND_RETURN(#tag  "-Y-SUB(x2,y):", findComp(x2, y));  /* A := (X1 + X2) + Y && X1 == B ならば X2 + Y (= A - X1) を出したい */ \
                if ((z1 != 0 && y2 == z1) || (z2 != 0 && y2 == z2)) CHECK_AND_RETURN(#tag  "-Y-SUB(x,y1):", findComp(x, y1));  /* A := X + (Y1 + Y2) && Y2 == B ならば X + Y1 (= A - Y2) を出したい */ \
                if ((z1 != 0 && y1 == z1) || (z2 != 0 && y1 == z2)) CHECK_AND_RETURN(#tag  "-Y-SUB(x,y2):", findComp(x, y2));  /* A := X + (Y1 + Y2) && Y1 == B ならば X + Y2 (= A - Y1) を出したい */

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

            if (SETTINGS->yamanobeEnabled) {
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

            if (SETTINGS->yamanobeEnabled) {
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
            }

            // 再帰足し算(一方の文字から合成できる文字を使って足し算)
            // (瞳=目+童 という定義があるとき、目+立 または 目+里 で 瞳 を合成する)
            // ただし、等価文字およびひらがな部品は除く
#define ADD_BY_COMPOSITE1(tag, cz, eqz, x) \
            if (cz) CHECK_AND_RETURN(#tag  "(cz,x)", findComp(cz, x)); \
            if (!eqz.empty()) CHECK_AND_RETURN(#tag  "(eqz,x)", findComp(eqz, x)); \
            if (cz) CHECK_AND_RETURN(#tag  "(x,cz)", findComp(x, cz)); \
            if (!eqz.empty()) CHECK_AND_RETURN(#tag  "(x,eqz)", findComp(x, eqz))

#define ADD_BY_COMPOSITE2(tag, cz, z, x) \
            _LOG_INFO(MAKE_LSTR(tag, ": cz={}, z={}, x={}"), _NC(cz), _NC(z), _NC(x)); \
            if (cz) CHECK_AND_RETURN(#tag  "(cz,x)", findComp(cz, x)); \
            if (z && z != cz) CHECK_AND_RETURN(#tag  "(z,x)", findComp(z, x)); \
            if (cz) CHECK_AND_RETURN(#tag  "(x,cz)", findComp(x, cz)); \
            if (z && z != cz) CHECK_AND_RETURN(#tag  "(x,z)", findComp(x, z))

            if (isComposableParts(cb)) {
                for (auto x : getBodies(cb)) {
                    ADD_BY_COMPOSITE1("T1", ca, eqa, (wchar_t)x);
                }
            }
            for (auto eb : eqb) {
                if (isComposableParts(eb)) {
                    for (auto x : getBodies(eb)) {
                        ADD_BY_COMPOSITE1("T2", ca, eqa, (wchar_t)x);
                    }
                }
            }

            // 同、逆順
            if (isComposableParts(ca)) {
                for (auto x : getBodies(ca)) {
                    ADD_BY_COMPOSITE1("T3", cb, eqb, (wchar_t)x);
                }
            }
            for (auto ea : eqa) {
                if (isComposableParts(ea)) {
                    for (auto x : getBodies(ea)) {
                        ADD_BY_COMPOSITE1("T4", cb, eqb, (wchar_t)x);
                    }
                }
            }

            // 部品との合成を試す
            if (isComposableParts(cb)) {
                for (auto x : getBodies(cb)) {
                    ADD_BY_COMPOSITE2("U1", a1, a2, (wchar_t)x);   // Aの部品との合成を試す
                }
            }
            for (auto eb : eqb) {
                if (isComposableParts(eb)) {
                    for (auto x : getBodies(eb)) {
                        ADD_BY_COMPOSITE2("U2", a1, a2, (wchar_t)x);   // Aの部品との合成を試す
                    }
                }
            }

            // 同、逆順
            if (isComposableParts(ca)) {
                for (auto x : getBodies(ca)) {
                    ADD_BY_COMPOSITE2("U3", b1, b2, (wchar_t)x);   // Bの部品との合成を試す
                }
            }
            for (auto ea : eqa) {
                if (isComposableParts(ea)) {
                    for (auto x : getBodies(ea)) {
                        ADD_BY_COMPOSITE2("U4", b1, b2, (wchar_t)x);   // Bの部品との合成を試す
                    }
                }
            }

            // 同じ文字同士の合成の場合は、等価文字を出力する
            if (ca == cb && !eqa.empty()) CHECK_AND_RETURN("V", *eqa.begin());

            LOG_DEBUGH(_T("result: NULL"));
            return 0;
        }
#undef _NC
#undef _LOG_INFO
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
        void GatherDerivedMoji(mchar_t m, std::vector<mchar_t>& list) override {
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

    private:
        // 常用・人名漢字
        const wchar_t* popularChars =
            L"人一日大年出本中子見国言上分生手自行者二間事思時気会十家女三前的方入小地合後目長場代私下立部学物月田何来彼話体動社知理山内同心発高実作当新世今書度明五戦力名金性対意用男主通関文屋感郎業定政持道外取所現"
            L"最化四先民身不口川東相多法全聞情野考向平成軍開教経信近以語面連問原顔正機九次数美回食表八声水報真味界無少要海変結切重天神記木集和員引公画死安兵親六治決太氏衛強使込朝受島解市期様村活頭題万組仕白指説七能"
            L"京葉第流然初足円在門調笑品電議直着保別夫音選元権特義父利制続風北石車進夜伝母加助点産務件命番落付得半戸好空有違吉殺起運置料士返藤論楽際歳色帰歩井悪広店反町形百光首勝必土係由愛都住江西売放確過張約馬状待"
            L"古想始官交読千米配若終資常果呼校武共計残判役城院他総術支送族両乗団松映応済線買設右供格病打視早勢御断式質師台党告深存争室覚史側飛参可態営府突巻容姿育之介建南構認位達転左皇宮守満消任医蔵止造居離根予路字"
            L"座工寺基客急船図追隊査背観誰黒素息価将伊改県撃失泉老良示振号像職王識警花優投英細局難種証走念寄商青谷害奥派僕佐頼横友再増紀統答差火苦器収段異血護紙俺歌渡与注条演算赤備独象清技州申例働景領春抜遠橋源芸影"
            L"型絶館眼香負福企旅球酒君験量察写望久婚単押割限戻科求津案談降妻岡境熱策浮階等末宗区波提去幸研移域飲肉草周昭越服接鉄密司登頃銀類検未材個康沢協茶各究帯規秀歴編興裏精洋率抱比坂評装監崎省鮮税激徳挙志労競処"
            L"退費非囲喜辺敷系河織製娘端逃探極遺防低犯薬園疑林導緒静具席房速街舞宿森程丸胸陸倒寝宅兄療絵諸尾株破秋堂従庭管婦仲革余敵展訪担冷効暮腹賞危毎星許復似片並底険描週修財遊温軽録腰我著乱章雑殿載響秘布恐攻値角"
            L"暗習健仏積裁試夏隠永誌夢環援故幕減略準委痛富督倉弟刻鳴令刊施焼欲途曲耳完里願罪圧額印嫌池陽臣庫継亡散障貴農掛板昨整怒衆恋羽及専逆腕盛玄留礼短普岩竹児毛列恵版授雪弾宇養驚払奈推給況樹為阿敗雄捕刀被雨岸超"
            L"豊忘含弁植妙模補抗級休暴課瞬称跡触玉晴華因震折億肩劇迎傷悲闘港責筋訳除射善刺黙柄刑節述輪困脱浅鳥厳純犬博陣薄阪閉吸奇忠夕固染巨講微髪標束縁眠壁午般湯捨駆衣替麻甲央藩骨彦齢易照云迫層踏窓弱討聖典剣症祖築"
            L"納勤昔脳便適弥融航快浜郷翌旧吹惑柳拠奉筆壊換益群句属候爆功捜帝賀魚堀油怖叫伸創辞泣憶幹否露矢承雲握練儀紹聴包庁測占混倍乳襲借徴荒詰飯栄床丁憲則禁順駅閣昼枚救厚皮陰繰那冬輸操智騒己濃魔遅簡撮携姉隣孫丈煙"
            L"黄曜宣徒届遣訴絡茂採釣批誘核哲豪傾締欠鹿就迷滅仰瀬洗互鼻致伏宝也杉患審才延律希避吾揺甘湾浦沈至販裕更盟欧執崩鬼酸砂尊拡紅漢複歯泊銃荷維盗枝縄詩廃充依鏡幼仮吐慣請晩眺沖躍威勇屈勘徹斎謝昇艦寿催舎仁菜季衝"
            L"液箱到券脚虎祭潮袋穴怪仙燃輝緊頂唇杯忍毒狂札牛奪診竜脇債鈴僧卒掲伯副皆敬熊針妹拝浴浪梅悩看俊汚摘灯項霊坊垣慢扱渉招涙如停寒了縮詳旦汗恥慮雅砲謀懐愚郵舌駄奴幅豆童又銭抑侍疲虫宙埋範舟棒貨龍肌臓塩潜酔呂還"
            L"丹亜亀沼巡頰均湖臭慶距釈侵僚貧損悟猫隆裂尋貸旗羅揮辛票乃稲胞双懸稿塚盤災曹尽嫁繁即軒績帳飾沿獲伴唐狭干添剤姓誤魅契掘邪挑免爵択籍珍廊析訓預輩敏署鶴虚努往趣烈索匂漁摩菊緑滑沙裸孝綱邸弘勉邦揚卓騎墓姫畳孔"
            L"耐須臨献脈咲貿芝踊唱封亭誕貫兆偽奮桜熟柱排透棄駐削奏幻祝麗逮誠炎炭椅寛斉穂柔幾兼飼促雇乾尚彩鋭暖俗較傍坐肝畑濡笠峰氷抵恩吞誇網隅渋冊賛糸魂牧控紛嬉募薩昌戒没既脅靖征覆郡丘佳叔託哀肥朗柴慎悠眉拒概顧硬腐"
            L"塗挨孤拶憎却泥賊荘宏匠悔朋獄滞脂粉遇淡浩購併崇唯劉垂詞岐俳筒斜嬢陥掃償鑑勧葬焦丞剛膨廷紫銘鎌菌稼偶譲随猛李遂冒泰翼忙凄序扉是寸賃偵澄殊緩頑塔賢拾紋撫麦庄溜糖煮芳刷惨歓嶋晋虐旨凝圏卵鷹拭涯貞堅倫揃壇呉這"
            L"械暇皿辰貌塞噴婆岳祈嘉蹴膳尺桂罰漏灰朱召覧摑漂汁溶寂嘆泳禅浄其翔酷喋刃漫磨霧暑粒喫棚袖壮旬机彫靴庵需偉鎖貯匹縦粧綿慌穏贈枠謎誉逸駒凍惜措瓶晶琴摂拍稽礎遭帽掌鍋弓克据胆跳縛祐鎮雷涼頁恨顕殖寧湧棋淳巧綾浸"
            L"秒桃隔班甚妊祉獣疾塾潟湿幡撲塊槍絞履苗芋冗陶励陳篠藝陀喧猿於葛傘蒸禄啓劣撤殴盾貰衰栗滝慰馴蛇梨癖潤菓鉢洲戯腸偏駕巣宴耕炉棟洞狩陛鴨磁潔膜乏祥曾淵舗抽桐駿睡括貢犠粗卑貼蘭牲帆挿翻胡羊枕錯謙鉱珠蓄儲伍拓鼓"
            L"膚粋尉胃后粘披徐悦堪挟冠郊愉蘇此狼尿蝶誓憂蔦繫簿糧架筈芽軸輔蓋銅圭盆紗凶妃哉庶秩裾幽凡猪惚漠拙郁嘩恒暦峠篇宰蒼蛮窮擦爪稚夷辱嵐憤癒疎溢彰蓮肺傑拘頻緯妖豚或藍矛鍛繊縫把楼捉漬紳飽宛閥旋坪菩崖幌鶏鈍峡溝逢"
            L"朴軌瓦喪墨疫貝鮎遍梁堺濁缶扇枯拳乙酵辻馳堤宋阻桑雀虜綺牡恭鐘喰剰慈径培擁郭尖砕汰勃翁絹譜陵痴笛昧訟駈漱肪塀蒲碁惣敢塁洛滴暁胴菱謡秦紐卿飢麿欄艶菅怠恰欺弦泡晃敦伐餅寮昂符厄奔昆亮噌椎懇唄渦襟吟覇衡畜呈隙"
            L"娠循懲錦棲叡猟幣附箇梶醜軟箸濯戚喚窺紺某鋼褒魯蒙惹赴媒隈遮窯侯隻茎壌蜜尼肢赦酬戴詠斗宜殻墳炊碑柏伺瘦但奨践滋毅儒沸薦磯曇栽刈閑錠扶妥妨苑萩詣胎窟巾蜂忌賑漕烏粛囚諏鉛肯桶燥搭諭辿阜喝享騰嗣鵜巳勅襖篤孟勲"
            L"埼堵曖只燈詐國笹綴稀焚岬曽條霞暫播讃雰爽盃燭肖瞳詮倭諾零柿芯淀莫詫董訂蔭捧楚汲蕎汽薫竿隷碗鳩俵傳窪遷枢肘麓兎帥漆酌庇頓賠渇慕叶裡蠟婿妄慨匿寅渓眸穿眞聡遥侮髄穀勿瑞蟹鳳薪轄臥洪牙迅該逐杏釘楠廣墜餓淋楊芥"
            L"錬桟琉樺賄盲鯨佑侶荻迦袴艇杜俣堕忽旭槽憩僅閲獅鞘柵禰畔睦蝦唆悼俱茜吏穫酢玲砦函苔賜腎瑠琶伽帖瓜簞簾嶺羨搬琵剖畿宵拐峯醸闇猶鷲絆諮畏泌槌轟愁橘冴薙逝稔朽硫厨瞭殆甥擬仔叙弊累煩裳輿梯藻蚊牽且蹟鋳蔽茨雛汝棺"
            L"註鷗舵裟吻曳隼斐芭藁硝晒舶租摺倣謹抹虹捻娯箕臼鞄撒槻鋒凌蛍蕉芦窒湊韓桁玩冶袈栓蔣鯉閃掠寡櫓鞍砥畝淑嫡迂屯糾凰纏櫻翠而檜鎧秤莉肴衿陪雌峨舷昏葵葡允筑霜殉紡榊梓庸套些韻繕搾椿燕樋刹岸嵯堆禍磐祇檀茅雁煎姻杭"
            L"閤蟬斑冥遙粥抄拷遜旺蔓壕准耶勾廉亨捲爾屑榎螺礁萬縞壱鴻畠樫升挽芙灘卸耗倖兜謁璃坑擢串鳶黎稜弔惟賓塡卯叢廟巌丑耽俄痢煌嚇濫萄俸伎湛梢灼蒔椀柑慧巴按暢甫粟邑凸顚鯛瞥坦詔繡禱凜晦圃凹錆凱櫛俠跨鍬諒煉胤罷曝巷"
            L"漸汐栖掬訣牟峻纂撰冨賦杵弧歎糊佛褐弛綸欽來堰碧憧魁瑛汎劫斯謂欣玖沫捷姪斥彬瀕傭楓亦厘迄倦紘矯犀萌毘窃遼颯戟嵩蓬瑚焔喬卜哨茉遵萱醬逗惰禎蕃蚕膏鵬乎錫倹宥款嘗斡亘沌竪笙悉槙肋沓渚尤檎絢憾怜茄戊佃蕾馨絃肇蒐"
            L"偲楕湘媛凪勁托奄宕蕪錐綜燦珂蓑捺已衷圓亥桔迭渥蕗彗柚窄箔熙嘱應壬脹煤鼎鋸墾瓢蓉堯誼彌與團澪饗埴脩惠硯廿椋汀疋惇碩琳槇笈釧匡鰯姥榛逓釉皓耀壽劾鞠凧恢雫毬莞菖酪匁禮萊灸奎德椰塑痘朕娩麒珈諺稟竺牒紬緋縣虞諦"
            L"徽鷺丙閏榮琥爲櫂斤杷祁穣芹寬橙庚柾珀梧朔弐琢豹皐徠竣枇苺桧彪謄繭璽勺錘銑頒"
            ;

        std::map<mchar_t, size_t> fullPopularCharMap;

        std::map<mchar_t, size_t> freqPopularCharMap;

#define FREQ_POPULAR_CHAR_NUM 500
#define MAX_SIZE_T  static_cast<size_t>(-1)

        void gatherPopularChars() {
            if (fullPopularCharMap.empty()) {
                const wchar_t* p = popularChars;
                size_t idx = 0;
                while (*p) {
                    wchar_t ch = *p++;
                    if (fullPopularCharMap.find(ch) == fullPopularCharMap.end()) fullPopularCharMap[ch] = idx;
                    if (idx < FREQ_POPULAR_CHAR_NUM) {
                        if (freqPopularCharMap.find(ch) == freqPopularCharMap.end()) freqPopularCharMap[ch] = idx;
                    }
                    ++idx;
                }
            }
        }

        inline size_t popularIndex(mchar_t ch) {
            if (fullPopularCharMap.empty()) { gatherPopularChars(); }
            auto pair = fullPopularCharMap.find(ch);
            return pair == fullPopularCharMap.end() ? MAX_SIZE_T : pair->second;
        }

        inline bool isPopular(mchar_t ch) {
            return popularIndex(ch) != MAX_SIZE_T;
        }

        inline size_t freqPopularIndex(mchar_t ch) {
            if (freqPopularCharMap.empty()) { gatherPopularChars(); }
            auto pair = freqPopularCharMap.find(ch);
            return pair == freqPopularCharMap.end() ? -1 : pair->second;
        }

        inline bool isFreqPopular(mchar_t ch) {
            return utils::is_punct(ch) || utils::is_hiragana(ch) || utils::is_katakana(ch) || freqPopularIndex(ch) != MAX_SIZE_T;
        }

        inline bool isEasyOrFreqPopular(mchar_t ch) {
            return EASY_CHARS->IsEasyChar(ch) || isFreqPopular(ch);
        }

        inline bool isStrokableOrNumeral(mchar_t ch) { return StrokeTableNode::IsStrokable(ch) || (ch < 0xffff && (is_numeral((wchar_t)ch) || is_wide_numeral((wchar_t)ch))); }

#define MINIMUM_BODIES 0
#define MINIMAL_BODIES 1
#define FULL_BODIES 2

        void gatherPopularBodies(mchar_t ch, std::vector<wchar_t>& bodies, int level) {
            std::set<wchar_t> usedChars;
            std::vector<std::pair<wchar_t, size_t>> easyChars;
            std::vector<std::pair<wchar_t, size_t>> popChars;
            std::vector<std::pair<wchar_t, size_t>> otherChars;

            auto classifyChar = [this, &easyChars, &popChars, &otherChars](mchar_t c) {
                if (isStrokableOrNumeral(c)) {
                    size_t idx = popularIndex(c);
                    std::pair<wchar_t, size_t> pair((wchar_t)c, idx);
                    if (EASY_CHARS->IsEasyChar(c)) {
                        easyChars.push_back(pair);
                    } else if (idx != MAX_SIZE_T) {
                        popChars.push_back(pair);
                    } else if (c >= 0x100) {
                        // 半角は除く
                        otherChars.push_back(pair);
                    }
                }
            };
            // 自身
            if (isStrokableOrNumeral(ch)) {
                if (level > MINIMUM_BODIES || isEasyOrFreqPopular(ch)) {
                    bodies.push_back((wchar_t)ch);
                    usedChars.insert((wchar_t)ch);
                }
            }
            // 等値文字
            for (auto ec : findEquiv(ch)) {
                if (isStrokableOrNumeral(ec)) {
                    if (level > MINIMUM_BODIES || isEasyOrFreqPopular(ch)) {
                        bodies.push_back((wchar_t)ec);
                        usedChars.insert(ec);
                    }
                }
            }
            if (level == MINIMUM_BODIES || (level == MINIMAL_BODIES && !usedChars.empty())) return;

            classifyChar(ch);
            for (auto c : getBodies(ch)) classifyChar(c);

            auto sortAndAppend = [this, &bodies, &usedChars, level](std::vector<std::pair<wchar_t, size_t>>& pairVec) {
                if (!pairVec.empty()) {
                    std::sort(pairVec.begin(), pairVec.end(), [](const auto& p, const auto& q) { return p.second < q.second; });
                    for (const auto& p : pairVec) {
                        wchar_t c = p.first;
                        if (usedChars.find(c) == usedChars.end() && isStrokableOrNumeral(c)) {
                            bodies.push_back(c);
                            usedChars.insert(c);
                        }
                        if (level < FULL_BODIES) return true;
                    }
                }
                return false;
            };
            if (sortAndAppend(easyChars)) return;
            if (sortAndAppend(popChars)) return;
            if (sortAndAppend(otherChars)) return;
        }

        std::vector<wchar_t> gatherBodies(mchar_t x) {
            std::vector<wchar_t> result;
            gatherPopularBodies(x, result, FULL_BODIES);
            return result;
        }

        // 指定文字を部首合成できる文字の組合せを集める
        void gatherBushuCompPartsCandidate(parts_t parts, std::vector<wchar_t>& partsA, std::vector<wchar_t>& partsB, int level) {
            if (!parts.empty()) {
                gatherPopularBodies(parts.a(), partsA, level);
                gatherPopularBodies(parts.b(), partsB, level);
            }
        }

    public:
        //仮想鍵盤に部首合成ヘルプの情報を設定する
        bool CopyBushuCompHelpToVkbFaces(mchar_t ch, wchar_t* faces, size_t kbLen, size_t kbNum, bool bSetAssoc) override {
            // クリアしておく
            size_t numFaces = kbLen * kbNum;
            for (size_t i = 0; i < numFaces; i += kbLen) {
                faces[i] = faces[i + 1] = 0;
            }

            auto parts = findParts(ch);
            if (parts.empty()) {
                // 引き算
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

            if (bSetAssoc) {
                wchar_t a = parts.a();
                wchar_t b = parts.b();
                setAssocTarget(a, b, ch);
            }

            size_t leftIn = (kbNum / 2) - 1;
            size_t rightIn = leftIn + 1;
            std::vector<wchar_t> partsA;
            std::vector<wchar_t> partsB;
            gatherBushuCompPartsCandidate(findParts(ch), partsA, partsB, FULL_BODIES);

            if (!partsA.empty() && !partsB.empty()) {
                // for partsA
                size_t pos = kbLen * leftIn;
                for (auto c : partsA) {
                    faces[pos] = c; faces[pos + 1] = 0;
                    if (pos == 0) break;
                    pos -= kbLen;
                }
                for (; pos > 0; pos -= kbLen) faces[pos] = 0;
                // for partsB
                pos = kbLen * rightIn;
                for (auto c : partsB) {
                    faces[pos] = c; faces[pos + 1] = 0;
                    if (pos == numFaces) break;
                    pos += kbLen;
                }
                for (; pos < numFaces; pos += kbLen) faces[pos] = 0;
            }
            return true;
        }

    private:
    public:
        //後置部首合成定義を書き出す
        void ExportPostfixBushuCompDefs(utils::OfstreamWriter& writer, StringRef postfix) override {
            LOG_INFO(_T("ENTER: writer.count={}"), writer.count());

            //// ストローク可能文字
            //std::set<mchar_t> strokableChars = StrokeTableNode::GatherStrokeChars();

            std::map<String, String> revMap;
            std::set<String> doneSet;


            wchar_t buf[3] = { 0, 0, 0 };
            wchar_t rev[3] = { 0, 0, 0 };

#define TAB_TAB  "\t\t"

            size_t MAX_LINES = 7500;        // TAB_TAB を "\t" にして「次の入力」でなく「出力」にしてやっても、MAXはせいぜい8000以下だった

            auto writeComp = [this, &writer, MAX_LINES, postfix, &doneSet](mchar_t c, const wchar_t* comp) {
                if (writer.count() < MAX_LINES) {
                    String cs = to_wstr(c);
                    if (/*isPopular(c) &&*/ doneSet.find(comp) == doneSet.end()) {
                        writer.writeLine(utils::utf8_encode(
                            std::format(_T("{}{}" TAB_TAB "{}"),
                                comp,
                                postfix,
                                cs)));
                        doneSet.insert(comp);
                    }
                }
                return writer.count() < MAX_LINES;
            };

            auto WRITE_CANDS = [this, &writeComp, &revMap, &buf, &rev](mchar_t c, const std::vector<wchar_t>& partsA, const std::vector<wchar_t>& partsB) {
                size_t ia = 0;
                size_t ib = 0;
                for (wchar_t a : partsA) {
                    for (wchar_t b : partsB) {
                        if (c != a && c != b) {
                            buf[0] = a;
                            buf[1] = b;
                            if (!writeComp(c, buf)) break;
                            if (isPopular(c) && !is_numeral(buf[1])) {
                                rev[0] = b;
                                rev[1] = a;
                                revMap[rev] = to_wstr(c);
                            }
                        }
                        if (++ib >= 2) break;
                    }
                    if (++ia >= 3) break;
                }
            };

            // 自動部首合成組み合わせの出力
            if (SETTINGS->autoBushuCompMinCount > 0) {
                for (const auto& pair : autoBushuDict) {
                    if (writer.count() >= MAX_LINES) break;
                    if (pair.first.size() == 2 && pair.second.target != '-' && pair.second.count >= SETTINGS->autoBushuCompMinCount) {
                        mchar_t a = pair.first[0];
                        mchar_t b = pair.first[1];
                        if (isEasyOrFreqPopular(a) && isEasyOrFreqPopular(b)) {
                            writer.writeLine(utils::utf8_encode(
                                std::format(_T("{}" TAB_TAB "{}"),
                                    to_wstr(a) + VkbTableMaker::ConvCharToStrokeString(b),
                                    to_wstr(pair.second.target))));
                            doneSet.insert(to_wstr(pair.first));
                        }
                    }
                }
            }

            // 部首合成組み合わせの出力
            for (const auto& pair : compMap) {
                parts_t parts = pair.first;
                mchar_t c = pair.second;
                if (isStrokableOrNumeral(c) && isEasyOrFreqPopular(c)) continue;       // ストローク可能で頻度が高ければ対象外

                if (!isPopular(c)) continue;                            // 常用・人名でなければ対象外

                bool bAdded = false;

                // 順方向の足し算
                std::vector<wchar_t> partsA;
                std::vector<wchar_t> partsB;

                // 数字は2番目の部品にする
                if (is_numeral(parts.a()) || is_wide_numeral(parts.a())) {
                    parts.exchange();
                }
                // 数字は半角にする
                if (is_wide_numeral(parts.b())) {
                    parts.set(parts.a(), make_halfwide_nummeral(parts.b()));
                }

                // 頻度の高い部品の組み合わせか
                gatherBushuCompPartsCandidate(parts, partsA, partsB, MINIMUM_BODIES);
                WRITE_CANDS(c, partsA, partsB);

                if (bAdded) continue;
#if 0
                // 引き算
                buf[0] = (wchar_t)c;
                buf[1] = a;
                tmpMap[buf] = to_wstr(b);
                buf[1] = b;
                tmpMap[buf] = to_wstr(a);
#endif
                // 頻度の低い部品の組み合わせか
                partsA.clear();
                partsB.clear();
                gatherBushuCompPartsCandidate(parts, partsA, partsB, MINIMAL_BODIES);
                WRITE_CANDS(c, partsA, partsB);

                // 下位部品
                wchar_t a = parts.a();
                wchar_t b = parts.b();
                if (isPopular(c) && (!isEasyOrFreqPopular(a) || !isEasyOrFreqPopular(b))) {
                    auto ADD_COMPO_BY_SUBPARTS = [this, c, &buf, &rev, &writeComp, &revMap](wchar_t x, wchar_t y, size_t px, size_t py) {
                        if (c != y && isEasyOrFreqPopular(y)) {
                            buf[py] = y;
                            rev[px] = y;
                            auto pa = findParts(x);
                            wchar_t _a = pa.a();
                            wchar_t _b = pa.b();
                            if (_a && c != _a && isStrokableOrNumeral(_a) && isEasyOrFreqPopular(_a) && isComposableParts(_a)) {
                                buf[px] = _a;
                                if (writeComp(c, buf)) return;
                                rev[py] = _a;
                                revMap[rev] = to_wstr(c);
                            }
                            if (_b && c != _b && isStrokableOrNumeral(_b) && isEasyOrFreqPopular(_b) && isComposableParts(_b)) {
                                buf[px] = _b;
                                if (!writeComp(c, buf)) return;
                                rev[py] = _b;
                                revMap[rev] = to_wstr(c);
                            }
                        }
                    };
                    ADD_COMPO_BY_SUBPARTS(a, b, 0, 1);
                    ADD_COMPO_BY_SUBPARTS(b, a, 1, 0);
                }
            }

            // 逆順
            LOG_INFO(_T("Reverse: writer.count={}, revMap.size={}"), writer.count(), revMap.size());
            for (const auto& pair : revMap) {
                if (doneSet.find(pair.first) == doneSet.end()) {
                    writer.writeLine(utils::utf8_encode(
                        std::format(_T("{}{}" TAB_TAB "{}"),
                            pair.first,
                            postfix,
                            pair.second)));
                    if (writer.count() >= MAX_LINES) break;
                }
            }

            LOG_INFO(_T("LEAVE: writer.count={}"), writer.count());
        }

    private:
#if 0
        inline void writeComp(utils::OfstreamWriter& writer, const wchar_t* postfix, mchar_t c, const wchar_t* comp, std::map<String, String>& tmpMap) {
            String cs = to_wstr(c);
            if (isPopular(c)) {
                writer.writeLine(utils::utf8_encode(
                    utils::format(_T("{}{}\t\t{}"),
                        comp,
                        postfix,
                        cs)));
            } else {
                tmpMap[comp] = cs;
            }
        }
#endif
    };
    DEFINE_CLASS_LOGGER(BushuDicImpl);

} // namespace

// -------------------------------------------------------------------
DEFINE_CLASS_LOGGER(BushuDic);

std::unique_ptr<BushuDic> BushuDic::_singleton;

BushuDic* BushuDic::Singleton() {
    return _singleton.get();
}

namespace {
    DEFINE_LOCAL_LOGGER(BushuDic);

    bool readBushuFile(StringRef path, BushuDicImpl* pDic) {
        LOG_INFO(_T("open bushu file: {}"), path);
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            pDic->ReadFile(reader.getAllLines());
            LOG_INFO(_T("close bushu file: {}"), path);
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_WARN(_T("Can't read bushu file: {}"), path);
            ERROR_HANDLER->Warn(std::format(_T("部首合成入力辞書ファイル({})が開けません"), path));
            return false;
        }
        return true;
    }

    bool writeBushuFile(StringRef path, BushuDicImpl* pDic) {
        LOG_INFO(_T("write bushu file: {}, dirty={}"), path, pDic && pDic->IsDirty());
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

    bool readAutoBushuFile(StringRef path, BushuDicImpl* pDic) {
        LOG_INFO(_T("open auto bushu file: {}"), path);
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            pDic->ReadAutoDicFile(reader.getAllLines());
            LOG_INFO(_T("close bushu file: {}"), path);
        }
        return true;
    }

    bool writeAutoBushuFile(StringRef path, BushuDicImpl* pDic) {
        LOG_INFO(_T("write auto bushu file: {}, dirty={}"), path, pDic && pDic->IsAutoDirty());
        if (!path.empty() && pDic) {
            if (pDic->IsAutoDirty()) {
                if (utils::moveFileToBackDirWithRotation(path, SETTINGS->backFileRotationGeneration)) {
                    utils::OfstreamWriter writer(path);
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
int BushuDic::CreateBushuDic() {
    LOG_INFO(_T("ENTER"));

    // 部首合成辞書の読み込み(ファイルが指定されていなくても、辞書は構築する)
    // 部首合成辞書ファイル名
    auto bushuFile = SETTINGS->bushuFile;
    auto autoBushuFile = SETTINGS->autoBushuFile;
    LOG_DEBUGH(_T("bushuFile={}, autoBushuFile={}"), bushuFile, autoBushuFile);

    if (_singleton != 0) {
        LOG_INFO(_T("already created: bushu file: {}"), bushuFile);
        return 0;
    }

    //if (bushuFile.empty()) {
    //    ERROR_HANDLER->Warn(_T("「bushu=(ファイル名)」の設定がまちがっているようです"));
    //}

    int result = 0;

    auto pImpl = new BushuDicImpl();
    if ((bushuFile.empty() || readBushuFile(bushuFile, pImpl)) && (autoBushuFile.empty() || readAutoBushuFile(autoBushuFile, pImpl))) {
        _singleton.reset(pImpl);
    } else {
        result = -1;
    }

    LOG_INFO(_T("LEAVE: result={}"), result);
    return result;
}

// 部首合成辞書を読み込む
void BushuDic::ReadBushuDic(StringRef path) {
    LOG_INFO(_T("CALLED: path={}"), path);
    if (!path.empty() && BUSHU_DIC) {
        readBushuFile(path, (BushuDicImpl*)BUSHU_DIC);
    }
}

// 部首合成辞書ファイルに書き込む
void BushuDic::WriteBushuDic(StringRef path) {
    LOG_INFO(_T("CALLED: path={}"), path);
    writeBushuFile(path, (BushuDicImpl*)BUSHU_DIC);
}

// 自動部首合成辞書を読み込む
void BushuDic::ReadAutoBushuDic(StringRef path) {
    LOG_INFO(_T("CALLED: path={}"), path);
    if (!path.empty() && BUSHU_DIC) {
        readAutoBushuFile(path, (BushuDicImpl*)BUSHU_DIC);
    }
}

// 自動部首合成辞書ファイルに書き込む
void BushuDic::WriteAutoBushuDic(StringRef path) {
    LOG_INFO(_T("CALLED: path={}"), path);
    writeAutoBushuFile(path, (BushuDicImpl*)BUSHU_DIC);
}

// 部首合成辞書ファイルに書き込む
void BushuDic::WriteBushuDic() {
    WriteBushuDic(SETTINGS->bushuFile);
}

// 部首合成辞書ファイルに書き込む
void BushuDic::WriteAutoBushuDic() {
    WriteAutoBushuDic(SETTINGS->autoBushuFile);
}

