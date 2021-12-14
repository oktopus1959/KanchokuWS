//#include "pch.h"

// このソースのかなりの部分は、オリジナル漢直Winの parser.c のソースコードを流用しています

#include "string_utils.h"
#include "file_utils.h"
#include "path_utils.h"
#include "Logger.h"
#include "ErrorHandler.h"
#include "Settings.h"

#include "Node.h"
#include "StrokeTable.h"
#include "StringNode.h"
#include "FunctionNodeManager.h"
#include "DeckeyToChars.h"
#include "deckey_id_defs.h"
#include "MyPrevChar.h"
#include "VkbTableMaker.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughStrokeTable)

#define BOOL_TO_WPTR(f) (utils::boolToString(f).c_str())

namespace {
    DEFINE_NAMESPACE_LOGGER(StrokeTreeBuilder);

    // -------------------------------------------------------------------
    // トークンの種類
    enum class TOKEN {
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

    // ビルトイン機能
    struct BuiltInMarker {
        //static const int MyChar = '^';
        //static const int PrevChar = 'v';
        static const int NumberInCircle = 'n';
        static const int WidePrevChar = 'w';
        static const int WideShiftPrevChar = 'W';
    };

    // 丸数字
    inline wchar_t makeNumberInCircle(int n) {
        return (n >= 0 && n < 40) ? make_enclosed_alphanumeric(n) : 0;
    }

    // 全角文字
    inline wchar_t makeFullWideChar(int ch) {
        return make_fullwide_char(ch);
    }

    // 機能ノードの生成
    Node* createFunctionNode(tstring marker, int prevNum, int ) {
        LOG_DEBUG(_T("marker=%s, prevNum=%d, myNum=%d"), marker.c_str(), prevNum, 0);
        if (prevNum < 0) prevNum = 0;
        switch (utils::safe_front(marker)) {
        //case BuiltInMarker::MyChar:
        //    return new StringNode(DECKEY_TO_CHARS->GetCharFromDeckey(myNum));
        //case BuiltInMarker::PrevChar:
        //    return new StringNode(DECKEY_TO_CHARS->GetCharFromDeckey(prevNum));
        case BuiltInMarker::NumberInCircle:
            return new StringNode(makeNumberInCircle(prevNum));
        case BuiltInMarker::WidePrevChar:
            return new StringNode(makeFullWideChar(DECKEY_TO_CHARS->GetCharFromDeckey(prevNum)));
        case BuiltInMarker::WideShiftPrevChar:
            return new StringNode(makeFullWideChar(DECKEY_TO_CHARS->GetCharFromDeckey(prevNum + SHIFT_DECKEY_START)));
        default:
            return FunctionNodeManager::CreateFunctionNode(marker);
        }
    }

// ストローク木の作成クラス
    class StrokeTreeBuilder {
    private:
        DECLARE_CLASS_LOGGER;

        std::vector<tstring>& tableLines;

        TOKEN currentToken = TOKEN::END;   // 最後に読んだトークン
        tstring currentStr;                 // 文字列トークン
        int arrowIndex = -1;                // ARROWインデックス
        size_t lineNumber = 0;              // 今読んでる行数

        tstring currentLine;                // 現在解析中の行
        size_t nextPos = 0;                 // 次の文字位置
        char_t currentChar = 0;             // 次の文字

        std::map<wstring, wstring> defines; // 定義

        wstring getAndRemoveDefines(const wstring& key) {
            wstring result;
            auto iter = defines.find(key);
            if (iter != defines.end()) {
                result = iter->second;
                defines.erase(key);
            }
            return result;
        }

#define NUM_SHIFT_PLANE  4
        // シフト面 -- 0:シフト無し、1:通常シフト、2:ShiftA, 3:ShiftB の4面
        int shiftPlane = 0;

        // 打鍵マップ
        std::map<MString, std::vector<int>>* strokeSerieses = 0;
        std::map<MString, std::vector<int>>* strokeSerieses2 = 0;

        // 打鍵列
        std::vector<int> strokes;

        // 漢字置換マップ
        std::map<tstring, tstring> kanjiConvMap;

        const tstring& conv_kanji(const tstring& k) {
            auto iter = kanjiConvMap.find(k);
            return iter == kanjiConvMap.end() ? k : iter->second;
        }

