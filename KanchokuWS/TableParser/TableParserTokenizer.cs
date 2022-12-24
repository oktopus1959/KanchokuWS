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
    class TableParserTokenizer : ContextAccessor
    {
        private static Logger logger = Logger.GetLogger();

        //protected StrokeTableNode rootTableNode;

        //private int _shiftPlane = -1;

        //protected StrokeTableNode rootTableNode => _rootTableNode != null ? _rootTableNode : context.rootTableNode;

        /// <summary>ルートノードテーブルを持つパーザか</summary>
        public virtual bool HasRootTable => false;

        ///// <summary>漢直モードのためのパーザか</summary>
        //protected bool IsKanchokuModeParser { get; set; } = false;

        private int shiftPlane {
            //get { return _shiftPlane >= 0 ? _shiftPlane : context.shiftPlane; }
            //set { context.shiftPlane = _shiftPlane = value; }
            get { return Context.shiftPlane; }
            set { Context.shiftPlane = value; }
        }

        protected OutputString[] StringPair = new OutputString[2];

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableParserTokenizer()
            : base()
        {
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

        /// <summary>トークンひとつ読んで currentToken にセット</summary>
        protected void readNextToken(bool bSkipNL = false)
        {
            currentToken = getToken(bSkipNL);
        }

        /// <summary>トークンを読む</summary>
        TOKEN getToken(bool bSkipNL)
        {
            ArrowIndex = -1;
            while (true) {
                ClearCurrentStr();
                char ch = GetNextChar();

                if (ch == '\0') {
                    // ファイルの終わり
                    return TOKEN.END;
                }

                if (ch == '#') {
                    // '#' 以降は、ディレクティブまたは行末までコメント
                    ReadWord();
                    var lcStr = CurrentStr._toLower();
                    if (lcStr == "include") {
                        // #include: ファイルのインクルード
                        IncludeFile();
                    } else if (lcStr == "define") {
                        // #define: 定数の定義
                        //outputNewLines();
                        //OutputLines.Add(CurrentLine); // define はすべてフロント側で処理するようにした
                        ReadWord();
                        if (CurrentStr._startsWith("display")) OutputLines.Add(";; " + CurrentLine); // display-name だけ出力する
                        if (CurrentStr._notEmpty()) definedNames.Add(CurrentStr);
                        var lcDef = CurrentStr._toLower();
                        if (lcDef._equalsTo("defguide")) {
                            // 'defguide': 配字案内
                            handleStrokePosition();
                        } else if (lcDef._startsWith("sequen")) {
                            // 'sequentialWords': 優先する順次打鍵
                            ReadStringToEol();
                            Settings.SequentialPriorityWordSet.UnionWith(CurrentStr._strip()._reSplit(@"[ ,]+"));
                            SkipToEndOfLine();
                        }
                    } else if (lcStr == "if") {
                        ReadWord();
                        bool flag = CurrentStr._notEmpty() && CurrentStr != "0" && CurrentStr._toLower() != "false";
                        RewriteIfdefBlock(flag);
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
                        // #store: #end store までの行を変数に格納
                        StoreLineBlock();
                    } else if (lcStr == "load") {
                        // #load: store で格納した行を展開
                        LoadLineBlock();
                    } else if (lcStr._startsWith("yomiconv")) {
                        // #yomiConvert: 読み変換(kw-uni側で処理)
                        ReadWord();
                        var keyword = CurrentStr._toLower();
                        if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"YomiConversion: keyword={keyword}");
                        if (keyword == "clear" || keyword == "end") {
                            kanjiConvMap?.Clear();
                        } else {
                            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"YomiConversion: {Settings.KanjiYomiFile}");
                            if (Settings.KanjiYomiFile._notEmpty()) readKanjiConvFile(Settings.KanjiYomiFile, true);
                            if (keyword == "with") {
                                ReadWordOrString();
                                if (CurrentStr._notEmpty()) {
                                    if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"YomiConversion: {CurrentStr}");
                                    readKanjiConvFile(CurrentStr, false);
                                }
                            }
                        }
                        //outputNewLines();
                        //OutputLines.Add(CurrentLine);
                    } else if (lcStr == "strokeposition") {
                        // #strokePosiion: 配字案内
                        //OutputLines.Add(CurrentLine);
                        handleStrokePosition();
                    } else if (lcStr == "extracharsposition") {
                        handleExtraCharsPosition();
                    } else if (lcStr == "noshift" || lcStr == "normal") {
                        shiftPlane = 0;
                    } else if (lcStr == "shift") {
                        // #shift: シフト面割り当て
                        shiftPlane = 1;
                    } else if (lcStr == "shifta") {
                        // #shift: 拡張シフトA面割り当て
                        shiftPlane = 2;
                        // #shift: 拡張シフトA面割り当て
                    } else if (lcStr == "shiftb") {
                        // #shift: 拡張シフトB面割り当て
                        shiftPlane = 3;
                    } else if (lcStr == "shiftc") {
                        // #shift: 拡張シフトC面割り当て
                        shiftPlane = 4;
                    } else if (lcStr == "shiftd") {
                        // #shift: 拡張シフトD面割り当て
                        shiftPlane = 5;
                    } else if (lcStr == "shifte") {
                        // #shift: 拡張シフトE面割り当て
                        shiftPlane = 6;
                    } else if (lcStr == "shiftf") {
                        // #shift: 拡張シフトF面割り当て
                        shiftPlane = 7;
                    } else if (lcStr == "combination" || lcStr == "overlapping") {
                        // #combination: 同時打鍵設定
                        ReadWord();
                        switch (CurrentStr._toLower()) {
                            case "prefix":
                            case "preshift":
                            case "ordered":
                                shiftKeyKind = ShiftKeyKind.PrefixSuccessiveShift;
                                break;
                            case "oneshot":
                                shiftKeyKind = ShiftKeyKind.UnorderedOneshotShift;
                                break;
                            case "successive":
                            case "unordered":
                            case "mutual":
                            case "":
                                shiftKeyKind = ShiftKeyKind.UnorderedSuccessiveShift;
                                break;
                            default:
                                ArgumentError(CurrentStr);
                                break;
                        }
                    //} else if (lcStr == "enablecomboonboth" || lcStr == "enablealways" || lcStr == "enabledalways") {
                    //    // #enableAlways: デコーダOFFでも有効
                    //    //bComboEffectiveAlways = true;
                    //    bComboEffectiveOnKanchokuMode = true;
                    //    bComboEffectiveOnEisuMode = true;
                    //} else if (lcStr == "enablecombooneisu") {
                    //    // #enableComboOnEisu: 英数モード時のみ有効
                    //    bComboEffectiveOnKanchokuMode = false;
                    //    bComboEffectiveOnEisuMode = true;
                    } else if (lcStr == "end") {
                        // #end: 各種ディレクティブの終了
                        ReadWord();
                        var strLower = CurrentStr._toLower();
                        switch (strLower._substring(0, 5)) {
                            case "combi":
                            case "overl":
                                shiftKeyKind = ShiftKeyKind.None;
                                break;
                            case "shift":
                            case "sands":
                                shiftPlane = 0;
                                break;
                            case "__inc":
                                if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"END INCLUDE/LOAD: lineNumber={LineNumber}");
                                EndInclude();
                                break;
                            //case "enabl":
                            //    if (strLower._endsWith("onboth") || strLower._endsWith("oneisu") || strLower._endsWith("always")) {
                            //        //bComboEffectiveAlways = false;
                            //        bComboEffectiveOnKanchokuMode = true;
                            //        bComboEffectiveOnEisuMode = false;
                            //        //if (keyComboPoolK != null) keyComboPoolK.HasComboEffectiveAlways = true;
                            //    }
                            //    break;
                        }
                    } else if (lcStr == "sands") {
                        // #SandS: SandS の有効化、無効化、面の割り当て
                        handleSandSState();
                    } else if (lcStr == "assignplane") {
                        // #assignPlane: 拡張シフトキーに対するシフト面の割り当て
                        assignShiftPlane();
                    } else if (lcStr == "set") {
                        // Settings 変数の値の変更
                        handleSettings();
                    } else if (lcStr == "disableextkey") {
                        // 拡張修飾キーの無効化
                        ReadWord();
                        if (CurrentStr._notEmpty()) VirtualKeys.AddDisabledExtKey(CurrentStr);
                    } else if (lcStr == "ignorewarning" || lcStr == "enablewarning") {
                        // 各種警告の無効化/有効化
                        bool flag = lcStr.StartsWith("ignore");
                        ReadWord();
                        var word = CurrentStr._toLower();
                        if (word._isEmpty() || word == "all") {
                            bIgnoreWarningAll = flag;
                            bIgnoreWarningBraceLevel = flag;
                            bIgnoreWarningOverwrite = flag;
                        } else if (word == "bracelevel") {
                            bIgnoreWarningBraceLevel = flag;
                        } else if (word == "overwrite") {
                            bIgnoreWarningOverwrite = flag;
                        }
                    } else {
                        // 上記以外は無視(コメント扱い) 
                        if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"#{CurrentStr}");
                    }
                    SkipToEndOfLine();
                    continue;
                }

                switch (CurrentChar) {
                    case ';':
                        // ';' 以降、行末までコメント
                        SkipToEndOfLine();
                        if (bSkipNL) break;
                        return TOKEN.NEW_LINE;

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
                        return parseSecondString(false) ? TOKEN.STRING_PAIR : TOKEN.STRING;

                    case '!':
                        // 機能キー文字列
                        if (PeekNextChar() == '{') {
                            RewindChar();
                            ReadStringUpto(true, '}');
                            return TOKEN.STRING;
                        }
                        ParseError($"getToken: unexpected char: '{CurrentChar}'");
                        SkipToEndOfLine();
                        return TOKEN.IGNORE;

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
                        if (parseArrow(true, false)) {
                            bRewriteEnabled = true;
                            return TOKEN.REWRITE_PRE;
                        }
                        break;

                    case '&':
                        if (parseArrow(false, true)) {
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
                        // BARE Stringか
                        ReadBareString(CurrentChar);
                        if (CurrentStr._notEmpty()) return parseSecondString(true) ? TOKEN.STRING_PAIR : TOKEN.BARE_STRING;

                        // エラー
                        ParseError($"getToken: unexpected char: '{CurrentChar}'");
                        return TOKEN.IGNORE;
                }
            }
        }

        // strokePosition 行を処理
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

        // extraCharsPosition 行を処理
        void handleExtraCharsPosition()
        {
            if (bPrimary) {
                Settings.StrokeHelpExtraCharsPosition1 = true;
            } else {
                Settings.StrokeHelpExtraCharsPosition2 = true;
            }
        }

        void handleSandSState()
        {
            ReadWord();
            var word = CurrentStr._toLower();
            if (word._isEmpty()) {
                logger.InfoH("SandS");
                Settings.SandSEnabledCurrently = true;
                int plane = VirtualKeys.GetSandSPlane();
                if (plane > 0) {
                    shiftPlane = plane;
                } else {
                    shiftPlane = 2;
                    VirtualKeys.AssignSanSPlane(shiftPlane);
                }
            } else if (word._startsWith("enable")) {
                Settings.SandSEnabledCurrently = true;
                logger.InfoH("SandS enabled");
            } else if (word._startsWith("disable")) {
                Settings.SandSEnabledCurrently = false;
                VirtualKeys.AddDisabledExtKey("space");
                logger.InfoH("SandS disabled");
            } else if (word == "s") {
                Settings.SandSEnabledCurrently = true;
                shiftPlane = 1;
                VirtualKeys.AssignSanSPlane(shiftPlane);
            } else if (word.Length == 1 && word[0] >= 'a' && word[0] <= 'f') {
                Settings.SandSEnabledCurrently = true;
                shiftPlane = word[0] - 'a' + 2;
                VirtualKeys.AssignSanSPlane(shiftPlane);
            } else if (word._startsWith("enabeoneshot")) {
                Settings.OneshotSandSEnabledCurrently = true;
            } else if (word._startsWith("disabeoneshot")) {
                Settings.OneshotSandSEnabledCurrently = false;
            } else if (word._startsWith("enabepostshift")) {
                Settings.SandSEnablePostShiftCurrently = true;
            } else if (word._startsWith("disabepostshift")) {
                Settings.SandSEnablePostShiftCurrently = false;
            }
        }

        //void changeSandSState(bool bEnabled)
        //{
        //    Settings.SandSEnabledCurrently = bEnabled;
        //}

        // 拡張シフトキーに対するシフト面の割り当て
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
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"CALLED: currentLine={CurrentLine}");
            ReadWord();
            var items = CurrentStr._split('=');
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"currentStr={CurrentStr}, items.Length={items._safeLength()}, items[0]={items._getFirst()}, items[1]={items._getSecond()}");
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
        protected bool parseArrow(bool bRewritePre = false, bool bRewritePost = false)
        {
            int shiftOffset = -1;
            int funckeyOffset = 0;
            bool bShiftPlane = false;

            RewritePreTargetStr = "";
            RewritePostChar = "";

            if (PeekNextChar() == '"') {
                ReadString();
            } else if (PeekNextChar() == '$') {
                ReadPlaceHolderName();
            } else {
                ReadStringUpto(false, '>', ',');
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
                ArrowIndex = placeHolders.Get(s._safeSubstring(1));
                if (ArrowIndex < 0) {
                    ParseError($"parseArrow: 定義されていないプレースホルダー: {s}");
                    return false;
                }
            } else {
                ArrowIndex = s._parseInt(-1);
                if (ArrowIndex < 0) {
                    ArrowIndex = s._safeSubstring(1)._parseInt(-1);
                    if (ArrowIndex < 0) {
                        if (s.Length == 1) {
                            // 1文字の場合は、プレースホルダを優先
                            ArrowIndex = placeHolders.Get(s);
                        }
                        if (ArrowIndex < 0) {
                            if (bRewritePre) {
                                // 前置書き換え対象文字列
                                RewritePreTargetStr = s;
                                return true;
                            } else if (bRewritePost) {
                                // 後置書き換え文字
                                RewritePostChar = s;
                                return true;
                            } else {
                                ArrowIndex = placeHolders.Get(s);
                                if (ArrowIndex < 0) {
                                    ParseError($"parseArrow: 定義されていないプレースホルダー: {s}");
                                    return false;
                                }
                            }
                        }
                    } else {
                        // 「文字 + 数字列」のパターン
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
            }

            ArrowIndex += funckeyOffset;
            ArrowIndex %= DecoderKeys.PLANE_DECKEY_NUM;    // 後で Offset を足すので Modulo 化しておく
            if (bShiftPlane) {
                // プレーン指定の場合
                shiftPlane = ArrowIndex;
                if (shiftPlane >= DecoderKeys.ALL_PLANE_NUM) ParseError($"parseArrow: shiftPlane out of range: {shiftPlane}");
                return false;
            } else {
                if (shiftOffset < 0) {
                    // シフト面のルートノードで明示的にシフトプレフィックスがなければ、shiftOffset をセット
                    shiftOffset = (shiftPlane > 0 && HasRootTable) ? shiftPlane * DecoderKeys.PLANE_DECKEY_NUM : 0;
                }
                ArrowIndex += shiftOffset;
                if (ArrowIndex < 0 || ArrowIndex >= DecoderKeys.EISU_COMBO_DECKEY_END) {
                    ParseError($"parseArrow: arrowIndex out of range: {ArrowIndex}");
                    return false;
                }
            }
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"arrowIndex={ArrowIndex}, shiftPlane={shiftPlane}, shiftOffset={shiftOffset}");
            return true;
        }

        // ARROW_BUNLE: -*>-nn>
        bool parseArrowBundle()
        {
            char c = GetNextChar();
            if (c != '>') ParseError($"parseArrowBundle: '>' is expected, but {c}");
            c = GetNextChar();
            if (c != '-') ParseError($"parseArrowBundle: '-' is expected, but {c}");

            ReadStringUpto(false, '>');
            string s = CurrentStr._strip();
            if (s._isEmpty()) {
                ParseError($"parseArrowBundle: arrowIndex is EMPTY");
            } else {
                c = s[0];
                if (c == '$') {
                    // TOKEN.PLACE_HOLDER
                    ArrowIndex = placeHolders.Get(s._safeSubstring(1));
                } else {
                    ArrowIndex = s._parseInt(-1);
                }
                if (ArrowIndex < 0 || ArrowIndex >= DecoderKeys.PLANE_DECKEY_NUM) ParseError($"parseArrowBundle: arrowIndex is out of range: {ArrowIndex}");
            }
            if (PeekNextChar() == '>') {
                GetNextChar();
            } else {
                ParseError($"parseArrowBundle-2: '>' is expected, but {c}");
            }
            return true;
        }

        // 「STRING:STRING」形式の2つめの文字列 (デリミタに '|' を使うことは不可。テーブルの簡易記法と間違うため)
        bool parseSecondString(bool bBare)
        {
            StringPair[0] = new OutputString(CurrentStr, bBare);

            char c = '\0';
            int countSpaces()
            {
                int n = 0;
                while (true) {
                    c = PeekNextChar(n);
                    if (c != ' ' && c != '\t') break;
                    ++n;
                }
                return n;
            }

            int spaceLen = countSpaces();
            if (c != ':') return false;
            AdvanceCharPos(spaceLen + 1);   // ':' の直後の位置に移動
            spaceLen = countSpaces();
            AdvanceCharPos(spaceLen);       // 空白の直後の位置に移動

            if (PeekNextChar() == '"') {
                ReadString();
                bBare = false;
            } else {
                ReadBareString();
                bBare = true;
            }
            StringPair[1] = new OutputString(CurrentStr, bBare);
            SkipToEndOfLine();
            if (StringPair[1]._isEmpty()) {
                ParseError("Invalid String Pair");
                return false;
            }
            return true;
        }

        // 漢字置換ファイルを読み込む
        // 一行の形式は「漢字 [<TAB>|Space]+ 読みの並び('|'区切り)」
        // 読みの並びの優先順は以下のとおり:
        // ①2文字以上のカタカナ
        // ②2文字以上のひらがな
        // ③漢字
        // bOnlyYomi == true なら、エントリの上書き禁止でカタカナをひらがなに変換
        // bOnlyYomi == false なら、エントリの上書きOKで、カタカナはそのまま、漢字と読みの入れ替えもOK
        void readKanjiConvFile(string filename, bool bOnlyYomi)
        {
            var reComment = @"#.*";
            var reBlank = @"[\t ]+";
            var reKatakanaMulti = @"[ァ-ン]{2,}";
            var reHiraganaMulti = @"[ぁ-ん]{2,}";

            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"filename: {filename}, bOnlyYomi={bOnlyYomi}");
            var lines = Helper.ReadAllLines(KanchokuIni.MakeFullPath(filename), e => {
                logger.Error($"Can't open: {filename}");
                FileOpenError(filename);
            });
            if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"lines.size(): {lines.Length}");
            if (lines._notEmpty() && kanjiConvMap != null) {
                foreach (var line in lines) {
                    //var items = utils::split(utils::strip(std::regex_replace(std::regex_replace(line, reComment, _T("")), reBlank, _T(" "))), ' ');
                    var items = line._reReplace(reComment, "")._reReplace(reBlank, " ")._stripAscii()._split(' ');
                    if (items.Length >= 2) {
                        var kanji = items[0];
                        if (kanji._notEmpty() && items[1]._notEmpty()) {
                            if (!bOnlyYomi || !kanjiConvMap.ContainsKey(kanji)) {
                                if (!bOnlyYomi) {
                                    // カタカナをそのまま入れる、漢字と読みの入れ替えもOK
                                    var yomi = items[1];
                                    if (yomi._notEmpty()) {
                                        kanjiConvMap[kanji] = yomi;
                                        kanjiConvMap[yomi] = kanji;
                                    }
                                } else {
                                    //std::wsmatch results;
                                    List<string> yomiItems = null;
                                    string yomi = null;
                                    if ((yomiItems = items[1]._reScan(reKatakanaMulti))._notEmpty()) {
                                        // カタカナ読みがあれば、それを優先して、ひらがな変換して使う
                                        yomi = Helper.ConvertKatakanaToHiragana(yomiItems[0]);
                                    } else if ((yomiItems = items[1]._reScan(reHiraganaMulti))._notEmpty()) {
                                        // カタカナ読みがなければ、ひらがな読みを使う
                                        yomi = yomiItems[0];
                                    }
                                    if (yomi._notEmpty() && yomi != kanji) kanjiConvMap[kanji] = yomi;
                                }
                            }
                        }
                    }
                }
                if (Settings.LoggingTableFileInfo) logger.InfoH(() => $"kanjiConvMap.size(): {kanjiConvMap.Count()}");
            }
        }

    }
}
