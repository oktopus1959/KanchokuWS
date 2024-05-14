#pragma once

#include "Logger.h"

#include "FunctionNode.h"
//#include "ResidentState.h"

// -------------------------------------------------------------------
// StrokeMergerNode - マージ入力機能常駐ノード
class StrokeMergerNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;
private:
    // マージ選択により出力された文字列
    MString prevOutString;

    // 上記出力文字列を検索したときのキー文字列
    MString prevKey;

public:
     StrokeMergerNode();

     ~StrokeMergerNode() override;

    // 当ノードを処理する State インスタンスを作成する
     State* CreateState() override;

    MString getString() const override { return to_mstr(_T("∈")); }

    String getNodeName() const { return _T("StrokeMergerNode"); }

public:
    void createStrokeTrees(bool bForceSecondary = false);

public:
    // マージ機能ノードの生成
    static void CreateSingleton();

    // マージ機能ノードのSingleton
    static std::unique_ptr<StrokeMergerNode> Singleton;
};
#define STROKE_MERGER_NODE (StrokeMergerNode::Singleton)

#if 0
// -------------------------------------------------------------------
// マージ入力(常駐)機能状態(抽象)クラス
class MergerResidentState : public ResidentState {
public:
    // 文字列を変換して出力、その後、マージの追加
    virtual void SetTranslatedOutString(const MString& outStr, size_t rewritableLen, int numBS = -1) = 0;

    void runTest();

public:
    // 唯一のインスタンスを指すポインタ (寿命管理は CreateState() を呼び出したところがやる)
    static MergerResidentState* Singleton;
};

#define MERGER_RESIDENT_STATE (MergerResidentState::Singleton)
#endif