    public:
        StrokeTreeBuilder(std::vector<tstring>& lines, bool bMakeStrokeSerieses)
            : tableLines(lines) {
            if (!tableLines.empty()) {
                currentLine = tableLines[0];
            }
            if (bMakeStrokeSerieses) {
                strokeSerieses = VkbTableMaker::StrokeSerieses();
                if (strokeSerieses) strokeSerieses->clear();
                strokeSerieses2 = VkbTableMaker::StrokeSerieses2();
                if (strokeSerieses2) strokeSerieses2->clear();
            }
        }

        // ストローク木を作成する
        // エラーがあったら例外を投げる
        StrokeTableNode* CreateStrokeTree() {
            // トップレベルはちょっと特殊
            // ブロックの外側に書かれている ARROW をブロックの内側にあるものとして扱う
            // つまり、
            // -n>... { ... } -m>...
            // を、
            // { -n>..., ..., -m>... }
            // として扱うということ。
            // なので、先に treeNode(テーブルノード)を作成しておく
            // RootStrokeTable は機能キーやCtrl修飾も含めたテーブルとする
            StrokeTableNode* tblNode = new StrokeTableNode(0, TOTAL_DECKEY_NUM);
            setupShiftedKeyFunction(tblNode);
            readNextToken(0);
            while (currentToken != TOKEN::END) {
                switch (currentToken) {
                case TOKEN::LBRACE:
                    makeSubTree(tblNode, 0, 0);
                    break;

                case TOKEN::ARROW:
                    createNodePositionedByArrow(tblNode, 0, arrowIndex);
                    break;

                case TOKEN::ARROW_BUNDLE:
                    allocateArrowBundle(tblNode, 0, arrowIndex);
                    break;

                case TOKEN::COMMA:             // ',' が来たら次のトークン待ち
                case TOKEN::SLASH:             // '/' が来ても次のトークン待ち
                    break;

                default:
                    parseError();
                    break;
                }
                readNextToken(0);
            }
            return tblNode;
        }

        // デフォルトのシフト面の機能(自身の文字を返す)ノードの設定
        void setupShiftedKeyFunction(StrokeTableNode* tblNode) {
            for (size_t i = 0; i < SHIFT_DECKEY_NUM; ++i) {
                tblNode->setNthChild(i + SHIFT_DECKEY_START, new MyCharNode());
            }
        }

        StrokeTableNode* makeSubTree(StrokeTableNode* tblNode, int depth, int prevNth) {
            wstring myGuideChars = getAndRemoveDefines(_T("defguide"));

            if (tblNode == 0) tblNode = new StrokeTableNode(depth);
            int shiftPlaneOffset = depth == 0 ? shiftPlane * SHIFT_DECKEY_NUM : 0;   // shift面によるオフセットは、ルートストロークだけに適用する
            int n = 0;
            bool isPrevDelim = true;
            readNextToken(depth);
            while (currentToken != TOKEN::RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                case TOKEN::ARROW:
                    createNodePositionedByArrow(tblNode, prevNth, arrowIndex);
                    isPrevDelim = false;
                    break;

                case TOKEN::ARROW_BUNDLE:
                    allocateArrowBundle(tblNode, 0, arrowIndex);
                    break;

                case TOKEN::LBRACE:
                case TOKEN::STRING:             // "str" : 文字列ノード
                case TOKEN::FUNCTION:           // @c : 機能ノード
                    tblNode->setNthChild(n + shiftPlaneOffset, createNode(currentToken, depth + 1, prevNth, n));
                    ++n;
                    isPrevDelim = false;
                    break;

                case TOKEN::COMMA:              // 次のトークン待ち
                case TOKEN::SLASH:              // 次のトークン待ち
                    if (isPrevDelim) ++n;
                    isPrevDelim = true;
                    break;

                default:                        // 途中でファイルが終わったりした場合 : エラー
                    parseError();
                    break;
                }

                readNextToken(depth);
            }

            if (!myGuideChars.empty()) {
                _LOG_DEBUGH(_T("DEFGUID: %s"), myGuideChars.c_str());
                tblNode->MakeStrokeGuide(myGuideChars);
            }

            strokes.resize(depth);
            return tblNode;
        }

