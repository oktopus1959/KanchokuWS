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
    /// テーブル字句解析器
    /// </summary>
    class TableParserTokenizer : TableParserContext
    {
        private static Logger logger = Logger.GetLogger();

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
        public TableParserTokenizer(ParserContext ctx, List<int> stkList, StrokeTableNode rootNode = null, int shiftPlane = -1)
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
                        if (CurrentStr._notEmpty()) definedNames.Add(CurrentStr);
                        if (CurrentStr._toLower()._equalsTo("defguide")) {
                            handleStrokePosition();
                        }
                    } else if (lcStr == "ifdef") {
                        ReadWord();
                        bool flag = CurrentStr._notEmpty() && definedNames.Contains(CurrentStr);
                        RewriteIfdefBlock(flag);
                    } else if (lcStr == "ifndef") {
                        ReadWord();
                        bool flag = CurrentStr._notEmpty() && definedNames.Contains(CurrentStr);
                        RewriteIfdefBlock(!flag);
                    } else if (lcStr == "else") {
                    } else if (lcStr == "endif") {
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
                    } else if (lcStr == "disableextkey") {
                        ReadWord();
                        if (CurrentStr._notEmpty()) VirtualKeys.AddDisabledExtKey(CurrentStr);
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
                        char c = PeekNextChar();
                        if (c == '*') {
                            // 矢印束記法
                            GetNextChar();
                            if (parseArrowBundle()) return TOKEN.ARROW_BUNDLE;
                        } else {
                            // 矢印記法
                            if (parseArrow()) return TOKEN.ARROW;
                        }
                    }
                    break;

                    case '$':
                        ReadBareString();
                        if (CurrentStr._notEmpty()) return TOKEN.PLACE_HOLDER;      // '$' を除く文字列
                        break;

                    case '%':
                        if (depth != 0) {
                            ParseError("'%'で始まる前置書き換え記法はテーブルがネストされた位置では使えません。");
                        } else if (parseArrow(true)) {
                            bRewriteEnabled = true;
                            return TOKEN.REWRITE_PRE;
                        }
                        break;

                    case '&':
                        if (depth != 0) {
                            ParseError("'%'で始まる前置書き換え記法はテーブルがネストされた位置では使えません。");
                        } else if (parseArrow()) {
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
                if (plane > 0) {
                    shiftPlane = plane;
                } else {
                    shiftPlane = 2;
                    VirtualKeys.AssignSanSPlane(shiftPlane);
                }
            } else if (word._startsWith("enable")) {
                Settings.SandSEnabled = true;
            } else if (word._startsWith("disable")) {
                Settings.SandSEnabled = false;
                VirtualKeys.AddDisabledExtKey("space");
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
        protected bool parseArrow(bool bRewritePre = false)
        {
            int shiftOffset = -1;
            int funckeyOffset = 0;
            bool bShiftPlane = false;

            if (PeekNextChar() == '"') {
                ReadString();
            } else if (PeekNextChar() == '$') {
                ReadPlaceHolderName();
            } else {
                ReadStringUpto('>', ',', '|');
            }
            if (PeekNextChar() == '>') {
                // エラーがあったら即時 return できるように、前もって進めておく
                GetNextChar();
            }

            string s = CurrentStr._strip();
            if (s._isEmpty()) {
                ParseError($"parseArrow: EMPTY");
                return false;
            }

            char c = s[0];

            if (c == '$') {
                // TOKEN.PLACE_HOLDER
                arrowIndex = placeHolders.Get(s._safeSubstring(1));
                if (arrowIndex < 0) {
                    ParseError($"parseArrow: 定義されていないプレースホルダー: {s}");
                    return false;
                }
            } else {
                arrowIndex = s._parseInt(-1);
                if (arrowIndex < 0) {
                    arrowIndex = s._safeSubstring(1)._parseInt(-1);
                    if (arrowIndex < 0) {
                        // 前置書き換え対象文字列
                        if (bRewritePre) {
                            RewriteTargetStr = s;
                            return true;
                        } else {
                            ParseError($"parseArrow: 前置書き換え以外の文字列: {s}");
                            return false;
                        }
                    }
                    if (c == 'N' || c == 'n') {
                        shiftOffset = 0;
                    } else if (c == 'S' || c == 's' || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')) {
                        shiftOffset = VirtualKeys.CalcShiftOffset(c);
                    } else if (c == 'X' || c == 'x') {
                        shiftOffset = 0;
                        funckeyOffset = DecoderKeys.FUNC_DECKEY_START;
                    } else if (c == 'P' || c == 'P') {
                        bShiftPlane = true;
                    } else {
                        ParseError($"parseArrow: 不正なプレーン指定: {s}");
                        return false;
                    }
                }
            }

            arrowIndex += funckeyOffset;
            arrowIndex %= DecoderKeys.PLANE_DECKEY_NUM;    // 後で Offset を足すので Modulo 化しておく
            if (bShiftPlane) {
                // プレーン指定の場合
                shiftPlane = arrowIndex;
                if (shiftPlane >= DecoderKeys.ALL_PLANE_NUM) ParseError($"parseArrow: shiftPlane out of range: {shiftPlane}");
                return false;
            } else {
                if (shiftOffset < 0) {
                    // シフト面のルートノードで明示的にシフトプレフィックスがなければ、shiftOffset をセット
                    shiftOffset = (shiftPlane > 0 && depth == 0) ? shiftPlane * DecoderKeys.PLANE_DECKEY_NUM : 0;
                }
                arrowIndex += shiftOffset;
                if (arrowIndex < 0 || arrowIndex >= DecoderKeys.COMBO_DECKEY_END) {
                    ParseError($"parseArrow: arrowIndex out of range: {arrowIndex}");
                    return false;
                }
            }
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

            ReadStringUpto('>');
            string s = CurrentStr._strip();
            if (s._isEmpty()) {
                ParseError($"parseArrowBundle: arrowIndex is EMPTY");
            } else {
                c = s[0];
                if (c == '$') {
                    // TOKEN.PLACE_HOLDER
                    arrowIndex = placeHolders.Get(s._safeSubstring(1));
                } else {
                    arrowIndex = s._parseInt(-1);
                }
                if (arrowIndex < 0 || arrowIndex >= DecoderKeys.PLANE_DECKEY_NUM) ParseError($"parseArrowBundle: arrowIndex is out of range: {arrowIndex}");
            }
            if (PeekNextChar() == '>') {
                GetNextChar();
            } else {
                ParseError($"parseArrowBundle-2: '>' is expected, but {c}");
            }
            return true;
        }

    }
}
