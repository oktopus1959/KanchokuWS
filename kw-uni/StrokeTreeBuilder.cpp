//#include "pch.h"

// このソースのかなりの部分は、オリジナル漢直Winの parser.c のソースコードを流用しています

#include "string_utils.h"
#include "Logger.h"
#include "ErrorHandler.h"

#include "Node.h"
#include "StrokeTable.h"
#include "StringNode.h"
#include "FunctionNodeManager.h"
#include "HotkeyToChars.h"

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
        //    return new StringNode(HOTKEY_TO_CHARS->GetCharFromHotkey(myNum));
        //case BuiltInMarker::PrevChar:
        //    return new StringNode(HOTKEY_TO_CHARS->GetCharFromHotkey(prevNum));
        case BuiltInMarker::NumberInCircle:
            return new StringNode(makeNumberInCircle(prevNum));
        case BuiltInMarker::WidePrevChar:
            return new StringNode(makeFullWideChar(HOTKEY_TO_CHARS->GetCharFromHotkey(prevNum)));
        case BuiltInMarker::WideShiftPrevChar:
            return new StringNode(makeFullWideChar(HOTKEY_TO_CHARS->GetCharFromHotkey(prevNum + NUM_STROKE_HOTKEY)));
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
        size_t lineNumber = 0;              // 今読んでる行数

        tstring currentLine;                // 現在解析中の行
        size_t currentPos = 0;              // 次の文字位置
        char_t currentChar = 0;             // 次の文字

    public:
        StrokeTreeBuilder(std::vector<tstring>& lines)
            : tableLines(lines) {
        }

        // ストローク木を作成する
        // エラーがあったら例外を投げる
        StrokeTableNode* CreateStrokeTree() {
            readNextToken();
            checkCurrentToken(TOKEN::LBRACE);         // 最初の '{'
            return createSubTree(0, 0);
        }

        StrokeTableNode* createSubTree(int depth, int nth) {
            StrokeTableNode* node = new StrokeTableNode(depth);
            size_t numComma = 0;
            readNextToken();
            while (currentToken != TOKEN::END) {
                // '}' が来たら部分木の終わり
                if (currentToken == TOKEN::RBRACE) break;

                switch (currentToken) {
                case TOKEN::LBRACE:
                    node->addNode(createSubTree(depth + 1, node->numChildren()));
                    break;
                case TOKEN::COMMA:             // ',' が来たら次のトークン
                case TOKEN::SLASH:             // '/' が来ても次のトークン
                    ++numComma;
                    LOG_TRACE(_T("COMMA: numChildren=%d, numComma=%d"), node->numChildren(), numComma);
                    if (node->numChildren() < numComma) {
                        // 前トークンが空だった
                        LOG_TRACE(_T("COMMA: previous token is null: numChildren=%d, numComma=%d"), node->numChildren(), numComma);
                        node->addNode(0);
                    }
                    numComma = node->numChildren();
                    break;
                case TOKEN::STRING:            // "str" : 文字列ノード
                    LOG_TRACE(_T("%d:%d=%s"), lineNumber, numComma, currentStr.c_str());
                    node->addNode(new StringNode(currentStr));
                    break;
                case TOKEN::FUNCTION:          // @c : 機能ノード
                    node->addNode(createFunctionNode(currentStr, nth, node->numChildren()));
                    break;
                default:                // 途中でファイルが終わったりした場合 : エラー
                    parseError();
                    break;
                }
                readNextToken();
            }
            return node;
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
            while (true) {
                switch (getNextChar()) {
                case '#':
                case ';':
                    // '#' または ';' 以降、行末までコメント
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

        char_t getNextChar() {
            if (currentPos >= currentLine.size()) {
                ++lineNumber;
                while (lineNumber <= tableLines.size()) {
                    currentLine = tableLines[lineNumber - 1];
                    if (!currentLine.empty()) {
                        currentPos = 0;
                        LOG_DEBUG(_T("%d: %s"), lineNumber, currentLine.c_str());
                        break;
                    }
                    ++lineNumber;
                }
            }
            currentChar = currentPos < currentLine.size() ? currentLine[currentPos++] : 0;
            return currentChar;
        }

        void skipToEndOfLine() {
            currentPos = currentLine.size();
        }

        // 読みこみに失敗した場合
        void parseError() {
            tstring msg = utils::format(_T("テーブルファイルの %d 行 %d文字目('%c')がまちがっているようです"), lineNumber, currentPos, currentChar);
            LOG_ERROR(msg);
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
    std::wregex reDigits(_T("^[0-9]+$"));
    for (auto k : utils::split(keys, ',')) {
        if (k.empty() || !std::regex_match(k, reDigits)) return;    // 10進数でなければエラー
        keyCodes.push_back((size_t)utils::strToInt(k, -1));
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
                pNode->setNth(key, FunctionNodeManager::CreateFunctionNodeByName(name));
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

