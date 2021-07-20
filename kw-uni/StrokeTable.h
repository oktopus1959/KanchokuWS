//#include "pch.h"

#include "hotkey_id_defs.h"
#include "Reporting/Logger.h"
#include "Node.h"

// -------------------------------------------------------------------
// StrokeTableNode - ストロークテーブルの連鎖となるノード
class StrokeTableNode : public Node {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    StrokeTableNode(int depth) : _depth(depth) {
        children.resize(NUM_STROKE_HOTKEY);
    }

    // デストラクタ
    ~StrokeTableNode() {
    }

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 表示用文字列を返す
    MString getString() const { return to_mstr(_T("□")); }

    NodeType getNodeType() const { return _depth == 0 ? NodeType::RootStroke : NodeType::Stroke; }

public:
    /* StrokeTableNode 独自メソッド */

    // n番目の子ノードを返す
    inline Node* getNth(size_t n) const {
        return n < children.size() ? children[n].get() : 0;
    }

    //// 新しい子ノードを追加する
    //inline void addNode(Node* node) {
    //    children.push_back(std::unique_ptr<Node>(node));
    //}

    // n番目の子ノードをセットする
    inline void setNthChild(size_t n, Node* node) {
        if (n < children.size()) children[n].reset(node);
    }

public:
    // 子ノード数を返す
    inline size_t numChildren() const {
        return children.size();
    }

    // 木の根からの深さを返す
    inline size_t depth() const {
        return _depth;
    }

    // 全ストロークノードが不要になった
    inline void setToRemoveAllStroke() {
        bRemoveAllStroke = true;
    }

    // 全ストロークノードが不要になったか
    inline bool isToRemoveAllStroke() {
        return bRemoveAllStroke;
    }

private:
    std::vector<std::unique_ptr<Node>> children;

    size_t _depth;

    // 全ストロークノードが不要になったら true (ステートの作成時にクリアする)
    bool bRemoveAllStroke = false;

public:
    // ストローク木を構築する
    static StrokeTableNode* CreateStrokeTree(std::vector<tstring>&);

    // 機能の再割り当て
    static void AssignFucntion(const tstring& keys, const tstring& name);

    static std::unique_ptr<StrokeTableNode> RootStrokeNode;
};

#define ROOT_STROKE_NODE (StrokeTableNode::RootStrokeNode)

