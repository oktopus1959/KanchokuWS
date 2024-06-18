#include "Logger.h"
#include "string_type.h"
#include "string_utils.h"
#include "misc_utils.h"
#include "file_utils.h"
#include "path_utils.h"

#include "Constants.h"
#include "Settings.h"
#include "ErrorHandler.h"
#include "OutputStack.h"
#include "MazegakiDic.h"
#include "EasyChars.h"
#include "History/HistoryDic.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughMazegakiDic)

#define DONT_READ_MAZEDIC_ON_DEBUG 1

#if 0
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define _LOG_DEBUGH LOG_INFO
#define _LOG_DEBUG LOG_INFO
#define _LOG_DEBUGH_COND LOG_INFO_COND
#define _LOG_DEBUG_COND LOG_INFO_COND
#endif

#define _WCHAR(ws)  (_T(ws)[0])

// 語幹のみでもOK
#define STEM_OK (wchar_t*)1

// 孤立OK 
#define ISOLATABLE (wchar_t*)2

// 任意の語尾OK
#define ANY_OK (wchar_t*)3

// 漢字の語尾OK
#define KANJI_OK (wchar_t*)4

// 条件付きで任意OK
#define COND_ANY_OK (wchar_t*)5

// 特殊の終わり
#define INF_SPECIAL_END (wchar_t*)9

// 無活用の最大語尾長
//#define NO_IFX_MAX_GOBI 2

namespace {
    // -------------------------------------------------------------------
    // 活用語尾
    // 一段活用
    wchar_t const * IFX_RU_1[] = { STEM_OK, _T("る"), _T("れ"), _T("よ"), _T("ざ"), _T("ず"), _T("な"), _T("にゃ"), _T("ね"), _T("た"), _T("て"), _T("ま"), _T("ら"), 0 };
    // 五段活用「く」(書く)
    wchar_t const * IFX_KU_5[] = { _T("かれた"), _T("かれて"), _T("かれ"), _T("か"), _T("き"), _T("く"), _T("け"), _T("こ"), _T("いた"), _T("いちゃ"), _T("いて"), /*_T("い"), */ 0 };
    // 五段活用「ぐ」(漕ぐ)
    wchar_t const * IFX_GU_5[] = { _T("がれた"), _T("がれて"), _T("がれ"), _T("が"), _T("ぎ"), _T("ぐ"), _T("げ"), _T("ご"), _T("いだ"), _T("いじゃ"), _T("いで"), /* _T("い"), */ 0 };
    // 五段活用「す」(話す)
    wchar_t const * IFX_SU_5[] = { _T("された"), _T("されて"), _T("され"), _T("さ"), _T("した"), _T("して"), _T("し"), _T("す"), _T("せ"), _T("そ"), 0 };
    // 五段活用「つ」(立つ)
    wchar_t const * IFX_TU_5[] = { _T("たれた"), _T("たれて"), _T("たれ"), _T("た"), _T("ち"), _T("つ"), _T("て"), _T("と"), _T("った"), _T("っちゃ"), _T("って"), /* _T("っ"), */ 0 };
    // 五段活用「ぬ」(死ぬ)
    wchar_t const * IFX_NU_5[] = { _T("なれた"), _T("なれて"), _T("なれ"), _T("な"), _T("に"), _T("ぬ"), _T("ね"), _T("の"), _T("んだ"), _T("んじゃ"), _T("んで"), /* _T("ん"), */ 0 };
    // 五段活用「ぶ」(飛ぶ)
    wchar_t const * IFX_BU_5[] = { _T("ばれた"), _T("ばれて"), _T("ばれ"), _T("ば"), _T("び"), _T("ぶ"), _T("べ"), _T("ぼ"), _T("んだ"), _T("んじゃ"), _T("んで"), /* _T("ん"), */ 0 };
    // 五段活用「む」(生む)
    wchar_t const * IFX_MU_5[] = { _T("まれた"), _T("まれて"), _T("まれ"), _T("ま"), _T("み"), _T("む"), _T("め"), _T("も"), _T("んだ"), _T("んじゃ"), _T("んで"), /* _T("ん"), */ 0 };
    // 五段活用「る」(振る)
    wchar_t const * IFX_RU_5[] = { _T("られた"), _T("られて"), _T("られ"), _T("ら"), _T("り"), _T("る"), _T("れ"), _T("ろ"), _T("った"), _T("っちゃ"), _T("って"), /* _T("っ"), */ 0 };
    // 五段活用「う」(会う)
    wchar_t const * IFX_WU_5[] = { _T("われた"), _T("われて"), _T("われ"), _T("わ"), _T("い"), _T("う"), _T("え"), _T("お"), _T("った"), _T("っちゃ"), _T("って"), /* _T("っ"), */ 0 };
    // サ変活用「する」(開発する)(達する、愛するは、五段として登録する)
    wchar_t const * IFX_SURU[] = { STEM_OK, KANJI_OK, \
        _T("された"), _T("されて"), _T("され"), _T("さ"), _T("した"), _T("して"), _T("しない"), _T("しな"), _T("し"), \
        _T("する"), _T("すれ"), _T("せ"), _T("を"),_T("、"), _T("。"), COND_ANY_OK, 0 };
    // ザ変活用「ずる」(信ずる)
    wchar_t const * IFX_ZURU[] = { _T("じた"), _T("じて"), _T("じない"), _T("じな"), _T("じられた"), _T("じられて"), _T("じられ"), _T("じら"), _T("じ"), _T("ずる"), _T("ずれ"), _T("ぜ"), 0 };
    // 形容詞「い」(美しい)
    wchar_t const * IFX_KYI[] = { _T("い"), _T("かった"), _T("か"), _T("き"), _T("く"), _T("けれ"), _T("けり"), _T("さ"), 0 };
    // 形容動詞「な」(静かな)
    wchar_t const * IFX_KDNA[] = { STEM_OK, _T("な"), _T("に"), _T("だ"), _T("で"), _T("じ"), _T("さ"), _T("、"), _T("。"), 0 };
    // 形容動詞「の」(本当の)
    wchar_t const * IFX_KDNO[] = { STEM_OK, _T("な"), _T("の"), _T("に"), _T("だ"), _T("で"), _T("じ"), _T("さ"), _T("、"), _T("。"), 0 };
    // 副詞
    wchar_t const * IFX_ADV[] = { STEM_OK, ISOLATABLE, ANY_OK, 0 };
    // 無活用
    //wchar_t const * IFX_NONE[] = { STEM_OK, 0 };
    wchar_t const * IFX_NONE[] = { STEM_OK, KANJI_OK,
        _T("が"), _T("だ"), _T("で"), _T("と"), _T("な"), _T("に"), _T("の"), _T("は"), _T("へ"), _T("も"), _T("を"),
        _T("から"), _T("こそ"), _T("ごと"), _T("さえ"), _T("じゃ"), _T("すら"),_T("まで"),_T("たち"),_T("や"),_T("よ"),_T("ゆえ"),
        _T("、"), _T("。"), COND_ANY_OK, 0 };

    inline int find_gobi(const wchar_t** ifxes, int id) {
        auto ppIfx = ifxes;
        while (*ppIfx != 0) {
            if (*ppIfx == (wchar_t*)id) return 0;
            ++ppIfx;
        }
        return -1;
    }

