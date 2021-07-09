//#include "pch.h"

#include "Node.h"

// -------------------------------------------------------------------
// StringNode - 文字列を格納するノード
class StringNode : public Node {
 public:
     StringNode(const wstring& s) {
         if (s.empty()) {   // 文字列がない場合
             str.clear();
         } else {           // 文字列がある場合 - 文字列を保存する
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

    NodeType getNodeType() const { return NodeType::String; }

private:
    // 打鍵による出力文字列
    MString str;
};

