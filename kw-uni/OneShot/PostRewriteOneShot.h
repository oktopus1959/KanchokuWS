#pragma once

#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
class RewriteInfo {
public:
    MString rewriteStr;
    size_t rewritableLen;

    RewriteInfo() : rewriteStr(), rewritableLen(0) { }

    RewriteInfo(const RewriteInfo& info)
        : rewriteStr(info.rewriteStr), rewritableLen(info.rewritableLen)
    {
    }

    RewriteInfo(const MString& ms, size_t rewLen)
        : rewriteStr(ms), rewritableLen(rewLen)
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
};

// PostRewriteOneShotNode - ノードのテンプレート
class PostRewriteOneShotNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;

    std::map<MString, RewriteInfo> rewriteMap;

    RewriteInfo myRewriteInfo;
    //MString myStr;

    //size_t myRewriteLen;

    //MString emptyStr;

public:
    PostRewriteOneShotNode(const wstring& s, bool bBare);

    ~PostRewriteOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    MString getString() const { return myRewriteInfo.rewriteStr; }

    size_t getRewritableLen() const { return myRewriteInfo.rewritableLen; }

    const RewriteInfo& getRewriteInfo() const { return myRewriteInfo; }

    void addRewritePair(const wstring& key, const wstring& value, bool bBare);

    void addRewriteMap(const std::map<MString, RewriteInfo>& rewMap) {
        rewriteMap.insert(rewMap.begin(), rewMap.end());
    }

    const RewriteInfo* getRewriteInfo(const MString& key) const {
        auto iter = rewriteMap.find(key);
        return iter == rewriteMap.end() ? 0 : &(iter->second);
    }

    const std::map<MString, RewriteInfo>& getRewriteMap() const { return rewriteMap; }

    const wstring getDebugString() const;
};

// -------------------------------------------------------------------
// DakutenOneShotNode - ノードのテンプレート
class DakutenOneShotNode : public PostRewriteOneShotNode {
    DECLARE_CLASS_LOGGER;

    MString markStr;

    wstring postfix;

public:
    DakutenOneShotNode(wstring markStr);

    ~DakutenOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 当機能を表す文字を設定
    MString getString() const { return markStr; }

    wstring getPostfix() const { return postfix; }
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
