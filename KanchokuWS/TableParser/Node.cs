using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.TableParser
{
    class Node
    {
        public enum NodeType
        {
            None,
            String,
            Function,
            StrokeTree
        }

        NodeType nodeType = NodeType.None;

        string str = "";

        public bool OutputFlag = false;

        public Node()
        {
        }

        public Node(NodeType nodeType, string str)
        {
            this.nodeType = nodeType;
            this.str = str;
        }

        public virtual Node getNth(int n) { return null; }

        public virtual List<Node> getChildren() { return null; }

        public virtual string getString() { return str; }

        public virtual string getMarker() { return str; }

        public bool isFunctionNode() { return nodeType == NodeType.Function; }

        public bool isStringNode() { return nodeType == NodeType.String; }

        public bool isStrokeTree() { return nodeType == NodeType.StrokeTree; }

        public virtual void OutputLine(List<string> outLines, string leaderStr)
        {
        }

        public string DebugString()
        {
            return $"NodeType={nodeType}, NodeStr={str._orElse("empty")}";
        }
    }

    class StrokeTableNode : Node
    {
        List<Node> children;

        public StrokeTableNode(bool bRoot = false) : base(NodeType.StrokeTree, "")
        {
            // 同時打鍵の終端と非終端の重複回避用に2倍(非終端はシフト面を使う)
            // たとえば「A B」と「A B C」という2つの同時打鍵列を使いたい場合など
            children = Helper.MakeList(new Node[(bRoot ? DecoderKeys.TOTAL_DECKEY_NUM : DecoderKeys.PLANE_DECKEY_NUM * 2)]);
        }

        public override List<Node> getChildren()
        {
            return children;
        }

        // n番目の子ノードを返す
        public override Node getNth(int n)
        {
            return children._getNth(n);
        }

        /// <summary>
        /// n番目の子ノードをセットする(ノードの重複があったら true を返す)<br/>
        /// 旧ノードが StrokeTableNodeで、新ノードがそれ以外だったら、新ノードは無視されるので注意
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public bool setNthChild(int n, Node node)
        {
            if (n >= 0 && n < children.Count) {
                if (children[n] != null) {
                    if (node is RewriteNode && children[n] is RewriteNode) {
                        // 新旧ノードが RewriteNode である
                        ((RewriteNode)children[n]).Merge((RewriteNode)node);
                        return false;
                    } else if (node is StrokeTableNode || !(children[n] is StrokeTableNode)) {
                        // 新旧ノードが StrokeTableNode であるか、旧ノードが StrokeTableNode でなければ、上書き
                        children[n] = node;
                        return !(node is RewriteNode);  // 新ノードが RewriteNode なら上書き警告しない
                    } else {
                        // それ以外は node を捨てる
                        return true;
                    }
                }
                children[n] = node;
            }
            return false;
        }

        public override void OutputLine(List<string> outLines, string leaderStr)
        {
            var list = new List<int>();
            outputNewLines(outLines, this, list);
        }

        void outputNewLines(List<string> outLines, Node p, List<int> list)
        {
            var children = p.getChildren();
            if (children._notEmpty()) {
                for (int i = 0; i < children.Count; ++i) {
                    var c = children[i];
                    if (c != null) {
                        list.Add(i);
                        outputNewLines(outLines, c, list);
                        list._popBack();
                    }
                }
            } else {
                p.OutputLine(outLines, $"-{list.Select(x => x.ToString())._join(">-")}>");
            }
        }

    }

    class StringNode : Node
    {
        bool bBare = false;

        public StringNode(string str, bool bare) : base(NodeType.String, str)
        {
            bBare = bare;
        }

        public override void OutputLine(List<string> outLines, string leaderStr)
        {
            if (!OutputFlag) {
                OutputFlag = true;
                if (getString()._notEmpty()) outLines.Add(leaderStr + getString()._quoteString(bBare));
            }
        }
    }

    class FunctionNode : Node
    {
        string marker;

        public FunctionNode(string str) : base(NodeType.Function, str)
        {
            marker = "@" + str;
        }

        public override string getMarker()
        {
            return marker;
        }

        public override void OutputLine(List<string> outLines, string leaderStr)
        {
            if (!OutputFlag) {
                OutputFlag = true;
                outLines.Add(leaderStr + marker);
            }
        }
    }

    // 書き換え情報
    class RewriteInfo {
        string outputStr;           // 書き換え後の出力文字列(bareでない文字列は、ダブルクォートで囲んでおく必要がある)
        StrokeTableNode subTable;   // 書き換え後のテーブル定義(outputStrとは排他になる)

        public RewriteInfo()
        {
        }

        public RewriteInfo(string outStr, StrokeTableNode subTbl)
        {
            outputStr = outStr;
            subTable = subTbl;
        }

        public void merge(RewriteInfo info)
        {
            if (info.outputStr._notEmpty()) {
                outputStr = info.outputStr;
            }
            if (info.subTable != null) {
                if (subTable == null) {
                    subTable = info.subTable;
                } else {
                    for (int i = 0; i < info.subTable.getChildren()._safeCount(); ++i) {
                        var p = info.subTable.getNth(i);
                        if (p != null) subTable.setNthChild(i, p);
                    }
                }
            }
        }

        public string OutputStr => outputStr;

        public StrokeTableNode SubTable => subTable;
    }

    class RewriteNode : FunctionNode
    {
        private static Logger logger = Logger.GetLogger();

        RewriteInfo myInfo;

        // 書き換え情報マップ -- 書き換え対象文字列がキーとなる
        Dictionary<string, RewriteInfo> rewriteMap = new Dictionary<string, RewriteInfo>();

        private void upsertRewrteMap(string key, RewriteInfo value)
        {
            if (rewriteMap.ContainsKey(key)) {
                rewriteMap[key].merge(value);
            } else {
                rewriteMap[key] = value;
            }
        }

        // コンストラクタ
        public RewriteNode(string outStr) : base("__PRE__")
        {
            myInfo = new RewriteInfo(outStr, null);
        }

        public override string getString()
        {
            return myInfo.OutputStr;
        }

        public void Merge(RewriteNode rewNode)
        {
            myInfo.merge(rewNode.myInfo);
            foreach (var pair in rewNode.rewriteMap) {
                upsertRewrteMap(pair.Key, pair.Value);
            }
        }

        public void AddRewritePair(string tgtStr, string outStr, StrokeTableNode pNode)
        {
            logger.DebugH(() => $"CALLED: key={tgtStr}, value={outStr}, pNode={(pNode != null ? pNode.getString() : "none")}");
            if (tgtStr._notEmpty() && (outStr._notEmpty() || pNode != null)) {
                upsertRewrteMap(tgtStr, new RewriteInfo(outStr, pNode));
            }
        }

        public override void OutputLine(List<string> outLines, string leaderStr)
        {
            if (!OutputFlag) {
                OutputFlag = true;
                outLines.Add(leaderStr + "@{" + myInfo.OutputStr);
                foreach (var pair in rewriteMap) {
                    if (pair.Key._notEmpty()) {
                        if (pair.Value.SubTable != null) {
                            outLines.Add($"{pair.Key}\t{{");
                            pair.Value.SubTable.OutputLine(outLines, "");
                            outLines.Add("}");
                        } else if (pair.Value.OutputStr._notEmpty()) {
                            outLines.Add($"{pair.Key}\t{pair.Value.OutputStr}");
                        }
                    }
                }
                outLines.Add("}");
            }
        }
    }

}