    // 後接可能な語尾を探し、その長さを返す
    inline int find_gobi(const wchar_t** ifxes, size_t xferLen, const mchar_t* pms) {
        auto ppIfx = ifxes;
        while (*ppIfx != 0) {
            wchar_t const* pIfx = *ppIfx++;
            if (pIfx == ANY_OK) return 0;
            if (pIfx == KANJI_OK && xferLen >= 2 && SETTINGS->mazeNoIfxConnectKanji && utils::is_kanji(pms[0])) return 0;  // 漢字後接もOKの場合
            if (pIfx == COND_ANY_OK && xferLen >= 2 && SETTINGS->mazeNoIfxConnectAny) return 0;  // 条件付きで任意後接もOKの場合
            if (pIfx <= INF_SPECIAL_END) continue;
            int i = 0;
            while (pIfx[i] != 0 && pIfx[i] == pms[i]) ++i;
            if (i > 0 && pIfx[i] == 0) return i;
        }
        //if (ifxes == IFX_NONE) {
        //    // 無活用の場合はひらがな以外を後接できる
        //    if (!utils::is_hiragana(mc)) return true;
        //}
        return -1;
    }

    // 活用型->活用語尾のマップ
    std::map<String, const wchar_t**> gobiMap = {
        { _T("一"), IFX_RU_1 },
        { _T("る:1"), IFX_RU_1 },
        { _T("く"), IFX_KU_5 },
        { _T("ぐ"), IFX_GU_5 },
        { _T("す"), IFX_SU_5 },
        { _T("つ"), IFX_TU_5 },
        { _T("ぬ"), IFX_NU_5 },
        { _T("ぶ"), IFX_BU_5 },
        { _T("む"), IFX_MU_5 },
        { _T("る"), IFX_RU_5 },
        { _T("る:5"), IFX_RU_5 },
        { _T("う"), IFX_WU_5 },
        { _T("する"), IFX_SURU },
        { _T("ずる"), IFX_ZURU },
        { _T("い"), IFX_KYI },
        { _T("な"), IFX_KDNA },
        { _T("の"), IFX_KDNO },
        { _T("副"), IFX_ADV },
    };

    // 上一段、下一段
    //std::wregex IchiDan(_T("[いえきけぎげしせじぜちてにねびべみめりれ]$"));
    std::set<wchar_t> IchiDan = {
        _WCHAR("い"), _WCHAR("え"), _WCHAR("き"), _WCHAR("け"), _WCHAR("ぎ"), _WCHAR("げ"), _WCHAR("し"), _WCHAR("せ"), _WCHAR("じ"), _WCHAR("ぜ"),
        _WCHAR("ち"), _WCHAR("て"), _WCHAR("に"), _WCHAR("ね"), _WCHAR("び"), _WCHAR("べ"), _WCHAR("み"), _WCHAR("め"), _WCHAR("り"), _WCHAR("れ"), };

    // イ段・エ段でも五段活用するもの
    std::set<String> GoDan = {
        _T("い入"), _T("い要"), _T("い煎"), _T("い陥"), _T("い参"), _T("え帰"), _T("え返"), _T("え覆"), _T("え翻"), _T("え蘇"), _T("え甦"),
        _T("き切"), _T("き斬"), _T("き伐"), _T("け嘲"), _T("け蹴"), _T("け耽"), 
        _T("ぎ切"), _T("ぎ脂"), _T("ぎ限"), _T("ぎ遮"), _T("ぎ契"), _T("ぎ握"), _T("げ茂"), _T("げ繁"), _T("げ陰"), 
        _T("し知"), _T("し軋"), _T("し走"), _T("し誹"), _T("し罵"), _T("せ競"), _T("せ焦"), _T("せ臥"),
        _T("じ詰"), _T("じ捻"), _T("じ捩"), _T("じ交"), _T("じ混"),
        _T("ち散"), _T("て照"),
        _T("ね阿"), _T("ね練"), _T("ね捻"),
        _T("び縊"), _T("べ喋"), _T("べ滑"), _T("べ侍"), 
        _T("め湿"), 
    };

    // '*' か
    inline bool is_wild_star(mchar_t mch) { return mch == '*'; }

    // 読みの文字 (ひらがな or '*')
    inline bool is_yomi_char(mchar_t mch) { return is_wild_star(mch) || utils::is_hiragana(mch); }

    // 変換形の文字か(ひらがな以外かつ'*'以外)
    inline bool is_xfer_char(mchar_t mch) { return !is_yomi_char(mch); }

    // 変換形の文字または '?' か
    inline bool is_xfer_char_or_ques(mchar_t mch) { return mch == '?' || is_xfer_char(mch); }

    // 変換形の文字またはワイルドカードか
    inline bool is_xfer_char_or_wildcard(mchar_t mch) { return is_wildcard(mch) || is_xfer_char(mch); }

    // -------------------------------------------------------------------
    // 活用型の段型を表す :N を削除するための正規表現
    std::wregex DanPattern(_T(":.*$"));

    // 交ぜ書き辞書のエントリ
    class MazeEntry {
    public:
        MString stem;               // 読み(語幹)(「たべ」)
        MString xfer;               // 変換形(語幹)(「食べ」)
        const wchar_t** inflexList; // 語尾リスト
        String ifxGobi;            // 活用語尾
        String origYomi;           // 元の読み形(語尾情報含む)
        bool userDic;               // ユーザ辞書由来
        bool deleted;               // 削除フラグ

    public:
        static MazeEntry* CreateEntry(StringRef yomi, const String xfer, bool bUser) {
            size_t pos = yomi.find_first_of('/');
            if (pos == String::npos) {
                // 語尾指定がない ⇒ 無活用
                return new MazeEntry{ to_mstr(yomi), to_mstr(xfer), IFX_NONE, _T("無"), yomi, bUser };
            } else {
                // 語尾指定あり
                String gobi = yomi.substr(pos + 1);
                //if (gobi == _T("る") && std::regex_match(gobi, IchiDan))
                if (pos > 0 && gobi == _T("る") && IchiDan.find(yomi[pos - 1]) != IchiDan.end()) {
                    // 語幹末尾が「イ段」または「エ段」であり、五段活用する語尾でなければ一段活用とみなす
                    String ts;
                    if (pos > 0) {
                        ts += yomi[pos - 1];
                        ts += xfer.back();
                    }
                    if (ts.empty() || GoDan.find(ts) == GoDan.end()) gobi += _T(":1");
                }
                auto iter = gobiMap.find(gobi);
                auto ifxList = iter != gobiMap.end() ? iter->second : IFX_NONE;
                return new MazeEntry{ to_mstr(yomi.substr(0, pos)), to_mstr(xfer), ifxList, gobi, yomi, bUser };
            }
        }

        // 変換形＋活用語尾の長さを返す(後でブロッカーの設定位置になる)
        size_t GetXferPlusGobiLen(const MString& resultStr) const {
            size_t xferLen = std::min(xfer.size(), resultStr.size());
            //if ((inflexList != IFX_NONE && inflexList != IFX_ADV && inflexList != IFX_SURU) && xferLen < resultStr.size()) { //}
                //// サ変以外の活用語で、語尾がある場合は、その語尾も変換形に含める
            if (inflexList != IFX_ADV && xferLen < resultStr.size()) {
                // 副詞以外で、語尾がある場合は、その語尾も変換形に含める
                int gobiLen = find_gobi(inflexList, xferLen, resultStr.c_str() + xferLen);
                if (gobiLen > 0) xferLen += gobiLen;
            }
            //return xferLen;
            // 末尾の残りの長さがN文字以下なら、それも語尾に含めてしまう
            if (xferLen + SETTINGS->mazeGobiLikeTailLen >= resultStr.size()) xferLen = resultStr.size();
            return xferLen;
        }

        // :N を削除した読み形を返す
        MString GetTrimmedYomi() const {
            return to_mstr(std::regex_replace(origYomi, DanPattern, _T("")));
        }
    };

    // 交ぜ書きエントリの全リストのクラス
    class MazeEntryList {
        std::vector<const MazeEntry*> list;

    public:
        MazeEntryList() { }

        ~MazeEntryList() {
            for (auto p : list) delete p;
        }

        MazeEntryList(const std::vector<const MazeEntry*>& ls) : list(ls) { }

