#pragma once

#include "file_utils.h"
#include "Logger.h"

// -------------------------------------------------------------------
// 履歴検索の出力クラス
struct HistResult {
    size_t KeyLen = 0;
    MString Word;
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
    virtual const std::vector<HistResult>& GetCandidates(const MString& key, MString&, bool checkMinKeyLen, size_t n) = 0;

    // 単語の使用
    virtual void UseWord(const MString& word) = 0;

    // 指定の単語と先頭単語の入れ替え。指定単語が存在しなければ先頭に追加
    virtual void SwapWord(const MString& word) = 0;

    // 先頭単語を元の位置に戻す
    virtual void RevertWord() = 0;

    // 辞書ファイルの内容の書き出し
    virtual void WriteFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsHistDicEmpty() const = 0;

    // 使用辞書の読み込み
    virtual void ReadUsedFile(const std::vector<wstring>& lines) = 0;

    // 使用辞書内容の保存
    virtual void WriteUsedFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsUsedDicEmpty() const = 0;

    // 除外辞書の読み込み
    virtual void ReadExcludeFile(const std::vector<wstring>& lines) = 0;

    // 除外辞書内容の保存
    virtual void WriteExcludeFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsExcludeDicEmpty() const = 0;

    // Nグラム辞書の読み込み
    virtual void ReadNgramFile(const std::vector<wstring>& lines) = 0;

    // Nグラム辞書内容の保存
    virtual void WriteNgramFile(utils::OfstreamWriter& writer) = 0;

    virtual bool IsNgramDicEmpty() const = 0;
};

#define HISTORY_DIC (HistoryDic::Singleton)
