using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KanchokuWS.Domain;
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
            return placeHolders._safeGet(key._toLower(), -1);
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
            placeHolders["q"] = 10;
            placeHolders["w"] = 11;
            placeHolders["e"] = 12;
            placeHolders["r"] = 13;
            placeHolders["t"] = 14;
            placeHolders["y"] = 15;
            placeHolders["u"] = 16;
            placeHolders["i"] = 17;
            placeHolders["o"] = 18;
            placeHolders["p"] = 19;
            placeHolders["a"] = 20;
            placeHolders["s"] = 21;
            placeHolders["d"] = 22;
            placeHolders["f"] = 23;
            placeHolders["g"] = 24;
            placeHolders["h"] = 25;
            placeHolders["j"] = 26;
            placeHolders["k"] = 27;
            placeHolders["l"] = 28;
            placeHolders[";"] = 29;
            placeHolders["sc"] = 29;
            placeHolders["z"] = 30;
            placeHolders["x"] = 31;
            placeHolders["c"] = 32;
            placeHolders["v"] = 33;
            placeHolders["b"] = 34;
            placeHolders["n"] = 35;
            placeHolders["m"] = 36;
            placeHolders[","] = 37;
            placeHolders["cm"] = 37;
            placeHolders["."] = 38;
            placeHolders["pd"] = 38;
            placeHolders["/"] = 39;
            placeHolders["sl"] = 39;
            placeHolders["space"] = 40;
            placeHolders["-"] = 41;
            placeHolders["hp"] = 41;
            placeHolders["@"] = 44;
            placeHolders["at"] = 44;
            placeHolders[":"] = 46;
            placeHolders["cl"] = 46;
            placeHolders["ej"] = DecoderKeyVsVKey.GetFuncDecKeyByName("zenkaku");
            placeHolders["hz"] = DecoderKeyVsVKey.GetFuncDecKeyByName("zenkaku");
            placeHolders["tab"] = DecoderKeyVsVKey.GetFuncDecKeyByName("tab");
            placeHolders["caps"] = DecoderKeyVsVKey.GetFuncDecKeyByName("caps");
            placeHolders["capslock"] = DecoderKeyVsVKey.GetFuncDecKeyByName("caps");
            placeHolders["alnum"] = DecoderKeyVsVKey.GetFuncDecKeyByName("alnum");
            placeHolders["eisu"] = DecoderKeyVsVKey.GetFuncDecKeyByName("alnum");
            placeHolders["nfer"] = DecoderKeyVsVKey.GetFuncDecKeyByName("nfer");
            placeHolders["muhenkan"] = DecoderKeyVsVKey.GetFuncDecKeyByName("nfer");
            placeHolders["xfer"] = DecoderKeyVsVKey.GetFuncDecKeyByName("xfer");
            placeHolders["henkan"] = DecoderKeyVsVKey.GetFuncDecKeyByName("xfer");
            placeHolders["kana"] = DecoderKeyVsVKey.GetFuncDecKeyByName("kana");
            placeHolders["imeon"] = DecoderKeyVsVKey.GetFuncDecKeyByName("imeon");
            placeHolders["imeoff"] = DecoderKeyVsVKey.GetFuncDecKeyByName("imeoff");
        }
    }

    // include/load ブロック情報のスタック
    class BlockInfoStack
    {
        private static Logger logger = Logger.GetLogger();

        class BlockInfo
        {
            public string DirPath;          // インクルードする場合の起動ディレクトリ
            public string BlockName;        // ファイル名やブロック名
            public int OrigLineNumber;      // ブロックの開始行番号(0起点)
            public int CurrentOffset;       // 当ブロック内での行番号を算出するための、真の起点から現在行におけるオフセット行数

            public BlockInfo(string dirPath, string name, int lineNum, int off)
            {
                DirPath = dirPath;
                BlockName = name;
                OrigLineNumber = lineNum;
                CurrentOffset = off;
            }
        }

        List<BlockInfo> blockInfoList = new List<BlockInfo>();

        public int Size() {
            return blockInfoList.Count;
        }

        public void Clear()
        {
            blockInfoList.Clear();
        }

        public string CurrentDirPath {
            get {
                var path = blockInfoList._isEmpty() ? "(empty)" : blockInfoList.Last().DirPath;
                if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"PATH: {path}");
                return path;
            }
        }

        public string CurrentBlockName {
            get {
                var name = blockInfoList._isEmpty() ? "" : blockInfoList.Last().BlockName;
                if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"NAME: {name}");
                return name;
            }
        }

        public int CurrentOffset {
            get {
                int offset = blockInfoList._isEmpty() ? 0 : blockInfoList.Last().CurrentOffset;
                if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"OFFSET: {offset}");
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
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => string.Format("POP ENTER: nextLineNum={0}, dirPath={1}, blockName={2}, origLine={3}, offset={4}",
                nextLineNum, lastInfo.DirPath, lastInfo.BlockName, lastInfo.OrigLineNumber, lastInfo.CurrentOffset));
            int insertedTotalLineNum = nextLineNum - lastInfo.OrigLineNumber;
            blockInfoList._safePopBack();
            if (!blockInfoList._isEmpty()) {
                var newLastInfo = blockInfoList._getLast();
                newLastInfo.CurrentOffset += insertedTotalLineNum;
                if (Settings.LoggingTableFileInfo) logger.InfoH(() => string.Format("POP LEAVE: newDirPath={0}, newBlockName={1}, newOrigLine={2}, newOffset={3}",
                    newLastInfo.DirPath, newLastInfo.BlockName, newLastInfo.OrigLineNumber, newLastInfo.CurrentOffset));
            }
        }

        public bool Find(string name)
        {
            return blockInfoList.Any(x => x.BlockName == name);
        }

        public bool HasNestedLines()
        {
            return blockInfoList.Count > 1 && CurrentOffset > 0;
        }
    }

    class TableLines
    {
        private static Logger logger = Logger.GetLogger();

        private List<string> tableLines = new List<string>();

        public bool IsPrimary { get; private set; } = false;

        bool IsForKanchoku { get; set; } = false;

        bool IsForEisu => !IsForKanchoku;

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

        public string RewritePreTargetStr { get; set; }                     // 前置書き換え対象文字列

        public string RewritePostChar { get; set; }                         // 後置書き換え文字

        public void InsertAtNextPos(string s)
        {
            InsertAtPos(nextPos, s);
        }

        public void InsertAtPos(int pos, string s)
        {
            if (lineNumber < tableLines.Count) {
                if (pos >= tableLines[lineNumber].Length) pos = tableLines[lineNumber].Length;
                tableLines[lineNumber] = tableLines[lineNumber].Insert(pos, s);
            }
        }

        public void SetCurrentLine(string line)
        {
            tableLines[lineNumber] = line;
        }

        /// <summary>コンストラクタ</summary>
        public TableLines()
        {
        }

        public void Initialize(bool bPrimary, bool bForKanchoku)
        {
            IsPrimary = bPrimary;
            IsForKanchoku = bForKanchoku;
            tableLines.Clear();
            blockInfoStack.Clear();
            blockInfoStack.Push(KanchokuIni.Singleton.KanchokuDir, "", 0);
        }

        private List<string> readAllLines(string filename, bool bInclude)
        {
            List<string> lines = new List<string>();
            if (filename._notEmpty()) {
                var includeFilePath = blockInfoStack.CurrentDirPath._joinPath(filename._canonicalPath());
                if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"ENTER: includeFilePath={includeFilePath}");
                var contents = Helper.GetFileContent(includeFilePath, (e) => logger.Error(e._getErrorMsg()));
                if (contents._notEmpty()) {
                    lines.AddRange(contents._safeReplace("\r", "")._split('\n'));
                    rewriteKanchokuOrEisuModeBlock(lines);
                    int nextLineNum = lineNumber;
                    if (bInclude) {
                        lines.Add("#end __include__");
                        ++nextLineNum;
                    }
                    blockInfoStack.Push(includeFilePath._getDirPath(), filename, nextLineNum);
                }
            }
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"LEAVE: num of lines={lines.Count}");
            return lines;
        }

        /// <summary>漢直モード/英数モードに対して、モード外となるブロックをコメントアウトする<br/>
        ///  - bBlockForKanchokuMode : true: 漢直モード用(英数モードのみのブロックをコメントアウト)<br/>
        ///  - bBlockForEisu : true: 英数モード用(漢直モードのみのブロックをコメントアウト)
        /// </summary>
        private void rewriteKanchokuOrEisuModeBlock(List<string> lines)
        {
            bool bInKanchokuMode = true;
            bool bInEisuMode = false;
            for (int idx = 0; idx < lines.Count; ++idx) {
                var line = lines[idx];
                var strippedLowerLine = line._strip()._toLower();
                if (strippedLowerLine._notEmpty() && strippedLowerLine[0] == '#') {
                    var items = strippedLowerLine._reReplace(@"[ \t]{2,}", " ")._split(' ');
                    if (items._notEmpty()) {
                        if (items[0] == "#enablecomboonboth" || items[0] == "#enablealways" || items[0] == "#enabledalways") {
                            // #enableComboOnBoth, enableAlways: デコーダOFFでも有効
                            bInKanchokuMode = true;
                            bInEisuMode = true;
                            lines[idx] = ";;;; " + line;
                            continue;
                        } else if (items[0] == "#enablecombooneisu") {
                            // #enableComboOnEisu: 英数モード時のみ有効
                            bInKanchokuMode = false;
                            bInEisuMode = true;
                            lines[idx] = ";;;; " + line;
                            continue;
                        } else if (items[0] == "#end") {
                            if (items.Length >= 2 &&
                                items[1] == "enablecomboonboth" || items[1] == "enablealways" || items[1] == "enabledalways" || items[1] == "enablecombooneisu") {
                                bInKanchokuMode = true;
                                bInEisuMode = false;
                                lines[idx] = ";;;; " + line;
                                continue;
                            }
                        }
                    }
                }
                if ((IsForKanchoku && !bInKanchokuMode) || (IsForEisu && !bInEisuMode)) {
                    if (strippedLowerLine._notEmpty() &&
                        (strippedLowerLine[0] != '#' ||
                        (!strippedLowerLine._safeContains("sands") && !strippedLowerLine._safeContains("combination") &&
                         !strippedLowerLine._startsWith("#if") &&
                         //!strippedLowerLine._startsWith("#ifdef") && !strippedLowerLine._startsWith("#ifndef") &&
                         !strippedLowerLine._startsWith("#else") && !strippedLowerLine._startsWith("#endif")))) {
                        // コメントアウト
                        lines[idx] = ";;;; " + line;
                    }
                }
            }
        }

        /// <summary>
        /// tableLinesをクリアしてから、ファイルの全行を読み込んで tableLnes に挿入する<br/>
        /// bForKanchokuフラグによって、それぞれのモードに無関係のところをコメントアウトする
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="bForKanchoku"></param>
        public void ReadAllLines(string filename, bool bPrimary, bool bForKanchoku)
        {
            Initialize(bPrimary, bForKanchoku);
            tableLines.AddRange(readAllLines(filename, false));
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CurrentLine:{LineNumber}:{CurrentLine}");
        }

        /// <summary>
        /// ファイルをインクルードする<br/>
        /// bForKanchokuフラグによって、それぞれのモードに無関係のところをコメントアウトする
        /// </summary>
        /// <param name="bForKanchoku"></param>
        public void IncludeFile() {
            if (Settings.LoggingTableFileInfo) logger.InfoH("CALLED");
            ReadWordOrString();
            var filename = CurrentStr;
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"INCLUDE: lineNum={LineNumber}, {filename}");
            if (filename._notEmpty()) {
                var lines = readAllLines(filename, true);
                if (lines._isEmpty()) {
                    logger.Error($"Can't open: {filename}");
                    FileOpenError(filename);
                } else {
                    tableLines.InsertRange(lineNumber + 1, lines);
                    if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"INCLUDE: {lines.Count} lines included");
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
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CALLED: {blockName}");
            List<string> lines = null;
            if (blockName._isEmpty()) {
                ParseError("StoreLineBlock: blockName empty");
            } else {
                lines = new List<string>();
                linesMap[blockName] = lines;
                if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"SET: lineNum={LineNumber}, {blockName}");
            }
            while (GetNextLine()) {
                if (CurrentLine._reMatch(@"^#end\b")) {
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
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CALLED: |{blockName}|");
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
                    if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"InsertRange: {blockName}, {lines.Count} lines");
                    tableLines.InsertRange(LineNumber, lines);
                    blockInfoStack.Push("", blockName, LineNumber);
                }
            }
        }

        /// <summary>if(n)def else endif ブロックで偽となる方をコメントアウトする<br/>
        /// - which : true = else側をコメントアウト; false = if(n)def 側をコメントアウト
        /// </summary>
        public void RewriteIfBlock(Dictionary<string, string> definedNames)
        {
            rewriteIfBlock(lineNumber, true, definedNames);
        }

        public int rewriteIfBlock(int lineNum, bool bOpenBlock, Dictionary<string, string> definedNames)
        {
            var line = tableLines[lineNum];
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"ENTER: lineNum={lineNum}: {line}");
            tableLines[lineNum] = ";;; " + line;
            ++lineNum;
            var items = line._reScan(@"^#\s*(if\w*)(\s+(\w+))?");
            var directive = items._getSecond();
            var arg = items._getNth(3);
            if (directive._notEmpty()) {
                bool flag;
                if (directive == "if") {
                    var defArg = arg._notEmpty() ? definedNames._safeGet(arg) : null;
                    flag = arg._notEmpty() && arg != "0" && arg._toLower() != "false" && (defArg._isEmpty() || (defArg != "0" && defArg._toLower() != "false"));
                } else {
                    flag = arg._notEmpty() && definedNames.ContainsKey(arg);
                    if (directive == "ifndef") {
                        flag = !flag;
                    }
                }
                if (flag) {
                    rewriteIfTrueBlock(lineNum, bOpenBlock, definedNames);
                } else {
                    rewriteIfFalseBlock(lineNum, bOpenBlock, definedNames);
                }
            }
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"LEAVE: lineNum={lineNum}");
            return lineNum;
        }

        // true側 (#else ～ #endif をコメントアウト)
        private int rewriteIfTrueBlock(int lineNum, bool bOpenBlock, Dictionary<string, string> definedNames)
        {
            while (lineNum < tableLines.Count) {
                int curLn = lineNum++;
                var line = tableLines[curLn];
                if (line._getFirst() == '#') {
                    var items = line._reScan(@"^#\s*(if|else|endif)");
                    var directive = items._getSecond();
                    if (directive._notEmpty()) {
                        if (directive == "if") {
                            lineNum = rewriteIfBlock(curLn, true, definedNames);
                        } else {
                            tableLines[curLn] = ";;; " + line;
                            if (bOpenBlock && directive == "else") {
                                // ここから false側
                                lineNum = rewriteIfFalseBlock(lineNum, false, definedNames);
                            }
                            break;
                        }
                    }
                }
            }
            return lineNum;
        }

        // false側 (#if ～ #else をコメントアウト)
        private int rewriteIfFalseBlock(int lineNum, bool bOpenBlock, Dictionary<string, string> definedNames)
        {
            while (lineNum < tableLines.Count) {
                var line = tableLines[lineNum];
                tableLines[lineNum] = ";;; " + line;
                ++lineNum;
                if (line._getFirst() == '#') {
                    if (line._reMatch(@"^#\s*if")) {                            // ネストされた #if
                        lineNum = rewriteIfFalseBlock(lineNum, false, definedNames);
                    } else if (bOpenBlock && line._reMatch($@"#\s*else")) {     // 同レベルのオープンな #else
                        lineNum = rewriteIfTrueBlock(lineNum, true, definedNames);
                        break;
                    } else if (line._reMatch($@"#\s*endif")) {                  // 同レベルの #endif
                        break;
                    }
                }
            }
            return lineNum;
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
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CurrentStr: \"{CurrentStr}\"");
        }

        // 何らかのデリミタが来るまで読みこんで、currentStr に格納。スラッシュは文字列に含む。バックスラッシュも含まれる。
        public void ReadBareString(char c = '\0') {
            var sb = new StringBuilder();
            bool isOutputChar() { return c == '/' || c == '\\' || (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c >= 0x80; }
            if (c != '\0') {
                if (!isOutputChar()) return;
                sb.Append(c);
            }
            while (true) {
                c = PeekNextChar();
                if (!isOutputChar()) break;
                if (c == '\\') AdvanceCharPos(1);       // バックスラッシュの場合は、単純にそれを読み飛ばして次の1文字を採用する
                sb.Append(GetNextChar());
            }
            CurrentStr = sb.ToString();
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CurrentBareStr: <{CurrentStr}>");
        }

        /// <summary>
        /// 行末まで(またはコメント開始まで)を CurrentStr に格納
        /// </summary>
        /// <param name="array"></param>
        public void ReadStringToEol() {
            readStringUpto(0, false, false, ';');
        }

        /// <summary>
        /// 指定の文字が来るまで読みこんで、CurrentStr に格納。bInclude==trueなら指定文字もCurrentStrに含める
        /// </summary>
        /// <param name="array"></param>
        public void ReadStringUpto(bool bInclude, params char[] array) {
            readStringUpto(0, bInclude, true, array);
        }

        /// <summary>
        /// 指定の文字が来るまで読みこんで、CurrentStr に格納。ポインタはデリミタの位置を指している
        /// </summary>
        /// <param name="array"></param>
        private void readStringUpto(int checkPos, bool bInclude, bool bErrorReport, params char[] array) {
            var sb = new StringBuilder();
            int pos = 0;
            while (true) {
                char ch = PeekNextChar();
                if (ch == '\r' || ch == '\n' || ch == 0) {
                    if (bErrorReport) ParseError("readStringUpto: unexpected EOL or EOF");
                    break;
                }
                if (pos >= checkPos && array._findIndex(ch) >= 0) {
                    // 文字列の終わり
                    if (bInclude) sb.Append(GetNextChar());
                    break;
                }
                if (ch == '\\') AdvanceCharPos(1);      // 最初の「\」は、単に読みとばす
                sb.Append(GetNextChar());
                ++pos;
            }
            CurrentStr = sb.ToString();
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"LEAVE: {CurrentStr}");
        }

        /// <summary>
        /// プレースホルダー名を読みこんで、CurrentStr に格納。ポインタはデリミタの位置を指している
        /// </summary>
        /// <param name="array"></param>
        public void ReadPlaceHolderName() {
            CurrentStr = "";
            if (PeekNextChar(0) == '$') {
                // '$' と次の1文字は必ずプレースホルダーに含める
                readStringUpto(2, false, true, ',', '>');
            }
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"LEAVE: {CurrentStr}");
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
                sb.Append(GetNextChar());
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
        public OutputString ReadWordOrString()
        {
            CurrentStr = "";
            bool bBare = false;
            if (nextPos < CurrentLine._safeLength()) {
                char c = SkipSpace();
                if (c > ' ') {
                    if (c == '"') {
                        ReadString();
                    } else if (c == ';' || (c == '/' && PeekNextChar() == '/')) {
                        SkipToEndOfLine();
                    } else {
                        CurrentStr = readWordSub(c);
                        bBare = true;
                    }
                }
            }
            return new OutputString(CurrentStr, bBare);
        }

        public char PeekNextChar(int offset = 0) {
            return ((nextPos + offset) < CurrentLine.Length) ? CurrentLine[nextPos + offset] : '\n';
        }

        public char PeekPrevChar() {
            return (nextPos > 0 && nextPos - 1 < CurrentLine.Length) ? CurrentLine[nextPos - 1] : '\0';
        }

        public char GetNextChar() {
            if (nextPos > CurrentLine.Length) {
                ++lineNumber;
                if (lineNumber >= tableLines.Count) {
                    return CurrentChar = '\0';
                }
                nextPos = 0;
                if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CurrentLine:{LineNumber}:{CurrentLine}");
            }
            if (nextPos < CurrentLine.Length) {
                CurrentChar = CurrentLine[nextPos++];
            } else {
                ++nextPos;
                CurrentChar = '\n';
            }
            return CurrentChar;
        }

        public void AdvanceCharPos(int offset)
        {
            nextPos += offset;
        }

        public bool GetNextLine() {
            ++lineNumber;
            if (lineNumber >= tableLines.Count) {
                return false;
            }
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CurrentLine:{LineNumber}:{CurrentLine}");
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

        string parsedFileAndLinenum() {
            return blockInfoStack.HasNestedLines() ? $"\r\n(tmp/parsedTableFile{(IsForKanchoku ? 'K' : 'A')}{(IsPrimary ? 1 : 2)}.txt の {LineNumber}行目)\r\n" : "";
        }

        int calcErrorColumn() {
            if (nextPos == 0 && lineNumber > 0) return tableLines[lineNumber - 1].Count();
            return nextPos;
        }

        // 解析エラー
        public void ParseError(string msg = null) {
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("{0}{1} {2} の {3}行{4}{5}文字目({6})がまちがっているようです：\r\n\r\n> {7} ...",
                msg._notEmpty() ? msg + "\r\n\r\n" : "",
                blockOrFile(),
                blockInfoStack.CurrentBlockName,
                calcErrorLineNumber(),
                parsedFileAndLinenum(),
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
            if (Settings.LoggingTableFileInfo) logger.InfoH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("引数 '{0}' が不正です。\r\nテーブルファイル {1} の {2}行目{3}がまちがっているようです：\r\n\r\n> {4} ...",
                arg, blockInfoStack.CurrentBlockName, calcErrorLineNumber(), parsedFileAndLinenum(), CurrentLine._safeSubstring(0, 50)));
        }

        // loadループエラー
        public void LoadLoopError(string name) {
            if (Settings.LoggingTableFileInfo) logger.InfoH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("ブロック {0} のロードがループしています。\r\n{1} {2} の {3}行目{4}がまちがっているようです：\r\n\r\n> {5} ...",
                name, blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), parsedFileAndLinenum(), CurrentLine._safeSubstring(0, 50)));
        }

        // storeブロックが存在しない
        public void NoSuchBlockError(string name) {
            if (Settings.LoggingTableFileInfo) logger.InfoH($"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("指定されたブロック {0} が存在しません。\r\n{1} {2} の {3}行目{4}がまちがっているようです：\r\n\r\n> {5} ...",
                name, blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), parsedFileAndLinenum(), CurrentLine._safeSubstring(0, 50)));
        }

        // ファイルの読み込みに失敗した場合
        public void FileOpenError(string filename) {
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleError(string.Format("ファイル {0} を読み込めません。\r\nテーブルファイル {1} の {2}行目{3}がまちがっているようです：\r\n\r\n> {4} ...",
                filename, blockInfoStack.CurrentBlockName, calcErrorLineNumber(), parsedFileAndLinenum(), CurrentLine._safeSubstring(0, 50)));
        }

        // ノードの重複が発生した場合
        public void NodeDuplicateWarning() {
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(string.Format("{0} {1} の {2}行目{3}でノードの重複が発生しました。意図したものであれば無視してください\r\n(#ignoreWarning overwrite を記述するとこの警告が出なくなります)：\r\n\r\n> {4} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), parsedFileAndLinenum(), CurrentLine._safeSubstring(0, 50)));
        }

        // カラム0で予期しないLBRACEが発生
        public void UnexpectedLeftBraceAtColumn0Warning() {
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(
                string.Format("{0} {1} の {2}行目{3}の行頭にネストされた '{{' があります。意図したものであれば無視してください\r\n(#ignoreWarning braceLevel を記述するとこの警告が出なくなります)：\r\n\r\n> {4} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), parsedFileAndLinenum(), CurrentLine._safeSubstring(0, 50)));
        }

        // カラム0で予期しないRBRACEが発生
        public void UnexpectedRightBraceAtColumn0Warning() {
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"lineNumber={lineNumber}, nextPos={nextPos}");
            handleWarning(
                string.Format("{0} {1} の {2}行目{3}の行頭にまだネスト中の '}}' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述するとこの警告が出なくなります)：\r\n\r\n> {4} ...",
                blockOrFile(), blockInfoStack.CurrentBlockName, calcErrorLineNumber(), parsedFileAndLinenum(), CurrentLine._safeSubstring(0, 50)));
        }

        // エラー処理
        void handleError(string msg) {
            logger.Error(msg);
            logger.Error("lines=\n" + MakeErrorLines());
            // エラーメッセージを投げる
            Error(msg);
        }

        // 警告の出力数をカウントし、最大数に達したらそれ以上は出力しないようにする
        int numWarnLog = 0;

        // 警告処理
        void handleWarning(string msg) {
            if (numWarnLog++ < 10) {
                logger.WarnH(msg);
                logger.WarnH("lines=\n" + MakeErrorLines());
                // エラーメッセージを投げる
                Warn(msg);
            }
        }

        public string getErrorMessage()
        {
            return errorMsg.ToString();
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
    /// グローバルなコンテキスト情報を格納するクラス
    /// </summary>
    class ParserContext
    {
        public TableLines tableLines;

        public TOKEN currentToken = TOKEN.IGNORE;    // 最後に読んだトークン

        public int arrowIndex = -1;                  // ARROWインデックス

        public bool bPrimary => tableLines.IsPrimary;                        // 主テーブルか

        public bool bRewriteEnabled = false;         // 書き換えノードがあった

        public Dictionary<string, string> definedNames = new Dictionary<string, string>();

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

        // COMBO用のDecKeyの開始位置
        public int comboDeckeyStart = 0;

        // 出力用のバッファ
        public List<string> OutputLines = new List<string>();

        // プレースホルダー
        public PlaceHolders placeHolders = new PlaceHolders();

        public bool bIgnoreWarningAll = false;
        public bool bIgnoreWarningBraceLevel = false;
        public bool bIgnoreWarningOverwrite = false;
        public int braceLevel = 0;

        public HashSet<int> sequentialShiftKeys = new HashSet<int>();

        // 真のルートテーブルノード
        public Node rootTableNode = Node.MakeTreeNode(true);

        // 漢字置換マップ
        public Dictionary<string, string> kanjiConvMap = new Dictionary<string, string>();

        /// <summary>漢字置換ファイルによる漢字の書きえ</summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public string ConvertKanji(string k) {
            return kanjiConvMap._safeGet(k)._orElse(k);
        }

        /// <summary>キー文字に到るストロークパスを得る辞書</summary>
        public Dictionary<string, CStrokeList> strokePathDict = new Dictionary<string, CStrokeList>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        /// <param name="comboDkStart"></param>
        private ParserContext(TableLines tableLines, KeyCombinationPool pool, int comboDkStart)
        {
            this.tableLines = tableLines;
            keyComboPool = pool;
            comboDeckeyStart = comboDkStart;
        }

        public static void CreateSingleton(TableLines tableLines, KeyCombinationPool pool, int comboDkStart)
        {
            Singleton = new ParserContext(tableLines, pool, comboDkStart);
        }

        public static void FinalizeSingleton()
        {
            Singleton = null;
        }

        public static ParserContext Singleton { get; private set; }
    }
}