        void Insert(const std::vector<const MazeEntry*>& ls) {
            size_t pos = 0;
            for (const auto& s : ls) {
                if (std::find(list.begin() + pos, list.end(), s) == list.end()) list.insert(list.begin() + (pos++), s);
            }
        }

        const MazeEntry* AddEntry(const MazeEntry* entry) {
            list.push_back(entry);
            return entry;
        }

        const MazeEntry* AddEntry(StringRef yomi, StringRef xfer, bool bUser = false) {
            return AddEntry(MazeEntry::CreateEntry(yomi, xfer, bUser));
        }

        const std::vector<const MazeEntry*>& GetList() const { return list; }

    }; // MazeEntryList

    // 交ぜ書きエントリの全リスト
    MazeEntryList MazeEntries;

    // ユーザー辞書エントリのリストのクラス
    class UserDicEntries {
        std::map<String, std::vector<const MazeEntry*>> userEntries;

        std::vector<const MazeEntry*> emptyList;

        bool bDirty = false;

    public:
        void ClearDirtyFlag() {
            bDirty = false;
        }

        bool IsDirty() {
            return bDirty;
        }

        void AddUserEntries(StringRef yomi, const std::vector<const MazeEntry*>& entries) {
            auto iter = userEntries.find(yomi);
            if (iter == userEntries.end()) {
                userEntries[yomi] = entries;
            } else {
                iter->second.insert(iter->second.begin(), entries.begin(), entries.end());
            }
            bDirty = true;
        }

        const std::vector<const MazeEntry*>& FindEntries(StringRef yomi) {
            auto iter = userEntries.find(yomi);
            return iter == userEntries.end() ? emptyList : iter->second;
        }

        const std::map<String, std::vector<const MazeEntry*>>& GetAllEntries() const {
            return userEntries;
        }

        void DeleteEntry(StringRef yomi, const MString& xfer) {
            for (auto p : FindEntries(yomi)) {
                if (p->xfer == xfer) {
                    ((MazeEntry*)p)->deleted = true;
                    bDirty = true;
                }
            }
        }

        void DeleteEntry(const MazeEntry* pEntry) {
            DeleteEntry(pEntry->origYomi, pEntry->xfer);
        }
    };

    // ユーザー辞書エントリのリスト(保存すべきユーザー辞書のエントリを格納する)
    UserDicEntries UserEntries;

    // 優先辞書エントリのリストのクラス
    class PrimaryDicEntries {
        DECLARE_CLASS_LOGGER;

        std::map<MString, MString> entries;

        bool bDirty = false;

    public:
        void ClearDirtyFlag() {
            bDirty = false;
        }

        bool IsDirty() {
            return bDirty;
        }

        void AddPrimaryEntry(StringRef yomi, StringRef xfer) {
            entries[to_mstr(yomi)] = to_mstr(xfer);
            bDirty = true;
        }

        bool AddPrimaryEntryIfNeeded(const MString& yomi, const MString& xfer) {
            _LOG_DEBUGH(_T("ENTER: yomi={}, xfer={}"), to_wstr(yomi), to_wstr(xfer));
            bool flag = false;
            auto iter = entries.find(yomi);
            if (iter == entries.end()) {
                entries[yomi] = xfer;
                bDirty = true;
                flag = true;
            }
            _LOG_DEBUGH(_T("LEAVE: result={}"), flag);
            return flag;
        }

        void DeletePrimaryEntry(const MString& yomi) {
            _LOG_DEBUGH(_T("CALLED: yomi={}"), to_wstr(yomi));
            entries.erase(yomi);
        }

        const MString& FindEntry(const MString& yomi) {
            auto iter = entries.find(yomi);
            LOG_DEBUG(_T("yomi={}, found={}"), to_wstr(yomi), iter == entries.end());
            return iter == entries.end() ? EMPTY_MSTR : iter->second;
        }

        const std::map<MString, MString>& GetAllEntries() const {
            return entries;
        }
    };
    DEFINE_CLASS_LOGGER(PrimaryDicEntries);

    // 優先辞書エントリのリストのクラス(保存すべき優先辞書のエントリを格納する)
    PrimaryDicEntries PrimaryEntries;

    // -------------------------------------------------------------------
    // 交ぜ書き辞書(読み、または変換形に含まれる1文字から、もとのエントリを集めたsetへのマップ)
    typedef std::map<mchar_t, std::set<const MazeEntry*>> MazeDictionary;

    // 交ぜ書き辞書の実装クラス
    class MazegakiDicImpl : public MazegakiDic {
    private:
        DECLARE_CLASS_LOGGER;

        // 交ぜ書き辞書インスタンス
        MazeDictionary mazeDic;

        // 検索されたエントリとそれから生成された出力形
        struct CandidateEntry {
            const MazeEntry* EntryPtr;
            MString yomi;               // 候補の読み
            MString yomiStem;           // 候補の読み語幹
            MString outXfer;            // 生成された変換形＋語尾
            MString output;             // 生成された出力形(変換形+語尾～入力末尾まで)
        };

        // 検索された候補のリスト
        std::vector<CandidateEntry> mazeCandidates;

        // 上記から生成された変換後文字列のリスト
        std::vector<MazeResult> mazeResults;

        std::vector<MString> emptyResult;

        template<typename Func>
        void insertEntries(const MazeEntry* pEntry, const MString& mstr, Func pred) {
            for (auto wc : mstr) {
                if (pred(wc)) {
                    auto iter = mazeDic.find(wc);
                    if (iter == mazeDic.end()) {
                        mazeDic[wc] = utils::make_one_element_set(pEntry);
                    } else {
                        iter->second.insert(pEntry);
                    }
                    _LOG_DEBUGH(_T("mazeDic.size()={}"), mazeDic.size());
                }
            }
        }

    public:
        void InsertEntry(const MazeEntry* pEntry) {
            insertEntries(pEntry, pEntry->stem, [](auto) { return true;});
            insertEntries(pEntry, pEntry->xfer, [](auto c) { return is_xfer_char(c);});  // ひらがな以外をキーとして登録する
        }

    private:
        // MazeEntry を生成して登録
        void addEntries(StringRef yomi, const std::vector<String>& list, bool bUser) {
            std::vector<const MazeEntry*> users;
            for (const auto& xfer : list) {
                const MazeEntry* pEntry = MazeEntries.AddEntry(yomi, xfer, bUser);
                InsertEntry(pEntry);
                if (pEntry->userDic) users.push_back(pEntry);
            }
            if (!users.empty()) UserEntries.AddUserEntries(yomi, users);
        }

    public:
        MazegakiDicImpl() {
        }

        // UTF8で書かれた辞書ソースを読み込む
        void ReadFile(const std::vector<String>& lines, bool bUser, bool bPrim) {
            LOG_DEBUGH(_T("ENTER: lines.size()={}, mazeDic.size()={}, MazeEntries.size()={}"), lines.size(), mazeDic.size(), MazeEntries.GetList().size());
            Reporting::Logger::SaveAndSetLevel(Reporting::Logger::LogLevelWarn);
            //Reporting::Logger::SaveAndSetLevel(Reporting::Logger::LogLevelInfoH);
            for (const auto& line : lines) {
                AddMazeDicEntry(line, bUser, bPrim);
            }
            Reporting::Logger::RestoreLevel();
            if (bUser) {
                UserEntries.ClearDirtyFlag();   // ファイルから読み込んだ場合はダーティフラグをクリアしておく
            }
            if (bPrim) {
                PrimaryEntries.ClearDirtyFlag();   // ファイルから読み込んだ場合はダーティフラグをクリアしておく
            }
            LOG_DEBUGH(_T("LEAVE: mazeDic.size()={}, MazeEntries.size()={}"), mazeDic.size(), MazeEntries.GetList().size());
        }

