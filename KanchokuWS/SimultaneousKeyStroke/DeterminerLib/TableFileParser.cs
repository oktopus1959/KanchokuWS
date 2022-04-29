using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS.SimultaneousKeyStroke.DeterminerLib
{
    class TableFileParser
    {
        private static Logger logger = Logger.GetLogger(true);

        private List<string> tableLines;

        // トークンの種類
        enum TOKEN {
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

        TOKEN currentToken = TOKEN.END;     // 最後に読んだトークン
        string currentStr;                  // 文字列トークン
        int arrowIndex = -1;                // ARROWインデックス
        int lineNumber = 0;                 // 今読んでる行数

        string currentLine;                 // 現在解析中の行
        int nextPos = 0;                    // 次の文字位置
        char currentChar = '\0';            // 次の文字

        // 関心のあるブロックの中か
        bool isInConcernedBlock = false;

        // 打鍵列
        List<int> strokes = new List<int>();

        // 定義列マップ
        Dictionary<string, List<string>> linesMap = new Dictionary<string, List<string>>();

        // シフト面 -- 0:シフト無し、1:通常シフト、2:ShiftA, 3:ShiftB, 4:ShiftX(Simultaneous) の5面
        int shiftPlane = 0;

        // テーブル定義を解析してストローク木を構築する
        // 後から部分的にストローク定義を差し込む際にも使用される
        public void ParseTable(string filename) {
            logger.InfoH($"ENTER: filename={filename}");
            tableLines = readAllLines(filename);

            if (tableLines._notEmpty()) {
                currentLine = tableLines[0];
                readNextToken(0);
                while (currentToken != TOKEN.END) {
                    switch (currentToken) {
                        case TOKEN.LBRACE:
                            parseSubTree(0, 0);
                            break;

                        case TOKEN.ARROW:
                            parseArrowNode(0, 0, arrowIndex);
                            break;

                        case TOKEN.ARROW_BUNDLE:
                            parseArrowBundleNode(0, arrowIndex);
                            break;

                        case TOKEN.COMMA:             // ',' が来たら次のトークン待ち
                        case TOKEN.SLASH:             // '/' が来ても次のトークン待ち
                            break;

                        default:
                            parseError();
                            break;
                    }
                    readNextToken(0);
                }
            }

            KeyCombinationPool.Singleton.SetNonTerminalMarkForSubkeys();
            if (Logger.IsInfoHEnabled && logger.IsInfoHPromoted) {
                KeyCombinationPool.Singleton.DebugPrint();
            }
            logger.InfoH($"LEAVE: KeyCombinationPool.Count={KeyCombinationPool.Singleton.Count}");
        }

        void parseSubTree(int depth, int prevNth)
        {
            int shiftPlaneOffset = depth == 0 ? shiftPlane * DecoderKeys.SHIFT_DECKEY_NUM : 0;   // shift面によるオフセットは、ルートストロークだけに適用する
            int n = 0;
            bool isPrevDelim = true;
            readNextToken(depth);
            while (currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                    case TOKEN.ARROW:
                        parseArrowNode(depth + 1, prevNth, arrowIndex);
                        isPrevDelim = false;
                        break;

                    case TOKEN.ARROW_BUNDLE:
                        parseArrowBundleNode(depth, arrowIndex);
                        break;

                    case TOKEN.LBRACE:
                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        parseNode(currentToken, depth + 1, prevNth, n);
                        ++n;
                        isPrevDelim = false;
                        break;

                    case TOKEN.COMMA:              // 次のトークン待ち
                    case TOKEN.SLASH:              // 次のトークン待ち
                        if (isPrevDelim) ++n;
                        isPrevDelim = true;
                        break;

                    default:                        // 途中でファイルが終わったりした場合 : エラー
                        parseError();
                        break;
                }

                readNextToken(depth);
            }

            strokes._resize(depth);
        }

        void parseArrowNode(int depth, int prevNth, int idx) {
            logger.DebugH(() => $"CALLED: currentLine={lineNumber}, depth={depth}, idx={idx}, prevN={prevNth}");
            readNextToken(depth);
            if (currentToken == TOKEN.ARROW) {
                strokes.Add(idx);
                parseArrowNode(depth + 1, idx, arrowIndex);
                strokes._popBack();
            }
            parseNode(currentToken, depth + 1, prevNth, idx);
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        void parseArrowBundleNode(int depth, int nextArrowIdx)
        {
            logger.DebugH(() => $"depth={depth}, nextArrowIdx={nextArrowIdx}");

            int shiftPlaneOffset = depth == 0 ? shiftPlane * DecoderKeys.SHIFT_DECKEY_NUM : 0;   // shift面によるオフセットは、ルートストロークだけに適用する
            int n = 0;
            bool isPrevDelim = true;
            readNextToken(depth);
            if (currentToken != TOKEN.LBRACE) { // 直後は '{' でブロックの始まりである必要がある
                parseError();
                return;
            }
            readNextToken(depth);
            while (currentToken != TOKEN.RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                    case TOKEN.ARROW:
                        parseArrowNode(depth + 1, 0, nextArrowIdx);
                        isPrevDelim = false;
                        break;

                    case TOKEN.LBRACE:
                    case TOKEN.STRING:             // "str" : 文字列ノード
                    case TOKEN.FUNCTION:           // @c : 機能ノード
                        parseNode(currentToken, depth + 2, n, nextArrowIdx);
                        ++n;
                        isPrevDelim = false;
                        break;

                    case TOKEN.COMMA:              // 次のトークン待ち
                    case TOKEN.SLASH:              // 次のトークン待ち
                        if (isPrevDelim) ++n;
                        isPrevDelim = true;
                        break;

                    default:                        // 途中でファイルが終わったりした場合 : エラー
                        parseError();
                        break;
                }

                readNextToken(depth);
            }

            strokes._resize(depth);
        }

        void parseNode(TOKEN token, int depth, int prevNth, int nth, bool bArrowBundle = false)
        {
            logger.DebugH(() => $"CALLED: token={token}, depth={depth}, prevNth={prevNth}, nth={nth}, bArrowBundle={bArrowBundle}");
            switch (token) {
                case TOKEN.LBRACE:
                    strokes.Add(nth);
                    parseSubTree(depth, nth);
                    strokes._popBack();
                    return;

                case TOKEN.STRING:            // "str" : 文字列ノード
                    if (isInConcernedBlock) {
                        makeSimultaneousKeyCombo(nth);
                    }
                    return;

                case TOKEN.RBRACE:
                case TOKEN.COMMA:             // ',' が来たら次のトークン
                case TOKEN.SLASH:             // '/' が来ても次のトークン
                case TOKEN.FUNCTION:          // @c : 機能ノード
                    return;
                default:                // 途中でファイルが終わったりした場合 : エラー
                    parseError();
                    return;
            }
        }

        // 同時打鍵組合せを作成する
        void makeSimultaneousKeyCombo(int nth)
        {
            var ss = new List<int>(strokes);
            ss.Add(nth);
            logger.DebugH(() => $"{ss.Select(x => x.ToString())._join(":")}={currentStr}");
            var keyCombo = new KeyCombination(ss);
            // 同時打鍵キー集合は、Normalキーで作成しておく
            var ts = ss.Select(x => x >= DecoderKeys.SHIFT_M_DECKEY_START ? x - DecoderKeys.SHIFT_M_DECKEY_START : x).ToList();
            KeyCombinationPool.Singleton.AddEntry(ts, keyCombo);
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
        void readNextToken(int depth) {
            currentToken = getToken(depth);
        }

        // トークンを読む
        TOKEN getToken(int depth)
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
                    // Directive: '#include', '#define', '#strokePosition', '#*shift*', '#simultaneous', '#yomiConvert', '#store', '#load', '#end' または '#' 以降、行末までコメント
                    readWord();
                    var lcStr = currentStr._toLower();
                    if (lcStr == "include") {
                        includeFile();
                    } else if (lcStr == "define") {
                        // do nothing
                    } else if (lcStr == "store") {
                        storeLineBlock();
                    } else if (lcStr == "load") {
                        loadLineBlock();
                    } else if (lcStr._startsWith("yomiconv")) {
                        while (getNextLine()) {
                            if (currentLine._startsWith("#yomiconv") && currentLine._safeIndexOf("end") >= 0) break;
                        }
                    } else if (lcStr == "strokePosition") {
                        // do nothing
                    } else if (lcStr == "noshift" || lcStr == "normal") {
                        shiftPlane = 0;
                    } else if (lcStr == "shift") {
                        shiftPlane = 1;
                    } else if (lcStr == "shifta") {
                        shiftPlane = 2;
                    } else if (lcStr == "shiftb") {
                        shiftPlane = 3;
                    } else if (lcStr == "shiftm" || lcStr == "simultaneous") {
                        shiftPlane = 4;
                        isInConcernedBlock = true;
                        getSimultaneousKeys();
                    } else if (lcStr == "end") {
                        shiftPlane = 0;
                        isInConcernedBlock = false;
                    } else if (lcStr == "sands") {
                        handleSandSState();
                    } else {
                        logger.DebugH(() => $"#{currentStr}");
                    }
                    currentStr = "";
                    skipToEndOfLine();
                    continue;
                }

                if (!isInConcernedBlock) {
                    skipToEndOfLine();
                    continue;
                }

                switch (currentChar) {
                    case ';':
                        // ';' 以降、行末までコメント
                        skipToEndOfLine();
                        break;

                    case '{': return TOKEN.LBRACE;
                    case '}': return TOKEN.RBRACE;
                    case ',': return TOKEN.COMMA;
                    case '/': return TOKEN.SLASH;

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
                            if (parseArrow(depth, c)) return TOKEN.ARROW;
                        }
                    }
                    break;

                    case '\0':
                        // ファイルの終わり
                        return TOKEN.END;

                    default:
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
                var lines = readAllLines(filename);
                if (lines._isEmpty()) {
                    logger.Error($"Can't open: {filename}");
                } else {
                    tableLines.InsertRange(lineNumber + 1, lines);
                    logger.DebugH(() => $"INCLUDE: {lines.Count} lines included");
                }
            }
        }

        // 名前を付けて、行ブロックを保存する
        void storeLineBlock()
        {
            readWord();
            logger.DebugH(() => $"CALLED: {currentStr}");
            List<string> lines = null;
            if (currentStr._isEmpty()) {
                parseError();
            } else {
                lines = new List<string>();
                linesMap[currentStr] = lines;
                logger.DebugH(() => $"SET: lineNum={lineNumber + 1}, {currentStr}");
            }
            while (getNextLine()) {
                if (currentLine._startsWith("#end")) break;
                if (lines != null) {
                    lines.Add(currentLine);
                }
            }
        }

        // 保存しておいた行ブロックをロードする
        void loadLineBlock()
        {
            readWord();
            logger.DebugH(() => $"CALLED: {currentStr}");
            if (currentStr._isEmpty()) {
                parseError();
            } else if (isInConcernedBlock) {
                var lines = linesMap._safeGet(currentStr);
                if (lines._isEmpty()) {
                    parseError();
                } else {
                    logger.DebugH(() => $"InsertRange: {currentStr}, {lines.Count} lines");
                    tableLines.InsertRange(lineNumber + 1, lines);
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

        void getSimultaneousKeys()
        {
            readWord();
            var items = currentStr._split('=');
            if (items._safeLength() == 2 && items[1]._notEmpty()) {
                int pri = 1;
                foreach (var keys in items[1]._split('|')) {
                    if (keys._notEmpty()) {
                        foreach (var k in keys._split(',')) {
                            KeyCombinationPool.Singleton.AddShiftKey(k._parseInt(), pri);
                        }
                    }
                    ++pri;
                }
            }
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

        // 次の空白文字までを読み込んで、currentStr に格納。
        void readWord() {
            currentStr = "";
            char c = skipSpace();
            if (c <= ' ') return;

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
        void readWordOrString() {
            currentStr = "";
            char c = skipSpace();
            if (c > ' ') {
                if (c == '"')
                    readString();
                else
                    readWordSub(c);
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
        bool parseArrow(int depth, char c) {
            int shiftOffset = -1;
            bool bShiftPlane = false;
            //char c = getNextChar();
            if (c == 'N' || c == 'n') {
                shiftOffset = 0;
                c = getNextChar();
            } else if (c == 'S' || c == 's') {
                shiftOffset = DecoderKeys.SHIFT_DECKEY_START;
                c = getNextChar();
            } else if (c == 'A' || c == 'a') {
                shiftOffset = DecoderKeys.SHIFT_A_DECKEY_START;
                c = getNextChar();
            } else if (c == 'B' || c == 'b') {
                shiftOffset = DecoderKeys.SHIFT_B_DECKEY_START;
                c = getNextChar();
            } else if (c == 'X' || c == 'x') {
                shiftOffset = DecoderKeys.FUNC_DECKEY_START;
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
            if (!bShiftPlane) {
                if (shiftOffset < 0) {
                    // シフト面のルートノードで明示的にシフトプレフィックスがなければ、shiftOffset をセット
                    shiftOffset = (shiftPlane > 0 && depth == 0) ? shiftPlane * DecoderKeys.SHIFT_DECKEY_NUM : 0;
                }
                arrowIndex += shiftOffset;
                if (arrowIndex >= DecoderKeys.FUNC_DECKEY_END) parseError();
            } else {
                shiftPlane = arrowIndex;
                if (shiftPlane >= DecoderKeys.ALL_SHIFT_PLANE_NUM) parseError();
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
            if (arrowIndex >= DecoderKeys.NORMAL_DECKEY_NUM) parseError();
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

        bool getNextLine() {
            ++lineNumber;
            if (lineNumber >= tableLines._safeCount()) {
                return false;
            }
            currentLine = tableLines[lineNumber];
            return true;
        }

        char peekNextChar() {
            return (nextPos < currentLine._safeLength()) ? currentLine[nextPos] : '\0';
        }

        void skipToEndOfLine() {
            nextPos = currentLine._safeLength() + 1;
        }

        List<string> readAllLines(string filename)
        {
            logger.DebugH(() => $"ENTER: filename={filename}");
            var lines = new List<string>();
            if (filename._notEmpty()) {
                lines = Helper.GetFileContent(KanchokuIni.Singleton.KanchokuDir._joinPath(filename))._safeReplace("\r", "")._split('\n')?.ToList() ?? new List<string>();
            }
            logger.DebugH(() => $"LEAVE: num of lines={lines.Count}");
            return lines;
        }

        void parseError() {
            string msg = $"テーブルファイルの {lineNumber}行 {nextPos}文字目('{currentChar}')がまちがっているようです：\r\n> {currentLine._safeSubstring(0, 50)} ...";
            logger.Error(msg);
            var sb = new StringBuilder();
            for (int i = 10; i > 0; --i) {
                if (lineNumber >= i + 2) sb.Append(tableLines[lineNumber - (i + 1)]).Append('\n');
            }
            sb.Append($">> {currentLine}\n");
            for (int i = 0; i < 10; ++i) {
                if (lineNumber + i < tableLines._safeCount())sb.Append(tableLines[lineNumber + i]).Append('\n');
            }
            logger.Error($"lines=\n{sb.ToString()}");
        }
    }
}
