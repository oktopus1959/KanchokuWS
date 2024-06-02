using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using KanchokuWS.Domain;
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

        private List<bool> comboFlagList { get; set; } = new List<bool>();

        public bool ComboFlagAt(int idx) => idx >= 0 && idx < comboFlagList.Count ? comboFlagList[idx] : false;

        public bool ComboFlagAtLast() => comboFlagList.Count > 0 ? comboFlagList[comboFlagList.Count - 1] : false;

        //// 追加の末尾打鍵
        //public int LastStroke { get; set; } = -1;

        public int Count => strokeList.Count;

        public int CountComboKey()
        {
            int count = 0;
            while (ComboFlagAt(count)) {
                ++count;
            }
            return count;
        }

        public bool IsEmpty => Count <= 0;

        public int At(int idx) { return strokeList._getNth(idx); }

        public int Last() { return strokeList._getLast(); }


        public CStrokeList(TableParser parser, CStrokeList strkList)
        {
            this.parser = parser;
            if (strkList != null && strkList.strokeList._notEmpty()) {
                strokeList.AddRange(strkList.strokeList);
                comboFlagList.AddRange(strkList.comboFlagList);
            }
        }

        public CStrokeList WithLastStrokeAdded(int lastStroke)
        {
            var result = new CStrokeList(parser, this);
            if (lastStroke >= 0) {
                // 先頭キーはシフト化
                if (parser.HasRootTable) {
                    lastStroke = parser.ShiftDecKey(lastStroke);
                } else {
                    // それ以外は Modulo
                    lastStroke %= DecoderKeys.PLANE_DECKEY_NUM;
                }
                result.strokeList.Add(lastStroke);
                result.comboFlagList.Add(parser.IsInCombinationBlock);
            }
            return result;
        }

        public string MakePathString(int dropTailLen = 0)
        {
            int len = strokeList.Count - dropTailLen;
            return len > 0 ? strokeList.Take(len).Select(x => parser.GetNthRootNodeString(x))._join("") : "";
        }

        public string StrokePathString(string delim, int skip = 0, int len = 100)
        {
            return strokeList.Skip(skip).Take(len).Select(x => x.ToString())._join(delim);
        }

    }

    /// <summary>
    /// テーブル解析器
    /// </summary>
    class TableParser : TableParserTokenizer
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>ルートノードテーブルを持つパーザか</summary>
        public override bool HasRootTable => Depth == 0;

        // ルートノードへのアクセッサ
        protected static Node RootTableNode => ParserContext.Singleton.rootTableNode;

        /// <summary>キー文字に到るストロークパスを得る辞書</summary>
        protected static Dictionary<string, CStrokeList> StrokePathDict => ParserContext.Singleton.strokePathDict;

        // ComboBlockerDepth
        // ComboBlockerDepth はブロックのネストが増えるたびに decrement される
        // これが 0 になったら、そこに同時打鍵無効化のブロッカーを設定する
        public const int DEFAULT_DEPTH = 9999;

        protected int ComboBlockerDepth => _comboBlockerDepth;

        private int _comboBlockerDepth = DEFAULT_DEPTH;

        private bool _isComboBlocked => _comboBlockerDepth <= 0;

        public bool IsInCombinationBlock => !_isComboBlocked && isInCombinationBlock;

        // 当Parserによる解析内容を格納するツリーノード
        Node _treeNode;

        protected Node TreeNode => _treeNode;

        // ルートノードから当ノードに至るまでの打鍵リスト
        CStrokeList _strokeList = null;

        // ルートパーザから当パーザに到る打鍵経路を表す
        // addCombinationKey() や SetOrMergeNthSubNode() で必要になる
        // 同時打鍵用にコードをシフトしておく必要はない(addCombinationKey()やSetOrMergeNthSubNode()でシフトされる)
        protected CStrokeList StrokeList => _strokeList;

        // ルートパーザから当パーザに到る打鍵経路の深さを表す(0ならルートパーザ)
        protected int Depth => StrokeList.Count;

        // 当ノードの ShiftPlane
        //int _shiftPlane = -1;
        //protected int ShiftPlane => _shiftPlane >= 0 ? _shiftPlane : Context.shiftPlane;

        protected int ShiftPlane => Context.shiftPlane;

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
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + comboDeckeyStart;
        }

        int makeShiftedDecKey(int decKey, int shiftOffset)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + shiftOffset;
        }

        int makeNonTerminalDuplicatableComboKey(int decKey)
        {
            //return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + DecoderKeys.PLANE_DECKEY_NUM;
            return KeyCombination.MakeNonTerminalDuplicatableComboKey(decKey);
        }

        public Node GetNthRootNode(int n)
        {
            return RootTableNode.GetNthSubNode(ShiftDecKey(n));
        }

        public string GetNthRootNodeString(int n)
        {
            return (GetNthRootNode(n)?.GetOutputString())?.GetBareString(n) ?? "";
        }

        public Node GetNthSubNode(int n)
        {
            return TreeNode?.GetNthSubNode(n);
        }

        public List<string> GetStrokeList(string word)
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"WORD: {word}");
            List<string> result = new List<string>();
            if (word._notEmpty()) {
                getStrokeListSub(0, word._reReplace(@"[。、ーぁ-龠]", @":$&:")._reReplace(@"::", @":")._reReplace(@"^:", @"")._reReplace(@":$", @"")._split(':'), new List<string>(), result);
            }
            return result;
        }

        private void getStrokeListSub(int n, string[] word, List<string> strokes, List<string> result)
        {
            if (n == 0) {
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"PRIORITY WORD({n}): {word._join(":")}");
            }

            if (strokes.Count >= 4) return; // 3キーを超えるものは無視

            if (n >= word.Length) {
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"PRIORITY WORD({n}): {word._join(":")}, keyString={strokes._join(":")}");
                result.Add(strokes.Select(x => x._safeSubstring(-2))._join(":"));   // 100以上なら99以下に丸める
                return;
            }

            string chStr = word[n].ToString();
            if (chStr._notEmpty()) {
                if (chStr[0] >= 0x100) {
                    int idx = RootTableNode.FindSubNode(chStr);
                    if (idx >= 0) {
                        getStrokeListSub(n + 1, word, makeList(strokes, idx), result);
                    } else {
                        for (int i = DecoderKeys.COMBO_DECKEY_START; i < DecoderKeys.COMBO_DECKEY_END; ++i) {
                            Node node = RootTableNode.GetNthSubNode(i);
                            if (node != null && node.IsTreeNode()) {
                                idx = node.FindSubNode(chStr);
                                if (idx >= 0) {
                                    // 同時打鍵の組み合わせの場合に限り
                                    getStrokeListSub(n + 1, word, makeList(strokes, i, idx), result);
                                    getStrokeListSub(n + 1, word, makeList(strokes, idx, i), result);
                                }
                            }
                        }
                    }
                } else {
                    getStrokeListSub(n + 1, word, Helper.MakeList(strokes, chStr), result);
                }
            }
        }

        private List<string> makeList(List<string> strokes, params int[] idxes)
        {
            List<string> result = new List<string>(strokes);
            foreach (var x in idxes) {
                var s = x.ToString();
                if (!strokes.Contains(s)) result.Add(s);
            }
            return result;
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
        public TableParser(Node treeNode, CStrokeList strkList, int comboBlockDepth)
            : base()
        {
            _treeNode = treeNode;
            _strokeList = new CStrokeList(this, strkList);
            _comboBlockerDepth = comboBlockDepth;
        }

        /// <summary>
        /// node を(同時打鍵を考慮しつつ)idx番目の子ノードにマージする(残ったほうのノードを返す)
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="idx"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        protected Node SetOrMergeNthSubNode(int idx, Node node, bool bComboBlocked = false)
        {
            if (IsInCombinationBlock) {
                // 同時打鍵定義ブロック
                if (HasRootTable) {
                    // 同時打鍵の先頭キーは Combo化(単打ノードの重複を避ける)
                    idx = makeComboDecKey(idx);
                } else if (node.IsTreeNode() && !bComboBlocked) {
                    // 同時打鍵の中間キー(非終端キー)は、Shift化して終端ノードとの重複を避ける
                    idx = makeNonTerminalDuplicatableComboKey(idx);
                }
            }
            (Node mergedNode, bool bOverwrite) = TreeNode.SetOrMergeNthSubNode(idx, node);
            if (bOverwrite && (Settings.DuplicateWarningEnabled || IsInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.WarnH($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return mergedNode;
        }

        protected static class VBarSeparationHelper
        {
            public static int calcRow(int idx, int currentRow)
            {
                idx %= DecoderKeys.PLANE_DECKEY_NUM;
                if (idx <= 40) return idx / 10;
                return currentRow;
            }

            public static int calcOverrunIndex(int idx)
            {
                int factor = idx / DecoderKeys.PLANE_DECKEY_NUM;
                idx %= DecoderKeys.PLANE_DECKEY_NUM;
                if (idx == 10) return factor * DecoderKeys.PLANE_DECKEY_NUM + 41;
                if (idx == 20) return factor * DecoderKeys.PLANE_DECKEY_NUM + 44;
                if (idx == 30) return factor * DecoderKeys.PLANE_DECKEY_NUM + 46;
                if (idx == 40) return factor * DecoderKeys.PLANE_DECKEY_NUM + 48;
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
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: lineNum={LineNumber}, strokeList={StrokeList.StrokePathString(":")}");

            bool bError = false;
            int idx = 0;
            int row = 0;
            //bool isPrevDelim = true;
            TOKEN prevToken = 0;
            TOKEN prevPrevToken = 0;
            readNextToken();
            while (!bError && currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                int pos = idx;
                if (HasRootTable) idx = ShiftDecKey(idx);   // ルートパーザの場合は、 idx を Shiftしておく
                switch (currentToken) {
                    case TOKEN.LBRACE:
                        if (HasRootTable) logger.InfoH($"LBRACE: shiftPlane={ShiftPlane}");
                        AddTreeNode(idx)?.ParseNodeBlock();
                        break;

                    case TOKEN.ARROW:
                        // ルートパーザの場合は、 ArrowIndex はすでに Shiftされている
                        pos = ArrowIndex;
                        MakeArrowParser(ArrowIndex).Parse();
                        //isPrevDelim = false;
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        // ルートパーザの場合は、 ArrowIndex はすでに Shiftされている
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
                        //    if (Settings.LoggingTableFileInfo) logger.Info(() => $"CALL addCombinationKey(false): prevToken={prevToken}, depth={depth}");
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

            if (HasRootTable) {
                logger.InfoH($"RBRACE: shiftPlane and placeHolders initialized");
                Context.shiftPlane = 0;
                placeHolders.Initialize();
            }
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE: lineNum={LineNumber}, depth={Depth}, bError={bError}");

        }

        /// <summary>新しいTreeNodeをidx番目の子ノードとして追加(SetOrMerge)する</summary>
        protected virtual TableParser AddTreeNode(int idx, int comboBlockerDepth = DEFAULT_DEPTH)
        {
            var strkList = StrokeList.WithLastStrokeAdded(idx);
            int depth = comboBlockerDepth._min(_comboBlockerDepth) - 1;
            bool bComboBlockerHit = depth == 0;
            if (IsInCombinationBlock && bComboBlockerHit) {
                // ComboBlocker が見つかったので、ここでいったん同時打鍵列を登録しておく
                addCombinationKey(strkList, false, false);
            }
            //Node node = SetNodeOrNewTreeNodeAtLast(strkList, null);
            Node node = SetOrMergeNthSubNode(idx, Node.MakeTreeNode(), bComboBlockerHit);
            if (node != null && node.IsTreeNode()) {
                // ComboDisabled になるのは、ComboBlockerが見つかった、その次からとなる
                //bool bComboBlocked = bComboBlockerFound || isComboBlocked;
                return new TableParser(node, strkList, depth);
            } else {
                return null;
            }
        }

        public virtual ArrowParser MakeArrowParser(int arrowIdx)
        {
            return new ArrowParser(TreeNode, StrokeList, arrowIdx, ComboBlockerDepth);
        }

        protected ArrowBundleParser MakeArrowBundleParser(int nextArrowIdx)
        {
            return new ArrowBundleParser(StrokeList, nextArrowIdx, ComboBlockerDepth);
        }

        // 前置書き換えパーザ
        protected PreRewriteParser MakePreRewriteParser(int idx)
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER");

            var targetStr = RewritePreTargetStr;

            // 前置文字列の指定がなければ単打テーブルから文字を拾ってくる
            if (targetStr._isEmpty()) {
                targetStr = idx >= 0 ? GetNthRootNodeString(idx) : "";
            }

            // 前置書き換えノード処理用のパーザを作成
            var parser = new PreRewriteParser(TreeNode, StrokeList, targetStr, ComboBlockerDepth);

            if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE");
            return parser;
        }

        // 後置書き換えパーザ
        protected PostRewriteParser MakePostRewriteParser(int idx)
        {
            // 後置書き換え文字の指定がある
            if (idx < 0 && RewritePostChar._notEmpty()) {
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"RewritePostChar={RewritePostChar}");
                var strokeList = StrokePathDict._safeGet(RewritePostChar);
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"strokeList={strokeList?.StrokePathString(":")}");
                if (strokeList != null && !strokeList.IsEmpty) {
                    idx = strokeList.At(0);
                    if (strokeList.Count > 1) {
                        InsertAtNextPos($",{strokeList.StrokePathString(",", 1)}{PeekPrevChar()}");  // 2打鍵目以降の挿入
                        if (Settings.LoggingTableFileInfo) logger.Info(() => $"RewritePostChar Index Found=({strokeList.StrokePathString(",")})");
                    }
                }
            }
            if (idx < 0) {
                ParseError($"単打面に存在しない後置書き換え文字: {RewritePostChar}");
                idx = 49;
            }
            // 後置書き換えノード処理用のパーザを作成
            return new PostRewriteParser(TreeNode, StrokeList, idx, ComboBlockerDepth);
        }

        public virtual void AddStringNode(int idx, bool bBare)
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: depth={Depth}, bBare={bBare}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            string str = ConvertKanji(CurrentStr);
            // TODO ひらがなや記号の削除については、ユーザーに指定させるようにする
            if (!IsSecondaryTableOnMultiStream || str._reMatch("^@[v^]") || (!str._isHiragana() && !str._isZenkakuSymbol() && !str._startsWith("@"))) {
                addTerminalNode(idx, Node.MakeStringNode($"{str}", bBare), true);
                if (!StrokePathDict.ContainsKey(str)) {
                    // 初めての文字ならストロークパスを登録
                    StrokePathDict[str] = StrokeList.WithLastStrokeAdded(idx);
                }
                if (HasRootTable && CurrentStr._startsWith("!{")) {
                    // Repeatable Key
                    if (Settings.LoggingTableFileInfo) logger.Info(() => $"REPEATABLE");
                    keyComboPool?.AddRepeatableKey(idx);
                }
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE: depth={Depth}");
            } else {
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE: Secondary table: IGNORE: {str}");
            }
        }

        public virtual void AddStringPairNode(OutputString[] stringPair = null)
        {
            ParseError($"unexpected token: {currentToken}");
        }

        public void AddFunctionNode(int idx)
        {
            AddFunctionNode(idx, CurrentStr);
        }

        public void AddFunctionNode(int idx, string funcMarker)
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: depth={Depth}, str={funcMarker}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            if (!IsSecondaryTableOnMultiStream || funcMarker == "v" || funcMarker == "^") {
                addTerminalNode(idx, Node.MakeFunctionNode(funcMarker), false);
                if (IsPrimary) savePresetFunction(idx, funcMarker);
            } else {
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE: Secondary table: IGNORE: {funcMarker}");
            }
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE: depth={Depth}");
        }

        void savePresetFunction(int idx, string marker)
        {
            var strkList = StrokeList.WithLastStrokeAdded(idx).strokeList.Select(x => x.ToString())._join(",");

            string chooseMinimalOne(string oldStr, string newStr)
            {
                return Settings.FunctionKeySeqSet.Contains(newStr) || (oldStr._notEmpty() && oldStr._countChar(',') <= newStr._countChar(',')) ? oldStr : newStr;
            }

            switch (marker) {
                case "B":   // BushuComp
                case "b":   // BushuComp
                    Settings.BushuCompKeySeq_Preset = chooseMinimalOne(Settings.BushuCompKeySeq_Preset, strkList);
                    break;
                case "A":   // bushuAssoc
                    Settings.BushuAssocKeySeq_Preset = chooseMinimalOne(Settings.BushuAssocKeySeq_Preset, strkList);
                    break;
                case "a":   // bushuAssocDirect
                    Settings.BushuAssocDirectKeySeq_Preset = chooseMinimalOne(Settings.BushuAssocDirectKeySeq_Preset, strkList);
                    break;
                case "M":   // mazegaki
                case "m":   // mazegaki
                    Settings.MazegakiKeySeq_Preset = chooseMinimalOne(Settings.MazegakiKeySeq_Preset, strkList);
                    break;
                case "!":   // history
                    Settings.HistoryKeySeq_Preset = chooseMinimalOne(Settings.HistoryKeySeq_Preset, strkList);
                    break;
                case "1":   // historyOneChar
                    Settings.HistoryOneCharKeySeq_Preset = chooseMinimalOne(Settings.HistoryOneCharKeySeq_Preset, strkList);
                    break;
                case "?":   // historyFewChars
                    Settings.HistoryFewCharsKeySeq_Preset = chooseMinimalOne(Settings.HistoryFewCharsKeySeq_Preset, strkList);
                    break;
                case "\\":  // nextThrough
                    Settings.NextThroughKeySeq_Preset = chooseMinimalOne(Settings.NextThroughKeySeq_Preset, strkList);
                    break;
                case "Z":   // zenkakuMode
                    Settings.ZenkakuModeKeySeq_Preset = chooseMinimalOne(Settings.ZenkakuModeKeySeq_Preset, strkList);
                    break;
                case "z":   //zenkakuOneChar
                    Settings.ZenkakuOneCharKeySeq_Preset = chooseMinimalOne(Settings.ZenkakuOneCharKeySeq_Preset, strkList);
                    break;
                case "K":   // katakanaMode
                    Settings.KatakanaModeKeySeq_Preset = chooseMinimalOne(Settings.KatakanaModeKeySeq_Preset, strkList);
                    break;
                case "k":   // katakanaOneShot
                    Settings.KatakanaOneShotKeySeq_Preset = chooseMinimalOne(Settings.KatakanaOneShotKeySeq_Preset, strkList);
                    break;
                case "h":   // hankakuKatakanaOneShot
                    Settings.HankakuKatakanaOneShotKeySeq_Preset = chooseMinimalOne(Settings.HankakuKatakanaOneShotKeySeq_Preset, strkList);
                    break;
                case "bs":  // blockerSetterOneShot
                    Settings.BlockerSetterOneShotKeySeq_Preset = chooseMinimalOne(Settings.BlockerSetterOneShotKeySeq_Preset, strkList);
                    break;
            }
        }

        /// <summary>
        /// 終端ノードの追加と同時打鍵列の組合せの登録<br/>
        /// 同時打鍵の場合は、ブロックのルートキーをCOMBO_DECKEY_STARTまでシフトする
        /// </summary>
        /// <param name="prevNth"></param>
        /// <param name="lastNth"></param>
        void addTerminalNode(int idx, Node node, bool bStr)
        {
            // 同時打鍵ブロックの外であっても、終端ノードであることを通知しておく必要がある
            addCombinationKey(StrokeList.WithLastStrokeAdded(idx), bStr, !bStr);

            //SetNodeOrNewTreeNodeAtLast(StrokeList.WithLastStrokeAdded(idx), node);
            SetOrMergeNthSubNode(idx, node);
        }

        /// <summary>
        /// 同時打鍵を含む打鍵列を KeyCombinationPool に登録する。<br/>
        /// 1ストロークの場合は、単打として登録する<br/>
        /// 先頭2打鍵が同時打鍵で、その後は順次打鍵になるケースもある。どの打鍵が同時打鍵かは、strokeList.comboFlagList で判断する。<br/>
        /// なお、先頭2打が同時打鍵でその後順次打鍵に移行する場合は、一回だけ、末尾に -1 が格納されて呼び出される。
        /// </summary>
        /// <param name="strkList"></param>
        /// <param name="hasStr"></param>
        protected void addCombinationKey(CStrokeList strkList, bool hasStr, bool hasFunc)
        {
            if (!strkList.IsEmpty) {
                int strk = 0;
                int shiftOffset = calcShiftOffset(strkList.At(0));

                if (Settings.LoggingTableFileInfo)
                    logger.Info($"CALLED: strkList={strkList?.StrokePathString(":")}, hasStr={hasStr}, hasFunc={hasFunc}, shiftOffset={shiftOffset}, ShiftPlane={ShiftPlane}");

                void addSeqShiftKey()
                {
                    if (!sequentialShiftKeys.Contains(strk)) {
                        addSequentialShiftKey(strk, shiftOffset);
                        sequentialShiftKeys.Add(strk);
                    }
                }

                var comboList = new List<int>();

                // 先頭キーはシフト化
                strk = strkList.At(0);
                if (strkList.ComboFlagAt(0)) {
                    strk = makeComboDecKey(strk);
                } else if (strk < DecoderKeys.PLANE_DECKEY_NUM) {
                    strk += shiftOffset;
                }

                if (strkList.Count == 1 || strkList.ComboFlagAt(0)) {
                    // 長さが1の場合は、単打として登録する
                    comboList.Add(strk);
                } else {
                    // 長さが2以上で、同時打鍵でない
                    addSeqShiftKey();
                }

                // 中間キー
                int comboKeyCount = strkList.CountComboKey();
                for (int i = 1; i < strkList.Count - 1; ++i) {
                    strk = strkList.At(i);
                    if (strkList.ComboFlagAt(i)) {
                        // 同時打鍵での中間キーは終端キーとの重複を避けるためシフト化しておく
                        comboList.Add(i < comboKeyCount - 1 ? makeNonTerminalDuplicatableComboKey(strk) : strk);
                    } else {
                        addSeqShiftKey();
                    }
                }

                // 末尾キー
                if (comboKeyCount == strkList.Count) {
                    // Comboの末尾はそのまま
                    comboList.Add(strkList.Last());
                }

                if (comboList._notEmpty()) {
                    bool comboBlocked = comboKeyCount > 0 && comboKeyCount < strkList.Count;
#if DEBUG
                    if (comboBlocked) {
                        if (Settings.LoggingTableFileInfo) logger.Info($"comboKeyCount={comboKeyCount}, strkList.Count={strkList.Count}");
                    }
#endif
                    AddCombinationKeyCombo(comboList, shiftOffset, hasStr, hasFunc, comboBlocked);
                }
            }
        }

        // 同時打鍵列の組合せを作成して登録しておく
        protected void AddCombinationKeyCombo(List<int> deckeyList, int shiftOffset, bool hasStr, bool hasFunc, bool comboBlocked)
        {
            if (Settings.LoggingTableFileInfo)
                logger.Info(() => $"{deckeyList._keyString()}={CurrentStr}, shiftOffset={shiftOffset}, hasStr={hasStr}, comboBlocked={comboBlocked}");
#if DEBUG
            if (deckeyList._keyString() == "826:127") {
                if (Settings.LoggingTableFileInfo) logger.Info("HIT");
            }
#endif
            var comboKeyList = deckeyList.Select(x => makeShiftedDecKey(x, shiftOffset)).ToList();      // 先頭キーのオフセットに合わせる
            keyComboPool?.AddComboShiftKeys(comboKeyList, shiftKeyKind); // 元の拡張シフトキーコードに戻して、同時打鍵キーとして登録
            keyComboPool?.AddEntry(deckeyList, comboKeyList, shiftKeyKind, hasStr, hasFunc, comboBlocked, isStackLikeCombo);
        }

        void addSequentialShiftKey(int decKey, int shiftOffset)
        {
            if (keyComboPool != null) {
                keyComboPool.ContainsSequentialShiftKey = true;
                keyComboPool.AddComboShiftKey(makeShiftedDecKey(decKey, shiftOffset), ShiftKeyKind.SequentialShift);
            }
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
        public ArrowParser(Node treeNode, CStrokeList strkList, int arrowIdx, int comboBlockerDepth)
            : base(treeNode, strkList, comboBlockerDepth)
        {
            UnhandledArrowIndex = arrowIdx;
        }

        // 矢印記法(-\d+(,\d+)*>)の直後を解析して第1打鍵位置に従って配置する
        public void Parse()
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: lineNum={LineNumber}, depth={Depth}, UnhandledArrowIndex={UnhandledArrowIndex}");
            //int arrowIdx = arrowIndex;
            int comboBlockerDepth = DEFAULT_DEPTH;
            if (PeekNextChar() == '|') {
                // '>|' という形式だった
                AdvanceCharPos(1);
                comboBlockerDepth = 1;
                if (PeekNextChar() == '|') {
                    // '>||' という形式だった
                    AdvanceCharPos(1);
                    comboBlockerDepth = 2;
                }
            }

            readNextToken(true);
            switch (currentToken) {
                case TOKEN.ARROW:
                    // 矢印記法の連続
                    // ルートパーザの場合は、 ArrowIndex はすでに Shiftされている
                    AddArrowNode(ArrowIndex).Parse();
                    break;

                case TOKEN.REWRITE_PRE:
                case TOKEN.REWRITE_POST:
                    ParseError($"トップレベル以外では前置または後置書き換え記法を使用できません。");
                    break;

                case TOKEN.COMMA:
                    if (parseArrow()) {
                        // 矢印記法連続の簡略記法
                        // ルートパーザの場合は、 ArrowIndex はすでに Shiftされている
                        AddArrowNode(ArrowIndex).Parse();
                    }
                    break;

                case TOKEN.LBRACE:
                    // ブロック開始
                    // ルートパーザの場合は、 UnhandledArrowIndex はすでに Shiftされている
                    if (HasRootTable) logger.InfoH($"LBRACE: shiftPlane={ShiftPlane}");
                    AddTreeNode(UnhandledArrowIndex, comboBlockerDepth)?.ParseNodeBlock();
                    break;

                //AddLeafNode(currentToken, UnhandledArrowIndex);
                case TOKEN.STRING:             // "str" : 文字列ノード
                case TOKEN.BARE_STRING:        // str : 文字列ノード
                    // ルートパーザの場合は、 UnhandledArrowIndex はすでに Shiftされている
                    AddStringNode(UnhandledArrowIndex, currentToken == TOKEN.BARE_STRING);
                    break;

                case TOKEN.FUNCTION:           // @c : 機能ノード
                    // ルートパーザの場合は、 UnhandledArrowIndex はすでに Shiftされている
                    AddFunctionNode(UnhandledArrowIndex);
                    break;

                case TOKEN.STRING_PAIR:
                    OutputString[] stringPair = Helper.Array(StringPair[0], StringPair[1]);
                    AddTreeNode(UnhandledArrowIndex, comboBlockerDepth)?.AddStringPairNode(stringPair);
                    break;

                default:
                    ParseError("PreRewriteParser-addArrowNode");
                    break;
            }

            currentToken = TOKEN.IGNORE;    // いったん末端ノードの処理をしたら、矢印記法を抜けるまで無視
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE: lineNum={LineNumber}, depth={Depth}");
        } 

        protected virtual ArrowParser AddArrowNode(int arrowIdx)
        {
            var strkList = StrokeList.WithLastStrokeAdded(UnhandledArrowIndex);
            //Node node = SetNodeOrNewTreeNodeAtLast(strkList, null);
            Node node = SetOrMergeNthSubNode(UnhandledArrowIndex, Node.MakeTreeNode());
            return new ArrowParser(node, strkList, arrowIdx, ComboBlockerDepth);
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
        public ArrowBundleParser(CStrokeList strkList, int nextArrowIdx, int comboBlockerDepth)
            : base(RootTableNode, strkList, comboBlockerDepth)
        {
            nextArrowIndex = nextArrowIdx;
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        public Node Parse()
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: depth={Depth}, nextArrowIndex={nextArrowIndex}");

            Node myNode = null;

            int n = 0;
            int row = 0;
            //bool isPrevDelim = true;
            readNextToken(true);
            if (currentToken != TOKEN.LBRACE) { // 直後は '{' でブロックの始まりである必要がある
                ParseError($"parseArrowBundleNode: TOKEN.LBRACE is excpected, but {currentToken}");
                return myNode;
            }
            if (HasRootTable) logger.InfoH($"LBRACE: shiftPlane={ShiftPlane}");

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

            if (HasRootTable) {
                logger.InfoH($"RBRACE: shiftPlane and placeHolders initialized");
                Context.shiftPlane = 0;
                placeHolders.Initialize();
            }
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE: depth={Depth}");

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

        public RewriteParser(Node treeNode, CStrokeList strkList, int arrowIdx, int comboBlockerDepth)
            : base(treeNode, strkList, arrowIdx, comboBlockerDepth)
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

        /// <summary>node を treeNode の idx 位置に merge する。merge に成功したら merge先のノードを返す</summary>
        protected Node mergeNode(Node treeNode, int idx, Node node)
        {
            int tgtIdx = ShiftDecKey(idx);  // 矢印記法でないルートブロックの場合は、まだShiftされていないので、ここで Shift する必要あり
            (Node mergedNode, bool bOverwrite) = treeNode.SetOrMergeNthSubNode(tgtIdx, node);
            if (bOverwrite && (Settings.DuplicateWarningEnabled || IsInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.WarnH($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return mergedNode;
        }

        /// <summary>書き換え情報を格納する node を NodeTable の idx 位置に upsert する。upsert に成功したら upsert先の RewriteNode を返す</summary>
        /// <param name="idx"></param>
        /// <param name="node"></param>
        protected Node upsertRewriteNodeToNodeTable(int idx, Node node /*, bool bRootTable */)
        {
            int rewriteIdx = TreeNode.IsRootTreeNode() ? ShiftDecKey(idx) : idx;  // 矢印記法でないルートブロックの場合は、まだShiftされていないので、ここで Shift する必要あり
            //var rewriteNode = bRootTable ? GetNthRootNode(rewriteIdx) : GetNthSubNode(idx);
            var rewriteNode = GetNthSubNode(rewriteIdx);
            if (rewriteNode != null) {
                // 挿入先を RewriteNode に変身させる
                // TODO: node.IsTreeNode() だったらどうする?
                rewriteNode.MakeRewritable();
            } else {
                // 挿入先が空なので新規に RewriteNode を作成しておく
                rewriteNode = SetOrMergeNthSubNode(rewriteIdx, Node.MakeRewriteNode());
            }
            if (rewriteNode.IsRewriteNode()) {
                // 挿入先の rewriteNode が Rewritable である場合に限り、node を upsert する
                rewriteNode.UpsertRewritePair(targetStr, node);
                return rewriteNode;
            } else {
                logger.WarnH("RewriteNode NOT merged");
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
        public PreRewriteParser(Node treeNode, CStrokeList strkList, string targetStr, int comboBlockerDepth)
            : base(treeNode, strkList, -1, comboBlockerDepth)
        {
            this.targetStr = targetStr;
        }

        // UnhandledArrowIndex の処理をした後、ネストされた新しいパーザを作成
        private PreRewriteParser makeNestedParser(int unhandledIdx = -1)
        {
            return new PreRewriteParser(TreeNode, StrokeList, myTargetString(UnhandledArrowIndex), ComboBlockerDepth) {
                bNested = true,
                UnhandledArrowIndex = unhandledIdx
            };
        }

        // ブロック処理(ネストされたパーザから呼ばれた場合は、通常のTableParserにする)
        protected override TableParser AddTreeNode(int idx, int _ = DEFAULT_DEPTH)
        {
            if (!bNested) {
                // 最初のブロック処理(パーザをネストさせる)
                // %X,,Y>{
                // のケース
                return makeNestedParser();
            } else if (idx >= 0) {
                // ネストされたパーザから呼ばれた場合は、通常のTableParserにする
                // %X,,Y>{
                //   -Z>{
                // のケース
                TableParser parser = null;
                var strkList = StrokeList.WithLastStrokeAdded(idx);
                if (IsInCombinationBlock) {
                    // 同時打鍵の場合は、直前キー用にTreeNodeを挿入しておく必要がある
                    Node node = SetOrMergeNthSubNode(idx, Node.MakeTreeNode());
                    parser = new PreRewriteParser(node, strkList, targetStr, ComboBlockerDepth);
                    UnhandledArrowIndex = -1;
                } else {
                    // 順次打鍵なら、直前キーを文字に変換して、それを書き換え対象文字とする
                    Node treeNode = Node.MakeTreeNode();
                    if (upsertRewriteNodeToNodeTable(idx, treeNode/*, true*/) != null) {
                        // 通常のTableParserを返す
                        parser = new TableParser(treeNode, strkList, ComboBlockerDepth);
                    }
                }
                return parser;
            } else {
                logger.WarnH($"Illegal index={idx}");
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
            ArrowParser parser = this;
            //targetStr = myTargetString(UnhandledArrowIndex);
            if (IsInCombinationBlock) {
                // 同時打鍵の場合は、直前キー用にTreeNodeを挿入しておく必要がある
                var strkList = StrokeList.WithLastStrokeAdded(arrowIdx);
                Node node = SetOrMergeNthSubNode(arrowIdx, Node.MakeTreeNode());
                parser = new PreRewriteParser(node, strkList, targetStr, ComboBlockerDepth);
                UnhandledArrowIndex = -1;
            } else {
                // 順次打鍵なら、直前キーを文字に変換して、それを書き換え対象文字とする
                targetStr = myTargetString(UnhandledArrowIndex);
                UnhandledArrowIndex = arrowIdx;
            }
            return parser;
        }

        // ここが呼ばれたとき、 idx にはテーブル内での index または矢印記法による UnhandledArrowIndex が入っており、これが書き換えノードのindexとなる
        public override void AddStringNode(int idx, bool bBare)
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={idx}");
            if (idx < 0) {
                // %X>の直後に文字列がきたケース。
                ParseError("前置書き換え記法には、少なくとも1つの後続キーが必要です。");
            } else {
                // 書き換え情報を upsert する
                upsertRewriteNodeToNodeTable(idx, Node.MakeStringNode(CurrentStr, bBare) /*, false*/);
            }
            if (Settings.LoggingTableFileInfo) logger.Info("LEAVE");
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
        public PostRewriteParser(Node rewriteNode, CStrokeList strkList, int arrowIdx, int comboBlockerDepth, string targetStr = null)
            : base(rewriteNode, strkList, arrowIdx, comboBlockerDepth)
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
            if (myStr._isEmpty() && idx >= 0 && !IsInCombinationBlock) {    // 同時打鍵の場合は、単打テーブルからは拾わない
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
        protected override TableParser AddTreeNode(int idx, int _ = DEFAULT_DEPTH)
        {
            if (bNested) {
                ParseError("後置書き換えではブロックのネストはできません");
                return null;
            }
            // 後置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
            var myStr = getMyString(idx);
            var strkList = StrokeList.WithLastStrokeAdded(idx);
            idx = strkList.Last();
            if (IsInCombinationBlock) {
                // 同時打鍵の場合
                bool bStr = myStr._notEmpty();
                bool bFunc = (myStr?.IsFunction() ?? false) || !bStr;    // 出力文字がなければ、機能キーとして扱う
                addCombinationKey(strkList, bStr, bFunc);
            }
            // 同時打鍵の場合は、TreeNodeは RootNodeの子ノードになっているはず。
            // 逆に同時打鍵でない場合は、 TreeNode == RootNode のはず
            Node node = mergeNode(TreeNode, idx, Node.MakeRewriteTreeNode(myStr));
            if (node != null) {
                return new PostRewriteParser(node, strkList, -1, ComboBlockerDepth, targetStr) { bNested = true };
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
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: prevIndex={UnhandledArrowIndex}, arrowIdx={arrowIdx}");
            ArrowParser parser = this;
            int prevIndex = UnhandledArrowIndex;
            UnhandledArrowIndex = arrowIdx;
            if (bNested) {
                prefixStr = myPrefixString(prevIndex);
            } else {
                Node node = TreeNode.GetNthSubNode(prevIndex);
                if (IsInCombinationBlock || (node != null && node.IsTreeNode())) {
                    // 同時打鍵または多ストロークの途中打鍵の場合
                    if (IsInCombinationBlock) {
                        // 同時打鍵の場合は、直前キー用にTreeNodeを挿入しておいたり、いろいろ処理が必要
                        node = SetOrMergeNthSubNode(prevIndex, Node.MakeTreeNode());
                    }
                    var strkList = StrokeList.WithLastStrokeAdded(prevIndex);
                    parser = new PostRewriteParser(node, strkList, arrowIdx, ComboBlockerDepth, targetStr);
                } else {
                    // 単打の順次打鍵なら、直前キーを文字に変換して、それを書き換え対象文字とする
                    targetStr = myTargetString(prevIndex);
                }
            }
            return parser;
        }

        public override void AddStringNode(int idx, bool bBare)
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={idx}");
            if (!TreeNode.IsRewriteNode()) {
                // &X,..>の直後に文字列がきた
                ParseError("後置書き換え記法には、ブロックが必要です。");
            } else {
                var tgtStr = prefixStr + GetNthRootNodeString(idx)._toSafe() + targetStr._toSafe();
                TreeNode.UpsertRewritePair(tgtStr, Node.MakeStringNode(CurrentStr, bBare));
            }
            if (Settings.LoggingTableFileInfo) logger.Info("LEAVE");
        }

        // 書き換え文字列のペア
        public override void AddStringPairNode(OutputString[] stringPair = null)
        {
            if (stringPair == null) stringPair = StringPair;
            var str1 = stringPair._getNth(0);
            var str2 = stringPair._getNth(1);
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: str1={str1.DebugString()}, str2={str2.DebugString()}");
            if (str1._isEmpty() || str2._isEmpty()) {
                ParseError("不正な書き換え文字列ペア");
            } else {
                var tgtStr = str1.GetBareString(-1) + targetStr._toSafe();
                TreeNode.UpsertRewritePair(tgtStr, Node.MakeStringNode(str2));
            }
            if (Settings.LoggingTableFileInfo) logger.Info("LEAVE");
        }

    }

    /// <summary>
    /// ルートテーブルの解析
    /// </summary>
    class RootTableParser : TableParser
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>ルートノードテーブルを持つパーザか</summary>
        public override bool HasRootTable => true;

        bool isKanchokuModeParser = true;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public RootTableParser(bool bForKanchoku)
            : base(RootTableNode, null, DEFAULT_DEPTH)
        {
            isKanchokuModeParser = bForKanchoku;
        }

        /// <summary>
        /// トップレベルのテーブル定義を解析してストローク木を構築する。<br/>
        /// 前置・後置の書き換えモードは、このトップレベルでしか記述できない。
        /// </summary>
        public void ParseRootTable()
        {
            if (Settings.LoggingTableFileInfo) logger.Info("ENTER");

            if (isKanchokuModeParser) {
                // 漢直モードの場合、ルートテーブルの文字キーを @^ (MyChar機能)で埋めておく
                fillRootStrokeTableByMyCharFunction();
            }

            // トップレベルの解析(ArrowIndex はすでに Shiftされている)
            readNextToken(true);
            while (Context.currentToken != TOKEN.END) {
                switch (Context.currentToken) {
                    case TOKEN.LBRACE:
                        if (HasRootTable) logger.InfoH($"LBRACE: shiftPlane={ShiftPlane}");
                        ParseNodeBlock();
                        break;

                    case TOKEN.ARROW:
                        // -X>形式
                        // ArrowIndex はすでに Shiftされている
                        //handleArrowNode(ArrowIndex);
                        MakeArrowParser(ArrowIndex).Parse();
                        break;

                    case TOKEN.REWRITE_PRE:
                        // %X>形式: 前置書き換えモードに移行
                        // ArrowIndex はすでに Shiftされている
                        MakePreRewriteParser(ArrowIndex).Parse();
                        break;

                    case TOKEN.REWRITE_POST:
                        // &X>形式: 後置書き換えモードに移行
                        // ArrowIndex はすでに Shiftされている
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

            ////順次打鍵列の先頭キーを単打として追加する → 先にRootTableを '@^' で埋めたので、これは不要になった
            //if (isKanchokuModeParser) {
            //    addSequentialHeadAsSingleHitKey();
            //}

            // ComboShiftキーのうち、4つ以上のcomboの先頭キーになっているものを Major キーとして登録
            keyComboPool?.AddMajorComboShiftKeys();

            // 部分キーに対して、非終端マークをセット
            keyComboPool?.SetNonTerminalMarkForSubkeys(!isKanchokuModeParser);
            if (Logger.IsInfoEnabled && logger.IsInfoPromoted) {
                keyComboPool?.DebugPrint();
            }

            if (Context.bRewriteEnabled) {
                // 書き換えノードがあったら、SandSの疑似同時打鍵サポートをOFFにしておく
                Settings.SandSEnablePostShiftCurrently = false;
            }

            // 優先する順次打鍵列
            if (keyComboPool?.ContainsUnorderedShiftKey ?? false) {
                foreach (var seq in Settings.SequentialPriorityWordSet) {
                    var strkList = GetStrokeList(seq);
                    if (strkList._notEmpty()) {
                        foreach (var s in strkList) {
                            if (s._startsWith("-:")) {
                                Settings.ThreeKeysComboPriorityTailKeyStringSet.Add(s._safeSubstring(2));
                            } else if (s._endsWith(":-")) {
                                Settings.ThreeKeysComboPriorityHeadKeyStringSet.Add(s._safeSubstring(0, -2));
                            } else {
                                Settings.SequentialPriorityWordKeyStringSet.Add(s);
                            }
                        }
                    } else if (seq._safeContains(':')) {
                        Settings.SequentialPriorityWordKeyStringSet.Add(seq);
                    }
                }
                if (Settings.LoggingTableFileInfo) {
                    logger.Info(() => $"SequentialPriorityWordKeyStringSet={Settings.SequentialPriorityWordKeyStringSet._join(",")}");
                    logger.Info(() => $"ThreeKeysComboPriorityHeadKeyStringSet={Settings.ThreeKeysComboPriorityHeadKeyStringSet._join(",")}");
                    logger.Info(() => $"ThreeKeysComboPriorityTailKeyStringSet={Settings.ThreeKeysComboPriorityTailKeyStringSet._join(",")}");
                }
            }

            // 全ノードの情報を OutputLines に書き出す
            RootTableNode.OutputLine(OutputLines);

            // 先にRootTableを '@^' で埋めたので、↓は不要になった
            //if (isKanchokuModeParser) {
            //    // 漢直モードの場合、ルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる
            //    addMyCharFunctionInRootStrokeTable();
            //}

            if (Settings.LoggingTableFileInfo) logger.Info(() => $"LEAVE: KeyCombinationPool.Count={keyComboPool?.Count}");
        }

        public void ParseDirectives()
        {
            readNextToken(false, true);
            while (currentToken != TOKEN.END) {
                SkipToEndOfLine();
                readNextToken(false, true);
            }
        }

        /// <summary>拡張修飾キーが同時打鍵キーとして使われた場合は、そのキーの単打設定として本来のキー出力を追加する</summary>
        void addExtModfierAsSingleHitKey()
        {
            void addExtModAsSingleKey(string keyName)
            {
                int dk = DecoderKeyVsVKey.GetFuncDecKeyByName(keyName);
                if (dk >= 0) {
                    if (RootTableNode.GetNthSubNode(dk) == null &&
                        RootTableNode.GetNthSubNode(dk + comboDeckeyStart) != null) {
                        // 単打設定が存在せず、同時打鍵の先頭キーになっている場合は、単打設定を追加する
                        AddCombinationKeyCombo(Helper.MakeList(dk), 0, true, false, false);  // 出力文字列を持つ単打指定
                        OutputLines.Add($"-{dk}>\"!{{{keyName}}}\"");
                    }
                }
            }

            if (Settings.UseComboExtModKeyAsSingleHit) {
                // とりあえず nfer/xfer/imeon/imeoff だけ対応
                addExtModAsSingleKey("nfer");
                addExtModAsSingleKey("xfer");
                addExtModAsSingleKey("imeon");
                addExtModAsSingleKey("imeoff");
            }
        }

        ///// <summary>順次打鍵列の先頭キーを単打として追加する</summary>
        //void addSequentialHeadAsSingleHitKey()
        //{
        //    if (keyComboPool != null) {
        //        for (int idx = 0; idx < DecoderKeys.NORMAL_DECKEY_NUM; ++idx) {
        //            if (RootTableNode.GetNthSubNode(idx)?.IsTreeNode() ?? false) {
        //                if (keyComboPool.GetEntry(idx) == null) {
        //                    logger.Info(() => $"Add DUMMY Single Hit for SequentialHead");
        //                    AddDummySingleHitCombo(dk);
        //                }
        //            }
        //        }
        //    }
        //}

        ///// <summary>もしルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる</summary>
        //void addMyCharFunctionInRootStrokeTable()
        //{
        //    for (int idx = 0; idx < DecoderKeys.NORMAL_DECKEY_NUM; ++idx) {
        //        if (RootTableNode.GetNthSubNode(idx) == null) {
        //            OutputLines.Add($"-{idx}>@^");
        //        }
        //    }
        //}

        /// <summary>ルートテーブルの文字キーを @^ (MyChar機能)で埋めておく</summary>
        void fillRootStrokeTableByMyCharFunction()
        {
            bool bLogging = Settings.LoggingTableFileInfo;
            Settings.LoggingTableFileInfo = false;
            if (bLogging) logger.Info("ENTER");
            for (int idx = 0; idx < DecoderKeys.NORMAL_DECKEY_NUM; ++idx) {
                AddFunctionNode(idx, "^");
            }
            if (bLogging) logger.Info("LEAVE");
            Settings.LoggingTableFileInfo = bLogging;
        }

    }

    /// <summary>
    /// テーブルファイル解析器
    /// </summary>
    class TableFileParser
    {
        private static Logger logger = Logger.GetLogger();

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
        public void ParseTableFile(string filename, string outFilename, KeyCombinationPool poolK, KeyCombinationPool poolA, int tableNo, bool secondaryOnMulti, bool bTest)
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: filename={filename}");

            List<string> outputLines = new List<string>();

            void parseRootTable(TableLines tblLines, KeyCombinationPool pool, bool bKanchoku)
            {
                ParserContext.CreateSingleton(tblLines, pool, DecoderKeys.GetComboDeckeyStart(bKanchoku), secondaryOnMulti);
                var parser = new RootTableParser(bKanchoku);
                parser.ParseRootTable();
                //writeAllLines(outFilename, ParserContext.Singleton.OutputLines);
                outputLines.AddRange(ParserContext.Singleton.OutputLines);
                writeAllLines($"tmp/parsedTableFile{(bKanchoku ? 'K' : 'A')}{tableNo}.txt", ParserContext.Singleton.tableLines.GetLines());
                ParserContext.FinalizeSingleton();
            }

            string errorMsg = "";

            // 漢直モードの解析
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"Analyze for KANCHOKU mode");
            TableLines tableLines = new TableLines();
            tableLines.ReadAllLines(filename, tableNo == 1, true);
            if (tableLines.NotEmpty) {
                parseRootTable(tableLines, poolK, true);
                errorMsg = tableLines.getErrorMessage();
                // 英数モードの解析
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"Analyze for EISU mode");
                tableLines = new TableLines();
                tableLines.ReadAllLines(filename, tableNo == 1, false);
                parseRootTable(tableLines, poolA, false);
                if (errorMsg._isEmpty()) errorMsg = tableLines.getErrorMessage();
                // 解析結果の出力
                writeAllLines(outFilename, outputLines);
            } else {
                tableLines.Error($"テーブルファイル({filename})が開けないか、内容が空でした。");
            }

            if (!bTest && errorMsg._notEmpty()) {
                SystemHelper.ShowWarningMessageBox(errorMsg);
            }

            if (Settings.LoggingTableFileInfo) logger.Info("LEAVE");
        }

        /// <summary>
        /// テーブル定義を読んでディレクティブだけを解析する
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="outFilename"></param>
        /// <param name="pool">対象となる KeyComboPool</param>
        public void ReadDirectives(string filename, bool primary)
        {
            if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: filename={filename}");

            TableLines tableLines = new TableLines();
            tableLines.ReadAllLines(filename, primary, true);

            if (tableLines.NotEmpty) {
                ParserContext.CreateSingleton(tableLines, null, DecoderKeys.GetComboDeckeyStart(true), !primary);
                var parser = new RootTableParser(true);
                parser.ParseDirectives();
                ParserContext.FinalizeSingleton();
            } else {
                tableLines.Error($"{(primary ? "主" : "副")}テーブルファイル({filename})が開けません\r\n設定ダイアログを開いて使用するファイルを選択しなおしてください。");
                tableLines.showErrorMessage();
            }

            //tableLines.showErrorMessage();

            if (Settings.LoggingTableFileInfo) logger.Info("LEAVE");
        }

        private void writeAllLines(string filename, List<string> lines)
        {
            KanchokuHelper.WriteAllLinesToFile(filename, lines);
        }

    }

}
