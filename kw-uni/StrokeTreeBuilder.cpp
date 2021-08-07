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

    public:
        StrokeTreeBuilder(std::vector<tstring>& lines)
            : tableLines(lines) {
            if (!tableLines.empty()) {
                currentLine = tableLines[0];
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
            int treeCount = 0;
            readNextToken();
            while (currentToken != TOKEN::END) {
                switch (currentToken) {
                case TOKEN::LBRACE:
                    ++treeCount;
                    if (treeCount > 1) {
                        // 現在のところ、トップレベルで2つ以上のブロックは許可していない
                        parseError();
                        break;
                    }
                    makeSubTree(tblNode, 0, 0);
                    break;

                case TOKEN::ARROW:
                    createNodePositionedByArrow(tblNode, 0, arrowIndex);
                    break;

                case TOKEN::COMMA:             // ',' が来たら次のトークン待ち
                case TOKEN::SLASH:             // '/' が来ても次のトークン待ち
                    break;

                default:
                    parseError();
                    break;
                }
                readNextToken();
            }
            return tblNode;
        }

        void setupShiftedKeyFunction(StrokeTableNode* tblNode) {
            for (size_t i = 0; i < SHIFT_DECKEY_NUM; ++i) {
                tblNode->setNthChild(i + SHIFT_DECKEY_START, new MyCharNode());
            }
        }

        StrokeTableNode* makeSubTree(StrokeTableNode* tblNode, int depth, int prevNth) {
            wstring myGuideChars = getAndRemoveDefines(_T("defguide"));

            if (tblNode == 0) tblNode = new StrokeTableNode(depth);
            int n = 0;
            bool isPrevEmpty = true;
            readNextToken();
            while (currentToken != TOKEN::RBRACE) { // '}' でブロックの終わり
                switch (currentToken) {
                case TOKEN::ARROW:
                    createNodePositionedByArrow(tblNode, prevNth, arrowIndex);
                    break;

                case TOKEN::LBRACE:
                case TOKEN::STRING:             // "str" : 文字列ノード
                case TOKEN::FUNCTION:           // @c : 機能ノード
                    tblNode->setNthChild(n, createNode(currentToken, depth + 1, prevNth, n));
                    ++n;
                    isPrevEmpty = false;
                    break;

                case TOKEN::COMMA:              // 次のトークン待ち
                case TOKEN::SLASH:              // 次のトークン待ち
                    if (isPrevEmpty) ++n;
                    isPrevEmpty = true;
                    break;

                default:                        // 途中でファイルが終わったりした場合 : エラー
                    parseError();
                    break;
                }

                readNextToken();
            }

            if (!myGuideChars.empty()) {
                LOG_INFOH(_T("DEFGUID: %s"), myGuideChars.c_str());
                tblNode->MakeStrokeGuide(myGuideChars);
            }
            return tblNode;
        }

        void createNodePositionedByArrow(StrokeTableNode* tblNode, int prevNth, int idx) {
            int nextDepth = tblNode->depth() + 1;
            LOG_INFOH(_T("CALLED: currentLine=%d, depth=%d, idx=%d, prevN=%d"), lineNumber, nextDepth, idx, prevNth);
            Node* node = tblNode->getNth(idx);
            if (node && node->isStrokeTableNode()) {
                createNodePositionedByArrowSub(dynamic_cast<StrokeTableNode*>(node), nextDepth, prevNth, idx);
            } else {
                tblNode->setNthChild(idx, createNodePositionedByArrowSub(0, nextDepth, prevNth, idx));
            }
        }

        Node* createNodePositionedByArrowSub(StrokeTableNode* tblNode, int depth, int prevNth, int nth) {
            readNextToken();
            if (currentToken == TOKEN::ARROW) {
                if (tblNode == 0) tblNode = new StrokeTableNode(depth);
                createNodePositionedByArrow(tblNode, nth, arrowIndex);
                return tblNode;
            }
            return createNode(currentToken, depth, prevNth, nth);
        }

        Node* createNode(TOKEN token, int depth, int prevNth, int nth) {
            switch (token) {
            case TOKEN::LBRACE:
                return makeSubTree(0, depth, nth);
            case TOKEN::RBRACE:
            case TOKEN::COMMA:             // ',' が来たら次のトークン
            case TOKEN::SLASH:             // '/' が来ても次のトークン
                return 0;
            case TOKEN::STRING:            // "str" : 文字列ノード
                LOG_TRACE(_T("%d:%d=%s"), lineNumber + 1, nth, currentStr.c_str());
                return new StringNode(currentStr);
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
        void readNextToken() {
            currentToken = getToken();
        }

        // トークンを読む
        TOKEN getToken() {
            currentStr.clear();
            arrowIndex = -1;
            while (true) {
                switch (getNextChar()) {
                case '#': {
                    // '#include' または '#' 以降、行末までコメント
                    wstring filename;
                    readWord();
                    if (currentStr == _T("include")) {
                        readWordOrString();
                        filename = currentStr;
                        LOG_INFOH(_T("INCLUDE: lineNum=%d, %s"), lineNumber + 1, filename.c_str());
                    } else if (currentStr == _T("define")) {
                        readWord();
                        if (!currentStr.empty()) {
                            wstring key = currentStr;
                            readWordOrString();
                            defines[key] = currentStr;
                            LOG_INFOH(_T("DEFINE: lineNum=%d, %s=%s"), lineNumber + 1, key.c_str(), currentStr.c_str());
                        }
                    } else if (currentStr == _T("strokePosition")) {
                        readWordOrString();
                        defines[_T("defguide")] = currentStr;
                        LOG_INFOH(_T("StrokePosition: %s"), currentStr.c_str());
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
                    currentStr.append(1, getNextChar());
                    return TOKEN::FUNCTION;

                case '"':
                    // 文字列
                    readString();
                    return TOKEN::STRING;

                case '-':
                    parseArrow();
                    return TOKEN::ARROW;

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

        // ARROW
        void parseArrow() {
            char_t c = getNextChar();
            if (!is_numeral(c)) parseError();
            arrowIndex = c - '0';
            c = getNextChar();
            while (is_numeral(c)) {
                arrowIndex = arrowIndex * 10 + c - '0';
                c = getNextChar();
            }
            if (arrowIndex >= NORMAL_DECKEY_NUM) parseError();
            if (c != '>') parseError();
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

        void skipToEndOfLine() {
            nextPos = currentLine.size();
        }

        void readFile(wstring filename) {
            LOG_INFOH(_T("INCLUDE: %s"), filename.c_str());
            auto reader = utils::IfstreamReader(utils::joinPath(SETTINGS->rootDir, filename));
            if (reader.success()) {
                auto lines = reader.getAllLines();
                tableLines.insert(tableLines.begin() + lineNumber + 1, lines.begin(), lines.end());
            } else {
                LOG_ERROR(_T("Can't open: %s"), filename.c_str());
            }
        }

        // 読みこみに失敗した場合
        void parseError() {
            wchar_t buf[2] = { currentChar, 0 };
            tstring msg = utils::format(_T("テーブルファイルの %d 行 %d文字目('%s')がまちがっているようです"), lineNumber, nextPos, buf);
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
    LOG_INFOH(_T("CALLED: keys=%s, name=%s"), keys.c_str(), name.c_str());

    if (keys.empty()) return;

    std::vector<size_t> keyCodes;
    std::wregex reDigits(_T("^[Ss]?[0-9]+$"));
    for (auto k : utils::split(keys, ',')) {
        if (k.empty() || !std::regex_match(k, reDigits)) return;    // 10進数でなければエラー
        int shiftOffset = 0;
        if (k[0] == 'S' || k[0] == 's') {
            shiftOffset = SHIFT_DECKEY_START;
            k = k.substr(1);
        }
        keyCodes.push_back((size_t)utils::strToInt(k, -1) + shiftOffset);
    }
    StrokeTableNode* pNode = RootStrokeNode.get();
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
                LOG_INFOH(_T("RESET: depth=%d, key=%d, name=%s"), idx, key, name.c_str());
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
    auto ptr = std::make_unique<StrokeTreeBuilder>(lines);
    RootStrokeNode.reset(ptr->CreateStrokeTree());
    return RootStrokeNode.get();
}

