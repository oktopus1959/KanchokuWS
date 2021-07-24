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

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

#define _WCHAR(ws)  (_T(ws)[0])

// 語幹のみでもOK
#define STEM_OK 1

// 無活用の最大語尾長
#define NO_IFX_MAX_GOBI 2

namespace {
    // -------------------------------------------------------------------
    // 活用語尾
    // 一段活用
    mchar_t IFX_RU_1[] = { _WCHAR("る"), _WCHAR("れ"), _WCHAR("よ"), _WCHAR("な"), _WCHAR("た"), _WCHAR("て"), _WCHAR("ま"), _WCHAR("ら"), STEM_OK, 0 };
    // 五段活用「く」(書く)
    mchar_t IFX_KU_5[] = { _WCHAR("か"), _WCHAR("き"), _WCHAR("く"), _WCHAR("け"), _WCHAR("こ"), _WCHAR("い"), 0 };
    // 五段活用「ぐ」(漕ぐ)
    mchar_t IFX_GU_5[] = { _WCHAR("が"), _WCHAR("ぎ"), _WCHAR("ぐ"), _WCHAR("げ"), _WCHAR("ご"), _WCHAR("い"), 0 };
    // 五段活用「す」(話す)
    mchar_t IFX_SU_5[] = { _WCHAR("さ"), _WCHAR("し"), _WCHAR("す"), _WCHAR("せ"), _WCHAR("そ"), 0 };
    // 五段活用「つ」(立つ)
    mchar_t IFX_TU_5[] = { _WCHAR("た"), _WCHAR("ち"), _WCHAR("つ"), _WCHAR("て"), _WCHAR("と"), _WCHAR("っ"), 0 };
    // 五段活用「ぬ」(死ぬ)
    mchar_t IFX_NU_5[] = { _WCHAR("な"), _WCHAR("に"), _WCHAR("ぬ"), _WCHAR("ね"), _WCHAR("の"), _WCHAR("ん"), 0 };
    // 五段活用「ぶ」(飛ぶ)
    mchar_t IFX_BU_5[] = { _WCHAR("ば"), _WCHAR("び"), _WCHAR("ぶ"), _WCHAR("べ"), _WCHAR("ぼ"), _WCHAR("ん"), 0 };
    // 五段活用「む」(生む)
    mchar_t IFX_MU_5[] = { _WCHAR("ま"), _WCHAR("み"), _WCHAR("む"), _WCHAR("め"), _WCHAR("も"), _WCHAR("ん"), 0 };
    // 五段活用「る」(振る)
    mchar_t IFX_RU_5[] = { _WCHAR("ら"), _WCHAR("り"), _WCHAR("る"), _WCHAR("れ"), _WCHAR("ろ"), _WCHAR("っ"), 0 };
    // 五段活用「う」(会う)
    mchar_t IFX_WU_5[] = { _WCHAR("わ"), _WCHAR("い"), _WCHAR("う"), _WCHAR("え"), _WCHAR("お"), _WCHAR("っ"), 0 };
    // サ変活用「する」(開発する)(達する、愛するは、五段として登録する)
    mchar_t IFX_SURU[] = { STEM_OK, _WCHAR("し"), _WCHAR("す"), 0 };
    // ザ変活用「ずる」(信ずる)
    mchar_t IFX_ZURU[] = { _WCHAR("じ"), _WCHAR("ず"), _WCHAR("ぜ"), 0 };
    // 形容詞「い」(美しい)
    mchar_t IFX_I[] = { _WCHAR("い"), _WCHAR("か"), _WCHAR("き"), _WCHAR("く"), _WCHAR("け"),  _WCHAR("さ"), 0 };
    // 形容動詞「な」(静かな)
    mchar_t IFX_NA[] = { STEM_OK, _WCHAR("な"), _WCHAR("に"), _WCHAR("だ"), _WCHAR("で"), _WCHAR("じ"), _WCHAR("さ"), 0 };
    // 形容詞「の」(本当の)
    mchar_t IFX_NO[] = { STEM_OK, _WCHAR("な"), _WCHAR("の"), _WCHAR("に"), _WCHAR("だ"), _WCHAR("で"), _WCHAR("じ"), _WCHAR("さ"), 0 };
    // 無活用
    //mchar_t IFX_NONE[] = { STEM_OK, 0 };
    mchar_t IFX_NONE[] = { STEM_OK, _WCHAR("が"), _WCHAR("は"), _WCHAR("も"), _WCHAR("を"), _WCHAR("な"), _WCHAR("の"), _WCHAR("に"), _WCHAR("だ"), _WCHAR("で"), _WCHAR("じ"), _WCHAR("さ"), _WCHAR("こ"), 0 };

