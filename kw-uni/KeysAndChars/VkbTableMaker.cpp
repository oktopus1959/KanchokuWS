// StrokeTableMaker
#include "file_utils.h"
#include "path_utils.h"
#include "StrokeTable.h"
#include "StringNode.h"
#include "FunctionNode.h"
#include "MyPrevChar.h"
#include "DecKeyToChars.h"
#include "Settings/Settings.h"

#include "VkbTableMaker.h"
#include "OneShot/PostRewriteOneShot.h"
//#define OUT_TABLE_SIZE 200
//#define VKB_TABLE_SIZE 50

#define _LOG_DEBUGH_FLAG false 
#if 0
#define IS_LOG_DEBUGH_ENABLED true
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

namespace VkbTableMaker {
    DEFINE_NAMESPACE_LOGGER(VkbTableMaker);

    inline void set_facestr(mchar_t m, wchar_t* faces) {
        auto p = decomp_mchar(m);
        if (p.first != 0) {
            *faces++ = p.first;
        } else {
            faces[1] = 0;
        }
        *faces = p.second;
    }

    //----------------------------------------------------------------------------
    wstring hiraganaArray1 = _T("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもや ゆ よらりるれろわ ん を");

    wstring hiraganaArray2 = _T("ぁぃぅぇぉがぎぐげござじずぜぞだぢづでど     ばびぶべぼぱぴぷぺぽゃ ゅ ょゕ  ゖ ゎゐゔゑ ");

    wstring katakanaArray1 = _T("アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤ ユ ヨラリルレロワ ン ヲ");

    wstring katakanaArray2 = _T("ァィゥェォガギグゲゴザジズゼゾダヂヅデド     バビブベボパピプペポャ ュ ョヵ  ヶ ヮヰヴヱ ");

    // ひらがなに到る第1打鍵集合
    std::set<int> hiraganaFirstIndexes;

    void makeStrokeKeysTable(wchar_t* table, std::set<int>* pSet, StrokeTableNode* pNode, const std::map<wchar_t, size_t>& idxMap, size_t firstIdx, size_t secondIdx, size_t depth) {
        for (size_t i = 0; i < NORMAL_DECKEY_NUM; ++i) {
            if (depth == 0)
                firstIdx = i;
            else if (depth == 1)
                secondIdx = i;
            Node* blk = pNode->getNth(i);
            if (blk) {
                if (blk->isStrokeTableNode()) {
                    makeStrokeKeysTable(table, pSet, (StrokeTableNode*)blk, idxMap, firstIdx, secondIdx, depth + 1);
                } else if (blk->isStringNode()) {
                    auto iter = idxMap.find((wchar_t)blk->getString()[0]);
                    if (iter != idxMap.end()) {
                        if (table) {
                            // DecKeyId をキー名に変換してテーブルに格納
                            table[iter->second * 2] = DECKEY_TO_CHARS->GetCharFromDeckey(firstIdx);
                            table[iter->second * 2 + 1] = DECKEY_TO_CHARS->GetCharFromDeckey(secondIdx);
                        }
                        if (pSet) {
                            pSet->insert(pSet->end(), (int)firstIdx);
                        }
                    }
                }
            }
        }
    }

