#include "Logger.h"
#include "string_type.h"
#include "file_utils.h"
#include "path_utils.h"

//#include "Constants.h"
#include "Settings.h"
#include "ErrorHandler.h"
//#include "OutputStack.h"
#include "BushuDic.h"
//#include "BushuAssoc.h"
#include "BushuAssocDic.h"

// -------------------------------------------------------------------
namespace {
    inline bool isDelim(mchar_t m) { return m == 0 || m == wchar_t('\r') || m == wchar_t('\n'); }

    // -------------------------------------------------------------------
    /** 部首連想入力リストのエントリ */
    class BushuAssocEntryImpl : public BushuAssocEntry {
    private:
        mchar_t key = 0;                // 部首
        size_t posFixed = 0;            // 固定位置文字数
        std::vector<mchar_t> list;      // key から生成される文字のリスト
        bool filled = false;            // bushuDic からターゲットを取得済みか

    private:
        // リストから tgt を検索し、その位置を返す。リストに存在しなければ末尾に追加する
        size_t findTarget(mchar_t tgt) {
            int x = posFixed;
            if (x < 0) x = 0;
            int n = -1;
            for (size_t i = 0; i < list.size(); ++i) {
                if (list[i] == tgt) {
                    n = i;
                    break;
                }
            }
            if (n < 0) {
                n = list.size();
                list.push_back(tgt);
            }
            return n;
        }

    public:
        BushuAssocEntryImpl(mchar_t k = 0) : key(k) {
        }

        void ReadLine(StringRef line) {
            MString mline = to_mstr(line);
            key = mline[0];
            bool bFixed = false;
            for (size_t pos = 1; pos < mline.size(); ++pos) {
                mchar_t a = mline[pos];
                if (isDelim(a)) break;
                if (a == '=') continue;
                if (a == '|') {
                    if (!bFixed) {
                        posFixed = list.size();
                        bFixed = true;
                    }
                    continue;
                }
                list.push_back(a);
            }
            if (!bFixed) posFixed = list.size();
        }

        // 連想リストの元となるキー文字を返す
        mchar_t GetKey() const { return key; }

        const std::vector<mchar_t>& GetList() const {
            return list;
        }

        size_t GetPosFixed() const { return posFixed; }

        std::string MakeDicLine() {
            String line;
            line.append(to_wstr(key));
            line.append(_T("="));
            size_t len = posFixed + 10;    // 固定位置以外に10文字まで保存
            if (len > list.size()) len = list.size();
            if (posFixed < len) {
                line.append(to_wstr(list, 0, posFixed));
                line.append(_T("|"));
                line.append(to_wstr(list, posFixed, len - posFixed));
            } else {
                line.append(to_wstr(list, 0, len));
            }
            return utils::utf8_encode(line);
        }

        bool DeleteSameChars(const BushuAssocEntryImpl* other) {
            bool bDirty = false;
            if (other) {
                for (auto x : other->list) {
                    for (size_t i = 0; i < list.size(); ++i) {
                        if (list[i] == x) {
                            list[i] = 0;
                            bDirty = true;
                        }
                    }
                }
            }
            return bDirty;
        }

        bool MergeChars(const BushuAssocEntryImpl* other) {
            bool bDirty = false;
            if (other) {
                for (auto x : other->list) {
                    if (x != 0) {
                        list.push_back(x);
                        bDirty = true;
                    }
                }
            }
            return bDirty;
        }

        // 合成辞書から連想入力リストを集めてくる
        bool GatherDerivedChars() {
            bool bDirty = false;
            if (!filled) {
                if (BUSHU_DIC) {
                    BUSHU_DIC->GatherDerivedMoji(key, list);
                    filled = true;
                    bDirty = true;
                }
            }
            return bDirty;
        }

        // 指定された tgt を選択する。存在しなければ末尾に追加する
        bool SelectTarget(mchar_t tgt) {
            bool bDirty = false;
            SelectNthTarget(findTarget(tgt), &bDirty);
            return bDirty;
        }

        // n番目の文字を選択して返す。選択されたものを固定位置の後の先頭に入れ替える
        mchar_t SelectNthTarget(size_t n, bool* pDirty) {
            if (n < list.size()) {
                mchar_t m = list[n];
                if (n > posFixed) { // 固定位置の直後なら移動の必要なし
                    for (size_t i = n; i > posFixed; --i) {
                        list[i] = list[i - 1];
                    }
                    list[posFixed] = m;
                    if (pDirty) *pDirty = true;
                }
                return m;
            }
            return 0;
        }

