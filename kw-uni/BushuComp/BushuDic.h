#pragma once

#include "Logger.h"

namespace utils {
    class OfstreamWriter;
}

// -------------------------------------------------------------------
// 部首合成辞書クラス
class BushuDic{
    DECLARE_CLASS_LOGGER;
public:
    const wchar_t BUSHU_NULL = '\x01';

    const int TC_BUSHU_ALGO_OKA = 1;
    const int TC_BUSHU_ALGO_YAMANOBE = 2;

private:
    // 作成された部首合成辞書インスタンスにアクセスするための Singleton
    static std::unique_ptr<BushuDic> _singleton;

public:
    // 仮想デストラクタ
    virtual ~BushuDic() { }

    // 作成された部首合成辞書インスタンスにアクセスするための Singleton
    static BushuDic* Singleton();

    // 部首合成辞書インスタンスを生成する
    static int CreateBushuDic();

    // 部首合成辞書を読み込む
    static void ReadBushuDic(StringRef);

    // 部首合成辞書ファイルに書き込む
    static void WriteBushuDic(StringRef path);
    static void WriteBushuDic();

    // 部首合成エントリの追加
    virtual void AddBushuEntry(StringRef) = 0;
    virtual void MakeStrokableMap() = 0;

    // a と b を組み合わせてできる合成文字を探す。
    virtual mchar_t FindComposite(mchar_t ca, mchar_t cb, mchar_t prev = 0) = 0;

    virtual void GatherDerivedMoji(mchar_t m, std::vector<mchar_t>& list) = 0;

    // 自動部首合成辞書を読み込む
    static void ReadAutoBushuDic(StringRef);

    // 自動部首合成辞書ファイルに書き込む
    static void WriteAutoBushuDic(StringRef path);
    static void WriteAutoBushuDic();

    // 自動部首合成エントリの追加
    virtual void AddAutoBushuEntry(StringRef) = 0;
    virtual void AddAutoBushuEntry(mchar_t a, mchar_t b, mchar_t c) = 0;

    // a と b を組み合わせてできる自動合成文字を探す。
    virtual mchar_t FindAutoComposite(mchar_t ca, mchar_t cb) = 0;

    //仮想鍵盤に部首合成ヘルプの情報を設定する
    virtual bool CopyBushuCompHelpToVkbFaces(mchar_t ch, wchar_t* faces, size_t kbLen, size_t kbNum, bool bSetAssoc = false) = 0;

    //後置部首合成定義を書き出す
    virtual void ExportPostfixBushuCompDefs(utils::OfstreamWriter& writer, StringRef postfix) = 0;
};

#define BUSHU_DIC (BushuDic::Singleton())
