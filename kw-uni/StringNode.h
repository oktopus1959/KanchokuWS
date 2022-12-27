#pragma once

#include "Node.h"

#include "OneShot/RewriteString.h"

// -------------------------------------------------------------------
// StringNode - 文字列を格納するノード
class StringNode : public Node {
 public:
     StringNode(const wstring& s, /*bool converted,*/ bool bRewritable) : /*bConverted(converted),*/ rewritableLen(0) {
         if (s.empty()) {   // 文字列がない場合
             str.clear();
         } else if (bRewritable) {           // 文字列がある場合 - 文字列を保存する
             wstring ws;
             ANALYZE_REWRITE_STR(s, ws, rewritableLen);
             str = to_mstr(ws);
             //str = to_mstr(utils::replace(s, _T("/"), _T("")));
             //size_t pos = s.find('/', 0);
             //rewritableLen = pos <= str.size() ? str.size() - pos : str.empty() ? 0 : 1;
         } else {
             str = to_mstr(s);
         }
     }

     StringNode(wchar_t ch) {
         str.clear();
         if (ch != 0) {
             str.push_back(ch);
         }
     }

    ~StringNode() { }

    // 当ノードを処理する State インスタンスを作成する
    State* CreateState();

    // 出力用文字列を返す
     MString getString() const { return str; }

    size_t getRewritableLen() const { return rewritableLen; }

    NodeType getNodeType() const { return NodeType::String; }

    //bool isConverted() const { return bConverted; }

private:
    // 打鍵による出力文字列
    MString str;

    size_t rewritableLen;

    //// 裏面定義文字か
    //bool bConverted;
};

