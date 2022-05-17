using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;


namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    using ShiftKeyKind = ComboShiftKeyPool.ComboKind;

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

        public Node()
        {
        }

        public Node(NodeType nodeType, string str)
        {
            this.nodeType = nodeType;
            this.str = str;
        }

        public virtual Node getNth(int n) { return null; }

        public string getString() { return str; }

        public bool isFunctionNode() { return nodeType == NodeType.Function; }

        public bool isStringNode() { return nodeType == NodeType.String; }

        public bool isStrokeTree() { return nodeType == NodeType.StrokeTree; }

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
            children = Helper.MakeList(new Node[(bRoot ? DecoderKeys.TOTAL_DECKEY_NUM : DecoderKeys.PLANE_DECKEY_NUM)]);
        }

        // n番目の子ノードを返す
        public override Node getNth(int n)
        {
            return children._getNth(n);
        }

        // n番目の子ノードをセットする
        public void setNthChild(int n, Node node)
        {
            if (n < children.Count) {
                children[n] = node;
            }
        }

    }

    class StringNode : Node
    {
        public StringNode(string str) : base(NodeType.String, str)
        {
        }
    }

    class FunctionNode : Node
    {
        public FunctionNode(string str) : base(NodeType.Function, str)
        {
        }
    }

    // include/load ブロック情報のスタック
    class BlockInfoStack
    {
        //private static Logger logger = Logger.GetLogger(true);
        private static Logger logger = Logger.GetLogger();

        struct BlockInfo
        {
            public string DirPath;        // インクルードする場合の起動ディレクトリ
            public string BlockName;      // ファイル名やブロック名
            public int OrigLineNumber;  // ブロックの開始行番号(0起点)
            public int CurrentOffset;   // 当ブロック内での行番号を算出するための、真の起点から現在行におけるオフセット行数

            public BlockInfo(string dirPath, string name, int lineNum, int off)
            {
                DirPath = dirPath;
                BlockName = name;
                OrigLineNumber = lineNum;
                CurrentOffset = off;
            }
        }

        List<BlockInfo> blockInfoList = new List<BlockInfo>();

        public string CurrentDirPath {
            get {
                var path = blockInfoList._isEmpty() ? "(empty)" : blockInfoList.Last().DirPath;
                logger.DebugH(() => $"PATH: {path}");
                return path;
            }
        }

        public string CurrentBlockName {
            get {
                var name = blockInfoList._isEmpty() ? "" : blockInfoList.Last().BlockName;
                logger.DebugH(() => $"NAME: {name}");
                return name;
            }
        }

        public int CurrentOffset {
            get {
                int offset = blockInfoList._isEmpty() ? 0 : blockInfoList.Last().CurrentOffset;
                logger.DebugH(() => $"OFFSET: {offset}");
                return offset;
            }
        }

        public int CalcCurrentLineNumber(int lineNum)
        {
            return lineNum - CurrentOffset;
        }

        public void Push(string dirPath, string name, int lineNum)
        {
            blockInfoList.Add(new BlockInfo(dirPath, name, lineNum, lineNum));
        }

        public void Pop(int nextLineNum)
        {
            var lastInfo = blockInfoList.Last();
            logger.DebugH(() => $"PUSH ENTER: nextLineNum={nextLineNum}, dirPath={lastInfo.DirPath}, blockName={lastInfo.BlockName}, origLine={lastInfo.OrigLineNumber}, offset={lastInfo.CurrentOffset}");
            int insertedTotalLineNum = nextLineNum - lastInfo.OrigLineNumber;
            blockInfoList._safePopBack();
            if (!blockInfoList._isEmpty()) {
                lastInfo.CurrentOffset += insertedTotalLineNum;
                logger.DebugH(() => $"PUSH LEAVE: dirPath={lastInfo.DirPath}, blockName={lastInfo.BlockName}, origLine={lastInfo.OrigLineNumber}, offset={lastInfo.CurrentOffset}");
            }
        }

        public bool Find(string name)
        {
            return blockInfoList.Any(x => x.BlockName == name);
        }
    }

    class TableFileParser
    {
        //private static Logger logger = Logger.GetLogger(true);
        private static Logger logger = Logger.GetLogger();

        private List<string> tableLines;

        // トークンの種類
        enum TOKEN {
            IGNORE,
            END,
            LBRACE,         // {
            RBRACE,         // }
            COMMA,          // ,
            STRING,         // "str"
            FUNCTION,       // @?
            SLASH,          // /
            ARROW,          // -n>
            ARROW_BUNDLE,   // -*>-n>
        };

        TOKEN currentToken = TOKEN.IGNORE;  // 最後に読んだトークン
        string currentStr;                  // 文字列トークン
        int arrowIndex = -1;                // ARROWインデックス
        int lineNumber = 0;                 // 今読んでる行数

        string currentLine;                 // 現在解析中の行
        int nextPos = 0;                    // 次の文字位置
        char currentChar = '\0';            // 次の文字

        // 同時打鍵定義ブロックの中か
        bool isInCombinationBlock => shiftKeyKind != ShiftKeyKind.None;

        // 同時打鍵によるシフト種別
        ShiftKeyKind shiftKeyKind = ShiftKeyKind.None;

        // 打鍵列
        List<int> strokeList = new List<int>();

        int depth => strokeList.Count;

        // 定義列マップ
        Dictionary<string, List<string>> linesMap = new Dictionary<string, List<string>>();

        // シフト面 -- 0:シフト無し、1:通常シフト、2:ShiftA, 3:ShiftB, ...
        int shiftPlane = 0;

        // 対象となる KeyComboPool
        KeyCombinationPool keyComboPool;

        // ブロック情報のスタック
        BlockInfoStack blockInfoStack = new BlockInfoStack();

        // 出力用のバッファ
        List<string> outputLines = new List<string>();

        StrokeTableNode rootTableNode = new StrokeTableNode(true);

        //StrokeTableNode getLastTreeNode(bool bPrev)
        //{
        //    int dep = depth;
        //    if (bPrev) --dep;
        //    if (dep<= 0) return null;
        //    var node = rootTableNode;
        //    for (int i = 0; i < dep; ++i) {
        //        var nd = node.getNth(strokeList[i]);
        //        if (!nd.isStrokeTree()) return null;
        //        node = (StrokeTableNode)nd;
        //    }
        //    return node;
        //}

        //StrokeTableNode getOrNewLastTreeNode()
        //{
        //    var node = rootTableNode;
        //    for (int i = 0; i < strokeList.Count; ++i) {
        //        var nd = node.getNth(strokeList[i]);
        //        if (nd != null && nd.isStrokeTree()) {
        //            node = (StrokeTableNode)nd;
        //        } else {
        //            var _pn = new StrokeTableNode();
        //            node.setNthChild(i, _pn);
        //            node = _pn;
        //        }
        //    }
        //    return node;
        //}

        //Node getNode(int idx, int prevIdx = -1)
        //{
        //    if (prevIdx < 0)
        //        return getLastTreeNode(false)?.getNth(idx);
        //    else {
        //        var nd = getLastTreeNode(true)?.getNth(prevIdx);
        //        if (nd != null && nd.isStrokeTree()) return nd.getNth(idx);
        //        return null;
        //    }
        //}

        void setNodeAtLast(List<int> stkList, Node node)
        {
            logger.DebugH(() => $"CALLED: stkList={stkList._keyString()}, {node.DebugString()}");
            bool bOverwritten = false;
            if (stkList._isEmpty()) {
                logger.Warn($"strokeList is empty");
            } else {
                var pn = rootTableNode;
                for (int i = 0; i < stkList.Count - 1; ++i) {
                    int idx = stkList[i];
                    var nd = pn.getNth(idx);
                    if (nd != null && nd.isStrokeTree()) {
                        pn = (StrokeTableNode)nd;
                    } else {
                        bOverwritten = bOverwritten || nd != null && !nd.isFunctionNode();
                        var _pn = new StrokeTableNode();
                        pn.setNthChild(idx, _pn);
                        pn = _pn;
                    }
                }
                if (pn == null) {
                    logger.Warn($"No such node: strokeList={stkList._keyString()}");
                } else {
                    bOverwritten = bOverwritten || !(pn.getNth(stkList.Last())?.isFunctionNode() ?? true);
                    pn.setNthChild(stkList.Last(), node);
                }
            }
            if (bOverwritten && isInCombinationBlock && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: {currentLine}");
                nodeDuplicateWarning();
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableFileParser(KeyCombinationPool pool)
        {
            keyComboPool = pool;
            blockInfoStack.Push(KanchokuIni.Singleton.KanchokuDir, "", 0);
        }

        /// <summary>
        /// テーブル定義を解析してストローク木を構築する。
        /// 解析結果を矢印記法に変換して出力ファイル(outFile)に書き込む
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="outFilename"></param>
        public void ParseTable(string filename, string outFilename)
        {
            logger.InfoH($"ENTER: filename={filename}");
            tableLines = readAllLines(filename, false);

            if (tableLines._notEmpty()) {
                currentLine = tableLines[0];
                readNextToken();
                while (currentToken != TOKEN.END) {
                    switch (currentToken) {
                        case TOKEN.LBRACE:
                            parseSubTree();
                            break;

                        case TOKEN.ARROW:
                            parseArrowNode(arrowIndex);
                            break;

                        case TOKEN.ARROW_BUNDLE:
                            parseArrowBundleNode(arrowIndex);
                            break;

                        case TOKEN.IGNORE:
                            break;

                        default:
                            parseError();
                            break;
                    }
                    readNextToken();
                }
            } else {
                Error($"テーブルファイル({filename})が開けません");
            }

            keyComboPool.SetNonTerminalMarkForSubkeys();
            if (Logger.IsInfoHEnabled && logger.IsInfoHPromoted) {
                keyComboPool.DebugPrint();
            }

            writeAllLines(outFilename, outputLines);

            showErrorMessage();

            logger.InfoH($"LEAVE: KeyCombinationPool.Count={keyComboPool.Count}");
        }

        void parseSubTree()
        {
            logger.DebugH(() => $"ENTER: currentLine={lineNumber}, strokeList={strokeList._keyString()}");
            bool bError = false;
            int idx = 0;
            bool isPrevDelim = true;
            readNextToken();
            while (!bError && currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                    case TOKEN.LBRACE:
                        strokeList.Add(idx);
                        //getOrNewLastTreeNode();
                        parseSubTree();
                        strokeList._popBack();
                        ++idx;
                        isPrevDelim = false;
                        break;

                    case TOKEN.ARROW:
                        parseArrowNode(arrowIndex);
                        isPrevDelim = false;
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        parseArrowBundleNode(arrowIndex);
                        break;

                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        parseNode(currentToken, idx);
                        ++idx;
                        isPrevDelim = false;
                        break;

                    case TOKEN.COMMA:              // 次のトークン待ち
                    case TOKEN.SLASH:              // 次のトークン待ち
                        if (isPrevDelim) ++idx;
                        isPrevDelim = true;
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:                        // 途中でファイルが終わったりした場合 : エラー
                        parseError();
                        bError = true;
                        break;
                }

                readNextToken();
            }

            logger.DebugH(() => $"LEAVE: currentLine={lineNumber}, depth={depth}, bError={bError}");
        }

        void parseArrowNode(int idx) {
            strokeList.Add(idx);
            logger.DebugH(() => $"ENTER: currentLine={lineNumber}, depth={depth}, idx={idx}");
            readNextToken();
            var tokenNextToArrow = currentToken;
            if (currentToken == TOKEN.ARROW) {
                parseArrowNode(arrowIndex);
            } else if (currentToken == TOKEN.LBRACE) {
                parseSubTree();
            } else {
                parseNode(currentToken, -1);
            }
            currentToken = TOKEN.IGNORE;    // いったん末端ノードの処理をしたら、矢印記法を抜けるまで無視
            logger.DebugH(() => $"LEAVE: currentLine={lineNumber}, depth={depth}");
            strokeList._popBack();
            return;
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        void parseArrowBundleNode(int nextArrowIdx)
        {
            logger.DebugH(() => $"ENTER: depth={depth}, nextArrowIdx={nextArrowIdx}");

            int n = 0;
            bool isPrevDelim = true;
            readNextToken();
            if (currentToken != TOKEN.LBRACE) { // 直後は '{' でブロックの始まりである必要がある
                parseError();
                return;
            }
            readNextToken();
            while (currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                    //case TOKEN.ARROW:
                    //    parseArrowNode(0, nextArrowIdx);
                    //    isPrevDelim = false;
                    //    break;

                    //case TOKEN.LBRACE:
                    //    strokeList.Add(n);
                    //    strokeList.Add(nextArrowIdx);
                    //    parseNode(currentToken, n);
                    //    strokeList._popBack();
                    //    strokeList._popBack();
                    //    ++n;
                    //    isPrevDelim = false;
                    //    break;

                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        parseNode(currentToken, nextArrowIdx, n);
                        ++n;
                        isPrevDelim = false;
                        break;

                    case TOKEN.COMMA:              // 次のトークン待ち
                    case TOKEN.SLASH:              // 次のトークン待ち
                        if (isPrevDelim) ++n;
                        isPrevDelim = true;
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:                        // 途中でファイルが終わったりした場合 : エラー
                        parseError();
                        break;
                }

                readNextToken();
            }

            logger.DebugH(() => $"LEAVE: depth={depth}");
        }

        void parseNode(TOKEN token, int nth, int prevNth = -1)
        {
            logger.DebugH(() => $"ENTER: token={token}, depth={depth}, prevNth={prevNth}, nth={nth}");
            switch (token) {
                //case TOKEN.LBRACE:
                //    parseSubTree(getOrNewLastTreeNode(), nth);
                //    logger.DebugH(() => $"LEAVE: depth={depth}");
                //    break;

                case TOKEN.STRING:            // "str" : 文字列ノード
                    // 終端ノードの追加と同時打鍵列の組合せの登録
                    addTerminalNode(new StringNode($"\"{currentStr._safeReplace(@"\", @"\\")._safeReplace(@"""", @"\""")}\""), prevNth, nth);
                    if (depth == 0 && nth >= 0 && currentStr._startsWith("!{")) {
                        logger.DebugH(() => $"REPEATABLE");
                        keyComboPool.AddRepeatableKey(nth);
                    }
                    logger.DebugH(() => $"LEAVE: depth={depth}");
                    break;

                case TOKEN.FUNCTION:          // @c : 機能ノード
                    // 終端ノードの追加と同時打鍵列の組合せの登録
                    addTerminalNode(new FunctionNode("@" + currentStr), prevNth, nth);
                    logger.DebugH(() => $"LEAVE: depth={depth}");
                    break;

                case TOKEN.RBRACE:
                case TOKEN.COMMA:             // ',' が来たら次のトークン
                case TOKEN.SLASH:             // '/' が来ても次のトークン
                    logger.DebugH(() => $"LEAVE: depth={depth}");
                    break;

                case TOKEN.IGNORE:
                    break;

                default:                // 途中でファイルが終わったりした場合 : エラー
                    parseError();
                    break;
            }
        }

        /// <summary>
        /// 終端ノードの追加と同時打鍵列の組合せの登録<br/>
        /// 同時打鍵の場合は、ブロックのルートキーをCOMBO_DECKEY_STARTまでシフトする
        /// </summary>
        /// <param name="prevNth"></param>
        /// <param name="lastNth"></param>
        /// <param name="outStr"></param>
        void addTerminalNode(Node node, int prevNth, int lastNth)
        {
            var list = new List<int>(strokeList);
            if (prevNth >= 0) list.Add(prevNth);
            if (lastNth >= 0) list.Add(lastNth);
            if (list._isEmpty()) return;

            int shiftOffset = calcShiftOffset(list[0]);

            // 先頭キーはシフト化
            if (isInCombinationBlock) {
                list[0] = makeComboDecKey(list[0]);
            } else if (list[0] < DecoderKeys.PLANE_DECKEY_NUM) {
                list[0] += shiftOffset;
            }
            // 残りは Modulo
            for (int i = 1; i < list.Count; ++i) {
                list[i] %= DecoderKeys.PLANE_DECKEY_NUM;
            }

            outputLines.Add($"-{list.Select(x => x.ToString())._join(">-")}>{node.getString()}");

            if (isInCombinationBlock) {
                makeCombinationKeyCombo(list, shiftOffset);
            }

            setNodeAtLast(list, node);
        }

        int calcShiftOffset(int deckey)
        {
            return (deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey / DecoderKeys.PLANE_DECKEY_NUM : shiftPlane) * DecoderKeys.PLANE_DECKEY_NUM;
        }

        // 同時打鍵列の組合せを作成して登録しておく
        void makeCombinationKeyCombo(List<int> deckeyList, int shiftOffset)
        {
            var comboKeyList = deckeyList.Select(x => makeShiftedDecKey(x, shiftOffset)).ToList();      // 先頭キーのオフセットに合わせる
            logger.DebugH(() => $"{deckeyList._keyString()}={currentStr}");
            keyComboPool.AddComboShiftKey(comboKeyList[0], shiftKeyKind); // 元の拡張シフトキーコードに戻して、同時打鍵キーとして登録
            keyComboPool.AddEntry(deckeyList, comboKeyList, shiftKeyKind);
        }

        int makeComboDecKey(int decKey)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + DecoderKeys.COMBO_DECKEY_START;
        }

        int makeShiftedDecKey(int decKey, int shiftOffset)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + shiftOffset;
        }

        void setNthChildNode(StrokeTableNode parentNode, int n, Node childNode) {
            if (parentNode != null && childNode != null) {
                if (!isInCombinationBlock) {
                    // 同時打鍵ブロック以外ならば上書きOK
                    parentNode.setNthChild(n, childNode);
                } else {
                    // 同時打鍵ブロックの場合
                    Node p = parentNode.getNth(n);
                    if (p == null || p.isFunctionNode()) {
                        // 未割り当て、または機能ノードならば上書きOK
                        parentNode.setNthChild(n, childNode);
                    } else if (childNode.isFunctionNode()) {
                        // 重複していて、子ノードが機能ノードなら無視
                    } else {
                        // 重複していて、親ノードも子ノードも機能ノード以外なら警告
                        if (!bIgnoreWarningOverwrite) {
                            logger.Warn($"DUPLICATED: {currentLine}");
                            nodeDuplicateWarning();
                        }
                    }
                }
            }
        }

        // 現在のトークンをチェックする
        bool isCurrentToken(TOKEN target) {
            return (currentToken == target);
        }

        // 現在のトークンをチェックする
        void checkCurrentToken(TOKEN target) {
            if (currentToken != target) {
                parseError();           // 違ったらエラー
            }
        }

        // トークンひとつ読んで currentToken にセット
        void readNextToken() {
            currentToken = getToken();
        }

        bool bIgnoreWarningAll = false;
        bool bIgnoreWarningBraceLevel = false;
        bool bIgnoreWarningOverwrite = false;
        int braceLevel = 0;

        // トークンを読む
        TOKEN getToken()
        {
            currentStr = "";
            arrowIndex = -1;
            while (true) {
                getNextChar();

                if (currentChar == '\0') {
                    // ファイルの終わり
                    return TOKEN.END;
                }

                if (currentChar == '#') {
                    // Directive: '#include', '#define', '#strokePosition', '#*shift*', '#combination', '#overlapping', '#yomiConvert', '#store', '#load', '#end' または '#' 以降、行末までコメント
                    readWord();
                    var lcStr = currentStr._toLower();
                    if (lcStr == "include") {
                        includeFile();
                    } else if (lcStr == "define") {
                        outputLines.Add(currentLine);
                    } else if (lcStr == "store") {
                        storeLineBlock();
                    } else if (lcStr == "load") {
                        loadLineBlock();
                    } else if (lcStr._startsWith("yomiconv")) {
                        outputLines.Add(currentLine);
                    } else if (lcStr == "strokePosition") {
                        outputLines.Add(currentLine);
                    } else if (lcStr == "noshift" || lcStr == "normal") {
                        shiftPlane = 0;
                    } else if (lcStr == "shift") {
                        shiftPlane = 1;
                    } else if (lcStr == "shifta") {
                        shiftPlane = 2;
                    } else if (lcStr == "shiftb") {
                        shiftPlane = 3;
                    } else if (lcStr == "shiftc") {
                        shiftPlane = 4;
                    } else if (lcStr == "shiftd") {
                        shiftPlane = 5;
                    } else if (lcStr == "shifte") {
                        shiftPlane = 6;
                    } else if (lcStr == "shiftf") {
                        shiftPlane = 7;
                    } else if (lcStr == "combination" || lcStr == "overlapping") {
                        readWord();
                        switch (currentStr._toLower()) {
                            case "prefix":
                            case "preshift":
                                shiftKeyKind = ShiftKeyKind.PrefixSuccessiveShift;
                                break;
                            case "oneshot":
                                shiftKeyKind = ShiftKeyKind.UnorderedOneshotShift;
                                break;
                            case "successive":
                            case "unordered":
                            case "mutual":
                                shiftKeyKind = ShiftKeyKind.UnorderedSuccessiveShift;
                                break;
                            default:
                                argumentError(currentStr);
                                break;
                        }
                    } else if (lcStr == "end") {
                        readWord();
                        switch (currentStr._toLower()) {
                            case "combination":
                            case "overlapping":
                                shiftKeyKind = ShiftKeyKind.None;
                                break;
                            case "shift":
                                shiftPlane = 0;
                                break;
                            case "__include__":
                                logger.DebugH(() => $"END INCLUDE/LOAD: lineNumber={lineNumber}");
                                blockInfoStack.Pop(lineNumber + 1);
                                break;
                        }
                    } else if (lcStr == "sands") {
                        handleSandSState();
                    } else if (lcStr == "assignplane") {
                        assignShiftPlane();
                    } else if (lcStr == "set") {
                        handleSettings();
                    } else if (lcStr == "ignorewarning") {
                        readWord();
                        var word = currentStr._toLower();
                        if (word._isEmpty() || word == "all") {
                            bIgnoreWarningAll = true;
                            bIgnoreWarningBraceLevel = true;
                            bIgnoreWarningOverwrite = true;
                        } else if (word == "bracelevel") {
                            bIgnoreWarningBraceLevel = true;
                        } else if (word == "overwrite") {
                            bIgnoreWarningOverwrite = true;
                        }
                    } else {
                        logger.DebugH(() => $"#{currentStr}");
                    }
                    currentStr = "";
                    skipToEndOfLine();
                    continue;
                }

                switch (currentChar) {
                    case ';':
                        // ';' 以降、行末までコメント
                        skipToEndOfLine();
                        break;

                    case '{':
                        if (!bIgnoreWarningBraceLevel && !bIgnoreWarningAll && nextPos == 1 && braceLevel > 0) unexpectedLeftBraceAtColumn0Warning();
                        ++braceLevel;
                        return TOKEN.LBRACE;

                    case '}':
                        if (!bIgnoreWarningBraceLevel && !bIgnoreWarningAll && nextPos == 1 && braceLevel > 1) unexpectedRightBraceAtColumn0Warning();
                        --braceLevel;
                        return TOKEN.RBRACE;

                    case ',': return TOKEN.COMMA;
                    case '|': return TOKEN.COMMA;

                    case '/':
                        if (peekNextChar() == '/') {
                            // 2重スラッシュはコメント扱い
                            skipToEndOfLine();
                            break;
                        }
                        return TOKEN.SLASH;

                    case '\n':
                    case ' ':                   // SPC : スキップ
                    case '\t':                  // TAB : スキップ
                    case '\r':                  // ^M  : スキップ
                    case '\f':                  // ^L  : スキップ
                        break;

                    case '@':
                        // 機能
                        readMarker();
                        return TOKEN.FUNCTION;

                    case '"':
                        // 文字列
                        readString();
                        return TOKEN.STRING;

                    case '-': {
                        char c = getNextChar();
                        if (c == '*') {
                            // 矢印束記法
                            if (parseArrowBundle()) return TOKEN.ARROW_BUNDLE;
                        } else {
                            // 矢印記法
                            if (parseArrow(c)) return TOKEN.ARROW;
                        }
                    }
                    break;

                    case '\0':
                        // ファイルの終わり
                        return TOKEN.END;

                    default:
                        readBareString(currentChar);
                        if (currentStr._notEmpty()) return TOKEN.STRING;

                        // エラー
                        parseError();
                        return TOKEN.END;
                }
            }
        }

        // ファイルをインクルードする
        void includeFile() {
            logger.DebugH("CALLED");
            readWordOrString();
            var filename = currentStr;
            logger.DebugH(() => $"INCLUDE: lineNum={lineNumber + 1}, {filename}");
            if (filename._notEmpty()) {
                var lines = readAllLines(filename, true);
                if (lines._isEmpty()) {
                    logger.Error($"Can't open: {filename}");
                    fileOpenError(filename);
                } else {
                    tableLines.InsertRange(lineNumber + 1, lines);
                    logger.DebugH(() => $"INCLUDE: {lines.Count} lines included");
                }
            } else {
                parseError("ファイル名が指定されていません。");
            }
        }

        // 名前を付けて、行ブロックを保存する
        void storeLineBlock()
        {
            readWord();
            var blockName = currentStr;
            logger.DebugH(() => $"CALLED: {blockName}");
            List<string> lines = null;
            if (blockName._isEmpty()) {
                parseError();
            } else {
                lines = new List<string>();
                linesMap[blockName] = lines;
                logger.DebugH(() => $"SET: lineNum={lineNumber + 1}, {blockName}");
            }
            while (getNextLine()) {
                if (currentLine._startsWith("#end")) {
                    lines.Add("#end __include__");
                    break;
                }
                if (lines != null) {
                    lines.Add(currentLine);
                }
            }
        }

        // 保存しておいた行ブロックをロードする
        void loadLineBlock()
        {
            readWord();
            var blockName = currentStr;
            logger.DebugH(() => $"CALLED: |{blockName}|");
            if (blockName._isEmpty()) {
                parseError();
            } else if (blockInfoStack.Find(blockName)) {
                loadLoopError(blockName);
            } else {
                var lines = linesMap._safeGet(blockName);
                if (lines._isEmpty()) {
                    logger.Error($"No stored lines for \"{blockName}\"");
                    noSuchBlockError(blockName);
                } else {
                    logger.DebugH(() => $"InsertRange: {blockName}, {lines.Count} lines");
                    int nextLineNum = lineNumber + 1;
                    tableLines.InsertRange(nextLineNum, lines);
                    blockInfoStack.Push("", blockName, nextLineNum);
                }
            }
        }

        void handleSandSState()
        {
            readWord();
            var word = currentStr._toLower();
            if (currentStr._startsWith("enable")) {
                Settings.SandSEnabled = true;
            } else if (currentStr._startsWith("disable")) {
                Settings.SandSEnabled = false;
            } else if (currentStr._startsWith("enabeoneshot")) {
                Settings.OneshotSandSEnabled = true;
            } else if (currentStr._startsWith("disabeoneshot")) {
                Settings.OneshotSandSEnabled = false;
            } else if (currentStr._startsWith("enabepostshift")) {
                Settings.SandSEnablePostShift = true;
            } else if (currentStr._startsWith("disabepostshift")) {
                Settings.SandSEnablePostShift = false;
            }
        }

        void assignShiftPlane()
        {
            readWord();
            if (currentStr._notEmpty()) {
                bool resultOK = VirtualKeys.AssignShiftPlane(currentStr);
                if (!resultOK) {
                    parseError();
                }
            }
        }

        /// <summary>
        /// Settings のプロパティに値を設定する
        /// </summary>
        void handleSettings()
        {
            logger.DebugH(() => $"CALLED: currentLine={currentLine}");
            readWord();
            var items = currentStr._split('=');
            logger.DebugH(() => $"currentStr={currentStr}, items.Length={items._safeLength()}, items[0]={items._getFirst()}, items[1]={items._getSecond()}");
            if (items._safeLength() == 2 && items[0]._notEmpty()) {
                var propName = items[0];
                var strVal = items[1]._strip();
                const int errorVal = -999999;
                int iVal = strVal._parseInt(errorVal, errorVal);
                if (iVal != errorVal) {
                    if (Settings.SetValueByName(propName, iVal)) return;
                } else if (strVal._toLower()._equalsTo("true")) {
                    if (Settings.SetValueByName(propName, true)) return;
                } else if (strVal._toLower()._equalsTo("false")) {
                    if (Settings.SetValueByName(propName, false)) return;
                } else {
                    if (Settings.SetValueByName(propName, strVal)) return;
                }
            }
            parseError();
        }

        // '"' が来るまで読みこんで、currentStr に格納。
        void readString() {
            // 「"」自身は「"\""」と表記することで指定できる。
            // 「\」自身は「"\\"」と表記する。
            // 「\」は、単に次の一文字をエスケープするだけで、
            // 「"\n"」「"\t"」「"\ooo"」は未対応。
            var sb = new StringBuilder();
            while (true) {
                char c = getNextChar();
                if (c == '\r' || c == '\n' || c == 0) {
                    parseError();
                }
                if (c == '"') {
                    // 文字列の終わり
                    break;
                }
                if (c == '\\') {
                    // 最初の「\」は、単に読みとばす
                    c = getNextChar();
                }
                sb.Append(c);
            }
            currentStr = sb.ToString();
        }

        // 何らかのデリミタが来るまで読みこんで、currentStr に格納。
        void readBareString(char c = '\0') {
            var sb = new StringBuilder();
            bool isOutputChar() { return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c >= 0x1000; }
            if (c != '\0') {
                if (!isOutputChar()) return;
                sb.Append(c);
            }
            while (true) {
                c = peekNextChar();
                if (!isOutputChar()) break;
                getNextChar();
                sb.Append(c);
            }
            currentStr = sb.ToString();
            logger.DebugH(() => $"LEAVE: {currentStr}");
        }

        // 空白またはカンマが来るまで読みこんで、currentStr に格納。
        void readMarker() {
            var sb = new StringBuilder();
            while (true) {
                char c = peekNextChar();
                if (c <= ' ' || c == ',') {
                    if (sb._isEmpty()) parseError();
                    currentStr = sb.ToString();
                    return;
                }
                getNextChar();
                sb.Append(c);
            }
        }

        // 行末までの範囲で次の空白文字またはコメント文字までを読み込んで、currentStr に格納。
        void readWord() {
            currentStr = "";
            if (nextPos >= currentLine._safeLength()) return;
            char c = skipSpace();
            if (c <= ' ') return;
            if (c == ';' || (c == '/' && peekNextChar() == '/')) {
                skipToEndOfLine();
                return;
            }
            readWordSub(c);
        }

        // 次の空白文字までを読み込んで、currentStr に格納。
        void readWordSub(char c) {
            var sb = new StringBuilder();
            sb.Append(c);
            while (true) {
                c = getNextChar();
                if (c <= ' ') {
                    currentStr = sb.ToString();
                    return;
                }
                sb.Append(c);
            }
        }

        // 文字列または単語を読み込む
        void readWordOrString()
        {
            currentStr = "";
            char c = skipSpace();
            if (c > ' ') {
                if (c == '"') {
                    readString();
                } else if (c == ';' || (c == '/' && peekNextChar() == '/')) {
                    skipToEndOfLine();
                } else {
                    readWordSub(c);
                }
            }
        }

        // 空白文字を読み飛ばす
        char skipSpace() {
            while (true) {
                char c = getNextChar();
                if (c == '\r' || c == '\n' || c == 0 || c > ' ')  return c;
            }
        }

        // ARROW: /-[SsXxPp]?[0-9]+>/
        bool parseArrow(char c) {
            int shiftOffset = -1;
            int funckeyOffset = 0;
            bool bShiftPlane = false;
            //char c = getNextChar();
            if (c == 'N' || c == 'n') {
                shiftOffset = 0;
                c = getNextChar();
            } else if (c == 'S' || c == 's' || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')) {
                shiftOffset = VirtualKeys.CalcShiftOffset(c);
                c = getNextChar();
            } else if (c == 'X' || c == 'x') {
                shiftOffset = 0;
                funckeyOffset = DecoderKeys.FUNC_DECKEY_START;
                c = getNextChar();
            } else if (c == 'P' || c == 'P') {
                bShiftPlane = true;
                c = getNextChar();
            }
            if (!is_numeral(c)) parseError();
            arrowIndex = c - '0';
            c = getNextChar();
            while (is_numeral(c)) {
                arrowIndex = arrowIndex * 10 + c - '0';
                c = getNextChar();
            }
            arrowIndex += funckeyOffset;
            arrowIndex %= DecoderKeys.PLANE_DECKEY_NUM;    // 後で Offset を足すので Modulo 化しておく
            if (!bShiftPlane) {
                //if (isInCombinationBlock) {
                //    // 同時打鍵ブロック用の Offset
                //    shiftOffset = DecoderKeys.COMBO_DECKEY_START;
                //} else
                if (shiftOffset < 0) {
                    // シフト面のルートノードで明示的にシフトプレフィックスがなければ、shiftOffset をセット
                    shiftOffset = (shiftPlane > 0 && depth == 0) ? shiftPlane * DecoderKeys.PLANE_DECKEY_NUM : 0;
                }
                arrowIndex += shiftOffset;
                if (arrowIndex >= DecoderKeys.COMBO_DECKEY_END) parseError();
            } else {
                shiftPlane = arrowIndex;
                if (shiftPlane >= DecoderKeys.ALL_PLANE_NUM) parseError();
                return false;
            }
            if (c != '>') parseError();
            logger.DebugH(() => $"depth={depth}, arrowIndex={arrowIndex}, shiftPlane={shiftPlane}, shiftOffset={shiftOffset}");
            return true;
        }

        // ARROW_BUNLE: -*>-nn>
        bool parseArrowBundle() {
            char c = getNextChar();
            if (c != '>') parseError();
            c = getNextChar();
            if (c != '-') parseError();
            c = getNextChar();
            if (!is_numeral(c)) parseError();
            arrowIndex = c - '0';
            c = getNextChar();
            while (is_numeral(c)) {
                arrowIndex = arrowIndex * 10 + c - '0';
                c = getNextChar();
            }
            if (arrowIndex >= DecoderKeys.PLANE_DECKEY_NUM) parseError();
            if (c != '>') parseError();
            return true;
        }

        bool is_numeral(char c)
        {
            return c >= '0' && c <= '9';
        }

        char getNextChar() {
            if (nextPos > currentLine._safeLength()) {
                ++lineNumber;
                if (lineNumber >= tableLines._safeCount()) {
                    return currentChar = '\0';
                }
                currentLine = tableLines[lineNumber];
                nextPos = 0;
            }
            if (nextPos < currentLine._safeLength()) {
                currentChar = currentLine[nextPos++];
            } else {
                ++nextPos;
                currentChar = '\n';
            }
            return currentChar;
        }

        char peekNextChar() {
            return (nextPos < currentLine._safeLength()) ? currentLine[nextPos] : '\0';
        }

        bool getNextLine() {
            ++lineNumber;
            if (lineNumber >= tableLines._safeCount()) {
                return false;
            }
            currentLine = tableLines[lineNumber];
            return true;
        }

        void skipToEndOfLine() {
            nextPos = currentLine._safeLength() + 1;
            currentChar = '\n';
        }

        List<string> readAllLines(string filename, bool bInclude)
        {
            var lines = new List<string>();
            if (filename._notEmpty()) {
                var includeFilePath = blockInfoStack.CurrentDirPath._joinPath(filename._canonicalPath());
                logger.DebugH(() => $"ENTER: includeFilePath={includeFilePath}");
                var contents = Helper.GetFileContent(includeFilePath, (e) => logger.Error(e._getErrorMsg()));
                if (contents._notEmpty()) {
                    lines.AddRange(contents._safeReplace("\r", "")._split('\n'));
                    int nextLineNum = lineNumber;
                    if (bInclude) {
                        lines.Add("#end __include__");
                        ++nextLineNum;
                    }
                    blockInfoStack.Push(includeFilePath._getDirPath(), filename, nextLineNum);
                }
            }
            logger.DebugH(() => $"LEAVE: num of lines={lines.Count}");
            return lines;
        }

        private void writeAllLines(string filename, List<string> lines)
        {
            var path = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
            Helper.CreateDirectory(Helper.GetDirectoryName(path));
            logger.InfoH($"ENTER: path={path}");
            try {
                using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite)) {
                    using (var sw = new System.IO.StreamWriter(fs)) {   // BOM無しで書き込む
                        sw.Write(lines._join("\n"));
                    }
                }
            } catch (Exception e) {
                logger.Error(e._getErrorMsg());
            }
            logger.InfoH($"LEAVE");
        }

        string blockOrFile() {
            return blockInfoStack.CurrentDirPath._isEmpty() ? "ブロック" : "テーブルファイル";
        }

        int calcErrorLineNumber() {
            return blockInfoStack.CalcCurrentLineNumber(lineNumber + 1);
        }

        int calcErrorColumn() {
            if (nextPos == 0 && lineNumber > 0) return tableLines[lineNumber - 1].Count();
            return nextPos;
        }

        // 解析エラー
        void parseError(string msg = null) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError($"{(msg._notEmpty() ? msg + "\r\n" : "")}{blockOrFile()} {blockInfoStack.CurrentBlockName} の {calcErrorLineNumber()}行 {calcErrorColumn()}文字目({errorChar()})がまちがっているようです：\r\n> {currentLine._safeSubstring(0, 50)} ...");
        }

        string errorChar()
        {
            switch (currentChar) {
                case '\n':
                case '\r':
                    return "NL";
                case '\0':
                    return "NULL";
                default:
                    return $"'{currentChar}'";
            }
        }

        // 引数エラー
        void argumentError(string arg) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError($"引数 {arg} が不正です。\r\nテーブルファイル {blockInfoStack.CurrentBlockName} の {calcErrorLineNumber()}行目がまちがっているようです：\r\n> {currentLine._safeSubstring(0, 50)} ...");
        }

        // loadループエラー
        void loadLoopError(string name) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError($"ブロック {name} のロードがループしています。\r\n{blockOrFile()} {blockInfoStack.CurrentBlockName} の {calcErrorLineNumber()}行目がまちがっているようです：\r\n> {currentLine._safeSubstring(0, 50)} ...");
        }

        // storeブロックが存在しない
        void noSuchBlockError(string name) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError($"指定されたブロック {name} が存在しません。\r\n{blockOrFile()} {blockInfoStack.CurrentBlockName} の {calcErrorLineNumber()}行目がまちがっているようです：\r\n> {currentLine._safeSubstring(0, 50)} ...");
        }

        // ファイルの読み込みに失敗した場合
        void fileOpenError(string filename) {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError($"ファイル {filename} を読み込めません。\r\nテーブルファイル {blockInfoStack.CurrentBlockName} の {calcErrorLineNumber()}行目がまちがっているようです：\r\n> {currentLine._safeSubstring(0, 50)} ...");
        }

        // ノードの重複が発生した場合
        void nodeDuplicateWarning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning($"{blockOrFile()} {blockInfoStack.CurrentBlockName} の {calcErrorLineNumber()}行目でノードの重複が発生しました。意図したものであれば無視してください (#ignoreWarning overwrite を記述すると無視されます)：\r\n> {currentLine._safeSubstring(0, 50)} ...");
        }

        // カラム0で予期しないLBRACEが発生
        void unexpectedLeftBraceAtColumn0Warning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning($"{blockOrFile()} {blockInfoStack.CurrentBlockName} の {calcErrorLineNumber()}行目の行頭にネストされた '{{' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述すると無視されます)：\r\n> {currentLine._safeSubstring(0, 50)} ...");
        }

        // カラム0で予期しないRBRACEが発生
        void unexpectedRightBraceAtColumn0Warning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning($"{blockOrFile()} {blockInfoStack.CurrentBlockName} の {calcErrorLineNumber()}行目の行頭にまだネスト中の '}}' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述すると無視されます)：\r\n> {currentLine._safeSubstring(0, 50)} ...");
        }

        // エラー処理
        void handleError(string msg) {
            logger.Error(msg);
            logger.Error("lines=\n" + makeErrorLines());
            // エラーメッセージを投げる
            Error(msg);
        }

        // 警告ー処理
        void handleWarning(string msg) {
            logger.Warn(msg);
            logger.Warn("lines=\n" + makeErrorLines());
            // エラーメッセージを投げる
            Warn(msg);
        }

        string makeErrorLines() {
            var sb = new StringBuilder();
            sb.Append("lines=\n");
            for (int i = 9; i > 0; --i) {
                if (lineNumber >= i && tableLines._safeCount() > lineNumber - i) sb.Append(tableLines[lineNumber - i]).Append('\n');
            }
            sb.Append($">> {currentLine}\n");
            for (int i = 1; i < 10; ++i) {
                if (lineNumber + i < tableLines._safeCount()) sb.Append(tableLines[lineNumber + i]).Append('\n');
            }
            return sb.ToString();
        }

        private void showErrorMessage()
        {
            if (!errorMsg._isEmpty()) {
                SystemHelper.ShowWarningMessageBox(errorMsg.ToString());
            }
        }

        private StringBuilder errorMsg = new StringBuilder();

        private int errorLevel = 0;

        // エラー情報を格納
        void setErrorInfo(int level, string msg) {
            errorLevel = level;
            if (errorMsg.Length + msg.Length < 800) {
                if (level == Logger.LogLevelError || errorMsg.Length + msg.Length < 500) {
                    if (!errorMsg._isEmpty()) errorMsg.Append("\r\n\r\n");
                    errorMsg.Append(msg);
                }
            }
        }

        // エラー情報を格納し、例外を送出
        void Error(string msg) {
            setErrorInfo(Logger.LogLevelError, msg);
        }

        // 警告情報を格納するが、継続する
        void Warn(string msg) {
            setErrorInfo(Logger.LogLevelWarn, msg);
        }
    }
}