        // startPos 番から n 個の候補を文字列としてコピーする
        // startPos が list の範囲を超えていたら false を返す
        // bIncludeBar = true なら固定位置を示す '|' を挿入する
        bool CopySubList(std::vector<MString>& strList, size_t startPos, size_t n, bool bIncludeBar) {
            if (startPos >= list.size()) return false;

            size_t j = 0;
            for (size_t i = 0; i < n && j < strList.size(); ++i) {
                if (bIncludeBar && i == posFixed) {
                    strList[j++] = to_mstr('|');
                    if (j >= strList.size()) break;
                }
                if (startPos + i < list.size()) {
                    strList[j] = to_mstr(list[startPos + i]);
                } else {
                    strList[j].clear();
                }
                ++j;
            }
            return true;
        }

    }; // class BushuAssocEntryImpl


    // -------------------------------------------------------------------
    // 部首連想入力辞書実装クラス
    class BushuAssocDicImpl : public BushuAssocDic {
    private:
        DECLARE_CLASS_LOGGER;

        std::map<mchar_t, BushuAssocEntryImpl*> bscEntries;

        bool bDirty = false;

    public:
        BushuAssocDicImpl() { }

        ~BushuAssocDicImpl() {
            for (const auto& pair : bscEntries) {
                delete pair.second;
            }
        }


        /**
        * 部首連想入力辞書ファイルの読み込み
        * 定義は一行に以下のような形式で記述する:
        *   K[=]AB|C...
        * 文字 K に対して、その変換候補文字 A, B, C, ... が表示され、ヒストリ機能と同じように選択する。
        * "=" は省略可。
        * "|" の前までは位置固定。"|"の後は選択されるごとに順序が変わる。"|"が無ければ末尾にあると仮定。
        * 先頭が # ならコメント行。
        * 空行は無視。
        */
        void ReadFile(const std::vector<String>& lines) {
            for (auto& _ln : lines) {
                auto line = utils::strip(_ln);
                if (line.empty() || isDelim(line[0]) || line[0] == '#') continue;   // 空行や # で始まる行は読み飛ばす

                BushuAssocEntryImpl* entp = new BushuAssocEntryImpl();
                entp->ReadLine(line);
                bscEntries[entp->GetKey()] = entp;
            }
            // ReadFileの直後ならクリーン状態である(辞書保存は不要)
            bDirty = false;
        }

    public:
        bool IsEmpty() const {
            return bscEntries.empty();
        }

        bool IsDirty() const {
            return bDirty;
        }

        // 辞書ファイルの内容を既に読み込んだリストにマージする
        void MergeFile(const std::vector<String>& lines) {
            // 現状のものを保存
            std::map<mchar_t, BushuAssocEntryImpl*> tempEntries;
            std::copy(bscEntries.begin(), bscEntries.end(), std::inserter(tempEntries, tempEntries.end()));

            // ファイルの内容を読み込む
            bscEntries.clear();
            ReadFile(lines);

            // 読み込んだものと同じ文字を削除しておく
            for (const auto& pair : bscEntries) {
                mchar_t m = pair.first;
                auto iter = tempEntries.find(m);
                if (iter != tempEntries.end()) {
                    if (iter->second->DeleteSameChars(pair.second)) {
                        bDirty = true;
                    }
                }
            }
            // 残った文字をコピーする
            for (auto& pair : tempEntries) {
                mchar_t ch = pair.first;
                auto iter = bscEntries.find(ch);
                if (iter != bscEntries.end()) {
                    if (iter->second->MergeChars(pair.second)) {
                        bDirty = true;
                    }
                } else {
                    // ファイルに記述されていないものだったので、戻しておく
                    // @fixme: 本当に削除したいときにどうするか
                    bscEntries[ch] = pair.second;
                    pair.second = nullptr;  // 下のクリーンアップで削除されないように
                }
            }
            // 残ったゴミをクリーンアアップ
            for (const auto& pair : tempEntries) delete pair.second;

            bDirty = true;
        }

        // ファイルへの保存
        void WriteFile(utils::OfstreamWriter& writer) {
            for (const auto& pair : bscEntries) {
                if (pair.second) {
                    writer.writeLine(pair.second->MakeDicLine());
                }
            }
            bDirty = false;
        }

