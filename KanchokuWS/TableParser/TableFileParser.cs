using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using KanchokuWS.CombinationKeyStroke;
using KanchokuWS.CombinationKeyStroke.DeterminerLib;

namespace KanchokuWS.TableParser
{
    using ShiftKeyKind = ComboShiftKeyPool.ComboKind;

    /// <summary>
    /// テーブル解析器
    /// </summary>
    class TableParser : TableParserTokenizer
    {
        private static Logger logger = Logger.GetLogger();

        // 当Parserによる解析内容を格納するツリーノード
        // 部分的にトップノードになる場合は、parentNode = null にしておくこと
        protected Node _treeNode;

        protected Node TreeNode => _treeNode;

        // 部分木のルートノード
        // 通常は全体木のルートだが、書き換え定義中は部分木のルートになる
        private Node _rootNode;

        protected Node RootNode => _rootNode;

        // ルートノードから当ノードに至るまでの打鍵リスト
        private List<int> _strokeList = new List<int>();

        protected List<int> StrokeList => _strokeList;

        protected int Depth => StrokeList.Count;

        // 当ノードの ShiftPlane
        private int _shiftPlane = -1;

        protected int ShiftPlane => _shiftPlane >= 0 ? _shiftPlane : Context.shiftPlane;

        protected int shiftDecKey(int deckey)
        {
            return deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey : deckey + ShiftPlane * DecoderKeys.PLANE_DECKEY_NUM;
        }

        protected string getNthRootNodeString(int n)
        {
            int idx = shiftDecKey(n);
            return (RootNode?.GetNthSubNode(idx)?.GetOutputString())._stripDq()._toSafe();
        }

        private string makePathStr(int dropTailLen = 0)
        {
            int len = StrokeList.Count - dropTailLen;
            return len > 0 ? StrokeList.Take(len).Select(x => getNthRootNodeString(x))._join("") : "";
        }

        private string _leaderStr = null;

        protected string leaderStr {
            get {
        //protected string leaderStr => strokeList.Count > 1 ? strokeList.Take(strokeList.Count - 1).Select(x => getNthRootNodeString(x))._join("") : "";
                if (_leaderStr == null) _leaderStr = makePathStr(1);
                return _leaderStr;
            }
        }

        private string _pathStr = null;

        protected string pathStr {
            get {
                if (_pathStr == null) _pathStr = makePathStr(0);
                return _pathStr;
            }
        }

        /// <summary>
        /// ノード解析コンストラクタ<br/>
        /// 部分木のトップノードになる場合は、treeNode.parentNode = null にしておくこと
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableParser(Node rootNode, Node treeNode, List<int> stkList, int myStroke, int shiftPlane = -1)
            : base()
        {
            _rootNode = rootNode;
            _treeNode = treeNode;
            if (stkList._notEmpty()) _strokeList.AddRange(stkList);
            if (myStroke >= 0) {
                // 先頭キーはシフト化
                if (IsRootParser) {
                    myStroke = shiftDecKey(myStroke);
                } else {
                    // それ以外は Modulo
                    myStroke %= DecoderKeys.PLANE_DECKEY_NUM;
                }
                _strokeList.Add(myStroke);
            }
            _shiftPlane = shiftPlane;
        }

