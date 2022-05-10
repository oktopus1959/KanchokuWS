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
        //children.resize(STROKE_DECKEY_NUM);     // normalキーとshift修飾キーの両方のキーの分を確保しておく
        children.resize(NORMAL_DECKEY_NUM);     // normalキーだけの分を確保しておけば十分である
    }

    StrokeTableNode(int depth, size_t numChildren) : _depth(depth) {
        children.resize(numChildren);
    }

    // デストラクタ
    ~StrokeTableNode() {
        for (auto p : children) {
            delete p;       // 子ノードの削除 (デストラクタ)
        }
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
        //return n < children.size() ? children[n].get() : 0;
        return n < children.size() ? children[n] : 0;
    }

    //// 新しい子ノードを追加する
    //inline void addNode(Node* node) {
    //    children.push_back(std::unique_ptr<Node>(node));
    //}

    // n番目の子ノードをセットする
    inline void setNthChild(size_t n, Node* node) {
        //if (n < children.size()) children[n].reset(node);
        if (n < children.size()) {
            if (children[n]) {
                delete children[n];     // 新しい n番目の子ノードをセットするために、既存のものを削除しておく
            }
            children[n] = node;
        }
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
    //std::vector<std::unique_ptr<Node>> children;
    std::vector<Node*> children;

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
            //Node* child = children[i].get();
            Node* child = getNth(i);
            if (ch != 0 && child && child->isStrokeTableNode()) {
                StrokeTableNode* tblNode = dynamic_cast<StrokeTableNode*>(child);
                if (tblNode) tblNode->nodeMarker[0] = ch;
            }
        }
    }

    // ストローク木を構築する
    static StrokeTableNode* CreateStrokeTree(const wstring&, std::vector<wstring>&);

    // ストローク木2を構築する
    static StrokeTableNode* CreateStrokeTree2(const wstring&, std::vector<wstring>&);

    // 機能の再割り当て
    static void AssignFucntion(const wstring& keys, const wstring& name);

    // ストロークノードの更新
    static void UpdateStrokeNodes(const wstring& strokeSource);

    // ストローク木の入れ替え
    static int ExchangeStrokeTable();

    // 現在のストローク木の番号
    static int GetCurrentStrokeTableNum();

    static std::unique_ptr<StrokeTableNode> RootStrokeNode1;
    static std::unique_ptr<StrokeTableNode> RootStrokeNode2;
    static StrokeTableNode* RootStrokeNode;
};

#define ROOT_STROKE_NODE (StrokeTableNode::RootStrokeNode)

