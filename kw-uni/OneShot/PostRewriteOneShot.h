#include "Logger.h"

#include "FunctionNode.h"

// -------------------------------------------------------------------
// PostRewriteOneShotNode - ノードのテンプレート
class PostRewriteOneShotNode : public FunctionNode {
    DECLARE_CLASS_LOGGER;

    std::map<MString, MString> rewriteMap;

    MString myStr;

    MString emptyStr;

public:
    PostRewriteOneShotNode(const wstring& s);

    ~PostRewriteOneShotNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    MString getString() const { return myStr; }

    void addRewritePair(const MString& key, const MString& value) { rewriteMap[key] = value; }

    const MString& getRewriteStr(const MString& key) {
        auto iter = rewriteMap.find(key);
        return iter == rewriteMap.end() ? emptyStr : iter->second;
    }
};


