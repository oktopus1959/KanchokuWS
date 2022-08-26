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
#include "Oneshot/PostRewriteOneShot.h"


#if 0
#define IS_LOG_DEBUGH_ENABLED true
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
//#define LOG_TRACE LOG_INFO
#endif

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
        BARE_STRING,    // str
        FUNCTION,       // @?
        SLASH,          // /
        ARROW,          // -n>
        ARROW_BUNDLE,   // -*>-n>
        REWRITE,        // @{ : 後置書き換え
    };

    inline wstring getTokenString(TOKEN token) {
        switch (token) {
        case TOKEN::END: return _T("END");
        case TOKEN::LBRACE: return _T("LBRACE");
        case TOKEN::RBRACE: return _T("RBRACE");
        case TOKEN::COMMA: return _T("COMMA");
        case TOKEN::STRING: return _T("STRING");
        case TOKEN::BARE_STRING: return _T("BARE_STRING");
        case TOKEN::FUNCTION: return _T("FUNCTION");
        case TOKEN::SLASH: return _T("SLASH");
        case TOKEN::ARROW: return _T("ARROW");
        case TOKEN::ARROW_BUNDLE: return _T("ARROW_BUNDLE");
        case TOKEN::REWRITE: return _T("REWRITE");
        default: return _T("UNKNOWN");
        };
    }

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
    Node* createFunctionNode(wstring marker, int prevNum, int ) {
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

    // include/load ブロック情報のスタック
    class BlockInfoStack {
    private:
        DECLARE_CLASS_LOGGER;

        struct BlockInfo {
            wstring DirPath;        // インクルードする場合の起動ディレクトリ
            wstring BlockName;      // ファイル名やブロック名
            size_t OrigLineNumber;  // ブロックの開始行番号(0起点)
            size_t CurrentOffset;   // 当ブロック内での行番号を算出するための、真の起点から現在行におけるオフセット行数

            BlockInfo() { }

            BlockInfo(const wstring& dirPath, const wstring& name, size_t lineNum, size_t off)
                : DirPath(dirPath), BlockName(name), OrigLineNumber(lineNum), CurrentOffset(off) { }

            BlockInfo(const BlockInfo& info)
                : BlockInfo(info.DirPath, info.BlockName, info.OrigLineNumber, info.CurrentOffset) { }
        };

        std::vector<BlockInfo> blockInfoStack;

        wstring safeDirPath(const wstring& dirPath) {
            wstring path = dirPath;
            if (path.empty() && !blockInfoStack.empty()) {
                path = blockInfoStack.back().DirPath;
            }
            return path;
        }

        wstring emptyStr = _T("");

    public:
        const wstring& CurrentDirPath() {
            _LOG_DEBUGH(_T("PATH: %s"), blockInfoStack.empty() ? _T("(empty)") : blockInfoStack.back().DirPath.c_str());
            return blockInfoStack.empty() ? emptyStr : blockInfoStack.back().DirPath;
        }

        const wstring& CurrentBlockName() {
            _LOG_DEBUGH(_T("NAME: %s"), blockInfoStack.empty() ? _T("empty") : blockInfoStack.back().BlockName.c_str());
            return blockInfoStack.empty() ? emptyStr : blockInfoStack.back().BlockName;
        }

        size_t CurrentOffset() {
            _LOG_DEBUGH(_T("OFFSET: %d"), blockInfoStack.empty() ? 0 : blockInfoStack.back().CurrentOffset);
            return blockInfoStack.empty() ? 0 : blockInfoStack.back().CurrentOffset;
        }

        size_t CalcCurrentLineNumber(size_t lineNum) {
            return lineNum - CurrentOffset();
        }

        void Push(const wstring& dirPath, const wstring& name, size_t lineNum) {
            blockInfoStack.push_back(BlockInfo(dirPath, name, lineNum, lineNum));
        }

        void Pop(size_t nextLineNum) {
            _LOG_DEBUGH(_T("PUSH ENTER: nextLineNum=%d, dirPath=%s, blockName=%s, origLine=%d, offset=%d"), 
                nextLineNum, blockInfoStack.back().DirPath.c_str(), blockInfoStack.back().BlockName.c_str(), blockInfoStack.back().OrigLineNumber, blockInfoStack.back().CurrentOffset);
            size_t insertedTotalLineNum = nextLineNum - blockInfoStack.back().OrigLineNumber;
            blockInfoStack.pop_back();
            if (!blockInfoStack.empty()) {
                blockInfoStack.back().CurrentOffset += insertedTotalLineNum;
                _LOG_DEBUGH(_T("PUSH LEAVE: dirPath=%s, blockName=%s, origLine=%d, offset=%d"),
                    blockInfoStack.back().DirPath.c_str(), blockInfoStack.back().BlockName.c_str(), blockInfoStack.back().OrigLineNumber, blockInfoStack.back().CurrentOffset);
            }
        }
    };
    DEFINE_CLASS_LOGGER(BlockInfoStack);

    // ストローク木の作成クラス
    class StrokeTreeBuilder {
    private:
        DECLARE_CLASS_LOGGER;

        bool bPrimaryTable = false;

        std::vector<wstring>& tableLines;

        TOKEN currentToken = TOKEN::END;   // 最後に読んだトークン
        wstring currentStr;                 // 文字列トークン
        int arrowIndex = -1;                // ARROWインデックス
        size_t lineNumber = 0;              // 今読んでる行数

        wstring currentLine;                // 現在解析中の行
        size_t nextPos = 0;                 // 次の文字位置
        char_t currentChar = 0;             // 次の文字

        //bool bPostRewriteNodeFound = false; // 後置書き換え機能ノードがあったか

        // ブロック情報のスタック
        BlockInfoStack blockInfoStack;

        //std::map<wstring, wstring> defines; // 定義

        //wstring getAndRemoveDefines(const wstring& key) {
        //    wstring result;
        //    auto iter = defines.find(key);
        //    if (iter != defines.end()) {
        //        result = iter->second;
        //        defines.erase(key);
        //    }
        //    return result;
        //}

        // 同時打鍵定義ブロック
        bool isInCombinationBlock = false;

        // シフト面 -- 0:シフト無し、1:通常シフト、2:ShiftA, 3:ShiftB, ...
        int shiftPlane = 0;

        // 打鍵列
        std::vector<int> strokes;

        // 定義列マップ
        std::map<wstring, std::shared_ptr<std::vector<wstring>>> linesMap;

        //// 漢字置換マップ
        //std::map<wstring, wstring> kanjiConvMap;

        //const wstring& conv_kanji(const wstring& k) {
        //    auto iter = kanjiConvMap.find(k);
        //    return iter == kanjiConvMap.end() ? k : iter->second;
        //}

    public:
        // コンストラクタ
        // lines: ソースとなるテーブル定義
        // bPrimary: 主テーブルなら true を渡す。副テーブルや後からの定義差し込みなら false を渡す
        StrokeTreeBuilder(const wstring& tableFile, std::vector<wstring>& lines, bool bPrimary)
            : tableLines(lines), bPrimaryTable(bPrimary) {
            blockInfoStack.Push(SETTINGS->rootDir, tableFile, 0);
            if (!tableLines.empty()) {
                currentLine = tableLines[0];
                _LOG_DEBUGH(_T("currentLine(1)=%s"), currentLine.c_str());
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
            StrokeTableNode* rootNode = new StrokeTableNode(0, TOTAL_DECKEY_NUM);
            setupShiftedKeyFunction(rootNode);
            ParseTableSource(rootNode);
            //rootNode->setPostRewrite(bPostRewriteNodeFound);
            return rootNode;
        }

        // デフォルトのシフト面の機能(自身の文字を返す)ノードの設定
        void setupShiftedKeyFunction(StrokeTableNode* tblNode) {
            _LOG_DEBUGH(_T("CALLED"));
            for (size_t i = 0; i < NORMAL_DECKEY_NUM; ++i) {
                //tblNode->setNthChild(i + SHIFT_DECKEY_START, new MyCharNode());
                setNthChildNode(tblNode, i + SHIFT_DECKEY_START, new MyCharNode());
            }
        }

        int makeComboDecKeyIfInComboBlock(int decKey)
        {
            return isInCombinationBlock ? (decKey % PLANE_DECKEY_NUM) + COMBO_DECKEY_START : decKey;
        }

        // テーブル定義を解析してストローク木を構築する
        // 後から部分的にストローク定義を差し込む際にも使用される
        void ParseTableSource(StrokeTableNode* tblNode) {
            _LOG_DEBUGH(_T("ENTER"));
            readNextToken(0);
            while (currentToken != TOKEN::END) {
                switch (currentToken) {
                case TOKEN::LBRACE:
                    makeSubTree(tblNode, 0, 0);
                    break;

                case TOKEN::ARROW:
                    createNodePositionedByArrow(tblNode, 0, makeComboDecKeyIfInComboBlock(arrowIndex));
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
            _LOG_DEBUGH(_T("LEAVE"));
        }

        StrokeTableNode* makeSubTree(StrokeTableNode* tblNode, int depth, int prevNth) {
            //wstring myGuideChars = getAndRemoveDefines(_T("defguide"));   // フロントエンドでサポート
            _LOG_DEBUGH(_T("ENTER: tblNode=%p, depth=%d, parevNth=%d"), tblNode, depth, prevNth);

            if (tblNode == 0) tblNode = new StrokeTableNode(depth);
            int shiftPlaneOffset = depth == 0 ? shiftPlane * PLANE_DECKEY_NUM : 0;   // shift面によるオフセットは、ルートストロークだけに適用する
            int n = 0;
            bool isPrevDelim = true;
            readNextToken(depth);
            while (currentToken != TOKEN::RBRACE) { // '}' でブロックの終わり
                _LOG_DEBUGH(_T("token=%s"), getTokenString(currentToken).c_str());
                switch (currentToken) {
                case TOKEN::ARROW:
                    createNodePositionedByArrow(tblNode, prevNth, arrowIndex);
                    isPrevDelim = false;
                    break;

                case TOKEN::ARROW_BUNDLE:
                    allocateArrowBundle(tblNode, 0, arrowIndex);
                    break;

                case TOKEN::LBRACE:
                    if (shiftPlane == COMBO_SHIFT_PLANE) { _LOG_DEBUGH(_T("LBRACE: line=%d, depth=%d, shiftPlane=%d, prevNth=%d, nth=%d"), lineNumber + 1, depth, shiftPlane, prevNth, n + shiftPlaneOffset); }
                case TOKEN::STRING:             // "str" : 文字列ノード
                case TOKEN::BARE_STRING:        // str : 文字列ノード
                case TOKEN::FUNCTION:           // @c : 機能ノード
                case TOKEN::REWRITE:            // @{ : 書き換えノード }
                    //tblNode->setNthChild(n + shiftPlaneOffset, createNode(currentToken, depth + 1, prevNth, n));
                    setNthChildNode(tblNode, n + shiftPlaneOffset, createNode(currentToken, depth + 1, prevNth, n));
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

            //if (!myGuideChars.empty()) {
            //    _LOG_DEBUGH(_T("DEFGUID: %s"), myGuideChars.c_str());
            //    tblNode->MakeStrokeGuide(myGuideChars, bPrimaryTable);
            //}

            strokes.resize(depth);
            _LOG_DEBUGH(_T("LEAVE"));
            return tblNode;
        }

        void createNodePositionedByArrow(StrokeTableNode* tblNode, int prevNth, int idx) {
            int nextDepth = tblNode->depth() + 1;
            _LOG_DEBUGH(_T("ENTER: currentLine=%s, nextDepth=%d, idx=%d, prevN=%d"), currentLine.c_str(), nextDepth, idx, prevNth);
            Node* node = tblNode->getNth(idx);
            if (node && node->isStrokeTableNode()) {
                _LOG_DEBUGH(_T("tblNode[%d] has been created"), idx);
                createNodePositionedByArrowSub(dynamic_cast<StrokeTableNode*>(node), nextDepth, prevNth, idx);
            } else {
                //tblNode->setNthChild(idx, createNodePositionedByArrowSub(0, nextDepth, prevNth, idx));
                setNthChildNode(tblNode, idx, createNodePositionedByArrowSub(0, nextDepth, prevNth, idx));
                _LOG_DEBUGH(_T("tblNode->setNthChild(%d)"), idx);
            }
            _LOG_DEBUGH(_T("LEAVE"));
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
            Node* p = createNode(currentToken, depth, prevNth, nth);
            if (tblNode) {
                setNthChildNode(tblNode, nth, p);
                _LOG_DEBUGH(_T("tblNode->setNthChild(%d)"), nth);
            }
            return p;
        }

        // 矢印束記法(-*>-nn>)を第1打鍵位置に従って配置する
        void allocateArrowBundle(StrokeTableNode* tblNode, int depth, int nextArrowIdx) {
            _LOG_DEBUGH(_T("tblNode=%p, depth=%d, nextArrowIdx=%d"), tblNode, depth, nextArrowIdx);

            if (!tblNode) return;

            int shiftPlaneOffset = depth == 0 ? shiftPlane * PLANE_DECKEY_NUM : 0;   // shift面によるオフセットは、ルートストロークだけに適用する
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
                case TOKEN::BARE_STRING:        // str : 文字列ノード
                case TOKEN::FUNCTION:           // @c : 機能ノード
                case TOKEN::REWRITE:            // @{ : 書き換えノード
                    //getNodePositionedByArrowBundle(tblNode, n + shiftPlaneOffset)->setNthChild(nextArrowIdx, createNode(currentToken, depth + 2, n, nextArrowIdx, true));
                    setNthChildNode(
                        getNodePositionedByArrowBundle(tblNode, n + shiftPlaneOffset),
                        nextArrowIdx,
                        createNode(currentToken, depth + 2, n, nextArrowIdx/*, true*/));
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
            //tblNode->setNthChild(idx, stNode);
            setNthChildNode(tblNode, idx, stNode);
            return stNode;
        }

        Node* createNode(TOKEN token, int depth, int prevNth, int nth/*, bool bArrowBundle = false*/) {
            _LOG_DEBUGH(_T("ENTER: token=%s, depth=%d, prevNth=%d, nth=%d"), getTokenString(token).c_str(), depth, prevNth, nth);
            Node* pResult = 0;
            bool bBareStr = token == TOKEN::BARE_STRING;
            switch (token) {
            case TOKEN::LBRACE:
                strokes.push_back(nth);
                pResult = makeSubTree(0, depth, nth);
                strokes.pop_back();
                break;

            case TOKEN::RBRACE:
            case TOKEN::COMMA:             // ',' が来たら次のトークン
            case TOKEN::SLASH:             // '/' が来ても次のトークン
                break;

            case TOKEN::STRING:            // "str" : 文字列ノード
            case TOKEN::BARE_STRING:       // str : 文字列ノード
                LOG_TRACE(_T("STRING: %d:%d=%s, shiftPlane=%d"), lineNumber + 1, nth, currentStr.c_str(), shiftPlane);
                if (shiftPlane == COMBO_SHIFT_PLANE) { _LOG_DEBUGH(_T("STRING: %s: line=%d, depth=%d, shiftPlane=%d, prevNth=%d, nth=%d"), currentStr.c_str(), lineNumber + 1, depth, shiftPlane, prevNth, nth); }
                if (currentStr.empty()) {
                    _LOG_DEBUGH(_T("empty str"));
                    break;
                }
                //if (kanjiConvMap.empty()) {
                //    LOG_TRACE(_T("kanjiConvMap.empty()"));
                //    pResult = new StringNode(currentStr, false, bBareStr);
                //} else {
                //    wstring convStr = conv_kanji(currentStr);
                //    pResult = new StringNode(convStr, true, false);
                //}
                pResult = new StringNode(currentStr, /*false,*/ bBareStr);
                _LOG_DEBUGH(_T("new StringNode(%s)"), currentStr.c_str());
                break;

            case TOKEN::FUNCTION:          // @c : 機能ノード
                pResult = createFunctionNode(currentStr, prevNth, nth);
                break;

            case TOKEN::REWRITE:            // @{ : 書き換えノード '}
                pResult = createRewriteNode();
                break;

            default:                // 途中でファイルが終わったりした場合 : エラー
                parseError();
                break;
            }
            _LOG_DEBUGH(_T("LEAVE: %s: ptr=%p"), getTokenString(token).c_str(), pResult);
            return pResult;
        }

        Node* createRewriteNode() {
            _LOG_DEBUGH(_T("ENTER"));
            readWord();
            bool bBare = !currentStr.empty() && currentStr[0] != '"';
            PostRewriteOneShotNode* rewNode = new PostRewriteOneShotNode(utils::strip(currentStr, _T("\"")), bBare);

            skipToEndOfLine();
            readNextToken(0);
            while (currentToken != TOKEN::RBRACE) { // '}' でブロックの終わり
                auto items = utils::split(utils::strip(currentLine), '\t');
                if (items.size() == 2) {
                    _LOG_DEBUGH(_T("REWRITE: %s -> %s"), items[0].c_str(), items[1].c_str());
                    auto key = utils::strip(items[0], _T("\""));
                    if (items[1] == _T("{")) {
                        _LOG_DEBUGH(_T("REWRITE: Add SubTable"), items[0].c_str(), items[1].c_str());
                        skipToEndOfLine();
                        auto p = makeSubTree(0, 1, 0);
                        rewNode->addRewritePair(key, _T(""), false, p);
                    } else {
                        bBare = items[1].size() > 0 && items[1][0] != '"';
                        rewNode->addRewritePair(key, utils::strip(items[1], _T("\"")), bBare, 0);
                        skipToEndOfLine();
                    }
                } else {
                    parseError();
                    break;
                }

                readNextToken(0);
            }

            //bPostRewriteNodeFound = true;
            _LOG_DEBUGH(_T("LEAVE: rewNode=%p"), rewNode);
            return rewNode;
        }

        // 親ノードに対して、n番目の子ノードをセットする
        void setNthChildNode(StrokeTableNode* parentNode, size_t n, Node* childNode) {
            if (parentNode && childNode) {
                Node* pn = parentNode->getNth(n);   // 既存ノード
                if (pn) {
                    _LOG_DEBUGH(_T("OVERWRITE: pn=%s(%p), str=%s, cn=%s(%p), str=%s"),
                        NODE_NAME_PTR(pn), pn, pn->getString().c_str(), NODE_NAME_PTR(childNode), childNode, childNode->getString().c_str());
                }
                if (!isInCombinationBlock) {
                    // 同時打鍵ブロック以外ならば上書きOK
                    PostRewriteOneShotNode* pp = pn ? dynamic_cast<PostRewriteOneShotNode*>(pn) : 0;
                    PostRewriteOneShotNode* cp = dynamic_cast<PostRewriteOneShotNode*>(childNode);
                    if (pp && cp) {
                        // 後置書き換えが重複した場合は、書き換え規則のマージ
                        pp->merge(*cp);
                        LOG_INFOH(_T("PostRewriteOneShotNode merged: pp(%p)=%s, tblNum=%d, cn(%p)=%s"), pp, pp->getDebugString().c_str(), pp->getSubTableNum(), cp, cp->getDebugString().c_str());
                        delete childNode;
                    } else {
                        // 後からのほうで上書きする(前のやつは delete される)
                        parentNode->setNthChild(n, childNode);
                    }
                } else {
                    // 同時打鍵ブロックの場合
                    if (pn == 0 || pn->isFunctionNode()) {
                        // 未割り当て、または機能ノードならば上書きOK
                        parentNode->setNthChild(n, childNode);
                    } else if (childNode->isFunctionNode()) {
                        // 重複していて、新子ノードが機能ノードなら無視
                    } else {
                        // 重複していて、既存ノードも新子ノードも機能ノード以外なら警告
                        LOG_WARN(_T("DUPLICATED: %s"), currentLine.c_str());
                        nodeDuplicateWarning();
                    }
                }
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
            _LOG_DEBUGH(_T("currentToken=%s"), getTokenString(currentToken).c_str());
        }

        bool bIgnoreWarningAll = false;
        bool bIgnoreWarningBraceLevel = false;
        int braceLevel = 0;

        // トークンを読む
        TOKEN getToken(int depth) {
            currentStr.clear();
            arrowIndex = -1;
            while (true) {
                switch (getNextChar()) {
                case '#': {
                    // '#include', '#define', '#strokePosition', '#*shift*', '#overlapping', '#yomiConvert', '#store', '#load', '#end', '#ignoreWarning' または '#' 以降、行末までコメント
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
                            //defines[key] = currentStr;
                            _LOG_DEBUGH(_T("DEFINE: lineNum=%d, %s=%s"), lineNumber + 1, key.c_str(), currentStr.c_str());
                        }
                    } else if (lcStr == _T("store")) {
                        std::shared_ptr<std::vector<wstring>> lines;
                        readWord();
                        if (currentStr.empty()) {
                            parseError();
                        } else {
                            lines.reset(new std::vector<wstring>());
                            linesMap[currentStr] = lines;
                            _LOG_DEBUGH(_T("SET: lineNum=%d, %s"), lineNumber + 1, currentStr.c_str());
                        }
                        while (getNextLine()) {
                            if (utils::startsWith(currentLine, _T("#end"))) {
                                lines->push_back(_T("#end __include__"));
                                break;
                            }
                            if (lines) {
                                lines->push_back(currentLine);
                            }
                        }
                    } else if (lcStr == _T("load")) {
                        readWord();
                        if (currentStr.empty()) {
                            parseError();
                        } else {
                            auto iter = linesMap.find(currentStr);
                            if (iter == linesMap.end()) {
                                parseError();
                            } else {
                                _LOG_DEBUGH(_T("LOAD: %s"), currentStr.c_str());
                                auto lines = iter->second;
                                size_t nextLineNum = lineNumber + 1;
                                tableLines.insert(tableLines.begin() + nextLineNum, lines->begin(), lines->end());
                                blockInfoStack.Push(_T(""), currentStr, nextLineNum);
                            }
                        }

                    // フロントエンドでサポート
                    //} else if (utils::startsWith(lcStr, _T("yomiconv"))) {
                    //    readWord();
                    //    auto keyword = currentStr;
                    //    _LOG_DEBUGH(_T("YomiConversion: keyword=%s"), keyword.c_str());
                    //    if (keyword == _T("clear") || keyword == _T("end")) {
                    //        kanjiConvMap.clear();
                    //    } else {
                    //        _LOG_DEBUGH(_T("YomiConversion: %s"), SETTINGS->kanjiYomiFile.c_str());
                    //        if (!SETTINGS->kanjiYomiFile.empty()) readKanjiConvFile(SETTINGS->kanjiYomiFile, true);
                    //        if (keyword == _T("with")) {
                    //            readWordOrString();
                    //            if (!currentStr.empty()) {
                    //                _LOG_DEBUGH(_T("YomiConversion: %s"), currentStr.c_str());
                    //                readKanjiConvFile(currentStr, false);
                    //            }
                    //        }
                    //    }

                    // フロントエンドでサポート
                    //} else if (lcStr == _T("strokePosition")) {   
                    //    readWordOrString();
                    //    defines[_T("defguide")] = currentStr;
                    //    _LOG_DEBUGH(_T("StrokePosition: %s"), currentStr.c_str());

                    } else if (lcStr == _T("noshift") || lcStr == _T("normal")) {
                        shiftPlane = 0;
                    } else if (lcStr == _T("shift")) {
                        shiftPlane = 1;
                    } else if (lcStr == _T("shifta")) {
                        shiftPlane = 2;
                    } else if (lcStr == _T("shiftb")) {
                        shiftPlane = 3;
                    } else if (lcStr == _T("shiftc")) {
                        shiftPlane = 4;
                    } else if (lcStr == _T("shiftd")) {
                        shiftPlane = 5;
                    } else if (lcStr == _T("shifte")) {
                        shiftPlane = 6;
                    } else if (lcStr == _T("shiftf")) {
                        shiftPlane = 7;
                    } else if (lcStr == _T("combination") || lcStr == _T("overlapping")) {
                        _LOG_DEBUGH(_T("START Combination: %s"), currentLine.c_str());
                        isInCombinationBlock = true;
                        skipToEndOfLine();
                    } else if (lcStr == _T("end")) {
                        readWord();
                        auto word = utils::toLower(currentStr);
                        _LOG_DEBUGH(_T("end %s"), word.c_str());
                        if (word == _T("combination") || word == _T("overlapping")) {
                            _LOG_DEBUGH(_T("END Combination: %s"), currentLine.c_str());
                            isInCombinationBlock = false;
                        } else if (word == _T("__include__")) {
                            _LOG_DEBUGH(_T("END INCLUDE/LOAD: lineNumber=%d"), lineNumber);
                            blockInfoStack.Pop(lineNumber + 1);
                        } else if (word == _T("shift")) {
                            shiftPlane = 0;
                        }
                        skipToEndOfLine();
                    } else if (lcStr == _T("ignorewarning")) {
                        readWord();
                        auto word = utils::toLower(currentStr);
                        if (word.empty()) {
                            bIgnoreWarningAll = true;
                            bIgnoreWarningBraceLevel = true;
                        } else if (word == _T("bracelevel")) {
                            bIgnoreWarningBraceLevel = true;
                        }
                    } else {
                        _LOG_DEBUGH(_T("#%s"), currentStr.c_str());
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

                case '{':
                    if (!bIgnoreWarningBraceLevel && nextPos == 1 && braceLevel > 0) unexpectedLeftBraceAtColumn0Warning();
                    ++braceLevel;
                    return TOKEN::LBRACE;

                case '}':
                    if (!bIgnoreWarningBraceLevel && nextPos == 1 && braceLevel > 1) unexpectedRightBraceAtColumn0Warning();
                    --braceLevel;
                    return TOKEN::RBRACE;

                case ',': return TOKEN::COMMA;
                case '|': return TOKEN::COMMA;

                case '/':
                    if (peekNextChar() == '/') {
                        // 2重スラッシュはコメント扱い
                        skipToEndOfLine();
                        break;
                    }
                    readBareString(currentChar);
                    if (currentStr.size() > 1) return TOKEN::BARE_STRING;
                    return TOKEN::SLASH;

                case '\n':
                case ' ':                   // SPC : スキップ
                case '\t':                  // TAB : スキップ
                case '\r':                  // ^M  : スキップ
                case '\f':                  // ^L  : スキップ
                    break;

                case '@':
                    // 機能
                    if (peekNextChar() == '{') {
                        getNextChar();
                        return TOKEN::REWRITE;
                    }
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
                    readBareString(currentChar);
                    if (!currentStr.empty()) return TOKEN::BARE_STRING;

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

        // スラッシュも含む
        bool isOutputChar(wchar_t c) {
            return (c == '/' || (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c >= 0x80);
        }

        // 何らかのデリミタが来るまで読みこんで、currentStr に格納。
        void readBareString(wchar_t c = '\0') {
            currentStr.clear();
            if (c != '\0') {
                if (!isOutputChar(c)) return;   // エラー
                currentStr.append(1, c);
            }
            while (true) {
                c = peekNextChar();
                if (!isOutputChar(c)) break;
                getNextChar();
                currentStr.append(1, c);
            }
            _LOG_DEBUGH(_T("RESULT: %s"), currentStr.c_str());
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

        // 行末までの範囲で次の空白文字またはコメント文字までを読み込んで、currentStr に格納。
        void readWord() {
            currentStr.clear();
            char_t c = skipSpace();
            if (c <= ' ') return;

            if (c == ';' || (c == '/' && peekNextChar() == '/')) {
                skipToEndOfLine();
                return;
            }

            readWordSub(c);
        }

        // 行末までの間で、次の空白文字までを読み込んで、currentStr に格納。
        void readWordSub(wchar_t c) {
            currentStr.append(1, c);
            while (true) {
                c = getNextChar();
                if (c <= ' ') return;
                currentStr.append(1, c);
            }
        }

        // 行末までの間で、文字列または単語を読み込む
        void readWordOrString() {
            currentStr.clear();
            char_t c = skipSpace();
            if (c > ' ') {
                if (c == '"') {
                    readString();
                } else if (c == ';' || (c == '/' && peekNextChar() == '/')) {
                    skipToEndOfLine();
                } else {
                    readWordSub(c);
                }
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
            int funckeyOffset = 0;
            bool bShiftPlane = false;
            //char_t c = getNextChar();
            if (c == 'N' || c == 'n') {
                shiftOffset = 0;
                c = getNextChar();
            } else if (c == 'S' || c == 's') {
                shiftOffset = SHIFT_DECKEY_START;
                c = getNextChar();
            } else if (c >= 'A' && c <= 'F') {
                shiftOffset = SHIFT_A_DECKEY_START + (c - 'A') * PLANE_DECKEY_NUM;
                c = getNextChar();
            } else if (c >= 'a' && c <= 'f') {
                shiftOffset = SHIFT_A_DECKEY_START + (c - 'a') * PLANE_DECKEY_NUM;
                c = getNextChar();
            } else if (c == 'X' || c == 'x') {
                shiftOffset = 0;
                funckeyOffset = FUNC_DECKEY_START;
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
            arrowIndex += funckeyOffset;
            if (shiftPlane > 0) arrowIndex %= PLANE_DECKEY_NUM;    // 後で Offset を足すので Modulo 化しておく
            if (!bShiftPlane) {
                //if (isInCombinationBlock) {
                //    // 同時打鍵ブロック用の Offset
                //    shiftOffset = COMBO_DECKEY_START;
                //} else
                if (shiftOffset < 0) {
                    // シフト面のルートノードで明示的にシフトプレフィックスがなければ、shiftOffset をセット
                    shiftOffset = (shiftPlane > 0 && depth == 0) ? shiftOffset = shiftPlane * PLANE_DECKEY_NUM : 0;
                }
                arrowIndex += shiftOffset;
                if (arrowIndex >= COMBO_DECKEY_END) parseError();
            } else {
                shiftPlane = arrowIndex;
                if (shiftPlane >= ALL_PLANE_NUM) parseError();
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
            if (arrowIndex >= PLANE_DECKEY_NUM) parseError();
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
                _LOG_DEBUGH(_T("currentLine(%d)=%s"), lineNumber + 1, currentLine.c_str());
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

        bool getNextLine() {
            ++lineNumber;
            if (lineNumber >= tableLines.size()) {
                return false;
            }
            currentLine = tableLines[lineNumber];
            _LOG_DEBUGH(_T("currentLine(%d)=%s"), lineNumber + 1, currentLine.c_str());
            return true;
        }

        void skipToEndOfLine() {
            nextPos = currentLine.size();
            currentChar = '\n';
        }

        void readFile(const wstring& filename) {
            auto includeFilePath = utils::joinPath(blockInfoStack.CurrentDirPath(), utils::canonicalizePathDelimiter(filename));
            _LOG_DEBUGH(_T("INCLUDE: FILE PATH: %s"), includeFilePath.c_str());
            auto reader = utils::IfstreamReader(includeFilePath);
            if (reader.success()) {
                auto lines = reader.getAllLines();
                lines.push_back(_T("#end __include__"));
                size_t nextLineNum = lineNumber + 1;
                tableLines.insert(tableLines.begin() + nextLineNum, lines.begin(), lines.end());
                blockInfoStack.Push(utils::getParentDirPath(includeFilePath), filename, nextLineNum);
            } else {
                LOG_ERROR(_T("Can't open: %s"), includeFilePath.c_str());
                fileOpenError(filename);
            }
        }

        //// 漢字置換ファイルを読み込む
        //// 一行の形式は「漢字 [<TAB>|Space]+ 読みの並び('|'区切り)」
        //// 読みの並びの優先順は以下のとおり:
        //// ①2文字以上のカタカナ
        //// ②2文字以上のひらがな
        //// ③漢字
        //// bOnlyYomi == true なら、エントリの上書き禁止でカタカナをひらがなに変換
        //// bOnlyYomi == false なら、エントリの上書きOKで、カタカナはそのまま
        //void readKanjiConvFile(const wstring& filename, bool bOnlyYomi) {
        //    std::wregex reComment(_T("#.*"));
        //    std::wregex reBlank(_T("[\\t ]+"));
        //    std::wregex reKatakanaMulti(_T("[ァ-ン]{2,}"));
        //    std::wregex reHiraganaMulti(_T("[ぁ-ん]{2,}"));
        //    _LOG_DEBUGH(_T("filename: %s, bOnlyYomi=%s"), filename.c_str(), BOOL_TO_WPTR(bOnlyYomi));
        //    auto reader = utils::IfstreamReader(utils::joinPath(SETTINGS->rootDir, filename));
        //    if (reader.success()) {
        //        auto lines = reader.getAllLines();
        //        _LOG_DEBUGH(_T("lines.size(): %d"), lines.size());
        //        for (auto line : lines) {
        //            auto items = utils::split(utils::strip(std::regex_replace(std::regex_replace(line, reComment, _T("")), reBlank, _T(" "))), ' ');
        //            if (items.size() >= 2) {
        //                auto kanji = items[0];
        //                if (!kanji.empty() && !items[1].empty()) {
        //                    if (!bOnlyYomi || kanjiConvMap.find(kanji) == kanjiConvMap.end()) {
        //                        if (!bOnlyYomi) {
        //                            auto yomi = items[1];
        //                            if (!yomi.empty()) {
        //                                kanjiConvMap[kanji] = yomi;
        //                                kanjiConvMap[yomi] = kanji;
        //                            }
        //                        } else {
        //                            std::wsmatch results;
        //                            if (std::regex_search(items[1], results, reKatakanaMulti)) {
        //                                auto yomi = utils::convert_katakana_to_hiragana(results.str());
        //                                if (!yomi.empty()) kanjiConvMap[kanji] = yomi;
        //                            } else if (std::regex_search(items[1], results, reHiraganaMulti)) {
        //                                auto yomi = results.str();
        //                                if (!yomi.empty()) kanjiConvMap[kanji] = yomi;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        _LOG_DEBUGH(_T("kanjiConvMap.size(): %d"), kanjiConvMap.size());
        //    } else {
        //        LOG_ERROR(_T("Can't open: %s"), filename.c_str());
        //        fileOpenError(filename);
        //    }
        //}

        inline wstring blockOrFile() {
            return blockInfoStack.CurrentDirPath().empty() ? _T("ブロック") : _T("テーブルファイル");
        }

        inline size_t calcErrorLineNumber() {
            return blockInfoStack.CalcCurrentLineNumber(lineNumber + 1);
        }

        inline size_t calcErrorColumn() {
            if (nextPos == 0 && lineNumber > 0) return tableLines[lineNumber - 1].size();
            return nextPos;
        }

        // 解析に失敗した場合
        void parseError() {
            _LOG_DEBUGH(_T("lineNumber=%d, nextPos=%d"), lineNumber, nextPos);
            wchar_t buf[2] = { currentChar, 0 };
            handleError(utils::format(_T("%s %s の %d行 %d文字目('%s')がまちがっているようです：\r\n> %s ..."), \
                blockOrFile().c_str(), blockInfoStack.CurrentBlockName().c_str(), calcErrorLineNumber(), calcErrorColumn(), buf, currentLine.substr(0, 50).c_str()));
        }

        // ファイルの読み込みに失敗した場合
        void fileOpenError(const wstring& filename) {
            _LOG_DEBUGH(_T("lineNumber=%d, nextPos=%d"), lineNumber, nextPos);
            handleError(utils::format(_T("ファイル %s を読み込めません。\r\nテーブルファイル %s の %d行目がまちがっているようです：\r\n> %s ..."), \
                filename.c_str(), blockInfoStack.CurrentBlockName().c_str(), calcErrorLineNumber(), currentLine.substr(0, 50).c_str()));
        }

        // ノードの重複が発生した場合
        void nodeDuplicateWarning() {
            _LOG_DEBUGH(_T("lineNumber=%d, nextPos=%d"), lineNumber, nextPos);
            handleWarning(utils::format(_T("%s %s の %d行目でノードの重複が発生しました：\r\n> %s ..."), \
                blockOrFile().c_str(), blockInfoStack.CurrentBlockName().c_str(), calcErrorLineNumber(), currentLine.substr(0, 50).c_str()));
        }

        // カラム0で予期しないLBRACEが発生
        void unexpectedLeftBraceAtColumn0Warning() {
            _LOG_DEBUGH(_T("lineNumber=%d, nextPos=%d"), lineNumber, nextPos);
            handleWarning(utils::format(_T("%s %s の %d行目の行頭にネストされた '{' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述すると無視されます)：\r\n> %s ..."), \
                blockOrFile().c_str(), blockInfoStack.CurrentBlockName().c_str(), calcErrorLineNumber(), currentLine.substr(0, 50).c_str()));
        }

        // カラム0で予期しないRBRACEが発生
        void unexpectedRightBraceAtColumn0Warning() {
            _LOG_DEBUGH(_T("lineNumber=%d, nextPos=%d"), lineNumber, nextPos);
            handleWarning(utils::format(_T("%s %s の %d行目の行頭にまだネスト中の '}' があります。意図したものであれば無視してください (#ignoreWarning braceLevel を記述すると無視されます)：\r\n> %s ..."), \
                blockOrFile().c_str(), blockInfoStack.CurrentBlockName().c_str(), calcErrorLineNumber(), currentLine.substr(0, 50).c_str()));
        }

        // エラー処理
        void handleError(const wstring& msg) {
            LOG_ERROR(msg);
            LOG_ERROR(_T("lines=\n%s"), makeErrorLines().c_str());
            // エラーメッセージを投げる
            ERROR_HANDLER->Error(msg);
        }

        // 警告ー処理
        void handleWarning(const wstring& msg) {
            LOG_WARN(msg);
            LOG_WARN(_T("lines=\n%s"), makeErrorLines().c_str());
            // エラーメッセージを投げる
            ERROR_HANDLER->Warn(msg);
        }

        wstring makeErrorLines() {
            wstring lines;
            for (size_t i = 9; i > 0; --i) {
                if (lineNumber >= i) lines = lines + tableLines[lineNumber - i] + _T("\n");
            }
            lines = lines + _T(">> ") + currentLine + _T("\n");
            for (size_t i = 1; i < 10; ++i) {
                if (lineNumber + i < tableLines.size())lines = lines + tableLines[lineNumber + i] + _T("\n");
            }
            return lines;
        }
    };
    DEFINE_CLASS_LOGGER(StrokeTreeBuilder);

    // 機能の再割り当て
    void assignFucntion(StrokeTableNode* pNode, const wstring& keys, const wstring& name) {
        _LOG_DEBUGH(_T("CALLED: keys=%s, name=%s"), keys.c_str(), name.c_str());

        if (pNode == 0 || keys.empty()) return;

        std::vector<size_t> keyCodes;
        std::wregex reDigits(_T("^[SsAaBbXx]?[0-9]+$"));
        for (auto k : utils::split(keys, ',')) {
            if (k.empty() || !std::regex_match(k, reDigits)) return;    // 10進数でなければエラー
            int shiftOffset = 0;
            int funckeyOffset = 0;
            if (k[0] == 'S' || k[0] == 's') {
                shiftOffset = SHIFT_DECKEY_START;
                k = k.substr(1);
            } else if (k[0] >= 'A' && k[0] <= 'F') {
                shiftOffset = SHIFT_DECKEY_START + (k[0] - 'A' + 2) * PLANE_DECKEY_NUM;
                k = k.substr(1);
            } else if (k[0] >= 'a' && k[0] <= 'f') {
                shiftOffset = SHIFT_DECKEY_START + (k[0] - 'a' + 2) * PLANE_DECKEY_NUM;
                k = k.substr(1);
            } else if (k[0] == 'X' || k[0] == 'x') {
                shiftOffset = 0;
                funckeyOffset = FUNC_DECKEY_START;
                k = k.substr(1);
            }
            keyCodes.push_back((size_t)utils::strToInt(k, -1) + funckeyOffset + shiftOffset);
        }

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

} // namespace

DEFINE_CLASS_LOGGER(StrokeTableNode);

// 機能の再割り当て
void StrokeTableNode::AssignFucntion(const wstring& keys, const wstring& name) {
    if (keys.empty()) return;

    if (RootStrokeNode1) assignFucntion(RootStrokeNode1.get(), keys, name);
    if (RootStrokeNode2) assignFucntion(RootStrokeNode2.get(), keys, name);
}

// ストロークノードの更新
void StrokeTableNode::UpdateStrokeNodes(const wstring& strokeSource) {
    auto list = utils::split(strokeSource, '\n');
    if (RootStrokeNode1) StrokeTreeBuilder(_T("(none)"), list, false).ParseTableSource(RootStrokeNode1.get());
    if (RootStrokeNode2) StrokeTreeBuilder(_T("(none)"), list, false).ParseTableSource(RootStrokeNode2.get());
}

// ストローク木を作成してそのルートを返す
StrokeTableNode* StrokeTableNode::CreateStrokeTree(const wstring& tableFile, std::vector<wstring>& lines) {
    ROOT_STROKE_NODE = 0;
    ROOT_STROKE_NODE = StrokeTreeBuilder(tableFile, lines, true).CreateStrokeTree();
    RootStrokeNode1.reset(ROOT_STROKE_NODE);
    return ROOT_STROKE_NODE;
}

// ストローク木2を作成してそのルートを返す
StrokeTableNode* StrokeTableNode::CreateStrokeTree2(const wstring& tableFile, std::vector<wstring>& lines) {
    RootStrokeNode2.reset(StrokeTreeBuilder(tableFile, lines, false).CreateStrokeTree());
    return RootStrokeNode2.get();
}

