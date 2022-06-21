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

        public virtual string getMarker() { return str; }

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
            if (n >= 0 && n < children.Count) {
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
        string marker;

        public FunctionNode(string str) : base(NodeType.Function, str)
        {
            marker = "@" + str;
        }

        public override string getMarker()
        {
            return marker;
        }
    }

    // include/load ブロック情報のスタック
    class BlockInfoStack
    {
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

    class TableLines
    {
        private static Logger logger = Logger.GetLogger();

        private List<string> tableLines = new List<string>();

        // ブロック情報のスタック
        BlockInfoStack blockInfoStack = new BlockInfoStack();

        // 定義列マップ
        Dictionary<string, List<string>> linesMap = new Dictionary<string, List<string>>();

        int lineNumber = 0;

        int nextPos = 0;

        public bool Empty => tableLines.Count == 0;

        public bool NotEmpty => tableLines.Count != 0;

        public string CurrentLine => tableLines[lineNumber];

        public int LineNumber => lineNumber + 1;

        public bool IsCurrentPosHeadOfLine => nextPos == 1;

        public char CurrentChar { get; private set; }

        public string currentStr;                  // 文字列トークン

        public TableLines()
        {
            blockInfoStack.Push(KanchokuIni.Singleton.KanchokuDir, "", 0);
        }

        private List<string> readAllLines(string filename, bool bInclude)
        {
            List<string> lines = new List<string>();
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

        public void ReadAllLines(string filename)
        {
            tableLines.InsertRange(0, readAllLines(filename, false));
        }

        // ファイルをインクルードする
        public void IncludeFile() {
            logger.DebugH("CALLED");
            var filename = ReadWordOrString();
            logger.DebugH(() => $"INCLUDE: lineNum={LineNumber}, {filename}");
            if (filename._notEmpty()) {
                var lines = readAllLines(filename, true);
                if (lines._isEmpty()) {
                    logger.Error($"Can't open: {filename}");
                    FileOpenError(filename);
                } else {
                    tableLines.InsertRange(lineNumber + 1, lines);
                    logger.DebugH(() => $"INCLUDE: {lines.Count} lines included");
                }
            } else {
                ParseError("ファイル名が指定されていません。");
            }
        }

        public void EndInclude()
        {
            blockInfoStack.Pop(LineNumber);
        }

        // 名前を付けて、行ブロックを保存する
        public void StoreLineBlock()
        {
            ReadWord();
            var blockName = currentStr;
            logger.DebugH(() => $"CALLED: {blockName}");
            List<string> lines = null;
            if (blockName._isEmpty()) {
                ParseError();
            } else {
                lines = new List<string>();
                linesMap[blockName] = lines;
                logger.DebugH(() => $"SET: lineNum={LineNumber}, {blockName}");
            }
            while (GetNextLine()) {
                if (CurrentLine._startsWith("#end")) {
                    lines.Add("#end __include__");
                    break;
                }
                if (lines != null) {
                    lines.Add(CurrentLine);
                }
            }
        }

        // 保存しておいた行ブロックをロードする
        public void LoadLineBlock()
        {
            var blockName = ReadWord();
            logger.DebugH(() => $"CALLED: |{blockName}|");
            if (blockName._isEmpty()) {
                ParseError();
            } else if (blockInfoStack.Find(blockName)) {
                LoadLoopError(blockName);
            } else {
                var lines = linesMap._safeGet(blockName);
                if (lines._isEmpty()) {
                    logger.Error($"No stored lines for \"{blockName}\"");
                    NoSuchBlockError(blockName);
                } else {
                    logger.DebugH(() => $"InsertRange: {blockName}, {lines.Count} lines");
                    tableLines.InsertRange(LineNumber, lines);
                    blockInfoStack.Push("", blockName, LineNumber);
                }
            }
        }

        // '"' が来るまで読みこんで、currentStr に格納。
        public void ReadString() {
            // 「"」自身は「"\""」と表記することで指定できる。
            // 「\」自身は「"\\"」と表記する。
            // 「\」は、単に次の一文字をエスケープするだけで、
            // 「"\n"」「"\t"」「"\ooo"」は未対応。
            var sb = new StringBuilder();
            while (true) {
                char c = GetNextChar();
                if (c == '\r' || c == '\n' || c == 0) {
                    ParseError();
                }
                if (c == '"') {
                    // 文字列の終わり
                    break;
                }
                if (c == '\\') {
                    // 最初の「\」は、単に読みとばす
                    c = GetNextChar();
                }
                sb.Append(c);
            }
            currentStr = sb.ToString();
        }

        // 何らかのデリミタが来るまで読みこんで、currentStr に格納。スラッシュは文字列に含む
        public void ReadBareString(char c = '\0') {
            var sb = new StringBuilder();
            bool isOutputChar() { return c == '/' || (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c >= 0x80; }
            if (c != '\0') {
                if (!isOutputChar()) return;
                sb.Append(c);
            }
            while (true) {
                c = PeekNextChar();
                if (!isOutputChar()) break;
                GetNextChar();
                sb.Append(c);
            }
            currentStr = sb.ToString();
            logger.DebugH(() => $"LEAVE: {currentStr}");
        }

        // 区切り文字が来るまで読みこんで、currentStr に格納。
        public void ReadMarker() {
            var sb = new StringBuilder();
            while (true) {
                char c = PeekNextChar();
                if (c <= ' ' || c == ',' || c == '|' || c == ';' || c == '/') {
                    if (sb._isEmpty()) ParseError();
                    currentStr = sb.ToString();
                    return;
                }
                GetNextChar();
                sb.Append(c);
            }
        }

        public string ReadWord() {
            currentStr = "";
            if (nextPos >= CurrentLine._safeLength()) return currentStr;
            char c = SkipSpace();
            if (c <= ' ') return currentStr;
            if (c == ';' || (c == '/' && PeekNextChar() == '/')) {
                SkipToEndOfLine();
                return currentStr;
            }
            currentStr = readWordSub(c);
            return currentStr;
        }

        string readWordSub(char c) {
            var sb = new StringBuilder();
            sb.Append(c);
            while (true) {
                c = GetNextChar();
                if (c <= ' ') {
                    return sb.ToString();
                }
                sb.Append(c);
            }
        }

        // 行末までの範囲で文字列または単語を読み込む
        public string ReadWordOrString()
        {
            currentStr = "";
            if (nextPos < CurrentLine._safeLength()) {
                char c = SkipSpace();
                if (c > ' ') {
                    if (c == '"') {
                        ReadString();
                    } else if (c == ';' || (c == '/' && PeekNextChar() == '/')) {
                        SkipToEndOfLine();
                    } else {
                        readWordSub(c);
                    }
                }
            }
            return currentStr;
        }

        public char PeekNextChar() {
            return CurrentChar = (nextPos < CurrentLine.Length) ? CurrentLine[nextPos] : '\n';
        }

        public char GetNextChar() {
            if (nextPos > CurrentLine.Length) {
                ++lineNumber;
                if (lineNumber >= tableLines.Count) {
                    return CurrentChar = '\0';
                }
                nextPos = 0;
            }
            if (nextPos < CurrentLine.Length) {
                CurrentChar = CurrentLine[nextPos++];
            } else {
                ++nextPos;
                CurrentChar = '\n';
            }
            return CurrentChar;
        }

        public bool GetNextLine() {
            ++lineNumber;
            if (lineNumber >= tableLines.Count) {
                return false;
            }
            return true;
        }

        public void SkipToEndOfLine() {
            nextPos = CurrentLine.Length + 1;
            CurrentChar = '\n';
        }

        // 空白文字を読み飛ばす
        public char SkipSpace() {
            while (true) {
                char c = GetNextChar();
                if (c == '\r' || c == '\n' || c == 0 || c > ' ')  return c;
            }
        }

        public void RewindChar()
        {
            --nextPos;
        }

        public string MakeErrorLines() {
            var sb = new StringBuilder();
            sb.Append("lines=\n");
            for (int i = 9; i > 0; --i) {
                if (lineNumber >= i && tableLines.Count > lineNumber - i) sb.Append(tableLines[lineNumber - i]).Append('\n');
            }
            sb.Append($">> {CurrentLine}\n");
            for (int i = 1; i < 10; ++i) {
                if (lineNumber + i < tableLines.Count) sb.Append(tableLines[lineNumber + i]).Append('\n');
            }
            return sb.ToString();
        }

        string blockOrFile() {
            return blockInfoStack.CurrentDirPath._isEmpty() ? "ブロック" : "テーブルファイル";
        }

        int calcErrorLineNumber() {
            return blockInfoStack.CalcCurrentLineNumber(LineNumber);
        }

        int calcErrorColumn() {
            if (nextPos == 0 && lineNumber > 0) return tableLines[lineNumber - 1].Count();
            return nextPos;
        }

        // 解析エラー
        public void ParseError(string msg = null) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(String.Format("{0}{1} {2} の {3}行 {4}文字目({5})がまちがっているようです：\r\n> {6} ...",
                msg._notEmpty() ? msg + "\r\n" : "",
                blockOrFile(),
                blockInfoStack.CurrentBlockName,
                calcErrorLineNumber(),
                calcErrorColumn(),
                errorChar(),
                CurrentLine._safeSubstring(0, 50)));
        }

        string errorChar()
        {
            switch (CurrentChar) {
                case '\n':
                case '\r':
                    return "NL";
                case '\0':
                    return "NULL";
                default:
                    return $"'{CurrentChar}'";
            }
        }

        // 引数エラー
        public void ArgumentError(string arg) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(String.Format("引数 {0} が不正です。\r\nテーブルファイル {1} の {2}行目がまちがっているようです：\r\n> {3} ...",
                arg, blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // loadループエラー
        public void LoadLoopError(string name) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(String.Format("ブロック {0} のロードがループしています。\r\n{1} {2} の {3}行目がまちがっているようです：\r\n> {4} ...",
                name, blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // storeブロックが存在しない
        public void NoSuchBlockError(string name) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(String.Format("指定されたブロック {0} が存在しません。\r\n{1} {2} の {3}行目がまちがっているようです：\r\n> {4} ...",
                name, blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // ファイルの読み込みに失敗した場合
        public void FileOpenError(string filename) {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(String.Format("ファイル {0} を読み込めません。\r\nテーブルファイル {1} の {2}行目がまちがっているようです：\r\n> {3} ...",
                filename, blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // ノードの重複が発生した場合
        public void NodeDuplicateWarning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(String.Format("{0} {1} の {2}行目でノードの重複が発生しました。意図したものであれば無視してください (#ignoreWarning overwrite を記述すると無視されます)：\r\n> {3} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // カラム0で予期しないLBRACEが発生
        public void UnexpectedLeftBraceAtColumn0Warning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(
                String.Format("{0} {1} の {2}行目の行頭にネストされた '{{' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述すると無視されます)：\r\n> {3} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // カラム0で予期しないRBRACEが発生
        public void UnexpectedRightBraceAtColumn0Warning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(
                String.Format("{0} {1} の {2}行目の行頭にまだネスト中の '}}' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述すると無視されます)：\r\n> {3} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // エラー処理
        void handleError(string msg) {
            logger.Error(msg);
            logger.Error("lines=\n" + MakeErrorLines());
            // エラーメッセージを投げる
            Error(msg);
        }

        // 警告ー処理
        void handleWarning(string msg) {
            logger.Warn(msg);
            logger.Warn("lines=\n" + MakeErrorLines());
            // エラーメッセージを投げる
            Warn(msg);
        }

        public void showErrorMessage()
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
        public void Error(string msg) {
            setErrorInfo(Logger.LogLevelError, msg);
        }

        // 警告情報を格納するが、継続する
        public void Warn(string msg) {
            setErrorInfo(Logger.LogLevelWarn, msg);
        }

    }

    /// <summary>
    /// テーブル解析器
    /// </summary>
    class TableParser
    {
        private static Logger logger = Logger.GetLogger();

        // トークンの種類
        enum TOKEN {
            IGNORE,
            END,
            LBRACE,         // {
            RBRACE,         // }
            COMMA,          // ,
            VBAR,           // |
            NEW_LINE,
            STRING,         // "str"
            BARE_STRING,    // str
            FUNCTION,       // @?
            SLASH,          // /
            ARROW,          // -n>
            ARROW_BUNDLE,   // -*>-n>
            REWRITE_PRE,    // %n>
            REWRITE_POST,   // &n>
            PLACE_HOLDER,   // $n
        };

        TableLines tableLines;

        TOKEN currentToken = TOKEN.IGNORE;  // 最後に読んだトークン
        string currentStr;                  // 文字列トークン
        int arrowIndex = -1;                // ARROWインデックス

        bool bPrimary;                      // 主テーブルか

        bool bRewriteEnabled = false;       // 書き換えノードがあった

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

        // 出力用のバッファ
        List<string> outputLines = new List<string>();

        public List<string> OutputLines => outputLines;

        // プレースホルダー
        Dictionary<string, int> placeHolders = new Dictionary<string, int>();

        // 書き換えテーブルが対象
        bool bRewriteTable = false;

        StrokeTableNode rootTableNode = new StrokeTableNode(true);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableParser(TableLines tableLines, KeyCombinationPool pool, bool primary)
        {
            this.tableLines = tableLines;
            bPrimary = primary;
            keyComboPool = pool;
        }

        /// <summary>
        /// テーブル定義を解析してストローク木を構築する。
        /// 解析結果を矢印記法に変換して出力ファイル(outFile)に書き込む
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="outFilename"></param>
        public void ParseTable()
        {
            logger.InfoH($"ENTER");

            readNextToken(true);
            while (currentToken != TOKEN.END) {
                switch (currentToken) {
                    case TOKEN.LBRACE:
                        parseSubTree();
                        break;

                    case TOKEN.ARROW:
                    case TOKEN.REWRITE_PRE:
                    case TOKEN.REWRITE_POST:
                        parseArrowNode(currentToken, arrowIndex);
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        parseArrowBundleNode(arrowIndex);
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:
                        tableLines.ParseError();
                        break;
                }
                readNextToken(true);
            }

            keyComboPool.SetNonTerminalMarkForSubkeys();
            if (Logger.IsInfoHEnabled && logger.IsInfoHPromoted) {
                keyComboPool.DebugPrint();
            }

            if (bRewriteEnabled) {
                // 書き換えノードがあったら、SandSの疑似同時打鍵サポートをOFFにしておく
                Settings.SandSEnablePostShift = false;
            }

            addMyCharFunctionInRootStrokeTable();

            logger.InfoH($"LEAVE: KeyCombinationPool.Count={keyComboPool.Count}");
        }

        /// <summary>もしルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる</summary>
        void addMyCharFunctionInRootStrokeTable()
        {
            for (int idx = 0; idx < DecoderKeys.NORMAL_DECKEY_NUM; ++idx) {
                if (rootTableNode.getNth(idx) == null) {
                    outputLines.Add($"-{idx}>@^");
                }
            }
        }

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
                logger.Warn($"DUPLICATED: {tableLines.CurrentLine}");
                tableLines.NodeDuplicateWarning();
            }
        }

        int calcRow(int idx, int currentRow)
        {
            if (idx <= 40) return idx / 10;
            return currentRow;
        }

        int calcOverrunIndex(int idx)
        {
            if (idx == 10) return 41;
            if (idx == 20) return 44;
            if (idx == 30) return 46;
            if (idx == 40) return 48;
            return idx;
        }

        int calcNewLinedIndex(int row)
        {
            return row * 10;
        }

        void parseSubTree()
        {
            logger.DebugH(() => $"ENTER: currentLine={tableLines.LineNumber}, strokeList={strokeList._keyString()}");
            bool bError = false;
            int idx = 0;
            int row = 0;
            //bool isPrevDelim = true;
            TOKEN prevToken = 0;
            TOKEN prevPrevToken = 0;
            readNextToken();
            while (!bError && currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                    case TOKEN.LBRACE:
                        strokeList.Add(idx);
                        //getOrNewLastTreeNode();
                        parseSubTree();
                        strokeList._popBack();
                        //++idx;
                        //isPrevDelim = false;
                        break;

                    case TOKEN.ARROW:
                        parseArrowNode(currentToken, arrowIndex);
                        //isPrevDelim = false;
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        parseArrowBundleNode(arrowIndex);
                        break;

                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.BARE_STRING:        // str : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        parseNode(currentToken, idx);
                        //++idx;
                        //isPrevDelim = false;
                        break;

                    case TOKEN.PLACE_HOLDER:
                        placeHolders[tableLines.currentStr] = idx;
                        break;

                    case TOKEN.VBAR:               // 次のトークン待ち
                        if ((prevToken == 0 || prevToken == TOKEN.VBAR) && isInCombinationBlock && depth > 0) {
                            // 空セルで、同時判定ブロック内で、深さ2以上なら、同時打鍵可能としておく
                            addCombinationKey(-1, idx, false);
                        }
                        row = calcRow(idx, row);
                        idx = calcOverrunIndex(idx + 1);
                        break;

                    case TOKEN.NEW_LINE:           // 次の行
                        if (prevToken == TOKEN.VBAR || prevPrevToken == TOKEN.VBAR) {
                            idx = calcNewLinedIndex(++row);
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
                        tableLines.ParseError();
                        bError = true;
                        break;
                }
                prevPrevToken = prevToken;
                prevToken = currentToken;

                readNextToken();
            }

            if (depth == 0) placeHolders.Clear();
            logger.DebugH(() => $"LEAVE: currentLine={tableLines.LineNumber}, depth={depth}, bError={bError}");
        }

        // 前置書き換対象文字列指定ノード
        // %あいう> {
        //  -nn>・・・
        // }
        // %あいう>-nn>{ } もあり
        //void parsePreRewriteNode()
        //{
        //    logger.DebugH(() => $"ENTER: currentLine={tableLines.LineNumber}, depth={depth}, idx={idx}");
        //    readNextToken(true);
        //    //var tokenNextToArrow = currentToken;
        //    if (currentToken == TOKEN.ARROW) {
        //        parseArrowNode(token, arrowIndex);
        //    } else if (token == TOKEN.REWRITE_PRE || token == TOKEN.REWRITE_POST) {
        //        if (currentToken == TOKEN.LBRACE) {
        //            // 書き換えノードの追加
        //            logger.DebugH(() => $"ADD REWRITE NODE");
        //            addRewriteNode(token, calcShiftOffset(idx), targetStr);
        //            if (token == TOKEN.REWRITE_PRE && idx >= 0) {
        //                keyComboPool.AddPreRewriteKey(idx);
        //            }
        //        } else {
        //            tableLines.ParseError();
        //        }
        //    } else {
        //        if (currentToken == TOKEN.LBRACE) {
        //            parseSubTree();
        //        } else {
        //            parseNode(currentToken, -1);
        //        }
        //    }
        //    currentToken = TOKEN.IGNORE;    // いったん末端ノードの処理をしたら、矢印記法を抜けるまで無視
        //    logger.DebugH(() => $"LEAVE: currentLine={tableLines.LineNumber}, depth={depth}");
        //    strokeList._popBack();
        //    return;
        //}

        // 矢印記法(-\d+(,\d+)*>)を解析して第1打鍵位置に従って配置する
        void parseArrowNode(TOKEN token, int idx, string targetStr = "") {
            strokeList.Add(idx);
            logger.DebugH(() => $"ENTER: currentLine={tableLines.LineNumber}, depth={depth}, idx={idx}");
            readNextToken(true);
            //var tokenNextToArrow = currentToken;
            if (currentToken == TOKEN.ARROW) {
                parseArrowNode(token, arrowIndex);
            } else if (currentToken == TOKEN.COMMA || currentToken == TOKEN.VBAR) {
                if (parseArrow(tableLines.GetNextChar())) parseArrowNode(token, arrowIndex);
            } else if (token == TOKEN.REWRITE_PRE || token == TOKEN.REWRITE_POST) {
                if (currentToken == TOKEN.LBRACE) {
                    // 書き換えノードの追加
                    logger.DebugH(() => $"ADD REWRITE NODE");
                    addRewriteNode(token, calcShiftOffset(idx), targetStr);
                    if (token == TOKEN.REWRITE_PRE && idx >= 0) {
                        keyComboPool.AddPreRewriteKey(idx);
                    }
                } else {
                    tableLines.ParseError();
                }
            } else {
                if (currentToken == TOKEN.LBRACE) {
                    parseSubTree();
                } else {
                    parseNode(currentToken, -1);
                }
            }
            currentToken = TOKEN.IGNORE;    // いったん末端ノードの処理をしたら、矢印記法を抜けるまで無視
            logger.DebugH(() => $"LEAVE: currentLine={tableLines.LineNumber}, depth={depth}");
            strokeList._popBack();
            return;
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        void parseArrowBundleNode(int nextArrowIdx)
        {
            logger.DebugH(() => $"ENTER: depth={depth}, nextArrowIdx={nextArrowIdx}");

            int n = 0;
            int row = 0;
            //bool isPrevDelim = true;
            readNextToken(true);
            if (currentToken != TOKEN.LBRACE) { // 直後は '{' でブロックの始まりである必要がある
                tableLines.ParseError();
                return;
            }
            TOKEN prevToken = 0;
            TOKEN prevPrevToken = 0;
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
                    case TOKEN.BARE_STRING:        // str : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        parseNode(currentToken, nextArrowIdx, n);
                        //++n;
                        //isPrevDelim = false;
                        break;

                    case TOKEN.PLACE_HOLDER:
                        placeHolders[tableLines.currentStr] = n;
                        break;

                    case TOKEN.VBAR:              // 次のトークン待ち
                        row = calcRow(n, row);
                        n = calcOverrunIndex(n + 1);
                        break;

                    case TOKEN.NEW_LINE:
                        if (prevToken == TOKEN.VBAR || prevPrevToken == TOKEN.VBAR) {
                            n = calcNewLinedIndex(++row);
                        }
                        break;

                    case TOKEN.COMMA:              // 次のトークン待ち
                    case TOKEN.SLASH:              // 次のトークン待ち
                        //if (isPrevDelim) ++n;
                        //isPrevDelim = true;
                        ++n;
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:                        // 途中でファイルが終わったりした場合 : エラー
                        tableLines.ParseError();
                        break;
                }
                prevPrevToken = prevToken;
                prevToken = currentToken;

                readNextToken();
            }

            if (depth == 0) placeHolders.Clear();
            logger.DebugH(() => $"LEAVE: depth={depth}");
        }

        void parseNode(TOKEN token, int nth, int prevNth = -1)
        {
            logger.DebugH(() => $"ENTER: token={token}, currentStr={tableLines.currentStr}, depth={depth}, prevNth={prevNth}, nth={nth}");
            switch (token) {
                //case TOKEN.LBRACE:
                //    parseSubTree(getOrNewLastTreeNode(), nth);
                //    logger.DebugH(() => $"LEAVE: depth={depth}");
                //    break;

                case TOKEN.STRING:            // "str" : 文字列ノード
                case TOKEN.BARE_STRING:       // str : 文字列ノード
                    // 終端ノードの追加と同時打鍵列の組合せの登録
                    addTerminalNode(token, new StringNode($"{tableLines.currentStr._safeReplace(@"\", @"\\")._safeReplace(@"""", @"\""")}"), prevNth, nth);
                    if (depth <= 1 && tableLines.currentStr._startsWith("!{")) {
                        // 矢印記法の場合も考慮する
                        int dk = nth;
                        int dp = depth;
                        if (dp == 1 && dk < 0) {
                            dk = strokeList[0];
                            dp = 0;
                        }
                        if (dp == 0 && dk >= 0) {
                            logger.DebugH(() => $"REPEATABLE");
                            keyComboPool.AddRepeatableKey(dk);
                        }
                    }
                    logger.DebugH(() => $"LEAVE: depth={depth}");
                    break;

                case TOKEN.FUNCTION:          // @c : 機能ノード
                    // 終端ノードの追加と同時打鍵列の組合せの登録
                    addTerminalNode(token, new FunctionNode(tableLines.currentStr), prevNth, nth);
                    logger.DebugH(() => $"LEAVE: depth={depth}");
                    break;

                case TOKEN.VBAR:              // '|' が来たら次のトークン
                    break;

                case TOKEN.RBRACE:
                case TOKEN.COMMA:             // ',' が来たら次のトークン
                case TOKEN.SLASH:             // '/' が来ても次のトークン
                    logger.DebugH(() => $"LEAVE: depth={depth}");
                    break;

                case TOKEN.NEW_LINE:
                case TOKEN.IGNORE:
                    break;

                default:                // 途中でファイルが終わったりした場合 : エラー
                    tableLines.ParseError();
                    break;
            }
        }

        HashSet<int> sequentialShiftKeys = new HashSet<int>();

        /// <summary>
        /// 終端ノードの追加と同時打鍵列の組合せの登録<br/>
        /// 同時打鍵の場合は、ブロックのルートキーをCOMBO_DECKEY_STARTまでシフトする
        /// </summary>
        /// <param name="prevNth"></param>
        /// <param name="lastNth"></param>
        void addTerminalNode(TOKEN token, Node node, int prevNth, int lastNth)
        {
            var list = addCombinationKey(prevNth, lastNth, true);

            if (list._notEmpty()) {
                string dq = token == TOKEN.STRING ? "\"" : "";
                string outStr = node.isStringNode() ? $"{dq}{node.getString()}{dq}" : node.getMarker();
                outputLines.Add($"-{list.Select(x => x.ToString())._join(">-")}>{outStr}");

                // 対象が書き換えテーブルでなければノードをセットする
                if (!bRewriteTable) setNodeAtLast(list, node);
            }
        }

        List<int> addCombinationKey(int prevNth, int lastNth, bool hasStr)
        {
            var list = new List<int>(strokeList);

            if (prevNth >= 0) list.Add(prevNth);
            if (lastNth >= 0) list.Add(lastNth);

            if (list._notEmpty()) {
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

                if (isInCombinationBlock || list.Count == 1) {
                    makeCombinationKeyCombo(list, shiftOffset, hasStr);
                } else {
                    keyComboPool.ContainsSequentialShiftKey = true;
                    for (int i = 0; i < list.Count - 1; ++i) {
                        int dk = list[i];
                        if (!sequentialShiftKeys.Contains(dk)) {
                            addSequentialShiftKey(dk, shiftOffset);
                            sequentialShiftKeys.Add(dk);
                        }
                    }
                }
            }

            return list;
        }

        void addRewriteNode(TOKEN token, int shiftOffset, string targetStr)
        {
            logger.DebugH(() => $"ENTER: token={token}");

            if (strokeList._isEmpty()) return;

            string getNthRootNodeString(int n)
            {
                return (rootTableNode.getNth(n)?.getString())._stripDq()._toSafe();
            }

            string getRewriteString(bool bBare)
            {
                return !bBare ? "\"" + tableLines.currentStr + "\"" : tableLines.currentStr;
            }

            //void setNodeAt(int _idx, Node _node)
            //{
            //    setNodeAtLast(Helper.MakeList(_idx), _node);
            //}

            int lastIdx = strokeList.Last();

            tableLines.ReadWordOrString(); // 後置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
            var myStr = tableLines.currentStr._orElse(() => lastIdx >= 0 ? getNthRootNodeString(lastIdx) : "");
            var node = token == TOKEN.REWRITE_POST ? new FunctionNode(myStr) : null;

            if (node != null && lastIdx >= 0) outputLines.Add($"-{lastIdx}>@{{{myStr}");

            string leaderStr = strokeList.Count > 1 ? strokeList.Take(strokeList.Count - 1).Select(x => getNthRootNodeString(x))._join("") : "";

            void setNodeAndOutputByIndex(int nth, bool bBare)
            {
                int rootIdx = nth + shiftOffset;
                string s = getNthRootNodeString(rootIdx);
                if (s._notEmpty()) {
                    string rewStr = getRewriteString(bBare);
                    if (node == null) {
                        Node nd = new FunctionNode(s);
                        outputLines.Add($"-{rootIdx}>@{{{s}");
                        //setNodeAt(nth, nd);
                        outputLines.Add($"{leaderStr}{myStr}\t{rewStr}");
                        outputLines.Add("}");
                    } else {
                        outputLines.Add($"{s}{leaderStr}\t{rewStr}");
                    }
                }
            }

            void setTableNodeAndOutputByIndex(int nth)
            {
                int rootIdx = nth + shiftOffset;
                string s = getNthRootNodeString(rootIdx);
                var shiftKeyKindSave = shiftKeyKind;
                shiftKeyKind = ShiftKeyKind.None;
                int shiftPlaneSave = shiftPlane;
                shiftPlane = 0;
                List<int> strokeListSave = new List<int>(strokeList);
                strokeList.Clear();
                bRewriteTable = true;

                if (s._notEmpty()) {
                    if (node == null) {
                        Node nd = new FunctionNode(s);
                        outputLines.Add($"-{rootIdx}>@{{{s}");
                        //setNodeAt(nth, nd);
                        outputLines.Add($"{leaderStr}{myStr}\t{{");
                    } else {
                        outputLines.Add($"{s}{leaderStr}\t{{");
                    }
                    parseSubTree();
                    outputLines.Add("}");
                    if (node == null) {
                        outputLines.Add("}");
                    }
                }

                bRewriteTable = false;
                strokeList.AddRange(strokeListSave);
                shiftPlane = shiftPlaneSave;
                shiftKeyKind = shiftKeyKindSave;
            }

            int idx = 0;
            int row = 0;
            //bool isPrevDelim = true;
            TOKEN prevToken = 0;
            TOKEN prevPrevToken = 0;
            var items = new string[2];
            int itemIdx = 0;
            bool bError = false;
            tableLines.SkipToEndOfLine();
            readNextToken();
            while (!bError && currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                    case TOKEN.LBRACE:          // { target, rewrite } 形式
                        readNextToken();
                        itemIdx = 0;
                        while (currentToken != TOKEN.RBRACE) {
                            switch (currentToken) {
                                case TOKEN.COMMA:
                                    break;

                                case TOKEN.STRING:
                                case TOKEN.BARE_STRING:
                                    if (tableLines.currentStr._isEmpty() || itemIdx >= items.Length) {
                                        tableLines.ParseError();
                                    } else {
                                        items[itemIdx++] = getRewriteString(currentToken == TOKEN.BARE_STRING);
                                    }
                                    break;

                                default:
                                    tableLines.ParseError();
                                    break;
                            }
                            readNextToken();
                        }
                        if (itemIdx != 2) {
                            tableLines.ParseError();
                        } else {
                            if (node != null) outputLines.Add(items._join("\t"));
                        }
                        break;

                    case TOKEN.ARROW:
                        int arrowIdx = arrowIndex;
                        readNextToken();
                        switch (currentToken) {
                            case TOKEN.LBRACE:
                                // -22>{ } 形式
                                setTableNodeAndOutputByIndex(arrowIdx);
                                break;
                            case TOKEN.STRING:
                            case TOKEN.BARE_STRING:
                                setNodeAndOutputByIndex(arrowIdx, currentToken == TOKEN.BARE_STRING);
                                break;
                            default:
                                tableLines.ParseError();
                                break;
                        }
                        break;

                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.BARE_STRING:        // str : 文字列ノード
                        setNodeAndOutputByIndex(idx, currentToken == TOKEN.BARE_STRING);
                        break;

                    case TOKEN.PLACE_HOLDER:        // プレースホルダー
                        placeHolders[tableLines.currentStr] = idx;
                        break;

                    case TOKEN.VBAR:               // 次のトークン待ち
                        row = calcRow(idx, row);
                        idx = calcOverrunIndex(idx + 1);
                        break;

                    case TOKEN.NEW_LINE:           // 次の行
                        if (prevToken == TOKEN.VBAR || prevPrevToken == TOKEN.VBAR) {
                            idx = calcNewLinedIndex(++row);
                        }
                        break;

                    case TOKEN.COMMA:              // 次のトークン待ち
                        ++idx;
                        break;

                    case TOKEN.IGNORE:
                        break;

                    default:                        // 途中でファイルが終わったりした場合 : エラー
                        tableLines.ParseError();
                        bError = true;
                        break;
                }
                prevPrevToken = prevToken;
                prevToken = currentToken;

                readNextToken();
            }

            if (node != null) {
                outputLines.Add("}");
                //setNodeAt(lastIdx, node);
            }

            if (depth == 0) placeHolders.Clear();
            logger.DebugH(() => $"LEAVE: str={node?.getMarker()}");
        }

        int calcShiftOffset(int deckey)
        {
            return (deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey / DecoderKeys.PLANE_DECKEY_NUM : shiftPlane) * DecoderKeys.PLANE_DECKEY_NUM;
        }

        // 同時打鍵列の組合せを作成して登録しておく
        void makeCombinationKeyCombo(List<int> deckeyList, int shiftOffset, bool hasStr)
        {
            var comboKeyList = deckeyList.Select(x => makeShiftedDecKey(x, shiftOffset)).ToList();      // 先頭キーのオフセットに合わせる
            logger.DebugH(() => $"{deckeyList._keyString()}={tableLines.currentStr}");
            keyComboPool.AddComboShiftKey(comboKeyList[0], shiftKeyKind); // 元の拡張シフトキーコードに戻して、同時打鍵キーとして登録
            keyComboPool.AddEntry(deckeyList, comboKeyList, shiftKeyKind, hasStr);
        }

        void addSequentialShiftKey(int decKey, int shiftOffset)
        {
            keyComboPool.AddComboShiftKey(makeShiftedDecKey(decKey, shiftOffset), ShiftKeyKind.SequentialShift);
        }

        int makeComboDecKey(int decKey)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + DecoderKeys.COMBO_DECKEY_START;
        }

        int makeShiftedDecKey(int decKey, int shiftOffset)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + shiftOffset;
        }

        //void setNthChildNode(StrokeTableNode parentNode, int n, Node childNode) {
        //    if (parentNode != null && childNode != null) {
        //        if (!isInCombinationBlock) {
        //            // 同時打鍵ブロック以外ならば上書きOK
        //            parentNode.setNthChild(n, childNode);
        //        } else {
        //            // 同時打鍵ブロックの場合
        //            Node p = parentNode.getNth(n);
        //            if (p == null || p.isFunctionNode()) {
        //                // 未割り当て、または機能ノードならば上書きOK
        //                parentNode.setNthChild(n, childNode);
        //            } else if (childNode.isFunctionNode()) {
        //                // 重複していて、子ノードが機能ノードなら無視
        //            } else {
        //                // 重複していて、親ノードも子ノードも機能ノード以外なら警告
        //                if (!bIgnoreWarningOverwrite) {
        //                    logger.Warn($"DUPLICATED: {tableLines.CurrentLine}");
        //                    tableLines.NodeDuplicateWarning();
        //                }
        //            }
        //        }
        //    }
        //}

        // 現在のトークンをチェックする
        bool isCurrentToken(TOKEN target) {
            return (currentToken == target);
        }

        // 現在のトークンをチェックする
        void checkCurrentToken(TOKEN target) {
            if (currentToken != target) {
                tableLines.ParseError();           // 違ったらエラー
            }
        }

        // トークンひとつ読んで currentToken にセット
        void readNextToken(bool bSkipNL = false) {
            currentToken = getToken(bSkipNL);
        }

        bool bIgnoreWarningAll = false;
        bool bIgnoreWarningBraceLevel = false;
        bool bIgnoreWarningOverwrite = false;
        int braceLevel = 0;

        // トークンを読む
        TOKEN getToken(bool bSkipNL)
        {
            currentStr = "";
            arrowIndex = -1;
            while (true) {
                char ch = tableLines.GetNextChar();

                if (ch == '\0') {
                    // ファイルの終わり
                    return TOKEN.END;
                }

                if (ch == '#') {
                    // Directive: '#include', '#define', '#strokePosition', '#*shift*', '#combination', '#overlapping', '#yomiConvert', '#store', '#load', '#end' または '#' 以降、行末までコメント
                    tableLines.ReadWord();
                    var lcStr = currentStr._toLower();
                    if (lcStr == "include") {
                        tableLines.IncludeFile();
                    } else if (lcStr == "define") {
                        outputLines.Add(tableLines.CurrentLine);
                        tableLines.ReadWord();
                        if (currentStr._toLower()._equalsTo("defguide")) {
                            handleStrokePosition();
                        }
                    } else if (lcStr == "store") {
                        tableLines.StoreLineBlock();
                    } else if (lcStr == "load") {
                        tableLines.LoadLineBlock();
                    } else if (lcStr._startsWith("yomiconv")) {
                        outputLines.Add(tableLines.CurrentLine);
                    } else if (lcStr == "strokeposition") {
                        //outputLines.Add(tableLines.CurrentLine);
                        handleStrokePosition();
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
                        tableLines.ReadWord();
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
                                tableLines.ArgumentError(currentStr);
                                break;
                        }
                    } else if (lcStr == "end") {
                        tableLines.ReadWord();
                        switch (currentStr._toLower()._substring(0, 5)) {
                            case "combi":
                            case "overl":
                                shiftKeyKind = ShiftKeyKind.None;
                                break;
                            case "shift":
                            case "sands":
                                shiftPlane = 0;
                                break;
                            case "__inc":
                                logger.DebugH(() => $"END INCLUDE/LOAD: lineNumber={tableLines.LineNumber}");
                                tableLines.EndInclude();
                                break;
                        }
                    } else if (lcStr == "sands") {
                        handleSandSState();
                    } else if (lcStr == "assignplane") {
                        assignShiftPlane();
                    } else if (lcStr == "set") {
                        handleSettings();
                    } else if (lcStr == "ignorewarning") {
                        tableLines.ReadWord();
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
                    tableLines.SkipToEndOfLine();
                    continue;
                }

                switch (tableLines.CurrentChar) {
                    case ';':
                        // ';' 以降、行末までコメント
                        tableLines.SkipToEndOfLine();
                        break;

                    case '{':
                        if (!bIgnoreWarningBraceLevel && !bIgnoreWarningAll && tableLines.IsCurrentPosHeadOfLine && braceLevel > 0) tableLines.UnexpectedLeftBraceAtColumn0Warning();
                        ++braceLevel;
                        return TOKEN.LBRACE;

                    case '}':
                        if (!bIgnoreWarningBraceLevel && !bIgnoreWarningAll && tableLines.IsCurrentPosHeadOfLine && braceLevel > 1) tableLines.UnexpectedRightBraceAtColumn0Warning();
                        --braceLevel;
                        return TOKEN.RBRACE;

                    case ',': return TOKEN.COMMA;
                    case '|': return TOKEN.VBAR;

                    case '/':
                        if (tableLines.PeekNextChar() == '/') {
                            // 2重スラッシュはコメント扱い
                            tableLines.SkipToEndOfLine();
                            break;
                        }
                        tableLines.ReadBareString(tableLines.CurrentChar);
                        if (currentStr._safeLength() > 1) return TOKEN.BARE_STRING;  // 2文字以だったら文字列扱い
                        return TOKEN.SLASH; // スラッシュ単体なので SLASH扱い

                    case '\n':
                        if (bSkipNL) break;
                        return TOKEN.NEW_LINE;

                    case ' ':                   // SPC : スキップ
                    case '\t':                  // TAB : スキップ
                    case '\r':                  // ^M  : スキップ
                    case '\f':                  // ^L  : スキップ
                        break;

                    case '@':
                        // 機能
                        tableLines.ReadMarker();
                        return TOKEN.FUNCTION;

                    case '"':
                        // 文字列
                        tableLines.ReadString();
                        return TOKEN.STRING;

                    case '-': {
                        char c = tableLines.GetNextChar();
                        if (c == '*') {
                            // 矢印束記法
                            if (parseArrowBundle()) return TOKEN.ARROW_BUNDLE;
                        } else {
                            // 矢印記法
                            if (parseArrow(c)) return TOKEN.ARROW;
                        }
                    }
                    break;

                    case '$':
                        tableLines.ReadBareString();
                        if (currentStr._notEmpty()) return TOKEN.PLACE_HOLDER;
                        break;

                    case '%':
                        if (depth != 0) {
                            tableLines.ParseError("'%'で始まる前置書き換え記法はテーブルがネストされた位置では使えません。");
                        } else if (parseArrow(tableLines.GetNextChar())) {
                            bRewriteEnabled = true;
                            return TOKEN.REWRITE_PRE;
                        }
                        break;

                    case '&':
                        if (depth != 0) {
                            tableLines.ParseError("'%'で始まる前置書き換え記法はテーブルがネストされた位置では使えません。");
                        } else if (parseArrow(tableLines.GetNextChar())) {
                            bRewriteEnabled = true;
                            return TOKEN.REWRITE_POST;
                        }
                        break;

                    case '\0':
                        // ファイルの終わり
                        return TOKEN.END;

                    case '゛':
                    case '゜':
                        // 機能
                        tableLines.RewindChar();
                        tableLines.ReadMarker();
                        return TOKEN.FUNCTION;

                    default:
                        tableLines.ReadBareString(tableLines.CurrentChar);
                        if (currentStr._notEmpty()) return TOKEN.BARE_STRING;

                        // エラー
                        tableLines.ParseError();
                        return TOKEN.IGNORE;
                }
            }
        }

        // define 行を処理
        void handleStrokePosition()
        {
            tableLines.ReadWordOrString();
            if (currentStr._notEmpty()) {
                if (bPrimary) {
                    Settings.DefGuide1 = currentStr;
                } else {
                    Settings.DefGuide2 = currentStr;
                }
            }
        }

        void handleSandSState()
        {
            tableLines.ReadWord();
            var word = currentStr._toLower();
            if (word._isEmpty()) {
                Settings.SandSEnabled = true;
                int plane = VirtualKeys.GetSandSPlane();
                if (plane > 0) shiftPlane = plane;
            } else if (word._startsWith("enable")) {
                Settings.SandSEnabled = true;
            } else if (word._startsWith("disable")) {
                Settings.SandSEnabled = false;
            } else if (word == "s") {
                Settings.SandSEnabled = true;
                shiftPlane = 1;
                VirtualKeys.AssignSanSPlane(shiftPlane);
            } else if (word.Length == 1 && word[0] >= 'a' && word[0] <= 'f') {
                Settings.SandSEnabled = true;
                shiftPlane = word[0] - 'a' + 2;
                VirtualKeys.AssignSanSPlane(shiftPlane);
            } else if (word._startsWith("enabeoneshot")) {
                Settings.OneshotSandSEnabled = true;
            } else if (word._startsWith("disabeoneshot")) {
                Settings.OneshotSandSEnabled = false;
            } else if (word._startsWith("enabepostshift")) {
                Settings.SandSEnablePostShift = true;
            } else if (word._startsWith("disabepostshift")) {
                Settings.SandSEnablePostShift = false;
            }
        }

        void assignShiftPlane()
        {
            tableLines.ReadWord();
            if (currentStr._notEmpty()) {
                bool resultOK = VirtualKeys.AssignShiftPlane(currentStr);
                if (!resultOK) {
                    tableLines.ParseError();
                }
            }
        }

        /// <summary>
        /// Settings のプロパティに値を設定する
        /// </summary>
        void handleSettings()
        {
            logger.DebugH(() => $"CALLED: currentLine={tableLines.CurrentLine}");
            tableLines.ReadWord();
            var items = currentStr._split('=');
            logger.DebugH(() => $"currentStr={currentStr}, items.Length={items._safeLength()}, items[0]={items._getFirst()}, items[1]={items._getSecond()}");
            if (items._safeLength() == 2 && items[0]._notEmpty()) {
                var propName = items[0];
                var strVal = items[1]._strip();
                const int errorVal = -999999;
                int iVal = strVal._parseInt(errorVal);
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
            tableLines.ParseError();
        }

        // ARROW: /-[SsXxPp]?[0-9]+>/
        bool parseArrow(char c) {
            int shiftOffset = -1;
            int funckeyOffset = 0;
            bool bShiftPlane = false;
            //char c = tableLines.GetNextChar();
            if (c == ' ' || c == '\t') c = tableLines.SkipSpace();
            if (c == 'N' || c == 'n') {
                shiftOffset = 0;
                c = tableLines.GetNextChar();
            } else if (c == 'S' || c == 's' || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')) {
                shiftOffset = VirtualKeys.CalcShiftOffset(c);
                c = tableLines.GetNextChar();
            } else if (c == 'X' || c == 'x') {
                shiftOffset = 0;
                funckeyOffset = DecoderKeys.FUNC_DECKEY_START;
                c = tableLines.GetNextChar();
            } else if (c == 'P' || c == 'P') {
                bShiftPlane = true;
                c = tableLines.GetNextChar();
            }
            if (c == ' ' || c == '\t') c = tableLines.SkipSpace();

            if (c == '$') {
                tableLines.ReadBareString();
                arrowIndex = placeHolders._safeGet(currentStr, -1);
            } else {
                arrowIndex = parseNumerical(c);
            }
            if (arrowIndex < 0) return false;

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
                if (arrowIndex < 0 || arrowIndex >= DecoderKeys.COMBO_DECKEY_END) {
                    tableLines.ParseError();
                    return false;
                }
            } else {
                shiftPlane = arrowIndex;
                if (shiftPlane >= DecoderKeys.ALL_PLANE_NUM) tableLines.ParseError();
                return false;
            }
            c = tableLines.GetNextChar();
            if (c == ' ' || c == '\t') c = tableLines.SkipSpace();
            if (c == ',') tableLines.RewindChar();
            else if (c != '>') tableLines.ParseError();
            logger.DebugH(() => $"depth={depth}, arrowIndex={arrowIndex}, shiftPlane={shiftPlane}, shiftOffset={shiftOffset}");
            return true;
        }

        // ARROW_BUNLE: -*>-nn>
        bool parseArrowBundle() {
            char c = tableLines.GetNextChar();
            if (c != '>') tableLines.ParseError();
            c = tableLines.GetNextChar();
            if (c != '-') tableLines.ParseError();
            c = tableLines.GetNextChar();
            if (c == '$') {
                tableLines.ReadBareString();
                arrowIndex = placeHolders._safeGet(tableLines.currentStr, -1);
            } else {
                arrowIndex = parseNumerical(c);
            }
            if (arrowIndex < 0 || arrowIndex >= DecoderKeys.PLANE_DECKEY_NUM) tableLines.ParseError();
            c = tableLines.GetNextChar();
            if (c != '>') tableLines.ParseError();
            return true;
        }

        int parseNumerical(char c)
        {
            if (!is_numeral(c)) {
                tableLines.ParseError();
                return -1;
            }
            int result = c - '0';
            while (true) {
                c = tableLines.PeekNextChar();
                if (!is_numeral(c)) break;
                tableLines.GetNextChar();
                result = result * 10 + c - '0';
            }
            return result;
        }

        bool is_numeral(char c)
        {
            return c >= '0' && c <= '9';
        }

    }

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
        public void ParseTableFile(string filename, string outFilename, KeyCombinationPool pool, bool primary)
        {
            logger.InfoH($"ENTER: filename={filename}");
            tableLines.ReadAllLines(filename);

            if (tableLines.NotEmpty) {
                TableParser parser = new TableParser(tableLines, pool, primary);
                parser.ParseTable();
                writeAllLines(outFilename, parser.OutputLines);
            } else {
                tableLines.Error($"テーブルファイル({filename})が開けません");
            }

            tableLines.showErrorMessage();

            logger.InfoH($"LEAVE");
        }

        private void writeAllLines(string filename, List<string> lines)
        {
            var path = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
            Helper.CreateDirectory(path._getDirPath());
            logger.InfoH($"ENTER: path={path}");
            Helper.WriteLinesToFile(path, lines, (e) => logger.Error(e._getErrorMsg()));
            logger.InfoH($"LEAVE");
        }

    }

}
