#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
struct RewriteInfo {
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
};

// PostRewriteOneShotNode - ノードのテンプレート
class PostRewriteOneShotNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;

    std::map<MString, RewriteInfo> rewriteMap;

    MString myStr;

    size_t myRewriteLen;

    MString emptyStr;

public:
    PostRewriteOneShotNode(const wstring& s, bool bBare);

    ~PostRewriteOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    MString getString() const { return myStr; }

    size_t getRewritableLen() const { return myRewriteLen; }

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


