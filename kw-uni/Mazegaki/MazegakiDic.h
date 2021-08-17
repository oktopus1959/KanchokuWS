#include "Logger.h"

// -------------------------------------------------------------------
struct MazeResult {
    // 交ぜ書き変換結果文字列
    MString resultStr;

    // 結果文字列の先頭の変換形文字列＋活用語尾の長さ
    size_t xferLen = 0;

public:
    MazeResult() { }

    MazeResult(const MString& str, size_t len) : resultStr(str), xferLen(len) { }


    static std::vector<MString> ToMStringVector(const std::vector<MazeResult>& results) {
        std::vector<MString> strvec;
        std::transform(results.begin(), results.end(), std::back_inserter(strvec), [](const MazeResult& mz) { return mz.resultStr;});
        return strvec;
    }
};

// 交ぜ書き辞書クラス
class MazegakiDic{
    DECLARE_CLASS_LOGGER;

public:
    // 仮想デストラクタ
    virtual ~MazegakiDic() { }

    // 作成された交ぜ書き辞書インスタンスにアクセスするための Singleton
    static std::unique_ptr<MazegakiDic> Singleton;

    // 交ぜ書き辞書インスタンスを生成する
    static int CreateMazegakiDic(const tstring&);

    // 交ぜ書き辞書ファイルを読み込む
    static void ReadMazegakiDic(const tstring& filename);

    // 交ぜ書き辞書ファイルに書き込む(SETTINGS->mazegakiFile)
    static void WriteMazegakiDic();

    static void WriteMazegakiDic(const tstring&);

    virtual bool AddMazeDicEntry(const wstring& line, bool bUser) = 0;

    // 指定の見出し語に対する変換候補のセットを取得する
    virtual const std::vector<MazeResult>& GetCandidates(const MString& key) = 0;

    // GetCandidates() が返した候補のうち target を持つものを選択してユーザー辞書にコピー
    virtual void SelectCadidate(const MString& target) = 0;

    // 指定の読みと変換形を持つユーザー辞書エントリを削除
    virtual void DeleteEntry(const wstring& yomi, const MString& xfer) = 0;

    // 交ぜ書き辞書が空か
    virtual bool IsEmpty() = 0;

    // 交ぜ書き辞書ファイルの読み込み
    virtual void ReadFile(const std::vector<wstring>& lines, bool bUser) = 0;

    // 辞書ファイルの内容の書き出し
    virtual void SaveUserDic(utils::OfstreamWriter&) = 0;
};

#define MAZEGAKI_DIC (MazegakiDic::Singleton)
