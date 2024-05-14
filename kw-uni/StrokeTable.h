//#include "pch.h"
#pragma once

#include "deckey_id_defs.h"
#include "Reporting/Logger.h"
#include "StringNode.h"
#include "FunctionNode.h"
#include "Node.h"

class PostRewriteOneShotNode;

// -------------------------------------------------------------------
// ストローク木のトラバーサ
class StrokeTreeTraverser {
    std::vector<class StrokeTableNode*> tblList;
    std::vector<int> path;

    bool bFull = false;
    bool bRewriteTable = false;

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

    String nodeMarker = _T("□");

    // 後置書き換え子ノードありか
    int iHasPostRewriteNode = 0;

    // 当ノードに対する書き換え定義
    PostRewriteOneShotNode* rewriteNode = 0;

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
    ~StrokeTableNode() override;

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState() override;

    // 子ノードの文字をコピーする
    void CopyChildrenFace(mchar_t* faces, size_t facesSize);

    // 表示用文字列を返す
    MString getString() const override { return to_mstr(nodeMarker); }

    String getNodeName() const override { return _depth == 0 ? _T("RootStrokeNode") : _T("StrokeNode"); }

    NodeType getNodeType() const override { return _depth == 0 ? NodeType::RootStroke : NodeType::Stroke; }

    // 後置書き換え子ノードありか
    bool hasPostRewriteNode();

    // (半)濁点のみの後置書き換え子ノードがあるか
    bool hasOnlyUsualRewriteNdoe();

private:
    int findPostRewriteNode(int result);

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

    // n番目の子ノードとスワップする
    inline Node* swapNthChild(size_t n, Node* node) {
        Node* old = 0;
        if (n < children.size()) {
            old = children[n];
            children[n] = node;
        }
        return old;
    }

    // 後置書き換えノードを取得
    PostRewriteOneShotNode* getRewriteNode();

    // 後置書き換えノードをマージ
    void mergeRewriteNode(PostRewriteOneShotNode* node);

public:
    // 子ノード数を返す
    inline size_t numChildren() const {
        return children.size();
    }

    // 木の根からの深さを返す
    inline size_t depth() const {
        return _depth;
    }

private:
    // 指定文字に至るストローク列を返す
    bool getStrokeListSub(const MString& target, std::vector<int>& list, bool bFull);

    // ストローク可能な文字の集合
    static std::set<mchar_t> strokableChars;

public:
    // 指定文字に至るストローク列を返す
    std::vector<int> getStrokeList(const MString& target, bool bFull) {
        std::vector<int> list;
        if (!getStrokeListSub(target, list, bFull)) list.clear();
        return list;
    }

    // ストロークガイドの構築
    void MakeStrokeGuide(StringRef targetChars, int tableId);

    // ストローク木を構築する
    static StrokeTableNode* CreateStrokeTree(StringRef, std::vector<String>&);

    // ストローク木2を構築する
    static StrokeTableNode* CreateStrokeTree2(StringRef, std::vector<String>&);

    // ストローク木3を構築する
    static StrokeTableNode* CreateStrokeTree3(StringRef, std::vector<String>&);

    // 機能の再割り当て
    static void AssignFucntion(StringRef keys, StringRef name);

    // ストローク可能文字を収集
    static std::set<mchar_t> GatherStrokeChars();

    // ストローク可能文字か
    static bool IsStrokable(mchar_t ch);

    // ストロークノードの更新
    static void UpdateStrokeNodes(StringRef strokeSource);

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

