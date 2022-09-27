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
                if (parser.IsRootParser) {
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

        // グローバルなルートパーザか
        public override bool IsRootParser => Depth == 0;

        // ルートノードへのアクセッサ
        protected static Node RootTableNode => ParserContext.Singleton.rootTableNode;

        protected bool isComboBlocked = false;
        public bool IsInCombinationBlock => !isComboBlocked && isInCombinationBlock;

        // 当Parserによる解析内容を格納するツリーノード
        Node _treeNode;

        protected Node TreeNode => _treeNode;

        // ルートノードから当ノードに至るまでの打鍵リスト
        CStrokeList _strokeList = null;

        // ルートパーザか否かの判定およびaddCombinationKey() で必要になる
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
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + comboDeckeyStart;
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
            return RootTableNode.GetNthSubNode(ShiftDecKey(n));
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
        public TableParser(Node treeNode, CStrokeList strkList, int shiftPlane = -1)
            : base()
        {
            _treeNode = treeNode;
            _strokeList = new CStrokeList(this, strkList);
            _shiftPlane = shiftPlane;
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
            if (IsInCombinationBlock) {
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
            if (bOverwrite && (Settings.DuplicateWarningEnabled || IsInCombinationBlock) && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return mergedNode;
        }

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
                if (IsRootParser) idx = ShiftDecKey(idx);   // ルートパーザの場合は、 idx を Shiftしておく
                switch (currentToken) {
                    case TOKEN.LBRACE:
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

        protected virtual TableParser AddTreeNode(int idx, bool bComboBlockerFound = false)
        {
            var strkList = StrokeList.WithLastStrokeAdded(idx);
            if (IsInCombinationBlock && bComboBlockerFound) {
                // ComboBlocker が見つかったので、ここでいったん同時打鍵列を登録しておく(末尾にダミーの -1 を追加して)
                addCombinationKey(strkList.WithLastStrokeAdded(-1), false);
            }
            //Node node = SetNodeOrNewTreeNodeAtLast(strkList, null);
            Node node = SetOrMergeNthSubNode(idx, Node.MakeTreeNode());
            if (node != null && node.IsTreeNode()) {
                // ComboDisabled になるのは、ComboBlockerが見つかった、その次からとなる
                bool bComboBlocked = bComboBlockerFound || isComboBlocked;
                return new TableParser(node, strkList) {
                    isComboBlocked = bComboBlocked
                };
            } else {
                return null;
            }
        }

        public virtual ArrowParser MakeArrowParser(int arrowIdx)
        {
            return new ArrowParser(TreeNode, StrokeList, arrowIdx);
        }

        protected ArrowBundleParser MakeArrowBundleParser(int nextArrowIdx)
        {
            return new ArrowBundleParser(StrokeList, -1, nextArrowIdx);
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
            var parser = new PreRewriteParser(TreeNode, StrokeList, targetStr);

            logger.DebugH(() => $"LEAVE");
            return parser;
        }

        // 後置書き換えパーザ
        protected PostRewriteParser MakePostRewriteParser(int idx)
        {
            // 後置書き換えノード処理用のパーザを作成
            return new PostRewriteParser(TreeNode, StrokeList, idx);
        }

        public virtual void AddStringNode(int idx, bool bBare)
        {
            logger.Debug(() => $"ENTER: depth={Depth}, bBare={bBare}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            addTerminalNode(idx, Node.MakeStringNode($"{ConvertKanji(CurrentStr)}", bBare));
            if (IsRootParser && CurrentStr._startsWith("!{")) {
                // Repeatable Key
                logger.DebugH(() => $"REPEATABLE");
                keyComboPool?.AddRepeatableKey(idx);
            }
            logger.Debug(() => $"LEAVE: depth={Depth}");
        }

        protected virtual void AddStringPairNode()
        {
            ParseError($"unexpected token: {currentToken}");
        }

        public void AddFunctionNode(int idx)
        {
            logger.DebugH(() => $"ENTER: depth={Depth}, str={CurrentStr}");
            // 終端ノードの追加と同時打鍵列の組合せの登録
            var funcMarker = CurrentStr;
            addTerminalNode(idx, Node.MakeFunctionNode(funcMarker));
            if (IsPrimary) savePresetFunction(idx, funcMarker);
            logger.DebugH(() => $"LEAVE: depth={Depth}");
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
        void addTerminalNode(int idx, Node node)
        {
            addCombinationKey(StrokeList.WithLastStrokeAdded(idx), true);

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
        protected void addCombinationKey(CStrokeList strkList, bool hasStr)
        {
            if (!strkList.IsEmpty) {
                int strk = 0;
                int shiftOffset = calcShiftOffset(strkList.At(0));

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
                for (int i = 1; i < strkList.Count - 1; ++i) {
                    strk = strkList.At(i);
                    if (strkList.ComboFlagAt(i)) {
                        // 同時打鍵での中間キーは終端キーとの重複を避けるためシフト化しておく
                        comboList.Add(makeNonTerminalDuplicatableComboKey(strk));
                    } else {
                        addSeqShiftKey();
                    }
                }

                // 末尾キー(先頭2打が同時打鍵でその後順次打鍵に移行する場合は、末尾にダミーの -1 が格納されてくるので、それは除外)
                if (strkList.Count > 1 && strkList.Last() >= 0 && strkList.ComboFlagAtLast()) {
                    // Comboの末尾はそのまま
                    comboList.Add(strkList.Last());
                }

                if (comboList._notEmpty()) {
                    AddCombinationKeyCombo(comboList, shiftOffset, hasStr);
                }

                // var list = new List<int>(strkList.strokeList);
                //// 先頭キーはシフト化
                //if (IsInCombinationBlock) {
                //    list[0] = makeComboDecKey(list[0]);
                //    for (int i = 1; i < list.Count - 1; ++i) {
                //        // 同時打鍵での中間キーは終端キーとの重複を避けるためシフト化しておく
                //        list[i] = makeNonTerminalDuplicatableComboKey(list[i]);
                //    }
                //    AddCombinationKeyCombo(list, shiftOffset, hasStr);
                //} else {
                //    if (list[0] < DecoderKeys.PLANE_DECKEY_NUM) {
                //        list[0] += shiftOffset;
                //    }
                //    if (list.Count == 1) {
                //        AddCombinationKeyCombo(list, shiftOffset, hasStr);
                //    } else {
                //        if (keyComboPool != null) keyComboPool.ContainsSequentialShiftKey = true;
                //        for (int i = 0; i < list.Count - 1; ++i) {
                //            int dk = list[i];
                //            if (!strkList.ComboFlagAt(i) && !sequentialShiftKeys.Contains(dk)) {
                //                addSequentialShiftKey(dk, shiftOffset);
                //                sequentialShiftKeys.Add(dk);
                //            }
                //        }
                //    }
                //}
            }
        }

        // 同時打鍵列の組合せを作成して登録しておく
        protected void AddCombinationKeyCombo(List<int> deckeyList, int shiftOffset, bool hasStr)
        {
            logger.DebugH(() => $"{deckeyList._keyString()}={CurrentStr}, shiftOffset={shiftOffset}, hasStr={hasStr}");
            var comboKeyList = deckeyList.Select(x => makeShiftedDecKey(x, shiftOffset)).ToList();      // 先頭キーのオフセットに合わせる
            keyComboPool?.AddComboShiftKey(comboKeyList[0], shiftKeyKind); // 元の拡張シフトキーコードに戻して、同時打鍵キーとして登録
            keyComboPool?.AddEntry(deckeyList, comboKeyList, shiftKeyKind, hasStr);
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
        public ArrowParser(Node treeNode, CStrokeList strkList, int arrowIdx)
            : base(treeNode, strkList)
        {
            UnhandledArrowIndex = arrowIdx;
        }

        // 矢印記法(-\d+(,\d+)*>)の直後を解析して第1打鍵位置に従って配置する
        public void Parse()
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, depth={Depth}, UnhandledArrowIndex={UnhandledArrowIndex}");
            //int arrowIdx = arrowIndex;
            bool bComboBlockerFound = false;
            if (PeekNextChar() == '|') {
                // '>|' という形式だった
                AdvanceCharPos(1);
                bComboBlockerFound = true;
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
                    AddTreeNode(UnhandledArrowIndex, bComboBlockerFound)?.ParseNodeBlock();
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
            return new ArrowParser(node, strkList, arrowIdx);
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
        public ArrowBundleParser(CStrokeList strkList, int lastStk, int nextArrowIdx)
            : base(RootTableNode, strkList, lastStk)
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

        public RewriteParser(Node treeNode, CStrokeList strkList, int arrowIdx)
            : base(treeNode, strkList, arrowIdx)
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
                logger.Warn($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return mergedNode;
        }

        /// <summary>書き換え情報を格納する node を RootTable の idx 位置に upsert する。upsert に成功したら upsert先の RewriteNode を返す</summary>
        /// <param name="idx"></param>
        /// <param name="node"></param>
        protected Node upsertNodeToRootTable(int idx, Node node)
        {
            int rewriteIdx = ShiftDecKey(idx);  // 矢印記法でないルートブロックの場合は、まだShiftされていないので、ここで Shift する必要あり
            var rewriteNode = GetNthRootNode(rewriteIdx);
            if (rewriteNode != null) {
                // TODO: node.IsTreeNode() だったらどうする?
                rewriteNode.AddRewriteMap();
            } else {
                rewriteNode = SetOrMergeNthSubNode(rewriteIdx, Node.MakeRewriteNode());
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
        public PreRewriteParser(Node treeNode, CStrokeList strkList, string targetStr)
            : base(treeNode, strkList, -1)
        {
            this.targetStr = targetStr;
        }

        // UnhandledArrowIndex の処理をした後、ネストされた新しいパーザを作成
        private PreRewriteParser makeNestedParser(int unhandledIdx = -1)
        {
            return new PreRewriteParser(TreeNode, StrokeList, myTargetString(UnhandledArrowIndex)) {
                bNested = true,
                UnhandledArrowIndex = unhandledIdx
            };
        }

        // ブロック処理(ネストされたパーザから呼ばれた場合は、通常のTableParserにする)
        protected override TableParser AddTreeNode(int idx, bool _ = false)
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
                if (upsertNodeToRootTable(idx, treeNode) != null) {
                    // 通常のTableParserを返す
                    return new TableParser(treeNode, strkList);
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
                //upsertNodeToRootTable(idx, Node.MakeRewriteNode(CurrentStr, bBare));
                upsertNodeToRootTable(idx, Node.MakeStringNode(CurrentStr, bBare));
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
        public PostRewriteParser(Node rewriteNode, CStrokeList strkList, int arrowIdx, string targetStr = null)
            : base(rewriteNode, strkList, arrowIdx)
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
        protected override TableParser AddTreeNode(int idx, bool _ = false)
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
                addCombinationKey(strkList, true);
            }
            // 同時打鍵の場合は、TreeNodeは RootNodeの子ノードになっているはず。
            // 逆に同時打鍵でない場合は、 TreeNode == RootNode のはず
            Node node = mergeNode(TreeNode, idx, Node.MakeRewriteTreeNode(myStr));
            if (node != null) {
                return new PostRewriteParser(node, strkList, -1, targetStr) { bNested = true };
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
            ArrowParser parser = this;
            int prevIndex = UnhandledArrowIndex;
            UnhandledArrowIndex = arrowIdx;
            if (bNested) {
                prefixStr = myPrefixString(prevIndex);
            } else {
                if (IsInCombinationBlock) {
                    // 同時打鍵の場合はTreeNodeを挿入しておく必要がある
                    var strkList = StrokeList.WithLastStrokeAdded(prevIndex);
                    Node node = SetOrMergeNthSubNode(prevIndex, Node.MakeTreeNode());
                    parser = new PostRewriteParser(node, strkList, arrowIdx, targetStr);
                } else {
                    targetStr = myTargetString(prevIndex);
                }
            }
            return parser;
        }

        public override void AddStringNode(int idx, bool bBare)
        {
            logger.DebugH(() => $"ENTER: bBare={bBare}, str={CurrentStr}, idx={idx}");
            if (!TreeNode.IsRewriteNode()) {
                // &X,..>の直後に文字列がきた
                ParseError("後置書き換え記法には、ブロックが必要です。");
            } else {
                var tgtStr = prefixStr + GetNthRootNodeString(idx)._toSafe() + targetStr._toSafe();
                TreeNode.UpsertRewritePair(tgtStr, Node.MakeStringNode(CurrentStr, bBare));
            }
            logger.DebugH("LEAVE");
        }

        // 書き換え文字列のペア
        protected override void AddStringPairNode()
        {
            var str1 = StringPair._getNth(0);
            var str2 = StringPair._getNth(1);
            logger.DebugH(() => $"ENTER: str1={str1.DebugString()}, str2={str2.DebugString()}");
            if (str1._isEmpty() || str2._isEmpty()) {
                ParseError("不正な書き換え文字列ペア");
            } else {
                var tgtStr = str1.GetSafeString() + targetStr._toSafe();
                TreeNode.UpsertRewritePair(tgtStr, Node.MakeStringNode(str2));
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

        // グローバルなルートパーザか
        public override bool IsRootParser => true;

        bool isKanchokuModeParser = true;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public RootTableParser(bool bForKanchoku)
            : base(RootTableNode, null)
        {
            isKanchokuModeParser = bForKanchoku;
        }

        /// <summary>
        /// トップレベルのテーブル定義を解析してストローク木を構築する。<br/>
        /// 前置・後置の書き換えモードは、このトップレベルでしか記述できない。
        /// </summary>
        public void ParseRootTable()
        {
            logger.InfoH($"ENTER");

            // トップレベルの解析(ArrowIndex はすでに Shiftされている)
            readNextToken(true);
            while (Context.currentToken != TOKEN.END) {
                switch (Context.currentToken) {
                    case TOKEN.LBRACE:
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

            // 部分キーに対して、非終端マークをセット
            keyComboPool?.SetNonTerminalMarkForSubkeys(!isKanchokuModeParser);
            if (Logger.IsInfoHEnabled && logger.IsInfoHPromoted) {
                keyComboPool?.DebugPrint();
            }

            if (Context.bRewriteEnabled) {
                // 書き換えノードがあったら、SandSの疑似同時打鍵サポートをOFFにしておく
                Settings.SandSEnablePostShift = false;
            }

            // 全ノードの情報を OutputLines に書き出す
            RootTableNode.OutputLine(OutputLines);

            if (isKanchokuModeParser) {
                // 漢直モードの場合、ルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる
                addMyCharFunctionInRootStrokeTable();
            }

            logger.InfoH($"LEAVE: KeyCombinationPool.Count={keyComboPool?.Count}");
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
                    if (RootTableNode.GetNthSubNode(dk) == null &&
                        RootTableNode.GetNthSubNode(dk + comboDeckeyStart) != null) {
                        // 単打設定が存在せず、同時打鍵の先頭キーになっている場合は、単打設定を追加する
                        AddCombinationKeyCombo(Helper.MakeList(dk), 0, true);  // 単打指定
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
                if (RootTableNode.GetNthSubNode(idx) == null) {
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
        public void ParseTableFile(string filename, string outFilename, KeyCombinationPool poolK, KeyCombinationPool poolA, bool primary, bool bTest = false)
        {
            logger.InfoH($"ENTER: filename={filename}");

            List<string> outputLines = new List<string>();

            void parseRootTable(TableLines tblLines, KeyCombinationPool pool, bool bKanchoku)
            {
                ParserContext.CreateSingleton(tblLines, pool, DecoderKeys.GetComboDeckeyStart(bKanchoku));
                var parser = new RootTableParser(bKanchoku);
                parser.ParseRootTable();
                //writeAllLines(outFilename, ParserContext.Singleton.OutputLines);
                outputLines.AddRange(ParserContext.Singleton.OutputLines);
                writeAllLines($"tmp/parsedTableFile{(bKanchoku ? 'K' : 'A')}{(primary ? 1 : 2)}.txt", ParserContext.Singleton.tableLines.GetLines());
                ParserContext.FinalizeSingleton();
            }

            string errorMsg = "";

            // 漢直モードの解析
            TableLines tableLines = new TableLines();
            tableLines.ReadAllLines(filename, primary, true);
            if (tableLines.NotEmpty) {
                parseRootTable(tableLines, poolK, true);
                errorMsg = tableLines.getErrorMessage();
                // 英数モードの解析
                tableLines = new TableLines();
                tableLines.ReadAllLines(filename, primary, false);
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

            TableLines tableLines = new TableLines();
            tableLines.ReadAllLines(filename, primary, true);

            if (tableLines.NotEmpty) {
                ParserContext.CreateSingleton(tableLines, null, DecoderKeys.GetComboDeckeyStart(true));
                var parser = new RootTableParser(true);
                parser.ParseDirectives();
                ParserContext.FinalizeSingleton();
            } else {
                //tableLines.Error($"テーブルファイル({filename})が開けません");
                //tableLines.showErrorMessage();
            }

            //tableLines.showErrorMessage();

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
