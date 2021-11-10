//#include "pch.h"

#include "deckey_id_defs.h"
#include "Reporting/Logger.h"
#include "Node.h"
#include "VkbTableMaker.h"

// -------------------------------------------------------------------
// StrokeTableNode - ストロークテーブルの連鎖となるノード
class StrokeTableNode : public Node {
    DECLARE_CLASS_LOGGER;

public:
    // コンストラクタ
    StrokeTableNode(int depth) : _depth(depth) {
        children.resize(STROKE_DECKEY_NUM);     // normalキーとshift修飾キーの両方のキーの分を確保しておく
    }

    StrokeTableNode(int depth, size_t numChildren) : _depth(depth) {
        children.resize(numChildren);
    }

    // デストラクタ
    ~StrokeTableNode() {
    }

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 表示用文字列を返す
    MString getString() const { return to_mstr(nodeMarker); }

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

    wstring nodeMarker = _T("□");

    // 全ストロークノードが不要になったら true (ステートの作成時にクリアする)
    bool bRemoveAllStroke = false;

public:
    // ストロークガイドの構築
    void MakeStrokeGuide(const wstring& targetChars) {
        std::vector<wchar_t> strokeGuide(VkbTableMaker::OUT_TABLE_SIZE);
        VkbTableMaker::ReorderByStrokePosition(this, strokeGuide.data(), targetChars);
        for (size_t i = 0; i * 2 < strokeGuide.size() && i < children.size(); ++i) {
            auto ch = strokeGuide[i * 2];
            Node* child = children[i].get();
            if (ch != 0 && child && child->isStrokeTableNode()) {
                StrokeTableNode* tblNode = dynamic_cast<StrokeTableNode*>(child);
                if (tblNode) tblNode->nodeMarker[0] = ch;
            }
        }
    }

    // ストローク木を構築する
    static StrokeTableNode* CreateStrokeTree(std::vector<tstring>&);

    // ストローク木2を構築する
    static StrokeTableNode* CreateStrokeTree2(std::vector<tstring>&);

    // 機能の再割り当て
    static void AssignFucntion(const tstring& keys, const tstring& name);

    static std::unique_ptr<StrokeTableNode> RootStrokeNode1;
    static std::unique_ptr<StrokeTableNode> RootStrokeNode2;
    static StrokeTableNode* RootStrokeNode;
};

#define ROOT_STROKE_NODE (StrokeTableNode::RootStrokeNode)