        // 一行の辞書ソース文字列を解析して辞書に登録する
        bool AddMazeDicEntry(StringRef line, bool bUser, bool bPrim) {
            _LOG_DEBUGH(_T("ENTER: line={}"), line);
            auto items = utils::filter_not_empty(utils::split(utils::strip(line), ' '));
            // 「見出し<空白>/?登録語/...」という形式でなければ、何もしない (登録語列の先頭は '/' でなくてもよい)
            if (items.size() != 2 || items[0].empty() || items[1].empty() /*|| items[1][0] != '/'*/) return false;

            // キー
            const auto& key = items[0];
            // 内容
            auto list = utils::filter_not_empty(utils::split(items[1], '/')); // 内容

            if (!list.empty()) {
                // 登録内容がある場合のみ登録する
                if (bPrim) {
                    PrimaryEntries.AddPrimaryEntry(key, list[0]);
                } else {
                    addEntries(key, list, bUser);
                }
                return true;
            }
            return false;
        }

    private:
        inline size_t count_head_wildcard(const MString& ms) {
            size_t len = 0;
            while (len < ms.size() && is_wildcard(ms[len])) {
                ++len;
            }
            return len;
        }

        inline size_t count_head_xfer_char_or_wildcard(const MString& ms) {
            size_t len = 0;
            while (len < ms.size() && is_xfer_char_or_wildcard(ms[len])) {
                ++len;
            }
            return len;
        }

        // 入力文字列中の部分ひらがな列または漢字列について、読みまたは変換形の中の一致する部分を探す
        // 「*」は任意長の文字列、「?」は任意の1文字とマッチする
        bool matcher(const MString& keyStem, size_t pos, size_t len, const MString& ms, size_t& mpos, bool debugFlag = false) {
            _LOG_DEBUG_COND(debugFlag, _T("ENTER: keyStem={}, pos={}, len={}, ms={}, mpos={}"), to_wstr(keyStem), pos, len, to_wstr(ms), mpos);
            if (pos + len >= keyStem.size()) {
                // 末尾のマッチングの場合
                bool tailStar = pos < keyStem.size() && keyStem.find('*', pos) != MString::npos;
                size_t tlen = len - (tailStar ? 1 : 0);
                if (mpos + tlen > ms.size()) return false;   // 読み(変換形)の文字数が足りないので不一致とする
                if (!tailStar) mpos = ms.size() - len;
                _LOG_DEBUG_COND(debugFlag, _T("tailStar={}, pos={}, tlen={}, mpos={}"), tailStar, pos, tlen, mpos);
                if (pos == 0 && mpos > 0) {
                    // 先頭マッチングでもあるが、長さが合わず、末尾マッチングによる移動でmsの比較先頭位置ずれた
                    _LOG_DEBUG_COND(debugFlag, _T("LEAVE: UNMATCH-1: FALSE: pos={}, mpos={}"), pos, mpos);
                    return false;
                }
            }

            size_t mpos1 = mpos;
            while (mpos < ms.length()) {
                _LOG_DEBUG_COND(debugFlag, _T("LOOP-1: mpos={}, len={}"), mpos, len);
                size_t i = 0;
                for (; i < len; ++i) {
                    auto kch = keyStem[pos + i];
                    _LOG_DEBUG_COND(debugFlag, _T("LOOP-2: i={}, pos={}, kch={}"), i, pos, (wchar_t)kch);
                    if (kch == '*') {
                        size_t next_pos = pos + i + 1;
                        if (next_pos >= pos + len) {
                            _LOG_DEBUG_COND(debugFlag, _T("LEAVE: MATCHED KEY-SEGMENT"));
                            return true;  // このキーセグメントの終わり
                        }
                        // キーセグメントの残りをやる
                        size_t next_len = pos + len - next_pos;
                        return matcher(keyStem, next_pos, next_len, ms, mpos, debugFlag);
                    }
                    ++mpos;
                    if (mpos > ms.length()) {
                        _LOG_DEBUG_COND(debugFlag, _T("OVERRUN: mpos={}, ms.length()={}"), mpos, ms.length());
                        break;
                    }
                    _LOG_DEBUG_COND(debugFlag, _T("CHECK: kch={}, mpos={}, ms[{}]={}"), (wchar_t)kch, mpos, mpos - 1, (wchar_t)ms[mpos - 1]);
                    if (kch == '?' || kch == ms[mpos - 1]) continue; // 一致したので次の文字へ
                    // 不一致だった
                    _LOG_DEBUG_COND(debugFlag, _T("UNMATCHED: pos={}"), pos);
                    if (pos == 0) {
                        _LOG_DEBUG_COND(debugFlag, _T("LEAVE: UNMATCH-2: FALSE"));
                        return false;     // 先頭の場合は、位置移動不可なので、不一致とする
                    }
                    break;  // 先頭でなければ位置を移動させてよい
                }
                if (i == len) {
                    _LOG_DEBUG_COND(debugFlag, _T("LEAVE: MATCHED ALL"));
                    return true;  // 全文字について一致した
                }
                // 次の文字へ(mposを巻き戻す)
                mpos = ++mpos1;
            }
            _LOG_DEBUG_COND(debugFlag, _T("LEAVE: UNMATCH-3: FALSE"));
            return false;
        }

        // ひらがなと漢字(orカタカナ)の出現順が、読みと変換形に一致していることを確認する
        // ひらがなと漢字は、それぞれ互いに相手のワイルドカードになる
        // '?' は直前の文字種を引き継ぐ
        //   - 先頭の'?'は後続文字種と同じにする
        //   - '?' は同じ文字種にはさまれていないと意味がない(文字種が変わると、そこに'*'が挿入されているのと同じ効果があるため)
        bool order_matched(const MString& keyStem, const MazeEntry* pEntry, bool debugFlag = false) {
            // ひらがなと漢字が切り替わる位置の配列
            std::vector<size_t> changePoses;
            changePoses.push_back(0);   // 先頭位置を登録しておく
            size_t pos = 0;
            int prevType = 0;       // 直前文字の型。0: any, 1:ひらがな、2:漢字
            bool bXfer = false;     // 現在の文字種
            for (auto ch : keyStem) {
                if (ch == '?') {
                    // '?' は直前の文字種を引き継ぐ
                    // 先頭の '?' は後続文字種と同じにするので不明のまま
                } else if (ch == '*') {
                    if (pos == 0) {
                        // 先頭の '*' はひらがな扱い
                        prevType = 1;
                        bXfer = false;   // 先頭文字種を yomi とする
                    } else {
                        // 先頭以外は、直前の文字種から切り替える(不明の場合はそのまま)
                        if (prevType != 0) {
                            changePoses.push_back(pos);
                            prevType = 3 - prevType;
                        }
                    }
                } else {
                    // 新しい文字種
                    int chType = is_xfer_char(ch) ? 2 : 1;
                    if (prevType == 0) {
                        // ここまで文字種不明だったら、先頭文字種を chType とする
                        bXfer = chType == 2;
                        // ここで文字種が決定されるが、先頭からこの文字種が続いてきたということになるので、切り替え位置の登録は行わない
                    } else if (prevType != chType) {
                        // 文字種が切り替わったので、切り替え位置を登録する
                        changePoses.push_back(pos);
                    }
                    prevType = chType;
                }
                ++pos;
            }
            changePoses.push_back(pos);     // end

            _LOG_DEBUG_COND(debugFlag, _T("changePoses.size={}"), changePoses.size());
            size_t xpos = 0;
            size_t ypos = 0;
            for (size_t i = 0; i < changePoses.size() - 1; ++i) {
                pos = changePoses[i];
                size_t len = changePoses[i + 1] - pos;
                _LOG_DEBUG_COND(debugFlag, _T("bXfer={}"), bXfer);
                if (bXfer) {
                    _LOG_DEBUG_COND(debugFlag, _T("CALL: matcher(keyStem={}, pos={}, len={}, xfer={}, xpos={})"), to_wstr(keyStem), pos, len, to_wstr(pEntry->xfer), xpos);
                    if (!matcher(keyStem, pos, len, pEntry->xfer, xpos, debugFlag)) {
                        _LOG_DEBUG_COND(debugFlag, _T("order_matched=FALSE"));
                        return false;
                    }
                } else {
                    _LOG_DEBUG_COND(debugFlag, _T("CALL: matcher(keyStem={}, pos={}, len={}, stem={}, ypos={})"), to_wstr(keyStem), pos, len, to_wstr(pEntry->stem), ypos);
                    if (!matcher(keyStem, pos, len, pEntry->stem, ypos, debugFlag)) {
                        _LOG_DEBUG_COND(debugFlag, _T("order_matched=FALSE"));
                        return false;
                    }
                }
                bXfer = !bXfer;                       // ひらがなと漢字が切り替わるので、フラグを反転させる
            }
            _LOG_DEBUG_COND(debugFlag, _T("order_matched=TRUE"));
            return true;
        }

