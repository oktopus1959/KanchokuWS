#pragma once

#include "Logger.h"

#include "FunctionNode.h"
//#include "StrokeMerger/StrokeMergerHistoryResidentState.h"

#if 0
#define HIST_LOG_DEBUGH LOG_INFO
#else
#define HIST_LOG_DEBUGH(...) {}
#endif

// -------------------------------------------------------------------
// StrokeMergerHistoryNode - マージ入力履歴機能常駐ノード
class StrokeMergerHistoryNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
private:
    // マージ選択により出力された文字列
    MString prevOutString;

    // 上記出力文字列を検索したときのキー文字列
    MString prevKey;

public:
     StrokeMergerHistoryNode();

     ~StrokeMergerHistoryNode() override;

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState() override;

    MString getString() const override { return to_mstr(_T("∈")); }

    String getNodeName() const { return _T("StrokeMergerNode"); }

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
    void createStrokeTrees(bool bForceSecondary = false);

public:
    // マージ履歴機能ノードのSingleton
    static std::unique_ptr<StrokeMergerHistoryNode> Singleton;

    // マージ履歴機能ノードの生成
    static void CreateSingleton();

    // マージ履歴機能ノードの初期化
    static void Initialize();

};
#define STROKE_MERGER_NODE (StrokeMergerHistoryNode::Singleton)