        void createNodePositionedByArrow(StrokeTableNode* tblNode, int prevNth, int idx) {
            int nextDepth = tblNode->depth() + 1;
            _LOG_DEBUGH(_T("CALLED: currentLine=%d, nextDepth=%d, idx=%d, prevN=%d"), lineNumber, nextDepth, idx, prevNth);
            Node* node = tblNode->getNth(idx);
            if (node && node->isStrokeTableNode()) {
                createNodePositionedByArrowSub(dynamic_cast<StrokeTableNode*>(node), nextDepth, prevNth, idx);
            } else {
                tblNode->setNthChild(idx, createNodePositionedByArrowSub(0, nextDepth, prevNth, idx));
            }
        }

        Node* createNodePositionedByArrowSub(StrokeTableNode* tblNode, int depth, int prevNth, int nth) {
            readNextToken(depth);
            if (currentToken == TOKEN::ARROW) {
                if (tblNode == 0) tblNode = new StrokeTableNode(depth);
                strokes.push_back(nth);
                createNodePositionedByArrow(tblNode, nth, arrowIndex);
                strokes.pop_back();
                return tblNode;
            }
            return createNode(currentToken, depth, prevNth, nth);
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        void allocateArrowBundle(StrokeTableNode* tblNode, int depth, int nextArrowIdx) {
            _LOG_DEBUGH(_T("tblNode=%p, depth=%d, nextArrowIdx=%d"), tblNode, depth, nextArrowIdx);

            if (!tblNode) return;

            int shiftPlaneOffset = depth == 0 ? shiftPlane * SHIFT_DECKEY_NUM : 0;   // shift面によるオフセットは、ルートストロークだけに適用する
            int n = 0;
            bool isPrevDelim = true;
            readNextToken(depth);
            if (currentToken != TOKEN::LBRACE) { // 直後は '{' でブロックの始まりである必要がある
                parseError();
                return;
            }
            readNextToken(depth);
            while (currentToken != TOKEN::RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                case TOKEN::ARROW:
                    createNodePositionedByArrow(getNodePositionedByArrowBundle(tblNode, arrowIndex), 0, nextArrowIdx);
                    isPrevDelim = false;
                    break;

                case TOKEN::LBRACE:
                case TOKEN::STRING:             // "str" : 文字列ノード
                case TOKEN::FUNCTION:           // @c : 機能ノード
                    getNodePositionedByArrowBundle(tblNode, n + shiftPlaneOffset)->setNthChild(nextArrowIdx, createNode(currentToken, depth + 2, n, nextArrowIdx, true));
                    ++n;
                    isPrevDelim = false;
                    break;

                case TOKEN::COMMA:              // 次のトークン待ち
                case TOKEN::SLASH:              // 次のトークン待ち
                    if (isPrevDelim) ++n;
                    isPrevDelim = true;
                    break;

                default:                        // 途中でファイルが終わったりした場合 : エラー
                    parseError();
                    break;
                }

                readNextToken(depth);
            }

            strokes.resize(depth);
        }

        StrokeTableNode* getNodePositionedByArrowBundle(StrokeTableNode* tblNode, int idx) {
            _LOG_DEBUGH(_T("CALLED: currentLine=%d, idx=%d"), lineNumber, idx);
            Node* node = tblNode->getNth(idx);
            if (node && node->isStrokeTableNode()) return dynamic_cast<StrokeTableNode*>(node);

            StrokeTableNode* stNode = new StrokeTableNode(tblNode->depth() + 1);
            tblNode->setNthChild(idx, stNode);
            return stNode;
        }

