// StrokeTableMaker
#include "file_utils.h"
#include "StrokeTable.h"
#include "StringNode.h"
#include "MyPrevChar.h"
#include "DecKeyToChars.h"

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
    wstring hiraganaArray1 = _T("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもや ゆ よらりるれろわ ん を");

    wstring hiraganaArray2 = _T("ぁぃぅぇぉがぎぐげござじずぜぞだぢづでど     ばびぶべぼぱぴぷぺぽゃ ゅ ょゕ  ゖ ゎゐゔゑ ");

    wstring katakanaArray1 = _T("アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤ ユ ヨラリルレロワ ン ヲ");

    wstring katakanaArray2 = _T("ァィゥェォガギグゲゴザジズゼゾダヂヅデド     バビブベボパピプペポャ ュ ョヵ  ヶ ヮヰヴヱ ");

    // ひらがなに到る第1打鍵集合
    std::set<int> hiraganaFirstIndexes;

    void makeStrokeKeysTable(wchar_t* table, std::set<int>* pSet, StrokeTableNode* pNode, const std::map<wchar_t, size_t>& idxMap, size_t firstIdx, size_t secondIdx, size_t depth) {
        for (size_t i = 0; i < STROKE_SPACE_DECKEY; ++i) {
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
        makeStrokeKeysTable(table, nullptr, ROOT_STROKE_NODE.get(), indexMap, STROKE_SPACE_DECKEY, STROKE_SPACE_DECKEY, 0);
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

        makeStrokeKeysTable(table, nullptr, ROOT_STROKE_NODE.get(), indexMap, 0, 0, 0);
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
            makeStrokeKeysTable(nullptr, &hiraganaFirstIndexes, ROOT_STROKE_NODE.get(), indexMap, 0, 0, 0);
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
                        auto iter = charSet.find((wchar_t)blk->getString()[0]);
                        if (iter != charSet.end()) ch = *iter;
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
    void ReorderByFirstStrokePosition(wchar_t* table, const wchar_t* targetChars) {
        LOG_INFO(_T("CALLED"));
        wstring orderedChars = targetChars;
        std::set<wchar_t> charSet(orderedChars.begin(), orderedChars.end());
        for (size_t i = 0; i < OUT_TABLE_SIZE; ++i) {
            table[i] = 0;
        }
        reorderByFirstStrokePosition(table, ROOT_STROKE_NODE.get(), orderedChars, charSet, STROKE_SPACE_DECKEY, 0);
    }

    // 指定の文字配列をストロークの位置に従って並べかえる
    // node: ストロークテーブルノード, table: 出力先のテーブル, targetChars: 並べ替えたい文字配列
    void ReorderByStrokePosition(StrokeTableNode* node, wchar_t* table, const wstring& targetChars) {
        LOG_INFO(_T("CALLED"));
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
    // シフトキー文字を集めたストローク表を作成する
    void MakeShiftKeyCharsStrokePositionTable(wchar_t* faces) {
        LOG_INFO(_T("CALLED"));
        for (size_t i = 0; i < STROKE_SPACE_DECKEY; ++i) {
            mchar_t ch = 0;
            auto blk = ROOT_STROKE_NODE->getNth(SHIFT_DECKEY_START + i);
            if (blk) {
                if (blk->isStrokeTableNode()) {
                    ch = _T("□")[0];
                } else if (blk->isStringNode()) {
                    ch = utils::safe_front(blk->getString());
                } else {
                    ch = _T("・")[0];
                }
            }
            set_facestr(ch, faces + i * 2);
        }
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

}

