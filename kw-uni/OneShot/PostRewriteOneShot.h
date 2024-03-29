#pragma once

#include "Logger.h"

#include "FunctionNode.h"
#include "StrokeTable.h"

// -------------------------------------------------------------------
// 書き換え情報
class RewriteInfo {
public:
    MString rewriteStr;
    size_t rewritableLen;
    StrokeTableNode* subTable = 0;

    RewriteInfo() : rewriteStr(), rewritableLen(0) { }

    RewriteInfo(const RewriteInfo& info)
        : rewriteStr(info.rewriteStr), rewritableLen(info.rewritableLen), subTable(info.subTable)
    {
    }

    RewriteInfo(const MString& ms, size_t rewLen, StrokeTableNode* pn)
        : rewriteStr(ms), rewritableLen(rewLen), subTable(pn)
    {
    }

    size_t getOutStrLen() const {
        return rewritableLen >= rewriteStr.size() ? 0 : rewriteStr.size() - rewritableLen;
    }
    MString getOutStr() const {
        return utils::safe_substr(rewriteStr, 0, getOutStrLen());
    }

    MString getNextStr() const {
        return utils::safe_substr(rewriteStr, getOutStrLen());
    }

    String getDebugStr() const {
        return to_wstr(getOutStr()) + _T("/") + to_wstr(getNextStr());
    }
};

// PostRewriteOneShotNode
class PostRewriteOneShotNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;

    // 書き換え情報マップ -- 前接する書き換え対象文字列がキーとなる
    std::map<MString, RewriteInfo> rewriteMap;

    RewriteInfo myRewriteInfo;

    // 生存管理のためのvector
    std::vector<StrokeTableNode*> subTables;

public:
    PostRewriteOneShotNode(StringRef s, bool bBare);

    ~PostRewriteOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    NodeType getNodeType() const override { return NodeType::Rewrite; }

    MString getString() const override { return myRewriteInfo.rewriteStr; }

    size_t getRewritableLen() const override { return myRewriteInfo.rewritableLen; }

    const RewriteInfo& getRewriteInfo() const { return myRewriteInfo; }

    void clearRewriteString() { myRewriteInfo.rewriteStr.clear(); }

    void addRewritePair(StringRef key, StringRef value, bool bBare, StrokeTableNode* pNode);

    void merge(PostRewriteOneShotNode& rewNode) {
        rewriteMap.insert(rewNode.rewriteMap.begin(), rewNode.rewriteMap.end());
        rewNode.rewriteMap.clear();
        utils::append(subTables, rewNode.subTables);
        rewNode.subTables.clear();
    }

    const RewriteInfo* getRewriteInfo(const MString& key) const {
        auto iter = rewriteMap.find(key);
        return iter == rewriteMap.end() ? 0 : &(iter->second);
    }

    // 末尾文字列にマッチする RewriteInfo を取得する
    std::tuple<const RewriteInfo*, size_t> matchWithTailString() const;

    const std::map<MString, RewriteInfo>& getRewriteMap() const { return rewriteMap; }

    size_t getSubTableNum() const { return subTables.size(); }

    const String getDebugString() const;
};

// -------------------------------------------------------------------
// DakutenOneShotNode - ノードのテンプレート
class DakutenOneShotNode : public PostRewriteOneShotNode {
    DECLARE_CLASS_LOGGER;

    MString markStr;

    String postfix;

public:
    DakutenOneShotNode(String markStr);

    ~DakutenOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return markStr; }

    String getPostfix() const { return postfix; }
};

// -------------------------------------------------------------------
// DakutenOneShotNodeBuilder - ノードビルダ
#include "FunctionNodeBuilder.h"

class DakutenOneShotNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

class HanDakutenOneShotNodeBuilder : public FunctionNodeBuilder {
    DECLARE_CLASS_LOGGER;
public:
    // これの呼び出しを FunctionNodeBuilderList.h に記述する
    Node* CreateNode();
};