        Node* createNode(TOKEN token, int depth, int prevNth, int nth, bool bArrowBundle = false) {
            switch (token) {
            case TOKEN::LBRACE: {
                strokes.push_back(nth);
                auto np = makeSubTree(0, depth, nth);
                strokes.pop_back();
                return np;
            }
            case TOKEN::RBRACE:
            case TOKEN::COMMA:             // ',' が来たら次のトークン
            case TOKEN::SLASH:             // '/' が来ても次のトークン
                return 0;
            case TOKEN::STRING:            // "str" : 文字列ノード
                LOG_TRACE(_T("%d:%d=%s"), lineNumber + 1, nth, currentStr.c_str());
                if (currentStr.empty()) return 0;
                if (kanjiConvMap.empty()) {
                    if (strokeSerieses && shiftPlane == 0) {
                        // 文字から、その文字の打鍵列へのマップに追加 (通常面)
                        auto ms = to_mstr(currentStr);
                        if (!ms.empty()) {
                            for (int k = 0; k < 10; ++k) {
                                auto iter = strokeSerieses->find(ms);
                                if (iter == strokeSerieses->end()) break;
                                // すでに同じものがあったら、末尾に TAB を追加しておく(後でローマ字テーブルを出力するときに複数の打鍵列も出力できるようにするため)
                                ms.push_back('\t');
                            }
                            if (bArrowBundle) {
                                LOG_DEBUGH(_T("line=%d, ArrowBundle, strokes.size=%d, strokes[0]=%d, prevNth=%d, nth=%d, str=%s"),
                                    lineNumber + 1, strokes.size(), strokes.size() > 0 ? strokes[0] : -1, prevNth, nth, currentStr.c_str());
                                strokes.push_back(prevNth);
                            }
                            strokes.push_back(nth);
                            (*strokeSerieses)[ms] = strokes;
                            strokes.pop_back();
                            if (bArrowBundle) strokes.pop_back();
                        }
                    }
                    return new StringNode(currentStr);
                } else {
                    tstring convStr = conv_kanji(currentStr);
                    if (strokeSerieses2) {
                        // 文字から、その文字の打鍵列へのマップに追加 (裏面)
                        auto ms = to_mstr(convStr);
                        if (!ms.empty()) {
                            for (int k = 0; k < 10; ++k) {
                                auto iter = strokeSerieses2->find(ms);
                                if (iter != strokeSerieses2->end()) {
                                    // すでに同じものがあったら、末尾に TAB を追加しておく(後でローマ字テーブルを出力するときに複数の打鍵列も出力できるようにするため)
                                    ms.push_back('\t');
                                }
                            }
                            strokes.push_back(nth);
                            (*strokeSerieses2)[ms] = strokes;
                            strokes.pop_back();
                        }
                    }
                    return new StringNode(convStr);
                }
            case TOKEN::FUNCTION:          // @c : 機能ノード
                return createFunctionNode(currentStr, prevNth, nth);
            default:                // 途中でファイルが終わったりした場合 : エラー
                parseError();
                return 0;
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
        void readNextToken(int depth) {
            currentToken = getToken(depth);
        }

        // トークンを読む
        TOKEN getToken(int depth) {
            currentStr.clear();
            arrowIndex = -1;
            while (true) {
                switch (getNextChar()) {
                case '#': {
                    // '#include', '#define', '#strokePosition', '#*shift*', '#yomiConvert', または '#' 以降、行末までコメント
                    wstring filename;
                    readWord();
                    auto lcStr = utils::toLower(currentStr);
                    if (lcStr == _T("include")) {
                        readWordOrString();
                        filename = currentStr;
                        _LOG_DEBUGH(_T("INCLUDE: lineNum=%d, %s"), lineNumber + 1, filename.c_str());
                    } else if (lcStr == _T("define")) {
                        readWord();
                        if (!currentStr.empty()) {
                            wstring key = currentStr;
                            readWordOrString();
                            defines[key] = currentStr;
                            _LOG_DEBUGH(_T("DEFINE: lineNum=%d, %s=%s"), lineNumber + 1, key.c_str(), currentStr.c_str());
                        }
                    } else if (utils::startsWith(lcStr, _T("yomiconv"))) {
                        readWord();
                        auto keyword = currentStr;
                        _LOG_DEBUGH(_T("YomiConversion: keyword=%s"), keyword.c_str());
                        if (keyword == _T("clear") || keyword == _T("end")) {
                            kanjiConvMap.clear();
                        } else {
                            _LOG_DEBUGH(_T("YomiConversion: %s"), SETTINGS->kanjiYomiFile.c_str());
                            if (!SETTINGS->kanjiYomiFile.empty()) readKanjiConvFile(SETTINGS->kanjiYomiFile, true);
                            if (keyword == _T("with")) {
                                readWordOrString();
                                if (!currentStr.empty()) {
                                    _LOG_DEBUGH(_T("YomiConversion: %s"), currentStr.c_str());
                                    readKanjiConvFile(currentStr, false);
                                }
                            }
                        }
                    } else if (lcStr == _T("strokePosition")) {
                        readWordOrString();
                        defines[_T("defguide")] = currentStr;
                        _LOG_DEBUGH(_T("StrokePosition: %s"), currentStr.c_str());
                    } else if (lcStr == _T("noshift")) {
                        shiftPlane = 0;
                    } else if (lcStr == _T("shift")) {
                        shiftPlane = 1;
                    } else if (lcStr == _T("shifta")) {
                        shiftPlane = 2;
                    } else if (lcStr == _T("shiftb")) {
                        shiftPlane = 3;
                    }
                    currentStr.clear();
                    skipToEndOfLine();
                    if (!filename.empty()) {
                        readFile(filename);
                    }
                }
                    break;
                case ';':
                    // ';' 以降、行末までコメント
                    skipToEndOfLine();
                    break;

                case '{': return TOKEN::LBRACE;
                case '}': return TOKEN::RBRACE;
                case ',': return TOKEN::COMMA;
                case '/': return TOKEN::SLASH;

                case '\n':
                case ' ':                   // SPC : スキップ
                case '\t':                  // TAB : スキップ
                case '\r':                  // ^M  : スキップ
                case '\f':                  // ^L  : スキップ
                    break;

                case '@':
                    // 機能
                    readMarker();
                    return TOKEN::FUNCTION;

                case '"':
                    // 文字列
                    readString();
                    return TOKEN::STRING;

                case '-': {
                    char_t c = getNextChar();
                    if (c == '*') {
                        // 矢印束記法
                        if (parseArrowBundle()) return TOKEN::ARROW_BUNDLE;
                    } else {
                        // 矢印記法
                        if (parseArrow(depth, c)) return TOKEN::ARROW;
                    }
                }
                    break;

                case 0:
                    // ファイルの終わり
                    return TOKEN::END;

                default:
                    // エラー
                    parseError();
                    return TOKEN::END;
                }
            }
        }

        // '"' が来るまで読みこんで、currentStr に格納。
        void readString() {
            // 「"」自身は「"\""」と表記することで指定できる。
            // 「\」自身は「"\\"」と表記する。
            // 「\」は、単に次の一文字をエスケープするだけで、
            // 「"\n"」「"\t"」「"\ooo"」は未対応。
            while (true) {
                char_t c = getNextChar();
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
                currentStr.append(1, c);
            }
        }

        // 空白またはカンマが来るまで読みこんで、currentStr に格納。
        void readMarker() {
            while (true) {
                char_t c = peekNextChar();
                if (c <= ' ' || c == ',') {
                    if (currentStr.empty()) parseError();
                    return;
                }
                getNextChar();
                currentStr.append(1, c);
            }
        }

        // 次の空白文字までを読み込んで、currentStr に格納。
        void readWord() {
            currentStr.clear();
            char_t c = skipSpace();
            if (c <= ' ') return;

            readWordSub(c);
        }

        // 次の空白文字までを読み込んで、currentStr に格納。
        void readWordSub(wchar_t c) {
            currentStr.append(1, c);
            while (true) {
                c = getNextChar();
                if (c <= ' ') return;
                currentStr.append(1, c);
            }
        }

        // 文字列または単語を読み込む
        void readWordOrString() {
            currentStr.clear();
            char_t c = skipSpace();
            if (c > ' ') {
                if (c == '"')
                    readString();
                else
                    readWordSub(c);
            }
        }

        // 空白文字を読み飛ばす
        char_t skipSpace() {
            while (true) {
                char_t c = getNextChar();
                if (c == '\r' || c == '\n' || c == 0 || c > ' ')  return c;
            }
        }

        // ARROW: /-[SsXxPp]?[0-9]+>/
        bool parseArrow(int depth, char_t c) {
            int shiftOffset = -1;
            bool bShiftPlane = false;
            //char_t c = getNextChar();
            if (c == 'N' || c == 'n') {
                shiftOffset = 0;
                c = getNextChar();
            } else if (c == 'S' || c == 's') {
                shiftOffset = SHIFT_DECKEY_START;
                c = getNextChar();
            } else if (c == 'A' || c == 'a') {
                shiftOffset = SHIFT_A_DECKEY_START;
                c = getNextChar();
            } else if (c == 'B' || c == 'b') {
                shiftOffset = SHIFT_B_DECKEY_START;
                c = getNextChar();
            } else if (c == 'X' || c == 'x') {
                shiftOffset = FUNC_DECKEY_START;
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
                    shiftOffset = (shiftPlane > 0 && depth == 0) ? shiftOffset = shiftPlane * SHIFT_DECKEY_NUM : 0;
                }
                arrowIndex += shiftOffset;
                if (arrowIndex >= FUNC_DECKEY_END) parseError();
            } else {
                shiftPlane = arrowIndex;
                if (arrowIndex >= NUM_SHIFT_PLANE) parseError();
                return false;
            }
            if (c != '>') parseError();
            return true;
        }

        // ARROW_BUNLE: -*>-nn>
        bool parseArrowBundle() {
            char_t c = getNextChar();
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
            if (arrowIndex >= NORMAL_DECKEY_NUM) parseError();
            if (c != '>') parseError();
            return true;
        }

        char_t getNextChar() {
            if (nextPos > currentLine.size()) {
                ++lineNumber;
                if (lineNumber >= tableLines.size()) {
                    return currentChar = 0;
                }
                currentLine = tableLines[lineNumber];
                nextPos = 0;
            }
            if (nextPos < currentLine.size()) {
                currentChar = currentLine[nextPos++];
            } else {
                ++nextPos;
                currentChar = '\n';
            }
            return currentChar;
        }

        char_t peekNextChar() {
            return (nextPos < currentLine.size()) ? currentLine[nextPos] : '\0';
        }

        void skipToEndOfLine() {
            nextPos = currentLine.size();
        }

        void readFile(const wstring& filename) {
            _LOG_DEBUGH(_T("INCLUDE: %s"), filename.c_str());
            auto reader = utils::IfstreamReader(utils::joinPath(SETTINGS->rootDir, filename));
            if (reader.success()) {
                auto lines = reader.getAllLines();
                tableLines.insert(tableLines.begin() + lineNumber + 1, lines.begin(), lines.end());
            } else {
                LOG_ERROR(_T("Can't open: %s"), filename.c_str());
            }
        }

        // 漢字置換ファイルを読み込む
        // 一行の形式は「漢字 [<TAB>|Space]+ 読みの並び('|'区切り)」
        // 読みの並びの優先順は以下のとおり:
        // ①2文字以上のカタカナ
        // ②2文字以上のひらがな
        // ③漢字
        // bOnlyYomi == true なら、エントリの上書き禁止でカタカナをひらがなに変換
        // bOnlyYomi == false なら、エントリの上書きOKで、カタカナはそのまま
        void readKanjiConvFile(const wstring& filename, bool bOnlyYomi) {
            std::wregex reComment(_T("#.*"));
            std::wregex reBlank(_T("[\\t ]+"));
            std::wregex reKatakanaMulti(_T("[ァ-ン]{2,}"));
            std::wregex reHiraganaMulti(_T("[ぁ-ん]{2,}"));
            _LOG_DEBUGH(_T("filename: %s, bOnlyYomi=%s"), filename.c_str(), BOOL_TO_WPTR(bOnlyYomi));
            auto reader = utils::IfstreamReader(utils::joinPath(SETTINGS->rootDir, filename));
            if (reader.success()) {
                auto lines = reader.getAllLines();
                _LOG_DEBUGH(_T("lines.size(): %d"), lines.size());
                for (auto line : lines) {
                    auto items = utils::split(utils::strip(std::regex_replace(std::regex_replace(line, reComment, _T("")), reBlank, _T(" "))), ' ');
                    if (items.size() >= 2) {
                        auto kanji = items[0];
                        if (!kanji.empty() && !items[1].empty()) {
                            if (!bOnlyYomi || kanjiConvMap.find(kanji) == kanjiConvMap.end()) {
                                if (!bOnlyYomi) {
                                    auto yomi = items[1];
                                    if (!yomi.empty()) {
                                        kanjiConvMap[kanji] = yomi;
                                        kanjiConvMap[yomi] = kanji;
                                    }
                                } else {
                                    std::wsmatch results;
                                    if (std::regex_search(items[1], results, reKatakanaMulti)) {
                                        auto yomi = utils::convert_katakana_to_hiragana(results.str());
                                        if (!yomi.empty()) kanjiConvMap[kanji] = yomi;
                                    } else if (std::regex_search(items[1], results, reHiraganaMulti)) {
                                        auto yomi = results.str();
                                        if (!yomi.empty()) kanjiConvMap[kanji] = yomi;
                                    }
                                }
                            }
                        }
                    }
                }
                _LOG_DEBUGH(_T("kanjiConvMap.size(): %d"), kanjiConvMap.size());
            } else {
                LOG_ERROR(_T("Can't open: %s"), filename.c_str());
            }
        }

        // 読みこみに失敗した場合
        void parseError() {
            wchar_t buf[2] = { currentChar, 0 };
            tstring msg = utils::format(_T("テーブルファイルの %d 行 %d文字目('%s')がまちがっているようです：\r\n> %s ..."), lineNumber, nextPos, buf, currentLine.substr(0, 50).c_str());
            LOG_ERROR(msg);
            wstring lines;
            for (size_t i = 10; i > 0; --i) {
                if (lineNumber >= i + 2) lines = lines + tableLines[lineNumber - (i + 1)] + _T("\n");
            }
            lines = lines + _T(">> ") + currentLine + _T("\n");
            for (size_t i = 0; i < 10; ++i) {
                if (lineNumber + i < tableLines.size())lines = lines + tableLines[lineNumber + i] + _T("\n");
            }
            LOG_ERROR(_T("lines=\n%s"), lines.c_str());
            // エラーメッセージを投げる
            ERROR_HANDLER->Error(msg);
        }
    };
    DEFINE_CLASS_LOGGER(StrokeTreeBuilder);

} // namespace

DEFINE_CLASS_LOGGER(StrokeTableNode);

// 機能の再割り当て
void StrokeTableNode::AssignFucntion(const tstring& keys, const tstring& name) {
    _LOG_DEBUGH(_T("CALLED: keys=%s, name=%s"), keys.c_str(), name.c_str());

    if (keys.empty()) return;

    std::vector<size_t> keyCodes;
    std::wregex reDigits(_T("^[SsAaBbXx]?[0-9]+$"));
    for (auto k : utils::split(keys, ',')) {
        if (k.empty() || !std::regex_match(k, reDigits)) return;    // 10進数でなければエラー
        int shiftOffset = 0;
        if (k[0] == 'S' || k[0] == 's') {
            shiftOffset = SHIFT_DECKEY_START;
            k = k.substr(1);
        } else if (k[0] == 'A' || k[0] == 'a') {
            shiftOffset = SHIFT_A_DECKEY_START;
            k = k.substr(1);
        } else if (k[0] == 'B' || k[0] == 'b') {
            shiftOffset = SHIFT_B_DECKEY_START;
            k = k.substr(1);
        } else if (k[0] == 'X' || k[0] == 'x') {
            shiftOffset = FUNC_DECKEY_START;
            k = k.substr(1);
        }
        keyCodes.push_back((size_t)utils::strToInt(k, -1) + shiftOffset);
    }
    StrokeTableNode* pNode = RootStrokeNode1.get();
    if (pNode == 0) return;
    size_t idx = 0;
    size_t key = 0;
    while (idx < keyCodes.size()) {
        key = keyCodes[idx++];
        if (key >= pNode->numChildren()) break;        // 子ノード数の範囲外ならばエラー
        Node* p = pNode->getNth(key);
        if (p == 0 || p->isFunctionNode()) {
            // 未割り当て、または機能ノードならばOK
            if (idx == keyCodes.size()) {
                // 打鍵列の最後まで行った
                _LOG_DEBUGH(_T("RESET: depth=%d, key=%d, name=%s"), idx, key, name.c_str());
                pNode->setNthChild(key, FunctionNodeManager::CreateFunctionNodeByName(name));
            }
            break;
        }
        if (p->isStrokeTableNode()) {
            pNode = dynamic_cast<StrokeTableNode*>(pNode->getNth(key));
            if (pNode != 0) continue;                   // 子ノードに
        }
        break;     // エラー
    }
}

// ストローク木を作成してそのルートを返す
StrokeTableNode* StrokeTableNode::CreateStrokeTree(std::vector<tstring>& lines) {
    auto ptr = std::make_unique<StrokeTreeBuilder>(lines, true);
    RootStrokeNode1.reset(ptr->CreateStrokeTree());
    ROOT_STROKE_NODE = RootStrokeNode1.get();
    return ROOT_STROKE_NODE;
}

// ストローク木2を作成してそのルートを返す
StrokeTableNode* StrokeTableNode::CreateStrokeTree2(std::vector<tstring>& lines) {
    auto ptr = std::make_unique<StrokeTreeBuilder>(lines, false);
    RootStrokeNode2.reset(ptr->CreateStrokeTree());
    return RootStrokeNode2.get();
}

