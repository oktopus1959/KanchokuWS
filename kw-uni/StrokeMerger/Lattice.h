#pragma once

#include "string_utils.h"

#include "FunctionNode.h"
#include "Logger.h"

#if 0
// -------------------------------------------------------------------
// LatticeState - ラティス
class LatticeState {
    DECLARE_CLASS_LOGGER;
public:
    LatticeState();

    ~LatticeState();

public:
    // 当ノードを処理する State インスタンスを作成する
    static void CreateSingleton();

    static std::unique_ptr<LatticeState> latticeState;
};
#define LATTICE_STATE (LatticeState::latticeState)
#endif

// -------------------------------------------------------------------
// 単語素片と、それの出力にかかった打鍵数
struct WordPiece {
    // 単語素片
    MString pieceStr;
    // 打鍵数
    size_t strokeLen;
    // 書き換え対象文字数
    size_t rewritableLen;
    // 削除文字数
    int numBS;

    WordPiece(const MString& ms, size_t len, size_t rewLen, int nBS = -1)
        : pieceStr(ms), strokeLen(len), rewritableLen(rewLen), numBS(nBS) {
    }

    String toString() const {
        return _T("(") + to_wstr(pieceStr) + _T(", stroke=") + std::to_wstring(strokeLen) + _T(", rewLen=") + std::to_wstring(rewritableLen) + _T(", numBS=") + std::to_wstring(numBS) + _T(")");
    }
};

// ラティスから取得した文字列と、修正用のBS数
struct LatticeResult {
    MString outStr;
    size_t numBS;

    LatticeResult(const MString& s, size_t n)
        : outStr(s), numBS(n) {
    }
};

// Lattice
class Lattice {
public:
    // デストラクタ
    virtual ~Lattice() { }

    // 単語素片リストの追加(単語素片が得られなかった場合も含め、各打鍵ごとに呼び出すこと)
    // 単語素片(WordPiece): 打鍵後に得られた出力文字列と、それにかかった打鍵数
    // return: 出力文字列と、修正用のBS数
    virtual LatticeResult addPieces(const std::vector<WordPiece>& pieces) = 0;

    virtual void clear() = 0;

    static void createLattice();

    static std::unique_ptr<Lattice> Singleton;

    //static void loadCostFile();

    static void runTest();
};

#define WORD_LATTICE Lattice::Singleton

