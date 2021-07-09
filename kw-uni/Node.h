#pragma once

#include "string_type.h"

class State;
class StateCommonInfo;

// -------------------------------------------------------------------
// ノードの型
enum class NodeType {
    Start,      // 開始ノード
    RootStroke, // ルートストロークテーブルノード
    Stroke,     // ストロークテーブルノード
    String,     // 文字列ノード
    Function   // 機能ノード
};

namespace {
    inline tstring nodeTypeName(NodeType nt) {
        switch (nt) {
        case NodeType::Start:
            return _T("StartNode");
        case NodeType::RootStroke:
            return _T("RootStroke");
        case NodeType::Stroke:
            return _T("Stroke");
        case NodeType::String:
            return _T("String");
        case NodeType::Function:
            return _T("Function");
        }
        return _T("None");
    }
}

#define NODE_NAME_PTR(n) (n == 0 ? _T("None") : nodeTypeName(n->getNodeType()).c_str())

// -------------------------------------------------------------------
// ノードの基底クラス (抽象クラス)
class Node {
public:
    virtual ~Node();

    // 当ノードを処理する State インスタンスを作成する
    virtual State* CreateState() = 0;

    // 表示用または出力用文字列を返す
    virtual MString getString() const = 0;

    // ノード型を返す
    virtual NodeType getNodeType() const = 0;

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
    inline bool isFunctionNode() const { return isNodeTypeOf(NodeType::Function); }

};

