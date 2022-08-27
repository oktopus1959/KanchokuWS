using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.TableParser
{
    ///// <summary>書き換え情報</summary>    
    //class RewriteInfo
    //{
    //    string outputStr;           // 書き換え後の出力文字列(bareでない文字列は、ダブルクォートで囲んでおく必要がある)
    //    bool isBareStr;             // 出力文字列は BareString か
    //    List<Node> subTable;        // 書き換え後のテーブル定義(outputStrとは排他になる)

    //    public string OutputString => outputStr;

    //    public bool IsBareStr => isBareStr;

    //    public string QuotedString => outputStr._quoteString(IsBareStr);

    //    public List<Node> SubTable => subTable;

    //    public RewriteInfo()
    //    {
    //    }

    //    // コンストラクタ
    //    public RewriteInfo(string outStr, List<Node> subTbl, bool bBareStr)
    //    {
    //        outputStr = outStr;
    //        isBareStr = bBareStr;
    //        subTable = subTbl;
    //    }

    //    /// <summary>別の書き換え情報をマージする。上書きが発生したら true を返す</summary>
    //    /// <param name="info"></param>
    //    /// <returns></returns>
    //    public bool Merge(RewriteInfo info)
    //    {
    //        bool bOverwrite = false;
    //        if (info.outputStr._notEmpty()) {
    //            outputStr = info.outputStr;
    //            bOverwrite = true;
    //        }
    //        if (info.subTable != null) {
    //            if (subTable == null) {
    //                subTable = info.subTable;
    //            } else {
    //                int num = info.GetSubNodeNum();
    //                for (int i = 0; i < num; ++i) {
    //                    var p = info.GetNthSubNode(i);
    //                    if (p != null) {
    //                        bOverwrite = bOverwrite || GetNthSubNode(i) != null;  // マージ先に既存ノードがあれば上書き発生
    //                        SetNthSubNode(i, p);
    //                    }
    //                }
    //            }
    //        }
    //        return bOverwrite;
    //    }

    //    /// <summary>子ノードの数を返す</summary>
    //    public int GetSubNodeNum()
    //    {
    //        return subTable._safeCount();
    //    }

    //    /// <summary>子ノードがあるか</summary>
    //    public bool HasSubNode()
    //    {
    //        return GetSubNodeNum() > 0;
    //    }

    //    /// <summary>n番目の子ノードを返す</summary>
    //    public Node GetNthSubNode(int n)
    //    {
    //        return subTable._getNth(n);
    //    }

    //    /// <summary>
    //    /// n番目の子ノードをセットする(ノードの重複があったら true を返す)<br/>
    //    /// 旧ノードが StrokeTableNodeで、新ノードがそれ以外だったら、新ノードは無視されるので注意
    //    /// </summary>
    //    /// <param name=""></param>
    //    /// <param name=""></param>
    //    public bool SetNthSubNode(int n, Node node)
    //    {
    //        if (subTable != null) {
    //            if (n >= 0 && n < subTable.Count) {
    //                if (subTable[n] != null) {
    //                    if (node.IsRewriteNode() && subTable[n].IsRewriteNode()) {
    //                        // 新旧ノードが RewriteNode である
    //                        subTable[n].Merge(node);
    //                        return false;
    //                    } else if (node.IsTreeNode() || !subTable[n].IsTreeNode()) {
    //                        // 新旧ノードが StrokeTableNode であるか、旧ノードが StrokeTableNode でなければ、上書き
    //                        subTable[n] = node;
    //                        return !node.IsRewriteNode();  // 新ノードが RewriteNode なら上書き警告しない
    //                    } else {
    //                        // それ以外は node を捨てる
    //                        return true;
    //                    }
    //                }
    //                subTable[n] = node;
    //            }
    //        }
    //        return false;
    //    }

    //    public string DebugString()
    //    {
    //        return $"OutputStr={OutputString._orElse("empty")}, IsBare={IsBareStr}, SubNodeNum={GetSubNodeNum()}";
    //    }
    //}

    /// <summary>書き換え情報も保持したノード</summary>    
    class Node
    {
        private static Logger logger = Logger.GetLogger();

        public enum NodeType
        {
            None,
            String,
            Function,
            StrokeTree,
            Rewrite
        }

        NodeType nodeType = NodeType.None;

        // 自分の情報
        //RewriteInfo myInfo;

        string outputStr;           // 書き換え後の出力文字列(bareでない文字列は、ダブルクォートで囲んでおく必要がある)
        bool isBareStr;             // 出力文字列は BareString か
        List<Node> subTable;        // 後続ストローク用のテーブル定義(outputStrとは排他になる)

        // 書き換え情報マップ -- 書き換え対象文字列がキーとなる
        Dictionary<string, Node> rewriteMap = new Dictionary<string, Node>();

        //string str = "";

        //public bool OutputFlag = false;

        public Node()
        {
        }

        // コンストラクタ
        public Node(NodeType nodeType, string outStr, List<Node> subTbl, bool bBareStr)
        {
            this.nodeType = nodeType;
            outputStr = outStr;
            isBareStr = bBareStr;
            subTable = subTbl;
            //myInfo = new RewriteInfo(outStr, subTbl, bBareStr);
        }

        // コンストラクタ
        //public RewriteNode(string outStr) : base(NodeType.Rewrite, "__PRE__")
        //{
        //    myInfo = new RewriteInfo(outStr, null);
        //}

        //public Node GetNthSubNode(int n) { return myInfo.GetNthSubNode(n); }
        /// <summary>n番目の子ノードを返す</summary>
        public Node GetNthSubNode(int n)
        {
            return subTable._getNth(n);
        }

        //public bool HasSubNode() { return myInfo.HasSubNode(); }
        /// <summary>子ノードがあるか</summary>
        public bool HasSubNode()
        {
            return GetSubNodeNum() > 0;
        }


        //public int GetSubNodeNum() { return myInfo.GetSubNodeNum(); }
        /// <summary>子ノードの数を返す</summary>
        public int GetSubNodeNum()
        {
            return subTable._safeCount();
        }


        public List<Node> GetSubNodes() { return subTable; }

        public bool HasOutputString() { return outputStr._notEmpty(); }

        public string GetOutputString() { return outputStr; }

        public string GetQuotedString() { return outputStr._quoteString(isBareStr); }

        public bool IsFunctionNode() { return nodeType == NodeType.Function; }

        public bool IsStringNode() { return nodeType == NodeType.String; }

        public bool IsTreeNode() { return nodeType == NodeType.StrokeTree; }

        public bool IsRewriteNode() { return nodeType == NodeType.Rewrite; }

        /// <summary>書き換え情報をマージする</summary>
        /// <param name="rewNode"></param>
        public void Merge(Node rewNode)
        {
            merge(rewNode);
            if (rewNode.rewriteMap._notEmpty()) {
                foreach (var pair in rewNode.rewriteMap) {
                    upsertRewrteMap(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>別の書き換え情報をマージする。上書きが発生したら true を返す</summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool merge(Node node)
        {
            bool bOverwrite = false;
            if (node.outputStr._notEmpty()) {
                outputStr = node.outputStr;
                bOverwrite = true;
            }
            if (node.subTable != null) {
                if (subTable == null) {
                    subTable = node.subTable;
                } else {
                    int num = node.GetSubNodeNum();
                    for (int i = 0; i < num; ++i) {
                        var p = node.GetNthSubNode(i);
                        if (p != null) {
                            bOverwrite = bOverwrite || GetNthSubNode(i) != null;  // マージ先に既存ノードがあれば上書き発生
                            SetNthSubNode(i, p);
                        }
                    }
                }
            }
            return bOverwrite;
        }

        private void upsertRewrteMap(string key, Node value)
        {
            if (rewriteMap == null) rewriteMap = new Dictionary<string, Node>();
            if (rewriteMap.ContainsKey(key)) {
                rewriteMap[key].Merge(value);
            } else {
                rewriteMap[key] = value;
            }
        }

        public void AddRewritePair(string tgtStr, Node rewriteNode)
        {
            logger.DebugH(() => $"CALLED: tgtStr={tgtStr}, rewriteNode={rewriteNode.DebugString()}");
            if (tgtStr._notEmpty() && rewriteNode != null) {
                upsertRewrteMap(tgtStr, rewriteNode);
            }
        }

        /// <summary>
        /// n番目の子ノードをセットする(ノードの重複があったら true を返す)<br/>
        /// 旧ノードが StrokeTableNodeで、新ノードがそれ以外だったら、新ノードは無視されるので注意
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public bool SetNthSubNode(int n, Node node)
        {
            if (subTable != null) {
                if (n >= 0 && n < subTable.Count) {
                    if (subTable[n] != null) {
                        if (node.IsRewriteNode() && subTable[n].IsRewriteNode()) {
                            // 新旧ノードが RewriteNode である
                            subTable[n].Merge(node);
                            return false;
                        } else if (node.IsTreeNode() || !subTable[n].IsTreeNode()) {
                            // 新旧ノードが StrokeTableNode であるか、旧ノードが StrokeTableNode でなければ、上書き
                            subTable[n] = node;
                            return !node.IsRewriteNode();  // 新ノードが RewriteNode なら上書き警告しない
                        } else {
                            // それ以外は node を捨てる
                            return true;
                        }
                    }
                    subTable[n] = node;
                }
            }
            return false;
        }

        /// <summary>
        /// Decoderが解釈できるテーブルの出力<br/>
        /// ・ツリーノードは、ネストしたテーブルを、各スレッドを矢印記法列に開いてフラットにして出力 (「-10>-12>-13>あ」など)<br/>
        /// ・文字列ノードまたは書き換えノードとツリーノードは排他的<br/>
        /// -22>@{。<br/>
        ///   、  {<br/>
        ///   -11>ぴゃ<br/>
        ///   -13>にゃ<br/>
        ///   -22>-17>あります<br/>
        ///   }<br/>
        /// }<br/>
        /// </summary>
        /// <param name="outLines"></param>
        /// <param name="list"></param>
        public void OutputLine(List<string> outLines)
        {
            outputLine(outLines, null);
        }

        private void outputLine(List<string> outLines, List<int> list)
        {
            if (HasSubNode()) {
                // ツリーノード
                //outputSubNodes(outLines, this);
                // 深さ優先でスレッドをたどっていく
                if (list == null) list = new List<int>();
                int num = GetSubNodeNum();
                for (int i = 0; i < num; ++i) {
                    var c = GetNthSubNode(i);
                    if (c != null) {
                        list.Add(i);
                        c.outputLine(outLines, list);
                        list._popBack();
                    }
                }
            } else  {
                if (list._notEmpty()) {
                    string leaderStr = $"-{list.Select(x => x.ToString())._join(">-")}>";
                    if (rewriteMap._notEmpty()) {
                        // 書き換えノード
                        outLines.Add(leaderStr + "@{" + GetQuotedString());
                        foreach (var pair in rewriteMap) {
                            // 書き換えMapの先のノードは、文字列ノードかツリーノードとみなす
                            if (pair.Key._notEmpty() && pair.Value != null) {
                                if (pair.Value.HasSubNode()) {
                                    outLines.Add($"{pair.Key}\t{{");
                                    pair.Value.outputLine(outLines, null);
                                    outLines.Add("}");
                                } else if (pair.Value.HasOutputString()) {
                                    outLines.Add($"{pair.Key}\t{pair.Value.GetQuotedString()}");
                                }
                            }
                        }
                        outLines.Add("}");
                    } else {
                        // 文字列ノード
                        // ツリーのスレッドを leaderStr として、終端ノードの文字列を出力
                        outputString(outLines, leaderStr);
                    }
                }
            }
        }

        //static void outputSubNodes(List<string> outLines, Node p, List<int> list = null)
        //{
        //    if (list == null) list = new List<int>();

        //    if (p.HasSubNode()) {
        //        // 深さ優先でスレッドをたどっていく
        //        int num = p.GetSubNodeNum();
        //        for (int i = 0; i < num; ++i) {
        //            var c = p.GetNthSubNode(i);
        //            if (c != null) {
        //                list.Add(i);
        //                outputSubNodes(outLines, c, list);
        //                list._popBack();
        //            }
        //        }
        //    } else {
        //        // ツリーのスレッドを leaderStr として、終端ノードの文字列を出力
        //        p.outputString(outLines, $"-{list.Select(x => x.ToString())._join(">-")}>");
        //    }
        //}

        protected virtual void outputString(List<string> outLines, string leaderStr)
        {
            if (HasOutputString()) {
                outLines.Add(leaderStr + GetQuotedString());
            }
        }

        public string DebugString()
        {
            return $"NodeType={nodeType}, OutputStr={outputStr._orElse("empty")}, IsBare={isBareStr}, SubNodeNum={GetSubNodeNum()}, RewriteMapNum={rewriteMap._safeCount()}";
        }

        static protected List<Node> makeNodeList(bool bRoot = false)
        {
            // 同時打鍵の終端と非終端の重複回避用に2倍(非終端はシフト面を使う)
            // たとえば「A B」と「A B C」という2つの同時打鍵列を使いたい場合など
            return Helper.MakeList(new Node[(bRoot ? DecoderKeys.TOTAL_DECKEY_NUM : DecoderKeys.PLANE_DECKEY_NUM * 2)]);
        }
    }

    class StrokeTableNode : Node
    {
        public StrokeTableNode(bool bRoot = false)
            : base(NodeType.StrokeTree, "", makeNodeList(bRoot), false)
        {
        }
    }

    class StringNode : Node
    {
        public StringNode(string str, bool bare) : base(NodeType.String, str, null, bare)
        {
        }
    }

    class FunctionNode : Node
    {
        public FunctionNode(string str) : base(NodeType.Function, "@" + str, null, true)
        {
        }
    }

    class RewriteNode : Node
    {
        // コンストラクタ
        public RewriteNode(string outStr, bool bBare) : base(NodeType.Rewrite, outStr, makeNodeList(), bBare)
        {
        }

        // コンストラクタ
        public RewriteNode(string outStr, List<Node> nodeList, bool bBare)
            : base(NodeType.Rewrite, outStr, nodeList ?? makeNodeList(), bBare)
        {
        }
    }
    //class RewriteNode : Node
    //{
    //    private static Logger logger = Logger.GetLogger();

    //    RewriteInfo myInfo;

    //    // 書き換え情報マップ -- 書き換え対象文字列がキーとなる
    //    Dictionary<string, RewriteInfo> rewriteMap = new Dictionary<string, RewriteInfo>();

    //    private void upsertRewrteMap(string key, RewriteInfo value)
    //    {
    //        if (rewriteMap.ContainsKey(key)) {
    //            rewriteMap[key].Merge(value);
    //        } else {
    //            rewriteMap[key] = value;
    //        }
    //    }

    //    // コンストラクタ
    //    public RewriteNode(string outStr) : base(NodeType.Rewrite, "__PRE__")
    //    {
    //        myInfo = new RewriteInfo(outStr, null);
    //    }

    //    public override string GetOutputString()
    //    {
    //        return myInfo.OutputString;
    //    }

    //    public void Merge(RewriteNode rewNode)
    //    {
    //        myInfo.Merge(rewNode.myInfo);
    //        foreach (var pair in rewNode.rewriteMap) {
    //            upsertRewrteMap(pair.Key, pair.Value);
    //        }
    //    }

    //    public void AddRewritePair(string tgtStr, string outStr, StrokeTableNode pNode)
    //    {
    //        logger.DebugH(() => $"CALLED: key={tgtStr}, value={outStr}, pNode={(pNode != null ? pNode.GetOutputString() : "none")}");
    //        if (tgtStr._notEmpty() && (outStr._notEmpty() || pNode != null)) {
    //            upsertRewrteMap(tgtStr, new RewriteInfo(outStr, pNode));
    //        }
    //    }

    //    public override void OutputLine(List<string> outLines, string leaderStr)
    //    {
    //        if (!OutputFlag) {
    //            OutputFlag = true;
    //            outLines.Add(leaderStr + "@{" + myInfo.OutputString);
    //            foreach (var pair in rewriteMap) {
    //                if (pair.Key._notEmpty()) {
    //                    if (pair.Value.SubTable != null) {
    //                        outLines.Add($"{pair.Key}\t{{");
    //                        pair.Value.SubTable.OutputLine(outLines, "");
    //                        outLines.Add("}");
    //                    } else if (pair.Value.OutputString._notEmpty()) {
    //                        outLines.Add($"{pair.Key}\t{pair.Value.OutputString}");
    //                    }
    //                }
    //            }
    //            outLines.Add("}");
    //        }
    //    }
    //}

}