        struct candidates_t {
            MString yomi;
            bool mazeSearch;    // 読みに漢字が交じっているか
            std::vector<CandidateEntry> difficultEntries;   // 容易打鍵文字以外も含む候補
            std::vector<CandidateEntry> easyEntries;        // 容易打鍵の候補

            candidates_t() :
                mazeSearch(false)
            { }

            candidates_t(const MString& y, bool maze) :
                yomi(y),
                mazeSearch(maze) { }

#pragma warning(push)
#pragma warning(disable:4100)
            // 出力された変換形を蓄積しておく
            void StockOutput(const MazeEntry* pEntry, const MString& yomiStem, const MString& output, const MString& outXfer, bool debugFlag = false) {
                _LOG_DEBUG_COND(debugFlag, _T("stemlen={}, stem={}, yomiStem={}, output={}, outXfer={}"), \
                    pEntry->stem.size(), to_wstr(pEntry->stem), to_wstr(yomiStem), pEntry->userDic, to_wstr(output), to_wstr(outXfer));

                // 同じ出力形のものを探す
                auto sameFinder = [output](const std::vector<CandidateEntry>& entries) {
                    auto _iter = entries.begin();
                    for (; _iter != entries.end(); ++_iter) { if (_iter->output == output) break; }
                    return _iter;
                };

#define CAND_ENTRY (CandidateEntry{ pEntry, yomi, yomiStem, outXfer, output })
                if (pEntry->userDic) {
                    // ユーザー辞書由来のものは難打鍵の先頭に挿入
                    auto iter1 = sameFinder(difficultEntries);
                    if (iter1 != difficultEntries.end()) {
                        // 同形の出力があったので、それを削除
                        difficultEntries.erase(iter1); // erase entry with same output
                    }
                    auto iter2 = sameFinder(easyEntries);
                    if (iter2 != easyEntries.end()) {
                        // 同形の出力があったので、それを削除
                        easyEntries.erase(iter2); // erase entry with same output
                    }
                    difficultEntries.insert(difficultEntries.begin(), CAND_ENTRY);
                    _LOG_DEBUG_COND(debugFlag, _T("USER: yomi={}, outXfer={}"), to_wstr(yomi), to_wstr(outXfer));
                } else {
                    bool bAllEasy = EASY_CHARS->AllContainedIn(pEntry->xfer);
                    // 全て容易打鍵文字なら easyEntries を使う
                    std::vector<CandidateEntry>* pEntries = bAllEasy ? &easyEntries : &difficultEntries;
                    auto iter = sameFinder(*pEntries);
                    if (iter == pEntries->end()) {
                        // 同じ出力のものがないので、追加
                        size_t stemLen = pEntry->stem.size();
                        if (mazeSearch && !pEntries->empty() && stemLen <= pEntries->front().EntryPtr->stem.size()) {
                            // 漢字交じり読みの場合は、語幹の短い方を優先
                            _LOG_DEBUG_COND(debugFlag, _T("SHORTER: stem={}, yomi={}, outXfer={}"), to_wstr(pEntry->stem), to_wstr(yomi), to_wstr(outXfer));
                            auto it = pEntries->begin();
                            for (; it != pEntries->end(); ++it) { if (stemLen < it->EntryPtr->stem.size()) break; }
                            pEntries->insert(it, CAND_ENTRY);
                        } else if (!mazeSearch && !pEntries->empty() && stemLen >= pEntries->front().EntryPtr->stem.size()) {
                            // ひらがなだけなら、語幹の長い方を優先
                            _LOG_DEBUG_COND(debugFlag, _T("LONGER: stem={}, yomi={}, outXfer={}"), to_wstr(pEntry->stem), to_wstr(yomi), to_wstr(outXfer));
                            auto it = pEntries->begin();
                            for (; it != pEntries->end(); ++it) { if (stemLen > it->EntryPtr->stem.size()) break; }
                            pEntries->insert(it, CAND_ENTRY);
                        } else {
                            //それ以外は末尾に追加
                            _LOG_DEBUG_COND(debugFlag, _T("PUSH_BACK: yomi={}, outXfer={}"), to_wstr(yomi), to_wstr(outXfer));
                            pEntries->push_back(CAND_ENTRY);
                        }
                    }
                }
            }
#undef CAND_ENTRY
#pragma warning(pop)

            void SerializeEntries(std::vector<CandidateEntry>& outCands) {
                _LOG_DEBUGH(_T("fullMatchEntries: syomi={}, num={}"), to_wstr(yomi), difficultEntries.size());
                serializeEntries(difficultEntries, outCands);
                _LOG_DEBUGH(_T("easyEntries: syomi={}, num={}"), to_wstr(yomi), easyEntries.size());
                serializeEntries(easyEntries, outCands);
            }

        private:
            void serializeEntries(const std::vector<CandidateEntry>& entries, std::vector<CandidateEntry>& outCands) {
                // 優先辞書を探す
                const auto& primXfer = PrimaryEntries.FindEntry(yomi);
                bool primInserted = false;
                _DEBUG_SENT(size_t n = 0);
                for (const auto& ent : entries) {
                    if (!primXfer.empty() && utils::startsWith(ent.output, primXfer)) {
                        // 同じ変換形のものは先頭に挿入
                        outCands.insert(outCands.begin(), ent);
                        primInserted = true;
                        _LOG_DEBUG_COND(n < 5, _T("INSERT: PRIM_YOMI: yomi={}, xfer={}"), to_wstr(yomi), to_wstr(primXfer));
                    } else {
                        if (!primInserted) {
                            const auto* pEnt = ent.EntryPtr;
                            const auto& origXfer = PrimaryEntries.FindEntry(pEnt->GetTrimmedYomi());
                            if (!origXfer.empty() && origXfer == pEnt->xfer) {
                                // 元の読み(語尾あり)が優先辞書に登録されていれば、その変換形を先頭に挿入
                                outCands.insert(outCands.begin(), ent);
                                _LOG_DEBUG_COND(n < 5, _T("INSERT: ORIG_YOMI: yomi={}, xfer={}"), pEnt->origYomi, to_wstr(origXfer));
                                continue;
                            }
                        }
                        outCands.push_back(ent);
                        _LOG_DEBUG_COND(n < 5, _T("PUSH_BACK: yomi={}, primXfer={}"), to_wstr(yomi), to_wstr(ent.EntryPtr->xfer));
                    }
                    _DEBUG_SENT(++n);
                }
            };
        };

        class MazeCandidates {
            std::map<MString, candidates_t> candidates;

            candidates_t& find_or_new(const MString& yomi, bool maze) {
                auto iter = candidates.find(yomi);
                if (iter == candidates.end()) {
                    return candidates[yomi] = candidates_t(yomi, maze);
                    //return candidates[yomi];
                } else {
                    return iter->second;
                }
            }