        /// <summary>
        /// n番目の子ノードをセットする(残ったほうのノードを返す)
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="n"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        protected Node SetNthSubNode(int n, Node node)
        {
            bool bOverwrite = TreeNode.SetNthSubNode(n, node);
            if (bOverwrite && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return TreeNode.GetNthSubNode(n);
        }

        ///// <summary>
        ///// n番目の子ノードをセットする(残ったほうのノードを返す)
        ///// </summary>
        ///// <param name="tbl"></param>
        ///// <param name="n"></param>
        ///// <param name="node"></param>
        ///// <returns></returns>
        //protected Node setNthChildNode(StrokeTableNode tbl, int n, Node node)
        //{
        //    bool bOverwrite = tbl.SetNthSubNode(n, node);
        //    if (bOverwrite && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
        //        logger.Warn($"DUPLICATED: {CurrentLine}");
        //        NodeDuplicateWarning();
        //    }
        //    return tbl.GetNthSubNode(n);
        //}

        /// <summary>
        /// TODO: 後で見直す
        /// lastStkに対応するノードを追加する。(lastStrk < 0 なら StrokeList の末尾 strokeを使う)<br/>
        /// node==null なら新しく TreeNode を作成する<br/>
        /// 途中、StrokeTableNodeが存在しない場所があれば、そこにStrokeTableNodeを生成して挿入(または置換)する<br/>
        /// 同時打鍵の中間キーのShift化も行う
        /// </summary>
        /// <param name="node"></param>
        protected Node SetNodeOrNewTreeNodeAtLast(int lastStk, Node node)
        {
            logger.DebugH(() => $"CALLED: stkList={StrokeList._keyString()}, lastStrk={lastStk}, isCombo={isInCombinationBlock}, {node?.DebugString() ?? "node=null"}");
            bool bOverwritten = false;
            if (StrokeList._isEmpty() && lastStk < 0) {
                logger.Warn($"strokeList is empty");
            } else {
                int endCount = lastStk >= 0 ? StrokeList.Count : StrokeList.Count - 1;

                int getStroke(int idx)
                {
                    // assert(idx < StrokeList.Count)
                    int stk = idx < StrokeList.Count ? StrokeList[idx] : lastStk;
                    if (isInCombinationBlock) {
                        // 同時打鍵定義ブロック
                        if (idx == 0) {
                            // 同時打鍵の先頭キーは Combo化(単打ノードの重複を避ける)
                            stk = makeComboDecKey(stk);
                        } else if (idx > 0 && (node == null || node.IsTreeNode() || idx + 1 < endCount)) {
                            // 同時打鍵の中間キー(非終端キー)は、Shift化して終端ノードとの重複を避ける
                            stk = makeNonTerminalDuplicatableComboKey(stk);
                        }
                    }
                    return stk;
                }

                var pn = RootNode;
                // 途中ノードのトラバース
                for (int i = 0; i < endCount; ++i) {
                    int stk = getStroke(i);
                    var nd = pn.GetNthSubNode(stk);
                    if (nd != null && nd.IsTreeNode()) {
                        pn = (StrokeTableNode)nd;
                    } else {
                        // 途中ノードが空(またはツリーノードでない)ならば、StrokeTableNodeを生成して挿入(または置換)する
                        bOverwritten = bOverwritten || (nd != null && !nd.IsFunctionNode());    // 置換先が機能ノード以外なら上書き(重複)警告を出す
                        var _pn = new StrokeTableNode();
                        pn.SetNthSubNode(stk, _pn);
                        pn = _pn;
                    }
                }

                if (pn == null) {
                    logger.Warn($"No such parent node: strokeList={StrokeList._keyString()}");
                } else {
                    int idx = getStroke(endCount);
                    var oldNode = pn.GetNthSubNode(idx);
                    if (node == null) {
                        if (oldNode == null || !oldNode.IsTreeNode()) {
                            // 新しく StrokeTableNode を作成して追加
                            node = new StrokeTableNode();
                            pn.SetNthSubNode(idx, node);
                            if (!bOverwritten) bOverwritten = oldNode != null && !oldNode.IsFunctionNode();     // 既存ノードが機能ノードでないので、上書き警告
                        } else {
                            node = oldNode;
                        }
                    } else {
                        pn.SetNthSubNode(idx, node);
                        if (!bOverwritten) {
                            // 既存が空でないツリーノードか、既存が FunctionNode でなく新規がRewriteNodeでない、なら上書き(重複)警告
                            bOverwritten = oldNode != null &&
                                ((oldNode.IsTreeNode() && oldNode.HasSubNode()) || ((!oldNode.IsFunctionNode() || oldNode.IsRewriteNode()) && !node.IsRewriteNode()));
                        }
                    }
                }
            }
            if (bOverwritten && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: strokeList={StrokeList._keyString()}, isCombo={isInCombinationBlock}, line={CurrentLine}");
                NodeDuplicateWarning();
            }

            return node;
        }

        //class StrokeListUndoer : IDisposable
        //{
        //    List<int> stkList = null;

        //    public StrokeListUndoer(List<int> list, int stroke)
        //    {
        //        if (stroke >= 0) {
        //            stkList = list;
        //            stkList.Add(stroke);
        //        }
        //    }

        //    public void Dispose()
        //    {
        //        stkList._popBack();
        //    }
        //}

        //StrokeListUndoer pushStroke(int stroke)
        //{
        //    if (stroke >= 0) {
        //        // 先頭キーはシフト化
        //        if (StrokeList._isEmpty()) {
        //            stroke = shiftDecKey(stroke);
        //        } else {
        //            // それ以外は Modulo
        //            stroke %= DecoderKeys.PLANE_DECKEY_NUM;
        //        }
        //    }
        //    return new StrokeListUndoer(StrokeList, stroke);
        //}

        //protected void addNodeTree(int stroke = -1)
        //{
        //    logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={strokeList._keyString()}, stroke={stroke}");
        //    using (pushStroke(stroke)) {
        //        // StrokeTableNodeを追加しておく(そうでないと矢印記法によって先に空のStrokeTableNodeが作られてしまう可能性があるため)
        //        setNodeOrNewTreeNodeAtLast(strokeList, null);
        //        new TableParser(context, strokeList, rootTableNode).MakeNodeTree();
        //        logger.DebugH(() => $"LEAVE");
        //    }
        //}

        protected static class VBarSeparationHelper
        {
            public static int calcRow(int idx, int currentRow)
            {
                if (idx <= 40) return idx / 10;
                return currentRow;
            }

            public static int calcOverrunIndex(int idx)
            {
                if (idx == 10) return 41;
                if (idx == 20) return 44;
                if (idx == 30) return 46;
                if (idx == 40) return 48;
                return idx;
            }

            public static int calcNewLinedIndex(int row)
            {
                return row * 10;
            }
        }

        /// <summary>
        /// 下位レベルのテーブル(ブロック)構造を解析してストロークノード木を構築する。
        /// </summary>
        public void ParseNodeBlock()
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={StrokeList._keyString()}");

            bool bError = false;
            int idx = 0;
            int row = 0;
            //bool isPrevDelim = true;
            TOKEN prevToken = 0;
            TOKEN prevPrevToken = 0;
            readNextToken();
            while (!bError && currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                int pos = idx;
                switch (currentToken) {
                    case TOKEN.LBRACE:
                        AddTreeNode(idx)?.ParseNodeBlock();
                        break;

                    case TOKEN.ARROW:
                        pos = ArrowIndex;
                        MakeArrowParser(ArrowIndex).Parse();
                        //isPrevDelim = false;
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        MakeArrowBundleParser(ArrowIndex).Parse();
                        break;

                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.BARE_STRING:        // str : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        AddLeafNode(currentToken, idx);
                        break;

                    case TOKEN.STRING_PAIR:
                        AddStringPairNode();
                        break;

                    case TOKEN.PLACE_HOLDER:
                        placeHolders.Put(CurrentStr, idx);
                        break;

                    case TOKEN.VBAR:               // 次のトークン待ち
                        // いろいろあって、ここは不要になった
                        //if ((prevToken == 0 || prevToken == TOKEN.VBAR) && isInSuccCombinationBlock && depth > 0) {
                        //    // 空セルで、同時打鍵ブロック内で、深さ2以上なら、同時打鍵可能(テーブルの出力定義はなし)としておく
                        //    // →たとえば、月光の連続シフトで「DKI」と打鍵したとき、「DK」でいったん同時打鍵成立と判定(出力は無し)して「K」を除去し、
                        //    // 次の「I」で「DI」⇒「よ」を出したいため。
                        //    // ただし、余計な同時打鍵候補が生成されることを防ぐため、固定順序の場合は、順序を置換した部分打鍵列を生成させないようにしておく必要あり。
                        //    logger.DebugH(() => $"CALL addCombinationKey(false): prevToken={prevToken}, depth={depth}");
                        //    using (pushStroke(idx)) {
                        //        addCombinationKey(false);
                        //    }
                        //}
                        row = VBarSeparationHelper.calcRow(idx, row);
                        idx = VBarSeparationHelper.calcOverrunIndex(idx + 1);
                        break;

                    case TOKEN.NEW_LINE:           // 次の行
                        if (prevToken == TOKEN.VBAR || prevPrevToken == TOKEN.VBAR) {
                            idx = VBarSeparationHelper.calcNewLinedIndex(++row);
                        }
                        break;

                    case TOKEN.COMMA:              // 次のトークン待ち
                    case TOKEN.SLASH:              // 次のトークン待ち
                        //if (isPrevDelim) ++idx;
                        //isPrevDelim = true;
                        ++idx;
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:                        // 途中でファイルが終わったりした場合 : エラー
                        ParseError($"MakeNodeTree: unexpected token: {currentToken}");
                        bError = true;
                        break;
                }

                prevPrevToken = prevToken;
                prevToken = currentToken;

                readNextToken();
            }

            if (Depth == 0) placeHolders.Initialize();
            logger.DebugH(() => $"LEAVE: lineNum={LineNumber}, depth={Depth}, bError={bError}");

        }