        // 1エントリのマージ
        void MergeEntry(StringRef ln) {
            LOG_INFO(_T("CALLED: ln={}"), ln);
            auto line = utils::strip(ln);
            if (line.empty() || isDelim(line[0]) || line[0] == '#') return;   // 空行や # で始まる行は無視

            auto entp = new BushuAssocEntryImpl();
            entp->ReadLine(line);
            auto iter = bscEntries.find(entp->GetKey());
            if (iter == bscEntries.end()) {
                bscEntries[entp->GetKey()] = entp;
            } else {
                delete iter->second;
                iter->second = entp;
            }
            bDirty = true;
        }

        BushuAssocEntry* GetEntry(mchar_t k) {
            BushuAssocEntryImpl* entp = 0;

            auto iter = bscEntries.find(k);
            if (iter == bscEntries.end()) {
                entp = new BushuAssocEntryImpl(k);
                bscEntries[k] = entp;
                bDirty = true;
            } else {
                entp = iter->second;
            }
            // 合成辞書から連想入力リストを集めてくる
            if (entp->GatherDerivedChars()) {
                bDirty = true;
            }

            return entp;
        }

        // 部首連想辞書エントリの候補の選択
        void SelectTarget(mchar_t k, mchar_t t) {
            auto entp = GetEntry(k);
            if (entp) {
                if (entp->SelectTarget(t)) {
                    bDirty = true;
                }
            }
        }
    };
    DEFINE_CLASS_LOGGER(BushuAssocDicImpl);

} // namespace

// -------------------------------------------------------------------
DEFINE_CLASS_LOGGER(BushuAssocDic);

std::unique_ptr<BushuAssocDic> BushuAssocDic::Singleton;

// 部首連想辞書の読み込み(ファイルが指定されていなくても、辞書は構築する)
int BushuAssocDic::CreateBushuAssocDic(StringRef bushuAssocFile) {
    LOG_INFO(_T("ENTER"));

    if (Singleton != 0) {
        LOG_INFO(_T("already created: bushu file: {}"), bushuAssocFile);
        return 0;
    }

    // 辞書ファイルが無くても辞書インスタンスは作成する
    Singleton.reset(new BushuAssocDicImpl());

    if (!bushuAssocFile.empty()) {
        LOG_INFO(_T("open bushu assoc file: {}"), bushuAssocFile);

        utils::IfstreamReader reader(bushuAssocFile);
        if (reader.success()) {
            Singleton->ReadFile(reader.getAllLines());
            LOG_INFO(_T("close bushu assoc file: {}"), bushuAssocFile);
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_WARN(_T("Can't read bushu assoc file: {}"), bushuAssocFile);
            ERROR_HANDLER->Warn(std::format(_T("部首連想入力辞書ファイル({})が開けません"), bushuAssocFile));
        }
    }

    LOG_INFO(_T("LEAVE"));
    return 0;
}

// 部首連想辞書ファイルを読み込んでマージする
void BushuAssocDic::MergeBushuAssocDic(StringRef path) {
    LOG_INFO(_T("CALLED: path={}"), path);
    if (!path.empty() && Singleton) {
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            Singleton->MergeFile(reader.getAllLines());
            LOG_INFO(_T("close bushu assoc file: {}"), path);
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_WARN(_T("Can't read bushu assoc file: {}"), path);
            ERROR_HANDLER->Warn(std::format(_T("部首連想入力辞書ファイル({})が開けません"), path));
        }
    }
}

// 部首連想辞書ファイルに書き込む
void BushuAssocDic::WriteBushuAssocDic(StringRef path) {
    LOG_INFO(_T("CALLED: path={}"), path);
    if (!path.empty() && Singleton) {
        if (Singleton->IsDirty() || SETTINGS->firstUse) {
            if (utils::moveFileToBackDirWithRotation(path, SETTINGS->backFileRotationGeneration)) {
                utils::OfstreamWriter writer(path);
                if (writer.success()) {
                    LOG_INFO(_T("WriteFile"));
                    Singleton->WriteFile(writer);
                }
            }
        }
    }
}

// 部首連想辞書ファイルに書き込む
void BushuAssocDic::WriteBushuAssocDic() {
    WriteBushuAssocDic(SETTINGS->bushuAssocFile);
}