        public:
            // 出力された変換形を蓄積しておく
            void StockOutput(const MString& yomi, const MString& keyStem, bool mazeSearch, const MazeEntry* pEntry, const MString& output, const MString& outXfer, bool debugFlag = false) {
                _LOG_DEBUG_COND(debugFlag, _T("yomi={}, mazeSearch={}, stem={}, userDic={}, output={}, outXfer={}"), \
                    to_wstr(yomi), mazeSearch, to_wstr(pEntry->stem), pEntry->userDic, to_wstr(output), to_wstr(outXfer));

                find_or_new(yomi, mazeSearch).StockOutput(pEntry, keyStem, output, outXfer, debugFlag);
            }

            void SerializeOutput(std::vector<CandidateEntry>& mazeCands) {
                // 長い読みから順にやる
                for (std::map<MString, candidates_t>::reverse_iterator ri = candidates.rbegin(); ri != candidates.rend(); ++ri) {
                    _LOG_DEBUGH(_T("yomi={}"), to_wstr(ri->first));
                    std::vector<CandidateEntry> outCands;
                    ri->second.SerializeEntries(outCands);
                    mazeCands.insert(mazeCands.end(), outCands.begin(), outCands.end());
                }
            }
        };

    public:
        // 指定の見出し語に対する変換候補のセットを取得する
        // 「か山」⇒「火山」より先に「海山」や「影山」が出てきてしまうのを防ぐ ⇒ 読みの短いほうを優先することで「火山」を先に出せる
        const std::vector<MazeResult>& GetCandidates(const MString& key, bool bPrim) {
            LOG_INFO(_T("\nENTER: key={}, bPrim={}"), to_wstr(key), bPrim);
            mazeCandidates.clear();
            mazeResults.clear();
            // 読み語幹＋語尾の長さごとに候補を保持しておくためのベクトル
            //std::vector<std::vector<CandidateEntry>> mazeCands;
            // 「読み語幹＋語尾」ごとに候補を保持しておくためのマップ
            MazeCandidates mazeCands;
            if (!key.empty()) {
                size_t stemMinLen = count_head_wildcard(key) + 1;   // 読みの部分にはワイルドカード以外の文字が少なくとも1文字は必要
                if (stemMinLen <= key.size()) {
                    _LOG_DEBUGH(_T("stemMinLen={}"), stemMinLen);
                    size_t tailHiraganaLen = std::min(utils::count_tail_hiragana_including_punct(key), key.size() - stemMinLen);
                    size_t gobiMaxLen = std::min(tailHiraganaLen, SETTINGS->mazeGobiMaxLen);
                    stemMinLen = key.size() > gobiMaxLen ? key.size() - gobiMaxLen : 1;
                    _LOG_DEBUGH(_T("tailHiraganaLen={}, mazeGobiMaxLen={}, gobiMaxLen={}, stemMinLen={}"), tailHiraganaLen, SETTINGS->mazeGobiMaxLen, gobiMaxLen, stemMinLen);
                    // やはり語尾にひらがな以外も含めてしまうと多々問題が生じるので、語尾はひらがなに限ることにする
                    // (2021/11/27)
                    // ⇒と思ったが、「ぶんき /分岐/」しか登録がないときに「分き点」も変換できるようにしたい
                    // ⇒漢字で終わる読みの長さが4文字以下で、末尾漢字列が2文字以下、stemが2文字以上残って漢字を含む場合のみ、末尾漢字列も語尾に含める
                    // (2021/12/11)
                    // ⇒mazeNoIfxConnectKanji==true or mazeNoIfxConnectAny==trueなら、読みの長さ制限と読みに漢字を含む制限を外す
                    // 「ひどい目にあう」⇒「ひ土井目にあう」などと変換されることもあるので注意
                    //size_t minStemTailKanjiKataLen = utils::count_tail_kanji(key.substr(0, stemMinLen));
                    // minStemTailKanjiKataLen:stemMinLenの語幹の末尾漢字・カタカナ文字列長
                    size_t minStemTailKanjiKataLen = utils::count_tail_kanji_or_katakana(key.substr(0, stemMinLen));
                    _LOG_DEBUGH(_T("minStemTailKanjiKataLen={}, stemMinLen={}, mazeNoIfxConnectKanji={}, mazeNoIfxConnectAny={}"), \
                        minStemTailKanjiKataLen, stemMinLen, SETTINGS->mazeNoIfxConnectKanji, SETTINGS->mazeNoIfxConnectAny);
                    if (minStemTailKanjiKataLen == stemMinLen) {
                        // 交ぜ書き変換対象の先頭部が漢字だけだった場合は、stemMinLen をインクリメントして、漢字以外を含める
                        stemMinLen += 1;
                    }
                    if (minStemTailKanjiKataLen > 0 && stemMinLen > minStemTailKanjiKataLen) {
                        // 語尾に漢字・カタカナががあり、語幹には少なくとも1文字のひらがながある場合⇒従来は2文字以上だったが、1文字以上にしないと「み力的」が変換できない
                        if (SETTINGS->mazeNoIfxConnectKanji || SETTINGS->mazeNoIfxConnectAny ||
                            (stemMinLen >= 3 && stemMinLen <= 4 && utils::contains_kanji(key.substr(0, stemMinLen - minStemTailKanjiKataLen)))) {
                            stemMinLen -= minStemTailKanjiKataLen;
                        }
                    }

                    _LOG_DEBUGH(_T("stemMinLen={}"), stemMinLen);
                    std::set<const MazeEntry*> entrySet;
                    bool mazeSearch = false;
                    bool mazeStar = false;
                    
                    // 交ぜ書き辞書からエントリを集める (読みが全てワイルドカードの場合は entrySet が empty になるので、無視される)
                    for (size_t i = 0; i < stemMinLen; ++i) {
                        mchar_t kch = key[i];
                        mazeSearch = mazeSearch || is_xfer_char_or_wildcard(kch);
                        mazeStar = mazeStar || is_wild_star(kch);
                        if (is_wildcard(kch)) continue;     // ワイルドカードは無視
                        auto iter = mazeDic.find(kch);
                        if (iter == mazeDic.end()) {
                            entrySet.clear();
                            break;
                        } else {
                            if (entrySet.empty())
                                entrySet.insert(iter->second.begin(), iter->second.end());
                            else {
                                utils::apply_intersection(entrySet, iter->second);
                                if (entrySet.empty()) break;
                            }
                        }
                    }

                    //bool starContained = key.find('*') != MString::npos;

                    size_t stemLen = stemMinLen;    // 最短語幹から始める
                    while (!entrySet.empty()) {
                        _LOG_DEBUGH(_T("\nentrySet.size()={}, stemLen={}, mazeSearch={}"), entrySet.size(), stemLen, mazeSearch);
                        // 長い語幹にマッチしたほうを優先
                        // 同じ語幹長の場合は、エントリの読みの短いほうが優先
                        // 同じ読みなら、ユーザー辞書を優先
                        auto keyStem = key.substr(0, stemLen);
                        if (stemLen > stemMinLen) {
                            mchar_t kch = keyStem[stemLen - 1];
                            mazeSearch = mazeSearch || is_xfer_char_or_wildcard(kch);
                            mazeStar = mazeStar || is_wild_star(kch);
                        }
                        _DEBUG_SENT(int _n = 0);
                        for (auto p : entrySet) {
                            bool debugFlag = _DEBUG_FLAG(_n < 5);
                            _LOG_DEBUG_COND(debugFlag, _T("key={}, keyStem={}, mazeSearch={}, mazeStar={}, p->ifxGobi={}, p->stem={}, p->xfer={}, user={}, deleted={}"),
                                to_wstr(key), to_wstr(keyStem), mazeSearch, mazeStar,
                                p->ifxGobi, to_wstr(p->stem), to_wstr(p->xfer), p->userDic, p->deleted);
                            if (p->deleted) continue;
                            _LOG_DEBUG_COND(debugFlag, _T("CP-A"));
                            // 読み語幹の先頭部が変換形と一致したものは除外(「代表しゃ」が「代表/する」の語幹+「し」にマッチするケースや「経い」→「経」のケース)
                            // 「けい /経/」と「けいい /経緯/」があって、「経い」を変換したときに「経」が候補として採用されることを防ぐ
                            // また、かりに「けい /緯/」という登録があったとして「け緯」⇒「緯」も防ぎたい
                            // ただし、「国民は」の「国民」は通す必要あり。そうしないと「国民派」に変換されてしまう
                            if (keyStem != p->xfer && (utils::startsWith(keyStem, p->xfer) || utils::endsWith(keyStem, p->xfer))) continue;

                            _LOG_DEBUG_COND(debugFlag, _T("CP-B"));
                            if (keyStem == p->stem || mazeSearch && (mazeStar || p->xfer.size() <= keyStem.size()) && order_matched(keyStem, p, debugFlag)) {
                                _LOG_DEBUG_COND(debugFlag, _T("CP-C"));
                                // 読み語幹が完全一致、または key に漢字が含まれている場合は、'*' を含むか変換形長 <= key長、かつ、ひらがな・漢字の出現順序の一致を確認
                                if (key.size() == stemLen) {
                                    _LOG_DEBUG_COND(debugFlag, _T("CP-D"));
                                    // 語尾がない⇒無活用または語幹OKの活用型か
                                    if (find_gobi(p->inflexList, (int)STEM_OK) == 0) {
                                        _LOG_DEBUG_COND(debugFlag, _T("No gobi found: {}: STEM_OK, userDic={}"), to_wstr(p->xfer), p->userDic);
                                        mazeCands.StockOutput(key, keyStem, mazeSearch, p, p->xfer, p->xfer, debugFlag);
                                    }
                                } else {
                                    _LOG_DEBUG_COND(debugFlag, _T("CP-E"));
                                    // 語尾がある
                                    // (「がいる」が「我いる」になったりしないようにするために,語幹が1文字の無活用語は採用しないようにしてみたが、やはり目とか手とかあるので、いったん様子見)
                                    int gobiLen = find_gobi(p->inflexList, stemLen, key.c_str() + stemLen);
                                    if (gobiLen >= 0) {
                                        _LOG_DEBUG_COND(debugFlag, _T("gobi found: {}: {}, gobiLen={}, userDic={}"), to_wstr(p->xfer), (wchar_t)key[stemLen], gobiLen, p->userDic);
                                        size_t yomiLen = stemLen + gobiLen;
                                        mazeCands.StockOutput(key.substr(0, yomiLen), keyStem, mazeSearch, p, p->xfer + key.substr(stemLen), p->xfer + key.substr(stemLen, gobiLen), debugFlag);
                                    }
                                }
                            }
                            _LOG_DEBUG_COND(debugFlag, _T("CP-F"));
                            _DEBUG_SENT(++_n);
                        }

                        // 語幹長を延ばす
                        if (stemLen >= key.size()) {
                            _LOG_DEBUGH(_T("stemLen: {}: END"), stemLen);
                            break;
                        }
                        auto nextCh = key[stemLen++];
                        auto iter = mazeDic.find(nextCh);
                        if (iter == mazeDic.end()) {
                            _LOG_DEBUGH(_T("mazeDic.find({}): not found: END"), (wchar_t)nextCh);
                            break;
                        }
                        _LOG_DEBUGH(_T("mazeDic.find({}): found size={}"), (wchar_t)nextCh, iter->second.size());
                        utils::apply_intersection(entrySet, iter->second);
                        _LOG_DEBUGH(_T("intersection applied entrySet.size={}"), entrySet.size());
                    }
                    _LOG_DEBUGH(_T("entrySet LOOP: END\n"));
                    
                }
            }
            // 結果を集める
            mazeCands.SerializeOutput(mazeCandidates);
            _LOG_DEBUGH(_T("mazeCandidates.size()={}"), mazeCandidates.size());

            // 結果を返す
            for (const auto& c : mazeCandidates) {
                mazeResults.push_back(MazeResult(c.output, c.EntryPtr ? c.EntryPtr->GetXferPlusGobiLen(c.output) : c.output.size(), key.size()));
            }
#ifdef _DEBUG
            if (IS_LOG_DEBUGH_ENABLED) {
                std::vector<String> xfers;
                std::transform(mazeResults.begin(), mazeResults.end(), std::back_inserter(xfers), [](const MazeResult& r) { return to_wstr(r.resultStr);});
                _LOG_DEBUGH(_T("maze results: {}"), utils::join(xfers, _T(","), 20));
            }
#endif
            LOG_INFO(_T("LEAVE: maze entries={}"), mazeResults.size());
            return mazeResults;
        }

