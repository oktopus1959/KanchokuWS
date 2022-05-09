// StrokeTableMaker
#include "file_utils.h"
#include "path_utils.h"
#include "StrokeTable.h"
#include "StringNode.h"
#include "MyPrevChar.h"
#include "DecKeyToChars.h"
#include "Settings/Settings.h"

#include "VkbTableMaker.h"
//#define OUT_TABLE_SIZE 200
//#define VKB_TABLE_SIZE 50

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
    // 文字に到る打鍵列
    std::map<MString, std::vector<int>> strokeSerieses;
    std::map<MString, std::vector<int>> strokeSerieses2;

    std::map<MString, std::vector<int>>* StrokeSerieses() { return &strokeSerieses; }
    std::map<MString, std::vector<int>>* StrokeSerieses2() { return &strokeSerieses2; }

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
        std::map<wchar_t, size_t> indexMap;

        for (size_t i = 0; targetChars[i] && i < VKB_TABLE_SIZE ; ++i) {
            indexMap[targetChars[i]] = i;
        }
        for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
            table[i] = 0;
        }
        makeStrokeKeysTable(table, nullptr, ROOT_STROKE_NODE, indexMap, STROKE_SPACE_DECKEY, STROKE_SPACE_DECKEY, 0);
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
        std::map<wchar_t, size_t> indexMap;
        setIndexMap(indexMap, kanaArray1, kanaArray2);

        for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
            table[i] = 0;
        }

        makeStrokeKeysTable(table, nullptr, ROOT_STROKE_NODE, indexMap, 0, 0, 0);
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
        if (hiraganaFirstIndexes.empty()) {
            std::map<wchar_t, size_t> indexMap;
            setIndexMap(indexMap, hiraganaArray1, hiraganaArray2);
            makeStrokeKeysTable(nullptr, &hiraganaFirstIndexes, ROOT_STROKE_NODE, indexMap, 0, 0, 0);
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
        wstring orderedChars = targetChars;
        std::set<wchar_t> charSet(orderedChars.begin(), orderedChars.end());
        for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
            table[i] = 0;
        }
        StrokeTableNode* node = tableId == 1 ? StrokeTableNode::RootStrokeNode1.get() : tableId == 2 ? StrokeTableNode::RootStrokeNode2.get() : ROOT_STROKE_NODE;
        reorderByFirstStrokePosition(table, node, orderedChars, charSet, STROKE_SPACE_DECKEY, 0);
    }

    // 指定の文字配列をストロークの位置に従って並べかえる
    // node: ストロークテーブルノード, table: 出力先のテーブル, targetChars: 並べ替えたい文字配列
    void ReorderByStrokePosition(StrokeTableNode* node, wchar_t* table, const wstring& targetChars) {
        LOG_INFO(_T("CALLED: targetChars=%s"), targetChars.c_str());
        std::set<wchar_t> charSet(targetChars.begin(), targetChars.end());
        for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
            table[i] = 0;
        }
        reorderByFirstStrokePosition(table, node, targetChars, charSet, STROKE_SPACE_DECKEY, 0);
    }

    //----------------------------------------------------------------------------
    // 外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
    void MakeExtraCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        size_t order1[6] = { 12, 22, 13, 23, 11, 21 };
        for (size_t i = 0; i < 10; ++i) {
            mchar_t ch = 0;
            auto blk = ROOT_STROKE_NODE->getNth(i);
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
            auto blk = ROOT_STROKE_NODE->getNth(i);
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
                    } else if (blk->isStringNode()) {
                        ch = utils::safe_front(blk->getString());
                    } else {
                        ch = _T("・")[0];
                    }
                }
            }
            set_facestr(ch, faces + i * 2);
        }
    }

    // キー文字を集めたストローク表を作成する
    void makeKeyCharsStrokePositionTable1(wchar_t* faces, size_t start, size_t num) {
        makeKeyCharsStrokePositionTable(StrokeTableNode::RootStrokeNode1.get(), faces, start, num);
    }

    // キー文字を集めたストローク表を作成する
    void makeKeyCharsStrokePositionTable2(wchar_t* faces, size_t start, size_t num) {
        makeKeyCharsStrokePositionTable(StrokeTableNode::RootStrokeNode2.get(), faces, start, num);
    }

    // アンシフトキー文字を集めたストローク表を作成する
    void MakeKeyCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable1(faces, 0, NORMAL_DECKEY_NUM);
    }

    // 第2テーブルからアンシフトキー文字を集めたストローク表を作成する
    void MakeKeyCharsStrokePositionTable2(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable2(faces, 0, NORMAL_DECKEY_NUM);
    }

    // シフトキー文字を集めたストローク表を作成する
    void MakeShiftKeyCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable1(faces, SHIFT_DECKEY_START, STROKE_SPACE_DECKEY);
    }

    // シフトA面キー文字を集めたストローク表を作成する
    void MakeShiftAKeyCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable1(faces, SHIFT_A_DECKEY_START, SHIFT_DECKEY_NUM);
    }

    // シフトB面キー文字を集めたストローク表を作成する
    void MakeShiftBKeyCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable1(faces, SHIFT_B_DECKEY_START, SHIFT_DECKEY_NUM);
    }

    // 同時打鍵シフト面キー文字を集めたストローク表を作成する
    void MakeCombinationKeyCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        makeKeyCharsStrokePositionTable1(faces, COMBO_DECKEY_START, SHIFT_DECKEY_NUM);
    }

    //----------------------------------------------------------------------------
    // 初期打鍵表(下端機能キー以外は空白)の作成
    void MakeInitialVkbTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
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
            wchar_t ch = DECKEY_TO_CHARS->GetCharFromDeckey(deckey);
            if (ch == 0) break;
            result.push_back(ch);
        }
        return result;
    }

    // ローマ字テーブルを作成してファイルに書き出す
    void SaveRomanStrokeTable(const wchar_t* prefix, const wchar_t* prefix2) {
        auto path = utils::joinPath(SETTINGS->rootDir, _T("roman-stroke-table.txt"));
        utils::OfstreamWriter writer(path);
        if (writer.success()) {
            // テーブルファイルから
            for (const auto& pair : strokeSerieses) {
                //if (pair.first.length() > 1) {
                //    LOG_DEBUGH(_T("str=%s, strokeLen=%d"), MAKE_WPTR(pair.first), pair.second.size());
                //}
                if (!pair.first.empty() && !pair.second.empty()) {
                    // 重複した出力文字(列)の場合は末尾にTABが付加されているのでそれを除去してから書き出し
                    // また、最初に出現する空白はromanSecPlanePrefixで置換
                    wstring str = utils::replace(convDeckeysToWstring(pair.second), _T(" "), SETTINGS->romanSecPlanePrefix.c_str());
                    if (str.find(' ') == wstring::npos) {
                        // 空白文字を含まないものだけを対象とする
                        writer.writeLine(utils::utf8_encode(
                            utils::format(_T("%s\t%s"), str.c_str(), MAKE_WPTR(utils::strip(pair.first, _T("\t"))))));
                    }
                }
            }
            // テーブルファイルの裏面から
            for (const auto& pair : strokeSerieses2) {
                //if (pair.first.length() > 1) {
                //    LOG_DEBUGH(_T("str=%s, strokeLen=%d"), MAKE_WPTR(pair.first), pair.second.size());
                //}
                if (!pair.first.empty() && !pair.second.empty()) {
                    // 重複した出力文字(列)の場合は末尾にTABが付加されているのでそれを除去してから書き出し
                    // また、最初に出現する空白はromanSecPlanePrefixで置換
                    wstring str = utils::replace(convDeckeysToWstring(pair.second), _T(" "), SETTINGS->romanSecPlanePrefix.c_str());
                    if (str.find(' ') == wstring::npos) {
                        // 空白文字を含まないものだけを対象とする
                        writer.writeLine(utils::utf8_encode(
                            utils::format(_T("%s%s\t%s"),
                                prefix2 && wcslen(prefix2) > 0 ? prefix2 : SETTINGS->romanSecPlanePrefix.c_str(),
                                str.c_str(),
                                MAKE_WPTR(utils::strip(pair.first, _T("\t"))))));
                    }
                }
            }
            // 部首合成から
            for (const auto& line : readBushuFile()) {
                if (line.size() == 3) {
                    auto iter1 = strokeSerieses.find(to_mstr(line.substr(1, 1)));
                    auto iter2 = strokeSerieses.find(to_mstr(line.substr(2, 1)));
                    if (iter1 != strokeSerieses.end() && iter2 != strokeSerieses.end()) {
                        writer.writeLine(utils::utf8_encode(
                            utils::format(_T("%s%s%s\t%s"),
                                prefix && wcslen(prefix) > 0 ? prefix : SETTINGS->romanBushuCompPrefix.c_str(),
                                convDeckeysToWstring(iter1->second).c_str(),
                                convDeckeysToWstring(iter2->second).c_str(),
                                line.substr(0, 1).c_str())));
                    }
                }
            }
        }
    }

    // eelll/JS用テーブルを作成してファイルに書き出す
    void SaveEelllJsTable() {
        auto path = utils::joinPath(SETTINGS->rootDir, _T("eelll-js-table.txt"));
        utils::OfstreamWriter writer(path);
        if (writer.success()) {
            // テーブルファイルから
            for (const auto& pair : strokeSerieses) {
                if (!pair.first.empty() && !pair.second.empty()) {
                    // 重複した出力文字(列)の場合は末尾にTABが付加されているのでそれを除去してから書き出し
                    writer.writeLine(utils::utf8_encode(
                        utils::format(_T("%s=%s"), MAKE_WPTR(utils::strip(pair.first, _T("\t"))), convDeckeysToWstring(pair.second).c_str())));
                }
            }
        }
    }

}

