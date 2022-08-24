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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableParser(ParserContext ctx, List<int> stkList, StrokeTableNode rootNode, int shiftPlane = -1)
            : base(ctx, stkList, rootNode, shiftPlane)
        {
        }

        /// <summary>もしルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる</summary>
        protected void addMyCharFunctionInRootStrokeTable()
        {
            for (int idx = 0; idx < DecoderKeys.NORMAL_DECKEY_NUM; ++idx) {
                if (rootTableNode.getNth(idx) == null) {
                    OutputLines.Add($"-{idx}>@^");
                }
            }
        }

        /// <summary>拡張修飾キーが同時打鍵キーとして使われた場合は、そのキーの単打設定として本来のキー出力を追加する</summary>
        protected void addExtModfierAsSingleHitKey()
        {
            void addExtModAsSingleKey(string keyName)
            {
                int dk = VirtualKeys.GetFuncDeckeyByName(keyName);
                if (dk >= 0) {
                    if (rootTableNode.getNth(dk) == null && rootTableNode.getNth(dk + DecoderKeys.COMBO_DECKEY_START) != null) {
                        // 単打設定が存在せず、同時打鍵の先頭キーになっている場合は、単打設定を追加する
                        makeCombinationKeyCombo(Helper.MakeList(dk), 0, true, false);  // 単打指定
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

        /// <summary>
        /// n番目の子ノードをセットする(残ったほうのノードを返す)
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="n"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        protected Node setNthChildNode(StrokeTableNode tbl, int n, Node node)
        {
            if (tbl.setNthChild(n, node) && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return tbl.getNth(n);
        }

        /// <summary>
        /// stkList の末尾 strokeに対応するノードを追加する。node==null なら新しく TreeNode を作成する<br/>
        /// 途中、StrokeTableNodeが存在しない場所があれば、そこにStrokeTableNodeを生成して挿入(または置換)する
        /// </summary>
        /// <param name="stkList"></param>
        /// <param name="node"></param>
        void setNodeOrNewTreeNodeAtLast(List<int> stkList, Node node)
        {
            logger.DebugH(() => $"CALLED: stkList={stkList._keyString()}, isCombo={isInCombinationBlock}, {node?.DebugString() ?? "node=null"}");
            bool bOverwritten = false;
            if (stkList._isEmpty()) {
                logger.Warn($"strokeList is empty");
            } else {
                int getStroke(int idx)
                {
                    // assert(idx < stkList.Count)
                    int stk = stkList[idx];
                    if (isInCombinationBlock) {
                        // 同時打鍵定義ブロック
                        if (idx == 0) {
                            // 同時打鍵の先頭キーは Combo化(単打ノードの重複を避ける)
                            stk = makeComboDecKey(stk);
                        } else if (idx > 0 && (node == null || node.isStrokeTree() || idx + 1 < stkList.Count)) {
                            // 同時打鍵の中間キー(非終端キー)は、Shift化(終端ノードとの重複を避ける)
                            stk = makeNonTerminalDuplicatableComboKey(stk);
                        }
                    }
                    return stk;
                }
                var pn = rootTableNode;
                for (int i = 0; i < stkList.Count - 1; ++i) {
                    int idx = getStroke(i);
                    var nd = pn.getNth(idx);
                    if (nd != null && nd.isStrokeTree()) {
                        pn = (StrokeTableNode)nd;
                    } else {
                        // StrokeTableNodeを生成して挿入(または置換)する
                        bOverwritten = bOverwritten || nd != null && !nd.isFunctionNode();
                        var _pn = new StrokeTableNode();
                        pn.setNthChild(idx, _pn);
                        pn = _pn;
                    }
                }
                if (pn == null) {
                    logger.Warn($"No such parent node: strokeList={stkList._keyString()}");
                } else {
                    int idx = getStroke(stkList.Count - 1);
                    if (node == null) {
                        if (pn.getNth(idx) == null) {
                            // 新しく StrokeTableNode を作成して追加
                            pn.setNthChild(idx, new StrokeTableNode());
                        } else if (!pn.getNth(idx).isStrokeTree()) {
                            bOverwritten = true;
                        }
                    } else {
                        if (!bOverwritten) {
                            var _nd = pn.getNth(idx);
                            if (_nd != null) {
                                // 既存が FunctionNode でないか RewriteNode であり、新規がRewriteNodeでない
                                bOverwritten = (!_nd.isFunctionNode() || _nd is RewriteNode) && !(node is RewriteNode);
                            }
                        }
                        pn.setNthChild(idx, node);
                    }
                }
            }
            if (bOverwritten && (Settings.DuplicateWarningEnabled || isInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: strokeList={stkList._keyString()}, isCombo={isInCombinationBlock}, line={CurrentLine}");
                NodeDuplicateWarning();
            }
        }

        class StrokeListUndoer : IDisposable
        {
            List<int> stkList = null;

            public StrokeListUndoer(List<int> list, int stroke)
            {
                if (stroke >= 0) {
                    stkList = list;
                    stkList.Add(stroke);
                }
            }

            public void Dispose()
            {
                stkList._popBack();
            }
        }

        StrokeListUndoer pushStroke(int stroke)
        {
            if (stroke >= 0) {
                // 先頭キーはシフト化
                if (strokeList._isEmpty()) {
                    stroke = shiftDecKey(stroke);
                } else {
                    // それ以外は Modulo
                    stroke %= DecoderKeys.PLANE_DECKEY_NUM;
                }
            }
            return new StrokeListUndoer(strokeList, stroke);
        }

        protected void addNodeTree(int stroke = -1)
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={strokeList._keyString()}, stroke={stroke}");
            using (pushStroke(stroke)) {
                // StrokeTableNodeを追加しておく(そうでないと矢印記法によって先に空のStrokeTableNodeが作られてしまう可能性があるため)
                setNodeOrNewTreeNodeAtLast(strokeList, null);
                new TableParser(context, strokeList, rootTableNode).MakeNodeTree();
                logger.DebugH(() => $"LEAVE");
            }
        }

        static class VBarSeparationHelper
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
        /// テーブル構造を解析してストロークノード木を構築する。
        /// </summary>
        public void MakeNodeTree()
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={strokeList._keyString()}");

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
                        addNodeTree(idx);
                        break;

                    case TOKEN.ARROW:
                        pos = arrowIndex;
                        addArrowNode(currentToken, arrowIndex);
                        //isPrevDelim = false;
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        parseArrowBundleNode(arrowIndex);
                        break;

                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.BARE_STRING:        // str : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        addLeafNode(currentToken, idx);
                        break;

                    case TOKEN.STRING_PAIR:
                        addStringPairNode();
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

            if (depth == 0) placeHolders.Initialize();
            logger.DebugH(() => $"LEAVE: lineNum={LineNumber}, depth={depth}, bError={bError}");

        }

        protected virtual void addNodeTreeByArrow()
        {
            addNodeTree();
        }

        // 矢印記法(-\d+(,\d+)*>)を解析して第1打鍵位置に従って配置する
        protected void addArrowNode(TOKEN prevToken, int idx)
        {
            using (pushStroke(idx)) {   // ここで idx は保存される
                logger.DebugH(() => $"ENTER: lineNum={LineNumber}, depth={depth}, idx={idx}");
                //int arrowIdx = arrowIndex;
                readNextToken(true);
                switch (currentToken) {
                    case TOKEN.ARROW:
                        if (prevToken == TOKEN.REWRITE_PRE) {
                            addPreRewriteNode(RewritePreTargetStr).addArrowNode(TOKEN.ARROW, arrowIndex);
                        } else {
                            addArrowNode(prevToken, arrowIndex);
                        }
                        break;

                    case TOKEN.COMMA:
                    case TOKEN.VBAR:
                        if (parseArrow()) {
                            if (prevToken == TOKEN.REWRITE_PRE) {
                                addPreRewriteNode(RewritePreTargetStr).addArrowNode(TOKEN.ARROW, arrowIndex);
                            } else {
                                addArrowNode(prevToken, arrowIndex);
                            }
                        }
                        break;

                    case TOKEN.LBRACE:
                        switch (prevToken) {
                            case TOKEN.ARROW:
                                // -X>{ } 形式
                                addNodeTreeByArrow();
                                break;
                            case TOKEN.REWRITE_PRE:
                                // %X>{ } 形式
                                addPreRewriteNode(RewritePreTargetStr).MakeNodeTree();
                                break;
                            case TOKEN.REWRITE_POST:
                                // &X>{ } 形式
                                addPostRewriteNode();
                                break;
                        }
                        break;

                    case TOKEN.STRING:
                    case TOKEN.BARE_STRING:
                        if (prevToken == TOKEN.REWRITE_PRE) {
                            addPreRewriteNode(RewritePreTargetStr).addLeafNode(currentToken, -1);
                        } else if (prevToken != TOKEN.ARROW) {
                            ParseError($"前置または後置書き換えの場合は、ブロック記述が必要です。");
                        }
                        addLeafNode(currentToken, -1);  // idx は既にpush済み
                        break;

                    //case TOKEN.STRING_PAIR:
                    //    addStringPairNode();
                    //    break;

                    default:
                        ParseError("PreRewriteParser-addArrowNode");
                        break;
                }

                currentToken = TOKEN.IGNORE;    // いったん末端ノードの処理をしたら、矢印記法を抜けるまで無視
                logger.DebugH(() => $"LEAVE: lineNum={LineNumber}, depth={depth}");
            }
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        protected Node parseArrowBundleNode(int nextArrowIdx)
        {
            logger.DebugH(() => $"ENTER: depth={depth}, nextArrowIdx={nextArrowIdx}");

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
                        addLeafNode(currentToken, nextArrowIdx, n);
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

            if (depth == 0) placeHolders.Initialize();
            logger.DebugH(() => $"LEAVE: depth={depth}");

            return myNode;
        }

        void addLeafNode(TOKEN token, int idx, int prevIdx = -1)
        {
            using (pushStroke(prevIdx)) {
                using (pushStroke(idx)) {
                    switch (token) {
                        case TOKEN.STRING:             // "str" : 文字列ノード
                            addStringNode(false);
                            break;
                        case TOKEN.BARE_STRING:        // str : 文字列ノード
                            addStringNode(true);
                            break;
                        case TOKEN.FUNCTION:           // @c : 機能ノード
                            addFunctionNode();
                            break;
                    }
                }
            }
        }

        protected virtual void addStringNode(bool bBare)
        {
            logger.DebugH(() => $"ENTER: depth={depth}, bBare={bBare}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            addTerminalNode(bBare, new StringNode($"{CurrentStr._safeReplace(@"\", @"\\")._safeReplace(@"""", @"\""")}", bBare));
            if (depth == 1 && CurrentStr._startsWith("!{")) {
                // Repeatable Key
                logger.DebugH(() => $"REPEATABLE");
                keyComboPool?.AddRepeatableKey(strokeList[0]);
            }
            logger.DebugH(() => $"LEAVE: depth={depth}");
        }

        protected virtual void addStringPairNode()
        {
            ParseError($"unexpected token: {currentToken}");
        }

        void addFunctionNode()
        {
            logger.DebugH(() => $"ENTER: depth={depth}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            addTerminalNode(false, new FunctionNode(CurrentStr));
            logger.DebugH(() => $"LEAVE: depth={depth}");
        }

        /// <summary>
        /// 終端ノードの追加と同時打鍵列の組合せの登録<br/>
        /// 同時打鍵の場合は、ブロックのルートキーをCOMBO_DECKEY_STARTまでシフトする
        /// </summary>
        /// <param name="prevNth"></param>
        /// <param name="lastNth"></param>
        void addTerminalNode(bool bBare, Node node)
        {
            addCombinationKey(true);

            setNodeOrNewTreeNodeAtLast(strokeList, node);
        }

        void addCombinationKey(bool hasStr)
        {
            var list = new List<int>(strokeList);

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
                    makeCombinationKeyCombo(list, shiftOffset, hasStr, bComboEffectiveAlways);
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
        PreRewriteParser addPreRewriteNode(string targetStr)
        {
            logger.DebugH(() => $"ENTER");

            if (strokeList.Count > (targetStr._notEmpty() ? 0 : 1)) {
                ParseError($"深さが1以上の前置書き換えはサポートされていません。");
            }

            // 前置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
            if (targetStr._isEmpty()) {
                if (strokeList.Count > 0) {
                    int preIdx = strokeList[0];
                    targetStr = preIdx >= 0 ? getNthRootNodeString(preIdx) : "";
                    keyComboPool?.AddPreRewriteKey(preIdx);
                }
            }

            // 前置書き換えノード処理用のパーザを作成
            var parser = new PreRewriteParser(rootTableNode, context, targetStr);

            logger.DebugH(() => $"LEAVE");
            return parser;
        }

        // 後置書き換えノード
        void addPostRewriteNode()
        {
            logger.DebugH("ENTER");

            Node node = null;
            if (isInCombinationBlock) {
                // 同時打鍵の場合
                addCombinationKey(true);
                var myStr = ReadWordOrString();
                node = new RewriteNode(myStr);
                setNodeOrNewTreeNodeAtLast(strokeList, node);
            } else {
                // 後置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
                int lastIdx = strokeList._getLast();
                var myStr = ReadWordOrString()._orElse(() => lastIdx >= 0 ? getNthRootNodeString(lastIdx) : "");
                node = setNthChildNode(rootTableNode, lastIdx, new RewriteNode(myStr));
            }
            if (node is RewriteNode) {
                // RewriteNode がノード木に反映された場合に限り、後置書き換えノードの処理を行う
                new PostRewriteParser(rootTableNode, (RewriteNode)node, context, leaderStr).MakeNodeTree();
            } else {
                logger.Warn("RewriteNode NOT merged");
            }
            logger.DebugH("LEAVE");
        }

        int calcShiftOffset(int deckey)
        {
            return (deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey / DecoderKeys.PLANE_DECKEY_NUM : shiftPlane) * DecoderKeys.PLANE_DECKEY_NUM;
        }

        // 同時打鍵列の組合せを作成して登録しておく
        void makeCombinationKeyCombo(List<int> deckeyList, int shiftOffset, bool hasStr, bool effectiveAlways)
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
    }

    /// <summary>
    /// 前置書き換えブロックの解析
    /// </summary>
    class PreRewriteParser : TableParser
    {
        private static Logger logger = Logger.GetLogger();

        string targetStr;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PreRewriteParser(StrokeTableNode rootTblNode, ParserContext ctx, string targetStr)
            : base(ctx, null, rootTblNode)
        {
            this.targetStr = targetStr;
        }

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        protected override void addNodeTreeByArrow()
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={strokeList._keyString()}");
            // 新しいテーブルを作成し、それをネストされたルートテーブルとする
            var nestedRootTblNode = new StrokeTableNode();
            if (addTreeOrStringNode("", nestedRootTblNode)) {
                new TableParser(context, null, nestedRootTblNode, 0).MakeNodeTree();
            }
            logger.DebugH(() => $"LEAVE");
        }

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        protected override void addStringNode(bool bBare)
        {
            logger.DebugH(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={strokeList._getLast()}");
            addTreeOrStringNode(CurrentStr._quoteString(bBare), null);
            logger.DebugH("LEAVE");
        }

        bool addTreeOrStringNode(string outStr, StrokeTableNode tblNode)
        {
            int lastIdx = shiftDecKey(strokeList._getLast());
            string myStr = getNthRootNodeString(lastIdx);
            var node = setNthChildNode(rootTableNode, lastIdx, new RewriteNode(myStr));
            if (node is RewriteNode) {
                // RewriteNode がノード木に反映された場合に限り、以下を実行
                var tgtStr = Helper.ConcatDqString(targetStr, leaderStr);
                ((RewriteNode)node).AddRewritePair(tgtStr, outStr, tblNode);
                return true;
            } else {
                logger.Warn("RewriteNode NOT merged");
                return false;
            }
        }

    }

    /// <summary>
    /// 後置書き換えブロックの解析
    /// </summary>
    class PostRewriteParser : TableParser
    {
        private static Logger logger = Logger.GetLogger();

        RewriteNode node = null;

        new string leaderStr;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public PostRewriteParser(StrokeTableNode rootTblNode, RewriteNode node, ParserContext ctx, string leaderStr)
            : base(ctx, null, rootTblNode)
        {
            this.node = node;
            this.leaderStr = leaderStr;
        }

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        protected override void addNodeTreeByArrow()
        {
            addNodeTree();
        }

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        protected override void addStringNode(bool bBare)
        {
            logger.DebugH(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={strokeList._getLast()}");
            var tgtStr = Helper.ConcatDqString(pathStr, leaderStr);
            var outStr = CurrentStr._quoteString(bBare);
            node.AddRewritePair(tgtStr, outStr, null);
            logger.DebugH("LEAVE");
        }

        // 書き換え文字列のペア
        protected override void addStringPairNode()
        {
            var str1 = StringPair._getNth(0);
            var str2 = StringPair._getNth(1);
            logger.DebugH(() => $"ENTER: str1={str1}, str2={str2}");
            if (str1._isEmpty() || str2._isEmpty()) {
                ParseError("Invalid String Piar");
            } else {
                node.AddRewritePair(str1, str2, null);
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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public RootTableParser(ParserContext ctx)
            : base(ctx, null, ctx.rootTableNode)
        {
        }

        /// <summary>
        /// トップレベルのテーブル定義を解析してストローク木を構築する。
        /// </summary>
        public void ParseRootTable()
        {
            logger.InfoH($"ENTER");

            // トップレベルの解析
            readNextToken(true);
            while (context.currentToken != TOKEN.END) {
                switch (context.currentToken) {
                    case TOKEN.LBRACE:
                        MakeNodeTree();
                        break;

                    case TOKEN.ARROW:
                    case TOKEN.REWRITE_PRE:
                    case TOKEN.REWRITE_POST:
                        addArrowNode(currentToken, arrowIndex);
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        parseArrowBundleNode(context.arrowIndex);
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:
                        context.tableLines.ParseError();
                        break;
                }
                readNextToken(true);
            }

            // 拡張修飾キーが同時打鍵キーとして使われた場合は、そのキーの単打設定として本来のキー出力を追加する
            addExtModfierAsSingleHitKey();

            // 部分キーに対して、非終端マークをセット
            context.keyComboPool?.SetNonTerminalMarkForSubkeys();
            if (Logger.IsInfoHEnabled && logger.IsInfoHPromoted) {
                context.keyComboPool?.DebugPrint();
            }

            if (context.bRewriteEnabled) {
                // 書き換えノードがあったら、SandSの疑似同時打鍵サポートをOFFにしておく
                Settings.SandSEnablePostShift = false;
            }

            // ここまでで未出力なノードを OutputLines に書き出す
            outputNewLines();

            // ルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる
            addMyCharFunctionInRootStrokeTable();

            logger.InfoH($"LEAVE: KeyCombinationPool.Count={context.keyComboPool?.Count}");
        }

        public void ParseDirectives()
        {
            readNextToken();
            while (currentToken != TOKEN.END) {
                SkipToEndOfLine();
                readNextToken();
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
        public void ParseTableFile(string filename, string outFilename, KeyCombinationPool pool, bool primary, bool bWriteExpandedTableLines = false)
        {
            logger.InfoH($"ENTER: filename={filename}");
            tableLines.ReadAllLines(filename);

            if (tableLines.NotEmpty) {
                var context = new ParserContext(tableLines, pool, primary);
                var parser = new RootTableParser(context);
                parser.ParseRootTable();
                writeAllLines(outFilename, context.OutputLines);
                if (bWriteExpandedTableLines) writeAllLines($"tmp/parsedTableFile{(primary ? 1 : 2)}.txt", context.tableLines.GetLines());
            } else {
                tableLines.Error($"テーブルファイル({filename})が開けません");
            }

            tableLines.showErrorMessage();

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
                var context = new ParserContext(tableLines, null, primary);
                var parser = new RootTableParser(context);
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
