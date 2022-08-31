﻿using System;
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

    // ルートノードから当ノードに至るまでの打鍵リスト
    class CStrokeList
    {
        private TableParser parser;

        public List<int> strokeList { get; private set; } = new List<int>();

        // 追加の末尾打鍵
        public int LastStroke { get; set; } = -1;

        public int Count => strokeList.Count;

        public bool IsEmpty => Count <= 0;

        public int At(int idx) { return strokeList[idx]; }

        public int Last() { return strokeList._getLast(); }


        public CStrokeList(TableParser parser, CStrokeList strkList)
        {
            this.parser = parser;
            if (strkList != null && strkList.strokeList._notEmpty()) strokeList.AddRange(strkList.strokeList);
        }

        public CStrokeList WithLastStrokeAdded(int lastStroke)
        {
            var result = new CStrokeList(parser, this);
            if (lastStroke >= 0) {
                // 先頭キーはシフト化
                if (parser.IsRootParser) {
                    lastStroke = parser.ShiftDecKey(lastStroke);
                } else {
                    // それ以外は Modulo
                    lastStroke %= DecoderKeys.PLANE_DECKEY_NUM;
                }
                result.strokeList.Add(lastStroke);
            }
            return result;
        }

        public string MakePathString(int dropTailLen = 0)
        {
            int len = strokeList.Count - dropTailLen;
            return len > 0 ? strokeList.Take(len).Select(x => parser.GetNthRootNodeString(x))._join("") : "";
        }

        public string DebugString()
        {
            return strokeList._keyString();
        }

    }

    /// <summary>
    /// テーブル解析器
    /// </summary>
    class TableParser : TableParserTokenizer
    {
        private static Logger logger = Logger.GetLogger();

        // 当Parserによる解析内容を格納するツリーノード
        // 部分的にトップノードになる場合は、parentNode = null にしておくこと
        Node _treeNode;

        protected Node TreeNode => _treeNode;

        // 部分木のルートノード
        // 通常は全体木のルートだが、書き換え定義中は部分木のルートになる
        Node _rootNode;

        protected Node RootNode => _rootNode;

        // ルートノードから当ノードに至るまでの打鍵リスト
        CStrokeList _strokeList = null;

        protected CStrokeList StrokeList => _strokeList;

        protected int Depth => StrokeList.Count;

        // 当ノードの ShiftPlane
        int _shiftPlane = -1;

        protected int ShiftPlane => _shiftPlane >= 0 ? _shiftPlane : Context.shiftPlane;

        public int ShiftDecKey(int deckey)
        {
            return deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey : deckey + ShiftPlane * DecoderKeys.PLANE_DECKEY_NUM;
        }

        int calcShiftOffset(int deckey)
        {
            return (deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey / DecoderKeys.PLANE_DECKEY_NUM : ShiftPlane) * DecoderKeys.PLANE_DECKEY_NUM;
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

        public Node GetNthRootNode(int n)
        {
            return RootNode?.GetNthSubNode(ShiftDecKey(n));
        }

        public string GetNthRootNodeString(int n)
        {
            return (GetNthRootNode(n)?.GetOutputString())._toSafe();
        }

        public Node GetNthSubNode(int n)
        {
            return TreeNode?.GetNthSubNode(n);
        }

        private string makePathStr(int dropTailLen = 0)
        {
            return StrokeList.MakePathString(dropTailLen);
        }

        private string _leaderStr = null;

        protected string LeaderStr {
            get {
                if (_leaderStr == null) _leaderStr = makePathStr(1);
                return _leaderStr;
            }
        }

        private string _pathStr = null;

        protected string PathStr {
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
        public TableParser(Node rootNode, Node treeNode, CStrokeList strkList, int shiftPlane = -1)
            : base()
        {
            _rootNode = rootNode;
            _treeNode = treeNode;
            _strokeList = new CStrokeList(this, strkList);
            _shiftPlane = shiftPlane;
        }

        /// <summary>
        /// n番目の子ノードをセットする(残ったほうのノードを返す)
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="idx"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        protected Node SetNthSubNode(int idx, Node node)
        {
            if (isInCombinationBlock) {
                // 同時打鍵定義ブロック
                int depth = Depth;
                if (depth == 0) {
                    // 同時打鍵の先頭キーは Combo化(単打ノードの重複を避ける)
                    idx = makeComboDecKey(idx);
                } else if (node.IsTreeNode()) {
                    // 同時打鍵の中間キー(非終端キー)は、Shift化して終端ノードとの重複を避ける
                    idx = makeNonTerminalDuplicatableComboKey(idx);
                }
            }
            bool bOverwrite = TreeNode.SetNthSubNode(idx, node);
            if (bOverwrite && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return TreeNode.GetNthSubNode(idx);
        }

        /// <summary>
        /// n番目の子ノードをマージする(残ったほうのノードを返す)
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="idx"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        protected Node SetOrMergeNthSubNode(int idx, Node node)
        {
            if (isInCombinationBlock) {
                // 同時打鍵定義ブロック
                int depth = Depth;
                if (depth == 0) {
                    // 同時打鍵の先頭キーは Combo化(単打ノードの重複を避ける)
                    idx = makeComboDecKey(idx);
                } else if (node.IsTreeNode()) {
                    // 同時打鍵の中間キー(非終端キー)は、Shift化して終端ノードとの重複を避ける
                    idx = makeNonTerminalDuplicatableComboKey(idx);
                }
            }
            (Node mergedNode, bool bOverwrite) = TreeNode.SetOrMergeNthSubNode(idx, node);
            if (bOverwrite && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return mergedNode;
        }

        ///// <summary>
        ///// TODO: 後で見直す
        ///// lastStkに対応するノードを追加する。(lastStrk < 0 なら StrokeList の末尾 strokeを使う)<br/>
        ///// node==null なら新しく TreeNode を作成する<br/>
        ///// 途中、StrokeTableNodeが存在しない場所があれば、そこにStrokeTableNodeを生成して挿入(または置換)する<br/>
        ///// 同時打鍵の中間キーのShift化も行う
        ///// </summary>
        ///// <param name="node"></param>
        //protected Node SetNodeOrNewTreeNodeAtLast(CStrokeList strokeList, Node node)
        //{
        //    logger.DebugH(() => $"CALLED: strkList={strokeList.DebugString()}, isCombo={isInCombinationBlock}, {node?.DebugString() ?? "node=null"}");
        //    bool bOverwritten = false;
        //    if (strokeList.IsEmpty) {
        //        logger.Warn($"strokeList is empty");
        //    } else {
        //        int endCount = strokeList.Count - 1;

        //        int getStroke(int idx)
        //        {
        //            // assert(idx < strokeList.Count)
        //            int stk = strokeList.At(idx);
        //            if (isInCombinationBlock) {
        //                // 同時打鍵定義ブロック
        //                if (idx == 0) {
        //                    // 同時打鍵の先頭キーは Combo化(単打ノードの重複を避ける)
        //                    stk = makeComboDecKey(stk);
        //                } else if (idx > 0 && (node == null || node.IsTreeNode() || idx + 1 < endCount)) {
        //                    // 同時打鍵の中間キー(非終端キー)は、Shift化して終端ノードとの重複を避ける
        //                    stk = makeNonTerminalDuplicatableComboKey(stk);
        //                }
        //            }
        //            return stk;
        //        }

        //        var pn = RootNode;
        //        Node convertToTreeNode(int stk)
        //        {
        //            var nd = pn.GetNthSubNode(stk);
        //            if (nd == null) {
        //                // 途中ノードが空
        //                var tn = Node.MakeTreeNode();
        //                pn.SetNthSubNode(stk, tn);
        //                return tn;
        //            } else {
        //                if (!nd.IsTreeNode()) {
        //                    // 途中ノードがツリーノードでないならば、SubTableを追加
        //                    bOverwritten = bOverwritten || (nd != null && !nd.IsFunctionNode());    // 置換先が機能ノード以外なら上書き(重複)警告を出す
        //                    nd.AddSubTable();
        //                }
        //                return nd;
        //            }
        //        }

        //        // 途中ノードのトラバース
        //        for (int i = 0; i < endCount; ++i) {
        //            pn = convertToTreeNode(getStroke(i));
        //        }

        //        if (pn == null) {
        //            logger.Warn($"No such parent node: strokeList={strokeList.DebugString()}");
        //        } else {
        //            int idx = getStroke(endCount);
        //            if (node == null) {
        //                // ツリーノードの挿入または変換
        //                convertToTreeNode(idx);
        //            } else {
        //                var oldNode = pn.GetNthSubNode(idx);
        //                pn.SetNthSubNode(idx, node);
        //                if (!bOverwritten) {
        //                    // 既存が空でないツリーノードか、既存が FunctionNode でなく新規がRewriteNodeでない、なら上書き(重複)警告
        //                    bOverwritten = oldNode != null &&
        //                        ((oldNode.IsTreeNode() && oldNode.HasSubNode()) || ((!oldNode.IsFunctionNode() || oldNode.IsRewriteNode()) && !node.IsRewriteNode()));
        //                }
        //            }
        //        }
        //    }
        //    if (bOverwritten && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
        //        logger.Warn($"DUPLICATED: strokeList={strokeList.DebugString()}, isCombo={isInCombinationBlock}, line={CurrentLine}");
        //        NodeDuplicateWarning();
        //    }

        //    return node;
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
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={StrokeList.DebugString()}");

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

                    //AddLeafNode(currentToken, idx);
                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.BARE_STRING:        // str : 文字列ノード
                        AddStringNode(idx, currentToken == TOKEN.BARE_STRING);
                        break;

                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        AddFunctionNode(idx);
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

        protected virtual TableParser AddTreeNode(int idx)
        {
            var strkList = StrokeList.WithLastStrokeAdded(idx);
            //Node node = SetNodeOrNewTreeNodeAtLast(strkList, null);
            Node node = SetOrMergeNthSubNode(idx, Node.MakeTreeNode());
            if (node != null && node.IsTreeNode()) {
                return new TableParser(RootNode, node, strkList);
            } else {
                return null;
            }
        }

        public virtual ArrowParser MakeArrowParser(int arrowIdx)
        {
            return new ArrowParser(RootNode, TreeNode, StrokeList, arrowIdx);
        }

        protected ArrowBundleParser MakeArrowBundleParser(int nextArrowIdx)
        {
            return new ArrowBundleParser(RootNode, StrokeList, -1, nextArrowIdx);
        }

        // 前置書き換えパーザ
        protected PreRewriteParser MakePreRewriteParser(int idx)
        {
            logger.DebugH(() => $"ENTER");

            var targetStr = RewritePreTargetStr;

            // 前置文字列の指定がなければ単打テーブルから文字を拾ってくる
            if (targetStr._isEmpty()) {
                targetStr = idx >= 0 ? GetNthRootNodeString(idx) : "";
            }

            // 前置書き換えノード処理用のパーザを作成
            var parser = new PreRewriteParser(RootNode, TreeNode, StrokeList, targetStr);

            logger.DebugH(() => $"LEAVE");
            return parser;
        }

        // 後置書き換えパーザ
        protected PostRewriteParser MakePostRewriteParser(int idx)
        {
            // 後置書き換えノード処理用のパーザを作成
            return new PostRewriteParser(RootNode, TreeNode, StrokeList, idx);
        }

        public virtual void AddStringNode(int idx, bool bBare)
        {
            logger.DebugH(() => $"ENTER: depth={Depth}, bBare={bBare}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            addTerminalNode(idx, Node.MakeStringNode($"{ConvertKanji(CurrentStr)}", bBare));
            if (Depth == 1 && CurrentStr._startsWith("!{")) {
                // Repeatable Key
                logger.DebugH(() => $"REPEATABLE");
                keyComboPool?.AddRepeatableKey(StrokeList.At(0));
            }
            logger.DebugH(() => $"LEAVE: depth={Depth}");
        }

        protected virtual void AddStringPairNode()
        {
            ParseError($"unexpected token: {currentToken}");
        }

        public void AddFunctionNode(int idx)
        {
            logger.DebugH(() => $"ENTER: depth={Depth}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            addTerminalNode(idx, Node.MakeFunctionNode(CurrentStr));
            logger.DebugH(() => $"LEAVE: depth={Depth}");
        }

        /// <summary>
        /// 終端ノードの追加と同時打鍵列の組合せの登録<br/>
        /// 同時打鍵の場合は、ブロックのルートキーをCOMBO_DECKEY_STARTまでシフトする
        /// </summary>
        /// <param name="prevNth"></param>
        /// <param name="lastNth"></param>
        void addTerminalNode(int idx, Node node)
        {
            addCombinationKey(true);

            //SetNodeOrNewTreeNodeAtLast(StrokeList.WithLastStrokeAdded(idx), node);
            SetOrMergeNthSubNode(idx, node);
        }

        void addCombinationKey(bool hasStr)
        {
            var list = new List<int>(StrokeList.strokeList);

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
                    AddCombinationKeyCombo(list, shiftOffset, hasStr, bComboEffectiveAlways);
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

        // 同時打鍵列の組合せを作成して登録しておく
        protected void AddCombinationKeyCombo(List<int> deckeyList, int shiftOffset, bool hasStr, bool effectiveAlways)
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

    }

    /// <summary>
    /// 矢印記法(-nn>)の解析
    /// </summary>
    class ArrowParser : TableParser
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>未処理のIndex</summary>
        protected int UnhandledArrowIndex;

        /// <summary>
        /// コンストラクタ<br/>
        /// パーザ構築時点では、まだRewriteNodeは生成されない
        /// </summary>
        public ArrowParser(Node rootNode, Node treeNode, CStrokeList strkList, int arrowIdx)
            : base(rootNode, treeNode, strkList)
        {
            UnhandledArrowIndex = arrowIdx;
        }

        // 矢印記法(-\d+(,\d+)*>)の直後を解析して第1打鍵位置に従って配置する
        public void Parse()
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, depth={Depth}, UnhandledArrowIndex={UnhandledArrowIndex}");
            //int arrowIdx = arrowIndex;
            readNextToken(true);
            switch (currentToken) {
                case TOKEN.ARROW:
                    // 矢印記法の連続
                    AddArrowNode(ArrowIndex).Parse();
                    break;

                case TOKEN.REWRITE_PRE:
                case TOKEN.REWRITE_POST:
                    ParseError($"トップレベル以外では前置または後置書き換え記法を使用できません。");
                    break;

                case TOKEN.COMMA:
                case TOKEN.VBAR:
                    if (parseArrow()) {
                        // 矢印記法連続の簡略記法
                        AddArrowNode(ArrowIndex).Parse();
                    }
                    break;

                case TOKEN.LBRACE:
                    // ブロック開始
                    AddTreeNode(UnhandledArrowIndex)?.ParseNodeBlock();
                    break;

                //AddLeafNode(currentToken, UnhandledArrowIndex);
                case TOKEN.STRING:             // "str" : 文字列ノード
                case TOKEN.BARE_STRING:        // str : 文字列ノード
                    AddStringNode(UnhandledArrowIndex, currentToken == TOKEN.BARE_STRING);
                    break;

                case TOKEN.FUNCTION:           // @c : 機能ノード
                    AddFunctionNode(UnhandledArrowIndex);
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
            var strkList = StrokeList.WithLastStrokeAdded(UnhandledArrowIndex);
            //Node node = SetNodeOrNewTreeNodeAtLast(strkList, null);
            Node node = SetOrMergeNthSubNode(UnhandledArrowIndex, Node.MakeTreeNode());
            return new ArrowParser(RootNode, node, strkList, arrowIdx);
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
        public ArrowBundleParser(Node rootNode, CStrokeList strkList, int lastStk, int nextArrowIdx)
            : base(rootNode, rootNode, strkList, lastStk)
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
                        addLeafNodeUnderNewTreeNode(currentToken, n, nextArrowIndex);
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

        void addLeafNodeUnderNewTreeNode(TOKEN token, int firstIdx, int secondIdx)
        {
            TableParser parser = AddTreeNode(firstIdx);
            if (parser != null) {
                switch (token) {
                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.BARE_STRING:        // str : 文字列ノード
                        parser.AddStringNode(secondIdx, token == TOKEN.BARE_STRING);
                        break;
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        parser.AddFunctionNode(secondIdx);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 書き換え解析の共通クラス
    /// </summary>
    abstract class RewriteParser : ArrowParser
    {
        private static Logger logger = Logger.GetLogger();

        protected string targetStr;

        protected bool bNested = false;

        public RewriteParser(Node rootNode, Node treeNode, CStrokeList strkList, int arrowIdx)
            : base(rootNode, treeNode, strkList, arrowIdx)
        {
        }

        protected string concatString(int idx, string oldStr)
        {
            if (idx < 0) return oldStr;

            var myStr = GetNthRootNodeString(idx);
            if (myStr._isEmpty()) return oldStr;

            // oldStr に後続文字を連結する
            return oldStr._isEmpty() ? myStr : Helper.ConcatDqString(oldStr, myStr);
        }

        protected string myTargetString(int idx)
        {
            return concatString(idx, targetStr);
        }

        /// <summary>node を idx 位置に merge する。merge に成功したら merge先のノードを返す</summary>
        protected Node mergeNode(int idx, Node node)
        {
            int tgtIdx = ShiftDecKey(idx);
            (Node mergedNode, bool bOverwrite) = RootNode.SetOrMergeNthSubNode(tgtIdx, node);
            if (bOverwrite && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return mergedNode;
        }

        /// <summary>書き換え情報を格納する node を idx 位置に upsert する。upsert に成功したら upsert先の RewriteNode を返す</summary>
        /// <param name="idx"></param>
        /// <param name="node"></param>
        protected Node upsertNode(int idx, Node node)
        {
            int rewriteIdx = ShiftDecKey(idx);
            var rewriteNode = GetNthRootNode(rewriteIdx);
            if (rewriteNode != null) {
                // TODO: node.IsTreeNode() だったらどうする?
                rewriteNode.AddRewriteMap();
            } else {
                rewriteNode = SetOrMergeNthSubNode(rewriteIdx, Node.MakeRewriteNode("", true));
            }
            if (rewriteNode.IsRewriteNode()) {
                // RewriteNode がノード木に反映された場合に限り、node を upsert する
                rewriteNode.UpsertRewritePair(targetStr, node);
                return rewriteNode;
            } else {
                logger.Warn("RewriteNode NOT merged");
                return null;
            }
        }
    }

    /// <summary>
    /// 前置書き換えブロックの解析<br/>
    /// ・書き換えブロックはネストできない<br/>
    /// ・前置書き換えは、'>'の直前のキーまたはブロック内の最上位レベルのキーに対して RewriteNode を作成する。
    ///   したがって、'%X>あ' のように、キーが1つで直後に書き換え文字列がくるような記述は不可
    /// </summary>
    class PreRewriteParser : RewriteParser
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>
        /// コンストラクタ<br/>
        /// ・パーザ構築時点では、まだRewriteNodeは生成されない<br/>
        /// ・UnhandledArrowIndex は、複数の矢印記法が並んだ場合の、直前の矢印記法によるキーIndexを表現する。
        ///   先頭ならすでに targetStr として処理されているので -1 にしておく。
        ///   これにより '%X>' の直後に AddStringNode が呼ばれたら、idx=-1 になるので、先頭であることを検知できるようになる
        /// </summary>
        public PreRewriteParser(Node rootNode, Node treeNode, CStrokeList strkList, string targetStr)
            : base(rootNode, treeNode, strkList, -1)
        {
            this.targetStr = targetStr;
        }

        // UnhandledArrowIndex の処理をした後、ネストされた新しいパーザを作成
        private PreRewriteParser makeNestedParser(int unhandledIdx = -1)
        {
            return new PreRewriteParser(RootNode, TreeNode, StrokeList, myTargetString(UnhandledArrowIndex)) {
                bNested = true,
                UnhandledArrowIndex = unhandledIdx
            };
        }

        // ブロック処理(ネストされたパーザから呼ばれた場合は、通常のTableParserにする)
        protected override TableParser AddTreeNode(int idx)
        {
            if (!bNested) {
                // 最初のブロック処理(パーザをネストさせる)
                // %X,,Y>{
                // のケース
                return makeNestedParser();
            } else {
                // ネストされたパーザから呼ばれた場合は、通常のTableParserにする
                // %X,,Y>{
                //   -Z>{
                // のケース
                var strkList = StrokeList.WithLastStrokeAdded(idx);
                Node treeNode = Node.MakeTreeNode();
                if (upsertNode(idx, treeNode) != null) {
                    // 通常のTableParserを返す
                    return new TableParser(RootNode, treeNode, strkList);
                }
            }
            return null;
        }

        // ブロック内の相対深さ0の矢印記法
        // %X,,Y>{
        //   -Z>
        // というケース。この後、文字列が来るか、後続矢印が来るか、{ が来るかはまだ分からないので、
        // UnhandledArrowIndex の処理をした後、新しいパーザを作成してどれにも対応できるようにする
        public override ArrowParser MakeArrowParser(int arrowIdx)
        {
            return makeNestedParser(arrowIdx);
        }

        // 前置書き換え用の後続矢印記法 (「%X,Y,Z>」における YやZ)
        // ブロック内での後続矢印記法({ -Z,..,W>)の処理も含む
        protected override ArrowParser AddArrowNode(int arrowIdx)
        {
            targetStr = myTargetString(UnhandledArrowIndex);
            UnhandledArrowIndex = arrowIdx;
            return this;
        }

        // ここが呼ばれたとき、 idx にはテーブル内での index または矢印記法による UnhandledArrowIndex が入っており、これが書き換えノードのindexとなる
        public override void AddStringNode(int idx, bool bBare)
        {
            logger.DebugH(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={idx}");
            if (idx < 0) {
                // %X>の直後に文字列がきたケース。
                ParseError("前置書き換え記法には、少なくとも1つの後続キーが必要です。");
            } else {
                // 書き換え情報を upsert する
                upsertNode(idx, Node.MakeRewriteNode(CurrentStr, bBare));
            }
            logger.DebugH("LEAVE");
        }

    }

    /// <summary>
    /// 後置書き換えブロックの解析<br/>
    /// ・書き換えブロックはネストできない
    /// ・後置書き換えは、'>'の直前のキーに対して RewriteNode を作成する。
    /// </summary>
    class PostRewriteParser : RewriteParser
    {
        private static Logger logger = Logger.GetLogger();

        string prefixStr = "";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public PostRewriteParser(Node rootNode, Node rewriteNode, CStrokeList strkList, int arrowIdx, string targetStr = null)
            : base(rootNode, rewriteNode, strkList, arrowIdx)
        {
            this.targetStr = targetStr;
        }

        private string myPrefixString(int idx)
        {
            return concatString(idx, prefixStr);
        }

        private OutputString getMyString(int idx)
        {
            var myStr = ReadWordOrString();
            if (myStr._isEmpty() && idx >= 0) {
                Node myNode = GetNthRootNode(idx);
                if (myNode != null) {
                    myStr = myNode.GetOutputString();
                    //bBare = myNode.IsBareString();
                }
            }
            return myStr;
        }

        // ブロック処理
        // &X,..,Y>{あ
        // というケース
        // ・後置書き換えではネストされたブロックは不可
        protected override TableParser AddTreeNode(int idx)
        {
            if (bNested) {
                ParseError("後置書き換えではブロックのネストはできません");
                return null;
            }
            // 後置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
            var myStr = getMyString(idx);
            var strkList = StrokeList.WithLastStrokeAdded(idx);
            Node node = mergeNode(idx, Node.MakeRewriteTreeNode(myStr));
            if (node != null) {
                return new PostRewriteParser(RootNode, node, strkList, -1, targetStr) { bNested = true };
            } else {
                return null;
            }
        }

        // ブロック内の相対深さ0の矢印記法
        // &X,,Y>{
        //   -Z>
        // というケース。この後は、文字列か後続矢印のみ可
        public override ArrowParser MakeArrowParser(int arrowIdx)
        {
            prefixStr = "";
            UnhandledArrowIndex = arrowIdx;
            return this;
        }

        // 後置書き換え用の後続矢印記法
        protected override ArrowParser AddArrowNode(int arrowIdx)
        {
            if (bNested) {
                prefixStr = myPrefixString(UnhandledArrowIndex);
            } else {
                targetStr = myTargetString(UnhandledArrowIndex);
            }
            UnhandledArrowIndex = arrowIdx;
            return this;
        }

        public override void AddStringNode(int idx, bool bBare)
        {
            logger.DebugH(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={idx}");
            if (!TreeNode.IsRewriteNode()) {
                // &X,..>の直後に文字列がきた
                ParseError("後置書き換え記法には、ブロックが必要です。");
            } else {
                var tgtStr = prefixStr + GetNthRootNodeString(idx)._toSafe() + targetStr._toSafe();
                TreeNode.UpsertRewritePair(tgtStr, Node.MakeRewriteNode(CurrentStr, bBare));
            }
            logger.DebugH("LEAVE");
        }

        // 書き換え文字列のペア
        protected override void AddStringPairNode()
        {
            var str1 = StringPair._getNth(0);
            var str2 = StringPair._getNth(1);
            logger.DebugH(() => $"ENTER: str1={str1}, str2={str2}");
            if (str1._isEmpty() || str2._isEmpty()) {
                ParseError("不正な書き換え文字列ペア");
            } else {
                var tgtStr = str1 + targetStr._toSafe();
                TreeNode.UpsertRewritePair(tgtStr, Node.MakeRewriteNode(str2, false));
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
        public override bool IsRootParser => true;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public RootTableParser()
            : base(ParserContext.Singleton.rootTableNode, ParserContext.Singleton.rootTableNode, null)
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
                        AddCombinationKeyCombo(Helper.MakeList(dk), 0, true, false);  // 単打指定
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
        public void ParseTableFile(string filename, string outFilename, KeyCombinationPool pool, bool primary, bool bTest = false)
        {
            logger.InfoH($"ENTER: filename={filename}");
            tableLines.ReadAllLines(filename);

            if (tableLines.NotEmpty) {
                ParserContext.CreateSingleton(tableLines, pool, primary);
                var parser = new RootTableParser();
                parser.ParseRootTable();
                writeAllLines(outFilename, ParserContext.Singleton.OutputLines);
                writeAllLines($"tmp/parsedTableFile{(primary ? 1 : 2)}.txt", ParserContext.Singleton.tableLines.GetLines());
            } else {
                tableLines.Error($"テーブルファイル({filename})が開けません");
            }

            if (!bTest) tableLines.showErrorMessage();

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
