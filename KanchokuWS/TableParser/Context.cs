using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.TableParser
{
    using ShiftKeyKind = ComboShiftKeyPool.ComboKind;

    // プレースホルダー
    class PlaceHolders
    {
        Dictionary<string, int> placeHolders = new Dictionary<string, int>();

        public PlaceHolders()
        {
            Initialize();
        }

        public void Put(string key, int value)
        {
            placeHolders[key] = value;
        }

        public int Get(string key)
        {
            return placeHolders._safeGet(key, -1);
        }

        public void Initialize()
        {
            placeHolders["1"] = 0;
            placeHolders["2"] = 1;
            placeHolders["3"] = 2;
            placeHolders["4"] = 3;
            placeHolders["5"] = 4;
            placeHolders["6"] = 5;
            placeHolders["7"] = 6;
            placeHolders["8"] = 7;
            placeHolders["9"] = 8;
            placeHolders["0"] = 9;
            placeHolders["Q"] = 10;
            placeHolders["q"] = 10;
            placeHolders["W"] = 11;
            placeHolders["w"] = 11;
            placeHolders["E"] = 12;
            placeHolders["e"] = 12;
            placeHolders["R"] = 13;
            placeHolders["r"] = 13;
            placeHolders["T"] = 14;
            placeHolders["t"] = 14;
            placeHolders["Y"] = 15;
            placeHolders["y"] = 15;
            placeHolders["U"] = 16;
            placeHolders["u"] = 16;
            placeHolders["I"] = 17;
            placeHolders["i"] = 17;
            placeHolders["O"] = 18;
            placeHolders["o"] = 18;
            placeHolders["P"] = 19;
            placeHolders["p"] = 19;
            placeHolders["A"] = 20;
            placeHolders["a"] = 20;
            placeHolders["S"] = 21;
            placeHolders["s"] = 21;
            placeHolders["D"] = 22;
            placeHolders["d"] = 22;
            placeHolders["F"] = 23;
            placeHolders["f"] = 23;
            placeHolders["G"] = 24;
            placeHolders["g"] = 24;
            placeHolders["H"] = 25;
            placeHolders["h"] = 25;
            placeHolders["J"] = 26;
            placeHolders["j"] = 26;
            placeHolders["K"] = 27;
            placeHolders["k"] = 27;
            placeHolders["L"] = 28;
            placeHolders["l"] = 28;
            placeHolders[";"] = 29;
            placeHolders["sc"] = 29;
            placeHolders["Z"] = 30;
            placeHolders["z"] = 30;
            placeHolders["X"] = 31;
            placeHolders["x"] = 31;
            placeHolders["C"] = 32;
            placeHolders["c"] = 32;
            placeHolders["V"] = 33;
            placeHolders["v"] = 33;
            placeHolders["B"] = 34;
            placeHolders["b"] = 34;
            placeHolders["N"] = 35;
            placeHolders["n"] = 35;
            placeHolders["M"] = 36;
            placeHolders["m"] = 36;
            placeHolders[","] = 37;
            placeHolders["cm"] = 37;
            placeHolders["."] = 38;
            placeHolders["pd"] = 38;
            placeHolders["/"] = 39;
            placeHolders["sl"] = 39;
            placeHolders["space"] = 40;
            placeHolders["Space"] = 40;
            placeHolders["nfer"] = VirtualKeys.GetFuncDeckeyByName("nfer");
            placeHolders["Nfer"] = VirtualKeys.GetFuncDeckeyByName("nfer");
            placeHolders["NFER"] = VirtualKeys.GetFuncDeckeyByName("nfer");
            placeHolders["xfer"] = VirtualKeys.GetFuncDeckeyByName("xfer");
            placeHolders["Xfer"] = VirtualKeys.GetFuncDeckeyByName("xfer");
            placeHolders["XFER"] = VirtualKeys.GetFuncDeckeyByName("xfer");
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

        public string RewriteTargetStr { get; set; }                    // 前置書き換え対象文字列

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

        // ifdef else endif ブロックで真となる方をロードする
        public void RewriteIfdefBlock(bool which)
        {
            logger.DebugH(() => $"CALLED: block={which}");
            int lineNum = lineNumber;
            tableLines[lineNum] = ";; " + tableLines[lineNum];
            ++lineNum;
            if (which) {
                while (lineNum < tableLines.Count) {
                    if (tableLines[lineNum]._startsWith("#else")) {
                        while (lineNum < tableLines.Count) {
                            tableLines[lineNum] = ";; " + tableLines[lineNum];
                            if (tableLines[lineNum]._startsWith(";; #endif")) {
                                break;
                            }
                            ++lineNum;
                        }
                        return;
                    } else if (tableLines[lineNum]._startsWith("#endif")) {
                        tableLines[lineNum] = ";; " + tableLines[lineNum];
                        return;
                    }
                    ++lineNum;
                }
            } else {
                while (lineNum < tableLines.Count) {
                    tableLines[lineNum] = ";; " + tableLines[lineNum];
                    if (tableLines[lineNum]._startsWith(";; #else")) {
                        while (lineNum < tableLines.Count) {
                            if (tableLines[lineNum]._startsWith("#endif")) {
                                tableLines[lineNum] = ";; #endif";
                                return;
                            }
                            ++lineNum;
                        }
                        return;
                    } else if (tableLines[lineNum]._startsWith(";; #endif")) {
                        return;
                    }
                    ++lineNum;
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
            logger.DebugH(() => $"LEAVE: {CurrentStr}");
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

        /// <summary>
        /// 指定の文字が来るまで読みこんで、CurrentStr に格納。ポインタはデリミタの位置を指している
        /// </summary>
        /// <param name="array"></param>
        public void ReadStringUpto(params char[] array) {
            var sb = new StringBuilder();
            while (true) {
                char ch = PeekNextChar();
                if (ch == '\r' || ch == '\n' || ch == 0) {
                    ParseError("ReadRewriteTargetString: unexpected EOL or EOF");
                    break;
                }
                if (array._findIndex(ch) >= 0) {
                    // 文字列の終わり
                    break;
                }
                if (ch == '\\') {
                    // 最初の「\」は、単に読みとばす
                    GetNextChar();
                }
                sb.Append(GetNextChar());
            }
            CurrentStr = sb.ToString();
            logger.DebugH(() => $"LEAVE: {CurrentStr}");
        }

        /// <summary>
        /// プレースホルダー名を読みこんで、CurrentStr に格納。ポインタはデリミタの位置を指している
        /// </summary>
        /// <param name="array"></param>
        public void ReadPlaceHolderName() {
            CurrentStr = "";
            if (PeekNextChar(0) == '$') {
                if (";,./".IndexOf(PeekNextChar(1)) >= 0) {   // $; $, $. $/ はプレースホルダー名として有効
                    ReadStringUpto('|', '>');
                } else {
                    ReadStringUpto(',', '|', '>');
                }
            }
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

        public char PeekNextChar(int offset = 0) {
            return ((nextPos + offset) < CurrentLine.Length) ? CurrentLine[nextPos + offset] : '\n';
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

    class ParserContext
    {
        public TableLines tableLines;

        public TOKEN currentToken = TOKEN.IGNORE;    // 最後に読んだトークン

        public int arrowIndex = -1;                  // ARROWインデックス

        public bool bPrimary;                        // 主テーブルか

        public bool bRewriteEnabled = false;         // 書き換えノードがあった

        public HashSet<string> definedNames = new HashSet<string>();

        // 同時打鍵定義ブロックの中か
        //public bool isInCombinationBlock => shiftKeyKind != ShiftKeyKind.None;
        public bool isInCombinationBlock => ComboShiftKeyPool.IsComboShift(shiftKeyKind);

        // 連続シフト可な同時打鍵定義ブロックの中か
        public bool isInSuccCombinationBlock => ComboShiftKeyPool.IsSuccessiveShift(shiftKeyKind);

        // 同時打鍵によるシフト種別
        public ShiftKeyKind shiftKeyKind = ShiftKeyKind.None;

        // 定義列マップ
        public Dictionary<string, List<string>> linesMap = new Dictionary<string, List<string>>();

        // シフト面 -- 0:シフト無し、1:通常シフト、2:ShiftA, 3:ShiftB, ...
        public int shiftPlane = 0;

        // 対象となる KeyComboPool
        public KeyCombinationPool keyComboPool;

        // 出力用のバッファ
        public List<string> OutputLines = new List<string>();

        // プレースホルダー
        public PlaceHolders placeHolders = new PlaceHolders();

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
}