        //void parseNewBlock(int stroke = -1)
        //{
        //    logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={StrokeList._keyString()}, stroke={stroke}");
        //    // StrokeTableNodeを追加しておく(そうでないと矢印記法によって先に空のStrokeTableNodeが作られてしまう可能性があるため)
        //    Node node = SetNodeOrNewTreeNodeAtLast(stroke, null);
        //    if (node != null && node is StrokeTableNode) {
        //        // 新しい TableParser を作成してネストさせる
        //        new TableParser(RootNode, node, StrokeList, stroke).ParseNodeBlock();
        //    }
        //    logger.DebugH(() => $"LEAVE");
        //}

        //protected virtual void addNodeTreeByArrow(int idx)
        //{
        //    parseNewBlock();
        //}

        // 矢印記法(-\d+(,\d+)*>)の直後を解析して第1打鍵位置に従って配置する
        //public void parseArrowNode(int idx)
        //{
        //    logger.DebugH(() => $"ENTER: lineNum={LineNumber}, depth={Depth}, idx={idx}");
        //    //int arrowIdx = arrowIndex;
        //    readNextToken(true);
        //    switch (currentToken) {
        //        case TOKEN.ARROW:
        //            // 矢印記法の連続
        //            //handleArrowNode(ArrowIndex);
        //            MakeArrowParser(ArrowIndex).Parse();
        //            //if (prevToken == TOKEN.REWRITE_PRE) {
        //            //    addPreRewriteNode(RewritePreTargetStr).addArrowNode(TOKEN.ARROW, arrowIndex);
        //            //} else {
        //            //    addArrowNode(prevToken, arrowIndex);
        //            //}
        //            break;

        //        case TOKEN.REWRITE_PRE:
        //        case TOKEN.REWRITE_POST:
        //            ParseError($"トップレベル以外では前置または後置書き換え記法を使用できません。");
        //            break;

        //        case TOKEN.COMMA:
        //        case TOKEN.VBAR:
        //            if (parseArrow()) {
        //                // 矢印記法連続の簡略記法
        //                //handleArrowNode(ArrowIndex);
        //                MakeArrowParser(ArrowIndex).Parse();
        //                //if (prevToken == TOKEN.REWRITE_PRE) {
        //                //    addPreRewriteNode(RewritePreTargetStr).addArrowNode(TOKEN.ARROW, arrowIndex);
        //                //} else {
        //                //    addArrowNode(prevToken, arrowIndex);
        //                //}
        //            }
        //            break;

        //        case TOKEN.LBRACE:
        //            // ブロック開始
        //            parseNewBlock(idx);
        //            //switch (prevToken) {
        //            //    case TOKEN.ARROW:
        //            //        // -X>{ } 形式
        //            //        addNodeTreeByArrow();
        //            //        break;
        //            //    case TOKEN.REWRITE_PRE:
        //            //        // %X>{ } 形式
        //            //        addPreRewriteNode(RewritePreTargetStr).MakeNodeTree();
        //            //        break;
        //            //    case TOKEN.REWRITE_POST:
        //            //        // &X>{ } 形式
        //            //        addPostRewriteNode();
        //            //        break;
        //            //}
        //            break;

        //        case TOKEN.STRING:             // "str" : 文字列ノード
        //        case TOKEN.BARE_STRING:        // str : 文字列ノード
        //        case TOKEN.FUNCTION:           // @c : 機能ノード
        //                                       //if (prevToken == TOKEN.REWRITE_PRE) {
        //                                       //    addPreRewriteNode(RewritePreTargetStr).addLeafNode(currentToken, -1);
        //                                       //} else if (prevToken != TOKEN.ARROW) {
        //                                       //    ParseError($"前置または後置書き換えの場合は、ブロック記述が必要です。");
        //                                       //}
        //            AddLeafNode(currentToken, -1);  // idx は既にpush済み
        //            break;

