using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.TableParser
{
    class OutputString
    {
        string str;
        bool isBare;

        public OutputString(string str, bool bBare)
        {
            this.str = str;
            isBare = bBare;
        }

        public static OutputString OfFunction(string str)
        {
            return new OutputString("@" + str, true);
        }

        public bool IsEmpty()
        {
            return str._isEmpty();
        }

        public bool IsBare()
        {
            return isBare;
        }

        public bool IsFunction()
        {
            return isBare && str._getFirst() == '@';
        }

        public string GetSafeString()
        {
            return str._toSafe();
        }

        public string GetBaseQuotedString()
        {
            return (IsFunction() ? str._safeSubstring(1) : str)._safeReplace(@"\", @"\\")._safeReplace(@"""", @"\""")._quoteString(isBare);
        }

        public string GetQuotedString()
        {
            return str._safeReplace(@"\", @"\\")._safeReplace(@"""", @"\""")._quoteString(isBare);
        }

        public bool EqualsTo(OutputString s)
        {
            return str._equalsTo(s?.str);
        }

        public bool EqualsTo(string s)
        {
            return str._equalsTo(s);
        }

        public string DebugString()
        {
            return $"OutputStr={str._orElse("empty")}, IsBare={isBare}";
        }
    }

    class NodeTable
    {
        List<Node> table;

        bool isRoot;

        public int Length => table?.Count ?? 0;

        public Node GetNth(int n) { return table._getNth(n); }

        public void SetNth(int n, Node node) { if (table != null) table[n] = node; }

        public bool IsRoot => isRoot;

        public NodeTable(bool bRoot = false)
        {
            // 同時打鍵の終端と非終端の重複回避用に2倍(非終端はシフト面を使う)
            // たとえば「A B」と「A B C」という2つの同時打鍵列を使いたい場合など
            table = Helper.MakeList(new Node[(bRoot ? DecoderKeys.TOTAL_DECKEY_NUM : DecoderKeys.PLANE_DECKEY_NUM * 2)]);
            isRoot = bRoot;
        }
    }

    class RewriteMap
    {
        Dictionary<string, Node> rewriteMap;

        public bool IsEmpty => rewriteMap._isEmpty();

        public int Count => rewriteMap._safeCount();

        public RewriteMap()
        {
            rewriteMap = new Dictionary<string, Node>();
        }

        public bool ContainsKey(string key)
        {
            return rewriteMap?.ContainsKey(key) ?? false;
        }

        public Node GetNode(string key)
        {
            return rewriteMap._safeGet(key);
        }

        public void PutPair(string key, Node val)
        {
            if (rewriteMap != null) rewriteMap[key] = val;
        }

        public void ForEach(Action<string, Node> action)
        {
            if (rewriteMap._notEmpty()) {
                foreach (var pair in rewriteMap) {
                    action(pair.Key, pair.Value);
                }
            }
        }
    }

    static class NodeHelper
    {
        public static bool _isEmpty(this OutputString outStr)
        {
            return outStr?.IsEmpty() ?? true;
        }

        public static bool _notEmpty(this OutputString outStr)
        {
            return !outStr._isEmpty();
        }

        public static bool _equalsTo(this OutputString lh, OutputString rh)
        {
            return lh != null ? lh.EqualsTo(rh) : false;
        }

        public static bool _ne(this OutputString lh, OutputString rh)
        {
            return !lh._equalsTo(rh);
        }

        public static string _toSafe(this OutputString s)
        {
            return s?.GetSafeString() ?? "";
        }

        public static int _safeCount(this NodeTable table)
        {
            return table?.Length ?? 0;
        }

        public static bool _isEmpty(this RewriteMap map)
        {
            return map == null || map.IsEmpty;
        }

        public static bool _notEmpty(this RewriteMap map)
        {
            return !map._isEmpty();
        }

        public static int _safeCount(this RewriteMap map)
        {
            return map?.Count ?? 0;
        }

        public static void _forEach(this RewriteMap map, Action<string, Node> action)
        {
            if (map._notEmpty()) {
                map.ForEach(action);
            }
        }
    }

    /// <summary>書き換え情報も保持したノード</summary>    
    class Node
    {
        private static Logger logger = Logger.GetLogger();

        OutputString outputStr;     // 出力文字列 (機能マーカーも含む)
        NodeTable subTable;         // 後続ストローク用のテーブル定義(outputStrとは排他になる)
        RewriteMap rewriteMap;      // 書き換え情報マップ -- 書き換え対象文字列がキーとなる

        private Node()
        {
        }

        //static protected List<Node> makeNodeList(bool bRoot = false)
        //{
        //    // 同時打鍵の終端と非終端の重複回避用に2倍(非終端はシフト面を使う)
        //    // たとえば「A B」と「A B C」という2つの同時打鍵列を使いたい場合など
        //    return Helper.MakeList(new Node[(bRoot ? DecoderKeys.TOTAL_DECKEY_NUM : DecoderKeys.PLANE_DECKEY_NUM * 2)]);
        //}

        // TreeNode を作成して返す
        public static Node MakeTreeNode(bool bRoot = false)
        {
            return new Node() { subTable = new NodeTable(bRoot) };
        }

        // StringNode を作成して返す
        public static Node MakeStringNode(OutputString outStr)
        {
            return new Node() { outputStr = outStr };
        }

        // StringNode を作成して返す
        public static Node MakeStringNode(string str, bool bare)
        {
            return new Node() { outputStr = new OutputString(str, bare) };
        }

        // FunctionNode を作成して返す
        public static Node MakeFunctionNode(string str)
        {
            return new Node() { outputStr = OutputString.OfFunction(str) };
        }

        /// <summary> 空のRewriteNode を作成して返す</summary>
        public static Node MakeRewriteNode()
        {
            return MakeRewriteNode(new OutputString("", true));
        }

        // RewriteNode を作成して返す
        public static Node MakeRewriteNode(string outStr, bool bBare)
        {
            return new Node() { outputStr = new OutputString(outStr, bBare), rewriteMap = new RewriteMap() };
        }

        // RewriteNode を作成して返す
        public static Node MakeRewriteNode(OutputString outStr)
        {
            return new Node() { outputStr = outStr, rewriteMap = new RewriteMap() };
        }

        // RewriteTreeNode を作成して返す
        public static Node MakeRewriteTreeNode(string outStr, bool bBare)
        {
            return new Node() { outputStr = new OutputString(outStr, bBare), subTable = new NodeTable(), rewriteMap = new RewriteMap() };
        }

        // RewriteTreeNode を作成して返す
        public static Node MakeRewriteTreeNode(OutputString outStr)
        {
            return new Node() { outputStr = outStr, subTable = new NodeTable(), rewriteMap = new RewriteMap() };
        }

        //public Node GetNthSubNode(int n) { return myInfo.GetNthSubNode(n); }
        /// <summary>n番目の子ノードを返す</summary>
        public Node GetNthSubNode(int n)
        {
            return subTable?.GetNth(n);
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
            return subTable?.Length ?? 0;
        }

        public int FindSubNode(string s)
        {
            if (subTable != null) {
                for (int i = 0; i < subTable.Length; ++i) {
                    var outStr = subTable.GetNth(i)?.GetOutputString();
                    if (outStr != null && outStr.EqualsTo(s)) {
                        return i;
                    }
                }
            }
            return -1;
        }

        public NodeTable GetSubNodes() { return subTable; }

        public bool HasOutputString() { return outputStr._notEmpty(); }

        //public string GetOutputString() { return outputStr; }
        public OutputString GetOutputString() { return outputStr; }

        public bool IsBareString() { return outputStr?.IsBare() ?? false; }

        public string GetBaseQuotedString() { return outputStr?.GetBaseQuotedString(); }

        public string GetQuotedString() { return outputStr?.GetQuotedString(); }

        public bool IsFunctionNode() { return outputStr?.IsFunction() ?? false; }

        public bool IsStringNode() { return outputStr != null && !IsFunctionNode() && !IsTreeNode() && !IsRewriteNode(); }

        public bool IsRootTreeNode() { return subTable?.IsRoot ?? false; }

        public bool IsTreeNode() { return subTable != null; }

        public bool IsRewriteNode() { return rewriteMap != null; }

        public void AddSubTable()
        {
            if (subTable == null) subTable = new NodeTable();
        }

        /// <summary>RewriteNodeに変身させる</summary>
        public void MakeRewritable()
        {
            if (rewriteMap == null) rewriteMap = new RewriteMap();
        }

        /// <summary>ノードの内容をマージする</summary>
        /// <param name="node"></param>
        private bool merge(Node node)
        {
            // マージする
            bool bOverwritten = _merge(node);

            if (node.IsRewriteNode()) {
                // マージされたnodeがRewritableだったら自身もRewritableに変身させる
                MakeRewritable();
                node.rewriteMap._forEach((key, val) => {
                    upsertRewrteMap(key, val);
                });
            }
            return bOverwritten;
        }

        /// <summary>書き換え情報以外のノード内容をマージする。上書きが発生したら true を返す</summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool _merge(Node node)
        {
            bool bOverwrite = false;
            if ((node.IsStringNode() || node.outputStr._notEmpty()) && node.outputStr._ne(outputStr)) {
                if (outputStr._isEmpty() || IsFunctionNode() || !node.IsFunctionNode()) {
                    bOverwrite = outputStr._notEmpty() && !IsFunctionNode();
                    outputStr = node.outputStr;
                    //isBareStr = node.isBareStr;
                }
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
                            setNthSubNode(i, p);
                        }
                    }
                }
                //if (nodeType != NodeType.Rewrite) nodeType = NodeType.StrokeTree;
            }
            return bOverwrite;
        }

        private void upsertRewrteMap(string key, Node value)
        {
            if (rewriteMap == null) rewriteMap = new RewriteMap();
            if (rewriteMap.ContainsKey(key)) {
                rewriteMap.GetNode(key)?.merge(value);
            } else {
                rewriteMap.PutPair(key, value);
            }
        }

        public void UpsertRewritePair(string tgtStr, Node rewriteNode)
        {
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CALLED: tgtStr={tgtStr}, rewriteNode={rewriteNode.DebugString()}");
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
        private bool setNthSubNode(int n, Node node)
        {
            if (subTable != null) {
                if (n >= 0 && n < subTable.Length) {
                    if (subTable.GetNth(n) != null) {
                        if (node.IsRewriteNode() && subTable.GetNth(n).IsRewriteNode()) {
                            // 新旧ノードが RewriteNode である
                            subTable.GetNth(n).merge(node);
                            return false;
                        } else if (node.IsTreeNode() || !subTable.GetNth(n).IsTreeNode()) {
                            // 新旧ノードが StrokeTableNode であるか、旧ノードが StrokeTableNode でなければ、上書き
                            subTable.SetNth(n, node);
                            return !node.IsRewriteNode();  // 新ノードが RewriteNode なら上書き警告しない
                        } else {
                            // それ以外は node を捨てる
                            return true;
                        }
                    }
                    subTable.SetNth(n, node);
                }
            }
            return false;
        }

        /// <summary>
        /// n番目の子ノードに node をマージする。子ノードがなければセットする<br/>
        /// (マージされた新しいノードを返す。重複があったら true を返す)<br/>
        /// 旧ノードが StrokeTableNodeで、新ノードがそれ以外だったら、新ノードは無視されるので注意
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public (Node, bool) SetOrMergeNthSubNode(int n, Node node)
        {
            if (subTable != null) {
                if (n >= 0 && n < subTable.Length) {
                    bool bOverwritten = false;
                    if (subTable.GetNth(n) != null) {
                        bOverwritten = subTable.GetNth(n).merge(node);
                    } else {
                        subTable.SetNth(n, node);
                    }
                    return (subTable.GetNth(n), bOverwritten);
                }
            }
            return (null, false);
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
            if (rewriteMap._notEmpty() || outputStr._notEmpty()) {
                string leaderStr = list._notEmpty() ? $"-{list.Select(x => x.ToString())._join(">-")}>" : "";
                if (rewriteMap._notEmpty()) {
                    // 書き換えノード
                    outLines.Add(leaderStr + "@{" + GetBaseQuotedString());
                    rewriteMap.ForEach((key, node) => {
                        // 書き換えMapの先のノードは、文字列ノードかツリーノードとみなす
                        if (key._notEmpty() && node != null) {
                            if (node.HasSubNode()) {
                                outLines.Add($"{key}\t{{");
                                node.outputLine(outLines, null);  // 部分木の出力なので list = null にしている
                                outLines.Add("}");
                            } else if (node.HasOutputString()) {
                                outLines.Add($"{key}\t{node.GetQuotedString()}");
                            }
                        }
                    });
                    outLines.Add("}");
                } else {
                    // 文字列ノード
                    // ツリーのスレッドを leaderStr として、終端ノードの文字列を出力
                    outputString(outLines, leaderStr);
                }
            }
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
            }
        }

        protected virtual void outputString(List<string> outLines, string leaderStr)
        {
            if (HasOutputString()) {
                outLines.Add(leaderStr + GetQuotedString());
            }
        }

        public string DebugString()
        {
            return $"OutputStr={outputStr?.DebugString() ?? ""}, SubNodeNum={GetSubNodeNum()}, RewriteMapNum={rewriteMap._safeCount()}";
        }

    }

}
