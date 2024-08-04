#pragma once

#include "file_utils.h"
#include "misc_utils.h"
#include "Logger.h"

// -------------------------------------------------------------------
// 検索された履歴候補のクラス
struct HistResult {
    MString OrigKey;        // 履歴検索の基となったキー (ex.「プログ」)
    MString Key;            // 当履歴候補のキー (ex.「ログ」)
    MString Word;           // 当履歴候補 (ex.「ログファイル」)
    bool WildKey = false;   // ワイルドカードを含むキーか
    size_t KeyLen() const { return Key.size(); }
};

// 検索された履歴候補リストのクラス
class HistResultList {
    std::vector<HistResult> histories;
    MString origKey;
    bool isWildKey = false;

    HistResult emptyResult;

private:
    // 履歴リストのサイズが10個以下なら、先頭から10個分の要素と比較する
    bool findSameEntry(const MString& word) {
        if (histories.size() < 10) {
            for (size_t i = 0; i < histories.size(); ++i) {
                if (histories[i].Word == word) return true;
            }
        }
        return false;
    }

public:
    void ClearKeyInfo() {
        histories.clear();
        origKey.clear();
        isWildKey = false;
    }

    // ClearKeyInfo() の直後のみ、基キーのセットをする
    void SetKeyInfoIfFirst(const MString& key, bool bWild = false) {
        if (origKey.empty()) {
            origKey = key;
            isWildKey = bWild;
        }
    }

    const MString& GetOrigKey() const {
        return origKey;
    }

    const std::vector<HistResult>& GetHistories() const {
        return histories;
    }

    void PushHistory(const MString& key, const MString& word) {
        auto result = HistResult{ origKey, key, utils::replace(word, MSTR_VERT_BAR_2, MSTR_VERT_BAR), isWildKey };
        if (!findSameEntry(word)) {
            histories.push_back(result);
        }
    }

    const MString& GetNthWord(size_t n) const {
        return GetNthHist(n).Word;

    }

    const HistResult& GetNthHist(size_t n) const {
        return n < histories.size() ? histories[n] : emptyResult;
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
            if (histories[i].Word.size() < shortestLen) {
                shortestIdx = i;
                shortestLen = histories[i].Word.size();
            }
        }
        if (shortestIdx > 1) {
            auto elem = histories[shortestIdx];
            histories.erase(histories.begin() + shortestIdx);
            histories.insert(histories.begin() + 1, elem);
        }
    }

    // 同じ履歴変換キーを探す
    const HistResult& findSameHistMapKey(const MString& key) {
        for (const auto& hr : histories) {
            if (key == hr.Key && hr.Word.size() > key.size() && hr.Word[key.size()] == VERT_BAR) return hr;
        }
        return emptyResult;
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
    static int CreateHistoryDic(const String&);

    // 辞書ファイルへの内容の書き出し
    static void WriteHistoryDic();

    // 辞書ファイルへの内容の書き出し
    static void WriteHistoryDic(const String&);

    // 履歴入力辞書ファイルの読み込み
    virtual void ReadFile(const std::vector<String>& lines) = 0;

    // 履歴入力辞書ファイルの読み込み(読み込み専用辞書)
    virtual void ReadRomanFileAsReadOnly(const std::vector<String>& lines) = 0;

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

    //// 指定の単語と先頭単語の入れ替え。指定単語が存在しなければ先頭に追加
    //virtual void SwapWord(const MString& word) = 0;

    //// 先頭単語を元の位置に戻す
    //virtual void RevertWord() = 0;

    // 辞書ファイルの内容の書き出し
    virtual void WriteFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsHistDicDirty() const = 0;

    // 使用辞書の読み込み
    virtual void ReadUsedFile(const std::vector<String>& lines) = 0;

    // 使用辞書内容の保存
    virtual void WriteUsedFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsUsedDicDirty() const = 0;

    // 除外辞書の読み込み
    virtual void ReadExcludeFile(const std::vector<String>& lines) = 0;

    // 除外辞書内容の保存
    virtual void WriteExcludeFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsExcludeDicDirty() const = 0;

    // Nグラム辞書の読み込み
    virtual void ReadNgramFile(const std::vector<String>& lines) = 0;

    // Nグラム辞書内容の保存
    virtual void WriteNgramFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsNgramDicDirty() const = 0;
};

#define HISTORY_DIC (HistoryDic::Singleton)
