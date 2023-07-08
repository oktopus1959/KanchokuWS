#pragma once

#include "Logger.h"

#include "FunctionNode.h"
#include "HistoryResidentState.h"

#if 0
#define HIST_LOG_DEBUGH LOG_INFOH
#else
#define HIST_LOG_DEBUGH(...) {}
#endif

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
// HistoryFewCharsNode - 2～3文字履歴機能ノード
class HistoryFewCharsNode : public HistoryNode {
    DECLARE_CLASS_LOGGER;
 public:
     HistoryFewCharsNode();

     ~HistoryFewCharsNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    MString getString() const { return to_mstr(_T("◇")); }
};

// -------------------------------------------------------------------
// HistoryFewCharsNodeBuilder - 2～3文字履歴機能ノードビルダ
#include "FunctionNodeBuilder.h"

class HistoryFewCharsNodeBuilder : public FunctionNodeBuilder {
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
// HistoryResidentNode - 履歴入力機能常駐ノード
class HistoryResidentNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
private:
    // 履歴選択により出力された文字列
    MString prevOutString;

    // 上記出力文字列を検索したときのキー文字列
    MString prevKey;

public:
     HistoryResidentNode();

     ~HistoryResidentNode();

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState();

    inline MString getString() const { return to_mstr(_T("◇")); }

    // 履歴選択により出力された文字列
    inline const MString& GetPrevOutString() const {
        return prevOutString;
    }

    // 選択された履歴に使われたキー
    inline const MString& GetPrevKey() const {
        return prevKey;
    }

    inline void SetPrevHistState(const MString& outStr, const MString& key /*, bool bPrevHistKeyUsed = true*/) {
        HIST_LOG_DEBUGH(_T("CALLED: outStr={}, key={}"), to_wstr(outStr), to_wstr(key));
        prevOutString = outStr;
        prevKey = key;
    }

    inline void SetPrevHistKeyState(const MString& key /*, bool bPrevHistKeyUsed = true*/) {
        HIST_LOG_DEBUGH(_T("CALLED: key={}"), to_wstr(key));
        prevOutString.clear();
        prevKey = key;
    }

    inline void ClearPrevHistState() {
        HIST_LOG_DEBUGH(_T("CALLED: ClearPrevHistState"));
        prevOutString.clear();
        prevKey.clear();
    }

public:
    // 履歴機能常駐ノードの生成
    static void CreateSingleton();

    // 履歴機能常駐ノードのSingleton
    static std::unique_ptr<HistoryResidentNode> Singleton;
};
#define HISTORY_RESIDENT_NODE (HistoryResidentNode::Singleton)