        //        //case TOKEN.STRING_PAIR:
        //        //    addStringPairNode();
        //        //    break;

        //        default:
        //            ParseError("PreRewriteParser-addArrowNode");
        //            break;
        //    }

        //    currentToken = TOKEN.IGNORE;    // いったん末端ノードの処理をしたら、矢印記法を抜けるまで無視
        //    logger.DebugH(() => $"LEAVE: lineNum={LineNumber}, depth={Depth}");
        //} 

        //public virtual void handleArrowNode(int arrowIdx)
        //{
        //    new ArrowParser(RootNode, TreeNode, StrokeList, -1, arrowIdx).Parse();
        //}

        public virtual ArrowParser MakeArrowParser(int arrowIdx)
        {
            return new ArrowParser(RootNode, TreeNode, StrokeList, -1, arrowIdx);
        }

        protected ArrowBundleParser MakeArrowBundleParser(int nextArrowIdx)
        {
            return new ArrowBundleParser(RootNode, StrokeList, -1, nextArrowIdx);
        }

        public void AddLeafNode(TOKEN token, int idx)
        {
            switch (token) {
                case TOKEN.STRING:             // "str" : 文字列ノード
                    AddStringNode(idx, false);
                    break;
                case TOKEN.BARE_STRING:        // str : 文字列ノード
                    AddStringNode(idx, true);
                    break;
                case TOKEN.FUNCTION:           // @c : 機能ノード
                    addFunctionNode(idx);
                    break;
            }
        }

