//#include "../pch.h"
#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// MazegakiNode - 交ぜ書き機能ノード
class MazegakiNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
 public:
     MazegakiNode();

     ~MazegakiNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("○")); }

    void SetYomiInfo(const MString& yomi, size_t xferLen, size_t count) {
        prevYomi = yomi;
        prevXferLen = xferLen;
        hotkeyCount = count;
    }

    size_t GetPrevYomiInfo(MString& yomi, size_t count) {
        if (count == hotkeyCount + 1) {
            yomi = prevYomi;
            return prevXferLen;
        }
        return 0;
    }

private:
    // 変換結果を元に戻すための変換前の読み
    MString prevYomi;

    // 変換結果を元に戻すための変換形の長さ
    size_t prevXferLen = 0;

    // 前回変換時のホットキーカウント
    size_t hotkeyCount = 0;

public:
    static MazegakiNode* Singleton;
};
#define MAZEGAKI_NODE (MazegakiNode::Singleton)

// -------------------------------------------------------------------
// MazegakiNodeBuilder - 交ぜ書き機能ノードビルダ
#include "FunctionNodeBuilder.h"

class MazegakiNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

