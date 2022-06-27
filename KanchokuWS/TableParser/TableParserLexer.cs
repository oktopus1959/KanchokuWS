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

    /// <summary>
    /// テーブル解析器のコンテキストデータ
    /// </summary>
    class TableParserContext
    {
        private static Logger logger = Logger.GetLogger(true);

        protected ParserContext context;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableParserContext(ParserContext ctx)
        {
            context = ctx;
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
        protected Dictionary<string, List<string>> linesMap => context.linesMap;
        protected KeyCombinationPool keyComboPool => context.keyComboPool;
        protected List<string> OutputLines => context.OutputLines;
        protected Dictionary<string, int> placeHolders => context.placeHolders;
        protected bool bRewriteTable {
            get { return context.bRewriteTable; }
            set { context.bRewriteTable = value; }
        }

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
    /// テーブル字句解析器
    /// </summary>
    class TableParserLexer : TableParserContext
    {
        private static Logger logger = Logger.GetLogger(true);

        StrokeTableNode _rootTableNode;

        int _shiftPlane = -1;

        protected StrokeTableNode rootTableNode => _rootTableNode != null ? _rootTableNode : context.rootTableNode;

        protected int shiftPlane {
            get { return _shiftPlane >= 0 ? _shiftPlane : context.shiftPlane; }
            set { context.shiftPlane = _shiftPlane = value; }
        }

        protected List<int> strokeList = new List<int>();

        protected int depth => strokeList.Count;

        protected int shiftDecKey(int deckey)
        {
            return deckey >= DecoderKeys.PLANE_DECKEY_NUM ? deckey : deckey + shiftPlane * DecoderKeys.PLANE_DECKEY_NUM;
        }

        protected string getNthRootNodeString(int n)
        {
            int idx = shiftDecKey(n);
            return (rootTableNode.getNth(idx)?.getString())._stripDq()._toSafe();
        }

        protected string leaderStr => strokeList.Count > 1 ? strokeList.Take(strokeList.Count - 1).Select(x => getNthRootNodeString(x))._join("") : "";

        protected string pathStr => strokeList.Count > 0 ? strokeList.Select(x => getNthRootNodeString(x))._join("") : "";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableParserLexer(ParserContext ctx, List<int> stkList, StrokeTableNode rootNode = null, int shiftPlane = -1)
            : base(ctx)
        {
            _rootTableNode = rootNode;
            _shiftPlane = shiftPlane;
            if (stkList._notEmpty()) {
                strokeList.AddRange(stkList);
            }
        }

        /// <summary>
        /// 未出力なノードを OutputLines に書き出す
        /// </summary>
        protected void outputNewLines()
        {
            var list = new List<int>();
            rootTableNode.OutputLine(OutputLines, "");
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
        protected void readNextToken(bool bSkipNL = false)
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

    }
}
