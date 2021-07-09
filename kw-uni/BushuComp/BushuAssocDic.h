#pragma once

#include "file_utils.h"
#include "Logger.h"

// -------------------------------------------------------------------
/** 部首連想入力リストのエントリのインターフェース */
class BushuAssocEntry {
public:
    // 連想リストの元となるキー文字を返す
    virtual mchar_t GetKey() const = 0;

    // startPos 番から n 個の候補を文字列としてコピーする。list の範囲を超えていたら false を返す
    virtual bool CopySubList(std::vector<MString>&, size_t startPos, size_t n) = 0;

    // 指定された tgt を選択する。存在しなければ末尾に追加する
    virtual void SelectTarget(mchar_t tgt) = 0;

    // n番目の文字を選択して返す。選択されたものを固定位置の後の先頭に入れ替える
    virtual mchar_t SelectNthTarget(size_t n) = 0;

    virtual ~BushuAssocEntry() { }
};

// -------------------------------------------------------------------
// 部首連想入力辞書クラス
class BushuAssocDic {
    DECLARE_CLASS_LOGGER;
public:
    const mchar_t BUSHU_NULL = '\x01';

    const int TC_BUSHU_ALGO_OKA = 1;
    const int TC_BUSHU_ALGO_YAMANOBE = 2;

public:
    // 仮想デストラクタ
    virtual ~BushuAssocDic() { }

    // 作成された部首連想入力辞書インスタンスにアクセスするための Singleton
    static std::unique_ptr<BushuAssocDic> Singleton;

    // 部首連想入力辞書インスタンスを生成する
    static int CreateBushuAssocDic(const tstring&);

    // 部首連想辞書ファイルを読み込んでマージする
    static void MergeBushuAssocDic(const tstring&);

    // 部首連想辞書ファイルに書き込む(SETTINGS->bushuAssocFile)
    static void WriteBushuAssocDic();

    // 部首連想辞書ファイルに書き込む
    static void WriteBushuAssocDic(const tstring&);

    // 部首連想辞書が空か
    virtual bool IsEmpty() const = 0;

    // 部首連想辞書エントリの取得
    virtual BushuAssocEntry* GetEntry(mchar_t ch) = 0;
    //virtual void gatherDerivedMoji(wchar_t m, std::vector<wchar_t>& list) = 0;

    // 部首連想辞書エントリの候補の選択
    virtual void SelectTarget(mchar_t k, mchar_t t) = 0;

    // 部首連想入力辞書ファイルの読み込み
    virtual void ReadFile(const std::vector<wstring>& lines) = 0;

    // 辞書ファイルの内容を既に読み込んだリストにマージする
    virtual void MergeFile(const std::vector<wstring>& lines) = 0;

    // 辞書ファイルの内容の書き出し
    virtual void WriteFile(utils::OfstreamWriter&) = 0;

    // 1エントリのマージ
    virtual void MergeEntry(const wstring& line) = 0;
};

// 部首連想辞書のシングルトン
#define BUSHU_ASSOC_DIC (BushuAssocDic::Singleton)
