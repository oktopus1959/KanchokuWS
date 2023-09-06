#pragma once

#include "Node.h"
#include "OneShot/RewriteString.h"

// -------------------------------------------------------------------
// StringNode - 文字列を格納するノード
class StringNode : public Node {
    DECLARE_CLASS_LOGGER;

public:
    StringNode(StringRef s, bool bRewritable);

    StringNode(wchar_t ch);

    ~StringNode() { }

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 出力用文字列を返す
     MString getString() const override { return str; }

    size_t getRewritableLen() const override { return rewritableLen; }

    NodeType getNodeType() const override { return NodeType::String; }

    //bool isConverted() const { return bConverted; }

private:
    // 打鍵による出力文字列
    MString str;

    size_t rewritableLen;

    //// 裏面定義文字か
    //bool bConverted;
};

