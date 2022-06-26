using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using KanchokuWS.CombinationKeyStroke;
using KanchokuWS.CombinationKeyStroke.DeterminerLib;

namespace KanchokuWS
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
            children = Helper.MakeList(new Node[(bRoot ? DecoderKeys.TOTAL_DECKEY_NUM : DecoderKeys.PLANE_DECKEY_NUM)]);
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
                    } else {
                        // それ以外は node を捨てる
                    }
                    return true;
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
            upsertRewrteMap(tgtStr, new RewriteInfo(outStr, pNode));
        }

        public override void OutputLine(List<string> outLines, string leaderStr)
        {
            if (!OutputFlag) {
                OutputFlag = true;
                outLines.Add(leaderStr + "@{" + myInfo.OutputStr);
                foreach (var pair in rewriteMap) {
                    if (pair.Value.SubTable != null) {
                        outLines.Add($"{pair.Key}\t{{");
                        pair.Value.SubTable.OutputLine(outLines, "");
                        outLines.Add("}");
                    } else if (pair.Value.OutputStr._notEmpty()) {
                        outLines.Add($"{pair.Key}\t{pair.Value.OutputStr}");
                    }
                }
                outLines.Add("}");
            }
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

        public List<string> GetLines() { return tableLines; }

        public bool Empty => tableLines.Count == 0;

        public bool NotEmpty => tableLines.Count != 0;

        public string CurrentLine => lineNumber < tableLines.Count ? tableLines[lineNumber] : "";

        public int LineNumber => lineNumber + 1;

        public bool IsCurrentPosHeadOfLine => nextPos == 1;

        public char CurrentChar { get; private set; }

        public string CurrentStr { get; private set; }                  // 文字列トークン

        public void ClearCurrentStr() { CurrentStr = ""; }

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
            var blockName = CurrentStr;
            logger.DebugH(() => $"CALLED: {blockName}");
            List<string> lines = null;
            if (blockName._isEmpty()) {
                ParseError("StoreLineBlock: blockName empty");
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
                ParseError("LoadLineBlock: blockName empty");
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
                    ParseError("ReadString: unexpected EOL or EOF");
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
            CurrentStr = sb.ToString();
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
            CurrentStr = sb.ToString();
            logger.DebugH(() => $"LEAVE: {CurrentStr}");
        }

        // 区切り文字が来るまで読みこんで、currentStr に格納。
        public void ReadMarker() {
            var sb = new StringBuilder();
            while (true) {
                char c = PeekNextChar();
                if (c <= ' ' || c == ',' || c == '|' || c == ';' || c == '/') {
                    if (sb._isEmpty()) ParseError("ReadMarker: unexpected delimiter");
                    CurrentStr = sb.ToString();
                    return;
                }
                GetNextChar();
                sb.Append(c);
            }
        }

        public string ReadWord() {
            CurrentStr = "";
            if (nextPos >= CurrentLine._safeLength()) return CurrentStr;
            char c = SkipSpace();
            if (c <= ' ') return CurrentStr;
            if (c == ';' || (c == '/' && PeekNextChar() == '/')) {
                SkipToEndOfLine();
                return CurrentStr;
            }
            CurrentStr = readWordSub(c);
            return CurrentStr;
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
            CurrentStr = "";
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
            return CurrentStr;
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
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("{0}{1} {2} の {3}行 {4}文字目({5})がまちがっているようです：\r\n> {6} ...",
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
            handleError(string.Format("引数 {0} が不正です。\r\nテーブルファイル {1} の {2}行目がまちがっているようです：\r\n> {3} ...",
                arg, blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // loadループエラー
        public void LoadLoopError(string name) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("ブロック {0} のロードがループしています。\r\n{1} {2} の {3}行目がまちがっているようです：\r\n> {4} ...",
                name, blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // storeブロックが存在しない
        public void NoSuchBlockError(string name) {
            logger.DebugH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("指定されたブロック {0} が存在しません。\r\n{1} {2} の {3}行目がまちがっているようです：\r\n> {4} ...",
                name, blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // ファイルの読み込みに失敗した場合
        public void FileOpenError(string filename) {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("ファイル {0} を読み込めません。\r\nテーブルファイル {1} の {2}行目がまちがっているようです：\r\n> {3} ...",
                filename, blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // ノードの重複が発生した場合
        public void NodeDuplicateWarning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(string.Format("{0} {1} の {2}行目でノードの重複が発生しました。意図したものであれば無視してください (#ignoreWarning overwrite を記述するとこの警告が出なくなります)：\r\n> {3} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // カラム0で予期しないLBRACEが発生
        public void UnexpectedLeftBraceAtColumn0Warning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(
                string.Format("{0} {1} の {2}行目の行頭にネストされた '{{' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述するとこの警告が出なくなります)：\r\n> {3} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // カラム0で予期しないRBRACEが発生
        public void UnexpectedRightBraceAtColumn0Warning() {
            logger.DebugH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(
                string.Format("{0} {1} の {2}行目の行頭にまだネスト中の '}}' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述するとこの警告が出なくなります)：\r\n> {3} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), CurrentLine._safeSubstring(0, 50)));
        }

        // エラー処理
        void handleError(string msg) {
            logger.Error(msg);
            logger.Error("lines=\n" + MakeErrorLines());
            // エラーメッセージを投げる
            Error(msg);
        }

        // 警告処理
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

    // トークンの種類
    public enum TOKEN {
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

    class ParserContext
    {
        public TableLines tableLines;

        public TOKEN currentToken = TOKEN.IGNORE;    // 最後に読んだトークン
        //string currentStr;                            // 文字列トークン
        public int arrowIndex = -1;                  // ARROWインデックス

        public bool bPrimary;                                  // 主テーブルか

        public bool bRewriteEnabled = false;         // 書き換えノードがあった

        // 同時打鍵定義ブロックの中か
        public bool isInCombinationBlock => shiftKeyKind != ShiftKeyKind.None;

        // 同時打鍵によるシフト種別
        public ShiftKeyKind shiftKeyKind = ShiftKeyKind.None;

        // 打鍵列
        //public List<int> strokeList = new List<int>();

        //public int depth => strokeList.Count;

        // 定義列マップ
        public Dictionary<string, List<string>> linesMap = new Dictionary<string, List<string>>();

        // シフト面 -- 0:シフト無し、1:通常シフト、2:ShiftA, 3:ShiftB, ...
        public int shiftPlane = 0;

        // 対象となる KeyComboPool
        public KeyCombinationPool keyComboPool;

        // 出力用のバッファ
        public List<string> OutputLines = new List<string>();

        // プレースホルダー
        public Dictionary<string, int> placeHolders = new Dictionary<string, int>();

        // 書き換えテーブルが対象
        public bool bRewriteTable = false;

        public bool bIgnoreWarningAll = false;
        public bool bIgnoreWarningBraceLevel = false;
        public bool bIgnoreWarningOverwrite = false;
        public int braceLevel = 0;

        public HashSet<int> sequentialShiftKeys = new HashSet<int>();

        public StrokeTableNode rootTableNode = new StrokeTableNode(true);

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public ParserContext(TableLines tableLines, KeyCombinationPool pool, bool primary)
        {
            this.tableLines = tableLines;
            bPrimary = primary;
            keyComboPool = pool;
        }

    }

    /// <summary>
    /// テーブル解析器
    /// </summary>
    class TableParser
    {
        private static Logger logger = Logger.GetLogger(true);

        protected ParserContext context;

        StrokeTableNode _rootTableNode;

        int _shiftPlane = -1;

        protected List<int> strokeList = new List<int>();

        protected int depth => strokeList.Count;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableParser(ParserContext ctx, List<int> stkList, StrokeTableNode rootNode = null, int shiftPlane = -1)
        {
            context = ctx;
            _rootTableNode = rootNode;
            _shiftPlane = shiftPlane;
            if (stkList._notEmpty()) {
                strokeList.AddRange(stkList);
            }
        }

        protected void outputNewLines()
        {
            var list = new List<int>();
            rootTableNode.OutputLine(OutputLines, "");
        }

        /// <summary>
        /// stkList の末尾 strokeに対応するノードを追加する<br/>
        /// 途中、StrokeTableNodeが存在しない場所があれば、そこにStrokeTableNodeを生成して挿入(または置換)する
        /// </summary>
        /// <param name="stkList"></param>
        /// <param name="node"></param>
        protected void setNodeAtLast(List<int> stkList, Node node)
        {
            logger.DebugH(() => $"CALLED: stkList={stkList._keyString()}, isCombo={isInCombinationBlock}, {node.DebugString()}");
            bool bOverwritten = false;
            if (stkList._isEmpty()) {
                logger.Warn($"strokeList is empty");
            } else {
                int getStroke(int idx)
                {
                    int stk = stkList[idx];
                    if (idx == 0 && isInCombinationBlock) stk = makeComboDecKey(stk);   // 同時打鍵の先頭キーは Combo化(ノードの重複を避ける)
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
                    logger.Warn($"No such node: strokeList={stkList._keyString()}");
                } else {
                    int idx = getStroke(stkList.Count - 1);
                    bOverwritten = bOverwritten || !(pn.getNth(idx)?.isFunctionNode() ?? true);
                    pn.setNthChild(idx, node);
                }
            }
            if (bOverwritten && isInCombinationBlock && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: strokeList={stkList._keyString()}, isCombo={isInCombinationBlock}, line={CurrentLine}");
                NodeDuplicateWarning();
            }
        }

        protected class StrokeListUndoer : IDisposable
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

        protected StrokeListUndoer pushStroke(int stroke)
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

        protected virtual void addNodeTree(int stroke = -1)
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={strokeList._keyString()}, stroke={stroke}");
            using (pushStroke(stroke)) {
                // StrokeTableNodeを追加しておく(そうでないと矢印記法によって先に空のStrokeTableNodeが作られてしまう可能性があるため)
                setNodeAtLast(strokeList, new StrokeTableNode());
                new TableParser(context, strokeList).MakeNodeTree();
                logger.DebugH(() => $"LEAVE");
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

                    case TOKEN.PLACE_HOLDER:
                        placeHolders[CurrentStr] = idx;
                        break;

                    case TOKEN.VBAR:               // 次のトークン待ち
                        if ((prevToken == 0 || prevToken == TOKEN.VBAR) && isInCombinationBlock && depth > 0) {
                            // 空セルで、同時判定ブロック内で、深さ2以上なら、同時打鍵可能としておく(TODO: 具体例）
                            using (pushStroke(idx)) {
                                addCombinationKey(false);
                            }
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
                        ParseError($"MakeNodeTree: unexpected token: {currentToken}");
                        bError = true;
                        break;
                }

                prevPrevToken = prevToken;
                prevToken = currentToken;

                readNextToken();
            }

            if (depth == 0) placeHolders.Clear();
            logger.DebugH(() => $"LEAVE: lineNum={LineNumber}, depth={depth}, bError={bError}");

        }

        // n番目の子ノードをセットする(残ったほうのノードを返す)
        protected Node setNthChildNode(StrokeTableNode tbl, int n, Node node)
        {
            if (tbl.setNthChild(n, node) && isInCombinationBlock && !bIgnoreWarningOverwrite) {
                logger.Warn($"DUPLICATED: {CurrentLine}");
                NodeDuplicateWarning();
            }
            return tbl.getNth(n);
        }

        /// <summary>もしルートテーブルのキーに何も割り当てられていなかったら、@^ (MyChar機能)を割り当てる</summary>
        public void addMyCharFunctionInRootStrokeTable()
        {
            for (int idx = 0; idx < DecoderKeys.NORMAL_DECKEY_NUM; ++idx) {
                if (rootTableNode.getNth(idx) == null) {
                    OutputLines.Add($"-{idx}>@^");
                }
            }
        }

        protected int calcRow(int idx, int currentRow)
        {
            if (idx <= 40) return idx / 10;
            return currentRow;
        }

        protected int calcOverrunIndex(int idx)
        {
            if (idx == 10) return 41;
            if (idx == 20) return 44;
            if (idx == 30) return 46;
            if (idx == 40) return 48;
            return idx;
        }

        protected int calcNewLinedIndex(int row)
        {
            return row * 10;
        }

        protected virtual void addNodeTreeByArrow()
        {
            addNodeTree();            
        }

        // 矢印記法(-\d+(,\d+)*>)を解析して第1打鍵位置に従って配置する
        protected virtual void addArrowNode(TOKEN token, int idx, string targetStr = "")
        {
            using (pushStroke(idx)) {
                logger.DebugH(() => $"ENTER: lineNum={LineNumber}, depth={depth}, idx={idx}");
                //int arrowIdx = arrowIndex;
                readNextToken(true);
                switch (currentToken) {
                    case TOKEN.ARROW:
                        if (token == TOKEN.REWRITE_PRE) {
                            ParseError($"前置書き換えの場合は、矢印記法を重ねることはできません。");
                        }
                        addArrowNode(token, arrowIndex, targetStr);
                        break;

                    case TOKEN.COMMA:
                    case TOKEN.VBAR:
                        if (token == TOKEN.REWRITE_PRE) {
                            ParseError($"前置書き換えの場合は、矢印記法を重ねることはできません。");
                        }
                        if (parseArrow(GetNextChar())) addArrowNode(token, arrowIndex, targetStr);
                        break;

                    case TOKEN.LBRACE:
                        // -22>{ } 形式
                        switch (token) {
                            case TOKEN.ARROW:
                                addNodeTreeByArrow();
                                break;
                            case TOKEN.REWRITE_PRE:
                                addPreRewriteNode(targetStr);
                                break;
                            case TOKEN.REWRITE_POST:
                                addPostRewriteNode();
                                break;
                        }
                        break;

                    case TOKEN.STRING:
                    case TOKEN.BARE_STRING:
                        if (token != TOKEN.ARROW) {
                            ParseError($"前置または後置書き換えの場合は、ブロック記述が必要です。");
                        }
                        addLeafNode(currentToken, -1);  // idx は既にpush済み
                        break;

                    default:
                        ParseError("PreRewriteParser-addArrowNode");
                        break;
                }

                currentToken = TOKEN.IGNORE;    // いったん末端ノードの処理をしたら、矢印記法を抜けるまで無視
                logger.DebugH(() => $"LEAVE: lineNum={LineNumber}, depth={depth}");
            }
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        protected virtual Node parseArrowBundleNode(int nextArrowIdx)
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
                        addLeafNode(currentToken, nextArrowIdx, n);
                        break;

                    case TOKEN.PLACE_HOLDER:
                        placeHolders[CurrentStr] = n;
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
                        ParseError($"parseArrowBundleNode: unexpected token: {currentToken}");
                        break;
                }
                prevPrevToken = prevToken;
                prevToken = currentToken;

                readNextToken();
            }

            if (depth == 0) placeHolders.Clear();
            logger.DebugH(() => $"LEAVE: depth={depth}");

            return myNode;
        }

        protected void addLeafNode(TOKEN token, int idx, int prevIdx = -1)
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
                keyComboPool.AddRepeatableKey(strokeList[0]);
            }
            logger.DebugH(() => $"LEAVE: depth={depth}");
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
        protected void addTerminalNode(bool bBare, Node node)
        {
            addCombinationKey(true);

            //string dq = !bBare ? "\"" : "";
            //string outStr = node.isStringNode() ? $"{dq}{node.getString()}{dq}" : node.getMarker();
            //OutputLines.Add($"-{strokeList.Select(x => x.ToString())._join(">-")}>{outStr}");

            // 対象が書き換えテーブルでなければノードをセットする
            if (!bRewriteTable) setNodeAtLast(strokeList, node);
        }

        protected void addCombinationKey(bool hasStr)
        {
            var list = new List<int>(strokeList);

            if (list._notEmpty()) {
                int shiftOffset = calcShiftOffset(list[0]);

                // 先頭キーはシフト化
                if (isInCombinationBlock) {
                    list[0] = makeComboDecKey(list[0]);
                } else if (list[0] < DecoderKeys.PLANE_DECKEY_NUM) {
                    list[0] += shiftOffset;
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
        }

        protected virtual string getNthRootNodeString(int n)
        {
            int idx = shiftDecKey(n);
            return (rootTableNode.getNth(idx)?.getString())._stripDq()._toSafe();
        }

        protected string leaderStr => strokeList.Count > 1 ? strokeList.Take(strokeList.Count - 1).Select(x => getNthRootNodeString(x))._join("") : "";

        protected string pathStr => strokeList.Count > 0 ? strokeList.Select(x => getNthRootNodeString(x))._join("") : "";

        protected void addPreRewriteNode(string targetStr)
        {
            logger.DebugH(() => $"ENTER");

            if (strokeList.Count != 1) {
                ParseError($"深さが1以上の前置書き換えはサポートされていません。");
            }

            // 前置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
            int preIdx = strokeList._getFirst();
            targetStr = targetStr._orElse(() => preIdx >= 0 ? getNthRootNodeString(preIdx) : "");
            keyComboPool.AddPreRewriteKey(preIdx);

            // 前置書き換えノードの処理を行う
            new PreRewriteParser(context, targetStr).MakeNodeTree();

            logger.DebugH(() => $"LEAVE");
        }

        protected void addPostRewriteNode()
        {
            logger.DebugH("ENTER");

            // 後置文字列の指定があるか→なければ単打テーブルから文字を拾ってくる
            int lastIdx = strokeList._getLast();
            var myStr = ReadWordOrString()._orElse(() => lastIdx >= 0 ? getNthRootNodeString(lastIdx) : "");
            var node = setNthChildNode(rootTableNode, lastIdx, new RewriteNode(myStr));
            if (node is RewriteNode) {
                // RewriteNode がノード木に反映された場合に限り、後置書き換えノードの処理を行う
                new PostRewriteParser((RewriteNode)node, context, leaderStr).MakeNodeTree();
            } else {
                logger.Warn("RewriteNode NOT merged");
            }
            logger.DebugH("LEAVE");
        }

        protected int calcShiftOffset(int deckey)
        {
            return (deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey / DecoderKeys.PLANE_DECKEY_NUM : shiftPlane) * DecoderKeys.PLANE_DECKEY_NUM;
        }

        protected int shiftDecKey(int deckey)
        {
            return deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey : deckey + shiftPlane * DecoderKeys.PLANE_DECKEY_NUM;
        }

        // 同時打鍵列の組合せを作成して登録しておく
        void makeCombinationKeyCombo(List<int> deckeyList, int shiftOffset, bool hasStr)
        {
            logger.DebugH(() => $"{deckeyList._keyString()}={CurrentStr}, shiftOffset={shiftOffset}, hasStr={hasStr}");
            var comboKeyList = deckeyList.Select(x => makeShiftedDecKey(x, shiftOffset)).ToList();      // 先頭キーのオフセットに合わせる
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

        // 現在のトークンをチェックする
        bool isCurrentToken(TOKEN target)
        {
            return (currentToken == target);
        }

        // 現在のトークンをチェックする
        void checkCurrentToken(TOKEN target)
        {
            if (currentToken != target) {
                ParseError("checkCurrentToken");           // 違ったらエラー
            }
        }

        // トークンひとつ読んで currentToken にセット
        public void readNextToken(bool bSkipNL = false)
        {
            currentToken = getToken(bSkipNL);
        }

        // トークンを読む
        TOKEN getToken(bool bSkipNL)
        {
            arrowIndex = -1;
            while (true) {
                ClearCurrentStr();
                char ch = GetNextChar();

                if (ch == '\0') {
                    // ファイルの終わり
                    return TOKEN.END;
                }

                if (ch == '#') {
                    // Directive: '#include', '#define', '#strokePosition', '#*shift*', '#combination', '#overlapping', '#yomiConvert', '#store', '#load', '#end' または '#' 以降、行末までコメント
                    ReadWord();
                    var lcStr = CurrentStr._toLower();
                    if (lcStr == "include") {
                        IncludeFile();
                    } else if (lcStr == "define") {
                        outputNewLines();
                        OutputLines.Add(CurrentLine);
                        ReadWord();
                        if (CurrentStr._toLower()._equalsTo("defguide")) {
                            handleStrokePosition();
                        }
                    } else if (lcStr == "store") {
                        StoreLineBlock();
                    } else if (lcStr == "load") {
                        LoadLineBlock();
                    } else if (lcStr._startsWith("yomiconv")) {
                        outputNewLines();
                        OutputLines.Add(CurrentLine);
                    } else if (lcStr == "strokeposition") {
                        //OutputLines.Add(CurrentLine);
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
                        ReadWord();
                        switch (CurrentStr._toLower()) {
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
                                ArgumentError(CurrentStr);
                                break;
                        }
                    } else if (lcStr == "end") {
                        ReadWord();
                        switch (CurrentStr._toLower()._substring(0, 5)) {
                            case "combi":
                            case "overl":
                                shiftKeyKind = ShiftKeyKind.None;
                                break;
                            case "shift":
                            case "sands":
                                shiftPlane = 0;
                                break;
                            case "__inc":
                                logger.DebugH(() => $"END INCLUDE/LOAD: lineNumber={LineNumber}");
                                EndInclude();
                                break;
                        }
                    } else if (lcStr == "sands") {
                        handleSandSState();
                    } else if (lcStr == "assignplane") {
                        assignShiftPlane();
                    } else if (lcStr == "set") {
                        handleSettings();
                    } else if (lcStr == "ignorewarning") {
                        ReadWord();
                        var word = CurrentStr._toLower();
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
                        logger.DebugH(() => $"#{CurrentStr}");
                    }
                    SkipToEndOfLine();
                    continue;
                }

                switch (CurrentChar) {
                    case ';':
                        // ';' 以降、行末までコメント
                        SkipToEndOfLine();
                        break;

                    case '{':
                        if (!bIgnoreWarningBraceLevel && !bIgnoreWarningAll && IsCurrentPosHeadOfLine && braceLevel > 0) UnexpectedLeftBraceAtColumn0Warning();
                        ++braceLevel;
                        return TOKEN.LBRACE;

                    case '}':
                        if (!bIgnoreWarningBraceLevel && !bIgnoreWarningAll && IsCurrentPosHeadOfLine && braceLevel > 1) UnexpectedRightBraceAtColumn0Warning();
                        --braceLevel;
                        return TOKEN.RBRACE;

                    case ',': return TOKEN.COMMA;
                    case '|': return TOKEN.VBAR;

                    case '/':
                        if (PeekNextChar() == '/') {
                            // 2重スラッシュはコメント扱い
                            SkipToEndOfLine();
                            break;
                        }
                        ReadBareString(CurrentChar);
                        if (CurrentStr._safeLength() > 1) return TOKEN.BARE_STRING;  // 2文字以だったら文字列扱い
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
                        ReadMarker();
                        return TOKEN.FUNCTION;

                    case '"':
                        // 文字列
                        ReadString();
                        return TOKEN.STRING;

                    case '-': {
                        char c = GetNextChar();
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
                        ReadBareString();
                        if (CurrentStr._notEmpty()) return TOKEN.PLACE_HOLDER;
                        break;

                    case '%':
                        if (depth != 0) {
                            ParseError("'%'で始まる前置書き換え記法はテーブルがネストされた位置では使えません。");
                        } else if (parseArrow(GetNextChar())) {
                            bRewriteEnabled = true;
                            return TOKEN.REWRITE_PRE;
                        }
                        break;

                    case '&':
                        if (depth != 0) {
                            ParseError("'%'で始まる前置書き換え記法はテーブルがネストされた位置では使えません。");
                        } else if (parseArrow(GetNextChar())) {
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
                        RewindChar();
                        ReadMarker();
                        return TOKEN.FUNCTION;

                    default:
                        ReadBareString(CurrentChar);
                        if (CurrentStr._notEmpty()) return TOKEN.BARE_STRING;

                        // エラー
                        ParseError($"getToken: unexpected char: '{CurrentChar}'");
                        return TOKEN.IGNORE;
                }
            }
        }

        // define 行を処理
        void handleStrokePosition()
        {
            ReadWordOrString();
            if (CurrentStr._notEmpty()) {
                if (bPrimary) {
                    Settings.DefGuide1 = CurrentStr;
                } else {
                    Settings.DefGuide2 = CurrentStr;
                }
            }
        }

        void handleSandSState()
        {
            ReadWord();
            var word = CurrentStr._toLower();
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
            ReadWord();
            if (CurrentStr._notEmpty()) {
                bool resultOK = VirtualKeys.AssignShiftPlane(CurrentStr);
                if (!resultOK) {
                    ParseError("assignShiftPlane");
                }
            }
        }

        /// <summary>
        /// Settings のプロパティに値を設定する
        /// </summary>
        void handleSettings()
        {
            logger.DebugH(() => $"CALLED: currentLine={CurrentLine}");
            ReadWord();
            var items = CurrentStr._split('=');
            logger.DebugH(() => $"currentStr={CurrentStr}, items.Length={items._safeLength()}, items[0]={items._getFirst()}, items[1]={items._getSecond()}");
            if (items._safeLength() == 2 && items[0]._notEmpty()) {
                var propName = items[0];
                var strVal = items[1]._strip();
                const int errorVal = -999999;
                int iVal = strVal._parseInt(errorVal);
                if (iVal != errorVal && Settings.SetValueByName(propName, iVal)) return;
                if (strVal._toLower()._equalsTo("true") && Settings.SetValueByName(propName, true)) return;
                if (strVal._toLower()._equalsTo("false") && Settings.SetValueByName(propName, false)) return;
                if (Settings.SetValueByName(propName, strVal._stripDq())) return;
            }
            ParseError("handleSettings");
        }

        // ARROW: /-[SsXxPp]?[0-9]+>/
        protected bool parseArrow(char c)
        {
            int shiftOffset = -1;
            int funckeyOffset = 0;
            bool bShiftPlane = false;
            //char c = GetNextChar();
            if (c == ' ' || c == '\t') c = SkipSpace();
            if (c == 'N' || c == 'n') {
                shiftOffset = 0;
                c = GetNextChar();
            } else if (c == 'S' || c == 's' || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')) {
                shiftOffset = VirtualKeys.CalcShiftOffset(c);
                c = GetNextChar();
            } else if (c == 'X' || c == 'x') {
                shiftOffset = 0;
                funckeyOffset = DecoderKeys.FUNC_DECKEY_START;
                c = GetNextChar();
            } else if (c == 'P' || c == 'P') {
                bShiftPlane = true;
                c = GetNextChar();
            }
            if (c == ' ' || c == '\t') c = SkipSpace();

            if (c == '$') {
                ReadBareString();
                arrowIndex = placeHolders._safeGet(CurrentStr, -1);
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
                    ParseError($"parseArrow: arrowIndex out of range: {arrowIndex}");
                    return false;
                }
            } else {
                shiftPlane = arrowIndex;
                if (shiftPlane >= DecoderKeys.ALL_PLANE_NUM) ParseError($"parseArrow: shiftPlane out of range: {shiftPlane}");
                return false;
            }
            c = GetNextChar();
            if (c == ' ' || c == '\t') c = SkipSpace();
            if (c == ',') RewindChar();
            else if (c != '>') ParseError($"parseArrow: '>' is expected, but {c}");
            logger.DebugH(() => $"depth={depth}, arrowIndex={arrowIndex}, shiftPlane={shiftPlane}, shiftOffset={shiftOffset}");
            return true;
        }

        // ARROW_BUNLE: -*>-nn>
        bool parseArrowBundle()
        {
            char c = GetNextChar();
            if (c != '>') ParseError($"parseArrowBundle: '>' is expected, but {c}");
            c = GetNextChar();
            if (c != '-') ParseError($"parseArrowBundle: '-' is expected, but {c}");
            c = GetNextChar();
            if (c == '$') {
                ReadBareString();
                arrowIndex = placeHolders._safeGet(CurrentStr, -1);
            } else {
                arrowIndex = parseNumerical(c);
            }
            if (arrowIndex < 0 || arrowIndex >= DecoderKeys.PLANE_DECKEY_NUM) ParseError($"parseArrowBundle: arrowIndex is out of range: {arrowIndex}");
            c = GetNextChar();
            if (c != '>') ParseError($"parseArrowBundle-2: '>' is expected, but {c}");
            return true;
        }

        int parseNumerical(char c)
        {
            if (!is_numeral(c)) {
                ParseError($"parseNumerical: {c}");
                return -1;
            }
            int result = c - '0';
            while (true) {
                c = PeekNextChar();
                if (!is_numeral(c)) break;
                GetNextChar();
                result = result * 10 + c - '0';
            }
            return result;
        }

        bool is_numeral(char c)
        {
            return c >= '0' && c <= '9';
        }

        protected TableLines tableLines => context.tableLines;

        protected TOKEN currentToken {
            get { return context.currentToken;}
            set { context.currentToken = value; }
        }
        protected int arrowIndex {
            get { return context.arrowIndex; }
            set { context.arrowIndex = value;}
        }
        protected bool bPrimary => context.bPrimary;
        protected bool bRewriteEnabled {
            get { return context.bRewriteEnabled; }
            set { context.bRewriteEnabled = value; }
        }
        protected bool isInCombinationBlock => context.isInCombinationBlock;
        protected ShiftKeyKind shiftKeyKind {
            get { return context.shiftKeyKind; }
            set { context.shiftKeyKind = value; }
        }
        //protected List<int> strokeList => context.strokeList;
        //protected int depth => context.depth;
        protected Dictionary<string, List<string>> linesMap => context.linesMap;
        protected int shiftPlane {
            get { return _shiftPlane >= 0 ? _shiftPlane : context.shiftPlane; }
            set { context.shiftPlane = _shiftPlane = value; }
        }
        protected KeyCombinationPool keyComboPool => context.keyComboPool;
        protected List<string> OutputLines => context.OutputLines;
        protected Dictionary<string, int> placeHolders => context.placeHolders;
        protected bool bRewriteTable {
            get { return context.bRewriteTable; }
            set { context.bRewriteTable = value; }
        }
        protected StrokeTableNode rootTableNode => _rootTableNode != null ? _rootTableNode : context.rootTableNode;

        protected bool bIgnoreWarningAll { get { return context.bIgnoreWarningAll; } set { context.bIgnoreWarningAll = value; } }
        protected bool bIgnoreWarningBraceLevel { get { return context.bIgnoreWarningBraceLevel; } set { context.bIgnoreWarningBraceLevel = value; } }
        protected bool bIgnoreWarningOverwrite { get { return context.bIgnoreWarningOverwrite; } set { context.bIgnoreWarningOverwrite = value; } }
        protected int braceLevel { get { return context.braceLevel; } set { context.braceLevel = value; } }

        protected HashSet<int> sequentialShiftKeys => context.sequentialShiftKeys;

        protected bool Empty => tableLines.Empty;
        protected bool NotEmpty => tableLines.NotEmpty;
        protected string CurrentLine => tableLines.CurrentLine;
        protected int LineNumber => tableLines.LineNumber;
        protected bool IsCurrentPosHeadOfLine => tableLines.IsCurrentPosHeadOfLine;
        protected char CurrentChar => tableLines.CurrentChar;
        protected string CurrentStr => tableLines.CurrentStr;
        protected void ClearCurrentStr() { tableLines.ClearCurrentStr(); }
        protected void ReadAllLines(string filename) { tableLines.ReadAllLines(filename); }
        protected void IncludeFile() { tableLines.IncludeFile(); }
        protected void EndInclude() { tableLines.EndInclude(); }
        protected void StoreLineBlock() { tableLines.StoreLineBlock(); }
        protected void LoadLineBlock() { tableLines.LoadLineBlock(); }
        protected void ReadString() { tableLines.ReadString(); }
        protected void ReadBareString(char c = '\0') { tableLines.ReadBareString(c); }
        protected void ReadMarker() { tableLines.ReadMarker(); }
        protected string ReadWord() { return tableLines.ReadWord(); }
        protected string ReadWordOrString() { return tableLines.ReadWordOrString(); }
        protected char PeekNextChar() { return tableLines.PeekNextChar(); }
        protected char GetNextChar() { return tableLines.GetNextChar(); }
        protected bool GetNextLine() { return tableLines.GetNextLine(); }
        protected void SkipToEndOfLine() { tableLines.SkipToEndOfLine(); }
        protected char SkipSpace() { return tableLines.SkipSpace(); }
        protected void RewindChar() { tableLines.RewindChar(); }
        protected string MakeErrorLines() { return tableLines.MakeErrorLines(); }
        protected void ParseError(string msg = null) { tableLines.ParseError(msg); }
        protected void ArgumentError(string arg) { tableLines.ArgumentError(arg); }
        protected void LoadLoopError(string name) { tableLines.LoadLoopError(name); }
        protected void NoSuchBlockError(string name) { tableLines.NoSuchBlockError(name); }
        protected void FileOpenError(string filename) { tableLines.FileOpenError(filename); }
        protected void NodeDuplicateWarning() { tableLines.NodeDuplicateWarning(); }
        protected void UnexpectedLeftBraceAtColumn0Warning() { tableLines.UnexpectedLeftBraceAtColumn0Warning(); }
        protected void UnexpectedRightBraceAtColumn0Warning() { tableLines.UnexpectedRightBraceAtColumn0Warning(); }
        protected void showErrorMessage() { tableLines.showErrorMessage(); }
        protected void Error(string msg) { tableLines.Error(msg); }
        protected void Warn(string msg) { tableLines.Warn(msg); }

    }

    /// <summary>
    /// 前置書き換えブロックの解析
    /// </summary>
    class PreRewriteParser : TableParser
    {
        private static Logger logger = Logger.GetLogger();

        protected string targetStr;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PreRewriteParser(ParserContext ctx, string targetStr)
            : base(ctx, null)
        {
            this.targetStr = targetStr;
        }

        // ここが呼ばれたとき、strokeListの末尾には nodeIndex が入っている
        protected override void addNodeTreeByArrow()
        {
            logger.DebugH(() => $"ENTER: lineNum={LineNumber}, strokeList={strokeList._keyString()}");
            var tblNode = new StrokeTableNode(true);
            if (addTreeOrStringNode("", tblNode)) {
                new TableParser(context, null, tblNode, 0).MakeNodeTree();
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

        protected RewriteNode node = null;

        new string leaderStr;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public PostRewriteParser(RewriteNode node, ParserContext ctx, string leaderStr)
            : base(ctx, null)
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
            : base(ctx, null)
        {
        }

        /// <summary>
        /// トップレベルのテーブル定義を解析してストローク木を構築する。
        /// 解析結果を矢印記法に変換して出力ファイル(outFile)に書き込む
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="outFilename"></param>
        public void ParseRootTable()
        {
            logger.InfoH($"ENTER");

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

            context.keyComboPool.SetNonTerminalMarkForSubkeys();
            if (Logger.IsInfoHEnabled && logger.IsInfoHPromoted) {
                context.keyComboPool.DebugPrint();
            }

            if (context.bRewriteEnabled) {
                // 書き換えノードがあったら、SandSの疑似同時打鍵サポートをOFFにしておく
                Settings.SandSEnablePostShift = false;
            }

            outputNewLines();
            addMyCharFunctionInRootStrokeTable();

            logger.InfoH($"LEAVE: KeyCombinationPool.Count={context.keyComboPool.Count}");
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
                var context = new ParserContext(tableLines, pool, primary);
                var parser = new RootTableParser(context);
                parser.ParseRootTable();
                writeAllLines(outFilename, context.OutputLines);
                if (Logger.IsInfoHEnabled) writeAllLines($"tmp/parsedTableFile{(primary ? 1 : 2)}.txt", context.tableLines.GetLines());
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
