//#include "pch.h"
#pragma once

#include "deckey_id_defs.h"
#include "Reporting/Logger.h"
#include "StringNode.h"
#include "FunctionNode.h"
#include "Node.h"

// -------------------------------------------------------------------
// ストローク木のトラバーサ
class StrokeTreeTraverser {
    std::vector<class StrokeTableNode*> tblList;
    std::vector<int> path;

    bool bFull = false;

public:
    StrokeTreeTraverser(class StrokeTableNode*, bool);

    Node* getNext();

    const std::vector<int>& getPath() { return path; }
};

// -------------------------------------------------------------------
// StrokeTableNode - ストロークテーブルの連鎖となるノード
class StrokeTableNode : public Node {
    DECLARE_CLASS_LOGGER;

private:
    //std::vector<std::unique_ptr<Node>> children;
    std::vector<Node*> children;

    size_t _depth;

    wstring nodeMarker = _T("□");

    // 全ストロークノードが不要になったら true (ステートの作成時にクリアする)
    bool bRemoveAllStroke = false;

    // 後置書き換え機能ありか
    int iHasPostRewriteNode = 0;

public:
    // コンストラクタ
    StrokeTableNode(int depth) : _depth(depth) {
        //children.resize(STROKE_DECKEY_NUM);     // normalキーとshift修飾キーの両方のキーの分を確保しておく
        //children.resize(PLANE_DECKEY_NUM);     // 通常面のキーの分を確保しておけば十分である
        children.resize(PLANE_DECKEY_NUM * 2);   // 通常面と、同時打鍵での重複用に2面確保しておく
    }

    StrokeTableNode(int depth, size_t numChildren) : _depth(depth) {
        children.resize(numChildren);
    }

    // デストラクタ
    ~StrokeTableNode();

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 子ノードの文字をコピーする
    void CopyChildrenFace(mchar_t* faces, size_t facesSize);

    // 表示用文字列を返す
    MString getString() const { return to_mstr(nodeMarker); }

    NodeType getNodeType() const { return _depth == 0 ? NodeType::RootStroke : NodeType::Stroke; }

    // 後置書き換え機能ありか
    bool hasPostRewriteNode();

private:
    int findPostRewriteNode();

public:
    /* StrokeTableNode 独自メソッド */

    // n番目の子ノードを返す
    inline Node* getNth(size_t n) const {
        return n < children.size() ? children[n] : 0;
    }

    // n番目の子ノードをセットする
    inline void setNthChild(size_t n, Node* node) {
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
    // 指定文字に至るストローク列を返す
    bool getStrokeListSub(const MString& target, std::vector<int>& list, bool bFull);

public:
    // 指定文字に至るストローク列を返す
    std::vector<int> getStrokeList(const MString& target, bool bFull) {
        std::vector<int> list;
        if (!getStrokeListSub(target, list, bFull)) list.clear();
        return list;
    }

    // ストロークガイドの構築
    void MakeStrokeGuide(const wstring& targetChars, int tableId);

    // ストローク木を構築する
    static StrokeTableNode* CreateStrokeTree(const wstring&, std::vector<wstring>&);

    // ストローク木2を構築する
    static StrokeTableNode* CreateStrokeTree2(const wstring&, std::vector<wstring>&);

    // ストローク木3を構築する
    static StrokeTableNode* CreateStrokeTree3(const wstring&, std::vector<wstring>&);

    // 機能の再割り当て
    static void AssignFucntion(const wstring& keys, const wstring& name);

    // ストロークノードの更新
    static void UpdateStrokeNodes(const wstring& strokeSource);

    // 主・副ストローク木の入れ替え
    static int ExchangeStrokeTable();

    // 主ストローク木を使う
    static int UseStrokeTable1();

    // 副ストローク木を使う
    static int UseStrokeTable2();

    // 第3ストローク木を使う
    static int UseStrokeTable3();

    // 現在のストローク木の番号
    static int GetCurrentStrokeTableNum();

    static std::unique_ptr<StrokeTableNode> RootStrokeNode1;
    static std::unique_ptr<StrokeTableNode> RootStrokeNode2;
    static std::unique_ptr<StrokeTableNode> RootStrokeNode3;
    static StrokeTableNode* RootStrokeNode;
};

#define ROOT_STROKE_NODE (StrokeTableNode::RootStrokeNode)

