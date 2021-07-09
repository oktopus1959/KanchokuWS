#pragma once

#include "Logger.h"

#include "FunctionNode.h"
#include "HistoryStayState.h"

// -------------------------------------------------------------------
// HistoryNode - 履歴入力機能ノード
class HistoryNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
 public:
     HistoryNode();

     ~HistoryNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("◆")); }

public:
    static HistoryNode* Singleton;
};
#define HISTORY_NODE (HistoryNode::Singleton)

// -------------------------------------------------------------------
// HistoryNodeBuilder - 履歴入力機能ノードビルダ
#include "FunctionNodeBuilder.h"

class HistoryNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

// -------------------------------------------------------------------
// HistoryOneCharNode - 1文字履歴機能ノード
class HistoryOneCharNode : public HistoryNode {
    DECLARE_CLASS_LOGGER;
 public:
     HistoryOneCharNode();

     ~HistoryOneCharNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("◇")); }
};

// -------------------------------------------------------------------
// HistoryOneCharNodeBuilder - 1文字履歴機能ノードビルダ
#include "FunctionNodeBuilder.h"

class HistoryOneCharNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    Node* CreateNode();
};

// -------------------------------------------------------------------
// HistoryStayNode - 履歴入力機能常駐ノード
class HistoryStayNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
 public:
     HistoryStayNode();

     ~HistoryStayNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("◇")); }

    // 履歴選択により出力された文字列
    MString prevOutString;
    // 履歴選択により文字列出力されたときのキー(デコーダONになったら -1 で初期化)
    int prevKeyLen = -1;

    // 履歴候補表示時に使われたキー(必ずしも履歴出力を必要とはしないので、上記の prevOutStringとprevKeyLenから得られるものとは異なる場合あり)
    MString prevKey;

public:
    // 履歴機能常駐ノードの生成
    static HistoryStayNode* CreateNode();

    // 履歴機能常駐ノードのSingleton
    static std::unique_ptr<HistoryStayNode> Singleton;
};
#define HISTORY_STAY_NODE (HistoryStayNode::Singleton)

