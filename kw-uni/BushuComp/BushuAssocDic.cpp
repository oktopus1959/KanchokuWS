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

        void ReadLine(const wstring& line) {
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
            wstring line;
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

        void DeleteSameChars(const BushuAssocEntryImpl* other) {
            if (other) {
                for (auto x : other->list) {
                    for (size_t i = 0; i < list.size(); ++i) {
                        if (list[i] == x) list[i] = 0;
                    }
                }
            }
        }

        void MergeChars(const BushuAssocEntryImpl* other) {
            if (other) {
                for (auto x : other->list) {
                    if (x != 0) list.push_back(x);
                }
            }
        }

        // 合成辞書から連想入力リストを集めてくる
        void GatherDerivedChars() {
            if (!filled) {
                if (BUSHU_DIC) BUSHU_DIC->GatherDerivedMoji(key, list);
                filled = true;
            }
        }

        // 指定された tgt を選択する。存在しなければ末尾に追加する
        void SelectTarget(mchar_t tgt) {
            SelectNthTarget(findTarget(tgt));
        }

        // n番目の文字を選択して返す。選択されたものを固定位置の後の先頭に入れ替える
        mchar_t SelectNthTarget(size_t n) {
            if (n < list.size()) {
                mchar_t m = list[n];
                if (n > posFixed) { // 固定位置の直後なら移動の必要なし
                    for (size_t i = n; i > posFixed; --i) {
                        list[i] = list[i - 1];
                    }
                    list[posFixed] = m;
                }
                return m;
            }
            return 0;
        }

        // startPos 番から n 個の候補を文字列としてコピーする
        // list の範囲を超えていたら false を返す
        bool CopySubList(std::vector<MString>& strList, size_t startPos, size_t n) {
            if (startPos >= list.size()) return false;

            for (size_t i = 0; i < n; ++i) {
                if (startPos + i < list.size()) {
                    strList[i] = to_mstr(list[startPos + i]);
                } else {
                    strList[i].clear();
                }
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
        void ReadFile(const std::vector<wstring>& lines) {
            for (auto& _ln : lines) {
                auto line = utils::strip(_ln);
                if (line.empty() || isDelim(line[0]) || line[0] == '#') continue;   // 空行や # で始まる行は読み飛ばす

                BushuAssocEntryImpl* entp = new BushuAssocEntryImpl();
                entp->ReadLine(line);
                bscEntries[entp->GetKey()] = entp;
            }
        }

    public:
        bool IsEmpty() const {
            return bscEntries.empty();
        }

        // 辞書ファイルの内容を既に読み込んだリストにマージする
        void MergeFile(const std::vector<wstring>& lines) {
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
                    iter->second->DeleteSameChars(pair.second);
                }
            }
            // 残った文字をコピーする
            for (auto& pair : tempEntries) {
                mchar_t ch = pair.first;
                auto iter = bscEntries.find(ch);
                if (iter != bscEntries.end()) {
                    iter->second->MergeChars(pair.second);
                } else {
                    // ファイルに記述されていないものだったので、戻しておく
                    // @fixme: 本当に削除したいときにどうするか
                    bscEntries[ch] = pair.second;
                    pair.second = nullptr;  // 下のクリーンアップで削除されないように
                }
            }
            // 残ったゴミをクリーンアアップ
            for (const auto& pair : tempEntries) delete pair.second;
        }

        // ファイルへの保存
        void WriteFile(utils::OfstreamWriter& writer) {
            for (const auto& pair : bscEntries) {
                if (pair.second) {
                    writer.writeLine(pair.second->MakeDicLine());
                }
            }
        }

        // 1エントリのマージ
        void MergeEntry(const wstring& ln) {
            LOG_INFO(_T("CALLED: ln=%s"), ln.c_str());
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
        }

        BushuAssocEntry* GetEntry(mchar_t k) {
            BushuAssocEntryImpl* entp = 0;

            auto iter = bscEntries.find(k);
            if (iter == bscEntries.end()) {
                entp = new BushuAssocEntryImpl(k);
                bscEntries[k] = entp;
            } else {
                entp = iter->second;
            }
            // 合成辞書から連想入力リストを集めてくる
            entp->GatherDerivedChars();

            return entp;
        }

        // 部首連想辞書エントリの候補の選択
        void SelectTarget(mchar_t k, mchar_t t) {
            auto entp = GetEntry(k);
            if (entp) {
                entp->SelectTarget(t);
            }
        }
    };
    DEFINE_CLASS_LOGGER(BushuAssocDicImpl);

} // namespace

// -------------------------------------------------------------------
DEFINE_CLASS_LOGGER(BushuAssocDic);

std::unique_ptr<BushuAssocDic> BushuAssocDic::Singleton;

// 部首連想辞書の読み込み(ファイルが指定されていなくても、辞書は構築する)
int BushuAssocDic::CreateBushuAssocDic(const tstring& bushuAssocFile) {
    LOG_INFO(_T("ENTER"));

    if (Singleton != 0) {
        LOG_INFO(_T("already created: bushu file: %s"), bushuAssocFile.c_str());
        return 0;
    }

    // 辞書ファイルが無くても辞書インスタンスは作成する
    Singleton.reset(new BushuAssocDicImpl());

    if (!bushuAssocFile.empty()) {
        LOG_INFO(_T("open bushu assoc file: %s"), bushuAssocFile.c_str());

        utils::IfstreamReader reader(bushuAssocFile);
        if (reader.success()) {
            Singleton->ReadFile(reader.getAllLines());
            LOG_INFO(_T("close bushu assoc file: %s"), bushuAssocFile.c_str());
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_WARN(_T("Can't read bushu assoc file: %s"), bushuAssocFile.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("部首連想入力辞書ファイル(%s)が開けません"), bushuAssocFile.c_str()));
        }
    }

    LOG_INFO(_T("LEAVE"));
    return 0;
}

// 部首連想辞書ファイルを読み込んでマージする
void BushuAssocDic::MergeBushuAssocDic(const tstring& path) {
    LOG_INFO(_T("CALLED: path=%s"), path.c_str());
    if (!path.empty() && Singleton) {
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            Singleton->MergeFile(reader.getAllLines());
            LOG_INFO(_T("close bushu assoc file: %s"), path.c_str());
        } else if (!SETTINGS->firstUse) {
            // エラーメッセージを表示
            LOG_WARN(_T("Can't read bushu assoc file: %s"), path.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("部首連想入力辞書ファイル(%s)が開けません"), path.c_str()));
        }
    }
}

// 部首連想辞書ファイルに書き込む
void BushuAssocDic::WriteBushuAssocDic(const tstring& path) {
    LOG_INFO(_T("CALLED: path=%s"), path.c_str());
    if (!path.empty() && Singleton) {
        if (!Singleton->IsEmpty() || SETTINGS->firstUse) {
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