    inline bool find_gobi(const mchar_t* ifxes, mchar_t mc) {
        while (*ifxes != 0) {
            if (*ifxes++ == mc) return true;
        }
        return false;
    }

    // 活用型->活用語尾のマップ
    std::map<wstring, const mchar_t*> gobiMap = {
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
        { _T("い"), IFX_I },
        { _T("な"), IFX_NA },
        { _T("の"), IFX_NO },
    };

    // 上一段、下一段
    //std::wregex IchiDan(_T("[いえきけぎげしせじぜちてにねびべみめりれ]$"));
    std::set<wchar_t> IchiDan = {
        _WCHAR("い"), _WCHAR("え"), _WCHAR("き"), _WCHAR("け"), _WCHAR("ぎ"), _WCHAR("げ"), _WCHAR("し"), _WCHAR("せ"), _WCHAR("じ"), _WCHAR("ぜ"),
        _WCHAR("ち"), _WCHAR("て"), _WCHAR("に"), _WCHAR("ね"), _WCHAR("び"), _WCHAR("べ"), _WCHAR("み"), _WCHAR("め"), _WCHAR("り"), _WCHAR("れ"), };

    // イ段・エ段でも五段活用するもの
    std::set<wstring> GoDan = {
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

    // 読みの文字 (ひらがな or '*')
    inline bool is_yomi_char(mchar_t mch) { return mch == '*' || utils::is_hiragana(mch); }

    // 変換形の文字か(ひらがな以外かつ'*'以外)
    inline bool is_xfer_char(mchar_t mch) { return !is_yomi_char(mch); }

    // 変換形の文字または '?' か
    inline bool is_xfer_char_or_ques(mchar_t mch) { return mch == '?' || is_xfer_char(mch); }

    // 変換形の文字またはワイルドカードか
    inline bool is_xfer_char_or_wildcard(mchar_t mch) { return is_wildcard(mch) || is_xfer_char(mch); }

    // -------------------------------------------------------------------
    // 交ぜ書き辞書のエントリ
    class MazeEntry {
    public:
        MString stem;               // 読み(語幹)(「たべ」)
        MString xfer;               // 変換形(語幹)(「食べ」)
        const mchar_t* inflexList;  // 語尾リスト
        wstring yomi;               // 元の読み形(語尾情報含む)
        bool userDic;               // ユーザ辞書由来
        bool deleted;               // 削除フラグ

    public:
        static MazeEntry* CreateEntry(const wstring& yomi, const wstring xfer, bool bUser) {
            size_t pos = yomi.find_first_of('/');
            if (pos == wstring::npos) {
                // 語尾指定がない ⇒ 無活用
                return new MazeEntry{ to_mstr(yomi), to_mstr(xfer), IFX_NONE, yomi, bUser };
            } else {
                // 語尾指定あり
                wstring gobi = yomi.substr(pos + 1);
                //if (gobi == _T("る") && std::regex_match(gobi, IchiDan))
                if (pos > 0 && gobi == _T("る") && IchiDan.find(yomi[pos - 1]) != IchiDan.end()) {
                    // 語幹末尾が「イ段」または「エ段」であり、五段活用する語尾でなければ一段活用とみなす
                    wstring ts;
                    if (pos > 0) {
                        ts += yomi[pos - 1];
                        ts += xfer.back();
                    }
                    if (ts.empty() || GoDan.find(ts) == GoDan.end()) gobi += _T(":1");
                }
                auto iter = gobiMap.find(gobi);
                auto ifxList = iter != gobiMap.end() ? iter->second : IFX_NONE;
                return new MazeEntry{ to_mstr(yomi.substr(0, pos)), to_mstr(xfer), ifxList, yomi, bUser };
            }
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

        const MazeEntry* AddEntry(const wstring& yomi, const wstring& xfer, bool bUser = false) {
            return AddEntry(MazeEntry::CreateEntry(yomi, xfer, bUser));
        }

        const std::vector<const MazeEntry*>& GetList() const { return list; }

    }; // MazeEntryList

    // 交ぜ書きエントリの全リスト
    MazeEntryList MazeEntries;

    // ユーザー辞書エントリのリストのクラス
    class UserDicEntries {
        std::map<wstring, std::vector<const MazeEntry*>> userEntries;

        std::vector<const MazeEntry*> emptyList;

    public:
        void AddUserEntries(const wstring& yomi, const std::vector<const MazeEntry*>& entries) {
            auto iter = userEntries.find(yomi);
            if (iter == userEntries.end()) {
                userEntries[yomi] = entries;
            } else {
                iter->second.insert(iter->second.begin(), entries.begin(), entries.end());
            }
        }

        const std::vector<const MazeEntry*>& FindEntries(const wstring& yomi) {
            auto iter = userEntries.find(yomi);
            return iter == userEntries.end() ? emptyList : iter->second;
        }

        const std::map<wstring, std::vector<const MazeEntry*>>& GetAllEntries() const {
            return userEntries;
        }

        void DeleteEntry(const wstring& yomi, const MString& xfer) {
            for (auto p : FindEntries(yomi)) {
                if (p->xfer == xfer) ((MazeEntry*)p)->deleted = true;
            }
        }

        void DeleteEntry(const MazeEntry* pEntry) {
            DeleteEntry(pEntry->yomi, pEntry->xfer);
        }
    };

    // ユーザー辞書エントリのリスト
    UserDicEntries UserEntries;

    // -------------------------------------------------------------------
    // 交ぜ書き辞書(読み、または変換形に含まれる1文字から、もとのエントリを集めたsetへのマップ)
    typedef std::map<mchar_t, std::set<const MazeEntry*>> MazeDictionary;

    // 交ぜ書き辞書の実装クラス
    class MazegakiDicImpl : public MazegakiDic {
    private:
        DECLARE_CLASS_LOGGER;

        // 交ぜ書き辞書インスタンス
        MazeDictionary mazeDic;

        // 検索されたエントリとそれから生成された変換形
        struct CandidateEntry {
            const MazeEntry* EntryPtr;
            MString output;             // 生成された変換形
        };

        // 検索された候補のリスト
        std::vector<CandidateEntry> mazeCandidates;

        // 上記から生成された変換後文字列のリスト
        std::vector<MString> mazeResult;

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
                    LOG_DEBUGH(_T("mazeDic.size()=%d"), mazeDic.size());
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
        void addEntries(const wstring& yomi, const std::vector<wstring>& list, bool bUser) {
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
        void ReadFile(const std::vector<wstring>& lines, bool bUser) {
            LOG_INFO(_T("ENTER: lines.size()=%d, mazeDic.size()=%d, MazeEntries.size()=%d"), lines.size(), mazeDic.size(), MazeEntries.GetList().size());
            Logger::SaveAndSetLevel(Logger::LogLevelWarn);
            //Logger::SaveAndSetLevel(Logger::LogLevelInfoH);
            for (const auto& line : lines) {
                AddMazeDicEntry(line, bUser);
            }
            Logger::RestoreLevel();
            LOG_INFO(_T("LEAVE: mazeDic.size()=%d, MazeEntries.size()=%d"), mazeDic.size(), MazeEntries.GetList().size());
        }

        // 一行の辞書ソース文字列を解析して辞書に登録する
        bool AddMazeDicEntry(const wstring& line, bool bUser) {
            LOG_DEBUGH(_T("ENTER: line=%s"), line.c_str());
            auto items = utils::filter_not_empty(utils::split(utils::strip(line), ' '));
            // 「見出し<空白>/?登録語/...」という形式でなければ、何もしない (登録語列の先頭は '/' でなくてもよい)
            if (items.size() != 2 || items[0].empty() || items[1].empty() /*|| items[1][0] != '/'*/) return false;

            // キー
            const auto& key = items[0];
            // 内容
            auto list = utils::filter_not_empty(utils::split(items[1], '/')); // 内容

            if (!list.empty()) {
                // 登録内容がある場合のみ登録する
                addEntries(key, list, bUser);
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
        bool matcher(const MString& keyStem, size_t pos, size_t len, const MString& ms, size_t& mpos) {
            LOG_DEBUGH(_T("ENTER: keyStem=%s, pos=%d, len=%d, ms=%s, mpos=%d"), MAKE_WPTR(keyStem), pos, len, MAKE_WPTR(ms), mpos);
            if (pos + len >= keyStem.size()) {
                // 末尾のマッチングの場合
                bool tailStar = pos < keyStem.size() && keyStem.find('*', pos) != MString::npos;
                size_t tlen = len - (tailStar ? 1 : 0);
                if (mpos + tlen > ms.size()) return false;   // 読み(変換形)の文字数が足りないので不一致とする
                if (!tailStar) mpos = ms.size() - len;
                LOG_DEBUGH(_T("tailStar=%s, pos=%d, tlen=%d, mpos=%d"), BOOL_TO_WPTR(tailStar), pos, tlen, mpos);
                if (pos == 0 && mpos > 0) {
                    // 先頭マッチングでもあるが、長さが合わず、末尾マッチングによる移動でmsの比較先頭位置ずれた
                    LOG_DEBUGH(_T("LEAVE: UNMATCH-1: pos=%d, mpos=%d"), pos, mpos);
                    return false;
                }
            }

            size_t mpos1 = mpos;
            while (mpos < ms.length()) {
                LOG_DEBUGH(_T("LOOP-1: mpos=%d, len=%d"), mpos, len);
                size_t i = 0;
                for (; i < len; ++i) {
                    auto kch = keyStem[pos + i];
                    LOG_DEBUGH(_T("LOOP-2: i=%d, pos=%d, kch=%c"), i, pos, kch);
                    if (kch == '*') {
                        size_t next_pos = pos + i + 1;
                        if (next_pos >= pos + len) {
                            LOG_DEBUGH(_T("LEAVE: MATCHED KEY-SEGMENT"));
                            return true;  // このキーセグメントの終わり
                        }
                        // キーセグメントの残りをやる
                        size_t next_len = pos + len - next_pos;
                        return matcher(keyStem, next_pos, next_len, ms, mpos);
                    }
                    ++mpos;
                    if (mpos > ms.length()) {
                        LOG_DEBUGH(_T("OVERRUN: mpos=%d, ms.length()=%d"), mpos, ms.length());
                        break;
                    }
                    LOG_DEBUGH(_T("CHECK: kch=%c, mpos=%d, ms[%d]=%c"), kch, mpos, mpos - 1, ms[mpos - 1]);
                    if (kch == '?' || kch == ms[mpos - 1]) continue; // 一致したので次の文字へ
                    // 不一致だった
                    LOG_DEBUGH(_T("UNMATCHED: pos=%d"), pos);
                    if (pos == 0) {
                        LOG_DEBUGH(_T("LEAVE: UNMATCH-2"));
                        return false;     // 先頭の場合は、位置移動不可なので、不一致とする
                    }
                    break;  // 先頭でなければ位置を移動させてよい
                }
                if (i == len) {
                    LOG_DEBUGH(_T("LEAVE: MATCHED ALL"));
                    return true;  // 全文字について一致した
                }
                // 次の文字へ(mposを巻き戻す)
                mpos = ++mpos1;
            }
            LOG_DEBUGH(_T("LEAVE: UNMATCH-3"));
            return false;
        }

        // ひらがなと漢字(orカタカナ)の出現順が、読みと変換形に一致していることを確認する
        // ひらがなと漢字は、それぞれ互いに相手のワイルドカードになる
        // '?' は直前の文字種を引き継ぐ
        //   - 先頭の'?'は後続文字種と同じにする
        //   - '?' は同じ文字種にはさまれていないと意味がない(文字種が変わると、そこに'*'が挿入されているのと同じ効果があるため)
        bool order_matched(const MString& keyStem, const MazeEntry* pEntry) {
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

            size_t xpos = 0;
            size_t ypos = 0;
            for (size_t i = 0; i < changePoses.size() - 1; ++i) {
                pos = changePoses[i];
                size_t len = changePoses[i + 1] - pos;
                if (bXfer) {
                    if (!matcher(keyStem, pos, len, pEntry->xfer, xpos)) return false;
                } else {
                    if (!matcher(keyStem, pos, len, pEntry->stem, ypos)) return false;
                }
                bXfer = !bXfer;                       // ひらがなと漢字が切り替わるので、フラグを反転させる
            }
            return true;
        }

#define UNDEF_LEN 100000000
#define CAND_ENTRY (CandidateEntry{ pEntry, output })
        struct candidates_t {
            std::vector<CandidateEntry> entries;    // 検索された候補
            size_t headYomiLen;                     // 先頭候補の読み長
            size_t headSameCnt;                     // 同じ読み長さの候補数

            candidates_t() :
                headYomiLen(UNDEF_LEN),            // 初期値としてありえない長さを設定
                headSameCnt(0) { }
        };

        // 出力された変換形を蓄積しておく
        void stock_output(candidates_t* pCands, const MazeEntry* pEntry, const MString& output) {
            size_t len = pEntry->stem.size();
            if (/*!starContained &&*/ len < pCands->headYomiLen) {
                // 読みの短いほうを優先する
                utils::insert_front(pCands->entries, CAND_ENTRY);
                pCands->headYomiLen = len;
                pCands->headSameCnt = 1;
            //} else if (starContained && (pCands->headYomiLen == UNDEF_LEN || len > pCands->headYomiLen)) {
            //    // 読みの長いほうを優先する
            //    utils::insert_front(pCands->entries, CAND_ENTRY);
            //    pCands->headYomiLen = len;
            //    pCands->headSameCnt = 1;
            } else if (len == pCands->headYomiLen) {
                // 読みの長さが同じなら、ユーザー辞書にあるものを優先
                auto iter = pCands->entries.begin();
                for (; iter != pCands->entries.end(); ++iter) { if (iter->output == output) break; }
                if (pEntry->userDic) {
                    if (iter != pCands->entries.end()) {
                        pCands->entries.erase(iter); // erase entry with same output
                        pCands->headSameCnt -= 1;
                    }
                    auto users = UserEntries.FindEntries(pEntry->yomi);
                    // ユーザー辞書中での位置を取得
                    auto finder = [users](const MString& xfer) {
                        size_t n = 0;
                        for (auto p : users) {
                            if (p->xfer == xfer) return n;
                            ++n;
                        }
                        return n;
                    };
                    size_t nth = finder(pEntry->xfer);
                    iter = pCands->entries.begin();
                    // ユーザー辞書順に並べる
                    while (iter != pCands->entries.end() && iter->EntryPtr->userDic && finder(iter->EntryPtr->xfer) < nth) ++iter;
                    pCands->entries.insert(iter, CAND_ENTRY);
                    pCands->headSameCnt += 1;
                } else {
                    if (iter == pCands->entries.end()) {
                        // 同じ読み長さ候補群の末尾に追加
                        utils::insert_at(pCands->entries, pCands->headSameCnt, CAND_ENTRY);
                        pCands->headSameCnt += 1;
                    }
                }
            } else {
                pCands->entries.push_back(CAND_ENTRY);
            }
        }
#undef CAND_ENTRY
#undef UNDEF_LEN

    public:
        // 指定の見出し語に対する変換候補のセットを取得する
        // 「か山」⇒「火山」より先に「海山」や「影山」が出てきてしまうのを防ぐ ⇒ 読みの短いほうを優先することで「火山」を先に出せる
        const std::vector<MString>& GetCandidates(const MString& key) {
            LOG_INFO(_T("ENTER: key=%s"), MAKE_WPTR(key));
            mazeCandidates.clear();
            mazeResult.clear();
            if (!key.empty()) {
                size_t stemMinLen = count_head_wildcard(key) + 1;   // 読みの部分にはワイルドカード以外の文字が少なくとも1文字は必要
                if (stemMinLen <= key.size()) {
                    size_t tailHiraganaLen = min(utils::count_tail_hiragana(key), key.size() - stemMinLen);
                    size_t gobiMaxLen = min(tailHiraganaLen, SETTINGS->mazeGobiMaxLen);
                    stemMinLen = key.size() > gobiMaxLen ? key.size() - gobiMaxLen : 1;
                    std::set<const MazeEntry*> entrySet;
                    bool mazeSearch = false;
                    
                    // 交ぜ書き辞書からエントリを集める (読みが全てワイルドカードの場合は entrySet が empty になるので、無視される)
                    for (size_t i = 0; i < stemMinLen; ++i) {
                        mchar_t kch = key[i];
                        mazeSearch = mazeSearch || is_xfer_char_or_wildcard(kch);
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
                        LOG_DEBUGH(_T("entrySet.size()=%d"), entrySet.size());
                        // 長い語幹にマッチしたほうを優先
                        // 同じ語幹長の場合は、エントリの読みの短いほうが優先
                        // 同じ読みなら、ユーザー辞書を優先
                        candidates_t cands1;                         // 読みが完全一致したもの用
                        candidates_t cands2;                         // 読みと変換形でマッチしたもの用
                        auto keyStem = key.substr(0, stemLen);
                        for (auto p : entrySet) {
                            LOG_DEBUGH(_T("keyStem=%s, stem=%s, xfer=%s, ifx=%s, user=%s, deleted=%s"),
                                MAKE_WPTR(keyStem), MAKE_WPTR(p->stem), MAKE_WPTR(p->xfer), MAKE_WPTR(p->inflexList), BOOL_TO_WPTR(p->userDic), BOOL_TO_WPTR(p->deleted));
                            if (p->deleted) continue;

                            candidates_t* pCands = 0;
                            if (keyStem == p->stem) {
                                // 語幹が完全一致
                                pCands = &cands1;
                            } else if (mazeSearch && order_matched(keyStem, p)) {
                                // key に漢字が含まれている場合は、ひらがな・漢字の出現順序の一致を確認
                                pCands = &cands2;
                            }
                            if (pCands) {
                                if (key.size() == stemLen) {
                                    // 語尾がない⇒無活用または語幹OKの活用型か
                                    if (find_gobi(p->inflexList, STEM_OK)) {
                                        LOG_DEBUGH(_T("No gobi found: %s: STEM_OK in %s, userDic=%s"), MAKE_WPTR(p->xfer), MAKE_WPTR(p->inflexList), BOOL_TO_WPTR(p->userDic));
                                        stock_output(pCands, p, p->xfer);
                                    }
                                } else if (p->inflexList != IFX_NONE || (key.size() > stemLen && (key.size() - stemLen) <= NO_IFX_MAX_GOBI)) {
                                    // 語尾がある(無活用語の場合は、語尾長が NO_IFX_MAX_GOBI 以下)⇒ 語尾リストに含まれるか
                                    if (find_gobi(p->inflexList, key[stemLen])) {
                                        LOG_DEBUGH(_T("gobi found: %s: %c in %s, userDic=%s"), MAKE_WPTR(p->xfer), key[stemLen], MAKE_WPTR(p->inflexList), BOOL_TO_WPTR(p->userDic));
                                        stock_output(pCands, p, p->xfer + key.substr(stemLen));
                                    }
                                }
                            }
                        }
                        // まず、動的交ぜ書きで一致したもの -- 読みの長いほうが後から処理されるので、先頭部に挿入
                        if (!cands2.entries.empty()) utils::insert_front(mazeCandidates, cands2.entries);
                        // 読みが完全一致したものを先頭のほうに挿入
                        if (!cands1.entries.empty()) utils::insert_front(mazeCandidates, cands1.entries);

                        // 語幹長を延ばす
                        if (stemLen >= key.size()) break;
                        auto iter = mazeDic.find(key[stemLen++]);
                        if (iter == mazeDic.end()) break;
                        utils::apply_intersection(entrySet, iter->second);
                    }
                    
                }
            }
            // 結果を返す
            for (const auto& c : mazeCandidates) mazeResult.push_back(c.output);
            LOG_INFO(_T("LEAVE: maze entries=%d"), mazeResult.size());
            return mazeResult;
        }

        // GetCandidates() が返した候補のうち output を持つものを選択してユーザー辞書にコピー
        void SelectCadidate(const MString& output) {
            for (size_t n = 1; n < mazeCandidates.size(); ++n) {
                if (mazeCandidates[n].output == output) {
                    auto nthPtr = mazeCandidates[n].EntryPtr;
                    if (mazeCandidates[n - 1].EntryPtr->yomi == nthPtr->yomi) {
                        LOG_INFO(_T("ADD: yomi=%s, xfer=%s"), nthPtr->yomi.c_str(), MAKE_WPTR(nthPtr->xfer));
                        // 1つ前の候補と同じ読みだった、つまり同じ読み群の中で先頭でなかったら、ユーザー辞書にコピーする
                        // 新しいユーザーエントリを作成して登録する
                        const MazeEntry* pEntry = MazeEntries.AddEntry(nthPtr->yomi, to_wstr(nthPtr->xfer), true);
                        InsertEntry(pEntry);
                        // 同一のエントリがあればそれを削除
                        UserEntries.DeleteEntry(pEntry);
                        // 同一読みの先頭に追加
                        UserEntries.AddUserEntries(pEntry->yomi, utils::make_one_element_vector(pEntry));
                    }
                }
            }
        }

        // 指定の読みと変換形を持つユーザー辞書エントリを削除
        void DeleteEntry(const wstring& yomi, const MString& xfer) {
            UserEntries.DeleteEntry(yomi, xfer);
        }

        bool IsEmpty() {
            return mazeDic.empty();
        }

        // ユーザー辞書ファイルへの保存
        void SaveUserDic(utils::OfstreamWriter& writer) {
            std::set<MString> xfers;    // 同じ変換形のチェック用
            for (const auto& pair : UserEntries.GetAllEntries()) {
                wstring line;
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
int MazegakiDic::CreateMazegakiDic(const tstring& mazeFile) {
    LOG_INFO(_T("ENTER: %s"), mazeFile.c_str());

    if (Singleton != 0) {
        LOG_INFO(_T("already created: maze file: %s"), mazeFile.c_str());
        return 0;
    }

    int result = 0;

    auto pImpl = new MazegakiDicImpl();
    Singleton.reset(pImpl);

    // '|' で区切られた複数ファイルを対象とする
    for (const auto& name : utils::split(mazeFile, '|')) {
        if (name.empty()) continue;

        if (name.find(_T(".*.")) != wstring::npos) continue;    // パターンファイルは無視

        bool bUser = utils::toLower(name).find(_T(".user.")) != wstring::npos;  // ユーザ辞書か

        auto path = utils::joinPath(SETTINGS->rootDir, name);
        LOG_INFO(_T("open maze file: %s"), path.c_str());

        utils::IfstreamReader reader(path);
        if (reader.success()) {
            //pImpl->ReadFile(utils::IfstreamReader(path).getAllLines());
            pImpl->ReadFile(reader.getAllLines(), bUser);
            LOG_INFO(_T("close maze file: %s"), path.c_str());
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_ERROR(_T("Can't read maze file: %s"), path.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("交ぜ書き辞書ファイル(%s)が開けません"), path.c_str()));
            result = -1;
        }
    }
    LOG_INFO(_T("LEAVE: result=%d"), result);
    return result;
}

// 交ぜ書き辞書ファイルを読み込む
void MazegakiDic::ReadMazegakiDic(const tstring& filename) {
        auto path = utils::joinPath(SETTINGS->rootDir, filename);
        LOG_INFO(_T("open maze file: %s"), path.c_str());

        utils::IfstreamReader reader(path);
        if (reader.success()) {
            Singleton->ReadFile(reader.getAllLines(), false);
            LOG_INFO(_T("close maze file: %s"), path.c_str());
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_ERROR(_T("Can't read maze file: %s"), path.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("交ぜ書き辞書ファイル(%s)が開けません"), path.c_str()));
        }
}

// 交ぜ書き辞書ファイルに書き込む
void MazegakiDic::WriteMazegakiDic(const tstring& path) {
    LOG_INFO(_T("CALLED: path=%s"), path.c_str());
    if (!path.empty() && Singleton) {
        if (!Singleton->IsEmpty()) {
            if (utils::moveFileToBackDirWithRotation(path, SETTINGS->backFileRotationGeneration)) {
                utils::OfstreamWriter writer(path);
                if (writer.success()) {
                    Singleton->SaveUserDic(writer);
                }
            }
        }
    }
}

// 交ぜ書き辞書ファイルに書き込む
void MazegakiDic::WriteMazegakiDic() {
    if (!SETTINGS->mazegakiFile.empty()) {
        wstring userDic = _T("kwmaze.user.dic");       // デフォルトのユーザー辞書名
        for (const auto& nm : utils::split(SETTINGS->mazegakiFile, '|')) {
            auto lname = utils::toLower(nm);
            if (lname.find(_T(".user.")) != wstring::npos) {
                // 指定のユーザ辞書名があった
                userDic = nm;
                LOG_INFO(_T("user maze filename found: %s"), userDic.c_str());
                break;
            } else {
                // パターンファイル(xxx.*.yyy) があれば xxx.user.yyy に変える
                size_t pos = lname.find(_T(".*."));
                if (pos != wstring::npos) {
                    userDic = nm.substr(0, pos + 1) + _T("user") + nm.substr(pos + 2);
                    LOG_INFO(_T("replaced user maze filename: %s"), userDic.c_str());
                }
            }
        }
        auto path = utils::joinPath(SETTINGS->rootDir, userDic);
        LOG_INFO(_T("save user maze file: %s"), path.c_str());
        WriteMazegakiDic(path);
    }
}