        // GetCandidates() が返した候補のうち output を持つものを選択して変換履歴登録と優先辞書にコピー
        void SelectCandidate(const MString& output, bool bRegMazeHist) {
            _LOG_DEBUGH(_T("ENTER: output={}, mazeCandidates.size()={}, bRegMazeHist={}"), to_wstr(output), mazeCandidates.size(), bRegMazeHist);
            if (!mazeCandidates.empty()) {
                const MString& firstYomi = mazeCandidates[0].yomi;
                _LOG_DEBUGH(_T("firstYomi={}"), to_wstr(firstYomi));
                for (size_t n = 0; n < mazeCandidates.size(); ++n) {
                    _LOG_DEBUGH(_T("{}: yomi={}, output={}"), n, to_wstr(mazeCandidates[n].yomi), to_wstr(mazeCandidates[n].output));
                    if (mazeCandidates[n].output == output) {
                        // まず、変換履歴登録
                        const MString& myYomi = mazeCandidates[n].yomi;
                        const MString& yomiStem = mazeCandidates[n].yomiStem;
                        MString origYomi = mazeCandidates[n].EntryPtr->GetTrimmedYomi();
                        const MString& xfer = mazeCandidates[n].EntryPtr->xfer;
                        LOG_INFO(_T("firstYomi={}, myYomi={}, yomiStem={}, origYomi={}, xfer={}, mazeHistRegisterMinLen={}"), \
                            to_wstr(firstYomi), to_wstr(myYomi), to_wstr(yomiStem), to_wstr(origYomi), to_wstr(xfer), SETTINGS->mazeHistRegisterMinLen);
                        if (bRegMazeHist && SETTINGS->mazeHistRegisterMinLen > 0 && yomiStem.size() >= SETTINGS->mazeHistRegisterMinLen && yomiStem != xfer) {
                            // 無条件先頭候補出力モードではなく、語幹長が設定長以上の場合のみ変換履歴登録する
                            auto mazeHistRegister = [](const MString& s) {
                                if (SETTINGS->mazeHistRegisterAnyway) {
                                    HISTORY_DIC->AddNewEntryAnyway(s);
                                } else {
                                    HISTORY_DIC->AddNewEntry(s);
                                }
                            };
                            LOG_INFO(_T("mazeHistRegister({})"), to_wstr(yomiStem + MSTR_VERT_BAR + xfer));
                            mazeHistRegister(yomiStem + MSTR_VERT_BAR + xfer);
                        }
                        // それを優先辞書に登録する
                        if (PrimaryEntries.AddPrimaryEntryIfNeeded(origYomi, xfer)) {
                            if (myYomi != origYomi) PrimaryEntries.DeletePrimaryEntry(myYomi);
                        }
                        break;
                    }
                }
            }
            _LOG_DEBUGH(_T("LEAVE"));
        }

        // 指定の読みと変換形を持つユーザー辞書エントリを削除
        void DeleteEntry(StringRef yomi, const MString& xfer) {
            UserEntries.DeleteEntry(yomi, xfer);
        }

        bool IsEmpty() {
            return mazeDic.empty();
        }

