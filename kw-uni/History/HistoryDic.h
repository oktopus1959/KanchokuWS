#pragma once

#include "file_utils.h"
#include "misc_utils.h"
#include "Logger.h"

// -------------------------------------------------------------------
// 履歴検索の出力クラス
struct HistResult {
    MString Key;
    MString Word;
    bool WildKey = false;
    size_t KeyLen() const { return Key.size(); }
};

class HistResultList {
    std::vector<MString> histories;
    MString histKey;
    bool isWildKey = false;

    MString emptyStr;

public:
    void Clear() {
        histories.clear();
        histKey.clear();
        isWildKey = false;
    }

    void SetKeyInfo(const MString& key, bool bWild = false) {
        histKey = key;
        isWildKey = bWild;
    }

    const std::vector<MString>& GetHistories() const {
        return histories;
    }

    void PushHistory(const MString& hist) {
        histories.push_back(hist);
    }

    const MString& GetNthWord(size_t n) const {
        return n < histories.size() ? histories[n] : emptyStr;
    }

    const HistResult GetNthHist(size_t n) const {
        return HistResult{ histKey, GetNthWord(n), isWildKey };
    }

    size_t Size() const { return histories.size(); }

    bool Empty() const { return Size() == 0; }

    void Append(const HistResultList& list) {
        utils::append(histories, list.histories);
    }

    // 最短語を少なくとも先頭から2番目に移動する
    void MoveShortestHistAt2nd() {
        size_t shortestIdx = 0;
        size_t shortestLen = size_t(-1);
        for (size_t i = 0; i < Size(); ++i) {
            if (histories[i].size() < shortestLen) {
                shortestIdx = i;
                shortestLen = histories[i].size();
            }
        }
        if (shortestIdx > 1) {
            auto elem = histories[shortestIdx];
            histories.erase(histories.begin() + shortestIdx);
            histories.insert(histories.begin() + 1, elem);
        }
    }
};

// -------------------------------------------------------------------
// 履歴入力辞書クラス
class HistoryDic{
    DECLARE_CLASS_LOGGER;

public:
    // 仮想デストラクタ
    virtual ~HistoryDic() { }

    // 作成された履歴入力辞書インスタンスにアクセスするための Singleton
    static std::unique_ptr<HistoryDic> Singleton;

    // 履歴入力辞書インスタンスを生成する
    static int CreateHistoryDic(const tstring&);

    // 辞書ファイルへの内容の書き出し
    static void WriteHistoryDic();

    // 辞書ファイルへの内容の書き出し
    static void WriteHistoryDic(const tstring&);

    // 履歴入力辞書ファイルの読み込み
    virtual void ReadFile(const std::vector<wstring>& lines) = 0;

    // 登録
    virtual void AddNewEntry(const MString& line) = 0;

    // 登録(条件なし)
    virtual void AddNewEntryAnyway(const MString& line) = 0;

    // Nグラム登録
    virtual void AddNgramEntries(const MString& line) = 0;

    // 登録済みNグラム集合をクリアする
    virtual void ClearNgramSet() = 0;

    // 指定の見出し文字のエントリを削除する
    virtual void DeleteEntry(const MString& word) = 0;

    // 指定の見出し文字に対する変換候補のセットを取得する
    virtual const HistResultList& GetCandidates(const MString& key, MString&, bool checkMinKeyLen, int len) = 0;

    // 単語の使用
    virtual void UseWord(const MString& word) = 0;

    // 指定の単語と先頭単語の入れ替え。指定単語が存在しなければ先頭に追加
    virtual void SwapWord(const MString& word) = 0;

    // 先頭単語を元の位置に戻す
    virtual void RevertWord() = 0;

    // 辞書ファイルの内容の書き出し
    virtual void WriteFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsHistDicDirty() const = 0;

    // 使用辞書の読み込み
    virtual void ReadUsedFile(const std::vector<wstring>& lines) = 0;

    // 使用辞書内容の保存
    virtual void WriteUsedFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsUsedDicDirty() const = 0;

    // 除外辞書の読み込み
    virtual void ReadExcludeFile(const std::vector<wstring>& lines) = 0;

    // 除外辞書内容の保存
    virtual void WriteExcludeFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsExcludeDicDirty() const = 0;

    // Nグラム辞書の読み込み
    virtual void ReadNgramFile(const std::vector<wstring>& lines) = 0;

    // Nグラム辞書内容の保存
    virtual void WriteNgramFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsNgramDicDirty() const = 0;
};

#define HISTORY_DIC (HistoryDic::Singleton)