    // 指定の文字配列をストロークキー配列に変換
    void MakeStrokeKeysTable(wchar_t* table, const wchar_t* targetChars) {
        LOG_INFO(_T("CALLED"));
        if (ROOT_STROKE_NODE) {
            std::map<wchar_t, size_t> indexMap;

            for (size_t i = 0; targetChars[i] && i < VKB_TABLE_SIZE; ++i) {
                indexMap[targetChars[i]] = i;
            }
            for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
                table[i] = 0;
            }
            makeStrokeKeysTable(table, nullptr, ROOT_STROKE_NODE, indexMap, STROKE_SPACE_DECKEY, STROKE_SPACE_DECKEY, 0);
        }
    }

    void setIndexMap(std::map<wchar_t, size_t>& map, const wstring& kanaArray1, const wstring& kanaArray2) {
        for (size_t i = 0; i < VKB_TABLE_SIZE && i < kanaArray1.size(); ++i) {
            map[kanaArray1[i]] = i;
        }
        for (size_t i = 0; i < VKB_TABLE_SIZE && i < kanaArray2.size(); ++i) {
            map[kanaArray2[i]] = i + 50;
        }
    }

    void makeKanaTable(wchar_t* table, const wstring& kanaArray1, const wstring& kanaArray2) {
        if (ROOT_STROKE_NODE) {
            std::map<wchar_t, size_t> indexMap;
            setIndexMap(indexMap, kanaArray1, kanaArray2);

            for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
                table[i] = 0;
            }

            makeStrokeKeysTable(table, nullptr, ROOT_STROKE_NODE, indexMap, 0, 0, 0);
        }
    }

    // ひらがな50音図配列を作成する (あかさたなはまやらわ、ぁがざだばぱゃ)
    void MakeVkbHiraganaTable(wchar_t* table) {
        LOG_INFO(_T("CALLED"));
        makeKanaTable(table, hiraganaArray1, hiraganaArray2);
    }

    // カタカナ50音図配列を作成する (アカサタナハマヤラワ、ァガザダバパャヮ)
    void MakeVkbKatakanaTable(wchar_t* table) {
        LOG_INFO(_T("CALLED"));
        makeKanaTable(table, katakanaArray1, katakanaArray2);
    }

    // ひらがなに到る第1打鍵集合を取得する
    const std::set<int>& GetHiraganaFirstDeckeys() {
        if (ROOT_STROKE_NODE) {
            if (hiraganaFirstIndexes.empty()) {
                std::map<wchar_t, size_t> indexMap;
                setIndexMap(indexMap, hiraganaArray1, hiraganaArray2);
                makeStrokeKeysTable(nullptr, &hiraganaFirstIndexes, ROOT_STROKE_NODE, indexMap, 0, 0, 0);
            }
        }
        return hiraganaFirstIndexes;
    }

    //----------------------------------------------------------------------------
    void reorderByFirstStrokePosition(wchar_t* table, StrokeTableNode* pNode, const wstring& orderedChars, const std::set<wchar_t>& charSet, size_t firstLevelIdx, size_t depth) {
        for (size_t i = 0; i < STROKE_SPACE_DECKEY; ++i) {
            if (depth == 0) firstLevelIdx = i;
            Node* blk = pNode->getNth(i);
            if (blk) {
                if (blk->isStrokeTableNode()) {
                    reorderByFirstStrokePosition(table, (StrokeTableNode*)blk, orderedChars, charSet, firstLevelIdx, depth + 1);
                } else if (blk->isStringNode() || (depth == 0 && blk->isFunctionNode() && (dynamic_cast<MyCharNode*>(blk) || dynamic_cast<PrevCharNode*>(blk)))) {
                    wchar_t ch = 0;
                    if (blk->isStringNode()) {
                        auto ms = blk->getString();
                        if (ms.size() == 1) {
                            auto iter = charSet.find((wchar_t)ms[0]);
                            if (iter != charSet.end()) ch = *iter;
                        }
                    } else {
                        ch = DECKEY_TO_CHARS->GetCharFromDeckey(i);
                    }
                    if (ch != 0) {
                        // 見つかった第1打鍵位置に格納
                        if (table[firstLevelIdx * 2] != 0) {
                            // 既に格納済みなら、優先順の高いものと入れ替える
                            wchar_t buf[3];
                            buf[0] = ch;
                            buf[1] = table[firstLevelIdx * 2];
                            buf[2] = 0;
                            size_t pos = orderedChars.find_first_of(buf);
                            if (pos != wstring::npos) table[firstLevelIdx * 2] = orderedChars[pos];
                        } else {
                            table[firstLevelIdx * 2] = ch;
                        }
                    }
                }
            }
        }
    }

    // 指定の文字配列を第1ストロークの位置に従って並べかえる
    // table: 出力先のテーブル, targetChars: 並べ替えたい文字配列
    void ReorderByFirstStrokePosition(wchar_t* table, const wchar_t* targetChars, int tableId) {
        LOG_INFO(_T("CALLED: targetChars=%s"), targetChars);
        if (ROOT_STROKE_NODE) {
            wstring orderedChars = targetChars;
            std::set<wchar_t> charSet(orderedChars.begin(), orderedChars.end());
            for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
                table[i] = 0;
            }
            StrokeTableNode* node =
                tableId == 1 ? StrokeTableNode::RootStrokeNode1.get()
                : tableId == 2 ? StrokeTableNode::RootStrokeNode2.get()
                : tableId == 3 ? StrokeTableNode::RootStrokeNode3.get()
                : ROOT_STROKE_NODE;
            if (node) reorderByFirstStrokePosition(table, node, orderedChars, charSet, STROKE_SPACE_DECKEY, 0);
        }
    }

    // 指定の文字配列をストロークの位置に従って並べかえる
    // node: ストロークテーブルノード, table: 出力先のテーブル, targetChars: 並べ替えたい文字配列
    void ReorderByStrokePosition(StrokeTableNode* node, wchar_t* table, const wstring& targetChars, int tableId) {
        LOG_INFO(_T("CALLED: targetChars=%s"), targetChars.c_str());
        std::set<wchar_t> charSet(targetChars.begin(), targetChars.end());
        for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
            table[i] = 0;
        }
        reorderByFirstStrokePosition(table, node, targetChars, charSet, STROKE_SPACE_DECKEY, tableId);
    }

    //----------------------------------------------------------------------------
    // 外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
    void MakeExtraCharsStrokePositionTable(StrokeTableNode* rootStrokeNode, wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        if (rootStrokeNode) {
            size_t order1[6] = { 12, 22, 13, 23, 11, 21 };
            for (size_t i = 0; i < 10; ++i) {
                mchar_t ch = 0;
                auto blk = rootStrokeNode->getNth(i);
                if (blk && blk->isStrokeTableNode()) {
                    size_t offset = (i % 10) < 5 ? 0 : 5;
                    for (size_t j = 0; j < 6 && ch == 0; ++j) {
                        Node* sb = ((StrokeTableNode*)blk)->getNth(order1[j] + offset);
                        if (sb && sb->isStringNode()) {
                            ch = utils::safe_front(sb->getString());
                        }
                    }
                }
                set_facestr(ch, faces + i * 2);
            }

            size_t order2[5] = { 2, 1, 3, 4, 0 };
            for (size_t i = 10; i < STROKE_SPACE_DECKEY; ++i) {
                wchar_t ch = 0;
                auto blk = rootStrokeNode->getNth(i);
                if (blk && blk->isStrokeTableNode()) {
                    size_t offset = (i % 10) < 5 ? 0 : 5;
                    for (size_t j = 0; j < 5 && ch == 0; ++j) {
                        Node* sb = ((StrokeTableNode*)blk)->getNth(order2[j] + offset);
                        if (sb && sb->isStringNode()) {
                            ch = (wchar_t)utils::safe_front(sb->getString());
                        }
                    }
                }
                set_facestr(ch, faces + i * 2);
            }
        }
    }

    //----------------------------------------------------------------------------
    // キー文字を集めたストローク表を作成する
    void makeKeyCharsStrokePositionTable(StrokeTableNode* rootStrokeNode, wchar_t* faces, size_t start, size_t num) {
        for (size_t i = 0; i < num; ++i) {
            mchar_t ch = 0;
            if (rootStrokeNode) {
                auto blk = rootStrokeNode->getNth(start + i);
                if (blk) {
                    if (blk->isStrokeTableNode()) {
                        ch = _T("□")[0];
                    } else if (blk->isStringNode() || blk->isFunctionNode()) {
                        ch = utils::safe_front(blk->getString());
                    } else {
                        ch = _T("・")[0];
                    }
                }
            }
            set_facestr(ch, faces + i * 2);
        }
    }

    // 主テーブル用の外字を集めたストローク表を作成する
    void MakeExtraCharsStrokePositionTable1(wchar_t* faces) {
        MakeExtraCharsStrokePositionTable(StrokeTableNode::RootStrokeNode1.get(), faces);
    }

    // 副テーブル用の外字を集めたストローク表を作成する
    void MakeExtraCharsStrokePositionTable2(wchar_t* faces) {
        MakeExtraCharsStrokePositionTable(StrokeTableNode::RootStrokeNode2.get(), faces);
    }

    // 第3テーブル用の外字を集めたストローク表を作成する
    void MakeExtraCharsStrokePositionTable3(wchar_t* faces) {
        MakeExtraCharsStrokePositionTable(StrokeTableNode::RootStrokeNode3.get(), faces);
    }

    // 主テーブル用のキー文字を集めたストローク表を作成する
    void makeKeyCharsStrokePositionTable1(wchar_t* faces, size_t shiftPlane) {
        makeKeyCharsStrokePositionTable(StrokeTableNode::RootStrokeNode1.get(), faces, shiftPlane * PLANE_DECKEY_NUM, NORMAL_DECKEY_NUM);
    }

    // 副テーブル用のキー文字を集めたストローク表を作成する
    void makeKeyCharsStrokePositionTable2(wchar_t* faces, size_t shiftPlane) {
        makeKeyCharsStrokePositionTable(StrokeTableNode::RootStrokeNode2.get(), faces, shiftPlane * PLANE_DECKEY_NUM, NORMAL_DECKEY_NUM);
    }

    // 第3テーブル用のキー文字を集めたストローク表を作成する
    void makeKeyCharsStrokePositionTable3(wchar_t* faces, size_t shiftPlane) {
        makeKeyCharsStrokePositionTable(StrokeTableNode::RootStrokeNode3.get(), faces, shiftPlane * PLANE_DECKEY_NUM, NORMAL_DECKEY_NUM);
    }

    // 主テーブルに対して指定されたシフト面の単打ストローク表を作成する
    void MakeShiftPlaneKeyCharsStrokePositionTable1(wchar_t* faces, size_t shiftPlane) {
        LOG_INFO(_T("CALLED: shiftPlane=%d"), shiftPlane);
        makeKeyCharsStrokePositionTable1(faces, shiftPlane);
    }

    // 副テーブルに対して指定されたシフト面の単打ストローク表を作成する
    void MakeShiftPlaneKeyCharsStrokePositionTable2(wchar_t* faces, size_t shiftPlane) {
        LOG_INFO(_T("CALLED: shiftPlane=%d"), shiftPlane);
        makeKeyCharsStrokePositionTable2(faces, shiftPlane);
    }

    // 第3テーブルに対して指定されたシフト面の単打ストローク表を作成する
    void MakeShiftPlaneKeyCharsStrokePositionTable3(wchar_t* faces, size_t shiftPlane) {
        LOG_INFO(_T("CALLED: shiftPlane=%d"), shiftPlane);
        makeKeyCharsStrokePositionTable3(faces, shiftPlane);
    }

    // 通常面の単打ストローク表を作成する
    void MakeKeyCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable1(faces, 0);
    }

    // 第2テーブルの通常面の単打ストローク表を作成する
    void MakeKeyCharsStrokePositionTable2(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable2(faces, 0);
    }

    // 第3テーブルの通常面の単打ストローク表を作成する
    void MakeKeyCharsStrokePositionTable3(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable3(faces, 0);
    }

    // 同時打鍵面通常キー文字を集めたストローク表を作成する
    void MakeCombinationKeyCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable1(faces, COMBO_SHIFT_PLANE);
    }

    //----------------------------------------------------------------------------
    // 初期打鍵表(下端機能キー以外は空白)の作成
    void MakeInitialVkbTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        if (ROOT_STROKE_NODE) {
            for (size_t i = 0; i < STROKE_SPACE_DECKEY; ++i) {
                set_facestr(0, faces + i * 2);
            }
            for (size_t i = STROKE_SPACE_DECKEY; i < NORMAL_DECKEY_NUM; ++i) {
                wchar_t ch = _T("・")[0];
                auto blk = ROOT_STROKE_NODE->getNth(i);
                if (blk /*&& blk->isFunctionNode()*/) {
                    ch = (wchar_t)utils::safe_front(blk->getString());
                }
                set_facestr(ch, faces + i * 2);
            }
        }
    }

    //----------------------------------------------------------------------------
    std::vector<wstring> readBushuFile() {
        utils::IfstreamReader reader(SETTINGS->bushuFile);
        if (reader.success()) {
            return reader.getAllLines();
        }
        return std::vector<wstring>();
    }

    wstring convDeckeysToWstring(std::vector<int> deckeys) {
        wstring result;
        for (auto deckey : deckeys) {
            int dk = deckey % PLANE_DECKEY_NUM;
            if (dk >= NORMAL_DECKEY_NUM) {
                result.clear();
                break;
            }
            wchar_t ch = DECKEY_TO_CHARS->GetCharFromDeckey(dk);
            if (ch == 0) break;
            result.push_back(ch);
        }
        return result;
    }

    wstring makeRewriteDefLine(const wstring& prev, const wstring& path, const RewriteInfo& info) {
        wstring line = prev;
        line.append(path);
        line.push_back('\t');
        line.append(to_wstr(info.getOutStr()));
        MString nextStr = info.getNextStr();
        if (!nextStr.empty()) {
            line.push_back('\t');
            line.append(to_wstr(nextStr));
        }
        return line;
    }

    // ローマ字テーブルを作成してファイルに書き出す
    // prefix: 部首合成用, prefix2: 裏面定義用
    void SaveRomanStrokeTable(const wchar_t* prefix, const wchar_t* prefix2) {
        if (!StrokeTableNode::RootStrokeNode1) return;

        _LOG_DEBUGH(_T("CALLED: prefix=%s, prefix2=%s"), prefix, prefix2);

        utils::OfstreamWriter writer(utils::joinPath(SETTINGS->rootDir, _T("roman-stroke-table.txt")));
        if (writer.success()) {
            // テーブルファイルから
            bool bPostRewrite = StrokeTableNode::RootStrokeNode1->hasPostRewriteNode();

            auto pfx2 = prefix2 && wcslen(prefix2) > 0 ? prefix2 : !SETTINGS->romanSecPlanePrefix.empty() ? SETTINGS->romanSecPlanePrefix.c_str() : _T(":");
            MString cmdMarker = to_mstr(_T("!{"));

            // 文字から、その文字の打鍵列へのマップに追加 (通常面)
            StrokeTreeTraverser traverser(StrokeTableNode::RootStrokeNode1.get(), true);
            while (true) {
                Node* np = traverser.getNext();
                if (!np) break;

                wstring origPath = convDeckeysToWstring(traverser.getPath());
                if (origPath.empty()) continue;

                StringNode* sp = dynamic_cast<StringNode*>(np);
                if (sp) {
                    auto ms = sp->getString();
                    if (ms.empty() || ms.find(cmdMarker) != MString::npos) continue;

                    if (bPostRewrite) {
                        size_t rewritableLen = sp->getRewritableLen();
                        if (rewritableLen > 0) ms.insert(rewritableLen > ms.size() ? 0 : ms.size() - rewritableLen, 1, '\t');   // 「次の入力」の位置に TAB を挿入しておく
                    }

                    // 最初に出現する空白はromanSecPlanePrefixで置換
                    wstring strPath = utils::replace(origPath, _T(" "), pfx2);
                    if (strPath.find(' ') == wstring::npos) {
                        // 2つ以上の空白文字を含まないものだけを対象とする
                        if (origPath.find(' ') == wstring::npos || origPath.front() == ' ') {
                            // 空白を含まないか、または先頭のみが空白文字
                            writer.writeLine(utils::utf8_encode(
                                utils::format(_T("%s\t\t%s"), strPath.c_str(), MAKE_WPTR(ms))));
                        } else {
                            writer.writeLine(utils::utf8_encode(
                                utils::format(_T("%s%s\t\t%s"),
                                    pfx2,
                                    strPath.c_str(),
                                    MAKE_WPTR(ms))));
                        }
                    }
                } else if (origPath.find(' ') == wstring::npos) {
                    // 後置書き換え(空白を含まないものだけ)
                    PostRewriteOneShotNode* prnp = dynamic_cast<PostRewriteOneShotNode*>(np);
                    if (prnp) {
                        writer.writeLine(utils::utf8_encode(makeRewriteDefLine(_T(""), origPath, prnp->getRewriteInfo())));
                        for (auto pair : prnp->getRewriteMap()) {
                            writer.writeLine(utils::utf8_encode(makeRewriteDefLine(to_wstr(pair.first), origPath, pair.second)));
                        }
                    }
                }
            }
            // 部首合成から
            if (prefix && wcslen(prefix) > 0) {
                _LOG_DEBUGH(_T("BUSHU_COMP: prefix=%s"), prefix);
                for (const auto& line : readBushuFile()) {
                    if (line.size() == 3) {
                        auto list1 = utils::replace(convDeckeysToWstring(StrokeTableNode::RootStrokeNode1->getStrokeList(to_mstr(line.substr(1, 1)), false)), _T(" "), pfx2);
                        auto list2 = utils::replace(convDeckeysToWstring(StrokeTableNode::RootStrokeNode1->getStrokeList(to_mstr(line.substr(2, 1)), false)), _T(" "), pfx2);
                        if (!list1.empty() && !list2.empty()) {
                            writer.writeLine(utils::utf8_encode(
                                utils::format(_T("%s%s%s\t%s"),
                                    prefix,
                                    list1.c_str(),
                                    list2.c_str(),
                                    line.substr(0, 1).c_str())));
                        }
                    }
                }
            }
        }
    }

    // eelll/JS用テーブルを作成してファイルに書き出す
    void SaveEelllJsTable() {
        if (!StrokeTableNode::RootStrokeNode1) return;

        utils::OfstreamWriter writer(utils::joinPath(SETTINGS->rootDir, _T("eelll-js-table.txt")));
        if (writer.success()) {
            // 文字から、その文字の打鍵列へのマップに追加 (通常面)
            StrokeTreeTraverser traverser(StrokeTableNode::RootStrokeNode1.get(), false);
            while (true) {
                Node* np = traverser.getNext();
                if (!np) break;

                StringNode* sp = dynamic_cast<StringNode*>(np);
                if (!sp) continue;

                auto ms = sp->getString();
                if (ms.empty()) continue;

                auto path = traverser.getPath();
                if (path.empty() || path[0] >= NORMAL_DECKEY_NUM) continue;

                writer.writeLine(utils::utf8_encode(
                    utils::format(_T("%s=%s"), MAKE_WPTR(ms), convDeckeysToWstring(path).c_str())));
            }
        }
    }

    // デバッグ用テーブルを作成してファイルに書き出す
    void saveDebugTable(StrokeTableNode* tbl, const wstring& outfile) {
        if (!tbl) return;

        utils::OfstreamWriter writer(utils::joinPath(SETTINGS->rootDir, outfile));
        if (writer.success()) {
            // テーブルファイルから
            bool bPostRewrite = tbl->hasPostRewriteNode();

            // 文字から、その文字の打鍵列へのマップに追加 (通常面)
            StrokeTreeTraverser traverser(tbl, true);
            while (true) {
                Node* np = traverser.getNext();
                if (!np) break;

                wstring origPath;
                for (int x : traverser.getPath()) {
                    if (!origPath.empty()) origPath.push_back(':');
                    origPath.append(std::to_wstring(x));
                }
                if (origPath.empty()) continue;

                StringNode* sp = dynamic_cast<StringNode*>(np);
                if (sp) {
                    auto ms = sp->getString();

                    if (bPostRewrite) {
                        size_t rewritableLen = sp->getRewritableLen();
                        if (rewritableLen > 0) ms.insert(rewritableLen > ms.size() ? 0 : ms.size() - rewritableLen, 1, '/');   // 「次の入力」の位置に '/' を挿入しておく
                    }

                    writer.writeLine(utils::utf8_encode(
                        utils::format(_T("%s\t\"%s\""), origPath.c_str(), MAKE_WPTR(ms))));
                } else {
                    // 後置書き換え
                    PostRewriteOneShotNode* prnp = dynamic_cast<PostRewriteOneShotNode*>(np);
                    if (prnp) {
                        writer.writeLine(utils::utf8_encode(makeRewriteDefLine(_T(""), origPath, prnp->getRewriteInfo())));
                        for (auto pair : prnp->getRewriteMap()) {
                            writer.writeLine(utils::utf8_encode(makeRewriteDefLine(to_wstr(pair.first), origPath, pair.second)));
                        }
                    } else {
                        // 機能ノード
                        FunctionNode* fp = dynamic_cast<FunctionNode*>(np);
                        if (fp) {
                            writer.writeLine(utils::utf8_encode(
                                utils::format(_T("%s\t@%s"), origPath.c_str(), MAKE_WPTR(fp->getString()))));
                        }
                    }
                }
            }
        }
    }

    // デバッグ用テーブルを作成してファイルに書き出す
    void SaveDumpTable() {
        saveDebugTable(StrokeTableNode::RootStrokeNode1.get(), _T("tmp/dump-table1.txt"));
        saveDebugTable(StrokeTableNode::RootStrokeNode2.get(), _T("tmp/dump-table2.txt"));
        saveDebugTable(StrokeTableNode::RootStrokeNode3.get(), _T("tmp/dump-table3.txt"));
    }

}