        bool IsUserDicDirty() {
            return UserEntries.IsDirty();
        }

        // ユーザー辞書ファイルへの保存
        void SaveUserDic(utils::OfstreamWriter& writer) {
            if (!UserEntries.IsDirty()) return;

            std::set<MString> xfers;    // 同じ変換形のチェック用
            for (const auto& pair : UserEntries.GetAllEntries()) {
                String line;
                line.append(pair.first);                // 読み
                line.append(_T(" /"));
                bool bFound = false;
                for (const auto* p  : pair.second) {
                    if (!p->deleted) {
                        const auto& xfer = p->xfer;
                        auto iter = xfers.find(xfer);
                        if (iter == xfers.end()) {
                            line.append(to_wstr(xfer));  // 変換形
                            line.append(_T("/"));
                            xfers.insert(xfer);         // 書き出される変換形
                            bFound = true;
                        }
                    }
                }
                if (bFound) {
                    // 書き込み
                    writer.writeLine(utils::utf8_encode(line));
                }
            }
            UserEntries.ClearDirtyFlag();
        }

        bool IsPrimaryDicDirty() {
            return PrimaryEntries.IsDirty();
        }

        // 優先辞書ファイルへの保存
        void SavePrimaryDic(utils::OfstreamWriter& writer) {
            if (!PrimaryEntries.IsDirty()) return;

            std::set<MString> xfers;    // 同じ変換形のチェック用
            for (const auto& pair : PrimaryEntries.GetAllEntries()) {
                String line;
                line.append(to_wstr(pair.first));                // 読み
                line.append(_T(" /"));
                line.append(to_wstr(pair.second));  // 変換形
                line.append(_T("/"));
                writer.writeLine(utils::utf8_encode(line));
            }
            PrimaryEntries.ClearDirtyFlag();
        }

    private:
    };
    DEFINE_CLASS_LOGGER(MazegakiDicImpl);

} // namespace

// -------------------------------------------------------------------
DEFINE_CLASS_LOGGER(MazegakiDic);

std::unique_ptr<MazegakiDic> MazegakiDic::Singleton;

// 交ぜ書き辞書ファイル読み込んで、内部辞書を作成する(ファイル名は '|'で区切って複数指定可能)
// xxx.user.yyy が存在しない場合は、xxx.*.yyy というファイル名を含んでいること
// エラーがあったら例外を投げる
int MazegakiDic::CreateMazegakiDic(StringRef mazeFile) {
    LOG_DEBUGH(_T("ENTER: {}"), mazeFile);

    if (Singleton != 0) {
        LOG_DEBUGH(_T("already created: maze file: {}"), mazeFile);
        return 0;
    }

    int result = 0;

    auto pImpl = new MazegakiDicImpl();
    Singleton.reset(pImpl);

#if defined(NDEBUG) || DONT_READ_MAZEDIC_ON_DEBUG == 0
    // '|' で区切られた複数ファイルを対象とする
    for (const auto& name : utils::split(mazeFile, '|')) {
        if (name.empty()) continue;

        if (name.find(_T(".*.")) != String::npos) continue;    // パターンファイルは無視

        bool bUser = utils::toLower(name).find(_T(".user.")) != String::npos;  // ユーザ辞書か
        bool bPrim = utils::toLower(name).find(_T(".prim.")) != String::npos;  // 優先辞書か

        auto path = utils::joinPath(SETTINGS->rootDir, name);
        LOG_DEBUGH(_T("open maze file: {}"), path);

        utils::IfstreamReader reader(path);
        if (reader.success()) {
            //pImpl->ReadFile(utils::IfstreamReader(path).getAllLines());
            pImpl->ReadFile(reader.getAllLines(), bUser, bPrim);
            LOG_DEBUGH(_T("close maze file: {}"), path);
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_ERROR(_T("Can't read maze file: {}"), path);
            ERROR_HANDLER->Warn(std::format(_T("交ぜ書き辞書ファイル({})が開けません"), path));
            result = -1;
        }
    }
#endif

    LOG_DEBUGH(_T("LEAVE: result={}"), result);
    return result;
}

// 交ぜ書き辞書ファイルを追加で読み込む(kwmaze.wiki.txtとか)
void MazegakiDic::ReadMazegakiDic(StringRef filename) {
        auto path = utils::joinPath(SETTINGS->rootDir, filename);
        LOG_DEBUGH(_T("open maze file: {}"), path);

        utils::IfstreamReader reader(path);
        if (reader.success()) {
            Singleton->ReadFile(reader.getAllLines(), false, false);
            LOG_DEBUGH(_T("close maze file: {}"), path);
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_ERROR(_T("Can't read maze file: {}"), path);
            ERROR_HANDLER->Warn(std::format(_T("交ぜ書き辞書ファイル({})が開けません"), path));
        }
}

// 交ぜ書き辞書ファイルに書き込む
void MazegakiDic::WriteMazegakiDic(StringRef path, bool bUser, bool bPrim) {
    LOG_DEBUGH(_T("CALLED: path={}, bUser={}, bPrim={}"), path, bUser, bPrim);
    if (!path.empty() && Singleton && !Singleton->IsEmpty()) {
        if (bPrim) {
            if (Singleton->IsPrimaryDicDirty()) {
                if (utils::moveFileToBackDirWithRotation(path, SETTINGS->backFileRotationGeneration)) {
                    utils::OfstreamWriter writer(path);
                    if (writer.success()) {
                        Singleton->SavePrimaryDic(writer);
                    }
                }
            }
        } else if (bUser) {
            if (Singleton->IsUserDicDirty()) {
                if (utils::moveFileToBackDirWithRotation(path, SETTINGS->backFileRotationGeneration)) {
                    utils::OfstreamWriter writer(path);
                    if (writer.success()) {
                        Singleton->SaveUserDic(writer);
                    }
                }
            }
        }
    }
}

// 交ぜ書き辞書ファイルに書き込む
void MazegakiDic::WriteMazegakiDic() {
    if (!SETTINGS->mazegakiFile.empty()) {
        String primDic;
        String userDic;
        for (const auto& nm : utils::split(SETTINGS->mazegakiFile, '|')) {
            auto lname = utils::toLower(nm);
            if (lname.find(_T(".prim.")) != String::npos) {
                // 指定の優先辞書名があった
                primDic = nm;
                LOG_DEBUGH(_T("primary maze filename found: {}"), primDic);
                break;
            } else if (lname.find(_T(".user.")) != String::npos) {
                // 指定のユーザ辞書名があった
                userDic = nm;
                LOG_DEBUGH(_T("user maze filename found: {}"), userDic);
                break;
            } else {
                // パターンファイル(xxx.*.yyy) があれば xxx.user.yyy に変える
                size_t pos = lname.find(_T(".*."));
                if (pos != String::npos) {
                    if (primDic.empty()) primDic = nm.substr(0, pos + 1) + _T("prim") + nm.substr(pos + 2);
                    if (userDic.empty()) userDic = nm.substr(0, pos + 1) + _T("user") + nm.substr(pos + 2);
                    LOG_DEBUGH(_T("replaced primary maze filename: {}"), primDic);
                    LOG_DEBUGH(_T("replaced user maze filename: {}"), userDic);
                }
            }
        }

        if (primDic.empty()) primDic = _T("kwmaze.prim.dic");       // デフォルトの優先辞書名
        if (userDic.empty()) userDic = _T("kwmaze.user.dic");       // デフォルトのユーザー辞書名

        auto path = utils::joinPath(SETTINGS->rootDir, userDic);
        LOG_DEBUGH(_T("save user maze file: {}"), path);
        WriteMazegakiDic(path, true, false);

        path = utils::joinPath(SETTINGS->rootDir, primDic);
        LOG_DEBUGH(_T("save primary maze file: {}"), path);
        WriteMazegakiDic(path, false, true);
    }
}