        protected virtual void AddStringNode(int idx, bool bBare)
        {
            logger.DebugH(() => $"ENTER: depth={Depth}, bBare={bBare}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            addTerminalNode(idx, bBare, new StringNode($"{ConvertKanji(CurrentStr)._safeReplace(@"\", @"\\")._safeReplace(@"""", @"\""")}", bBare));
            if (Depth == 1 && CurrentStr._startsWith("!{")) {
                // Repeatable Key
                logger.DebugH(() => $"REPEATABLE");
                keyComboPool?.AddRepeatableKey(StrokeList[0]);
            }
            logger.DebugH(() => $"LEAVE: depth={Depth}");
        }

        protected virtual void AddStringPairNode()
        {
            ParseError($"unexpected token: {currentToken}");
        }

        void addFunctionNode(int idx)
        {
            logger.DebugH(() => $"ENTER: depth={Depth}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            addTerminalNode(idx, false, new FunctionNode(CurrentStr));
            logger.DebugH(() => $"LEAVE: depth={Depth}");
        }

        /// <summary>
        /// 終端ノードの追加と同時打鍵列の組合せの登録<br/>
        /// 同時打鍵の場合は、ブロックのルートキーをCOMBO_DECKEY_STARTまでシフトする
        /// </summary>
        /// <param name="prevNth"></param>
        /// <param name="lastNth"></param>
        void addTerminalNode(int idx, bool bBare, Node node)
        {
            addCombinationKey(true);

            SetNodeOrNewTreeNodeAtLast(idx, node);
        }

        void addCombinationKey(bool hasStr)
        {
            var list = new List<int>(StrokeList);

            if (list._notEmpty()) {
                int shiftOffset = calcShiftOffset(list[0]);

                // 先頭キーはシフト化
                if (isInCombinationBlock) {
                    list[0] = makeComboDecKey(list[0]);
                    for (int i = 1; i < list.Count - 1; ++i) {
                        // 同時打鍵での中間キーは終端キーとの重複を避けるためシフト化しておく
                        list[i] = makeNonTerminalDuplicatableComboKey(list[i]);
                    }
                } else if (list[0] < DecoderKeys.PLANE_DECKEY_NUM) {
                    list[0] += shiftOffset;
                }

                if (isInCombinationBlock || list.Count == 1) {
                    MakeCombinationKeyCombo(list, shiftOffset, hasStr, bComboEffectiveAlways);
                } else {
                    if (keyComboPool != null) keyComboPool.ContainsSequentialShiftKey = true;
                    for (int i = 0; i < list.Count - 1; ++i) {
                        int dk = list[i];
                        if (!sequentialShiftKeys.Contains(dk)) {
                            addSequentialShiftKey(dk, shiftOffset);
                            sequentialShiftKeys.Add(dk);
                        }
                    }
                }
            }
        }

        // 前置書き換えノード
        //protected PreRewriteParser addPreRewriteNode(string targetStr)
        //{
        //    logger.DebugH(() => $"ENTER");

        //    if (StrokeList.Count > (targetStr._notEmpty() ? 0 : 1)) {
        //        ParseError($"深さが1以上の前置書き換えはサポートされていません。");
        //    }

        //    // 前置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
        //    if (targetStr._isEmpty()) {
        //        if (StrokeList.Count > 0) {
        //            int preIdx = StrokeList[0];
        //            targetStr = preIdx >= 0 ? getNthRootNodeString(preIdx) : "";
        //            //keyComboPool?.AddPreRewriteKey(preIdx);
        //        }
        //    }

        //    // 前置書き換えノード処理用のパーザを作成
        //    var parser = new PreRewriteParser(RootNode, StrokeList, targetStr);

        //    logger.DebugH(() => $"LEAVE");
        //    return parser;
        //}

        // 前置書き換えパーザ
        protected PreRewriteParser MakePreRewriteParser(int idx)
        {
            logger.DebugH(() => $"ENTER");

            var targetStr = RewritePreTargetStr;

            // 前置文字列の指定がなければ単打テーブルから文字を拾ってくる
            if (targetStr._isEmpty()) {
                targetStr = idx >= 0 ? getNthRootNodeString(idx) : "";
            }

            // 前置書き換えノード処理用のパーザを作成
            var parser = new PreRewriteParser(RootNode, TreeNode, StrokeList, -1, idx, targetStr);

            logger.DebugH(() => $"LEAVE");
            return parser;
        }

        // 後置書き換えパーザ
        protected PostRewriteParser MakePostRewriteParser(int idx)
        {
            logger.DebugH(() => $"ENTER");

            var targetStr = RewritePreTargetStr;

            // 前置文字列の指定がなければ単打テーブルから文字を拾ってくる
            if (targetStr._isEmpty()) {
                targetStr = idx >= 0 ? getNthRootNodeString(idx) : "";
            }

            // 前置書き換えノード処理用のパーザを作成
            var parser = new PostRewriteParser(RootNode, TreeNode, StrokeList, -1, idx, null);

            logger.DebugH(() => $"LEAVE");
            return parser;
        }

        // 後置書き換えノード
        //protected PostRewriteParser AddPostRewriteNode(int idx)
        //{
        //    logger.DebugH("ENTER");

        //    Node node = null;
        //    bool bBare = ReadWordOrString();
        //    var myStr = CurrentStr;
        //    if (isInCombinationBlock) {
        //        // 同時打鍵の場合
        //        addCombinationKey(true);
        //        node = new RewriteNode(myStr, bBare);
        //        SetNodeOrNewTreeNodeAtLast(idx, node);
        //    } else {
        //        // 後置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
        //        //int lastIdx = StrokeList._getLast();
        //        int lastIdx = idx;
        //        myStr = myStr._orElse(() => lastIdx >= 0 ? getNthRootNodeString(lastIdx) : "");
        //        node = SetNthSubNode(lastIdx, new RewriteNode(myStr, bBare));
        //    }
        //    PostRewriteParser parser = null;
        //    if (node.IsRewriteNode()) {
        //        // RewriteNode がノード木に反映された場合に限り、後置書き換えノードの処理を行う
        //        //parser = new PostRewriteParser(RootNode, StrokeList, idx, node, leaderStr);
        //    } else {
        //        logger.Warn("RewriteNode NOT merged");
        //    }
        //    logger.DebugH("LEAVE");

        //    return parser;
        //}

        int calcShiftOffset(int deckey)
        {
            return (deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey / DecoderKeys.PLANE_DECKEY_NUM : ShiftPlane) * DecoderKeys.PLANE_DECKEY_NUM;
        }

        // 同時打鍵列の組合せを作成して登録しておく
        protected void MakeCombinationKeyCombo(List<int> deckeyList, int shiftOffset, bool hasStr, bool effectiveAlways)
        {
            logger.DebugH(() => $"{deckeyList._keyString()}={CurrentStr}, shiftOffset={shiftOffset}, hasStr={hasStr}");
            var comboKeyList = deckeyList.Select(x => makeShiftedDecKey(x, shiftOffset)).ToList();      // 先頭キーのオフセットに合わせる
            keyComboPool?.AddComboShiftKey(comboKeyList[0], shiftKeyKind); // 元の拡張シフトキーコードに戻して、同時打鍵キーとして登録
            keyComboPool?.AddEntry(deckeyList, comboKeyList, shiftKeyKind, hasStr, effectiveAlways);
        }

        void addSequentialShiftKey(int decKey, int shiftOffset)
        {
            keyComboPool?.AddComboShiftKey(makeShiftedDecKey(decKey, shiftOffset), ShiftKeyKind.SequentialShift);
        }

        int makeComboDecKey(int decKey)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + DecoderKeys.COMBO_DECKEY_START;
        }

        int makeShiftedDecKey(int decKey, int shiftOffset)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + shiftOffset;
        }

        int makeNonTerminalDuplicatableComboKey(int decKey)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + DecoderKeys.PLANE_DECKEY_NUM;
        }

        protected virtual TableParser AddTreeNode(int idx)
        {
            Node node = SetNodeOrNewTreeNodeAtLast(idx, null);
            if (node != null && node is StrokeTableNode) {
                return new TableParser(RootNode, node, StrokeList, idx);
            } else {
                return null;
            }
        }

    }

    /// <summary>
    /// 矢印記法(-nn>)の解析
    /// </summary>
    class ArrowParser : TableParser
    {
        private static Logger logger = Logger.GetLogger();

        protected int myArrowIndex;

        /// <summary>
        /// コンストラクタ<br/>
        /// パーザ構築時点では、まだRewriteNodeは生成されない
        /// </summary>
        public ArrowParser(Node rootNode, Node treeNode, List<int> stkList, int lastStk, int arrowIdx)
            : base(rootNode, treeNode, stkList, lastStk)
        {
            myArrowIndex = arrowIdx;
        }

        // 矢印記法(-\d+(,\d+)*>)の直後を解析して第1打鍵位置に従って配置する
        public void Parse()
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, depth={Depth}, arrowIndex={myArrowIndex}");
            //int arrowIdx = arrowIndex;
            readNextToken(true);
            switch (currentToken) {
                case TOKEN.ARROW:
                    // 矢印記法の連続
                    AddArrowNode(ArrowIndex).Parse();
                    //if (prevToken == TOKEN.REWRITE_PRE) {
                    //    addPreRewriteNode(RewritePreTargetStr).addArrowNode(TOKEN.ARROW, arrowIndex);
                    //} else {
                    //    addArrowNode(prevToken, arrowIndex);
                    //}
                    break;

                case TOKEN.REWRITE_PRE:
                case TOKEN.REWRITE_POST:
                    ParseError($"トップレベル以外では前置または後置書き換え記法を使用できません。");
                    break;

                case TOKEN.COMMA:
                case TOKEN.VBAR:
                    if (parseArrow()) {
                        // 矢印記法連続の簡略記法
                        AddArrowNode(myArrowIndex).Parse();
                        //if (prevToken == TOKEN.REWRITE_PRE) {
                        //    addPreRewriteNode(RewritePreTargetStr).addArrowNode(TOKEN.ARROW, arrowIndex);
                        //} else {
                        //    addArrowNode(prevToken, arrowIndex);
                        //}
                    }
                    break;

                case TOKEN.LBRACE:
                    // ブロック開始
                    AddTreeNode(myArrowIndex)?.ParseNodeBlock();
                    //switch (prevToken) {
                    //    case TOKEN.ARROW:
                    //        // -X>{ } 形式
                    //        addNodeTreeByArrow();
                    //        break;
                    //    case TOKEN.REWRITE_PRE:
                    //        // %X>{ } 形式
                    //        addPreRewriteNode(RewritePreTargetStr).MakeNodeTree();
                    //        break;
                    //    case TOKEN.REWRITE_POST:
                    //        // &X>{ } 形式
                    //        addPostRewriteNode();
                    //        break;
                    //}
                    break;

                case TOKEN.STRING:             // "str" : 文字列ノード
                case TOKEN.BARE_STRING:        // str : 文字列ノード
                case TOKEN.FUNCTION:           // @c : 機能ノード
                    AddLeafNode(currentToken, myArrowIndex);
                    break;

                //case TOKEN.STRING_PAIR:
                //    addStringPairNode();
                //    break;

                default:
                    ParseError("PreRewriteParser-addArrowNode");
                    break;
            }

            currentToken = TOKEN.IGNORE;    // いったん末端ノードの処理をしたら、矢印記法を抜けるまで無視
            logger.DebugH(() => $"LEAVE: lineNum={LineNumber}, depth={Depth}");
        } 

        protected virtual ArrowParser AddArrowNode(int arrowIdx)
        {
            Node node = SetNodeOrNewTreeNodeAtLast(myArrowIndex, null);
            return new ArrowParser(RootNode, node, StrokeList, myArrowIndex, arrowIdx);
        }

    }

    /// <summary>
    /// 矢印束記法(-*>-nn>)の解析
    /// </summary>
    class ArrowBundleParser : TableParser
    {
        private static Logger logger = Logger.GetLogger();

        int nextArrowIndex;

        /// <summary>
        /// コンストラクタ<br/>
        /// パーザ構築時点では、まだRewriteNodeは生成されない
        /// </summary>
        public ArrowBundleParser(Node rootNode, List<int> stkList, int lastStk, int nextArrowIdx)
            : base(rootNode, null, stkList, lastStk)
        {
            nextArrowIndex = nextArrowIdx;
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        public Node Parse()
        {
            logger.DebugH(() => $"ENTER: depth={Depth}, nextArrowIndex={nextArrowIndex}");

            Node myNode = null;

            int n = 0;
            int row = 0;
            //bool isPrevDelim = true;
            readNextToken(true);
            if (currentToken != TOKEN.LBRACE) { // 直後は '{' でブロックの始まりである必要がある
                ParseError($"parseArrowBundleNode: TOKEN.LBRACE is excpected, but {currentToken}");
                return myNode;
            }
            TOKEN prevToken = 0;
            TOKEN prevPrevToken = 0;
            readNextToken();
            while (currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.BARE_STRING:        // str : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        AddTreeNode(n)?.AddLeafNode(currentToken, nextArrowIndex);
                        break;

                    case TOKEN.PLACE_HOLDER:
                        placeHolders.Put(CurrentStr, n);
                        break;

                    case TOKEN.VBAR:              // 次のトークン待ち
                        row = VBarSeparationHelper.calcRow(n, row);
                        n = VBarSeparationHelper.calcOverrunIndex(n + 1);
                        break;

                    case TOKEN.NEW_LINE:
                        if (prevToken == TOKEN.VBAR || prevPrevToken == TOKEN.VBAR) {
                            n = VBarSeparationHelper.calcNewLinedIndex(++row);
                        }
                        break;

                    case TOKEN.COMMA:              // 次のトークン待ち
                    case TOKEN.SLASH:              // 次のトークン待ち
                        ++n;
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:                        // 途中でファイルが終わったりした場合 : エラー
                        ParseError($"parseArrowBundleNode: unexpected token: {currentToken}");
                        break;
                }
                prevPrevToken = prevToken;
                prevToken = currentToken;

                readNextToken();
            }

            if (Depth == 0) placeHolders.Initialize();
            logger.DebugH(() => $"LEAVE: depth={Depth}");

            return myNode;
        }

    }

    /// <summary>
    /// 前置書き換えブロックの解析
    /// </summary>
    class PreRewriteParser : ArrowParser
    {
        private static Logger logger = Logger.GetLogger();

        string targetStr;

        /// <summary>
        /// コンストラクタ<br/>
        /// パーザ構築時点では、まだRewriteNodeは生成されない
        /// </summary>
        public PreRewriteParser(Node rootNode, Node treeNode, List<int> stkList, int lastStk, int arrowIdx, string targetStr)
            : base(rootNode, treeNode, stkList, lastStk, arrowIdx)
        {
            this.targetStr = targetStr;
        }

        // TODO: プロック処理
        protected override TableParser AddTreeNode(int idx)
        {
            Node node = SetNodeOrNewTreeNodeAtLast(idx, null);
            if (node != null && node is StrokeTableNode) {
                return new TableParser(RootNode, node, StrokeList, idx);
            } else {
                return null;
            }
        }

        // 前置書き換え用の矢印記法
        protected override ArrowParser AddArrowNode(int arrowIdx)
        {
            Node node = SetNodeOrNewTreeNodeAtLast(myArrowIndex, null);
            return new PreRewriteParser(RootNode, node, StrokeList, myArrowIndex, arrowIdx, targetStr);
        }
        //public override void handleArrowNode(int arrowIdx)
        //{
        //    addPreRewriteNode(RewritePreTargetStr).parseArrowNode(ArrowIndex);
        //}

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        //protected override void addNodeTreeByArrow(int idx)
        //{
        //    logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={StrokeList._keyString()}, idx={idx}");
        //    // 新しいテーブルを作成し、それがネストされたルートテーブルとする
        //    var nestedRootTblNode = new StrokeTableNode();
        //    var node = addTreeOrStringNode(idx, "", nestedRootTblNode);
        //    if (node.IsRewriteNode()) {
        //        new TableParser(nestedRootTblNode, node, null, idx).ParseNodeBlock();
        //    }
        //    logger.DebugH(() => $"LEAVE");
        //}

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        protected override void AddStringNode(int idx, bool bBare)
        {
            logger.DebugH(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={idx}");
            addTreeOrStringNode(idx, CurrentStr._quoteString(bBare), null);
            logger.DebugH("LEAVE");
        }

        Node addTreeOrStringNode(int idx, string outStr, StrokeTableNode tblNode)
        {
            //int lastIdx = shiftDecKey(StrokeList._getLast());
            int lastIdx = shiftDecKey(idx);
            string myStr = getNthRootNodeString(lastIdx);
            var node = SetNthSubNode(lastIdx, new RewriteNode(myStr, false));
            if (node.IsRewriteNode()) {
                // RewriteNode がノード木に反映された場合に限り、以下を実行
                var tgtStr = Helper.ConcatDqString(targetStr, leaderStr);
                node.AddRewritePair(tgtStr, new RewriteNode(outStr, tblNode.GetSubNodes(), false));
            } else {
                logger.Warn("RewriteNode NOT merged");
            }
            return node;
        }

    }

    /// <summary>
    /// 後置書き換えブロックの解析
    /// </summary>
    class PostRewriteParser : ArrowParser
    {
        private static Logger logger = Logger.GetLogger();

        Node _rewriteNode = null;

        new string leaderStr;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public PostRewriteParser(Node rootNode, Node rewriteNode, List<int> stkList, int lastStk, int arrowIdx, string leaderStr)
            : base(rootNode, rewriteNode, stkList, lastStk, arrowIdx)
        {
            this.leaderStr = leaderStr;
        }

        // TODO: プロック処理
        protected override TableParser AddTreeNode(int idx)
        {
            Node node = SetNodeOrNewTreeNodeAtLast(idx, null);
            if (node != null && node is StrokeTableNode) {
                return new TableParser(RootNode, node, StrokeList, idx);
            } else {
                return null;
            }
        }

        // 後置書き換え用の矢印記法
        protected override ArrowParser AddArrowNode(int arrowIdx)
        {
            ParseError($"");
            return null;
        }
        //public override void handleArrowNode(int arrowIdx)
        //{
        //    parseArrowNode(arrowIdx);
        //}

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        //protected override void addNodeTreeByArrow(int idx)
        //{
        //    parseNewBlock();
        //}

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        protected override void AddStringNode(int idx, bool bBare)
        {
            logger.DebugH(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={StrokeList._getLast()}");
            var tgtStr = Helper.ConcatDqString(pathStr, leaderStr);
            //var outStr = CurrentStr._quoteString(bBare);
            _rewriteNode.AddRewritePair(tgtStr, new RewriteNode(CurrentStr, bBare));
            logger.DebugH("LEAVE");
        }

        // 書き換え文字列のペア
        protected override void AddStringPairNode()
        {
            var str1 = StringPair._getNth(0);
            var str2 = StringPair._getNth(1);
            logger.DebugH(() => $"ENTER: str1={str1}, str2={str2}");
            if (str1._isEmpty() || str2._isEmpty()) {
                ParseError("Invalid String Piar");
            } else {
                _rewriteNode.AddRewritePair(str1, new RewriteNode(str2, false));
            }
            logger.DebugH("LEAVE");
        }

    }

    /// <summary>
    /// ルートテーブルの解析
    /// </summary>
    class RootTableParser : TableParser
    {
        private static Logger logger = Logger.GetLogger();

        // グローバルなルートノードか
        protected override bool IsRootParser => true;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public RootTableParser()
            : base(ParserContext.Singleton.rootTableNode, ParserContext.Singleton.rootTableNode, null, -1)
        {
        }

        /// <summary>
        /// トップレベルのテーブル定義を解析してストローク木を構築する。<br/>
        /// 前置・後置の書き換えモードは、このトップレベルでしか記述できない。
        /// </summary>
        public void ParseRootTable()
        {
            logger.InfoH($"ENTER");

            // トップレベルの解析
            readNextToken(true);
            while (Context.currentToken != TOKEN.END) {
                switch (Context.currentToken) {
                    case TOKEN.LBRACE:
                        ParseNodeBlock();
                        break;

                    case TOKEN.ARROW:
                        // -X>形式
                        //handleArrowNode(ArrowIndex);
                        MakeArrowParser(ArrowIndex).Parse();
                        break;

                    case TOKEN.REWRITE_PRE:
                        // %X>形式: 前置書き換えモードに移行
                        MakePreRewriteParser(ArrowIndex).Parse();
                        break;

                    case TOKEN.REWRITE_POST:
                        // &X>形式: 後置書き換えモードに移行
                        MakePostRewriteParser(ArrowIndex).Parse();
                        //AddPostRewriteNode(ArrowIndex)?.ParseNodeBlock();
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        MakeArrowBundleParser(Context.arrowIndex).Parse();
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:
                        Context.tableLines.ParseError();
                        break;
                }
                readNextToken(true);
            }

            // 拡張修飾キーが同時打鍵キーとして使われた場合は、そのキーの単打設定として本来のキー出力を追加する
            addExtModfierAsSingleHitKey();

            // 部分キーに対して、非終端マークをセット
            Context.keyComboPool?.SetNonTerminalMarkForSubkeys();
            if (Logger.IsInfoHEnabled && logger.IsInfoHPromoted) {
                Context.keyComboPool?.DebugPrint();
            }

            if (Context.bRewriteEnabled) {
                // 書き換えノードがあったら、SandSの疑似同時打鍵サポートをOFFにしておく
                Settings.SandSEnablePostShift = false;
            }

            // 全ノードの情報を OutputLines に書き出す
            RootNode.OutputLine(OutputLines);

            // ルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる
            addMyCharFunctionInRootStrokeTable();

            logger.InfoH($"LEAVE: KeyCombinationPool.Count={Context.keyComboPool?.Count}");
        }

        public void ParseDirectives()
        {
            readNextToken();
            while (currentToken != TOKEN.END) {
                SkipToEndOfLine();
                readNextToken();
            }
        }

        /// <summary>拡張修飾キーが同時打鍵キーとして使われた場合は、そのキーの単打設定として本来のキー出力を追加する</summary>
        void addExtModfierAsSingleHitKey()
        {
            void addExtModAsSingleKey(string keyName)
            {
                int dk = VirtualKeys.GetFuncDeckeyByName(keyName);
                if (dk >= 0) {
                    if (RootNode.GetNthSubNode(dk) == null && RootNode.GetNthSubNode(dk + DecoderKeys.COMBO_DECKEY_START) != null) {
                        // 単打設定が存在せず、同時打鍵の先頭キーになっている場合は、単打設定を追加する
                        MakeCombinationKeyCombo(Helper.MakeList(dk), 0, true, false);  // 単打指定
                        OutputLines.Add($"-{dk}>\"!{{{keyName}}}\"");
                    }
                }
            }

            if (Settings.UseComboExtModKeyAsSingleHit) {
                // とりあえず nfer と xfer だけ対応
                addExtModAsSingleKey("nfer");
                addExtModAsSingleKey("xfer");
            }
        }

        /// <summary>もしルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる</summary>
        void addMyCharFunctionInRootStrokeTable()
        {
            for (int idx = 0; idx < DecoderKeys.NORMAL_DECKEY_NUM; ++idx) {
                if (RootNode.GetNthSubNode(idx) == null) {
                    OutputLines.Add($"-{idx}>@^");
                }
            }
        }

    }

    /// <summary>
    /// テーブルファイル解析器
    /// </summary>
    class TableFileParser
    {
        private static Logger logger = Logger.GetLogger();

        TableLines tableLines = new TableLines();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TableFileParser()
        {
        }

        /// <summary>
        /// テーブル定義を解析してストローク木を構築する。
        /// 解析結果を矢印記法に変換して出力ファイル(outFile)に書き込む
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="outFilename"></param>
        /// <param name="pool">対象となる KeyComboPool</param>
        public void ParseTableFile(string filename, string outFilename, KeyCombinationPool pool, bool primary, bool bWriteExpandedTableLines = false, bool bTest = false)
        {
            logger.InfoH($"ENTER: filename={filename}");
            tableLines.ReadAllLines(filename);

            if (tableLines.NotEmpty) {
                ParserContext.CreateSingleton(tableLines, pool, primary);
                var parser = new RootTableParser();
                parser.ParseRootTable();
                writeAllLines(outFilename, ParserContext.Singleton.OutputLines);
                if (bWriteExpandedTableLines) writeAllLines($"tmp/parsedTableFile{(primary ? 1 : 2)}.txt", ParserContext.Singleton.tableLines.GetLines());
            } else {
                tableLines.Error($"テーブルファイル({filename})が開けません");
            }

            if (!bWriteExpandedTableLines && !bTest) tableLines.showErrorMessage();

            logger.InfoH($"LEAVE");
        }

        /// <summary>
        /// テーブル定義を読んでディレクティブだけを解析する
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="outFilename"></param>
        /// <param name="pool">対象となる KeyComboPool</param>
        public void ReadDirectives(string filename, bool primary)
        {
            logger.InfoH($"ENTER: filename={filename}");
            tableLines.ReadAllLines(filename);

            if (tableLines.NotEmpty) {
                ParserContext.CreateSingleton(tableLines, null, primary);
                var parser = new RootTableParser();
                parser.ParseDirectives();
            } else {
                tableLines.Error($"テーブルファイル({filename})が開けません");
            }

            tableLines.showErrorMessage();

            logger.InfoH($"LEAVE");
        }

        private void writeAllLines(string filename, List<string> lines)
        {
            if (filename._notEmpty()) {
                var path = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
                Helper.CreateDirectory(path._getDirPath());
                logger.InfoH($"ENTER: path={path}");
                Helper.WriteLinesToFile(path, lines, (e) => logger.Error(e._getErrorMsg()));
                logger.InfoH($"LEAVE");
            }
        }

    }

}
