#pragma once

#include "string_type.h"
#include "Logger.h"

class State;
class AbstractStrokeState;
class StateCommonInfo;

// -------------------------------------------------------------------
// ノードの型
enum class NodeType {
    Start,      // 開始ノード
    RootStroke, // ルートストロークテーブルノード
    Stroke,     // ストロークテーブルノード
    String,     // 文字列ノード
    FunctionT,   // 機能ノード
    Rewrite,    // 書き換えノード
    None,
};

namespace {
    inline String nodeTypeName(NodeType nt) {
        switch (nt) {
        case NodeType::Start:
            return _T("StartNode");
        case NodeType::RootStroke:
            return _T("RootStroke");
        case NodeType::Stroke:
            return _T("Stroke");
        case NodeType::String:
            return _T("String");
        case NodeType::FunctionT:
            return _T("Function");
        case NodeType::Rewrite:
            return _T("Rewrite");
        }
        return _T("None");
    }
}

//#define NODE_NAME(n) (n == 0 ? _T("None") : nodeTypeName(n->getNodeType()))
#define NODE_NAME(n) (n == 0 ? _T("None") : n->getNodeName())

// -------------------------------------------------------------------
// ノードの基底クラス (抽象クラス)
class Node {
    DECLARE_CLASS_LOGGER;

public:
    virtual ~Node();

    // 当ノードを処理する State インスタンスを作成する
    virtual State* CreateState() = 0;

    // StrokeStateインスタンスの作成
    virtual AbstractStrokeState* CreateStrokeState() { return nullptr; }

    // 表示用または出力用文字列を返す
    virtual MString getString() const = 0;

    // Google日本語入力における「次の入力」に相当する文字列の長さを返す
    virtual size_t getRewritableLen() const { return 0; }

    // ノード型を返す
    virtual NodeType getNodeType() const = 0;

    // ノード名を返す
    virtual String getNodeName() const = 0;

public:
    // ノード型を判定する
    inline bool isNodeTypeOf(NodeType nodeType) const { return getNodeType() == nodeType; }

    // 開始ノードか
    inline bool isStartNode() const { return isNodeTypeOf(NodeType::Start); };

    // 文字列ノードか
    inline bool isStringNode() const { return isNodeTypeOf(NodeType::String); };

    // 始ストロークノードか
    inline bool isRootStrokeTableNode() const { return isNodeTypeOf(NodeType::RootStroke); }

    // ストロークノードか
    inline bool isStrokeTableNode() const { return isRootStrokeTableNode() || isNodeTypeOf(NodeType::Stroke); }

    // 機能ノードか
    inline bool isFunctionNode() const { return isNodeTypeOf(NodeType::FunctionT); }

    // 書き換えノードか
    inline bool isRewriteNode() const { return isNodeTypeOf(NodeType::Rewrite); }

    // 文字列ノードまたは書き換えノードか
    inline bool isStringLikeNode() const { return isStringNode() || isRewriteNode(); };

};

