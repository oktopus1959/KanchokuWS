// StrokeHelp
#include "file_utils.h"
#include "StrokeTable.h"
#include "StringNode.h"

#include "deckey_id_defs.h"
#include "StrokeHelp.h"

#define STROKE_HELP_ARRAY_SIZE 10000

namespace {
}

// Decoder.cpp で生成される
std::unique_ptr<StrokeHelp> StrokeHelp::Singleton;

StrokeHelp::StrokeHelp() : temp{} {
    //strokeHelpArray = new int[STROKE_HELP_ARRAY_SIZE];
}

StrokeHelp::~StrokeHelp() {
    //delete[] strokeHelpArray;
}

// 各文字に対するストロークを求める
void StrokeHelp::GatherStrokeHelp() {
    //if (Singleton) return;

    Singleton.reset(new StrokeHelp());

    if (ROOT_STROKE_NODE) Singleton->gatherStrokeHelp(ROOT_STROKE_NODE, 0);
}

void StrokeHelp::gatherStrokeHelp(StrokeTableNode* pNode, size_t depth) {
    if (depth >= utils::array_length(temp)) return;

    for (size_t i = 0; i < STROKE_SPACE_DECKEY; ++i) {
        Node* blk = pNode->getNth(i);
        if (blk) {
            if (blk->isStrokeTableNode()) {
                temp[depth] = (int)i;
                gatherStrokeHelp((StrokeTableNode*)blk, depth + 1);
            } else if (blk->isStringLikeNode()) {
                if (blk->getString().size() == 1) {
                    //if (blk->getString()[0] == L'邋') {
                    //    int j = 0;
                    //}
                    auto& vec = strokeHelpMap[utils::safe_front(blk->getString())];
                    if (vec.size() > depth + 1) vec.clear();
                    if (vec.empty()) {
                        for (size_t j = 0; j < depth; ++j) {
                            vec.push_back(temp[j]);
                        }
                        vec.push_back((int)i);
                    }
                }
            }
        }
    }
}

namespace {
    const String SameStrokeMarkers = L"◎③④⑤⑥";
    const String strokeMarkers = _T("●○▲△▼▽");

    //仮想鍵盤にストロークヘルプの情報を設定する (faces は 1セルが 2 wchar で構成されることに注意)
    template<typename T>
    bool copyStrokeHelpToVkbFacesImpl(const std::vector<int>& strokes, T* faces, size_t factor, size_t facesSize) {
        if (strokes.empty()) return false;

        size_t count = 0;
        bool bSsmFound = false;
        for (int strPos : strokes) {
            if (count >= strokeMarkers.size()) {
                break;
            }
            size_t facePos = strPos * factor;
            if (facePos < facesSize) {
                auto marker = faces[facePos];
                size_t ssmCnt = 0;
                while (ssmCnt < SameStrokeMarkers.size()) {
                    if (marker == SameStrokeMarkers[ssmCnt]) {
                        if (ssmCnt + 1 < SameStrokeMarkers.size()) {
                            faces[facePos] = SameStrokeMarkers[ssmCnt + 1];
                        }
                        break;
                    }
                    ++ssmCnt;
                }
                if (ssmCnt == SameStrokeMarkers.size()) {
                    if (marker == 0) {
                        faces[facePos] = strokeMarkers[count];
                    } else {
                        if (bSsmFound) break;   // 2つ以上の重複ストロークがあったら停止する
                        faces[facePos] = SameStrokeMarkers[0];
                        bSsmFound = true;
                    }
                }
            }
            ++count;
        }
            
        return true;

    }
}

//仮想鍵盤にストロークヘルプの情報を設定する
bool StrokeHelp::copyStrokeHelpToVkbFacesOutParams(mchar_t ch, wchar_t* faces, size_t facesSize) {
    return copyStrokeHelpToVkbFacesImpl(GetStrokeHelp(ch), faces, 2, facesSize);
}

//仮想鍵盤にストロークヘルプの情報を設定する
bool StrokeHelp::copyStrokeHelpToVkbFacesStateCommon(mchar_t ch, mchar_t* faces, size_t facesSize) {
    return copyStrokeHelpToVkbFacesImpl(GetStrokeHelp(ch), faces, 1, facesSize);
}
